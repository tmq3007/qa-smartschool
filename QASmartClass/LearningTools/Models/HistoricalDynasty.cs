namespace QASmartClass.LearningTools.Models
{
    public class HistoricalDynasty
    {
        public int Id { get; set; }
        public string Category { get; set; } = "Vietnam"; // "Vietnam" or "World"
        public string NameVi { get; set; } = "";
        public string NameEn { get; set; } = "";
        public string Period { get; set; } = "";
        public string DetailIcon { get; set; } = "👤";
        public string DetailVi { get; set; } = "";
        public string DetailEn { get; set; } = "";
        public string EventsVi { get; set; } = "";
        public string EventsEn { get; set; } = "";
        public int DisplayOrder { get; set; }
    }
}
