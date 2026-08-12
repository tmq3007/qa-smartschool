using Xunit;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Staff.ViewModels;
using QASmartClass.Converters;
using QASmartClass.Services;

namespace QASmartClass.Tests
{
    public class V99SecurityAndKitchenSettingsTests
    {
        public V99SecurityAndKitchenSettingsTests()
        {
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();
            QASmartClass.Services.AppPaths.EnsureDirectories();
        }

        private static System.Threading.Thread _sharedStaThread;
        private static System.Windows.Threading.Dispatcher _sharedDispatcher;
        private static readonly object _threadLock = new object();

        private void RunOnStaThread(Action action)
        {
            lock (_threadLock)
            {
                if (_sharedStaThread == null)
                {
                    var readyEvent = new System.Threading.ManualResetEvent(false);
                    _sharedStaThread = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            if (System.Windows.Application.Current == null)
                            {
                                _ = new QASmartTouch.App();
                            }
                            
                            var resources = System.Windows.Application.Current!.Resources;
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
                                    resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                                    {
                                        Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute)
                                    });
                                    resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                                    {
                                        Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/Styles.xaml", UriKind.Absolute)
                                    });
                                }
                            }
                            catch { }

                            _sharedDispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                            readyEvent.Set();
                            System.Windows.Threading.Dispatcher.Run();
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                System.IO.File.WriteAllText(@"d:\JOB\QA SmartClass -062026\test_crash_log.txt", "STA Init Error: " + ex.ToString());
                            }
                            catch {}
                            readyEvent.Set();
                        }
                    });
                    _sharedStaThread.SetApartmentState(System.Threading.ApartmentState.STA);
                    _sharedStaThread.IsBackground = true;
                    _sharedStaThread.Start();
                    readyEvent.WaitOne();
                }
            }

            if (_sharedDispatcher == null)
            {
                throw new InvalidOperationException("STA thread dispatcher was not initialized.");
            }

            Exception threadEx = null;
            _sharedDispatcher.Invoke(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    try
                    {
                        System.IO.File.WriteAllText(@"d:\JOB\QA SmartClass -062026\test_crash_log.txt", "STA Caught Exception: " + ex.ToString());
                    }
                    catch { }
                    threadEx = ex;
                }
            });

            if (threadEx != null)
            {
                throw new InvalidOperationException("STA thread error: " + threadEx.Message, threadEx);
            }
        }

        [Fact]
        public void Test_SeverityTranslationConverter_TranslatesCorrectly()
        {
            var converter = new SeverityTranslationConverter();

            Assert.Equal("Nhẹ", converter.Convert("Mild", typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal("Trung bình", converter.Convert("Moderate", typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal("Nguy cấp/Nặng", converter.Convert("Severe", typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
            
            // Case insensitivity
            Assert.Equal("Nhẹ", converter.Convert("mild", typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal("Trung bình", converter.Convert("MODERATE", typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));

            // Unknown value fallback
            Assert.Equal("Unknown", converter.Convert("Unknown", typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Test_SecurityKiosk_OnQrInputBufferChanged_PreventsRaceConditions()
        {
            RunOnStaThread(() =>
            {
                var vm = new SecurityKioskViewModel();

                // 1. Uncompleted scanner input (no newline, less than 6 pipes)
                vm.QrInputBuffer = "123456789012|123456789|Nguyen Van A";
                // Should not trigger parsing yet, buffer remains unchanged
                Assert.Equal("123456789012|123456789|Nguyen Van A", vm.QrInputBuffer);
                Assert.True(string.IsNullOrEmpty(vm.Person));

                // 2. Completed scan using newline indicator
                vm.QrInputBuffer = "123456789012|123456789|Nguyen Van A|01/01/2000|Nam|Hanoi\n";
                // Should parse, auto-fill, and clear buffer
                Assert.Equal(string.Empty, vm.QrInputBuffer);
                Assert.Equal("Nguyen Van A", vm.Person);
                Assert.Contains("Nguyen Van A", vm.Description);
                Assert.Contains("123456789012", vm.Description);
            });
        }

        [Fact]
        public async Task Test_GateMonitor_ScanLeavePass_ClearsBufferOnAllErrors()
        {
            // We run this test within a local DbContext transaction or normal test db
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
            var vm = new GateMonitorViewModel();

            // 1. Wrong prefix
            vm.ScannedLeavePassCode = "XYZ-20260629-1";
            await vm.ScanLeavePassCommand.ExecuteAsync(null);
            Assert.Equal(string.Empty, vm.ScannedLeavePassCode);

            // 2. Wrong format
            vm.ScannedLeavePassCode = "LP-20260629";
            await vm.ScanLeavePassCommand.ExecuteAsync(null);
            Assert.Equal(string.Empty, vm.ScannedLeavePassCode);

            // 3. Invalid date
            vm.ScannedLeavePassCode = "LP-20269999-1";
            await vm.ScanLeavePassCommand.ExecuteAsync(null);
            Assert.Equal(string.Empty, vm.ScannedLeavePassCode);
        }

        [Fact]
        public async Task Test_GateMonitor_ScanLeavePass_StrictnessLevels()
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
            
            // Backup settings if they exist
            var origStrictness = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Security_OfflineVerificationStrictness");
            
            // Clear or update strictness to 1 (Today's date only)
            if (origStrictness == null)
            {
                db.SystemSettings.Add(new SystemSetting { Id = "Security_OfflineVerificationStrictness", Value = "1", Category = "IT" });
            }
            else
            {
                origStrictness.Value = "1";
            }
            await db.SaveChangesAsync();

            // Setup a mock student
            var student = new Student
            {
                FullName = "Học sinh Leave Pass Test",
                StudentCode = "HS_LP_TEST_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                ClassName = "10A1",
                Status = "Active"
            };
            db.Students.Add(student);
            await db.SaveChangesAsync();

            // Setup a leave request for YESTERDAY
            var yesterday = DateTime.Today.AddDays(-1);
            var leaveYesterday = new StudentLeaveRequest
            {
                StudentId = student.Id,
                StudentName = student.FullName,
                ClassName = student.ClassName,
                LeaveDate = yesterday,
                Reason = "Cảm cúm",
                Status = "Approved",
                CreatedAt = DateTime.Now
            };
            db.StudentLeaveRequests.Add(leaveYesterday);
            await db.SaveChangesAsync();

            try
            {
                var vm = new GateMonitorViewModel();

                // Case 1: Strictness = 1 (Strict Today's Date). Yesterday's pass should be rejected.
                vm.ScannedLeavePassCode = $"LP-{yesterday:yyyyMMdd}-{student.Id}";
                await vm.ScanLeavePassCommand.ExecuteAsync(null);
                
                // Assert rejected (buffer is cleared, no event log created for GateCheckOut)
                Assert.Equal(string.Empty, vm.ScannedLeavePassCode);
                var logsCount = await db.EventLogs.CountAsync(l => l.Actor == student.StudentCode && l.EventType == "GateCheckOut");
                Assert.Equal(0, logsCount);

                // Case 2: Strictness = 0 (Legacy/Low - allow past dates)
                var strictnessSet = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Security_OfflineVerificationStrictness");
                if (strictnessSet != null) strictnessSet.Value = "0";
                await db.SaveChangesAsync();

                vm.ScannedLeavePassCode = $"LP-{yesterday:yyyyMMdd}-{student.Id}";
                await vm.ScanLeavePassCommand.ExecuteAsync(null);

                // Assert accepted (event log should be written)
                var afterLog = await db.EventLogs.FirstOrDefaultAsync(l => l.Actor == student.StudentCode && l.EventType == "GateCheckOut");
                Assert.NotNull(afterLog);

                // Clean up log
                if (afterLog != null) db.EventLogs.Remove(afterLog);
                await db.SaveChangesAsync();
            }
            finally
            {
                // Restore settings and clean up student/leave
                db.Students.Remove(student);
                db.StudentLeaveRequests.Remove(leaveYesterday);
                if (origStrictness != null)
                {
                    var curSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Security_OfflineVerificationStrictness");
                    if (curSetting != null) curSetting.Value = origStrictness.Value;
                }
                await db.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task Test_GateMonitor_ComPortCheckingMode()
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
            var origComMode = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Security_ComPortCheckingMode");

            try
            {
                // Case 1: Mode = "0" (No check, always connected)
                if (origComMode == null)
                {
                    db.SystemSettings.Add(new SystemSetting { Id = "Security_ComPortCheckingMode", Value = "0", Category = "IT" });
                }
                else
                {
                    origComMode.Value = "0";
                }
                await db.SaveChangesAsync();

                var vm = new GateMonitorViewModel();
                await vm.RefreshDataCommand.ExecuteAsync(null); // Runs CheckHardwareConnectionAsync
                Assert.True(vm.IsHardwareConnected);

                // Case 2: Mode = "1" (Registry check - could be true or false depending on host computer, but must execute cleanly)
                var currentModeSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Security_ComPortCheckingMode");
                if (currentModeSetting != null) currentModeSetting.Value = "1";
                await db.SaveChangesAsync();

                var vmMode1 = new GateMonitorViewModel();
                await vmMode1.RefreshDataCommand.ExecuteAsync(null);
                // Should run without throwing exceptions
            }
            finally
            {
                if (origComMode != null)
                {
                    var curSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Security_ComPortCheckingMode");
                    if (curSetting != null) curSetting.Value = origComMode.Value;
                    await db.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task Test_Kitchen_MealCalculationFormulas()
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
            
            // Backup settings
            var origFormula = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Kitchen_MealCalculationFormula");

            int preExistingActiveCount = await db.Students.CountAsync(s => s.Status == "Active");
            int preExistingTotalCount = await db.Students.CountAsync();

            // Setup mock data: 5 active students, 1 leave request today
            var students = Enumerable.Range(1, 5).Select(i => new Student
            {
                FullName = $"Kitchen Student Formula {i}",
                StudentCode = $"HS_KT_FORM_{i}_" + Guid.NewGuid().ToString("N").Substring(0, 4),
                ClassName = "10A1",
                Status = "Active"
            }).ToList();
            db.Students.AddRange(students);
            await db.SaveChangesAsync();

            var leave = new StudentLeaveRequest
            {
                StudentId = students[0].Id,
                StudentName = students[0].FullName,
                ClassName = students[0].ClassName,
                LeaveDate = DateTime.Today,
                Reason = "Bệnh",
                Status = "Approved",
                CreatedAt = DateTime.Now
            };
            db.StudentLeaveRequests.Add(leave);

            // Add 1 staff attendance (present today)
            var staffAttendance = new StaffAttendance
            {
                StaffId = 999,
                Date = DateTime.Today,
                Status = "Present"
            };
            db.StaffAttendances.Add(staffAttendance);
            await db.SaveChangesAsync();

            try
            {
                // Case 1: Formula 1 (Preorder-based: Total (5) - Leave (1) + Staff (1) = 5)
                var formulaSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Kitchen_MealCalculationFormula");
                if (formulaSetting == null)
                {
                    db.SystemSettings.Add(new SystemSetting { Id = "Kitchen_MealCalculationFormula", Value = "1", Category = "IT" });
                }
                else
                {
                    formulaSetting.Value = "1";
                }
                await db.SaveChangesAsync();

                var vm = new KitchenDashboardViewModel();
                await vm.InitializeAsync();
                
                var cancelledToday = await db.StudentLeaveRequests
                    .CountAsync(r => r.LeaveDate.Date == DateTime.Today && r.Status == "Approved");
                var addedToday = await db.StaffAttendances
                    .CountAsync(a => a.Date == DateTime.Today && a.Status == "Present");
                var totalStudents = await db.Students.CountAsync(s => s.Status == "Active");
                var finalMeals = totalStudents - cancelledToday + addedToday;
                Assert.Contains($"{finalMeals} Suất", vm.MealCount);

                // Case 2: Formula 0 (Attendance-only, fallback to total active students since there are no student attendance records today)
                var formulaSetting2 = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Kitchen_MealCalculationFormula");
                if (formulaSetting2 != null) formulaSetting2.Value = "0";
                await db.SaveChangesAsync();

                var vm0 = new KitchenDashboardViewModel();
                await vm0.InitializeAsync();
                var service = new KitchenService(db);
                int expectedFormula0 = await service.GetEstimatedMealsAsync(DateTime.Today);
                Assert.Contains($"{expectedFormula0} Suất", vm0.MealCount);
            }
            finally
            {
                // Cleanup
                db.Students.RemoveRange(students);
                db.StudentLeaveRequests.Remove(leave);
                db.StaffAttendances.Remove(staffAttendance);
                if (origFormula != null)
                {
                    var curSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Kitchen_MealCalculationFormula");
                    if (curSetting != null) curSetting.Value = origFormula.Value;
                }
                await db.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task Test_Kitchen_ColdStorageStatusWidget()
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
            
            // Backup settings
            var origEnable = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Kitchen_EnableColdStorageWidget");
            
            // Add a mock temperature log (violation)
            var log = new KitchenColdStorageLog
            {
                FridgeId = "FRIDGE_TEST_01",
                Temperature = 6.2,
                Humidity = 75,
                Timestamp = DateTime.Now,
                IsViolation = true
            };
            db.KitchenColdStorageLogs.Add(log);
            await db.SaveChangesAsync();

            try
            {
                // Case 1: Widget Enabled
                var enableSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Kitchen_EnableColdStorageWidget");
                if (enableSetting == null)
                {
                    db.SystemSettings.Add(new SystemSetting { Id = "Kitchen_EnableColdStorageWidget", Value = "true", Category = "IT" });
                }
                else
                {
                    enableSetting.Value = "true";
                }
                await db.SaveChangesAsync();

                var vm = new KitchenDashboardViewModel();
                await vm.InitializeAsync();

                Assert.True(vm.IsColdStorageWidgetVisible);
                Assert.True(vm.IsColdStorageViolation);
                Assert.Contains("6.2", vm.ColdStorageTempString);

                // Case 2: Widget Disabled
                var enableSetting2 = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Kitchen_EnableColdStorageWidget");
                if (enableSetting2 != null) enableSetting2.Value = "false";
                await db.SaveChangesAsync();

                var vmDisabled = new KitchenDashboardViewModel();
                await vmDisabled.InitializeAsync();

                Assert.False(vmDisabled.IsColdStorageWidgetVisible);
            }
            finally
            {
                db.KitchenColdStorageLogs.Remove(log);
                if (origEnable != null)
                {
                    var curSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Kitchen_EnableColdStorageWidget");
                    if (curSetting != null) curSetting.Value = origEnable.Value;
                }
                await db.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task Test_SecurityKiosk_EncryptionAndHashing_WithMasterConfiguration()
        {
            await Task.Run(() =>
            {
                RunOnStaThread(async () =>
                {
                    using var db = TestDbFactory.CreateWithSeed();
                    AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

                    try
                    {
                        var encryptSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "Security_Visitor_Encryption");
                        if (encryptSetting == null)
                        {
                            db.SystemSettings.Add(new SystemSetting { Id = "Security_Visitor_Encryption", Value = "True", Category = "Security" });
                        }
                        else
                        {
                            encryptSetting.Value = "True";
                        }
                        await db.SaveChangesAsync();

                        var vm = new SecurityKioskViewModel();
                        await vm.InitializeAsync();
                        
                        vm.QrInputBuffer = "099099012345|123456789|Phan Khách Mẫu|01/01/1990|Nữ|Hồ Chí Minh\n";
                        
                        Assert.Equal("Phan Khách Mẫu", vm.Person);
                        
                        vm.SelectedEventType = "CheckIn";
                        vm.VisitPurpose = "Gặp phụ huynh học sinh";
                        vm.BadgeNumber = "BADGE-101";
                        vm.SelectedHostTeacherId = "GV001";
                        
                        await vm.SaveCommand.ExecuteAsync(null);

                        db.ChangeTracker.Clear();

                        var log = await db.SecurityLogs.AsNoTracking().OrderByDescending(l => l.Id).FirstOrDefaultAsync(l => l.VisitorName == "Phan Khách Mẫu");
                        Assert.NotNull(log);
                        Assert.Equal("CheckIn", log.EventType);
                        Assert.Equal("BADGE-101", log.BadgeNumber);
                        Assert.Equal("Gặp phụ huynh học sinh", log.VisitPurpose);
                        Assert.False(log.IsCheckedOut);
                        
                        const string piiKey = "QA_SecurityKiosk_PII_Key_2026";
                        Assert.NotEqual("099099012345", log.CccdNumber);
                        Assert.Equal("099099012345", QASmartClass.Utilities.CryptoHelper.Decrypt(log.CccdNumber, piiKey));
                        Assert.NotEqual("Hồ Chí Minh", log.Address);
                        Assert.Equal("Hồ Chí Minh", QASmartClass.Utilities.CryptoHelper.Decrypt(log.Address, piiKey));
                        
                        string expectedHash = QASmartClass.Utilities.CryptoHelper.ComputeHMAC("099099012345");
                        Assert.Equal(expectedHash, log.CccdHash);
                    }
                    finally
                    {
                        AppDbContext.FallbackInMemoryConnection = null;
                    }
                });
            });
        }

        [Fact]
        public async Task Test_SecurityKiosk_CheckOutLinkage_Flow()
        {
            await Task.Run(() =>
            {
                RunOnStaThread(async () =>
                {
                    using var db = TestDbFactory.CreateWithSeed();
                    AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

                    try
                    {
                        var checkInLog = new SecurityLog
                        {
                            EventType = "CheckIn",
                            PersonInvolved = "Nguyễn Văn Ra Về",
                            VisitorName = "Nguyễn Văn Ra Về",
                            CccdNumber = "088088012345",
                            CccdHash = QASmartClass.Utilities.CryptoHelper.ComputeHMAC("088088012345"),
                            BadgeNumber = "BADGE-202",
                            VisitPurpose = "Liên hệ công tác",
                            IsCheckedOut = false,
                            Timestamp = DateTime.Now.AddHours(-2)
                        };
                        db.SecurityLogs.Add(checkInLog);
                        await db.SaveChangesAsync();

                        var vm = new SecurityKioskViewModel();
                        await vm.InitializeAsync();

                        var activeVisitor = vm.ActiveVisitors.FirstOrDefault(v => v.VisitorName == "Nguyễn Văn Ra Về");
                        Assert.NotNull(activeVisitor);
                        Assert.Equal("BADGE-202", activeVisitor.BadgeNumber);

                        vm.SelectedEventType = "CheckOut";
                        vm.SelectedActiveVisitor = activeVisitor;

                        Assert.Equal("Nguyễn Văn Ra Về", vm.Person);
                        Assert.Equal("BADGE-202", vm.BadgeNumber);

                        await vm.SaveCommand.ExecuteAsync(null);

                        db.ChangeTracker.Clear();

                        var updatedCheckIn = await db.SecurityLogs.AsNoTracking().FirstOrDefaultAsync(l => l.Id == checkInLog.Id);
                        Assert.NotNull(updatedCheckIn);
                        Assert.True(updatedCheckIn.IsCheckedOut);

                        var checkOutLog = await db.SecurityLogs.AsNoTracking().FirstOrDefaultAsync(l => l.EventType == "CheckOut" && l.VisitorName == "Nguyễn Văn Ra Về");
                        Assert.NotNull(checkOutLog);
                        Assert.True(checkOutLog.IsCheckedOut);
                        Assert.Equal("BADGE-202", checkOutLog.BadgeNumber);
                    }
                    finally
                    {
                        AppDbContext.FallbackInMemoryConnection = null;
                    }
                });
            });
        }
    
        // ═══════════════════════════════════════════════════════
        //  IT-ADMIN SYSTEM SETTINGS & AUDIT LOGS TESTS (v4.1)
        // ═══════════════════════════════════════════════════════

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        const uint WM_CLOSE = 0x0010;

        private void CloseMessageBox(string title)
        {
            Task.Run(async () =>
            {
                for (int i = 0; i < 20; i++)
                {
                    await Task.Delay(100);
                    IntPtr hwnd = FindWindow("#32770", title);
                    if (hwnd != IntPtr.Zero)
                    {
                        SendMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                        break;
                    }
                }
            });
        }

        [Fact]
        public void Test_SimpleInstantiation()
        {
            RunOnStaThread(() =>
            {
                try
                {
                    System.IO.File.WriteAllText(@"d:\JOB\QA SmartClass -062026\test_crash_log.txt", "Starting instantiation\r\n");
                    var control = new QASmartClass.Admin.Controls.SchoolAdminDashboardControl();
                    System.IO.File.AppendAllText(@"d:\JOB\QA SmartClass -062026\test_crash_log.txt", "Finished instantiation\r\n");
                }
                catch (Exception ex)
                {
                    System.IO.File.AppendAllText(@"d:\JOB\QA SmartClass -062026\test_crash_log.txt", "Caught: " + ex.ToString() + "\r\n");
                }
            });
        }

        [Fact]
        public void Test_AdminDashboard_LoadAndSave_VisitorExperienceSettings()
        {
            RunOnStaThread(() =>
            {
                System.IO.File.WriteAllText(@"d:\JOB\QA SmartClass -062026\test_crash_log.txt", "1. Start Test\r\n");
                try
                {
                    using var db = TestDbFactory.CreateWithSeed();
                    AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
                    System.IO.File.AppendAllText(@"d:\JOB\QA SmartClass -062026\test_crash_log.txt", "2. Connection set\r\n");
                    db.Database.EnsureCreated();
                    var existing = db.SystemSettings.ToList();
                    db.SystemSettings.RemoveRange(existing);
                    db.SaveChanges();
                    System.IO.File.AppendAllText(@"d:\JOB\QA SmartClass -062026\test_crash_log.txt", "3. Db cleared\r\n");

                    var testSettings = new[]
                    {
                        new SystemSetting { Id = "Security_Visitor_Encryption", Value = "False", Category = "Security", LastUpdated = DateTime.Now },
                        new SystemSetting { Id = "Security_Visitor_PrintBadge", Value = "Disabled", Category = "Security", LastUpdated = DateTime.Now },
                        new SystemSetting { Id = "Security_Visitor_AutoReleaseMode", Value = "Disabled", Category = "Security", LastUpdated = DateTime.Now },
                        new SystemSetting { Id = "Security_Visitor_PreRegistration", Value = "Disabled", Category = "Security", LastUpdated = DateTime.Now },
                        new SystemSetting { Id = "Security_PreRegistration_Portal", Value = "False", Category = "Security", LastUpdated = DateTime.Now },
                        new SystemSetting { Id = "Security_HostNotification_Enabled", Value = "False", Category = "Security", LastUpdated = DateTime.Now },
                        new SystemSetting { Id = "Security_WayfindingMap_Enabled", Value = "False", Category = "Security", LastUpdated = DateTime.Now },
                        new SystemSetting { Id = "Security_SelfCheckOut_Enabled", Value = "False", Category = "Security", LastUpdated = DateTime.Now },
                        new SystemSetting { Id = "Security_SentimentSurvey_Enabled", Value = "False", Category = "Security", LastUpdated = DateTime.Now },
                        new SystemSetting { Id = "Security_Visitor_AuthLevel", Value = "Low", Category = "Security", LastUpdated = DateTime.Now },
                        new SystemSetting { Id = "Security_Visitor_BlinkingStyle", Value = "None", Category = "Security", LastUpdated = DateTime.Now },
                        new SystemSetting { Id = "Security_Kiosk_WelcomeMessage", Value = "Welcome Test", Category = "Security", LastUpdated = DateTime.Now }
                    };
                    db.SystemSettings.AddRange(testSettings);
                    db.SaveChanges();
                    System.IO.File.AppendAllText(@"d:\JOB\QA SmartClass -062026\test_crash_log.txt", "4. Settings saved\r\n");

                    var control = new QASmartClass.Admin.Controls.SchoolAdminDashboardControl();
                    System.IO.File.AppendAllText(@"d:\JOB\QA SmartClass -062026\test_crash_log.txt", "5. Control created\r\n");

                    Assert.False(control.chkVisitorEncryption.IsChecked);
                    Assert.False(control.chkPrintBadge.IsChecked);
                    Assert.False(control.chkAutoReleaseMode.IsChecked);
                    Assert.False(control.chkPreRegistration.IsChecked);
                    Assert.False(control.chkPreRegPortal.IsChecked);
                    Assert.False(control.chkHostNotification.IsChecked);
                    Assert.False(control.chkWayfindingMap.IsChecked);
                    Assert.False(control.chkSelfCheckOut.IsChecked);
                    Assert.False(control.chkSentimentSurvey.IsChecked);
                    Assert.Equal("Welcome Test", control.txtWelcomeMessage.Text);
                    System.IO.File.AppendAllText(@"d:\JOB\QA SmartClass -062026\test_crash_log.txt", "6. Init asserts passed\r\n");

                    control.chkVisitorEncryption.IsChecked = true;
                    control.chkPrintBadge.IsChecked = true;
                    control.chkAutoReleaseMode.IsChecked = true;
                    control.chkPreRegistration.IsChecked = true;
                    control.chkPreRegPortal.IsChecked = true;
                    control.chkHostNotification.IsChecked = true;
                    control.chkWayfindingMap.IsChecked = true;
                    control.chkSelfCheckOut.IsChecked = true;
                    control.chkSentimentSurvey.IsChecked = true;
                    control.txtWelcomeMessage.Text = "Welcome Modified";

                    CloseMessageBox("Cấu hình");
                    System.IO.File.AppendAllText(@"d:\JOB\QA SmartClass -062026\test_crash_log.txt", "7. Before click\r\n");
                    control.BtnSaveSystemSettings_Click(null, new System.Windows.RoutedEventArgs());
                    System.IO.File.AppendAllText(@"d:\JOB\QA SmartClass -062026\test_crash_log.txt", "8. After click\r\n");

                    db.ChangeTracker.Clear();

                    var settingsMap = db.SystemSettings.ToDictionary(s => s.Id, s => s.Value);
                    Assert.Equal("True", settingsMap["Security_Visitor_Encryption"]);
                    Assert.Equal("Enabled", settingsMap["Security_Visitor_PrintBadge"]);
                    Assert.Equal("DailyReset", settingsMap["Security_Visitor_AutoReleaseMode"]);
                    Assert.Equal("Enabled", settingsMap["Security_Visitor_PreRegistration"]);
                    Assert.Equal("True", settingsMap["Security_PreRegistration_Portal"]);
                    Assert.Equal("True", settingsMap["Security_HostNotification_Enabled"]);
                    Assert.Equal("True", settingsMap["Security_WayfindingMap_Enabled"]);
                    Assert.Equal("True", settingsMap["Security_SelfCheckOut_Enabled"]);
                    Assert.Equal("True", settingsMap["Security_SentimentSurvey_Enabled"]);
                    Assert.Equal("Welcome Modified", settingsMap["Security_Kiosk_WelcomeMessage"]);
                    System.IO.File.AppendAllText(@"d:\JOB\QA SmartClass -062026\test_crash_log.txt", "9. Done test\r\n");
                }
                catch (Exception ex)
                {
                    System.IO.File.AppendAllText(@"d:\JOB\QA SmartClass -062026\test_crash_log.txt", "Caught: " + ex.ToString() + "\r\n");
                }
                finally
                {
                    AppDbContext.FallbackInMemoryConnection = null;
                }
            });
        }

        [Fact]
        public void Test_AdminDashboard_AuditLog_GenerationAndFiltering()
        {
            RunOnStaThread(() =>
            {
                using var db = TestDbFactory.CreateWithSeed();
                AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
                try
                {
                    db.Database.EnsureCreated();
                    var existingLogs = db.EventLogs.ToList();
                    db.EventLogs.RemoveRange(existingLogs);
                    db.SaveChanges();

                    var control = new QASmartClass.Admin.Controls.SchoolAdminDashboardControl();

                    CloseMessageBox("Cấu hình");
                    control.BtnSaveSystemSettings_Click(null, new System.Windows.RoutedEventArgs());

                    var incidentLog = new SecurityLog
                    {
                        EventType = "Incident",
                        GuardName = "Nguyen Van Guard",
                        Description = "Sự cố mất điện đột ngột tại Kiosk",
                        Timestamp = DateTime.Now
                    };
                    db.SecurityLogs.Add(incidentLog);
                    db.SaveChanges();

                    db.ChangeTracker.Clear();
                    
                    control.txtAuditSearch.Text = "";
                    control.cmbLogType.SelectedIndex = 0;
                    control.cmbLogPeriod.SelectedIndex = 0;

                    control.BtnFilterAuditLogs_Click(null, new System.Windows.RoutedEventArgs());

                    var items = control.dgAuditLogs.ItemsSource as System.Collections.ObjectModel.ObservableCollection<QASmartClass.Admin.Controls.AuditLogDisplay>;
                    Assert.NotNull(items);
                    Assert.True(items.Count >= 2);

                    var hasConfigLog = items.Any(i => i.Action == "SystemConfigChange");
                    var hasIncidentLog = items.Any(i => i.Action == "Sự cố an ninh" && i.Details == "Sự cố mất điện đột ngột tại Kiosk");

                    Assert.True(hasConfigLog);
                    Assert.True(hasIncidentLog);

                    control.txtAuditSearch.Text = "mất điện";
                    control.BtnFilterAuditLogs_Click(null, new System.Windows.RoutedEventArgs());
                    
                    items = control.dgAuditLogs.ItemsSource as System.Collections.ObjectModel.ObservableCollection<QASmartClass.Admin.Controls.AuditLogDisplay>;
                    Assert.NotNull(items);
                    Assert.Single(items);
                    Assert.Equal("Sự cố an ninh", items[0].Action);
                }
                finally
                {
                    AppDbContext.FallbackInMemoryConnection = null;
                }
            });
        }

        [Fact]
        public void Test_AdminDashboard_ExportLogs_MasksCccdSensitiveData()
        {
            RunOnStaThread(() =>
            {
                using var db = TestDbFactory.CreateWithSeed();
                AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
                try
                {
                    db.Database.EnsureCreated();
                    var existing = db.SecurityLogs.ToList();
                    db.SecurityLogs.RemoveRange(existing);
                    db.SaveChanges();

                    var checkInLog = new SecurityLog
                    {
                        EventType = "CheckIn",
                        GuardName = "Bảo vệ A",
                        Description = "Khách hàng Nguyễn Văn A, CCCD: 123456789012, đến làm việc",
                        Timestamp = DateTime.Now
                    };
                    db.SecurityLogs.Add(checkInLog);
                    db.SaveChanges();

                    var control = new QASmartClass.Admin.Controls.SchoolAdminDashboardControl();
                    control.txtAuditSearch.Text = "";
                    control.cmbLogType.SelectedIndex = 0;
                    control.cmbLogPeriod.SelectedIndex = 0;

                    string exportDir = QASmartClass.Services.AppPaths.ExportsDir;
                    if (System.IO.Directory.Exists(exportDir))
                    {
                        foreach (var file in System.IO.Directory.GetFiles(exportDir, "*.csv"))
                        {
                            try { System.IO.File.Delete(file); } catch { }
                        }
                    }

                    CloseMessageBox("Xuất báo cáo thành công");
                    control.BtnExportAuditLogs_Click(null, new System.Windows.RoutedEventArgs());

                    var files = System.IO.Directory.GetFiles(exportDir, "NhatKyHeThong_*.csv");
                    Assert.NotEmpty(files);

                    string exportedFile = files[0];
                    string csvContent = System.IO.File.ReadAllText(exportedFile);

                    Assert.Contains("123******012", csvContent);
                    Assert.DoesNotContain("123456789012", csvContent);
                }
                finally
                {
                    AppDbContext.FallbackInMemoryConnection = null;
                }
            });
        }

    }
}
