using System;
using System.Linq;
using System.Windows;
using Serilog;
using QASmartClass.Services;
using QASmartClass.Data;
using QASmartClass.Shared;
using QASmartClass.Classroom.Views;
using QASmartTouch.Forms;
using QASmartTouch.Services;
using QASmartTouch.Services.VersionManagement;

namespace QASmartTouch
{
    public partial class App
    {
        private void InitializeCoreServices(StartupEventArgs e, Action<string, int> updateStatus)
        {
            updateStatus("Khởi tạo hệ thống...", 5);

            // Global Exception Handler
            StatusReportService.Instance.Start();
            GlobalExceptionHandler.Initialize();

            // JSON structured logging setup
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(),
                    "logs/qasmarttouch-.json",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();

            Log.Information("=== QA SmartTouch v3.0 Starting ===");
            updateStatus("Tối ưu hiển thị...", 15);

            // Performance Optimization (Must run on UI thread)
            Application.Current.Dispatcher.Invoke(() =>
            {
                var config = QASmartClass.Services.AppConfig.Load();
                System.Windows.Media.RenderOptions.ProcessRenderMode = config.EnableHardwareAcceleration 
                    ? System.Windows.Interop.RenderMode.Default 
                    : System.Windows.Interop.RenderMode.SoftwareOnly;
                System.Windows.Media.Animation.Timeline.DesiredFrameRateProperty.OverrideMetadata(
                    typeof(System.Windows.Media.Animation.Timeline),
                    new FrameworkPropertyMetadata { DefaultValue = 60 }
                );
            });

            Log.Information("Hardware acceleration enabled, VSync 60 FPS");

            // Database Initialization
            updateStatus("Khởi tạo cơ sở dữ liệu...", 30);
            try
            {
                AppPaths.EnsureDirectories();
                QASmartClass.Services.SystemIntegrityService.HealConfigurations();
                QASmartClass.Services.SystemIntegrityService.HealDatabase("6.04.0");

                // Start telemetry session only after database healing has completed
                TelemetryService.Instance.StartSession();

                // Run background cleanup tasks
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        await System.Threading.Tasks.Task.Delay(5000);

                        // 1. Database Retention Policy Cleanup (30 days)
                        try
                        {
                            using var db = new AppDbContext();
                            var cutoff = DateTime.Now.AddDays(-30);
                            string cutoffStr = cutoff.ToString("yyyy-MM-dd HH:mm:ss");

                            var oldLogs = db.EventLogs.Where(l => l.Timestamp < cutoff).ToList();
                            if (oldLogs.Any())
                            {
                                db.EventLogs.RemoveRange(oldLogs);
                                Log.Information("Cleaned up {Count} obsolete event logs from database", oldLogs.Count);
                            }

                            var oldSurveys = db.Surveys.AsEnumerable().Where(s => {
                                if (DateTime.TryParse(s.CreatedAt, out var date))
                                {
                                    return date < cutoff;
                                }
                                return string.Compare(s.CreatedAt, cutoffStr) < 0;
                            }).ToList();

                            if (oldSurveys.Any())
                            {
                                db.Surveys.RemoveRange(oldSurveys);
                                db.SaveChanges();
                                Log.Information("Cleaned up {Count} obsolete surveys from database", oldSurveys.Count);
                            }

                            var surveyIds = db.Surveys.Select(s => s.Id).ToHashSet();
                            var orphanedResponses = db.SurveyResponses.AsEnumerable()
                                .Where(r => !surveyIds.Contains(r.SurveyId))
                                .ToList();
                            if (orphanedResponses.Any())
                            {
                                db.SurveyResponses.RemoveRange(orphanedResponses);
                                Log.Information("Cleaned up {Count} orphaned survey responses", orphanedResponses.Count);
                            }

                            db.SaveChanges();
                        }
                        catch (Exception dbEx)
                        {
                            Log.Warning("Background database retention auto cleanup failed: {Err}", dbEx.Message);
                        }

                        // 2. Orphaned Blackboard Images Cleanup
                        var folder = AppPaths.PicturesDir;
                        if (System.IO.Directory.Exists(folder))
                        {
                            using var db = new AppDbContext();
                            var activePaths = db.LessonContents
                                .Where(c => c.ContentType.StartsWith("Image"))
                                .Select(c => c.Data)
                                .ToList()
                                .Select(p => System.IO.Path.GetFullPath(p).ToLowerInvariant())
                                .ToHashSet();

                            var files = System.IO.Directory.GetFiles(folder, "Lesson_*_Board_*.png");
                            int deleteCount = 0;
                            foreach (var file in files)
                            {
                                var fullPath = System.IO.Path.GetFullPath(file).ToLowerInvariant();
                                if (!activePaths.Contains(fullPath))
                                {
                                    try
                                    {
                                        System.IO.File.Delete(file);
                                        deleteCount++;
                                    }
                                    catch { }
                                }
                            }
                            if (deleteCount > 0)
                            {
                                Log.Information("Cleaned up {Count} orphaned blackboard images from disk in background", deleteCount);
                            }
                        }

                        // 3. SQLite Offline Queue Log Auto-Cleanup (7 days)
                        try
                        {
                            await QASmartClass.StudentClient.Services.StudentNetworkClient.CleanupOldQueueLogsAsync();
                        }
                        catch (Exception cleanupEx)
                        {
                            Log.Warning("Background SQLite offline queue cleanup failed: {Err}", cleanupEx.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Background cleanup tasks error: {Err}", ex.Message);
                    }
                });

                Log.Information("Database initialized: SQLite v{Ver} + Seed data ready", "6.02.0");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Database initialization failed");
            }

