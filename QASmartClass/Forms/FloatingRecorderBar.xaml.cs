using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using QASmartTouch.Services;

namespace QASmartTouch.Forms
{
    /// <summary>
    /// Thanh điều khiển nổi ghi hình bài giảng (Screen Recording Bar)
    /// Hỗ trợ tương tác cảm ứng trực tiếp trên màn hình lớp học và chuột máy tính
    /// </summary>
    public partial class FloatingRecorderBar : Window
    {
        private readonly ScreenRecorderService _recorderService;
        private readonly DispatcherTimer _durationTimer;
        private bool _isClosing = false;
        private bool _suppressCompletionPrompt = false;
        private bool _hasCameraHardware = false;
        private string? _sessionCameraId = null;
        private int _sessionCameraShapeMode = 0;
        private bool _cameraHardwareDisconnected = false;
        private FloatingCameraWindow? _trackedCameraWindow = null;

        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        private const string PlayPathData = "M8,5.14V19.14L19,12.14L8,5.14Z";
        private const string PausePathData = "M14,19H18V5H14M6,19H10V5H6V19Z";

        private const string MicOnPathData = "M12,2A3,3 0 0,1 15,5V11A3,3 0 0,1 12,14A3,3 0 0,1 9,11V5A3,3 0 0,1 12,2M19,11C19,14.53 16.39,17.44 13,17.93V21H11V17.93C7.61,17.44 5,14.53 5,11H7A5,5 0 0,0 12,16A5,5 0 0,0 17,11H19Z";
        private const string MicMutePathData = "M19,11C19,12.19 18.66,13.3 18.1,14.28L16.87,13.05C17.14,12.43 17.3,11.74 17.3,11H19.3M12,5A3,3 0 0,1 15,8V9.18L10.82,5C11.18,5 11.58,5 12,5M3.27,2L2,3.27L8.73,10H8.7V11C8.7,12.83 10.17,14.3 12,14.3C12.43,14.3 12.83,14.22 13.2,14.07L14.73,15.6C13.91,16.27 12.88,16.7 11.75,16.77V20H9.75V22H14.25V20H12.25V16.77C15.64,16.28 18.25,13.37 18.25,9.84H16.25A5.25,5.25 0 0,1 11,15.09L3.27,2Z";

        private const string CamOnPathData = "M17,10.5V7A1,1 0 0,0 16,6H4A1,1 0 0,0 3,7V17A1,1 0 0,0 4,18H16A1,1 0 0,0 17,17V13.5L21,17.5V6.5L17,10.5Z";
        private const string CamOffPathData = "M1.2,4.27L2.47,5.54C2.18,5.92 2,6.39 2,6.9V17.1A2.1,2.1 0 0,0 4.1,19.2H16.13L19.73,22.8L21,21.53L1.2,4.27M18,10.5L22,6.5V17.5L18,13.5V17.1A2.1,2.1 0 0,1 15.9,19.2L14.1,17.4L18,13.5V10.5M5.8,5.1L4.1,3.4L5.5,2H15.9A2.1,2.1 0 0,1 18,4.1V9.9L16.2,8.1V4.1A0.3,0.3 0 0,0 15.9,3.8H7.5L5.8,5.1Z";

        public FloatingRecorderBar()
        {
            InitializeComponent();
            QASmartTouch.Helpers.TouchActivationHelper.ApplyToWindow(this);

            _recorderService = ScreenRecorderService.Instance;

            // Đặt vị trí mặc định ở góc trên bên phải màn hình
            this.Loaded += (s, e) =>
            {
                var workArea = SystemParameters.WorkArea;
                this.Left = workArea.Right - this.ActualWidth - 40;
                this.Top = workArea.Top + 25;
            };

            // Timer đếm giây
            _durationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _durationTimer.Tick += DurationTimer_Tick;

            // Đăng ký nhận sự kiện lưu hoàn tất
            _recorderService.SaveCompleted += OnSaveCompleted;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    // Ẩn thanh điều khiển khỏi luồng quay video (WDA_EXCLUDEFROMCAPTURE):
                    // 1. Video bài giảng xuất ra sạch đẹp, không bị dính thanh công cụ của giáo viên.
                    // 2. Bộ đếm giây (00:00:01) và đèn REC nhấp nháy không làm biến đổi pixel khung hình,
                    //    giúp màn hình tĩnh 100% khi giáo viên dừng viết, giảm mạnh dung lượng file.
                    SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SetWindowDisplayAffinity warning: {ex.Message}");
            }
        }

        public static void OpenOrActivate(
            bool enableMic = true,
            bool enableSystemAudio = true,
            string? selectedMicId = null,
            RecordingQualityPreset qualityPreset = RecordingQualityPreset.Standard1080p,
            bool hasCamera = false,
            bool hasCameraHardware = false,
            string? selectedCameraId = null,
            int cameraShapeMode = 0)
        {
            var existing = System.Linq.Enumerable.FirstOrDefault(Application.Current.Windows.OfType<FloatingRecorderBar>());
            if (existing != null)
            {
                existing.Show();
                existing.Activate();
                return;
            }

            var recorderBar = new FloatingRecorderBar();
            recorderBar.StartNewRecording(
                enableMic,
                enableSystemAudio,
                selectedMicId,
                qualityPreset,
                hasCamera,
                hasCameraHardware,
                selectedCameraId,
                cameraShapeMode);
        }

