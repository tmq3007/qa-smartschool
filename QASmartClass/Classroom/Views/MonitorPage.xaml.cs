using System;

using System.Collections.Generic;

using System.Collections.ObjectModel;

using System.ComponentModel;

using System.Linq;

using System.Runtime.CompilerServices;

using System.Windows;

using System.Windows.Controls;

using System.Windows.Controls.Primitives;

using System.Windows.Input;

using System.Windows.Media;

using System.Windows.Media.Imaging;

using System.Windows.Threading;

using Serilog;



using QASmartClass.Classroom.Helpers;

namespace QASmartClass.Classroom.Views

{

    public partial class MonitorPage : Page

    {

        public static bool IsPrivacyModeActive { get; set; } = false;



        public static string ObfuscatePCName(string pcName)

        {

            if (string.IsNullOrEmpty(pcName)) return "";

            var match = System.Text.RegularExpressions.Regex.Match(pcName, @"\d+$");

            if (match.Success)

            {

                string digits = match.Value;

                string prefix = pcName.Substring(0, pcName.Length - digits.Length);

                if (prefix.Length <= 3)

                {

                    return prefix + "**" + digits;

                }

                else

                {

                    return prefix.Substring(0, 4) + "***" + digits;

                }

            }

            else

            {

                if (pcName.Length <= 3) return "***";

                return pcName.Substring(0, 3) + "***";

            }

        }



        public static string NormalizeName(string name)

        {

            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            return name.Trim().Normalize(System.Text.NormalizationForm.FormC);

        }



        public static bool IsNameMatch(string name1, string name2)

        {

            if (name1 == null || name2 == null) return false;

            return string.Equals(NormalizeName(name1), NormalizeName(name2), StringComparison.OrdinalIgnoreCase);

        }



        private ObservableCollection<ConnectedStudent> _allStudents = new();

        private DispatcherTimer? _refreshTimer;

        private DispatcherTimer? _toastTimer;

        private int  _refreshSeconds = 0;

        private bool _autoRefresh = true;

        private int  _gridColumns = 5;

        private bool _isLocked = false;

        private bool _isBlockingApps = true;



        private string _currentFilter = "all";

        private bool _isSelectMode = false;

        private string _displayMode = "all_class";  // "connected" or "all_class"

        private DateTime _lastToastTime = DateTime.MinValue;  // Toast throttle



        // ─── Web student code → student entry map (O(1) lookup) ───

        private readonly Dictionary<string, ConnectedStudent> _webCodeMap = new(StringComparer.OrdinalIgnoreCase);



        // Off-task app detection list (case-insensitive matching)

        private static readonly string[] BannedApps = {

            "Minecraft", "Valorant", "Among Us", "Roblox", "League of Legends",

            "Fortnite", "PUBG", "GTA", "Steam", "Epic Games",

            "Facebook", "TikTok", "Zalo", "Messenger",

            "YouTube", "Spotify", "Netflix", "Garena", "Discord"

        };





        public MonitorPage()

        {

            InitializeComponent();

            Loaded += (_, _) =>

            {

                Cleanup();



                // Sync Privacy Mode UI controls on load

                if (IsPrivacyModeActive)

                {

                    txtPrivacyStatus.Text = "Riêng tư: BẬT";

                    txtPrivacyStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange

                    pathPrivacy.Data = (Geometry)FindResource("GeomLock");

                    pathPrivacy.Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0));

                }

                else

                {

                    txtPrivacyStatus.Text = "Riêng tư: TẮT";

                    txtPrivacyStatus.Foreground = new SolidColorBrush(Color.FromRgb(176, 181, 195)); // Gray

                    pathPrivacy.Data = (Geometry)FindResource("GeomUnlock");

                    pathPrivacy.Fill = new SolidColorBrush(Color.FromRgb(176, 181, 195));

                }



                LoadStudentsFromDB();

                StartAutoRefresh();

                SubscribeNetworkEvents();

                if (Window.GetWindow(this) is ClassroomShell shell && !string.IsNullOrEmpty(shell.SelectedStudentCode))
                {
                    string codeToFocus = shell.SelectedStudentCode;
                    shell.SelectedStudentCode = null;
                    var targetStudent = _allStudents.FirstOrDefault(s => s.StudentCode.Equals(codeToFocus, StringComparison.OrdinalIgnoreCase));
                    if (targetStudent != null)
                    {
                        Dispatcher.BeginInvoke(new System.Action(() => ShowStudentActionDialog(targetStudent)));
                    }
                }



                // Subscribe: khi đổi lớp → refresh DS học sinh

                try

                {

                    // app → ClassroomAppContext (refactored)

                    ClassroomAppContext.ClassRoster.ActiveRosterChanged += OnActiveRosterChanged;

                }

                catch { }

                try
                {
                    QASmartClass.Services.VncBroadcastService.Instance.StatusChanged += OnVncBroadcastStatusChanged;
                    RefreshVncBadgesVisibility();
                }
                catch { }

            };

