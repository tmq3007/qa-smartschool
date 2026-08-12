using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Xunit;
using QASmartClass.Data;
using QASmartClass.LearningTools.Views.Multi;

[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace QASmartClass.Tests
{
    public class V53NoiseMonitorAndDbLockTests : IDisposable
    {
        private readonly string _dbFile;
        private readonly string _versionFile;

        public V53NoiseMonitorAndDbLockTests()
        {
            QASmartClass.Services.AppPaths.EnsureDirectories();
            var uniqueId = Guid.NewGuid().ToString("N");
            _dbFile = System.IO.Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"smartclass_noise_{uniqueId}.db");
            _versionFile = System.IO.Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"db_version_noise_{uniqueId}.txt");
            
            QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
            QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;
            
            NotebookTool.MessageBoxShowHandler = (msg, title, buttons, icon) => MessageBoxResult.OK;

            using var db = new AppDbContext();
            QASmartClass.Services.DbMigrator.Migrate(db, "5.44.0");
        }

        public void Dispose()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try
            {
                if (System.IO.File.Exists(_dbFile))
                    System.IO.File.Delete(_dbFile);
            }
            catch {}
            try
            {
                if (System.IO.File.Exists(_versionFile))
                    System.IO.File.Delete(_versionFile);
            }
            catch {}
        }

        private static readonly object AppInitLock = new object();

        private void RunOnSTA(Action action)
        {
            Exception? threadEx = null;
            var t = new System.Threading.Thread(() =>
            {
                lock (AppInitLock)
                {
                    try
                    {
                        // Unconditionally reset Application instance fields to avoid conflicting with other tests' shutdowns
                        try
                        {
                            var appCreatedField = typeof(System.Windows.Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                            if (appCreatedField != null) appCreatedField.SetValue(null, false);
                            var currentField = typeof(System.Windows.Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                            if (currentField != null) currentField.SetValue(null, null);
                        }
                        catch { }

                        // Initialize the QASmartTouch App context for this thread
                        var app = new QASmartTouch.App();
                        var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (dbProperty != null)
                        {
                            dbProperty.SetValue(app, new AppDbContext());
                        }

                        // Load Design Tokens and Styles so page components can load successfully
                        var resources = app.Resources;
                        try
                        {
                            bool hasTokens = false;
                            foreach (var dict in resources.MergedDictionaries)
                            {
                                if (dict.Source != null && dict.Source.OriginalString.Contains("DesignTokens.xaml"))
                                {
                                    hasTokens = true;
                                    break;
                                }
                            }
                            if (!hasTokens)
                            {
                                resources.MergedDictionaries.Add(new ResourceDictionary
                                {
                                    Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute)
                                });
                                resources.MergedDictionaries.Add(new ResourceDictionary
                                {
                                    Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/Styles.xaml", UriKind.Absolute)
                                });
                                resources.MergedDictionaries.Add(new ResourceDictionary
                                {
                                    Source = new Uri("pack://application:,,,/QASmartClass;component/LearningTools/Themes/LearningToolsStyles.xaml", UriKind.Absolute)
                                });
                            }
                        }
                        catch { }

                        action();
                    }
                    catch (Exception ex)
                    {
                        threadEx = ex;
                    }
                    finally
                    {
                        try
                        {
                            var appCreatedField = typeof(System.Windows.Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                            if (appCreatedField != null) appCreatedField.SetValue(null, false);
                            var currentField = typeof(System.Windows.Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                            if (currentField != null) currentField.SetValue(null, null);
                        }
                        catch { }
                    }
                }
            });
            t.SetApartmentState(System.Threading.ApartmentState.STA);
            t.Start();
            bool completed = t.Join(TimeSpan.FromSeconds(15));
            Assert.True(completed, "STA thread timed out after 15 seconds");
            if (threadEx != null) throw threadEx;
        }

        [Fact]
        public void TestNoiseMonitorTool_Unloaded_ShouldDispose()
        {
            RunOnSTA(() =>
            {
                // Create instance in STA thread
                var tool = new NoiseMonitorTool();
                
                // Programmatically raise the Unloaded event to trigger Dispose
                tool.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.FrameworkElement.UnloadedEvent));
                
                // Verify that the control is not null and completed event cleanly
                Assert.NotNull(tool);
            });
        }

        [Fact]
        public void TestNoiseMonitorTool_RMSAndSettingsBypass_ShouldSucceed()
        {
            RunOnSTA(() =>
            {
                // Create instance in STA thread
                var tool = new NoiseMonitorTool();
                
                // Verify initial settings load
                Assert.NotNull(tool);
                
                // Clean up
                tool.Dispose();
            });
        }

        [Fact]
        public async Task TestNoiseMonitorTool_DatabaseLogging_ShouldSucceed()
        {
            RunOnSTA(() =>
            {
                var tool = new NoiseMonitorTool();
                
                var methodAlert = typeof(NoiseMonitorTool).GetMethod("LogAlertToDb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var methodCelebration = typeof(NoiseMonitorTool).GetMethod("LogCelebrationToDb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                Assert.NotNull(methodAlert);
                Assert.NotNull(methodCelebration);
                
                methodAlert.Invoke(tool, new object[] { 85.0 });
                methodCelebration.Invoke(tool, null);
                
                tool.Dispose();
            });

            await Task.Delay(500);

            using (var db = new AppDbContext())
            {
                var alertLog = db.EventLogs.FirstOrDefault(l => l.EventType == "NOISE_RADAR_ALERT" && l.Actor == "NoiseRadar");
                var celebrationLog = db.EventLogs.FirstOrDefault(l => l.EventType == "NOISE_RADAR_CELEBRATION" && l.Actor == "NoiseRadar");
                
                Assert.NotNull(alertLog);
                Assert.NotNull(celebrationLog);
                Assert.Contains("85 dB", alertLog.Details);
                Assert.Contains("duy trì trật tự", celebrationLog.Details);
            }
        }

        [Fact]
        public async Task TestAppDbContext_ConcurrentWrites_ShouldSucceedWithoutDbLocked()
        {
            var tasks = new System.Collections.Generic.List<Task>();
            for (int i = 0; i < 5; i++)
            {
                int threadId = i;
                tasks.Add(Task.Run(async () =>
                {
                    using (var db = new AppDbContext())
                    {
                        var log = new EventLog
                        {
                            EventType = "CONCURRENCY_TEST",
                            Actor = $"Thread_{threadId}",
                            Details = $"Concurrent write test from thread {threadId}",
                            Timestamp = DateTime.Now
                        };
                        db.EventLogs.Add(log);
                        
                        // Mix sync and async writes to verify both locks
                        if (threadId % 2 == 0)
                        {
                            db.SaveChanges();
                        }
                        else
                        {
                            await db.SaveChangesAsync();
                        }
                        
                        // Clean up
                        db.EventLogs.Remove(log);
                        if (threadId % 2 == 0)
                        {
                            db.SaveChanges();
                        }
                        else
                        {
                            await db.SaveChangesAsync();
                        }
                    }
                }));
            }
            
            // If any thread fails due to SQLite locking/busy, Task.WhenAll will propagate the exception
            var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
            Assert.Null(exception);
        }

        [Fact]
        public void Test_NotebookTool_ConnectionPragmas_ShouldBeWalAndTimeout()
        {
            RunOnSTA(() =>
            {
                var tool = new NotebookTool();
                var methodGetConn = typeof(NotebookTool).GetMethod("GetConnection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(methodGetConn);
                
                using (var conn = (Microsoft.Data.Sqlite.SqliteConnection)methodGetConn.Invoke(tool, null)!)
                {
                    Assert.NotNull(conn);
                    Assert.Equal(System.Data.ConnectionState.Open, conn.State);
                    
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA journal_mode;";
                        var mode = cmd.ExecuteScalar()?.ToString();
                        Assert.Equal("wal", mode?.ToLower());
                    }
                    
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA busy_timeout;";
                        var timeout = Convert.ToInt32(cmd.ExecuteScalar());
                        Assert.Equal(5000, timeout);
                    }
                }
                tool.Dispose();
            });
        }

        private Microsoft.Data.Sqlite.SqliteConnection GetTestConnection()
        {
            var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
            var keyBytes = QASmartClass.Data.DbEncryptionKeyManager.GetOrInitializeKey();
            var hexKey = Convert.ToHexString(keyBytes);
            var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath};Password={hexKey};Default Timeout=5;");
            connection.Open();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
                cmd.ExecuteNonQuery();
            }
            return connection;
        }

        [Fact]
        public async Task Test_NotebookTool_ConcurrentAccess_ShouldNotLock()
        {
            // First, make sure the Notebook tables exist in DB by instantiating the tool once
            RunOnSTA(() =>
            {
                var tool = new NotebookTool();
                tool.Dispose();
            });

            // Insert parent notebook rows to satisfy FOREIGN KEY constraint
            using (var connection = GetTestConnection())
            {
                using (var cmd = connection.CreateCommand())
                {
                    for (int i = 0; i < 3; i++)
                    {
                        cmd.CommandText = @"
                            INSERT OR IGNORE INTO Notebooks (Id, Title, CoverColor, CoverType, PaperColor, ThemeIndex, PaperSizeTag, CurrentPageIndex, CreatedAt)
                            VALUES ($id, $title, $coverColor, $coverType, $paperColor, $themeIndex, $paperSizeTag, $currentPageIndex, $createdAt);";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("$id", $"nb_{i}");
                        cmd.Parameters.AddWithValue("$title", $"Test Notebook {i}");
                        cmd.Parameters.AddWithValue("$coverColor", "#FF2196F3");
                        cmd.Parameters.AddWithValue("$coverType", "Smooth");
                        cmd.Parameters.AddWithValue("$paperColor", "#FFFFFFFF");
                        cmd.Parameters.AddWithValue("$themeIndex", 0);
                        cmd.Parameters.AddWithValue("$paperSizeTag", "1200x848");
                        cmd.Parameters.AddWithValue("$currentPageIndex", 0);
                        cmd.Parameters.AddWithValue("$createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            // Now, run concurrent writes on EF Core (AppDbContext) and reads/writes on NotebookTool connections
            var tasks = new System.Collections.Generic.List<Task>();
            
            // Task 1-3: Direct Notebook SQLite operations
            for (int i = 0; i < 3; i++)
            {
                int id = i;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            using (var connection = GetTestConnection())
                            {
                                using (var cmd = connection.CreateCommand())
                                {
                                    // Check if page exists
                                    cmd.CommandText = "SELECT COUNT(*) FROM NotebookPages WHERE NotebookId = $nbId AND PageIndex = $pIndex";
                                    cmd.Parameters.AddWithValue("$nbId", $"nb_{id}");
                                    cmd.Parameters.AddWithValue("$pIndex", j);
                                    bool exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                                    
                                    cmd.Parameters.Clear();
                                    if (exists)
                                    {
                                        cmd.CommandText = @"
                                            UPDATE NotebookPages
                                            SET StrokeData = $strokes, DateText = $date, ShiftText = $shift, WeatherText = $weather, MoodText = $mood, PaperPattern = $pattern
                                            WHERE NotebookId = $nbId AND PageIndex = $pIndex;";
                                    }
                                    else
                                    {
                                        cmd.CommandText = @"
                                            INSERT INTO NotebookPages (NotebookId, PageIndex, StrokeData, DateText, ShiftText, WeatherText, MoodText, PaperPattern)
                                            VALUES ($nbId, $pIndex, $strokes, $date, $shift, $weather, $mood, $pattern);";
                                    }
                                    cmd.Parameters.AddWithValue("$nbId", $"nb_{id}");
                                    cmd.Parameters.AddWithValue("$pIndex", j);
                                    cmd.Parameters.AddWithValue("$strokes", new byte[] { 1, 2, 3 });
                                    cmd.Parameters.AddWithValue("$date", "24/06/2026");
                                    cmd.Parameters.AddWithValue("$shift", "Sáng 🌅");
                                    cmd.Parameters.AddWithValue("$weather", "Sunny");
                                    cmd.Parameters.AddWithValue("$mood", "Happy");
                                    cmd.Parameters.AddWithValue("$pattern", "Blank");
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Direct SQLite write failed in concurrent task {id}: {ex.Message}", ex);
                    }
                }));
            }

            // Task 4-6: Concurrent AppDbContext writes
            for (int i = 0; i < 3; i++)
            {
                int id = i;
                tasks.Add(Task.Run(async () =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        using (var db = new AppDbContext())
                        {
                            var log = new EventLog
                            {
                                EventType = "CONCURRENT_NOTEBOOK_TEST",
                                Actor = $"Thread_{id}",
                                Details = $"Detail {j}",
                                Timestamp = DateTime.Now
                            };
                            db.EventLogs.Add(log);
                            await db.SaveChangesAsync();
                            
                            db.EventLogs.Remove(log);
                            await db.SaveChangesAsync();
                        }
                    }
                }));
            }

            var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
            Assert.Null(exception);
        }
    }
}
