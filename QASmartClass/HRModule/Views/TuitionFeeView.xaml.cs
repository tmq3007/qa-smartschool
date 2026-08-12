using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace QASmartClass.HRModule.Views
{
    public partial class TuitionFeeView : Page
    {
        private AppDbContext? _db;

        public TuitionFeeView()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _db = new AppDbContext();
            LoadData();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.Dispose();
            _db = null;
        }

        private void LoadData()
        {
            if (_db == null) return;
            try
            {
                if (!_db.TuitionRecords.Any())
                {
                    _db.TuitionRecords.AddRange(
                        new TuitionRecord { StudentId = 1, StudentName = "Nguyễn Văn A", ClassName = "10A", Amount = 5000000, DueDate = DateTime.Today.AddDays(5), Status = "Unpaid" },
                        new TuitionRecord { StudentId = 2, StudentName = "Trần Thị B", ClassName = "10A", Amount = 5000000, DueDate = DateTime.Today.AddDays(-2), Status = "Overdue" },
                        new TuitionRecord { StudentId = 3, StudentName = "Lê Văn C", ClassName = "10B", Amount = 5000000, DueDate = DateTime.Today.AddDays(10), PaidDate = DateTime.Today.AddDays(-1), Status = "Paid", PaymentMethod = "Transfer" }
                    );
                    _db.SaveChanges();
                }

                FilterData();
            }
            catch (Exception ex) { Log.Warning("Tuition Load error: {Err}", ex.Message); }
        }

        private void FilterData()
        {
            if (_db == null) return;
            var query = _db.TuitionRecords.AsQueryable();
            DgTuition.ItemsSource = query.ToList();
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) FilterData();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadData();

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;

            var dialog = new CreateTuitionRecordDialog(_db);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                FilterData();
            }
        }

        private void BtnPay_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            if (DgTuition.SelectedItem is TuitionRecord record)
            {
                if (record.Status == "Paid")
                {
                    MessageBox.Show("Học phí này đã được đóng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                record.Status = "Paid";
                record.PaidDate = DateTime.Today;
                record.PaymentMethod = "Cash"; // Giả lập tiền mặt
                _db.SaveChanges();
                LoadData();
                MessageBox.Show($"Xác nhận đã thu tiền từ {record.StudentName}.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Vui lòng chọn 1 phiếu thu chưa đóng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void BtnNotify_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            try
            {
                this.IsEnabled = false;

                var unpaidRecords = _db.TuitionRecords
                    .Where(r => r.Status == "Unpaid" || r.Status == "Overdue")
                    .ToList();

                if (!unpaidRecords.Any())
                {
                    MessageBox.Show("Không có phụ huynh nào cần nhắc nợ học phí.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int count = 0;
                await Task.Run(() =>
                {
                    var notifService = new QASmartClass.Services.NotificationService(_db);
                    foreach (var record in unpaidRecords)
                    {
                        string reason = $"Học phí kỳ này của con em là {record.Amount:N0} VND chưa được thanh toán (Hạn nộp: {record.DueDate:dd/MM/yyyy}). Vui lòng thanh toán sớm.";
                        bool success = notifService.SendToParent(record.StudentId, "Nhắc nhở học phí", reason);
                        if (success) count++;
                    }
                });

                MessageBox.Show($"Đã tự động gửi thông báo nhắc nhở thành công cho {count} phụ huynh học sinh!", "Nhắc nợ Tự động", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi gửi thông báo nhắc nợ: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.IsEnabled = true;
            }
        }
    }
}
