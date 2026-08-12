using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using QASmartClass.Data;
using Serilog;
using QASmartClass.Classroom.Services;

namespace QASmartClass.Classroom.Views
{
    public partial class BroadcastPage : Page
    {
        private bool _isBroadcasting = false;
        private bool _isPaused = false;
        private bool _isTextMode = false;
        private bool _isMulticastMode = false;
        private System.Diagnostics.Process? _vncServerProcess = null;
        private bool _isVncBroadcasting = false;
        private int _vncPort = 5901;
        private string _vncSessionCode = string.Empty;
        private DispatcherTimer? _timer;
        private int _seconds = 0;
        private string _currentEmergencyOtp = string.Empty; // === UPGRADE_11: OTP bypass ===
        private string? _selectedFilePath;
        private System.Collections.Generic.List<QASmartClass.Data.Student>? _cachedStudents;
        private bool _isBroadcastPausedDueToPrivacy = false;
        private System.Windows.Threading.DispatcherTimer? _cacheRefreshTimer;
        private int _cacheRefreshTickCount = 0;
        private BroadcastPrivacyBorder? _privacyBorder;
        private System.Collections.Generic.HashSet<string> _dynamicWhitelist = new System.Collections.Generic.HashSet<string>();
        private System.IO.FileSystemWatcher? _configWatcher;
        private static ImageSource? _cachedPlaceholder;

        public BroadcastPage()
        {
            InitializeComponent();
            LoadDynamicWhitelist();
            SetupConfigWatcher();
            // btnTurboBroadcast.Visibility = QASmartTouch.Services.AppSettings.Broadcast_AllowTurboMode ? Visibility.Visible : Visibility.Collapsed; // Tạm ẩn theo yêu cầu

            _cacheRefreshTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _cacheRefreshTimer.Tick += CacheRefreshTimer_Tick;

            Loaded += async (s, ev) =>
            {
                _cacheRefreshTimer.Start();
                try
                {
                    _cachedStudents = await Task.Run(() => GetStudentsForCurrentContext());
                }
                catch (Exception ex) { Log.Warning("Failed to cache students: {Err}", ex.Message); }

                Dispatcher.Invoke(() =>
                {
                    LoadRecipientCount();

                    var state = QASmartClass.Services.BroadcastStateService.Instance;
                    if (state.TargetSelectionMode == "GROUP")
                    {
                        rbGroup.IsChecked = true;
                        LoadGroupList();
                    }
                    else if (state.TargetSelectionMode == "INDIVIDUAL")
                    {
                        rbIndividual.IsChecked = true;
                        LoadStudentList();
                    }
                    else
                    {
                        rbAllStudents.IsChecked = true;
                    }
                    UpdateRecipientCount();
                });

                // Tải lại lịch sử thông báo đã gửi từ SQLite Database
                LoadNoticeHistoryFromDb();

                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.PreviewKeyDown -= Window_PreviewKeyDown;
                    window.PreviewKeyDown += Window_PreviewKeyDown;
                }

                // Subscribe VNC events (LOI_VID_46)
                QASmartClass.Services.VncBroadcastService.Instance.StatusChanged += OnVncBroadcastStatusChanged;
                QASmartClass.Services.VncBroadcastService.Instance.TimerTick += OnVncBroadcastTimerTick;

                // Sync VNC UI state
                SyncVncUiState(QASmartClass.Services.VncBroadcastService.Instance.IsVncBroadcasting);
            };
            Unloaded += (_, _) =>
            {
                _cacheRefreshTimer.Stop();
                ApplyScreenCaptureExclusion(false);
                _isBroadcasting = false;
                _isPaused = false;
                _isBroadcastPausedDueToPrivacy = false;
                _timer?.Stop();
                _captureTimer?.Dispose();

                // Dọn dẹp VNC Server khi thoát trang - KHÔNG dừng VNC ở đây để chạy ngầm (LOI_VID_46)
                if (!_isVncBroadcasting)
                {
                    StopVncProcessOnly();
                }
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.PreviewKeyDown -= Window_PreviewKeyDown;
                }
                if (_privacyBorder != null)
                {
                    _privacyBorder.Close();
                    _privacyBorder = null;
                }
                try
                {
                    if (_configWatcher != null)
                    {
                        _configWatcher.EnableRaisingEvents = false;
                        _configWatcher.Dispose();
                        _configWatcher = null;
                    }
                }
                catch (Exception ex) { Log.Debug("[BroadcastPage][Unloaded] ConfigWatcher dispose: {Err}", ex.Message); }

