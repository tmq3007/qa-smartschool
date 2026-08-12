namespace QASmartClass.Data
{
    /// <summary>Mở rộng LessonPlan — helper</summary>
    public partial class LessonPlan
    {
        /// <summary>Đã hoàn thành</summary>
        public bool IsDone => Status == "Done";
    }
}

