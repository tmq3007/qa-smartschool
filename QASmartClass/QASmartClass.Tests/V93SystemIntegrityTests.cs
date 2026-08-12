using System;
using System.IO;
using System.Linq;
using System.Threading;
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
                SystemIntegrityService.HealDatabase("6.04.0");

                // Verify that the corrupted database was moved to backup
                var corruptedFiles = Directory.GetFiles(AppPaths.DocumentsDir, "smartclass_corrupted_*.db");
                Assert.NotEmpty(corruptedFiles);

                // Verify that new database file exists and is healthy
                Assert.True(File.Exists(dbFile));
                
                // Try checking read integrity
                bool canOpen = false;
                var keyBytes = QASmartClass.Data.DbEncryptionKeyManager.GetOrInitializeKey();
                var hexKey = Convert.ToHexString(keyBytes);
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbFile};Password={hexKey};Foreign Keys=False"))
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
                    // Corrupt db file
                    File.WriteAllText(dbFile, "CORRUPTED DATA " + i);

                    // Execute healing
                    SystemIntegrityService.HealDatabase("6.04.0");
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
                SystemIntegrityService.HealDatabase("6.04.0");

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
    }
}
