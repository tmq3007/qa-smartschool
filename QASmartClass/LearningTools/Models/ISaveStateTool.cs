namespace QASmartClass.LearningTools.Models
{
    /// <summary>
    /// Nền tảng cho công cụ hỗ trợ tính năng lưu trạng thái bài giảng (Save State / Workspace).
    /// Chuẩn bị cho giai đoạn mở rộng tiếp theo.
    /// </summary>
    public interface ISaveStateTool
    {
        /// <summary>Lưu trạng thái hiện tại của công cụ thành chuỗi JSON/XML</summary>
        string GetCurrentState();

        /// <summary>Phục hồi trạng thái công cụ từ dữ liệu đã lưu</summary>
        void RestoreState(string stateData);
    }
}
