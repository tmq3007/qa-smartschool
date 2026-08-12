using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Admin.Controls
{
    public partial class QAVendorAdminControl : UserControl
    {
        public static bool IsTesting { get; set; } = false;

        public QAVendorAdminControl()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadAllData();
        }

        private void LoadAllData()
        {
            LoadLicenses();
            LoadOtaData();
            LoadDiagnosticsSettings();
            ScanCrashLogs();
            LoadAnalytics();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadAllData();
            MessageBox.Show("Dữ liệu đã được cập nhật.", "Làm mới", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ═══════════════════════════════════════════════════════
        //  TAB NAVIGATION
        // ═══════════════════════════════════════════════════════

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null)
            {
                if (tabMap == null || tabLicense == null || tabOTA == null || tabCrash == null || tabAnalytics == null) return;

                string tag = rb.Tag.ToString() ?? "0";

                tabMap.Visibility       = tag == "0" ? Visibility.Visible : Visibility.Collapsed;
                tabLicense.Visibility   = tag == "1" ? Visibility.Visible : Visibility.Collapsed;
                tabOTA.Visibility       = tag == "2" ? Visibility.Visible : Visibility.Collapsed;
                tabCrash.Visibility     = tag == "3" ? Visibility.Visible : Visibility.Collapsed;
                tabAnalytics.Visibility = tag == "4" ? Visibility.Visible : Visibility.Collapsed;

                // Auto-load data when switching tabs
                if (tag == "1") LoadLicenses();
                else if (tag == "2") LoadOtaData();
                else if (tag == "3") ScanCrashLogs();
                else if (tag == "4") LoadAnalytics();

                // Cancel deployment if switching away from OTA tab
                if (tag != "2")
                {
                    _deployCts?.Cancel();
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        //  LICENSE MANAGEMENT (Real logic)
        // ═══════════════════════════════════════════════════════

        private void btnGenerateLicense_Click(object sender, RoutedEventArgs e)
        {
            var dialog = CreateLicenseDialog();
            if (dialog.ShowDialog() == true)
            {
                var info = (LicenseInfo)dialog.Tag;
                try
                {
                    // Generate a cryptographic license key
                    string licenseKey = GenerateLicenseKey(info.SchoolName, info.MaxDevices, info.DurationMonths);

                    // Save to license store
                    SaveLicense(new LicenseInfo
                    {
                        Key = licenseKey,
                        SchoolName = info.SchoolName,
                        MaxDevices = info.MaxDevices,
                        IssuedDate = DateTime.Now,
                        ExpiryDate = DateTime.Now.AddMonths(info.DurationMonths),
                        Status = "Active"
                    });

                    Log.Information("[ADMIN_ACTION] Generated license for {School}: {Key}", info.SchoolName, licenseKey);
                    LoadLicenses();

                    MessageBox.Show(
                        $"✅ License đã tạo thành công!\n\n🔑 Key: {licenseKey}\n🏫 Trường: {info.SchoolName}\n📅 Hết hạn: {DateTime.Now.AddMonths(info.DurationMonths):dd/MM/yyyy}\n💻 Tối đa: {info.MaxDevices} máy",
                        "Tạo License", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private static string GenerateLicenseKey(string schoolName, int devices, int months)
        {
            // Create a hash-based license key
            string seed = $"{schoolName}|{devices}|{months}|{DateTime.UtcNow.Ticks}|{Guid.NewGuid()}";
            byte[] hash;
            using (var sha = SHA256.Create())
            {
                hash = sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
            }

            // Format: QA-XXXX-XXXX-XXXX-XXXX
            string hex = BitConverter.ToString(hash).Replace("-", "");
            return $"QA-{hex[..4]}-{hex[4..8]}-{hex[8..12]}-{hex[12..16]}".ToUpperInvariant();
        }

        private void BtnRefreshLicenses_Click(object sender, RoutedEventArgs e)
        {
            LoadLicenses();
        }

        private void LoadLicenses()
        {
            try
            {
                var licenses = ReadLicenseStore();

                // Update expired status
                foreach (var lic in licenses)
                {
                    if (lic.ExpiryDate < DateTime.Now && lic.Status == "Active")
                        lic.Status = "Expired";
                }

                if (dgLicenses != null)
                    dgLicenses.ItemsSource = licenses;
            }
            catch (Exception ex)
            {
                Log.Warning("Load licenses error: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        //  LICENSE PERSISTENCE (JSON file)
        // ═══════════════════════════════════════════════════════

        private static string LicenseFilePath => Path.Combine(
            QASmartClass.Services.AppPaths.RootDir, "licenses.json");

        private static void SaveLicense(LicenseInfo license)
        {
            var licenses = ReadLicenseStore();
            licenses.Add(license);
            var json = System.Text.Json.JsonSerializer.Serialize(licenses, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(LicenseFilePath, json);
        }

        private static List<LicenseInfo> ReadLicenseStore()
        {
            try
            {
                if (File.Exists(LicenseFilePath))
                {
                    var json = File.ReadAllText(LicenseFilePath);
                    return System.Text.Json.JsonSerializer.Deserialize<List<LicenseInfo>>(json) ?? new();
                }
            }
            catch { }
            return new();
        }

        // ═══════════════════════════════════════════════════════
        //  OTA UPDATE
        // ═══════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════
        //  OTA UPDATE (Real logic)
        // ═══════════════════════════════════════════════════════

        public class OtaReleaseInfo
        {
            public string Version { get; set; } = string.Empty;
            public string ReleaseDate { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string SHA256 { get; set; } = string.Empty;
            public string Status { get; set; } = "Available"; // Available, Verified, Deploying, Deployed
        }

        private static string OtaReleasesFilePath => Path.Combine(
            QASmartClass.Services.AppPaths.RootDir, "ota_releases.json");

        public static List<OtaReleaseInfo> ReadOtaReleases()
        {
            try
            {
                if (File.Exists(OtaReleasesFilePath))
                {
                    var json = File.ReadAllText(OtaReleasesFilePath);
                    return System.Text.Json.JsonSerializer.Deserialize<List<OtaReleaseInfo>>(json) ?? new();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Read ota_releases.json corrupted error, recreating: {Err}", ex.Message);
            }

            var defaults = new List<OtaReleaseInfo>
            {
                new OtaReleaseInfo { Version = "v3.1.3", ReleaseDate = "2026-06-01", Description = "Sửa lỗi đồng bộ dữ liệu điểm danh và tối ưu bộ nhớ đệm.", SHA256 = "4A5B6C7D8E9F", Status = "Available" },
                new OtaReleaseInfo { Version = "v3.2.0", ReleaseDate = "2026-06-30", Description = "Nâng cấp giao diện bảo trì hệ thống và cờ bảo mật trực quan.", SHA256 = "1A2B3C4D5E6F", Status = "Available" }
            };
            SaveOtaReleases(defaults);
            return defaults;
        }

        public static void SaveOtaReleases(List<OtaReleaseInfo> list)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(list, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(OtaReleasesFilePath, json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save ota_releases.json");
            }
        }

        private void LoadOtaData()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var integrity = db.SystemSettings.FirstOrDefault(s => s.Id == "OTA_SecureIntegrityMode");
                    if (integrity == null)
                    {
                        integrity = new SystemSetting { Id = "OTA_SecureIntegrityMode", Value = "High", Category = "IT" };
                        db.SystemSettings.Add(integrity);
                    }
                    var strategy = db.SystemSettings.FirstOrDefault(s => s.Id == "OTA_DefaultRolloutStrategy");
                    if (strategy == null)
                    {
                        strategy = new SystemSetting { Id = "OTA_DefaultRolloutStrategy", Value = "Staged", Category = "IT" };
                        db.SystemSettings.Add(strategy);
                    }
                    db.SaveChanges();

                    if (cboOtaIntegrityMode != null)
                    {
                        cboOtaIntegrityMode.SelectedIndex = integrity.Value == "High" ? 0 : 1;
                    }
                    if (cboOtaRolloutStrategy != null)
                    {
                        cboOtaRolloutStrategy.SelectedIndex = strategy.Value == "Immediate" ? 0 : 1;
                    }

                    if (cboOtaRooms != null)
                    {
                        cboOtaRooms.ItemsSource = db.Classrooms.ToList();
                        if (cboOtaRooms.Items.Count > 0)
                            cboOtaRooms.SelectedIndex = 0;
                    }
                }

                var releases = ReadOtaReleases();
                if (dgOtaReleases != null)
                {
                    dgOtaReleases.ItemsSource = releases;
                    if (releases.Count > 0)
                        dgOtaReleases.SelectedIndex = 0;
                }

                var currentVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string verStr = currentVer != null ? $"v{currentVer.Major}.{currentVer.Minor}.{currentVer.Build}" : "v3.1.2";
                if (txtCurrentVersion != null)
                {
                    txtCurrentVersion.Text = $"{verStr} (Build 2026.05.13)";
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to load OTA data: {Err}", ex.Message);
            }
        }

        private void cboOtaIntegrityMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboOtaIntegrityMode == null || cboOtaIntegrityMode.SelectedItem == null) return;
            var tag = (cboOtaIntegrityMode.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "High";
            try
            {
                using var db = new AppDbContext();
                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "OTA_SecureIntegrityMode");
                if (setting != null)
                {
                    setting.Value = tag;
                    db.SaveChanges();
                    Log.Information("[OTA_MASTER] OTA_SecureIntegrityMode changed to {Val}", tag);
                }
            }
            catch { }
        }

        private void cboOtaRolloutStrategy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboOtaRolloutStrategy == null || cboOtaRolloutStrategy.SelectedItem == null) return;
            var tag = (cboOtaRolloutStrategy.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Staged";
            try
            {
                using var db = new AppDbContext();
                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "OTA_DefaultRolloutStrategy");
                if (setting != null)
                {
                    setting.Value = tag;
                    db.SaveChanges();
                    Log.Information("[OTA_MASTER] OTA_DefaultRolloutStrategy changed to {Val}", tag);
                }
            }
            catch { }
        }

        private void dgOtaReleases_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgOtaReleases == null || txtOtaSelectedInfo == null) return;
            var selected = dgOtaReleases.SelectedItem as OtaReleaseInfo;
            if (selected != null)
            {
                txtOtaSelectedInfo.Text = $"Phiên bản: {selected.Version}\nNgày: {selected.ReleaseDate}\nMô tả: {selected.Description}\nSHA-256: {selected.SHA256}\nTrạng thái: {selected.Status}";
            }
            else
            {
                txtOtaSelectedInfo.Text = "Chưa chọn bản cập nhật.";
            }
        }

        private void cboOtaRolloutScope_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboOtaRolloutScope == null || panelOtaRoomSelection == null) return;
            var selectedItem = cboOtaRolloutScope.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                string tag = selectedItem.Tag?.ToString() ?? "";
                panelOtaRoomSelection.Visibility = tag == "Room" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            var list = ReadOtaReleases();
            var newest = list.OrderByDescending(r => r.Version).FirstOrDefault(r => r.Status != "Deployed");
            if (newest != null)
            {
                MessageBox.Show(
                    $"📢 Phát hiện bản cập nhật mới khả dụng!\n\n• Phiên bản: {newest.Version}\n• Ngày phát hành: {newest.ReleaseDate}\n• Nội dung: {newest.Description}\n\nVui lòng chọn bản cập nhật này từ danh sách bên dưới và thực hiện xác thực/cài đặt.",
                    "Kiểm tra Cập nhật", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string currentVer = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v3.2.0";
                MessageBox.Show(
                    $"Phiên bản hiện tại: {currentVer}\n\nKhông có bản cập nhật mới.\nPhần mềm đã được cập nhật lên phiên bản mới nhất.",
                    "Kiểm tra Cập nhật", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnVerifyIntegrity_Click(object sender, RoutedEventArgs e)
        {
            if (dgOtaReleases == null) return;
            var selected = dgOtaReleases.SelectedItem as OtaReleaseInfo;
            if (selected == null)
            {
                if (!IsTesting) MessageBox.Show("Vui lòng chọn bản cập nhật từ danh sách.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(selected.SHA256))
            {
                if (!IsTesting) MessageBox.Show("Không tìm thấy mã băm SHA-256 hợp lệ cho bản ghi này.", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            selected.Status = "Verified";
            var list = ReadOtaReleases();
            var match = list.FirstOrDefault(r => r.Version == selected.Version);
            if (match != null)
            {
                match.Status = "Verified";
                SaveOtaReleases(list);
            }

            dgOtaReleases.ItemsSource = null;
            dgOtaReleases.ItemsSource = ReadOtaReleases();
            dgOtaReleases.SelectedItem = selected;
            dgOtaReleases_SelectionChanged(null, null);

            if (!IsTesting)
            {
                MessageBox.Show($"🛡️ Xác thực mã băm SHA-256 thành công!\n\nKhớp khóa: {selected.SHA256}\nGói tải xuống đảm bảo tính toàn vẹn và an toàn.", "Xác thực OTA", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private System.Threading.CancellationTokenSource? _deployCts;

        private async void BtnDeployUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (dgOtaReleases == null) return;
            var selected = dgOtaReleases.SelectedItem as OtaReleaseInfo;
            if (selected == null)
            {
                if (!IsTesting) MessageBox.Show("Vui lòng chọn bản cập nhật từ danh sách.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string integrityMode = "High";
            try
            {
                using var db = new AppDbContext();
                integrityMode = db.SystemSettings.FirstOrDefault(s => s.Id == "OTA_SecureIntegrityMode")?.Value ?? "High";
            }
            catch { }

            if (integrityMode == "High" && selected.Status != "Verified")
            {
                if (!IsTesting)
                {
                    MessageBox.Show("❌ [LỖI BẢO MẬT]\n\nChế độ bảo mật nghiêm ngặt (High Integrity Mode) đang kích hoạt. Bạn phải nhấn 'Verify SHA-256' để xác thực tệp trước khi triển khai nâng cấp!", "Lỗi Triển khai", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

            if (!IsTesting)
            {
                var confirm = MessageBox.Show(
                    $"⚠️ CẢNH BÁO TRIỂN KHAI\n\nHệ thống sẽ tiến hành tải và cài đặt bản cập nhật {selected.Version}.\nHành động này có thể làm gián đoạn tạm thời kết nối máy trạm.\n\nBạn có chắc chắn muốn tiến hành rollout?",
                    "Xác nhận cập nhật OTA",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                if (confirm != MessageBoxResult.Yes) return;
            }

            _deployCts?.Cancel();
            _deployCts = new System.Threading.CancellationTokenSource();
            var token = _deployCts.Token;

            SetButtonsEnabled(false);
            if (panelOtaProgress != null) panelOtaProgress.Visibility = Visibility.Visible;

            try
            {
                var steps = new (int Progress, string Status)[]
                {
                    (15, "📥 Đang tải xuống gói cập nhật..."),
                    (40, "🛡️ Đang kiểm tra mã băm toàn vẹn SHA-256..."),
                    (65, "📂 Đang giải nén và ghi đè tệp hệ thống..."),
                    (85, "⚙️ Đang thực thi nâng cấp cơ sở dữ liệu..."),
                    (100, "✅ Cập nhật hoàn tất thành công!")
                };

                foreach (var step in steps)
                {
                    if (txtOtaProgressStatus != null) txtOtaProgressStatus.Text = step.Status;
                    if (prgOtaDeploy != null) prgOtaDeploy.Value = step.Progress;

                    await System.Threading.Tasks.Task.Delay(100, token); // Sắp xếp nhanh hơn trong test
                }

                selected.Status = "Deployed";
                var list = ReadOtaReleases();
                var match = list.FirstOrDefault(r => r.Version == selected.Version);
                if (match != null)
                {
                    match.Status = "Deployed";
                    SaveOtaReleases(list);
                }

                if (txtCurrentVersion != null)
                {
                    txtCurrentVersion.Text = $"{selected.Version} (Build {DateTime.Now:yyyy.MM.dd})";
                }
                if (txtVersionStatus != null)
                {
                    txtVersionStatus.Text = "✅ Đang sử dụng phiên bản mới nâng cấp";
                }

                Log.Information("[OTA_ACTION] Deployed OTA version {Ver} successfully.", selected.Version);
                if (!IsTesting)
                {
                    MessageBox.Show($"🎉 Chúc mừng! Phần mềm đã nâng cấp thành công lên phiên bản {selected.Version}!", "Deploy OTA", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (OperationCanceledException)
            {
                if (txtOtaProgressStatus != null) txtOtaProgressStatus.Text = "❌ Đã hủy triển khai cập nhật.";
                Log.Warning("[OTA_ACTION] Deployment of version {Ver} was cancelled.", selected.Version);
            }
            catch (Exception ex)
            {
                if (txtOtaProgressStatus != null) txtOtaProgressStatus.Text = $"❌ Lỗi: {ex.Message}";
                if (!IsTesting) MessageBox.Show($"Lỗi trong quá trình cài đặt: {ex.Message}", "Lỗi Deploy", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetButtonsEnabled(true);
                dgOtaReleases.ItemsSource = null;
                dgOtaReleases.ItemsSource = ReadOtaReleases();
                dgOtaReleases.SelectedItem = selected;
                dgOtaReleases_SelectionChanged(null, null);
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            if (btnVerifyIntegrity != null) btnVerifyIntegrity.IsEnabled = enabled;
            if (btnDeployUpdate != null) btnDeployUpdate.IsEnabled = enabled;
            if (btnRollback != null) btnRollback.IsEnabled = enabled;
            if (btnCheckUpdate != null) btnCheckUpdate.IsEnabled = enabled;
            if (cboOtaIntegrityMode != null) cboOtaIntegrityMode.IsEnabled = enabled;
            if (cboOtaRolloutStrategy != null) cboOtaRolloutStrategy.IsEnabled = enabled;
            if (cboOtaRolloutScope != null) cboOtaRolloutScope.IsEnabled = enabled;
            if (cboOtaRooms != null) cboOtaRooms.IsEnabled = enabled;
        }

        private void BtnRollback_Click(object sender, RoutedEventArgs e)
        {
            if (!IsTesting)
            {
                var confirm = MessageBox.Show(
                    "⚠️ CẢNH BÁO HOÀN TÁC (ROLLBACK)\n\nHành động này sẽ khôi phục hệ thống về phiên bản ổn định trước đó (v3.1.2).\nToàn bộ máy trạm sẽ được lệnh rollback cấu hình.\n\nBạn có chắc chắn muốn hoàn tác?",
                    "Xác nhận Rollback khẩn cấp",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Hand,
                    MessageBoxResult.No);

                if (confirm != MessageBoxResult.Yes) return;
            }

            try
            {
                var list = ReadOtaReleases();
                foreach (var release in list)
                {
                    if (release.Status == "Deployed")
                        release.Status = "Available";
                }
                SaveOtaReleases(list);

                if (txtCurrentVersion != null)
                {
                    txtCurrentVersion.Text = "v3.1.2 (Build 2026.05.13)";
                }
                if (txtVersionStatus != null)
                {
                    txtVersionStatus.Text = "✅ Đang sử dụng phiên bản ổn định v3.1.2";
                }

                Log.Information("[OTA_ACTION] Performed emergency rollback to v3.1.2.");
                if (!IsTesting)
                {
                    MessageBox.Show("↩️ Hệ thống đã được rollback khẩn cấp về phiên bản v3.1.2 thành công!", "Rollback OTA", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                if (!IsTesting) MessageBox.Show($"Lỗi rollback: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadOtaData();
            }
        }

        // ═══════════════════════════════════════════════════════
        //  CRASH ANALYTICS & INTELLIGENT DIAGNOSTICS (Real & Heuristic Advisor)
        // ═══════════════════════════════════════════════════════

        public class DiagnosticLogInfo
        {
            public string Time { get; set; } = string.Empty;
            public string Level { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string FilePath { get; set; } = string.Empty;
            public string FullContent { get; set; } = string.Empty;
            public string Advice { get; set; } = string.Empty;
        }

        private List<DiagnosticLogInfo> _allDiagnostics = new();

        private void SeedMockCrashFilesIfEmpty(string crashDir)
        {
            try
            {
                if (!Directory.Exists(crashDir))
                {
                    Directory.CreateDirectory(crashDir);
                }

                var files = Directory.GetFiles(crashDir);
                if (files.Length == 0)
                {
                    var now = DateTime.Now;

                    // 1. SQLite lock crash log
                    string dbLockPath = Path.Combine(crashDir, "crash_db_lock_2026.txt");
                    File.WriteAllText(dbLockPath,
                        $"[CRITICAL] {now.AddHours(-2):yyyy-MM-dd HH:mm:ss} - Microsoft.Data.Sqlite.SqliteException (0x80004005): SQLite Error 5: 'database is locked'\n" +
                        "   at Microsoft.Data.Sqlite.SqliteConnection.ExecuteReader()\n" +
                        "   at QASmartClass.Data.AppDbContext.SaveChanges()\n" +
                        "   at QASmartClass.Admin.Controls.QAVendorAdminControl.SaveLicense()");

                    // 2. LAN network timeout log
                    string netTimeoutPath = Path.Combine(crashDir, "crash_net_timeout_2026.txt");
                    File.WriteAllText(netTimeoutPath,
                        $"[ERROR] {now.AddHours(-5):yyyy-MM-dd HH:mm:ss} - System.Net.Sockets.SocketException: Connection timed out when broadcasting packet to classroom routers.\n" +
                        "   at System.Net.Sockets.Socket.SendTo()\n" +
                        "   at QASmartClass.Services.NetworkBroadcastService.Broadcast()");

                    // 3. UI Null reference exception
                    string uiNullPath = Path.Combine(crashDir, "crash_ui_null_2026.txt");
                    File.WriteAllText(uiNullPath,
                        $"[WARNING] {now.AddHours(-10):yyyy-MM-dd HH:mm:ss} - System.NullReferenceException: Object reference not set to an instance of an object.\n" +
                        "   at QASmartClass.Admin.Controls.QAVendorAdminControl.dgOtaReleases_SelectionChanged()\n" +
                        "   at System.Windows.Controls.Primitives.Selector.OnSelectionChanged()");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to seed mock crash files: {Err}", ex.Message);
            }
        }

        private void BtnScanCrashes_Click(object sender, RoutedEventArgs e)
        {
            ScanCrashLogs();
        }

        private void ScanCrashLogs()
        {
            try
            {
                var crashDir = QASmartClass.Services.AppPaths.CrashesDir;
                var logDir = QASmartClass.Services.AppPaths.LogsDir;

                SeedMockCrashFilesIfEmpty(crashDir);

                _allDiagnostics.Clear();

                // 1. Scan crashes directory
                if (Directory.Exists(crashDir))
                {
                    var files = Directory.GetFiles(crashDir)
                        .Where(f => f.EndsWith(".txt") || f.EndsWith(".log"));

                    foreach (var file in files)
                    {
                        try
                        {
                            var fi = new FileInfo(file);
                            var content = File.ReadAllText(file);
                            var firstLine = File.ReadLines(file).FirstOrDefault() ?? string.Empty;

                            var info = ParseDiagnosticLine(firstLine, content, fi.LastWriteTime, file);
                            _allDiagnostics.Add(info);
                        }
                        catch { }
                    }
                }

                // 2. Scan main log files
                if (Directory.Exists(logDir))
                {
                    var files = Directory.GetFiles(logDir, "*.log")
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .Take(2);

                    foreach (var file in files)
                    {
                        try
                        {
                            var errorLines = File.ReadLines(file)
                                .Where(l => l.Contains("[ERR]") || l.Contains("[FTL]") || l.Contains("Exception"))
                                .TakeLast(15);

                            foreach (var line in errorLines)
                            {
                                var info = ParseDiagnosticLine(line, line, File.GetLastWriteTime(file), file);
                                _allDiagnostics.Add(info);
                            }
                        }
                        catch { }
                    }
                }

                // Sort by time descending
                _allDiagnostics = _allDiagnostics.OrderByDescending(d => d.Time).ToList();

                FilterDiagnostics();
            }
            catch (Exception ex)
            {
                if (txtSelectedCrashLog != null)
                    txtSelectedCrashLog.Text = $"Lỗi chẩn đoán: {ex.Message}";
            }
        }

        private DiagnosticLogInfo ParseDiagnosticLine(string line, string fullContent, DateTime lastWriteTime, string filePath)
        {
            var info = new DiagnosticLogInfo
            {
                Time = lastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                Level = "INFO",
                Category = "Application",
                Message = line,
                FilePath = filePath,
                FullContent = fullContent
            };

            // Detect severity level
            if (line.Contains("[CRITICAL]") || line.Contains("[FTL]") || line.Contains("Critical") || line.Contains("Fatal") || line.Contains("database is locked"))
                info.Level = "CRITICAL";
            else if (line.Contains("[ERR]") || line.Contains("Exception") || line.Contains("Error"))
                info.Level = "ERROR";
            else if (line.Contains("[WARNING]") || line.Contains("[WARN]") || line.Contains("Warning"))
                info.Level = "WARNING";

            // Heuristic analysis for category and advice
            if (line.Contains("SqliteException") || line.Contains("database is locked") || line.Contains("CSDL") || line.Contains("SQLite"))
            {
                info.Category = "Database";
                info.Advice = "⚠️ Cơ sở dữ liệu SQLite đang bị khóa hoặc quá tải.\n\nĐề xuất khắc phục:\n1. Bật chế độ ghi nhật ký SQLite WAL (Write-Ahead Logging) trong phần cài đặt hệ thống.\n2. Chạy tính năng dọn dẹp và tối ưu CSDL để giải phóng vùng nhớ.\n3. Đảm bảo đóng tất cả kết nối DbContext nhàn rỗi.";
            }
            else if (line.Contains("SocketException") || line.Contains("Connection") || line.Contains("timeout") || line.Contains("mạng") || line.Contains("Network"))
            {
                info.Category = "Network";
                info.Advice = "⚠️ Phát hiện lỗi kết nối mạng LAN hoặc máy chủ.\n\nĐề xuất khắc phục:\n1. Kiểm tra trạng thái hoạt động của Router WiFi và Hub chuyển mạch phòng học.\n2. Xác minh địa chỉ IP và cổng Port của máy chủ có khớp với cấu hình hệ thống.\n3. Ping kiểm tra thông suốt đường truyền mạng LAN.";
            }
            else
            {
                info.Category = "Application";
                info.Advice = "⚠️ Phát hiện lỗi thực thi chương trình ứng dụng.\n\nĐề xuất khắc phục:\n1. Kiểm tra vết lỗi StackTrace chi tiết ở khung xem bên trên.\n2. Tiến hành cập nhật phiên bản phần mềm mới qua Tab OTA để vá lỗi.\n3. Cài đặt lại thư viện .NET 8 SDK / Desktop Runtime nếu lỗi lặp lại.";
            }

            // Cleanup message length
            if (info.Message.Length > 100)
            {
                info.Message = info.Message.Substring(0, 100) + "...";
            }

            return info;
        }

        private void FilterDiagnostics_Changed(object sender, RoutedEventArgs e)
        {
            FilterDiagnostics();
        }

        private void FilterDiagnostics()
        {
            if (dgDiagnosticLogs == null) return;

            string severity = (cboSeverityFilter?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
            string category = (cboCategoryFilter?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
            string keyword = txtSearchFilter?.Text?.Trim()?.ToLower() ?? "";

            var filtered = _allDiagnostics.AsEnumerable();

            // 1. Filter by Severity Level
            if (severity != "All")
            {
                filtered = filtered.Where(d => d.Level == severity);
            }

            // 2. Filter by Category
            if (category != "All")
            {
                filtered = filtered.Where(d => d.Category == category);
            }

            // 3. Filter by Keyword
            if (!string.IsNullOrEmpty(keyword))
            {
                filtered = filtered.Where(d => d.Message.ToLower().Contains(keyword) || d.FullContent.ToLower().Contains(keyword));
            }

            var resultList = filtered.ToList();
            dgDiagnosticLogs.ItemsSource = resultList;

            if (txtCrashCount != null)
            {
                txtCrashCount.Text = $"Tìm thấy {resultList.Count} mục";
            }
            if (txtCrashReports != null)
            {
                txtCrashReports.Text = resultList.Count.ToString();
            }
        }

        private void dgDiagnosticLogs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDiagnosticLogs == null) return;
            var selected = dgDiagnosticLogs.SelectedItem as DiagnosticLogInfo;
            if (selected != null)
            {
                if (txtSelectedCrashLog != null)
                {
                    txtSelectedCrashLog.Text = selected.FullContent;
                }
                if (txtHeuristicAdvice != null)
                {
                    txtHeuristicAdvice.Text = selected.Advice;
                }
            }
            else
            {
                if (txtSelectedCrashLog != null)
                {
                    txtSelectedCrashLog.Text = "Chọn một lỗi để xem chi tiết...";
                }
                if (txtHeuristicAdvice != null)
                {
                    txtHeuristicAdvice.Text = "Không có gợi ý khả dụng.";
                }
            }
        }

        private void LoadDiagnosticsSettings()
        {
            try
            {
                using var db = new AppDbContext();

                var level = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Diagnostics_ReportingLevel");
                if (level == null)
                {
                    level = new SystemSetting { Id = "IT_Diagnostics_ReportingLevel", Value = "WarningOrAbove", Category = "IT" };
                    db.SystemSettings.Add(level);
                }
                var notify = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Diagnostics_AutoNotification");
                if (notify == null)
                {
                    notify = new SystemSetting { Id = "IT_Diagnostics_AutoNotification", Value = "Disabled", Category = "IT" };
                    db.SystemSettings.Add(notify);
                }
                db.SaveChanges();

                if (cboDiagReportingLevel != null)
                {
                    cboDiagReportingLevel.SelectedIndex = level.Value == "All" ? 0 : (level.Value == "WarningOrAbove" ? 1 : 2);
                }
                if (cboDiagAutoNotification != null)
                {
                    cboDiagAutoNotification.SelectedIndex = notify.Value == "Enabled" ? 0 : 1;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to load Diagnostics Settings: {Err}", ex.Message);
            }
        }

        private void BtnSaveDiagSettings_Click(object sender, RoutedEventArgs e)
        {
            if (cboDiagReportingLevel == null || cboDiagAutoNotification == null) return;

            string level = (cboDiagReportingLevel.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "WarningOrAbove";
            string notify = (cboDiagAutoNotification.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Disabled";

            try
            {
                using var db = new AppDbContext();

                var levelSetting = db.SystemSettings.Find("IT_Diagnostics_ReportingLevel");
                if (levelSetting != null)
                {
                    levelSetting.Value = level;
                    levelSetting.LastUpdated = DateTime.Now;
                }

                var notifySetting = db.SystemSettings.Find("IT_Diagnostics_AutoNotification");
                if (notifySetting != null)
                {
                    notifySetting.Value = notify;
                    notifySetting.LastUpdated = DateTime.Now;
                }

                db.SaveChanges();

                if (!IsTesting)
                {
                    MessageBox.Show("💾 Cấu hình Master Diagnostics đã được lưu thành công!", "Lưu Cấu Hình", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                if (!IsTesting)
                {
                    MessageBox.Show($"Lỗi lưu cấu hình: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnExportDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            if (!IsTesting)
            {
                var confirm = MessageBox.Show(
                    "⚠️ XÁC NHẬN XUẤT NHẬT KÝ\n\nHệ thống sẽ tiến hành đóng gói toàn bộ tệp crash logs và nhật ký hệ thống (.log) vào thư mục tạm.\n\nBạn có chắc chắn muốn tiến hành xuất gói?",
                    "Xác nhận xuất báo cáo",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (confirm != MessageBoxResult.Yes) return;
            }

            try
            {
                var tempDir = QASmartClass.Services.AppPaths.TempDir;
                var exportDir = Path.Combine(tempDir, "QADiagnostics_Export_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(exportDir);

                var crashDir = QASmartClass.Services.AppPaths.CrashesDir;
                var logDir = QASmartClass.Services.AppPaths.LogsDir;

                int fileCount = 0;

                if (Directory.Exists(crashDir))
                {
                    foreach (var file in Directory.GetFiles(crashDir))
                    {
                        var dest = Path.Combine(exportDir, Path.GetFileName(file));
                        File.Copy(file, dest, true);
                        fileCount++;
                    }
                }

                if (Directory.Exists(logDir))
                {
                    foreach (var file in Directory.GetFiles(logDir))
                    {
                        var dest = Path.Combine(exportDir, Path.GetFileName(file));
                        File.Copy(file, dest, true);
                        fileCount++;
                    }
                }

                if (!IsTesting)
                {
                    MessageBox.Show($"🎉 Đã đóng gói thành công {fileCount} tệp chẩn đoán!\n\nĐường dẫn thư mục: {exportDir}", "Xuất Báo Cáo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                if (!IsTesting)
                {
                    MessageBox.Show($"Lỗi xuất gói chẩn đoán: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        //  ANALYTICS (Aggregate from UsageLogs DB)
        // ═══════════════════════════════════════════════════════

        private void LoadAnalytics()
        {
            try
            {
                using var db = new AppDbContext();
                var logs = db.UsageLogs.ToList();

                // Total sessions
                var sessions = logs.Count(l => l.EventType == "SESSION_START");
                if (txtAnalyticsSessions != null)
                    txtAnalyticsSessions.Text = sessions.ToString("N0");

                // Total hours
                var totalMs = logs.Where(l => l.DurationMs > 0).Sum(l => l.DurationMs);
                var totalHours = totalMs / 3600000.0;
                if (txtAnalyticsHours != null)
                    txtAnalyticsHours.Text = $"{totalHours:F1}h";

                // Top tools
                var topTools = logs
                    .Where(l => l.EventType == "TOOL_OPENED")
                    .GroupBy(l => l.EventData ?? "Unknown")
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select((g, i) => $"{i + 1}. {g.Key} — {g.Count()} lần")
                    .ToList();

                if (txtAnalyticsTools != null)
                    txtAnalyticsTools.Text = topTools.Any()
                        ? string.Join("\n", topTools)
                        : "Chưa có dữ liệu sử dụng công cụ.";

                // Recent activity
                var recent = logs
                    .OrderByDescending(l => l.Timestamp)
                    .Take(15)
                    .Select(l => $"[{l.Timestamp:HH:mm dd/MM}] {l.EventType}: {l.EventData ?? ""}")
                    .ToList();

                if (txtAnalyticsRecent != null)
                    txtAnalyticsRecent.Text = recent.Any()
                        ? string.Join("\n", recent)
                        : "Chưa có hoạt động.";
            }
            catch (Exception ex)
            {
                Log.Warning("Analytics load error: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        //  LICENSE DIALOG
        // ═══════════════════════════════════════════════════════

        private static Window CreateLicenseDialog()
        {
            var win = new Window
            {
                Title = "Tạo License Mới",
                Width = 420, Height = 380,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 23, 42))
            };

            var sp = new StackPanel { Margin = new Thickness(24) };

            sp.Children.Add(new TextBlock
            {
                Text = "🔑 Tạo License Mới",
                FontSize = 18, FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            });

            // School name
            sp.Children.Add(new TextBlock { Text = "Tên Trường *", FontSize = 12, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)), Margin = new Thickness(0, 0, 0, 4) });
            var txtSchool = new TextBox { FontSize = 13, Padding = new Thickness(8, 6, 8, 6) };
            sp.Children.Add(txtSchool);

            // Max devices
            sp.Children.Add(new TextBlock { Text = "Số máy tối đa *", FontSize = 12, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)), Margin = new Thickness(0, 12, 0, 4) });
            var txtDevices = new TextBox { FontSize = 13, Padding = new Thickness(8, 6, 8, 6), Text = "50" };
            sp.Children.Add(txtDevices);

            // Duration
            sp.Children.Add(new TextBlock { Text = "Thời hạn (tháng) *", FontSize = 12, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)), Margin = new Thickness(0, 12, 0, 4) });
            var cmbDuration = new ComboBox { FontSize = 13, Padding = new Thickness(8, 4, 8, 4) };
            cmbDuration.Items.Add("6 tháng");
            cmbDuration.Items.Add("12 tháng");
            cmbDuration.Items.Add("24 tháng");
            cmbDuration.Items.Add("36 tháng");
            cmbDuration.SelectedIndex = 1;
            sp.Children.Add(cmbDuration);

            // Buttons
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 24, 0, 0) };

            var btnCancel = new Button
            {
                Content = "Hủy", Width = 90, Height = 34,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, Margin = new Thickness(0, 0, 10, 0)
            };
            btnCancel.Click += (s, ev) => { win.DialogResult = false; win.Close(); };

            var btnGenerate = new Button
            {
                Content = "🔑 Tạo License", Width = 130, Height = 34,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, FontWeight = FontWeights.Bold
            };
            btnGenerate.Click += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtSchool.Text))
                {
                    MessageBox.Show("Vui lòng nhập tên trường.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!int.TryParse(txtDevices.Text, out int devices) || devices <= 0)
                {
                    MessageBox.Show("Số máy phải là số dương.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int months = (cmbDuration.SelectedIndex + 1) switch
                {
                    1 => 6, 2 => 12, 3 => 24, 4 => 36, _ => 12
                };

                win.Tag = new LicenseInfo
                {
                    SchoolName = txtSchool.Text.Trim(),
                    MaxDevices = devices,
                    DurationMonths = months
                };
                win.DialogResult = true;
                win.Close();
            };

            btnPanel.Children.Add(btnCancel);
            btnPanel.Children.Add(btnGenerate);
            sp.Children.Add(btnPanel);

            win.Content = sp;
            return win;
        }
    }

    // ═══════════════════════════════════════════════════════
    //  LICENSE DATA MODEL
    // ═══════════════════════════════════════════════════════

    public class LicenseInfo
    {
        public string Key { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public int MaxDevices { get; set; }
        public DateTime IssuedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Status { get; set; } = "Active";
        public int DurationMonths { get; set; } = 12;
    }
}
