using QASmartClass.Data;
using QASmartClass.Staff.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.TeacherHub.Views
{
    public partial class SoftSkillRubricWindow : Window
    {
        private int _studentId;
        private string _studentName;

        public SoftSkillRubricWindow(int studentId, string studentName)
        {
            InitializeComponent();
            _studentId = studentId;
            _studentName = studentName;
            TxtStudentName.Text = $"Học sinh: {_studentName}";
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtTeamworkVal != null && SldTeamwork != null)
                TxtTeamworkVal.Text = SldTeamwork.Value.ToString("0.0");
            
            if (TxtPresentationVal != null && SldPresentation != null)
                TxtPresentationVal.Text = SldPresentation.Value.ToString("0.0");
            
            if (TxtCriticalVal != null && SldCritical != null)
                TxtCriticalVal.Text = SldCritical.Value.ToString("0.0");
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new AppDbContext();
                string currentTeacherName = StaffSession.CurrentUser?.FullName ?? "Giáo viên Hệ thống";
                var record = new SoftSkillRecord
                {
                    StudentId = _studentId,
                    TeamworkScore = SldTeamwork.Value,
                    PresentationScore = SldPresentation.Value,
                    CriticalThinkingScore = SldCritical.Value,
                    Notes = TxtNotes.Text.Trim(),
                    AssessorName = currentTeacherName,
                    Subject = "Chung",
                    ProjectName = "Đánh giá Định kỳ",
                    CreatedAt = DateTime.Now
                };

                db.SoftSkillRecords.Add(record);
                db.SaveChanges();

                // Log audit
                QASmartClass.Services.AuditHelper.Log(db, "Assess_SoftSkill", currentTeacherName, $"Assessed soft skills for Student ID {_studentId}");

                MessageBox.Show("✅ Đã lưu kết quả đánh giá kỹ năng mềm thành công!", "Lưu thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

