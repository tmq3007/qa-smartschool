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
using System.IO;
using System.Text.Json;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class SequenceTool : BaseToolControl
    {
        private bool _isCalculating;
        private double _apQuizCorrectAnswer;
        private int _apStreak = 0;
        private int _gpStreak = 0;
        private string _apQuizQuestionText;
        private double _gpQuizCorrectAnswer;
        private string _gpQuizQuestionText;
        private readonly Random _random = new();

        public class APConfig
        {
            public int Mode { get; set; }
            public double U1 { get; set; }
            public double D { get; set; }
            public int N { get; set; }
            public double Un { get; set; }
            public double Sn { get; set; }
        }

        public class GPConfig
        {
            public int Mode { get; set; }
            public double U1 { get; set; }
            public double Q { get; set; }
            public int N { get; set; }
            public double Un { get; set; }
            public double Sn { get; set; }
        }

        public class HistoryData
        {
            public List<APConfig> ApHistory { get; set; } = new();
            public List<GPConfig> GpHistory { get; set; } = new();
        }

        private List<APConfig> _apHistory = new();
        private List<GPConfig> _gpHistory = new();

        public SequenceTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextAP != null) menuTextAP.Text = isVN ? "Cấp số cộng" : "Arithmetic Progression";
                if (menuTextGP != null) menuTextGP.Text = isVN ? "Cấp số nhân" : "Geometric Progression";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                LoadHistoryFromDisk();
                BuildAPPresets(); BuildGPPresets();
                UpdateAPFieldsVisibility(); UpdateGPFieldsVisibility();
                UpdateAPHistoryUI(); UpdateGPHistoryUI();
                SolveAP(); SolveGP();
                GenerateAPQuiz(); GenerateGPQuiz();
                // Bàn phím số mini
                if (txtAP_U1 != null) TouchNumPad.Attach(txtAP_U1, step: 1);
                if (txtAP_D != null) TouchNumPad.Attach(txtAP_D, step: 1);
                if (txtAP_N != null) TouchNumPad.Attach(txtAP_N, step: 1, min: 1);
                if (txtGP_U1 != null) TouchNumPad.Attach(txtGP_U1, step: 1);
                if (txtGP_Q != null) TouchNumPad.Attach(txtGP_Q, step: 0.5);
                if (txtGP_N != null) TouchNumPad.Attach(txtGP_N, step: 1, min: 1);
                if (txtAP_Un != null) TouchNumPad.Attach(txtAP_Un, step: 1);
                if (txtAP_Sn != null) TouchNumPad.Attach(txtAP_Sn, step: 1);
                if (txtGP_Un != null) TouchNumPad.Attach(txtGP_Un, step: 1);
                if (txtGP_Sn != null) TouchNumPad.Attach(txtGP_Sn, step: 1);
                LoadPracticalApps();
            };
        }

        private void APMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            UpdateAPFieldsVisibility();
            SolveAP();
        }

        private void GPMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            UpdateGPFieldsVisibility();
            SolveGP();
        }

        private void UpdateAPFieldsVisibility()
        {
            if (cbAP_Mode == null) return;
            int mode = cbAP_Mode.SelectedIndex;
            bool isU1Enabled = (mode == 0 || mode == 3 || mode == 4 || mode == 5 || mode == 6);
            bool isDEnabled = (mode == 0 || mode == 1 || mode == 2 || mode == 5 || mode == 6);
            bool isNEnabled = (mode == 0 || mode == 1 || mode == 2 || mode == 3 || mode == 4);
            bool isUnEnabled = (mode == 1 || mode == 3 || mode == 5);
            bool isSnEnabled = (mode == 2 || mode == 4 || mode == 6);

            txtAP_U1.IsEnabled = isU1Enabled;
            borderAP_U1.Opacity = isU1Enabled ? 1.0 : 0.5;
            txtAP_D.IsEnabled = isDEnabled;
            borderAP_D.Opacity = isDEnabled ? 1.0 : 0.5;
            txtAP_N.IsEnabled = isNEnabled;
            borderAP_N.Opacity = isNEnabled ? 1.0 : 0.5;
            txtAP_Un.IsEnabled = isUnEnabled;
            borderAP_Un.Opacity = isUnEnabled ? 1.0 : 0.5;
            txtAP_Sn.IsEnabled = isSnEnabled;
            borderAP_Sn.Opacity = isSnEnabled ? 1.0 : 0.5;
        }

        private void UpdateGPFieldsVisibility()
        {
            if (cbGP_Mode == null) return;
            int mode = cbGP_Mode.SelectedIndex;
            bool isU1Enabled = (mode == 0 || mode == 3 || mode == 4 || mode == 5 || mode == 6);
            bool isQEnabled = (mode == 0 || mode == 1 || mode == 2 || mode == 5 || mode == 6);
            bool isNEnabled = (mode == 0 || mode == 1 || mode == 2 || mode == 3 || mode == 4);
            bool isUnEnabled = (mode == 1 || mode == 3 || mode == 5);
            bool isSnEnabled = (mode == 2 || mode == 4 || mode == 6);

            txtGP_U1.IsEnabled = isU1Enabled;
            borderGP_U1.Opacity = isU1Enabled ? 1.0 : 0.5;
            txtGP_Q.IsEnabled = isQEnabled;
            borderGP_Q.Opacity = isQEnabled ? 1.0 : 0.5;
            txtGP_N.IsEnabled = isNEnabled;
            borderGP_N.Opacity = isNEnabled ? 1.0 : 0.5;
            txtGP_Un.IsEnabled = isUnEnabled;
            borderGP_Un.Opacity = isUnEnabled ? 1.0 : 0.5;
            txtGP_Sn.IsEnabled = isSnEnabled;
            borderGP_Sn.Opacity = isSnEnabled ? 1.0 : 0.5;
        }

        private void AP_Changed(object sender, EventArgs e) { if (IsLoaded) SolveAP(); }
        private void GP_Changed(object sender, EventArgs e) { if (IsLoaded) SolveGP(); }

        // ═══ CSC: Cấp Số Cộng ═══
        private void SolveAP()
        {
            if (_isCalculating) return;
            apResultPanel.Children.Clear();

            int mode = cbAP_Mode?.SelectedIndex ?? 0;
            double u1 = 0, d = 0, un = 0, sn = 0;
            int n = 0;
            bool hasDualRoots = false;
            int nAlternate = 0;

            if (mode == 0 || mode == 3 || mode == 4 || mode == 5 || mode == 6)
            {
                if (!ParsingHelper.TryParseDouble(txtAP_U1?.Text, out u1))
                {
                    UI.ResultRow("⚠️ Vui lòng nhập số hạng đầu U₁ hợp lệ!", "#B71C1C", apResultPanel);
                    return;
                }
            }

            if (mode == 0 || mode == 1 || mode == 2 || mode == 5 || mode == 6)
            {
                if (!ParsingHelper.TryParseDouble(txtAP_D?.Text, out d))
                {
                    UI.ResultRow("⚠️ Vui lòng nhập công sai d hợp lệ!", "#B71C1C", apResultPanel);
                    return;
                }
            }

            if (mode == 0 || mode == 1 || mode == 2 || mode == 3 || mode == 4)
            {
                if (!int.TryParse(txtAP_N?.Text, out n) || n < 1 || n > 1000)
                {
                    UI.ResultRow("⚠️ Số số hạng n phải là số nguyên từ 1 đến 1000!", "#B71C1C", apResultPanel);
                    return;
                }
            }

            if (mode == 1 || mode == 3 || mode == 5)
            {
                if (!ParsingHelper.TryParseDouble(txtAP_Un?.Text, out un))
                {
                    UI.ResultRow("⚠️ Vui lòng nhập số hạng thứ n Uₙ hợp lệ!", "#B71C1C", apResultPanel);
                    return;
                }
            }

            if (mode == 2 || mode == 4 || mode == 6)
            {
                if (!ParsingHelper.TryParseDouble(txtAP_Sn?.Text, out sn))
                {
                    UI.ResultRow("⚠️ Vui lòng nhập tổng Sₙ hợp lệ!", "#B71C1C", apResultPanel);
                    return;
                }
            }

            string labelInfo = "";
            if (mode == 0) // Xuôi
            {
                un = u1 + (n - 1) * d;
                sn = n * (u1 + un) / 2.0;
                labelInfo = $"📝 CSC: U₁={UI.Fmt(u1)}, d={UI.Fmt(d)}, n={n}";
            }
            else if (mode == 1) // Tìm U1 biết d, n, Un
            {
                u1 = un - (n - 1) * d;
                sn = n * (u1 + un) / 2.0;
                labelInfo = $"📝 Giải ngược: Tìm U₁ khi biết d={UI.Fmt(d)}, n={n}, Uₙ={UI.Fmt(un)}";
            }
            else if (mode == 2) // Tìm U1 biết d, n, Sn
            {
                u1 = sn / n - (n - 1) * d / 2.0;
                un = u1 + (n - 1) * d;
                labelInfo = $"📝 Giải ngược: Tìm U₁ khi biết d={UI.Fmt(d)}, n={n}, Sₙ={UI.Fmt(sn)}";
            }
            else if (mode == 3) // Tìm d biết U1, n, Un
            {
                if (n <= 1)
                {
                    UI.ResultRow("⚠️ Không thể tìm công sai d khi n = 1!", "#B71C1C", apResultPanel);
                    return;
                }
                d = (un - u1) / (n - 1);
                sn = n * (u1 + un) / 2.0;
                labelInfo = $"📝 Giải ngược: Tìm d khi biết U₁={UI.Fmt(u1)}, n={n}, Uₙ={UI.Fmt(un)}";
            }
            else if (mode == 4) // Tìm d biết U1, n, Sn
            {
                if (n <= 1)
                {
                    UI.ResultRow("⚠️ Không thể tìm công sai d khi n = 1!", "#B71C1C", apResultPanel);
                    return;
                }
                d = 2.0 * (sn - n * u1) / (n * (n - 1));
                un = u1 + (n - 1) * d;
                labelInfo = $"📝 Giải ngược: Tìm d khi biết U₁={UI.Fmt(u1)}, n={n}, Sₙ={UI.Fmt(sn)}";
            }
            else if (mode == 5) // Tìm n biết U1, d, Un
            {
                if (System.Math.Abs(d) < 1e-12)
                {
                    if (System.Math.Abs(un - u1) < 1e-12)
                    {
                        UI.ResultRow("⚠️ d = 0 và U₁ = Uₙ, số số hạng n có thể là vô số!", "#B71C1C", apResultPanel);
                        return;
                    }
                    else
                    {
                        UI.ResultRow("⚠️ Không tồn tại cấp số thỏa mãn (d = 0 nhưng U₁ ≠ Uₙ)!", "#B71C1C", apResultPanel);
                        return;
                    }
                }
                double nVal = (un - u1) / d + 1;
                if (nVal < 1 || System.Math.Abs(nVal - System.Math.Round(nVal)) > 1e-9)
                {
                    UI.ResultRow($"⚠️ Không tồn tại cấp số thỏa mãn! (n = {nVal:G6} không nguyên dương)", "#B71C1C", apResultPanel);
                    return;
                }
                n = (int)System.Math.Round(nVal);
                if (n > 1000)
                {
                    UI.ResultRow("⚠️ Số số hạng n quá lớn (n > 1000)! Vui lòng điều chỉnh thông số để đảm bảo hiệu năng đồ thị.", "#B71C1C", apResultPanel);
                    return;
                }
                sn = n * (u1 + un) / 2.0;
                labelInfo = $"📝 Giải ngược: Tìm n khi biết U₁={UI.Fmt(u1)}, d={UI.Fmt(d)}, Uₙ={UI.Fmt(un)}";
            }
            else if (mode == 6) // Tìm n biết U1, d, Sn
            {
                if (System.Math.Abs(d) < 1e-12)
                {
                    if (System.Math.Abs(u1) < 1e-12)
                    {
                        UI.ResultRow("⚠️ U₁ = 0 và d = 0, số số hạng n có thể là bất kỳ số nào!", "#B71C1C", apResultPanel);
                        return;
                    }
                    double nVal = sn / u1;
                    if (nVal < 1 || System.Math.Abs(nVal - System.Math.Round(nVal)) > 1e-9)
                    {
                        UI.ResultRow($"⚠️ Không tồn tại cấp số thỏa mãn! (n = {nVal:G6} không nguyên dương)", "#B71C1C", apResultPanel);
                        return;
                    }
                    n = (int)System.Math.Round(nVal);
                }
                else
                {
                    // d * n^2 + (2*u1 - d)*n - 2*sn = 0
                    double a = d;
                    double b = 2.0 * u1 - d;
                    double c = -2.0 * sn;
                    double delta = b * b - 4.0 * a * c;
                    if (delta < 0)
                    {
                        UI.ResultRow("⚠️ Không tồn tại cấp số thỏa mãn! (Phương trình vô nghiệm thực)", "#B71C1C", apResultPanel);
                        return;
                    }
                    double sqrtDelta = System.Math.Sqrt(delta);
                    double r1 = (-b + sqrtDelta) / (2.0 * a);
                    double r2 = (-b - sqrtDelta) / (2.0 * a);

                    bool r1Valid = r1 >= 1 && System.Math.Abs(r1 - System.Math.Round(r1)) < 1e-9;
                    bool r2Valid = r2 >= 1 && System.Math.Abs(r2 - System.Math.Round(r2)) < 1e-9;

                    var validRoots = new List<int>();
                    if (r1Valid)
                    {
                        int r1Val = (int)System.Math.Round(r1);
                        if (!validRoots.Contains(r1Val)) validRoots.Add(r1Val);
                    }
                    if (r2Valid)
                    {
                        int r2Val = (int)System.Math.Round(r2);
                        if (!validRoots.Contains(r2Val)) validRoots.Add(r2Val);
                    }

                    if (validRoots.Count == 0)
                    {
                        UI.ResultRow($"⚠️ Không tồn tại cấp số thỏa mãn! (Nghiệm n₁={r1:G6}, n₂={r2:G6} không nguyên dương)", "#B71C1C", apResultPanel);
                        return;
                    }

                    validRoots.Sort();
                    if (validRoots.Count == 2)
                    {
                        hasDualRoots = true;
                        nAlternate = validRoots[0]; // smaller
                        n = validRoots[1]; // larger
                    }
                    else
                    {
                        n = validRoots[0];
                    }
                }
                if (n > 1000 || (hasDualRoots && nAlternate > 1000))
                {
                    UI.ResultRow("⚠️ Số số hạng n quá lớn (n > 1000)! Vui lòng điều chỉnh thông số để đảm bảo hiệu năng đồ thị.", "#B71C1C", apResultPanel);
                    return;
                }
                un = u1 + (n - 1) * d;
                labelInfo = $"📝 Giải ngược: Tìm n khi biết U₁={UI.Fmt(u1)}, d={UI.Fmt(d)}, Sₙ={UI.Fmt(sn)}";
            }

            _isCalculating = true;
            try
            {
                if (mode == 0)
                {
                    txtAP_Un.Text = UI.Fmt(un);
                    txtAP_Sn.Text = UI.Fmt(sn);
                }
                else if (mode == 1)
                {
                    txtAP_U1.Text = UI.Fmt(u1);
                    txtAP_Sn.Text = UI.Fmt(sn);
                }
                else if (mode == 2)
                {
                    txtAP_U1.Text = UI.Fmt(u1);
                    txtAP_Un.Text = UI.Fmt(un);
                }
                else if (mode == 3)
                {
                    txtAP_D.Text = UI.Fmt(d);
                    txtAP_Sn.Text = UI.Fmt(sn);
                }
                else if (mode == 4)
                {
                    txtAP_D.Text = UI.Fmt(d);
                    txtAP_Un.Text = UI.Fmt(un);
                }
                else if (mode == 5)
                {
                    txtAP_N.Text = n.ToString();
                    txtAP_Sn.Text = UI.Fmt(sn);
                }
                else if (mode == 6)
                {
                    txtAP_N.Text = n.ToString();
                    txtAP_Un.Text = UI.Fmt(un);
                }
            }
            finally
            {
                _isCalculating = false;
            }

            string dFraction = UI.RealToFraction(d);
            string dDisplay = string.IsNullOrEmpty(dFraction) ? UI.Fmt(d) : $"{UI.Fmt(d)} ({dFraction})";
            string dFormatedForFormula = d < 0 ? $"({dDisplay})" : dDisplay;
            labelInfo = labelInfo.Replace($"d={UI.Fmt(d)}", $"d={dDisplay}");

            UI.ResultRow(labelInfo, "#00695C", apResultPanel);

            if (hasDualRoots)
            {
                UI.ResultRow($"💡 Bài toán có 2 nghiệm nguyên dương hợp lệ: n = {n} và n = {nAlternate}", "#00796B", apResultPanel);
                UI.ResultRow($"👉 [Trường hợp 1 (mặc định)]: n = {n}", "#00695C", apResultPanel);
            }

            UI.ResultRow($"🔑 Uₙ = U₁ + (n−1)d = {UI.Fmt(u1)} + {n - 1}×{dFormatedForFormula} = {UI.Fmt(un)}", "#00897B", apResultPanel);
            UI.ResultRow($"🔑 Sₙ = n(U₁+Uₙ)/2 = {n}×({UI.Fmt(u1)}+{UI.Fmt(un)})/2 = {UI.Fmt(sn)}", "#26A69A", apResultPanel);

            // Liệt kê 5 số đầu
            var terms = new System.Text.StringBuilder("📋 Dãy: ");
            int show = System.Math.Min(n, 8);
            for (int i = 1; i <= show; i++)
                terms.Append($"{UI.Fmt(u1 + (i - 1) * d)}{(i < show ? ", " : "")}");
            if (n > 8) terms.Append($", ... , {UI.Fmt(un)}");
            UI.ResultRowScrolling(terms.ToString(), "#004D40", apResultPanel);

            if (hasDualRoots)
            {
                double unAlt = u1 + (nAlternate - 1) * d;
                double snAlt = nAlternate * (u1 + unAlt) / 2.0;

                UI.ResultRow($"👉 [Trường hợp 2]: n = {nAlternate}", "#00695C", apResultPanel);
                UI.ResultRow($"🔑 Uₙ = U₁ + (n−1)d = {UI.Fmt(u1)} + {nAlternate - 1}×{dFormatedForFormula} = {UI.Fmt(unAlt)}", "#00897B", apResultPanel);
                UI.ResultRow($"🔑 Sₙ = n(U₁+Uₙ)/2 = {nAlternate}×({UI.Fmt(u1)}+{UI.Fmt(unAlt)})/2 = {UI.Fmt(snAlt)}", "#26A69A", apResultPanel);

                var termsAlt = new System.Text.StringBuilder("📋 Dãy: ");
                int showAlt = System.Math.Min(nAlternate, 8);
                for (int i = 1; i <= showAlt; i++)
                    termsAlt.Append($"{UI.Fmt(u1 + (i - 1) * d)}{(i < showAlt ? ", " : "")}");
                if (nAlternate > 8) termsAlt.Append($", ... , {UI.Fmt(unAlt)}");
                UI.ResultRowScrolling(termsAlt.ToString(), "#004D40", apResultPanel);

                double u1Val = u1, dVal = d;
                int nAltVal = nAlternate;
                UI.PresetButton($"👈 Nạp trường hợp n = {nAlternate} để xem đồ thị", (Color)ColorConverter.ConvertFromString("#00796B"), () =>
                {
                    _isCalculating = true;
                    try
                    {
                        cbAP_Mode.SelectedIndex = 0; // Chuyển về bài toán xuôi
                        txtAP_U1.Text = UI.Fmt(u1Val);
                        txtAP_D.Text = UI.Fmt(dVal);
                        txtAP_N.Text = nAltVal.ToString();
                    }
                    finally
                    {
                        _isCalculating = false;
                    }
                    SolveAP();
                }, apResultPanel);
            }

            // Tính chất
            if (d > 0) UI.ResultRow("📈 Dãy tăng (d > 0)", "#1B5E20", apResultPanel);
            else if (d < 0) UI.ResultRow("📉 Dãy giảm (d < 0)", "#B71C1C", apResultPanel);
            else UI.ResultRow("➡️ Dãy hằng (d = 0)", "#757575", apResultPanel);

            AddAPToHistory(mode, u1, d, n, un, sn);
        }

        // ═══ CSN: Cấp Số Nhân ═══
        private void SolveGP()
        {
            if (_isCalculating) return;
            gpResultPanel.Children.Clear();

            int mode = cbGP_Mode?.SelectedIndex ?? 0;
            double u1 = 0, q = 0, un = 0, sn = 0;
            int n = 0;
            bool gpHasDualQ = false;
            double qAlternate = 0;

            if (mode == 0 || mode == 3 || mode == 4 || mode == 5 || mode == 6)
            {
                if (!ParsingHelper.TryParseDouble(txtGP_U1?.Text, out u1))
                {
                    UI.ResultRow("⚠️ Vui lòng nhập số hạng đầu U₁ hợp lệ!", "#B71C1C", gpResultPanel);
                    return;
                }
            }

            if (mode == 0 || mode == 1 || mode == 2 || mode == 5 || mode == 6)
            {
                if (!ParsingHelper.TryParseDouble(txtGP_Q?.Text, out q))
                {
                    UI.ResultRow("⚠️ Vui lòng nhập công bội q hợp lệ!", "#B71C1C", gpResultPanel);
                    return;
                }
            }

            if (mode == 0 || mode == 1 || mode == 2 || mode == 3 || mode == 4)
            {
                if (!int.TryParse(txtGP_N?.Text, out n) || n < 1 || n > 100)
                {
                    UI.ResultRow("⚠️ Số số hạng n phải là số nguyên từ 1 đến 100!", "#B71C1C", gpResultPanel);
                    return;
                }
            }

            if (mode == 1 || mode == 3 || mode == 5)
            {
                if (!ParsingHelper.TryParseDouble(txtGP_Un?.Text, out un))
                {
                    UI.ResultRow("⚠️ Vui lòng nhập số hạng thứ n Uₙ hợp lệ!", "#B71C1C", gpResultPanel);
                    return;
                }
            }

            if (mode == 2 || mode == 4 || mode == 6)
            {
                if (!ParsingHelper.TryParseDouble(txtGP_Sn?.Text, out sn))
                {
                    UI.ResultRow("⚠️ Vui lòng nhập tổng Sₙ hợp lệ!", "#B71C1C", gpResultPanel);
                    return;
                }
            }

            string labelInfo = "";
            if (mode == 0) // Xuôi
            {
                un = u1 * System.Math.Pow(q, n - 1);
                if (System.Math.Abs(q - 1) < 1e-12)
                    sn = n * u1;
                else
                    sn = u1 * (System.Math.Pow(q, n) - 1) / (q - 1);
                labelInfo = $"📝 CSN: U₁={UI.Fmt(u1)}, q={UI.Fmt(q)}, n={n}";
            }
            else if (mode == 1) // Tìm U1 biết q, n, Un
            {
                double divisor = System.Math.Pow(q, n - 1);
                if (System.Math.Abs(divisor) < 1e-12)
                {
                    UI.ResultRow("⚠️ Không thể tìm U₁ khi q = 0 và n > 1!", "#B71C1C", gpResultPanel);
                    return;
                }
                u1 = un / divisor;
                if (System.Math.Abs(q - 1) < 1e-12)
                    sn = n * u1;
                else
                    sn = u1 * (System.Math.Pow(q, n) - 1) / (q - 1);
                labelInfo = $"📝 Giải ngược: Tìm U₁ khi biết q={UI.Fmt(q)}, n={n}, Uₙ={UI.Fmt(un)}";
            }
            else if (mode == 2) // Tìm U1 biết q, n, Sn
            {
                if (System.Math.Abs(q - 1) < 1e-12)
                {
                    u1 = sn / n;
                }
                else
                {
                    double divisor = (System.Math.Pow(q, n) - 1) / (q - 1);
                    if (System.Math.Abs(divisor) < 1e-12)
                    {
                        UI.ResultRow("⚠️ qⁿ - 1 = 0, không thể chia để tìm U₁!", "#B71C1C", gpResultPanel);
                        return;
                    }
                    u1 = sn / divisor;
                }
                un = u1 * System.Math.Pow(q, n - 1);
                labelInfo = $"📝 Giải ngược: Tìm U₁ khi biết q={UI.Fmt(q)}, n={n}, Sₙ={UI.Fmt(sn)}";
            }
            else if (mode == 3) // Tìm q biết U1, n, Un
            {
                if (n <= 1)
                {
                    UI.ResultRow("⚠️ Không thể tìm công bội q khi n = 1!", "#B71C1C", gpResultPanel);
                    return;
                }
                if (System.Math.Abs(u1) < 1e-12)
                {
                    if (System.Math.Abs(un) < 1e-12)
                    {
                        UI.ResultRow("⚠️ U₁ = 0 và Uₙ = 0, công bội q có thể là bất kỳ số nào!", "#B71C1C", gpResultPanel);
                        return;
                    }
                    else
                    {
                        UI.ResultRow("⚠️ U₁ = 0 nhưng Uₙ ≠ 0, không tồn tại công bội q hợp lệ!", "#B71C1C", gpResultPanel);
                        return;
                    }
                }
                double ratio = un / u1;
                int power = n - 1;
                if (power % 2 == 0 && ratio < 0)
                {
                    UI.ResultRow($"⚠️ Tỉ số Uₙ/U₁ = {ratio:G6} < 0, không thể lấy căn bậc chẵn {power}!", "#B71C1C", gpResultPanel);
                    return;
                }
                if (ratio < 0)
                {
                    q = -System.Math.Pow(-ratio, 1.0 / power);
                }
                else
                {
                    q = System.Math.Pow(ratio, 1.0 / power);
                    if (power % 2 == 0 && ratio > 1e-9)
                    {
                        gpHasDualQ = true;
                        qAlternate = -q;
                    }
                }
                if (System.Math.Abs(q - 1) < 1e-12)
                    sn = n * u1;
                else
                    sn = u1 * (System.Math.Pow(q, n) - 1) / (q - 1);
                labelInfo = $"📝 Giải ngược: Tìm q khi biết U₁={UI.Fmt(u1)}, n={n}, Uₙ={UI.Fmt(un)}";
            }
            else if (mode == 4) // Tìm q biết U1, n, Sn
            {
                if (n <= 1)
                {
                    UI.ResultRow("⚠️ Không thể tìm công bội q khi n = 1!", "#B71C1C", gpResultPanel);
                    return;
                }
                if (System.Math.Abs(u1) < 1e-12)
                {
                    UI.ResultRow("⚠️ U₁ = 0, Sₙ = 0, công bội q có thể là bất kỳ số nào!", "#B71C1C", gpResultPanel);
                    return;
                }
                double targetRatio = sn / u1;
                Func<double, (bool Solved, double Value)> solveNewton = (startGuess) =>
                {
                    double currentQ = startGuess;
                    bool ok = false;
                    for (int iter = 0; iter < 100; iter++)
                    {
                        double h = 0;
                        double dh = 0;
                        for (int i = n - 1; i >= 0; i--)
                        {
                            dh = dh * currentQ + h;
                            h = h * currentQ + 1.0;
                        }
                        h -= targetRatio;
                        if (System.Math.Abs(dh) < 1e-12) break;
                        double nextQ = currentQ - h / dh;
                        if (System.Math.Abs(nextQ - currentQ) < 1e-9)
                        {
                            currentQ = nextQ;
                            double checkH = 0;
                            for (int i = n - 1; i >= 0; i--)
                            {
                                checkH = checkH * currentQ + 1.0;
                            }
                            checkH -= targetRatio;
                            if (System.Math.Abs(checkH) < 1e-6)
                            {
                                ok = true;
                            }
                            break;
                        }
                        currentQ = nextQ;
                    }
                    return (ok && !double.IsNaN(currentQ) && !double.IsInfinity(currentQ), currentQ);
                };

                var res1 = solveNewton(targetRatio > n ? 1.5 : (targetRatio > 0 ? 0.5 : -0.5));
                var res2 = solveNewton(targetRatio > n ? -1.5 : (targetRatio > 0 ? -0.5 : -1.5));

                if (res1.Solved && res2.Solved && System.Math.Abs(res1.Value - res2.Value) > 1e-5)
                {
                    if (res1.Value >= res2.Value)
                    {
                        q = res1.Value;
                        qAlternate = res2.Value;
                    }
                    else
                    {
                        q = res2.Value;
                        qAlternate = res1.Value;
                    }
                    gpHasDualQ = true;
                }
                else if (res1.Solved)
                {
                    q = res1.Value;
                }
                else if (res2.Solved)
                {
                    q = res2.Value;
                }
                else
                {
                    UI.ResultRow("⚠️ Không tìm thấy nghiệm thực cho công bội q!", "#B71C1C", gpResultPanel);
                    return;
                }

                un = u1 * System.Math.Pow(q, n - 1);
                labelInfo = $"📝 Giải ngược: Tìm q khi biết U₁={UI.Fmt(u1)}, n={n}, Sₙ={UI.Fmt(sn)}";
            }
            else if (mode == 5) // Tìm n biết U1, q, Un
            {
                if (System.Math.Abs(u1) < 1e-12)
                {
                    UI.ResultRow("⚠️ U₁ = 0, không thể dùng phép chia để tìm n!", "#B71C1C", gpResultPanel);
                    return;
                }
                if (System.Math.Abs(q - 1) < 1e-12)
                {
                    UI.ResultRow("⚠️ Khi công bội q = 1, các số hạng của cấp số nhân đều bằng nhau (dãy hằng). Do đó, không thể xác định duy nhất số số hạng n từ U₁ và Uₙ.", "#B71C1C", gpResultPanel);
                    return;
                }

                if (System.Math.Abs(q + 1.0) < 1e-12)
                {
                    if (System.Math.Abs(un - u1) < 1e-12)
                    {
                        UI.ResultRow("⚠️ Với q = -1 và Uₙ = U₁, số số hạng n có thể là số lẻ bất kỳ (1, 3, 5,...). Mặc định chọn n = 1.", "#00695C", gpResultPanel);
                        n = 1;
                    }
                    else if (System.Math.Abs(un + u1) < 1e-12)
                    {
                        UI.ResultRow("⚠️ Với q = -1 và Uₙ = -U₁, số số hạng n có thể là số chẵn bất kỳ (2, 4, 6,...). Mặc định chọn n = 2.", "#00695C", gpResultPanel);
                        n = 2;
                    }
                    else
                    {
                        UI.ResultRow("⚠️ Không tồn tại cấp số thỏa mãn! (Với q = -1, Uₙ chỉ có thể bằng U₁ hoặc -U₁)", "#B71C1C", gpResultPanel);
                        return;
                    }
                }
                else
                {
                    double ratio = un / u1;
                    double absQ = System.Math.Abs(q);
                    double absRatio = System.Math.Abs(ratio);

                    if (absRatio <= 0 || absQ <= 0 || System.Math.Abs(absQ - 1) < 1e-12)
                    {
                        UI.ResultRow("⚠️ Không thể tính n do giá trị không hợp lệ!", "#B71C1C", gpResultPanel);
                        return;
                    }

                    double nVal = System.Math.Log(absRatio, absQ) + 1;
                    if (nVal < 1 || System.Math.Abs(nVal - System.Math.Round(nVal)) > 1e-9)
                    {
                        UI.ResultRow($"⚠️ Không tồn tại cấp số thỏa mãn! (n = {nVal:G6} không nguyên dương)", "#B71C1C", gpResultPanel);
                        return;
                    }
                    n = (int)System.Math.Round(nVal);

                    if (q < 0)
                    {
                        double expectedRatio = System.Math.Pow(q, n - 1);
                        if (System.Math.Abs(expectedRatio - ratio) > 1e-9)
                        {
                            UI.ResultRow("⚠️ Dấu của Uₙ và U₁ không khớp với số mũ của công bội q âm!", "#B71C1C", gpResultPanel);
                            return;
                        }
                    }
                }

                if (n > 100)
                {
                    UI.ResultRow("⚠️ Số số hạng n quá lớn (n > 100)! Vui lòng điều chỉnh thông số để đảm bảo hiệu năng đồ thị.", "#B71C1C", gpResultPanel);
                    return;
                }

                if (System.Math.Abs(q - 1) < 1e-12)
                    sn = n * u1;
                else
                    sn = u1 * (System.Math.Pow(q, n) - 1) / (q - 1);
                labelInfo = $"📝 Giải ngược: Tìm n khi biết U₁={UI.Fmt(u1)}, q={UI.Fmt(q)}, Uₙ={UI.Fmt(un)}";
            }
            else if (mode == 6) // Tìm n biết U1, q, Sn
            {
                if (System.Math.Abs(u1) < 1e-12)
                {
                    if (System.Math.Abs(sn) < 1e-12)
                    {
                        UI.ResultRow("⚠️ U₁ = 0 và Sₙ = 0, số số hạng n có thể là bất kỳ số nguyên dương nào!", "#B71C1C", gpResultPanel);
                        return;
                    }
                    else
                    {
                        UI.ResultRow("⚠️ U₁ = 0 nhưng Sₙ ≠ 0, không tồn tại cấp số thỏa mãn!", "#B71C1C", gpResultPanel);
                        return;
                    }
                }

                if (System.Math.Abs(q - 1.0) < 1e-12)
                {
                    double nVal = sn / u1;
                    if (nVal < 1 || System.Math.Abs(nVal - System.Math.Round(nVal)) > 1e-9)
                    {
                        UI.ResultRow($"⚠️ Không tồn tại cấp số thỏa mãn! (n = {nVal:G6} không nguyên dương)", "#B71C1C", gpResultPanel);
                        return;
                    }
                    n = (int)System.Math.Round(nVal);
                }
                else if (System.Math.Abs(q + 1.0) < 1e-12)
                {
                    if (System.Math.Abs(sn - u1) < 1e-12)
                    {
                        UI.ResultRow("⚠️ Với q = -1 và Sₙ = U₁, số số hạng n có thể là số lẻ bất kỳ (1, 3, 5,...). Mặc định chọn n = 1.", "#00695C", gpResultPanel);
                        n = 1;
                    }
                    else if (System.Math.Abs(sn) < 1e-12)
                    {
                        UI.ResultRow("⚠️ Với q = -1 và Sₙ = 0, số số hạng n có thể là số chẵn bất kỳ (2, 4, 6,...). Mặc định chọn n = 2.", "#00695C", gpResultPanel);
                        n = 2;
                    }
                    else
                    {
                        UI.ResultRow("⚠️ Không tồn tại cấp số thỏa mãn! (Với q = -1, Sₙ chỉ có thể bằng U₁ hoặc 0)", "#B71C1C", gpResultPanel);
                        return;
                    }
                }
                else if (System.Math.Abs(q) < 1e-12)
                {
                    if (System.Math.Abs(sn - u1) < 1e-12)
                    {
                        UI.ResultRow("⚠️ q = 0 và Sₙ = U₁, số số hạng n có thể là bất kỳ số nguyên dương nào!", "#B71C1C", gpResultPanel);
                        return;
                    }
                    else
                    {
                        UI.ResultRow("⚠️ Không tồn tại cấp số thỏa mãn! (q = 0 nhưng Sₙ ≠ U₁)", "#B71C1C", gpResultPanel);
                        return;
                    }
                }
                else
                {
                    double Y = 1.0 + (sn * (q - 1.0)) / u1;
                    if (q > 0)
                    {
                        if (Y <= 0)
                        {
                            UI.ResultRow("⚠️ Không tồn tại cấp số thỏa mãn! (Biểu thức logarit không dương)", "#B71C1C", gpResultPanel);
                            return;
                        }
                        double nVal = System.Math.Log(Y, q);
                        if (nVal < 1 || System.Math.Abs(nVal - System.Math.Round(nVal)) > 1e-9)
                        {
                            UI.ResultRow($"⚠️ Không tồn tại cấp số thỏa mãn! (n = {nVal:G6} không nguyên dương)", "#B71C1C", gpResultPanel);
                            return;
                        }
                        n = (int)System.Math.Round(nVal);
                    }
                    else // q < 0
                    {
                        if (System.Math.Abs(Y) < 1e-12)
                        {
                            UI.ResultRow("⚠️ Không tồn tại cấp số thỏa mãn! (Biểu thức logarit bằng 0)", "#B71C1C", gpResultPanel);
                            return;
                        }
                        double absQ = System.Math.Abs(q);
                        double absY = System.Math.Abs(Y);
                        double nVal = System.Math.Log(absY, absQ);
                        if (nVal < 1 || System.Math.Abs(nVal - System.Math.Round(nVal)) > 1e-9)
                        {
                            UI.ResultRow($"⚠️ Không tồn tại cấp số thỏa mãn! (n = {nVal:G6} không nguyên dương)", "#B71C1C", gpResultPanel);
                            return;
                        }
                        n = (int)System.Math.Round(nVal);
                        double expectedY = System.Math.Pow(q, n);
                        if (System.Math.Abs(expectedY - Y) > 1e-9)
                        {
                            UI.ResultRow("⚠️ Dấu của Sₙ không khớp với lũy thừa của công bội q âm!", "#B71C1C", gpResultPanel);
                            return;
                        }
                    }
                }
                if (n > 100)
                {
                    UI.ResultRow("⚠️ Số số hạng n quá lớn (n > 100)! Vui lòng điều chỉnh thông số để đảm bảo hiệu năng đồ thị.", "#B71C1C", gpResultPanel);
                    return;
                }
                un = u1 * System.Math.Pow(q, n - 1);
                labelInfo = $"📝 Giải ngược: Tìm n khi biết U₁={UI.Fmt(u1)}, q={UI.Fmt(q)}, Sₙ={UI.Fmt(sn)}";
            }

            _isCalculating = true;
            try
            {
                if (mode == 0)
                {
                    txtGP_Un.Text = UI.Fmt(un);
                    txtGP_Sn.Text = UI.Fmt(sn);
                }
                else if (mode == 1)
                {
                    txtGP_U1.Text = UI.Fmt(u1);
                    txtGP_Sn.Text = UI.Fmt(sn);
                }
                else if (mode == 2)
                {
                    txtGP_U1.Text = UI.Fmt(u1);
                    txtGP_Un.Text = UI.Fmt(un);
                }
                else if (mode == 3)
                {
                    txtGP_Q.Text = UI.Fmt(q);
                    txtGP_Sn.Text = UI.Fmt(sn);
                }
                else if (mode == 4)
                {
                    txtGP_Q.Text = UI.Fmt(q);
                    txtGP_Un.Text = UI.Fmt(un);
                }
                else if (mode == 5)
                {
                    txtGP_N.Text = n.ToString();
                    txtGP_Sn.Text = UI.Fmt(sn);
                }
                else if (mode == 6)
                {
                    txtGP_N.Text = n.ToString();
                    txtGP_Un.Text = UI.Fmt(un);
                }
            }
            finally
            {
                _isCalculating = false;
            }

            string qFraction = UI.RealToFraction(q);
            string qDisplay = string.IsNullOrEmpty(qFraction) ? UI.Fmt(q) : $"{UI.Fmt(q)} ({qFraction})";
            string qDisplayFmt = q < 0 ? $"({qDisplay})" : qDisplay;
            labelInfo = labelInfo.Replace($"q={UI.Fmt(q)}", $"q={qDisplay}");

            UI.ResultRow(labelInfo, "#BF360C", gpResultPanel);

            if (gpHasDualQ)
            {
                UI.ResultRow($"💡 Bài toán có 2 nghiệm công bội hợp lệ: q = {UI.Fmt(q)} và q = {UI.Fmt(qAlternate)}", "#E64A19", gpResultPanel);
                UI.ResultRow($"👉 [Trường hợp 1 (mặc định)]: q = {UI.Fmt(q)}", "#BF360C", gpResultPanel);
            }

            string qFmt = q < 0 ? $"({UI.Fmt(q)})" : UI.Fmt(q);
            string u1Fmt = u1 < 0 ? $"({UI.Fmt(u1)})" : UI.Fmt(u1);
            UI.ResultRow($"🔑 Uₙ = U₁·qⁿ⁻¹ = {UI.Fmt(u1)}×{qDisplayFmt}{UI.ToSuperscript(n - 1)} = {UI.Fmt(un)}", "#D84315", gpResultPanel);
            if (System.Math.Abs(q - 1) < 1e-12)
            {
                UI.ResultRow($"🔑 Sₙ = n·U₁ = {n}×{UI.Fmt(u1)} = {UI.Fmt(sn)}", "#E64A19", gpResultPanel);
            }
            else
            {
                string u1Mult = u1 == 1 ? "" : (u1 == -1 ? "-" : $"{u1Fmt}×");
                UI.ResultRow($"🔑 Sₙ = U₁(qⁿ−1)/(q−1) = {u1Mult}({qDisplayFmt}{UI.ToSuperscript(n)}−1)/({qDisplayFmt}−1) = {UI.Fmt(sn)}", "#E64A19", gpResultPanel);
            }

            var terms = new System.Text.StringBuilder("📋 Dãy: ");
            int show = System.Math.Min(n, 8);
            for (int i = 1; i <= show; i++)
                terms.Append($"{UI.Fmt(u1 * System.Math.Pow(q, i - 1))}{(i < show ? ", " : "")}");
            if (n > 8) terms.Append($", ... , {UI.Fmt(un)}");
            UI.ResultRowScrolling(terms.ToString(), "#4E342E", gpResultPanel);

            if (System.Math.Abs(q) < 1)
                UI.ResultRow($"♾️ |q|<1 → Tổng vô hạn S∞ = U₁/(1−q) = {UI.Fmt(u1 / (1 - q))}", "#1565C0", gpResultPanel);
            if (q > 1) UI.ResultRow("📈 Dãy tăng theo hàm mũ", "#1B5E20", gpResultPanel);
            else if (q < -1) UI.ResultRow("🔄 Dãy dao động phân kỳ", "#C62828", gpResultPanel);

            if (gpHasDualQ)
            {
                double qAlt = qAlternate;
                double unAlt = u1 * System.Math.Pow(qAlt, n - 1);
                double snAlt = 0;
                if (System.Math.Abs(qAlt - 1) < 1e-12)
                    snAlt = n * u1;
                else
                    snAlt = u1 * (System.Math.Pow(qAlt, n) - 1.0) / (qAlt - 1.0);

                string qAltFraction = UI.RealToFraction(qAlt);
                string qAltDisplay = string.IsNullOrEmpty(qAltFraction) ? UI.Fmt(qAlt) : $"{UI.Fmt(qAlt)} ({qAltFraction})";
                string qAltDisplayFmt = qAlt < 0 ? $"({qAltDisplay})" : qAltDisplay;

                UI.ResultRow($"👉 [Trường hợp 2]: q = {UI.Fmt(qAlt)}", "#BF360C", gpResultPanel);
                UI.ResultRow($"🔑 Uₙ = U₁·qⁿ⁻¹ = {UI.Fmt(u1)}×{qAltDisplayFmt}{UI.ToSuperscript(n - 1)} = {UI.Fmt(unAlt)}", "#D84315", gpResultPanel);
                if (System.Math.Abs(qAlt - 1) < 1e-12)
                {
                    UI.ResultRow($"🔑 Sₙ = n·U₁ = {n}×{UI.Fmt(u1)} = {UI.Fmt(snAlt)}", "#E64A19", gpResultPanel);
                }
                else
                {
                    string u1Mult = u1 == 1 ? "" : (u1 == -1 ? "-" : $"{u1Fmt}×");
                    UI.ResultRow($"🔑 Sₙ = U₁(qⁿ−1)/(q−1) = {u1Mult}({qAltDisplayFmt}{UI.ToSuperscript(n)}−1)/({qAltDisplayFmt}−1) = {UI.Fmt(snAlt)}", "#E64A19", gpResultPanel);
                }

                var termsAlt = new System.Text.StringBuilder("📋 Dãy: ");
                int showAlt = System.Math.Min(n, 8);
                for (int i = 1; i <= showAlt; i++)
                    termsAlt.Append($"{UI.Fmt(u1 * System.Math.Pow(qAlt, i - 1))}{(i < showAlt ? ", " : "")}");
                if (n > 8) termsAlt.Append($", ... , {UI.Fmt(unAlt)}");
                UI.ResultRowScrolling(termsAlt.ToString(), "#4E342E", gpResultPanel);

                if (System.Math.Abs(qAlt) < 1)
                    UI.ResultRow($"♾️ |q|<1 → Tổng vô hạn S∞ = U₁/(1−q) = {UI.Fmt(u1 / (1 - qAlt))}", "#1565C0", gpResultPanel);
                if (qAlt > 1) UI.ResultRow("📈 Dãy tăng theo hàm mũ", "#1B5E20", gpResultPanel);
                else if (qAlt < -1) UI.ResultRow("🔄 Dãy dao động phân kỳ", "#C62828", gpResultPanel);

                double u1Val = u1;
                int nVal = n;
                UI.PresetButton($"👈 Nạp trường hợp q = {UI.Fmt(qAlt)} để xem đồ thị", (Color)ColorConverter.ConvertFromString("#BF360C"), () =>
                {
                    _isCalculating = true;
                    try
                    {
                        cbGP_Mode.SelectedIndex = 0; // Chuyển về bài toán xuôi
                        txtGP_U1.Text = UI.Fmt(u1Val);
                        txtGP_Q.Text = UI.Fmt(qAlt);
                        txtGP_N.Text = nVal.ToString();
                    }
                    finally
                    {
                        _isCalculating = false;
                    }
                    SolveGP();
                }, gpResultPanel);
            }

            AddGPToHistory(mode, u1, q, n, un, sn);
        }

        // ═══ Graph ═══
        private void OpenGraph_AP(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ParsingHelper.TryParseDouble(txtAP_U1?.Text, out double u1);
                ParsingHelper.TryParseDouble(txtAP_D?.Text, out double d);
                int.TryParse(txtAP_N?.Text, out int n);
                var ci = CultureInfo.InvariantCulture;
                string expJs = $@"
        calc.setExpression({{id:'f', latex:'f(x)={u1.ToString(ci)}+({d.ToString(ci)})(x-1)', color:'#00695C', lineWidth:2, lineStyle:'DASHED'}});
        calc.setExpression({{id:'pts', latex:'(n, {u1.ToString(ci)}+({d.ToString(ci)})(n-1))', color:'#00695C', pointSize:8, pointStyle:'POINT'}});
        calc.setExpression({{id:'slider', latex:'n=[1,...,{n}]'}});
        calc.setMathBounds({{ left: 0, right: {n + 2}, bottom: {System.Math.Min(u1, u1 + (n - 1) * d) - 5}, top: {System.Math.Max(u1, u1 + (n - 1) * d) + 5} }});";
                var win = new GraphWindow(expJs, $"📏 CSC: U₁={u1}, d={d}");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        public string GetGPGraphScript(double u1, double q, int n)
        {
            var ci = CultureInfo.InvariantCulture;
            double maxVal = System.Math.Max(System.Math.Abs(u1), System.Math.Abs(u1 * System.Math.Pow(q, n - 1)));
            string qStr = q < 0 ? $"({q.ToString(ci)})" : q.ToString(ci);
            string expJs = $@"
        calc.setExpression({{id:'f', latex:'f(x)={u1.ToString(ci)}\\cdot {qStr}^{{(x-1)}}', color:'#BF360C', lineWidth:2, lineStyle:'DASHED'}});
        calc.setExpression({{id:'pts', latex:'(n, {u1.ToString(ci)}\\cdot {qStr}^{{(n-1)}})', color:'#BF360C', pointSize:8, pointStyle:'POINT'}});
        calc.setExpression({{id:'slider', latex:'n=[1,...,{n}]'}});
        calc.setMathBounds({{ left: 0, right: {n + 2}, bottom: {(-maxVal * 0.2).ToString(ci)}, top: {(maxVal * 1.3).ToString(ci)} }});";
            if (System.Math.Abs(q) < 1.0)
            {
                double sInfinity = u1 / (1.0 - q);
                expJs += $"\n        calc.setExpression({{id:'sinf', latex:'y={sInfinity.ToString(ci)}', color:'#1565C0', lineWidth:1.5, lineStyle:'DASHED'}});";
            }
            return expJs;
        }

        private void OpenGraph_GP(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ParsingHelper.TryParseDouble(txtGP_U1?.Text, out double u1);
                ParsingHelper.TryParseDouble(txtGP_Q?.Text, out double q);
                int.TryParse(txtGP_N?.Text, out int n);
                string expJs = GetGPGraphScript(u1, q, n);
                var win = new GraphWindow(expJs, $"📊 CSN: U₁={u1}, q={q}");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        // ═══ PRESETS ═══
        private void BuildAPPresets()
        {
            if (apPresetPanel == null) return;
            var presets = new (string L, double U, double D, int N)[]
            {
                ("U₁=1, d=2, n=10", 1, 2, 10),
                ("U₁=100, d=−5, n=20", 100, -5, 20),
                ("U₁=3, d=7, n=15", 3, 7, 15),
            };
            foreach (var (l, u, d, n) in presets)
            {
                double cu = u, cd = d; int cn = n;
                UI.PresetButton(l, (Color)ColorConverter.ConvertFromString("#00695C"),
                    () => { txtAP_U1.Text = UI.Fmt(cu); txtAP_D.Text = UI.Fmt(cd); txtAP_N.Text = cn.ToString(); },
                    apPresetPanel);
            }
        }

        private void BuildGPPresets()
        {
            if (gpPresetPanel == null) return;
            var presets = new (string L, double U, double Q, int N)[]
            {
                ("U₁=2, q=3, n=8", 2, 3, 8),
                ("U₁=1, q=0.5, n=10", 1, 0.5, 10),
                ("U₁=5, q=−2, n=6", 5, -2, 6),
            };
            foreach (var (l, u, q, n) in presets)
            {
                double cu = u, cq = q; int cn = n;
                UI.PresetButton(l, (Color)ColorConverter.ConvertFromString("#BF360C"),
                    () => { txtGP_U1.Text = UI.Fmt(cu); txtGP_Q.Text = UI.Fmt(cq); txtGP_N.Text = cn.ToString(); },
                    gpPresetPanel);
            }
        }

        // ═══ HELPERS ═══
        // AddResult(), MakePresetButton(), Fmt() đã được thay thế bằng UI.ResultRow(), UI.PresetButton(), UI.Fmt()

        private void AddAPToHistory(int mode, double u1, double d, int n, double un, double sn)
        {
            _apHistory.RemoveAll(x => x.Mode == mode &&
                                      System.Math.Abs(x.U1 - u1) < 1e-9 &&
                                      System.Math.Abs(x.D - d) < 1e-9 &&
                                      x.N == n &&
                                      System.Math.Abs(x.Un - un) < 1e-9 &&
                                      System.Math.Abs(x.Sn - sn) < 1e-9);

            _apHistory.Add(new APConfig
            {
                Mode = mode,
                U1 = u1,
                D = d,
                N = n,
                Un = un,
                Sn = sn
            });

            if (_apHistory.Count > 5)
            {
                _apHistory.RemoveAt(0);
            }

            UpdateAPHistoryUI();
            SaveHistoryToDisk();
        }

        private void UpdateAPHistoryUI()
        {
            if (apHistoryPanel == null || borderAP_History == null) return;
            apHistoryPanel.Children.Clear();
            
            if (_apHistory.Count == 0)
            {
                borderAP_History.Visibility = Visibility.Collapsed;
                return;
            }
            
            borderAP_History.Visibility = Visibility.Visible;
            
            for (int i = _apHistory.Count - 1; i >= 0; i--)
            {
                var cfg = _apHistory[i];
                string label = "";
                switch (cfg.Mode)
                {
                    case 0: label = $"U₁={UI.Fmt(cfg.U1)}, d={UI.Fmt(cfg.D)}, n={cfg.N}"; break;
                    case 1: label = $"d={UI.Fmt(cfg.D)}, n={cfg.N}, Uₙ={UI.Fmt(cfg.Un)}"; break;
                    case 2: label = $"d={UI.Fmt(cfg.D)}, n={cfg.N}, Sₙ={UI.Fmt(cfg.Sn)}"; break;
                    case 3: label = $"U₁={UI.Fmt(cfg.U1)}, n={cfg.N}, Uₙ={UI.Fmt(cfg.Un)}"; break;
                    case 4: label = $"U₁={UI.Fmt(cfg.U1)}, n={cfg.N}, Sₙ={UI.Fmt(cfg.Sn)}"; break;
                    case 5: label = $"U₁={UI.Fmt(cfg.U1)}, d={UI.Fmt(cfg.D)}, Uₙ={UI.Fmt(cfg.Un)}"; break;
                    case 6: label = $"U₁={UI.Fmt(cfg.U1)}, d={UI.Fmt(cfg.D)}, Sₙ={UI.Fmt(cfg.Sn)}"; break;
                }
                
                UI.PresetButton(label, (Color)ColorConverter.ConvertFromString("#00796B"), () =>
                {
                    if (_isCalculating) return;
                    _isCalculating = true;
                    try
                    {
                        cbAP_Mode.SelectedIndex = cfg.Mode;
                        txtAP_U1.Text = UI.Fmt(cfg.U1);
                        txtAP_D.Text = UI.Fmt(cfg.D);
                        txtAP_N.Text = cfg.N.ToString();
                        txtAP_Un.Text = UI.Fmt(cfg.Un);
                        txtAP_Sn.Text = UI.Fmt(cfg.Sn);
                    }
                    finally
                    {
                        _isCalculating = false;
                    }
                    SolveAP();
                }, apHistoryPanel);
            }
        }

        private void AddGPToHistory(int mode, double u1, double q, int n, double un, double sn)
        {
            _gpHistory.RemoveAll(x => x.Mode == mode &&
                                      System.Math.Abs(x.U1 - u1) < 1e-9 &&
                                      System.Math.Abs(x.Q - q) < 1e-9 &&
                                      x.N == n &&
                                      System.Math.Abs(x.Un - un) < 1e-9 &&
                                      System.Math.Abs(x.Sn - sn) < 1e-9);

            _gpHistory.Add(new GPConfig
            {
                Mode = mode,
                U1 = u1,
                Q = q,
                N = n,
                Un = un,
                Sn = sn
            });

            if (_gpHistory.Count > 5)
            {
                _gpHistory.RemoveAt(0);
            }

            UpdateGPHistoryUI();
            SaveHistoryToDisk();
        }

        private void UpdateGPHistoryUI()
        {
            if (gpHistoryPanel == null || borderGP_History == null) return;
            gpHistoryPanel.Children.Clear();

            if (_gpHistory.Count == 0)
            {
                borderGP_History.Visibility = Visibility.Collapsed;
                return;
            }

            borderGP_History.Visibility = Visibility.Visible;

            for (int i = _gpHistory.Count - 1; i >= 0; i--)
            {
                var cfg = _gpHistory[i];
                string label = "";
                switch (cfg.Mode)
                {
                    case 0: label = $"U₁={UI.Fmt(cfg.U1)}, q={UI.Fmt(cfg.Q)}, n={cfg.N}"; break;
                    case 1: label = $"q={UI.Fmt(cfg.Q)}, n={cfg.N}, Uₙ={UI.Fmt(cfg.Un)}"; break;
                    case 2: label = $"q={UI.Fmt(cfg.Q)}, n={cfg.N}, Sₙ={UI.Fmt(cfg.Sn)}"; break;
                    case 3: label = $"U₁={UI.Fmt(cfg.U1)}, n={cfg.N}, Uₙ={UI.Fmt(cfg.Un)}"; break;
                    case 4: label = $"U₁={UI.Fmt(cfg.U1)}, n={cfg.N}, Sₙ={UI.Fmt(cfg.Sn)}"; break;
                    case 5: label = $"U₁={UI.Fmt(cfg.U1)}, q={UI.Fmt(cfg.Q)}, Uₙ={UI.Fmt(cfg.Un)}"; break;
                    case 6: label = $"U₁={UI.Fmt(cfg.U1)}, q={UI.Fmt(cfg.Q)}, Sₙ={UI.Fmt(cfg.Sn)}"; break;
                }

                UI.PresetButton(label, (Color)ColorConverter.ConvertFromString("#D84315"), () =>
                {
                    if (_isCalculating) return;
                    _isCalculating = true;
                    try
                    {
                        cbGP_Mode.SelectedIndex = cfg.Mode;
                        txtGP_U1.Text = UI.Fmt(cfg.U1);
                        txtGP_Q.Text = UI.Fmt(cfg.Q);
                        txtGP_N.Text = cfg.N.ToString();
                        txtGP_Un.Text = UI.Fmt(cfg.Un);
                        txtGP_Sn.Text = UI.Fmt(cfg.Sn);
                    }
                    finally
                    {
                        _isCalculating = false;
                    }
                    SolveGP();
                }, gpHistoryPanel);
            }
        }

        private static string GetHistoryFilePath()
        {
            string dir = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "QASmartClass");
            return System.IO.Path.Combine(dir, "sequence_history.json");
        }

        private void SaveHistoryToDisk()
        {
            try
            {
                string filePath = GetHistoryFilePath();
                string dir = System.IO.Path.GetDirectoryName(filePath);
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                var data = new HistoryData
                {
                    ApHistory = _apHistory,
                    GpHistory = _gpHistory
                };
                string json = System.Text.Json.JsonSerializer.Serialize(data);
                System.IO.File.WriteAllText(filePath, json);
            }
            catch
            {
                // Ignore IO errors
            }
        }

        private void LoadHistoryFromDisk()
        {
            try
            {
                string filePath = GetHistoryFilePath();
                if (System.IO.File.Exists(filePath))
                {
                    string json = System.IO.File.ReadAllText(filePath);
                    var data = System.Text.Json.JsonSerializer.Deserialize<HistoryData>(json);
                    if (data != null)
                    {
                        _apHistory = data.ApHistory ?? new();
                        _gpHistory = data.GpHistory ?? new();
                    }
                }
            }
            catch
            {
                // Ignore deserialization/IO errors
            }
        }

        private void GenerateAPQuiz()
        {
            if (lblAP_QuizQuestion == null || txtAP_QuizAnswer == null || lblAP_QuizFeedback == null) return;
            
            txtAP_QuizAnswer.Text = "";
            lblAP_QuizFeedback.Visibility = Visibility.Collapsed;

            int type = _random.Next(2); // 0: Tìm Un, 1: Tìm Sn
            double u1 = _random.Next(1, 11);
            double d = _random.Next(2, 7);
            int n = _random.Next(3, 11);

            if (type == 0)
            {
                _apQuizCorrectAnswer = u1 + (n - 1) * d;
                _apQuizQuestionText = $"Cho cấp số cộng có U₁ = {u1}, d = {d}. Tìm số hạng U{UI.ToSubscript(n)}?";
            }
            else
            {
                _apQuizCorrectAnswer = n * (2 * u1 + (n - 1) * d) / 2.0;
                _apQuizQuestionText = $"Cho cấp số cộng có U₁ = {u1}, d = {d}. Tìm tổng S{UI.ToSubscript(n)}?";
            }

            lblAP_QuizQuestion.Text = _apQuizQuestionText;
        }

        private void GenerateGPQuiz()
        {
            if (lblGP_QuizQuestion == null || txtGP_QuizAnswer == null || lblGP_QuizFeedback == null) return;

            txtGP_QuizAnswer.Text = "";
            lblGP_QuizFeedback.Visibility = Visibility.Collapsed;

            int type = _random.Next(2); // 0: Tìm Un, 1: Tìm Sn
            double u1 = _random.Next(1, 6);
            double q = _random.Next(2, 5);
            int n = type == 0 ? _random.Next(3, 7) : _random.Next(3, 6); // Keep Sn values small to prevent overflow

            if (type == 0)
            {
                _gpQuizCorrectAnswer = u1 * System.Math.Pow(q, n - 1);
                _gpQuizQuestionText = $"Cho cấp số nhân có U₁ = {u1}, q = {q}. Tìm số hạng U{UI.ToSubscript(n)}?";
            }
            else
            {
                _gpQuizCorrectAnswer = u1 * (System.Math.Pow(q, n) - 1.0) / (q - 1.0);
                _gpQuizQuestionText = $"Cho cấp số nhân có U₁ = {u1}, q = {q}. Tìm tổng S{UI.ToSubscript(n)}?";
            }

            lblGP_QuizQuestion.Text = _gpQuizQuestionText;
        }

        private void APQuizCheck_Click(object sender, RoutedEventArgs e)
        {
            if (lblAP_QuizFeedback == null) return;
            string input = txtAP_QuizAnswer.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                lblAP_QuizFeedback.Text = "⚠️ Vui lòng nhập câu trả lời!";
                lblAP_QuizFeedback.Foreground = DS.Brush(DS.ResultDanger);
                lblAP_QuizFeedback.Visibility = Visibility.Visible;
                return;
            }

            if (ParsingHelper.TryParseDouble(input, out double val))
            {
                if (System.Math.Abs(val - _apQuizCorrectAnswer) < 1e-3)
                {
                    _apStreak++;
                    lblAP_QuizFeedback.Text = $"🎉 Chính xác! Bạn đã làm rất tốt. (Chuỗi đúng liên tiếp: {_apStreak} 🔥)";
                    lblAP_QuizFeedback.Foreground = DS.Brush(DS.ResultSuccess);
                }
                else
                {
                    _apStreak = 0;
                    lblAP_QuizFeedback.Text = $"❌ Chưa chính xác. Đáp án đúng là: {UI.Fmt(_apQuizCorrectAnswer)}";
                    lblAP_QuizFeedback.Foreground = DS.Brush(DS.ResultDanger);
                }
            }
            else
            {
                lblAP_QuizFeedback.Text = "⚠️ Đáp án phải là một số hợp lệ!";
                lblAP_QuizFeedback.Foreground = DS.Brush(DS.ResultDanger);
            }
            lblAP_QuizFeedback.Visibility = Visibility.Visible;
        }

        private void APQuizNew_Click(object sender, RoutedEventArgs e)
        {
            GenerateAPQuiz();
        }

        private void GPQuizCheck_Click(object sender, RoutedEventArgs e)
        {
            if (lblGP_QuizFeedback == null) return;
            string input = txtGP_QuizAnswer.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                lblGP_QuizFeedback.Text = "⚠️ Vui lòng nhập câu trả lời!";
                lblGP_QuizFeedback.Foreground = DS.Brush(DS.ResultDanger);
                lblGP_QuizFeedback.Visibility = Visibility.Visible;
                return;
            }

            if (ParsingHelper.TryParseDouble(input, out double val))
            {
                if (System.Math.Abs(val - _gpQuizCorrectAnswer) < 1e-3)
                {
                    _gpStreak++;
                    lblGP_QuizFeedback.Text = $"🎉 Chính xác! Bạn đã làm rất tốt. (Chuỗi đúng liên tiếp: {_gpStreak} 🔥)";
                    lblGP_QuizFeedback.Foreground = DS.Brush(DS.ResultSuccess);
                }
                else
                {
                    _gpStreak = 0;
                    lblGP_QuizFeedback.Text = $"❌ Chưa chính xác. Đáp án đúng là: {UI.Fmt(_gpQuizCorrectAnswer)}";
                    lblGP_QuizFeedback.Foreground = DS.Brush(DS.ResultDanger);
                }
            }
            else
            {
                lblGP_QuizFeedback.Text = "⚠️ Đáp án phải là một số hợp lệ!";
                lblGP_QuizFeedback.Foreground = DS.Brush(DS.ResultDanger);
            }
            lblGP_QuizFeedback.Visibility = Visibility.Visible;
        }

        private void GPQuizNew_Click(object sender, RoutedEventArgs e)
        {
            GenerateGPQuiz();
        }

        private void APExport_Click(object sender, RoutedEventArgs e)
        {
            ExportToHtml(true);
        }

        private void GPExport_Click(object sender, RoutedEventArgs e)
        {
            ExportToHtml(false);
        }

        private void ExportToHtml(bool isAp)
        {
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "HTML Files (*.html)|*.html",
                    FileName = isAp ? "BaiGiai_CapSoCong.html" : "BaiGiai_CapSoNhan.html",
                    Title = "Lưu bài giải chi tiết"
                };

                if (sfd.ShowDialog() == true)
                {
                    string html = GenerateHtmlReport(isAp);
                    System.IO.File.WriteAllText(sfd.FileName, html, System.Text.Encoding.UTF8);
                    MessageBox.Show("Đã xuất bài giải chi tiết ra tệp HTML thành công!", "Xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất bài giải: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetTextFromResultElement(UIElement child)
        {
            if (child is TextBlock tb) return tb.Text;
            if (child is Border border)
            {
                if (border.Child is TextBlock btb) return btb.Text;
                if (border.Child is Grid grid)
                {
                    foreach (var gridChild in grid.Children)
                    {
                        if (gridChild is TextBlock gtb && Grid.GetColumn(gtb) == 0) return gtb.Text;
                        if (gridChild is WpfMath.Controls.FormulaControl formulaCtrl) return formulaCtrl.Formula;
                        if (gridChild is ScrollViewer innerSv && innerSv.Content is TextBlock innerSvTb) return innerSvTb.Text;
                        if (gridChild is System.Windows.Controls.Panel panel)
                        {
                            var sb = new System.Text.StringBuilder();
                            foreach (var pChild in panel.Children)
                            {
                                if (pChild is TextBlock ptb) sb.Append(ptb.Text);
                                if (pChild is WpfMath.Controls.FormulaControl fCtrl) sb.Append(fCtrl.Formula);
                            }
                            return sb.ToString();
                        }
                    }
                }
                if (border.Child is ScrollViewer sv && sv.Content is TextBlock svTb) return svTb.Text;
            }
            return "";
        }

        private string GenerateHtmlReport(bool isAp)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("<title>BÀI GIẢI CHI TIẾT — QA SMARTCLASS</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #F4F6F9; color: #333; margin: 0; padding: 40px; }");
            sb.AppendLine(".card { background: white; max-width: 800px; margin: 0 auto; padding: 30px; border-radius: 16px; box-shadow: 0 8px 24px rgba(0,0,0,0.08); border-top: 8px solid " + (isAp ? "#00695C" : "#BF360C") + "; }");
            sb.AppendLine("h1 { color: " + (isAp ? "#00695C" : "#BF360C") + "; margin-bottom: 5px; }");
            sb.AppendLine(".subtitle { font-size: 14px; color: #777; margin-bottom: 25px; }");
            sb.AppendLine(".section { margin-bottom: 20px; }");
            sb.AppendLine(".section-title { font-weight: bold; font-size: 16px; color: #555; border-bottom: 1px solid #EEE; padding-bottom: 5px; margin-bottom: 10px; }");
            sb.AppendLine(".item { font-size: 15px; line-height: 1.6; margin: 8px 0; }");
            sb.AppendLine(".formula { background-color: " + (isAp ? "#E0F2F1" : "#FBE9E7") + "; border-left: 4px solid " + (isAp ? "#00897B" : "#D84315") + "; padding: 12px; margin: 10px 0; font-family: 'Segoe UI', sans-serif; font-size: 16px; border-radius: 4px; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='card'>");
            
            if (isAp)
            {
                sb.AppendLine("<h1>📊 BÀI GIẢI CHI TIẾT: CẤP SỐ CỘNG</h1>");
                sb.AppendLine("<div class='subtitle'>Công cụ học tập thông minh — QA SmartClass</div>");

                sb.AppendLine("<div class='section'>");
                sb.AppendLine("<div class='section-title'>I. Giả thiết đầu vào</div>");
                sb.AppendLine($"<div class='item'>- Chế độ giải: <strong>{(((ComboBoxItem)cbAP_Mode.SelectedItem)?.Content?.ToString())}</strong></div>");
                sb.AppendLine($"<div class='item'>- Số hạng đầu U₁: <strong>{txtAP_U1.Text}</strong></div>");
                sb.AppendLine($"<div class='item'>- Công sai d: <strong>{txtAP_D.Text}</strong></div>");
                sb.AppendLine($"<div class='item'>- Số lượng số hạng n: <strong>{txtAP_N.Text}</strong></div>");
                sb.AppendLine("</div>");

                sb.AppendLine("<div class='section'>");
                sb.AppendLine("<div class='section-title'>II. Kết quả & Phương pháp giải</div>");
                foreach (UIElement child in apResultPanel.Children)
                {
                    string text = GetTextFromResultElement(child);
                    if (!string.IsNullOrEmpty(text))
                    {
                        if (text.StartsWith("🔑") || text.StartsWith("📋"))
                        {
                            sb.AppendLine($"<div class='formula'>{text}</div>");
                        }
                        else
                        {
                            sb.AppendLine($"<div class='item'>{text}</div>");
                        }
                    }
                }
                sb.AppendLine("</div>");
            }
            else
            {
                sb.AppendLine("<h1>📊 BÀI GIẢI CHI TIẾT: CẤP SỐ NHÂN</h1>");
                sb.AppendLine("<div class='subtitle'>Công cụ học tập thông minh — QA SmartClass</div>");

                sb.AppendLine("<div class='section'>");
                sb.AppendLine("<div class='section-title'>I. Giả thiết đầu vào</div>");
                sb.AppendLine($"<div class='item'>- Chế độ giải: <strong>{(((ComboBoxItem)cbGP_Mode.SelectedItem)?.Content?.ToString())}</strong></div>");
                sb.AppendLine($"<div class='item'>- Số hạng đầu U₁: <strong>{txtGP_U1.Text}</strong></div>");
                sb.AppendLine($"<div class='item'>- Công bội q: <strong>{txtGP_Q.Text}</strong></div>");
                sb.AppendLine($"<div class='item'>- Số lượng số hạng n: <strong>{txtGP_N.Text}</strong></div>");
                sb.AppendLine("</div>");

                sb.AppendLine("<div class='section'>");
                sb.AppendLine("<div class='section-title'>II. Kết quả & Phương pháp giải</div>");
                foreach (UIElement child in gpResultPanel.Children)
                {
                    string text = GetTextFromResultElement(child);
                    if (!string.IsNullOrEmpty(text))
                    {
                        if (text.StartsWith("🔑") || text.StartsWith("📋"))
                        {
                            sb.AppendLine($"<div class='formula'>{text}</div>");
                        }
                        else
                        {
                            sb.AppendLine($"<div class='item'>{text}</div>");
                        }
                    }
                }
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
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
                        Icon = "💵",
                        Title = isVN ? "Lợi nhuận lãi kép & Đầu tư" : "Compound Interest & Investments",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_sequence_1_{suffix}.png",
                        Description = isVN 
                            ? "Cấp số nhân được ứng dụng để tính tăng trưởng tiền gửi tiết kiệm hoặc đầu tư với lãi kép. Ví dụ: Nếu gửi 10 triệu đồng với lãi suất 6%/năm, số tiền thu được sau mỗi năm lập thành một cấp số nhân với số hạng đầu U1 = 10 và công bội q = 1.06. Công thức U_n = U1 * q^(n-1) giúp tính chính xác tài sản tích lũy." 
                            : "Geometric progressions model money growth under compound interest. For example, depositing $1,000 at a 6% annual interest rate creates a sequence where U1 = 1000 and q = 1.06. The formula U_n = U1 * q^(n-1) calculates the future value after n years."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💊",
                        Title = isVN ? "Sự đào thải thuốc trong cơ thể" : "Drug Elimination Half-Life",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_sequence_2_{suffix}.png",
                        Description = isVN 
                            ? "Khi uống thuốc, cơ thể sẽ đào thải một tỷ lệ cố định lượng thuốc sau mỗi khoảng thời gian (chu kỳ bán rã). Ví dụ: Một liều thuốc 100mg với tỷ lệ đào thải 50% sau mỗi 2 giờ. Lượng thuốc còn lại trong máu sau mỗi chu kỳ lập thành cấp số nhân giảm: 100mg, 50mg, 25mg, 12.5mg,... giúp bác sĩ kê đơn liều lượng duy trì hợp lý." 
                            : "Drug concentrations in the bloodstream decrease by a constant percentage over regular half-life intervals, forming a decreasing geometric progression. For a 100mg dosage with 50% elimination every 2 hours, the drug levels form the sequence: 100, 50, 25, 12.5mg, guiding proper dosage schedules."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💻",
                        Title = isVN ? "Độ phức tạp thuật toán chia để trị" : "Algorithm Time Complexity",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_sequence_3_{suffix}.png",
                        Description = isVN 
                            ? "Nhiều thuật toán đệ quy trong khoa học máy tính có độ phức tạp thời gian tăng theo cấp số nhân. Ví dụ: Trò chơi Tháp Hà Nội với n đĩa yêu cầu tối thiểu S_n = 2^n - 1 bước di chuyển. Số bước di chuyển cho mỗi đĩa thêm vào tăng gấp đôi, minh họa trực quan sự bùng nổ lũy thừa trong tính toán máy tính." 
                            : "Many recursive divide-and-conquer computer algorithms exhibit geometric complexity growth. E.g., solving the Tower of Hanoi puzzle with n disks requires a minimum of S_n = 2^n - 1 moves. Adding each additional disk doubles the step count, demonstrating exponential growth."
                    }
                };

                if (practicalAppViewer != null) practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for SequenceTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewAP == null || viewGP == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewAP.Visibility = Visibility.Collapsed;
            viewGP.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewAP.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewGP.Visibility = Visibility.Visible;
                    break;
                case 3:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}

