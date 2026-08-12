using System;

namespace QASmartClass.Data
{
    public class ResourceWasteLedger
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string JanitorStaffCode { get; set; } = string.Empty;
        public double OrganicKg { get; set; }
        public double RecyclableKg { get; set; }
        public double NonRecyclableKg { get; set; }
        public double HazardousKg { get; set; }
        public double ElectricityKwh { get; set; }
        public double WaterCubicMeters { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.Now;
    }
}
