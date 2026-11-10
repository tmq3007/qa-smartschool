using System;
using System.Windows;

namespace QASmartTouch.Helpers
{
    /// <summary>
    /// Tập trung toàn bộ hằng số định danh Tag cho các phần tử trên bảng vẽ (MainInteractiveBoard).
    /// Tương tự ZIndexConstants, giúp loại bỏ magic strings và đồng bộ giữa Selection, History, BoardManager và Export.
    /// </summary>
    public static class BoardElementTags
    {
        // ── Tầng nền & Hệ thống ──
        public const string BackgroundLayer = "BackgroundLayer";

        // ── Chế độ Thi đua / Đa người dùng (Multi-User) ──
        public const string MultiUserDivider = "MultiUserDivider";
        public const string MultiUserLabel = "MultiUserLabel";
        public const string Student1Stroke = "Student1Stroke";
        public const string Student2Stroke = "Student2Stroke";

        // ── Hình học 2D & 3D ──
        public const string Shape3D = "3DShape";
        public const string PolygonShape = "PolygonShape";

        // ── Học liệu Toán học, Bảng biểu & Ghi chú ──
        public const string TableContainer = "TableContainer";
        public const string SmartNoteCard = "SmartNoteCard";
        public const string CalculatorMathCard = "CalculatorMathCard";
        public const string CalculatorResult = "CalculatorResult";
        public const string FunctionGraph = "FunctionGraph";
        public const string VariationTable = "VariationTable";

        // ── Nội dung Nhúng & Đa phương tiện (Widgets / Media) ──
        public const string EmbeddedYouTube = "EmbeddedYouTube";
        public const string EmbeddedBrowser = "EmbeddedBrowser";
        public const string Embedded3DModel = "Embedded3DModel";
        public const string EmbeddedSimulation = "EmbeddedSimulation";

        /// <summary>
        /// Lấy chuỗi Tag của một UIElement một cách an toàn.
        /// </summary>
        public static string? GetTagString(UIElement? element)
        {
            return (element as FrameworkElement)?.Tag?.ToString();
        }

        /// <summary>
        /// Kiểm tra xem UIElement có mang Tag khớp với hằng số chỉ định hay không.
        /// </summary>
        public static bool HasTag(UIElement? element, string expectedTag)
        {
            if (element is not FrameworkElement fe || fe.Tag == null)
                return false;

            return string.Equals(fe.Tag.ToString(), expectedTag, StringComparison.Ordinal);
        }

        /// <summary>
        /// Kiểm tra xem phần tử có phải là lớp nền bảng (BackgroundLayer) hay không.
        /// </summary>
        public static bool IsBackgroundLayer(UIElement? element)
        {
            return HasTag(element, BackgroundLayer);
        }

        /// <summary>
        /// Kiểm tra xem phần tử có phải là thẻ kết quả từ máy tính khoa học QA Calculator hay không.
        /// </summary>
        public static bool IsCalculatorCard(UIElement? element)
        {
            string? tag = GetTagString(element);
            return tag == CalculatorMathCard || tag == CalculatorResult;
        }
    }
}

