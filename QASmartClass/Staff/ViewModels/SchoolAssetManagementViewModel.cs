using CommunityToolkit.Mvvm.ComponentModel;
using QASmartClass.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace QASmartClass.Staff.ViewModels
{
    public class AssetDisplayModel
    {
        public int Id { get; set; }
        public string AssetCode { get; set; }
        public string AssetName { get; set; }
        public string Category { get; set; }
        public string AssignedRoom { get; set; }
        public string StatusText { get; set; }
        public string StatusBg { get; set; }
        public string StatusFg { get; set; }

        public DateTime NextMaintenanceDate { get; set; }
        public int RepairCount { get; set; }
        public bool IsMaintenanceOverdue => (StatusText == "Active" || StatusText == "NeedsRepair") && NextMaintenanceDate < DateTime.Today;
        public string MaintenanceStatusText => StatusText == "Broken" ? "Không áp dụng" : (IsMaintenanceOverdue ? "Quá hạn bảo trì!" : $"Hạn: {NextMaintenanceDate:dd/MM/yyyy}");
        public string MaintenanceStatusColor => StatusText == "Broken" ? "#9CA3AF" : (IsMaintenanceOverdue ? "#DC2626" : "#4B5563");

        public string FriendlyStatus => StatusText switch
        {
            "Active" => "Đang hoạt động",
            "NeedsRepair" => "Cần sửa chữa",
            "Broken" => "Đã hỏng / Chờ thanh lý",
            _ => StatusText
        };
    }

    public partial class SchoolAssetManagementViewModel : ObservableObject, IDisposable
    {
        private readonly AppDbContext _db;

        [ObservableProperty] private ObservableCollection<AssetDisplayModel> _assets = new();
        [ObservableProperty] private string _selectedStatus = "Tất cả";
        [ObservableProperty] private string _newAssetName = string.Empty;
        [ObservableProperty] private string _newAssetCategory = "Smartboard";
        [ObservableProperty] private string _newAssetLocation = string.Empty;

        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _isProcessing;

        [ObservableProperty] private int _bookingAssetId;
        [ObservableProperty] private DateTime? _bookingDate = DateTime.Today;
        [ObservableProperty] private string _bookingTimeSlot = "1";
        [ObservableProperty] private string _bookedBy = string.Empty;
        [ObservableProperty] private string _bookedByTeacherName = string.Empty;
        [ObservableProperty] private ObservableCollection<SchoolAssetBooking> _bookings = new();
        [ObservableProperty] private ObservableCollection<AssetDisplayModel> _bookableAssets = new();

        [ObservableProperty] private bool _isRecurring;
        [ObservableProperty] private string _recurringWeeks = "1";

        public SchoolAssetManagementViewModel()
        {
            _db = new AppDbContext();
        }

        public async Task InitializeAsync()
        {
            await LoadDataAsync();
            await LoadBookingsAsync();
        }

        partial void OnSelectedStatusChanged(string value)
        {
            _ = LoadDataAsync();
        }

        partial void OnSearchTextChanged(string value)
        {
            _ = LoadDataAsync();
        }

        partial void OnBookedByChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                BookedByTeacherName = string.Empty;
                return;
            }
            _ = Task.Run(async () =>
            {
                try
                {
                    using var db = new AppDbContext();
                    var teacher = await db.TeacherProfiles.FirstOrDefaultAsync(t => t.TeacherCode == value.Trim());
                    string status = teacher != null ? $"✔ {teacher.FullName}" : "❌ Không tìm thấy giáo viên";
                    if (System.Windows.Application.Current != null)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            BookedByTeacherName = status;
                        });
                    }
                    else
                    {
                        BookedByTeacherName = status;
                    }
                }
                catch
                {
                    // Safe fallback in non-WPF test contexts
                    BookedByTeacherName = string.Empty;
                }
            });
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            _db.ChangeTracker.Clear();
            try
            {
                var query = _db.SchoolAssets.AsQueryable();

                if (SelectedStatus != "Tất cả")
                {
                    query = query.Where(a => a.Status == SelectedStatus);
                }

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var kw = SearchText.ToLower().Trim();
                    query = query.Where(a => a.AssetType.ToLower().Contains(kw) || a.Location.ToLower().Contains(kw) || a.AssetCode.ToLower().Contains(kw));
                }

                var rawList = await query.OrderByDescending(a => a.PurchaseDate).ToListAsync();
                var list = rawList.Select(a =>
                {
                    string category = a.AssetType;
                    string name = a.AssetType;
                    int idx = a.AssetType.IndexOf(" - ");
                    if (idx >= 0)
                    {
                        category = a.AssetType.Substring(0, idx).Trim();
                        name = a.AssetType.Substring(idx + 3).Trim();
                    }
                    return new AssetDisplayModel
                    {
                        Id = a.Id,
                        AssetCode = a.AssetCode,
                        AssetName = name,
                        Category = category,
                        AssignedRoom = a.Location,
                        StatusText = a.Status,
                        StatusBg = a.Status == "Active" ? "#DCFCE7" : (a.Status == "NeedsRepair" ? "#FEF9C3" : "#FEE2E2"),
                        StatusFg = a.Status == "Active" ? "#16A34A" : (a.Status == "NeedsRepair" ? "#CA8A04" : "#DC2626"),
                        NextMaintenanceDate = a.NextMaintenanceDate,
                        RepairCount = a.RepairCount
                    };
                }).ToList();

                Assets = new ObservableCollection<AssetDisplayModel>(list);
                BookableAssets = new ObservableCollection<AssetDisplayModel>(list.Where(a => a.StatusText == "Active"));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[AssetManager] Load error");
            }
        }

        [RelayCommand]
        private async Task AddAssetAsync()
        {
            if (IsProcessing) return;
            IsProcessing = true;
            try
            {
                bool isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
                if (string.IsNullOrWhiteSpace(NewAssetName) || string.IsNullOrWhiteSpace(NewAssetLocation))
                {
                    await AppServices.UIService.ShowInfoAsync("Vui lòng nhập Tên thiết bị và Vị trí.", "Lỗi");
                    return;
                }

                var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "IT_Asset_MaintenanceIntervalMonths");
                int months = setting != null && int.TryParse(setting.Value, out int m) ? m : 6;

                var asset = new SchoolAsset
                {
                    AssetCode = $"AST-{DateTime.Now:yyMMddHHmmss}-{new Random().Next(1000, 9999)}",
                    AssetType = $"{NewAssetCategory} - {NewAssetName.Trim()}",
                    PurchaseDate = DateTime.Now,
                    NextMaintenanceDate = DateTime.Today.AddMonths(months),
                    Status = "Active",
                    Location = NewAssetLocation.Trim()
                };
                
                _db.SchoolAssets.Add(asset);
                await _db.SaveChangesAsync();
                string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Staff";
                QASmartClass.Services.AuditHelper.Log(_db, "Add_Asset", actor, $"Added asset {asset.AssetCode}");
                
                NewAssetName = string.Empty;
                NewAssetLocation = string.Empty;
                
                await LoadDataAsync();
                await AppServices.UIService.ShowInfoAsync("✅ Đã thêm thiết bị mới thành công!", "Thành công");
            }
            catch (Exception ex)
            {
                bool isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
                await AppServices.UIService.ShowInfoAsync($"Lỗi: {ex.Message}", "Lỗi");
            }
            finally
            {
                IsProcessing = false;
            }
        }
 
        private async Task ProcessBookingsOnAssetFailureAsync(SchoolAsset asset)
        {
            var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Asset_AutoCancelBookingsOnRepair");
            string mode = setting?.Value ?? "1"; // Mặc định "1" - Hủy & Thông báo

            var bookings = await _db.AssetBookings
                .Where(b => b.AssetId == asset.Id && b.BookingDate.Date >= DateTime.Today)
                .ToListAsync();

            if (!bookings.Any()) return;

            if (mode == "0") // Tự động thay thế
            {
                // Lấy loại thiết bị (trước dấu " - ")
                string assetType = asset.AssetType;
                int idx = assetType.IndexOf(" - ");
                string category = idx >= 0 ? assetType.Substring(0, idx).Trim() : assetType;

                // Quét các thiết bị cùng category đang Active
                var alternateAssets = await _db.SchoolAssets
                    .Where(a => a.Id != asset.Id && a.Status == "Active" && a.AssetType.StartsWith(category))
                    .ToListAsync();

                foreach (var booking in bookings)
                {
                    // Tìm thiết bị thay thế trống trong ngày/tiết đó
                    var replacement = alternateAssets.FirstOrDefault(alt => 
                        !_db.AssetBookings.Any(b => b.AssetId == alt.Id && b.BookingDate.Date == booking.BookingDate.Date && b.TimeSlot == booking.TimeSlot)
                    );

                    if (replacement != null)
                    {
                        booking.AssetId = replacement.Id;
                        // Gửi thông báo đổi thiết bị
                        var mobileApi = new MobileApiService(_db);
                        var teacher = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.TeacherCode == booking.BookedBy);
                        int teacherId = teacher?.Id ?? 1;
                        await mobileApi.SendPushNotificationAsync(
                            teacherId,
                            "Teacher",
                            "Thay đổi thiết bị mượn tự động",
                            $"Thiết bị {asset.AssetCode} bị hỏng. Lịch đặt của bạn vào ngày {booking.BookingDate:dd/MM} (Tiết {booking.TimeSlot}) đã được đổi sang thiết bị {replacement.AssetCode} cùng loại.",
                            "General"
                        );
                    }
                    else
                    {
                        // Hủy lịch nếu không có máy thay thế
                        _db.AssetBookings.Remove(booking);
                        // Gửi thông báo hủy lịch
                        var mobileApi = new MobileApiService(_db);
                        var teacher = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.TeacherCode == booking.BookedBy);
                        int teacherId = teacher?.Id ?? 1;
                        await mobileApi.SendPushNotificationAsync(
                            teacherId,
                            "Teacher",
                            "Hủy lịch mượn thiết bị do sự cố",
                            $"Lịch đặt thiết bị {asset.AssetCode} vào ngày {booking.BookingDate:dd/MM} (Tiết {booking.TimeSlot}) đã bị hủy do thiết bị gặp sự cố hỏng hóc và không có thiết bị trống cùng loại thay thế.",
                            "General"
                        );
                    }
                }
            }
            else // Hủy lịch & Thông báo (mode == "1")
            {
                foreach (var booking in bookings)
                {
                    _db.AssetBookings.Remove(booking);
                    var mobileApi = new MobileApiService(_db);
                    var teacher = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.TeacherCode == booking.BookedBy);
                    int teacherId = teacher?.Id ?? 1;
                    await mobileApi.SendPushNotificationAsync(
                        teacherId,
                        "Teacher",
                        "Hủy lịch mượn thiết bị",
                        $"Lịch đặt thiết bị {asset.AssetCode} vào ngày {booking.BookingDate:dd/MM} (Tiết {booking.TimeSlot}) đã bị hủy do thiết bị được báo hỏng để bảo trì.",
                        "General"
                    );
                }
            }
            await _db.SaveChangesAsync();
        }

        [RelayCommand]
        private async Task ReportAsync(int id)
        {
            var asset = await _db.SchoolAssets.FindAsync(id);
            if (asset != null)
            {
                asset.Status = "NeedsRepair";
                asset.NextMaintenanceDate = DateTime.Now;
                await _db.SaveChangesAsync();
                
                await ProcessBookingsOnAssetFailureAsync(asset);
                
                string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Staff";
                QASmartClass.Services.AuditHelper.Log(_db, "Report_Asset", actor, $"Reported asset {asset.AssetCode} needs repair");
                await LoadDataAsync();
            }
        }
 
        [RelayCommand]
        private async Task FixAsync(int id)
        {
            var asset = await _db.SchoolAssets.FindAsync(id);
            if (asset != null)
            {
                var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "IT_Asset_MaintenanceIntervalMonths");
                int months = setting != null && int.TryParse(setting.Value, out int m) ? m : 6;

                asset.Status = "Active";
                asset.NextMaintenanceDate = DateTime.Today.AddMonths(months);
                await _db.SaveChangesAsync();
                string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Staff";
                QASmartClass.Services.AuditHelper.Log(_db, "Fix_Asset", actor, $"Fixed asset {asset.AssetCode}");
                await LoadDataAsync();
            }
        }

        [RelayCommand]
        private async Task MaintenanceAsync(int id)
        {
            var asset = await _db.SchoolAssets.FindAsync(id);
            if (asset != null)
            {
                var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "IT_Asset_MaintenanceIntervalMonths");
                int months = setting != null && int.TryParse(setting.Value, out int m) ? m : 6;

                asset.Status = "Active";
                asset.RepairCount += 1;
                asset.NextMaintenanceDate = DateTime.Today.AddMonths(months);
                await _db.SaveChangesAsync();

                string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Staff";
                QASmartClass.Services.AuditHelper.Log(_db, "Maintenance_Asset", actor, $"Performed scheduled maintenance on asset {asset.AssetCode}");
                await LoadDataAsync();
                await AppServices.UIService.ShowInfoAsync($"✅ Đã hoàn tất bảo trì thiết bị {asset.AssetCode}. Lịch bảo trì tiếp theo: {asset.NextMaintenanceDate:dd/MM/yyyy}.", "Thành công");
            }
        }

        [RelayCommand]
        private async Task DecommissionAssetAsync(int id)
        {
            bool isConfirm = await AppServices.UIService.ShowConfirmAsync(
                "Bạn có chắc chắn muốn thanh lý tài sản này không?\nThao tác này sẽ đánh dấu tài sản là Đã hỏng / Chờ thanh lý và không thể khôi phục trực tiếp.",
                "⚠ Xác nhận thanh lý tài sản", true);

            if (!isConfirm) return;

            var asset = await _db.SchoolAssets.FindAsync(id);
            if (asset != null)
            {
                asset.Status = "Broken";
                await _db.SaveChangesAsync();
                
                await ProcessBookingsOnAssetFailureAsync(asset);
                
                string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Staff";
                QASmartClass.Services.AuditHelper.Log(_db, "Decommission_Asset", actor, $"Decommissioned asset: {asset.AssetCode} ({asset.AssetType})");
                await LoadDataAsync();
            }
        }

        [RelayCommand]
        public async Task LoadBookingsAsync()
        {
            _db.ChangeTracker.Clear();
            try
            {
                var list = await _db.AssetBookings.OrderByDescending(b => b.BookingDate).ToListAsync();
                Bookings = new ObservableCollection<SchoolAssetBooking>(list);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[AssetManager] Load bookings error");
            }
        }

        [RelayCommand]
        public async Task SaveBookingAsync()
        {
            if (BookingAssetId <= 0)
            {
                await AppServices.UIService.ShowInfoAsync("Vui lòng chọn thiết bị để đăng ký.", "Lỗi");
                return;
            }

            if (!BookingDate.HasValue)
            {
                await AppServices.UIService.ShowInfoAsync("Vui lòng chọn ngày mượn.", "Lỗi");
                return;
            }

            if (BookingDate.Value.Date < DateTime.Today)
            {
                await AppServices.UIService.ShowInfoAsync("Ngày đăng ký mượn không được ở trong quá khứ.", "Lỗi");
                return;
            }

            int timeSlot = int.TryParse(BookingTimeSlot, out int ts) ? ts : 1;
            if (timeSlot < 1 || timeSlot > 10)
            {
                await AppServices.UIService.ShowInfoAsync("Tiết học phải từ 1 đến 10.", "Lỗi");
                return;
            }

            // Kiểm tra sự tồn tại của giáo viên trong database
            string checkBookedBy = string.IsNullOrWhiteSpace(BookedBy) ? (QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Staff") : BookedBy.Trim();
            if (checkBookedBy != "Staff" && checkBookedBy != "ADMIN")
            {
                var teacherExists = await _db.TeacherProfiles.AnyAsync(t => t.TeacherCode == checkBookedBy);
                if (!teacherExists)
                {
                    await AppServices.UIService.ShowInfoAsync($"Mã giáo viên '{checkBookedBy}' không tồn tại trong hệ thống.", "Lỗi");
                    return;
                }
            }

            try
            {
                int inputWeeks = int.TryParse(RecurringWeeks, out int rw) ? rw : 1;
                int totalWeeks = IsRecurring ? Math.Clamp(inputWeeks, 1, 10) : 1;
                var skippedDates = new System.Collections.Generic.List<string>();
                int successCount = 0;

                for (int i = 0; i < totalWeeks; i++)
                {
                    DateTime currentDate = BookingDate.Value.AddDays(i * 7);

                    // 1. Kiểm tra ngày nghỉ cuối tuần / lễ cứng
                    bool isWeekend = currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday;
                    bool isHoliday = false;
                    string holidayName = string.Empty;
                    string mmdd = currentDate.ToString("MM-dd");
                    if (mmdd == "01-01") { isHoliday = true; holidayName = "Tết Dương Lịch"; }
                    else if (mmdd == "04-30") { isHoliday = true; holidayName = "Ngày Giải phóng Miền Nam"; }
                    else if (mmdd == "05-01") { isHoliday = true; holidayName = "Ngày Quốc tế Lao động"; }
                    else if (mmdd == "09-02") { isHoliday = true; holidayName = "Ngày Quốc khánh"; }

                    // 2. Kiểm tra ngày lễ động từ SchoolEvents
                    var holidayEvent = await _db.SchoolEvents.FirstOrDefaultAsync(e => 
                        e.StartTime.Date <= currentDate.Date && 
                        e.EndTime.Date >= currentDate.Date && 
                        (e.Title.ToLower().Contains("nghỉ") || 
                         e.Title.ToLower().Contains("lễ") || 
                         e.Title.ToLower().Contains("tết") || 
                         e.Title.ToLower().Contains("holiday")));

                    if (holidayEvent != null)
                    {
                        isHoliday = true;
                        holidayName = holidayEvent.Title;
                    }

                    if (isWeekend || isHoliday)
                    {
                        if (!IsRecurring)
                        {
                            string dateType = isWeekend ? "ngày nghỉ cuối tuần" : $"ngày lễ ({holidayName})";
                            bool confirmDate = await AppServices.UIService.ShowConfirmAsync(
                                $"Ngày đăng ký mượn {currentDate:dd/MM/yyyy} trùng vào {dateType}. Bạn có chắc chắn muốn tiếp tục đăng ký không?",
                                "Cảnh báo mượn tài sản vào ngày nghỉ/lễ",
                                true);
                            if (!confirmDate) return;
                        }
                        else
                        {
                            string reason = isWeekend ? "Ngày nghỉ cuối tuần" : $"Ngày lễ ({holidayName})";
                            skippedDates.Add($"{currentDate:dd/MM/yyyy} - Bỏ qua do: {reason}");
                            continue;
                        }
                    }

                    // 3. Kiểm tra trùng lịch mượn
                    var existing = await _db.AssetBookings.FirstOrDefaultAsync(b => 
                        b.AssetId == BookingAssetId && 
                        b.BookingDate.Date == currentDate.Date && 
                        b.TimeSlot == timeSlot);

                    if (existing != null)
                    {
                        if (!IsRecurring)
                        {
                            string originalBooker = existing.BookedBy;
                            string bookerDisplayName = originalBooker;
                            if (!string.IsNullOrWhiteSpace(originalBooker))
                            {
                                var teacher = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.TeacherCode == originalBooker);
                                if (teacher != null && !string.IsNullOrWhiteSpace(teacher.FullName))
                                {
                                    bookerDisplayName = $"{teacher.FullName} (Mã: {originalBooker})";
                                }
                            }

                            bool confirm = await AppServices.UIService.ShowConfirmAsync(
                                $"Thiết bị này đã được giáo viên {bookerDisplayName} đăng ký mượn trong cùng tiết và ngày học. Bạn có chắc chắn muốn tiếp tục đăng ký mượn đè không?",
                                "Cảnh báo trùng lịch mượn tài sản",
                                true);
                            
                            if (!confirm) return;

                            // Xóa lịch mượn cũ để tránh bị trùng lặp dữ liệu mượn đè
                            _db.AssetBookings.Remove(existing);
                        }
                        else
                        {
                            skippedDates.Add($"{currentDate:dd/MM/yyyy} - Trùng lịch mượn của giáo viên khác");
                            continue;
                        }
                    }

                    // Thực hiện logic lưu vào DB
                    var booking = new SchoolAssetBooking
                    {
                        AssetId = BookingAssetId,
                        BookingDate = currentDate.Date,
                        TimeSlot = timeSlot,
                        BookedBy = checkBookedBy
                    };

                    _db.AssetBookings.Add(booking);
                    successCount++;
                }

                if (successCount > 0)
                {
                    await _db.SaveChangesAsync();

                    string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Staff";
                    QASmartClass.Services.AuditHelper.Log(_db, "Book_Asset", actor, $"Booked asset ID {BookingAssetId} recurring total {successCount} times starting {BookingDate?.ToString("yyyy-MM-dd")}");

                    await LoadBookingsAsync();
                }

                // Reset inputs
                BookingAssetId = 0;
                BookedBy = string.Empty;

                string msg = $"Đăng ký mượn thành công {successCount} ngày học.";
                if (skippedDates.Count > 0)
                {
                    msg += "\nCác ngày sau bị bỏ qua:\n" + string.Join("\n", skippedDates);
                }
                await AppServices.UIService.ShowInfoAsync(msg, "Kết quả đăng ký");
            }
            catch (Exception ex)
            {
                await AppServices.UIService.ShowInfoAsync($"Lỗi khi đăng ký: {ex.Message}", "Lỗi");
            }
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}
