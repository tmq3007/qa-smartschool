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
using QASmartClass.Classroom.Helpers;
using QASmartTouch.Services.Camera;

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
            InitializeHighFpsOptions();
            LoadDynamicWhitelist();
            SetupConfigWatcher();
            // btnTurboBroadcast.Visibility = QASmartTouch.Services.AppSettings.Broadcast_AllowTurboMode ? Visibility.Visible : Visibility.Collapsed; // Tạm ẩn theo yêu cầu

            _cacheRefreshTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _cacheRefreshTimer.Tick += CacheRefreshTimer_Tick;

            // Đăng ký ngay từ Constructor để dù trang chạy ngầm (chưa mở tab Broadcast) vẫn dừng phát chuẩn xác khi bấm từ FloatingModeBar
            ScreenShareController.Instance.StopRequested -= OnStopShareRequested;
            ScreenShareController.Instance.StopRequested += OnStopShareRequested;

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
                    RefreshInlineSourcePreviews();
                    UpdateSelectedSourcePreviewThumb();
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
                ScreenShareController.Instance.StopRequested -= OnStopShareRequested;
                ScreenShareController.Instance.StopRequested += OnStopShareRequested;
                LiveBroadcastMediaService.Instance.StateChanged -= OnLiveBroadcastMediaStateChanged;
                LiveBroadcastMediaService.Instance.StateChanged += OnLiveBroadcastMediaStateChanged;
                if (ClassroomAppContext.Network != null)
                {
                    ClassroomAppContext.Network.StudentConnected -= OnBroadcastNetworkStudentConnected;
                    ClassroomAppContext.Network.StudentConnected += OnBroadcastNetworkStudentConnected;
                    ClassroomAppContext.Network.StudentDisconnected -= OnBroadcastNetworkStudentDisconnected;
                    ClassroomAppContext.Network.StudentDisconnected += OnBroadcastNetworkStudentDisconnected;
                }
                SyncMeetPageMediaUi();

                // Sync VNC UI state
                SyncVncUiState(QASmartClass.Services.VncBroadcastService.Instance.IsVncBroadcasting);
            };
            Unloaded += (_, _) =>
            {
                _cacheRefreshTimer.Stop();
                LiveBroadcastMediaService.Instance.StateChanged -= OnLiveBroadcastMediaStateChanged;
                if (ClassroomAppContext.Network != null)
                {
                    ClassroomAppContext.Network.StudentConnected -= OnBroadcastNetworkStudentConnected;
                    ClassroomAppContext.Network.StudentDisconnected -= OnBroadcastNetworkStudentDisconnected;
                }

                // Nếu GV đang phát màn hình hoặc chiếu trang QA SmartClass và chỉ chuyển tab trong ClassroomShell,
                // giữ nguyên luồng phát chạy ngầm cho tới khi GV bấm Dừng phát hoặc kết thúc tiết.
                if (!_isBroadcasting)
                {
                    ApplyScreenCaptureExclusion(false);
                    _isPaused = false;
                    _isBroadcastPausedDueToPrivacy = false;
                    _timer?.Stop();
                    _captureTimer?.Dispose();
                    if (_privacyBorder != null)
                    {
                        _privacyBorder.Close();
                        _privacyBorder = null;
                    }
                    ScreenShareController.Instance.StopRequested -= OnStopShareRequested;
                }

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

        // Kết thúc lớp → dừng chiếu màn hình (gửi lệnh STOP cho HS trước khi mạng bị đóng)
        private void OnStopShareRequested(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(StopAllBroadcasting);
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

        // [MYTHWARE_LITE] Cho phép nút 1-Click Broadcast trên Top Command Ribbon mở hộp chọn nguồn trực quan và bật/tắt chiếu màn hình kèm Thanh điều khiển nổi (Floating Share Bar + Live PiP)
        private bool _skipConfirmForQuickBroadcast = false;
        private FloatingBroadcastBarWindow? _floatingBroadcastBar;
        private bool _hideClassroomShellFromStudents = false;

        public void ToggleMythwareQuickBroadcast()
        {
            if (_isBroadcasting || _isVncBroadcasting)
            {
                StopAllBroadcasting();
                return;
            }

            // Mở hộp chọn nguồn (Toàn màn hình / Cửa sổ ứng dụng / Vùng chọn / Trang QA SmartClass) để GV thấy rõ mình sắp chiếu gì
            if (!OpenSourcePicker(ScreenSourceKind.Screen, out bool autoMinimizeShell))
            {
                return;
            }

            _skipConfirmForQuickBroadcast = true;
            try
            {
                Broadcast_Click(this, new RoutedEventArgs());
                if (_isBroadcasting && autoMinimizeShell)
                {
                    var shell = (Window.GetWindow(this) as ClassroomShell)
                        ?? Application.Current?.Windows.OfType<ClassroomShell>().FirstOrDefault();
                    if (shell != null && shell.IsVisible) shell.WindowState = WindowState.Minimized;
                }
            }
            finally
            {
                _skipConfirmForQuickBroadcast = false;
            }
        }

        // ═══ CÁC ĐIỂM VÀO TRỰC TIẾP TỪ FLOATING MODE BAR (CHẠY XUYÊN SUỐT Ở CẢ 3 CHẾ ĐỘ) ═══

        public void StartOrSwitchSourceFromFloatingBar()
        {
            if (_isBroadcasting)
            {
                OpenSourcePicker(ScreenSourceKind.Screen, out _);
            }
            else
            {
                PickSourceAndStart_Click(this, new RoutedEventArgs());
            }
        }

        public void StartOrSwitchRegionFromFloatingBar()
        {
            if (_isBroadcasting)
            {
                PickRegion_Click(this, new RoutedEventArgs());
            }
            else
            {
                PickRegionAndStart_Click(this, new RoutedEventArgs());
            }
        }

        public void StartOrSwitchQaPageFromFloatingBar()
        {
            if (_isBroadcasting)
            {
                OpenSourcePicker(ScreenSourceKind.QaClassPage, out _);
            }
            else
            {
                PickQaPageAndStart_Click(this, new RoutedEventArgs());
            }
        }

        public void ShareSmartTouchBoardImmediately()
        {
            var src = new ScreenSourceInfo
            {
                Kind = ScreenSourceKind.QaClassPage,
                PageFormId = "SMART_TOUCH",
                Title = "Bảng trắng tương tác SMART TOUCH",
                Width = (int)SystemParameters.PrimaryScreenWidth,
                Height = (int)SystemParameters.PrimaryScreenHeight,
                AutoNavigateToPage = false
            };

            ApplyPickedSource(src);

            if (!_isBroadcasting)
            {
                _skipConfirmForQuickBroadcast = true;
                try
                {
                    Broadcast_Click(this, new RoutedEventArgs());
                }
                finally
                {
                    _skipConfirmForQuickBroadcast = false;
                }
            }
        }

        public void TogglePauseFromFloatingBar()
        {
            if (_isBroadcasting || _isVncBroadcasting)
            {
                Pause_Click(this, new RoutedEventArgs());
            }
        }

        public void ShowFloatingPipFromFloatingBar()
        {
            ShowFloatingBar_Click(this, new RoutedEventArgs());
        }

        private QASmartClass.Shared.FloatingModeBar? GetUnifiedEasyTouchOrb()
        {
            if (Application.Current is QASmartTouch.App app)
            {
                app.EnsureFloatingModeBar();
                return app._floatingModeBar;
            }
            return null;
        }

        private void EnsureFloatingBroadcastBarShown(string sourceDescription, string resolution, string fps)
        {
            try
            {
                // [MYTHWARE_LITE] Hợp nhất toàn bộ điều khiển trình chiếu, âm thanh, camera & khóa HS vào 1 Cục Tròn EasyTouch duy nhất (FloatingModeBar)
                var orb = GetUnifiedEasyTouchOrb();
                if (orb != null)
                {
                    if (!orb.IsBroadcastBridgeBound)
                    {
                        orb.IsBroadcastBridgeBound = true;
                        orb.ShareCurrentInsteadRequested += (_, _) =>
                        {
                            Dispatcher.Invoke(SwitchBroadcastToActiveScreenInstead);
                        };
                        orb.ChangeSourceRequested += (_, _) =>
                        {
                            Dispatcher.Invoke(() => OpenSourcePicker(ScreenSourceKind.Screen, out _));
                        };
                        orb.SelectRegionRequested += (_, _) =>
                        {
                            Dispatcher.Invoke(() => PickRegion_Click(this, new RoutedEventArgs()));
                        };
                        orb.TogglePauseRequested += (_, _) =>
                        {
                            Dispatcher.Invoke(() => Pause_Click(this, new RoutedEventArgs()));
                        };
                        orb.StopRequested += (_, _) =>
                        {
                            Dispatcher.Invoke(StopAllBroadcasting);
                        };
                        orb.RestoreShellRequested += (_, _) =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                var shell = (Window.GetWindow(this) as ClassroomShell) ?? (Application.Current?.MainWindow as ClassroomShell);
                                if (shell != null)
                                {
                                    if (shell.WindowState == WindowState.Minimized)
                                        shell.WindowState = WindowState.Maximized;
                                    shell.Show();
                                    shell.Activate();
                                }
                            });
                        };
                        orb.HideClassroomShellChanged += (_, hideShell) =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                _hideClassroomShellFromStudents = hideShell;
                                ScreenSourceInfo? curPicked;
                                lock (_srcLock) curPicked = _picked;
                                bool shouldExclude = _hideClassroomShellFromStudents && (curPicked == null || curPicked.Kind == ScreenSourceKind.Screen);
                                ApplyScreenCaptureExclusion(shouldExclude);
                            });
                        };
                        orb.AllowStudentExitChanged += (_, allowExit) =>
                        {
                            Dispatcher.Invoke(() => SetAllowStudentExitLive(allowExit));
                        };
                    }

                    orb.SetHideClassroomShellCheck(_hideClassroomShellFromStudents);
                    orb.SetAllowStudentExitState(chkAllowStudentExit?.IsChecked == true);
                    int recipientCount = ClassroomAppContext.Network?.ConnectedCount ?? 0;
                    orb.UpdateStatus(_isPaused || _isBroadcastPausedDueToPrivacy, "00:00", recipientCount, sourceDescription);
                    UpdateGoogleMeetSourceLockState();
                }

                /* // [MYTHWARE_LITE] Đã hợp nhất vào cục tròn FloatingModeBar ở trên — không mở thêm cửa sổ thanh nổi thứ 2
                if (_floatingBroadcastBar == null || !_floatingBroadcastBar.IsLoaded)
                {
                    _floatingBroadcastBar = new FloatingBroadcastBarWindow(_hideClassroomShellFromStudents);
                    _floatingBroadcastBar.Show();
                }
                */
            }
            catch (Exception ex)
            {
                Log.Warning("[BroadcastPage] EnsureFloatingBroadcastBarShown error: {Err}", ex.Message);
            }
        }

        private void CloseFloatingBroadcastBar()
        {
            try
            {
                if (_floatingBroadcastBar != null)
                {
                    _floatingBroadcastBar.Close();
                    _floatingBroadcastBar = null;
                }
            }
            catch { }
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
                // app → ClassroomAppContext (refactored)
                string activeClass = ClassroomAppContext.Session?.CurrentClassName;
                if (string.IsNullOrEmpty(activeClass))
                {
                    activeClass = ClassroomAppContext.ClassRoster.ActiveRoster?.ClassName;
                }

                using (var db = new QASmartClass.Data.AppDbContext())
                {
                    System.Collections.Generic.List<QASmartClass.Data.Student>? baseList = null;

                    if (!string.IsNullOrEmpty(activeClass))
                    {
                        var filtered = db.Students?
                            .Where(s => s.ClassName == activeClass)
                            .ToList();
                        if (filtered != null && filtered.Any())
                        {
                            baseList = filtered;
                        }
                    }

                    if (baseList == null && ClassroomAppContext.ClassRoster.ActiveRoster != null)
                    {
                        var activeRosterId = ClassroomAppContext.ClassRoster.ActiveRoster.Id;
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
                            baseList = rosterStudents;
                        }
                    }

                    baseList ??= db.Students?.ToList() ?? new System.Collections.Generic.List<QASmartClass.Data.Student>();
                    return MergeLiveNetworkStudents(baseList);
                }
            }
            catch
            {
                return MergeLiveNetworkStudents(new System.Collections.Generic.List<QASmartClass.Data.Student>());
            }
        }

        private System.Collections.Generic.List<QASmartClass.Data.Student> MergeLiveNetworkStudents(
            System.Collections.Generic.List<QASmartClass.Data.Student> dbList)
        {
            var merged = new System.Collections.Generic.List<QASmartClass.Data.Student>(dbList);
            var onlineClients = ClassroomAppContext.Network?.GetConnectedStudents();
            if (onlineClients != null)
            {
                foreach (var c in onlineClients)
                {
                    var existing = merged.FirstOrDefault(s =>
                        !string.IsNullOrWhiteSpace(s.StudentCode) &&
                        s.StudentCode.Equals(c.Code, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        existing.IsOnline = true;
                        existing.IPAddress = c.IPAddress;
                        existing.PCName = c.PCName;
                    }
                    else
                    {
                        merged.Add(new QASmartClass.Data.Student
                        {
                            StudentCode = c.Code,
                            FullName = string.IsNullOrWhiteSpace(c.Name) ? c.Code : c.Name,
                            PCName = c.PCName,
                            IPAddress = c.IPAddress,
                            ClassName = ClassroomAppContext.Network?.CurrentClassName ?? "",
                            IsOnline = true
                        });
                    }
                }
            }
            return VietnameseNameHelper.SortByVietnameseName(merged, s => s.FullName);
        }

        private void OnBroadcastNetworkStudentConnected(object? sender, Services.StudentConnectedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadRecipientCount();
                if (rbIndividual?.IsChecked == true) LoadStudentList();
                else UpdateRecipientCount();
            }));
        }

        private void OnBroadcastNetworkStudentDisconnected(object? sender, string code)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadRecipientCount();
                if (rbIndividual?.IsChecked == true) LoadStudentList();
                else UpdateRecipientCount();
            }));
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
                // app → ClassroomAppContext (refactored)
                int count = 0;
                if (ClassroomAppContext.Network != null)
                {
                    count = ClassroomAppContext.Network.ConnectedCount;
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
                SelectBroadcastSubTab(tag);
            }
        }

        public void SelectBroadcastSubTab(string tag)
        {
            var tabs = new[] { tabScreen, tabNotice, tabMedia, tabWeb };
            Border? activeBorder = tag switch
            {
                "notice" => tabNotice,
                "media" => tabMedia,
                "web" => tabWeb,
                _ => tabScreen
            };

            foreach (var t in tabs)
            {
                if (t == null) continue;
                t.Background = Brushes.White;
                t.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            }
            if (activeBorder != null)
            {
                activeBorder.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                activeBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210));
            }

            if (panelScreen != null) panelScreen.Visibility = tag == "screen" ? Visibility.Visible : Visibility.Collapsed;
            if (panelNotice != null) panelNotice.Visibility = tag == "notice" ? Visibility.Visible : Visibility.Collapsed;
            if (panelMedia != null) panelMedia.Visibility = tag == "media" ? Visibility.Visible : Visibility.Collapsed;
            if (panelWeb != null) panelWeb.Visibility = tag == "web" ? Visibility.Visible : Visibility.Collapsed;
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
                _lastCompositedCamSeq = -1;
                _lastIsolatedSourceBitmap = null;
                _pendingSwitchCandidate = null;
                _pendingSwitchLabel = "";
                _sharedWindowClosedStopTriggered = false;
                var stopShell = (Window.GetWindow(this) as ClassroomShell) ?? (Application.Current?.MainWindow as ClassroomShell);
                stopShell?.ClearPinnedBroadcastPage();
                _ = LiveBroadcastMediaService.Instance.StopSessionAsync();
                
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
                CloseFloatingBroadcastBar();

                screenIdleState.Visibility = Visibility.Visible;
                screenLiveState.Visibility = Visibility.Collapsed;
                txtStatus.Text = "Đã dừng phát";
                // V22-14: Status color đỏ khi dừng
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));

                try
                {
                    // app → ClassroomAppContext (refactored)
                    var stopCmd1 = "CMD|SCREEN_BROADCAST_STOP|0";
                    var stopCmd2 = "CMD|BROADCAST_STOP|0";

                    // Luôn gửi qua local bus
                    ClassroomAppContext.DispatchCommand(stopCmd1);
                    ClassroomAppContext.DispatchCommand(stopCmd2);

                    // Nếu có network → gửi thêm qua mạng
                    if (ClassroomAppContext.Network?.IsBroadcasting == true)
                    {
                        // === UPGRADE_06 + UPGRADE_10: Gửi STOP cho target students trước ===
                        try { await SendCommandToTargetsAsync(stopCmd1); } catch { /* ignore */ }
                        try { await SendCommandToTargetsAsync(stopCmd2); } catch { /* ignore */ }

                        // Fallback: vẫn gửi ALL để đảm bảo không HS nào bị kẹt (Safety-first)
                        await ClassroomAppContext.Network.SendCommandAsync(stopCmd1);
                        await ClassroomAppContext.Network.SendCommandAsync(stopCmd2);
                    }
                    _currentEmergencyOtp = string.Empty; // === UPGRADE_11: Reset OTP ===
                    ClassroomAppContext.BroadcastState.IsScreenBroadcastActive = false;
                    ClassroomAppContext.BroadcastState.ScreenCapturePath = string.Empty;
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
                // app → ClassroomAppContext (refactored)
                var targetCodes = GetTargetStudentCodes();
                int recipientCount = 0;
                if (targetCodes == null)
                {
                    recipientCount = ClassroomAppContext.Network?.ConnectedCount ?? 0;
                }
                else
                {
                    recipientCount = targetCodes.Count;
                }

                // === UPGRADE_06: Phân biệt rõ null (ALL) vs empty list (chưa chọn ai) ===
                if (targetCodes != null && targetCodes.Count == 0)
                {
                    // Trường hợp: GV chọn "Nhóm" hoặc "Từng HS" nhưng không tick ai
                    ClassroomDialog.Warn(
                        "⚠️ Chưa chọn đối tượng nhận chiếu màn hình.\n\n" +
                        "Vui lòng chọn ít nhất 1 học sinh hoặc 1 nhóm trước khi bắt đầu phát.", "Lỗi cấu hình — Không có đối tượng nhận");
                    Log.Warning("[BroadcastPage] UPGRADE_06: Target list rỗng (Group/Individual mode nhưng chưa tick HS). Chặn broadcast.");
                    return;
                }

                if (recipientCount == 0 && !_skipConfirmForQuickBroadcast)
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

            // [MYTHWARE_LITE] Mặc định khóa cứng toàn màn hình Học sinh khi Giáo viên bấm Share (chỉ mở khóa khi tích chkAllowStudentExit).
            // Bỏ hộp thoại hỏi xác nhận rườm rà để Giáo viên bấm Share là phát ngay lập tức.
            bool allowExit = chkAllowStudentExit?.IsChecked == true;
            chkForceWatch.IsChecked = !allowExit;
            /* [MYTHWARE_LITE] Đã ẩn hộp thoại xác nhận cũ
            // Xác nhận lại số lượng học sinh nhận trình chiếu khi không bật Force Watch
            if (chkForceWatch.IsChecked == false && !_skipConfirmForQuickBroadcast)
            {
                try
                {
                    // app → ClassroomAppContext (refactored)
                    string activeClass = ClassroomAppContext.Session?.CurrentClassName;
                    if (string.IsNullOrEmpty(activeClass))
                    {
                        activeClass = ClassroomAppContext.ClassRoster.ActiveRoster?.ClassName;
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
                        if (ClassroomAppContext.CurrentGroups?.Count > 0)
                        {
                            foreach (var cb in selectedGroups)
                            {
                                if (cb.Tag is int idx && idx < ClassroomAppContext.CurrentGroups.Count)
                                    totalMembers += ClassroomAppContext.CurrentGroups[idx].Members.Count;
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
                // app → ClassroomAppContext (refactored)
                string activeClass = ClassroomAppContext.Session?.CurrentClassName;
                string classText = !string.IsNullOrEmpty(activeClass) ? $" lớp {activeClass}" : "";

                int onlineCount = ClassroomAppContext.Network?.ConnectedCount ?? 0;
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
            */

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

            // === UPGRADE_13: Không bật Exclusion khi chạy ở chế độ Turbo, hoặc khi GV chọn chiếu Vùng màn hình / Trang trong QA SmartClass ===
            ScreenSourceInfo? curPicked;
            lock (_srcLock) curPicked = _picked;

            // [MYTHWARE_LITE] Mặc định cho phép chiếu đúng những gì Giáo viên đang nhìn thấy trên màn hình;
            // chỉ ẩn cửa sổ Quản lý lớp khi Giáo viên chủ động bật "Ẩn cửa sổ Quản lý lớp khỏi màn hình Học sinh".
            bool shouldExclude = _hideClassroomShellFromStudents && (curPicked == null || curPicked.Kind == ScreenSourceKind.Screen);
            if (QASmartTouch.Services.AppSettings.Broadcast_TurboMode && QASmartTouch.Services.AppSettings.Broadcast_TurboDisableExclusion)
            {
                shouldExclude = false;
            }
            if (curPicked != null && (curPicked.Kind == ScreenSourceKind.Region || curPicked.Kind == ScreenSourceKind.QaClassPage))
            {
                shouldExclude = false;
            }
            ApplyScreenCaptureExclusion(shouldExclude);
            _isFirstCapture = true;
            _lastFrameSampleHash = 0;
            _lastCompositedCamSeq = -1;
            _lastIsolatedSourceBitmap = null;
            _pendingSwitchCandidate = null;
            _pendingSwitchLabel = "";
            _sharedWindowClosedStopTriggered = false;
            _settlementKeyframeSent = false;
            _lastMotionDetectedUtc = DateTime.UtcNow;

            if (curPicked != null && curPicked.Kind == ScreenSourceKind.QaClassPage && string.Equals(curPicked.PageFormId, "CURRENT", StringComparison.OrdinalIgnoreCase))
            {
                var pinShell = (Window.GetWindow(this) as ClassroomShell) ?? (Application.Current?.MainWindow as ClassroomShell);
                pinShell?.PinCurrentPageForBroadcast();
            }

            // Khởi động luồng Âm thanh (Hệ thống + Micro) & Camera PiP chuẩn Google Meet
            _ = LiveBroadcastMediaService.Instance.StartSessionAsync();

            // CR-06 FIX: Use fast JPEG encoding for real-time low-latency screen broadcast
            _cachedCompressionFormat = "JPEG";
            _seconds = 0;
            RefreshCachedBroadcastParams();

            var resolution = (cmbResolution.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1920×1080";
            var fps = (cmbFPS.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "30 fps";
            var source = curPicked != null
                ? DescribeSource(curPicked)
                : (rbFullScreen.IsChecked == true ? "Toàn màn hình" : rbCamera.IsChecked == true ? "Camera" : "Cửa sổ");

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

            // Show privacy border (vẽ khung quanh vùng chọn nếu chiếu Region; ẩn nếu chiếu trang nội bộ QA SmartClass)
            if (curPicked?.Kind != ScreenSourceKind.QaClassPage)
            {
                _privacyBorder = new BroadcastPrivacyBorder();
                if (curPicked?.Kind == ScreenSourceKind.Region)
                {
                    _privacyBorder.SetBounds(curPicked.DipLeft, curPicked.DipTop, curPicked.DipWidth, curPicked.DipHeight);
                }
                _privacyBorder.Show();
                _privacyBorder.SetState(_isPaused || _isBroadcastPausedDueToPrivacy);
            }

            if (curPicked != null && curPicked.Kind == ScreenSourceKind.QaClassPage && curPicked.AutoNavigateToPage && !string.IsNullOrEmpty(curPicked.PageFormId) && curPicked.PageFormId != "CURRENT")
            {
                var shell = (Window.GetWindow(this) as ClassroomShell) ?? (Application.Current?.MainWindow as ClassroomShell);
                shell?.NavigateToQaPageForBroadcast(curPicked.PageFormId);
            }

            // [MYTHWARE_LITE] Hiển thị Thanh điều khiển nổi (Floating Share Bar + Live PiP Monitor)
            EnsureFloatingBroadcastBarShown(source, resolution, fps);

            // Timer đếm thời gian
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, ev) =>
            {
                _seconds++;
                string timerStr = $"{_seconds / 60:D2}:{_seconds % 60:D2}";
                txtTimer.Text = timerStr;
                ScreenSourceInfo? latestPicked;
                lock (_srcLock) latestPicked = _picked;
                string latestSource = latestPicked != null ? DescribeSource(latestPicked) : source;
                txtStatus.Text = $"Đang phát — {latestSource} — {timerStr}";
                // V22-14: Status color xanh khi đang phát
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                string otpInfo = !string.IsNullOrEmpty(_currentEmergencyOtp) ? $" | OTP Khẩn cấp: {_currentEmergencyOtp}" : "";
                txtBroadcastInfo.Text = $"{resolution} | {fps} | {txtRecipientCount.Text.Replace("→ ", "")}{otpInfo}";
                int recipientCnt = ClassroomAppContext.Network?.ConnectedCount ?? 0;
                _floatingBroadcastBar?.UpdateStatus(_isPaused || _isBroadcastPausedDueToPrivacy, timerStr, recipientCnt, latestSource);
                (Application.Current as QASmartTouch.App)?._floatingModeBar?.UpdateStatus(_isPaused || _isBroadcastPausedDueToPrivacy, timerStr, recipientCnt, latestSource);
                UpdateGoogleMeetSourceLockState();
                RefreshCachedBroadcastParams();
            };
            _timer.Start();

            // Timer chụp màn hình — interval tùy FPS (hỗ trợ 30 / 60 / 120 FPS)
            int targetFps = GetSelectedTargetFps();
            int captureMs = Math.Max(8, (int)Math.Round(1000.0 / targetFps));
            // V22-02: Background thread capture — tránh lag UI ở 60-120fps
            _captureTimer = new System.Threading.Timer(
                async _ => await CaptureAndSendAsync(),
                null, 0, captureMs);

            // Timer tự gọi lần đầu nhờ dueTime=0, không cần gọi thủ công

            try
            {
                // app → ClassroomAppContext (refactored)
                ClassroomAppContext.BroadcastState.IsScreenBroadcastActive = true;
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

                if (ClassroomAppContext.Network?.IsBroadcasting == true)
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
                    _ = ClassroomAppContext.Network.SendCommandAsync(configCmd);
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
        private uint _lastFrameSampleHash = 0;
        private long _lastCompositedCamSeq = -1;
        private bool _suppressMeetPageMediaEvents = false;
        private byte[]? _lastEncodedFrameBytes = null;
        private byte[]? _lastSettlementBytes = null;
        private DateTime _lastEncodedFrameUtc = DateTime.MinValue;
        private DateTime _lastMotionDetectedUtc = DateTime.MinValue;
        private DateTime _lastKeepAlivePushUtc = DateTime.MinValue;
        private bool _settlementKeyframeSent = false;
        private DateTime _lastTcpBroadcastCmdUtc = DateTime.MinValue;
        private volatile string _activeQualityPreset = "ADAPTIVE";
        private volatile bool _cachedShowTeacherCursor = true;

        private void InitializeHighFpsOptions()
        {
            try
            {
                if (cmbFPS == null) return;
                bool has24 = cmbFPS.Items.OfType<ComboBoxItem>().Any(i => i.Tag?.ToString() == "24");
                if (!has24)
                {
                    int insertIdx = Math.Min(4, cmbFPS.Items.Count);
                    cmbFPS.Items.Insert(insertIdx, new ComboBoxItem { Content = "Chuẩn điện ảnh (24 fps)", Tag = "24" });
                }
                bool has60 = cmbFPS.Items.OfType<ComboBoxItem>().Any(i => i.Tag?.ToString() == "60");
                if (!has60)
                {
                    cmbFPS.Items.Add(new ComboBoxItem { Content = "Siêu mượt (60 fps)", Tag = "60" });
                }
                bool has120 = cmbFPS.Items.OfType<ComboBoxItem>().Any(i => i.Tag?.ToString() == "120");
                if (!has120)
                {
                    cmbFPS.Items.Add(new ComboBoxItem { Content = "Cực đại (120 fps)", Tag = "120" });
                }

                cmbFPS.SelectionChanged -= CmbFPS_SelectionChanged;
                cmbFPS.SelectionChanged += CmbFPS_SelectionChanged;
                if (cmbResolution != null)
                {
                    cmbResolution.SelectionChanged -= CmbQualityOrResolution_SelectionChanged;
                    cmbResolution.SelectionChanged += CmbQualityOrResolution_SelectionChanged;
                }
                if (cmbQuality != null)
                {
                    cmbQuality.SelectionChanged -= CmbQualityOrResolution_SelectionChanged;
                    cmbQuality.SelectionChanged += CmbQualityOrResolution_SelectionChanged;
                }

                // Mặc định khởi tạo theo chuẩn Google Meet & Mythware: Tự động Thích ứng (1080p, 30 FPS mượt + Chốt nét Q=94 sau 220ms)
                ApplyQualityPreset("ADAPTIVE");
            }
            catch (Exception ex)
            {
                Log.Debug("[BroadcastPage] InitializeHighFpsOptions error: {Err}", ex.Message);
            }
        }

        private bool _isApplyingPreset = false;

        private void QualityPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string presetTag)
            {
                ApplyQualityPreset(presetTag);
            }
        }

        private bool _suppressAllowExitSync;

        private void AllowStudentExit_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressAllowExitSync) return;
            SetAllowStudentExitLive(chkAllowStudentExit?.IsChecked == true);
        }

        private void SetAllowStudentExitLive(bool allowExit)
        {
            _suppressAllowExitSync = true;
            try
            {
                if (chkAllowStudentExit != null && chkAllowStudentExit.IsChecked != allowExit)
                {
                    chkAllowStudentExit.IsChecked = allowExit;
                }
                if (chkForceWatch != null)
                {
                    chkForceWatch.IsChecked = !allowExit;
                }
                _cachedIsForceWatch = !allowExit;
                _floatingBroadcastBar?.SetAllowStudentExitState(allowExit);
                (Application.Current as QASmartTouch.App)?._floatingModeBar?.SetAllowStudentExitState(allowExit);
                _lastTcpBroadcastCmdUtc = DateTime.MinValue;

                if ((_isBroadcasting || _isVncBroadcasting) && ClassroomAppContext.Network?.IsBroadcasting == true)
                {
                    string cmd = $"CMD|BROADCAST_ALLOW_EXIT|{(allowExit ? "1" : "0")}";
                    var targetCodes = GetTargetStudentCodes();
                    if (targetCodes == null)
                    {
                        _ = ClassroomAppContext.Network.SendCommandAsync(cmd);
                    }
                    else
                    {
                        _ = ClassroomAppContext.Network.SendToStudentsAsync(targetCodes, cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug("[BroadcastPage] SetAllowStudentExitLive error: {Err}", ex.Message);
            }
            finally
            {
                _suppressAllowExitSync = false;
            }
        }

        private void CursorOption_Changed(object sender, RoutedEventArgs e)
        {
            _cachedShowTeacherCursor = chkShowTeacherCursor?.IsChecked != false;
            _lastFrameSampleHash = 0;
            _settlementKeyframeSent = false;
        }

        private void CmbQualityOrResolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isApplyingPreset) return;
            RefreshCachedBroadcastParams();
            _lastFrameSampleHash = 0;
            _settlementKeyframeSent = false;
        }

        private void ApplyQualityPreset(string preset)
        {
            _isApplyingPreset = true;
            try
            {
                _activeQualityPreset = preset;
                string targetResMatch = "1920";
                string targetFpsTag = "30";
                string targetQualityTag = "80";
                string hintText = "✨ Đang bật Thích ứng Kép: 30 FPS mượt khi thao tác • Chốt nét căng Q=94 sau 220ms dừng tay";

                if (string.Equals(preset, "CRISP_TEXT", StringComparison.OrdinalIgnoreCase))
                {
                    targetResMatch = "1920";
                    targetFpsTag = "24";
                    targetQualityTag = "94";
                    hintText = "📄 Siêu nét Chữ & Code: Giữ nguyên độ phân giải gốc (Q=94, 24 FPS) — rõ nét từng chữ nhỏ & công thức Toán";
                }
                else if (string.Equals(preset, "SMOOTH_VIDEO", StringComparison.OrdinalIgnoreCase))
                {
                    targetResMatch = "1920";
                    targetFpsTag = "60";
                    targetQualityTag = "80";
                    hintText = "🎬 Video / Mô phỏng 3D: 60 FPS liên tục + tự chốt nét khi dừng video";
                }

                if (cmbResolution != null)
                {
                    var resItem = cmbResolution.Items.OfType<ComboBoxItem>()
                        .FirstOrDefault(i => (i.Content?.ToString() ?? "").Contains(targetResMatch));
                    if (resItem != null) cmbResolution.SelectedItem = resItem;
                }

                if (cmbFPS != null)
                {
                    var fpsItem = cmbFPS.Items.OfType<ComboBoxItem>()
                        .FirstOrDefault(i => i.Tag?.ToString() == targetFpsTag);
                    if (fpsItem != null) cmbFPS.SelectedItem = fpsItem;
                }

                if (cmbQuality != null)
                {
                    var qItem = cmbQuality.Items.OfType<ComboBoxItem>()
                        .FirstOrDefault(i => i.Tag?.ToString() == targetQualityTag)
                        ?? cmbQuality.Items.OfType<ComboBoxItem>().LastOrDefault();
                    if (qItem != null) cmbQuality.SelectedItem = qItem;
                }

                if (txtPresetHint != null) txtPresetHint.Text = hintText;

                var activeBg = new SolidColorBrush(Color.FromRgb(29, 78, 216));
                var normalBg = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                var normalFg = new SolidColorBrush(Color.FromRgb(51, 65, 85));
                Button?[] btns = { btnPresetAdaptive, btnPresetCrispText, btnPresetSmoothVideo };
                foreach (var b in btns)
                {
                    if (b == null) continue;
                    bool isSel = string.Equals(b.Tag as string, preset, StringComparison.OrdinalIgnoreCase);
                    b.Background = isSel ? activeBg : normalBg;
                    b.Foreground = isSel ? Brushes.White : normalFg;
                    b.FontWeight = isSel ? FontWeights.Bold : FontWeights.SemiBold;
                }
            }
            finally
            {
                _isApplyingPreset = false;
            }

            RefreshCachedBroadcastParams();
            _lastFrameSampleHash = 0;
            _settlementKeyframeSent = false;

            if (_isBroadcasting && _captureTimer != null)
            {
                int targetFps = GetSelectedTargetFps();
                int captureMs = Math.Max(8, (int)Math.Round(1000.0 / targetFps));
                _captureTimer.Change(0, captureMs);
                var resolution = (cmbResolution?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1920×1080";
                var fpsText = (cmbFPS?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? $"{targetFps} fps";
                if (txtRes != null) txtRes.Text = $"{resolution}  {fpsText}";
            }
        }

        private int GetSelectedTargetFps()
        {
            int targetFps = 60;
            try
            {
                var fpsItem = cmbFPS?.SelectedItem as ComboBoxItem;
                if (fpsItem?.Tag is string fpsTag && int.TryParse(fpsTag, out int parsedFps) && parsedFps > 0)
                {
                    targetFps = parsedFps;
                }
                else if (fpsItem?.Content != null)
                {
                    var fpsDigits = new string((fpsItem.Content.ToString() ?? "").Where(char.IsDigit).ToArray());
                    if (int.TryParse(fpsDigits, out int parsedFpsContent) && parsedFpsContent > 0)
                    {
                        targetFps = parsedFpsContent;
                    }
                }
            }
            catch { }
            return Math.Clamp(targetFps, 1, 120);
        }

        private void CmbFPS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isBroadcasting || _captureTimer == null) return;
            try
            {
                int targetFps = GetSelectedTargetFps();
                int captureMs = Math.Max(8, (int)Math.Round(1000.0 / targetFps));
                _captureTimer.Change(0, captureMs);
                var resolution = (cmbResolution?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1920×1080";
                var fpsText = (cmbFPS?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? $"{targetFps} fps";
                if (txtRes != null) txtRes.Text = $"{resolution}  {fpsText}";
                Log.Information("[BroadcastPage] Live FPS updated to {Fps} fps ({Ms}ms interval)", targetFps, captureMs);
            }
            catch (Exception ex)
            {
                Log.Debug("[BroadcastPage] CmbFPS_SelectionChanged error: {Err}", ex.Message);
            }
        }

        // Cached UI state to avoid blocking UI thread with synchronous Dispatcher.Invoke on every frame (30fps)
        private volatile int _cachedScreenW = 1920;
        private volatile int _cachedScreenH = 1080;
        private double _cachedScaleX = 1.0;
        private double _cachedScaleY = 1.0;
        private volatile bool _cachedIsWindowSource = false;
        private IntPtr _cachedWindowHwnd = IntPtr.Zero;
        private IntPtr _cachedSmartTouchHwnd = IntPtr.Zero;
        private volatile bool _cachedSmartTouchReadyForPrint = false;
        private volatile BitmapSource? _lastIsolatedSourceBitmap = null;
        private volatile bool _sharedWindowClosedStopTriggered = false;
        private ScreenSourceInfo? _pendingSwitchCandidate = null;
        private string _pendingSwitchLabel = "";
        private volatile int _cachedTargetWidth = 1280;
        private volatile int _cachedQualityLevel = 70;
        private volatile bool _cachedIsForceWatch = false;
        private volatile int _cachedWebPort = 8080;
        private volatile string _cachedLessonTitle = "";
        private System.Collections.Generic.List<string>? _cachedTargetCodes = null;

        private void RefreshCachedBroadcastParams()
        {
            try
            {
                _cachedScreenW = (int)SystemParameters.PrimaryScreenWidth;
                _cachedScreenH = (int)SystemParameters.PrimaryScreenHeight;
                var dpiScale = VisualTreeHelper.GetDpi(this);
                _cachedScaleX = dpiScale.DpiScaleX;
                _cachedScaleY = dpiScale.DpiScaleY;

                _cachedIsWindowSource = rbWindow.IsChecked == true;
                if (_cachedIsWindowSource && cmbWindowList.SelectedItem is ComboBoxItem item && item.Tag is int pId)
                {
                    try
                    {
                        var proc = System.Diagnostics.Process.GetProcessById(pId);
                        _cachedWindowHwnd = proc.MainWindowHandle;
                    }
                    catch { _cachedWindowHwnd = IntPtr.Zero; }
                }

                if (cmbResolution.SelectedItem is ComboBoxItem resItem)
                {
                    string resText = resItem.Content?.ToString() ?? "";
                    if (resText.Contains("1920")) _cachedTargetWidth = 1920;
                    else if (resText.Contains("2560")) _cachedTargetWidth = 2560;
                    else _cachedTargetWidth = 1280;
                }

                int qLevel = 72;
                if (cmbQuality.SelectedItem is ComboBoxItem qItem && int.TryParse(qItem.Tag?.ToString(), out int qVal))
                {
                    qLevel = qVal;
                }

                if (ClassroomAppContext.Network?.IsBroadcasting == true)
                {
                    try
                    {
                        var clients = ClassroomAppContext.Network.GetConnectedStudents();
                        if (clients != null && clients.Count > 0)
                        {
                            int maxLatency = 0;
                            foreach (var c in clients)
                            {
                                if (c.LatencyMs > maxLatency) maxLatency = c.LatencyMs;
                            }
                            if (maxLatency > 300)
                            {
                                qLevel = Math.Max(55, qLevel - 15);
                            }
                        }
                    }
                    catch { }
                }
                _cachedQualityLevel = qLevel;
                _cachedIsForceWatch = chkAllowStudentExit?.IsChecked != true;
                _cachedShowTeacherCursor = chkShowTeacherCursor?.IsChecked != false;
                if (ClassroomAppContext.Network?.WebBridge != null)
                {
                    _cachedWebPort = ClassroomAppContext.Network.WebBridge.WebPort;
                }
                _cachedLessonTitle = txtLessonTitle != null ? txtLessonTitle.Text?.Trim() ?? "" : "";
                _cachedTargetCodes = GetTargetStudentCodes();

                var stWin = Application.Current?.Windows.OfType<QASmartTouch.Forms.Form2_MainDashboard>().FirstOrDefault();
                _cachedSmartTouchHwnd = stWin != null
                    ? new System.Windows.Interop.WindowInteropHelper(stWin).Handle
                    : IntPtr.Zero;
                var appInstance = Application.Current as QASmartTouch.App;
                var modeSvc = appInstance?.ModeService;
                _cachedSmartTouchReadyForPrint = stWin != null
                    && stWin.IsVisible
                    && stWin.WindowState != WindowState.Minimized
                    && stWin.Opacity >= 0.99
                    && (modeSvc == null || (!modeSvc.IsTransitioning && modeSvc.CurrentMode == QASmartClass.Shared.AppMode.SmartScreen));
            }
            catch (Exception ex)
            {
                Log.Debug("[BroadcastPage] RefreshCachedBroadcastParams error: {Err}", ex.Message);
            }
        }

        /// <summary>Tính mã băm lấy mẫu mật độ cao (48x64 = 3072 điểm ảnh) để phát hiện ngay cả nét bút mảnh nhất trên bảng viết.</summary>
        private static uint ComputeFastSampleHash(BitmapSource? bmp)
        {
            if (bmp == null) return 0;
            try
            {
                int w = bmp.PixelWidth;
                int h = bmp.PixelHeight;
                if (w < 16 || h < 16) return 0;
                int bpp = (bmp.Format.BitsPerPixel + 7) / 8;
                if (bpp < 3) return 0;
                uint hash = 2166136261u;
                int stride = w * bpp;
                byte[] rowBuf = new byte[stride];
                const int rows = 48;
                const int cols = 64;
                for (int ry = 1; ry <= rows; ry++)
                {
                    int y = (h * ry) / (rows + 1);
                    bmp.CopyPixels(new Int32Rect(0, y, w, 1), rowBuf, stride, 0);
                    for (int rx = 1; rx <= cols; rx++)
                    {
                        int idx = ((w * rx) / (cols + 1)) * bpp;
                        if (idx + 2 < rowBuf.Length)
                        {
                            hash = (hash ^ rowBuf[idx]) * 16777619u;
                            hash = (hash ^ rowBuf[idx + 1]) * 16777619u;
                            hash = (hash ^ rowBuf[idx + 2]) * 16777619u;
                        }
                    }
                }
                return hash;
            }
            catch { return 0; }
        }

        // V22-02: Async + SemaphoreSlim guard chống overlap, KHÔNG khóa UI thread mỗi frame
        private async Task CaptureAndSendAsync()
        {
            if (!_isBroadcasting || _isPaused || _isBroadcastPausedDueToPrivacy) return;
            if (!await _captureLock.WaitAsync(15)) return;
            try
            {
                int screenW = _cachedScreenW;
                int screenH = _cachedScreenH;
                double scaleX = _cachedScaleX;
                double scaleY = _cachedScaleY;
                bool isWindowSource = _cachedIsWindowSource;
                IntPtr targetHWnd = _cachedWindowHwnd;
                ScreenSourceInfo? pickedSrc;
                lock (_srcLock) pickedSrc = _picked;
                BitmapSource? qaPageBitmap = null;
                string resolvedQaPageTitle = "";
                int targetWidth = _cachedTargetWidth;
                int qualityLevel = _cachedQualityLevel;
                bool isNetworkActive = ClassroomAppContext.Network?.IsBroadcasting == true;
                bool isForceWatch = _cachedIsForceWatch;
                int webPort = _cachedWebPort;
                string lessonTitle = _cachedLessonTitle;
                var targetCodes = _cachedTargetCodes;

                // ═══ CƠ CHẾ KHÓA CỐ ĐỊNH NGUỒN CHIẾU CHUẨN GOOGLE MEET (ISOLATED WINDOW / TAB / BOARD LOCK) ═══
                // - Khi Giáo viên chọn Trang QA SmartClass / Bảng SMART TOUCH hoặc Cửa sổ ứng dụng:
                //   tuyệt đối KHÔNG dùng BitBlt/StretchBlt từ màn hình nền (GetDC(0)) để tránh rò rỉ cửa sổ đè lên hoặc màn hình mới khi chuyển tab/mode.
                // - Khi nguồn cố định bị ẩn (Window.Hide khi chuyển mode) hoặc thu nhỏ (Minimize), giữ nguyên khung hình hợp lệ cuối cùng (_lastIsolatedSourceBitmap).
                bool captureSmartTouchIsolated = false;
                IntPtr smartTouchHwnd = _cachedSmartTouchHwnd;
                var appRef = Application.Current as QASmartTouch.App;
                var modeService = appRef?.ModeService;

                if (pickedSrc != null && pickedSrc.Kind == ScreenSourceKind.QaClassPage)
                {
                    if (string.Equals(pickedSrc.PageFormId, "SMART_TOUCH", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrEmpty(lessonTitle)) lessonTitle = "Bảng trắng tương tác SMART TOUCH";

                        if (smartTouchHwnd == IntPtr.Zero)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                var stWin = Application.Current?.Windows.OfType<QASmartTouch.Forms.Form2_MainDashboard>().FirstOrDefault();
                                if (stWin != null)
                                {
                                    _cachedSmartTouchHwnd = new System.Windows.Interop.WindowInteropHelper(stWin).Handle;
                                    smartTouchHwnd = _cachedSmartTouchHwnd;
                                }
                            }, DispatcherPriority.Render);
                        }

                        bool isSmartTouchActiveMode = modeService == null
                            || (!modeService.IsTransitioning && modeService.CurrentMode == QASmartClass.Shared.AppMode.SmartScreen);

                        if (isSmartTouchActiveMode
                            && smartTouchHwnd != IntPtr.Zero
                            && NativeMethods.IsWindow(smartTouchHwnd)
                            && NativeMethods.IsWindowVisible(smartTouchHwnd)
                            && !NativeMethods.IsIconic(smartTouchHwnd))
                        {
                            // Chụp độc lập cửa sổ SMART TOUCH bằng PrintWindow(PW_RENDERFULLCONTENT) trên thread pool (không dính cửa sổ đè phía trên)
                            captureSmartTouchIsolated = true;
                        }
                        else if (_lastIsolatedSourceBitmap != null)
                        {
                            // Giáo viên đã chuyển sang SMART CLASS hoặc DESKTOP -> giữ cố định 100% bảng SMART TOUCH trên máy Học sinh!
                            qaPageBitmap = _lastIsolatedSourceBitmap;
                        }
                        else
                        {
                            // Khung hình đầu tiên khi Giáo viên bấm phát SMART TOUCH từ bên trong SMART CLASS
                            Dispatcher.Invoke(() =>
                            {
                                var shell = (Window.GetWindow(this) as ClassroomShell) ?? (Application.Current?.MainWindow as ClassroomShell);
                                if (shell != null)
                                {
                                    qaPageBitmap = shell.CaptureQaClassPageBitmap("SMART_TOUCH", targetWidth, out resolvedQaPageTitle);
                                    if (qaPageBitmap != null) _lastIsolatedSourceBitmap = qaPageBitmap;
                                }
                            }, DispatcherPriority.Render);
                        }
                    }
                    else
                    {
                        bool isModeTransitioning = modeService != null && modeService.IsTransitioning;
                        if (isModeTransitioning && _lastIsolatedSourceBitmap != null)
                        {
                            qaPageBitmap = _lastIsolatedSourceBitmap;
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                var shell = (Window.GetWindow(this) as ClassroomShell) ?? (Application.Current?.MainWindow as ClassroomShell);
                                if (shell != null)
                                {
                                    // Nếu cửa sổ ClassroomShell đang ẩn (GV chuyển sang DESKTOP / SMART TOUCH) và đã có ảnh trang khóa cố định,
                                    // giữ nguyên khung hình của trang đó để tránh WPF trả về khung trắng khi cửa sổ bị Hide().
                                    if ((!shell.IsVisible || shell.WindowState == WindowState.Minimized || shell.Opacity < 0.99)
                                        && _lastIsolatedSourceBitmap != null)
                                    {
                                        qaPageBitmap = _lastIsolatedSourceBitmap;
                                    }
                                    else
                                    {
                                        var rendered = shell.CaptureQaClassPageBitmap(pickedSrc.PageFormId, targetWidth, out resolvedQaPageTitle);
                                        if (rendered != null && !IsMostlyBlack(rendered))
                                        {
                                            qaPageBitmap = rendered;
                                            _lastIsolatedSourceBitmap = rendered;
                                        }
                                        else
                                        {
                                            qaPageBitmap = _lastIsolatedSourceBitmap ?? rendered;
                                        }
                                    }
                                }
                            }, DispatcherPriority.Render);
                        }

                        if (string.IsNullOrEmpty(lessonTitle) && !string.IsNullOrEmpty(resolvedQaPageTitle))
                        {
                            lessonTitle = resolvedQaPageTitle;
                        }
                    }
                }

                // Chạy logic chụp & lưu file trên thread pool để tránh block UI thread
                await System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        int x = 0, y = 0;
                        int pixelW = (int)(screenW * scaleX);
                        int pixelH = (int)(screenH * scaleY);
                        bool captureDesktopRegion = false;
                        IntPtr isolatedHwnd = IntPtr.Zero;

                        if (pickedSrc != null)
                        {
                            if (pickedSrc.Kind == ScreenSourceKind.Screen)
                            {
                                var scr = ScreenSourceEnumerator.FindScreen(pickedSrc.X, pickedSrc.Y, pickedSrc.Width, pickedSrc.Height);
                                if (scr != null) { x = scr.X; y = scr.Y; pixelW = scr.Width; pixelH = scr.Height; }
                                isWindowSource = false;
                                captureDesktopRegion = true;
                            }
                            else if (pickedSrc.Kind == ScreenSourceKind.Region)
                            {
                                x = pickedSrc.X;
                                y = pickedSrc.Y;
                                pixelW = Math.Max(64, pickedSrc.Width);
                                pixelH = Math.Max(64, pickedSrc.Height);
                                isWindowSource = false;
                                captureDesktopRegion = true;
                            }
                            else if (pickedSrc.Kind == ScreenSourceKind.QaClassPage)
                            {
                                isWindowSource = false;
                                if (captureSmartTouchIsolated && smartTouchHwnd != IntPtr.Zero)
                                {
                                    isolatedHwnd = smartTouchHwnd;
                                }
                            }
                            else if (pickedSrc.Kind == ScreenSourceKind.Window)
                            {
                                if (!ScreenSourceEnumerator.IsWindowHandleValid(pickedSrc.Hwnd))
                                {
                                    // [MYTHWARE_LITE] Chuẩn Google Meet: Khi cửa sổ đang chiếu bị đóng hẳn (!IsWindow),
                                    // tự động dừng chia sẻ an toàn thay vì lặng lẽ chuyển về Toàn màn hình làm lộ màn hình riêng của Giáo viên!
                                    // Dispatcher.BeginInvoke(new Action(ResetPickedToPrimary));
                                    if (!_sharedWindowClosedStopTriggered)
                                    {
                                        _sharedWindowClosedStopTriggered = true;
                                        Dispatcher.BeginInvoke(new Action(OnSharedWindowClosedStopBroadcast));
                                    }
                                    return;
                                }

                                isWindowSource = true;
                                targetHWnd = pickedSrc.Hwnd;
                                isolatedHwnd = pickedSrc.Hwnd;
                            }
                        }
                        else if (isWindowSource)
                        {
                            if (targetHWnd == IntPtr.Zero || !ScreenSourceEnumerator.IsWindowHandleValid(targetHWnd))
                            {
                                if (!_sharedWindowClosedStopTriggered)
                                {
                                    _sharedWindowClosedStopTriggered = true;
                                    Dispatcher.BeginInvoke(new Action(OnSharedWindowClosedStopBroadcast));
                                }
                                return;
                            }
                            isolatedHwnd = targetHWnd;
                        }
                        else
                        {
                            captureDesktopRegion = true;
                        }

                        BitmapSource? bmpSource = qaPageBitmap;

                        // Nhánh 1: Chụp Cửa sổ độc lập / Bảng SMART TOUCH độc lập chuẩn Google Meet (PrintWindow + HALFTONE, tuyệt đối không lấy GetDC(0))
                        if (bmpSource == null && isolatedHwnd != IntPtr.Zero)
                        {
                            var winBmp = TryCaptureIsolatedWindowBitmap(isolatedHwnd, targetWidth, _cachedShowTeacherCursor);
                            if (winBmp != null)
                            {
                                _lastIsolatedSourceBitmap = winBmp;
                                bmpSource = winBmp;
                            }
                            else
                            {
                                // Cửa sổ đang thu nhỏ (Minimize), tạm ẩn (Window.Hide khi đổi mode) hoặc đang chuyển tiếp:
                                // giữ nguyên khung hình cuối cùng của đúng cửa sổ đó!
                                bmpSource = _lastIsolatedSourceBitmap;
                                if (bmpSource == null && ScreenSourceEnumerator.GetWindowProcessId(isolatedHwnd) == (uint)Environment.ProcessId)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        foreach (Window w in Application.Current.Windows)
                                        {
                                            if (new System.Windows.Interop.WindowInteropHelper(w).Handle == isolatedHwnd)
                                            {
                                                bmpSource = ScreenSourceEnumerator.RenderElementToBitmap(w.Content as FrameworkElement ?? w, targetWidth);
                                                if (bmpSource != null) _lastIsolatedSourceBitmap = bmpSource;
                                                break;
                                            }
                                        }
                                    }, DispatcherPriority.Render);
                                }
                                if (bmpSource == null) return;
                            }
                        }

                        // Nhánh 2: Chỉ chụp điểm ảnh màn hình vật lý khi Giáo viên chủ động chọn "Toàn màn hình" hoặc "Chọn vùng"
                        if (bmpSource == null && captureDesktopRegion)
                        {
                            if (pixelW <= 0 || pixelH <= 0) return;

                            IntPtr hdcSrc = IntPtr.Zero;
                            IntPtr hdcDest = IntPtr.Zero;
                            IntPtr hBitmap = IntPtr.Zero;
                            IntPtr hOld = IntPtr.Zero;
                            try
                            {
                                double scaleRatio = pixelW > targetWidth ? (double)targetWidth / pixelW : 1.0;
                                int outW = Math.Max(64, (int)Math.Round(pixelW * scaleRatio));
                                int outH = Math.Max(64, (int)Math.Round(pixelH * scaleRatio));

                                hdcSrc = NativeMethods.GetDC(IntPtr.Zero);
                                hdcDest = NativeMethods.CreateCompatibleDC(hdcSrc);
                                hBitmap = NativeMethods.CreateCompatibleBitmap(hdcSrc, outW, outH);
                                hOld = NativeMethods.SelectObject(hdcDest, hBitmap);
                                // [MEET/MYTHWARE CRISP SCALING] Dùng HALFTONE (4) + SetBrushOrgEx thay vì COLORONCOLOR (3) để giữ nguyên chống răng cưa ClearType cho chữ nhỏ & công thức
                                NativeMethods.SetStretchBltMode(hdcDest, 4); // HALFTONE = 4
                                NativeMethods.SetBrushOrgEx(hdcDest, 0, 0, IntPtr.Zero);

                                if (outW == pixelW && outH == pixelH)
                                    NativeMethods.BitBlt(hdcDest, 0, 0, outW, outH, hdcSrc, x, y, 0x00CC0020); // SRCCOPY
                                else
                                    NativeMethods.StretchBlt(hdcDest, 0, 0, outW, outH, hdcSrc, x, y, pixelW, pixelH, 0x00CC0020);

                                if (_cachedShowTeacherCursor)
                                {
                                    DrawCursorOverlayOnHdc(hdcDest, x, y, pixelW, pixelH, outW, outH);
                                }

                                var rawBmp = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                    hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                                bmpSource = new FormatConvertedBitmap(rawBmp, PixelFormats.Bgr32, null, 0);
                                bmpSource.Freeze();
                            }
                            finally
                            {
                                if (hOld != IntPtr.Zero) NativeMethods.SelectObject(hdcDest, hOld);
                                if (hBitmap != IntPtr.Zero) NativeMethods.DeleteObject(hBitmap);
                                if (hdcDest != IntPtr.Zero) NativeMethods.DeleteDC(hdcDest);
                                if (hdcSrc != IntPtr.Zero) NativeMethods.ReleaseDC(IntPtr.Zero, hdcSrc);
                            }
                        }

                        if (bmpSource == null) return;

                        // Ghép ô Camera Giáo viên (Google Meet 16:9 / SmartTouch Circle) lên luồng hình ảnh phát sóng nếu Camera đang bật
                        bool camActive = LiveBroadcastMediaService.Instance.IsCameraEnabled;
                        long currentCamSeq = LiveBroadcastMediaService.Instance.LatestCameraFrameSeq;
                        bool hasCameraChange = (camActive && currentCamSeq != _lastCompositedCamSeq)
                                            || (!camActive && _lastCompositedCamSeq != -1);
                        if (camActive && bmpSource != null)
                        {
                            bmpSource = LiveBroadcastMediaService.Instance.CompositeCameraTileIfActive(bmpSource);
                        }
                        _lastCompositedCamSeq = camActive ? currentCamSeq : -1;

                        // Double-buffer: alternating file names to avoid lock
                        int idx = System.Threading.Interlocked.Increment(ref _captureIndex) % 2;
                        string filePath = System.IO.Path.Combine(_captureDir, $"broadcast_{idx}.jpg");

                        // ═══ ADAPTIVE 3-GEAR ENGINE (CHUẨN GOOGLE MEET & MYTHWARE) ═══
                        // Số 1 (Motion Gear): Khi màn hình hoặc con trỏ đang chuyển động -> nén nhanh (Q=78..82) ở 30/60 FPS để nét vẽ nối liền 100%, độ trễ < 50ms.
                        // Số 2 (Ultra-Sharp Settlement): Ngay khi dừng tay >= 220ms -> chốt 1 khung Siêu Nét (Q=94) để chữ nhỏ, code, công thức Toán nét căng như gốc.
                        // Số 3 (Idle Zero-Skip): Khi màn hình tiếp tục đứng yên -> nghỉ gửi khung trùng lặp (0% CPU thừa, tiết kiệm >90% băng thông), chỉ nhịp Keep-Alive mỗi 1.2s.
                        var nowUtc = DateTime.UtcNow;
                        uint currentSampleHash = ComputeFastSampleHash(bmpSource);
                        bool hasVisualChange = _isFirstCapture || hasCameraChange || currentSampleHash == 0 || currentSampleHash != _lastFrameSampleHash;
                        bool isUltraSharpKeyframe = false;
                        byte[]? imgBytes = null;

                        if (hasVisualChange)
                        {
                            _lastFrameSampleHash = currentSampleHash;
                            _lastMotionDetectedUtc = nowUtc;
                            _settlementKeyframeSent = false;

                            int motionQuality = qualityLevel;
                            if (string.Equals(_activeQualityPreset, "ADAPTIVE", StringComparison.OrdinalIgnoreCase))
                            {
                                motionQuality = Math.Clamp(qualityLevel, 75, 82);
                            }
                            else if (string.Equals(_activeQualityPreset, "CRISP_TEXT", StringComparison.OrdinalIgnoreCase))
                            {
                                motionQuality = Math.Max(qualityLevel, 92);
                                isUltraSharpKeyframe = true;
                            }

                            var encoder = new JpegBitmapEncoder { QualityLevel = motionQuality };
                            encoder.Frames.Add(BitmapFrame.Create(bmpSource));
                            using (var ms = new System.IO.MemoryStream(98304))
                            {
                                encoder.Save(ms);
                                imgBytes = ms.ToArray();
                            }
                            _lastEncodedFrameBytes = imgBytes;
                            _lastEncodedFrameUtc = nowUtc;
                            _lastKeepAlivePushUtc = nowUtc;
                        }
                        else
                        {
                            double idleMs = (nowUtc - _lastMotionDetectedUtc).TotalMilliseconds;
                            if (!_settlementKeyframeSent && idleMs >= 220)
                            {
                                // SỐ 2: CHỐT KHUNG SIÊU NÉT ULTRA-SHARP (Q=94) NGAY KHI DỪNG THAO TÁC
                                _settlementKeyframeSent = true;
                                isUltraSharpKeyframe = true;
                                int settlementQuality = Math.Max(qualityLevel, 94);
                                var sharpEncoder = new JpegBitmapEncoder { QualityLevel = settlementQuality };
                                sharpEncoder.Frames.Add(BitmapFrame.Create(bmpSource));
                                using (var ms = new System.IO.MemoryStream(163840))
                                {
                                    sharpEncoder.Save(ms);
                                    imgBytes = ms.ToArray();
                                }
                                _lastSettlementBytes = imgBytes;
                                _lastEncodedFrameBytes = imgBytes;
                                _lastEncodedFrameUtc = nowUtc;
                                _lastKeepAlivePushUtc = nowUtc;
                            }
                            else if (_settlementKeyframeSent && (nowUtc - _lastKeepAlivePushUtc).TotalMilliseconds >= 1200)
                            {
                                // SỐ 3 (Keep-Alive mỗi 1.2s): Gửi lại khung Siêu Nét có sẵn trong RAM cho máy HS mới vào lớp hoặc vừa hồi phục Wi-Fi
                                _lastKeepAlivePushUtc = nowUtc;
                                imgBytes = _lastSettlementBytes ?? _lastEncodedFrameBytes;
                                isUltraSharpKeyframe = true;
                                if (imgBytes != null && isNetworkActive)
                                {
                                    _ = QASmartClass.Services.UdpScreenBroadcastService.Instance.SendFrameAsync(imgBytes, isKeyframe: true);
                                }
                            }
                            else
                            {
                                // SỐ 3 (Idle Zero-Skip): Màn hình đứng yên -> không mã hóa thừa & không bắn lặp qua mạng!
                                return;
                            }
                        }

                        if (imgBytes == null || imgBytes.Length == 0) return;

                        // Chỉ đẩy thêm bản ghi mới vào RingBuffer khi thực sự có khung chuyển động mới hoặc khung chốt nét Ultra-Sharp
                        if (hasVisualChange || (isUltraSharpKeyframe && _lastEncodedFrameUtc == nowUtc))
                        {
                            QASmartClass.Services.BroadcastStateService.Instance.PushBroadcastFrame(imgBytes, isUltraSharpKeyframe);

                            // Chỉ ghi đĩa nếu chạy ở chế độ cục bộ (không qua mạng LAN)
                            if (!isNetworkActive)
                            {
                                System.IO.File.WriteAllBytes(filePath, imgBytes);
                            }
                            else
                            {
                                _ = QASmartClass.Services.UdpScreenBroadcastService.Instance.SendFrameAsync(imgBytes, isKeyframe: isUltraSharpKeyframe);
                                // Với khung chốt nét Ultra-Sharp (Q=94), gửi bồi thêm 1 lần sau 45ms nếu màn hình vẫn tĩnh để chống rớt gói UDP trên sóng Wi-Fi
                                if (isUltraSharpKeyframe && !hasVisualChange)
                                {
                                    byte[] sharpCopy = imgBytes;
                                    uint hashSnapshot = _lastFrameSampleHash;
                                    _ = Task.Run(async () =>
                                    {
                                        await Task.Delay(45);
                                        if (_isBroadcasting && !_isPaused && _lastFrameSampleHash == hashSnapshot)
                                        {
                                            await QASmartClass.Services.UdpScreenBroadcastService.Instance.SendFrameAsync(sharpCopy, isKeyframe: true);
                                        }
                                    });
                                }
                            }
                        }

                        // Gửi command
                        string cmdType = _isFirstCapture ? "SCREEN_BROADCAST_START" : "SCREEN_BROADCAST_UPDATE";
                        string token = QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken;

                        // Đồng bộ giao thức và cổng kết nối động bảo mật (V2.2.2)
                        string protocol = "UDP_MULTICAST";
                        if (_isTextMode)
                        {
                            protocol = "HTTP_PULL";
                        }
                        int multicastPort = QASmartClass.Services.UdpScreenBroadcastService.MulticastPort;
                        int ttl = QASmartClass.Services.UdpScreenBroadcastService.MulticastTTL;
                        string key = QASmartClass.Services.UdpScreenBroadcastService.EncryptionKey;

                        string cmd = $"CMD|{cmdType}|{filePath}|PROTOCOL={protocol}|PORT={multicastPort}|KEY={key}|TTL={ttl}|AUDIO={(LiveBroadcastMediaService.Instance.IsAnyAudioActive ? "1" : "0")}|{(isForceWatch ? "FORCE_WATCH|" : "")}{webPort}|{token}";
                        if (!string.IsNullOrEmpty(lessonTitle))
                        {
                            cmd += $"|TITLE={System.Uri.EscapeDataString(lessonTitle)}";
                        }

                        // Cập nhật state
                        ClassroomAppContext.BroadcastState.ScreenCapturePath = filePath;
                        ClassroomAppContext.BroadcastState.ScreenCaptureTime = DateTime.Now;

                        // Chống bão TCP khi chạy 40-50 máy:
                        // Luồng ảnh thực tế đã chạy qua /api/broadcast_stream (Android) và UdpScreenBroadcastService (PC Lab).
                        // Do đó lệnh TCP SCREEN_BROADCAST_UPDATE chỉ gửi ngay ở frame đầu tiên, hoặc giãn nhịp mỗi 350ms (~3 lần/giây) cho Web/Fallback.
                        double msSinceLastTcpCmd = (nowUtc - _lastTcpBroadcastCmdUtc).TotalMilliseconds;
                        int tcpCmdMinIntervalMs = _isTextMode ? 250 : 400;
                        bool shouldSendTcpCmd = _isFirstCapture || msSinceLastTcpCmd >= tcpCmdMinIntervalMs;

                        if (shouldSendTcpCmd)
                        {
                            _lastTcpBroadcastCmdUtc = nowUtc;
                            if (ClassroomAppContext.Network?.IsBroadcasting == true)
                            {
                                if (targetCodes == null)
                                {
                                    Log.Debug("[BroadcastPage] Gửi TCP command tới tất cả học sinh: {Cmd}", cmd);
                                    _ = ClassroomAppContext.Network.SendCommandAsync(cmd);
                                }
                                else
                                {
                                    Log.Debug("[BroadcastPage] Gửi TCP command tới các học sinh {Targets}: {Cmd}", string.Join(",", targetCodes), cmd);
                                    _ = ClassroomAppContext.Network.SendToStudentsAsync(targetCodes, cmd);
                                }
                            }
                            else if (_isFirstCapture)
                            {
                                Log.Warning("[Broadcast-GV] NetworkService.IsBroadcasting=false! Frame TCP command NOT sent to students via network.");
                            }
                        }

                        // Cập nhật Live Preview và Local Bus bất đồng bộ (không khóa luồng chụp màn hình)
                        bool shouldUpdatePreview = (nowUtc - _lastLivePreview).TotalMilliseconds >= 80;
                        if (shouldUpdatePreview || shouldSendTcpCmd)
                        {
                            if (shouldUpdatePreview) _lastLivePreview = nowUtc;
                            _ = Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
                            {
                                if (shouldUpdatePreview)
                                {
                                    // [MYTHWARE_LITE] Luôn hiển thị khung hình thực tế mà máy Học sinh đang nhận được
                                    // (Cả trong trang Trình chiếu lẫn trên Thanh điều khiển nổi PiP)
                                    imgLivePreview.Source = bmpSource ?? GetBroadcastPlaceholder();
                                    string srcDesc = pickedSrc != null
                                        ? DescribeSource(pickedSrc)
                                        : (isWindowSource ? "Cửa sổ ứng dụng" : "Toàn màn hình");
                                    string lbl = "Đang chiếu: " + srcDesc;
                                    if (txtPreviewLabel.Text != lbl) txtPreviewLabel.Text = lbl;
                                    _floatingBroadcastBar?.UpdateLiveFrame(bmpSource, srcDesc, txtRes.Text);
                                    (Application.Current as QASmartTouch.App)?._floatingModeBar?.UpdateLiveFrame(bmpSource, srcDesc, txtRes.Text);
                                }
                                if (shouldSendTcpCmd)
                                {
                                    ClassroomAppContext.DispatchCommand(cmd);
                                }
                            }));
                        }

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

        /// <summary>
        /// Vẽ con trỏ chuột thực tế của Giáo viên kèm vòng tròn sáng vàng dẫn hướng chuẩn Mythware trực tiếp lên khung hình GDI.
        /// Đảm bảo giải phóng 100% GDI handles (hPen, hbmMask, hbmColor) trong khối finally để không rò rỉ tài nguyên khi chạy hàng giờ.
        /// </summary>
        private static void DrawCursorOverlayOnHdc(IntPtr hdcDest, int srcX, int srcY, int srcW, int srcH, int destW, int destH)
        {
            if (hdcDest == IntPtr.Zero || srcW <= 0 || srcH <= 0 || destW <= 0 || destH <= 0) return;

            IntPtr hPenOuter = IntPtr.Zero;
            IntPtr hPenInner = IntPtr.Zero;
            IntPtr hOldPen = IntPtr.Zero;
            IntPtr hOldBrush = IntPtr.Zero;
            NativeMethods.ICONINFO iconInfo = default;

            try
            {
                var pci = new NativeMethods.CURSORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.CURSORINFO>() };
                if (!NativeMethods.GetCursorInfo(ref pci) || (pci.flags & 0x00000001) == 0 || pci.hCursor == IntPtr.Zero)
                {
                    return;
                }

                int relX = pci.ptScreenPos.X - srcX;
                int relY = pci.ptScreenPos.Y - srcY;
                if (relX < 0 || relY < 0 || relX >= srcW || relY >= srcH)
                {
                    return;
                }

                int destCurX = (int)Math.Round((double)relX * destW / srcW);
                int destCurY = (int)Math.Round((double)relY * destH / srcH);

                // 1. Vẽ vòng sáng dẫn hướng chuẩn Mythware (viền kép tương phản cao, rỗng tâm để KHÔNG che khuất chữ bên dưới mũi tên)
                int radius = Math.Clamp((int)Math.Round(16.0 * destW / 1920.0), 11, 22);
                IntPtr nullBrush = NativeMethods.GetStockObject(5); // NULL_BRUSH = 5
                hPenOuter = NativeMethods.CreatePen(0, 4, 0x000099FF); // Cam hổ phách đậm (BGR: #FF9900)
                hPenInner = NativeMethods.CreatePen(0, 2, 0x0033EBFF); // Vàng sáng nổi bật (BGR: #FFEB33)

                if (nullBrush != IntPtr.Zero && hPenOuter != IntPtr.Zero && hPenInner != IntPtr.Zero)
                {
                    hOldBrush = NativeMethods.SelectObject(hdcDest, nullBrush);
                    hOldPen = NativeMethods.SelectObject(hdcDest, hPenOuter);
                    NativeMethods.Ellipse(hdcDest, destCurX - radius, destCurY - radius, destCurX + radius, destCurY + radius);
                    NativeMethods.SelectObject(hdcDest, hPenInner);
                    NativeMethods.Ellipse(hdcDest, destCurX - radius + 1, destCurY - radius + 1, destCurX + radius - 1, destCurY + radius - 1);
                }

                // 2. Vẽ chính xác hình dạng con trỏ hệ thống (mũi tên / bàn tay / I-beam soạn thảo chữ) trừ đúng tọa độ Hotspot
                int hotX = 0, hotY = 0;
                if (NativeMethods.GetIconInfo(pci.hCursor, out iconInfo))
                {
                    hotX = (int)Math.Round((double)iconInfo.xHotspot * destW / srcW);
                    hotY = (int)Math.Round((double)iconInfo.yHotspot * destH / srcH);
                }

                int iconSize = Math.Clamp((int)Math.Round(32.0 * destW / Math.Max(srcW, 1280)), 20, 40);
                NativeMethods.DrawIconEx(hdcDest, destCurX - hotX, destCurY - hotY, pci.hCursor, iconSize, iconSize, 0, IntPtr.Zero, 0x0003); // DI_NORMAL = 3
            }
            catch
            {
                // Bỏ qua an toàn nếu con trỏ hệ thống đang chuyển đổi trạng thái UAC/Secure Desktop
            }
            finally
            {
                if (hOldPen != IntPtr.Zero) NativeMethods.SelectObject(hdcDest, hOldPen);
                if (hOldBrush != IntPtr.Zero) NativeMethods.SelectObject(hdcDest, hOldBrush);
                if (hPenOuter != IntPtr.Zero) NativeMethods.DeleteObject(hPenOuter);
                if (hPenInner != IntPtr.Zero) NativeMethods.DeleteObject(hPenInner);
                if (iconInfo.hbmMask != IntPtr.Zero) NativeMethods.DeleteObject(iconInfo.hbmMask);
                if (iconInfo.hbmColor != IntPtr.Zero) NativeMethods.DeleteObject(iconInfo.hbmColor);
            }
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
            public static extern bool StretchBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSrc, int xSrc, int ySrc, int wSrc, int hSrc, int rop);
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            public static extern int SetStretchBltMode(IntPtr hdc, int iStretchMode);
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            public static extern bool SetBrushOrgEx(IntPtr hdc, int nXOrg, int nYOrg, IntPtr lppt);
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            public static extern IntPtr CreatePen(int fnPenStyle, int nWidth, uint crColor);
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            public static extern IntPtr GetStockObject(int fnObject);
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            public static extern bool Ellipse(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            public static extern bool DeleteObject(IntPtr hObject);
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            public static extern bool DeleteDC(IntPtr hdc);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool GetCursorInfo(ref CURSORINFO pci);
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyWidth, uint istepIfAniCur, IntPtr hbrFlickerFreeDraw, uint diFlags);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool IsIconic(IntPtr hWnd);
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool IsWindow(IntPtr hWnd);
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool IsWindowVisible(IntPtr hWnd);
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
            
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            public struct POINT
            {
                public int X;
                public int Y;
            }

            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            public struct CURSORINFO
            {
                public int cbSize;
                public int flags;
                public IntPtr hCursor;
                public POINT ptScreenPos;
            }

            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            public struct ICONINFO
            {
                public bool fIcon;
                public int xHotspot;
                public int yHotspot;
                public IntPtr hbmMask;
                public IntPtr hbmColor;
            }

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

            ScreenSourceInfo? curSrc;
            lock (_srcLock) curSrc = _picked;
            string curDesc = curSrc != null ? DescribeSource(curSrc) : "Toàn màn hình";
            int recipientCnt = ClassroomAppContext.Network?.ConnectedCount ?? 0;
            _floatingBroadcastBar?.UpdateStatus(_isPaused || _isBroadcastPausedDueToPrivacy, txtTimer.Text, recipientCnt, curDesc);
            (Application.Current as QASmartTouch.App)?._floatingModeBar?.UpdateStatus(_isPaused || _isBroadcastPausedDueToPrivacy, txtTimer.Text, recipientCnt, curDesc);
        }

        // ═══════════════════════════════════════════════════════════
        //  SOURCE & TARGET PICKERS
        // ═══════════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════════
        //  CHỌN NGUỒN CHIẾU KIỂU GOOGLE MEET (màn hình / cửa sổ có ảnh xem trước)
        // ═══════════════════════════════════════════════════════════
        private readonly object _srcLock = new();
        private ScreenSourceInfo? _picked;
        private bool _suppressSourceReset;
        private const string DefaultSourceText = "Nguồn hiện tại: Toàn màn hình (màn hình chính)";

        private void PickSource_Click(object sender, RoutedEventArgs e)
        {
            OpenSourcePicker(ScreenSourceKind.Screen, out _);
        }

        private void PickSourceAndStart_Click(object sender, RoutedEventArgs e)
        {
            if (OpenSourcePicker(ScreenSourceKind.Screen, out bool autoMin) && !_isBroadcasting)
            {
                _skipConfirmForQuickBroadcast = true;
                try
                {
                    Broadcast_Click(this, new RoutedEventArgs());
                    if (_isBroadcasting && autoMin)
                    {
                        var shell = (Window.GetWindow(this) as ClassroomShell)
                            ?? Application.Current?.Windows.OfType<ClassroomShell>().FirstOrDefault();
                        if (shell != null && shell.IsVisible) shell.WindowState = WindowState.Minimized;
                    }
                }
                finally
                {
                    _skipConfirmForQuickBroadcast = false;
                }
            }
        }

        private void PickRegionAndStart_Click(object sender, RoutedEventArgs e)
        {
            PickRegion_Click(sender, e);
            ScreenSourceInfo? cur;
            lock (_srcLock) cur = _picked;
            if (cur != null && cur.Kind == ScreenSourceKind.Region && !_isBroadcasting)
            {
                _skipConfirmForQuickBroadcast = true;
                try
                {
                    Broadcast_Click(this, new RoutedEventArgs());
                }
                finally
                {
                    _skipConfirmForQuickBroadcast = false;
                }
            }
        }

        private void PickQaPageAndStart_Click(object sender, RoutedEventArgs e)
        {
            if (OpenSourcePicker(ScreenSourceKind.QaClassPage, out _) && !_isBroadcasting)
            {
                _skipConfirmForQuickBroadcast = true;
                try
                {
                    Broadcast_Click(this, new RoutedEventArgs());
                }
                finally
                {
                    _skipConfirmForQuickBroadcast = false;
                }
            }
        }

        private void ShowFloatingBar_Click(object sender, RoutedEventArgs e)
        {
            if (_isBroadcasting)
            {
                ScreenSourceInfo? cur;
                lock (_srcLock) cur = _picked;
                string srcDesc = cur != null ? DescribeSource(cur) : "Toàn màn hình";
                var resolution = (cmbResolution.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1920×1080";
                var fps = (cmbFPS.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "30 fps";
                EnsureFloatingBroadcastBarShown(srcDesc, resolution, fps);
                GetUnifiedEasyTouchOrb()?.ShowLivePipMonitor();
                _floatingBroadcastBar?.Activate();
            }
        }

        private void PickRegion_Click(object sender, RoutedEventArgs e)
        {
            if (_isBroadcasting && rbCamera.IsChecked == true)
            {
                txtPickedSource.Text = "Đang phát Camera — hãy dừng phát trước khi đổi sang vùng màn hình.";
                return;
            }

            ScreenSourceInfo? cur;
            lock (_srcLock) cur = _picked;
            Rect? initialRect = (cur != null && cur.Kind == ScreenSourceKind.Region && cur.DipWidth >= 60 && cur.DipHeight >= 60)
                ? new Rect(cur.DipLeft, cur.DipTop, cur.DipWidth, cur.DipHeight)
                : null;

            var selector = new ScreenRegionSelectorWindow(initialRect);
            if (selector.ShowDialog() == true && selector.SelectedRegion != null)
            {
                ApplyPickedSource(selector.SelectedRegion);
            }
        }

        private void PickQaPage_Click(object sender, RoutedEventArgs e)
        {
            OpenSourcePicker(ScreenSourceKind.QaClassPage, out _);
        }

        private void OpenSharedQaPage_Click(object sender, RoutedEventArgs e)
        {
            ScreenSourceInfo? cur;
            lock (_srcLock) cur = _picked;
            if (cur != null && cur.Kind == ScreenSourceKind.QaClassPage)
            {
                var shell = (Window.GetWindow(this) as ClassroomShell) ?? (Application.Current?.MainWindow as ClassroomShell);
                shell?.NavigateToQaPageForBroadcast(cur.PageFormId);
            }
        }

        private bool OpenSourcePicker(ScreenSourceKind initialTab, out bool autoMinimizeClassroomShell)
        {
            autoMinimizeClassroomShell = false;
            if (_isBroadcasting && rbCamera.IsChecked == true)
            {
                txtPickedSource.Text = "Đang phát Camera — hãy dừng phát trước khi đổi sang màn hình.";
                return false;
            }

            var owner = Window.GetWindow(this) ?? Application.Current?.Windows.OfType<ClassroomShell>().FirstOrDefault();
            IntPtr self = owner != null ? new System.Windows.Interop.WindowInteropHelper(owner).Handle : IntPtr.Zero;
            Window? visibleOwner = (owner != null && owner.IsVisible && owner.WindowState != WindowState.Minimized)
                ? owner
                : Application.Current?.Windows.OfType<Window>().FirstOrDefault(w =>
                    w.IsVisible
                    && w.WindowState != WindowState.Minimized
                    && w is not QASmartClass.Shared.FloatingModeBar
                    && w is not FloatingBroadcastBarWindow
                    && w is not BroadcastPrivacyBorder);

            var dlg = new ScreenSourcePickerWindow(self, IsCurrentSource, initialTab, _hideClassroomShellFromStudents);
            if (visibleOwner != null)
            {
                dlg.Owner = visibleOwner;
            }
            else
            {
                dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dlg.Topmost = true;
            }
            dlg.SourceInitialized += (_, _) =>
            {
                // Luôn ẩn hộp chọn khỏi ảnh chụp màn hình để ảnh preview của Màn hình 1 không bị đè bởi chính hộp chọn này
                NativeMethods.SetWindowDisplayAffinity(new System.Windows.Interop.WindowInteropHelper(dlg).Handle, 0x11); // WDA_EXCLUDEFROMCAPTURE
            };
            if (dlg.ShowDialog() != true || dlg.Selected == null) return false;
            autoMinimizeClassroomShell = dlg.AutoMinimizeClassroomShell;
            _hideClassroomShellFromStudents = dlg.HideClassroomShellFromStudents;
            _floatingBroadcastBar?.SetHideClassroomShellCheck(_hideClassroomShellFromStudents);
            (Application.Current as QASmartTouch.App)?._floatingModeBar?.SetHideClassroomShellCheck(_hideClassroomShellFromStudents);
            ApplyPickedSource(dlg.Selected);
            if (_isBroadcasting)
            {
                bool shouldExclude = _hideClassroomShellFromStudents && dlg.Selected.Kind == ScreenSourceKind.Screen;
                ApplyScreenCaptureExclusion(shouldExclude);
            }
            return true;
        }

        private bool IsCurrentSource(ScreenSourceInfo s)
        {
            ScreenSourceInfo? cur;
            lock (_srcLock) cur = _picked;
            if (cur == null) return s.Kind == ScreenSourceKind.Screen && s.IsPrimary;
            if (cur.Kind != s.Kind) return false;
            return s.Kind switch
            {
                ScreenSourceKind.Window => s.Hwnd == cur.Hwnd,
                ScreenSourceKind.QaClassPage => string.Equals(s.PageFormId, cur.PageFormId, StringComparison.OrdinalIgnoreCase),
                _ => s.X == cur.X && s.Y == cur.Y && s.Width == cur.Width && s.Height == cur.Height
            };
        }

        private void ApplyPickedSource(ScreenSourceInfo src)
        {
            lock (_srcLock) _picked = src;
            _lastIsolatedSourceBitmap = null;
            _sharedWindowClosedStopTriggered = false;
            _pendingSwitchCandidate = null;
            _pendingSwitchLabel = "";

            if (src.Kind == ScreenSourceKind.QaClassPage && string.Equals(src.PageFormId, "CURRENT", StringComparison.OrdinalIgnoreCase))
            {
                var pinShell = (Window.GetWindow(this) as ClassroomShell) ?? (Application.Current?.MainWindow as ClassroomShell);
                pinShell?.PinCurrentPageForBroadcast();
            }

            _suppressSourceReset = true;
            try
            {
                if (src.Kind == ScreenSourceKind.Window) rbWindow.IsChecked = true;
                else rbFullScreen.IsChecked = true;
            }
            finally { _suppressSourceReset = false; }
            panelWindowPicker.Visibility = Visibility.Collapsed; // hộp chọn mới thay cho ComboBox cũ

            if (btnOpenSharedQaPage != null)
            {
                btnOpenSharedQaPage.Visibility = (src.Kind == ScreenSourceKind.QaClassPage && src.PageFormId != "CURRENT")
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                if (src.Kind == ScreenSourceKind.QaClassPage)
                {
                    btnOpenSharedQaPage.Content = $"🚀 Mở '{src.Title}' để thao tác";
                }
            }

            if (src.Kind == ScreenSourceKind.Window)
            {
                if (NativeMethods.IsIconic(src.Hwnd)) NativeMethods.ShowWindow(src.Hwnd, 9);
                txtPickedSource.Text = $"Nguồn hiện tại: Cửa sổ — {src.Title}";
            }
            else if (src.Kind == ScreenSourceKind.Region)
            {
                txtPickedSource.Text = $"Nguồn hiện tại: ✂️ {src.Title}";
                if (_isBroadcasting)
                {
                    ApplyScreenCaptureExclusion(false);
                    if (_privacyBorder == null)
                    {
                        _privacyBorder = new BroadcastPrivacyBorder();
                        _privacyBorder.Show();
                    }
                    _privacyBorder.SetBounds(src.DipLeft, src.DipTop, src.DipWidth, src.DipHeight);
                }
            }
            else if (src.Kind == ScreenSourceKind.QaClassPage)
            {
                txtPickedSource.Text = $"Nguồn hiện tại: 📑 Trang QA SmartClass — {src.Title}";
                if (_isBroadcasting)
                {
                    ApplyScreenCaptureExclusion(false);
                    if (_privacyBorder != null)
                    {
                        _privacyBorder.Close();
                        _privacyBorder = null;
                    }
                    if (src.AutoNavigateToPage && !string.IsNullOrEmpty(src.PageFormId) && src.PageFormId != "CURRENT")
                    {
                        var shell = (Window.GetWindow(this) as ClassroomShell) ?? (Application.Current?.MainWindow as ClassroomShell);
                        shell?.NavigateToQaPageForBroadcast(src.PageFormId);
                    }
                }
            }
            else
            {
                int no = ScreenSourceEnumerator.GetScreens().FindIndex(s => s.X == src.X && s.Y == src.Y) + 1;
                txtPickedSource.Text = $"Nguồn hiện tại: Màn hình {Math.Max(no, 1)}{(src.IsPrimary ? " (chính)" : "")} — {src.Width}×{src.Height}";
                if (_isBroadcasting && _privacyBorder != null)
                {
                    _privacyBorder.SetBounds(0, 0, 0, 0);
                }
            }
            UpdateSelectedSourcePreviewThumb();
            HighlightSelectedInlineCard();
            RefreshCachedBroadcastParams();
            UpdateGoogleMeetSourceLockState();
            _lastFrameSampleHash = 0;
            _settlementKeyframeSent = false;
            Log.Information("[BroadcastPage] Source picked: {Kind} {Title} ({W}x{H}) live={Live}", src.Kind, src.Title, src.Width, src.Height, _isBroadcasting);
        }

        private string _inlinePreviewFilter = "ALL";
        private int _inlinePreviewVersion = 0;

        private void RefreshInlinePreviews_Click(object sender, RoutedEventArgs e)
        {
            RefreshInlineSourcePreviews();
            UpdateSelectedSourcePreviewThumb();
        }

        private void FilterInlinePreviews_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string filterTag)
            {
                _inlinePreviewFilter = filterTag;
                var activeBg = new SolidColorBrush(Color.FromRgb(29, 78, 216));
                var normalBg = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                var normalFg = new SolidColorBrush(Color.FromRgb(51, 65, 85));

                Button[] filterBtns = { btnFilterAllSources, btnFilterScreens, btnFilterWindows, btnFilterQaPages, btnFilterRegions };
                foreach (var b in filterBtns)
                {
                    if (b == null) continue;
                    bool isMatch = string.Equals(b.Tag as string, filterTag, StringComparison.OrdinalIgnoreCase);
                    b.Background = isMatch ? activeBg : normalBg;
                    b.Foreground = isMatch ? Brushes.White : normalFg;
                    b.FontWeight = isMatch ? FontWeights.Bold : FontWeights.SemiBold;
                }
                RefreshInlineSourcePreviews();
            }
        }

        private void UpdateSelectedSourcePreviewThumb()
        {
            if (imgSelectedSourceThumb == null) return;
            ScreenSourceInfo? cur;
            lock (_srcLock) cur = _picked;
            var srcToPreview = cur ?? ScreenSourceEnumerator.GetScreens().FirstOrDefault();
            if (srcToPreview == null) return;

            var owner = Window.GetWindow(this);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var bmp = ScreenSourceEnumerator.CaptureSourcePreview(srcToPreview, owner);
                if (bmp != null)
                {
                    imgSelectedSourceThumb.Source = bmp;
                }
            }), DispatcherPriority.Background);
        }

        private void HighlightSelectedInlineCard()
        {
            if (wrapInlineSourcePreviews == null) return;
            foreach (var child in wrapInlineSourcePreviews.Children)
            {
                if (child is Border card && card.Tag is ScreenSourceInfo info)
                {
                    bool sel = IsCurrentSource(info);
                    card.BorderBrush = sel
                        ? new SolidColorBrush(Color.FromRgb(37, 99, 235))
                        : new SolidColorBrush(Color.FromRgb(203, 213, 225));
                    card.Background = sel
                        ? new SolidColorBrush(Color.FromRgb(239, 246, 255))
                        : Brushes.White;
                }
            }
        }

        private void RefreshInlineSourcePreviews()
        {
            if (wrapInlineSourcePreviews == null) return;
            wrapInlineSourcePreviews.Children.Clear();

            int version = ++_inlinePreviewVersion;
            var owner = Window.GetWindow(this);
            IntPtr selfHwnd = owner != null ? new System.Windows.Interop.WindowInteropHelper(owner).Handle : IntPtr.Zero;
            var dpi = VisualTreeHelper.GetDpi(this);

            System.Collections.Generic.List<ScreenSourceInfo> sources = _inlinePreviewFilter switch
            {
                "SCREEN" => ScreenSourceEnumerator.GetScreens(),
                "WINDOW" => ScreenSourceEnumerator.GetWindows(selfHwnd),
                "QAPAGE" => ScreenSourceEnumerator.GetQaClassPages(),
                "REGION" => ScreenSourceEnumerator.GetRegionPresets(dpi.DpiScaleX, dpi.DpiScaleY),
                _ => ScreenSourceEnumerator.GetAllShareableSources(selfHwnd, dpi.DpiScaleX, dpi.DpiScaleY)
            };

            if (sources.Count == 0)
            {
                wrapInlineSourcePreviews.Children.Add(new TextBlock
                {
                    Text = "Không có cửa sổ hoặc nguồn nào trong mục này.",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    Margin = new Thickness(8)
                });
                return;
            }

            foreach (var src in sources)
            {
                var hold = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                    CornerRadius = new CornerRadius(6),
                    Height = 122,
                    Margin = new Thickness(0, 0, 0, 8),
                    ClipToBounds = true
                };

                var thumbImg = new Image { Stretch = Stretch.Uniform };
                RenderOptions.SetBitmapScalingMode(thumbImg, BitmapScalingMode.HighQuality);

                var fallbackStack = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                fallbackStack.Children.Add(new TextBlock
                {
                    Text = src.IconGlyph,
                    FontSize = 30,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                fallbackStack.Children.Add(new TextBlock
                {
                    Text = src.Kind switch
                    {
                        ScreenSourceKind.Screen => "TOÀN MÀN HÌNH",
                        ScreenSourceKind.Window => "CỬA SỔ ỨNG DỤNG",
                        ScreenSourceKind.Region => "VÙNG MÀN HÌNH",
                        _ => "TRANG QA SMARTCLASS"
                    },
                    FontSize = 9.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(147, 197, 253)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                });

                var previewGrid = new Grid();
                previewGrid.Children.Add(fallbackStack);
                previewGrid.Children.Add(thumbImg);
                hold.Child = previewGrid;

                var capturedSrc = src;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (version != _inlinePreviewVersion) return;
                    var bmp = ScreenSourceEnumerator.CaptureSourcePreview(capturedSrc, owner);
                    if (bmp != null)
                    {
                        thumbImg.Source = bmp;
                        fallbackStack.Visibility = Visibility.Collapsed;
                    }
                }), DispatcherPriority.Background);

                var titleTxt = new TextBlock
                {
                    Text = src.Title,
                    FontSize = 12.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    ToolTip = src.Title
                };

                var subTxt = new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(src.Subtitle) ? $"{src.Width}×{src.Height} px" : src.Subtitle,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 2, 0, 6)
                };

                var btnStartThis = new Button
                {
                    Content = "🚀 Chiếu nguồn này ngay",
                    Padding = new Thickness(10, 5, 10, 5),
                    FontSize = 11.5,
                    FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                btnStartThis.Click += (_, ev) =>
                {
                    ev.Handled = true;
                    ApplyPickedSource(capturedSrc);
                    if (!_isBroadcasting)
                    {
                        _skipConfirmForQuickBroadcast = true;
                        try { Broadcast_Click(this, new RoutedEventArgs()); }
                        finally { _skipConfirmForQuickBroadcast = false; }
                    }
                };

                var cardStack = new StackPanel();
                cardStack.Children.Add(hold);
                cardStack.Children.Add(titleTxt);
                cardStack.Children.Add(subTxt);
                cardStack.Children.Add(btnStartThis);

                bool isSelected = IsCurrentSource(src);
                var cardBorder = new Border
                {
                    Width = 236,
                    Margin = new Thickness(0, 0, 10, 10),
                    Padding = new Thickness(8),
                    CornerRadius = new CornerRadius(8),
                    BorderThickness = new Thickness(2),
                    BorderBrush = isSelected
                        ? new SolidColorBrush(Color.FromRgb(37, 99, 235))
                        : new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    Background = isSelected
                        ? new SolidColorBrush(Color.FromRgb(239, 246, 255))
                        : Brushes.White,
                    Cursor = Cursors.Hand,
                    Tag = src,
                    Child = cardStack
                };

                cardBorder.MouseLeftButtonUp += (_, ev) =>
                {
                    ApplyPickedSource(capturedSrc);
                    if (ev.ClickCount >= 2 && !_isBroadcasting)
                    {
                        _skipConfirmForQuickBroadcast = true;
                        try { Broadcast_Click(this, new RoutedEventArgs()); }
                        finally { _skipConfirmForQuickBroadcast = false; }
                    }
                };

                wrapInlineSourcePreviews.Children.Add(cardBorder);
            }
        }

        private void ResetPickedToPrimary()
        {
            lock (_srcLock) _picked = null;
            _suppressSourceReset = true;
            try { rbFullScreen.IsChecked = true; }
            finally { _suppressSourceReset = false; }
            panelWindowPicker.Visibility = Visibility.Collapsed;
            if (btnOpenSharedQaPage != null) btnOpenSharedQaPage.Visibility = Visibility.Collapsed;
            txtPickedSource.Text = "Cửa sổ đang chiếu đã đóng — chuyển về Toàn màn hình (màn hình chính)";
        }

        private void ClearPickedSource()
        {
            lock (_srcLock) _picked = null;
            _lastIsolatedSourceBitmap = null;
            _pendingSwitchCandidate = null;
            _pendingSwitchLabel = "";
            if (btnOpenSharedQaPage != null) btnOpenSharedQaPage.Visibility = Visibility.Collapsed;
            if (txtPickedSource != null) txtPickedSource.Text = DefaultSourceText;
            UpdateGoogleMeetSourceLockState();
        }

        private DateTime _lastLivePreview = DateTime.MinValue;

        private static bool IsScreenExclusionActive() =>
            QASmartTouch.Services.AppSettings.Broadcast_EnableScreenExclusion
            && !(QASmartTouch.Services.AppSettings.Broadcast_TurboMode && QASmartTouch.Services.AppSettings.Broadcast_TurboDisableExclusion);

        private static string DescribeSource(ScreenSourceInfo src)
        {
            return src.Kind switch
            {
                ScreenSourceKind.Window => $"📌 Cửa sổ — {src.Title}",
                ScreenSourceKind.Region => $"✂️ Vùng — {src.Title}",
                ScreenSourceKind.QaClassPage => $"📌 {src.Title}",
                _ => $"🖥️ Màn hình {Math.Max(ScreenSourceEnumerator.GetScreens().FindIndex(s => s.X == src.X && s.Y == src.Y) + 1, 1)}{(src.IsPrimary ? " (chính)" : "")}"
            };
        }

        /// <summary>
        /// Chụp độc lập 100% bề mặt của một cửa sổ (hWnd) bằng PrintWindow(PW_RENDERFULLCONTENT = 2) ở kích thước gốc rồi thu phóng HALFTONE.
        /// Tuyệt đối KHÔNG đọc điểm ảnh màn hình nền (GetDC(0)) và KHÔNG tự khôi phục cửa sổ đang thu nhỏ (SW_RESTORE),
        /// đảm bảo chuẩn Google Meet: mở cửa sổ khác đè lên hoặc chuyển màn hình khác thì Học sinh vẫn CHỈ thấy đúng cửa sổ đã chọn!
        /// </summary>
        private static BitmapSource? TryCaptureIsolatedWindowBitmap(IntPtr targetHWnd, int targetWidth, bool drawCursor)
        {
            if (targetHWnd == IntPtr.Zero
                || !NativeMethods.IsWindow(targetHWnd)
                || !NativeMethods.IsWindowVisible(targetHWnd)
                || NativeMethods.IsIconic(targetHWnd))
            {
                return null;
            }

            if (!NativeMethods.GetWindowRect(targetHWnd, out var rect))
            {
                return null;
            }

            int winW = rect.Right - rect.Left;
            int winH = rect.Bottom - rect.Top;
            if (winW < 64 || winH < 64)
            {
                return null;
            }

            double scaleRatio = winW > targetWidth ? (double)targetWidth / winW : 1.0;
            int outW = Math.Max(64, (int)Math.Round(winW * scaleRatio));
            int outH = Math.Max(64, (int)Math.Round(winH * scaleRatio));

            IntPtr hdcRef = IntPtr.Zero;
            IntPtr hdcWinFull = IntPtr.Zero;
            IntPtr hbmWinFull = IntPtr.Zero;
            IntPtr hOldWinFull = IntPtr.Zero;
            IntPtr hdcScaled = IntPtr.Zero;
            IntPtr hbmScaled = IntPtr.Zero;
            IntPtr hOldScaled = IntPtr.Zero;

            try
            {
                hdcRef = NativeMethods.GetDC(IntPtr.Zero);
                hdcWinFull = NativeMethods.CreateCompatibleDC(hdcRef);
                hbmWinFull = NativeMethods.CreateCompatibleBitmap(hdcRef, winW, winH);
                hOldWinFull = NativeMethods.SelectObject(hdcWinFull, hbmWinFull);

                bool printed = NativeMethods.PrintWindow(targetHWnd, hdcWinFull, 2); // PW_RENDERFULLCONTENT = 2
                if (!printed)
                {
                    printed = NativeMethods.PrintWindow(targetHWnd, hdcWinFull, 0);
                }
                if (!printed)
                {
                    return null;
                }

                // Chỉ vẽ con trỏ chuột khi cửa sổ đang được chiếu thực sự là cửa sổ tiền cảnh (Foreground)
                bool isForegroundSharedWindow = drawCursor && NativeMethods.GetForegroundWindow() == targetHWnd;

                IntPtr finalHBitmap;
                if (outW == winW && outH == winH)
                {
                    if (isForegroundSharedWindow)
                    {
                        DrawCursorOverlayOnHdc(hdcWinFull, rect.Left, rect.Top, winW, winH, winW, winH);
                    }
                    finalHBitmap = hbmWinFull;
                }
                else
                {
                    hdcScaled = NativeMethods.CreateCompatibleDC(hdcRef);
                    hbmScaled = NativeMethods.CreateCompatibleBitmap(hdcRef, outW, outH);
                    hOldScaled = NativeMethods.SelectObject(hdcScaled, hbmScaled);
                    NativeMethods.SetStretchBltMode(hdcScaled, 4); // HALFTONE = 4
                    NativeMethods.SetBrushOrgEx(hdcScaled, 0, 0, IntPtr.Zero);
                    NativeMethods.StretchBlt(hdcScaled, 0, 0, outW, outH, hdcWinFull, 0, 0, winW, winH, 0x00CC0020); // SRCCOPY từ hdcWinFull (KHÔNG lấy từ màn hình nền!)

                    if (isForegroundSharedWindow)
                    {
                        DrawCursorOverlayOnHdc(hdcScaled, rect.Left, rect.Top, winW, winH, outW, outH);
                    }
                    finalHBitmap = hbmScaled;
                }

                var rawBmp = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    finalHBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                var opaqueBmp = new FormatConvertedBitmap(rawBmp, PixelFormats.Bgr32, null, 0);
                opaqueBmp.Freeze();

                if (IsMostlyBlack(opaqueBmp))
                {
                    return null;
                }

                return opaqueBmp;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (hOldScaled != IntPtr.Zero) NativeMethods.SelectObject(hdcScaled, hOldScaled);
                if (hbmScaled != IntPtr.Zero) NativeMethods.DeleteObject(hbmScaled);
                if (hdcScaled != IntPtr.Zero) NativeMethods.DeleteDC(hdcScaled);
                if (hOldWinFull != IntPtr.Zero) NativeMethods.SelectObject(hdcWinFull, hOldWinFull);
                if (hbmWinFull != IntPtr.Zero) NativeMethods.DeleteObject(hbmWinFull);
                if (hdcWinFull != IntPtr.Zero) NativeMethods.DeleteDC(hdcWinFull);
                if (hdcRef != IntPtr.Zero) NativeMethods.ReleaseDC(IntPtr.Zero, hdcRef);
            }
        }

        private void OnSharedWindowClosedStopBroadcast()
        {
            if (!_isBroadcasting) return;
            StopAllBroadcasting();
            if (txtPickedSource != null)
            {
                txtPickedSource.Text = "⏹️ Đã tự động dừng chia sẻ vì cửa sổ trình chiếu đã đóng (Chuẩn Google Meet).";
            }
            ShowToast("⏹️ Đã dừng chia sẻ do cửa sổ trình chiếu đã đóng.", "#1976D2");
        }

        /// <summary>
        /// Kiểm tra trạng thái khóa cố định nguồn chiếu chuẩn Google Meet:
        /// - Khi đang chiếu Cửa sổ hoặc Trang / Bảng SMART TOUCH: giữ cố định 100% nguồn đó.
        /// - Nếu Giáo viên đang thao tác trên một cửa sổ / trang bài giảng khác, hiển thị nút "🔄 Chiếu '...' thay thế" trên thanh nổi (nhưng KHÔNG tự ý đổi luồng chiếu cho tới khi Giáo viên bấm!).
        /// </summary>
        private void UpdateGoogleMeetSourceLockState()
        {
            var unifiedOrb = (Application.Current as QASmartTouch.App)?._floatingModeBar;
            if ((_floatingBroadcastBar == null && unifiedOrb == null) || !_isBroadcasting) return;

            void ApplyLockIndicator(bool locked, bool canSwitch, string label)
            {
                _floatingBroadcastBar?.UpdateSourceLockIndicator(locked, canSwitch, label);
                unifiedOrb?.UpdateSourceLockIndicator(locked, canSwitch, label);
            }

            ScreenSourceInfo? curPicked;
            lock (_srcLock) curPicked = _picked;

            bool isLocked = (curPicked != null && (curPicked.Kind == ScreenSourceKind.Window || curPicked.Kind == ScreenSourceKind.QaClassPage))
                         || (curPicked == null && rbWindow?.IsChecked == true);

            if (!isLocked)
            {
                _pendingSwitchCandidate = null;
                _pendingSwitchLabel = "";
                ApplyLockIndicator(false, false, "");
                return;
            }

            try
            {
                IntPtr fgHwnd = NativeMethods.GetForegroundWindow();
                if (fgHwnd == IntPtr.Zero)
                {
                    ApplyLockIndicator(true, _pendingSwitchCandidate != null, _pendingSwitchLabel);
                    return;
                }

                // Nếu Giáo viên vừa chạm vào chính Thanh điều khiển nổi hoặc Thanh chuyển chế độ, giữ nguyên gợi ý trước đó để nút không bị biến mất dưới ngón tay/chuột!
                if (Application.Current != null)
                {
                    foreach (Window w in Application.Current.Windows)
                    {
                        var wh = new System.Windows.Interop.WindowInteropHelper(w).Handle;
                        if (wh != IntPtr.Zero && wh == fgHwnd)
                        {
                            if (w is FloatingBroadcastBarWindow
                                || w is QASmartClass.Shared.FloatingModeBar
                                || w is BroadcastPrivacyBorder
                                || w is ScreenSourcePickerWindow)
                            {
                                ApplyLockIndicator(true, _pendingSwitchCandidate != null, _pendingSwitchLabel);
                                return;
                            }

                            // Đang mở bảng SMART TOUCH
                            if (w is QASmartTouch.Forms.Form2_MainDashboard)
                            {
                                bool alreadySharingSmartTouch =
                                    (curPicked?.Kind == ScreenSourceKind.QaClassPage && string.Equals(curPicked.PageFormId, "SMART_TOUCH", StringComparison.OrdinalIgnoreCase))
                                    || (curPicked?.Kind == ScreenSourceKind.Window && curPicked.Hwnd == fgHwnd);

                                if (alreadySharingSmartTouch)
                                {
                                    _pendingSwitchCandidate = null;
                                    _pendingSwitchLabel = "";
                                }
                                else
                                {
                                    _pendingSwitchCandidate = new ScreenSourceInfo
                                    {
                                        Kind = ScreenSourceKind.QaClassPage,
                                        PageFormId = "SMART_TOUCH",
                                        IconGlyph = "🎨",
                                        Title = "Bảng trắng tương tác SMART TOUCH",
                                        Width = (int)SystemParameters.PrimaryScreenWidth,
                                        Height = (int)SystemParameters.PrimaryScreenHeight,
                                        AutoNavigateToPage = false
                                    };
                                    _pendingSwitchLabel = "Bảng SMART TOUCH";
                                }
                                ApplyLockIndicator(true, _pendingSwitchCandidate != null, _pendingSwitchLabel);
                                return;
                            }

                            // Đang mở cửa sổ Quản lý lớp SMART CLASS
                            if (w is ClassroomShell shell)
                            {
                                string? lockedPageId = curPicked?.Kind == ScreenSourceKind.QaClassPage ? curPicked.PageFormId : null;
                                var (_, activeTitle, canSwitch) = shell.GetActiveQaPageSwitchCandidate(lockedPageId);
                                if (canSwitch && !string.IsNullOrWhiteSpace(activeTitle))
                                {
                                    _pendingSwitchCandidate = new ScreenSourceInfo
                                    {
                                        Kind = ScreenSourceKind.QaClassPage,
                                        PageFormId = "CURRENT",
                                        IconGlyph = "📌",
                                        Title = activeTitle,
                                        AutoNavigateToPage = false
                                    };
                                    _pendingSwitchLabel = activeTitle;
                                }
                                else
                                {
                                    _pendingSwitchCandidate = null;
                                    _pendingSwitchLabel = "";
                                }
                                ApplyLockIndicator(true, _pendingSwitchCandidate != null, _pendingSwitchLabel);
                                return;
                            }
                        }
                    }
                }

                // Đang mở một cửa sổ ứng dụng ngoài Desktop (PowerPoint, Chrome, Word, PDF...)
                if (NativeMethods.IsWindowVisible(fgHwnd) && !NativeMethods.IsIconic(fgHwnd))
                {
                    bool alreadySharingThisWindow =
                        (curPicked?.Kind == ScreenSourceKind.Window && curPicked.Hwnd == fgHwnd)
                        || (curPicked == null && rbWindow?.IsChecked == true && _cachedWindowHwnd == fgHwnd);

                    if (alreadySharingThisWindow)
                    {
                        _pendingSwitchCandidate = null;
                        _pendingSwitchLabel = "";
                    }
                    else if ((NativeMethods.GetWindowLong(fgHwnd, -20) & 0x80) == 0) // Bỏ qua WS_EX_TOOLWINDOW
                    {
                        string fgTitle = ScreenSourceEnumerator.GetWindowTitle(fgHwnd);
                        if (!string.IsNullOrWhiteSpace(fgTitle)
                            && fgTitle != "Program Manager"
                            && NativeMethods.GetWindowRect(fgHwnd, out var fgRc))
                        {
                            int fw = fgRc.Right - fgRc.Left;
                            int fh = fgRc.Bottom - fgRc.Top;
                            if (fw >= 160 && fh >= 120)
                            {
                                _pendingSwitchCandidate = new ScreenSourceInfo
                                {
                                    Kind = ScreenSourceKind.Window,
                                    IconGlyph = "🪟",
                                    Title = fgTitle,
                                    Hwnd = fgHwnd,
                                    X = fgRc.Left,
                                    Y = fgRc.Top,
                                    Width = fw,
                                    Height = fh
                                };
                                _pendingSwitchLabel = fgTitle;
                            }
                        }
                    }
                }

                ApplyLockIndicator(true, _pendingSwitchCandidate != null, _pendingSwitchLabel);
            }
            catch
            {
                // An toàn luồng UI
            }
        }

        private void SwitchBroadcastToActiveScreenInstead()
        {
            if (!_isBroadcasting || _pendingSwitchCandidate == null) return;
            var nextSource = _pendingSwitchCandidate;
            if (nextSource.Kind == ScreenSourceKind.QaClassPage && string.Equals(nextSource.PageFormId, "CURRENT", StringComparison.OrdinalIgnoreCase))
            {
                var shell = (Window.GetWindow(this) as ClassroomShell) ?? (Application.Current?.MainWindow as ClassroomShell);
                shell?.PinCurrentPageForBroadcast(forceActiveFrameContent: true);
            }
            ApplyPickedSource(nextSource);
            ShowToast($"📌 Đã chuyển sang giữ cố định: {nextSource.Title}", "#1976D2");
        }

        /// <summary>true nếu ảnh gần như đen toàn bộ (mẫu 16×9 điểm).</summary>
        private static bool IsMostlyBlack(BitmapSource bs)
        {
            try
            {
                int w = bs.PixelWidth, h = bs.PixelHeight;
                if (w < 16 || h < 9) return false;
                var small = new TransformedBitmap(bs, new ScaleTransform(16.0 / w, 9.0 / h));
                var conv = new FormatConvertedBitmap(small, PixelFormats.Bgra32, null, 0);
                int sw = conv.PixelWidth, sh = conv.PixelHeight;
                var buf = new byte[sw * sh * 4];
                conv.CopyPixels(buf, sw * 4, 0);
                for (int i = 0; i < buf.Length; i += 4)
                    if (buf[i] > 8 || buf[i + 1] > 8 || buf[i + 2] > 8) return false;
                return true;
            }
            catch { return false; }
        }

        private void SourceChanged(object sender, RoutedEventArgs e)
        {
            if (panelWindowPicker == null || panelCameraPicker == null) return;
            if (!_suppressSourceReset) ClearPickedSource(); // GV tự bấm radio cũ → bỏ nguồn đã chọn trong hộp
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
                // app → ClassroomAppContext (refactored)

                // Ưu tiên dữ liệu từ GroupPage (ClassroomAppContext.CurrentGroups)
                var state = QASmartClass.Services.BroadcastStateService.Instance;
                var groups = ClassroomAppContext.CurrentGroups;
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
                    var students = ClassroomAppContext.Db?.Students?.ToList();
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
                // app → ClassroomAppContext (refactored)

                // Load HS theo active roster
                var allStudents = Services.VietnameseNameHelper.SortByVietnameseName(
                    GetStudentsForCurrentContext(), s => s.FullName);
                _cachedStudents = allStudents;

                if (allStudents != null && allStudents.Count > 0)
                {
                    var state = QASmartClass.Services.BroadcastStateService.Instance;
                    var onlineStudents = ClassroomAppContext.Network?.GetConnectedStudents();

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
                    // app → ClassroomAppContext (refactored)
                    var selectedGroups = groupCheckList.Children.OfType<CheckBox>()
                        .Where(c => c.IsChecked == true).ToList();
                    int groupCount = selectedGroups.Count;

                    // Đếm tổng HS trong các nhóm được chọn
                    int totalMembers = 0;
                    if (ClassroomAppContext.CurrentGroups?.Count > 0)
                    {
                        foreach (var cb in selectedGroups)
                        {
                            if (cb.Tag is int idx && idx < ClassroomAppContext.CurrentGroups.Count)
                                totalMembers += ClassroomAppContext.CurrentGroups[idx].Members.Count;
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
                // app → ClassroomAppContext (refactored)
                if (ClassroomAppContext.Db == null) return;

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
                // app → ClassroomAppContext (refactored)
                ClassroomAppContext.BroadcastState.AddBroadcastFile(_selectedFilePath);

                var ft = ClassroomAppContext.FileTransfer;
                if (ft != null)
                {
                    var desc = QASmartClass.Services.BroadcastStateService.Instance.RegisterSharedFile(_selectedFilePath, autoOpen: true);
                    int webPort = ClassroomAppContext.Network?.WebBridge?.WebPort ?? 8080;
                    string serverIp = ClassroomAppContext.Network?.ServerIP ?? "127.0.0.1";
                    string token = QASmartClass.Services.BroadcastStateService.Instance.EnsureBroadcastToken();
                    string dlUrl = $"http://{serverIp}:{webPort}/api/files/download?id={Uri.EscapeDataString(desc.FileId)}&token={Uri.EscapeDataString(token)}";
                    string offerCmd = $"CMD|FILE_OFFER|{desc.FileId}|{desc.FileName.Replace('|', '_')}|{desc.FileSize}|1|{webPort}|{token}|{dlUrl}";
                    await SendCommandToTargetsAsync(offerCmd);
                }
                /* // [MYTHWARE_LITE] Gửi đường dẫn cục bộ qua CMD|FILE_BROADCAST:
                int webPort = 8080;
                if (ClassroomAppContext.Network?.WebBridge != null)
                {
                    webPort = ClassroomAppContext.Network.WebBridge.WebPort;
                }
                if (string.IsNullOrEmpty(QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken))
                {
                    QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken = "TK_" + Guid.NewGuid().ToString("N").Substring(0, 16);
                }
                var token = QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken;
                var cmd = $"CMD|FILE_BROADCAST|{_selectedFilePath}|{webPort}|{token}";

                // Gửi theo đối tượng đã chọn
                await SendCommandToTargetsAsync(cmd);
                */

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

            // app → ClassroomAppContext (refactored)
            var codes = new System.Collections.Generic.List<string>();

            if (rbGroup.IsChecked == true)
            {
                // L\u1EA5y t\u00EAn HS t\u1EEB c\u00E1c nh\u00F3m \u0111\u01B0\u1EE3c ch\u1ECDn, map sang StudentCode
                var selectedGroupChecks = groupCheckList.Children.OfType<CheckBox>()
                    .Where(c => c.IsChecked == true).ToList();

                var groups = ClassroomAppContext.CurrentGroups;
                if (groups != null && groups.Count > 0)
                {
                    // CurrentGroups mode: Members l\u00E0 danh s\u00E1ch t\u00EAn HS
                    var groupIndexes = selectedGroupChecks
                        .Where(c => c.Tag is int)
                        .Select(c => (int)c.Tag!);
                    codes.AddRange(QASmartClass.Classroom.Services.StudentSelectionService.ResolveGroupCodes(groupIndexes));
                    // Ánh xạ tên HS → StudentCode (xử lý trùng tên theo lớp đang hoạt động) do StudentSelectionService đảm nhiệm.
                }
                else
                {
                    // ClassName fallback mode: Tag l\u00E0 className string
                    var selectedClassNames = selectedGroupChecks
                        .Where(c => c.Tag is string)
                        .Select(c => (string)c.Tag!).ToList();
                    var students = ClassroomAppContext.Db?.Students?.ToList();
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
            // app → ClassroomAppContext (refactored)
            // app always available via ClassroomAppContext
            var targetCodes = GetTargetStudentCodes();

            if (targetCodes == null)
            {
                // Broadcast t\u1EA5t c\u1EA3
                if (ClassroomAppContext.Network?.IsBroadcasting == true)
                    await ClassroomAppContext.Network.SendCommandAsync(cmd);
                else
                    Dispatcher.BeginInvoke(new Action(() => ClassroomAppContext.DispatchCommand(cmd)), DispatcherPriority.Background);
            }
            else
            {
                // G\u1EEDi cho nh\u00F3m HS c\u1EE5 th\u1EC3
                if (ClassroomAppContext.Network?.IsBroadcasting == true)
                    await ClassroomAppContext.Network.SendToStudentsAsync(targetCodes, cmd);
                else
                    Dispatcher.BeginInvoke(new Action(() => ClassroomAppContext.DispatchCommand(cmd)), DispatcherPriority.Background);
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
                // app → ClassroomAppContext (refactored)
                if (ClassroomAppContext.Db == null) return;
                ClassroomAppContext.Db.EventLogs.Add(new EventLog { EventType = type, Actor = actor, Details = details, Timestamp = DateTime.Now });
                await ClassroomAppContext.Db.SaveChangesAsync();
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
                    if (win is FloatingBroadcastBarWindow)
                    {
                        // Thanh điều khiển nổi (Floating Share Bar + PiP) luôn ẩn khỏi ảnh chụp của Học sinh
                        var fHelper = new System.Windows.Interop.WindowInteropHelper(win);
                        if (fHelper.Handle != IntPtr.Zero)
                        {
                            NativeMethods.SetWindowDisplayAffinity(fHelper.Handle, 0x00000011U);
                        }
                        continue;
                    }
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

            // Ghi đè UI Settings tạm thời sang cấu hình đọc chữ cực nét (SP-04) nhưng vẫn giữ 30 FPS mượt mà
            cmbResolution.SelectedIndex = 1; // 1920x1080
            if (cmbFPS.Items.Count > 4) cmbFPS.SelectedIndex = 4; // Rất mượt (30 fps)
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
                // app → ClassroomAppContext (refactored)
                var targetCodes = GetTargetStudentCodes();
                int recipientCount = targetCodes == null ? (ClassroomAppContext.Network?.ConnectedCount ?? 0) : targetCodes.Count;

                if (targetCodes != null && targetCodes.Count == 0)
                {
                    ClassroomDialog.Warn(
                        "⚠️ Chưa chọn đối tượng nhận chiếu màn hình.\n\nVui lòng chọn ít nhất 1 học sinh hoặc 1 nhóm trước khi bắt đầu phát.", "Lỗi cấu hình — Không có đối tượng nhận");
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

            // [MYTHWARE_LITE] Mặc định khóa toàn màn hình HS (chỉ mở khóa khi tích chkAllowStudentExit)
            bool allowExitVnc = chkAllowStudentExit?.IsChecked == true;
            chkForceWatch.IsChecked = !allowExitVnc;
            /* [MYTHWARE_LITE] Đã ẩn hộp thoại xác nhận cũ
            // Xác nhận trước khi bật FORCE_WATCH
            if (chkForceWatch.IsChecked == true)
            {
                var confirmResult = QASmartClass.Shared.QAConfirmDialog.ShowDialog(
                    "Xác nhận khóa màn hình (VNC)",
                    "[Cảnh báo] BẠN ĐANG BẬT CHẾ ĐỘ KHÓA MÀN HÌNH HỌC SINH\n\nHọc sinh sẽ không thể đóng màn hình chiếu và bàn phím bị vô hiệu hóa.\n\nBạn có chắc chắn muốn tiếp tục?",
                    Window.GetWindow(this));
                if (!confirmResult) return;
            }
            */

            // Bắt đầu phát sóng thông qua Singleton Service
            bool isForceWatch = !allowExitVnc;
            var selectedTargets = GetTargetStudentCodes();

            bool started = await QASmartClass.Services.VncBroadcastService.Instance.StartBroadcastAsync(isForceWatch, selectedTargets);

            if (started)
            {
                SaveEvent("VNC_BROADCAST", "GV", $"Bắt đầu phát VNC: {QASmartClass.Services.VncBroadcastService.Instance.VncPort} | {QASmartClass.Services.VncBroadcastService.Instance.VncSessionCode}");
            }
            else
            {
                ClassroomDialog.Error("Khởi động VNC Server thất bại. Vui lòng kiểm tra lại cấu hình hệ thống.", "Lỗi tích hợp VNC");
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

        // ═══════════════════════════════════════════════════════════════════
        //  GOOGLE MEET AUDIO & CAMERA CONTROLS ON BROADCAST PAGE
        // ═══════════════════════════════════════════════════════════════════

        private void OnLiveBroadcastMediaStateChanged(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(SyncMeetPageMediaUi));
        }

        private void SyncMeetPageMediaUi()
        {
            if (btnMeetPageSysAudio == null || btnMeetPageMic == null || btnMeetPageCam == null) return;
            _suppressMeetPageMediaEvents = true;
            try
            {
                var svc = LiveBroadcastMediaService.Instance;

                bool sysOn = svc.HasSystemAudioHardware && svc.IsSystemAudioEnabled;
                btnMeetPageSysAudio.IsEnabled = svc.HasSystemAudioHardware;
                btnMeetPageSysAudio.Content = !svc.HasSystemAudioHardware
                    ? "🔇 Không có loa"
                    : (sysOn ? "🔊 Âm thanh: BẬT" : "🔈 Âm thanh: TẮT");
                btnMeetPageSysAudio.Background = new SolidColorBrush(!svc.HasSystemAudioHardware
                    ? Color.FromRgb(0x30, 0x31, 0x34)
                    : (sysOn ? Color.FromRgb(0x1A, 0x73, 0xE8) : Color.FromRgb(0xEA, 0x43, 0x35)));

                bool micOn = svc.HasMicrophoneHardware && svc.IsMicrophoneEnabled;
                btnMeetPageMic.IsEnabled = svc.HasMicrophoneHardware;
                btnMeetPageMic.Content = !svc.HasMicrophoneHardware
                    ? "🚫 Không Mic"
                    : (micOn ? "🎙️ Micro: BẬT" : "🔇 Micro: TẮT");
                btnMeetPageMic.Background = new SolidColorBrush(!svc.HasMicrophoneHardware
                    ? Color.FromRgb(0x30, 0x31, 0x34)
                    : (micOn ? Color.FromRgb(0x3C, 0x40, 0x43) : Color.FromRgb(0xEA, 0x43, 0x35)));

                cmbMeetPageMic.ItemsSource = svc.AvailableMicrophones;
                cmbMeetPageMic.IsEnabled = svc.HasMicrophoneHardware;
                if (svc.HasMicrophoneHardware)
                {
                    cmbMeetPageMic.SelectedItem = svc.AvailableMicrophones.FirstOrDefault(m => m.DeviceId == svc.SelectedMicrophoneId)
                                               ?? svc.AvailableMicrophones.FirstOrDefault();
                }

                bool camOn = svc.HasCameraHardware && svc.IsCameraEnabled;
                btnMeetPageCam.IsEnabled = svc.HasCameraHardware || svc.IsScanningCameras;
                btnMeetPageCam.Content = svc.IsScanningCameras && !svc.HasCameraHardware
                    ? "📷 Đang dò..."
                    : (!svc.HasCameraHardware ? "🚫 Không Cam" : (camOn ? "📷 Camera: BẬT" : "🚫 Camera: TẮT"));
                btnMeetPageCam.Background = new SolidColorBrush(!svc.HasCameraHardware && !svc.IsScanningCameras
                    ? Color.FromRgb(0x30, 0x31, 0x34)
                    : (camOn ? Color.FromRgb(0x3C, 0x40, 0x43) : Color.FromRgb(0xEA, 0x43, 0x35)));

                cmbMeetPageCam.ItemsSource = svc.AvailableCameras;
                cmbMeetPageCam.IsEnabled = svc.HasCameraHardware;
                if (svc.HasCameraHardware)
                {
                    cmbMeetPageCam.SelectedItem = svc.AvailableCameras.FirstOrDefault(c => c.DeviceId == svc.SelectedCameraId)
                                               ?? svc.AvailableCameras.FirstOrDefault();
                }
            }
            finally
            {
                _suppressMeetPageMediaEvents = false;
            }
        }

        private void MeetPageSysAudio_Click(object sender, RoutedEventArgs e)
        {
            var svc = LiveBroadcastMediaService.Instance;
            svc.SetSystemAudioEnabled(!svc.IsSystemAudioEnabled);
            SyncMeetPageMediaUi();
        }

        private void MeetPageMic_Click(object sender, RoutedEventArgs e)
        {
            var svc = LiveBroadcastMediaService.Instance;
            svc.SetMicrophoneEnabled(!svc.IsMicrophoneEnabled);
            SyncMeetPageMediaUi();
        }

        private async void MeetPageCam_Click(object sender, RoutedEventArgs e)
        {
            var svc = LiveBroadcastMediaService.Instance;
            await svc.SetCameraEnabledAsync(!svc.IsCameraEnabled);
            SyncMeetPageMediaUi();
        }

        private void CmbMeetPageMic_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressMeetPageMediaEvents) return;
            if (cmbMeetPageMic.SelectedItem is AudioEndpointOption opt)
            {
                LiveBroadcastMediaService.Instance.SelectMicrophone(opt.DeviceId);
            }
        }

        private async void CmbMeetPageCam_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressMeetPageMediaEvents) return;
            if (cmbMeetPageCam.SelectedItem is CameraEndpointOption cam)
            {
                await LiveBroadcastMediaService.Instance.SelectCameraAsync(cam.DeviceId);
            }
        }
    }
}