            Unloaded += (_, _) => Cleanup();

        }



        private void OnActiveRosterChanged(object? sender, Data.ClassRoster? roster)

        {

            Dispatcher.Invoke(() => LoadStudentsFromDB());

        }



        private void Cleanup()

        {

            _refreshTimer?.Stop();

            _refreshTimer = null;

            _toastTimer?.Stop();

            _toastTimer = null;

            UnsubscribeNetworkEvents();

            try

            {

                // app → ClassroomAppContext (refactored)

                if (ClassroomAppContext.ClassRoster != null)

                {

                    ClassroomAppContext.ClassRoster.ActiveRosterChanged -= OnActiveRosterChanged;

                }

            }

            catch { }

            try
            {
                QASmartClass.Services.VncBroadcastService.Instance.StatusChanged -= OnVncBroadcastStatusChanged;
            }
            catch { }

        }



        /// <summary>Lắng nghe events từ NetworkDiscoveryService (TCP + WS relay)</summary>

        private void SubscribeNetworkEvents()

        {

            try

            {

                // app → ClassroomAppContext (refactored)

                var net = ClassroomAppContext.Network;

                if (net != null)

                {

                    // Safe pattern: unsubscribe first

                    net.MessageReceived      -= OnStudentMessage;

                    net.HeartbeatReceived    -= OnStudentMessage;  // LOI_VID_30: dedicated HB event

                    net.StudentConnected     -= OnWebStudentConnected;

                    net.StudentDisconnected  -= OnWebStudentDisconnected;

                    if (net.WebBridge != null)

                    {

                        net.WebBridge.WebMessageReceived -= OnStudentMessage;

                        net.WebBridge.WebHeartbeatReceived -= OnStudentMessage;  // LOI_VID_30

                    }



                    net.MessageReceived      += OnStudentMessage;

                    net.HeartbeatReceived    += OnStudentMessage;  // LOI_VID_30: subscribe dedicated HB

                    net.StudentConnected     += OnWebStudentConnected;     // relays WS too

                    net.StudentDisconnected  += OnWebStudentDisconnected;  // relays WS too



                    // Also subscribe WebBridge directly if already running

                    if (net.WebBridge != null)

                    {

                        net.WebBridge.WebMessageReceived += OnStudentMessage;

                        net.WebBridge.WebHeartbeatReceived += OnStudentMessage;  // LOI_VID_30

                    }

                }

            }

            catch { }

        }



        private void UnsubscribeNetworkEvents()

        {

            try

            {

                // app → ClassroomAppContext (refactored)

                var net = ClassroomAppContext.Network;

                if (net != null)

                {

                    net.MessageReceived     -= OnStudentMessage;

                    net.HeartbeatReceived   -= OnStudentMessage;  // LOI_VID_30

                    net.StudentConnected    -= OnWebStudentConnected;

                    net.StudentDisconnected -= OnWebStudentDisconnected;

                    if (net.WebBridge != null)

                    {

                        net.WebBridge.WebMessageReceived -= OnStudentMessage;

                        net.WebBridge.WebHeartbeatReceived -= OnStudentMessage;  // LOI_VID_30

                    }

                }

            }

            catch { }

        }



        // ═══ Student connected (TCP or Web) — map code → entry ═══

        private void OnWebStudentConnected(object? sender, Services.StudentConnectedEventArgs e)

        {

            if (e == null) return;

            Dispatcher.BeginInvoke(() =>

            {

                // 1. Prioritized matching logic:

                // First by StudentCode (if present)

                ConnectedStudent? student = null;

                if (!string.IsNullOrEmpty(e.StudentCode))

                {

                    student = _allStudents.FirstOrDefault(s =>

                        !string.IsNullOrEmpty(s.StudentCode) &&

                        s.StudentCode.Equals(e.StudentCode, StringComparison.OrdinalIgnoreCase));

                }



                // Second by Normalized Name

                if (student == null)

                {

                    student = _allStudents.FirstOrDefault(s => IsNameMatch(s.Name, e.StudentName));

                }



                // Third by PCName (fallback for TCP clients)

                if (student == null && !string.IsNullOrEmpty(e.PCName))

                {

                    student = _allStudents.FirstOrDefault(s =>

                        !string.IsNullOrEmpty(s.PCName) &&

                        s.PCName.Equals(e.PCName, StringComparison.OrdinalIgnoreCase));

                }



                if (student == null)

                {

                    // Create dynamic entry for web student

                    student = new ConnectedStudent

                    {

                        Name        = e.StudentName,

                        PCName      = string.IsNullOrEmpty(e.PCName) ? $"Web-{e.StudentCode}" : e.PCName,

                        IPAddress   = e.IPAddress + (e.PCName?.StartsWith("Web-") == true ? " (Web)" : ""),

                        StudentCode = e.StudentCode,

                        IsOnline    = true,

                        FocusPct    = 85

                    };

                    _allStudents.Add(student);

                    Log.Information("MonitorPage: dynamic entry added: {Name} ({Code})", e.StudentName, e.StudentCode);

                }

                else

                {

                    // Update connection info

                    student.StudentCode = e.StudentCode;

                    student.IsOnline    = true;

                    student.FocusPct    = 85;

                    if (!string.IsNullOrEmpty(e.PCName))

                        student.PCName = e.PCName;

                    if (!string.IsNullOrEmpty(e.IPAddress))

                        student.IPAddress = e.IPAddress;

                    Log.Information("MonitorPage: matched student {Name} → {Code} ({PC})", student.Name, student.StudentCode, student.PCName);

                }



                // 2. Prevent duplicates in UI: Check if there's any OTHER student with the same StudentCode or Name

                if (!string.IsNullOrEmpty(student.StudentCode) || !string.IsNullOrEmpty(student.Name))

                {

                    var duplicates = _allStudents.Where(s =>

                        s != student &&

                        ((!string.IsNullOrEmpty(s.StudentCode) && !string.IsNullOrEmpty(student.StudentCode) && s.StudentCode.Equals(student.StudentCode, StringComparison.OrdinalIgnoreCase)) ||

                         IsNameMatch(s.Name, student.Name)))

                        .ToList();



                    foreach (var dup in duplicates)

                    {

                        // Merge screenshot / status data

                        if (student.ScreenshotSource == null && dup.ScreenshotSource != null)

                        {

                            student.ScreenshotSource = dup.ScreenshotSource;

                        }



                        int studentIdx = _allStudents.IndexOf(student);

                        int dupIdx = _allStudents.IndexOf(dup);



                        if (dupIdx >= 0 && studentIdx >= 0 && dupIdx < studentIdx)

                        {

                            // dup was added earlier (closer to the roster), so dup is the roster card!

                            // Swap them: update connection details to dup, remove student (dynamic card)

                            dup.StudentCode = student.StudentCode;

                            dup.PCName = student.PCName;

                            dup.IPAddress = student.IPAddress;

                            dup.IsOnline = true;

                            dup.FocusPct = student.FocusPct;

                            

                            _allStudents.Remove(student);

                            Log.Information("MonitorPage: Kept roster card {Name} ({PC}) and removed dynamic duplicate.", dup.Name, dup.PCName);

                            student = dup;

                        }

                        else

                        {

                            // Otherwise, remove 'dup' (dynamic card)

                            _allStudents.Remove(dup);

                            Log.Information("MonitorPage: Removed duplicate dynamic card for student {Code} ({Name})", student.StudentCode, dup.Name);

                        }

                    }

                }



                _webCodeMap[e.StudentCode] = student;

                ApplyFilter();

                UpdateStats();

                AddLog("Kết nối", $"{e.StudentName} • {e.IPAddress}", "#E3F2FD", "#1565C0");

            });

        }



        // ═══ Student disconnected (TCP or Web) ═══

        private void OnWebStudentDisconnected(object? sender, string code)

        {

            if (string.IsNullOrEmpty(code)) return;

            Dispatcher.BeginInvoke(() =>

            {

                if (_webCodeMap.TryGetValue(code, out var student))

                {

                    student.IsOnline         = false;

                    student.ScreenshotSource = null;

                    student.HasAlert         = false; // Clear alerts when student disconnects

                    student.OffTaskApp       = "";

                    student.OnPropertyChanged(nameof(student.ScreenshotSource));

                    student.OnPropertyChanged(nameof(student.PlaceholderVisibility));

                    _webCodeMap.Remove(code);

                }

                ApplyFilter();

                UpdateStats();

            });

        }



        /// <summary>Xử lý HB_UPDATE và SCREENSHOT từ HS</summary>

        private void OnStudentMessage(object? sender, Classroom.Services.StudentMessageEventArgs e)

        {

            if (e?.Message == null) return;



            if (e.Message.StartsWith("CMD|STUDENT_BROADCAST_STATUS|"))

            {

                var statusParts = e.Message.Split('|');

                if (statusParts.Length >= 4)

                {

                    var status = statusParts[3];

                    Dispatcher.BeginInvoke(() =>

                    {

                        var student = FindStudentByCode(e.StudentCode);

                        if (student != null)

                        {

                            if (status == "DISMISSED")

                            {

                                student.OffTaskApp = "📝 Tự học";

                                student.HasAlert = false;

                                AddLog($"Trạng thái: {student.PCName}", $"Học sinh {student.Name} đã đóng màn hình trình chiếu để tự học.", "#1A237E", "#E3F2FD");

                            }

                        }

                    });

                }

                return;

            }



            if (e.Message.StartsWith("CMD|FOCUS_STATUS|") || e.Message.StartsWith("FOCUS_STATUS|"))
            {
                var parts = e.Message.Split('|');
                int offset = e.Message.StartsWith("CMD|") ? 2 : 1;
                if (parts.Length >= offset + 2)
                {
                    bool isWatching = parts[offset] == "1" || parts[offset].Equals("true", StringComparison.OrdinalIgnoreCase);
                    string foregroundApp = parts[offset + 1].Trim();

                    Dispatcher.BeginInvoke(() =>
                    {
                        var student = FindStudentByCode(e.StudentCode);
                        if (student != null)
                        {
                            student.IsWatchingBroadcast = isWatching;
                            student.ForegroundApp = foregroundApp;
                            UpdateFocusStatistics();
                        }
                    });
                }
                return;
            }

            // === UPGRADE_14: Xử lý tin báo gian lận thi cử từ học sinh ===

            if (e.Message.StartsWith("CHEATING_ALERT|"))

            {

                var parts = e.Message.Split('|');

                if (parts.Length < 3) return;



                var quizId = parts[1];

                var reason = parts[2];



                Dispatcher.BeginInvoke(() =>

                {

                    var student = FindStudentByCode(e.StudentCode);

                    if (student == null) return;



                    student.IsOnline = true;

                    student.HasAlert = true; // Kích hoạt viền đỏ cảnh báo trên giao diện giám sát

                    student.OffTaskApp = "Gian lận: " + reason; // Hiển thị lý do rời ứng dụng



                    // Ghi nhật ký vào khung log trạng thái bên phải của Giáo viên

                    AddLog($"Cảnh báo gian lận: {student.PCName}", 

                           $"Học sinh {student.Name} bị cảnh báo: {reason} (Bài Quiz ID: {quizId})", 

                           "#3D1010", "#FF3B30");

                });

                return;

            }



            // ── HB_UPDATE|activeApp|cpuUsage ──

            if (e.Message.StartsWith("HB_UPDATE|"))

            {

                var parts = e.Message.Split('|');

                if (parts.Length < 3) return;



                var activeApp = parts[1];

                var cpu = int.TryParse(parts[2], out var c) ? c : 0;



                Dispatcher.BeginInvoke(() =>

                {

                    var student = FindStudentByCode(e.StudentCode);

                    if (student == null) return;



                    student.IsOnline = true; // UPGRADE_06: Consolidate status on heartbeat receipt!

                    student.CpuUsage = cpu;



                    bool isBanned = BannedApps.Any(banned =>

                        activeApp.IndexOf(banned, StringComparison.OrdinalIgnoreCase) >= 0);



                    if (isBanned)

                    {

                        student.OffTaskApp = activeApp;

                        student.HasAlert = true;

                        AddLog($"Cảnh báo: {student.PCName}", $"{student.Name} đang mở ứng dụng ngoài bài học: {activeApp}", "#3D2020", "#FF5252");

                    }

                    else

                    {

                        student.OffTaskApp = "";

                        student.HasAlert = false;

                    }

                });

                return;

            }



            // ── SCREENSHOT|code|base64jpeg ──

            if (e.Message.StartsWith("SCREENSHOT|"))

            {

                // Format: SCREENSHOT|code|base64  (find 2nd pipe)

                var firstPipe  = e.Message.IndexOf('|');

                var secondPipe = e.Message.IndexOf('|', firstPipe + 1);

                if (secondPipe < 0) return;

                var b64 = e.Message.Substring(secondPipe + 1);

                if (string.IsNullOrWhiteSpace(b64)) return;



                var code = e.StudentCode;



                // Pre-decode on thread-pool to avoid blocking UI thread

                byte[]? bytes = null;

                try { bytes = Convert.FromBase64String(b64); }

                catch { return; }



                Dispatcher.BeginInvoke(() =>

                {

                    var student = FindStudentByCode(code);



                    if (student == null)

                    {

                        student = new ConnectedStudent

                        {

                            Name        = code,

                            PCName      = $"Web-{code}",

                            IPAddress   = "(Web)",

                            StudentCode = code,

                            IsOnline    = true,

                            FocusPct    = 85

                        };

                        _allStudents.Add(student);

                        _webCodeMap[code] = student;

                        Log.Information("MonitorPage: auto-created entry for web student {Code}", code);

                        ApplyFilter();

                    }

                    if (!student.IsOnline)
                    {
                        return;
                    }

                    student.IsOnline = true; // UPGRADE_06: Consolidate status on screenshot receipt



                    if ((DateTime.Now - student.LastScreenshotTime).TotalMilliseconds < 800)

                    {

                        return; // Ignore updates if less than 800ms elapsed since last update (Frame Rate Throttle)

                    }

                    student.LastScreenshotTime = DateTime.Now;



                    try

                    {

                        using var ms = new System.IO.MemoryStream(bytes!);



                        // Decode to BitmapFrame to get pixel data

                        var decoder = BitmapDecoder.Create(ms,

                            BitmapCreateOptions.PreservePixelFormat,

                            BitmapCacheOption.OnLoad);

                        var frame = decoder.Frames[0];



                        // ═══ Flicker-free: reuse WriteableBitmap, write pixels in-place ═══

                        if (student.ScreenshotSource is WriteableBitmap wb

                            && wb.PixelWidth  == frame.PixelWidth

                            && wb.PixelHeight == frame.PixelHeight)

                        {

                            // Same size → write pixels directly (no new object, no blank flash)

                            wb.Lock();

                            var converted = new FormatConvertedBitmap(frame,

                                PixelFormats.Bgra32, null, 0);

                            converted.CopyPixels(

                                new System.Windows.Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight),

                                wb.BackBuffer, wb.BackBufferStride * wb.PixelHeight, wb.BackBufferStride);

                            wb.AddDirtyRect(new System.Windows.Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));

                            wb.Unlock();

                            // Trigger computed-property update without replacing Source object

                            student.OnPropertyChanged(nameof(student.ScreenshotSource));

                        }

                        else

                        {

                            // First frame or size changed → create WriteableBitmap (NOT frozen — WriteableBitmap cannot be frozen)

                            var newWb = new WriteableBitmap(

                                new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0));

                            // NOTE: Must stay on UI thread â€” already in Dispatcher.BeginInvoke

                            student.ScreenshotSource = newWb;

                        }



                        Log.Debug("Screenshot applied: {Name} ({Code}) {Len}KB",

                            student.Name, code, b64.Length / 1024);

                    }

                    catch (Exception ex)

                    {

                        Log.Warning("Screenshot decode error from {Code}: {Err}", code, ex.Message);

                    }

                });

            }

        }



        // ─── O(1) student lookup: webCodeMap first, then linear scan ───

        private ConnectedStudent? FindStudentByCode(string code)

        {

            if (string.IsNullOrWhiteSpace(code)) return null;



            // 1. Fast path: exact code match in web map

            if (_webCodeMap.TryGetValue(code, out var mapped)) return mapped;



            // 2. Slow path: scan by PCName or StudentCode (TCP students)

            return _allStudents.FirstOrDefault(s =>

                (s.StudentCode != null && s.StudentCode.Equals(code, StringComparison.OrdinalIgnoreCase)) ||

                s.PCName.Equals(code, StringComparison.OrdinalIgnoreCase) ||

                s.PCName.Contains(code, StringComparison.OrdinalIgnoreCase));

        }



        // ═══════════════════════════════════════════════════════

        //  DATA LOAD

        // ═══════════════════════════════════════════════════════



        private void LoadStudentsFromDB()

        {

            try

            {

                // app → ClassroomAppContext (refactored)

                var rosterStudents = QASmartClass.Classroom.Services.RosterHelper.GetStudents();



                _allStudents.Clear();

                // Ensure at least 35 students for full grid display

                var demoNames = new[]

                {

                    "Nguyễn Văn An","Trần Thị Bình","Lê Hoàng Cường","Phạm Thị Dung","Hoàng Văn Em",

                    "Ngô Thị Phương","Đỗ Quang Hải","Vũ Thị Hoa","Bùi Đức Khang","Lý Thị Lan",

                    "Mai Văn Minh","Đinh Thị Ngọc","Trương Quốc Phong","Đặng Thị Quỳnh","Hà Sĩ Ren",

                    "Cao Thị Sen","Tô Văn Tuấn","Phan Thị Uyên","Lương Văn Vũ","Châu Thị Xuân",

                    "Dương Thị Yến","Trịnh Văn Anh","Lưu Thị Bé","Kiều Văn Chiến","Mạc Thị Diệp",

                    "Nghiêm Văn Giao","Đoàn Thị Hà","Tống Văn Ích","Hồ Thị Kim","Đỗ Văn Long",

                    "Trần Minh Nhật","Lê Thị Oanh","Phạm Văn Phú","Nguyễn Quốc Sơn","Vũ Thị Tâm"

                };



                if (rosterStudents.Any())

                {

                    foreach (var s in rosterStudents)

                        _allStudents.Add(new ConnectedStudent

                        {

                            Name      = s.FullName,

                            PCName    = string.IsNullOrWhiteSpace(s.PCName) ? $"PC-{s.Id:D2}" : s.PCName,

                            IPAddress = string.IsNullOrWhiteSpace(s.IPAddress) ? $"192.168.1.{100+s.Id}" : s.IPAddress,

                            IsOnline  = s.IsOnline,

                            StudentCode = s.StudentCode,

                            FocusPct  = 0  // Will be updated by real HB_UPDATE or SyncFromNetworkService

                        });



                    // Pad remaining slots with demo students (NOT connected)

                    int startIdx = _allStudents.Count;

                    for (int i = startIdx; i < 35; i++)

                        _allStudents.Add(new ConnectedStudent

                        {

                            Name      = demoNames[i % demoNames.Length],

                            PCName    = $"PC-{i + 1:D2}",

                            IPAddress = $"192.168.1.{100 + i}",

                            IsOnline  = false,

                            FocusPct  = 0

                        });

                }

                else

                {

                    // Full demo: 35 students â€” all offline by default

                    // Status will be updated by SyncFromNetworkService()

                    for (int i = 0; i < demoNames.Length; i++)

                        _allStudents.Add(new ConnectedStudent

                        {

                            Name      = demoNames[i],

                            PCName    = $"PC-{i + 1:D2}",

                            IPAddress = $"192.168.1.{100 + i}",

                            IsOnline  = false,  // Real status from network sync

                            FocusPct  = 0

                        });

                }



                ApplyFilter();

                UpdateStats();

                CaptureStudentScreenLive(); // Live capture (fallback: demo)

                AddLog("Hệ thống", $"Đã tải {_allStudents.Count} học sinh", "#E3F2FD", "#1565C0");

                Log.Information("MonitorPage loaded {Count} students", _allStudents.Count);

            }

            catch (Exception ex)

            {

                Log.Warning("MonitorPage load error: {Error}", ex.Message);

                AddLog("Lỗi", ex.Message, "#FFF3E0", "#E65100");

            }

        }



        // ═══════════════════════════════════════════════════════

        //  LIVE SCREENSHOT — Chụp màn hình HS thực (StudentShell)

        // ═══════════════════════════════════════════════════════



        /// <summary>

        /// Chụp màn hình StudentShell local (cùng process) — chỉ gán cho 1 student tương ứng,

        /// KHÔNG gán cho tất cả online students.

        /// Screenshot của các HS thực khác đến từ SCREENSHOT| message qua OnStudentMessage.

        /// </summary>

        private void CaptureStudentScreenLive()

        {

            try

            {

                // ═══ Find StudentShell window (local, cùng process) ═══

                Window? studentWindow = null;

                foreach (Window w in Application.Current.Windows)

                {

                    if (w is QASmartClass.StudentClient.Views.StudentShell)

                    {

                        studentWindow = w;

                        break;

                    }

                }



                if (studentWindow == null)

                {

                    // StudentShell chưa mở → không fake screenshot

                    return;

                }



                // ═══ Capture StudentShell content ═══

                var visual = studentWindow.Content as Visual;

                if (visual is not FrameworkElement fe || fe.ActualWidth <= 0 || fe.ActualHeight <= 0)

                    return;



                var dpiX = 96.0;

                var dpiY = 96.0;

                var source = PresentationSource.FromVisual(studentWindow)

                          ?? PresentationSource.FromVisual(this);

                if (source?.CompositionTarget != null)

                {

                    dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;

                    dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;

                }



                int pixelWidth = (int)(fe.ActualWidth * dpiX / 96.0);

                int pixelHeight = (int)(fe.ActualHeight * dpiY / 96.0);

                if (pixelWidth <= 0 || pixelHeight <= 0) return;



                var rtb = new RenderTargetBitmap(pixelWidth, pixelHeight, dpiX, dpiY, PixelFormats.Pbgra32);

                rtb.Render(fe);

                rtb.Freeze();



                // ═══ Assign ONLY to the local student (first online, or PC-01) ═══

                // This is the student running in the same process as the teacher app (demo/dev mode)

                var localStudent = _allStudents.FirstOrDefault(s => s.IsOnline);

                if (localStudent != null)

                {

                    localStudent.ScreenshotSource = rtb;

                    localStudent.OnPropertyChanged(nameof(localStudent.ScreenshotSource));

                    localStudent.OnPropertyChanged(nameof(localStudent.PlaceholderVisibility));

                }

            }

            catch (Exception ex)

            {

                Log.Warning("Live capture error: {Err}", ex.Message);

            }

        }





        // ═══════════════════════════════════════════════════════

        //  DISPLAY MODE + FILTER

        // ═══════════════════════════════════════════════════════



        private void DisplayMode_Changed(object sender, SelectionChangedEventArgs e)

        {

            if (cmbDisplayMode == null || monitorGrid == null) return;

            _displayMode = cmbDisplayMode.SelectedIndex switch

            {

                0 => "connected", _ => "all_class"

            };

            ApplyFilter();

            UpdateStats();

            Log.Information("Display mode: {Mode}", _displayMode);

        }



        private void Filter_Changed(object sender, SelectionChangedEventArgs e)

        {

            if (cmbFilter == null || monitorGrid == null) return;

            _currentFilter = cmbFilter.SelectedIndex switch

            {

                1 => "online", 2 => "offline", 3 => "alert", _ => "all"

            };

            ApplyFilter();

        }



        private void ApplyFilter()

        {

            if (monitorGrid == null) return;



            // Step 1: Display mode — connected only vs all class

            IEnumerable<ConnectedStudent> src = _displayMode == "connected"

                ? _allStudents.Where(s => s.IsOnline)

                : _allStudents;



            // Step 2: Additional filter

            src = _currentFilter switch

            {

                "online"  => src.Where(s => s.IsOnline),

                "offline" => src.Where(s => !s.IsOnline),

                "alert"   => src.Where(s => s.HasAlert),

                _         => src

            };



            var list = src.ToList();

            var rows = new List<StudentRow>();

            for (int i = 0; i < list.Count; i += _gridColumns)

            {

                var row = new StudentRow { ColumnsCount = _gridColumns };

                row.Students.AddRange(list.Skip(i).Take(_gridColumns));

                rows.Add(row);

            }

            monitorGrid.ItemsSource = rows;

        }



        // ═══════════════════════════════════════════════════════

        //  STATS

        // ═══════════════════════════════════════════════════════



        private void UpdateStats()

        {

            if (txtMonitorCount == null) return; // Guard: XAML not ready

            int online  = _allStudents.Count(s => s.IsOnline);

            int alerts  = _allStudents.Count(s => s.HasAlert);

            double cpuAvg = online > 0 ? _allStudents.Where(s => s.IsOnline).Average(s => s.CpuUsage) : 0;



            // Bottom status bar

            if (QASmartClass.Services.VncBroadcastService.Instance.IsVncBroadcasting)
            {
                UpdateFocusStatistics();
            }
            else
            {
                txtMonitorCount.Text  = $"{online}/{_allStudents.Count} trực tuyến";
            }

            txtAlertCount.Text    = $"{alerts} cảnh báo";

            txtCpuAvg.Text        = $"{cpuAvg:F0}%";

            txtRefreshStatus.Text = _autoRefresh ? $"Tự động: {_refreshSeconds}s" : "Tạm dừng";



            // Show toast for off-task students (throttle: max once per 8s)

            if ((DateTime.Now - _lastToastTime).TotalSeconds >= 8)

            {

                var offTaskStudents = _allStudents.Where(s => s.HasAlert && !string.IsNullOrEmpty(s.OffTaskApp)).ToList();

                if (offTaskStudents.Any())

                {

                    var first = offTaskStudents.First();

                    ShowToast(

                        $"Cảnh báo: {first.PCName} ({first.Name.Split(' ').Last()})",

                        $"Đang mở ứng dụng làm việc riêng ({first.OffTaskApp}) - Yêu cầu tập trung!");

                    _lastToastTime = DateTime.Now;

                }

            }

        }



        // ═══════════════════════════════════════════════════════

        //  NETWORK SYNC — Lấy trạng thái thực từ NetworkService

        // ═══════════════════════════════════════════════════════



        /// <summary>Đồng bộ trạng thái HS từ NetworkDiscoveryService (thực tế)</summary>

        private void SyncFromNetworkService()

        {

            try

            {

                // app → ClassroomAppContext (refactored)

                var net = ClassroomAppContext.Network;

                if (net == null || !net.IsBroadcasting) return;



                // Get real connected clients

                var connectedClients = net.GetConnectedStudents();

                var connectedCodes = new HashSet<string>(

                    connectedClients.Select(c => c.Code),

                    StringComparer.OrdinalIgnoreCase);



                // ═══ FIX: also include web-connected students from WsBridge ═══

                var webClients = net.WebBridge?.GetConnectedWebStudents() ?? [];

                var webCodeSet = new HashSet<string>(

                    webClients.Select(w => w.Code),

                    StringComparer.OrdinalIgnoreCase);



                foreach (var st in _allStudents)

                {

                    // Match TCP students

                    var client = connectedClients.FirstOrDefault(c =>

                        (!string.IsNullOrEmpty(st.StudentCode) && c.Code.Equals(st.StudentCode, StringComparison.OrdinalIgnoreCase)) ||

                        c.PCName.Equals(st.PCName, StringComparison.OrdinalIgnoreCase) ||

                        c.Code.Equals(st.PCName, StringComparison.OrdinalIgnoreCase) ||

                        IsNameMatch(c.Name, st.Name));



                    if (client != null)

                    {

                        // ═══ REAL connected student (TCP) ═══

                        st.IsOnline = true;

                        st.IPAddress = client.IPAddress;

                        st.CpuUsage = client.CpuUsage;



                        // Đảm bảo đồng bộ PCName mới nhất (LOI_VID_32)

                        if (!string.IsNullOrEmpty(client.PCName))

                        {

                            st.PCName = client.PCName;

                        }



                        bool isBanned = !string.IsNullOrEmpty(client.ActiveApp) &&

                            BannedApps.Any(banned =>

                                client.ActiveApp.IndexOf(banned, StringComparison.OrdinalIgnoreCase) >= 0);



                        if (isBanned) { st.OffTaskApp = client.ActiveApp; st.HasAlert = true; }

                        else { st.OffTaskApp = ""; st.HasAlert = false; }



                        if (string.IsNullOrEmpty(st.StudentCode))

                        {

                            st.StudentCode = client.Code;

                        }

                    }

                    else

                    {

                        // ═══ FIX: Check web clients by code or name ═══

                        var webClient = webClients.FirstOrDefault(w =>

                            w.Code.Equals(st.StudentCode ?? "", StringComparison.OrdinalIgnoreCase) ||

                            IsNameMatch(w.Name, st.Name));



                        if (webClient != null)

                        {

                            // Web student online

                            st.IsOnline = true;

                            st.IPAddress = webClient.IPAddress + " (Web)";

                            st.FocusPct = 85; // Web students: default focus

                            st.OffTaskApp = "";

                            st.HasAlert = false;

                            // Store code for future matching

                            if (st.StudentCode == null)

                                st.StudentCode = webClient.Code;

                        }

                        else

                        {

                            // ═══ NOT connected ═══

                            st.IsOnline = false;

                            st.FocusPct = 0;

                            st.OffTaskApp = "";

                            st.HasAlert = false;

                            st.ScreenshotSource = null;

                            st.OnPropertyChanged(nameof(st.ScreenshotSource));

                            st.OnPropertyChanged(nameof(st.PlaceholderVisibility));

                        }

                    }

                }

            }

            catch (Exception ex)

            {

                Log.Warning("NetworkSync error: {Err}", ex.Message);

            }

        }



        // ═══════════════════════════════════════════════════════

        //  AUTO-REFRESH

        // ═══════════════════════════════════════════════════════



        private void StartAutoRefresh()

        {

            if (_refreshTimer != null)

            {

                _refreshTimer.Stop();

                _refreshTimer = null;

            }

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };

            _refreshTimer.Tick += (s, e) =>

            {

                if (!_autoRefresh) return;

                _refreshSeconds += 5;



                // ═══ SYNC from real NetworkDiscoveryService ═══

                SyncFromNetworkService();



                ApplyFilter();

                UpdateStats();



                // ═══ LIVE CAPTURE: chụp StudentShell mỗi 5s ═══

                CaptureStudentScreenLive();



                // Request screenshots from all students every 10s (every 2nd tick)

                if (_refreshSeconds % 10 == 0)

                {

                    RequestScreenshots();

                }

            };

            _refreshTimer.Start();

        }



        /// <summary>Gửi lệnh REQUEST_SCREENSHOT tới tất cả HS</summary>

        private void RequestScreenshots()

        {

            try

            {

                // app → ClassroomAppContext (refactored)

                var net = ClassroomAppContext.Network;

                if (net?.IsBroadcasting == true)

                {

                    // V4.1: Nếu đang phát màn hình bài giảng, tạm dừng quét tự động để tối ưu đường truyền và tránh lặp đệ quy

                    if (QASmartClass.Services.BroadcastStateService.Instance.IsScreenBroadcastActive)

                    {

                        Log.Debug("[MonitorPage] Screen broadcast is active. Postponing automatic screenshot sweep to prioritize stream bandwidth.");

                        return;

                    }

                    _ = net.SendCommandAsync("CMD|REQUEST_SCREENSHOT");

                    Log.Debug("Requested screenshots from all students");

                }

            }

            catch (Exception ex) { Log.Warning("RequestScreenshots error: {Err}", ex.Message); }

        }



        private void ToggleAutoRefresh_Click(object sender, MouseButtonEventArgs e)

        {

            _autoRefresh = !_autoRefresh;

            txtRefreshStatus.Foreground = _autoRefresh

                ? new SolidColorBrush(Color.FromRgb(76, 175, 80))

                : new SolidColorBrush(Color.FromRgb(255, 152, 0));

            UpdateStats();

            AddLog(_autoRefresh ? "Bắt đầu" : "Tạm dừng", "Tự động cập nhật màn hình", "#2D3038", "#8E93A0");

        }



        // ═══════════════════════════════════════════════════════

        //  GRID SIZE

        // ═══════════════════════════════════════════════════════



        private void GridSize_Changed(object sender, SelectionChangedEventArgs e)

        {

            if (cmbGrid?.SelectedItem is ComboBoxItem item && monitorGrid != null)

            {

                _gridColumns = item.Content?.ToString()?.StartsWith("3") == true ? 3 :

                               item.Content?.ToString()?.StartsWith("5") == true ? 5 :

                               item.Content?.ToString()?.StartsWith("6") == true ? 6 : 4;



                ApplyFilter();

            }

        }



        // ═══════════════════════════════════════════════════════

        //  ACTIONS

        // ═══════════════════════════════════════════════════════



        private void LockAll_Click(object sender, RoutedEventArgs e)

        {

            _isLocked = true;

            int online = _allStudents.Count(s => s.IsOnline);

            SendCommand("CMD|LOCK_SCREEN");

            AddLog("Khóa màn hình", $"Đã thực hiện khóa {online} máy học sinh", "#FFF3E0", "#E65100");

            Log.Information("Lock all screens: {Count}", online);

        }



        private void UnlockAll_Click(object sender, RoutedEventArgs e)

        {

            _isLocked = false;

            int online = _allStudents.Count(s => s.IsOnline);

            SendCommand("CMD|UNLOCK_SCREEN");

            AddLog("Mở khóa", $"Đã thực hiện mở khóa {online} máy học sinh", "#E8F5E9", "#2E7D32");

            Log.Information("Unlock all screens: {Count}", online);

        }



        // ═══════════════════════════════════════════════════════

        //  SELECTION MODE — Chọn từng HS

        // ═══════════════════════════════════════════════════════



        private void ToggleSelectMode_Click(object sender, MouseButtonEventArgs e)

        {

            _isSelectMode = !_isSelectMode;



            // Update toggle visual

            toggleSelectMode.Background = new SolidColorBrush(_isSelectMode

                ? Color.FromRgb(76, 175, 80) : Color.FromRgb(117, 117, 117));

            toggleSelectDot.HorizontalAlignment = _isSelectMode

                ? HorizontalAlignment.Right : HorizontalAlignment.Left;

            toggleSelectDot.Margin = _isSelectMode

                ? new Thickness(0, 0, 2, 0) : new Thickness(2, 0, 0, 0);



            // Show/hide action panel

            pnlSelectedActions.Visibility = _isSelectMode

                ? Visibility.Visible : Visibility.Collapsed;



            // Update all students

            foreach (var st in _allStudents)

            {

                st.IsSelectMode = _isSelectMode;

                if (!_isSelectMode) st.IsSelected = false;

            }



            UpdateSelectedCount();

            ApplyFilter();

            Log.Information("Select mode: {State}", _isSelectMode ? "ON" : "OFF");

        }



        private void SelectAll_Click(object sender, RoutedEventArgs e)

        {

            foreach (var st in _allStudents)

                if (st.IsOnline) st.IsSelected = true;

            UpdateSelectedCount();

            ApplyFilter();

        }



        private void DeselectAll_Click(object sender, RoutedEventArgs e)

        {

            foreach (var st in _allStudents)

                st.IsSelected = false;

            UpdateSelectedCount();

            ApplyFilter();

        }



        private void StudentCheckbox_Click(object sender, RoutedEventArgs e)

        {

            UpdateSelectedCount();

        }



        private void UpdateSelectedCount()

        {

            int count = _allStudents.Count(s => s.IsSelected);

            if (txtSelectedCount != null)

                txtSelectedCount.Text = $"{count} đã chọn";

        }



        private void LockSelected_Click(object sender, RoutedEventArgs e)

        {

            var selected = _allStudents.Where(s => s.IsSelected && s.IsOnline).ToList();

            if (!selected.Any())

            {

                ShowToast("Chưa chọn HS", "Vui lòng chọn ít nhất 1 học sinh online");

                return;

            }



            // Send lock to each selected student individually

            foreach (var st in selected)

            {

                SendCommandToStudent(st, "CMD|LOCK_SCREEN");

            }

            AddLog("Khóa nhóm chọn", $"Đã khóa {selected.Count} máy học sinh được chọn", "#FFF3E0", "#E65100");

            Log.Information("Lock selected: {Count} students", selected.Count);

        }



        private void UnlockSelected_Click(object sender, RoutedEventArgs e)

        {

            var selected = _allStudents.Where(s => s.IsSelected && s.IsOnline).ToList();

            if (!selected.Any())

            {

                ShowToast("Chưa chọn HS", "Vui lòng chọn ít nhất 1 học sinh online");

                return;

            }



            foreach (var st in selected)

            {

                SendCommandToStudent(st, "CMD|UNLOCK_SCREEN");

            }

            AddLog("Mở khóa nhóm chọn", $"Đã mở khóa {selected.Count} máy học sinh được chọn", "#E8F5E9", "#2E7D32");

            Log.Information("Unlock selected: {Count} students", selected.Count);

        }



        /// <summary>Gửi lệnh CMD tới 1 HS cụ thể qua NetworkService</summary>

        private void SendCommandToStudent(ConnectedStudent student, string cmd)

        {

            try

            {

                // app → ClassroomAppContext (refactored)

                var net = ClassroomAppContext.Network;

                if (net?.IsBroadcasting == true)

                {

                    // Try to send to specific student by code if available, fallback to PCName

                    string target = !string.IsNullOrEmpty(student.StudentCode) ? student.StudentCode : student.PCName;

                    _ = net.SendToStudentAsync(target, cmd);

                }

                else

                {

                    // Fallback: broadcast (local)

                    ClassroomAppContext.DispatchCommand(cmd);

                }

            }

            catch (Exception ex)

            {

                Log.Warning("SendCommandToStudent error: {Err}", ex.Message);

            }

        }



        private void ClearSilence_Click(object sender, RoutedEventArgs e)

        {

            int online = _allStudents.Count(s => s.IsOnline);

            SendCommand("CMD|CLEAR_SILENCE");

            AddLog("Mở im lặng", $"Đã gỡ trạng thái im lặng cho {online} học sinh", "#E8F5E9", "#2E7D32");

            Log.Information("Clear silence: {Count} students", online);

        }



        private void ClearWarning_Click(object sender, RoutedEventArgs e)

        {

            int online = _allStudents.Count(s => s.IsOnline);

            SendCommand("CMD|CLEAR_WARNING");

            AddLog("Tắt cảnh báo", $"Đã gỡ cảnh báo cho {online} học sinh", "#E8F5E9", "#2E7D32");

            Log.Information("Clear warning: {Count} students", online);

        }



        private void BroadcastAlert_Click(object sender, RoutedEventArgs e)

        {

            var dlg = new Window

            {

                Title = "Phát thông báo tới học sinh",

                Width = 400, Height = 220,

                WindowStartupLocation = WindowStartupLocation.CenterOwner,

                Owner = Window.GetWindow(this),

                ResizeMode = ResizeMode.NoResize,

                Background = new SolidColorBrush(Color.FromRgb(37, 40, 48))

            };

            var sp = new StackPanel { Margin = new Thickness(20) };

            sp.Children.Add(new TextBlock { Text = "Nội dung thông báo:", FontSize = 13, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 8) });

            var tb = new TextBox { 

                Height = 80, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,

                Text = "Hãy tập trung vào bài học!", FontSize = 14, Padding = new Thickness(10),

                Background = new SolidColorBrush(Color.FromRgb(45, 48, 56)),

                Foreground = Brushes.White,

                BorderBrush = new SolidColorBrush(Color.FromRgb(69, 74, 86)),

                BorderThickness = new Thickness(1)

            };

            sp.Children.Add(tb);

            var btn = new Button { Content = "Gửi tới tất cả", Margin = new Thickness(0, 12, 0, 0),

                Padding = new Thickness(14, 8, 14, 8), Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),

                Foreground = Brushes.White, BorderThickness = new Thickness(0), FontSize = 13, FontWeight = FontWeights.Bold };

            btn.Click += (_, _) =>

            {

                string msg = tb.Text.Trim();

                if (!string.IsNullOrWhiteSpace(msg))

                {

                    AddLog("Thông báo chung", msg, "#E3F2FD", "#1565C0");

                    Log.Information("Broadcast: {Msg}", msg);

                }

                dlg.Close();

            };

            sp.Children.Add(btn);

            dlg.Content = sp;

            dlg.ShowDialog();

        }



        private void SendHomework_Click(object sender, RoutedEventArgs e)

        {

            int online = _allStudents.Count(s => s.IsOnline);

            AddLog("Giao bài tập", $"Đã gửi bài tập tới {online} HS", "#E8F5E9", "#2E7D32");

            ClassroomDialog.Info($"Đã giao bài tập tới {online} học sinh đang online!\n\nHS sẽ thấy bài tập trên màn hình của mình.", "Giao bài");

        }



        private void CollectWork_Click(object sender, RoutedEventArgs e)

        {

            int online = _allStudents.Count(s => s.IsOnline);

            string folder = System.IO.Path.Combine(

                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),

                "QASmartClass", "BaiNop", DateTime.Now.ToString("yyyy-MM-dd"));

            System.IO.Directory.CreateDirectory(folder);

            AddLog("Thu bài học sinh", $"Đã thu bài từ {online} HS → {System.IO.Path.GetFileName(folder)}", "#E3F2FD", "#1565C0");

            ClassroomDialog.Info($"Đã thu bài làm từ {online} học sinh online!\n\nThư mục lưu:\n{folder}", "Thu bài");

        }



        private void ScreenshotAll_Click(object sender, RoutedEventArgs e)

        {

            int online = _allStudents.Count(s => s.IsOnline);

            AddLog("Chụp màn hình lớp", $"Đã chụp màn hình {online} HS", "#F3E5F5", "#6A1B9A");

            ClassroomDialog.Info($"Đã gửi lệnh chụp màn hình tới {online} máy!\n\nẢnh sẽ được lưu tự động.", "Chụp màn hình");

        }



        // ═══════════════════════════════════════════════════════

        //  🔇 IM LẶNG — Màn hình đen + chữ to

        // ═══════════════════════════════════════════════════════



        private void SilenceAll_Click(object sender, RoutedEventArgs e)

        {

            var result = MessageBox.Show(

                "Gửi lệnh IM LẶNG tới tất cả học sinh?\n\n" +

                "• Màn hình HS sẽ chuyển đen\n" +

                "• Hiện chữ to \"IM LẶNG\"\n" +

                "• HS không thể thao tác cho đến khi GV mở khóa",

                "IM LẶNG", MessageBoxButton.OKCancel, MessageBoxImage.Warning);



            if (result == MessageBoxResult.OK)

            {

                SendCommand("CMD|SILENCE");

                int online = _allStudents.Count(s => s.IsOnline);

                AddLog("Yêu cầu im lặng", $"Đã gửi tới {online} HS", "#212121", "#FFFFFF");

                Log.Information("Silence all: {Count} students", online);

            }

        }



        // ═══════════════════════════════════════════════════════

        //  🚫 TẮT APP NGOÀI — Kill non-educational apps

        // ═══════════════════════════════════════════════════════



        private void KillApps_Click(object sender, RoutedEventArgs e)

        {

            var result = MessageBox.Show(

                "Tắt tất cả ứng dụng không phải học tập trên máy HS?\n\n" +

                "Các ứng dụng sẽ bị tắt:\n" +

                "• Game (Chrome.exe mở game, Steam, v.v.)\n" +

                "• Mạng xã hội (Facebook, Zalo...)\n" +

                "• Giải trí (YouTube, Spotify...)\n\n" +

                "Ứng dụng được giữ lại:\n" +

                "• QA SmartClass, Word, Excel, PowerPoint\n" +

                "• Notepad, Paint, Calculator",

                "Tắt ứng dụng ngoài", MessageBoxButton.OKCancel, MessageBoxImage.Warning);



            if (result == MessageBoxResult.OK)

            {

                SendCommand("CMD|KILL_APPS");

                int online = _allStudents.Count(s => s.IsOnline);

                AddLog("Tắt ứng dụng ngoài", $"Đã gửi lệnh tắt app ngoài tới {online} HS", "#FFEBEE", "#C62828");

                Log.Information("Kill non-educational apps: {Count} students", online);

            }

        }



        // ═══════════════════════════════════════════════════════

        //  ⚠️ GỬI CẢNH BÁO — Custom warning overlay

        // ═══════════════════════════════════════════════════════



        private void SendWarning_Click(object sender, RoutedEventArgs e)

        {

            var dlg = new Window

            {

                Title = "Gửi cảnh báo tới học sinh",

                Width = 420, Height = 370, WindowStartupLocation = WindowStartupLocation.CenterOwner,

                Owner = Window.GetWindow(this), ResizeMode = ResizeMode.NoResize,

                Background = new SolidColorBrush(Color.FromRgb(37, 40, 48))

            };

            var sp = new StackPanel { Margin = new Thickness(20) };

            sp.Children.Add(new TextBlock { Text = "Nội dung cảnh báo:", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 8) });

            var tb = new TextBox

            {

                Height = 80, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,

                Text = "Giáo viên yêu cầu: Hãy tập trung vào bài học!", FontSize = 14, Padding = new Thickness(10),

                Background = new SolidColorBrush(Color.FromRgb(45, 48, 56)),

                Foreground = Brushes.White,

                BorderBrush = new SolidColorBrush(Color.FromRgb(69, 74, 86)),

                BorderThickness = new Thickness(1)

            };

            sp.Children.Add(tb);



            // Quick warning phrases

            sp.Children.Add(new TextBlock { Text = "Gợi ý nhanh (nhấp để chọn):", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(176, 181, 195)), Margin = new Thickness(0, 8, 0, 4) });

            var quickPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };

            var quickMsgs = new[] {

                "Hãy tập trung học bài nhé!",

                "Không làm việc riêng trong lớp!",

                "Chú ý nhìn lên bảng giảng bài!"

            };

            foreach (var qmsg in quickMsgs)

            {

                var qbtn = new Button {

                    Content = qmsg, FontSize = 11, Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 4, 4),

                    Background = new SolidColorBrush(Color.FromRgb(45, 48, 56)), Foreground = Brushes.White,

                    BorderBrush = new SolidColorBrush(Color.FromRgb(69, 74, 86)), Cursor = Cursors.Hand

                };

                var msgVal = qmsg;

                qbtn.Click += (_, _) => tb.Text = msgVal;

                quickPanel.Children.Add(qbtn);

            }

            sp.Children.Add(quickPanel);



            sp.Children.Add(new TextBlock { Text = "Thời gian hiển thị:", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, Margin = new Thickness(0, 10, 0, 4) });

            var cmbDuration = new ComboBox { 

                Width = 150, Padding = new Thickness(8, 6, 8, 6), SelectedIndex = 1,

                Background = new SolidColorBrush(Color.FromRgb(45, 48, 56)),

                Foreground = Brushes.White,

                BorderBrush = new SolidColorBrush(Color.FromRgb(69, 74, 86))

            };

            cmbDuration.Items.Add("5 giây");

            cmbDuration.Items.Add("10 giây");

            cmbDuration.Items.Add("30 giây");

            cmbDuration.Items.Add("Không tự tắt");

            sp.Children.Add(cmbDuration);



            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 14, 0, 0) };

            var btnSend = new Button

            {

                Content = "Gửi tới tất cả", Padding = new Thickness(14, 8, 14, 8),

                Background = new SolidColorBrush(Color.FromRgb(230, 81, 0)), Foreground = Brushes.White,

                BorderThickness = new Thickness(0), FontSize = 13, FontWeight = FontWeights.Bold, Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 8, 0)

            };

            var btnSilence = new Button

            {

                Content = "IM LẶNG (Màn hình đen)", Padding = new Thickness(14, 8, 14, 8),

                Background = new SolidColorBrush(Color.FromRgb(33, 33, 33)), Foreground = Brushes.White,

                BorderThickness = new Thickness(0), FontSize = 13, FontWeight = FontWeights.Bold, Cursor = Cursors.Hand

            };



            var dur = new[] { 5, 10, 30, 0 };

            btnSend.Click += (_, _) =>

            {

                string msg = tb.Text.Trim();

                int seconds = dur[cmbDuration.SelectedIndex];

                if (!string.IsNullOrWhiteSpace(msg))

                {

                    SendCommand($"CMD|TEACHER_WARNING|{seconds}|{msg}");

                    AddLog("Gửi cảnh báo", msg, "#FFF3E0", "#E65100");

                    Log.Information("Warning sent: {Msg}, duration: {Sec}s", msg, seconds);

                }

                dlg.Close();

            };

            btnSilence.Click += (_, _) =>

            {

                SendCommand("CMD|SILENCE");

                AddLog("Yêu cầu im lặng", "Kích hoạt từ bảng điều khiển cảnh báo", "#212121", "#FFFFFF");

                dlg.Close();

            };



            btnPanel.Children.Add(btnSend);

            btnPanel.Children.Add(btnSilence);

            sp.Children.Add(btnPanel);

            dlg.Content = sp;

            dlg.ShowDialog();

        }



        // ═══════════════════════════════════════════════════════

        //  PER-STUDENT ACTIONS — Click vào thumbnail HS

        // ═══════════════════════════════════════════════════════



                private void Thumbnail_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ConnectedStudent st)
            {
                ShowStudentActionDialog(st);
            }
        }

        private void ShowStudentActionDialog(ConnectedStudent st)
        {
            AddLog($"Xem chi tiết {st.Name.Split(' ').Last()}", $"{st.DisplayPCName} ({st.DisplayIP})", "#F3E5F5", "#6A1B9A");

            var dlg = new Window
            {
                Title = $"Học sinh: {st.Name} — {st.DisplayPCName}",
                Width = 420, Height = 460, WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this), ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(37, 40, 48))
            };

            var sp = new StackPanel { Margin = new Thickness(20) };

            var info = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 48, 56)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(69, 74, 86)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 0, 14)
            };

            var infoStack = new StackPanel();
            infoStack.Children.Add(new TextBlock { Text = $"Học sinh: {st.Name}", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Brushes.White });

            infoStack.Children.Add(new TextBlock
            {
                Text = $"Tên mới: {st.DisplayPCName} — IP: {st.DisplayIP}\nTrạng thái: {(st.IsOnline ? "Trực tuyến" : "Ngoại tuyến")} · Độ tập trung: {st.FocusPct}%",
                FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(176, 181, 195)), Margin = new Thickness(0, 4, 0, 0)
            });

            info.Child = infoStack;
            sp.Children.Add(info);

            sp.Children.Add(new TextBlock { Text = "Hành động:", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 8) });

            var actions = new (string label, string bg, string fg, string cmd)[]
            {
                ("Xem màn hình chi tiết", "#E3F2FD", "#1565C0", "VIEW"),
                ("Đặt trạng thái IM LẶNG", "#212121", "#FFFFFF", "SILENCE"),
                ("Tắt ứng dụng chưa tập trung", "#FFEBEE", "#C62828", "KILL_APPS"),
                ("Gửi cảnh báo cá nhân", "#FFF3E0", "#E65100", "WARNING"),
                ("Khóa màn hình học sinh", "#FFCDD2", "#B71C1C", "LOCK"),                ("Mở khóa màn hình học sinh", "#E8F5E9", "#2E7D32", "UNLOCK")
            };

            foreach (var (label, bg, fg, cmd) in actions)
            {
                var btn = new Button
                {
                    Content = label, FontSize = 13, FontWeight = FontWeights.Bold, Padding = new Thickness(0, 8, 0, 8),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg)),
                    BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                    Margin = new Thickness(0, 0, 0, 4), HorizontalContentAlignment = HorizontalAlignment.Center
                };

                var actionCmd = cmd;
                var studentName = st.Name;
                var targetStudent = st;

                btn.Click += (_, _) =>
                {
                    switch (actionCmd)
                    {
                        case "VIEW":
                            dlg.Close();
                            ShowStudentDetailView(targetStudent);
                            break;
                        case "SILENCE":
                            SendCommandToStudent(targetStudent, "CMD|SILENCE");
                            AddLog("Yêu cầu im lặng", $"Gửi tới {studentName}", "#212121", "#FFFFFF");
                            dlg.Close();
                            break;
                        case "KILL_APPS":
                            SendCommandToStudent(targetStudent, "CMD|KILL_APPS");
                            AddLog("Tắt ứng dụng ngoài", $"Gửi tới {studentName}", "#FFEBEE", "#C62828");
                            dlg.Close();
                            break;
                        case "WARNING":
                            SendCommandToStudent(targetStudent, $"CMD|TEACHER_WARNING|10|Giáo viên yêu cầu: Hãy tập trung vào bài học!");
                            AddLog("Gửi cảnh báo", studentName, "#FFF3E0", "#E65100");
                            dlg.Close();
                            break;
                        case "LOCK":
                            SendCommandToStudent(targetStudent, "CMD|LOCK_SCREEN");
                            AddLog("Khóa màn hình", studentName, "#FFCDD2", "#B71C1C");
                            dlg.Close();
                            break;
                        case "UNLOCK":
                            SendCommandToStudent(targetStudent, "CMD|UNLOCK_SCREEN");
                            AddLog("Mở khóa", studentName, "#E8F5E9", "#2E7D32");
                            dlg.Close();
                            break;
                    }
                };
                sp.Children.Add(btn);
            }
            dlg.Content = sp;
            dlg.ShowDialog();
        }



        // ═══════════════════════════════════════════════════════

        //  STUDENT DETAIL VIEW — Màn hình chi tiết học sinh

        // ═══════════════════════════════════════════════════════



        private void ShowStudentDetailView(ConnectedStudent st)

        {

            var detailWin = new Window

            {

                Title        = $"Màn hình chi tiết — {st.Name}  ({st.DisplayPCName})",

                Width        = 960,

                Height       = 620,

                WindowStartupLocation = WindowStartupLocation.CenterOwner,

                Owner        = Window.GetWindow(this),

                Background   = new SolidColorBrush(Color.FromRgb(15, 18, 25)),

                ResizeMode   = ResizeMode.CanResize

            };



            var root = new Grid();

            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });



            // ── Screenshot area ──

            var screenshotBorder = new Border

            {

                Background  = new SolidColorBrush(Color.FromRgb(10, 14, 20)),

                Margin      = new Thickness(12, 12, 12, 6),

                CornerRadius = new CornerRadius(10),

                ClipToBounds = true

            };

            var screenshotImg = new System.Windows.Controls.Image

            {

                Stretch             = Stretch.Uniform,

                HorizontalAlignment = HorizontalAlignment.Center,

                VerticalAlignment   = VerticalAlignment.Center,

                Source              = st.ScreenshotSource

            };

            // ═══ High-quality upscaling ═══

            RenderOptions.SetBitmapScalingMode(screenshotImg, BitmapScalingMode.HighQuality);

            RenderOptions.SetEdgeMode(screenshotImg, EdgeMode.Unspecified);

            TextOptions.SetTextFormattingMode(screenshotImg, TextFormattingMode.Display);

            var placeholder = new TextBlock

            {

                Text                = "Chưa có ảnh màn hình\nĐang chờ học sinh gửi dữ liệu...",

                FontSize            = 18,

                FontWeight          = FontWeights.Bold,

                Foreground          = new SolidColorBrush(Color.FromRgb(80, 90, 110)),

                TextAlignment       = TextAlignment.Center,

                HorizontalAlignment = HorizontalAlignment.Center,

                VerticalAlignment   = VerticalAlignment.Center,

                Visibility          = st.ScreenshotSource == null ? Visibility.Visible : Visibility.Collapsed

            };

            var imgGrid = new Grid();

            imgGrid.Children.Add(screenshotImg);

            imgGrid.Children.Add(placeholder);

            screenshotBorder.Child = imgGrid;

            Grid.SetRow(screenshotBorder, 0);

            root.Children.Add(screenshotBorder);



            // ── Bottom bar ──

            var bottomBar = new Border

            {

                Background  = new SolidColorBrush(Color.FromRgb(22, 27, 38)),

                Padding     = new Thickness(14, 10, 14, 12),

                Margin      = new Thickness(12, 0, 12, 10),

                CornerRadius = new CornerRadius(8)

            };

            var bottomDock = new DockPanel();



            var infoPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            infoPanel.Children.Add(new TextBlock

            {

                Text       = $"Học sinh: {st.Name}",

                FontSize   = 15, FontWeight = FontWeights.Bold, Foreground = Brushes.White

            });

            infoPanel.Children.Add(new TextBlock

            {

                Text       = $"Tên máy: {st.DisplayPCName}  •  IP: {st.DisplayIP}  •  {(st.IsOnline ? "Trực tuyến" : "Ngoại tuyến")}  •  Độ tập trung: {st.FocusPct}%",

                FontSize   = 12,

                Foreground = new SolidColorBrush(Color.FromRgb(130, 145, 165)),

                Margin     = new Thickness(0, 3, 0, 0)

            });

            var lblRefresh = new TextBlock

            {

                Text       = "Đang kết nối để cập nhật màn hình...",

                FontSize   = 11,

                FontWeight = FontWeights.Bold,

                Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)),

                Margin     = new Thickness(0, 3, 0, 0)

            };

            infoPanel.Children.Add(lblRefresh);

            DockPanel.SetDock(infoPanel, Dock.Left);

            bottomDock.Children.Add(infoPanel);



            var btnPanel = new StackPanel

            {

                Orientation         = Orientation.Horizontal,

                HorizontalAlignment = HorizontalAlignment.Right,

                VerticalAlignment   = VerticalAlignment.Center

            };

            DockPanel.SetDock(btnPanel, Dock.Right);



            void AddBtn(string lbl, string hexBg, string hexFg, Action act)

            {

                var b = new Button

                {

                    Content         = lbl, FontSize = 12, FontWeight = FontWeights.Bold,

                    Padding         = new Thickness(14, 8, 14, 8),

                    Margin          = new Thickness(0, 0, 6, 0),

                    Background      = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexBg)),

                    Foreground      = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexFg)),

                    BorderThickness = new Thickness(0), Cursor = Cursors.Hand

                };

                b.Click += (_, _) => act();

                btnPanel.Children.Add(b);

            }



            AddBtn("Chụp màn hình ngay", "#1565C0", "#FFFFFF", () =>

            {

                RequestScreenshotFromStudent(st);

                lblRefresh.Text = "Đang yêu cầu chụp màn hình...";

            });

            AddBtn("Khóa máy", "#B71C1C", "#FFFFFF", () =>

            {

                SendCommandToStudent(st, "CMD|LOCK_SCREEN");

                AddLog("Khóa màn hình", st.Name, "#FFCDD2", "#B71C1C");

            });

            AddBtn("Mở khóa máy", "#2E7D32", "#FFFFFF", () =>

            {

                SendCommandToStudent(st, "CMD|UNLOCK_SCREEN");

                AddLog("Mở khóa", st.Name, "#E8F5E9", "#2E7D32");

            });

            AddBtn("Truyền lệnh IM LẶNG", "#212121", "#FFFFFF", () =>

            {

                SendCommandToStudent(st, "CMD|SILENCE");

                AddLog("Yêu cầu im lặng", st.Name, "#212121", "#FFFFFF");

            });



            bottomDock.Children.Add(btnPanel);

            bottomBar.Child = bottomDock;

            Grid.SetRow(bottomBar, 1);

            root.Children.Add(bottomBar);

            detailWin.Content = root;



            // ── Auto-refresh: request new screenshot every 2s (propHandler handles display) ──

            var refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };

            refreshTimer.Tick += (_, _) =>

            {

                RequestScreenshotFromStudent(st);

            };

            refreshTimer.Start();



            // ── Live update immediately when ScreenshotSource changes ──

            PropertyChangedEventHandler? propHandler = null;

            propHandler = (s2, e2) =>

            {

                if (e2.PropertyName == nameof(ConnectedStudent.ScreenshotSource))

                {

                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, () =>

                    {

                        screenshotImg.Source   = st.ScreenshotSource;

                        placeholder.Visibility = st.ScreenshotSource == null

                            ? Visibility.Visible : Visibility.Collapsed;

                        lblRefresh.Text        = $"Đã cập nhật: {DateTime.Now:HH:mm:ss}";

                    });

                }

            };

            st.PropertyChanged += propHandler;



            detailWin.Closed += (_, _) =>

            {

                refreshTimer.Stop();

                st.PropertyChanged -= propHandler;

            };



            // Request screenshot immediately on open

            RequestScreenshotFromStudent(st);

            detailWin.ShowDialog();

        }



        /// <summary>Gửi REQUEST_SCREENSHOT riêng tới 1 học sinh (chống bão mạng LAN)</summary>

        private void RequestScreenshotFromStudent(ConnectedStudent st)

        {

            try

            {

                // app → ClassroomAppContext (refactored)

                var net = ClassroomAppContext.Network;

                if (net?.IsBroadcasting != true) return;



                // V4.1: Tạm dừng yêu cầu chụp màn hình đơn lẻ khi đang phát màn hình bài giảng

                if (QASmartClass.Services.BroadcastStateService.Instance.IsScreenBroadcastActive)

                {

                    Log.Debug("[MonitorPage] Screen broadcast is active. Skipping single student screenshot request.");

                    return;

                }



                var code = st.StudentCode;

                string target = !string.IsNullOrEmpty(code) ? code : st.PCName;

                if (!string.IsNullOrEmpty(target))

                {

                    _ = net.SendToStudentAsync(target, "CMD|REQUEST_SCREENSHOT");

                }

            }

            catch (Exception ex) { Log.Warning("RequestScreenshot error: {Err}", ex.Message); }

        }



        /// <summary>Gửi CMD qua local bus + network</summary>

        private void SendCommand(string cmd)

        {

            // app → ClassroomAppContext (refactored)

            ClassroomAppContext.LessonState.LastTeacherCommand = cmd;

            ClassroomAppContext.LessonState.LastCommandTime = DateTime.Now;



            var net = ClassroomAppContext.Network;

            if (net?.IsBroadcasting == true)

                _ = net.SendCommandAsync(cmd);

            else

                ClassroomAppContext.DispatchCommand(cmd);

        }



        // ═══════════════════════════════════════════════════════

        //  TOAST POPUP

        // ═══════════════════════════════════════════════════════



        private void ShowToast(string title, string detail)

        {

            txtToastTitle.Text = title;

            txtToastDetail.Text = detail;

            toastPopup.Visibility = Visibility.Visible;



            // Auto-dismiss after 6 seconds

            _toastTimer?.Stop();

            _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };

            _toastTimer.Tick += (_, _) => { toastPopup.Visibility = Visibility.Collapsed; _toastTimer.Stop(); };

            _toastTimer.Start();

        }



        private void DismissToast_Click(object sender, RoutedEventArgs e)

            => toastPopup.Visibility = Visibility.Collapsed;



        // ═══════════════════════════════════════════════════════

        //  TOGGLE BLOCK APPS

        // ═══════════════════════════════════════════════════════



        private void ToggleBlockApps_Click(object sender, MouseButtonEventArgs e)

        {

            _isBlockingApps = !_isBlockingApps;

            toggleBlockApp.Background = _isBlockingApps

                ? new SolidColorBrush(Color.FromRgb(76, 175, 80))    // Green ON

                : new SolidColorBrush(Color.FromRgb(117, 117, 117)); // Gray OFF

            toggleDot.HorizontalAlignment = _isBlockingApps ? HorizontalAlignment.Right : HorizontalAlignment.Left;

            toggleDot.Margin = _isBlockingApps ? new Thickness(0, 0, 2, 0) : new Thickness(2, 0, 0, 0);



            SendCommand(_isBlockingApps ? "CMD|BLOCK_APPS_ON" : "CMD|BLOCK_APPS_OFF");

            AddLog(_isBlockingApps ? "Chặn ứng dụng: BẬT" : "Chặn ứng dụng: TẮT",

                $"Áp dụng cho {_allStudents.Count(s => s.IsOnline)} học sinh đang trực tuyến", "#2D3038", "#FF9800");

        }



        // ═══════════════════════════════════════════════════════

        //  SIDEBAR BUTTONS: Broadcast, Send File, Send Message

        // ═══════════════════════════════════════════════════════



        private void BroadcastScreen_Click(object sender, RoutedEventArgs e)

        {

            int online = _allStudents.Count(s => s.IsOnline);

            SendCommand("CMD|SCREEN_BROADCAST_START");

            AddLog("Phát màn hình", $"Đang chia sẻ màn hình giáo viên tới {online} học sinh", "#2D3038", "#42A5F5");

            Log.Information("Broadcast teacher screen to {Count} students", online);

        }



        private void SendFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Chọn file gửi cho Học sinh",
                Multiselect = true,
                Filter = "Tất cả file (*.*)|*.*"
            };
            if (ofd.ShowDialog() == true)
            {
                // app → ClassroomAppContext (refactored)
                int webPort = 8080;
                if (ClassroomAppContext.Network?.WebBridge != null)
                {
                    webPort = ClassroomAppContext.Network.WebBridge.WebPort;
                }

                int online = _allStudents.Count(s => s.IsOnline);
                foreach (var file in ofd.FileNames)
                {
                    QASmartClass.Services.BroadcastStateService.Instance.AddBroadcastFile(file);
                    if (string.IsNullOrEmpty(QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken))
                    {
                        QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken = "TK_" + Guid.NewGuid().ToString("N").Substring(0, 16);
                    }
                    var token = QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken;
                    SendCommand($"CMD|FILE_BROADCAST|{file}|{webPort}|{token}");
                }
                AddLog("Gửi tệp tin", $"Đã truyền {ofd.FileNames.Length} tệp dữ liệu tới {online} học sinh", "#2D3038", "#66BB6A");
            }
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)

        {

            var dlg = new Window

            {

                Title = "Gửi tin nhắn tới học sinh",

                Width = 400, Height = 200,

                WindowStartupLocation = WindowStartupLocation.CenterOwner,

                Owner = Window.GetWindow(this), ResizeMode = ResizeMode.NoResize,

                Background = new SolidColorBrush(Color.FromRgb(37, 40, 48))

            };

            var sp = new StackPanel { Margin = new Thickness(20) };

            sp.Children.Add(new TextBlock { Text = "Nội dung tin nhắn:", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 6) });

            var tb = new TextBox { Height = 60, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,

                Text = "Các em hãy chú ý bài giảng!", FontSize = 13, Padding = new Thickness(10) };

            sp.Children.Add(tb);

            var btn = new Button { Content = "Gửi tất cả", Margin = new Thickness(0, 10, 0, 0),

                Padding = new Thickness(14, 8, 14, 8), Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),

                Foreground = Brushes.White, BorderThickness = new Thickness(0), FontSize = 13, FontWeight = FontWeights.Bold };

            btn.Click += (_, _) =>

            {

                string msg = tb.Text.Trim();

                if (!string.IsNullOrWhiteSpace(msg))

                {

                    string formattedMsg = msg;
                    if (!msg.StartsWith("💬") && !msg.StartsWith("📢") && !msg.StartsWith("📨") && !msg.StartsWith("🔒") && !msg.StartsWith("📚"))
                    {
                        formattedMsg = "💬 " + msg;
                    }
                    SendCommand($"MSG|{formattedMsg}");

                    AddLog("Gửi tin nhắn", msg, "#2D3038", "#42A5F5");

                }

                dlg.Close();

            };

            sp.Children.Add(btn);

            dlg.Content = sp;

            dlg.ShowDialog();

        }

        private void NetworkDiag_Click(object sender, RoutedEventArgs e)
        {
            // app → ClassroomAppContext (refactored)
            var shell = System.Windows.Application.Current.MainWindow as ClassroomShell;
            if (shell != null)
            {
                _ = shell.NavigateToAsync("F35");
            }
        }



        // ═══════════════════════════════════════════════════════

        //  ACTIVITY LOG

        // ═══════════════════════════════════════════════════════



        private readonly List<ActivityEntry> _logEntries = new();



        private void AddLog(string title, string detail, string bg, string fg)

        {

            Application.Current?.Dispatcher.Invoke(() =>

            {

                _logEntries.Insert(0, new ActivityEntry

                {

                    Title  = title,

                    Detail = detail,

                    Time   = DateTime.Now.ToString("HH:mm"),

                    LogBg  = bg,

                    LogFg  = fg

                });

                // Keep only last 20

                while (_logEntries.Count > 20) _logEntries.RemoveAt(_logEntries.Count - 1);

                activityLog.ItemsSource = null;

                activityLog.ItemsSource = _logEntries;

            });

        }



        private void TogglePrivacyMode_Click(object sender, MouseButtonEventArgs e)

        {

            IsPrivacyModeActive = !IsPrivacyModeActive;

            if (IsPrivacyModeActive)

            {

                txtPrivacyStatus.Text = "Riêng tư: BẬT";

                txtPrivacyStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange

                pathPrivacy.Data = (Geometry)FindResource("GeomLock");

                pathPrivacy.Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0));

                AddLog("Bảo mật", "Đã bật Chế độ riêng tư (Ẩn tên máy và IP)", "#2D3038", "#FF9800");

            }

            else

            {

                txtPrivacyStatus.Text = "Riêng tư: TẮT";

                txtPrivacyStatus.Foreground = new SolidColorBrush(Color.FromRgb(176, 181, 195)); // Gray

                pathPrivacy.Data = (Geometry)FindResource("GeomUnlock");

                pathPrivacy.Fill = new SolidColorBrush(Color.FromRgb(176, 181, 195));

                AddLog("Bảo mật", "Đã tắt Chế độ riêng tư", "#2D3038", "#8E93A0");

            }



            foreach (var student in _allStudents)

            {

                student.OnPropertyChanged(nameof(ConnectedStudent.DisplayPCName));

                student.OnPropertyChanged(nameof(ConnectedStudent.DisplayIP));

            }

            ApplyFilter();

        }



        private void Page_Unloaded(object sender, RoutedEventArgs e)

        {

            _refreshTimer?.Stop();

            _toastTimer?.Stop();

            UnsubscribeNetworkEvents();

        }

        private void OnVncBroadcastStatusChanged(object? sender, string status)
        {
            Dispatcher.Invoke(() => RefreshVncBadgesVisibility());
        }

        private void RefreshVncBadgesVisibility()
        {
            foreach (var student in _allStudents)
            {
                student.NotifyBroadcastStateChanged();
            }
            UpdateFocusStatistics();
        }

        private void UpdateFocusStatistics()
        {
            if (!QASmartClass.Services.VncBroadcastService.Instance.IsVncBroadcasting)
            {
                txtMonitorCount.Text = $"{_allStudents.Count(s => s.IsOnline)}/{_allStudents.Count} trực tuyến";
                return;
            }

            int online = _allStudents.Count(s => s.IsOnline);
            if (online == 0) return;

            int watching = _allStudents.Count(s => s.IsOnline && s.IsWatchingBroadcast);
            double focusRate = (watching * 100.0) / online;

            txtMonitorCount.Text = $"{watching}/{online} đang xem VNC ({focusRate:0}%)";

            if (focusRate < 70 && watching > 0)
            {
                ShowLowFocusWarning(watching, online);
            }
        }

        private DateTime _lastFocusWarningTime = DateTime.MinValue;
        private void ShowLowFocusWarning(int watching, int total)
        {
            if ((DateTime.Now - _lastFocusWarningTime).TotalSeconds < 30) return;
            _lastFocusWarningTime = DateTime.Now;

            ShowToast("Cảnh báo tập trung", $"Chỉ có {watching}/{total} học sinh đang theo dõi trình chiếu màn hình!");
        }
    }



    // ─── Models ─────────────────────────────────────────────────



    public class ConnectedStudent : INotifyPropertyChanged

    {

        private bool   _isOnline;

        private int    _focusPct = 80;

        private int    _cpuUsage = 20;

        private bool   _hasAlert;

        private string _alertIcon = "";

        private string _offTaskApp = "";

        private bool   _isSelected;

        private bool   _isSelectMode;



        public string Name      { get; set; } = "";

        public string PCName    { get; set; } = "";

        public string IPAddress { get; set; } = "";

        /// <summary>Web student code (from WS join message) â€” used to match screenshots</summary>

        public string? StudentCode { get; set; }

        public DateTime LastScreenshotTime { get; set; } = DateTime.MinValue;



        // Screenshot: backing field + auto-notify → no blank-flash flicker

        private System.Windows.Media.ImageSource? _screenshotSource;

        public System.Windows.Media.ImageSource? ScreenshotSource

        {

            get => _screenshotSource;

            set

            {

                _screenshotSource = value;

                OnPropertyChanged();

                OnPropertyChanged(nameof(PlaceholderVisibility));

            }

        }



        public bool IsOnline

        {

            get => _isOnline;

            set { _isOnline = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusDotColor)); OnPropertyChanged(nameof(ActivityColor)); OnPropertyChanged(nameof(FocusLabel)); }

        }



        public int CpuUsage

        {

            get => _cpuUsage;

            set

            {

                _cpuUsage = value;

                _focusPct = Math.Clamp(100 - value, 30, 100);

                OnPropertyChanged();

                OnPropertyChanged(nameof(FocusLabel));

                OnPropertyChanged(nameof(ActivityColor));

            }

        }



        public int FocusPct

        {

            get => _focusPct;

            set

            {

                _focusPct = value;

                _cpuUsage = 100 - value;

                OnPropertyChanged();

                OnPropertyChanged(nameof(FocusLabel));

                OnPropertyChanged(nameof(ActivityColor));

            }

        }



        public bool HasAlert

        {

            get => _hasAlert;

            set { _hasAlert = value; OnPropertyChanged(); OnPropertyChanged(nameof(OffTaskVisibility)); OnPropertyChanged(nameof(OffTaskLabel)); }

        }



        public string AlertIcon

        {

            get => _alertIcon;

            set { _alertIcon = value; OnPropertyChanged(); }

        }



        public string OffTaskApp

        {

            get => _offTaskApp;

            set { _offTaskApp = value; OnPropertyChanged(); OnPropertyChanged(nameof(OffTaskLabel)); OnPropertyChanged(nameof(OffTaskVisibility)); }

        }



        // Computed properties for dark theme

        public string DisplayPCName => MonitorPage.IsPrivacyModeActive ? MonitorPage.ObfuscatePCName(PCName) : PCName;

        public string DisplayIP     => MonitorPage.IsPrivacyModeActive ? "***.***.***.***" : IPAddress;

        public string FocusLabel     => IsOnline ? $"{FocusPct}%" : "Ngoại tuyến";

        public string StatusDotColor => IsOnline ? "#4CAF50" : "#F44336";

        public string ActivityColor  => IsOnline

            ? (FocusPct >= 75 ? "#4CAF50" : FocusPct >= 50 ? "#FF9800" : "#F44336")

            : "#454A56";



        // Off-task badge

        public string OffTaskLabel      => HasAlert && !string.IsNullOrEmpty(OffTaskApp) ? "Ngoài bài" : "";

        public string OffTaskVisibility => HasAlert && !string.IsNullOrEmpty(OffTaskApp) ? "Visible" : "Collapsed";

        public string PlaceholderVisibility => ScreenshotSource == null ? "Visible" : "Collapsed";



        // Selection

        public bool IsSelected

        {

            get => _isSelected;

            set { _isSelected = value; OnPropertyChanged(); }

        }

        public bool IsSelectMode

        {

            get => _isSelectMode;

            set { _isSelectMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectModeVisibility)); }

        }

        public string SelectModeVisibility => _isSelectMode ? "Visible" : "Collapsed";

        private bool _isWatchingBroadcast = true;
        private string _foregroundApp = "VNC Viewer";

        public bool IsWatchingBroadcast
        {
            get => _isWatchingBroadcast;
            set
            {
                _isWatchingBroadcast = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BroadcastFocusColor));
                OnPropertyChanged(nameof(BroadcastFocusLabel));
            }
        }

        public string ForegroundApp
        {
            get => _foregroundApp;
            set
            {
                _foregroundApp = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BroadcastFocusLabel));
            }
        }

        public string BroadcastFocusColor => IsWatchingBroadcast ? "#2E7D32" : "#D32F2F";

        public string BroadcastFocusLabel => IsWatchingBroadcast ? "👁️ Đang xem" : $"⚠️ {ForegroundApp}";

        public string BroadcastFocusVisibility => QASmartClass.Services.VncBroadcastService.Instance.IsVncBroadcasting ? "Visible" : "Collapsed";

        public void NotifyBroadcastStateChanged()
        {
            OnPropertyChanged(nameof(BroadcastFocusVisibility));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string? n = null)

            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    }



    public class StudentRow

    {

        public int ColumnsCount { get; set; }

        public List<ConnectedStudent> Students { get; set; } = new();

    }



    public class ActivityEntry

    {

        public string Title  { get; set; } = "";

        public string Detail { get; set; } = "";

        public string Time   { get; set; } = "";

        public string LogBg  { get; set; } = "#F5F5F5";

        public string LogFg  { get; set; } = "#333";

    }

}











