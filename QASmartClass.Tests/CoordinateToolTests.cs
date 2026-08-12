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
    public class CoordinateToolTests
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

        private void SetMode(CoordinateTool tool, string modeName)
        {
            var modeEnum = typeof(CoordinateTool).GetNestedType("Mode", BindingFlags.NonPublic);
            Assert.NotNull(modeEnum);
            var modeVal = Enum.Parse(modeEnum, modeName);
            var modeField = typeof(CoordinateTool).GetField("_mode", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(modeField);
            modeField.SetValue(tool, modeVal);
        }

        private void TriggerCalc(CoordinateTool tool)
        {
            var calcMethod = typeof(CoordinateTool).GetMethod("Calc", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(calcMethod);
            calcMethod.Invoke(tool, null);
        }

        private List<string> GetResultTexts(CoordinateTool tool)
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
            var method = typeof(CoordinateTool).GetMethod("FormatExactRoot", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Distance calculations dx, dy
            Assert.Equal("5", method.Invoke(null, new object[] { 3.0, 4.0 }));
            Assert.Equal("√2 ≈ 1.41421", method.Invoke(null, new object[] { 1.0, 1.0 }));
            Assert.Equal("2√2 ≈ 2.82843", method.Invoke(null, new object[] { 2.0, 2.0 }));
            Assert.Equal("0", method.Invoke(null, new object[] { 0.0, 0.0 }));

            // Test non-integer coordinates
            Assert.Equal("2.5", method.Invoke(null, new object[] { 1.5, 2.0 }));
        }

        [Fact]
        public void TestCalcTwoPointsNormal()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new CoordinateTool();
                SetMode(tool, "TwoPoints");

                var txtX1 = (TextBox)tool.FindName("txtX1");
                var txtY1 = (TextBox)tool.FindName("txtY1");
                var txtX2 = (TextBox)tool.FindName("txtX2");
                var txtY2 = (TextBox)tool.FindName("txtY2");

                txtX1.Text = "1";
                txtY1.Text = "2";
                txtX2.Text = "4";
                txtY2.Text = "6";

                TriggerCalc(tool);

                var results = GetResultTexts(tool);
                Assert.Contains("📍 A(1; 2), B(4; 6)", results);
                Assert.Contains("📏 Khoảng cách AB = √[(4−1)² + (6−2)²] = 5", results);
                Assert.Contains("⊕ Trung điểm M(2.5; 4)", results);
                Assert.Contains("📐 Hệ số góc k = (y₂−y₁)/(x₂−x₁) = 1.33333", results);
                Assert.Contains("📏 PT đường thẳng AB: y = 1.33333x + (0.666667)", results);
                Assert.Contains("📐 Góc với trục hoành Ox: α = 53.1301°", results);
                Assert.Contains("📝 Dạng tổng quát: 1.33333x + (−1)y + (0.666667) = 0", results);
            });
        }

        [Fact]
        public void TestCalcTwoPointsEdgeCases()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new CoordinateTool();
                SetMode(tool, "TwoPoints");

                var txtX1 = (TextBox)tool.FindName("txtX1");
                var txtY1 = (TextBox)tool.FindName("txtY1");
                var txtX2 = (TextBox)tool.FindName("txtX2");
                var txtY2 = (TextBox)tool.FindName("txtY2");

                // Case 1: Vertical line (x1 == x2)
                txtX1.Text = "3"; txtY1.Text = "2";
                txtX2.Text = "3"; txtY2.Text = "7";
                TriggerCalc(tool);
                var resultsVertical = GetResultTexts(tool);
                Assert.Contains("📐 Hệ số góc k: Không xác định (đường thẳng đứng)", resultsVertical);
                Assert.Contains("📐 Góc với trục hoành Ox: α = 90°", resultsVertical);
                Assert.Contains("📏 PT đường thẳng AB: x = 3", resultsVertical);
                Assert.Contains("📝 Dạng tổng quát: 1x + 0y + (-3) = 0", resultsVertical);

                // Case 2: Negative slope (k < 0) ensuring angle alpha is in [0, 180)
                txtX1.Text = "1"; txtY1.Text = "5";
                txtX2.Text = "3"; txtY2.Text = "3";
                TriggerCalc(tool);
                var resultsNegSlope = GetResultTexts(tool);
                Assert.Contains("📐 Hệ số góc k = (y₂−y₁)/(x₂−x₁) = -1", resultsNegSlope);
                Assert.Contains("📐 Góc với trục hoành Ox: α = 135°", resultsNegSlope); // positive angle!

                // Case 3: Overlapping points (A == B)
                txtX1.Text = "2"; txtY1.Text = "3";
                txtX2.Text = "2"; txtY2.Text = "3";
                TriggerCalc(tool);
                var resultsOverlap = GetResultTexts(tool);
                Assert.Contains("📏 Khoảng cách AB = √[(2−2)² + (3−3)²] = 0", resultsOverlap);
                Assert.Contains("📐 Hệ số góc k: Không xác định (hai điểm trùng nhau)", resultsOverlap);
                Assert.Contains("📏 PT đường thẳng AB: Không xác định", resultsOverlap);
            });
        }

        [Fact]
        public void TestCalcLineMode()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new CoordinateTool();
                SetMode(tool, "Line");

                var txtK = (TextBox)tool.FindName("txtK");
                var txtM = (TextBox)tool.FindName("txtM");

                // Case 1: Slanted line y = 2x - 1
                txtK.Text = "2";
                txtM.Text = "-1";
                TriggerCalc(tool);
                var results1 = GetResultTexts(tool);
                Assert.Contains("📏 PT đường thẳng: y = 2x + (-1)", results1);
                Assert.Contains("🔴 Giao Ox: (0.5; 0)", results1);
                Assert.Contains("🔵 Giao Oy: (0; -1)", results1);
                Assert.Contains("📐 Góc với trục hoành Ox: α = 63.4349°", results1);
                Assert.Contains("📈 Đường thẳng đi lên (k > 0)", results1);

                // Case 2: Horizontal line y = 3 (k = 0)
                txtK.Text = "0";
                txtM.Text = "3";
                TriggerCalc(tool);
                var results2 = GetResultTexts(tool);
                Assert.Contains("🔴 Song song Ox (không cắt)", results2);
                Assert.Contains("🔵 Giao Oy: (0; 3)", results2);
                Assert.Contains("📐 Góc với trục hoành Ox: α = 0°", results2);
                Assert.Contains("➡️ Đường nằm ngang (k = 0)", results2);
            });
        }

        [Fact]
        public void TestUXUpgrades()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new CoordinateTool();

                // 1. Coordinate warning trigger
                SetMode(tool, "TwoPoints");
                var txtX1 = (TextBox)tool.FindName("txtX1");
                txtX1.Text = "150";
                TriggerCalc(tool);
                var results = GetResultTexts(tool);
                Assert.Contains("⚠️ Tọa độ lớn (>100) có thể làm đồ thị khó quan sát.", results);

                // 2. Reset Click - TwoPoints Mode
                var resetMethod = typeof(CoordinateTool).GetMethod("Reset_Click", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(resetMethod);
                resetMethod.Invoke(tool, new object[] { null, null });

                var txtY1 = (TextBox)tool.FindName("txtY1");
                var txtX2 = (TextBox)tool.FindName("txtX2");
                var txtY2 = (TextBox)tool.FindName("txtY2");
                Assert.Equal("1", txtX1.Text);
                Assert.Equal("2", txtY1.Text);
                Assert.Equal("4", txtX2.Text);
                Assert.Equal("6", txtY2.Text);

                // 3. Reset Click - Line Mode
                SetMode(tool, "Line");
                var txtK = (TextBox)tool.FindName("txtK");
                var txtM = (TextBox)tool.FindName("txtM");
                txtK.Text = "-5";
                txtM.Text = "10";
                resetMethod.Invoke(tool, new object[] { null, null });
                Assert.Equal("2", txtK.Text);
                Assert.Equal("-1", txtM.Text);
            });
        }
    }
}
