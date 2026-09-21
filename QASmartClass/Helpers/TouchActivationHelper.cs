using System;
using System.Windows;
using System.Windows.Interop;

namespace QASmartTouch.Helpers
{
    /// <summary>
    /// QC_4.2_TOUCH_ACTIVATION: Helper tĩnh đảm bảo mọi Window nhận cú chạm đầu tiên
    /// trên màn hình tương tác (IFP) mà không bị OS nuốt (MA_ACTIVATEANDEAT).
    /// 
    /// Nguyên nhân gốc rễ:
    /// Khi một WPF Window chưa Active (không phải foreground), cú chạm đầu tiên trên 
    /// Touch Screen gửi WM_MOUSEACTIVATE (0x0021) tới cửa sổ đó. WPF mặc định trả về 
    /// MA_ACTIVATEANDEAT (2) = kích hoạt cửa sổ NHƯNG NUỐT (hủy bỏ) cú chạm đó.
    /// Người dùng phải chạm lần 2 mới thực sự tương tác được.
    /// 
    /// Giải pháp:
    /// Hook WndProc để trả về MA_ACTIVATE (1) = kích hoạt cửa sổ VÀ GIỮ NGUYÊN cú chạm.
    /// 
    /// Cách dùng: Gọi TouchActivationHelper.Apply(this) trong constructor của Window.
    /// 
    /// Tương thích:
    /// - Desktop (chuột): WM_MOUSEACTIVATE hiếm khi xảy ra do cửa sổ Owner đã active → không ảnh hưởng.
    /// - Touch Screen (IFP): Fix triệt để lỗi "phải nhấn 2 lần" trên mọi cửa sổ.
    /// </summary>
    public static class TouchActivationHelper
    {
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_ACTIVATE = 1;

        /// <summary>
        /// Áp dụng hook WM_MOUSEACTIVATE cho một Window, đảm bảo cú chạm đầu tiên
        /// trên màn hình tương tác không bị OS nuốt.
        /// </summary>
        /// <param name="window">Window cần áp dụng hook.</param>
        public static void Apply(Window window)
        {
            if (window == null) return;

            window.SourceInitialized += (s, e) =>
            {
                var source = PresentationSource.FromVisual(window) as HwndSource;
                source?.AddHook(WndProc);
            };
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEACTIVATE)
            {
                handled = true;
                return new IntPtr(MA_ACTIVATE); // Kích hoạt cửa sổ VÀ KHÔNG NUỐT cú chạm/click
            }
            return IntPtr.Zero;
        }
    }
}
