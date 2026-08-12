using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using Xunit;

namespace QASmartClass.Tests
{
    public class DatabaseEncryptionTests : IDisposable
    {
        private readonly string _dbFile;
        private readonly string _keyFile;

        public DatabaseEncryptionTests()
        {
            AppPaths.EnsureDirectories();
            string uniqueId = Guid.NewGuid().ToString("N");
            _dbFile = Path.Combine(AppPaths.RootDir, $"test_encrypted_db_{uniqueId}.db");
            _keyFile = Path.ChangeExtension(_dbFile, ".key");

            // Override AppPaths database path for this test run
            AppPaths.DatabaseFile = _dbFile;
            
            // Clear any cached key to force generating a fresh key for this test database
            DbEncryptionKeyManager.ClearCache();
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            try { if (File.Exists(_dbFile)) File.Delete(_dbFile); } catch { }
            try { if (File.Exists(_keyFile)) File.Delete(_keyFile); } catch { }
            try { if (File.Exists(_dbFile + "-wal")) File.Delete(_dbFile + "-wal"); } catch { }
            try { if (File.Exists(_dbFile + "-shm")) File.Delete(_dbFile + "-shm"); } catch { }
        }

        [Fact]
        public void TC01_DatabaseFileIsEncrypted_StandardConnectionThrows()
        {
            // Initialize database using EF Core (which has SQLCipher password configured)
            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
                // Add a dummy record to verify
                db.EventLogs.Add(new EventLog
                {
                    EventType = "SECURITY_TEST",
                    Actor = "TESTER",
                    Details = "SQLCipher Validation",
                    Timestamp = DateTime.Now
                });
                db.SaveChanges();
            }

            Assert.True(File.Exists(_dbFile), "Database file should exist.");

            // Attempt to open the database file using an unencrypted connection (NO password)
            var connStr = $"Data Source={_dbFile};Foreign Keys=False";
            var ex = Assert.Throws<SqliteException>(() =>
            {
                using (var conn = new SqliteConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM EventLogs;";
                        cmd.ExecuteScalar();
                    }
                }
            });

            // SQLCipher throws "file is not a database" (error code 11) when it cannot decrypt the database header
            Assert.Contains("is not a database", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TC02_DatabaseFileCanBeOpened_WithCorrectKey()
        {
            byte[] keyBytes = DbEncryptionKeyManager.GetOrInitializeKey();
            string hexKey = Convert.ToHexString(keyBytes);

            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
                db.EventLogs.Add(new EventLog
                {
                    EventType = "KEY_TEST",
                    Actor = "TESTER",
                    Details = "Mã hóa mở khóa thành công",
                    Timestamp = DateTime.Now
                });
                db.SaveChanges();
            }

            // Attempt to open using correct hexadecimal password
            var connStr = $"Data Source={_dbFile};Password={hexKey};Foreign Keys=False";
            using (var conn = new SqliteConnection(connStr))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT EventType FROM EventLogs LIMIT 1;";
                    var result = cmd.ExecuteScalar() as string;
                    Assert.Equal("KEY_TEST", result);
                }
            }
        }

        [Fact]
        public void TC03_DpapiEncryptionDecryption_Works()
        {
            byte[] key = DbEncryptionKeyManager.GetOrInitializeKey();
            Assert.NotNull(key);
            Assert.Equal(32, key.Length); // 256-bit

            Assert.True(File.Exists(_keyFile), "Key file must be created on disk.");

            // Verify the key file is encrypted using Windows DPAPI
            byte[] encryptedBytes = File.ReadAllBytes(_keyFile);
            
            // Check that it's not plaintext
            Assert.NotEqual(key, encryptedBytes);

            // Decrypt manually using DPAPI to verify it matches
            byte[] entropy = new byte[] { 0x51, 0x41, 0x5f, 0x53, 0x6d, 0x61, 0x72, 0x74, 0x43, 0x6c, 0x61, 0x73, 0x73, 0x5f, 0x4b, 0x65, 0x79 };
            byte[] decrypted = ProtectedData.Unprotect(encryptedBytes, entropy, DataProtectionScope.CurrentUser);
            Assert.Equal(key, decrypted);
        }

        [Fact]
        public void TC04_InPlaceMigration_EncryptsPlaintextDatabase()
        {
            // 1. Create a plaintext database file
            var connStrPlain = $"Data Source={_dbFile};Foreign Keys=False";
            using (var conn = new SqliteConnection(connStrPlain))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE DummyTable (Id INTEGER PRIMARY KEY, Val TEXT);";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "INSERT INTO DummyTable (Id, Val) VALUES (123, 'PlaintextData');";
                    cmd.ExecuteNonQuery();
                }
            }

            // Verify we can read it without password
            using (var conn = new SqliteConnection(connStrPlain))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Val FROM DummyTable WHERE Id = 123;";
                    var val = cmd.ExecuteScalar() as string;
                    Assert.Equal("PlaintextData", val);
                }
            }

            // Clear pool locks on test connection
            SqliteConnection.ClearAllPools();

            // 2. Run the migration
            DbEncryptionKeyManager.EnsureDatabaseEncrypted();

            // 3. Verify it is now encrypted (opening without password fails)
            var ex = Assert.Throws<SqliteException>(() =>
            {
                using (var conn = new SqliteConnection(connStrPlain))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Val FROM DummyTable WHERE Id = 123;";
                        cmd.ExecuteScalar();
                    }
                }
            });
            Assert.Contains("is not a database", ex.Message, StringComparison.OrdinalIgnoreCase);

            // 4. Verify we can open it with the password, and data is preserved
            byte[] keyBytes = DbEncryptionKeyManager.GetOrInitializeKey();
            string hexKey = Convert.ToHexString(keyBytes);
            var connStrEncrypted = $"Data Source={_dbFile};Password={hexKey};Foreign Keys=False";

            using (var conn = new SqliteConnection(connStrEncrypted))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Val FROM DummyTable WHERE Id = 123;";
                    var val = cmd.ExecuteScalar() as string;
                    Assert.Equal("PlaintextData", val);
                }
            }
        }
    }
}
