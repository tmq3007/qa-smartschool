using System;
using System.IO;
using System.Text.Json;
using Serilog;

namespace QASmartClass.Services
{
    public class AppConfig
    {
        // ═══ TRƯỜNG HỌC ═══
        public string SchoolName { get; set; } = "Trường THPT QA";
        public string SchoolCode { get; set; } = "";
        public string CanteenComPort { get; set; } = "";
        public string YouthUnionComPort { get; set; } = "";
        public int YouthUnionCardStandard { get; set; } = 0; // 0: Shared Canteen Card, 1: Separate DoanCardNo
        public int YouthUnionDbEncodingMode { get; set; } = 0; // 0: Legacy (ASCII), 1: Unicode (Vietnamese Accent)

        // ═══ MẠNG ═══
        public int TcpPort { get; set; } = 29877;
        public int MaxStudents { get; set; } = 35;
        public bool AutoReconnect { get; set; } = true;

        // ═══ LƯU TRỮ ═══
        public string CustomBackupDir { get; set; } = "";
        public string CustomExportDir { get; set; } = "";
        public int MaxBackupFiles { get; set; } = 10;
        public bool AutoBackupOnStartup { get; set; } = true;
        public bool EnableAutoSave { get; set; } = true;
        public int AutoSaveIntervalMinutes { get; set; } = 5;
        public bool EnableAutoBackup { get; set; } = true;
        public string BackupFrequency { get; set; } = "Daily";

        // ═══ GIAO DIỆN ═══
        public string Theme { get; set; } = "Light";
        public string Language { get; set; } = "vi";
        public int FontSize { get; set; } = 14;
        public string AccentColor { get; set; } = "#2E86DE";

        // ═══ BẢO MẬT ═══
        public int PinMaxAttempts { get; set; } = 5;
        public int PinLockoutMinutes { get; set; } = 5;
        public bool RequirePinForReset { get; set; } = true;
        public bool EnableAdminLock { get; set; } = false;
        public string AdminPasswordHash { get; set; } = "";
        public string AuditSecretKeyEncrypted { get; set; } = "";

        // ═══ TELEMETRY ═══
        public bool EnableTelemetry { get; set; } = true;
        public int TelemetryRetentionDays { get; set; } = 90;

        // ═══ ĐỒ HỌA 3D ═══
        public bool AutoOptimizeForIntegratedGraphics { get; set; } = true;
        public bool Disable3DAntiAliasing { get; set; } = false;
        public bool Reduce3DMeshResolution { get; set; } = false;
        public bool EnableHardwareAcceleration { get; set; } = true;
        public bool ReduceAnimations { get; set; } = false;

        private static bool? _isIntegratedGraphicsDetected = null;

        public static bool IsIntegratedGraphicsDetected
        {
            get
            {
                if (_isIntegratedGraphicsDetected == null)
                {
                    _isIntegratedGraphicsDetected = CheckIsIntegratedGraphics();
                }
                return _isIntegratedGraphicsDetected.Value;
            }
        }

        private static bool CheckIsIntegratedGraphics()
        {
            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string name = obj["Name"]?.ToString() ?? "";
                        if (string.IsNullOrEmpty(name)) continue;

                        name = name.ToLower();
                        if (name.Contains("intel") || 
                            name.Contains("iris") || 
                            name.Contains("uhd") || 
                            name.Contains("hd graphics") || 
                            name.Contains("integrated") || 
                            (name.Contains("amd") && (name.Contains("radeon") && (name.Contains("tm") || name.Contains("graphics")) && !name.Contains("rx") && !name.Contains("pro"))))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("CheckIsIntegratedGraphics failed: {Error}", ex.Message);
            }
            return false;
        }


        // ═══ ADMIN ═══
        public int AdminHandshakeTimeoutSec { get; set; } = 60;

        // ═══ SERVER ═══
        public string ServerUrl { get; set; } = "";
        public int HeartbeatIntervalSec { get; set; } = 300;

        // Load / Save
        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(AppPaths.AppConfigFile))
                {
                    var json = File.ReadAllText(AppPaths.AppConfigFile);
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error loading AppConfig: {Err}", ex.Message);
            }
            return new AppConfig();
        }

        public void Save()
        {
            string tempFile = AppPaths.AppConfigFile + ".tmp";
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(this, options);
                
                // Write atomically to temporary file first
                File.WriteAllText(tempFile, json);
                
                // Atomically replace target file
                if (File.Exists(AppPaths.AppConfigFile))
                {
                    File.Delete(AppPaths.AppConfigFile);
                }
                File.Move(tempFile, AppPaths.AppConfigFile);
            }
            catch (Exception ex)
            {
                Log.Warning("Error saving AppConfig: {Err}", ex.Message);
                try
                {
                    if (File.Exists(tempFile)) File.Delete(tempFile);
                }
                catch { }
            }
        }
    }
}
