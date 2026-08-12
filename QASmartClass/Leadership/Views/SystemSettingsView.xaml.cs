using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Leadership.Views
{
    public partial class SystemSettingsView : Page
    {
        private readonly AppDbContext? _db;

        public SystemSettingsView()
        {
            InitializeComponent();
            _db = ((QASmartTouch.App)Application.Current).Database;
            Loaded += (_, __) => LoadSettingsFromDb();
        }

        private void SelectComboByTag(ComboBox cb, string tagValue)
        {
            if (cb == null || string.IsNullOrEmpty(tagValue)) return;
            foreach (ComboBoxItem item in cb.Items)
            {
                if (item.Tag?.ToString() == tagValue)
                {
                    cb.SelectedItem = item;
                    break;
                }
            }
        }

        private string GetSelectedTag(ComboBox cb, string defaultVal)
        {
            return (cb?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? defaultVal;
        }

        /// <summary>S2-02: Đọc settings từ DB vào UI khi load</summary>
        private void LoadSettingsFromDb()
        {
            if (_db == null) return;

            try
            {
                // Đọc từng setting key từ DB
                var settings = _db.SystemSettings.ToList();
                foreach (var s in settings)
                {
                    switch (s.Id)
                    {
                        case "SchoolName":
                            if (FindName("txtSchoolName") is TextBox txName) txName.Text = s.Value;
                            break;
                        case "AcademicYear":
                            if (FindName("txtAcademicYear") is TextBox txYear) txYear.Text = s.Value;
                            break;
                        case "SchoolId":
                            if (FindName("txtSchoolId") is TextBox txSId) txSId.Text = s.Value;
                            break;
                        case "IP_Address":
                            if (FindName("txtIPAddress") is TextBox txIP) txIP.Text = s.Value;
                            break;
                        case "Port":
                            if (FindName("txtPort") is TextBox txPort) txPort.Text = s.Value;
                            break;
                        case "AutoBackupEnabled":
                            if (FindName("chkAutoBackup") is CheckBox cb) cb.IsChecked = s.Value == "true";
                            break;
                        case "ParentPortal_AuthSecureLevel":
                            SelectComboByTag(cmbPPAuthLevel, s.Value);
                            break;
                        case "ParentPortal_MessageRouting":
                            SelectComboByTag(cmbPPRouting, s.Value);
                            break;
                        case "ParentPortal_AttendanceCase":
                            SelectComboByTag(cmbPPAttendance, s.Value);
                            break;
                        case "IT_Security_PwdHashLevel":
                            SelectComboByTag(cmbPwdHashLevel, s.Value);
                            break;
                        case "IT_Database_OverLimitAction":
                            SelectComboByTag(cmbOverLimitAction, s.Value);
                            break;
                        case "IT_Backup_StorageTarget":
                            SelectComboByTag(cmbStorageTarget, s.Value);
                            break;
                        case "PrimaryColor":
                            if (FindName("txtThemeHex") is TextBox txHex) txHex.Text = s.Value;
                            break;
                        case "BackupTime":
                            if (FindName("txtBackupTime") is TextBox txBTime) txBTime.Text = s.Value;
                            break;
                        case "DbSizeLimit":
                            if (FindName("txtDbSizeLimit") is TextBox txSizeLimit) txSizeLimit.Text = s.Value;
                            break;
                        case "IT_Database_GrowthForecastMode":
                            SelectComboByTag(cmbGrowthForecastMode, s.Value);
                            break;
                        case "IT_Database_ArchiveMode":
                            SelectComboByTag(cmbArchiveMode, s.Value);
                            break;
                        case "IT_Network_ScanIntervalSeconds":
                            SelectComboByTag(cmbScanInterval, s.Value);
                            break;
                        case "IT_Backup_RecoverySandboxMode":
                            SelectComboByTag(cmbSandboxMode, s.Value);
                            break;
                        case "IT_System_DeploymentModel":
                            SelectComboByTag(cmbDeploymentModel, s.Value);
                            break;
                        case "IT_Database_WALMode":
                            SelectComboByTag(cmbWALMode, s.Value);
                            break;
                        case "Security_SingleInstanceMode":
                            SelectComboByTag(cmbSingleInstanceMode, s.Value);
                            break;
                        case "Network_KeepAliveTimeout":
                            SelectComboByTag(cmbNetworkKeepAliveTimeout, s.Value);
                            break;
                        case "Network_ScreenCompressionFormat":
                            SelectComboByTag(cmbNetworkCompressionFormat, s.Value);
                            break;
                        case "Kiosk_StreamFallbackMode":
                            SelectComboByTag(cmbStreamFallbackMode, s.Value);
                            break;

                        case "IT_Sync_Interval":
                            SelectComboByTag(cmbSyncInterval, s.Value);
                            break;
                        case "IT_UI_GraphicsLevel":
                            SelectComboByTag(cmbGraphicsLevel, s.Value);
                            break;
                        case "IT_Mobile_AutoApprove":
                            SelectComboByTag(cmbMobileAutoApprove, s.Value);
                            break;
                        case "Asset_AutoCancelBookingsOnRepair":
                            SelectComboByTag(cmbAutoCancelBookings, s.Value);
                            break;
                        case "IT_Document_DefaultSLADays":
                            SelectComboByTag(cmbDefaultSLADays, s.Value);
                            break;
                        case "IT_Incident_AutoNotifyParents":
                            SelectComboByTag(cmbIncidentAutoNotify, s.Value);
                            break;
                        case "IT_Audit_IntegrityCheckMode":
                            SelectComboByTag(cmbAuditIntegrityMode, s.Value);
                            break;
                        case "IT_Location_TrackingTechnology":
                            SelectComboByTag(cmbTrackingTech, s.Value);
                            break;
                        case "IT_Gate_OfflineFallbackMode":
                            SelectComboByTag(cmbOfflineFallback, s.Value);
                            break;
                        case "IT_Gate_AntiPassbackMode":
                            SelectComboByTag(cmbAntiPassbackMode, s.Value);
                            break;
                        case "IT_Gate_AntiPassbackAction":
                            SelectComboByTag(cmbAntiPassbackAction, s.Value);
                            break;
                        case "IT_Gate_FaceMatchRequirement":
                            SelectComboByTag(cmbFaceMatchRequirement, s.Value);
                            break;

                        // Các khóa cấu hình mới
                        case "LessonApproval_Required":
                            chkRequireLessonApproval.IsChecked = s.Value == "true";
                            break;
                        case "LessonApproval_ApproverRole":
                            SelectComboByTag(cmbLessonApprovalRole, s.Value);
                            break;
                        case "Timetable_Periods_Config":
                            try
                            {
                                using (var doc = System.Text.Json.JsonDocument.Parse(s.Value))
                                {
                                    foreach (var item in doc.RootElement.EnumerateArray())
                                    {
                                        int period = item.GetProperty("Period").GetInt32();
                                        string start = item.GetProperty("Start").GetString() ?? "";
                                        string end = item.GetProperty("End").GetString() ?? "";
                                        switch (period)
                                        {
                                            case 1: txtP1Start.Text = start; txtP1End.Text = end; break;
                                            case 2: txtP2Start.Text = start; txtP2End.Text = end; break;
                                            case 3: txtP3Start.Text = start; txtP3End.Text = end; break;
                                            case 4: txtP4Start.Text = start; txtP4End.Text = end; break;
                                            case 5: txtP5Start.Text = start; txtP5End.Text = end; break;
                                            case 6: txtP6Start.Text = start; txtP6End.Text = end; break;
                                            case 7: txtP7Start.Text = start; txtP7End.Text = end; break;
                                            case 8: txtP8Start.Text = start; txtP8End.Text = end; break;
                                            case 9: txtP9Start.Text = start; txtP9End.Text = end; break;
                                            case 10: txtP10Start.Text = start; txtP10End.Text = end; break;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Warning("Lỗi đọc cấu hình thời khóa biểu: {Err}", ex.Message);
                            }
                            break;
                    }
                }
                ApplyThemeFromDb();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi tải cấu hình ban đầu từ cơ sở dữ liệu");
            }
        }

        private void ThemeColor_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string hex)
            {
                txtThemeHex.Text = hex;
            }
        }

        /// <summary>S2-02: Lưu settings vào DB thay vì chỉ hiện MessageBox</summary>
        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null)
            {
                MessageBox.Show("Không thể kết nối CSDL!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string ip = (FindName("txtIPAddress") as TextBox)?.Text ?? "";
                string port = (FindName("txtPort") as TextBox)?.Text ?? "";
                string hexColor = (FindName("txtThemeHex") as TextBox)?.Text ?? "";
                string bTime = (FindName("txtBackupTime") as TextBox)?.Text ?? "";
                string sizeLimit = (FindName("txtDbSizeLimit") as TextBox)?.Text ?? "";

                // 1. Validate IP Address
                if (!string.IsNullOrEmpty(ip))
                {
                    var parts = ip.Split('.');
                    if (parts.Length != 4 || parts.Any(p => !int.TryParse(p, out int val) || val < 0 || val > 255))
                    {
                        MessageBox.Show("Địa chỉ IP không đúng định dạng IPv4!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // 2. Validate Port
                if (!int.TryParse(port, out int portVal) || portVal < 1024 || portVal > 65535)
                {
                    MessageBox.Show("Cổng Port phải là số nguyên trong khoảng 1024 - 65535!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 3. Validate Theme Hex Color
                if (!System.Text.RegularExpressions.Regex.IsMatch(hexColor, "^#[0-9A-Fa-f]{6}$"))
                {
                    MessageBox.Show("Mã màu Hex phải đúng định dạng, ví dụ: #1976D2", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 3b. Validate Color Contrast / Luminance (v4.1 pedagogical requirement)
                try
                {
                    var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor);
                    double r = color.R / 255.0;
                    double g = color.G / 255.0;
                    double b = color.B / 255.0;
                    double l = 0.2126 * r + 0.7152 * g + 0.0722 * b;
                    if (l > 0.8) // Too bright / near white color
                    {
                        MessageBox.Show("Màu chủ đạo được chọn quá sáng, có thể gây mỏi mắt hoặc làm giảm độ tương phản của chữ màu trắng. Vui lòng chọn màu đậm hơn!", "Cảnh báo sư phạm (Contrast)", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                catch
                {
                    MessageBox.Show("Mã màu Hex không hợp lệ!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 4. Validate Backup Time
                if (!System.Text.RegularExpressions.Regex.IsMatch(bTime, "^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$"))
                {
                    MessageBox.Show("Giờ sao lưu tự động phải có định dạng HH:mm, ví dụ: 23:00", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 5. Validate Database Size Limit
                if (!int.TryParse(sizeLimit, out int limitVal) || limitVal < 10)
                {
                    MessageBox.Show("Giới hạn dung lượng CSDL phải là số nguyên lớn hơn hoặc bằng 10 MB!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 6. Validate Timetable formatting and logic (v4.1 pedagogical validation)
                var periods = new[]
                {
                    new { Name = "Tiết 1", Start = txtP1Start.Text.Trim(), End = txtP1End.Text.Trim() },
                    new { Name = "Tiết 2", Start = txtP2Start.Text.Trim(), End = txtP2End.Text.Trim() },
                    new { Name = "Tiết 3", Start = txtP3Start.Text.Trim(), End = txtP3End.Text.Trim() },
                    new { Name = "Tiết 4", Start = txtP4Start.Text.Trim(), End = txtP4End.Text.Trim() },
                    new { Name = "Tiết 5", Start = txtP5Start.Text.Trim(), End = txtP5End.Text.Trim() },
                    new { Name = "Tiết 6", Start = txtP6Start.Text.Trim(), End = txtP6End.Text.Trim() },
                    new { Name = "Tiết 7", Start = txtP7Start.Text.Trim(), End = txtP7End.Text.Trim() },
                    new { Name = "Tiết 8", Start = txtP8Start.Text.Trim(), End = txtP8End.Text.Trim() },
                    new { Name = "Tiết 9", Start = txtP9Start.Text.Trim(), End = txtP9End.Text.Trim() },
                    new { Name = "Tiết 10", Start = txtP10Start.Text.Trim(), End = txtP10End.Text.Trim() }
                };

                var timeRegex = new System.Text.RegularExpressions.Regex("^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$");
                TimeSpan lastEndTime = TimeSpan.Zero;

                for (int i = 0; i < periods.Length; i++)
                {
                    var p = periods[i];
                    if (!timeRegex.IsMatch(p.Start) || !timeRegex.IsMatch(p.End))
                    {
                        MessageBox.Show($"Thời gian {p.Name} không hợp lệ! Vui lòng nhập đúng định dạng HH:mm (00:00 - 23:59).", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var start = TimeSpan.Parse(p.Start);
                    var end = TimeSpan.Parse(p.End);

                    if (end <= start)
                    {
                        MessageBox.Show($"Thời gian kết thúc của {p.Name} phải sau thời gian bắt đầu!", "Lỗi logic thời gian", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (i > 0)
                    {
                        if (start < lastEndTime)
                        {
                            MessageBox.Show($"Thời gian bắt đầu của {p.Name} ({p.Start}) không thể trước thời gian kết thúc của {periods[i-1].Name} ({periods[i-1].End})!", "Lỗi logic thời gian", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    lastEndTime = end;
                }

                // Thực hiện lưu các thiết lập
                SaveSetting("SchoolName", (FindName("txtSchoolName") as TextBox)?.Text ?? "", "General");
                SaveSetting("AcademicYear", (FindName("txtAcademicYear") as TextBox)?.Text ?? "", "General");
                SaveSetting("SchoolId", (FindName("txtSchoolId") as TextBox)?.Text ?? "", "General");
                SaveSetting("IP_Address", ip, "Network");
                SaveSetting("Port", port, "Network");
                SaveSetting("AutoBackupEnabled", (FindName("chkAutoBackup") as CheckBox)?.IsChecked == true ? "true" : "false", "Backup");

                SaveSetting("ParentPortal_AuthSecureLevel", GetSelectedTag(cmbPPAuthLevel, "Simple"), "ParentPortal");
                SaveSetting("ParentPortal_MessageRouting", GetSelectedTag(cmbPPRouting, "Homeroom"), "ParentPortal");
                SaveSetting("ParentPortal_AttendanceCase", GetSelectedTag(cmbPPAttendance, "CaseInsensitive"), "ParentPortal");

                SaveSetting("IT_Security_PwdHashLevel", GetSelectedTag(cmbPwdHashLevel, "PBKDF2"), "Security");
                SaveSetting("IT_Database_OverLimitAction", GetSelectedTag(cmbOverLimitAction, "WarnOnly"), "Database");
                SaveSetting("IT_Backup_StorageTarget", GetSelectedTag(cmbStorageTarget, "LocalOnly"), "Backup");

                SaveSetting("PrimaryColor", hexColor, "Theme");
                SaveSetting("BackupTime", bTime, "Backup");
                SaveSetting("DbSizeLimit", sizeLimit, "Database");

                // Lưu các tùy chọn Master
                SaveSetting("IT_Database_GrowthForecastMode", GetSelectedTag(cmbGrowthForecastMode, "Linear"), "Database");
                SaveSetting("IT_Database_ArchiveMode", GetSelectedTag(cmbArchiveMode, "AutoArchive"), "Database");
                SaveSetting("IT_Network_ScanIntervalSeconds", GetSelectedTag(cmbScanInterval, "30"), "Network");
                SaveSetting("IT_Backup_RecoverySandboxMode", GetSelectedTag(cmbSandboxMode, "Strict"), "Backup");

                SaveSetting("IT_System_DeploymentModel", GetSelectedTag(cmbDeploymentModel, "Model_B_Standard"), "IT");
                SaveSetting("IT_Database_WALMode", GetSelectedTag(cmbWALMode, "Enabled"), "IT");
                SaveSetting("Security_SingleInstanceMode", GetSelectedTag(cmbSingleInstanceMode, "KillLingering"), "IT");
                SaveSetting("Network_KeepAliveTimeout", GetSelectedTag(cmbNetworkKeepAliveTimeout, "30"), "IT");
                SaveSetting("Network_ScreenCompressionFormat", GetSelectedTag(cmbNetworkCompressionFormat, "WebP"), "IT");
                SaveSetting("Kiosk_StreamFallbackMode", GetSelectedTag(cmbStreamFallbackMode, "AutoFallback"), "KIOSK");
                SaveSetting("IT_Sync_Interval", GetSelectedTag(cmbSyncInterval, "5"), "IT");
                SaveSetting("IT_UI_GraphicsLevel", GetSelectedTag(cmbGraphicsLevel, "DynamicSVG"), "IT");
                SaveSetting("IT_Mobile_AutoApprove", GetSelectedTag(cmbMobileAutoApprove, "Enabled"), "IT");
                SaveSetting("Asset_AutoCancelBookingsOnRepair", GetSelectedTag(cmbAutoCancelBookings, "1"), "Database");
                SaveSetting("IT_Document_DefaultSLADays", GetSelectedTag(cmbDefaultSLADays, "3"), "IT");
                SaveSetting("IT_Incident_AutoNotifyParents", GetSelectedTag(cmbIncidentAutoNotify, "Enabled"), "IT");
                SaveSetting("IT_Audit_IntegrityCheckMode", GetSelectedTag(cmbAuditIntegrityMode, "Strict"), "IT");

                SaveSetting("IT_Location_TrackingTechnology", GetSelectedTag(cmbTrackingTech, "2"), "IT");
                SaveSetting("IT_Gate_OfflineFallbackMode", GetSelectedTag(cmbOfflineFallback, "1"), "IT");
                SaveSetting("IT_Gate_AntiPassbackMode", GetSelectedTag(cmbAntiPassbackMode, "0"), "IT");
                SaveSetting("IT_Gate_AntiPassbackAction", GetSelectedTag(cmbAntiPassbackAction, "2"), "IT");
                SaveSetting("IT_Gate_FaceMatchRequirement", GetSelectedTag(cmbFaceMatchRequirement, "2"), "IT");

                // Lưu các thiết lập mới của Admin Trường
                SaveSetting("LessonApproval_Required", chkRequireLessonApproval.IsChecked == true ? "true" : "false", "LessonApproval");
                SaveSetting("LessonApproval_ApproverRole", GetSelectedTag(cmbLessonApprovalRole, "SchoolAdmin"), "LessonApproval");

                var timetableJson = System.Text.Json.JsonSerializer.Serialize(periods.Select((p, idx) => new
                {
                    Period = idx + 1,
                    Start = p.Start,
                    End = p.End
                }));
                SaveSetting("Timetable_Periods_Config", timetableJson, "Timetable");

                _db.SaveChanges();

                // Áp dụng màu chủ đạo trực tiếp
                ApplyThemeFromDb();

                // Lưu AuditLog
                _db.AuditLogs.Add(new AuditLog
                {
                    Action = "SystemSettings_Save",
                    ActorName = Environment.MachineName,
                    Details = $"Cập nhật cấu hình hệ thống: PPAuth={GetSelectedTag(cmbPPAuthLevel, "Simple")}, Theme={hexColor}, LessonApproval={chkRequireLessonApproval.IsChecked}",
                    Timestamp = DateTime.Now
                });
                _db.SaveChanges();

                MessageBox.Show("Đã lưu cấu hình hệ thống vào cơ sở dữ liệu thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu settings: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSetting(string key, string value, string category)
        {
            var existing = _db!.SystemSettings.Find(key);
            if (existing != null)
            {
                existing.Value = value;
                existing.LastUpdated = DateTime.Now;
            }
            else
            {
                _db.SystemSettings.Add(new SystemSetting
                {
                    Id = key,
                    Value = value,
                    Category = category,
                    LastUpdated = DateTime.Now
                });
            }
        }

        /// <summary>S2-03: Apply theme color from DB to Application Resources</summary>
        private void ApplyThemeFromDb()
        {
            try
            {
                var themeSetting = _db?.SystemSettings.Find("PrimaryColor");
                if (themeSetting != null && !string.IsNullOrEmpty(themeSetting.Value))
                {
                    var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(themeSetting.Value);
                    Application.Current.Resources["PrimaryColor"] = new System.Windows.Media.SolidColorBrush(color);
                    Application.Current.Resources["PrimaryColorHex"] = themeSetting.Value;
                }
            }
            catch { /* Invalid color — skip */ }
        }

        private void BtnCleanData_Click(object sender, RoutedEventArgs e)
        {
            if (_db != null)
            {
                var service = new QASmartClass.Services.DataRetentionService(_db);
                service.CleanOldData(90, 30);
                MessageBox.Show("Tiến trình dọn dẹp dữ liệu (Data Retention) đã chạy. Đã xóa các file log cũ để giải phóng không gian.", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnArchiveLogs_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            var archiveService = new QASmartClass.Services.DbArchiveService(_db);
            bool success = archiveService.ArchiveOldLogs(90);
            if (success)
            {
                MessageBox.Show("Tiến trình nén và lưu trữ nhật ký (Archive) đã hoàn tất thành công. CSDL đã được tối ưu giải phóng dung lượng đĩa.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Có lỗi xảy ra trong quá trình nén lưu trữ dữ liệu!", "Thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSelectBackup_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            var openFileDialog = new OpenFileDialog
            {
                Filter = "SQLite Databases (*.db)|*.db|All Files (*.*)|*.*",
                Title = "Chọn tệp tin sao lưu cơ sở dữ liệu"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                txtBackupFilePath.Text = filePath;

                // Run Sandbox integrity check
                var archiveService = new QASmartClass.Services.DbArchiveService(_db);
                bool isValid = archiveService.VerifyBackupSandbox(filePath);
                if (isValid)
                {
                    txtSandboxStatus.Text = "Trạng thái Sandbox: Hợp lệ (An toàn để khôi phục)";
                    txtSandboxStatus.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    txtSandboxStatus.Text = "Trạng thái Sandbox: LỖI CẤU TRÚC / HỎNG FILE!";
                    txtSandboxStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
        }

        private void BtnRestoreSandbox_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;

            string backupPath = txtBackupFilePath.Text;
            if (string.IsNullOrEmpty(backupPath) || !File.Exists(backupPath))
            {
                MessageBox.Show("Vui lòng chọn tệp tin sao lưu hợp lệ trước!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var archiveService = new QASmartClass.Services.DbArchiveService(_db);
            
            // Re-verify strictly if required
            bool sandboxStrict = GetSelectedTag(cmbSandboxMode, "Strict") == "Strict";
            if (sandboxStrict)
            {
                bool isValid = archiveService.VerifyBackupSandbox(backupPath);
                if (!isValid)
                {
                    MessageBox.Show("Tệp sao lưu không vượt qua được kiểm tra Sandbox nghiêm ngặt! Không thể tiến hành khôi phục.", "Lỗi bảo mật dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Show confirmation dialog with default focus on "HỦY" (No)
            var result = MessageBox.Show(
                "⚠️ CẢNH BÁO NGUY HIỂM ⚠️\n\nBạn có chắc chắn muốn khôi phục cơ sở dữ liệu từ tệp này không?\nToàn bộ dữ liệu hiện tại sẽ bị ghi đè hoàn toàn và hệ thống sẽ đóng ứng dụng ngay lập tức.\n\nNhấn 'Yes' để tiếp tục khôi phục hoặc 'No' để hủy bỏ.",
                "Xác nhận khôi phục CSDL",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No // Default button is NO
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _db?.Database?.GetDbConnection()?.Close();
                    
                    // Clear connection pools and force collection to release file locks on Windows
                    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();

                    // Copy file over safely
                    string dbFile = QASmartClass.Services.AppPaths.DatabaseFile;
                    File.Copy(backupPath, dbFile, overwrite: true);

                    MessageBox.Show(
                        "Khôi phục cơ sở dữ liệu thành công!\nỨng dụng sẽ đóng ngay bây giờ. Vui lòng khởi động lại hệ thống.",
                        "Thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi ghi đè tệp cơ sở dữ liệu: {ex.Message}", "Lỗi khôi phục", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CmbDeploymentModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            var selectedTag = (cmbDeploymentModel.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (selectedTag == "Model_A_Low")
            {
                var result = MessageBox.Show(
                    "Bạn đã chọn Mô hình A (Hạ tầng yếu).\nHệ thống đề xuất tự động cấu hình tối ưu để giảm tải phần cứng:\n- Bật SQLite WAL Mode (giảm nghẽn HDD)\n- Đồng bộ mạng LAN: 15 phút (giảm nghẽn LAN)\n- Đồ họa tối giản Static Emoji (giảm tải RAM < 120MB)\n\nBạn có muốn tự động áp dụng các thiết lập tối ưu này không?",
                    "Đề xuất Thích ứng Hạ tầng (Model A)",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );
                if (result == MessageBoxResult.Yes)
                {
                    SelectComboByTag(cmbWALMode, "Enabled");
                    SelectComboByTag(cmbSyncInterval, "15");
                    SelectComboByTag(cmbGraphicsLevel, "StaticEmoji");
                }
            }
            else if (selectedTag == "Model_C_High")
            {
                var result = MessageBox.Show(
                    "Bạn đã chọn Mô hình C (Hạ tầng hiện đại).\nHệ thống đề xuất tự động cấu hình tối ưu hiệu năng:\n- Bật SQLite WAL Mode (tốc độ cao)\n- Đồng bộ mạng LAN: 1 phút (thời gian thực)\n- Đầy đủ hiệu ứng và ảnh động SVG\n\nBạn có muốn tự động áp dụng các thiết lập tối ưu này không?",
                    "Đề xuất Thích ứng Hạ tầng (Model C)",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );
                if (result == MessageBoxResult.Yes)
                {
                    SelectComboByTag(cmbWALMode, "Enabled");
                    SelectComboByTag(cmbSyncInterval, "1");
                    SelectComboByTag(cmbGraphicsLevel, "DynamicSVG");
                }
            }
        }
    }
}
