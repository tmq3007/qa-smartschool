using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace QASmartTouch.Controls
{
    /// <summary>
    /// QC_4.2_TOUCH_TOOLBAR (Radial/Dock): Kích hoạt "chạm 1 lần ăn ngay" cho các phần tử của toolbar mới
    /// trên cả Chuột, Cảm ứng (Touch/Multi-touch) và Bút (Stylus).
    ///
    /// Lý do: Trên màn hình tương tác, nếu chỉ dựa vào Touch-to-Mouse promotion của WPF thì cú chạm đầu tiên
    /// có thể bị hiểu nhầm là Hover (lỗi "phải ấn 2 lần" đã gặp ở panelTools). Helper này bắt trực tiếp
    /// PreviewStylus/PreviewTouch (đánh dấu Handled để chặn promotion → không bao giờ bị kích hoạt 2 lần)
    /// và chỉ xử lý chuột "thật" (StylusDevice == null).
    /// </summary>
    internal static class ToolbarPointerHelper
    {
        /// <param name="element">Phần tử cần bắt chạm.</param>
        /// <param name="onTap">Hành động khi chạm/nhấn-thả bên trong phần tử.</param>
        /// <param name="onPressedChanged">Tuỳ chọn: phản hồi trạng thái nhấn (true khi đè, false khi nhả).</param>
        /// <param name="handleMouse">
        /// true  = tự xử lý chuột (dùng cho Path/Border không phải Button).
        /// false = để Button.Click tự xử lý chuột (tránh kích hoạt 2 lần).
        /// </param>
        public static void AttachTap(UIElement element, Action onTap, Action<bool>? onPressedChanged = null, bool handleMouse = true)
        {
            if (element == null || onTap == null) return;

            if (element is FrameworkElement fe) fe.Focusable = false;
            Stylus.SetIsPressAndHoldEnabled(element, false);
            Stylus.SetIsFlicksEnabled(element, false);

            // Cờ nhấn dùng chung cho mọi nguồn nhập → đảm bảo 1 cú chạm chỉ kích hoạt đúng 1 lần,
            // kể cả khi thiết bị phát đồng thời chuỗi Stylus + Touch.
            bool pressed = false;

            void Press()
            {
                if (pressed) return;
                pressed = true;
                onPressedChanged?.Invoke(true);
            }

            void Release(bool inside)
            {
                if (!pressed) return;
                pressed = false;
                onPressedChanged?.Invoke(false);
                if (inside) element.Dispatcher.BeginInvoke(onTap, DispatcherPriority.Input);
            }

            void Cancel()
            {
                if (!pressed) return;
                pressed = false;
                onPressedChanged?.Invoke(false);
            }

            // ── Stylus (bao gồm cả cảm ứng ngón tay trên WPF Stylus stack) ──
            element.PreviewStylusDown += (s, e) =>
            {
                e.StylusDevice.Capture(element);
                Press();
                e.Handled = true;
            };
            element.PreviewStylusUp += (s, e) =>
            {
                bool inside = IsInside(element, e.GetPosition(element));
                if (e.StylusDevice.Captured == element) element.ReleaseStylusCapture();
                Release(inside);
                e.Handled = true;
            };

            // ── Touch (dự phòng nếu thiết bị không đi qua Stylus stack) ──
            element.PreviewTouchDown += (s, e) =>
            {
                e.TouchDevice.Capture(element);
                Press();
                e.Handled = true;
            };
            element.PreviewTouchUp += (s, e) =>
            {
                bool inside = IsInside(element, e.GetTouchPoint(element).Position);
                if (e.TouchDevice.Captured == element) element.ReleaseTouchCapture(e.TouchDevice);
                Release(inside);
                e.Handled = true;
            };

            if (!handleMouse) return;

            // ── Chuột thật (bỏ qua chuột được promote từ chạm/bút) ──
            element.MouseLeftButtonDown += (s, e) =>
            {
                if (e.StylusDevice != null) return;
                element.CaptureMouse();
                Press();
                e.Handled = true;
            };
            element.MouseLeftButtonUp += (s, e) =>
            {
                if (e.StylusDevice != null) return;
                bool inside = IsInside(element, e.GetPosition(element));
                if (element.IsMouseCaptured) element.ReleaseMouseCapture();
                Release(inside);
                e.Handled = true;
            };
            element.LostMouseCapture += (s, e) =>
            {
                // Chỉ huỷ khi không còn thiết bị nào đang giữ (tránh huỷ nhầm khi Release đã chạy)
                if (Mouse.LeftButton == MouseButtonState.Pressed) Cancel();
            };
        }

        /// <summary>
        /// Kéo liên tục (slider cung tròn) cho Chuột / Chạm / Bút. Toạ độ trả về tính theo <paramref name="relativeTo"/>.
        /// Chỉ một thiết bị được điều khiển tại một thời điểm; sự kiện chạm được đánh dấu Handled để không vẽ lên bảng.
        /// </summary>
        public static void AttachDrag(UIElement element, IInputElement relativeTo,
            Action<Point> onDown, Action<Point> onMove, Action onUp)
        {
            if (element == null) return;
            if (element is FrameworkElement fe) fe.Focusable = false;
            Stylus.SetIsPressAndHoldEnabled(element, false);
            Stylus.SetIsFlicksEnabled(element, false);

            int source = 0; // 0 none, 1 mouse, 2 stylus, 3 touch
            TouchDevice? touch = null;

            void End()
            {
                if (source == 0) return;
                source = 0;
                touch = null;
                onUp();
            }

            element.PreviewStylusDown += (s, e) =>
            {
                e.Handled = true;
                if (source != 0) return;
                source = 2;
                e.StylusDevice.Capture(element);
                onDown(e.GetPosition(relativeTo));
            };
            element.PreviewStylusMove += (s, e) =>
            {
                if (source != 2) return;
                onMove(e.GetPosition(relativeTo));
                e.Handled = true;
            };
            element.PreviewStylusUp += (s, e) =>
            {
                e.Handled = true;
                if (source != 2) return;
                End();
                if (e.StylusDevice.Captured == element) element.ReleaseStylusCapture();
            };
            element.LostStylusCapture += (s, e) => { if (source == 2) End(); };

            element.PreviewTouchDown += (s, e) =>
            {
                e.Handled = true;
                if (source != 0) return;
                source = 3;
                touch = e.TouchDevice;
                e.TouchDevice.Capture(element);
                onDown(e.GetTouchPoint(relativeTo).Position);
            };
            element.PreviewTouchMove += (s, e) =>
            {
                if (source != 3 || e.TouchDevice != touch) return;
                onMove(e.GetTouchPoint(relativeTo).Position);
                e.Handled = true;
            };
            element.PreviewTouchUp += (s, e) =>
            {
                e.Handled = true;
                if (source != 3 || e.TouchDevice != touch) return;
                var dev = e.TouchDevice;
                End();
                element.ReleaseTouchCapture(dev);
            };

            element.MouseLeftButtonDown += (s, e) =>
            {
                if (e.StylusDevice != null || source != 0) return;
                source = 1;
                element.CaptureMouse();
                onDown(e.GetPosition(relativeTo));
                e.Handled = true;
            };
            element.MouseMove += (s, e) =>
            {
                if (source != 1 || e.StylusDevice != null) return;
                onMove(e.GetPosition(relativeTo));
            };
            element.MouseLeftButtonUp += (s, e) =>
            {
                if (source != 1 || e.StylusDevice != null) return;
                End();
                element.ReleaseMouseCapture();
                e.Handled = true;
            };
            element.LostMouseCapture += (s, e) => { if (source == 1) End(); };
        }

        /// <summary>
        /// Kiểm tra điểm có nằm trên phần tử hay không (dùng HitTest hình học → chính xác cho múi hình quạt).
        /// </summary>
        public static bool IsInside(UIElement element, Point p)
        {
            try
            {
                return VisualTreeHelper.HitTest(element, p) != null;
            }
            catch
            {
                return true;
            }
        }
    }
}
