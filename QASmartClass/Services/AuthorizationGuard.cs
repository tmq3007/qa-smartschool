using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;

namespace QASmartClass.Services
{
    public class UnauthorizedRoleException : Exception
    {
        public string RequiredRoles { get; }
        public string CurrentRole { get; }

        public UnauthorizedRoleException(string currentRole, string[] requiredRoles)
            : base($"Truy cập bị từ chối: Quyền hiện tại '{currentRole}' không thu?c danh sách quyền yêu cầu [{string.Join(", ", requiredRoles)}].")
        {
            CurrentRole = currentRole;
            RequiredRoles = string.Join(", ", requiredRoles);
        }
    }

    /// <summary>
    /// WI-09 Fix: Enforce role-based access control.
    /// Roles: GV (teacher), ToTruong (dept head), HieuPho (vice principal), HieuTruong (principal), Admin, Counselor
    /// </summary>
    public static class AuthorizationGuard
    {
        private static readonly string[] LeadershipRoles = { "HieuTruong", "HieuPho", "Admin" };
        private static readonly string[] DeptHeadRoles = { "HieuTruong", "HieuPho", "Admin", "ToTruong" };
        private static readonly string[] AllRoles = { "GV", "ToTruong", "HieuPho", "HieuTruong", "Admin", "Counselor" };
        private static readonly string[] CounselingRoles = { "HieuTruong", "Admin", "Counselor" };

        /// <summary>
        /// Ki?m tra vai tṛ c?a ngu?i dùng hi?n tại, ném ngo?i l? n?u không có quy?n.
        /// </summary>
        public static void EnsureRole(string currentRole, params string[] allowedRoles)
        {
            if (string.IsNullOrEmpty(currentRole))
            {
                Log.Warning("AuthorizationGuard: Denied access - role is null or empty");
                throw new UnauthorizedRoleException("Chưa đăng nhập", allowedRoles);
            }

            if (!allowedRoles.Contains(currentRole))
            {
                Log.Warning("AuthorizationGuard: Denied access - Current role '{Role}' is not allowed. Allowed: [{Allowed}]", 
                    currentRole, string.Join(", ", allowedRoles));
                
                throw new UnauthorizedRoleException(currentRole, allowedRoles);
            }
        }

        /// <summary>
        /// Helper ki?m tra nhanh xem ngu?i dùng hi?n tại có thu?c vai tṛ ch? d?nh hay không.
        /// </summary>
        public static bool IsAuthorized(string currentRole, params string[] allowedRoles)
        {
            if (string.IsNullOrEmpty(currentRole)) return false;
            return allowedRoles.Contains(currentRole);
        }

        /// <summary>
        /// Check if the current user has permission to access Leadership features.
        /// Returns true if allowed, false if denied.
        /// </summary>
        public static bool CanAccessLeadership(string userRole)
        {
            if (string.IsNullOrWhiteSpace(userRole)) return false;
            bool allowed = LeadershipRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
            if (!allowed) Log.Warning("[AuthGuard] Access denied: Role '{Role}' cannot access Leadership", userRole);
            return allowed;
        }

        /// <summary>
        /// Check if user can manage department (T? tru?ng tr? lên).
        /// </summary>
        public static bool CanManageDepartment(string userRole)
        {
            if (string.IsNullOrWhiteSpace(userRole)) return false;
            return DeptHeadRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if user can approve awards/discipline (HieuTruong/Admin only).
        /// </summary>
        public static bool CanApprove(string userRole)
        {
            if (string.IsNullOrWhiteSpace(userRole)) return false;
            return userRole.Equals("HieuTruong", StringComparison.OrdinalIgnoreCase)
                || userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if user can export MOET reports (HieuTruong/Admin only).
        /// </summary>
        public static bool CanExportMoet(string userRole)
        {
            return CanApprove(userRole);
        }

        /// <summary>
        /// Check if user can modify system settings (Admin only).
        /// </summary>
        public static bool CanAccessSystemSettings(string userRole)
        {
            if (string.IsNullOrWhiteSpace(userRole)) return false;
            return userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Phase 5: Check if the user can access Counseling Hub.
        /// </summary>
        public static bool CanAccessCounselingHub(string userRole)
        {
            if (string.IsNullOrWhiteSpace(userRole)) return false;
            return CounselingRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validate role string.
        /// </summary>
        public static bool IsValidRole(string role)
        {
            return !string.IsNullOrWhiteSpace(role) && AllRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }
    }
}

