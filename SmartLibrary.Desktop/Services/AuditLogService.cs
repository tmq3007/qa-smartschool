using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO.Compression;
using SmartLibrary.Desktop.Models;

namespace SmartLibrary.Desktop.Services
{
    public class AuditLogService
    {
        private static ApiService? _apiService;
        private static readonly string LocalLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartLibrary", "logs", "audit_log.json");
        private static readonly string PendingLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartLibrary", "logs", "audit_log_pending.json");

        public static ObservableCollection<AuditLogDto> Logs { get; } = new();
        public static int PendingSyncCount { get; private set; } = 0;

        private static int _nextId = 1;

        /// <summary>
        /// Gọi 1 lần khi app khởi động để inject ApiService
        /// </summary>
        public static void Initialize(ApiService? apiService = null)
        {
            _apiService = apiService;

            // Tạo thư mục logs nếu chưa có
            var logsDir = Path.GetDirectoryName(LocalLogPath);
            if (!string.IsNullOrEmpty(logsDir) && !Directory.Exists(logsDir))
                Directory.CreateDirectory(logsDir);

            // Đếm pending logs
            CountPendingLogs();

            // Tải log cũ từ file local
            LoadLocalLogs();
        }

        /// <summary>
        /// Ghi log — Dual Write: API + Local File
        /// </summary>
        public static async Task WriteLogAsync(string actionType, string details, bool isSuccess = true)
        {
            var ssoId = AuthService.CurrentUserSsoId ?? "Hệ thống";
            var log = new AuditLogDto
            {
                Id = _nextId++,
                Timestamp = DateTime.Now,
                SsoUserId = ssoId,
                ActionType = actionType,
                Details = details,
                Status = isSuccess ? "Success" : "Failure"
            };

            // 1. Ghi vào ObservableCollection (hiển thị UI)
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                Logs.Insert(0, log);
            });

            // 2. Ghi file local (append-only, bất kể API thành công hay không)
            WriteToLocalFile(log);

