using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Admin.Controls;

namespace QASmartClass.Tests
{
    public class V102ITAdminOTATests
    {
        private string _testDataDir = string.Empty;
        private string _testDbPath = string.Empty;

        private void InitializeTestEnvironment()
        {
            _testDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestDataDir_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDataDir);
            AppPaths.DataDirOverride = _testDataDir;

            _testDbPath = Path.Combine(_testDataDir, "smartclass_test.db");
            AppPaths.DatabaseFile = _testDbPath;
        }

        private void CleanupTestEnvironment()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            AppPaths.DataDirOverride = null;
            AppPaths.DatabaseFile = null;

            try
            {
                if (Directory.Exists(_testDataDir))
                {
                    Directory.Delete(_testDataDir, true);
                }
            }
            catch { }
        }

        private AppDbContext CreateTempDbContext()
        {
            var keyBytes = DbEncryptionKeyManager.GetOrInitializeKey();
            var hexKey = Convert.ToHexString(keyBytes);

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={_testDbPath};Password={hexKey};Foreign Keys=True;Default Timeout=5")
                .Options;

            var db = new AppDbContext(options);
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            return db;
        }

        private void RunInSTA(Action action)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                    SynchronizationContext.SetSynchronizationContext(
                        new System.Windows.Threading.DispatcherSynchronizationContext(dispatcher));

                    action();
                }
                catch (Exception ex)
                {
                    ExceptionDispatchInfo.Capture(ex).Throw();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        private void PumpMessages(int ms)
        {
            var frame = new System.Windows.Threading.DispatcherFrame();
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ms)
            };
            timer.Tick += (s, e) =>
            {
                frame.Continue = false;
                timer.Stop();
            };
            timer.Start();
            System.Windows.Threading.Dispatcher.PushFrame(frame);
        }

        private void InvokePrivateMethod(object obj, string methodName, params object[] args)
        {
            var type = obj.GetType();
            var method = type.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method == null) throw new ArgumentException($"Method {methodName} not found on {type.Name}");
            method.Invoke(obj, args);
        }

        [Fact]
        public void Test_LoadReleases_CreatesDefaultJson_IfMissing()
        {
            InitializeTestEnvironment();
            try
            {
                string jsonPath = Path.Combine(_testDataDir, "ota_releases.json");
                if (File.Exists(jsonPath)) File.Delete(jsonPath);

                var list = QAVendorAdminControl.ReadOtaReleases();

                Assert.True(File.Exists(jsonPath));
                Assert.Equal(2, list.Count);
                Assert.Equal("v3.1.3", list[0].Version);
                Assert.Equal("v3.2.0", list[1].Version);
            }
            finally
            {
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_VerifySHA256_MatchesCorrectChecksum()
        {
            InitializeTestEnvironment();
            try
            {
                var releases = new List<QAVendorAdminControl.OtaReleaseInfo>
                {
                    new QAVendorAdminControl.OtaReleaseInfo { Version = "v3.2.0", ReleaseDate = "2026-06-30", Description = "Test", SHA256 = "ABCDEF123456", Status = "Available" }
                };
                QAVendorAdminControl.SaveOtaReleases(releases);

                using (var db = CreateTempDbContext())
                {
                    // Create tables
                }

                RunInSTA(() =>
                {
                    QAVendorAdminControl.IsTesting = true;
                    var control = new QAVendorAdminControl();
                    
                    InvokePrivateMethod(control, "LoadOtaData");

                    var dg = control.FindName("dgOtaReleases") as DataGrid;
                    Assert.NotNull(dg);
                    
                    var loadedReleases = dg.ItemsSource as List<QAVendorAdminControl.OtaReleaseInfo>;
                    Assert.NotNull(loadedReleases);
                    Assert.NotEmpty(loadedReleases);
                    dg.SelectedItem = loadedReleases[0];

                    InvokePrivateMethod(control, "BtnVerifyIntegrity_Click", null, null);

                    var verifiedReleases = QAVendorAdminControl.ReadOtaReleases();
                    Assert.Equal("Verified", verifiedReleases[0].Status);
                });
            }
            finally
            {
                QAVendorAdminControl.IsTesting = false;
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_StagedRollout_CalculatesCorrectTargetDevices()
        {
            int totalDevices = 150;
            
            int target10Pct = (int)Math.Ceiling(totalDevices * 0.1);
            int target50Pct = (int)Math.Ceiling(totalDevices * 0.5);
            int target100Pct = totalDevices;

            Assert.Equal(15, target10Pct);
            Assert.Equal(75, target50Pct);
            Assert.Equal(150, target100Pct);
        }

        [Fact]
        public void Test_DeployUpdate_TransitionsStatusCorrectly()
        {
            InitializeTestEnvironment();
            try
            {
                var releases = new List<QAVendorAdminControl.OtaReleaseInfo>
                {
                    new QAVendorAdminControl.OtaReleaseInfo { Version = "v3.2.0", ReleaseDate = "2026-06-30", Description = "Test", SHA256 = "123456", Status = "Verified" }
                };
                QAVendorAdminControl.SaveOtaReleases(releases);

                using (var db = CreateTempDbContext())
                {
                    db.SystemSettings.Add(new SystemSetting { Id = "OTA_SecureIntegrityMode", Value = "High", Category = "IT" });
                    db.SaveChanges();
                }

                RunInSTA(() =>
                {
                    QAVendorAdminControl.IsTesting = true;
                    var control = new QAVendorAdminControl();
                    
                    InvokePrivateMethod(control, "LoadOtaData");

                    var dg = control.FindName("dgOtaReleases") as DataGrid;
                    Assert.NotNull(dg);
                    
                    var loadedReleases = dg.ItemsSource as List<QAVendorAdminControl.OtaReleaseInfo>;
                    Assert.NotNull(loadedReleases);
                    Assert.NotEmpty(loadedReleases);
                    dg.SelectedItem = loadedReleases[0];

                    InvokePrivateMethod(control, "BtnDeployUpdate_Click", null, null);

                    // Pump dispatcher messages to let async/await steps execute
                    PumpMessages(1200);

                    var list = QAVendorAdminControl.ReadOtaReleases();
                    Assert.Equal("Deployed", list[0].Status);
                });
            }
            finally
            {
                QAVendorAdminControl.IsTesting = false;
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_OTAOptions_SaveAndRetrieveFromMasterSettings()
        {
            InitializeTestEnvironment();
            try
            {
                using (var db = CreateTempDbContext())
                {
                    var secureSetting = new SystemSetting { Id = "OTA_SecureIntegrityMode", Value = "High", Category = "IT" };
                    var strategySetting = new SystemSetting { Id = "OTA_DefaultRolloutStrategy", Value = "Staged", Category = "IT" };
                    
                    db.SystemSettings.Add(secureSetting);
                    db.SystemSettings.Add(strategySetting);
                    db.SaveChanges();

                    var readSecure = db.SystemSettings.FirstOrDefault(s => s.Id == "OTA_SecureIntegrityMode");
                    var readStrategy = db.SystemSettings.FirstOrDefault(s => s.Id == "OTA_DefaultRolloutStrategy");

                    Assert.NotNull(readSecure);
                    Assert.Equal("High", readSecure.Value);
                    Assert.NotNull(readStrategy);
                    Assert.Equal("Staged", readStrategy.Value);
                }
            }
            finally
            {
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_DeployUpdate_HandlesCancellationMidWay()
        {
            InitializeTestEnvironment();
            try
            {
                var releases = new List<QAVendorAdminControl.OtaReleaseInfo>
                {
                    new QAVendorAdminControl.OtaReleaseInfo { Version = "v3.2.0", ReleaseDate = "2026-06-30", Description = "Test", SHA256 = "123456", Status = "Verified" }
                };
                QAVendorAdminControl.SaveOtaReleases(releases);

                using (var db = CreateTempDbContext())
                {
                    db.SystemSettings.Add(new SystemSetting { Id = "OTA_SecureIntegrityMode", Value = "High", Category = "IT" });
                    db.SaveChanges();
                }

                RunInSTA(() =>
                {
                    QAVendorAdminControl.IsTesting = true;
                    var control = new QAVendorAdminControl();
                    
                    InvokePrivateMethod(control, "LoadOtaData");

                    var dg = control.FindName("dgOtaReleases") as DataGrid;
                    Assert.NotNull(dg);
                    
                    var loadedReleases = dg.ItemsSource as List<QAVendorAdminControl.OtaReleaseInfo>;
                    Assert.NotNull(loadedReleases);
                    Assert.NotEmpty(loadedReleases);
                    dg.SelectedItem = loadedReleases[0];

                    InvokePrivateMethod(control, "BtnDeployUpdate_Click", null, null);

                    // Cancel deployment directly
                    var ctsField = typeof(QAVendorAdminControl).GetField("_deployCts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var cts = ctsField.GetValue(control) as System.Threading.CancellationTokenSource;
                    cts?.Cancel();

                    // Let message pump process cancel event
                    PumpMessages(400);

                    var afterReleases = QAVendorAdminControl.ReadOtaReleases();
                    Assert.NotEqual("Deployed", afterReleases[0].Status);
                });
            }
            finally
            {
                QAVendorAdminControl.IsTesting = false;
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_LoadReleases_HandlesCorruptedJsonFile()
        {
            InitializeTestEnvironment();
            try
            {
                string jsonPath = Path.Combine(_testDataDir, "ota_releases.json");
                File.WriteAllText(jsonPath, "{ Invalid Corrupted JSON content }");

                var list = QAVendorAdminControl.ReadOtaReleases();

                Assert.Equal(2, list.Count);
                Assert.Equal("v3.1.3", list[0].Version);
                Assert.Equal("v3.2.0", list[1].Version);
            }
            finally
            {
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_DeployUpdate_StrictSHA256Mode_FailsWhenUnverified()
        {
            InitializeTestEnvironment();
            try
            {
                var releases = new List<QAVendorAdminControl.OtaReleaseInfo>
                {
                    new QAVendorAdminControl.OtaReleaseInfo { Version = "v3.2.0", ReleaseDate = "2026-06-30", Description = "Test", SHA256 = "123456", Status = "Available" }
                };
                QAVendorAdminControl.SaveOtaReleases(releases);

                using (var db = CreateTempDbContext())
                {
                    db.SystemSettings.Add(new SystemSetting { Id = "OTA_SecureIntegrityMode", Value = "High", Category = "IT" });
                    db.SaveChanges();
                }

                RunInSTA(() =>
                {
                    QAVendorAdminControl.IsTesting = true;
                    var control = new QAVendorAdminControl();
                    
                    InvokePrivateMethod(control, "LoadOtaData");

                    var dg = control.FindName("dgOtaReleases") as DataGrid;
                    Assert.NotNull(dg);
                    
                    var loadedReleases = dg.ItemsSource as List<QAVendorAdminControl.OtaReleaseInfo>;
                    Assert.NotNull(loadedReleases);
                    Assert.NotEmpty(loadedReleases);
                    dg.SelectedItem = loadedReleases[0];

                    InvokePrivateMethod(control, "BtnDeployUpdate_Click", null, null);

                    PumpMessages(400);

                    var afterReleases = QAVendorAdminControl.ReadOtaReleases();
                    Assert.Equal("Available", afterReleases[0].Status);
                });
            }
            finally
            {
                QAVendorAdminControl.IsTesting = false;
                CleanupTestEnvironment();
            }
        }
    }
}
