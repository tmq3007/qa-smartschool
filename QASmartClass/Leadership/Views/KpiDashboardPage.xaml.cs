using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;
using System;
using System.Windows;
using System.Windows.Controls;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace QASmartClass.Leadership.Views
{
    public partial class KpiDashboardPage : Page
    {
        private readonly AppDbContext _db;
        private readonly KpiService _kpiService;

        public KpiDashboardPage(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            _kpiService = new KpiService(_db);

            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;
            LoadKpiData();
            RunComparison();
        }

        private void LoadKpiData()
        {
            try
            {
                var kpi = _kpiService.GetSchoolKpi();

                TxtTotalStudents.Text = kpi.TotalStudents.ToString();
                TxtAttendanceRate.Text = $"{kpi.AttendanceRate}%";
                TxtAvgScore.Text = kpi.AvgScore.ToString();
                TxtTaskRate.Text = $"{kpi.TaskCompletionRate}%";

                TxtEmulationLeader.Text = string.IsNullOrEmpty(kpi.EmulationLeader) ? "Chưa có dữ liệu" : kpi.EmulationLeader;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi tải dữ liệu KpiDashboardPage.");
                MessageBox.Show("Có lỗi xảy ra khi tải bảng điều khiển KPI.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CompareSemesters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                RunComparison();
            }
        }

        private void BtnCompare_Click(object sender, RoutedEventArgs e)
        {
            RunComparison();
        }

        private void RunComparison()
        {
            try
            {
                string schoolYear = (CboSchoolYear.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "2025-2026";
                var analyticsService = new ComparativeAnalyticsService(_db);

                // So sánh k?t qu? h?c t?p
                var acadComp = analyticsService.CompareAcademicResults("HK1", "HK2", schoolYear);
                TxtScore1.Text = acadComp.AverageScore1.ToString("F2");
                TxtScore2.Text = acadComp.AverageScore2.ToString("F2");
                TxtScoreDelta.Text = (acadComp.ScoreDelta >= 0 ? "+" : "") + acadComp.ScoreDelta.ToString("F2");
                TxtScoreDelta.Foreground = acadComp.ScoreDelta >= 0 
                    ? System.Windows.Media.Brushes.Green 
                    : System.Windows.Media.Brushes.Red;
                
                TxtCompareScore.Text = $"{acadComp.AverageScore1:F1} / {acadComp.AverageScore2:F1}";
                TxtTrendScore.Text = acadComp.ScoreTrend switch { "Up" => "▲", "Down" => "▼", _ => "―" };
                TxtTrendScore.Foreground = acadComp.ScoreTrend switch { "Up" => System.Windows.Media.Brushes.Green, "Down" => System.Windows.Media.Brushes.Red, _ => System.Windows.Media.Brushes.Gray };

                TxtPassRate1.Text = $"{acadComp.PassRate1}%";
                TxtPassRate2.Text = $"{acadComp.PassRate2}%";
                TxtPassRateDelta.Text = (acadComp.PassRateDelta >= 0 ? "+" : "") + acadComp.PassRateDelta.ToString("F2") + "%";
                TxtPassRateDelta.Foreground = acadComp.PassRateDelta >= 0 
                    ? System.Windows.Media.Brushes.Green 
                    : System.Windows.Media.Brushes.Red;

                // So sánh chuyên c?n
                var attComp = analyticsService.CompareAttendance("HK1", "HK2", schoolYear);
                TxtAtt1.Text = $"{attComp.AttendanceRate1}%";
                TxtAtt2.Text = $"{attComp.AttendanceRate2}%";
                TxtAttDelta.Text = (attComp.RateDelta >= 0 ? "+" : "") + attComp.RateDelta.ToString("F2") + "%";
                TxtAttDelta.Foreground = attComp.RateDelta >= 0 
                    ? System.Windows.Media.Brushes.Green 
                    : System.Windows.Media.Brushes.Red;

                TxtCompareAttendance.Text = $"{attComp.AttendanceRate1:F1}% / {attComp.AttendanceRate2:F1}%";
                TxtTrendAttendance.Text = attComp.RateTrend switch { "Up" => "▲", "Down" => "▼", _ => "―" };
                TxtTrendAttendance.Foreground = attComp.RateTrend switch { "Up" => System.Windows.Media.Brushes.Green, "Down" => System.Windows.Media.Brushes.Red, _ => System.Windows.Media.Brushes.Gray };

                // So sánh thi dua/k? lu?t
                var emComp = analyticsService.CompareEmulation("HK1", "HK2", schoolYear);
                TxtCommendation1.Text = emComp.CommendationCount1.ToString();
                TxtCommendation2.Text = emComp.CommendationCount2.ToString();
                int commDelta = emComp.CommendationCount2 - emComp.CommendationCount1;
                TxtCommendationDelta.Text = (commDelta >= 0 ? "+" : "") + commDelta;
                TxtCommendationDelta.Foreground = commDelta >= 0 
                    ? System.Windows.Media.Brushes.Green 
                    : System.Windows.Media.Brushes.Red;

                TxtDiscipline1.Text = emComp.DisciplineCount1.ToString();
                TxtDiscipline2.Text = emComp.DisciplineCount2.ToString();
                int discDelta = emComp.DisciplineCount2 - emComp.DisciplineCount1;
                TxtDisciplineDelta.Text = (discDelta >= 0 ? "+" : "") + discDelta;
                TxtDisciplineDelta.Foreground = discDelta <= 0 
                    ? System.Windows.Media.Brushes.Green 
                    : System.Windows.Media.Brushes.Red;

                TxtCompareConduct.Text = $"{emComp.AverageConductDelta1:F1} / {emComp.AverageConductDelta2:F1}";
                TxtTrendConduct.Text = emComp.ConductTrend switch { "Up" => "▲", "Down" => "▼", _ => "―" };
                TxtTrendConduct.Foreground = emComp.ConductTrend switch { "Up" => System.Windows.Media.Brushes.Green, "Down" => System.Windows.Media.Brushes.Red, _ => System.Windows.Media.Brushes.Gray };

                // --- OxyPlot: ChartAcademic ---
                var acadPlot = new PlotModel
                {
                    Title = "Phân Phối Xếp Loại Học Lực",
                    TitleFontSize = 12,
                    TitleFontWeight = OxyPlot.FontWeights.Bold,
                    Background = OxyColors.White
                };
                var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Key = "CategoryAxis" };
                categoryAxis.Labels.Add("Giỏi");
                categoryAxis.Labels.Add("Khá");
                categoryAxis.Labels.Add("Trung bình");
                categoryAxis.Labels.Add("Yếu");
                categoryAxis.Labels.Add("Kém");
                acadPlot.Axes.Add(categoryAxis);

                var valueAxis = new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 100, Title = "Tỷ lệ (%)", Key = "ValueAxis" };
                acadPlot.Axes.Add(valueAxis);

                var seriesHk1 = new BarSeries { Title = "Học kỳ 1", FillColor = OxyColor.FromRgb(59, 130, 246), XAxisKey = "ValueAxis", YAxisKey = "CategoryAxis" };
                var seriesHk2 = new BarSeries { Title = "Học kỳ 2", FillColor = OxyColor.FromRgb(16, 185, 129), XAxisKey = "ValueAxis", YAxisKey = "CategoryAxis" };

                var classifications = new[] { "Giỏi", "Khá", "Trung bình", "Yếu", "Kém" };
                foreach (var c in classifications)
                {
                    double val1 = acadComp.ClassificationPercentage1 != null && acadComp.ClassificationPercentage1.TryGetValue(c, out double v1) ? v1 : 0.0;
                    double val2 = acadComp.ClassificationPercentage2 != null && acadComp.ClassificationPercentage2.TryGetValue(c, out double v2) ? v2 : 0.0;
                    seriesHk1.Items.Add(new BarItem(val1));
                    seriesHk2.Items.Add(new BarItem(val2));
                }
                acadPlot.Series.Add(seriesHk1);
                acadPlot.Series.Add(seriesHk2);
                ChartAcademic.Model = acadPlot;

                // --- OxyPlot: ChartKpi ---
                var kpiPlot = new PlotModel
                {
                    Title = "Tỷ Lệ Chuyên Cần & Tỷ Lệ Đạt",
                    TitleFontSize = 12,
                    TitleFontWeight = OxyPlot.FontWeights.Bold,
                    Background = OxyColors.White
                };
                var kpiCategoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Key = "KpiCategoryAxis" };
                kpiCategoryAxis.Labels.Add("Chuyên cần");
                kpiCategoryAxis.Labels.Add("Tỷ lệ đạt (>=5.0)");
                kpiPlot.Axes.Add(kpiCategoryAxis);

                var kpiValueAxis = new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 100, Title = "Tỷ lệ (%)", Key = "KpiValueAxis" };
                kpiPlot.Axes.Add(kpiValueAxis);

                var kpiSeriesHk1 = new BarSeries { Title = "Học kỳ 1", FillColor = OxyColor.FromRgb(99, 102, 241), XAxisKey = "KpiValueAxis", YAxisKey = "KpiCategoryAxis" };
                var kpiSeriesHk2 = new BarSeries { Title = "Học kỳ 2", FillColor = OxyColor.FromRgb(245, 158, 11), XAxisKey = "KpiValueAxis", YAxisKey = "KpiCategoryAxis" };

                kpiSeriesHk1.Items.Add(new BarItem(attComp.AttendanceRate1));
                kpiSeriesHk1.Items.Add(new BarItem(acadComp.PassRate1));

                kpiSeriesHk2.Items.Add(new BarItem(attComp.AttendanceRate2));
                kpiSeriesHk2.Items.Add(new BarItem(acadComp.PassRate2));

                kpiPlot.Series.Add(kpiSeriesHk1);
                kpiPlot.Series.Add(kpiSeriesHk2);
                ChartKpi.Model = kpiPlot;

                ChartAcademic.InvalidatePlot(true);
                ChartKpi.InvalidatePlot(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi chạy so sánh học kỳ");
            }
        }
    }
}

