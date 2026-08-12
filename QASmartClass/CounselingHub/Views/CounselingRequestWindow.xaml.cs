using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;

namespace QASmartClass.CounselingHub.Views
{
    public partial class CounselingRequestWindow : Window
    {
        public CounselingRequestWindow()
        {
            InitializeComponent();
            LoadStudents();
        }

        private void LoadStudents()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var students = db.Students
                        .Where(s => s.Status == "Active")
                        .OrderBy(s => s.FullName)
                        .ToList()
                        .Select(s => new StudentDisplay
                        {
                            Id = s.Id,
                            ClassName = s.ClassName,
                            DisplayInfo = $"{s.StudentCode} - {s.FullName}"
                        }).ToList();

                    CboStudent.ItemsSource = students;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải danh sách học sinh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CboStudent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboStudent.SelectedItem is StudentDisplay selectedStudent)
            {
                TxtClassName.Text = selectedStudent.ClassName;
            }
            else
            {
                TxtClassName.Clear();
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (CboStudent.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn học sinh!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (CboProblemType.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn loại vấn đề!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    int studentId = (int)CboStudent.SelectedValue;
                    var student = await db.Students.FindAsync(studentId);
                    if (student == null) return;

                    var profile = new CounselingProfile
                    {
                        StudentId = student.Id,
                        StudentName = student.FullName,
                        ClassName = student.ClassName,
                        ProblemType = (CboProblemType.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Khác",
                        Notes = TxtNotes.Text.Trim(),
                        CreatedAt = DateTime.Now,
                        Status = StatusConstants.CounselingStatus.Pending
                    };

                    db.CounselingProfiles.Add(profile);
                    await db.SaveChangesAsync();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu hồ sơ tư vấn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private class StudentDisplay
        {
            public int Id { get; set; }
            public string ClassName { get; set; } = "";
            public string DisplayInfo { get; set; } = "";
        }
    }
}
