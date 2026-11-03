using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QASmartClass.Classroom.Services
{
    public enum ScreenSourceKind
    {
        Screen,
        Window,
        Region,
        QaClassPage
    }

    /// <summary>Một nguồn chiếu có thể chọn: màn hình, cửa sổ ứng dụng, vùng màn hình tùy chọn, hoặc trang bên trong QA SmartClass.</summary>
    public sealed class ScreenSourceInfo
    {
        public ScreenSourceKind Kind { get; init; }
        public string Title { get; init; } = "";
        public string Subtitle { get; init; } = "";
        public string IconGlyph { get; init; } = "🖥️";
        public IntPtr Hwnd { get; init; }
        /// <summary>Mã trang nội bộ trong QA SmartClass (dùng khi Kind = QaClassPage, vd: "CURRENT", "SMART_TOUCH", "F2", "F20", "F32"...).</summary>
        public string PageFormId { get; init; } = "";
        /// <summary>Tự động điều hướng tới trang QA SmartClass khi chọn nguồn này.</summary>
        public bool AutoNavigateToPage { get; set; }
        /// <summary>Vùng màn hình theo pixel vật lý (dùng cho Kind = Screen hoặc Region).</summary>
        public int X { get; init; }
        public int Y { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        /// <summary>Tọa độ DIP của WPF (dùng để vẽ khung viền vùng chọn trên màn hình).</summary>
        public double DipLeft { get; init; }
        public double DipTop { get; init; }
        public double DipWidth { get; init; }
        public double DipHeight { get; init; }
        public bool IsPrimary { get; init; }
        public BitmapSource? Thumbnail { get; set; }
    }

    /// <summary>
    /// Liệt kê màn hình + vùng màn hình + trang bên trong QA SmartClass + cửa sổ ứng dụng và tạo ảnh xem trước.
    /// Không phụ thuộc WinForms; chỉ dùng Win32 + WPF Visual.
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
                        IconGlyph = "🖥️",
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

        /// <summary>Danh sách các trang / phân hệ bên trong QA SmartClass có thể chiếu trực tiếp cho học sinh.</summary>
        public static List<ScreenSourceInfo> GetQaClassPages()
        {
            return new List<ScreenSourceInfo>
            {
                new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.QaClassPage,
                    PageFormId = "CURRENT",
                    IconGlyph = "✨",
                    Title = "Trang đang mở trên SMART CLASS (Tự động)",
                    Subtitle = "Tự động chiếu trang bài học bạn mở (ẩn thanh menu trái & thanh công cụ của GV)"
                },
                new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.QaClassPage,
                    PageFormId = "SMART_TOUCH",
                    IconGlyph = "🎨",
                    Title = "Bảng trắng tương tác SMART TOUCH",
                    Subtitle = "Chiếu trực tiếp bảng viết phấn / bút cảm ứng SMART TOUCH"
                },
                new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.QaClassPage,
                    PageFormId = "F_RUNNER",
                    IconGlyph = "▶️",
                    Title = "Tiết học đang trình chiếu (Lesson Runner)",
                    Subtitle = "Chiếu tiến trình 6 giai đoạn giảng dạy của tiết học đang mở"
                },
                new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.QaClassPage,
                    PageFormId = "F2",
                    IconGlyph = "📚",
                    Title = "Trang Bài giảng & Giáo án",
                    Subtitle = "Danh sách bài giảng, giáo án điện tử và trình xem tài liệu"
                },
                new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.QaClassPage,
                    PageFormId = "F20",
                    IconGlyph = "🖌️",
                    Title = "Trang Bảng vẽ Bài giảng (Canvas)",
                    Subtitle = "Bảng vẽ minh họa nhanh tích hợp trong SMART CLASS"
                },
                new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.QaClassPage,
                    PageFormId = "F32",
                    IconGlyph = "🧪",
                    Title = "Trang Công cụ Học tập & Mô phỏng",
                    Subtitle = "Bảng tuần hoàn hóa học, công cụ Toán - Lý - Hóa - Sinh trực quan"
                },
                new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.QaClassPage,
                    PageFormId = "F31",
                    IconGlyph = "🔬",
                    Title = "Trang Công cụ STEM & Thí nghiệm ảo",
                    Subtitle = "Mô phỏng thí nghiệm khoa học và hoạt động STEM"
                },
                new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.QaClassPage,
                    PageFormId = "F34",
                    IconGlyph = "🌍",
                    Title = "Trang Ứng dụng Thực tế",
                    Subtitle = "Hình ảnh trực quan và bài học gắn liền thực tiễn"
                },
                new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.QaClassPage,
                    PageFormId = "F6",
                    IconGlyph = "📝",
                    Title = "Trang Kiểm tra & Đánh giá (Quiz)",
                    Subtitle = "Chiếu đề kiểm tra và bảng thống kê kết quả làm bài"
                },
                new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.QaClassPage,
                    PageFormId = "F18",
                    IconGlyph = "📊",
                    Title = "Trang Khảo sát nhanh (Survey)",
                    Subtitle = "Chiếu biểu đồ kết quả thăm dò ý kiến trực tiếp"
                },
                new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.QaClassPage,
                    PageFormId = "F23",
                    IconGlyph = "🎡",
                    Title = "Trang Vòng quay Gọi tên Ngẫu nhiên",
                    Subtitle = "Chiếu vòng quay may mắn khi gọi học sinh trả lời"
                },
                new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.QaClassPage,
                    PageFormId = "F22",
                    IconGlyph = "⏱️",
                    Title = "Trang Đồng hồ Bấm giờ (Timer)",
                    Subtitle = "Chiếu đồng hồ đếm ngược thời gian thảo luận nhóm / làm bài"
                },
                new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.QaClassPage,
                    PageFormId = "F21",
                    IconGlyph = "📈",
                    Title = "Trang Biểu đồ & Thống kê Lớp học",
                    Subtitle = "Chiếu biểu đồ phân tích học tập và thi đua"
                }
            };
        }

        /// <summary>Danh sách các mẫu vùng màn hình dựng sẵn (Presets) để chọn nhanh 1 chạm.</summary>
        public static List<ScreenSourceInfo> GetRegionPresets(double dpiScaleX = 1.0, double dpiScaleY = 1.0)
        {
            var screens = GetScreens();
            var primary = screens.Count > 0 ? screens[0] : new ScreenSourceInfo { Kind = ScreenSourceKind.Screen, X = 0, Y = 0, Width = 1920, Height = 1080, IsPrimary = true };
            int sw = Math.Max(800, primary.Width);
            int sh = Math.Max(600, primary.Height);
            double sx = dpiScaleX > 0 ? dpiScaleX : 1.0;
            double sy = dpiScaleY > 0 ? dpiScaleY : 1.0;

            // Vùng nội dung bài học trong ClassroomShell (bỏ thanh menu trái ~250dp và thanh top ~64dp)
            int sidePx = (int)(250 * sx);
            int topPx = (int)(64 * sy);
            int contentW = Math.Max(400, sw - sidePx);
            int contentH = Math.Max(300, sh - topPx);

            // Vùng 16:9 chính giữa màn hình (75% diện tích)
            int centerW = (int)(sw * 0.75);
            int centerH = (int)(centerW * 9.0 / 16.0);
            if (centerH > (int)(sh * 0.8))
            {
                centerH = (int)(sh * 0.8);
                centerW = (int)(centerH * 16.0 / 9.0);
            }
            int centerX = primary.X + (sw - centerW) / 2;
            int centerY = primary.Y + (sh - centerH) / 2;

            return new List<ScreenSourceInfo>
            {
                new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.Region,
                    IconGlyph = "📐",
                    Title = "Khung nội dung SMART CLASS (Ẩn menu trái & thanh trên)",
                    Subtitle = $"{contentW}×{contentH} px — Chỉ chiếu phần trang nội dung bên phải",
                    X = primary.X + sidePx,
                    Y = primary.Y + topPx,
                    Width = contentW,
                    Height = contentH,
                    DipLeft = (primary.X + sidePx) / sx,
                    DipTop = (primary.Y + topPx) / sy,
                    DipWidth = contentW / sx,
                    DipHeight = contentH / sy
                },
                new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.Region,
                    IconGlyph = "🎯",
                    Title = "Vùng trung tâm 16:9 (75% màn hình)",
                    Subtitle = $"{centerW}×{centerH} px — Tập trung vào giữa màn hình",
                    X = centerX,
                    Y = centerY,
                    Width = centerW,
                    Height = centerH,
                    DipLeft = centerX / sx,
                    DipTop = centerY / sy,
                    DipWidth = centerW / sx,
                    DipHeight = centerH / sy
                },
                new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.Region,
                    IconGlyph = "◧",
                    Title = "Nửa trái màn hình (Split-Screen Trái)",
                    Subtitle = $"{sw / 2}×{sh} px — Chiếu nửa màn hình bên trái",
                    X = primary.X,
                    Y = primary.Y,
                    Width = sw / 2,
                    Height = sh,
                    DipLeft = primary.X / sx,
                    DipTop = primary.Y / sy,
                    DipWidth = (sw / 2) / sx,
                    DipHeight = sh / sy
                },
                new ScreenSourceInfo
                {
                    Kind = ScreenSourceKind.Region,
                    IconGlyph = "◨",
                    Title = "Nửa phải màn hình (Split-Screen Phải)",
                    Subtitle = $"{sw / 2}×{sh} px — Chiếu nửa màn hình bên phải",
                    X = primary.X + sw / 2,
                    Y = primary.Y,
                    Width = sw / 2,
                    Height = sh,
                    DipLeft = (primary.X + sw / 2) / sx,
                    DipTop = primary.Y / sy,
                    DipWidth = (sw / 2) / sx,
                    DipHeight = sh / sy
                }
            };
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
                        IconGlyph = "🪟",
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

        /// <summary>Chụp một FrameworkElement của WPF thành BitmapSource (phải gọi trên luồng UI).</summary>
        public static BitmapSource? RenderElementToBitmap(FrameworkElement? element, int targetWidth = 1280)
        {
            if (element == null) return null;
            try
            {
                double actualW = element.ActualWidth;
                double actualH = element.ActualHeight;
                if (actualW < 10 || actualH < 10)
                {
                    actualW = element.Width > 10 ? element.Width : 1280;
                    actualH = element.Height > 10 ? element.Height : 720;
                    element.Measure(new Size(actualW, actualH));
                    element.Arrange(new Rect(0, 0, actualW, actualH));
                    element.UpdateLayout();
                }

                int renderW = Math.Max(64, targetWidth);
                int renderH = Math.Max(64, (int)Math.Round(actualH * (renderW / actualW)));
                double dpiX = 96.0 * (renderW / actualW);
                double dpiY = 96.0 * (renderH / actualH);

                var rtb = new RenderTargetBitmap(renderW, renderH, dpiX, dpiY, PixelFormats.Pbgra32);
                var dv = new DrawingVisual();
                using (var dc = dv.RenderOpen())
                {
                    dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, actualW, actualH));
                    var vb = new VisualBrush(element)
                    {
                        Stretch = Stretch.Fill,
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top
                    };
                    dc.DrawRectangle(vb, null, new Rect(0, 0, actualW, actualH));
                }
                rtb.Render(dv);
                rtb.Freeze();
                return rtb;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Tạo ảnh xem trước (đã Freeze, dùng được từ mọi luồng). Trả null nếu không chụp được.</summary>
        public static BitmapSource? CaptureThumbnail(ScreenSourceInfo src)
        {
            if (src.Thumbnail != null) return src.Thumbnail;
            if (src.Kind == ScreenSourceKind.QaClassPage) return null;

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

                if (src.Kind == ScreenSourceKind.Screen || src.Kind == ScreenSourceKind.Region)
                {
                    StretchBlt(hdcThumb, 0, 0, tw, th, hdcScreen, src.X, src.Y, w, h, SRCCOPY);
                }
                else
                {
                    if (IsIconic(src.Hwnd)) return null;
                    hdcFull = CreateCompatibleDC(hdcScreen);
                    hbmFull = CreateCompatibleBitmap(hdcScreen, w, h);
                    hOldFull = SelectObject(hdcFull, hbmFull);
                    if (!PrintWindow(src.Hwnd, hdcFull, PW_RENDERFULLCONTENT))
                    {
                        StretchBlt(hdcThumb, 0, 0, tw, th, hdcScreen, src.X, src.Y, w, h, SRCCOPY);
                    }
                    else
                    {
                        SetStretchBltMode(hdcThumb, HALFTONE);
                        StretchBlt(hdcThumb, 0, 0, tw, th, hdcFull, 0, 0, w, h, SRCCOPY);
                    }
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
