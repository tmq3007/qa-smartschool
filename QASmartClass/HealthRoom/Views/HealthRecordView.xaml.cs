using QASmartClass.Data;
using System.Globalization;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartClass.HealthRoom.Views
{
    public partial class HealthRecordView : Page
    {
        private readonly AppDbContext _db;

        public HealthRecordView()
        {
            InitializeComponent();
            _db = new AppDbContext();
            Unloaded += (s, e) => { _db?.Dispose(); };
            Loaded += (_, __) => LoadRecords();
        }

        private void LoadRecords()
        {
            try
            {
                var records = _db.HealthRecords.OrderByDescending(h => h.ExamDate).ToList();
                DgRecords.ItemsSource = records;
            }
            catch (Exception ex) { Log.Warning("[Health] Load error: {Err}", ex.Message); }
        }

        // WHO standard heights (cm) for Boys (ages 6 to 18)
        private static readonly double[] WhoP5_Boy  = { 110, 115, 120, 125, 130, 135, 140, 147, 155, 162, 166, 168, 169 };
        private static readonly double[] WhoP50_Boy = { 116, 122, 128, 134, 139, 145, 151, 158, 165, 170, 173, 175, 176 };
        private static readonly double[] WhoP95_Boy = { 122, 129, 136, 143, 149, 156, 163, 171, 178, 182, 185, 187, 188 };

        // WHO standard heights (cm) for Girls (ages 6 to 18)
        private static readonly double[] WhoP5_Girl  = { 109, 114, 119, 124, 130, 136, 142, 148, 152, 154, 155, 156, 156 };
        private static readonly double[] WhoP50_Girl = { 115, 121, 127, 133, 139, 145, 151, 157, 160, 162, 163, 163, 164 };
        private static readonly double[] WhoP95_Girl = { 121, 128, 135, 142, 149, 155, 161, 166, 169, 170, 171, 171, 172 };

        private void DgRecords_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgRecords.SelectedItem is HealthRecord r)
            {
                TxtDetailName.Text = r.StudentName;
                TxtDetailClass.Text = $"Lớp: {r.ClassName}";
                double vl = r.VisionLeft > 2.0 ? r.VisionLeft / 10.0 : r.VisionLeft;
                double vr = r.VisionRight > 2.0 ? r.VisionRight / 10.0 : r.VisionRight;
                TxtVision.Text = $"{vl:F1} / {vr:F1}";
                
                double bmi = r.Height > 0 ? r.Weight / Math.Pow(r.Height / 100.0, 2) : 0;
                string bmiCategory = "Bình thường";

                var bmiStandardSetting = _db.SystemSettings.FirstOrDefault(s => s.Id == "Medical_BmiCalculationStandard");
                string useChildStandard = bmiStandardSetting?.Value ?? "1"; // Default to 1 (WHO Percentiles)

                var student = _db.Students.FirstOrDefault(s => s.Id == r.StudentId);
                if (student != null)
                {
                    // Check and show allergy warning banner
                    var allergy = _db.FoodAllergies.FirstOrDefault(a => a.StudentCode == student.StudentCode);
                    if (allergy != null)
                    {
                        BorderAllergyAlert.Visibility = Visibility.Visible;
                        TxtAllergyAlert.Text = $"{allergy.Allergen} ({allergy.Severity}) - {allergy.ActionPlan}";
                    }
                    else
                    {
                        BorderAllergyAlert.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    BorderAllergyAlert.Visibility = Visibility.Collapsed;
                }

                if (useChildStandard == "1" && student != null)
                {
                    string gender = student.Gender ?? "Nam";
                    int grade = 10;
                    var match = System.Text.RegularExpressions.Regex.Match(student.ClassName ?? "", @"\d+");
                    if (match.Success)
                    {
                        int.TryParse(match.Value, out grade);
                    }
                    
                    // Use ExactAge if entered by nurse, otherwise default to calculated grade + 5
                    int age = r.ExactAge ?? Math.Max(6, Math.Min(18, grade + 5));

                    bool isBoy = string.Equals(gender, "Nam", StringComparison.OrdinalIgnoreCase);
                    double p5 = 13.0;
                    double p85 = 17.0;
                    double p95 = 18.5;

                    if (isBoy)
                    {
                        if (age <= 7)  { p5 = 13.0; p85 = 17.0; p95 = 18.5; }
                        else if (age <= 9)  { p5 = 13.5; p85 = 18.0; p95 = 20.0; }
                        else if (age <= 11) { p5 = 14.0; p85 = 19.5; p95 = 22.0; }
                        else if (age <= 13) { p5 = 15.0; p85 = 21.0; p95 = 24.0; }
                        else if (age <= 15) { p5 = 16.5; p85 = 23.0; p95 = 26.0; }
                        else { p5 = 17.5; p85 = 25.0; p95 = 28.0; }
                    }
                    else
                    {
                        if (age <= 7)  { p5 = 12.7; p85 = 16.8; p95 = 18.0; }
                        else if (age <= 9)  { p5 = 13.2; p85 = 17.8; p95 = 19.8; }
                        else if (age <= 11) { p5 = 13.8; p85 = 19.2; p95 = 21.8; }
                        else if (age <= 13) { p5 = 14.8; p85 = 20.8; p95 = 23.8; }
                        else if (age <= 15) { p5 = 16.0; p85 = 22.8; p95 = 25.8; }
                        else { p5 = 17.2; p85 = 24.8; p95 = 27.8; }
                    }

                    if (bmi < p5)
                        bmiCategory = "Gầy";
                    else if (bmi < p85)
                        bmiCategory = "Bình thường";
                    else if (bmi < p95)
                        bmiCategory = "Thừa cân";
                    else
                        bmiCategory = "Béo phì";
                }
                else
                {
                    bmiCategory = bmi < 18.5 ? "Gầy" : bmi > 25.0 ? (bmi > 30.0 ? "Béo phì" : "Thừa cân") : "Bình thường";
                }

                TxtBmi.Text = $"{bmi:F1} ({bmiCategory})";
                
                // Set BMI warning color dynamically
                var bmiColorBrush = bmiCategory switch
                {
                    "Bình thường" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                    "Gầy" or "Thừa cân" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                    "Béo phì" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"))
                };
                TxtBmi.Foreground = bmiColorBrush;

                TxtChronic.Text = string.IsNullOrEmpty(r.ChronicConditions) ? "Không có" : r.ChronicConditions;
                
                // Draw WHO standard-based line growth chart
                var history = _db.HealthRecords
                    .Where(h => h.StudentId == r.StudentId)
                    .OrderBy(h => h.ExamDate)
                    .ToList();
                DrawGrowthChart(student, history, r);

                PanelDetails.Visibility = Visibility.Visible;
                PanelPlaceholder.Visibility = Visibility.Collapsed;
            }
            else
            {
                PanelDetails.Visibility = Visibility.Collapsed;
                PanelPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void DrawGrowthChart(Student student, List<HealthRecord> history, HealthRecord currentRecord)
        {
            CvGrowthChart.Children.Clear();
            if (history == null || history.Count == 0) return;

            double w = CvGrowthChart.ActualWidth > 0 ? CvGrowthChart.ActualWidth : 260;
            double h = CvGrowthChart.ActualHeight > 0 ? CvGrowthChart.ActualHeight : 170;

            double leftMargin = 30;
            double rightMargin = 35;
            double topMargin = 15;
            double bottomMargin = 20;

            double drawW = w - leftMargin - rightMargin;
            double drawH = h - topMargin - bottomMargin;

            // Y scale maps height 50.0 -> 200.0 to drawH -> 0
            double ScaleY(double height) => topMargin + drawH - ((height - 50.0) / 150.0) * drawH;

            // X scale maps actual age linearly from age 6.0 to 18.0 to leftMargin -> leftMargin + drawW
            double ScaleX(double age)
            {
                double minAge = 6.0;
                double maxAge = 18.0;
                double val = Math.Max(minAge, Math.Min(maxAge, age));
                return leftMargin + ((val - minAge) / (maxAge - minAge)) * drawW;
            }

            // Draw horizontal grid lines and labels
            double[] gridHeights = { 50, 100, 150, 200 };
            foreach (var gh in gridHeights)
            {
                double y = ScaleY(gh);
                
                // Horizontal Line
                var gridLine = new Line
                {
                    X1 = leftMargin,
                    Y1 = y,
                    X2 = w - rightMargin,
                    Y2 = y,
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                    StrokeThickness = 1
                };
                CvGrowthChart.Children.Add(gridLine);

                // Y-axis label
                var label = new TextBlock
                {
                    Text = $"{gh}",
                    FontSize = 8,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"))
                };
                Canvas.SetLeft(label, 5);
                Canvas.SetTop(label, y - 6);
                CvGrowthChart.Children.Add(label);
            }

            string gender = student?.Gender ?? "Nam";
            bool isBoy = string.Equals(gender, "Nam", StringComparison.OrdinalIgnoreCase);

            double[] p5_arr = isBoy ? WhoP5_Boy : WhoP5_Girl;
            double[] p50_arr = isBoy ? WhoP50_Boy : WhoP50_Girl;
            double[] p95_arr = isBoy ? WhoP95_Boy : WhoP95_Girl;

            // Draw WHO Percentile Reference lines (P5, P50, P95) from age 6 to 18 (indices 0 to 12)
            var pointsP5 = new PointCollection();
            var pointsP50 = new PointCollection();
            var pointsP95 = new PointCollection();

            for (int i = 0; i < p5_arr.Length; i++)
            {
                double age = 6.0 + i;
                double x = ScaleX(age);
                double y_p5 = ScaleY(p5_arr[i]);
                double y_p50 = ScaleY(p50_arr[i]);
                double y_p95 = ScaleY(p95_arr[i]);

                pointsP5.Add(new Point(x, y_p5));
                pointsP50.Add(new Point(x, y_p50));
                pointsP95.Add(new Point(x, y_p95));
            }

            var polyP5 = new Polyline { Points = pointsP5, Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FECACA")), StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 3, 3 } };
            var polyP50 = new Polyline { Points = pointsP50, Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")), StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 3, 3 } };
            var polyP95 = new Polyline { Points = pointsP95, Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE4E6")), StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 3, 3 } };

            CvGrowthChart.Children.Add(polyP5);
            CvGrowthChart.Children.Add(polyP50);
            CvGrowthChart.Children.Add(polyP95);

            // Add standard line labels at the right end of the chart (age 18)
            double labelX = ScaleX(18.0);
            AddLineLabel("P5", labelX + 4, ScaleY(p5_arr.Last()) - 6, "#EF4444");
            AddLineLabel("P50", labelX + 4, ScaleY(p50_arr.Last()) - 6, "#64748B");
            AddLineLabel("P95", labelX + 4, ScaleY(p95_arr.Last()) - 6, "#F59E0B");

            // Draw student growth actual line
            var studentPoints = new PointCollection();
            for (int i = 0; i < history.Count; i++)
            {
                double age = GetAgeAtDate(student, history[i]);
                studentPoints.Add(new Point(ScaleX(age), ScaleY(history[i].Height)));
            }

            if (history.Count > 1)
            {
                var studentLine = new Polyline
                {
                    Points = studentPoints,
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                    StrokeThickness = 2.5
                };
                CvGrowthChart.Children.Add(studentLine);
            }

            // Draw dots & dates labels
            for (int i = 0; i < history.Count; i++)
            {
                var record = history[i];
                double age = GetAgeAtDate(student, record);
                double x = ScaleX(age);
                double y = ScaleY(record.Height);

                // Point dot
                var dot = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    ToolTip = $"{record.StudentName}\nChiều cao: {record.Height} cm\nNgày: {record.ExamDate:dd/MM/yyyy}\nTuổi: {age:F1} tuổi"
                };

                if (record.Id == currentRecord.Id)
                {
                    dot.Fill = Brushes.White;
                    dot.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
                    dot.StrokeThickness = 3;
                }
                else
                {
                    dot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
                }

                Canvas.SetLeft(dot, x - 4);
                Canvas.SetTop(dot, y - 4);
                CvGrowthChart.Children.Add(dot);

                // Label actual value
                var valText = new TextBlock
                {
                    Text = $"{record.Height:F0}",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"))
                };
                Canvas.SetLeft(valText, x - 8);
                Canvas.SetTop(valText, y - 16);
                CvGrowthChart.Children.Add(valText);

                // Label Date with age
                var dateText = new TextBlock
                {
                    Text = $"{record.ExamDate:dd/MM} ({age:F0}t)",
                    FontSize = 8,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"))
                };
                Canvas.SetLeft(dateText, x - 15);
                Canvas.SetTop(dateText, h - 15);
                CvGrowthChart.Children.Add(dateText);
            }
        }

        private void AddLineLabel(string text, double x, double y, string colorHex)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 8,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex))
            };
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, y);
            CvGrowthChart.Children.Add(tb);
        }

        private double GetAgeAtDate(Student student, HealthRecord r)
        {
            if (r.ExactAge.HasValue) return r.ExactAge.Value;
            
            // Fallback to estimated age
            int grade = 10;
            var match = System.Text.RegularExpressions.Regex.Match(student?.ClassName ?? "", @"\d+");
            if (match.Success)
            {
                int.TryParse(match.Value, out grade);
            }
            int baseAge = Math.Max(6, Math.Min(18, grade + 5));
            
            // Adjust based on the exam year relative to current year
            int diffYears = r.ExamDate.Year - DateTime.Today.Year;
            return Math.Max(6, Math.Min(18, baseAge + diffYears));
        }

        private double GetWhoHeight(double age, double[] arr, int lineType)
        {
            double idxDouble = age - 6.0;
            if (idxDouble <= 0) return arr[0];
            if (idxDouble >= arr.Length - 1) return arr[arr.Length - 1];

            int idxFloor = (int)Math.Floor(idxDouble);
            int idxCeil = (int)Math.Ceiling(idxDouble);
            if (idxFloor == idxCeil) return arr[idxFloor];

            double t = idxDouble - idxFloor;
            return arr[idxFloor] * (1.0 - t) + arr[idxCeil] * t;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new HealthRecordInputDialog(_db);
            if (dlg.ShowDialog() == true)
            {
                LoadRecords();
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (DgRecords.SelectedItem is HealthRecord r)
            {
                var student = _db.Students.FirstOrDefault(s => s.Id == r.StudentId);
                var history = _db.HealthRecords
                    .Where(h => h.StudentId == r.StudentId)
                    .OrderBy(h => h.ExamDate)
                    .ToList();

                var pdfService = new QASmartClass.Services.PdfExportService(_db);
                string path = pdfService.ExportHealthRecordReport(r, student, history);
                
                if (!string.IsNullOrEmpty(path))
                {
                    MessageBox.Show($"Đã xuất báo cáo PDF hồ sơ sức khỏe thành công!\nĐường dẫn: {path}", "Xuất báo cáo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Có lỗi xảy ra khi xuất báo cáo PDF.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một hồ sơ sức khỏe để xuất báo cáo.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CvGrowthChart_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DgRecords.SelectedItem is HealthRecord r)
            {
                var student = _db.Students.FirstOrDefault(s => s.Id == r.StudentId);
                var history = _db.HealthRecords
                    .Where(h => h.StudentId == r.StudentId)
                    .OrderBy(h => h.ExamDate)
                    .ToList();
                DrawGrowthChart(student, history, r);
            }
        }
    }
}

