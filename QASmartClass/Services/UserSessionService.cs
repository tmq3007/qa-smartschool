using System.Linq;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    /// <summary>
    /// V7 P2.2: Singleton luu thong tin user dang dang nhap.
    /// Set khi login thanh cong, doc o bat ky dau trong app.
    /// </summary>
    public class UserSessionService
    {
        private static UserSessionService? _instance;
        private static readonly object _lock = new();

        public static UserSessionService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock) { _instance ??= new UserSessionService(); }
                }
                return _instance;
            }
        }

        private UserSessionService() { }

        // --- Properties -----------------------------------
        public int TeacherId { get; private set; }
        public string TeacherCode { get; private set; } = string.Empty;
        public string FullName { get; private set; } = string.Empty;
        public string Role { get; private set; } = StatusConstants.TeacherRole.GiaoVien;
        public string Subject { get; private set; } = string.Empty;
        public bool IsLoggedIn => TeacherId > 0;

        // --- Methods --------------------------------------

        /// <summary>
        /// Goi sau khi AuthenticationService xac thuc thanh cong.
        /// Load thong tin GV tu DB va luu vao session.
        /// </summary>
        public void SetSession(string teacherCode)
        {
            try
            {
                using var db = new AppDbContext();
                var teacher = db.TeacherProfiles.FirstOrDefault(t => t.TeacherCode == teacherCode);
                if (teacher != null)
                {
                    TeacherId = teacher.Id;
                    TeacherCode = teacher.TeacherCode;
                    FullName = teacher.FullName;
                    Role = string.IsNullOrEmpty(teacher.Role) ? StatusConstants.TeacherRole.GiaoVien : teacher.Role;
                    Subject = teacher.Subject;

                    Serilog.Log.Information("[UserSession] Session set: {Code} / {Name} / Role={Role}",
                        TeacherCode, FullName, Role);
                }
            }
            catch (System.Exception ex)
            {
                Serilog.Log.Warning("[UserSession] Failed to set session: {Err}", ex.Message);
            }
        }

        /// <summary>Kiem tra user co vai tro cu the khong</summary>
        public bool HasRole(string role) => Role == role;

        /// <summary>Kiem tra user co quyen quan tri (ToTruong tro len)</summary>
        public bool IsManager =>
            Role == StatusConstants.TeacherRole.ToTruong ||
            Role == StatusConstants.TeacherRole.ToPho ||
            Role == StatusConstants.TeacherRole.HieuPho ||
            Role == StatusConstants.TeacherRole.HieuTruong ||
            Role == StatusConstants.TeacherRole.Admin;

        /// <summary>Kiem tra user la nhan vien tu van tam ly</summary>
        public bool IsCounselor =>
            Role == StatusConstants.TeacherRole.Counselor ||
            Role == StatusConstants.TeacherRole.HieuTruong ||
            Role == StatusConstants.TeacherRole.Admin;

        /// <summary>Xóa session khi logout</summary>
        public void ClearSession()
        {
            TeacherId = 0;
            TeacherCode = string.Empty;
            FullName = string.Empty;
            Role = StatusConstants.TeacherRole.GiaoVien;
            Subject = string.Empty;
        }
    }
}

