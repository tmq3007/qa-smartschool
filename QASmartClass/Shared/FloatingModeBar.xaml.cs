using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using QASmartClass.Classroom.Helpers;
using QASmartClass.Classroom.Services;
using Serilog;

namespace QASmartClass.Shared
{
    public partial class FloatingModeBar : Window
    {
        private readonly ModeService _modeService;
        private readonly UserRoleService? _roleService;
        private bool _isVoiceBroadcasting = false;
        private bool _isRecording = false;
        private bool _isBroadcasting = false;
        private DateTime _lastQuickActionUtc = DateTime.MinValue;
        private readonly System.Windows.Threading.DispatcherTimer _quickStatusSyncTimer;

        #region Win32 Native Drag Constants & APIs

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;
        private const uint WDA_NONE = 0x00;
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x11;

        #endregion

        public FloatingModeBar(ModeService modeService, UserRoleService? roleService = null)
        {
            InitializeComponent();

            _modeService = modeService;
            _roleService = roleService;
            _modeService.ModeChanged += OnModeChanged;

            // Đặt vị trí mặc định ở phía DƯỚI màn hình (trên Taskbar)
            PositionAtBottom();

            // SmartTouchOnly / Guest: ẩn nút Smart Class + các nút chức năng quản lý lớp học
            //if (_roleService?.IsSmartTouchOnly == true || _roleService?.CurrentRole == UserRole.Guest)
            //{
            //    btnClass.Visibility = Visibility.Collapsed;
            //    btnVoice.Visibility = Visibility.Collapsed;
            //    btnBroadcast.Visibility = Visibility.Collapsed;
            //    Log.Information("FloatingModeBar: SmartTouchOnly/Guest mode — " +
            //        "btnClass, btnVoice, btnBroadcast COLLAPSED");
            //}

            // Keyboard shortcuts: Ctrl+1 = Class, Ctrl+2 = Screen, Ctrl+3 = Desktop
            InputBindings.Add(new KeyBinding(new RelayCommand(() =>
            {
                if (_modeService.IsTransitioning) return;
                if (_roleService?.IsSmartTouchOnly != true && _roleService?.CurrentRole != UserRole.Guest)
                    _ = _modeService.GoToClassAsync();
            }),
                new KeyGesture(Key.D1, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(new RelayCommand(() =>
            {
                if (_modeService.IsTransitioning) return;
                _ = _modeService.GoToScreenAsync();
            }),
                new KeyGesture(Key.D2, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(new RelayCommand(() =>
            {
                if (_modeService.IsTransitioning) return;
                _ = _modeService.GoToDesktopAsync();
            }),
                new KeyGesture(Key.D3, ModifierKeys.Control)));

            UpdateUI(_modeService.CurrentMode);

            if (_modeService.CurrentMode == AppMode.SmartScreen)
            {
                CollapseBar();
                PositionAtTopRight();
            }
            else
            {
                ExpandBar();
                PositionAtBottom();
            }

            Log.Information("FloatingModeBar initialized (Mode: {Mode}) - Smooth Native Drag enabled", _modeService.CurrentMode);
            
            // Giữ thanh nổi luôn hiển thị trên cùng khi ở chế độ DESKTOP hoặc đang phát trực tiếp bài giảng
            Application.Current.Activated += (s, e) => { this.Topmost = true; };
            Application.Current.Deactivated += (s, e) =>
            {
                if (_modeService.CurrentMode != AppMode.Desktop && !ScreenShareController.Instance.IsLive)
                {
                    this.Topmost = false;
                }
            };

            SourceInitialized += (_, _) => ApplyCaptureExclusionForLiveState(ScreenShareController.Instance.IsLive);
            ScreenShareController.Instance.LiveChanged += OnScreenShareLiveChanged;
            Services.BroadcastStateService.Instance.StateChanged += OnScreenShareLiveChanged;
            QASmartTouch.Services.ScreenRecorderService.Instance.SaveCompleted += OnRecorderSaveCompleted;

            _quickStatusSyncTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.2)
            };
            _quickStatusSyncTimer.Tick += (_, _) =>
            {
                SyncBroadcastVisualState();
                SyncRecorderVisualState();
            };
            _quickStatusSyncTimer.Start();

            Closed += (_, _) =>
            {
                _quickStatusSyncTimer.Stop();
                ScreenShareController.Instance.LiveChanged -= OnScreenShareLiveChanged;
                Services.BroadcastStateService.Instance.StateChanged -= OnScreenShareLiveChanged;
                QASmartTouch.Services.ScreenRecorderService.Instance.SaveCompleted -= OnRecorderSaveCompleted;
            };

            SyncBroadcastVisualState();
            SyncRecorderVisualState();
        }

        /// <summary>
        /// Đặt vị trí thanh công cụ ở phía dưới màn hình (căn giữa)
        /// Thường áp dụng cho chế độ SMART CLASS và DESKTOP
        /// </summary>
        public void PositionAtBottom()
        {
            try
            {
                var workArea = SystemParameters.WorkArea;
                var screenW = SystemParameters.PrimaryScreenWidth;
                UpdateLayout();
                var targetWidth = (pnlExpanded.Visibility == Visibility.Visible)
                    ? Math.Max(this.ActualWidth, 728)
                    : (this.ActualWidth > 0 ? this.ActualWidth : 200);
                var targetHeight = (pnlExpanded.Visibility == Visibility.Visible)
                    ? Math.Max(this.ActualHeight, 62)
                    : (this.ActualHeight > 0 ? this.ActualHeight : 56);

                Left = (screenW - targetWidth) / 2;
                Top = Math.Max(20, workArea.Bottom - targetHeight - 12);

                if (txtExpandArrow != null)
                {
                    txtExpandArrow.Text = "▲";
                }

                EnsureWithinScreenBounds();
            }
            catch (Exception ex)
            {
                Log.Warning("PositionAtBottom error: {Err}", ex.Message);
            }
        }

        /// <summary>
        /// Đặt vị trí thanh công cụ ở phía trên bên phải màn hình (vùng Header của Bảng vẽ)
        /// Tránh che khuất thanh công cụ vẽ (panelTools) ở đáy bảng trong chế độ SMART TOUCH
        /// </summary>
        public void PositionAtTopRight()
        {
            try
            {
                var workArea = SystemParameters.WorkArea;
                var screenW = SystemParameters.PrimaryScreenWidth;
                UpdateLayout();
                var targetWidth = (pnlExpanded.Visibility == Visibility.Visible)
                    ? Math.Max(this.ActualWidth, 728)
                    : (this.ActualWidth > 0 ? this.ActualWidth : 200);

                Left = screenW - targetWidth - 20;
                Top = Math.Max(4, workArea.Top + 3);

                if (txtExpandArrow != null)
                {
                    txtExpandArrow.Text = "▼";
                }

                EnsureWithinScreenBounds();
            }
            catch (Exception ex)
            {
                Log.Warning("PositionAtTopRight error: {Err}", ex.Message);
            }
        }

        #region Smooth Native Drag & Bounds Management

        private void DragHandle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                StartNativeDrag();
            }
        }

