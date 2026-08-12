using System;

namespace QASmartClass.Data
{
    public class SchoolAssetBooking
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public string BookedBy { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public int TimeSlot { get; set; } // 1 - 10
    }
}
