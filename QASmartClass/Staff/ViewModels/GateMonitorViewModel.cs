using System;

using System.Collections.ObjectModel;

using System.Linq;

using System.Threading.Tasks;

using System.Windows.Media;

using System.Windows.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using Microsoft.EntityFrameworkCore;

using QASmartClass.Data;
using QASmartClass.Services;



namespace QASmartClass.Staff.ViewModels

{

    public partial class GateMonitorViewModel : ObservableObject, IDisposable

    {

        private static Brush GetBrush(string hex)

        {

            var brush = (Brush)new BrushConverter().ConvertFromString(hex);

            brush?.Freeze();

            return brush ?? Brushes.Transparent;

        }



        [ObservableProperty]

        private ObservableCollection<GateLogDisplay> _gateLogs = new();



        [ObservableProperty]

        private ObservableCollection<Student> _missingStudents = new();



        [ObservableProperty]

        private ObservableCollection<Student> _absentStudents = new();



        [ObservableProperty]

        private int _studentsInSchool;



        [ObservableProperty]

        private int _totalStudents;



        [ObservableProperty]

        private GateLogDisplay? _lastSwipedStudent;



        [ObservableProperty]

        private ObservableCollection<StudentLeaveRequest> _earlyLeaveRequests = new();



        [ObservableProperty]

        private bool _isLoading;



        [ObservableProperty]

        private string _scannedLeavePassCode = string.Empty;



        [ObservableProperty]

        private string _scanFeedbackMessage = string.Empty;



        [ObservableProperty]

        private Brush _scanFeedbackColor = GetBrush("#10B981");



        [ObservableProperty]

        private ObservableCollection<MapNodeDisplay> _mapNodes = new();



        [ObservableProperty]

        private bool _isLockdownActive;



        [ObservableProperty]

        private string _lockdownPin = string.Empty;



        [ObservableProperty]

        private string _lockdownType = "Kẻ xâm nhập";



        [ObservableProperty]

        private string _resolutionNotes = string.Empty;



        [ObservableProperty]

        private string _lockdownStatusText = "Hệ thống bình thường";



        [ObservableProperty]
        private Brush _lockdownStatusColor = GetBrush("#10B981");

        [ObservableProperty]
        private bool _hasStudentInRestrictedArea;

        [ObservableProperty]
        private string _restrictedWarningMessage = string.Empty;

        private DispatcherTimer _refreshTimer;

        private DispatcherTimer? _hardwareTimer;



        [ObservableProperty]

        private bool _isHardwareConnected = true;



        [ObservableProperty]

        private string _missingHeaderTitle = "Đang ở trường";



        [ObservableProperty]

        private string _missingHeaderIcon = "\xE77B"; // Default: user icon



        [ObservableProperty]

        private Brush _missingHeaderBgColor = GetBrush("#DBEAFE"); // Soft blue



        [ObservableProperty]

        private Brush _missingHeaderFgColor = GetBrush("#1D4ED8"); // Dark blue



        [ObservableProperty]

        private string _absentHeaderTitle = "Chưa đến trường";



        [ObservableProperty]

        private Brush _absentHeaderBgColor = GetBrush("#FEF3C7"); // Soft yellow



        [ObservableProperty]

        private Brush _absentHeaderFgColor = GetBrush("#D97706"); // Dark yellow



        public GateMonitorViewModel()

        {

            _refreshTimer = new DispatcherTimer

            {

                Interval = TimeSpan.FromSeconds(10)

            };

            _refreshTimer.Tick += async (s, e) => await RefreshDataAsync();

            

            // Initial load

            _ = RefreshDataAsync();

            _refreshTimer.Start();



            // Hardware Indicator Timer (10s)

            _hardwareTimer = new DispatcherTimer

            {

                Interval = TimeSpan.FromSeconds(10)

            };

            _hardwareTimer.Tick += async (s, e) => await CheckHardwareConnectionAsync();

            _ = CheckHardwareConnectionAsync();

            _hardwareTimer.Start();

        }



        [RelayCommand]

        private async Task RefreshDataAsync()

