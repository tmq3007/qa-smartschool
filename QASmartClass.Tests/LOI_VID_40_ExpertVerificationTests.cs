using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using QASmartTouch;
using QASmartClass.Data;

namespace QASmartClass.Tests
{
    /// <summary>
    /// BỘ KIỂM THỬ XÁC MINH VÀ NGHIỆM THU LOI_VID_40 (HỘI ĐỒNG CHUYÊN GIA)
    /// 
    /// Xác minh các cải tiến tránh tranh chấp khóa SQLite và kiểm soát Single Instance:
    ///   TC-01: Kiểm tra SqliteWalInterceptor thực thi pragma busy_timeout lập tức khi mở kết nối.
    ///   TC-02: Kiểm tra sự hiện diện của EnforceSingleInstance trong class App.
    ///   TC-03: Kiểm tra cấu hình Security_SingleInstanceMode được nạp và ghi chính xác.
    /// </summary>
    public class LOI_VID_40_ExpertVerificationTests : IDisposable
    {
        private readonly string _dbFile;
        private readonly string _versionFile;

        public LOI_VID_40_ExpertVerificationTests()
        {
            QASmartClass.Services.AppPaths.EnsureDirectories();
            var uniqueId = Guid.NewGuid().ToString("N");
            _dbFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"smartclass_expert_40_{uniqueId}.db");
            _versionFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"db_version_expert_40_{uniqueId}.txt");

            QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
            QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;

            // Khởi tạo schema database rỗng
            using (var db = new AppDbContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.SaveChanges();
            }

            // Mock Application Context cho môi trường UI WPF
            RunOnSTA(() =>
            {
                if (Application.Current != null && (!(Application.Current is QASmartTouch.App) || Application.Current.Dispatcher.Thread != Thread.CurrentThread))
                {
                    var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                    if (appCreatedField != null)
                    {
                        appCreatedField.SetValue(null, false);
                    }
                    var currentField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                    if (currentField != null)
                    {
                        currentField.SetValue(null, null);
                    }
                }

                if (Application.Current == null)
                {
                    var app = new QASmartTouch.App();
                    var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance);
                    if (dbProperty != null)
                    {
                        dbProperty.SetValue(app, new AppDbContext());
                    }
                }
            });
        }

        public void Dispose()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { if (File.Exists(_dbFile)) File.Delete(_dbFile); } catch { }
            try { if (File.Exists(_versionFile)) File.Delete(_versionFile); } catch { }
        }

        private void RunOnSTA(Action action)
        {
            void InitializeApplicationFull()
            {
                var urls = new[] {
                    "pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/Styles.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/StaffTheme.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/InterOutfitFonts.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/SvgIcons.xaml",
                    "pack://application:,,,/QASmartClass;component/Localization/Strings_vi.xaml",
                    "pack://application:,,,/QASmartClass;component/LearningTools/Themes/LearningToolsStyles.xaml"
                };

                try
                {
                    var appField = typeof(System.Windows.Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    var createdField = typeof(System.Windows.Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (appField != null) appField.SetValue(null, null);
                    if (createdField != null) createdField.SetValue(null, false);

                    var app = new QASmartTouch.App();
                    foreach (var url in urls)
                    {
                        app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                        {
                            Source = new Uri(url, UriKind.Absolute)
                        });
                    }
                }
                catch { }
            }
            Exception? threadEx = null;
            var t = new Thread(() =>
            {
                try
                {
                    QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
                    QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;
                    InitializeApplicationFull(); action();
                }
                catch (Exception ex) { threadEx = ex; }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            bool completed = t.Join(TimeSpan.FromSeconds(15));
            Assert.True(completed, "STA thread timed out after 15 seconds");
            Assert.Null(threadEx);
        }

        [Fact]
        public void TC01_Verification_BusyTimeoutPrecedesWALQuery()
        {
            // Verify that ConnectionOpened interceptor sets busy_timeout immediately
            var interceptor = new AppDbContext.SqliteWalInterceptor();
            
            using (var conn = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:;"))
            {
                conn.Open();
                
                // Invoke ConnectionOpened through reflection or direct call (we just want to check it executes without throwing missing tables)
                var ex = Record.Exception(() => interceptor.ConnectionOpened(conn, null!));
                Assert.Null(ex);
                
                // Verify that busy_timeout is set to 5000 in the connection
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA busy_timeout;";
                    var timeout = cmd.ExecuteScalar();
                    Assert.Equal(5000L, Convert.ToInt64(timeout));
                }
            }
        }

        [Fact]
        public void TC02_Verification_EnforceSingleInstanceMethodExists()
        {
            RunOnSTA(() =>
            {
                var appType = typeof(QASmartTouch.App);
                var method = appType.GetMethod("EnforceSingleInstance", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
            });
        }

        [Fact]
        public void TC03_MasterConfig_SingleInstanceMode_GetAndSet()
        {
            using (var db = new AppDbContext())
            {
                db.SystemSettings.Add(new SystemSetting
                {
                    Id = "Security_SingleInstanceMode",
                    Value = "ExitNew",
                    Category = "Security",
                    LastUpdated = DateTime.Now
                });
                db.SaveChanges();

                var val = db.SystemSettings.Find("Security_SingleInstanceMode")?.Value;
                Assert.Equal("ExitNew", val);
            }
        }
    }
}
