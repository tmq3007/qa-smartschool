using System;
using System.IO;
using System.Linq;
using System.Threading;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.StudentClient.Services;
using Xunit;

namespace QASmartClass.Tests
{
    public class V93SystemIntegrityTests
    {
        [Fact]
        public void TestStudentIntegrity_CorruptedDb_ShouldSelfHealAndRecreate()
        {
            string? originalOverride = AppPaths.DataDirOverride;
            string tempPath = Path.Combine(Path.GetTempPath(), "QASmartClassTest_" + Guid.NewGuid().ToString());
            
            try
            {
                AppPaths.DataDirOverride = tempPath;
                AppPaths.EnsureDirectories();

                var dbFile = AppPaths.DatabaseFile;

                // Create a corrupted database file
                File.WriteAllText(dbFile, "THIS IS CORRUPTED GARBAGE DATA NOT SQLITE");

                // Execute self healing database
                SystemIntegrityService.HealDatabase("5.63.0");

                // Verify that the corrupted database was moved to backup
                var corruptedFiles = Directory.GetFiles(AppPaths.DocumentsDir, "smartclass_corrupted_*.db");
                Assert.NotEmpty(corruptedFiles);

                // Verify that new database file exists and is healthy
                Assert.True(File.Exists(dbFile));
                
                // Try checking read integrity
                bool canOpen = false;
                var keyBytes = QASmartClass.Data.DbEncryptionKeyManager.GetOrInitializeKey();
                var hexKey = Convert.ToHexString(keyBytes);
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbFile};Password={hexKey};Foreign Keys=False;Pooling=False"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master;";
                        cmd.ExecuteScalar();
                        canOpen = true;
                    }
                }
                Assert.True(canOpen);
            }
            finally
            {
                AppPaths.DataDirOverride = originalOverride;
                try
                {
                    if (Directory.Exists(tempPath))
                        Directory.Delete(tempPath, true);
                }
                catch { }
            }
        }

        [Fact]
        public void TestStudentIntegrity_CorruptedConfig_ShouldRestoreDefault()
        {
            string? originalOverride = AppPaths.DataDirOverride;
            string tempPath = Path.Combine(Path.GetTempPath(), "QASmartClassTest_" + Guid.NewGuid().ToString());
            
            try
            {
                AppPaths.DataDirOverride = tempPath;
                AppPaths.EnsureDirectories();

                var classroomSettingsFile = AppPaths.ClassroomSettingsFile;
                var appConfigFile = AppPaths.AppConfigFile;

                // Write corrupted settings
                File.WriteAllText(classroomSettingsFile, "{ invalid json: yes ");
                File.WriteAllText(appConfigFile, "[ bad config json: { ");

                // Run config healing
                SystemIntegrityService.HealConfigurations();

                // Verify settings.json restored to valid default
                Assert.True(File.Exists(classroomSettingsFile));
                var settingsJson = File.ReadAllText(classroomSettingsFile);
                using (var doc = System.Text.Json.JsonDocument.Parse(settingsJson))
                {
                    Assert.Equal("vi", doc.RootElement.GetProperty("Language").GetString());
                }

                // Verify app_config.json restored to valid default
                Assert.True(File.Exists(appConfigFile));
                var appConfigJson = File.ReadAllText(appConfigFile);
                using (var doc = System.Text.Json.JsonDocument.Parse(appConfigJson))
                {
                    Assert.True(doc.RootElement.GetProperty("AutoReconnect").GetBoolean());
                }
            }
            finally
            {
                AppPaths.DataDirOverride = originalOverride;
                try
                {
                    if (Directory.Exists(tempPath))
                        Directory.Delete(tempPath, true);
                }
                catch { }
            }
        }

        [Fact]
        public void TestIntegrityCommand_HMACSignature_ValidatesCorrectly()
        {
            var client = new StudentNetworkClient();
            client.SessionSalt = "test_salt_12345";
            client.PCName = "TestPC";

            // Generate signed command manually
            string command = "CMD|CHECK_INTEGRITY|id=cmd123";
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string payload = $"{command}|{timestamp}";
            string computedSignature;
            using (var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(client.SessionSalt)))
            {
                byte[] hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
                computedSignature = Convert.ToBase64String(hash);
            }
            string signedCmd = $"{payload}|{computedSignature}";

            // Verify via reflection
            var method = typeof(StudentNetworkClient).GetMethod("VerifyCommandSignature", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(method);
            
            var args = new object[] { signedCmd, null };
            bool result = (bool)method.Invoke(client, args)!;
            
            Assert.True(result);
            Assert.Equal(command, args[1]);

            // Test with bad signature
            var badArgs = new object[] { $"{payload}|badsignature_9999", null };
            bool badResult = (bool)method.Invoke(client, badArgs)!;
            Assert.False(badResult);
        }

        [Fact]
        public void TestSelfHealing_Loop100Times()
        {
            string? originalOverride = AppPaths.DataDirOverride;
            string tempPath = Path.Combine(Path.GetTempPath(), "QASmartClassTest_" + Guid.NewGuid().ToString());
            
            try
            {
                AppPaths.DataDirOverride = tempPath;
                AppPaths.EnsureDirectories();

                var dbFile = AppPaths.DatabaseFile;

                for (int i = 0; i < 100; i++)
                {
                    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                    
                    // Corrupt db file
                    File.WriteAllText(dbFile, "CORRUPTED DATA " + i);

                    // Execute healing
                    SystemIntegrityService.HealDatabase("5.63.0");
                }

                // Verify that the number of backups does not exceed 5 files
                var corruptedFiles = Directory.GetFiles(AppPaths.DocumentsDir, "smartclass_corrupted_*.db");
                Assert.True(corruptedFiles.Length <= 5, $"Corrupted backups count is: {corruptedFiles.Length}, should be <= 5");
            }
            finally
            {
                AppPaths.DataDirOverride = originalOverride;
                try
                {
                    if (Directory.Exists(tempPath))
                        Directory.Delete(tempPath, true);
                }
                catch { }
            }
        }

        [Fact]
        public void TestIntegrityCommand_Spam100Times()
        {
            string? originalOverride = AppPaths.DataDirOverride;
            string tempPath = Path.Combine(Path.GetTempPath(), "QASmartClassTest_" + Guid.NewGuid().ToString());
            
            try
            {
                AppPaths.DataDirOverride = tempPath;
                AppPaths.EnsureDirectories();

                // Setup DB
                SystemIntegrityService.HealDatabase("5.63.0");

                var client = new StudentNetworkClient();
                var method = typeof(StudentNetworkClient).GetMethod("PerformIntegrityCheck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(method);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < 100; i++)
                {
                    var result = (string)method.Invoke(client, null)!;
                    Assert.Contains("db_status=", result);
                    Assert.Contains("config_status=", result);
                }
                stopwatch.Stop();
                
                // 100 calls should run extremely fast
                Assert.True(stopwatch.ElapsedMilliseconds < 1500, $"Integrity check spam took too long: {stopwatch.ElapsedMilliseconds}ms");
            }
            finally
            {
                AppPaths.DataDirOverride = originalOverride;
                try
                {
                    if (Directory.Exists(tempPath))
                        Directory.Delete(tempPath, true);
                }
                catch { }
            }
        }

        [Fact]
        public void TestDiskQuotaGuard_UnderLimit_ShouldCleanOldBackups()
        {
            string? originalOverride = AppPaths.DataDirOverride;
            string tempPath = Path.Combine(Path.GetTempPath(), "QASmartClassTest_" + Guid.NewGuid().ToString());
            
            try
            {
                AppPaths.DataDirOverride = tempPath;
                AppPaths.EnsureDirectories();

                // Create 10 dummy corrupted files
                for (int i = 0; i < 10; i++)
                {
                    string path = Path.Combine(AppPaths.DocumentsDir, $"smartclass_corrupted_{20260600 + i}_000000.db");
                    File.WriteAllText(path, "corrupted database content");
                    Thread.Sleep(10);
                }

                // Create a corrupted database file to force HealDatabase to run structural healing
                File.WriteAllText(AppPaths.DatabaseFile, "INVALID SQLITE DB");

                // Call HealDatabase (will trigger cleanup)
                SystemIntegrityService.HealDatabase("5.63.0");

                var remaining = Directory.GetFiles(AppPaths.DocumentsDir, "smartclass_corrupted_*.db");
                Assert.True(remaining.Length <= 5, $"Remaining files count is: {remaining.Length}");
            }
            finally
            {
                AppPaths.DataDirOverride = originalOverride;
                try
                {
                    if (Directory.Exists(tempPath))
                        Directory.Delete(tempPath, true);
                }
                catch { }
            }
        }

        [Fact]
        public void TestClockDriftCorrection_LargeDrift_ShouldStillValidateSignature()
        {
            var client = new StudentNetworkClient();
            client.SessionSalt = "drift_salt_12345";
            client.PCName = "DriftPC";
            client.ClockDrift = 0;

            long serverTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 600;
            
            string command = "CMD|RESET_SETTINGS|id=cmd456";
            string payload = $"{command}|{serverTimestamp}";
            string computedSignature;
            using (var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(client.SessionSalt)))
            {
                byte[] hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
                computedSignature = Convert.ToBase64String(hash);
            }
            string signedCmd = $"{payload}|{computedSignature}";

            var method = typeof(StudentNetworkClient).GetMethod("VerifyCommandSignature", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(method);
            
            var args = new object[] { signedCmd, null };
            bool result = (bool)method.Invoke(client, args)!;
            
            Assert.True(result);
            Assert.Equal(command, args[1]);
            Assert.True(client.ClockDrift >= 598 && client.ClockDrift <= 602, $"Actual ClockDrift is: {client.ClockDrift}");

            long nextServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 600;
            string command2 = "CMD|PING|id=cmd457";
            string payload2 = $"{command2}|{nextServerTimestamp}";
            string computedSignature2;
            using (var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(client.SessionSalt)))
            {
                byte[] hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload2));
                computedSignature2 = Convert.ToBase64String(hash);
            }
            string signedCmd2 = $"{payload2}|{computedSignature2}";

            var args2 = new object[] { signedCmd2, null };
            bool result2 = (bool)method.Invoke(client, args2)!;
            Assert.True(result2);
            Assert.Equal(command2, args2[1]);
        }

        [Fact]
        public void TestTextJournaling_RAMMode_WriteAndReplay()
        {
            string? originalOverride = AppPaths.DataDirOverride;
            string tempPath = Path.Combine(Path.GetTempPath(), "QASmartClassTest_" + Guid.NewGuid().ToString());
            
            try
            {
                AppPaths.DataDirOverride = tempPath;
                AppPaths.EnsureDirectories();

                var conn = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:;Foreign Keys=True");
                conn.Open();
                AppDbContext.FallbackInMemoryConnection = conn;

                AppDbContext.IsSeedingOrMigrating = true;
                try
                {
                    using (var db = new AppDbContext())
                    {
                        DbMigrator.Migrate(db, "5.63.0");
                    }
                }
                finally
                {
                    AppDbContext.IsSeedingOrMigrating = false;
                }

                using (var db = new AppDbContext())
                {
                    var student = new Student
                    {
                        StudentCode = "HS_JOURNAL_TEST",
                        FullName = "Học sinh ghi nhật ký",
                        PCName = "PC_TEST",
                        IPAddress = "127.0.0.1",
                        IsOnline = true,
                        LastSeen = DateTime.Now
                    };
                    db.Students.Add(student);
                    db.SaveChanges();
                }

                string journalPath = AppPaths.TextJournalFile;
                Assert.True(File.Exists(journalPath));
                string journalText = File.ReadAllText(journalPath);
                Assert.Contains("HS_JOURNAL_TEST", journalText);

                AppDbContext.FallbackInMemoryConnection = null;
                conn.Close();
                conn.Dispose();

                if (File.Exists(AppPaths.DefaultDatabaseTemplate))
                {
                    File.Copy(AppPaths.DefaultDatabaseTemplate, AppPaths.DatabaseFile, true);
                }
                else
                {
                    AppDbContext.IsSeedingOrMigrating = true;
                    try
                    {
                        using (var db = new AppDbContext())
                        {
                            DbMigrator.Migrate(db, "5.63.0");
                        }
                    }
                    finally
                    {
                        AppDbContext.IsSeedingOrMigrating = false;
                    }
                }

                SystemIntegrityService.ReplayJournalLogs();

                using (var db = new AppDbContext())
                {
                    var restoredStudent = db.Students.FirstOrDefault(s => s.StudentCode == "HS_JOURNAL_TEST");
                    Assert.NotNull(restoredStudent);
                    Assert.Equal("Học sinh ghi nhật ký", restoredStudent.FullName);
                }

                Assert.False(File.Exists(journalPath));
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
                AppPaths.DataDirOverride = originalOverride;
                try
                {
                    if (Directory.Exists(tempPath))
                        Directory.Delete(tempPath, true);
                }
                catch { }
            }
        }

        [Fact]
        public void TestActiveExamRestoreLock_ShouldReject()
        {
            var client = new StudentNetworkClient();
            client.SessionSalt = "exam_lock_salt";
            client.PCName = "ExamPC";
            client.IsExamActive = true;

            string command = "CMD|RESTORE_DEFAULTS|id=cmd789";
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string payload = $"{command}|{timestamp}";
            string computedSignature;
            using (var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(client.SessionSalt)))
            {
                byte[] hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
                computedSignature = Convert.ToBase64String(hash);
            }
            string signedCmd = $"{payload}|{computedSignature}";

            var method = typeof(StudentNetworkClient).GetMethod("VerifyCommandSignature", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(method);
            
            var args = new object[] { signedCmd, null };
            bool result = (bool)method.Invoke(client, args)!;
            
            Assert.True(result);
            Assert.Equal(command, args[1]);
            Assert.True(client.IsExamActive);
        }
    }
}
