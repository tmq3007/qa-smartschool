using System;

namespace QASmartClass.Data
{
    /// <summary>Thông tin giáo viên (avatar, tên, chức danh...)</summary>
    public partial class TeacherProfile
    {
        public int Id { get; set; }
        /// <summary>Mã giáo viên (khóa chính nghiệp vụ, duy nhất). VD: GV001, GV002</summary>
        public string TeacherCode { get; set; } = string.Empty;
        /// <summary>HMACSHA512 salted hash format: "salt:hash"</summary>
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = "Giáo viên";
        public string Subject { get; set; } = string.Empty;
        public string School { get; set; } = string.Empty;
        public string Title { get; set; } = "GV"; // GV, ThS, TS, PGS, GS
        public string AvatarPath { get; set; } = string.Empty;
        /// <summary>Mật khẩu xác thực chuyển chế độ GV (mặc định: rỗng)</summary>
        public string TeacherPassword { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string Notes { get; set; } = string.Empty;
        /// <summary>Vai trò trong hệ thống (GV, ToTruong, HieuPho, HieuTruong, Admin, Counselor)</summary>
        public string Role { get; set; } = "GV";
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>Tên hiển thị: "GV. Nguyễn Thị B"</summary>
        public string DisplayName => $"GV. {FullName}";
    }
}
