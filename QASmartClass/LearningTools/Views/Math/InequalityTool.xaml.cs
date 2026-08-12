using QASmartClass.LearningTools.Models;
using System;
using QASmartClass.LearningTools.Helpers;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class InequalityTool : BaseToolControl
    {
        private enum Mode { Linear, Quadratic }
        private Mode _mode = Mode.Linear;

        public InequalityTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn xét dấu" : "Sign Chart Guide";
                if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Tính toán & Đồ thị" : "Calculator & Graph";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";

                if (sideMenu != null)
                {
                    sideMenu.SelectionChanged += SideMenu_SelectionChanged;
                    sideMenu.SelectedIndex = 0;
                }

                BuildModeButtons();
                BuildLinearPresets();
                BuildQuadPresets();
                LoadPracticalApps();
                UpdateUI();
                SolveLinear();
                SolveQuadratic();

                // Attach touch numeric pad
                TouchNumPad.Attach(txtIneqA1, step: 1);
                TouchNumPad.Attach(txtIneqB1, step: 1);
                TouchNumPad.Attach(txtIneqA2, step: 1);
                TouchNumPad.Attach(txtIneqB2, step: 1);
                TouchNumPad.Attach(txtIneqC2, step: 1);
            };
        }

        // ═══ EVENTS ═══
        private void Linear_Changed(object sender, EventArgs e) { if (IsLoaded) SolveLinear(); }
        private void Quadratic_Changed(object sender, EventArgs e) { if (IsLoaded) SolveQuadratic(); }

        private void Reset_Click(object sender, MouseButtonEventArgs e)
        {
            if (_mode == Mode.Linear)
            {
                txtIneqA1.Text = "2";
                txtIneqB1.Text = "-6";
                cboSign1.SelectedIndex = 0;
                SolveLinear();
            }
            else
            {
                txtIneqA2.Text = "1";
                txtIneqB2.Text = "-5";
                txtIneqC2.Text = "6";
                cboSign2.SelectedIndex = 0;
                SolveQuadratic();
            }
        }

        // ═══ CHIPS & TABS ═══
        private void BuildModeButtons()
        {
            modePanel.Children.Clear();
            var modes = new (string Label, Mode M)[]
            {
                ("📐 BPT Bậc 1 (ax + b > 0)", Mode.Linear),
                ("📐 BPT Bậc 2 (ax² + bx + c > 0)", Mode.Quadratic)
            };
            foreach (var (label, m) in modes)
            {
                bool active = m == _mode;
                var btn = MakeChip(label, active);
                var cm = m;
                btn.MouseLeftButtonDown += (_, _) =>
                {
                    _mode = cm;
                    BuildModeButtons();
                    UpdateUI();
                    if (_mode == Mode.Linear) SolveLinear();
                    else SolveQuadratic();
                };
                modePanel.Children.Add(btn);
            }
        }

        private Border MakeChip(string label, bool active)
        {
            var border = new Border
            {
                Background = active ? new SolidColorBrush(Color.FromRgb(40, 53, 147)) : new SolidColorBrush(Color.FromRgb(245, 247, 250)),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(0, 0, 8, 8),
                Cursor = Cursors.Hand,
                BorderBrush = active ? new SolidColorBrush(Color.FromRgb(40, 53, 147)) : new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1)
            };
            border.Child = new TextBlock
            {
                Text = label,
                FontSize = 13,
                FontWeight = active ? FontWeights.Bold : FontWeights.SemiBold,
                Foreground = active ? Brushes.White : new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                FontFamily = new FontFamily("Segoe UI")
            };
            return border;
        }

        private void UpdateUI()
        {
            if (linearInputPanel == null) return;

            linearInputPanel.Visibility = _mode == Mode.Linear ? Visibility.Visible : Visibility.Collapsed;
            quadInputPanel.Visibility = _mode == Mode.Quadratic ? Visibility.Visible : Visibility.Collapsed;

            linearPresetPanel.Visibility = _mode == Mode.Linear ? Visibility.Visible : Visibility.Collapsed;
            quadPresetPanel.Visibility = _mode == Mode.Quadratic ? Visibility.Visible : Visibility.Collapsed;

            svLinearResult.Visibility = _mode == Mode.Linear ? Visibility.Visible : Visibility.Collapsed;
            svQuadResult.Visibility = _mode == Mode.Quadratic ? Visibility.Visible : Visibility.Collapsed;

            btnGraphLinear.Visibility = _mode == Mode.Linear ? Visibility.Visible : Visibility.Collapsed;
            btnGraphQuad.Visibility = _mode == Mode.Quadratic ? Visibility.Visible : Visibility.Collapsed;

            txtModeTitle.Text = _mode == Mode.Linear ? "📐 Bất phương trình Bậc 1" : "📐 Bất phương trình Bậc 2";
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootGrid == null || sideMenu == null || contentGuide == null || contentCalc == null || gridApp == null)
                return;

            contentGuide.Visibility = Visibility.Collapsed;
            contentCalc.Visibility = Visibility.Collapsed;
            gridApp.Visibility = Visibility.Collapsed;

            int index = sideMenu.SelectedIndex;
            if (index == 0)
            {
                contentGuide.Visibility = Visibility.Visible;
            }
            else if (index == 1)
            {
                contentCalc.Visibility = Visibility.Visible;
            }
            else if (index == 2)
            {
                gridApp.Visibility = Visibility.Visible;
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
            if (tag == "guide")
                sideMenu.SelectedIndex = 1;
            else if (tag == "calc" || tag == "practice")
                sideMenu.SelectedIndex = 1;
            else if (tag == "app" || tag == "practical")
                sideMenu.SelectedIndex = 1;
        }

        // ═══ BPT BẬC 1: ax + b ? 0 ═══
        private void SolveLinear()
        {
            if (linearResultPanel == null) return;
            linearResultPanel.Children.Clear();

            if (!ParsingHelper.TryParseDouble(txtIneqA1?.Text, out double a)) return;
            if (!ParsingHelper.TryParseDouble(txtIneqB1?.Text, out double b)) return;
            int sign = cboSign1?.SelectedIndex ?? 0; // 0:>, 1:>=, 2:<, 3:<=

            string[] signs = { ">", "≥", "<", "≤" };
            AddLinearResult($"📝 BPT: {FormatLinearIneq(a, b, signs[sign])}", "#283593");

            if (a == 0)
            {
                bool ok = sign switch
                {
                    0 => b > 0, 1 => b >= 0, 2 => b < 0, 3 => b <= 0, _ => false
                };
                AddLinearResult(ok ? "✅ Đúng với mọi x ∈ ℝ (vô số nghiệm)" : "❌ Vô nghiệm (không có x thỏa mãn)", ok ? "#2E7D32" : "#C62828");
                AddLinearResult(ok ? "📐 Tập nghiệm: S = ℝ" : "📐 Tập nghiệm: S = ∅", "#283593");
                return;
            }

            double root = -b / a;
            string step1Right = b == 0 ? "0" : Fmt(-b);
            AddLinearResult($"🔑 Bước 1: Chuyển vế tự do → {Fmt(a)}x {signs[sign]} {step1Right}", "#1565C0");
            AddLinearResult($"🔑 Bước 2: Chia hai vế cho a = {Fmt(a)} ({(a > 0 ? "a > 0 → giữ nguyên chiều" : "a < 0 → ĐỔI CHIỀU BPT")})", "#1565C0");

            string result;
            if (a > 0)
                result = sign switch { 0 => $"x > {Fmt(root)}", 1 => $"x ≥ {Fmt(root)}", 2 => $"x < {Fmt(root)}", 3 => $"x ≤ {Fmt(root)}", _ => "" };
            else
            {
                result = sign switch { 0 => $"x < {Fmt(root)}", 1 => $"x ≤ {Fmt(root)}", 2 => $"x > {Fmt(root)}", 3 => $"x ≥ {Fmt(root)}", _ => "" };
                AddLinearResult("⚠️ Hệ số a < 0 → Đã đổi chiều bất phương trình!", "#E65100");
            }

            AddLinearResult($"✅ Kết luận nghiệm: {result}", "#2E7D32");

            // Interval notation
            bool strict = sign == 0 || sign == 2;
            string bracket = strict ? "(" : "[";
            string interval;
            if ((a > 0 && sign <= 1) || (a < 0 && sign >= 2))
                interval = $"{bracket}{Fmt(root)}; +∞)";
            else
                interval = $"(-∞; {Fmt(root)}{(strict ? ")" : "]")}";
            AddLinearResult($"📐 Tập nghiệm: S = {interval}", "#283593");
        }

        // ═══ BPT BẬC 2: ax² + bx + c ? 0 ═══
        private void SolveQuadratic()
        {
            if (quadraticResultPanel == null) return;
            quadraticResultPanel.Children.Clear();

            if (!ParsingHelper.TryParseDouble(txtIneqA2?.Text, out double a)) return;
            if (!ParsingHelper.TryParseDouble(txtIneqB2?.Text, out double b)) return;
            if (!ParsingHelper.TryParseDouble(txtIneqC2?.Text, out double c)) return;
            int sign = cboSign2?.SelectedIndex ?? 0;

            string[] signs = { ">", "≥", "<", "≤" };

            // a = 0 fallback
            if (a == 0)
            {
                AddQuadResult("⚠️ Hệ số a = 0. Bất phương trình bậc hai trở thành bậc nhất!", "#E65100");
                if (b == 0)
                {
                    bool ok = sign switch
                    {
                        0 => c > 0, 1 => c >= 0, 2 => c < 0, 3 => c <= 0, _ => false
                    };
                    AddQuadResult($"📝 BPT: {Fmt(c)} {signs[sign]} 0", "#283593");
                    AddQuadResult(ok ? "✅ Đúng với mọi x ∈ ℝ" : "❌ Vô nghiệm", ok ? "#2E7D32" : "#C62828");
                    AddQuadResult(ok ? "📐 Tập nghiệm: S = ℝ" : "📐 Tập nghiệm: S = ∅", "#283593");
                }
                else
                {
                    double root = -c / b;
                    AddQuadResult($"📝 BPT: {FormatLinearIneq(b, c, signs[sign])}", "#283593");
                    AddQuadResult($"🔑 Bước 1: Chuyển vế → {Fmt(b)}x {signs[sign]} {(c == 0 ? "0" : Fmt(-c))}", "#1565C0");
                    AddQuadResult($"🔑 Bước 2: Chia cho b = {Fmt(b)} ({(b > 0 ? "dương → giữ dấu" : "âm → ĐỔI ĐẤU")})", "#1565C0");
                    string result;
                    if (b > 0)
                        result = sign switch { 0 => $"x > {Fmt(root)}", 1 => $"x ≥ {Fmt(root)}", 2 => $"x < {Fmt(root)}", 3 => $"x ≤ {Fmt(root)}", _ => "" };
                    else
                    {
                        result = sign switch { 0 => $"x < {Fmt(root)}", 1 => $"x ≤ {Fmt(root)}", 2 => $"x > {Fmt(root)}", 3 => $"x ≥ {Fmt(root)}", _ => "" };
                        AddQuadResult("⚠️ Hệ số b < 0 → Đổi chiều bất phương trình!", "#E65100");
                    }
                    AddQuadResult($"✅ Nghiệm: {result}", "#2E7D32");
                    bool strict1 = sign == 0 || sign == 2;
                    string bracket1 = strict1 ? "(" : "[";
                    string interval1;
                    if ((b > 0 && sign <= 1) || (b < 0 && sign >= 2))
                        interval1 = $"{bracket1}{Fmt(root)}; +∞)";
                    else
                        interval1 = $"(-∞; {Fmt(root)}{(strict1 ? ")" : "]")}";
                    AddQuadResult($"📐 Tập nghiệm: S = {interval1}", "#283593");
                }
                return;
            }

            double delta = b * b - 4 * a * c;
            AddQuadResult($"📝 BPT: {FormatQuadIneq(a, b, c, signs[sign])}", "#283593");

            // Delta format with negative parentheses
            string bPart = b < 0 ? $"({Fmt(b)})²" : $"{Fmt(b)}²";
            string aPart = a < 0 ? $"({Fmt(a)})" : $"{Fmt(a)}";
            string cPart = c < 0 ? $"({Fmt(c)})" : $"{Fmt(c)}";
            AddQuadResult($"🔑 Bước 1: Tính Δ = b² − 4ac = {bPart} − 4·{aPart}·{cPart} = {Fmt(delta)}", "#1565C0");

            if (delta < 0)
            {
                AddQuadResult($"📊 Δ = {Fmt(delta)} < 0 → Tam thức không đổi dấu (luôn cùng dấu a = {Fmt(a)})", "#E65100");
                bool sameAsA = a > 0;
                bool ok = false;
                if (sign == 0 || sign == 1) // > 0 or >= 0
                    ok = sameAsA;
                else // < 0 or <= 0
                    ok = !sameAsA;

                AddQuadResult(ok ? "✅ Nghiệm đúng với mọi x ∈ ℝ" : "❌ Vô nghiệm", ok ? "#2E7D32" : "#C62828");
                AddQuadResult(ok ? "📐 Tập nghiệm: S = ℝ" : "📐 Tập nghiệm: S = ∅", "#283593");
            }
            else if (delta == 0)
            {
                double x0 = -b / (2 * a);
                AddQuadResult($"📊 Δ = 0 → Tam thức có nghiệm kép x₀ = −b/(2a) = {Fmt(x0)}", "#E65100");
                
                string factorPart = a == 1 ? "" : (a == -1 ? "-" : $"{Fmt(a)}");
                string xPart = x0 == 0 ? "x²" : (x0 > 0 ? $"(x − {Fmt(x0)})²" : $"(x + {Fmt(-x0)})²");
                AddQuadResult($"💡 Phân tích nhân tử: f(x) = {factorPart}{xPart}", "#283593");

                if (sign == 0) // > 0
                {
                    if (a > 0)
                    {
                        AddQuadResult($"✅ Nghiệm: mọi x ≠ {Fmt(x0)}", "#2E7D32");
                        AddQuadResult($"📐 Tập nghiệm: S = ℝ \\ {{{Fmt(x0)}}}", "#283593");
                    }
                    else
                    {
                        AddQuadResult("❌ Vô nghiệm", "#C62828");
                        AddQuadResult("📐 Tập nghiệm: S = ∅", "#283593");
                    }
                }
                else if (sign == 1) // >= 0
                {
                    if (a > 0)
                    {
                        AddQuadResult("✅ Nghiệm đúng với mọi x ∈ ℝ", "#2E7D32");
                        AddQuadResult("📐 Tập nghiệm: S = ℝ", "#283593");
                    }
                    else
                    {
                        AddQuadResult($"✅ Nghiệm duy nhất: x = {Fmt(x0)}", "#2E7D32");
                        AddQuadResult($"📐 Tập nghiệm: S = {{{Fmt(x0)}}}", "#283593");
                    }
                }
                else if (sign == 2) // < 0
                {
                    if (a < 0)
                    {
                        AddQuadResult($"✅ Nghiệm: mọi x ≠ {Fmt(x0)}", "#2E7D32");
                        AddQuadResult($"📐 Tập nghiệm: S = ℝ \\ {{{Fmt(x0)}}}", "#283593");
                    }
                    else
                    {
                        AddQuadResult("❌ Vô nghiệm", "#C62828");
                        AddQuadResult("📐 Tập nghiệm: S = ∅", "#283593");
                    }
                }
                else // <= 0
                {
                    if (a < 0)
                    {
                        AddQuadResult("✅ Nghiệm đúng với mọi x ∈ ℝ", "#2E7D32");
                        AddQuadResult("📐 Tập nghiệm: S = ℝ", "#283593");
                    }
                    else
                    {
                        AddQuadResult($"✅ Nghiệm duy nhất: x = {Fmt(x0)}", "#2E7D32");
                        AddQuadResult($"📐 Tập nghiệm: S = {{{Fmt(x0)}}}", "#283593");
                    }
                }
            }
            else
            {
                double x1 = (-b - System.Math.Sqrt(delta)) / (2 * a);
                double x2 = (-b + System.Math.Sqrt(delta)) / (2 * a);
                if (x1 > x2) (x1, x2) = (x2, x1);

                AddQuadResult($"📊 Δ > 0 → Có hai nghiệm phân biệt: x₁ = {Fmt(x1)}, x₂ = {Fmt(x2)}", "#E65100");

                if (a > 0)
                {
                    AddQuadResult("💡 Hệ số a > 0: parabol hướng lên ∪", "#5C6BC0");
                    AddQuadResult($"💡 f(x) < 0 khi {Fmt(x1)} < x < {Fmt(x2)} (trong khoảng hai nghiệm)", "#5C6BC0");
                    AddQuadResult($"💡 f(x) > 0 khi x < {Fmt(x1)} hoặc x > {Fmt(x2)} (ngoài khoảng hai nghiệm)", "#5C6BC0");
                }
                else
                {
                    AddQuadResult("💡 Hệ số a < 0: parabol hướng xuống ∩", "#5C6BC0");
                    AddQuadResult($"💡 f(x) > 0 khi {Fmt(x1)} < x < {Fmt(x2)} (trong khoảng hai nghiệm)", "#5C6BC0");
                    AddQuadResult($"💡 f(x) < 0 khi x < {Fmt(x1)} hoặc x > {Fmt(x2)} (ngoài khoảng hai nghiệm)", "#5C6BC0");
                }

                string sol = "";
                bool strict = sign == 0 || sign == 2;
                string o = strict ? "(" : "[", cl = strict ? ")" : "]";
                if ((a > 0 && sign >= 2) || (a < 0 && sign <= 1)) // trong
                {
                    sol = $"{o}{Fmt(x1)}; {Fmt(x2)}{cl}";
                    AddQuadResult($"✅ Nghiệm: {Fmt(x1)} {(strict ? "<" : "≤")} x {(strict ? "<" : "≤")} {Fmt(x2)}", "#2E7D32");
                }
                else // ngoài
                {
                    sol = $"(-∞; {Fmt(x1)}{cl} ∪ {o}{Fmt(x2)}; +∞)";
                    AddQuadResult($"✅ Nghiệm: x {(strict ? "<" : "≤")} {Fmt(x1)} hoặc x {(strict ? ">" : "≥")} {Fmt(x2)}", "#2E7D32");
                }

                AddQuadResult($"📐 Tập nghiệm: S = {sol}", "#2E7D32");
            }
        }

        // ═══ GRAPH PLOTTING (Dynamic Bounds) ═══
        private void OpenGraph_Linear(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!ParsingHelper.TryParseDouble(txtIneqA1?.Text, out double a)) return;
                if (!ParsingHelper.TryParseDouble(txtIneqB1?.Text, out double b)) return;
                var ci = CultureInfo.InvariantCulture;
                int sign = cboSign1?.SelectedIndex ?? 0;
                string[] GraphSign = { ">", "\\ge", "<", "\\le" };

                // Calculate dynamic bounds centered on the root
                double root = a != 0 ? -b / a : 0;
                double left = root - 10;
                double right = root + 10;

                string expJs = $@"
        calc.setExpression({{id:'line', latex:'y={a.ToString(ci)}x+{b.ToString(ci)}', color:'#283593', lineWidth:3}});
        calc.setExpression({{id:'ineq', latex:'{a.ToString(ci)}x+{b.ToString(ci)}{GraphSign[sign]}0', color:'#5C6BC0', lineWidth:0}});
        calc.setExpression({{id:'xaxis', latex:'y=0', color:'#9E9E9E', lineWidth:1, lineStyle:'DASHED'}});
        calc.setMathBounds({{ left: {left.ToString(ci)}, right: {right.ToString(ci)}, bottom: -10, top: 10 }});";
                var win = new GraphWindow(expJs, $"📐 BPT: {Fmt(a)}x + ({Fmt(b)}) {GraphSign[sign].Replace("\\ge", "≥").Replace("\\le", "≤")} 0");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void OpenGraph_Quadratic(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!ParsingHelper.TryParseDouble(txtIneqA2?.Text, out double a)) return;
                if (!ParsingHelper.TryParseDouble(txtIneqB2?.Text, out double b)) return;
                if (!ParsingHelper.TryParseDouble(txtIneqC2?.Text, out double c)) return;
                var ci = CultureInfo.InvariantCulture;
                int sign = cboSign2?.SelectedIndex ?? 0;
                string[] GraphSign = { ">", "\\ge", "<", "\\le" };

                // Calculate dynamic math bounds based on quadratic features
                double left = -10, right = 10, bottom = -15, top = 15;
                string expJs;
                string title;
                if (a == 0)
                {
                    // Linear fallback graphing
                    double root = b != 0 ? -c / b : 0;
                    left = root - 10;
                    right = root + 10;
                    expJs = $@"
        calc.setExpression({{id:'para', latex:'y={b.ToString(ci)}x+{c.ToString(ci)}', color:'#283593', lineWidth:3}});
        calc.setExpression({{id:'ineq', latex:'{b.ToString(ci)}x+{c.ToString(ci)}{GraphSign[sign]}0', color:'#5C6BC0', lineWidth:0}});
        calc.setExpression({{id:'xaxis', latex:'y=0', color:'#9E9E9E', lineWidth:1, lineStyle:'DASHED'}});
        calc.setMathBounds({{ left: {left.ToString(ci)}, right: {right.ToString(ci)}, bottom: -10, top: 10 }});";
                    title = $"📐 BPT: {FormatLinearIneq(b, c, GraphSign[sign].Replace("\\ge", "≥").Replace("\\le", "≤"))}";
                }
                else
                {
                    double xv = -b / (2 * a);
                    double yv = a * xv * xv + b * xv + c;
                    double delta = b * b - 4 * a * c;
                    double halfWidth = 10;
                    if (delta > 0)
                    {
                        double x1 = (-b - System.Math.Sqrt(delta)) / (2 * a);
                        double x2 = (-b + System.Math.Sqrt(delta)) / (2 * a);
                        halfWidth = System.Math.Max(10, System.Math.Abs(x2 - x1) * 1.5);
                    }
                    left = xv - halfWidth;
                    right = xv + halfWidth;

                    double halfHeight = System.Math.Max(15, System.Math.Abs(yv) * 1.5);
                    if (a > 0) // opening upwards
                    {
                        bottom = yv - 5;
                        top = yv + halfHeight * 1.8;
                    }
                    else // opening downwards
                    {
                        bottom = yv - halfHeight * 1.8;
                        top = yv + 5;
                    }

                    expJs = $@"
        calc.setExpression({{id:'para', latex:'y={a.ToString(ci)}x^2+{b.ToString(ci)}x+{c.ToString(ci)}', color:'#283593', lineWidth:3}});
        calc.setExpression({{id:'ineq', latex:'{a.ToString(ci)}x^2+{b.ToString(ci)}x+{c.ToString(ci)}{GraphSign[sign]}0', color:'#5C6BC0', lineWidth:0}});
        calc.setExpression({{id:'xaxis', latex:'y=0', color:'#9E9E9E', lineWidth:1, lineStyle:'DASHED'}});
        calc.setMathBounds({{ left: {left.ToString(ci)}, right: {right.ToString(ci)}, bottom: {bottom.ToString(ci)}, top: {top.ToString(ci)} }});";
                    title = $"📐 BPT: {FormatQuadIneq(a, b, c, GraphSign[sign].Replace("\\ge", "≥").Replace("\\le", "≤"))}";
                }

                var win = new GraphWindow(expJs, title);
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        // ═══ PRESETS ═══
        private void BuildLinearPresets()
        {
            if (linearPresetPanel == null) return;
            linearPresetPanel.Children.Clear();
            var presets = new (string Label, double A, double B, int Sign)[]
            {
                ("2x − 6 > 0", 2, -6, 0),
                ("−3x + 9 ≤ 0", -3, 9, 3),
                ("x + 4 < 0", 1, 4, 2),
                ("−x − 2 ≥ 0", -1, -2, 1),
                ("5x ≥ 0", 5, 0, 1),
            };
            foreach (var (label, a, b, sign) in presets)
            {
                var btn = MakePresetButton(label, "#283593");
                double ca = a, cb = b; int cs = sign;
                btn.MouseLeftButtonDown += (_, _) =>
                {
                    txtIneqA1.Text = Fmt(ca);
                    txtIneqB1.Text = Fmt(cb);
                    cboSign1.SelectedIndex = cs;
                    SolveLinear();
                };
                linearPresetPanel.Children.Add(btn);
            }
        }

        private void BuildQuadPresets()
        {
            if (quadPresetPanel == null) return;
            quadPresetPanel.Children.Clear();
            var presets = new (string Label, double A, double B, double C, int Sign)[]
            {
                ("x² − 5x + 6 > 0", 1, -5, 6, 0),
                ("−x² + 4 ≥ 0", -1, 0, 4, 1),
                ("x² + 1 > 0", 1, 0, 1, 0),
                ("2x² − 3x − 2 ≤ 0", 2, -3, -2, 3),
                ("x² − 4x + 4 ≤ 0", 1, -4, 4, 3),
            };
            foreach (var (label, a, b, c, sign) in presets)
            {
                var btn = MakePresetButton(label, "#283593");
                double ca = a, cb = b, cc = c; int cs = sign;
                btn.MouseLeftButtonDown += (_, _) =>
                {
                    txtIneqA2.Text = Fmt(ca);
                    txtIneqB2.Text = Fmt(cb);
                    txtIneqC2.Text = Fmt(cc);
                    cboSign2.SelectedIndex = cs;
                    SolveQuadratic();
                };
                quadPresetPanel.Children.Add(btn);
            }
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
                        Icon = "💰",
                        Title = isVN ? "Tối Ưu Chi Phí" : "Linear Programming in Logistics",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_inequality_1_{suffix}.png",
                        Description = isVN 
                            ? "Bất phương trình mô tả các ràng buộc về ngân sách sản xuất tối đa và chỉ tiêu doanh thu tối thiểu, giúp doanh nghiệp tìm phương án tối ưu lợi nhuận." 
                            : "Maximize transport cargo loads under weight, volume, and budget inequalities (e.g. W <= 20 tons)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏗",
                        Title = isVN ? "️ Quy Hoạch Xây Dựng" : "Financial Budget Constraints",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_inequality_2_{suffix}.png",
                        Description = isVN 
                            ? "Quy hoạch tài nguyên tuyến tính sử dụng hệ bất phương trình để xác định giới hạn vật liệu (sắt, thép, xi măng) cho phép xây dựng an toàn và tiết kiệm." 
                            : "Optimize household or project spending so that total expenses do not exceed total available revenues: E <= R."
                    },
                    new PracticalAppItem
                    {
                        Icon = "✈",
                        Title = isVN ? "️ Vùng Bay An Toàn" : "Structural Safety Limits",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_inequality_3_{suffix}.png",
                        Description = isVN 
                            ? "Quy định độ cao bay an toàn tối thiểu và tối đa của máy bay tránh va chạm địa hình và điều kiện khí quyển được mô tả bằng hệ bất phương trình độ cao." 
                            : "Ensure bridge design load limits remain strictly below safe material breaking capacities: Load < Capacity."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌉",
                        Title = isVN ? "Tải Trọng Cầu Đường" : "Telecommunication Bandwidth",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_inequality_4_{suffix}.png",
                        Description = isVN 
                            ? "Giới hạn tải trọng giao thông đi qua cầu hoặc chất tải lên mặt đường là bất phương trình bất đẳng thức bảo vệ an toàn cho kết cấu hạ tầng giao thông." 
                            : "Manage traffic flow so that aggregate user requests stay within the maximum network channel capacity limit."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧪",
                        Title = isVN ? "Ngưỡng Phản Ứng" : "Quality Control Tolerance",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_inequality_5_{suffix}.png",
                        Description = isVN 
                            ? "Để phản ứng hóa học diễn ra an toàn và ổn định, nồng độ chất phản ứng và nhiệt độ lò nung cần được duy trì nghiêm ngặt trong một khoảng bất đẳng thức xác định." 
                            : "Reject manufactured parts whose dimensions exceed strict engineering tolerance boundaries (e.g. |d - D| <= 0.05mm)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⚙",
                        Title = isVN ? "️ Dung Sai Kỹ Thuật" : "Dietary Nutrition Planning",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_inequality_6_{suffix}.png",
                        Description = isVN 
                            ? "Dung sai kỹ thuật cơ khí mô tả giới hạn kích thước sai lệch cho phép của các chi tiết máy, giúp lắp ráp chính xác, tránh phế phẩm." 
                            : "Formulate menu options that satisfy daily minimum vitamin requirements and stay under maximum calorie limits."
                    }
                ,
                    new PracticalAppItem
                    {
                        Icon = "🚚",
                        Title = isVN ? "Tối ưu hóa tải trọng vận chuyển" : "Logistics Load Optimization",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_inequality_7_{suffix}.png",
                        Description = isVN 
                            ? "Thiết lập các bất phương trình ràng buộc về trọng lượng và thể tích của xe container để sắp xếp hàng hóa tối đa." 
                            : "Set constraint inequalities for weight and volume of containers to maximize shipped goods and avoid overloading."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌉",
                        Title = isVN ? "Biên độ an toàn cầu đường" : "Bridge Safety Margin",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_inequality_8_{suffix}.png",
                        Description = isVN 
                            ? "Tính toán hệ số an toàn bằng bất đẳng thức để đảm bảo tải trọng xe chạy qua luôn nhỏ hơn giới hạn chịu lực của cầu." 
                            : "Calculate safety margins using inequalities to ensure vehicle weight remains strictly below a bridge's load capacity."
                    }};

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for InequalityTool: {Err}", ex.Message);
            }
        }

        // ═══ HELPERS ═══
        private void AddLinearResult(string text, string colorHex)
        {
            var c = (Color)ColorConverter.ConvertFromString(colorHex);
            bool isKeyResult = text.StartsWith("✅") || text.StartsWith("📐") || text.StartsWith("❌") || text.StartsWith("⚠️");
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, c.R, c.G, c.B)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, isKeyResult ? 10 : 8, 14, isKeyResult ? 10 : 8),
                Margin = new Thickness(0, 0, 0, 4),
                Cursor = Cursors.Hand,
                ToolTip = "📋 Nhấn để copy"
            };
            border.Child = new TextBlock
            {
                Text = text,
                FontSize = isKeyResult ? 14 : 13,
                FontWeight = isKeyResult ? FontWeights.Bold : FontWeights.Normal,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(c),
                TextWrapping = TextWrapping.Wrap
            };
            string ct = text;
            border.MouseLeftButtonDown += (_, _) => { try { Clipboard.SetText(ct); ShowToast(); } catch { } };
            linearResultPanel.Children.Add(border);
        }

        private void AddQuadResult(string text, string colorHex)
        {
            var c = (Color)ColorConverter.ConvertFromString(colorHex);
            bool isKeyResult = text.StartsWith("✅") || text.StartsWith("📐") || text.StartsWith("❌") || text.StartsWith("⚠️");
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, c.R, c.G, c.B)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, isKeyResult ? 10 : 8, 14, isKeyResult ? 10 : 8),
                Margin = new Thickness(0, 0, 0, 4),
                Cursor = Cursors.Hand,
                ToolTip = "📋 Nhấn để copy"
            };
            border.Child = new TextBlock
            {
                Text = text,
                FontSize = isKeyResult ? 14 : 13,
                FontWeight = isKeyResult ? FontWeights.Bold : FontWeights.Normal,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(c),
                TextWrapping = TextWrapping.Wrap
            };
            string ct = text;
            border.MouseLeftButtonDown += (_, _) => { try { Clipboard.SetText(ct); ShowToast(); } catch { } };
            quadraticResultPanel.Children.Add(border);
        }

        private static Border MakePresetButton(string label, string colorHex)
        {
            var c = (Color)ColorConverter.ConvertFromString(colorHex);
            var btn = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(25, c.R, c.G, c.B)),
                BorderBrush = new SolidColorBrush(c),
                BorderThickness = new Thickness(0, 0, 0, 2),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 8, 8),
                Cursor = Cursors.Hand
            };
            btn.Child = new TextBlock
            {
                Text = label,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(c),
                FontFamily = new FontFamily("Segoe UI")
            };
            return btn;
        }

        private string FormatLinearIneq(double a, double b, string signStr)
        {
            if (a == 0) return $"{Fmt(b)} {signStr} 0";
            string termA = a == 1 ? "x" : (a == -1 ? "-x" : $"{Fmt(a)}x");
            string termB = b == 0 ? "" : (b > 0 ? $" + {Fmt(b)}" : $" - {Fmt(System.Math.Abs(b))}");
            return $"{termA}{termB} {signStr} 0";
        }

        private string FormatQuadIneq(double a, double b, double c, string signStr)
        {
            string termA = a == 1 ? "x²" : (a == -1 ? "-x²" : $"{Fmt(a)}x²");
            string termB = b == 0 ? "" : (b > 0 ? $" + {Fmt(b)}x" : $" - {Fmt(System.Math.Abs(b))}x");
            string termC = c == 0 ? "" : (c > 0 ? $" + {Fmt(c)}" : $" - {Fmt(System.Math.Abs(c))}");
            return $"{termA}{termB}{termC} {signStr} 0";
        }

        private async void ShowToast()
        {
            if (copyToast != null)
            {
                copyToast.IsOpen = true;
                await System.Threading.Tasks.Task.Delay(1000);
                copyToast.IsOpen = false;
            }
        }

        private static string Fmt(double v) => v == System.Math.Floor(v) ? ((int)v).ToString() : v.ToString("G6");
    }
}