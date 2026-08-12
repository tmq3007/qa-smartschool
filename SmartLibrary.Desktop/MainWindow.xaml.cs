using System.Windows;
using SmartLibrary.Desktop.Services;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _mainViewModel;
        private readonly ApiService _apiService;

        public MainWindow()
        {
            InitializeComponent();
            
            // Basic DI setup for now
            _apiService = new ApiService();
            var authService = new AuthService(_apiService);
            var qrScannerService = new QrScannerService();

            // Khởi tạo AuditLogService với ApiService (Item 1.3 — Dual-write)
            AuditLogService.Initialize(_apiService);
            
            var loginViewModel = new LoginViewModel(authService, qrScannerService);
            loginViewModel.OnLoginSuccess = HandleLoginSuccess;
            LoginControl.DataContext = loginViewModel;

            _apiService.OnConnectionError += (s, e) => Dispatcher.Invoke(() => OfflineOverlay.Visibility = Visibility.Visible);

            _mainViewModel = new MainViewModel(authService, _apiService);
            this.DataContext = _mainViewModel;
            _mainViewModel.OnLogoutRequested += HandleLogoutRequested;

            // Heartbeat network check: 30s check 1 lần
            var heartbeatTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            heartbeatTimer.Tick += async (s, e) =>
            {
                if (!ApiService.IsOfflineMode)
                {
                    try
                    {
                        await _apiService.GetAsync<object>("/Auth/health", silent: true);
                        if (OfflineOverlay.Visibility == Visibility.Visible)
                        {
                            OfflineOverlay.Visibility = Visibility.Collapsed;
                        }
                    }
                    catch
                    {
                        ApiService.IsOfflineMode = true;
                        OfflineOverlay.Visibility = Visibility.Collapsed;
                        
                        ShowNotification("⚠️ Mất kết nối máy chủ! Hệ thống đã tự động chuyển sang chế độ ngoại tuyến (Offline Mode).", "#EF4444");
                            
                        _mainViewModel.Initialize();
                    }
                }
                else
                {
                    try
                    {
                        // Ping thử xem máy chủ đã online lại chưa
                        await _apiService.GetAsync<object>("/Auth/health", silent: true);
                        
                        // Khôi phục kết nối thành công
                        ApiService.IsOfflineMode = false;
                        OfflineOverlay.Visibility = Visibility.Collapsed;
                        
                        ShowNotification("✅ Kết nối máy chủ đã được khôi phục! Hệ thống tự động chuyển lại chế độ trực tuyến (Online Mode).", "#10B981");
                            
                        _mainViewModel.Initialize();
                        
                        // Tự động đồng bộ log tích lũy ngoại tuyến lên máy chủ
                        _ = System.Threading.Tasks.Task.Run(async () => await AuditLogService.SyncPendingLogsAsync());
                        
                        // Tự động đồng bộ đề xuất tích lũy ngoại tuyến lên máy chủ
                        _ = System.Threading.Tasks.Task.Run(async () => await SmartLibrary.Desktop.ViewModels.ProposalViewModel.SyncPendingProposalsAsync(_apiService));
                    }
                    catch
                    {
                        // Vẫn offline -> bỏ qua
                    }
                }
            };
            heartbeatTimer.Start();

            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Tác vụ dọn dẹp biên lai in `.txt` cũ quá 30 ngày chạy ngầm
            System.Threading.Tasks.Task.Run(() => CleanOldReceipts());

            try
            {
                // Ping thử API tĩnh khi load
                await _apiService.GetAsync<object>("/Auth/health", silent: true);
            }
            catch
            {
                // API offline ngay từ đầu -> Silent offline bypass
                GoOffline();
            }
        }

        private void CleanOldReceipts()
        {
            try
            {
                string appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                string receiptsPath = System.IO.Path.Combine(appData, "SmartLibrary", "receipts");
                if (System.IO.Directory.Exists(receiptsPath))
                {
                    var files = System.IO.Directory.GetFiles(receiptsPath, "*.txt");
                    int deletedCount = 0;
                    foreach (var file in files)
                    {
                        var fileInfo = new System.IO.FileInfo(file);
                        if (fileInfo.LastWriteTime < System.DateTime.Now.AddDays(-30))
                        {
                            fileInfo.Delete();
                            deletedCount++;
                        }
                    }
                    if (deletedCount > 0)
                    {
                        _ = AuditLogService.WriteLogAsync("Dọn dẹp hệ thống", $"Đã tự động xóa sạch {deletedCount} biên lai giấy cũ (> 30 ngày) chạy ngầm.", true);
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi dọn dẹp biên lai: {ex.Message}");
                _ = AuditLogService.WriteLogAsync("Dọn dẹp hệ thống", $"Lỗi dọn dẹp biên lai cũ chạy ngầm: {ex.Message}", false);
            }
        }

        private void HandleLoginSuccess()
        {
            LoginContainer.Visibility = Visibility.Collapsed;
            MainShell.Visibility = Visibility.Visible;
            _mainViewModel.Initialize();
        }

        private void HandleLogoutRequested()
        {
            var authService = new AuthService(_apiService);
            authService.Logout();
            _mainViewModel.LogoutCleanup();
            LoginContainer.Visibility = Visibility.Visible;
            MainShell.Visibility = Visibility.Collapsed;
            ShowNotification("🚪 Đã đăng xuất khỏi hệ thống thành công!", "#10B981");
        }

        public void GoOffline()
        {
            ApiService.IsOfflineMode = true;
            OfflineOverlay.Visibility = Visibility.Collapsed;
            _mainViewModel.Initialize();
        }

        private void ShowNotification(string message, string hexColor)
        {
            Dispatcher.Invoke(() =>
            {
                NotificationText.Text = message;
                NotificationBanner.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor));
                NotificationBanner.Visibility = Visibility.Visible;
                
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (s, e) => { timer.Stop(); NotificationBanner.Visibility = Visibility.Collapsed; };
                timer.Start();
            });
        }

        private void CloseNotification_Click(object sender, RoutedEventArgs e)
        {
            NotificationBanner.Visibility = Visibility.Collapsed;
        }

        public static void PlayAudio(string url, string title)
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow win)
            {
                win.AudioPlayer.Play(url, title);
            }
        }
    }
}