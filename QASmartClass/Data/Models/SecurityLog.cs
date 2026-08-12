using System;

namespace QASmartClass.Data
{
    public class SecurityLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string EventType { get; set; } = string.Empty; // CheckIn, CheckOut, Incident, KeyExchange
        public string Description { get; set; } = string.Empty;
        public string PersonInvolved { get; set; } = string.Empty;
        public string GuardName { get; set; } = string.Empty;

        // Structured visitor attributes
        public string CccdNumber { get; set; } = string.Empty;
        public string CccdHash { get; set; } = string.Empty;
        public string VisitorName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string HostTeacherId { get; set; } = string.Empty;
        public string VisitPurpose { get; set; } = string.Empty;
        public string BadgeNumber { get; set; } = string.Empty;
        public bool IsCheckedOut { get; set; } = false;
    }
}
