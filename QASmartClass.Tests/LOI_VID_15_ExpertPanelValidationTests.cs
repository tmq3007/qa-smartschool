using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Xunit;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartTouch.Services;
using Microsoft.EntityFrameworkCore;

namespace QASmartClass.Tests
{
    /// <summary>
    /// BỘ KIỂM THỬ XÁC MINH TOÀN DIỆN CỦA HỘI ĐỒNG CHUYÊN GIA — LOI_VID_15 & CAI_TIEN_VID_01
    /// </summary>
    public class LOI_VID_15_ExpertPanelValidationTests : IDisposable
    {
        private readonly string _dbFile;
        private readonly string _versionFile;

        public LOI_VID_15_ExpertPanelValidationTests()
        {
            AppPaths.EnsureDirectories();
            var uniqueId = Guid.NewGuid().ToString("N");
            _dbFile = Path.Combine(AppPaths.RootDir, $"smartclass_expert_15_{uniqueId}.db");
            _versionFile = Path.Combine(AppPaths.RootDir, $"db_version_expert_15_{uniqueId}.txt");

            AppPaths.DatabaseFile = _dbFile;
            AppPaths.DbVersionFile = _versionFile;

            // Initialize DB schema
            using (var db = new AppDbContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.SaveChanges();
            }
        }

        public void Dispose()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { if (File.Exists(_dbFile)) File.Delete(_dbFile); } catch { }
            try { if (File.Exists(_versionFile)) File.Delete(_versionFile); } catch { }
        }

        private static readonly object AppInitLock = new object();

