using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using ScreenRecorderLib;
using QASmartTouch.Services;
using QASmartTouch.Services.Camera;

namespace QASmartTouch.Forms
{
    public partial class PreRecordSetupDialog : Window
    {
        private class AudioDeviceItem
        {
            public string DisplayName { get; set; } = "";
            public string DeviceName { get; set; } = "";
            public string RawFriendlyName { get; set; } = "";
            public override string ToString() => DisplayName;
        }

        private class CameraDeviceItem
        {
            public string DisplayName { get; set; } = "";
            public string DeviceId { get; set; } = "0";
            public override string ToString() => DisplayName;
        }

        private readonly DispatcherTimer _meterTimer;
        private CameraPreviewService? _dialogCameraService;

        // Real-time WASAPI Hardware Microphone Level Monitor
        private MMDeviceEnumerator? _mmEnumerator;
        private MMDevice? _activeMicDevice;
        private WasapiCapture? _micCapture;
        private volatile float _latestBufferPeak = 0f;
        private double _smoothedLevel = 0.0;
        private bool _hasMicHardware = false;
        private bool _hasCameraHardware = false;
        private bool _isScanningCameras = true;
        private Task? _cameraScanTask;
        private bool _isSuppressingCameraSelectionChanged = false;
        private bool _isStartingRecording = false;
        private bool _isCancelledOrClosed = false;

        public PreRecordSetupDialog()
        {
            InitializeComponent();
            QASmartTouch.Helpers.TouchActivationHelper.ApplyToWindow(this);

            _meterTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _meterTimer.Tick += MeterTimer_Tick;

            Loaded += PreRecordSetupDialog_Loaded;
            Unloaded += PreRecordSetupDialog_Unloaded;
        }

        private void PreRecordSetupDialog_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateCameraUiState();
            UpdateCameraPreviewShape();

            // Khôi phục lựa chọn chất lượng gần nhất trong phiên làm việc
            bool isCompact = ScreenRecorderService.Instance.CurrentQualityPreset == RecordingQualityPreset.Compact720p;
            if (rbQuality720p != null) rbQuality720p.IsChecked = isCompact;
            if (rbQuality1080p != null) rbQuality1080p.IsChecked = !isCompact;

            LoadAudioDevices();
            StartRealMicMonitor();
            UpdateMicrophoneUiState();

            _cameraScanTask = LoadCameraDevicesAsync();
            _meterTimer.Start();

            EnsureWindowWithinWorkArea();
        }

