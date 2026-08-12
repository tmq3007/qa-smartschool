using System;

namespace QASmartClass.Data
{
    public class SubstituteAssignment
    {
        public int Id { get; set; }
        public int LeaveRequestId { get; set; }
        public string OriginalTeacherCode { get; set; } = string.Empty;
        public string SubstituteTeacherCode { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int PeriodIndex { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Status { get; set; } = "Assigned"; // Assigned, Completed, Cancelled
        public string RoomName { get; set; } = string.Empty;
    }
}
