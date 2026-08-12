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
    public class VectorToolTests
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

        private void SetMode(VectorTool tool, string modeName)
        {
            var modeEnum = typeof(VectorTool).GetNestedType("Mode", BindingFlags.NonPublic);
            Assert.NotNull(modeEnum);
            var modeVal = Enum.Parse(modeEnum, modeName);
            var modeField = typeof(VectorTool).GetField("_mode", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(modeField);
            modeField.SetValue(tool, modeVal);
        }

        private void TriggerCalc(VectorTool tool)
        {
            var calcMethod = typeof(VectorTool).GetMethod("Calc", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(calcMethod);
            calcMethod.Invoke(tool, null);
        }

        private List<string> GetResultTexts(VectorTool tool)
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
        public void TestFormatExactRoot()
        {
            var method = typeof(VectorTool).GetMethod("FormatExactRoot", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Test integer square root values
            Assert.Equal("5", method.Invoke(null, new object[] { 3.0, 4.0 }));
            Assert.Equal("√2 ≈ 1.41421", method.Invoke(null, new object[] { 1.0, 1.0 }));
            Assert.Equal("2√2 ≈ 2.82843", method.Invoke(null, new object[] { 2.0, 2.0 }));
            Assert.Equal("0", method.Invoke(null, new object[] { 0.0, 0.0 }));

            // Test non-integer values
            Assert.Equal("2.5", method.Invoke(null, new object[] { 1.5, 2.0 }));
        }

        [Fact]
        public void TestCalcTwoVecNormal()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new VectorTool();
                SetMode(tool, "TwoVec");

                // Set inputs: a = (3, 4), b = (-1, 2)
                var txtX1 = (TextBox)tool.FindName("txtX1");
                var txtY1 = (TextBox)tool.FindName("txtY1");
                var txtX2 = (TextBox)tool.FindName("txtX2");
                var txtY2 = (TextBox)tool.FindName("txtY2");

                txtX1.Text = "3";
                txtY1.Text = "4";
                txtX2.Text = "-1";
                txtY2.Text = "2";

                TriggerCalc(tool);

                var results = GetResultTexts(tool);
                Assert.Contains("$\\vec{a} = (3, 4)$", results);
                Assert.Contains("$\\vec{b} = (-1, 2)$", results);
                Assert.Contains("$\\vec{a} + \\vec{b} = (2, 6)$", results);
                Assert.Contains("$\\vec{a} - \\vec{b} = (4, 2)$", results);
                Assert.Contains("$|\\vec{a}| = 5$", results);
                Assert.Contains("$|\\vec{b}| = \\sqrt{5} \\approx 2.23607$", results);
                Assert.Contains("$\\vec{a} \\cdot \\vec{b} = 5$", results);
                Assert.Contains("📌 Quan hệ: Không cùng phương, không vuông góc", results);
            });
        }

        [Fact]
        public void TestCalcTwoVecSpecialRelations()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new VectorTool();
                SetMode(tool, "TwoVec");

                var txtX1 = (TextBox)tool.FindName("txtX1");
                var txtY1 = (TextBox)tool.FindName("txtY1");
                var txtX2 = (TextBox)tool.FindName("txtX2");
                var txtY2 = (TextBox)tool.FindName("txtY2");

                // Case 1: Collinear (cùng hướng)
                txtX1.Text = "2"; txtY1.Text = "4";
                txtX2.Text = "1"; txtY2.Text = "2";
                TriggerCalc(tool);
                Assert.Contains("📌 Quan hệ: Cùng phương (cùng hướng)  $x_1y_2 - y_1x_2 = 0$", GetResultTexts(tool));

                // Case 2: Perpendicular
                txtX1.Text = "1"; txtY1.Text = "0";
                txtX2.Text = "0"; txtY2.Text = "1";
                TriggerCalc(tool);
                Assert.Contains("📌 Quan hệ: Vuông góc  $\\vec{a} \\cdot \\vec{b} = 0$", GetResultTexts(tool));

                // Case 3: Zero Vector
                txtX1.Text = "0"; txtY1.Text = "0";
                txtX2.Text = "3"; txtY2.Text = "4";
                TriggerCalc(tool);
                Assert.Contains("📌 Quan hệ: Cùng phương và vuông góc với mọi vectơ (do có vectơ không)", GetResultTexts(tool));
            });
        }

        [Fact]
        public void TestCalcScalarMode()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new VectorTool();
                SetMode(tool, "Scalar");

                var txtSX = (TextBox)tool.FindName("txtSX");
                var txtSY = (TextBox)tool.FindName("txtSY");
                var txtK = (TextBox)tool.FindName("txtK");

                txtSX.Text = "3";
                txtSY.Text = "4";
                txtK.Text = "-1.5";

                TriggerCalc(tool);

                var results = GetResultTexts(tool);
                Assert.Contains("$\\vec{a} = (3, 4)$", results);
                Assert.Contains("$|\\vec{a}| = 5$", results);
                Assert.Contains("$-1.5\\cdot\\vec{a} = (-4.5, -6)$", results);
                Assert.Contains("$|-1.5\\cdot\\vec{a}| = 7.5$", results);
                Assert.Contains("Góc với trục hoành Ox = $0.927295\\text{ rad} = 53.13^\\circ$", results);
            });
        }

        [Fact]
        public void TestPhase2Features()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new VectorTool();
                
                // 1. Test coordinate warning for > 100
                SetMode(tool, "TwoVec");
                var txtX1 = (TextBox)tool.FindName("txtX1");
                txtX1.Text = "150";
                TriggerCalc(tool);
                var results = GetResultTexts(tool);
                Assert.Contains("⚠️ Tọa độ lớn (>100) có thể làm đồ thị khó quan sát.", results);

                // 2. Test Reset_Click in TwoVec mode
                var resetMethod = typeof(VectorTool).GetMethod("Reset_Click", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(resetMethod);
                resetMethod.Invoke(tool, new object[] { null, null });
                
                var txtY1 = (TextBox)tool.FindName("txtY1");
                var txtX2 = (TextBox)tool.FindName("txtX2");
                var txtY2 = (TextBox)tool.FindName("txtY2");
                Assert.Equal("3", txtX1.Text);
                Assert.Equal("4", txtY1.Text);
                Assert.Equal("-1", txtX2.Text);
                Assert.Equal("2", txtY2.Text);

                // 3. Test Reset_Click in Scalar mode
                SetMode(tool, "Scalar");
                var txtSX = (TextBox)tool.FindName("txtSX");
                var txtSY = (TextBox)tool.FindName("txtSY");
                var txtK = (TextBox)tool.FindName("txtK");
                txtSX.Text = "10";
                txtSY.Text = "20";
                txtK.Text = "5";
                
                resetMethod.Invoke(tool, new object[] { null, null });
                Assert.Equal("3", txtSX.Text);
                Assert.Equal("4", txtSY.Text);
                Assert.Equal("2", txtK.Text);
            });
        }
    }
}
