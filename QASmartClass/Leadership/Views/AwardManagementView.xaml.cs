using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;

namespace QASmartClass.Leadership.Views
{
    public partial class AwardManagementView : Page
    {
        private readonly AppDbContext _db;
        private readonly AwardService _awardService;
        private int _pendingRejectRecordId;

        public AwardManagementView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;
            _db = new AppDbContext();
            _awardService = new AwardService(_db);

            // WI-09: Synchronize UserSessionService session role guard
            if (QASmartClass.Staff.Services.StaffSession.CurrentUser != null)
            {
                UserSessionService.Instance.SetSession(QASmartClass.Staff.Services.StaffSession.CurrentUser.TeacherCode);
            }

            LoadSchoolYears();
            RefreshAll();
            Unloaded += (s, e) => { _db?.Dispose(); };

            // Phân quyền cột thao tác duyệt
            bool canApprove = QASmartClass.Staff.Services.StaffSession.IsLoggedIn && QASmartClass.Staff.Services.StaffSession.CanApprove();
            if (!canApprove && dgPending.Columns.Count > 7)
            {
                dgPending.Columns[7].Visibility = Visibility.Collapsed;
            }
        }

        private void LoadSchoolYears()
        {
            try
            {
                var years = _db.AwardRecords
                    .Select(a => a.SchoolYear)
                    .Where(y => !string.IsNullOrEmpty(y))
                    .Distinct()
                    .ToList();

                string currentYear = $"{DateTime.Now.Year - 1}-{DateTime.Now.Year}";
                if (!years.Contains(currentYear))
                {
                    years.Add(currentYear);
                }
                
                string currentYearNext = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}";
                if (!years.Contains(currentYearNext))
                {
                    years.Add(currentYearNext);
                }

                cmbYearFilter.ItemsSource = years.OrderByDescending(y => y).ToList();
                cmbYearFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải năm học: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshAll()
        {
            RefreshPendingList();
            RefreshApprovedList();
            RefreshStats();
        }

        private void RefreshPendingList()
        {
            try
            {
                var pending = _awardService.GetPendingAwards()
                    .Select(a => new PendingAwardItem(a))
                    .ToList();

                dgPending.ItemsSource = pending;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách chờ duyệt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshApprovedList()
        {
            try
            {
                string year = cmbYearFilter.SelectedValue as string ?? string.Empty;
                var approved = _awardService.GetApprovedAwards(year)
                    .Select(a => new ApprovedAwardItem(a))
                    .ToList();

                dgApproved.ItemsSource = approved;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách đã duyệt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshStats()
        {
            try
            {
                string year = cmbYearFilter.SelectedValue as string ?? string.Empty;
                var stats = _awardService.GetAwardStatistics(year);

                txtStatTotal.Text = stats.TotalAwards.ToString();
                txtStatApproved.Text = stats.ApprovedAwards.ToString();
                txtStatPending.Text = stats.PendingAwards.ToString();
                txtStatRejected.Text = stats.RejectedAwards.ToString();

                var breakdownList = stats.AwardsByType
                    .Select(kv => new StatBreakdownItem { TypeName = MapAwardTypeDesc(kv.Key), Count = kv.Value })
                    .ToList();
                lstStatsByType.ItemsSource = breakdownList;

                // Load all history log
                var allRecords = _db.AwardRecords
                    .Where(a => string.IsNullOrEmpty(year) || a.SchoolYear == year)
                    .OrderByDescending(a => a.ProposedDate)
                    .ToList()
                    .Select(a => new AllAwardItem(a))
                    .ToList();
                dgAllAwards.ItemsSource = allRecords;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải thống kê: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefreshPending_Click(object sender, RoutedEventArgs e)
        {
            RefreshPendingList();
        }

        private void BtnRefreshApproved_Click(object sender, RoutedEventArgs e)
        {
            RefreshApprovedList();
        }

        private void CmbYearFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshApprovedList();
            RefreshStats();
        }

        // Approve Proposal
        private void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!QASmartClass.Staff.Services.StaffSession.IsLoggedIn || !QASmartClass.Staff.Services.StaffSession.CanApprove())
                {
                    MessageBox.Show("Bạn không có quyền thực hiện hành động này hoặc phiên đăng nhập đã hết hạn.", "Cảnh báo phân quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (sender is Button btn && btn.DataContext is PendingAwardItem item)
                {
                    string approver = QASmartClass.Staff.Services.StaffSession.DisplayName;
                    bool success = _awardService.ApproveAward(item.Id, approver);
                    if (success)
                    {
                        RefreshAll();
                        MessageBox.Show($"Đã phê duyệt đề xuất khen thưởng cho {item.TargetName}!", "Phê duyệt thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Phê duyệt thất bại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (UnauthorizedRoleException ex)
            {
                MessageBox.Show(ex.Message, "Lỗi phân quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Reject Flow
        private void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PendingAwardItem item)
            {
                _pendingRejectRecordId = item.Id;
                txtRejectReason.Text = string.Empty;
                popRejectReason.IsOpen = true;
            }
        }

        private void BtnCancelReject_Click(object sender, RoutedEventArgs e)
        {
            popRejectReason.IsOpen = false;
        }

        private void BtnConfirmReject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!QASmartClass.Staff.Services.StaffSession.IsLoggedIn || !QASmartClass.Staff.Services.StaffSession.CanApprove())
                {
                    MessageBox.Show("Bạn không có quyền thực hiện hành động này hoặc phiên đăng nhập đã hết hạn.", "Cảnh báo phân quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string reason = txtRejectReason.Text.Trim();
                if (string.IsNullOrEmpty(reason))
                {
                    MessageBox.Show("Vui lòng nhập lý do từ chối để tiếp tục.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string approver = QASmartClass.Staff.Services.StaffSession.DisplayName;
                bool success = _awardService.RejectAward(_pendingRejectRecordId, approver, reason);
                if (success)
                {
                    popRejectReason.IsOpen = false;
                    RefreshAll();
                    MessageBox.Show("Đã từ chối đề xuất khen thưởng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Từ chối đề xuất thất bại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (UnauthorizedRoleException ex)
            {
                MessageBox.Show(ex.Message, "Lỗi phân quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Print PDF Certificates
        private void BtnPrintSingle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.DataContext is ApprovedAwardItem item)
                {
                    string path = _awardService.PrintCertificate(item.Id);
                    if (!string.IsNullOrEmpty(path))
                    {
                        RefreshAll();
                        MessageBox.Show($"Đã in giấy khen thành công và lưu tại:\n{path}", "In thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Open printed PDF
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
                    }
                    else
                    {
                        MessageBox.Show("In giấy khen thất bại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPrintBatch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var approvedList = dgApproved.ItemsSource as List<ApprovedAwardItem>;
                if (approvedList == null) return;

                var selectedItems = approvedList.Where(i => i.IsSelected).ToList();
                if (selectedItems.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất một giấy khen để in hàng loạt.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int successCount = 0;
                string firstPrintedPath = string.Empty;

                foreach (var item in selectedItems)
                {
                    string path = _awardService.PrintCertificate(item.Id);
                    if (!string.IsNullOrEmpty(path))
                    {
                        successCount++;
                        if (string.IsNullOrEmpty(firstPrintedPath))
                        {
                            firstPrintedPath = path;
                        }
                    }
                }

                RefreshAll();

                if (successCount > 0)
                {
                    MessageBox.Show($"Đã in thành công {successCount}/{selectedItems.Count} giấy khen!", "Hoàn thành in hàng loạt", MessageBoxButton.OK, MessageBoxImage.Information);
                    if (!string.IsNullOrEmpty(firstPrintedPath))
                    {
                        // Open the first printed PDF
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(firstPrintedPath) { UseShellExecute = true });
                    }
                }
                else
                {
                    MessageBox.Show("In hàng loạt thất bại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi khi in hàng loạt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Translation helper
        public static string MapTargetTypeDesc(string targetType)
        {
            return targetType switch
            {
                "Student" => "Học sinh",
                "Teacher" => "Giáo viên",
                "Class" => "Tập thể lớp",
                _ => targetType
            };
        }

        public static string MapAwardTypeDesc(string awardType)
        {
            return awardType switch
            {
                "HSG" => "Học sinh giỏi",
                "HSTT" => "Học sinh tiên tiến",
                "GVDG" => "Giáo viên dạy giỏi",
                "LopTienTien" => "Lớp tiên tiến",
                "GiayKhen" => "Giấy khen",
                "BangKhen" => "Bằng khen",
                _ => awardType
            };
        }
    }

    public class PendingAwardItem
    {
        public int Id { get; set; }
        public string TargetType { get; set; }
        public string TargetTypeDesc => AwardManagementView.MapTargetTypeDesc(TargetType);
        public string TargetName { get; set; }
        public string AwardType { get; set; }
        public string Semester { get; set; }
        public string SchoolYear { get; set; }
        public string ProposedBy { get; set; }
        public DateTime ProposedDate { get; set; }

        public PendingAwardItem(AwardRecord record)
        {
            Id = record.Id;
            TargetType = record.TargetType;
            TargetName = record.TargetName;
            AwardType = record.AwardType;
            Semester = record.Semester;
            SchoolYear = record.SchoolYear;
            ProposedBy = record.ProposedBy;
            ProposedDate = record.ProposedDate;
        }
    }

    public class ApprovedAwardItem
    {
        public bool IsSelected { get; set; }
        public int Id { get; set; }
        public string TargetType { get; set; }
        public string TargetTypeDesc => AwardManagementView.MapTargetTypeDesc(TargetType);
        public string TargetName { get; set; }
        public string AwardType { get; set; }
        public string Semester { get; set; }
        public string SchoolYear { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public ApprovedAwardItem(AwardRecord record)
        {
            IsSelected = false;
            Id = record.Id;
            TargetType = record.TargetType;
            TargetName = record.TargetName;
            AwardType = record.AwardType;
            Semester = record.Semester;
            SchoolYear = record.SchoolYear;
            ApprovedBy = record.ApprovedBy;
            ApprovedDate = record.ApprovedDate;
        }
    }

    public class AllAwardItem
    {
        public string TargetName { get; set; }
        public string AwardType { get; set; }
        public string Semester { get; set; }
        public string SchoolYear { get; set; }
        public string Status { get; set; }
        public string StatusDesc => Status switch
        {
            "Proposed" => "Chờ duyệt",
            "Approved" => "Đã duyệt",
            "Rejected" => "Đã từ chối",
            "Printed" => "Đã in giấy khen",
            _ => Status
        };
        public string Notes { get; set; }

        public AllAwardItem(AwardRecord record)
        {
            TargetName = record.TargetName;
            AwardType = record.AwardType;
            Semester = record.Semester;
            SchoolYear = record.SchoolYear;
            Status = record.Status;
            Notes = record.Notes;
        }
    }

    public class StatBreakdownItem
    {
        public string TypeName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}

