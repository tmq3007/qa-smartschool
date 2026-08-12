using System;
using System.Globalization;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using QASmartClass.LearningTools.Helpers;

namespace QASmartClass.Classroom.Views
{
    public partial class StemToolsPage : Page
    {
        private bool _isLoaded = false;
        private TextBox? _activeExprTextBox;
        private bool _isSwapping = false;

        public StemToolsPage()
        {
            InitializeComponent();
            Unloaded += (_, _) =>
            {
                try
                {
                    _stopwatchTimer?.Stop();
                    _stopwatch.Stop();
                }
                catch { }
            };
            Loaded += (_, _) =>
            {
                if (_isLoaded) return;
                _isLoaded = true;

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

                // Khởi tạo timer bấm giờ một lần duy nhất để tránh rò rỉ bộ nhớ
                _stopwatchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(37) };
                _stopwatchTimer.Tick += (_, _) =>
                {
                    var ts = _stopwatch.Elapsed;
                    if (ts.TotalHours >= 1)
                    {
                        txtStopwatch.Text = $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
                    }
                    else
                    {
                        txtStopwatch.Text = $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
                    }
                };
                
                // Bàn phím số mini cho phương trình
                QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtCoeffA, step: 1, min: -100, max: 100);
                QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtCoeffB, step: 1, min: -100, max: 100);
                QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtCoeffC, step: 1, min: -100, max: 100);

                // Bàn phím cảm ứng cho nhập PTHH
                QASmartClass.LearningTools.Controls.TouchTextPad.Attach(txtChemEquation, mode: "chem");

                // Đồ thị nhanh
                QASmartClass.LearningTools.Controls.TouchTextPad.Attach(txtExpr1, mode: "text");
                QASmartClass.LearningTools.Controls.TouchTextPad.Attach(txtExpr2, mode: "text");

                _activeExprTextBox = txtExpr1;
                txtExpr1.GotFocus += (s, e) => _activeExprTextBox = txtExpr1;
                txtExpr2.GotFocus += (s, e) => _activeExprTextBox = txtExpr2;

                // Phân quyền: Ẩn nút điều hướng và điều khiển nếu là Học sinh
                try
                {
                    if (QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudent())
                    {
                        btnBackHome.Visibility = Visibility.Collapsed;
                        btnFocusHS.Visibility = Visibility.Collapsed;
                        btnUnfocusHS.Visibility = Visibility.Collapsed;
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning("StemTools role check error: {Err}", ex.Message);
                }
            };
        }

        public void SetSelectedTab(int index)
        {
            if (mainTabControl != null && index >= 0 && index < mainTabControl.Items.Count)
            {
                mainTabControl.SelectedIndex = index;
            }
        }

        // =======================================================
        //  NAVIGATION SUB-FLOW
        // =======================================================
        private void GoBackHome_Click(object sender, RoutedEventArgs e)
        {
            // Tự động gỡ bỏ Focus cho học sinh khi thoát trang
            try
            {
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.UnfocusSectionVisual();
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.UnfocusStudents();
            }
            catch { }

            // Điều hướng quay lại Trang chủ (F1)
            var shell = Window.GetWindow(this) as ClassroomShell;
            if (shell != null)
            {
                Serilog.Log.Information("StemTools sub-flow: Navigating back to Home (F1)");
                shell.NavigateTo("F1");
            }
        }

        private void FocusHS_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string toolId = "stem_tools";
                if (mainTabControl != null)
                {
                    if (mainTabControl.SelectedIndex == 5)
                        toolId = "quickgraph";
                    else if (mainTabControl.SelectedIndex == 6)
                        toolId = "eqbalance";
                }
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.FocusStudents(toolId);
                btnFocusHS.Content = "✅ Đang Focus";
                btnFocusHS.IsEnabled = false;
                btnFocusHS.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 230, 201));
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Focus STEM tools click error: {Err}", ex.Message);
            }
        }

        private void UnfocusHS_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.UnfocusSectionVisual();
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.UnfocusStudents();
                btnFocusHS.Content = "🎯 Focus HS";
                btnFocusHS.IsEnabled = true;
                btnFocusHS.Background = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFrom("#E8F5E9")!;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Unfocus STEM tools click error: {Err}", ex.Message);
            }
        }

        // =======================================================
        //  CALCULATOR ?" Máy tính khoa học nâng cao
        // =======================================================

        private readonly List<string> _calcHistory = new();

        private void BuildCalcButtons()
        {
            if (calcButtons == null) return;
            calcButtons.Children.Clear();

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
                    if (double.TryParse(txtCalcDisplay.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
                        txtCalcDisplay.Text = (-val).ToString("G10", CultureInfo.InvariantCulture);
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

                // Replace comma with dot for decimal notation (pedagogical Vietnamese UI supports comma, but engine needs dot)
                expr = expr.Replace(",", ".");

                // Replace constants
                expr = System.Text.RegularExpressions.Regex.Replace(expr, @"\bpi\b|π", "3.14159265359", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                expr = System.Text.RegularExpressions.Regex.Replace(expr, @"\be\b", "2.71828182846", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // Check for scientific functions (case-insensitive)
                double result;
                string lowerExpr = expr.ToLowerInvariant();
                if (lowerExpr.Contains("sin(") || lowerExpr.Contains("cos(") || lowerExpr.Contains("tan(") ||
                    lowerExpr.Contains("sqrt(") || lowerExpr.Contains("log10(") || lowerExpr.Contains("ln(") ||
                    lowerExpr.Contains("^"))
                {
                    result = EvalScientific(expr);
                }
                else
                {
                    var dt = new DataTable { Locale = CultureInfo.InvariantCulture };
                    var computed = dt.Compute(expr.Replace("×", "*").Replace("÷", "/"), null);
                    result = Convert.ToDouble(computed, CultureInfo.InvariantCulture);
                }

                _lastResult = result.ToString("G10", CultureInfo.InvariantCulture);
                txtCalcDisplay.Text = FormatUiNumber(result, "G10");

                // Save to history
                _calcHistory.Insert(0, $"{txtCalcInput.Text} = {txtCalcDisplay.Text}");
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
            expr = expr.ToLowerInvariant().Replace("×", "*").Replace("÷", "/");

            // Handle functions
            bool useDeg = chkDegMode?.IsChecked == true;
            expr = EvalFunc(expr, "sin", val => Math.Sin(useDeg ? val * Math.PI / 180 : val));
            expr = EvalFunc(expr, "cos", val => Math.Cos(useDeg ? val * Math.PI / 180 : val));
            expr = EvalFunc(expr, "tan", val => Math.Tan(useDeg ? val * Math.PI / 180 : val));
            expr = EvalFunc(expr, "sqrt", Math.Sqrt);
            expr = EvalFunc(expr, "log10", Math.Log10);
            expr = EvalFunc(expr, "ln", Math.Log);

            // Handle ^ (power)
            while (expr.Contains("^"))
            {
                int idx = expr.IndexOf("^");
                
                // Find base number (left of ^)
                int baseStart = idx - 1;
                if (baseStart >= 0 && expr[baseStart] == ')')
                {
                    int depth = 1;
                    baseStart--;
                    while (baseStart >= 0 && depth > 0)
                    {
                        if (expr[baseStart] == ')') depth++;
                        else if (expr[baseStart] == '(') depth--;
                        baseStart--;
                    }
                    baseStart++;
                }
                else
                {
                    while (baseStart > 0 && (char.IsDigit(expr[baseStart - 1]) || expr[baseStart - 1] == '.' || expr[baseStart - 1] == '-'))
                    {
                        if (expr[baseStart - 1] == '-')
                        {
                            if (baseStart - 1 == 0 || "+-*/(".Contains(expr[baseStart - 2]))
                                baseStart--;
                            break;
                        }
                        baseStart--;
                    }
                }

                // Find exponent (right of ^)
                int expEnd = idx + 1;
                if (expEnd < expr.Length && expr[expEnd] == '(')
                {
                    int depth = 1;
                    expEnd++;
                    while (expEnd < expr.Length && depth > 0)
                    {
                        if (expr[expEnd] == '(') depth++;
                        else if (expr[expEnd] == ')') depth--;
                        expEnd++;
                    }
                }
                else
                {
                    if (expEnd < expr.Length && expr[expEnd] == '-') expEnd++;
                    while (expEnd < expr.Length && (char.IsDigit(expr[expEnd]) || expr[expEnd] == '.'))
                        expEnd++;
                }

                string baseStr = expr[baseStart..idx];
                double baseVal;
                try
                {
                    var dt = new DataTable { Locale = CultureInfo.InvariantCulture };
                    baseVal = Convert.ToDouble(dt.Compute(baseStr, null), CultureInfo.InvariantCulture);
                }
                catch
                {
                    if (baseStr.StartsWith("(") && baseStr.EndsWith(")"))
                        baseStr = baseStr[1..^1];
                    baseVal = double.Parse(baseStr, CultureInfo.InvariantCulture);
                }

                string expStr = expr[(idx + 1)..expEnd];
                double expVal;
                try
                {
                    var dt = new DataTable { Locale = CultureInfo.InvariantCulture };
                    expVal = Convert.ToDouble(dt.Compute(expStr, null), CultureInfo.InvariantCulture);
                }
                catch
                {
                    if (expStr.StartsWith("(") && expStr.EndsWith(")"))
                        expStr = expStr[1..^1];
                    expVal = double.Parse(expStr, CultureInfo.InvariantCulture);
                }

                double powResult = Math.Pow(baseVal, expVal);
                expr = expr[..baseStart] + powResult.ToString("G10", CultureInfo.InvariantCulture) + expr[expEnd..];
            }

            var dt2 = new DataTable { Locale = CultureInfo.InvariantCulture };
            return Convert.ToDouble(dt2.Compute(expr, null), CultureInfo.InvariantCulture);
        }

        private string EvalFunc(string expr, string funcName, Func<double, double> func)
        {
            while (expr.Contains(funcName + "("))
            {
                int start = expr.IndexOf(funcName + "(");
                int openParen = start + funcName.Length;
                
                int depth = 1;
                int closeParen = openParen + 1;
                while (closeParen < expr.Length && depth > 0)
                {
                    if (expr[closeParen] == '(') depth++;
                    else if (expr[closeParen] == ')') depth--;
                    closeParen++;
                }
                if (depth > 0) break;
                closeParen--;

                string inner = expr[(openParen + 1)..closeParen];
                double innerVal = EvalScientific(inner);

                double result = func(innerVal);
                expr = expr[..start] + result.ToString("G10", CultureInfo.InvariantCulture) + expr[(closeParen + 1)..];
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
            ["📏 Chiều dài"] = new() { ("m", 1), ("cm", 0.01), ("mm", 0.001), ("km", 1000), ("inch", 0.0254), ("ft", 0.3048), ("mile", 1609.34), ("nm", 1e-9), ("μm", 1e-6), ("AU", 1.495978707e11), ("ly", 9.46073047258e15) },
            ["⚖️ Khối lượng"] = new() { ("kg", 1), ("g", 0.001), ("mg", 1e-6), ("tấn", 1000), ("lb", 0.453592), ("oz", 0.0283495), ("troy oz", 0.0311035), ("carat", 0.0002), ("tạ", 100) },
            ["🌡️ Nhiệt độ"] = new() { ("°C", 1), ("°F", 1), ("K", 1) },
            ["📐 Diện tích"] = new() { ("m²", 1), ("cm²", 1e-4), ("km²", 1e6), ("ha", 1e4), ("acre", 4046.86), ("sào", 360) },
            ["🧊 Thể tích"] = new() { ("lít", 1), ("ml", 0.001), ("m³", 1000), ("gallon", 3.78541), ("cc", 0.001) },
            ["⏱️ Thời gian"] = new() { ("giây", 1), ("phút", 60), ("giờ", 3600), ("ngày", 86400), ("tuần", 604800), ("ms", 0.001) },
            ["⚡ Năng lượng"] = new() { ("J", 1), ("kJ", 1000), ("cal", 4.184), ("kcal", 4184), ("kWh", 3.6e6), ("eV", 1.602e-19), ("BTU", 1055.056) },
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
            if (_isSwapping) return;
            try
            {
                if (cboUnitType?.SelectedItem is not ComboBoxItem typeItem) return;
                string type = typeItem.Content?.ToString() ?? "";
                if (!Units.ContainsKey(type)) return;
                if (txtToValue == null || txtConvertFormula == null) return;

                string fromName = (cboFromUnit?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                string toName = (cboToUnit?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

                if (!ParsingHelper.TryParseDouble(txtFromValue?.Text ?? "", out double value)) { txtToValue.Text = ""; return; }

                if (panelSaoRegion != null)
                {
                    if (type == "📐 Diện tích" && (fromName == "sào" || toName == "sào"))
                    {
                        panelSaoRegion.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        panelSaoRegion.Visibility = Visibility.Collapsed;
                    }
                }

                double saoFactor = 360;
                if (cboSaoRegion?.SelectedItem is ComboBoxItem saoItem && 
                    saoItem.Tag is string tagStr && 
                    double.TryParse(tagStr, out double parsedTag))
                {
                    saoFactor = parsedTag;
                }

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
                    double fromFactor = Units[type].FirstOrDefault(u => u.Name == fromName).Factor;
                    double toFactor = Units[type].FirstOrDefault(u => u.Name == toName).Factor;

                    if (type == "📐 Diện tích")
                    {
                        if (fromName == "sào") fromFactor = saoFactor;
                        if (toName == "sào") toFactor = saoFactor;
                    }

                    if (toFactor == 0) return;
                    result = value * fromFactor / toFactor;
                }

                txtToValue.Text = FormatUiNumber(result, "G8");

                if (type == "🌡️ Nhiệt độ")
                {
                    string formula = "";
                    if (fromName == "°C" && toName == "°F") formula = "°F = °C × 1,8 + 32";
                    else if (fromName == "°F" && toName == "°C") formula = "°C = (°F − 32) / 1,8";
                    else if (fromName == "°C" && toName == "K") formula = "K = °C + 273,15";
                    else if (fromName == "K" && toName == "°C") formula = "°C = K − 273,15";
                    else if (fromName == "°F" && toName == "K") formula = "K = (°F − 32) / 1,8 + 273,15";
                    else if (fromName == "K" && toName == "°F") formula = "°F = (K − 273,15) × 1,8 + 32";
                    else formula = $"1 {fromName} = 1 {toName}";
                    
                    txtConvertFormula.Text = formula;
                }
                else
                {
                    double fromF = Units[type].FirstOrDefault(u => u.Name == fromName).Factor;
                    double toF = Units[type].FirstOrDefault(u => u.Name == toName).Factor;
                    if (type == "📐 Diện tích")
                    {
                        if (fromName == "sào") fromF = saoFactor;
                        if (toName == "sào") toF = saoFactor;
                    }
                    if (toF != 0)
                    {
                        double factorVal = fromF / toF;
                        txtConvertFormula.Text = $"1 {fromName} = {FormatUiNumber(factorVal, "G6")} {toName}";
                    }
                }
            }
            catch { }
        }

        private void SaoRegion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DoConversion();
        }

        private void SwapUnits_Click(object sender, RoutedEventArgs e)
        {
            if (cboFromUnit == null || cboToUnit == null) return;
            _isSwapping = true;
            try
            {
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
            finally
            {
                _isSwapping = false;
                DoConversion();
            }
        }

        private void SetQuickConversion(string unitType, string fromUnit, string toUnit, string value)
        {
            if (cboUnitType == null || cboFromUnit == null || cboToUnit == null || txtFromValue == null) return;

            // 1. Tìm và chọn nhóm đơn vị
            for (int i = 0; i < cboUnitType.Items.Count; i++)
            {
                if (cboUnitType.Items[i] is ComboBoxItem item && item.Content?.ToString() == unitType)
                {
                    cboUnitType.SelectedIndex = i;
                    break;
                }
            }

            // 2. Tìm và chọn đơn vị nguồn (phải duyệt theo chuỗi hiển thị của ComboBoxItem)
            for (int i = 0; i < cboFromUnit.Items.Count; i++)
            {
                if (cboFromUnit.Items[i] is ComboBoxItem item && item.Content?.ToString() == fromUnit)
                {
                    cboFromUnit.SelectedIndex = i;
                    break;
                }
            }

            // 3. Tìm và chọn đơn vị đích
            for (int i = 0; i < cboToUnit.Items.Count; i++)
            {
                if (cboToUnit.Items[i] is ComboBoxItem item && item.Content?.ToString() == toUnit)
                {
                    cboToUnit.SelectedIndex = i;
                    break;
                }
            }

            // 4. Nhập giá trị và kích hoạt tính toán
            txtFromValue.Text = value;
            DoConversion();
        }

        private void Preset_AstronomicalLength(object sender, RoutedEventArgs e) => SetQuickConversion("📏 Chiều dài", "AU", "km", "1");
        private void Preset_LightYear(object sender, RoutedEventArgs e) => SetQuickConversion("📏 Chiều dài", "ly", "km", "1");
        private void Preset_NanoLength(object sender, RoutedEventArgs e) => SetQuickConversion("📏 Chiều dài", "nm", "m", "1");
        private void Preset_InchToCm(object sender, RoutedEventArgs e) => SetQuickConversion("📏 Chiều dài", "inch", "cm", "1");

        private void Preset_MgToG(object sender, RoutedEventArgs e) => SetQuickConversion("⚖️ Khối lượng", "mg", "g", "500");
        private void Preset_TaToKg(object sender, RoutedEventArgs e) => SetQuickConversion("⚖️ Khối lượng", "tạ", "kg", "1");
        private void Preset_TanToKg(object sender, RoutedEventArgs e) => SetQuickConversion("⚖️ Khối lượng", "tấn", "kg", "1");
        private void Preset_LbToKg(object sender, RoutedEventArgs e) => SetQuickConversion("⚖️ Khối lượng", "lb", "kg", "150");
        private void Preset_GoldOunce(object sender, RoutedEventArgs e) => SetQuickConversion("⚖️ Khối lượng", "troy oz", "g", "10");

        private void Preset_BodyTemp(object sender, RoutedEventArgs e) => SetQuickConversion("🌡️ Nhiệt độ", "°C", "°F", "37");
        private void Preset_AbsoluteZero(object sender, RoutedEventArgs e) => SetQuickConversion("🌡️ Nhiệt độ", "K", "°C", "0");
        private void Preset_BoilingTemp(object sender, RoutedEventArgs e) => SetQuickConversion("🌡️ Nhiệt độ", "°C", "°F", "100");
        private void Preset_StandardFahrenheit(object sender, RoutedEventArgs e) => SetQuickConversion("🌡️ Nhiệt độ", "°F", "°C", "98.6");

        private void Preset_Km2ToHa(object sender, RoutedEventArgs e) => SetQuickConversion("📐 Diện tích", "km²", "ha", "1");
        private void Preset_HaToM2(object sender, RoutedEventArgs e) => SetQuickConversion("📐 Diện tích", "ha", "m²", "10000");
        private void Preset_SaoToM2(object sender, RoutedEventArgs e) => SetQuickConversion("📐 Diện tích", "sào", "m²", "1");
        private void Preset_Cm2ToMm2(object sender, RoutedEventArgs e) => SetQuickConversion("📐 Diện tích", "cm²", "mm²", "1");

        private void Preset_M3ToLit(object sender, RoutedEventArgs e) => SetQuickConversion("🧊 Thể tích", "m³", "lít", "1.5");
        private void Preset_CcToLit(object sender, RoutedEventArgs e) => SetQuickConversion("🧊 Thể tích", "cc", "lít", "150");
        private void Preset_GallonToLit(object sender, RoutedEventArgs e) => SetQuickConversion("🧊 Thể tích", "gallon", "lít", "1");
        private void Preset_NuocNgotLit(object sender, RoutedEventArgs e) => SetQuickConversion("🧊 Thể tích", "ml", "lít", "330");

        private void Preset_DayToSec(object sender, RoutedEventArgs e) => SetQuickConversion("⏱️ Thời gian", "ngày", "giây", "1");
        private void Preset_HourToSec(object sender, RoutedEventArgs e) => SetQuickConversion("⏱️ Thời gian", "giờ", "giây", "1");
        private void Preset_MsToSec(object sender, RoutedEventArgs e) => SetQuickConversion("⏱️ Thời gian", "ms", "giây", "5");

        private void Preset_KwhToJ(object sender, RoutedEventArgs e) => SetQuickConversion("⚡ Năng lượng", "kWh", "J", "1");
        private void Preset_CalToJ(object sender, RoutedEventArgs e) => SetQuickConversion("⚡ Năng lượng", "cal", "J", "100");
        private void Preset_KcalToKj(object sender, RoutedEventArgs e) => SetQuickConversion("⚡ Năng lượng", "kcal", "kJ", "2000");
        private void Preset_BtuToJ(object sender, RoutedEventArgs e) => SetQuickConversion("⚡ Năng lượng", "BTU", "J", "9000");

        // =======================================================
        //  CONSTANTS — Hằng số Vật lý & Toán học
        // =======================================================

        private void LoadConstants()
        {
            if (constantsPanel == null) return;
            constantsPanel.Children.Clear();

            var constants = DbManager.GetStemConstants();

            foreach (var c in constants)
            {
                var cardBg = (SolidColorBrush)new BrushConverter().ConvertFrom(c.ColorHex)!;

                var card = new Border
                {
                    Background = cardBg, CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(16, 14, 16, 14), Margin = new Thickness(0, 0, 12, 12),
                    Width = 240, Cursor = Cursors.Hand,
                    BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                    BorderThickness = new Thickness(1),
                    ToolTip = $"Nhấp để copy giá trị máy tính: {c.CopyValue}"
                };

                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = c.Symbol, FontSize = 28, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                    FontFamily = new FontFamily("Cambria Math, Segoe UI")
                });
                sp.Children.Add(new TextBlock
                {
                    Text = c.Name, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                    Margin = new Thickness(0, 2, 0, 4)
                });
                sp.Children.Add(new TextBlock
                {
                    Text = $"{c.Symbol} = {c.Value} {c.Unit}",
                    FontSize = 14, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                    TextWrapping = TextWrapping.Wrap
                });

                card.Child = sp;
                string copyVal = c.CopyValue;
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

        private void LoadFormulas(string category)
        {
            if (formulasPanel == null) return;
            formulasPanel.Children.Clear();

            var formulas = DbManager.GetStemFormulas(category);

            foreach (var f in formulas)
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
                    ToolTip = $"Nhấn để copy: {f.Formula}"
                };

                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = f.Name, FontSize = 12, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0))
                });
                sp.Children.Add(new TextBlock
                {
                    Text = f.Formula, FontSize = 18, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(26, 35, 126)),
                    FontFamily = new FontFamily("Cambria Math, Segoe UI"),
                    Margin = new Thickness(0, 4, 0, 4)
                });
                sp.Children.Add(new TextBlock
                {
                    Text = f.Description, FontSize = 12, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap
                });

                card.Child = sp;

                string copyFormula = f.Formula;
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

        private static string FormatUiNumber(double value, string format = "G6")
        {
            string s = value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
            s = s.Replace(".", ",");
            if (s.StartsWith("-"))
            {
                s = "−" + s.Substring(1);
            }
            return s;
        }

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

                if (!ParsingHelper.TryParseDouble(txtCoeffA.Text, out double a) || a == 0)
                { txtQuadResult.Text = "a phải ≠ 0"; return; }
                if (!ParsingHelper.TryParseDouble(txtCoeffB.Text, out double b))
                { txtQuadResult.Text = "b không hợp lệ"; return; }
                if (!ParsingHelper.TryParseDouble(txtCoeffC.Text, out double c))
                { txtQuadResult.Text = "c không hợp lệ"; return; }

                double delta = b * b - 4 * a * c;
                txtDelta.Text = $"Δ = {FormatUiNumber(delta)}";

                if (delta > 0)
                {
                    double x1 = (-b + Math.Sqrt(delta)) / (2 * a);
                    double x2 = (-b - Math.Sqrt(delta)) / (2 * a);
                    txtQuadResult.Text = $"x₁ = {FormatUiNumber(x1)}, x₂ = {FormatUiNumber(x2)}";
                    txtDelta.Foreground = new SolidColorBrush(Color.FromRgb(85, 139, 47)); // green
                    txtQuadResult.Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32));
                }
                else if (delta == 0)
                {
                    double x = -b / (2 * a);
                    txtQuadResult.Text = $"x₁ = x₂ = {FormatUiNumber(x)}";
                    txtDelta.Foreground = new SolidColorBrush(Color.FromRgb(245, 127, 23)); // orange
                    txtQuadResult.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0));
                }
                else
                {
                    double real = -b / (2 * a);
                    double imag = Math.Sqrt(-delta) / (2 * Math.Abs(a));
                    txtQuadResult.Text = $"x = {FormatUiNumber(real, "G4")} ± {FormatUiNumber(imag, "G4")}i";
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
                ParsingHelper.TryParseDouble(txtCoeffA?.Text ?? "", out a);
                ParsingHelper.TryParseDouble(txtCoeffB?.Text ?? "", out b);
                ParsingHelper.TryParseDouble(txtCoeffC?.Text ?? "", out c);

                if (a == 0)
                {
                    MessageBox.Show("Hệ số a của phương trình bậc 2 phải khác 0 để có dạng đồ thị parabol.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var win = new QASmartClass.LearningTools.Views.Math.GraphWindow(a, b, c);
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
                btnStopwatchLap.IsEnabled = false;
            }
            else
            {
                // Start/Resume
                if (!_stopwatch.IsRunning)
                {
                    _stopwatchTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Render, (s, ev) =>
                    {
                        var ts = _stopwatch.Elapsed;
                        txtStopwatch.Text = $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
                    }, Dispatcher.CurrentDispatcher);
                }
                _stopwatch.Start();
                _stopwatchTimer?.Start();

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
            string lapText;
            if (ts.TotalHours >= 1)
            {
                lapText = $"Lần {_lapCount}: {(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
            }
            else
            {
                lapText = $"Lần {_lapCount}: {ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
            }

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

        private static bool IsValidExpression(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return false;

            // Kiểm tra ngoặc đơn
            int parenDepth = 0;
            foreach (char c in expr)
            {
                if (c == '(') parenDepth++;
                else if (c == ')') parenDepth--;
                if (parenDepth < 0) return false;
            }
            if (parenDepth != 0) return false;

            // Kiểm tra ngoặc nhọn (LaTeX của presets)
            int braceDepth = 0;
            foreach (char c in expr)
            {
                if (c == '{') braceDepth++;
                else if (c == '}') braceDepth--;
                if (braceDepth < 0) return false;
            }
            if (braceDepth != 0) return false;

            // Kiểm tra các lỗi viết liền toán tử không hợp lệ
            if (expr.Contains("++") || expr.Contains("--") || expr.Contains("//") || expr.Contains("^^") || expr.Contains("**"))
                return false;

            return true;
        }

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

                if (!IsValidExpression(expr1))
                {
                    MessageBox.Show("Biểu thức 1 không hợp lệ (sai dấu ngoặc hoặc sai cú pháp toán tử). Vui lòng kiểm tra lại.", "Lỗi cú pháp", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string expr2 = txtExpr2?.Text?.Trim() ?? "";
                if (!string.IsNullOrEmpty(expr2) && !IsValidExpression(expr2))
                {
                    MessageBox.Show("Biểu thức 2 không hợp lệ (sai dấu ngoặc hoặc sai cú pháp toán tử). Vui lòng kiểm tra lại.", "Lỗi cú pháp", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string latex1 = System.Text.Json.JsonSerializer.Serialize($"y={expr1}");
                string expJs = $@"
        calc.setExpression({{id:'f1', latex:{latex1}, color:'#1565C0', lineWidth:3}});
        calc.setExpression({{id:'xaxis', latex:'y=0', color:'#999', lineWidth:1}});";

                if (!string.IsNullOrEmpty(expr2))
                {
                    string latex2 = System.Text.Json.JsonSerializer.Serialize($"y={expr2}");
                    expJs += $"\n        calc.setExpression({{id:'f2', latex:{latex2}, color:'#E65100', lineWidth:3}});";
                }

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
                    // Chỉ cho phép: chữ cái, số, (, ), .
                    var invalidChars = compound.Where(c => !char.IsLetterOrDigit(c) && c != '(' && c != ')' && c != '.').ToList();
                    if (invalidChars.Count > 0)
                    {
                        ShowChemError($"⚠️ Công thức \"{compound}\" chứa ký tự không hợp lệ: '{string.Join("', '", invalidChars.Distinct())}'\n\nChỉ sử dụng: chữ cái (H, O, Fe...), số (2, 3...) và dấu ngoặc ()." + "\n" + "Dùng dấu chấm (.) cho chất ngậm nước (VD: CuSO4.5H2O).");
                        return;
                    }
                    // Kiểm tra có bắt đầu bằng chữ hoa (bỏ qua hệ số đứng đầu nếu có)
                    string valCompound = compound;
                    int coeffLength = 0;
                    while (coeffLength < valCompound.Length && char.IsDigit(valCompound[coeffLength]))
                    {
                        coeffLength++;
                    }
                    if (coeffLength > 0)
                    {
                        valCompound = valCompound[coeffLength..];
                    }
                    if (valCompound.Length == 0)
                    {
                        ShowChemError($"⚠️ Công thức \"{compound}\" không hợp lệ.\nKhông tìm thấy công thức hóa học.");
                        return;
                    }
                    if (!char.IsUpper(valCompound[0]))
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

            // Solve using integer Gaussian elimination
            Fraction[,] matrix = new Fraction[m, n];
            for (int i = 0; i < m; i++)
            {
                string el = elemList[i];
                for (int j = 0; j < n; j++)
                {
                    int count = compositions[j].ContainsKey(el) ? compositions[j][el] : 0;
                    if (j < reactants.Count)
                        matrix[i, j] = new Fraction(count);
                    else
                        matrix[i, j] = new Fraction(-count);
                }
            }

            // REF Gaussian Elimination
            int r = 0;
            for (int col = 0; col < n; col++)
            {
                int pivotRow = -1;
                for (int row = r; row < m; row++)
                {
                    if (matrix[row, col].Num != 0)
                    {
                        pivotRow = row;
                        break;
                    }
                }
                if (pivotRow == -1) continue;

                if (pivotRow != r)
                {
                    for (int c = 0; c < n; c++)
                    {
                        var temp = matrix[r, c];
                        matrix[r, c] = matrix[pivotRow, c];
                        matrix[pivotRow, c] = temp;
                    }
                }

                for (int row = r + 1; row < m; row++)
                {
                    if (matrix[row, col].Num != 0)
                    {
                        Fraction factor = matrix[row, col] / matrix[r, col];
                        for (int c = col; c < n; c++)
                        {
                            matrix[row, c] = matrix[row, c] - factor * matrix[r, c];
                        }
                    }
                }
                r++;
            }

            // Back substitution to find null space vector
            Fraction[] sol = new Fraction[n];
            bool[] isPivot = new bool[n];
            int[] pivotRowOfCol = new int[n];
            for (int c = 0; c < n; c++) pivotRowOfCol[c] = -1;

            int currRow = 0;
            for (int c = 0; c < n; c++)
            {
                if (currRow < m && matrix[currRow, c].Num != 0)
                {
                    isPivot[c] = true;
                    pivotRowOfCol[c] = currRow;
                    currRow++;
                }
            }

            for (int c = 0; c < n; c++)
            {
                if (!isPivot[c])
                {
                    sol[c] = Fraction.One;
                }
            }

            for (int c = n - 1; c >= 0; c--)
            {
                if (isPivot[c])
                {
                    int row = pivotRowOfCol[c];
                    Fraction sum = Fraction.Zero;
                    for (int j = c + 1; j < n; j++)
                    {
                        sum = sum + matrix[row, j] * sol[j];
                    }
                    sol[c] = -sum / matrix[row, c];
                }
            }

            bool hasInvalidCoeff = false;
            for (int c = 0; c < n; c++)
            {
                if (sol[c].Num <= 0)
                {
                    hasInvalidCoeff = true;
                    break;
                }
            }

            if (hasInvalidCoeff)
            {
                ShowChemError("Không thể cân bằng phương trình này.\nVui lòng kiểm tra lại công thức hóa học.");
                return;
            }

            // Find LCM of denominators
            long lcmVal = 1;
            for (int i = 0; i < n; i++)
            {
                lcmVal = Lcm(lcmVal, sol[i].Den);
            }

            // Convert to integers
            long[] intCoeffs = new long[n];
            for (int i = 0; i < n; i++)
            {
                intCoeffs[i] = sol[i].Num * (lcmVal / sol[i].Den);
            }

            // Find GCD of all
            long gcdVal = intCoeffs[0];
            for (int i = 1; i < n; i++)
            {
                gcdVal = Gcd(gcdVal, intCoeffs[i]);
            }

            int[] bestCoeffs = new int[n];
            for (int i = 0; i < n; i++)
            {
                bestCoeffs[i] = (int)(intCoeffs[i] / gcdVal);
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
        var parts = formula.Split('.');
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part)) continue;
            
            int i = 0;
            while (i < part.Length && char.IsDigit(part[i]))
            {
                i++;
            }
            
            int coefficient = 1;
            string subFormula = part;
            if (i > 0)
            {
                coefficient = int.Parse(part[..i]);
                subFormula = part[i..];
            }
            
            var partResult = new Dictionary<string, int>();
            ParseGroup(subFormula, 0, subFormula.Length, 1, partResult);
            
            foreach (var kvp in partResult)
            {
                if (!result.ContainsKey(kvp.Key)) result[kvp.Key] = 0;
                result[kvp.Key] += kvp.Value * coefficient;
            }
        }
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

    private struct Fraction
    {
        public long Num { get; }
        public long Den { get; }

        public Fraction(long num, long den = 1)
        {
            if (den == 0) throw new ArgumentException("Denominator cannot be zero");
            if (den < 0)
            {
                num = -num;
                den = -den;
            }
            long g = Gcd(Math.Abs(num), den);
            Num = num / g;
            Den = den / g;
        }

        public static Fraction Zero => new Fraction(0);
        public static Fraction One => new Fraction(1);

        public static Fraction operator +(Fraction a, Fraction b) => new Fraction(a.Num * b.Den + b.Num * a.Den, a.Den * b.Den);
        public static Fraction operator -(Fraction a, Fraction b) => new Fraction(a.Num * b.Den - b.Num * a.Den, a.Den * b.Den);
        public static Fraction operator *(Fraction a, Fraction b) => new Fraction(a.Num * b.Num, a.Den * b.Den);
        public static Fraction operator /(Fraction a, Fraction b) => new Fraction(a.Num * b.Den, a.Den * b.Num);
        public static Fraction operator -(Fraction a) => new Fraction(-a.Num, a.Den);

        public override string ToString() => Den == 1 ? Num.ToString() : $"{Num}/{Den}";
    }

    private static long Gcd(long a, long b)
    {
        while (b != 0)
        {
            long temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }

    private static long Lcm(long a, long b)
    {
        if (a == 0 || b == 0) return 0;
        return Math.Abs(a * b) / Gcd(a, b);
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

        private (string Sin, string Cos, string Tan, string Cot) GetExactTrigValues(int angle)
        {
            switch (angle)
            {
                case 0:
                case 360:
                    return ("0", "1", "0", "KXĐ");
                case 30:
                    return ("1/2 (0,5)", "√3/2 (≈0,86603)", "√3/3 (≈0,57735)", "√3 (≈1,73205)");
                case 45:
                    return ("√2/2 (≈0,70711)", "√2/2 (≈0,70711)", "1", "1");
                case 60:
                    return ("√3/2 (≈0,86603)", "1/2 (0,5)", "√3 (≈1,73205)", "√3/3 (≈0,57735)");
                case 90:
                    return ("1", "0", "KXĐ", "0");
                case 120:
                    return ("√3/2 (≈0,86603)", "−1/2 (−0,5)", "−√3 (≈−1,73205)", "−√3/3 (≈−0,57735)");
                case 135:
                    return ("√2/2 (≈0,70711)", "−√2/2 (≈−0,70711)", "−1", "−1");
                case 150:
                    return ("1/2 (0,5)", "−√3/2 (≈−0,86603)", "−√3/3 (≈−0,57735)", "−√3 (≈−1,73205)");
                case 180:
                    return ("0", "−1", "0", "KXĐ");
                case 210:
                    return ("−1/2 (−0,5)", "−√3/2 (≈−0,86603)", "√3/3 (≈0,57735)", "√3 (≈1,73205)");
                case 225:
                    return ("−√2/2 (≈−0,70711)", "−√2/2 (≈−0,70711)", "1", "1");
                case 240:
                    return ("−√3/2 (≈−0,86603)", "−1/2 (−0,5)", "√3 (≈1,73205)", "√3/3 (≈0,57735)");
                case 270:
                    return ("−1", "0", "KXĐ", "0");
                case 300:
                    return ("−√3/2 (≈−0,86603)", "1/2 (0,5)", "−√3 (≈−1,73205)", "−√3/3 (≈−0,57735)");
                case 315:
                    return ("−√2/2 (≈−0,70711)", "√2/2 (≈0,70711)", "−1", "−1");
                case 330:
                    return ("−1/2 (−0,5)", "√3/2 (≈0,86603)", "−√3/3 (≈−0,57735)", "−√3 (≈−1,73205)");
                default:
                    double rad = angle * Math.PI / 180;
                    string sinV = FormatUiNumber(Math.Sin(rad), "F5");
                    string cosV = FormatUiNumber(Math.Cos(rad), "F5");
                    string tanV = Math.Abs(Math.Cos(rad)) < 1e-9 ? "KXĐ" : FormatUiNumber(Math.Tan(rad), "F5");
                    string cotV = Math.Abs(Math.Sin(rad)) < 1e-9 ? "KXĐ" : FormatUiNumber(1.0 / Math.Tan(rad), "F5");
                    return (sinV, cosV, tanV, cotV);
            }
        }

        private void LoadStemTable(string category)
        {
            if (stemTablePanel == null) return;
            stemTablePanel.Children.Clear();

            switch (category)
            {
                case "Trig":
                    int[] angles = { 0, 15, 30, 45, 60, 90, 120, 135, 150, 180, 210, 225, 240, 270, 300, 315, 330, 360 };
                    foreach (var a in angles)
                    {
                        var (sinV, cosV, tanV, cotV) = GetExactTrigValues(a);
                        bool special = a == 0 || a == 30 || a == 45 || a == 60 || a == 90 || a == 120 || a == 135 || a == 150 || a == 180 || a == 210 || a == 225 || a == 240 || a == 270 || a == 300 || a == 315 || a == 330 || a == 360;
                        AddStemCard($"{a}°", $"sin = {sinV}\ncos = {cosV}\ntan = {tanV}\ncot = {cotV}", special ? "#EDE7F6" : "#FAFAFA", "#4527A0");
                    }
                    break;
                case "Square":
                    for (int n = 1; n <= 30; n++)
                    {
                        double sqrtVal = Math.Sqrt(n);
                        string sqrtStr = (sqrtVal % 1 == 0) ? sqrtVal.ToString("0") : FormatUiNumber(sqrtVal, "F5");
                        AddStemCard($"{n}", $"n² = {n * n}\n√{n} = {sqrtStr}", n <= 10 ? "#E3F2FD" : "#FAFAFA", "#1565C0");
                    }
                    break;
                case "Log":
                    int[] vals = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 20, 50, 100 };
                    foreach (var v in vals)
                    {
                        double log10Val = Math.Log10(v);
                        double lnVal = Math.Log(v);
                        string log10Str = (log10Val == 0) ? "0" : FormatUiNumber(log10Val, "F5");
                        string lnStr = (lnVal == 0) ? "0" : FormatUiNumber(lnVal, "F5");
                        AddStemCard($"{v}", $"log₁₀ = {log10Str}\nln = {lnStr}", "#FFF8E1", "#E65100");
                    }
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
                Width = 240, Cursor = Cursors.Hand,
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
                    if (ParsingHelper.TryParseDouble(v, out double val))
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
            
            double sumSqDiff = values.Sum(x => (x - mean) * (x - mean));
            double popVariance = sumSqDiff / values.Count;
            double popStdDev = Math.Sqrt(popVariance);
            
            double sampleVariance = values.Count > 1 ? sumSqDiff / (values.Count - 1) : 0;
            double sampleStdDev = Math.Sqrt(sampleVariance);

            // Show stats
            statsValuesPanel.Children.Clear();
            void AddStat(string label, string val, string color)
            {
                var b = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 8, 4) };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)) });
                sp.Children.Add(new TextBlock { Text = val, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)) });
                b.Child = sp;
                statsValuesPanel.Children.Add(b);
            }
            AddStat("Số trung bình", mean.ToString("F2"), "#E3F2FD");
            AddStat("Số trung vị", median.ToString("F2"), "#E8F5E9");
            AddStat("Mốt", mode.ToString("F2"), "#FFF3E0");
            AddStat("Độ lệch chuẩn mẫu (s)", values.Count > 1 ? sampleStdDev.ToString("F2") : "KXĐ", "#F3E5F5");
            AddStat("Phương sai mẫu (s²)", values.Count > 1 ? sampleVariance.ToString("F2") : "KXĐ", "#FCE4EC");
            AddStat("Độ lệch chuẩn tổng thể (σ)", popStdDev.ToString("F2"), "#E0F7FA");
            AddStat("Phương sai tổng thể (σ²)", popVariance.ToString("F2"), "#E0F2F1");
            AddStat("Giá trị nhỏ nhất (Min)", sorted.First().ToString("F2"), "#FBE9E7");
            AddStat("Giá trị lớn nhất (Max)", sorted.Last().ToString("F2"), "#FFEBEE");
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

            string[] boxPlotLabels = { "minLabel", "q1Label", "medLabel", "q3Label", "maxLabel" };
            foreach (var labelName in boxPlotLabels)
            {
                if (this.FindName(labelName) != null)
                {
                    this.UnregisterName(labelName);
                }
            }

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
                        var valTb = new TextBlock 
                        { 
                            Text = values[i].ToString("F1"), 
                            FontSize = (values.Count > 10) ? 8 : 10, 
                            FontWeight = FontWeights.SemiBold, 
                            Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                            TextAlignment = TextAlignment.Center,
                            Width = 30
                        };
                        Canvas.SetLeft(valTb, x + barW / 2 - 15); Canvas.SetTop(valTb, baseY - barH - 16);
                        chartCanvas.Children.Add(valTb);
                    }
                    if (showLabels)
                    {
                        double labelY = baseY + 4;
                        double labelW = barW + 4;
                        if (values.Count > 8)
                        {
                            labelW = (barW + 8) * 2 - 4;
                            if (i % 2 == 1) labelY += 14;
                        }
                        var nameTb = new TextBlock 
                        { 
                            Text = names[i], 
                            FontSize = 9, 
                            Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)), 
                            MaxWidth = labelW, 
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            TextAlignment = TextAlignment.Center
                        };
                        Canvas.SetLeft(nameTb, x + barW / 2 - labelW / 2); Canvas.SetTop(nameTb, labelY);
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
                    
                    if (sweepAngle >= 359.99)
                    {
                        var circle = new System.Windows.Shapes.Ellipse
                        {
                            Width = 2 * r,
                            Height = 2 * r,
                            Fill = new SolidColorBrush(palette[i % palette.Length])
                        };
                        Canvas.SetLeft(circle, cx - r);
                        Canvas.SetTop(circle, cy - r);
                        chartCanvas.Children.Add(circle);
                    }
                    else
                    {
                        var path = new System.Windows.Shapes.Path { Fill = new SolidColorBrush(palette[i % palette.Length]) };
                        var fig = new PathFigure { StartPoint = new Point(cx, cy) };
                        double sa = startAngle * Math.PI / 180;
                        double ea = (startAngle + sweepAngle) * Math.PI / 180;
                        fig.Segments.Add(new LineSegment(new Point(cx + r * Math.Cos(sa), cy + r * Math.Sin(sa)), true));
                        fig.Segments.Add(new ArcSegment(new Point(cx + r * Math.Cos(ea), cy + r * Math.Sin(ea)), new Size(r, r), 0, sweepAngle > 180, SweepDirection.Clockwise, true));
                        fig.IsClosed = true;
                        path.Data = new PathGeometry(new[] { fig });
                        chartCanvas.Children.Add(path);
                    }

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
            else if (type == 3) // Box plot
            {
                if (values.Count == 0) return;
                var sorted = values.OrderBy(x => x).ToList();
                double min = sorted.First();
                double max = sorted.Last();
                
                double GetMedian(List<double> list)
                {
                    if (list.Count == 0) return 0;
                    int mid = list.Count / 2;
                    return list.Count % 2 == 1 ? list[mid] : (list[mid - 1] + list[mid]) / 2.0;
                }
                
                double median = GetMedian(sorted);
                double q1, q3;
                int n = sorted.Count;
                if (n == 1)
                {
                    q1 = min;
                    q3 = max;
                }
                else
                {
                    int halfSize = n / 2;
                    var lowerHalf = sorted.Take(halfSize).ToList();
                    var upperHalf = sorted.Skip(n - halfSize).ToList();
                    q1 = GetMedian(lowerHalf);
                    q3 = GetMedian(upperHalf);
                }

                double padL = 50, padR = 70;
                double plotW = w - padL - padR;
                double centerY = h / 2 - 15.0;

                bool isIdentical = Math.Abs(max - min) < 1e-9;

                if (isIdentical)
                {
                    double x_center = padL + 0.5 * plotW;

                    // Draw single thick vertical line representing the collapsed box
                    var line = new System.Windows.Shapes.Line
                    {
                        X1 = x_center, Y1 = centerY - 20,
                        X2 = x_center, Y2 = centerY + 20,
                        Stroke = new SolidColorBrush(palette[0]),
                        StrokeThickness = 3
                    };
                    chartCanvas.Children.Add(line);

                    if (showVals)
                    {
                        void AddLabel(string text, double x, double top, string name)
                        {
                            var tb = new TextBlock
                            {
                                Text = text,
                                FontSize = 10,
                                FontWeight = FontWeights.SemiBold,
                                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                                HorizontalAlignment = HorizontalAlignment.Center
                            };
                            Canvas.SetLeft(tb, x - 100); // offset left more for longer text
                            Canvas.SetTop(tb, top);
                            chartCanvas.Children.Add(tb);
                            if (!string.IsNullOrEmpty(name))
                            {
                                if (this.FindName(name) != null) this.UnregisterName(name);
                                this.RegisterName(name, tb);
                            }
                        }

                        // Draw label with the combined text
                        AddLabel($"Min = Q1 = Trung vị = Q3 = Max = {min:G6}", x_center + 10, centerY - 35, "minLabel");
                    }
                }
                else
                {
                    double range = max - min;
                    double mapXVal(double val)
                    {
                        return padL + ((val - min) / range) * plotW;
                    }

                    double x_min = mapXVal(min);
                    double x_q1 = mapXVal(q1);
                    double x_med = mapXVal(median);
                    double x_q3 = mapXVal(q3);
                    double x_max = mapXVal(max);

                    // Draw Box
                    var box = new System.Windows.Shapes.Rectangle
                    {
                        Width = Math.Max(1, x_q3 - x_q1),
                        Height = 40,
                        Fill = new SolidColorBrush(Color.FromArgb(50, palette[0].R, palette[0].G, palette[0].B)),
                        Stroke = new SolidColorBrush(palette[0]),
                        StrokeThickness = 2
                    };
                    Canvas.SetLeft(box, x_q1);
                    Canvas.SetTop(box, centerY - 20);
                    chartCanvas.Children.Add(box);

                    // Draw Median line
                    var medLine = new System.Windows.Shapes.Line
                    {
                        X1 = x_med, Y1 = centerY - 20,
                        X2 = x_med, Y2 = centerY + 20,
                        Stroke = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                        StrokeThickness = 2.5
                    };
                    chartCanvas.Children.Add(medLine);

                    // Draw Whiskers
                    var leftWhisker = new System.Windows.Shapes.Line
                    {
                        X1 = x_min, Y1 = centerY,
                        X2 = x_q1, Y2 = centerY,
                        Stroke = new SolidColorBrush(palette[0]),
                        StrokeThickness = 1.5
                    };
                    chartCanvas.Children.Add(leftWhisker);

                    var rightWhisker = new System.Windows.Shapes.Line
                    {
                        X1 = x_q3, Y1 = centerY,
                        X2 = x_max, Y2 = centerY,
                        Stroke = new SolidColorBrush(palette[0]),
                        StrokeThickness = 1.5
                    };
                    chartCanvas.Children.Add(rightWhisker);

                    // Draw caps
                    var leftCap = new System.Windows.Shapes.Line
                    {
                        X1 = x_min, Y1 = centerY - 10,
                        X2 = x_min, Y2 = centerY + 10,
                        Stroke = new SolidColorBrush(palette[0]),
                        StrokeThickness = 1.5
                    };
                    chartCanvas.Children.Add(leftCap);

                    var rightCap = new System.Windows.Shapes.Line
                    {
                        X1 = x_max, Y1 = centerY - 10,
                        X2 = x_max, Y2 = centerY + 10,
                        Stroke = new SolidColorBrush(palette[0]),
                        StrokeThickness = 1.5
                    };
                    chartCanvas.Children.Add(rightCap);

                    // Collision detection
                    double minOffset = (x_q1 - x_min < 55) ? 18.0 : -32.0;
                    double maxOffset = (x_max - x_q3 < 55) ? 18.0 : -32.0;

                    double minTop = centerY + minOffset;
                    double maxTop = centerY + maxOffset;
                    double q1Top = centerY - 44.0;
                    double q3Top = centerY - 44.0;
                    double medTop = centerY + 24.0;

                    void AddLabel(string text, double x, double top, string name)
                    {
                        var tb = new TextBlock
                        {
                            Text = text,
                            FontSize = 10,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        Canvas.SetLeft(tb, x - 25);
                        Canvas.SetTop(tb, top);
                        chartCanvas.Children.Add(tb);
                        if (!string.IsNullOrEmpty(name))
                        {
                            if (this.FindName(name) != null) this.UnregisterName(name);
                            this.RegisterName(name, tb);
                        }
                    }

                    if (showVals)
                    {
                        AddLabel($"Min: {min:G6}", x_min, minTop, "minLabel");
                        AddLabel($"Q1: {q1:G6}", x_q1, q1Top, "q1Label");
                        AddLabel($"Med: {median:G6}", x_med, medTop, "medLabel");
                        AddLabel($"Q3: {q3:G6}", x_q3, q3Top, "q3Label");
                        AddLabel($"Max: {max:G6}", x_max, maxTop, "maxLabel");
                    }
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
                        double labelY = padT + plotH + 6;
                        double labelW = 50;
                        if (values.Count > 8)
                        {
                            labelW = step * 2 - 4;
                            if (i % 2 == 1) labelY += 14;
                        }
                        var nt = new TextBlock 
                        { 
                            Text = names[i], 
                            FontSize = 9, 
                            Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)), 
                            TextAlignment = TextAlignment.Center, 
                            Width = labelW, 
                            TextTrimming = TextTrimming.CharacterEllipsis 
                        };
                        Canvas.SetLeft(nt, x - labelW / 2); Canvas.SetTop(nt, labelY);
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
            graphPresetPanel.Children.Clear();

            var presets = DbManager.GetStemPresets("Graph");
            
            // Group by GroupName
            var groups = presets.GroupBy(p => p.GroupName);
            
            var groupColors = new Dictionary<string, string>
            {
                ["📐 Hàm số Bậc 1 & Bậc 2 (Lớp 10)"] = "#1565C0",
                ["📈 Hàm Lũy thừa, Mũ & Lôgarit (Lớp 11)"] = "#E65100",
                ["🔄 Hàm số Lượng giác (Lớp 11)"] = "#2E7D32",
                ["🔬 Phân thức & Đa thức bậc cao (Lớp 12)"] = "#7B1FA2",
                ["📐 Đường Conic (Hình học lớp 10)"] = "#00838F",
                ["🚀 Mô hình Vật lý & STEM (Liên môn)"] = "#D84315"
            };

            foreach (var g in groups)
            {
                string groupName = g.Key;
                string colorHex = groupColors.ContainsKey(groupName) ? groupColors[groupName] : "#374151";
                var groupColor = (Color)ColorConverter.ConvertFromString(colorHex);

                // Section container
                var groupSection = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

                // Header
                var header = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(20, groupColor.R, groupColor.G, groupColor.B)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 4, 8, 4),
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

                // Buttons wrap panel
                var btnPanel = new WrapPanel { Margin = new Thickness(4, 0, 0, 0) };
                foreach (var p in g)
                {
                    var btn = new Button
                    {
                        Content = p.Label, Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(0, 0, 4, 4), FontSize = 11, Cursor = Cursors.Hand,
                        FontFamily = new FontFamily("Segoe UI"),
                        ToolTip = $"Nhấn để điền: {p.Expression}"
                    };
                    string expr = p.Expression;
                    btn.Click += (_, _) =>
                    {
                        var targetTextBox = _activeExprTextBox ?? txtExpr1;
                        if (targetTextBox != null)
                        {
                            targetTextBox.Text = expr;
                            targetTextBox.Focus();
                        }
                    };
                    btnPanel.Children.Add(btn);
                }
                groupSection.Children.Add(btnPanel);
                graphPresetPanel.Children.Add(groupSection);
            }
        }

        private void BuildChemPresets()
        {
            if (chemPresetPanel == null) return;
            chemPresetPanel.Children.Clear();

            var presets = DbManager.GetStemPresets("Chem");
            
            var groups = presets.GroupBy(p => p.GroupName);
            
            var groupColors = new Dictionary<string, string>
            {
                ["🔥 Phản ứng cháy & Oxi hóa nhanh"] = "#E65100",
                ["⚗️ Axit – Bazơ & Oxit"] = "#2E7D32",
                ["🧪 Phân hủy"] = "#1565C0",
                ["🔄 Trao đổi (Kết tủa / Bay hơi)"] = "#00695C",
                ["⚡ Oxi hóa – Khử & Nhiệt luyện"] = "#7B1FA2",
                ["🧬 Kim loại + Axit (Thường & Đặc nóng)"] = "#C62828",
                ["🔗 Phản ứng hóa hợp"] = "#00838F",
                ["🌟 Hợp chất hữu cơ & Khác"] = "#D84315"
            };

            foreach (var g in groups)
            {
                string groupName = g.Key;
                string colorHex = groupColors.ContainsKey(groupName) ? groupColors[groupName] : "#374151";
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

                // Buttons in a 3-column UniformGrid, left-aligned
                var btnPanel = new System.Windows.Controls.Primitives.UniformGrid { Columns = 3, Margin = new Thickness(8, 0, 0, 0) };
                foreach (var p in g)
                {
                    string eq = p.Label;
                    var btn = new Button
                    {
                        Content = eq,
                        Padding = new Thickness(7, 3, 7, 3),
                        Margin = new Thickness(0, 0, 4, 4),
                        FontSize = 10.5,
                        Cursor = Cursors.Hand,
                        FontFamily = new FontFamily("Segoe UI"),
                        ToolTip = $"Nhấn để điền: {eq}",
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    btn.Click += (_, _) => { if (txtChemEquation != null) txtChemEquation.Text = eq; };
                    btnPanel.Children.Add(btn);
                }
                groupSection.Children.Add(btnPanel);

                chemPresetPanel.Children.Add(groupSection);
            }
        }

        // =======================================================
        //  PRESETS FOR STEM UPGRADE (Tab 2, Tab 3, Tab 4)
        // =======================================================

        // --- Tab 2: Unit Converter Presets ---
        private void Preset_FlightAltitude(object sender, RoutedEventArgs e) => SetQuickConversion("📏 Chiều dài", "ft", "m", "30000");
        private void Preset_MarathonDistance(object sender, RoutedEventArgs e) => SetQuickConversion("📏 Chiều dài", "mile", "km", "26.21875");
        private void Preset_HairThickness(object sender, RoutedEventArgs e) => SetQuickConversion("📏 Chiều dài", "μm", "mm", "80");
        private void Preset_DiamondHope(object sender, RoutedEventArgs e) => SetQuickConversion("⚖️ Khối lượng", "carat", "g", "45.52");
        private void Preset_AsianElephant(object sender, RoutedEventArgs e) => SetQuickConversion("⚖️ Khối lượng", "tấn", "kg", "5");
        private void Preset_BakingTemp(object sender, RoutedEventArgs e) => SetQuickConversion("🌡️ Nhiệt độ", "°F", "°C", "350");
        private void Preset_LiquidNitrogen(object sender, RoutedEventArgs e) => SetQuickConversion("🌡️ Nhiệt độ", "°C", "K", "-196");
        private void Preset_USFarm(object sender, RoutedEventArgs e) => SetQuickConversion("📐 Diện tích", "acre", "m²", "160");
        private void Preset_FootballPitch(object sender, RoutedEventArgs e) => SetQuickConversion("📐 Diện tích", "ha", "m²", "0.714");
        private void Preset_MotorcycleTank(object sender, RoutedEventArgs e) => SetQuickConversion("🧊 Thể tích", "lít", "ml", "6");
        private void Preset_WaterDrop(object sender, RoutedEventArgs e) => SetQuickConversion("🧊 Thể tích", "ml", "lít", "0.05");
        private void Preset_SummerVacation(object sender, RoutedEventArgs e) => SetQuickConversion("⏱️ Thời gian", "tuần", "ngày", "8");
        private void Preset_SoundWavePeriod(object sender, RoutedEventArgs e) => SetQuickConversion("⏱️ Thời gian", "ms", "giây", "1");
        private void Preset_PhotonEnergy(object sender, RoutedEventArgs e) => SetQuickConversion("⚡ Năng lượng", "eV", "J", "2.5");

        // --- Tab 3: Quadratic Solver Presets ---
        private void SetQuickQuadratic(string a, string b, string c, string presetTitle, string presetDesc)
        {
            if (txtCoeffA == null || txtCoeffB == null || txtCoeffC == null) return;
            txtCoeffA.Text = a;
            txtCoeffB.Text = b;
            txtCoeffC.Text = c;
            SolveQuadratic();

            if (quadPresetDescPanel != null && txtQuadPresetTitle != null && txtQuadPresetDesc != null)
            {
                quadPresetDescPanel.Visibility = Visibility.Visible;
                txtQuadPresetTitle.Text = presetTitle;
                txtQuadPresetDesc.Text = presetDesc;
            }
        }

        private void Preset_QuadNemXien(object sender, RoutedEventArgs e) =>
            SetQuickQuadratic("-0.05", "1", "0", 
                "Quỹ đạo ném xiên: y = −0,05x² + x", 
                "Phương trình chuyển động của vật ném xiên từ mặt đất với góc 45°: −0,05x² + x = 0. Nghiệm x = 20m là tầm xa của vật.");

        private void Preset_QuadCongVom(object sender, RoutedEventArgs e) =>
            SetQuickQuadratic("-0.25", "2", "0", 
                "Cổng vòm Parabol: y = −0,25x² + 2x", 
                "Mô phỏng cổng vòm chịu lực có chiều cao cực đại h = 4m. Nghiệm x = 8m là khoảng cách giữa 2 chân cổng.");

        private void Preset_QuadChaoDen(object sender, RoutedEventArgs e) =>
            SetQuickQuadratic("0.1", "0", "-2.5", 
                "Chao đèn phản xạ: y = 0,1x² − 2,5", 
                "Biểu diễn mặt cắt gương phản xạ parabol có độ sâu 2,5m. Nghiệm x = ±5m xác định khẩu độ của chao đèn.");

        private void Preset_QuadLoiNhuan(object sender, RoutedEventArgs e) =>
            SetQuickQuadratic("-2", "120", "-1000", 
                "Tối ưu doanh thu: P(x) = −2x² + 120x − 1000", 
                "Hàm lợi nhuận của doanh nghiệp theo sản lượng x. Nghiệm x₁ = 10, x₂ = 50 là các điểm hòa vốn. Đỉnh parabol x = 30 cho lợi nhuận cực đại.");

        private void Preset_QuadRoiTuDo(object sender, RoutedEventArgs e) =>
            SetQuickQuadratic("-4.9", "0", "49", 
                "Rơi tự do: s(t) = −4,9t² + 49", 
                "Vật rơi tự do từ độ cao 49m: −4,9t² + 49 = 0. Nghiệm dương t ≈ 3,16 giây là thời gian vật chạm đất.");

        private void Preset_QuadCatDiem(object sender, RoutedEventArgs e) =>
            SetQuickQuadratic("1", "-6", "9", 
                "Cắt điểm Parabol (Nghiệm kép): y = x² − 6x + 9", 
                "Trường hợp đường thẳng tiếp xúc với parabol tại 1 điểm duy nhất (nghiệm kép x = 3). Đồ thị tiếp xúc trục hoành.");

        // --- Tab 4: Stopwatch Presets ---
        private void SetQuickStopwatch(string presetTitle, string presetDesc)
        {
            if (stopwatchPresetDescPanel != null && txtStopwatchPresetTitle != null && txtStopwatchPresetDesc != null)
            {
                stopwatchPresetDescPanel.Visibility = Visibility.Visible;
                txtStopwatchPresetTitle.Text = presetTitle;
                txtStopwatchPresetDesc.Text = presetDesc;
            }
        }

        private void Preset_FreeFall2m(object sender, RoutedEventArgs e) =>
            SetQuickStopwatch("Rơi tự do (h = 2m)", 
                "Công thức: t = √(2h/g) với g ≈ 9,8m/s².\nThời gian lý thuyết vật rơi chạm đất: t ≈ 0,64 giây.\nDùng để kiểm chứng gia tốc trọng trường.");

        private void Preset_FreeFall5m(object sender, RoutedEventArgs e) =>
            SetQuickStopwatch("Rơi tự do (h = 5m)", 
                "Công thức: t = √(2h/g) với g ≈ 9,8m/s².\nThời gian lý thuyết vật rơi chạm đất: t ≈ 1,01 giây.\nPhù hợp cho thí nghiệm thả rơi từ tầng 2 nhà trường.");

        private void Preset_Pendulum1m(object sender, RoutedEventArgs e) =>
            SetQuickStopwatch("Con lắc đơn (L = 1m)", 
                "Chu kỳ lý thuyết: T = 2π√(L/g) ≈ 2,01 giây.\nMẹo thí nghiệm: Đo thời gian của 10 dao động toàn phần (ví dụ: 20,1 giây), sau đó chia cho 10 để tính T chính xác hơn.");

        private void Preset_ChemReaction(object sender, RoutedEventArgs e) =>
            SetQuickStopwatch("Phản ứng kết tủa (Na2S2O3 + H2SO4)", 
                "Mục tiêu: Đo thời gian từ lúc đổ axit vào muối đến khi dung dịch vẩn đục hoàn toàn che khuất dấu X vẽ dưới đáy cốc. Thường dao động từ 10 - 45 giây tùy thuộc nhiệt độ và nồng độ chất phản ứng.");

        private void Preset_HeartRateSport(object sender, RoutedEventArgs e) =>
            SetQuickStopwatch("Nhịp tim sau vận động", 
                "Mục tiêu: Đếm đủ 30 nhịp đập của tim ngay sau khi chạy/nhảy dây. Bấm giờ xem mất bao nhiêu giây (ví dụ: t = 18s). Tính nhịp tim (nhịp/phút) = (30 / t) * 60.");

        private void Preset_SpeedOfSound(object sender, RoutedEventArgs e) =>
            SetQuickStopwatch("Tốc độ âm thanh (d = 340m)", 
                "Mục tiêu: Đứng cách bức tường lớn 170m, phát ra tiếng bộp lớn và đo thời gian âm thanh đi đến tường và phản xạ lại (tổng quãng đường d = 340m). Thời gian đo lý thuyết t ≈ 1,00 giây (v ≈ 340 m/s).");
    }
}
