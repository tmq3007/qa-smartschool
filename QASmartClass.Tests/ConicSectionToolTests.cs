using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.LearningTools.Views.Math;
using Xunit;

namespace QASmartClass.Tests
{
    public class ConicSectionToolTests
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
            Exception ex = null;
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

        private void InitializeAppAndResources()
        {
            var app = Application.Current;
            if (app == null)
            {
                try
                {
                    app = new Application();
                }
                catch { }
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
        }

        private void SetMode(ConicSectionTool tool, string modeName)
        {
            var modeEnum = typeof(ConicSectionTool).GetNestedType("Mode", BindingFlags.NonPublic);
            Assert.NotNull(modeEnum);
            var modeVal = Enum.Parse(modeEnum, modeName);
            var modeField = typeof(ConicSectionTool).GetField("_mode", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(modeField);
            modeField.SetValue(tool, modeVal);
        }

        private void TriggerCalc(ConicSectionTool tool)
        {
            var calcMethod = typeof(ConicSectionTool).GetMethod("Calc", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(calcMethod);
            calcMethod.Invoke(tool, null);
        }

        private List<string> GetResultTexts(ConicSectionTool tool)
        {
            var resultPanel = (StackPanel)tool.FindName("resultPanel");
            Assert.NotNull(resultPanel);
            var texts = new List<string>();
            foreach (var child in resultPanel.Children)
            {
                if (child is Border border)
                {
                    string txt = GetBorderText(border);
                    if (!string.IsNullOrEmpty(txt)) texts.Add(txt);
                }
            }
            return texts;
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
        public void TestConicSectionTool_Initialization()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new ConicSectionTool();
                Assert.NotNull(tool);
            });
        }

        [Fact]
        public void TestConicSectionTool_Ellipse_Valid()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new ConicSectionTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                SetMode(tool, "Ellipse");

                var txtEA = (TextBox)tool.FindName("txtEA");
                var txtEB = (TextBox)tool.FindName("txtEB");
                Assert.NotNull(txtEA);
                Assert.NotNull(txtEB);

                txtEA.Text = "5";
                txtEB.Text = "3";

                TriggerCalc(tool);

                var results = GetResultTexts(tool);
                Assert.NotEmpty(results);
                Assert.Contains(results, r => r.Contains("Phương trình chính tắc: x²/5² + y²/3² = 1"));
                Assert.Contains(results, r => r.Contains("a = 5"));
                Assert.Contains(results, r => r.Contains("b = 3"));
                Assert.Contains(results, r => r.Contains("c = "));
                Assert.Contains(results, r => r.Contains("Tiêu điểm:"));
                Assert.Contains(results, r => r.Contains("Tâm sai:"));
            });
        }

        [Fact]
        public void TestConicSectionTool_Ellipse_CircleDegeneration()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new ConicSectionTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                SetMode(tool, "Ellipse");

                var txtEA = (TextBox)tool.FindName("txtEA");
                var txtEB = (TextBox)tool.FindName("txtEB");
                Assert.NotNull(txtEA);
                Assert.NotNull(txtEB);

                txtEA.Text = "4";
                txtEB.Text = "4";

                TriggerCalc(tool);

                var results = GetResultTexts(tool);
                Assert.NotEmpty(results);
                Assert.Contains(results, r => r.Contains("Đường tròn (Trường hợp đặc biệt a = b): R = 4"));
                Assert.Contains(results, r => r.Contains("Phương trình thu gọn: x² + y² = 16"));
            });
        }

        [Fact]
        public void TestConicSectionTool_Ellipse_InvalidWarning()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new ConicSectionTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                SetMode(tool, "Ellipse");

                var txtEA = (TextBox)tool.FindName("txtEA");
                var txtEB = (TextBox)tool.FindName("txtEB");
                Assert.NotNull(txtEA);
                Assert.NotNull(txtEB);

                // a < b warning check
                txtEA.Text = "3";
                txtEB.Text = "5";

                TriggerCalc(tool);

                var results = GetResultTexts(tool);
                Assert.NotEmpty(results);
                Assert.Contains(results, r => r.Contains("Cảnh báo: Theo chương trình Toán 10 chính quy, Elip yêu cầu a > b > 0."));
            });
        }

        [Fact]
        public void TestConicSectionTool_Ellipse_BoundsWarning()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new ConicSectionTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                SetMode(tool, "Ellipse");

                var txtEA = (TextBox)tool.FindName("txtEA");
                var txtEB = (TextBox)tool.FindName("txtEB");
                Assert.NotNull(txtEA);
                Assert.NotNull(txtEB);

                // Check lower bound error
                txtEA.Text = "0.005";
                txtEB.Text = "3";
                TriggerCalc(tool);
                var results = GetResultTexts(tool);
                Assert.Contains(results, r => r.Contains("Lỗi: Các giá trị a, b phải nằm trong khoảng từ 0.01 đến 1000."));

                // Check upper bound error
                txtEA.Text = "1500";
                txtEB.Text = "3";
                TriggerCalc(tool);
                results = GetResultTexts(tool);
                Assert.Contains(results, r => r.Contains("Lỗi: Các giá trị a, b phải nằm trong khoảng từ 0.01 đến 1000."));
            });
        }

        [Fact]
        public void TestConicSectionTool_Ellipse_InvalidAlphabetic()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new ConicSectionTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                SetMode(tool, "Ellipse");

                var txtEA = (TextBox)tool.FindName("txtEA");
                var txtEB = (TextBox)tool.FindName("txtEB");
                Assert.NotNull(txtEA);
                Assert.NotNull(txtEB);

                txtEA.Text = "abc";
                txtEB.Text = "3";

                TriggerCalc(tool);

                var results = GetResultTexts(tool);
                Assert.NotEmpty(results);
                Assert.Contains(results, r => r.Contains("Lỗi: Các hệ số a, b phải là số thực dương"));
            });
        }

        [Fact]
        public void TestConicSectionTool_Hyperbola_Valid()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new ConicSectionTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                SetMode(tool, "Hyperbola");

                var txtHA = (TextBox)tool.FindName("txtHA");
                var txtHB = (TextBox)tool.FindName("txtHB");
                Assert.NotNull(txtHA);
                Assert.NotNull(txtHB);

                txtHA.Text = "3";
                txtHB.Text = "4";

                TriggerCalc(tool);

                var results = GetResultTexts(tool);
                Assert.NotEmpty(results);
                Assert.Contains(results, r => r.Contains("Phương trình chính tắc: x²/3² − y²/4² = 1"));
                Assert.Contains(results, r => r.Contains("a = 3"));
                Assert.Contains(results, r => r.Contains("b = 4"));
                Assert.Contains(results, r => r.Contains("c = √"));
                Assert.Contains(results, r => r.Contains("Tiệm cận:"));
            });
        }

        [Fact]
        public void TestConicSectionTool_Hyperbola_BoundsWarning()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new ConicSectionTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                SetMode(tool, "Hyperbola");

                var txtHA = (TextBox)tool.FindName("txtHA");
                var txtHB = (TextBox)tool.FindName("txtHB");
                Assert.NotNull(txtHA);
                Assert.NotNull(txtHB);

                // Lower bound check
                txtHA.Text = "0.005";
                txtHB.Text = "4";
                TriggerCalc(tool);
                var results = GetResultTexts(tool);
                Assert.Contains(results, r => r.Contains("Lỗi: Các giá trị a, b phải nằm trong khoảng từ 0.01 đến 1000."));

                // Upper bound check
                txtHA.Text = "1500";
                txtHB.Text = "4";
                TriggerCalc(tool);
                results = GetResultTexts(tool);
                Assert.Contains(results, r => r.Contains("Lỗi: Các giá trị a, b phải nằm trong khoảng từ 0.01 đến 1000."));
            });
        }

        [Fact]
        public void TestConicSectionTool_Parabola_Valid()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new ConicSectionTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                SetMode(tool, "Parabola");

                var txtPP = (TextBox)tool.FindName("txtPP");
                Assert.NotNull(txtPP);

                txtPP.Text = "2";

                TriggerCalc(tool);

                var results = GetResultTexts(tool);
                Assert.NotEmpty(results);
                Assert.Contains(results, r => r.Contains("Phương trình chính tắc: y² = 2·2·x = 4x"));
                Assert.Contains(results, r => r.Contains("p = 2"));
                Assert.Contains(results, r => r.Contains("Tiêu điểm: F(1, 0)"));
                Assert.Contains(results, r => r.Contains("Đường chuẩn: x = -1"));
            });
        }

        [Fact]
        public void TestConicSectionTool_Parabola_BoundsWarning()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new ConicSectionTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                SetMode(tool, "Parabola");

                var txtPP = (TextBox)tool.FindName("txtPP");
                Assert.NotNull(txtPP);

                // Lower bound check
                txtPP.Text = "0.005";
                TriggerCalc(tool);
                var results = GetResultTexts(tool);
                Assert.Contains(results, r => r.Contains("Lỗi: Tham số tiêu p phải nằm trong khoảng từ 0.01 đến 1000."));

                // Upper bound check
                txtPP.Text = "1500";
                TriggerCalc(tool);
                results = GetResultTexts(tool);
                Assert.Contains(results, r => r.Contains("Lỗi: Tham số tiêu p phải nằm trong khoảng từ 0.01 đến 1000."));
            });
        }

        [Fact]
        public void TestConicSectionTool_PresetAutoTab()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new ConicSectionTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                // Switch to Guide tab first
                var switchTabMethod = typeof(ConicSectionTool).GetMethod("SwitchToTab", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(switchTabMethod);
                switchTabMethod.Invoke(tool, new object[] { "guide" });

                var contentGuide = (ScrollViewer)tool.FindName("contentGuide");
                var contentCalc = (ScrollViewer)tool.FindName("contentCalc");
                Assert.NotNull(contentGuide);
                Assert.NotNull(contentCalc);
                Assert.Equal(Visibility.Visible, contentGuide.Visibility);
                Assert.Equal(Visibility.Collapsed, contentCalc.Visibility);

                // Now get the presetPanel children (chips) and trigger a MouseLeftButtonDown event on one
                var presetPanel = (WrapPanel)tool.FindName("presetPanel");
                Assert.NotNull(presetPanel);
                Assert.NotEmpty(presetPanel.Children);

                var firstPreset = (Border)presetPanel.Children[0];
                firstPreset.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(
                    System.Windows.Input.InputManager.Current.PrimaryMouseDevice,
                    0, System.Windows.Input.MouseButton.Left)
                {
                    RoutedEvent = UIElement.MouseLeftButtonDownEvent
                });

                // The tab should automatically switch back to Calc
                Assert.Equal(Visibility.Collapsed, contentGuide.Visibility);
                Assert.Equal(Visibility.Visible, contentCalc.Visibility);
            });
        }

        [Fact]
        public void TestConicSectionTool_GenerateReportText()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new ConicSectionTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                SetMode(tool, "Ellipse");

                var txtEA = (TextBox)tool.FindName("txtEA");
                var txtEB = (TextBox)tool.FindName("txtEB");
                Assert.NotNull(txtEA);
                Assert.NotNull(txtEB);

                txtEA.Text = "5";
                txtEB.Text = "3";

                var generateReportMethod = typeof(ConicSectionTool).GetMethod("GenerateReportText", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(generateReportMethod);

                string report = (string)generateReportMethod.Invoke(tool, null);
                Assert.NotNull(report);
                Assert.Contains("BÁO CÁO PHÂN TÍCH ĐƯỜNG CONIC", report);
                Assert.Contains("Loại đường conic: Elip", report);
                Assert.Contains("Nửa trục lớn a = 5", report);
                Assert.Contains("Nửa trục nhỏ b = 3", report);
                Assert.Contains("Tiêu cự 2c = 8", report);
                Assert.Contains("Tâm sai: e = 0.8", report);
                Assert.Contains("Đường chuẩn: x = ±6.25", report);
            });
        }
    }
}
