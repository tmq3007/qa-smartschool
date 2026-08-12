namespace QASmartClass.Data
{
    /// <summary>
    /// P1.4: Constants thay th? magic strings toàn hệ thống.
    /// Tránh typo, d? refactor, IntelliSense h? tr?.
    /// </summary>
    public static class StatusConstants
    {
        public static class Approval
        {
            public const string Pending = "Pending";
            public const string Approved = "Approved";
            public const string Rejected = "Rejected";
        }

        public static class Risk
        {
            public const string Low = "Low";
            public const string Medium = "Medium";
            public const string High = "High";
            public const string Critical = "Critical";
        }

        public static class TaskStatus
        {
            public const string Todo = "Todo";
            public const string InProgress = "InProgress";
            public const string Done = "Done";
        }

        public static class StudentStatus
        {
            public const string Active = "Active";
            public const string Inactive = "Inactive";
            public const string Graduated = "Graduated";
            public const string Transferred = "Transferred";
        }

        public static class TeacherRole
        {
            public const string GiaoVien = "GV";
            public const string ToTruong = "ToTruong";
            public const string ToPho = "ToPho";
            public const string HieuPho = "HieuPho";
            public const string HieuTruong = "HieuTruong";
            public const string Admin = "Admin";
            public const string Counselor = "Counselor";
        }

        public static class CounselingStatus
        {
            public const string Pending = "Chờ xử lý";
            public const string Scheduled = "Đã lên lịch";
            public const string InProgress = "Đang theo dõi";
            public const string Completed = "Đã hoàn thành";
        }
    }
}

