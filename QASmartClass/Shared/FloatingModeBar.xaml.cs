using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using QASmartClass.Classroom.Helpers;
using QASmartClass.Classroom.Services;
using QASmartTouch.Services.Camera;
using Serilog;

namespace QASmartClass.Shared
{
    public partial class FloatingModeBar : Window
    {
        private readonly ModeService _modeService;
        private readonly UserRoleService? _roleService;

        private bool _isRecording;
        private bool _isBroadcasting;
        private bool _isBroadcastPaused;
        private bool _isLockedSource;
        private bool _allowStudentExit;
        private bool _isClassScreenLocked;
        private bool _isPipVisible;
        private string _openPopoverMode = "";
        private bool _suppressDeviceSelectionEvent;
        private bool _suppressHideShellEvent;
        private bool _suppressAllowExitEvent;
        private bool _isBroadcastBridgeBound;

        private Point _orbDragStartScreen;
        private Point _orbWindowStartPos;
        private double _orbAnchorLeft = -1;
        private double _orbAnchorTop = -1;
        private bool _isOrbMouseDown;
        private bool _isOrbTouchDown;
        private bool _hasOrbMoved;

        private DateTime _lastQuickActionUtc = DateTime.MinValue;
        private readonly DispatcherTimer _quickStatusSyncTimer;
        private readonly DispatcherTimer _mediaMeterTimer;
        private readonly DispatcherTimer _idleOrbDimTimer;

        // ═══ Sự kiện đồng bộ trực tiếp với BroadcastPage (Hợp nhất 1 Cục Tròn EasyTouch) ═══
        public event EventHandler? ShareCurrentInsteadRequested;
        public event EventHandler? ChangeSourceRequested;
        public event EventHandler? SelectRegionRequested;
        public event EventHandler? TogglePauseRequested;
        public event EventHandler? StopRequested;
        public event EventHandler? RestoreShellRequested;
        public event EventHandler<bool>? HideClassroomShellChanged;
        public event EventHandler<bool>? AllowStudentExitChanged;

        public bool IsBroadcastBridgeBound
        {
            get => _isBroadcastBridgeBound;
            set => _isBroadcastBridgeBound = value;
        }

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

            // Phím tắt nhanh: Ctrl+1 = SMART CLASS, Ctrl+2 = SMART TOUCH, Ctrl+3 = DESKTOP
            InputBindings.Add(new KeyBinding(new RelayCommand(() =>
            {
                if (_modeService.IsTransitioning) return;
                if (_roleService?.IsSmartTouchOnly != true && _roleService?.CurrentRole != UserRole.Guest)
                    _ = _modeService.GoToClassAsync();
            }), new KeyGesture(Key.D1, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(new RelayCommand(() =>
            {
                if (_modeService.IsTransitioning) return;
                _ = _modeService.GoToScreenAsync();
            }), new KeyGesture(Key.D2, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(new RelayCommand(() =>
            {
                if (_modeService.IsTransitioning) return;
                _ = _modeService.GoToDesktopAsync();
            }), new KeyGesture(Key.D3, ModifierKeys.Control)));

            // Mặc định luôn khởi tạo dưới dạng 1 Cục Tròn EasyTouch gọn gàng ở mép phải màn hình
            CollapseBar();
            PositionOrbDefaultEdge();
            UpdateUI(_modeService.CurrentMode);

            Application.Current.Activated += (_, _) => { Topmost = true; };
            Application.Current.Deactivated += (_, _) =>
            {
                if (_modeService.CurrentMode != AppMode.Desktop && !ScreenShareController.Instance.IsLive)
                {
                    Topmost = true; // Giữ nút tròn EasyTouch luôn nổi gọn trên ứng dụng
                }
            };

            Deactivated += (_, _) =>
            {
                // Chuẩn EasyTouch / AssistiveTouch: Khi Giáo viên bấm ra ngoài để giảng bài / viết bảng thì tự thu về cục tròn
                if (pnlExpanded.Visibility == Visibility.Visible && string.IsNullOrEmpty(_openPopoverMode) && !_isPipVisible)
                {
                    CollapseBar();
                }
            };

            SourceInitialized += (_, _) =>
            {
                // Luôn bật cờ chống lọt hình vào luồng chụp màn hình để Học sinh không bao giờ thấy cục tròn của Giáo viên
                ApplyCaptureExclusionForLiveState(true);
            };

            EventHandler mediaStateHandler = (_, _) => Dispatcher.BeginInvoke(new Action(SyncMediaControlsState));
            EventHandler<string> mediaWarnHandler = (_, msg) => Dispatcher.BeginInvoke(new Action(() => ShowHardwareWarning(msg)));

            ScreenShareController.Instance.LiveChanged += OnScreenShareLiveChanged;
            Services.BroadcastStateService.Instance.StateChanged += OnScreenShareLiveChanged;
            QASmartTouch.Services.ScreenRecorderService.Instance.SaveCompleted += OnRecorderSaveCompleted;
            LiveBroadcastMediaService.Instance.StateChanged += mediaStateHandler;
            LiveBroadcastMediaService.Instance.HardwareWarning += mediaWarnHandler;

            _quickStatusSyncTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.0) };
            _quickStatusSyncTimer.Tick += (_, _) =>
            {
                SyncBroadcastVisualState();
                SyncRecorderVisualState();
            };
            _quickStatusSyncTimer.Start();

            _mediaMeterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
            _mediaMeterTimer.Tick += OnMediaMeterTimerTick;
            _mediaMeterTimer.Start();

            _idleOrbDimTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
            _idleOrbDimTimer.Tick += (_, _) =>
            {
                _idleOrbDimTimer.Stop();
                if (pnlCollapsed.Visibility == Visibility.Visible && !_isOrbMouseDown && !_isOrbTouchDown)
                {
                    pnlCollapsed.Opacity = _isBroadcasting ? 0.88 : 0.78;
                }
            };
            _idleOrbDimTimer.Start();

            Closed += (_, _) =>
            {
                _quickStatusSyncTimer.Stop();
                _mediaMeterTimer.Stop();
                _idleOrbDimTimer.Stop();
                ScreenShareController.Instance.LiveChanged -= OnScreenShareLiveChanged;
                Services.BroadcastStateService.Instance.StateChanged -= OnScreenShareLiveChanged;
                QASmartTouch.Services.ScreenRecorderService.Instance.SaveCompleted -= OnRecorderSaveCompleted;
                LiveBroadcastMediaService.Instance.StateChanged -= mediaStateHandler;
                LiveBroadcastMediaService.Instance.HardwareWarning -= mediaWarnHandler;
            };

            SyncMediaControlsState();
            SyncBroadcastVisualState();
            SyncRecorderVisualState();
            UpdateOrbAppearance();

            Log.Information("Unified EasyTouch FloatingModeBar initialized (Mode: {Mode})", _modeService.CurrentMode);
        }

        #region Positioning, Orb Gesture & Edge-Snap

        private void PositionOrbDefaultEdge()
        {
            try
            {
                var workArea = SystemParameters.WorkArea;
                _orbAnchorLeft = Math.Max(workArea.Left + 12, workArea.Right - 82);
                _orbAnchorTop = Math.Max(workArea.Top + 72, workArea.Top + workArea.Height * 0.18);
                Left = _orbAnchorLeft;
                Top = _orbAnchorTop;
            }
            catch (Exception ex)
            {
                Log.Debug("PositionOrbDefaultEdge error: {Err}", ex.Message);
            }
        }

        public void PositionAtBottom()
        {
            // [MYTHWARE_LITE] Giữ hàm tương thích API nhưng không còn trải dài dưới đáy màn hình; giữ nguyên vị trí cục tròn EasyTouch
            if (_orbAnchorLeft < 0 || _orbAnchorTop < 0)
            {
                PositionOrbDefaultEdge();
            }
            else if (pnlCollapsed.Visibility == Visibility.Visible)
            {
                EnsureWithinScreenBounds();
            }
        }

        public void PositionAtTopRight()
        {
            if (_orbAnchorLeft < 0 || _orbAnchorTop < 0)
            {
                PositionOrbDefaultEdge();
            }
            else if (pnlCollapsed.Visibility == Visibility.Visible)
            {
                EnsureWithinScreenBounds();
            }
        }

        private void SnapOrbToNearestScreenEdge()
        {
            try
            {
                var workArea = SystemParameters.WorkArea;
                double orbW = ActualWidth > 0 ? ActualWidth : 74;
                double orbH = ActualHeight > 0 ? ActualHeight : 74;
                double centerX = Left + orbW * 0.5;
                double midScreenX = workArea.Left + workArea.Width * 0.5;

                Left = (centerX < midScreenX)
                    ? workArea.Left + 8
                    : Math.Max(workArea.Left + 8, workArea.Right - orbW - 8);
                Top = Math.Max(workArea.Top + 8, Math.Min(Top, workArea.Bottom - orbH - 8));

                _orbAnchorLeft = Left;
                _orbAnchorTop = Top;
            }
            catch { }
        }

        private void EnsureWithinScreenBounds()
        {
            try
            {
                var workArea = SystemParameters.WorkArea;
                UpdateLayout();
                double curW = ActualWidth > 0 ? ActualWidth : (pnlExpanded.Visibility == Visibility.Visible ? 412 : 74);
                double curH = ActualHeight > 0 ? ActualHeight : (pnlExpanded.Visibility == Visibility.Visible ? 320 : 74);

                Left = Math.Max(workArea.Left + 6, Math.Min(Left, workArea.Right - curW - 6));
                Top = Math.Max(workArea.Top + 6, Math.Min(Top, workArea.Bottom - curH - 6));
            }
            catch { }
        }

        private void DragHandle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    var helper = new WindowInteropHelper(this);
                    if (helper.Handle != IntPtr.Zero)
                    {
                        ReleaseCapture();
                        SendMessage(helper.Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                    }
                    else
                    {
                        DragMove();
                    }
                    EnsureWithinScreenBounds();
                    var workArea = SystemParameters.WorkArea;
                    double curW = ActualWidth > 0 ? ActualWidth : 412;
                    _orbAnchorLeft = (Left + curW * 0.5 > workArea.Left + workArea.Width * 0.5)
                        ? Math.Max(workArea.Left + 8, workArea.Right - 74 - 8)
                        : workArea.Left + 8;
                    _orbAnchorTop = Top;
                }
                catch
                {
                    try { DragMove(); EnsureWithinScreenBounds(); } catch { }
                }
            }
        }

