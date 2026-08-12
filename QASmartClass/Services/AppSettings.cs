using System;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Quản lý cài đặt toàn cục của ứng dụng
    /// </summary>
    public static class AppSettings
    {
        /// <summary>
        /// Biến master kiểm soát xác nhận thoát
        /// 1 = Hiển thị đầy đủ thông báo xác nhận
        /// 0 = Bỏ qua các xác nhận (thoát trực tiếp)
        /// </summary>
        public static int ShowExitConfirmation { get; set; } = 1;

        /// <summary>
        /// Kiểm tra có cần hiển thị xác nhận thoát không
        /// </summary>
        public static bool ShouldShowExitConfirmation => ShowExitConfirmation == 1;

        /// <summary>
        /// Biến kiểm soát hiển thị màn hình đăng nhập
        /// 1 = Hiển thị màn hình đăng nhập
        /// 0 = Bỏ qua đăng nhập và mở trực tiếp Dashboard
        /// </summary>
        public static int ShowLoginScreen { get; set; } = 0;

        /// <summary>
        /// Kiểm tra có cần hiển thị màn hình đăng nhập không
        /// </summary>
        public static bool ShouldShowLoginScreen => ShowLoginScreen == 1;

        // ═══════════════════════════════════════════════════════════
        //  FOCUS SETTINGS — Cài đặt hệ thống Focus bài giảng
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Tỉ lệ phóng to layout khi Focus block (ScaleTransform)
        /// 1.0 = không zoom, 1.2 = 120%, 1.3 = 130%
        /// Mặc định: 1.25 (kết hợp với FontScale = 170% tổng thể)
        /// </summary>
        public static double FocusZoomScale { get; set; } = 1.25;

        /// <summary>
        /// Tỉ lệ phóng to font chữ khi Focus block
        /// 1.0 = giữ nguyên, 1.4 = 140%, 1.5 = 150%
        /// Mặc định: 1.45 (kết hợp với ZoomScale = 170% tổng thể)
        /// </summary>
        public static double FocusFontScale { get; set; } = 1.45;

        /// <summary>
        /// Độ mờ (opacity) của các block KHÔNG được focus
        /// 0.0 = ẩn hoàn toàn, 0.05 = mờ 95%, 0.1 = mờ 90%
        /// Mặc định: 0.05 (mờ 95%)
        /// </summary>
        public static double FocusDimOpacity { get; set; } = 0.05;

        /// <summary>
        /// Đánh dấu đã hoàn thành First Run (Tour hướng dẫn)
        /// </summary>
        public static bool HasCompletedFirstRun { get; set; } = false;

        // ═══════════════════════════════════════════════════════════
        //  MASTER LOGIN & TEST SETTINGS
        // ═══════════════════════════════════════════════════════════

        public static int Broadcast_UdpHeartbeatTimeout { get; set; } = 15; // === UPGRADE_07: Đồng bộ default 15s với StudentShell ===
        public static bool Broadcast_EnableScreenExclusion { get; set; } = true;
        public static bool Broadcast_TurboMode { get; set; } = false; // === UPGRADE_13: Turbo Stream Mode ===
        public static bool Broadcast_AllowTurboMode { get; set; } = true; // === UPGRADE_13: Cho phép dùng chế độ siêu tốc ===
        public static bool Broadcast_TurboDisableExclusion { get; set; } = true; // === UPGRADE_13: Cho phép tắt Exclusion trong chế độ siêu tốc ===
        public static string Broadcast_TurboStudentBg { get; set; } = "FlatBlack"; // === UPGRADE_13: Nền màn hình HS (FlatBlack / BrandColor) ===

        public static string RunningMode { get; set; } = "Release"; // "Test" hoặc "Release"
        public static string ActiveUserRole { get; set; } = "All"; // "All", "Teacher", "Student", "Parent", "Staff"
        public static string ShowInactivePackagesMode { get; set; } = "Hidden"; // "Hidden" hoặc "LockedUpsell"
        public static string LastTestUserTeacher { get; set; } = "";
        public static string LastTestUserStudent { get; set; } = "";
        public static string LastTestUserParent { get; set; } = "";
        public static string LastTestUserStaff { get; set; } = "";
        public static int LastCountdownSeconds { get; set; } = 300;
        public static int CacheRetentionDays { get; set; } = 7;

        public static string GraphicsQuality { get; set; } = "Auto";
        public static string GraphicsLevel { get; set; } = "DynamicSVG";

        private static bool? _isIntegratedGraphics;
        public static bool IsIntegratedGraphics()
        {
            if (_isIntegratedGraphics.HasValue) return _isIntegratedGraphics.Value;
            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string name = obj["Name"]?.ToString() ?? "";
                        if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("HD Graphics", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Iris", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Basic Render", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Microsoft Basic Display", StringComparison.OrdinalIgnoreCase))
                        {
                            _isIntegratedGraphics = true;
                            return true;
                        }
                        
                        var memObj = obj["AdapterRAM"];
                        if (memObj != null)
                        {
                            long bytes = Convert.ToInt64(memObj);
                            double mb = bytes / (1024.0 * 1024.0);
                            if (mb > 0 && mb <= 512)
                            {
                                _isIntegratedGraphics = true;
                                return true;
                            }
                        }
                    }
                }
            }
            catch { }
            _isIntegratedGraphics = false;
            return false;
        }

        public static int GetRecommended3DResolution()
        {
            if (string.Equals(GraphicsQuality, "Low", StringComparison.OrdinalIgnoreCase)) return 20;
            if (string.Equals(GraphicsQuality, "Medium", StringComparison.OrdinalIgnoreCase)) return 35;
            if (string.Equals(GraphicsQuality, "High", StringComparison.OrdinalIgnoreCase)) return 50;
            return IsIntegratedGraphics() ? 20 : 50;
        }

        public static void Load()
        {
            try
            {
                string settingsDir = QASmartClass.Services.AppPaths.SettingsDir;
                string path = System.IO.Path.Combine(settingsDir, "app_settings.json");
                if (System.IO.File.Exists(path))
                {
                    string json = System.IO.File.ReadAllText(path);
                    var data = System.Text.Json.JsonSerializer.Deserialize<AppSettingsData>(json);
                    if (data != null)
                    {
                        ShowExitConfirmation = data.ShowExitConfirmation;
                        ShowLoginScreen = data.ShowLoginScreen;
                        FocusZoomScale = data.FocusZoomScale;
                        FocusFontScale = data.FocusFontScale;
                        FocusDimOpacity = data.FocusDimOpacity;
                        HasCompletedFirstRun = data.HasCompletedFirstRun;
                        RunningMode = data.RunningMode ?? "Release";
                        ActiveUserRole = data.ActiveUserRole ?? "All";
                        ShowInactivePackagesMode = data.ShowInactivePackagesMode ?? "Hidden";
                        LastTestUserTeacher = data.LastTestUserTeacher ?? "";
                        LastTestUserStudent = data.LastTestUserStudent ?? "";
                        LastTestUserParent = data.LastTestUserParent ?? "";
                        LastCountdownSeconds = data.LastCountdownSeconds > 0 ? data.LastCountdownSeconds : 300;
                        CacheRetentionDays = data.CacheRetentionDays > 0 ? data.CacheRetentionDays : 7;
                        GraphicsQuality = data.GraphicsQuality ?? "Auto";
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Load AppSettings failed: {Error}", ex.Message);
            }
        }

        public static void Save()
        {
            try
            {
                string settingsDir = QASmartClass.Services.AppPaths.SettingsDir;
                System.IO.Directory.CreateDirectory(settingsDir);
                string path = System.IO.Path.Combine(settingsDir, "app_settings.json");
                var data = new AppSettingsData
                {
                    ShowExitConfirmation = ShowExitConfirmation,
                    ShowLoginScreen = ShowLoginScreen,
                    FocusZoomScale = FocusZoomScale,
                    FocusFontScale = FocusFontScale,
                    FocusDimOpacity = FocusDimOpacity,
                    HasCompletedFirstRun = HasCompletedFirstRun,
                    RunningMode = RunningMode,
                    ActiveUserRole = ActiveUserRole,
                    ShowInactivePackagesMode = ShowInactivePackagesMode,
                    LastTestUserTeacher = LastTestUserTeacher,
                    LastTestUserStudent = LastTestUserStudent,
                    LastTestUserStaff = LastTestUserStaff,
                    LastCountdownSeconds = LastCountdownSeconds,
                    CacheRetentionDays = CacheRetentionDays,
                    GraphicsQuality = GraphicsQuality
                };
                string json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Save AppSettings failed: {Error}", ex.Message);
            }
        }

        public static void LoadFromDatabase(QASmartClass.Data.AppDbContext db)
        {
            try
            {
                var timeoutSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Broadcast_UdpHeartbeatTimeout");
                if (timeoutSetting != null && int.TryParse(timeoutSetting.Value, out int timeoutVal))
                {
                    Broadcast_UdpHeartbeatTimeout = timeoutVal;
                }

                var exclusionSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Broadcast_EnableScreenExclusion");
                if (exclusionSetting != null)
                {
                    Broadcast_EnableScreenExclusion = string.Equals(exclusionSetting.Value, "true", StringComparison.OrdinalIgnoreCase);
                }

                var graphicsSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_UI_GraphicsLevel");
                if (graphicsSetting != null)
                {
                    GraphicsLevel = graphicsSetting.Value;
                }

                var allowTurboSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Broadcast_AllowTurboMode");
                if (allowTurboSetting != null)
                {
                    Broadcast_AllowTurboMode = string.Equals(allowTurboSetting.Value, "true", StringComparison.OrdinalIgnoreCase);
                }

                var turboDisableExSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Broadcast_TurboDisableExclusion");
                if (turboDisableExSetting != null)
                {
                    Broadcast_TurboDisableExclusion = string.Equals(turboDisableExSetting.Value, "true", StringComparison.OrdinalIgnoreCase);
                }

                var turboBgSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Broadcast_TurboStudentBg");
                if (turboBgSetting != null)
                {
                    Broadcast_TurboStudentBg = turboBgSetting.Value;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("[AppSettings] Load from database failed: {Error}", ex.Message);
            }
        }
    }

    public class AppSettingsData
    {
        public int ShowExitConfirmation { get; set; } = 1;
        public int ShowLoginScreen { get; set; } = 0;
        public double FocusZoomScale { get; set; } = 1.25;
        public double FocusFontScale { get; set; } = 1.45;
        public double FocusDimOpacity { get; set; } = 0.05;
        public bool HasCompletedFirstRun { get; set; } = false;
        public string RunningMode { get; set; } = "Release";
        public string ActiveUserRole { get; set; } = "All";
        public string ShowInactivePackagesMode { get; set; } = "Hidden";
        public string LastTestUserTeacher { get; set; } = "";
        public string LastTestUserStudent { get; set; } = "";
        public string LastTestUserParent { get; set; } = "";
        public string LastTestUserStaff { get; set; } = "";
        public int LastCountdownSeconds { get; set; } = 300;
        public int CacheRetentionDays { get; set; } = 7;
        public string GraphicsQuality { get; set; } = "Auto";
    }
}