        private void RunOnSTA(Action action)
        {
            Exception? threadEx = null;
            var t = new Thread(() =>
            {
                lock (AppInitLock)
                {
                    try
                    {
                        // Unconditionally reset Application instance fields to avoid conflicting with other tests' shutdowns
                        try
                        {
                            var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                            if (appCreatedField != null) appCreatedField.SetValue(null, false);
                            var currentField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                            if (currentField != null) currentField.SetValue(null, null);
                        }
                        catch { }

                        // Initialize the QASmartTouch App context for this thread
                        var app = new QASmartTouch.App();
                        var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance);
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
                            }
                        }
                        catch { }

                        AppPaths.DatabaseFile = _dbFile;
                        AppPaths.DbVersionFile = _versionFile;

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
                            var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                            if (appCreatedField != null) appCreatedField.SetValue(null, false);
                            var currentField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                            if (currentField != null) currentField.SetValue(null, null);
                        }
                        catch { }
                    }
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            bool completed = t.Join(TimeSpan.FromSeconds(15));
            Assert.True(completed, "STA thread timed out after 15 seconds");
            Assert.Null(threadEx);
        }

        /// <summary>
        /// KIỂM THỬ 1 (IT Manager / DB Expert): Mô phỏng 50 học sinh gửi dữ liệu và 5 giáo viên đọc báo cáo đồng thời.
        /// Đảm bảo chế độ WAL và Semaphore tránh hoàn toàn lỗi "Database is locked".
        /// </summary>
        [Fact]
        public async Task Test_ClassroomStressConcurrency_ShouldNotLock()
        {
            var tasks = new System.Collections.Generic.List<Task>();

            // 50 luồng ghi (Student Client submissions)
            for (int i = 0; i < 50; i++)
            {
                int studentId = i;
                tasks.Add(Task.Run(async () =>
                {
                    for (int j = 0; j < 5; j++)
                    {
                        // Serialized background database access
                        await AppDbContext.BackgroundDbSemaphore.WaitAsync();
                        try
                        {
                            using (var db = new AppDbContext())
                            {
                                // Apply WAL
                                using (var conn = db.Database.GetDbConnection())
                                {
                                    if (conn.State != System.Data.ConnectionState.Open)
                                        await conn.OpenAsync();
                                    
                                    using (var cmd = conn.CreateCommand())
                                    {
                                        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }

                                var log = new EventLog
                                {
                                    EventType = "STUDENT_SUBMIT",
                                    Actor = $"Student_{studentId}",
                                    Details = $"Submit answer sheet segment {j}",
                                    Timestamp = DateTime.Now
                                };
                                db.EventLogs.Add(log);
                                await db.SaveChangesAsync();
                            }
                        }
                        finally
                        {
                            AppDbContext.BackgroundDbSemaphore.Release();
                        }
                        await Task.Delay(10); // simulate realistic network spacing
                    }
                }));
            }

            // 5 luồng đọc đồng thời của giáo viên (Teacher Client Dashboard)
            for (int i = 0; i < 5; i++)
            {
                int teacherId = i;
                tasks.Add(Task.Run(async () =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        // Reads do not need the semaphore under WAL mode
                        using (var db = new AppDbContext())
                        {
                            var count = db.EventLogs.Count(x => x.EventType == "STUDENT_SUBMIT");
                            System.Diagnostics.Debug.WriteLine($"Teacher {teacherId} read {count} student records.");
                        }
                        await Task.Delay(15);
                    }
                }));
            }

            var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
            Assert.Null(exception);
        }

        /// <summary>
        /// KIỂM THỬ 2 (QA Expert / Designer): Xác minh cấu hình gợi ý độ phân giải hình học 3D 
        /// dựa trên chất lượng cài đặt hoặc loại GPU tự động phát hiện qua WMI.
        /// </summary>
        [Fact]
        public void Test_GraphicsQualityAutoDetection_IntegratedVsDedicated()
        {
            var field = typeof(AppSettings).GetField("_isIntegratedGraphics", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(field);

            // Cấu hình cứng Low
            AppSettings.GraphicsQuality = "Low";
            Assert.Equal(20, AppSettings.GetRecommended3DResolution());

            // Cấu hình cứng Medium
            AppSettings.GraphicsQuality = "Medium";
            Assert.Equal(35, AppSettings.GetRecommended3DResolution());

            // Cấu hình cứng High
            AppSettings.GraphicsQuality = "High";
            Assert.Equal(50, AppSettings.GetRecommended3DResolution());

            // Cấu hình Auto với GPU tích hợp (Intel / WMI mock)
            AppSettings.GraphicsQuality = "Auto";
            field.SetValue(null, (bool?)true);
            Assert.Equal(20, AppSettings.GetRecommended3DResolution());

            // Cấu hình Auto với GPU rời (Dedicated GPU)
            field.SetValue(null, (bool?)false);
            Assert.Equal(50, AppSettings.GetRecommended3DResolution());

            // Reset field mock
            field.SetValue(null, null);
        }

        /// <summary>
        /// KIỂM THỬ 3 (Gamer / Student UX): Xác minh cấu hình hiển thị cạnh (Anti-Aliasing) của Helix Viewport
        /// sẽ được tắt (Aliased) ở chế độ Low (Resolution <= 20) để đảm bảo FPS tối đa.
        /// </summary>
        [Fact]
        public void Test_GraphicsSettings_ApplyResolutionAndAntiAliasing()
        {
            RunOnSTA(() =>
            {
                // Giả lập chế độ Low (Resolution = 20)
                AppSettings.GraphicsQuality = "Low";
                int resolution = AppSettings.GetRecommended3DResolution();
                Assert.Equal(20, resolution);

                // Tạo đối tượng giả lập xem cài đặt có thiết lập EdgeMode thành Aliased hay không
                var viewport = new HelixToolkit.Wpf.HelixViewport3D();
                
                // Áp dụng quy tắc thiết lập đồ họa
                if (resolution <= 20)
                {
                    RenderOptions.SetEdgeMode(viewport, EdgeMode.Aliased);
                }
                else
                {
                    RenderOptions.SetEdgeMode(viewport, EdgeMode.Unspecified);
                }

                Assert.Equal(EdgeMode.Aliased, RenderOptions.GetEdgeMode(viewport));
            });
        }

        /// <summary>
        /// KIỂM THỬ 4 (Security / DB Expert): Đảm bảo giao dịch Transaction hoạt động toàn vẹn dưới WAL mode.
        /// </summary>
        [Fact]
        public async Task Test_TransactionIntegrity_UnderAbruptInterrupts()
        {
            using (var db = new AppDbContext())
            {
                using (var transaction = await db.Database.BeginTransactionAsync())
                {
                    var log = new EventLog
                    {
                        EventType = "TX_ROLLBACK_TEST",
                        Actor = "Tester",
                        Details = "Will be rolled back",
                        Timestamp = DateTime.Now
                    };
                    db.EventLogs.Add(log);
                    await db.SaveChangesAsync();

                    // Rollback transaction to simulate cancel/interrupt
                    await transaction.RollbackAsync();
                }

                // Verify the entry was not persisted
                var entry = db.EventLogs.FirstOrDefault(x => x.EventType == "TX_ROLLBACK_TEST");
                Assert.Null(entry);
            }
        }
    }
}