        public void StartNewRecording(
            bool enableMic = true,
            bool enableSystemAudio = true,
            string? selectedMicId = null,
            RecordingQualityPreset qualityPreset = RecordingQualityPreset.Standard1080p,
            bool hasCamera = false,
            bool hasCameraHardware = false,
            string? selectedCameraId = null,
            int cameraShapeMode = 0)
        {
            try
            {
                _hasCameraHardware = hasCameraHardware || hasCamera || !string.IsNullOrWhiteSpace(selectedCameraId);
                _sessionCameraId = !string.IsNullOrWhiteSpace(selectedCameraId) ? selectedCameraId : "0";
                _sessionCameraShapeMode = cameraShapeMode;
                _cameraHardwareDisconnected = false;

                AttachSessionCamera();
                _recorderService.StartRecording(
                    enableMic,
                    enableSystemAudio,
                    specifiedMicId: selectedMicId,
                    customPath: null,
                    qualityPreset: qualityPreset);
                UpdateMicButtonVisual();
                UpdateCamButtonVisual();
                _durationTimer.Start();
                this.Show();
            }
            catch (Exception ex)
            {
                DetachSessionCamera();
                MessageBox.Show($"Không thể bắt đầu ghi hình bài giảng:\n{ex.Message}", 
                    "Lỗi Ghi Hình", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void AttachSessionCamera()
        {
            DetachSessionCamera();
            var cam = FloatingCameraWindow.CurrentInstance;
            if (cam != null)
            {
                _trackedCameraWindow = cam;
                _trackedCameraWindow.CameraClosed += OnFloatingCameraClosed;
            }
        }

        private void DetachSessionCamera()
        {
            if (_trackedCameraWindow != null)
            {
                _trackedCameraWindow.CameraClosed -= OnFloatingCameraClosed;
                _trackedCameraWindow = null;
            }
        }

        private void OnFloatingCameraClosed()
        {
            var closedWindow = _trackedCameraWindow;
            bool wasHardwareError = closedWindow?.ClosedByHardwareError == true;
            if (closedWindow != null)
            {
                _sessionCameraShapeMode = closedWindow.CurrentShapeMode;
            }

            Dispatcher.InvokeAsync(() =>
            {
                DetachSessionCamera();
                _cameraHardwareDisconnected = wasHardwareError;
                UpdateCamButtonVisual();
            });
        }

        private void UpdateMicButtonVisual()
        {
            if (!_recorderService.HasAudioSources)
            {
                btnToggleMic.IsEnabled = false;
                btnToggleMic.Opacity = 0.45;
                iconMic.Data = Geometry.Parse(MicMutePathData);
                iconMic.Fill = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
                btnToggleMic.ToolTip = "Không tìm thấy thiết bị âm thanh";
                return;
            }

            btnToggleMic.IsEnabled = true;
            btnToggleMic.Opacity = 1.0;
            bool isMuted = _recorderService.IsMicrophoneMuted;
            iconMic.Data = Geometry.Parse(isMuted ? MicMutePathData : MicOnPathData);
            iconMic.Fill = isMuted ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)) : new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
            btnToggleMic.ToolTip = isMuted ? "Bật thu âm" : "Tắt thu âm";
        }

        private void UpdateCamButtonVisual()
        {
            if (!_hasCameraHardware)
            {
                btnToggleCam.IsEnabled = false;
                btnToggleCam.Opacity = 0.45;
                iconCam.Data = Geometry.Parse(CamOffPathData);
                iconCam.Fill = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
                btnToggleCam.ToolTip = "Không tìm thấy thiết bị Camera";
                return;
            }

            btnToggleCam.IsEnabled = true;
            btnToggleCam.Opacity = 1.0;

            var cam = FloatingCameraWindow.CurrentInstance;
            bool isVisible = cam != null && cam.Visibility == Visibility.Visible;

            if (isVisible)
            {
                iconCam.Data = Geometry.Parse(CamOnPathData);
                iconCam.Fill = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
                btnToggleCam.ToolTip = "Ẩn Camera";
            }
            else if (_cameraHardwareDisconnected)
            {
                iconCam.Data = Geometry.Parse(CamOffPathData);
                iconCam.Fill = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                btnToggleCam.ToolTip = "Camera mất kết nối (Nhấn để thử bật lại)";
            }
            else
            {
                iconCam.Data = Geometry.Parse(CamOffPathData);
                iconCam.Fill = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
                btnToggleCam.ToolTip = "Hiện Camera";
            }
        }

        private void btnToggleMic_Click(object sender, RoutedEventArgs e)
        {
            if (!_recorderService.HasAudioSources) return;
            _recorderService.ToggleMicrophoneMute();
            UpdateMicButtonVisual();
        }

        private void btnToggleCam_Click(object sender, RoutedEventArgs e)
        {
            if (!_hasCameraHardware)
            {
                UpdateCamButtonVisual();
                return;
            }

            var cam = FloatingCameraWindow.CurrentInstance;
            if (cam == null)
            {
                _cameraHardwareDisconnected = false;
                FloatingCameraWindow.OpenOrActivate(_sessionCameraId ?? "0", _sessionCameraShapeMode);
                AttachSessionCamera();
                UpdateCamButtonVisual();
                return;
            }

            _sessionCameraShapeMode = cam.CurrentShapeMode;
            FloatingCameraWindow.ToggleVisibility();
            UpdateCamButtonVisual();
        }

        private void DurationTimer_Tick(object? sender, EventArgs e)
        {
            if (_recorderService.IsRecording && !_recorderService.IsPaused)
            {
                var duration = _recorderService.RecordingDuration;
                txtDuration.Text = $"{duration:hh\\:mm\\:ss}";

                // Nhấp nháy đèn REC nếu không bị tạm dừng
                recDot.Opacity = recDot.Opacity > 0.5 ? 0.2 : 1.0;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void btnPauseResume_Click(object sender, RoutedEventArgs e)
        {
            if (_recorderService.IsPaused)
            {
                _recorderService.ResumeRecording();
                iconPauseResume.Data = Geometry.Parse(PausePathData);
                txtRecLabel.Text = "REC";
                txtRecLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                recDot.Fill = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                recDot.Opacity = 1.0;
            }
            else
            {
                _recorderService.PauseRecording();
                iconPauseResume.Data = Geometry.Parse(PlayPathData);
                txtRecLabel.Text = "PAUSE";
                txtRecLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
                recDot.Fill = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
                recDot.Opacity = 1.0;
            }
        }

        public void PrepareForApplicationExit(bool discardRecording)
        {
            _suppressCompletionPrompt = true;
            _durationTimer.Stop();
            txtDuration.Text = discardRecording ? "Đang hủy..." : "Đang lưu...";
            btnStop.IsEnabled = false;
            btnPauseResume.IsEnabled = false;
            btnToggleMic.IsEnabled = false;
            btnToggleCam.IsEnabled = false;
            btnOpenFolder.IsEnabled = false;
            btnCancel.IsEnabled = false;
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            txtDuration.Text = "Đang lưu...";
            btnStop.IsEnabled = false;
            btnPauseResume.IsEnabled = false;
            btnCancel.IsEnabled = false;
            _durationTimer.Stop();

            _recorderService.StopRecording();
        }

        private void OnSaveCompleted(string? filePath, string? error)
        {
            if (_isClosing) return;
            _isClosing = true;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    this.Close();
                }
                catch { }

                if (_suppressCompletionPrompt)
                {
                    return;
                }

                Window? owner = Application.Current?.MainWindow;
                if (owner != null && (!owner.IsLoaded || !owner.IsVisible))
                {
                    owner = null;
                }

                if (!string.IsNullOrEmpty(error))
                {
                    if (owner != null)
                    {
                        MessageBox.Show(owner, $"Quá trình lưu video bài giảng gặp lỗi:\n{error}",
                            "Lỗi Lưu Video", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show($"Quá trình lưu video bài giảng gặp lỗi:\n{error}",
                            "Lỗi Lưu Video", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    return;
                }

                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    var result = owner != null
                        ? MessageBox.Show(owner,
                            $"Đã ghi hình bài giảng thành công!\n\nVideo được lưu tại:\n{filePath}\n\nBạn có muốn mở video ngay bây giờ không?",
                            "Ghi Hình Hoàn Tất", MessageBoxButton.YesNoCancel, MessageBoxImage.Information)
                        : MessageBox.Show(
                            $"Đã ghi hình bài giảng thành công!\n\nVideo được lưu tại:\n{filePath}\n\nBạn có muốn mở video ngay bây giờ không?",
                            "Ghi Hình Hoàn Tất", MessageBoxButton.YesNoCancel, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Không thể mở video: {ex.Message}");
                        }
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        try
                        {
                            string? dir = Path.GetDirectoryName(filePath);
                            if (!string.IsNullOrEmpty(dir))
                            {
                                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                            }
                        }
                        catch { }
                    }
                }
            }));
        }

        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            _recorderService.OpenRecordingsFolder();
        }

        private async void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                this,
                "Bạn có muốn dừng và hủy phiên ghi hình bài giảng hiện tại không?\n(Video đang quay dở sẽ không được lưu)",
                "Hủy Ghi Hình", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                PrepareForApplicationExit(discardRecording: true);
                await _recorderService.StopAndFinalizeAsync(discardFile: true);
                try
                {
                    this.Close();
                }
                catch { }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _durationTimer.Stop();
            _recorderService.SaveCompleted -= OnSaveCompleted;
            DetachSessionCamera();
            try
            {
                FloatingCameraWindow.CurrentInstance?.Close();
            }
            catch { }
            base.OnClosed(e);
        }
    }
}
