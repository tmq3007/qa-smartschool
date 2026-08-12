using Xunit;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.Classroom.Views;
using System.Reflection;

namespace QASmartClass.Tests
{
    public class BroadcastPageTests
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

        private void EnsureApplication()
        {
            if (System.Windows.Application.Current == null)
            {
                try
                {
                    new QASmartTouch.App();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BroadcastPageTests] App creation error: {ex.Message}");
                }
            }
        }

        [Fact]
        public void BroadcastPage_Constructor_InitializesUIAndFields()
        {
            RunOnStaThread(() =>
            {
                EnsureApplication();
                var page = new BroadcastPage();
                Assert.NotNull(page);
                
                // Verify initial values
                var isBroadcastingField = typeof(BroadcastPage).GetField("_isBroadcasting", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isBroadcastingField);
                Assert.False((bool)isBroadcastingField.GetValue(page)!);

                var isPausedField = typeof(BroadcastPage).GetField("_isPaused", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isPausedField);
                Assert.False((bool)isPausedField.GetValue(page)!);
            });
        }

        [Fact]
        public void BroadcastPage_PauseClick_TogglesStateAndColors()
        {
            RunOnStaThread(() =>
            {
                EnsureApplication();
                var page = new BroadcastPage();

                // Get private field _isPaused
                var isPausedField = typeof(BroadcastPage).GetField("_isPaused", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isPausedField);

                // Pause button
                var btnPause = page.FindName("btnPause") as Button;
                Assert.NotNull(btnPause);

                // Call Pause_Click via reflection
                var pauseClickMethod = typeof(BroadcastPage).GetMethod("Pause_Click", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(pauseClickMethod);

                // Initially false
                Assert.False((bool)isPausedField.GetValue(page)!);

                // First click: pause
                pauseClickMethod.Invoke(page, new object[] { btnPause, new RoutedEventArgs() });
                Assert.True((bool)isPausedField.GetValue(page)!);
                Assert.Equal("▶️ Tiếp tục", btnPause.Content);

                // Verify background and foreground colors for paused state
                var pausedBg = btnPause.Background as SolidColorBrush;
                var pausedFg = btnPause.Foreground as SolidColorBrush;
                Assert.NotNull(pausedBg);
                Assert.NotNull(pausedFg);
                Assert.Equal(Color.FromRgb(232, 245, 233), pausedBg.Color); // #E8F5E9
                Assert.Equal(Color.FromRgb(46, 125, 50), pausedFg.Color);   // #2E7D32

                // Second click: resume
                pauseClickMethod.Invoke(page, new object[] { btnPause, new RoutedEventArgs() });
                Assert.False((bool)isPausedField.GetValue(page)!);
                Assert.Equal("⏸️ Tạm dừng", btnPause.Content);

                // Verify background and foreground colors for running state
                var runningBg = btnPause.Background as SolidColorBrush;
                var runningFg = btnPause.Foreground as SolidColorBrush;
                Assert.NotNull(runningBg);
                Assert.NotNull(runningFg);
                Assert.Equal(Color.FromRgb(255, 243, 224), runningBg.Color); // #FFF3E0
                Assert.Equal(Color.FromRgb(230, 81, 0), runningFg.Color);   // #E65100
            });
        }

        [Fact]
        public void BroadcastPage_Unloaded_StopsTimersAndCleanup()
        {
            RunOnStaThread(() =>
            {
                EnsureApplication();
                var page = new BroadcastPage();

                // Setup active state
                var isBroadcastingField = typeof(BroadcastPage).GetField("_isBroadcasting", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isBroadcastingField);
                isBroadcastingField.SetValue(page, true);

                var isPausedField = typeof(BroadcastPage).GetField("_isPaused", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isPausedField);
                isPausedField.SetValue(page, true);

                // Trigger Unloaded event
                page.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

                // Verify state is reset
                Assert.False((bool)isBroadcastingField.GetValue(page)!);
                Assert.False((bool)isPausedField.GetValue(page)!);
            });
        }
    }
}