        private void Orb_MouseEnter(object sender, MouseEventArgs e)
        {
            _idleOrbDimTimer.Stop();
            pnlCollapsed.Opacity = 1.0;
        }

        private void Orb_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isOrbMouseDown && !_isOrbTouchDown)
            {
                _idleOrbDimTimer.Stop();
                _idleOrbDimTimer.Start();
            }
        }

        private void Orb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null) return;
            try
            {
                _idleOrbDimTimer.Stop();
                pnlCollapsed.Opacity = 1.0;
                _orbDragStartScreen = PointToScreen(e.GetPosition(this));
                _orbWindowStartPos = new Point(Left, Top);
                _isOrbMouseDown = true;
                _hasOrbMoved = false;
                pnlCollapsed.CaptureMouse();
                e.Handled = true;
            }
            catch { }
        }

        private void Orb_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isOrbMouseDown) return;
            try
            {
                var curScreen = PointToScreen(e.GetPosition(this));
                var dpi = VisualTreeHelper.GetDpi(this);
                double dx = (curScreen.X - _orbDragStartScreen.X) / dpi.DpiScaleX;
                double dy = (curScreen.Y - _orbDragStartScreen.Y) / dpi.DpiScaleY;
                if (Math.Sqrt(dx * dx + dy * dy) >= 6.0)
                {
                    _hasOrbMoved = true;
                    Left = _orbWindowStartPos.X + dx;
                    Top = _orbWindowStartPos.Y + dy;
                    EnsureWithinScreenBounds();
                }
                e.Handled = true;
            }
            catch { }
        }

        private void Orb_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isOrbMouseDown) return;
            bool moved = _hasOrbMoved;
            _isOrbMouseDown = false;
            _hasOrbMoved = false;
            pnlCollapsed.ReleaseMouseCapture();
            e.Handled = true;

            if (!moved)
            {
                ExpandBar();
            }
            else
            {
                SnapOrbToNearestScreenEdge();
                _idleOrbDimTimer.Stop();
                _idleOrbDimTimer.Start();
            }
        }

        private void Orb_TouchDown(object? sender, TouchEventArgs e)
        {
            try
            {
                _idleOrbDimTimer.Stop();
                pnlCollapsed.Opacity = 1.0;
                _orbDragStartScreen = PointToScreen(e.GetTouchPoint(this).Position);
                _orbWindowStartPos = new Point(Left, Top);
                _isOrbTouchDown = true;
                _hasOrbMoved = false;
                pnlCollapsed.CaptureTouch(e.TouchDevice);
                e.Handled = true;
            }
            catch { }
        }

        private void Orb_TouchMove(object? sender, TouchEventArgs e)
        {
            if (!_isOrbTouchDown) return;
            try
            {
                var curScreen = PointToScreen(e.GetTouchPoint(this).Position);
                var dpi = VisualTreeHelper.GetDpi(this);
                double dx = (curScreen.X - _orbDragStartScreen.X) / dpi.DpiScaleX;
                double dy = (curScreen.Y - _orbDragStartScreen.Y) / dpi.DpiScaleY;
                if (Math.Sqrt(dx * dx + dy * dy) >= 8.0)
                {
                    _hasOrbMoved = true;
                    Left = _orbWindowStartPos.X + dx;
                    Top = _orbWindowStartPos.Y + dy;
                    EnsureWithinScreenBounds();
                }
                e.Handled = true;
            }
            catch { }
        }

        private void Orb_TouchUp(object? sender, TouchEventArgs e)
        {
            if (!_isOrbTouchDown) return;
            bool moved = _hasOrbMoved;
            _isOrbTouchDown = false;
            _hasOrbMoved = false;
            pnlCollapsed.ReleaseTouchCapture(e.TouchDevice);
            e.Handled = true;

            if (!moved)
            {
                ExpandBar();
            }
            else
            {
                SnapOrbToNearestScreenEdge();
                _idleOrbDimTimer.Stop();
                _idleOrbDimTimer.Start();
            }
        }

        #endregion

        #region Collapse / Expand Hub

        public void CollapseBar()
        {
            _openPopoverMode = "";
            if (bdrDevicePopover != null) bdrDevicePopover.Visibility = Visibility.Collapsed;
            if (bdrHardwareWarning != null) bdrHardwareWarning.Visibility = Visibility.Collapsed;
            _isPipVisible = false;
            if (bdrPipMonitor != null) bdrPipMonitor.Visibility = Visibility.Collapsed;
            UpdatePipButtonVisual();

            pnlExpanded.Visibility = Visibility.Collapsed;
            pnlCollapsed.Visibility = Visibility.Visible;
            pnlCollapsed.Opacity = 0.92;
            UpdateLayout();

            if (_orbAnchorLeft >= 0 && _orbAnchorTop >= 0)
            {
                Left = _orbAnchorLeft;
                Top = _orbAnchorTop;
                EnsureWithinScreenBounds();
            }

            _idleOrbDimTimer?.Stop();
            _idleOrbDimTimer?.Start();
        }

        public void ExpandBar()
        {
            if (pnlCollapsed.Visibility == Visibility.Visible)
            {
                _orbAnchorLeft = Left;
                _orbAnchorTop = Top;
            }

            SyncMediaControlsState();
            SyncBroadcastVisualState();
            SyncRecorderVisualState();

            pnlCollapsed.Visibility = Visibility.Collapsed;
            pnlExpanded.Visibility = Visibility.Visible;
            UpdateLayout();

            var workArea = SystemParameters.WorkArea;
            double hubW = ActualWidth > 0 ? ActualWidth : 412;
            double hubH = ActualHeight > 0 ? ActualHeight : 330;

            double anchorX = _orbAnchorLeft >= 0 ? _orbAnchorLeft : Left;
            double anchorY = _orbAnchorTop >= 0 ? _orbAnchorTop : Top;

            // Mở rộng thông minh: nếu cục tròn ở nửa phải màn hình thì bung sang trái, nửa trái thì bung sang phải
            double targetLeft = (anchorX > workArea.Left + workArea.Width * 0.5)
                ? (anchorX + 74 - hubW)
                : anchorX;

            Left = Math.Max(workArea.Left + 8, Math.Min(targetLeft, workArea.Right - hubW - 8));
            Top = Math.Max(workArea.Top + 8, Math.Min(anchorY, workArea.Bottom - hubH - 8));

            Activate();
        }

        private void OnCollapseClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            CollapseBar();
        }

        private void btnCollapseBar_PreviewTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            CollapseBar();
        }

        #endregion

        #region Mode Switch Handlers

        private async void OnClassClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            await SwitchToClassModeAsync();
        }

        private async void OnClassTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            await SwitchToClassModeAsync();
        }

        private async System.Threading.Tasks.Task SwitchToClassModeAsync()
        {
            if (_modeService.IsTransitioning) return;
            if (_roleService?.IsSmartTouchOnly == true || _roleService?.CurrentRole == UserRole.Guest) return;
            ApplyPressedFeedback(btnClass);
            CollapseBar();
            await _modeService.GoToClassAsync();
        }

        private async void OnScreenClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            await SwitchToScreenModeAsync();
        }

        private async void OnScreenTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            await SwitchToScreenModeAsync();
        }

        private async System.Threading.Tasks.Task SwitchToScreenModeAsync()
        {
            if (_modeService.IsTransitioning) return;
            ApplyPressedFeedback(btnScreen);
            CollapseBar();
            await _modeService.GoToScreenAsync();
        }

        private async void OnDesktopClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            await SwitchToDesktopModeAsync();
        }

        private async void OnDesktopTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            await SwitchToDesktopModeAsync();
        }

        private async System.Threading.Tasks.Task SwitchToDesktopModeAsync()
        {
            if (_modeService.IsTransitioning) return;
            ApplyPressedFeedback(btnDesktop);
            CollapseBar();
            await _modeService.GoToDesktopAsync();
        }

        private void OnModeChanged(object? sender, ModeChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateUI(e.NewMode);
                // Luôn giữ giao diện ở trạng thái 1 Cục Tròn EasyTouch gọn gàng khi chuyển giữa SMART CLASS / SMART TOUCH / DESKTOP
                CollapseBar();
            });
        }

        private void UpdateUI(AppMode mode)
        {
            var inactiveColor = Brushes.Transparent;
            var classColor = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));
            var screenColor = new SolidColorBrush(Color.FromRgb(0x4F, 0x46, 0xE5));
            var desktopColor = new SolidColorBrush(Color.FromRgb(0x0E, 0xA5, 0xE9));

            btnClass.Background = mode == AppMode.SmartClass ? classColor : inactiveColor;
            btnScreen.Background = mode == AppMode.SmartScreen ? screenColor : inactiveColor;
            btnDesktop.Background = mode == AppMode.Desktop ? desktopColor : inactiveColor;

            SetModeCardTextColor(btnClass, mode == AppMode.SmartClass);
            SetModeCardTextColor(btnScreen, mode == AppMode.SmartScreen);
            SetModeCardTextColor(btnDesktop, mode == AppMode.Desktop);

            if (!_isBroadcasting)
            {
                txtHubStatusTitle.Text = mode switch
                {
                    AppMode.SmartClass => "SMART CLASS",
                    AppMode.SmartScreen => "SMART TOUCH",
                    _ => "DESKTOP"
                };
                txtHubStatusTitle.Foreground = new SolidColorBrush(Color.FromRgb(0x93, 0xC5, 0xFD));
                int online = ClassroomAppContext.Network?.ConnectedCount ?? 0;
                txtHubSubStatus.Text = online > 0 ? $"👥 {online} máy kết nối" : "Sẵn sàng";
                dotHubLive.Fill = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6));
            }

            UpdateOrbAppearance();
        }

        private static void SetModeCardTextColor(Border border, bool isActive)
        {
            if (border.Child is StackPanel sp && sp.Children.Count >= 2)
            {
                if (sp.Children[0] is TextBlock titleTb)
                    titleTb.Foreground = isActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
                if (sp.Children[1] is TextBlock subTb)
                    subTb.Foreground = isActive
                        ? new SolidColorBrush(Color.FromRgb(0xDB, 0xEA, 0xFE))
                        : new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
            }
        }

        private void UpdateOrbAppearance()
        {
            if (pnlCollapsed == null || dotOrbStatus == null) return;

            bool micOn = LiveBroadcastMediaService.Instance.HasMicrophoneHardware && LiveBroadcastMediaService.Instance.IsMicrophoneEnabled;
            if (_isBroadcasting || _isRecording)
            {
                if (_isBroadcastPaused)
                {
                    pnlCollapsed.BorderBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                    dotOrbStatus.Fill = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                }
                else
                {
                    pnlCollapsed.BorderBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                    dotOrbStatus.Fill = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                }
            }
            else if (_isClassScreenLocked)
            {
                pnlCollapsed.BorderBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                dotOrbStatus.Fill = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
            }
            else if (micOn)
            {
                pnlCollapsed.BorderBrush = new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99));
                dotOrbStatus.Fill = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
            }
            else
            {
                Color modeDot = _modeService.CurrentMode switch
                {
                    AppMode.SmartClass => Color.FromRgb(0x3B, 0x82, 0xF6),
                    AppMode.SmartScreen => Color.FromRgb(0x81, 0x8C, 0xF8),
                    _ => Color.FromRgb(0x38, 0xBD, 0xF8)
                };
                pnlCollapsed.BorderBrush = new SolidColorBrush(Color.FromArgb(0x99, 0x93, 0xC5, 0xFD));
                dotOrbStatus.Fill = new SolidColorBrush(modeDot);
            }
        }

        #endregion

        #region Media (Mic • Âm thanh • Camera) & Class Lock Integration

        private void OnMicToggleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            ToggleMicrophoneLive();
        }

        private void OnMicToggleTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            ToggleMicrophoneLive();
        }

        private void ToggleMicrophoneLive()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                app.EnsureClassroomBroadcastEngineReady();
                var svc = LiveBroadcastMediaService.Instance;
                bool nextState = !svc.IsMicrophoneEnabled;
                svc.SetMicrophoneEnabled(nextState);

                var net = ClassroomAppContext.Network ?? app.NetworkService;
                if (net != null && net.IsBroadcasting)
                {
                    string flag = nextState ? "1" : "0";
                    _ = net.SendCommandAsync($"CMD|VOICE_BROADCAST|{flag}");
                }
                SyncMediaControlsState();
            }
            catch (Exception ex)
            {
                Log.Warning("[EasyTouch] ToggleMicrophoneLive error: {Err}", ex.Message);
            }
        }

        private void OnSysAudioToggleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            ToggleSystemAudioLive();
        }

        private void OnSysAudioToggleTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            ToggleSystemAudioLive();
        }

        private void ToggleSystemAudioLive()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                app.EnsureClassroomBroadcastEngineReady();
                var svc = LiveBroadcastMediaService.Instance;
                svc.SetSystemAudioEnabled(!svc.IsSystemAudioEnabled);
                SyncMediaControlsState();
            }
            catch (Exception ex)
            {
                Log.Warning("[EasyTouch] ToggleSystemAudioLive error: {Err}", ex.Message);
            }
        }

        private async void OnCamToggleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            await ToggleCameraLiveAsync();
        }

        private async void OnCamToggleTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            await ToggleCameraLiveAsync();
        }

        private async System.Threading.Tasks.Task ToggleCameraLiveAsync()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                app.EnsureClassroomBroadcastEngineReady();
                var svc = LiveBroadcastMediaService.Instance;
                await svc.SetCameraEnabledAsync(!svc.IsCameraEnabled);
                SyncMediaControlsState();
            }
            catch (Exception ex)
            {
                Log.Warning("[EasyTouch] ToggleCameraLiveAsync error: {Err}", ex.Message);
            }
        }

        private void OnAudioPopoverClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ToggleDevicePopover("AUDIO");
        }

        private void OnAudioPopoverTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            ToggleDevicePopover("AUDIO");
        }

        private void OnCamPopoverClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ToggleDevicePopover("CAMERA");
        }

        private void OnCamPopoverTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            ToggleDevicePopover("CAMERA");
        }

        private void ToggleDevicePopover(string mode)
        {
            if (string.IsNullOrEmpty(mode) || _openPopoverMode == mode)
            {
                _openPopoverMode = "";
                bdrDevicePopover.Visibility = Visibility.Collapsed;
                bdrHardwareWarning.Visibility = Visibility.Collapsed;
                EnsureWithinScreenBounds();
                return;
            }
            ShowDevicePopoverSection(mode);
        }

        private void ShowDevicePopoverSection(string mode)
        {
            _openPopoverMode = mode;
            bdrDevicePopover.Visibility = Visibility.Visible;
            pnlAudioSettings.Visibility = mode == "AUDIO" ? Visibility.Visible : Visibility.Collapsed;
            pnlCameraSettings.Visibility = mode == "CAMERA" ? Visibility.Visible : Visibility.Collapsed;
            btnTabAudio.Background = new SolidColorBrush(mode == "AUDIO" ? Color.FromRgb(0x25, 0x63, 0xEB) : Color.FromRgb(0x2A, 0x32, 0x41));
            btnTabCamera.Background = new SolidColorBrush(mode == "CAMERA" ? Color.FromRgb(0x25, 0x63, 0xEB) : Color.FromRgb(0x2A, 0x32, 0x41));
            SyncMediaControlsState();
            EnsureWithinScreenBounds();
        }

        private void OnSwitchPopoverAudioTab(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ShowDevicePopoverSection("AUDIO");
        }

        private void OnSwitchPopoverCameraTab(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ShowDevicePopoverSection("CAMERA");
        }

        private void OnCloseDevicePopover(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ToggleDevicePopover("");
        }

        private async void OnRescanHardwareClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            LiveBroadcastMediaService.Instance.RefreshAudioHardwareCatalog();
            await LiveBroadcastMediaService.Instance.RefreshCameraHardwareCatalogAsync();
            SyncMediaControlsState();
        }

        private void CmbMicrophone_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressDeviceSelectionEvent) return;
            if (cmbMicrophone.SelectedItem is AudioEndpointOption opt)
            {
                LiveBroadcastMediaService.Instance.SelectMicrophone(opt.DeviceId);
            }
        }

        private void ChkPopoverSystemAudio_Click(object sender, RoutedEventArgs e)
        {
            if (_suppressDeviceSelectionEvent) return;
            LiveBroadcastMediaService.Instance.SetSystemAudioEnabled(chkPopoverSystemAudio.IsChecked == true);
            SyncMediaControlsState();
        }

        private void CmbSystemOutput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressDeviceSelectionEvent) return;
            if (cmbSystemOutput.SelectedItem is AudioEndpointOption opt)
            {
                LiveBroadcastMediaService.Instance.SelectSystemOutput(opt.DeviceId);
            }
        }

        private async void CmbCamera_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressDeviceSelectionEvent) return;
            if (cmbCamera.SelectedItem is CameraEndpointOption cam)
            {
                await LiveBroadcastMediaService.Instance.SelectCameraAsync(cam.DeviceId);
            }
        }

        private void CmbCamShape_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressDeviceSelectionEvent || cmbCamShape == null) return;
            LiveBroadcastMediaService.Instance.CameraShape = cmbCamShape.SelectedIndex == 1
                ? MeetCameraShape.SmartTouchCircle
                : MeetCameraShape.GoogleMeetRect;
        }

        private void CmbCamCorner_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressDeviceSelectionEvent || cmbCamCorner == null) return;
            LiveBroadcastMediaService.Instance.CameraCorner = cmbCamCorner.SelectedIndex switch
            {
                1 => MeetCameraCorner.BottomLeft,
                2 => MeetCameraCorner.TopRight,
                _ => MeetCameraCorner.BottomRight
            };
        }

        private void ChkCamMirror_Click(object sender, RoutedEventArgs e)
        {
            if (_suppressDeviceSelectionEvent) return;
            LiveBroadcastMediaService.Instance.IsCameraMirrored = chkCamMirror.IsChecked == true;
        }

        private void ShowHardwareWarning(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            txtHardwareWarning.Text = "⚠️ " + message;
            bdrHardwareWarning.Visibility = Visibility.Visible;
            if (pnlExpanded.Visibility == Visibility.Visible && bdrDevicePopover.Visibility != Visibility.Visible)
            {
                ShowDevicePopoverSection("AUDIO");
            }
        }

        private void SyncMediaControlsState()
        {
            _suppressDeviceSelectionEvent = true;
            try
            {
                var svc = LiveBroadcastMediaService.Instance;

                // 1. Micro
                bool micOn = svc.HasMicrophoneHardware && svc.IsMicrophoneEnabled;
                btnVoice.IsEnabled = svc.HasMicrophoneHardware;
                bdrMicPill.Background = new SolidColorBrush(!svc.HasMicrophoneHardware
                    ? Color.FromRgb(0x33, 0x41, 0x55)
                    : (micOn ? Color.FromRgb(0x05, 0x96, 0x69) : Color.FromRgb(0x7F, 0x1D, 0x1D)));
                txtVoice.Text = !svc.HasMicrophoneHardware
                    ? "🚫 Không Mic"
                    : (micOn ? "🎙️ Mic: BẬT" : "🔇 Tắt mic");

                // 2. Âm thanh (Hệ thống)
                bool sysOn = svc.HasSystemAudioHardware && svc.IsSystemAudioEnabled;
                bdrSysAudioPill.IsEnabled = svc.HasSystemAudioHardware;
                bdrSysAudioPill.Background = new SolidColorBrush(!svc.HasSystemAudioHardware
                    ? Color.FromRgb(0x33, 0x41, 0x55)
                    : (sysOn ? Color.FromRgb(0x1D, 0x4E, 0xD8) : Color.FromRgb(0x7F, 0x1D, 0x1D)));
                txtSysAudio.Text = !svc.HasSystemAudioHardware
                    ? "🔇 Không loa"
                    : (sysOn ? "🔊 Âm thanh" : "🔈 Tắt âm thanh");

                // 3. Camera
                bool camOn = svc.HasCameraHardware && svc.IsCameraEnabled;
                btnCamToggle.IsEnabled = svc.HasCameraHardware || svc.IsScanningCameras;
                bdrCamPill.Background = new SolidColorBrush(!svc.HasCameraHardware && !svc.IsScanningCameras
                    ? Color.FromRgb(0x33, 0x41, 0x55)
                    : (camOn ? Color.FromRgb(0x05, 0x96, 0x69) : Color.FromRgb(0x2A, 0x32, 0x41)));
                txtCamState.Text = svc.IsScanningCameras && !svc.HasCameraHardware
                    ? "📷 Đang dò..."
                    : (!svc.HasCameraHardware ? "🚫 Không Cam" : (camOn ? "📷 Cam: BẬT" : "📷 Tắt cam"));

                // 4. Đồng bộ danh sách thiết bị trong Popover
                cmbMicrophone.ItemsSource = svc.AvailableMicrophones;
                cmbMicrophone.IsEnabled = svc.HasMicrophoneHardware;
                if (svc.HasMicrophoneHardware)
                {
                    cmbMicrophone.SelectedItem = svc.AvailableMicrophones.FirstOrDefault(m => m.DeviceId == svc.SelectedMicrophoneId)
                                              ?? svc.AvailableMicrophones.FirstOrDefault();
                }

                chkPopoverSystemAudio.IsEnabled = svc.HasSystemAudioHardware;
                chkPopoverSystemAudio.IsChecked = sysOn;
                cmbSystemOutput.ItemsSource = svc.AvailableSystemOutputs;
                cmbSystemOutput.IsEnabled = svc.HasSystemAudioHardware;
                if (svc.HasSystemAudioHardware)
                {
                    cmbSystemOutput.SelectedItem = svc.AvailableSystemOutputs.FirstOrDefault(o => o.DeviceId == svc.SelectedSystemOutputId)
                                                ?? svc.AvailableSystemOutputs.FirstOrDefault();
                }

                cmbCamera.ItemsSource = svc.AvailableCameras;
                cmbCamera.IsEnabled = svc.HasCameraHardware;
                if (svc.HasCameraHardware)
                {
                    cmbCamera.SelectedItem = svc.AvailableCameras.FirstOrDefault(c => c.DeviceId == svc.SelectedCameraId)
                                          ?? svc.AvailableCameras.FirstOrDefault();
                }

                cmbCamShape.SelectedIndex = svc.CameraShape == MeetCameraShape.SmartTouchCircle ? 1 : 0;
                cmbCamCorner.SelectedIndex = svc.CameraCorner switch
                {
                    MeetCameraCorner.BottomLeft => 1,
                    MeetCameraCorner.TopRight => 2,
                    _ => 0
                };
                chkCamMirror.IsChecked = svc.IsCameraMirrored;
                txtCamPreviewHint.Text = !svc.HasCameraHardware
                    ? "⚠️ Không tìm thấy thiết bị Camera trên máy Giáo viên."
                    : (camOn ? "Đang khởi tạo hình ảnh Camera..." : "Camera đang tắt — Bấm nút 📷 Cam để bật");

                UpdateOrbAppearance();
            }
            catch (Exception ex)
            {
                Log.Debug("[EasyTouch] SyncMediaControlsState error: {Err}", ex.Message);
            }
            finally
            {
                _suppressDeviceSelectionEvent = false;
            }
        }

        private void OnMediaMeterTimerTick(object? sender, EventArgs e)
        {
            if (pnlExpanded.Visibility != Visibility.Visible) return;

            var svc = LiveBroadcastMediaService.Instance;
            double micPct = svc.IsMicrophoneEnabled ? svc.CurrentMicLevelPercent : 0;
            barMic1.Height = Math.Clamp(3 + micPct * 0.09, 3, 13);
            barMic2.Height = Math.Clamp(3 + micPct * 0.12, 3, 14);
            barMic3.Height = Math.Clamp(3 + micPct * 0.08, 3, 12);

            double sysPct = svc.IsSystemAudioEnabled ? svc.CurrentSystemLevelPercent : 0;
            barSys1.Height = Math.Clamp(3 + sysPct * 0.11, 3, 13);
            barSys2.Height = Math.Clamp(3 + sysPct * 0.08, 3, 12);

            if (bdrDevicePopover.Visibility == Visibility.Visible)
            {
                double maxW = 366;
                bdrMicMeterFill.Width = Math.Clamp(maxW * (micPct / 100.0), 0, maxW);
                bdrSysMeterFill.Width = Math.Clamp(maxW * (sysPct / 100.0), 0, maxW);

                if (pnlCameraSettings.Visibility == Visibility.Visible)
                {
                    var camBmp = svc.LatestCameraPreviewBitmap;
                    if (svc.IsCameraEnabled && camBmp != null)
                    {
                        imgSelfCamPreview.Source = camBmp;
                        imgSelfCamPreview.Visibility = Visibility.Visible;
                        txtCamPreviewHint.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        imgSelfCamPreview.Source = null;
                        imgSelfCamPreview.Visibility = Visibility.Collapsed;
                        txtCamPreviewHint.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        // ─── KHÓA / MỞ KHÓA MÀN HÌNH HỌC SINH TOÀN LỚP ───
        private void OnClassLockToggleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            ToggleClassScreenLock();
        }

        private void OnClassLockToggleTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            ToggleClassScreenLock();
        }

        private void ToggleClassScreenLock()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                app.EnsureClassroomBroadcastEngineReady();

                SetClassScreenLockState(!_isClassScreenLocked);
                string cmd = _isClassScreenLocked ? "CMD|LOCK_SCREEN" : "CMD|UNLOCK_SCREEN";

                var net = ClassroomAppContext.Network ?? app.NetworkService;
                if (net != null && net.IsBroadcasting)
                {
                    _ = net.SendCommandAsync(cmd);
                }
                else
                {
                    ClassroomAppContext.DispatchCommand(cmd);
                }

                Log.Information("[EasyTouch] Class screen lock toggled: {Locked} ({Cmd})", _isClassScreenLocked, cmd);
            }
            catch (Exception ex)
            {
                Log.Warning("[EasyTouch] ToggleClassScreenLock error: {Err}", ex.Message);
            }
        }

        public void SetClassScreenLockState(bool isLocked)
        {
            _isClassScreenLocked = isLocked;
            if (btnClassLockToggle != null && txtClassLockState != null)
            {
                if (isLocked)
                {
                    btnClassLockToggle.Background = new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06));
                    txtClassLockState.Text = "🔓 Mở khóa HS";
                    txtClassLockState.Foreground = Brushes.White;
                    btnClassLockToggle.ToolTip = "Đang KHÓA màn hình máy Học sinh. Nhấn để MỞ KHÓA cho cả lớp.";
                }
                else
                {
                    btnClassLockToggle.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x32, 0x41));
                    txtClassLockState.Text = "🔒 Khóa máy HS";
                    txtClassLockState.Foreground = new SolidColorBrush(Color.FromRgb(0xFD, 0xE6, 0x8A));
                    btnClassLockToggle.ToolTip = "Khóa màn hình toàn bộ máy Học sinh để tập trung nghe giảng.";
                }
            }
            UpdateOrbAppearance();
        }

        #endregion

        #region Broadcast Controls & Bridge Methods (Synced with BroadcastPage)

        public void SetAllowStudentExitState(bool allowExit)
        {
            _suppressAllowExitEvent = true;
            try
            {
                _allowStudentExit = allowExit;
                if (btnAllowStudentExit != null && txtAllowStudentExit != null)
                {
                    if (allowExit)
                    {
                        btnAllowStudentExit.Background = new SolidColorBrush(Color.FromRgb(0x06, 0x5F, 0x46));
                        btnAllowStudentExit.BorderBrush = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
                        txtAllowStudentExit.Text = "🔓 Đang cho phép HS bấm ra ngoài";
                        txtAllowStudentExit.Foreground = new SolidColorBrush(Color.FromRgb(0xA7, 0xF3, 0xD0));
                    }
                    else
                    {
                        btnAllowStudentExit.Background = new SolidColorBrush(Color.FromRgb(0x3B, 0x1D, 0x24));
                        btnAllowStudentExit.BorderBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                        txtAllowStudentExit.Text = "🔒 Đang khóa toàn màn hình HS (Chạm để mở)";
                        txtAllowStudentExit.Foreground = new SolidColorBrush(Color.FromRgb(0xFC, 0xA5, 0xA5));
                    }
                }
            }
            finally
            {
                _suppressAllowExitEvent = false;
            }
        }

        public void SetHideClassroomShellCheck(bool hideShell)
        {
            _suppressHideShellEvent = true;
            try
            {
                if (chkHideClassroomShell != null)
                {
                    chkHideClassroomShell.IsChecked = hideShell;
                }
            }
            finally
            {
                _suppressHideShellEvent = false;
            }
        }

        public void UpdateLiveFrame(ImageSource? frame, string sourceDescription, string resolutionAndFps)
        {
            if (frame != null && imgLivePip != null)
            {
                imgLivePip.Source = frame;
            }
            if (txtPipSubtitle != null)
            {
                txtPipSubtitle.Text = $"Nguồn: {sourceDescription} ({resolutionAndFps})";
            }
        }

        public void UpdateStatus(bool isPaused, string timerText, int recipientCount, string sourceDescription)
        {
            _isBroadcastPaused = isPaused;

            if (_isBroadcasting)
            {
                txtHubStatusTitle.Text = isPaused
                    ? "⏸ ĐÓNG BĂNG"
                    : (_isLockedSource ? "📌 GIỮ CỐ ĐỊNH" : "🔴 ĐANG CHIẾU");
                txtHubStatusTitle.Foreground = new SolidColorBrush(isPaused
                    ? Color.FromRgb(0xFD, 0xE6, 0x8A)
                    : Color.FromRgb(0xFC, 0xA5, 0xA5));
                txtHubSubStatus.Text = $"{timerText} • 👥 {recipientCount} • {sourceDescription}";
                dotHubLive.Fill = new SolidColorBrush(isPaused
                    ? Color.FromRgb(0xF5, 0x9E, 0x0B)
                    : Color.FromRgb(0xEF, 0x44, 0x44));
            }

            if (btnLivePause != null && txtLivePause != null)
            {
                btnLivePause.Background = new SolidColorBrush(isPaused
                    ? Color.FromRgb(0x05, 0x96, 0x69)
                    : Color.FromRgb(0x2A, 0x32, 0x41));
                txtLivePause.Text = isPaused ? "▶️ Tiếp tục" : "⏸️ Đóng băng";
            }

            if (txtPipBadge != null)
            {
                txtPipBadge.Text = isPaused
                    ? "⏸️ ĐÃ ĐÓNG BĂNG TRÊN MÁY HS"
                    : (_isLockedSource ? "📌 CỐ ĐỊNH TRÊN MÁY HS" : "🟢 TRỰC TIẾP TRÊN MÁY HS");
                txtPipBadge.Foreground = new SolidColorBrush(isPaused
                    ? Color.FromRgb(0xFD, 0xE6, 0x8A)
                    : Color.FromRgb(0x86, 0xEF, 0xAC));
            }

            UpdateOrbAppearance();
        }

        public void UpdateSourceLockIndicator(bool isLockedSource, bool canSwitchToCurrentScreen, string switchTargetLabel)
        {
            _isLockedSource = isLockedSource;
            if (btnShareActiveInstead == null || txtShareActiveInstead == null) return;

            if (isLockedSource && canSwitchToCurrentScreen && !string.IsNullOrWhiteSpace(switchTargetLabel))
            {
                string shortLabel = switchTargetLabel.Length > 24
                    ? switchTargetLabel.Substring(0, 24) + "…"
                    : switchTargetLabel;
                txtShareActiveInstead.Text = $"🔄 Chiếu '{shortLabel}' thay thế";
                btnShareActiveInstead.ToolTip = $"Đang giữ cố định nguồn cũ. Bấm để chuyển sang chiếu: {switchTargetLabel}";
                btnShareActiveInstead.Visibility = Visibility.Visible;
            }
            else
            {
                btnShareActiveInstead.Visibility = Visibility.Collapsed;
            }
        }

        public void ShowLivePipMonitor()
        {
            ExpandBar();
            _isPipVisible = true;
            bdrPipMonitor.Visibility = Visibility.Visible;
            UpdatePipButtonVisual();
            EnsureWithinScreenBounds();
        }

        private void UpdatePipButtonVisual()
        {
            if (btnLivePip != null && txtLivePip != null)
            {
                btnLivePip.Background = new SolidColorBrush(_isPipVisible
                    ? Color.FromRgb(0x25, 0x63, 0xEB)
                    : Color.FromRgb(0x2A, 0x32, 0x41));
                txtLivePip.Text = _isPipVisible ? "👁️ Ẩn thử" : "👁️ Xem thử";
            }
        }

        private void OnAllowStudentExitClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce() || _suppressAllowExitEvent) return;
            SetAllowStudentExitState(!_allowStudentExit);
            AllowStudentExitChanged?.Invoke(this, _allowStudentExit);
        }

        private void OnAllowStudentExitTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce() || _suppressAllowExitEvent) return;
            SetAllowStudentExitState(!_allowStudentExit);
            AllowStudentExitChanged?.Invoke(this, _allowStudentExit);
        }

        private void OnShareCurrentInsteadClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ShareCurrentInsteadRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnShareCurrentInsteadTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            ShareCurrentInsteadRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnLiveChangeSourceClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            if (_isBroadcastBridgeBound) ChangeSourceRequested?.Invoke(this, EventArgs.Empty);
            else DispatchBroadcastAction(QuickBroadcastAction.PickScreenOrWindow);
        }

        private void OnLiveChangeSourceTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            if (_isBroadcastBridgeBound) ChangeSourceRequested?.Invoke(this, EventArgs.Empty);
            else DispatchBroadcastAction(QuickBroadcastAction.PickScreenOrWindow);
        }

        private void OnPickRegionQuickClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            CollapseBar();
            if (_isBroadcasting && _isBroadcastBridgeBound) SelectRegionRequested?.Invoke(this, EventArgs.Empty);
            else DispatchBroadcastAction(QuickBroadcastAction.PickRegion);
        }

        private void OnPickRegionQuickTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            CollapseBar();
            if (_isBroadcasting && _isBroadcastBridgeBound) SelectRegionRequested?.Invoke(this, EventArgs.Empty);
            else DispatchBroadcastAction(QuickBroadcastAction.PickRegion);
        }

        private void OnShareSmartTouchQuickClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            CollapseBar();
            DispatchBroadcastAction(QuickBroadcastAction.ShareSmartTouchNow);
        }

        private void OnShareSmartTouchQuickTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            CollapseBar();
            DispatchBroadcastAction(QuickBroadcastAction.ShareSmartTouchNow);
        }

        private void OnLivePauseClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            if (_isBroadcastBridgeBound) TogglePauseRequested?.Invoke(this, EventArgs.Empty);
            else DispatchBroadcastAction(QuickBroadcastAction.TogglePause);
        }

        private void OnLivePauseTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            if (_isBroadcastBridgeBound) TogglePauseRequested?.Invoke(this, EventArgs.Empty);
            else DispatchBroadcastAction(QuickBroadcastAction.TogglePause);
        }

        private void OnLivePipToggleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            _isPipVisible = !_isPipVisible;
            bdrPipMonitor.Visibility = _isPipVisible ? Visibility.Visible : Visibility.Collapsed;
            UpdatePipButtonVisual();
            EnsureWithinScreenBounds();
        }

        private void OnLivePipToggleTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            _isPipVisible = !_isPipVisible;
            bdrPipMonitor.Visibility = _isPipVisible ? Visibility.Visible : Visibility.Collapsed;
            UpdatePipButtonVisual();
            EnsureWithinScreenBounds();
        }

        private void OnClosePipClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            _isPipVisible = false;
            bdrPipMonitor.Visibility = Visibility.Collapsed;
            UpdatePipButtonVisual();
            EnsureWithinScreenBounds();
        }

        private void ChkHideClassroomShell_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressHideShellEvent) return;
            HideClassroomShellChanged?.Invoke(this, chkHideClassroomShell.IsChecked == true);
        }

        private void OnLiveStopBroadcastClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            if (_isBroadcastBridgeBound) StopRequested?.Invoke(this, EventArgs.Empty);
            else ScreenShareController.Instance.RequestStop();
            SyncBroadcastVisualState();
        }

        private void OnLiveStopBroadcastTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            if (_isBroadcastBridgeBound) StopRequested?.Invoke(this, EventArgs.Empty);
            else ScreenShareController.Instance.RequestStop();
            SyncBroadcastVisualState();
        }

        #endregion

        #region Quick Utility Actions & Broadcast Context Menu

        private static void ApplyPressedFeedback(Border button)
        {
            try
            {
                var scaleTransform = new ScaleTransform(1, 1);
                button.RenderTransform = scaleTransform;
                button.RenderTransformOrigin = new Point(0.5, 0.5);

                var anim = new System.Windows.Media.Animation.DoubleAnimation(1.0, 0.95, TimeSpan.FromMilliseconds(55))
                {
                    AutoReverse = true
                };
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
            }
            catch { }
        }

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
                    ApplyCaptureExclusionForLiveState(true);
                    if (isLive) Topmost = true;
                }
                _isBroadcasting = isLive;

                if (pnlShareIdle != null && pnlShareLive != null)
                {
                    pnlShareIdle.Visibility = isLive ? Visibility.Collapsed : Visibility.Visible;
                    pnlShareLive.Visibility = isLive ? Visibility.Visible : Visibility.Collapsed;
                }

                if (!isLive)
                {
                    _isBroadcastPaused = false;
                    _isPipVisible = false;
                    if (bdrPipMonitor != null) bdrPipMonitor.Visibility = Visibility.Collapsed;
                    UpdatePipButtonVisual();
                    UpdateUI(_modeService.CurrentMode);
                }

                UpdateOrbAppearance();
                if (pnlExpanded.Visibility == Visibility.Visible)
                {
                    EnsureWithinScreenBounds();
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
                if (btnRecord != null && txtRecord != null && txtRecordLabel != null)
                {
                    btnRecord.Background = active
                        ? new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26))
                        : new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF));
                    txtRecord.Text = active ? "🔴" : "⏺";
                    txtRecordLabel.Text = active ? "Đang ghi" : "Ghi hình";
                }
                UpdateOrbAppearance();
            }
            catch { }
        }

        private void OnShowHideClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
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
                Window? targetWindow = app.ModeService.CurrentMode switch
                {
                    AppMode.SmartClass => app._classroomShell,
                    AppMode.SmartScreen => app._whiteboardShell,
                    _ => null
                };

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
                    if (targetWindow.Visibility == Visibility.Visible && targetWindow.WindowState != WindowState.Minimized)
                    {
                        targetWindow.Hide();
                        txtShowHide.Text = "📲";
                        txtShowHideLabel.Text = "Mở cửa sổ";
                    }
                    else
                    {
                        if (targetWindow.WindowState == WindowState.Minimized)
                            targetWindow.WindowState = WindowState.Maximized;
                        targetWindow.Show();
                        targetWindow.Activate();
                        txtShowHide.Text = "🔲";
                        txtShowHideLabel.Text = "Ẩn/Hiện";
                    }
                }
                CollapseBar();
            }
            catch (Exception ex)
            {
                Log.Warning("ShowHide error: {Err}", ex.Message);
            }
        }

        private void OnPenClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
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
                CollapseBar();
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
            }
            catch (Exception ex)
            {
                Log.Warning("Pen click error: {Err}", ex.Message);
            }
        }

        private void OnRecordClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
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
                CollapseBar();
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
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            CollapseBar();
            DispatchBroadcastAction(QuickBroadcastAction.PickScreenOrWindow);
        }

        private void OnBroadcastTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            CollapseBar();
            DispatchBroadcastAction(QuickBroadcastAction.PickScreenOrWindow);
        }

        private void OnBroadcastRightClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            OpenQuickBroadcastMenu(pnlCollapsed);
        }

        private void OnBroadcastMenuClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            OpenQuickBroadcastMenu(sender as FrameworkElement ?? btnBroadcastMenu);
        }

        private void OnBroadcastMenuTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            OpenQuickBroadcastMenu(sender as FrameworkElement ?? btnBroadcastMenu);
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
                    Placement = Top < 180
                        ? System.Windows.Controls.Primitives.PlacementMode.Bottom
                        : System.Windows.Controls.Primitives.PlacementMode.Top,
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(199, 210, 254)),
                    BorderThickness = new Thickness(1.5),
                    Padding = new Thickness(6),
                    FontSize = 13
                };

                MenuItem BuildItem(string icon, string title, Action onClick, bool danger = false)
                {
                    var mi = new MenuItem
                    {
                        Header = $"{icon}  {title}",
                        MinHeight = 36,
                        FontWeight = danger ? FontWeights.Bold : FontWeights.SemiBold,
                        Foreground = danger
                            ? new SolidColorBrush(Color.FromRgb(220, 38, 38))
                            : new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                        Cursor = Cursors.Hand
                    };
                    mi.Click += (_, _) =>
                    {
                        CollapseBar();
                        onClick();
                    };
                    return mi;
                }

                menu.Items.Add(new MenuItem
                {
                    IsEnabled = false,
                    Header = isLive ? $"🔴 ĐANG CHIẾU • {onlineCount} máy HS" : $"📡 CHẾ ĐỘ QUẢNG BÁ ({onlineCount} HS online)",
                    FontWeight = FontWeights.Bold
                });
                menu.Items.Add(new Separator());

                if (isLive)
                {
                    menu.Items.Add(BuildItem("⏹️", "Dừng chia sẻ màn hình ngay", () => ScreenShareController.Instance.RequestStop(), danger: true));
                }
                menu.Items.Add(BuildItem("👨‍🎓", "Chia sẻ màn hình Học sinh cho cả lớp", () => DispatchBroadcastAction(QuickBroadcastAction.OpenStudentDemo)));
                menu.Items.Add(BuildItem("📢", "Gửi Thông báo nổi tới cả lớp...", () => DispatchBroadcastAction(QuickBroadcastAction.OpenNoticeTab)));
                menu.Items.Add(BuildItem("🌐", "Mở Website học tập đồng loạt...", () => DispatchBroadcastAction(QuickBroadcastAction.OpenWebsiteTab)));
                menu.Items.Add(new Separator());
                menu.Items.Add(BuildItem("⚙️", "Mở Trung tâm Quảng bá (Chỉnh FPS & Độ nét)", () => DispatchBroadcastAction(QuickBroadcastAction.OpenBroadcastHub)));

                menu.IsOpen = true;
            }
            catch (Exception ex)
            {
                Log.Warning("OpenQuickBroadcastMenu error: {Err}", ex.Message);
            }
        }

        private void OnCloseTouchDown(object? sender, TouchEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            ExecuteCloseBar();
        }

        private void OnCloseClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!AcquireQuickActionDebounce()) return;
            ExecuteCloseBar();
        }

        private void ExecuteCloseBar()
        {
            bool isLiveShare = ScreenShareController.Instance.IsLive;
            bool isMicOn = LiveBroadcastMediaService.Instance.IsMicrophoneEnabled;
            bool isRec = QASmartTouch.Services.ScreenRecorderService.Instance.IsSessionActive;
            bool isTransmitting = isLiveShare || isMicOn || isRec;

            string warningMsg = "Ẩn nút điều khiển tròn EasyTouch?\n\nBạn có thể mở lại bất cứ lúc nào bằng tổ hợp phím Ctrl+Alt+M.";
            if (isTransmitting)
            {
                warningMsg = "⚠️ Hệ thống đang phát màn hình / âm thanh hoặc ghi hình.\n\n" +
                             "Bạn có muốn DỪNG tất cả phiên phát sóng và ẩn nút điều khiển không?\n" +
                             "(YES: Dừng & Ẩn • NO: Chỉ ẩn nút • CANCEL: Hủy)";
            }

            var result = MessageBox.Show(warningMsg, "Xác nhận ẩn nút điều khiển",
                isTransmitting ? MessageBoxButton.YesNoCancel : MessageBoxButton.YesNo,
                isTransmitting ? MessageBoxImage.Warning : MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (isLiveShare) ScreenShareController.Instance.RequestStop();
                if (isMicOn) LiveBroadcastMediaService.Instance.SetMicrophoneEnabled(false);
                if (isRec) QASmartTouch.Services.ScreenRecorderService.Instance.StopRecording();
                Hide();
            }
            else if (result == MessageBoxResult.No && isTransmitting)
            {
                Hide();
            }
        }

        #endregion
    }
}
