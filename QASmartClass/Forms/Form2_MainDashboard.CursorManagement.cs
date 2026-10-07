using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace QASmartTouch.Forms
{
    /// <summary>
    /// QC_4.2_DUAL_INPUT_CURSOR: Phân hệ quản lý con trỏ chuột cho cửa sổ toàn màn hình không viền (Borderless Fullscreen).
    /// Đảm bảo con trỏ chuột luôn hiển thị tức thì khi người dùng quay lại dùng chuột máy tính sau khi chạm màn hình tương tác (IFP),
    /// khắc phục triệt để hiện tượng Windows bật cờ CURSOR_SUPPRESSED ẩn chuột trong Client Area.
    /// </summary>
    public partial class Form2_MainDashboard
    {
        #region Win32 Native Interop for Cursor Management

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        private const int CURSOR_SHOWING = 0x00000001;
        private const int CURSOR_SUPPRESSED = 0x00000002;
        private const int WM_SETCURSOR = 0x0020;
        private const int MOUSEEVENTF_MOVE = 0x0001;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern int ShowCursor(bool bShow);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        #endregion

        #region Cursor Management Lifecycle & Handlers

        private long _lastCursorUnsuppressTick = 0;

        /// <summary>
        /// Khởi tạo cơ chế bảo vệ và đánh thức con trỏ chuột vật lý
        /// </summary>
        private void InitializeCursorManagement()
        {
            // 1. Lắng nghe chuyển động chuột ở cấp độ toàn bộ Window (Tunneling event)
            this.PreviewMouseMove += Form2_MainDashboard_PreviewMouseMove;

            // 2. Đánh thức con trỏ khi cửa sổ được kích hoạt lại (Focus / Alt-Tab)
            this.Activated += (s, e) =>
            {
                if (_touchHandler?.HasActiveTouches != true)
                {
                    EnsurePhysicalMouseCursorVisible();
                }
            };

            // 3. Đảm bảo khi chuột vừa đi vào cửa sổ từ ngoài màn hình
            this.MouseEnter += (s, e) =>
            {
                if (e.StylusDevice == null && _touchHandler?.HasActiveTouches != true)
                {
                    EnsurePhysicalMouseCursorVisible();
                }
            };

            System.Diagnostics.Debug.WriteLine("✅ CursorManagement initialized: Dual-Input protection active");
        }

        /// <summary>
        /// Xử lý sự kiện di chuyển chuột ở tầng Preview của Window:
        /// Tự động phát hiện chuột vật lý thật và giải phóng cờ CURSOR_SUPPRESSED nếu đang bị ẩn bởi Windows Touch.
        /// </summary>
        private void Form2_MainDashboard_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // CHỈ xử lý khi đây là chuột vật lý thật (không phải sự kiện cảm ứng giả lập từ Stylus/Touch)
            // và trên bảng hiện không có ngón tay/bút nào đang chạm viết vẽ
            if (e.StylusDevice == null && _touchHandler?.HasActiveTouches != true)
            {
                EnsurePhysicalMouseCursorVisible();
            }
        }

        /// <summary>
        /// Kiểm tra và đánh thức con trỏ chuột hệ thống nếu đang bị Windows OS đưa vào chế độ ẩn/suppressed do chạm cảm ứng trước đó.
        /// </summary>
        public void EnsurePhysicalMouseCursorVisible()
        {
            try
            {
                // Nếu đang có ngón tay chạm vẽ trên màn hình tương tác, giữ nguyên trạng thái ẩn để viết chữ sạch đẹp
                if (_touchHandler?.HasActiveTouches == true)
                {
                    return;
                }

                var ci = new CURSORINFO { cbSize = Marshal.SizeOf<CURSORINFO>() };
                if (GetCursorInfo(out ci))
                {
                    // Kiểm tra xem con trỏ chuột có đang bị Windows giấu đi (flags = 0) hoặc bị SUPPRESSED (0x02) không
                    bool isHidden = (ci.flags & CURSOR_SHOWING) == 0;
                    bool isSuppressed = (ci.flags & CURSOR_SUPPRESSED) != 0;

                    if (isHidden || isSuppressed)
                    {
                        long currentTick = Environment.TickCount64;
                        // Giới hạn tần suất xử lý tối thiểu 50ms để không tốn tài nguyên
                        if (currentTick - _lastCursorUnsuppressTick > 50)
                        {
                            _lastCursorUnsuppressTick = currentTick;

                            // 1. Đưa bộ đếm ShowCursor của Win32 về >= 0 (Hiển thị)
                            while (ShowCursor(true) < 0) { }

                            // 2. Phát vi xung chuyển động chuột 0px để Windows Input Subsystem gỡ bỏ trạng thái Touch Suppression
                            mouse_event(MOUSEEVENTF_MOVE, 1, 0, 0, 0);
                            mouse_event(MOUSEEVENTF_MOVE, -1, 0, 0, 0);

                            // 3. Yêu cầu WPF cập nhật lại con trỏ chuột theo phần tử UI hiện tại
                            Mouse.UpdateCursor();

                            System.Diagnostics.Debug.WriteLine($"🖱️ Restored mouse cursor: Hidden={isHidden}, Suppressed={isSuppressed}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ EnsurePhysicalMouseCursorVisible error: {ex.Message}");
            }
        }

        /// <summary>
        /// Hook xử lý thông điệp WM_SETCURSOR trong WndProc:
        /// Giúp duy trì hiển thị con trỏ trên toàn bộ 100% diện tích Client Area của cửa sổ toàn màn hình không viền.
        /// </summary>
        private void HandleSetCursorMessage(IntPtr hwnd, IntPtr lParam, ref bool handled)
        {
            // Không đánh dấu handled = true để WPF tiếp tục dispatch QueryCursor cho các nút bấm (Hand), thanh công cụ...
            if (_touchHandler?.HasActiveTouches != true)
            {
                EnsurePhysicalMouseCursorVisible();
            }
        }

        #endregion
    }
}
