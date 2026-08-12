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
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class StatisticsTool : BaseToolControl
    {
        private double[] _data = Array.Empty<double>();

        public StatisticsTool()
        {
            InitializeComponent();
            Loaded += (_, _) => { 
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Tính toán & Lý thuyết" : "Calculator & Theory";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";

                if (sideMenu != null)
                {
                    sideMenu.SelectionChanged += SideMenu_SelectionChanged;
                    sideMenu.SelectedIndex = 0;
                }

                BuildPresets(); 
                Calc(); 
                TouchNumPad.Attach(txtData, step: 1, allowDecimal: true, allowNegative: true);
                LoadPracticalApps();
            };
        }

        private void Data_Changed(object sender, EventArgs e) { if (IsLoaded) Calc(); }

        private void Calc()
        {
            resultPanel.Children.Clear();
            _data = ParseData(txtData?.Text);
            if (_data.Length == 0)
            {
                txtCount.Text = "";
                UI.ResultRow("⚠️ Vui lòng nhập dãy số hợp lệ (ví dụ: 4, 8, 6, 5)", "#C62828", resultPanel);
                return;
            }

            txtCount.Text = $"📋 {_data.Length} giá trị | Min={_data.Min():G6} | Max={_data.Max():G6}";

            double mean = _data.Average();
            double median = GetMedian(_data);
            var mode = GetMode(_data);
            double variance = _data.Sum(x => (x - mean) * (x - mean)) / _data.Length;
            double stdDev = System.Math.Sqrt(variance);
            double range = _data.Max() - _data.Min();
            double sum = _data.Sum();

            UI.ResultRow($"📊 Cỡ mẫu n = {_data.Length}", "#0D47A1", resultPanel);
            UI.ResultRow($"➕ Tổng Σx = {UI.Fmt(sum)}", "#1565C0", resultPanel);
            UI.ResultRow($"📏 Số trung bình x̄ = Σx/n = {UI.Fmt(sum)}/{_data.Length} = {UI.Fmt(mean)}", "#1565C0", resultPanel);
            UI.ResultRow($"📐 Trung vị Me = {UI.Fmt(median)}", "#2E7D32", resultPanel);
            UI.ResultRow($"🎯 Mốt Mo = {(mode.Count > 0 ? string.Join(", ", mode.Select(UI.Fmt)) : "Không có")}", "#E65100", resultPanel);
            UI.ResultRow($"📊 Phương sai s² = Σ(xᵢ−x̄)²/n = {UI.Fmt(variance)}", "#7B1FA2", resultPanel);
            UI.ResultRow($"📊 Độ lệch chuẩn s = √s² = {UI.Fmt(stdDev)}", "#C62828", resultPanel);
            UI.ResultRow($"📏 Khoảng biến thiên R = Max − Min = {UI.Fmt(_data.Max())} − {UI.Fmt(_data.Min())} = {UI.Fmt(range)}", "#004D40", resultPanel);

            // Quartiles
            if (_data.Length >= 4)
            {
                var (q1, q3) = GetQuartilesSGK(_data);
                double iqr = q3 - q1;
                UI.ResultRow($"📐 Tứ phân vị: Q₁ = {UI.Fmt(q1)} | Q₂ (Trung vị) = {UI.Fmt(median)} | Q₃ = {UI.Fmt(q3)}", "#283593", resultPanel);
                UI.ResultRow($"📐 Khoảng tứ phân vị Δ_Q = Q₃ − Q₁ = {UI.Fmt(q3)} − {UI.Fmt(q1)} = {UI.Fmt(iqr)}", "#283593", resultPanel);
            }

            // Sorted data
            var sorted = string.Join(", ", _data.OrderBy(x => x).Select(UI.Fmt));
            if (sorted.Length > 120) sorted = sorted.Substring(0, 117) + "...";
            UI.ResultRow($"📋 Sắp xếp: {sorted}", "#757575", resultPanel);
        }

        // ═══ Graph ═══
        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_data.Length == 0) return;
                var ci = CultureInfo.InvariantCulture;
                double mean = _data.Average();

                // Plot points as 1D distribution list L on the x-axis
                var yList = string.Join(",", _data.Select(d => d.ToString(ci)));

                string expJs = $@"
        calc.setExpression({{id:'list', latex:'L=[{yList}]'}});
        calc.setExpression({{id:'dots', latex:'dotplot(L)', color:'#1565C0', pointSize:10}});
        calc.setExpression({{id:'box', latex:'boxplot(L, 2.5, 1)', color:'#2E7D32', lineWidth:2}});
        calc.setExpression({{id:'mean', latex:'x={mean.ToString(ci)}', color:'#C62828', lineWidth:2, lineStyle:'DASHED', label:'x̄={mean.ToString("G4", ci)}', showLabel:true}});
        calc.setMathBounds({{ left: {(_data.Min() - 2).ToString(ci)}, right: {(_data.Max() + 2).ToString(ci)}, bottom: -1, top: 5 }});";

                var win = new GraphWindow(expJs, $"📊 Thống kê phân bố n={_data.Length}");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        // ═══ PRESETS ═══
        private void BuildPresets()
        {
            if (presetPanel == null) return;
            var presets = new (string L, string D)[]
            {
                ("Điểm thi", "4, 5, 6, 7, 8, 8, 9, 7, 6, 5, 8, 7"),
                ("Nhiệt độ", "22, 24, 25, 23, 26, 28, 27, 25, 24, 23"),
                ("Chiều cao (cm)", "155, 160, 162, 158, 170, 165, 168, 157, 163, 161"),
                ("Cân nặng (kg)", "45, 50, 55, 48, 62, 58, 52, 47, 53, 56"),
            };
            var presetColor = (Color)ColorConverter.ConvertFromString("#0D47A1");
            foreach (var (l, d) in presets)
            {
                string cd = d;
                UI.PresetButton(l, presetColor, () => { txtData.Text = cd; }, presetPanel);
            }
        }

        // ═══ HELPERS ═══
        private static double[] ParseData(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return Array.Empty<double>();

            var list = new List<double>();

            // Case 1: Has semicolon
            if (input.Contains(';'))
            {
                var parts = input.Split(new[] { ';', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    string clean = part.Trim();
                    if (string.IsNullOrEmpty(clean)) continue;
                    string normalized = clean.Replace(',', '.');
                    if (ParsingHelper.TryParseDouble(normalized, out double val))
                    {
                        list.Add(val);
                    }
                }
                if (list.Count > 0) return list.ToArray();
            }

            // Case 2: Split by whitespace
            var wsParts = input.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (wsParts.Length > 1)
            {
                foreach (var part in wsParts)
                {
                    string clean = part.Trim(',', ' ', '\t');
                    if (string.IsNullOrEmpty(clean)) continue;
                    
                    if (ParsingHelper.TryParseDouble(clean, out double val))
                    {
                        list.Add(val);
                    }
                    else
                    {
                        string normalized = clean.Replace(',', '.');
                        if (ParsingHelper.TryParseDouble(normalized, out double val2))
                        {
                            list.Add(val2);
                        }
                    }
                }
                if (list.Count > 0) return list.ToArray();
            }

            // Case 3: Split by comma
            var commaParts = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in commaParts)
            {
                string clean = part.Trim();
                if (string.IsNullOrEmpty(clean)) continue;
                if (ParsingHelper.TryParseDouble(clean, out double val))
                {
                    list.Add(val);
                }
            }

            return list.ToArray();
        }

        private static double GetMedian(double[] data)
        {
            if (data == null || data.Length == 0) return 0;
            var sorted = data.OrderBy(x => x).ToArray();
            int n = sorted.Length;
            return n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
        }

        private static List<double> GetMode(double[] data)
        {
            if (data == null || data.Length == 0) return new List<double>();
            var freq = data.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
            int maxFreq = freq.Values.Max();
            if (maxFreq == 1) return new List<double>();
            return freq.Where(kv => kv.Value == maxFreq).Select(kv => kv.Key).OrderBy(x => x).ToList();
        }

        private static (double q1, double q3) GetQuartilesSGK(double[] data)
        {
            if (data == null || data.Length < 2) return (0, 0);
            var sorted = data.OrderBy(x => x).ToArray();
            int n = sorted.Length;
            
            double[] lowerHalf;
            double[] upperHalf;
            
            if (n % 2 == 0)
            {
                int halfSize = n / 2;
                lowerHalf = new double[halfSize];
                upperHalf = new double[halfSize];
                Array.Copy(sorted, 0, lowerHalf, 0, halfSize);
                Array.Copy(sorted, halfSize, upperHalf, 0, halfSize);
            }
            else
            {
                int halfSize = (n - 1) / 2;
                lowerHalf = new double[halfSize];
                upperHalf = new double[halfSize];
                Array.Copy(sorted, 0, lowerHalf, 0, halfSize);
                Array.Copy(sorted, halfSize + 1, upperHalf, 0, halfSize);
            }
            
            double q1 = GetMedian(lowerHalf);
            double q3 = GetMedian(upperHalf);
            return (q1, q3);
        }

        // AddR(), Fmt() đã được thay thế bằng UI.ResultRow(), UI.Fmt()

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
                        Icon = "📈",
                        Title = isVN ? "Phân tích điểm số học sinh" : "Student Grade Analysis",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_statistics_1_{suffix}.png",
                        Description = isVN 
                            ? "Thống kê mô tả (Mean, Median, Mode, StdDev) được giáo viên sử dụng để đánh giá chất lượng học tập của lớp. Điểm trung bình (Mean) thể hiện mức học chung, Trung vị (Median) giúp loại bỏ ảnh hưởng của điểm số quá cao hoặc quá thấp, và Độ lệch chuẩn (StdDev) đo mức độ đồng đều của học sinh trong lớp." 
                            : "Descriptive statistics (Mean, Median, Mode, StdDev) are used by teachers to evaluate class performance. Mean represents the average level, Median filters out outlier grades, and Standard Deviation (StdDev) measures the uniformity of students' academic performance."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📏",
                        Title = isVN ? "Khảo sát thể trạng dân số" : "Population Height Survey",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_statistics_2_{suffix}.png",
                        Description = isVN 
                            ? "Dữ liệu chiều cao của một mẫu dân cư được phân tích thống kê để vẽ biểu đồ phân phối chuẩn. Khoảng tứ phân vị (Q1, Q3) giúp phân loại chiều cao ở các mức thấp, trung bình và cao, hỗ trợ bộ Y tế đưa ra các chương trình dinh dưỡng học đường và phát triển thể chất quốc gia." 
                            : "Height data from a population sample is statistically analyzed to model normal distribution. Quartiles (Q1, Q3) help classify height tiers (low, average, high), aiding public health organizations in designing physical development and nutrition programs."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⚙",
                        Title = isVN ? "Kiểm tra sai số kích thước chi tiết" : "Dimensional Tolerance Control",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_statistics_3_{suffix}.png",
                        Description = isVN 
                            ? "Trong gia công cơ khí, đường kính của các chi tiết (ví dụ: bu-lông) được đo đạc liên tục. Độ lệch chuẩn (StdDev) thể hiện sai số chế tạo của máy móc. Độ lệch chuẩn càng nhỏ chứng tỏ dây chuyền hoạt động càng ổn định và chính xác, giảm thiểu phế phẩm lỗi kích thước." 
                            : "In precision machining, workpiece dimensions (e.g. bolts) are constantly measured. The standard deviation (StdDev) represents manufacturing error variance. A smaller StdDev indicates a more stable and precise assembly line, minimizing out-of-tolerance waste."
                    }
                };

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for StatisticsTool: {Err}", ex.Message);
            }
        }
    }
}