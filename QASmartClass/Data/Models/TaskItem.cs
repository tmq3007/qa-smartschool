namespace QASmartClass.Data
{
    /// <summary>Mở rộng TaskItem — helper</summary>
    public partial class TaskItem
    {
        /// <summary>Đã hoàn thành</summary>
        public bool IsCompleted => Status == "Done" || Status == "Completed";
        
        /// <summary>Quá h?n</summary>
        public bool IsOverdue => Deadline < System.DateTime.Now && !IsCompleted;
    }
}

