using System;
using System.ComponentModel.DataAnnotations;

namespace QASmartClass.Data
{
    /// <summary>
    /// Lưu trữ lịch sử hạnh kiểm của học sinh theo học kỳ và năm học.
    /// </summary>
    public class StudentConductHistory
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID học sinh
        /// </summary>
        public int StudentId { get; set; }

        /// <summary>
        /// Học kỳ: "HK1" hoặc "HK2"
        /// </summary>
        public string Semester { get; set; } = string.Empty;

        /// <summary>
        /// Năm học: ví dụ "2025-2026"
        /// </summary>
        public string SchoolYear { get; set; } = string.Empty;

        /// <summary>
        /// Điểm hạnh kiểm (0 - 100)
        /// </summary>
        public int ConductScore { get; set; } = 100;
    }
}
