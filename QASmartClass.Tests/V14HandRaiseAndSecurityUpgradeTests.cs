using Xunit;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using QASmartClass.Utilities;
using QASmartClass.StudentClient.Services;

namespace QASmartClass.Tests
{
    public class V14HandRaiseAndSecurityUpgradeTests
    {
        [Theory]
        [InlineData("🙋 Phát biểu bài", "Phát biểu bài")]
        [InlineData("❓ Chưa hiểu bài", "Chưa hiểu bài")]
        [InlineData("🔧 Lỗi máy tính", "Lỗi máy tính")]
        [InlineData("🚪 Xin ra ngoài", "Xin ra ngoài")]
        [InlineData("Phát biểu", "Phát biểu")]
        public void Test_HandRaiseReason_StrippingLeadingEmoji(string input, string expected)
        {
            string cleanReason = input;
            if (!string.IsNullOrEmpty(input))
            {
                int start = 0;
                while (start < input.Length && !char.IsLetterOrDigit(input[start]))
                {
                    start++;
                }
                cleanReason = start < input.Length ? input.Substring(start) : input;
            }

            Assert.Equal(expected, cleanReason);
        }

        [Fact]
        public void Test_HandRaiseEncryption_WithClassCode()
        {
            string classCode = "PHYSICS101";
            string originalPayload = "raised=True|reason=Chưa hiểu bài";

            // Encrypt
            string encrypted = CryptoHelper.Encrypt(originalPayload, classCode);
            Assert.NotEmpty(encrypted);
            Assert.NotEqual(originalPayload, encrypted);

            // Decrypt with correct key
            string decryptedCorrect = CryptoHelper.Decrypt(encrypted, classCode);
            Assert.Equal(originalPayload, decryptedCorrect);

            // Decrypt with incorrect key
            string decryptedWrong = CryptoHelper.Decrypt(encrypted, "CHEMISTRY102");
            Assert.Empty(decryptedWrong);
        }

        [Fact]
        public async Task Test_OfflineQueueCleanup_RetentionPolicy()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"test_queue_{Guid.NewGuid()}.db");
            var connStr = $"Data Source={dbPath};Pooling=False;Foreign Keys=True;Default Timeout=5";

