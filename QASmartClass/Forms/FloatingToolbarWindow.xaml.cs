using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace QASmartTouch.Forms
{
    public partial class FloatingToolbarWindow : Window
    {
        private DispatcherTimer? _recDotTimer;
        private DockPosition? _currentDockPosition = null;

        // Events
        public event EventHandler? BackToMainApp;
        public event EventHandler? PenSelected;
        public event EventHandler? MouseModeRequested;
        public event EventHandler? ShapesSelected;
        public event EventHandler? FillSelected;
        public event EventHandler? ClearAllRequested;
        public event EventHandler? UndoRequested;
        public event EventHandler? RedoRequested;
        public event EventHandler? ScreenshotRequested;
        public event EventHandler? WindowModeToggled;
        public event EventHandler? SelectDisplayRequested;
        public event EventHandler? SelectAreaRequested;
        public event EventHandler? RecordToggled;
        public event EventHandler? DeleteLastStrokeRequested;
        
        /// <summary>
        /// Phát ra khi toolbar bị đóng hoặc ẩn đi để các dock button hiện lại
        /// </summary>
        public event EventHandler? ToolbarHidden;

        public DockPosition? CurrentDockPosition => _currentDockPosition;

        public FloatingToolbarWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            
            // Mặc định ban đầu ở cạnh dưới theo chiều ngang
            MoveToolbarToPosition(DockPosition.Bottom);
            
            // Animate record dot
            StartRecDotAnimation();
            
            System.Diagnostics.Debug.WriteLine("✅ FloatingToolbarWindow initialized with Unfurl Animation");
        }
        
        private void StartRecDotAnimation()
        {
            _recDotTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            
            _recDotTimer.Tick += (s, e) =>
            {
                if (recDot != null)
                {
                    recDot.Opacity = recDot.Opacity == 1.0 ? 0.3 : 1.0;
                }
            };
            
            _recDotTimer.Start();
        }

        #region Dynamic Layout Orientation & Unfurl Animation

        /// <summary>
        /// Cấu hình hướng nằm Ngang hoặc Dọc cho toàn bộ Toolbar
        /// </summary>
        public void SetLayoutOrientation(Orientation orientation)
        {
            toolsPanel.Orientation = orientation;
            bool isHoriz = (orientation == Orientation.Horizontal);

            if (isHoriz)
            {
                // Toolbar NẰM NGANG
                btnExit.Margin = new Thickness(0, 0, 6, 0);
                sepTools.Width = 1.5;
                sepTools.Height = 32;
                sepTools.Margin = new Thickness(5, 4, 5, 4);

                btnPen.Margin = new Thickness(2, 0, 2, 0);
                btnMouse.Margin = new Thickness(2, 0, 2, 0);
                btnClearAll.Margin = new Thickness(2, 0, 2, 0);
                btnDeleteStroke.Margin = new Thickness(2, 0, 2, 0);
                btnUndo.Margin = new Thickness(2, 0, 2, 0);
                btnRedo.Margin = new Thickness(2, 0, 2, 0);
                btnSelectArea.Margin = new Thickness(2, 0, 2, 0);
                btnScreenshot.Margin = new Thickness(2, 0, 2, 0);
                btnRecord.Margin = new Thickness(2, 0, 2, 0);
            }
            else
            {
                // Toolbar NẰM DỌC
                btnExit.Margin = new Thickness(0, 0, 0, 6);
                sepTools.Width = double.NaN;
                sepTools.Height = 1.5;
                sepTools.Margin = new Thickness(4, 5, 4, 5);

                btnPen.Margin = new Thickness(0, 2, 0, 2);
                btnMouse.Margin = new Thickness(0, 2, 0, 2);
                btnClearAll.Margin = new Thickness(0, 2, 0, 2);
                btnDeleteStroke.Margin = new Thickness(0, 2, 0, 2);
                btnUndo.Margin = new Thickness(0, 2, 0, 2);
                btnRedo.Margin = new Thickness(0, 2, 0, 2);
                btnSelectArea.Margin = new Thickness(0, 2, 0, 2);
                btnScreenshot.Margin = new Thickness(0, 2, 0, 2);
                btnRecord.Margin = new Thickness(0, 2, 0, 2);
            }

            this.UpdateLayout();
        }

        /// <summary>
        /// Hiệu ứng bung tràn ra mượt mà (Unfurl Animation) từ icon thành toàn bộ thanh toolbar
        /// </summary>
        private void PlayUnfurlAnimation(Orientation orientation, DockPosition position)
        {
            try
            {
                var duration = TimeSpan.FromMilliseconds(220);
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

                // Thiết lập tâm nở (RenderTransformOrigin) theo đúng vị trí nút bấm ở mép cạnh:
                // - Cạnh Trái: Nở tràn từ mép trái ra (X=0.0, Y=0.5)
                // - Cạnh Phải: Nở tràn từ mép phải vào (X=1.0, Y=0.5)
                // - Cạnh Trên: Nở tràn từ mép trên xuống (X=0.5, Y=0.0)
                // - Cạnh Dưới: Nở tràn từ mép dưới lên (X=0.5, Y=1.0)
                switch (position)
                {
                    case DockPosition.Left:
                        mainBorder.RenderTransformOrigin = new Point(0.0, 0.5);
                        break;
                    case DockPosition.Right:
                        mainBorder.RenderTransformOrigin = new Point(1.0, 0.5);
                        break;
                    case DockPosition.Top:
                        mainBorder.RenderTransformOrigin = new Point(0.5, 0.0);
                        break;
                    case DockPosition.Bottom:
                        mainBorder.RenderTransformOrigin = new Point(0.5, 1.0);
                        break;
                }

                if (orientation == Orientation.Vertical)
                {
                    // Tràn ra theo chiều dọc (chiều dài dãn dài và chiều ngang bung mở)
                    var scaleXAnim = new DoubleAnimation(0.25, 1.0, duration) { EasingFunction = ease };
                    var scaleYAnim = new DoubleAnimation(0.08, 1.0, duration) { EasingFunction = ease };
                    toolbarScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
                    toolbarScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
                }
                else
                {
                    // Tràn ra theo chiều ngang (chiều ngang trải rộng và chiều cao bung mở)
                    var scaleXAnim = new DoubleAnimation(0.08, 1.0, duration) { EasingFunction = ease };
                    var scaleYAnim = new DoubleAnimation(0.25, 1.0, duration) { EasingFunction = ease };
                    toolbarScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
                    toolbarScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
                }

                // Fade in nhẹ nhàng hòa quyện
                var opacityAnim = new DoubleAnimation(0.1, 1.0, duration) { EasingFunction = ease };
                this.BeginAnimation(OpacityProperty, opacityAnim);
            }
            catch { }
        }

        #region Monitor Native API
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [System.Runtime.InteropServices.DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT { public int left, top, right, bottom; }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }

        private IntPtr _targetMonitor = IntPtr.Zero;
        public IntPtr TargetMonitor => _targetMonitor;

        public void SetTargetMonitor(IntPtr monitor)
        {
            _targetMonitor = monitor;
            if (_currentDockPosition.HasValue)
                MoveToolbarToPosition(_currentDockPosition.Value);
        }
        #endregion

        /// <summary>
        /// Di chuyển toolbar đến cạnh được chọn:
        /// - Cạnh Nằm Dọc (Left, Right) ➔ Toolbar NẰM DỌC
        /// - Cạnh Nằm Ngang (Top, Bottom) ➔ Toolbar NẰM NGANG
        /// Kèm hiệu ứng bung tràn ra từ vị trí nút bấm
        /// </summary>
        public void MoveToolbarToPosition(DockPosition position)
        {
            try
            {
                // Nếu bấm vào đúng cạnh đang mở ➔ Toggle Ẩn đi và thông báo hiện lại icon
                if (_currentDockPosition == position && this.Visibility == Visibility.Visible)
                {
                    this.Hide();
                    _currentDockPosition = null;
                    ToolbarHidden?.Invoke(this, EventArgs.Empty);
                    System.Diagnostics.Debug.WriteLine($"📍 Toolbar toggled OFF at {position}");
                    return;
                }

                _currentDockPosition = position;

                bool isVertical = (position == DockPosition.Left || position == DockPosition.Right);
                var orientation = isVertical ? Orientation.Vertical : Orientation.Horizontal;

                SetLayoutOrientation(orientation);

                this.Show();
                this.Activate();
                this.UpdateLayout();

                double w = this.ActualWidth > 0 ? this.ActualWidth : (isVertical ? 68 : 520);
                double h = this.ActualHeight > 0 ? this.ActualHeight : (isVertical ? 500 : 58);

                // Lấy thông tin màn hình vật lý
                IntPtr monitor = _targetMonitor;
                if (monitor == IntPtr.Zero)
                {
                    Window? targetWindow = null;
                    foreach (Window win in Application.Current.Windows)
                    {
                        if (win.GetType().Name == "AnnotationOverlay" && win.IsVisible) { targetWindow = win; break; }
                    }
                    if (targetWindow == null) targetWindow = Application.Current.MainWindow;

                    if (targetWindow != null)
                    {
                        var hwndTarget = new System.Windows.Interop.WindowInteropHelper(targetWindow).Handle;
                        if (hwndTarget != IntPtr.Zero)
                            monitor = MonitorFromWindow(hwndTarget, 2 /* MONITOR_DEFAULTTONEAREST */);
                    }
                }

                var info = new MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(MONITORINFO)) };
                if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref info))
                {
                    uint dpiX = 96, dpiY = 96;
                    try { GetDpiForMonitor(monitor, 0 /* MDT_EFFECTIVE_DPI */, out dpiX, out dpiY); } catch { }
                    if (dpiX == 0) dpiX = 96;
                    if (dpiY == 0) dpiY = 96;

                    double scaleX = dpiX / 96.0;
                    double scaleY = dpiY / 96.0;

                    int tbPhysW = (int)Math.Round(w * scaleX);
                    int tbPhysH = (int)Math.Round(h * scaleY);
                    int marginPhys = (int)Math.Round(8 * scaleX);

                    int physX = 0;
                    int physY = 0;

                    switch (position)
                    {
                        case DockPosition.Left:
                            physX = info.rcWork.left + marginPhys;
                            physY = info.rcWork.top + Math.Max(10, (info.rcWork.bottom - info.rcWork.top - tbPhysH) / 2);
                            break;

                        case DockPosition.Right:
                            physX = info.rcWork.right - tbPhysW - marginPhys;
                            physY = info.rcWork.top + Math.Max(10, (info.rcWork.bottom - info.rcWork.top - tbPhysH) / 2);
                            break;

                        case DockPosition.Top:
                            physX = info.rcWork.left + (info.rcWork.right - info.rcWork.left - tbPhysW) / 2;
                            physY = info.rcWork.top + marginPhys;
                            break;

                        case DockPosition.Bottom:
                            physX = info.rcWork.left + (info.rcWork.right - info.rcWork.left - tbPhysW) / 2;
                            physY = info.rcWork.bottom - tbPhysH - marginPhys;
                            break;
                    }

                    // Clamping Guard (Tuyệt đối không tràn)
                    if (physX < info.rcWork.left + marginPhys)
                        physX = info.rcWork.left + marginPhys;
                    if (physX + tbPhysW > info.rcWork.right - marginPhys)
                        physX = info.rcWork.right - tbPhysW - marginPhys;
                    if (physY < info.rcWork.top + marginPhys)
                        physY = info.rcWork.top + marginPhys;
                    if (physY + tbPhysH > info.rcWork.bottom - marginPhys)
                        physY = info.rcWork.bottom - tbPhysH - marginPhys;

                    this.Left = physX / scaleX;
                    this.Top = physY / scaleY;

                    var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        SetWindowPos(hwnd, IntPtr.Zero, physX, physY, tbPhysW, tbPhysH, SWP_NOZORDER | SWP_NOACTIVATE);
                    }
                }

                // Kích hoạt hiệu ứng tràn ra từ mép cạnh nút bấm
                PlayUnfurlAnimation(orientation, position);

                System.Diagnostics.Debug.WriteLine($"📍 Toolbar unfurled at {position} (Orientation={orientation})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MoveToolbarToPosition error: {ex.Message}");
            }
        }

        public void MoveToolbarToSide(bool isLeftSide)
        {
            MoveToolbarToPosition(isLeftSide ? DockPosition.Left : DockPosition.Right);
        }

        #endregion

        #region Button Click Handlers

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Exit Window Mode clicked");
            _currentDockPosition = null;
            ToolbarHidden?.Invoke(this, EventArgs.Empty);
            BackToMainApp?.Invoke(this, EventArgs.Empty);
        }

        private void btnPen_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("✏️ Pen selected");
            PenSelected?.Invoke(this, EventArgs.Empty);
        }

        private void btnMouse_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🖥️ Mouse mode requested");
            MouseModeRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnShapes_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📐 Shapes selected");
            ShapesSelected?.Invoke(this, EventArgs.Empty);
        }

        private void btnFill_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🎨 Fill selected");
            FillSelected?.Invoke(this, EventArgs.Empty);
        }

        private void btnClearAll_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🗑️ Clear all requested");
            ClearAllRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnDeleteStroke_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("❌ Delete last stroke requested");
            DeleteLastStrokeRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("↶ Undo requested");
            UndoRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnRedo_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("↷ Redo requested");
            RedoRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📷 Screenshot requested");
            ScreenshotRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnWindowMode_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🪟 Window Mode toggled");
            WindowModeToggled?.Invoke(this, EventArgs.Empty);
        }

        private void btnSelectDisplay_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📺 Select display requested");
            SelectDisplayRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnSelectArea_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📋 Select area requested");
            SelectAreaRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnRecord_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("⏺️ Record toggled");
            RecordToggled?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Public Methods
        
        /// <summary>
        /// Set recording state
        /// </summary>
        public void SetRecordingState(bool isRecording)
        {
            if (isRecording)
            {
                btnRecord.Background = new SolidColorBrush(Color.FromRgb(191, 97, 106)); // Red
            }
            else
            {
                btnRecord.Background = new SolidColorBrush(Color.FromRgb(46, 52, 64)); // Dark
            }
        }

        /// <summary>
        /// Toggle visual state of Delete Stroke button (highlight when erase mode is active)
        /// </summary>
        public void SetDeleteStrokeButtonActive(bool isActive)
        {
            if (isActive)
            {
                btnDeleteStroke.Background = new SolidColorBrush(Color.FromRgb(191, 97, 106)); // Red
                btnPen.Background = new SolidColorBrush(Color.FromRgb(46, 52, 64));
            }
            else
            {
                btnDeleteStroke.Background = new SolidColorBrush(Color.FromRgb(46, 52, 64));
            }
        }

        /// <summary>
        /// Update enabled state of Undo/Redo buttons
        /// </summary>
        public void UpdateUndoRedoButtonsState(bool canUndo, bool canRedo)
        {
            if (btnUndo != null)
            {
                btnUndo.IsEnabled = canUndo;
            }
            if (btnRedo != null)
            {
                btnRedo.IsEnabled = canRedo;
            }
        }

        /// <summary>
        /// Set active tool button (highlight selected tool, reset all others)
        /// </summary>
        public void SetActiveToolButton(string toolName)
        {
            var activeColor  = new SolidColorBrush(Color.FromRgb(92, 107, 192));  // #5C6BC0 Indigo
            var mouseColor   = new SolidColorBrush(Color.FromRgb(92, 107, 192));  // #5C6BC0 Indigo
            var defaultColor = Brushes.Transparent;
            var defaultIconColor = new SolidColorBrush(Color.FromRgb(55, 71, 79)); // #37474F Dark Gray

            btnPen.Background         = defaultColor;
            btnMouse.Background       = defaultColor;
            btnDeleteStroke.Background = defaultColor;

            SetPathFill(btnPen, defaultIconColor);
            SetPathFill(btnMouse, defaultIconColor);
            SetPathFill(btnDeleteStroke, defaultIconColor);

            switch (toolName.ToLower())
            {
                case "pen":
                    btnPen.Background = activeColor;
                    SetPathFill(btnPen, Brushes.White);
                    break;
                case "mouse":
                    btnMouse.Background = mouseColor;
                    SetPathFill(btnMouse, Brushes.White);
                    break;
                case "erase":
                    btnDeleteStroke.Background = new SolidColorBrush(Color.FromRgb(211, 47, 47)); // Red
                    SetPathFill(btnDeleteStroke, Brushes.White);
                    break;
                case "none":
                default:
                    break;
            }

            System.Diagnostics.Debug.WriteLine($"🎨 Active tool: {toolName}");
        }

        private void SetPathFill(DependencyObject parent, Brush brush)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is System.Windows.Shapes.Path path)
                {
                    path.Fill = brush;
                    return;
                }
                SetPathFill(child, brush);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _recDotTimer?.Stop();
            _recDotTimer = null;
        }

        #endregion
    }
}
