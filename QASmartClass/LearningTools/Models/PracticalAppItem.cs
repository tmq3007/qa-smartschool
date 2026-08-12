namespace QASmartClass.LearningTools.Models
{
    public class PracticalAppItem
    {
        public string Icon { get; set; } = "🌍";
        public string Title { get; set; } = "";
        public string ImagePath { get; set; } = ""; // Path to resource (e.g. Pack URI)
        public string Description { get; set; } = "";

        // Nâng cấp bản v4.1+ giải quyết khoảng trống hiển thị
        public string Formula { get; set; } = "";
        public string MathAnalysis { get; set; } = "";
        public string DiscussionQuestion { get; set; } = "";
        public string InteractiveTask { get; set; } = "";
    }
}
