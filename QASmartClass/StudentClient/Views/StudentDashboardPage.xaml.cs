using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Serilog;

namespace QASmartClass.StudentClient.Views
{
    public partial class StudentDashboardPage : Page
    {
        public StudentDashboardPage()
        {
            InitializeComponent();
            string dateText = DateTime.Now.ToString("dddd, dd/MM/yyyy", new System.Globalization.CultureInfo("vi-VN"));
            if (!string.IsNullOrEmpty(dateText))
            {
                dateText = char.ToUpper(dateText[0]) + dateText.Substring(1);
            }
            txtDate.Text = $"📅 {dateText}    •    Trường THPT QA";
            Loaded += (_, _) => LoadDashboardData();
            Unloaded += (_, _) => UnsubscribeNetworkEvents();
        }

        private void LoadDashboardData()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.Database == null) return;

                var db = app.Database;

                // Load student identity via StudentIdentityService
                var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(db);
                var (currentId, currentCode, currentName) = identityService.GetCurrentStudent();
                var student = db.Students.Find(currentId);

                if (student != null)
                {
                    int streak = student.DailyStreak;
                    string streakText = streak > 0 ? $"  🔥 {streak} ngày" : "  ⚡ Bắt đầu học nào!";
                    txtWelcome.Text = $"Xin chào, {student.FullName} 👋{streakText}";
                }
                else
                {
                    txtWelcome.Text = $"Xin chào, {currentName} 👋";
                }

                // Load current lesson
                var todayLesson = db.Lessons
                    .Where(l => l.Status == "Approved" || l.Status == "Taught")
                    .OrderByDescending(l => l.UpdatedAt)
                    .FirstOrDefault();

                if (todayLesson != null)
                {
                    txtCurrentLessonClass.Text = $"{todayLesson.Subject} {todayLesson.ClassName} — Tiết {todayLesson.Period}";
                    txtCurrentLessonTitle.Text = todayLesson.Title;
                    txtCurrentLessonTeacher.Text = $"GV: {todayLesson.TeacherName}";
                    
                    int slideCount = db.LessonContents.Count(c => c.LessonId == todayLesson.Id);
                    txtCurrentLessonSlide.Text = slideCount > 0 ? $"Tài liệu: {slideCount} slide" : "Tài liệu: Trống";
                    txtCurrentLessonSlide.Visibility = Visibility.Visible;
                }
                else
                {
                    txtCurrentLessonClass.Text = "Không có bài giảng diễn ra";
                    txtCurrentLessonTitle.Text = "Các em có thể tự ôn tập hoặc xem lại các bài học cũ.";
                    txtCurrentLessonTeacher.Text = "GV: —";
                    txtCurrentLessonSlide.Text = "—";
                    txtCurrentLessonSlide.Visibility = Visibility.Collapsed;
                }

                // ═══ Load & display stats ═══
                string currentClass = app.StudentNetwork?.ClassName ?? "";
                var lessonCount = db.Lessons.Count(l => l.ScheduledFor != null && l.ScheduledFor.Value.Date == DateTime.Today
                    && (string.IsNullOrEmpty(currentClass) || l.ClassName == currentClass || l.Grade == currentClass));
                if (lessonCount == 0) lessonCount = db.Lessons.Count(l => (l.Status == "Approved" || l.Status == "Taught")
                    && (string.IsNullOrEmpty(currentClass) || l.ClassName == currentClass || l.Grade == currentClass));

                var quizCount = (from q in db.Quizzes
                                 join l in db.Lessons on q.LessonId equals l.Id
                                 where string.IsNullOrEmpty(currentClass) || l.ClassName == currentClass || l.Grade == currentClass
                                 select q).Count();
                var submitCount = db.QuizResults.Count(r => r.StudentId == currentId);
                double avgScore = 0;
                if (submitCount > 0)
                {
                    try 
                    { 
                        var results = db.QuizResults.Where(r => r.StudentId == currentId).ToList();
                        avgScore = results.Average(r => r.Score / 10.0);
                    }
                    catch { avgScore = 0; }
                }

