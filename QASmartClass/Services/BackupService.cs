using System;
using System.IO;
using System.Linq;
using Serilog;
using QASmartClass.Data;
using Microsoft.Data.Sqlite;

namespace QASmartClass.Services
{
    public enum RestoreResult
    {
        Success,
        FileNotFound,
        IntegrityCheckFailed,
        ChecksumMismatch,
        UnknownError
    }

    /// <summary>
    /// Service sao lưu và khôi phục cơ sở dữ liệu SQLite.
    /// Giữ lại tối đa 7 bản backup gần nhất, tự động xóa bản cũ.
    /// </summary>
    public class BackupService
    {
        private readonly string _dbPath;
        private readonly string _backupDir;
        private const int MAX_BACKUPS = 7;

        public BackupService()
        {
            _dbPath = AppPaths.DatabaseFile;
            _backupDir = AppPaths.BackupsDir;
            Directory.CreateDirectory(_backupDir);
        }

        /// <summary>
        /// Tạo bản sao lưu thủ công (nút "Sao lưu" trên UI).
        /// Trả về đường dẫn file backup hoặc null nếu lỗi.
        /// </summary>
        public string? CreateBackup(string label = "manual")
        {
            try
            {
                // Only Admin or HieuTruong can backup. Note: AutoBackupService might invoke this,
                // but since AutoBackup is run by the application, we should check role or allow automated backup if it is run in system context.
                // Wait, if label == "auto", we might skip the guard or allow it.
                // Let's check: if label is "auto" or "pre_restore", should we check role? 
                // To be safe, if we are in UI-initiated manual backup, we definitely check role.
                // Or we can just check if UserSessionService.Instance.IsLoggedIn is true, check role. If not logged in (e.g. startup auto backup), allow it.
                if (UserSessionService.Instance.IsLoggedIn && label == "manual")
                {
                    AuthorizationGuard.EnsureRole(UserSessionService.Instance.Role, 
                        StatusConstants.TeacherRole.Admin, 
                        StatusConstants.TeacherRole.HieuTruong);
                }

                if (!File.Exists(_dbPath))
                {
                    Log.Warning("BackupService: Database file not found: {Path}", _dbPath);
                    return null;
                }

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFile = Path.Combine(_backupDir, $"smartclass_{label}_{timestamp}.db");

                File.Copy(_dbPath, backupFile, overwrite: true);
                Log.Information("BackupService: Created backup → {Path} ({Size} KB)",
                    backupFile, new FileInfo(backupFile).Length / 1024);

                string checksum = CalculateSHA256(backupFile);
                File.WriteAllText(backupFile + ".sha256", checksum);

                // Auto-cleanup bản cũ
                CleanupOldBackups();

                return backupFile;
            }
            catch (Exception ex)
            {
                Log.Error("BackupService: Backup failed — {Err}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Khôi phục database từ file backup.
        /// LƯU Ý: Cần restart ứng dụng sau khi restore.
        /// </summary>
        public RestoreResult Restore(string backupFilePath)
        {
            try
            {
                AuthorizationGuard.EnsureRole(UserSessionService.Instance.Role, 
                    StatusConstants.TeacherRole.Admin, 
                    StatusConstants.TeacherRole.HieuTruong);

                if (!File.Exists(backupFilePath))
                {
                    Log.Warning("BackupService: Backup file not found: {Path}", backupFilePath);
                    return RestoreResult.FileNotFound;
                }

                // Kiểm tra tính toàn vẹn của tệp SQLite sao lưu trước khi ghi đè
                if (!VerifyDatabaseIntegrity(backupFilePath))
                {
                    Log.Error("BackupService: SQLite database integrity check failed for file {Path}", backupFilePath);
                    return RestoreResult.IntegrityCheckFailed;
                }

                string checksumPath = backupFilePath + ".sha256";
                if (File.Exists(checksumPath))
                {
                    string expected = File.ReadAllText(checksumPath).Trim().ToLowerInvariant();
                    string actual = CalculateSHA256(backupFilePath);
                    if (expected != actual)
                    {
                        Log.Error("BackupService: Checksum verification failed for file {Path}. Expected: {Exp}, Actual: {Act}", backupFilePath, expected, actual);
                        return RestoreResult.ChecksumMismatch;
                    }
                }

                // Tạo bản backup trước khi restore (phòng sự cố)
                CreateBackup("pre_restore");

                // Giải phóng các kết nối SQLite trong pool và yêu cầu GC thu hồi tài nguyên để mở khóa tệp tin
                SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                File.Copy(backupFilePath, _dbPath, overwrite: true);
                Log.Information("BackupService: Restored from {Path}", backupFilePath);
                return RestoreResult.Success;
            }
            catch (Exception ex)
            {
                Log.Error("BackupService: Restore failed — {Err}", ex.Message);
                return RestoreResult.UnknownError;
            }
        }

        /// <summary>
        /// Kiểm tra tính toàn vẹn của tệp tin SQLite thông qua lệnh PRAGMA integrity_check.
        /// </summary>
        private bool VerifyDatabaseIntegrity(string filePath)
        {
            try
            {
                var keyBytes = QASmartClass.Data.DbEncryptionKeyManager.GetOrInitializeKey();
                var hexKey = Convert.ToHexString(keyBytes);
                string connectionString = $"Data Source={filePath};Password={hexKey};Pooling=False;";
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "PRAGMA integrity_check;";
                        var result = command.ExecuteScalar()?.ToString();
                        return result == "ok";
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("BackupService: Database integrity check failed for file {Path}: {Err}", filePath, ex.Message);
                return false;
            }
        }

        /// <summary>Lấy danh sách backup hiện có (mới nhất trước)</summary>
        public FileInfo[] GetBackupList()
        {
            try
            {
                return new DirectoryInfo(_backupDir)
                    .GetFiles("smartclass_*.db")
                    .OrderByDescending(f => f.CreationTime)
                    .ToArray();
            }
            catch { return Array.Empty<FileInfo>(); }
        }

        /// <summary>Giữ lại MAX_BACKUPS bản, xóa bản cũ nhất</summary>
        private void CleanupOldBackups()
        {
            try
            {
                var files = new DirectoryInfo(_backupDir)
                    .GetFiles("smartclass_*.db")
                    .OrderByDescending(f => f.CreationTime)
                    .ToArray();

                if (files.Length <= MAX_BACKUPS) return;

                for (int i = MAX_BACKUPS; i < files.Length; i++)
                {
                    try
                    {
                        string shaFile = files[i].FullName + ".sha256";
                        if (File.Exists(shaFile)) File.Delete(shaFile);
                    }
                    catch {}
                    files[i].Delete();
                    Log.Information("BackupService: Cleaned up old backup {File}", files[i].Name);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("BackupService: Cleanup error — {Err}", ex.Message);
            }
        }

        private string CalculateSHA256(string filePath)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}

