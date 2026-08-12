using System;
using System.Collections.Generic;
using QASmartClass.LearningTools.Helpers;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;



namespace QASmartClass.LearningTools.Views.Math

{

    public partial class ProbabilityTool : BaseToolControl

    {

        private enum Mode { Classic, Bernoulli, Simulation }

        private Mode _mode = Mode.Classic;

        private readonly Random _rng = new();

        private static readonly BigInteger[] FactCache = new BigInteger[171];

        static ProbabilityTool()
        {
            FactCache[0] = 1;
            BigInteger current = 1;
            for (int i = 1; i <= 170; i++)
            {
                current *= i;
                FactCache[i] = current;
            }
        }



        public ProbabilityTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Tính toán & Lý thuyết" : "Calculator & Theory";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";

                if (sideMenu != null)
                {
                    sideMenu.SelectionChanged += SideMenu_SelectionChanged;
                    sideMenu.SelectedIndex = 0;
                }

                BuildModeButtons(); BuildPresets(); Calc();
                LoadPracticalApps();

                // Bàn phím số mini — đồng bộ với các tool khác
                TouchNumPad.Attach(txtFavorable, step: 1, min: 0, allowDecimal: false);
                TouchNumPad.Attach(txtTotal, step: 1, min: 1, allowDecimal: false);
                TouchNumPad.Attach(txtBernN, step: 1, min: 0, max: 170, allowDecimal: false);
                TouchNumPad.Attach(txtBernK, step: 1, min: 0, max: 170, allowDecimal: false);
                TouchNumPad.Attach(txtBernP, step: 0.05, min: 0, max: 1);
                TouchNumPad.Attach(txtSimTrials, step: 100, min: 1, max: 100000, allowDecimal: false);
                TouchNumPad.Attach(txtRedBalls, step: 1, min: 1, max: 100, allowDecimal: false);
                TouchNumPad.Attach(txtBlueBalls, step: 1, min: 1, max: 100, allowDecimal: false);
                TouchNumPad.Attach(txtDraws, step: 1, min: 1, max: 200, allowDecimal: false);
                TouchNumPad.Attach(txtBallSimTrials, step: 100, min: 1, max: 100000, allowDecimal: false);
            };
        }



        // ═══ MODE SELECTOR ═══

        private void BuildModeButtons()

        {

            modePanel.Children.Clear();

            var modes = new (Mode M, string Label, string Desc)[]

            {

                (Mode.Classic,    "Xác suất Cổ điển",     "|A|/|Ω|"),

                (Mode.Bernoulli,  "Bernoulli",        "C(n,k)·pᵏ·(1-p)ⁿ⁻ᵏ"),

                (Mode.Simulation, "🎲 Mô phỏng",     "Xúc xắc, đồng xu"),

            };



            foreach (var (m, label, desc) in modes)

            {

                var c = (Color)ColorConverter.ConvertFromString("#1B5E20");

                bool isActive = m == _mode;

                var btn = new Border

                {

                    Background = new SolidColorBrush(isActive ? Color.FromArgb(50, c.R, c.G, c.B) : Color.FromArgb(15, c.R, c.G, c.B)),

                    BorderBrush = new SolidColorBrush(c),

                    BorderThickness = new Thickness(0, 0, 0, isActive ? 3 : 1),

                    CornerRadius = new CornerRadius(8),

                    Padding = new Thickness(14, 8, 14, 8),

                    Margin = new Thickness(0, 0, 8, 8),

                    Cursor = Cursors.Hand, ToolTip = desc

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

                    if (_mode != Mode.Simulation) Calc();

                };

                modePanel.Children.Add(btn);

            }

        }



        private void UpdateUI()

        {

            classicInput.Visibility = _mode == Mode.Classic ? Visibility.Visible : Visibility.Collapsed;

            bernoulliInput.Visibility = _mode == Mode.Bernoulli ? Visibility.Visible : Visibility.Collapsed;

            simInput.Visibility = _mode == Mode.Simulation ? Visibility.Visible : Visibility.Collapsed;



            switch (_mode)

            {

                case Mode.Classic:

                    txtFormula.Text = "📐 P(A) = |A| / |Ω|";

                    txtHint.Text = "|A| = Số kết quả thuận lợi cho biến cố A, |Ω| = Số phần tử của không gian mẫu";

                    break;

                case Mode.Bernoulli:

                    txtFormula.Text = "📐 P(X=k) = C(n,k) · pᵏ · (1-p)ⁿ⁻ᵏ";

                    txtHint.Text = "n = số lần thử, k = số lần thành công, p = xác suất thành công mỗi lần";

                    break;

                case Mode.Simulation:

                    txtFormula.Text = "🎲 Mô Phỏng Xác Suất Thực Nghiệm";

                    txtHint.Text = "Nhấn nút để mô phỏng gieo xúc xắc / tung đồng xu";

                    break;

            }

            if (btnExportCsv != null) btnExportCsv.Visibility = Visibility.Collapsed;

        }



        // ═══ CALCULATION ═══

        private void Input_Changed(object sender, EventArgs e) { if (IsLoaded && _mode != Mode.Simulation) Calc(); }



        private void Calc()

        {

            resultPanel.Children.Clear();



            switch (_mode)

            {

                case Mode.Classic:

                    CalcClassic();

                    break;

                case Mode.Bernoulli:

                    CalcBernoulli();

                    break;

            }

        }



        private void CalcClassic()

        {
            if (btnExportCsv != null) btnExportCsv.Visibility = Visibility.Collapsed;

            if (!long.TryParse(txtFavorable?.Text?.Trim(), out long favCount) || favCount < 0)

            { UI.ResultRow("⚠️ Số phần tử |A| phải là số nguyên không âm", "#C62828", resultPanel); return; }

            if (!long.TryParse(txtTotal?.Text?.Trim(), out long totalCount) || totalCount <= 0)

            { UI.ResultRow("⚠️ Số phần tử |Ω| phải là số nguyên dương", "#C62828", resultPanel); return; }

            if (favCount > totalCount)

            { UI.ResultRow("⚠️ Số phần tử thuận lợi |A| không thể lớn hơn không gian mẫu |Ω|", "#C62828", resultPanel); return; }



            double p = (double)favCount / totalCount;

            double pComplement = 1 - p;



            UI.ResultRow($"📊 P(A) = |A| / |Ω| = {UI.Fmt((double)favCount)} / {UI.Fmt((double)totalCount)}", "#0D47A1", resultPanel);

            UI.ResultRow($"📐 P(A) = {UI.Fmt(p)} = {(p * 100):F2}%", "#1B5E20", resultPanel);

            UI.ResultRow($"🔄 P(Ā) = 1 - P(A) = {UI.Fmt(pComplement)} = {(pComplement * 100):F2}%", "#E65100", resultPanel);



            // Odds

            if (favCount > 0 && pComplement > 0)

            {

                double odds = (double)favCount / (totalCount - favCount);

                UI.ResultRow($"📊 Odds = |A| : |Ā| = {UI.Fmt((double)favCount)} : {UI.Fmt((double)(totalCount - favCount))} ≈ {UI.Fmt(odds)}", "#7B1FA2", resultPanel);

            }



            // Fraction representation

            var (num, den) = SimplifyFraction(favCount, totalCount);

            if (den != totalCount)

                UI.ResultRow($"📝 Rút gọn: {num}/{den}", "#2E7D32", resultPanel);



            // Interpretation

            if (p == 0) UI.ResultRow("💡 Biến cố không thể xảy ra (P = 0)", "#757575", resultPanel);

            else if (p == 1) UI.ResultRow("💡 Biến cố chắc chắn xảy ra (P = 1)", "#757575", resultPanel);

            else if (p < 0.1) UI.ResultRow("💡 Biến cố rất khó xảy ra (P < 10%)", "#757575", resultPanel);

            else if (p < 0.5) UI.ResultRow("💡 Biến cố ít khả năng xảy ra (P < 50%)", "#757575", resultPanel);

            else if (p > 0.9) UI.ResultRow("💡 Biến cố rất có khả năng xảy ra (P > 90%)", "#757575", resultPanel);

            else UI.ResultRow("💡 Biến cố có khả năng xảy ra vừa phải", "#757575", resultPanel);

        }



        private void CalcBernoulli()

        {
            if (btnExportCsv != null) btnExportCsv.Visibility = Visibility.Visible;

            if (!int.TryParse(txtBernN?.Text?.Trim(), out int n) || n < 0 || n > 170)

            { UI.ResultRow("⚠️ Nhập n hợp lệ (0 ≤ n ≤ 170)", "#C62828", resultPanel); return; }

            if (!int.TryParse(txtBernK?.Text?.Trim(), out int k) || k < 0 || k > n)

            { UI.ResultRow("⚠️ Nhập k hợp lệ (0 ≤ k ≤ n)", "#C62828", resultPanel); return; }

            if (!ParsingHelper.TryParseDouble(txtBernP?.Text?.Trim(), out double p) || p < 0 || p > 1)

            { UI.ResultRow("⚠️ Nhập p hợp lệ (0 ≤ p ≤ 1)", "#C62828", resultPanel); return; }



            // P(X=k)

            double cnk = (double)(Factorial(n) / (Factorial(k) * Factorial(n - k)));

            double pk = System.Math.Pow(p, k) * System.Math.Pow(1 - p, n - k);

            double prob = cnk * pk;



            UI.ResultRow($"📊 P(X={k}) = C({n},{k}) · {UI.Fmt(p)}^{k} · {UI.Fmt(1 - p)}^{n - k}", "#0D47A1", resultPanel);

            UI.ResultRow($"📐 = {UI.Fmt(cnk)} × {UI.Fmt(System.Math.Pow(p, k))} × {UI.Fmt(System.Math.Pow(1 - p, n - k))}", "#1565C0", resultPanel);

            UI.ResultRow($"🎯 P(X={k}) = {UI.Fmt(prob)} = {(prob * 100):F4}%", "#1B5E20", resultPanel);



            // Expected value & Variance

            double mu = n * p;

            double sigma2 = n * p * (1 - p);

            double sigma = System.Math.Sqrt(sigma2);

            UI.ResultRow($"📏 E(X) = np = {n}×{UI.Fmt(p)} = {UI.Fmt(mu)}", "#7B1FA2", resultPanel);

            UI.ResultRow($"📏 Var(X) = np(1-p) = {UI.Fmt(sigma2)}, σ = {UI.Fmt(sigma)}", "#283593", resultPanel);



            // P(X ≤ k) cumulative

            double cumProb = 0;

            for (int i = 0; i <= k; i++)

            {

                double ci = (double)(Factorial(n) / (Factorial(i) * Factorial(n - i)));

                cumProb += ci * System.Math.Pow(p, i) * System.Math.Pow(1 - p, n - i);

            }

            UI.ResultRow($"📊 P(X ≤ {k}) = {UI.Fmt(cumProb)} = {(cumProb * 100):F4}%", "#E65100", resultPanel);

            UI.ResultRow($"📊 P(X > {k}) = {UI.Fmt(1 - cumProb)} = {((1 - cumProb) * 100):F4}%", "#C62828", resultPanel);



            // Distribution table (for small n)

            if (n <= 15)

            {

                UI.ResultRow("── Bảng phân bố ──", "#424242", resultPanel);

                for (int i = 0; i <= n; i++)

                {

                    double ci = (double)(Factorial(n) / (Factorial(i) * Factorial(n - i)));

                    double pi = ci * System.Math.Pow(p, i) * System.Math.Pow(1 - p, n - i);

                    string marker = i == k ? " ◀ bạn chọn" : "";

                    UI.ResultRow($"  P(X={i}) = {(pi * 100):F2}%{marker}", i == k ? "#1B5E20" : "#757575");

                }

            }

        }



        // ═══ SIMULATION ═══

        private void AddSimulationBar(string label, int count, double freq, string colorHex, StackPanel panel)

        {

            var color = (Color)ColorConverter.ConvertFromString(colorHex);

            

            var rowGrid = new Grid { Margin = new Thickness(0, 2, 0, 2) };

            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });

            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });



            var textBlock = new TextBlock

            {

                Text = $"{label}: {count:N0} lần ({(freq * 100):F2}%)",

                FontSize = 14,

                FontWeight = FontWeights.SemiBold,

                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),

                VerticalAlignment = VerticalAlignment.Center

            };

            Grid.SetColumn(textBlock, 0);

            rowGrid.Children.Add(textBlock);



            var barContainer = new Border

            {

                Background = new SolidColorBrush(Color.FromRgb(240, 244, 241)),

                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 230, 222)),

                BorderThickness = new Thickness(1),

                Height = 16,

                CornerRadius = new CornerRadius(8),

                HorizontalAlignment = HorizontalAlignment.Left,

                Width = 250,

                Margin = new Thickness(12, 0, 0, 0),

                ClipToBounds = true

            };



            var barFill = new Border

            {

                Background = new SolidColorBrush(color),

                HorizontalAlignment = HorizontalAlignment.Left,

                Width = System.Math.Max(2, freq * 250),

                CornerRadius = new CornerRadius(8)

            };

            

            barContainer.Child = barFill;

            Grid.SetColumn(barContainer, 1);

            rowGrid.Children.Add(barContainer);



            var wrapperBorder = new Border

            {

                Background = new SolidColorBrush(Color.FromArgb(12, color.R, color.G, color.B)),

                CornerRadius = new CornerRadius(6),

                Padding = new Thickness(12, 8, 12, 8),

                Margin = new Thickness(0, 0, 0, 6)

            };

            wrapperBorder.Child = rowGrid;

            panel.Children.Add(wrapperBorder);

        }



        private void SimMode_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || panelSimDiceCoin == null || panelSimBoxBall == null) return;
            panelSimDiceCoin.Visibility = rdoSimDiceCoin.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            panelSimBoxBall.Visibility = rdoSimBoxBall.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SimulateBalls_Click(object sender, RoutedEventArgs e)
        {
            resultPanel.Children.Clear();

            // 1. Parse and validate inputs
            if (!int.TryParse(txtRedBalls?.Text?.Trim(), out int red) || red < 1)
            { UI.ResultRow("⚠️ Nhập số bi Đỏ hợp lệ (R ≥ 1)", "#C62828", resultPanel); return; }

            if (!int.TryParse(txtBlueBalls?.Text?.Trim(), out int blue) || blue < 1)
            { UI.ResultRow("⚠️ Nhập số bi Xanh hợp lệ (B ≥ 1)", "#C62828", resultPanel); return; }

            int totalBalls = red + blue;
            if (!int.TryParse(txtDraws?.Text?.Trim(), out int draws) || draws < 1 || draws > totalBalls)
            { UI.ResultRow($"⚠️ Số lần rút d phải hợp lệ (1 ≤ d ≤ {totalBalls})", "#C62828", resultPanel); return; }

            if (!int.TryParse(txtBallSimTrials?.Text?.Trim(), out int trials) || trials < 1 || trials > 100000)
            { UI.ResultRow("⚠️ Nhập số lượt thực nghiệm (1 - 100,000)", "#C62828", resultPanel); return; }

            bool isWithReplacement = rdoWithReplacement.IsChecked == true;

            // 2. Perform simulation
            int[] results = new int[draws + 1]; // Index represents the number of Red balls drawn (0 to d)
            
            if (isWithReplacement)
            {
                // Rút có hoàn lại: Xác suất rút bi đỏ mỗi lần luôn là p = Red / Total
                double pRed = (double)red / totalBalls;
                for (int t = 0; t < trials; t++)
                {
                    int redDrawn = 0;
                    for (int d = 0; d < draws; d++)
                    {
                        if (_rng.NextDouble() < pRed)
                            redDrawn++;
                    }
                    results[redDrawn]++;
                }
            }
            else
            {
                // Rút không hoàn lại: Tạo hộp bi ảo
                int[] box = new int[totalBalls];
                for (int i = 0; i < red; i++) box[i] = 1; // 1 represents Red
                for (int i = red; i < totalBalls; i++) box[i] = 0; // 0 represents Blue

                for (int t = 0; t < trials; t++)
                {
                    // Trộn bi bằng Fisher-Yates shuffle
                    for (int i = totalBalls - 1; i > 0; i--)
                    {
                        int j = _rng.Next(i + 1);
                        int temp = box[i];
                        box[i] = box[j];
                        box[j] = temp;
                    }

                    // Rút d bi đầu tiên
                    int redDrawn = 0;
                    for (int d = 0; d < draws; d++)
                    {
                        if (box[d] == 1)
                            redDrawn++;
                    }
                    results[redDrawn]++;
                }
            }

            // 3. Render results
            string methodStr = isWithReplacement ? "CÓ HOÀN LẠI (Bernoulli)" : "KHÔNG HOÀN LẠI (Siêu bội)";
            UI.ResultRow($"🔴🔵 MÔ PHỎNG HỘP BI — {methodStr}", "#0D47A1", resultPanel);
            UI.ResultRow($"📦 Hộp gồm: {red} Đỏ, {blue} Xanh (Tổng: {totalBalls} bi). Rút {draws} bi.", "#424242", resultPanel);
            UI.ResultRow($"📊 Chạy thực nghiệm {trials:N0} lượt:", "#424242", resultPanel);

            for (int k = 0; k <= draws; k++)
            {
                int count = results[k];
                double freq = (double)count / trials;

                // Tính xác suất lý thuyết tương ứng
                double theoretical = 0;
                if (isWithReplacement)
                {
                    // Phân phối nhị thức Bernoulli
                    double pRed = (double)red / totalBalls;
                    double cnk = (double)(Factorial(draws) / (Factorial(k) * Factorial(draws - k)));
                    theoretical = cnk * System.Math.Pow(pRed, k) * System.Math.Pow(1 - pRed, draws - k);
                }
                else
                {
                    // Phân phối Siêu bội (Hypergeometric)
                    if (k <= red && (draws - k) <= blue)
                    {
                        BigInteger c_red_k = Factorial(red) / (Factorial(k) * Factorial(red - k));
                        BigInteger c_blue_dk = Factorial(blue) / (Factorial(draws - k) * Factorial(blue - (draws - k)));
                        BigInteger c_total_d = Factorial(totalBalls) / (Factorial(draws) * Factorial(totalBalls - draws));
                        theoretical = (double)(c_red_k * c_blue_dk) / (double)c_total_d;
                    }
                }

                string label = $"Có {k} bi Đỏ (Lý thuyết: {(theoretical * 100):F2}%)";
                AddSimulationBar(label, count, freq, k % 2 == 0 ? "#E53935" : "#1E88E5", resultPanel);
            }
        }

        private void Simulate_Click(object sender, RoutedEventArgs e)

        {

            resultPanel.Children.Clear();



            if (!int.TryParse(txtSimTrials?.Text?.Trim(), out int trials) || trials < 1 || trials > 100000)

            { UI.ResultRow("⚠️ Nhập số lần thử (1 - 100,000)", "#C62828", resultPanel); return; }



            // Simulate dice (1-6)

            var diceResults = new int[6];

            for (int i = 0; i < trials; i++)

                diceResults[_rng.Next(6)]++;



            UI.ResultRow($"🎲 MÔ PHỎNG GIEO XÚC XẮC ({trials:N0} lần)", "#0D47A1", resultPanel);

            UI.ResultRow("── Kết quả ──", "#424242", resultPanel);

            for (int face = 0; face < 6; face++)

            {

                int count = diceResults[face];

                double freq = (double)count / trials;

                AddSimulationBar($"  Mặt {face + 1}", count, freq, "#1565C0", resultPanel);

            }



            UI.ResultRow($"📊 Lý thuyết: P(mỗi mặt) = 1/6 ≈ {(100.0 / 6):F2}%", "#2E7D32", resultPanel);

            if (trials >= 30)

            {

                double chiSq = 0;

                for (int i = 0; i < 6; i++)

                {

                    double expected = trials / 6.0;

                    chiSq += (diceResults[i] - expected) * (diceResults[i] - expected) / expected;

                }

                UI.ResultRow($"📏 χ² = {UI.Fmt(chiSq)} ({(chiSq < 11.07 ? "✅ Phân bố đều" : "⚠️ Có thể không đều")})", "#7B1FA2", resultPanel);

            }

            else

            {

                UI.ResultRow("⚠️ Số lần thử < 30: Không đủ kích thước mẫu tối thiểu để chạy kiểm định χ²", "#757575", resultPanel);

            }



            // Simulate coin

            UI.ResultRow("", "#FFFFFF", resultPanel);

            int heads = 0;

            for (int i = 0; i < trials; i++)

                if (_rng.Next(2) == 0) heads++;

            int tails = trials - heads;



            UI.ResultRow($"🪙 MÔ PHỎNG TUNG ĐỒNG XU ({trials:N0} lần)", "#E65100", resultPanel);

            AddSimulationBar("  Mặt Sấp (Heads)", heads, (double)heads / trials, "#1565C0", resultPanel);

            AddSimulationBar("  Mặt Ngửa (Tails)", tails, (double)tails / trials, "#C62828", resultPanel);

            UI.ResultRow($"📊 Lý thuyết: P(Sấp) = P(Ngửa) = 50%", "#2E7D32", resultPanel);

            UI.ResultRow($"💡 Quy luật số lớn: Càng nhiều lần thử → Càng gần 50%", "#757575", resultPanel);

        }



        // ═══ Graph ═══

        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)

        {

            try

            {

                var ci = CultureInfo.InvariantCulture;



                if (_mode == Mode.Bernoulli)

                {

                    if (!int.TryParse(txtBernN?.Text?.Trim(), out int n) || n < 0) return;

                    if (!ParsingHelper.TryParseDouble(txtBernP?.Text?.Trim(), out double p)) p = 0.5;



                    // Tính toán đỉnh phân bố thực tế (mode) để định dạng độ cao trục y động

                    int modeK = (int)System.Math.Floor((n + 1) * p);

                    if (modeK > n) modeK = n;

                    if (modeK < 0) modeK = 0;

                    double peakProb = (double)(Factorial(n) / (Factorial(modeK) * Factorial(n - modeK))) * System.Math.Pow(p, modeK) * System.Math.Pow(1 - p, n - modeK);

                    double topY = System.Math.Max(0.1, peakProb * 1.2);

                    double bottomY = -topY * 0.1;



                    var xList = string.Join(",", Enumerable.Range(0, n + 1));

                    var yList = string.Join(",", Enumerable.Range(0, n + 1).Select(k =>

                    {

                        double ck = (double)(Factorial(n) / (Factorial(k) * Factorial(n - k)));

                        return (ck * System.Math.Pow(p, k) * System.Math.Pow(1 - p, n - k)).ToString(ci);

                    }));



                    string expJs = $@"
            calc.setExpression({{id:'pts', latex:'([{xList}],[{yList}])', color:'#1B5E20', pointSize:10, pointStyle:'POINT'}});
            calc.setExpression({{id:'mu', latex:'x={(n * p).ToString(ci)}', color:'#C62828', lineWidth:2, lineStyle:'DASHED', label:'μ={UI.Fmt(n * p)}', showLabel:true}});
            calc.setMathBounds({{ left: -1, right: {n + 1}, bottom: {bottomY.ToString(ci)}, top: {topY.ToString(ci)} }});";

                    var win = new GraphWindow(expJs, $"🎲 Bernoulli B({n},{UI.Fmt(p)})", $"Đồ thị phân bố Bernoulli với n = {n}, p = {UI.Fmt(p)}");
                    win.Owner = Window.GetWindow(this);

                    win.Show();

                }

                else
                {
                    // Simple: P(A) bar chart
                    if (!ParsingHelper.TryParseDouble(txtFavorable?.Text?.Trim(), out double fav)) return;
                    if (!ParsingHelper.TryParseDouble(txtTotal?.Text?.Trim(), out double total) || total <= 0) return;
                    double pA = fav / total;

                    string expJs = $@"
            calc.setExpression({{id:'bar1', latex:'(1,{pA.ToString(ci)})', color:'#1B5E20', pointSize:20, pointStyle:'POINT', label:'P(A)={UI.Fmt(pA)}', showLabel:true}});
            calc.setExpression({{id:'line1', latex:'\\operatorname{{polygon}}((1,0),(1,{pA.ToString(ci)}),(1,0))', color:'#1B5E20', lineWidth:4}});
            calc.setExpression({{id:'bar2', latex:'(2,{(1 - pA).ToString(ci)})', color:'#C62828', pointSize:20, pointStyle:'POINT', label:'P(Ā)={UI.Fmt(1 - pA)}', showLabel:true}});
            calc.setExpression({{id:'line2', latex:'\\operatorname{{polygon}}((2,0),(2,{(1 - pA).ToString(ci)}),(2,0))', color:'#C62828', lineWidth:4}});
            calc.setMathBounds({{ left: 0, right: 3, bottom: -0.1, top: 1.1 }});";



                    var win = new GraphWindow(expJs, $"🎲 P(A) = {UI.Fmt(pA)}");

                    win.Owner = Window.GetWindow(this);

                    win.Show();

                }

            }

            catch (Exception ex) { MessageBox.Show(ex.Message); }

        }



        // ═══ PRESETS ═══

        private void BuildPresets()

        {

            if (presetPanel == null) return;

            var presets = new (string L, Action Apply)[]

            {

                ("🎲 1 xúc xắc chẵn", () => { _mode = Mode.Classic; txtFavorable.Text = "3"; txtTotal.Text = "6"; }),

                ("🎲 2 xúc xắc tổng 7", () => { _mode = Mode.Classic; txtFavorable.Text = "6"; txtTotal.Text = "36"; }),

                ("🃏 Rút Át Bích", () => { _mode = Mode.Classic; txtFavorable.Text = "1"; txtTotal.Text = "52"; }),

                ("🃏 Rút Quân đỏ", () => { _mode = Mode.Classic; txtFavorable.Text = "26"; txtTotal.Text = "52"; }),

                ("🪙 5 xu, 3 sấp", () => { _mode = Mode.Bernoulli; txtBernN.Text = "5"; txtBernK.Text = "3"; txtBernP.Text = "0.5"; }),

                ("📝 10 câu TN, 7 đúng", () => { _mode = Mode.Bernoulli; txtBernN.Text = "10"; txtBernK.Text = "7"; txtBernP.Text = "0.25"; }),

                ("🏭 KCS 5/100", () => { _mode = Mode.Bernoulli; txtBernN.Text = "100"; txtBernK.Text = "5"; txtBernP.Text = "0.02"; }),

            };



            var presetColor = (Color)ColorConverter.ConvertFromString("#1B5E20");

            foreach (var (l, apply) in presets)

            {

                var capturedApply = apply;

                UI.PresetButton(l, presetColor, () =>

                {

                    capturedApply();

                    BuildModeButtons();

                    UpdateUI();

                    Calc();

                }, presetPanel);

            }

        }



        // ═══ HELPERS ═══

        private static BigInteger Factorial(int n)
        {
            if (n < 0 || n > 170) return 1;
            return FactCache[n];
        }



        private static (long num, long den) SimplifyFraction(long a, long b)

        {

            if (b == 0) return (a, b);

            long g = GCD(System.Math.Abs(a), System.Math.Abs(b));

            return (a / g, b / g);

        }



        private static long GCD(long a, long b) => b == 0 ? a : GCD(b, a % b);

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mode != Mode.Bernoulli) return;
                if (!int.TryParse(txtBernN?.Text?.Trim(), out int n) || n < 0 || n > 170) return;
                if (!ParsingHelper.TryParseDouble(txtBernP?.Text?.Trim(), out double p) || p < 0 || p > 1) return;

                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV File (*.csv)|*.csv",
                    FileName = $"PhanPhoi_Bernoulli_n{n}_p{p.ToString(System.Globalization.CultureInfo.InvariantCulture)}.csv",
                    Title = "Xuất bảng phân phối Bernoulli ra CSV"
                };

                if (sfd.ShowDialog() == true)
                {
                    using (var writer = new System.IO.StreamWriter(sfd.FileName, false, new System.Text.UTF8Encoding(true)))
                    {
                        writer.WriteLine("Giá trị k,Công thức tính P(X=k),Xác suất P(X=k) (Thập phân),Xác suất P(X=k) (%)");

                        for (int k = 0; k <= n; k++)
                        {
                            BigInteger cnk_big = Factorial(n) / (Factorial(k) * Factorial(n - k));
                            double cnk = (double)cnk_big;
                            double prob = cnk * System.Math.Pow(p, k) * System.Math.Pow(1 - p, n - k);
                            
                            string formula = $"C({n};{k}) * {UI.Fmt(p)}^{k} * {UI.Fmt(1 - p)}^{n - k}";
                            string probDecimal = prob.ToString("F8", System.Globalization.CultureInfo.InvariantCulture);
                            string probPercent = $"{(prob * 100):F4}%";

                            writer.WriteLine($"{k},{formula},{probDecimal},{probPercent}");
                        }
                    }
                    MessageBox.Show("Xuất tệp CSV thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất CSV: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Fmt(), AddR() đã được thay thế bằng UI.Fmt(), UI.ResultRow()

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootGrid == null || sideMenu == null || contentCalc == null || viewPractical == null)
                return;

            contentCalc.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            int index = sideMenu.SelectedIndex;
            if (index == 0)
            {
                contentCalc.Visibility = Visibility.Visible;
            }
            else if (index == 1)
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
            if (tag == "calc" || tag == "practice")
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
                        Icon = "🧬",
                        Title = isVN ? "Quy luật phân ly di truyền Mendel" : "Mendel's Laws of Inheritance",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_probability_1_{suffix}.png",
                        Description = isVN 
                            ? "Xác suất được ứng dụng trong sinh học để dự đoán tỉ lệ kiểu hình ở đời con. Ví dụ: Khi lai hai cây đậu Hà Lan dị hợp tử (Aa x Aa), xác suất đời con mang kiểu hình trội (AA hoặc Aa) là 3/4 (75%), mang kiểu hình lặn (aa) là 1/4 (25%). Đây là nền tảng của di truyền học cổ điển." 
                            : "Probability is used in genetics to predict offspring phenotype ratios. E.g., when crossing two heterozygous pea plants (Aa x Aa), the probability of the offspring showing the dominant trait (AA or Aa) is 3/4 (75%), and the recessive trait (aa) is 1/4 (25%)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏭",
                        Title = isVN ? "Kiểm soát chất lượng sản xuất" : "Manufacturing Quality Control",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_probability_2_{suffix}.png",
                        Description = isVN 
                            ? "Công thức Bernoulli giúp các kỹ sư tính xác suất xuất hiện sản phẩm lỗi trong một lô hàng. Ví dụ: Nếu tỉ lệ lỗi của dây chuyền là 2%, xác suất để chọn ngẫu nhiên 5 sản phẩm mà không có sản phẩm nào bị lỗi được tính bằng Bernoulli: C(5,0) * 0.02^0 * 0.98^5 ≈ 90.39%." 
                            : "Bernoulli trials help quality engineers calculate defect rates in batches. For instance, if a production line has a 2% defect rate, the probability of selecting 5 random items with zero defects is C(5,0) * 0.02^0 * 0.98^5 ≈ 90.39%."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏦",
                        Title = isVN ? "Định giá bảo hiểm và Đánh giá rủi ro" : "Insurance Underwriting & Risk",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_probability_3_{suffix}.png",
                        Description = isVN 
                            ? "Các công ty bảo hiểm sử dụng thống kê và xác suất thực nghiệm để tính tỷ lệ rủi ro xảy ra tai nạn hoặc bệnh tật trong một nhóm khách hàng. Từ đó, họ định giá phí bảo hiểm (premium) sao cho doanh thu đủ bù đắp chi phí bồi thường và có lợi nhuận." 
                            : "Actuaries use experimental probability and large sample statistics to estimate accident or illness rates across demographics. This allows insurance companies to price policy premiums to cover payouts while maintaining profitability."
                    }
                };

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for ProbabilityTool: {Err}", ex.Message);
            }
        }
    }
}