        private void EnsureWindowWithinWorkArea()
        {
            try
            {
                var workArea = SystemParameters.WorkArea;
                if (ActualWidth > 0 && ActualHeight > 0)
                {
                    Left = workArea.Left + Math.Max(10, (workArea.Width - ActualWidth) / 2);
                    Top = workArea.Top + Math.Max(10, (workArea.Height - ActualHeight) / 2);
                }
            }
            catch { }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch { }
            }
        }

        private void PreRecordSetupDialog_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!_isStartingRecording)
            {
                _isCancelledOrClosed = true;
            }
            _meterTimer.Stop();
            StopRealMicMonitor();

            var svc = _dialogCameraService;
            _dialogCameraService = null;
            if (svc != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await svc.StopAsync().ConfigureAwait(false);
                        svc.Dispose();
                    }
                    catch { }
                });
            }
        }

        #region Real-Time Hardware Microphone Level Metering (NAudio WASAPI)
        private void StartRealMicMonitor()
        {
            StopRealMicMonitor();

            if (chkMicrophone?.IsChecked != true)
            {
                _smoothedLevel = 0;
                if (pbAudioMeter != null) pbAudioMeter.Value = 0;
                return;
            }

            try
            {
                _mmEnumerator = new MMDeviceEnumerator();
                var endpoints = _mmEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();

                if (endpoints.Count == 0)
                {
                    _hasMicHardware = false;
                    return;
                }

                var selectedItem = cboMicrophone?.SelectedItem as AudioDeviceItem;
                MMDevice? targetDevice = null;

                if (selectedItem != null)
                {
                    // 1. Khớp theo Device ID
                    if (!string.IsNullOrEmpty(selectedItem.DeviceName))
                    {
                        targetDevice = endpoints.FirstOrDefault(ep =>
                            string.Equals(ep.ID, selectedItem.DeviceName, StringComparison.OrdinalIgnoreCase) ||
                            ep.ID.Contains(selectedItem.DeviceName, StringComparison.OrdinalIgnoreCase) ||
                            selectedItem.DeviceName.Contains(ep.ID, StringComparison.OrdinalIgnoreCase));
                    }

                    // 2. Khớp theo FriendlyName nếu ID khác định dạng
                    if (targetDevice == null && !string.IsNullOrEmpty(selectedItem.RawFriendlyName))
                    {
                        targetDevice = endpoints.FirstOrDefault(ep =>
                            string.Equals(ep.FriendlyName, selectedItem.RawFriendlyName, StringComparison.OrdinalIgnoreCase) ||
                            ep.FriendlyName.Contains(selectedItem.RawFriendlyName, StringComparison.OrdinalIgnoreCase) ||
                            selectedItem.RawFriendlyName.Contains(ep.FriendlyName, StringComparison.OrdinalIgnoreCase));
                    }
                }

                // 3. Fallback về thiết bị thu âm mặc định của hệ thống (ưu tiên thiết bị thật không phải tranScreen)
                targetDevice ??= endpoints.FirstOrDefault(ep => !ep.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                              ?? _mmEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia)
                              ?? endpoints[0];

                _activeMicDevice = targetDevice;
                _hasMicHardware = _activeMicDevice != null;

                if (_activeMicDevice != null)
                {
                    // Mở luồng WasapiCapture nhẹ (30ms buffer) để kích hoạt bộ đo phần cứng và tính biên độ sóng âm thực tế
                    _micCapture = new WasapiCapture(_activeMicDevice, true, 30);
                    _micCapture.DataAvailable += OnMicDataAvailable;
                    _micCapture.StartRecording();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StartRealMicMonitor warning: {ex.Message}");
                _hasMicHardware = false;
            }
        }

        private void OnMicDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded <= 0 || _micCapture == null) return;

            try
            {
                float maxPeak = 0f;
                var format = _micCapture.WaveFormat;

                if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
                {
                    for (int i = 0; i <= e.BytesRecorded - 4; i += 4)
                    {
                        float sample = Math.Abs(BitConverter.ToSingle(e.Buffer, i));
                        if (sample > maxPeak) maxPeak = sample;
                    }
                }
                else if (format.BitsPerSample == 16)
                {
                    for (int i = 0; i <= e.BytesRecorded - 2; i += 2)
                    {
                        short sample16 = BitConverter.ToInt16(e.Buffer, i);
                        float sample = Math.Abs(sample16 / 32768f);
                        if (sample > maxPeak) maxPeak = sample;
                    }
                }

                if (maxPeak > _latestBufferPeak)
                {
                    _latestBufferPeak = maxPeak;
                }
            }
            catch { }
        }

        private void StopRealMicMonitor()
        {
            _latestBufferPeak = 0f;
            _smoothedLevel = 0;

            if (_micCapture != null)
            {
                try
                {
                    _micCapture.DataAvailable -= OnMicDataAvailable;
                    _micCapture.StopRecording();
                }
                catch { }
                try
                {
                    _micCapture.Dispose();
                }
                catch { }
                _micCapture = null;
            }

            if (_activeMicDevice != null)
            {
                try
                {
                    _activeMicDevice.Dispose();
                }
                catch { }
                _activeMicDevice = null;
            }

            if (_mmEnumerator != null)
            {
                try
                {
                    _mmEnumerator.Dispose();
                }
                catch { }
                _mmEnumerator = null;
            }
        }

        private void MeterTimer_Tick(object? sender, EventArgs e)
        {
            if (pbAudioMeter == null) return;

            if (chkMicrophone?.IsChecked != true)
            {
                pbAudioMeter.Value = 0;
                return;
            }

            float rawPeak = _latestBufferPeak;
            _latestBufferPeak = 0f; // Reset sau mỗi chu kỳ đọc

            try
            {
                if (_activeMicDevice != null)
                {
                    float meterPeak = _activeMicDevice.AudioMeterInformation.MasterPeakValue;
                    if (meterPeak > rawPeak)
                    {
                        rawPeak = meterPeak;
                    }
                }
            }
            catch { }

            // Khử nhiễu nền cực nhỏ (Noise gate < 0.8%) để khi im lặng thanh về 0 tuyệt đối
            double targetPercent = 0.0;
            if (rawPeak > 0.008f)
            {
                // Ánh xạ phi tuyến (Log/Power curve) giúp giọng nói bình thường hiển thị nhạy và trực quan trên thang 0-100%
                targetPercent = Math.Min(100.0, Math.Pow(rawPeak, 0.55) * 115.0);
            }

            // Làm mượt chuyển động (Attack nhanh khi nói, Decay êm khi ngắt lời)
            if (targetPercent >= _smoothedLevel)
            {
                _smoothedLevel = _smoothedLevel * 0.3 + targetPercent * 0.7;
            }
            else
            {
                _smoothedLevel = _smoothedLevel * 0.65 + targetPercent * 0.35;
                if (_smoothedLevel < 1.0) _smoothedLevel = 0.0;
            }

            pbAudioMeter.Value = _smoothedLevel;

            // Cập nhật trạng thái phản hồi trực quan theo tín hiệu thật
            if (dotMicStatus != null && txtMicStatus != null)
            {
                if (!_hasMicHardware)
                {
                    dotMicStatus.Fill = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                    txtMicStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                    txtMicStatus.Text = "Không tìm thấy Mic";
                }
                else if (_smoothedLevel > 6.0)
                {
                    dotMicStatus.Fill = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
                    txtMicStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
                    txtMicStatus.Text = "Có tín hiệu";
                }
                else
                {
                    dotMicStatus.Fill = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
                    txtMicStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
                    txtMicStatus.Text = "Sẵn sàng";
                }
            }
        }
        #endregion

        private void LoadAudioDevices()
        {
            var items = new List<AudioDeviceItem>();
            try
            {
                var inputDevices = Recorder.GetSystemAudioCaptureDevices();

                if (inputDevices != null && inputDevices.Count > 0)
                {
                    // Sắp xếp: Ưu tiên thiết bị phần cứng thật (không phải virtual driver như tranScreen)
                    var sortedDevices = inputDevices
                        .OrderByDescending(d => !d.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                        .ThenByDescending(d => d.IsDefaultDevice)
                        .ToList();

                    foreach (var d in sortedDevices)
                    {
                        var isTranScreen = d.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase);
                        var tag = d.IsDefaultDevice ? (isTranScreen ? " [Thiết bị ảo]" : " [Mặc định]") : (isTranScreen ? " [Thiết bị ảo]" : "");
                        items.Add(new AudioDeviceItem
                        {
                            DisplayName = $"{d.FriendlyName}{tag}",
                            DeviceName = d.DeviceName,
                            RawFriendlyName = d.FriendlyName
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading mics: {ex.Message}");
            }

            if (items.Count > 0)
            {
                _hasMicHardware = true;
                cboMicrophone.ItemsSource = items;
                cboMicrophone.IsEnabled = true;

                var defItem = items.FirstOrDefault(i => !i.DisplayName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase) && i.DisplayName.Contains("[Mặc định]"))
                           ?? items.FirstOrDefault(i => !i.DisplayName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                           ?? items.FirstOrDefault();
                cboMicrophone.SelectedItem = defItem;

                if (chkMicrophone != null)
                {
                    chkMicrophone.IsEnabled = true;
                }
            }
            else
            {
                _hasMicHardware = false;
                items.Add(new AudioDeviceItem
                {
                    DisplayName = "Không tìm thấy thiết bị thu âm",
                    DeviceName = "",
                    RawFriendlyName = ""
                });
                cboMicrophone.ItemsSource = items;
                cboMicrophone.SelectedIndex = 0;
                cboMicrophone.IsEnabled = false;

                if (chkMicrophone != null)
                {
                    chkMicrophone.IsChecked = false;
                    chkMicrophone.IsEnabled = false;
                }
            }
        }

        private void cboMicrophone_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && _hasMicHardware && chkMicrophone?.IsChecked == true)
            {
                StartRealMicMonitor();
                UpdateMicrophoneUiState();
            }
        }

        private async Task LoadCameraDevicesAsync()
        {
            _isScanningCameras = true;
            _hasCameraHardware = false;
            UpdateCameraUiState();

            var items = new List<CameraDeviceItem>();
            try
            {
                var cameraDiscovery = new CameraDiscoveryService();
                var cameras = await cameraDiscovery.RefreshCamerasAsync();

                if (!IsLoaded || _isCancelledOrClosed) return;

                if (cameras != null && cameras.Count > 0)
                {
                    var sortedCameras = cameras
                        .OrderByDescending(c => !c.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    foreach (var cam in sortedCameras)
                    {
                        bool isVirtual = cam.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase);
                        string suffix = isVirtual ? " [Thiết bị ảo]" : "";
                        items.Add(new CameraDeviceItem
                        {
                            DisplayName = $"{cam.FriendlyName}{suffix}",
                            DeviceId = cam.DeviceId
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading cameras: {ex.Message}");
            }

            if (!IsLoaded || _isCancelledOrClosed) return;

            _isSuppressingCameraSelectionChanged = true;
            try
            {
                if (items.Count > 0)
                {
                    _hasCameraHardware = true;
                    cboCamera.ItemsSource = items;
                    cboCamera.SelectedIndex = 0;
                    cboCamera.IsEnabled = true;
                }
                else
                {
                    _hasCameraHardware = false;
                    cboCamera.ItemsSource = null;
                    cboCamera.IsEnabled = false;

                    if (chkCamera != null && chkCamera.IsChecked == true)
                    {
                        chkCamera.IsChecked = false;
                    }
                }
            }
            finally
            {
                _isSuppressingCameraSelectionChanged = false;
            }

            _isScanningCameras = false;
            UpdateCameraUiState();
        }

        private void UpdateMicrophoneUiState()
        {
            if (panelMicOptions == null) return;
            bool isMicOn = chkMicrophone?.IsChecked == true && _hasMicHardware;
            panelMicOptions.IsEnabled = isMicOn;
            panelMicOptions.Opacity = isMicOn ? 1.0 : 0.4;

            if (dotMicStatus != null && txtMicStatus != null)
            {
                if (!_hasMicHardware)
                {
                    dotMicStatus.Fill = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                    txtMicStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                    txtMicStatus.Text = "Không tìm thấy Mic";
                    if (pbAudioMeter != null) pbAudioMeter.Value = 0;
                }
                else if (!isMicOn)
                {
                    dotMicStatus.Fill = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
                    txtMicStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
                    txtMicStatus.Text = "Đã tắt";
                    if (pbAudioMeter != null) pbAudioMeter.Value = 0;
                }
                else
                {
                    dotMicStatus.Fill = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
                    txtMicStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
                    txtMicStatus.Text = "Sẵn sàng";
                }
            }
        }

        private void UpdateCameraUiState()
        {
            if (panelCameraOptions == null || panelCameraDisabledState == null) return;

            bool canUseCamera = !_isScanningCameras && _hasCameraHardware;
            if (chkCamera != null)
            {
                chkCamera.IsEnabled = canUseCamera;
                if (!canUseCamera && chkCamera.IsChecked == true)
                {
                    chkCamera.IsChecked = false;
                }
            }

            if (_isScanningCameras)
            {
                if (txtCameraDisabledTitle != null)
                {
                    txtCameraDisabledTitle.Text = "Đang kiểm tra Camera...";
                    txtCameraDisabledTitle.Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
                }
                if (txtCameraDisabledDesc != null)
                {
                    txtCameraDisabledDesc.Text = "Hệ thống đang dò tìm thiết bị Camera kết nối với máy...";
                }
                if (btnEnableCameraQuick != null)
                {
                    btnEnableCameraQuick.Content = "Đang kiểm tra...";
                    btnEnableCameraQuick.IsEnabled = false;
                    btnEnableCameraQuick.Opacity = 0.5;
                }
            }
            else if (!_hasCameraHardware)
            {
                if (txtCameraDisabledTitle != null)
                {
                    txtCameraDisabledTitle.Text = "Không tìm thấy Camera";
                    txtCameraDisabledTitle.Foreground = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                }
                if (txtCameraDisabledDesc != null)
                {
                    txtCameraDisabledDesc.Text = "Thiết bị hiện tại chưa kết nối Camera. Hệ thống sẽ chỉ ghi hình màn hình bài giảng và âm thanh.";
                }
                if (btnEnableCameraQuick != null)
                {
                    btnEnableCameraQuick.Content = "Không có thiết bị Camera";
                    btnEnableCameraQuick.IsEnabled = false;
                    btnEnableCameraQuick.Opacity = 0.45;
                }
            }
            else
            {
                if (txtCameraDisabledTitle != null)
                {
                    txtCameraDisabledTitle.Text = "Camera đang tắt";
                    txtCameraDisabledTitle.Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1));
                }
                if (txtCameraDisabledDesc != null)
                {
                    txtCameraDisabledDesc.Text = "Chỉ ghi hình nội dung trên màn hình bảng viết. Bật công tắc phía trên nếu muốn hiển thị camera.";
                }
                if (btnEnableCameraQuick != null)
                {
                    btnEnableCameraQuick.Content = "Bật Camera";
                    btnEnableCameraQuick.IsEnabled = true;
                    btnEnableCameraQuick.Opacity = 1.0;
                }
            }

            bool showOptions = canUseCamera && chkCamera?.IsChecked == true;
            panelCameraOptions.Visibility = showOptions ? Visibility.Visible : Visibility.Collapsed;
            panelCameraDisabledState.Visibility = showOptions ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateCameraPreviewShape()
        {
            if (bdrCameraPreview == null) return;
            bool isCircle = rbCircle?.IsChecked == true;
            if (isCircle)
            {
                bdrCameraPreview.Width = 108;
                bdrCameraPreview.Height = 108;
                bdrCameraPreview.CornerRadius = new CornerRadius(54);
            }
            else
            {
                bdrCameraPreview.Width = 192;
                bdrCameraPreview.Height = 108;
                bdrCameraPreview.CornerRadius = new CornerRadius(10);
            }
        }

        private void chkMicrophone_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            if (chkMicrophone?.IsChecked == true)
            {
                StartRealMicMonitor();
            }
            else
            {
                StopRealMicMonitor();
            }
            UpdateMicrophoneUiState();
        }

        private void chkCamera_Changed(object sender, RoutedEventArgs e)
        {
            if (_isScanningCameras || !_hasCameraHardware)
            {
                if (chkCamera != null && chkCamera.IsChecked == true)
                {
                    chkCamera.IsChecked = false;
                }
                UpdateCameraUiState();
                return;
            }

            UpdateCameraUiState();
            bool isChecked = chkCamera?.IsChecked == true;

            if (isChecked)
            {
                _ = StartDialogCameraPreviewAsync();
            }
            else
            {
                _ = StopDialogCameraPreviewAsync();
            }
        }

        private void btnEnableCameraQuick_Click(object sender, RoutedEventArgs e)
        {
            if (_isScanningCameras || !_hasCameraHardware) return;
            if (chkCamera != null)
            {
                chkCamera.IsChecked = true;
            }
        }

        private void CameraShape_Changed(object sender, RoutedEventArgs e)
        {
            UpdateCameraPreviewShape();
        }

        private void cboCamera_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSuppressingCameraSelectionChanged || _isScanningCameras || !_hasCameraHardware) return;
            if (chkCamera?.IsChecked == true)
            {
                _ = StartDialogCameraPreviewAsync();
            }
        }

        private async Task StartDialogCameraPreviewAsync()
        {
            await StopDialogCameraPreviewAsync();

            if (!IsLoaded || _isCancelledOrClosed || _isStartingRecording || !_hasCameraHardware || chkCamera?.IsChecked != true)
            {
                return;
            }

            string? cameraId = (cboCamera?.SelectedItem as CameraDeviceItem)?.DeviceId;
            if (string.IsNullOrWhiteSpace(cameraId))
            {
                return;
            }

            CameraPreviewService? svc = null;
            try
            {
                if (panelCameraPreviewLoading != null)
                    panelCameraPreviewLoading.Visibility = Visibility.Visible;
                if (txtCameraPreviewStatus != null)
                    txtCameraPreviewStatus.Text = "Đang kết nối camera...";

                svc = new CameraPreviewService();
                _dialogCameraService = svc;

                svc.FrameArrived += frame =>
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        if (_dialogCameraService != svc) return;
                        if (panelCameraPreviewLoading != null)
                            panelCameraPreviewLoading.Visibility = Visibility.Collapsed;
                        if (imgDialogCameraPreview != null)
                            imgDialogCameraPreview.Source = frame;
                    });
                };
                svc.ErrorOccurred += err =>
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        if (_dialogCameraService != svc) return;
                        if (panelCameraPreviewLoading != null)
                            panelCameraPreviewLoading.Visibility = Visibility.Visible;
                        if (txtCameraPreviewStatus != null)
                            txtCameraPreviewStatus.Text = $"Không mở được camera: {err}";
                    });
                };

                await svc.StartAsync(new QASmartTouch.Models.Camera.CameraProfile
                {
                    SelectedCameraId = cameraId,
                    Width = 640,
                    Height = 480,
                    FPS = 30
                });

                if (_dialogCameraService != svc || !IsLoaded || _isCancelledOrClosed || _isStartingRecording || chkCamera?.IsChecked != true)
                {
                    if (_dialogCameraService == svc)
                    {
                        _dialogCameraService = null;
                    }
                    await svc.StopAsync();
                    svc.Dispose();
                }
            }
            catch (Exception ex)
            {
                if (_dialogCameraService == svc && txtCameraPreviewStatus != null)
                {
                    txtCameraPreviewStatus.Text = $"Không mở được camera: {ex.Message}";
                }
            }
        }

        private async Task StopDialogCameraPreviewAsync()
        {
            var svc = _dialogCameraService;
            _dialogCameraService = null;

            if (svc != null)
            {
                try
                {
                    await svc.StopAsync();
                    svc.Dispose();
                }
                catch { }
            }
            if (imgDialogCameraPreview != null)
            {
                imgDialogCameraPreview.Source = null;
            }
        }

        private async void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_isStartingRecording || _isCancelledOrClosed) return;
            _isCancelledOrClosed = true;
            StopRealMicMonitor();
            await StopDialogCameraPreviewAsync();
            Close();
        }

        private async void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_isStartingRecording || _isCancelledOrClosed) return;
            _isCancelledOrClosed = true;
            StopRealMicMonitor();
            await StopDialogCameraPreviewAsync();
            Close();
        }

        private async void btnStartRecording_Click(object sender, RoutedEventArgs e)
        {
            if (_isStartingRecording || _isCancelledOrClosed) return;
            _isStartingRecording = true;

            if (btnStartRecording != null) btnStartRecording.IsEnabled = false;
            if (btnCancel != null) btnCancel.IsEnabled = false;
            if (btnClose != null) btnClose.IsEnabled = false;

            if (_isScanningCameras && _cameraScanTask != null)
            {
                try
                {
                    await _cameraScanTask;
                }
                catch { }
            }

            bool enableMic = chkMicrophone.IsChecked == true && _hasMicHardware;
            // Khi có Mic thật thì đồng bộ thu âm hệ thống theo công tắc; khi máy không có Mic vẫn giữ thu âm hệ thống (nếu có loa)
            bool enableSystemAudio = _hasMicHardware ? enableMic : true;

            string? selectedMicId = _hasMicHardware ? (cboMicrophone.SelectedItem as AudioDeviceItem)?.DeviceName : null;
            if (string.IsNullOrWhiteSpace(selectedMicId)) selectedMicId = null;

            string? selectedCameraId = _hasCameraHardware ? (cboCamera.SelectedItem as CameraDeviceItem)?.DeviceId : null;
            bool hasCameraHardware = _hasCameraHardware && !string.IsNullOrWhiteSpace(selectedCameraId);
            bool enableCamera = chkCamera.IsChecked == true && hasCameraHardware;
            int shapeMode = rbCircle.IsChecked == true ? 0 : 2; // 0 = Tròn, 2 = Chữ nhật

            var qualityPreset = rbQuality720p?.IsChecked == true
                ? RecordingQualityPreset.Compact720p
                : RecordingQualityPreset.Standard1080p;
            ScreenRecorderService.Instance.CurrentQualityPreset = qualityPreset;

            // Giải phóng triệt để thiết bị lắng nghe thử Mic và Camera trước khi chuyển giao cho bộ ghi hình chính
            StopRealMicMonitor();
            await StopDialogCameraPreviewAsync();

            if (_isCancelledOrClosed) return;

            Close();

            // Kích hoạt Countdown 3-2-1 trước khi bắt đầu ghi hình
            var countdown = new CountdownOverlay(
                onComplete: () =>
                {
                    // 1. Chỉ mở Camera nổi khi thiết bị có Camera thật và được bật
                    if (enableCamera && !string.IsNullOrWhiteSpace(selectedCameraId))
                    {
                        FloatingCameraWindow.OpenOrActivate(selectedCameraId, shapeMode);
                    }

                    // 2. Mở thanh ghi hình nổi và bắt đầu quay (vẫn truyền thông tin phần cứng Camera để có thể bật/tắt giữa chừng)
                    FloatingRecorderBar.OpenOrActivate(
                        enableMic,
                        enableSystemAudio,
                        selectedMicId,
                        qualityPreset,
                        enableCamera,
                        hasCameraHardware,
                        selectedCameraId,
                        shapeMode);
                },
                onCancel: () =>
                {
                    System.Diagnostics.Debug.WriteLine("Recording cancelled by user during countdown.");
                });

            countdown.Show();
        }
    }
}
