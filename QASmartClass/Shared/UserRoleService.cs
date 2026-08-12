using System;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace QASmartClass.Shared
{
    // ═══════════════════════════════════════════════════════════
    //  USER ROLES — Hệ sinh thái QA Smart School
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Vai trò người dùng trong hệ sinh thái QA Smart School.
    /// Mỗi vai trò quyết định:
    /// - Giao diện nào được hiển thị
    /// - Chức năng nào được bật/tắt
    /// - Quyền truy cập dữ liệu
    /// </summary>
    public enum UserRole
    {
        /// <summary>1. Chỉ sử dụng bảng trắng tương tác (không có quản lý lớp)</summary>
        SmartTouchOnly = 1,

        /// <summary>2. Giáo viên — Quản lý lớp học + Bảng trắng</summary>
        Teacher = 2,

        /// <summary>3. Học sinh — Kết nối vào lớp, nhận bài, nộp bài</summary>
        Student = 3,

        /// <summary>4. Nhân viên thư viện — Quản lý tài liệu, sách, mượn/trả</summary>
        Librarian = 4,

        /// <summary>5. Quản trị viên CNTT — Cài đặt, mạng, bảo trì hệ thống</summary>
        ITAdmin = 5,

        /// <summary>6. Ban giám hiệu — Giám sát, báo cáo tổng thể</summary>
        SchoolAdmin = 6,

        /// <summary>7. Phụ huynh — Xem kết quả, nhắn tin với GV</summary>
        Parent = 7,

        /// <summary>8. Nhân viên văn phòng — Hành chính, thông báo</summary>
        OfficeStaff = 8,

        /// <summary>9. Trợ giảng / Thực tập sinh — Quyền hạn chế</summary>
        TeachingAssistant = 9,

        /// <summary>10. Khách — Chỉ xem, không tương tác</summary>
        Guest = 10
    }

    /// <summary>
    /// Thông tin mô tả chi tiết cho mỗi vai trò.
    /// </summary>
    public class UserRoleInfo
    {
        public UserRole Role { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Color { get; set; } = "#1976D2";
        public string[] AvailableModules { get; set; } = Array.Empty<string>();
        public bool IsAvailable { get; set; } = true;

        /// <summary>Danh sách tất cả vai trò với mô tả</summary>
        public static UserRoleInfo[] All => new[]
        {
            new UserRoleInfo
            {
                Role = UserRole.SmartTouchOnly,
                Name = "QA SmartTouch",
                Icon = "🖊️",
                Description = "Chỉ sử dụng bảng trắng tương tác. Không có quản lý lớp học.",
                Color = "#5C6BC0",
                AvailableModules = new[] { "SmartScreen" },
                IsAvailable = true
            },
            new UserRoleInfo
            {
                Role = UserRole.Teacher,
                Name = "Giáo viên (Smart Class)",
                Icon = "👨‍🏫",
                Description = "Quản lý lớp học, soạn bài, kiểm tra, tương tác với học sinh.",
                Color = "#1976D2",
                AvailableModules = new[] { "SmartScreen", "SmartClass", "Quiz", "Lesson", "Report", "Student", "Monitor" },
                IsAvailable = true
            },
            new UserRoleInfo
            {
                Role = UserRole.Student,
                Name = "Học sinh",
                Icon = "🎓",
                Description = "Kết nối vào lớp, nhận bài giảng, làm bài kiểm tra, chat.",
                Color = "#43A047",
                AvailableModules = new[] { "StudentClient" },
                IsAvailable = true
            },
            new UserRoleInfo
            {
                Role = UserRole.Librarian,
                Name = "Nhân viên thư viện",
                Icon = "📚",
                Description = "Quản lý tài liệu, sách giáo khoa, mượn/trả, thống kê.",
                Color = "#FF8F00",
                AvailableModules = new[] { "Library", "Report" },
                IsAvailable = true
            },
            new UserRoleInfo
            {
                Role = UserRole.ITAdmin,
                Name = "Quản trị viên CNTT",
                Icon = "🔧",
                Description = "Cài đặt hệ thống, quản lý mạng, bảo trì, sao lưu dữ liệu.",
                Color = "#E65100",
                AvailableModules = new[] { "SmartScreen", "SmartClass", "Settings", "Monitor", "EventLog", "Report" },
                IsAvailable = true
            },
            new UserRoleInfo
            {
                Role = UserRole.SchoolAdmin,
                Name = "Ban giám hiệu",
                Icon = "🏫",
                Description = "Giám sát toàn trường, xem báo cáo, phê duyệt bài giảng.",
                Color = "#6A1B9A",
                AvailableModules = new[] { "Dashboard", "Report", "Timetable", "Monitor" },
                IsAvailable = false // Chưa phát triển
            },
            new UserRoleInfo
            {
                Role = UserRole.Parent,
                Name = "Phụ huynh",
                Icon = "👨‍👩‍👧",
                Description = "Xem kết quả học tập, nhắn tin với giáo viên, theo dõi con em.",
                Color = "#00838F",
                AvailableModules = new[] { "ParentPortal" },
                IsAvailable = false // Chưa phát triển
            },
            new UserRoleInfo
            {
                Role = UserRole.OfficeStaff,
                Name = "Nhân viên văn phòng",
                Icon = "📋",
                Description = "Hành chính, thông báo, quản lý hồ sơ, lịch công tác.",
                Color = "#4E342E",
                AvailableModules = new[] { "Office", "Notification" },
                IsAvailable = false // Chưa phát triển
            },
            new UserRoleInfo
            {
                Role = UserRole.TeachingAssistant,
                Name = "Trợ giảng / Thực tập",
                Icon = "🧑‍🏫",
                Description = "Hỗ trợ giáo viên, quản lý lớp với quyền hạn chế.",
                Color = "#558B2F",
                AvailableModules = new[] { "SmartScreen", "SmartClass" },
                IsAvailable = false // Chưa phát triển
            },
            new UserRoleInfo
            {
                Role = UserRole.Guest,
                Name = "Khách tham quan",
                Icon = "👤",
                Description = "Chỉ xem bảng trắng, không tương tác, không lưu dữ liệu.",
                Color = "#9E9E9E",
                AvailableModules = new[] { "SmartScreen" },
                IsAvailable = true
            }
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  USER ROLE SERVICE — Quản lý vai trò người dùng
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Service quản lý vai trò người dùng.
    /// Lưu/đọc từ file JSON, quyết định giao diện và chức năng.
    /// </summary>
    public partial class UserRoleService : ObservableObject
    {
        private static readonly string RoleFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QASmartClass", "user_role.json");

        [ObservableProperty]
        private UserRole _currentRole = UserRole.Teacher;

        [ObservableProperty]
        private bool _isFirstLaunch = false;

        [ObservableProperty]
        private string _displayName = "Giáo viên";

        /// <summary>Event khi vai trò thay đổi</summary>
        public event EventHandler<UserRole>? RoleChanged;

        public UserRoleService()
        {
            LoadRole();
        }

        // ─── LOAD ────────────────────────────────────────────────

        /// <summary>Đọc vai trò đã lưu từ file</summary>
        public void LoadRole()
        {
            try
            {
                if (File.Exists(RoleFilePath))
                {
                    var json = File.ReadAllText(RoleFilePath);
                    var saved = JsonSerializer.Deserialize<SavedRole>(json);
                    if (saved != null)
                    {
                        CurrentRole = saved.Role;
                        DisplayName = saved.DisplayName;
                        IsFirstLaunch = false;
                        Log.Information("UserRole loaded: {Role} ({Name})", CurrentRole, DisplayName);
                        return;
                    }
                }

                // File chưa tồn tại → lần đầu chạy
                IsFirstLaunch = true;
                CurrentRole = UserRole.Teacher; // Mặc định
                DisplayName = "Giáo viên";
                Log.Information("UserRole: First launch, default = Teacher");
            }
            catch (Exception ex)
            {
                Log.Warning("UserRole load error: {Err}", ex.Message);
                IsFirstLaunch = true;
            }
        }

        // ─── SAVE ────────────────────────────────────────────────

        /// <summary>Lưu vai trò vào file</summary>
        public void SaveRole(UserRole role, string displayName = "")
        {
            try
            {
                var oldRole = CurrentRole;
                CurrentRole = role;
                DisplayName = string.IsNullOrEmpty(displayName)
                    ? GetRoleInfo(role).Name
                    : displayName;

                var saved = new SavedRole
                {
                    Role = role,
                    DisplayName = DisplayName,
                    SavedAt = DateTime.Now
                };

                Directory.CreateDirectory(Path.GetDirectoryName(RoleFilePath)!);
                var json = JsonSerializer.Serialize(saved, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(RoleFilePath, json);

                IsFirstLaunch = false;
                RoleChanged?.Invoke(this, role);

                Log.Information("UserRole saved: {Old} → {New} ({Name})", oldRole, role, DisplayName);
            }
            catch (Exception ex)
            {
                Log.Warning("UserRole save error: {Err}", ex.Message);
            }
        }

        // ─── QUERY ───────────────────────────────────────────────

        /// <summary>Lấy thông tin chi tiết vai trò</summary>
        public static UserRoleInfo GetRoleInfo(UserRole role)
        {
            foreach (var info in UserRoleInfo.All)
                if (info.Role == role) return info;
            return UserRoleInfo.All[0];
        }

        /// <summary>Kiểm tra vai trò hiện tại có quyền truy cập module không</summary>
        public bool HasModule(string moduleName)
        {
            var info = GetRoleInfo(CurrentRole);
            foreach (var m in info.AvailableModules)
                if (m.Equals(moduleName, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>Vai trò hiện tại có bảng trắng không?</summary>
        public bool CanUseSmartScreen => HasModule("SmartScreen");

        /// <summary>Vai trò hiện tại có quản lý lớp không?</summary>
        public bool CanUseSmartClass => HasModule("SmartClass");

        /// <summary>Vai trò hiện tại là học sinh?</summary>
        public bool IsStudentRole => CurrentRole == UserRole.Student;

        /// <summary>Vai trò hiện tại cần mở StudentClient?</summary>
        public bool ShouldOpenStudentClient => CurrentRole == UserRole.Student;

        /// <summary>Vai trò hiện tại cần FloatingModeBar?</summary>
        public bool ShouldShowModeBar =>
            CurrentRole == UserRole.Teacher ||
            CurrentRole == UserRole.ITAdmin ||
            CurrentRole == UserRole.TeachingAssistant;

        /// <summary>Vai trò hiện tại chỉ dùng SmartTouch?</summary>
        public bool IsSmartTouchOnly => CurrentRole == UserRole.SmartTouchOnly;

        // ─── MODEL ───────────────────────────────────────────────

        private class SavedRole
        {
            public UserRole Role { get; set; } = UserRole.Teacher;
            public string DisplayName { get; set; } = "Giáo viên";
            public DateTime SavedAt { get; set; } = DateTime.Now;
        }
    }
}
