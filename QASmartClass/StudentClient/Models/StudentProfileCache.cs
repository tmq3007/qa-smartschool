using System;

namespace QASmartClass.StudentClient.Models
{
    public class StudentProfileCache
    {
        public string StudentCode { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string TeacherIP { get; set; } = "";
        public bool RememberMe { get; set; } = true;
        public bool IsExitPinRequired { get; set; } = false;
        public string ExitPinCode { get; set; } = "";
        public int NetworkPort { get; set; } = 29877;
    }
}
