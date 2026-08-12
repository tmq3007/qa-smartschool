using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class ChartPage : Page
    {
        public ChartPage()
        {
            InitializeComponent();
            Loaded += (_, _) => LoadChart(0);
        }

        private void LoadChart(int type)
        {
            if (barChart == null) return; // guard — XAML not ready yet
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var db  = app.Database;

                List<BarItem> bars;
                double max = 10, min = 0, avg = 0;
                string passText = "N/A";

                if (type == 0) // Điểm TB theo HS
                {
                    var rawResults = db.QuizResults
                        .Select(r => new { r.StudentId, r.Score, r.TotalPoints })
                        .ToList();

                    var results = rawResults
                        .GroupBy(r => r.StudentId)
                        .Select(g => new { Id = g.Key, Avg = g.Average(r => r.TotalPoints > 0 ? ((double)r.Score / r.TotalPoints) * 10.0 : 0.0) })
                        .OrderByDescending(x => x.Avg)
                        .Take(12)
                        .ToList();

                    var students = db.Students.ToDictionary(s => s.Id, s => s.FullName.Split(' ').Last());

                    bars = results.Select(r => new BarItem
                    {
                        Label     = students.TryGetValue(r.Id, out var n) ? n : $"HS{r.Id}",
                        Value     = $"{r.Avg:F1}",
                        BarHeight = Math.Max(8, r.Avg / 10.0 * 180), // scale 0-10 to 0-180px
                        BarColor  = r.Avg >= 8.5 ? Color.FromRgb(76,175,80)
                                  : r.Avg >= 7.0 ? Color.FromRgb(33,150,243)
                                  : r.Avg >= 5.0 ? Color.FromRgb(255,152,0)
                                  : Color.FromRgb(244,67,54)
                    }).ToList();

                    if (bars.Any())
                    {
                        var vals = results.Select(r => r.Avg).ToList();
                        max = vals.Max(); min = vals.Min(); avg = vals.Average();
                        int pass = results.Count(r => r.Avg >= 5.0);
                        passText = $"{pass * 100 / results.Count}%";
                    }
                }
                else if (type == 1) // Phân loại học lực
                {
                    var demo = new[] { ("Giỏi", 8, 8.5), ("Khá", 7, 7.0), ("Trung bình", 4, 5.0), ("Yếu", 1, 0.0) };
                    bars = demo.Select(d => new BarItem
                    {
                        Label = d.Item1, Value = d.Item2.ToString(),
                        BarHeight = d.Item2 * 18.0,
                        BarColor = d.Item3 >= 8.5 ? Color.FromRgb(76,175,80)
                                 : d.Item3 >= 7.0 ? Color.FromRgb(33,150,243)
                                 : d.Item3 >= 5.0 ? Color.FromRgb(255,152,0)
                                 : Color.FromRgb(244,67,54)
                    }).ToList();
                    max = 10; min = 0; avg = 7.8; passText = "91%";
                }
                else // Tiến độ
                {
                    var months = new[] { "T1","T2","T3","T4","T5","T6","T7","T8","T9","T10","T11","T12" };
                    var rand = new Random(42);
                    bars = months.Select(m => new BarItem
                    {
                        Label = m, Value = $"{rand.Next(60, 98)}%",
                        BarHeight = rand.Next(80, 180),
                        BarColor = Color.FromRgb(25, 118, 210)
                    }).ToList();
                    max = 100; min = 60; avg = 82; passText = "82%";
                }

                // Fallback demo nếu DB trống
                if (!bars.Any())
                {
                    var names = new[] { "An","Bình","Cường","Dung","Em","Phương","Hải","Hoa","Khang","Lan" };
                    var scores = new[] { 9.8, 9.3, 8.5, 7.2, 7.8, 6.5, 5.0, 8.0, 9.0, 7.5 };
                    bars = names.Zip(scores, (n, s) => new BarItem
                    {
                        Label = n, Value = s.ToString("F1"),
                        BarHeight = s / 10.0 * 180,
                        BarColor = s >= 8.5 ? Color.FromRgb(76,175,80)
                                 : s >= 7.0 ? Color.FromRgb(33,150,243)
                                 : s >= 5.0 ? Color.FromRgb(255,152,0)
                                 : Color.FromRgb(244,67,54)
                    }).ToList();
                    max=9.8; min=5.0; avg=7.86; passText="90%";
                }

                barChart.ItemsSource = bars;
                txtMax.Text  = max.ToString("F1");
                txtMin.Text  = min.ToString("F1");
                txtAvg.Text  = avg.ToString("F1");
                txtPass.Text = passText;
                Log.Information("ChartPage: {Type} chart loaded with {Count} bars", type, bars.Count);
            }
            catch (Exception ex) { Log.Warning("ChartPage error: {Err}", ex.Message); }
        }

        private void ChartType_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (barChart != null && cmbChartType != null)
                LoadChart(cmbChartType.SelectedIndex);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) =>
            LoadChart(cmbChartType.SelectedIndex);
    }

    public class BarItem
    {
        public string Label     { get; set; } = string.Empty;
        public string Value     { get; set; } = string.Empty;
        public double BarHeight { get; set; }
        public Color  BarColor  { get; set; }
    }
}
