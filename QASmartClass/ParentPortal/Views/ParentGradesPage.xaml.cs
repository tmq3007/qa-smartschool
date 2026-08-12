using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.ParentPortal.Views
{
    public partial class ParentGradesPage : Page
    {
        private readonly AppDbContext _db;
        private readonly Student _student;

        public ParentGradesPage(AppDbContext db, Student student)
        {
            InitializeComponent();
            _db = db;
            _student = student;
            txtStudentInfo.Text = $"{student.FullName} \u2022 {student.ClassName} \u2022 {student.StudentCode}";

            LoadGradesTable();
            LoadProgressChart();
        }

        // ─── Bảng điểm (giữ nguyên logic cũ) ───────────
        private void LoadGradesTable()
        {
            var grades = _db.StudentGrades
                .Where(g => g.StudentId == _student.Id)
                .OrderByDescending(g => g.UpdatedAt)
                .ToList();

            if (grades.Any())
            {
                var gradeTypes = _db.GradeTypeMasters.ToList();
                var rosters = _db.ClassRosters.ToList();

                listGrades.ItemsSource = grades.Select(g =>
                {
                    var roster = rosters.FirstOrDefault(r => r.Id == g.RosterId);
                    return new
                    {
                        Subject = roster?.Subject ?? "\u2014",
                        GradeType = gradeTypes.FirstOrDefault(t => t.Id == g.GradeTypeId)?.DisplayName ?? "Khác",
                        ScoreText = g.Score.ToString("F1"),
                        ScoreColor = g.Score >= 8 ? new SolidColorBrush(Color.FromRgb(16, 185, 129))
                                   : g.Score >= 5 ? new SolidColorBrush(Color.FromRgb(59, 130, 246))
                                   : new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                        DateText = g.UpdatedAt.ToString("dd/MM/yyyy")
                    };
                }).ToList();
            }
            else
            {
                txtNoGrades.Visibility = Visibility.Visible;
            }
        }

        // ─── P1-05: Biểu đồ tiến bộ theo tuần ──────────
        private void LoadProgressChart()
        {
            try
            {
                var myGrades = _db.StudentGrades
                    .Where(g => g.StudentId == _student.Id)
                    .ToList();

                if (myGrades.Count < 2)
                {
                    TxtNoChart.Visibility = Visibility.Visible;
                    return;
                }

                // Lấy niên khóa và học kỳ hoạt động hiện tại của học sinh
                var activeRoster = (from crs in _db.ClassRosterStudents
                                    join r in _db.ClassRosters on crs.RosterId equals r.Id
                                    where crs.StudentId == _student.Id && r.IsActive
                                    select new { r.SchoolYear, r.Semester }).FirstOrDefault();

                string currentYear = activeRoster?.SchoolYear ?? "2023-2024";
                string currentSemester = activeRoster?.Semester ?? "HK1";

                // Tính điểm TB lớp (tất cả HS cùng lớp ở niên khóa và học kỳ đó)
                var classRosterIds = _db.ClassRosters
                    .Where(r => r.ClassName == _student.ClassName && r.SchoolYear == currentYear && r.Semester == currentSemester && r.IsActive)
                    .Select(r => r.Id).ToList();
                var allClassGrades = _db.StudentGrades
                    .Where(g => classRosterIds.Contains(g.RosterId))
                    .ToList();
                double classAvg = allClassGrades.Any()
                    ? Math.Round(allClassGrades.Average(g => g.Score), 1) : 0;
                TxtClassAvg.Text = $"TB lớp: {classAvg:F1}";

                // Nhóm điểm theo tuần (ISO week)
                var weeklyData = myGrades
                    .GroupBy(g => GetWeekKey(g.UpdatedAt))
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        WeekKey = g.Key,
                        Avg = Math.Round(g.Average(x => x.Score), 1),
                        WeekStart = g.Min(x => x.UpdatedAt)
                    })
                    .TakeLast(8) // Max 8 tuần gần nhất
                    .ToList();

                if (weeklyData.Count < 2)
                {
                    TxtNoChart.Visibility = Visibility.Visible;
                    return;
                }

                // Scale: 10 điểm = 300px
                double scale = 30.0;
                var brushGreen = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                var brushRed   = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                var brushGray  = new SolidColorBrush(Color.FromRgb(148, 163, 184));

                var chartItems = new List<object>();
                for (int i = 0; i < weeklyData.Count; i++)
                {
                    var w = weeklyData[i];
                    double prevAvg = i > 0 ? weeklyData[i - 1].Avg : w.Avg;
                    bool isUp = w.Avg >= prevAvg;

                    chartItems.Add(new
                    {
                        WeekLabel  = $"T{w.WeekStart:dd/MM}",
                        BarWidth   = w.Avg * scale,
                        BarBrush   = i == 0 ? brushGray : (isUp ? brushGreen : brushRed),
                        ScoreDisplay = w.Avg.ToString("F1"),
                        Arrow      = i == 0 ? "" : (isUp ? "\u2191" : "\u2193"),
                        ClassAvgMargin = new Thickness(classAvg * scale, 0, 0, 0)
                    });
                }

                ChartBars.ItemsSource = chartItems;

                // Xu hướng tổng thể
                double first = weeklyData.First().Avg;
                double last  = weeklyData.Last().Avg;
                if (last > first)
                    TxtTrend.Text = $"\u2191 +{last - first:F1}";
                else if (last < first)
                    TxtTrend.Text = $"\u2193 {last - first:F1}";
                else
                    TxtTrend.Text = "\u2194 0";

                Log.Information("[ParentProgress] Chart loaded: {Weeks} weeks, ClassAvg={Avg}",
                    weeklyData.Count, classAvg);
            }
            catch (Exception ex)
            {
                Log.Warning("[ParentProgress] Chart error: {Err}", ex.Message);
                TxtNoChart.Visibility = Visibility.Visible;
            }
        }

        private static string GetWeekKey(DateTime date)
        {
            var cal = CultureInfo.CurrentCulture.Calendar;
            int week = cal.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            return $"{date.Year}-W{week:D2}";
        }
    }
}

