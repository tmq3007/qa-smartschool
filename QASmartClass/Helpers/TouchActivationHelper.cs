using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace QASmartTouch.Helpers
{
    /// <summary>
    /// QC_4.2_TOUCH_ACTIVATION: Helper tĩnh toàn diện đảm bảo mọi Window và Control nhận cú chạm đầu tiên
    /// trên màn hình tương tác (IFP) mà không bị OS nuốt (MA_ACTIVATEANDEAT) và không bị trễ do gesture.
    /// 
    /// Nguyên nhân gốc rễ:
    /// 1. Khi một WPF Window chưa Active (không phải foreground), cú chạm đầu tiên trên 
    ///    Touch Screen gửi WM_MOUSEACTIVATE (0x0021) tới cửa sổ đó. WPF mặc định trả về 
    ///    MA_ACTIVATEANDEAT (2) = kích hoạt cửa sổ NHƯNG NUỐT (hủy bỏ) cú chạm đó.
    /// 2. WPF ButtonBase chỉ lắng nghe sự kiện chuột, mặc định Stylus.IsPressAndHoldEnabled = true 
    ///    và Focusable = true gây độ trễ và nuốt sự kiện chạm đầu tiên.
    /// 
    /// Giải pháp:
    /// 1. Hook WndProc trả về MA_ACTIVATE (1) = kích hoạt cửa sổ VÀ GIỮ NGUYÊN cú chạm.
    /// 2. Tự động gắn pipeline PreviewTouchDown/Up và PreviewStylusDown/Up cho toàn bộ ButtonBase,
    ///    tắt IsPressAndHoldEnabled, tắt Focusable, phát sự kiện Click ngay ở mili-giây đầu tiên.
    /// </summary>
    public static class TouchActivationHelper
    {
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_ACTIVATE = 1;

        // DependencyProperty để đánh dấu các control đã được gắn touch, tránh gán trùng lặp
        private static readonly DependencyProperty IsTouchWiredProperty =
            DependencyProperty.RegisterAttached(
                "IsTouchWired",
                typeof(bool),
                typeof(TouchActivationHelper),
                new PropertyMetadata(false));

        public static bool GetIsTouchWired(DependencyObject obj) =>
            (bool)obj.GetValue(IsTouchWiredProperty);

        public static void SetIsTouchWired(DependencyObject obj, bool value) =>
            obj.SetValue(IsTouchWiredProperty, value);

        /// <summary>
        /// Áp dụng hook WM_MOUSEACTIVATE cho một Window, đảm bảo cú chạm đầu tiên
        /// trên màn hình tương tác không bị OS nuốt (MA_ACTIVATEANDEAT).
        /// </summary>
        public static void Apply(Window window)
        {
            if (window == null) return;

            var source = PresentationSource.FromVisual(window) as HwndSource;
            if (source != null)
            {
                source.RemoveHook(WndProc);
                source.AddHook(WndProc);
            }
            else
            {
                window.SourceInitialized += (s, e) =>
                {
                    var src = PresentationSource.FromVisual(window) as HwndSource;
                    src?.RemoveHook(WndProc);
                    src?.AddHook(WndProc);
                };
            }
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

        /// <summary>
        /// Tự động áp dụng toàn diện cho SubMenu Window:
        /// 1. Hook WM_MOUSEACTIVATE để chống nuốt cú chạm khi kích hoạt cửa sổ.
        /// 2. Duyệt cây Visual Tree khi Loaded để gắn pipeline cảm ứng trực tiếp cho tất cả Buttons.
        /// </summary>
        public static void ApplyToSubMenu(Window window, FrameworkElement? exclude = null)
        {
            if (window == null) return;

            Apply(window);

            if (window.IsLoaded)
            {
                WireAllInteractiveControls(window, exclude);
            }
            else
            {
                window.Loaded += (s, e) =>
                {
                    WireAllInteractiveControls(window, exclude);
                };
            }
        }

        /// <summary>
        /// Duyệt đệ quy cây Visual Tree, gắn pipeline cảm ứng cho tất cả ButtonBase (Button, RadioButton, ToggleButton...)
        /// </summary>
        public static void WireAllInteractiveControls(DependencyObject parent, FrameworkElement? exclude = null)
        {
            if (parent == null) return;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is ButtonBase buttonBase && child != exclude)
                {
                    WireButton(buttonBase);
                }

                // Đệ quy tiếp vào các con
                WireAllInteractiveControls(child, exclude);
            }
        }

        /// <summary>
        /// Gắn Touch/Stylus trực tiếp cho một ButtonBase đơn lẻ (hoặc control tạo động).
        /// </summary>
        public static void WireButton(ButtonBase button)
        {
            if (button == null || GetIsTouchWired(button)) return;
            SetIsTouchWired(button, true);

            button.Focusable = false;
            Stylus.SetIsPressAndHoldEnabled(button, false);

            Point? touchStart = null;

            button.PreviewTouchDown += (s, e) =>
            {
                touchStart = e.GetTouchPoint(button).Position;
                e.TouchDevice.Capture(button);
                e.Handled = true;
            };

            button.PreviewTouchUp += (s, e) =>
            {
                if (e.TouchDevice.Captured == button)
                {
                    button.ReleaseTouchCapture(e.TouchDevice);
                    TriggerClickIfInside(button, touchStart, e.GetTouchPoint(button).Position);
                }
                touchStart = null;
                e.Handled = true;
            };

            Point? stylusStart = null;

            button.PreviewStylusDown += (s, e) =>
            {
                stylusStart = e.GetPosition(button);
                e.StylusDevice.Capture(button);
                e.Handled = true;
            };

            button.PreviewStylusUp += (s, e) =>
            {
                if (e.StylusDevice.Captured == button)
                {
                    button.ReleaseStylusCapture();
                    TriggerClickIfInside(button, stylusStart, e.GetPosition(button));
                }
                stylusStart = null;
                e.Handled = true;
            };
        }

        private static void TriggerClickIfInside(ButtonBase button, Point? startPoint, Point currentPoint)
        {
            try
            {
                double dist = startPoint.HasValue
                    ? Math.Sqrt(Math.Pow(currentPoint.X - startPoint.Value.X, 2) + Math.Pow(currentPoint.Y - startPoint.Value.Y, 2))
                    : 0;

                bool isInside = (button.ActualWidth > 0 && button.ActualHeight > 0)
                    ? (currentPoint.X >= 0 && currentPoint.X <= button.ActualWidth &&
                       currentPoint.Y >= 0 && currentPoint.Y <= button.ActualHeight)
                    : true;

                // Tap (< 15px) và nằm trong phạm vi nút -> Kích hoạt ngay lập tức
                if (dist < 15 && isInside)
                {
                    button.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (button is RadioButton radio)
                        {
                            radio.IsChecked = true;
                            radio.RaiseEvent(new RoutedEventArgs(ToggleButton.CheckedEvent, radio));
                        }
                        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
                    }), System.Windows.Threading.DispatcherPriority.Normal);
                }
            }
            catch
            {
                button.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (button is RadioButton radio)
                    {
                        radio.IsChecked = true;
                        radio.RaiseEvent(new RoutedEventArgs(ToggleButton.CheckedEvent, radio));
                    }
                    button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
                }), System.Windows.Threading.DispatcherPriority.Normal);
            }
        }
    }
}
