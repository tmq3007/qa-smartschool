using System;
using System.Windows;
using System.Windows.Input;

namespace QASmartTouch.Helpers
{
    /// <summary>
    /// QC_4.2_TOUCH_ISOLATION: Helper utility cho việc áp dụng Touch/Stylus event isolation
    /// lên các Canvas-hosted UserControl nằm trên Canvas layer.
    /// Ngăn chặn sự kiện chạm lọt xuống (pass-through) Canvas vẽ phía dưới.
    /// </summary>
    public static class InputValidationHelper
    {
        /// <summary>
        /// QC_4.2_TOUCH_PIPELINE: Áp dụng touch event isolation cho Canvas-hosted UIElements.
        /// 
        /// ⚠️ CHỈ SỬ DỤNG cho Nhóm C (Canvas-hosted UserControl):
        ///   - SelectionBox, ContextToolbar, FloatingTouchKeyboard, MoreMenu,
        ///   - NotificationWindow, TableEditorControl, RichTextBoxControl
        /// 
        /// ❌ KHÔNG SỬ DỤNG cho:
        ///   - Nhóm A (STEM Window): RulerTool, ProtractorTool, SetSquareTool, CompassTool
        ///   - Nhóm B (Popup Window): SubMenuPen, SubMenuEraser, SubMenuDrawShapes, TextBoxEditor
        ///   → WPF Window tự cô lập sự kiện. ApplyTouchIsolation sẽ chặn Touch-to-Mouse promotion,
        ///     khiến các Mouse event handlers (MouseDown, MouseMove) không fire khi dùng cảm ứng.
        /// </summary>
        /// <param name="element">Canvas-hosted UIElement cần bảo vệ khỏi touch pass-through</param>
        public static void ApplyTouchIsolation(UIElement element)
        {
            if (element == null) return;

            element.TouchDown += (s, e) => { e.Handled = true; };
            element.TouchMove += (s, e) => { e.Handled = true; };
            element.TouchUp += (s, e) => { e.Handled = true; };

            element.StylusDown += (s, e) => { e.Handled = true; };
            element.StylusMove += (s, e) => { e.Handled = true; };
            element.StylusUp += (s, e) => { e.Handled = true; };
        }
    }
}
