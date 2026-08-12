namespace QASmartClass.LearningTools.Models
{
    /// <summary>
    /// Phân loại nhóm công cụ học tập
    /// </summary>
    public enum ToolCategory
    {
        Math,         // 🔢 Toán học
        Science,      // 🔬 Khoa học
        Language,     // 🗣️ Ngôn ngữ
        MultiSubject, // 🌐 Đa môn
        Thinking,     // 🧠 Tư duy & IQ
        Workplace,    // 💼 Kỹ năng nghề nghiệp
        PracticalApps // 🌍 Ứng dụng thực tế
    }

    /// <summary>
    /// Metadata mô tả 1 công cụ học tập
    /// </summary>
    public class ToolDefinition
    {
        /// <summary>ID duy nhất, ví dụ: "multiplication"</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Tên hiển thị, ví dụ: "Bảng Cửu Chương"</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Icon emoji</summary>
        public string Icon { get; set; } = "📋";

        /// <summary>Mô tả ngắn</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Nhóm công cụ</summary>
        public ToolCategory Category { get; set; }

        /// <summary>Cấp học phù hợp, ví dụ: "Lớp 1-5"</summary>
        public string GradeLevel { get; set; } = "Mọi cấp";

        /// <summary>Có chế độ quiz/luyện tập không</summary>
        public bool IsInteractive { get; set; }

        /// <summary>Đã triển khai chưa</summary>
        public bool IsAvailable { get; set; }

        /// <summary>Màu nền accent (hex)</summary>
        public string ColorAccent { get; set; } = "#E3F2FD";

        /// <summary>Form ID nếu navigate sang page khác (ví dụ: "F31" cho StemTools)</summary>
        public string? NavigateFormId { get; set; }

        /// <summary>Tags tìm kiếm</summary>
        public string[] Tags { get; set; } = System.Array.Empty<string>();

        /// <summary>Ẩn nút Bảng trắng (áp dụng cho một số game/quiz đặc thù)</summary>
        public bool HideWhiteboard { get; set; } = false;
    }
}
