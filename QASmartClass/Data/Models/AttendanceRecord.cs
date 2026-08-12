using System;

namespace QASmartClass.Data
{
    public partial class AttendanceRecord
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int RosterId { get; set; }
        public DateTime Date { get; set; }
        /// <summary>present, late, absent, unknown</summary>
        public string Status { get; set; } = "unknown";
        public string Note { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>Kiểm tra có mặt</summary>
        public bool IsPresent => Status == "Present" || Status == "P";
    }
}
