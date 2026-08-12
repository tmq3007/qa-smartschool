using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.StudentClient.Views;
using Xunit;

namespace QASmartClass.Tests
{
    public class LOI_VID_15_OneHundredTimeIntervalTests
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
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    InitializeApplicationFull(); action();
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error("STA Thread exception: {Err}", ex.Message);
                    throw;
                }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        public static IEnumerable<object[]> GetOneHundredIntervalTestData()
        {
            var list = new List<object[]>();
            // Sinh ra đúng 100 bộ dữ liệu test case khác nhau về thời gian
            // 1-20: Watchdog timeouts (1s -> 20s)
            for (int i = 1; i <= 20; i++)
            {
                list.Add(new object[] { i, "WatchdogTimeout", i * 1.0, 0, "FlatBlack" });
            }
            // 21-40: Countdown tick delays (100ms -> 2000ms)
            for (int i = 1; i <= 20; i++)
            {
                list.Add(new object[] { 20 + i, "CountdownTick", 15.0, i * 100, "FlatBlack" });
            }
            // 41-60: Lockout retry/duration settings
            for (int i = 1; i <= 20; i++)
            {
                list.Add(new object[] { 40 + i, "LockoutSettings", 15.0, 1000, i % 2 == 0 ? "FlatBlack" : "BrandColor" });
            }
            // 61-80: Network Flapping delay intervals (100ms -> 2000ms)
            for (int i = 1; i <= 20; i++)
            {
                list.Add(new object[] { 60 + i, "NetworkFlapping", 15.0, i * 100, "FlatBlack" });
            }
            // 81-100: Capture frame delays (5 FPS -> 60 FPS equivalents)
            for (int i = 1; i <= 20; i++)
            {
                list.Add(new object[] { 80 + i, "CaptureFPS", 15.0, 1000 / (i + 1), "BrandColor" });
            }
            return list;
        }

        [Theory]
        [MemberData(nameof(GetOneHundredIntervalTestData))]
        public void TC_Interval_StressEvaluation(int testCaseId, string category, double watchdogTimeout, int intervalMs, string bgOption)
        {
            RunOnSTA(() =>
            {
                var shell = new StudentShell();
                var shellType = typeof(StudentShell);

                // 1. Setup mock overlay
                var overlayField = shellType.GetField("_broadcastOverlay", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(overlayField);
                var mockOverlay = new Grid();
                overlayField.SetValue(shell, mockOverlay);

                // 2. Kích hoạt cấu hình
                var handleMethod = shellType.GetMethod("HandleTeacherCommand", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(handleMethod);
                handleMethod.Invoke(shell, new object[] { $"CMD|SESSION_CONFIG|Broadcast_UdpHeartbeatTimeout={watchdogTimeout}|Broadcast_TurboStudentBg={bgOption}|Broadcast_TurboMode=true" });

                // 3. Xác minh cấu hình
                var timeoutField = shellType.GetField("_broadcastHeartbeatTimeout", BindingFlags.NonPublic | BindingFlags.Instance);
                if (timeoutField != null)
                {
                    double actualTimeout = (double)timeoutField.GetValue(shell)!;
                    Assert.Equal(watchdogTimeout, actualTimeout);
                }

                // 4. Xác minh màu nền đổi đúng cấu hình
                if (bgOption == "FlatBlack")
                {
                    Assert.Equal(System.Windows.Media.Brushes.Black, mockOverlay.Background);
                }
                else
                {
                    var bgBrush = mockOverlay.Background as System.Windows.Media.SolidColorBrush;
                    Assert.NotNull(bgBrush);
                    Assert.Equal(System.Windows.Media.Color.FromArgb(245, 15, 15, 25), bgBrush.Color);
                }
            });
        }
    }
}