            // 3. Thử ghi lên API
            if (_apiService != null)
            {
                try
                {
                    await _apiService.PostAsync("/AuditLogs", log, true);
                    // API thành công → không cần pending
                }
                catch (Exception)
                {
                    // API thất bại → ghi vào pending
                    WriteToPendingFile(log);
                    PendingSyncCount++;
                }
            }
            else
            {
                // Không có ApiService → ghi pending
                WriteToPendingFile(log);
                PendingSyncCount++;
            }
        }

        /// <summary>
        /// Đồng bộ các log pending lên server — gọi khi mạng hồi phục
        /// </summary>
        public static async Task<int> SyncPendingLogsAsync()
        {
            if (_apiService == null || !File.Exists(PendingLogPath)) return 0;

            int synced = 0;
            var lines = new List<string>();

            try
            {
                lines.AddRange(await File.ReadAllLinesAsync(PendingLogPath));
            }
            catch { return 0; }

            var failedLines = new List<string>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                AuditLogDto? log = null;
                try
                {
                    var decryptedLine = UnprotectString(line);
                    log = string.IsNullOrWhiteSpace(decryptedLine) ? null : JsonSerializer.Deserialize<AuditLogDto>(decryptedLine);
                }
                catch
                {
                    // Lỗi giải mã cục bộ thật sự -> ghi nhận và loại bỏ khỏi hàng chờ (không add vào failedLines)
                    try
                    {
                        var corruptedPath = Path.Combine(Path.GetDirectoryName(PendingLogPath) ?? "", "corrupted_logs.txt");
                        File.AppendAllText(corruptedPath, line + Environment.NewLine);
                    }
                    catch {}
                    continue;
                }

                if (log != null)
                {
                    try
                    {
                        await _apiService.PostAsync("/AuditLogs", log, true);
                        synced++;
                    }
                    catch
                    {
                        // Lỗi kết nối mạng API -> Giữ lại dòng này trong hàng chờ để đồng bộ lại sau
                        failedLines.Add(line);
                    }
                }
            }

            // Ghi lại chỉ các line thất bại
            await File.WriteAllLinesAsync(PendingLogPath, failedLines);
            PendingSyncCount = failedLines.Count;

            return synced;
        }

        private static void TrimLocalLogFile()
        {
            try
            {
                if (!File.Exists(LocalLogPath)) return;
                var lines = File.ReadAllLines(LocalLogPath);
                if (lines.Length > 5000)
                {
                    // Chỉ giữ lại 5000 dòng cuối cùng (bản ghi mới nhất)
                    var trimmedLines = lines.Skip(lines.Length - 5000).ToArray();
                    File.WriteAllLines(LocalLogPath, trimmedLines);
                }
            }
            catch { }
        }

        private static void WriteToLocalFile(AuditLogDto log)
        {
            try
            {
                var json = JsonSerializer.Serialize(log);
                var encrypted = ProtectString(json);
                File.AppendAllText(LocalLogPath, encrypted + Environment.NewLine);
                
                // Tiến hành cắt tỉa nếu dung lượng vượt quá giới hạn dòng
                TrimLocalLogFile();
            }
            catch { /* Silent fail — log file không critical bằng UI */ }
        }

        private static void WriteToPendingFile(AuditLogDto log)
        {
            try
            {
                var json = JsonSerializer.Serialize(log);
                var encrypted = ProtectString(json);
                File.AppendAllText(PendingLogPath, encrypted + Environment.NewLine);
            }
            catch { }
        }

        private static void CountPendingLogs()
        {
            try
            {
                if (File.Exists(PendingLogPath))
                {
                    PendingSyncCount = File.ReadAllLines(PendingLogPath)
                        .Count(l => !string.IsNullOrWhiteSpace(l));
                }
            }
            catch { PendingSyncCount = 0; }
        }

        private static void LoadLocalLogs()
        {
            try
            {
                if (!File.Exists(LocalLogPath)) return;
                var lines = File.ReadAllLines(LocalLogPath);

                // Đọc 100 dòng gần nhất (tránh load quá nhiều)
                int start = Math.Max(0, lines.Length - 100);
                for (int i = lines.Length - 1; i >= start; i--)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    try
                    {
                        var decrypted = UnprotectString(lines[i]);
                        if (string.IsNullOrWhiteSpace(decrypted)) continue;
                        var log = JsonSerializer.Deserialize<AuditLogDto>(decrypted);
                        if (log != null)
                        {
                            log.Id = _nextId++;
                            Logs.Add(log);
                        }
                    }
                    catch { /* Skip dòng bị hỏng */ }
                }
            }
            catch { }
        }

        private static string ProtectString(string input)
        {
            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(input);
                byte[] cipherBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(cipherBytes);
            }
            catch
            {
                return "";
            }
        }

        private static string UnprotectString(string base64Input)
        {
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(base64Input);
                byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return "";
            }
        }

        public static async Task ArchiveAndClearLogsAsync(string zipPath)
        {
            await Task.Run(async () =>
            {
                using (var zipStream = new FileStream(zipPath, FileMode.Create))
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                {
                    // 1. Ghi tệp mã hóa gốc
                    if (File.Exists(LocalLogPath))
                    {
                        archive.CreateEntryFromFile(LocalLogPath, "audit_log_encrypted.dat");
                    }

                    // 2. Tạo bản giải mã JSON dễ đọc
                    var entry = archive.CreateEntry("audit_log_readable.json");
                    using (var entryStream = entry.Open())
                    using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                    {
                        var decryptedLogs = new List<AuditLogDto>();
                        if (File.Exists(LocalLogPath))
                        {
                            var lines = File.ReadAllLines(LocalLogPath);
                            foreach (var line in lines)
                            {
                                if (string.IsNullOrWhiteSpace(line)) continue;
                                try
                                {
                                    var decrypted = UnprotectString(line);
                                    if (!string.IsNullOrWhiteSpace(decrypted))
                                    {
                                        var log = JsonSerializer.Deserialize<AuditLogDto>(decrypted);
                                        if (log != null) decryptedLogs.Add(log);
                                    }
                                }
                                catch {}
                            }
                        }
                        var options = new JsonSerializerOptions { WriteIndented = true };
                        var json = JsonSerializer.Serialize(decryptedLogs, options);
                        writer.Write(json);

                        // Tính toán mã băm SHA256 và lưu kèm
                        using (var sha256 = System.Security.Cryptography.SHA256.Create())
                        {
                            byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
                            string hashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                            
                            var hashEntry = archive.CreateEntry("audit_log_checksum.sha256");
                            using (var hashStream = hashEntry.Open())
                            using (var hashWriter = new StreamWriter(hashStream, System.Text.Encoding.UTF8))
                            {
                                hashWriter.Write(hashHex);
                            }
                        }
                    }
                }

                // 3. Clear bộ nhớ UI trên Dispatcher
                System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                {
                    Logs.Clear();
                });

                // 4. Xóa tệp cục bộ
                try
                {
                    if (File.Exists(LocalLogPath))
                    {
                        File.Delete(LocalLogPath);
                    }
                }
                catch {}

                // Ghi nhận log lưu trữ hành động mới vào file log mới
                await WriteLogAsync("ArchiveLogs", "Nhật ký hệ thống đã được lưu trữ và xóa sạch cục bộ.");
            });
        }
    }
}
