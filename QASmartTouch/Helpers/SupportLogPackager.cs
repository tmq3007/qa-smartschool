using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using QASmartTouch.Services;

namespace QASmartTouch.Helpers
{
    public static class SupportLogPackager
    {
        public static async Task<string> PackageLogsAsync(bool anonymizeData = false)
        {
            await Task.CompletedTask;
            string tempDir = Path.Combine(AppPaths.TempDir, "SupportPack_" + Guid.NewGuid().ToString().Substring(0, 8));
            Directory.CreateDirectory(tempDir);

            string zipPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"QA_SmartTouch_SupportLog_{DateTime.Now:yyyyMMdd_HHmmss}.zip"
            );

            try
            {
                // 1. Chụp ảnh màn hình
                string screenshotPath = Path.Combine(tempDir, "screenshot.png");
                CaptureScreenshot(screenshotPath);

                // 2. Thu thập thông tin phần cứng
                string sysInfoPath = Path.Combine(tempDir, "sysinfo.txt");
                CollectSystemInfo(sysInfoPath);

                // 3. Thu thập file log 3 ngày gần đây
                string logsDestDir = Path.Combine(tempDir, "Logs");
                Directory.CreateDirectory(logsDestDir);
                CollectRecentLogs(logsDestDir);

                // 4. Nén tất cả thành file ZIP
                if (File.Exists(zipPath)) File.Delete(zipPath);
                ZipFile.CreateFromDirectory(tempDir, zipPath);

                return zipPath;
            }
            finally
            {
                // Dọn dẹp thư mục tạm
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch { }
            }
        }

        private static void CaptureScreenshot(string path)
        {
            try
            {
                int width = (int)System.Windows.SystemParameters.PrimaryScreenWidth;
                int height = (int)System.Windows.SystemParameters.PrimaryScreenHeight;
                using var bmp = new System.Drawing.Bitmap(width, height);
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                }
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[SupportPack] Failed to capture screenshot");
            }
        }

        private static void CollectSystemInfo(string path)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== THÔNG TIN HỆ THỐNG ===");
                sb.AppendLine($"Thời gian xuất: {DateTime.Now}");
                sb.AppendLine($"Hệ điều hành: {Environment.OSVersion}");
                sb.AppendLine($"Tên máy: {Environment.MachineName}");
                sb.AppendLine($"Số luồng CPU: {Environment.ProcessorCount}");
                sb.AppendLine($"64-Bit HĐH: {Environment.Is64BitOperatingSystem}");
                sb.AppendLine($"64-Bit Tiến trình: {Environment.Is64BitProcess}");
                sb.AppendLine($"Phiên bản .NET: {Environment.Version}");
                sb.AppendLine($"Thư mục chạy: {AppDomain.CurrentDomain.BaseDirectory}");

                try
                {
                    string wvVer = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
                    sb.AppendLine($"Thư viện WebView2: {wvVer}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Thư viện WebView2: Chưa cài đặt / Lỗi ({ex.Message})");
                }

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[SupportPack] Failed to collect system info");
            }
        }

        private static void CollectRecentLogs(string destDir)
        {
            string srcDir = AppPaths.LogsDir;
            if (!Directory.Exists(srcDir)) return;

            try
            {
                var files = Directory.GetFiles(srcDir, "*.log");
                DateTime cutoff = DateTime.Now.AddDays(-3);

                foreach (var file in files)
                {
                    var fi = new FileInfo(file);
                    if (fi.LastWriteTime >= cutoff)
                    {
                        string destPath = Path.Combine(destDir, fi.Name);
                        File.Copy(file, destPath, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[SupportPack] Failed to collect recent logs");
            }
        }
    }
}
