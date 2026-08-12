using System;

namespace QASmartClass.Data
{
    public class ChemicalRequisition
    {
        public int Id { get; set; }
        public int AssetBookingId { get; set; }
        public string ChemicalName { get; set; } = string.Empty;
        public double RequiredQuantity { get; set; }
        public bool IsHazardous { get; set; } = false;
        public string ApprovedByHOD { get; set; } = string.Empty; // Pending, Approved, Rejected
        public string ApprovedByPrincipal { get; set; } = string.Empty; // Pending, Approved, Rejected
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public string SafetyNotes { get; set; } = string.Empty;
    }
}
