using Xunit;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using System.Collections.Generic;
using QASmartClass.Classroom.Views;

namespace QASmartClass.Tests
{
    public class V90StemToolsUpgradeTests
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

        [Fact]
        public void Test_EvalScientific_SinInDegrees()
        {
            RunOnStaThread(() =>
            {
                var page = new StemToolsPage();
                var chkDegMode = (CheckBox)page.FindName("chkDegMode");
                Assert.NotNull(chkDegMode);
                chkDegMode.IsChecked = true; // Degrees mode

                var method = typeof(StemToolsPage).GetMethod("EvalScientific", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                double result = (double)method.Invoke(page, new object[] { "Sin(30)" });
                Assert.InRange(result, 0.4999, 0.5001);
            });
        }

        [Fact]
        public void Test_EvalScientific_SinInRadians()
        {
            RunOnStaThread(() =>
            {
                var page = new StemToolsPage();
                var chkDegMode = (CheckBox)page.FindName("chkDegMode");
                Assert.NotNull(chkDegMode);
                chkDegMode.IsChecked = false; // Radians mode

                var method = typeof(StemToolsPage).GetMethod("EvalScientific", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                double result = (double)method.Invoke(page, new object[] { "Sin(3.14159265359 / 6)" });
                Assert.InRange(result, 0.4999, 0.5001);
            });
        }

        [Fact]
        public void Test_EvalScientific_ParenthesizedPower()
        {
            RunOnStaThread(() =>
            {
                var page = new StemToolsPage();
                var method = typeof(StemToolsPage).GetMethod("EvalScientific", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                double result1 = (double)method.Invoke(page, new object[] { "(2)^3" });
                Assert.Equal(8.0, result1);

                double result2 = (double)method.Invoke(page, new object[] { "(2+3)^(1+1)" });
                Assert.Equal(25.0, result2);
            });
        }

        [Fact]
        public void Test_ParseCompound_HydratedSalt()
        {
            var method = typeof(StemToolsPage).GetMethod("ParseCompound", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // CuSO4.5H2O -> Cu=1, S=1, O=4 + 5*1 = 9, H=5*2 = 10
            var result = (Dictionary<string, int>)method.Invoke(null, new object[] { "CuSO4.5H2O" });
            Assert.NotNull(result);
            Assert.Equal(1, result["Cu"]);
            Assert.Equal(1, result["S"]);
            Assert.Equal(9, result["O"]);
            Assert.Equal(10, result["H"]);
        }

        [Fact]
        public void Test_ChemicalEquationBalancing_Gauss()
        {
            RunOnStaThread(() =>
            {
                var page = new StemToolsPage();
                
                // Let's get txtChemEquation
                var txtEquation = (TextBox)page.FindName("txtChemEquation");
                Assert.NotNull(txtEquation);
                txtEquation.Text = "KMnO4 + HCl -> KCl + MnCl2 + Cl2 + H2O";

                // Invoke BalanceChem_Click
                var method = typeof(StemToolsPage).GetMethod("BalanceChem_Click", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                method.Invoke(page, new object[] { page, new RoutedEventArgs() });

                // Get txtChemResult
                var txtResult = (TextBlock)page.FindName("txtChemResult");
                Assert.NotNull(txtResult);

                // Expected: 2KMnO4 + 16HCl  →  2KCl + 2MnCl2 + 5Cl2 + 8H2O
                string expected = "2KMnO4 + 16HCl  →  2KCl + 2MnCl2 + 5Cl2 + 8H2O";
                string actual = txtResult.Text.Trim();
                Assert.Equal(expected, actual);
            });
        }

        [Fact]
        public void Test_BoxPlot_IdenticalValues_ShouldDrawSingleLabel()
        {
            RunOnStaThread(() =>
            {
                var page = new StemToolsPage();
                var cboChartType = page.FindName("cboChartType") as ComboBox;
                Assert.NotNull(cboChartType);
                cboChartType.SelectedIndex = 3; // Box plot

                var chkShowValues = page.FindName("chkShowValues") as CheckBox;
                if (chkShowValues != null)
                {
                    chkShowValues.IsChecked = true;
                }

                var drawChartMethod = typeof(StemToolsPage).GetMethod("DrawChart",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(drawChartMethod);

                var names = new List<string> { "An", "Bình", "Châu" };
                var values = new List<double> { 10.0, 10.0, 10.0 };

                drawChartMethod.Invoke(page, new object[] { names, values });

                var canvas = page.FindName("chartCanvas") as Canvas;
                Assert.NotNull(canvas);

                // Check minLabel exists and has the combined text
                var minLabel = page.FindName("minLabel") as TextBlock;
                Assert.NotNull(minLabel);
                Assert.Equal("Min = Q1 = Trung vị = Q3 = Max = 10", minLabel.Text);

                // Check that other labels do not exist (unregistered)
                Assert.Null(page.FindName("q1Label"));
                Assert.Null(page.FindName("medLabel"));
                Assert.Null(page.FindName("q3Label"));
                Assert.Null(page.FindName("maxLabel"));
            });
        }

        [Fact]
        public void Test_Graph_FractionCoefficients_HtmlTemplate()
        {
            var method = typeof(QASmartClass.LearningTools.Views.Math.GraphWindow).GetMethod("BuildHtml",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            string html = (string)method.Invoke(null, new object[] { "calc.setExpression({id:'f1', latex:'y=(1/2)x^2'});" });
            Assert.NotNull(html);
            // Verify that the template contains the replacement function for fractions
            Assert.Contains("latex = latex.replace(/([-+]?)\\(([-+]?[0-9]+)\\/([0-9]+)\\)/g", html);
        }

        [Fact]
        public void Test_Graph_BoxPlot_DotPlot_HtmlTemplate()
        {
            var method = typeof(QASmartClass.LearningTools.Views.Math.GraphWindow).GetMethod("BuildHtml",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            string html = (string)method.Invoke(null, new object[] { "calc.setExpression({id:'dots', latex:'dotplot(L)'});" });
            Assert.NotNull(html);
            // Verify that the template contains the boxplot and dotplot handlers
            Assert.Contains("jsGetMedian", html);
            Assert.Contains("jsGetQuartiles", html);
            Assert.Contains("boxplot(L,", html);
            Assert.Contains("dotplot(L)", html);
        }

        [Fact]
        public void Test_FormatUiNumber_VietnameseStyle()
        {
            var method = typeof(QASmartClass.LearningTools.Views.Math.GraphWindow).GetMethod("FormatUiNumber",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);

            string result1 = (string)method.Invoke(null, new object[] { 1.5, "G6" });
            Assert.Equal("1,5", result1);

            string result2 = (string)method.Invoke(null, new object[] { -3.25, "G6" });
            Assert.Equal("−3,25", result2); // Sử dụng ký tự \u2212
        }

        [Fact]
        public void Test_EvaluateLatex_Offline_Complex()
        {
            var method = typeof(QASmartClass.LearningTools.Views.Math.GraphWindow).GetMethod("BuildHtml",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);

            string html = (string)method.Invoke(null, new object[] { "calc.setExpression({id:'f1', latex:'\\floor(x)'});" });
            Assert.NotNull(html);
            Assert.Contains("Math.floor", html);
            Assert.Contains("Math.abs", html);
            Assert.Contains("Math.log10", html);
            Assert.Contains("Math.E", html);
            Assert.Contains("Math.log($2)/Math.log($1)", html);
        }

        [Fact]
        public void Test_UnitConverter_Presets()
        {
            RunOnStaThread(() =>
            {
                var page = new StemToolsPage();

                var method = typeof(StemToolsPage).GetMethod("Preset_BodyTemp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(method);

                method.Invoke(page, new object[] { page, new RoutedEventArgs() });

                var txtToValue = (TextBlock)page.FindName("txtToValue");
                Assert.NotNull(txtToValue);
                
                string txt = txtToValue.Text.Replace(",", ".");
                Assert.Equal("98.6", txt);
            });
        }

        [Fact]
        public void Test_UnitConverter_NewPresets()
        {
            RunOnStaThread(() =>
            {
                var page = new StemToolsPage();

                var method1 = typeof(StemToolsPage).GetMethod("Preset_BakingTemp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(method1);
                method1.Invoke(page, new object[] { page, new RoutedEventArgs() });

                var txtToValue = (TextBlock)page.FindName("txtToValue");
                Assert.NotNull(txtToValue);
                // 350 Fahrenheit ≈ 176.67 Celsius
                double val1 = double.Parse(txtToValue.Text.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                Assert.InRange(val1, 176.6, 176.7);

                var method2 = typeof(StemToolsPage).GetMethod("Preset_LiquidNitrogen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(method2);
                method2.Invoke(page, new object[] { page, new RoutedEventArgs() });

                // -196 Celsius ≈ 77.15 Kelvin
                double val2 = double.Parse(txtToValue.Text.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                Assert.InRange(val2, 77.1, 77.2);
            });
        }

        [Fact]
        public void Test_Quadratic_Presets()
        {
            RunOnStaThread(() =>
            {
                var page = new StemToolsPage();
                var method = typeof(StemToolsPage).GetMethod("Preset_QuadNemXien", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(page, new object[] { page, new RoutedEventArgs() });

                var txtCoeffA = (TextBox)page.FindName("txtCoeffA");
                var txtCoeffB = (TextBox)page.FindName("txtCoeffB");
                var txtCoeffC = (TextBox)page.FindName("txtCoeffC");
                var txtQuadPresetTitle = (TextBlock)page.FindName("txtQuadPresetTitle");
                var txtQuadPresetDesc = (TextBlock)page.FindName("txtQuadPresetDesc");

                Assert.Equal("-0.05", txtCoeffA.Text);
                Assert.Equal("1", txtCoeffB.Text);
                Assert.Equal("0", txtCoeffC.Text);
                Assert.Equal("Quỹ đạo ném xiên: y = −0,05x² + x", txtQuadPresetTitle.Text);
                Assert.Contains("x = 20m", txtQuadPresetDesc.Text);
            });
        }

        [Fact]
        public void Test_Stopwatch_Presets()
        {
            RunOnStaThread(() =>
            {
                var page = new StemToolsPage();
                var method = typeof(StemToolsPage).GetMethod("Preset_FreeFall2m", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(page, new object[] { page, new RoutedEventArgs() });

                var txtStopwatchPresetTitle = (TextBlock)page.FindName("txtStopwatchPresetTitle");
                var txtStopwatchPresetDesc = (TextBlock)page.FindName("txtStopwatchPresetDesc");

                Assert.Equal("Rơi tự do (h = 2m)", txtStopwatchPresetTitle.Text);
                Assert.Contains("t ≈ 0,64 giây", txtStopwatchPresetDesc.Text);
            });
        }

        [Fact]
        public void Test_EvalScientific_LowerCaseFunctions()
        {
            RunOnStaThread(() =>
            {
                var page = new StemToolsPage();
                var chkDegMode = (CheckBox)page.FindName("chkDegMode");
                Assert.NotNull(chkDegMode);
                chkDegMode.IsChecked = true; // Degrees mode

                var method = typeof(StemToolsPage).GetMethod("EvalScientific", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                double result = (double)method.Invoke(page, new object[] { "sin(30) + cos(60)" });
                Assert.InRange(result, 0.9999, 1.0001);
            });
        }

        [Fact]
        public void Test_EvaluateExpression_VietnameseCulture()
        {
            RunOnStaThread(() =>
            {
                var originalCulture = Thread.CurrentThread.CurrentCulture;
                var originalUiCulture = Thread.CurrentThread.CurrentUICulture;
                try
                {
                    var viCulture = new System.Globalization.CultureInfo("vi-VN");
                    Thread.CurrentThread.CurrentCulture = viCulture;
                    Thread.CurrentThread.CurrentUICulture = viCulture;

                    var page = new StemToolsPage();
                    var txtCalcInput = (TextBox)page.FindName("txtCalcInput");
                    var txtCalcDisplay = (TextBlock)page.FindName("txtCalcDisplay");
                    Assert.NotNull(txtCalcInput);
                    Assert.NotNull(txtCalcDisplay);

                    txtCalcInput.Text = "1.5 * 2";

                    var method = typeof(StemToolsPage).GetMethod("EvaluateExpression", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(method);
                    method.Invoke(page, null);

                    Assert.Equal("3", txtCalcDisplay.Text);
                }
                finally
                {
                    Thread.CurrentThread.CurrentCulture = originalCulture;
                    Thread.CurrentThread.CurrentUICulture = originalUiCulture;
                }
            });
        }

        [Fact]
        public void Test_QuadraticSolver_VietnameseFractionCoefficients()
        {
            RunOnStaThread(() =>
            {
                var page = new StemToolsPage();
                var txtCoeffA = (TextBox)page.FindName("txtCoeffA");
                var txtCoeffB = (TextBox)page.FindName("txtCoeffB");
                var txtCoeffC = (TextBox)page.FindName("txtCoeffC");
                var txtQuadResult = (TextBlock)page.FindName("txtQuadResult");

                Assert.NotNull(txtCoeffA);
                Assert.NotNull(txtCoeffB);
                Assert.NotNull(txtCoeffC);
                Assert.NotNull(txtQuadResult);

                // a = 1/2, b = 2,5, c = 3
                txtCoeffA.Text = "1/2";
                txtCoeffB.Text = "2,5";
                txtCoeffC.Text = "3";

                var method = typeof(StemToolsPage).GetMethod("SolveQuadratic", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method.Invoke(page, null);

                // equation: 0.5x^2 + 2.5x + 3 = 0 -> roots are x = -2, x = -3
                string resultText = txtQuadResult.Text;
                Assert.Contains("−2", resultText);
                Assert.Contains("−3", resultText);
            });
        }
    }
}

