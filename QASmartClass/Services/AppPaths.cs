using System;
using System.IO;

namespace QASmartClass.Services
{
    public static class AppPaths
    {
        // ═══ GỐC ═══
        public static string? DataDirOverride { get; set; }

        public static string RootDir => DataDirOverride != null 
            ? DataDirOverride 
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QASmartClass");

        // ═══ DATABASE ═══
        private static readonly System.Threading.AsyncLocal<string?> _databaseFileOverride = new();
        private static readonly System.Threading.AsyncLocal<string?> _dbVersionFileOverride = new();

        public static string DatabaseFile
        {
            get
            {
                if (_databaseFileOverride.Value != null) return _databaseFileOverride.Value;

                string dbName = string.Equals(QASmartTouch.Services.AppSettings.RunningMode, "Test", StringComparison.OrdinalIgnoreCase)
                    ? "smartclass_test.db"
                    : "smartclass.db";

                // Priority 1: Biến môi trường override (cho sysadmin/deploy linh hoạt)
                string? envPath = Environment.GetEnvironmentVariable("QASC_DB_PATH");
                if (!string.IsNullOrWhiteSpace(envPath))
                    return Path.IsPathRooted(envPath) ? envPath : Path.Combine(envPath, dbName);

                // Priority 2: Cạnh file .exe (thư mục cài đặt) — nếu DB đã tồn tại ở đó
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string exeDbPath = Path.Combine(exeDir, dbName);
                if (File.Exists(exeDbPath))
                    return exeDbPath;

                // Priority 3: %LocalAppData%\QASmartClass (mặc định, không cần quyền Admin)
                return Path.Combine(RootDir, dbName);
            }
            set => _databaseFileOverride.Value = value;
        }

        public static string DbVersionFile
        {
            get => _dbVersionFileOverride.Value ?? Path.Combine(RootDir, "db_version.txt");
            set => _dbVersionFileOverride.Value = value;
        }

        // ═══ CẤU HÌNH ═══
        public static string SettingsDir => Path.Combine(RootDir, "Settings");
        public static string AppConfigFile => Path.Combine(SettingsDir, "app_config.json");
        public static string ClassroomSettingsFile => Path.Combine(SettingsDir, "settings.json");
        public static string TextJournalFile => Path.Combine(SettingsDir, "temp_journal.log");
        public static string AdminSecurityFile => Path.Combine(SettingsDir, "admin_security.json");
        public static string StudentProfileFile => GetStudentProfilePath();
        public static string WorkstationConfigFile => Path.Combine(SettingsDir, "workstation.json");
        public static string FirstRunFlag => Path.Combine(RootDir, ".first_run_done");

        // ═══ LOGS ═══
        public static string LogsDir => Path.Combine(RootDir, "Logs");

        // ═══ LƯU TRỮ ═══
        public static string BackupsDir => Path.Combine(DocumentsDir, "Backups");
        public static string ExportsDir => Path.Combine(DocumentsDir, "Exports");
        public static string SharedFilesDir => Path.Combine(RootDir, "SharedFiles");
        public static string ReceivedFilesDir => Path.Combine(DocumentsDir, "ReceivedFiles");
        public static string SubmittedFilesDir => Path.Combine(DocumentsDir, "Submissions");
        public static string TempDir => Path.Combine(RootDir, "Temp");
        public static string CrashesDir => Path.Combine(RootDir, "crashes");
        public static string ScreenCapturesDir => Path.Combine(RootDir, "ScreenCaptures");
        public static string DocumentsDir => DataDirOverride != null 
            ? Path.Combine(DataDirOverride, "Documents")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QASmartClass");
        public static string PicturesDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "QASmartClass");
        
        public static string DefaultDatabaseTemplate
        {
            get
            {
                string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "smartclass_default.db");
                if (File.Exists(p1)) return p1;
                string p2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "smartclass.db");
                if (File.Exists(p2)) return p2;
                string p3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QASmartClass.db");
                if (File.Exists(p3)) return p3;
                string p4 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "smartclass.db");
                if (File.Exists(p4)) return p4;
                return p1;
            }
        }
        public static string SystemRestoreBackupDir => Path.Combine(DocumentsDir, "Backups", "SystemRestore");

        /// <summary>Tạo tất cả thư mục cần thiết khi khởi động</summary>
        public static void EnsureDirectories()
        {
            foreach (var dir in new[] { RootDir, SettingsDir, LogsDir, BackupsDir,
                ExportsDir, SharedFilesDir, ReceivedFilesDir, SubmittedFilesDir, TempDir, CrashesDir, SystemRestoreBackupDir })
            {
                Directory.CreateDirectory(dir);
            }
        }
        
        public static string GetTopologyFile(string roomId) => Path.Combine(SettingsDir, $"topology_{roomId}.json");

        public static string CurrentStudentCode { get; set; } = "";

        private static string GetStudentProfilePath()
        {
            string hwId = GetHardwareId();
            if (string.IsNullOrEmpty(CurrentStudentCode))
            {
                return Path.Combine(SettingsDir, $"student_profile_{hwId}.json");
            }
            return Path.Combine(SettingsDir, $"student_profile_{hwId}_{CurrentStudentCode}.json");
        }

        public static string GetHardwareId()
        {
            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string uuid = obj["UUID"]?.ToString();
                        if (!string.IsNullOrEmpty(uuid) && uuid != "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")
                        {
                            return uuid.Replace("-", "").ToLower();
                        }
                    }
                }
            }
            catch
            {
                // ignore WMI errors
            }

            try
            {
                var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in interfaces)
                {
                    if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && 
                        ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    {
                        string mac = ni.GetPhysicalAddress().ToString();
                        if (!string.IsNullOrEmpty(mac)) return mac.ToLower();
                    }
                }
            }
            catch
            {
                // ignore network adapter errors
            }

            return Environment.MachineName.ToLower();
        }
    }
}
