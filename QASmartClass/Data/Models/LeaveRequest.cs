using System;

namespace QASmartClass.Data
{
    public class LeaveRequest
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string LeaveType { get; set; } = string.Empty; // Sick, Annual, Unpaid
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string ApprovedBy { get; set; } = string.Empty;
        public string ResponseNotes { get; set; } = string.Empty;
    }
}
