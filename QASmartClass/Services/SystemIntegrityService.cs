using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public static class SystemIntegrityService
    {
        public static void HealConfigurations()
        {
            try
            {
                string classroomSettingsFile = AppPaths.ClassroomSettingsFile;
                if (File.Exists(classroomSettingsFile))
                {
                    try
                    {
                        var json = File.ReadAllText(classroomSettingsFile);
                        using (var doc = System.Text.Json.JsonDocument.Parse(json)) { }
                    }
                    catch (Exception)
                    {
                        Log.Warning("settings.json bị hỏng cú pháp. Phục hồi cấu hình mặc định...");
                        File.Delete(classroomSettingsFile);
                        File.WriteAllText(classroomSettingsFile, "{\"Language\": \"vi\"}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Không thể phục hồi settings.json: {Err}", ex.Message);
            }

            try
            {
                string appConfigFile = AppPaths.AppConfigFile;
                if (File.Exists(appConfigFile))
                {
                    try
                    {
                        var json = File.ReadAllText(appConfigFile);
                        using (var doc = System.Text.Json.JsonDocument.Parse(json)) { }
                    }
                    catch (Exception)
                    {
                        Log.Warning("app_config.json bị hỏng. Phục hồi cấu hình mặc định...");
                        var defaultCfg = new AppConfig();
                        defaultCfg.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Không thể phục hồi app_config.json: {Err}", ex.Message);
            }
        }

        public static void HealDatabase(string currentDbVersion)
        {
            var dbPath = AppPaths.DatabaseFile;
            
            // ═══ TỰ ĐỘNG SAO CHÉP & ĐỒNG BỘ CSDL KHI ĐÓNG GÓI/PHÁT HÀNH ═══
            EnsureBundledDatabaseCopied(dbPath);
            
            // Check if db file is locked
            bool isDbLocked = false;
            try
            {
                if (File.Exists(dbPath))
                {
                    using (var fs = new FileStream(dbPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        // File is not locked
                    }
                }
            }
            catch (IOException)
            {
                isDbLocked = true;
            }

            if (isDbLocked)
            {
                Log.Warning("CSDL đang bị khóa bởi tiến trình khác. Kích hoạt chế độ CSDL tạm thời trên RAM (Read-Only)...");
                var conn = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:;Foreign Keys=True");
                conn.Open();
                AppDbContext.FallbackInMemoryConnection = conn;

                AppDbContext.IsSeedingOrMigrating = true;
                try
                {
                    using (var tempDb = new AppDbContext())
                    {
                        var tempRoster = new QASmartClass.Classroom.Services.ClassRosterService(tempDb);
                        DbMigrator.Migrate(tempDb, currentDbVersion);
                        DatabaseSeeder.SeedIfEmpty(tempDb);
                        SampleDataSeeder.SeedIfEmpty(tempDb, tempRoster);
                        QASmartClass.Staff.Services.StaffDataSeeder.SeedAll(tempDb);
                    }
                }
                finally
                {
                    AppDbContext.IsSeedingOrMigrating = false;
                }

                // === UPGRADE_07: Start Background Database Lock Release Monitor ===
                StartDatabaseRecoveryMonitor(currentDbVersion);
            }
            else
            {
                // Level 1 structural check
                bool needHealing = false;
                if (File.Exists(dbPath))
                {
                    try
                    {
                        DbEncryptionKeyManager.EnsureDatabaseEncrypted();
                        var keyBytes = DbEncryptionKeyManager.GetOrInitializeKey();
                        var hexKey = Convert.ToHexString(keyBytes);
                        using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath};Password={hexKey};Foreign Keys=False;Pooling=False"))
                        {
                            conn.Open();
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master;";
                                cmd.ExecuteScalar();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "CSDL bị lỗi vật lý hoặc hỏng cấu trúc. Đánh dấu cần tự phục hồi...");
                        needHealing = true;
                        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                    }
                }

                if (needHealing)
                {
                    // Clean up old .corrupted backup files, keeping at most 5 files to avoid full disk
                    // Disk Quota check: if free space < 200MB, delete ALL old corrupted backups
                    try
                    {
                        var dir = AppPaths.DocumentsDir;
                        if (Directory.Exists(dir))
                        {
                            var drive = new DriveInfo(Path.GetPathRoot(dir)!);
                            bool lowDisk = drive.AvailableFreeSpace < 200L * 1024L * 1024L;

                            var corruptedFiles = Directory.GetFiles(dir, "smartclass_corrupted_*.db")
                                .Select(f => new FileInfo(f))
                                .OrderByDescending(f => f.CreationTime)
                                .ToList();

                            int maxAllowed = lowDisk ? 0 : 4; // if low disk, delete all, keeping only the new one about to be created
                            if (corruptedFiles.Count > maxAllowed)
                            {
                                foreach (var oldFile in corruptedFiles.Skip(maxAllowed))
                                {
                                    try { oldFile.Delete(); } catch { }
                                }
                            }
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        Log.Warning("Failed to clean up old corrupted databases: {Err}", cleanupEx.Message);
                    }

                    try
                    {
                        string corruptedPath = Path.Combine(AppPaths.DocumentsDir, $"smartclass_corrupted_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                        if (File.Exists(dbPath))
                        {
                            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            File.Move(dbPath, corruptedPath);
                            Log.Information("Đã di chuyển file CSDL lỗi sang: {CorruptedPath}", corruptedPath);
                        }

                        if (File.Exists(AppPaths.DefaultDatabaseTemplate))
                        {
                            File.Copy(AppPaths.DefaultDatabaseTemplate, dbPath, true);
                            Log.Information("Đã khôi phục CSDL từ template mặc định.");
                        }
                        else
                        {
                            Log.Information("Không tìm thấy template mặc định. Sẽ tạo CSDL mới hoàn toàn...");
                        }
                    }
                    catch (Exception healEx)
                    {
                        Log.Error(healEx, "Không thể phục hồi CSDL tự động.");
                    }
                }

                DbEncryptionKeyManager.EnsureDatabaseEncrypted();

                // Replay logs first!
                ReplayJournalLogs();

                // Physical backup scheduled in background to avoid delaying startup
                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(3000);
                        var backupFolder = AppPaths.BackupsDir;
                        Directory.CreateDirectory(backupFolder);
                        var backupFile = Path.Combine(backupFolder, $"smartclass_backup_{DateTime.Now:yyyyMMdd}.db");
                        if (File.Exists(dbPath) && !File.Exists(backupFile))
                        {
                            using (var srcStream = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (var dstStream = new FileStream(backupFile, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                await srcStream.CopyToAsync(dstStream);
                            }
                            Log.Information("Database backed up in background to: {BackupPath}", backupFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Background database backup failed: {Error}", ex.Message);
                    }
                });

                // Migrate and seed
                AppDbContext.IsSeedingOrMigrating = true;
                try
                {
                    using (var tempDb = new AppDbContext())
                    {
                        var tempRoster = new QASmartClass.Classroom.Services.ClassRosterService(tempDb);
                        DbMigrator.Migrate(tempDb, currentDbVersion);
                        DatabaseSeeder.SeedIfEmpty(tempDb);
                        SampleDataSeeder.SeedIfEmpty(tempDb, tempRoster);
                        QASmartClass.Staff.Services.StaffDataSeeder.SeedAll(tempDb);
                    }
                }
                finally
                {
                    AppDbContext.IsSeedingOrMigrating = false;
                }
            }
        }

        public static void ReplayJournalLogs()
        {
            string journalPath = AppPaths.TextJournalFile;
            if (!File.Exists(journalPath)) return;

            Log.Information("Tìm thấy nhật ký dự phòng. Đang phát lại dữ liệu...");
            try
            {
                var lines = File.ReadAllLines(journalPath);
                if (lines.Length == 0)
                {
                    File.Delete(journalPath);
                    return;
                }

                using (var db = new AppDbContext())
                {
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        System.Collections.Generic.List<AppDbContext.JournalEntry>? entries = null;
                        try
                        {
                            entries = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<AppDbContext.JournalEntry>>(line);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Lỗi cú pháp khi đọc dòng nhật ký.");
                            continue;
                        }

                        if (entries == null) continue;

                        foreach (var entry in entries)
                        {
                            try
                            {
                                var type = Type.GetType(entry.EntityType) ?? 
                                           AppDomain.CurrentDomain.GetAssemblies()
                                              .Select(a => a.GetType(entry.EntityType))
                                              .FirstOrDefault(t => t != null);

                                if (type == null)
                                {
                                    Log.Warning("Không tìm thấy kiểu thực thể: {EntityType}", entry.EntityType);
                                    continue;
                                }

                                if (entry.Action == "Added")
                                {
                                    var entity = Activator.CreateInstance(type);
                                    if (entity == null) continue;

                                    PopulateEntity(entity, type, entry.PropertyValues);
                                    
                                    var keysToPopulate = new System.Collections.Generic.Dictionary<string, object>();
                                    foreach (var kvp in entry.KeyValues)
                                    {
                                        if (kvp.Key.Equals("Id", StringComparison.OrdinalIgnoreCase)) continue;
                                        keysToPopulate[kvp.Key] = kvp.Value;
                                    }
                                    PopulateEntity(entity, type, keysToPopulate);
                                    
                                    db.Add(entity);
                                }
                                else if (entry.Action == "Modified")
                                {
                                    var entity = FindEntity(db, type, entry.KeyValues);
                                    if (entity != null)
                                    {
                                        PopulateEntity(entity, type, entry.PropertyValues);
                                        db.Update(entity);
                                    }
                                }
                                else if (entry.Action == "Deleted")
                                {
                                    var entity = FindEntity(db, type, entry.KeyValues);
                                    if (entity != null)
                                    {
                                        db.Remove(entity);
                                    }
                                }
                            }
                            catch (Exception entryEx)
                            {
                                Log.Error(entryEx, "Không thể xử lý dòng nhật ký cho thực thể {EntityType}", entry.EntityType);
                            }
                        }
                    }
                    try
                    {
                        db.SaveChanges();
                    }
                    catch (Exception saveEx)
                    {
                        Log.Error(saveEx, "Lỗi SaveChanges khi Replay");
                        throw;
                    }
                }

                File.Delete(journalPath);
                Log.Information("Phát lại nhật ký dự phòng hoàn tất. Đã đồng bộ dữ liệu vật lý.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi chạy ReplayJournalLogs.");
            }
        }

        private static void PopulateEntity(object entity, Type type, System.Collections.Generic.Dictionary<string, object> values)
        {
            if (values == null) return;
            foreach (var kvp in values)
            {
                var prop = type.GetProperty(kvp.Key);
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        var rawVal = kvp.Value;
                        if (rawVal is System.Text.Json.JsonElement elem)
                        {
                            object? val = null;
                            if (elem.ValueKind == System.Text.Json.JsonValueKind.Null)
                            {
                                val = null;
                            }
                            else if (prop.PropertyType == typeof(string))
                            {
                                val = elem.GetString();
                            }
                            else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                            {
                                val = elem.GetInt32();
                            }
                            else if (prop.PropertyType == typeof(double) || prop.PropertyType == typeof(double?))
                            {
                                val = elem.GetDouble();
                            }
                            else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
                            {
                                val = elem.GetBoolean();
                            }
                            else if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                            {
                                val = DateTime.Parse(elem.GetString()!);
                            }
                            else if (prop.PropertyType == typeof(Guid) || prop.PropertyType == typeof(Guid?))
                            {
                                val = Guid.Parse(elem.GetString()!);
                            }
                            else if (prop.PropertyType == typeof(byte[]))
                            {
                                val = Convert.FromBase64String(elem.GetString()!);
                            }
                            else
                            {
                                val = System.Text.Json.JsonSerializer.Deserialize(elem.GetRawText(), prop.PropertyType);
                            }
                            prop.SetValue(entity, val);
                        }
                        else
                        {
                            var convertedVal = Convert.ChangeType(rawVal, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                            prop.SetValue(entity, convertedVal);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Không thể map thuộc tính {Prop} trên thực thể: {Err}", kvp.Key, ex.Message);
                    }
                }
            }
        }

        private static object? FindEntity(AppDbContext db, Type type, System.Collections.Generic.Dictionary<string, object> keys)
        {
            if (keys == null || keys.Count == 0) return null;
            try
            {
                var dbSet = db.GetType().GetMethods()
                    .FirstOrDefault(m => m.Name == "Set" && m.IsGenericMethod)
                    ?.MakeGenericMethod(type)
                    .Invoke(db, null);

                if (dbSet == null) return null;

                var keyList = keys.Values.Select(v => {
                    if (v is System.Text.Json.JsonElement elem)
                    {
                        if (elem.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            if (elem.TryGetInt32(out int i)) return (object)i;
                            return (object)elem.GetDouble();
                        }
                        return (object)elem.GetString()!;
                    }
                    return v;
                }).ToArray();

                var findMethod = dbSet.GetType().GetMethod("Find", new Type[] { typeof(object[]) });
                if (findMethod != null)
                {
                    return findMethod.Invoke(db, new object[] { keyList });
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Lỗi khi tìm thực thể trong CSDL: {Err}", ex.Message);
            }
            return null;
        }

        private static void StartDatabaseRecoveryMonitor(string currentDbVersion)
        {
            var ramConnection = AppDbContext.FallbackInMemoryConnection;
            if (ramConnection == null) return;

            Task.Run(async () =>
            {
                var dbPath = AppPaths.DatabaseFile;
                Log.Information("[DatabaseRecovery] Đã bắt đầu luồng giám sát giải phóng khóa CSDL file...");

                while (AppDbContext.FallbackInMemoryConnection == ramConnection)
                {
                    await Task.Delay(30000); // Check every 30 seconds

                    // Concurrency check with 3-retry attempt loop
                    bool isUnlocked = false;
                    for (int i = 1; i <= 3; i++)
                    {
                        try
                        {
                            if (File.Exists(dbPath))
                            {
                                using (var fs = new FileStream(dbPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                                {
                                    // File is not locked
                                    isUnlocked = true;
                                    break;
                                }
                            }
                            else
                            {
                                // If database file doesn't exist, it's not locked
                                isUnlocked = true;
                                break;
                            }
                        }
                        catch (IOException)
                        {
                            Log.Warning("[DatabaseRecovery] Lần thử {Retry}/3: CSDL file vẫn đang bị khóa. Đang thử lại...", i);
                            if (i < 3) await Task.Delay(1000);
                        }
                    }

                    if (isUnlocked)
                    {
                        Log.Information("[DatabaseRecovery] Phát hiện khóa CSDL file đã được giải phóng. Đang bắt đầu đồng bộ ngược...");

                        // Retrieve data from RAM DB before clearing the FallbackInMemoryConnection
                        List<EventLog> ramLogs;
                        List<SurveyResponse> ramResponses;
                        try
                        {
                            using (var ramDb = new AppDbContext())
                            {
                                ramLogs = ramDb.EventLogs.AsNoTracking().ToList();
                                ramResponses = ramDb.SurveyResponses.AsNoTracking().ToList();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("[DatabaseRecovery] Không thể truy vấn dữ liệu từ RAM DB: {Err}", ex.Message);
                            continue; // Skip this sync and try again next loop cycle
                        }

                        // Start sync transaction to real DB file
                        try
                        {
                            AppDbContext.FallbackInMemoryConnection = null;

                            // Ensure directories exist and migrate DB file just in case it is fresh
                            AppPaths.EnsureDirectories();
                            try
                            {
                                DbEncryptionKeyManager.EnsureDatabaseEncrypted();
                            }
                            catch (Exception encEx)
                            {
                                Log.Warning("[DatabaseRecovery] Không thể mã hóa tệp đĩa: {Msg}", encEx.Message);
                            }

                            using (var realDb = new AppDbContext())
                            {
                                try
                                {
                                    DbMigrator.Migrate(realDb, "6.02.0");
                                }
                                catch (Exception migEx)
                                {
                                    Log.Error("[DatabaseRecovery] Lỗi nâng cấp cấu trúc CSDL đĩa vật lý: {Msg}", migEx.Message);
                                    throw;
                                }

                                using (var transaction = await realDb.Database.BeginTransactionAsync())
                                {
                                    Log.Information("[DatabaseRecovery] Đang ghi đè và đồng bộ dữ liệu vào file DB...");

                                    foreach (var log in ramLogs)
                                    {
                                        log.Id = 0; // Regenerate primary key on file DB
                                        realDb.EventLogs.Add(log);
                                    }

                                    foreach (var resp in ramResponses)
                                    {
                                        realDb.SurveyResponses.Add(resp);
                                    }

                                    await realDb.SaveChangesAsync();
                                    await transaction.CommitAsync();
                                }
                            }

                            Log.Information("[DatabaseRecovery] Đồng bộ ngược dữ liệu thành công. Kết thúc chế độ RAM DB.");

                            try
                            {
                                ramConnection.Close();
                                ramConnection.Dispose();
                            }
                            catch { }

                            break; // Stop loop as we recovered successfully!
                        }
                        catch (Exception ex)
                        {
                            Log.Error("[DatabaseRecovery] Lỗi trong quá trình ghi dữ liệu xuống file DB: {Err}", ex.Message);
                            // Restore fallback connection
                            AppDbContext.FallbackInMemoryConnection = ramConnection;
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Tự động kiểm tra và sao chép CSDL đóng gói đi kèm (.db) từ thư mục cài đặt/chạy app
        /// sang thư mục LocalAppData nếu chưa có hoặc nếu bản đi kèm mới hơn.
        /// </summary>
        private static void EnsureBundledDatabaseCopied(string dbPath)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] candidatePaths = new string[]
                {
                    Path.Combine(baseDir, "smartclass.db"),
                    Path.Combine(baseDir, "QASmartClass.db"),
                    Path.Combine(baseDir, "Assets", "smartclass.db"),
                    Path.Combine(baseDir, "Assets", "smartclass_default.db"),
                    Path.Combine(baseDir, "Assets", "sample_smartclass.db")
                };

                string? foundBundledDb = null;
                foreach (var candidate in candidatePaths)
                {
                    if (File.Exists(candidate) && new FileInfo(candidate).Length > 0)
                    {
                        foundBundledDb = candidate;
                        break;
                    }
                }

                if (foundBundledDb != null)
                {
                    bool shouldCopy = false;
                    if (!File.Exists(dbPath) || new FileInfo(dbPath).Length == 0)
                    {
                        shouldCopy = true;
                        Log.Information("[SystemIntegrity] CSDL local chưa tồn tại. Đang sao chép CSDL đóng gói đi kèm: {Source} -> {Dest}", foundBundledDb, dbPath);
                    }
                    else
                    {
                        var srcInfo = new FileInfo(foundBundledDb);
                        var dstInfo = new FileInfo(dbPath);
                        if (srcInfo.LastWriteTime > dstInfo.LastWriteTime)
                        {
                            shouldCopy = true;
                            Log.Information("[SystemIntegrity] Phát hiện CSDL đi kèm ứng dụng mới hơn. Đang cập nhật: {Source} -> {Dest}", foundBundledDb, dbPath);
                        }
                    }

                    if (shouldCopy)
                    {
                        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                        string? targetDir = Path.GetDirectoryName(dbPath);
                        if (!string.IsNullOrEmpty(targetDir))
                        {
                            Directory.CreateDirectory(targetDir);
                        }
                        File.Copy(foundBundledDb, dbPath, overwrite: true);
                        Log.Information("[SystemIntegrity] Đã sao chép CSDL thành công ({Size} bytes).", new FileInfo(dbPath).Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[SystemIntegrity] Lỗi khi sao chép CSDL đi kèm ứng dụng.");
            }
        }
    }
}
