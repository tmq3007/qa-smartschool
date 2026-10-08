using System;
using System.Linq;
using System.Windows;
using QASmartTouch.Services;
using QASmartTouch.Services.VersionManagement;
using QASmartTouch.Forms;
using QASmartClass.Shared;
using QASmartClass.Classroom.Views;
using QASmartClass.Classroom.Services;
using QASmartClass.StudentClient.Views;
using QASmartClass.Data;
using Serilog;
using Microsoft.EntityFrameworkCore;

namespace QASmartTouch
{
    public partial class App : Application
    {
        // ═══ QA SMART CLASS: Core Services (Singletons) ═══
        public ModeService ModeService { get; } = new ModeService();
        public AppDbContext Database { get; private set; } = null!;
        public NetworkDiscoveryService NetworkService { get; } = new NetworkDiscoveryService();
        public QASmartClass.StudentClient.Services.StudentNetworkClient StudentNetwork { get; } = new();

        // ═══ File Transfer Services (TCP 29879) ═══
        private FileTransferService? _fileTransferService;
        public FileTransferService FileTransfer => _fileTransferService ??= new FileTransferService(NetworkService);
        public QASmartClass.StudentClient.Services.StudentFileTransfer StudentFileTransfer { get; } = new();

        // ═══ Device Memory Service — ghi nhớ thiết bị trong lớp ═══
        private DeviceMemoryService? _deviceMemoryService;
        public DeviceMemoryService DeviceMemory => _deviceMemoryService ??= new DeviceMemoryService(Database);

        // ═══ Class Roster Service — quản lý danh sách lớp (phòng STEM dùng chung) ═══
        private ClassRosterService? _classRosterService;
        public ClassRosterService ClassRoster => _classRosterService ??= new ClassRosterService(Database);

        // ═══ Classroom Session Service — quản lý phiên lớp học (đồng bộ kết nối) ═══
        private ClassroomSessionService? _classroomSessionService;
        public ClassroomSessionService ClassroomSession => _classroomSessionService ??= new ClassroomSessionService(NetworkService, Database);

        // ═══ User Role Service — vai trò sử dụng trong hệ sinh thái QA Smart School ═══
        public UserRoleService UserRoleService { get; } = new UserRoleService();

        // ═══ LOCAL COMMAND BUS — đồng bộ GV → HS trên cùng 1 máy ═══
        /// <summary>
        /// Event fired when teacher sends a command locally (same machine).
        /// StudentShell listens to this for instant sync without TCP.
        /// </summary>
        public event EventHandler<string>? LocalCommandReceived;
        public void RaiseLocalCommand(string command)
        {
            LocalCommandReceived?.Invoke(this, command);
            Log.Debug("Local command bus: {Cmd}", command);
        }

        // ─── Group State (giữ tại App — chưa đủ lớn để tách) ───
        private readonly object _stateLock = new();
        public System.Collections.Generic.List<QASmartClass.Classroom.Views.GroupVm> CurrentGroups { get { lock (_stateLock) return _currentGroups; } set { lock (_stateLock) _currentGroups = value; } }
        private System.Collections.Generic.List<QASmartClass.Classroom.Views.GroupVm> _currentGroups = new();

        // ─── Service Shortcuts ───
        public static QASmartClass.Services.LessonStateService LessonState => QASmartClass.Services.LessonStateService.Instance;
        public static QASmartClass.Services.BroadcastStateService BroadcastState => QASmartClass.Services.BroadcastStateService.Instance;
        public static QASmartClass.Services.ClassControlService ClassControl => QASmartClass.Services.ClassControlService.Instance;
        public static QASmartClass.Services.AssessmentStateService AssessmentState => QASmartClass.Services.AssessmentStateService.Instance;
        public static QASmartClass.Services.FocusStateService FocusState => QASmartClass.Services.FocusStateService.Instance;

        internal QASmartClass.Shared.FloatingModeBar? _floatingModeBar;
        internal ClassroomShell? _classroomShell;
        internal Form2_MainDashboard? _whiteboardShell;
        internal StudentShell? _studentShell;
        // [LOI_VID_50] Cờ cho phép đóng Window thật khi thoát ứng dụng
        internal static bool _isAppShuttingDown = false;
        internal System.Windows.Window? _staffShell;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // === QC_4.2_TOUCH_SLIDER: Tối ưu cảm ứng 1 chạm cho thanh trượt Slider toàn hệ thống ===
            QASmartTouch.Helpers.TouchSliderHelper.Initialize();

