using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;

namespace QASmartClass.Admin.Controls
{
    public partial class SchoolDirectorDashboard : UserControl
    {
        private readonly AppDbContext? _db;
        private readonly PdfExportService? _pdfExportService;

        public SchoolDirectorDashboard()
        {
            InitializeComponent();
            
            if (Application.Current is QASmartTouch.App app && app.Database != null)
            {
                _db = app.Database;
                _pdfExportService = new PdfExportService(_db);
                LoadRealData();
            }
        }

        private void LoadRealData()
        {
            if (_db == null) return;
            try
            {
                // 1. Thống kê bài giảng mới trong tháng từ bảng Lessons
                int newLessonsCount = _db.Lessons.Count(l => l.CreatedAt >= DateTime.Now.AddDays(-30));
                txtNewLessons.Text = newLessonsCount.ToString();

                // 2. Thống kê giờ dạy từ bảng Lessons (giả định trung bình 45 phút = 0.75h một tiết học)
                int totalLessons = _db.Lessons.Count();
                double totalHours = totalLessons * 0.75;
                txtTotalHours.Text = $"{totalHours:F1}";

                // 3. Tỉ lệ hoạt động (giáo viên có bài giảng / tổng số giáo viên)
                int totalTeachers = _db.TeacherProfiles.Count();
                var activeTeacherNames = _db.Lessons.Select(l => l.TeacherName).Distinct().ToList();
                int activeTeachers = _db.TeacherProfiles.Count(t => activeTeacherNames.Contains(t.FullName));
                double usageRate = totalTeachers > 0 ? (double)activeTeachers / totalTeachers * 100 : 0;
                txtUsageRate.Text = totalTeachers > 0 ? $"{usageRate:F0}%" : "0%";

                // 4. Điểm trung bình từ StudentGrades
                double avgScore = _db.StudentGrades.Any() ? _db.StudentGrades.Average(g => g.Score) : 0.0;
                txtAvgScore.Text = $"{avgScore:F1}";

                // 5. Thống kê bài giảng theo tuần (biểu đồ chartUsage)
                var chartData = new List<object>();
                for (int i = 3; i >= 0; i--)
                {
                    var start = DateTime.Now.Date.AddDays(-7 * (i + 1));
                    var end = DateTime.Now.Date.AddDays(-7 * i);
                    int count = _db.Lessons.Count(l => l.CreatedAt >= start && l.CreatedAt < end);
                    double height = Math.Min(200, Math.Max(20, count * 25)); // Quy đổi chiều cao (min 20px, max 200px)
                    chartData.Add(new { Label = $"Tuần {4 - i}", ChartHeight = height });
                }
                chartUsage.ItemsSource = chartData;

                // 6. Danh sách giáo viên tích cực nhất từ bảng Lessons (truy vấn in-memory để tránh lỗi EF compile)
                var lessonTeacherNames = _db.Lessons
                    .Select(l => l.TeacherName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList();

                var topActiveTeachers = lessonTeacherNames
                    .GroupBy(name => name)
                    .OrderByDescending(g => g.Count())
                    .Take(4)
                    .Select(g => {
                        string name = g.Key;
                        var profile = _db.TeacherProfiles.FirstOrDefault(t => t.FullName == name || t.TeacherCode == name);
                        string subject = profile?.Subject ?? "Bộ môn khác";
                        string displayName = profile?.FullName ?? name;
                        string initials = displayName.Length >= 2 ? displayName.Substring(0, 2).ToUpper() : "GV";
                        return (object)new { Initials = initials, Name = displayName, Subject = subject, Hours = $"{g.Count()} tiết" };
                    })
                    .ToList();

                if (topActiveTeachers.Count == 0)
                {
                    topActiveTeachers = new List<object>
                    {
                        new { Initials = "HĐ", Name = "Hoàng Duy", Subject = "Toán Học", Hours = "0 tiết" },
                        new { Initials = "MN", Name = "Mai Ngọc", Subject = "Ngữ Văn", Hours = "0 tiết" },
                        new { Initials = "TK", Name = "Trần Kiên", Subject = "Tiếng Anh", Hours = "0 tiết" },
                        new { Initials = "PL", Name = "Phạm Lan", Subject = "Hóa Học", Hours = "0 tiết" }
                    };
                }

                listTopTeachers.ItemsSource = topActiveTeachers;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Không thể tải số liệu thật cho BGH Dashboard: " + ex.Message);
            }
        }

        private void ExportReport_Click(object sender, RoutedEventArgs e)
        {
            if (_pdfExportService == null || _db == null) return;

            // ── Dữ liệu mẫu cho Template 1 & 2 ─────────────────────────
            var dummyStudents = new List<Student>
            {
                new Student { Id = 1, StudentCode = "HS001", FullName = "Nguyễn Văn A" },
                new Student { Id = 2, StudentCode = "HS002", FullName = "Trần Thị B" },
                new Student { Id = 3, StudentCode = "HS003", FullName = "Lê Văn C" },
                new Student { Id = 4, StudentCode = "HS004", FullName = "Phạm Quốc D" },
            };
            var presentIds = new List<int> { 1, 3, 4 };

            var dummyGrades = new List<StudentGradeDto>
            {
                new StudentGradeDto { StudentCode = "HS001", FullName = "Nguyễn Văn A", Score = 8.5 },
                new StudentGradeDto { StudentCode = "HS002", FullName = "Trần Thị B",   Score = 6.0 },
                new StudentGradeDto { StudentCode = "HS003", FullName = "Lê Văn C",     Score = 9.2 },
                new StudentGradeDto { StudentCode = "HS004", FullName = "Phạm Quốc D", Score = 4.5 },
            };

            // ── Dữ liệu mẫu cho Template 3: Quiz Summary ────────────────
            var dummyQuiz = new QuizSummaryDto
            {
                QuizTitle    = "Kiểm tra 15 phút - Chương 3",
                QuizType     = "Competition",
                ClassName    = "10A1",
                TotalQuestions = 10,
                TimeLimitSeconds = 900,
                HeldAt       = DateTime.Now,
                Results      = new List<QuizResultRowDto>
                {
                    new QuizResultRowDto { FullName = "Nguyễn Văn A", CorrectCount = 9, TotalQuestions = 10, ScorePercent = 90, TimeSpentSeconds = 420 },
                    new QuizResultRowDto { FullName = "Trần Thị B",   CorrectCount = 6, TotalQuestions = 10, ScorePercent = 60, TimeSpentSeconds = 810 },
                    new QuizResultRowDto { FullName = "Lê Văn C",     CorrectCount = 8, TotalQuestions = 10, ScorePercent = 80, TimeSpentSeconds = 650 },
                    new QuizResultRowDto { FullName = "Phạm Quốc D", CorrectCount = 4, TotalQuestions = 10, ScorePercent = 40, TimeSpentSeconds = 890 },
                }
            };

            // ── Dữ liệu mẫu cho Template 4: Usage Report ────────────────
            var dummyUsage = new UsageReportDto
            {
                FromDate          = DateTime.Now.AddDays(-30),
                ToDate            = DateTime.Now,
                TotalSessions     = 42,
                TotalHours        = 168.5,
                TotalQuizzes      = 18,
                TotalFileTransfers = 95,
                TopEvents = new List<EventSummaryDto>
                {
                    new EventSummaryDto { EventType = "SESSION_START",  Count = 42, AvgDurationMs = 0 },
                    new EventSummaryDto { EventType = "QUIZ_STARTED",   Count = 18, AvgDurationMs = 900000 },
                    new EventSummaryDto { EventType = "FILE_TRANSFER",  Count = 95, AvgDurationMs = 3200 },
                    new EventSummaryDto { EventType = "BROADCAST_START",Count = 30, AvgDurationMs = 7200000 },
                    new EventSummaryDto { EventType = "SCREEN_LOCK",    Count = 25, AvgDurationMs = 0 },
                },
                DailyActivity = new List<DailyActivityDto>
                {
                    new DailyActivityDto { Date = DateTime.Now.AddDays(-1), EventCount = 85, TotalHours = 6.5 },
                    new DailyActivityDto { Date = DateTime.Now.AddDays(-2), EventCount = 72, TotalHours = 5.0 },
                    new DailyActivityDto { Date = DateTime.Now.AddDays(-3), EventCount = 90, TotalHours = 7.0 },
                    new DailyActivityDto { Date = DateTime.Now.AddDays(-6), EventCount = 60, TotalHours = 4.0 },
                    new DailyActivityDto { Date = DateTime.Now.AddDays(-7), EventCount = 78, TotalHours = 6.0 },
                }
            };

            try
            {
                // Gọi cả 4 templates
                string attPath   = _pdfExportService.ExportAttendanceReport("10A1", DateTime.Now, dummyStudents, presentIds);
                string gradePath = _pdfExportService.ExportGradeReport("10A1", "Toán", dummyGrades);
                string quizPath  = _pdfExportService.ExportQuizSummaryReport(dummyQuiz);
                string usagePath = _pdfExportService.ExportUsageReport(dummyUsage);

                var results = new List<string> { attPath, gradePath, quizPath, usagePath }
                                  .Where(p => !string.IsNullOrEmpty(p)).ToList();

                if (results.Count > 0)
                {
                    string summary = string.Join("\n", results.Select((p, i) => $"{i + 1}. {System.IO.Path.GetFileName(p)}"));
                    MessageBox.Show($"Đã xuất {results.Count}/4 báo cáo thành công:\n{summary}\n\nVào thư mục: {AppPaths.ExportsDir}",
                                    "Xuất PDF thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Mở thư mục exports
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AppPaths.ExportsDir) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("Không thể xuất báo cáo. Kiểm tra log để biết chi tiết.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất PDF: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
