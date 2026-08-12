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
    public partial class IntegralTool : BaseToolControl
    {
        private enum Mode { Power, Definite, Special }
        private Mode _mode = Mode.Power;
        private int _specialIndex;
        private string _reportContent = "";
        private readonly string[] _specialDomains = new[]
        {
            "ℝ", // sin(x)
            "ℝ", // cos(x)
            "ℝ \\ {π/2 + kπ, k ∈ ℤ}", // 1/cos²x
            "ℝ \\ {kπ, k ∈ ℤ}", // 1/sin²x
            "ℝ", // eˣ
            "ℝ \\ {0}", // 1/x
            "ℝ", // aˣ
            "[0; +∞)", // x^(1/2)
            "ℝ", // sin²x
            "ℝ"  // cos²x
        };

        public IntegralTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn chi tiết" : "Detailed Guide";
                if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Tính toán & Ví dụ" : "Calculator & Samples";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";

                if (sideMenu != null)
                {
                    sideMenu.SelectionChanged += SideMenu_SelectionChanged;
                    sideMenu.SelectedIndex = 0;
                }

                BuildModeButtons();
                BuildFormulas();
                BuildSpecialButtons();
                Calc();
                LoadPracticalApps();

                // Bàn phím số mini
                TouchNumPad.Attach(txtCoeff, step: 1);
                TouchNumPad.Attach(txtPower, step: 1);
                TouchNumPad.Attach(txtDefA, step: 1);
                TouchNumPad.Attach(txtDefB, step: 1);
                TouchNumPad.Attach(txtDefC, step: 1);
                TouchNumPad.Attach(txtDefD, step: 1);
                TouchNumPad.Attach(txtLower, step: 0.5);
                TouchNumPad.Attach(txtUpper, step: 0.5);
            };
        }

        // ═══ MODE SELECTOR ═══
        private void BuildModeButtons()
        {
            modePanel.Children.Clear();
            var modes = new (string Label, Mode M)[]
            {
                ("∫ Nguyên hàm đơn thức", Mode.Power),
                ("∫ₐᵇ Tích phân xác định", Mode.Definite),
                ("📐 Hàm đặc biệt", Mode.Special),
            };

            var accent = Color.FromRgb(0, 105, 92);
            foreach (var (label, m) in modes)
            {
                bool active = m == _mode;
                var btn = new Border
                {
                    Background = new SolidColorBrush(active ? accent : Color.FromArgb(20, accent.R, accent.G, accent.B)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(14, 8, 14, 8),
                    Margin = new Thickness(0, 0, 8, 6),
                    Cursor = Cursors.Hand,
                    BorderBrush = new SolidColorBrush(accent),
                    BorderThickness = new Thickness(active ? 0 : 1)
                };
                btn.Child = new TextBlock
                {
                    Text = label, FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(active ? Colors.White : accent),
                    FontFamily = new FontFamily("Segoe UI")
                };
                var capturedMode = m;
                btn.MouseLeftButtonDown += (_, _) =>
                {
                    _mode = capturedMode;
                    BuildModeButtons();
                    UpdateUI();
                    Calc();
                };
                modePanel.Children.Add(btn);
            }
        }

        private void UpdateUI()
        {
            powerInput.Visibility = _mode == Mode.Power ? Visibility.Visible : Visibility.Collapsed;
            definiteInput.Visibility = _mode == Mode.Definite ? Visibility.Visible : Visibility.Collapsed;
            specialInput.Visibility = _mode == Mode.Special ? Visibility.Visible : Visibility.Collapsed;

            txtModeTitle.Text = _mode switch
            {
                Mode.Power => "∫ Nguyên hàm — ∫ axⁿ dx = a/(n+1) · xⁿ⁺¹ + C",
                Mode.Definite => "∫ₐᵇ Tích phân xác định — Newton-Leibniz",
                Mode.Special => "∫ Nguyên hàm hàm đặc biệt",
                _ => ""
            };
        }

        private void Input_Changed(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) Calc();
        }

        // ═══ CALCULATE ═══
        private void Calc()
        {
            resultPanel.Children.Clear();
            switch (_mode)
            {
                case Mode.Power: CalcPower(); break;
                case Mode.Definite: CalcDefinite(); break;
                case Mode.Special: CalcSpecial(); break;
            }
        }

        // — Indefinite: ∫ axⁿ dx = a/(n+1) · xⁿ⁺¹ + C —
        private void CalcPower()
        {
            string coeffText = txtCoeff?.Text?.Trim() ?? "";
            string powerText = txtPower?.Text?.Trim() ?? "";

            if (!ParsingHelper.TryParseDouble(coeffText, out double a) ||
                !ParsingHelper.TryParseDouble(powerText, out double n))
            {
                resultPanel.Children.Clear();
                UI.ResultRow("⚠️ Vui lòng nhập hệ số và số mũ hợp lệ (số thực)!", "#D32F2F", resultPanel);
                _reportContent = "";
                return;
            }

            UI.ResultRow($"f(x) = {FormatTerm(a, n, "x")}", "#1565C0", resultPanel);

            string domain = (n == (int)n) ? (n >= 0 ? "ℝ" : "ℝ \\ {0}") : (n >= 0 ? "[0; +∞)" : "(0; +∞)");
            UI.ResultRow($"👉 Tập xác định: D = {domain}", "#E65100", resultPanel);

            string resultText = "";
            if (System.Math.Abs(n + 1) < 1e-12)
            {
                if (System.Math.Abs(a) < 1e-12)
                {
                    resultText = "C";
                    UI.ResultRow("∫ f(x) dx = C", "#1565C0", resultPanel);
                }
                else
                {
                    resultText = $"{FormatLnTerm(a)} + C";
                    UI.ResultRow($"∫ f(x) dx = {resultText}", "#1565C0", resultPanel);
                }
            }
            else
            {
                double newCoeff = a / (n + 1);
                double newPow = n + 1;
                resultText = $"{FormatTerm(newCoeff, newPow, "x")} + C";
                UI.ResultRow($"∫ f(x) dx = {resultText}", "#1565C0", resultPanel);
            }

            UI.ResultRow($"📌 Công thức: ∫ xⁿ dx = xⁿ⁺¹/(n+1) + C  (n ≠ -1)", "#E65100", resultPanel);

            _reportContent = $"### BÁO CÁO KẾT QUẢ GIẢI NGUYÊN HÀM\n" +
                             $"* **Hàm số f(x):** {FormatTerm(a, n, "x")}\n" +
                             $"* **Tập xác định:** D = {domain}\n" +
                             $"* **Kết quả nguyên hàm:** {resultText}\n" +
                             $"* **Công thức áp dụng:** ∫ xⁿ dx = xⁿ⁺¹/(n+1) + C (n ≠ -1)";
        }

        private void CalcDefinite()
        {
            string defAText = txtDefA?.Text?.Trim() ?? "";
            string defBText = txtDefB?.Text?.Trim() ?? "";
            string defCText = txtDefC?.Text?.Trim() ?? "";
            string defDText = txtDefD?.Text?.Trim() ?? "";
            string lowerText = txtLower?.Text?.Trim() ?? "";
            string upperText = txtUpper?.Text?.Trim() ?? "";

            if (!ParsingHelper.TryParseDouble(defAText, out double A) ||
                !ParsingHelper.TryParseDouble(defBText, out double B) ||
                !ParsingHelper.TryParseDouble(defCText, out double C) ||
                !ParsingHelper.TryParseDouble(defDText, out double D) ||
                !ParsingHelper.TryParseDouble(lowerText, out double lo) ||
                !ParsingHelper.TryParseDouble(upperText, out double hi))
            {
                resultPanel.Children.Clear();
                UI.ResultRow("⚠️ Vui lòng nhập đầy đủ hệ số và cận hợp lệ (số thực)!", "#D32F2F", resultPanel);
                _reportContent = "";
                return;
            }

            UI.ResultRow($"f(x) = {FormatPoly(A, B, C, D)}", "#1565C0", resultPanel);
            UI.ResultRow("👉 Tập xác định: D = ℝ", "#E65100", resultPanel);

            // F(x) = Ax⁴/4 + Bx³/3 + Cx²/2 + Dx
            double F(double x) => A * x * x * x * x / 4.0 + B * x * x * x / 3.0 + C * x * x / 2.0 + D * x;

            // Build F(x) display
            string Fx = FormatAntideriv(A, B, C, D);
            UI.ResultRow($"F(x) = {Fx}", "#1565C0", resultPanel);

            double fHi = F(hi), fLo = F(lo);
            double result = fHi - fLo;

            UI.ResultRow($"F({UI.Fmt(hi)}) = {fHi:G8}", "#1565C0", resultPanel);
            UI.ResultRow($"F({UI.Fmt(lo)}) = {fLo:G8}", "#1565C0", resultPanel);
            
            string intExpr = $"∫_{{{UI.Fmt(lo)}}}^{{{UI.Fmt(hi)}}} f(x) dx";
            UI.ResultRow($"{intExpr} = F({UI.Fmt(hi)}) − F({UI.Fmt(lo)}) = {result:G8}", "#1565C0", resultPanel);

            // Kiểm tra đổi dấu
            double start = System.Math.Min(lo, hi);
            double end = System.Math.Max(lo, hi);
            bool changesSign = false;
            
            if (System.Math.Abs(end - start) > 1e-12)
            {
                double firstVal = A * start * start * start + B * start * start + C * start + D;
                bool isPositive = firstVal >= 0;
                for (int i = 1; i <= 100; i++)
                {
                    double x = start + (end - start) * i / 100.0;
                    double val = A * x * x * x + B * x * x + C * x + D;
                    if (System.Math.Abs(val) > 1e-9)
                    {
                        if ((val > 0) != isPositive)
                        {
                            changesSign = true;
                            break;
                        }
                    }
                }
            }

            double actualArea = CalculateSimpsonArea(A, B, C, D, lo, hi);

            string areaText = "";
            if (changesSign)
            {
                areaText = $"📏 Diện tích S = ∫_{{{UI.Fmt(start)}}}^{{{UI.Fmt(end)}}} |f(x)| dx ≈ {actualArea:G8} (Hàm số đổi dấu trên [{UI.Fmt(start)}, {UI.Fmt(end)}], khác |{intExpr}| = {System.Math.Abs(result):G8})";
                UI.ResultRow(areaText, "#AD1457", resultPanel);
            }
            else
            {
                areaText = $"📏 Diện tích S = ∫_{{{UI.Fmt(start)}}}^{{{UI.Fmt(end)}}} |f(x)| dx = |{intExpr}| = {System.Math.Abs(result):G8}";
                UI.ResultRow(areaText, "#2E7D32", resultPanel);
            }

            UI.ResultRow($"📌 Newton-Leibniz: {intExpr} = F({UI.Fmt(hi)}) − F({UI.Fmt(lo)})", "#E65100", resultPanel);

            string signNotes = changesSign 
                ? $"* **Hiện tượng đổi dấu:** Hàm số đổi dấu trên đoạn [{UI.Fmt(start)}; {UI.Fmt(end)}], do đó diện tích hình phẳng S = {actualArea:G8} khác trị tuyệt đối của tích phân ({System.Math.Abs(result):G8})."
                : $"* **Hiện tượng đổi dấu:** Hàm số không đổi dấu trên đoạn [{UI.Fmt(start)}; {UI.Fmt(end)}], diện tích S bằng trị tuyệt đối tích phân.";

            _reportContent = $"### BÁO CÁO KẾT QUẢ TÍCH PHÂN XÁC ĐỊNH\n" +
                             $"* **Hàm số f(x):** {FormatPoly(A, B, C, D)}\n" +
                             $"* **Tập xác định:** D = ℝ\n" +
                             $"* **Nguyên hàm F(x):** {Fx} + C\n" +
                             $"* **Cận tích phân:** [{UI.Fmt(lo)}; {UI.Fmt(hi)}]\n" +
                             $"* **Giá trị thế cận:**\n" +
                             $"  - F({UI.Fmt(hi)}) = {fHi:G8}\n" +
                             $"  - F({UI.Fmt(lo)}) = {fLo:G8}\n" +
                             $"* **Giá trị tích phân:** {intExpr} = {result:G8}\n" +
                             $"* **Diện tích hình phẳng S:** {actualArea:G8}\n" +
                             $"{signNotes}\n" +
                             $"* **Công thức áp dụng:** Newton-Leibniz: {intExpr} = F({UI.Fmt(hi)}) - F({UI.Fmt(lo)})";
        }

        // — Special functions —
        private readonly (string Name, string Fx, string IntFx)[] _specials = new[]
        {
            ("sin(x)",  "sin(x)",      "-cos(x) + C"),
            ("cos(x)",  "cos(x)",      "sin(x) + C"),
            ("1/cos²x", "1/cos²(x)",   "tan(x) + C"),
            ("1/sin²x", "1/sin²(x)",   "-cot(x) + C"),
            ("eˣ",      "eˣ",          "eˣ + C"),
            ("1/x",     "1/x",         "ln|x| + C"),
            ("aˣ",      "aˣ (a>0,a≠1)","aˣ / ln(a) + C"),
            ("x^(1/2)", "√x = x^(1/2)","(2/3)·x^(3/2) + C"),
            ("sin²x",   "sin²(x)",     "x/2 − sin(2x)/4 + C"),
            ("cos²x",   "cos²(x)",     "x/2 + sin(2x)/4 + C"),
        };

        private void BuildSpecialButtons()
        {
            specialFuncPanel.Children.Clear();
            var accent = Color.FromRgb(0, 105, 92);
            for (int i = 0; i < _specials.Length; i++)
            {
                var (name, _, _) = _specials[i];
                bool active = i == _specialIndex;
                var btn = new Border
                {
                    Background = new SolidColorBrush(active ? accent : Color.FromArgb(20, accent.R, accent.G, accent.B)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(0, 0, 6, 6),
                    Cursor = Cursors.Hand
                };
                btn.Child = new TextBlock
                {
                    Text = name, FontSize = 13, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(active ? Colors.White : accent),
                    FontFamily = new FontFamily("Segoe UI")
                };
                int idx = i;
                btn.MouseLeftButtonDown += (_, _) =>
                {
                    _specialIndex = idx;
                    BuildSpecialButtons();
                    Calc();
                };
                specialFuncPanel.Children.Add(btn);
            }
        }

        private void CalcSpecial()
        {
            if (_specialIndex < 0 || _specialIndex >= _specials.Length) return;
            var (name, fx, intFx) = _specials[_specialIndex];
            UI.ResultRow($"f(x) = {fx}", "#1565C0", resultPanel);

            string domain = _specialDomains[_specialIndex];
            UI.ResultRow($"👉 Tập xác định: D = {domain}", "#E65100", resultPanel);

            UI.ResultRow($"∫ f(x) dx = {intFx}", "#1565C0", resultPanel);
            UI.ResultRow($"📌 Đổi biến: ∫ f(u)·u' dx = ∫ f(u) du  (đặt u = g(x))", "#E65100", resultPanel);

            _reportContent = $"### BÁO CÁO KẾT QUẢ GIẢI HÀM ĐẶC BIỆT\n" +
                             $"* **Hàm số f(x):** {fx}\n" +
                             $"* **Tập xác định:** D = {domain}\n" +
                             $"* **Nguyên hàm F(x):** {intFx}\n" +
                             $"* **Phương pháp áp dụng:** Tra cứu bảng nguyên hàm cơ bản & phép đổi biến số.";
        }

        // ═══ FORMULA REFERENCE ═══
        private void BuildFormulas()
        {
            formulaPanel.Children.Clear();
            var accent = Color.FromRgb(0, 105, 92);

            var sections = new (string Title, string[] Items)[]
            {
                ("Nguyên hàm cơ bản", new[]
                {
                    "∫ xⁿ dx = xⁿ⁺¹/(n+1) + C  (n ≠ -1)",
                    "∫ (1/x) dx = ln|x| + C",
                    "∫ eˣ dx = eˣ + C",
                    "∫ aˣ dx = aˣ/ln(a) + C  (a > 0, a ≠ 1)",
                    "∫ 0 dx = C",
                }),
                ("Lượng giác", new[]
                {
                    "∫ sin(x) dx = -cos(x) + C",
                    "∫ cos(x) dx = sin(x) + C",
                    "∫ (1/cos²x) dx = tan(x) + C",
                    "∫ (1/sin²x) dx = -cot(x) + C",
                }),
                ("Phương pháp", new[]
                {
                    "Đổi biến: ∫ f(u)·u' dx = ∫ f(u) du",
                    "Từng phần: ∫ u dv = uv − ∫ v du",
                }),
                ("Ứng dụng", new[]
                {
                    "Diện tích: S = ∫ₐᵇ |f(x)| dx",
                    "Thể tích tròn xoay: V = π ∫ₐᵇ [f(x)]² dx",
                    "Quãng đường: s = ∫ₜ₁ᵗ² |v(t)| dt",
                }),
            };

            foreach (var (title, items) in sections)
            {
                var section = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(15, accent.R, accent.G, accent.B)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = title, FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(accent), Margin = new Thickness(0, 0, 0, 4)
                });
                foreach (var item in items)
                    sp.Children.Add(new TextBlock
                    {
                        Text = $"  {item}", FontSize = 12,
                        FontFamily = new FontFamily("Segoe UI"),
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                        Margin = new Thickness(0, 1, 0, 1)
                    });
                section.Child = sp;
                formulaPanel.Children.Add(section);
            }
        }

        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var ci = CultureInfo.InvariantCulture;
                string expJs, title, info;

                if (_mode == Mode.Definite)
                {
                    if (!ParsingHelper.TryParseDouble(txtDefA?.Text?.Trim(), out double A) ||
                        !ParsingHelper.TryParseDouble(txtDefB?.Text?.Trim(), out double B) ||
                        !ParsingHelper.TryParseDouble(txtDefC?.Text?.Trim(), out double C) ||
                        !ParsingHelper.TryParseDouble(txtDefD?.Text?.Trim(), out double D) ||
                        !ParsingHelper.TryParseDouble(txtLower?.Text?.Trim(), out double lo) ||
                        !ParsingHelper.TryParseDouble(txtUpper?.Text?.Trim(), out double hi))
                    {
                        MessageBox.Show("Vui lòng kiểm tra và nhập các hệ số, cận tích phân hợp lệ (phải là số thực) trước khi xem đồ thị!", "Cảnh báo nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string aS = A.ToString(ci), bS = B.ToString(ci), cS = C.ToString(ci), dS = D.ToString(ci);
                    string loS = lo.ToString(ci), hiS = hi.ToString(ci);

                    double left = lo - 2;
                    double right = hi + 2;
                    var bounds = GetYBounds(x => A * x * x * x + B * x * x + C * x + D, left, right);

                    expJs = $@"
        calc.setExpression({{id:'fx', latex:'f(x)={aS}x^3+{bS}x^2+{cS}x+{dS}', color:'#00695C', lineWidth:3}});
        calc.setExpression({{id:'shade', latex:'\\\\min(0, f(x)) \\\\le y \\\\le \\\\max(0, f(x)) \\\\left\\{{{loS} \\\\le x \\\\le {hiS}\\\\right\\}}', color:'#80CBC4', fillOpacity:0.4}});
        calc.setExpression({{id:'lo', latex:'x={loS}', color:'#C62828', lineWidth:1.5, lineStyle:'DASHED'}});
        calc.setExpression({{id:'hi', latex:'x={hiS}', color:'#C62828', lineWidth:1.5, lineStyle:'DASHED'}});
        calc.setMathBounds({{ left: {left.ToString(ci)}, right: {right.ToString(ci)}, bottom: {bounds.bottom.ToString(ci)}, top: {bounds.top.ToString(ci)} }});";
                    
                    double F(double x) => A * x * x * x * x / 4.0 + B * x * x * x / 3.0 + C * x * x / 2.0 + D * x;
                    double val = F(hi) - F(lo);
                    double area = CalculateSimpsonArea(A, B, C, D, lo, hi);

                    title = $"∫ Tích phân xác định [{UI.Fmt(lo)}, {UI.Fmt(hi)}]";
                    info = $"f(x) = {FormatPoly(A, B, C, D)}  |  Tích phân = {val:G6}  |  Diện tích S ≈ {area:G6}";
                }
                else if (_mode == Mode.Power)
                {
                    if (!ParsingHelper.TryParseDouble(txtCoeff?.Text?.Trim(), out double a) ||
                        !ParsingHelper.TryParseDouble(txtPower?.Text?.Trim(), out double n))
                    {
                        MessageBox.Show("Vui lòng kiểm tra và nhập hệ số, số mũ hợp lệ (phải là số thực) trước khi xem đồ thị!", "Cảnh báo nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    string aS = a.ToString(ci), nS = n.ToString(ci);

                    double f(double x)
                    {
                        if (n < 0 && System.Math.Abs(x) < 1e-3) return double.NaN;
                        if (System.Math.Abs(n - (int)n) > 1e-9 && x < 0) return double.NaN;
                        return a * System.Math.Pow(x, n);
                    }
                    double F(double x)
                    {
                        if (System.Math.Abs(n + 1) < 1e-12)
                        {
                            if (System.Math.Abs(x) < 1e-3) return double.NaN;
                            return a * System.Math.Log(System.Math.Abs(x));
                        }
                        else
                        {
                            double newP = n + 1;
                            if (newP < 0 && System.Math.Abs(x) < 1e-3) return double.NaN;
                            if (System.Math.Abs(newP - (int)newP) > 1e-9 && x < 0) return double.NaN;
                            return (a / newP) * System.Math.Pow(x, newP);
                        }
                    }

                    var bounds1 = GetYBounds(f, -5, 5);
                    var bounds2 = GetYBounds(F, -5, 5);
                    double bottom = System.Math.Min(bounds1.bottom, bounds2.bottom);
                    double top = System.Math.Max(bounds1.top, bounds2.top);
                    if (bottom < -50) bottom = -50;
                    if (top > 50) top = 50;

                    if (System.Math.Abs(n + 1) < 1e-12)
                    {
                        expJs = $@"
        calc.setExpression({{id:'fx', latex:'f(x)={aS}x^{{-1}}', color:'#00695C', lineWidth:3}});
        calc.setExpression({{id:'Fx', latex:'F(x)={aS}\\\\ln\\\\left|x\\\\right|', color:'#1565C0', lineWidth:2.5, lineStyle:'DASHED'}});
        calc.setMathBounds({{ left: -5, right: 5, bottom: {bottom.ToString(ci)}, top: {top.ToString(ci)} }});";
                    }
                    else
                    {
                        double newC = a / (n + 1);
                        double newP = n + 1;
                        string ncS = newC.ToString(ci), npS = newP.ToString(ci);

                        expJs = $@"
        calc.setExpression({{id:'fx', latex:'f(x)={aS}x^{{{nS}}}', color:'#00695C', lineWidth:3}});
        calc.setExpression({{id:'Fx', latex:'{ncS}x^{{{npS}}}', color:'#1565C0', lineWidth:2.5, lineStyle:'DASHED'}});
        calc.setMathBounds({{ left: -5, right: 5, bottom: {bottom.ToString(ci)}, top: {top.ToString(ci)} }});";
                    }

                    title = "∫ f(x) và F(x) Đơn thức";
                    info = $"f(x) = {FormatTerm(a, n, "x")}  |  F(x) = " + (System.Math.Abs(n + 1) < 1e-12 ? $"{FormatLnTerm(a)} + C" : $"{FormatTerm(a / (n + 1), n + 1, "x")} + C");
                }
                else
                {
                    if (_specialIndex < 0 || _specialIndex >= _specials.Length) return;
                    var name = _specials[_specialIndex].Name;

                    string fxDesmos = "", FxDesmos = "";
                    string fxLabel = _specials[_specialIndex].Fx;
                    string FxLabel = _specials[_specialIndex].IntFx;

                    switch (_specialIndex)
                    {
                        case 0: // sin(x)
                            fxDesmos = "f(x)=\\\\sin(x)";
                            FxDesmos = "F(x)=-\\\\cos(x)";
                            break;
                        case 1: // cos(x)
                            fxDesmos = "f(x)=\\\\cos(x)";
                            FxDesmos = "F(x)=\\\\sin(x)";
                            break;
                        case 2: // 1/cos^2(x)
                            fxDesmos = "f(x)=1/(\\\\cos(x))^2";
                            FxDesmos = "F(x)=\\\\tan(x)";
                            break;
                        case 3: // 1/sin^2(x)
                            fxDesmos = "f(x)=1/(\\\\sin(x))^2";
                            FxDesmos = "F(x)=-\\\\cot(x)";
                            break;
                        case 4: // e^x
                            fxDesmos = "f(x)=e^x";
                            FxDesmos = "F(x)=e^x";
                            break;
                        case 5: // 1/x
                            fxDesmos = "f(x)=1/x";
                            FxDesmos = "F(x)=\\\\ln(|x|)";
                            break;
                        case 6: // a^x (a=2)
                            fxDesmos = "f(x)=2^x";
                            FxDesmos = "F(x)=2^x/\\\\ln(2)";
                            fxLabel = "aˣ (minh họa a=2)";
                            FxLabel = "2ˣ/ln(2) + C";
                            break;
                        case 7: // x^(1/2)
                            fxDesmos = "f(x)=\\\\sqrt{x}";
                            FxDesmos = "F(x)=(2/3)x^{1.5}";
                            break;
                        case 8: // sin^2(x)
                            fxDesmos = "f(x)=(\\\\sin(x))^2";
                            FxDesmos = "F(x)=x/2-\\\\sin(2x)/4";
                            break;
                        case 9: // cos^2(x)
                            fxDesmos = "f(x)=(\\\\cos(x))^2";
                            FxDesmos = "F(x)=x/2+\\\\sin(2x)/4";
                            break;
                    }

                    expJs = $@"
        calc.setExpression({{id:'fx', latex:'{fxDesmos}', color:'#00695C', lineWidth:3}});
        calc.setExpression({{id:'Fx', latex:'{FxDesmos}', color:'#1565C0', lineWidth:2.5, lineStyle:'DASHED'}});
        calc.setMathBounds({{ left: -5, right: 5, bottom: -5, top: 5 }});";

                    title = $"∫ Đồ thị hàm đặc biệt: {name}";
                    info = $"f(x) = {fxLabel}  |  F(x) = {FxLabel}";
                }

                var win = new GraphWindow(expJs, title, info);
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở đồ thị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ═══ HELPERS ═══
        /* AddResult / AddR replaced by UI.ResultRow */

        private static string Fmt(double v) => v == (int)v ? $"{(int)v}" : $"{v:G6}";

        public static int Gcd(int x, int y)
        {
            return y == 0 ? System.Math.Abs(x) : Gcd(y, x % y);
        }

        public static (double value, string text) FormatCoefficient(double coeff, int den)
        {
            double val = coeff / den;
            if (System.Math.Abs(val) < 1e-12) return (0, "");
            
            // Nếu kết quả là số nguyên
            if (System.Math.Abs(val - System.Math.Round(val)) < 1e-12)
            {
                double intVal = System.Math.Round(val);
                if (System.Math.Abs(intVal - 1) < 1e-12) return (1, "");
                if (System.Math.Abs(intVal + 1) < 1e-12) return (-1, "-");
                return (intVal, UI.Fmt(intVal));
            }

            // Nếu hệ số gốc là số nguyên, ta rút gọn phân số
            if (System.Math.Abs(coeff - System.Math.Round(coeff)) < 1e-12)
            {
                int num = (int)System.Math.Round(coeff);
                int g = Gcd(num, den);
                int simNum = num / g;
                int simDen = den / g;

                if (simDen < 0) { simNum = -simNum; simDen = -simDen; }

                string sign = simNum < 0 ? "-" : "";
                int absNum = System.Math.Abs(simNum);
                
                if (absNum == 1) return (val, $"{sign}(1/{simDen})");
                return (val, $"{sign}({absNum}/{simDen})");
            }

            // Nếu hệ số gốc là số thập phân, giữ nguyên định dạng thập phân
            return (val, UI.Fmt(val));
        }

        public static string FormatLnTerm(double coeff)
        {
            if (System.Math.Abs(coeff) < 1e-12) return "0";
            if (System.Math.Abs(coeff - 1) < 1e-12) return "ln|x|";
            if (System.Math.Abs(coeff + 1) < 1e-12) return "-ln|x|";
            return $"{UI.Fmt(coeff)}·ln|x|";
        }

        private (double bottom, double top) GetYBounds(Func<double, double> f, double left, double right)
        {
            double yMin = double.MaxValue;
            double yMax = double.MinValue;
            int samples = 40;
            
            for (int i = 0; i <= samples; i++)
            {
                double x = left + (right - left) * i / samples;
                try
                {
                    double y = f(x);
                    if (!double.IsNaN(y) && !double.IsInfinity(y) && y >= -100 && y <= 100)
                    {
                        if (y < yMin) yMin = y;
                        if (y > yMax) yMax = y;
                    }
                }
                catch {}
            }
            
            if (yMin == double.MaxValue || yMax == double.MinValue)
            {
                return (-5, 5);
            }
            
            double height = yMax - yMin;
            if (height < 1.0)
            {
                double center = (yMin + yMax) / 2.0;
                return (center - 4, center + 4);
            }
            
            return (yMin - height * 0.1, yMax + height * 0.1);
        }

        public static double CalculateSimpsonArea(double A, double B, double C, double D, double lo, double hi)
        {
            double start = System.Math.Min(lo, hi);
            double end = System.Math.Max(lo, hi);
            if (System.Math.Abs(end - start) < 1e-12) return 0;
            
            int n = 1000;
            double h = (end - start) / n;
            
            double F_abs(double x) => System.Math.Abs(A * x * x * x + B * x * x + C * x + D);
            
            double sum = F_abs(start) + F_abs(end);
            for (int i = 1; i < n; i++)
            {
                double x = start + i * h;
                sum += (i % 2 == 0 ? 2 : 4) * F_abs(x);
            }
            return sum * h / 3.0;
        }

        public static string FormatTerm(double coeff, double power, string variable)
        {
            if (System.Math.Abs(coeff) < 1e-12) return "0";
            if (System.Math.Abs(power) < 1e-12) return UI.Fmt(coeff);
            if (System.Math.Abs(power - 1) < 1e-12)
                return System.Math.Abs(coeff - 1) < 1e-12 ? variable
                    : System.Math.Abs(coeff + 1) < 1e-12 ? $"-{variable}"
                    : $"{UI.Fmt(coeff)}{variable}";
            string c = System.Math.Abs(coeff - 1) < 1e-12 ? ""
                : System.Math.Abs(coeff + 1) < 1e-12 ? "-"
                : UI.Fmt(coeff);
            return $"{c}{variable}^{UI.Fmt(power)}";
        }

        public static string FormatPoly(double a, double b, double c, double d)
        {
            var parts = new System.Collections.Generic.List<string>();
            void Add(double coeff, double pow)
            {
                if (System.Math.Abs(coeff) < 1e-12) return;
                string t = FormatTerm(System.Math.Abs(coeff), pow, "x");
                if (parts.Count == 0) parts.Add(coeff < 0 ? $"-{t}" : t);
                else parts.Add(coeff > 0 ? $"+ {t}" : $"- {t}");
            }
            Add(a, 3); Add(b, 2); Add(c, 1);
            if (System.Math.Abs(d) > 1e-12)
            {
                string t = UI.Fmt(System.Math.Abs(d));
                if (parts.Count == 0) parts.Add(d < 0 ? $"-{t}" : t);
                else parts.Add(d > 0 ? $"+ {t}" : $"- {t}");
            }
            return parts.Count > 0 ? string.Join(" ", parts) : "0";
        }

        public static string FormatAntideriv(double A, double B, double C, double D)
        {
            var parts = new System.Collections.Generic.List<string>();
            
            void AddTerm(double coeff, int origPow)
            {
                int newPow = origPow + 1;
                var (val, coeffText) = FormatCoefficient(coeff, newPow);
                if (System.Math.Abs(val) < 1e-12) return;

                string term;
                if (coeffText == "" || coeffText == "-")
                {
                    term = $"x^{newPow}";
                }
                else
                {
                    string absCoeffText = coeffText.StartsWith("-") ? coeffText.Substring(1) : coeffText;
                    term = $"{absCoeffText}x^{newPow}";
                }

                if (parts.Count == 0)
                {
                    parts.Add(val < 0 ? $"-{term}" : term);
                }
                else
                {
                    parts.Add(val > 0 ? $"+ {term}" : $"- {term}");
                }
            }
            
            AddTerm(A, 3);
            AddTerm(B, 2);
            AddTerm(C, 1);

            if (System.Math.Abs(D) > 1e-12)
            {
                string term = D == 1 ? "x" : D == -1 ? "x" : $"{UI.Fmt(System.Math.Abs(D))}x";
                if (parts.Count == 0)
                {
                    parts.Add(D < 0 ? $"-{term}" : term);
                }
                else
                {
                    parts.Add(D > 0 ? $"+ {term}" : $"- {term}");
                }
            }

            return parts.Count > 0 ? string.Join(" ", parts) : "0";
        }

        // ═══════════════════════════════════════════════════════════
        //  TABS & PRACTICAL APPLICATIONS / PRESETS
        // ═══════════════════════════════════════════════════════════

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootGrid == null || sideMenu == null || contentGuide == null || contentCalc == null || viewPractical == null)
                return;

            contentGuide.Visibility = Visibility.Collapsed;
            contentCalc.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

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
                viewPractical.Visibility = Visibility.Visible;
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

        private void Tab_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                if (border.Background is SolidColorBrush brush && brush.Color != Color.FromRgb(30, 136, 229))
                {
                    border.Background = new SolidColorBrush(Color.FromRgb(223, 228, 238));
                }
            }
        }

        private void Tab_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                if (border.Background is SolidColorBrush brush && brush.Color != Color.FromRgb(30, 136, 229))
                {
                    border.Background = Brushes.Transparent;
                }
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
                        Icon = "🌉",
                        Title = isVN ? "Diện tích mái vòm Parabol" : "Irregular Area Calculations",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_integral_1_{suffix}.png",
                        Description = isVN 
                            ? "Tính diện tích bề mặt vòm cầu parabol của các công trình kiến trúc để dự toán nguyên vật liệu. Công thức: S = ∫₋w/2^w/2 y dx." 
                            : "Determine exact areas of irregular land plots and structural concrete foundations by integrating boundary curves."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏺",
                        Title = isVN ? "Thể tích vật thể tròn xoay" : "Total Electric Charge",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_integral_2_{suffix}.png",
                        Description = isVN 
                            ? "Tính dung tích của các chi tiết máy, bình chứa nước xoay quanh một trục đối xứng trong cơ khí chế tạo. Công thức: V = π ∫ₐᵇ f²(x) dx." 
                            : "Calculate total electric charge accumulated on surfaces by integrating variable electric current density over time."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🚀",
                        Title = isVN ? "Quãng đường chuyển động" : "Rocket Propulsion & Altitude",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_integral_3_{suffix}.png",
                        Description = isVN 
                            ? "Tính quãng đường di chuyển của phương tiện (tên lửa, ô tô) khi biết hàm vận tốc biến thiên theo thời gian. Công thức: s = ∫ₜ₁^ₜ₂ v(t) dt." 
                            : "Integrate thrust force over time to calculate total impulse, rocket velocity, and peak altitude reached."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔩",
                        Title = isVN ? "Công cơ học (Lực nén lò xo)" : "Financial Present Value",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_integral_4_{suffix}.png",
                        Description = isVN 
                            ? "Tính công cần thiết sinh ra khi lực kéo/nén của một lò xo thay đổi theo li độ biến dạng. Công thức vật lý: W = ∫ₐᵇ F(x) dx = ∫ₐᵇ kx dx." 
                            : "Calculate the accumulated value of continuous cash flows over time using definite integrals with discount rates."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔌",
                        Title = isVN ? "Điện lượng xoay chiều" : "Center of Mass in Engineering",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_integral_5_{suffix}.png",
                        Description = isVN 
                            ? "Tính lượng điện tích đi qua tiết diện dây dẫn trong một khoảng thời gian của dòng điện xoay chiều. Công thức: q = ∫ₜ₁^ₜ₂ i(t) dt." 
                            : "Locate centers of gravity for ship hulls, aircraft wings, and structures using double and triple integral coordinates."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📈",
                        Title = isVN ? "Thặng dư sản xuất & tiêu dùng" : "Probability & Statistics",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_integral_6_{suffix}.png",
                        Description = isVN 
                            ? "Đo lường lợi ích kinh tế của người mua và người bán trên thị trường dựa trên đường cung và cầu. Công thức: CS = ∫₀^Q* (P_d(Q) - P*) dQ." 
                            : "Compute probabilities under continuous probability density functions by integrating the curve between interval bounds."
                    }
                ,
                    new PracticalAppItem
                    {
                        Icon = "🏺",
                        Title = isVN ? "Tính thể tích bình chứa tròn xoay" : "Volume of Solids of Revolution",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_integral_7_{suffix}.png",
                        Description = isVN 
                            ? "Áp dụng tích phân để tính chính xác thể tích của các vật thể tròn xoay như bình gốm, lu nước, chi tiết máy." 
                            : "Apply integration to calculate the exact volume of solids of revolution like pottery, tanks, and machine parts."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⚙️",
                        Title = isVN ? "Công cơ học của lực biến đổi" : "Mechanical Work of Variable Force",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_integral_8_{suffix}.png",
                        Description = isVN 
                            ? "Tích phân lực tác dụng theo quãng đường để tính toán công sinh ra bởi một lực thay đổi liên tục trong động cơ." 
                            : "Integrate variable force over distance to calculate the total work done by continuous changing forces in engines."
                    }};

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for IntegralTool: {Err}", ex.Message);
            }
        }

        private void Preset_Sample1_Click(object sender, MouseButtonEventArgs e)
        {
            _mode = Mode.Power;
            BuildModeButtons();
            UpdateUI();

            if (txtCoeff != null) txtCoeff.Text = "3";
            if (txtPower != null) txtPower.Text = "2";

            SwitchToTab("calc");
            Calc();
        }

        private void Preset_Sample2_Click(object sender, MouseButtonEventArgs e)
        {
            _mode = Mode.Definite;
            BuildModeButtons();
            UpdateUI();

            if (txtDefA != null) txtDefA.Text = "1";
            if (txtDefB != null) txtDefB.Text = "-3";
            if (txtDefC != null) txtDefC.Text = "2";
            if (txtDefD != null) txtDefD.Text = "0";
            if (txtLower != null) txtLower.Text = "0";
            if (txtUpper != null) txtUpper.Text = "2";

            SwitchToTab("calc");
            Calc();
        }

        private void Preset_Sample3_Click(object sender, MouseButtonEventArgs e)
        {
            _mode = Mode.Special;
            _specialIndex = 5; // 1/x
            BuildModeButtons();
            BuildSpecialButtons();
            UpdateUI();

            SwitchToTab("calc");
            Calc();
        }

        private void BtnCopyReport_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_reportContent))
                {
                    MessageBox.Show("Chưa có kết quả tính toán để sao chép báo cáo.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Clipboard.SetText(_reportContent);
                MessageBox.Show("Đã sao chép báo cáo kết quả chi tiết vào bộ nhớ tạm (Clipboard) dưới dạng Markdown!", "Sao chép thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi sao chép: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
