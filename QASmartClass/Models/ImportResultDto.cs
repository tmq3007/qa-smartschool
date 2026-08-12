using System.Collections.Generic;

namespace QASmartClass.Models
{
    public class ImportResultDto
    {
        public int TotalProcessed { get; set; } = 0;
        public int SuccessCount { get; set; } = 0;
        public int ErrorCount { get; set; } = 0;
        public List<string> ErrorDetails { get; set; } = new List<string>();
        public bool IsSuccess => ErrorCount == 0;
    }
}

