using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Serilog;
using QASmartTouch.Forms;
using QASmartTouch.Services;
using QASmartTouch.Helpers;
using QASmartTouch.Services.License;
using QASmartClass.Forms;
using QASmartClass.Services;
using QASmartClass.Helpers;
using QASmartClass.Models;

namespace QASmartTouch
{
    public partial class App : Application
    {
        private static Mutex? _singleInstanceMutex;
        private const string MutexName = "Global\\QASmartTouch_SingleInstance_Mutex";
        // Tiêu đề cửa sổ chính: dùng chung cho Title và FindWindow của single-instance
        private const string MainWindowTitle = "QA Smart Touch";
        public static string? StartupLecturePath { get; private set; }
        private Form2_MainDashboard? _whiteboardWindow;
        public Form2_MainDashboard? _whiteboardShell => _whiteboardWindow;
        public QASmartClass.Shared.ModeService ModeService { get; } = new QASmartClass.Shared.ModeService();

        static App()
        {
            // Khởi tạo thư mục môi trường WebView2 ngay từ đầu
            WebView2Helper.InitializeEnvironment();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 1. Cố định thư mục làm việc luôn là thư mục cài đặt ứng dụng
            try
            {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            }
            catch { }

            // 2. Tiếp nhận tham số tệp bài giảng .qasc khi người dùng click đúp mở từ Explorer
            try
            {
                var rawArgs = Environment.GetCommandLineArgs().Skip(1).Concat(e.Args ?? Array.Empty<string>());
                var lectureArg = rawArgs.FirstOrDefault(a =>
                    !string.IsNullOrWhiteSpace(a) &&
                    a.EndsWith(".qasc", StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(a));
                if (!string.IsNullOrEmpty(lectureArg))
                {
                    StartupLecturePath = Path.GetFullPath(lectureArg);
                }
            }
            catch { }

            // 3. Kiểm soát Single Instance (tránh mở nhiều bảng vẽ cùng lúc)
            EnforceSingleInstance();

            // 4. ⭐ HIỂN THỊ MÀN HÌNH CHỜ (SPLASH SCREEN)
            var splash = new QASmartTouch.Forms.SplashScreen();
            this.MainWindow = splash;
            splash.Show();
            splash.UpdateStatus("Khởi tạo hệ thống...", 25);

            // 5. Khởi tạo Exception Handler & Logger
            StatusReportService.Instance.Start();
            GlobalExceptionHandler.Initialize();

            string logFilePath = Path.Combine(QASmartClass.Services.AppPaths.LogsDir, "qasmarttouch-.json");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(),
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();

            Log.Information("=== QA Smart Touch Starting ===");

            // 6. Tối ưu hiển thị phần cứng & Tốc độ khung hình 60 FPS
            try
            {
                var config = QASmartClass.Services.AppConfig.Load();
                System.Windows.Media.RenderOptions.ProcessRenderMode = config.EnableHardwareAcceleration
                    ? System.Windows.Interop.RenderMode.Default
                    : System.Windows.Interop.RenderMode.SoftwareOnly;
                System.Windows.Media.Animation.Timeline.DesiredFrameRateProperty.OverrideMetadata(
                    typeof(System.Windows.Media.Animation.Timeline),
                    new FrameworkPropertyMetadata { DefaultValue = 60 }
                );
            }
            catch { }

            // 7. Tối ưu cảm ứng 1 chạm cho thanh trượt Slider toàn hệ thống
            TouchSliderHelper.Initialize();

            splash.UpdateStatus("Nạp cấu hình & bảng vẽ...", 60);
            await Task.Delay(150);

            // 8. Nạp cấu hình hệ thống
            try
            {
                AppSettings.Load();
                SettingsManager.Instance.LoadSettings();
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to load settings at early startup: {Err}", ex.Message);
            }

            splash.UpdateStatus("Đang mở bảng viết...", 85);
            await Task.Delay(150);

            base.OnStartup(e);

            // 9. KHỞI TẠO BẢNG VẼ FORM2_MAINDASHBOARD
            _whiteboardWindow = new Form2_MainDashboard();
            _whiteboardWindow.Title = MainWindowTitle;

            // Nếu có tệp bài giảng cần nạp lúc khởi động (.qasc)
            if (!string.IsNullOrEmpty(StartupLecturePath) && File.Exists(StartupLecturePath))
            {
                string lectureToOpen = StartupLecturePath;
                StartupLecturePath = null;
                if (_whiteboardWindow.IsLoaded)
                {
                    _whiteboardWindow.LoadLecture(lectureToOpen);
                }
                else
                {
                    _whiteboardWindow.Loaded += (s, ev) =>
                    {
                        _whiteboardWindow.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            _whiteboardWindow.LoadLecture(lectureToOpen);
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    };
                }
            }

            splash.UpdateStatus("Sẵn sàng!", 100);
            await Task.Delay(250);

            // Chuyển giao cửa sổ chính sang Bảng vẽ và đóng Splash Screen với hiệu ứng Fade-Out mượt mà
            this.MainWindow = _whiteboardWindow;
            _whiteboardWindow.Show();
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;

            await splash.FadeOutAndClose();

            Log.Information("QA Smart Touch Dashboard opened successfully.");

            // 10. TÁC VỤ NỀN BẤT ĐỒNG BỘ (Non-blocking): Không làm chậm giao diện
            Task.Run(() =>
            {
                try
                {
                    // Đăng ký liên kết định dạng .qasc trong Windows Registry
                    FileAssociationHelper.EnsureFileAssociation();
                }
                catch { }

                try
                {
                    // Kiểm tra bản quyền nền
                    var licenseStatus = LicenseService.Instance.CheckLicense();
                    if (licenseStatus == QASmartTouch.Models.LicenseStatus.FullyExpired ||
                        licenseStatus == QASmartTouch.Models.LicenseStatus.Invalid ||
                        licenseStatus == QASmartTouch.Models.LicenseStatus.NotActivated)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            var actDlg = new ActivationDialog();
                            actDlg.Owner = _whiteboardWindow;
                            actDlg.ShowDialog();
                        });
                    }
                }
                catch (Exception licEx)
                {
                    Log.Warning("Background license check error: {Err}", licEx.Message);
                }
            });
        }

        private void EnforceSingleInstance()
        {
            try
            {
                _singleInstanceMutex = new Mutex(true, MutexName, out bool isNewInstance);
                if (!isNewInstance)
                {
                    // Nếu đã có instance khác đang chạy, gửi tín hiệu kích hoạt rồi thoát
                    if (!string.IsNullOrEmpty(StartupLecturePath))
                    {
                        IntPtr hWnd = NativeMethods.FindWindow(null, MainWindowTitle);
                        if (hWnd != IntPtr.Zero)
                        {
                            NativeMethods.SendCopyDataString(hWnd, StartupLecturePath);
                            NativeMethods.SetForegroundWindow(hWnd);
                        }
                    }
                    Environment.Exit(0);
                }
            }
            catch
            {
                // Bỏ qua lỗi mutex nếu môi trường hạn chế quyền
            }
        }
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        public static void SendCopyDataString(IntPtr hWnd, string text)
        {
            byte[] sarr = System.Text.Encoding.Unicode.GetBytes(text);
            int len = sarr.Length;
            COPYDATASTRUCT cds;
            cds.dwData = (IntPtr)100;
            cds.lpData = System.Runtime.InteropServices.Marshal.StringToHGlobalUni(text);
            cds.cbData = len + 2;
            SendMessage(hWnd, 0x004A, IntPtr.Zero, ref cds);
            System.Runtime.InteropServices.Marshal.FreeHGlobal(cds.lpData);
        }
    }
}
