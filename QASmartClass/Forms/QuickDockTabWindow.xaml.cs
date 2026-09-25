using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace QASmartTouch.Forms
{
    public enum DockPosition
    {
        Left,
        Right,
        Top,
        Bottom
    }

    public partial class QuickDockTabWindow : Window
    {
        private readonly DockPosition _position;
        public DockPosition Position => _position;

        private IntPtr _targetMonitor = IntPtr.Zero;
        public IntPtr TargetMonitor => _targetMonitor;

        public event EventHandler<DockPosition>? DockTabClicked;

        public QuickDockTabWindow(DockPosition position, IntPtr targetMonitor = default)
        {
            InitializeComponent();
            _position = position;
            _targetMonitor = targetMonitor;

            this.WindowStartupLocation = WindowStartupLocation.Manual;
            ConfigureAppearance();
            
            PositionWindow();
            this.Loaded += (s, e) => PositionWindow();
            this.IsVisibleChanged += (s, e) => { if (this.IsVisible) PositionWindow(); };
        }

        public void SetTargetMonitor(IntPtr monitor)
        {
            _targetMonitor = monitor;
            PositionWindow();
        }

        private void ConfigureAppearance()
        {
            switch (_position)
            {
                case DockPosition.Left:
                    bdrTab.ToolTip = "Mở thanh công cụ ở CẠNH TRÁI";
                    break;

                case DockPosition.Right:
                    bdrTab.ToolTip = "Mở thanh công cụ ở CẠNH PHẢI";
                    break;

                case DockPosition.Top:
                    bdrTab.ToolTip = "Mở thanh công cụ ở CẠNH TRÊN";
                    break;

                case DockPosition.Bottom:
                    bdrTab.ToolTip = "Mở thanh công cụ ở CẠNH DƯỚI";
                    break;
            }
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
        #endregion

        public void PositionWindow()
        {
            try
            {
                IntPtr monitor = _targetMonitor;
                if (monitor == IntPtr.Zero)
                {
                    // Lấy window overlay đang mở (WindowMode) hoặc MainWindow làm mốc để xác định màn hình
                    Window? targetWindow = null;
                    foreach (Window w in Application.Current.Windows)
                    {
                        if (w.GetType().Name == "AnnotationOverlay" && w.IsVisible) { targetWindow = w; break; }
                    }
                    if (targetWindow == null) targetWindow = Application.Current.MainWindow;

                    if (targetWindow != null)
                    {
                        var hwndTarget = new System.Windows.Interop.WindowInteropHelper(targetWindow).Handle;
                        if (hwndTarget != IntPtr.Zero)
                        {
                            monitor = MonitorFromWindow(hwndTarget, 2 /* MONITOR_DEFAULTTONEAREST */);
                        }
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

                    int btnPhysW = (int)Math.Round((this.ActualWidth > 0 ? this.ActualWidth : (this.Width > 0 ? this.Width : 44)) * scaleX);
                    int btnPhysH = (int)Math.Round((this.ActualHeight > 0 ? this.ActualHeight : (this.Height > 0 ? this.Height : 44)) * scaleY);
                    int marginPhys = (int)Math.Round(12 * scaleX);

                    int physX = 0;
                    int physY = 0;

                    switch (_position)
                    {
                        case DockPosition.Left:
                            physX = info.rcWork.left + marginPhys;
                            physY = info.rcWork.top + Math.Max(10, (info.rcWork.bottom - info.rcWork.top - btnPhysH) / 2);
                            break;

                        case DockPosition.Right:
                            physX = info.rcWork.right - btnPhysW - marginPhys;
                            physY = info.rcWork.top + Math.Max(10, (info.rcWork.bottom - info.rcWork.top - btnPhysH) / 2);
                            break;

                        case DockPosition.Top:
                            physX = info.rcWork.left + (info.rcWork.right - info.rcWork.left - btnPhysW) / 2;
                            physY = info.rcWork.top + marginPhys;
                            break;

                        case DockPosition.Bottom:
                            physX = info.rcWork.left + (info.rcWork.right - info.rcWork.left - btnPhysW) / 2;
                            physY = info.rcWork.bottom - btnPhysH - marginPhys;
                            break;
                    }

                    // Giới hạn an toàn (Clamping Guard) tuyệt đối 100% trong màn hình mục tiêu, không thể tràn sang màn hình 2
                    if (physX < info.rcWork.left + marginPhys)
                        physX = info.rcWork.left + marginPhys;
                    if (physX + btnPhysW > info.rcWork.right - marginPhys)
                        physX = info.rcWork.right - btnPhysW - marginPhys;
                    if (physY < info.rcWork.top + marginPhys)
                        physY = info.rcWork.top + marginPhys;
                    if (physY + btnPhysH > info.rcWork.bottom - marginPhys)
                        physY = info.rcWork.bottom - btnPhysH - marginPhys;

                    // Cập nhật vị trí WPF DIPs
                    this.Left = physX / scaleX;
                    this.Top = physY / scaleY;

                    // Nếu HWND đã tồn tại, dùng SetWindowPos để ghim chuẩn xác đến từng pixel vật lý
                    var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        SetWindowPos(hwnd, IntPtr.Zero, physX, physY, btnPhysW, btnPhysH, SWP_NOZORDER | SWP_NOACTIVATE);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PositionWindow error: {ex.Message}");
            }
        }

        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ApplyFeedbackAndTrigger();
            e.Handled = true;
        }

        private void Window_TouchDown(object sender, TouchEventArgs e)
        {
            ApplyFeedbackAndTrigger();
            e.Handled = true;
        }

        private void ApplyFeedbackAndTrigger()
        {
            try
            {
                // Touch visual feedback
                bdrTab.Background = new SolidColorBrush(Color.FromRgb(0xC5, 0xCA, 0xE9));
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(80)
                };
                timer.Tick += (s, ev) =>
                {
                    timer.Stop();
                    bdrTab.Background = Brushes.White;
                };
                timer.Start();

                DockTabClicked?.Invoke(this, _position);
                System.Diagnostics.Debug.WriteLine($"🎯 QuickDockTab clicked at: {_position}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DockTab error: {ex.Message}");
            }
        }
    }
}
