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
    public class V105ITAdminComputerLabTests
    {
        private string _testDataDir = string.Empty;
        private string _testDbPath = string.Empty;

        private void InitializeTestEnvironment()
        {
            DbEncryptionKeyManager.ClearCache();
            _testDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestDataDir_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDataDir);
            AppPaths.DataDirOverride = _testDataDir;

            _testDbPath = Path.Combine(_testDataDir, "smartclass_test.db");
            AppPaths.DatabaseFile = _testDbPath;
        }

        private void CleanupTestEnvironment()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            DbEncryptionKeyManager.ClearCache();
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
            string dbPath = _testDbPath;
            string dataDir = _testDataDir;

            var thread = new Thread(() =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(dbPath))
                    {
                        AppPaths.DatabaseFile = dbPath;
                    }
                    if (!string.IsNullOrEmpty(dataDir))
                    {
                        AppPaths.DataDirOverride = dataDir;
                    }

                    var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                    SynchronizationContext.SetSynchronizationContext(
                        new System.Windows.Threading.DispatcherSynchronizationContext(dispatcher));

                    InitializeApplicationFull(); action();
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

        private void InvokePrivateMethod(object obj, string methodName, params object[] args)
        {
            var type = obj.GetType();
            var method = type.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method == null) throw new ArgumentException($"Method {methodName} not found on {type.Name}");
            method.Invoke(obj, args);
        }

        private object InvokePrivateMethodWithReturn(object obj, string methodName, params object[] args)
        {
            var type = obj.GetType();
            var method = type.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method == null) throw new ArgumentException($"Method {methodName} not found on {type.Name}");
            return method.Invoke(obj, args)!;
        }

        [Fact]
        public void Test_LoadLabRooms_PopulatesDefaultRooms()
        {
            InitializeTestEnvironment();
            try
            {
                using var db = CreateTempDbContext();
                RunInSTA(() =>
                {
                    var control = new SchoolAdminDashboardControl();
                    InvokePrivateMethod(control, "LoadLabRooms");

                    var cbo = control.FindName("cboLabRoom") as ComboBox;
                    Assert.NotNull(cbo);
                    Assert.Equal(3, cbo.Items.Count);
                    Assert.Equal("LAB01", cbo.Items[0]);
                    Assert.Equal("LAB02", cbo.Items[1]);
                    Assert.Equal("LAB03", cbo.Items[2]);
                    Assert.Equal("LAB01", cbo.SelectedItem);
                });
            }
            finally
            {
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_LoadLabDevices_CreatesDefaultTopologyFile_IfMissing()
        {
            InitializeTestEnvironment();
            try
            {
                using var db = CreateTempDbContext();
                string path = AppPaths.GetTopologyFile("LAB01");
                if (File.Exists(path)) File.Delete(path);

                RunInSTA(() =>
                {
                    var control = new SchoolAdminDashboardControl();
                    InvokePrivateMethod(control, "LoadLabDevices", "LAB01");

                    Assert.True(File.Exists(path));
                    var dg = control.FindName("dgLabDevices") as DataGrid;
                    Assert.NotNull(dg);
                    
                    var list = dg.ItemsSource as List<SchoolAdminDashboardControl.LabDeviceItem>;
                    Assert.NotNull(list);
                    Assert.Equal(23, list.Count); // 1 IB, 1 CAM, 1 Teacher, 20 Students
                });
            }
            finally
            {
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_LoadLabDevices_ReadsValidTopology()
        {
            InitializeTestEnvironment();
            try
            {
                using var db = CreateTempDbContext();
                string path = AppPaths.GetTopologyFile("LAB02");
                var data = new QASmartClass.Models.TopologyData { RoomId = "LAB02" };
                data.Nodes.Add(new QASmartClass.Models.TopologyNode { MachineId = "PC-TEST", DisplayName = "Test Machine", NodeType = "Student", Status = "Online" });
                
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(data));

                RunInSTA(() =>
                {
                    var control = new SchoolAdminDashboardControl();
                    InvokePrivateMethod(control, "LoadLabDevices", "LAB02");

                    var dg = control.FindName("dgLabDevices") as DataGrid;
                    var list = dg.ItemsSource as List<SchoolAdminDashboardControl.LabDeviceItem>;
                    
                    Assert.NotNull(list);
                    Assert.Single(list);
                    Assert.Equal("PC-TEST", list[0].MachineId);
                    Assert.Equal("Test Machine", list[0].DisplayName);
                });
            }
            finally
            {
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_CalculateLabKpi_CountsStatusCorrectly()
        {
            InitializeTestEnvironment();
            try
            {
                using var db = CreateTempDbContext();
                RunInSTA(() =>
                {
                    var control = new SchoolAdminDashboardControl();
                    InvokePrivateMethod(control, "LoadLabDevices", "LAB01");

                    var txtTotal = control.FindName("txtLabTotal") as TextBlock;
                    var txtOnline = control.FindName("txtLabOnline") as TextBlock;
                    var txtOffline = control.FindName("txtLabOffline") as TextBlock;
                    var txtFaulty = control.FindName("txtLabFaulty") as TextBlock;

                    Assert.NotNull(txtTotal);
                    Assert.NotNull(txtOnline);
                    Assert.NotNull(txtOffline);
                    Assert.NotNull(txtFaulty);

                    int total = int.Parse(txtTotal.Text);
                    int online = int.Parse(txtOnline.Text);
                    int offline = int.Parse(txtOffline.Text);
                    int faulty = int.Parse(txtFaulty.Text);

                    Assert.Equal(23, total);
                    Assert.Equal(total, online + offline + faulty);
                });
            }
            finally
            {
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_FilterLabDevices_FiltersListCorrectly()
        {
            InitializeTestEnvironment();
            try
            {
                using var db = CreateTempDbContext();
                RunInSTA(() =>
                {
                    var control = new SchoolAdminDashboardControl();
                    InvokePrivateMethod(control, "LoadLabDevices", "LAB01");

                    var cboFilter = control.FindName("cboStatusFilter") as ComboBox;
                    Assert.NotNull(cboFilter);

                    // Filter Online
                    cboFilter.SelectedIndex = 1; // Online
                    var dg = control.FindName("dgLabDevices") as DataGrid;
                    var list = dg.ItemsSource as List<SchoolAdminDashboardControl.LabDeviceItem>;
                    Assert.All(list, item => Assert.Equal("Online", item.Status));

                    // Filter Offline
                    cboFilter.SelectedIndex = 2; // Offline
                    list = dg.ItemsSource as List<SchoolAdminDashboardControl.LabDeviceItem>;
                    Assert.All(list, item => Assert.Equal("Offline", item.Status));
                });
            }
            finally
            {
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_ExecuteRemoteCommand_IndividualDevice_UpdatesStatusAndJson()
        {
            InitializeTestEnvironment();
            try
            {
                using var db = CreateTempDbContext();
                string path = AppPaths.GetTopologyFile("LAB01");

                RunInSTA(() =>
                {
                    var control = new SchoolAdminDashboardControl();
                    InvokePrivateMethod(control, "LoadLabRooms");
                    
                    // Set RemoteControlMode to AdminOnly and User Role to Admin
                    QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { TeacherCode = "Admin", FullName = "Admin User", Role = "Admin" });

                    // Shutdown PC-01
                    InvokePrivateMethod(control, "ExecuteRemoteCommand", "STUDENT-PC-01", "Shutdown", false);

                    var json = File.ReadAllText(path);
                    var data = System.Text.Json.JsonSerializer.Deserialize<QASmartClass.Models.TopologyData>(json);
                    var node = data.Nodes.First(n => n.MachineId == "STUDENT-PC-01");
                    
                    Assert.Equal("Offline", node.Status);
                });
            }
            finally
            {
                QASmartClass.Staff.Services.StaffSession.Logout();
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_ExecuteRemoteCommand_IndividualDevice_WritesAuditLog()
        {
            InitializeTestEnvironment();
            try
            {
                using var db = CreateTempDbContext();
                RunInSTA(() =>
                {
                    var control = new SchoolAdminDashboardControl();
                    InvokePrivateMethod(control, "LoadLabRooms");
                    
                    QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { TeacherCode = "Admin", FullName = "Admin User", Role = "Admin" });

                    InvokePrivateMethod(control, "ExecuteRemoteCommand", "STUDENT-PC-02", "Shutdown", false);

                    using var verifyDb = new AppDbContext();
                    var logs = verifyDb.AuditLogs.Where(l => l.Action == "REMOTE_COMMAND").ToList();
                    Assert.NotEmpty(logs);
                    Assert.Contains("STUDENT-PC-02", logs[0].Details);
                    Assert.Contains("Shutdown", logs[0].Details);
                });
            }
            finally
            {
                QASmartClass.Staff.Services.StaffSession.Logout();
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_ExecuteRemoteCommand_AllDevices_UpdatesStatusAndJson()
        {
            InitializeTestEnvironment();
            try
            {
                using var db = CreateTempDbContext();
                string path = AppPaths.GetTopologyFile("LAB01");

                RunInSTA(() =>
                {
                    var control = new SchoolAdminDashboardControl();
                    InvokePrivateMethod(control, "LoadLabRooms");
                    
                    QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { TeacherCode = "Admin", FullName = "Admin User", Role = "Admin" });

                    // Lock all
                    InvokePrivateMethod(control, "ExecuteRemoteCommand", "", "Lock", true);

                    var json = File.ReadAllText(path);
                    var data = System.Text.Json.JsonSerializer.Deserialize<QASmartClass.Models.TopologyData>(json);
                    
                    // All students should be Offline (Locked)
                    var studentNodes = data.Nodes.Where(n => n.NodeType == "Student").ToList();
                    Assert.All(studentNodes, n => Assert.Equal("Offline", n.Status));
                });
            }
            finally
            {
                QASmartClass.Staff.Services.StaffSession.Logout();
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_ExecuteRemoteCommand_AllDevices_WritesAuditLog()
        {
            InitializeTestEnvironment();
            try
            {
                using var db = CreateTempDbContext();
                RunInSTA(() =>
                {
                    var control = new SchoolAdminDashboardControl();
                    InvokePrivateMethod(control, "LoadLabRooms");
                    
                    QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { TeacherCode = "Admin", FullName = "Admin User", Role = "Admin" });

                    InvokePrivateMethod(control, "ExecuteRemoteCommand", "", "WOL", true);

                    using var verifyDb = new AppDbContext();
                    var logs = verifyDb.AuditLogs.Where(l => l.Action == "REMOTE_COMMAND").ToList();
                    Assert.NotEmpty(logs);
                    Assert.Contains("đồng loạt", logs[0].Details);
                    Assert.Contains("WOL", logs[0].Details);
                });
            }
            finally
            {
                QASmartClass.Staff.Services.StaffSession.Logout();
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_RemoteControlMode_Disabled_BlocksCommands()
        {
            InitializeTestEnvironment();
            try
            {
                using var db = CreateTempDbContext();
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Lab_RemoteControlMode", Value = "Disabled", Category = "Lab", LastUpdated = DateTime.Now });
                db.SaveChanges();

                RunInSTA(() =>
                {
                    var control = new SchoolAdminDashboardControl();
                    InvokePrivateMethod(control, "LoadLabRooms");

                    QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { TeacherCode = "Admin", FullName = "Admin User", Role = "Admin" });

                    string path = AppPaths.GetTopologyFile("LAB01");
                    var originalJson = File.ReadAllText(path);

                    InvokePrivateMethod(control, "ExecuteRemoteCommand", "STUDENT-PC-01", "Shutdown", false);

                    var currentJson = File.ReadAllText(path);
                    // JSON should be unchanged because command was blocked
                    Assert.Equal(originalJson, currentJson);
                });
            }
            finally
            {
                QASmartClass.Staff.Services.StaffSession.Logout();
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_FaultyDeviceAction_AutoLockAndBanner_UpdatesStatusToFaulty()
        {
            InitializeTestEnvironment();
            try
            {
                using var db = CreateTempDbContext();
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Lab_FaultyDeviceAction", Value = "AutoLockAndBanner", Category = "Lab", LastUpdated = DateTime.Now });
                db.SaveChanges();

                RunInSTA(() =>
                {
                    var control = new SchoolAdminDashboardControl();
                    InvokePrivateMethod(control, "LoadLabRooms");

                    QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { TeacherCode = "Admin", FullName = "Admin User", Role = "Admin" });

                    InvokePrivateMethod(control, "ExecuteRemoteCommand", "STUDENT-PC-03", "Maintenance", false);

                    string path = AppPaths.GetTopologyFile("LAB01");
                    var json = File.ReadAllText(path);
                    var data = System.Text.Json.JsonSerializer.Deserialize<QASmartClass.Models.TopologyData>(json);
                    var node = data.Nodes.First(n => n.MachineId == "STUDENT-PC-03");

                    Assert.Equal("Faulty", node.Status);
                });
            }
            finally
            {
                QASmartClass.Staff.Services.StaffSession.Logout();
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_SaveAndLoad_NewLabMasterSettings()
        {
            InitializeTestEnvironment();
            try
            {
                using var db = CreateTempDbContext();
                RunInSTA(() =>
                {
                    var control = new SchoolAdminDashboardControl();
                    InvokePrivateMethod(control, "LoadSystemSettings");

                    var cmbRemote = control.FindName("cmbLabRemoteControlMode") as ComboBox;
                    var cmbRefresh = control.FindName("cmbLabStatusRefreshInterval") as ComboBox;
                    var cmbAutoPower = control.FindName("cmbLabAutoPowerOffPolicy") as ComboBox;
                    var cmbFaulty = control.FindName("cmbLabFaultyDeviceAction") as ComboBox;

                    Assert.NotNull(cmbRemote);
                    Assert.NotNull(cmbRefresh);
                    Assert.NotNull(cmbAutoPower);
                    Assert.NotNull(cmbFaulty);

                    // Change values
                    cmbRemote.SelectedIndex = 2; // AdminAndTeacher
                    cmbRefresh.SelectedIndex = 2; // 30
                    cmbAutoPower.SelectedIndex = 1; // AfterClassEnd
                    cmbFaulty.SelectedIndex = 1; // AutoLockAndBanner

                    // Save
                    InvokePrivateMethod(control, "BtnSaveSystemSettings_Click", null, null);

                    // Re-load settings in another control instance
                    var control2 = new SchoolAdminDashboardControl();
                    InvokePrivateMethod(control2, "LoadSystemSettings");

                    var cmbRemote2 = control2.FindName("cmbLabRemoteControlMode") as ComboBox;
                    var cmbRefresh2 = control2.FindName("cmbLabStatusRefreshInterval") as ComboBox;
                    var cmbAutoPower2 = control2.FindName("cmbLabAutoPowerOffPolicy") as ComboBox;
                    var cmbFaulty2 = control2.FindName("cmbLabFaultyDeviceAction") as ComboBox;

                    Assert.Equal(2, cmbRemote2.SelectedIndex);
                    Assert.Equal(2, cmbRefresh2.SelectedIndex);
                    Assert.Equal(1, cmbAutoPower2.SelectedIndex);
                    Assert.Equal(1, cmbFaulty2.SelectedIndex);
                });
            }
            finally
            {
                CleanupTestEnvironment();
            }
        }
    }
}
