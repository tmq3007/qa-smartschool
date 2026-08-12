using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace QASmartClass.Classroom.Views
{
    public partial class StemToolsPage : Page
    {
        private TextBox? _lastGraphExprBox;
        public StemToolsPage()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                BuildCalcButtons();
                LoadUnits();
                LoadConstants();
                LoadFormulas("Physics");
                SolveQuadratic();

                // Tab 6: Graph presets
                BuildGraphPresets();
                // Tab 7: Chem presets
                BuildChemPresets();
                // Tab 8: Default STEM table
                LoadStemTable("Trig");
                // Tab 9: Default stats rows
                for (int i = 0; i < 5; i++) AddStatsRow("", "");
                
                // Bàn phím số mini cho phương trình
                QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtCoeffA, step: 1, min: -100, max: 100);
                QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtCoeffB, step: 1, min: -100, max: 100);
                QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtCoeffC, step: 1, min: -100, max: 100);

                // Bàn phím cảm ứng cho nhập PTHH
                QASmartClass.LearningTools.Controls.TouchTextPad.Attach(txtChemEquation, mode: "chem");

                // Đồ thị nhanh
                QASmartClass.LearningTools.Controls.TouchTextPad.Attach(txtExpr1, mode: "text");
                QASmartClass.LearningTools.Controls.TouchTextPad.Attach(txtExpr2, mode: "text");
            };
        }

        // =======================================================
        //  NAVIGATION SUB-FLOW
        // =======================================================
        private void GoBackHome_Click(object sender, RoutedEventArgs e)
        {
            // Điều hướng quay lại Trang chủ (F1)
            var shell = Window.GetWindow(this) as ClassroomShell;
            if (shell != null)
            {
                Serilog.Log.Information("StemTools sub-flow: Navigating back to Home (F1)");
                shell.NavigateTo("F1");
            }
        }

        // =======================================================
        //  CALCULATOR ?" Máy tính khoa học nâng cao
        // =======================================================

        private readonly List<string> _calcHistory = new();

        private void BuildCalcButtons()
        {
            // Row 1: Scientific functions
            // Row 2-5: Numbers + operators (like real calculator)  
            // Row 6: Actions
            var buttons = new (string Label, string Bg, string Fg)[]
            {
                // Row 1 — Scientific
                ("sin",  "#374151", "#E5E7EB"), ("cos",  "#374151", "#E5E7EB"),
                ("tan",  "#374151", "#E5E7EB"), ("log",  "#374151", "#E5E7EB"),
                ("ln",   "#374151", "#E5E7EB"), ("√",    "#374151", "#E5E7EB"),

                // Row 2 — 7 8 9 ÷ ( )
                ("7",    "#FFFFFF", "#1F2937"), ("8",    "#FFFFFF", "#1F2937"),
                ("9",    "#FFFFFF", "#1F2937"), ("÷",    "#DBEAFE", "#1E40AF"),
                ("(",    "#F3F4F6", "#6B7280"), (")",    "#F3F4F6", "#6B7280"),

                // Row 3 — 4 5 6 × ^ %
                ("4",    "#FFFFFF", "#1F2937"), ("5",    "#FFFFFF", "#1F2937"),
                ("6",    "#FFFFFF", "#1F2937"), ("×",    "#DBEAFE", "#1E40AF"),
                ("^",    "#DBEAFE", "#1E40AF"), ("%",    "#DBEAFE", "#1E40AF"),

                // Row 4 — 1 2 3 - ⌫ C
                ("1",    "#FFFFFF", "#1F2937"), ("2",    "#FFFFFF", "#1F2937"),
                ("3",    "#FFFFFF", "#1F2937"), ("-",    "#DBEAFE", "#1E40AF"),
                ("⌫",    "#FEF3C7", "#92400E"), ("C",    "#FEE2E2", "#991B1B"),

                // Row 5 — 0 . π e + =
                ("0",    "#FFFFFF", "#1F2937"), (".",    "#FFFFFF", "#1F2937"),
                ("π",    "#F3F4F6", "#6B7280"), ("e",    "#F3F4F6", "#6B7280"),
                ("+",    "#DBEAFE", "#1E40AF"), ("=",    "#2563EB", "#FFFFFF"),
            };

            foreach (var (label, bg, fg) in buttons)
            {
                var btn = new Button
                {
                    Content = label, FontSize = 20, Height = 64,
                    Margin = new Thickness(4), BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand, Tag = label
                };

                var bgColor = (Color)ColorConverter.ConvertFromString(bg);
                var fgColor = (Color)ColorConverter.ConvertFromString(fg);

                var template = new ControlTemplate(typeof(Button));
                var bd = new FrameworkElementFactory(typeof(Border), "bd");
                bd.SetValue(Border.BackgroundProperty, new SolidColorBrush(bgColor));
                bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
                bd.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)));
                bd.SetValue(Border.BorderThicknessProperty, new Thickness(1));
                var cp = new FrameworkElementFactory(typeof(ContentPresenter));
                cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                cp.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(fgColor));
                cp.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
                bd.AppendChild(cp);
                template.VisualTree = bd;

                // Hover — darken slightly
                var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                byte r = (byte)Math.Max(0, bgColor.R - 20);
                byte g = (byte)Math.Max(0, bgColor.G - 20);
                byte b = (byte)Math.Max(0, bgColor.B - 20);
                hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(r, g, b)), "bd"));
                template.Triggers.Add(hoverTrigger);

                btn.Template = template;
                btn.Click += CalcBtn_Click;
                calcButtons.Children.Add(btn);
            }
        }

        private string _lastResult = "0";

        private void CalcBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string label) return;

            switch (label)
            {
                case "C":
                    txtCalcInput.Text = "";
                    txtCalcDisplay.Text = "0";
                    break;
                case "⌫":
                    if (txtCalcInput.Text.Length > 0)
                        txtCalcInput.Text = txtCalcInput.Text[..^1];
                    break;
                case "=":
                    EvaluateExpression();
                    break;
                case "π":
                    txtCalcInput.Text += "3.14159265359";
                    break;
                case "e":
                    txtCalcInput.Text += "2.71828182846";
                    break;
                case "√":
                    txtCalcInput.Text += "Sqrt(";
                    break;
                case "log":
                    txtCalcInput.Text += "Log10(";
                    break;
                case "ln":
                    txtCalcInput.Text += "Ln(";
                    break;
                case "±":
                    if (double.TryParse(txtCalcDisplay.Text, out double val))
                        txtCalcDisplay.Text = (-val).ToString();
                    break;
                case "÷": txtCalcInput.Text += "/"; break;
                case "×": txtCalcInput.Text += "*"; break;
                case "^": txtCalcInput.Text += "^"; break;
                case "sin": txtCalcInput.Text += "Sin("; break;
                case "cos": txtCalcInput.Text += "Cos("; break;
                case "tan": txtCalcInput.Text += "Tan("; break;
                case "Ans": txtCalcInput.Text += _lastResult; break;
                default:
                    txtCalcInput.Text += label;
                    break;
            }
        }

        private void CalcInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) EvaluateExpression();
        }

        private void EvaluateExpression()
        {
            try
            {
                string expr = txtCalcInput.Text;
                if (string.IsNullOrWhiteSpace(expr)) return;

                // Replace math functions with .NET equivalents
                string evalExpr = expr
                    .Replace("×", "*").Replace("÷", "/")
                    .Replace("^", "**")
                    .Replace("Sin(", "System.Math.Sin(")
                    .Replace("Cos(", "System.Math.Cos(")
                    .Replace("Tan(", "System.Math.Tan(")
                    .Replace("Sqrt(", "System.Math.Sqrt(")
                    .Replace("Log10(", "System.Math.Log10(")
                    .Replace("Ln(", "System.Math.Log(");

                // Simple DataTable evaluation (support +-*/%)
                string simpleExpr = txtCalcInput.Text
                    .Replace("×", "*").Replace("÷", "/")
                    .Replace("Sin(", "0+").Replace("Cos(", "0+").Replace("Tan(", "0+")
                    .Replace("Sqrt(", "0+").Replace("Log10(", "0+").Replace("Ln(", "0+");

                // Check for scientific functions
                double result;
                if (expr.Contains("Sin(") || expr.Contains("Cos(") || expr.Contains("Tan(") ||
                    expr.Contains("Sqrt(") || expr.Contains("Log10(") || expr.Contains("Ln(") ||
                    expr.Contains("^"))
                {
                    result = EvalScientific(expr);
                }
                else
                {
                    var dt = new DataTable();
                    var computed = dt.Compute(expr.Replace("×", "*").Replace("÷", "/"), null);
                    result = Convert.ToDouble(computed);
                }

                _lastResult = result.ToString("G10");
                txtCalcDisplay.Text = _lastResult;

                // Save to history
                _calcHistory.Insert(0, $"{expr} = {_lastResult}");
                if (_calcHistory.Count > 20) _calcHistory.RemoveAt(20);
            }
            catch
            {
                txtCalcDisplay.Text = "Lỗi biểu thức";
            }
        }

        private double EvalScientific(string expr)
        {
            // Parse and evaluate scientific expressions
            expr = expr.Replace("×", "*").Replace("÷", "/");

            // Handle ^ (power)
            while (expr.Contains("^"))
            {
                int idx = expr.IndexOf("^");
                // Find base number (left of ^)
                int baseStart = idx - 1;
                while (baseStart > 0 && (char.IsDigit(expr[baseStart - 1]) || expr[baseStart - 1] == '.'))
                    baseStart--;
                // Find exponent (right of ^)
                int expEnd = idx + 1;
                if (expEnd < expr.Length && expr[expEnd] == '-') expEnd++;
                while (expEnd < expr.Length && (char.IsDigit(expr[expEnd]) || expr[expEnd] == '.'))
                    expEnd++;

                double baseVal = double.Parse(expr[baseStart..idx]);
                double expVal = double.Parse(expr[(idx + 1)..expEnd]);
                double powResult = Math.Pow(baseVal, expVal);
                expr = expr[..baseStart] + powResult.ToString("G10") + expr[expEnd..];
            }

            // Handle functions
            expr = EvalFunc(expr, "Sin", Math.Sin);
            expr = EvalFunc(expr, "Cos", Math.Cos);
            expr = EvalFunc(expr, "Tan", Math.Tan);
            expr = EvalFunc(expr, "Sqrt", Math.Sqrt);
            expr = EvalFunc(expr, "Log10", Math.Log10);
            expr = EvalFunc(expr, "Ln", Math.Log);

            var dt = new DataTable();
            return Convert.ToDouble(dt.Compute(expr, null));
        }

        private string EvalFunc(string expr, string funcName, Func<double, double> func)
        {
            while (expr.Contains(funcName + "("))
            {
                int start = expr.IndexOf(funcName + "(");
                int openParen = start + funcName.Length;
                int closeParen = expr.IndexOf(")", openParen);
                if (closeParen < 0) break;

                string inner = expr[(openParen + 1)..closeParen];
                // Try to evaluate inner expression first
                double innerVal;
                try
                {
                    var dt = new DataTable();
                    innerVal = Convert.ToDouble(dt.Compute(inner, null));
                }
                catch { innerVal = double.Parse(inner); }

                double result = func(innerVal);
                expr = expr[..start] + result.ToString("G10") + expr[(closeParen + 1)..];
            }
            return expr;
        }

        private void CalcHistory_Click(object sender, RoutedEventArgs e)
        {
            calcHistoryPanel.Visibility = calcHistoryPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed : Visibility.Visible;

            calcHistoryList.Children.Clear();
            if (_calcHistory.Count == 0)
            {
                calcHistoryList.Children.Add(new TextBlock
                {
                    Text = "Chưa có lịch sử", FontSize = 11, Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                return;
            }

            foreach (var item in _calcHistory)
            {
                var row = new Border
                {
                    Background = Brushes.White, CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 0, 3), Cursor = Cursors.Hand
                };
                var tb = new TextBlock { Text = item, FontSize = 11, FontFamily = new FontFamily("Segoe UI") };
                row.Child = tb;
                row.MouseLeftButtonDown += (s, _) =>
                {
                    var parts = item.Split(" = ");
                    if (parts.Length == 2) txtCalcInput.Text = parts[0];
                };
                row.MouseEnter += (s, _) => row.Background = new SolidColorBrush(Color.FromRgb(232, 234, 246));
                row.MouseLeave += (s, _) => row.Background = Brushes.White;
                calcHistoryList.Children.Add(row);
            }
        }

        private void CalcClearAll_Click(object sender, RoutedEventArgs e)
        {
            txtCalcInput.Text = "";
            txtCalcDisplay.Text = "0";
            _calcHistory.Clear();
            calcHistoryPanel.Visibility = Visibility.Collapsed;
        }

        //  UNIT CONVERTER — Đổi đơn vị nâng cao
        // =======================================================

        private static readonly Dictionary<string, List<(string Name, double Factor)>> Units = new()
        {
            ["📏 Chiều dài"] = new() { ("m", 1), ("cm", 0.01), ("mm", 0.001), ("km", 1000), ("inch", 0.0254), ("ft", 0.3048), ("mile", 1609.34), ("nm", 1e-9), ("μm", 1e-6) },
            ["⚖️ Khối lượng"] = new() { ("kg", 1), ("g", 0.001), ("mg", 1e-6), ("tấn", 1000), ("lb", 0.453592), ("oz", 0.0283495), ("carat", 0.0002) },
            ["🌡️ Nhiệt độ"] = new() { ("°C", 1), ("°F", 1), ("K", 1) },
            ["📐 Diện tích"] = new() { ("m²", 1), ("cm²", 1e-4), ("km²", 1e6), ("ha", 1e4), ("acre", 4046.86), ("sào", 360) },
            ["🧊 Thể tích"] = new() { ("lít", 1), ("ml", 0.001), ("m³", 1000), ("gallon", 3.78541), ("cc", 0.001) },
            ["⏱️ Thời gian"] = new() { ("giây", 1), ("phút", 60), ("giờ", 3600), ("ngày", 86400), ("tuần", 604800), ("ms", 0.001) },
            ["⚡ Năng lượng"] = new() { ("J", 1), ("kJ", 1000), ("cal", 4.184), ("kcal", 4184), ("kWh", 3.6e6), ("eV", 1.602e-19) },
        };

        private void LoadUnits()
        {
            UnitType_Changed(null, null);
        }

        private void UnitType_Changed(object? sender, SelectionChangedEventArgs? e)
        {
            if (cboUnitType?.SelectedItem is not ComboBoxItem item) return;
            string type = item.Content?.ToString() ?? "";
            if (!Units.ContainsKey(type)) return;
            if (cboFromUnit == null || cboToUnit == null) return;

            cboFromUnit.Items.Clear();
            cboToUnit.Items.Clear();
            foreach (var (name, _) in Units[type])
            {
                cboFromUnit.Items.Add(new ComboBoxItem { Content = name });
                cboToUnit.Items.Add(new ComboBoxItem { Content = name });
            }
            if (cboFromUnit.Items.Count > 0) cboFromUnit.SelectedIndex = 0;
            if (cboToUnit.Items.Count > 1) cboToUnit.SelectedIndex = 1;
        }

        private void Convert_UnitChanged(object sender, SelectionChangedEventArgs e)
        {
            DoConversion();
        }

        private void Convert_Changed(object sender, TextChangedEventArgs e)
        {
            DoConversion();
        }

        private void DoConversion()
        {
            try
            {
                if (cboUnitType?.SelectedItem is not ComboBoxItem typeItem) return;
                string type = typeItem.Content?.ToString() ?? "";
                if (!Units.ContainsKey(type)) return;
                if (txtToValue == null || txtConvertFormula == null) return;

                string fromName = (cboFromUnit?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                string toName = (cboToUnit?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

                if (!double.TryParse(txtFromValue?.Text, out double value)) { txtToValue.Text = ""; return; }

                double result;
                if (type == "🌡️ Nhiệt độ")
                {
                    double celsius = fromName switch
                    {
                        "°F" => (value - 32) * 5.0 / 9,
                        "K" => value - 273.15,
                        _ => value
                    };
                    result = toName switch
                    {
                        "°F" => celsius * 9.0 / 5 + 32,
                        "K" => celsius + 273.15,
                        _ => celsius
                    };
                }
                else
                {
                    var fromFactor = Units[type].FirstOrDefault(u => u.Name == fromName).Factor;
                    var toFactor = Units[type].FirstOrDefault(u => u.Name == toName).Factor;
                    if (toFactor == 0) return;
                    result = value * fromFactor / toFactor;
                }

                txtToValue.Text = result.ToString("G8");
                txtConvertFormula.Text = $"1 {fromName} = {(type == "🌡️ Nhiệt độ" ? "..." : (Units[type].First(u => u.Name == fromName).Factor / Units[type].First(u => u.Name == toName).Factor).ToString("G6"))} {toName}";
            }
            catch { }
        }

        private void SwapUnits_Click(object sender, RoutedEventArgs e)
        {
            if (cboFromUnit == null || cboToUnit == null) return;
            int fromIdx = cboFromUnit.SelectedIndex;
            int toIdx = cboToUnit.SelectedIndex;
            cboFromUnit.SelectedIndex = toIdx;
            cboToUnit.SelectedIndex = fromIdx;

            // Swap value
            if (txtToValue != null && txtFromValue != null && !string.IsNullOrEmpty(txtToValue.Text))
            {
                txtFromValue.Text = txtToValue.Text;
            }
        }

        // =======================================================
        //  CONSTANTS — Hằng số Vật lý & Toán học
        // =======================================================

        private void LoadConstants()
        {
            if (constantsPanel == null) return;

            var constants = new (string Symbol, string Name, string Value, string Unit, string Color)[]
            {
                ("c",  "Tốc độ ánh sáng",     "299,792,458",        "m/s",       "#E3F2FD"),
                ("h",  "Hằng số Planck",       "6.626 × 10⁻³⁴",     "J·s",       "#F3E5F5"),
                ("kB", "Hằng số Boltzmann",    "1.381 × 10⁻²³",     "J/K",       "#FFF3E0"),
                ("e",  "Điện tích electron",   "1.602 × 10⁻¹⁹",     "C",         "#E0F2F1"),
                ("NA", "Số Avogadro",          "6.022 × 10²³",      "mol⁻¹",     "#FCE4EC"),
                ("G",  "Hằng số hấp dẫn",     "6.674 × 10⁻¹¹",     "N·m²/kg²",  "#E8F5E9"),
                ("g",  "Gia tốc trọng trường", "9.80665",            "m/s²",      "#FFF8E1"),
                ("R",  "Hằng số khí",          "8.31446",            "J/(mol·K)", "#E8EAF6"),
                ("π",  "Số Pi",                "3.14159265359",      "—",         "#FFEBEE"),
                ("e",  "Số Euler",             "2.71828182846",      "—",         "#F1F8E9"),
                ("√2", "Căn 2",                "1.41421356237",      "—",         "#E0F7FA"),
                ("me", "Khối lượng electron",  "9.109 × 10⁻³¹",     "kg",        "#FBE9E7"),
            };

            foreach (var (symbol, name, value, unit, colorHex) in constants)
            {
                var cardBg = (SolidColorBrush)new BrushConverter().ConvertFrom(colorHex)!;

                var card = new Border
                {
                    Background = cardBg, CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(16, 14, 16, 14), Margin = new Thickness(0, 0, 12, 12),
                    Width = 240, Cursor = Cursors.Hand,
                    BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                    BorderThickness = new Thickness(1),
                    ToolTip = $"Nhấn để copy: {value} {unit}"
                };

                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = symbol, FontSize = 28, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                    FontFamily = new FontFamily("Cambria Math, Segoe UI")
                });
                sp.Children.Add(new TextBlock
                {
                    Text = name, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                    Margin = new Thickness(0, 2, 0, 4)
                });
                sp.Children.Add(new TextBlock
                {
                    Text = $"{symbol} = {value} {unit}",
                    FontSize = 14, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                    TextWrapping = TextWrapping.Wrap
                });

                card.Child = sp;
                string copyVal = value;
                card.MouseLeftButtonDown += (s, _) =>
                {
                    try
                    {
                        Clipboard.SetText(copyVal);
                        // Flash feedback
                        card.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                        card.BorderThickness = new Thickness(2);
                        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
                        timer.Tick += (_, _) =>
                        {
                            card.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
                            card.BorderThickness = new Thickness(1);
                            timer.Stop();
                        };
                        timer.Start();
                    }
                    catch { }
                };

                constantsPanel.Children.Add(card);
            }
        }

        // =======================================================
        //  FORMULA REFERENCE — Công thức tham khảo
        // =======================================================

        private static readonly Dictionary<string, List<(string Name, string Formula, string Desc)>> Formulas = new()
        {
            ["Physics"] = new()
            {
                ("Vận tốc",          "v = s / t",              "v: vận tốc, s: quãng đường, t: thời gian"),
                ("Gia tốc",          "a = (v - v₀) / t",      "a: gia tốc, v₀: vận tốc đầu"),
                ("Lực",              "F = m × a",              "Định luật II Newton"),
                ("Trọng lực",        "P = m × g",              "P: trọng lực, g ≈ 9.8 m/s²"),
                ("Động năng",        "Wđ = ½mv²",             "m: khối lượng, v: vận tốc"),
                ("Thế năng",         "Wt = mgh",               "h: độ cao so với gốc"),
                ("Công suất",        "P = W / t",              "W: công, t: thời gian"),
                ("Điện trở",         "R = U / I",              "Định luật Ohm"),
                ("Công suất điện",   "P = U × I",              "U: hiệu điện thế, I: cường độ"),
                ("Tần số",           "f = 1 / T",              "T: chu kỳ"),
                ("Bước sóng",        "λ = v / f",              "v: tốc độ sóng, f: tần số"),
                ("Năng lượng photon","E = h × f",              "h: hằng số Planck"),
            },
            ["Chemistry"] = new()
            {
                ("Số mol",           "n = m / M",              "m: khối lượng, M: khối lượng mol"),
                ("Nồng độ mol",      "CM = n / V",             "n: số mol, V: thể tích (lít)"),
                ("Nồng độ %",        "C% = mct/mdd × 100%",   "mct: chất tan, mdd: dung dịch"),
                ("PV = nRT",         "PV = nRT",               "PT trạng thái khí lý tưởng"),
                ("pH",               "pH = -log[H⁺]",         "[H⁺]: nồng độ ion H⁺"),
                ("Tốc độ phản ứng",  "v = ΔC / Δt",           "ΔC: biến thiên nồng độ"),
                ("Entanpi",          "ΔH = Σ(sp) - Σ(tc)",    "sp: sản phẩm, tc: tác chất"),
                ("Hằng số cân bằng", "Kc = [sp]ⁿ / [tc]ᵐ",   "Ở trạng thái cân bằng"),
            },
            ["Math"] = new()
            {
                ("PT bậc 2",        "x = (-b±√Δ) / 2a",      "Δ = b² - 4ac"),
                ("Diện tích tròn",   "S = πr²",               "r: bán kính"),
                ("Chu vi tròn",      "C = 2πr",               "r: bán kính"),
                ("Thể tích cầu",    "V = 4πr³/3",            "r: bán kính"),
                ("Pythagoras",       "a² + b² = c²",          "Tam giác vuông"),
                ("Sin/Cos",          "sin²α + cos²α = 1",     "Hệ thức lượng giác cơ bản"),
                ("Đạo hàm",         "(xⁿ)' = n·xⁿ⁻¹",       "Công thức đạo hàm lũy thừa"),
                ("Tích phân",        "∫xⁿdx = xⁿ⁺¹/(n+1)+C", "n ≠ -1"),
                ("Logarit",          "logₐ(xy) = logₐx + logₐy", "Tính chất logarit"),
                ("Lãi suất kép",     "A = P(1+r)ⁿ",           "P: vốn, r: lãi suất, n: kỳ"),
            },
        };

        private void LoadFormulas(string category)
        {
            if (formulasPanel == null) return;
            formulasPanel.Children.Clear();

            if (!Formulas.ContainsKey(category)) return;

            foreach (var (name, formula, desc) in Formulas[category])
            {
                var card = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 253, 248)),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(16, 14, 16, 14),
                    Margin = new Thickness(0, 0, 12, 12),
                    Width = 260, Cursor = Cursors.Hand,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(240, 230, 215)),
                    BorderThickness = new Thickness(1),
                    ToolTip = $"Nhấn để copy: {formula}"
                };

                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = name, FontSize = 12, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0))
                });
                sp.Children.Add(new TextBlock
                {
                    Text = formula, FontSize = 18, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(26, 35, 126)),
                    FontFamily = new FontFamily("Cambria Math, Segoe UI"),
                    Margin = new Thickness(0, 4, 0, 4)
                });
                sp.Children.Add(new TextBlock
                {
                    Text = desc, FontSize = 12, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap
                });

                card.Child = sp;

                string copyFormula = formula;
                card.MouseLeftButtonDown += (s, _) =>
                {
                    try
                    {
                        Clipboard.SetText(copyFormula);
                        card.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                        card.BorderThickness = new Thickness(2);
                        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
                        timer.Tick += (_, _) =>
                        {
                            card.BorderBrush = new SolidColorBrush(Color.FromRgb(240, 230, 215));
                            card.BorderThickness = new Thickness(1);
                            timer.Stop();
                        };
                        timer.Start();
                    }
                    catch { }
                };
                card.MouseEnter += (s, _) => card.Background = new SolidColorBrush(Color.FromRgb(255, 245, 228));
                card.MouseLeave += (s, _) => card.Background = new SolidColorBrush(Color.FromRgb(255, 253, 248));

                formulasPanel.Children.Add(card);
            }
        }

        private void FormulaTab_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border tab || tab.Tag is not string category) return;

            // Update tab styles
            var tabs = new[] { tabPhysics, tabChemistry, tabMath };
            var activeColors = new Dictionary<string, (string bg, string fg)>
            {
                ["Physics"] = ("#E3F2FD", "#1565C0"),
                ["Chemistry"] = ("#E8F5E9", "#2E7D32"),
                ["Math"] = ("#FFF3E0", "#E65100"),
            };

            foreach (var t in tabs)
            {
                if (t == null) continue;
                string tagStr = t.Tag?.ToString() ?? "";
                if (tagStr == category)
                {
                    var (bg, fg) = activeColors[category];
                    t.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(bg)!;
                    if (t.Child is TextBlock tb)
                    {
                        tb.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(fg)!;
                        tb.FontWeight = FontWeights.SemiBold;
                    }
                }
                else
                {
                    t.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                    if (t.Child is TextBlock tb)
                    {
                        tb.Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117));
                        tb.FontWeight = FontWeights.Normal;
                    }
                }
            }

            LoadFormulas(category);
        }

        // =======================================================
        //  QUADRATIC SOLVER — Giải phương trình bậc 2
        // =======================================================

        private void SolveQuadratic_Changed(object sender, TextChangedEventArgs e)
        {
            SolveQuadratic();
        }

        private void SolveQuadratic()
        {
            try
            {
                if (txtCoeffA == null || txtCoeffB == null || txtCoeffC == null) return;
                if (txtDelta == null || txtQuadResult == null) return;

                if (!double.TryParse(txtCoeffA.Text, out double a) || a == 0)
                { txtQuadResult.Text = "a phải ≠ 0"; return; }
                if (!double.TryParse(txtCoeffB.Text, out double b))
                { txtQuadResult.Text = "b không hợp lệ"; return; }
                if (!double.TryParse(txtCoeffC.Text, out double c))
                { txtQuadResult.Text = "c không hợp lệ"; return; }

                double delta = b * b - 4 * a * c;
                txtDelta.Text = $"Δ = b² - 4ac = {delta:G6}";

                if (delta > 0)
                {
                    double x1 = (-b + Math.Sqrt(delta)) / (2 * a);
                    double x2 = (-b - Math.Sqrt(delta)) / (2 * a);
                    txtQuadResult.Text = $"x₁ = {x1:G6},  x₂ = {x2:G6}";
                    txtDelta.Foreground = new SolidColorBrush(Color.FromRgb(85, 139, 47)); // green
                    txtQuadResult.Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32));
                }
                else if (delta == 0)
                {
                    double x = -b / (2 * a);
                    txtQuadResult.Text = $"x₁ = x₂ = {x:G6}  (nghiệm kép)";
                    txtDelta.Foreground = new SolidColorBrush(Color.FromRgb(245, 127, 23)); // orange
                    txtQuadResult.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0));
                }
                else
                {
                    double real = -b / (2 * a);
                    double imag = Math.Sqrt(-delta) / (2 * a);
                    txtQuadResult.Text = $"x₁ = {real:G4} + {imag:G4}i\nx₂ = {real:G4} - {imag:G4}i";
                    txtDelta.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)); // red
                    txtQuadResult.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                }
            }
            catch { }
        }

        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                double a = 1, b = -5, c = 6;
                double.TryParse(txtCoeffA?.Text, out a);
                double.TryParse(txtCoeffB?.Text, out b);
                double.TryParse(txtCoeffC?.Text, out c);
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                
                string expr = $"y={a.ToString(ci)}x^2+({b.ToString(ci)})x+({c.ToString(ci)})";
                string expJs = $@"
        calc.setExpression({{id:'parabola', latex:'{expr}', color:'#2E7D32', lineWidth:3}});
        calc.setExpression({{id:'xaxis', latex:'y=0', color:'#000000', lineWidth:1}});
        calc.setMathBounds({{ left: -10, right: 10, bottom: -10, top: 10 }});";

                var win = new QASmartClass.LearningTools.Views.Math.GraphWindow(expJs, $"📈 Đồ thị: {expr}");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Không thể mở đồ thị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // =======================================================
        //  STOPWATCH — Bấm giờ thí nghiệm
        // =======================================================

        private readonly Stopwatch _stopwatch = new();
        private DispatcherTimer? _stopwatchTimer;
        private int _lapCount = 0;

        private void Stopwatch_Start(object sender, RoutedEventArgs e)
        {
            if (_stopwatch.IsRunning)
            {
                // Pause
                _stopwatch.Stop();
                _stopwatchTimer?.Stop();
                btnStopwatchStart.Content = "▶ Tiếp tục";
                btnStopwatchStart.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            }
            else
            {
                // Start/Resume
                _stopwatch.Start();
                _stopwatchTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(37) };
                _stopwatchTimer.Tick += (_, _) =>
                {
                    var ts = _stopwatch.Elapsed;
                    txtStopwatch.Text = $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
                };
                _stopwatchTimer.Start();

                btnStopwatchStart.Content = "⏸ Tạm dừng";
                btnStopwatchStart.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                btnStopwatchLap.IsEnabled = true;
            }
        }

        private void Stopwatch_Lap(object sender, RoutedEventArgs e)
        {
            if (!_stopwatch.IsRunning) return;
            _lapCount++;
            var ts = _stopwatch.Elapsed;
            var lapText = $"Lần {_lapCount}: {ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";

            var badge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 243, 224)),
                CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 4, 4)
            };
            badge.Child = new TextBlock
            {
                Text = lapText, FontSize = 11, FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0))
            };
            lapTimesPanel.Children.Insert(0, badge);
        }

        private void Stopwatch_Reset(object sender, RoutedEventArgs e)
        {
            _stopwatch.Reset();
            _stopwatchTimer?.Stop();
            txtStopwatch.Text = "00:00.000";
            btnStopwatchStart.Content = "▶ Bắt đầu";
            btnStopwatchStart.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            btnStopwatchLap.IsEnabled = false;
            lapTimesPanel.Children.Clear();
            _lapCount = 0;
        }

        // =======================================================
        //  TAB 6: ĐỒ THỊ NHANH — Quick Graph (Desmos)
        // =======================================================

        private void DrawGraph_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string expr1 = txtExpr1?.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(expr1))
                {
                    MessageBox.Show("Vui lòng nhập biểu thức hàm số.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string expr2 = txtExpr2?.Text?.Trim() ?? "";
                var ci = System.Globalization.CultureInfo.InvariantCulture;

                string expJs = $@"
        calc.setExpression({{id:'f1', latex:'y={expr1}', color:'#1565C0', lineWidth:3}});
        calc.setExpression({{id:'xaxis', latex:'y=0', color:'#999', lineWidth:1}});";

                if (!string.IsNullOrEmpty(expr2))
                    expJs += $"\n        calc.setExpression({{id:'f2', latex:'y={expr2}', color:'#E65100', lineWidth:3}});";

                expJs += "\n        calc.setMathBounds({ left: -10, right: 10, bottom: -10, top: 10 });";

                string title = string.IsNullOrEmpty(expr2) ? $"📊 y = {expr1}" : $"📊 y = {expr1}  vs  y = {expr2}";
                var win = new QASmartClass.LearningTools.Views.Math.GraphWindow(expJs, title);
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // =======================================================
        //  TAB 7: CÂN BẰNG PTHH
        // =======================================================

        private void BalanceChem_Click(object sender, RoutedEventArgs e)
        {
            chemErrorPanel.Visibility = Visibility.Collapsed;
            chemResultPanel.Visibility = Visibility.Collapsed;
            if (chemEmptyState != null) chemEmptyState.Visibility = Visibility.Visible;

            string input = txtChemEquation?.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(input))
            {
                ShowChemError("Vui lòng nhập phương trình hóa học.");
                return;
            }

            // Normalize arrow
            input = input.Replace("→", "->").Replace("=>", "->").Replace("=", "->");
            var sides = input.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
            if (sides.Length != 2)
            {
                ShowChemError("Vui lòng dùng -> để phân tách chất tham gia và sản phẩm.\nVD: Fe + O2 -> Fe2O3");
                return;
            }

            try
            {
                var reactants = sides[0].Split('+').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
                var products = sides[1].Split('+').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

                // ═══ VALIDATION 1: Kiểm tra có chất tham gia và sản phẩm ═══
                if (reactants.Count == 0 || products.Count == 0)
                {
                    ShowChemError("⚠️ Phương trình phải có ít nhất 1 chất tham gia VÀ 1 sản phẩm.\nVD: Fe + O2 -> Fe2O3");
                    return;
                }

                // ═══ VALIDATION 2: Kiểm tra ký tự không hợp lệ ═══
                var allCompounds = reactants.Concat(products).ToList();
                foreach (var compound in allCompounds)
                {
                    // Chỉ cho phép: chữ cái, số, (, )
                    var invalidChars = compound.Where(c => !char.IsLetterOrDigit(c) && c != '(' && c != ')').ToList();
                    if (invalidChars.Count > 0)
                    {
                        ShowChemError($"⚠️ Công thức \"{compound}\" chứa ký tự không hợp lệ: '{string.Join("', '", invalidChars.Distinct())}'\n\nChỉ sử dụng: chữ cái (H, O, Fe...), số (2, 3...) và dấu ngoặc ().");
                        return;
                    }
                    // Kiểm tra có bắt đầu bằng chữ hoa
                    if (compound.Length > 0 && !char.IsUpper(compound[0]))
                    {
                        ShowChemError($"⚠️ Công thức \"{compound}\" không hợp lệ.\nCông thức hóa học phải bắt đầu bằng ký hiệu nguyên tố (chữ hoa).\nVD: Fe, NaCl, H2SO4");
                        return;
                    }
                    // Kiểm tra ngoặc đóng mở
                    int depth = 0;
                    foreach (char c in compound)
                    {
                        if (c == '(') depth++;
                        else if (c == ')') depth--;
                        if (depth < 0) break;
                    }
                    if (depth != 0)
                    {
                        ShowChemError($"⚠️ Công thức \"{compound}\" có ngoặc không khớp.\nKiểm tra lại dấu ( và ).\nVD: Ca(OH)2, Fe2(SO4)3");
                        return;
                    }
                }

                // ═══ VALIDATION 3: Kiểm tra nguyên tố hợp lệ (bảng tuần hoàn) ═══
                var validElements = new HashSet<string> {
                    "H","He","Li","Be","B","C","N","O","F","Ne",
                    "Na","Mg","Al","Si","P","S","Cl","Ar",
                    "K","Ca","Sc","Ti","V","Cr","Mn","Fe","Co","Ni","Cu","Zn",
                    "Ga","Ge","As","Se","Br","Kr",
                    "Rb","Sr","Y","Zr","Nb","Mo","Tc","Ru","Rh","Pd","Ag","Cd",
                    "In","Sn","Sb","Te","I","Xe",
                    "Cs","Ba","La","Ce","Pr","Nd","Pm","Sm","Eu","Gd","Tb","Dy",
                    "Ho","Er","Tm","Yb","Lu","Hf","Ta","W","Re","Os","Ir","Pt",
                    "Au","Hg","Tl","Pb","Bi","Po","At","Rn",
                    "Fr","Ra","Ac","Th","Pa","U","Np","Pu"
                };

                int n = allCompounds.Count;

                // Parse elements for each compound
                var elements = new HashSet<string>();
                var compositions = new List<Dictionary<string, int>>();
                foreach (var compound in allCompounds)
                {
                    var comp = ParseCompound(compound);
                    if (comp.Count == 0)
                    {
                        ShowChemError($"⚠️ Không thể phân tích công thức \"{compound}\".\nKiểm tra lại cú pháp.\nVD: H2O, NaCl, Ca(OH)2");
                        return;
                    }
                    // Kiểm tra từng nguyên tố có trong bảng tuần hoàn
                    foreach (var el in comp.Keys)
                    {
                        if (!validElements.Contains(el))
                        {
                            ShowChemError($"⚠️ \"{el}\" không phải nguyên tố hóa học hợp lệ!\n\nTìm thấy trong công thức \"{compound}\".\nKiểm tra lại ký hiệu nguyên tố (viết hoa chữ đầu).\nVD: Fe (sắt), Na (natri), Cl (clo) — không phải FE, NA, CL.");
                            return;
                        }
                    }
                    compositions.Add(comp);
                    foreach (var el in comp.Keys) elements.Add(el);
                }

                // ═══ VALIDATION 4: Kiểm tra nguyên tố 2 vế ═══
                var leftElements = new HashSet<string>();
                var rightElements = new HashSet<string>();
                for (int i = 0; i < reactants.Count; i++)
                    foreach (var el in compositions[i].Keys) leftElements.Add(el);
                for (int i = 0; i < products.Count; i++)
                    foreach (var el in compositions[reactants.Count + i].Keys) rightElements.Add(el);

                var onlyLeft = leftElements.Except(rightElements).ToList();
                var onlyRight = rightElements.Except(leftElements).ToList();

                if (onlyLeft.Count > 0 || onlyRight.Count > 0)
                {
                    var msg = "⚠️ Phương trình vi phạm định luật bảo toàn nguyên tố!\n\n";
                    if (onlyLeft.Count > 0)
                        msg += $"• Nguyên tố chỉ có ở VẾ TRÁI: {string.Join(", ", onlyLeft)}\n";
                    if (onlyRight.Count > 0)
                        msg += $"• Nguyên tố chỉ có ở VẾ PHẢI: {string.Join(", ", onlyRight)}\n";
                    msg += "\nMọi nguyên tố ở vế trái phải xuất hiện ở vế phải và ngược lại.";
                    ShowChemError(msg);
                    return;
                }

                var elemList = elements.OrderBy(x => x).ToList();
                int m = elemList.Count;

                // Brute force balance (coefficients 1-20)
                int[] bestCoeffs = null;
                int maxCoeff = 20;
                int[] coeffs = new int[n];

                bool TryBalance(int idx)
                {
                    if (idx == n)
                    {
                        // Check conservation
                        foreach (var el in elemList)
                        {
                            int left = 0, right = 0;
                            for (int i = 0; i < reactants.Count; i++)
                                left += coeffs[i] * (compositions[i].ContainsKey(el) ? compositions[i][el] : 0);
                            for (int i = 0; i < products.Count; i++)
                                right += coeffs[reactants.Count + i] * (compositions[reactants.Count + i].ContainsKey(el) ? compositions[reactants.Count + i][el] : 0);
                            if (left != right) return false;
                        }
                        bestCoeffs = (int[])coeffs.Clone();
                        return true;
                    }
                    for (int c = 1; c <= maxCoeff; c++)
                    {
                        coeffs[idx] = c;
                        if (TryBalance(idx + 1)) return true;
                    }
                    return false;
                }

                if (!TryBalance(0))
                {
                    ShowChemError("Không thể cân bằng phương trình này.\nVui lòng kiểm tra lại công thức hóa học.");
                    return;
                }

                // Display result
                var leftParts = new List<string>();
                for (int i = 0; i < reactants.Count; i++)
                    leftParts.Add(bestCoeffs[i] == 1 ? reactants[i] : $"{bestCoeffs[i]}{reactants[i]}");

                var rightParts = new List<string>();
                for (int i = 0; i < products.Count; i++)
                    rightParts.Add(bestCoeffs[reactants.Count + i] == 1 ? products[i] : $"{bestCoeffs[reactants.Count + i]}{products[i]}");

                txtChemResult.Text = string.Join(" + ", leftParts) + "  →  " + string.Join(" + ", rightParts);
                chemResultPanel.Visibility = Visibility.Visible;
                if (chemEmptyState != null) chemEmptyState.Visibility = Visibility.Collapsed;

                // Verification table
                chemVerifyPanel.Children.Clear();
                foreach (var el in elemList)
                {
                    int left = 0, right = 0;
                    for (int i = 0; i < reactants.Count; i++)
                        left += bestCoeffs[i] * (compositions[i].ContainsKey(el) ? compositions[i][el] : 0);
                    for (int i = 0; i < products.Count; i++)
                        right += bestCoeffs[reactants.Count + i] * (compositions[reactants.Count + i].ContainsKey(el) ? compositions[reactants.Count + i][el] : 0);

                    bool ok = left == right;
                    var tb = new TextBlock
                    {
                        Text = $"{(ok ? "✅" : "❌")}  {el}: Trái = {left}, Phải = {right}",
                        FontSize = 13, Foreground = new SolidColorBrush(ok ? Color.FromRgb(46, 125, 50) : Color.FromRgb(198, 40, 40)),
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                    chemVerifyPanel.Children.Add(tb);
                }
            }
            catch (Exception ex)
            {
                ShowChemError($"Lỗi phân tích: {ex.Message}\nVui lòng kiểm tra lại công thức.");
            }
        }

        private static Dictionary<string, int> ParseCompound(string formula)
        {
            var result = new Dictionary<string, int>();
            ParseGroup(formula, 0, formula.Length, 1, result);
            return result;
        }

        private static int ParseGroup(string s, int start, int end, int multiplier, Dictionary<string, int> result)
        {
            int i = start;
            while (i < end)
            {
                if (s[i] == '(')
                {
                    // Find matching ')'
                    int depth = 1, j = i + 1;
                    while (j < end && depth > 0) { if (s[j] == '(') depth++; else if (s[j] == ')') depth--; j++; }
                    // Read number after ')'
                    int num = 0, k = j;
                    while (k < end && char.IsDigit(s[k])) { num = num * 10 + (s[k] - '0'); k++; }
                    if (num == 0) num = 1;
                    ParseGroup(s, i + 1, j - 1, multiplier * num, result);
                    i = k;
                }
                else if (char.IsUpper(s[i]))
                {
                    int j = i + 1;
                    while (j < end && char.IsLower(s[j])) j++;
                    string element = s[i..j];
                    int num = 0;
                    while (j < end && char.IsDigit(s[j])) { num = num * 10 + (s[j] - '0'); j++; }
                    if (num == 0) num = 1;
                    if (!result.ContainsKey(element)) result[element] = 0;
                    result[element] += multiplier * num;
                    i = j;
                }
                else i++;
            }
            return i;
        }

        private void ShowChemError(string msg)
        {
            txtChemError.Text = msg;
            chemErrorPanel.Visibility = Visibility.Visible;
            chemResultPanel.Visibility = Visibility.Collapsed;
            if (chemEmptyState != null) chemEmptyState.Visibility = Visibility.Collapsed;
        }

        // =======================================================
        //  TAB 8: BẢNG TRA STEM
        // =======================================================

        private void StemTab_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border tab || tab.Tag is not string category) return;
            var tabs = new[] { tabTrig, tabSquare, tabLog, tabPow2 };
            foreach (var t in tabs)
            {
                if (t == null) continue;
                bool active = t.Tag?.ToString() == category;
                t.Background = new SolidColorBrush(active ? Color.FromRgb(237, 231, 246) : Color.FromRgb(245, 245, 245));
                if (t.Child is TextBlock tb)
                {
                    tb.Foreground = new SolidColorBrush(active ? Color.FromRgb(69, 39, 160) : Color.FromRgb(117, 117, 117));
                    tb.FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal;
                }
            }
            LoadStemTable(category);
        }

        private void LoadStemTable(string category)
        {
            if (stemTablePanel == null) return;
            stemTablePanel.Children.Clear();

            switch (category)
            {
                case "Trig":
                    int[] angles = { 0, 15, 30, 45, 60, 90, 120, 135, 150, 180, 270, 360 };
                    foreach (var a in angles)
                    {
                        double rad = a * Math.PI / 180;
                        string sinV = a == 90 || a == 270 ? (a == 90 ? "1" : "-1") : Math.Sin(rad).ToString("F5");
                        string cosV = a == 90 || a == 270 ? "0" : Math.Cos(rad).ToString("F5");
                        string tanV = (a == 90 || a == 270) ? "∞" : Math.Tan(rad).ToString("F5");
                        bool special = a == 0 || a == 30 || a == 45 || a == 60 || a == 90;
                        AddStemCard($"{a}°", $"sin={sinV}\ncos={cosV}\ntan={tanV}", special ? "#EDE7F6" : "#FAFAFA", "#4527A0");
                    }
                    break;
                case "Square":
                    for (int n = 1; n <= 30; n++)
                        AddStemCard($"{n}", $"n² = {n * n}\n√{n} = {Math.Sqrt(n):F5}", n <= 10 ? "#E3F2FD" : "#FAFAFA", "#1565C0");
                    break;
                case "Log":
                    int[] vals = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 20, 50, 100 };
                    foreach (var v in vals)
                        AddStemCard($"{v}", $"log₁₀ = {Math.Log10(v):F5}\nln = {Math.Log(v):F5}", "#FFF8E1", "#E65100");
                    break;
                case "Pow2":
                    for (int n = 0; n <= 20; n++)
                        AddStemCard($"2^{n}", $"= {(long)Math.Pow(2, n):N0}", "#E8F5E9", "#2E7D32");
                    break;
            }
        }

        private void AddStemCard(string title, string value, string bgHex, string fgHex)
        {
            var bg = (Color)ColorConverter.ConvertFromString(bgHex);
            var fg = (Color)ColorConverter.ConvertFromString(fgHex);
            var card = new Border
            {
                Background = new SolidColorBrush(bg), CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 10, 10),
                Width = 160, Cursor = Cursors.Hand,
                BorderBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)), BorderThickness = new Thickness(1),
                ToolTip = "Click để copy"
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(fg), FontFamily = new FontFamily("Cambria Math, Segoe UI") });
            sp.Children.Add(new TextBlock { Text = value, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)), Margin = new Thickness(0, 4, 0, 0), FontFamily = new FontFamily("Segoe UI") });
            card.Child = sp;

            string copyVal = value.Replace("\n", " | ");
            card.MouseLeftButtonDown += (_, _) =>
            {
                try { Clipboard.SetText($"{title}: {copyVal}"); } catch { }
                card.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                card.BorderThickness = new Thickness(2);
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                timer.Tick += (_, _) => { card.BorderBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)); card.BorderThickness = new Thickness(1); timer.Stop(); };
                timer.Start();
            };
            stemTablePanel.Children.Add(card);
        }

        // =======================================================
        //  TAB 9: THỐNG KÊ & BIỂU ĐỒ
        // =======================================================

        private void StatsAddRow_Click(object sender, RoutedEventArgs e) => AddStatsRow("", "");

        private void AddStatsRow(string name, string val)
        {
            if (statsInputPanel == null) return;
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };

            // ——— Name TextBox (text) ———
            var txtName = new TextBox
            {
                Width = 100, FontSize = 13, Padding = new Thickness(8, 6, 8, 6),
                Text = name, Margin = new Thickness(0, 0, 4, 0),
                ToolTip = "Tên danh mục (VD: An, T2, Chậu 1...)",
                FontFamily = new FontFamily("Segoe UI")
            };
            // Highlight on focus for touch
            txtName.GotFocus += (_, _) => txtName.BorderBrush = new SolidColorBrush(Color.FromRgb(21, 101, 192));
            txtName.LostFocus += (_, _) => txtName.ClearValue(TextBox.BorderBrushProperty);
            // Bàn phím text cảm ứng cho nhập tên
            QASmartClass.LearningTools.Controls.TouchTextPad.Attach(txtName, mode: "text");
            row.Children.Add(txtName);

            // ——— Value TextBox (number) — attached TouchNumPad ———
            var txtVal = new TextBox
            {
                Width = 80, FontSize = 13, Padding = new Thickness(8, 6, 8, 6),
                Text = val, TextAlignment = TextAlignment.Center,
                ToolTip = "Nhập giá trị số — chạm để mở bàn phím số",
                FontFamily = new FontFamily("Segoe UI")
            };
            txtVal.GotFocus += (_, _) => txtVal.BorderBrush = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            txtVal.LostFocus += (_, _) => txtVal.ClearValue(TextBox.BorderBrushProperty);
            // Attach TouchNumPad cho nhập liệu bằng màn hình cảm ứng
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtVal, step: 1, allowDecimal: true, allowNegative: false);
            row.Children.Add(txtVal);

            // ——— Delete button ———
            var del = new Button
            {
                Content = "✕", FontSize = 12, Cursor = Cursors.Hand,
                Width = 32, Height = 32,
                Margin = new Thickness(4, 0, 0, 0),
                ToolTip = "Xóa dòng này"
            };
            del.Click += (_, _) => statsInputPanel.Children.Remove(row);
            row.Children.Add(del);

            statsInputPanel.Children.Add(row);
        }

        private void ChartType_Changed(object sender, SelectionChangedEventArgs e) { }

        private void StatsCalc_Click(object sender, RoutedEventArgs e)
        {
            var names = new List<string>();
            var values = new List<double>();

            foreach (var child in statsInputPanel.Children)
            {
                if (child is StackPanel row && row.Children.Count >= 2)
                {
                    string n = (row.Children[0] as TextBox)?.Text ?? "";
                    string v = (row.Children[1] as TextBox)?.Text ?? "";
                    if (double.TryParse(v, out double val))
                    {
                        names.Add(string.IsNullOrWhiteSpace(n) ? $"#{names.Count + 1}" : n);
                        values.Add(val);
                    }
                }
            }

            if (values.Count == 0)
            {
                MessageBox.Show("Vui lòng nhập ít nhất 1 giá trị số.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Calculate stats
            double mean = values.Average();
            var sorted = values.OrderBy(x => x).ToList();
            double median = sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2.0;
            double mode = values.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;
            double variance = values.Average(x => (x - mean) * (x - mean));
            double stddev = Math.Sqrt(variance);

            // Show stats
            statsValuesPanel.Children.Clear();
            void AddStat(string label, string val, string color)
            {
                var b = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 8, 4) };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = label, FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)) });
                sp.Children.Add(new TextBlock { Text = val, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)) });
                b.Child = sp;
                statsValuesPanel.Children.Add(b);
            }
            AddStat("Mean (TB)", mean.ToString("F2"), "#E3F2FD");
            AddStat("Median (TrV)", median.ToString("F2"), "#E8F5E9");
            AddStat("Mode (YV)", mode.ToString("F2"), "#FFF3E0");
            AddStat("σ (Std Dev)", stddev.ToString("F2"), "#F3E5F5");
            AddStat("Variance", variance.ToString("F2"), "#FCE4EC");
            AddStat("Min", sorted.First().ToString("F2"), "#E0F7FA");
            AddStat("Max", sorted.Last().ToString("F2"), "#FBE9E7");
            statsResultPanel.Visibility = Visibility.Visible;

            // Draw chart
            DrawChart(names, values);
        }

        private Color[] GetPalette()
        {
            int theme = cboColorTheme?.SelectedIndex ?? 0;
            return theme switch
            {
                1 => new[] { // Sinh động
                    Color.FromRgb(244, 67, 54), Color.FromRgb(33, 150, 243), Color.FromRgb(76, 175, 80),
                    Color.FromRgb(255, 193, 7), Color.FromRgb(156, 39, 176), Color.FromRgb(0, 188, 212),
                    Color.FromRgb(255, 87, 34), Color.FromRgb(63, 81, 181)
                },
                2 => new[] { // Pastel
                    Color.FromRgb(144, 202, 249), Color.FromRgb(165, 214, 167), Color.FromRgb(255, 204, 128),
                    Color.FromRgb(206, 147, 216), Color.FromRgb(239, 154, 154), Color.FromRgb(128, 222, 234),
                    Color.FromRgb(255, 171, 145), Color.FromRgb(159, 168, 218)
                },
                3 => new[] { // Ấm
                    Color.FromRgb(230, 81, 0), Color.FromRgb(198, 40, 40), Color.FromRgb(255, 143, 0),
                    Color.FromRgb(191, 54, 12), Color.FromRgb(245, 124, 0), Color.FromRgb(183, 28, 28),
                    Color.FromRgb(255, 111, 0), Color.FromRgb(211, 47, 47)
                },
                _ => new[] { // Chuyên nghiệp (default)
                    Color.FromRgb(25, 118, 210), Color.FromRgb(46, 125, 50), Color.FromRgb(230, 81, 0),
                    Color.FromRgb(123, 31, 162), Color.FromRgb(198, 40, 40), Color.FromRgb(0, 137, 123),
                    Color.FromRgb(255, 143, 0), Color.FromRgb(69, 90, 100)
                }
            };
        }

        private void DrawMeanLine(double mean, double max, double baseY, double chartH, double w, double padL)
        {
            if (chkMeanLine?.IsChecked != true) return;
            double meanY = baseY - (mean / max) * chartH;
            var line = new System.Windows.Shapes.Line
            {
                X1 = padL - 5, Y1 = meanY, X2 = w - 10, Y2 = meanY,
                Stroke = new SolidColorBrush(Color.FromRgb(211, 47, 47)),
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection(new[] { 6.0, 3.0 })
            };
            chartCanvas.Children.Add(line);
            var label = new TextBlock
            {
                Text = $"TB: {mean:F1}",
                FontSize = 9, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47)),
                Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                Padding = new Thickness(3, 1, 3, 1)
            };
            Canvas.SetLeft(label, w - 60); Canvas.SetTop(label, meanY - 14);
            chartCanvas.Children.Add(label);
        }

        private void DrawChart(List<string> names, List<double> values)
        {
            if (chartCanvas == null) return;
            chartCanvas.Children.Clear();

            double w = chartCanvas.ActualWidth > 0 ? chartCanvas.ActualWidth : 600;
            double h = chartCanvas.ActualHeight > 0 ? chartCanvas.ActualHeight : 280;
            int type = cboChartType?.SelectedIndex ?? 0;
            bool showVals = chkShowValues?.IsChecked ?? true;
            bool showLabels = chkShowLabels?.IsChecked ?? true;
            Color[] palette = GetPalette();
            double mean = values.Average();

            if (type == 0) // Bar chart
            {
                double max = values.Max();
                if (max <= 0) max = 1;
                double barW = Math.Min(60, (w - 60) / values.Count - 8);
                double baseY = h - 30;
                double chartH = h - 60;

                for (int i = 0; i < values.Count; i++)
                {
                    double barH = (values[i] / max) * chartH;
                    double x = 40 + i * (barW + 8);
                    var rect = new System.Windows.Shapes.Rectangle
                    {
                        Width = barW, Height = barH,
                        Fill = new SolidColorBrush(palette[i % palette.Length]),
                        RadiusX = 4, RadiusY = 4
                    };
                    Canvas.SetLeft(rect, x); Canvas.SetTop(rect, baseY - barH);
                    chartCanvas.Children.Add(rect);

                    if (showVals)
                    {
                        var valTb = new TextBlock { Text = values[i].ToString("F1"), FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)) };
                        Canvas.SetLeft(valTb, x + barW / 2 - 12); Canvas.SetTop(valTb, baseY - barH - 16);
                        chartCanvas.Children.Add(valTb);
                    }
                    if (showLabels)
                    {
                        var nameTb = new TextBlock { Text = names[i], FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)), MaxWidth = barW + 4, TextTrimming = TextTrimming.CharacterEllipsis };
                        Canvas.SetLeft(nameTb, x); Canvas.SetTop(nameTb, baseY + 4);
                        chartCanvas.Children.Add(nameTb);
                    }
                }
                DrawMeanLine(mean, max, baseY, chartH, w, 40);
            }
            else if (type == 1) // Pie chart
            {
                double total = values.Sum();
                if (total <= 0) return;
                double cx = w / 2, cy = h / 2, r = Math.Min(w, h) / 2 - 40;
                double startAngle = 0;

                for (int i = 0; i < values.Count; i++)
                {
                    double sweepAngle = values[i] / total * 360;
                    var path = new System.Windows.Shapes.Path { Fill = new SolidColorBrush(palette[i % palette.Length]) };
                    var fig = new PathFigure { StartPoint = new Point(cx, cy) };
                    double sa = startAngle * Math.PI / 180;
                    double ea = (startAngle + sweepAngle) * Math.PI / 180;
                    fig.Segments.Add(new LineSegment(new Point(cx + r * Math.Cos(sa), cy + r * Math.Sin(sa)), true));
                    fig.Segments.Add(new ArcSegment(new Point(cx + r * Math.Cos(ea), cy + r * Math.Sin(ea)), new Size(r, r), 0, sweepAngle > 180, SweepDirection.Clockwise, true));
                    fig.IsClosed = true;
                    path.Data = new PathGeometry(new[] { fig });
                    chartCanvas.Children.Add(path);

                    if (showLabels || showVals)
                    {
                        double midA = (startAngle + sweepAngle / 2) * Math.PI / 180;
                        string label = showLabels && showVals ? $"{names[i]} ({values[i] / total * 100:F0}%)"
                            : showVals ? $"{values[i] / total * 100:F0}%" : names[i];
                        var lb = new TextBlock { Text = label, FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)) };
                        Canvas.SetLeft(lb, cx + (r * 0.65) * Math.Cos(midA) - 20);
                        Canvas.SetTop(lb, cy + (r * 0.65) * Math.Sin(midA) - 8);
                        chartCanvas.Children.Add(lb);
                    }
                    startAngle += sweepAngle;
                }
            }
            else // Line chart
            {
                double max = values.Max(), min = values.Min();
                double range = max - min;
                if (range < 0.001) { max += 1; min -= 1; range = 2; }
                min -= range * 0.1; max += range * 0.1; range = max - min;

                double padL = 50, padR = 20, padT = 20, padB = 40;
                double plotW = w - padL - padR, plotH = h - padT - padB;

                // Y-axis grid + labels
                for (int t = 0; t <= 5; t++)
                {
                    double val = min + t * range / 5;
                    double y = padT + (1 - (double)t / 5) * plotH;
                    chartCanvas.Children.Add(new System.Windows.Shapes.Line
                    {
                        X1 = padL, Y1 = y, X2 = w - padR, Y2 = y,
                        Stroke = new SolidColorBrush(Color.FromRgb(224, 224, 224)), StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection(new[] { 4.0, 3.0 })
                    });
                    var yLabel = new TextBlock { Text = val.ToString("F1"), FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)), TextAlignment = TextAlignment.Right, Width = 42 };
                    Canvas.SetLeft(yLabel, 2); Canvas.SetTop(yLabel, y - 7);
                    chartCanvas.Children.Add(yLabel);
                }
                // Axes
                chartCanvas.Children.Add(new System.Windows.Shapes.Line { X1 = padL, Y1 = padT, X2 = padL, Y2 = padT + plotH, Stroke = new SolidColorBrush(Color.FromRgb(189, 189, 189)), StrokeThickness = 1 });
                chartCanvas.Children.Add(new System.Windows.Shapes.Line { X1 = padL, Y1 = padT + plotH, X2 = w - padR, Y2 = padT + plotH, Stroke = new SolidColorBrush(Color.FromRgb(189, 189, 189)), StrokeThickness = 1 });

                // Mean line
                if (chkMeanLine?.IsChecked == true)
                {
                    double meanY = padT + (1 - (mean - min) / range) * plotH;
                    chartCanvas.Children.Add(new System.Windows.Shapes.Line
                    {
                        X1 = padL, Y1 = meanY, X2 = w - padR, Y2 = meanY,
                        Stroke = new SolidColorBrush(Color.FromRgb(211, 47, 47)), StrokeThickness = 1.5,
                        StrokeDashArray = new DoubleCollection(new[] { 6.0, 3.0 })
                    });
                    var ml = new TextBlock { Text = $"TB: {mean:F1}", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47)), Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)), Padding = new Thickness(3, 1, 3, 1) };
                    Canvas.SetLeft(ml, w - 70); Canvas.SetTop(ml, meanY - 14);
                    chartCanvas.Children.Add(ml);
                }

                // Lines + dots
                double step = values.Count > 1 ? plotW / (values.Count - 1) : plotW;
                for (int i = 0; i < values.Count - 1; i++)
                {
                    double x1 = padL + i * step, y1 = padT + (1 - (values[i] - min) / range) * plotH;
                    double x2 = padL + (i + 1) * step, y2 = padT + (1 - (values[i + 1] - min) / range) * plotH;
                    chartCanvas.Children.Add(new System.Windows.Shapes.Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = new SolidColorBrush(palette[0]), StrokeThickness = 2.5 });
                }
                for (int i = 0; i < values.Count; i++)
                {
                    double x = padL + i * step, y = padT + (1 - (values[i] - min) / range) * plotH;
                    var dot = new System.Windows.Shapes.Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(palette[0]), Stroke = Brushes.White, StrokeThickness = 2 };
                    Canvas.SetLeft(dot, x - 4); Canvas.SetTop(dot, y - 4);
                    chartCanvas.Children.Add(dot);
                    if (showVals)
                    {
                        var vt = new TextBlock { Text = values[i].ToString("F1"), FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(palette[0]), TextAlignment = TextAlignment.Center, Width = 40 };
                        Canvas.SetLeft(vt, x - 20); Canvas.SetTop(vt, y - 18);
                        chartCanvas.Children.Add(vt);
                    }
                    if (showLabels)
                    {
                        var nt = new TextBlock { Text = names[i], FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)), TextAlignment = TextAlignment.Center, Width = 50, TextTrimming = TextTrimming.CharacterEllipsis };
                        Canvas.SetLeft(nt, x - 25); Canvas.SetTop(nt, padT + plotH + 6);
                        chartCanvas.Children.Add(nt);
                    }
                }
            }
        }

        // ——— Nhóm 1: Toán & Điểm số ———
        private void StatsPreset_Scores(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            var data = new[] {
                ("An", "8"), ("Bình", "6.5"), ("Châu", "9"), ("Dũng", "5"),
                ("Em", "7.5"), ("Giang", "4"), ("Hà", "8.5"), ("Khoa", "7"),
                ("Lan", "6"), ("Minh", "9.5")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        private void StatsPreset_Quiz15(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            var data = new[] {
                ("HS 1", "8"), ("HS 2", "5"), ("HS 3", "7"), ("HS 4", "9"),
                ("HS 5", "6"), ("HS 6", "10"), ("HS 7", "4"), ("HS 8", "7"),
                ("HS 9", "8"), ("HS 10", "6"), ("HS 11", "9"), ("HS 12", "5")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        private void StatsPreset_HK1(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            var data = new[] {
                ("Hùng", "7.2"), ("Trang", "8.5"), ("Nam", "6.0"), ("Linh", "9.1"),
                ("Đức", "5.8"), ("Hoa", "7.8"), ("Tuấn", "8.0"), ("Mai", "6.5"),
                ("Phong", "7.5"), ("Ngọc", "8.8")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        // ——— Nhóm 2: Khoa học tự nhiên ———
        private void StatsPreset_Temp(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            var data = new[] {
                ("T2", "28"), ("T3", "30"), ("T4", "32"), ("T5", "29"),
                ("T6", "27"), ("T7", "31"), ("CN", "33")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        private void StatsPreset_Concentration(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            // Nồng độ dung dịch NaCl (%) trong 6 lần thí nghiệm
            var data = new[] {
                ("TN 1", "5.2"), ("TN 2", "4.8"), ("TN 3", "5.0"), ("TN 4", "5.5"),
                ("TN 5", "4.9"), ("TN 6", "5.1")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        private void StatsPreset_PlantHeight(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            // Chiều cao cây đậu xanh sau 14 ngày (cm)
            var data = new[] {
                ("Chậu 1", "12.5"), ("Chậu 2", "14.2"), ("Chậu 3", "10.8"),
                ("Chậu 4", "15.0"), ("Chậu 5", "11.3"), ("Chậu 6", "13.7"),
                ("Chậu 7", "16.1"), ("Chậu 8", "9.5")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        private void StatsPreset_Resistance(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            // Đo điện trở (Ω) của 7 dây dẫn trong bài thực hành Vật lý 11
            var data = new[] {
                ("Dây 1", "10"), ("Dây 2", "15"), ("Dây 3", "22"),
                ("Dây 4", "18"), ("Dây 5", "12"), ("Dây 6", "25"), ("Dây 7", "20")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        // ——— Nhóm 3: Xã hội & Đời sống ———
        private void StatsPreset_ClassSize(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            var data = new[] {
                ("10A1", "42"), ("10A2", "40"), ("10A3", "38"),
                ("10A4", "45"), ("10A5", "41"), ("10A6", "39")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        private void StatsPreset_StudyHours(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            // Số giờ tự học/tuần của 8 HS lớp 11
            var data = new[] {
                ("Anh", "12"), ("Bảo", "8"), ("Cường", "15"), ("Dương", "6"),
                ("Hải", "10"), ("Khánh", "14"), ("Liên", "9"), ("Phúc", "11")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        private void StatsPreset_Spending(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            var data = new[] {
                ("T2", "25"), ("T3", "30"), ("T4", "15"), ("T5", "35"),
                ("T6", "20"), ("T7", "50"), ("CN", "45")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        private void StatsPreset_BooksRead(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            // Số sách đọc/tháng của HS lớp 10
            var data = new[] {
                ("Hương", "4"), ("Tuấn", "2"), ("Linh", "6"), ("Đạt", "1"),
                ("Mai", "5"), ("Khôi", "3"), ("Vy", "7"), ("Bảo", "2"),
                ("Ngân", "4"), ("Phúc", "3")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        // ——— Nhóm 4: Sức khỏe & Thể dục ———
        private void StatsPreset_HeartRate(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            // Nhịp tim (lần/phút) sau chạy 200m
            var data = new[] {
                ("An", "120"), ("Bình", "135"), ("Châu", "110"), ("Dũng", "145"),
                ("Em", "125"), ("Giang", "140"), ("Hà", "115"), ("Khoa", "130")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        private void StatsPreset_Weight(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            // Cân nặng HS lớp 10 (kg)
            var data = new[] {
                ("HS 1", "48"), ("HS 2", "52"), ("HS 3", "45"), ("HS 4", "55"),
                ("HS 5", "50"), ("HS 6", "58"), ("HS 7", "43"), ("HS 8", "51"),
                ("HS 9", "47"), ("HS 10", "54")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        private void StatsPreset_Sprint(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            // Thành tích chạy 100m (giây)
            var data = new[] {
                ("An", "14.2"), ("Bình", "13.5"), ("Châu", "15.1"), ("Dũng", "12.8"),
                ("Em", "14.7"), ("Giang", "13.9"), ("Hà", "16.0"), ("Khoa", "13.2")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        private void StatsPreset_StudentHeight(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            // Chiều cao HS lớp 11 (cm)
            var data = new[] {
                ("HS 1", "162"), ("HS 2", "170"), ("HS 3", "155"), ("HS 4", "168"),
                ("HS 5", "175"), ("HS 6", "158"), ("HS 7", "165"), ("HS 8", "172"),
                ("HS 9", "160"), ("HS 10", "163")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        // ——— Nhóm 5: Môi trường & Địa lý ———
        private void StatsPreset_Rainfall(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            // Lượng mưa TB (mm) 6 tháng đầu năm tại TP.HCM
            var data = new[] {
                ("T1", "14"), ("T2", "5"), ("T3", "12"), ("T4", "50"),
                ("T5", "220"), ("T6", "310")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        private void StatsPreset_AirQuality(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            // Chỉ số AQI 7 ngày (Hà Nội)
            var data = new[] {
                ("T2", "85"), ("T3", "120"), ("T4", "95"), ("T5", "150"),
                ("T6", "75"), ("T7", "110"), ("CN", "65")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        private void StatsPreset_Population(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            // Dân số một số tỉnh (triệu người, 2024)
            var data = new[] {
                ("HCM", "9.5"), ("HN", "8.4"), ("ĐN", "1.2"), ("HP", "2.1"),
                ("CT", "1.3"), ("BD", "2.6"), ("ĐL", "2.2"), ("TH", "3.7")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        private void StatsPreset_RiceYield(object sender, RoutedEventArgs e)
        {
            statsInputPanel.Children.Clear();
            // Sản lượng lúa (tấn/ha) theo vùng ĐBSCL
            var data = new[] {
                ("AG", "6.8"), ("KG", "6.5"), ("ĐT", "7.1"), ("VL", "6.2"),
                ("CT", "5.9"), ("BL", "6.4"), ("ST", "6.7"), ("TG", "7.0")
            };
            foreach (var (n, v) in data) AddStatsRow(n, v);
        }

        // =======================================================
        //  PRESET BUILDERS
        // =======================================================

        private void BuildGraphPresets()
        {
            if (graphPresetPanel == null) return;

            // Nhóm 1: Đa thức
            AddPresetGroupLabel(graphPresetPanel, "📐 Đa thức");
            var polyPresets = new[] {
                ("y = x²", "x^2"), ("y = -x²", "-x^2"), ("y = x³", "x^3"),
                ("y = 2x + 1", "2x+1"), ("y = -3x + 5", "-3x+5"), ("y = x⁴ - x²", "x^4-x^2"),
            };
            foreach (var (l, e) in polyPresets) AddGraphPresetBtn(l, e);

            // Nhóm 2: Lượng giác
            AddPresetGroupLabel(graphPresetPanel, "🔄 Lượng giác");
            var trigPresets = new[] {
                ("y = sin(x)", "\\sin(x)"), ("y = cos(x)", "\\cos(x)"), ("y = tan(x)", "\\tan(x)"),
                ("y = sin(2x)", "\\sin(2x)"), ("y = 2sin(x)", "2\\sin(x)"), ("y = sin(x)+cos(x)", "\\sin(x)+\\cos(x)"),
            };
            foreach (var (l, e) in trigPresets) AddGraphPresetBtn(l, e);

            // Nhóm 3: Hàm mũ & logarit
            AddPresetGroupLabel(graphPresetPanel, "📈 Mũ & Logarit");
            var expPresets = new[] {
                ("y = 2^x", "2^x"), ("y = e^x", "e^x"), ("y = e^(-x)", "e^{-x}"),
                ("y = log(x)", "\\log(x)"), ("y = ln(x)", "\\ln(x)"), ("y = log₂(x)", "\\log_2(x)"),
            };
            foreach (var (l, e) in expPresets) AddGraphPresetBtn(l, e);

            // Nhóm 4: Hàm đặc biệt
            AddPresetGroupLabel(graphPresetPanel, "✨ Hàm đặc biệt");
            var specialPresets = new[] {
                ("y = |x|", "\\left|x\\right|"), ("y = √x", "\\sqrt{x}"), ("y = 1/x", "\\frac{1}{x}"),
                ("y = ⌊x⌋ (floor)", "\\floor(x)"), ("y = x·sin(x)", "x\\sin(x)"), ("y = 1/(1+e^(-x))", "\\frac{1}{1+e^{-x}}"),
            };
            foreach (var (l, e) in specialPresets) AddGraphPresetBtn(l, e);
        }

        private void AddGraphPresetBtn(string label, string expr)
        {
            var btn = new Button
            {
                Content = label, Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 6, 6), FontSize = 12, Cursor = Cursors.Hand,
                FontFamily = new FontFamily("Segoe UI"),
                ToolTip = $"Nhấn để điền: {expr}"
            };
            btn.Click += (_, _) => { if (_lastGraphExprBox != null) _lastGraphExprBox.Text = expr; else if (txtExpr1 != null) txtExpr1.Text = expr; };
            graphPresetPanel.Children.Add(btn);
        }

        private static void AddPresetGroupLabel(WrapPanel panel, string text)
        {
            var label = new TextBlock
            {
                Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                Margin = new Thickness(0, 6, 600, 4), // large right margin to force newline
                Width = 280
            };
            panel.Children.Add(label);
        }

        private void BuildChemPresets()
        {
            if (chemPresetPanel == null) return;

            // 6 nhóm phản ứng phổ biến — phủ CT Hóa học THCS + THPT
            var groups = new[] {
                ("🔥 Phản ứng cháy", "#E65100", new[] {
                    "C + O2 -> CO2",
                    "S + O2 -> SO2",
                    "Fe + O2 -> Fe3O4",
                    "Al + O2 -> Al2O3",
                    "Mg + O2 -> MgO",
                    "CH4 + O2 -> CO2 + H2O",
                    "C2H2 + O2 -> CO2 + H2O",
                    "C2H6 + O2 -> CO2 + H2O",
                    "C3H8 + O2 -> CO2 + H2O",
                    "C2H5OH + O2 -> CO2 + H2O",
                }),
                ("⚗️ Axit – Bazơ (Trung hòa)", "#2E7D32", new[] {
                    "HCl + NaOH -> NaCl + H2O",
                    "H2SO4 + NaOH -> Na2SO4 + H2O",
                    "HCl + Ca(OH)2 -> CaCl2 + H2O",
                    "H2SO4 + Ba(OH)2 -> BaSO4 + H2O",
                    "HNO3 + KOH -> KNO3 + H2O",
                    "H3PO4 + NaOH -> Na3PO4 + H2O",
                    "H2SO4 + Cu(OH)2 -> CuSO4 + H2O",
                    "HCl + Al(OH)3 -> AlCl3 + H2O",
                    "H2SO4 + Fe(OH)3 -> Fe2(SO4)3 + H2O",
                }),
                ("🧪 Phân hủy", "#1565C0", new[] {
                    "CaCO3 -> CaO + CO2",
                    "KClO3 -> KCl + O2",
                    "H2O2 -> H2O + O2",
                    "KMnO4 -> K2MnO4 + MnO2 + O2",
                    "Cu(OH)2 -> CuO + H2O",
                    "Fe(OH)3 -> Fe2O3 + H2O",
                    "NaHCO3 -> Na2CO3 + CO2 + H2O",
                    "Ca(HCO3)2 -> CaCO3 + CO2 + H2O",
                }),
                ("🔄 Trao đổi", "#00695C", new[] {
                    "AgNO3 + NaCl -> AgCl + NaNO3",
                    "BaCl2 + Na2SO4 -> BaSO4 + NaCl",
                    "Pb(NO3)2 + KI -> PbI2 + KNO3",
                    "CaCl2 + Na2CO3 -> CaCO3 + NaCl",
                    "Fe2O3 + HCl -> FeCl3 + H2O",
                    "CuO + H2SO4 -> CuSO4 + H2O",
                    "Na2O + H2O -> NaOH",
                    "SO3 + H2O -> H2SO4",
                    "P2O5 + H2O -> H3PO4",
                }),
                ("⚡ Oxi hóa – Khử", "#7B1FA2", new[] {
                    "H2 + O2 -> H2O",
                    "Fe + CuSO4 -> FeSO4 + Cu",
                    "Cu + AgNO3 -> Cu(NO3)2 + Ag",
                    "Zn + CuSO4 -> ZnSO4 + Cu",
                    "Fe + Cl2 -> FeCl3",
                    "Na + H2O -> NaOH + H2",
                    "Al + Fe2O3 -> Al2O3 + Fe",
                    "MnO2 + HCl -> MnCl2 + Cl2 + H2O",
                    "Fe + HNO3 -> Fe(NO3)3 + NO + H2O",
                }),
                ("🧬 Kim loại + Axit (giải phóng H₂)", "#C62828", new[] {
                    "Zn + HCl -> ZnCl2 + H2",
                    "Al + HCl -> AlCl3 + H2",
                    "Fe + H2SO4 -> FeSO4 + H2",
                    "Mg + HCl -> MgCl2 + H2",
                    "Zn + H2SO4 -> ZnSO4 + H2",
                    "Al + H2SO4 -> Al2(SO4)3 + H2",
                    "Ca + HCl -> CaCl2 + H2",
                }),
            };

            foreach (var (groupName, colorHex, equations) in groups)
            {
                var groupColor = (Color)ColorConverter.ConvertFromString(colorHex);

                // ——— Group section container ———
                var groupSection = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };

                // Header bar
                var header = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(25, groupColor.R, groupColor.G, groupColor.B)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 5, 10, 5),
                    Margin = new Thickness(0, 0, 0, 6),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, groupColor.R, groupColor.G, groupColor.B)),
                    BorderThickness = new Thickness(2, 0, 0, 0)
                };
                header.Child = new TextBlock
                {
                    Text = groupName, FontSize = 12, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(groupColor),
                    FontFamily = new FontFamily("Segoe UI")
                };
                groupSection.Children.Add(header);

                // Buttons in a WrapPanel
                var btnPanel = new WrapPanel { Margin = new Thickness(8, 0, 0, 0) };
                foreach (var eq in equations)
                {
                    var btn = new Button
                    {
                        Content = eq,
                        Padding = new Thickness(7, 3, 7, 3),
                        Margin = new Thickness(0, 0, 4, 4),
                        FontSize = 10.5,
                        Cursor = Cursors.Hand,
                        FontFamily = new FontFamily("Segoe UI"),
                        ToolTip = $"Nhấn để điền: {eq}"
                    };
                    btn.Click += (_, _) => { if (txtChemEquation != null) txtChemEquation.Text = eq; };
                    btnPanel.Children.Add(btn);
                }
                groupSection.Children.Add(btnPanel);

                chemPresetPanel.Children.Add(groupSection);
            }
        }
    }
}

