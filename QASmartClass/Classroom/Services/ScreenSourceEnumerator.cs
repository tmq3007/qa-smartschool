using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace QASmartClass.Classroom.Services
{
    public enum ScreenSourceKind { Screen, Window }

    /// <summary>Một nguồn chiếu có thể chọn: một màn hình hoặc một cửa sổ ứng dụng.</summary>
    public sealed class ScreenSourceInfo
    {
        public ScreenSourceKind Kind { get; init; }
        public string Title { get; init; } = "";
        public IntPtr Hwnd { get; init; }
        /// <summary>Vùng màn hình theo pixel vật lý (dùng cho Kind = Screen).</summary>
        public int X { get; init; }
        public int Y { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public bool IsPrimary { get; init; }
        public BitmapSource? Thumbnail { get; set; }
    }

    /// <summary>
    /// Liệt kê màn hình + cửa sổ ứng dụng và tạo ảnh xem trước (kiểu hộp chọn của Google Meet).
    /// Không phụ thuộc WinForms; chỉ dùng Win32.
    /// </summary>
    public static class ScreenSourceEnumerator
    {
        private const int ThumbMaxW = 320;
        private const int ThumbMaxH = 180;

        #region Win32
        private delegate bool EnumMonitorProc(IntPtr hMon, IntPtr hdc, ref RECT rc, IntPtr data);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MONITORINFOEX
        {
            public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string szDevice;
        }

        [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, EnumMonitorProc proc, IntPtr data);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr hMon, ref MONITORINFOEX mi);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder sb, int max);
        [DllImport("user32.dll")] private static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int index);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT rc);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);
        [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr hWnd, int attr, out int value, int size);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr dst, int dx, int dy, int w, int h, IntPtr src, int sx, int sy, int rop);
        [DllImport("gdi32.dll")] private static extern bool StretchBlt(IntPtr dst, int dx, int dy, int dw, int dh, IntPtr src, int sx, int sy, int sw, int sh, int rop);
        [DllImport("gdi32.dll")] private static extern int SetStretchBltMode(IntPtr hdc, int mode);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const int DWMWA_CLOAKED = 14;
        private const int SRCCOPY = 0x00CC0020;
        private const int HALFTONE = 4;
        private const uint PW_RENDERFULLCONTENT = 2;
        #endregion

        /// <summary>Kiểm tra cửa sổ có còn tồn tại và hiển thị được không.</summary>
        public static bool IsWindowAlive(IntPtr hWnd) => hWnd != IntPtr.Zero && IsWindowVisible(hWnd);

        public static bool IsMinimized(IntPtr hWnd) => hWnd != IntPtr.Zero && IsIconic(hWnd);

        /// <summary>Danh sách màn hình, màn hình chính đứng đầu.</summary>
        public static List<ScreenSourceInfo> GetScreens()
        {
            var list = new List<ScreenSourceInfo>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMon, IntPtr hdc, ref RECT rc, IntPtr data) =>
            {
                var mi = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
                if (GetMonitorInfo(hMon, ref mi))
                {
                    list.Add(new ScreenSourceInfo
                    {
                        Kind = ScreenSourceKind.Screen,
                        X = mi.rcMonitor.Left,
                        Y = mi.rcMonitor.Top,
                        Width = mi.rcMonitor.Right - mi.rcMonitor.Left,
                        Height = mi.rcMonitor.Bottom - mi.rcMonitor.Top,
                        IsPrimary = (mi.dwFlags & 1) != 0
                    });
                }
                return true;
            }, IntPtr.Zero);

            list.Sort((a, b) => b.IsPrimary.CompareTo(a.IsPrimary));
            return list;
        }

        /// <summary>Tìm màn hình đang chứa điểm (x,y); dự phòng màn hình chính.</summary>
        public static ScreenSourceInfo? FindScreen(int x, int y, int w, int h)
        {
            foreach (var s in GetScreens())
                if (s.X == x && s.Y == y && s.Width == w && s.Height == h) return s;
            var all = GetScreens();
            return all.Count > 0 ? all[0] : null;
        }

        /// <summary>Danh sách cửa sổ ứng dụng cấp cao nhất có thể chiếu.</summary>
        public static List<ScreenSourceInfo> GetWindows(IntPtr excludeHwnd)
        {
            var list = new List<ScreenSourceInfo>();
            EnumWindows((hWnd, _) =>
            {
                try
                {
                    if (hWnd == excludeHwnd || !IsWindowVisible(hWnd)) return true;
                    if ((GetWindowLong(hWnd, GWL_EXSTYLE) & WS_EX_TOOLWINDOW) != 0) return true;
                    if (DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0) return true;
                    int len = GetWindowTextLength(hWnd);
                    if (len <= 0) return true;
                    var sb = new StringBuilder(len + 1);
                    GetWindowText(hWnd, sb, sb.Capacity);
                    string title = sb.ToString();
                    if (string.IsNullOrWhiteSpace(title) || title == "Program Manager") return true;
                    if (!GetWindowRect(hWnd, out var rc)) return true;
                    int w = rc.Right - rc.Left, h = rc.Bottom - rc.Top;
                    if (!IsIconic(hWnd) && (w < 80 || h < 60)) return true;

                    GetWindowThreadProcessId(hWnd, out uint pid);
                    if (pid == (uint)Environment.ProcessId && hWnd == excludeHwnd) return true;

                    list.Add(new ScreenSourceInfo
                    {
                        Kind = ScreenSourceKind.Window,
                        Title = title,
                        Hwnd = hWnd,
                        X = rc.Left, Y = rc.Top, Width = w, Height = h
                    });
                }
                catch { /* bỏ qua cửa sổ lỗi */ }
                return true;
            }, IntPtr.Zero);
            return list;
        }

        /// <summary>Tạo ảnh xem trước (đã Freeze, dùng được từ mọi luồng). Trả null nếu không chụp được.</summary>
        public static BitmapSource? CaptureThumbnail(ScreenSourceInfo src)
        {
            IntPtr hdcScreen = IntPtr.Zero, hdcFull = IntPtr.Zero, hbmFull = IntPtr.Zero, hOldFull = IntPtr.Zero;
            IntPtr hdcThumb = IntPtr.Zero, hbmThumb = IntPtr.Zero, hOldThumb = IntPtr.Zero;
            try
            {
                int w = src.Width, h = src.Height;
                if (w <= 0 || h <= 0) return null;
                double k = Math.Min((double)ThumbMaxW / w, (double)ThumbMaxH / h);
                int tw = Math.Max(1, (int)(w * k)), th = Math.Max(1, (int)(h * k));

                hdcScreen = GetDC(IntPtr.Zero);
                hdcThumb = CreateCompatibleDC(hdcScreen);
                hbmThumb = CreateCompatibleBitmap(hdcScreen, tw, th);
                hOldThumb = SelectObject(hdcThumb, hbmThumb);
                SetStretchBltMode(hdcThumb, HALFTONE);

                if (src.Kind == ScreenSourceKind.Screen)
                {
                    StretchBlt(hdcThumb, 0, 0, tw, th, hdcScreen, src.X, src.Y, w, h, SRCCOPY);
                }
                else
                {
                    if (IsIconic(src.Hwnd)) return null;
                    hdcFull = CreateCompatibleDC(hdcScreen);
                    hbmFull = CreateCompatibleBitmap(hdcScreen, w, h);
                    hOldFull = SelectObject(hdcFull, hbmFull);
                    if (!PrintWindow(src.Hwnd, hdcFull, PW_RENDERFULLCONTENT)) return null;
                    SetStretchBltMode(hdcThumb, HALFTONE);
                    StretchBlt(hdcThumb, 0, 0, tw, th, hdcFull, 0, 0, w, h, SRCCOPY);
                }

                SelectObject(hdcThumb, hOldThumb); hOldThumb = IntPtr.Zero;
                var bmp = Imaging.CreateBitmapSourceFromHBitmap(hbmThumb, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
            finally
            {
                if (hOldFull != IntPtr.Zero) SelectObject(hdcFull, hOldFull);
                if (hbmFull != IntPtr.Zero) DeleteObject(hbmFull);
                if (hdcFull != IntPtr.Zero) DeleteDC(hdcFull);
                if (hOldThumb != IntPtr.Zero) SelectObject(hdcThumb, hOldThumb);
                if (hbmThumb != IntPtr.Zero) DeleteObject(hbmThumb);
                if (hdcThumb != IntPtr.Zero) DeleteDC(hdcThumb);
                if (hdcScreen != IntPtr.Zero) ReleaseDC(IntPtr.Zero, hdcScreen);
            }
        }
    }
}
