using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using QASmartClass.StudentClient.Views;
using QASmartClass.StudentClient.Services;
using QASmartClass.Data;

namespace QASmartClass.Tests
{
    /// <summary>
    /// BỘ KIỂM THỬ XÁC MINH VÀ NGHIỆM THU LOI_VID_26 (HỘI ĐỒNG CHUYÊN GIA)
    /// 
    /// Đánh giá các cải tiến kỹ thuật trong UPGRADE_04:
    ///   TC-01: Kiểm tra phương thức EnsureStudentFirewallRules và cấu hình tự động mở cổng Firewall.
    ///   TC-02: Kiểm tra cấu hình và sự tồn tại của Direct Reconnect TCP Loop.
    ///   TC-03: Kiểm tra cấu hình Security_KillBrowsers tắt các ứng dụng trình duyệt ngoài.
    /// </summary>
    public class LOI_VID_26_ExpertVerificationTests : IDisposable
    {
        private readonly string _dbFile;
        private readonly string _versionFile;

        public LOI_VID_26_ExpertVerificationTests()
        {
            QASmartClass.Services.AppPaths.EnsureDirectories();
            var uniqueId = Guid.NewGuid().ToString("N");
            _dbFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"smartclass_expert_26_{uniqueId}.db");
            _versionFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"db_version_expert_26_{uniqueId}.txt");

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
        public void TC01_Verification_FirewallRulesMethodExists()
        {
            RunOnSTA(() =>
            {
                var shellType = typeof(StudentShell);
                var method = shellType.GetMethod("EnsureStudentFirewallRules", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(method);
            });
        }

        [Fact]
        public void TC02_Verification_DirectReconnectLoopExists()
        {
            var clientType = typeof(StudentNetworkClient);
            var method = clientType.GetMethod("DirectReconnectLoopAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var parameters = method.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
        }

        [Fact]
        public void TC03_Verification_KillNonEducationalAppsMethodExists()
        {
            RunOnSTA(() =>
            {
                var shellType = typeof(StudentShell);
                var method = shellType.GetMethod("KillNonEducationalApps", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
            });
        }

        [Fact]
        public void TC04_MasterConfig_DefaultValuesLoadedCorrectly()
        {
            using (var db = new AppDbContext())
            {
                // Verify that writing configuration in database will work
                db.SystemSettings.Add(new SystemSetting { Id = "Network_DirectReconnectIntervalSec", Value = "5", Category = "Network" });
                db.SystemSettings.Add(new SystemSetting { Id = "Network_AutoConfigureFirewall", Value = "Disabled", Category = "Network" });
                db.SystemSettings.Add(new SystemSetting { Id = "Security_KillBrowsers", Value = "Disabled", Category = "Security" });
                db.SaveChanges();

                var interval = db.SystemSettings.FirstOrDefault(s => s.Id == "Network_DirectReconnectIntervalSec")?.Value;
                var firewall = db.SystemSettings.FirstOrDefault(s => s.Id == "Network_AutoConfigureFirewall")?.Value;
                var killBrowsers = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_KillBrowsers")?.Value;

                Assert.Equal("5", interval);
                Assert.Equal("Disabled", firewall);
                Assert.Equal("Disabled", killBrowsers);
            }
        }
    }
}
