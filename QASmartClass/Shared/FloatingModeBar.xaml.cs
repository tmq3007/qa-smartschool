using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
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

        #region Win32 Native Drag Constants & APIs

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

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
            Log.Information("FloatingModeBar initialized at bottom - Smooth Native Drag enabled");
            
            // Tự động ẩn thanh công cụ xuống dưới các ứng dụng khác khi ứng dụng mất tiêu điểm
            Application.Current.Activated += (s, e) => { this.Topmost = true; };
            Application.Current.Deactivated += (s, e) => { this.Topmost = false; };
        }

        /// <summary>
        /// Đặt vị trí thanh công cụ ở phía dưới màn hình (căn giữa)
        /// </summary>
        public void PositionAtBottom()
        {
            try
            {
                var workArea = SystemParameters.WorkArea;
                var screenW = SystemParameters.PrimaryScreenWidth;
                var targetWidth = this.ActualWidth > 0 ? this.ActualWidth : 660;
                var targetHeight = this.ActualHeight > 0 ? this.ActualHeight : 50;

                Left = (screenW - targetWidth) / 2;
                Top = Math.Max(20, workArea.Bottom - targetHeight - 12);

                EnsureWithinScreenBounds();
            }
            catch (Exception ex)
            {
                Log.Warning("PositionAtBottom error: {Err}", ex.Message);
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

                var currentWidth = this.ActualWidth > 0 ? this.ActualWidth : 660;
                var currentHeight = this.ActualHeight > 0 ? this.ActualHeight : 50;

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

        private void OnCollapseClick(object sender, MouseButtonEventArgs e)
        {
            pnlExpanded.Visibility = Visibility.Collapsed;
            pnlCollapsed.Visibility = Visibility.Visible;
            EnsureWithinScreenBounds();
            Log.Debug("FloatingModeBar collapsed");
        }

        private void btnExpandModeBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            pnlCollapsed.Visibility = Visibility.Collapsed;
            pnlExpanded.Visibility = Visibility.Visible;
            EnsureWithinScreenBounds();
            Log.Debug("FloatingModeBar expanded instantly");
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

        /// <summary>Hiển thị / Ẩn cửa sổ chính</summary>
        private void OnShowHideClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
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
        private async void OnVoiceClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _isVoiceBroadcasting = !_isVoiceBroadcasting;
                var app = (QASmartTouch.App)Application.Current;
                var net = app.NetworkService;

                if (net != null && net.IsBroadcasting)
                    await net.SendCommandAsync($"VOICE_BROADCAST|enabled={_isVoiceBroadcasting}");

                btnVoice.Background = _isVoiceBroadcasting
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                    : Brushes.Transparent;
                txtVoice.Foreground = _isVoiceBroadcasting ? Brushes.White : Brushes.Black;
                btnVoice.ToolTip = _isVoiceBroadcasting ? "Đang phát giọng nói - Click để dừng" : "Bắt đầu phát giọng nói";

                app.Database.EventLogs.Add(new Data.EventLog
                {
                    EventType = _isVoiceBroadcasting ? "VOICE_ON" : "VOICE_OFF",
                    Actor = "GV",
                    Details = _isVoiceBroadcasting ? "Bắt đầu phát giọng nói" : "Dừng phát giọng nói",
                    Timestamp = DateTime.Now
                });
                app.Database.SaveChanges();
                Log.Information("Voice broadcast: {State}", _isVoiceBroadcasting);
            }
            catch (Exception ex) { Log.Warning("Voice broadcast error: {Err}", ex.Message); }
        }

        /// <summary>Mở bút chú thích trên màn hình (chuyển sang SmartScreen)</summary>
        private void OnPenClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _modeService.GoToScreen();
                Log.Information("Pen annotation: switched to SmartScreen");
            }
            catch (Exception ex) { Log.Warning("Pen click error: {Err}", ex.Message); }
        }

        /// <summary>Ghi lại hoạt động trình chiếu vào file</summary>
        private async void OnRecordClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _isRecording = !_isRecording;
                var app = (QASmartTouch.App)Application.Current;
                var net = app.NetworkService;

                if (net != null && net.IsBroadcasting)
                    await net.SendCommandAsync($"SCREEN_RECORD|enabled={_isRecording}");

                btnRecord.Background = _isRecording
                    ? new SolidColorBrush(Color.FromRgb(211, 47, 47))
                    : Brushes.Transparent;
                txtRecord.Foreground = _isRecording ? Brushes.White : Brushes.Black;
                btnRecord.ToolTip = _isRecording ? "Đang ghi hình - Click để dừng" : "Bắt đầu ghi lại trình chiếu";

                app.Database.EventLogs.Add(new Data.EventLog
                {
                    EventType = _isRecording ? "RECORD_START" : "RECORD_STOP",
                    Actor = "GV",
                    Details = _isRecording ? "Bắt đầu ghi hình trình chiếu" : "Dừng ghi hình",
                    Timestamp = DateTime.Now
                });
                app.Database.SaveChanges();
                Log.Information("Screen recording: {State}", _isRecording);
            }
            catch (Exception ex) { Log.Warning("Record error: {Err}", ex.Message); }
        }

        private System.Windows.Threading.DispatcherTimer? _broadcastTimer;
        private int _broadcastTickCount = 0;

        private void OnBroadcastClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;

                if (!_isBroadcasting)
                {
                    System.Windows.Controls.Canvas? canvas = null;
                    foreach (Window win in Application.Current.Windows)
                    {
                        var tn = win.GetType().Name;
                        if (tn.Contains("MainDashboard") || tn.Contains("Form2"))
                        {
                            canvas = win.FindName("MainInteractiveBoard") as System.Windows.Controls.Canvas;
                            break;
                        }
                    }

                    if (canvas == null || canvas.ActualWidth < 1)
                    {
                        MessageBox.Show("Chưa mở SmartScreen hoặc canvas trống.\nHãy chuyển sang SmartScreen trước.",
                            "Phát Bảng", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _isBroadcasting = true;
                    _broadcastTickCount = 0;
                    QASmartTouch.App.BroadcastState.IsScreenBroadcastActive = true;

                    CaptureSmartScreenCanvas(app);

                    var startCmd = $"CMD|SCREEN_BROADCAST_START|{QASmartTouch.App.BroadcastState.ScreenCapturePath}";
                    QASmartTouch.App.LessonState.LastTeacherCommand = startCmd;
                    QASmartTouch.App.LessonState.LastCommandTime = DateTime.Now;
                    var net = app.NetworkService;
                    if (net?.IsBroadcasting == true) _ = net.SendCommandAsync(startCmd);
                    else app.RaiseLocalCommand(startCmd);

                    _broadcastTimer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(QASmartTouch.App.BroadcastState.ScreenBroadcastIntervalSec)
                    };
                    _broadcastTimer.Tick += (s, ev) =>
                    {
                        CaptureSmartScreenCanvas(app);
                        var updateCmd = $"CMD|SCREEN_BROADCAST_UPDATE|{QASmartTouch.App.BroadcastState.ScreenCapturePath}";
                        if (net?.IsBroadcasting == true) _ = net.SendCommandAsync(updateCmd);
                        else app.RaiseLocalCommand(updateCmd);
                        _broadcastTickCount++;
                    };
                    _broadcastTimer.Start();

                    btnBroadcast.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    txtBroadcast.Foreground = Brushes.White;
                    txtBroadcast.Text = "🔴";
                    btnBroadcast.ToolTip = "Đang phát bảng trắng 🔴 - Click để dừng";

                    app.Database.EventLogs.Add(new Data.EventLog
                    {
                        EventType = "BROADCAST_ON", Actor = "GV",
                        Details = "Bắt đầu phát bảng trắng cho HS",
                        Timestamp = DateTime.Now
                    });
                    app.Database.SaveChanges();
                    Log.Information("Screen broadcast STARTED from FloatingModeBar");
                }
                else
                {
                    _isBroadcasting = false;
                    QASmartTouch.App.BroadcastState.IsScreenBroadcastActive = false;

                    _broadcastTimer?.Stop();
                    _broadcastTimer = null;

                    var stopCmd = "CMD|SCREEN_BROADCAST_STOP";
                    QASmartTouch.App.LessonState.LastTeacherCommand = stopCmd;
                    QASmartTouch.App.LessonState.LastCommandTime = DateTime.Now;
                    var net = app.NetworkService;
                    if (net?.IsBroadcasting == true) _ = net.SendCommandAsync(stopCmd);
                    else app.RaiseLocalCommand(stopCmd);

                    btnBroadcast.Background = Brushes.Transparent;
                    txtBroadcast.Foreground = Brushes.Black;
                    txtBroadcast.Text = "📺";
                    btnBroadcast.ToolTip = "Quảng bá bảng trắng cho HS";

                    app.Database.EventLogs.Add(new Data.EventLog
                    {
                        EventType = "BROADCAST_OFF", Actor = "GV",
                        Details = $"Dừng phát bảng trắng (tổng {_broadcastTickCount} cập nhật)",
                        Timestamp = DateTime.Now
                    });
                    app.Database.SaveChanges();
                    Log.Information("Screen broadcast STOPPED from FloatingModeBar ({Count} ticks)", _broadcastTickCount);
                }
            }
            catch (Exception ex) { Log.Warning("Broadcast error: {Err}", ex.Message); }
        }

        private static void CaptureSmartScreenCanvas(QASmartTouch.App app)
        {
            try
            {
                System.Windows.Controls.Canvas? canvas = null;
                foreach (Window win in Application.Current.Windows)
                {
                    var tn = win.GetType().Name;
                    if (tn.Contains("MainDashboard") || tn.Contains("Form2"))
                    {
                        canvas = win.FindName("MainInteractiveBoard") as System.Windows.Controls.Canvas;
                        break;
                    }
                }
                if (canvas == null || canvas.ActualWidth < 1) return;

                double scale = 2.0;
                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)(canvas.ActualWidth * scale), (int)(canvas.ActualHeight * scale),
                    96 * scale, 96 * scale, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(canvas);

                var tempDir = Services.AppPaths.ScreenCapturesDir;
                System.IO.Directory.CreateDirectory(tempDir);
                var filePath = System.IO.Path.Combine(tempDir, "live_broadcast.png");

                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                using (var fs = System.IO.File.Create(filePath))
                    encoder.Save(fs);

                QASmartTouch.App.BroadcastState.ScreenCapturePath = filePath;
                QASmartTouch.App.BroadcastState.ScreenCaptureTime = DateTime.Now;
            }
            catch (Exception ex) { Log.Warning("CaptureSmartScreenCanvas error: {Err}", ex.Message); }
        }

        private void OnCloseClick(object sender, MouseButtonEventArgs e)
        {
            bool isTransmitting = _isBroadcasting || _isVoiceBroadcasting || _isRecording;
            
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
                if (_isBroadcasting) OnBroadcastClick(sender, e);
                if (_isVoiceBroadcasting) OnVoiceClick(sender, e);
                if (_isRecording) OnRecordClick(sender, e);

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
                // Khi chuyển sang DESKTOP, tự động đưa thanh xuống dưới đáy màn hình
                if (e.NewMode == AppMode.Desktop)
                {
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
