using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Serilog;

namespace QASmartClass.Shared
{
    /// <summary>
    /// [LOI_VID_49] Transition Overlay — Hiển thị toàn màn hình khi đang chuyển mode.
    /// 
    /// Chức năng:
    /// - Hiển thị Loading Spinner xoay liên tục
    /// - Hiển thị tên mode đích (SMART CLASS / SMART TOUCH / DESKTOP) theo QC_4.2_LANGUAGE_BRANDING
    /// - Chặn toàn bộ touch/mouse input (IsHitTestVisible = true)
    /// - Nút "Hủy & Thử lại" xuất hiện sau 10s timeout
    /// - Fade-in 200ms, Fade-out 200ms
    /// </summary>
    public partial class TransitionOverlay : System.Windows.Controls.UserControl
    {
        private readonly DispatcherTimer _spinnerTimer;
        private readonly DispatcherTimer _timeoutTimer;
        private double _spinnerAngle = 0;
        private const double SPINNER_SPEED = 6.0; // degrees per tick
        private const int TIMEOUT_SECONDS = 10;

        /// <summary>
        /// Event phát ra khi người dùng bấm nút "Hủy & Thử lại"
        /// </summary>
        public event EventHandler? CancelRequested;

        public TransitionOverlay()
        {
            InitializeComponent();

            // Spinner animation timer (60fps)
            _spinnerTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60fps
            };
            _spinnerTimer.Tick += (s, e) =>
            {
                _spinnerAngle = (_spinnerAngle + SPINNER_SPEED) % 360;
                SpinnerRotation.Angle = _spinnerAngle;
            };

            // Timeout timer — hiện nút Cancel sau TIMEOUT_SECONDS
            _timeoutTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(TIMEOUT_SECONDS)
            };
            _timeoutTimer.Tick += (s, e) =>
            {
                _timeoutTimer.Stop();
                CancelButton.Visibility = Visibility.Visible;
                StatusText.Text = "⚠️ Chuyển chế độ bị trễ...";
                Log.Warning("TransitionOverlay: Timeout after {Sec}s — Cancel button shown", TIMEOUT_SECONDS);
            };
        }

        /// <summary>
        /// Hiển thị Overlay với Fade-in animation.
        /// </summary>
        /// <param name="targetMode">Mode đích đang chuyển tới</param>
        public void Show(AppMode targetMode)
        {
            // Cập nhật tên mode (giữ nguyên tiếng Anh theo QC_4.2_LANGUAGE_BRANDING)
            ModeNameText.Text = ModeService.GetModeDisplayName(targetMode);
            StatusText.Text = "Đang chuyển sang chế độ...";
            CancelButton.Visibility = Visibility.Collapsed;

            // Hiển thị overlay
            Visibility = Visibility.Visible;
            Opacity = 0;

            // Fade-in animation 200ms
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(OpacityProperty, fadeIn);

            // Bắt đầu spinner
            _spinnerTimer.Start();

            // Bắt đầu timeout timer
            _timeoutTimer.Start();

            Log.Debug("TransitionOverlay SHOWN for mode: {Mode}", targetMode);
        }

        /// <summary>
        /// Ẩn Overlay với Fade-out animation.
        /// </summary>
        public void Hide()
        {
            // Dừng timers
            _spinnerTimer.Stop();
            _timeoutTimer.Stop();

            // Fade-out animation 200ms
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            fadeOut.Completed += (s, e) =>
            {
                Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Collapsed;
            };
            BeginAnimation(OpacityProperty, fadeOut);

            Log.Debug("TransitionOverlay HIDDEN");
        }

        /// <summary>
        /// Xử lý khi bấm nút "Hủy & Thử lại"
        /// </summary>
        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Log.Information("TransitionOverlay: User clicked Cancel & Retry");
            Hide();
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
