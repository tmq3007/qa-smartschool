using System;

namespace QASmartClass.Data
{
    public class OfflineSyncItem
    {
        public int Id { get; set; }
        public string ActionType { get; set; } = string.Empty;       // E.g., 'ATTENDANCE', 'GRADE', 'CLASS_CREATE', 'TICKET'
        public string PayloadJson { get; set; } = string.Empty;      // Serialized JSON of payload
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int RetryCount { get; set; } = 0;
        public string Status { get; set; } = "Pending";              // Pending, Failed
    }
}
