using QASmartClass.LearningTools.Models;
using System;

using QASmartClass.LearningTools.Helpers;

using System.Globalization;

using System.Linq;

using System.Numerics;

using System.Windows;

using System.Windows.Controls;

using System.Windows.Input;

using System.Windows.Media;

using QASmartClass.LearningTools.Controls;



namespace QASmartClass.LearningTools.Views.Math

{

    public partial class CombinatoricsTool : BaseToolControl

    {

        private enum Mode { Permutation, Arrangement, Combination, Newton }

        private Mode _mode = Mode.Permutation;



        public CombinatoricsTool()

        {

            InitializeComponent();

            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                menuTextPractice.Text = isVN ? "Thực hành tính toán" : "Practice Calculation";
                menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";
                
                sideMenu.SelectedIndex = 0;

                BuildModeButtons(); UpdateUI(); BuildPresets(); Calc(); LoadPracticalApps();
                
                // Bàn phím số mini — đồng bộ với các tool khác
                TouchNumPad.Attach(txtN, step: 1, min: 0, max: 170, allowDecimal: false);
                TouchNumPad.Attach(txtK, step: 1, min: 0, max: 170, allowDecimal: false);
                TouchNumPad.Attach(txtA, step: 1);
                TouchNumPad.Attach(txtB, step: 1);
            };

        }



        // ═══ MODE SELECTOR ═══

        private void BuildModeButtons()

