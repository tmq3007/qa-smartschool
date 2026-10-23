using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.Classroom.Helpers;
using QASmartClass.Classroom.Services;
using QASmartClass.Services;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    /// <summary>
    /// ClassControlHub - Màn hình điều khiển lớp học tập trung (Gộp 3.1 - 3.5).
    /// Tích hợp Giám sát màn hình HS, Phiên dạy, Trình chiếu & Phát sóng,
    /// Trao đổi tài liệu và Điều hướng Website vào chung một giao diện tối ưu thao tác.
    /// </summary>
    public partial class ClassControlHub : Page
    {
        private readonly ClassroomSessionService _sessionService;
        private string _currentTab = "Monitor";
        private bool _isInitialized = false;

        public ClassControlHub(string initialTab = "Monitor")
        {
            InitializeComponent();
            _sessionService = ClassroomAppContext.Session;
            _currentTab = initialTab;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                SelectTab(_currentTab);

                // Đăng ký lắng nghe trạng thái phát màn hình
                ScreenShareController.Instance.LiveChanged += OnLiveStateChanged;

                // Lắng nghe thay đổi học sinh kết nối
                if (_sessionService.ConnectedStudents != null)
                {
                    _sessionService.ConnectedStudents.CollectionChanged += ConnectedStudents_CollectionChanged;
                }
            }

            UpdateLiveBroadcastUI();
            UpdateSessionStatusUI();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            ScreenShareController.Instance.LiveChanged -= OnLiveStateChanged;
            if (_sessionService.ConnectedStudents != null)
            {
                _sessionService.ConnectedStudents.CollectionChanged -= ConnectedStudents_CollectionChanged;
            }
        }

        private void ConnectedStudents_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.Invoke(UpdateSessionStatusUI);
        }

        private void OnLiveStateChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(UpdateLiveBroadcastUI);
        }

        private void UpdateLiveBroadcastUI()
        {
            bool isLive = ScreenShareController.Instance.IsLive;
            var source = ScreenShareController.Instance.Source;

            if (isLive)
            {
                dotLiveStatus.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red #EF4444
                txtLiveStatus.Text = source switch
                {
                    ScreenShareSource.Vnc => "🔴 Đang phát (VNC)",
                    ScreenShareSource.Desktop => "🔴 Đang phát màn hình",
                    _ => "🔴 Đang phát trực tiếp"
                };
                txtLiveStatus.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38)); // #DC2626
                pillLiveStatus.BorderBrush = new SolidColorBrush(Color.FromRgb(254, 202, 202)); // #FECACA
                pillLiveStatus.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)); // #FEF2F2
            }
            else
            {
                dotLiveStatus.Fill = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // Slate #94A3B8
                txtLiveStatus.Text = "Chưa phát màn hình";
                txtLiveStatus.Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)); // #475569
                pillLiveStatus.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)); // #CBD5E1
                pillLiveStatus.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // #F1F5F9
            }
        }

        private void UpdateSessionStatusUI()
        {
            int count = _sessionService.ConnectedStudents?.Count ?? 0;
            txtStudentCount.Text = $"👥 {count} Học sinh";

            if (_sessionService.IsSilenceActive)
            {
                pillSilence.Visibility = Visibility.Visible;
                txtQuickMute.Text = "Mở im lặng";
                iconQuickMute.Text = "🔊";
                btnQuickMute.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242));
                btnQuickMute.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            }
            else
            {
                pillSilence.Visibility = Visibility.Collapsed;
                txtQuickMute.Text = "Im lặng";
                iconQuickMute.Text = "🔇";
                btnQuickMute.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                btnQuickMute.Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85));
            }
        }

        public void SelectTabByFormId(string formId)
        {
            string tab = formId switch
            {
                "F4" or "F5" or "F12" => "Monitor",
                "F16" => "Broadcast",
                "F11" => "Files",
                "F30" => "Web",
                _ => "Monitor"
            };
            SelectTab(tab);
        }

        public void SelectTab(string tabKey)
        {
            _currentTab = tabKey;

            // Reset tab button styles
            var normalStyle = (Style)FindResource("HubTabButton");
            var activeStyle = (Style)FindResource("HubTabButtonActive");

            tabBtnMonitor.Style = tabKey == "Monitor" ? activeStyle : normalStyle;
            tabBtnBroadcast.Style = tabKey == "Broadcast" ? activeStyle : normalStyle;
            tabBtnFiles.Style = tabKey == "Files" ? activeStyle : normalStyle;
            tabBtnWeb.Style = tabKey == "Web" ? activeStyle : normalStyle;

            // Hide all frames
            frameMonitor.Visibility = Visibility.Collapsed;
            frameBroadcast.Visibility = Visibility.Collapsed;
            frameFiles.Visibility = Visibility.Collapsed;
            frameWeb.Visibility = Visibility.Collapsed;

            // Lazy-load page instance into frame if not already loaded, then show
            switch (tabKey)
            {
                case "Monitor":
                    if (frameMonitor.Content == null)
                        frameMonitor.Navigate(new MonitorPage());
                    frameMonitor.Visibility = Visibility.Visible;
                    break;

                case "Broadcast":
                    if (frameBroadcast.Content == null)
                        frameBroadcast.Navigate(new BroadcastPage());
                    frameBroadcast.Visibility = Visibility.Visible;
                    break;

                case "Files":
                    if (frameFiles.Content == null)
                        frameFiles.Navigate(new FileTransferPage());
                    frameFiles.Visibility = Visibility.Visible;
                    break;

                case "Web":
                    if (frameWeb.Content == null)
                        frameWeb.Navigate(new WebsitePushPage());
                    frameWeb.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tabKey)
            {
                SelectTab(tabKey);
            }
        }

        #region Quick Actions

        private async void QuickLock_Click(object sender, RoutedEventArgs e)
        {
            if (!_sessionService.IsSessionActive)
            {
                ClassroomDialog.Warn("Vui lòng bắt đầu phiên học trước khi thực hiện thao tác!", "Thông báo");
                return;
            }

            try
            {
                await _sessionService.LockAllAsync();
                ClassroomDialog.Info("🔒 Đã gửi lệnh khóa toàn bộ màn hình học sinh thành công!", "Khóa máy");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi thực hiện khóa toàn bộ máy học sinh");
                ClassroomDialog.Error($"Không thể khóa màn hình: {ex.Message}", "Lỗi");
            }
        }

        private async void QuickUnlock_Click(object sender, RoutedEventArgs e)
        {
            if (!_sessionService.IsSessionActive)
            {
                ClassroomDialog.Warn("Vui lòng bắt đầu phiên học trước khi thực hiện thao tác!", "Thông báo");
                return;
            }

            try
            {
                await _sessionService.UnlockAllAsync();
                ClassroomDialog.Info("🔓 Đã mở khóa tất cả máy học sinh thành công!", "Mở khóa máy");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi thực hiện mở khóa toàn bộ máy học sinh");
                ClassroomDialog.Error($"Không thể mở khóa màn hình: {ex.Message}", "Lỗi");
            }
        }

        private async void QuickMute_Click(object sender, RoutedEventArgs e)
        {
            if (!_sessionService.IsSessionActive)
            {
                ClassroomDialog.Warn("Vui lòng bắt đầu phiên học trước khi thực hiện thao tác!", "Thông báo");
                return;
            }

            try
            {
                if (_sessionService.IsSilenceActive)
                {
                    await _sessionService.ClearSilenceAllAsync();
                    ClassroomDialog.Info("🔊 Đã gỡ bỏ trạng thái im lặng!", "Mở im lặng");
                }
                else
                {
                    await _sessionService.SilenceAllAsync();
                    ClassroomDialog.Info("🔇 Đã kích hoạt chế độ im lặng trên các máy học sinh!", "Im lặng");
                }
                UpdateSessionStatusUI();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi thay đổi chế độ im lặng");
                ClassroomDialog.Error($"Không thể thực hiện: {ex.Message}", "Lỗi");
            }
        }

        #endregion
    }
}
