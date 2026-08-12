using QASmartClass.Data;
using System.Globalization;
using System;
using System.Linq;
using System.Windows;

namespace QASmartClass.HRModule.Views
{
    public partial class CreateTuitionRecordDialog : Window
    {
        private readonly AppDbContext _db;

        public CreateTuitionRecordDialog(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            Loaded += CreateTuitionRecordDialog_Loaded;
        }

        private void CreateTuitionRecordDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var activeStudents = _db.Students
                    .Where(s => s.Status == "Active")
                    .OrderBy(s => s.FullName)
                    .ToList();

                CbStudent.ItemsSource = activeStudents.Select(s => new { s.Id, Display = $"{s.FullName} ({s.ClassName} - {s.StudentCode})" }).ToList();
                if (activeStudents.Any())
                {
                    CbStudent.SelectedIndex = 0;
                }

                DpDueDate.SelectedDate = DateTime.Today.AddDays(7);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách học sinh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (CbStudent.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn một học sinh.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int studentId = (int)CbStudent.SelectedValue;

            string amountText = TxtAmount.Text.Trim();
            if (!decimal.TryParse(amountText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Vui lòng nhập số tiền hợp lệ (> 0).", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DpDueDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Vui lòng chọn hạn nộp.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var student = _db.Students.FirstOrDefault(st => st.Id == studentId);
                if (student == null)
                {
                    MessageBox.Show("Không tìm thấy học sinh trong cơ sở dữ liệu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var newRecord = new TuitionRecord
                {
                    StudentId = student.Id,
                    StudentName = student.FullName,
                    ClassName = student.ClassName,
                    Amount = (double)amount, // Giữ kiểu tương thích với model TuitionRecord.Amount
                    DueDate = DpDueDate.SelectedDate.Value,
                    Status = "Unpaid",
                    PaymentMethod = string.Empty,
                    PaidDate = null
                };

                _db.TuitionRecords.Add(newRecord);
                _db.SaveChanges();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tạo phiếu thu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
