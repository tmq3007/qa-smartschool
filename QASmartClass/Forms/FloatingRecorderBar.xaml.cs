using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
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

        private const string PlayPathData = "M8,5.14V19.14L19,12.14L8,5.14Z";
        private const string PausePathData = "M14,19H18V5H14M6,19H10V5H6V19Z";

        private const string MicOnPathData = "M12,2A3,3 0 0,1 15,5V11A3,3 0 0,1 12,14A3,3 0 0,1 9,11V5A3,3 0 0,1 12,2M19,11C19,14.53 16.39,17.44 13,17.93V21H11V17.93C7.61,17.44 5,14.53 5,11H7A5,5 0 0,0 12,16A5,5 0 0,0 17,11H19Z";
        private const string MicMutePathData = "M19,11C19,12.19 18.66,13.3 18.1,14.28L16.87,13.05C17.14,12.43 17.3,11.74 17.3,11H19.3M12,5A3,3 0 0,1 15,8V9.18L10.82,5C11.18,5 11.58,5 12,5M3.27,2L2,3.27L8.73,10H8.7V11C8.7,12.83 10.17,14.3 12,14.3C12.43,14.3 12.83,14.22 13.2,14.07L14.73,15.6C13.91,16.27 12.88,16.7 11.75,16.77V20H9.75V22H14.25V20H12.25V16.77C15.64,16.28 18.25,13.37 18.25,9.84H16.25A5.25,5.25 0 0,1 11,15.09L3.27,2Z";

        private const string CamOnPathData = "M17,10.5V7A1,1 0 0,0 16,6H4A1,1 0 0,0 3,7V17A1,1 0 0,0 4,18H16A1,1 0 0,0 17,17V13.5L21,17.5V6.5L17,10.5Z";
        private const string CamOffPathData = "M1.2,4.27L2.47,5.54C2.18,5.92 2,6.39 2,6.9V17.1A2.1,2.1 0 0,0 4.1,19.2H16.13L19.73,22.8L21,21.53L1.2,4.27M18,10.5L22,6.5V17.5L18,13.5V17.1A2.1,2.1 0 0,1 15.9,19.2L14.1,17.4L18,13.5V10.5M5.8,5.1L4.1,3.4L5.5,2H15.9A2.1,2.1 0 0,1 18,4.1V9.9L16.2,8.1V4.1A0.3,0.3 0 0,0 15.9,3.8H7.5L5.8,5.1Z";

        public FloatingRecorderBar()
        {
            InitializeComponent();

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

        public static void OpenOrActivate(bool enableMic = true, bool enableSystemAudio = true, string? selectedMicId = null)
        {
            var existing = System.Linq.Enumerable.FirstOrDefault(Application.Current.Windows.OfType<FloatingRecorderBar>());
            if (existing != null)
            {
                existing.Show();
                existing.Activate();
                return;
            }

            var recorderBar = new FloatingRecorderBar();
            recorderBar.StartNewRecording(enableMic, enableSystemAudio, selectedMicId);
        }

        public void StartNewRecording(bool enableMic = true, bool enableSystemAudio = true, string? selectedMicId = null)
        {
            try
            {
                _recorderService.StartRecording(enableMic, enableSystemAudio, specifiedMicId: selectedMicId, customPath: null);
                _durationTimer.Start();
                this.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể bắt đầu ghi hình bài giảng:\n{ex.Message}", 
                    "Lỗi Ghi Hình", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void btnToggleMic_Click(object sender, RoutedEventArgs e)
        {
            _recorderService.ToggleMicrophoneMute();
            bool isMuted = _recorderService.IsMicrophoneMuted;
            iconMic.Data = Geometry.Parse(isMuted ? MicMutePathData : MicOnPathData);
            iconMic.Fill = isMuted ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)) : new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
            btnToggleMic.ToolTip = isMuted ? "Bật Microphone" : "Tắt Microphone";
        }

        private void btnToggleCam_Click(object sender, RoutedEventArgs e)
        {
            FloatingCameraWindow.ToggleVisibility();
            var cam = FloatingCameraWindow.CurrentInstance;
            bool isVisible = cam != null && cam.Visibility == Visibility.Visible;
            iconCam.Data = Geometry.Parse(isVisible ? CamOnPathData : CamOffPathData);
            iconCam.Fill = isVisible ? new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC)) : new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
            btnToggleCam.ToolTip = isVisible ? "Ẩn Camera" : "Hiện Camera";
        }

        private void DurationTimer_Tick(object? sender, EventArgs e)
        {
            if (_recorderService.IsRecording)
            {
                var duration = _recorderService.RecordingDuration;
                txtDuration.Text = $"{duration:hh\\:mm\\:ss}";

                // Nhấp nháy đèn REC nếu không bị tạm dừng
                if (!_recorderService.IsPaused)
                {
                    recDot.Opacity = recDot.Opacity > 0.5 ? 0.2 : 1.0;
                }
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

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            txtDuration.Text = "Đang lưu...";
            btnStop.IsEnabled = false;
            btnPauseResume.IsEnabled = false;
            _durationTimer.Stop();

            _recorderService.StopRecording();
        }

        private void OnSaveCompleted(string? filePath, string? error)
        {
            if (_isClosing) return;
            _isClosing = true;

            Dispatcher.Invoke(() =>
            {
                this.Close();

                if (!string.IsNullOrEmpty(error))
                {
                    MessageBox.Show($"Quá trình lưu video bài giảng gặp lỗi:\n{error}",
                        "Lỗi Lưu Video", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    var result = MessageBox.Show(
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
            });
        }

        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            _recorderService.OpenRecordingsFolder();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có muốn dừng và hủy phiên ghi hình bài giảng hiện tại không?",
                "Hủy Ghi Hình", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _durationTimer.Stop();
                _recorderService.StopRecording();
                this.Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _durationTimer.Stop();
            _recorderService.SaveCompleted -= OnSaveCompleted;
            try
            {
                FloatingCameraWindow.CurrentInstance?.Close();
            }
            catch { }
            base.OnClosed(e);
        }
    }
}
