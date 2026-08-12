using QASmartClass.LearningTools.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartClass.LearningTools.Controls;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Color = System.Windows.Media.Color;




namespace QASmartClass.LearningTools.Views.Math
{
    public partial class FractionTool : BaseToolControl
    {
        private enum Mode { Operations, Simplify, Compare, MixedNumber }
        private Mode _mode = Mode.Operations;
        private GraphWindow _graphWindow;

        public class FractionQuestion
        {
            public string QuestionText { get; set; } = "";
            public int ExpectedNum { get; set; }
            public int ExpectedDen { get; set; }
            public string ExpectedCompareSymbol { get; set; } = "";
            public string DetailedSolution { get; set; } = "";
            public bool IsComparison { get; set; }
        }

        private FractionQuestion _currentQuestion;
        private int _quizScore = 0;
        private int _quizStreak = 0;
        private int _quizHighScore = 0;
        private int _targetColorN = 0;
        private int _targetColorD = 1;
        private bool[] _coloringState = Array.Empty<bool>();
        private static readonly Random Rnd = new Random();

        private static readonly Color Accent = Color.FromRgb(230, 81, 0); // Orange 900


        public FractionTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextLearning != null) menuTextLearning.Text = isVN ? "Công cụ học tập" : "Learning Tool";
                if (menuTextPlay != null) menuTextPlay.Text = isVN ? "Trò chơi & Thực hành" : "Game & Practice";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                BuildModeButtons();
                BuildPresets();
                BuildFormulas();
                LoadPracticalApps();
                Calc();

                bool isPrimary = IsPrimaryLevel();
                int minVal = isPrimary ? 0 : -100;

                TouchNumPad.Attach(txtN1, step: 1, min: minVal);
                TouchNumPad.Attach(txtD1, step: 1, min: 1);
                TouchNumPad.Attach(txtN2, step: 1, min: minVal);
                TouchNumPad.Attach(txtD2, step: 1, min: 1);
                TouchNumPad.Attach(txtSN, step: 1, min: minVal);
                TouchNumPad.Attach(txtSD, step: 1, min: 1);
                TouchNumPad.Attach(txtQuizAnsN, step: 1, min: minVal);
                TouchNumPad.Attach(txtQuizAnsD, step: 1, min: 1);
                
