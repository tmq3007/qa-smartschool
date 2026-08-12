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
    public class InequalityToolTests
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

        private void SetMode(InequalityTool tool, string modeName)
        {
            var modeEnum = typeof(InequalityTool).GetNestedType("Mode", BindingFlags.NonPublic);
            Assert.NotNull(modeEnum);
            var modeVal = Enum.Parse(modeEnum, modeName);
            var modeField = typeof(InequalityTool).GetField("_mode", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(modeField);
            modeField.SetValue(tool, modeVal);

            // Trigger UpdateUI to synchronize panels
            var updateUiMethod = typeof(InequalityTool).GetMethod("UpdateUI", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(updateUiMethod);
            updateUiMethod.Invoke(tool, null);
        }

        private void TriggerSolveLinear(InequalityTool tool)
        {
            var method = typeof(InequalityTool).GetMethod("SolveLinear", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method.Invoke(tool, null);
        }

        private void TriggerSolveQuadratic(InequalityTool tool)
        {
            var method = typeof(InequalityTool).GetMethod("SolveQuadratic", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method.Invoke(tool, null);
        }

        private List<string> GetLinearResultTexts(InequalityTool tool)
        {
            var resultPanel = (StackPanel)tool.FindName("linearResultPanel");
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

        private List<string> GetQuadResultTexts(InequalityTool tool)
        {
            var resultPanel = (StackPanel)tool.FindName("quadraticResultPanel");
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
        public void TestLinearIneqNormalPositive()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new InequalityTool();
                SetMode(tool, "Linear");

                var txtA = (TextBox)tool.FindName("txtIneqA1");
                var txtB = (TextBox)tool.FindName("txtIneqB1");
                var cboSign = (ComboBox)tool.FindName("cboSign1");

                txtA.Text = "2";
                txtB.Text = "-6";
                cboSign.SelectedIndex = 0; // > 0

                TriggerSolveLinear(tool);

                var results = GetLinearResultTexts(tool);
                Assert.Contains("📝 BPT: 2x - 6 > 0", results);
                Assert.Contains("🔑 Bước 1: Chuyển vế tự do → 2x > 6", results);
                Assert.Contains("🔑 Bước 2: Chia hai vế cho a = 2 (a > 0 → giữ nguyên chiều)", results);
                Assert.Contains("✅ Kết luận nghiệm: x > 3", results);
                Assert.Contains("📐 Tập nghiệm: S = (3; +∞)", results);
            });
        }

        [Fact]
        public void TestLinearIneqNormalNegative()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new InequalityTool();
                SetMode(tool, "Linear");

                var txtA = (TextBox)tool.FindName("txtIneqA1");
                var txtB = (TextBox)tool.FindName("txtIneqB1");
                var cboSign = (ComboBox)tool.FindName("cboSign1");

                txtA.Text = "-3";
                txtB.Text = "9";
                cboSign.SelectedIndex = 3; // <= 0

                TriggerSolveLinear(tool);

                var results = GetLinearResultTexts(tool);
                Assert.Contains("📝 BPT: -3x + 9 ≤ 0", results);
                Assert.Contains("🔑 Bước 1: Chuyển vế tự do → -3x ≤ -9", results);
                Assert.Contains("🔑 Bước 2: Chia hai vế cho a = -3 (a < 0 → ĐỔI CHIỀU BPT)", results);
                Assert.Contains("⚠️ Hệ số a < 0 → Đã đổi chiều bất phương trình!", results);
                Assert.Contains("✅ Kết luận nghiệm: x ≥ 3", results);
                Assert.Contains("📐 Tập nghiệm: S = [3; +∞)", results);
            });
        }

        [Fact]
        public void TestLinearIneqZeroA()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new InequalityTool();
                SetMode(tool, "Linear");

                var txtA = (TextBox)tool.FindName("txtIneqA1");
                var txtB = (TextBox)tool.FindName("txtIneqB1");
                var cboSign = (ComboBox)tool.FindName("cboSign1");

                txtA.Text = "0";
                txtB.Text = "-5";
                cboSign.SelectedIndex = 2; // < 0

                TriggerSolveLinear(tool);

                var results = GetLinearResultTexts(tool);
                Assert.Contains("📝 BPT: -5 < 0", results);
                Assert.Contains("✅ Đúng với mọi x ∈ ℝ (vô số nghiệm)", results);
                Assert.Contains("📐 Tập nghiệm: S = ℝ", results);
            });
        }

        [Fact]
        public void TestQuadraticIneqNormal()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new InequalityTool();
                SetMode(tool, "Quadratic");

                var txtA = (TextBox)tool.FindName("txtIneqA2");
                var txtB = (TextBox)tool.FindName("txtIneqB2");
                var txtC = (TextBox)tool.FindName("txtIneqC2");
                var cboSign = (ComboBox)tool.FindName("cboSign2");

                txtA.Text = "1";
                txtB.Text = "-5";
                txtC.Text = "6";
                cboSign.SelectedIndex = 0; // > 0

                TriggerSolveQuadratic(tool);

                var results = GetQuadResultTexts(tool);
                Assert.Contains("📝 BPT: x² - 5x + 6 > 0", results);
                Assert.Contains("🔑 Bước 1: Tính Δ = b² − 4ac = (-5)² − 4·1·6 = 1", results);
                Assert.Contains("📊 Δ > 0 → Có hai nghiệm phân biệt: x₁ = 2, x₂ = 3", results);
                Assert.Contains("📐 Tập nghiệm: S = (-∞; 2) ∪ (3; +∞)", results);
            });
        }

        [Fact]
        public void TestQuadraticIneqDeltaLessZero()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new InequalityTool();
                SetMode(tool, "Quadratic");

                var txtA = (TextBox)tool.FindName("txtIneqA2");
                var txtB = (TextBox)tool.FindName("txtIneqB2");
                var txtC = (TextBox)tool.FindName("txtIneqC2");
                var cboSign = (ComboBox)tool.FindName("cboSign2");

                txtA.Text = "1";
                txtB.Text = "2";
                txtC.Text = "5";
                cboSign.SelectedIndex = 2; // < 0

                TriggerSolveQuadratic(tool);

                var results = GetQuadResultTexts(tool);
                Assert.Contains("📝 BPT: x² + 2x + 5 < 0", results);
                Assert.Contains("🔑 Bước 1: Tính Δ = b² − 4ac = 2² − 4·1·5 = -16", results);
                Assert.Contains("📊 Δ = -16 < 0 → Tam thức không đổi dấu (luôn cùng dấu a = 1)", results);
                Assert.Contains("❌ Vô nghiệm", results);
                Assert.Contains("📐 Tập nghiệm: S = ∅", results);
            });
        }

        [Fact]
        public void TestQuadraticIneqDeltaZero()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new InequalityTool();
                SetMode(tool, "Quadratic");

                var txtA = (TextBox)tool.FindName("txtIneqA2");
                var txtB = (TextBox)tool.FindName("txtIneqB2");
                var txtC = (TextBox)tool.FindName("txtIneqC2");
                var cboSign = (ComboBox)tool.FindName("cboSign2");

                txtA.Text = "1";
                txtB.Text = "-4";
                txtC.Text = "4";
                cboSign.SelectedIndex = 3; // <= 0

                TriggerSolveQuadratic(tool);

                var results = GetQuadResultTexts(tool);
                Assert.Contains("📝 BPT: x² - 4x + 4 ≤ 0", results);
                Assert.Contains("🔑 Bước 1: Tính Δ = b² − 4ac = (-4)² − 4·1·4 = 0", results);
                Assert.Contains("📊 Δ = 0 → Tam thức có nghiệm kép x₀ = −b/(2a) = 2", results);
                Assert.Contains("📐 Tập nghiệm: S = {2}", results);
            });
        }

        [Fact]
        public void TestQuadraticIneqZeroA()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new InequalityTool();
                SetMode(tool, "Quadratic");

                var txtA = (TextBox)tool.FindName("txtIneqA2");
                var txtB = (TextBox)tool.FindName("txtIneqB2");
                var txtC = (TextBox)tool.FindName("txtIneqC2");
                var cboSign = (ComboBox)tool.FindName("cboSign2");

                txtA.Text = "0";
                txtB.Text = "2";
                txtC.Text = "-6";
                cboSign.SelectedIndex = 0; // > 0

                TriggerSolveQuadratic(tool);

                var results = GetQuadResultTexts(tool);
                Assert.Contains("⚠️ Hệ số a = 0. Bất phương trình bậc hai trở thành bậc nhất!", results);
                Assert.Contains("📝 BPT: 2x - 6 > 0", results);
                Assert.Contains("🔑 Bước 1: Chuyển vế → 2x > 6", results);
                Assert.Contains("✅ Nghiệm: x > 3", results);
                Assert.Contains("📐 Tập nghiệm: S = (3; +∞)", results);
            });
        }
    }
}
