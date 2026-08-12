using System;
using QASmartClass.LearningTools.Helpers;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;
using System.Collections.Generic;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class LimitTool : BaseToolControl
    {
        private Border tabCalc;
        private TextBlock tabCalcText;
        private Border tabGuide;
        private TextBlock tabGuideText;
        private Border tabApp;
        private TextBlock tabAppText;

        private enum Mode { Polynomial, Sequence, Special }
        private Mode _mode = Mode.Polynomial;
        private bool _isInfinity;
        private int _seqType;  // 0 = rational, 1 = geometric, 2 = sqrt
        private int _specialIndex;

        private static readonly Color Accent = Color.FromRgb(230, 81, 0);

        public LimitTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Tính toán & Lý thuyết" : "Calculation & Theory";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";

                if (sideMenu != null) 
                {
                    sideMenu.SelectionChanged += SideMenu_SelectionChanged;
                    sideMenu.SelectedIndex = 0;
                }

                BuildModeButtons();
                BuildSeqTypes();
                BuildSpecials();
                BuildFormulas();
                LoadPracticalApps();
                Calc();

                TouchNumPad.Attach(txtNA, step: 1);
                TouchNumPad.Attach(txtNB, step: 1);
                TouchNumPad.Attach(txtNC, step: 1);
                TouchNumPad.Attach(txtDA, step: 1);
                TouchNumPad.Attach(txtDB, step: 1);
                TouchNumPad.Attach(txtDC, step: 1);
                TouchNumPad.Attach(txtX0, step: 0.5);

                btnInfinity.MouseEnter += (_, _) =>
                {
                    if (!_isInfinity)
                    {
                        btnInfinity.Background = new SolidColorBrush(Color.FromArgb(40, 21, 101, 192));
                    }
                };
                btnInfinity.MouseLeave += (_, _) =>
                {
                    if (!_isInfinity)
                    {
                        btnInfinity.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                    }
                };
            };
        }

        // ═══ MODE SELECTOR ═══
        private void BuildModeButtons()
        {
            modePanel.Children.Clear();
            var modes = new (string Label, Mode M)[]
            {
                ("📊 Giới hạn hàm phân thức", Mode.Polynomial),
                ("🔢 Giới hạn dãy số", Mode.Sequence),
                ("📌 Giới hạn đặc biệt", Mode.Special),
            };
            foreach (var (label, m) in modes)
            {
                bool active = m == _mode;
                var btn = MakeChip(label, active);
                var cm = m;
                btn.MouseLeftButtonDown += (_, _) => { _mode = cm; BuildModeButtons(); UpdateUI(); Calc(); };
                modePanel.Children.Add(btn);
            }
        }

        private void UpdateUI()
        {
            polyInput.Visibility = _mode == Mode.Polynomial ? Visibility.Visible : Visibility.Collapsed;
            seqInput.Visibility = _mode == Mode.Sequence ? Visibility.Visible : Visibility.Collapsed;
            specialInput.Visibility = _mode == Mode.Special ? Visibility.Visible : Visibility.Collapsed;
            txtModeTitle.Text = _mode switch
            {
                Mode.Polynomial => "∞ Giới hạn hàm phân thức — lim f(x)/g(x)",
                Mode.Sequence => "🔢 Giới hạn dãy số — lim aₙ khi n → ∞",
                Mode.Special => "📌 Giới hạn đặc biệt cần nhớ",
                _ => ""
            };
        }

        private void Input_Changed(object sender, TextChangedEventArgs e) { if (IsLoaded) Calc(); }
        private void Infinity_Click(object sender, MouseButtonEventArgs e)
        {
            _isInfinity = !_isInfinity;
            txtX0.IsEnabled = !_isInfinity;
            txtX0.Text = _isInfinity ? "∞" : "1";
            if (_isInfinity)
            {
                btnInfinity.Background = new SolidColorBrush(Color.FromRgb(21, 101, 192)); // BrandPrimary
                txtInfinity.Foreground = Brushes.White;
            }
            else
            {
                btnInfinity.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253)); // CatMathBg
                txtInfinity.Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)); // BrandPrimary
            }
            if (IsLoaded) Calc();
        }

        // ═══ CALCULATE ═══
        private void Calc()
        {
            resultPanel.Children.Clear();
            switch (_mode)
            {
                case Mode.Polynomial: CalcPoly(); break;
                case Mode.Sequence: CalcSeq(); break;
                case Mode.Special: CalcSpecial(); break;
            }
        }

        // — Polynomial rational limit —
        private void CalcPoly()
        {
            if (!ParsingHelper.TryParseDouble(txtNA?.Text, out double a)) return;
            if (!ParsingHelper.TryParseDouble(txtNB?.Text, out double b)) return;
            if (!ParsingHelper.TryParseDouble(txtNC?.Text, out double c)) return;
            if (!ParsingHelper.TryParseDouble(txtDA?.Text, out double d)) return;
            if (!ParsingHelper.TryParseDouble(txtDB?.Text, out double e)) return;
            if (!ParsingHelper.TryParseDouble(txtDC?.Text, out double f)) return;

            // Kiểm tra mẫu số identically zero
            if (System.Math.Abs(d) < 1e-12 && System.Math.Abs(e) < 1e-12 && System.Math.Abs(f) < 1e-12)
            {
                UI.ResultRow("⚠️ Lỗi: Mẫu số g(x) = 0 với mọi x. Không thể xác định giới hạn.", "#C62828", resultPanel);
                return;
            }

            string num = FormatPoly2(a, b, c);
            string den = FormatPoly2(d, e, f);
            UI.ResultRow($"f(x) = {num}", "#1565C0", resultPanel);
            UI.ResultRow($"g(x) = {den}", "#1565C0", resultPanel);

            if (_isInfinity)
            {
                // lim x→∞: compare leading coefficients
                int degN = System.Math.Abs(a) > 1e-12 ? 2 : System.Math.Abs(b) > 1e-12 ? 1 : 0;
                int degD = System.Math.Abs(d) > 1e-12 ? 2 : System.Math.Abs(e) > 1e-12 ? 1 : 0;
                double leadN = degN == 2 ? a : degN == 1 ? b : c;
                double leadD = degD == 2 ? d : degD == 1 ? e : f;

                if (degN > degD)
                {
                    int diff = degN - degD;
                    string signInf = (leadN > 0 == leadD > 0) ? "+∞" : "-∞";
                    string signNegInf = ((leadN > 0 == leadD > 0) ^ (diff % 2 != 0)) ? "+∞" : "-∞";

                    UI.ResultRow($"lim(x→+∞) = {signInf}", "#1565C0", resultPanel);
                    UI.ResultRow($"lim(x→-∞) = {signNegInf}", "#1565C0", resultPanel);
                    UI.ResultRow($"📌 Nhận xét: Bậc tử ({degN}) > bậc mẫu ({degD}) → Giới hạn tiến về vô cực.", "#E65100", resultPanel);
                }
                else if (degN == degD)
                {
                    double ratio = leadN / leadD;
                    UI.ResultRow($"lim(x→+∞) = {ratio:G8}", "#1565C0", resultPanel);
                    UI.ResultRow($"lim(x→-∞) = {ratio:G8}", "#1565C0", resultPanel);
                    UI.ResultRow($"📌 Nhận xét: Bậc tử = bậc mẫu → Giới hạn bằng tỷ lệ hệ số dẫn đầu = {UI.Fmt(leadN)}/{UI.Fmt(leadD)}", "#E65100", resultPanel);
                }
                else
                {
                    UI.ResultRow($"lim(x→+∞) = 0", "#1565C0", resultPanel);
                    UI.ResultRow($"lim(x→-∞) = 0", "#1565C0", resultPanel);
                    UI.ResultRow($"📌 Nhận xét: Bậc tử ({degN}) < bậc mẫu ({degD}) → Giới hạn bằng 0.", "#E65100", resultPanel);
                }
            }
            else
            {
                if (!ParsingHelper.TryParseDouble(txtX0?.Text, out double x0)) return;
                double numVal = a * x0 * x0 + b * x0 + c;
                double denVal = d * x0 * x0 + e * x0 + f;

                UI.ResultRow($"f({UI.Fmt(x0)}) = {numVal:G8}", "#1565C0", resultPanel);
                UI.ResultRow($"g({UI.Fmt(x0)}) = {denVal:G8}", "#1565C0", resultPanel);

                if (System.Math.Abs(denVal) > 1e-12)
                {
                    double result = numVal / denVal;
                    UI.ResultRow($"lim(x→{UI.Fmt(x0)}) = {result:G8}", "#1565C0", resultPanel);
                }
                else if (System.Math.Abs(numVal) > 1e-12)
                {
                    double eps = 1e-6;
                    double denLeft = d * (x0 - eps) * (x0 - eps) + e * (x0 - eps) + f;
                    double denRight = d * (x0 + eps) * (x0 + eps) + e * (x0 + eps) + f;

                    string signLeft = (numVal * denLeft > 0) ? "+∞" : "-∞";
                    string signRight = (numVal * denRight > 0) ? "+∞" : "-∞";

                    UI.ResultRow($"lim(x→{UI.Fmt(x0)}⁻) = {signLeft}", "#1565C0", resultPanel);
                    UI.ResultRow($"lim(x→{UI.Fmt(x0)}⁺) = {signRight}", "#1565C0", resultPanel);

                    if (signLeft == signRight)
                    {
                        UI.ResultRow($"lim(x→{UI.Fmt(x0)}) = {signLeft} (Giới hạn 2 phía bằng nhau)", "#2E7D32", resultPanel);
                    }
                    else
                    {
                        UI.ResultRow($"lim(x→{UI.Fmt(x0)}) = Không tồn tại (Giới hạn trái ≠ Giới hạn phải)", "#C62828", resultPanel);
                    }
                    UI.ResultRow($"📌 Dạng vô cực c/0: Tử f({UI.Fmt(x0)}) = {UI.Fmt(numVal)} ≠ 0, Mẫu g({UI.Fmt(x0)}) = 0", "#E65100", resultPanel);
                }
                else
                {
                    UI.ResultRow($"📌 Dạng vô định: 0/0 — phân tích nhân tử hoặc L'Hôpital", "#E65100", resultPanel);
                    // Try L'Hôpital: f'(x0)/g'(x0)
                    double numD = 2 * a * x0 + b;
                    double denD = 2 * d * x0 + e;
                    if (System.Math.Abs(denD) > 1e-12)
                    {
                        double lhResult = numD / denD;
                        UI.ResultRow($"L'Hôpital lần 1: f'({UI.Fmt(x0)})/g'({UI.Fmt(x0)}) = {UI.Fmt(numD)}/{UI.Fmt(denD)} = {lhResult:G8}", "#1565C0", resultPanel);
                    }
                    else
                    {
                        // denD == 0 và numD == 0 -> L'Hôpital lần 2: f''(x0)/g''(x0) = 2a/2d = a/d
                        if (System.Math.Abs(numD) <= 1e-12)
                        {
                            if (System.Math.Abs(d) > 1e-12)
                            {
                                double lh2Result = a / d;
                                UI.ResultRow($"L'Hôpital lần 2: f''({UI.Fmt(x0)})/g''({UI.Fmt(x0)}) = {UI.Fmt(2 * a)}/{UI.Fmt(2 * d)} = {lh2Result:G8}", "#1565C0", resultPanel);
                            }
                            else
                            {
                                UI.ResultRow("L'Hôpital lần 2: Không thể chia cho 0 (hệ số d = 0)", "#C62828", resultPanel);
                            }
                        }
                        else
                        {
                            UI.ResultRow($"lim(x→{UI.Fmt(x0)}) = ±∞ (Đạo hàm f' ≠ 0, g' = 0)", "#1565C0", resultPanel);
                        }
                    }
                }
            }
        }

        // — Sequence limits —
        private readonly (string Name, string Formula, string Limit, string Note)[] _seqDefs = new[]
        {
            ("1/n",     "aₙ = 1/n",         "0",    "n → ∞, 1/n → 0"),
            ("1/n²",    "aₙ = 1/n²",        "0",    "Giảm nhanh hơn 1/n"),
            ("(n+1)/n", "aₙ = (n+1)/n = 1+1/n", "1", "Viết lại: 1 + 1/n → 1"),
            ("n/(n+1)", "aₙ = n/(n+1)",      "1",    "= 1 − 1/(n+1) → 1"),
            ("qⁿ |q|<1","aₙ = qⁿ, |q| < 1", "0",    "Cấp số nhân lùi dần"),
            ("(1+1/n)ⁿ","aₙ = (1+1/n)ⁿ",     "e ≈ 2.71828", "Định nghĩa số e"),
            ("n·sin(1/n)", "aₙ = n·sin(1/n)", "1",   "= sin(1/n)/(1/n) → 1"),
            ("√(n+1)−√n", "aₙ = √(n+1)−√n",  "0",   "Nhân liên hợp: 1/(√(n+1)+√n) → 0"),
        };

        private void BuildSeqTypes()
        {
            seqTypePanel.Children.Clear();
            for (int i = 0; i < _seqDefs.Length; i++)
            {
                bool active = i == _seqType;
                var btn = MakeChip(_seqDefs[i].Name, active);
                int idx = i;
                btn.MouseLeftButtonDown += (_, _) => { _seqType = idx; BuildSeqTypes(); Calc(); };
                seqTypePanel.Children.Add(btn);
            }
        }

        private void CalcSeq()
        {
            if (_seqType < 0 || _seqType >= _seqDefs.Length) return;
            var (name, formula, limit, note) = _seqDefs[_seqType];
            UI.ResultRow($"Dãy số = {formula}", "#1565C0", resultPanel);
            UI.ResultRow($"lim(n→∞) = {limit}", "#1565C0", resultPanel);
            UI.ResultRow($"📌 Giải thích: {note}", "#E65100", resultPanel);
        }

        // — Special limits —
        private readonly (string Name, string Expression, string Result, string Note)[] _specials = new[]
        {
            ("sin(x)/x",     "lim(x→0) sin(x)/x",              "= 1",        "Giới hạn lượng giác cơ bản"),
            ("(1-cos x)/x²", "lim(x→0) (1−cos x)/x²",          "= 1/2",      "Công thức lượng giác: 2·sin²(x/2)/x² → 1/2"),
            ("tan(x)/x",     "lim(x→0) tan(x)/x",              "= 1",        "= sin(x)/x · 1/cos(x) → 1"),
            ("(eˣ-1)/x",     "lim(x→0) (eˣ−1)/x",              "= 1",        "Đạo hàm của f(x) = eˣ tại x = 0"),
            ("ln(1+x)/x",    "lim(x→0) ln(1+x)/x",             "= 1",        "Đạo hàm của f(x) = ln(x) tại x = 1"),
            ("(1+x)^(1/x)",  "lim(x→0) (1+x)^(1/x)",           "= e",        "Định nghĩa số e"),
            ("(1+1/n)ⁿ",     "lim(n→∞) (1+1/n)ⁿ",              "= e ≈ 2.718","Dạng dãy số"),
            ("xⁿ/n!",        "lim(n→∞) xⁿ/n!",                 "= 0",        "Giai thừa tăng nhanh hơn lũy thừa"),
            ("(aˣ-1)/x",     "lim(x→0) (aˣ−1)/x",              "= ln(a)",    "Đạo hàm của f(x) = aˣ tại x = 0 (a > 0, a ≠ 1)"),
            ("sin(ax)/sin(bx)", "lim(x→0) sin(ax)/sin(bx)",     "= a/b",      "b ≠ 0"),
        };

        private void BuildSpecials()
        {
            specialPanel.Children.Clear();
            for (int i = 0; i < _specials.Length; i++)
            {
                bool active = i == _specialIndex;
                var btn = MakeChip(_specials[i].Name, active);
                int idx = i;
                btn.MouseLeftButtonDown += (_, _) => { _specialIndex = idx; BuildSpecials(); Calc(); };
                specialPanel.Children.Add(btn);
            }
        }

        private void CalcSpecial()
        {
            if (_specialIndex < 0 || _specialIndex >= _specials.Length) return;
            var (_, expr, result, note) = _specials[_specialIndex];
            UI.ResultRow($"Biểu thức = {expr}", "#1565C0", resultPanel);
            UI.ResultRow($"Kết quả = {result}", "#1565C0", resultPanel);
            UI.ResultRow($"📌 Ghi nhớ: {note}", "#E65100", resultPanel);
        }

        // ═══ FORMULAS ═══
        private void BuildFormulas()
        {
            formulaPanel.Children.Clear();
            var sections = new (string Title, string[] Items)[]
            {
                ("Định nghĩa", new[]
                {
                    "lim(x→a) f(x) = L  ⟺  ∀ε>0, ∃δ>0: 0 < |x−a| < δ ⟹ |f(x)−L| < ε",
                    "lim(x→∞) f(x) = L  ⟺  x đủ lớn thì f(x) tiến về L",
                }),
                ("Tính chất", new[]
                {
                    "lim [f ± g] = lim f ± lim g",
                    "lim [f · g] = lim f · lim g",
                    "lim [f / g] = lim f / lim g  (lim g ≠ 0)",
                    "lim [k · f] = k · lim f",
                }),
                ("Dạng vô định", new[]
                {
                    "0/0  →  Phân tích nhân tử, nhân liên hợp",
                    "∞/∞  →  Chia cho lũy thừa cao nhất",
                    "∞ − ∞  →  Quy đồng hoặc nhân liên hợp",
                    "0 · ∞  →  Viết lại thành 0/0 hoặc ∞/∞",
                }),
                ("Quy tắc L'Hôpital", new[]
                {
                    "Nếu lim f/g = 0/0 hoặc ∞/∞:",
                    "  lim f(x)/g(x) = lim f'(x)/g'(x)",
                }),
            };

            foreach (var (title, items) in sections)
            {
                var section = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(15, Accent.R, Accent.G, Accent.B)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = title, FontSize = 12, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Accent), Margin = new Thickness(0, 0, 0, 4)
                });
                foreach (var item in items)
                    sp.Children.Add(new TextBlock
                    {
                        Text = $"  {item}", FontSize = 14,
                        FontFamily = new FontFamily("Segoe UI"),
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                        Margin = new Thickness(0, 1, 0, 1)
                    });
                section.Child = sp;
                formulaPanel.Children.Add(section);
            }
        }

        // ═══ Graph ═══
        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var ci = CultureInfo.InvariantCulture;
                string expJs = "";
                string title = "Đồ thị Giới Hạn";

                if (_mode == Mode.Polynomial)
                {
                    ParsingHelper.TryParseDouble(txtNA?.Text, out double a);
                    ParsingHelper.TryParseDouble(txtNB?.Text, out double b);
                    ParsingHelper.TryParseDouble(txtNC?.Text, out double c);
                    ParsingHelper.TryParseDouble(txtDA?.Text, out double d);
                    ParsingHelper.TryParseDouble(txtDB?.Text, out double ee);
                    ParsingHelper.TryParseDouble(txtDC?.Text, out double f);

                    if (System.Math.Abs(d) < 1e-12 && System.Math.Abs(ee) < 1e-12 && System.Math.Abs(f) < 1e-12)
                    {
                        MessageBox.Show("Mẫu số g(x) = 0 với mọi x. Không thể vẽ đồ thị.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string numLatex = FormatPoly2Latex(a, b, c);
                    string denLatex = FormatPoly2Latex(d, ee, f);
                    title = "∞ Đồ thị giới hạn hàm phân thức";

                    expJs = $@"
        calc.setExpression({{id:'fx', latex:'f(x)=\\frac{{{numLatex}}}{{{denLatex}}}', color:'#1565C0', lineWidth:3}});
        calc.setExpression({{id:'num', latex:'g(x)={numLatex}', color:'#00897B', lineWidth:1.5, lineStyle:'DASHED'}});
        calc.setExpression({{id:'den', latex:'h(x)={denLatex}', color:'#2E7D32', lineWidth:1.5, lineStyle:'DASHED'}});";

                    if (!_isInfinity && ParsingHelper.TryParseDouble(txtX0?.Text, out double x0))
                    {
                        string x0S = x0.ToString(ci);
                        double numVal = a * x0 * x0 + b * x0 + c;
                        double denVal = d * x0 * x0 + ee * x0 + f;

                        if (System.Math.Abs(denVal) > 1e-12)
                        {
                            double lim = numVal / denVal;
                            expJs += $@"
        calc.setExpression({{id:'vline', latex:'x={x0S}', color:'#C62828', lineWidth:1.5, lineStyle:'DASHED'}});
        calc.setExpression({{id:'pt', latex:'({x0S},{lim.ToString(ci)})', color:'#1565C0', pointStyle:'POINT', pointSize:12, label:'x₀={x0S}', showLabel:true}});";
                        }
                        else
                        {
                            if (System.Math.Abs(numVal) <= 1e-12)
                            {
                                double numD = 2 * a * x0 + b;
                                double denD = 2 * d * x0 + ee;
                                double? lim = null;
                                if (System.Math.Abs(denD) > 1e-12) lim = numD / denD;
                                else if (System.Math.Abs(d) > 1e-12) lim = a / d;

                                if (lim.HasValue && !double.IsInfinity(lim.Value) && !double.IsNaN(lim.Value))
                                {
                                    string limS = lim.Value.ToString(ci);
                                    expJs += $@"
        calc.setExpression({{id:'vline', latex:'x={x0S}', color:'#C62828', lineWidth:1.5, lineStyle:'DASHED'}});
        calc.setExpression({{id:'pt', latex:'({x0S},{limS})', color:'#1565C0', pointStyle:'OPEN', pointSize:12, label:'Điểm khuyết x₀={x0S}', showLabel:true}});";
                                }
                                else
                                {
                                    expJs += $@"
        calc.setExpression({{id:'vline', latex:'x={x0S}', color:'#C62828', lineWidth:1.5, lineStyle:'DASHED', label:'Tiệm cận x={x0S}', showLabel:true}});";
                                }
                            }
                            else
                            {
                                expJs += $@"
        calc.setExpression({{id:'vline', latex:'x={x0S}', color:'#C62828', lineWidth:1.5, lineStyle:'DASHED', label:'Tiệm cận x={x0S}', showLabel:true}});";
                            }
                        }
                    }
                    expJs += "calc.setMathBounds({ left: -10, right: 10, bottom: -10, top: 10 });";
                }
                else if (_mode == Mode.Sequence)
                {
                    title = $"🔢 Đồ thị dãy số: {_seqDefs[_seqType].Name}";
                    string pointsList = "";
                    switch (_seqType)
                    {
                        case 0: // 1/n
                            pointsList = "[(n_1, 1/n_1) for n_1=n_{list}]";
                            expJs = $@"
        calc.setExpression({{id:'n_list', latex:'n_{{list}}=[1...20]'}});
        calc.setExpression({{id:'seq', latex:'{pointsList}', color:'#1565C0', pointSize:10}});
        calc.setExpression({{id:'asym', latex:'y=0', color:'#2E7D32', lineStyle:'DASHED', label:'Giới hạn L=0', showLabel:true}});";
                            break;
                        case 1: // 1/n^2
                            pointsList = "[(n_1, 1/n_1^2) for n_1=n_{list}]";
                            expJs = $@"
        calc.setExpression({{id:'n_list', latex:'n_{{list}}=[1...20]'}});
        calc.setExpression({{id:'seq', latex:'{pointsList}', color:'#1565C0', pointSize:10}});
        calc.setExpression({{id:'asym', latex:'y=0', color:'#2E7D32', lineStyle:'DASHED', label:'Giới hạn L=0', showLabel:true}});";
                            break;
                        case 2: // (n+1)/n
                            pointsList = "[(n_1, (n_1+1)/n_1) for n_1=n_{list}]";
                            expJs = $@"
        calc.setExpression({{id:'n_list', latex:'n_{{list}}=[1...20]'}});
        calc.setExpression({{id:'seq', latex:'{pointsList}', color:'#1565C0', pointSize:10}});
        calc.setExpression({{id:'asym', latex:'y=1', color:'#2E7D32', lineStyle:'DASHED', label:'Giới hạn L=1', showLabel:true}});";
                            break;
                        case 3: // n/(n+1)
                            pointsList = "[(n_1, n_1/(n_1+1)) for n_1=n_{list}]";
                            expJs = $@"
        calc.setExpression({{id:'n_list', latex:'n_{{list}}=[1...20]'}});
        calc.setExpression({{id:'seq', latex:'{pointsList}', color:'#1565C0', pointSize:10}});
        calc.setExpression({{id:'asym', latex:'y=1', color:'#2E7D32', lineStyle:'DASHED', label:'Giới hạn L=1', showLabel:true}});";
                            break;
                        case 4: // q^n với q=0.5
                            pointsList = "[(n_1, 0.5^n_1) for n_1=n_{list}]";
                            expJs = $@"
        calc.setExpression({{id:'n_list', latex:'n_{{list}}=[1...20]'}});
        calc.setExpression({{id:'seq', latex:'{pointsList}', color:'#1565C0', pointSize:10}});
        calc.setExpression({{id:'asym', latex:'y=0', color:'#2E7D32', lineStyle:'DASHED', label:'Giới hạn L=0', showLabel:true}});";
                            break;
                        case 5: // (1+1/n)^n
                            pointsList = "[(n_1, (1+1/n_1)^n_1) for n_1=n_{list}]";
                            expJs = $@"
        calc.setExpression({{id:'n_list', latex:'n_{{list}}=[1...20]'}});
        calc.setExpression({{id:'seq', latex:'{pointsList}', color:'#1565C0', pointSize:10}});
        calc.setExpression({{id:'asym', latex:'y=2.71828', color:'#E53935', lineStyle:'DASHED', label:'Giới hạn L=e', showLabel:true}});";
                            break;
                        case 6: // n*sin(1/n)
                            pointsList = "[(n_1, n_1*\\sin(1/n_1)) for n_1=n_{list}]";
                            expJs = $@"
        calc.setExpression({{id:'n_list', latex:'n_{{list}}=[1...20]'}});
        calc.setExpression({{id:'seq', latex:'{pointsList}', color:'#1565C0', pointSize:10}});
        calc.setExpression({{id:'asym', latex:'y=1', color:'#2E7D32', lineStyle:'DASHED', label:'Giới hạn L=1', showLabel:true}});";
                            break;
                        case 7: // sqrt(n+1)-sqrt(n)
                            pointsList = "[(n_1, \\sqrt{n_1+1}-\\sqrt{n_1}) for n_1=n_{list}]";
                            expJs = $@"
        calc.setExpression({{id:'n_list', latex:'n_{{list}}=[1...20]'}});
        calc.setExpression({{id:'seq', latex:'{pointsList}', color:'#1565C0', pointSize:10}});
        calc.setExpression({{id:'asym', latex:'y=0', color:'#2E7D32', lineStyle:'DASHED', label:'Giới hạn L=0', showLabel:true}});";
                            break;
                    }
                    expJs += "calc.setMathBounds({ left: -2, right: 22, bottom: -1, top: 4 });";
                }
                else if (_mode == Mode.Special)
                {
                    title = $"📌 Đồ thị Giới hạn đặc biệt: {_specials[_specialIndex].Name}";
                    switch (_specialIndex)
                    {
                        case 0: // sin(x)/x
                            expJs = @"
          calc.setExpression({id:'fx', latex:'f(x)=\\frac{\\sin(x)}{x}', color:'#1565C0', lineWidth:3});
          calc.setExpression({id:'pt', latex:'(0,1)', color:'#1565C0', pointStyle:'OPEN', pointSize:12, label:'Điểm khuyết (0,1)', showLabel:true});";
                            break;
                        case 1: // (1-cos x)/x^2
                            expJs = @"
          calc.setExpression({id:'fx', latex:'f(x)=\\frac{1-\\cos(x)}{x^2}', color:'#1565C0', lineWidth:3});
          calc.setExpression({id:'pt', latex:'(0,0.5)', color:'#1565C0', pointStyle:'OPEN', pointSize:12, label:'Điểm khuyết (0,0.5)', showLabel:true});";
                            break;
                        case 2: // tan(x)/x
                            expJs = @"
          calc.setExpression({id:'fx', latex:'f(x)=\\frac{\\tan(x)}{x}', color:'#1565C0', lineWidth:3});
          calc.setExpression({id:'pt', latex:'(0,1)', color:'#1565C0', pointStyle:'OPEN', pointSize:12, label:'Điểm khuyết (0,1)', showLabel:true});";
                            break;
                        case 3: // (e^x-1)/x
                            expJs = @"
          calc.setExpression({id:'fx', latex:'f(x)=\\frac{e^x-1}{x}', color:'#1565C0', lineWidth:3});
          calc.setExpression({id:'pt', latex:'(0,1)', color:'#1565C0', pointStyle:'OPEN', pointSize:12, label:'Điểm khuyết (0,1)', showLabel:true});";
                            break;
                        case 4: // ln(1+x)/x
                            expJs = @"
          calc.setExpression({id:'fx', latex:'f(x)=\\frac{\\ln(1+x)}{x}', color:'#1565C0', lineWidth:3});
          calc.setExpression({id:'pt', latex:'(0,1)', color:'#1565C0', pointStyle:'OPEN', pointSize:12, label:'Điểm khuyết (0,1)', showLabel:true});";
                            break;
                        case 5: // (1+x)^(1/x)
                            expJs = @"
          calc.setExpression({id:'fx', latex:'f(x)=(1+x)^{\\frac{1}{x}}', color:'#1565C0', lineWidth:3});
          calc.setExpression({id:'pt', latex:'(0,2.71828)', color:'#1565C0', pointStyle:'OPEN', pointSize:12, label:'Điểm khuyết (0,e)', showLabel:true});";
                            break;
                        case 6: // (1+1/n)^n
                            expJs = @"
          calc.setExpression({id:'fx', latex:'f(x)=(1+\\frac{1}{x})^x', color:'#1565C0', lineWidth:3});
          calc.setExpression({id:'asym', latex:'y=2.71828', color:'#E53935', lineStyle:'DASHED', label:'y=e', showLabel:true});";
                            break;
                        case 7: // x^n/n!
                            expJs = @"
          calc.setExpression({id:'fx', latex:'f(x)=\\frac{2^x}{\\operatorname{gamma}(x+1)}', color:'#1565C0', lineWidth:3});
          calc.setExpression({id:'asym', latex:'y=0', color:'#2E7D32', lineStyle:'DASHED', label:'Giới hạn L=0 (khi x->∞)', showLabel:true});";
                            break;
                        case 8: // (a^x-1)/x với a=2
                            expJs = @"
          calc.setExpression({id:'fx', latex:'f(x)=\\frac{2^x-1}{x}', color:'#1565C0', lineWidth:3});
          calc.setExpression({id:'pt', latex:'(0,0.69315)', color:'#1565C0', pointStyle:'OPEN', pointSize:12, label:'Điểm khuyết (0,ln(2))', showLabel:true});";
                            break;
                        case 9: // sin(ax)/sin(bx) với a=2, b=3
                            expJs = @"
          calc.setExpression({id:'fx', latex:'f(x)=\\frac{\\sin(2x)}{\\sin(3x)}', color:'#1565C0', lineWidth:3});
          calc.setExpression({id:'pt', latex:'(0,0.66667)', color:'#1565C0', pointStyle:'OPEN', pointSize:12, label:'Điểm khuyết (0,2/3)', showLabel:true});";
                            break;
                    }
                    expJs += "calc.setMathBounds({ left: -6, right: 6, bottom: -2, top: 4 });";
                }

                var win = new GraphWindow(expJs, title);
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở đồ thị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ═══ HELPERS ═══
        private Border MakeChip(string text, bool active)
        {
            var btn = new Border
            {
                Background = new SolidColorBrush(active ? Accent : Color.FromArgb(20, Accent.R, Accent.G, Accent.B)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 6, 6),
                Cursor = Cursors.Hand,
                BorderBrush = new SolidColorBrush(Accent),
                BorderThickness = new Thickness(active ? 0 : 1)
            };
            btn.Child = new TextBlock
            {
                Text = text, FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(active ? Colors.White : Accent),
                FontFamily = new FontFamily("Segoe UI")
            };

            // Hover effects
            btn.MouseEnter += (_, _) =>
            {
                if (!active)
                {
                    btn.Background = new SolidColorBrush(Color.FromArgb(40, Accent.R, Accent.G, Accent.B));
                }
            };
            btn.MouseLeave += (_, _) =>
            {
                if (!active)
                {
                    btn.Background = new SolidColorBrush(Color.FromArgb(20, Accent.R, Accent.G, Accent.B));
                }
            };

            return btn;
        }

        private static string Fmt(double v) => v == (int)v ? $"{(int)v}" : $"{v:G6}";

        private static string FormatPoly2(double a, double b, double c)
        {
            var parts = new System.Collections.Generic.List<string>();
            void Add(double coeff, string suffix)
            {
                if (System.Math.Abs(coeff) < 1e-12) return;
                double val = System.Math.Abs(coeff);
                string term = (System.Math.Abs(val - 1) < 1e-12 && suffix.Length > 0) ? suffix : (UI.Fmt(val) + suffix);
                if (parts.Count == 0)
                {
                    parts.Add(coeff < 0 ? $"-{term}" : term);
                }
                else
                {
                    parts.Add(coeff > 0 ? $"+ {term}" : $"- {term}");
                }
            }
            Add(a, "x²"); Add(b, "x"); Add(c, "");
            return parts.Count > 0 ? string.Join(" ", parts) : "0";
        }

        private static string FormatPoly2Latex(double a, double b, double c)
        {
            var parts = new System.Collections.Generic.List<string>();
            void Add(double coeff, string suffix)
            {
                if (System.Math.Abs(coeff) < 1e-12) return;
                double val = System.Math.Abs(coeff);
                string term = (System.Math.Abs(val - 1) < 1e-12 && suffix.Length > 0) ? suffix : (val.ToString(System.Globalization.CultureInfo.InvariantCulture) + suffix);
                if (parts.Count == 0)
                {
                    parts.Add(coeff < 0 ? $"-{term}" : term);
                }
                else
                {
                    parts.Add(coeff > 0 ? $"+{term}" : $"-{term}");
                }
            }
            Add(a, "x^2"); Add(b, "x"); Add(c, "");
            return parts.Count > 0 ? string.Join("", parts) : "0";
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mainGrid == null || sideMenu == null || contentGuide == null || contentCalc == null || viewPractical == null)
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
                        Icon = "💊",
                        Title = isVN ? "Nồng độ thuốc trong máu qua thời gian" : "Blood Drug Concentration",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_limit_1_{suffix}.png",
                        Description = isVN 
                            ? "Trong y khoa, nồng độ thuốc trong cơ thể sau khi tiêm được mô hình hóa bằng các hàm phân thức. Khi thời gian t tiến tới vô cùng (t → ∞), nồng độ thuốc sẽ giảm dần về giới hạn bằng 0, giúp bác sĩ xác định chu kỳ liều lượng tiêm an toàn." 
                            : "In pharmacokinetics, patient drug concentration over time is modeled using rational equations. As time t approaches infinity (t → ∞), the drug concentration limit decays to 0, which guides clinicians in scheduling safe dosing intervals."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📈",
                        Title = isVN ? "Chi phí sản xuất tối thiểu của doanh nghiệp" : "Minimum Average Production Cost",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_limit_2_{suffix}.png",
                        Description = isVN 
                            ? "Chi phí trung bình để sản xuất một đơn vị sản phẩm AC(x) = C(x)/x sẽ giảm khi sản lượng x tăng. Giới hạn của AC(x) khi x → ∞ (tiệm cận ngang) chính là chi phí biến đổi tối thiểu, giúp doanh nghiệp tối ưu giá bán." 
                            : "The average cost to produce a single product unit AC(x) = C(x)/x decreases as production volume x increases. The limit of AC(x) as x → ∞ represents the horizontal asymptote of minimum unit cost, which optimizes profit margins."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🚀",
                        Title = isVN ? "Khối lượng tiệm cận vận tốc ánh sáng" : "Mass Expansion near Light Speed",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_limit_3_{suffix}.png",
                        Description = isVN 
                            ? "Theo Thuyết tương đối hẹp của Einstein, khi vận tốc v của một vật có khối lượng tiến dần tới vận tốc ánh sáng c (v → c⁻), giới hạn khối lượng tương đối tính sẽ tiến ra vô cùng (∞). Đây là lý do vật lý giải thích vì sao vật thể có khối lượng không bao giờ đạt được tốc độ ánh sáng." 
                            : "According to Einstein's special relativity, as an object's velocity v approaches the speed of light c (v → c⁻), the limit of its relativistic mass approaches infinity (∞). This provides the physical proof of why massive objects can never travel at light speed."
                    },
                    new PracticalAppItem
                    {
                        Icon = "👥",
                        Title = isVN ? "Sức chứa tối đa của môi trường (Sinh thái học)" : "Environmental Carrying Capacity (Ecology)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_limit_4_{suffix}.png",
                        Description = isVN 
                            ? "Dân số sinh vật trong một khu vực không thể tăng trưởng vô tận. Theo mô hình logistic, giới hạn của số lượng cá thể khi thời gian t → ∞ chính là sức chứa tối đa K của môi trường, quyết định sự cân bằng sinh thái." 
                            : "A biological population cannot grow indefinitely. According to the logistic model, the limit of the population size as time t → ∞ is the carrying capacity K of the environment, ensuring ecological balance."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💰",
                        Title = isVN ? "Mô hình lãi kép liên tục trong tài chính" : "Continuous Compound Interest (Finance)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_limit_5_{suffix}.png",
                        Description = isVN 
                            ? "Khi số lần tính lãi kép trong một năm tăng lên vô hạn (n → ∞), công thức lãi kép sẽ hội tụ về một giới hạn đặc biệt. Đây là cơ sở để tính lãi suất liên tục và tăng trưởng kinh tế vĩ mô." 
                            : "When the compounding frequency per year approaches infinity (n → ∞), the compound interest formula converges to a special limit. This is the foundation of continuous interest calculations and macroeconomic growth models."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧪",
                        Title = isVN ? "Nồng độ sản phẩm trong phản ứng hóa học tự giới hạn" : "Reactant Limit in Self-Limiting Reactions",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_limit_6_{suffix}.png",
                        Description = isVN 
                            ? "Trong phản ứng hóa học hoặc sinh hóa xúc tác bởi enzyme, lượng sản phẩm tạo ra tăng dần nhưng bị chặn trên bởi lượng chất tham gia ban đầu. Khi thời gian t → ∞, lượng sản phẩm đạt giới hạn bão hòa tối đa." 
                            : "In chemical or enzyme-catalyzed reactions, the generated product increases over time but is bounded by the initial reactants. As time t → ∞, the product quantity converges to its maximum saturation limit."
                    }
                };

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for LimitTool: {Err}", ex.Message);
            }
        }
    }
}
