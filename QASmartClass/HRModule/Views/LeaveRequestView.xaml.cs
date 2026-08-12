using QASmartClass.Data;
using QASmartClass.Staff.Services;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace QASmartClass.HRModule.Views
{
    public partial class LeaveRequestView : Page
    {
        private AppDbContext? _db;

        public LeaveRequestView()
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
            try
            {
                if (_db == null) return;
                if (!_db.LeaveRequests.Any())
                {
                    _db.LeaveRequests.AddRange(
                        new LeaveRequest { StaffId = 1, StaffName = "Nguyễn Văn A", LeaveType = "Sick", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(1), Reason = "Cảm sốt", Status = "Pending" },
                        new LeaveRequest { StaffId = 2, StaffName = "Trần Thị B", LeaveType = "Annual", StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(7), Reason = "Nghỉ phép năm", Status = "Approved" }
                    );
                    _db.SaveChanges();
                }

                DgLeaves.ItemsSource = _db.LeaveRequests.OrderByDescending(x => x.CreatedAt).ToList();
            }
            catch (Exception ex) { Log.Warning("LeaveRequest Load error: {Err}", ex.Message); }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadData();

        private async void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (!StaffSession.CanApprove())
            {
                MessageBox.Show("Bạn không có quyền duyệt đơn nghỉ phép.", "Lỗi phân quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_db != null && DgLeaves.SelectedItem is LeaveRequest req)
            {
                this.IsEnabled = false;
                try
                {
                    string approver = StaffSession.CurrentUser?.FullName ?? "Quản lý nhân sự";
                    req.Status = "Approved";
                    req.ApprovedBy = approver;
                    req.ResponseNotes = "Đồng ý";
                    
                    await _db.SaveChangesAsync();

                    // Ghi Audit log
                    QASmartClass.Services.AuditHelper.Log(_db, "Leave_Request_Approved", approver, $"Approved leave request for staff {req.StaffName}");

                    LoadData();
                    MessageBox.Show("Đã duyệt đơn nghỉ phép thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi lưu dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    this.IsEnabled = true;
                }
            }
        }

        private async void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (!StaffSession.CanApprove())
            {
                MessageBox.Show("Bạn không có quyền từ chối đơn nghỉ phép.", "Lỗi phân quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_db != null && DgLeaves.SelectedItem is LeaveRequest req)
            {
                var dialog = new RejectReasonDialog();
                dialog.Owner = Window.GetWindow(this);
                if (dialog.ShowDialog() == true)
                {
                    string rejectReason = dialog.ReasonText;
                    this.IsEnabled = false;
                    try
                    {
                        string approver = StaffSession.CurrentUser?.FullName ?? "Quản lý nhân sự";
                        req.Status = "Rejected";
                        req.ApprovedBy = approver;
                        req.ResponseNotes = rejectReason;
                        
                        await _db.SaveChangesAsync();

                        // Ghi Audit log
                        QASmartClass.Services.AuditHelper.Log(_db, "Leave_Request_Rejected", approver, $"Rejected leave request for staff {req.StaffName}. Lý do: {rejectReason}");

                        LoadData();
                        MessageBox.Show("Đã từ chối đơn nghỉ phép thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi lưu dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        this.IsEnabled = true;
                    }
                }
            }
        }
    }
}

