using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;

namespace QASmartClass.Leadership.Views
{
    public partial class SystemDataExporterView : Page
    {
        private AppDbContext _db;

        public SystemDataExporterView()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private bool CheckPermission()
        {
            if (QASmartClass.Services.UserSessionService.Instance.IsLoggedIn)
            {
                return QASmartClass.Services.UserSessionService.Instance.IsManager;
            }
            if (QASmartClass.Staff.Services.StaffSession.IsLoggedIn)
            {
                var role = QASmartClass.Staff.Services.StaffSession.Role;
                return role == "HieuTruong" || role == "Admin";
            }
            return false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            if (!CheckPermission())
            {
                MessageBox.Show("Bạn không có quyền truy cập chức năng xuất dữ liệu hệ thống.", "Từ chối truy cập", MessageBoxButton.OK, MessageBoxImage.Stop);
                GridExporterContent.Visibility = Visibility.Collapsed;
                return;
            }
            else
            {
                GridExporterContent.Visibility = Visibility.Visible;
            }

            _db = QASmartClass.Services.AppServices.Database ?? QASmartClass.Services.AppServices.CreateDb();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db = null;
        }

        private void BtnExportStudents_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckPermission()) return;
            string defaultName = "Students_Export_" + DateTime.Now.ToString("yyyyMMdd");
            string filePath = QASmartClass.Services.ExcelDataService.GetSaveFilePath(defaultName);
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                if (_db != null)
                {
                    var students = _db.Students.Select(s => new {
                        Mã_Học_Sinh = s.StudentCode,
                        Họ_Tên = s.FullName,
                        Lớp = s.ClassName,
                        Trạng_Thái = s.IsOnline ? "Online" : "Offline"
                    }).ToList();

                    bool ok = QASmartClass.Services.ExcelDataService.ExportToExcel(filePath, students, "Học Sinh");
                    if (ok) MessageBox.Show($"Đã xuất thành công!\nFile: {filePath}", "Xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportStaff_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckPermission()) return;
            string defaultName = "Staff_KPI_Export_" + DateTime.Now.ToString("yyyyMMdd");
            string filePath = QASmartClass.Services.ExcelDataService.GetSaveFilePath(defaultName);
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                if (_db != null)
                {
                    var tasks = _db.DailyTasks.ToList();
                    var staffGroups = tasks.GroupBy(t => t.AssignedTo).ToList();
                    
                    var exportData = staffGroups.Select(group => {
                        int total = group.Count();
                        int done = group.Count(t => t.Status == "Done");
                        int overdue = group.Count(t => t.Status != "Done" && t.DueDate < DateTime.Now.Date);
                        double rate = total > 0 ? (double)done / total * 100 : 0;
                        return new {
                            Nhân_viên = group.Key,
                            Phòng_ban = group.FirstOrDefault()?.Department ?? "Chưa rõ",
                            Tổng_công_việc = total,
                            Hoàn_thành = done,
                            Quá_hạn = overdue,
                            Tỉ_lệ_hoàn_thành_PT = Math.Round(rate, 1)
                        };
                    }).ToList();

                    bool ok = QASmartClass.Services.ExcelDataService.ExportToExcel(filePath, exportData, "KPI Nhân Viên");
                    if (ok) MessageBox.Show($"Đã xuất thành công!\nFile: {filePath}", "Xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExportSystem_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckPermission()) return;
            string defaultName = "System_Logs_Export_" + DateTime.Now.ToString("yyyyMMdd");
            string filePath = QASmartClass.Services.ExcelDataService.GetSaveFilePath(defaultName);
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                if (_db != null)
                {
                    var logs = _db.AuditLogs.OrderByDescending(a => a.Timestamp).Take(200)
                        .Select(log => new {
                            Mã_Log = log.Id,
                            Thời_gian = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                            Hành_động = log.Action,
                            Người_thực_hiện = log.ActorName,
                            Chi_tiết = log.Details
                        }).ToList();

                    bool ok = QASmartClass.Services.ExcelDataService.ExportToExcel(filePath, logs, "Nhật Ký");
                    if (ok) MessageBox.Show($"Đã xuất thành công!\nFile: {filePath}", "Xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

