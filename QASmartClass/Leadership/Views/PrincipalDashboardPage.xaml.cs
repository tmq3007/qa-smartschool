using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.Leadership.Views
{
    public partial class PrincipalDashboardPage : Page
    {
        private readonly AppDbContext _db;

        public PrincipalDashboardPage(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadKPIs();
            LoadEmulation();
            LoadBulletins();
            LoadAnalytics();   // P1-08
        }

        private void LoadKPIs()
        {
            try
            {
                TxtTotalStudents.Text = _db.Students.Count().ToString();
                TxtTotalTeachers.Text = _db.TeacherProfiles.Count().ToString();

                // Attendance today
                var today = DateTime.Now.Date;
                var totalToday = _db.AttendanceRecords.Count(a => a.Date == today);
                var presentToday = _db.AttendanceRecords.Count(a => a.Date == today && (a.Status == "Present" || a.Status == "Có mặt"));
                TxtAttendanceRate.Text = totalToday > 0
                    ? $"{Math.Round((double)presentToday / totalToday * 100, 1)}%"
                    : "Chưa có dữ liệu";

                // Lessons this week
                var weekStart = today.AddDays(-(int)today.DayOfWeek + 1);
                TxtLessonsWeek.Text = _db.LessonPlans
                    .Count(p => p.CreatedAt >= weekStart && p.CreatedAt <= today)
                    .ToString();

                // TKB Compliance
                var complianceService = new TimetableComplianceService(_db);
                var complianceRate = complianceService.GetComplianceRate(today.Month, today.Year);
                TxtTkbCompliance.Text = $"{complianceRate}%";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi tải KPI Dashboard BGH");
            }
        }

        private void LoadEmulation()
        {
            try
            {
                var service = new EmulationService(_db);
                var ranking = service.CalcClassRanking(DateTime.Now.Month, DateTime.Now.Year);
                var top5 = ranking.Take(5).Select(r => new
                {
                    RankIcon = r.Rank switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $"#{r.Rank}" },
                    r.ClassName,
                    ScoreDisplay = $"{r.TotalScore} điểm"
                }).ToList();
                LvTopClasses.ItemsSource = top5;
            }
            catch (Exception ex) { Log.Error(ex, "Lỗi tải thi đua BGH"); }
        }

        private void LoadBulletins()
        {
            try
            {
                var bulletins = _db.Bulletins
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(5)
                    .ToList()
                    .Select(b => new
                    {
                        b.Title,
                        DateStr = b.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                    }).ToList();
                LvRecentBulletins.ItemsSource = bulletins;
            }
            catch (Exception ex) { Log.Error(ex, "Lỗi tải thông báo BGH"); }
        }
        // ═══ DRILL-DOWN: Click KPI cards ═══

        public void DrillDown_Attendance(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                var today = DateTime.Now.Date;
                var totalToday = _db.AttendanceRecords.Count(a => a.Date == today);
                if (totalToday == 0)
                {
                    MessageBox.Show("📋 Chưa có dữ liệu điểm danh ngày hôm nay.", "Chi tiết điểm danh", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var absentStudents = (from a in _db.AttendanceRecords
                                      join s in _db.Students on a.StudentId equals s.Id
                                      where a.Date == today && a.Status != "Present" && a.Status != "Có mặt"
                                      select new { s.FullName, s.StudentCode, a.Status }).Take(20).ToList();

                if (!absentStudents.Any())
                {
                    MessageBox.Show("✅ Hôm nay tất cả học sinh đều có mặt!", "Chi tiết điểm danh");
                    return;
                }

                var details = string.Join("\n", absentStudents.Select(a =>
                    $"• {a.FullName} ({a.StudentCode}) — Trạng thái: {a.Status}"));
                MessageBox.Show($"📋 Học sinh vắng hôm nay ({absentStudents.Count}):\n\n{details}",
                    "Chi tiết điểm danh", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Log.Error(ex, "Drill-down attendance error"); }
        }

        public void DrillDown_Lessons(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                var weekStart = DateTime.Now.Date.AddDays(-(int)DateTime.Now.DayOfWeek + 1);
                var plans = _db.LessonPlans
                    .Where(p => p.CreatedAt >= weekStart)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(10).ToList();

                if (!plans.Any())
                {
                    MessageBox.Show("⚠ Chưa có giáo án nào trong tuần này.", "Chi tiết bài giảng");
                    return;
                }

                var details = string.Join("\n", plans.Select(p =>
                    $"• {p.TeacherId}: {p.Title} [{p.Status}]"));
                MessageBox.Show($"📝 Giáo án tuần này ({plans.Count}):\n\n{details}",
                    "Chi tiết bài giảng", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { Log.Error(ex, "Drill-down lessons error"); }
        }

        public void DrillDown_TkbCompliance(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                var complianceService = new TimetableComplianceService(_db);
                var today = DateTime.Now;
                var nonCompliantList = complianceService.GetNonCompliantTeachers(today.Month, today.Year).Take(20).ToList();

                if (!nonCompliantList.Any())
                {
                    MessageBox.Show("✅ Tất cả giáo viên đều tuân thủ thời khóa biểu trong tháng này!", "Chi tiết tuân thủ TKB");
                    return;
                }

                var details = string.Join("\n", nonCompliantList.Select(nc =>
                    $"• GV: {nc.TeacherName} — {nc.Reason}"));
                MessageBox.Show($"📋 Danh sách vi phạm TKB trong tháng ({nonCompliantList.Count}):\n\n{details}",
                    "Chi tiết tuân thủ TKB", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Drill-down TKB compliance error");
            }
        }
        // ═══ P1-07: Mở Lịch công tác ═══
        private void BtnOpenCalendar_Click(object sender, RoutedEventArgs e)
        {
            try { NavigationService?.Navigate(new SchoolCalendarView()); }
            catch (Exception ex) { Log.Error(ex, "Open calendar error"); }
        }

        // P2-05: Dự giờ
        private void BtnObservation_Click(object sender, RoutedEventArgs e)
        {
            try { NavigationService?.Navigate(new ClassObservationView()); }
            catch (Exception ex) { Log.Error(ex, "Open observation error"); }
        }

        // ═══ P1-08: SMART ANALYTICS ═══════════════════════════
        private void LoadAnalytics()
        {
            try
            {
                var now = DateTime.Now;

                // ─── 1. Xu hướng điểm 6 tháng ──────────────────────
                var grades = _db.StudentGrades.ToList();
                var trendData = Enumerable.Range(0, 6)
                    .Select(i =>
                    {
                        var month = now.AddMonths(-5 + i);
                        var monthGrades = grades
                            .Where(g => g.UpdatedAt.Year == month.Year &&
                                        g.UpdatedAt.Month == month.Month)
                            .ToList();
                        double avg = monthGrades.Any()
                            ? Math.Round(monthGrades.Average(g => g.Score), 1) : 0;
                        return new MonthTrendPoint
                        {
                            MonthLabel = month.ToString("T/MM"),
                            Avg        = avg,
                            BarWidth   = avg * 20,
                            AvgDisplay = avg > 0 ? avg.ToString("F1") : "--",
                            BarColor   = avg >= 7 ? "#10B981" : avg >= 5 ? "#F59E0B" : "#EF4444"
                        };
                    }).ToList();
                IcMonthlyTrend.ItemsSource = trendData;

                // ─── 2. Môn học yếu nhất (join ClassRosters) ────────
                var rosters = _db.ClassRosters.ToList();
                var subjectAvg = grades
                    .GroupBy(g => rosters.FirstOrDefault(r => r.Id == g.RosterId)?.Subject ?? "Khác")
                    .Select(g => new { Subject = g.Key, Avg = g.Average(x => x.Score) })
                    .OrderBy(x => x.Avg)
                    .FirstOrDefault();

                if (subjectAvg != null)
                {
                    TxtWeakestSubject.Text = subjectAvg.Subject ?? "---";
                    TxtWeakestAvg.Text     = $"TB: {subjectAvg.Avg:F1} / 10";
                }

                // ─── 3. HS nguy cơ (F2.1: Early Warning System) ──────────────
                // Cập nhật dữ liệu phân tích trước khi hiển thị
                PredictiveAnalyticsService.RunDailyAnalysis();
                
                var atRisk = _db.Students
                    .Where(s => s.IsAtRisk)
                    .Select(s => new { StudentName = s.FullName, ClassName = s.ClassName, Reason = s.RiskReason })
                    .Take(5)
                    .ToList();

                if (atRisk.Any())
                {
                    LstAtRisk.ItemsSource = atRisk
                        .Select(r => $"• {r.StudentName} ({r.ClassName}) — {r.Reason}")
                        .ToList();
                    TxtNoRisk.Visibility = Visibility.Collapsed;
                }
                else
                {
                    LstAtRisk.ItemsSource = null;
                    TxtNoRisk.Visibility = Visibility.Visible;
                }

                Log.Information("[Analytics] Loaded: {Months} months trend, WeakSubject={Sub}, AtRisk={Risk}",
                    trendData.Count, subjectAvg?.Subject ?? "N/A", atRisk.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[PrincipalDashboard] LoadAnalytics error");
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadKPIs();
            LoadEmulation();
            LoadBulletins();
            LoadAnalytics();
            MessageBox.Show("✅ Dữ liệu đã được cập nhật!", "Làm mới", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnRefreshAnalytics_Click(object sender, RoutedEventArgs e)
        {
            LoadKPIs();
            LoadAnalytics();
        }

        // DTO cho biểu đồ xu hướng
        private class MonthTrendPoint
        {
            public string MonthLabel  { get; set; } = "";
            public double Avg         { get; set; }
            public double BarWidth    { get; set; }
            public string AvgDisplay  { get; set; } = "";
            public string BarColor    { get; set; } = "#3B82F6";
        }
    }
}