        {

            try

            {

                IsLoading = true;

                using var db = new AppDbContext();

                

                // Lấy tổng học sinh

                TotalStudents = await db.Students.CountAsync();



                // Kiểm tra trạng thái phong tỏa từ Database

                var activeLockdown = await db.SecurityLockdownLogs

                    .OrderByDescending(l => l.StartedAt)

                    .FirstOrDefaultAsync(l => l.Status == "Active");



                if (activeLockdown != null)

                {

                    IsLockdownActive = true;

                    LockdownStatusText = "ĐANG PHONG TỎA (" + activeLockdown.LockdownType + ")";

                    LockdownStatusColor = GetBrush("#EF4444");

                }

                else

                {

                    IsLockdownActive = false;

                    LockdownStatusText = "Hệ thống bình thường";

                    LockdownStatusColor = GetBrush("#10B981");

                }



                // Lấy logs cổng hôm nay

                var today = DateTime.Today;

                var todayLogs = await db.EventLogs

                    .Where(l => (l.EventType == "GateCheckIn" || l.EventType == "GateCheckOut") && l.Timestamp >= today)

                    .OrderBy(l => l.Timestamp)

                    .ToListAsync();



                var allStudentCodes = todayLogs.Select(l => l.Actor).Distinct().ToList();

                var studentsMap = await db.Students

                    .Where(s => allStudentCodes.Contains(s.StudentCode))

                    .ToDictionaryAsync(s => s.StudentCode, s => s);



                var list = todayLogs.OrderByDescending(l => l.Timestamp).Take(50).Select(log =>

                {

                    string action = log.EventType == "GateCheckIn" ? "Vào trường" : "Ra trường";

                    string device = "Cổng chính";

                    try

                    {

                        using var doc = System.Text.Json.JsonDocument.Parse(log.Details);

                        var root = doc.RootElement;

                        if (root.TryGetProperty("Location", out var loc)) device = loc.GetString() ?? "Cổng chính";

                    }

                    catch { }



                    studentsMap.TryGetValue(log.Actor, out var stu);



                    return new GateLogDisplay

                    {

                        Timestamp = log.Timestamp,

                        EventType = action,

                        Actor = log.Actor,

                        StudentName = stu?.FullName ?? string.Empty,

                        ClassName = stu?.ClassName ?? string.Empty,

                        AvatarPath = stu?.AvatarPath ?? string.Empty,

                        FriendlyDetails = $"{action} tại {device}"

                    };

                }).ToList();



                GateLogs = new ObservableCollection<GateLogDisplay>(list);

                LastSwipedStudent = GateLogs.FirstOrDefault();



                // Gom nhóm theo Actor (StudentCode) và lấy EventType cuối cùng của học sinh đó

                var lastEvents = todayLogs

                    .GroupBy(l => l.Actor)

                    .ToDictionary(g => g.Key, g => g.Last().EventType);



                // Học sinh đang ở trường là học sinh có sự kiện cuối cùng là Check-in

                StudentsInSchool = lastEvents.Values.Count(v => v == "GateCheckIn");



                // Đọc cấu hình Master: Cảnh báo học sinh chưa Checkout

                var warningSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Security_Gate_MissingStudentsActiveWarning");

                string warningMode = warningSetting?.Value ?? "1";



                var missingCodes = lastEvents.Where(kvp => kvp.Value == "GateCheckIn").Select(kvp => kvp.Key).ToList();

                var missing = await db.Students.Where(s => missingCodes.Contains(s.StudentCode)).ToListAsync();



                if (warningMode == "0")

                {

                    // Tắt danh sách / không hiển thị

                    MissingStudents = new ObservableCollection<Student>();

                    MissingHeaderTitle = "Đang ở trường (Tắt)";

                    MissingHeaderIcon = "\xE77B"; // User

                    MissingHeaderBgColor = GetBrush("#DBEAFE"); // Soft blue

                    MissingHeaderFgColor = GetBrush("#1D4ED8"); // Dark blue

                }

                else if (warningMode == "1")

                {

                    // Cảnh báo sau giờ học (sau 17h)

                    if (DateTime.Now.Hour >= 17)

                    {

                        MissingStudents = new ObservableCollection<Student>(missing);

                        MissingHeaderTitle = "Chưa Check-out";

                        MissingHeaderIcon = "\xE7BA"; // Warning icon

                        MissingHeaderBgColor = GetBrush("#FEE2E2"); // Soft red

                        MissingHeaderFgColor = GetBrush("#DC2626"); // Red

                    }

                    else

                    {

                        MissingStudents = new ObservableCollection<Student>(); // Khởi tạo rỗng để tránh lag UI

                        MissingHeaderTitle = "Đang ở trường";

                        MissingHeaderIcon = "\xE77B"; // User

                        MissingHeaderBgColor = GetBrush("#DBEAFE"); // Soft blue

                        MissingHeaderFgColor = GetBrush("#1D4ED8"); // Dark blue

                    }

                }

                else // warningMode == "2"

                {

                    // Luôn hiển thị cảnh báo

                    MissingStudents = new ObservableCollection<Student>(missing);

                    MissingHeaderTitle = "Chưa Check-out";

                    MissingHeaderIcon = "\xE7BA"; // Warning icon

                    MissingHeaderBgColor = GetBrush("#FEE2E2"); // Soft red

                    MissingHeaderFgColor = GetBrush("#DC2626"); // Red

                }



                // Danh sách vắng hôm nay & Nhãn "Chưa đến trường" / "Vắng Không Phép"

                var activeStudents = await db.Students.Where(s => s.Status == "Active").ToListAsync();

                var swipedInCodes = todayLogs

                    .Where(l => l.EventType == "GateCheckIn")

                    .Select(l => l.Actor)

                    .Distinct()

                    .ToList();

                var approvedLeaveIds = await db.StudentLeaveRequests

                    .Where(r => r.LeaveDate.Date == today && r.Status == "Approved")

                    .Select(r => r.StudentId)

                    .ToListAsync();



                var absent = activeStudents

                    .Where(s => !swipedInCodes.Contains(s.StudentCode) && !approvedLeaveIds.Contains(s.Id))

                    .ToList();



                AbsentStudents = new ObservableCollection<Student>(absent);



                if (DateTime.Now.Hour < 8)

                {

                    AbsentHeaderTitle = "Chưa đến trường";

                    AbsentHeaderBgColor = GetBrush("#FEF3C7"); // Soft yellow

                    AbsentHeaderFgColor = GetBrush("#D97706"); // Dark yellow

                }

                else

                {

                    AbsentHeaderTitle = "Vắng Không Phép";

                    AbsentHeaderBgColor = GetBrush("#FEE2E2"); // Soft red

                    AbsentHeaderFgColor = GetBrush("#B91C1C"); // Dark red

                }



                // B6: Tải danh sách học sinh được duyệt nghỉ phép và ĐÃ QUẸT THẺ VÀO TRƯỜNG để đối chiếu việc về sớm

                var swipedInStudentIds = await db.Students

                    .Where(s => swipedInCodes.Contains(s.StudentCode))

                    .Select(s => s.Id)

                    .ToListAsync();



                var earlyLeaves = await db.StudentLeaveRequests

                    .Where(r => r.LeaveDate.Date == today && r.Status == "Approved" && swipedInStudentIds.Contains(r.StudentId))

                    .ToListAsync();

                EarlyLeaveRequests = new ObservableCollection<StudentLeaveRequest>(earlyLeaves);



                // Tải danh sách vị trí Beacons phục vụ Bản đồ nhiệt 2D thời gian thực (5 phút gần nhất)

                var beacons = await db.CampusBeacons.Where(b => b.IsActive).ToListAsync();

                

                var fiveMinutesAgo = DateTime.Now.AddMinutes(-5);

                var recentPings = await db.StudentLocationHistories

                    .Where(h => h.Timestamp >= fiveMinutesAgo)

                    .ToListAsync();



                // B5: Lấy vị trí trạm Beacons mới nhất của từng học sinh đang có mặt ở trường thực tế

                var studentsInSchoolCodes = lastEvents.Where(kvp => kvp.Value == "GateCheckIn").Select(kvp => kvp.Key).ToHashSet();

                var latestLocations = recentPings

                    .Where(p => studentsInSchoolCodes.Contains(p.StudentCode))

                    .GroupBy(p => p.StudentCode)

                    .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.Timestamp).First().NearbyBeaconId);



