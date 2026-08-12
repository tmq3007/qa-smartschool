using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace QASmartClass.Staff.ViewModels
{
    public class SecurityLogDisplay
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string PersonInvolved { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FriendlyEventType => EventType switch
        {
            "CheckIn" => "Khách vào cổng",
            "CheckOut" => "Khách ra cổng",
            "KeyExchange" => "Bàn giao chìa khóa",
            "Incident" => "Sự cố an ninh",
            _ => "Khác"
        };
    }

    public partial class SecurityKioskViewModel : ObservableObject, IDisposable
    {
        private System.Threading.CancellationTokenSource? _debounceCts;

        [ObservableProperty] private ObservableCollection<SecurityLogDisplay> _logs = new();
        [ObservableProperty] private bool _isProcessing;
        [ObservableProperty] private string _person = string.Empty;
        [ObservableProperty] private string _description = string.Empty;
        [ObservableProperty] private string _guardName = string.Empty;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private string _selectedEventType = "CheckIn"; // default
        [ObservableProperty] private string _qrInputBuffer = string.Empty;

        // Additional properties for Check-In / Check-Out linkage (v4.1 specifications)
        [ObservableProperty] private ObservableCollection<TeacherProfile> _teachers = new();
        [ObservableProperty] private string _selectedHostTeacherId = string.Empty;
        [ObservableProperty] private string _visitPurpose = "Liên hệ công tác";
        [ObservableProperty] private string _badgeNumber = string.Empty;

        [ObservableProperty] private ObservableCollection<SecurityLog> _activeVisitors = new();
        [ObservableProperty] private SecurityLog? _selectedActiveVisitor;

        // Manual entry properties for CCCD and Address fallback
        [ObservableProperty] private string _manualCccd = string.Empty;
        [ObservableProperty] private string _manualAddress = string.Empty;

        // Scanned CCCD temporary fields
        private string _scannedCccd = string.Empty;
        private string _scannedGender = string.Empty;
        private string _scannedAddress = string.Empty;

        public SecurityKioskViewModel()
        {
            GuardName = QASmartClass.Staff.Services.StaffSession.DisplayName;

            // Background load
            _ = LoadTeachersAsync();
            _ = LoadActiveVisitorsAsync();
        }

        public async Task InitializeAsync()
        {
            await LoadTodayLogsAsync();
            await LoadTeachersAsync();
            await LoadActiveVisitorsAsync();
        }

        private async Task LoadTeachersAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var list = await db.TeacherProfiles
                    .OrderBy(t => t.DisplayName)
                    .ToListAsync();
                Teachers = new ObservableCollection<TeacherProfile>(list);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[SecurityKiosk] LoadTeachers failed");
            }
        }

        private async Task LoadActiveVisitorsAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var active = await db.SecurityLogs
                    .Where(l => l.EventType == "CheckIn" && !l.IsCheckedOut)
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();
                ActiveVisitors = new ObservableCollection<SecurityLog>(active);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[SecurityKiosk] LoadActiveVisitors failed");
            }
        }

        partial void OnSelectedEventTypeChanged(string value)
        {
            // Reset fields on event type change
            Person = string.Empty;
            Description = string.Empty;
            BadgeNumber = string.Empty;
            VisitPurpose = "Liên hệ công tác";
            SelectedHostTeacherId = string.Empty;
            SelectedActiveVisitor = null;
            _scannedCccd = string.Empty;
            _scannedGender = string.Empty;
            _scannedAddress = string.Empty;
            ManualCccd = string.Empty;
            ManualAddress = string.Empty;
        }

        partial void OnSelectedActiveVisitorChanged(SecurityLog? value)
        {
            if (value != null)
            {
                Person = value.VisitorName;
                BadgeNumber = value.BadgeNumber;

                string cccd = value.CccdNumber;
                string address = value.Address;

                using var db = new AppDbContext();
                var encryptionSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_Visitor_Encryption")?.Value ?? "True";
                if (encryptionSetting.Equals("True", System.StringComparison.OrdinalIgnoreCase))
                {
                    const string piiKey = "QA_SecurityKiosk_PII_Key_2026";
                    cccd = QASmartClass.Utilities.CryptoHelper.Decrypt(cccd, piiKey);
                    address = QASmartClass.Utilities.CryptoHelper.Decrypt(address, piiKey);
                }

                string maskedCccd = MaskCccd(cccd);
                string maskedAddress = MaskAddress(address);
                Description = $"Khách ra cổng: {value.VisitorName} - CCCD: {maskedCccd} - Thẻ số: {value.BadgeNumber} - Vào lúc: {value.Timestamp:HH:mm} - Người gặp: {value.HostTeacherId} - Mục đích: {value.VisitPurpose} - Địa chỉ: {maskedAddress}";
            }
        }

        partial void OnQrInputBufferChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !value.Contains("|")) return;

            bool isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
            if (isTestHost)
            {
                ProcessQrCode(value);
                return;
            }

            // Debounce processing: wait 150ms after the last character is received
            _debounceCts?.Cancel();
            _debounceCts = new System.Threading.CancellationTokenSource();
            var token = _debounceCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(150, token);
                    if (token.IsCancellationRequested) return;

                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        ProcessQrCode(value);
                    });
                }
                catch (TaskCanceledException) { }
            }, token);
        }

        private void ProcessQrCode(string value)
        {
            var cleanValue = value.Trim('\r', '\n');
            var parts = cleanValue.Split('|');
            if (parts.Length >= 5)
            {
                string cccd = parts[0].Trim();
                string name = parts.Length > 2 ? parts[2].Trim() : string.Empty;
                string gender = parts.Length > 4 ? parts[4].Trim() : string.Empty;
                string address = parts.Length > 5 ? parts[5].Trim() : string.Empty;
                if (!string.IsNullOrEmpty(name))
                {
                    Person = name;
                    _scannedCccd = cccd;
                    _scannedGender = gender;
                    _scannedAddress = address;

                    // Sync to manual entry textboxes for guard validation
                    ManualCccd = cccd;
                    ManualAddress = address;

                    Description = $"Khách: {name} - CCCD: {cccd} - Giới tính: {gender} - Địa chỉ: {address}";
                    QrInputBuffer = string.Empty;
                    _ = AppServices.UIService.ShowInfoAsync("✓ Đã tự động điền thông tin từ thẻ CCCD!", "Thành công");
                }
            }
        }

        private string MaskCccd(string cccd)
        {
            if (string.IsNullOrEmpty(cccd) || cccd.Length < 6) return cccd;
            return cccd.Substring(0, 3) + "******" + cccd.Substring(cccd.Length - 3);
        }

        private string MaskAddress(string address)
        {
            if (string.IsNullOrEmpty(address)) return address;
            return "[Đã mã hóa bảo mật]";
        }

        [RelayCommand]
        private async Task LoadTodayLogsAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var securityService = new SecurityService(db);
                var data = await securityService.GetLogsByDateAsync(DateTime.Today);
                var list = data.Select(l => new SecurityLogDisplay
                {
                    Timestamp = l.Timestamp,
                    EventType = l.EventType,
                    PersonInvolved = l.PersonInvolved,
                    Description = l.Description
                }).ToList();
                Logs = new ObservableCollection<SecurityLogDisplay>(list);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[SecurityKiosk] Load failed");
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (IsProcessing) return;
            IsProcessing = true;
            try
            {
                using var db = new AppDbContext();
                var securityService = new SecurityService(db);

                if (SelectedEventType == "CheckOut")
                {
                    if (SelectedActiveVisitor == null)
                    {
                        System.Media.SystemSounds.Hand.Play();
                        await AppServices.UIService.ShowInfoAsync("Vui lòng chọn khách cần Check-Out.", "Lỗi");
                        return;
                    }
                    
                    // Mark the Check-In log as CheckedOut
                    var checkInLog = await db.SecurityLogs.FindAsync(SelectedActiveVisitor.Id);
                    if (checkInLog != null)
                    {
                        checkInLog.IsCheckedOut = true;
                    }
                    
                    // Create Check-Out event log
                    var log = new SecurityLog
                    {
                        EventType = "CheckOut",
                        PersonInvolved = SelectedActiveVisitor.VisitorName,
                        VisitorName = SelectedActiveVisitor.VisitorName,
                        CccdNumber = SelectedActiveVisitor.CccdNumber,
                        CccdHash = SelectedActiveVisitor.CccdHash,
                        Gender = SelectedActiveVisitor.Gender,
                        Address = SelectedActiveVisitor.Address,
                        HostTeacherId = SelectedActiveVisitor.HostTeacherId,
                        VisitPurpose = SelectedActiveVisitor.VisitPurpose,
                        BadgeNumber = SelectedActiveVisitor.BadgeNumber,
                        IsCheckedOut = true,
                        Description = Description,
                        GuardName = GuardName
                    };
                    
                    await securityService.AddLogAsync(log);
                    
                    // Audit log
                    string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "BaoVe";
                    QASmartClass.Services.AuditHelper.Log(db, "Add_SecurityLog_CheckOut", actor, $"Check-Out visitor: Name={log.VisitorName}, Badge={log.BadgeNumber}");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(Person) || string.IsNullOrWhiteSpace(Description))
                    {
                        System.Media.SystemSounds.Hand.Play();
                        await AppServices.UIService.ShowInfoAsync("Vui lòng nhập người liên quan và chi tiết sự kiện.", "Lỗi");
                        return;
                    }

                    if (Description.Trim().Length < 5)
                    {
                        System.Media.SystemSounds.Hand.Play();
                        await AppServices.UIService.ShowInfoAsync("Chi tiết sự kiện phải dài ít nhất 5 ký tự.", "Lỗi");
                        return;
                    }

                    if (SelectedEventType == "CheckIn")
                    {
                        if (string.IsNullOrWhiteSpace(BadgeNumber))
                        {
                            System.Media.SystemSounds.Hand.Play();
                            await AppServices.UIService.ShowInfoAsync("Vui lòng nhập Số thẻ khách cấp để quản lý ra vào.", "Lỗi");
                            return;
                        }
                        if (string.IsNullOrWhiteSpace(SelectedHostTeacherId))
                        {
                            System.Media.SystemSounds.Hand.Play();
                            await AppServices.UIService.ShowInfoAsync("Vui lòng chọn Giáo viên/Cán bộ tiếp đón trong trường.", "Lỗi");
                            return;
                        }
                    }
                    
                    var log = new SecurityLog
                    {
                        EventType = SelectedEventType,
                        PersonInvolved = Person,
                        Description = Description,
                        GuardName = GuardName
                    };

                    if (SelectedEventType == "CheckIn")
                    {
                        log.VisitorName = Person;
                        log.VisitPurpose = VisitPurpose;
                        log.HostTeacherId = SelectedHostTeacherId;
                        log.BadgeNumber = BadgeNumber;
                        log.IsCheckedOut = false;

                        // Encryption
                        string rawCccd = string.IsNullOrEmpty(_scannedCccd) ? ManualCccd : _scannedCccd;
                        string rawAddress = string.IsNullOrEmpty(_scannedAddress) ? ManualAddress : _scannedAddress;
                        
                        var encryptionSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_Visitor_Encryption")?.Value ?? "True";
                        if (encryptionSetting.Equals("True", System.StringComparison.OrdinalIgnoreCase) && (!string.IsNullOrEmpty(rawCccd) || !string.IsNullOrEmpty(rawAddress)))
                        {
                            const string piiKey = "QA_SecurityKiosk_PII_Key_2026";
                            log.CccdNumber = string.IsNullOrEmpty(rawCccd) ? "" : QASmartClass.Utilities.CryptoHelper.Encrypt(rawCccd, piiKey);
                            log.Address = string.IsNullOrEmpty(rawAddress) ? "" : QASmartClass.Utilities.CryptoHelper.Encrypt(rawAddress, piiKey);
                        }
                        else
                        {
                            log.CccdNumber = rawCccd;
                            log.Address = rawAddress;
                        }

                        // Hash for search index
                        if (!string.IsNullOrEmpty(rawCccd))
                        {
                            log.CccdHash = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(rawCccd);
                        }

                        string displayGender = string.IsNullOrEmpty(_scannedGender) ? "Chưa xác định" : _scannedGender;
                        log.Description = $"Khách: {Person} - CCCD: {MaskCccd(rawCccd)} - Giới tính: {displayGender} - Địa chỉ: {MaskAddress(rawAddress)} - Mục đích: {VisitPurpose} - Tiếp đón: {SelectedHostTeacherId} - Thẻ: {BadgeNumber}";
                    }

                    await securityService.AddLogAsync(log);

                    // Audit log
                    string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "BaoVe";
                    QASmartClass.Services.AuditHelper.Log(db, "Add_SecurityLog", actor, $"Added security log: Event={SelectedEventType}, Person={Person}, Description={log.Description}");
                }

                // Success beep sound
                System.Media.SystemSounds.Asterisk.Play();

                // Reset
                Person = string.Empty;
                Description = string.Empty;
                VisitPurpose = "Liên hệ công tác";
                SelectedHostTeacherId = string.Empty;
                BadgeNumber = string.Empty;
                SelectedActiveVisitor = null;
                _scannedCccd = string.Empty;
                _scannedGender = string.Empty;
                _scannedAddress = string.Empty;
                ManualCccd = string.Empty;
                ManualAddress = string.Empty;

                await LoadTodayLogsAsync();
                await LoadActiveVisitorsAsync();
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Hand.Play();
                Serilog.Log.Error(ex, "[SecurityKiosk] Save failed");
                await AppServices.UIService.ShowInfoAsync("Có lỗi xảy ra khi lưu sự kiện.", "Lỗi");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var securityService = new SecurityService(db);
                var data = await securityService.SearchLogsAsync(SearchText);
                var list = data.Select(l => new SecurityLogDisplay
                {
                    Timestamp = l.Timestamp,
                    EventType = l.EventType,
                    PersonInvolved = l.PersonInvolved,
                    Description = l.Description
                }).ToList();
                Logs = new ObservableCollection<SecurityLogDisplay>(list);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[SecurityKiosk] Search failed");
            }
        }

        public void Dispose()
        {
            _debounceCts?.Dispose();
        }
    }
}
