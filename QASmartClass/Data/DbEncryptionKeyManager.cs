using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using QASmartClass.Services;
using Serilog;

namespace QASmartClass.Data
{
    public static class DbEncryptionKeyManager
    {
        private static readonly byte[] Entropy = new byte[] { 0x51, 0x41, 0x5f, 0x53, 0x6d, 0x61, 0x72, 0x74, 0x43, 0x6c, 0x61, 0x73, 0x73, 0x5f, 0x4b, 0x65, 0x79 }; // "QA_SmartClass_Key" in ASCII
        private static readonly object LockObj = new object();
        private static byte[]? _cachedKey;

        public static byte[] GetOrInitializeKey()
        {
            lock (LockObj)
            {
                if (_cachedKey != null)
                {
                    return _cachedKey;
                }

                string dbFile = AppPaths.DatabaseFile;
                string keyFile = Path.ChangeExtension(dbFile, ".key");

                try
                {
                    if (File.Exists(keyFile))
                    {
                        byte[] encryptedBytes = File.ReadAllBytes(keyFile);
                        byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
                        
                        Log.Information("[DbKeyManager] Decrypted database key successfully using Windows DPAPI from {KeyFile}", keyFile);
                        _cachedKey = decryptedBytes;
                        return decryptedBytes;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[DbKeyManager] Failed to decrypt database key from {KeyFile}. Generating a new one to recover.", keyFile);
                }

                try
                {
                    byte[] rawKey = new byte[32]; // 256-bit AES key
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        rng.GetBytes(rawKey);
                    }

                    byte[] encryptedBytes = ProtectedData.Protect(rawKey, Entropy, DataProtectionScope.CurrentUser);
                    
                    string dir = Path.GetDirectoryName(keyFile);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    
                    File.WriteAllBytes(keyFile, encryptedBytes);
                    Log.Information("[DbKeyManager] Generated new database key and saved securely using Windows DPAPI to {KeyFile}", keyFile);
                    
                    _cachedKey = rawKey;
                    return rawKey;
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "[DbKeyManager] Critical error occurred while generating or saving database key.");
                    throw;
                }
            }
        }

        public static void EnsureDatabaseEncrypted()
        {
            lock (LockObj)
            {
                string dbFile = AppPaths.DatabaseFile;
                if (!File.Exists(dbFile))
                {
                    // No database exists, EF Core will create it encrypted on first use.
                    return;
                }

                // Check if the database is plaintext
                bool isPlaintext = false;
                try
                {
                    // Attempt connection with NO password
                    using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbFile};Foreign Keys=False;Pooling=False"))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT 1 FROM sqlite_master LIMIT 1;";
                            cmd.ExecuteScalar();
                        }
                    }
                    isPlaintext = true;
                    Log.Information("[DbKeyManager] Detected plaintext database at {DbFile}. Starting migration to encrypted format.", dbFile);
                }
                catch (Exception)
                {
                    // If it fails, it's either already encrypted or corrupt.
                    Log.Information("[DbKeyManager] Database at {DbFile} appears to be already encrypted or inaccessible.", dbFile);
                    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                }

                if (isPlaintext)
                {
                    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                    string tempDb = dbFile + ".temp_plaintext";
                    try
                    {
                        if (File.Exists(tempDb))
                        {
                            File.Delete(tempDb);
                        }
                        File.Move(dbFile, tempDb);

                        var keyBytes = GetOrInitializeKey();
                        var hexKey = Convert.ToHexString(keyBytes);

                        // Open plaintext database, attach new encrypted database, and export
                        using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={tempDb};Foreign Keys=False"))
                        {
                            conn.Open();
                            using (var cmd = conn.CreateCommand())
                            {
                                // Attach new DB
                                cmd.CommandText = $"ATTACH DATABASE '{dbFile}' AS encrypted KEY '{hexKey}';";
                                cmd.ExecuteNonQuery();

                                // Export schema and data
                                cmd.CommandText = "SELECT sqlcipher_export('encrypted');";
                                cmd.ExecuteNonQuery();

                                // Detach
                                cmd.CommandText = "DETACH DATABASE encrypted;";
                                cmd.ExecuteNonQuery();
                            }
                        }

                        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                        File.Delete(tempDb);
                        Log.Information("[DbKeyManager] Database successfully migrated to encrypted format using SQLCipher at {DbFile}", dbFile);
                    }
                    catch (Exception ex)
                    {
                        Log.Fatal(ex, "[DbKeyManager] Critical failure during database encryption migration!");
                        // Rollback if possible
                        if (File.Exists(tempDb) && !File.Exists(dbFile))
                        {
                            File.Move(tempDb, dbFile);
                        }
                        throw;
                    }
                }
            }
        }

        public static void ClearCache()
        {
            lock (LockObj)
            {
                _cachedKey = null;
            }
        }
    }
}
