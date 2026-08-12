using System;

namespace SmartLibrary.Desktop.Models
{
    public class LostBookVoucherDto
    {
        public int Id { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string SsoUserId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal PenaltyAmount { get; set; }
        public string PenaltyRule { get; set; } = string.Empty;
        public DateTime LostDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Chờ thu phạt"; // e.g. "Chờ thu phạt", "Đã thu phạt"
    }
}