        {

            modePanel.Children.Clear();

            var modes = new (Mode M, string Label, string Desc)[]

            {

                (Mode.Permutation,  "P(n) Hoán vị",    "n!"),

                (Mode.Arrangement,  "A(n,k) Chỉnh hợp", "n!/(n-k)!"),

                (Mode.Combination,  "C(n,k) Tổ hợp",   "n!/[k!(n-k)!]"),

                (Mode.Newton,       "Newton (a+b)ⁿ",   "Nhị thức Newton"),

            };



            foreach (var (m, label, desc) in modes)

            {

                var c = (Color)ColorConverter.ConvertFromString("#0D47A1");

                bool isActive = m == _mode;

                var btn = new Border

                {

                    Background = new SolidColorBrush(isActive ? Color.FromArgb(50, c.R, c.G, c.B) : Color.FromArgb(15, c.R, c.G, c.B)),

                    BorderBrush = new SolidColorBrush(c),

                    BorderThickness = new Thickness(0, 0, 0, isActive ? 3 : 1),

                    CornerRadius = new CornerRadius(8),

                    Padding = new Thickness(14, 8, 14, 8),

                    Margin = new Thickness(0, 0, 8, 8),

                    Cursor = Cursors.Hand,

                    ToolTip = desc

                };

                var sp = new StackPanel();

                sp.Children.Add(new TextBlock

                {

                    Text = label, FontSize = 12, FontWeight = isActive ? FontWeights.Bold : FontWeights.SemiBold,

                    Foreground = new SolidColorBrush(c)

                });

                sp.Children.Add(new TextBlock

                {

                    Text = desc, FontSize = 9, Foreground = new SolidColorBrush(Color.FromArgb(160, c.R, c.G, c.B))

                });

                btn.Child = sp;



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

            switch (_mode)

            {

                case Mode.Permutation:

                    txtFormula.Text = "📐 Hoán vị: P(n) = n!";

                    kInputPanel.Visibility = Visibility.Collapsed;

                    newtonPanel.Visibility = Visibility.Collapsed;

                    txtConstraint.Text = "n ≥ 0, n ≤ 170 (giới hạn double)";

                    break;

                case Mode.Arrangement:

                    txtFormula.Text = "📐 Chỉnh hợp: A(n,k) = n! / (n-k)!";

                    kInputPanel.Visibility = Visibility.Visible;

                    newtonPanel.Visibility = Visibility.Collapsed;

                    txtConstraint.Text = "0 ≤ k ≤ n ≤ 170";

                    break;

                case Mode.Combination:

                    txtFormula.Text = "📐 Tổ hợp: C(n,k) = n! / [k!(n-k)!]";

                    kInputPanel.Visibility = Visibility.Visible;

                    newtonPanel.Visibility = Visibility.Collapsed;

                    txtConstraint.Text = "0 ≤ k ≤ n ≤ 170";

                    break;

                case Mode.Newton:

                    txtFormula.Text = "📐 Nhị thức Newton: (a+b)ⁿ = Σ C(n,k)·aⁿ⁻ᵏ·bᵏ";

                    kInputPanel.Visibility = Visibility.Collapsed;

                    newtonPanel.Visibility = Visibility.Visible;

                    txtConstraint.Text = "n ≥ 0, n ≤ 20 (khai triển)";

                    break;

            }

        }



        // ═══ CALCULATION ═══

        private void Input_Changed(object sender, EventArgs e) { if (IsLoaded) Calc(); }

        private void Newton_Changed(object sender, EventArgs e) { if (IsLoaded && _mode == Mode.Newton) Calc(); }



        private void Calc()

        {

            resultPanel.Children.Clear();



            if (!int.TryParse(txtN?.Text?.Trim(), out int n) || n < 0 || n > 170) { UI.ResultRow("Vui lòng nhập n hợp lệ (0-170)", "#C62828", resultPanel); return; }



            switch (_mode)

            {

                case Mode.Permutation:

                    CalcPermutation(n);

                    break;

                case Mode.Arrangement:

                    if (!int.TryParse(txtK?.Text?.Trim(), out int ka) || ka < 0 || ka > n) { UI.ResultRow("k phải ≤ n", "#C62828", resultPanel); return; }

                    CalcArrangement(n, ka);

                    break;

                case Mode.Combination:

                    if (!int.TryParse(txtK?.Text?.Trim(), out int kc) || kc < 0 || kc > n) { UI.ResultRow("k phải ≤ n", "#C62828", resultPanel); return; }

                    CalcCombination(n, kc);

                    break;

                case Mode.Newton:

                    CalcNewton(n);

                    break;

            }

        }



        private void CalcPermutation(int n)

        {

            var result = Factorial(n);

            UI.ResultRow($"Công thức = P({n}) = {n}!", "#1565C0", resultPanel);

            UI.ResultRow($"Kết quả = {FormatBig(result)}", "#1565C0", resultPanel);



            // Show step-by-step for small n

            if (n <= 12 && n > 0)

            {

                var steps = string.Join(" × ", Enumerable.Range(1, n).Reverse());

                UI.ResultRow($"Khai triển = {steps}", "#1565C0", resultPanel);

            }



            UI.ResultRow($"📌 Ý nghĩa: Số cách xếp {n} phần tử vào {n} vị trí", "#E65100", resultPanel);



            // Example

            if (n >= 2 && n <= 8)

                UI.ResultRow($"Ví dụ {n} người xếp hàng: {FormatBig(result)} cách", "#1565C0", resultPanel);

        }



        private void CalcArrangement(int n, int k)

        {

            var result = Factorial(n) / Factorial(n - k);

            UI.ResultRow($"Công thức = A({n},{k}) = {n}! / ({n}-{k})!", "#1565C0", resultPanel);

            UI.ResultRow($"Kết quả = {FormatBig(result)}", "#1565C0", resultPanel);



            // Step-by-step

            if (n <= 20 && k > 0)

            {

                var steps = string.Join(" × ", Enumerable.Range(n - k + 1, k).Reverse());

                UI.ResultRow($"Khai triển = {steps}", "#1565C0", resultPanel);

            }



            UI.ResultRow($"📌 Ý nghĩa: Số cách chọn và sắp xếp {k} phần tử từ {n} phần tử", "#E65100", resultPanel);

            UI.ResultRow($"💡 A({n},{k}) = C({n},{k}) × P({k})", "#E65100", resultPanel);

        }



        private void CalcCombination(int n, int k)

        {

            var result = Factorial(n) / (Factorial(k) * Factorial(n - k));

            UI.ResultRow($"Công thức = C({n},{k}) = {n}! / ({k}!({n}-{k})!)", "#1565C0", resultPanel);

            UI.ResultRow($"Kết quả = {FormatBig(result)}", "#1565C0", resultPanel);



            // Properties

            UI.ResultRow($"💡 Tính chất: C({n},{k}) = C({n},{n - k})", "#E65100", resultPanel);



            if (n <= 20)

            {

                // Pascal's triangle row

                var row = Enumerable.Range(0, n + 1)

                    .Select(i => FormatBig(Factorial(n) / (Factorial(i) * Factorial(n - i))))

                    .ToArray();

                if (row.Length <= 15)

                    UI.ResultRow($"📐 Hàng {n} Pascal: {string.Join(", ", row)}", "#7B1FA2", resultPanel);

            }



            UI.ResultRow($"📌 Ý nghĩa: Số cách chọn {k} phần tử từ {n} phần tử (không xét thứ tự)", "#E65100", resultPanel);

        }



        private static string GetNewtonAlgebraicString(double a, double b, int n)

        {

            if (n < 0) return "";

            if (n == 0) return "1";



            var sb = new System.Text.StringBuilder();

            

            // Định dạng vế trái

            string termA = "";

            if (a != 0)

            {

                termA = a == 1 ? "x" : (a == -1 ? "-x" : $"{UI.Fmt(a)}x");

            }

            

            string termB = "";

            if (b != 0)

            {

                if (a == 0)

                {

                    termB = b == 1 ? "y" : (b == -1 ? "-y" : $"{UI.Fmt(b)}y");

                }

                else

                {

                    termB = b > 0 ? $" + {UI.Fmt(b)}y" : $" - {UI.Fmt(-b)}y";

                }

            }

            

            if (a == 0 && b == 0) return "0";



            string toSuperscript(int power)

            {

                if (power <= 1) return "";

                var s = power.ToString();

                var res = "";

                foreach (var c in s)

                {

                    switch (c)

                    {

                        case '0': res += "⁰"; break;

                        case '1': res += "¹"; break;

                        case '2': res += "²"; break;

                        case '3': res += "³"; break;

                        case '4': res += "⁴"; break;

                        case '5': res += "⁵"; break;

                        case '6': res += "⁶"; break;

                        case '7': res += "⁷"; break;

                        case '8': res += "⁸"; break;

                        case '9': res += "⁹"; break;

                    }

                }

                return res;

            }

            

            string leftSideFormatted = $"({termA}{termB.TrimStart()}){toSuperscript(n)}";

            sb.Append($"{leftSideFormatted} = ");



            bool isFirst = true;

            for (int k = 0; k <= n; k++)

            {

                var ck = Factorial(n) / (Factorial(k) * Factorial(n - k));

                double coeff = (double)ck * System.Math.Pow(a, n - k) * System.Math.Pow(b, k);

                

                if (System.Math.Abs(coeff) < 1e-9) continue; // Bỏ qua hệ số 0



                // Quyết định dấu

                if (coeff > 0)

                {

                    if (!isFirst) sb.Append(" + ");

                }

                else

                {

                    if (isFirst) sb.Append("-");

                    else sb.Append(" - ");

                }



                double absCoeff = System.Math.Abs(coeff);

                int powerX = n - k;

                int powerY = k;



                bool hasVars = powerX > 0 || powerY > 0;

                

                // In hệ số

                if (!hasVars)

                {

                    sb.Append(UI.Fmt(absCoeff));

                }

                else

                {

                    if (System.Math.Abs(absCoeff - 1) > 1e-9)

                    {

                        sb.Append(UI.Fmt(absCoeff));

                    }

                }



                // In biến X

                if (powerX > 0)

                {

                    sb.Append("x" + toSuperscript(powerX));

                }



                // In biến Y

                if (powerY > 0)

                {

                    sb.Append("y" + toSuperscript(powerY));

                }



                isFirst = false;

            }



            return sb.ToString();

        }



        private void CalcNewton(int n)

        {

            newtonResultPanel.Children.Clear();



            if (n > 20) { UI.ResultRow("⚠️ n quá lớn, giới hạn n ≤ 20", "#C62828", newtonResultPanel); return; }



            if (!ParsingHelper.TryParseDouble(txtA?.Text?.Trim(), out double a) ||

                !ParsingHelper.TryParseDouble(txtB?.Text?.Trim(), out double b))

            {

                UI.ResultRow("⚠️ Vui lòng nhập hệ số a và b hợp lệ (số thực)", "#C62828", newtonResultPanel);

                return;

            }



            string algebraicStr = GetNewtonAlgebraicString(a, b, n);

            if (!string.IsNullOrEmpty(algebraicStr))

            {

                UI.ResultRow("📝 Khai triển đại số:", "#D81B60", newtonResultPanel);

                UI.ResultRow($"   {algebraicStr}", "#D81B60", newtonResultPanel);

            }



            UI.ResultRow($"📊 ({UI.Fmt(a)} + {UI.Fmt(b)})^{n} = Σ C({n},k)·{UI.Fmt(a)}^({n}-k)·{UI.Fmt(b)}^k", "#0D47A1", newtonResultPanel);



            var terms = new string[n + 1];

            double total = 0;

            for (int k = 0; k <= n; k++)

            {

                var ck = Factorial(n) / (Factorial(k) * Factorial(n - k));

                double coeff = (double)ck * System.Math.Pow(a, n - k) * System.Math.Pow(b, k);

                total += coeff;



                string term;

                if (a == 1 && b == 1)

                    term = $"C({n},{k})={FormatBig(ck)}";

                else

                    term = $"C({n},{k})·{UI.Fmt(System.Math.Pow(a, n - k))}·{UI.Fmt(System.Math.Pow(b, k))} = {UI.Fmt(coeff)}";

                terms[k] = term;

            }



            // Show terms (max 15)

            int showCount = System.Math.Min(n + 1, 15);

            for (int k = 0; k < showCount; k++)

                UI.ResultRow($"  k={k}: {terms[k]}", k % 2 == 0 ? "#1565C0" : "#2E7D32", newtonResultPanel);

            if (n + 1 > 15)

                UI.ResultRow($"  ... và {n + 1 - 15} số hạng nữa", "#757575", newtonResultPanel);



            UI.ResultRow($"📐 Kết quả: ({UI.Fmt(a)} + {UI.Fmt(b)})^{n} = {UI.Fmt(total)}", "#C62828", newtonResultPanel);



            // Sum of C(n,k)

            if (a == 1 && b == 1)

                UI.ResultRow($"💡 Σ C({n},k) = 2^{n} = {FormatBig(BigInteger.Pow(2, n))}", "#7B1FA2", newtonResultPanel);

        }



        // ═══ Graph ═══
        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtN?.Text?.Trim(), out int n) || n < 0) return;

                int plotN = System.Math.Min(n, 20);
                string warning = n > 20 ? " (Đồ thị hiển thị tối đa hàng n = 20)" : "";
                
                var ci = CultureInfo.InvariantCulture;
                string xList, yList;
                string title, subtitle;

                if (_mode == Mode.Newton)
                {
                    if (!ParsingHelper.TryParseDouble(txtA?.Text?.Trim(), out double a)) a = 1;
                    if (!ParsingHelper.TryParseDouble(txtB?.Text?.Trim(), out double b)) b = 1;

                    xList = string.Join(",", Enumerable.Range(0, plotN + 1));
                    yList = string.Join(",", Enumerable.Range(0, plotN + 1).Select(k =>
                    {
                        double ck = (double)(Factorial(plotN) / (Factorial(k) * Factorial(plotN - k)));
                        double coeff = ck * System.Math.Pow(a, plotN - k) * System.Math.Pow(b, k);
                        return coeff.ToString(ci);
                    }));
                    
                    title = $"📊 Hệ số ({UI.Fmt(a)}x + {UI.Fmt(b)}y)^{plotN}";
                    subtitle = $"Đồ thị phân bố hệ số khai triển Newton bậc {plotN}{warning}";
                }
                else
                {
                    xList = string.Join(",", Enumerable.Range(0, plotN + 1));
                    yList = string.Join(",", Enumerable.Range(0, plotN + 1).Select(k =>
                        ((double)(Factorial(plotN) / (Factorial(k) * Factorial(plotN - k)))).ToString(ci)));

                    title = $"🎲 C({plotN},k) — Hàng {plotN} Pascal";
                    subtitle = $"Tam giác Pascal hàng thứ {plotN}{warning}";
                }

                // Tính toán giới hạn trục Y hợp lý
                double yMax = 10;
                if (_mode == Mode.Newton)
                {
                    if (!ParsingHelper.TryParseDouble(txtA?.Text?.Trim(), out double a)) a = 1;
                    if (!ParsingHelper.TryParseDouble(txtB?.Text?.Trim(), out double b)) b = 1;
                    
                    var values = Enumerable.Range(0, plotN + 1).Select(k =>
                    {
                        double ck = (double)(Factorial(plotN) / (Factorial(k) * Factorial(plotN - k)));
                        return System.Math.Abs(ck * System.Math.Pow(a, plotN - k) * System.Math.Pow(b, k));
                    }).ToArray();
                    yMax = values.Length > 0 ? values.Max() : 10;
                }
                else
                {
                    double ckMax = (double)(Factorial(plotN) / (Factorial(plotN / 2) * Factorial(plotN - plotN / 2)));
                    yMax = ckMax;
                }
                if (yMax <= 0) yMax = 10;

                string expJs = $@"
        calc.setExpression({{id:'pts', latex:'([{xList}],[{yList}])', color:'#1565C0', pointSize:8, pointStyle:'POINT'}});
        calc.setExpression({{id:'bar', latex:'y=0', color:'#BBDEFB', lineWidth:1}});
        calc.setMathBounds({{ left: -1, right: {plotN + 1}, bottom: -{yMax * 0.1}, top: {yMax * 1.1} }});";

                var win = new GraphWindow(expJs, title, subtitle);
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        // ═══ PRESETS ═══
        private void BuildPresets()
        {
            if (presetPanel == null) return;
            presetPanel.Children.Clear();
            var presets = new (string L, string N, string K, string A, string B, Mode M)[]
            {
                ("P(5) = 120",          "5", "",  "",   "",   Mode.Permutation),
                ("P(7) = 5040",         "7", "",  "",   "",   Mode.Permutation),
                ("A(5,2) = 20",         "5", "2", "",   "",   Mode.Arrangement),
                ("A(10,3) = 720",       "10","3", "",   "",   Mode.Arrangement),
                ("C(6,2) = 15",         "6", "2", "",   "",   Mode.Combination),
                ("C(52,5) = 2.598.960", "52","5", "",   "",   Mode.Combination),
                ("(x + y)⁵",            "5", "",  "1",  "1",  Mode.Newton),
                ("(2x - 3y)³",          "3", "",  "2",  "-3", Mode.Newton),
                ("(x - 2y)⁴",           "4", "",  "1",  "-2", Mode.Newton),
            };
            foreach (var (l, n, k, a, b, m) in presets)
            {
                var c = (Color)ColorConverter.ConvertFromString("#0D47A1");
                var btn = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(25, c.R, c.G, c.B)),
                    BorderBrush = new SolidColorBrush(c), BorderThickness = new Thickness(0, 0, 0, 2),
                    CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 8, 8), Cursor = Cursors.Hand
                };
                btn.Child = new TextBlock { Text = l, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(c) };
                string cn = n; string ck = k; string ca = a; string cb = b; Mode cm = m;
                btn.MouseLeftButtonDown += (_, _) =>
                {
                    _mode = cm;
                    txtN.Text = cn;
                    if (!string.IsNullOrEmpty(ck)) txtK.Text = ck;
                    if (!string.IsNullOrEmpty(ca)) txtA.Text = ca;
                    if (!string.IsNullOrEmpty(cb)) txtB.Text = cb;
                    BuildModeButtons();
                    UpdateUI();
                    Calc();
                };
                presetPanel.Children.Add(btn);
            }
        }

        // ═══ HELPERS ═══

        private static BigInteger Factorial(int n)

        {

            if (n <= 1) return 1;

            BigInteger result = 1;

            for (int i = 2; i <= n; i++) result *= i;

            return result;

        }



        private static string FormatBig(BigInteger v)

        {

            var s = v.ToString();

            if (s.Length <= 30) return s;

            return s.Substring(0, 5) + "..." + s.Substring(s.Length - 5) + $" ({s.Length} chữ số)";

        }



        private static string Fmt(double v) =>

            System.Math.Abs(v - System.Math.Round(v)) < 1e-9

                ? ((long)System.Math.Round(v)).ToString()

                : v.ToString("G6");



        /* AddResult / AddR replaced by UI.ResultRow */



        /* AddResult / AddR replaced by UI.ResultRow */



    
        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootGrid == null || sideMenu == null || viewGuide == null || viewPractice == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewPractice.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            int index = sideMenu.SelectedIndex;
            if (index == 0)
            {
                viewGuide.Visibility = Visibility.Visible;
            }
            else if (index == 1)
            {
                viewPractice.Visibility = Visibility.Visible;
            }
            else if (index == 2)
            {
                viewPractical.Visibility = Visibility.Visible;
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
                        Icon = "🎮",
                        Title = isVN ? "Thiết Kế Game" : "Game Design",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_combinatorics_1_{suffix}.png",
                        Description = isVN 
                            ? "Ứng dụng: Thiết kế màn chơi Rubik, Sudoku hoặc tạo map ngẫu nhiên sử dụng tổ hợp/chỉnh hợp để tính toán số lượng cấu hình màn chơi khả thi." 
                            : "Design level systems for Rubik, Sudoku, or random map generation using permutations and combinations to calculate playable states."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔒",
                        Title = isVN ? "Bảo Mật Mật Khẩu" : "Password Security",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_combinatorics_2_{suffix}.png",
                        Description = isVN 
                            ? "Ứng dụng: Phân tích độ dài và số tổ hợp ký tự để đánh giá khả năng chống chịu tấn công dò mật khẩu (brute-force) bảo mật hệ thống." 
                            : "Analyze password lengths and character combinations to evaluate defense strength against brute-force guessing attacks."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧬",
                        Title = isVN ? "Giải Mã Gen DNA" : "DNA Decoding",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_combinatorics_3_{suffix}.png",
                        Description = isVN 
                            ? "Ứng dụng: Phân tích tổ hợp liên kết các cặp nucleotide (A, T, G, C) trong phân tử DNA để nghiên cứu cấu trúc di truyền sinh học." 
                            : "Analyze combinations of nucleotide pairs (A, T, G, C) in DNA molecules to study biological genetic structures."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌐",
                        Title = isVN ? "Định Tuyến Mạng" : "Network Routing",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_combinatorics_4_{suffix}.png",
                        Description = isVN 
                            ? "Ứng dụng: Thuật toán tìm số lượng và chỉnh hợp đường đi tối ưu giữa các nút mạng để truyền dữ liệu nhanh nhất, tránh nghẽn băng thông." 
                            : "Calculate optimal routes and permutations between network nodes to speed up data transmission and avoid congestion."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎰",
                        Title = isVN ? "Xác Suất Vé Số" : "Lottery Probability",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_combinatorics_5_{suffix}.png",
                        Description = isVN 
                            ? "Ứng dụng: Tính xác suất trúng giải độc đắc Vietlott (ví dụ Power 6/55) dựa trên số tổ hợp chọn 6 số từ tập hợp 55 số: C(55,6)." 
                            : "Calculate the probability of winning the jackpot (e.g. Power 6/55) based on choosing 6 numbers from a pool of 55: C(55,6)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💼",
                        Title = isVN ? "Tối Ưu Hóa Đầu Tư" : "Portfolio Optimization",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_combinatorics_6_{suffix}.png",
                        Description = isVN 
                            ? "Ứng dụng: Lựa chọn tổ hợp tối ưu các nhóm tài sản, cổ phiếu từ danh mục thị trường để đa dạng hóa vốn và giảm thiểu rủi ro tài chính." 
                            : "Choose optimal combinations of assets and stocks to diversify capital and minimize financial risks."
                    }
                };

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for CombinatoricsTool: {Err}", ex.Message);
            }
        }
}

}




