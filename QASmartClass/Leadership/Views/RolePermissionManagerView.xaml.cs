using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Staff.Services;

namespace QASmartClass.Leadership.Views
{
    public partial class RolePermissionManagerView : Page
    {
        private readonly AppDbContext? _db;
        private bool _isDirty = false;
        private bool _isLoading = false;
        private bool _isChangingSelection = false;
        private SystemRole? _lastSelectedRole = null;
        private readonly Dictionary<string, bool> _originalPermissions = new();

        public RolePermissionManagerView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;
            _db = QASmartClass.Services.AppServices.Database ?? QASmartClass.Services.AppServices.CreateDb();
            LoadRoles();
        }

        // Constructor cho dependency injection
        public RolePermissionManagerView(AppDbContext db) : this()
        {
            _db = db;
        }

        private void LoadRoles()
        {
            var roles = new List<SystemRole>
            {
                new SystemRole { Id = 1, Name = "BGH (Leadership)", Description = "Toàn quyền truy cập quản lý hệ thống." },
                new SystemRole { Id = 2, Name = "Giáo viên (Teacher)", Description = "Chỉ truy cập Teacher Hub và gửi công văn." },
                new SystemRole { Id = 3, Name = "Nhân viên Y tế", Description = "Truy cập Phòng Y tế và Sổ sức khỏe." },
                new SystemRole { Id = 4, Name = "Nhân viên Bếp ăn", Description = "Truy cập thông tin suất ăn canteen." },
                new SystemRole { Id = 5, Name = "Bảo vệ (Security)", Description = "Theo dõi camera và cổng từ." }
            };

            lstRoles.ItemsSource = roles;
            lstRoles.SelectedIndex = 0;
        }

