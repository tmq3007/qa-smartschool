using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.Leadership.Views
{
    public partial class EmulationBoardPage : Page
    {
        private readonly AppDbContext _db;
        private readonly EmulationService _emulationService;
        private string _currentSchoolYear = "2025-2026";

        public EmulationBoardPage(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            _emulationService = new EmulationService(_db);

            // Populate months
            for (int m = 1; m <= 12; m++)
                CbMonth.Items.Add(new ComboBoxItem { Content = $"Tháng {m:D2}/{DateTime.Now.Year}", Tag = m.ToString() });
            CbMonth.SelectedIndex = DateTime.Now.Month - 1;

            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;
            LoadMonthlyData();
            LoadStudentEmulation();
            LoadClassEmulation();
            LoadComparison();
        }

        private void CbMonth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadMonthlyData();
        }

        private void CbSemester_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                LoadStudentEmulation();
                LoadClassEmulation();
            }
        }

        private int GetSelectedMonth() =>
            int.TryParse((CbMonth.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out int m) ? m : DateTime.Now.Month;

        private string GetSelectedSemester() =>
            (CbSemester?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "HK2";

        // ════════════════════════════════════════════════════════════
        //  TAB 1: Xếp hạng tháng (existing)
        // ════════════════════════════════════════════════════════════

        private void LoadMonthlyData()
        {
            try
            {
                int month = GetSelectedMonth();
                int year = DateTime.Now.Year;

                var classRanking = _emulationService.CalcClassRanking(month, year);
                LvClassRanking.ItemsSource = classRanking;

                var teacherRanking = _emulationService.CalcTeacherEmulation(month, year);
                LvTeacherRanking.ItemsSource = teacherRanking;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi tải bảng vàng thi đua");
            }
        }

        // ════════════════════════════════════════════════════════════
        //  TAB 2: Thi đua HS (WI-15)
        // ════════════════════════════════════════════════════════════

        private void LoadStudentEmulation()
        {
            try
            {
                string semester = GetSelectedSemester();
                var students = _emulationService.CalcStudentEmulation(semester, _currentSchoolYear);

                LvStudentEmulation.ItemsSource = students;

                // Update KPI badges
                TxtHSG.Text = $"HSG: {students.Count(s => s.EmulationTitle == "HSG")}";
                TxtHSTT.Text = $"HSTT: {students.Count(s => s.EmulationTitle == "HSTT")}";
                TxtHSTB.Text = $"HSTB: {students.Count(s => s.EmulationTitle == "HSTB")}";
                TxtFail.Text = $"Chưa đạt: {students.Count(s => s.EmulationTitle == "Chưa đạt")}";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[EmulationBoard] Lỗi tải thi đua HS");
            }
        }

        // ════════════════════════════════════════════════════════════
        //  TAB 3: Thi đua Lớp (WI-15)
        // ════════════════════════════════════════════════════════════

        private void LoadClassEmulation()
        {
            try
            {
                string semester = GetSelectedSemester();
                var classes = _emulationService.CalcClassEmulation(semester, _currentSchoolYear);
                LvClassEmulation.ItemsSource = classes;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[EmulationBoard] Lỗi tải thi đua lớp");
            }
        }

        // ════════════════════════════════════════════════════════════
        //  TAB 4: So sánh HK1 vs HK2 (WI-15)
        // ════════════════════════════════════════════════════════════

        private void LoadComparison()
        {
            try
            {
                var comparisons = _emulationService.CompareEmulationBySemester(_currentSchoolYear);
                LvCompare.ItemsSource = comparisons;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[EmulationBoard] Lỗi tải so sánh HK1/HK2");
            }
        }

        // ════════════════════════════════════════════════════════════
        //  EXPORT
        // ════════════════════════════════════════════════════════════

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;
            try
            {
                int month = GetSelectedMonth();
                int year = DateTime.Now.Year;
                string semester = GetSelectedSemester();
                string schoolYear = _currentSchoolYear;

                string outputDir = PdfTemplateHelper.GetOutputDirectory();

                var result = await System.Threading.Tasks.Task.Run(() =>
                {
                    using var db = new AppDbContext();
                    var emulationService = new EmulationService(db);

                    var classes = emulationService.CalcClassRanking(month, year);
                    var teachers = emulationService.CalcTeacherEmulation(month, year);
                    var studentEmulations = emulationService.CalcStudentEmulation(semester, schoolYear);
                    var classEmulations = emulationService.CalcClassEmulation(semester, schoolYear);

                    string content = $"BẢNG THI ĐUA TỔNG HỢP — Năm học {schoolYear}\n{"=".PadRight(60, '=')}\n\n";

                    content += $"I. XẾP HẠNG THÁNG {month:D2}/{year}\n\n";
                    content += "XẾP HẠNG LỚP HỌC:\n";
                    foreach (var c in classes)
                        content += $"  #{c.Rank} {c.ClassName} — Nề nếp: {c.ConductScore} | Học lực: {c.AcademicScore} | Tổng: {c.TotalScore}\n";

                    content += "\nGIÁO VIÊN TIÊU BIỂU:\n";
                    foreach (var t in teachers)
                        content += $"  {t.TeacherName} — Hoàn thành: {t.CompletedLessons} GA | Đúng hạn: {t.OnTimeTaskRate}% | Tổng: {t.TotalScore}\n";

                    content += $"\n\nII. THI ĐUA HỌC SINH — {semester}\n\n";
                    content += $"  HSG: {studentEmulations.Count(s => s.EmulationTitle == "HSG")} | " +
                               $"HSTT: {studentEmulations.Count(s => s.EmulationTitle == "HSTT")} | " +
                               $"HSTB: {studentEmulations.Count(s => s.EmulationTitle == "HSTB")} | " +
                               $"Chưa đạt: {studentEmulations.Count(s => s.EmulationTitle == "Chưa đạt")}\n\n";
                    foreach (var s in studentEmulations.Take(20))
                        content += $"  #{s.Rank} {s.StudentName} ({s.ClassName}) — GPA: {s.GPA:0.00} | HK: {s.ConductClassification} | {s.EmulationTitle}\n";

                    content += $"\n\nIII. THI ĐUA LỚP — {semester}\n\n";
                    foreach (var c in classEmulations)
                        content += $"  #{c.Rank} {c.ClassName} — GPA TB: {c.AvgGPA:0.00} | Chuyên cần: {c.AttendanceRate:0.0}% | {c.ClassTitle}\n";

                    string filePath = System.IO.Path.Combine(outputDir, $"ThiDua_TongHop_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                    System.IO.File.WriteAllText(filePath, content);
                    return filePath;
                });

                MessageBox.Show($"📄 Đã xuất báo cáo thi đua thành công!\n\nFile: {result}", "Xuất báo cáo", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(result) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi xuất báo cáo thi đua");
                MessageBox.Show("Đã xảy ra lỗi khi xuất báo cáo.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }
    }
}

