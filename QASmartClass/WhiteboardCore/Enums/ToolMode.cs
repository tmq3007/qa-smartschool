namespace QASmartClass.WhiteboardCore.Enums
{
    /// <summary>
    /// Trạng thái hiện tại của công cụ bảng vẽ.
    /// Enum tập trung — mọi module trong WhiteboardCore đều tham chiếu enum này
    /// để biết "đang ở chế độ nào".
    /// </summary>
    public enum ToolMode
    {
        /// <summary>Không có công cụ nào được chọn (trạng thái mặc định).</summary>
        None = 0,

        /// <summary>Chế độ vẽ tự do bằng bút (Freehand Pen).</summary>
        Pen,

        /// <summary>Chế độ tẩy nét (Eraser — Stroke / Drag / ClearAll).</summary>
        Eraser,

        /// <summary>Chế độ chọn đối tượng (Rectangle / Lasso / MagicWand).</summary>
        Select,

        /// <summary>Chế độ vẽ hình học (Line, Rectangle, Ellipse, Triangle...).</summary>
        Shape,

        /// <summary>Chế độ chèn nội dung (Image, Video, Text, Camera).</summary>
        Insert,

        /// <summary>Chế độ zoom / pan canvas.</summary>
        Zoom,

        /// <summary>Chế độ con trỏ thuyết trình (Pointer / Spotlight).</summary>
        Pointer,

        /// <summary>Chế độ di chuyển (Pan) bảng.</summary>
        Pan,

        /// <summary>Chế độ nhập văn bản (Text Tool).</summary>
        Text
    }

    /// <summary>
    /// Chế độ phụ của Eraser.
    /// </summary>
    public enum EraserMode
    {
        /// <summary>Xóa theo nét: chạm vào nét nào thì xóa nét đó.</summary>
        Stroke,

        /// <summary>Kéo vùng (Marquee): kéo khoanh vùng rồi xóa tất cả bên trong.</summary>
        Drag,

        /// <summary>Xóa tất cả nội dung trên bảng.</summary>
        ClearAll
    }

    /// <summary>
    /// Chế độ phụ của Selection.
    /// </summary>
    public enum SelectionMode
    {
        /// <summary>Chọn bằng khung hình chữ nhật.</summary>
        Rectangle,

        /// <summary>Chọn bằng đường bao tự do (Lasso).</summary>
        Lasso,

        /// <summary>Chọn theo cùng màu (Magic Wand).</summary>
        MagicWand
    }
}
