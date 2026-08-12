using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Xunit;
using QASmartClass.LearningTools.Views.Math;

namespace QASmartClass.Tests
{
    public class QuadraticToolTests
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
        public void Test_FormatVietnamese_DecimalsAndMinusSign()
        {
            // Test standard positive decimal
            string res1 = QuadraticTool.FormatVietnamese(2.5);
            Assert.Equal("2,5", res1);

            // Test negative decimal
            string res2 = QuadraticTool.FormatVietnamese(-0.125);
            Assert.Equal("−0,125", res2); // uses math minus sign \u2212

            // Test integer
            string res3 = QuadraticTool.FormatVietnamese(3.0);
            Assert.Equal("3", res3);

            // Test NaN
            string res4 = QuadraticTool.FormatVietnamese(double.NaN);
            Assert.Equal("NaN", res4);

            // Test Infinity
            string res5 = QuadraticTool.FormatVietnamese(double.PositiveInfinity);
            Assert.Equal("∞", res5);
        }

        [Fact]
        public void Test_SolveQuadratic_TwoDistinctRealRoots()
        {
            RunTest(() =>
            {
                var tool = new QuadraticTool();
                var txtA = (TextBox)tool.FindName("txtCoeffA");
                var txtB = (TextBox)tool.FindName("txtCoeffB");
                var txtC = (TextBox)tool.FindName("txtCoeffC");
                var txtResult = (TextBlock)tool.FindName("txtQuadResult");
                var txtDelta = (TextBlock)tool.FindName("txtDelta");
                var txtVertex = (TextBlock)tool.FindName("txtVertex");
                var txtFactored = (TextBlock)tool.FindName("txtFactored");

                Assert.NotNull(txtA);
                Assert.NotNull(txtB);
                Assert.NotNull(txtC);
                Assert.NotNull(txtResult);
                Assert.NotNull(txtDelta);
                Assert.NotNull(txtVertex);
                Assert.NotNull(txtFactored);

                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                // Input: a=1, b=-5, c=6
                txtA.Text = "1";
                txtB.Text = "-5";
                txtC.Text = "6";

                // SolveQuadratic is automatically invoked via TextChanged / Loaded
                // Let's assert the outputs are formatted in Vietnamese
                Assert.Contains("Δ = (−5)² − 4·(1)·(6) = 1", txtDelta.Text);
                Assert.Equal("x₁ = 3\nx₂ = 2", txtResult.Text);
                Assert.Equal("I(2,5; −0,25)", txtVertex.Text);
                Assert.Equal("(x − 3)(x − 2)", txtFactored.Text);
            });
        }

        [Fact]
        public void Test_SolveQuadratic_DoubleRoot()
        {
            RunTest(() =>
            {
                var tool = new QuadraticTool();
                var txtA = (TextBox)tool.FindName("txtCoeffA");
                var txtB = (TextBox)tool.FindName("txtCoeffB");
                var txtC = (TextBox)tool.FindName("txtCoeffC");
                var txtResult = (TextBlock)tool.FindName("txtQuadResult");
                var txtDelta = (TextBlock)tool.FindName("txtDelta");
                var txtVertex = (TextBlock)tool.FindName("txtVertex");
                var txtFactored = (TextBlock)tool.FindName("txtFactored");

                // Input: a=1, b=-6, c=9
                txtA.Text = "1";
                txtB.Text = "-6";
                txtC.Text = "9";

                Assert.Contains("Δ = (−6)² − 4·(1)·(9) = 0", txtDelta.Text);
                Assert.Equal("x₁ = x₂ = 3  (nghiệm kép)", txtResult.Text);
                Assert.Equal("I(3; 0)", txtVertex.Text);
                Assert.Equal("(x − 3)²", txtFactored.Text);
            });
        }

        [Fact]
        public void Test_SolveQuadratic_ComplexRoots()
        {
            RunTest(() =>
            {
                var tool = new QuadraticTool();
                var txtA = (TextBox)tool.FindName("txtCoeffA");
                var txtB = (TextBox)tool.FindName("txtCoeffB");
                var txtC = (TextBox)tool.FindName("txtCoeffC");
                var txtResult = (TextBlock)tool.FindName("txtQuadResult");
                var txtDelta = (TextBlock)tool.FindName("txtDelta");
                var chkComplex = (CheckBox)tool.FindName("chkShowComplex");

                Assert.NotNull(chkComplex);

                // Input: a=1, b=2, c=5
                txtA.Text = "1";
                txtB.Text = "2";
                txtC.Text = "5";

                // Complex mode off
                chkComplex.IsChecked = false;
                Assert.Contains("Δ = (2)² − 4·(1)·(5) = −16", txtDelta.Text);
                Assert.Equal("Vô nghiệm thực", txtResult.Text);

                // Complex mode on
                chkComplex.IsChecked = true;
                Assert.Equal("x₁ = −1 + 2i\nx₂ = −1 − 2i", txtResult.Text);
            });
        }

        [Fact]
        public void Test_Verify_PracticalAppImages_CopiedSuccessfully()
        {
            RunTest(() =>
            {
                var tool = new QuadraticTool(); // This triggers CopyMissingImagesHelper()
                
                string targetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Assets\Images");
                if (!Directory.Exists(targetDir) || !File.Exists(Path.Combine(targetDir, "app_quadratic_7_VN.png")))
                {
                    targetDir = @"d:\JOB\QA SmartClass -062026\QASmartClass\Assets\Images";
                }

                string[] requiredImages = new string[]
                {
                    "app_quadratic_7_VN.png",
                    "app_quadratic_7_EN.png",
                    "app_quadratic_8_VN.png",
                    "app_quadratic_8_EN.png"
                };

                foreach (var imgName in requiredImages)
                {
                    string path = Path.Combine(targetDir, imgName);
                    Assert.True(File.Exists(path), $"Tệp ảnh {imgName} chưa được sao chép thành công vào Assets/Images.");
                }
            });
        }
    }
}
