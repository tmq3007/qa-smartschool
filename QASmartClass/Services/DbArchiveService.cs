using System;
using System.IO;
using System.Linq;
using System.Data;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class DbArchiveService
    {
        private readonly AppDbContext _db;

        public DbArchiveService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Predicts the number of days until the database size hits the limit.
        /// </summary>
        /// <param name="sizeHistory">Array of historical sizes in MB.</param>
        /// <param name="limitMb">Limits in MB.</param>
        /// <param name="mode">Linear, Exponential, or Disabled.</param>
        /// <returns>Number of days, or -1 if growth is negative/disabled.</returns>
        public double PredictDbGrowthDays(double[] sizeHistory, double limitMb, string mode)
        {
            if (string.Equals(mode, "Disabled", StringComparison.OrdinalIgnoreCase))
                return -1;

            if (sizeHistory == null || sizeHistory.Length < 2)
                return -1;

            double currentSize = sizeHistory.Last();
            if (currentSize >= limitMb)
                return 0;

            int n = sizeHistory.Length;
            double[] x = Enumerable.Range(0, n).Select(i => (double)i).ToArray();

            if (string.Equals(mode, "Exponential", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Fit ln(y) = a_exp * x + b_exp
                    double[] z = sizeHistory.Select(y => Math.Log(y > 0.01 ? y : 0.01)).ToArray();

                    double sumX = x.Sum();
                    double sumZ = z.Sum();
                    double sumXX = x.Sum(val => val * val);
                    double sumXZ = x.Zip(z, (xVal, zVal) => xVal * zVal).Sum();

                    double denom = n * sumXX - sumX * sumX;
                    if (Math.Abs(denom) < 1e-9) return -1;

                    double a_exp = (n * sumXZ - sumX * sumZ) / denom;
                    double b_exp = (sumZ - a_exp * sumX) / n;

                    if (a_exp <= 0) return -1; // Negative or flat growth

                    // ln(Limit) = a_exp * x + b_exp => x = (ln(Limit) - b_exp) / a_exp
                    double targetX = (Math.Log(limitMb) - b_exp) / a_exp;
                    double days = targetX - (n - 1);
                    return days < 0 ? 0 : days;
                }
                catch
                {
                    return -1;
                }
            }
            else // Default to Linear
            {
                try
                {
                    // Fit y = a * x + b
                    double sumX = x.Sum();
                    double sumY = sizeHistory.Sum();
                    double sumXX = x.Sum(val => val * val);
                    double sumXY = x.Zip(sizeHistory, (xVal, yVal) => xVal * yVal).Sum();

                    double denom = n * sumXX - sumX * sumX;
                    if (Math.Abs(denom) < 1e-9) return -1;

                    double a = (n * sumXY - sumX * sumY) / denom;
                    if (a <= 0) return -1; // Negative or flat growth

                    double days = (limitMb - currentSize) / a;
                    return days < 0 ? 0 : days;
                }
                catch
                {
                    return -1;
                }
            }
        }

        /// <summary>
        /// Moves AuditLogs older than retainDays to a separate archive SQLite DB.
        /// </summary>
        public bool ArchiveOldLogs(int retainDays)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-retainDays);
                var conn = _db.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }

                // Construct archive database path in the same directory
                string dbPath = AppPaths.DatabaseFile;
                string archivePath = Path.Combine(Path.GetDirectoryName(dbPath) ?? string.Empty, "smartclass_archive.db");

                using (var cmd = conn.CreateCommand())
                {
                    // 1. Attach the archive database
                    cmd.CommandText = $"ATTACH DATABASE '{archivePath}' AS archive;";
                    cmd.ExecuteNonQuery();

                    // 2. Create AuditLogs table in archive if not exists
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS archive.AuditLogs (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Action TEXT,
                            ActorName TEXT,
                            Details TEXT,
                            Timestamp TEXT,
                            RowHash TEXT
                        );";
                    cmd.ExecuteNonQuery();

                    // 3. Move old logs
                    cmd.CommandText = "INSERT OR IGNORE INTO archive.AuditLogs (Action, ActorName, Details, Timestamp, RowHash) " +
                                      "SELECT Action, ActorName, Details, Timestamp, RowHash FROM main.AuditLogs WHERE Timestamp < @cutoff;";
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@cutoff";
                    p.Value = cutoffDate.ToString("yyyy-MM-dd HH:mm:ss");
                    cmd.Parameters.Add(p);
                    cmd.ExecuteNonQuery();

                    // 4. Delete from main
                    cmd.CommandText = "DELETE FROM main.AuditLogs WHERE Timestamp < @cutoff;";
                    cmd.ExecuteNonQuery();

                    // 5. Detach database
                    cmd.CommandText = "DETACH DATABASE archive;";
                    cmd.ExecuteNonQuery();
                }

                // Run VACUUM to reclaim disk space
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "VACUUM;";
                    cmd.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Archiving failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verification Sandbox: Checks backup integrity and core tables.
        /// </summary>
        public bool VerifyBackupSandbox(string backupFilePath)
        {
            if (string.IsNullOrEmpty(backupFilePath) || !File.Exists(backupFilePath))
                return false;

            try
            {
                var keyBytes = QASmartClass.Data.DbEncryptionKeyManager.GetOrInitializeKey();
                var hexKey = Convert.ToHexString(keyBytes);
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite($"Data Source={backupFilePath};Password={hexKey};Pooling=False;")
                    .Options;

                using (var sandboxDb = new AppDbContext(options))
                {
                    var conn = sandboxDb.Database.GetDbConnection();
                    if (conn.State != ConnectionState.Open)
                    {
                        conn.Open();
                    }

                    // 1. Run PRAGMA integrity_check
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA integrity_check;";
                        var result = cmd.ExecuteScalar()?.ToString();
                        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }

                    // 2. Verify schema by selecting counts from core tables
                    int settingsCount = sandboxDb.SystemSettings.Count();
                    int studentsCount = sandboxDb.Students.Count();
                    int classroomsCount = sandboxDb.Classrooms.Count();

                    // If we can query these, the backup schema is valid
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