                var mapNodeList = beacons.Select(b =>

                {

                    int count = latestLocations.Values.Count(beaconId => beaconId == b.Id);

                    Brush color = b.AreaZone.Equals("Restricted", StringComparison.OrdinalIgnoreCase) ? GetBrush("#EF4444") : GetBrush("#10B981");



                    return new MapNodeDisplay
                {
                    BeaconId = b.Id,
                    LocationName = b.LocationName,
                    CoordinateX = b.CoordinateX,
                    CoordinateY = b.CoordinateY,
                    StudentCount = count,
                    StatusColor = color,
                    AreaZone = b.AreaZone
                };

                }).ToList();

                MapNodes = new ObservableCollection<MapNodeDisplay>(mapNodeList);

            // Kiểm tra xem có học sinh trong vùng cấm nguy hiểm không
            var restrictedWithStudents = mapNodeList
                .Where(n => n.AreaZone.Equals("Restricted", StringComparison.OrdinalIgnoreCase) && n.StudentCount > 0)
                .ToList();

            if (restrictedWithStudents.Any())
            {
                HasStudentInRestrictedArea = true;
                string zones = string.Join(", ", restrictedWithStudents.Select(n => n.LocationName));
                RestrictedWarningMessage = "🚨 CẢNH BÁO: Phát hiện học sinh tại vùng cấm: " + zones + "!";
            }
            else
            {
                HasStudentInRestrictedArea = false;
                RestrictedWarningMessage = string.Empty;
            }

            }

