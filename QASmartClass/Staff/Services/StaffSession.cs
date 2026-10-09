using QASmartClass.Data;

namespace QASmartClass.Staff.Services
{
    public static class StaffSession
    {
        // BUG-03 FIX: Production build yêu cầu dang nh?p th?c, ch? DEV m?i dùng mock user
#if DEBUG
        public static TeacherProfile? CurrentUser { get; private set; } = new TeacherProfile { TeacherCode = "GV001", FullName = "Giáo viên H? th?ng", Role = "GV" };
#else
        public static TeacherProfile? CurrentUser { get; private set; } = null;
#endif
        public static string Role => CurrentUser?.Role ?? "GV";
        public static bool IsLoggedIn => CurrentUser != null;
        public static string DisplayName => CurrentUser?.FullName ?? "Khách";

        public static void Login(TeacherProfile user)
        {
            CurrentUser = user;
            if (user != null && !string.IsNullOrEmpty(user.TeacherCode))
            {
                try
                {
                    QASmartClass.Services.UserSessionService.Instance.SetSession(user.TeacherCode);
                }
                catch { }
            }
        }

        public static void Logout()
        {
            CurrentUser = null;
            try
            {
                QASmartClass.Services.UserSessionService.Instance.ClearSession();
            }
            catch { }
        }

        public static bool CanApprove() => Role is "Admin" or "HieuTruong" or "HieuPho";
        public static bool CanApproveLeaveRequests() => Role is "Admin" or "HieuTruong" or "HieuPho";
        public static bool CanResolveIncidents() => Role is "Admin" or "HieuTruong" or "HieuPho";
        public static bool CanReviewLessonPlans() => Role is "Admin" or "HieuTruong" or "HieuPho";
        public static bool CanReviewSkkn() => Role is "Admin" or "HieuTruong" or "HieuPho";
        public static bool CanManagePayroll() => Role is "Admin" or "HieuTruong";
        public static bool CanApproveOfficialDocuments() => Role is "Admin" or "HieuTruong";
        public static bool CanSendPush() => Role is "Admin" or "HieuTruong";
        public static bool CanAccessOverview() => Role is "Admin" or "HieuTruong";
        public static bool CanAccessSettings() => Role == "Admin";
        public static bool CanManageBulletin() => Role is "Admin" or "HieuTruong" or "HieuPho";
        public static bool CanCreateBulletin() => Role is "Admin" or "HieuTruong" or "HieuPho" or "GV" or "DV";

        // Youth Union position cached after login
        public static string YouthPosition { get; private set; } = "";
        public static void SetYouthPosition(string position) => YouthPosition = position ?? "";

        // Task 1.5: RBAC for YouthUnion module
        // youthPosition: "Bi thu", "Pho BT", "UV BCH", "Thanh vien"
        public static bool CanManageYouthUnion(string youthPosition = "")
        {
            // Admin/HieuTruong always can manage
            if (Role is "Admin" or "HieuTruong" or "HieuPho") return true;
            // BCH members (Bi thu, Pho BT, UV BCH)
            return youthPosition is "Bi thu" or "Pho BT" or "UV BCH" or "Bí thư" or "Phó BT" or "Phó Bí thư" or "UV BCH";
        }

        public static bool CanDeleteYouth(string youthPosition = "")
        {
            if (Role is "Admin" or "HieuTruong") return true;
            return youthPosition is "Bi thu" or "Bí thư"; // Only Bi thu can delete
        }

        public static bool CanApproveYouth(string youthPosition = "")
        {
            if (Role is "Admin" or "HieuTruong" or "HieuPho") return true;
            return youthPosition is "Bi thu" or "Pho BT" or "Bí thư" or "Phó BT" or "Phó Bí thư";
        }
    }
}

