namespace QASmartClass.Data
{
    /// <summary>Mở rộng StudentGrade — helper</summary>
    public partial class StudentGrade
    {
        /// <summary>Đi?m d?t (>=5.0)</summary>
        public bool IsPassing => Score >= 5.0;
    }
}

