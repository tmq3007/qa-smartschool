using System;

namespace QASmartClass.Data
{
    public class MaintenanceTicket
    {
        public int Id { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string FacilityName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
        public string Status { get; set; } = "Pending"; // Pending, InProgress, Resolved
        public string AssignedStaffCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ResolvedAt { get; set; }
        public bool IsEscalated { get; set; } = false;
        public string ResolutionNotes { get; set; } = string.Empty;
    }
}