        private void OnDragBar(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                StartNativeDrag();
            }
        }

        private void StartNativeDrag()
        {
            try
            {
                var helper = new WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    ReleaseCapture();
                    SendMessage(helper.Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                    EnsureWithinScreenBounds();
                }
            }
            catch (Exception ex)
            {
                Log.Debug("StartNativeDrag fallback: {Err}", ex.Message);
                try { DragMove(); EnsureWithinScreenBounds(); } catch { }
            }
        }

        private void EnsureWithinScreenBounds()
        {
            try
            {
                var virtualLeft = SystemParameters.VirtualScreenLeft;
                var virtualTop = SystemParameters.VirtualScreenTop;
                var virtualWidth = SystemParameters.VirtualScreenWidth;
                var virtualHeight = SystemParameters.VirtualScreenHeight;

                var currentWidth = (pnlExpanded.Visibility == Visibility.Visible)
                    ? Math.Max(this.ActualWidth, 728)
                    : (this.ActualWidth > 0 ? this.ActualWidth : 200);
                var currentHeight = (pnlExpanded.Visibility == Visibility.Visible)
                    ? Math.Max(this.ActualHeight, 62)
                    : (this.ActualHeight > 0 ? this.ActualHeight : 56);

                if (this.Left < virtualLeft)
                    this.Left = virtualLeft;
                if (this.Top < virtualTop)
                    this.Top = virtualTop;
                if (this.Left + currentWidth > virtualLeft + virtualWidth)
                    this.Left = virtualLeft + virtualWidth - currentWidth;
                if (this.Top + currentHeight > virtualTop + virtualHeight)
                    this.Top = virtualTop + virtualHeight - currentHeight;
            }
            catch { }
        }

        #endregion

        #region Collapse / Expand Handlers

        public void CollapseBar()
        {
            pnlExpanded.Visibility = Visibility.Collapsed;
            pnlCollapsed.Visibility = Visibility.Visible;
            UpdateLayout();
            if (_modeService?.CurrentMode == AppMode.SmartScreen)
            {
                PositionAtTopRight();
            }
            else
            {
                EnsureWithinScreenBounds();
            }
        }

        public void ExpandBar()
        {
            pnlCollapsed.Visibility = Visibility.Collapsed;
            pnlExpanded.Visibility = Visibility.Visible;
            UpdateLayout();
            EnsureWithinScreenBounds();
        }

        private void OnCollapseClick(object sender, MouseButtonEventArgs e)
        {
            CollapseBar();
            Log.Debug("FloatingModeBar collapsed");
        }

