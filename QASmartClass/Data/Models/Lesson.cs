namespace QASmartClass.Data
{
    /// <summary>Mở rộng Lesson — helper methods</summary>
    public partial class Lesson
    {
        /// <summary>Kiểm tra bài giảng đã được dạy</summary>
        public bool HasBeenTaught => UseCount > 0;

        /// <summary>Mô tả ngắn: "Toán - Lớp 10A - Tiết 3"</summary>
        public string ShortDescription =>
            $"{Subject} - Lớp {ClassName} - Tiết {Period}";

        /// <summary>Cờ hiệu dạy học khẩn cấp khi chưa kịp duyệt</summary>
        public bool IsEmergency => !string.IsNullOrEmpty(Tags) && Tags.Contains("Emergency", System.StringComparison.OrdinalIgnoreCase);
    }
}

