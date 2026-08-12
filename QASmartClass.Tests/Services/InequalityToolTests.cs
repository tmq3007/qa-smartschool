using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;
using QASmartClass.LearningTools.Views.Math;

namespace QASmartClass.Tests.Services
{
    public class InequalityToolTests
    {
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
            Exception? exception = null;
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    if (Application.Current == null)
                    {
                        _ = new Application();
                    }
                    var resources = Application.Current!.Resources;
                    if (!resources.Contains("Gray100"))
                    {
                        resources["Gray100"] = new SolidColorBrush(Color.FromRgb(248, 249, 250));
                    }
                    if (!resources.Contains("BrandPrimary"))
                    {
                        resources["BrandPrimary"] = new SolidColorBrush(Color.FromRgb(21, 101, 192));
                    }
                    if (!resources.Contains("BrandSecondary"))
                    {
                        resources["BrandSecondary"] = new SolidColorBrush(Color.FromRgb(13, 71, 161));
                    }
                    if (!resources.Contains("BrandAccent"))
                    {
                        resources["BrandAccent"] = new SolidColorBrush(Color.FromRgb(230, 81, 0));
                    }
                    InitializeApplicationFull(); action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (exception != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
            }
        }

        [Fact]
        public void TestInequalityTool_SolveLinear_PositiveAndNegative()
        {
            RunInSta(() =>
            {
                var tool = new InequalityTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                var txtA = tool.FindName("txtIneqA1") as TextBox;
                var txtB = tool.FindName("txtIneqB1") as TextBox;
                var cboSign = tool.FindName("cboSign1") as ComboBox;
                var resultPanel = tool.FindName("resultPanel") as StackPanel;

                Assert.NotNull(txtA);
                Assert.NotNull(txtB);
                Assert.NotNull(cboSign);
                Assert.NotNull(resultPanel);

                var solveLinear = typeof(InequalityTool).GetMethod("Calc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(solveLinear);

                // Case 1: 2x - 6 > 0 -> x > 3, S = (3; +∞)
                txtA.Text = "2";
                txtB.Text = "-6";
                cboSign.SelectedIndex = 0; // > 0

                solveLinear.Invoke(tool, null);

                var results1 = resultPanel.Children.Cast<Border>()
                    .Select(b => (b.Child as TextBlock)?.Text)
                    .ToList();

                Assert.Contains(results1, text => text != null && text.Contains("S = (3; +∞)"));

                // Case 2: -3x + 9 <= 0 -> x >= 3, S = [3; +∞)
                txtA.Text = "-3";
                txtB.Text = "9";
                cboSign.SelectedIndex = 3; // <= 0

                solveLinear.Invoke(tool, null);

                var results2 = resultPanel.Children.Cast<Border>()
                    .Select(b => (b.Child as TextBlock)?.Text)
                    .ToList();

                Assert.Contains(results2, text => text != null && text.Contains("S = [3; +∞)"));
            });
        }

        [Fact]
        public void TestInequalityTool_SolveQuadratic()
        {
            RunInSta(() =>
            {
                var tool = new InequalityTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                var cboIneqType = tool.FindName("cboIneqType") as ComboBox;
                var txtA = tool.FindName("txtIneqA2") as TextBox;
                var txtB = tool.FindName("txtIneqB2") as TextBox;
                var txtC = tool.FindName("txtIneqC2") as TextBox;
                var cboSign = tool.FindName("cboSign2") as ComboBox;
                var resultPanel = tool.FindName("resultPanel") as StackPanel;

                Assert.NotNull(cboIneqType);
                Assert.NotNull(txtA);
                Assert.NotNull(txtB);
                Assert.NotNull(txtC);
                Assert.NotNull(cboSign);
                Assert.NotNull(resultPanel);

                // Switch to Quadratic BPT
                cboIneqType.SelectedIndex = 1;

                var solveQuadratic = typeof(InequalityTool).GetMethod("Calc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(solveQuadratic);

                // Case 1: x^2 - 5x + 6 > 0 -> S = (-∞; 2) U (3; +∞)
                txtA.Text = "1";
                txtB.Text = "-5";
                txtC.Text = "6";
                cboSign.SelectedIndex = 0; // > 0

                solveQuadratic.Invoke(tool, null);

                var results = resultPanel.Children.Cast<Border>()
                    .Select(b => (b.Child as TextBlock)?.Text)
                    .ToList();

                Assert.Contains(results, text => text != null && text.Contains("S = (−∞; 2) ∪ (3; +∞)"));
            });
        }

        [Fact]
        public void TestInequalityTool_StepsDisplay()
        {
            RunInSta(() =>
            {
                var tool = new InequalityTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                var txtA1 = tool.FindName("txtIneqA1") as TextBox;
                var txtB1 = tool.FindName("txtIneqB1") as TextBox;
                var txtSteps = tool.FindName("txtSteps") as TextBlock;

                Assert.NotNull(txtA1);
                Assert.NotNull(txtB1);
                Assert.NotNull(txtSteps);

                txtA1.Text = "2";
                txtB1.Text = "-6";

                var calcMethod = typeof(InequalityTool).GetMethod("Calc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(calcMethod);
                calcMethod.Invoke(tool, null);

                Assert.Contains("vế phải", txtSteps.Text);
            });
        }

        [Fact]
        public void TestInequalityTool_InteractiveExamples()
        {
            RunInSta(() =>
            {
                var tool = new InequalityTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                var lstExamples = tool.FindName("lstExamples") as ListBox;
                var btnApply = tool.FindName("btnApplyExample") as Button;
                var txtA = tool.FindName("txtIneqA1") as TextBox;
                var txtB = tool.FindName("txtIneqB1") as TextBox;
                var cboSign = tool.FindName("cboSign1") as ComboBox;
                var resultPanel = tool.FindName("resultPanel") as StackPanel;

                Assert.NotNull(lstExamples);
                Assert.NotNull(btnApply);
                Assert.NotNull(txtA);
                Assert.NotNull(txtB);
                Assert.NotNull(cboSign);
                Assert.NotNull(resultPanel);

                // Select index 1 (Lợi Nhuận)
                lstExamples.SelectedIndex = 1;

                // Simulate clicking Apply
                btnApply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                // Verify values populated: a = 10, b = -3000, sign = > 0 (Index 0)
                Assert.Equal("10", txtA.Text);
                Assert.Equal("-3000", txtB.Text);
                Assert.Equal(0, cboSign.SelectedIndex);

                // Verify results computed
                var results = resultPanel.Children.Cast<Border>()
                    .Select(b => (b.Child as TextBlock)?.Text)
                    .ToList();
                Assert.Contains(results, t => t != null && t.Contains("S = (300; +∞)"));
            });
        }
    }
}