            catch (Exception ex)

            {

                Serilog.Log.Error(ex, "[GateMonitor] Refresh failed");

            }

            finally

            {

                IsLoading = false;

            }

        }



        private string ComputeSha256Hash(string rawData)

        {

            using (var sha256Hash = System.Security.Cryptography.SHA256.Create())

            {

                byte[] bytes = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));

                var builder = new System.Text.StringBuilder();

                for (int i = 0; i < bytes.Length; i++)

                {

                    builder.Append(bytes[i].ToString("x2"));

                }

                return builder.ToString();

            }

        }



        private void ShowScanFeedback(string message, string colorHex)

        {

            ScanFeedbackMessage = message;

            ScanFeedbackColor = GetBrush(colorHex);

            

            _ = Task.Run(async () =>

            {

                await Task.Delay(5000);

                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>

                {

                    if (ScanFeedbackMessage == message)

                    {

                        ScanFeedbackMessage = string.Empty;

                    }

                });

            });

        }



        [RelayCommand]

        private async Task ScanLeavePassAsync()

        {

            if (string.IsNullOrWhiteSpace(ScannedLeavePassCode)) return;



            try

            {

                if (!ScannedLeavePassCode.StartsWith("LP-"))

                {

                    System.Media.SystemSounds.Hand.Play();

                    ShowScanFeedback("Mã QR không hợp lệ (Không bắt đầu bằng LP-).", "#EF4444");

                    ScannedLeavePassCode = string.Empty; // Clear buffer

                    return;

                }



                var parts = ScannedLeavePassCode.Split('-');

                if (parts.Length != 3 && parts.Length != 5)

                {

                    System.Media.SystemSounds.Hand.Play();

                    ShowScanFeedback("Mã QR sai định dạng hoặc số phân đoạn.", "#EF4444");

                    ScannedLeavePassCode = string.Empty; // Clear buffer

                    return;

                }



                string dateStr = parts[1];

                string studentIdStr = parts[2];



                if (!int.TryParse(studentIdStr, out int studentId) ||

                    !DateTime.TryParseExact(dateStr, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime leaveDate))

                {

                    System.Media.SystemSounds.Hand.Play();

                    ShowScanFeedback("Mã học sinh hoặc ngày phép trên QR không hợp lệ.", "#EF4444");

                    ScannedLeavePassCode = string.Empty; // Clear buffer

                    return;

                }



                using var db = new AppDbContext();

                

                // Read system settings for strictness

                var strictnessSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Security_OfflineVerificationStrictness");

                string strictness = strictnessSetting?.Value ?? "1";



                if (strictness == "1" || strictness == "2")

                {

                    if (leaveDate.Date != DateTime.Today)

                    {

                        System.Media.SystemSounds.Hand.Play();

                        ShowScanFeedback($"Mã QR đăng ký cho ngày {leaveDate:dd/MM/yyyy}, không phải hôm nay.", "#EF4444");

                        ScannedLeavePassCode = string.Empty; // Clear buffer

                        return;

                    }

                }



                if (strictness == "2")

                {

                    if (parts.Length != 5)

                    {

                        System.Media.SystemSounds.Hand.Play();

                        ShowScanFeedback("Mã thiếu chữ ký và dấu thời gian an ninh.", "#EF4444");

                        ScannedLeavePassCode = string.Empty;

                        return;

                    }



                    string timestampStr = parts[3];

                    string signature = parts[4];



                    if (!long.TryParse(timestampStr, out long unixTime))

                    {

                        System.Media.SystemSounds.Hand.Play();

                        ShowScanFeedback("Dấu thời gian mã QR không hợp lệ.", "#EF4444");

                        ScannedLeavePassCode = string.Empty;

                        return;

                    }

                    var qrTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;

                    var nowUtc = DateTime.UtcNow;

                    if (Math.Abs((nowUtc - qrTime).TotalMinutes) > 10)

                    {

                        System.Media.SystemSounds.Hand.Play();

                        ShowScanFeedback($"Mã QR đã hết hạn hiệu lực xác thực (Quá 10 phút). Giờ QR: {qrTime.ToLocalTime():HH:mm:ss}, Giờ PC: {DateTime.Now:HH:mm:ss}.", "#EF4444");

                        ScannedLeavePassCode = string.Empty;

                        return;

                    }



                    string rawData = $"LP-{dateStr}-{studentIdStr}-{timestampStr}";

                    string expectedSignature = ComputeSha256Hash(rawData + "QASmartClassOfflineSalt_2026");

                    if (signature != expectedSignature)

                    {

                        System.Media.SystemSounds.Hand.Play();

                        ShowScanFeedback("Xác thực chữ ký số offline thất bại (Mã QR giả mạo).", "#EF4444");

                        ScannedLeavePassCode = string.Empty;

                        return;

                    }

                }



                var request = await db.StudentLeaveRequests

                    .FirstOrDefaultAsync(r => r.StudentId == studentId && r.LeaveDate.Date == leaveDate.Date && r.Status == "Approved");



                if (request != null)

                {

                    var student = await db.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId);

                    if (student != null)

                    {

                        var detailsObj = new { Location = "Cổng chính (Xác thực QR)", VerificationCode = ScannedLeavePassCode };

                        var log = new EventLog

                        {

                            EventType = "GateCheckOut",

                            Timestamp = DateTime.Now,

                            Actor = student.StudentCode,

                            Details = System.Text.Json.JsonSerializer.Serialize(detailsObj)

                        };

                        db.EventLogs.Add(log);

                        await db.SaveChangesAsync();



                        System.Media.SystemSounds.Asterisk.Play();

                        ShowScanFeedback($"✅ Học sinh {student.FullName} ra cổng thành công!", "#10B981");



                        ScannedLeavePassCode = string.Empty; // Clear buffer on success

                        await RefreshDataAsync();

                    }

                    else

                    {

                        System.Media.SystemSounds.Hand.Play();

                        ShowScanFeedback("Không tìm thấy thông tin hồ sơ học sinh trên hệ thống.", "#EF4444");

                        ScannedLeavePassCode = string.Empty; // Clear buffer

                    }

                }

                else

                {

                    System.Media.SystemSounds.Hand.Play();

                    ShowScanFeedback("Học sinh không có đơn xin nghỉ/ra sớm được duyệt hôm nay.", "#EF4444");

                    ScannedLeavePassCode = string.Empty; // Clear buffer

                }

            }

            catch (Exception ex)

            {

                Serilog.Log.Error(ex, "[GateMonitor] Verify leave pass failed");

                ShowScanFeedback($"Lỗi hệ thống: {ex.Message}", "#EF4444");

                ScannedLeavePassCode = string.Empty; // Clear buffer on exception

            }

        }



        private async Task CheckHardwareConnectionAsync()

        {

            try

            {

                using var db = new AppDbContext();

                var comPortCheckingModeSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Security_ComPortCheckingMode");

                string comPortCheckingMode = comPortCheckingModeSetting?.Value ?? "1";



                if (comPortCheckingMode == "0")

                {

                    IsHardwareConnected = true;

                    return;

                }



                bool hasComPorts = await Task.Run(() =>

                {

                    try

                    {

                        var ports = System.IO.Ports.SerialPort.GetPortNames();

                        if (ports == null || ports.Length == 0)

                        {

                            return false;

                        }



                        if (comPortCheckingMode == "2")

                        {

                            string targetPort = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_RfidReaderComPort")?.Value ?? "COM3";

                            if (!Array.Exists(ports, p => p.Equals(targetPort, StringComparison.OrdinalIgnoreCase)))

                            {

                                return false;

                            }



                            try

                            {

                                using (var serial = new System.IO.Ports.SerialPort(targetPort, 9600))

                                {

                                    serial.ReadTimeout = 500;

                                    serial.WriteTimeout = 500;

                                    serial.Open();

                                    byte[] pingBytes = new byte[] { 0x02, 0x50, 0x49, 0x4E, 0x47, 0x03 };

                                    serial.Write(pingBytes, 0, pingBytes.Length);

                                    

                                    int response = serial.ReadByte();

                                    return response == 0x06; // ACK

                                }

                            }

                            catch (UnauthorizedAccessException)

                            {

                                // Cổng đang bận có nghĩa là thiết bị đã kết nối và được tiến trình RFID chính mở thành công

                                return true;

                            }

                            catch

                            {

                                return false;

                            }

                        }



                        return true;

                    }

                    catch { }

                    return false;

                });

                IsHardwareConnected = hasComPorts;

            }

            catch

            {

                IsHardwareConnected = false;

            }

        }



        [RelayCommand]

        private async Task TriggerLockdownAsync()

        {

            if (string.IsNullOrWhiteSpace(LockdownPin))

            {

                await AppServices.UIService.ShowInfoAsync("Vui lòng nhập mã PIN bảo mật để kích hoạt phong tỏa.", "Lỗi");

                return;

            }



            try

            {

                using var db = new AppDbContext();

                string staffCode = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "ADMIN";

                

                var alreadyActive = await db.SecurityLockdownLogs.AnyAsync(l => l.Status == "Active");

                if (alreadyActive)

                {

                    await AppServices.UIService.ShowInfoAsync("Hệ thống đã ở trạng thái phong tỏa hoạt động.", "Thông tin");

                    return;

                }



                bool result = QASmartClass.Services.StaffDailyWorkflowsServices.TriggerSecurityLockdown(db, staffCode, LockdownType, LockdownPin);

                if (result)

                {

                    LockdownPin = string.Empty;

                    await AppServices.UIService.ShowInfoAsync(" ĐÃ KÍCH HOẠT PHONG TỎA TOÀN TRƯỜNG THÀNH CÔNG!", "Khẩn cấp");

                    await RefreshDataAsync();

                }

                else

                {

                    await AppServices.UIService.ShowInfoAsync("Kích hoạt phong tỏa thất bại. Vui lòng kiểm tra mã PIN hoặc quyền hạn.", "Lỗi");

                }

            }

            catch (Exception ex)

            {

                Serilog.Log.Error(ex, "Failed to trigger lockdown");

                await AppServices.UIService.ShowInfoAsync("Lỗi hệ thống khi kích hoạt phong tỏa.", "Lỗi");

            }

        }



        [RelayCommand]

        private async Task ReleaseLockdownAsync()

        {

            if (string.IsNullOrWhiteSpace(LockdownPin))

            {

                await AppServices.UIService.ShowInfoAsync("Vui lòng nhập mã PIN bảo mật để giải tỏa phong tỏa.", "Lỗi");

                return;

            }



            if (string.IsNullOrWhiteSpace(ResolutionNotes))

            {

                await AppServices.UIService.ShowInfoAsync("Vui lòng nhập ghi chú giải quyết/tình hình xử lý.", "Lỗi");

                return;

            }



            try

            {

                using var db = new AppDbContext();

                string staffCode = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "ADMIN";



                bool result = QASmartClass.Services.StaffDailyWorkflowsServices.ReleaseSecurityLockdown(db, staffCode, LockdownPin, ResolutionNotes);

                if (result)

                {

                    LockdownPin = string.Empty;

                    ResolutionNotes = string.Empty;

                    await AppServices.UIService.ShowInfoAsync(" Đã giải tỏa phong tỏa trường học thành công.", "Thông báo");

                    await RefreshDataAsync();

                }

                else

                {

                    await AppServices.UIService.ShowInfoAsync("Giải tỏa phong tỏa thất bại. Vui lòng kiểm tra mã PIN hoặc quyền hạn.", "Lỗi");

                }

            }

            catch (Exception ex)

            {

                Serilog.Log.Error(ex, "Failed to release lockdown");

                await AppServices.UIService.ShowInfoAsync("Lỗi hệ thống khi giải tỏa phong tỏa.", "Lỗi");

            }

        }



        public void Dispose()

        {

            _refreshTimer?.Stop();

            _hardwareTimer?.Stop();

        }

    }



    public class GateLogDisplay

    {

        public DateTime Timestamp { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string Actor { get; set; } = string.Empty;

        public string StudentName { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public string AvatarPath { get; set; } = string.Empty;

        public string FriendlyDetails { get; set; } = string.Empty;

        public string DisplayStudentInfo => string.IsNullOrEmpty(StudentName) ? Actor : $"{StudentName} ({ClassName})";

    }



    public class MapNodeDisplay
    {
        public string BeaconId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public double CoordinateX { get; set; }
        public double CoordinateY { get; set; }
        public int StudentCount { get; set; }
        public Brush StatusColor { get; set; } = Brushes.Transparent;
        public string AreaZone { get; set; } = string.Empty;
    }

}