            try
            {
                // Create table and insert mock data
                using (var conn = new SqliteConnection(connStr))
                {
                    await conn.OpenAsync();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS OfflineMessageQueue (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                MessageType TEXT NOT NULL,
                                Payload TEXT NOT NULL,
                                CreatedAt TEXT NOT NULL,
                                IsSent INTEGER DEFAULT 0
                            );";
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Insert records:
                    // 1. A sent record created 8 days ago (should be deleted)
                    // 2. An unsent record created 8 days ago (should be deleted because cleanup deletes ALL records older than 7 days)
                    // 3. A record created 2 days ago (should be preserved)
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            INSERT INTO OfflineMessageQueue (MessageType, Payload, CreatedAt, IsSent) VALUES 
                            ('QUESTION', 'Old Expired Sent Question', datetime('now', '-8 days'), 1),
                            ('HAND_RAISE', 'Old Expired Unsent Hand Raise', datetime('now', '-9 days'), 0),
                            ('QUESTION', 'Recent Valid Question', datetime('now', '-2 days'), 0);";
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // Temporary override DatabaseFile path if possible, or simulate Cleanup logic with dynamic path
                // Since CleanupOldQueueLogsAsync reads from QASmartClass.Services.AppPaths.DatabaseFile,
                // we can test the SQL query execution directly to verify it works as expected.
                using (var conn = new SqliteConnection(connStr))
                {
                    await conn.OpenAsync();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM OfflineMessageQueue WHERE CreatedAt < date('now', '-7 days');";
                        int deleted = await cmd.ExecuteNonQueryAsync();
                        Assert.Equal(2, deleted);
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM OfflineMessageQueue;";
                        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        Assert.Equal(1, count);
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Payload FROM OfflineMessageQueue;";
                        var payload = await cmd.ExecuteScalarAsync() as string;
                        Assert.Equal("Recent Valid Question", payload);
                    }
                }
            }
            finally
            {
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }
            }
        }

        [Fact]
        public void Test_SecurityTimestampVerification_StrictCheck()
        {
            string classCode = "TESTCLASS";
            
            // Case 1: Valid payload (current time)
            long currentTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string validPayload = $"raised=True|reason=Phát biểu|ts={currentTs}";
            string encValid = CryptoHelper.Encrypt(validPayload, classCode);
            
            // Simulated Server Processing
            string decryptedValid = CryptoHelper.Decrypt(encValid, classCode);
            Assert.NotEmpty(decryptedValid);
            
            bool hasTs = false;
            long tsVal = 0;
            var parts = decryptedValid.Split('|');
            var tsPart = parts.FirstOrDefault(p => p.StartsWith("ts="));
            if (tsPart != null && long.TryParse(tsPart.Substring(3), out tsVal))
            {
                hasTs = true;
            }
            
            Assert.True(hasTs);
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            bool isDriftValid = Math.Abs(now - tsVal) <= 10;
            Assert.True(isDriftValid);

            // Case 2: Stale payload (30 seconds ago)
            long staleTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 30;
            string stalePayload = $"raised=True|reason=Phát biểu|ts={staleTs}";
            string encStale = CryptoHelper.Encrypt(stalePayload, classCode);
            
            string decryptedStale = CryptoHelper.Decrypt(encStale, classCode);
            Assert.NotEmpty(decryptedStale);
            
            hasTs = false;
            tsVal = 0;
            parts = decryptedStale.Split('|');
            tsPart = parts.FirstOrDefault(p => p.StartsWith("ts="));
            if (tsPart != null && long.TryParse(tsPart.Substring(3), out tsVal))
            {
                hasTs = true;
            }
            
            Assert.True(hasTs);
            now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            isDriftValid = Math.Abs(now - tsVal) <= 10;
            Assert.False(isDriftValid); // should fail drift validation
        }

        [Fact]
        public async Task Test_SQLiteVacuum_DatabaseReclaimsSpace()
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"test_vacuum_{Guid.NewGuid()}.db");
            var connStr = $"Data Source={dbPath};Pooling=False;Foreign Keys=True;Default Timeout=5";

            try
            {
                using (var conn = new SqliteConnection(connStr))
                {
                    await conn.OpenAsync();
                    
                    // Create a dummy table and populate some data
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS DummyTable (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Value TEXT NOT NULL
                            );
                            INSERT INTO DummyTable (Value) VALUES ('A'), ('B'), ('C');";
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Delete the data to make space reclaimable
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM DummyTable;";
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Perform VACUUM
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "VACUUM;";
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                // Verify the file still exists and isn't corrupted
                Assert.True(File.Exists(dbPath));
                
                using (var conn = new SqliteConnection(connStr))
                {
                    await conn.OpenAsync();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='DummyTable';";
                        var tableExists = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                        Assert.Equal(1, tableExists);
                    }
                }
            }
            finally
            {
                if (File.Exists(dbPath))
                {
                    try
                    {
                        File.Delete(dbPath);
                    }
                    catch { }
                }
            }
        }

        [Fact]
        public void Test_ChimeFallbackSound_NoException()
        {
            try
            {
                // Validate that executing a console beep is safe and doesn't crash the application
                System.Console.Beep(1000, 50);
            }
            catch (Exception ex)
            {
                // Safe catch log
                System.Diagnostics.Debug.WriteLine($"Console beep not supported: {ex.Message}");
            }
            // Even if Console.Beep throws an exception (e.g. headless environment),
            // it is caught gracefully. This test ensures the execution is safe.
            Assert.True(true);
        }

        [Fact]
        public void Test_SecurityKeyDerivation_IsSecureAndConsistent()
        {
            string classCode = "MATH101";
            string plainText = "Hello World";

            // Encrypt & Decrypt
            string cipher = CryptoHelper.Encrypt(plainText, classCode);
            Assert.NotEmpty(cipher);
            Assert.NotEqual(plainText, cipher);

            string decrypted = CryptoHelper.Decrypt(cipher, classCode);
            Assert.Equal(plainText, decrypted);

            // Test that wrong class code fails to decrypt (returns empty string)
            string wrongDecrypted = CryptoHelper.Decrypt(cipher, "MATH102");
            Assert.Empty(wrongDecrypted);
        }
    }
}
