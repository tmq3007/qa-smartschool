using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;

namespace QASmartClass.HealthRoom.Views
{
    public partial class HealthRecordInputDialog : Window
    {
        private readonly AppDbContext _db;

        public HealthRecordInputDialog(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            LoadStudents();
        }

        private void LoadStudents()
        {
            try
            {
                var activeStudents = _db.Students
                    .Where(s => s.Status == "Active")
                    .OrderBy(s => s.FullName)
                    .ToList();
                CboStudent.ItemsSource = activeStudents;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách học sinh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CboStudent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboStudent.SelectedItem is Student selectedStudent)
            {
                TxtClass.Text = selectedStudent.ClassName ?? "";
                TxtAge.Text = ""; // Clear custom age suggestion
            }
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
                if (CboStudent.SelectedItem is not Student selectedStudent)
                {
                    MessageBox.Show("Vui lòng chọn học sinh.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CboStudent.Focus();
                    return;
                }

                string heightText = TxtHeight.Text.Replace(',', '.').Trim();
                if (!double.TryParse(heightText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double h) || h < 50.0 || h > 250.0)
                {
                    MessageBox.Show("Vui lòng nhập chiều cao hợp lệ (từ 50 đến 250 cm).", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtHeight.Focus();
                    return;
                }

                string weightText = TxtWeight.Text.Replace(',', '.').Trim();
                if (!double.TryParse(weightText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double w) || w < 5.0 || w > 150.0)
                {
                    MessageBox.Show("Vui lòng nhập cân nặng hợp lệ (từ 5 đến 150 kg).", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtWeight.Focus();
                    return;
                }

                string vlText = TxtVisionLeft.Text.Replace(',', '.').Trim();
                if (!double.TryParse(vlText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double vl) || vl < 0.1 || vl > 2.0)
                {
                    MessageBox.Show("Vui lòng nhập thị lực mắt trái hợp lệ từ mốc 0.1 đến 2.0 (thang đo thập phân).", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtVisionLeft.Focus();
                    return;
                }

                string vrText = TxtVisionRight.Text.Replace(',', '.').Trim();
                if (!double.TryParse(vrText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double vr) || vr < 0.1 || vr > 2.0)
                {
                    MessageBox.Show("Vui lòng nhập thị lực mắt phải hợp lệ từ mốc 0.1 đến 2.0 (thang đo thập phân).", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtVisionRight.Focus();
                    return;
                }

                int? exactAge = null;
                if (!string.IsNullOrWhiteSpace(TxtAge.Text))
                {
                    if (int.TryParse(TxtAge.Text.Trim(), out int age) && age >= 3 && age <= 100)
                    {
                        exactAge = age;
                    }
                    else
                    {
                        MessageBox.Show("Vui lòng nhập tuổi thực tế hợp lệ (từ 3 đến 100).", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TxtAge.Focus();
                        return;
                    }
                }

                var record = new HealthRecord
                {
                    StudentId = selectedStudent.Id,
                    StudentName = selectedStudent.FullName,
                    ClassName = selectedStudent.ClassName,
                    Height = h,
                    Weight = w,
                    VisionLeft = vl,
                    VisionRight = vr,
                    ChronicConditions = TxtNotes.Text.Trim(),
                    ExactAge = exactAge,
                    ExamDate = DateTime.Today
                };

                _db.HealthRecords.Add(record);
                _db.SaveChanges();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu kết quả khám: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
