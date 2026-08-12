using System;

namespace SmartLibrary.Desktop.Models
{
    public class AuditLogDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string SsoUserId { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Status { get; set; } = "Success"; // "Success" or "Failure"
    }
}
