using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QASmartClass.Data;
using QASmartClass.Services;
using Xunit;

namespace QASmartClass.Tests
{
    public class AuditHelperTests
    {
        [Fact]
        public void TestAuditHelper_WritesWithHashChain_Verify()
        {
            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();

                string action1 = "Test_Action_1";
                string actor1 = "Tester_1";
                string details1 = "Details_1";

                string action2 = "Test_Action_2";
                string actor2 = "Tester_2";
                string details2 = "Details_2";

                // Clear logs for testing
                db.AuditLogs.RemoveRange(db.AuditLogs);
                db.SaveChanges();

                // Act - write first log
                AuditHelper.Log(db, action1, actor1, details1);
                
                // Assert first log in DB
                var log1 = db.AuditLogs.FirstOrDefault(l => l.Action == action1);
                Assert.NotNull(log1);
                Assert.False(string.IsNullOrEmpty(log1.RowHash));

                // Act - write second log
                AuditHelper.Log(db, action2, actor2, details2);

                // Assert second log in DB
                var log2 = db.AuditLogs.FirstOrDefault(l => l.Action == action2);
                Assert.NotNull(log2);
                Assert.False(string.IsNullOrEmpty(log2.RowHash));
                Assert.NotEqual(log1.RowHash, log2.RowHash);

                // Act - check integrity
                bool isIntegrityValid = AuditHelper.VerifyIntegrity(out string errorDetails);
                Assert.True(isIntegrityValid, $"Integrity check failed: {errorDetails}");
            }
        }

        [Fact]
        public void TestAuditHelper_VerifyIntegrity_DetectsTampering()
        {
            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
                db.AuditLogs.RemoveRange(db.AuditLogs);
                db.SaveChanges();

                // Act - Log some entries
                AuditHelper.Log(db, "SecureAction_A", "Admin", "Safe details A");
                AuditHelper.Log(db, "SecureAction_B", "Admin", "Safe details B");

                // Verify initially valid
                bool isInitiallyValid = AuditHelper.VerifyIntegrity(out _);
                Assert.True(isInitiallyValid);

                // Tamper with database record B
                var logB = db.AuditLogs.FirstOrDefault(l => l.Action == "SecureAction_B");
                Assert.NotNull(logB);
                logB.Details = "TAMPERED DETAILS"; // Alter the log
                db.SaveChanges();

                // Act - Check integrity again
                bool isAfterTamperingValid = AuditHelper.VerifyIntegrity(out string errorDetails);
                
                // Assert - Tampering must be detected
                Assert.False(isAfterTamperingValid);
                Assert.Contains("SecureAction_B", errorDetails);
            }
        }

        [Fact]
        public async Task TestAuditHelper_ConcurrencyStress_Verify()
        {
            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
                db.AuditLogs.RemoveRange(db.AuditLogs);
                db.SaveChanges();
            }

            int concurrentLogsCount = 20;
            Task[] tasks = new Task[concurrentLogsCount];

            // Act - Write logs concurrently from 20 separate threads/tasks
            for (int i = 0; i < concurrentLogsCount; i++)
            {
                int index = i;
                tasks[i] = Task.Run(() =>
                {
                    using (var db = new AppDbContext())
                    {
                        AuditHelper.Log(db, $"ConcurrentAction_{index}", "StressTester", $"Stress log details {index}");
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Assert
            using (var db = new AppDbContext())
            {
                var logsCount = db.AuditLogs.Count(l => l.ActorName == "StressTester");
                Assert.Equal(concurrentLogsCount, logsCount);

                bool integrity = AuditHelper.VerifyIntegrity(out _);
                Assert.True(integrity);
            }
        }

        [Fact]
        public void TestAuditHelper_DpapiEncryption_EncryptDecryptSuccess()
        {
            string secret = "TestSecretKey_123456";
            string encrypted = DpapiHelper.Encrypt(secret);
            Assert.False(string.IsNullOrEmpty(encrypted));
            Assert.NotEqual(secret, encrypted);

            string decrypted = DpapiHelper.Decrypt(encrypted);
            Assert.Equal(secret, decrypted);
        }

        [Fact]
        public void TestAuditHelper_LogRotation_Success()
        {
            string testLogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_logs");
            if (Directory.Exists(testLogDir))
            {
                Directory.Delete(testLogDir, true);
            }
            Directory.CreateDirectory(testLogDir);

            try
            {
                string mainLog = Path.Combine(testLogDir, "audit.log");
                File.WriteAllText(mainLog, "Main Log Content");

                // Create some old logs
                File.WriteAllText(Path.Combine(testLogDir, "audit.1.log"), "Log 1");
                File.WriteAllText(Path.Combine(testLogDir, "audit.2.log"), "Log 2");
                File.WriteAllText(Path.Combine(testLogDir, "audit.3.log"), "Log 3");
                File.WriteAllText(Path.Combine(testLogDir, "audit.4.log"), "Log 4");
                File.WriteAllText(Path.Combine(testLogDir, "audit.5.log"), "Log 5");

                // Invoke RotateLogs using reflection
                var method = typeof(AuditHelper).GetMethod("RotateLogs", 
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                Assert.NotNull(method);

                method.Invoke(null, new object[] { testLogDir, mainLog });

                // Verify files rotated:
                Assert.False(File.Exists(mainLog));
                Assert.True(File.Exists(Path.Combine(testLogDir, "audit.1.log")));
                Assert.Equal("Main Log Content", File.ReadAllText(Path.Combine(testLogDir, "audit.1.log")));
                Assert.Equal("Log 1", File.ReadAllText(Path.Combine(testLogDir, "audit.2.log")));
                Assert.Equal("Log 2", File.ReadAllText(Path.Combine(testLogDir, "audit.3.log")));
                Assert.Equal("Log 3", File.ReadAllText(Path.Combine(testLogDir, "audit.4.log")));
                Assert.Equal("Log 4", File.ReadAllText(Path.Combine(testLogDir, "audit.5.log")));
            }
            finally
            {
                if (Directory.Exists(testLogDir))
                {
                    Directory.Delete(testLogDir, true);
                }
            }
        }
    }
}
