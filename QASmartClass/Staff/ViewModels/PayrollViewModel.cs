using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QASmartClass.Data;
using QASmartClass.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace QASmartClass.Staff.ViewModels
{
    public partial class PayrollViewModel : ObservableObject, IDisposable
    {
        private readonly AppDbContext _db;
        private readonly PayrollService _payrollService;

        [ObservableProperty] private ObservableCollection<PayrollRecord> _payrolls = new();
        [ObservableProperty] private string _staffName = string.Empty;
        [ObservableProperty] private ObservableCollection<string> _staffNames = new();
        [ObservableProperty] private string _selectedMonth = "5";
        [ObservableProperty] private string _calculateYear = "2026";
        [ObservableProperty] private string _filterMonth = "5";
        [ObservableProperty] private string _filterYear = "2026";

        public class ComboboxItem
        {
            public string Content { get; set; } = string.Empty;
            public string Tag { get; set; } = string.Empty;
        }

        public ObservableCollection<ComboboxItem> MonthOptions { get; } = new(
            System.Linq.Enumerable.Range(1, 12).Select(m => new ComboboxItem { Content = $"Tháng {m}", Tag = m.ToString() })
        );

        public ObservableCollection<ComboboxItem> YearOptions { get; } = new(
            System.Linq.Enumerable.Range(DateTime.Now.Year - 3, 8).Select(y => new ComboboxItem { Content = y.ToString(), Tag = y.ToString() })
        );

        public PayrollViewModel()
        {
            _db = new AppDbContext();
            _payrollService = new PayrollService(_db);
        }

        public async Task InitializeAsync()
        {
            await LoadPayrollsAsync();
            await LoadStaffNamesAsync();
        }

        private async Task LoadStaffNamesAsync()
        {
            try
            {
                var names = await _db.StaffProfiles
                    .Select(s => s.FullName)
                    .ToListAsync();
                StaffNames = new ObservableCollection<string>(names);
                if (string.IsNullOrEmpty(StaffName))
                {
                    StaffName = StaffNames.FirstOrDefault() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[Payroll] Load staff names failed");
            }
        }

        [RelayCommand]
        private async Task LoadPayrollsAsync()
        {
            if (int.TryParse(FilterMonth, out int month) && int.TryParse(FilterYear, out int year))
            {
                try
                {
                    var data = await _payrollService.GetPayrollsByMonthAsync(month, year);
                    Payrolls = new ObservableCollection<PayrollRecord>(data);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "[Payroll] Load failed");
                }
            }
        }

        [RelayCommand]
        private async Task CalculateAsync()
        {
            bool isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
            if (!QASmartClass.Staff.Services.StaffSession.CanManagePayroll())
            {
                await AppServices.UIService.ShowInfoAsync("Bạn không có quyền quản lý lương.", "Quyền truy cập");
                return;
            }

            if (string.IsNullOrWhiteSpace(StaffName))
            {
                await AppServices.UIService.ShowInfoAsync("Vui lòng nhập tên nhân sự.", "Lỗi");
                return;
            }

            if (int.TryParse(SelectedMonth, out int month) && int.TryParse(CalculateYear, out int year))
            {
                if (month < 1 || month > 12 || year < 2020 || year > 2030)
                {
                    await AppServices.UIService.ShowInfoAsync("Tháng (1-12) hoặc Năm (2020-2030) không hợp lệ.", "Lỗi");
                    return;
                }
                
                try
                {
                    await _payrollService.CalculatePayrollAsync(StaffName, month, year);
                    
                    // Ghi nhật ký kiểm toán
                    string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Admin";
                    QASmartClass.Services.AuditHelper.Log(_db, "Payroll_Calculated", actor, $"Calculated payroll for {StaffName} - Month {month}/{year}");

                    await AppServices.UIService.ShowInfoAsync("Tính lương thành công!", "Thành công");
                    
                    StaffName = StaffNames.FirstOrDefault() ?? string.Empty;
                    FilterMonth = month.ToString();
                    FilterYear = year.ToString();
                    
                    await LoadPayrollsAsync();
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "[Payroll] Calculate failed");
                    await AppServices.UIService.ShowInfoAsync($"Lỗi khi tính lương: {ex.Message}", "Lỗi");
                }
            }
            else
            {
                await AppServices.UIService.ShowInfoAsync("Tháng hoặc năm không hợp lệ.", "Lỗi");
            }
        }

        [RelayCommand]
        private async Task BatchCalculateAsync()
        {
            bool isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
            if (!QASmartClass.Staff.Services.StaffSession.CanManagePayroll())
            {
                await AppServices.UIService.ShowInfoAsync("Bạn không có quyền quản lý lương.", "Quyền truy cập");
                return;
            }

            if (int.TryParse(SelectedMonth, out int month) && int.TryParse(CalculateYear, out int year))
            {
                if (month < 1 || month > 12 || year < 2020 || year > 2030)
                {
                    await AppServices.UIService.ShowInfoAsync("Tháng (1-12) hoặc Năm (2020-2030) không hợp lệ.", "Lỗi");
                    return;
                }

                bool confirmBatch = await AppServices.UIService.ShowConfirmAsync($"Xác nhận TÍNH LƯƠNG HÀNG LOẠT cho tất cả nhân sự đang hoạt động trong tháng {month}/{year}?", "Xác nhận", false);
                if (!confirmBatch) return;

                try
                {
                    var activeStaff = await _db.StaffProfiles.ToListAsync();
                    int count = 0;
                    foreach (var t in activeStaff)
                    {
                        await _payrollService.CalculatePayrollAsync(t.FullName, month, year);
                        count++;
                    }

                    string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Admin";
                    QASmartClass.Services.AuditHelper.Log(_db, "Payroll_BatchCalculated", actor, $"Batch calculated payroll for {count} staff - Month {month}/{year}");

                    await AppServices.UIService.ShowInfoAsync($"Đã tính lương hàng loạt thành công cho {count} nhân sự!", "Thành công");
                    FilterMonth = month.ToString();
                    FilterYear = year.ToString();
                    await LoadPayrollsAsync();
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "[Payroll] Batch calculate failed");
                    await AppServices.UIService.ShowInfoAsync($"Lỗi tính lương hàng loạt: {ex.Message}", "Lỗi");
                }
            }
            else
            {
                await AppServices.UIService.ShowInfoAsync("Tháng hoặc năm không hợp lệ.", "Lỗi");
            }
        }

        [RelayCommand]
        private async Task FinalizeAsync(PayrollRecord record)
        {
            bool isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
            if (!QASmartClass.Staff.Services.StaffSession.CanManagePayroll())
            {
                await AppServices.UIService.ShowInfoAsync("Bạn không có quyền quản lý lương.", "Quyền truy cập");
                return;
            }

            if (record == null)
            {
                await AppServices.UIService.ShowInfoAsync("Vui lòng chọn một bản ghi chưa chốt (Draft) để chốt lương.", "Lỗi");
                return;
            }

            if (record.Status == "Draft")
            {
                try
                {
                    bool confirmFinal = await AppServices.UIService.ShowConfirmAsync(
                        $"Bạn có chắc chắn muốn CHỐT LƯƠNG tháng {record.Month}/{record.Year} cho {record.StaffName}?\n\n" +
                        $"Lương thực nhận: {record.NetSalary:N0} VNĐ\n\nThao tác này KHÔNG THỂ hoàn tác.",
                        "⚠ Xác nhận chốt lương", true);
                        
                    if (confirmFinal)
                    {
                        if (await _payrollService.FinalizePayrollAsync(record.Id))
                        {
                            // Ghi nhật ký kiểm toán chốt lương
                            string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Admin";
                            QASmartClass.Services.AuditHelper.Log(_db, "Payroll_Finalized", actor, $"Finalized payroll for {record.StaffName} - Month {record.Month}/{record.Year}");

                            await AppServices.UIService.ShowInfoAsync("Chốt lương thành công!", "Thành công");
                            await LoadPayrollsAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "[Payroll] Finalize failed");
                }
            }
            else
            {
                await AppServices.UIService.ShowInfoAsync("Bản ghi này đã được chốt.", "Thông báo");
            }
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}