        private void btnCollapseBar_PreviewTouchDown(object? sender, TouchEventArgs e)
        {
            CollapseBar();
            e.Handled = true;
            Log.Debug("FloatingModeBar collapsed via touch");
        }

        private void btnExpandModeBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ExpandBar();
            Log.Debug("FloatingModeBar expanded instantly");
        }

        private void btnExpandModeBar_PreviewTouchDown(object? sender, TouchEventArgs e)
        {
            ExpandBar();
            e.Handled = true;
            Log.Debug("FloatingModeBar expanded instantly via touch");
        }

        #endregion

        #region Mode Switch Handlers

        private async void OnClassClick(object sender, MouseButtonEventArgs e)
        {
            if (_modeService.IsTransitioning) return;

            if (_roleService?.IsSmartTouchOnly == true || _roleService?.CurrentRole == UserRole.Guest)
            {
                Log.Debug("SmartTouchOnly: blocked Smart Class switch");
                return;
            }
            ApplyPressedFeedback(btnClass);
            await _modeService.GoToClassAsync();
        }

        private async void OnScreenClick(object sender, MouseButtonEventArgs e)
        {
            if (_modeService.IsTransitioning) return;
            ApplyPressedFeedback(btnScreen);
            await _modeService.GoToScreenAsync();
        }

        private async void OnDesktopClick(object sender, MouseButtonEventArgs e)
        {
            if (_modeService.IsTransitioning) return;
            ApplyPressedFeedback(btnDesktop);
            await _modeService.GoToDesktopAsync();
        }

        private static void ApplyPressedFeedback(System.Windows.Controls.Border button)
        {
            try
            {
                var scaleTransform = new ScaleTransform(1, 1);
                button.RenderTransform = scaleTransform;
                button.RenderTransformOrigin = new Point(0.5, 0.5);

                var scaleXAnim = new System.Windows.Media.Animation.DoubleAnimation(
                    1.0, 0.95, TimeSpan.FromMilliseconds(50))
                {
                    AutoReverse = true
                };
                var scaleYAnim = new System.Windows.Media.Animation.DoubleAnimation(
                    1.0, 0.95, TimeSpan.FromMilliseconds(50))
                {
                    AutoReverse = true
                };
                var opacityAnim = new System.Windows.Media.Animation.DoubleAnimation(
                    1.0, 0.7, TimeSpan.FromMilliseconds(50))
                {
                    AutoReverse = true
                };

                scaleTransform.BeginAnimation(
                    ScaleTransform.ScaleXProperty, scaleXAnim);
                scaleTransform.BeginAnimation(
                    ScaleTransform.ScaleYProperty, scaleYAnim);
                button.BeginAnimation(OpacityProperty, opacityAnim);
            }
            catch (Exception ex)
            {
                Log.Debug("ApplyPressedFeedback error: {Err}", ex.Message);
            }
        }

        #endregion

        #region Quick Action Handlers

        private bool AcquireQuickActionDebounce(int debounceMs = 180)
        {
            var now = DateTime.UtcNow;
            if ((now - _lastQuickActionUtc).TotalMilliseconds < debounceMs)
                return false;
            _lastQuickActionUtc = now;
            return true;
        }

        private void ApplyCaptureExclusionForLiveState(bool excludeFromCapture)
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    SetWindowDisplayAffinity(hwnd, excludeFromCapture ? WDA_EXCLUDEFROMCAPTURE : WDA_NONE);
                }
            }
            catch { }
        }

        private void OnScreenShareLiveChanged(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(SyncBroadcastVisualState));
        }

        private void OnRecorderSaveCompleted(string? filePath, string? error)
        {
            Dispatcher.BeginInvoke(new Action(SyncRecorderVisualState));
        }

        private void SyncBroadcastVisualState()
        {
            try
            {
                bool isLive = ScreenShareController.Instance.IsLive;
                if (_isBroadcasting != isLive)
                {
                    ApplyCaptureExclusionForLiveState(isLive);
                    if (isLive) this.Topmost = true;
                }
                _isBroadcasting = isLive;

                if (bdrBroadcastGroup != null && txtBroadcast != null && txtBroadcastCaret != null && sepBroadcastSplit != null)
                {
                    if (isLive)
                    {
                        bdrBroadcastGroup.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)); // #DC2626
                        bdrBroadcastGroup.BorderBrush = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                        txtBroadcast.Text = "🔴 LIVE";
                        txtBroadcast.FontSize = 10.5;
                        txtBroadcast.Foreground = Brushes.White;
                        txtBroadcastCaret.Foreground = Brushes.White;
                        sepBroadcastSplit.Background = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255));
                        btnBroadcast.ToolTip = "Đang chia sẻ màn hình trực tiếp — Nhấn để điều khiển / đổi nguồn / dừng chiếu";
                    }
                    else
                    {
                        bdrBroadcastGroup.Background = new SolidColorBrush(Color.FromRgb(220, 227, 249)); // #DCE3F9
                        bdrBroadcastGroup.BorderBrush = new SolidColorBrush(Color.FromRgb(159, 168, 218));
                        txtBroadcast.Text = "📺";
                        txtBroadcast.FontSize = 14;
                        txtBroadcast.Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                        txtBroadcastCaret.Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85));
                        sepBroadcastSplit.Background = new SolidColorBrush(Color.FromRgb(159, 168, 218));
                        btnBroadcast.ToolTip = "Chia sẻ màn hình & Quảng bá bài giảng (Bấm 📺 chọn nhanh màn hình, bấm ▾ để mở đầy đủ chế độ)";
                    }
                }

                if (bdrMiniLiveBadge != null)
                {
                    bdrMiniLiveBadge.Visibility = isLive ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Log.Debug("[FloatingModeBar] SyncBroadcastVisualState error: {Err}", ex.Message);
            }
        }

        private void SyncRecorderVisualState()
        {
            try
            {
                bool active = QASmartTouch.Services.ScreenRecorderService.Instance.IsSessionActive;
                _isRecording = active;
                if (btnRecord != null && txtRecord != null)
                {
                    btnRecord.Background = active
                        ? new SolidColorBrush(Color.FromRgb(211, 47, 47))
                        : Brushes.Transparent;
                    txtRecord.Foreground = active ? Brushes.White : Brushes.Black;
                    txtRecord.Text = active ? "🔴" : "⏺️";
                    btnRecord.ToolTip = active
                        ? "Đang ghi hình bài giảng — Nhấn để mở thanh điều khiển ghi hình"
                        : "Ghi hình bài giảng & hoạt động trình chiếu";
                }
            }
            catch { }
        }

        /// <summary>Hiển thị / Ẩn cửa sổ chính</summary>
        private void OnShowHideClick(object sender, MouseButtonEventArgs e)
        {
            if (!AcquireQuickActionDebounce()) return;
            ExecuteShowHideToggle();
        }

        private void OnShowHideTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            ExecuteShowHideToggle();
        }

        private void ExecuteShowHideToggle()
        {
            try
            {
                ApplyPressedFeedback(btnShowHide);
                var app = (QASmartTouch.App)Application.Current;
                Window? targetWindow = null;

                if (app.ModeService.CurrentMode == AppMode.SmartClass)
                {
                    targetWindow = app._classroomShell;
                }
                else if (app.ModeService.CurrentMode == AppMode.SmartScreen)
                {
                    targetWindow = app._whiteboardShell;
                }

                if (targetWindow == null)
                {
                    foreach (Window w in app.Windows)
                    {
                        var name = w.GetType().Name;
                        if (name.Contains("ClassroomShell") || name.Contains("MainDashboard") || name.Contains("Form2"))
                        {
                            targetWindow = w;
                            break;
                        }
                    }
                }

                if (targetWindow != null)
                {
                    if (targetWindow.Visibility == Visibility.Visible)
                    {
                        targetWindow.Hide();
                        txtShowHide.Text = "📲";
                        btnShowHide.ToolTip = "Hiển thị cửa sổ chính";
                        Log.Information("Main window {WindowName} hidden", targetWindow.GetType().Name);
                    }
                    else
                    {
                        targetWindow.Show();
                        targetWindow.Activate();
                        txtShowHide.Text = "🔲";
                        btnShowHide.ToolTip = "Ẩn cửa sổ chính";
                        Log.Information("Main window {WindowName} shown", targetWindow.GetType().Name);
                    }
                }
            }
            catch (Exception ex) { Log.Warning("ShowHide error: {Err}", ex.Message); }
        }

        /// <summary>Bắt đầu / Ngừng phát giọng nói cho HS</summary>
        private void OnVoiceClick(object sender, MouseButtonEventArgs e)
        {
            if (!AcquireQuickActionDebounce()) return;
            _ = ToggleVoiceBroadcastAsync();
        }

        private void OnVoiceTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            _ = ToggleVoiceBroadcastAsync();
        }

        private async System.Threading.Tasks.Task ToggleVoiceBroadcastAsync()
        {
            try
            {
                ApplyPressedFeedback(btnVoice);
                var app = (QASmartTouch.App)Application.Current;
                if (!app.EnsureClassroomBroadcastEngineReady()) return;

                _isVoiceBroadcasting = !_isVoiceBroadcasting;
                var net = ClassroomAppContext.Network ?? app.NetworkService;

                if (net != null && net.IsBroadcasting)
                {
                    string stateFlag = _isVoiceBroadcasting ? "1" : "0";
                    await net.SendCommandAsync($"CMD|VOICE_BROADCAST|{stateFlag}");
                    await net.SendCommandAsync($"VOICE_BROADCAST|enabled={_isVoiceBroadcasting}");
                }

                btnVoice.Background = _isVoiceBroadcasting
                    ? new SolidColorBrush(Color.FromRgb(22, 163, 74))
                    : Brushes.Transparent;
                txtVoice.Foreground = _isVoiceBroadcasting ? Brushes.White : Brushes.Black;
                btnVoice.ToolTip = _isVoiceBroadcasting ? "Đang phát giọng nói tới cả lớp — Nhấn để dừng" : "Bắt đầu phát giọng nói tới cả lớp";

                try
                {
                    app.Database.EventLogs.Add(new Data.EventLog
                    {
                        EventType = _isVoiceBroadcasting ? "VOICE_ON" : "VOICE_OFF",
                        Actor = "GV",
                        Details = _isVoiceBroadcasting ? "Bắt đầu phát giọng nói" : "Dừng phát giọng nói",
                        Timestamp = DateTime.Now
                    });
                    app.Database.SaveChanges();
                }
                catch { }
                Log.Information("Voice broadcast: {State}", _isVoiceBroadcasting);
            }
            catch (Exception ex) { Log.Warning("Voice broadcast error: {Err}", ex.Message); }
        }

        /// <summary>Mở bút chú thích trên màn hình (chuyển sang SmartScreen)</summary>
        private void OnPenClick(object sender, MouseButtonEventArgs e)
        {
            if (!AcquireQuickActionDebounce()) return;
            ExecutePenSwitch();
        }

        private void OnPenTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            ExecutePenSwitch();
        }

        private void ExecutePenSwitch()
        {
            try
            {
                ApplyPressedFeedback(btnPen);
                if (_modeService.CurrentMode != AppMode.SmartScreen)
                {
                    _ = _modeService.GoToScreenAsync();
                }
                else
                {
                    var app = Application.Current as QASmartTouch.App;
                    if (app?._whiteboardShell != null)
                    {
                        if (!app._whiteboardShell.IsVisible) app._whiteboardShell.Show();
                        app._whiteboardShell.Activate();
                    }
                }
                Log.Information("Pen annotation: switched/focused SmartScreen");
            }
            catch (Exception ex) { Log.Warning("Pen click error: {Err}", ex.Message); }
        }

        /// <summary>Ghi hình bài giảng thực tế qua ScreenRecorderService</summary>
        private void OnRecordClick(object sender, MouseButtonEventArgs e)
        {
            if (!AcquireQuickActionDebounce()) return;
            ExecuteRecordAction();
        }

        private void OnRecordTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            ExecuteRecordAction();
        }

        private void ExecuteRecordAction()
        {
            try
            {
                ApplyPressedFeedback(btnRecord);
                var recorder = QASmartTouch.Services.ScreenRecorderService.Instance;
                if (recorder.IsSessionActive)
                {
                    var existingBar = Application.Current.Windows
                        .OfType<QASmartTouch.Forms.FloatingRecorderBar>()
                        .FirstOrDefault();
                    if (existingBar != null)
                    {
                        existingBar.Show();
                        existingBar.Activate();
                    }
                    else
                    {
                        recorder.StopRecording();
                    }
                    SyncRecorderVisualState();
                    return;
                }

                var setupDialog = new QASmartTouch.Forms.PreRecordSetupDialog
                {
                    Topmost = true,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                setupDialog.ShowDialog();
                SyncRecorderVisualState();
            }
            catch (Exception ex)
            {
                Log.Warning("Record error: {Err}", ex.Message);
            }
        }

        private void OnBroadcastClick(object sender, MouseButtonEventArgs e)
        {
            if (!AcquireQuickActionDebounce()) return;
            HandlePrimaryBroadcastTrigger(bdrBroadcastGroup);
        }

        private void OnBroadcastTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            HandlePrimaryBroadcastTrigger(bdrBroadcastGroup);
        }

        private void OnBroadcastRightClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            OpenQuickBroadcastMenu(bdrBroadcastGroup);
        }

        private void OnBroadcastMenuClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            ApplyPressedFeedback(bdrBroadcastGroup);
            OpenQuickBroadcastMenu(bdrBroadcastGroup);
        }

        private void OnBroadcastMenuTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            ApplyPressedFeedback(bdrBroadcastGroup);
            OpenQuickBroadcastMenu(bdrBroadcastGroup);
        }

        private void OnMiniLiveBadgeClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            OpenQuickBroadcastMenu(bdrMiniLiveBadge);
        }

        private void OnMiniLiveBadgeTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            OpenQuickBroadcastMenu(bdrMiniLiveBadge);
        }

        private void HandlePrimaryBroadcastTrigger(FrameworkElement anchor)
        {
            try
            {
                ApplyPressedFeedback(anchor as Border ?? bdrBroadcastGroup);
                if (ScreenShareController.Instance.IsLive)
                {
                    // Khi đang chiếu trực tiếp: mở Menu điều khiển nhanh để GV đổi nguồn / tạm dừng / dừng chiếu an toàn, không sợ bấm nhầm
                    OpenQuickBroadcastMenu(anchor);
                    return;
                }

                DispatchBroadcastAction(QuickBroadcastAction.PickScreenOrWindow);
            }
            catch (Exception ex)
            {
                Log.Warning("HandlePrimaryBroadcastTrigger error: {Err}", ex.Message);
            }
        }

        private void DispatchBroadcastAction(QuickBroadcastAction action)
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (!app.EnsureClassroomBroadcastEngineReady()) return;

                ScreenShareController.Instance.RequestAction(action);
                SyncBroadcastVisualState();
            }
            catch (Exception ex)
            {
                Log.Warning("DispatchBroadcastAction({Action}) error: {Err}", action, ex.Message);
            }
        }

        private void OpenQuickBroadcastMenu(FrameworkElement placementTarget)
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (!app.EnsureClassroomBroadcastEngineReady()) return;

                bool isLive = ScreenShareController.Instance.IsLive;
                int onlineCount = ClassroomAppContext.Network?.ConnectedCount ?? 0;

                var menu = new ContextMenu
                {
                    PlacementTarget = placementTarget,
                    Placement = (_modeService.CurrentMode == AppMode.SmartScreen && Top < 120)
                        ? System.Windows.Controls.Primitives.PlacementMode.Bottom
                        : System.Windows.Controls.Primitives.PlacementMode.Top,
                    VerticalOffset = (_modeService.CurrentMode == AppMode.SmartScreen && Top < 120) ? 6 : -6,
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(199, 210, 254)),
                    BorderThickness = new Thickness(1.5),
                    Padding = new Thickness(6),
                    FontSize = 13.5
                };

                var headerItem = new MenuItem
                {
                    IsEnabled = false,
                    Header = new TextBlock
                    {
                        Text = isLive
                            ? $"🔴 ĐANG CHIẾU TRỰC TIẾP  •  {onlineCount} máy HS kết nối"
                            : $"📡 QUẢNG BÁ & CHIA SẺ MÀN HÌNH ({onlineCount} HS online)",
                        FontWeight = FontWeights.Bold,
                        FontSize = 12,
                        Foreground = isLive
                            ? new SolidColorBrush(Color.FromRgb(220, 38, 38))
                            : new SolidColorBrush(Color.FromRgb(30, 64, 175)),
                        Margin = new Thickness(2, 2, 2, 2)
                    }
                };
                menu.Items.Add(headerItem);
                menu.Items.Add(new Separator());

                MenuItem BuildTouchItem(string icon, string title, string subtitle, Action onClick, bool danger = false, bool accent = false)
                {
                    var sp = new StackPanel { Margin = new Thickness(2, 3, 6, 3) };
                    sp.Children.Add(new TextBlock
                    {
                        Text = $"{icon}  {title}",
                        FontWeight = (danger || accent) ? FontWeights.Bold : FontWeights.SemiBold,
                        FontSize = 13.5,
                        Foreground = danger
                            ? new SolidColorBrush(Color.FromRgb(220, 38, 38))
                            : accent
                                ? new SolidColorBrush(Color.FromRgb(29, 78, 216))
                                : new SolidColorBrush(Color.FromRgb(15, 23, 42))
                    });
                    if (!string.IsNullOrEmpty(subtitle))
                    {
                        sp.Children.Add(new TextBlock
                        {
                            Text = subtitle,
                            FontSize = 11,
                            Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                            Margin = new Thickness(22, 1, 0, 0)
                        });
                    }

                    var mi = new MenuItem
                    {
                        Header = sp,
                        MinHeight = 40,
                        Cursor = Cursors.Hand
                    };
                    mi.Click += (_, _) => onClick();
                    return mi;
                }

                if (isLive)
                {
                    menu.Items.Add(BuildTouchItem(
                        "⏹️", "Dừng chia sẻ màn hình ngay",
                        "Ngắt luồng phát màn hình trên toàn bộ máy Học sinh",
                        () =>
                        {
                            ScreenShareController.Instance.RequestStop();
                            SyncBroadcastVisualState();
                        },
                        danger: true));

                    menu.Items.Add(BuildTouchItem(
                        "⏸️", "Tạm dừng / Tiếp tục khung hình chiếu",
                        "Đóng băng tạm thời hình ảnh bên máy Học sinh để GV thao tác riêng tư",
                        () => DispatchBroadcastAction(QuickBroadcastAction.TogglePause)));

                    menu.Items.Add(BuildTouchItem(
                        "👁️", "Hiện thanh điều khiển & Xem trước nổi (PiP)",
                        "Mở cửa sổ nhỏ xem trực tiếp hình ảnh Học sinh đang nhìn thấy",
                        () => DispatchBroadcastAction(QuickBroadcastAction.ShowFloatingPip)));

                    menu.Items.Add(new Separator());

                    menu.Items.Add(BuildTouchItem(
                        "🔄", "Đổi sang Màn hình / Cửa sổ ứng dụng khác...",
                        "Chuyển mượt nguồn chiếu (PowerPoint, Chrome, Video) không ngắt kết nối",
                        () => DispatchBroadcastAction(QuickBroadcastAction.PickScreenOrWindow),
                        accent: true));

                    menu.Items.Add(BuildTouchItem(
                        "✂️", "Kéo chọn Vùng màn hình khác để chiếu...",
                        "Chỉ chiếu đúng khung hình chữ nhật GV khoanh chọn",
                        () => DispatchBroadcastAction(QuickBroadcastAction.PickRegion)));

                    menu.Items.Add(BuildTouchItem(
                        "🖊️", "Chuyển sang chiếu Bảng trắng SMART TOUCH",
                        "Truyền trực tiếp nét viết bảng tương tác 30/60 FPS siêu nét",
                        () => DispatchBroadcastAction(QuickBroadcastAction.ShareSmartTouchNow)));
                }
                else
                {
                    menu.Items.Add(BuildTouchItem(
                        "🖥️", "Chọn Màn hình / Cửa sổ ứng dụng để chiếu...",
                        "Hộp chọn trực quan chuẩn Google Meet (Toàn màn hình, PowerPoint, Trình duyệt...)",
                        () => DispatchBroadcastAction(QuickBroadcastAction.PickScreenOrWindow),
                        accent: true));

                    menu.Items.Add(BuildTouchItem(
                        "✂️", "Kéo chọn Vùng màn hình chiếu...",
                        "Khoanh vùng bất kỳ trên màn hình để chiếu riêng vùng đó cho HS",
                        () => DispatchBroadcastAction(QuickBroadcastAction.PickRegion)));

                    menu.Items.Add(BuildTouchItem(
                        "🖊️", "Chiếu ngay Bảng trắng SMART TOUCH (1 chạm)",
                        "Phát trực tiếp bảng viết tương tác cho cả lớp không cần rời bảng",
                        () => DispatchBroadcastAction(QuickBroadcastAction.ShareSmartTouchNow)));

                    menu.Items.Add(BuildTouchItem(
                        "📑", "Chiếu Trang bài giảng QA SmartClass...",
                        "Chọn trang nội bộ trong hệ thống để trình chiếu",
                        () => DispatchBroadcastAction(QuickBroadcastAction.PickQaPage)));
                }

                menu.Items.Add(new Separator());

                menu.Items.Add(BuildTouchItem(
                    "👨‍🎓", "Chia sẻ màn hình Học sinh cho cả lớp (Mẫu HS)",
                    "Chọn 1 máy Học sinh tiêu biểu để phát bài làm lên toàn lớp",
                    () => DispatchBroadcastAction(QuickBroadcastAction.OpenStudentDemo)));

                menu.Items.Add(BuildTouchItem(
                    "📢", "Gửi Thông báo nổi tới cả lớp...",
                    "Phát thông báo / dặn dò hiển thị tức thì lên màn hình Học sinh",
                    () => DispatchBroadcastAction(QuickBroadcastAction.OpenNoticeTab)));

                menu.Items.Add(BuildTouchItem(
                    "🌐", "Mở Website học tập đồng loạt...",
                    "Đẩy đường link bài học tự động mở trên máy tính & Tablet Học sinh",
                    () => DispatchBroadcastAction(QuickBroadcastAction.OpenWebsiteTab)));

                menu.Items.Add(new Separator());

                menu.Items.Add(BuildTouchItem(
                    "⚙️", "Mở Trung tâm Quảng bá (Chỉnh 30/60 FPS & Độ nét)",
                    "Tùy chỉnh chế độ Tự động / Siêu nét Chữ / Video 60fps & chọn Nhóm nhận",
                    () => DispatchBroadcastAction(QuickBroadcastAction.OpenBroadcastHub)));

                menu.IsOpen = true;
            }
            catch (Exception ex)
            {
                Log.Warning("OpenQuickBroadcastMenu error: {Err}", ex.Message);
            }
        }

        /* // [MYTHWARE_LITE] Giữ lại mã nguồn ghi hình / phát bảng cũ (1 FPS PNG) theo quy tắc bảo toàn lịch sử không xóa trắng
        private async void Legacy_OnRecordClick(object sender, MouseButtonEventArgs e)
        {
            _isRecording = !_isRecording;
        }
        private System.Windows.Threading.DispatcherTimer? _broadcastTimer;
        private int _broadcastTickCount = 0;
        private static void CaptureSmartScreenCanvas(QASmartTouch.App app) { }
        */

        private void OnCloseTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            ExecuteCloseBar();
        }

        private void OnCloseClick(object sender, MouseButtonEventArgs e)
        {
            if (!AcquireQuickActionDebounce()) return;
            ExecuteCloseBar();
        }

        private void ExecuteCloseBar()
        {
            bool isLiveShare = ScreenShareController.Instance.IsLive;
            bool isRec = QASmartTouch.Services.ScreenRecorderService.Instance.IsSessionActive;
            bool isTransmitting = isLiveShare || _isVoiceBroadcasting || isRec;
            
            string warningMsg = "Ẩn thanh công cụ nổi?\n\nBạn có thể mở lại bằng cách nhấn vào biểu tượng Công cụ nổi ở menu chính hoặc dùng tổ hợp phím Ctrl+Alt+M.";
            if (isTransmitting)
            {
                warningMsg = "⚠️ CẢNH BÁO AN NINH & RIÊNG TƯ:\n" +
                             "Hệ thống đang phát màn hình/giọng nói hoặc ghi hình trong nền.\n\n" +
                             "Bạn có muốn DỪNG TẤT CẢ các kết nối phát sóng này và ẩn thanh công cụ nổi không?\n" +
                             "(Chọn YES để dừng và ẩn, NO để ẩn và tiếp tục phát ngầm, CANCEL để hủy)";
            }

            var result = MessageBox.Show(warningMsg, "Xác nhận đóng thanh công cụ", 
                isTransmitting ? MessageBoxButton.YesNoCancel : MessageBoxButton.YesNo, 
                isTransmitting ? MessageBoxImage.Warning : MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (isLiveShare) ScreenShareController.Instance.RequestStop();
                if (_isVoiceBroadcasting) _ = ToggleVoiceBroadcastAsync();
                if (isRec) QASmartTouch.Services.ScreenRecorderService.Instance.StopRecording();

                Hide();
                Log.Information("FloatingModeBar safely hidden and stopped all background transmissions");
            }
            else if (result == MessageBoxResult.No)
            {
                if (isTransmitting)
                {
                    Hide();
                    Log.Information("FloatingModeBar hidden, transmissions remain running in background");
                }
                else
                {
                    Log.Debug("Close cancelled by user");
                }
            }
        }

        #endregion

        #region Mode UI Update

        private void OnModeChanged(object? sender, ModeChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateUI(e.NewMode);

                if (e.NewMode == AppMode.SmartScreen)
                {
                    // [Smart Touch UX] Tự động thu gọn thành Mini-Pill và đưa lên góc trên bên phải (khu vực Header)
                    // để hoàn toàn giải phóng không gian cho thanh công cụ vẽ (panelTools) ở đáy bảng
                    CollapseBar();
                    PositionAtTopRight();
                }
                else
                {
                    // Khi chuyển sang SMART CLASS hoặc DESKTOP: mở rộng và đưa xuống đáy màn hình
                    ExpandBar();
                    PositionAtBottom();
                }
            });
        }

        private void UpdateUI(AppMode mode)
        {
            var inactiveColor = Brushes.Transparent;

            var classColor   = new SolidColorBrush(Color.FromRgb(0x19, 0x76, 0xD2)); // #1976D2 Xanh
            var screenColor  = new SolidColorBrush(Color.FromRgb(0x5C, 0x6B, 0xC0)); // #5C6BC0 Indigo
            var desktopColor = new SolidColorBrush(Color.FromRgb(0x45, 0x5A, 0x64)); // #455A64 Xám

            btnClass.Background   = mode == AppMode.SmartClass  ? classColor   : inactiveColor;
            btnScreen.Background  = mode == AppMode.SmartScreen  ? screenColor  : inactiveColor;
            btnDesktop.Background = mode == AppMode.Desktop      ? desktopColor : inactiveColor;

            SetButtonTextColor(btnClass,   mode == AppMode.SmartClass);
            SetButtonTextColor(btnScreen,  mode == AppMode.SmartScreen);
            SetButtonTextColor(btnDesktop, mode == AppMode.Desktop);

            // Cập nhật trạng thái cho Mini Pill (khi thu gọn)
            if (txtMiniModeName != null && bdrMiniModeBadge != null)
            {
                switch (mode)
                {
                    case AppMode.SmartClass:
                        txtMiniModeName.Text = "📚 SMART CLASS";
                        bdrMiniModeBadge.Background = classColor;
                        break;
                    case AppMode.SmartScreen:
                        txtMiniModeName.Text = "🖊️ SMART TOUCH";
                        bdrMiniModeBadge.Background = screenColor;
                        break;
                    case AppMode.Desktop:
                    default:
                        txtMiniModeName.Text = "🔲 DESKTOP";
                        bdrMiniModeBadge.Background = desktopColor;
                        break;
                }
            }

            Log.Debug("FloatingModeBar updated: {Mode}", mode);
        }

        private static void SetButtonTextColor(System.Windows.Controls.Border border, bool isActive)
        {
            if (border.Child is System.Windows.Controls.StackPanel sp)
            {
                foreach (var child in sp.Children)
                {
                    if (child is System.Windows.Controls.TextBlock tb)
                    {
                        tb.Foreground = isActive
                             ? Brushes.White
                             : new SolidColorBrush(Color.FromRgb(55, 71, 79));
                    }
                }
            }
        }

        #endregion
    }
}
