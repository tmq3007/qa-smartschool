using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;
using QASmartClass.LearningTools.Views.Math;

namespace QASmartClass.Tests.Services
{
    public class LinearSystemToolTests
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
                    if (!resources.Contains("CommonFontFamily")) resources["CommonFontFamily"] = new FontFamily("Segoe UI");
                    if (!resources.Contains("BrandPrimary")) resources["BrandPrimary"] = new SolidColorBrush(Color.FromRgb(21, 101, 192));
                    if (!resources.Contains("BrandSecondary")) resources["BrandSecondary"] = new SolidColorBrush(Color.FromRgb(13, 71, 161));
                    if (!resources.Contains("BrandAccent")) resources["BrandAccent"] = new SolidColorBrush(Color.FromRgb(230, 81, 0));
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
        public void TestLinearSystemTool_SolveUniqueSolution()
        {
            RunInSta(() =>
            {
                var tool = new LinearSystemTool();
                var txtA1 = tool.FindName("txtA1") as TextBox;
                var txtB1 = tool.FindName("txtB1") as TextBox;
                var txtC1 = tool.FindName("txtC1") as TextBox;
                var txtA2 = tool.FindName("txtA2") as TextBox;
                var txtB2 = tool.FindName("txtB2") as TextBox;
                var txtC2 = tool.FindName("txtC2") as TextBox;
                var txtResult = tool.FindName("txtResult") as TextBlock;
                var txtClassify = tool.FindName("txtClassify") as TextBlock;

                Assert.NotNull(txtA1);
                Assert.NotNull(txtB1);
                Assert.NotNull(txtC1);
                Assert.NotNull(txtA2);
                Assert.NotNull(txtB2);
                Assert.NotNull(txtC2);
                Assert.NotNull(txtResult);
                Assert.NotNull(txtClassify);

                // System: 2x + 3y = 8 & 1x - 1y = 1
                txtA1.Text = "2";
                txtB1.Text = "3";
                txtC1.Text = "8";
                txtA2.Text = "1";
                txtB2.Text = "-1";
                txtC2.Text = "1";

                // Trigger Solve by invoking Solve() via reflection
                var method = typeof(LinearSystemTool).GetMethod("Solve", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                // Solution should be x = 2.2, y = 1.2
                string resultText = txtResult.Text;
                Assert.Contains("x = 2.2", resultText);
                Assert.Contains("y = 1.2", resultText);
                Assert.Contains("nghiệm duy nhất", txtClassify.Text);
            });
        }

        [Fact]
        public void TestLinearSystemTool_SolveNoSolution()
        {
            RunInSta(() =>
            {
                var tool = new LinearSystemTool();
                var txtA1 = tool.FindName("txtA1") as TextBox;
                var txtB1 = tool.FindName("txtB1") as TextBox;
                var txtC1 = tool.FindName("txtC1") as TextBox;
                var txtA2 = tool.FindName("txtA2") as TextBox;
                var txtB2 = tool.FindName("txtB2") as TextBox;
                var txtC2 = tool.FindName("txtC2") as TextBox;
                var txtResult = tool.FindName("txtResult") as TextBlock;
                var txtClassify = tool.FindName("txtClassify") as TextBlock;

                // Parallel lines: x + 2y = 3 & 2x + 4y = 5 (No Solution)
                txtA1.Text = "1";
                txtB1.Text = "2";
                txtC1.Text = "3";
                txtA2.Text = "2";
                txtB2.Text = "4";
                txtC2.Text = "5";

                var method = typeof(LinearSystemTool).GetMethod("Solve", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                Assert.Contains("Hệ vô nghiệm", txtResult.Text);
                Assert.Contains("song song", txtClassify.Text);
            });
        }

        [Fact]
        public void TestLinearSystemTool_ShowHideSteps()
        {
            RunInSta(() =>
            {
                var tool = new LinearSystemTool();
                var txtA1 = tool.FindName("txtA1") as TextBox;
                var txtB1 = tool.FindName("txtB1") as TextBox;
                var txtC1 = tool.FindName("txtC1") as TextBox;
                var txtA2 = tool.FindName("txtA2") as TextBox;
                var txtB2 = tool.FindName("txtB2") as TextBox;
                var txtC2 = tool.FindName("txtC2") as TextBox;
                var chkShowSteps = tool.FindName("chkShowSteps") as CheckBox;
                var stepsPanel = tool.FindName("stepsPanel") as StackPanel;

                Assert.NotNull(chkShowSteps);
                Assert.NotNull(stepsPanel);

                // Default checked -> stepsPanel has children
                txtA1.Text = "2";
                txtB1.Text = "3";
                txtC1.Text = "8";
                txtA2.Text = "1";
                txtB2.Text = "-1";
                txtC2.Text = "1";

                var method = typeof(LinearSystemTool).GetMethod("Solve", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, null);

                Assert.True(chkShowSteps.IsChecked);
                Assert.True(stepsPanel.Children.Count > 0);

                // Uncheck and toggle
                chkShowSteps.IsChecked = false;
                var toggleMethod = typeof(LinearSystemTool).GetMethod("ToggleSteps_Click", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(toggleMethod);
                toggleMethod.Invoke(tool, new object[] { chkShowSteps, new RoutedEventArgs() });

                Assert.Empty(stepsPanel.Children);
                Assert.Equal(Visibility.Collapsed, stepsPanel.Visibility);
            });
        }

        [Fact]
        public void TestLinearSystemTool_InteractiveExamples()
        {
            RunInSta(() =>
            {
                var tool = new LinearSystemTool();
                var btnBreakeven = tool.FindName("btnExSysBreakeven") as Button;
                var btnMixture = tool.FindName("btnExSysMixture") as Button;
                var btnApply = tool.FindName("btnApplySysExample") as Button;
                var txtA1 = tool.FindName("txtA1") as TextBox;
                var txtB1 = tool.FindName("txtB1") as TextBox;
                var txtC1 = tool.FindName("txtC1") as TextBox;
                var txtA2 = tool.FindName("txtA2") as TextBox;
                var txtB2 = tool.FindName("txtB2") as TextBox;
                var txtC2 = tool.FindName("txtC2") as TextBox;
                var txtResult = tool.FindName("txtResult") as TextBlock;

                Assert.NotNull(btnBreakeven);
                Assert.NotNull(btnMixture);
                Assert.NotNull(btnApply);
                Assert.NotNull(txtA1);
                Assert.NotNull(txtB1);
                Assert.NotNull(txtC1);
                Assert.NotNull(txtA2);
                Assert.NotNull(txtB2);
                Assert.NotNull(txtC2);
                Assert.NotNull(txtResult);

                // Simulate clicking the mixture example button
                btnMixture.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                // Verify that selected index changed in code-behind
                var selectedIndexField = typeof(LinearSystemTool).GetField("_selectedSystemExampleIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(selectedIndexField);
                int indexVal = (int)selectedIndexField.GetValue(tool)!;
                Assert.Equal(1, indexVal); // Mixture is index 1

                // Simulate clicking Apply
                btnApply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                // Verify values populated: a1=1, b1=1, c1=200, a2=0.1, b2=0.3, c2=50
                Assert.Equal("1", txtA1.Text);
                Assert.Equal("1", txtB1.Text);
                Assert.Equal("200", txtC1.Text);
                Assert.Equal("0.1", txtA2.Text);
                Assert.Equal("0.3", txtB2.Text);
                Assert.Equal("50", txtC2.Text);

                // Verify results computed: x = 50, y = 150
                string resultText = txtResult.Text;
                Assert.Contains("x = 50", resultText);
                Assert.Contains("y = 150", resultText);
            });
        }
    }
}
