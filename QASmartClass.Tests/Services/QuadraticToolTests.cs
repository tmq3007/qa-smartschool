using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;
using QASmartClass.LearningTools.Views.Math;

namespace QASmartClass.Tests.Services
{
    public class QuadraticToolTests
    {
        private static readonly System.Threading.Tasks.TaskScheduler _staScheduler;

        static QuadraticToolTests()
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<System.Threading.Tasks.TaskScheduler>();
            var thread = new System.Threading.Thread(() =>
            {
                var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                var syncContext = new System.Windows.Threading.DispatcherSynchronizationContext(dispatcher);
                System.Threading.SynchronizationContext.SetSynchronizationContext(syncContext);

                try
                {
                    if (Application.Current == null)
                    {
                        try
                        {
                            _ = new Application();
                        }
                        catch { }
                    }
                    var resources = Application.Current?.Resources;
                    if (resources != null)
                    {
                        if (!resources.Contains("CommonFontFamily")) resources["CommonFontFamily"] = new FontFamily("Segoe UI");
                        if (!resources.Contains("BrandPrimary")) resources["BrandPrimary"] = new SolidColorBrush(Color.FromRgb(21, 101, 192));
                        if (!resources.Contains("BrandSecondary")) resources["BrandSecondary"] = new SolidColorBrush(Color.FromRgb(13, 71, 161));
                        if (!resources.Contains("BrandAccent")) resources["BrandAccent"] = new SolidColorBrush(Color.FromRgb(230, 81, 0));
                    }
                }
                catch { }

                tcs.SetResult(System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
                System.Windows.Threading.Dispatcher.Run();
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            _staScheduler = tcs.Task.Result;
        }

        private void RunInSta(Action action)
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
            var task = System.Threading.Tasks.Task.Factory.StartNew(action, 
                System.Threading.CancellationToken.None, 
                System.Threading.Tasks.TaskCreationOptions.None, 
                _staScheduler);
            task.GetAwaiter().GetResult();
        }

        [Fact]
        public void TestQuadraticTool_StableSolving_DeltaPositive()
        {
            RunInSta(() =>
            {
                var tool = new QuadraticTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                var txtCoeffA = tool.FindName("txtCoeffA") as TextBox;
                var txtCoeffB = tool.FindName("txtCoeffB") as TextBox;
                var txtCoeffC = tool.FindName("txtCoeffC") as TextBox;
                var txtQuadResult = tool.FindName("txtQuadResult") as TextBlock;

                Assert.NotNull(txtCoeffA);
                Assert.NotNull(txtCoeffB);
                Assert.NotNull(txtCoeffC);
                Assert.NotNull(txtQuadResult);

                // Equation: x^2 - 5x + 6 = 0
                txtCoeffA.Text = "1";
                txtCoeffB.Text = "-5";
                txtCoeffC.Text = "6"; // This TextChanged triggers SolveQuadratic

                // Roots should be x1 = 3, x2 = 2
                string resultText = txtQuadResult.Text;
                Assert.Contains("x₁ = 3", resultText);
                Assert.Contains("x₂ = 2", resultText);
            });
        }

        [Fact]
        public void TestQuadraticTool_StableSolving_ExtremeValues()
        {
            RunInSta(() =>
            {
                var tool = new QuadraticTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                var txtCoeffA = tool.FindName("txtCoeffA") as TextBox;
                var txtCoeffB = tool.FindName("txtCoeffB") as TextBox;
                var txtCoeffC = tool.FindName("txtCoeffC") as TextBox;
                var txtQuadResult = tool.FindName("txtQuadResult") as TextBlock;

                Assert.NotNull(txtCoeffA);
                Assert.NotNull(txtCoeffB);
                Assert.NotNull(txtCoeffC);
                Assert.NotNull(txtQuadResult);

                // Equation: x^2 + 1000000x + 1 = 0
                // High precision loss target. Stable root should be around -1e-6 and -1e6.
                txtCoeffA.Text = "1";
                txtCoeffB.Text = "1000000";
                txtCoeffC.Text = "1";

                string resultText = txtQuadResult.Text;
                Assert.Contains("−1000000", resultText);
                Assert.Contains("E−06", resultText.ToUpperInvariant());
            });
        }

        [Fact]
        public void TestQuadraticTool_ShowHideComplexRoots()
        {
            RunInSta(() =>
            {
                var tool = new QuadraticTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                var txtCoeffA = tool.FindName("txtCoeffA") as TextBox;
                var txtCoeffB = tool.FindName("txtCoeffB") as TextBox;
                var txtCoeffC = tool.FindName("txtCoeffC") as TextBox;
                var chkShowComplex = tool.FindName("chkShowComplex") as CheckBox;
                var txtQuadResult = tool.FindName("txtQuadResult") as TextBlock;

                Assert.NotNull(txtCoeffA);
                Assert.NotNull(txtCoeffB);
                Assert.NotNull(txtCoeffC);
                Assert.NotNull(chkShowComplex);
                Assert.NotNull(txtQuadResult);

                // Equation: x^2 + 2x + 5 = 0 (delta = 4 - 20 = -16 < 0)
                txtCoeffA.Text = "1";
                txtCoeffB.Text = "2";
                txtCoeffC.Text = "5";

                // Default: chkShowComplex is false
                Assert.False(chkShowComplex.IsChecked);
                Assert.Equal("Vô nghiệm thực", txtQuadResult.Text);

                // Check chkShowComplex
                chkShowComplex.IsChecked = true; // This Checked event triggers SolveQuadratic

                // Now it should show complex roots x1 = -1 + 2i, x2 = -1 - 2i
                string resultText = txtQuadResult.Text;
                Assert.Contains("i", resultText);
                Assert.Contains("−1 + 2i", resultText);
                Assert.Contains("−1 − 2i", resultText);

                // Uncheck chkShowComplex
                chkShowComplex.IsChecked = false; // This Unchecked event triggers SolveQuadratic
                Assert.Equal("Vô nghiệm thực", txtQuadResult.Text);
            });
        }

        [Fact]
        public void TestQuadraticTool_InteractivePresets()
        {
            RunInSta(() =>
            {
                var tool = new QuadraticTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                
                var presetPanel = tool.FindName("presetPanel") as WrapPanel;
                var txtCoeffA = tool.FindName("txtCoeffA") as TextBox;
                var txtCoeffB = tool.FindName("txtCoeffB") as TextBox;
                var txtCoeffC = tool.FindName;
                var txtQuadResult = tool.FindName("txtQuadResult") as TextBlock;

                Assert.NotNull(presetPanel);
                Assert.NotNull(txtCoeffA);
                Assert.NotNull(txtCoeffB);
                
                // Wait! txtCoeffC is text box
                var txtCoeffC_real = tool.FindName("txtCoeffC") as TextBox;
                Assert.NotNull(txtCoeffC_real);
                Assert.NotNull(txtQuadResult);

                // Retrieve the first preset button (which is "Δ > 0" example: a=1, b=-5, c=6, implemented as a Border)
                var presetBtn = presetPanel.Children.OfType<Border>().FirstOrDefault();
                Assert.NotNull(presetBtn);

                // Simulate clicking the preset Border
                presetBtn.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(
                    System.Windows.Input.InputManager.Current.PrimaryMouseDevice,
                    0,
                    System.Windows.Input.MouseButton.Left)
                {
                    RoutedEvent = UIElement.MouseLeftButtonDownEvent
                });

                // Verify coefficients are populated
                Assert.Equal("1", txtCoeffA.Text);
                Assert.Equal("-5", txtCoeffB.Text);
                Assert.Equal("6", txtCoeffC_real.Text);

                // Verify results are correct
                string resultText = txtQuadResult.Text;
                Assert.Contains("x₁ = 3", resultText);
                Assert.Contains("x₂ = 2", resultText);
            });
        }

        [Fact]
        public void TestQuadraticTool_EulerConstantParsing()
        {
            RunInSta(() =>
            {
                var tool = new QuadraticTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                var txtCoeffA = tool.FindName("txtCoeffA") as TextBox;
                var txtCoeffB = tool.FindName("txtCoeffB") as TextBox;
                var txtCoeffC = tool.FindName("txtCoeffC") as TextBox;
                var txtQuadResult = tool.FindName("txtQuadResult") as TextBlock;

                Assert.NotNull(txtCoeffA);
                Assert.NotNull(txtCoeffB);
                Assert.NotNull(txtCoeffC);
                Assert.NotNull(txtQuadResult);

                // Equation: x^2 - ex = 0 (roots: x = 0, x = e ≈ 2.71828)
                txtCoeffA.Text = "1";
                txtCoeffB.Text = "-e";
                txtCoeffC.Text = "0";

                string resultText = txtQuadResult.Text;
                Assert.Contains("2,71828", resultText);
                Assert.Contains("x₂ = 0", resultText);
            });
        }
    }
}
