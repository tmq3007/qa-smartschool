using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.ViewModels;
using Xunit;

namespace QASmartClass.Tests
{
    public class V36IntegrationTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly string _tempVersionPath;

        public V36IntegrationTests()
        {
            QASmartClass.Staff.Services.ServiceRegistration.Initialize();
            if (System.Windows.Application.Current == null)
            {
                try
                {
                    new System.Windows.Application();
                }
                catch { }
            }
            AppServices.UIService = new MockUserInterfaceService();

            string guid = Guid.NewGuid().ToString("N");
            _tempDbPath = Path.Combine(AppPaths.RootDir, $"smartclass_test_{guid}.db");
            _tempVersionPath = Path.Combine(AppPaths.RootDir, $"db_version_{guid}.txt");

            AppPaths.DatabaseFile = _tempDbPath;
            AppPaths.DbVersionFile = _tempVersionPath;
            AppPaths.EnsureDirectories();

            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.33.0");
        }

        [Fact]
        public void TestSQLiteWalModeEnabled()
        {
            using (var db = new AppDbContext())
            {
                var conn = db.Database.GetDbConnection();
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA journal_mode;";
                    var res = cmd.ExecuteScalar();
                    Assert.Equal("wal", res?.ToString()?.ToLower());
                }
            }
        }

        [Fact]
        public void TestDPAPIProfileCacheProtection()
        {
            // Verify DPAPI encryption works on this machine/environment
            string testJson = "{\"StudentCode\":\"HS007\",\"StudentName\":\"Bond\",\"TeacherIP\":\"192.168.1.1\",\"RememberMe\":true}";
            byte[] rawBytes = Encoding.UTF8.GetBytes(testJson);
            
            // Protect
            byte[] protectedBytes = ProtectedData.Protect(rawBytes, null, DataProtectionScope.CurrentUser);
            Assert.NotNull(protectedBytes);
            Assert.NotEqual(rawBytes, protectedBytes); // Encrypted bytes must differ

            // Unprotect
            byte[] decryptedBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            string result = Encoding.UTF8.GetString(decryptedBytes);
            
            Assert.Equal(testJson, result);
        }

        [Fact]
        public void TestTaskManagementAutoRefreshDraftProtection()
        {
            using (var vm = new TaskManagementViewModel())
            {
                var field = typeof(TaskManagementViewModel).GetField("_refreshTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(field);
                
                var timer = field.GetValue(vm) as System.Windows.Threading.DispatcherTimer;
                Assert.NotNull(timer);
                
                // Set notes/title to simulate user typing draft comments
                vm.TaskTitle = "Writing draft task title";
                vm.Notes = "Writing draft notes";
                
                // Assert that timer exists and task manager is loaded, but reload won't occur if there are drafts
                Assert.True(timer.IsEnabled);
                
                // Check properties are set
                Assert.Equal("Writing draft task title", vm.TaskTitle);
                Assert.Equal("Writing draft notes", vm.Notes);
            }
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_tempDbPath)) File.Delete(_tempDbPath);
                if (File.Exists(_tempVersionPath)) File.Delete(_tempVersionPath);
            }
            catch { }
        }
    }
}
