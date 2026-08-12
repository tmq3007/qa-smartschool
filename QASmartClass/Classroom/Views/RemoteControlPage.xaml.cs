﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using QASmartClass.Classroom.Services;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class RemoteControlPage : Page
    {
        // ═══ Win32 API — must move REAL cursor (WPF ignores PostMessage lParam) ═══
        [DllImport("user32.dll")] private static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")] private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo);
        [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        private ConnectedStudent? _selected;
        private DispatcherTimer? _captureTimer;
        private int _captureCount = 0;
        private bool _isCapturing = false;
        private bool _mouseEnabled = false;
        private bool _keyboardEnabled = false;

        public RemoteControlPage()
        {
            InitializeComponent();
            Loaded += (_, _) => LoadStudents();
        }

        // ═══════════════════════════════════════════════════════
        //  DATA LOAD
        // ═══════════════════════════════════════════════════════

        private void LoadStudents()
        {
            try
            {
                var rosterStudents = QASmartClass.Classroom.Services.RosterHelper.GetStudents()
                    .Select(s => new ConnectedStudent
                    {
                        Name = s.FullName, PCName = s.PCName ?? $"PC-{s.Id:D2}",
                        IPAddress = s.IPAddress ?? $"192.168.1.{100 + s.Id}", IsOnline = s.IsOnline
                    }).ToList();

                studentList.ItemsSource = rosterStudents.Any() ? rosterStudents :
                    Enumerable.Range(1, 10).Select(i => new ConnectedStudent
                    {
                        Name = $"HS Demo {i:D2}", PCName = $"PC-{i:D2}",
                        IPAddress = $"192.168.1.{100 + i}", IsOnline = i % 4 != 0
                    }).ToList();
            }
            catch (Exception ex) { Log.Warning("RemoteControl load error: {Err}", ex.Message); }
        }

        // ═══════════════════════════════════════════════════════
        //  SELECT STUDENT → Start screen capture
        // ═══════════════════════════════════════════════════════

        private void Student_Selected(object sender, SelectionChangedEventArgs e)
        {
            if (studentList.SelectedItem is ConnectedStudent s)
            {
                _selected = s;
                txtViewerStatus.Text = $"🖥️ {s.Name} — {s.PCName}";
                txtViewerInfo.Text = $"IP: {s.IPAddress}  |  {(s.IsOnline ? "🟢 Online" : "⚫ Offline")}";

                if (s.IsOnline)
                {
                    StartScreenCapture();
                }
                else
                {
                    StopScreenCapture();
                    placeholderPanel.Visibility = Visibility.Visible;
                    screenImage.Visibility = Visibility.Collapsed;
                    liveBadge.Visibility = Visibility.Collapsed;
                }
                Log.Information("Remote view: {Name} @ {IP}", s.Name, s.IPAddress);
            }
        }

        // ═══════════════════════════════════════════════════════
        //  SCREEN CAPTURE — Chụp màn hình HS (cùng máy: chụp desktop)
        // ═══════════════════════════════════════════════════════

        private void StartScreenCapture()
        {
            _captureCount = 0;
            _isCapturing = true;

            // Capture immediately
            CaptureAndDisplay();

            // Then auto-refresh every 2s
            _captureTimer?.Stop();
            _captureTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _captureTimer.Tick += (s, e) =>
            {
                if (_isCapturing)
                    CaptureAndDisplay();
            };
            _captureTimer.Start();

            liveBadge.Visibility = Visibility.Visible;
            placeholderPanel.Visibility = Visibility.Collapsed;
            screenImage.Visibility = Visibility.Visible;
        }

        private void StopScreenCapture()
        {
            _isCapturing = false;
            _captureTimer?.Stop();
            liveBadge.Visibility = Visibility.Collapsed;
            txtRefreshInfo.Text = "";
        }

        private void CaptureAndDisplay()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;

                // ═══ Find StudentShell window (visible OR hidden) ═══
                Window? studentWindow = null;
                foreach (Window w in Application.Current.Windows)
                {
                    if (w is QASmartClass.StudentClient.Views.StudentShell)
                    {
                        studentWindow = w;
                        break;
                    }
                }

                if (studentWindow == null)
                {
                    // StudentShell chưa được mở → hướng dẫn GV
                    txtRefreshInfo.Text = "💡 Nhấn nút [Học sinh] trên thanh trên để mở giao diện HS";
                    placeholderPanel.Visibility = Visibility.Visible;
                    screenImage.Visibility = Visibility.Collapsed;
                    return;
                }

                // ═══ Capture StudentShell content ═══
                var visual = studentWindow.Content as Visual;
                if (visual is FrameworkElement fe && fe.ActualWidth > 0 && fe.ActualHeight > 0)
                {
                    var dpiX = 96.0;
                    var dpiY = 96.0;

                    // Get DPI from source or student window
                    var source = PresentationSource.FromVisual(studentWindow);
                    if (source == null && studentWindow.IsVisible)
                        source = PresentationSource.FromVisual(studentWindow);
                    // Fallback: use this page's DPI
                    if (source == null)
                        source = PresentationSource.FromVisual(this);

                    if (source?.CompositionTarget != null)
                    {
                        dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                        dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
                    }

                    int pixelWidth = (int)(fe.ActualWidth * dpiX / 96.0);
                    int pixelHeight = (int)(fe.ActualHeight * dpiY / 96.0);

                    if (pixelWidth > 0 && pixelHeight > 0)
                    {
                        var rtb = new RenderTargetBitmap(pixelWidth, pixelHeight, dpiX, dpiY, PixelFormats.Pbgra32);
                        rtb.Render(fe);
                        rtb.Freeze();

                        screenImage.Source = rtb;
                        placeholderPanel.Visibility = Visibility.Collapsed;
                        screenImage.Visibility = Visibility.Visible;
                        _captureCount++;
                        txtRefreshInfo.Text = $"📸 #{_captureCount} · {DateTime.Now:HH:mm:ss}";
                        return;
                    }
                }

                // Window exists but no renderable content yet
                txtRefreshInfo.Text = $"🔄 Đang chờ HS load... · {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                Log.Warning("Screen capture error: {Err}", ex.Message);
                txtRefreshInfo.Text = $"⚠️ Lỗi: {ex.Message}";
            }
        }

        // ═══════════════════════════════════════════════════════
        //  COMMANDS
        // ═══════════════════════════════════════════════════════

        private void RemoteCmd_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null)
            {
                MessageBox.Show("Vui lòng chọn học sinh trước!", "Chưa chọn HS", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var tag = (sender as FrameworkElement)?.Tag?.ToString() ?? "";

            switch (tag)
            {
                case "screenshot":
                    // Save current capture to file
                    SaveScreenshot();
                    break;

                case "lock":
                    SendCommand("CMD|LOCK_SCREEN");
                    Log.Information("Lock screen: {Name}", _selected.Name);
                    break;

                case "unlock":
                    SendCommand("CMD|UNLOCK_SCREEN");
                    Log.Information("Unlock screen: {Name}", _selected.Name);
                    break;

                case "silence":
                    SendCommand("CMD|SILENCE");
                    Log.Information("Silence: {Name}", _selected.Name);
                    break;

                case "killapps":
                    SendCommand("CMD|KILL_APPS");
                    Log.Information("Kill apps: {Name}", _selected.Name);
                    break;

                case "warning":
                    SendCommand($"CMD|TEACHER_WARNING|10|⚠️ {_selected.Name}: GV yêu cầu bạn tập trung vào bài học!");
                    Log.Information("Warning sent: {Name}", _selected.Name);
                    break;

                case "clearsilence":
                    SendCommand("CMD|CLEAR_SILENCE");
                    Log.Information("Clear silence: {Name}", _selected.Name);
                    break;

                case "clearwarning":
                    SendCommand("CMD|CLEAR_WARNING");
                    Log.Information("Clear warning: {Name}", _selected.Name);
                    break;

                case "clearall":
                    SendCommand("CMD|CLEAR_ALL");
                    Log.Information("Clear all overlays: {Name}", _selected.Name);
                    break;

                case "disconnect":
                    StopScreenCapture();
                    studentList.SelectedItem = null;
                    _selected = null;
                    txtViewerStatus.Text = "← Chọn học sinh để xem màn hình";
                    txtViewerInfo.Text = "";
                    placeholderPanel.Visibility = Visibility.Visible;
                    screenImage.Visibility = Visibility.Collapsed;
                    screenImage.Source = null;
                    break;
            }
        }

        private void SaveScreenshot()
        {
            try
            {
                if (screenImage.Source is BitmapSource bmpSrc)
                {
                    var folder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "QA SmartClass", "Screenshots");
                    Directory.CreateDirectory(folder);

                    var fileName = $"{_selected?.Name?.Replace(' ', '_')}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    var filePath = Path.Combine(folder, fileName);

                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bmpSrc));
                    using var fs = File.Create(filePath);
                    encoder.Save(fs);

                    MessageBox.Show($"📸 Đã lưu ảnh màn hình:\n\n{filePath}",
                        "Chụp màn hình", MessageBoxButton.OK, MessageBoxImage.Information);
                    Log.Information("Screenshot saved: {Path}", filePath);
                }
                else
                {
                    MessageBox.Show("Chưa có ảnh màn hình để lưu.", "Chụp màn hình",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SendCommand(string cmd)
        {
            var app = (QASmartTouch.App)Application.Current;
            QASmartTouch.App.LessonState.LastTeacherCommand = cmd;
            QASmartTouch.App.LessonState.LastCommandTime = DateTime.Now;

            var net = app.NetworkService;
            if (net?.IsBroadcasting == true)
                _ = net.SendCommandAsync(cmd);
            else
                app.RaiseLocalCommand(cmd);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            StopScreenCapture();
            _mouseEnabled = false;
            _keyboardEnabled = false;
        }

        // ═══════════════════════════════════════════════════════
        //  🖱️ MOUSE REMOTE CONTROL
        // ═══════════════════════════════════════════════════════

        private void ToggleMouse_Click(object sender, MouseButtonEventArgs e)
        {
            _mouseEnabled = !_mouseEnabled;
            badgeMouse.Background = new SolidColorBrush(_mouseEnabled
                ? Color.FromRgb(232, 245, 233) : Color.FromRgb(224, 224, 224));
            txtMouseBadge.Text = _mouseEnabled ? "🖱️ Chuột: BẬT" : "🖱️ Chuột: TẮT";
            txtMouseBadge.Foreground = new SolidColorBrush(_mouseEnabled
                ? Color.FromRgb(46, 125, 50) : Color.FromRgb(153, 153, 153));
            UpdateRemoteIndicator();
            Log.Information("Mouse remote: {State}", _mouseEnabled ? "ON" : "OFF");
        }

        private void ToggleKeyboard_Click(object sender, MouseButtonEventArgs e)
        {
            _keyboardEnabled = !_keyboardEnabled;
            badgeKeyboard.Background = new SolidColorBrush(_keyboardEnabled
                ? Color.FromRgb(232, 245, 233) : Color.FromRgb(224, 224, 224));
            txtKeyBadge.Text = _keyboardEnabled ? "⌨️ Phím: BẬT" : "⌨️ Phím: TẮT";
            txtKeyBadge.Foreground = new SolidColorBrush(_keyboardEnabled
                ? Color.FromRgb(46, 125, 50) : Color.FromRgb(153, 153, 153));
            UpdateRemoteIndicator();

            if (_keyboardEnabled)
            {
                // Focus the page to receive keyboard input
                this.Focusable = true;
                Keyboard.Focus(this);
                this.KeyDown -= Page_KeyDown;
                this.KeyDown += Page_KeyDown;
                this.KeyUp -= Page_KeyUp;
                this.KeyUp += Page_KeyUp;
            }
            else
            {
                this.KeyDown -= Page_KeyDown;
                this.KeyUp -= Page_KeyUp;
            }
            Log.Information("Keyboard remote: {State}", _keyboardEnabled ? "ON" : "OFF");
        }

        private void UpdateRemoteIndicator()
        {
            if (_mouseEnabled || _keyboardEnabled)
            {
                remoteIndicator.Visibility = Visibility.Visible;
                var parts = new System.Collections.Generic.List<string>();
                if (_mouseEnabled) parts.Add("🖱️ Chuột");
                if (_keyboardEnabled) parts.Add("⌨️ Bàn phím");
                txtRemoteMode.Text = "Điều khiển: " + string.Join(" + ", parts);
                screenImage.Cursor = _mouseEnabled ? Cursors.Cross : Cursors.Arrow;
            }
            else
            {
                remoteIndicator.Visibility = Visibility.Collapsed;
                screenImage.Cursor = Cursors.Arrow;
            }
        }

        /// <summary>Get StudentShell window for coordinate mapping</summary>
        private Window? FindStudentWindow()
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w is QASmartClass.StudentClient.Views.StudentShell)
                    return w;
            }
            return null;
        }

        /// <summary>Get HWND of StudentShell window</summary>
        private IntPtr GetStudentHwnd()
        {
            var sw = FindStudentWindow();
            if (sw == null) return IntPtr.Zero;
            var helper = new System.Windows.Interop.WindowInteropHelper(sw);
            return helper.Handle;
        }

        /// <summary>
        /// Convert click on screenImage → SCREEN coordinates using PointToScreen.
        /// PointToScreen handles DPI + window chrome automatically.
        /// </summary>
        private Point? MapToScreenPoint(Point clickPos)
        {
            var studentWin = FindStudentWindow();
            if (studentWin == null || !studentWin.IsVisible) return null;

            var content = studentWin.Content as FrameworkElement;
            if (content == null || content.ActualWidth <= 0 || content.ActualHeight <= 0)
                return null;

            // ═══ Step 1: Source DIP size (WPF uses DIPs for Uniform stretch) ═══
            double srcDipW, srcDipH;
            if (screenImage.Source is BitmapSource bmp && bmp.DpiX > 0 && bmp.DpiY > 0)
            {
                srcDipW = bmp.PixelWidth * 96.0 / bmp.DpiX;
                srcDipH = bmp.PixelHeight * 96.0 / bmp.DpiY;
            }
            else
            {
                srcDipW = content.ActualWidth;
                srcDipH = content.ActualHeight;
            }
            if (srcDipW <= 0 || srcDipH <= 0) return null;

            // ═══ Step 2: Uniform stretch letterbox (DIPs) ═══
            double ctrlW = screenImage.ActualWidth;
            double ctrlH = screenImage.ActualHeight;
            if (ctrlW <= 0 || ctrlH <= 0) return null;

            double scale = Math.Min(ctrlW / srcDipW, ctrlH / srcDipH);
            double padX = (ctrlW - srcDipW * scale) / 2.0;
            double padY = (ctrlH - srcDipH * scale) / 2.0;

            // ═══ Step 3: Click → relative (0.0-1.0) ═══
            double relX = (clickPos.X - padX) / (srcDipW * scale);
            double relY = (clickPos.Y - padY) / (srcDipH * scale);
            if (relX < 0 || relX > 1 || relY < 0 || relY > 1) return null;

            // ═══ Step 4: Relative → student content DIP point ═══
            double dipX = relX * content.ActualWidth;
            double dipY = relY * content.ActualHeight;

            // ═══ Step 5: PointToScreen → physical screen coordinates ═══
            try
            {
                var screenPt = content.PointToScreen(new Point(dipX, dipY));

                txtRefreshInfo.Text = $"🖱 rel({relX:F2},{relY:F2}) dip({dipX:F0},{dipY:F0}) scr({screenPt.X:F0},{screenPt.Y:F0})";

                return screenPt;
            }
            catch (Exception ex)
            {
                txtRefreshInfo.Text = $"âš  MapError: {ex.Message}";
                return null;
            }
        }

        // ═══ Mouse handlers — async SetCursorPos + mouse_event ═══
        // WPF uses GetCursorPos() internally, so MUST move the real cursor

        private async void ScreenImage_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            if (!_mouseEnabled || _selected == null) return;

            var screenPt = MapToScreenPoint(e.GetPosition(screenImage));
            if (screenPt == null) return;

            int sx = (int)screenPt.Value.X;
            int sy = (int)screenPt.Value.Y;

            // Save teacher window ref BEFORE switching
            var teacherWin = Window.GetWindow(this);

            // 1. Bring student window to front
            var studentWin = FindStudentWindow();
            studentWin?.Activate();
            await Task.Delay(120);

            // 2. Move real cursor + click
            SetCursorPos(sx, sy);
            await Task.Delay(30);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
            await Task.Delay(30);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);

            // 3. Return to teacher
            await Task.Delay(150);
            teacherWin?.Activate();

            Log.Information("Remote CLICK: screen({SX},{SY})", sx, sy);
        }

        private async void ScreenImage_MouseRightDown(object sender, MouseButtonEventArgs e)
        {
            if (!_mouseEnabled || _selected == null) return;

            var screenPt = MapToScreenPoint(e.GetPosition(screenImage));
            if (screenPt == null) return;

            int sx = (int)screenPt.Value.X;
            int sy = (int)screenPt.Value.Y;

            var teacherWin = Window.GetWindow(this);
            var studentWin = FindStudentWindow();
            studentWin?.Activate();
            await Task.Delay(120);

            SetCursorPos(sx, sy);
            await Task.Delay(30);
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, IntPtr.Zero);
            await Task.Delay(30);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, IntPtr.Zero);

            await Task.Delay(150);
            teacherWin?.Activate();
        }

        private void ScreenImage_MouseMove(object sender, MouseEventArgs e)
        {
            // Drag not supported with async approach â€” show position only
            if (!_mouseEnabled || _selected == null) return;
            var screenPt = MapToScreenPoint(e.GetPosition(screenImage));
            // Just updates debug info
        }

        // ═══════════════════════════════════════════════════════
        //  âŒ¨ï¸ KEYBOARD REMOTE CONTROL
        // ═══════════════════════════════════════════════════════

        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_keyboardEnabled || _selected == null) return;

            byte vk = (byte)KeyInterop.VirtualKeyFromKey(e.Key);
            if (vk == 0) return;

            var hwnd = GetStudentHwnd();
            if (hwnd == IntPtr.Zero) return;

            PostMessage(hwnd, WM_KEYDOWN, (IntPtr)vk, IntPtr.Zero);
            e.Handled = true;
            Log.Debug("Remote key down: {Key} (VK={VK})", e.Key, vk);
        }

        private void Page_KeyUp(object sender, KeyEventArgs e)
        {
            if (!_keyboardEnabled || _selected == null) return;

            byte vk = (byte)KeyInterop.VirtualKeyFromKey(e.Key);
            if (vk == 0) return;

            var hwnd = GetStudentHwnd();
            if (hwnd == IntPtr.Zero) return;

            PostMessage(hwnd, WM_KEYUP, (IntPtr)vk, IntPtr.Zero);
            e.Handled = true;
        }
    }
}





