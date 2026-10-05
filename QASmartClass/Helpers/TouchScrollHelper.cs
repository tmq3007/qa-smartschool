using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace QASmartTouch.Helpers
{
    /// <summary>
    /// QC_4.2_TOUCH_SCROLL: Tiện ích tĩnh cung cấp khả năng cuộn cảm ứng và kéo thả (Touch & Drag-to-Scroll)
    /// toàn diện cho ScrollViewer trên cả máy tính lẫn màn hình tương tác phòng học (IFP).
    /// 
    /// Các vấn đề giải quyết triệt để:
    /// 1. Bật PanningMode.Both cho Native Touch / Multi-touch.
    /// 2. Bổ sung Kinetic Mouse/Touch Drag-to-Scroll cho màn hình cảm ứng chạy chế độ giả lập chuột (Single-Touch Mouse Emulation).
    /// 3. Phân biệt chính xác giữa Chạm nhẹ (Tap/Click < 8px) và Vuốt kéo (Drag >= 8px).
    /// 4. Không can thiệp vào thanh cuộn ScrollBar và thanh trượt Slider.
    /// 5. Chuyển giao cuộn (Scroll Bubbling) cho ScrollViewer con lồng nhau.
    /// </summary>
    public static class TouchScrollHelper
    {
        private static readonly DependencyProperty IsDragScrollEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsDragScrollEnabled",
                typeof(bool),
                typeof(TouchScrollHelper),
                new PropertyMetadata(false));

        public static bool GetIsDragScrollEnabled(DependencyObject obj) =>
            (bool)obj.GetValue(IsDragScrollEnabledProperty);

        public static void SetIsDragScrollEnabled(DependencyObject obj, bool value) =>
            obj.SetValue(IsDragScrollEnabledProperty, value);

        /// <summary>
        /// Kích hoạt chế độ cuộn chạm và kéo cho một ScrollViewer cụ thể.
        /// </summary>
        public static void EnableTouchAndDragScroll(ScrollViewer? scrollViewer)
        {
            if (scrollViewer == null || GetIsDragScrollEnabled(scrollViewer)) return;
            SetIsDragScrollEnabled(scrollViewer, true);

            // 1. Cấu hình Native Touch Panning của WPF
            scrollViewer.PanningMode = PanningMode.Both;
            scrollViewer.PanningDeceleration = 0.002;
            scrollViewer.PanningRatio = 1.0;
            Stylus.SetIsPressAndHoldEnabled(scrollViewer, false);

            // 2. Trạng thái kéo chuột / cảm ứng
            bool isPotentialDrag = false;
            bool isDragging = false;
            Point startPoint = new Point();
            double startVOffset = 0;
            double startHOffset = 0;

            scrollViewer.PreviewMouseDown += (sender, e) =>
            {
                if (e.LeftButton != MouseButtonState.Pressed) return;

                // Không can thiệp nếu nhấn vào ScrollBar hoặc Slider
                if (IsInsideExcludedControl(e.OriginalSource as DependencyObject))
                {
                    return;
                }

                startPoint = e.GetPosition(scrollViewer);
                startVOffset = scrollViewer.VerticalOffset;
                startHOffset = scrollViewer.HorizontalOffset;
                isPotentialDrag = true;
                isDragging = false;
            };

            scrollViewer.PreviewMouseMove += (sender, e) =>
            {
                if (!isPotentialDrag || e.LeftButton != MouseButtonState.Pressed)
                {
                    return;
                }

                Point currentPoint = e.GetPosition(scrollViewer);
                double deltaX = currentPoint.X - startPoint.X;
                double deltaY = currentPoint.Y - startPoint.Y;

                // Ngưỡng phát hiện kéo vuốt: 8 pixel
                if (!isDragging && (Math.Abs(deltaY) > 8 || Math.Abs(deltaX) > 8))
                {
                    isDragging = true;
                    scrollViewer.CaptureMouse();
                }

                if (isDragging)
                {
                    // Cuộn nội dung mượt mà theo ngón tay kéo
                    if (scrollViewer.ScrollableHeight > 0)
                    {
                        scrollViewer.ScrollToVerticalOffset(startVOffset - deltaY);
                    }
                    if (scrollViewer.ScrollableWidth > 0)
                    {
                        scrollViewer.ScrollToHorizontalOffset(startHOffset - deltaX);
                    }

                    // Chặn sự kiện để tránh bôi đen text hoặc focus ngoài ý muốn
                    e.Handled = true;
                }
            };

            scrollViewer.PreviewMouseUp += (sender, e) =>
            {
                isPotentialDrag = false;

                if (isDragging)
                {
                    isDragging = false;
                    scrollViewer.ReleaseMouseCapture();
                    // Ngăn chặn kích hoạt Click vào nút bấm hay control bên dưới khi vừa kết thúc thao tác kéo
                    e.Handled = true;
                }
            };

            scrollViewer.MouseLeave += (sender, e) =>
            {
                if (isDragging && !scrollViewer.IsMouseCaptured)
                {
                    isDragging = false;
                    isPotentialDrag = false;
                }
            };
        }

        /// <summary>
        /// Kích hoạt Scroll Bubbling cho ScrollViewer con để khi cuộn hết nội dung con,
        /// thao tác cuộn con lăn / vuốt sẽ được chuyển tiếp lên ScrollViewer cha.
        /// </summary>
        public static void EnableMouseWheelBubbling(ScrollViewer? childScrollViewer)
        {
            if (childScrollViewer == null) return;

            childScrollViewer.PreviewMouseWheel += (sender, e) =>
            {
                if (e.Handled) return;

                // Kiểm tra nếu ScrollViewer con đã ở đỉnh hoặc đáy
                bool atTop = e.Delta > 0 && childScrollViewer.VerticalOffset <= 0.001;
                bool atBottom = e.Delta < 0 && childScrollViewer.VerticalOffset >= (childScrollViewer.ScrollableHeight - 0.001);

                if (atTop || atBottom)
                {
                    e.Handled = true;
                    var parent = FindVisualParent<ScrollViewer>(childScrollViewer);
                    if (parent != null)
                    {
                        var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                        {
                            RoutedEvent = UIElement.MouseWheelEvent,
                            Source = sender
                        };
                        parent.RaiseEvent(eventArg);
                    }
                }
            };
        }

        /// <summary>
        /// Duyệt đệ quy cây giao diện và tự động gắn cuộn cảm ứng cho toàn bộ ScrollViewer trong cửa sổ.
        /// </summary>
        public static void AttachToAllScrollViewers(DependencyObject? root)
        {
            if (root == null) return;

            if (root is Window window)
            {
                if (window.IsLoaded)
                {
                    WireScrollViewersRecursively(window);
                }
                else
                {
                    window.Loaded += (s, e) => WireScrollViewersRecursively(window);
                }
                return;
            }

            WireScrollViewersRecursively(root);
        }

        private static void WireScrollViewersRecursively(DependencyObject parent)
        {
            if (parent == null) return;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is ScrollViewer scrollViewer)
                {
                    EnableTouchAndDragScroll(scrollViewer);

                    // Kiểm tra nếu là ScrollViewer con nằm trong 1 ScrollViewer khác
                    var parentScroll = FindVisualParent<ScrollViewer>(scrollViewer);
                    if (parentScroll != null)
                    {
                        EnableMouseWheelBubbling(scrollViewer);
                    }
                }

                WireScrollViewersRecursively(child);
            }
        }

        private static bool IsInsideExcludedControl(DependencyObject? element)
        {
            if (element == null) return false;

            // Nếu người dùng nhấn vào ScrollBar (thanh trượt cuộn) hoặc Slider -> cho phép tương tác bình thường
            if (FindVisualParent<ScrollBar>(element) != null) return true;
            if (FindVisualParent<Slider>(element) != null) return true;

            return false;
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                {
                    return parent;
                }
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }
}
