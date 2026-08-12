using System;

namespace QASmartClass.Data
{
    public class StaffProfile
    {
        public int Id { get; set; }
        public string StaffCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public double BaseSalary { get; set; }
        public DateTime JoinedDate { get; set; } = DateTime.Now;
    }
}