            // === UPGRADE_06: CommandLine Auto-Fix Firewall ===
            if (e.Args != null && System.Linq.Enumerable.Contains(e.Args, "--configure-firewall"))
            {
                Log.Information("[Firewall] Khởi chạy tác vụ cấu hình tường lửa đặc quyền.");
                var success = System.Threading.Tasks.Task.Run(async () => await QASmartClass.Services.NetworkDiagnosticsService.RunFirewallAutoFixAsync()).Result;
                Log.Information("[Firewall] Kết quả tự động cấu hình: {Result}", success);
                Environment.Exit(success ? 0 : 1);
            }

            // === UPGRADE_05: Fast Win32 OS Mutex & Async Background Tasks ===
            EnforceSingleInstance();

            // Run thematic image sync in background to avoid blocking early UI startup
            System.Threading.Tasks.Task.Run(() => SyncThematicImages());

            // Định tuyến đường dẫn dữ liệu từ db_path_config.json nếu có
            try
            {
                string defaultSettingsDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QASmartClass", "Settings");
                string pathConfigFile = System.IO.Path.Combine(defaultSettingsDir, "db_path_config.json");
                if (System.IO.File.Exists(pathConfigFile))
                {
                    var json = System.IO.File.ReadAllText(pathConfigFile);
                    using (var doc = System.Text.Json.JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("CustomDbPath", out var prop))
                        {
                            string customPath = prop.GetString() ?? "";
                            if (!string.IsNullOrEmpty(customPath) && System.IO.Directory.Exists(customPath))
                            {
                                QASmartClass.Services.AppPaths.DataDirOverride = customPath;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to load db_path_config.json at early startup: {Err}", ex.Message);
            }

            // Định tuyến đường dẫn dữ liệu nếu có tham số dòng lệnh --data-dir=
            var cmdArgs = Environment.GetCommandLineArgs();
            var dataDirArg = cmdArgs.FirstOrDefault(a => a.StartsWith("--data-dir=", StringComparison.OrdinalIgnoreCase));
            if (dataDirArg != null)
            {
                var dir = dataDirArg.Substring("--data-dir=".Length).Trim('"');
                if (!string.IsNullOrEmpty(dir))
                {
                    QASmartClass.Services.AppPaths.DataDirOverride = dir;
                }
            }

            // Nạp nhanh cấu hình hệ thống đồng bộ để lấy ActiveUserRole
            try
            {
                AppSettings.Load();
                SettingsManager.Instance.LoadSettings();
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to load settings at early startup: {Err}", ex.Message);
            }

            string activeRole = AppSettings.ActiveUserRole;
            var args = Environment.GetCommandLineArgs();
            bool isTestMode = args.Any(a => a.Equals("--test", StringComparison.OrdinalIgnoreCase));
            bool isStudentMode = args.Any(a => a.Equals("--student", StringComparison.OrdinalIgnoreCase));

            // Nếu phân hệ là All (mặc định) và không có tham số dòng lệnh override
            if (!isTestMode && !isStudentMode && (string.IsNullOrEmpty(activeRole) || activeRole.Equals("All", StringComparison.OrdinalIgnoreCase)))
            {
                // Hiển thị ngay màn hình chọn đăng nhập nhưng KHÔNG kích hoạt overlay block toàn màn hình
                var loginSelection = new Form0_LoginSelection(true);
                loginSelection.Show();

                // Chạy ngầm tác vụ khởi tạo
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        InitializeCoreServices(e, (status, progress) =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                // Cập nhật progress bar nhúng trong Form thay vì block overlay
                                loginSelection.UpdateLoadingStatus(status, progress);
                            });
                        });

                        // Khởi tạo Database thực tế ở Background Thread để tránh block Main UI
                        var tempDb = new AppDbContext();
                        QASmartClass.Services.AppServices.Initialize(tempDb);
                        QASmartTouch.Services.AppSettings.LoadFromDatabase(tempDb);
                        QASmartClass.Helpers.OfflineSyncManager.Start();

                        // Chỉ invoke UI khi đã sẵn sàng
                        await Dispatcher.InvokeAsync(() =>
                        {
                            Database = tempDb;
                            loginSelection.HideLoadingOverlay();
                            InitializeModeAndRoleAfterLoading();
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Critical error during startup initialization");
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("Đã xảy ra lỗi nghiêm trọng khi khởi động phần mềm: " + ex.Message,
                                "Lỗi khởi động", MessageBoxButton.OK, MessageBoxImage.Error);
                            Shutdown();
                        });
                    }
                });
            }
            else
            {
                // Sử dụng Splash Screen truyền thống cho các trường hợp khác
                var splash = new Forms.SplashScreen();
                splash.Show();
                splash.UpdateStatus("Khởi tạo hệ thống...", 5);

                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        InitializeCoreServices(e, (status, progress) =>
                        {
                            splash.UpdateStatus(status, progress);
                        });

                        await Dispatcher.InvokeAsync(() =>
                        {
                            Database = new AppDbContext();
                            QASmartClass.Services.AppServices.Initialize(Database);
                            QASmartTouch.Services.AppSettings.LoadFromDatabase(Database);
                            QASmartClass.Helpers.OfflineSyncManager.Start();

                            InitializeModeAndRole(splash);
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Critical error during startup initialization");
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("Đã xảy ra lỗi nghiêm trọng khi khởi động phần mềm: " + ex.Message,
                                "Lỗi khởi động", MessageBoxButton.OK, MessageBoxImage.Error);
                            Shutdown();
                        });
                    }
                });
            }
        }

        // ═══════════════════════════════════════════════════════
        //  [LOI_VID_49] TRANSITION OVERLAY
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// [LOI_VID_49] Overlay toàn màn hình hiển thị Loading khi chuyển mode.
        /// Lazy-initialized khi cần.
        /// </summary>
        private QASmartClass.Shared.TransitionOverlay? _transitionOverlay;

        /// <summary>
        /// [LOI_VID_49] Lấy hoặc tạo TransitionOverlay instance.
        /// Overlay được nhúng vào Window hiện đang active.
        /// </summary>
        private QASmartClass.Shared.TransitionOverlay GetOrCreateTransitionOverlay()
        {
            if (_transitionOverlay == null)
            {
                _transitionOverlay = new QASmartClass.Shared.TransitionOverlay();
                _transitionOverlay.CancelRequested += (s, ev) =>
                {
                    Log.Warning("[LOI_VID_49] TransitionOverlay: User cancelled transition — ForceReset");
                    ModeService.ForceResetTransition();
                };
            }
            return _transitionOverlay;
        }

        /// <summary>
        /// [LOI_VID_49] Xử lý khi bắt đầu chuyển mode — hiển thị Transition Overlay
        /// </summary>
        internal void OnModeTransitionStarted(object? sender, ModeChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    var overlay = GetOrCreateTransitionOverlay();

                    // Tìm Window đang hiển thị để nhúng overlay
                    Window? activeWindow = _whiteboardShell?.IsVisible == true ? _whiteboardShell
                                         : _classroomShell?.IsVisible == true ? (Window)_classroomShell
                                         : null;

                    if (activeWindow != null)
                    {
                        // Nhúng overlay vào Grid ngoài cùng của Window
                        if (activeWindow.Content is System.Windows.Controls.Grid rootGrid)
                        {
                            if (!rootGrid.Children.Contains(overlay))
                            {
                                System.Windows.Controls.Grid.SetRowSpan(overlay, 100);
                                System.Windows.Controls.Grid.SetColumnSpan(overlay, 100);
                                System.Windows.Controls.Panel.SetZIndex(overlay, 9999);
                                rootGrid.Children.Add(overlay);
                            }
                        }
                    }

                    overlay.Show(e.NewMode);
                    Log.Debug("[LOI_VID_49] TransitionOverlay SHOWN for {Mode}", e.NewMode);
                }
                catch (Exception ex)
                {
                    Log.Warning("[LOI_VID_49] TransitionOverlay show error: {Err}", ex.Message);
                }
            });
        }

        /// <summary>
        /// [LOI_VID_49] Xử lý khi hoàn tất chuyển mode — ẩn Transition Overlay
        /// </summary>
        internal void OnModeTransitionCompleted(object? sender, ModeChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    _transitionOverlay?.Hide();
                    Log.Debug("[LOI_VID_49] TransitionOverlay HIDDEN after {Ms}ms",
                        ModeService.LastTransitionDurationMs);
                }
                catch (Exception ex)
                {
                    Log.Warning("[LOI_VID_49] TransitionOverlay hide error: {Err}", ex.Message);
                }
            });
        }

        /// <summary>
        /// Xử lý khi chuyển mode
        /// </summary>
        internal void OnModeChanged(object? sender, ModeChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Log.Information("Mode changing: {Old} → {New}", e.OldMode, e.NewMode);

                switch (e.NewMode)
                {
                    case AppMode.SmartClass:
                        if (UserRoleService.IsSmartTouchOnly ||
                            UserRoleService.CurrentRole == UserRole.Guest)
                        {
                            Log.Warning("OnModeChanged: BLOCKED SmartClass for role {Role}",
                                UserRoleService.CurrentRole);
                            ModeService.SwitchTo(AppMode.SmartScreen);
                            return;
                        }

                        if (!QASmartTouch.Services.License.LicenseService.Instance.HasFeature("smartclass"))
                        {
                            MessageBox.Show("Tính năng QA SmartClass không có trong gói bản quyền của bạn.", "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                            ModeService.SwitchTo(AppMode.SmartScreen); // Fallback
                            return;
                        }
                        ShowClassroom();
                        break;

                    case AppMode.SmartScreen:
                        if (!QASmartTouch.Services.License.LicenseService.Instance.HasFeature("smartscreen"))
                        {
                            MessageBox.Show("Tính năng QA SmartScreen không có trong gói bản quyền của bạn.", "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                            ModeService.SwitchTo(AppMode.Desktop); // Fallback
                            return;
                        }
                        ShowWhiteboard();
                        break;

                    case AppMode.Desktop:
                        HideAll();
                        break;
                }
            });
        }

        // ═══════════════════════════════════════════════════════
        //  [LOI_VID_50] VIEW CACHING — Tái sử dụng Window
        //  Chuyển mode bằng Visibility.Hidden/Visible thay vì Close/New
        //  để giảm thời gian chuyển từ 3-5s xuống < 300ms.
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// [LOI_VID_50] Cấu hình Window để chặn đóng (Closing → Hide thay vì Destroy).
        /// Giữ Window trong cache để tái sử dụng nhanh.
        /// </summary>
        private static void ConfigureWindowCaching(Window window)
        {
            window.Closing += (s, e) =>
            {
                // [LOI_VID_50] Cho phép đóng thật khi app đang shutdown
                if (_isAppShuttingDown) return;

                // Chặn đóng thật → chỉ ẩn (giữ trong cache)
                e.Cancel = true;
                window.Hide();
                Log.Debug("[LOI_VID_50] Window {Name} Closing intercepted → Hidden (cached)",
                    window.GetType().Name);
            };
        }

        /// <summary>
        /// [LOI_VID_50] Hiệu ứng Fade-out cho Window đang ẩn (100ms)
        /// </summary>
        private static void FadeOutWindow(Window? window)
        {
            if (window == null || window.Visibility != Visibility.Visible) return;

            try
            {
                var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(
                    1.0, 0.0, TimeSpan.FromMilliseconds(100))
                {
                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase
                    {
                        EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn
                    }
                };
                fadeOut.Completed += (s, e) =>
                {
                    window.Hide();
                    window.Opacity = 1.0; // Reset opacity cho lần hiện tiếp theo
                };
                window.BeginAnimation(Window.OpacityProperty, fadeOut);
            }
            catch
            {
                window.Hide(); // Fallback nếu animation lỗi
            }
        }

        /// <summary>
        /// [LOI_VID_50] Hiệu ứng Fade-in cho Window đang hiện (100ms)
        /// </summary>
        private static void FadeInWindow(Window window)
        {
            try
            {
                // [QC_4.2_WHITE_FLASH_FIX] Cửa sổ chưa Loaded (vừa tạo lần đầu) cần hiển thị ngay lập tức với Opacity = 1
                // để tránh hiện tượng Windows DWM / WS_EX_LAYERED vẽ chổi nền trắng trước frame đầu tiên.
                if (!window.IsLoaded)
                {
                    window.Opacity = 1.0;
                    window.Show();
                    window.Activate();
                    return;
                }

                window.Opacity = 0;
                window.Show();
                window.Activate();

                var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(
                    0.0, 1.0, TimeSpan.FromMilliseconds(100))
                {
                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase
                    {
                        EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
                    }
                };
                window.BeginAnimation(Window.OpacityProperty, fadeIn);
            }
            catch
            {
                window.Opacity = 1.0;
                window.Show();
                window.Activate();
            }
        }

        public void ShowClassroom()
        {
            // ✅ FIX: Đóng tool windows trước khi ẩn whiteboard
            _whiteboardShell?.CloseAllToolWindows();

            // [LOI_VID_50] Fade-out cửa sổ cũ thay vì Hide đột ngột
            FadeOutWindow(_whiteboardShell);
            _studentShell?.Hide();

            if (_classroomShell == null)
            {
                _classroomShell = new ClassroomShell();
                // [LOI_VID_50] Chặn đóng → giữ cache
                ConfigureWindowCaching(_classroomShell);
            }

            MainWindow = _classroomShell;
            // [LOI_VID_50] Fade-in cửa sổ mới
            FadeInWindow(_classroomShell);

            EnsureFloatingModeBar();

            Log.Information("Switched to SMART CLASS mode (cached={Cached})",
                _classroomShell != null);
        }

        public void ShowWhiteboard()
        {
            // [LOI_VID_50] Fade-out cửa sổ cũ thay vì Hide đột ngột
            FadeOutWindow(_classroomShell);
            _studentShell?.Hide();

            if (_whiteboardShell == null)
            {
                _whiteboardShell = new Form2_MainDashboard();
                // [LOI_VID_50] Chặn đóng → giữ cache
                ConfigureWindowCaching(_whiteboardShell);
            }

            MainWindow = _whiteboardShell;
            // [LOI_VID_50] Fade-in cửa sổ mới
            FadeInWindow(_whiteboardShell);

            EnsureFloatingModeBar();

            Log.Information("Switched to SMART TOUCH mode (cached={Cached})",
                _whiteboardShell != null);
        }

        private void HideAll()
        {
            // ✅ FIX: Đóng tool windows trước khi ẩn whiteboard
            _whiteboardShell?.CloseAllToolWindows();

            // [LOI_VID_50] Fade-out cả hai cửa sổ
            FadeOutWindow(_classroomShell);
            FadeOutWindow(_whiteboardShell);
            _studentShell?.Hide();
            
            EnsureFloatingModeBar();
            
            Log.Information("DESKTOP mode — app hidden, floating toolbar kept visible");
        }

        public void EnsureFloatingModeBar()
        {
            if (UserRoleService.ShouldShowModeBar)
            {
                if (_floatingModeBar == null)
                {
                    _floatingModeBar = new FloatingModeBar(ModeService, UserRoleService);
                }
                _floatingModeBar.Show();
                _floatingModeBar.Activate();
            }
            else
            {
                _floatingModeBar?.Hide();
            }
        }

        // ═══════════════════════════════════════════════════════
        //  STUDENT CLIENT
        // ═══════════════════════════════════════════════════════

        public void ShowStudentClient()
        {
            Dispatcher.Invoke(() =>
            {
                _classroomShell?.Hide();
                _whiteboardShell?.Hide();

                var login = new QASmartClass.StudentClient.Views.StudentLoginWindow();
                if (login.ShowDialog() == true)
                {
                    if (_studentShell == null)
                    {
                        _studentShell = new QASmartClass.StudentClient.Views.StudentShell();
                        _studentShell.Closed += (s, e) => _studentShell = null;
                    }
                    _studentShell.Show();
                    _studentShell.Activate();
                    Log.Information("Student client opened");
                }
                else
                {
                    Log.Information("Student login cancelled, falling back to Teacher mode");
                    UserRoleService.SaveRole(QASmartClass.Shared.UserRole.Teacher);

                    if (_classroomShell == null)
                    {
                        _classroomShell = new ClassroomShell();
                    }
                    _classroomShell.Show();
                    _classroomShell.Activate();

                    EnsureFloatingModeBar();
                }
            });
        }

        private void GlobalKeyDownHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F12 &&
                System.Windows.Input.Keyboard.Modifiers == (System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift | System.Windows.Input.ModifierKeys.Alt))
            {
                e.Handled = true;

                var pinDialog = new QASmartClass.Admin.Views.PinDialog();
                if (pinDialog.ShowDialog() == true)
                {
                    var console = new QASmartClass.Admin.Views.AdminConsoleWindow();
                    console.ShowDialog();
                }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // [LOI_VID_50] Cho phép đóng tất cả Window cached
            _isAppShuttingDown = true;

            SettingsManager.Instance.SaveSettings();
            QASmartClass.Services.TelemetryService.Instance.EndSession();
            QASmartClass.Services.StatusReportService.Instance.Stop();
            QASmartClass.Helpers.OfflineSyncManager.Stop();
            
            if (Database != null)
            {
                try
                {
                    var conn = Database.Database.GetDbConnection();
                    if (conn.State == System.Data.ConnectionState.Closed)
                    {
                        conn.Open();
                    }
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Failed to truncate SQLite WAL file on exit: {Err}", ex.Message);
                }
                Database.Dispose();
            }

            Log.Information("=== QA SmartTouch Shutting Down ===");
            Log.CloseAndFlush();
            base.OnExit(e);
        }

        private void SyncThematicImages()
        {
            try
            {
                string brainDir = @"C:\Users\DELL\.gemini\antigravity\brain\d61b062d-bf0f-448d-95b9-bd5b75cdf8f0";
                if (!System.IO.Directory.Exists(brainDir)) return;

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string sourceImgDir = "";
                string current = baseDir;
                for (int i = 0; i < 5; i++)
                {
                    if (System.IO.File.Exists(System.IO.Path.Combine(current, "QASmartClass.csproj")))
                    {
                        sourceImgDir = System.IO.Path.Combine(current, "Assets", "Images");
                        break;
                    }
                    var parent = System.IO.Directory.GetParent(current);
                    if (parent == null) break;
                    current = parent.FullName;
                }

                if (string.IsNullOrEmpty(sourceImgDir))
                {
                    sourceImgDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, @"..\..\..\Assets\Images"));
                }

                string binImgDir = System.IO.Path.Combine(baseDir, "Assets", "Images");

                var mapping = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>
                {
                    { "grammar_nlp_vn", new System.Collections.Generic.List<string> { "app_grammar_7_VN.png", "app_grammar_7_EN.png" } },
                    { "language_linguistics_1", new System.Collections.Generic.List<string> { "app_grammar_8_VN.png", "app_grammar_8_EN.png", "app_ipa_7_VN.png", "app_ipa_7_EN.png", "app_ipa_8_VN.png", "app_ipa_8_EN.png" } },
                    { "math_applied_1", new System.Collections.Generic.List<string> { 
                        "app_basicmath_7_VN.png", "app_basicmath_7_EN.png",
                        "app_conic_7_VN.png", "app_conic_7_EN.png"
                    } },
                    { "math_applied_2", new System.Collections.Generic.List<string> { 
                        "app_basicmath_8_VN.png", "app_basicmath_8_EN.png",
                        "app_conic_8_VN.png", "app_conic_8_EN.png"
                    } },
                    { "science_physics", new System.Collections.Generic.List<string> { 
                        "app_circuit_7_VN.png", "app_circuit_7_EN.png",
                        "app_circuit_8_VN.png", "app_circuit_8_EN.png"
                    } },
                    { "science_chemistry", new System.Collections.Generic.List<string> { 
                        "app_density_7_VN.png", "app_density_7_EN.png",
                        "app_density_8_VN.png", "app_density_8_EN.png",
                        "app_electron_7_VN.png", "app_electron_7_EN.png",
                        "app_electron_8_VN.png", "app_electron_8_EN.png"
                    } },
                    { "science_biology", new System.Collections.Generic.List<string> { 
                        "app_molecular_genetics_7_VN.png", "app_molecular_genetics_7_EN.png",
                        "app_molecular_genetics_8_VN.png", "app_molecular_genetics_8_EN.png"
                    } }
                };

                var brainFiles = System.IO.Directory.GetFiles(brainDir, "*.png");
                foreach (var kvp in mapping)
                {
                    string prefix = kvp.Key;
                    var matchedFiles = new System.Collections.Generic.List<string>();
                    foreach (var bf in brainFiles)
                    {
                        string name = System.IO.Path.GetFileName(bf);
                        if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            matchedFiles.Add(bf);
                        }
                    }

                    if (matchedFiles.Count == 0) continue;

                    matchedFiles.Sort((x, y) => System.IO.File.GetLastWriteTime(y).CompareTo(System.IO.File.GetLastWriteTime(x)));
                    string matchedFile = matchedFiles[0];

                    foreach (var targetName in kvp.Value)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(sourceImgDir))
                            {
                                System.IO.Directory.CreateDirectory(sourceImgDir);
                                string targetPath = System.IO.Path.Combine(sourceImgDir, targetName);
                                if (!System.IO.File.Exists(targetPath) || 
                                    System.IO.File.GetLastWriteTime(matchedFile) > System.IO.File.GetLastWriteTime(targetPath))
                                {
                                    System.IO.File.Copy(matchedFile, targetPath, overwrite: true);
                                    Log.Information("Copied {Src} to source {Dst}", matchedFile, targetPath);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Failed to copy to source path {Name}: {Err}", targetName, ex.Message);
                        }

                        try
                        {
                            if (!string.IsNullOrEmpty(binImgDir))
                            {
                                System.IO.Directory.CreateDirectory(binImgDir);
                                string targetPath = System.IO.Path.Combine(binImgDir, targetName);
                                if (!System.IO.File.Exists(targetPath) || 
                                    System.IO.File.GetLastWriteTime(matchedFile) > System.IO.File.GetLastWriteTime(targetPath))
                                {
                                    System.IO.File.Copy(matchedFile, targetPath, overwrite: true);
                                    Log.Information("Copied {Src} to bin {Dst}", matchedFile, targetPath);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Failed to copy to bin path {Name}: {Err}", targetName, ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error syncing thematic images: {Err}", ex.Message);
            }
        }

        private bool IsUnitTest => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.StartsWith("xunit", StringComparison.OrdinalIgnoreCase));

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            public const int SW_RESTORE = 9;
        }

        private static System.Threading.Mutex? _singleInstanceMutex;

        private void EnforceSingleInstance()
        {
            if (IsUnitTest) return;

            // Fast OS Level Mutex check (< 1ms execution, 0ms UI thread blocking)
            bool createdNew;
            _singleInstanceMutex = new System.Threading.Mutex(true, "Global\\QASmartClass_SingleInstance_Mutex_v4.2", out createdNew);

            if (!createdNew)
            {
                Log.Warning("[SingleInstance] Instance already running (OS Mutex locked). Bringing active window to front.");
                var currentProc = System.Diagnostics.Process.GetCurrentProcess();
                var duplicateProcs = System.Diagnostics.Process.GetProcessesByName(currentProc.ProcessName)
                    .Where(p => p.Id != currentProc.Id)
                    .ToList();

                foreach (var proc in duplicateProcs)
                {
                    try
                    {
                        IntPtr hWnd = proc.MainWindowHandle;
                        if (hWnd != IntPtr.Zero)
                        {
                            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
                            NativeMethods.SetForegroundWindow(hWnd);
                            break;
                        }
                    }
                    catch { }
                }

                Environment.Exit(0);
                return;
            }

            // Cleanup lingering zombie processes asynchronously in background
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var currentProc = System.Diagnostics.Process.GetCurrentProcess();
                    var duplicateProcs = System.Diagnostics.Process.GetProcessesByName(currentProc.ProcessName)
                        .Where(p => p.Id != currentProc.Id)
                        .ToList();

                    foreach (var proc in duplicateProcs)
                    {
                        try
                        {
                            if (!proc.Responding || proc.MainWindowHandle == IntPtr.Zero)
                            {
                                Log.Warning("[SingleInstance] Killing lingering zombie process ID {Id} in background", proc.Id);
                                proc.Kill(true);
                            }
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug("[SingleInstance] Background process cleanup note: {Err}", ex.Message);
                }
            });
        }
    }
}
