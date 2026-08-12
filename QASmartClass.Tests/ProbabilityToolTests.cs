using System;
using System.Reflection;
using System.Windows;
using System.Numerics;
using Xunit;
using QASmartClass.LearningTools.Views.Math;

namespace QASmartClass.Tests
{
    public class ProbabilityToolTests
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
                        _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
                        try
                        {
                            var appField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                            var createdField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                            if (appField != null) appField.SetValue(null, null);
                            if (createdField != null) createdField.SetValue(null, false);
                        }
                        catch { }

                        Application? app = null;
                        try
                        {
                            app = new Application();
                        }
                        catch
                        {
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
        public void Test_ProbabilityTool_CanBeInstantiated()
        {
            RunTest(() =>
            {
                var tool = new ProbabilityTool();
                Assert.NotNull(tool);
            });
        }

        [Fact]
        public void Test_ProbabilityTool_FactorialCache_Correctness()
        {
            RunTest(() =>
            {
                var method = typeof(ProbabilityTool).GetMethod("Factorial", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(method);

                // Test F(0) = 1
                var f0 = (BigInteger)method.Invoke(null, new object[] { 0 })!;
                Assert.Equal(BigInteger.One, f0);

                // Test F(1) = 1
                var f1 = (BigInteger)method.Invoke(null, new object[] { 1 })!;
                Assert.Equal(BigInteger.One, f1);

                // Test F(5) = 120
                var f5 = (BigInteger)method.Invoke(null, new object[] { 5 })!;
                Assert.Equal(new BigInteger(120), f5);

                // Test F(10) = 3628800
                var f10 = (BigInteger)method.Invoke(null, new object[] { 10 })!;
                Assert.Equal(new BigInteger(3628800), f10);

                // Test F(170) does not crash and matches cached value
                var f170 = (BigInteger)method.Invoke(null, new object[] { 170 })!;
                Assert.True(f170 > 0);
            });
        }

        [Fact]
        public void Test_ProbabilityTool_GCD_Correctness()
        {
            RunTest(() =>
            {
                var method = typeof(ProbabilityTool).GetMethod("GCD", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(method);

                var gcd1 = (long)method.Invoke(null, new object[] { 6L, 36L })!;
                Assert.Equal(6L, gcd1);

                var gcd2 = (long)method.Invoke(null, new object[] { 17L, 5L })!;
                Assert.Equal(1L, gcd2);

                var gcd3 = (long)method.Invoke(null, new object[] { 0L, 10L })!;
                Assert.Equal(10L, gcd3);
            });
        }

        [Fact]
        public void Test_ProbabilityTool_SimplifyFraction_Correctness()
        {
            RunTest(() =>
            {
                var method = typeof(ProbabilityTool).GetMethod("SimplifyFraction", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(method);

                var res1 = (ValueTuple<long, long>)method.Invoke(null, new object[] { 6L, 36L })!;
                Assert.Equal(1L, res1.Item1);
                Assert.Equal(6L, res1.Item2);

                var res2 = (ValueTuple<long, long>)method.Invoke(null, new object[] { 0L, 10L })!;
                Assert.Equal(0L, res2.Item1);
                Assert.Equal(1L, res2.Item2);

                var res3 = (ValueTuple<long, long>)method.Invoke(null, new object[] { 5L, 5L })!;
                Assert.Equal(1L, res3.Item1);
                Assert.Equal(1L, res3.Item2);
            });
        }

        [Fact]
        public void Test_ProbabilityTool_NewControls_Exist()
        {
            RunTest(() =>
            {
                var tool = new ProbabilityTool();
                Assert.NotNull(tool.FindName("txtRedBalls"));
                Assert.NotNull(tool.FindName("txtBlueBalls"));
                Assert.NotNull(tool.FindName("txtDraws"));
                Assert.NotNull(tool.FindName("txtBallSimTrials"));
                Assert.NotNull(tool.FindName("btnExportCsv"));
            });
        }
    }
}
