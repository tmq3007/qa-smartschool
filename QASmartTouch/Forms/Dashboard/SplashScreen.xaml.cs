using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace QASmartTouch.Forms
{
    /// <summary>
    /// Splash Screen — Màn hình khởi động QA SmartTouch
    /// Hiển thị 2-3 giây trong khi app khởi tạo database, settings, services
    /// </summary>
    public partial class SplashScreen : Window
    {
        private double _maxWidth;

        public SplashScreen()
        {
            InitializeComponent();

            // Hiển thị version từ Assembly
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                int major = version?.Major ?? 1;
                int minor = version?.Minor ?? 0;
                int build = version?.Build ?? 0;
                tbVersion.Text = build > 0 ? $"v{major}.{minor}.{build}" : $"v{major}.{minor}";
            }
            catch
            {
                tbVersion.Text = "v1.0";
            }

            Loaded += (s, e) =>
            {
                // Lấy max width của progress bar container
                var parent = progressFill.Parent as FrameworkElement;
                _maxWidth = parent?.ActualWidth ?? 480;
            };
        }

        /// <summary>
        /// Cập nhật trạng thái loading (gọi từ App.xaml.cs)
        /// </summary>
        public void UpdateStatus(string status, int progress)
        {
            Dispatcher.Invoke(() =>
            {
                tbStatus.Text = status;

                // Animate progress fill width
                double targetWidth = (_maxWidth > 0 ? _maxWidth : 480) * (progress / 100.0);
                var animation = new DoubleAnimation(targetWidth, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                progressFill.BeginAnimation(WidthProperty, animation);
            });
        }

        /// <summary>
        /// Fade out và đóng splash screen
        /// </summary>
        public async Task FadeOutAndClose()
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };

                var tcs = new TaskCompletionSource<bool>();
                fadeOut.Completed += (s, e) => tcs.SetResult(true);
                BeginAnimation(OpacityProperty, fadeOut);
                await tcs.Task;

                Close();
            });
        }
    }
}