            // Settings
            updateStatus("Nạp cấu hình & tính năng...", 60);
            AppSettings.Load(); // Nạp cấu hình toàn cục từ app_settings.json
            SettingsManager.Instance.LoadSettings();

            // Nạp cấu hình lớp học và thiết lập ngôn ngữ hiển thị
            try
            {
                string classroomSettingsFile = QASmartClass.Services.AppPaths.ClassroomSettingsFile;
                string lang = "vi";
                if (System.IO.File.Exists(classroomSettingsFile))
                {
                    var json = System.IO.File.ReadAllText(classroomSettingsFile);
                    using (var doc = System.Text.Json.JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("Language", out var langProp))
                        {
                            lang = langProp.GetString() ?? "vi";
                        }
                    }
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    QASmartClass.Shared.LanguageManager.SetLanguage(lang);
                });
                Log.Information("Loaded classroom language setting: {Lang}", lang);
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to load classroom language setting at startup: {Err}", ex.Message);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    QASmartClass.Shared.LanguageManager.SetLanguage("vi");
                });
            }

            try
            {
                var stats = FeatureManager.Instance.GetStatistics();
                Log.Information("Features loaded: {Enabled}/{Total} enabled", stats.Enabled, stats.Total);
            }
            catch (Exception ex)
            {
                Log.Warning("Version config load error: {Error}", ex.Message);
            }

            // License Checking
            updateStatus("Kiểm tra bản quyền...", 70);
            var licenseStatus = QASmartTouch.Services.License.LicenseService.Instance.CheckLicense();

            if (licenseStatus == QASmartTouch.Models.LicenseStatus.FreeTrial)
            {
                var trialMsg = QASmartTouch.Services.License.LicenseService.Instance.GetFreeTrialMessage();
                Log.Information("Free Trial Mode: {Message}", trialMsg);
                updateStatus("🎁 Chế độ dùng thử miễn phí 1 ngày", 75);
                System.Threading.Thread.Sleep(1500);
            }
            else if (licenseStatus == QASmartTouch.Models.LicenseStatus.FullyExpired ||
                licenseStatus == QASmartTouch.Models.LicenseStatus.Invalid ||
                licenseStatus == QASmartTouch.Models.LicenseStatus.NotActivated)
            {
                bool activated = false;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Tắt Splash Screen nếu có
                    var openSplash = Application.Current.Windows.OfType<QASmartTouch.Forms.SplashScreen>().FirstOrDefault();
                    openSplash?.Close();

                    MessageBox.Show("License không hợp lệ hoặc đã hết hạn hoàn toàn. Vui lòng kích hoạt phần mềm.",
                                  "Lỗi Bản Quyền", MessageBoxButton.OK, MessageBoxImage.Error);

                    var activationDialog = new ActivationDialog();
                    if (activationDialog.ShowDialog() == true)
                    {
                        activated = true;
                    }
                });

                if (!activated)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.Shutdown();
                    });
                    return;
                }

                licenseStatus = QASmartTouch.Services.License.LicenseService.Instance.CheckLicense();
            }
        }

        private void InitializeModeAndRole(QASmartTouch.Forms.SplashScreen splash)
        {
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            ModeService.ModeChanged += OnModeChanged;
            // [LOI_VID_49] Đăng ký Transition Overlay events
            ModeService.ModeTransitionStarted += OnModeTransitionStarted;
            ModeService.ModeTransitionCompleted += OnModeTransitionCompleted;

            var args = Environment.GetCommandLineArgs();
            bool isTestMode = args.Any(a => a.Equals("--test", StringComparison.OrdinalIgnoreCase));
            bool isStudentMode = args.Any(a => a.Equals("--student", StringComparison.OrdinalIgnoreCase));

            if (isTestMode)
            {
                Log.Information("Starting in TEST mode");
                var result = QASmartClass.Tests.IntegrationTest.RunAll();
                Log.Information("Test result:\n{Result}", result);

                var testLogPath = System.IO.Path.Combine(AppPaths.RootDir, "test_results.txt");
                System.IO.File.WriteAllText(testLogPath, result);

                MessageBox.Show(result, "Integration Test Results", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Shutdown();
                return;
            }

            if (isStudentMode)
            {
                Log.Information("Starting in STUDENT mode (command-line override)");
                UserRoleService.SaveRole(UserRole.Student);
                splash.UpdateStatus("Sẵn sàng!", 100);
                System.Threading.Thread.Sleep(300);
                splash.Close();
                this.ShowStudentClient();
                this.ShutdownMode = ShutdownMode.OnLastWindowClose;
            }
            else
            {
                string activeRole = AppSettings.ActiveUserRole;
                Log.Information("Active user role configuration: {ActiveRole}", activeRole);

                if (string.IsNullOrEmpty(activeRole) || activeRole.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    var loginSelection = new Form0_LoginSelection();
                    loginSelection.Show();
                }
                else
                {
                    if (activeRole.Equals("Teacher", StringComparison.OrdinalIgnoreCase))
                    {
                        var loginWindow = new Form1_MainLogin();
                        loginWindow.Show();
                    }
                    else if (activeRole.Equals("Student", StringComparison.OrdinalIgnoreCase))
                    {
                        UserRoleService.SaveRole(UserRole.Student);
                        if (splash.IsVisible)
                        {
                            splash.Close();
                        }
                        var studentLogin = new QASmartClass.StudentClient.Views.StudentLoginWindow();
                        if (studentLogin.ShowDialog() == true)
                        {
                            var app = (App)Application.Current;
                            if (app._studentShell == null)
                            {
                                app._studentShell = new QASmartClass.StudentClient.Views.StudentShell();
                                app._studentShell.Closed += (s, ev) => app._studentShell = null;
                            }
                            this.ShutdownMode = ShutdownMode.OnLastWindowClose;
                            app._studentShell.Show();
                            app._studentShell.Activate();
                        }
                        else
                        {
                            this.Shutdown();
                        }
                    }
                    else if (activeRole.Equals("Parent", StringComparison.OrdinalIgnoreCase))
                    {
                        var parentHost = new QASmartTouch.Forms.ParentLoginHostWindow();
                        parentHost.Show();
                    }
                    else if (activeRole.Equals("Staff", StringComparison.OrdinalIgnoreCase))

                    {

                        var staffLogin = new QASmartClass.Staff.Views.StaffLoginWindow();

                        if (staffLogin.ShowDialog() == true)

                        {

                            var staffDash = new QASmartClass.Staff.Views.StaffDashboardWindow();

                            staffDash.Show();

                        }

                        else

                        {

                            this.Shutdown();

                        }

                    }
                    else
                    {
                        var loginSelection = new Form0_LoginSelection();
                        loginSelection.Show();
                    }
                }
            }

            if (splash.IsVisible)
            {
                splash.UpdateStatus("Sẵn sàng!", 100);
                System.Threading.Thread.Sleep(300);
                splash.Close();
            }

            this.ShutdownMode = ShutdownMode.OnLastWindowClose;

            LogStartupQualityGate("[Splash Mode]");
            Log.Information("App started. Mode: {Mode}, Role: {Role}", ModeService.CurrentMode, UserRoleService.CurrentRole);

            var expiryTimer = new System.Windows.Threading.DispatcherTimer();
            expiryTimer.Interval = TimeSpan.FromHours(1);
            expiryTimer.Tick += (s, ev) =>
            {
                var warning = QASmartTouch.Services.License.LicenseService.Instance.GetExpiryWarning();
                if (!string.IsNullOrEmpty(warning))
                {
                    Log.Warning("License Expiry Notification: {Warning}", warning);
                }
            };
            expiryTimer.Start();
        }

        public static System.Diagnostics.Stopwatch StartupTimer { get; } = System.Diagnostics.Stopwatch.StartNew();

        private void LogStartupQualityGate(string context)
        {
            if (StartupTimer.IsRunning)
            {
                StartupTimer.Stop();
            }
            long elapsed = StartupTimer.ElapsedMilliseconds;
            Log.Information("[QualityGate] {Context} App Startup Completed in {ElapsedMs} ms. Target P95 <= 800ms. Status: {Status}", 
                context, elapsed, elapsed <= 800 ? "PASS" : "WARN");
        }

        private void InitializeModeAndRoleAfterLoading()
        {
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            ModeService.ModeChanged += OnModeChanged;
            // [LOI_VID_49] Đăng ký Transition Overlay events
            ModeService.ModeTransitionStarted += OnModeTransitionStarted;
            ModeService.ModeTransitionCompleted += OnModeTransitionCompleted;
            this.ShutdownMode = ShutdownMode.OnLastWindowClose;

            LogStartupQualityGate("[LoginSelection Mode]");
            Log.Information("App started in Login Selection mode. Mode: {Mode}, Role: {Role}", ModeService.CurrentMode, UserRoleService.CurrentRole);

            var expiryTimer = new System.Windows.Threading.DispatcherTimer();
            expiryTimer.Interval = TimeSpan.FromHours(1);
            expiryTimer.Tick += (s, ev) =>
            {
                var warning = QASmartTouch.Services.License.LicenseService.Instance.GetExpiryWarning();
                if (!string.IsNullOrEmpty(warning))
                {
                    Log.Warning("License Expiry Notification: {Warning}", warning);
                }
            };
            expiryTimer.Start();
        }
    }
}
