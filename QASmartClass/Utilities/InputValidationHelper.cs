using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace QASmartTouch.Utilities
{
    /// <summary>
    /// Bộ lọc sự kiện tương tác dùng chung cho các Canvas lớn trên bảng trắng.
    /// Giúp xác định xem điểm chạm/click chuột có nằm trên nút bấm hay các control tương tác hay không.
    /// </summary>
    public static class InputValidationHelper
    {
        /// <summary>
        /// Kiểm tra xem sự kiện đầu vào (Touch/Mouse) bắt nguồn từ một nút bấm hoặc control tương tác khác hay không.
        /// Bao gồm: Button, ToggleButton, RepeatButton, TextBox, Selector, Slider, Viewport3D, UserControl
        /// </summary>
        /// <param name="originalSource">Nguồn phát sinh sự kiện gốc (e.OriginalSource)</param>
        /// <param name="parentBoundary">Giới hạn trên của cấu trúc giao diện (ví dụ Canvas lớn)</param>
        /// <returns>True nếu thuộc nút bấm hoặc control tương tác</returns>
        public static bool IsEventFromInteractiveControl(object originalSource, DependencyObject parentBoundary)
        {
            return WalkUpVisualTree(originalSource, parentBoundary, obj =>
            {
                // Kiểm tra nếu là Nút bấm (Button, ToggleButton, RepeatButton)
                if (obj is ButtonBase)
                    return true;

                // Kiểm tra các điều khiển nhập liệu hoặc tương tác khác
                if (obj is TextBox || obj is Selector || obj is Slider || obj is Viewport3D || obj is UserControl)
                    return true;

                // ✅ QC_4.2_WIDGET_GUARD: Kiểm tra DragHandle hoặc Container widget tương tác (YouTube, Google Maps, Widget, SelectionBox)
                if (obj is FrameworkElement fe)
                {
                    if (fe.Tag is string tagStr && 
                        (tagStr == "DragHandle" || tagStr == "InteractiveYouTubeVideo" || 
                         tagStr == "InteractiveGoogleMaps" || tagStr == "InteractiveImage" || 
                         tagStr == "InteractiveLocalVideo" ||
                         tagStr == "SelectionBox" || tagStr == "YouTubeControlPanel" || 
                         tagStr == "GoogleMapsControlPanel" || tagStr == "ImageControlPanel" || 
                         tagStr == "VideoControlPanel" || tagStr == "ResizeHandle"))
                    {
                        return true;
                    }
                }

                return false;
            });
        }

        /// <summary>
        /// Kiểm tra xem sự kiện đầu vào có bắt nguồn từ một nút bấm (Button, ToggleButton, RepeatButton) hay không.
        /// Phiên bản thu hẹp hơn IsEventFromInteractiveControl — chỉ kiểm tra ButtonBase.
        /// </summary>
        /// <param name="originalSource">Nguồn phát sinh sự kiện gốc (e.OriginalSource)</param>
        /// <param name="parentBoundary">Giới hạn trên của cấu trúc giao diện (ví dụ Canvas lớn)</param>
        /// <returns>True nếu thuộc nút bấm (ButtonBase)</returns>
        public static bool IsEventFromButton(object originalSource, DependencyObject parentBoundary)
        {
            return WalkUpVisualTree(originalSource, parentBoundary, obj => obj is ButtonBase);
        }

        /// <summary>
        /// Hàm nội bộ dùng chung: đi ngược lên Visual/Logical Tree từ nguồn sự kiện,
        /// kiểm tra từng node bằng predicate cho đến khi gặp parentBoundary hoặc null.
        /// </summary>
        /// <param name="originalSource">Nguồn phát sinh sự kiện gốc</param>
        /// <param name="parentBoundary">Giới hạn trên của cấu trúc giao diện</param>
        /// <param name="matchPredicate">Điều kiện kiểm tra trên mỗi node</param>
        /// <returns>True nếu có node nào thỏa mãn predicate</returns>
        private static bool WalkUpVisualTree(object originalSource, DependencyObject parentBoundary, Func<DependencyObject, bool> matchPredicate)
        {
            DependencyObject? obj = originalSource as DependencyObject;
            while (obj != null && obj != parentBoundary)
            {
                if (matchPredicate(obj))
                {
                    return true;
                }

                DependencyObject? parent = null;
                try
                {
                    parent = VisualTreeHelper.GetParent(obj);
                }
                catch (InvalidOperationException)
                {
                    // Bỏ qua lỗi Visual Tree (element chưa gắn vào tree)
                }

                if (parent == null)
                {
                    try
                    {
                        parent = LogicalTreeHelper.GetParent(obj);
                    }
                    catch (InvalidOperationException)
                    {
                        // Bỏ qua lỗi Logical Tree
                    }
                }
                obj = parent;
            }
            return false;
        }
    }
}
