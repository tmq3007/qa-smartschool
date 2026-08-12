using System;
using System.IO;
using System.Text.Json;
using Serilog;

namespace QASmartClass.Services
{
    public class WorkstationConfig
    {
        // ═══ ĐỊNH DANH MÁY ═══
        public string MachineId { get; set; } = "";        // VD: "PC-LAB01-05"
        public string MachineName { get; set; } = "";      // Tên hiển thị: "Máy 05"
        public string RoomId { get; set; } = "";           // VD: "LAB01", "PHONG-TIN-1"
        public string RoomName { get; set; } = "";         // VD: "Phòng Tin học 1"
        public string SeatPosition { get; set; } = "";     // VD: "Dãy A - Bàn 3"
        public string MachineRole { get; set; } = "student"; // "teacher" | "student" | "kiosk"

        // ═══ MẠNG ═══
        public int DiscoveryPort { get; set; } = 29876;
        public int TcpPort { get; set; } = 29877;
        public int FilePort { get; set; } = 29879;
        public string DefaultTeacherIP { get; set; } = "";  // IP GV mặc định (auto-fill)
        public bool AutoConnect { get; set; } = true;       // Tự kết nối khi mở app
        public int ReconnectIntervalSec { get; set; } = 15;

        // ═══ THƯ MỤC ═══
        public string ReceivedFilesDir { get; set; } = "";  // Rỗng = dùng AppPaths default
        public string SubmissionsDir { get; set; } = "";    // Thư mục nộp bài
        public string ScreenshotDir { get; set; } = "";     // Lưu screenshot tạm

        // ═══ GIỚI HẠN ═══
        public int MaxFileReceiveMB { get; set; } = 100;
        public int MaxSubmitFileMB { get; set; } = 50;
        public int ScreenshotQuality { get; set; } = 40;   // JPEG quality %
        public int ScreenshotWidth { get; set; } = 320;
        public int ScreenshotHeight { get; set; } = 180;

        // ═══ CHÍNH SÁCH ═══
        public bool AllowFileReceive { get; set; } = true;
        public bool AllowScreenCapture { get; set; } = true;
        public bool AllowWebBrowsing { get; set; } = true;
        public bool ShowTaskbar { get; set; } = false;      // Ẩn taskbar khi đang học

        // ═══ METADATA ═══
        public DateTime ConfiguredAt { get; set; } = DateTime.MinValue;
        public string ConfiguredBy { get; set; } = "";      // IT admin đã cấu hình

        // Load / Save
        public static WorkstationConfig Load()
        {
            try
            {
                if (File.Exists(AppPaths.WorkstationConfigFile))
                {
                    var json = File.ReadAllText(AppPaths.WorkstationConfigFile);
                    return JsonSerializer.Deserialize<WorkstationConfig>(json) ?? new WorkstationConfig();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error loading WorkstationConfig: {Err}", ex.Message);
            }
            return new WorkstationConfig();
        }

        public void Save()
        {
            string tempFile = AppPaths.WorkstationConfigFile + ".tmp";
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(this, options);
                
                // Write atomically to temporary file first
                File.WriteAllText(tempFile, json);
                
                // Atomically replace target file
                if (File.Exists(AppPaths.WorkstationConfigFile))
                {
                    File.Delete(AppPaths.WorkstationConfigFile);
                }
                File.Move(tempFile, AppPaths.WorkstationConfigFile);
            }
            catch (Exception ex)
            {
                Log.Warning("Error saving WorkstationConfig: {Err}", ex.Message);
                try
                {
                    if (File.Exists(tempFile)) File.Delete(tempFile);
                }
                catch { }
            }
        }
    }
}