                // Update stat cards
                txtLessonCount.Text = lessonCount.ToString();
                txtQuizCount.Text = quizCount.ToString();
                txtSubmitCount.Text = submitCount.ToString();
                txtAvgScore.Text = avgScore > 0 ? avgScore.ToString("F1") : "—";

                if (app?.StudentNetwork != null)
                {
                    app.StudentNetwork.MessageReceived -= StudentNetwork_MessageReceived;
                    app.StudentNetwork.MessageReceived += StudentNetwork_MessageReceived;
                }

                Log.Information("Student dashboard loaded: {Lessons} lessons, {Quizzes} quizzes, {Submitted} submitted, avg={Avg:F1}",
                    lessonCount, quizCount, submitCount, avgScore);
            }
            catch (Exception ex) { Log.Warning("Dashboard load error: {Err}", ex.Message); }
        }

        // ═══════════════════════════════════════════════════
        //  NAVIGATION — chuyển trang qua StudentShell
        // ═══════════════════════════════════════════════════

        /// <summary>Tìm parent StudentShell và điều hướng</summary>
        private void NavigateToPage(string tag)
        {
            try
            {
                var shell = Window.GetWindow(this) as StudentShell;
                if (shell != null)
                {
                    shell.NavigateTo(tag);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Dashboard navigate error: {Err}", ex.Message);
            }
        }

        // ═══ Quick Action Buttons ═══
        private void GoToHandRaise_Click(object sender, MouseButtonEventArgs e) => NavigateToPage("S7");
        private void GoToChat_Click(object sender, MouseButtonEventArgs e) => NavigateToPage("S6");

        private void StudentNetwork_MessageReceived(object? sender, string line)
        {
            if (line.StartsWith("CLAIM_ACK"))
            {
                Dispatcher.Invoke(() =>
                {
                    lblSeatStatus.Text = $"🟢 Ghế số {txtSeatInput.Text} - Đã xác nhận";
                    lblSeatStatus.Foreground = System.Windows.Media.Brushes.Green;
                });
            }
            else if (line.StartsWith("CLAIM_REJECTED|"))
            {
                var parts = line.Split('|');
                string reason = parts.Length > 1 ? parts[1] : "Lỗi không rõ";
                Dispatcher.Invoke(() =>
                {
                    lblSeatStatus.Text = $"🔴 Từ chối: {reason}";
                    lblSeatStatus.Foreground = System.Windows.Media.Brushes.Red;
                });
            }
        }

        private void RegisterSeat_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtSeatInput.Text, out var seat) && seat > 0)
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.StudentNetwork != null && app.StudentNetwork.IsConnected)
                {
                    lblSeatStatus.Text = "⏳ Đang gửi yêu cầu...";
                    lblSeatStatus.Foreground = System.Windows.Media.Brushes.Orange;
                    _ = app.StudentNetwork.SendAsync($"CLAIM_SEAT|{seat}");
                }
                else
                {
                    lblSeatStatus.Text = "⚠️ Chưa kết nối với máy GV!";
                    lblSeatStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            else
            {
                lblSeatStatus.Text = "⚠️ Nhập số ghế hợp lệ (vd: 1, 2, ...)";
                lblSeatStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void UnsubscribeNetworkEvents()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.StudentNetwork != null)
                {
                    app.StudentNetwork.MessageReceived -= StudentNetwork_MessageReceived;
                }
            }
            catch { }
        }

        // ═══ Stat Card Clicks ═══
        private void GoToLesson_Click(object sender, MouseButtonEventArgs e) => NavigateToPage("S2");
        private void GoToQuiz_Click(object sender, MouseButtonEventArgs e) => NavigateToPage("S3");
        private void GoToSubmit_Click(object sender, MouseButtonEventArgs e) => NavigateToPage("S4");
        private void GoToResults_Click(object sender, MouseButtonEventArgs e) => NavigateToPage("S8");
    }
}
