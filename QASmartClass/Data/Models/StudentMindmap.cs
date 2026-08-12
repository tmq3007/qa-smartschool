using System;
using System.ComponentModel.DataAnnotations;

namespace QASmartClass.Data
{
    public class StudentMindmap
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; } = "Chủ đề";

        [Required]
        public string DataJson { get; set; } = "{}";

        public string? Category { get; set; }

        [Required]
        public string UpdatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        public string? StudentCode { get; set; }
    }
}
