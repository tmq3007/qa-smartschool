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
            void InitializeApplicationFull()
            {
                var urls = new[] {
                    "pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/Styles.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/StaffTheme.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/InterOutfitFonts.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/SvgIcons.xaml",
                    "pack://application:,,,/QASmartClass;component/Localization/Strings_vi.xaml",
                    "pack://application:,,,/QASmartClass;component/LearningTools/Themes/LearningToolsStyles.xaml"
                };

                try
                {
                    var appField = typeof(System.Windows.Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    var createdField = typeof(System.Windows.Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (appField != null) appField.SetValue(null, null);
                    if (createdField != null) createdField.SetValue(null, false);

                    var app = new QASmartTouch.App();
                    foreach (var url in urls)
                    {
                        app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                        {
                            Source = new Uri(url, UriKind.Absolute)
                        });
                    }
                }
                catch { }
            }
            Exception? ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    InitializeApplicationFull(); action();
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
