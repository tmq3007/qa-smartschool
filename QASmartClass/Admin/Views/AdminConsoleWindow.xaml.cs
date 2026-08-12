using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Admin.Views
{
    /// <summary>
    /// Quyền truy cập Admin Console:
    /// L1 = QA Vendor (Tổng công ty) — xem tất cả
    /// L2 = School Admin (Quản trị trường) — xem tabs 0-4, 6 (ẩn Vendor)
    /// L3 = Teacher IT (Giáo viên IT) — xem tabs 0-3, 6 (ẩn School + Vendor)
    /// </summary>
    public enum AdminRole
    {
        L1_Vendor,      // Full access
        L2_SchoolAdmin, // School-level access
        L3_Teacher      // Teacher-level access
    }

    public partial class AdminConsoleWindow : Window
    {
        private readonly string _baseDir;
        private AdminRole _currentRole = AdminRole.L3_Teacher;

        /// <summary>
        /// Constructor mặc định — tự xác định role từ cấu hình
        /// </summary>
        public AdminConsoleWindow() : this(DetectRole())
        {
        }

        /// <summary>
        /// Constructor cho phép chỉ định role rõ ràng
        /// </summary>
        public AdminConsoleWindow(AdminRole role)
        {
            InitializeComponent();
            _baseDir = QASmartClass.Services.AppPaths.RootDir;
            _currentRole = role;

            // Cho phép kéo thả cửa sổ
            this.MouseLeftButtonDown += (s, e) => { this.DragMove(); };
            
            Loaded += (_, _) =>
            {
                ApplyPermissions();
                RefreshLogs_Click(null, null);
            };
        }

        /// <summary>
        /// Tự động xác định role dựa trên file cấu hình admin_role.txt
        /// Mặc định: L3_Teacher (quyền thấp nhất — an toàn nhất)
        /// </summary>
        private static AdminRole DetectRole()
        {
            try
            {
                var roleFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, "admin_role.txt");
                if (File.Exists(roleFile))
                {
                    using var db = new AppDbContext();
                    var secureSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "RoleSecurityMode");
                    string secureMode = secureSetting?.Value ?? "DPAPI_Encrypted";

                    if (secureMode == "DPAPI_Encrypted")
                    {
                        byte[] encryptedBytes = File.ReadAllBytes(roleFile);
                        byte[] decryptedBytes = ProtectedData.Unprotect(
                            encryptedBytes, null, DataProtectionScope.LocalMachine);
                        string roleName = System.Text.Encoding.UTF8.GetString(decryptedBytes).Trim().ToUpperInvariant();
                        return ParseRole(roleName);
                    }
                    else
                    {
                        // Chế độ Plaintext cho môi trường phòng máy BootROM/Virtual Lab
                        string roleName = File.ReadAllText(roleFile).Trim().ToUpperInvariant();
                        return ParseRole(roleName);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error decrypting admin role file or file was altered: " + ex.Message);
            }
            return AdminRole.L3_Teacher;
        }

        private static AdminRole ParseRole(string roleName)
        {
            return roleName switch
            {
                "L1" or "VENDOR" or "L1_VENDOR" => AdminRole.L1_Vendor,
                "L2" or "SCHOOL" or "L2_SCHOOLADMIN" => AdminRole.L2_SchoolAdmin,
                _ => AdminRole.L3_Teacher
            };
        }

        /// <summary>
        /// Lưu trữ an toàn vai trò của admin
        /// </summary>
        public static void SaveRoleSecurely(string roleName)
        {
            try
            {
                var roleFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, "admin_role.txt");
                using var db = new AppDbContext();
                var secureSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "RoleSecurityMode");
                string secureMode = secureSetting?.Value ?? "DPAPI_Encrypted";

                if (secureMode == "DPAPI_Encrypted")
                {
                    byte[] plaintextBytes = System.Text.Encoding.UTF8.GetBytes(roleName.Trim().ToUpperInvariant());
                    byte[] encryptedBytes = ProtectedData.Protect(
                        plaintextBytes, null, DataProtectionScope.LocalMachine);
                    File.WriteAllBytes(roleFile, encryptedBytes);
                }
                else
                {
                    File.WriteAllText(roleFile, roleName.Trim().ToUpperInvariant());
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi lưu trữ bảo mật vai trò quản trị.");
            }
        }

        /// <summary>
        /// Áp dụng phân quyền hiển thị dựa trên role
        /// </summary>
        private void ApplyPermissions()
        {
            switch (_currentRole)
            {
                case AdminRole.L1_Vendor:
                    // Full access — hiện tất cả
                    if (btnTabSchool != null) btnTabSchool.Visibility = Visibility.Visible;
                    if (btnTabVendor != null) btnTabVendor.Visibility = Visibility.Visible;
                    if (txtRoleDisplay != null)
                    {
                        txtRoleDisplay.Text = "L1 — QA Vendor (Full)";
                        txtRoleDisplay.Foreground = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(244, 63, 94)); // #F43F5E
                    }
                    break;

                case AdminRole.L2_SchoolAdmin:
                    // Ẩn Vendor tab
                    if (btnTabSchool != null) btnTabSchool.Visibility = Visibility.Visible;
                    if (btnTabVendor != null) btnTabVendor.Visibility = Visibility.Collapsed;
                    if (txtRoleDisplay != null)
                    {
                        txtRoleDisplay.Text = "L2 — Quản trị Trường";
                        txtRoleDisplay.Foreground = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(244, 114, 182)); // #F472B6
                    }
                    break;

                case AdminRole.L3_Teacher:
                default:
                    // Ẩn cả School + Vendor tab
                    if (btnTabSchool != null) btnTabSchool.Visibility = Visibility.Collapsed;
                    if (btnTabVendor != null) btnTabVendor.Visibility = Visibility.Collapsed;
                    if (txtRoleDisplay != null)
                    {
                        txtRoleDisplay.Text = "L3 — Giáo viên IT";
                        txtRoleDisplay.Foreground = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(251, 191, 36)); // #FBBF24
                    }
                    break;
            }

            Log.Information("[ADMIN_ACTION] Admin Console opened with role: {Role}", _currentRole);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int index))
            {
                MainTabControl.SelectedIndex = index;
            }
        }

        // ═══════════════════════════════════════════════════════
        //  MODE SWITCHER
        // ═══════════════════════════════════════════════════════

        private void ForceTeacher_Click(object sender, RoutedEventArgs e)
        {
            var overlay = new Controls.AdminRequestOverlay("Chế độ Giáo viên");
            if (overlay.ShowDialog() == true)
            {
                var app = (QASmartTouch.App)Application.Current;
                app.UserRoleService.SaveRole(QASmartClass.Shared.UserRole.Teacher);
                app.ShowClassroom();
                Log.Information("[ADMIN_ACTION] SwitchMode to Teacher");
                this.Close();
            }
            else
            {
                MessageBox.Show("Người dùng hiện tại đã từ chối hoặc yêu cầu quá hạn.", "Bị từ chối", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ForceStudent_Click(object sender, RoutedEventArgs e)
        {
            var overlay = new Controls.AdminRequestOverlay("Chế độ Học sinh");
            if (overlay.ShowDialog() == true)
            {
                var app = (QASmartTouch.App)Application.Current;
                app.UserRoleService.SaveRole(QASmartClass.Shared.UserRole.Student);
                app.ShowStudentClient();
                Log.Information("[ADMIN_ACTION] SwitchMode to Student");
                this.Close();
            }
            else
            {
                MessageBox.Show("Người dùng hiện tại đã từ chối hoặc yêu cầu quá hạn.", "Bị từ chối", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ForceWhiteboard_Click(object sender, RoutedEventArgs e)
        {
            var overlay = new Controls.AdminRequestOverlay("Smart Touch (Bảng trắng)");
            if (overlay.ShowDialog() == true)
            {
                var app = (QASmartTouch.App)Application.Current;
                app.UserRoleService.SaveRole(QASmartClass.Shared.UserRole.SmartTouchOnly);
                app.ShowWhiteboard();
                Log.Information("[ADMIN_ACTION] SwitchMode to Smart Touch");
                this.Close();
            }
            else
            {
                MessageBox.Show("Người dùng hiện tại đã từ chối hoặc yêu cầu quá hạn.", "Bị từ chối", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ═══════════════════════════════════════════════════════
        //  STORAGE EXPLORER
        // ═══════════════════════════════════════════════════════

        private void OpenDbFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(_baseDir))
                    Process.Start("explorer.exe", _baseDir);
                else
                    MessageBox.Show("Thư mục chưa được tạo.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        private void OpenLogsFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logDir = Path.Combine(_baseDir, "Logs");
                if (Directory.Exists(logDir))
                    Process.Start("explorer.exe", logDir);
                else
                    MessageBox.Show("Chưa có logs nào được tạo.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        private void OpenStudentProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                if (File.Exists(profilePath))
                {
                    Process.Start("notepad.exe", profilePath);
                }
                else
                {
                    string fileName = Path.GetFileName(profilePath);
                    MessageBox.Show($"File cấu hình học sinh '{fileName}' chưa được khởi tạo (Thiết bị này chưa có học sinh nào đăng nhập).", 
                                    "Không tìm thấy cấu hình", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) 
            { 
                MessageBox.Show($"Lỗi khi mở file cấu hình học sinh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); 
            }
        }

        private void FactoryReset_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "CẢNH BÁO NGUY HIỂM:\n\nThao tác này sẽ xóa toàn bộ Database, Cấu hình và Logs. Phần mềm sẽ mất hoàn toàn dữ liệu lớp học.\n\nBạn có chắc chắn muốn xóa không?",
                "Factory Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var pinDialog = new PinDialog();
                if (pinDialog.ShowDialog() != true)
                {
                    Log.Information("Factory reset aborted by user (PIN cancelled or failed).");
                    return;
                }

                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    app.Database?.Dispose();
                    
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    System.Threading.Thread.Sleep(500);
                    
                    var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
                    
                    bool success = false;
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            if (File.Exists(dbPath))
                            {
                                var backupName = $"smartclass_BEFORERESET_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                                var backupPath = Path.Combine(QASmartClass.Services.AppPaths.BackupsDir, backupName);
                                File.Copy(dbPath, backupPath, true);
                                Log.Information("[ADMIN_ACTION] Auto-backup created at {BackupPath} before Factory Reset.", backupPath);
                                
                                File.Delete(dbPath);
                            }
                            success = true;
                            break;
                        }
                        catch (IOException)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            System.Threading.Thread.Sleep(500);
                        }
                    }

                    if (!success)
                    {
                        if (File.Exists(dbPath))
                        {
                            File.Delete(dbPath);
                        }
                    }

                    Log.Information("[ADMIN_ACTION] FactoryReset completed.");
                    MessageBox.Show("Đã khôi phục cài đặt gốc thành công. Vui lòng khởi động lại phần mềm.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể xóa Database (Có thể file đang bị khóa bởi tiến trình khác).\n\nChi tiết lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Luôn bắt buộc xác thực mã PIN trước khi phục hồi CSDL theo quy chuẩn an ninh v4.1 (TC-03)
                var pinDialog = new PinDialog();
                if (pinDialog.ShowDialog() != true)
                {
                    Log.Information("Restore backup aborted: PIN verification failed.");
                    return;
                }

                string backupDir = QASmartClass.Services.AppPaths.BackupsDir;
                using (var db = new AppDbContext())
                {
                    var backupSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "BackupFolderPath");
                    if (backupSetting != null && !string.IsNullOrEmpty(backupSetting.Value))
                    {
                        backupDir = backupSetting.Value;
                    }
                }

                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Chọn file Backup để khôi phục",
                    Filter = "Database Files (*.db)|*.db|All Files (*.*)|*.*",
                    InitialDirectory = backupDir
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string selectedFile = openFileDialog.FileName;
                    
                    var result = MessageBox.Show(
                        $"Bạn có chắc chắn muốn khôi phục từ file backup này không?\n\n{Path.GetFileName(selectedFile)}\n\nDữ liệu hiện tại sẽ bị ghi đè hoàn toàn!",
                        "Xác nhận Khôi phục", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        var app = (QASmartTouch.App)Application.Current;
                        var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;

                        app.Database?.Dispose(); // Ngắt kết nối DB hiện tại để tránh lock file
                        
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        System.Threading.Thread.Sleep(500);

                        if (File.Exists(dbPath))
                        {
                            var autoBackupName = $"smartclass_AUTOBACKUP_BEFORE_RESTORE_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                            var autoBackupPath = Path.Combine(backupDir, autoBackupName);
                            
                            bool emergencyBackupSuccess = false;
                            for (int i = 0; i < 3; i++)
                            {
                                try
                                {
                                    File.Copy(dbPath, autoBackupPath, true);
                                    emergencyBackupSuccess = true;
                                    break;
                                }
                                catch (IOException)
                                {
                                    GC.Collect();
                                    GC.WaitForPendingFinalizers();
                                    System.Threading.Thread.Sleep(500);
                                }
                            }
                            if (!emergencyBackupSuccess)
                            {
                                Log.Warning("Failed to create emergency backup after 3 retries.");
                            }
                        }

                        bool success = false;
                        for (int i = 0; i < 3; i++)
                        {
                            try
                            {
                                File.Copy(selectedFile, dbPath, true);
                                success = true;
                                break;
                            }
                            catch (IOException)
                            {
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                System.Threading.Thread.Sleep(500);
                            }
                        }

                        if (!success)
                        {
                            File.Copy(selectedFile, dbPath, true);
                        }
                        
                        Log.Information("[ADMIN_ACTION] RestoreBackup completed from {BackupFile}", selectedFile);
                        MessageBox.Show("Đã khôi phục dữ liệu thành công. Phần mềm sẽ thoát, vui lòng khởi động lại.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        Application.Current.Shutdown();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khôi phục Backup: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════
        //  LOGS & DIAGNOSTICS
        // ═══════════════════════════════════════════════════════

        private void RefreshLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logDir = Path.Combine(_baseDir, "Logs");
                if (!Directory.Exists(logDir))
                {
                    txtLogs.Text = "Không tìm thấy thư mục Logs.";
                    return;
                }

                var files = Directory.GetFiles(logDir, "*.txt").OrderByDescending(f => f).ToList();
                if (files.Any())
                {
                    var latestLog = files.First();
                    // Read file safely if it's locked by Serilog
                    using (var fs = new FileStream(latestLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                    {
                        var allText = sr.ReadToEnd();
                        var lines = allText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                        
                        // Lấy 100 dòng cuối
                        var last100 = lines.Skip(Math.Max(0, lines.Length - 100)).ToArray();
                        txtLogs.Text = string.Join(Environment.NewLine, last100);
                        txtLogs.ScrollToEnd();
                    }
                }
                else
                {
                    txtLogs.Text = "Chưa có file log nào.";
                }
            }
            catch (Exception ex)
            {
                txtLogs.Text = $"Lỗi khi đọc log: {ex.Message}";
            }
        }

        private async void PingTeacher_Click(object sender, RoutedEventArgs e)
        {
            var app = (QASmartTouch.App)Application.Current;
            var ip = app.NetworkService?.ServerIP;
            if (string.IsNullOrEmpty(ip)) ip = app.StudentNetwork?.ServerIP;

            if (string.IsNullOrEmpty(ip))
            {
                MessageBox.Show("Không tìm thấy IP của máy Giáo viên trong cấu hình mạng hiện tại.", "Ping Thất Bại", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                txtLogs.Text += $"\n\n[DIAGNOSTICS] Pinging {ip}...\n";
                var ping = new Ping();
                var reply = await ping.SendPingAsync(ip, 3000); // timeout 3s

                if (reply.Status == IPStatus.Success)
                {
                    txtLogs.Text += $"[DIAGNOSTICS] SUCCESS: {reply.RoundtripTime}ms\n";
                }
                else
                {
                    txtLogs.Text += $"[DIAGNOSTICS] FAILED: {reply.Status}\n";
                }
                txtLogs.ScrollToEnd();
            }
            catch (Exception ex)
            {
                txtLogs.Text += $"[DIAGNOSTICS] ERROR: {ex.Message}\n";
            }
        }
        // ═══════════════════════════════════════════════════════
        //  TELEMETRY STATS (Phase 5)
        // ═══════════════════════════════════════════════════════

        private void RefreshStats_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new QASmartClass.Data.AppDbContext();
                var logs = db.UsageLogs.ToList();

                // Total sessions
                int sessions = logs.Count(l => l.EventType == "SESSION_START");
                txtTotalSessions.Text = sessions.ToString();

                // Total hours (sum of SESSION_END durations)
                long totalMs = logs.Where(l => l.EventType == "SESSION_END").Sum(l => l.DurationMs);
                double totalHours = totalMs / 3600000.0;
                txtTotalHours.Text = $"{totalHours:F1}h";

                // Total events
                txtTotalEvents.Text = logs.Count.ToString();

                // Top tools
                var topTools = logs
                    .Where(l => l.EventType == "TOOL_OPENED")
                    .GroupBy(l => l.EventData)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select((g, i) => $"{i + 1}. {g.Key} — {g.Count()} lần")
                    .ToList();

                txtTopTools.Text = topTools.Count > 0
                    ? string.Join("\n", topTools)
                    : "Chưa có dữ liệu tool nào được ghi nhận.";

                // Recent events (last 20)
                var recent = logs
                    .OrderByDescending(l => l.Timestamp)
                    .Take(20)
                    .Select(l => $"[{l.Timestamp:HH:mm:ss}] {l.EventType}: {l.EventData}")
                    .ToList();

                txtRecentEvents.Text = recent.Count > 0
                    ? string.Join("\n", recent)
                    : "Chưa có sự kiện nào.";

                Log.Information("[ADMIN_ACTION] Telemetry stats refreshed. Sessions={Sessions}, Events={Events}", sessions, logs.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu thống kê: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
