using QASmartClass.WhiteboardCore.Enums;

namespace QASmartClass.WhiteboardCore.Tools
{
    /// <summary>
    /// Interface bắt buộc cho mọi công cụ bảng vẽ.
    /// Mỗi công cụ (Pen, Eraser, Selection, Shape...) phải implement interface này
    /// để ToolManager có thể quản lý vòng đời và trạng thái thống nhất.
    /// 
    /// Lưu ý thiết kế: Interface này KHÔNG chứa tham chiếu tới UI controls.
    /// Các phương thức Activate/Deactivate chỉ thay đổi internal state.
    /// Việc đồng bộ UI (cursor, touch handler...) do Form2 xử lý qua event callbacks.
    /// </summary>
    public interface IWhiteboardTool
    {
        /// <summary>Tên hiển thị của công cụ (VD: "Pen", "Eraser").</summary>
        string Name { get; }

        /// <summary>ToolMode enum tương ứng.</summary>
        ToolMode Mode { get; }

        /// <summary>Công cụ đang hoạt động hay không.</summary>
        bool IsActive { get; }

        /// <summary>
        /// Kích hoạt công cụ.
        /// Chỉ thay đổi internal state (IsActive = true, cấu hình mặc định).
        /// KHÔNG truy cập UI controls trực tiếp.
        /// </summary>
        void Activate();

        /// <summary>
        /// Hủy kích hoạt công cụ.
        /// Reset internal state, giải phóng resources nếu cần.
        /// </summary>
        void Deactivate();
    }
}
