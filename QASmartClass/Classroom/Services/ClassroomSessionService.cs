using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using QASmartClass.Data;
using QASmartClass.Classroom.Helpers;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    /// <summary>
    /// Service quản lý phiên lớp học: tạo lớp, quản lý HS online, điểm danh
    /// </summary>
    public partial class ClassroomSessionService : ObservableObject
    {
        private readonly NetworkDiscoveryService _networkService;
        private readonly AppDbContext _db;

        [ObservableProperty]
        private bool _isSessionActive = false;

        [ObservableProperty]
        private bool _isSilenceActive = false;

        [ObservableProperty]
        private DateTime? _sessionStartTime = null;

        [ObservableProperty]
        private string _currentClassName = string.Empty;

        [ObservableProperty]
        private string _classCode = string.Empty;

        [ObservableProperty]
        private int _onlineCount = 0;

        public string TeacherName { get; private set; } = string.Empty;

        public BulkObservableCollection<ConnectedStudent> ConnectedStudents { get; } = new();

        public ClassroomSessionService(NetworkDiscoveryService networkService, AppDbContext db)
        {
            _networkService = networkService;
            _db = db;

            _networkService.StudentConnected += OnStudentConnected;
            _networkService.StudentDisconnected += OnStudentDisconnected;
            _networkService.CommandAckReceived += OnCommandAckReceived;
            _networkService.MessageReceived += OnNetworkMessageReceived;
        }

        /// <summary>
        /// Bắt đầu phiên lớp học mới
        /// </summary>
        public async Task StartSessionAsync(string className, string teacherName)
        {
            // ═══ LOI_VID_21 FIX: Lưu muối bảo mật CŨ trước khi sinh phiên mới ═══
            // Muối cũ cần thiết để ký lệnh UPDATE_SESSION mà HS hiện tại có thể xác minh
            string oldSessionSalt = QASmartClass.Services.ClassControlService.Instance.SessionSalt;

            QASmartClass.Services.ClassControlService.Instance.InitializeNewSession();
            CurrentClassName = className;
            TeacherName = teacherName;
            ClassCode = GenerateClassCode();
            IsSessionActive = true;
            SessionStartTime = DateTime.Now;

            // Nạp danh sách học sinh từ active roster dưới dạng offline trước theo dạng Batch
            var rosterStudents = RosterHelper.GetStudents();
            var list = new List<ConnectedStudent>();
            foreach (var s in rosterStudents)
            {
                list.Add(new ConnectedStudent
                {
                    Name = s.FullName,
                    StudentCode = string.IsNullOrWhiteSpace(s.StudentCode) ? $"ST-{s.Id:D3}" : s.StudentCode,
                    PCName = string.IsNullOrWhiteSpace(s.PCName) ? $"PC-{s.Id:D2}" : s.PCName,
                    IPAddress = string.IsNullOrWhiteSpace(s.IPAddress) ? "" : s.IPAddress,
                    IsOnline = false,
                    IsLocked = false
                });
            }
            ConnectedStudents.ReplaceRange(list);
            OnlineCount = 0;

            // Log event
            _db.EventLogs.Add(new EventLog
            {
                EventType = "SessionStart",
                Actor = teacherName,
                Details = $"Lớp: {className}, Mã: {ClassCode}",
                Timestamp = DateTime.Now
            });
            await _db.SaveChangesAsync();

            // Bắt đầu broadcast UDP + TCP listener
            await _networkService.StartAsync(className, teacherName);

            // ═══ LOI_VID_21 FIX: Đồng bộ phiên mới tới HS đã kết nối trước đó ═══
            // Gửi ClassCode & SessionSalt mới tới tất cả socket đang hoạt động
            // Ký bằng muối CŨ để HS xác minh, mã hóa bằng SessionKey riêng từng HS
            string newSessionSalt = QASmartClass.Services.ClassControlService.Instance.SessionSalt;
            await _networkService.SyncActiveClientsToNewSessionAsync(ClassCode, newSessionSalt, oldSessionSalt);

            // Cập nhật trạng thái IsOnline trên giao diện GV cho các HS đang kết nối
            UpdateOnlineStatusFromDiscovery();

            Log.Information("Session started: {Class} ({Code}) — ServerIP: {IP}",
                className, ClassCode, _networkService.ServerIP);
        }

        /// <summary>
        /// LOI_VID_21: Quét danh sách socket đang kết nối từ NetworkDiscoveryService
        /// và đánh dấu IsOnline = true cho HS tương ứng trên giao diện GV.
        /// </summary>
        private void UpdateOnlineStatusFromDiscovery()
        {
            var activeCodes = _networkService.GetActiveStudentCodes();
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                foreach (var code in activeCodes)
                {
                    var student = ConnectedStudents.FirstOrDefault(s =>
                        s.StudentCode.Equals(code, StringComparison.OrdinalIgnoreCase));
                    if (student != null)
                    {
                        student.IsOnline = true;
                    }
                    else
                    {
                        // HS kết nối nhưng không có trong Roster → thêm mới vào danh sách
                        ConnectedStudents.Add(new ConnectedStudent
                        {
                            Name = code,
                            StudentCode = code,
                            IsOnline = true,
                            JoinedAt = DateTime.Now
                        });
                    }
                }
                OnlineCount = ConnectedStudents.Count(s => s.IsOnline);
                Log.Information("[SessionSync] Cập nhật UI: {Online}/{Total} HS online sau đồng bộ phiên",
                    OnlineCount, ConnectedStudents.Count);
            });
        }

        /// <summary>
        /// Kết thúc phiên lớp học
        /// </summary>
        public async Task EndSessionAsync()
        {
            _networkService.Stop();
            SessionStartTime = null;

            _db.EventLogs.Add(new EventLog
            {
                EventType = "SessionEnd",
                Actor = string.IsNullOrEmpty(TeacherName) ? "Teacher" : TeacherName,
                Details = $"Lớp: {CurrentClassName}, Tổng HS: {OnlineCount}",
                Timestamp = DateTime.Now
            });
            await _db.SaveChangesAsync();

            TeacherName = string.Empty;
            ConnectedStudents.Clear();
            OnlineCount = 0;
            IsSessionActive = false;

            Log.Information("Session ended: {Class}", CurrentClassName);
        }

        /// <summary>Gửi lệnh khóa tất cả màn hình HS qua network</summary>
        public async Task LockAllAsync()
        {
            foreach (var s in ConnectedStudents) 
            {
                if (s.IsOnline) s.IsLocked = true;
            }
            await _networkService.LockAllScreensAsync();
            Log.Information("All screens locked via network");
        }

        /// <summary>Gửi lệnh mở khóa tất cả màn hình HS qua network</summary>
        public async Task UnlockAllAsync()
        {
            foreach (var s in ConnectedStudents) s.IsLocked = false;
            await _networkService.UnlockAllScreensAsync();
            Log.Information("All screens unlocked via network");
        }

        /// <summary>Gửi lệnh im lặng tới toàn bộ máy học sinh</summary>
        public async Task SilenceAllAsync()
        {
            IsSilenceActive = true;
            await _networkService.SendCommandAsync("CMD|SILENCE");
            Log.Information("All student screens set to silence mode");
        }

        /// <summary>Gỡ bỏ trạng thái im lặng trên toàn bộ máy học sinh</summary>
        public async Task ClearSilenceAllAsync()
        {
            IsSilenceActive = false;
            await _networkService.SendCommandAsync("CMD|CLEAR_SILENCE");
            Log.Information("All student screens cleared from silence mode");
        }

        /// <summary>Phát Quiz tới tất cả HS qua network</summary>
        public async Task BroadcastQuizAsync(int quizId)
        {
            await _networkService.StartQuizAsync(quizId);
            Log.Information("Quiz {Id} broadcast to {Count} students", quizId, OnlineCount);
        }

        private void OnStudentConnected(object? sender, StudentConnectedEventArgs e)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                var student = ConnectedStudents.FirstOrDefault(s => 
                    (!string.IsNullOrEmpty(s.StudentCode) && s.StudentCode.Equals(e.StudentCode, StringComparison.OrdinalIgnoreCase)) ||
                    s.Name.Equals(e.StudentName, StringComparison.OrdinalIgnoreCase));

                if (student != null)
                {
                    student.IsOnline = true;
                    student.PCName = e.PCName;
                    student.IPAddress = e.IPAddress;
                    student.JoinedAt = DateTime.Now;
                }
                else
                {
                    ConnectedStudents.Add(new ConnectedStudent
                    {
                        Name = e.StudentName,
                        StudentCode = e.StudentCode,
                        PCName = e.PCName,
                        IPAddress = e.IPAddress,
                        JoinedAt = DateTime.Now,
                        IsOnline = true
                    });
                }
                OnlineCount = ConnectedStudents.Count(s => s.IsOnline);
            });

            // Lưu log sự kiện kết nối vào DB (chạy bất đồng bộ, sử dụng context riêng để tránh xung đột luồng)
            Task.Run(async () =>
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
                        db.EventLogs.Add(new EventLog
                        {
                            EventType = "StudentConnect",
                            Actor = e.StudentCode,
                            Details = $"Học sinh {e.StudentName} kết nối từ máy trạm {e.PCName}",
                            MacAddress = e.MacAddress,
                            ClientIP = e.IPAddress,
                            Timestamp = DateTime.Now
                        });
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Không thể lưu log học sinh kết nối: {Err}", ex.Message);
                }
            });
        }

        private void OnStudentDisconnected(object? sender, string studentCode)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                // ═══ LOI_VID_22 FIX: So khớp bằng StudentCode thay vì IPAddress ═══
                var student = ConnectedStudents.FirstOrDefault(s => s.StudentCode.Equals(studentCode, StringComparison.OrdinalIgnoreCase));
                if (student != null)
                {
                    student.IsOnline = false;
                    student.IsLocked = false; // Mở khóa khi mất kết nối
                    OnlineCount = ConnectedStudents.Count(s => s.IsOnline);
                }
            });
        }

        private void OnCommandAckReceived(object? sender, CommandAckEventArgs e)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                var student = ConnectedStudents.FirstOrDefault(s => 
                    s.StudentCode.Equals(e.StudentCode, StringComparison.OrdinalIgnoreCase));
                if (student != null)
                {
                    if (!string.IsNullOrEmpty(e.ErrorMessage) && e.ErrorMessage.Contains("db_status="))
                    {
                        string dbStatus = "OK";
                        string configStatus = "OK";
                        var parts = e.ErrorMessage.Split(',');
                        foreach (var part in parts)
                        {
                            if (part.StartsWith("db_status=")) dbStatus = part.Substring("db_status=".Length);
                            else if (part.StartsWith("config_status=")) configStatus = part.Substring("config_status=".Length);
                        }

                        if (dbStatus.StartsWith("OK") && configStatus.StartsWith("OK"))
                        {
                            student.ShieldStatus = "OK";
                            student.ShieldTooltip = "CSDL & Cấu hình: Hoạt động bình thường";
                        }
                        else if (dbStatus.StartsWith("CORRUPTED") || configStatus.StartsWith("CORRUPTED"))
                        {
                            student.ShieldStatus = "CORRUPTED";
                            student.ShieldTooltip = $"Phát hiện lỗi hỏng! CSDL: {dbStatus}, Cấu hình: {configStatus}";
                        }
                        else if (dbStatus == "RAM_DB_RO")
                        {
                            student.ShieldStatus = "WARNING";
                            student.ShieldTooltip = "Lỗi khóa CSDL! Hệ thống đang chạy trên bộ nhớ RAM (Chế độ dự phòng)";
                        }
                        else
                        {
                            student.ShieldStatus = "WARNING";
                            student.ShieldTooltip = $"Cảnh báo! CSDL: {dbStatus}, Cấu hình: {configStatus}";
                        }
                        Log.Information("Updated student {Code} shield status to {Status} (db={Db}, cfg={Cfg})", 
                            student.StudentCode, student.ShieldStatus, dbStatus, configStatus);
                    }
                }
            });
        }

        private void OnNetworkMessageReceived(object? sender, StudentMessageEventArgs e)
        {
            if (e.Message.StartsWith("PERIPHERAL_UPDATE|"))
            {
                var parts = e.Message.Split('|');
                bool hpOk = true;
                bool micOk = true;
                for (int i = 1; i < parts.Length; i++)
                {
                    if (parts[i].StartsWith("Headphones="))
                    {
                        hpOk = bool.Parse(parts[i].Substring("Headphones=".Length));
                    }
                    else if (parts[i].StartsWith("Mic="))
                    {
                        micOk = bool.Parse(parts[i].Substring("Mic=".Length));
                    }
                }
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    var student = ConnectedStudents.FirstOrDefault(s => s.StudentCode.Equals(e.StudentCode, StringComparison.OrdinalIgnoreCase));
                    if (student != null)
                    {
                        student.IsHeadphoneOk = hpOk;
                        student.IsMicOk = micOk;
                    }
                });
            }
            else if (e.Message.StartsWith("CLAIM_SUCCESS|"))
            {
                var parts = e.Message.Split('|');
                if (parts.Length >= 2 && int.TryParse(parts[1], out var seatNumber))
                {
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        var student = ConnectedStudents.FirstOrDefault(s => s.StudentCode.Equals(e.StudentCode, StringComparison.OrdinalIgnoreCase));
                        if (student != null)
                        {
                            student.SeatNumber = seatNumber;
                        }
                    });

                    Task.Run(async () =>
                    {
                        try
                        {
                            using (var db = new AppDbContext())
                            {
                                var dbStudent = db.Students.FirstOrDefault(s => s.StudentCode.Equals(e.StudentCode, StringComparison.OrdinalIgnoreCase));
                                if (dbStudent != null)
                                {
                                    dbStudent.SeatNumber = seatNumber;
                                    int cols = 8;
                                    int i = seatNumber - 1;
                                    int r = i / cols;
                                    int c = i % cols;
                                    double itemWidth = 150;
                                    double itemHeight = 90;
                                    double spacingX = 30;
                                    double spacingY = 30;
                                    double left = 50 + c * (itemWidth + spacingX);
                                    double top = 40 + r * (itemHeight + spacingY);
                                    dbStudent.PositionX = left / 1000.0;
                                    dbStudent.PositionY = top / 600.0;

                                    dbStudent.LastSeen = DateTime.Now;
                                    dbStudent.IsOnline = true;
                                    await db.SaveChangesAsync();
                                    Log.Information("Saved student seat reservation to CSDL: {StudentCode} -> Seat {Seat}", e.StudentCode, seatNumber);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Failed to save student seat registration");
                        }
                    });
                }
            }
        }

        private static string GenerateClassCode()
        {
            return $"QA-{DateTime.Now:MMdd}-{new Random().Next(1000, 9999)}";
        }
    }

    /// <summary>
    /// Model cho HS đang kết nối
    /// </summary>
    public partial class ConnectedStudent : ObservableObject
    {
        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _studentCode = string.Empty;
        [ObservableProperty] private string _pCName = string.Empty;
        [ObservableProperty] private string _iPAddress = string.Empty;
        [ObservableProperty] private bool _isOnline = true;
        [ObservableProperty] private bool _isLocked = false;
        [ObservableProperty] private bool _isHandRaised = false;
        [ObservableProperty] private DateTime _joinedAt = DateTime.Now;
        [ObservableProperty] private string _shieldStatus = "UNKNOWN"; // "OK" (xanh), "WARNING" (vàng), "CORRUPTED" (đỏ), "UNKNOWN" (xám)
        [ObservableProperty] private string _shieldTooltip = "Trạng thái hệ thống: Chưa rõ";
        [ObservableProperty] private int _seatNumber = 0;
        [ObservableProperty] private bool _isHeadphoneOk = true;
        [ObservableProperty] private bool _isMicOk = true;
    }
}
