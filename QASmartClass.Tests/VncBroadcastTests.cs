using Xunit;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Reflection;
using QASmartClass.Classroom.Views;
using QASmartClass.StudentClient.Views;

namespace QASmartClass.Tests
{
    public class VncBroadcastTests
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
            try
            {
                // Đăng ký pack:// scheme cho WPF unit test
                var packScheme = System.IO.Packaging.PackUriHelper.UriSchemePack;
            }
            catch {}

            if (System.Windows.Application.Current == null)
            {
                try
                {
                    new QASmartTouch.App();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[VncBroadcastTests] App creation error: {ex.Message}");
                }
            }

            if (System.Windows.Application.Current != null)
            {
                try
                {
                    var resources = System.Windows.Application.Current.Resources;
                    string[] resourceFiles = new string[]
                    {
                        "Resources/DesignTokens.xaml",
                        "Resources/Styles.xaml",
                        "Resources/StaffTheme.xaml",
                        "Resources/SvgIcons.xaml"
                    };

                    foreach (var file in resourceFiles)
                    {
                        bool exists = false;
                        foreach (ResourceDictionary merged in resources.MergedDictionaries)
                        {
                            if (merged.Source != null && merged.Source.OriginalString.Contains(file))
                            {
                                exists = true;
                                break;
                            }
                        }

                        if (!exists)
                        {
                            resources.MergedDictionaries.Add(new ResourceDictionary
                            {
                                Source = new Uri($"pack://application:,,,/QASmartClass;component/{file}", UriKind.Absolute)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[VncBroadcastTests] Resource merging error: {ex.Message}");
                }
            }
        }

        [Fact]
        public void BroadcastPage_VncFields_InitializeToDefault()
        {
            RunOnStaThread(() =>
            {
                EnsureApplication();
                var page = new BroadcastPage();
                Assert.NotNull(page);

                // Verify fields exists and have correct defaults
                var isVncBroadcastingField = typeof(BroadcastPage).GetField("_isVncBroadcasting", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isVncBroadcastingField);
                Assert.False((bool)isVncBroadcastingField.GetValue(page)!);

                var vncPortField = typeof(BroadcastPage).GetField("_vncPort", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(vncPortField);
                Assert.Equal(5901, (int)vncPortField.GetValue(page)!);

                var vncSessionCodeField = typeof(BroadcastPage).GetField("_vncSessionCode", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(vncSessionCodeField);
                Assert.Equal(string.Empty, (string)vncSessionCodeField.GetValue(page)!);
            });
        }

        [Fact]
        public void BroadcastPage_GetAvailablePort_ReturnsValidPort()
        {
            RunOnStaThread(() =>
            {
                EnsureApplication();
                var page = new BroadcastPage();

                var getAvailablePortMethod = typeof(BroadcastPage).GetMethod("GetAvailablePort", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(getAvailablePortMethod);

                // Check starting from 5901
                int port = (int)getAvailablePortMethod.Invoke(page, new object[] { 5901 })!;
                Assert.True(port >= 5901 && port < 6000);
            });
        }

        [Fact]
        public void BroadcastPage_WriteVncServerIni_GeneratesFile()
        {
            RunOnStaThread(() =>
            {
                EnsureApplication();
                var page = new BroadcastPage();

                var writeVncServerIniMethod = typeof(BroadcastPage).GetMethod("WriteVncServerIni", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(writeVncServerIniMethod);

                string testSessionCode = "TK_TEST123";
                int testPort = 5999;
                
                // Invoke method
                writeVncServerIniMethod.Invoke(page, new object[] { testPort, testSessionCode });

                // Check file existence
                string vncDir = @"D:\JOB\vnctool";
                string iniPath = System.IO.Path.Combine(vncDir, "server.ini");
                Assert.True(System.IO.File.Exists(iniPath));

                // Verify file content
                string content = System.IO.File.ReadAllText(iniPath);
                Assert.Contains($"port = {testPort}", content);
                Assert.Contains($"session_code = {testSessionCode}", content);
            });
        }

        [Fact]
        public void StudentShell_VncFields_InitializeToDefault()
        {
            RunOnStaThread(() =>
            {
                EnsureApplication();
                var shell = new StudentShell();
                Assert.NotNull(shell);

                var isVncClientRunningField = typeof(StudentShell).GetField("_isVncClientRunning", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isVncClientRunningField);
                Assert.False((bool)isVncClientRunningField.GetValue(shell)!);

                var vncServerIpField = typeof(StudentShell).GetField("_vncServerIp", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(vncServerIpField);
                Assert.Equal(string.Empty, (string)vncServerIpField.GetValue(shell)!);

                var vncServerPortField = typeof(StudentShell).GetField("_vncServerPort", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(vncServerPortField);
                Assert.Equal(5901, (int)vncServerPortField.GetValue(shell)!);

                var vncSessionCodeField = typeof(StudentShell).GetField("_vncSessionCode", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(vncSessionCodeField);
                Assert.Equal(string.Empty, (string)vncSessionCodeField.GetValue(shell)!);
            });
        }
    }
}
