using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;
using QASmartClass.LearningTools.Views.Science;

namespace QASmartClass.Tests.Services
{
    public class PhScaleToolTests
    {
        private void RunInSta(Action action)
        {
            Exception? exception = null;
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    // Initialize WPF Application object if not already present
                    if (Application.Current == null)
                    {
                        _ = new Application();
                    }
                    var resources = Application.Current!.Resources;
                    if (!resources.Contains("Gray100"))
                    {
                        resources["Gray100"] = new SolidColorBrush(Color.FromRgb(248, 249, 250));
                    }
                    try
                    {
                        bool hasTokens = false;
                        foreach (var dict in resources.MergedDictionaries)
                        {
                            if (dict.Source != null && dict.Source.OriginalString.Contains("DesignTokens.xaml"))
                            {
                                hasTokens = true;
                                break;
                            }
                        }
                        if (!hasTokens)
                        {
                            resources.MergedDictionaries.Add(new ResourceDictionary
                            {
                                Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute)
                            });
                            resources.MergedDictionaries.Add(new ResourceDictionary
                            {
                                Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/Styles.xaml", UriKind.Absolute)
                            });
                        }
                    }
                    catch { }
                    action();
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
        public void TestPhScaleTool_ConversionMath_Ph7_And_Ph2()
        {
            RunInSta(() =>
            {
                // Instantiate the PhScaleTool
                var tool = new PhScaleTool();

                // Find child controls using public FrameworkElement.FindName method
                var txtHConc = tool.FindName("txtHConc") as TextBox;
                var sliderPhControl = tool.FindName("sliderPhControl") as Slider;

                Assert.NotNull(txtHConc);
                Assert.NotNull(sliderPhControl);

                // Test pH 7 conversion: 10^-7 mol/L
                txtHConc.Text = "1.000E-07";
                Assert.Equal(7.0, sliderPhControl.Value, 2);

                // Test pH 2 conversion: 10^-2 mol/L (0.01)
                txtHConc.Text = "0.01";
                Assert.Equal(2.0, sliderPhControl.Value, 2);

                // Test slider-to-textbox calculation: pH 3 -> 10^-3 mol/L (1.000E-03)
                sliderPhControl.Value = 3.0;
                Assert.Contains("1.000E-03", txtHConc.Text.Replace(',', '.'));
            });
        }

        [Fact]
        public void TestPhScaleTool_OutOfRangeWarnings()
        {
            RunInSta(() =>
            {
                var tool = new PhScaleTool();

                var txtHConc = tool.FindName("txtHConc") as TextBox;
                var phCalcResultPanel = tool.FindName("phCalcResultPanel") as StackPanel;

                Assert.NotNull(txtHConc);
                Assert.NotNull(phCalcResultPanel);

                // Test pH < 0: [H+] = 10 -> pH = -1
                txtHConc.Text = "10";
                
                bool foundWarning = false;
                foreach (var child in phCalcResultPanel.Children)
                {
                    if (child is Border border)
                    {
                        string text = GetBorderText(border);
                        if (text.Contains("ngoài khoảng đo chuẩn"))
                        {
                            foundWarning = true;
                            break;
                        }
                    }
                }
                var childrenInfo = string.Join("; ", phCalcResultPanel.Children.Cast<object>().Select(c => 
                    c is Border b ? GetBorderText(b) : c.GetType().Name));
                Assert.True(foundWarning, $"Out-of-range warning should be shown for pH < 0. Found children: {childrenInfo}");

                // Test pH > 14: [H+] = 1e-15 -> pH = 15
                txtHConc.Text = "1e-15";
                
                foundWarning = false;
                foreach (var child in phCalcResultPanel.Children)
                {
                    if (child is Border border)
                    {
                        string text = GetBorderText(border);
                        if (text.Contains("ngoài khoảng đo chuẩn"))
                        {
                            foundWarning = true;
                            break;
                        }
                    }
                }
                var childrenInfo2 = string.Join("; ", phCalcResultPanel.Children.Cast<object>().Select(c => 
                    c is Border b ? GetBorderText(b) : c.GetType().Name));
                Assert.True(foundWarning, $"Out-of-range warning should be shown for pH > 14. Found children: {childrenInfo2}");
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
    }
}
