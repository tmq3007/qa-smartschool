namespace QASmartClass.LearningTools.Models
{
    /// <summary>
    /// Nền tảng cho công cụ hỗ trợ tính năng vẽ chú thích (Annotation Overlay).
    /// Chuẩn bị cho giai đoạn mở rộng tiếp theo.
    /// </summary>
    public interface IAnnotatableTool
    {
        bool IsAnnotationEnabled { get; set; }
        void EnableAnnotationMode();
        void DisableAnnotationMode();
        void ClearAnnotations();
    }
}
