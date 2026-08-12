using System;

namespace QASmartClass.Data
{
    public class SchoolAsset
    {
        public int Id { get; set; }
        public string AssetType { get; set; } = string.Empty; // Smartboard, Projector, Tablet
        public string AssetCode { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty; // VD: Phòng 10A1
        public DateTime PurchaseDate { get; set; }
        public DateTime NextMaintenanceDate { get; set; }
        public string Status { get; set; } = "Good"; // Good, NeedsMaintenance, Broken
        public decimal OriginalValue { get; set; }
        public decimal AccumulatedDepreciation { get; set; }
        public decimal RemainingValue { get; set; }
        public int DepreciationRatePercent { get; set; } = 10;
        public int RepairCount { get; set; } = 0;
        public double PositionX { get; set; } = 0.0;
        public double PositionY { get; set; } = 0.0;
    }
}
