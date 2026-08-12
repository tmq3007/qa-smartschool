using QASmartClass.LearningTools.Models;
using System;
using QASmartClass.LearningTools.Helpers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class QuadraticTool : BaseToolControl
    {
        private readonly List<string> _history = new();

        public QuadraticTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Tính toán & Đồ thị" : "Calculator & Graph";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn sử dụng" : "User Guide";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";

                if (sideMenu != null)
                {
                    sideMenu.SelectionChanged += SideMenu_SelectionChanged;
                    sideMenu.SelectedIndex = 0;
                }

                CopyMissingImagesHelper();
                BuildPresets();
                BuildTheory();
                LoadHistory();
                SolveQuadratic();
                RenderHistory();
                LoadPracticalApps();

                // Bàn phím số mini cho màn hình tương tác
                TouchNumPad.Attach(txtCoeffA, step: 1, enableMathKeys: true);
                TouchNumPad.Attach(txtCoeffB, step: 1, enableMathKeys: true);
                TouchNumPad.Attach(txtCoeffC, step: 1, enableMathKeys: true);

                // Show submit button if running inside StudentShell
                var win = Window.GetWindow(this);
                if (win != null && win.GetType().Name == "StudentShell")
                {
                    btnSubmit.Visibility = Visibility.Visible;
                }
            };
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ParsingHelper.TryParseDouble(txtCoeffA.Text, out double a) || a == 0) return;
                if (!ParsingHelper.TryParseDouble(txtCoeffB.Text, out double b)) return;
                if (!ParsingHelper.TryParseDouble(txtCoeffC.Text, out double c)) return;

                string equation = $"{a}x² + ({b})x + ({c}) = 0";
                string result = txtQuadResult.Text.Replace("\n", ", ");
                string data = $"PT: {equation} -> Kết quả: {result}";

                var win = Window.GetWindow(this);
                if (win != null && win.GetType().Name == "StudentShell")
                {
                    var method = win.GetType().GetMethod("SendToolSubmission");
                    if (method != null)
                    {
                        method.Invoke(win, new object[] { "quadratic", data });
                        MessageBox.Show("Đã gửi phương trình và nghiệm của bạn lên máy giáo viên thành công!", "Nộp bài giải", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nộp bài: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  PRESETS — Ví dụ nhanh
        // ═══════════════════════════════════════════════════════════

        private void BuildPresets()
        {
            if (presetPanel == null) return;
            presetPanel.Children.Clear();

            var presets = new (string Label, double A, double B, double C, string Desc)[]
            {
                ("Δ > 0",     1,  -5,   6,  "2 nghiệm phân biệt"),
                ("Δ = 0",     1,  -6,   9,  "Nghiệm kép"),
                ("Δ < 0",     1,   2,   5,  "Vô nghiệm thực"),
                ("a = -1",   -1,   4,  -3,  "Parabol quay xuống"),
                ("Vieta",     1,  -7,  12,  "x₁+x₂=7, x₁·x₂=12"),
                ("Lớn",       2, -10,  12,  "Hệ số lớn"),
                ("Thập phân", 0.5, -3,  2,  "Hệ số thập phân"),
            };

            var presetColor = DS.ResultSuccess; // #2E7D32
            foreach (var (label, a, b, c, desc) in presets)
            {
                double ca = a, cb = b, cc = c;
                var btn = UI.PresetButton($"📌 {label}", presetColor, () =>
                {
                    txtCoeffA.Text = ca.ToString();
                    txtCoeffB.Text = cb.ToString();
                    txtCoeffC.Text = cc.ToString();
                }, presetPanel);
                btn.ToolTip = $"{a}x² + ({b})x + ({c}) = 0 — {desc}";
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  SOLVER — Giải phương trình bậc 2
        // ═══════════════════════════════════════════════════════════

        private void SolveQuadratic_Changed(object sender, RoutedEventArgs e)
        {
            SolveQuadratic();
        }

        private void ClearAllResults()
        {
            if (txtDelta != null) txtDelta.Text = "Δ = ?";
            if (txtFactored != null) txtFactored.Text = "";
            if (txtVertex != null) txtVertex.Text = "";
            ClearSteps();
        }

        private void SolveQuadratic()
        {
            try
            {
                if (txtCoeffA == null || txtCoeffB == null || txtCoeffC == null) return;
                if (txtDelta == null || txtQuadResult == null) return;

                if (!ParsingHelper.TryParseDouble(txtCoeffA.Text, out double a) || System.Math.Abs(a) < 1e-9)
                {
                    txtQuadResult.Text = "a phải ≠ 0";
                    ClearAllResults();
                    return;
                }
                if (!ParsingHelper.TryParseDouble(txtCoeffB.Text, out double b))
                {
                    txtQuadResult.Text = "b không hợp lệ";
                    ClearAllResults();
                    return;
                }
                if (!ParsingHelper.TryParseDouble(txtCoeffC.Text, out double c))
                {
                    txtQuadResult.Text = "c không hợp lệ";
                    ClearAllResults();
                    return;
                }

                double delta = b * b - 4 * a * c;
                txtDelta.Text = $"Δ = ({FormatVietnamese(b)})² − 4·({FormatVietnamese(a)})·({FormatVietnamese(c)}) = {FormatVietnamese(delta, "G8")}";

                // ── Vertex ──
                double xV = -b / (2 * a);
                double yV = a * xV * xV + b * xV + c;
                if (xV == 0.0) xV = 0.0;
                if (yV == 0.0) yV = 0.0;
                if (txtVertex != null)
                    txtVertex.Text = $"I({FormatVietnamese(xV, "G6")}; {FormatVietnamese(yV, "G6")})";

                string historyEntry;

                if (delta > 0)
                {
                    double x1 = (-b + System.Math.Sqrt(delta)) / (2 * a);
                    double x2 = (-b - System.Math.Sqrt(delta)) / (2 * a);
                    if (x1 == 0.0) x1 = 0.0;
                    if (x2 == 0.0) x2 = 0.0;
                    txtQuadResult.Text = $"x₁ = {FormatVietnamese(x1, "G8")}\nx₂ = {FormatVietnamese(x2, "G8")}";

                    SetDeltaStyle("positive");

                    // Factored form
                    if (txtFactored != null)
                        txtFactored.Text = $"{FormatCoeff(a)}{FormatFactor(x1)}{FormatFactor(x2)}";

                    BuildSteps(a, b, c, delta, x1, x2);
                    historyEntry = $"{FormatEq(a, b, c)} → x₁={FormatVietnamese(x1, "G6")}, x₂={FormatVietnamese(x2, "G6")}";
                }
                else if (delta == 0)
                {
                    double x = -b / (2 * a);
                    if (x == 0.0) x = 0.0;
                    txtQuadResult.Text = $"x₁ = x₂ = {FormatVietnamese(x, "G8")}  (nghiệm kép)";

                    SetDeltaStyle("zero");

                    if (txtFactored != null)
                    {
                        if (x == 0)
                            txtFactored.Text = $"{FormatCoeff(a)}x²";
                        else
                            txtFactored.Text = $"{FormatCoeff(a)}{FormatFactor(x)}²";
                    }

                    BuildSteps(a, b, c, delta, x, x);
                    historyEntry = $"{FormatEq(a, b, c)} → x={FormatVietnamese(x, "G6")} (kép)";
                }
                else
                {
                    double real = -b / (2 * a);
                    double imag = System.Math.Sqrt(-delta) / (2 * a);
                    imag = System.Math.Abs(imag);
                    if (real == 0.0) real = 0.0;
                    if (imag == 0.0) imag = 0.0;

                    bool showComplex = chkShowComplex != null && chkShowComplex.IsChecked == true;

                    if (showComplex)
                    {
                        txtQuadResult.Text = $"x₁ = {FormatVietnamese(real, "G6")} + {FormatVietnamese(imag, "G6")}i\nx₂ = {FormatVietnamese(real, "G6")} − {FormatVietnamese(imag, "G6")}i";
                        if (txtFactored != null)
                            txtFactored.Text = "Vô nghiệm thực — PT có 2 nghiệm phức liên hợp";
                    }
                    else
                    {
                        txtQuadResult.Text = "Vô nghiệm thực";
                        if (txtFactored != null)
                            txtFactored.Text = "Không có phân tích nhân tử trên tập số thực";
                    }

                    SetDeltaStyle("negative");

                    BuildSteps(a, b, c, delta, double.NaN, double.NaN);
                    historyEntry = $"{FormatEq(a, b, c)} → Vô nghiệm thực";
                }

                // Vieta
                // (included in steps)

                // History
                if (!_history.Contains(historyEntry))
                {
                    _history.Insert(0, historyEntry);
                    if (_history.Count > 15) _history.RemoveAt(15);
                    RenderHistory();
                    SaveHistory();
                }
            }
            catch { }
        }

        private void SetDeltaStyle(string type)
        {
            if (deltaCard == null || resultCard == null) return;

            switch (type)
            {
                case "positive":
                    deltaCard.Background = new SolidColorBrush(Color.FromRgb(241, 248, 233));
                    resultCard.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    txtDelta.Foreground = new SolidColorBrush(Color.FromRgb(85, 139, 47));
                    txtQuadResult.Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32));
                    break;
                case "zero":
                    deltaCard.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                    resultCard.Background = new SolidColorBrush(Color.FromRgb(255, 248, 225));
                    txtDelta.Foreground = new SolidColorBrush(Color.FromRgb(245, 127, 23));
                    txtQuadResult.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0));
                    break;
                case "negative":
                    deltaCard.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                    resultCard.Background = new SolidColorBrush(Color.FromRgb(252, 228, 236));
                    txtDelta.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                    txtQuadResult.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  STEPS — Các bước giải chi tiết
        // ═══════════════════════════════════════════════════════════

        private void ClearSteps()
        {
            if (stepsPanel != null) stepsPanel.Children.Clear();
        }

        private void BuildSteps(double a, double b, double c, double delta, double x1, double x2)
        {
            if (stepsPanel == null) return;
            stepsPanel.Children.Clear();

            var steps = new List<(string Step, string Detail, string Color)>
            {
                ("Bước 1: Xác định hệ số",
                 $"a = {FormatVietnamese(a)},  b = {FormatVietnamese(b)},  c = {FormatVietnamese(c)}",
                 "#1565C0"),

                ("Bước 2: Tính biệt thức Δ",
                 $"Δ = b² − 4ac = ({FormatVietnamese(b)})² − 4·({FormatVietnamese(a)})·({FormatVietnamese(c)})\nΔ = {FormatVietnamese(b * b)} − {FormatVietnamese(4 * a * c)} = {FormatVietnamese(delta)}",
                 "#7B1FA2"),
            };

            if (delta > 0)
            {
                steps.Add(("Bước 3: Δ > 0 → 2 nghiệm phân biệt",
                    $"x₁ = (−b + √Δ) / 2a = (−({FormatVietnamese(b)}) + √{FormatVietnamese(delta, "G6")}) / (2·{FormatVietnamese(a)})\nx₁ = {FormatVietnamese(x1)}\n\n" +
                    $"x₂ = (−b − √Δ) / 2a = (−({FormatVietnamese(b)}) − √{FormatVietnamese(delta, "G6")}) / (2·{FormatVietnamese(a)})\nx₂ = {FormatVietnamese(x2)}",
                    "#2E7D32"));

                steps.Add(("Bước 4: Kiểm tra Vieta",
                    $"x₁ + x₂ = {FormatVietnamese(x1, "G6")} + {FormatVietnamese(x2, "G6")} = {FormatVietnamese(x1 + x2)}  (= −b/a = {FormatVietnamese(-b / a)} ✓)\n" +
                    $"x₁ · x₂ = {FormatVietnamese(x1, "G6")} × {FormatVietnamese(x2, "G6")} = {FormatVietnamese(x1 * x2)}  (= c/a = {FormatVietnamese(c / a)} ✓)",
                    "#E65100"));
            }
            else if (delta == 0)
            {
                steps.Add(("Bước 3: Δ = 0 → Nghiệm kép",
                    $"x = −b / 2a = −({FormatVietnamese(b)}) / (2·{FormatVietnamese(a)}) = {FormatVietnamese(x1)}",
                    "#F57F17"));
            }
            else
            {
                double real = -b / (2 * a);
                double imag = System.Math.Sqrt(-delta) / (2 * a);
                imag = System.Math.Abs(imag);
                if (real == 0.0) real = 0.0;
                if (imag == 0.0) imag = 0.0;

                bool showComplex = chkShowComplex != null && chkShowComplex.IsChecked == true;

                if (showComplex)
                {
                    steps.Add(("Bước 3: Δ < 0 → Vô nghiệm thực (Có 2 nghiệm phức)",
                        $"PT có 2 nghiệm phức liên hợp:\n" +
                        $"x₁ = {FormatVietnamese(real, "G6")} + {FormatVietnamese(imag, "G6")}i\n" +
                        $"x₂ = {FormatVietnamese(real, "G6")} − {FormatVietnamese(imag, "G6")}i",
                        "#C62828"));
                }
                else
                {
                    steps.Add(("Bước 3: Δ < 0 → Vô nghiệm thực",
                        "Vì Δ < 0 nên phương trình không có nghiệm thực.",
                        "#C62828"));
                }
            }

            // Vertex
            double xV = -b / (2 * a);
            double yV = a * xV * xV + b * xV + c;
            steps.Add(($"Bước {steps.Count + 1}: Đỉnh Parabol",
                $"xᵥ = −b/2a = −({FormatVietnamese(b)})/(2·{FormatVietnamese(a)}) = {FormatVietnamese(xV, "G6")}\n" +
                $"yᵥ = f(xᵥ) = {FormatVietnamese(a)}·({FormatVietnamese(xV, "G6")})² + ({FormatVietnamese(b)})·({FormatVietnamese(xV, "G6")}) + ({FormatVietnamese(c)}) = {FormatVietnamese(yV)}\n" +
                $"Đỉnh I({FormatVietnamese(xV, "G6")}; {FormatVietnamese(yV, "G6")})\n" +
                $"Parabol {(a > 0 ? "quay lên ∪ → y có giá trị nhỏ nhất" : "quay xuống ∩ → y có giá trị lớn nhất")}",
                "#00695C"));

            for (int i = 0; i < steps.Count; i++)
            {
                var (step, detail, color) = steps[i];
                var accentColor = (Color)ColorConverter.ConvertFromString(color);

                var card = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(14, 10, 14, 10),
                    Margin = new Thickness(0, 0, 0, 6),
                    Background = new SolidColorBrush(Color.FromArgb(15, accentColor.R, accentColor.G, accentColor.B)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, accentColor.R, accentColor.G, accentColor.B)),
                    BorderThickness = new Thickness(0, 0, 0, 2)
                };

                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = step,
                    FontSize = 12, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(accentColor)
                });
                sp.Children.Add(new TextBlock
                {
                    Text = detail,
                    FontSize = 12, Margin = new Thickness(0, 4, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                    FontFamily = new FontFamily("Segoe UI"),
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 22
                });
                card.Child = sp;

                // Wrap with section focus toolbar
                string sectionId = $"step_{i + 1}";
                stepsPanel.Children.Add(WrapWithSectionToolbar(card, "quadratic", sectionId, $"Bước {i + 1}"));
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  THEORY — Lý thuyết
        // ═══════════════════════════════════════════════════════════

        private void BuildTheory()
        {
            if (theoryPanel == null) return;
            theoryPanel.Children.Clear();

            var items = new (string Title, string Content, string Bg)[]
            {
                ("📐 Công thức nghiệm",
                 "x = (−b ± √Δ) / 2a\nΔ = b² − 4ac",
                 "#E3F2FD"),

                ("📊 Phân loại nghiệm",
                 "• Δ > 0: PT có 2 nghiệm phân biệt\n• Δ = 0: PT có nghiệm kép x = −b/2a\n• Δ < 0: PT vô nghiệm thực (có 2 nghiệm phức)",
                 "#E8F5E9"),

                ("🔗 Định lý Vieta",
                 "Nếu x₁, x₂ là nghiệm:\n• x₁ + x₂ = −b/a  (tổng)\n• x₁ · x₂ = c/a  (tích)",
                 "#FFF3E0"),

                ("📈 Đồ thị Parabol y = ax² + bx + c",
                 "• a > 0: Parabol quay lên ∪\n• a < 0: Parabol quay xuống ∩\n• Đỉnh I(−b/2a; −Δ/4a)\n• Trục đối xứng: x = −b/2a",
                 "#F3E5F5"),

                ("🧩 Phân tích nhân tử",
                 "ax² + bx + c = a(x − x₁)(x − x₂)\nkhi Δ ≥ 0",
                 "#FBE9E7"),

                ("💡 Mẹo giải nhanh",
                 "• a+b+c=0 → x₁=1, x₂=c/a\n• a−b+c=0 → x₁=−1, x₂=−c/a\n• c=0 → x₁=0, x₂=−b/a",
                 "#E0F7FA"),
            };

            foreach (var (title, content, bgHex) in items)
            {
                var bgColor = (Color)ColorConverter.ConvertFromString(bgHex);
                var card = new Border
                {
                    Background = new SolidColorBrush(bgColor),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(14, 10, 14, 10),
                    Margin = new Thickness(0, 0, 8, 8),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                    BorderThickness = new Thickness(1)
                };

                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = title,
                    FontSize = 13, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
                });
                sp.Children.Add(new TextBlock
                {
                    Text = content,
                    FontSize = 12, Margin = new Thickness(0, 6, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                    FontFamily = new FontFamily("Segoe UI"),
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 22
                });
                card.Child = sp;

                string secId = title.Split(' ').Last().ToLowerInvariant();
                theoryPanel.Children.Add(WrapWithSectionToolbar(card, "quadratic_theory", secId, title));
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  HISTORY — Lịch sử giải
        // ═══════════════════════════════════════════════════════════

        private void RenderHistory()
        {
            if (historyPanel == null) return;
            historyPanel.Children.Clear();

            if (_history.Count == 0)
            {
                historyPanel.Children.Add(new TextBlock
                {
                    Text = "Chưa có lịch sử — nhập hệ số rồi xem kết quả ở đây",
                    FontSize = 11, Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 0)
                });
                return;
            }

            for (int i = 0; i < System.Math.Min(_history.Count, 10); i++)
            {
                var entry = _history[i];
                bool isPositive = entry.Contains("x₁=") || entry.Contains("x₂=");
                bool isZero = entry.Contains("(kép)");

                var row = new Border
                {
                    Background = i % 2 == 0
                        ? new SolidColorBrush(Color.FromRgb(241, 248, 233))
                        : Brushes.White,
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 5, 10, 5),
                    Margin = new Thickness(0, 0, 0, 2),
                    Cursor = Cursors.Hand,
                    ToolTip = "Click để copy"
                };

                var dp = new DockPanel();

                // Status icon
                string icon = isPositive ? "✅" : (isZero ? "🟡" : "❌");
                dp.Children.Add(new TextBlock
                {
                    Text = icon, FontSize = 12, Width = 20,
                    VerticalAlignment = VerticalAlignment.Center
                });

                dp.Children.Add(new TextBlock
                {
                    Text = entry,
                    FontSize = 11, FontFamily = new FontFamily("Segoe UI"),
                    Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.NoWrap,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });

                row.Child = dp;
                string capturedEntry = entry;
                row.MouseLeftButtonDown += (_, _) =>
                {
                    try { Clipboard.SetText(capturedEntry); } catch { }
                };

                historyPanel.Children.Add(row);
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            _history.Clear();
            RenderHistory();
            SaveHistory();
        }

        // ═══════════════════════════════════════════════════════════
        //  Graph — Mở đồ thị trên Graph Calculator
        // ═══════════════════════════════════════════════════════════

        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!ParsingHelper.TryParseDouble(txtCoeffA?.Text, out double a) || a == 0) return;
                if (!ParsingHelper.TryParseDouble(txtCoeffB?.Text, out double b)) return;
                if (!ParsingHelper.TryParseDouble(txtCoeffC?.Text, out double c)) return;

                var win = new GraphWindow(a, b, c);
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở đồ thị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════

        public static string FormatVietnamese(double value, string format = "G8")
        {
            if (double.IsNaN(value)) return "NaN";
            if (double.IsInfinity(value)) return value > 0 ? "∞" : "−∞";
            var nfi = new System.Globalization.NumberFormatInfo
            {
                NumberDecimalSeparator = ",",
                NumberGroupSeparator = ""
            };
            return value.ToString(format, nfi).Replace('-', '−');
        }

        private void CopyMissingImagesHelper()
        {
            try
            {
                string sourceDir = @"C:\Users\DELL\.gemini\antigravity\brain\a71d5e05-8236-483e-9691-23ea4b7fd55e";
                string targetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Assets\Images");
                if (!Directory.Exists(targetDir))
                {
                    targetDir = @"d:\JOB\QA SmartClass -062026\QASmartClass\Assets\Images";
                }
                if (Directory.Exists(sourceDir) && Directory.Exists(targetDir))
                {
                    string[][] filesToCopy = new string[][]
                    {
                        new string[] { "app_quadratic_7_vn_1782010992165.png", "app_quadratic_7_VN.png" },
                        new string[] { "app_quadratic_7_vn_1782010992165.png", "app_quadratic_7_EN.png" },
                        new string[] { "app_quadratic_8_vn_1782011006196.png", "app_quadratic_8_VN.png" },
                        new string[] { "app_quadratic_8_vn_1782011006196.png", "app_quadratic_8_EN.png" }
                    };

                    foreach (var pair in filesToCopy)
                    {
                        string src = Path.Combine(sourceDir, pair[0]);
                        string dest = Path.Combine(targetDir, pair[1]);
                        if (File.Exists(src) && !File.Exists(dest))
                        {
                            File.Copy(src, dest, true);
                        }
                    }
                }
            }
            catch { }
        }

        private static string FormatEq(double a, double b, double c)
        {
            string eq = "";
            
            // Format ax²
            if (a == 1) eq += "x²";
            else if (a == -1) eq += "−x²";
            else eq += $"{FormatVietnamese(a, "G6")}x²";

            // Format bx
            if (b != 0)
            {
                if (b == 1) eq += " + x";
                else if (b == -1) eq += " − x";
                else if (b > 0) eq += $" + {FormatVietnamese(b, "G6")}x";
                else eq += $" − {FormatVietnamese(System.Math.Abs(b), "G6")}x";
            }

            // Format c
            if (c != 0)
            {
                if (c > 0) eq += $" + {FormatVietnamese(c, "G6")}";
                else eq += $" − {FormatVietnamese(System.Math.Abs(c), "G6")}";
            }

            eq += " = 0";
            return eq;
        }

        private static string FormatCoeff(double v)
        {
            if (v == 1) return "";
            if (v == -1) return "−";
            return FormatVietnamese(v, "G6");
        }

        private static string FormatFactor(double root)
        {
            if (root == 0) return "x";
            if (root < 0) return $"(x + {FormatVietnamese(System.Math.Abs(root), "G6")})";
            return $"(x − {FormatVietnamese(root, "G6")})";
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootGrid == null || sideMenu == null || contentCalc == null || contentGuide == null || contentApp == null)
                return;

            contentCalc.Visibility = Visibility.Collapsed;
            contentGuide.Visibility = Visibility.Collapsed;
            contentApp.Visibility = Visibility.Collapsed;

            var fadeAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(250)
            };

            int index = sideMenu.SelectedIndex;
            if (index == 0)
            {
                contentCalc.Visibility = Visibility.Visible;
                contentCalc.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
            }
            else if (index == 1)
            {
                contentGuide.Visibility = Visibility.Visible;
                contentGuide.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
            }
            else if (index == 2)
            {
                contentApp.Visibility = Visibility.Visible;
                contentApp.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
            }
        }

        private void Tab_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedBorder && clickedBorder.Tag is string tag)
            {
                SwitchToTab(tag);
            }
        }

        private void SwitchToTab(string tag)
        {
            if (sideMenu == null) return;
            if (tag == "calc" || tag == "practice")
                sideMenu.SelectedIndex = 1;
            else if (tag == "guide")
                sideMenu.SelectedIndex = 1;
            else if (tag == "app" || tag == "practical")
                sideMenu.SelectedIndex = 1;
        }

        private void LoadPracticalApps()
        {
            try
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                string suffix = isVN ? "VN" : "EN";

                var items = new List<PracticalAppItem>
                {
                    new PracticalAppItem
                    {
                        Icon = "🏀",
                        Title = isVN ? "Quỹ Đạo Vật Thể" : "Projectile & Rocket Trajectory",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_quadratic_1_{suffix}.png",
                        Description = isVN 
                            ? "Mô tả quỹ đạo bay của quả bóng, mũi tên hay vật thể bị ném xiên trong không khí dưới tác dụng của trọng lực." 
                            : "Model parabolic paths of soccer balls, arrows, or rockets and calculate their peak heights and landing distances."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌉",
                        Title = isVN ? "Cầu Vòm Parabol" : "Satellite Dish Reflection",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_quadratic_2_{suffix}.png",
                        Description = isVN 
                            ? "Thiết kế kết cấu vòm cầu chịu lực tối ưu giúp phân tán lực đều sang hai bên mố cầu, tạo độ bền bỉ vững chắc." 
                            : "Focus incoming parallel signal waves to a central receiver point using parabolic surfaces defined by quadratic curves."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📡",
                        Title = isVN ? "Ăng-ten Chảo Vệ Tinh" : "Bridge Arch Structures",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_quadratic_3_{suffix}.png",
                        Description = isVN 
                            ? "Khả năng phản xạ và hội tụ toàn bộ sóng vô tuyến/ánh sáng song song đi thẳng vào một điểm thu duy nhất (tiêu điểm)." 
                            : "Design suspension bridge cables and arch support structures that distribute weight load evenly along parabolic curves."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📈",
                        Title = isVN ? "Tối Ưu Doanh Thu" : "Profit Maximization Analysis",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_quadratic_4_{suffix}.png",
                        Description = isVN 
                            ? "Xác định mức giá bán sản phẩm phù hợp để tối đa hóa doanh thu và lợi nhuận kinh doanh (tương ứng tọa độ đỉnh Parabol)." 
                            : "Model business revenue as a quadratic function of price to determine the sweet spot that maximizes net profits."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔦",
                        Title = isVN ? "Thiết Kế Đèn Pha" : "Automobile Braking Distance",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_quadratic_5_{suffix}.png",
                        Description = isVN 
                            ? "Phản xạ các tia sáng phát ra từ bóng đèn nằm ở tiêu điểm thành một chùm sáng song song rọi thẳng đi xa." 
                            : "Calculate stopping distance relative to driving speed, which grows quadratically due to kinetic energy formulas: d = k * v²."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🕸",
                        Title = isVN ? "️ Cáp Cầu Treo Võng" : "Searchlight Optical Design",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_quadratic_6_{suffix}.png",
                        Description = isVN 
                            ? "Đường dây cáp treo chịu lực võng xuống chịu trọng lượng của toàn bộ mặt cầu treo phẳng phân bố đều." 
                            : "Project light source rays into parallel beams using parabolic mirrors to maximize illumination distance."
                    }
                ,
                    new PracticalAppItem
                    {
                        Icon = "🚀",
                        Title = isVN ? "Quỹ đạo tên lửa tầm thấp" : "Low-Altitude Rocket Trajectory",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_quadratic_7_{suffix}.png",
                        Description = isVN 
                            ? "Mở rộng mô hình quỹ đạo parabol để tính toán góc phóng và vận tốc ban đầu giúp tên lửa mini đạt độ cao mong muốn." 
                            : "Extend parabolic trajectory models to calculate launch angle and initial velocity for mini rockets to hit targets."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💸",
                        Title = isVN ? "Tối ưu hóa giá bán lẻ" : "Retail Price Optimization",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_quadratic_8_{suffix}.png",
                        Description = isVN 
                            ? "Thiết lập hàm số doanh thu bậc hai giữa giá bán và lượng cầu để tìm mức giá bán mang lại lợi nhuận cao nhất." 
                            : "Formulate a quadratic revenue function relative to pricing to determine the optimal price point for maximum profit."
                    }};

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for QuadraticTool: {Err}", ex.Message);
            }
        }

        private string GetHistoryFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string qaFolder = Path.Combine(appData, "QASmartClass");
            if (!Directory.Exists(qaFolder))
            {
                Directory.CreateDirectory(qaFolder);
            }
            return Path.Combine(qaFolder, "quadratic_history.json");
        }

        private void LoadHistory()
        {
            try
            {
                string path = GetHistoryFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(json);
                    if (list != null)
                    {
                        _history.Clear();
                        _history.AddRange(list);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading history: " + ex.Message);
            }
        }

        private void SaveHistory()
        {
            try
            {
                string path = GetHistoryFilePath();
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(_history);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error saving history: " + ex.Message);
            }
        }
    }
}