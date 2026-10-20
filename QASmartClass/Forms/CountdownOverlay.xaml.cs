using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace QASmartTouch.Forms
{
    public partial class CountdownOverlay : Window
    {
        private readonly DispatcherTimer _timer;
        private int _currentCount = 3;
        private readonly Action _onComplete;
        private readonly Action? _onCancel;
        private bool _isFinished = false;

        public CountdownOverlay(Action onComplete, Action? onCancel = null)
        {
            InitializeComponent();
            _onComplete = onComplete ?? throw new ArgumentNullException(nameof(onComplete));
            _onCancel = onCancel;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;

            Loaded += CountdownOverlay_Loaded;
            KeyDown += CountdownOverlay_KeyDown;
            MouseDown += CountdownOverlay_MouseDown;
            TouchDown += CountdownOverlay_TouchDown;
        }

        private void CountdownOverlay_Loaded(object sender, RoutedEventArgs e)
        {
            AnimateNumber();
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _currentCount--;
            if (_currentCount > 0)
            {
                txtCount.Text = _currentCount.ToString();
                AnimateNumber();
            }
            else
            {
                _timer.Stop();
                FinishAndTrigger();
            }
        }

        private void AnimateNumber()
        {
            // Scale animation: phóng to nhẹ rồi thu về 1.0
            var scaleAnim = new DoubleAnimation(1.3, 1.0, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            badgeScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleAnim);
            badgeScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleAnim);
        }

        private void CountdownOverlay_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Cancel();
            }
        }

        private void CountdownOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Cho phép hủy nếu chạm ra ngoài hoặc bấm ESC
            // Nhưng để tránh bấm nhầm khi chuẩn bị, chỉ hủy nếu bấm phím ESC
        }

        private void CountdownOverlay_TouchDown(object? sender, TouchEventArgs e)
        {
        }

        private void Cancel()
        {
            if (_isFinished) return;
            _isFinished = true;
            _timer.Stop();
            Close();
            _onCancel?.Invoke();
        }

        private void FinishAndTrigger()
        {
            if (_isFinished) return;
            _isFinished = true;
            Close();
            _onComplete?.Invoke();
        }
    }
}
