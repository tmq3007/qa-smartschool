using System;
using System.IO;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class AutoBackupService
    {
        private readonly AppDbContext _db;

        public AutoBackupService(AppDbContext dbContext)
        {
            _db = dbContext;
        }

        public void RunBackup(string triggeredBy = "System")
        {
            string fileName = $"backup_db_{DateTime.Now:yyyyMMdd_HHmm}.db";
            string backupDir = AppPaths.BackupsDir;
            Directory.CreateDirectory(backupDir);
            string backupPath = Path.Combine(backupDir, fileName);

            double fileSize = 0;
            string status = "Success";

            try
            {
                string dbPath = AppPaths.DatabaseFile;

                if (File.Exists(dbPath))
                {
                    File.Copy(dbPath, backupPath, overwrite: true);
                    fileSize = new FileInfo(backupPath).Length;
                }
                else
                {
                    status = "Warning_NoDB";
                    fileSize = 0;
                }
            }
            catch (Exception ex)
            {
                status = $"Error: {ex.Message}";
                fileSize = 0;
            }

            var log = new BackupLog
            {
                FileName = fileName,
                FileSizeBytes = fileSize,
                TriggeredBy = triggeredBy,
                Status = status,
                CreatedAt = DateTime.Now
            };

            _db.BackupLogs.Add(log);
            _db.SaveChanges();

            // S2-08: Ghi audit log
            AuditHelper.Log(_db, "Backup_Run", triggeredBy, $"Backup '{fileName}' — {status} ({fileSize:N0} bytes)");
        }
    }
}


