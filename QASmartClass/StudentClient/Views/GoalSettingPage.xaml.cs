using QASmartClass.Data;
using System.Globalization;
using QASmartClass.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.StudentClient.Views
{
    public partial class GoalSettingPage : Page
    {
        private readonly AppDbContext _db;
        private readonly int _studentId;

        public GoalSettingPage(AppDbContext db, int studentId)
        {
            InitializeComponent();
            _db = db;
            _studentId = studentId;
            Loaded += (_, __) => LoadAll();
        }

        private void LoadAll()
        {
            LoadGoals();
            LoadAiSuggestion();
        }

        private void UpdateStudentGoals()
        {
            try
            {
                var goals = _db.StudentGoals.Where(g => g.StudentId == _studentId).ToList();
                if (!goals.Any()) return;

                // Lấy niên khóa hiện hành của học sinh
                var activeRoster = (from crs in _db.ClassRosterStudents
                                    join r in _db.ClassRosters on crs.RosterId equals r.Id
                                    where crs.StudentId == _studentId && r.IsActive
                                    select r.SchoolYear).FirstOrDefault() ?? "2023-2024";

                bool hasChanges = false;
                foreach (var goal in goals)
                {
                    string rSem = goal.Semester switch
                    {
                        "Học kỳ I" => "HK1",
                        "Học kỳ II" => "HK2",
                        _ => goal.Semester
                    };

                    var targetRosters = (from crs in _db.ClassRosterStudents
                                         join r in _db.ClassRosters on crs.RosterId equals r.Id
                                         where crs.StudentId == _studentId &&
                                               r.Subject == goal.Subject &&
                                               r.SchoolYear == activeRoster &&
                                               (rSem == "Cả năm" || r.Semester == rSem)
                                         select r.Id).ToList();

                    var grades = _db.StudentGrades
                        .Where(g => g.StudentId == _studentId && targetRosters.Contains(g.RosterId))
                        .ToList();

                    if (grades.Any())
                    {
                        double totalWeighted = 0;
                        int totalWeight = 0;
                        foreach (var g in grades)
                        {
                            int weight = g.GradeTypeId switch
                            {
                                1 => 1,
                                2 => 2,
                                3 => 3,
                                _ => 1
                            };
                            totalWeighted += g.Score * weight;
                            totalWeight += weight;
                        }
                        double actual = totalWeight > 0 ? Math.Round(totalWeighted / totalWeight, 2) : 0.0;

                        string newStatus = "Active";
                        if (actual >= goal.TargetScore)
                        {
                            newStatus = "Achieved";
                        }
                        else
                        {
                            bool hasFinal = grades.Any(g => g.GradeTypeId == 3); // Điểm cuối kỳ
                            if (hasFinal) newStatus = "Failed";
                        }

                        if (goal.ActualScore != actual || goal.Status != newStatus)
                        {
                            goal.ActualScore = actual;
                            goal.Status = newStatus;
                            hasChanges = true;
                        }
                    }
                }

                if (hasChanges)
                {
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Lỗi tự động cập nhật mục tiêu học sinh: {Err}", ex.Message);
            }
        }

        private void LoadGoals()
        {
            UpdateStudentGoals();
            try
            {
                var goals = _db.StudentGoals
                    .Where(g => g.StudentId == _studentId)
                    .OrderByDescending(g => g.CreatedAt)
                    .ToList();

                var green = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                var blue  = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
                var red   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                var gray  = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));

                LstGoals.ItemsSource = goals.Select(g =>
                {
                    double progress = g.TargetScore > 0 ? Math.Min(g.ActualScore / g.TargetScore, 1.0) : 0;
                    return new
                    {
                        SubjectLine = $"{g.Subject} — Mục tiêu: {g.TargetScore:F1}",
                        StatusText = g.Status.StartsWith("Achieved") ? "Đạt" : g.Status == "Failed" ? "Chưa đạt" : "Đang thực hiện",
                        StatusBg = g.Status.StartsWith("Achieved") ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4"))
                                 : g.Status == "Failed" ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF2F2"))
                                 : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF")),
                        StatusFg = g.Status.StartsWith("Achieved") ? green : g.Status == "Failed" ? red : blue,
                        SemesterText = g.Semester,
                        ProgressWidth = progress * 250,
                        ProgressBrush = g.Status.StartsWith("Achieved") ? green : g.Status == "Failed" ? red : blue,
                        ScoreDisplay = g.ActualScore > 0 ? g.ActualScore.ToString("F1") : "—",
                        TargetDisplay = $"/ {g.TargetScore:F1}"
                    };
                }).ToList();

            }
            catch (Exception ex) { Log.Warning("[GoalSetting] Load error: {Err}", ex.Message); }
        }

        private void LoadAiSuggestion()
        {
            try
            {
                var student = _db.Students.Find(_studentId);
                var myClass = student?.ClassName ?? "";
                var rosters = _db.ClassRosters
                    .Where(r => r.ClassName == myClass)
                    .ToList();

                var subjects = rosters.Select(r => r.Subject).Distinct().ToList();
                var suggestions = new List<string>();

                foreach (var subj in subjects.Take(3))
                {
                    var rosterId = rosters.FirstOrDefault(r => r.Subject == subj)?.Id ?? 0;
                    var recentGrades = _db.StudentGrades
                        .Where(g => g.StudentId == _studentId && g.RosterId == rosterId)
                        .OrderByDescending(g => g.UpdatedAt)
                        .Take(3).ToList();

                    if (recentGrades.Any())
                    {
                        double avg = Math.Round(recentGrades.Average(g => g.Score), 1);
                        double target = Math.Min(avg + 0.5, 10);
                        suggestions.Add($"{subj}: TB 3 bai gan nhat = {avg:F1} → Goi y: {target:F1}");
                    }
                }

                TxtAiSuggestion.Text = suggestions.Any()
                    ? string.Join("\n", suggestions)
                    : "Chua du du lieu de goi y. Hay hoan thanh them bai kiem tra.";
            }
            catch { TxtAiSuggestion.Text = "Khong the phan tich."; }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Window
            {
                Title = "Dat muc tieu", Width = 380, Height = 340,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                FontFamily = new FontFamily("Segoe UI"), ResizeMode = ResizeMode.NoResize
            };
            var sp = new StackPanel { Margin = new Thickness(20) };

            var cboSubj = new ComboBox { FontSize = 13, Margin = new Thickness(0, 0, 0, 10) };
            foreach (var s in new[] { "Toán", "Vật lý", "Hóa học", "Sinh học", "Ngữ văn", "Tiếng Anh", "Lịch sử", "Địa lý", "GDCD", "Tin học" })
                cboSubj.Items.Add(s);
            cboSubj.SelectedIndex = 0;
            sp.Children.Add(Lbl("Môn học:")); sp.Children.Add(cboSubj);

            var txtTarget = new TextBox { Text = "8.0", FontSize = 13, Margin = new Thickness(0, 0, 0, 10) };
            sp.Children.Add(Lbl("Điểm mục tiêu:")); sp.Children.Add(txtTarget);

            var cboSem = new ComboBox { FontSize = 13, Margin = new Thickness(0, 0, 0, 16) };
            cboSem.Items.Add("Học kỳ I"); cboSem.Items.Add("Học kỳ II"); cboSem.Items.Add("Cả năm");
            cboSem.SelectedIndex = 0;
            sp.Children.Add(Lbl("Học kỳ:")); sp.Children.Add(cboSem);

            var btnSave = new Button
            {
                Content = "Luu muc tieu", FontSize = 14, Padding = new Thickness(16, 8, 16, 8),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                Foreground = Brushes.White, BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            btnSave.Click += (_, __) =>
            {
                if (!btnSave.IsEnabled) return;
                btnSave.IsEnabled = false;
                try
                {
                    if (!double.TryParse(txtTarget.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double target) || target < 0.0 || target > 10.0)
                    {
                        MessageBox.Show("Điểm mục tiêu phải nằm trong thang điểm từ 0.0 đến 10.0!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        btnSave.IsEnabled = true;
                        return;
                    }
                    _db.StudentGoals.Add(new StudentGoal
                    {
                        StudentId = _studentId,
                        Subject = cboSubj.SelectedItem?.ToString() ?? "Toán",
                        TargetScore = target,
                        Semester = cboSem.SelectedItem?.ToString() ?? "Học kỳ I"
                    });
                    _db.SaveChanges();
                    MessageBox.Show("Đã lưu mục tiêu!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    dlg.Close();
                    LoadAll();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    btnSave.IsEnabled = true;
                }
            };
            sp.Children.Add(btnSave);
            dlg.Content = sp;
            dlg.ShowDialog();
        }

        private static TextBlock Lbl(string t) => new TextBlock
        {
            Text = t, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
            Margin = new Thickness(0, 0, 0, 4)
        };
    }
}