        private void LstRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isChangingSelection) return;

            if (_isDirty && _lastSelectedRole != null)
            {
                var confirm = MessageBox.Show(
                    $"Bạn có thay đổi chưa lưu cho vai trò '{_lastSelectedRole.Name}'. Bạn có muốn lưu các thay đổi này trước khi chuyển sang vai trò khác không?",
                    "Thay đổi chưa lưu",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question
                );

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        SaveAllPermissionsForRole(_lastSelectedRole);
                    }
                    catch
                    {
                        // Khôi phục nếu lỗi lưu
                        _isChangingSelection = true;
                        lstRoles.SelectedItem = _lastSelectedRole;
                        _isChangingSelection = false;
                        return;
                    }
                }
                else if (confirm == MessageBoxResult.Cancel)
                {
                    _isChangingSelection = true;
                    lstRoles.SelectedItem = _lastSelectedRole;
                    _isChangingSelection = false;
                    return;
                }
                else
                {
                    _isDirty = false;
                }
            }

            if (lstRoles.SelectedItem is SystemRole role)
            {
                _lastSelectedRole = role;
                _isLoading = true;

                txtRoleTitle.Text = $"Cấu hình quyền cho: {role.Name}";
                
                var perms = LoadPermissionsFromDb(role.Id);
                
                _originalPermissions.Clear();
                _originalPermissions["TeacherHub"] = perms.GetValueOrDefault("TeacherHub", role.Id == 1 || role.Id == 2);
                _originalPermissions["HealthRoom"] = perms.GetValueOrDefault("HealthRoom", role.Id == 1 || role.Id == 3);
                _originalPermissions["Kitchen"] = perms.GetValueOrDefault("Kitchen", role.Id == 1 || role.Id == 4);
                _originalPermissions["Security"] = perms.GetValueOrDefault("Security", role.Id == 1 || role.Id == 5);
                _originalPermissions["Leadership"] = perms.GetValueOrDefault("Leadership", role.Id == 1);
                _originalPermissions["AssignTasks"] = perms.GetValueOrDefault("AssignTasks", role.Id == 1);
                _originalPermissions["ApproveLeave"] = perms.GetValueOrDefault("ApproveLeave", role.Id == 1);
                _originalPermissions["SendPush"] = perms.GetValueOrDefault("SendPush", role.Id == 1);
                _originalPermissions["EditSystem"] = perms.GetValueOrDefault("EditSystem", role.Id == 1);

                chkTeacherHub.IsChecked = _originalPermissions["TeacherHub"];
                chkHealthRoom.IsChecked = _originalPermissions["HealthRoom"];
                chkKitchen.IsChecked = _originalPermissions["Kitchen"];
                chkSecurity.IsChecked = _originalPermissions["Security"];
                chkLeadership.IsChecked = _originalPermissions["Leadership"];

                chkAssignTasks.IsChecked = _originalPermissions["AssignTasks"];
                chkApproveLeave.IsChecked = _originalPermissions["ApproveLeave"];
                chkSendPush.IsChecked = _originalPermissions["SendPush"];
                chkEditSystem.IsChecked = _originalPermissions["EditSystem"];

                _isLoading = false;
                _isDirty = false;
            }
        }

        private void OnPermissionChanged(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;

            bool dirty = false;
            if (_originalPermissions.TryGetValue("TeacherHub", out var origTeacherHub) && chkTeacherHub.IsChecked != origTeacherHub) dirty = true;
            if (_originalPermissions.TryGetValue("HealthRoom", out var origHealthRoom) && chkHealthRoom.IsChecked != origHealthRoom) dirty = true;
            if (_originalPermissions.TryGetValue("Kitchen", out var origKitchen) && chkKitchen.IsChecked != origKitchen) dirty = true;
            if (_originalPermissions.TryGetValue("Security", out var origSecurity) && chkSecurity.IsChecked != origSecurity) dirty = true;
            if (_originalPermissions.TryGetValue("Leadership", out var origLeadership) && chkLeadership.IsChecked != origLeadership) dirty = true;
            if (_originalPermissions.TryGetValue("AssignTasks", out var origAssignTasks) && chkAssignTasks.IsChecked != origAssignTasks) dirty = true;
            if (_originalPermissions.TryGetValue("ApproveLeave", out var origApproveLeave) && chkApproveLeave.IsChecked != origApproveLeave) dirty = true;
            if (_originalPermissions.TryGetValue("SendPush", out var origSendPush) && chkSendPush.IsChecked != origSendPush) dirty = true;
            if (_originalPermissions.TryGetValue("EditSystem", out var origEditSystem) && chkEditSystem.IsChecked != origEditSystem) dirty = true;

            _isDirty = dirty;

            if (lstRoles.SelectedItem is SystemRole role)
            {
                if (_isDirty)
                {
                    txtRoleTitle.Text = $"Cấu hình quyền cho: {role.Name} (* Chưa lưu)";
                }
                else
                {
                    txtRoleTitle.Text = $"Cấu hình quyền cho: {role.Name}";
                }
            }
        }

        private void SaveAllPermissionsForRole(SystemRole role)
        {
            if (_db == null) return;

            SavePermissionAndRbac(role.Id, "TeacherHub", chkTeacherHub.IsChecked == true);
            SavePermissionAndRbac(role.Id, "HealthRoom", chkHealthRoom.IsChecked == true);
            SavePermissionAndRbac(role.Id, "Kitchen", chkKitchen.IsChecked == true);
            SavePermissionAndRbac(role.Id, "Security", chkSecurity.IsChecked == true);
            SavePermissionAndRbac(role.Id, "Leadership", chkLeadership.IsChecked == true);
            SavePermissionAndRbac(role.Id, "AssignTasks", chkAssignTasks.IsChecked == true);
            SavePermissionAndRbac(role.Id, "ApproveLeave", chkApproveLeave.IsChecked == true);
            SavePermissionAndRbac(role.Id, "SendPush", chkSendPush.IsChecked == true);
            SavePermissionAndRbac(role.Id, "EditSystem", chkEditSystem.IsChecked == true);

            string currentActorName = StaffSession.CurrentUser?.FullName ?? "Quản trị viên";
            _db.AuditLogs.Add(new AuditLog
            {
                Action = "RolePermission_Update",
                ActorName = currentActorName,
                Details = $"Cập nhật phân quyền cho role '{role.Name}' (ID={role.Id})",
                Timestamp = DateTime.Now
            });

            _db.SaveChanges();
        }

        /// <summary>S2-04: Lưu permissions vào DB qua SystemSettings và RolePermissions</summary>
        private void BtnSavePermissions_Click(object sender, RoutedEventArgs e)
        {
            if (lstRoles.SelectedItem is not SystemRole role || _db == null) return;

            try
            {
                SaveAllPermissionsForRole(role);

                _originalPermissions["TeacherHub"] = chkTeacherHub.IsChecked == true;
                _originalPermissions["HealthRoom"] = chkHealthRoom.IsChecked == true;
                _originalPermissions["Kitchen"] = chkKitchen.IsChecked == true;
                _originalPermissions["Security"] = chkSecurity.IsChecked == true;
                _originalPermissions["Leadership"] = chkLeadership.IsChecked == true;
                _originalPermissions["AssignTasks"] = chkAssignTasks.IsChecked == true;
                _originalPermissions["ApproveLeave"] = chkApproveLeave.IsChecked == true;
                _originalPermissions["SendPush"] = chkSendPush.IsChecked == true;
                _originalPermissions["EditSystem"] = chkEditSystem.IsChecked == true;

                _isDirty = false;
                txtRoleTitle.Text = $"Cấu hình quyền cho: {role.Name}";

                MessageBox.Show("Cập nhật phân quyền thành công! Đã lưu vào cơ sở dữ liệu.", "Lưu phân quyền", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu phân quyền: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePermissionAndRbac(int roleId, string permName, bool value)
        {
            // 1. Save to SystemSettings
            SavePermission(roleId, permName, value);

            // 2. Save to RolePermissions for StaffDashboard dynamic authorization
            string dbRole = GetDbRole(roleId);
            string[] tags = GetTagsForPermission(permName);

            foreach (var tag in tags)
            {
                var existing = _db!.RolePermissions.FirstOrDefault(rp => rp.Role == dbRole && rp.PermissionTag == tag);
                if (value)
                {
                    if (existing == null)
                    {
                        _db.RolePermissions.Add(new RolePermission
                        {
                            Role = dbRole,
                            PermissionTag = tag
                        });
                    }
                }
                else
                {
                    if (existing != null)
                    {
                        _db.RolePermissions.Remove(existing);
                    }
                }
            }
        }

        private string GetDbRole(int roleId) => roleId switch
        {
            1 => "HieuPho",
            2 => "GV",
            3 => "YTe",
            4 => "Bep",
            5 => "BaoVe",
            _ => "GV"
        };

        private string[] GetTagsForPermission(string permName) => permName switch
        {
            "TeacherHub" => new[] { "TeacherHub" },
            "HealthRoom" => new[] { "health", "epidemic", "medical_inventory", "emergency", "counseling" },
            "Kitchen" => new[] { "kitchen", "Canteen", "food_safety" },
            "Security" => new[] { "security", "Gate", "emergency" },
            "Leadership" => new[] { "push_notification", "staff_performance", "moet", "reports", "document_manager", "leave_request", "mobile_app", "app_analytics", "tuition", "assets", "department_mgmt", "award_mgmt" },
            "AssignTasks" => new[] { "task_management" },
            "ApproveLeave" => new[] { "leave_request" },
            "SendPush" => new[] { "push_notification" },
            "EditSystem" => new[] { "system_settings", "backup_restore", "role_manager", "system_audit", "data_exporter" },
            _ => Array.Empty<string>()
        };

        private void SavePermission(int roleId, string permName, bool value)
        {
            string key = $"Role_{roleId}_Perm_{permName}";
            var existing = _db!.SystemSettings.Find(key);
            if (existing != null)
            {
                existing.Value = value ? "true" : "false";
                existing.LastUpdated = DateTime.Now;
            }
            else
            {
                _db.SystemSettings.Add(new SystemSetting
                {
                    Id = key,
                    Value = value ? "true" : "false",
                    Category = "Permission",
                    LastUpdated = DateTime.Now
                });
            }
        }

        private Dictionary<string, bool> LoadPermissionsFromDb(int roleId)
        {
            var result = new Dictionary<string, bool>();
            if (_db == null) return result;

            try
            {
                var perms = _db.SystemSettings
                    .Where(s => s.Category == "Permission" && s.Id.StartsWith($"Role_{roleId}_Perm_"))
                    .ToList();

                foreach (var p in perms)
                {
                    string permName = p.Id.Replace($"Role_{roleId}_Perm_", "");
                    result[permName] = p.Value == "true";
                }
            }
            catch { }

            return result;
        }
    }

    public class SystemRole
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}