                LoadHighScore();
            };
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewLearning == null || viewPlay == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewLearning.Visibility = Visibility.Collapsed;
            viewPlay.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewLearning.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPlay.Visibility = Visibility.Visible;
                    break;
                case 3:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }

        // ═══ MODE SELECTOR ═══
        private void BuildModeButtons()
        {
            modePanel.Children.Clear();
            var modes = new (string Label, Mode M)[]
            {
                ("➕ Phép tính (+−×÷)", Mode.Operations),
                ("✂️ Rút gọn & Quy đồng", Mode.Simplify),
                ("⚖️ So sánh phân số", Mode.Compare),
                ("🔄 Hỗn số ↔ Phân số", Mode.MixedNumber),
            };
            foreach (var (label, m) in modes)
            {
                bool active = m == _mode;
                var btn = MakeChip(label, active);
                var cm = m;
                btn.MouseLeftButtonDown += (_, _) => { _mode = cm; BuildModeButtons(); BuildPresets(); UpdateUI(); Calc(); };
                modePanel.Children.Add(btn);
            }
        }

        private void UpdateUI()
        {
            bool useTwoFracs = _mode == Mode.Operations || _mode == Mode.Compare;
            twoFracInput.Visibility = useTwoFracs ? Visibility.Visible : Visibility.Collapsed;
            singleFracInput.Visibility = useTwoFracs ? Visibility.Collapsed : Visibility.Visible;
            txtModeTitle.Text = _mode switch
            {
                Mode.Operations => "➕ Phép tính — cộng, trừ, nhân, chia phân số",
                Mode.Simplify => "✂️ Rút gọn — tối giản & quy đồng mẫu",
                Mode.Compare => "⚖️ So sánh — lớn hơn, nhỏ hơn, bằng nhau",
                Mode.MixedNumber => "🔄 Hỗn số — đổi qua lại phân số ↔ hỗn số",
                _ => ""
            };
        }

        // ═══ PRESETS ═══
        private void BuildPresets()
        {
            presetPanel.Children.Clear();
            switch (_mode)
            {
                case Mode.Operations:
                    AddPresetTwo("1/2 + 1/3", 1, 2, 1, 3);
                    AddPresetTwo("3/4 − 1/-2", 3, 4, 1, -2);
                    AddPresetTwo("2/5 × -3/4", 2, 5, -3, 4);
                    AddPresetTwo("-4/5 ÷ 2/3", -4, 5, 2, 3);
                    AddPresetTwo("5/6 + 1/6", 5, 6, 1, 6);
                    break;
                case Mode.Simplify:
                    AddPresetSingle("12/8", 12, 8);
                    AddPresetSingle("-15/25", -15, 25);
                    AddPresetSingle("24/6", 24, 6);
                    AddPresetSingle("100/-75", 100, -75);
                    break;
                case Mode.Compare:
                    AddPresetTwo("1/3 và 1/4", 1, 3, 1, 4);
                    AddPresetTwo("-1/2 và -2/3", -1, 2, -2, 3);
                    AddPresetTwo("2/3 và 4/6", 2, 3, 4, 6);
                    AddPresetTwo("-3/5 và 3/-5", -3, 5, 3, -5);
                    break;
                case Mode.MixedNumber:
                    AddPresetSingle("7/3", 7, 3);
                    AddPresetSingle("-11/4", -11, 4);
                    AddPresetSingle("15/5", 15, 5);
                    AddPresetSingle("3/5", 3, 5);
                    break;
            }
        }

        private void AddPresetTwo(string label, int n1, int d1, int n2, int d2)
        {
            var btn = MakeChip(label, false);
            btn.MouseLeftButtonDown += (_, _) =>
            {
                txtN1.Text = $"{n1}"; txtD1.Text = $"{d1}";
                txtN2.Text = $"{n2}"; txtD2.Text = $"{d2}";
            };
            presetPanel.Children.Add(btn);
        }

        private void AddPresetSingle(string label, int n, int d)
        {
            var btn = MakeChip(label, false);
            btn.MouseLeftButtonDown += (_, _) =>
            {
                txtSN.Text = $"{n}"; txtSD.Text = $"{d}";
            };
            presetPanel.Children.Add(btn);
        }

        private void Input_Changed(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) Calc();
        }

        // ═══ CALCULATE ═══
        private void Calc()
        {
            resultPanel.Children.Clear();
            visualPanel.Children.Clear();
            switch (_mode)
            {
                case Mode.Operations: CalcOps(); break;
                case Mode.Simplify: CalcSimplify(); break;
                case Mode.Compare: CalcCompare(); break;
                case Mode.MixedNumber: CalcMixed(); break;
            }
        }

        private void CalcOps()
        {
            if (string.IsNullOrWhiteSpace(txtN1?.Text) || string.IsNullOrWhiteSpace(txtD1?.Text) ||
                string.IsNullOrWhiteSpace(txtN2?.Text) || string.IsNullOrWhiteSpace(txtD2?.Text))
            {
                UI.ResultRow("⚠️ Vui lòng nhập đầy đủ tử số và mẫu số cho hai phân số.", "#C62828", resultPanel);
                return;
            }

            if (!int.TryParse(txtN1?.Text, out int n1) || !int.TryParse(txtD1?.Text, out int d1) ||
                !int.TryParse(txtN2?.Text, out int n2) || !int.TryParse(txtD2?.Text, out int d2))
            {
                UI.ResultRow("⚠️ Lỗi định dạng: Vui lòng chỉ nhập số nguyên.", "#C62828", resultPanel);
                return;
            }

            if (d1 == 0 || d2 == 0)
            {
                UI.ResultRow("⚠️ Mẫu số phải khác 0.", "#C62828", resultPanel);
                return;
            }

            NormalizeFraction(ref n1, ref d1);
            NormalizeFraction(ref n2, ref d2);

            UI.ResultRow($"Phân số thứ nhất (chuẩn hóa) = {FracStr(n1, d1)}", "#1565C0", resultPanel);
            UI.ResultRow($"Phân số thứ hai (chuẩn hóa) = {FracStr(n2, d2)}", "#1565C0", resultPanel);

            // Addition
            long addN = (long)n1 * d2 + (long)n2 * d1;
            long addD = (long)d1 * d2;
            long g1 = Gcd((int)System.Math.Abs(addN), (int)System.Math.Abs(addD));
            long addN_simp = addN / g1;
            long addD_simp = addD / g1;

            string addSteps = "";
            if (d1 == d2)
            {
                addSteps = $"  Cùng mẫu số: {FracStr(n1, d1)} + {FracStr(n2, d2)} = ({n1} + {n2})/{d1} = {FracStr((int)(n1 + n2), d1)}";
                if (g1 > 1)
                {
                    addSteps += $" = {FracStr((int)addN_simp, (int)addD_simp)} (Rút gọn chia cả tử và mẫu cho {g1})";
                }
            }
            else
            {
                int msc = Lcm(d1, d2);
                int f1 = msc / d1;
                int f2 = msc / d2;
                addSteps = $"  • Quy đồng mẫu số (MSC = {msc}):\n" +
                           $"    {FracStr(n1, d1)} = ({n1}×{f1})/({d1}×{f1}) = {FracStr(n1 * f1, msc)}\n" +
                           $"    {FracStr(n2, d2)} = ({n2}×{f2})/({d2}×{f2}) = {FracStr(n2 * f2, msc)}\n" +
                           $"  • Cộng tử số, giữ nguyên mẫu số chung:\n" +
                           $"    {FracStr(n1 * f1, msc)} + {FracStr(n2 * f2, msc)} = ({n1 * f1} + {n2 * f2})/{msc} = {FracStr((int)addN, (int)addD)}";
                if (g1 > 1)
                {
                    addSteps += $" = {FracStr((int)addN_simp, (int)addD_simp)} (Rút gọn chia cả tử và mẫu cho {g1})";
                }
            }
            UI.ResultRow($"➕ Phép cộng:\n{addSteps}", "#1565C0", resultPanel);

            // Subtraction
            long subN = (long)n1 * d2 - (long)n2 * d1;
            long subD = (long)d1 * d2;
            long g2 = Gcd((int)System.Math.Abs(subN), (int)System.Math.Abs(subD));
            long subN_simp = subN / g2;
            long subD_simp = subD / g2;

            string subSteps = "";
            if (d1 == d2)
            {
                subSteps = $"  Cùng mẫu số: {FracStr(n1, d1)} − {FracStr(n2, d2)} = ({n1} − {n2})/{d1} = {FracStr((int)(n1 - n2), d1)}";
                if (g2 > 1)
                {
                    subSteps += $" = {FracStr((int)subN_simp, (int)subD_simp)} (Rút gọn chia cả tử và mẫu cho {g2})";
                }
            }
            else
            {
                int msc = Lcm(d1, d2);
                int f1 = msc / d1;
                int f2 = msc / d2;
                subSteps = $"  • Quy đồng mẫu số (MSC = {msc}):\n" +
                           $"    {FracStr(n1, d1)} = ({n1}×{f1})/({d1}×{f1}) = {FracStr(n1 * f1, msc)}\n" +
                           $"    {FracStr(n2, d2)} = ({n2}×{f2})/({d2}×{f2}) = {FracStr(n2 * f2, msc)}\n" +
                           $"  • Trừ tử số, giữ nguyên mẫu số chung:\n" +
                           $"    {FracStr(n1 * f1, msc)} − {FracStr(n2 * f2, msc)} = ({n1 * f1} − {n2 * f2})/{msc} = {FracStr((int)subN, (int)subD)}";
                if (g2 > 1)
                {
                    subSteps += $" = {FracStr((int)subN_simp, (int)subD_simp)} (Rút gọn chia cả tử và mẫu cho {g2})";
                }
            }
            UI.ResultRow($"➖ Phép trừ:\n{subSteps}", "#1565C0", resultPanel);

            // Multiplication
            long mulN = (long)n1 * n2;
            long mulD = (long)d1 * d2;
            long g3 = Gcd((int)System.Math.Abs(mulN), (int)System.Math.Abs(mulD));
            long mulN_simp = mulN / g3;
            long mulD_simp = mulD / g3;

            string mulSteps = $"  • Nhân tử với tử, mẫu với mẫu:\n" +
                              $"    {FracStr(n1, d1)} × {FracStr(n2, d2)} = ({n1}×{n2})/({d1}×{d2}) = {FracStr((int)mulN, (int)mulD)}";
            if (g3 > 1)
            {
                mulSteps += $" = {FracStr((int)mulN_simp, (int)mulD_simp)} (Rút gọn chia cả tử và mẫu cho {g3})";
            }
            UI.ResultRow($"✖️ Phép nhân:\n{mulSteps}", "#1565C0", resultPanel);

            // Division
            if (n2 == 0)
            {
                UI.ResultRow("➗ Phép chia:\n  ⚠️ Không thể chia cho 0 (phân số thứ hai bằng 0).", "#C62828", resultPanel);
            }
            else
            {
                long divN = (long)n1 * d2;
                long divD = (long)d1 * n2;
                if (divD < 0) { divN = -divN; divD = -divD; }
                long g4 = Gcd((int)System.Math.Abs(divN), (int)System.Math.Abs(divD));
                long divN_simp = divN / g4;
                long divD_simp = divD / g4;

                string divSteps = $"  • Nhân nghịch đảo phân số thứ hai:\n" +
                                  $"    {FracStr(n1, d1)} ÷ {FracStr(n2, d2)} = {FracStr(n1, d1)} × {FracStr(d2, n2)} = ({n1}×{d2})/({d1}×{n2}) = {FracStr((int)(n1 * d2), (int)(d1 * n2))}";
                if (d1 * n2 < 0)
                {
                    divSteps += $" = {FracStr((int)divN, (int)divD)} (Chuẩn hóa mẫu số dương)";
                }
                if (g4 > 1)
                {
                    divSteps += $" = {FracStr((int)divN_simp, (int)divD_simp)} (Rút gọn chia cả tử và mẫu cho {g4})";
                }
                UI.ResultRow($"➗ Phép chia:\n{divSteps}", "#1565C0", resultPanel);
            }

            // Decimal
            UI.ResultRow($"📊 Giá trị thập phân:\n  Phân số 1 = {(double)n1 / d1:G6}\n  Phân số 2 = {(double)n2 / d2:G6}", "#1565C0", resultPanel);

            // Visual bars
            DrawFractionBar(n1, d1, "Phân số 1");
            DrawFractionBar(n2, d2, "Phân số 2");
        }

        private void CalcSimplify()
        {
            if (string.IsNullOrWhiteSpace(txtSN?.Text) || string.IsNullOrWhiteSpace(txtSD?.Text))
            {
                UI.ResultRow("⚠️ Vui lòng nhập đầy đủ tử số và mẫu số.", "#C62828", resultPanel);
                return;
            }

            if (!int.TryParse(txtSN?.Text, out int n) || !int.TryParse(txtSD?.Text, out int d))
            {
                UI.ResultRow("⚠️ Lỗi định dạng: Vui lòng chỉ nhập số nguyên.", "#C62828", resultPanel);
                return;
            }

            if (d == 0)
            {
                UI.ResultRow("⚠️ Mẫu số phải khác 0.", "#C62828", resultPanel);
                return;
            }

            NormalizeFraction(ref n, ref d);

            UI.ResultRow($"Phân số gốc = {FracStr(n, d)}", "#1565C0", resultPanel);

            int g = Gcd(System.Math.Abs(n), System.Math.Abs(d));
            int sn = n / g, sd = d / g;

            string simpSteps = "";
            if (g == 1)
            {
                simpSteps = $"  Phân số {FracStr(n, d)} đã tối giản vì ƯCLN({System.Math.Abs(n)}, {System.Math.Abs(d)}) = 1.";
            }
            else
            {
                simpSteps = $"  Chia cả tử và mẫu cho ƯCLN = {g}:\n" +
                            $"  {n}/{d} = ({n}÷{g})/({d}÷{g}) = {FracStr(sn, sd)}";
            }
            UI.ResultRow($"✂️ Rút gọn phân số:\n{simpSteps}", g > 1 ? "#E65100" : "#1565C0", resultPanel);

            // Decimal & Percentage
            UI.ResultRow($"📊 Thập phân = {(double)n / d:G8}\n📊 Phần trăm = {(double)n / d * 100:G6}%", "#1565C0", resultPanel);

            // Quy đồng with some common denominators
            int lcm10 = Lcm(System.Math.Abs(d), 10);
            int lcm12 = Lcm(System.Math.Abs(d), 12);

            if (sd != 1)
            {
                int f10 = lcm10 / d;
                int f12 = lcm12 / d;
                string qdSteps = $"  • Quy đồng mẫu 10 (MSC = {lcm10}, nhân tử phụ {f10}):\n" +
                                 $"    {FracStr(n, d)} = ({n}×{f10})/({d}×{f10}) = {FracStr(n * f10, lcm10)}\n" +
                                 $"  • Quy đồng mẫu 12 (MSC = {lcm12}, nhân tử phụ {f12}):\n" +
                                 $"    {FracStr(n, d)} = ({n}×{f12})/({d}×{f12}) = {FracStr(n * f12, lcm12)}";
                UI.ResultRow($"📌 Quy đồng mẫu số:\n{qdSteps}", "#E65100", resultPanel);
            }

            // Visual
            DrawFractionBar(n, d, "Gốc");
            DrawFractionBar(sn, sd, "Tối giản");
        }

        private void CalcCompare()
        {
            if (string.IsNullOrWhiteSpace(txtN1?.Text) || string.IsNullOrWhiteSpace(txtD1?.Text) ||
                string.IsNullOrWhiteSpace(txtN2?.Text) || string.IsNullOrWhiteSpace(txtD2?.Text))
            {
                UI.ResultRow("⚠️ Vui lòng nhập đầy đủ tử số và mẫu số cho hai phân số.", "#C62828", resultPanel);
                return;
            }

            if (!int.TryParse(txtN1?.Text, out int n1) || !int.TryParse(txtD1?.Text, out int d1) ||
                !int.TryParse(txtN2?.Text, out int n2) || !int.TryParse(txtD2?.Text, out int d2))
            {
                UI.ResultRow("⚠️ Lỗi định dạng: Vui lòng chỉ nhập số nguyên.", "#C62828", resultPanel);
                return;
            }

            if (d1 == 0 || d2 == 0)
            {
                UI.ResultRow("⚠️ Mẫu số phải khác 0.", "#C62828", resultPanel);
                return;
            }

            NormalizeFraction(ref n1, ref d1);
            NormalizeFraction(ref n2, ref d2);

            UI.ResultRow($"Phân số 1 (chuẩn hóa) = {FracStr(n1, d1)}", "#1565C0", resultPanel);
            UI.ResultRow($"Phân số 2 (chuẩn hóa) = {FracStr(n2, d2)}", "#1565C0", resultPanel);

            // Cross multiply
            long left = (long)n1 * d2;
            long right = (long)n2 * d1;

            string symbol = left > right ? ">" : left < right ? "<" : "=";
            string word = left > right ? "LỚN hơn" : left < right ? "NHỎ hơn" : "BẰNG nhau";

            // Common denominator
            int lcm = Lcm(System.Math.Abs(d1), System.Math.Abs(d2));
            int f1 = lcm / d1;
            int f2 = lcm / d2;
            int cn1 = n1 * f1;
            int cn2 = n2 * f2;

            string compareSteps = $"  • Cách 1: Quy đồng mẫu số (Mẫu số chung = {lcm}):\n" +
                                   $"    {FracStr(n1, d1)} = ({n1}×{f1})/({d1}×{f1}) = {FracStr(cn1, lcm)}\n" +
                                   $"    {FracStr(n2, d2)} = ({n2}×{f2})/({d2}×{f2}) = {FracStr(cn2, lcm)}\n" +
                                   $"    So sánh tử số: {cn1} {symbol} {cn2}  =>  {FracStr(n1, d1)} {symbol} {FracStr(n2, d2)}\n\n" +
                                   $"  • Cách 2: Nhân chéo tử số với mẫu số đối diện:\n" +
                                   $"    {n1} × {d2} = {left}  so với  {n2} × {d1} = {right}\n" +
                                   $"    So sánh tích: {left} {symbol} {right}  =>  {FracStr(n1, d1)} {symbol} {FracStr(n2, d2)}";

            UI.ResultRow($"⚖️ So sánh phân số:\n{compareSteps}", "#E65100", resultPanel);

            // Decimal
            double v1 = (double)n1 / d1, v2 = (double)n2 / d2;
            UI.ResultRow($"📊 Thập phân: {v1:G6} {symbol} {v2:G6}", "#1565C0", resultPanel);

            // Visual
            DrawFractionBar(n1, d1, "Phân số 1");
            DrawFractionBar(n2, d2, "Phân số 2");
        }

        private void CalcMixed()
        {
            if (string.IsNullOrWhiteSpace(txtSN?.Text) || string.IsNullOrWhiteSpace(txtSD?.Text))
            {
                UI.ResultRow("⚠️ Vui lòng nhập đầy đủ tử số và mẫu số.", "#C62828", resultPanel);
                return;
            }

            if (!int.TryParse(txtSN?.Text, out int n) || !int.TryParse(txtSD?.Text, out int d))
            {
                UI.ResultRow("⚠️ Lỗi định dạng: Vui lòng chỉ nhập số nguyên.", "#C62828", resultPanel);
                return;
            }

            if (d == 0)
            {
                UI.ResultRow("⚠️ Mẫu số phải khác 0.", "#C62828", resultPanel);
                return;
            }

            NormalizeFraction(ref n, ref d);

            UI.ResultRow($"Phân số = {FracStr(n, d)}", "#1565C0", resultPanel);

            int absN = System.Math.Abs(n);
            int whole = absN / d;
            int rem = absN % d;
            string sign = n < 0 ? "−" : "";

            if (whole > 0 && rem > 0)
            {
                string steps = $"  • Bước 1: Chia tử số cho mẫu số:\n" +
                               $"    {absN} ÷ {d} = {whole} (dư {rem})\n" +
                               $"  • Bước 2: Viết hỗn số:\n" +
                               $"    Phần nguyên là thương ({whole}), phần phân số có tử là số dư ({rem}) và mẫu số cũ ({d}).\n" +
                               $"    => Hỗn số = {sign}{whole} {rem}/{d}";
                UI.ResultRow($"🔄 Đổi sang hỗn số:\n{steps}", "#1565C0", resultPanel);

                // Reverse: mixed → improper
                int impN = whole * d + rem;
                string revSteps = $"  • Bước 1: Nhân phần nguyên với mẫu số rồi cộng tử số:\n" +
                                  $"    {whole} × {d} + {rem} = {impN}\n" +
                                  $"  • Bước 2: Giữ nguyên mẫu số:\n" +
                                  $"    => Phân số = {sign}{impN}/{d}";
                UI.ResultRow($"🔁 Đổi hỗn số ngược lại phân số:\n{revSteps}", "#1565C0", resultPanel);
            }
            else if (rem == 0)
            {
                UI.ResultRow($"🔄 Chia hết thành số nguyên = {sign}{whole}", "#E65100", resultPanel);
            }
            else // |n| < d
            {
                int g = Gcd(absN, d);
                UI.ResultRow($"🔄 Phân số thực sự = {FracStr(n / g, d / g)}\n  (|tử| < mẫu số, không đổi sang hỗn số được)", "#1565C0", resultPanel);
            }

            // Decimal
            UI.ResultRow($"📊 Thập phân = {(double)n / d:G8}", "#1565C0", resultPanel);

            // Visual
            DrawFractionBar(n, d, FracStr(n, d));
        }

        // ═══ VISUAL FRACTION BAR ═══
        private void DrawFractionBar(int num, int den, string label)
        {
            if (den <= 0 || den > 20 || num < 0) return; // only draw for reasonable values

            var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            sp.Children.Add(new TextBlock
            {
                Text = $"{label}: {FracStr(num, den)}", FontSize = 14,
                FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Accent),
                Margin = new Thickness(0, 0, 0, 6)
            });

            if (num > 5 * den)
            {
                sp.Children.Add(new TextBlock
                {
                    Text = "⚠️ Phân số quá lớn (> 5 đơn vị), không hỗ trợ vẽ hình trực quan.",
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(4, 0, 0, 0)
                });
                visualPanel.Children.Add(sp);
                return;
            }

            int totalBars = num <= 0 ? 1 : (num + den - 1) / den;
            double totalWidth = 300;
            double partWidth = totalWidth / den;

            int remainingFilled = num;

            for (int barIdx = 0; barIdx < totalBars; barIdx++)
            {
                var barPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
                int filledInThisBar = System.Math.Min(remainingFilled, den);
                remainingFilled -= filledInThisBar;

                for (int i = 0; i < den; i++)
                {
                    bool filled = i < filledInThisBar;
                    var rect = new Border
                    {
                        Width = partWidth - 1,
                        Height = 24,
                        Background = new SolidColorBrush(filled
                            ? Color.FromArgb(180, Accent.R, Accent.G, Accent.B)
                            : Color.FromRgb(238, 238, 238)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(189, 189, 189)),
                        BorderThickness = new Thickness(0.5),
                        CornerRadius = new CornerRadius(
                            i == 0 ? 4 : 0, i == den - 1 ? 4 : 0,
                            i == den - 1 ? 4 : 0, i == 0 ? 4 : 0),
                        Margin = new Thickness(0, 0, 1, 0)
                    };
                    barPanel.Children.Add(rect);
                }
                sp.Children.Add(barPanel);
            }

            visualPanel.Children.Add(sp);
        }

        // ═══ FORMULAS ═══
        private void BuildFormulas()
        {
            formulaPanel.Children.Clear();
            var sections = new (string Title, string[] Items)[]
            {
                ("💡 Hướng dẫn nhanh", new[]
                {
                    "1. Chọn chức năng ở bảng 'Chọn chức năng'.",
                    "2. Nhập Tử số và Mẫu số (hỗ trợ dấu âm '-' cho tử).",
                    "3. Xem các bước giải chi tiết và hình vẽ minh họa ở cột phải.",
                    "4. Nhấn chuột vào thẻ kết quả bất kỳ để copy nhanh.",
                    "5. Bấm 'Xem trên đồ thị' để biểu diễn trên trục số.",
                }),
                ("Phép cộng / trừ", new[]
                {
                    "Cùng mẫu: a/c ± b/c = (a±b)/c",
                    "Khác mẫu: a/b ± c/d = (ad±bc)/(bd)",
                    "Quy đồng mẫu → cộng/trừ tử",
                }),
                ("Phép nhân / chia", new[]
                {
                    "Nhân: a/b × c/d = (ac)/(bd)",
                    "Chia: a/b ÷ c/d = a/b × d/c",
                    "Nghịch đảo: 1/(a/b) = b/a",
                }),
                ("Rút gọn & So sánh", new[]
                {
                    "Rút gọn: chia tử và mẫu cho ƯCLN",
                    "So sánh: quy đồng hoặc tích chéo",
                    "a/b so với c/d: so sánh a·d với c·b",
                }),
                ("Hỗn số", new[]
                {
                    "PS → Hỗn số: chia tử cho mẫu",
                    "Hỗn số → PS: a(b/c) = (ac+b)/c",
                    "Phân số thực sự: |tử| < mẫu",
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
                var ssp = new StackPanel();
                ssp.Children.Add(new TextBlock
                {
                    Text = title, FontSize = 12, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Accent), Margin = new Thickness(0, 0, 0, 4)
                });
                foreach (var item in items)
                    ssp.Children.Add(new TextBlock
                    {
                        Text = $"  {item}", FontSize = 12,
                        FontFamily = new FontFamily("Segoe UI"),
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                        Margin = new Thickness(0, 1, 0, 1)
                    });
                section.Child = ssp;
                formulaPanel.Children.Add(section);
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
                        Icon = "🍕",
                        Title = isVN ? "Chia sẻ thức ăn" : "Culinary Recipe Scaling",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fraction_1_{suffix}.png",
                        Description = isVN 
                            ? "Phân số biểu diễn việc chia một chiếc pizza hoặc bánh ngọt thành các phần bằng nhau." 
                            : "Adjust cooking ingredient portions up or down by multiplying fraction ratios.",
                        Formula = isVN ? "Phần thức ăn nhận được = Số phần ăn lấy đi / Tổng số phần bằng nhau" : "Portion = Parts Taken / Total Equal Parts",
                        MathAnalysis = isVN 
                            ? "Một chiếc bánh pizza được cắt thành 8 phần bằng nhau. Nếu em ăn 3 phần, em đã ăn 3/8 chiếc bánh. Phần bánh còn lại là 5/8 chiếc bánh.\nPhép cộng: 3/8 (phần đã ăn) + 5/8 (phần còn lại) = 8/8 = 1 chiếc bánh nguyên vẹn."
                            : "A pizza is cut into 8 equal slices. Eating 3 slices means you consumed 3/8 of the pizza. The remaining fraction is 5/8.",
                        DiscussionQuestion = isVN
                            ? "Nếu cắt chiếc bánh làm 4 phần và ăn 2 phần, so với cắt làm 8 phần và ăn 4 phần thì lượng bánh ăn được có bằng nhau không? Hãy liên hệ với bài học phân số bằng nhau."
                            : "Is 2/4 of a cake equal to 4/8 of the same cake? Explain using equivalent fractions.",
                        InteractiveTask = isVN
                            ? "Thực hành chia một chiếc bánh hình tròn giấy thành 6 phần bằng nhau. Hãy tô màu đỏ vào 2 phần, và màu xanh vào 3 phần. Viết các phân số tương ứng biểu diễn phần tô màu."
                            : "Divide a paper circle into 6 equal slices. Color 2 slices red and 3 slices green. Write down the corresponding fractions."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🍳",
                        Title = isVN ? "Đo lường nguyên liệu nấu ăn" : "Culinary Portions",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fraction_2_{suffix}.png",
                        Description = isVN 
                            ? "Các công thức nấu ăn thường dùng phân số để đong đếm nguyên liệu chính xác." 
                            : "Recipes use fractions for measuring ingredients precisely.",
                        Formula = isVN ? "Nguyên liệu cần dùng = Định lượng chuẩn × Tỷ lệ tăng/giảm công thức" : "Required Ingredient = Base Amount × Scale Ratio",
                        MathAnalysis = isVN
                            ? "Công thức chuẩn cần dùng 3/4 cốc bột mì. Nếu ta muốn làm một nửa công thức (tỷ lệ 1/2), lượng bột mì cần đong là:\n3/4 × 1/2 = 3/8 cốc bột mì."
                            : "A recipe requires 3/4 cup of flour. Making a half batch means multiplying 3/4 by 1/2, resulting in 3/8 cup of flour.",
                        DiscussionQuestion = isVN
                            ? "Làm thế nào để đong được đúng 3/8 cốc bột mì nếu em chỉ có các loại cốc đong chia vạch 1/8 cốc?"
                            : "How can you measure 3/8 cup of flour if you only have a 1/8 cup measuring tool?",
                        InteractiveTask = isVN
                            ? "Đọc một công thức làm bánh ngọt bất kỳ trên sách. Hãy tính lại định lượng của tất cả nguyên liệu nếu chúng ta muốn làm gấp đôi công thức (nhân với 2)."
                            : "Choose a recipe and double all the fractional measurements. Write down the new recipe proportions."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⏰",
                        Title = isVN ? "Xem đồng hồ & Thời gian" : "Clock & Time Intervals",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fraction_3_{suffix}.png",
                        Description = isVN 
                            ? "Mặt đồng hồ kim được chia thành các khoảng góc phần tư giúp chúng ta hình dung thời gian trôi qua trực quan." 
                            : "Analog clocks are divided into quadrants to visualize time intervals.",
                        Formula = isVN ? "Phân số thời gian = Số phút trôi qua / 60 phút" : "Time Fraction = Minutes Passed / 60 Minutes",
                        MathAnalysis = isVN
                            ? "Mặt đồng hồ hình tròn có 60 phút. Khoảng thời gian 15 phút tương đương phân số 15/60 = 1/4 giờ. Khoảng thời gian 30 phút tương đương phân số 30/60 = 1/2 giờ (nửa tiếng)."
                            : "An hour has 60 minutes. A 15-minute interval represents 15/60 = 1/4 of an hour. A 30-minute interval represents 30/60 = 1/2 of an hour.",
                        DiscussionQuestion = isVN
                            ? "Tại sao người ta thường gọi 15 phút là 'một phần tư giờ' và 45 phút là 'ba phần tư giờ'?"
                            : "Why do we say 'a quarter past' for 15 minutes and 'a quarter to' for 45 minutes?",
                        InteractiveTask = isVN
                            ? "Vẽ một mặt đồng hồ tròn. Hãy tô màu xanh lá cây vào góc đại diện cho khoảng thời gian 20 phút. Rút gọn phân số biểu diễn khoảng thời gian này."
                            : "Draw a clock face. Color the area representing 20 minutes and simplify the resulting fraction."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⛽",
                        Title = isVN ? "Vạch nhiên liệu xe cộ" : "Fuel Gauge Levels",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fraction_4_{suffix}.png",
                        Description = isVN 
                            ? "Kim chỉ nhiên liệu trên bảng điều khiển xe hiển thị lượng xăng còn lại dưới dạng phân số như 1/4, 1/2 hoặc 3/4 bình xăng." 
                            : "Fuel gauges display remaining fuel level as fractional markers like 1/4, 1/2, or 3/4.",
                        Formula = isVN ? "Tỷ lệ xăng còn lại = Lượng xăng thực tế trong bình / Thể tích tối đa của bình xăng" : "Remaining Fuel Ratio = Current Volume / Tank Capacity",
                        MathAnalysis = isVN
                            ? "Nếu bình xăng của xe máy có dung tích tối đa là 4.8 lít xăng. Khi kim xăng chỉ ở vạch 1/4 bình, lượng xăng còn lại trong bình là:\n4.8 × 1/4 = 1.2 lít xăng."
                            : "If a motorcycle tank holds 4.8 liters, a 1/4 full marker indicates the remaining fuel volume is 4.8 × 1/4 = 1.2 liters.",
                        DiscussionQuestion = isVN
                            ? "Nếu xe đi hết 1.2 lít xăng cho quãng đường 50 km, bình xăng đầy (4.8 lít) sẽ giúp xe đi được quãng đường tối đa là bao nhiêu km?"
                            : "If the vehicle travels 50 km on 1.2 liters (1/4 tank), how far can it go on a full tank (4.8 liters)?",
                        InteractiveTask = isVN
                            ? "Quan sát kim chỉ vạch xăng trên xe của gia đình và ghi nhận phân số chỉ xăng. Tính toán lượng xăng còn lại nếu biết dung tích tối đa của bình chứa."
                            : "Check a family vehicle's fuel gauge. Calculate the remaining liters if you know the maximum tank capacity."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎵",
                        Title = isVN ? "Nốt nhạc và Nhịp điệu" : "Musical Notes & Rhythm",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fraction_5_{suffix}.png",
                        Description = isVN 
                            ? "Các nốt nhạc khác nhau biểu thị trường độ dài của âm thanh bằng các phân số." 
                            : "Musical notes represent duration values as fraction ratios.",
                        Formula = isVN ? "Trường độ nốt nhạc = Giá trị phân số của nốt × Độ dài nhịp phách cơ sở" : "Note Duration = Note Value × Beat Duration",
                        MathAnalysis = isVN
                            ? "Nốt tròn đại diện cho 1 nhịp nguyên. Nốt trắng bằng 1/2 nốt tròn. Nốt đen bằng 1/4 nốt tròn. Nốt móc đơn bằng 1/8 nốt tròn.\nPhép nhân: Một nhịp phách chứa 4 nốt móc đơn sẽ có độ dài bằng: 4 × 1/8 = 4/8 = 1/2 nhịp nguyên."
                            : "A whole note has a value of 1. A half note is 1/2, a quarter note is 1/4, and an eighth note is 1/8.",
                        DiscussionQuestion = isVN
                            ? "Có bao nhiêu nốt móc đơn (1/8) cần kết hợp lại để có trường độ dài bằng đúng một nốt trắng (1/2)?"
                            : "How many eighth notes (1/8) are needed to equal the duration of a half note (1/2)?",
                        InteractiveTask = isVN
                            ? "Gõ nhịp phách bằng tay theo chuỗi nốt sau: 1 nốt đen (1/4), 2 nốt móc đơn (1/8 + 1/8), và 1 nốt trắng (1/2). Tính tổng trường độ của cả chuỗi."
                            : "Clap the rhythm for a quarter note, two eighth notes, and a half note. Calculate the sum of their durations."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🗺",
                        Title = isVN ? "️ Bản đồ & Tỷ lệ xích" : "Geography & Map Scale",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fraction_6_{suffix}.png",
                        Description = isVN 
                            ? "Bản đồ địa lý sử dụng phân số tỷ lệ xích để biểu diễn khoảng cách thực tế được thu nhỏ lại trên bản vẽ." 
                            : "Map scales express real-world distance reductions using fractional ratios.",
                        Formula = isVN ? "Tỷ lệ xích (m) = Khoảng cách đo được trên bản đồ / Khoảng cách thực tế ngoài thực địa" : "Map Scale = Map Distance / Real Distance",
                        MathAnalysis = isVN
                            ? "Tỷ lệ xích 1/10.000 có nghĩa là 1 cm trên bản đồ đại diện cho 10.000 cm (tương đương 100 mét) ngoài thực tế. Nếu khoảng cách giữa trường học và thư viện trên bản đồ đo được là 5 cm, thì khoảng cách thực tế sẽ là:\n5 cm × 10.000 = 50.000 cm = 500 mét."
                            : "With a scale of 1/10,000, 1 cm on the map represents 10,000 cm (100 m) in reality. A map distance of 5 cm equals a real distance of 500 meters.",
                        DiscussionQuestion = isVN
                            ? "Nếu bản đồ tăng độ chi tiết và đổi tỷ lệ xích thành 1/2.000 thì kích thước của trường học trên bản đồ sẽ to lên hay nhỏ đi? Giải thích tại sao mẫu số nhỏ hơn lại làm hình ảnh to hơn?"
                            : "If the scale changes to 1/2,000, will the school's size on the map appear larger or smaller? Explain.",
                        InteractiveTask = isVN
                            ? "Sử dụng thước kẻ đo khoảng cách từ Town Center đến Green Park trên bản đồ minh họa. Áp dụng tỷ lệ xích 1/10.000 để tính khoảng cách thực tế."
                            : "Measure the map distance from Town Center to Green Park. Calculate the real distance using the 1/10,000 scale."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📈",
                        Title = isVN ? "Phân chia cổ phần công ty" : "Corporate Equity Split",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fraction_7_{suffix}.png",
                        Description = isVN 
                            ? "Biểu diễn tỷ lệ vốn góp của các cổ đông dưới dạng phân số để thực hiện chia cổ tức và biểu quyết." 
                            : "Represent investor share ratios as fractions for dividend distribution and voting.",
                        Formula = isVN ? "Tỷ lệ cổ phần = Số cổ phiếu sở hữu / Tổng số cổ phiếu lưu hành" : "Shareholding Ratio = Shares Owned / Total Shares",
                        MathAnalysis = isVN
                            ? "Công ty có tổng cộng 1.000 cổ phần. Cổ đông A sở hữu 300 cổ phần (tương đương phân số 3/10), cổ đông B sở hữu 500 cổ phần (tương đương phân số 1/2), cổ đông C sở hữu 200 cổ phần (tương đương 1/5).\nTổng cộng cổ phần: 3/10 + 1/2 + 1/5 = 3/10 + 5/10 + 2/10 = 10/10 = 1 (toàn bộ công ty)."
                            : "A company has 1,000 total shares. Shareholder A owns 300 shares (3/10), B owns 500 (1/2), and C owns 200 (1/5). Summing these gives 3/10 + 5/10 + 2/10 = 1.",
                        DiscussionQuestion = isVN
                            ? "Để thông qua một quyết định quan trọng, công ty cần sự đồng ý của các cổ đông sở hữu hơn 1/2 tổng số cổ phần. Cổ đông A và C có thể bắt tay nhau để đạt được tỷ lệ này không?"
                            : "A decision requires >1/2 approval. Can shareholders A (3/10) and C (1/5) approve it together by combining their shares?",
                        InteractiveTask = isVN
                            ? "Giả sử công ty chia mức cổ tức trị giá 200 triệu đồng. Hãy tính số tiền cổ tức mỗi cổ đông A, B, C nhận được dựa trên tỷ lệ phân số cổ phần tương ứng."
                            : "Distribute a 200 million VND dividend among shareholders A (3/10), B (1/2), and C (1/5). Calculate each payout."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎶",
                        Title = isVN ? "Nhịp phách âm nhạc" : "Musical Time Signatures",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_fraction_8_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng phân số chỉ nhịp (như 3/4, 4/4) để phân chia trường độ của nốt nhạc trong mỗi ô nhịp." 
                            : "Use time signatures (like 3/4, 4/4) to divide beat durations within measures.",
                        Formula = isVN ? "Số lượng phách mỗi ô nhịp = Tử số của số chỉ nhịp; Độ dài phách cơ sở = 1 / Mẫu số" : "Beats per Measure = Numerator; Beat Value = 1 / Denominator",
                        MathAnalysis = isVN
                            ? "Số chỉ nhịp 3/4 có tử số là 3 và mẫu số là 4. Nghĩa là mỗi ô nhịp có 3 phách, và mỗi phách có giá trị bằng một nốt đen (1/4). Tổng độ dài các nốt nhạc trong một ô nhịp bắt buộc phải bằng: 3 × 1/4 = 3/4 phách tiêu chuẩn."
                            : "A 3/4 time signature has a numerator of 3 and a denominator of 4, meaning 3 beats per measure, each valued at a quarter note (1/4).",
                        DiscussionQuestion = isVN
                            ? "Trong ô nhịp 3/4, em có thể sử dụng đồng thời 1 nốt trắng (1/2) và 2 nốt móc đơn (1/8 + 1/8) được không? Hãy tính tổng phân số của chúng."
                            : "In a 3/4 measure, can you place a half note (1/2) and two eighth notes (1/8 + 1/8)? Verify by sum.",
                        InteractiveTask = isVN
                            ? "Hãy tính tổng trường độ của một ô nhịp chứa: 1 nốt đen (1/4), 1 nốt móc kép (1/16) và 1 nốt đen chấm dôi (3/8). Ô nhịp đó có đủ nhịp 3/4 không?"
                            : "Sum the duration values: 1/4 note, 1/16 note, and 3/8 note. Check if they fit in a 3/4 measure."
                    }
                };

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for FractionTool: {Err}", ex.Message);
            }
        }

        // ═══ HIGH SCORE MANAGEMENT ═══
        private void LoadHighScore()
        {
            try
            {
                string path = System.IO.Path.Combine(global::QASmartClass.Services.AppPaths.SettingsDir, "fraction_highscore.txt");
                if (System.IO.File.Exists(path))
                {
                    string content = System.IO.File.ReadAllText(path);
                    var parts = content.Split('|');
                    if (parts.Length == 2)
                    {
                        string scoreStr = parts[0];
                        string hash = parts[1];
                        string salt = "QASmartClass_Salt_2026";
                        if (hash == GetSha256(scoreStr + salt))
                        {
                            if (int.TryParse(scoreStr, out int hs))
                            {
                                _quizHighScore = hs;
                                txtQuizHighScore.Text = $"Kỷ lục cao nhất: {_quizHighScore} 👑";
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void SaveHighScore()
        {
            try
            {
                System.IO.Directory.CreateDirectory(global::QASmartClass.Services.AppPaths.SettingsDir);
                string path = System.IO.Path.Combine(global::QASmartClass.Services.AppPaths.SettingsDir, "fraction_highscore.txt");
                string scoreStr = _quizHighScore.ToString();
                string salt = "QASmartClass_Salt_2026";
                string hash = GetSha256(scoreStr + salt);
                System.IO.File.WriteAllText(path, $"{scoreStr}|{hash}");
            }
            catch { }
        }

        // ═══ GAMIFIED QUIZ LOGIC ═══
        private void QuizConfig_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                _currentQuestion = null;
                txtQuizQuestion.Text = "Nhấn nút 'Tạo Câu Hỏi Mới' để bắt đầu thử thách!";
                quizInputArea.Visibility = Visibility.Collapsed;
                if (quizFeedbackPanel != null) quizFeedbackPanel.Children.Clear();
            }
        }

        private void BtnNewQuiz_Click(object sender, RoutedEventArgs e)
        {
            if (cboQuizTopic == null || cboQuizDifficulty == null) return;

            _currentQuestion = GenerateQuestion(cboQuizTopic.SelectedIndex, cboQuizDifficulty.SelectedIndex);
            txtQuizQuestion.Text = _currentQuestion.QuestionText;
            quizFeedbackPanel.Children.Clear();

            txtQuizAnsN.Text = "";
            txtQuizAnsD.Text = "";

            if (_currentQuestion.IsComparison)
            {
                quizFracAnswerArea.Visibility = Visibility.Collapsed;
                quizCompAnswerArea.Visibility = Visibility.Visible;
            }
            else
            {
                quizFracAnswerArea.Visibility = Visibility.Visible;
                quizCompAnswerArea.Visibility = Visibility.Collapsed;
            }

            quizInputArea.Visibility = Visibility.Visible;
        }

        private void BtnCheckQuiz_Click(object sender, RoutedEventArgs e)
        {
            if (_currentQuestion == null) return;

            if (string.IsNullOrWhiteSpace(txtQuizAnsN.Text) || string.IsNullOrWhiteSpace(txtQuizAnsD.Text))
            {
                ShowNotification("Vui lòng nhập câu trả lời đầy đủ (cả tử số và mẫu số).", "warning");
                return;
            }

            if (!int.TryParse(txtQuizAnsN.Text, out int ansN) || !int.TryParse(txtQuizAnsD.Text, out int ansD))
            {
                ShowNotification("Vui lòng chỉ nhập số nguyên.", "warning");
                return;
            }

            if (ansD == 0)
            {
                ShowNotification("Mẫu số của câu trả lời phải khác 0.", "warning");
                return;
            }

            NormalizeFraction(ref ansN, ref ansD);

            // strict check: value must match AND it must be simplified
            int g = Gcd(System.Math.Abs(ansN), System.Math.Abs(ansD));
            int userN_simp = ansN / g;
            int userD_simp = ansD / g;
            NormalizeFraction(ref userN_simp, ref userD_simp);

            quizFeedbackPanel.Children.Clear();

            if (ansN == _currentQuestion.ExpectedNum && ansD == _currentQuestion.ExpectedDen)
            {
                // Correct!
                int pts = cboQuizDifficulty.SelectedIndex switch { 0 => 10, 1 => 20, _ => 30 };
                _quizStreak++;
                if (_quizStreak >= 5) pts *= 2; // double points for streak >= 5

                _quizScore += pts;
                if (_quizScore > _quizHighScore)
                {
                    _quizHighScore = _quizScore;
                    txtQuizHighScore.Text = $"Kỷ lục cao nhất: {_quizHighScore} 👑";
                    SaveHighScore();
                }

                txtQuizScore.Text = $"Điểm số: {_quizScore}";
                txtQuizStreak.Text = $"Chuỗi thắng: {_quizStreak} 🔥";
                UpdateBadge();

                UI.ResultRow($"🎉 Chính xác! Bạn được +{pts} điểm. Chuỗi thắng hiện tại là {_quizStreak}.", "#2E7D32", quizFeedbackPanel);
                quizInputArea.Visibility = Visibility.Collapsed;
            }
            else if (userN_simp == _currentQuestion.ExpectedNum && userD_simp == _currentQuestion.ExpectedDen)
            {
                // Correct value but not simplified
                _quizStreak = 0;
                txtQuizStreak.Text = $"Chuỗi thắng: {_quizStreak} 🔥";
                UI.ResultRow($"⚠️ Đúng giá trị nhưng phân số chưa tối giản! Bạn cần rút gọn chia cả tử và mẫu cho {g}.", "#E65100", quizFeedbackPanel);
            }
            else
            {
                // Incorrect
                _quizStreak = 0;
                txtQuizStreak.Text = $"Chuỗi thắng: {_quizStreak} 🔥";
                UI.ResultRow($"❌ Chưa chính xác! Câu trả lời đúng là: {FracStr(_currentQuestion.ExpectedNum, _currentQuestion.ExpectedDen)}", "#C62828", quizFeedbackPanel);
                UI.ResultRow($"Lời giải chi tiết:\n{_currentQuestion.DetailedSolution}", "#1565C0", quizFeedbackPanel);
                quizInputArea.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnQuizComp_Click(object sender, RoutedEventArgs e)
        {
            if (_currentQuestion == null) return;
            var btn = (Button)sender;
            string userChoice = btn.Tag.ToString();
            CheckCompareAnswer(userChoice);
        }

        private void CheckCompareAnswer(string userChoice)
        {
            quizFeedbackPanel.Children.Clear();

            if (userChoice == _currentQuestion.ExpectedCompareSymbol)
            {
                int pts = cboQuizDifficulty.SelectedIndex switch { 0 => 10, 1 => 20, _ => 30 };
                _quizStreak++;
                if (_quizStreak >= 5) pts *= 2;

                _quizScore += pts;
                if (_quizScore > _quizHighScore)
                {
                    _quizHighScore = _quizScore;
                    txtQuizHighScore.Text = $"Kỷ lục cao nhất: {_quizHighScore} 👑";
                    SaveHighScore();
                }

                txtQuizScore.Text = $"Điểm số: {_quizScore}";
                txtQuizStreak.Text = $"Chuỗi thắng: {_quizStreak} 🔥";
                UpdateBadge();

                UI.ResultRow($"🎉 Chính xác! Bạn được +{pts} điểm. Chuỗi thắng hiện tại là {_quizStreak}.", "#2E7D32", quizFeedbackPanel);
                quizInputArea.Visibility = Visibility.Collapsed;
            }
            else
            {
                _quizStreak = 0;
                txtQuizStreak.Text = $"Chuỗi thắng: {_quizStreak} 🔥";
                UI.ResultRow($"❌ Chưa chính xác! Kết quả đúng là: {_currentQuestion.ExpectedCompareSymbol}", "#C62828", quizFeedbackPanel);
                UI.ResultRow($"Lời giải chi tiết:\n{_currentQuestion.DetailedSolution}", "#1565C0", quizFeedbackPanel);
                quizInputArea.Visibility = Visibility.Collapsed;
            }
        }

        private FractionQuestion GenerateQuestion(int topicIdx, int difficultyIdx)
        {
            var q = new FractionQuestion();
            int maxVal = difficultyIdx switch { 0 => 9, 1 => 20, _ => 50 };

            if (topicIdx == 0) // Operations (+ - * /)
            {
                q.IsComparison = false;
                int op = Rnd.Next(4); // 0: +, 1: -, 2: *, 3: /
                int n1 = Rnd.Next(1, maxVal);
                int d1 = Rnd.Next(2, maxVal);
                int n2 = Rnd.Next(1, maxVal);
                int d2 = Rnd.Next(2, maxVal);

                if (difficultyIdx == 2 && Rnd.Next(2) == 0) n1 = -n1; // Negative support in hard
                if (difficultyIdx == 2 && Rnd.Next(2) == 0) n2 = -n2;

                NormalizeFraction(ref n1, ref d1);
                NormalizeFraction(ref n2, ref d2);

                if (difficultyIdx == 0) // Same denominator for easy add/sub
                {
                    d2 = d1;
                    op = Rnd.Next(2); // only + or -
                }

                if (op == 0) // Addition
                {
                    long ansN = (long)n1 * d2 + (long)n2 * d1;
                    long ansD = (long)d1 * d2;
                    int g = Gcd((int)System.Math.Abs(ansN), (int)System.Math.Abs(ansD));
                    int tempN = (int)(ansN / g);
                    int tempD = (int)(ansD / g);
                    NormalizeFraction(ref tempN, ref tempD);
                    q.ExpectedNum = tempN;
                    q.ExpectedDen = tempD;
                    q.QuestionText = $"Hãy tính: {FracStr(n1, d1)} + {FracStr(n2, d2)} (Nhập kết quả dưới dạng phân số tối giản)";
                    q.DetailedSolution = $"• Quy đồng và tính:\n  {FracStr(n1, d1)} + {FracStr(n2, d2)} = ({n1}×{d2} + {n2}×{d1})/({d1}×{d2}) = {ansN}/{ansD}\n• Rút gọn tối giản chia cả tử và mẫu cho {g} => {FracStr(q.ExpectedNum, q.ExpectedDen)}";
                }
                else if (op == 1) // Subtraction
                {
                    long ansN = (long)n1 * d2 - (long)n2 * d1;
                    long ansD = (long)d1 * d2;
                    int g = Gcd((int)System.Math.Abs(ansN), (int)System.Math.Abs(ansD));
                    int tempN = (int)(ansN / g);
                    int tempD = (int)(ansD / g);
                    NormalizeFraction(ref tempN, ref tempD);
                    q.ExpectedNum = tempN;
                    q.ExpectedDen = tempD;
                    q.QuestionText = $"Hãy tính: {FracStr(n1, d1)} − {FracStr(n2, d2)} (Nhập kết quả dưới dạng phân số tối giản)";
                    q.DetailedSolution = $"• Quy đồng và tính:\n  {FracStr(n1, d1)} − {FracStr(n2, d2)} = ({n1}×{d2} − {n2}×{d1})/({d1}×{d2}) = {ansN}/{ansD}\n• Rút gọn tối giản chia cả tử và mẫu cho {g} => {FracStr(q.ExpectedNum, q.ExpectedDen)}";
                }
                else if (op == 2) // Multiplication
                {
                    long ansN = (long)n1 * n2;
                    long ansD = (long)d1 * d2;
                    int g = Gcd((int)System.Math.Abs(ansN), (int)System.Math.Abs(ansD));
                    int tempN = (int)(ansN / g);
                    int tempD = (int)(ansD / g);
                    NormalizeFraction(ref tempN, ref tempD);
                    q.ExpectedNum = tempN;
                    q.ExpectedDen = tempD;
                    q.QuestionText = $"Hãy tính: {FracStr(n1, d1)} × {FracStr(n2, d2)} (Nhập kết quả dưới dạng phân số tối giản)";
                    q.DetailedSolution = $"• Nhân tử với tử, mẫu với mẫu:\n  {n1}×{n2} / {d1}×{d2} = {ansN}/{ansD}\n• Rút gọn tối giản chia cả tử và mẫu cho {g} => {FracStr(q.ExpectedNum, q.ExpectedDen)}";
                }
                else // Division
                {
                    if (n2 == 0) n2 = 1; // avoid divide by zero
                    long ansN = (long)n1 * d2;
                    long ansD = (long)d1 * n2;
                    if (ansD < 0) { ansN = -ansN; ansD = -ansD; }
                    int g = Gcd((int)System.Math.Abs(ansN), (int)System.Math.Abs(ansD));
                    int tempN = (int)(ansN / g);
                    int tempD = (int)(ansD / g);
                    NormalizeFraction(ref tempN, ref tempD);
                    q.ExpectedNum = tempN;
                    q.ExpectedDen = tempD;
                    q.QuestionText = $"Hãy tính: {FracStr(n1, d1)} ÷ {FracStr(n2, d2)} (Nhập kết quả dưới dạng phân số tối giản)";
                    q.DetailedSolution = $"• Nhân nghịch đảo phân số thứ hai:\n  {FracStr(n1, d1)} × {FracStr(d2, n2)} = {ansN}/{ansD}\n• Rút gọn tối giản chia cả tử và mẫu cho {g} => {FracStr(q.ExpectedNum, q.ExpectedDen)}";
                }
            }
            else if (topicIdx == 1) // Simplify
            {
                q.IsComparison = false;
                int common = Rnd.Next(2, 6);
                int sd = Rnd.Next(2, maxVal);
                int sn = Rnd.Next(1, sd); // keep it a proper fraction
                if (difficultyIdx == 2 && Rnd.Next(2) == 0) sn = -sn;

                int n = sn * common;
                int d = sd * common;
                NormalizeFraction(ref n, ref d);

                int g = Gcd(System.Math.Abs(n), System.Math.Abs(d));
                int tempN = n / g;
                int tempD = d / g;
                NormalizeFraction(ref tempN, ref tempD);
                q.ExpectedNum = tempN;
                q.ExpectedDen = tempD;

                q.QuestionText = $"Hãy rút gọn phân số sau về dạng tối giản: {FracStr(n, d)}";
                q.DetailedSolution = $"• Tìm ƯCLN({System.Math.Abs(n)}, {System.Math.Abs(d)}) = {g}\n• Chia cả tử và mẫu cho {g}:\n  {n}÷{g} / {d}÷{g} = {FracStr(q.ExpectedNum, q.ExpectedDen)}";
            }
            else if (topicIdx == 2) // Compare
            {
                q.IsComparison = true;
                int n1 = Rnd.Next(1, maxVal);
                int d1 = Rnd.Next(2, maxVal);
                int n2 = Rnd.Next(1, maxVal);
                int d2 = Rnd.Next(2, maxVal);

                if (difficultyIdx == 2 && Rnd.Next(2) == 0) n1 = -n1;
                if (difficultyIdx == 2 && Rnd.Next(2) == 0) n2 = -n2;

                NormalizeFraction(ref n1, ref d1);
                NormalizeFraction(ref n2, ref d2);

                if (difficultyIdx == 0) // same denominator
                {
                    d2 = d1;
                }

                long left = (long)n1 * d2;
                long right = (long)n2 * d1;

                q.ExpectedCompareSymbol = left > right ? ">" : left < right ? "<" : "=";
                q.QuestionText = $"So sánh hai phân số: {FracStr(n1, d1)} và {FracStr(n2, d2)}";
                q.DetailedSolution = $"• Cách 1 (Quy đồng): MSC = {Lcm(d1, d2)}, đưa về cùng mẫu rồi so sánh tử số.\n" +
                                     $"• Cách 2 (Nhân chéo): {n1}×{d2} = {left} so với {n2}×{d1} = {right}.\n" +
                                     $"  Vì {left} {q.ExpectedCompareSymbol} {right} nên {FracStr(n1, d1)} {q.ExpectedCompareSymbol} {FracStr(n2, d2)}";
            }
            else // Mixed Number
            {
                q.IsComparison = false;
                int whole = Rnd.Next(1, 6);
                int d = Rnd.Next(2, 8);
                int n = Rnd.Next(1, d);
                int sign = (difficultyIdx == 2 && Rnd.Next(2) == 0) ? -1 : 1;

                int impN = (whole * d + n) * sign;
                int impD = d;
                NormalizeFraction(ref impN, ref impD);

                int g = Gcd(System.Math.Abs(impN), System.Math.Abs(impD));
                int tempN = impN / g;
                int tempD = impD / g;
                NormalizeFraction(ref tempN, ref tempD);
                q.ExpectedNum = tempN;
                q.ExpectedDen = tempD;

                string mixedStr = sign < 0 ? $"−({whole} {n}/{d})" : $"{whole} {n}/{d}";
                q.QuestionText = $"Đổi hỗn số sau sang phân số tối giản: {mixedStr}";
                q.DetailedSolution = $"• Lấy phần nguyên nhân mẫu số cộng tử số: {whole} × {d} + {n} = {whole * d + n}\n" +
                                     $"• Giữ nguyên mẫu số: {whole * d + n}/{d}\n" +
                                     $"• Thêm dấu âm nếu có và rút gọn tối giản => {FracStr(q.ExpectedNum, q.ExpectedDen)}";
            }

            return q;
        }

        private void UpdateBadge()
        {
            if (brdBadge == null || txtBadgeIcon == null || txtBadgeName == null) return;

            if (_quizScore >= 500)
            {
                brdBadge.Visibility = Visibility.Visible;
                txtBadgeIcon.Text = "👑";
                txtBadgeName.Text = "Trưởng Lão";
            }
            else if (_quizScore >= 200)
            {
                brdBadge.Visibility = Visibility.Visible;
                txtBadgeIcon.Text = "🏆";
                txtBadgeName.Text = "Hiệp Sĩ";
            }
            else if (_quizScore >= 100)
            {
                brdBadge.Visibility = Visibility.Visible;
                txtBadgeIcon.Text = "🥈";
                txtBadgeName.Text = "Chiến Binh";
            }
            else if (_quizScore >= 50)
            {
                brdBadge.Visibility = Visibility.Visible;
                txtBadgeIcon.Text = "🎖️";
                txtBadgeName.Text = "Tập Sự";
            }
            else
            {
                brdBadge.Visibility = Visibility.Collapsed;
            }
        }

        // ═══ INTERACTIVE COLORING LOGIC ═══
        private void BtnNewColorChallenge_Click(object sender, RoutedEventArgs e)
        {
            int b = Rnd.Next(2, 13); // limit denominator to 12
            int a = Rnd.Next(1, b);

            _targetColorN = a;
            _targetColorD = b;
            _coloringState = new bool[b];

            txtColorChallenge.Text = $"Hãy tô màu biểu diễn phân số: {a}/{b}";
            txtColorChallenge.Foreground = new SolidColorBrush(Color.FromRgb(2, 119, 189)); // Blue 800

            coloringBoardPanel.Children.Clear();

            var grid = new UniformGrid { Rows = 1, Columns = b, Width = 300, Height = 40 };

            for (int i = 0; i < b; i++)
            {
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(238, 238, 238)),
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Tag = i,
                    Margin = new Thickness(1),
                    CornerRadius = new CornerRadius(i == 0 ? 4 : 0, i == b - 1 ? 4 : 0, i == b - 1 ? 4 : 0, i == 0 ? 4 : 0),
                    Cursor = Cursors.Hand
                };
                border.PreviewMouseLeftButtonDown += ColoringSegment_Click;
                grid.Children.Add(border);
            }

            coloringBoardPanel.Children.Add(grid);
            btnCheckColoring.Visibility = Visibility.Visible;
        }

        private void ColoringSegment_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int idx)
            {
                _coloringState[idx] = !_coloringState[idx];
                border.Background = new SolidColorBrush(_coloringState[idx]
                    ? Color.FromRgb(230, 81, 0) // Accent orange
                    : Color.FromRgb(238, 238, 238));
            }
        }

        private void BtnCheckColoring_Click(object sender, RoutedEventArgs e)
        {
            int count = 0;
            foreach (bool val in _coloringState)
            {
                if (val) count++;
            }

            if (count == _targetColorN)
            {
                txtColorChallenge.Text = $"🎉 Chính xác! Bạn đã tô đúng {_targetColorN}/{_targetColorD} phần.";
                txtColorChallenge.Foreground = Brushes.Green;
            }
            else
            {
                txtColorChallenge.Text = $"❌ Chưa chính xác! Bạn đã tô {count}/{_targetColorD} phần (Yêu cầu là {_targetColorN}/{_targetColorD}). Hãy thử lại!";
                txtColorChallenge.Foreground = Brushes.Red;
            }
        }

        // ═══ PDF EXPORT LOGIC ═══
        private void ExportPDF_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var lines = new List<string>();
                foreach (var child in resultPanel.Children)
                {
                    if (child is Border b && b.Child is TextBlock tb)
                    {
                        lines.Add(tb.Text);
                    }
                }

                if (lines.Count == 0)
                {
                    ShowNotification("Không có lời giải để xuất. Vui lòng thực hiện phép tính hợp lệ trước.", "warning");
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF Document (*.pdf)|*.pdf",
                    FileName = $"Phieu_Bai_Tap_Phan_So_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    string filePath = dialog.FileName;

                    QuestPDF.Fluent.Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(2, Unit.Centimetre);
                            page.PageColor(QuestPDF.Helpers.Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                            page.Header().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("TRƯỜNG TIỂU HỌC: .......................................").FontSize(10).Bold();
                                    col.Item().Text("LỚP: .................................... SỐ: .........").FontSize(10).Bold();
                                    col.Item().Text("HỌ VÀ TÊN: ..........................................").FontSize(10).Bold();
                                });
                                row.ConstantItem(180).Column(col =>
                                {
                                    col.Item().Text("PHIẾU HỌC TẬP PHÂN SỐ").FontSize(12).Bold().FontColor("#E65100");
                                    col.Item().Text($"Ngày xuất bản: {DateTime.Now:dd/MM/yyyy}").FontSize(9).Italic();
                                });
                            });

                            page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                            {
                                col.Item().PaddingBottom(12).Text("Đề bài & Hướng dẫn giải chi tiết").FontSize(14).Bold().FontColor("#1565C0");

                                foreach (var line in lines)
                                {
                                    col.Item().BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingVertical(8).Text(line).FontSize(11);
                                }
                            });

                            page.Footer().AlignCenter().Text(t =>
                            {
                                t.Span("Trang ").FontSize(9);
                                t.CurrentPageNumber().FontSize(9);
                                t.Span(" / ").FontSize(9);
                                t.TotalPages().FontSize(9);
                            });
                        });
                    })
                    .GeneratePdf(filePath);

                    ShowNotification("Xuất file PDF thành công!", "success");
                }
            }
            catch (Exception ex)
            {
                try
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("TRƯỜNG TIỂU HỌC: .......................................");
                    sb.AppendLine("LỚP: .................................... SỐ: .........");
                    sb.AppendLine("HỌ VÀ TÊN: ..........................................");
                    sb.AppendLine("--- PHIẾU HỌC TẬP PHÂN SỐ ---");
                    sb.AppendLine($"Ngày xuất bản: {DateTime.Now:dd/MM/yyyy}");
                    sb.AppendLine();
                    foreach (var child in resultPanel.Children)
                    {
                        if (child is Border b && b.Child is TextBlock tb)
                        {
                            sb.AppendLine(tb.Text);
                            sb.AppendLine("--------------------------------------------------");
                        }
                    }
                    Clipboard.SetText(sb.ToString());
                    ShowNotification($"Lỗi xuất PDF ({ex.Message}). Đã sao chép nội dung lời giải chi tiết vào Clipboard.", "warning");
                }
                catch
                {
                    ShowNotification($"Lỗi: {ex.Message}", "error");
                }
            }
        }


        // ═══ HELPERS ═══
        private static void NormalizeFraction(ref int num, ref int den)
        {
            if (den < 0)
            {
                num = -num;
                den = -den;
            }
        }

        private static int Gcd(int a, int b)
        {
            a = System.Math.Abs(a); b = System.Math.Abs(b);
            while (b != 0) { int t = b; b = a % b; a = t; }
            return a == 0 ? 1 : a;
        }

        private static int Lcm(int a, int b)
        {
            if (a == 0 || b == 0) return 1;
            return System.Math.Abs(a / Gcd(a, b) * b);
        }

        private static string FracStr(int n, int d) =>
            d == 1 ? $"{n}" : $"{n}/{d}";

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
                Text = text, FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(active ? System.Windows.Media.Colors.White : Accent),
                FontFamily = new FontFamily("Segoe UI")
            };
            return btn;
        }

        /* AddResult / AddR replaced by UI.ResultRow */

        // ═══ Graph — Number Line Visualization ═══
        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var ci = CultureInfo.InvariantCulture;
                string expJs;
                string title;

                if (_mode == Mode.Operations || _mode == Mode.Compare)
                {
                    if (!int.TryParse(txtN1?.Text, out int n1))
                    {
                        ShowNotification("Vui lòng nhập đúng tử số thứ nhất.", "warning");
                        return;
                    }
                    if (!int.TryParse(txtD1?.Text, out int d1) || d1 == 0)
                    {
                        ShowNotification("Mẫu số thứ nhất phải khác 0.", "warning");
                        return;
                    }
                    if (!int.TryParse(txtN2?.Text, out int n2))
                    {
                        ShowNotification("Vui lòng nhập đúng tử số thứ hai.", "warning");
                        return;
                    }
                    if (!int.TryParse(txtD2?.Text, out int d2) || d2 == 0)
                    {
                        ShowNotification("Mẫu số thứ hai phải khác 0.", "warning");
                        return;
                    }

                    NormalizeFraction(ref n1, ref d1);
                    NormalizeFraction(ref n2, ref d2);

                    double v1 = (double)n1 / d1, v2 = (double)n2 / d2;
                    double lo = System.Math.Min(v1, v2) - 0.5;
                    double hi = System.Math.Max(v1, v2) + 0.5;

                    expJs = $@"
        calc.setExpression({{id:'ax', latex:'y=0', color:'#9E9E9E', lineWidth:1}});
        calc.setExpression({{id:'p1', latex:'({v1.ToString(ci)},0)', color:'#E65100', pointSize:12, label:'{n1}/{d1}', showLabel:true}});
        calc.setExpression({{id:'p2', latex:'({v2.ToString(ci)},0)', color:'#1565C0', pointSize:12, label:'{n2}/{d2}', showLabel:true}});
        calc.setMathBounds({{left:{lo.ToString(ci)},right:{hi.ToString(ci)},bottom:-1,top:1}});";
                    title = $"📐 {n1}/{d1} so với {n2}/{d2} trên trục số";
                }
                else
                {
                    if (!int.TryParse(txtSN?.Text, out int n))
                    {
                        ShowNotification("Vui lòng nhập đúng tử số.", "warning");
                        return;
                    }
                    if (!int.TryParse(txtSD?.Text, out int d) || d == 0)
                    {
                        ShowNotification("Mẫu số phải khác 0.", "warning");
                        return;
                    }

                    NormalizeFraction(ref n, ref d);

                    double v = (double)n / d;
                    int g = Gcd(System.Math.Abs(n), System.Math.Abs(d));
                    int sn = n / g, sd = d / g;
                    double sv = (double)sn / sd;
                    double lo = System.Math.Min(0, v) - 0.5;
                    double hi = System.Math.Max(1, v) + 0.5;

                    expJs = $@"
        calc.setExpression({{id:'ax', latex:'y=0', color:'#9E9E9E', lineWidth:1}});
        calc.setExpression({{id:'p0', latex:'(0,0)', color:'#757575', pointSize:6, label:'0', showLabel:true}});
        calc.setExpression({{id:'p1', latex:'(1,0)', color:'#757575', pointSize:6, label:'1', showLabel:true}});
        calc.setExpression({{id:'pv', latex:'({v.ToString(ci)},0)', color:'#E65100', pointSize:12, label:'{n}/{d} = {sv.ToString(ci)}', showLabel:true}});
        calc.setMathBounds({{left:{lo.ToString(ci)},right:{hi.ToString(ci)},bottom:-1,top:1}});";
                    title = _mode == Mode.MixedNumber
                        ? $"🔄 {n}/{d} trên trục số"
                        : $"✂️ {n}/{d} = {sn}/{sd} trên trục số";
                }

                if (_graphWindow != null && _graphWindow.IsLoaded)
                {
                    _graphWindow.UpdateExpressions(expJs, title, "Đồ thị — QA SmartClass");
                    _graphWindow.Focus();
                }
                else
                {
                    _graphWindow = new GraphWindow(expJs, title);
                    _graphWindow.Owner = Window.GetWindow(this);
                    _graphWindow.Show();
                }
            }
            catch (Exception ex) { ShowNotification($"Lỗi: {ex.Message}", "error"); }
        }
        private void ShowNotification(string message, string type = "error")
        {
            if (inlineNotification == null || txtNotificationMessage == null || txtNotificationIcon == null) return;
            
            txtNotificationMessage.Text = message;
            if (type == "error")
            {
                inlineNotification.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 235, 235));
                inlineNotification.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 83, 80));
                txtNotificationIcon.Text = "❌";
            }
            else if (type == "warning")
            {
                inlineNotification.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 243, 224));
                inlineNotification.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 167, 38));
                txtNotificationIcon.Text = "⚠️";
            }
            else
            {
                inlineNotification.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 245, 233));
                inlineNotification.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 187, 106));
                txtNotificationIcon.Text = "🎉";
                
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                timer.Tick += (s, args) => { inlineNotification.Visibility = System.Windows.Visibility.Collapsed; timer.Stop(); };
                timer.Start();
            }
            inlineNotification.Visibility = System.Windows.Visibility.Visible;
        }

        private bool IsPrimaryLevel()
        {
            try
            {
                var app = System.Windows.Application.Current as global::QASmartTouch.App;
                if (app != null)
                {
                    var prop = app.GetType().GetProperty("StudentNetwork");
                    if (prop != null)
                    {
                        var net = prop.GetValue(app);
                        if (net != null)
                        {
                            var classProp = net.GetType().GetProperty("ClassName");
                            if (classProp != null)
                            {
                                string className = classProp.GetValue(net) as string ?? "";
                                if (!string.IsNullOrEmpty(className))
                                {
                                    char first = className[0];
                                    if (first == '1' || first == '2' || first == '3' || first == '4' || first == '5')
                                    {
                                        if (className.StartsWith("10") || className.StartsWith("11") || className.StartsWith("12"))
                                        {
                                            return false;
                                        }
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private static string GetSha256(string input)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
