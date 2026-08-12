using System;

namespace QASmartClass.Data
{
    public class KitchenColdStorageLog
    {
        public int Id { get; set; }
        public string FridgeId { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsViolation { get; set; } = false;
    }
}
