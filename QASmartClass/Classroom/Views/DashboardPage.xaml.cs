using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Classroom.Helpers;
using Serilog;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QASmartClass.Classroom.Views
{
    public partial class DashboardPage : Page, INavigatedPage, IDisposable
    {
        private bool _isDataLoaded = false;
        private bool _isLoading = false;

        public DashboardPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            // Chỉ load nếu OnNavigatedToAsync() chưa kịp chạy (fallback)
            if (!_isDataLoaded && !_isLoading)
            {
                _ = LoadDataAsync();
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            _isDataLoaded = false; // Cho phép reload
            _ = LoadDataAsync();
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadDataAsync();
        }

        private bool _isDisposed = false;
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            // Unsubscribe from Loaded event to prevent reference leaks
            Loaded -= OnPageLoaded;

            Log.Information("DashboardPage disposed successfully");
        }

        private async Task LoadDataAsync()
        {
            if (_isLoading) return; // Guard chống gọi trùng lặp
            _isLoading = true;

            try
            {
                // → ClassroomAppContext
                var db = ClassroomAppContext.Db;
                var activeRoster = ClassroomAppContext.ClassRoster?.ActiveRoster;

                if (activeRoster == null)
                {
                    txtSubtitle.Text = "Chưa chọn lớp. Hãy chọn lớp ở danh sách bên trái để xem thống kê.";
                    await UpdateStepIndicatorStatesAsync();
                    return;
                }

                txtSubtitle.Text = $"Tổng quan tình hình học tập - Lớp {activeRoster.DisplayName}";
                int rosterId = activeRoster.Id;

                // 1. Sĩ số
                int totalStudents = ClassroomAppContext.ClassRoster.GetActiveStudents().Count;
                txtTotalStudents.Text = totalStudents.ToString();

                // 2. Điểm danh
                var attendanceRecords = await db.AttendanceRecords.Where(r => r.RosterId == rosterId).ToListAsync();
                int totalAttendances = attendanceRecords.Count;
                int presentCount = attendanceRecords.Count(r => r.Status == "present" || r.Status == "late");
                
                double attRate = totalAttendances > 0 ? (double)presentCount / totalAttendances * 100 : 100;
                txtAttendanceRate.Text = $"{attRate:F1}%";

                // 3. Vắng/Trễ nhiều (>= 2 buổi)
                var absenceCounts = attendanceRecords
                    .Where(r => r.Status == "absent" || r.Status == "late")
                    .GroupBy(r => r.StudentId)
                    .Select(g => new { StudentId = g.Key, Count = g.Count() })
                    .Where(x => x.Count >= 2)
                    .OrderByDescending(x => x.Count)
                    .ToList();

                // 4. Điểm trung bình và xếp hạng
                var grades = await db.StudentGrades.Where(g => g.RosterId == rosterId).ToListAsync();
                
                // Thu thập các ID học sinh cần lấy tên để tối ưu truy vấn thành 1 câu duy nhất (chống N+1)
                var top5StudentIds = grades.GroupBy(g => g.StudentId)
                    .Select(g => new { StudentId = g.Key, Avg = g.Average(x => x.Score) })
                    .OrderByDescending(x => x.Avg)
                    .Take(5)
                    .Select(x => x.StudentId)
                    .ToList();

                var absenceStudentIds = absenceCounts.Select(x => x.StudentId).ToList();
                var combinedStudentIds = top5StudentIds.Concat(absenceStudentIds).Distinct().ToList();

                // Tải danh sách tên học sinh bất đồng bộ
                var studentList = await db.Students.Where(s => combinedStudentIds.Contains(s.Id)).ToListAsync();
                var studentMap = studentList.ToDictionary(s => s.Id, s => s.FullName);

                var warningList = absenceCounts.Select(x => new
                {
                    Name = studentMap.TryGetValue(x.StudentId, out var name) ? name : "Unknown",
                    AbsenceCount = $"{x.Count} buổi"
                }).ToList();

                txtWarnings.Text = warningList.Count.ToString();
                
                if (warningList.Count > 0)
                {
                    listAbsences.ItemsSource = warningList;
                    listAbsences.Visibility = Visibility.Visible;
                    txtNoAbsences.Visibility = Visibility.Collapsed;
                }
                else
                {
                    listAbsences.Visibility = Visibility.Collapsed;
                    txtNoAbsences.Visibility = Visibility.Visible;
                }

                if (grades.Count > 0)
                {
                    // Avg score
                    double avgScore = grades.Average(g => g.Score);
                    txtAvgScore.Text = $"{avgScore:F2}";

                    // Top 5 Students by Average Score
                    var studentAverages = grades.GroupBy(g => g.StudentId)
                        .Select(g => new
                        {
                            StudentId = g.Key,
                            Avg = g.Average(x => x.Score)
                        })
                        .OrderByDescending(x => x.Avg)
                        .Take(5)
                        .ToList();

                    var top5 = studentAverages.Select((x, i) => new
                    {
                        Rank = i + 1,
                        Name = studentMap.TryGetValue(x.StudentId, out var name) ? name : "Unknown",
                        Score = $"{x.Avg:F1}"
                    }).ToList();
                    
                    listTopStudents.ItemsSource = top5;

                    // 5. Phân phối điểm số
                    int countGioi = grades.Count(g => g.Score >= 8);
                    int countKha = grades.Count(g => g.Score >= 6.5 && g.Score < 8);
                    int countTB = grades.Count(g => g.Score >= 5 && g.Score < 6.5);
                    int countYeu = grades.Count(g => g.Score < 5);
                    int totalGrades = grades.Count;

                    txtGioi.Text = countGioi.ToString();
                    txtKha.Text = countKha.ToString();
                    txtTB.Text = countTB.ToString();
                    txtYeu.Text = countYeu.ToString();

                    barGioi.Value = totalGrades > 0 ? (double)countGioi / totalGrades * 100 : 0;
                    barKha.Value = totalGrades > 0 ? (double)countKha / totalGrades * 100 : 0;
                    barTB.Value = totalGrades > 0 ? (double)countTB / totalGrades * 100 : 0;
                    barYeu.Value = totalGrades > 0 ? (double)countYeu / totalGrades * 100 : 0;
                }
                else
                {
                    txtAvgScore.Text = "N/A";
                    listTopStudents.ItemsSource = null;
                }

                Log.Information("Dashboard loaded for Roster {RosterId}", rosterId);
                await UpdateStepIndicatorStatesAsync();
                _isDataLoaded = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load Dashboard data");
                ClassroomDialog.Error($"Lỗi tải dữ liệu Dashboard: {ex.Message}", "Lỗi");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void NavigateFromDashboard(string targetFormId)
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow is ClassroomShell shell)
            {
                shell.NavigateTo(targetFormId);
            }
        }

        private void Step1_Click(object sender, RoutedEventArgs e)
        {
            NavigateFromDashboard("F15");
        }

        private void Step2_Click(object sender, RoutedEventArgs e)
        {
            NavigateFromDashboard("F27");
        }

        private void Step3_Click(object sender, RoutedEventArgs e)
        {
            NavigateFromDashboard("F18");
        }

        private void Step4_Click(object sender, RoutedEventArgs e)
        {
            NavigateFromDashboard("F4");
        }

        private void Step5_Click(object sender, RoutedEventArgs e)
        {
            NavigateFromDashboard("F6");
        }

        private void Step6_Click(object sender, RoutedEventArgs e)
        {
            NavigateFromDashboard("F28");
        }

        private async Task UpdateStepIndicatorStatesAsync()
        {
            try
            {
                // → ClassroomAppContext
                var db = ClassroomAppContext.Db;
                var activeRoster = ClassroomAppContext.ClassRoster?.ActiveRoster;

                int step1 = 0;
                int step2 = 0;
                int step3 = 0;
                int step4 = 0;
                int step5 = 0;
                int step6 = 0;

                // Step 1: Chọn lớp
                if (activeRoster != null)
                {
                    step1 = 2; // Completed
                }
                else
                {
                    step1 = 1; // Active
                }

                // Step 2: Điểm danh
                if (step1 == 2)
                {
                    int rosterId = activeRoster!.Id;
                    bool hasAttendance = await db.AttendanceRecords.AnyAsync(r => r.RosterId == rosterId);
                    if (hasAttendance)
                    {
                        step2 = 2;
                    }
                    else
                    {
                        step2 = 1;
                    }
                }

                // Step 3: Khởi động
                if (step2 == 2)
                {
                    int rosterId = activeRoster!.Id;
                    bool hasSurveys = await db.Surveys.AnyAsync(s => s.TargetClasses.Contains(activeRoster.DisplayName) || s.TargetClasses.Contains(activeRoster.Id.ToString()));
                    if (hasSurveys)
                    {
                        step3 = 2;
                    }
                    else
                    {
                        step3 = 1;
                    }
                }

                // Step 4: Giảng dạy
                if (step3 == 2)
                {
                    if (ClassroomAppContext.Session != null && ClassroomAppContext.Session.IsSessionActive)
                    {
                        step4 = 2;
                    }
                    else
                    {
                        step4 = 1;
                    }
                }

                // Step 5: Luyện tập
                if (step4 == 2)
                {
                    bool hasQuiz = await db.Quizzes.AnyAsync() || await db.MathQuizHistories.AnyAsync() || await db.GrammarQuizHistories.AnyAsync();
                    if (hasQuiz)
                    {
                        step5 = 2;
                    }
                    else
                    {
                        step5 = 1;
                    }
                }

                // Step 6: Giao bài tập
                if (step5 == 2)
                {
                    bool hasHomework = await db.Homeworks.AnyAsync();
                    if (hasHomework)
                    {
                        step6 = 2;
                    }
                    else
                    {
                        step6 = 1;
                    }
                }

                ApplyStepStyle(borderStep1, txtStep1Title, txtStep1Desc, "Bước 1", "📋 Chọn lớp học", step1);
                ApplyStepStyle(borderStep2, txtStep2Title, txtStep2Desc, "Bước 2", "👥 Điểm danh", step2);
                ApplyStepStyle(borderStep3, txtStep3Title, txtStep3Desc, "Bước 3", "⚡ Khởi động", step3);
                ApplyStepStyle(borderStep4, txtStep4Title, txtStep4Desc, "Bước 4", "🖥️ Tiến trình dạy", step4);
                ApplyStepStyle(borderStep5, txtStep5Title, txtStep5Desc, "Bước 5", "🏆 Luyện tập/Quiz", step5);
                ApplyStepStyle(borderStep6, txtStep6Title, txtStep6Desc, "Bước 6", "📝 Giao bài tập", step6);
            }
            catch (Exception ex)
            {
                Log.Warning("Error updating step indicator states: {Err}", ex.Message);
            }
        }

        private void ApplyStepStyle(Border border, TextBlock txtTitle, TextBlock txtDesc, string originalTitle, string originalDesc, int state)
        {
            if (border == null || txtTitle == null || txtDesc == null) return;

            border.BeginAnimation(Border.BorderBrushProperty, null);
            border.BeginAnimation(Border.BackgroundProperty, null);

            if (state == 2) // Completed
            {
                border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 252, 231)); // #DCFCE7
                border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94)); // #22C55E
                border.BorderThickness = new Thickness(1.5);
                txtTitle.Text = "✓ " + originalTitle;
                txtTitle.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(21, 128, 61)); // #15803D
                txtDesc.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59)); // #1E293B
            }
            else if (state == 1) // Active
            {
                border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 249, 195)); // #FEF9C3
                border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(234, 179, 8)); // #EAB308
                border.BorderThickness = new Thickness(2);
                txtTitle.Text = "▶ " + originalTitle;
                txtTitle.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(161, 98, 7)); // #A16207
                txtDesc.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59)); // #1E293B

                var colorAnim = new System.Windows.Media.Animation.ColorAnimation
                {
                    From = System.Windows.Media.Color.FromRgb(234, 179, 8),
                    To = System.Windows.Media.Color.FromRgb(254, 240, 138),
                    Duration = TimeSpan.FromSeconds(1),
                    AutoReverse = true,
                    RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
                };
                var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(234, 179, 8));
                border.BorderBrush = brush;
                brush.BeginAnimation(System.Windows.Media.SolidColorBrush.ColorProperty, colorAnim);
            }
            else // Waiting
            {
                border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)); // #F8FAFC
                border.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)); // #E2E8F0
                border.BorderThickness = new Thickness(1);
                txtTitle.Text = originalTitle;
                txtTitle.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)); // #94A3B8
                txtDesc.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)); // #64748B
            }
        }
    }
}
