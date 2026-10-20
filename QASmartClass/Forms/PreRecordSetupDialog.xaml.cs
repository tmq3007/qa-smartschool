using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ScreenRecorderLib;
using QASmartTouch.Services.Camera;

namespace QASmartTouch.Forms
{
    public partial class PreRecordSetupDialog : Window
    {
        private class AudioDeviceItem
        {
            public string DisplayName { get; set; } = "";
            public string DeviceName { get; set; } = "";
            public override string ToString() => DisplayName;
        }

        private class CameraDeviceItem
        {
            public string DisplayName { get; set; } = "";
            public string DeviceId { get; set; } = "0";
            public override string ToString() => DisplayName;
        }

        private readonly DispatcherTimer _meterTimer;
        private readonly Random _rand = new();
        private CameraPreviewService? _dialogCameraService;

        public PreRecordSetupDialog()
        {
            InitializeComponent();
            QASmartTouch.Helpers.TouchActivationHelper.ApplyToWindow(this);

            _meterTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(120)
            };
            _meterTimer.Tick += MeterTimer_Tick;

            Loaded += PreRecordSetupDialog_Loaded;
            Unloaded += PreRecordSetupDialog_Unloaded;
        }

        private void PreRecordSetupDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (panelMicOptions != null)
                panelMicOptions.Visibility = chkMicrophone?.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (panelCameraOptions != null)
                panelCameraOptions.Visibility = chkCamera?.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            LoadAudioDevices();
            _ = LoadCameraDevicesAsync();
            _meterTimer.Start();
        }

        private void PreRecordSetupDialog_Unloaded(object sender, RoutedEventArgs e)
        {
            _meterTimer.Stop();
            _ = StopDialogCameraPreviewAsync();
        }

        private void MeterTimer_Tick(object? sender, EventArgs e)
        {
            if (chkMicrophone?.IsChecked == true && pbAudioMeter != null)
            {
                // Giả lập mức sóng âm thanh sinh động (40% - 85%) để giáo viên an tâm Mic có tín hiệu
                pbAudioMeter.Value = 35 + _rand.Next(0, 45);
            }
            else if (pbAudioMeter != null)
            {
                pbAudioMeter.Value = 0;
            }
        }

        private void LoadAudioDevices()
        {
            try
            {
                var inputDevices = Recorder.GetSystemAudioCaptureDevices();
                var items = new List<AudioDeviceItem>();

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
                        var tag = d.IsDefaultDevice ? (isTranScreen ? " [Ảo - tranScreen]" : " [Mặc định]") : "";
                        var icon = isTranScreen ? "🔕 " : "🎙️ ";
                        items.Add(new AudioDeviceItem
                        {
                            DisplayName = $"{icon}{d.FriendlyName}{tag}",
                            DeviceName = d.DeviceName
                        });
                    }
                }

                if (items.Count == 0)
                {
                    items.Add(new AudioDeviceItem
                    {
                        DisplayName = "🎙️ Microphone mặc định của hệ thống",
                        DeviceName = ""
                    });
                }

                cboMicrophone.ItemsSource = items;

                // Chọn thiết bị mặc định: Ưu tiên thiết bị thật có cờ [Mặc định], tránh tranScreen
                var defItem = items.FirstOrDefault(i => !i.DisplayName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase) && i.DisplayName.Contains("[Mặc định]"))
                           ?? items.FirstOrDefault(i => !i.DisplayName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                           ?? items.FirstOrDefault();
                cboMicrophone.SelectedItem = defItem;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading mics: {ex.Message}");
            }
        }

        private async Task LoadCameraDevicesAsync()
        {
            try
            {
                var cameraDiscovery = new CameraDiscoveryService();
                var cameras = await cameraDiscovery.RefreshCamerasAsync();
                var items = new List<CameraDeviceItem>();

                if (cameras != null && cameras.Count > 0)
                {
                    foreach (var cam in cameras)
                    {
                        items.Add(new CameraDeviceItem
                        {
                            DisplayName = $"📹 {cam.FriendlyName}",
                            DeviceId = cam.DeviceId
                        });
                    }
                }

                if (items.Count == 0)
                {
                    items.Add(new CameraDeviceItem
                    {
                        DisplayName = "📹 Camera mặc định (Tích hợp / USB)",
                        DeviceId = "0"
                    });
                }

                cboCamera.ItemsSource = items;
                cboCamera.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading cameras: {ex.Message}");
                cboCamera.ItemsSource = new List<CameraDeviceItem>
                {
                    new CameraDeviceItem { DisplayName = "📹 Camera mặc định (Index 0)", DeviceId = "0" }
                };
                cboCamera.SelectedIndex = 0;
            }
        }

        private void chkMicrophone_Changed(object sender, RoutedEventArgs e)
        {
            if (panelMicOptions == null) return;
            panelMicOptions.Visibility = chkMicrophone?.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void chkCamera_Changed(object sender, RoutedEventArgs e)
        {
            if (panelCameraOptions == null) return;
            bool isChecked = chkCamera?.IsChecked == true;
            panelCameraOptions.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;

            if (isChecked)
            {
                _ = StartDialogCameraPreviewAsync();
            }
            else
            {
                _ = StopDialogCameraPreviewAsync();
            }
        }

        private void cboCamera_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (chkCamera?.IsChecked == true)
            {
                _ = StartDialogCameraPreviewAsync();
            }
        }

        private async Task StartDialogCameraPreviewAsync()
        {
            await StopDialogCameraPreviewAsync();

            string cameraId = (cboCamera?.SelectedItem as CameraDeviceItem)?.DeviceId ?? "0";

            try
            {
                if (panelCameraPreviewLoading != null)
                    panelCameraPreviewLoading.Visibility = Visibility.Visible;
                if (txtCameraPreviewStatus != null)
                    txtCameraPreviewStatus.Text = "📷 Đang kết nối camera...";

                _dialogCameraService = new CameraPreviewService();
                _dialogCameraService.FrameArrived += frame =>
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        if (panelCameraPreviewLoading != null)
                            panelCameraPreviewLoading.Visibility = Visibility.Collapsed;
                        if (imgDialogCameraPreview != null)
                            imgDialogCameraPreview.Source = frame;
                    });
                };
                _dialogCameraService.ErrorOccurred += err =>
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        if (panelCameraPreviewLoading != null)
                            panelCameraPreviewLoading.Visibility = Visibility.Visible;
                        if (txtCameraPreviewStatus != null)
                            txtCameraPreviewStatus.Text = $"⚠️ {err}";
                    });
                };

                await _dialogCameraService.StartAsync(new QASmartTouch.Models.Camera.CameraProfile
                {
                    SelectedCameraId = cameraId,
                    Width = 640,
                    Height = 480,
                    FPS = 30
                });
            }
            catch (Exception ex)
            {
                if (txtCameraPreviewStatus != null)
                    txtCameraPreviewStatus.Text = $"⚠️ {ex.Message}";
            }
        }

        private async Task StopDialogCameraPreviewAsync()
        {
            if (_dialogCameraService != null)
            {
                try
                {
                    await _dialogCameraService.StopAsync();
                    _dialogCameraService.Dispose();
                }
                catch { }
                _dialogCameraService = null;
            }
            if (imgDialogCameraPreview != null)
            {
                imgDialogCameraPreview.Source = null;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            _ = StopDialogCameraPreviewAsync();
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            _ = StopDialogCameraPreviewAsync();
            Close();
        }

        private void btnStartRecording_Click(object sender, RoutedEventArgs e)
        {
            _ = StopDialogCameraPreviewAsync();

            bool enableMic = chkMicrophone.IsChecked == true;
            bool enableSystemAudio = chkSystemAudio.IsChecked == true;
            bool enableCamera = chkCamera.IsChecked == true;

            string? selectedMicId = (cboMicrophone.SelectedItem as AudioDeviceItem)?.DeviceName;
            if (string.IsNullOrWhiteSpace(selectedMicId)) selectedMicId = null;
            string selectedCameraId = (cboCamera.SelectedItem as CameraDeviceItem)?.DeviceId ?? "0";
            int shapeMode = rbCircle.IsChecked == true ? 0 : 2; // 0 = Tròn, 2 = Chữ nhật

            Close();

            // Kích hoạt Countdown 3-2-1 trước khi bắt đầu ghi hình
            var countdown = new CountdownOverlay(
                onComplete: () =>
                {
                    // 1. Mở Camera nổi nếu được bật
                    if (enableCamera)
                    {
                        FloatingCameraWindow.OpenOrActivate(selectedCameraId, shapeMode);
                    }

                    // 2. Mở thanh ghi hình nổi và bắt đầu quay
                    FloatingRecorderBar.OpenOrActivate(enableMic, enableSystemAudio, selectedMicId);
                },
                onCancel: () =>
                {
                    System.Diagnostics.Debug.WriteLine("Recording cancelled by user during countdown.");
                });

            countdown.Show();
        }
    }
}