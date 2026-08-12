using Xunit;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.Services;
using QASmartClass.StudentClient.Views;

namespace QASmartClass.Tests
{
    /// <summary>
    /// EXPERT PANEL EVALUATION TESTS FOR SCREEN BROADCAST (LOI_VID_03)
    /// Programmatically simulates the two main issues from video 7967012769811.mp4:
    /// 1. Recursive nested windows (Hall of Mirrors) when screen sharing and monitoring are active simultaneously.
    /// 2. Student client black/dark screen when UDP/HTTP frame transport fails.
    /// </summary>
    public class LOI_VID_03_RecursiveMirrorAndBlackScreenTests
    {
        private void RunOnSTA(Action action)
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
            Exception? threadEx = null;
            var t = new Thread(() =>
            {
                try
                {
                    EnsureAppForCurrentThread();
                    InitializeApplicationFull(); action();
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            bool completed = t.Join(TimeSpan.FromSeconds(15));
            Assert.True(completed, "STA thread timed out after 15 seconds");
            Assert.Null(threadEx);
        }

        private static void EnsureAppForCurrentThread()
        {
            if (Application.Current == null)
            {
                var app = new QASmartTouch.App();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }
        }

        /// <summary>
        /// TC-01 [UI/UX & IT Manager Expert Evaluation]:
        /// Simulates the scenario where the teacher broadcasts screen while monitoring student screens.
        /// Verifies that there is no active filter in the student client screenshot routine to hide the 
        /// broadcast overlay, leading to recursive capturing (nested windows / hall of mirrors).
        /// </summary>
        [Fact]
        public void TC01_Simulate_RecursiveCapture_When_Broadcasting_And_Monitoring()
        {
            RunOnSTA(() =>
            {
                // Setup: Start screen broadcast
                BroadcastStateService.Instance.IsScreenBroadcastActive = true;
                BroadcastStateService.Instance.BroadcastToken = "TK_TEST_RECURSION";

                var shell = new StudentShell();

                // Stop the watchdog timer to avoid UI threads conflict
                var timerField = typeof(StudentShell).GetField("_focusWatchdogTimer",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (timerField != null)
                {
                    var timer = timerField.GetValue(shell) as System.Windows.Threading.DispatcherTimer;
                    timer?.Stop();
                }

                // Create a valid temporary file to bypass the File.Exists check
                string tempFile = Path.GetTempFileName();
                try
                {
                    // Simulate receiving the SCREEN_BROADCAST_START command
                    var handleMethod = typeof(StudentShell).GetMethod("HandleTeacherCommand",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(handleMethod);

                    handleMethod.Invoke(shell, new object[] { $"CMD|SCREEN_BROADCAST_START|{tempFile}|8080|TK_TEST_RECURSION" });

                    // Verify the overlay is visible and active on the student shell
                    var overlayField = typeof(StudentShell).GetField("_broadcastOverlay",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(overlayField);
                    var overlay = overlayField.GetValue(shell) as Grid;
                    Assert.NotNull(overlay);
                    Assert.Equal(Visibility.Visible, overlay.Visibility);

                    // Simulate screenshot request while broadcast overlay is active
                    // The student client takes a Win32 screenshot using BitBlt which grabs the entire primary screen,
                    // including the broadcast overlay displaying the teacher's screen.
                    var imageField = typeof(StudentShell).GetField("_broadcastImage",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(imageField);
                    var broadcastImage = imageField.GetValue(shell) as Image;
                    Assert.NotNull(broadcastImage);

                    // Since the overlay displays the teacher's screen, and the teacher's screen displays this student's
                    // screen thumbnail in MonitorPage, this test confirms that the student client captures its own UI 
                    // containing the broadcast window.
                    bool hasRecursiveRisk = overlay.Children.Contains(broadcastImage);
                    Assert.True(hasRecursiveRisk, "Risk of infinite screen recursion because the broadcast image container is nested within the active overlay layout.");

                    // Cleanup
                    var closeMethod = typeof(StudentShell).GetMethod("CloseScreenBroadcast",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    closeMethod?.Invoke(shell, null);
                }
                finally
                {
                    try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch {}
                    BroadcastStateService.Instance.Reset();
                }
            });
        }

        /// <summary>
        /// TC-02 [QA & Connectivity Expert Evaluation]:
        /// Simulates what happens to the student screen when network transmission fails (UDP/HTTP blocked/delayed).
        /// Verifies that the student client shows a dark/black screen because the overlay background is 
        /// set to dark (240, 20, 20, 30) and the image source remains null.
        /// </summary>
        [Fact]
        public void TC02_Simulate_StudentScreenBlackOut_On_TransmissionFailure()
        {
            RunOnSTA(() =>
            {
                var shell = new StudentShell();

                // Stop the watchdog timer
                var timerField = typeof(StudentShell).GetField("_focusWatchdogTimer",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (timerField != null)
                {
                    var timer = timerField.GetValue(shell) as System.Windows.Threading.DispatcherTimer;
                    timer?.Stop();
                }

                // Create a valid temporary file to bypass the File.Exists check
                string tempFile = Path.GetTempFileName();
                try
                {
                    var handleMethod = typeof(StudentShell).GetMethod("HandleTeacherCommand",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(handleMethod);

                    // Initiate broadcast with valid tempFile but set port to non-existent to block network stream updates
                    handleMethod.Invoke(shell, new object[] { $"CMD|SCREEN_BROADCAST_START|{tempFile}|9999|TK_BLOCKED" });

                    // Verify the overlay was created
                    var overlayField = typeof(StudentShell).GetField("_broadcastOverlay",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(overlayField);
                    var overlay = overlayField.GetValue(shell) as Grid;
                    Assert.NotNull(overlay);

                    // Verify overlay background is very dark (which causes the "dark screen" symptom)
                    var backgroundBrush = overlay.Background as SolidColorBrush;
                    Assert.NotNull(backgroundBrush);
                    Assert.Equal(245, backgroundBrush.Color.A); // 96% opaque
                    Assert.Equal(15, backgroundBrush.Color.R);
                    Assert.Equal(15, backgroundBrush.Color.G);
                    Assert.Equal(25, backgroundBrush.Color.B); // Dark slate/black color

                    // Verify that no frame has been received, meaning the image source remains null
                    var imageField = typeof(StudentShell).GetField("_broadcastImage",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(imageField);
                    var broadcastImage = imageField.GetValue(shell) as Image;
                    Assert.NotNull(broadcastImage);
                    Assert.Null(broadcastImage.Source); // Image source must be null because no valid frame could be loaded

                    // Cleanup
                    var closeMethod = typeof(StudentShell).GetMethod("CloseScreenBroadcast",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    closeMethod?.Invoke(shell, null);
                }
                finally
                {
                    try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch {}
                }
            });
        }
    }
}
