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
    public class V103ITAdminDiagnosticsTests
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
            var thread = new Thread(() =>
            {
                try
                {
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
            var method = type.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method == null) throw new ArgumentException($"Method {methodName} not found on {type.Name}");
            method.Invoke(obj, args);
        }

        private object InvokePrivateMethodWithReturn(object obj, string methodName, params object[] args)
        {
            var type = obj.GetType();
            var method = type.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method == null) throw new ArgumentException($"Method {methodName} not found on {type.Name}");
            return method.Invoke(obj, args)!;
        }

        [Fact]
        public void Test_ScanCrashLogs_GeneratesMockCrashes_IfNoneExist()
        {
            InitializeTestEnvironment();
            try
            {
                using (var db = CreateTempDbContext()) { }

                RunInSTA(() =>
                {
                    QAVendorAdminControl.IsTesting = true;
                    var control = new QAVendorAdminControl();

                    string crashDir = AppPaths.CrashesDir;
                    if (Directory.Exists(crashDir))
                    {
                        foreach (var file in Directory.GetFiles(crashDir))
                            File.Delete(file);
                    }

                    InvokePrivateMethod(control, "ScanCrashLogs");

                    var dg = control.FindName("dgDiagnosticLogs") as DataGrid;
                    Assert.NotNull(dg);
                    var items = dg.ItemsSource as List<QAVendorAdminControl.DiagnosticLogInfo>;
                    Assert.NotNull(items);
                    Assert.NotEmpty(items);
                    
                    Assert.True(Directory.GetFiles(crashDir).Length > 0);
                });
            }
            finally
            {
                QAVendorAdminControl.IsTesting = false;
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_CategoryFilter_FiltersDataGridCorrectly()
        {
            InitializeTestEnvironment();
            try
            {
                using (var db = CreateTempDbContext()) { }

                RunInSTA(() =>
                {
                    QAVendorAdminControl.IsTesting = true;
                    var control = new QAVendorAdminControl();

                    InvokePrivateMethod(control, "ScanCrashLogs");

                    var dg = control.FindName("dgDiagnosticLogs") as DataGrid;
                    var cboCategory = control.FindName("cboCategoryFilter") as ComboBox;
                    Assert.NotNull(dg);
                    Assert.NotNull(cboCategory);

                    cboCategory.SelectedIndex = 1; // Database

                    var items = dg.ItemsSource as List<QAVendorAdminControl.DiagnosticLogInfo>;
                    Assert.NotNull(items);
                    Assert.All(items, item => Assert.Equal("Database", item.Category));
                });
            }
            finally
            {
                QAVendorAdminControl.IsTesting = false;
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_SeverityFilter_FiltersDataGridCorrectly()
        {
            InitializeTestEnvironment();
            try
            {
                using (var db = CreateTempDbContext()) { }

                RunInSTA(() =>
                {
                    QAVendorAdminControl.IsTesting = true;
                    var control = new QAVendorAdminControl();

                    InvokePrivateMethod(control, "ScanCrashLogs");

                    var dg = control.FindName("dgDiagnosticLogs") as DataGrid;
                    var cboSeverity = control.FindName("cboSeverityFilter") as ComboBox;
                    Assert.NotNull(dg);
                    Assert.NotNull(cboSeverity);

                    cboSeverity.SelectedIndex = 2; // WARNING

                    var items = dg.ItemsSource as List<QAVendorAdminControl.DiagnosticLogInfo>;
                    Assert.NotNull(items);
                    Assert.All(items, item => Assert.Equal("WARNING", item.Level));
                });
            }
            finally
            {
                QAVendorAdminControl.IsTesting = false;
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_HeuristicAdvisor_ProvidesCorrectAdviceForSqliteError()
        {
            InitializeTestEnvironment();
            try
            {
                RunInSTA(() =>
                {
                    QAVendorAdminControl.IsTesting = true;
                    var control = new QAVendorAdminControl();

                    var info = (QAVendorAdminControl.DiagnosticLogInfo)InvokePrivateMethodWithReturn(
                        control, 
                        "ParseDiagnosticLine", 
                        "Microsoft.Data.Sqlite.SqliteException: database is locked", 
                        "Full stack trace", 
                        DateTime.Now, 
                        "test_file.txt"
                    );

                    Assert.Equal("Database", info.Category);
                    Assert.Contains("SQLite", info.Advice);
                    Assert.Contains("WAL", info.Advice);
                });
            }
            finally
            {
                QAVendorAdminControl.IsTesting = false;
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_HeuristicAdvisor_ProvidesCorrectAdviceForNetworkError()
        {
            InitializeTestEnvironment();
            try
            {
                RunInSTA(() =>
                {
                    QAVendorAdminControl.IsTesting = true;
                    var control = new QAVendorAdminControl();

                    var info = (QAVendorAdminControl.DiagnosticLogInfo)InvokePrivateMethodWithReturn(
                        control, 
                        "ParseDiagnosticLine", 
                        "System.Net.Sockets.SocketException: Connection timed out", 
                        "Full stack trace", 
                        DateTime.Now, 
                        "test_file.txt"
                    );

                    Assert.Equal("Network", info.Category);
                    Assert.Contains("Router", info.Advice);
                    Assert.Contains("LAN", info.Advice);
                });
            }
            finally
            {
                QAVendorAdminControl.IsTesting = false;
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_SaveDiagnosticsSettings_SavesToDatabaseCorrectly()
        {
            InitializeTestEnvironment();
            try
            {
                using (var db = CreateTempDbContext()) { }

                RunInSTA(() =>
                {
                    QAVendorAdminControl.IsTesting = true;
                    var control = new QAVendorAdminControl();

                    InvokePrivateMethod(control, "LoadDiagnosticsSettings");

                    var cboLevel = control.FindName("cboDiagReportingLevel") as ComboBox;
                    var cboNotify = control.FindName("cboDiagAutoNotification") as ComboBox;
                    Assert.NotNull(cboLevel);
                    Assert.NotNull(cboNotify);

                    cboLevel.SelectedIndex = 2; // ErrorOnly
                    cboNotify.SelectedIndex = 0; // Enabled

                    InvokePrivateMethod(control, "BtnSaveDiagSettings_Click", null, null);

                    using (var db = new AppDbContext())
                    {
                        var levelVal = db.SystemSettings.Find("IT_Diagnostics_ReportingLevel")?.Value;
                        var notifyVal = db.SystemSettings.Find("IT_Diagnostics_AutoNotification")?.Value;

                        Assert.Equal("ErrorOnly", levelVal);
                        Assert.Equal("Enabled", notifyVal);
                    }
                });
            }
            finally
            {
                QAVendorAdminControl.IsTesting = false;
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_ExportDiagnostics_CreatesOutputFolder()
        {
            InitializeTestEnvironment();
            try
            {
                using (var db = CreateTempDbContext()) { }

                RunInSTA(() =>
                {
                    QAVendorAdminControl.IsTesting = true;
                    var control = new QAVendorAdminControl();

                    InvokePrivateMethod(control, "ScanCrashLogs");

                    string tempDir = AppPaths.TempDir;
                    
                    if (Directory.Exists(tempDir))
                    {
                        foreach (var dir in Directory.GetDirectories(tempDir))
                        {
                            try { Directory.Delete(dir, true); } catch { }
                        }
                    }

                    InvokePrivateMethod(control, "BtnExportDiagnostics_Click", null, null);

                    var subdirs = Directory.GetDirectories(tempDir);
                    Assert.NotEmpty(subdirs);
                    Assert.Contains("QADiagnostics_Export_", subdirs[0]);
                    
                    var files = Directory.GetFiles(subdirs[0]);
                    Assert.NotEmpty(files);
                });
            }
            finally
            {
                QAVendorAdminControl.IsTesting = false;
                CleanupTestEnvironment();
            }
        }

        [Fact]
        public void Test_LoadDiagnosticsSettings_PopulatesUIComboBoxes()
        {
            InitializeTestEnvironment();
            try
            {
                using (var db = CreateTempDbContext())
                {
                    db.SystemSettings.Add(new SystemSetting { Id = "IT_Diagnostics_ReportingLevel", Value = "ErrorOnly", Category = "IT" });
                    db.SystemSettings.Add(new SystemSetting { Id = "IT_Diagnostics_AutoNotification", Value = "Enabled", Category = "IT" });
                    db.SaveChanges();
                }

                RunInSTA(() =>
                {
                    QAVendorAdminControl.IsTesting = true;
                    var control = new QAVendorAdminControl();

                    InvokePrivateMethod(control, "LoadDiagnosticsSettings");

                    var cboLevel = control.FindName("cboDiagReportingLevel") as ComboBox;
                    var cboNotify = control.FindName("cboDiagAutoNotification") as ComboBox;

                    Assert.NotNull(cboLevel);
                    Assert.NotNull(cboNotify);

                    Assert.Equal(2, cboLevel.SelectedIndex); // ErrorOnly
                    Assert.Equal(0, cboNotify.SelectedIndex); // Enabled
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
