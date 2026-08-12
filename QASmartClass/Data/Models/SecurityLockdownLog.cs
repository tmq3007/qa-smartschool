using System;

namespace QASmartClass.Data
{
    public class SecurityLockdownLog
    {
        public int Id { get; set; }
        public string TriggeredByStaffCode { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime? EndedAt { get; set; }
        public string LockdownType { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public string ResolutionNotes { get; set; } = string.Empty;
    }
}
