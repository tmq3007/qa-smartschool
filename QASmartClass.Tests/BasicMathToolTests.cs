using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Xunit;
using QASmartClass.LearningTools.Views.Math;

namespace QASmartClass.Tests
{
    public class BasicMathToolTests
    {
        private static System.Threading.Thread? _staThread;
        private static System.Windows.Threading.Dispatcher? _dispatcher;
        private static readonly object _lock = new();

        private static void EnsureStaThread()
        {
            lock (_lock)
            {
                if (_staThread == null)
                {
                    var readyEvent = new System.Threading.ManualResetEvent(false);
                    _staThread = new System.Threading.Thread(() =>
                    {
                        // Đăng ký pack scheme cần thiết cho WPF Uri
                        _ = System.IO.Packaging.PackUriHelper.UriSchemePack;

                        try
                        {
                            var appField = typeof(Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                            var createdField = typeof(Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                            System.Console.WriteLine($"[DEBUG] appField found: {appField != null}, createdField found: {createdField != null}");
                            if (createdField != null)
                            {
                                System.Console.WriteLine($"[DEBUG] _appCreatedInThisAppDomain before: {createdField.GetValue(null)}");
                            }
                            if (appField != null) appField.SetValue(null, null);
                            if (createdField != null) createdField.SetValue(null, false);
                            if (createdField != null)
                            {
                                System.Console.WriteLine($"[DEBUG] _appCreatedInThisAppDomain after: {createdField.GetValue(null)}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Console.WriteLine($"[DEBUG] Reflection reset failed: {ex}");
                        }

                        Application? app = null;
                        try
                        {
                            app = new Application();
                        }
                        catch (Exception ex)
                        {
                            System.Console.WriteLine($"[DEBUG] new Application() failed: {ex}");
                            app = Application.Current;
                        }

                        if (app != null)
                        {
                            try
                            {
                                bool hasTokens = false;
                                foreach (var dict in app.Resources.MergedDictionaries)
                                {
                                    if (dict.Source != null && dict.Source.OriginalString.Contains("DesignTokens.xaml"))
                                    {
                                        hasTokens = true;
                                        break;
                                    }
                                }

                                if (!hasTokens)
                                {
                                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                                    {
                                        Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute)
                                    });
                                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                                    {
                                        Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/Styles.xaml", UriKind.Absolute)
                                    });
                                }
                            }
                            catch { }
                        }

                        _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                        readyEvent.Set();
                        System.Windows.Threading.Dispatcher.Run();
                    });
                    _staThread.SetApartmentState(System.Threading.ApartmentState.STA);
                    _staThread.IsBackground = true;
                    _staThread.Start();
                    readyEvent.WaitOne();
                }
            }
        }

        private void RunTest(Action action)
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
            EnsureStaThread();
            Exception? testEx = null;
            _dispatcher!.Invoke(() =>
            {
                try
                {
                    InitializeApplicationFull(); action();
                }
                catch (Exception ex)
                {
                    testEx = ex;
                }
            });
            if (testEx != null) throw testEx;
        }

        [Fact]
        public void Test_BasicMathTool_CanBeInstantiated()
        {
            RunTest(() =>
            {
                var tool = new BasicMathTool();
                Assert.NotNull(tool);
            });
        }

        [Fact]
        public void Test_MathPlayerZone_CanBeInstantiated()
        {
            RunTest(() =>
            {
                var zone = new MathPlayerZone();
                Assert.NotNull(zone);
            });
        }

        [Fact]
        public void Test_MathPlayerZone_StartGame_InitializesProperties()
        {
            RunTest(() =>
            {
                var zone = new MathPlayerZone();
                zone.StartGame(
                    playerName: "Test Player 1",
                    themeColor: Colors.Blue,
                    rangeMax: 100,
                    op: MathPlayerZone.MathOp.Add,
                    mode: MathPlayerZone.PlayMode.SelfCalc,
                    maxQuestions: 10
                );

                // Verify properties using reflection
                var modeField = typeof(MathPlayerZone).GetField("_mode", BindingFlags.NonPublic | BindingFlags.Instance);
                var rangeMaxField = typeof(MathPlayerZone).GetField("_rangeMax", BindingFlags.NonPublic | BindingFlags.Instance);
                var operationField = typeof(MathPlayerZone).GetField("_operation", BindingFlags.NonPublic | BindingFlags.Instance);
                var maxQuestionsField = typeof(MathPlayerZone).GetField("_maxQuestions", BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.NotNull(modeField);
                Assert.NotNull(rangeMaxField);
                Assert.NotNull(operationField);
                Assert.NotNull(maxQuestionsField);

                Assert.Equal(MathPlayerZone.PlayMode.SelfCalc, (MathPlayerZone.PlayMode)modeField.GetValue(zone));
                Assert.Equal(100, (int)rangeMaxField.GetValue(zone));
                Assert.Equal(MathPlayerZone.MathOp.Add, (MathPlayerZone.MathOp)operationField.GetValue(zone));
                Assert.Equal(10, (int)maxQuestionsField.GetValue(zone));
            });
        }
    }
}