                // Unsubscribe VNC events (LOI_VID_46)
                QASmartClass.Services.VncBroadcastService.Instance.StatusChanged -= OnVncBroadcastStatusChanged;
                QASmartClass.Services.VncBroadcastService.Instance.TimerTick -= OnVncBroadcastTimerTick;
            };
        }

        internal void StopAllBroadcasting()
        {
            if (_isBroadcasting)
            {
                Broadcast_Click(this, new RoutedEventArgs());
            }
            if (_isVncBroadcasting)
            {
                StopVncBroadcast();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Alt)
            {
                e.Handled = true;
                txtSearchStudent?.Focus();
                return;
            }

            var focusedElement = FocusManager.GetFocusedElement(this);
            bool isTyping = focusedElement is TextBox;

            if (!isTyping)
            {
                if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Alt)
                {
                    e.Handled = true;
                    if (rbIndividual.IsChecked == true) SelectAllStudents_Click(this, new RoutedEventArgs());
                    else if (rbGroup.IsChecked == true) SelectAllGroups_Click(this, new RoutedEventArgs());
                    return;
                }
                else if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Alt)
                {
                    e.Handled = true;
                    if (rbIndividual.IsChecked == true) DeselectAllStudents_Click(this, new RoutedEventArgs());
                    else if (rbGroup.IsChecked == true) DeselectAllGroups_Click(this, new RoutedEventArgs());
                    return;
                }
            }

            if (e.Key == Key.S && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                e.Handled = true;
                Broadcast_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.P && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                e.Handled = true;
                _isBroadcastPausedDueToPrivacy = !_isBroadcastPausedDueToPrivacy;
                brdFreezeOverlay.Visibility = _isBroadcastPausedDueToPrivacy ? Visibility.Visible : Visibility.Collapsed;
                _privacyBorder?.SetState(_isPaused || _isBroadcastPausedDueToPrivacy);
                if (_isBroadcastPausedDueToPrivacy)
                {
                    // V22-16: Dừng capture timer thực sự — HS không nhận frame mới
                    _captureTimer?.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                    ShowToast("Đã tạm dừng phát màn hình (Riêng tư)", "#C62828");
                }
                else
                {
                    // V22-16: Tiếp tục capture timer
                    int captureMs = _captureTimer != null ? 500 : 1000; // fallback
                    try
                    {
                        var fpsItem = cmbFPS.SelectedItem as ComboBoxItem;
                        if (fpsItem?.Tag is string fpsTag && int.TryParse(fpsTag, out int fpsVal) && fpsVal > 0)
                        {
                            captureMs = 1000 / fpsVal;
                        }
                        else if (fpsItem != null)
                        {
                            var fpsDigits = new string(fpsItem.Content.ToString().Where(char.IsDigit).ToArray());
                            if (int.TryParse(fpsDigits, out int fps) && fps > 0) captureMs = 1000 / fps;
                        }
                    }
                    catch { }
                    _captureTimer?.Change(0, captureMs);
                    ShowToast("Đã tiếp tục chiếu màn hình", "#2E7D32");
                }
            }
        }

        private System.Collections.Generic.List<QASmartClass.Data.Student> GetStudentsForCurrentContext()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                string activeClass = app.ClassroomSession?.CurrentClassName;
                if (string.IsNullOrEmpty(activeClass))
                {
                    activeClass = app.ClassRoster.ActiveRoster?.ClassName;
                }

                using (var db = new QASmartClass.Data.AppDbContext())
                {
                    if (!string.IsNullOrEmpty(activeClass))
                    {
                        var filtered = db.Students?
                            .Where(s => s.ClassName == activeClass)
                            .ToList();
                        if (filtered != null && filtered.Any())
                        {
                            return VietnameseNameHelper.SortByVietnameseName(filtered, s => s.FullName);
                        }
                    }

                    if (app.ClassRoster.ActiveRoster != null)
                    {
                        var activeRosterId = app.ClassRoster.ActiveRoster.Id;
                        var links = db.ClassRosterStudents
                            .Where(rs => rs.RosterId == activeRosterId)
                            .ToList();

                        var studentIds = links.Select(rs => rs.StudentId).ToList();
                        var rosterStudents = db.Students
                            .Where(s => studentIds.Contains(s.Id))
                            .ToList();

                        var linkMap = links.ToDictionary(l => l.StudentId);
                        foreach (var s in rosterStudents)
                        {
                            if (linkMap.TryGetValue(s.Id, out var link))
                                s.SeatNumber = link.SeatNumber;
                        }

                        if (rosterStudents.Any())
                        {
                            return VietnameseNameHelper.SortByVietnameseName(rosterStudents, s => s.FullName);
                        }
                    }

                    var all = db.Students?.ToList() ?? new System.Collections.Generic.List<QASmartClass.Data.Student>();
                    return VietnameseNameHelper.SortByVietnameseName(all, s => s.FullName);
                }
            }
            catch
            {
                return new System.Collections.Generic.List<QASmartClass.Data.Student>();
            }
        }

        private void CacheRefreshTimer_Tick(object? sender, EventArgs e)
        {
            _cacheRefreshTickCount++;
            if (_cacheRefreshTickCount >= 20) // 10 phút (20 * 30 giây)
            {
                _cacheRefreshTickCount = 0;
                try
                {
                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();
                    System.GC.Collect();
                    Log.Information("[BroadcastPage] Triggered 10-minute scheduled memory garbage collection.");
                }
                catch (Exception ex)
                {
                    Log.Debug("[BroadcastPage] GC collection error: {Err}", ex.Message);
                }
            }

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var list = GetStudentsForCurrentContext();
                    if (list != null) _cachedStudents = list;
                }
                catch (Exception ex) { Log.Warning("[BroadcastPage][LoadStudents] Cache students error: {Err}", ex.Message); }
            }).ContinueWith(t => 
            {
                Dispatcher.Invoke(() => LoadRecipientCount());
            }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void LoadRecipientCount()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                int count = 0;
                if (app.NetworkService != null)
                {
                    count = app.NetworkService.ConnectedCount;
                }
                else if (_cachedStudents != null)
                {
                    count = _cachedStudents.Count(s => s.IsOnline);
                }
                else
                {
                    count = GetStudentsForCurrentContext().Count(s => s.IsOnline);
                }
                txtRecipientCount.Text = $"→ {count} máy HS sẽ nhận";
            }
            catch { txtRecipientCount.Text = "→ 0 máy HS sẽ nhận"; }
        }

        // ═══════════════════════════════════════════════════════════
        //  TAB SWITCHING
        // ═══════════════════════════════════════════════════════════

        private void Tab_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string tag)
            {
                var tabs = new[] { tabScreen, tabNotice, tabMedia, tabWeb };
                foreach (var t in tabs)
                {
                    t.Background = Brushes.White;
                    t.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                }
                border.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210));

                panelScreen.Visibility = tag == "screen" ? Visibility.Visible : Visibility.Collapsed;
                panelNotice.Visibility = tag == "notice" ? Visibility.Visible : Visibility.Collapsed;
                panelMedia.Visibility = tag == "media" ? Visibility.Visible : Visibility.Collapsed;
                panelWeb.Visibility = tag == "web" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TAB 1: SCREEN BROADCAST — Chụp + phát màn hình thực
        // ═══════════════════════════════════════════════════════════

        private System.Threading.Timer? _captureTimer;
        private readonly System.Threading.SemaphoreSlim _captureLock = new(1, 1);
        private string _captureDir = "";
        private bool _isFirstCapture = true;
        private string _cachedCompressionFormat = "WebP";
        private OpenCvSharp.VideoCapture? _cameraCapture;
        private System.Windows.Threading.DispatcherTimer? _cameraPreviewTimer;

        private async void Broadcast_Click(object sender, RoutedEventArgs e)
        {
            if (_isBroadcasting)
            {
                // === STOP ===
                _timer?.Stop();
                _captureTimer?.Dispose();
                ApplyScreenCaptureExclusion(false);
                _isBroadcasting = false;
                _isPaused = false;
                _seconds = 0;
                
                // Dừng bộ giám sát tài nguyên (V2.2.4)
                QASmartClass.Services.AppPerformanceMonitor.Instance.Stop();
                
                // Reset nút 1: Phát thường
                btnBroadcast.Content = "▶️ Phát Thường";
                btnBroadcast.Background = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                btnBroadcast.IsEnabled = true;
                
                // Reset nút 2: Phát siêu tốc
                QASmartTouch.Services.AppSettings.Broadcast_TurboMode = false;
                btnTurboBroadcast.Content = "🚀 Phát Siêu Tốc";
                btnTurboBroadcast.IsEnabled = true;
                
                // Reset nút 3: Phát chuyên chữ
                _isTextMode = false;
                btnTextBroadcast.Content = "📄 Phát Chuyên Chữ";
                btnTextBroadcast.IsEnabled = true;
                
                // Reset nút 4: Phát đa thiết bị
                _isMulticastMode = false;
                btnMulticastBroadcast.Content = "📡 Phát Đa Thiết Bị";
                btnMulticastBroadcast.IsEnabled = true;
                
                // Mở khóa lại các tùy chọn chất lượng để cấu hình
                cmbResolution.IsEnabled = true;
                cmbFPS.IsEnabled = true;
                cmbQuality.IsEnabled = true;
                
                btnPause.Visibility = Visibility.Collapsed;

                if (_privacyBorder != null)
                {
                    _privacyBorder.Close();
                    _privacyBorder = null;
                }

                screenIdleState.Visibility = Visibility.Visible;
                screenLiveState.Visibility = Visibility.Collapsed;
                txtStatus.Text = "Đã dừng phát";
                // V22-14: Status color đỏ khi dừng
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));

                try
                {
                    var app2 = (QASmartTouch.App)Application.Current;
                    var stopCmd1 = "CMD|SCREEN_BROADCAST_STOP|0";
                    var stopCmd2 = "CMD|BROADCAST_STOP|0";

                    // Luôn gửi qua local bus
                    app2.RaiseLocalCommand(stopCmd1);
                    app2.RaiseLocalCommand(stopCmd2);

                    // Nếu có network → gửi thêm qua mạng
                    if (app2.NetworkService?.IsBroadcasting == true)
                    {
                        // === UPGRADE_06 + UPGRADE_10: Gửi STOP cho target students trước ===
                        try { await SendCommandToTargetsAsync(stopCmd1); } catch { /* ignore */ }
                        try { await SendCommandToTargetsAsync(stopCmd2); } catch { /* ignore */ }

                        // Fallback: vẫn gửi ALL để đảm bảo không HS nào bị kẹt (Safety-first)
                        await app2.NetworkService.SendCommandAsync(stopCmd1);
                        await app2.NetworkService.SendCommandAsync(stopCmd2);
                    }
                    _currentEmergencyOtp = string.Empty; // === UPGRADE_11: Reset OTP ===
                    QASmartTouch.App.BroadcastState.IsScreenBroadcastActive = false;
                    QASmartTouch.App.BroadcastState.ScreenCapturePath = string.Empty;
                    QASmartClass.Services.BroadcastStateService.Instance.Reset();

                    // Giải phóng bộ nhớ lập tức khi dừng phát
                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();
                    System.GC.Collect();
                }
                catch (Exception ex) { Log.Warning("[BroadcastPage][StopBroadcast] Reset state error: {Err}", ex.Message); }

                SaveEvent("BROADCAST", "GV", "Dừng phát màn hình");
                Log.Information("Broadcast stopped");
                return;
            }

            // === START ===
            // UI-06 FIX: Cảnh báo khi 0 HS kết nối
            try
            {
                var appStart = (QASmartTouch.App)Application.Current;
                var targetCodes = GetTargetStudentCodes();
                int recipientCount = 0;
                if (targetCodes == null)
                {
                    recipientCount = appStart.NetworkService?.ConnectedCount ?? 0;
                }
                else
                {
                    recipientCount = targetCodes.Count;
                }

                // === UPGRADE_06: Phân biệt rõ null (ALL) vs empty list (chưa chọn ai) ===
                if (targetCodes != null && targetCodes.Count == 0)
                {
                    // Trường hợp: GV chọn "Nhóm" hoặc "Từng HS" nhưng không tick ai
                    MessageBox.Show(
                        "⚠️ Chưa chọn đối tượng nhận chiếu màn hình.\n\n" +
                        "Vui lòng chọn ít nhất 1 học sinh hoặc 1 nhóm trước khi bắt đầu phát.",
                        "Lỗi cấu hình — Không có đối tượng nhận",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    Log.Warning("[BroadcastPage] UPGRADE_06: Target list rỗng (Group/Individual mode nhưng chưa tick HS). Chặn broadcast.");
                    return;
                }

                if (recipientCount == 0)
                {
                    var warnResult = MessageBox.Show(
                        "[Cảnh báo] HIỆN CHƯA CÓ HỌC SINH NÀO NHẬN\n\n" +
                        "Bạn chưa chọn học sinh hoặc chưa có học sinh nào kết nối.\n" +
                        "Bạn có chắc chắn muốn tiếp tục phát màn hình?",
                        "Cảnh báo — Không có học sinh",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    if (warnResult != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug("[BroadcastPage][ZeroHS] Check error: {Err}", ex.Message);
            }

            // Xác nhận lại số lượng học sinh nhận trình chiếu khi không bật Force Watch
            if (chkForceWatch.IsChecked == false)
            {
                try
                {
                    var appStart = (QASmartTouch.App)Application.Current;
                    string activeClass = appStart.ClassroomSession?.CurrentClassName;
                    if (string.IsNullOrEmpty(activeClass))
                    {
                        activeClass = appStart.ClassRoster.ActiveRoster?.ClassName;
                    }
                    string classSuffix = !string.IsNullOrEmpty(activeClass) ? $" lớp {activeClass}" : "";

                    string confirmMsg = "";
                    if (rbAllStudents.IsChecked == true)
                    {
                        int total = _cachedStudents?.Count ?? GetStudentsForCurrentContext().Count;
                        confirmMsg = $"Bạn đang chuẩn bị trình chiếu màn hình tới tất cả học sinh (Tổng số: {total} máy HS{classSuffix}).\n\nBạn có chắc chắn muốn bắt đầu?";
                    }
                    else if (rbGroup.IsChecked == true)
                    {
                        var selectedGroups = groupCheckList.Children.OfType<CheckBox>().Where(c => c.IsChecked == true).ToList();
                        int totalMembers = 0;
                        if (appStart.CurrentGroups?.Count > 0)
                        {
                            foreach (var cb in selectedGroups)
                            {
                                if (cb.Tag is int idx && idx < appStart.CurrentGroups.Count)
                                    totalMembers += appStart.CurrentGroups[idx].Members.Count;
                            }
                        }
                        confirmMsg = $"Bạn đang chuẩn bị trình chiếu màn hình tới {selectedGroups.Count} nhóm ({totalMembers} học sinh{classSuffix}).\n\nBạn có chắc chắn muốn bắt đầu?";
                    }
                    else // rbIndividual
                    {
                        int hsCount = studentCheckList.Children.OfType<CheckBox>().Count(c => c.IsChecked == true);
                        confirmMsg = $"Bạn đang chuẩn bị trình chiếu màn hình tới {hsCount} học sinh được chọn{classSuffix}.\n\nBạn có chắc chắn muốn bắt đầu?";
                    }

                    bool proceed = QASmartClass.Shared.QAConfirmDialog.ShowDialog(
                        "Xác nhận trình chiếu màn hình",
                        confirmMsg,
                        Window.GetWindow(this));
                    if (!proceed)
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Confirm student count error: {Err}", ex.Message);
                }
            }

            // V22-01: Xác nhận trước khi bật FORCE_WATCH
            if (chkForceWatch.IsChecked == true)
            {
                var app = (QASmartTouch.App)Application.Current;
                string activeClass = app.ClassroomSession?.CurrentClassName;
                string classText = !string.IsNullOrEmpty(activeClass) ? $" lớp {activeClass}" : "";

                int onlineCount = app.NetworkService?.ConnectedCount ?? 0;
                int totalCount = _cachedStudents?.Count ?? GetStudentsForCurrentContext().Count;

                string messageText = $"[Cảnh báo] BẠN ĐANG BẬT CHẾ ĐỘ KHÓA MÀN HÌNH HỌC SINH\n\n" +
                                     $"• {onlineCount}/{totalCount} máy HS{classText} đang online và sẽ nhận\n" +
                                     $"• Học sinh sẽ KHÔNG THỂ chủ động đóng màn hình chiếu\n" +
                                     $"• Tất cả phím tắt thoát (Alt+F4, Alt+Tab) bị vô hiệu hóa\n\n" +
                                     $"Bạn có chắc chắn muốn tiếp tục?";

                bool confirmResult = QASmartClass.Shared.QAConfirmDialog.ShowDialog(
                    "Xác nhận khóa màn hình",
                    messageText,
                    Window.GetWindow(this));

                if (!confirmResult)
                {
                    chkForceWatch.IsChecked = false;
                    return;
                }
            }

            _isBroadcasting = true;
            if (QASmartTouch.Services.AppSettings.Broadcast_TurboMode)
            {
                btnBroadcast.IsEnabled = false;
                btnTextBroadcast.IsEnabled = false;
                btnMulticastBroadcast.IsEnabled = false;
            }
            else if (_isTextMode)
            {
                btnBroadcast.IsEnabled = false;
                btnTurboBroadcast.IsEnabled = false;
                btnMulticastBroadcast.IsEnabled = false;
            }
            else if (_isMulticastMode)
            {
                btnBroadcast.IsEnabled = false;
                btnTurboBroadcast.IsEnabled = false;
                btnTextBroadcast.IsEnabled = false;
            }
            else
            {
                btnTurboBroadcast.IsEnabled = false;
                btnTextBroadcast.IsEnabled = false;
                btnMulticastBroadcast.IsEnabled = false;
            }

            // Giải phóng bộ nhớ lập tức khi bắt đầu phát
            try
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
            }
            catch { }

            // === UPGRADE_13: Không bật Exclusion khi chạy ở chế độ Turbo (nếu nhà trường cấu hình cho phép tắt exclusion để tối ưu hiệu năng capture của DWM) ===
            bool shouldExclude = QASmartTouch.Services.AppSettings.Broadcast_EnableScreenExclusion;
            if (QASmartTouch.Services.AppSettings.Broadcast_TurboMode && QASmartTouch.Services.AppSettings.Broadcast_TurboDisableExclusion)
            {
                shouldExclude = false;
            }
            if (shouldExclude)
            {
                ApplyScreenCaptureExclusion(true);
            }
            _isFirstCapture = true;

            // CR-06 FIX: Cache compression format once at start instead of querying DB every frame
            try
            {
                using (var db = new QASmartClass.Data.AppDbContext())
                {
                    var formatSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Network_ScreenCompressionFormat");
                    if (formatSetting != null) _cachedCompressionFormat = formatSetting.Value;
                    else _cachedCompressionFormat = "WebP";
                }
            }
            catch { _cachedCompressionFormat = "WebP"; }
            _seconds = 0;

            var resolution = (cmbResolution.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1920×1080";
            var fps = (cmbFPS.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "30 fps";
            var source = rbFullScreen.IsChecked == true ? "Toàn màn hình" : rbCamera.IsChecked == true ? "Camera" : "Cửa sổ";

            // Tạo thư mục lưu ảnh capture
            _captureDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "QASmartClass_Broadcast");
            if (!System.IO.Directory.Exists(_captureDir))
                System.IO.Directory.CreateDirectory(_captureDir);

            // UI
            screenIdleState.Visibility = Visibility.Collapsed;
            screenLiveState.Visibility = Visibility.Visible;
            txtRes.Text = $"{resolution}  {fps}";
            btnPause.Visibility = Visibility.Visible;
            if (QASmartTouch.Services.AppSettings.Broadcast_TurboMode)
            {
                btnTurboBroadcast.Content = "\u23F9\uFE0F Dừng Siêu Tốc";
                btnTurboBroadcast.Background = new SolidColorBrush(Color.FromRgb(211, 47, 47));
            }
            else if (_isTextMode)
            {
                btnTextBroadcast.Content = "\u23F9\uFE0F Dừng Chuyên Chữ";
                btnTextBroadcast.Background = new SolidColorBrush(Color.FromRgb(211, 47, 47));
            }
            else if (_isMulticastMode)
            {
                btnMulticastBroadcast.Content = "\u23F9\uFE0F Dừng Đa Thiết Bị";
                btnMulticastBroadcast.Background = new SolidColorBrush(Color.FromRgb(211, 47, 47));
            }
            else
            {
                btnBroadcast.Content = "\u23F9\uFE0F Dừng Phát";
                btnBroadcast.Background = new SolidColorBrush(Color.FromRgb(211, 47, 47));
            }
            txtPreviewLabel.Text = $"Đang chiếu: {source}";
            txtBroadcastStatus.Text = "Đang chụp & gửi hình ảnh tới HS...";
            txtLiveRecipients.Text = $"{txtRecipientCount.Text.Replace("→ ", "")}";

            // Show privacy border
            _privacyBorder = new BroadcastPrivacyBorder();
            _privacyBorder.Show();
            _privacyBorder.SetState(_isPaused || _isBroadcastPausedDueToPrivacy);

            // Timer đếm thời gian
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, ev) =>
            {
                _seconds++;
                txtTimer.Text = $"{_seconds / 60:D2}:{_seconds % 60:D2}";
                txtStatus.Text = $"Đang phát — {source} — {_seconds / 60:D2}:{_seconds % 60:D2}";
                // V22-14: Status color xanh khi đang phát
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                string otpInfo = !string.IsNullOrEmpty(_currentEmergencyOtp) ? $" | OTP Khẩn cấp: {_currentEmergencyOtp}" : "";
                txtBroadcastInfo.Text = $"{resolution} | {fps} | {txtRecipientCount.Text.Replace("→ ", "")}{otpInfo}";
            };
            _timer.Start();

            // Timer chụp màn hình — interval tùy FPS
            int targetFps = 30;
            try
            {
                var fpsItem = cmbFPS.SelectedItem as ComboBoxItem;
                if (fpsItem?.Tag is string fpsTag && int.TryParse(fpsTag, out int parsedFps) && parsedFps > 0)
                {
                    targetFps = parsedFps;
                }
                else
                {
                    var fpsDigits = new string(fps.Where(char.IsDigit).ToArray());
                    if (int.TryParse(fpsDigits, out int parsedFpsContent) && parsedFpsContent > 0)
                    {
                        targetFps = parsedFpsContent;
                    }
                }
            }
            catch (Exception ex) { Log.Warning("[BroadcastPage][ParseFps] FPS parse error: {Err}", ex.Message); }
            int captureMs = 1000 / targetFps;
            // V22-02: Background thread capture — tránh lag UI ở 30fps
            _captureTimer = new System.Threading.Timer(
                async _ => await CaptureAndSendAsync(),
                null, 0, captureMs);

            // Timer tự gọi lần đầu nhờ dueTime=0, không cần gọi thủ công

            try
            {
                var app = (QASmartTouch.App)Application.Current;
                QASmartTouch.App.BroadcastState.IsScreenBroadcastActive = true;
                string tokenVal = "TK_" + Guid.NewGuid().ToString("N").Substring(0, 16);
                QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken = tokenVal;
                
                // Khởi tạo EncryptionKey từ tokenVal (V2.2.2)
                QASmartClass.Services.UdpScreenBroadcastService.EncryptionKey = tokenVal.Substring(3);

                // Khởi động bộ giám sát tài nguyên (V2.2.4)
                QASmartClass.Services.AppPerformanceMonitor.Instance.Start();

                if (chkForceWatch.IsChecked == true)
                {
                    _currentEmergencyOtp = new Random().Next(100000, 999999).ToString();
                }
                else
                {
                    _currentEmergencyOtp = string.Empty;
                }

                string protocolType = _isTextMode ? "HTTP_PULL" : "UDP_MULTICAST";
                Log.Information("[BroadcastPage] Bắt đầu phiên phát hình. Giao thức={Protocol}, Cổng UDP={Port}, TTL={Ttl}, Key={Key}", 
                    protocolType, 
                    QASmartClass.Services.UdpScreenBroadcastService.MulticastPort, 
                    QASmartClass.Services.UdpScreenBroadcastService.MulticastTTL, 
                    QASmartClass.Services.UdpScreenBroadcastService.EncryptionKey);

                if (app.NetworkService?.IsBroadcasting == true)
                {
                    QASmartClass.Services.UdpScreenBroadcastService.Instance.LoadConfigFromDb();
                    string configCmd = $"CMD|SESSION_CONFIG|Broadcast_UdpHeartbeatTimeout={QASmartTouch.Services.AppSettings.Broadcast_UdpHeartbeatTimeout}|Broadcast_EnableScreenExclusion={QASmartTouch.Services.AppSettings.Broadcast_EnableScreenExclusion}|Broadcast_TurboMode={QASmartTouch.Services.AppSettings.Broadcast_TurboMode}";
                    configCmd += $"|MulticastIP={QASmartClass.Services.UdpScreenBroadcastService.MulticastIP}|MulticastPort={QASmartClass.Services.UdpScreenBroadcastService.MulticastPort}|MulticastTTL={QASmartClass.Services.UdpScreenBroadcastService.MulticastTTL}";
                    configCmd += $"|EncryptionKey={QASmartClass.Services.UdpScreenBroadcastService.EncryptionKey}";
                    if (!string.IsNullOrEmpty(_currentEmergencyOtp))
                    {
                        configCmd += $"|Broadcast_EmergencyOTP={_currentEmergencyOtp}";
                    }
                    Log.Information("[BroadcastPage] Gửi TCP SESSION_CONFIG: {Cmd}", configCmd);
                    _ = app.NetworkService.SendCommandAsync(configCmd);
                }
            }
            catch (Exception ex) { Log.Warning("[BroadcastPage][StartBroadcast] Set token / session config error: {Err}", ex.Message); }

            SaveEvent("BROADCAST", "GV", $"Bắt đầu phát: {source} {resolution} {fps}");
            Log.Information("Broadcast started: {Src} {Res} {FPS}", source, resolution, fps);
        }

        /// <summary>
        /// Chụp màn hình + gửi ảnh cho HS qua SCREEN_BROADCAST / SCREEN_BROADCAST_UPDATE
        /// Sử dụng Win32 API + double-buffered files để tránh file lock
        /// </summary>
        private int _captureIndex = -1;

        // V22-02: Async + SemaphoreSlim guard chống overlap
        private async Task CaptureAndSendAsync()
        {
            if (!_isBroadcasting || _isPaused || _isBroadcastPausedDueToPrivacy) return;
            if (!await _captureLock.WaitAsync(100)) return; // === PHASE6-GD4: Timeout 100ms thay vì 0ms để tránh deadlock ===
            try
            {
                // === PHASE6-GD4: Lấy TẤT CẢ UI properties trên UI thread qua Dispatcher.Invoke ===
                int screenW = 0, screenH = 0;
                double scaleX = 1, scaleY = 1;
                bool isWindowSource = false;
                IntPtr targetHWnd = IntPtr.Zero;
                int targetWidth = 1280;

                Dispatcher.Invoke(() =>
                {
                    // === PHASE6-GD4: SystemParameters + VisualTreeHelper PHẢI truy cập trên UI thread ===
                    screenW = (int)SystemParameters.PrimaryScreenWidth;
                    screenH = (int)SystemParameters.PrimaryScreenHeight;
                    var dpiScale = VisualTreeHelper.GetDpi(this);
                    scaleX = dpiScale.DpiScaleX;
                    scaleY = dpiScale.DpiScaleY;

                    isWindowSource = rbWindow.IsChecked == true;
                    if (isWindowSource && cmbWindowList.SelectedItem is ComboBoxItem item && item.Tag is int pId)
                    {
                        try
                        {
                            var proc = System.Diagnostics.Process.GetProcessById(pId);
                            targetHWnd = proc.MainWindowHandle;
                        }
                        catch (Exception ex) { Log.Debug("[BroadcastPage][CaptureWindow] GetProcessById error: {Err}", ex.Message); }
                    }
                    
                    if (cmbResolution.SelectedItem is ComboBoxItem resItem)
                    {
                        string resText = resItem.Content.ToString();
                        if (resText.Contains("1920")) targetWidth = 1920;
                        else if (resText.Contains("2560")) targetWidth = 2560;
                        else targetWidth = 1280;
                    }
                });

                // Chạy logic chụp & lưu file trên thread pool để tránh block UI thread
                await System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        int x = 0, y = 0;
                        int pixelW = (int)(screenW * scaleX);
                        int pixelH = (int)(screenH * scaleY);

                        if (isWindowSource && targetHWnd != IntPtr.Zero)
                        {
                            NativeMethods.RECT rect;
                            if (NativeMethods.GetWindowRect(targetHWnd, out rect))
                            {
                                // Phục hồi cửa sổ nếu bị thu nhỏ
                                NativeMethods.ShowWindow(targetHWnd, 9); // SW_RESTORE = 9

                                x = rect.Left;
                                y = rect.Top;
                                pixelW = rect.Right - rect.Left;
                                pixelH = rect.Bottom - rect.Top;

                                // Kiểm tra biên an toàn
                                if (pixelW <= 0 || pixelH <= 0)
                                {
                                    x = 0; y = 0;
                                    pixelW = (int)(screenW * scaleX);
                                    pixelH = (int)(screenH * scaleY);
                                }
                            }
                        }

                        if (pixelW <= 0 || pixelH <= 0) return;

                        // HI-03 FIX: Initialize GDI handles before try block for guaranteed cleanup
                        IntPtr hdcSrc = IntPtr.Zero;
                        IntPtr hdcDest = IntPtr.Zero;
                        IntPtr hBitmap = IntPtr.Zero;
                        IntPtr hOld = IntPtr.Zero;
                        System.Drawing.Bitmap? drawingBitmap = null;
                        BitmapSource? bmpSource = null;
                        // CR-06 FIX: Use cached compression format instead of querying DB every frame
                        string compFormat = _cachedCompressionFormat;
                        try
                        {
                            hdcSrc = NativeMethods.GetDC(IntPtr.Zero);
                            hdcDest = NativeMethods.CreateCompatibleDC(hdcSrc);
                            hBitmap = NativeMethods.CreateCompatibleBitmap(hdcSrc, pixelW, pixelH);
                            hOld = NativeMethods.SelectObject(hdcDest, hBitmap);

                            NativeMethods.BitBlt(hdcDest, 0, 0, pixelW, pixelH, hdcSrc, x, y, 0x00CC0020); // SRCCOPY

                            // Convert to BitmapSource
                            bmpSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                hBitmap, IntPtr.Zero, Int32Rect.Empty,
                                BitmapSizeOptions.FromWidthAndHeight(targetWidth, (int)(pixelH * ((double)targetWidth / pixelW))));

                            // Freeze bmpSource TRƯỚC KHI giải phóng tài nguyên GDI để tránh lỗi mất handle
                            bmpSource.Freeze();

                            // ─── UPGRADE_07: Get Drawing Bitmap from hBitmap for WebP encoding ───
                            if (compFormat == "WebP")
                            {
                                try
                                {
                                    drawingBitmap = System.Drawing.Bitmap.FromHbitmap(hBitmap);
                                }
                                catch (Exception exBmp)
                                {
                                    Log.Warning("FromHbitmap failed, fallback to JPEG: {Err}", exBmp.Message);
                                    compFormat = "JPEG";
                                }
                            }
                        }
                        finally
                        {
                            // Cleanup GDI — guaranteed even on exceptions
                            if (hOld != IntPtr.Zero) NativeMethods.SelectObject(hdcDest, hOld);
                            if (hBitmap != IntPtr.Zero) NativeMethods.DeleteObject(hBitmap);
                            if (hdcDest != IntPtr.Zero) NativeMethods.DeleteDC(hdcDest);
                            if (hdcSrc != IntPtr.Zero) NativeMethods.ReleaseDC(IntPtr.Zero, hdcSrc);
                        }

                        int qualityLevel = 65;
                        bool isNetworkActive = false;
                        bool isForceWatch = false;
                        int webPort = 8080;
                        Dispatcher.Invoke(() =>
                        {
                            var app = (QASmartTouch.App)Application.Current;
                            isNetworkActive = app.NetworkService?.IsBroadcasting == true;
                            isForceWatch = chkForceWatch.IsChecked == true;
                            if (app.NetworkService?.WebBridge != null)
                            {
                                webPort = app.NetworkService.WebBridge.WebPort;
                            }
                            if (cmbQuality.SelectedItem is ComboBoxItem qItem && int.TryParse(qItem.Tag?.ToString(), out int qVal))
                            {
                                qualityLevel = qVal;
                            }

                            // Tự động thích ứng chất lượng nén (Auto-scaling quality) dựa trên độ trễ học sinh
                            if (isNetworkActive && app.NetworkService != null)
                            {
                                try
                                {
                                    var clients = app.NetworkService.GetConnectedStudents();
                                    if (clients != null && clients.Count > 0)
                                    {
                                        int maxLatency = 0;
                                        foreach (var c in clients)
                                        {
                                            if (c.LatencyMs > maxLatency)
                                                maxLatency = c.LatencyMs;
                                        }

                                        if (maxLatency > 300)
                                        {
                                            qualityLevel = Math.Max(60, qualityLevel - 20);
                                            Log.Debug("[BroadcastPage] High latency detected ({MaxLatency}ms). Quality auto-scaled down to {Quality}", maxLatency, qualityLevel);
                                        }
                                    }
                                }
                                catch (Exception exLat)
                                {
                                    Log.Debug("Auto-scaling quality error: {Err}", exLat.Message);
                                }
                            }
                        });

                        // Double-buffer: alternating file names to avoid lock
                        int idx = System.Threading.Interlocked.Increment(ref _captureIndex) % 2;
                        string filePath = System.IO.Path.Combine(_captureDir, $"broadcast_{idx}.jpg");

                        // Nén ảnh sang WebP hoặc JPEG
                        byte[] imgBytes;
                        if (compFormat == "WebP" && drawingBitmap != null)
                        {
                            using (drawingBitmap)
                            using (var mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(drawingBitmap))
                            {
                                OpenCvSharp.Cv2.ImEncode(".webp", mat, out imgBytes, new OpenCvSharp.ImageEncodingParam(OpenCvSharp.ImwriteFlags.WebPQuality, qualityLevel));
                            }
                        }
                        else
                        {
                            if (drawingBitmap != null) drawingBitmap.Dispose();
                            var encoder = new JpegBitmapEncoder { QualityLevel = qualityLevel };
                            encoder.Frames.Add(BitmapFrame.Create(bmpSource));
                            using (var ms = new System.IO.MemoryStream())
                            {
                                encoder.Save(ms);
                                imgBytes = ms.ToArray();
                            }
                        }

                        // Lưu ảnh chụp màn hình trực tiếp trên RAM để tránh ghi đĩa cứng
                        QASmartClass.Services.BroadcastStateService.Instance.ScreenCaptureBytes = imgBytes;

                        // Chỉ ghi đĩa nếu chạy ở chế độ cục bộ (không qua mạng LAN)
                        if (!isNetworkActive)
                        {
                            System.IO.File.WriteAllBytes(filePath, imgBytes);
                        }
                        else
                        {
                            _ = QASmartClass.Services.UdpScreenBroadcastService.Instance.SendFrameAsync(imgBytes);
                        }

                        // Gửi command
                        string cmdType = _isFirstCapture ? "SCREEN_BROADCAST_START" : "SCREEN_BROADCAST_UPDATE";
                        string token = QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken;
                        string lessonTitle = "";
                        Dispatcher.Invoke(() => lessonTitle = txtLessonTitle != null ? txtLessonTitle.Text?.Trim() ?? "" : "");

                        // Đồng bộ giao thức và cổng kết nối động bảo mật (V2.2.2)
                        string protocol = "UDP_MULTICAST";
                        if (_isTextMode)
                        {
                            protocol = "HTTP_PULL";
                        }
                        int multicastPort = QASmartClass.Services.UdpScreenBroadcastService.MulticastPort;
                        int ttl = QASmartClass.Services.UdpScreenBroadcastService.MulticastTTL;
                        string key = QASmartClass.Services.UdpScreenBroadcastService.EncryptionKey;

                        string cmd = $"CMD|{cmdType}|{filePath}|PROTOCOL={protocol}|PORT={multicastPort}|KEY={key}|TTL={ttl}|{(isForceWatch ? "FORCE_WATCH|" : "")}{webPort}|{token}";
                        if (!string.IsNullOrEmpty(lessonTitle))
                        {
                            cmd += $"|TITLE={System.Uri.EscapeDataString(lessonTitle)}";
                        }

                        // Cập nhật state
                        QASmartTouch.App.BroadcastState.ScreenCapturePath = filePath;
                        QASmartTouch.App.BroadcastState.ScreenCaptureTime = DateTime.Now;

                        Dispatcher.Invoke(() =>
                        {
                            var app = (QASmartTouch.App)Application.Current;
                            
                            // Cập nhật ảnh Live Preview xem trước trên màn hình GV (Sử dụng ảnh tĩnh tránh đệ quy Hall of Mirrors)
                            imgLivePreview.Source = GetBroadcastPlaceholder();

                            // Luôn gửi qua local bus (chế độ demo trên cùng máy)
                            app.RaiseLocalCommand(cmd);

                            // Nếu có network → cũng gửi qua mạng
                            if (app.NetworkService?.IsBroadcasting == true)
                            {
                                var targetCodes = GetTargetStudentCodes();
                                if (targetCodes == null)
                                {
                                    Log.Information("[BroadcastPage] Gửi TCP command tới tất cả học sinh: {Cmd}", cmd);
                                    _ = app.NetworkService.SendCommandAsync(cmd);
                                }
                                else
                                {
                                    Log.Information("[BroadcastPage] Gửi TCP command tới các học sinh {Targets}: {Cmd}", string.Join(",", targetCodes), cmd);
                                    _ = app.NetworkService.SendToStudentsAsync(targetCodes, cmd);
                                }
                            }
                            else
                            {
                                // === PHASE6-GD5: Log cảnh báo khi network không broadcast ===
                                Log.Warning("[Broadcast-GV] ⚠️ NetworkService.IsBroadcasting=false! Frame TCP command NOT sent to students via network.");
                            }
                        });

                        if (_isFirstCapture)
                            Log.Information("Screen capture broadcast FIRST frame sent: {Path}", filePath);

                        _isFirstCapture = false;
                    }
                    catch (Exception threadEx)
                    {
                        Log.Warning("Background screen capture error: {Err}", threadEx.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Warning("Screen capture initialization error: {Err}", ex.Message);
            }
            finally
            {
                _captureLock.Release();
            }
        }

        private ImageSource GetBroadcastPlaceholder()
        {
            if (_cachedPlaceholder != null) return _cachedPlaceholder;

            try
            {
                int width = 800;
                int height = 600;
                using (var bmp = new System.Drawing.Bitmap(width, height))
                {
                    using (var g = System.Drawing.Graphics.FromImage(bmp))
                    {
                        // Background màu tối #0D1B2A theo nhận diện thương hiệu
                        using (var bgBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0x0D, 0x1B, 0x2A)))
                        {
                            g.FillRectangle(bgBrush, 0, 0, width, height);
                        }

                        // Status bar background #1B2A4A
                        using (var barBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0x1B, 0x2A, 0x4A)))
                        {
                            g.FillRectangle(barBrush, 0, 220, width, 160);
                        }

                        // Vẽ Tiêu đề và Mô tả
                        using (var fontTitle = new System.Drawing.Font("Segoe UI", 24, System.Drawing.FontStyle.Bold))
                        using (var fontSub = new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Regular))
                        using (var brushWhite = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                        using (var brushMuted = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(200, 200, 200)))
                        {
                            var sf = new System.Drawing.StringFormat
                            {
                                Alignment = System.Drawing.StringAlignment.Center,
                                LineAlignment = System.Drawing.StringAlignment.Center
                            };
                            g.DrawString("ĐANG TRÌNH CHIẾU MÀN HÌNH", fontTitle, brushWhite, new System.Drawing.RectangleF(0, 220, width, 90), sf);
                            g.DrawString("Hệ thống đang chia sẻ màn hình của bạn tới tất cả máy học sinh.", fontSub, brushMuted, new System.Drawing.RectangleF(0, 300, width, 60), sf);
                        }
                    }

                    using (var ms = new System.IO.MemoryStream())
                    {
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        ms.Position = 0;
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = ms;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();
                        _cachedPlaceholder = bitmapImage;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to generate broadcast placeholder: {Err}", ex.Message);
                // Fallback to empty bitmap source
                _cachedPlaceholder = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Indexed1, new BitmapPalette(new List<Color> { Colors.Black }), new byte[2], 1);
            }

            return _cachedPlaceholder;
        }

        /// <summary>Win32 API cho screen capture</summary>
        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern IntPtr GetDC(IntPtr hWnd);
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            public static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSrc, int xSrc, int ySrc, int rop);
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            public static extern bool DeleteObject(IntPtr hObject);
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            public static extern bool DeleteDC(IntPtr hdc);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
            
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            public struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }
        }

        private async void Pause_Click(object sender, RoutedEventArgs e)
        {
            _isPaused = !_isPaused;
            _privacyBorder?.SetState(_isPaused || _isBroadcastPausedDueToPrivacy);
            
            // Nếu đang chạy VNC Broadcast
            if (_isVncBroadcasting)
            {
                if (_isPaused)
                {
                    QASmartClass.Services.VncBroadcastService.Instance.PauseBroadcast();
                }
                else
                {
                    QASmartClass.Services.VncBroadcastService.Instance.ResumeBroadcast();
                }
            }

            if (_isPaused)
            {
                _timer?.Stop();
                btnPause.Content = "\u25B6\uFE0F Ti\u1EBFp t\u1EE5c";
                txtStatus.Text = _isVncBroadcasting ? "⏸️ Tạm dừng phát VNC" : "⏸️ Tạm dừng phát";
                liveBadge.Visibility = Visibility.Collapsed;
                btnPause.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                btnPause.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));

                // LOGIC-02 FIX: Gửi lệnh tạm dừng cho HS
                await SendCommandToTargetsAsync("CMD|BROADCAST_PAUSE|1");
                SaveEvent(_isVncBroadcasting ? "VNC_BROADCAST" : "BROADCAST", "GV", "Tạm dừng phát màn hình");
                Log.Information("[BroadcastPage] Broadcast PAUSED — sent BROADCAST_PAUSE to students");
            }
            else
            {
                _timer?.Start();
                btnPause.Content = "\u23F8\uFE0F T\u1EA1m d\u1EEBng";
                txtStatus.Text = _isVncBroadcasting ? "🟢 Đang phát VNC..." : "🟢 Đang phát...";
                liveBadge.Visibility = Visibility.Visible;
                btnPause.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                btnPause.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0));

                // LOGIC-02 FIX: Gửi lệnh tiếp tục cho HS
                await SendCommandToTargetsAsync("CMD|BROADCAST_RESUME|0");
                SaveEvent(_isVncBroadcasting ? "VNC_BROADCAST" : "BROADCAST", "GV", "Tiếp tục phát màn hình");
                Log.Information("[BroadcastPage] Broadcast RESUMED — sent BROADCAST_RESUME to students");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  SOURCE & TARGET PICKERS
        // ═══════════════════════════════════════════════════════════

        private void SourceChanged(object sender, RoutedEventArgs e)
        {
            if (panelWindowPicker == null || panelCameraPicker == null) return;
            panelWindowPicker.Visibility = rbWindow.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            panelCameraPicker.Visibility = rbCamera.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            // Stop preview if we switch to another source
            if (rbCamera.IsChecked == false)
            {
                StopCameraPreview();
            }

            if (rbWindow.IsChecked == true) RefreshWindowsList();
            if (rbCamera.IsChecked == true) RefreshCameraList();
        }

        private void TargetChanged(object sender, RoutedEventArgs e)
        {
            if (panelGroupPicker == null || panelStudentPicker == null) return;
            panelGroupPicker.Visibility = rbGroup.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            panelStudentPicker.Visibility = rbIndividual.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            if (rbIndividual.IsChecked == true) LoadStudentList();
            if (rbGroup.IsChecked == true) LoadGroupList();
            UpdateRecipientCount();
        }

        private void RefreshWindows_Click(object sender, RoutedEventArgs e) => RefreshWindowsList();

        private void RefreshWindowsList()
        {
            try
            {
                cmbWindowList.Items.Clear();
                var processes = System.Diagnostics.Process.GetProcesses()
                    .Where(p => !string.IsNullOrWhiteSpace(p.MainWindowTitle) && p.MainWindowHandle != IntPtr.Zero)
                    .OrderBy(p => p.MainWindowTitle);

                foreach (var p in processes)
                {
                    cmbWindowList.Items.Add(new ComboBoxItem { Content = $"{p.MainWindowTitle}", Tag = p.Id });
                }
                if (cmbWindowList.Items.Count > 0) cmbWindowList.SelectedIndex = 0;
            }
            catch (Exception ex) { Log.Warning("[BroadcastPage][LoadWindowList] Enumerate windows error: {Err}", ex.Message); }
        }

        private void RefreshCameraList()
        {
            try
            {
                cmbCameraList.Items.Clear();
                // OpenCVSharp4 enumerate: thử mở VideoCapture index 0, 1, 2...
                for (int i = 0; i < 5; i++)
                {
                    using var cap = new OpenCvSharp.VideoCapture(i);
                    if (cap.IsOpened())
                    {
                        string label = i == 0 ? "Camera mặc định (Webcam)" : $"Camera {i + 1}";
                        cmbCameraList.Items.Add(new ComboBoxItem { Content = label, Tag = i });
                        cap.Release();
                    }
                    else break;
                }
                if (cmbCameraList.Items.Count > 0)
                {
                    cmbCameraList.SelectedIndex = 0;
                    if (txtCameraPlaceholder != null)
                        txtCameraPlaceholder.Text = "Nhấn để xem trước camera";
                }
                else
                {
                    cmbCameraList.Items.Add(new ComboBoxItem
                    {
                        Content = "❌ Không tìm thấy camera", IsEnabled = false
                    });
                    cmbCameraList.SelectedIndex = 0;
                    if (txtCameraPlaceholder != null)
                        txtCameraPlaceholder.Text = "❌ Vui lòng kết nối webcam rồi thử lại";
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[BroadcastPage][RefreshCamera] Enumerate error: {Err}", ex.Message);
                cmbCameraList.Items.Clear();
                cmbCameraList.Items.Add(new ComboBoxItem
                {
                    Content = "Lỗi khi dò camera", IsEnabled = false
                });
            }
        }

        private void StopCameraPreview()
        {
            try
            {
                if (_cameraPreviewTimer != null)
                {
                    _cameraPreviewTimer.Stop();
                    _cameraPreviewTimer = null;
                }
                if (_cameraCapture != null)
                {
                    _cameraCapture.Release();
                    _cameraCapture.Dispose();
                    _cameraCapture = null;
                }
                if (txtCameraPlaceholder != null)
                {
                    txtCameraPlaceholder.Visibility = Visibility.Visible;
                    txtCameraPlaceholder.Text = "Nhấn để xem trước camera";
                }
                if (imgCameraPreview != null)
                {
                    imgCameraPreview.Source = null;
                }
                if (btnPreviewCamera != null)
                {
                    btnPreviewCamera.Content = "Xem trước Camera";
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[BroadcastPage][StopCameraPreview] Error: {Err}", ex.Message);
            }
        }

        private void CameraPreview_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (_cameraCapture == null || !_cameraCapture.IsOpened())
                {
                    StopCameraPreview();
                    return;
                }
                using (var frame = new OpenCvSharp.Mat())
                {
                    if (_cameraCapture.Read(frame) && !frame.Empty())
                    {
                        byte[] buf;
                        OpenCvSharp.Cv2.ImEncode(".jpg", frame, out buf);
                        var bitmap = new BitmapImage();
                        using (var ms = new System.IO.MemoryStream(buf))
                        {
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = ms;
                            bitmap.EndInit();
                        }
                        bitmap.Freeze();
                        if (imgCameraPreview != null)
                        {
                            imgCameraPreview.Source = bitmap;
                        }
                    }
                    else
                    {
                        StopCameraPreview();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[BroadcastPage][CameraPreview_Tick] Error: {Err}", ex.Message);
                StopCameraPreview();
            }
        }

        // TASK-16: Camera preview trước khi phát
        private void PreviewCamera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cameraCapture != null)
                {
                    StopCameraPreview();
                    return;
                }

                int camIndex = 0;
                if (cmbCameraList.SelectedItem is ComboBoxItem ci && ci.Tag is int idx)
                {
                    camIndex = idx;
                }
                else
                {
                    ShowToast("Vui lòng kết nối và chọn camera trước.", "#E65100");
                    return;
                }

                if (txtCameraPlaceholder != null)
                {
                    txtCameraPlaceholder.Text = "Đang kết nối camera...";
                }

                _cameraCapture = new OpenCvSharp.VideoCapture(camIndex);
                if (!_cameraCapture.IsOpened())
                {
                    ShowToast("❌ Không thể mở camera. Vui lòng kiểm tra kết nối.", "#C62828");
                    _cameraCapture.Dispose();
                    _cameraCapture = null;
                    if (txtCameraPlaceholder != null)
                    {
                        txtCameraPlaceholder.Text = "Không thể kết nối camera";
                    }
                    return;
                }

                if (txtCameraPlaceholder != null)
                {
                    txtCameraPlaceholder.Visibility = Visibility.Collapsed;
                }

                if (btnPreviewCamera != null)
                {
                    btnPreviewCamera.Content = "⏹️ Dừng xem trước";
                }

                _cameraPreviewTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(66) // ~15 FPS
                };
                _cameraPreviewTimer.Tick += CameraPreview_Tick;
                _cameraPreviewTimer.Start();
                Log.Information("[BroadcastPage] Camera preview started for index {Index}", camIndex);
            }
            catch (Exception ex)
            {
                Log.Warning("[BroadcastPage][PreviewCamera] Error: {Err}", ex.Message);
                ShowToast($"❌ Lỗi camera: {ex.Message}", "#C62828");
                StopCameraPreview();
            }
        }

        private void LoadGroupList()
        {
            try
            {
                groupCheckList.Children.Clear();
                var app = (QASmartTouch.App)Application.Current;

                // Ưu tiên dữ liệu từ GroupPage (App.CurrentGroups)
                var state = QASmartClass.Services.BroadcastStateService.Instance;
                var groups = app.CurrentGroups;
                if (groups != null && groups.Count > 0)
                {
                    foreach (var g in groups)
                    {
                        bool isChecked = true;
                        if (state.TargetSelectionMode == "GROUP")
                        {
                            isChecked = state.TargetSelectedGroups.Contains(g.Index.ToString());
                        }

                        var cb = new CheckBox
                        {
                            Content = $"{g.Emoji} {g.Name} ({g.Members.Count} HS)",
                            FontSize = 11, IsChecked = isChecked, Margin = new Thickness(0, 0, 0, 4),
                            Tag = g.Index, ToolTip = string.Join(", ", g.Members)
                        };
                        cb.Checked += (_, _) => UpdateRecipientCount();
                        cb.Unchecked += (_, _) => UpdateRecipientCount();
                        groupCheckList.Children.Add(cb);
                    }
                }
                else
                {
                    // Fallback: nhóm theo ClassName trong DB
                    var students = app.Database?.Students?.ToList();
                    var classNames = students?.Select(s => s.ClassName)
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .Distinct().OrderBy(c => c).ToList();

                    if (classNames != null && classNames.Count > 0)
                    {
                        foreach (var cn in classNames)
                        {
                            int cnt = students!.Count(s => s.ClassName == cn);
                            bool isChecked = true;
                            if (state.TargetSelectionMode == "GROUP")
                            {
                                isChecked = state.TargetSelectedGroups.Contains(cn);
                            }

                            var cb = new CheckBox
                            {
                                Content = $"{cn} ({cnt} HS)",
                                FontSize = 11, IsChecked = isChecked, Margin = new Thickness(0, 0, 0, 4),
                                Tag = cn
                            };
                            cb.Checked += (_, _) => UpdateRecipientCount();
                            cb.Unchecked += (_, _) => UpdateRecipientCount();
                            groupCheckList.Children.Add(cb);
                        }
                    }
                    else
                    {
                        groupCheckList.Children.Add(new TextBlock
                        {
                            Text = "Chưa có nhóm. Vào mục 2.4 Nhóm học tập để tạo nhóm.",
                            FontSize = 10, Foreground = Brushes.Gray, FontStyle = FontStyles.Italic,
                            TextWrapping = TextWrapping.Wrap
                        });
                    }
                }
                UpdateRecipientCount();
            }
            catch (Exception ex) { Log.Warning("LoadGroupList error: {Err}", ex.Message); }
        }

        private void LoadStudentList()
        {
            try
            {
                studentCheckList.Children.Clear();
                var app = (QASmartTouch.App)Application.Current;

                // Load HS theo active roster
                var allStudents = Services.VietnameseNameHelper.SortByVietnameseName(
                    GetStudentsForCurrentContext(), s => s.FullName);
                _cachedStudents = allStudents;

                if (allStudents != null && allStudents.Count > 0)
                {
                    var state = QASmartClass.Services.BroadcastStateService.Instance;
                    var onlineStudents = app.NetworkService?.GetConnectedStudents();

                    foreach (var s in allStudents)
                    {
                        var client = onlineStudents?.FirstOrDefault(c => c.Code.Equals(s.StudentCode, StringComparison.OrdinalIgnoreCase));
                        bool isOnline = client != null || s.IsOnline;
                        string statusIcon = isOnline ? "🟢" : "⚪";
                        string latencyStr = client != null ? $" ({client.LatencyMs}ms)" : "";
                        string className = !string.IsNullOrWhiteSpace(s.ClassName) ? $" [{s.ClassName}]" : "";

                        bool isChecked = true;
                        if (state.TargetSelectionMode == "INDIVIDUAL")
                        {
                            isChecked = state.TargetSelectedStudents.Contains(s.StudentCode);
                        }

                        var cb = new CheckBox
                        {
                            Content = $"{statusIcon} {s.FullName}{className}{latencyStr}",
                            FontSize = 11, IsChecked = isChecked, Margin = new Thickness(0, 0, 0, 3),
                            Tag = s.StudentCode, ToolTip = $"Mã: {s.StudentCode} | IP: {s.IPAddress} | PC: {s.PCName}"
                        };
                        cb.Checked += (_, _) => UpdateRecipientCount();
                        cb.Unchecked += (_, _) => UpdateRecipientCount();
                        studentCheckList.Children.Add(cb);
                    }
                }
                else
                {
                    studentCheckList.Children.Add(new TextBlock
                    {
                        Text = "Chưa có HS trong hệ thống. Vào mục 2.3 Học sinh để thêm.",
                        FontSize = 10, Foreground = Brushes.Gray, FontStyle = FontStyles.Italic,
                        TextWrapping = TextWrapping.Wrap
                    });
                }
                UpdateRecipientCount();
            }
            catch (Exception ex) { Log.Warning("LoadStudentList error: {Err}", ex.Message); }
        }

        private void SaveTargetSelectionState()
        {
            try
            {
                var state = QASmartClass.Services.BroadcastStateService.Instance;
                if (rbAllStudents.IsChecked == true) state.TargetSelectionMode = "ALL";
                else if (rbGroup.IsChecked == true) state.TargetSelectionMode = "GROUP";
                else if (rbIndividual.IsChecked == true) state.TargetSelectionMode = "INDIVIDUAL";

                state.TargetSelectedGroups.Clear();
                foreach (var cb in groupCheckList.Children.OfType<CheckBox>())
                {
                    if (cb.IsChecked == true && cb.Tag != null)
                        state.TargetSelectedGroups.Add(cb.Tag.ToString()!);
                }

                state.TargetSelectedStudents.Clear();
                foreach (var cb in studentCheckList.Children.OfType<CheckBox>())
                {
                    if (cb.IsChecked == true && cb.Tag is string code)
                        state.TargetSelectedStudents.Add(code);
                }
            }
            catch (Exception ex) { Log.Warning("SaveTargetSelectionState error: {Err}", ex.Message); }
        }

        private void UpdateRecipientCount()
        {
            try
            {
                SaveTargetSelectionState();
                if (rbAllStudents.IsChecked == true)
                {
                    int total = _cachedStudents?.Count ?? GetStudentsForCurrentContext().Count;
                    txtRecipientCount.Text = $"→ {total} máy HS sẽ nhận";
                }
                else if (rbGroup.IsChecked == true)
                {
                    var app = (QASmartTouch.App)Application.Current;
                    var selectedGroups = groupCheckList.Children.OfType<CheckBox>()
                        .Where(c => c.IsChecked == true).ToList();
                    int groupCount = selectedGroups.Count;

                    // Đếm tổng HS trong các nhóm được chọn
                    int totalMembers = 0;
                    if (app.CurrentGroups?.Count > 0)
                    {
                        foreach (var cb in selectedGroups)
                        {
                            if (cb.Tag is int idx && idx < app.CurrentGroups.Count)
                                totalMembers += app.CurrentGroups[idx].Members.Count;
                        }
                    }
                    txtRecipientCount.Text = totalMembers > 0
                        ? $"→ {groupCount} nhóm ({totalMembers} HS)"
                        : $"→ {groupCount} nhóm được chọn";
                }
                else if (rbIndividual.IsChecked == true)
                {
                    int hsCount = studentCheckList.Children.OfType<CheckBox>().Count(c => c.IsChecked == true);
                    int total = studentCheckList.Children.OfType<CheckBox>().Count();
                    txtRecipientCount.Text = $"→ {hsCount}/{total} HS được chọn";
                }
            }
            catch (Exception ex) { Log.Warning("[BroadcastPage][UpdateRecipientCount] Count error: {Err}", ex.Message); }
        }

        private void SelectAllGroups_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cb in groupCheckList.Children.OfType<CheckBox>()) cb.IsChecked = true;
            UpdateRecipientCount();
        }
        private void DeselectAllGroups_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cb in groupCheckList.Children.OfType<CheckBox>()) cb.IsChecked = false;
            UpdateRecipientCount();
        }
        private void SelectAllStudents_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cb in studentCheckList.Children.OfType<CheckBox>()) cb.IsChecked = true;
            UpdateRecipientCount();
        }
        private void DeselectAllStudents_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cb in studentCheckList.Children.OfType<CheckBox>()) cb.IsChecked = false;
            UpdateRecipientCount();
        }

        // V22-29: Tìm kiếm HS theo tên
        private void SearchStudent_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = txtSearchStudent.Text.Trim().ToLowerInvariant();
            foreach (var child in studentCheckList.Children)
            {
                if (child is CheckBox cb)
                {
                    if (string.IsNullOrEmpty(keyword))
                    {
                        cb.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        string content = cb.Content?.ToString()?.ToLowerInvariant() ?? "";
                        cb.Visibility = content.Contains(keyword) ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TAB 2: PUSH NOTIFICATION
        // ═══════════════════════════════════════════════════════════

        private async void SendNotice_Click(object sender, RoutedEventArgs e)
        {
            var title = txtNoticeTitle.Text.Trim();
            var body = txtNoticeBody.Text.Trim();
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
            {
                ShowToast("\u26A0\uFE0F Vui l\u00F2ng nh\u1EADp ti\u00EAu \u0111\u1EC1 ho\u1EB7c n\u1ED9i dung!", "#E65100");
                return;
            }

            var type = (cboNoticeType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "info";
            int duration = 30;
            if (cboNoticeDuration.SelectedItem is ComboBoxItem di && di.Tag is string ds && int.TryParse(ds, out int d))
                duration = d;

            // CMD|NOTICE|type|duration|title|body
            var cmd = $"CMD|NOTICE|{type}|{duration}|{title}|{body}";

            // G\u1EEDi theo \u0111\u1ED1i t\u01B0\u1EE3ng \u0111\u00E3 ch\u1ECDn
            await SendCommandToTargetsAsync(cmd);

            // Add to history
            AddNoticeHistory(title, body, type);
            var targetDesc = GetTargetDescription();
            SaveEvent("NOTICE", "GV", $"{type}|{title}|{body}|{targetDesc}");
            ShowToast($"\u2705 \u0110\u00E3 g\u1EEDi th\u00F4ng b\u00E1o: \"{title}\" [{targetDesc}]", "#2E7D32");

            txtNoticeTitle.Text = "";
            txtNoticeBody.Text = "";
            Log.Information("Notice sent: {Title} ({Type}) to {Target}", title, type, targetDesc);
        }

        private void AddNoticeHistory(string title, string body, string type)
        {
            AddNoticeHistoryItem(title, body, type, DateTime.Now, true);
        }

        private void AddNoticeHistoryItem(string title, string body, string type, DateTime timestamp, bool isNew = false)
        {
            if (isNew)
            {
                if (noticeHistory.Children.Count == 1 && noticeHistory.Children[0] is TextBlock tb && tb.FontStyle == FontStyles.Italic)
                    noticeHistory.Children.Clear();
            }

            string typeLabel = type switch
            {
                "warning" => "[C\u1EA3nh b\u00E1o]",
                "urgent" => "[Kh\u1EA9n c\u1EA5p]",
                "celebrate" => "[Ch\u00FAc m\u1EEBng]",
                _ => "[Th\u00F4ng tin]"
            };
            var typeColor = type switch
            {
                "warning" => Color.FromRgb(230, 81, 0),
                "urgent" => Color.FromRgb(198, 40, 40),
                "celebrate" => Color.FromRgb(46, 125, 50),
                _ => Color.FromRgb(21, 101, 192)
            };

            var entry = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 247, 250)),
                CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 0, 0, 3)
            };
            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            stack.Children.Add(new TextBlock
            {
                Text = typeLabel, FontSize = 10, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(typeColor),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            });
            stack.Children.Add(new TextBlock
            {
                Text = title, FontSize = 11, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                VerticalAlignment = VerticalAlignment.Center
            });
            stack.Children.Add(new TextBlock
            {
                Text = $" - {timestamp:HH:mm}", FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(189, 189, 189)),
                VerticalAlignment = VerticalAlignment.Center
            });
            entry.Child = stack;

            if (isNew)
            {
                noticeHistory.Children.Insert(0, entry);
                while (noticeHistory.Children.Count > 15)
                {
                    noticeHistory.Children.RemoveAt(noticeHistory.Children.Count - 1);
                }
            }
            else
            {
                noticeHistory.Children.Add(entry);
            }
        }

        private void LoadNoticeHistoryFromDb()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.Database == null) return;

                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        System.Collections.Generic.List<EventLog> logs;
                        using (var db = new QASmartClass.Data.AppDbContext())
                        {
                            logs = db.EventLogs
                                .Where(e => e.EventType == "NOTICE")
                                .OrderByDescending(e => e.Timestamp)
                                .Take(15)
                                .ToList();
                        }

                        Dispatcher.Invoke(() =>
                        {
                            noticeHistory.Children.Clear();

                            if (logs.Count == 0)
                            {
                                noticeHistory.Children.Add(new TextBlock
                                {
                                    Text = "Ch\u01B0a c\u00F3 th\u00F4ng b\u00E1o n\u00E0o",
                                    FontSize = 11,
                                    Foreground = Brushes.LightGray,
                                    FontStyle = FontStyles.Italic,
                                    HorizontalAlignment = HorizontalAlignment.Center
                                });
                                return;
                            }

                            foreach (var log in logs)
                            {
                                if (string.IsNullOrEmpty(log.Details)) continue;

                                string type = "info";
                                string title = "";
                                string body = "";

                                if (log.Details.Contains('|'))
                                {
                                    var parts = log.Details.Split('|');
                                    if (parts.Length >= 3)
                                    {
                                        type = parts[0];
                                        title = parts[1];
                                        body = parts[2];
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        string details = log.Details;
                                        if (details.StartsWith("Thông báo (") || details.StartsWith("Th\u00F4ng b\u00E1o ("))
                                        {
                                            int idxClose = details.IndexOf("): ");
                                            if (idxClose > 11)
                                            {
                                                type = details.Substring(11, idxClose - 11);
                                                string rest = details.Substring(idxClose + 3);
                                                int idxDash = rest.LastIndexOf(" \u2014 ");
                                                if (idxDash > 0)
                                                {
                                                    title = rest.Substring(0, idxDash);
                                                    body = rest.Substring(idxDash + 3);
                                                }
                                                else
                                                {
                                                    title = rest;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            title = details;
                                        }
                                    }
                                    catch
                                    {
                                        title = log.Details;
                                    }
                                }

                                AddNoticeHistoryItem(title, body, type, log.Timestamp);
                            }
                        });
                    }
                    catch (Exception threadEx)
                    {
                        Log.Warning("Background LoadNoticeHistoryFromDb error: {Err}", threadEx.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Warning("LoadNoticeHistoryFromDb error: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TAB 3: SEND MEDIA / FILE
        // ═══════════════════════════════════════════════════════════

        private void PickFile_Click(object sender, MouseButtonEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Tất cả file hỗ trợ|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.pdf;*.mp4;*.pptx;*.docx|Ảnh|*.png;*.jpg;*.jpeg;*.gif;*.bmp|PDF|*.pdf|Video|*.mp4|Office|*.pptx;*.docx",
                Title = "Chọn file để gửi cho HS"
            };
            if (dlg.ShowDialog() == true)
            {
                _selectedFilePath = dlg.FileName;
                txtFileName.Text = $"{System.IO.Path.GetFileName(dlg.FileName)}";

                // Preview if image
                var ext = System.IO.Path.GetExtension(dlg.FileName).ToLower();
                if (ext is ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp")
                {
                    try
                    {
                        imgPreview.Source = new BitmapImage(new Uri(dlg.FileName));
                        filePreview.Visibility = Visibility.Visible;
                    }
                    catch { filePreview.Visibility = Visibility.Collapsed; }
                }
                else
                {
                    filePreview.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void SendMedia_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                ShowToast("\u26A0\uFE0F Vui l\u00F2ng ch\u1ECDn file tr\u01B0\u1EDBc!", "#E65100");
                return;
            }

            try
            {
                var app = (QASmartTouch.App)Application.Current;
                QASmartTouch.App.BroadcastState.AddBroadcastFile(_selectedFilePath);
                
                int webPort = 8080;
                if (app.NetworkService?.WebBridge != null)
                {
                    webPort = app.NetworkService.WebBridge.WebPort;
                }
                if (string.IsNullOrEmpty(QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken))
                {
                    QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken = "TK_" + Guid.NewGuid().ToString("N").Substring(0, 16);
                }
                var token = QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken;
                var cmd = $"CMD|FILE_BROADCAST|{_selectedFilePath}|{webPort}|{token}";

                // G\u1EEDi theo \u0111\u1ED1i t\u01B0\u1EE3ng \u0111\u00E3 ch\u1ECDn
                await SendCommandToTargetsAsync(cmd);

                var targetDesc = GetTargetDescription();
                SaveEvent("FILE_BROADCAST", "GV", $"G\u1EEDi file: {System.IO.Path.GetFileName(_selectedFilePath)} [{targetDesc}]");
                ShowToast($"\u2705 \u0110\u00E3 g\u1EEDi: {System.IO.Path.GetFileName(_selectedFilePath)} [{targetDesc}]", "#2E7D32");
                Log.Information("File broadcast: {Path} to {Target}", _selectedFilePath, targetDesc);
            }
            catch (Exception ex)
            {
                ShowToast($"\u274C L\u1ED7i: {ex.Message}", "#C62828");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TAB 4: OPEN WEBSITE
        // ═══════════════════════════════════════════════════════════

        private void QuickLink_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string url)
                txtWebUrl.Text = url;
        }

        private bool IsSafeUrl(string url, out string normalizedUrl)
        {
            normalizedUrl = url.Trim();
            if (string.IsNullOrWhiteSpace(normalizedUrl)) return false;

            if (!normalizedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                !normalizedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                normalizedUrl = "https://" + normalizedUrl;
            }

            return Uri.TryCreate(normalizedUrl, UriKind.Absolute, out Uri? uriResult) && 
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private void LoadDynamicWhitelist()
        {
            _dynamicWhitelist = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_config.json");
                if (System.IO.File.Exists(configPath))
                {
                    string jsonContent = System.IO.File.ReadAllText(configPath);
                    using (var doc = System.Text.Json.JsonDocument.Parse(jsonContent))
                    {
                        if (doc.RootElement.TryGetProperty("UrlWhitelist", out var whitelistProp) && whitelistProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var item in whitelistProp.EnumerateArray())
                            {
                                string val = item.GetString()?.Trim().ToLower() ?? "";
                                if (!string.IsNullOrEmpty(val))
                                {
                                    _dynamicWhitelist.Add(val);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to load dynamic whitelist, fallback to default: {Err}", ex.Message);
            }
        }

        private void SetupConfigWatcher()
        {
            try
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                if (System.IO.Directory.Exists(dir))
                {
                    _configWatcher = new System.IO.FileSystemWatcher
                    {
                        Path = dir,
                        Filter = "app_config.json",
                        NotifyFilter = System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.Size | System.IO.NotifyFilters.FileName
                    };
                    _configWatcher.Changed += (s, e) =>
                    {
                        Dispatcher.Invoke(() => LoadDynamicWhitelist());
                    };
                    _configWatcher.Created += (s, e) =>
                    {
                        Dispatcher.Invoke(() => LoadDynamicWhitelist());
                    };
                    _configWatcher.Deleted += (s, e) =>
                    {
                        Dispatcher.Invoke(() => LoadDynamicWhitelist());
                    };
                    _configWatcher.EnableRaisingEvents = true;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("SetupConfigWatcher error: {Err}", ex.Message);
            }
        }

        private bool IsWhitelisted(string url)
        {
            try
            {
                var uri = new Uri(url);
                var host = uri.Host.ToLower();

                if (_dynamicWhitelist != null && _dynamicWhitelist.Count > 0)
                {
                    if (_dynamicWhitelist.Any(w => host == w || host.EndsWith("." + w))) return true;
                }

                string[] whitelist = {
                    "google.com", "youtube.com", "wikipedia.org", "vi.wikipedia.org",
                    "en.wikipedia.org", "kahoot.it", "quizlet.com",
                    // Trang giáo dục Việt Nam phổ biến
                    "hocmai.vn", "vietjack.com", "loigiaihay.com", "olm.vn",
                    "violet.vn", "truonghoc247.com", "toanhoc.org", "phet.colorado.edu"
                };
                if (whitelist.Any(w => host == w || host.EndsWith("." + w))) return true;
                if (host.EndsWith(".edu.vn") || host.EndsWith(".edu") || host.EndsWith(".gov.vn")) return true;
            }
            catch (Exception ex) { Log.Debug("[BroadcastPage][IsSafeUrl] URL check error: {Err}", ex.Message); }
            return false;
        }

        private async void SendWeb_Click(object sender, RoutedEventArgs e)
        {
            var rawUrl = txtWebUrl.Text.Trim();
            if (string.IsNullOrWhiteSpace(rawUrl) || rawUrl == "https://")
            {
                ShowToast("\u26A0\uFE0F Vui l\u00F2ng nh\u1EADp URL!", "#E65100");
                return;
            }

            if (!IsSafeUrl(rawUrl, out string url))
            {
                ShowToast("\u26A0\uFE0F \u0110\u1ECBnh d\u1EA1ng URL kh\u00F4ng h\u1EE3p l\u1EC7!", "#C62828");
                return;
            }

            if (!IsWhitelisted(url))
            {
                var result = MessageBox.Show(
                    "Li\u00EAn k\u1EBFt n\u00E0y kh\u00F4ng n\u1EB1m trong danh s\u00E1ch khuy\u00EAn d\u00F9ng c\u1EE7a nh\u00E0 tr\u01B0\u1EDDng.\nB\u1EA1n c\u00F3 ch\u1EAFc ch\u1EAFn mu\u1ED1n g\u1EEDi li\u00EAn k\u1EBFt n\u00E0y t\u1EDBi h\u1ECDc sinh?", 
                    "C\u1EA3nh b\u00E1o b\u1EA3o m\u1EADt", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            try
            {
                var cmd = $"CMD|OPEN_URL|{url}";

                // G\u1EEDi theo \u0111\u1ED1i t\u01B0\u1EE3ng \u0111\u00E3 ch\u1ECDn
                await SendCommandToTargetsAsync(cmd);

                var targetDesc = GetTargetDescription();
                SaveEvent("OPEN_URL", "GV", $"M\u1EDF URL: {url} [{targetDesc}]");
                ShowToast($"\u2705 \u0110\u00E3 g\u1EEDi URL: {url} [{targetDesc}]", "#7B1FA2");
                Log.Information("URL sent: {Url} to {Target}", url, targetDesc);
            }
            catch (Exception ex) { ShowToast($"\u274C L\u1ED7i: {ex.Message}", "#C62828"); }
        }

        // ═══════════════════════════════════════════════════════════
        //  QUICK ACTIONS (Lock/Unlock/Black screen)
        // ═══════════════════════════════════════════════════════════

        private async void LockScreen_Click(object sender, RoutedEventArgs e)
        {
            await SendQuickCommand("CMD|LOCK_SCREEN|1", "Đã khóa màn hình HS", "LOCK_SCREEN");
        }

        private async void UnlockScreen_Click(object sender, RoutedEventArgs e)
        {
            await SendQuickCommand("CMD|UNLOCK_SCREEN|0", "Đã mở khóa màn hình HS", "UNLOCK_SCREEN");
        }

        private async void BlackScreen_Click(object sender, RoutedEventArgs e)
        {
            await SendQuickCommand("CMD|BLACK_SCREEN|1", "Đã tắt màn hình HS", "BLACK_SCREEN");
        }

        private async System.Threading.Tasks.Task SendQuickCommand(string cmd, string toast, string eventType)
        {
            try
            {
                // Quick actions (Lock/Unlock/Black) g\u1EEDi theo \u0111\u1ED1i t\u01B0\u1EE3ng \u0111\u00E3 ch\u1ECDn
                await SendCommandToTargetsAsync(cmd);

                var targetDesc = GetTargetDescription();
                SaveEvent(eventType, "GV", $"{cmd} [{targetDesc}]");
                ShowToast($"\u2705 {toast} [{targetDesc}]", "#2E7D32");
                Log.Information("Quick command: {Cmd} to {Target}", cmd, targetDesc);
            }
            catch (Exception ex) { ShowToast($"\u274C L\u1ED7i: {ex.Message}", "#C62828"); }
        }

        // ═══════════════════════════════════════════════════════════
        //  TARGETED SEND HELPERS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// L\u1EA5y danh s\u00E1ch StudentCode c\u1EE7a c\u00E1c HS \u0111\u01B0\u1EE3c ch\u1ECDn l\u00E0m \u0111\u1ED1i t\u01B0\u1EE3ng nh\u1EADn.
        /// Tr\u1EA3 v\u1EC1 null n\u1EBFu g\u1EEDi t\u1EA5t c\u1EA3 (broadcast).
        /// </summary>
        private System.Collections.Generic.List<string>? GetTargetStudentCodes()
        {
            if (rbAllStudents.IsChecked == true)
                return null; // broadcast to all

            var app = (QASmartTouch.App)Application.Current;
            var codes = new System.Collections.Generic.List<string>();

            if (rbGroup.IsChecked == true)
            {
                // L\u1EA5y t\u00EAn HS t\u1EEB c\u00E1c nh\u00F3m \u0111\u01B0\u1EE3c ch\u1ECDn, map sang StudentCode
                var selectedGroupChecks = groupCheckList.Children.OfType<CheckBox>()
                    .Where(c => c.IsChecked == true).ToList();

                var groups = app.CurrentGroups;
                if (groups != null && groups.Count > 0)
                {
                    // CurrentGroups mode: Members l\u00E0 danh s\u00E1ch t\u00EAn HS
                    var memberNames = new System.Collections.Generic.HashSet<string>();
                    foreach (var cb in selectedGroupChecks)
                    {
                        if (cb.Tag is int idx && idx < groups.Count)
                        {
                            foreach (var name in groups[idx].Members)
                                memberNames.Add(name);
                        }
                    }
                    // Map t\u00EAn HS -> StudentCode t\u1EEB DB (lọc theo ActiveRoster ClassName nếu trùng tên học sinh)
                    var activeClassName = app.ClassRoster.ActiveRoster?.ClassName;
                    var students = app.Database?.Students?.ToList();
                    if (students != null)
                    {
                        foreach (var s in students)
                        {
                            if (memberNames.Contains(s.FullName) && !string.IsNullOrEmpty(s.StudentCode))
                            {
                                var duplicateCount = students.Count(x => x.FullName == s.FullName);
                                if (duplicateCount > 1)
                                {
                                    if (!string.IsNullOrEmpty(activeClassName) && s.ClassName == activeClassName)
                                    {
                                        codes.Add(s.StudentCode);
                                    }
                                }
                                else
                                {
                                    codes.Add(s.StudentCode);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // ClassName fallback mode: Tag l\u00E0 className string
                    var selectedClassNames = selectedGroupChecks
                        .Where(c => c.Tag is string)
                        .Select(c => (string)c.Tag!).ToList();
                    var students = app.Database?.Students?.ToList();
                    if (students != null)
                    {
                        foreach (var s in students)
                        {
                            if (selectedClassNames.Contains(s.ClassName) && !string.IsNullOrEmpty(s.StudentCode))
                                codes.Add(s.StudentCode);
                        }
                    }
                }
            }
            else if (rbIndividual.IsChecked == true)
            {
                // L\u1EA5y StudentCode t\u1EEB Tag c\u1EE7a c\u00E1c CheckBox \u0111\u01B0\u1EE3c ch\u1ECDn
                foreach (var cb in studentCheckList.Children.OfType<CheckBox>())
                {
                    if (cb.IsChecked == true && cb.Tag is string code && !string.IsNullOrEmpty(code))
                        codes.Add(code);
                }
            }

            // === UPGRADE_06: Fix Loi_34 — empty selection trả empty list (KHÔNG broadcast) thay vì null (broadcast ALL) ===
            return codes.Count > 0 ? codes : new System.Collections.Generic.List<string>();
        }

        /// <summary>
        /// G\u1EEDi command t\u1EDBi \u0111\u1ED1i t\u01B0\u1EE3ng \u0111\u00E3 ch\u1ECDn (t\u1EA5t c\u1EA3 / nh\u00F3m / t\u1EEBng HS).
        /// </summary>
        private async System.Threading.Tasks.Task SendCommandToTargetsAsync(string cmd)
        {
            var app = Application.Current as QASmartTouch.App;
            if (app == null) return;
            var targetCodes = GetTargetStudentCodes();

            if (targetCodes == null)
            {
                // Broadcast t\u1EA5t c\u1EA3
                if (app.NetworkService?.IsBroadcasting == true)
                    await app.NetworkService.SendCommandAsync(cmd);
                else
                    Dispatcher.BeginInvoke(new Action(() => app.RaiseLocalCommand(cmd)), DispatcherPriority.Background);
            }
            else
            {
                // G\u1EEDi cho nh\u00F3m HS c\u1EE5 th\u1EC3
                if (app.NetworkService?.IsBroadcasting == true)
                    await app.NetworkService.SendToStudentsAsync(targetCodes, cmd);
                else
                    Dispatcher.BeginInvoke(new Action(() => app.RaiseLocalCommand(cmd)), DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// M\u00F4 t\u1EA3 \u0111\u1ED1i t\u01B0\u1EE3ng nh\u1EADn \u0111\u1EC3 hi\u1EC3n th\u1ECB trong toast v\u00E0 log.
        /// </summary>
        private string GetTargetDescription()
        {
            if (rbAllStudents.IsChecked == true)
                return "T\u1EA5t c\u1EA3 HS";
            if (rbGroup.IsChecked == true)
            {
                int count = groupCheckList.Children.OfType<CheckBox>().Count(c => c.IsChecked == true);
                return $"{count} nh\u00F3m";
            }
            if (rbIndividual.IsChecked == true)
            {
                int count = studentCheckList.Children.OfType<CheckBox>().Count(c => c.IsChecked == true);
                int total = studentCheckList.Children.OfType<CheckBox>().Count();
                return $"{count}/{total} HS";
            }
            return "T\u1EA5t c\u1EA3 HS";
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════

        private async void SaveEvent(string type, string actor, string details)
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.Database == null) return;
                app.Database.EventLogs.Add(new EventLog { EventType = type, Actor = actor, Details = details, Timestamp = DateTime.Now });
                await app.Database.SaveChangesAsync();
            }
            catch (Exception ex) { Log.Debug("[BroadcastPage][SaveEvent] DB save error: {Err}", ex.Message); }
        }

        private void ShowToast(string msg, string colorHex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorHex);
                txtStatus.Text = msg;
                txtStatus.Foreground = new SolidColorBrush(color);
                var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
                t.Tick += (s, ev) =>
                {
                    t.Stop();
                    txtStatus.Text = _isBroadcasting ? $"Đang phát..." : "Chưa phát";
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117));
                };
                t.Start();
            }
            catch (Exception ex) { Log.Debug("[BroadcastPage][ShowToast] Toast display error: {Err}", ex.Message); }
        }

        private void DropZone_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string filePath = files[0];
                    _selectedFilePath = filePath;
                    txtFileName.Text = System.IO.Path.GetFileName(filePath);

                    // Preview if image
                    var ext = System.IO.Path.GetExtension(filePath).ToLower();
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".bmp")
                    {
                        try
                        {
                            imgPreview.Source = new BitmapImage(new Uri(filePath));
                            filePreview.Visibility = Visibility.Visible;
                        }
                        catch
                        {
                            filePreview.Visibility = Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        filePreview.Visibility = Visibility.Collapsed;
                    }
                }
            }
            e.Handled = true;
        }

        private void ApplyScreenCaptureExclusion(bool enable)
        {
            try
            {
                uint affinity = enable ? 0x00000011U : 0x00000000U; // WDA_EXCLUDEFROMCAPTURE (0x00000011) vs WDA_NONE
                foreach (Window win in Application.Current.Windows)
                {
                    if (win is BroadcastPrivacyBorder) continue;
                    var helper = new System.Windows.Interop.WindowInteropHelper(win);
                    if (helper.Handle != IntPtr.Zero)
                    {
                        NativeMethods.SetWindowDisplayAffinity(helper.Handle, affinity);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("ApplyScreenCaptureExclusion failed: {Err}", ex.Message);
            }
        }

        // === UPGRADE_13: Click handler cho chế độ truyền phát siêu tốc ===
        private void TurboBroadcast_Click(object sender, RoutedEventArgs e)
        {
            if (_isBroadcasting)
            {
                // Nếu đang phát thì bấm nút này sẽ dừng phát
                Broadcast_Click(sender, e);
                return;
            }

            // 1. Cảnh báo giáo viên về việc tắt Exclusion (Quyền riêng tư)
            bool confirmResult = QASmartClass.Shared.QAConfirmDialog.ShowDialog(
                "Xác nhận phát Siêu tốc",
                "Chế độ Siêu tốc sẽ tắt tính năng che giấu thông tin ứng dụng nhạy cảm để giải phóng tối đa tài nguyên và tăng tốc độ truyền tải.\n\nBạn có đồng ý bắt đầu không?",
                Window.GetWindow(this));
            
            if (!confirmResult) return;

            // 2. Tự động tắt Privacy/Exclusion để giải phóng tài nguyên chụp hình của DWM
            ApplyScreenCaptureExclusion(false); 
            QASmartTouch.Services.AppSettings.Broadcast_TurboMode = true;

            // 3. Chạy luồng phát chính
            Broadcast_Click(sender, e);
            
            btnTurboBroadcast.Content = "⏹️ Dừng Siêu Tốc";
            btnBroadcast.IsEnabled = false; // Khóa nút phát thường
        }

        // === UPGRADE_13_ADD: Click handler cho chế độ truyền phát Chuyên Chữ (Lossless) ===
        private void TextBroadcast_Click(object sender, RoutedEventArgs e)
        {
            if (_isBroadcasting)
            {
                Broadcast_Click(sender, e);
                return;
            }

            // Ghi đè UI Settings tạm thời sang cấu hình đọc chữ cực nét (SP-04)
            cmbResolution.SelectedIndex = 1; // 1920x1080
            cmbFPS.SelectedIndex = 0;        // Thấp (1 fps)
            cmbQuality.SelectedIndex = 2;    // Nén ít (85%)
            
            _isTextMode = true;

            // Chạy luồng phát chính
            Broadcast_Click(sender, e);
        }

        // === UPGRADE_13_ADD: Click handler cho chế độ truyền phát Đa Thiết Bị (Multicast) ===
        private void MulticastBroadcast_Click(object sender, RoutedEventArgs e)
        {
            if (_isBroadcasting)
            {
                Broadcast_Click(sender, e);
                return;
            }

            _isMulticastMode = true;

            // Khóa các tùy chọn chất lượng để hệ thống tự động chạy theo cấu hình Multicast thích ứng
            cmbResolution.IsEnabled = false;
            cmbFPS.IsEnabled = false;
            cmbQuality.IsEnabled = false;

            // Chạy luồng phát chính
            Broadcast_Click(sender, e);
        }

        // === VNC_BROADCAST: Click handler cho chế độ truyền phát VNC (Rust VNCTool) ===
        private async void VncBroadcast_Click(object sender, RoutedEventArgs e)
        {
            if (_isVncBroadcasting)
            {
                StopVncBroadcast();
                return;
            }

            if (_isBroadcasting)
            {
                ShowToast("⚠️ Vui lòng dừng các chế độ phát khác trước.", "#E65100");
                return;
            }

            // 1. Kiểm tra cảnh báo và xác nhận số lượng HS nhận
            try
            {
                var appStart = (QASmartTouch.App)Application.Current;
                var targetCodes = GetTargetStudentCodes();
                int recipientCount = targetCodes == null ? (appStart.NetworkService?.ConnectedCount ?? 0) : targetCodes.Count;

                if (targetCodes != null && targetCodes.Count == 0)
                {
                    MessageBox.Show(
                        "⚠️ Chưa chọn đối tượng nhận chiếu màn hình.\n\nVui lòng chọn ít nhất 1 học sinh hoặc 1 nhóm trước khi bắt đầu phát.",
                        "Lỗi cấu hình — Không có đối tượng nhận",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (recipientCount == 0)
                {
                    var warnResult = MessageBox.Show(
                        "[Cảnh báo] HIỆN CHƯA CÓ HỌC SINH NÀO NHẬN\n\nBạn chưa chọn học sinh hoặc chưa có học sinh nào kết nối.\nBạn có chắc chắn muốn tiếp tục phát màn hình?",
                        "Cảnh báo — Không có học sinh",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    if (warnResult != MessageBoxResult.Yes) return;
                }
            }
            catch (Exception ex) { Log.Debug("[VncBroadcast] Warn check error: {Err}", ex.Message); }

            // Xác nhận trước khi bật FORCE_WATCH
            if (chkForceWatch.IsChecked == true)
            {
                var confirmResult = QASmartClass.Shared.QAConfirmDialog.ShowDialog(
                    "Xác nhận khóa màn hình (VNC)",
                    "[Cảnh báo] BẠN ĐANG BẬT CHẾ ĐỘ KHÓA MÀN HÌNH HỌC SINH\n\nHọc sinh sẽ không thể đóng màn hình chiếu và bàn phím bị vô hiệu hóa.\n\nBạn có chắc chắn muốn tiếp tục?",
                    Window.GetWindow(this));
                if (!confirmResult) return;
            }

            // Bắt đầu phát sóng thông qua Singleton Service
            bool isForceWatch = chkForceWatch.IsChecked == true;
            var selectedTargets = GetTargetStudentCodes();

            bool started = await QASmartClass.Services.VncBroadcastService.Instance.StartBroadcastAsync(isForceWatch, selectedTargets);

            if (started)
            {
                SaveEvent("VNC_BROADCAST", "GV", $"Bắt đầu phát VNC: {QASmartClass.Services.VncBroadcastService.Instance.VncPort} | {QASmartClass.Services.VncBroadcastService.Instance.VncSessionCode}");
            }
            else
            {
                MessageBox.Show("Khởi động VNC Server thất bại. Vui lòng kiểm tra lại cấu hình hệ thống.", "Lỗi tích hợp VNC", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Dừng truyền phát VNC
        private void StopVncBroadcast()
        {
            if (!QASmartClass.Services.VncBroadcastService.Instance.IsVncBroadcasting) return;

            QASmartClass.Services.VncBroadcastService.Instance.StopBroadcast();
            SaveEvent("VNC_BROADCAST", "GV", "Dừng phát VNC");
        }

        // Tắt tiến trình VNC Server
        private void StopVncProcessOnly()
        {
            try
            {
                foreach (var p in System.Diagnostics.Process.GetProcessesByName("vnctool-server"))
                {
                    try
                    {
                        p.Kill();
                        p.Dispose();
                        Log.Information("[VNC-Broadcast] Zombie process killed");
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.Debug("[VNC-Broadcast] Kill process error: {Err}", ex.Message);
            }
        }

        private void OnVncBroadcastStatusChanged(object? sender, string status)
        {
            Dispatcher.Invoke(() =>
            {
                bool isBroadcasting = status == "STARTED";
                SyncVncUiState(isBroadcasting);
            });
        }

        private void OnVncBroadcastTimerTick(object? sender, string timeDisplay)
        {
            Dispatcher.Invoke(() =>
            {
                txtTimer.Text = timeDisplay;
                txtStatus.Text = $"Đang phát VNC — {timeDisplay}";
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            });
        }

        private void SyncVncUiState(bool isBroadcasting)
        {
            _isVncBroadcasting = isBroadcasting;

            if (isBroadcasting)
            {
                // Disable configuration UI
                cmbResolution.IsEnabled = false;
                cmbFPS.IsEnabled = false;
                cmbQuality.IsEnabled = false;
                btnBroadcast.IsEnabled = false;
                btnTurboBroadcast.IsEnabled = false;
                btnTextBroadcast.IsEnabled = false;
                btnMulticastBroadcast.IsEnabled = false;

                btnVncBroadcast.Content = "⏹️ Dừng VNC";
                btnVncBroadcast.Background = new SolidColorBrush(Color.FromRgb(211, 47, 47));

                screenIdleState.Visibility = Visibility.Collapsed;
                screenLiveState.Visibility = Visibility.Visible;
                txtRes.Text = $"1920×1080  30 fps (VNC)";
                txtPreviewLabel.Text = "Đang chiếu: VNC-LAN";
                txtBroadcastStatus.Text = "Hệ thống đang truyền phát màn hình hiệu năng cao (VNC Rust)...";
                txtLiveRecipients.Text = $"{txtRecipientCount.Text.Replace("→ ", "")}";
                btnPause.Visibility = Visibility.Visible;
                btnPause.Content = "⏸️ Tạm dừng";
                btnPause.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                btnPause.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0));
                
                txtTimer.Text = QASmartClass.Services.VncBroadcastService.Instance.ElapsedTimeDisplay;
                txtStatus.Text = $"Đang phát VNC — {txtTimer.Text}";
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                txtBroadcastInfo.Text = $"1920×1080 | VNC-LAN | {txtRecipientCount.Text.Replace("→ ", "")}";
            }
            else
            {
                // Enable configuration UI
                cmbResolution.IsEnabled = true;
                cmbFPS.IsEnabled = true;
                cmbQuality.IsEnabled = true;
                btnBroadcast.IsEnabled = true;
                btnTurboBroadcast.IsEnabled = true;
                btnTextBroadcast.IsEnabled = true;
                btnMulticastBroadcast.IsEnabled = true;

                btnVncBroadcast.Content = "⚡ Phát VNC (Tối ưu mạng)";
                btnVncBroadcast.Background = new SolidColorBrush(Color.FromRgb(255, 140, 66));

                btnPause.Visibility = Visibility.Collapsed;
                screenIdleState.Visibility = Visibility.Visible;
                screenLiveState.Visibility = Visibility.Collapsed;
                txtStatus.Text = "Đã dừng phát VNC";
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
            }
        }
    }
}
