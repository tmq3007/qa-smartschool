using System;

namespace QASmartClass.Data
{
    public class StaffPayrollLedger
    {
        public int Id { get; set; }
        public string StaffCode { get; set; } = string.Empty;
        public string MonthYear { get; set; } = string.Empty; // "MM/yyyy"
        public decimal BaseSalary { get; set; }
        public double OvertimeHours { get; set; }
        public decimal OvertimeRate { get; set; }
        public decimal TeachingBonus { get; set; }
        public decimal FinalAmount { get; set; }
        public string LedgerChecksum { get; set; } = string.Empty; // HMAC-SHA256 checksum
        public bool IsLocked { get; set; } = false;
        public DateTime CalculatedAt { get; set; } = DateTime.Now;
    }
}
