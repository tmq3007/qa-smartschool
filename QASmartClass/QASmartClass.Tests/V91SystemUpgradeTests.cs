using System;
using System.IO;
using System.Threading;
using System.Windows;
using QASmartClass.Services;
using QASmartClass.LearningTools.Views.Math;
using Xunit;

namespace QASmartClass.Tests
{
    public class V91SystemUpgradeTests
    {
        private void RunOnStaThread(Action action)
        {
            Exception? ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (ex != null)
            {
                throw ex;
            }
        }

        [Fact]
        public void TestDataDirOverride_SetsAppPaths()
        {
            string? originalOverride = AppPaths.DataDirOverride;
            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), "QASmartClassTest_" + Guid.NewGuid().ToString());
                AppPaths.DataDirOverride = tempPath;

                Assert.Equal(tempPath, AppPaths.RootDir);
                Assert.Equal(Path.Combine(tempPath, "Documents"), AppPaths.DocumentsDir);
                Assert.Equal(Path.Combine(tempPath, "Settings"), AppPaths.SettingsDir);
                Assert.Equal(Path.Combine(tempPath, "Logs"), AppPaths.LogsDir);
            }
            finally
            {
                AppPaths.DataDirOverride = originalOverride;
            }
        }

        [Fact]
        public void TestWebView2FallbackUI_TogglesVisibilityOnInitializationError()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try
                    {
                        var app = new System.Windows.Application();
                    }
                    catch { }
                }

                if (System.Windows.Application.Current != null)
                {
                    if (!System.Windows.Application.Current.Resources.Contains("BrandPrimary"))
                    {
                        System.Windows.Application.Current.Resources["BrandPrimary"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
                    }
                }

                // Khởi tạo cửa sổ đồ thị
                var window = new GraphWindow(1.0, 2.0, 3.0);
                Assert.NotNull(window);

                var webViewControl = window.FindName("webView") as UIElement;
                var fallbackGridControl = window.FindName("fallbackGrid") as UIElement;

                Assert.NotNull(webViewControl);
                Assert.NotNull(fallbackGridControl);

                // Giả lập sự kiện Loaded của WPF để kích hoạt LoadHtml
                var loadedRoutedEventArgs = new RoutedEventArgs(FrameworkElement.LoadedEvent);
                window.RaiseEvent(loadedRoutedEventArgs);

                // Chạy Dispatcher frame để xử lý tác vụ bất đồng bộ trong Loaded
                var frame = new System.Windows.Threading.DispatcherFrame();
                System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() => frame.Continue = false));
                System.Windows.Threading.Dispatcher.PushFrame(frame);

                // Kiểm tra logic ẩn/hiện nếu xảy ra lỗi WebView2
                if (webViewControl.Visibility == Visibility.Collapsed)
                {
                    Assert.Equal(Visibility.Visible, fallbackGridControl.Visibility);
                }
            });
        }
    }
}
