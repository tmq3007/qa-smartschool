namespace QASmartClass.Data
{
    /// <summary>Mở rộng Bulletin — helper</summary>
    public partial class Bulletin
    {
        /// <summary>Ki?m tra dă công b?</summary>
        public bool IsPublished => Status == "Published";
        
        /// <summary>Đang ch? duy?t</summary>
        public bool IsPending => Status == "Pending";
    }
}

