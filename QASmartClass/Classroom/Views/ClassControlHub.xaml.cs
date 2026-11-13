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
        private static ClassControlHub? _activeHub;
        private readonly ClassroomSessionService _sessionService;
        private BroadcastPage? _broadcastPage;
        private string _currentTab = "Monitor";
        private bool _isInitialized = false;

        public ClassControlHub(string initialTab = "Monitor")
        {
            InitializeComponent();
            _sessionService = ClassroomAppContext.Session;
            _currentTab = initialTab;

            if (_activeHub != null && !ReferenceEquals(_activeHub, this))
            {
                ScreenShareController.Instance.ActionRequested -= _activeHub.OnQuickBroadcastActionRequested;
            }
            _activeHub = this;
            ScreenShareController.Instance.ActionRequested -= OnQuickBroadcastActionRequested;
            ScreenShareController.Instance.ActionRequested += OnQuickBroadcastActionRequested;
        }

        public BroadcastPage EnsureBroadcastPageLoaded()
        {
            if (frameBroadcast.Content is BroadcastPage loadedBp)
            {
                _broadcastPage = loadedBp;
                return loadedBp;
            }

            if (_broadcastPage == null)
            {
                _broadcastPage = new BroadcastPage();
                frameBroadcast.Navigate(_broadcastPage);
            }
            else if (frameBroadcast.Content == null)
            {
                frameBroadcast.Navigate(_broadcastPage);
            }

            return _broadcastPage;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                _isInitialized = true;

                // Khởi tạo sẵn MessagingPage để lắng nghe và chuyển tiếp tin nhắn Cả lớp / Học sinh ngay từ đầu
                if (frameMessaging.Content == null)
                {
                    frameMessaging.Navigate(new MessagingPage());
                }

                // Khởi tạo sẵn BroadcastPage để nút Quảng bá / Share màn hình trên FloatingModeBar phản hồi tức thì ở mọi chế độ
                EnsureBroadcastPageLoaded();

                SelectTab(_currentTab);

                // Đăng ký lắng nghe trạng thái phát màn hình
                ScreenShareController.Instance.LiveChanged += OnLiveStateChanged;

                // Lắng nghe thay đổi học sinh kết nối
                if (_sessionService.ConnectedStudents != null)
                {
                    _sessionService.ConnectedStudents.CollectionChanged += ConnectedStudents_CollectionChanged;
                }
            }

            if (ClassroomAppContext.Network != null)
            {
                ClassroomAppContext.Network.StudentConnected -= OnNetworkStudentChanged;
                ClassroomAppContext.Network.StudentConnected += OnNetworkStudentChanged;
                ClassroomAppContext.Network.StudentDisconnected -= OnNetworkStudentDisconnected;
                ClassroomAppContext.Network.StudentDisconnected += OnNetworkStudentDisconnected;
                ClassroomAppContext.Network.MessageReceived -= OnNetworkMessageForHub;
                ClassroomAppContext.Network.MessageReceived += OnNetworkMessageForHub;
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
            if (ClassroomAppContext.Network != null)
            {
                ClassroomAppContext.Network.StudentConnected -= OnNetworkStudentChanged;
                ClassroomAppContext.Network.StudentDisconnected -= OnNetworkStudentDisconnected;
                ClassroomAppContext.Network.MessageReceived -= OnNetworkMessageForHub;
            }
        }

        private void OnNetworkStudentChanged(object? sender, Services.StudentConnectedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateSessionStatusUI();
                if (popupConnectedStudents?.IsOpen == true) PopulateConnectedStudentsPopup();
            }));
        }

        private void OnNetworkStudentDisconnected(object? sender, string code)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateSessionStatusUI();
                if (popupConnectedStudents?.IsOpen == true) PopulateConnectedStudentsPopup();
            }));
        }

        private void OnNetworkMessageForHub(object? sender, Services.StudentMessageEventArgs e)
        {
            if (e?.Message != null && (e.Message.StartsWith("CMD|FOCUS_STATUS|") || e.Message.StartsWith("FOCUS_STATUS|")))
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateSessionStatusUI();
                    if (popupConnectedStudents?.IsOpen == true) PopulateConnectedStudentsPopup();
                }));
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
                    ScreenShareSource.Vnc => "🔴 Đang phát VNC • Dừng",
                    ScreenShareSource.Desktop => "🔴 Đang chiếu • Bấm để dừng",
                    _ => "🔴 Đang phát • Bấm để dừng"
                };
                txtLiveStatus.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38)); // #DC2626
                pillLiveStatus.BorderBrush = new SolidColorBrush(Color.FromRgb(254, 202, 202)); // #FECACA
                pillLiveStatus.Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)); // #FEF2F2
                pillLiveStatus.Cursor = System.Windows.Input.Cursors.Hand;

                if (btnQuickBroadcast != null)
                {
                    btnQuickBroadcast.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226)); // #FEE2E2
                    btnQuickBroadcast.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));  // #B91C1C
                    btnQuickBroadcast.BorderBrush = new SolidColorBrush(Color.FromRgb(248, 113, 113));
                    if (iconQuickBroadcast != null) iconQuickBroadcast.Text = "⏹️";
                    if (txtQuickBroadcast != null) txtQuickBroadcast.Text = "Dừng chiếu";
                }
            }
            else
            {
                dotLiveStatus.Fill = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // Slate #94A3B8
                txtLiveStatus.Text = "Chưa phát màn hình";
                txtLiveStatus.Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)); // #475569
                pillLiveStatus.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)); // #CBD5E1
                pillLiveStatus.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // #F1F5F9
                pillLiveStatus.Cursor = System.Windows.Input.Cursors.Arrow;

                if (btnQuickBroadcast != null)
                {
                    btnQuickBroadcast.Background = Brushes.Transparent;
                    btnQuickBroadcast.Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                    btnQuickBroadcast.BorderBrush = Brushes.Transparent;
                    if (iconQuickBroadcast != null) iconQuickBroadcast.Text = "📺";
                    if (txtQuickBroadcast != null) txtQuickBroadcast.Text = "Chiếu bài giảng";
                }
            }
        }

        private void UpdateSessionStatusUI()
        {
            var clients = ClassroomAppContext.Network?.GetConnectedStudents();
            int netCount = clients?.Count ?? (ClassroomAppContext.Network?.ConnectedCount ?? 0);
            int sessCount = _sessionService.ConnectedStudents?.Count ?? 0;
            int count = Math.Max(netCount, sessCount);
            int offTaskCount = clients?.Count(c => !c.IsFocusedInClass) ?? 0;

            if (offTaskCount > 0)
            {
                txtStudentCount.Text = $"👥 {count} HS (⚠️ {offTaskCount})";
                if (pillStudentRoster != null)
                {
                    pillStudentRoster.Background = new SolidColorBrush(Color.FromRgb(255, 247, 237)); // #FFF7ED
                    pillStudentRoster.BorderBrush = new SolidColorBrush(Color.FromRgb(253, 186, 116)); // #FDBA74
                    txtStudentCount.Foreground = new SolidColorBrush(Color.FromRgb(194, 65, 12)); // #C2410C
                }
            }
            else
            {
                txtStudentCount.Text = $"👥 {count} Học sinh";
                if (pillStudentRoster != null)
                {
                    pillStudentRoster.Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)); // #EFF6FF
                    pillStudentRoster.BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254)); // #BFDBFE
                    txtStudentCount.Foreground = new SolidColorBrush(Color.FromRgb(29, 78, 216)); // #1D4ED8
                }
            }

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
                btnQuickMute.Background = Brushes.Transparent;
                btnQuickMute.Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59));
            }
        }

        public void SelectTabByFormId(string formId)
        {
            string tab = formId switch
            {
                "F4" or "F5" or "F12" => "Monitor",
                "F16" => "Broadcast",
                "F13" => "Messaging",
                "F18" => "Survey",
                "F11" => "Files",
                "F30" => "Web",
                _ => "Monitor"
            };
            SelectTab(tab);
        }

        public void OpenChatWithStudent(string studentCode)
        {
            SelectTab("Messaging");
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (frameMessaging.Content is MessagingPage mp && !string.IsNullOrWhiteSpace(studentCode))
                {
                    mp.SelectChannel(studentCode);
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public void SelectTab(string tabKey)
        {
            _currentTab = tabKey;

            // Reset tab button styles
            var normalStyle = (Style)FindResource("HubTabButton");
            var activeStyle = (Style)FindResource("HubTabButtonActive");

            tabBtnMonitor.Style = tabKey == "Monitor" ? activeStyle : normalStyle;
            tabBtnBroadcast.Style = tabKey == "Broadcast" ? activeStyle : normalStyle;
            if (tabBtnMessaging != null)
                tabBtnMessaging.Style = tabKey == "Messaging" ? activeStyle : normalStyle;
            if (tabBtnSurvey != null)
                tabBtnSurvey.Style = tabKey == "Survey" ? activeStyle : normalStyle;
            tabBtnFiles.Style = tabKey == "Files" ? activeStyle : normalStyle;
            tabBtnWeb.Style = tabKey == "Web" ? activeStyle : normalStyle;

            // Hide all frames
            frameMonitor.Visibility = Visibility.Collapsed;
            frameBroadcast.Visibility = Visibility.Collapsed;
            frameMessaging.Visibility = Visibility.Collapsed;
            if (frameSurvey != null)
                frameSurvey.Visibility = Visibility.Collapsed;
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
                    EnsureBroadcastPageLoaded();
                    frameBroadcast.Visibility = Visibility.Visible;
                    break;

                case "Messaging":
                    if (frameMessaging.Content == null)
                        frameMessaging.Navigate(new MessagingPage());
                    frameMessaging.Visibility = Visibility.Visible;
                    break;

                case "Survey":
                    if (frameSurvey != null)
                    {
                        if (frameSurvey.Content == null)
                            frameSurvey.Navigate(new SurveyPage());
                        frameSurvey.Visibility = Visibility.Visible;
                    }
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

        private void QuickBroadcast_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ScreenShareController.Instance.IsLive)
                {
                    ScreenShareController.Instance.StopAllActiveBroadcasts();
                    UpdateLiveBroadcastUI();
                    return;
                }

                var bp = EnsureBroadcastPageLoaded();
                bp.ToggleMythwareQuickBroadcast();
                UpdateLiveBroadcastUI();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi bật/tắt trình chiếu màn hình nhanh");
                ClassroomDialog.Error($"Không thể phát màn hình: {ex.Message}", "Lỗi");
            }
        }

        private void PillLiveStatus_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ScreenShareController.Instance.IsLive)
            {
                e.Handled = true;
                ScreenShareController.Instance.StopAllActiveBroadcasts();
                UpdateLiveBroadcastUI();
            }
        }

        private void PillLiveStatus_TouchDown(object? sender, System.Windows.Input.TouchEventArgs e)
        {
            if (ScreenShareController.Instance.IsLive)
            {
                e.Handled = true;
                ScreenShareController.Instance.StopAllActiveBroadcasts();
                UpdateLiveBroadcastUI();
            }
        }

        private void BringClassroomHubToFront()
        {
            try
            {
                var app = Application.Current as QASmartTouch.App;
                if (app != null && app.ModeService.CurrentMode != QASmartClass.Shared.AppMode.SmartClass)
                {
                    app.ModeService.GoToClass();
                }

                var shell = (Window.GetWindow(this) as ClassroomShell)
                    ?? Application.Current?.Windows.OfType<ClassroomShell>().FirstOrDefault();
                if (shell != null)
                {
                    if (shell.WindowState == WindowState.Minimized)
                        shell.WindowState = WindowState.Maximized;
                    if (!shell.IsVisible)
                        shell.Show();
                    shell.Activate();
                    shell.NavigateTo("FHUB");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[ClassControlHub] BringClassroomHubToFront error: {Err}", ex.Message);
            }
        }

        private void OnQuickBroadcastActionRequested(object? sender, QuickBroadcastAction action)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    switch (action)
                    {
                        case QuickBroadcastAction.ToggleQuickShare:
                            EnsureBroadcastPageLoaded().ToggleMythwareQuickBroadcast();
                            UpdateLiveBroadcastUI();
                            break;

                        case QuickBroadcastAction.PickScreenOrWindow:
                            EnsureBroadcastPageLoaded().StartOrSwitchSourceFromFloatingBar();
                            UpdateLiveBroadcastUI();
                            break;

                        case QuickBroadcastAction.PickRegion:
                            EnsureBroadcastPageLoaded().StartOrSwitchRegionFromFloatingBar();
                            UpdateLiveBroadcastUI();
                            break;

                        case QuickBroadcastAction.PickQaPage:
                            EnsureBroadcastPageLoaded().StartOrSwitchQaPageFromFloatingBar();
                            UpdateLiveBroadcastUI();
                            break;

                        case QuickBroadcastAction.ShareSmartTouchNow:
                            EnsureBroadcastPageLoaded().ShareSmartTouchBoardImmediately();
                            UpdateLiveBroadcastUI();
                            break;

                        case QuickBroadcastAction.TogglePause:
                            EnsureBroadcastPageLoaded().TogglePauseFromFloatingBar();
                            UpdateLiveBroadcastUI();
                            break;

                        case QuickBroadcastAction.ShowFloatingPip:
                            EnsureBroadcastPageLoaded().ShowFloatingPipFromFloatingBar();
                            break;

                        case QuickBroadcastAction.OpenBroadcastHub:
                            BringClassroomHubToFront();
                            SelectTab("Broadcast");
                            EnsureBroadcastPageLoaded().SelectBroadcastSubTab("screen");
                            break;

                        case QuickBroadcastAction.OpenNoticeTab:
                            BringClassroomHubToFront();
                            SelectTab("Broadcast");
                            EnsureBroadcastPageLoaded().SelectBroadcastSubTab("notice");
                            break;

                        case QuickBroadcastAction.OpenWebsiteTab:
                            BringClassroomHubToFront();
                            SelectTab("Web");
                            break;

                        case QuickBroadcastAction.OpenStudentDemo:
                            BringClassroomHubToFront();
                            SelectTab("Monitor");
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                EnsureMonitorPageLoaded()?.TriggerVeyonStudentDemo();
                            }), System.Windows.Threading.DispatcherPriority.Loaded);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[ClassControlHub] Lỗi xử lý QuickBroadcastAction={Action}", action);
                    ClassroomDialog.Error($"Không thể thực hiện tác vụ quảng bá: {ex.Message}", "Lỗi Quảng bá");
                }
            });
        }

        private async void QuickLock_Click(object sender, RoutedEventArgs e)
        {
            /* // [MYTHWARE_LITE] Cho phép khóa máy ngay lập tức mà không bắt buộc phải bấm "Bắt đầu phiên học"
            if (!_sessionService.IsSessionActive)
            {
                ClassroomDialog.Warn("Vui lòng bắt đầu phiên học trước khi thực hiện thao tác!", "Thông báo");
                return;
            }
            */

            try
            {
                if (ClassroomAppContext.Network?.IsBroadcasting == true)
                {
                    await ClassroomAppContext.Network.SendCommandAsync("CMD|LOCK_SCREEN");
                }
                if (_sessionService.IsSessionActive)
                {
                    await _sessionService.LockAllAsync();
                }
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
            /* // [MYTHWARE_LITE]
            if (!_sessionService.IsSessionActive)
            {
                ClassroomDialog.Warn("Vui lòng bắt đầu phiên học trước khi thực hiện thao tác!", "Thông báo");
                return;
            }
            */

            try
            {
                if (ClassroomAppContext.Network?.IsBroadcasting == true)
                {
                    await ClassroomAppContext.Network.SendCommandAsync("CMD|UNLOCK_SCREEN");
                }
                if (_sessionService.IsSessionActive)
                {
                    await _sessionService.UnlockAllAsync();
                }
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
            /* // [MYTHWARE_LITE]
            if (!_sessionService.IsSessionActive)
            {
                ClassroomDialog.Warn("Vui lòng bắt đầu phiên học trước khi thực hiện thao tác!", "Thông báo");
                return;
            }
            */

            try
            {
                if (_sessionService.IsSilenceActive)
                {
                    if (ClassroomAppContext.Network?.IsBroadcasting == true)
                    {
                        await ClassroomAppContext.Network.SendCommandAsync("CMD|CLEAR_SILENCE");
                        await ClassroomAppContext.Network.SendCommandAsync("CMD|MUTE_AUDIO|0");
                    }
                    await _sessionService.ClearSilenceAllAsync();
                    ClassroomDialog.Info("🔊 Đã gỡ bỏ trạng thái im lặng trên toàn bộ máy Học sinh (Windows & Android)!", "Mở im lặng");
                }
                else
                {
                    if (ClassroomAppContext.Network?.IsBroadcasting == true)
                    {
                        await ClassroomAppContext.Network.SendCommandAsync("CMD|SILENCE");
                        await ClassroomAppContext.Network.SendCommandAsync("CMD|MUTE_AUDIO|1");
                    }
                    await _sessionService.SilenceAllAsync();
                    ClassroomDialog.Info("🔇 Đã kích hoạt chế độ im lặng trên các máy Học sinh (Windows & Android)!", "Im lặng");
                }
                UpdateSessionStatusUI();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi thay đổi chế độ im lặng");
                ClassroomDialog.Error($"Không thể thực hiện: {ex.Message}", "Lỗi");
            }
        }

        private MonitorPage? EnsureMonitorPageLoaded()
        {
            if (frameMonitor.Content == null)
            {
                frameMonitor.Navigate(new MonitorPage());
            }
            return frameMonitor.Content as MonitorPage;
        }

        private void VeyonStudentDemo_Click(object sender, RoutedEventArgs e)
        {
            SelectTab("Monitor");
            Dispatcher.BeginInvoke(new Action(() =>
            {
                EnsureMonitorPageLoaded()?.TriggerVeyonStudentDemo();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void VeyonRemoteView_Click(object sender, RoutedEventArgs e)
        {
            SelectTab("Monitor");
            Dispatcher.BeginInvoke(new Action(() =>
            {
                EnsureMonitorPageLoaded()?.TriggerVeyonRemoteView();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void VeyonRemoteControl_Click(object sender, RoutedEventArgs e)
        {
            SelectTab("Monitor");
            Dispatcher.BeginInvoke(new Action(() =>
            {
                EnsureMonitorPageLoaded()?.TriggerVeyonRemoteControl();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void VeyonTextMessage_Click(object sender, RoutedEventArgs e)
        {
            SelectTab("Monitor");
            Dispatcher.BeginInvoke(new Action(() =>
            {
                EnsureMonitorPageLoaded()?.TriggerVeyonTextMessage();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void VeyonStartApp_Click(object sender, RoutedEventArgs e)
        {
            SelectTab("Monitor");
            Dispatcher.BeginInvoke(new Action(() =>
            {
                EnsureMonitorPageLoaded()?.TriggerVeyonStartApp();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void VeyonScreenshot_Click(object sender, RoutedEventArgs e)
        {
            SelectTab("Monitor");
            Dispatcher.BeginInvoke(new Action(() =>
            {
                EnsureMonitorPageLoaded()?.TriggerVeyonScreenshot();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void PillStudentRoster_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PopulateConnectedStudentsPopup();
            popupConnectedStudents.IsOpen = !popupConnectedStudents.IsOpen;
        }

        private void PopupGoToMonitor_Click(object sender, RoutedEventArgs e)
        {
            popupConnectedStudents.IsOpen = false;
            SelectTab("Monitor");
        }

        private void PopulateConnectedStudentsPopup()
        {
            if (pnlPopupStudents == null || txtPopupStudentHeader == null) return;
            pnlPopupStudents.Children.Clear();

            var clients = ClassroomAppContext.Network?.GetConnectedStudents() ?? Array.Empty<Services.ClientInfo>();
            txtPopupStudentHeader.Text = $"👥 Danh sách Học sinh ({clients.Count})";

            if (clients.Count == 0)
            {
                pnlPopupStudents.Children.Add(new TextBlock
                {
                    Text = "Hiện chưa có học sinh nào kết nối vào lớp học.",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(4, 10, 4, 10),
                    TextWrapping = TextWrapping.Wrap
                });
                return;
            }

            foreach (var c in clients)
            {
                bool offTask = !c.IsFocusedInClass;
                string icon = c.IsAndroid ? "🤖" : "🪟";
                string statusText = offTask
                    ? $"⚠️ Đang ở: {ConnectedStudent.FriendlyAppName(string.IsNullOrWhiteSpace(c.ForegroundApp) ? "unknown" : c.ForegroundApp)}"
                    : (string.IsNullOrWhiteSpace(c.CurrentTabPage) ? "🟢 Đang trong lớp học" : $"🟢 Trong lớp • {c.CurrentTabPage}");

                var card = new Border
                {
                    Background = offTask
                        ? new SolidColorBrush(Color.FromRgb(254, 242, 242))
                        : new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    BorderBrush = offTask
                        ? new SolidColorBrush(Color.FromRgb(252, 165, 165))
                        : new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 6, 8, 6),
                    Margin = new Thickness(0, 0, 0, 5),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = c.Code
                };

                var sp = new StackPanel();
                var topDock = new DockPanel { LastChildFill = true };
                var codeText = new TextBlock
                {
                    Text = $"{icon} {c.PCName} • {c.IPAddress}",
                    FontSize = 10.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(codeText, Dock.Right);
                topDock.Children.Add(codeText);

                topDock.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(c.Name) ? c.Code : c.Name,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });

                sp.Children.Add(topDock);
                sp.Children.Add(new TextBlock
                {
                    Text = statusText,
                    FontSize = 11,
                    FontWeight = offTask ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = offTask
                        ? new SolidColorBrush(Color.FromRgb(185, 28, 28))
                        : new SolidColorBrush(Color.FromRgb(5, 150, 105)),
                    Margin = new Thickness(0, 2, 0, 0)
                });

                card.Child = sp;
                string studentCode = c.Code;
                card.MouseLeftButtonDown += (_, _) =>
                {
                    popupConnectedStudents.IsOpen = false;
                    SelectTab("Monitor");
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        EnsureMonitorPageLoaded()?.OpenStudentActionByCode(studentCode);
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                };

                pnlPopupStudents.Children.Add(card);
            }
        }

        private void VeyonPowerControl_Click(object sender, RoutedEventArgs e)
        {
            SelectTab("Monitor");
            var targetBtn = sender as UIElement;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                EnsureMonitorPageLoaded()?.TriggerVeyonPowerMenu(targetBtn);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        #endregion
    }
}
