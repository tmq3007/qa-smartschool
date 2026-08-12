using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;
using QASmartClass.LearningTools.Views.Science;

namespace QASmartClass.Tests.Services
{
    public class LensToolTests
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
        public void TestLensTool_ConvergentLensCalculation()
        {
            RunInSta(() =>
            {
                var tool = new LensTool();

                var txtF = tool.FindName("txtF") as TextBox;
                var txtD = tool.FindName("txtD") as TextBox;
                var txtDp = tool.FindName("txtDp") as TextBox;

                Assert.NotNull(txtF);
                Assert.NotNull(txtD);
                Assert.NotNull(txtDp);

                // Set f = 20, d = 30
                txtF.Text = "20";
                txtD.Text = "30";
                txtDp.Text = "";

                // d' should be calculated as 60
                Assert.Equal("60", txtDp.Text);

                // Check slider sync
                var sliderF = tool.FindName("sliderF") as Slider;
                var sliderD = tool.FindName("sliderD") as Slider;
                var sliderDp = tool.FindName("sliderDp") as Slider;

                Assert.NotNull(sliderF);
                Assert.NotNull(sliderD);
                Assert.NotNull(sliderDp);

                Assert.Equal(20, sliderF.Value);
                Assert.Equal(30, sliderD.Value);
                Assert.Equal(60, sliderDp.Value);
            });
        }

        [Fact]
        public void TestLensTool_DivergentLensCalculation()
        {
            RunInSta(() =>
            {
                var tool = new LensTool();

                var txtF = tool.FindName("txtF") as TextBox;
                var txtD = tool.FindName("txtD") as TextBox;
                var txtDp = tool.FindName("txtDp") as TextBox;

                Assert.NotNull(txtF);
                Assert.NotNull(txtD);
                Assert.NotNull(txtDp);

                // Set f = -15, d = 10
                txtF.Text = "-15";
                txtD.Text = "10";
                txtDp.Text = "";

                // dp = (-15 * 10) / (10 - (-15)) = -150 / 25 = -6
                Assert.Equal("-6", txtDp.Text);

                var sliderDp = tool.FindName("sliderDp") as Slider;
                Assert.NotNull(sliderDp);
                Assert.Equal(-6, sliderDp.Value);
            });
        }

        [Fact]
        public void TestLensTool_InfinityWarning()
        {
            RunInSta(() =>
            {
                var tool = new LensTool();

                var txtF = tool.FindName("txtF") as TextBox;
                var txtD = tool.FindName("txtD") as TextBox;
                var txtDp = tool.FindName("txtDp") as TextBox;
                var resultPanel = tool.FindName("resultPanel") as StackPanel;

                Assert.NotNull(txtF);
                Assert.NotNull(txtD);
                Assert.NotNull(txtDp);
                Assert.NotNull(resultPanel);

                // Set f = 20, d = 20
                txtF.Text = "20";
                txtD.Text = "20";
                txtDp.Text = "";

                // Check warning shown
                bool foundWarning = false;
                foreach (var child in resultPanel.Children)
                {
                    if (child is Border border)
                    {
                        string text = GetBorderText(border);
                        if (text.Contains("Ảnh ở vô cực"))
                        {
                            foundWarning = true;
                            break;
                        }
                    }
                }
                Assert.True(foundWarning, "d = f warning should be displayed in results.");
            });
        }

        private static string GetBorderText(Border border)
        {
            if (border.Child is Grid grid && grid.Children.Count > 0)
            {
                var contentElement = grid.Children[0];
                if (contentElement is TextBlock tb) return tb.Text;
                if (contentElement is WrapPanel wp)
                {
                    var parts = new System.Collections.Generic.List<string>();
                    foreach (var child in wp.Children)
                    {
                        if (child is TextBlock childTb) parts.Add(childTb.Text);
                        else if (child.GetType().Name == "FormulaControl")
                        {
                            var formulaProp = child.GetType().GetProperty("Formula");
                            if (formulaProp != null) parts.Add($"${formulaProp.GetValue(child)}$");
                        }
                    }
                    return string.Join("", parts);
                }
                if (contentElement.GetType().Name == "FormulaControl")
                {
                    var formulaProp = contentElement.GetType().GetProperty("Formula");
                    if (formulaProp != null) return $"${formulaProp.GetValue(contentElement)}$";
                }
            }
            else if (border.Child is TextBlock textBlock)
            {
                return textBlock.Text;
            }
            return string.Empty;
        }

        [Fact]
        public void TestLensTool_ApplyPracticalAppParams()
        {
            RunInSta(() =>
            {
                var tool = new LensTool();

                var cmbRealWorldApp = tool.FindName("cmbRealWorldApp") as ComboBox;
                var btnApplyAppParams = tool.FindName("btnApplyAppParams") as Border;
                var txtF = tool.FindName("txtF") as TextBox;
                var txtD = tool.FindName("txtD") as TextBox;
                var txtDp = tool.FindName("txtDp") as TextBox;

                Assert.NotNull(cmbRealWorldApp);
                Assert.NotNull(btnApplyAppParams);
                Assert.NotNull(txtF);
                Assert.NotNull(txtD);
                Assert.NotNull(txtDp);

                // Select index 0 (Kính lúp: f=20, d=12)
                cmbRealWorldApp.SelectedIndex = 0;

                // Trigger ApplyAppParams_Click
                var method = typeof(LensTool).GetMethod("ApplyAppParams_Click", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(tool, new object[] { btnApplyAppParams, null! });

                // Check applied and calculated values
                Assert.Equal("20", txtF.Text);
                Assert.Equal("12", txtD.Text);
                Assert.Equal("-30", txtDp.Text);

                // Select index 5 (Kính thiên văn khúc xạ: f1 = 100, d = oo, dp = 100)
                cmbRealWorldApp.SelectedIndex = 5;
                method.Invoke(tool, new object[] { btnApplyAppParams, null! });

                // Check applied and calculated values
                Assert.Equal("100", txtF.Text);
                Assert.Equal("oo", txtD.Text);
                Assert.Equal("100", txtDp.Text);
            });
        }
    }
}
