using System;

namespace QASmartClass.Data
{
    public class CleaningTask
    {
        public int Id { get; set; }
        public string Area { get; set; } = string.Empty;
        public DateTime TaskDate { get; set; } = DateTime.Today;
        public string Shift { get; set; } = "Morning"; // Morning, Afternoon
        public string JanitorName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, InProgress, Done
        public string Note { get; set; } = string.Empty;
        public string JanitorCode { get; set; } = string.Empty;
    }
}
