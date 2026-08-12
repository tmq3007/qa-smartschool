using QASmartClass.LearningTools.Models;
using System;
using QASmartClass.LearningTools.Helpers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class TrigEquationTool : BaseToolControl
    {
        private string _func = "sin";
        private int _eqType = 0; // 0=basic,1=sin(kx+p),2=quadratic,3=asinx+bcosx,4=product
        private TextBox _txtValue, _txtK, _txtPhi, _txtA, _txtB, _txtC, _txtCoeffA, _txtCoeffB, _txtCoeffC;
        private TextBlock _txtFuncLabel;
        private string _activeTab = "calc";

        public TrigEquationTool()
        {
            InitializeComponent();
            Loaded += (_, _) => { 
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Tính toán & Ví dụ" : "Calculator & Examples";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn chi tiết" : "User Guide";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";

                if (sideMenu != null)
                {
                    sideMenu.SelectionChanged += SideMenu_SelectionChanged;
                    sideMenu.SelectedIndex = 0;
                }

                BuildTypeSelector(); 
                BuildUI(); 
                BuildTheory(); 
                SwitchTab(_activeTab); 
                LoadPracticalApps(); 
            };
        }

        private void BuildTypeSelector()
        {
            if (typePanel == null) return;
            typePanel.Children.Clear();
            var types = new[] {
                (0, "sin x = a", "Cơ bản"),
                (1, "sin(kx+φ) = a", "Mở rộng"),
                (2, "a·sin²x + b·sinx + c = 0", "Bậc 2"),
                (3, "a·sinx + b·cosx = c", "Tổng hợp"),
                (4, "sinx · cosx = a", "Tích"),
            };
            foreach (var (id, label, desc) in types)
            {
                bool active = id == _eqType;
                var btn = new Border
                {
                    Background = active ? new SolidColorBrush(Color.FromRgb(123,31,162)) : new SolidColorBrush(Color.FromRgb(243,229,245)),
                    CornerRadius = new CornerRadius(10), Padding = new Thickness(12,8,12,8),
                    Margin = new Thickness(0,0,6,6), Cursor = Cursors.Hand,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(206,147,216)), BorderThickness = new Thickness(1),
                    ToolTip = desc
                };
                btn.Child = new TextBlock {
                    Text = label, FontSize = 13, FontWeight = active ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = active ? Brushes.White : new SolidColorBrush(Color.FromRgb(123,31,162)),
                    FontFamily = new FontFamily("Segoe UI")
                };
                int capId = id;
                btn.MouseLeftButtonDown += (_, _) => { _eqType = capId; BuildTypeSelector(); BuildUI(); };
                typePanel.Children.Add(btn);
            }
        }

        private void BuildUI()
        {
            BuildFuncSelector();
            BuildInputPanel();
            BuildPresets();
            Solve();
        }

        private void BuildFuncSelector()
        {
            if (funcPanel == null) return;
            funcPanel.Children.Clear();
            if (_eqType >= 3) return; // no func selector for types 3,4
            var funcs = new[] { ("sin", "#7B1FA2"), ("cos", "#1565C0"), ("tan", "#E65100"), ("cot", "#2E7D32") };
            foreach (var (name, hex) in funcs)
            {
                bool active = name == _func;
                var color = (Color)ColorConverter.ConvertFromString(hex);
                var btn = new Border
                {
                    Background = active ? new SolidColorBrush(color) : new SolidColorBrush(Color.FromArgb(20, color.R, color.G, color.B)),
                    CornerRadius = new CornerRadius(10), Padding = new Thickness(14,7,14,7),
                    Margin = new Thickness(0,0,6,6), Cursor = Cursors.Hand,
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, color.R, color.G, color.B)),
                    BorderThickness = active ? new Thickness(2) : new Thickness(1)
                };
                string lbl = _eqType == 0 ? $"{name} x = a" : _eqType == 1 ? $"{name}(kx+φ) = a" : $"a·{name}²x+b·{name}x+c=0";
                btn.Child = new TextBlock { Text = lbl, FontSize = 13, FontWeight = active ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = active ? Brushes.White : new SolidColorBrush(color), FontFamily = new FontFamily("Segoe UI") };
                string n = name;
                btn.MouseLeftButtonDown += (_, _) => { _func = n; BuildUI(); };
                funcPanel.Children.Add(btn);
            }
        }

        private TextBox MakeInput(string text, string fg = "#4A148C")
        {
            var tb = new TextBox { Width = 70, FontSize = 16, Padding = new Thickness(6,4,6,4), Text = text,
                TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold, Background = Brushes.White,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(fg) };
            tb.TextChanged += (_, _) => Solve();
            // Bàn phím số mini cho màn hình tương tác
            TouchNumPad.Attach(tb, step: 0.1, allowDecimal: true);
            return tb;
        }

        private void BuildInputPanel()
        {
            if (inputPanel == null) return;
            inputPanel.Children.Clear();
            if (txtInputTitle != null) txtInputTitle.Text = _eqType switch {
                0 => "📈 Dạng cơ bản: f(x) = a", 1 => "📈 Dạng mở rộng: f(kx+φ) = a",
                2 => "📈 Dạng bậc 2", 3 => "📈 a·sinx + b·cosx = c", _ => "📈 sinx · cosx = a" };

            var bg = new Border { Background = new SolidColorBrush(Color.FromRgb(243,229,245)),
                CornerRadius = new CornerRadius(10), Padding = new Thickness(12,8,12,8), Margin = new Thickness(0,0,0,4) };
            var sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            switch (_eqType)
            {
                case 0: // sin x = a
                    _txtFuncLabel = new TextBlock { Text = $"{_func} x =", FontSize = 16, FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(123,31,162)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,8,0) };
                    _txtValue = MakeInput("0.5");
                    sp.Children.Add(_txtFuncLabel); sp.Children.Add(_txtValue);
                    break;
                case 1: // sin(kx + phi) = a
                    _txtFuncLabel = new TextBlock { Text = $"{_func}(", FontSize = 16, FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(123,31,162)), VerticalAlignment = VerticalAlignment.Center };
                    _txtK = MakeInput("2"); _txtPhi = MakeInput("1.0472"); _txtValue = MakeInput("0.5");
                    sp.Children.Add(_txtFuncLabel);
                    sp.Children.Add(_txtK);
                    sp.Children.Add(new TextBlock { Text = "x +", FontSize = 16, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4,0,4,0) });
                    sp.Children.Add(_txtPhi);
                    sp.Children.Add(new TextBlock { Text = ") =", FontSize = 16, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4,0,4,0) });
                    sp.Children.Add(_txtValue);
                    break;
                case 2: // a*sin^2(x) + b*sin(x) + c = 0
                    _txtCoeffA = MakeInput("2"); _txtCoeffB = MakeInput("-3"); _txtCoeffC = MakeInput("1");
                    sp.Children.Add(_txtCoeffA);
                    sp.Children.Add(new TextBlock { Text = $"·{_func}²x +", FontSize = 16, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4,0,4,0) });
                    sp.Children.Add(_txtCoeffB);
                    sp.Children.Add(new TextBlock { Text = $"·{_func}x +", FontSize = 16, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4,0,4,0) });
                    sp.Children.Add(_txtCoeffC);
                    sp.Children.Add(new TextBlock { Text = "= 0", FontSize = 16, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4,0,0,0), FontWeight = FontWeights.Bold });
                    break;
                case 3: // a*sinx + b*cosx = c
                    _txtA = MakeInput("1"); _txtB = MakeInput("1.7321"); _txtC = MakeInput("1");
                    sp.Children.Add(_txtA);
                    sp.Children.Add(new TextBlock { Text = "·sinx +", FontSize = 16, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4,0,4,0) });
                    sp.Children.Add(_txtB);
                    sp.Children.Add(new TextBlock { Text = "·cosx =", FontSize = 16, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4,0,4,0) });
                    sp.Children.Add(_txtC);
                    break;
                case 4: // sinx * cosx = a  <=>  sin2x = 2a
                    _txtValue = MakeInput("0.25");
                    sp.Children.Add(new TextBlock { Text = "sinx · cosx =", FontSize = 16, FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(123,31,162)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,8,0) });
                    sp.Children.Add(_txtValue);
                    break;
            }
            bg.Child = sp;
            inputPanel.Children.Add(bg);
        }

        private void BuildPresets()
        {
            if (presetPanel == null) return;
            presetPanel.Children.Clear();
            (string Lbl, string Func, string[] Vals)[] presets = _eqType switch {
                0 => (_func == "sin" || _func == "cos")
                    ? new[] { ("0","0"),("1/2","0.5"),("√2/2","0.7071"),("√3/2","0.8660"),("1","1"),("-1/2","-0.5"),("-1","-1") }.Select(v => (v.Item1, "", new[]{v.Item2})).ToArray()
                    : new[] { ("0","0"),("1","1"),("√3","1.7321"),("-1","-1") }.Select(v => (v.Item1, "", new[]{v.Item2})).ToArray(),
                1 => new (string,string,string[])[] { 
                    ("sin(2x+π/3)=1/2", "sin", new[]{"2","1.0472","0.5"}), 
                    ("cos(3x-π/4)=0", "cos", new[]{"3","-0.7854","0"}) 
                },
                2 => new (string,string,string[])[] { 
                    ("2sin²x-3sinx+1=0", "sin", new[]{"2","-3","1"}), 
                    ("cos²x-cosx=0", "cos", new[]{"1","-1","0"}), 
                    ("2sin²x+sinx-1=0", "sin", new[]{"2","1","-1"}),
                    ("tan²x-3tanx+2=0", "tan", new[]{"1","-3","2"}),
                    ("3cot²x-4cotx+1=0", "cot", new[]{"3","-4","1"})
                },
                3 => new (string,string,string[])[] { 
                    ("sinx+√3·cosx=1", "", new[]{"1","1.7321","1"}), 
                    ("3sinx+4cosx=5", "", new[]{"3","4","5"}), 
                    ("sinx-cosx=1", "", new[]{"1","-1","1"}) 
                },
                4 => new (string,string,string[])[] { 
                    ("sinx·cosx=1/4", "", new[]{"0.25"}), 
                    ("sinx·cosx=0", "", new[]{"0"}), 
                    ("sinx·cosx=√3/4", "", new[]{"0.4330"}) 
                },
                _ => Array.Empty<(string,string,string[])>()
            };
            foreach (var (lbl, func, vals) in presets)
            {
                var btn = new Border { Background = new SolidColorBrush(Color.FromRgb(243,229,245)),
                    CornerRadius = new CornerRadius(8), Padding = new Thickness(10,5,10,5),
                    Margin = new Thickness(0,0,6,6), Cursor = Cursors.Hand,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(225,190,231)), BorderThickness = new Thickness(1) };
                btn.Child = new TextBlock { Text = $"📌 {lbl}", FontSize = 12, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(123,31,162)) };
                var cv = vals;
                string cf = func;
                btn.MouseLeftButtonDown += (_, _) => ApplyPreset(cf, cv);
                btn.MouseEnter += (_, _) => btn.Background = new SolidColorBrush(Color.FromRgb(225,190,231));
                btn.MouseLeave += (_, _) => btn.Background = new SolidColorBrush(Color.FromRgb(243,229,245));
                presetPanel.Children.Add(btn);
            }
        }

        private void ApplyPreset(string func, string[] vals)
        {
            if (!string.IsNullOrEmpty(func))
            {
                _func = func;
            }
            switch (_eqType)
            {
                case 0: case 4: if (_txtValue != null) _txtValue.Text = vals[0]; break;
                case 1: if (_txtK!=null) _txtK.Text=vals[0]; if (_txtPhi!=null) _txtPhi.Text=vals[1]; if (_txtValue!=null) _txtValue.Text=vals[2]; break;
                case 2: if (_txtCoeffA!=null) _txtCoeffA.Text=vals[0]; if (_txtCoeffB!=null) _txtCoeffB.Text=vals[1]; if (_txtCoeffC!=null) _txtCoeffC.Text=vals[2]; break;
                case 3: if (_txtA!=null) _txtA.Text=vals[0]; if (_txtB!=null) _txtB.Text=vals[1]; if (_txtC!=null) _txtC.Text=vals[2]; break;
            }
            SwitchTab("calc");
            BuildUI();
        }

        private double P(TextBox t) => ParsingHelper.TryParseDouble(t?.Text, out double v) ? v : double.NaN;

        private void Solve()
        {
            try
            {
                if (txtResult == null) return;
                if (stepsPanel != null) stepsPanel.Children.Clear();
                switch (_eqType)
                {
                    case 0: SolveBasic(); break;
                    case 1: SolveExtended(); break;
                    case 2: SolveQuadratic(); break;
                    case 3: SolveLinearComb(); break;
                    case 4: SolveProduct(); break;
                }
            }
            catch { }
        }

        private void SolveBasic()
        {
            double a = P(_txtValue); if (double.IsNaN(a)) { txtResult.Text = "Giá trị không hợp lệ"; return; }
            switch (_func)
            {
                case "sin":
                    if (a < -1 || a > 1) { SetNo("sin x ∈ [-1;1]"); return; }
                    // Special values
                    if (System.Math.Abs(a - 1.0) < 1e-4)
                    {
                        SetOK("x = π/2 + k2π\n(k ∈ ℤ)", "Điều kiện: x ∈ ℝ", "x = π/2 ≈ 90°");
                        AS("B1: PT", "sin x = 1", "#7B1FA2");
                        AS("B2: Nghiệm đặc biệt", "x = π/2 + k2π", "#2E7D32");
                        return;
                    }
                    if (System.Math.Abs(a + 1.0) < 1e-4)
                    {
                        SetOK("x = −π/2 + k2π\n(k ∈ ℤ)", "Điều kiện: x ∈ ℝ", "x = 3π/2 ≈ 270°");
                        AS("B1: PT", "sin x = -1", "#7B1FA2");
                        AS("B2: Nghiệm đặc biệt", "x = −π/2 + k2π", "#2E7D32");
                        return;
                    }
                    if (System.Math.Abs(a) < 1e-4)
                    {
                        SetOK("x = kπ\n(k ∈ ℤ)", "Điều kiện: x ∈ ℝ", "x = 0 ≈ 0°\nx = π ≈ 180°");
                        AS("B1: PT", "sin x = 0", "#7B1FA2");
                        AS("B2: Nghiệm đặc biệt", "x = kπ", "#2E7D32");
                        return;
                    }
                    // General case
                    double al = System.Math.Asin(a);
                    string solSin = $"{FormatSolution(al, 2*System.Math.PI)}\n{FormatSolution(System.Math.PI - al, 2*System.Math.PI)}\n(k ∈ ℤ)";
                    SetOK(solSin, "Điều kiện: x ∈ ℝ", GetSA("sin",a));
                    AS("B1: PT", $"sin x = {a}","#7B1FA2"); 
                    AS("B2: α=arcsin(a)", $"α = arcsin({a}) = {FormatRad(al)} ≈ {Deg(al)}°","#00695C");
                    AS("B3: Nghiệm TQ", $"{FormatSolution(al, 2*System.Math.PI)}\n{FormatSolution(System.Math.PI - al, 2*System.Math.PI)}","#2E7D32"); 
                    break;
                case "cos":
                    if (a < -1 || a > 1) { SetNo("cos x ∈ [-1;1]"); return; }
                    // Special values
                    if (System.Math.Abs(a - 1.0) < 1e-4)
                    {
                        SetOK("x = k2π\n(k ∈ ℤ)", "Điều kiện: x ∈ ℝ", "x = 0 ≈ 0°");
                        AS("B1: PT", "cos x = 1", "#7B1FA2");
                        AS("B2: Nghiệm đặc biệt", "x = k2π", "#2E7D32");
                        return;
                    }
                    if (System.Math.Abs(a + 1.0) < 1e-4)
                    {
                        SetOK("x = π + k2π\n(k ∈ ℤ)", "Điều kiện: x ∈ ℝ", "x = π ≈ 180°");
                        AS("B1: PT", "cos x = -1", "#7B1FA2");
                        AS("B2: Nghiệm đặc biệt", "x = π + k2π", "#2E7D32");
                        return;
                    }
                    if (System.Math.Abs(a) < 1e-4)
                    {
                        SetOK("x = π/2 + kπ\n(k ∈ ℤ)", "Điều kiện: x ∈ ℝ", "x = π/2 ≈ 90°\nx = 3π/2 ≈ 270°");
                        AS("B1: PT", "cos x = 0", "#7B1FA2");
                        AS("B2: Nghiệm đặc biệt", "x = π/2 + kπ", "#2E7D32");
                        return;
                    }
                    // General case
                    double be = System.Math.Acos(a);
                    string solCos = $"x = ±{FormatRad(be)} + k2π\n(k ∈ ℤ)";
                    SetOK(solCos, "Điều kiện: x ∈ ℝ", GetSA("cos",a));
                    AS("B1: PT", $"cos x = {a}","#7B1FA2"); 
                    AS("B2: β=arccos(a)", $"β = arccos({a}) = {FormatRad(be)} ≈ {Deg(be)}°","#00695C");
                    AS("B3: Nghiệm TQ", $"x = ±{FormatRad(be)} + k2π","#2E7D32"); 
                    break;
                case "tan":
                    double ga = System.Math.Atan(a);
                    string solTan = $"{FormatSolution(ga, System.Math.PI)}\n(k ∈ ℤ)";
                    SetOK(solTan, "Điều kiện: x ≠ π/2 + kπ (k ∈ ℤ)", GetSA("tan",a));
                    AS("B1: PT", $"tan x = {a}","#7B1FA2"); 
                    AS("B2: γ=arctan(a)", $"γ = arctan({a}) = {FormatRad(ga)} ≈ {Deg(ga)}°","#00695C");
                    AS("B3: Nghiệm TQ", FormatSolution(ga, System.Math.PI),"#2E7D32"); 
                    break;
                default: // cot
                    if (System.Math.Abs(a) < 1e-9)
                    {
                        SetOK("x = π/2 + kπ\n(k ∈ ℤ)", "Điều kiện: x ≠ kπ (k ∈ ℤ)", "x = π/2 ≈ 90°\nx = 3π/2 ≈ 270°");
                        AS("B1: PT", "cot x = 0", "#7B1FA2");
                        AS("B2: Nghiệm đặc biệt", "x = π/2 + kπ", "#2E7D32");
                        return;
                    }
                    double de = a > 0 ? System.Math.Atan(1.0/a) : System.Math.PI + System.Math.Atan(1.0/a);
                    string solCot = $"{FormatSolution(de, System.Math.PI)}\n(k ∈ ℤ)";
                    SetOK(solCot, "Điều kiện: x ≠ kπ (k ∈ ℤ)", GetSA("cot",a));
                    AS("B1: PT", $"cot x = {a}","#7B1FA2"); 
                    AS("B2: γ=arccot(a)", $"γ = arccot({a}) = {FormatRad(de)} ≈ {Deg(de)}°","#00695C");
                    AS("B3: Nghiệm TQ", FormatSolution(de, System.Math.PI),"#2E7D32"); 
                    break;
            }
        }

        private void SolveExtended()
        {
            double k = P(_txtK), phi = P(_txtPhi), a = P(_txtValue);
            if (double.IsNaN(k)||double.IsNaN(phi)||double.IsNaN(a)||k==0) { txtResult.Text = "Nhập chưa hợp lệ (k≠0)"; return; }
            string fStr = _func;
            bool isSinCos = fStr == "sin" || fStr == "cos";
            if (isSinCos && (a < -1 || a > 1)) { SetNo($"{fStr} ∈ [-1;1]"); return; }

            string phiStr = FormatRad(phi);
            string eqLeft = phi < 0 
                ? $"{fStr}({k}x − {FormatRad(System.Math.Abs(phi))})" 
                : $"{fStr}({k}x + {phiStr})";

            AS("B1: PT", $"{eqLeft} = {a}","#7B1FA2");
            AS("B2: Đặt u", $"Đặt u = {k}x " + (phi < 0 ? $"− {FormatRad(System.Math.Abs(phi))}" : $"+ {phiStr}") + $"\n→ {fStr}(u) = {a}","#1565C0");

            double angle = fStr switch { 
                "sin" => System.Math.Asin(a), 
                "cos" => System.Math.Acos(a), 
                "tan" => System.Math.Atan(a), 
                _ => (System.Math.Abs(a) > 1e-9) ? System.Math.Atan(1.0/a) : System.Math.PI/2 
            };
            if (fStr == "cot" && a < 0) angle = System.Math.PI + System.Math.Atan(1.0/a);

            string aStr = FormatRad(angle);
            double T = (fStr == "tan" || fStr == "cot") ? System.Math.PI / System.Math.Abs(k) : 2 * System.Math.PI / System.Math.Abs(k);

            string sol = "";
            string condText = "";

            if (fStr == "sin")
            {
                string u1 = $"u = {aStr} + k2π", u2 = $"u = π − {aStr} + k2π";
                AS("B3: Nghiệm u", $"{u1}\n{u2}","#00695C");

                double x1 = (angle - phi) / k, x2 = (System.Math.PI - angle - phi) / k;
                
                // Check if duplicate solution sets
                double diff = (x1 - x2) / T;
                bool isDup = System.Math.Abs(diff - System.Math.Round(diff)) < 1e-4;

                string term1 = phi < 0 ? $"{aStr} + {FormatRad(System.Math.Abs(phi))}" : $"{aStr} − {phiStr}";
                string term2 = phi < 0 ? $"π − {aStr} + {FormatRad(System.Math.Abs(phi))}" : $"π − {aStr} − {phiStr}";
                
                AS("B4: Thế lại", $"x = (u − φ)/{k}\nx₁ = ({term1})/{k} = {FormatRad(x1)}\nx₂ = ({term2})/{k} = {FormatRad(x2)}","#2E7D32");

                if (isDup)
                {
                    sol = $"{FormatSolution(x1, T)}\n(k ∈ ℤ)";
                    SetOK(sol, $"Điều kiện: x ∈ ℝ, k={k}", $"{FormatSolution(x1, T)}");
                }
                else
                {
                    sol = $"{FormatSolution(x1, T)}\n{FormatSolution(x2, T)}\n(k ∈ ℤ)";
                    SetOK(sol, $"Điều kiện: x ∈ ℝ, k={k}", $"{FormatSolution(x1, T)}\n{FormatSolution(x2, T)}");
                }
            }
            else if (fStr == "cos")
            {
                AS("B3: u = ±β + k2π", $"u = ±{aStr} + k2π","#00695C");

                double x1 = (angle - phi) / k, x2 = (-angle - phi) / k;

                // Check if duplicate solution sets
                double diff = (x1 - x2) / T;
                bool isDup = System.Math.Abs(diff - System.Math.Round(diff)) < 1e-4;

                string term1 = phi < 0 ? $"{aStr} + {FormatRad(System.Math.Abs(phi))}" : $"{aStr} − {phiStr}";
                string term2 = phi < 0 ? $"−{aStr} + {FormatRad(System.Math.Abs(phi))}" : $"−{aStr} − {phiStr}";

                AS("B4: x = (u − φ)/{k}", $"x₁ = ({term1})/{k} = {FormatRad(x1)}\nx₂ = ({term2})/{k} = {FormatRad(x2)}","#2E7D32");

                if (isDup)
                {
                    sol = $"{FormatSolution(x1, T)}\n(k ∈ ℤ)";
                    SetOK(sol, $"Điều kiện: x ∈ ℝ, k={k}", $"{FormatSolution(x1, T)}");
                }
                else
                {
                    sol = $"{FormatSolution(x1, T)}\n{FormatSolution(x2, T)}\n(k ∈ ℤ)";
                    SetOK(sol, $"Điều kiện: x ∈ ℝ, k={k}", $"{FormatSolution(x1, T)}\n{FormatSolution(x2, T)}");
                }
            }
            else // tan or cot
            {
                string uSymb = fStr == "tan" ? "arctan" : "arccot";
                AS($"B3: u = {uSymb}(a) + kπ", $"u = {aStr} + kπ","#00695C");

                double x1 = (angle - phi) / k;
                string term1 = phi < 0 ? $"{aStr} + {FormatRad(System.Math.Abs(phi))}" : $"{aStr} − {phiStr}";

                AS("B4: x = (u − φ)/k", $"x = ({term1})/{k} = {FormatRad(x1)}","#2E7D32");

                sol = $"{FormatSolution(x1, T)}\n(k ∈ ℤ)";

                if (fStr == "tan")
                {
                    double condVal = (System.Math.PI/2 - phi) / k;
                    double condPeriod = System.Math.PI / System.Math.Abs(k);
                    condText = $"Điều kiện: x ≠ {FormatRad(condVal)} + m{FormatRad(condPeriod)} (m ∈ ℤ)";
                }
                else // cot
                {
                    double condVal = -phi / k;
                    double condPeriod = System.Math.PI / System.Math.Abs(k);
                    condText = $"Điều kiện: x ≠ {FormatRad(condVal)} + m{FormatRad(condPeriod)} (m ∈ ℤ)";
                }

                SetOK(sol, condText, $"{FormatSolution(x1, T)}");
            }
        }

        private void SolveQuadratic()
        {
            double a = P(_txtCoeffA), b = P(_txtCoeffB), c = P(_txtCoeffC);
            if (double.IsNaN(a)||double.IsNaN(b)||double.IsNaN(c)||a==0) { txtResult.Text = "a ≠ 0"; return; }
            double delta = b*b - 4*a*c;
            string f = _func;
            AS("B1: PT", $"{a}·{f}²x + ({b})·{f}x + ({c}) = 0","#7B1FA2");
            AS("B2: Đặt t", $"Đặt t = {f}x" + (f=="sin"||f=="cos" ? ", t ∈ [-1;1]" : "") + $"\n→ {a}t² + ({b})t + ({c}) = 0","#1565C0");
            AS("B3: Δ", $"Δ = ({b})² − 4·({a})·({c}) = {delta:G6}","#00695C");

            if (delta < 0) { SetNo("Δ < 0 → PT bậc 2 theo t vô nghiệm"); return; }
            var roots = new List<double>();
            if (delta == 0) { roots.Add(-b/(2*a)); }
            else { roots.Add((-b+System.Math.Sqrt(delta))/(2*a)); roots.Add((-b-System.Math.Sqrt(delta))/(2*a)); }

            var solLines = new List<string>();
            bool sincos = f == "sin" || f == "cos";
            int step = 4;
            foreach (var t in roots)
            {
                if (sincos && (t < -1 || t > 1)) { AS($"B{step}: t={t:G6}", $"|t| > 1 → loại","#C62828"); step++; continue; }
                double ang = f=="sin" ? System.Math.Asin(t) : f=="cos" ? System.Math.Acos(t) : f=="tan" ? System.Math.Atan(t) : (t!=0?System.Math.Atan(1.0/t):System.Math.PI/2);
                if (f == "cot" && t < 0) ang = System.Math.PI + System.Math.Atan(1.0/t);

                string aStr = FormatRad(ang);
                string subSol;
                if (f == "sin")
                {
                    if (System.Math.Abs(t - 1.0) < 1e-4) subSol = "sinx=1 → x=π/2+k2π";
                    else if (System.Math.Abs(t + 1.0) < 1e-4) subSol = "sinx=-1 → x=−π/2+k2π";
                    else if (System.Math.Abs(t) < 1e-4) subSol = "sinx=0 → x=kπ";
                    else subSol = $"sinx={t:G4} → x={aStr}+k2π hoặc x={FormatRad(System.Math.PI-ang)}+k2π";
                }
                else if (f == "cos")
                {
                    if (System.Math.Abs(t - 1.0) < 1e-4) subSol = "cosx=1 → x=k2π";
                    else if (System.Math.Abs(t + 1.0) < 1e-4) subSol = "cosx=-1 → x=π+k2π";
                    else if (System.Math.Abs(t) < 1e-4) subSol = "cosx=0 → x=π/2+kπ";
                    else subSol = $"cosx={t:G4} → x=±{aStr}+k2π";
                }
                else
                {
                    subSol = $"{f}x={t:G4} → {FormatSolution(ang, System.Math.PI)}";
                }
                solLines.Add(subSol);
                AS($"B{step}: {f}x = {t:G6}", $"→ x = {aStr} ≈ {Deg(ang)}°","#2E7D32"); step++;
            }
            if (solLines.Count == 0) { SetNo("Tất cả nghiệm t đều ngoài TXĐ"); return; }

            string condText;
            if (sincos) condText = "Điều kiện: t ∈ [-1; 1]";
            else if (f == "tan") condText = "Điều kiện: x ≠ π/2 + mπ (m ∈ ℤ)";
            else condText = "Điều kiện: x ≠ mπ (m ∈ ℤ)";

            SetOK(string.Join("\n", solLines) + "\n(k ∈ ℤ)", condText, "");
        }

        private void SolveLinearComb()
        {
            double a = P(_txtA), b = P(_txtB), c = P(_txtC);
            if (double.IsNaN(a)||double.IsNaN(b)||double.IsNaN(c)) { txtResult.Text = "Nhập chưa hợp lệ"; return; }
            if (System.Math.Abs(a) < 1e-9 && System.Math.Abs(b) < 1e-9)
            {
                txtResult.Text = "Hệ số a và b không được đồng thời bằng 0";
                if (txtSpecial != null) txtSpecial.Text = "";
                if (txtCondition != null) txtCondition.Text = "❌ Không hợp lệ";
                return;
            }

            double R = System.Math.Sqrt(a*a + b*b);
            if (System.Math.Abs(c) > R + 1e-9) { SetNo($"|c| = {System.Math.Abs(c):G4} > √(a²+b²) = {R:G4}"); return; }

            double phi = System.Math.Atan2(b, a);
            string phiS = FormatRad(phi);
            AS("B1: PT", $"{a}·sinx + {b}·cosx = {c}","#7B1FA2");
            AS("B2: R = √(a²+b²)", $"R = √({a}²+{b}²) = {R:G6}","#1565C0");
            
            string phiEqPart = phi < 0 
                ? $"sin(x − {FormatRad(System.Math.Abs(phi))})" 
                : $"sin(x + {phiS})";
            AS("B3: Chia 2 vế cho R", $"(a/R)·sinx + (b/R)·cosx = c/R\n→ {phiEqPart} = {c}/{R:G4} = {(c/R):G6}\nvới φ = {phiS} (thỏa cosφ = {a}/{R:G4}, sinφ = {b}/{R:G4})","#00695C");

            double val = c / R;
            double alpha = System.Math.Asin(val);
            string alphaS = FormatRad(alpha);
            
            string alphaEqPart = phi < 0 
                ? $"x − {FormatRad(System.Math.Abs(phi))}" 
                : $"x + {phiS}";
            AS("B4: Giải sin(x+φ) = c/R", $"{alphaEqPart} = {alphaS} + k2π\n{alphaEqPart} = π − {alphaS} + k2π","#2E7D32");

            double x1 = alpha - phi, x2 = System.Math.PI - alpha - phi;
            double T = 2 * System.Math.PI;

            // Check if duplicate solution sets
            double diff = (x1 - x2) / T;
            bool isDup = System.Math.Abs(diff - System.Math.Round(diff)) < 1e-4;

            string sol;
            if (isDup)
            {
                sol = $"{FormatSolution(x1, T)}\n(k ∈ ℤ)";
                SetOK(sol, $"Điều kiện: |c|≤R={R:G4} ✓", $"{FormatSolution(x1, T)}");
            }
            else
            {
                sol = $"{FormatSolution(x1, T)}\n{FormatSolution(x2, T)}\n(k ∈ ℤ)";
                SetOK(sol, $"Điều kiện: |c|≤R={R:G4} ✓", $"{FormatSolution(x1, T)}\n{FormatSolution(x2, T)}");
            }

            AS("B5: Nghiệm", (isDup ? $"{FormatSolution(x1, T)}" : $"{FormatSolution(x1, T)}\n{FormatSolution(x2, T)}") + $"\n≈ {Deg(x1)}° hoặc {Deg(x2)}° + k·360°","#E65100");
        }

        private void SolveProduct()
        {
            double a = P(_txtValue); if (double.IsNaN(a)) { txtResult.Text = "Nhập chưa hợp lệ"; return; }
            AS("B1: PT", $"sinx · cosx = {a}","#7B1FA2");
            AS("B2: Công thức nhân đôi", $"sinx·cosx = sin(2x)/2\n→ sin(2x)/2 = {a}\n→ sin(2x) = {(2*a):G6}","#1565C0");
            double val = 2 * a;
            if (val < -1 || val > 1) { SetNo($"|sin(2x)| = |{val:G4}| > 1"); return; }
            double alpha = System.Math.Asin(val);
            string als = FormatRad(alpha);
            AS("B3: Giải sin(2x) = " + $"{val:G4}", $"2x = {als} + k2π\n2x = π − {als} + k2π","#00695C");

            double x1 = alpha/2, x2 = System.Math.PI/2 - alpha/2;
            double T = System.Math.PI;

            // Check if duplicate solution sets
            double diff = (x1 - x2) / T;
            bool isDup = System.Math.Abs(diff - System.Math.Round(diff)) < 1e-4;

            string sol;
            if (isDup)
            {
                sol = $"{FormatSolution(x1, T)}\n(k ∈ ℤ)";
                SetOK(sol, "Điều kiện: |2a|≤1 ✓", $"{FormatSolution(x1, T)}");
            }
            else
            {
                sol = $"{FormatSolution(x1, T)}\n{FormatSolution(x2, T)}\n(k ∈ ℤ)";
                SetOK(sol, "Điều kiện: |2a|≤1 ✓", $"{FormatSolution(x1, T)}\n{FormatSolution(x2, T)}");
            }

            AS("B4: Chia 2", (isDup ? $"{FormatSolution(x1, T)}" : $"{FormatSolution(x1, T)}\n{FormatSolution(x2, T)}") + $"\n≈ {Deg(x1)}° hoặc {Deg(x2)}° + k·180°","#2E7D32");
        }

        private void SetOK(string result, string cond, string special)
        {
            txtResult.Text = result;
            if (txtSpecial != null) txtSpecial.Text = string.IsNullOrEmpty(special) ? "—" : special;
            if (txtCondition != null) txtCondition.Text = cond;
            SetStyle("#F3E5F5","#4A148C");
        }

        private void SetNo(string reason)
        {
            txtResult.Text = "Phương trình vô nghiệm";
            if (txtSpecial != null) txtSpecial.Text = "";
            if (txtCondition != null) txtCondition.Text = $"❌ {reason}";
            SetStyle("#FFEBEE","#C62828");
        }

        private void SetStyle(string bg, string fg)
        {
            if (resultCard != null) resultCard.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(bg);
            if (txtResult != null) txtResult.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(fg);
        }

        private void AS(string title, string content, string c)
        {
            if (stepsPanel == null) return;
            var color = (Color)ColorConverter.ConvertFromString(c);
            var card = new Border { CornerRadius = new CornerRadius(10), Padding = new Thickness(14,10,14,10), Margin = new Thickness(0,0,0,6),
                Background = new SolidColorBrush(Color.FromArgb(15,color.R,color.G,color.B)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60,color.R,color.G,color.B)), BorderThickness = new Thickness(0,0,0,2) };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(color) });
            sp.Children.Add(new TextBlock { Text = content, FontSize = 13, Margin = new Thickness(0,4,0,0), Foreground = new SolidColorBrush(Color.FromRgb(55,71,79)),
                FontFamily = new FontFamily("Segoe UI"), TextWrapping = TextWrapping.Wrap, LineHeight = 22 });
            card.Child = sp;
            stepsPanel.Children.Add(WrapWithSectionToolbar(card, "trig_eq", $"t{stepsPanel.Children.Count}", title));
        }

        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var ci = CultureInfo.InvariantCulture;
                string expJs;
                switch (_eqType)
                {
                    case 0:
                        double a0 = P(_txtValue); if(double.IsNaN(a0)) return;
                        string fl0 = _func=="cot" ? "\\cot(x)" : $"\\{_func}(x)";
                        expJs = $"calc.setExpression({{id:'f',latex:'y={fl0}',color:'#7B1FA2',lineWidth:3}});calc.setExpression({{id:'l',latex:'y={a0.ToString(ci)}',color:'#E53935',lineWidth:2,lineStyle:'DASHED'}});calc.setMathBounds({{left:-2*System.Math.PI,right:2*System.Math.PI,bottom:-3,top:3}});";
                        break;
                    case 1:
                        double k1=P(_txtK),p1=P(_txtPhi),a1=P(_txtValue); if(double.IsNaN(k1)||double.IsNaN(a1)) return;
                        string fl1 = _func=="cot" ? "\\cot" : $"\\{_func}";
                        expJs = $"calc.setExpression({{id:'f',latex:'y={fl1}({k1.ToString(ci)}x+{p1.ToString(ci)})',color:'#7B1FA2',lineWidth:3}});calc.setExpression({{id:'l',latex:'y={a1.ToString(ci)}',color:'#E53935',lineWidth:2,lineStyle:'DASHED'}});calc.setMathBounds({{left:-2*System.Math.PI,right:2*System.Math.PI,bottom:-3,top:3}});";
                        break;
                    case 3:
                        double aa=P(_txtA),bb=P(_txtB),cc=P(_txtC); if(double.IsNaN(aa)) return;
                        expJs = $"calc.setExpression({{id:'f',latex:'y={aa.ToString(ci)}\\\\sin(x)+{bb.ToString(ci)}\\\\cos(x)',color:'#7B1FA2',lineWidth:3}});calc.setExpression({{id:'l',latex:'y={cc.ToString(ci)}',color:'#E53935',lineWidth:2,lineStyle:'DASHED'}});calc.setMathBounds({{left:-2*System.Math.PI,right:2*System.Math.PI,bottom:-5,top:5}});";
                        break;
                    case 4:
                        double a4=P(_txtValue); if(double.IsNaN(a4)) return;
                        expJs = $"calc.setExpression({{id:'f',latex:'y=\\\\sin(x)\\\\cos(x)',color:'#7B1FA2',lineWidth:3}});calc.setExpression({{id:'l',latex:'y={a4.ToString(ci)}',color:'#E53935',lineWidth:2,lineStyle:'DASHED'}});calc.setMathBounds({{left:-2*System.Math.PI,right:2*System.Math.PI,bottom:-1,top:1}});";
                        break;
                    default:
                        double ca=P(_txtCoeffA),cb=P(_txtCoeffB),cc2=P(_txtCoeffC); if(double.IsNaN(ca)) return;
                        string fl2 = $"\\{_func}";
                        expJs = $"calc.setExpression({{id:'f',latex:'y={ca.ToString(ci)}({fl2}(x))^2+{cb.ToString(ci)}{fl2}(x)+{cc2.ToString(ci)}',color:'#7B1FA2',lineWidth:3}});calc.setExpression({{id:'l',latex:'y=0',color:'#E53935',lineWidth:2,lineStyle:'DASHED'}});calc.setMathBounds({{left:-2*System.Math.PI,right:2*System.Math.PI,bottom:-3,top:3}});";
                        break;
                }
                var win = new GraphWindow(expJs, "📈 PT Lượng Giác");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        private void BuildTheory()
        {
            if (theoryPanel == null) return; theoryPanel.Children.Clear();
            var items = new (string T, string C, string B)[] {
                ("📈 sin x = a (|a|≤1)", "x = arcsin(a) + k2π\nx = π − arcsin(a) + k2π", "#F3E5F5"),
                ("📈 cos x = a (|a|≤1)", "x = ±arccos(a) + k2π", "#E3F2FD"),
                ("📈 tan x = a", "x = arctan(a) + kπ  (x ≠ π/2+kπ)", "#FFF3E0"),
                ("📈 cot x = a", "x = arccot(a) + kπ  (x ≠ kπ)", "#E8F5E9"),
                ("🔄 sin(kx+φ) = a", "Đặt u=kx+φ → giải sin u=a\nx = (u−φ)/k", "#FCE4EC"),
                ("📊 a·sin²x+b·sinx+c=0", "Đặt t=sinx (t∈[-1;1])\nGiải PT bậc 2 theo t", "#E0F7FA"),
                ("➕ a·sinx+b·cosx=c", "R=√(a²+b²), chia 2 vế cho R\nsin(x+φ)=c/R, ĐK: |c|≤R", "#FFF8E1"),
                ("✖️ sinx·cosx=a", "sin(2x)/2=a → sin(2x)=2a\nĐK: |2a|≤1", "#F3E5F5"),
                ("🔢 Giá trị đặc biệt", "sin0=0, sinπ/6=1/2, sinπ/4=√2/2\nsinπ/3=√3/2, sinπ/2=1\ncos0=1, tanπ/4=1, tanπ/3=√3", "#E0F7FA"),
                ("🔗 Hệ thức", "sin²x+cos²x=1\nsin2x=2sinxcosx\ncos2x=cos²x−sin²x", "#FBE9E7"),
            };
            foreach (var (title, content, bgHex) in items)
            {
                var bgC = (Color)ColorConverter.ConvertFromString(bgHex);
                var card = new Border { Background = new SolidColorBrush(bgC), CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(14,10,14,10), Margin = new Thickness(0,0,0,8),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(40,0,0,0)), BorderThickness = new Thickness(1) };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(33,33,33)) });
                sp.Children.Add(new TextBlock { Text = content, FontSize = 13, Margin = new Thickness(0,6,0,0),
                    Foreground = new SolidColorBrush(Color.FromRgb(66,66,66)), FontFamily = new FontFamily("Segoe UI"),
                    TextWrapping = TextWrapping.Wrap, LineHeight = 22 });
                card.Child = sp;
                theoryPanel.Children.Add(WrapWithSectionToolbar(card, "trig_th", title.GetHashCode().ToString(), title));
            }
        }

        private static int GCD(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        private static string FormatRad(double rad)
        {
            if (System.Math.Abs(rad) < 1e-4) return "0";
            double pi = System.Math.PI;
            double ratio = rad / pi;
            for (int q = 1; q <= 12; q++)
            {
                double pDouble = ratio * q;
                int p = (int)System.Math.Round(pDouble);
                if (System.Math.Abs(pDouble - p) < 1e-3)
                {
                    int gcd = GCD(System.Math.Abs(p), q);
                    int num = p / gcd;
                    int den = q / gcd;
                    
                    string sign = num < 0 ? "−" : "";
                    int absNum = System.Math.Abs(num);
                    
                    if (den == 1)
                    {
                        if (absNum == 1) return $"{sign}π";
                        return $"{sign}{absNum}π";
                    }
                    else
                    {
                        if (absNum == 1) return $"{sign}π/{den}";
                        return $"{sign}{absNum}π/{den}";
                    }
                }
            }
            return $"{rad:F4}";
        }

        private static string FormatPeriod(double p)
        {
            string pStr = FormatRad(p);
            if (pStr == "0") return "";
            return "k" + pStr;
        }

        private static string FormatSolution(double x0, double period)
        {
            string x0Str = FormatRad(x0);
            string pStr = FormatPeriod(period);
            if (x0Str == "0") return $"x = {pStr}";
            return $"x = {x0Str} + {pStr}";
        }

        private static string FA(double rad) => FormatRad(rad);
        private static string Deg(double rad) => $"{(rad*180/System.Math.PI):F1}";
        private static string GetSA(string func, double a)
        {
            double pi = System.Math.PI; var r = new List<string>();
            foreach (double ang in new[] { 0,pi/6,pi/4,pi/3,pi/2,2*pi/3,3*pi/4,5*pi/6,pi,7*pi/6,5*pi/4,4*pi/3,3*pi/2,5*pi/3,7*pi/4,11*pi/6 })
            {
                double v = func switch { "sin"=>System.Math.Sin(ang),"cos"=>System.Math.Cos(ang),
                    "tan"=>System.Math.Abs(System.Math.Cos(ang))<1e-10?double.NaN:System.Math.Tan(ang),
                    _=>System.Math.Abs(System.Math.Sin(ang))<1e-10?double.NaN:1.0/System.Math.Tan(ang) };
                if (!double.IsNaN(v) && System.Math.Abs(v-a)<1e-4) { string t=$"x={FormatRad(ang)}≈{Deg(ang)}°"; if(!r.Contains(t)) r.Add(t); }
            }
            return string.Join("\n", r);
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mainGrid == null || sideMenu == null || contentCalc == null || contentGuide == null || viewPractical == null)
                return;

            contentCalc.Visibility = Visibility.Collapsed;
            contentGuide.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            int index = sideMenu.SelectedIndex;
            if (index == 0)
            {
                contentCalc.Visibility = Visibility.Visible;
            }
            else if (index == 1)
            {
                contentGuide.Visibility = Visibility.Visible;
            }
            else if (index == 2)
            {
                viewPractical.Visibility = Visibility.Visible;
            }
        }

        private void Tab_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedBorder && clickedBorder.Tag is string tag)
            {
                SwitchTab(tag);
            }
        }

        private void SwitchTab(string tag)
        {
            if (sideMenu == null) return;
            if (tag == "calc" || tag == "practice")
                sideMenu.SelectedIndex = 0;
            else if (tag == "guide")
                sideMenu.SelectedIndex = 1;
            else if (tag == "app" || tag == "practical")
                sideMenu.SelectedIndex = 2;
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
                        Icon = "📡",
                        Title = isVN ? "Tần Số Sóng Không Dây" : "Wireless Signal Frequencies",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_trig_1_{suffix}.png",
                        Description = isVN 
                            ? "Tối ưu hóa các tần số sóng mang trong mạng Wi-Fi, Bluetooth và 5G để tránh nhiễu và chồng chéo tín hiệu." 
                            : "Optimize frequency carrier waves in Wi-Fi, Bluetooth, and 5G networks to avoid overlap and signal interference."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔄",
                        Title = isVN ? "Dao Động Điều Hòa Vật Lý" : "Physical Harmonic Oscillation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_trig_2_{suffix}.png",
                        Description = isVN 
                            ? "Chuyển động của con lắc lò xo, pit-tông động cơ và sóng cơ học được mô tả bởi các hàm sin/cos. Việc tìm thời điểm vật qua vị trí cân bằng chính là giải phương trình lượng giác." 
                            : "Model spring-mass oscillators and piston motion in engines using trigonometric equations."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌊",
                        Title = isVN ? "Thủy Triều Đại Dương" : "Oceanic Tides Prediction",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_trig_3_{suffix}.png",
                        Description = isVN 
                            ? "Độ cao của mực nước biển lên xuống có tính chu kỳ do lực hấp dẫn của Mặt Trăng và Mặt Trời, được dự báo chính xác nhờ các mô hình sóng lượng giác." 
                            : "Model high and low tide periods over time using sinusoidal waves to schedule ship departures and arrivals safely."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎡",
                        Title = isVN ? "Vòng Quay Ferris Khổng Lồ" : "Giant Ferris Wheel",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_trig_4_{suffix}.png",
                        Description = isVN 
                            ? "Vị trí độ cao và tọa độ của cabin trên đu quay theo thời gian được mô tả bằng hàm số lượng giác tròn, giúp kiểm soát tốc độ và độ an toàn." 
                            : "Model the height and coordinate position of a passenger cabin on a giant Ferris wheel over time using sinusoidal functions."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⚡",
                        Title = isVN ? "Dòng Điện Xoay Chiều (AC)" : "Alternating Current (AC)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_trig_5_{suffix}.png",
                        Description = isVN 
                            ? "Điện áp và dòng điện xoay chiều biến thiên dạng hình sin theo thời gian. Giải phương trình lượng giác giúp tính toán pha, công suất và tần số dòng điện." 
                            : "Solve trigonometric equations to find peak voltage, frequency, and phase alignment in AC electrical circuits."
                    },
                    new PracticalAppItem
                    {
                        Icon = "☀",
                        Title = isVN ? "Chu Kỳ Khí Hậu & Thời Tiết" : "Climate & Weather Cycles",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_trig_6_{suffix}.png",
                        Description = isVN 
                            ? "Sự thay đổi nhiệt độ trung bình và số giờ nắng giữa các tháng trong năm lặp lại có chu kỳ, được mô hình hóa bằng các phương trình sóng lượng giác." 
                            : "Model periodic variations in average temperature and daylight hours throughout the seasons using trigonometric wave equations."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🫁",
                        Title = isVN ? "Nhịp thở sinh học" : "Biological Breathing Cycle",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_trig_7_{suffix}.png",
                        Description = isVN 
                            ? "Mô hình hóa lưu lượng khí hít vào và thở ra tuần hoàn của phổi bằng phương trình lượng giác hình sin theo thời gian." 
                            : "Model the periodic inhalation and exhalation airflow of human lungs using a sinusoidal trigonometric equation."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📻",
                        Title = isVN ? "Tần số sóng truyền hình" : "Broadcast Signal Frequencies",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_trig_8_{suffix}.png",
                        Description = isVN 
                            ? "Giải phương trình lượng giác để điều chế tần số (FM) và biên độ (AM) của sóng vô tuyến mang tín hiệu âm thanh và hình ảnh." 
                            : "Solve trigonometric equations to modulate the frequency (FM) and amplitude (AM) of radio broadcast signals."
                    }};

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for TrigEquationTool: {Err}", ex.Message);
            }
        }
    }
}
