using System;
using System.IO;

namespace QASmartTouch.Services
{
    public static class AppPaths
    {
        // ═══ GỐC ═══
        public static string? DataDirOverride { get; set; }

        public static string RootDir => DataDirOverride != null
            ? DataDirOverride
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QASmartTouch");

        // ═══ CẤU HÌNH ═══
        public static string SettingsDir => Path.Combine(RootDir, "Settings");
        public static string AppConfigFile => Path.Combine(SettingsDir, "app_config.json");
        public static string ClassroomSettingsFile => Path.Combine(SettingsDir, "settings.json");
        public static string TextJournalFile => Path.Combine(SettingsDir, "temp_journal.log");
        public static string AdminSecurityFile => Path.Combine(SettingsDir, "admin_security.json");
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
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QASmartTouch");
        public static string PicturesDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "QASmartTouch");

        public static string SystemRestoreBackupDir => Path.Combine(DocumentsDir, "Backups", "SystemRestore");
        public static string WebView2UserDataDir => Path.Combine(RootDir, "WebView2");

        /// <summary>Tạo tất cả thư mục cần thiết khi khởi động</summary>
        public static void EnsureDirectories()
        {
            foreach (var dir in new[] { RootDir, SettingsDir, LogsDir, BackupsDir,
                ExportsDir, SharedFilesDir, ReceivedFilesDir, SubmittedFilesDir, TempDir, CrashesDir, SystemRestoreBackupDir, WebView2UserDataDir })
            {
                Directory.CreateDirectory(dir);
            }
        }

        public static string GetTopologyFile(string roomId) => Path.Combine(SettingsDir, $"topology_{roomId}.json");

        public static string GetHardwareId()
        {
            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string? uuid = obj["UUID"]?.ToString();
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
