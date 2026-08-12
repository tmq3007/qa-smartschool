using System;

namespace QASmartClass.Data
{
    public class StudentCarePlan
    {
        public int Id { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string ChronicCondition { get; set; } = string.Empty;
        public string MedicalTriggers { get; set; } = string.Empty;
        public string EmergencyMeasures { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
