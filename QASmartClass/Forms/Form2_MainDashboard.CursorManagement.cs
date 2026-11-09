using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace QASmartTouch.Forms
{
    /// <summary>
    /// QC_4.2_DUAL_INPUT_CURSOR: Phân hệ quản lý con trỏ chuột cho cửa sổ toàn màn hình không viền (Borderless Fullscreen).
    /// Đảm bảo:
    /// 1. Màn hình tương tác (IFP / Touch / Stylus): Tôn trọng 100% cơ chế CURSOR_SUPPRESSED của Windows OS,
    ///    hoàn toàn triệt tiêu hiện tượng Ghost Cursor (con trỏ chuột +, SizeAll, Hand... bị đóng băng/kẹt lại sau khi chạm).
    /// 2. Chuột vật lý máy tính (Physical Mouse): Tự động phát hiện khi người dùng di chuyển chuột thật (lọc bỏ triệt để
    ///    touch promotion bằng Win32 signature 0xFF515700) và khôi phục hiển thị con trỏ mượt mà.
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
        private const int MOUSEEVENTF_MOVE = 0x0001;

        // Chữ ký chuẩn của Microsoft Windows Input Subsystem để nhận diện sự kiện thăng hạng (promoted) từ Touch hoặc Stylus
        private const long MOUSEEVENTF_FROMTOUCH = 0xFF515700;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern int ShowCursor(bool bShow);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        /// <summary>
        /// Kiểm tra xem thông điệp chuột hiện tại có phải do Touch hoặc Stylus/Pen của Windows thăng hạng (promoted) hay không.
        /// </summary>
        private static bool IsCurrentInputFromTouchOrPen()
        {
            long extra = GetMessageExtraInfo().ToInt64();
            return (extra & 0xFFFFFF00) == MOUSEEVENTF_FROMTOUCH;
        }

        #endregion

        #region Cursor Management Lifecycle & Handlers

        private long _lastCursorUnsuppressTick = 0;
        private long _lastTouchOrStylusTick = 0;
        private Point _lastPhysicalMousePos = new Point(-9999, -9999);
        private bool _isUnsuppressingCursor = false;

        /// <summary>
        /// Khởi tạo cơ chế bảo vệ và quản lý con trỏ chuột chuẩn kép (Touch & Physical Mouse)
        /// </summary>
        private void InitializeCursorManagement()
        {
            // 1. Ghi nhận thời điểm chạm cảm ứng/bút ở tầng Window để chặn hoàn toàn touch-promoted mouse events
            this.PreviewTouchDown += (s, e) => _lastTouchOrStylusTick = Environment.TickCount64;
            this.PreviewTouchMove += (s, e) => _lastTouchOrStylusTick = Environment.TickCount64;
            this.PreviewTouchUp += (s, e) => _lastTouchOrStylusTick = Environment.TickCount64;
            this.PreviewStylusDown += (s, e) => _lastTouchOrStylusTick = Environment.TickCount64;
            this.PreviewStylusMove += (s, e) => _lastTouchOrStylusTick = Environment.TickCount64;
            this.PreviewStylusUp += (s, e) => _lastTouchOrStylusTick = Environment.TickCount64;

            // 2. Lắng nghe chuyển động chuột ở cấp độ toàn bộ Window (Tunneling event)
            this.PreviewMouseMove += Form2_MainDashboard_PreviewMouseMove;

            System.Diagnostics.Debug.WriteLine("✅ CursorManagement initialized: Dual-Input protection active (Ghost-Cursor free)");
        }

        /// <summary>
        /// Xử lý sự kiện di chuyển chuột ở tầng Preview của Window:
        /// CHỈ can thiệp khi xác nhận 100% đây là CHUỘT VẬT LÝ THẬT (Desktop/Laptop Mouse/Touchpad) đang di chuyển,
        /// tuyệt đối không can thiệp sau các cú chạm cảm ứng trên màn hình tương tác.
        /// </summary>
        private void Form2_MainDashboard_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // Tránh đệ quy khi phát xung mouse_event
            if (_isUnsuppressingCursor) return;

            // BỘ LỌC 1: Sự kiện mang thông tin thiết bị bút/cảm ứng của WPF
            if (e.StylusDevice != null) return;

            // BỘ LỌC 2: Kiểm tra Win32 Hardware ExtraInfo Signature (Touch/Stylus promoted message)
            if (IsCurrentInputFromTouchOrPen()) return;

            // BỘ LỌC 3: Trên Canvas đang có ngón tay chạm vẽ
            if (_touchHandler?.HasActiveTouches == true) return;

            // BỘ LỌC 4: Vừa có tương tác cảm ứng/bút trong vòng 500ms trước đó
            if (Environment.TickCount64 - _lastTouchOrStylusTick < 500) return;

            // BỘ LỌC 5: Kiểm tra tọa độ chuột vật lý có thực sự thay đổi vị trí do người dùng rê chuột không
            Point currentPos = e.GetPosition(this);
            if (Math.Abs(currentPos.X - _lastPhysicalMousePos.X) < 1.0 &&
                Math.Abs(currentPos.Y - _lastPhysicalMousePos.Y) < 1.0)
            {
                return;
            }
            _lastPhysicalMousePos = currentPos;

            // Khi đã vượt qua tất cả 5 bộ lọc: ĐÂY LÀ CHUỘT VẬT LÝ THẬT SỰ
            EnsurePhysicalMouseCursorVisible();
        }

        /// <summary>
        /// Đánh thức con trỏ chuột hệ thống khi người dùng di chuyển chuột máy tính vật lý.
        /// </summary>
        public void EnsurePhysicalMouseCursorVisible()
        {
            try
            {
                if (_isUnsuppressingCursor) return;

                // Nếu đang có ngón tay chạm vẽ trên màn hình tương tác hoặc vừa chạm trong 500ms, không can thiệp
                if (_touchHandler?.HasActiveTouches == true || Environment.TickCount64 - _lastTouchOrStylusTick < 500)
                {
                    return;
                }

                var ci = new CURSORINFO { cbSize = Marshal.SizeOf<CURSORINFO>() };
                if (GetCursorInfo(out ci))
                {
                    bool isHidden = (ci.flags & CURSOR_SHOWING) == 0;
                    bool isSuppressed = (ci.flags & CURSOR_SUPPRESSED) != 0;

                    if (isHidden || isSuppressed)
                    {
                        long currentTick = Environment.TickCount64;
                        if (currentTick - _lastCursorUnsuppressTick > 100)
                        {
                            _lastCursorUnsuppressTick = currentTick;
                            _isUnsuppressingCursor = true;

                            try
                            {
                                // 1. Đưa bộ đếm ShowCursor của Win32 về >= 0 (Hiển thị)
                                while (ShowCursor(true) < 0) { }

                                // 2. Phát vi xung chuyển động chuột nhỏ để Windows gỡ bỏ trạng thái Touch Suppression cho chuột thật
                                mouse_event(MOUSEEVENTF_MOVE, 1, 0, 0, 0);
                                mouse_event(MOUSEEVENTF_MOVE, -1, 0, 0, 0);

                                // 3. Yêu cầu WPF cập nhật lại con trỏ chuột theo vị trí hiện tại của chuột vật lý
                                Mouse.UpdateCursor();

                                System.Diagnostics.Debug.WriteLine($"🖱️ Restored physical mouse cursor: Hidden={isHidden}, Suppressed={isSuppressed}");
                            }
                            finally
                            {
                                _isUnsuppressingCursor = false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ EnsurePhysicalMouseCursorVisible error: {ex.Message}");
            }
        }

        #endregion
    }
}
