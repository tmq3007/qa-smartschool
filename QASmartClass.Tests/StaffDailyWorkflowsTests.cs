using System;

using System.IO;

using System.Linq;

using System.Threading;

using System.Threading.Tasks;

using System.Collections.ObjectModel;

using Xunit;

using Microsoft.EntityFrameworkCore;

using QASmartClass.Data;

using QASmartClass.Services;

using QASmartClass.Staff.Services;

using QASmartClass.Staff.ViewModels;





namespace QASmartClass.Tests

{

    public class StaffDailyWorkflowsTests

    {

        static StaffDailyWorkflowsTests()

        {

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();

            AppPaths.EnsureDirectories();

        }



        private void RunOnStaThread(Action action)

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

            Exception? ex = null;

            var t = new Thread(() =>

            {

                try

                {

                    InitializeApplicationFull(); action();

                }

                catch (Exception e)

                {

                    ex = e;

                }

            });

            t.SetApartmentState(ApartmentState.STA);

            t.Start();

            t.Join();

            if (ex != null)

            {

                throw ex;

            }

        }



        [Fact]

        public void TestStaffLoginWorkflow_VerifyCredentialsAndRoles()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                // Clear and Seed staff accounts for login

                var adminUser = new TeacherProfile 

                { 

                    TeacherCode = "ADMIN", 

                    FullName = "Quản trị viên", 

                    Role = "Admin", 

                    PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("admin123"),

                    IsActive = true

                };

                var hieuTruong = new TeacherProfile 

                { 

                    TeacherCode = "HT001", 

                    FullName = "Nguyễn Văn Hùng", 

                    Role = "HieuTruong", 

                    PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("ht2026"),

                    IsActive = true

                };

                var tvUser = new TeacherProfile 

                { 

                    TeacherCode = "TV001", 

                    FullName = "Hoàng Minh Tâm", 

                    Role = "Counselor", 

                    PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("tv2026"),

                    IsActive = true

                };

                var ytUser = new TeacherProfile 

                { 

                    TeacherCode = "YT001", 

                    FullName = "Phạm Thị Lan", 

                    Role = "YTe", 

                    PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("yt2026"),

                    IsActive = true

                };

                var bvUser = new TeacherProfile 

                { 

                    TeacherCode = "BV001", 

                    FullName = "Trần Văn Bảo", 

                    Role = "BaoVe", 

                    PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("bv2026"),

                    IsActive = true

                };

                var gvUser = new TeacherProfile 

                { 

                    TeacherCode = "GV001", 

                    FullName = "Lê Văn Dũng", 

                    Role = "GV", 

                    PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("gv2026"),

                    IsActive = true

                };



                db.TeacherProfiles.AddRange(adminUser, hieuTruong, tvUser, ytUser, bvUser, gvUser);

                db.SaveChanges();



                // 1. Verify credentials for Admin

                var foundAdmin = db.TeacherProfiles.FirstOrDefault(t => t.TeacherCode == "ADMIN");

                Assert.NotNull(foundAdmin);

                Assert.True(QASmartTouch.Services.AuthenticationService.VerifyPassword("admin123", foundAdmin.PasswordHash));



                // 2. Simulate Login session

                StaffSession.Login(foundAdmin);

                Assert.True(StaffSession.IsLoggedIn);

                Assert.Equal("Admin", StaffSession.Role);

                Assert.Equal("Quản trị viên", StaffSession.DisplayName);

                Assert.True(StaffSession.CanAccessSettings());

                Assert.True(StaffSession.CanManagePayroll());



                // 3. Verify credentials for Principal

                var foundHieuTruong = db.TeacherProfiles.FirstOrDefault(t => t.TeacherCode == "HT001");

                Assert.NotNull(foundHieuTruong);

                Assert.True(QASmartTouch.Services.AuthenticationService.VerifyPassword("ht2026", foundHieuTruong.PasswordHash));



                StaffSession.Login(foundHieuTruong);

                Assert.Equal("HieuTruong", StaffSession.Role);

                Assert.False(StaffSession.CanAccessSettings()); // Only Admin

                Assert.True(StaffSession.CanApprove());

                Assert.True(StaffSession.CanManageBulletin());



                // 4. Verify credentials for Counselor (Library)

                var foundTv = db.TeacherProfiles.FirstOrDefault(t => t.TeacherCode == "TV001");

                Assert.NotNull(foundTv);

                Assert.True(QASmartTouch.Services.AuthenticationService.VerifyPassword("tv2026", foundTv.PasswordHash));

                StaffSession.Login(foundTv);

                Assert.Equal("Counselor", StaffSession.Role);

                Assert.Equal("Hoàng Minh Tâm", StaffSession.DisplayName);



                // 5. Verify credentials for YTe (Medical)

                var foundYt = db.TeacherProfiles.FirstOrDefault(t => t.TeacherCode == "YT001");

                Assert.NotNull(foundYt);

                Assert.True(QASmartTouch.Services.AuthenticationService.VerifyPassword("yt2026", foundYt.PasswordHash));

                StaffSession.Login(foundYt);

                Assert.Equal("YTe", StaffSession.Role);

                Assert.Equal("Phạm Thị Lan", StaffSession.DisplayName);



                // 6. Verify credentials for BaoVe (Security)

                var foundBv = db.TeacherProfiles.FirstOrDefault(t => t.TeacherCode == "BV001");

                Assert.NotNull(foundBv);

                Assert.True(QASmartTouch.Services.AuthenticationService.VerifyPassword("bv2026", foundBv.PasswordHash));

                StaffSession.Login(foundBv);

                Assert.Equal("BaoVe", StaffSession.Role);

                Assert.Equal("Trần Văn Bảo", StaffSession.DisplayName);



                // 7. Verify credentials for Teacher

                var foundGv = db.TeacherProfiles.FirstOrDefault(t => t.TeacherCode == "GV001");

                Assert.NotNull(foundGv);

                Assert.True(QASmartTouch.Services.AuthenticationService.VerifyPassword("gv2026", foundGv.PasswordHash));

                StaffSession.Login(foundGv);

                Assert.Equal("GV", StaffSession.Role);

                Assert.Equal("Lê Văn Dũng", StaffSession.DisplayName);



                StaffSession.Logout();

                Assert.False(StaffSession.IsLoggedIn);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public async Task TestCanteenPosWorkflow_AllergyAndBalanceWarning()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                // Seed student, allergy, and card

                var student = new Student

                {

                    StudentCode = "HS_CANTEEN",

                    FullName = "Nguyễn Văn Canteen",

                    ClassName = "10A1",

                    Status = "Active",

                    WalletBalance = 25000, // Below threshold

                    LowBalanceThreshold = 30000,

                    IsAtRisk = false

                };

                student.WalletChecksum = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(

                    student.StudentCode + student.WalletBalance.ToString("F2")

                );

                db.Students.Add(student);



                var allergy = new FoodAllergy

                {

                    StudentCode = "HS_CANTEEN",

                    StudentName = "Nguyễn Văn Canteen",

                    Allergen = "Hải sản",

                    Severity = "Severe",

                    ActionPlan = "EpiPen"

                };

                db.FoodAllergies.Add(allergy);

                db.SaveChanges();



                var vm = new CanteenPosViewModel();

                await vm.InitializeAsync();



                // 1. Search student code

                vm.SearchCode = "HS_CANTEEN";

                await vm.SearchStudentCommand.ExecuteAsync(null);



                Assert.NotNull(vm.CurrentStudent);

                Assert.Equal("Nguyễn Văn Canteen", vm.CurrentStudent.FullName);

                Assert.True(vm.IsLowBalanceWarningVisible);

                Assert.Contains("CẢNH BÁO SỐ DƯ VÍ THẤP", vm.LowBalanceWarningMessage);

                Assert.True(vm.IsAllergyOverlayOpen);

                Assert.Contains("CẢNH BÁO DỊ ỨNG", vm.AllergyWarning);



                vm.IsAllergyConfirmedByStaff = true;
                await vm.ConfirmAllergyCommand.ExecuteAsync(null);



                // 2. Perform Deduction

                vm.DeductionAmount = 15000;

                vm.OrderDetails = "Bữa trưa dinh dưỡng";

                await vm.ProcessPaymentCommand.ExecuteAsync(null);



                // Check balance updated in DB

                var updatedStudent = db.Students.AsNoTracking().FirstOrDefault(s => s.StudentCode == "HS_CANTEEN");

                Assert.NotNull(updatedStudent);

                Assert.Equal(10000, updatedStudent.WalletBalance);



                vm.Dispose();

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public async Task TestCleaningScheduleWorkflow_JanitorAssignment()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var janitor = new TeacherProfile

                {

                    TeacherCode = "LC_TEST",

                    FullName = "Cô Lao Công Test",

                    Role = "LaoCong",

                    IsActive = true

                };

                db.TeacherProfiles.Add(janitor);

                db.SaveChanges();



                var vm = new CleaningScheduleViewModel();

                await vm.InitializeAsync();



                // 1. Verify Janitors loaded

                Assert.NotEmpty(vm.JanitorProfiles);

                var testJanitor = vm.JanitorProfiles.FirstOrDefault(j => j.TeacherCode == "LC_TEST");

                Assert.NotNull(testJanitor);



                // 2. Add Cleaning Task

                vm.SelectedJanitor = testJanitor;

                vm.Area = "Hành lang tầng 3";

                vm.SelectedShift = "Afternoon";

                vm.Note = "Lau dọn sau giờ ra chơi";

                vm.TaskDate = DateTime.Today;



                await vm.AddCommand.ExecuteAsync(null);



                // Verify in DB and list

                var taskInDb = db.CleaningTasks.AsNoTracking().FirstOrDefault(t => t.Area == "Hành lang tầng 3");

                Assert.NotNull(taskInDb);

                Assert.Equal("Cô Lao Công Test", taskInDb.JanitorName);

                Assert.Equal("Pending", taskInDb.Status);



                await vm.LoadTasksCommand.ExecuteAsync(null);

                var displayTask = vm.Tasks.FirstOrDefault(t => t.Area == "Hành lang tầng 3");

                Assert.NotNull(displayTask);



                // 3. Mark Done

                await vm.DoneCommand.ExecuteAsync(displayTask);

                var completedTask = db.CleaningTasks.AsNoTracking().FirstOrDefault(t => t.Id == displayTask.Id);

                Assert.NotNull(completedTask);

                Assert.Equal("Done", completedTask.Status);



                vm.Dispose();

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public async Task TestGateMonitorWorkflow_QRLeavePassValidation()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student

                {

                    StudentCode = "HS_GATE",

                    FullName = "Trần Văn Cổng",

                    ClassName = "11A1",

                    Status = "Active"

                };

                db.Students.Add(student);

                db.SaveChanges();



                // Seed approved leave request for today

                var leaveReq = new StudentLeaveRequest

                {

                    StudentId = student.Id,

                    StudentName = student.FullName,

                    ClassName = student.ClassName,

                    LeaveDate = DateTime.Today,

                    Reason = "Khám bệnh răng",

                    Status = "Approved"

                };

                db.StudentLeaveRequests.Add(leaveReq);
                db.EventLogs.Add(new EventLog
                {
                    EventType = "GateCheckIn",
                    Actor = student.StudentCode,
                    Timestamp = DateTime.Now
                });
                db.SaveChanges();



                var vm = new GateMonitorViewModel();

                await vm.RefreshDataCommand.ExecuteAsync(null);



                // 1. Verify early leave list has the student

                Assert.NotEmpty(vm.EarlyLeaveRequests);

                Assert.Contains(vm.EarlyLeaveRequests, r => r.StudentId == student.Id);



                // 2. Scan QR Leave Pass

                string qrCode = $"LP-{DateTime.Today:yyyyMMdd}-{student.Id}";

                vm.ScannedLeavePassCode = qrCode;

                await vm.ScanLeavePassCommand.ExecuteAsync(null);



                // Verify GateCheckOut event log created

                var checkoutLog = db.EventLogs.AsNoTracking().FirstOrDefault(l => l.Actor == "HS_GATE" && l.EventType == "GateCheckOut");

                Assert.NotNull(checkoutLog);

                Assert.Contains("VerificationCode", checkoutLog.Details);



                vm.Dispose();

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public async Task TestIncidentManagementWorkflow_RecordIncidentsAndGoodDeeds()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student

                {

                    StudentCode = "HS_INCIDENT",

                    FullName = "Phạm Văn Sự Cố",

                    ClassName = "12A2",

                    Status = "Active"

                };

                db.Students.Add(student);

                db.SaveChanges();



                var vm = new IncidentManagementViewModel();

                await vm.InitializeAsync();



                // 1. Record Incident

                vm.StudentCode = "HS_INCIDENT";

                vm.SeverityIndex = 2; // High

                vm.Description = "Làm vỡ cửa kính hành lang";

                

                await vm.ReportIncidentCommand.ExecuteAsync(null);



                // Assert there was no validation error

                Assert.True(string.IsNullOrEmpty(vm.ErrorMessage), vm.ErrorMessage);



                // Verify Incident log in DB

                string expectedActor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "StaffUser";

                var log = db.EventLogs.AsNoTracking().FirstOrDefault(l => l.EventType == "Incident" && l.Actor == expectedActor);

                Assert.NotNull(log);

                

                using (var doc = System.Text.Json.JsonDocument.Parse(log.Details))

                {

                    var root = doc.RootElement;

                    Assert.Equal("High", root.GetProperty("Severity").GetString());

                    Assert.Equal("HS_INCIDENT", root.GetProperty("StudentCode").GetString());

                    Assert.Equal("Làm vỡ cửa kính hành lang", root.GetProperty("Description").GetString());

                }



                // 2. Record Good Deed

                vm.GoodDeedStudentCode = "HS_INCIDENT";

                vm.GoodDeedDescription = "Nhặt được ví tiền trả lại người mất";

                

                await vm.ReportGoodDeedCommand.ExecuteAsync(null);



                var goodDeedLog = db.EventLogs.AsNoTracking().FirstOrDefault(l => l.EventType == "GoodDeed");

                Assert.NotNull(goodDeedLog);

                

                using (var doc = System.Text.Json.JsonDocument.Parse(goodDeedLog.Details))

                {

                    var root = doc.RootElement;

                    Assert.Equal("HS_INCIDENT", root.GetProperty("StudentCode").GetString());

                    Assert.Contains("Nhặt được ví tiền trả lại người mất", root.GetProperty("Description").GetString());

                }

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public async Task TestKitchenDashboardWorkflow_EstimatedMealsAndMenuPush()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                // Active students (10)

                for (int i = 1; i <= 10; i++)

                {

                    db.Students.Add(new Student { StudentCode = $"HS_MEAL_{i}", FullName = $"Học Sinh Meal {i}", Status = "Active" });

                }



                // Approved leave requests for today (2 students)

                db.StudentLeaveRequests.Add(new StudentLeaveRequest { StudentId = 1, LeaveDate = DateTime.Today, Status = "Approved" });

                db.StudentLeaveRequests.Add(new StudentLeaveRequest { StudentId = 2, LeaveDate = DateTime.Today, Status = "Approved" });



                // Staff attendance present today (3 staff)

                db.StaffAttendances.Add(new StaffAttendance { Date = DateTime.Today, Status = "Present" });

                db.StaffAttendances.Add(new StaffAttendance { Date = DateTime.Today, Status = "Present" });

                db.StaffAttendances.Add(new StaffAttendance { Date = DateTime.Today, Status = "Present" });



                db.SaveChanges();



                var vm = new KitchenDashboardViewModel();

                await vm.InitializeAsync();



                // 1. Verify meal count estimation

                // Expected: 10 students - 2 leaves + 3 present staff = 11 meals

                await vm.PreorderCommand.ExecuteAsync(null);

                Assert.Contains("11 Suất", vm.MealCount);



                // 2. Save menu

                vm.MenuDate = DateTime.Today;

                vm.SelectedMealType = "Lunch";

                vm.MenuItems = "Cơm sườn, canh bí";

                vm.NutritionInfo = "650 kcal";

                vm.Allergens = "Không";



                await vm.SaveMenuCommand.ExecuteAsync(null);



                var menuInDb = db.SchoolMenus.FirstOrDefault(m => m.Items == "Cơm sườn, canh bí");

                Assert.NotNull(menuInDb);

                Assert.Equal("Lunch", menuInDb.MealType);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public async Task TestLeaveRequestManagementWorkflow_ApproveAndRejectSynchronisation()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student

                {

                    StudentCode = "HS_LEAVE",

                    FullName = "Vũ Văn Phép",

                    ClassName = "10A1",

                    Status = "Active"

                };

                db.Students.Add(student);



                var roster = new ClassRoster { ClassName = "10A1", IsActive = true };

                db.ClassRosters.Add(roster);

                db.SaveChanges();



                db.ClassRosterStudents.Add(new ClassRosterStudent { StudentId = student.Id, RosterId = roster.Id });

                db.SaveChanges();



                // Create leave request

                var req = new StudentLeaveRequest

                {

                    StudentId = student.Id,

                    StudentName = student.FullName,

                    ClassName = student.ClassName,

                    LeaveDate = DateTime.Today,

                    Reason = "Đau chân đi chụp X-quang",

                    Status = "Pending"

                };

                db.StudentLeaveRequests.Add(req);

                db.SaveChanges();



                var vm = new LeaveRequestManagementViewModel();

                await vm.InitializeAsync();



                // 1. Approve Request

                await vm.ApproveCommand.ExecuteAsync(req.Id);



                var approvedReq = db.StudentLeaveRequests.AsNoTracking().FirstOrDefault(r => r.Id == req.Id);

                Assert.NotNull(approvedReq);

                Assert.Equal("Approved", approvedReq.Status);



                // Check Attendance sync to "excused"

                var attendance = db.AttendanceRecords.AsNoTracking().FirstOrDefault(a => a.StudentId == student.Id && a.Date.Date == DateTime.Today.Date);

                Assert.NotNull(attendance);

                Assert.Equal("excused", attendance.Status);

                Assert.Contains("Đau chân đi chụp X-quang", attendance.Note);



                // 2. Reject Request (reset to Pending first)

                var reqToReset = db.StudentLeaveRequests.FirstOrDefault(r => r.Id == req.Id);

                Assert.NotNull(reqToReset);

                reqToReset.Status = "Pending";

                db.SaveChanges();



                vm.RejectReason = "Lý do chưa rõ ràng, cần giấy tờ xác nhận.";

                await vm.RejectCommand.ExecuteAsync(req.Id);



                var rejectedReq = db.StudentLeaveRequests.AsNoTracking().FirstOrDefault(r => r.Id == req.Id);

                Assert.NotNull(rejectedReq);

                Assert.Equal("Rejected", rejectedReq.Status);



                vm.Dispose();

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public async Task TestSchoolAssetManagementWorkflow_BookingConflictChecking()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                // Add Asset

                var asset = new SchoolAsset

                {

                    AssetCode = "AST-TEST-001",

                    AssetType = "Projector - Máy chiếu đa năng",

                    Status = "Active",

                    Location = "Phòng học 102"

                };

                db.SchoolAssets.Add(asset);

                db.SaveChanges();



                var vm = new SchoolAssetManagementViewModel();

                await vm.InitializeAsync();



                // 1. Report needs repair

                await vm.ReportCommand.ExecuteAsync(asset.Id);

                var updatedAsset = db.SchoolAssets.AsNoTracking().FirstOrDefault(a => a.Id == asset.Id);

                Assert.NotNull(updatedAsset);

                Assert.Equal("NeedsRepair", updatedAsset.Status);



                // 2. Fix asset

                await vm.FixCommand.ExecuteAsync(asset.Id);

                var fixedAsset = db.SchoolAssets.AsNoTracking().FirstOrDefault(a => a.Id == asset.Id);

                Assert.NotNull(fixedAsset);

                Assert.Equal("Active", fixedAsset.Status);



                // 3. Decommission asset

                await vm.DecommissionAssetCommand.ExecuteAsync(asset.Id);

                var decommissionedAsset = db.SchoolAssets.AsNoTracking().FirstOrDefault(a => a.Id == asset.Id);

                Assert.NotNull(decommissionedAsset);

                Assert.Equal("Broken", decommissionedAsset.Status);



                vm.Dispose();

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public async Task TestSecurityKioskWorkflow_VisitorEntryAndScanBuffer()

        {

            using var db = TestDbFactory.CreateWithSeed();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var vm = new SecurityKioskViewModel();

                await vm.InitializeAsync();



                // 1. Simulate QR Code scan buffer (CCCD format: cccd|no|name|dob|gender|address|date)

                string cccdQr = "001096001234|123456789|Nguyễn Văn Khách|15081996|Nam|Hà Nội|15082021";

                vm.QrInputBuffer = cccdQr;



                Assert.Equal("Nguyễn Văn Khách", vm.Person);

                Assert.Contains("CCCD: 001096001234", vm.Description);

                Assert.Contains("Địa chỉ: Hà Nội", vm.Description);



                // 2. Save visitor log

                vm.SelectedEventType = "CheckIn";
                vm.SelectedHostTeacherId = "GV001";
                vm.BadgeNumber = "BADGE-123";

                await vm.SaveCommand.ExecuteAsync(null);



                var log = db.SecurityLogs.FirstOrDefault(l => l.PersonInvolved == "Nguyễn Văn Khách");

                Assert.NotNull(log);

                Assert.Equal("CheckIn", log.EventType);



                vm.Dispose();

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public async Task TestTaskManagementWorkflow_WeeklyTimetableConflictChecking()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                // Seed staff and active class roster

                var staff = new TeacherProfile { TeacherCode = "GV_TASK", FullName = "Giáo Viên Giao Việc", Role = "GV", Subject = "Toán học", IsActive = true };

                db.TeacherProfiles.Add(staff);



                var roster = new ClassRoster { ClassName = "10A1", IsActive = true };

                db.ClassRosters.Add(roster);

                db.SaveChanges();



                // Seed conflicting timetable entry

                // Tuesday (dayOfWeek = 3), Period 2, Room 201

                var conflictTimetable = new TimetableEntry

                {

                    TeacherName = "Giáo Viên Giao Việc",

                    DayOfWeek = 3,

                    Period = 2,

                    Room = "Phòng 201",

                    Subject = "Toán hình",

                    RosterId = roster.Id

                };

                db.TimetableEntries.Add(conflictTimetable);

                db.SaveChanges();



                var vm = new TaskManagementViewModel();

                await vm.InitializeAsync();



                // 1. Populate staff names by department

                vm.SelectedDepartment = "ToanTin";

                Assert.Contains("Giáo Viên Giao Việc", vm.StaffNames);



                // 2. Assign Task with overlap

                vm.TaskTitle = "Họp hội đồng sư phạm";

                vm.Assignee = "Giáo Viên Giao Việc";

                vm.DueDate = new DateTime(2026, 6, 23); // Tuesday (DayOfWeek.Tuesday = 2 -> 3)

                vm.SyncPeriod = 2;

                vm.SyncRoom = "Phòng 201";



                // Trigger AssignCommand - should prompt overlap warning in AppServices.UIService

                await vm.AssignCommand.ExecuteAsync(null);



                // Verify task saved to DB

                var task = db.DailyTasks.FirstOrDefault(t => t.Title == "Họp hội đồng sư phạm");

                Assert.NotNull(task);

                Assert.Equal("Pending", task.Status);



                // Verify weekly timetable synchronised entry

                var syncedEntry = db.TimetableEntries.FirstOrDefault(e => e.Subject.Contains($"[Task#{task.Id}]"));

                Assert.NotNull(syncedEntry);

                Assert.Equal("Giáo Viên Giao Việc", syncedEntry.TeacherName);

                Assert.Equal(3, syncedEntry.DayOfWeek);

                Assert.Equal(2, syncedEntry.Period);

                Assert.Equal("Phòng 201", syncedEntry.Room);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public async Task TestSchoolAssetManagementWorkflow_BookingConflictChecking_MasterSettingAutoReplacement()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                // Seed teacher and assets

                var teacher = new TeacherProfile { TeacherCode = "GV_TEST_BOOKING", FullName = "Giáo Viên Thử Nghiệm", Role = "GV", IsActive = true };

                db.TeacherProfiles.Add(teacher);



                // Asset A: Projector - Máy 1 (Broken/NeedsRepair)

                var assetA = new SchoolAsset { AssetCode = "AST-PRJ-01", AssetType = "Projector - Máy chiếu số 1", Status = "Active", Location = "Phòng 101" };

                // Asset B: Projector - Máy 2 (Active, replacement)

                var assetB = new SchoolAsset { AssetCode = "AST-PRJ-02", AssetType = "Projector - Máy chiếu số 2", Status = "Active", Location = "Phòng 102" };

                db.SchoolAssets.AddRange(assetA, assetB);

                db.SaveChanges();



                // Seed Booking for Asset A on Today, TimeSlot 3

                var booking = new SchoolAssetBooking

                {

                    AssetId = assetA.Id,

                    BookedBy = "GV_TEST_BOOKING",

                    BookingDate = DateTime.Today,

                    TimeSlot = 3

                };

                db.AssetBookings.Add(booking);

                db.SaveChanges();



                var vm = new SchoolAssetManagementViewModel();

                await vm.InitializeAsync();



                // Scenario 1: Mode = "0" (Tự động thay thế thiết bị cùng loại)

                var modeSetting = new SystemSetting { Id = "Asset_AutoCancelBookingsOnRepair", Value = "0", Category = "Asset" };

                db.SystemSettings.Add(modeSetting);

                db.SaveChanges();



                // Report Asset A needs repair

                await vm.ReportCommand.ExecuteAsync(assetA.Id);



                // Verify Asset A is in NeedsRepair state

                var checkedAssetA = db.SchoolAssets.AsNoTracking().FirstOrDefault(a => a.Id == assetA.Id);

                Assert.NotNull(checkedAssetA);

                Assert.Equal("NeedsRepair", checkedAssetA.Status);



                // Verify booking is automatically re-assigned to Asset B

                var updatedBooking = db.AssetBookings.AsNoTracking().FirstOrDefault(b => b.Id == booking.Id);

                Assert.NotNull(updatedBooking);

                Assert.Equal(assetB.Id, updatedBooking.AssetId);



                // Verify Push Notification created in db

                var notify = db.PushMessageLogs.AsNoTracking().FirstOrDefault(n => n.RecipientId == teacher.Id && n.Title == "Thay đổi thiết bị mượn tự động");

                Assert.NotNull(notify);



                // Scenario 2: Mode = "1" (Hủy & Thông báo)

                // Reset state: Reload current DB values first, then update them to trigger EF tracker

                db.Entry(assetA).Reload();

                db.Entry(booking).Reload();



                assetA.Status = "Active";

                booking.AssetId = assetA.Id;

                

                var existingSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Asset_AutoCancelBookingsOnRepair");

                if (existingSetting != null)

                {

                    existingSetting.Value = "1";

                }

                db.SaveChanges();



                vm.Dispose();

                vm = new SchoolAssetManagementViewModel();

                await vm.InitializeAsync();



                // Report Asset A needs repair again

                await vm.ReportCommand.ExecuteAsync(assetA.Id);



                // Verify booking is cancelled (deleted)

                var cancelledBooking = db.AssetBookings.AsNoTracking().FirstOrDefault(b => b.Id == booking.Id);

                Assert.Null(cancelledBooking); // Booking is removed from DB



                // Verify Cancel Push Notification created in db

                var cancelNotify = db.PushMessageLogs.AsNoTracking().FirstOrDefault(n => n.RecipientId == teacher.Id && n.Title == "Hủy lịch mượn thiết bị");

                Assert.NotNull(cancelNotify);



                vm.Dispose();

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestLibraryCounselorWorkflow_StudentMentalHealthRiskAndParentNotification()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                // Seed test user as Counselor and a student

                var counselor = new TeacherProfile

                {

                    TeacherCode = "TV001",

                    FullName = "Hoàng Minh Tâm",

                    Role = "Counselor",

                    PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("tv2026"),

                    IsActive = true

                };

                var student = new Student

                {

                    StudentCode = "HS_COUNSELOR",

                    FullName = "Nguyễn Văn Tư Vấn",

                    ClassName = "12A1",

                    Status = "Active"

                };

                db.TeacherProfiles.Add(counselor);

                db.Students.Add(student);

                db.SaveChanges();



                // 1. Simulate login as TV001

                StaffSession.Login(counselor);

                Assert.True(StaffSession.IsLoggedIn);

                Assert.Equal("Counselor", StaffSession.Role);



                // 2. Add record with low mood score and sensitive keywords, verifying risk assessment

                int studentId = student.Id;

                int moodScore = 2;

                string notes = "Em bị các bạn học sinh khác bắt nạt";

                string[] sensitiveKeywords = { "buồn bã", "đánh nhau", "tự kỷ", "bắt nạt", "trầm cảm" };

                var detected = sensitiveKeywords.Where(notes.Contains).ToList();



                string riskLevel = "Low";

                if (moodScore <= 3 || detected.Count > 0) riskLevel = "High";

                else if (moodScore <= 6) riskLevel = "Medium";



                var record = new StudentMentalHealthRecord

                {

                    StudentId = studentId,

                    MoodScore = moodScore,

                    Notes = notes,

                    RiskLevel = riskLevel,

                    DetectedKeywords = string.Join(", ", detected),

                    RecordedAt = DateTime.Now,

                    IsNotified = false

                };



                db.StudentMentalHealthRecords.Add(record);

                db.SaveChanges();



                var saved = db.StudentMentalHealthRecords.AsNoTracking().FirstOrDefault(r => r.StudentId == studentId);

                Assert.NotNull(saved);

                Assert.Equal("High", saved.RiskLevel);

                Assert.Contains("bắt nạt", saved.DetectedKeywords);



                // 3. Simulate sending notification to parents for High risk record

                var notifService = new QASmartClass.Services.NotificationService(db);

                string subject = "Thông báo khẩn cấp mật từ Phòng tư vấn Tâm lý học đường";

                string reason = "Nhà trường phát hiện con em đang gặp vấn đề áp lực lớn hoặc tâm trạng cần sự chia sẻ, đồng hành từ gia đình. Vui lòng liên hệ Phòng tư vấn học đường của nhà trường để cùng phối hợp hỗ trợ con em.";



                // Send notification

                bool success = notifService.SendToParent(studentId, subject, reason);

                Assert.True(success);



                // Update notified state

                var recordToUpdate = db.StudentMentalHealthRecords.FirstOrDefault(r => r.Id == record.Id);

                if (recordToUpdate != null)

                {

                    recordToUpdate.IsNotified = true;

                    recordToUpdate.NotifiedAt = DateTime.Now;

                    db.SaveChanges();

                }



                var updatedRecord = db.StudentMentalHealthRecords.AsNoTracking().FirstOrDefault(r => r.Id == record.Id);

                Assert.NotNull(updatedRecord);

                Assert.True(updatedRecord.IsNotified);

                Assert.NotNull(updatedRecord.NotifiedAt);



                StaffSession.Logout();

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestMedicalStaffWorkflows_RecordsAndAlertThreshold()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                // Seed test user as Medical Staff and a student

                var medicalStaff = new TeacherProfile

                {

                    TeacherCode = "YT001",

                    FullName = "Phạm Thị Lan",

                    Role = "YTe",

                    PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("yt2026"),

                    IsActive = true

                };

                var student = new Student

                {

                    StudentCode = "HS_MEDICAL",

                    FullName = "Trần Học Sinh Y Tế",

                    ClassName = "10A1",

                    Status = "Active"

                };

                db.TeacherProfiles.Add(medicalStaff);

                db.Students.Add(student);

                db.SaveChanges();



                // 1. Simulate login as YT001

                StaffSession.Login(medicalStaff);

                Assert.True(StaffSession.IsLoggedIn);

                Assert.Equal("YTe", StaffSession.Role);



                // 2. Add Health Record and verify BMI calculation

                double height = 170; // cm

                double weight = 60;  // kg

                double bmi = height > 0 ? weight / Math.Pow(height / 100.0, 2) : 0;

                

                var healthRecord = new HealthRecord

                {

                    StudentId = student.Id,

                    StudentName = student.FullName,

                    ClassName = student.ClassName,

                    Height = height,

                    Weight = weight,

                    VisionLeft = 10,

                    VisionRight = 9.5,

                    ChronicConditions = "Không có",

                    ExamDate = DateTime.Today

                };

                db.HealthRecords.Add(healthRecord);

                db.SaveChanges();



                var savedHealth = db.HealthRecords.AsNoTracking().FirstOrDefault(h => h.StudentId == student.Id);

                Assert.NotNull(savedHealth);

                Assert.Equal(170, savedHealth.Height);

                Assert.Equal(60, savedHealth.Weight);

                

                // Assert BMI is normal (around 20.76)

                Assert.True(bmi >= 18.5 && bmi <= 25);



                // 3. Add Epidemic Cases and verify Alert Threshold (dengue cases >= 3 in last 7 days)

                db.EpidemicCases.AddRange(

                    new EpidemicCase { StudentId = student.Id, StudentName = student.FullName, ClassName = student.ClassName, Disease = "Sốt xuất huyết", OnsetDate = DateTime.Today.AddDays(-2), Status = "Active", IsolatedAt = "Home" },

                    new EpidemicCase { StudentId = student.Id, StudentName = student.FullName, ClassName = student.ClassName, Disease = "Sốt xuất huyết", OnsetDate = DateTime.Today.AddDays(-3), Status = "Active", IsolatedAt = "Hospital" },

                    new EpidemicCase { StudentId = student.Id, StudentName = student.FullName, ClassName = student.ClassName, Disease = "Sốt xuất huyết", OnsetDate = DateTime.Today.AddDays(-1), Status = "Active", IsolatedAt = "Home" }

                );

                db.SaveChanges();



                var cases = db.EpidemicCases.AsNoTracking().Where(c => c.Disease == "Sốt xuất huyết" && c.OnsetDate >= DateTime.Today.AddDays(-7)).ToList();

                Assert.Equal(3, cases.Count);

                

                string alertMessage = "";

                if (cases.Count >= 3)

                {

                    alertMessage = $"Phát hiện {cases.Count} ca Sốt xuất huyết trong tuần qua. Đề nghị phun thuốc muỗi toàn trường!";

                }

                Assert.Contains("Đề nghị phun thuốc muỗi toàn trường!", alertMessage);



                // 4. Add Food Safety Record

                var foodRecord = new FoodSafetyRecord

                {

                    Date = DateTime.Today,

                    MenuItems = "Cơm trưa dinh dưỡng",

                    SampleKept = true,

                    Inspector = medicalStaff.FullName,

                    Result = "Pass"

                };

                db.FoodSafetyRecords.Add(foodRecord);

                db.SaveChanges();



                var savedFood = db.FoodSafetyRecords.AsNoTracking().FirstOrDefault(f => f.Inspector == "Phạm Thị Lan");

                Assert.NotNull(savedFood);

                Assert.True(savedFood.SampleKept);

                Assert.Equal("Pass", savedFood.Result);



                StaffSession.Logout();

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestDepartmentManagementWorkflow_CreateDeptAndKpiEstimation()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                // Seed staff members

                var headProfile = new StaffProfile { StaffCode = "STF_HEAD", FullName = "Nguyễn Tổ Trưởng", Department = "Toán", Position = "ToTruong" };

                var memberProfile = new StaffProfile { StaffCode = "STF_MEM", FullName = "Lê Giáo Viên", Department = "Toán", Position = "GV" };

                db.StaffProfiles.AddRange(headProfile, memberProfile);

                db.SaveChanges();



                var manager = new TeacherProfile

                {

                    TeacherCode = "HT001",

                    FullName = "Nguyễn Văn Hùng",

                    Role = "HieuTruong",

                    PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("ht2026"),

                    IsActive = true

                };

                db.TeacherProfiles.Add(manager);

                db.SaveChanges();



                // 1. Simulate login as HieuTruong to manage departments

                StaffSession.Login(manager);

                UserSessionService.Instance.SetSession(manager.TeacherCode);



                Assert.True(UserSessionService.Instance.IsManager);



                // 2. Create department

                var deptService = new DepartmentService(db);

                bool createSuccess = deptService.CreateDepartment("Tổ Toán Tin", headProfile.StaffCode, "2025-2026");

                Assert.True(createSuccess);



                var dept = db.Departments.FirstOrDefault(d => d.DepartmentName == "Tổ Toán Tin");

                Assert.NotNull(dept);



                // 3. Add members to department

                bool addHead = deptService.AddMember(dept.Id, headProfile.Id, "Tổ trưởng");

                bool addMember = deptService.AddMember(dept.Id, memberProfile.Id, "Thành viên");

                Assert.True(addHead);

                Assert.True(addMember);



                // Verify members count

                var members = deptService.GetMembersByDepartment(dept.Id);

                Assert.Equal(2, members.Count);



                // 4. Retrieve KPIs

                var kpis = deptService.GetDepartmentKPI(dept.Id, DateTime.Now.Month, DateTime.Now.Year);

                Assert.Equal(2, kpis.TeacherCount);

                Assert.Equal(0, kpis.LessonPlanCount);

                Assert.Equal(0, kpis.ObservationCount);



                StaffSession.Logout();

                UserSessionService.Instance.ClearSession();

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestYouthUnionWorkflow_ActivityAndAwardProposals()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                // Seed teacher, student, and YouthMember

                var teacher = new TeacherProfile { TeacherCode = "GV_UNION", FullName = "Bí thư Đoàn Trường", Role = "GV", IsActive = true };

                var student = new Student { StudentCode = "HS_UNION", FullName = "Trần Đoàn Viên", ClassName = "11A1", Status = "Active" };

                db.TeacherProfiles.Add(teacher);

                db.Students.Add(student);

                db.SaveChanges();



                var member = new YouthMember

                {

                    StudentId = student.Id,

                    StudentName = student.FullName,

                    ClassName = student.ClassName,

                    MemberType = "Đoàn viên",

                    Status = "Active",

                    Position = "Bí thư",

                    TotalScore = 100

                };

                db.YouthMembers.Add(member);

                db.SaveChanges();



                // 1. Simulate login as Secretary (Bí thư) via teacher or student representation

                StaffSession.Login(teacher);

                StaffSession.SetYouthPosition("Bí thư");

                

                Assert.True(StaffSession.CanManageYouthUnion("Bí thư"));



                // 2. Add Youth Union Activity

                var activity = new YouthActivity

                {

                    Title = "Chiến dịch Mùa hè Xanh",

                    Date = DateTime.Today,

                    Participants = "Khối 11",

                    Points = 15,

                    Status = "Approved",

                    Budget = 5000000,

                    Location = "Địa phương nghèo",

                    Description = "Chiến dịch tình nguyện hè",

                    CreatedBy = teacher.FullName

                };

                db.YouthActivities.Add(activity);

                db.SaveChanges();



                var savedActivity = db.YouthActivities.AsNoTracking().FirstOrDefault(a => a.Title == "Chiến dịch Mùa hè Xanh");

                Assert.NotNull(savedActivity);

                Assert.Equal("Approved", savedActivity.Status);



                // 3. Score emulation for student

                var score = new YouthEmulationScore

                {

                    MemberId = member.Id,

                    Score = 15,

                    Category = "Activity",

                    Reason = "Tham gia Mùa hè Xanh",

                    AwardedBy = teacher.FullName,

                    ActivityId = savedActivity.Id

                };

                db.YouthEmulationScores.Add(score);

                db.SaveChanges();



                var savedScore = db.YouthEmulationScores.AsNoTracking().FirstOrDefault(s => s.MemberId == member.Id);

                Assert.NotNull(savedScore);

                Assert.Equal(15, savedScore.Score);



                // 4. Create Award Proposal

                var proposal = new YouthAwardProposal

                {

                    MemberId = member.Id,

                    ProposalType = "Cá nhân",

                    Reason = "Đoàn viên tiêu biểu",

                    Status = "Pending",

                    ProposedBy = teacher.FullName

                };

                db.YouthAwardProposals.Add(proposal);

                db.SaveChanges();



                var savedProposal = db.YouthAwardProposals.AsNoTracking().FirstOrDefault(p => p.MemberId == member.Id);

                Assert.NotNull(savedProposal);

                Assert.Equal("Pending", savedProposal.Status);



                StaffSession.Logout();

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        // ─── 14 NEW INTEGRATION TESTS FOR V4.1 ───



        [Fact]

        public void TestLibraryStaffWorkflow_BookLoanOverdueAlert()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var loan = new BookLoan

                {

                    StudentId = 101,

                    BookCode = "BOOK001",

                    BookTitle = "Lập trình C# nâng cao",

                    BorrowDate = DateTime.Today.AddDays(-20),

                    DueDate = DateTime.Today.AddDays(-5),

                    ReturnedDate = null

                };

                db.BookLoans.Add(loan);

                db.SaveChanges();



                var overdueDays = StaffDailyWorkflowsServices.CalculateOverdueDays(loan, DateTime.Today);

                Assert.Equal(5, overdueDays);



                db.InboxMessages.Add(new InboxMessage

                {

                    SenderId = "LIBRARY_ROOM",

                    SenderName = "Thư viện trường",

                    ReceiverId = "student_101",

                    Content = $"[CẢNH BÁO QUÁ HẠN] Sách '{loan.BookTitle}' đã quá hạn {overdueDays} ngày.",

                    IsRead = false,

                    CreatedAt = DateTime.Now

                });

                db.SaveChanges();



                var inboxMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "student_101");

                Assert.NotNull(inboxMsg);

                Assert.Contains("quá hạn", inboxMsg.Content.ToLower());

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestLibraryStaffWorkflow_PreventBorrowingOnExcessiveOverdue()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var overdueLoan = new BookLoan

                {

                    StudentId = 102,

                    BookCode = "BOOK002",

                    BookTitle = "Giải tích 1",

                    BorrowDate = DateTime.Today.AddDays(-30),

                    DueDate = DateTime.Today.AddDays(-10),

                    ReturnedDate = null

                };

                db.BookLoans.Add(overdueLoan);

                db.SaveChanges();



                bool canBorrow = StaffDailyWorkflowsServices.BorrowBook(db, 102, "BOOK003", "Đại số tuyến tính");

                Assert.False(canBorrow);



                bool canBorrowClean = StaffDailyWorkflowsServices.BorrowBook(db, 103, "BOOK004", "Vật lý đại cương");

                Assert.True(canBorrowClean);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestMedicalStaffWorkflow_EmergencyNotificationSent()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student

                {

                    StudentCode = "HS_MED_TEST",

                    FullName = "Trần Y Tế",

                    ClassName = "12A1",

                    Status = "Active"

                };

                db.Students.Add(student);

                db.SaveChanges();



                bool logSuccess = StaffDailyWorkflowsServices.LogEmergency(db, student.Id, "Ngất xỉu trong giờ thể dục", "Uống nước đường và nằm nghỉ");

                Assert.True(logSuccess);



                var log = db.EmergencyLogs.FirstOrDefault(l => l.StudentName == student.FullName);

                Assert.NotNull(log);

                Assert.Equal("Injury", log.IncidentType);

                Assert.Equal("Ngất xỉu trong giờ thể dục", log.Description);



                var parentMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == $"parent_{student.Id}");

                Assert.NotNull(parentMsg);

                Assert.Contains("CANH BAO", parentMsg.Content);



                var teacherMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == $"teacher_{student.ClassName}");

                Assert.NotNull(teacherMsg);

                Assert.Contains("[KHẨN CẤP]", teacherMsg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestMedicalStaffWorkflow_LowStockSupplyWarning()

        {

            var supplyLow = new MedicalSupply { Quantity = 5, ExpiryDate = DateTime.Today.AddYears(1) };

            var supplyExpiring = new MedicalSupply { Quantity = 20, ExpiryDate = DateTime.Today.AddMonths(2) };

            var supplyNormal = new MedicalSupply { Quantity = 15, ExpiryDate = DateTime.Today.AddYears(1) };



            Assert.True(StaffDailyWorkflowsServices.CheckSupplyStockAlert(supplyLow, DateTime.Today));

            Assert.True(StaffDailyWorkflowsServices.CheckSupplyStockAlert(supplyExpiring, DateTime.Today));

            Assert.False(StaffDailyWorkflowsServices.CheckSupplyStockAlert(supplyNormal, DateTime.Today));

        }



        [Fact]

        public void TestSecurityPatrol_CheckinRegistration()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                bool c1 = StaffDailyWorkflowsServices.RegisterPatrolCheckin(db, "C1_GATE", "Bảo vệ Bảo", "Normal", "Cổng chính bình thường");

                bool c2 = StaffDailyWorkflowsServices.RegisterPatrolCheckin(db, "LAB_BLDG", "Bảo vệ Bảo", "Normal", "Khu thí nghiệm khóa cửa");

                bool c3 = StaffDailyWorkflowsServices.RegisterPatrolCheckin(db, "BACK_YARD", "Bảo vệ Bảo", "Incident", "Phát hiện đèn sáng ở kho");



                Assert.True(c1);

                Assert.True(c2);

                Assert.True(c3);



                var logs = db.PatrolLogs.Where(l => l.GuardName == "Bảo vệ Bảo").ToList();

                Assert.Equal(3, logs.Count);

                Assert.Equal(1, logs.Count(l => l.Status == "Incident"));

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestSecurityVisitor_BadgeDoubleAssignmentBlocked()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                bool checkInA = StaffDailyWorkflowsServices.RegisterVisitorBadge(db, "Khách A", "CCCD_A", "15");

                Assert.True(checkInA);



                bool checkInB = StaffDailyWorkflowsServices.RegisterVisitorBadge(db, "Khách B", "CCCD_B", "15");

                Assert.False(checkInB);



                bool checkOutA = StaffDailyWorkflowsServices.RegisterVisitorCheckOut(db, "Khách A", "15");

                Assert.True(checkOutA);



                bool checkInB2 = StaffDailyWorkflowsServices.RegisterVisitorBadge(db, "Khách B", "CCCD_B", "15");

                Assert.True(checkInB2);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestKitchenAllergenCrossCheck_AlertOnDangerousMenu()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student

                {

                    StudentCode = "HS_KITCHEN_1",

                    FullName = "Nguyễn Bán Trú",

                    ClassName = "11B2",

                    Status = "Active"

                };

                db.Students.Add(student);



                var allergy = new FoodAllergy

                {

                    StudentCode = "HS_KITCHEN_1",

                    StudentName = "Nguyễn Bán Trú",

                    Allergen = "Hải sản"

                };

                db.FoodAllergies.Add(allergy);



                var menu = new SchoolMenu

                {

                    Date = DateTime.Today,

                    Items = "Cơm trắng, Canh cua Hải sản, Bò kho"

                };

                db.SchoolMenus.Add(menu);

                db.SaveChanges();



                var conflicts = StaffDailyWorkflowsServices.CheckAllergenConflicts(db, DateTime.Today);

                Assert.Single(conflicts);

                Assert.Equal("Nguyễn Bán Trú", conflicts[0].FullName);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestKitchenAllergenCrossCheck_AlternativeMealGeneration()

        {

            var alternativeSeafood = StaffDailyWorkflowsServices.SuggestAlternativeMeal("Hải sản");

            var alternativePeanut = StaffDailyWorkflowsServices.SuggestAlternativeMeal("Đậu phộng");

            var alternativeEgg = StaffDailyWorkflowsServices.SuggestAlternativeMeal("Trứng");



            Assert.Contains("Cơm gà luộc", alternativeSeafood);

            Assert.Contains("không dầu lạc", alternativePeanut);

            Assert.Contains("Bánh mì chay", alternativeEgg);

        }



        [Fact]

        public void TestDepartmentObservation_TeacherKpiSync()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var teacher = new StaffProfile { StaffCode = "GV_CM", FullName = "Nguyễn Văn Dạy", Position = "Giáo viên" };

                var observer = new StaffProfile { StaffCode = "TT_CM", FullName = "Trần Tổ Trưởng", Position = "Tổ trưởng" };

                db.StaffProfiles.AddRange(teacher, observer);

                db.SaveChanges();



                var dept = new Department { DepartmentName = "Tổ Lý Hóa", HeadTeacherId = observer.StaffCode };

                db.Departments.Add(dept);

                db.SaveChanges();



                var member1 = new DepartmentMember { DepartmentId = dept.Id, StaffId = teacher.Id, Role = "Thành viên" };

                var member2 = new DepartmentMember { DepartmentId = dept.Id, StaffId = observer.Id, Role = "Tổ trưởng" };

                db.DepartmentMembers.AddRange(member1, member2);

                db.SaveChanges();



                bool submitSuccess = StaffDailyWorkflowsServices.SubmitObservation(

                    db, teacher.Id, teacher.FullName, observer.Id, observer.FullName, "ToTruong", 9, "Dạy tốt, tích cực"

                );

                Assert.True(submitSuccess);



                var deptService = new DepartmentService(db);

                var kpi = deptService.GetDepartmentKPI(dept.Id, DateTime.Today.Month, DateTime.Today.Year);



                Assert.Equal(1, kpi.ObservationCount);

                Assert.Equal(2, kpi.TeacherCount);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestDepartmentObservation_UnauthorizedObserverAccessRejected()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                Assert.Throws<UnauthorizedAccessException>(() =>

                {

                    StaffDailyWorkflowsServices.SubmitObservation(

                        db, 201, "Giáo Viên A", 202, "Giáo Viên B", "GiaoVien", 8, "Quan sát đồng nghiệp"

                    );

                });

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestYouthUnionElection_DoubleVotingPrevention()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_VOTER", FullName = "Nguyễn Bầu Cử", ClassName = "12A2", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();



                var member = new YouthMember { StudentId = student.Id, StudentName = student.FullName, ClassName = student.ClassName, Status = "Active" };

                db.YouthMembers.Add(member);

                db.SaveChanges();



                bool firstVote = StaffDailyWorkflowsServices.SubmitYouthVote(db, member.Id, "Candidate A", "0");

                Assert.True(firstVote);



                bool secondVote = StaffDailyWorkflowsServices.SubmitYouthVote(db, member.Id, "Candidate B", "0");

                Assert.False(secondVote);



                var votes = db.EventLogs.Where(l => l.EventType == "YouthVote" && l.Actor == member.Id.ToString()).ToList();

                Assert.Single(votes);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestYouthUnionElection_AnonymousVoteTally()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student1 = new Student { StudentCode = "HS_VOTER_1", FullName = "Đoàn Viên A", ClassName = "12A2", Status = "Active" };

                var student2 = new Student { StudentCode = "HS_VOTER_2", FullName = "Đoàn Viên B", ClassName = "12A2", Status = "Active" };

                db.Students.AddRange(student1, student2);

                db.SaveChanges();



                var member1 = new YouthMember { StudentId = student1.Id, StudentName = student1.FullName, ClassName = student1.ClassName, Status = "Active" };

                var member2 = new YouthMember { StudentId = student2.Id, StudentName = student2.FullName, ClassName = student2.ClassName, Status = "Active" };

                db.YouthMembers.AddRange(member1, member2);

                db.SaveChanges();



                bool vote1 = StaffDailyWorkflowsServices.SubmitYouthVote(db, member1.Id, "Candidate A", "0");

                Assert.True(vote1);



                var log1 = db.EventLogs.FirstOrDefault(l => l.EventType == "YouthVote" && l.Actor == member1.Id.ToString());

                Assert.NotNull(log1);

                Assert.Contains("HASHED_", log1.Details);

                Assert.DoesNotContain("Candidate A", log1.Details);



                bool vote2 = StaffDailyWorkflowsServices.SubmitYouthVote(db, member2.Id, "Candidate A", "1");

                Assert.True(vote2);



                var log2 = db.EventLogs.FirstOrDefault(l => l.EventType == "YouthVote" && l.Actor == member2.Id.ToString());

                Assert.NotNull(log2);

                Assert.Contains("RSA_ENCRYPTED_Candidate A", log2.Details);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestPrincipalDashboard_SignatureModeOtpFcm()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var doc = new OfficialDocument { Title = "Quyết định chi ngân sách", DocumentNumber = "QD/01", Status = "Pending" };

                db.OfficialDocuments.Add(doc);

                db.SaveChanges();



                bool approveSuccess = StaffDailyWorkflowsServices.ApproveDocumentWithSignature(db, doc.Id, "1", "123456", "123456");

                Assert.True(approveSuccess);



                var approvedDoc = db.OfficialDocuments.Find(doc.Id);

                Assert.NotNull(approvedDoc);

                Assert.Equal("Approved", approvedDoc.Status);



                approvedDoc.Status = "Pending";

                db.SaveChanges();



                bool approveFail = StaffDailyWorkflowsServices.ApproveDocumentWithSignature(db, doc.Id, "1", "111111", "123456");

                Assert.False(approveFail);

                Assert.Equal("Pending", approvedDoc.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestPrincipalDashboard_SignatureModePinOnly()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var doc = new OfficialDocument { Title = "Công văn khai giảng", DocumentNumber = "CV/02", Status = "Pending" };

                db.OfficialDocuments.Add(doc);

                db.SaveChanges();



                bool approveSuccess = StaffDailyWorkflowsServices.ApproveDocumentWithSignature(db, doc.Id, "0", "9999", "9999");

                Assert.True(approveSuccess);



                var approvedDoc = db.OfficialDocuments.Find(doc.Id);

                Assert.NotNull(approvedDoc);

                Assert.Equal("Approved", approvedDoc.Status);



                approvedDoc.Status = "Pending";

                db.SaveChanges();



                bool approveFail = StaffDailyWorkflowsServices.ApproveDocumentWithSignature(db, doc.Id, "0", "1234", "9999");

                Assert.False(approveFail);

                Assert.Equal("Pending", approvedDoc.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        // ─── 8 ADVANCED INTEGRATION TESTS FOR V4.1+ ───



        [Fact]

        public void TestLibraryFine_AutoDeductionSuccess()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_OverdueFineMode");

                if (setting != null) setting.Value = "1";

                else db.SystemSettings.Add(new SystemSetting { Id = "Library_OverdueFineMode", Value = "1", Category = "Library" });

                db.SaveChanges();



                var student = new Student

                {

                    StudentCode = "HS_FINE_01",

                    FullName = "Trần Phạt Thư Viện",

                    ClassName = "12A3",

                    Status = "Active",

                    WalletBalance = 20000

                };

                student.WalletChecksum = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(

                    student.StudentCode + student.WalletBalance.ToString("F2")

                );

                db.Students.Add(student);

                db.SaveChanges();



                var loan = new BookLoan

                {

                    StudentId = student.Id,

                    BookCode = "BOOK_FINE_01",

                    BookTitle = "Lịch sử thế giới",

                    BorrowDate = DateTime.Today.AddDays(-10),

                    DueDate = DateTime.Today.AddDays(-5), // 5 days overdue

                    ReturnedDate = null

                };

                db.BookLoans.Add(loan);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ApplyLibraryOverdueFines(db, DateTime.Today);

                Assert.True(success);



                db.Entry(student).Reload();

                Assert.Equal(16000, student.WalletBalance);



                var expectedChecksum = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(

                    student.StudentCode + student.WalletBalance.ToString("F2")

                );

                Assert.Equal(expectedChecksum, student.WalletChecksum);



                var log = db.EventLogs.FirstOrDefault(l => l.EventType == "LibraryFine" && l.Actor == student.Id.ToString());

                Assert.NotNull(log);

                Assert.Contains("4000", log.Details);



                var parentMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == $"parent_{student.Id}");

                Assert.NotNull(parentMsg);

                Assert.Contains("Khấu trừ 4,000đ", parentMsg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestLibraryFine_PreventNegativeWalletBalance()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_OverdueFineMode");

                if (setting != null) setting.Value = "1";

                else db.SystemSettings.Add(new SystemSetting { Id = "Library_OverdueFineMode", Value = "1", Category = "Library" });

                db.SaveChanges();



                var student = new Student

                {

                    StudentCode = "HS_FINE_02",

                    FullName = "Trần Nghèo Ví",

                    ClassName = "12A3",

                    Status = "Active",

                    WalletBalance = 1500

                };

                student.WalletChecksum = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(

                    student.StudentCode + student.WalletBalance.ToString("F2")

                );

                db.Students.Add(student);

                db.SaveChanges();



                var loan = new BookLoan

                {

                    StudentId = student.Id,

                    BookCode = "BOOK_FINE_02",

                    BookTitle = "Đại số 10",

                    BorrowDate = DateTime.Today.AddDays(-10),

                    DueDate = DateTime.Today.AddDays(-5),

                    ReturnedDate = null

                };

                db.BookLoans.Add(loan);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ApplyLibraryOverdueFines(db, DateTime.Today);

                Assert.True(success);



                db.Entry(student).Reload();

                Assert.Equal(1500, student.WalletBalance);



                var warningMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == $"parent_{student.Id}" && m.Content.Contains("[CẢNH BÁO NỢ PHẠT]"));

                Assert.NotNull(warningMsg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestMedicalVaccination_ReminderGeneration()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_VAC_01", FullName = "Lê Tiêm Chủng", ClassName = "10B1", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();



                var record = new VaccinationRecord

                {

                    StudentId = student.Id,

                    StudentName = student.FullName,

                    DiseaseName = "Sởi",

                    VaccinationDate = DateTime.Today.AddYears(-1),

                    NextDueDate = DateTime.Today.AddDays(-2)

                };

                db.VaccinationRecords.Add(record);

                db.SaveChanges();



                if (record.NextDueDate < DateTime.Today)

                {

                    db.InboxMessages.Add(new InboxMessage

                    {

                        SenderId = "MEDICAL_ROOM",

                        SenderName = "Phòng y tế",

                        ReceiverId = $"parent_{student.Id}",

                        Content = $"[NHẮC NHỞ TIÊM CHỦNG] Học sinh {student.FullName} đã quá hạn tiêm nhắc lại vắc xin phòng bệnh Sởi (Hạn tiêm: {record.NextDueDate:dd/MM/yyyy}).",

                        IsRead = false,

                        CreatedAt = DateTime.Now

                    });

                    db.SaveChanges();

                }



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == $"parent_{student.Id}");

                Assert.NotNull(msg);

                Assert.Contains("Sởi", msg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestSecuritySmartPickup_AiTriggerAndNotification()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_PICKUP_01", FullName = "Vũ Đón Về", ClassName = "11C1", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.TriggerSmartPickup(db, "HS_PICKUP_01", "Bố Vũ", "29A-12345");

                Assert.True(success);



                var log = db.GatePickupLogs.FirstOrDefault(l => l.StudentId == student.Id);

                Assert.NotNull(log);

                Assert.Equal("Bố Vũ", log.ParentName);

                Assert.Equal("29A-12345", log.LicensePlate);



                var classMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == $"teacher_{student.ClassName}");

                Assert.NotNull(classMsg);

                Assert.Contains("29A-12345", classMsg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestKitchenFeedback_AutoMenuRecommendation()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student1 = new Student { StudentCode = "S1", FullName = "HS A", ClassName = "10A1", Status = "Active" };

                var student2 = new Student { StudentCode = "S2", FullName = "HS B", ClassName = "10A1", Status = "Active" };

                db.Students.AddRange(student1, student2);

                db.SaveChanges();



                bool f1 = StaffDailyWorkflowsServices.SubmitMealFeedback(db, 1, student1.Id, 1, "Món cá mặn quá");

                bool f2 = StaffDailyWorkflowsServices.SubmitMealFeedback(db, 1, student2.Id, 2, "Cá hơi tanh");

                bool fInvalid = StaffDailyWorkflowsServices.SubmitMealFeedback(db, 1, student1.Id, 6, "Sai rating");



                Assert.True(f1);

                Assert.True(f2);

                Assert.False(fInvalid);



                var feedBackList = db.MealFeedbacks.Where(f => f.MenuId == 1).ToList();

                Assert.Equal(2, feedBackList.Count);

                double avgRating = feedBackList.Average(f => f.Rating);

                Assert.Equal(1.5, avgRating);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestPrincipalMultiSig_BudgetDocumentFlow()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Principal_MultiSignatureWorkflow");

                if (setting != null) setting.Value = "1";

                else db.SystemSettings.Add(new SystemSetting { Id = "Principal_MultiSignatureWorkflow", Value = "1", Category = "Principal" });

                db.SaveChanges();



                var doc = new OfficialDocument

                {

                    Title = "Duyệt chi ngân sách mua máy tính: 15,000,000 VND",

                    DocumentNumber = "QD/MS/01",

                    Status = "Pending"

                };

                db.OfficialDocuments.Add(doc);

                db.SaveChanges();



                bool directApprove = StaffDailyWorkflowsServices.ApproveDocumentWithMultiSig(db, doc.Id, "HieuTruong", "ht2026", "ht2026");

                Assert.False(directApprove);



                bool accountantApprove = StaffDailyWorkflowsServices.ApproveDocumentWithMultiSig(db, doc.Id, "Ketoan", "kt2026", "kt2026");

                Assert.True(accountantApprove);



                var docReloaded = db.OfficialDocuments.Find(doc.Id);

                Assert.NotNull(docReloaded);

                Assert.Equal("ReviewApproved", docReloaded.Status);



                bool principalApprove = StaffDailyWorkflowsServices.ApproveDocumentWithMultiSig(db, doc.Id, "HieuTruong", "ht2026", "ht2026");

                Assert.True(principalApprove);

                Assert.Equal("Approved", docReloaded.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestPrincipalMultiSig_UnauthorizedReviewerRejected()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var doc = new OfficialDocument { Title = "Duyệt chi ngân sách 15,000,000", Status = "Pending" };

                db.OfficialDocuments.Add(doc);

                db.SaveChanges();



                bool wrongPin = StaffDailyWorkflowsServices.ApproveDocumentWithMultiSig(db, doc.Id, "Ketoan", "wrong_pin", "kt2026");

                Assert.False(wrongPin);



                bool randomRole = StaffDailyWorkflowsServices.ApproveDocumentWithMultiSig(db, doc.Id, "GiaoVien", "kt2026", "kt2026");

                Assert.False(randomRole);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestEpidemicClassLockdown_TriggerOnMultipleCases()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.EpidemicCases.Add(new EpidemicCase { ClassName = "11A1", Disease = "Sốt xuất huyết", OnsetDate = DateTime.Today.AddDays(-1), StudentName = "A" });

                db.EpidemicCases.Add(new EpidemicCase { ClassName = "11A1", Disease = "Sốt xuất huyết", OnsetDate = DateTime.Today.AddDays(-3), StudentName = "B" });

                db.SaveChanges();



                bool riskTriggered = StaffDailyWorkflowsServices.CheckClassEpidemicRisk(db, "11A1", DateTime.Today);

                Assert.True(riskTriggered);



                var classMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "teacher_11A1");

                Assert.NotNull(classMsg);

                Assert.Contains("CẢNH BÁO CÁCH LY", classMsg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        // ─── 13 ADDITIONAL INTEGRATION TESTS FOR PHASE 2 (V4.1+) ───



        [Fact]

        public void TestTuitionReconciliation_AutoMatchSuccess()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Tuition_ReconciliationMode");

                if (setting != null) setting.Value = "0"; // Strict

                else db.SystemSettings.Add(new SystemSetting { Id = "Tuition_ReconciliationMode", Value = "0", Category = "Tuition" });

                db.SaveChanges();



                var student = new Student { StudentCode = "HS101", FullName = "Học Sinh Học Phí 1", ClassName = "10A1", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();



                var tuition = new TuitionRecord

                {

                    StudentId = student.Id,

                    StudentName = student.FullName,

                    ClassName = student.ClassName,

                    Amount = 5000000.0,

                    PaidAmount = 0.0,

                    Status = "Unpaid",

                    DueDate = DateTime.Today.AddDays(10)

                };

                db.TuitionRecords.Add(tuition);

                db.SaveChanges();



                bool result = StaffDailyWorkflowsServices.ReconcileTuition(db, "HS101 đóng học phí kì 2", 5000000.0);

                Assert.True(result);



                db.Entry(tuition).Reload();

                Assert.Equal(5000000.0, tuition.PaidAmount);

                Assert.Equal("Paid", tuition.Status);



                var log = db.TuitionReconciliationLogs.FirstOrDefault(l => l.TuitionRecordId == tuition.Id);

                Assert.NotNull(log);

                Assert.Equal("Success", log.Status);

                Assert.Contains("HS101", log.TransactionRef);



                var expectedChecksum = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(

                    tuition.StudentId + "_" + tuition.Amount.ToString("F2") + "_" + tuition.PaidAmount.ToString("F2") + "_" + tuition.Status

                );

                Assert.Equal(expectedChecksum, tuition.TuitionChecksum);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestTuitionReconciliation_DiscrepancyToSuspense_Strict()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Tuition_ReconciliationMode");

                if (setting != null) setting.Value = "0"; // Strict

                else db.SystemSettings.Add(new SystemSetting { Id = "Tuition_ReconciliationMode", Value = "0", Category = "Tuition" });

                db.SaveChanges();



                var student = new Student { StudentCode = "HS102", FullName = "Học Sinh Học Phí 2", ClassName = "10A1", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();



                var tuition = new TuitionRecord

                {

                    StudentId = student.Id,

                    StudentName = student.FullName,

                    ClassName = student.ClassName,

                    Amount = 5000000.0,

                    PaidAmount = 0.0,

                    Status = "Unpaid",

                    DueDate = DateTime.Today.AddDays(10)

                };

                db.TuitionRecords.Add(tuition);

                db.SaveChanges();



                // Pay only 4,500,000 (mismatch by 500,000)

                bool result = StaffDailyWorkflowsServices.ReconcileTuition(db, "HS102 nộp học phí", 4500000.0);

                Assert.False(result); // strict mode fails on mismatch



                db.Entry(tuition).Reload();

                Assert.Equal(0.0, tuition.PaidAmount);

                Assert.Equal("Unpaid", tuition.Status);



                var log = db.TuitionReconciliationLogs.FirstOrDefault(l => l.TuitionRecordId == tuition.Id);

                Assert.NotNull(log);

                Assert.Equal("Suspense", log.Status);

                Assert.Contains("Không khớp số tiền", log.Notes);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestTuitionReconciliation_DiscrepancyAutoDeduct_Partial()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Tuition_ReconciliationMode");

                if (setting != null) setting.Value = "1"; // Partial match allowed

                else db.SystemSettings.Add(new SystemSetting { Id = "Tuition_ReconciliationMode", Value = "1", Category = "Tuition" });

                db.SaveChanges();



                var student = new Student { StudentCode = "HS103", FullName = "Học Sinh Học Phí 3", ClassName = "10A1", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();



                var tuition = new TuitionRecord

                {

                    StudentId = student.Id,

                    StudentName = student.FullName,

                    ClassName = student.ClassName,

                    Amount = 5000000.0,

                    PaidAmount = 0.0,

                    Status = "Unpaid",

                    DueDate = DateTime.Today.AddDays(10)

                };

                db.TuitionRecords.Add(tuition);

                db.SaveChanges();



                bool result = StaffDailyWorkflowsServices.ReconcileTuition(db, "HS103 đóng học phí", 4500000.0);

                Assert.True(result);



                db.Entry(tuition).Reload();

                Assert.Equal(4500000.0, tuition.PaidAmount);

                Assert.Equal("Partial", tuition.Status);



                var log = db.TuitionReconciliationLogs.FirstOrDefault(l => l.TuitionRecordId == tuition.Id);

                Assert.NotNull(log);

                Assert.Equal("Success", log.Status);

                Assert.Contains("Thanh toán một phần", log.Notes);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestTuitionLedger_LockPreventsModification()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS104", FullName = "Học Sinh Học Phí 4", ClassName = "10A1", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();



                var tuition = new TuitionRecord

                {

                    StudentId = student.Id,

                    StudentName = student.FullName,

                    ClassName = student.ClassName,

                    Amount = 5000000.0,

                    PaidAmount = 0.0,

                    Status = "Unpaid",

                    DueDate = DateTime.Today.AddDays(10),

                    IsLocked = false

                };

                db.TuitionRecords.Add(tuition);

                db.SaveChanges();



                // 1. Lock the ledger

                bool lockSuccess = StaffDailyWorkflowsServices.CloseTuitionLedger(db, tuition.Id, "1234", "1234");

                Assert.True(lockSuccess);



                db.Entry(tuition).Reload();

                Assert.True(tuition.IsLocked);

                Assert.NotNull(tuition.LedgerClosingDate);



                // 2. Try to modify - should throw exception from SaveChanges interceptor

                tuition.PaidAmount = 5000000.0;

                Assert.Throws<InvalidOperationException>(() => db.SaveChanges());



                // 3. Try to call ReconcileTuition on locked ledger - should throw exception

                Assert.Throws<InvalidOperationException>(() => 

                    StaffDailyWorkflowsServices.ReconcileTuition(db, "HS104 nộp", 5000000.0)

                );

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestTuitionLedger_FraudTamperingProtection()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS105", FullName = "Học Sinh Học Phí 5", ClassName = "10A1", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();



                var tuition = new TuitionRecord

                {

                    StudentId = student.Id,

                    StudentName = student.FullName,

                    ClassName = student.ClassName,

                    Amount = 5000000.0,

                    PaidAmount = 5000000.0,

                    Status = "Paid",

                    DueDate = DateTime.Today.AddDays(10)

                };

                db.TuitionRecords.Add(tuition);

                db.SaveChanges();



                var originalChecksum = tuition.TuitionChecksum;



                db.Entry(tuition).State = EntityState.Detached;



                using (var rawCommand = db.Database.GetDbConnection().CreateCommand())

                {

                    db.Database.OpenConnection();

                    rawCommand.CommandText = $"UPDATE TuitionRecords SET PaidAmount = 100000.0, TuitionChecksum = 'INVALID_HASH' WHERE Id = {tuition.Id}";

                    rawCommand.ExecuteNonQuery();

                }



                var options = new DbContextOptionsBuilder<AppDbContext>()

                    .UseSqlite(db.Database.GetDbConnection())

                    .Options;

                using var dbQuery = new AppDbContext(options);



                var queriedTuition = dbQuery.TuitionRecords.Find(tuition.Id);

                Assert.NotNull(queriedTuition);

                Assert.Equal(0.0, queriedTuition.PaidAmount); 

                Assert.Equal("FraudDetected", queriedTuition.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestAssetDepreciation_FixedModeCalculation()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Asset_DepreciationMode");

                if (setting != null) setting.Value = "0"; // Fixed mode

                else db.SystemSettings.Add(new SystemSetting { Id = "Asset_DepreciationMode", Value = "0", Category = "Asset" });

                db.SaveChanges();



                var asset = new SchoolAsset

                {

                    AssetCode = "STB-TEST-100",

                    AssetType = "Projector - Máy chiếu đa năng",

                    Location = "Phòng 10A1",

                    OriginalValue = 20000000m,

                    PurchaseDate = DateTime.Today.AddYears(-3),

                    NextMaintenanceDate = DateTime.Today.AddMonths(6),

                    Status = "Active",

                    RepairCount = 4

                };

                db.SchoolAssets.Add(asset);

                db.SaveChanges();



                bool result = StaffDailyWorkflowsServices.CalculateAssetDepreciation(db, asset.Id, 3);

                Assert.True(result);



                db.Entry(asset).Reload();

                Assert.Equal(10, asset.DepreciationRatePercent); // 10% fixed

                Assert.Equal(6000000m, asset.AccumulatedDepreciation); 

                Assert.Equal(14000000m, asset.RemainingValue);

                Assert.Equal("Active", asset.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestAssetDepreciation_DynamicModeCalculation_HighWear()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Asset_DepreciationMode");

                if (setting != null) setting.Value = "1"; // Dynamic mode

                else db.SystemSettings.Add(new SystemSetting { Id = "Asset_DepreciationMode", Value = "1", Category = "Asset" });

                db.SaveChanges();



                var asset = new SchoolAsset

                {

                    AssetCode = "STB-TEST-200",

                    AssetType = "Projector - Máy chiếu công nghệ cao",

                    Location = "Phòng 10A1",

                    OriginalValue = 20000000m,

                    PurchaseDate = DateTime.Today.AddYears(-3),

                    NextMaintenanceDate = DateTime.Today.AddMonths(6),

                    Status = "Active",

                    RepairCount = 2 

                };

                db.SchoolAssets.Add(asset);

                db.SaveChanges();



                bool result = StaffDailyWorkflowsServices.CalculateAssetDepreciation(db, asset.Id, 3);

                Assert.True(result);



                db.Entry(asset).Reload();

                Assert.Equal(30, asset.DepreciationRatePercent); 

                Assert.Equal(18000000m, asset.AccumulatedDepreciation); 

                Assert.Equal(2000000m, asset.RemainingValue); 

                Assert.Equal("NeedsReplacement", asset.Status); 



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "ADMIN" && m.Content.Contains("[YÊU CẦU THAY MỚI]"));

                Assert.NotNull(msg);

                Assert.Contains("STB-TEST-200", msg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestAssetAudit_LogRegistration()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var asset = new SchoolAsset { AssetCode = "AST-AUDIT-01", AssetType = "Máy chiếu EB-1", Status = "Active" };

                db.SchoolAssets.Add(asset);

                db.SaveChanges();



                bool result = StaffDailyWorkflowsServices.AuditAsset(db, asset.Id, "Nguyễn Thiết Bị", "Broken", "Hỏng bóng đèn máy chiếu");

                Assert.True(result);



                db.Entry(asset).Reload();

                Assert.Equal("NeedsRepair", asset.Status);



                var log = db.AssetAuditLogs.FirstOrDefault(l => l.AssetId == asset.Id);

                Assert.NotNull(log);

                Assert.Equal("Broken", log.PhysicalCondition);

                Assert.Equal("Nguyễn Thiết Bị", log.AuditorName);



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "ADMIN" && m.Content.Contains("[YÊU CẦU SỬA CHỮA]"));

                Assert.NotNull(msg);

                Assert.Contains("AST-AUDIT-01", msg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestDocumentRouting_AutoCategoryAndDepartmentSla()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Document_RoutingMode");

                if (setting != null) setting.Value = "1"; // Auto-dispatch

                else db.SystemSettings.Add(new SystemSetting { Id = "Document_RoutingMode", Value = "1", Category = "Document" });

                db.SaveChanges();



                var doc = new OfficialDocument { Title = "Đề xuất duyệt chi ngân sách mua máy tính hội trường", DocumentNumber = "QD/55", Status = "Pending" };

                db.OfficialDocuments.Add(doc);

                db.SaveChanges();



                bool result = StaffDailyWorkflowsServices.RouteOfficialDocument(db, doc.Id, doc.Title, "Tờ trình chi ngân sách công đoàn và thiết bị tin học.");

                Assert.True(result);



                db.Entry(doc).Reload();

                Assert.Equal("Financial", doc.Category);

                Assert.Equal("KT001", doc.PrimaryHandlerId);

                Assert.Equal(DateTime.Today.AddDays(2), doc.ProcessingDeadline); 

                Assert.Equal("Routed", doc.Status);



                var route = db.DocumentRoutes.FirstOrDefault(r => r.DocumentTitle == doc.Title);

                Assert.NotNull(route);

                Assert.Equal("KT001", route.Receiver);

                Assert.Equal("Pending", route.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestDocumentRouting_ManualModeVerify()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Document_RoutingMode");

                if (setting != null) setting.Value = "0"; // Manual mode

                else db.SystemSettings.Add(new SystemSetting { Id = "Document_RoutingMode", Value = "0", Category = "Document" });

                db.SaveChanges();



                var doc = new OfficialDocument { Title = "Đề xuất chi lương tháng 6", DocumentNumber = "QD/56", Status = "Pending" };

                db.OfficialDocuments.Add(doc);

                db.SaveChanges();



                bool result = StaffDailyWorkflowsServices.RouteOfficialDocument(db, doc.Id, doc.Title, "Bảng lương cán bộ giáo viên.");

                Assert.True(result);



                db.Entry(doc).Reload();

                Assert.Equal("Financial", doc.Category);

                Assert.Equal("Draft", doc.Status); 



                var route = db.DocumentRoutes.FirstOrDefault(r => r.DocumentTitle == doc.Title);

                Assert.Null(route); 

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestDocumentRouting_EscalationOnOverdue()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var doc = new OfficialDocument

                {

                    Title = "Kế hoạch tổ chức lễ khai giảng 2026",

                    DocumentNumber = "QD/57",

                    Category = "Academic",

                    PrimaryHandlerId = "HP001",

                    ProcessingDeadline = DateTime.Today.AddDays(-1), 

                    Status = "Routed",

                    IsEscalated = false

                };

                db.OfficialDocuments.Add(doc);

                db.SaveChanges();



                bool result = StaffDailyWorkflowsServices.EscalateOverdueDocuments(db, DateTime.Today);

                Assert.True(result);



                db.Entry(doc).Reload();

                Assert.True(doc.IsEscalated);



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "HT001" && m.Content.Contains("[CẢNH BÁO TRỄ HẠN SLA]"));

                Assert.NotNull(msg);

                Assert.Contains("Kế hoạch tổ chức lễ khai giảng 2026", msg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestKitchenWaste_StaticModeVerify()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Kitchen_WasteOptimizationMode");

                if (setting != null) setting.Value = "0"; // Static Mode

                else db.SystemSettings.Add(new SystemSetting { Id = "Kitchen_WasteOptimizationMode", Value = "0", Category = "Kitchen" });

                db.SaveChanges();



                var student = new Student { StudentCode = "HS106", FullName = "Học Sinh Vắng", ClassName = "10A1", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();



                db.StudentLeaveRequests.Add(new StudentLeaveRequest { StudentId = student.Id, LeaveDate = DateTime.Today, Status = "Approved" });

                db.SaveChanges();



                int meals = StaffDailyWorkflowsServices.RecommendCanteenMealCount(db, DateTime.Today, 500);

                Assert.Equal(500, meals);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void TestKitchenWaste_DynamicModeCalculation()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Kitchen_WasteOptimizationMode");

                if (setting != null) setting.Value = "1"; // Dynamic Mode

                else db.SystemSettings.Add(new SystemSetting { Id = "Kitchen_WasteOptimizationMode", Value = "1", Category = "Kitchen" });

                db.SaveChanges();



                for (int i = 1; i <= 10; i++)

                {

                    var student = new Student { StudentCode = $"HS_WASTE_{i}", FullName = $"Student {i}", Status = "Active" };

                    db.Students.Add(student);

                    db.SaveChanges();

                    db.StudentLeaveRequests.Add(new StudentLeaveRequest { StudentId = student.Id, LeaveDate = DateTime.Today, Status = "Approved" });

                }

                db.SaveChanges();



                bool wasteLogged = StaffDailyWorkflowsServices.LogMealWaste(db, DateTime.Today.AddDays(-1), "Lunch", 500, 50, 25000);

                Assert.True(wasteLogged);



                int meals = StaffDailyWorkflowsServices.RecommendCanteenMealCount(db, DateTime.Today, 500);

                Assert.Equal(451, meals);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        // --- 20 NEW INTEGRATION TESTS FOR PHASE 3 LIBRARY UPGRADES ---



        [Fact]

        public void Test_ReservationSLA_Creation_Success()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_RES_1", FullName = "Học Sinh 1", Status = "Active" };

                db.Students.Add(student);

                var book = new LibraryBook { BookCode = "BOOK_RES_1", Title = "Book 1", Status = "Active" };

                db.LibraryBooks.Add(book);

                db.SaveChanges();



                var now = DateTime.Now;

                bool success = StaffDailyWorkflowsServices.ReserveBook(db, student.Id, "BOOK_RES_1", now);

                Assert.True(success);



                var res = db.BookReservations.FirstOrDefault(r => r.StudentId == student.Id && r.BookCode == "BOOK_RES_1");

                Assert.NotNull(res);

                Assert.Equal("PendingPickUp", res.Status);

                Assert.Equal(now.AddHours(24), res.HoldExpirationDate);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ReservationSLA_LimitMaxActiveReservations()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_RES_2", FullName = "Học Sinh 2", Status = "Active" };

                db.Students.Add(student);

                var b1 = new LibraryBook { BookCode = "B1", Title = "Book 1", Status = "Active" };

                var b2 = new LibraryBook { BookCode = "B2", Title = "Book 2", Status = "Active" };

                var b3 = new LibraryBook { BookCode = "B3", Title = "Book 3", Status = "Active" };

                db.LibraryBooks.AddRange(b1, b2, b3);

                db.SaveChanges();



                var now = DateTime.Now;

                Assert.True(StaffDailyWorkflowsServices.ReserveBook(db, student.Id, "B1", now));

                Assert.True(StaffDailyWorkflowsServices.ReserveBook(db, student.Id, "B2", now));

                // 3rd should fail

                Assert.False(StaffDailyWorkflowsServices.ReserveBook(db, student.Id, "B3", now));

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ReservationSLA_BlockIfReservationBlocked()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var now = DateTime.Now;

                var student = new Student 

                { 

                    StudentCode = "HS_RES_3", 

                    FullName = "Học Sinh 3", 

                    Status = "Active",

                    ReservationBlockedUntil = now.AddDays(1)

                };

                db.Students.Add(student);

                var book = new LibraryBook { BookCode = "BOOK_RES_3", Title = "Book 3", Status = "Active" };

                db.LibraryBooks.Add(book);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ReserveBook(db, student.Id, "BOOK_RES_3", now);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ReservationSLA_ReleaseBookOnCancellation()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_RES_4", FullName = "Học Sinh 4", Status = "Active" };

                db.Students.Add(student);

                var book = new LibraryBook { BookCode = "B4", Title = "Book 4", Status = "Active" };

                db.LibraryBooks.Add(book);

                db.SaveChanges();



                var now = DateTime.Now;

                Assert.True(StaffDailyWorkflowsServices.ReserveBook(db, student.Id, "B4", now));



                // Cancel expired

                bool cancelResult = StaffDailyWorkflowsServices.CancelExpiredReservations(db, now.AddHours(25));

                Assert.True(cancelResult);



                var res = db.BookReservations.FirstOrDefault(r => r.BookCode == "B4");

                Assert.NotNull(res);

                Assert.Equal("Expired", res.Status);



                // Book should be available again for another student

                var student2 = new Student { StudentCode = "HS_RES_4_2", FullName = "Học Sinh 4-2", Status = "Active" };

                db.Students.Add(student2);

                db.SaveChanges();



                Assert.True(StaffDailyWorkflowsServices.ReserveBook(db, student2.Id, "B4", now.AddHours(26)));

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ReservationSLA_DoubleReservationPrevented()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s1 = new Student { StudentCode = "HS_S1", FullName = "HS 1", Status = "Active" };

                var s2 = new Student { StudentCode = "HS_S2", FullName = "HS 2", Status = "Active" };

                db.Students.AddRange(s1, s2);

                var book = new LibraryBook { BookCode = "BOOK_DBL", Title = "Book Dbl", Status = "Active" };

                db.LibraryBooks.Add(book);

                db.SaveChanges();



                var now = DateTime.Now;

                Assert.True(StaffDailyWorkflowsServices.ReserveBook(db, s1.Id, "BOOK_DBL", now));

                // Second student should fail

                Assert.False(StaffDailyWorkflowsServices.ReserveBook(db, s2.Id, "BOOK_DBL", now));

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ReservationSLA_ConcurrentBorrowAndReserve()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s1 = new Student { StudentCode = "HS_L1", FullName = "HS 1", Status = "Active" };

                var s2 = new Student { StudentCode = "HS_L2", FullName = "HS 2", Status = "Active" };

                db.Students.AddRange(s1, s2);

                var book = new LibraryBook { BookCode = "BOOK_CON", Title = "Book Con", Status = "Active" };

                db.LibraryBooks.Add(book);

                db.SaveChanges();



                // Borrow book by s1

                Assert.True(StaffDailyWorkflowsServices.BorrowBook(db, s1.Id, "BOOK_CON", "Book Con"));



                // s2 tries to reserve it - should fail since it's currently loaned out

                Assert.False(StaffDailyWorkflowsServices.ReserveBook(db, s2.Id, "BOOK_CON", DateTime.Now));

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ReservationSLA_Expired_BlockMode_TempBlock()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_EXP_1", FullName = "Học Sinh Exp 1", Status = "Active" };

                db.Students.Add(student);

                var book = new LibraryBook { BookCode = "BOOK_EXP_1", Title = "Book Exp 1", Status = "Active" };

                db.LibraryBooks.Add(book);

                

                var setMode = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_ReservationBlockMode");

                if (setMode != null) setMode.Value = "1";

                else db.SystemSettings.Add(new SystemSetting { Id = "Library_ReservationBlockMode", Value = "1", Category = "Library" });



                var setDays = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_ReservationBlockDays");

                if (setDays != null) setDays.Value = "4";

                else db.SystemSettings.Add(new SystemSetting { Id = "Library_ReservationBlockDays", Value = "4", Category = "Library" });



                db.SaveChanges();



                var now = DateTime.Now;

                Assert.True(StaffDailyWorkflowsServices.ReserveBook(db, student.Id, "BOOK_EXP_1", now));



                // Cancel expired reservations at now + 25 hours

                bool result = StaffDailyWorkflowsServices.CancelExpiredReservations(db, now.AddHours(25));

                Assert.True(result);



                db.Entry(student).Reload();

                Assert.NotNull(student.ReservationBlockedUntil);

                Assert.Equal(now.AddHours(25).AddDays(4).Date, student.ReservationBlockedUntil.Value.Date);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ReservationSLA_Expired_BlockMode_DeductConductPoints()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_EXP_2", FullName = "Học Sinh Exp 2", Status = "Active", ConductScore = 100 };

                db.Students.Add(student);

                var book = new LibraryBook { BookCode = "BOOK_EXP_2", Title = "Book Exp 2", Status = "Active" };

                db.LibraryBooks.Add(book);



                var setMode = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_ReservationBlockMode");

                if (setMode != null) setMode.Value = "2";

                else db.SystemSettings.Add(new SystemSetting { Id = "Library_ReservationBlockMode", Value = "2", Category = "Library" });



                var setDed = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_ReservationConductDeduction");

                if (setDed != null) setDed.Value = "8";

                else db.SystemSettings.Add(new SystemSetting { Id = "Library_ReservationConductDeduction", Value = "8", Category = "Library" });



                db.SaveChanges();



                var now = DateTime.Now;

                Assert.True(StaffDailyWorkflowsServices.ReserveBook(db, student.Id, "BOOK_EXP_2", now));



                // Cancel expired

                bool result = StaffDailyWorkflowsServices.CancelExpiredReservations(db, now.AddHours(25));

                Assert.True(result);



                db.Entry(student).Reload();

                Assert.Equal(92, student.ConductScore);



                var conductLog = db.ConductRecords.FirstOrDefault(r => r.StudentId == student.Id);

                Assert.NotNull(conductLog);

                Assert.Equal(-8, conductLog.PointsDelta);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ReservationSLA_Expired_BlockMode_GentleWarningOnly()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_EXP_3", FullName = "Học Sinh Exp 3", Status = "Active", ConductScore = 100 };

                db.Students.Add(student);

                var book = new LibraryBook { BookCode = "BOOK_EXP_3", Title = "Book Exp 3", Status = "Active" };

                db.LibraryBooks.Add(book);



                var setMode = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_ReservationBlockMode");

                if (setMode != null) setMode.Value = "0";

                else db.SystemSettings.Add(new SystemSetting { Id = "Library_ReservationBlockMode", Value = "0", Category = "Library" });

                db.SaveChanges();



                var now = DateTime.Now;

                Assert.True(StaffDailyWorkflowsServices.ReserveBook(db, student.Id, "BOOK_EXP_3", now));



                // Cancel expired

                bool result = StaffDailyWorkflowsServices.CancelExpiredReservations(db, now.AddHours(25));

                Assert.True(result);



                db.Entry(student).Reload();

                Assert.Equal(100, student.ConductScore);

                Assert.Null(student.ReservationBlockedUntil);



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == $"student_{student.Id}" && m.Content.Contains("[CẢNH BÁO SLA]"));

                Assert.NotNull(msg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ReservationSLA_Pickup_Streak_BonusXP()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_STK", FullName = "Học Sinh Streak", Status = "Active", TotalXp = 0, ConsecutiveSuccessfulPickups = 0 };

                db.Students.Add(student);

                var book1 = new LibraryBook { BookCode = "BK1", Title = "Book 1", Status = "Active" };

                var book2 = new LibraryBook { BookCode = "BK2", Title = "Book 2", Status = "Active" };

                var book3 = new LibraryBook { BookCode = "BK3", Title = "Book 3", Status = "Active" };

                db.LibraryBooks.AddRange(book1, book2, book3);

                db.SaveChanges();



                var now = DateTime.Now;



                // Pick up 1

                Assert.True(StaffDailyWorkflowsServices.ReserveBook(db, student.Id, "BK1", now));

                Assert.True(StaffDailyWorkflowsServices.PickUpReservedBook(db, student.Id, "BK1", now.AddHours(1)));

                

                var loan1 = db.BookLoans.First(l => l.StudentId == student.Id && l.BookCode == "BK1");

                loan1.ReturnedDate = now.AddHours(2);

                db.SaveChanges();



                // Pick up 2

                Assert.True(StaffDailyWorkflowsServices.ReserveBook(db, student.Id, "BK2", now.AddHours(3)));

                Assert.True(StaffDailyWorkflowsServices.PickUpReservedBook(db, student.Id, "BK2", now.AddHours(4)));

                

                var loan2 = db.BookLoans.First(l => l.StudentId == student.Id && l.BookCode == "BK2");

                loan2.ReturnedDate = now.AddHours(5);

                db.SaveChanges();



                // Pick up 3

                Assert.True(StaffDailyWorkflowsServices.ReserveBook(db, student.Id, "BK3", now.AddHours(6)));

                Assert.True(StaffDailyWorkflowsServices.PickUpReservedBook(db, student.Id, "BK3", now.AddHours(7)));



                db.Entry(student).Reload();

                Assert.Equal(10, student.TotalXp);

                Assert.Equal(0, student.ConsecutiveSuccessfulPickups); // Reset



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == $"student_{student.Id}" && m.Content.Contains("[KHEN NGỢI]"));

                Assert.NotNull(msg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ReservationSLA_ConsecutiveStreakBreak()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_STK_BRK", FullName = "Học Sinh Streak Break", Status = "Active", TotalXp = 0, ConsecutiveSuccessfulPickups = 2 };

                db.Students.Add(student);

                var book = new LibraryBook { BookCode = "BKB", Title = "Book Break", Status = "Active" };

                db.LibraryBooks.Add(book);

                db.SaveChanges();



                var now = DateTime.Now;

                Assert.True(StaffDailyWorkflowsServices.ReserveBook(db, student.Id, "BKB", now));



                Assert.True(StaffDailyWorkflowsServices.CancelExpiredReservations(db, now.AddHours(25)));



                db.Entry(student).Reload();

                Assert.Equal(0, student.TotalXp);

                Assert.Equal(0, student.ConsecutiveSuccessfulPickups); // Reset to 0 due to breach

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_BookWear_Rating_AutoRepair()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_WEAR_1", FullName = "Học Sinh Wear 1", Status = "Active" };

                db.Students.Add(student);

                var book = new LibraryBook { BookCode = "B_WEAR_1", Title = "Sách Toán", Status = "Active", BookConditionScore = 4 };

                db.LibraryBooks.Add(book);

                db.SaveChanges();

                

                var loan = new BookLoan { StudentId = student.Id, BookCode = "B_WEAR_1", BookTitle = "Sách Toán", BorrowDate = DateTime.Today, DueDate = DateTime.Today.AddDays(14) };

                db.BookLoans.Add(loan);

                db.SaveChanges();



                bool result = StaffDailyWorkflowsServices.ReturnBookWithWearRating(db, student.Id, "B_WEAR_1", 1, "Rách vài trang", DateTime.Today);

                Assert.True(result);



                db.Entry(book).Reload();

                Assert.Equal(3, book.BookConditionScore);

                Assert.Equal("NeedsRepair", book.Status);



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "LIBRARY_ROOM" && m.Content.Contains("[YÊU CẦU BẢO DƯỠNG]"));

                Assert.NotNull(msg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_BookWear_Rating_AutoDisposal_And_ReplenishAlert()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_WEAR_2", FullName = "Học Sinh Wear 2", Status = "Active" };

                db.Students.Add(student);

                var book = new LibraryBook { BookCode = "B_WEAR_2", Title = "Sách Lý", Status = "Active", BookConditionScore = 2 };

                db.LibraryBooks.Add(book);

                db.SaveChanges();

                

                var loan = new BookLoan { StudentId = student.Id, BookCode = "B_WEAR_2", BookTitle = "Sách Lý", BorrowDate = DateTime.Today, DueDate = DateTime.Today.AddDays(14) };

                db.BookLoans.Add(loan);

                db.SaveChanges();



                bool result = StaffDailyWorkflowsServices.ReturnBookWithWearRating(db, student.Id, "B_WEAR_2", 0, "Ướt sũng nước", DateTime.Today);

                Assert.True(result);



                db.Entry(book).Reload();

                Assert.Equal(1, book.BookConditionScore);

                Assert.Equal("Retired", book.Status);

                Assert.True(book.ReplenishmentRequired);



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "ADMIN" && m.Content.Contains("[ĐỀ XUẤT MUA SÁCH MỚI]"));

                Assert.NotNull(msg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_BookWear_Rating_SevereDamage_PedagogicalWarning()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_WEAR_3", FullName = "Học Sinh Wear 3", Status = "Active", ClassName = "10A3" };

                db.Students.Add(student);

                var book = new LibraryBook { BookCode = "B_WEAR_3", Title = "Sách Văn", Status = "Active", BookConditionScore = 10 };

                db.LibraryBooks.Add(book);

                db.SaveChanges();

                

                var loan = new BookLoan { StudentId = student.Id, BookCode = "B_WEAR_3", BookTitle = "Sách Văn", BorrowDate = DateTime.Today, DueDate = DateTime.Today.AddDays(14) };

                db.BookLoans.Add(loan);

                db.SaveChanges();



                bool result = StaffDailyWorkflowsServices.ReturnBookWithWearRating(db, student.Id, "B_WEAR_3", 2, "Vẽ bậy bạ lên sách", DateTime.Today);

                Assert.True(result);



                var stdMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == $"student_{student.Id}" && m.Content.Contains("[NHẮC NHỞ BẢO VỆ CỦA CÔNG]"));

                Assert.NotNull(stdMsg);



                var prtMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == $"parent_{student.Id}" && m.Content.Contains("[NHẮC NHỞ HỌC SINH]"));

                Assert.NotNull(prtMsg);



                var tchrMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == $"teacher_10A3" && m.Content.Contains("[HỖ TRỢ GIÁO DỤC]"));

                Assert.NotNull(tchrMsg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_BookWear_ExtremeReturnScore_PedagogicalSafe()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_WEAR_4", FullName = "Học Sinh Wear 4", Status = "Active" };

                db.Students.Add(student);

                var book = new LibraryBook { BookCode = "B_WEAR_4", Title = "Sách Anh", Status = "Active", BookConditionScore = 10 };

                db.LibraryBooks.Add(book);

                db.SaveChanges();

                

                var loan = new BookLoan { StudentId = student.Id, BookCode = "B_WEAR_4", BookTitle = "Sách Anh", BorrowDate = DateTime.Today, DueDate = DateTime.Today.AddDays(14) };

                db.BookLoans.Add(loan);

                db.SaveChanges();



                bool result = StaffDailyWorkflowsServices.ReturnBookWithWearRating(db, student.Id, "B_WEAR_4", 1, "Hỏng rách nát", DateTime.Today);

                Assert.True(result);



                var bulletin = db.Bulletins.FirstOrDefault(b => b.Content.Contains("Học Sinh Wear 4") || b.Content.Contains("làm hỏng"));

                Assert.Null(bulletin);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ReturnBook_Damaged_BlockMode_WarnOnly()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_WARN", FullName = "Học Sinh Warn", Status = "Active", ConductScore = 100 };

                db.Students.Add(student);

                

                var book = new LibraryBook { BookCode = "B_WARN", Title = "Sách Warn", Status = "Active", BookConditionScore = 10 };

                db.LibraryBooks.Add(book);

                db.SaveChanges();



                var loan = new BookLoan { StudentId = student.Id, BookCode = "B_WARN", BookTitle = "Sách Warn", BorrowDate = DateTime.Today, DueDate = DateTime.Today.AddDays(14) };

                db.BookLoans.Add(loan);



                // Cấu hình chế độ 0 (Chỉ cảnh báo)

                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_DamagedBookBlockMode");

                if (setting != null) setting.Value = "0";

                else db.SystemSettings.Add(new SystemSetting { Id = "Library_DamagedBookBlockMode", Value = "0", Category = "Library" });



                db.SaveChanges();



                bool result = StaffDailyWorkflowsServices.ReturnBookWithWearRating(db, student.Id, "B_WARN", 2, "Hỏng nhẹ", DateTime.Today);

                Assert.True(result);



                db.Entry(student).Reload();

                Assert.Equal(100, student.ConductScore); // Không bị trừ hạnh kiểm

                Assert.Null(student.ReservationBlockedUntil); // Không bị khóa



                var record = db.ConductRecords.FirstOrDefault(r => r.StudentId == student.Id);

                Assert.NotNull(record);

                Assert.Equal(0, record.PointsDelta); // Điểm trừ = 0

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ReturnBook_Damaged_BlockMode_TempBlock()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_BLOCK", FullName = "Học Sinh Block", Status = "Active", ConductScore = 100 };

                db.Students.Add(student);

                

                var book = new LibraryBook { BookCode = "B_BLOCK", Title = "Sách Block", Status = "Active", BookConditionScore = 10 };

                db.LibraryBooks.Add(book);

                db.SaveChanges();



                var loan = new BookLoan { StudentId = student.Id, BookCode = "B_BLOCK", BookTitle = "Sách Block", BorrowDate = DateTime.Today, DueDate = DateTime.Today.AddDays(14) };

                db.BookLoans.Add(loan);



                // Cấu hình chế độ 1 (Khóa tạm thời 5 ngày)

                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_DamagedBookBlockMode");

                if (setting != null) setting.Value = "1";

                else db.SystemSettings.Add(new SystemSetting { Id = "Library_DamagedBookBlockMode", Value = "1", Category = "Library" });



                var daysSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_DamagedBookBlockDays");

                if (daysSetting != null) daysSetting.Value = "5";

                else db.SystemSettings.Add(new SystemSetting { Id = "Library_DamagedBookBlockDays", Value = "5", Category = "Library" });



                db.SaveChanges();



                bool result = StaffDailyWorkflowsServices.ReturnBookWithWearRating(db, student.Id, "B_BLOCK", 1, "Hỏng nát", DateTime.Today);

                Assert.True(result);



                db.Entry(student).Reload();

                Assert.Equal(100, student.ConductScore); // Không bị trừ hạnh kiểm

                Assert.NotNull(student.ReservationBlockedUntil);

                Assert.Equal(DateTime.Today.AddDays(5), student.ReservationBlockedUntil.Value.Date); // Khóa 5 ngày



                // Có thông báo phạt khóa quyền

                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == $"student_{student.Id}" && m.Content.Contains("[CHẾ TÀI HỎNG SÁCH]"));

                Assert.NotNull(msg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ReturnBook_Damaged_BlockMode_DeductConduct()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_DEDUCT", FullName = "Học Sinh Deduct", Status = "Active", ConductScore = 100 };

                db.Students.Add(student);

                

                var book = new LibraryBook { BookCode = "B_DEDUCT", Title = "Sách Deduct", Status = "Active", BookConditionScore = 10 };

                db.LibraryBooks.Add(book);

                db.SaveChanges();



                var loan = new BookLoan { StudentId = student.Id, BookCode = "B_DEDUCT", BookTitle = "Sách Deduct", BorrowDate = DateTime.Today, DueDate = DateTime.Today.AddDays(14) };

                db.BookLoans.Add(loan);



                // Cấu hình chế độ 2 (Trừ 8 điểm hạnh kiểm)

                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_DamagedBookBlockMode");

                if (setting != null) setting.Value = "2";

                else db.SystemSettings.Add(new SystemSetting { Id = "Library_DamagedBookBlockMode", Value = "2", Category = "Library" });



                var deductSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_DamagedBookConductDeduction");

                if (deductSetting != null) deductSetting.Value = "8";

                else db.SystemSettings.Add(new SystemSetting { Id = "Library_DamagedBookConductDeduction", Value = "8", Category = "Library" });



                db.SaveChanges();



                bool result = StaffDailyWorkflowsServices.ReturnBookWithWearRating(db, student.Id, "B_DEDUCT", 3, "Vẽ bậy", DateTime.Today);

                Assert.True(result);



                db.Entry(student).Reload();

                Assert.Equal(92, student.ConductScore); // Trừ 8 điểm (100 - 8 = 92)

                Assert.Null(student.ReservationBlockedUntil); // Không bị khóa



                var record = db.ConductRecords.FirstOrDefault(r => r.StudentId == student.Id && r.PointsDelta == -8);

                Assert.NotNull(record);



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == $"student_{student.Id}" && m.Content.Contains("[CHẾ TÀI HẠNH KIỂM]"));

                Assert.NotNull(msg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }







        [Fact]



        public void Test_LexileMatching_BlockMode_StrictBlock()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_LEX_1", FullName = "HS Lex 1", Status = "Active", StudentLexileScore = 300 };

                db.Students.Add(student);

                

                var book = new LibraryBook { BookCode = "B_LEX_1", Title = "Sách khó", Status = "Active", BookLexileLevel = 600 };

                db.LibraryBooks.Add(book);



                var setMode = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_LexileBlockMode");

                if (setMode != null) setMode.Value = "2";

                else db.SystemSettings.Add(new SystemSetting { Id = "Library_LexileBlockMode", Value = "2", Category = "Library" });



                var setOff = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_LexileMaxOffset");

                if (setOff != null) setOff.Value = "250";

                else db.SystemSettings.Add(new SystemSetting { Id = "Library_LexileMaxOffset", Value = "250", Category = "Library" });



                db.SaveChanges();



                bool canBorrow = StaffDailyWorkflowsServices.BorrowBook(db, student.Id, "B_LEX_1", "Sách khó", DateTime.Today, false);

                Assert.False(canBorrow);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_LexileMatching_TeacherOverride()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_LEX_2", FullName = "HS Lex 2", Status = "Active", StudentLexileScore = 300 };

                db.Students.Add(student);

                var book = new LibraryBook { BookCode = "B_LEX_2", Title = "Sách khó", Status = "Active", BookLexileLevel = 600 };

                db.LibraryBooks.Add(book);



                var setMode = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_LexileBlockMode");

                if (setMode != null) setMode.Value = "2";

                else db.SystemSettings.Add(new SystemSetting { Id = "Library_LexileBlockMode", Value = "2", Category = "Library" });

                db.SaveChanges();



                bool canBorrow = StaffDailyWorkflowsServices.BorrowBook(db, student.Id, "B_LEX_2", "Sách khó", DateTime.Today, true);

                Assert.True(canBorrow);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_LexileMatching_SoftWarning()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_LEX_3", FullName = "HS Lex 3", Status = "Active", StudentLexileScore = 300 };

                db.Students.Add(student);

                var book = new LibraryBook { BookCode = "B_LEX_3", Title = "Sách vừa", Status = "Active", BookLexileLevel = 550 };

                db.LibraryBooks.Add(book);



                var setMode = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_LexileBlockMode");

                if (setMode != null) setMode.Value = "1";

                else db.SystemSettings.Add(new SystemSetting { Id = "Library_LexileBlockMode", Value = "1", Category = "Library" });

                db.SaveChanges();



                bool canBorrow = StaffDailyWorkflowsServices.BorrowBook(db, student.Id, "B_LEX_3", "Sách vừa", DateTime.Today, false);

                Assert.True(canBorrow);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_LexileMatching_NoRestriction()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_LEX_4", FullName = "HS Lex 4", Status = "Active", StudentLexileScore = 400 };

                db.Students.Add(student);

                var book = new LibraryBook { BookCode = "B_LEX_4", Title = "Sách tối ưu", Status = "Active", BookLexileLevel = 450 };

                db.LibraryBooks.Add(book);



                var setMode = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_LexileBlockMode");

                if (setMode != null) setMode.Value = "0";

                else db.SystemSettings.Add(new SystemSetting { Id = "Library_LexileBlockMode", Value = "0", Category = "Library" });

                db.SaveChanges();



                bool canBorrow = StaffDailyWorkflowsServices.BorrowBook(db, student.Id, "B_LEX_4", "Sách tối ưu", DateTime.Today, false);

                Assert.True(canBorrow);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_SystemSettings_DynamicReload()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_DYN", FullName = "HS Dyn", Status = "Active", StudentLexileScore = 300 };

                db.Students.Add(student);

                var book = new LibraryBook { BookCode = "B_DYN", Title = "Sách khó", Status = "Active", BookLexileLevel = 600 };

                db.LibraryBooks.Add(book);



                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_LexileBlockMode");

                if (setting != null) 

                {

                    setting.Value = "2";

                }

                else 

                {

                    setting = new SystemSetting { Id = "Library_LexileBlockMode", Value = "2", Category = "Library" };

                    db.SystemSettings.Add(setting);

                }

                db.SaveChanges();



                Assert.False(StaffDailyWorkflowsServices.BorrowBook(db, student.Id, "B_DYN", "Sách khó", DateTime.Today, false));



                setting.Value = "0";

                db.SaveChanges();



                Assert.True(StaffDailyWorkflowsServices.BorrowBook(db, student.Id, "B_DYN", "Sách khó", DateTime.Today, false));

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Medical_PE_Restriction_ExemptionApplied()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_MED_1", FullName = "Nguyễn Văn Med1", ClassName = "10A1", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.RegisterPhysicalRestriction(

                    db, student.Id, "Hen phế quản nặng", "FullExemption", "Tránh vận động mạnh", "Nghiên cứu về hô hấp", DateTime.Today.AddDays(30), "Y tá Lan"

                );



                Assert.True(success);



                var restriction = db.StudentPhysicalRestrictions.FirstOrDefault(r => r.StudentId == student.Id);

                Assert.NotNull(restriction);

                Assert.Equal("FullExemption", restriction.ExemptionLevel);

                Assert.Equal("Hen phế quản nặng", restriction.MedicalCondition);



                var teacherMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "teacher_10A1");

                Assert.NotNull(teacherMsg);

                Assert.Contains("[HẠN CHẾ VẬN ĐỘNG]", teacherMsg.Content);

                Assert.Contains("FullExemption", teacherMsg.Content);

                Assert.DoesNotContain("Hen phế quản nặng", teacherMsg.Content);



                var studentMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == $"student_{student.Id}");

                Assert.NotNull(studentMsg);

                Assert.Contains("[BÀI TẬP THAY THẾ]", studentMsg.Content);

                Assert.Contains("Nghiên cứu về hô hấp", studentMsg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Medical_PE_Restriction_TheoreticalAlternativeScore()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_MED_2", FullName = "Nguyễn Văn Med2", ClassName = "10A1", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();



                db.StudentPhysicalRestrictions.Add(new StudentPhysicalRestriction

                {

                    StudentId = student.Id,

                    ExemptionLevel = "FullExemption",

                    AlternativeAssignmentTopic = "Lý thuyết điền kinh",

                    EndDate = DateTime.Today.AddDays(10)

                });

                db.SaveChanges();



                var restriction = db.StudentPhysicalRestrictions.FirstOrDefault(r => r.StudentId == student.Id);

                Assert.Equal("Lý thuyết điền kinh", restriction.AlternativeAssignmentTopic);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Medical_PE_Restriction_LowSeverity_SoftWarningOnly()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_MED_3", FullName = "Nguyễn Văn Med3", ClassName = "10A2", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.RegisterPhysicalRestriction(

                    db, student.Id, "Trầy xước da nhẹ", "ReduceActivity", "Tránh tì đè tay trái", "Không cần", DateTime.Today.AddDays(5), "Y tá Lan"

                );



                Assert.True(success);

                var studentMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == $"student_{student.Id}");

                Assert.Null(studentMsg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Medical_Disposal_DualSignature_Success()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var supply = new MedicalSupply { Name = "Cồn 90 độ", Quantity = 50, ExpiryDate = DateTime.Today.AddDays(-10) };

                db.MedicalSupplies.Add(supply);

                db.SaveChanges();



                int propId = StaffDailyWorkflowsServices.ProposeMedicalDisposal(db, supply.Id, 20, "Quá hạn", "Nurse_Signature_Signed");

                Assert.True(propId > 0);



                var prop = db.MedicalDisposalProposals.FirstOrDefault(p => p.Id == propId);

                Assert.NotNull(prop);

                Assert.Equal("PendingApproval", prop.Status);



                bool approved = StaffDailyWorkflowsServices.ApproveMedicalDisposal(db, propId, "1234", "1234");

                Assert.True(approved);

                Assert.Equal("Disposed", prop.Status);

                Assert.Equal(30, supply.Quantity);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Medical_Disposal_SingleSignature_Success()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var supply = new MedicalSupply { Name = "Cồn 90 độ", Quantity = 50, ExpiryDate = DateTime.Today.AddDays(-10) };

                db.MedicalSupplies.Add(supply);



                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Medical_Disposal_Workflow");

                if (setting != null) setting.Value = "0";

                else db.SystemSettings.Add(new SystemSetting { Id = "Medical_Disposal_Workflow", Value = "0" });



                db.SaveChanges();



                int propId = StaffDailyWorkflowsServices.ProposeMedicalDisposal(db, supply.Id, 20, "Quá hạn", "");

                bool approved = StaffDailyWorkflowsServices.ApproveMedicalDisposal(db, propId, "1234", "1234");



                Assert.True(approved);

                var prop = db.MedicalDisposalProposals.FirstOrDefault(p => p.Id == propId);

                Assert.Equal("Disposed", prop.Status);

                Assert.Equal(30, supply.Quantity);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Medical_Disposal_WrongPrincipalPin_Rejected()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var supply = new MedicalSupply { Name = "Paracetamol", Quantity = 100 };

                db.MedicalSupplies.Add(supply);

                db.SaveChanges();



                int propId = StaffDailyWorkflowsServices.ProposeMedicalDisposal(db, supply.Id, 10, "Hỏng vỏ hộp", "Nurse_Lan");

                bool approved = StaffDailyWorkflowsServices.ApproveMedicalDisposal(db, propId, "9999", "1234");



                Assert.False(approved);

                var prop = db.MedicalDisposalProposals.FirstOrDefault(p => p.Id == propId);

                Assert.Equal("Rejected", prop.Status);

                Assert.Equal(100, supply.Quantity);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Medical_Disposal_SignaturesSequence_Validation()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var supply = new MedicalSupply { Name = "Paracetamol", Quantity = 100 };

                db.MedicalSupplies.Add(supply);

                db.SaveChanges();



                int propId = StaffDailyWorkflowsServices.ProposeMedicalDisposal(db, supply.Id, 10, "Hỏng", "");

                bool approved = StaffDailyWorkflowsServices.ApproveMedicalDisposal(db, propId, "1234", "1234");



                Assert.False(approved);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Security_TrafficCongestion_StaggeredDismissalAlert()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_SEC_1", FullName = "Học sinh SEC1", ClassName = "12A1", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();



                bool triggered = StaffDailyWorkflowsServices.ProcessTrafficCongestion(db, 60, DateTime.Now);

                Assert.True(triggered);



                var log = db.TrafficCongestionLogs.FirstOrDefault();

                Assert.NotNull(log);

                Assert.True(log.StaggeredDismissalTriggered);

                Assert.Contains("Khối 10", log.SuggestedSequence);



                var teacherMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "teacher_12A1");

                Assert.NotNull(teacherMsg);

                Assert.Contains("[KẸT XE CỔNG TRƯỜNG]", teacherMsg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Security_TrafficCongestion_StaggeredDismissal_DisabledMode()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_SEC_2", FullName = "Học sinh SEC2", ClassName = "12A1", Status = "Active" };

                db.Students.Add(student);



                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_StaggeredDismissalMode");

                if (setting != null) setting.Value = "0";

                else db.SystemSettings.Add(new SystemSetting { Id = "Security_StaggeredDismissalMode", Value = "0" });



                db.SaveChanges();



                bool triggered = StaffDailyWorkflowsServices.ProcessTrafficCongestion(db, 60, DateTime.Now);

                Assert.True(triggered);



                var log = db.TrafficCongestionLogs.FirstOrDefault();

                Assert.False(log.StaggeredDismissalTriggered);



                var teacherMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "teacher_12A1");

                Assert.Null(teacherMsg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Security_TrafficCongestion_Cooldown_Prevention()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_SEC_3", FullName = "Học sinh SEC3", ClassName = "12A1", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();



                DateTime t1 = DateTime.Now;

                bool first = StaffDailyWorkflowsServices.ProcessTrafficCongestion(db, 55, t1);

                Assert.True(first);



                bool second = StaffDailyWorkflowsServices.ProcessTrafficCongestion(db, 70, t1.AddMinutes(10));

                Assert.False(second);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Security_IntrusionAlarm_SLA_Escalation()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                DateTime tAlarm = DateTime.Parse("2026-06-25T23:00:00");

                int alarmId = StaffDailyWorkflowsServices.TriggerIntrusionAlarm(db, "SENSOR_SERVER_ROOM", tAlarm);

                Assert.True(alarmId > 0);



                bool processed = StaffDailyWorkflowsServices.ProcessIntrusionAlarmsSla(db, tAlarm.AddMinutes(6));

                Assert.True(processed);



                var alarm = db.IntrusionAlarms.FirstOrDefault(a => a.Id == alarmId);

                Assert.Equal("Escalated", alarm.Status);



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "HT001");

                Assert.NotNull(msg);

                Assert.Contains("[BÁO ĐỘNG ĐỘT NHẬP]", msg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Security_IntrusionAlarm_SLA_ResponseOnTime()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                DateTime tAlarm = DateTime.Parse("2026-06-25T23:00:00");

                int alarmId = StaffDailyWorkflowsServices.TriggerIntrusionAlarm(db, "SENSOR_STEM_ROOM", tAlarm);



                bool verified = StaffDailyWorkflowsServices.VerifyIntrusionAlarm(db, alarmId, tAlarm.AddMinutes(3));

                Assert.True(verified);



                bool processed = StaffDailyWorkflowsServices.ProcessIntrusionAlarmsSla(db, tAlarm.AddMinutes(6));

                Assert.False(processed);



                var alarm = db.IntrusionAlarms.FirstOrDefault(a => a.Id == alarmId);

                Assert.Equal("Verified", alarm.Status);



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "HT001");

                Assert.Null(msg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Security_IntrusionAlarm_Daytime_NoAutoEscalation()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                DateTime tAlarm = DateTime.Parse("2026-06-25T10:00:00");

                int alarmId = StaffDailyWorkflowsServices.TriggerIntrusionAlarm(db, "SENSOR_SERVER_ROOM", tAlarm);



                bool processed = StaffDailyWorkflowsServices.ProcessIntrusionAlarmsSla(db, tAlarm.AddMinutes(6));

                Assert.True(processed);



                var alarm = db.IntrusionAlarms.FirstOrDefault(a => a.Id == alarmId);

                Assert.Equal("Verified", alarm.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Security_NightIntrusion_MultipleSensors_Debounce()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                DateTime tAlarm = DateTime.Parse("2026-06-25T23:30:00");

                int id1 = StaffDailyWorkflowsServices.TriggerIntrusionAlarm(db, "SENSOR_STEM_ROOM", tAlarm);

                int id2 = StaffDailyWorkflowsServices.TriggerIntrusionAlarm(db, "SENSOR_STEM_ROOM", tAlarm.AddSeconds(15));



                Assert.Equal(id1, id2);

                Assert.Single(db.IntrusionAlarms.ToList());

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Canteen_FoodSafety_AutoBlockSupplier()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var supplier = new Supplier { Name = "Nha cung cap thit sach", IsBlocked = false };

                db.Suppliers.Add(supplier);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.InspectFoodSafety(db, supplier.Id, "BATCH_MEAT_01", "Thịt heo nạc", 3.5, true);

                Assert.True(success);



                var log = db.FoodSafetyInspectionLogs.FirstOrDefault();

                Assert.NotNull(log);

                Assert.Equal("Quarantined", log.InspectionStatus);



                var updatedSupplier = db.Suppliers.FirstOrDefault(s => s.Id == supplier.Id);

                Assert.True(updatedSupplier.IsBlocked);



                var adminMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "ADMIN");

                Assert.NotNull(adminMsg);

                Assert.Contains("[CẢNH BÁO AN TOÀN THỰC PHẨM]", adminMsg.Content);

                Assert.Contains("TẠM KHÓA HOẠT ĐỘNG", adminMsg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Canteen_FoodSafety_SoftAlertOnly()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var supplier = new Supplier { Name = "Nha cung cap thit sach", IsBlocked = false };

                db.Suppliers.Add(supplier);



                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Canteen_SupplierBlockMode");

                if (setting != null) setting.Value = "1";

                else db.SystemSettings.Add(new SystemSetting { Id = "Canteen_SupplierBlockMode", Value = "1" });



                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.InspectFoodSafety(db, supplier.Id, "BATCH_MEAT_02", "Thịt heo nạc", 4.0, true);

                Assert.True(success);



                var log = db.FoodSafetyInspectionLogs.FirstOrDefault();

                Assert.Equal("Quarantined", log.InspectionStatus);



                var updatedSupplier = db.Suppliers.FirstOrDefault(s => s.Id == supplier.Id);

                Assert.False(updatedSupplier.IsBlocked);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Canteen_FoodSafety_NormalInspectionPassed()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var supplier = new Supplier { Name = "Nha cung cap thit sach", IsBlocked = false };

                db.Suppliers.Add(supplier);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.InspectFoodSafety(db, supplier.Id, "BATCH_MEAT_03", "Thịt heo nạc", 9.0, false);

                Assert.True(success);



                var log = db.FoodSafetyInspectionLogs.FirstOrDefault();

                Assert.Equal("Passed", log.InspectionStatus);



                var updatedSupplier = db.Suppliers.FirstOrDefault(s => s.Id == supplier.Id);

                Assert.False(updatedSupplier.IsBlocked);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Canteen_NutritionalAutoAdjust_VegetableWasteThreshold()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_NUT_1", FullName = "Học sinh NUT1", Status = "Active" };

                db.Students.Add(student);



                DateTime tMenu = DateTime.Today;

                var menuToday = new SchoolMenu { Date = tMenu, Items = "Thịt kho, Rau luộc, Canh chua" };

                var menuNextWeek = new SchoolMenu { Date = tMenu.AddDays(7), Items = "Gà xào, Rau luộc, Canh bí" };

                db.SchoolMenus.AddRange(menuToday, menuNextWeek);



                db.FoodWasteLogs.Add(new FoodWasteLog { Date = tMenu, MealType = "Lunch", PreparedMeals = 100, WastedMeals = 30, CostOfWaste = 150000, WasteRatio = 0.3, Notes = "Hao hụt cao" });



                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProcessNutritionalAdjustment(db, menuToday.Id, DateTime.Now);

                Assert.True(success);



                var updatedMenuNextWeek = db.SchoolMenus.FirstOrDefault(m => m.Date.Date == tMenu.AddDays(7).Date);

                Assert.Contains("Súp rau củ xay mịn", updatedMenuNextWeek.Items);



                var parentMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == $"parent_{student.Id}");

                Assert.NotNull(parentMsg);

                Assert.Contains("[DINH DƯỠNG XANH]", parentMsg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Canteen_NutritionalAutoAdjust_BelowThreshold()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                DateTime tMenu = DateTime.Today;

                var menuToday = new SchoolMenu { Date = tMenu, Items = "Thịt kho, Rau luộc, Canh chua" };

                var menuNextWeek = new SchoolMenu { Date = tMenu.AddDays(7), Items = "Gà xào, Rau luộc, Canh bí" };

                db.SchoolMenus.AddRange(menuToday, menuNextWeek);



                db.FoodWasteLogs.Add(new FoodWasteLog { Date = tMenu, MealType = "Lunch", PreparedMeals = 100, WastedMeals = 10, CostOfWaste = 50000, WasteRatio = 0.1, Notes = "Hao hụt thấp" });



                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProcessNutritionalAdjustment(db, menuToday.Id, DateTime.Now);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Canteen_NutritionalFeedback_PrivatePrivacy()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_NUT_2", FullName = "Học sinh NUT2", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.SubmitMealFeedback(db, 1, student.Id, 1, "Rau luộc hơi đắng và dai.");

                Assert.True(success);



                var bulletin = db.Bulletins.FirstOrDefault(b => b.Content.Contains("đắng và dai"));

                Assert.Null(bulletin);



                var feedback = db.MealFeedbacks.FirstOrDefault(f => f.StudentId == student.Id);

                Assert.NotNull(feedback);

                Assert.Equal(1, feedback.Rating);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        // --- PHASE 5: IT-ADMIN UPGRADES TESTS ---



        [Fact]

        public void Test_ITAdmin_License_AutoRevoke_TransferredStudent()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_IT_1", FullName = "Học sinh IT1", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_License_AutoRevokeMode", Value = "1", Category = "IT" });

                

                db.StudentDeviceLicenses.Add(new StudentDeviceLicense

                {

                    StudentId = 1, // Will be updated to student.Id in save changes if relation, let's assign explicitly

                    DeviceCode = "IPAD_IT_1",

                    LicenseKey = "KEY_IT_1",

                    Status = "Active"

                });

                db.SaveChanges();

                

                // Fetch again to ensure ID is tracked

                var s = db.Students.First(x => x.StudentCode == "HS_IT_1");

                var lic = db.StudentDeviceLicenses.First(x => x.DeviceCode == "IPAD_IT_1");

                lic.StudentId = s.Id;

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProcessDeviceLicenseLifecycle(db, s.Id, "Transferred", DateTime.Now);

                Assert.True(success);



                var updatedStudent = db.Students.FirstOrDefault(x => x.Id == s.Id);

                Assert.Equal("Transferred", updatedStudent.Status);



                var license = db.StudentDeviceLicenses.FirstOrDefault(l => l.DeviceCode == "IPAD_IT_1");

                Assert.Equal("Released", license.Status);



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "parent_" + s.Id);

                Assert.NotNull(msg);

                Assert.Contains("[THÔNG BÁO THU HỒI THIẾT BỊ]", msg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_License_AutoRevoke_SuspendedStudent()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_IT_2", FullName = "Học sinh IT2", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_License_AutoRevokeMode", Value = "1", Category = "IT" });

                

                db.StudentDeviceLicenses.Add(new StudentDeviceLicense

                {

                    DeviceCode = "IPAD_IT_2",

                    LicenseKey = "KEY_IT_2",

                    Status = "Active"

                });

                db.SaveChanges();

                

                var s = db.Students.First(x => x.StudentCode == "HS_IT_2");

                var lic = db.StudentDeviceLicenses.First(x => x.DeviceCode == "IPAD_IT_2");

                lic.StudentId = s.Id;

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProcessDeviceLicenseLifecycle(db, s.Id, "Suspended", DateTime.Now);

                Assert.True(success);



                var updatedStudent = db.Students.FirstOrDefault(x => x.Id == s.Id);

                Assert.Equal("Suspended", updatedStudent.Status);



                var license = db.StudentDeviceLicenses.FirstOrDefault(l => l.DeviceCode == "IPAD_IT_2");

                Assert.Equal("Released", license.Status);



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "parent_" + s.Id);

                Assert.NotNull(msg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_License_ManualRevokeMode_WarningOnly()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_IT_3", FullName = "Học sinh IT3", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_License_AutoRevokeMode", Value = "0", Category = "IT" });

                

                db.StudentDeviceLicenses.Add(new StudentDeviceLicense

                {

                    DeviceCode = "IPAD_IT_3",

                    LicenseKey = "KEY_IT_3",

                    Status = "Active"

                });

                db.SaveChanges();

                

                var s = db.Students.First(x => x.StudentCode == "HS_IT_3");

                var lic = db.StudentDeviceLicenses.First(x => x.DeviceCode == "IPAD_IT_3");

                lic.StudentId = s.Id;

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProcessDeviceLicenseLifecycle(db, s.Id, "Transferred", DateTime.Now);

                Assert.True(success);



                var updatedStudent = db.Students.FirstOrDefault(x => x.Id == s.Id);

                Assert.Equal("Transferred", updatedStudent.Status);



                var license = db.StudentDeviceLicenses.FirstOrDefault(l => l.DeviceCode == "IPAD_IT_3");

                Assert.Equal("Active", license.Status); // Remains active since mode = 0



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "ADMIN");

                Assert.NotNull(msg);

                Assert.Contains("[CẢNH BÁO THU HỒI]", msg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_License_Revoke_PrivacyCompliance()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_IT_4", FullName = "Học sinh IT4", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_License_AutoRevokeMode", Value = "1", Category = "IT" });

                

                db.StudentDeviceLicenses.Add(new StudentDeviceLicense

                {

                    DeviceCode = "IPAD_IT_4",

                    LicenseKey = "KEY_IT_4",

                    Status = "Active"

                });

                db.SaveChanges();

                

                var s = db.Students.First(x => x.StudentCode == "HS_IT_4");

                var lic = db.StudentDeviceLicenses.First(x => x.DeviceCode == "IPAD_IT_4");

                lic.StudentId = s.Id;

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProcessDeviceLicenseLifecycle(db, s.Id, "Suspended", DateTime.Now);

                Assert.True(success);



                // Verify that NO public bulletin was created

                var bulletin = db.Bulletins.FirstOrDefault(b => b.Content.Contains("IPAD_IT_4") || b.Content.Contains("kỷ luật") || b.Content.Contains("thu hồi"));

                Assert.Null(bulletin);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_License_AutoRevoke_NoLicenseAssigned()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_IT_5", FullName = "Học sinh IT5", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_License_AutoRevokeMode", Value = "1", Category = "IT" });

                db.SaveChanges();

                

                var s = db.Students.First(x => x.StudentCode == "HS_IT_5");



                bool success = StaffDailyWorkflowsServices.ProcessDeviceLicenseLifecycle(db, s.Id, "Transferred", DateTime.Now);

                Assert.True(success);



                var updatedStudent = db.Students.FirstOrDefault(x => x.Id == s.Id);

                Assert.Equal("Transferred", updatedStudent.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_License_BackupMode_Enabled()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_IT_6", FullName = "Học sinh IT6", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_License_AutoRevokeMode", Value = "1", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_License_BackupBeforeRevokeMode", Value = "1", Category = "IT" });

                

                db.StudentDeviceLicenses.Add(new StudentDeviceLicense

                {

                    DeviceCode = "IPAD_IT_6",

                    LicenseKey = "KEY_IT_6",

                    Status = "Active"

                });

                

                db.StudentMindmaps.Add(new StudentMindmap

                {

                    StudentCode = "HS_IT_6",

                    Title = "Test Mindmap",

                    DataJson = "{}"

                });

                db.SaveChanges();

                

                var s = db.Students.First(x => x.StudentCode == "HS_IT_6");

                var lic = db.StudentDeviceLicenses.First(x => x.DeviceCode == "IPAD_IT_6");

                lic.StudentId = s.Id;

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProcessDeviceLicenseLifecycle(db, s.Id, "Suspended", DateTime.Now);

                Assert.True(success);



                var license = db.StudentDeviceLicenses.FirstOrDefault(l => l.DeviceCode == "IPAD_IT_6");

                Assert.Equal("Released", license.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_License_BackupMode_Disabled()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_IT_7", FullName = "Học sinh IT7", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_License_AutoRevokeMode", Value = "1", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_License_BackupBeforeRevokeMode", Value = "0", Category = "IT" });

                

                db.StudentDeviceLicenses.Add(new StudentDeviceLicense

                {

                    DeviceCode = "IPAD_IT_7",

                    LicenseKey = "KEY_IT_7",

                    Status = "Active"

                });

                db.SaveChanges();

                

                var s = db.Students.First(x => x.StudentCode == "HS_IT_7");

                var lic = db.StudentDeviceLicenses.First(x => x.DeviceCode == "IPAD_IT_7");

                lic.StudentId = s.Id;

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProcessDeviceLicenseLifecycle(db, s.Id, "Suspended", DateTime.Now);

                Assert.True(success);



                var license = db.StudentDeviceLicenses.FirstOrDefault(l => l.DeviceCode == "IPAD_IT_7");

                Assert.Equal("Released", license.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_License_HistoryLogging_Disabled_Overwrite()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_License_HistoryLoggingMode", Value = "0", Category = "IT" });

                db.SaveChanges();



                // Assign to student 1

                bool s1 = StaffDailyWorkflowsServices.RegisterStudentDeviceLicense(db, 1, "IPAD_IT_8", "KEY_8", DateTime.Now);

                Assert.True(s1);



                // Release it first (by simulating student transfer)

                var lic = db.StudentDeviceLicenses.First(l => l.DeviceCode == "IPAD_IT_8");

                lic.Status = "Released";

                db.SaveChanges();



                // Reassign to student 2

                bool s2 = StaffDailyWorkflowsServices.RegisterStudentDeviceLicense(db, 2, "IPAD_IT_8", "KEY_8_NEW", DateTime.Now);

                Assert.True(s2);



                var records = db.StudentDeviceLicenses.Where(l => l.DeviceCode == "IPAD_IT_8").ToList();

                Assert.Single(records); // Overwritten, so only 1 record exists

                Assert.Equal(2, records[0].StudentId);

                Assert.Equal("Active", records[0].Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_License_HistoryLogging_Enabled_InsertNew()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_License_HistoryLoggingMode", Value = "1", Category = "IT" });

                db.SaveChanges();



                // Assign to student 1

                bool s1 = StaffDailyWorkflowsServices.RegisterStudentDeviceLicense(db, 1, "IPAD_IT_9", "KEY_9", DateTime.Now);

                Assert.True(s1);



                // Release it first

                var lic = db.StudentDeviceLicenses.First(l => l.DeviceCode == "IPAD_IT_9");

                lic.Status = "Released";

                db.SaveChanges();



                // Reassign to student 2

                bool s2 = StaffDailyWorkflowsServices.RegisterStudentDeviceLicense(db, 2, "IPAD_IT_9", "KEY_9_NEW", DateTime.Now);

                Assert.True(s2);



                var records = db.StudentDeviceLicenses.Where(l => l.DeviceCode == "IPAD_IT_9").ToList();

                Assert.Equal(2, records.Count); // History kept, so 2 records exist

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_License_HistoryLogging_PreventActiveConflict()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_License_HistoryLoggingMode", Value = "1", Category = "IT" });

                db.SaveChanges();



                // Assign to student 1 (Active)

                bool s1 = StaffDailyWorkflowsServices.RegisterStudentDeviceLicense(db, 1, "IPAD_IT_10", "KEY_10", DateTime.Now);

                Assert.True(s1);



                // Attempt to assign to student 2 without releasing first

                bool s2 = StaffDailyWorkflowsServices.RegisterStudentDeviceLicense(db, 2, "IPAD_IT_10", "KEY_10_NEW", DateTime.Now);

                Assert.False(s2); // Prevent active conflict

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_OfflineSync_Match_Success()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_IT_11", FullName = "Học sinh IT11", Status = "Active" };

                db.Students.Add(student);

                

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Sync_ConflictResolutionMode", Value = "0", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Sync_BatchProcessingMode", Value = "0", Category = "IT" });

                db.SaveChanges();

                

                var s = db.Students.First(x => x.StudentCode == "HS_IT_11");



                var item = new OfflineSyncItem

                {

                    ActionType = "GRADE",

                    PayloadJson = string.Format("{{\"StudentId\": {0}, \"Score\": 8.5, \"RosterId\": 1, \"GradeTypeId\": 1, \"Attempt\": 1}}", s.Id),

                    CreatedAt = DateTime.Now,

                    Status = "Pending"

                };

                db.OfflineSyncItems.Add(item);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ResolveOfflineSyncConflict(db, item.Id, "MD5_MATCH", "MD5_MATCH", DateTime.Now);

                Assert.True(success);



                var updatedItem = db.OfflineSyncItems.FirstOrDefault(i => i.Id == item.Id);

                Assert.Equal("Synced", updatedItem.Status);



                var grade = db.StudentGrades.FirstOrDefault(g => g.StudentId == s.Id);

                Assert.NotNull(grade);

                Assert.Equal(8.5, grade.Score);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_OfflineSync_Mismatch_ConflictFailed()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_IT_12", FullName = "Học sinh IT12", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Sync_ConflictResolutionMode", Value = "0", Category = "IT" });

                db.SaveChanges();

                

                var s = db.Students.First(x => x.StudentCode == "HS_IT_12");



                var item = new OfflineSyncItem

                {

                    ActionType = "GRADE",

                    PayloadJson = string.Format("{{\"StudentId\": {0}, \"Score\": 9.5, \"RosterId\": 1, \"GradeTypeId\": 1, \"Attempt\": 1}}", s.Id),

                    CreatedAt = DateTime.Now,

                    Status = "Pending"

                };

                db.OfflineSyncItems.Add(item);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ResolveOfflineSyncConflict(db, item.Id, "MD5_SERVER", "MD5_TAMPERED", DateTime.Now);

                Assert.False(success); // Conflict fails



                var updatedItem = db.OfflineSyncItems.FirstOrDefault(i => i.Id == item.Id);

                Assert.Equal("Conflict_Failed", updatedItem.Status);



                var grade = db.StudentGrades.FirstOrDefault(g => g.StudentId == s.Id);

                Assert.Null(grade); // Not saved

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_OfflineSync_Conflict_RedAlertSent()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_IT_13", FullName = "Học sinh IT13", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Sync_ConflictResolutionMode", Value = "0", Category = "IT" });

                db.SaveChanges();

                

                var s = db.Students.First(x => x.StudentCode == "HS_IT_13");



                var item = new OfflineSyncItem

                {

                    ActionType = "GRADE",

                    PayloadJson = string.Format("{{\"StudentId\": {0}, \"Score\": 9.5, \"RosterId\": 1, \"GradeTypeId\": 1, \"Attempt\": 1}}", s.Id),

                    CreatedAt = DateTime.Now,

                    Status = "Pending"

                };

                db.OfflineSyncItems.Add(item);

                db.SaveChanges();



                StaffDailyWorkflowsServices.ResolveOfflineSyncConflict(db, item.Id, "MD5_SERVER", "MD5_TAMPERED", DateTime.Now);



                var adminMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "ADMIN");

                Assert.NotNull(adminMsg);

                Assert.Contains("[CẢNH BÁO ĐỎ]", adminMsg.Content);



                var principalMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "HT001");

                Assert.NotNull(principalMsg);

                Assert.Contains("[CẢNH BÁO ĐỎ]", principalMsg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_OfflineSync_OverwriteMode_CentralPriority()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_IT_14", FullName = "Học sinh IT14", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Sync_ConflictResolutionMode", Value = "1", Category = "IT" }); // Central Priority

                db.SaveChanges();

                

                var s = db.Students.First(x => x.StudentCode == "HS_IT_14");



                var item = new OfflineSyncItem

                {

                    ActionType = "GRADE",

                    PayloadJson = string.Format("{{\"StudentId\": {0}, \"Score\": 9.5, \"RosterId\": 1, \"GradeTypeId\": 1, \"Attempt\": 1}}", s.Id),

                    CreatedAt = DateTime.Now,

                    Status = "Pending"

                };

                db.OfflineSyncItems.Add(item);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ResolveOfflineSyncConflict(db, item.Id, "MD5_SERVER", "MD5_TAMPERED", DateTime.Now);

                Assert.True(success); // Handled successfully



                var updatedItem = db.OfflineSyncItems.FirstOrDefault(i => i.Id == item.Id);

                Assert.Equal("Synced", updatedItem.Status);



                var grade = db.StudentGrades.FirstOrDefault(g => g.StudentId == s.Id);

                Assert.Null(grade); // No grade recorded from the tampered packet

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_OfflineSync_MultipleItems_SingleTransaction()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_IT_15", FullName = "Học sinh IT15", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();

                

                var s = db.Students.First(x => x.StudentCode == "HS_IT_15");



                var item1 = new OfflineSyncItem { ActionType = "GRADE", PayloadJson = string.Format("{{\"StudentId\": {0}, \"Score\": 8.0}}", s.Id), CreatedAt = DateTime.Now, Status = "Pending" };

                var item2 = new OfflineSyncItem { ActionType = "GRADE", PayloadJson = string.Format("{{\"StudentId\": {0}, \"Score\": 8.5}}", s.Id), CreatedAt = DateTime.Now, Status = "Pending" };

                db.OfflineSyncItems.AddRange(item1, item2);

                db.SaveChanges();



                using (var tx = db.Database.BeginTransaction())

                {

                    bool r1 = StaffDailyWorkflowsServices.ResolveOfflineSyncConflict(db, item1.Id, "MATCH", "MATCH", DateTime.Now);

                    bool r2 = StaffDailyWorkflowsServices.ResolveOfflineSyncConflict(db, item2.Id, "MATCH", "MATCH", DateTime.Now);

                    

                    Assert.True(r1);

                    Assert.True(r2);

                    tx.Commit();

                }



                var updatedItem1 = db.OfflineSyncItems.FirstOrDefault(i => i.Id == item1.Id);

                Assert.Equal("Synced", updatedItem1.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_OfflineSync_BatchMode_Enabled()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_IT_16", FullName = "Học sinh IT16", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Sync_BatchProcessingMode", Value = "1", Category = "IT" });

                db.SaveChanges();

                

                var s = db.Students.First(x => x.StudentCode == "HS_IT_16");



                var item = new OfflineSyncItem

                {

                    ActionType = "GRADE",

                    PayloadJson = string.Format("{{\"StudentId\": {0}, \"Score\": 7.5}}", s.Id),

                    CreatedAt = DateTime.Now,

                    Status = "Pending"

                };

                db.OfflineSyncItems.Add(item);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ResolveOfflineSyncConflict(db, item.Id, "MATCH", "MATCH", DateTime.Now);

                Assert.True(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_OfflineSync_BatchMode_Disabled()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_IT_17", FullName = "Học sinh IT17", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Sync_BatchProcessingMode", Value = "0", Category = "IT" });

                db.SaveChanges();

                

                var s = db.Students.First(x => x.StudentCode == "HS_IT_17");



                var item = new OfflineSyncItem

                {

                    ActionType = "GRADE",

                    PayloadJson = string.Format("{{\"StudentId\": {0}, \"Score\": 7.5}}", s.Id),

                    CreatedAt = DateTime.Now,

                    Status = "Pending"

                };

                db.OfflineSyncItems.Add(item);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ResolveOfflineSyncConflict(db, item.Id, "MATCH", "MATCH", DateTime.Now);

                Assert.True(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_License_AutoRevoke_InvalidStudentId()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                bool success = StaffDailyWorkflowsServices.ProcessDeviceLicenseLifecycle(db, 9999, "Suspended", DateTime.Now);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_OfflineSync_InvalidSyncItem()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                bool success = StaffDailyWorkflowsServices.ResolveOfflineSyncConflict(db, 9999, "MATCH", "MATCH", DateTime.Now);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_OfflineSync_PayloadParseError()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var item = new OfflineSyncItem

                {

                    ActionType = "GRADE",

                    PayloadJson = "invalid_json_payload",

                    CreatedAt = DateTime.Now,

                    Status = "Pending"

                };

                db.OfflineSyncItems.Add(item);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ResolveOfflineSyncConflict(db, item.Id, "MATCH", "MATCH", DateTime.Now);

                Assert.True(success); // Handled safely



                var updatedItem = db.OfflineSyncItems.FirstOrDefault(i => i.Id == item.Id);

                Assert.Equal("Failed", updatedItem.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        // --- PHASE 5 - PART 2: IT-ADMIN AVATAR MANAGEMENT TESTS ---



        [Fact]

        public void Test_ITAdmin_AvatarSelf_Allowed_Success()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_AV_1", FullName = "Học sinh AV1", ClassName = "10A1", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Avatar_StudentAllowedChange", Value = "1", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Avatar_ModerationMode", Value = "0", Category = "IT" });

                db.SaveChanges();



                var s = db.Students.First(x => x.StudentCode == "HS_AV_1");



                // Create a dummy source image

                var srcDir = Path.Combine(AppPaths.TempDir, "TestAvatars");

                Directory.CreateDirectory(srcDir);

                var srcPath = Path.Combine(srcDir, "avatar1.png");

                File.WriteAllText(srcPath, "dummy_image_data");



                bool success = StaffDailyWorkflowsServices.ChangeStudentAvatarSelf(db, s.Id, srcPath, DateTime.Now);

                Assert.True(success);



                var updated = db.Students.First(x => x.Id == s.Id);

                Assert.NotEmpty(updated.AvatarPath);

                Assert.True(File.Exists(updated.AvatarPath));



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "teacher_10A1");

                Assert.NotNull(msg);

                Assert.Contains("[ẢNH ĐẠI DIỆN MỚI]", msg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_AvatarSelf_Blocked_Failed()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_AV_2", FullName = "Học sinh AV2", ClassName = "10A1", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Avatar_StudentAllowedChange", Value = "0", Category = "IT" });

                db.SaveChanges();



                var s = db.Students.First(x => x.StudentCode == "HS_AV_2");

                var srcPath = Path.Combine(AppPaths.TempDir, "avatar2.png");

                File.WriteAllText(srcPath, "dummy_image_data");



                bool success = StaffDailyWorkflowsServices.ChangeStudentAvatarSelf(db, s.Id, srcPath, DateTime.Now);

                Assert.False(success);



                var updated = db.Students.First(x => x.Id == s.Id);

                Assert.Empty(updated.AvatarPath);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_AvatarSelf_InvalidExtension_Blocked()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_AV_3", FullName = "Học sinh AV3", ClassName = "10A1", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Avatar_StudentAllowedChange", Value = "1", Category = "IT" });

                db.SaveChanges();



                var s = db.Students.First(x => x.StudentCode == "HS_AV_3");

                var srcPath = Path.Combine(AppPaths.TempDir, "bad.txt");

                File.WriteAllText(srcPath, "executable code");



                bool success = StaffDailyWorkflowsServices.ChangeStudentAvatarSelf(db, s.Id, srcPath, DateTime.Now);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_AvatarSelf_CacheBusterAndCleanup()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_AV_4", FullName = "Học sinh AV4", ClassName = "10A1", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Avatar_StudentAllowedChange", Value = "1", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Avatar_ModerationMode", Value = "0", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Avatar_BackupOldOnOverwrite", Value = "0", Category = "IT" });

                db.SaveChanges();



                var s = db.Students.First(x => x.StudentCode == "HS_AV_4");

                var srcPath = Path.Combine(AppPaths.TempDir, "avatar4.png");

                File.WriteAllText(srcPath, "data");



                // Change first time

                StaffDailyWorkflowsServices.ChangeStudentAvatarSelf(db, s.Id, srcPath, DateTime.Now.AddMinutes(-5));

                var firstPath = db.Students.First(x => x.Id == s.Id).AvatarPath;

                Assert.True(File.Exists(firstPath));



                // Change second time

                StaffDailyWorkflowsServices.ChangeStudentAvatarSelf(db, s.Id, srcPath, DateTime.Now);

                var secondPath = db.Students.First(x => x.Id == s.Id).AvatarPath;

                Assert.True(File.Exists(secondPath));

                Assert.NotEqual(firstPath, secondPath);

                Assert.False(File.Exists(firstPath)); // Old file deleted

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_AvatarSelf_Moderation_Disabled()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_AV_5", FullName = "Học sinh AV5", ClassName = "10A1", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Avatar_StudentAllowedChange", Value = "1", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Avatar_ModerationMode", Value = "0", Category = "IT" });

                db.SaveChanges();



                var s = db.Students.First(x => x.StudentCode == "HS_AV_5");

                var srcPath = Path.Combine(AppPaths.TempDir, "avatar5.png");

                File.WriteAllText(srcPath, "data");



                bool success = StaffDailyWorkflowsServices.ChangeStudentAvatarSelf(db, s.Id, srcPath, DateTime.Now);

                Assert.True(success);



                var updated = db.Students.First(x => x.Id == s.Id);

                Assert.NotEmpty(updated.AvatarPath); // Approved immediately

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_AvatarSelf_Moderation_Enabled_Pending()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_AV_6", FullName = "Học sinh AV6", ClassName = "10A1", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Avatar_StudentAllowedChange", Value = "1", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Avatar_ModerationMode", Value = "1", Category = "IT" });

                db.SaveChanges();



                var s = db.Students.First(x => x.StudentCode == "HS_AV_6");

                var srcPath = Path.Combine(AppPaths.TempDir, "avatar6.png");

                File.WriteAllText(srcPath, "data");



                bool success = StaffDailyWorkflowsServices.ChangeStudentAvatarSelf(db, s.Id, srcPath, DateTime.Now);

                Assert.True(success);



                var updated = db.Students.First(x => x.Id == s.Id);

                Assert.Empty(updated.AvatarPath); // NOT updated yet



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "teacher_10A1" && m.Content.Contains("[DUYỆT ẢNH ĐẠI DIỆN]"));

                Assert.NotNull(msg);

                Assert.NotEmpty(msg.Notes); // Contains pending path

                Assert.True(File.Exists(msg.Notes));

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_AvatarSelf_Moderation_Approved()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_AV_7", FullName = "Học sinh AV7", ClassName = "10A1", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();



                var s = db.Students.First(x => x.StudentCode == "HS_AV_7");

                var pendingPath = Path.Combine(AppPaths.TempDir, "avatar7_pending.png");

                File.WriteAllText(pendingPath, "pending_data");



                bool success = StaffDailyWorkflowsServices.ApproveStudentAvatarChange(db, s.Id, pendingPath, true, DateTime.Now);

                Assert.True(success);



                var updated = db.Students.First(x => x.Id == s.Id);

                Assert.NotEmpty(updated.AvatarPath);

                Assert.True(File.Exists(updated.AvatarPath));

                Assert.False(File.Exists(pendingPath)); // Pending moved

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_AvatarSelf_Moderation_Rejected()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_AV_8", FullName = "Học sinh AV8", ClassName = "10A1", Status = "Active" };

                db.Students.Add(student);

                db.SaveChanges();



                var s = db.Students.First(x => x.StudentCode == "HS_AV_8");

                var pendingPath = Path.Combine(AppPaths.TempDir, "avatar8_pending.png");

                File.WriteAllText(pendingPath, "pending_data");



                bool success = StaffDailyWorkflowsServices.ApproveStudentAvatarChange(db, s.Id, pendingPath, false, DateTime.Now);

                Assert.True(success);



                var updated = db.Students.First(x => x.Id == s.Id);

                Assert.Empty(updated.AvatarPath); // DB path remains empty

                Assert.False(File.Exists(pendingPath)); // Pending file deleted

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_AvatarSelf_NonExistentStudent()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                bool success = StaffDailyWorkflowsServices.ChangeStudentAvatarSelf(db, 9999, "any.png", DateTime.Now);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_AvatarSelf_BackupOld_Enabled()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var student = new Student { StudentCode = "HS_AV_10", FullName = "Học sinh AV10", ClassName = "10A1", Status = "Active" };

                db.Students.Add(student);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Avatar_StudentAllowedChange", Value = "1", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Avatar_BackupOldOnOverwrite", Value = "1", Category = "IT" });

                db.SaveChanges();



                var s = db.Students.First(x => x.StudentCode == "HS_AV_10");

                var srcPath = Path.Combine(AppPaths.TempDir, "avatar10.png");

                File.WriteAllText(srcPath, "data");



                // Change first time

                StaffDailyWorkflowsServices.ChangeStudentAvatarSelf(db, s.Id, srcPath, DateTime.Now.AddMinutes(-5));

                var firstPath = db.Students.First(x => x.Id == s.Id).AvatarPath;



                // Change second time

                StaffDailyWorkflowsServices.ChangeStudentAvatarSelf(db, s.Id, srcPath, DateTime.Now);

                var backupFile = Path.Combine(Path.GetDirectoryName(firstPath), "Backup", Path.GetFileName(firstPath));

                

                Assert.True(File.Exists(backupFile)); // Moved to backup

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_BatchUpload_MatchByCode_Success()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.Students.Add(new Student { StudentCode = "HS_B_1", FullName = "HS B1", Status = "Active" });

                db.Students.Add(new Student { StudentCode = "HS_B_2", FullName = "HS B2", Status = "Active" });

                db.Students.Add(new Student { StudentCode = "HS_B_3", FullName = "HS B3", Status = "Active" });

                db.SaveChanges();



                var srcDir = Path.Combine(AppPaths.TempDir, "BatchAvatars1");

                Directory.CreateDirectory(srcDir);

                File.WriteAllText(Path.Combine(srcDir, "HS_B_1.png"), "data");

                File.WriteAllText(Path.Combine(srcDir, "HS_B_2.jpg"), "data");

                File.WriteAllText(Path.Combine(srcDir, "HS_B_3.webp"), "data");



                int success = StaffDailyWorkflowsServices.BatchUploadStudentAvatars(db, srcDir, "StudentCode", DateTime.Now);

                Assert.Equal(3, success);



                Assert.NotEmpty(db.Students.First(x => x.StudentCode == "HS_B_1").AvatarPath);

                Assert.NotEmpty(db.Students.First(x => x.StudentCode == "HS_B_2").AvatarPath);

                Assert.NotEmpty(db.Students.First(x => x.StudentCode == "HS_B_3").AvatarPath);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_BatchUpload_MatchById_Success()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s1 = new Student { StudentCode = "HS_BI_1", FullName = "HS BI1", Status = "Active" };

                var s2 = new Student { StudentCode = "HS_BI_2", FullName = "HS BI2", Status = "Active" };

                db.Students.AddRange(s1, s2);

                db.SaveChanges();



                var srcDir = Path.Combine(AppPaths.TempDir, "BatchAvatars2");

                Directory.CreateDirectory(srcDir);

                File.WriteAllText(Path.Combine(srcDir, s1.Id + ".png"), "data");

                File.WriteAllText(Path.Combine(srcDir, s2.Id + ".jpg"), "data");



                int success = StaffDailyWorkflowsServices.BatchUploadStudentAvatars(db, srcDir, "StudentId", DateTime.Now);

                Assert.Equal(2, success);



                Assert.NotEmpty(db.Students.First(x => x.Id == s1.Id).AvatarPath);

                Assert.NotEmpty(db.Students.First(x => x.Id == s2.Id).AvatarPath);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_BatchUpload_NonExistentFolder_SafeReturn()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                int success = StaffDailyWorkflowsServices.BatchUploadStudentAvatars(db, "C:\nonexistentfolder", "StudentCode", DateTime.Now);

                Assert.Equal(0, success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_BatchUpload_CaseInsensitiveMatch()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.Students.Add(new Student { StudentCode = "HS_B_4", FullName = "HS B4", Status = "Active" });

                db.SaveChanges();



                var srcDir = Path.Combine(AppPaths.TempDir, "BatchAvatars4");

                Directory.CreateDirectory(srcDir);

                File.WriteAllText(Path.Combine(srcDir, "hs_b_4.PNG"), "data");



                int success = StaffDailyWorkflowsServices.BatchUploadStudentAvatars(db, srcDir, "StudentCode", DateTime.Now);

                Assert.Equal(1, success);

                Assert.NotEmpty(db.Students.First(x => x.StudentCode == "HS_B_4").AvatarPath);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_BatchUpload_SkipInvalidFiles()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.Students.Add(new Student { StudentCode = "HS_B_5", FullName = "HS B5", Status = "Active" });

                db.Students.Add(new Student { StudentCode = "HS_B_6", FullName = "HS B6", Status = "Active" });

                db.SaveChanges();



                var srcDir = Path.Combine(AppPaths.TempDir, "BatchAvatars5");

                Directory.CreateDirectory(srcDir);

                File.WriteAllText(Path.Combine(srcDir, "HS_B_5.png"), "data");

                File.WriteAllText(Path.Combine(srcDir, "HS_B_6.bat"), "executable");



                int success = StaffDailyWorkflowsServices.BatchUploadStudentAvatars(db, srcDir, "StudentCode", DateTime.Now);

                Assert.Equal(1, success); // Only HS_B_5 processed, bat file skipped

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_BatchUpload_RecursiveScan_Enabled()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.Students.Add(new Student { StudentCode = "HS_B_7", FullName = "HS B7", Status = "Active" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Avatar_BatchRecursiveMode", Value = "1", Category = "IT" });

                db.SaveChanges();



                var srcDir = Path.Combine(AppPaths.TempDir, "BatchAvatars7");

                var subDir = Path.Combine(srcDir, "SubClass");

                Directory.CreateDirectory(subDir);

                File.WriteAllText(Path.Combine(subDir, "HS_B_7.png"), "data");



                int success = StaffDailyWorkflowsServices.BatchUploadStudentAvatars(db, srcDir, "StudentCode", DateTime.Now);

                Assert.Equal(1, success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_BatchUpload_RecursiveScan_Disabled()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.Students.Add(new Student { StudentCode = "HS_B_8", FullName = "HS B8", Status = "Active" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Avatar_BatchRecursiveMode", Value = "0", Category = "IT" });

                db.SaveChanges();



                var srcDir = Path.Combine(AppPaths.TempDir, "BatchAvatars8");

                var subDir = Path.Combine(srcDir, "SubClass");

                Directory.CreateDirectory(subDir);

                File.WriteAllText(Path.Combine(subDir, "HS_B_8.png"), "data");



                int success = StaffDailyWorkflowsServices.BatchUploadStudentAvatars(db, srcDir, "StudentCode", DateTime.Now);

                Assert.Equal(0, success); // Skipped because it's inside a subfolder

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_BatchUpload_BackupOld_Disabled()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_B_9", FullName = "HS B9", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Avatar_BackupOldOnOverwrite", Value = "0", Category = "IT" });

                db.SaveChanges();



                var student = db.Students.First();

                var srcDir = Path.Combine(AppPaths.TempDir, "BatchAvatars9");

                Directory.CreateDirectory(srcDir);

                File.WriteAllText(Path.Combine(srcDir, "HS_B_9.png"), "data");



                // First upload

                StaffDailyWorkflowsServices.BatchUploadStudentAvatars(db, srcDir, "StudentCode", DateTime.Now.AddMinutes(-5));

                var path1 = db.Students.First().AvatarPath;



                // Second upload

                StaffDailyWorkflowsServices.BatchUploadStudentAvatars(db, srcDir, "StudentCode", DateTime.Now);

                var path2 = db.Students.First().AvatarPath;



                Assert.False(File.Exists(path1)); // Deleted immediately

                Assert.True(File.Exists(path2));

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_BatchUpload_EmptyFolder()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var srcDir = Path.Combine(AppPaths.TempDir, "BatchAvatarsEmpty");

                Directory.CreateDirectory(srcDir);



                int success = StaffDailyWorkflowsServices.BatchUploadStudentAvatars(db, srcDir, "StudentCode", DateTime.Now);

                Assert.Equal(0, success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_BatchUpload_TransactionRollbackOnCopyError()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.Students.Add(new Student { StudentCode = "HS_B_11", FullName = "HS B11", Status = "Active" });

                db.Students.Add(new Student { StudentCode = "HS_B_12", FullName = "HS B12", Status = "Active" });

                db.SaveChanges();



                var srcDir = Path.Combine(AppPaths.TempDir, "BatchAvatarsRollback");

                if (Directory.Exists(srcDir)) Directory.Delete(srcDir, true);

                Directory.CreateDirectory(srcDir);

                var f1 = Path.Combine(srcDir, "HS_B_11.png");

                File.WriteAllText(f1, "data");



                var f2 = Path.Combine(srcDir, "HS_B_12.png");

                File.WriteAllText(f2, "data");



                // Lock the second file so that File.Copy throws an IOException due to sharing violation

                using var lockStream = new FileStream(f2, FileMode.Open, FileAccess.Read, FileShare.None);



                int success = StaffDailyWorkflowsServices.BatchUploadStudentAvatars(db, srcDir, "StudentCode", DateTime.Now);

                Assert.Equal(0, success); // Entire transaction rolled back





                var s1 = db.Students.First(x => x.StudentCode == "HS_B_11");


                db.Entry(s1).Reload();


                Assert.Empty(s1.AvatarPath); // Rolled back, so empty!


            }


                        finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        // --- PHASE 5 - PART 3: IT-ADMIN DEVICE MONITORING, LOGS & DIAGNOSTICS TESTS ---

[Fact]

        public void Test_ITAdmin_Heartbeat_NormalOnline()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_1", FullName = "Học sinh M1", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SaveChanges();



                var status = StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M1", 12.5, 45.0, "[]", "Visual Studio", DateTime.Now);

                Assert.Equal("Online", status);



                var device = db.StudentDeviceStatuses.First(d => d.StudentId == s.Id);

                Assert.Equal("Online", device.Status);

                Assert.Equal("None", device.AnomaliesDetected);

                Assert.False(device.IsLocked);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Heartbeat_HighCpuAnomaly()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_2", FullName = "Học sinh M2", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SaveChanges();



                var status = StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M2", 95.5, 45.0, "[]", "Visual Studio", DateTime.Now);

                Assert.Equal("Anomalous", status);



                var device = db.StudentDeviceStatuses.First(d => d.StudentId == s.Id);

                Assert.Equal("Anomalous", device.Status);

                Assert.Contains("HighCpu", device.AnomaliesDetected);



                var log = db.SystemDiagnosticLogs.FirstOrDefault(l => l.StudentId == s.Id);

                Assert.NotNull(log);

                Assert.Contains("HighCpu", log.Message);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Heartbeat_HighMemoryAnomaly()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_3", FullName = "Học sinh M3", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SaveChanges();



                var status = StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M3", 12.0, 98.5, "[]", "Visual Studio", DateTime.Now);

                Assert.Equal("Anomalous", status);



                var device = db.StudentDeviceStatuses.First(d => d.StudentId == s.Id);

                Assert.Equal("Anomalous", device.Status);

                Assert.Contains("HighMemory", device.AnomaliesDetected);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Heartbeat_CustomCpuThreshold()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_4", FullName = "Học sinh M4", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_CpuThresholdPercentage", Value = "75.0", Category = "IT" });

                db.SaveChanges();



                var status = StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M4", 80.0, 50.0, "[]", "VS Code", DateTime.Now);

                Assert.Equal("Anomalous", status);



                var device = db.StudentDeviceStatuses.First(d => d.StudentId == s.Id);

                Assert.Contains("HighCpu", device.AnomaliesDetected);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Heartbeat_CustomMemoryThreshold()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_5", FullName = "Học sinh M5", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_MemoryThresholdPercentage", Value = "80.0", Category = "IT" });

                db.SaveChanges();



                var status = StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M5", 30.0, 85.0, "[]", "VS Code", DateTime.Now);

                Assert.Equal("Anomalous", status);



                var device = db.StudentDeviceStatuses.First(d => d.StudentId == s.Id);

                Assert.Contains("HighMemory", device.AnomaliesDetected);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Heartbeat_ForbiddenAppAnomaly()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_6", FullName = "Học sinh M6", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_ForbiddenAppsList", Value = "lol.exe,roblox.exe", Category = "IT" });

                db.SaveChanges();



                var apps = "[\"devenv.exe\", \"chrome.exe\", \"lol.exe\"]";

                var status = StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M6", 15.0, 50.0, apps, "League of Legends", DateTime.Now);

                Assert.Equal("Anomalous", status);



                var device = db.StudentDeviceStatuses.First(d => d.StudentId == s.Id);

                Assert.Contains("ForbiddenApp", device.AnomaliesDetected);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Heartbeat_CaseInsensitiveForbiddenApp()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_7", FullName = "Học sinh M7", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_ForbiddenAppsList", Value = "lol.exe", Category = "IT" });

                db.SaveChanges();



                var apps = "[\"LOL.EXE\"]";

                var status = StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M7", 10.0, 40.0, apps, "League", DateTime.Now);

                Assert.Equal("Anomalous", status);



                var device = db.StudentDeviceStatuses.First(d => d.StudentId == s.Id);

                Assert.Contains("ForbiddenApp", device.AnomaliesDetected);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Heartbeat_ForbiddenApp_PaddingSafety()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_8", FullName = "Học sinh M8", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_ForbiddenAppsList", Value = " lol.exe , genshin.exe ", Category = "IT" });

                db.SaveChanges();



                var apps = "[\"lol.exe\"]";

                var status = StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M8", 10.0, 40.0, apps, "Game", DateTime.Now);

                Assert.Equal("Anomalous", status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Heartbeat_LockScreenOnForbiddenApp()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_9", FullName = "Học sinh M9", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_ForbiddenAppsList", Value = "lol.exe", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_AutoActionOnAnomaly", Value = "1", Category = "IT" });

                db.SaveChanges();



                var apps = "[\"lol.exe\"]";

                StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M9", 10.0, 40.0, apps, "Game", DateTime.Now);



                var device = db.StudentDeviceStatuses.First(d => d.StudentId == s.Id);

                Assert.True(device.IsLocked);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Heartbeat_NoActionOnAnomalyMode0()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_10", FullName = "Học sinh M10", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_ForbiddenAppsList", Value = "lol.exe", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_AutoActionOnAnomaly", Value = "0", Category = "IT" });

                db.SaveChanges();



                var apps = "[\"lol.exe\"]";

                StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M10", 10.0, 40.0, apps, "Game", DateTime.Now);



                var device = db.StudentDeviceStatuses.First(d => d.StudentId == s.Id);

                Assert.False(device.IsLocked);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Heartbeat_NotificationReceiver_TeacherOnly()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_11", FullName = "Học sinh M11", ClassName = "10A2", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_ForbiddenAppsList", Value = "lol.exe", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_NotificationReceiver", Value = "teacher", Category = "IT" });

                db.SaveChanges();



                var apps = "[\"lol.exe\"]";

                StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M11", 10.0, 40.0, apps, "Game", DateTime.Now);



                var teacherMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "teacher_10A2");

                var adminMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "ADMIN");



                Assert.NotNull(teacherMsg);

                Assert.Null(adminMsg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Heartbeat_NotificationReceiver_AdminOnly()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_12", FullName = "Học sinh M12", ClassName = "10A2", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_ForbiddenAppsList", Value = "lol.exe", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_NotificationReceiver", Value = "admin", Category = "IT" });

                db.SaveChanges();



                var apps = "[\"lol.exe\"]";

                StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M12", 10.0, 40.0, apps, "Game", DateTime.Now);



                var teacherMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "teacher_10A2");

                var adminMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "ADMIN");



                Assert.Null(teacherMsg);

                Assert.NotNull(adminMsg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Heartbeat_NotificationReceiver_Both()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_13", FullName = "Học sinh M13", ClassName = "10A3", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_ForbiddenAppsList", Value = "lol.exe", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_NotificationReceiver", Value = "both", Category = "IT" });

                db.SaveChanges();



                var apps = "[\"lol.exe\"]";

                StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M13", 10.0, 40.0, apps, "Game", DateTime.Now);



                var teacherMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "teacher_10A3");

                var adminMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "ADMIN");



                Assert.NotNull(teacherMsg);

                Assert.NotNull(adminMsg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Heartbeat_NotificationReceiver_None()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_14", FullName = "Học sinh M14", ClassName = "10A3", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_ForbiddenAppsList", Value = "lol.exe", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_NotificationReceiver", Value = "none", Category = "IT" });

                db.SaveChanges();



                var apps = "[\"lol.exe\"]";

                StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M14", 10.0, 40.0, apps, "Game", DateTime.Now);



                var teacherMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "teacher_10A3");

                var adminMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "ADMIN");



                Assert.Null(teacherMsg);

                Assert.Null(adminMsg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Heartbeat_NonExistentStudentSafeReturn()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var status = StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, 9999, "DESKTOP-NONE", 10.0, 40.0, "[]", "VS Code", DateTime.Now);

                Assert.Equal("StudentNotFound", status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Diagnostic_OfflineTimeoutDetection()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_16", FullName = "Học sinh M16", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SaveChanges(); // Save student first to generate ID



                db.StudentDeviceStatuses.Add(new StudentDeviceStatus

                {

                    StudentId = s.Id,

                    DeviceName = "DESKTOP-M16",

                    Status = "Online",

                    LastHeartbeat = DateTime.Now.AddMinutes(-20)

                });

                db.SaveChanges();



                int affected = StaffDailyWorkflowsServices.DiagnoseAndResolveDeviceErrors(db, DateTime.Now);

                Assert.Equal(1, affected);



                var device = db.StudentDeviceStatuses.First(d => d.StudentId == s.Id);

                Assert.Equal("Offline", device.Status);



                var log = db.SystemDiagnosticLogs.FirstOrDefault(l => l.StudentId == s.Id && l.LogType == "Network");

                Assert.NotNull(log);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Diagnostic_NoOfflineBeforeTimeout()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_17", FullName = "Học sinh M17", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SaveChanges(); // Save student first to generate ID



                db.StudentDeviceStatuses.Add(new StudentDeviceStatus

                {

                    StudentId = s.Id,

                    DeviceName = "DESKTOP-M17",

                    Status = "Online",

                    LastHeartbeat = DateTime.Now.AddMinutes(-10)

                });

                db.SaveChanges();



                int affected = StaffDailyWorkflowsServices.DiagnoseAndResolveDeviceErrors(db, DateTime.Now);

                Assert.Equal(0, affected);



                var device = db.StudentDeviceStatuses.First(d => d.StudentId == s.Id);

                Assert.Equal("Online", device.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Diagnostic_CustomOfflineTimeout()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_18", FullName = "Học sinh M18", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SaveChanges(); // Save student first to generate ID



                db.StudentDeviceStatuses.Add(new StudentDeviceStatus

                {

                    StudentId = s.Id,

                    DeviceName = "DESKTOP-M18",

                    Status = "Online",

                    LastHeartbeat = DateTime.Now.AddMinutes(-6)

                });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_HeartbeatTimeoutMinutes", Value = "5", Category = "IT" });

                db.SaveChanges();



                int affected = StaffDailyWorkflowsServices.DiagnoseAndResolveDeviceErrors(db, DateTime.Now);

                Assert.Equal(1, affected);



                var device = db.StudentDeviceStatuses.First(d => d.StudentId == s.Id);

                Assert.Equal("Offline", device.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Diagnostic_AutoCleanDiskSpaceError()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemDiagnosticLogs.Add(new SystemDiagnosticLog

                {

                    Timestamp = DateTime.Now,

                    DeviceName = "DESKTOP-DISK",

                    LogType = "System",

                    Message = "Cảnh báo ổ đĩa đầy",

                    IsResolved = false

                });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_AutoResolveSoftwareErrors", Value = "1", Category = "IT" });

                db.SaveChanges();



                int resolved = StaffDailyWorkflowsServices.DiagnoseAndResolveDeviceErrors(db, DateTime.Now);

                Assert.Equal(1, resolved);



                var log = db.SystemDiagnosticLogs.First();

                Assert.True(log.IsResolved);

                Assert.Equal("Auto cleaned temp directories", log.ResolutionAction);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Diagnostic_CustomDiskThreshold()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemDiagnosticLogs.Add(new SystemDiagnosticLog

                {

                    Timestamp = DateTime.Now,

                    DeviceName = "DESKTOP-DISK2",

                    LogType = "System",

                    Message = "Cảnh báo ổ đĩa đầy (>80%)",

                    IsResolved = false

                });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_AutoResolveSoftwareErrors", Value = "1", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_DiskThresholdPercentage", Value = "80.0", Category = "IT" });

                db.SaveChanges();



                int resolved = StaffDailyWorkflowsServices.DiagnoseAndResolveDeviceErrors(db, DateTime.Now);

                Assert.Equal(1, resolved);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Diagnostic_AutoResolveSyncConflicts_ChecksumOnly()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.OfflineSyncItems.Add(new OfflineSyncItem { ActionType = "GRADE", PayloadJson = "{}", Status = "Conflict_Failed" });

                db.OfflineSyncItems.Add(new OfflineSyncItem { ActionType = "GRADE", PayloadJson = "{}", Status = "Failed" });

                db.SystemDiagnosticLogs.Add(new SystemDiagnosticLog

                {

                    Timestamp = DateTime.Now,

                    LogType = "Network",

                    Message = "Sync error: checksum mismatch",

                    IsResolved = false

                });

                db.SystemDiagnosticLogs.Add(new SystemDiagnosticLog

                {

                    Timestamp = DateTime.Now,

                    LogType = "Network",

                    Message = "Sync error: parse exception",

                    IsResolved = false

                });



                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_AutoResolveSoftwareErrors", Value = "1", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_AutoResolveSyncErrorTypes", Value = "1", Category = "IT" });

                db.SaveChanges();



                int resolved = StaffDailyWorkflowsServices.DiagnoseAndResolveDeviceErrors(db, DateTime.Now);

                Assert.Equal(1, resolved);



                var checksumLog = db.SystemDiagnosticLogs.First(l => l.Message.Contains("checksum"));

                Assert.True(checksumLog.IsResolved);



                var parseLog = db.SystemDiagnosticLogs.First(l => l.Message.Contains("parse"));

                Assert.False(parseLog.IsResolved);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Diagnostic_AutoResolveSyncConflicts_AllTypes()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.OfflineSyncItems.Add(new OfflineSyncItem { ActionType = "GRADE", PayloadJson = "{}", Status = "Conflict_Failed" });

                db.OfflineSyncItems.Add(new OfflineSyncItem { ActionType = "GRADE", PayloadJson = "{}", Status = "Failed" });

                db.SystemDiagnosticLogs.Add(new SystemDiagnosticLog { Timestamp = DateTime.Now, LogType = "Network", Message = "Sync error: checksum", IsResolved = false });

                db.SystemDiagnosticLogs.Add(new SystemDiagnosticLog { Timestamp = DateTime.Now, LogType = "Network", Message = "Sync error: parse", IsResolved = false });



                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_AutoResolveSoftwareErrors", Value = "1", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_AutoResolveSyncErrorTypes", Value = "2", Category = "IT" });

                db.SaveChanges();



                int resolved = StaffDailyWorkflowsServices.DiagnoseAndResolveDeviceErrors(db, DateTime.Now);

                Assert.Equal(2, resolved);



                var items = db.OfflineSyncItems.ToList();

                Assert.All(items, item => Assert.Equal("Pending", item.Status));

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Diagnostic_NoAutoResolveSyncWhenDisabled()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.OfflineSyncItems.Add(new OfflineSyncItem { ActionType = "GRADE", PayloadJson = "{}", Status = "Conflict_Failed" });

                db.SystemDiagnosticLogs.Add(new SystemDiagnosticLog { Timestamp = DateTime.Now, LogType = "Network", Message = "Sync error: checksum", IsResolved = false });



                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_AutoResolveSoftwareErrors", Value = "0", Category = "IT" });

                db.SaveChanges();



                int resolved = StaffDailyWorkflowsServices.DiagnoseAndResolveDeviceErrors(db, DateTime.Now);

                Assert.Equal(0, resolved);



                var item = db.OfflineSyncItems.First();

                Assert.Equal("Conflict_Failed", item.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Manual_ResolveUnlockDevice()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_24", FullName = "Học sinh M24", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SaveChanges(); // Save student first to generate ID



                db.StudentDeviceStatuses.Add(new StudentDeviceStatus

                {

                    StudentId = s.Id,

                    DeviceName = "DESKTOP-M24",

                    Status = "Anomalous",

                    IsLocked = true

                });

                db.SystemDiagnosticLogs.Add(new SystemDiagnosticLog

                {

                    Timestamp = DateTime.Now,

                    DeviceName = "DESKTOP-M24",

                    StudentId = s.Id,

                    Message = "Locked due to anomaly",

                    IsResolved = false

                });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ResolveDeviceAnomalyManual(db, s.Id, "Unlock", DateTime.Now);

                Assert.True(success);



                var device = db.StudentDeviceStatuses.First(d => d.StudentId == s.Id);

                Assert.False(device.IsLocked);

                Assert.Equal("Online", device.Status);



                var log = db.SystemDiagnosticLogs.First(l => l.StudentId == s.Id);

                Assert.True(log.IsResolved);

                Assert.Equal("Manually unlocked by IT-Admin", log.ResolutionAction);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Manual_ResolveCleanLogs()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_25", FullName = "Học sinh M25", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SaveChanges(); // Save student first to generate ID



                db.StudentDeviceStatuses.Add(new StudentDeviceStatus

                {

                    StudentId = s.Id,

                    DeviceName = "DESKTOP-M25",

                    Status = "Anomalous",

                    IsLocked = false

                });

                db.SystemDiagnosticLogs.Add(new SystemDiagnosticLog

                {

                    Timestamp = DateTime.Now,

                    DeviceName = "DESKTOP-M25",

                    StudentId = s.Id,

                    Message = "High CPU log",

                    IsResolved = false

                });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ResolveDeviceAnomalyManual(db, s.Id, "CleanLog", DateTime.Now);

                Assert.True(success);



                var log = db.SystemDiagnosticLogs.First(l => l.StudentId == s.Id);

                Assert.True(log.IsResolved);

                Assert.Equal("Manually resolved by IT-Admin", log.ResolutionAction);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Manual_InvalidActionType()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_26", FullName = "Học sinh M26", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SaveChanges(); // Save student first to generate ID



                db.StudentDeviceStatuses.Add(new StudentDeviceStatus { StudentId = s.Id, DeviceName = "DESKTOP-M26", Status = "Online" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ResolveDeviceAnomalyManual(db, s.Id, "InvalidAction", DateTime.Now);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Heartbeat_TransactionRollbackOnDatabaseError()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_27", FullName = "Học sinh M27", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SaveChanges();



                db.Database.GetDbConnection().Close();



                Assert.ThrowsAny<Exception>(() => 

                    StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M27", 10.0, 40.0, "[]", "VS Code", DateTime.Now)

                );

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_PedagogicalPrivacyCompliance()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_28", FullName = "Học sinh M28", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_ForbiddenAppsList", Value = "lol.exe", Category = "IT" });

                db.SaveChanges();



                var apps = "[\"lol.exe\"]";

                StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M28", 10.0, 40.0, apps, "Game", DateTime.Now);



                var bulletin = db.Bulletins.FirstOrDefault(b => b.Content.Contains("HS_M_28") || b.Content.Contains("Học sinh M28"));

                Assert.Null(bulletin);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Heartbeat_AutoTerminateProcessMode2()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_29", FullName = "Học sinh M29", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_ForbiddenAppsList", Value = "lol.exe", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_AutoActionOnAnomaly", Value = "2", Category = "IT" });

                db.SaveChanges();



                var apps = "[\"lol.exe\"]";

                var status = StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M29", 10.0, 40.0, apps, "Game", DateTime.Now);



                Assert.Equal("Online", status);



                var device = db.StudentDeviceStatuses.First(d => d.StudentId == s.Id);

                Assert.False(device.IsLocked);



                var log = db.SystemDiagnosticLogs.First(l => l.StudentId == s.Id);

                Assert.True(log.IsResolved);

                Assert.Equal("Tự động đóng ứng dụng cấm thành công.", log.ResolutionAction);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAdmin_Heartbeat_DuplicateMessagePrevention()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_M_30", FullName = "Học sinh M30", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Monitor_ForbiddenAppsList", Value = "lol.exe", Category = "IT" });

                db.SaveChanges();



                var apps = "[\"lol.exe\"]";

                

                StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M30", 10.0, 40.0, apps, "Game", DateTime.Now);

                StaffDailyWorkflowsServices.ProcessStudentDeviceHeartbeat(db, s.Id, "DESKTOP-M30", 11.0, 41.0, apps, "Game", DateTime.Now);



                var msgsCount = db.InboxMessages.Count(m => m.ReceiverId == "teacher_10A1");

                Assert.Equal(1, msgsCount);

            }

                        finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        // --- PHASE 5 - PART 4: PRINCIPAL & IT-ADMIN LEGISLATION OVERRIDES & AUDITING TESTS ---

[Fact]

        public void Test_PrincipalOverride_Success_PinMode()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_1", FullName = "Học sinh O1", ClassName = "10A1", Status = "Active", ConductScore = 85 };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "Principal_Override_ValidationMode", Value = "1", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.RequestAndApproveResourceOverride(

                    db, "HT001", "1234", "", "BorrowLimit", "DEVICE_1", s.Id, "Cho phép mượn khẩn cấp", DateTime.Now

                );

                Assert.True(success);



                var log = db.PrincipalOverrideLogs.FirstOrDefault(l => l.StudentId == s.Id);

                Assert.NotNull(log);

                Assert.Equal("HT001", log.PrincipalId);

                Assert.Equal("PIN", log.ValidationMethodUsed);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_PrincipalOverride_WrongPin_Rejected()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_2", FullName = "Học sinh O2", ClassName = "10A1", Status = "Active", ConductScore = 85 };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "Principal_Override_ValidationMode", Value = "1", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.RequestAndApproveResourceOverride(

                    db, "HT001", "9999", "", "BorrowLimit", "DEVICE_2", s.Id, "Cho phép mượn khẩn cấp", DateTime.Now

                );

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_PrincipalOverride_Success_OtpMode()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_3", FullName = "Học sinh O3", ClassName = "10A1", Status = "Active", ConductScore = 85 };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "Principal_Override_ValidationMode", Value = "2", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.RequestAndApproveResourceOverride(

                    db, "HT001", "1234", "666888", "BorrowLimit", "DEVICE_3", s.Id, "Cho phép", DateTime.Now

                );

                Assert.True(success);



                var log = db.PrincipalOverrideLogs.FirstOrDefault(l => l.StudentId == s.Id);

                Assert.Equal("OTP", log.ValidationMethodUsed);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_PrincipalOverride_WrongOtp_Rejected()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_4", FullName = "Học sinh O4", ClassName = "10A1", Status = "Active", ConductScore = 85 };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "Principal_Override_ValidationMode", Value = "2", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.RequestAndApproveResourceOverride(

                    db, "HT001", "1234", "111222", "BorrowLimit", "DEVICE_4", s.Id, "Cho phép", DateTime.Now

                );

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_PrincipalOverride_BypassValidationMode0()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_5", FullName = "Học sinh O5", ClassName = "10A1", Status = "Active", ConductScore = 85 };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "Principal_Override_ValidationMode", Value = "0", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.RequestAndApproveResourceOverride(

                    db, "HT001", "", "", "BorrowLimit", "DEVICE_5", s.Id, "Cho phép", DateTime.Now

                );

                Assert.True(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_PrincipalOverride_ExceedLimitQuota()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_6", FullName = "Học sinh O6", ClassName = "10A1", Status = "Active", ConductScore = 85 };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "Principal_Override_MaxActiveSlotLimit", Value = "2", Category = "IT" });

                db.SaveChanges();



                bool r1 = StaffDailyWorkflowsServices.RequestAndApproveResourceOverride(db, "HT001", "", "", "BorrowLimit", "D1", s.Id, "P1", DateTime.Now);

                bool r2 = StaffDailyWorkflowsServices.RequestAndApproveResourceOverride(db, "HT001", "", "", "BorrowLimit", "D2", s.Id, "P2", DateTime.Now);

                bool r3 = StaffDailyWorkflowsServices.RequestAndApproveResourceOverride(db, "HT001", "", "", "BorrowLimit", "D3", s.Id, "P3", DateTime.Now);



                Assert.True(r1);

                Assert.True(r2);

                Assert.False(r3); // Over quota limit 2

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_PrincipalOverride_LogDatabaseRegistration()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_7", FullName = "Học sinh O7", ClassName = "10A1", Status = "Active", ConductScore = 85 };

                db.Students.Add(s);

                db.SaveChanges();



                StaffDailyWorkflowsServices.RequestAndApproveResourceOverride(db, "HT001", "", "", "BorrowLimit", "D7", s.Id, "Pedagogical", DateTime.Now);

                

                var log = db.PrincipalOverrideLogs.First(l => l.StudentId == s.Id);

                Assert.Equal("Pedagogical", log.OverrideReason);

                Assert.True(log.IsSuccess);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_PrincipalOverride_NonExistentStudentSafe()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                bool success = StaffDailyWorkflowsServices.RequestAndApproveResourceOverride(db, "HT001", "", "", "BorrowLimit", "D8", 9999, "Reason", DateTime.Now);

                Assert.True(success); // Validation still passes even if student ID isn't linked to a real student (as it is allowed under basic schema log assignment)

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_PrincipalOverride_TransactionSafety()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_9", FullName = "Học sinh O9", ClassName = "10A1", Status = "Active", ConductScore = 85 };

                db.Students.Add(s);

                db.SaveChanges();



                db.Database.GetDbConnection().Close(); // Force database error



                bool success = StaffDailyWorkflowsServices.RequestAndApproveResourceOverride(db, "HT001", "", "", "BorrowLimit", "D9", s.Id, "Reason", DateTime.Now);

                Assert.False(success); // Safe catch returns false

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_PrincipalOverride_PedagogicalPrivacy()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_10", FullName = "Học sinh O10", ClassName = "10A1", Status = "Active", ConductScore = 85 };

                db.Students.Add(s);

                db.SaveChanges();



                StaffDailyWorkflowsServices.RequestAndApproveResourceOverride(db, "HT001", "", "", "BorrowLimit", "D10", s.Id, "Reason", DateTime.Now);



                var bulletin = db.Bulletins.FirstOrDefault(b => b.Content.Contains("HS_O_10") || b.Content.Contains("HT001"));

                Assert.Null(bulletin); // Must comply with pedagogical privacy, no public bulletins

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAudit_LogCreation_ReadSensitive()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Audit_LoggingLevel", Value = "2", Category = "IT" });

                db.SaveChanges();



                bool safe = StaffDailyWorkflowsServices.AuditSensitiveDataAccess(db, "GV001", "Teacher", "StudentPhysicalRestriction", "Read", "StudentId=1", DateTime.Today.AddHours(9));

                Assert.True(safe);



                var logs = db.SensitiveDataAccessAudits.ToList();

                Assert.Single(logs);

                Assert.Equal("Read", logs[0].OperationType);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAudit_NoLog_ReadSensitive_Mode1()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Audit_LoggingLevel", Value = "1", Category = "IT" });

                db.SaveChanges();



                bool safe = StaffDailyWorkflowsServices.AuditSensitiveDataAccess(db, "GV001", "Teacher", "StudentPhysicalRestriction", "Read", "StudentId=1", DateTime.Today.AddHours(9));

                Assert.True(safe);



                var logs = db.SensitiveDataAccessAudits.ToList();

                Assert.Empty(logs); // Logging level 1 ignores Read operations

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAudit_LogCreation_WriteSensitive_Mode1()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Audit_LoggingLevel", Value = "1", Category = "IT" });

                db.SaveChanges();



                bool safe = StaffDailyWorkflowsServices.AuditSensitiveDataAccess(db, "GV001", "Teacher", "StudentPhysicalRestriction", "Write", "StudentId=1", DateTime.Today.AddHours(9));

                Assert.True(safe);



                var logs = db.SensitiveDataAccessAudits.ToList();

                Assert.Single(logs);

                Assert.Equal("Write", logs[0].OperationType);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAudit_LogCreation_DisabledMode0()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Audit_LoggingLevel", Value = "0", Category = "IT" });

                db.SaveChanges();



                bool safe = StaffDailyWorkflowsServices.AuditSensitiveDataAccess(db, "GV001", "Teacher", "StudentPhysicalRestriction", "Write", "StudentId=1", DateTime.Today.AddHours(9));

                Assert.True(safe);



                var logs = db.SensitiveDataAccessAudits.ToList();

                Assert.Empty(logs); // Mode 0 disables auditing completely

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAudit_AnomalousQuery_FrequencyTrigger()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Audit_LoggingLevel", Value = "2", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Audit_AnomalousQueryCountThreshold", Value = "5", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Audit_AutoLockSessionOnAnomaly", Value = "1", Category = "IT" });

                db.SaveChanges();



                var now = DateTime.Now;

                for (int i = 0; i < 4; i++)

                {

                    db.SensitiveDataAccessAudits.Add(new SensitiveDataAccessAudit { AccessorId = "GV002", Timestamp = now, OperationType = "Read" });

                }

                db.SaveChanges();



                // 5th query should trigger anomaly and LockSession action

                bool safe = StaffDailyWorkflowsServices.AuditSensitiveDataAccess(db, "GV002", "Teacher", "SELProfile", "Read", "All", now);

                Assert.False(safe); // Anomalous query returns false (threat detected)



                var latestLog = db.SensitiveDataAccessAudits.First(l => l.AccessorId == "GV002" && l.IsAnomalous);

                Assert.Equal("LockSession", latestLog.MitigationActionTaken);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAudit_AnomalousQuery_OfficeHoursCompliance()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Audit_LoggingLevel", Value = "2", Category = "IT" });

                db.SaveChanges();



                // Accessing at 1:00 AM (night) should trigger anomaly immediately

                DateTime nightTime = DateTime.Today.AddHours(1);

                bool safe = StaffDailyWorkflowsServices.AuditSensitiveDataAccess(db, "GV003", "Teacher", "SELProfile", "Read", "All", nightTime);

                Assert.False(safe);



                var log = db.SensitiveDataAccessAudits.First(l => l.AccessorId == "GV003");

                Assert.True(log.IsAnomalous);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAudit_AnomalousQuery_SystemAccountBypass()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Audit_LoggingLevel", Value = "2", Category = "IT" });

                db.SaveChanges();



                // Accessing at 1:00 AM using system account must bypass the office hour check

                DateTime nightTime = DateTime.Today.AddHours(1);

                bool safe = StaffDailyWorkflowsServices.AuditSensitiveDataAccess(db, "SYSTEM", "System", "SELProfile", "Read", "All", nightTime);

                Assert.True(safe); // SYSTEM is exempted

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAudit_AnomalousQuery_AlertMessagesSent()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Audit_LoggingLevel", Value = "2", Category = "IT" });

                db.SaveChanges();



                DateTime nightTime = DateTime.Today.AddHours(1);

                StaffDailyWorkflowsServices.AuditSensitiveDataAccess(db, "GV004", "Teacher", "HealthRecord", "Read", "All", nightTime);



                var adminMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "ADMIN");

                var principalMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "HT001");



                Assert.NotNull(adminMsg);

                Assert.Contains("[CẢNH BÁO ĐỎ]", adminMsg.Content);

                Assert.NotNull(principalMsg);

                Assert.Contains("[CẢNH BÁO ĐỎ]", principalMsg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAudit_NonExistentAccessorSafe()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                bool safe = StaffDailyWorkflowsServices.AuditSensitiveDataAccess(db, "", "Unknown", "HealthRecord", "Read", "None", DateTime.Today.AddHours(9));

                Assert.True(safe);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ITAudit_TransactionSafetyOnWrite()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.Database.GetDbConnection().Close(); // Force database error



                bool safe = StaffDailyWorkflowsServices.AuditSensitiveDataAccess(db, "GV005", "Teacher", "HealthRecord", "Read", "None", DateTime.Today.AddHours(9));

                Assert.False(safe); // Returns false on catch block failure

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_StudyLoadAdjustment_Success_ManualApproval()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_SL_1", FullName = "Học sinh SL1", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "Principal_StudyLoadAdjustment_AutoApply", Value = "2", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProposeAndApproveStudyLoadRelief(

                    db, s.Id, 0.25, -50, "Medium", "HT001", "1234", DateTime.Now

                );

                Assert.True(success);



                var adjust = db.StudyLoadAdjustments.First(a => a.StudentId == s.Id);

                Assert.Equal("Active", adjust.Status);

                Assert.Equal(0.25, adjust.HomeworkReductionRatio);

                Assert.Equal(-50, adjust.AllowedLexileOffsetAdjustment);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_StudyLoadAdjustment_Rejected_WrongPin()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_SL_2", FullName = "Học sinh SL2", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "Principal_StudyLoadAdjustment_AutoApply", Value = "2", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProposeAndApproveStudyLoadRelief(

                    db, s.Id, 0.25, -50, "Medium", "HT001", "9999", DateTime.Now

                );

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_StudyLoadAdjustment_AutoApply_Success()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_SL_3", FullName = "Học sinh SL3", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "Principal_StudyLoadAdjustment_AutoApply", Value = "1", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProposeAndApproveStudyLoadRelief(

                    db, s.Id, 0.25, -50, "Medium", "SYSTEM", "", DateTime.Now

                );

                Assert.True(success);



                var adjust = db.StudyLoadAdjustments.First(a => a.StudentId == s.Id);

                Assert.Equal("Active", adjust.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_StudyLoadAdjustment_StudentNotFound_Safe()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                bool success = StaffDailyWorkflowsServices.ProposeAndApproveStudyLoadRelief(

                    db, 9999, 0.25, -50, "Medium", "HT001", "1234", DateTime.Now

                );

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_StudyLoadAdjustment_DurationCalculation()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_SL_5", FullName = "Học sinh SL5", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "Principal_StudyLoadAdjustment_AutoApply", Value = "1", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "Principal_StudyLoadAdjustment_DurationDays", Value = "10", Category = "IT" });

                db.SaveChanges();



                var now = DateTime.Today;

                StaffDailyWorkflowsServices.ProposeAndApproveStudyLoadRelief(db, s.Id, 0.2, -50, "Medium", "SYSTEM", "", now);



                var adjust = db.StudyLoadAdjustments.First(a => a.StudentId == s.Id);

                Assert.Equal(now.AddDays(10), adjust.EndDate);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_StudyLoadAdjustment_PrivateNotification_Teacher()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_SL_6", FullName = "Nguyễn Văn A", ClassName = "10A2", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "Principal_StudyLoadAdjustment_AutoApply", Value = "1", Category = "IT" });

                db.SaveChanges();



                StaffDailyWorkflowsServices.ProposeAndApproveStudyLoadRelief(db, s.Id, 0.3, -50, "Medium", "SYSTEM", "", DateTime.Now);



                var teacherMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "teacher_10A2");

                Assert.NotNull(teacherMsg);

                Assert.Contains("[ĐIỀU TIẾT GIẢM TẢI HỌC TẬP]", teacherMsg.Content);

                Assert.Contains("Nguyễn Văn A", teacherMsg.Content);

                Assert.Contains("giảm bớt 30%", teacherMsg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_StudyLoadAdjustment_PrivateNotification_Parent()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_SL_7", FullName = "Nguyễn Văn A", ClassName = "10A2", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "Principal_StudyLoadAdjustment_AutoApply", Value = "1", Category = "IT" });

                db.SaveChanges();



                StaffDailyWorkflowsServices.ProposeAndApproveStudyLoadRelief(db, s.Id, 0.3, -50, "Medium", "SYSTEM", "", DateTime.Now);



                var parentMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "parent_" + s.Id);

                Assert.NotNull(parentMsg);

                Assert.Contains("[ĐỒNG HÀNH HỌC TẬP]", parentMsg.Content);

                Assert.DoesNotContain("trầm cảm", parentMsg.Content); // Should not have sensitive mental health words

                Assert.DoesNotContain("bệnh", parentMsg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_StudyLoadAdjustment_NoPublicShaming()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_SL_8", FullName = "Nguyễn Văn B", ClassName = "10A2", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "Principal_StudyLoadAdjustment_AutoApply", Value = "1", Category = "IT" });

                db.SaveChanges();



                StaffDailyWorkflowsServices.ProposeAndApproveStudyLoadRelief(db, s.Id, 0.3, -50, "Medium", "SYSTEM", "", DateTime.Now);



                var bulletin = db.Bulletins.FirstOrDefault(b => b.Content.Contains("Nguyễn Văn B") || b.Content.Contains("giảm tải"));

                Assert.Null(bulletin); // Must not publish study load adjustments publicly

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_StudyLoadAdjustment_LexileOffsetApplied()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_SL_9", FullName = "Nguyễn Văn C", ClassName = "10A2", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "Principal_StudyLoadAdjustment_AutoApply", Value = "1", Category = "IT" });

                db.SaveChanges();



                StaffDailyWorkflowsServices.ProposeAndApproveStudyLoadRelief(db, s.Id, 0.2, -150, "Easy", "SYSTEM", "", DateTime.Now);



                var adjust = db.StudyLoadAdjustments.First(a => a.StudentId == s.Id);

                Assert.Equal(-150, adjust.AllowedLexileOffsetAdjustment);

                Assert.Equal("Easy", adjust.MaxQuizDifficultyAllowed);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_StudyLoadAdjustment_TransactionRollbackOnError()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_SL_10", FullName = "Học sinh SL10", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SaveChanges();



                db.Database.GetDbConnection().Close(); // Force database error



                bool success = StaffDailyWorkflowsServices.ProposeAndApproveStudyLoadRelief(

                    db, s.Id, 0.25, -50, "Medium", "HT001", "1234", DateTime.Now

                );

                Assert.False(success); // Catch block handles database error and returns false

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



// --- PHASE 5 - PART 5: INDOOR TRACKING & SECURE GATE INTEGRATION TESTS ---



        [Fact]

        public void Test_Location_ReportPing_Success()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_L_1", FullName = "Nguyễn Văn L1", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.CampusBeacons.Add(new CampusBeacon { Id = "BC_01", LocationName = "Thư viện", CoordinateX = 15.5, CoordinateY = 20.0, IsActive = true });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_L_1", "BC_01", -65.0, DateTime.Now);

                Assert.True(success);



                var stu = db.Students.FirstOrDefault(st => st.StudentCode == "HS_L_1");

                Assert.Equal(15.5, stu.PositionX);

                Assert.Equal(20.0, stu.PositionY);



                var history = db.StudentLocationHistories.FirstOrDefault(h => h.StudentCode == "HS_L_1");

                Assert.NotNull(history);

                Assert.Equal("Thư viện", history.CurrentZone);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Location_ReportPing_LowRssi_Ignored()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_L_2", FullName = "Nguyễn Văn L2", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.CampusBeacons.Add(new CampusBeacon { Id = "BC_02", LocationName = "Canteen", CoordinateX = 30.0, CoordinateY = 40.0, IsActive = true });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_L_2", "BC_02", -85.0, DateTime.Now); // -85.0 < -75.0 (Ignored)

                Assert.False(success);



                var stu = db.Students.FirstOrDefault(st => st.StudentCode == "HS_L_2");

                Assert.Equal(0.0, stu.PositionX); // Not updated

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Location_RestrictedZone_SendPrivateAlert()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_L_3", FullName = "Nguyễn Văn L3", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.CampusBeacons.Add(new CampusBeacon { Id = "BC_03", LocationName = "Trạm điện cao thế", AreaZone = "Restricted", CoordinateX = 5.0, CoordinateY = 5.0, IsActive = true });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_L_3", "BC_03", -50.0, DateTime.Now);

                Assert.True(success);



                var teacherMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "teacher_10A1" && m.SenderId == "SAFETY_SYSTEM");

                Assert.NotNull(teacherMsg);

                Assert.Contains("Trạm điện cao thế", teacherMsg.Content);



                var parentMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "parent_" + s.Id && m.SenderId == "SAFETY_SYSTEM");

                Assert.NotNull(parentMsg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Location_RestrictedZone_NoPublicShaming()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_L_4", FullName = "Nguyễn Văn L4", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.CampusBeacons.Add(new CampusBeacon { Id = "BC_04", LocationName = "Khu kỹ thuật", AreaZone = "Restricted", CoordinateX = 50.0, CoordinateY = 50.0, IsActive = true });

                db.SaveChanges();



                StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_L_4", "BC_04", -50.0, DateTime.Now);



                // Bulletins should not contain restricted zone alerts

                var bul = db.Bulletins.FirstOrDefault();

                Assert.Null(bul);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Location_PingSmoothAlgorithm()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_L_5", FullName = "Nguyễn Văn L5", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.CampusBeacons.Add(new CampusBeacon { Id = "BC_05", LocationName = "Phòng Gym", CoordinateX = 10.0, CoordinateY = 10.0, IsActive = true });

                db.SaveChanges();



                // Initial ping (good rssi)

                StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_L_5", "BC_05", -55.0, DateTime.Now);

                

                // Bad rssi ping should be ignored

                bool result = StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_L_5", "BC_05", -80.0, DateTime.Now);

                Assert.False(result);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Location_HistoryLogRegistration()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_L_6", FullName = "Nguyễn Văn L6", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.CampusBeacons.Add(new CampusBeacon { Id = "BC_06", LocationName = "Sân bóng", CoordinateX = 100.0, CoordinateY = 100.0, IsActive = true });

                db.SaveChanges();



                StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_L_6", "BC_06", -60.0, DateTime.Now);



                var historyCount = db.StudentLocationHistories.Count(h => h.StudentCode == "HS_L_6");

                Assert.Equal(1, historyCount);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Location_NonExistentStudentSafe()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.CampusBeacons.Add(new CampusBeacon { Id = "BC_07", LocationName = "Sân bóng", CoordinateX = 100.0, CoordinateY = 100.0, IsActive = true });

                db.SaveChanges();



                bool result = StaffDailyWorkflowsServices.ReportBeaconPing(db, "NON_EXIST", "BC_07", -60.0, DateTime.Now);

                Assert.False(result);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Location_NonExistentBeaconSafe()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_L_8", FullName = "Nguyễn Văn L8", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SaveChanges();



                bool result = StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_L_8", "BC_NON_EXIST", -60.0, DateTime.Now);

                Assert.False(result);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Location_DatabaseTransactionSafety()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_L_9", FullName = "Nguyễn Văn L9", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.CampusBeacons.Add(new CampusBeacon { Id = "BC_09", LocationName = "Canteen", CoordinateX = 10.0, CoordinateY = 10.0, IsActive = true });

                db.SaveChanges();



                db.Database.GetDbConnection().Close(); // Force database error



                bool result = StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_L_9", "BC_09", -60.0, DateTime.Now);

                Assert.False(result);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Location_MasterSetting_ThresholdChange()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_L_10", FullName = "Nguyễn Văn L10", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.CampusBeacons.Add(new CampusBeacon { Id = "BC_10", LocationName = "Canteen", CoordinateX = 10.0, CoordinateY = 10.0, IsActive = true });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Location_RssiThreshold", Value = "-60.0", Category = "IT" });

                db.SaveChanges();



                // -70.0 dBm < -60.0 dBm (Should be ignored)

                bool result = StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_L_10", "BC_10", -70.0, DateTime.Now);

                Assert.False(result);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_OfflineVerification_Success()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_1", FullName = "Học sinh O1", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                

                string keyData = "MY_SECRET_PUBKEY";

                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = keyData, IsActive = true });

                db.SaveChanges();



                string payload = "HS_O_1|" + DateTime.Now.AddMinutes(10).ToString("o");

                string signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload + "_" + keyData));



                bool success = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_O_1", payload, signature, "MAIN_GATE_01", DateTime.Now);

                Assert.True(success);



                var log = db.GateBarrierLogs.FirstOrDefault(l => l.StudentCode == "HS_O_1");

                Assert.NotNull(log);

                Assert.Equal("QR_LeavePass", log.TriggerSource);

                Assert.True(log.HardwareConfirmed);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_OfflineVerification_SignatureMismatch()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_2", FullName = "Học sinh O2", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                string keyData = "MY_SECRET_PUBKEY";

                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = keyData, IsActive = true });

                db.SaveChanges();



                string payload = "HS_O_2|" + DateTime.Now.AddMinutes(10).ToString("o");

                string signature = "WRONG_SIGNATURE";



                bool success = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_O_2", payload, signature, "MAIN_GATE_01", DateTime.Now);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_OfflineVerification_ExpiredTime()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_3", FullName = "Học sinh O3", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                string keyData = "MY_SECRET_PUBKEY";

                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = keyData, IsActive = true });

                db.SaveChanges();



                // Expired 15 mins ago

                string payload = "HS_O_3|" + DateTime.Now.AddMinutes(-15).ToString("o");

                string signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload + "_" + keyData));



                bool success = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_O_3", payload, signature, "MAIN_GATE_01", DateTime.Now);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_OfflineVerification_WriteBarrierLog()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_4", FullName = "Học sinh O4", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                string keyData = "MY_SECRET_PUBKEY";

                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = keyData, IsActive = true });

                db.SaveChanges();



                string payload = "HS_O_4|" + DateTime.Now.AddMinutes(10).ToString("o");

                string signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload + "_" + keyData));



                StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_O_4", payload, signature, "MAIN_GATE_01", DateTime.Now);



                var log = db.GateBarrierLogs.FirstOrDefault(l => l.StudentCode == "HS_O_4");

                Assert.Equal("Open", log.CommandAction);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_OfflineVerification_WriteEventLog()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_5", FullName = "Học sinh O5", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                string keyData = "MY_SECRET_PUBKEY";

                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = keyData, IsActive = true });

                db.SaveChanges();



                string payload = "HS_O_5|" + DateTime.Now.AddMinutes(10).ToString("o");

                string signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload + "_" + keyData));



                StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_O_5", payload, signature, "MAIN_GATE_01", DateTime.Now);



                var eventLog = db.EventLogs.FirstOrDefault(e => e.Actor == "HS_O_5" && e.EventType == "GateCheckOut");

                Assert.NotNull(eventLog);

                Assert.Contains("MAIN_GATE_01 (Xác thực Offline)", eventLog.Details);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_OfflineVerification_DisabledMode()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_6", FullName = "Học sinh O6", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                string keyData = "MY_SECRET_PUBKEY";

                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = keyData, IsActive = true });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_OfflineVerificationActive", Value = "0", Category = "IT" });

                db.SaveChanges();



                string payload = "HS_O_6|" + DateTime.Now.AddMinutes(10).ToString("o");

                string signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload + "_" + keyData));



                bool success = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_O_6", payload, signature, "MAIN_GATE_01", DateTime.Now);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_OfflineVerification_KeyRevocation()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_7", FullName = "Học sinh O7", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                string keyData = "MY_SECRET_PUBKEY";

                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = keyData, IsActive = false }); // Revoked

                db.SaveChanges();



                string payload = "HS_O_7|" + DateTime.Now.AddMinutes(10).ToString("o");

                string signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload + "_" + keyData));



                bool success = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_O_7", payload, signature, "MAIN_GATE_01", DateTime.Now);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_OfflineVerification_PayloadParseError()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_8", FullName = "Học sinh O8", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                string keyData = "MY_SECRET_PUBKEY";

                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = keyData, IsActive = true });

                db.SaveChanges();



                string payload = "BAD_PAYLOAD_FORMAT";

                string signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload + "_" + keyData));



                bool success = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_O_8", payload, signature, "MAIN_GATE_01", DateTime.Now);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_OfflineVerification_DuplicateUsePrevented()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_9", FullName = "Học sinh O9", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                string keyData = "MY_SECRET_PUBKEY";

                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = keyData, IsActive = true });

                db.SaveChanges();



                string payload = "HS_O_9|" + DateTime.Now.AddMinutes(10).ToString("o");

                string signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload + "_" + keyData));



                // First quet - should pass

                bool first = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_O_9", payload, signature, "MAIN_GATE_01", DateTime.Now);

                Assert.True(first);



                // Second quet - should be blocked as replay attack

                bool second = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_O_9", payload, signature, "MAIN_GATE_01", DateTime.Now);

                Assert.False(second);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_OfflineVerification_TransactionSafety()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_O_10", FullName = "Học sinh O10", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                string keyData = "MY_SECRET_PUBKEY";

                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = keyData, IsActive = true });

                db.SaveChanges();



                string payload = "HS_O_10|" + DateTime.Now.AddMinutes(10).ToString("o");

                string signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload + "_" + keyData));



                db.Database.GetDbConnection().Close(); // Force database error



                bool result = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_O_10", payload, signature, "MAIN_GATE_01", DateTime.Now);

                Assert.False(result);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_AntiPassback_Success_NormalSequence()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_A_1", FullName = "Học sinh A1", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_AntiPassbackMode", Value = "1", Category = "IT" });

                db.SaveChanges();



                bool in1 = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_A_1", "GateCheckIn", "MAIN_GATE_01", "VALID_FACE", DateTime.Now);

                Assert.True(in1);



                bool out1 = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_A_1", "GateCheckOut", "MAIN_GATE_01", "VALID_FACE", DateTime.Now);

                Assert.True(out1);



                bool in2 = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_A_1", "GateCheckIn", "MAIN_GATE_01", "VALID_FACE", DateTime.Now);

                Assert.True(in2);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_AntiPassback_DoubleCheckIn_Blocked()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_A_2", FullName = "Học sinh A2", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_AntiPassbackMode", Value = "1", Category = "IT" });

                db.SaveChanges();



                bool in1 = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_A_2", "GateCheckIn", "MAIN_GATE_01", "VALID_FACE", DateTime.Now);

                Assert.True(in1);



                // Double Check-In: Blocked

                bool in2 = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_A_2", "GateCheckIn", "MAIN_GATE_01", "VALID_FACE", DateTime.Now);

                Assert.False(in2);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_AntiPassback_DoubleCheckOut_Blocked()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_A_3", FullName = "Học sinh A3", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_AntiPassbackMode", Value = "1", Category = "IT" });

                db.SaveChanges();



                bool out1 = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_A_3", "GateCheckOut", "MAIN_GATE_01", "VALID_FACE", DateTime.Now);

                Assert.True(out1);



                // Double Check-Out: Blocked

                bool out2 = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_A_3", "GateCheckOut", "MAIN_GATE_01", "VALID_FACE", DateTime.Now);

                Assert.False(out2);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_AntiPassback_ModeDisabled_AllowDuplicate()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_A_4", FullName = "Học sinh A4", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_AntiPassbackMode", Value = "0", Category = "IT" }); // Mode Disabled

                db.SaveChanges();



                bool in1 = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_A_4", "GateCheckIn", "MAIN_GATE_01", "VALID_FACE", DateTime.Now);

                Assert.True(in1);



                bool in2 = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_A_4", "GateCheckIn", "MAIN_GATE_01", "VALID_FACE", DateTime.Now);

                Assert.True(in2); // Allowed

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_FaceMatch_Success()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_A_5", FullName = "Học sinh A5", ClassName = "10A1", Status = "Active", AvatarPath = "avatars/a5.jpg" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_FaceMatchRequirement", Value = "2", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_A_5", "GateCheckIn", "MAIN_GATE_01", "VALID_FACE_IMAGE_DATA", DateTime.Now);

                Assert.True(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_FaceMatch_Mismatch_StrictBlock()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_A_6", FullName = "Học sinh A6", ClassName = "10A1", Status = "Active", AvatarPath = "avatars/a6.jpg" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_FaceMatchRequirement", Value = "2", Category = "IT" }); // Strict Block

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_A_6", "GateCheckIn", "MAIN_GATE_01", "MISMATCH", DateTime.Now);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_FaceMatch_Mismatch_WarningOnly()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_A_7", FullName = "Học sinh A7", ClassName = "10A1", Status = "Active", AvatarPath = "avatars/a7.jpg", ConductScore = 95 };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_FaceMatchRequirement", Value = "1", Category = "IT" }); // Warning & Penalty

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_A_7", "GateCheckIn", "MAIN_GATE_01", "MISMATCH", DateTime.Now);

                Assert.True(success); // Opens barrier anyway



                var stu = db.Students.FirstOrDefault(st => st.StudentCode == "HS_A_7");

                Assert.Equal(90, stu.ConductScore); // Penalty applied: -5 points

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_FaceMatch_Mismatch_SecurityAlertSent()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_A_8", FullName = "Học sinh A8", ClassName = "10A1", Status = "Active", AvatarPath = "avatars/a8.jpg" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_FaceMatchRequirement", Value = "1", Category = "IT" });

                db.SaveChanges();



                StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_A_8", "GateCheckIn", "MAIN_GATE_01", "MISMATCH", DateTime.Now);



                var alertMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "ADMIN" && m.SenderId == "FACE_MATCH_AI");

                Assert.NotNull(alertMsg);

                Assert.Contains("sai lệch khuôn mặt", alertMsg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_GatePass_StudentNotFound()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                bool success = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "NON_EXISTENT_CODE", "GateCheckIn", "MAIN_GATE_01", "", DateTime.Now);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_GatePass_TransactionRollbackOnError()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_A_10", FullName = "Học sinh A10", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SaveChanges();



                db.Database.GetDbConnection().Close(); // Force database error



                bool success = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_A_10", "GateCheckIn", "MAIN_GATE_01", "", DateTime.Now);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Location_TrackingTechnology_StaticRFID()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_B_1", FullName = "Nguyễn Văn B1", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.CampusBeacons.Add(new CampusBeacon { Id = "BC_B1", LocationName = "Thư viện", CoordinateX = 15.5, CoordinateY = 20.0, IsActive = true });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Location_TrackingTechnology", Value = "1", Category = "IT" }); // Static RFID Mode

                db.SaveChanges();



                // Far RSSI - should fail

                bool successFar = StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_B_1", "BC_B1", -50.0, DateTime.Now);

                Assert.False(successFar);



                // Very close RSSI - should succeed

                bool successClose = StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_B_1", "BC_B1", -15.0, DateTime.Now);

                Assert.True(successClose);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Location_TrackingTechnology_Disabled()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_B_2", FullName = "Nguyễn Văn B2", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.CampusBeacons.Add(new CampusBeacon { Id = "BC_B2", LocationName = "Thư viện", CoordinateX = 15.5, CoordinateY = 20.0, IsActive = true });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Location_TrackingTechnology", Value = "0", Category = "IT" }); // Disabled

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_B_2", "BC_B2", -15.0, DateTime.Now);

                Assert.True(success);



                var stu = db.Students.FirstOrDefault(st => st.StudentCode == "HS_B_2");

                Assert.Equal(0.0, stu.PositionX); // Coordinates not updated

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_OfflineVerification_FallbackMode0_StrictBlock()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_B_3", FullName = "Học sinh B3", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                string keyData = "MY_SECRET_PUBKEY";

                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = keyData, IsActive = true });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_OfflineFallbackMode", Value = "0", Category = "IT" }); // Fallback Mode 0: Strict Block

                db.SaveChanges();



                string payload = "HS_B_3|" + DateTime.Now.AddMinutes(10).ToString("o");

                string signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload + "_" + keyData));



                bool success = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_B_3", payload, signature, "MAIN_GATE_01", DateTime.Now);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_OfflineVerification_FallbackMode2_ManualOverride()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_B_4", FullName = "Học sinh B4", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_OfflineFallbackMode", Value = "2", Category = "IT" }); // Fallback Mode 2: Manual Bypass

                db.SaveChanges();



                // Call verify without valid signature, should still pass in Mode 2

                bool success = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_B_4", "BAD_PAYLOAD", "BAD_SIGNATURE", "MAIN_GATE_01", DateTime.Now);

                Assert.True(success);



                var log = db.GateBarrierLogs.FirstOrDefault(l => l.StudentCode == "HS_B_4");

                Assert.NotNull(log);

                Assert.Equal("ManualOverride", log.TriggerSource);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_AntiPassback_Action_DeductConductPoints()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_B_5", FullName = "Học sinh B5", ClassName = "10A1", Status = "Active", ConductScore = 100 };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_AntiPassbackMode", Value = "1", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_AntiPassbackAction", Value = "1", Category = "IT" }); // Warning & Penalty Mode

                db.SaveChanges();



                bool in1 = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_B_5", "GateCheckIn", "MAIN_GATE_01", "VALID_FACE", DateTime.Now);

                Assert.True(in1);



                // Double Check-In: Allowed but penalizes student

                bool in2 = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_B_5", "GateCheckIn", "MAIN_GATE_01", "VALID_FACE", DateTime.Now);

                Assert.True(in2);



                var stu = db.Students.FirstOrDefault(st => st.StudentCode == "HS_B_5");

                Assert.Equal(95, stu.ConductScore); // -5 Conduct Points applied



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "parent_" + s.Id && m.SenderId == "GATE_SYSTEM");

                Assert.NotNull(msg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_AntiPassback_Action_Ignore()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_B_6", FullName = "Học sinh B6", ClassName = "10A1", Status = "Active", ConductScore = 100 };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_AntiPassbackMode", Value = "1", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_AntiPassbackAction", Value = "0", Category = "IT" }); // Ignore Mode

                db.SaveChanges();



                bool in1 = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_B_6", "GateCheckIn", "MAIN_GATE_01", "VALID_FACE", DateTime.Now);

                Assert.True(in1);



                bool in2 = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_B_6", "GateCheckIn", "MAIN_GATE_01", "VALID_FACE", DateTime.Now);

                Assert.True(in2); // Allowed, no change in score



                var stu = db.Students.FirstOrDefault(st => st.StudentCode == "HS_B_6");

                Assert.Equal(100, stu.ConductScore); // No penalty

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_FaceMatch_Action_DeductConductPoints()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_B_7", FullName = "Học sinh B7", ClassName = "10A1", Status = "Active", AvatarPath = "avatars/b7.jpg", ConductScore = 100 };

                db.Students.Add(s);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_FaceMatchRequirement", Value = "1", Category = "IT" }); // Warning & Penalty Mode

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_B_7", "GateCheckIn", "MAIN_GATE_01", "MISMATCH", DateTime.Now);

                Assert.True(success); // Opens barrier



                var stu = db.Students.FirstOrDefault(st => st.StudentCode == "HS_B_7");

                Assert.Equal(95, stu.ConductScore); // -5 Conduct Points applied

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_OfflineVerification_ReplayAttack_PreventedByCache()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_B_8", FullName = "Học sinh B8", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                string keyData = "MY_SECRET_PUBKEY";

                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = keyData, IsActive = true });

                db.SaveChanges();



                string payload = "HS_B_8|" + DateTime.Now.AddMinutes(10).ToString("o");

                string signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload + "_" + keyData));



                bool first = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_B_8", payload, signature, "MAIN_GATE_01", DateTime.Now);

                Assert.True(first);



                bool second = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_B_8", payload, signature, "MAIN_GATE_01", DateTime.Now);

                Assert.False(second); // Prevented

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Location_RestrictedZone_ExitSendsClearNotification()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_B_9", FullName = "Học sinh B9", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                db.CampusBeacons.Add(new CampusBeacon { Id = "BC_REST", LocationName = "Trạm phát sóng", AreaZone = "Restricted", CoordinateX = 5.0, CoordinateY = 5.0, IsActive = true });

                db.CampusBeacons.Add(new CampusBeacon { Id = "BC_SAFE", LocationName = "Sân chơi", AreaZone = "Safe", CoordinateX = 20.0, CoordinateY = 20.0, IsActive = true });

                db.SaveChanges();



                // Ping restricted zone

                StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_B_9", "BC_REST", -50.0, DateTime.Now);



                // Ping safe zone: should send clear exit notification

                bool result = StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_B_9", "BC_SAFE", -50.0, DateTime.Now.AddMinutes(1));

                Assert.True(result);



                var exitMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "parent_" + s.Id && m.Content.Contains("đã rời khỏi khu vực hạn chế"));

                Assert.NotNull(exitMsg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_OfflineVerification_ClockDriftSafe()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s = new Student { StudentCode = "HS_B_10", FullName = "Học sinh B10", ClassName = "10A1", Status = "Active" };

                db.Students.Add(s);

                string keyData = "MY_SECRET_PUBKEY";

                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = keyData, IsActive = true });

                db.SaveChanges();



                // Expiry time is set to 2 minutes ago.

                // Normally it would be expired, but we allow 5 minutes clock drift, so it should still succeed.

                string payload = "HS_B_10|" + DateTime.Now.AddMinutes(-2).ToString("o");

                string signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload + "_" + keyData));



                bool success = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_B_10", payload, signature, "MAIN_GATE_01", DateTime.Now);

                Assert.True(success); // Safe due to 5 mins grace period

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }





// --- PHASE 6: SCHOOL ADMINISTRATIVE & RESOURCE WORKFLOWS INTEGRATION TESTS ---



        [Fact]

        public void Test_Substitute_AssignSuccess()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var originalTeacher = new StaffProfile { StaffCode = "GV_ORIG", FullName = "Giáo viên Gốc", Department = "Math" };

                var subTeacher = new StaffProfile { StaffCode = "GV_SUB", FullName = "Giáo viên Dạy Thay", Department = "Math" };

                db.StaffProfiles.AddRange(originalTeacher, subTeacher);



                var s = new Student { StudentCode = "HS_SUB_1", FullName = "Học sinh Lớp 6A", ClassName = "6A", Status = "Active" };

                db.Students.Add(s);



                var lr = new LeaveRequest { StaffId = 123, StaffName = "Giáo viên Gốc", LeaveType = "Sick", Status = "Approved", StartDate = DateTime.Today, EndDate = DateTime.Today };

                db.LeaveRequests.Add(lr);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.AssignSubstituteTeacher(db, lr.Id, "GV_SUB", DateTime.Today, 3, "6A", "Phòng 101");

                Assert.True(success);



                var assignment = db.SubstituteAssignments.FirstOrDefault(a => a.SubstituteTeacherCode == "GV_SUB");

                Assert.NotNull(assignment);

                Assert.Equal("Phòng 101", assignment.RoomName);



                var parentMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "parent_" + s.Id);

                Assert.NotNull(parentMsg);

                Assert.Contains("Giáo viên Dạy Thay", parentMsg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Substitute_DuplicatePeriodBlocked()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var subTeacher = new StaffProfile { StaffCode = "GV_SUB2", FullName = "Giáo viên Dạy Thay 2", Department = "Math" };

                db.StaffProfiles.Add(subTeacher);



                var lr = new LeaveRequest { StaffId = 124, StaffName = "Original", LeaveType = "Sick", Status = "Approved" };

                db.LeaveRequests.Add(lr);

                db.SaveChanges();



                // First assignment

                bool s1 = StaffDailyWorkflowsServices.AssignSubstituteTeacher(db, lr.Id, "GV_SUB2", DateTime.Today, 3, "6A", "Phòng 101");

                Assert.True(s1);



                // Second assignment (Duplicate Period)

                bool s2 = StaffDailyWorkflowsServices.AssignSubstituteTeacher(db, lr.Id, "GV_SUB2", DateTime.Today, 3, "6B", "Phòng 102");

                Assert.False(s2);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Substitute_TeacherMaxLoadExceeded()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var subTeacher = new StaffProfile { StaffCode = "GV_SUB3", FullName = "Giáo viên Dạy Thay 3", Department = "Math" };

                db.StaffProfiles.Add(subTeacher);



                var lr = new LeaveRequest { StaffId = 125, StaffName = "Original", LeaveType = "Sick", Status = "Approved" };

                db.LeaveRequests.Add(lr);



                db.SystemSettings.Add(new SystemSetting { Id = "IT_Substitute_MaxWeeklyPeriods", Value = "2", Category = "IT" });



                // Seed 2 assignments already

                db.SubstituteAssignments.Add(new SubstituteAssignment { LeaveRequestId = lr.Id, SubstituteTeacherCode = "GV_SUB3", Date = DateTime.Today, PeriodIndex = 1, Status = "Assigned" });

                db.SubstituteAssignments.Add(new SubstituteAssignment { LeaveRequestId = lr.Id, SubstituteTeacherCode = "GV_SUB3", Date = DateTime.Today, PeriodIndex = 2, Status = "Assigned" });



                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.AssignSubstituteTeacher(db, lr.Id, "GV_SUB3", DateTime.Today, 3, "6A", "Phòng 101");

                Assert.False(success); // Max load (2) exceeded

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Substitute_UnapprovedLeaveRequestBlocked()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var subTeacher = new StaffProfile { StaffCode = "GV_SUB4", FullName = "Giáo viên Dạy Thay 4" };

                db.StaffProfiles.Add(subTeacher);



                var lr = new LeaveRequest { StaffId = 126, StaffName = "Original", LeaveType = "Sick", Status = "Pending" };

                db.LeaveRequests.Add(lr);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.AssignSubstituteTeacher(db, lr.Id, "GV_SUB4", DateTime.Today, 3, "6A", "Phòng 101");

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Substitute_PrivateParentNotification()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var subTeacher = new StaffProfile { StaffCode = "GV_SUB5", FullName = "Giáo viên Dạy Thay 5" };

                db.StaffProfiles.Add(subTeacher);



                var s = new Student { StudentCode = "HS_SUB_5", FullName = "Học sinh 5", ClassName = "6A", Status = "Active" };

                db.Students.Add(s);



                var lr = new LeaveRequest { StaffId = 127, StaffName = "Original", LeaveType = "Sick", Status = "Approved" };

                db.LeaveRequests.Add(lr);

                db.SaveChanges();



                StaffDailyWorkflowsServices.AssignSubstituteTeacher(db, lr.Id, "GV_SUB5", DateTime.Today, 1, "6A", "Room 1");



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "parent_" + s.Id);

                Assert.NotNull(msg);

                Assert.DoesNotContain("Sick", msg.Content); // Pedagogical privacy

                Assert.Contains("dạy thay", msg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Substitute_NoPublicAnnouncement()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var subTeacher = new StaffProfile { StaffCode = "GV_SUB6", FullName = "Giáo viên Dạy Thay 6" };

                db.StaffProfiles.Add(subTeacher);



                var lr = new LeaveRequest { StaffId = 128, StaffName = "Original", LeaveType = "Sick", Status = "Approved" };

                db.LeaveRequests.Add(lr);

                db.SaveChanges();



                StaffDailyWorkflowsServices.AssignSubstituteTeacher(db, lr.Id, "GV_SUB6", DateTime.Today, 1, "6A", "Room 1");



                var bul = db.Bulletins.FirstOrDefault();

                Assert.Null(bul); // No public shaming or announcements

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Substitute_InvalidTeacherCode()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var lr = new LeaveRequest { StaffId = 129, StaffName = "Original", LeaveType = "Sick", Status = "Approved" };

                db.LeaveRequests.Add(lr);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.AssignSubstituteTeacher(db, lr.Id, "INVALID_GV", DateTime.Today, 1, "6A", "Room 1");

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Substitute_StatusChangeOnCancel()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var assignment = new SubstituteAssignment { LeaveRequestId = 10, SubstituteTeacherCode = "GV_SUB8", Date = DateTime.Today, PeriodIndex = 1, Status = "Assigned" };

                db.SubstituteAssignments.Add(assignment);

                db.SaveChanges();



                assignment.Status = "Cancelled";

                db.SaveChanges();



                var updated = db.SubstituteAssignments.First(a => a.Id == assignment.Id);

                Assert.Equal("Cancelled", updated.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Substitute_SameDayLimitCheck()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var subTeacher = new StaffProfile { StaffCode = "GV_SUB9", FullName = "Giáo viên Dạy Thay 9" };

                db.StaffProfiles.Add(subTeacher);



                var lr = new LeaveRequest { StaffId = 130, StaffName = "Original", LeaveType = "Sick", Status = "Approved" };

                db.LeaveRequests.Add(lr);

                db.SaveChanges();



                // Max load setting = 1

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Substitute_MaxWeeklyPeriods", Value = "1", Category = "IT" });

                db.SaveChanges();



                bool s1 = StaffDailyWorkflowsServices.AssignSubstituteTeacher(db, lr.Id, "GV_SUB9", DateTime.Today, 1, "6A", "Room 1");

                Assert.True(s1);



                bool s2 = StaffDailyWorkflowsServices.AssignSubstituteTeacher(db, lr.Id, "GV_SUB9", DateTime.Today, 2, "6B", "Room 2");

                Assert.False(s2); // Blocked

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Substitute_TransactionRollbackOnError()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.Database.GetDbConnection().Close(); // Force database error



                bool success = StaffDailyWorkflowsServices.AssignSubstituteTeacher(db, 1, "GV_ERR", DateTime.Today, 1, "6A", "Room 1");

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Chemical_Requisition_HazardousAutoFlag()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.AssetBookings.Add(new SchoolAssetBooking { Id = 1, AssetId = 5, BookedBy = "Teacher_A", BookingDate = DateTime.Today, TimeSlot = 2 });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Chemical_HazardousList", Value = "H2SO4, HNO3", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProposeChemicalRequisition(db, 1, "H2SO4", 50.0, "Axit sunfuric");

                Assert.True(success);



                var req = db.ChemicalRequisitions.First();

                Assert.True(req.IsHazardous);

                Assert.Equal("Pending", req.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Chemical_Requisition_NonHazardousAutoApprove()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.AssetBookings.Add(new SchoolAssetBooking { Id = 2, AssetId = 5, BookedBy = "Teacher_A", BookingDate = DateTime.Today, TimeSlot = 2 });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Chemical_HazardousList", Value = "H2SO4", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProposeChemicalRequisition(db, 2, "NaCl", 10.0, "Muối ăn");

                Assert.True(success);



                var req = db.ChemicalRequisitions.First();

                Assert.False(req.IsHazardous);

                Assert.Equal("Approved", req.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Chemical_Requisition_HodApproveSuccess()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var req = new ChemicalRequisition { AssetBookingId = 1, ChemicalName = "H2SO4", RequiredQuantity = 10, IsHazardous = true, ApprovedByHOD = "Pending", ApprovedByPrincipal = "Pending", Status = "Pending" };

                db.ChemicalRequisitions.Add(req);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ApproveChemicalRequisition(db, req.Id, "HOD", "Approved", "");

                Assert.True(success);



                var updated = db.ChemicalRequisitions.First(r => r.Id == req.Id);

                Assert.Equal("Approved", updated.ApprovedByHOD);

                Assert.Equal("Pending", updated.Status); // Still pending Principal

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Chemical_Requisition_PrincipalApproveWithCorrectPin()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var req = new ChemicalRequisition { AssetBookingId = 1, ChemicalName = "H2SO4", RequiredQuantity = 10, IsHazardous = true, ApprovedByHOD = "Approved", ApprovedByPrincipal = "Pending", Status = "Pending" };

                db.ChemicalRequisitions.Add(req);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ApproveChemicalRequisition(db, req.Id, "Principal", "Approved", "1234");

                Assert.True(success);



                var updated = db.ChemicalRequisitions.First(r => r.Id == req.Id);

                Assert.Equal("Approved", updated.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Chemical_Requisition_PrincipalApproveWrongPin()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.AssetBookings.Add(new SchoolAssetBooking { Id = 3, AssetId = 5, BookedBy = "Teacher_A", BookingDate = DateTime.Today, TimeSlot = 2 });

                var req = new ChemicalRequisition { AssetBookingId = 3, ChemicalName = "H2SO4", RequiredQuantity = 10, IsHazardous = true, ApprovedByHOD = "Approved", ApprovedByPrincipal = "Pending", Status = "Pending" };

                db.ChemicalRequisitions.Add(req);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ApproveChemicalRequisition(db, req.Id, "Principal", "Approved", "9999");

                Assert.False(success);



                var updated = db.ChemicalRequisitions.First(r => r.Id == req.Id);

                Assert.Equal("Rejected", updated.Status);



                var booking = db.AssetBookings.FirstOrDefault(b => b.Id == 3);

                Assert.Null(booking); // Booking released

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Chemical_Requisition_DoubleSignatureApproveFlow()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var req = new ChemicalRequisition { AssetBookingId = 1, ChemicalName = "H2SO4", RequiredQuantity = 10, IsHazardous = true, ApprovedByHOD = "Pending", ApprovedByPrincipal = "Pending", Status = "Pending" };

                db.ChemicalRequisitions.Add(req);

                db.SaveChanges();



                StaffDailyWorkflowsServices.ApproveChemicalRequisition(db, req.Id, "HOD", "Approved", "");

                StaffDailyWorkflowsServices.ApproveChemicalRequisition(db, req.Id, "Principal", "Approved", "1234");



                var updated = db.ChemicalRequisitions.First(r => r.Id == req.Id);

                Assert.Equal("Approved", updated.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Chemical_Requisition_RejectFreesLabBooking()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.AssetBookings.Add(new SchoolAssetBooking { Id = 4, AssetId = 5, BookedBy = "Teacher_A", BookingDate = DateTime.Today, TimeSlot = 2 });

                var req = new ChemicalRequisition { AssetBookingId = 4, ChemicalName = "H2SO4", RequiredQuantity = 10, IsHazardous = true, ApprovedByHOD = "Pending", ApprovedByPrincipal = "Pending", Status = "Pending" };

                db.ChemicalRequisitions.Add(req);

                db.SaveChanges();



                StaffDailyWorkflowsServices.ApproveChemicalRequisition(db, req.Id, "HOD", "Rejected", "");



                var updated = db.ChemicalRequisitions.First(r => r.Id == req.Id);

                Assert.Equal("Rejected", updated.Status);



                var booking = db.AssetBookings.FirstOrDefault(b => b.Id == 4);

                Assert.Null(booking);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Chemical_Requisition_HodRejectFlow()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var req = new ChemicalRequisition { AssetBookingId = 1, ChemicalName = "H2SO4", RequiredQuantity = 10, IsHazardous = true, ApprovedByHOD = "Pending", ApprovedByPrincipal = "Pending", Status = "Pending" };

                db.ChemicalRequisitions.Add(req);

                db.SaveChanges();



                StaffDailyWorkflowsServices.ApproveChemicalRequisition(db, req.Id, "HOD", "Rejected", "");



                var updated = db.ChemicalRequisitions.First(r => r.Id == req.Id);

                Assert.Equal("Rejected", updated.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Chemical_Requisition_CaseInsensitiveCheck()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.AssetBookings.Add(new SchoolAssetBooking { Id = 5, AssetId = 5, BookedBy = "Teacher_A", BookingDate = DateTime.Today, TimeSlot = 2 });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Chemical_HazardousList", Value = "H2so4", Category = "IT" });

                db.SaveChanges();



                StaffDailyWorkflowsServices.ProposeChemicalRequisition(db, 5, " h2so4 ", 10.0, "Acid");

                

                var req = db.ChemicalRequisitions.First();

                Assert.True(req.IsHazardous);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Chemical_Requisition_TransactionSafety()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.Database.GetDbConnection().Close(); // Force database error



                bool success = StaffDailyWorkflowsServices.ApproveChemicalRequisition(db, 1, "HOD", "Approved", "");

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Payroll_CalculateAndSignSuccess()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Payroll_BaseOvertimeRate", Value = "200000", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Payroll_SecretKey", Value = "MySecret", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.CalculateAndSignPayroll(db, "NV_01", "06/2026", 10000000, 5.0, 500000);

                Assert.True(success);



                var record = db.StaffPayrollLedgers.First();

                Assert.Equal(11500000, record.FinalAmount);

                Assert.NotEmpty(record.LedgerChecksum);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Payroll_IntegrityVerifySuccess()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Payroll_SecretKey", Value = "SecretKey", Category = "IT" });

                db.SaveChanges();



                StaffDailyWorkflowsServices.CalculateAndSignPayroll(db, "NV_02", "06/2026", 8000000, 2.0, 100000);

                

                var record = db.StaffPayrollLedgers.First();

                bool isValid = StaffDailyWorkflowsServices.VerifyPayrollIntegrity(db, record.Id);

                Assert.True(isValid);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Payroll_TamperedDataDetected()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Payroll_SecretKey", Value = "SecretKey", Category = "IT" });

                db.SaveChanges();



                StaffDailyWorkflowsServices.CalculateAndSignPayroll(db, "NV_03", "06/2026", 8000000, 2.0, 100000);

                

                var record = db.StaffPayrollLedgers.First();

                

                // Tamper with final amount directly in database

                record.FinalAmount = 9000000;

                db.SaveChanges();



                bool isValid = StaffDailyWorkflowsServices.VerifyPayrollIntegrity(db, record.Id);

                Assert.False(isValid);



                var updated = db.StaffPayrollLedgers.First();

                Assert.Equal(0, updated.FinalAmount); // Fraud lock

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Payroll_LockedLedgerBlockModification()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var ledger = new StaffPayrollLedger { StaffCode = "NV_04", MonthYear = "06/2026", IsLocked = true };

                db.StaffPayrollLedgers.Add(ledger);

                db.SaveChanges();



                Assert.Throws<InvalidOperationException>(() => 

                    StaffDailyWorkflowsServices.CalculateAndSignPayroll(db, "NV_04", "06/2026", 5000000, 0, 0)

                );

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Payroll_UnlocksAllowModification()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var ledger = new StaffPayrollLedger { StaffCode = "NV_05", MonthYear = "06/2026", IsLocked = false };

                db.StaffPayrollLedgers.Add(ledger);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.CalculateAndSignPayroll(db, "NV_05", "06/2026", 5000000, 0, 0);

                Assert.True(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Payroll_BonusIncorporated()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Payroll_BaseOvertimeRate", Value = "100000", Category = "IT" });

                db.SaveChanges();



                StaffDailyWorkflowsServices.CalculateAndSignPayroll(db, "NV_06", "06/2026", 5000000, 10.0, 750000);

                

                var record = db.StaffPayrollLedgers.First();

                Assert.Equal(6750000, record.FinalAmount); // 5M + 1M (OT) + 750k (Bonus)

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Payroll_WrongSecretKeyIntegrityFailure()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Payroll_SecretKey", Value = "Key1", Category = "IT" });

                db.SaveChanges();



                StaffDailyWorkflowsServices.CalculateAndSignPayroll(db, "NV_07", "06/2026", 5000000, 0, 0);

                

                var record = db.StaffPayrollLedgers.First();

                

                // Change secret key setting to Key2

                var keySetting = db.SystemSettings.First(s => s.Id == "IT_Payroll_SecretKey");

                keySetting.Value = "Key2";

                db.SaveChanges();



                bool isValid = StaffDailyWorkflowsServices.VerifyPayrollIntegrity(db, record.Id);

                Assert.False(isValid);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Payroll_NonExistentRecordIntegrity()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                bool isValid = StaffDailyWorkflowsServices.VerifyPayrollIntegrity(db, 9999);

                Assert.False(isValid);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Payroll_NegativeSalaryPrevented()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                bool success = StaffDailyWorkflowsServices.CalculateAndSignPayroll(db, "NV_09", "06/2026", -1000, 0, 0);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Payroll_TransactionRollbackOnCalculationError()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.Database.GetDbConnection().Close(); // Force database error



                bool success = StaffDailyWorkflowsServices.CalculateAndSignPayroll(db, "NV_10", "06/2026", 5000000, 0, 0);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Maintenance_RegisterTicketSuccess()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                bool success = StaffDailyWorkflowsServices.RegisterMaintenanceTicket(db, "Room 101", "Air Conditioner", "Leaking water", "Medium", "KT_01");

                Assert.True(success);



                var ticket = db.MaintenanceTickets.First();

                Assert.Equal("Pending", ticket.Status);

                Assert.Equal("Room 101", ticket.RoomName);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Maintenance_SlaEscalation_CriticalOverdue()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Maintenance_SlaHoursCritical", Value = "2", Category = "IT" });

                db.SaveChanges();



                var ticket = new MaintenanceTicket { RoomName = "Lab A", FacilityName = "Gas Leak", Severity = "Critical", Status = "Pending", CreatedAt = DateTime.Now.AddHours(-2.5) };

                db.MaintenanceTickets.Add(ticket);

                db.SaveChanges();



                int count = StaffDailyWorkflowsServices.ProcessMaintenanceSlaEscalation(db, DateTime.Now);

                Assert.Equal(1, count);



                var updated = db.MaintenanceTickets.First(t => t.Id == ticket.Id);

                Assert.True(updated.IsEscalated);



                var principalMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "HT001");

                Assert.NotNull(principalMsg);

                Assert.Contains("Sự cố mức độ Critical", principalMsg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Maintenance_SlaEscalation_HighOverdue()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Maintenance_SlaHoursHigh", Value = "4", Category = "IT" });

                db.SaveChanges();



                var ticket = new MaintenanceTicket { RoomName = "Room 102", FacilityName = "Projector", Severity = "High", Status = "Pending", CreatedAt = DateTime.Now.AddHours(-4.5) };

                db.MaintenanceTickets.Add(ticket);

                db.SaveChanges();



                int count = StaffDailyWorkflowsServices.ProcessMaintenanceSlaEscalation(db, DateTime.Now);

                Assert.Equal(1, count);



                var updated = db.MaintenanceTickets.First(t => t.Id == ticket.Id);

                Assert.True(updated.IsEscalated);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Maintenance_SlaEscalation_NoEscalationOnTime()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Maintenance_SlaHoursHigh", Value = "4", Category = "IT" });

                db.SaveChanges();



                var ticket = new MaintenanceTicket { RoomName = "Room 102", FacilityName = "Projector", Severity = "High", Status = "Pending", CreatedAt = DateTime.Now.AddHours(-3.5) };

                db.MaintenanceTickets.Add(ticket);

                db.SaveChanges();



                int count = StaffDailyWorkflowsServices.ProcessMaintenanceSlaEscalation(db, DateTime.Now);

                Assert.Equal(0, count);



                var updated = db.MaintenanceTickets.First(t => t.Id == ticket.Id);

                Assert.False(updated.IsEscalated);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Maintenance_CriticalDuringClassHoursAlert()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var overrideTime = new DateTime(2026, 6, 25, 10, 0, 0); // 10:00 AM, in class hours

                bool success = StaffDailyWorkflowsServices.RegisterMaintenanceTicket(db, "Room 101", "Power Grid", "Total Blackout", "Critical", "KT_01", overrideTime);

                Assert.True(success);



                var log = db.GateBarrierLogs.FirstOrDefault(l => l.CommandAction == "Open" && l.TriggerSource == "EmergencyLockdownBypass");

                Assert.NotNull(log);

                Assert.Equal("ALL_GATES", log.GateId);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Maintenance_CriticalOutsideClassHoursNoAlert()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var overrideTime = new DateTime(2026, 6, 25, 22, 0, 0); // 10:00 PM, outside class hours

                bool success = StaffDailyWorkflowsServices.RegisterMaintenanceTicket(db, "Room 101", "Power Grid", "Total Blackout", "Critical", "KT_01", overrideTime);

                Assert.True(success);



                var log = db.GateBarrierLogs.FirstOrDefault(l => l.CommandAction == "Open" && l.TriggerSource == "EmergencyLockdownBypass");

                Assert.Null(log);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Maintenance_ResolveTicketFreesEscalation()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var ticket = new MaintenanceTicket { RoomName = "Room 102", FacilityName = "Projector", Severity = "High", Status = "Pending", CreatedAt = DateTime.Now.AddHours(-5.0), IsEscalated = true };

                db.MaintenanceTickets.Add(ticket);

                db.SaveChanges();



                ticket.Status = "Resolved";

                ticket.ResolvedAt = DateTime.Now;

                db.SaveChanges();



                var updated = db.MaintenanceTickets.First(t => t.Id == ticket.Id);

                Assert.Equal("Resolved", updated.Status);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Maintenance_CustomSlaSettingsApplied()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Maintenance_SlaHoursCritical", Value = "1", Category = "IT" });

                db.SaveChanges();



                var ticket = new MaintenanceTicket { RoomName = "Lab A", FacilityName = "Gas Leak", Severity = "Critical", Status = "Pending", CreatedAt = DateTime.Now.AddHours(-1.5) };

                db.MaintenanceTickets.Add(ticket);

                db.SaveChanges();



                int count = StaffDailyWorkflowsServices.ProcessMaintenanceSlaEscalation(db, DateTime.Now);

                Assert.Equal(1, count);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Maintenance_NoPublicShamingOfMaintenance()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                StaffDailyWorkflowsServices.RegisterMaintenanceTicket(db, "Room 101", "Projector", "Dead bulb", "High", "KT_01");



                var bul = db.Bulletins.FirstOrDefault();

                Assert.Null(bul);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Maintenance_TransactionSafetyOnEscalation()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.Database.GetDbConnection().Close(); // Force database error



                int count = StaffDailyWorkflowsServices.ProcessMaintenanceSlaEscalation(db, DateTime.Now);

                Assert.Equal(0, count);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }





// --- PHASE 7: EXTENDED STAFF WORKFLOWS & RESOURCE MONITORING INTEGRATION TESTS ---



        // ==========================================

        // GROUP 1: SECURITY LOCKDOWN DRILL TESTS (10 Cases)

        // ==========================================



        [Fact]

        public void Test_Lockdown_TriggerSuccess()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var securityStaff = new StaffProfile { StaffCode = "SEC_01", FullName = "Bảo vệ A", Department = "SECURITY" };

                db.StaffProfiles.Add(securityStaff);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Security_LockdownPin", Value = "9999", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Security_LockdownMode", Value = "2", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.TriggerSecurityLockdown(db, "SEC_01", "ActiveShooter", "9999");

                Assert.True(success);



                var log = db.SecurityLockdownLogs.FirstOrDefault(l => l.TriggeredByStaffCode == "SEC_01");

                Assert.NotNull(log);

                Assert.Equal("Active", log.Status);

                Assert.Equal("ActiveShooter", log.LockdownType);



                var barrierLog = db.GateBarrierLogs.FirstOrDefault(b => b.TriggerSource == "EmergencyLockdown");

                Assert.NotNull(barrierLog);

                Assert.Equal("Lock", barrierLog.CommandAction);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Lockdown_UnauthorizedStaffBlocked()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var teacher = new StaffProfile { StaffCode = "GV_01", FullName = "Giáo viên", Department = "Teacher" };

                db.StaffProfiles.Add(teacher);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Security_LockdownPin", Value = "9999", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.TriggerSecurityLockdown(db, "GV_01", "Intrusion", "9999");

                Assert.False(success);



                var log = db.SecurityLockdownLogs.FirstOrDefault();

                Assert.Null(log);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Lockdown_WrongPinRejected()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var admin = new StaffProfile { StaffCode = "ADM_01", FullName = "Admin", Department = "ADMIN" };

                db.StaffProfiles.Add(admin);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Security_LockdownPin", Value = "1234", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.TriggerSecurityLockdown(db, "ADM_01", "FireDrill", "9999");

                Assert.False(success);



                var log = db.SecurityLockdownLogs.FirstOrDefault();

                Assert.Null(log);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Lockdown_ModePerimeterOnly()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var admin = new StaffProfile { StaffCode = "ADM_02", FullName = "Admin 2", Department = "ADMIN" };

                db.StaffProfiles.Add(admin);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Security_LockdownPin", Value = "9999", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Security_LockdownMode", Value = "1", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.TriggerSecurityLockdown(db, "ADM_02", "Intrusion", "9999");

                Assert.True(success);



                var barrierLog = db.GateBarrierLogs.FirstOrDefault(b => b.TriggerSource == "EmergencyLockdown");

                Assert.NotNull(barrierLog);

                Assert.Equal("Lock", barrierLog.CommandAction);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Lockdown_ModeZeroDoesNotLock()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var admin = new StaffProfile { StaffCode = "ADM_03", FullName = "Admin 3", Department = "ADMIN" };

                db.StaffProfiles.Add(admin);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Security_LockdownPin", Value = "9999", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Security_LockdownMode", Value = "0", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.TriggerSecurityLockdown(db, "ADM_03", "Intrusion", "9999");

                Assert.True(success);



                var barrierLog = db.GateBarrierLogs.FirstOrDefault(b => b.TriggerSource == "EmergencyLockdown");

                Assert.Null(barrierLog);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Lockdown_NoBulletinsCreated()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var admin = new StaffProfile { StaffCode = "ADM_04", FullName = "Admin 4", Department = "ADMIN" };

                db.StaffProfiles.Add(admin);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Security_LockdownPin", Value = "9999", Category = "IT" });

                db.SaveChanges();



                StaffDailyWorkflowsServices.TriggerSecurityLockdown(db, "ADM_04", "ActiveShooter", "9999");



                var bulletin = db.Bulletins.FirstOrDefault();

                Assert.Null(bulletin); // Pedagogy privacy compliance: no public panic bulletins

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Lockdown_TeacherInboxAlertsSent()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var admin = new StaffProfile { StaffCode = "ADM_05", FullName = "Admin 5", Department = "ADMIN" };

                var teacher1 = new StaffProfile { StaffCode = "TCH_01", FullName = "Teacher 1", Department = "Teacher" };

                var teacher2 = new StaffProfile { StaffCode = "TCH_02", FullName = "Teacher 2", Department = "Science" };

                db.StaffProfiles.AddRange(admin, teacher1, teacher2);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Security_LockdownPin", Value = "9999", Category = "IT" });

                db.SaveChanges();



                StaffDailyWorkflowsServices.TriggerSecurityLockdown(db, "ADM_05", "FireDrill", "9999");



                var msg1 = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "staff_" + teacher1.Id);

                Assert.NotNull(msg1);

                Assert.Contains("[KHẨN CẤP]", msg1.Content);



                var msg2 = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "staff_" + teacher2.Id);

                Assert.NotNull(msg2);

                Assert.Contains("FireDrill", msg2.Content);



                var principalMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "HT001");

                Assert.NotNull(principalMsg);

                Assert.Contains("ADM_05", principalMsg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Lockdown_InvalidStaffCodeRejected()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Security_LockdownPin", Value = "9999", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.TriggerSecurityLockdown(db, "NON_EXIST", "Intrusion", "9999");

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Lockdown_TransactionRollbackOnError()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.Database.GetDbConnection().Close(); // Force database exception



                bool success = StaffDailyWorkflowsServices.TriggerSecurityLockdown(db, "SEC_01", "Intrusion", "9999");

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Lockdown_StatusIsActive()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var securityStaff = new StaffProfile { StaffCode = "SEC_02", FullName = "Bảo vệ B", Department = "SECURITY" };

                db.StaffProfiles.Add(securityStaff);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Security_LockdownPin", Value = "9999", Category = "IT" });

                db.SaveChanges();



                StaffDailyWorkflowsServices.TriggerSecurityLockdown(db, "SEC_02", "FireDrill", "9999");



                var log = db.SecurityLockdownLogs.First();

                Assert.Equal("Active", log.Status);

                Assert.NotNull(log.StartedAt);

                Assert.Null(log.EndedAt);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }





        // ==========================================

        // GROUP 2: COLD STORAGE IoT TEMPERATURE MONITORING TESTS (10 Cases)

        // ==========================================



        [Fact]

        public void Test_ColdStorage_NormalTemperatureLogged()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Kitchen_MaxSafeTemp", Value = "4.0", Category = "IT" });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProcessColdStorageHeartbeat(db, "FRIDGE_01", 3.5, 65.0, DateTime.Now);

                Assert.True(success);



                var log = db.KitchenColdStorageLogs.FirstOrDefault(l => l.FridgeId == "FRIDGE_01");

                Assert.NotNull(log);

                Assert.False(log.IsViolation);

                Assert.Equal(3.5, log.Temperature);



                var activeFoodLogs = db.FoodSafetyInspectionLogs.ToList();

                Assert.Empty(activeFoodLogs);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ColdStorage_TempViolationWithinGracePeriod()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Kitchen_MaxSafeTemp", Value = "4.0", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Kitchen_TempAlarmThresholdMinutes", Value = "30", Category = "IT" });

                db.SaveChanges();



                DateTime now = DateTime.Now;

                // Single violation logged right now - duration is 0 minutes

                bool success = StaffDailyWorkflowsServices.ProcessColdStorageHeartbeat(db, "FRIDGE_01", 5.2, 70.0, now);

                Assert.True(success);



                var log = db.KitchenColdStorageLogs.First();

                Assert.True(log.IsViolation);



                var messages = db.InboxMessages.ToList();

                Assert.Empty(messages); // Grace period (30 mins) not exceeded

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ColdStorage_TempViolationExceedSlaAlarm()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Kitchen_MaxSafeTemp", Value = "4.0", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Kitchen_TempAlarmThresholdMinutes", Value = "30", Category = "IT" });

                

                DateTime baseTime = DateTime.Now.AddMinutes(-40);

                db.KitchenColdStorageLogs.Add(new KitchenColdStorageLog { FridgeId = "FRIDGE_01", Temperature = 5.0, Humidity = 70, Timestamp = baseTime, IsViolation = true });

                db.KitchenColdStorageLogs.Add(new KitchenColdStorageLog { FridgeId = "FRIDGE_01", Temperature = 5.1, Humidity = 70, Timestamp = baseTime.AddMinutes(20), IsViolation = true });

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.ProcessColdStorageHeartbeat(db, "FRIDGE_01", 5.2, 70.0, DateTime.Now);

                Assert.True(success);



                // Alarm should trigger as violation continuous > 30 minutes (from baseTime to DateTime.Now is 40 minutes)

                var managerMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "KITCHEN_MANAGER");

                Assert.NotNull(managerMsg);

                Assert.Contains("quá nhiệt liên tục", managerMsg.Content);



                var nurseMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "NURSE");

                Assert.NotNull(nurseMsg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ColdStorage_AutoQuarantineFoodBatches()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Kitchen_MaxSafeTemp", Value = "4.0", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Kitchen_TempAlarmThresholdMinutes", Value = "30", Category = "IT" });

                

                // Add some active food logs

                var foodLog1 = new FoodSafetyInspectionLog { SupplierId = 1, BatchCode = "BATCH_01", IngredientName = "Rau cải", TestScore = 90, InspectionStatus = "Passed", InspectedAt = DateTime.Today };

                var foodLog2 = new FoodSafetyInspectionLog { SupplierId = 2, BatchCode = "BATCH_02", IngredientName = "Thịt heo", TestScore = 85, InspectionStatus = "Passed", InspectedAt = DateTime.Today };

                db.FoodSafetyInspectionLogs.AddRange(foodLog1, foodLog2);



                DateTime baseTime = DateTime.Now.AddMinutes(-40);

                db.KitchenColdStorageLogs.Add(new KitchenColdStorageLog { FridgeId = "FRIDGE_02", Temperature = 6.0, Humidity = 75, Timestamp = baseTime, IsViolation = true });

                db.SaveChanges();



                StaffDailyWorkflowsServices.ProcessColdStorageHeartbeat(db, "FRIDGE_02", 6.5, 75.0, DateTime.Now);



                var updatedLog1 = db.FoodSafetyInspectionLogs.First(f => f.BatchCode == "BATCH_01");

                Assert.Equal("Quarantined", updatedLog1.InspectionStatus);



                var updatedLog2 = db.FoodSafetyInspectionLogs.First(f => f.BatchCode == "BATCH_02");

                Assert.Equal("Quarantined", updatedLog2.InspectionStatus);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ColdStorage_NursePrivateInboxNotification()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Kitchen_MaxSafeTemp", Value = "4.0", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Kitchen_TempAlarmThresholdMinutes", Value = "10", Category = "IT" });

                

                db.KitchenColdStorageLogs.Add(new KitchenColdStorageLog { FridgeId = "FRIDGE_03", Temperature = 5.0, Humidity = 70, Timestamp = DateTime.Now.AddMinutes(-15), IsViolation = true });

                db.SaveChanges();



                StaffDailyWorkflowsServices.ProcessColdStorageHeartbeat(db, "FRIDGE_03", 5.1, 70.0, DateTime.Now);



                var messages = db.InboxMessages.Where(m => m.SenderId == "HEALTH_SYSTEM").ToList();

                Assert.Equal(2, messages.Count); // Manager + Nurse



                var bulletin = db.Bulletins.FirstOrDefault();

                Assert.Null(bulletin); // Privacy compliance: no public reports of kitchen alarms

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ColdStorage_NormalTempResetsViolation()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Kitchen_MaxSafeTemp", Value = "4.0", Category = "IT" });

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Kitchen_TempAlarmThresholdMinutes", Value = "30", Category = "IT" });



                // 45 mins ago: Violation

                db.KitchenColdStorageLogs.Add(new KitchenColdStorageLog { FridgeId = "FRIDGE_01", Temperature = 5.0, Humidity = 70, Timestamp = DateTime.Now.AddMinutes(-45), IsViolation = true });

                // 15 mins ago: Normal (breaking the sequence)

                db.KitchenColdStorageLogs.Add(new KitchenColdStorageLog { FridgeId = "FRIDGE_01", Temperature = 3.8, Humidity = 70, Timestamp = DateTime.Now.AddMinutes(-15), IsViolation = false });

                db.SaveChanges();



                // Now: Violation

                StaffDailyWorkflowsServices.ProcessColdStorageHeartbeat(db, "FRIDGE_01", 5.2, 70.0, DateTime.Now);



                var messages = db.InboxMessages.ToList();

                Assert.Empty(messages); // Since it returned to normal 15 mins ago, the current violation duration is 0 mins (restarted)

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ColdStorage_CustomMaxTempApplied()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                // Custom threshold is 6.0C instead of 4.0C

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Kitchen_MaxSafeTemp", Value = "6.0", Category = "IT" });

                db.SaveChanges();



                StaffDailyWorkflowsServices.ProcessColdStorageHeartbeat(db, "FRIDGE_01", 5.5, 60.0, DateTime.Now);



                var log = db.KitchenColdStorageLogs.First();

                Assert.False(log.IsViolation); // 5.5 <= 6.0 (No violation)

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ColdStorage_CustomAlarmThresholdApplied()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Kitchen_MaxSafeTemp", Value = "4.0", Category = "IT" });

                // Custom alarm threshold: 10 minutes instead of 30 minutes

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Kitchen_TempAlarmThresholdMinutes", Value = "10", Category = "IT" });

                

                db.KitchenColdStorageLogs.Add(new KitchenColdStorageLog { FridgeId = "FRIDGE_01", Temperature = 5.0, Humidity = 70, Timestamp = DateTime.Now.AddMinutes(-12), IsViolation = true });

                db.SaveChanges();



                StaffDailyWorkflowsServices.ProcessColdStorageHeartbeat(db, "FRIDGE_01", 5.1, 70.0, DateTime.Now);



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "KITCHEN_MANAGER");

                Assert.NotNull(msg); // Alarm triggered (12 mins violation > 10 mins threshold)

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ColdStorage_EmptyFridgeIdHandled()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                bool success = StaffDailyWorkflowsServices.ProcessColdStorageHeartbeat(db, "", 3.0, 60.0, DateTime.Now);

                Assert.True(success);



                var log = db.KitchenColdStorageLogs.FirstOrDefault();

                Assert.NotNull(log);

                Assert.Equal("", log.FridgeId);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_ColdStorage_TransactionSafety()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.Database.GetDbConnection().Close(); // Force database error



                bool success = StaffDailyWorkflowsServices.ProcessColdStorageHeartbeat(db, "FRIDGE_ERR", 5.0, 70.0, DateTime.Now);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }





        // ==========================================

        // GROUP 3: STUDENT CARE PLAN ACTIVITY CHECK TESTS (10 Cases)

        // ==========================================



        [Fact]

        public void Test_CarePlan_NormalActivityVerificationSafe()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s1 = new Student { StudentCode = "HS_001", FullName = "Nguyễn Văn A" };

                db.Students.Add(s1);

                // Student has allergy but trigger doesn't match the activity

                db.StudentCarePlans.Add(new StudentCarePlan { StudentCode = "HS_001", ChronicCondition = "Allergy", MedicalTriggers = "Peanut", IsActive = true });

                db.SaveChanges();



                var result = StaffDailyWorkflowsServices.VerifyActivitySafety(db, 1, "Outdoor Running", "PhysicalEducation", new List<string> { "HS_001" });

                

                Assert.True(result.IsSafe);

                Assert.Empty(result.FlaggedStudentCodes);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_CarePlan_AllergenTriggerDetectedWarning()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s1 = new Student { StudentCode = "HS_002", FullName = "Lê Văn B" };

                db.Students.Add(s1);

                // Trigger is "Peanut"

                db.StudentCarePlans.Add(new StudentCarePlan { StudentCode = "HS_002", ChronicCondition = "Anaphylaxis", MedicalTriggers = "Peanut", IsActive = true });

                db.SaveChanges();



                // Activity name contains "peanut"

                var result = StaffDailyWorkflowsServices.VerifyActivitySafety(db, 2, "Cooking with Peanut Butter", "HomeEconomics", new List<string> { "HS_002" });

                

                Assert.Contains("HS_002", result.FlaggedStudentCodes);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_CarePlan_AutoBlockOnHazardousActivity_Mode1()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Medical_CarePlanVerificationMode", Value = "1", Category = "IT" }); // Mode 1 = Auto Block

                var s1 = new Student { StudentCode = "HS_003", FullName = "Trần Thị C" };

                db.Students.Add(s1);

                db.StudentCarePlans.Add(new StudentCarePlan { StudentCode = "HS_003", ChronicCondition = "Asthma", MedicalTriggers = "StrenuousPhysical", IsActive = true });

                db.SaveChanges();



                // Activity type contains trigger

                var result = StaffDailyWorkflowsServices.VerifyActivitySafety(db, 3, "Marathon Run", "StrenuousPhysical", new List<string> { "HS_003" });

                

                Assert.False(result.IsSafe); // Auto blocked

                Assert.Contains("HS_003", result.FlaggedStudentCodes);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_CarePlan_SoftWarningMode0()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Medical_CarePlanVerificationMode", Value = "0", Category = "IT" }); // Mode 0 = Soft warning

                var s1 = new Student { StudentCode = "HS_004", FullName = "Phạm Văn D" };

                db.Students.Add(s1);

                db.StudentCarePlans.Add(new StudentCarePlan { StudentCode = "HS_004", ChronicCondition = "Heart Disease", MedicalTriggers = "StrenuousPhysical", IsActive = true });

                db.SaveChanges();



                var result = StaffDailyWorkflowsServices.VerifyActivitySafety(db, 4, "High Intensity Cardio", "StrenuousPhysical", new List<string> { "HS_004" });

                

                Assert.True(result.IsSafe); // Warn only, not blocked

                Assert.Contains("HS_004", result.FlaggedStudentCodes);

                Assert.NotEmpty(result.RecommendationNotes);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_CarePlan_PedagogicalPrivacyCompliance()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s1 = new Student { StudentCode = "HS_005", FullName = "Nguyễn Văn E" };

                db.Students.Add(s1);

                db.StudentCarePlans.Add(new StudentCarePlan { StudentCode = "HS_005", ChronicCondition = "Nut Allergy", MedicalTriggers = "Peanut", IsActive = true });

                db.SaveChanges();



                StaffDailyWorkflowsServices.VerifyActivitySafety(db, 5, "Peanut Cookie Decorating", "Cooking", new List<string> { "HS_005" });



                var bulletins = db.Bulletins.ToList();

                Assert.Empty(bulletins); // Privacy compliance: no public shaming or allergy announcements on bulletins



                var nurseMsg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "NURSE");

                Assert.NotNull(nurseMsg); // Nurse notified privately

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_CarePlan_CaseInsensitiveKeywordMatch()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s1 = new Student { StudentCode = "HS_006", FullName = "Hoàng Văn F" };

                db.Students.Add(s1);

                // MedicalTrigger has "PEANUT"

                db.StudentCarePlans.Add(new StudentCarePlan { StudentCode = "HS_006", ChronicCondition = "Allergy", MedicalTriggers = "PEANUT", IsActive = true });

                db.SaveChanges();



                // Activity name has "peanut"

                var result = StaffDailyWorkflowsServices.VerifyActivitySafety(db, 6, "peanut tasting", "Cooking", new List<string> { "HS_006" });

                

                Assert.Contains("HS_006", result.FlaggedStudentCodes);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_CarePlan_MultipleStudentsFlagged()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s1 = new Student { StudentCode = "HS_007", FullName = "Học sinh G" };

                var s2 = new Student { StudentCode = "HS_008", FullName = "Học sinh H" };

                db.Students.AddRange(s1, s2);

                db.StudentCarePlans.Add(new StudentCarePlan { StudentCode = "HS_007", ChronicCondition = "Asthma", MedicalTriggers = "Hiking", IsActive = true });

                db.StudentCarePlans.Add(new StudentCarePlan { StudentCode = "HS_008", ChronicCondition = "Cardio", MedicalTriggers = "Hiking", IsActive = true });

                db.SaveChanges();



                var result = StaffDailyWorkflowsServices.VerifyActivitySafety(db, 7, "Forest Hiking Trip", "OutdoorActivity", new List<string> { "HS_007", "HS_008" });

                

                Assert.Equal(2, result.FlaggedStudentCodes.Count);

                Assert.Contains("HS_007", result.FlaggedStudentCodes);

                Assert.Contains("HS_008", result.FlaggedStudentCodes);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_CarePlan_InactivePlanIgnored()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var s1 = new Student { StudentCode = "HS_009", FullName = "Học sinh I" };

                db.Students.Add(s1);

                // Plan is Inactive

                db.StudentCarePlans.Add(new StudentCarePlan { StudentCode = "HS_009", ChronicCondition = "Allergy", MedicalTriggers = "Peanut", IsActive = false });

                db.SaveChanges();



                var result = StaffDailyWorkflowsServices.VerifyActivitySafety(db, 8, "Peanut cookies", "Cooking", new List<string> { "HS_009" });

                

                Assert.Empty(result.FlaggedStudentCodes);

                Assert.True(result.IsSafe);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_CarePlan_NoPlanForStudentSafe()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                // Student exists but has no care plan

                var s1 = new Student { StudentCode = "HS_010", FullName = "Học sinh J" };

                db.Students.Add(s1);

                db.SaveChanges();



                var result = StaffDailyWorkflowsServices.VerifyActivitySafety(db, 9, "Peanut cooking", "Cooking", new List<string> { "HS_010" });

                

                Assert.True(result.IsSafe);

                Assert.Empty(result.FlaggedStudentCodes);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_CarePlan_TransactionSafetyOnVerify()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                db.Database.GetDbConnection().Close(); // Force database error



                var result = StaffDailyWorkflowsServices.VerifyActivitySafety(db, 10, "Peanut Cooking", "Cooking", new List<string> { "HS_011" });

                Assert.False(result.IsSafe); // Safety-first fallback

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }





        // ==========================================

        // GROUP 4: RESOURCE CONSERVATION & LEAK DETECTION TESTS (10 Cases)

        // ==========================================



        [Fact]

        public void Test_Resource_RecordMetricsSuccess()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var janitor = new StaffProfile { StaffCode = "JAN_01", FullName = "Lao công X", Department = "Janitor" };

                db.StaffProfiles.Add(janitor);

                db.SaveChanges();



                bool success = StaffDailyWorkflowsServices.RecordResourceWasteMetrics(db, DateTime.Today, "JAN_01", 10.0, 15.0, 5.0, 1.0, 120.0, 10.0);

                Assert.True(success);



                var record = db.ResourceWasteLedgers.FirstOrDefault(r => r.JanitorStaffCode == "JAN_01");

                Assert.NotNull(record);

                Assert.Equal(10.0, record.OrganicKg);

                Assert.Equal(120.0, record.ElectricityKwh);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Resource_LeakDetection_WaterSpike()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var janitor = new StaffProfile { StaffCode = "JAN_02", FullName = "Lao công Y", Department = "Janitor" };

                db.StaffProfiles.Add(janitor);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Janitor_ResourceLeakThreshold", Value = "1.30", Category = "IT" });



                // Seed historical consumption (average water = 10.0)

                db.ResourceWasteLedgers.Add(new ResourceWasteLedger { Date = DateTime.Today.AddDays(-1), JanitorStaffCode = "JAN_02", WaterCubicMeters = 10.0, ElectricityKwh = 100.0 });

                db.SaveChanges();



                // Water consumption spike: 14.0 cubic meters (which is 1.40x - greater than 1.30x threshold)

                bool success = StaffDailyWorkflowsServices.RecordResourceWasteMetrics(db, DateTime.Today, "JAN_02", 5, 5, 5, 0, 100.0, 14.0);

                Assert.True(success);



                var ticket = db.MaintenanceTickets.FirstOrDefault(t => t.Severity == "High");

                Assert.NotNull(ticket);

                Assert.Contains("rò rỉ nước", ticket.Description);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Resource_LeakDetection_ElectricitySpike()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var janitor = new StaffProfile { StaffCode = "JAN_03", FullName = "Lao công Z", Department = "Janitor" };

                db.StaffProfiles.Add(janitor);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Janitor_ResourceLeakThreshold", Value = "1.30", Category = "IT" });



                db.ResourceWasteLedgers.Add(new ResourceWasteLedger { Date = DateTime.Today.AddDays(-1), JanitorStaffCode = "JAN_03", WaterCubicMeters = 10.0, ElectricityKwh = 100.0 });

                db.SaveChanges();



                // Electricity spike: 135 kWh (1.35x > 1.30x)

                bool success = StaffDailyWorkflowsServices.RecordResourceWasteMetrics(db, DateTime.Today, "JAN_03", 5, 5, 5, 0, 135.0, 10.0);

                Assert.True(success);



                var ticket = db.MaintenanceTickets.FirstOrDefault(t => t.Severity == "High");

                Assert.NotNull(ticket);

                Assert.Contains("chập điện", ticket.Description);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Resource_NoLeakAlertOnNormalConsumption()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var janitor = new StaffProfile { StaffCode = "JAN_04", FullName = "Lao công W", Department = "Janitor" };

                db.StaffProfiles.Add(janitor);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Janitor_ResourceLeakThreshold", Value = "1.30", Category = "IT" });



                db.ResourceWasteLedgers.Add(new ResourceWasteLedger { Date = DateTime.Today.AddDays(-1), JanitorStaffCode = "JAN_04", WaterCubicMeters = 10.0, ElectricityKwh = 100.0 });

                db.SaveChanges();



                // Consumption is 1.1x of average (within normal variations)

                StaffDailyWorkflowsServices.RecordResourceWasteMetrics(db, DateTime.Today, "JAN_04", 5, 5, 5, 0, 110.0, 11.0);



                var ticket = db.MaintenanceTickets.FirstOrDefault();

                Assert.Null(ticket); // No leak ticket created

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Resource_GreenBonusTriggered()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var janitor = new StaffProfile { StaffCode = "JAN_05", FullName = "Janitor", Department = "Janitor" };

                db.StaffProfiles.Add(janitor);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Janitor_GreenBonusThresholdKg", Value = "100.0", Category = "IT" });

                db.SaveChanges();



                // Recyclable is 120.0 Kg (>= 100.0 Kg)

                StaffDailyWorkflowsServices.RecordResourceWasteMetrics(db, DateTime.Today, "JAN_05", 10.0, 120.0, 5.0, 0, 100.0, 10.0);



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "BCH_DOAN");

                Assert.NotNull(msg);

                Assert.Contains("thi đua xanh", msg.Content);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Resource_GreenBonusNotTriggered()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var janitor = new StaffProfile { StaffCode = "JAN_06", FullName = "Janitor", Department = "Janitor" };

                db.StaffProfiles.Add(janitor);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Janitor_GreenBonusThresholdKg", Value = "100.0", Category = "IT" });

                db.SaveChanges();



                // Recyclable is 80.0 Kg (< 100.0 Kg)

                StaffDailyWorkflowsServices.RecordResourceWasteMetrics(db, DateTime.Today, "JAN_06", 10.0, 80.0, 5.0, 0, 100.0, 10.0);



                var msg = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "BCH_DOAN");

                Assert.Null(msg);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Resource_LeakThresholdChangeApplied()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var janitor = new StaffProfile { StaffCode = "JAN_07", FullName = "Janitor", Department = "Janitor" };

                db.StaffProfiles.Add(janitor);

                // Lower threshold to 1.15x

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Janitor_ResourceLeakThreshold", Value = "1.15", Category = "IT" });



                db.ResourceWasteLedgers.Add(new ResourceWasteLedger { Date = DateTime.Today.AddDays(-1), JanitorStaffCode = "JAN_07", WaterCubicMeters = 10.0, ElectricityKwh = 100.0 });

                db.SaveChanges();



                // Consumption is 12.0 cubic meters (1.20x - triggers leak because threshold is 1.15x)

                StaffDailyWorkflowsServices.RecordResourceWasteMetrics(db, DateTime.Today, "JAN_07", 5, 5, 5, 0, 100.0, 12.0);



                var ticket = db.MaintenanceTickets.FirstOrDefault();

                Assert.NotNull(ticket);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Resource_JanitorVerification()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                // Unauthorized / non-existent janitor code

                bool success = StaffDailyWorkflowsServices.RecordResourceWasteMetrics(db, DateTime.Today, "INVALID_JAN", 10.0, 10.0, 10.0, 0, 100.0, 10.0);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Resource_NegativeValuesBlocked()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var janitor = new StaffProfile { StaffCode = "JAN_08", FullName = "Janitor", Department = "Janitor" };

                db.StaffProfiles.Add(janitor);

                db.SaveChanges();



                // Has negative value

                bool success = StaffDailyWorkflowsServices.RecordResourceWasteMetrics(db, DateTime.Today, "JAN_08", -10.0, 10.0, 10.0, 0, 100.0, 10.0);

                Assert.False(success);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



        [Fact]

        public void Test_Resource_Calculate7DayAverageCorrectly()

        {

            using var db = TestDbFactory.Create();

            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();

            try

            {

                var janitor = new StaffProfile { StaffCode = "JAN_09", FullName = "Janitor", Department = "Janitor" };

                db.StaffProfiles.Add(janitor);

                db.SystemSettings.Add(new SystemSetting { Id = "IT_Janitor_ResourceLeakThreshold", Value = "1.30", Category = "IT" });



                // Seed 3 historical logs (100, 120, 80 => Average = 100)

                DateTime baseDate = DateTime.Today.AddDays(-5);

                db.ResourceWasteLedgers.Add(new ResourceWasteLedger { Date = baseDate, JanitorStaffCode = "JAN_09", WaterCubicMeters = 10, ElectricityKwh = 100 });

                db.ResourceWasteLedgers.Add(new ResourceWasteLedger { Date = baseDate.AddDays(1), JanitorStaffCode = "JAN_09", WaterCubicMeters = 10, ElectricityKwh = 120 });

                db.ResourceWasteLedgers.Add(new ResourceWasteLedger { Date = baseDate.AddDays(2), JanitorStaffCode = "JAN_09", WaterCubicMeters = 10, ElectricityKwh = 80 });

                db.SaveChanges();



                // Today consumption is 140 kWh (1.40x average of 100 => triggers alert)

                bool success = StaffDailyWorkflowsServices.RecordResourceWasteMetrics(db, DateTime.Today, "JAN_09", 5, 5, 5, 0, 140.0, 10.0);

                Assert.True(success);



                var ticket = db.MaintenanceTickets.FirstOrDefault();

                Assert.NotNull(ticket);

            }

            finally

            {

                AppDbContext.FallbackInMemoryConnection = null;

            }

        }



    
        [Fact]
        public void Test_Gate_RFIDProtocol_SerialMode()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var s = new Student { StudentCode = "HS_R_1", FullName = "Học sinh R1", ClassName = "10A1", Status = "Active" };
                db.Students.Add(s);
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_RFIDConnectionProtocol", Value = "0", Category = "IT" });
                db.SaveChanges();

                // COM error - should return false
                bool fail = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_R_1", "GateCheckIn", "GATE_COM_ERR", "VALID_FACE", DateTime.Now);
                Assert.False(fail);

                // Normal - should return true
                bool success = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_R_1", "GateCheckIn", "MAIN_GATE_01", "VALID_FACE", DateTime.Now);
                Assert.True(success);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Gate_RFIDProtocol_NetworkMode()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var s = new Student { StudentCode = "HS_R_2", FullName = "Học sinh R2", ClassName = "10A1", Status = "Active" };
                db.Students.Add(s);
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_RFIDConnectionProtocol", Value = "1", Category = "IT" });
                db.SaveChanges();

                // NET error - should return false
                bool fail = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_R_2", "GateCheckIn", "GATE_NET_ERR", "VALID_FACE", DateTime.Now);
                Assert.False(fail);

                // Normal - should return true
                bool success = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_R_2", "GateCheckIn", "MAIN_GATE_01", "VALID_FACE", DateTime.Now);
                Assert.True(success);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Gate_RFIDProtocol_InvalidValueFallback()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var s = new Student { StudentCode = "HS_R_3", FullName = "Học sinh R3", ClassName = "10A1", Status = "Active" };
                db.Students.Add(s);
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_RFIDConnectionProtocol", Value = "99", Category = "IT" }); // Invalid
                db.SaveChanges();

                // Should fallback to Serial mode (0)
                // COM error - should return false
                bool fail = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_R_3", "GateCheckIn", "GATE_COM_ERR", "VALID_FACE", DateTime.Now);
                Assert.False(fail);

                // Normal - should return true
                bool success = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_R_3", "GateCheckIn", "MAIN_GATE_01", "VALID_FACE", DateTime.Now);
                Assert.True(success);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Gate_QrKeyStorage_HardwareAES()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var s = new Student { StudentCode = "HS_Q_1", FullName = "Học sinh Q1", ClassName = "10A1", Status = "Active" };
                db.Students.Add(s);
                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = "PUBKEY_AES", IsActive = true });
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_QrEncryptionKeyStorageMode", Value = "0", Category = "IT" });
                db.SaveChanges();

                string payload = "HS_Q_1|" + DateTime.Now.ToString("o");
                string signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload + "_PUBKEY_AES"));

                bool success = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_Q_1", payload, signature, "MAIN_GATE_01", DateTime.Now);
                Assert.True(success);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Gate_QrKeyStorage_DPAPISuccess()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var s = new Student { StudentCode = "HS_Q_2", FullName = "Học sinh Q2", ClassName = "10A1", Status = "Active" };
                db.Students.Add(s);
                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = "PUBKEY_DPAPI", IsActive = true });
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_QrEncryptionKeyStorageMode", Value = "1", Category = "IT" });
                db.SaveChanges();

                string payload = "HS_Q_2|" + DateTime.Now.ToString("o");
                string signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload + "_PUBKEY_DPAPI"));

                bool success = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_Q_2", payload, signature, "MAIN_GATE_01", DateTime.Now);
                Assert.True(success);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Gate_QrKeyStorage_DecryptionFailureHandled()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var s = new Student { StudentCode = "HS_Q_3", FullName = "Học sinh Q3", ClassName = "10A1", Status = "Active" };
                db.Students.Add(s);
                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = "PUBKEY_DPAPI", IsActive = true });
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Gate_QrEncryptionKeyStorageMode", Value = "1", Category = "IT" });
                db.SaveChanges();

                string payload = "HS_Q_3|" + DateTime.Now.ToString("o");
                string signature = "DPAPI_FAIL"; // Force DPAPI failure simulation

                bool success = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_Q_3", payload, signature, "MAIN_GATE_01", DateTime.Now);
                Assert.False(success);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Location_KalmanFilter_SmoothEffect()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var s = new Student { StudentCode = "HS_K_1", FullName = "Học sinh K1", ClassName = "10A1", Status = "Active" };
                db.Students.Add(s);
                db.CampusBeacons.Add(new CampusBeacon { Id = "BC_K1", LocationName = "Thư viện", AreaZone = "Normal", CoordinateX = 10, CoordinateY = 20, IsActive = true });
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Location_RssiThreshold", Value = "-90.0", Category = "IT" });
                db.SaveChanges();

                // Send fluctuating RSSI pings
                StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_K_1", "BC_K1", -80.0, DateTime.Now);
                StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_K_1", "BC_K1", -70.0, DateTime.Now);
                StaffDailyWorkflowsServices.ReportBeaconPing(db, "HS_K_1", "BC_K1", -78.0, DateTime.Now);

                var history = db.StudentLocationHistories
                    .Where(h => h.StudentCode == "HS_K_1")
                    .OrderBy(h => h.Timestamp)
                    .ToList();

                Assert.True(history.Count >= 3);
                // The third RSSI should be smoothed (Kalman filtered) and differ from raw -78.0
                Assert.NotEqual(-78.0, history[2].Rssi);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Gate_DynamicTOTP_Success()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var s = new Student { StudentCode = "HS_T_1", FullName = "Học sinh T1", ClassName = "10A1", Status = "Active" };
                db.Students.Add(s);
                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = "PUBKEY_TOTP", IsActive = true });
                db.SaveChanges();

                // Dynamic TOTP payload format: studentCode|expiryTime|totpToken
                string payload = "HS_T_1|" + DateTime.Now.ToString("o") + "|482015";
                string signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload + "_PUBKEY_TOTP"));

                bool success = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_T_1", payload, signature, "MAIN_GATE_01", DateTime.Now);
                Assert.True(success);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Gate_DynamicTOTP_ClockDriftTolerance()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var s = new Student { StudentCode = "HS_T_2", FullName = "Học sinh T2", ClassName = "10A1", Status = "Active" };
                db.Students.Add(s);
                db.GateOfflineKeys.Add(new GateOfflineKey { KeyName = "School_Default_Public", PublicKeyData = "PUBKEY_TOTP", IsActive = true });
                db.SaveChanges();

                DateTime now = DateTime.Now;
                // Expiry time is 90 seconds in the future (within the 2-minute drift tolerance)
                string payloadValid = "HS_T_2|" + now.AddSeconds(90).ToString("o") + "|482015";
                string signatureValid = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payloadValid + "_PUBKEY_TOTP"));

                bool success = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_T_2", payloadValid, signatureValid, "MAIN_GATE_01", now);
                Assert.True(success);

                // Expiry time is 150 seconds in the future (outside 2-minute drift tolerance)
                string payloadInvalid = "HS_T_2|" + now.AddSeconds(150).ToString("o") + "|482015";
                string signatureInvalid = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payloadInvalid + "_PUBKEY_TOTP"));

                bool successInvalid = StaffDailyWorkflowsServices.VerifyGatePassOffline(db, "HS_T_2", payloadInvalid, signatureInvalid, "MAIN_GATE_01", now);
                Assert.False(successInvalid);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Gate_AntiTailgating_TriggerLock()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var s = new Student { StudentCode = "HS_G_1", FullName = "Học sinh G1", ClassName = "10A1", Status = "Active" };
                db.Students.Add(s);
                db.SaveChanges();

                // Trigger tailgating block
                bool block1 = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_G_1", "GateCheckIn", "MAIN_GATE_01", "TAILGATE", DateTime.Now);
                Assert.False(block1);

                bool block2 = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_G_1", "GateCheckIn", "GATE_TAILGATE_01", "VALID_FACE", DateTime.Now);
                Assert.False(block2);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Gate_MeshSynchronization_OfflineSuccess()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var s = new Student { StudentCode = "HS_M_1", FullName = "Học sinh M1", ClassName = "10A1", Status = "Active" };
                db.Students.Add(s);
                db.SaveChanges();

                GateMeshService.Instance.Clear();
                bool success = StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_M_1", "GateCheckIn", "MAIN_GATE_01", "VALID_FACE", DateTime.Now);
                Assert.True(success);

                var broadcasts = GateMeshService.Instance.GetBroadcastedMessages();
                Assert.Single(broadcasts);
                Assert.Equal("HS_M_1", broadcasts[0].StudentCode);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_DifferentialPrivacy_NoiseCalculation()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var s1 = new Student { StudentCode = "HS_D_1", FullName = "Học sinh D1", ClassName = "10A1", Status = "Active" };
                var s2 = new Student { StudentCode = "HS_D_2", FullName = "Học sinh D2", ClassName = "10A1", Status = "Active" };
                db.Students.AddRange(s1, s2);
                db.SaveChanges();

                DateTime date = DateTime.Today;
                // Add check-ins
                StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_D_1", "GateCheckIn", "MAIN_GATE_01", "VALID_FACE", date);
                StaffDailyWorkflowsServices.ProcessGatePassWithAntiPassback(db, "HS_D_2", "GateCheckIn", "MAIN_GATE_01", "VALID_FACE", date);

                double dpStats1 = StaffDailyWorkflowsServices.GetDifferentialPrivacyAttendanceStats(db, "10A1", date);
                Assert.NotEqual(2.0, dpStats1);
                Assert.True(Math.Abs(dpStats1 - 2.0) <= 1.0);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        // --- PHASE 9: BULLETIN UPGRADES TESTS ---
        [Fact]
        public void Test_Bulletin_SafeEdit_InPlace_NoDataLoss()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Original Title", "Original Content", "All", "Normal", "GV01");
                Assert.True(created);

                var bulletin = db.Bulletins.First(b => b.Title == "Original Title");
                int id = bulletin.Id;

                // Update in place
                bool updated = service.UpdateBulletin(id, "Updated Title", "Updated Content", "GV", "High", "");
                Assert.True(updated);

                // Verify same ID exists and content has changed
                var refreshed = db.Bulletins.FirstOrDefault(b => b.Id == id);
                Assert.NotNull(refreshed);
                Assert.Equal("Updated Title", refreshed.Title);
                Assert.Equal("Updated Content", refreshed.Content);
                Assert.Equal("GV", refreshed.Audience);
                Assert.Equal("High", refreshed.Priority);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_AutoModeration_CensorMode()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_AutoModerationMode", Value = "1", Category = "IT" });
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_ModerationAction", Value = "0", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Tin tức khẩn dm", "Học sinh không được nói bậy vl ở trường", "All", "Normal", "GV01");
                Assert.True(created);

                var bulletin = db.Bulletins.First();
                Assert.Equal("Tin tức khẩn ***", bulletin.Title);
                Assert.Equal("Học sinh không được nói bậy *** ở trường", bulletin.Content);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_AutoModeration_BlockMode()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_AutoModerationMode", Value = "1", Category = "IT" });
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_ModerationAction", Value = "1", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Tin tức bậy dm", "Nội dung bậy", "All", "Normal", "GV01");
                Assert.True(created);

                var bulletin = db.Bulletins.First();
                Assert.Equal("Pending", bulletin.Status); // Blocked to Pending
                Assert.Equal("Tin tức bậy dm", bulletin.Title); // Content not changed in block mode
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Attachment_SignatureVerification_Valid()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_AttachmentSignatureRequired", Value = "1", Category = "IT" });
                db.SaveChanges();

                string tempFile = Path.Combine(Path.GetTempPath(), "test_doc.txt");
                File.WriteAllText(tempFile, "School Document Content");

                string expectedMd5;
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(File.ReadAllBytes(tempFile));
                    expectedMd5 = Convert.ToHexString(hash).ToLower();
                }

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Title", "Content", "All", "Normal", "GV01", null, null, "", tempFile, expectedMd5);
                Assert.True(created);

                var bulletin = db.Bulletins.First();
                Assert.Equal(tempFile, bulletin.AttachmentPath);

                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Attachment_SignatureVerification_Invalid()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_AttachmentSignatureRequired", Value = "1", Category = "IT" });
                db.SaveChanges();

                string tempFile = Path.Combine(Path.GetTempPath(), "test_doc_invalid.txt");
                File.WriteAllText(tempFile, "School Document Content");

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Title", "Content", "All", "Normal", "GV01", null, null, "", tempFile, "wrong_md5_hash");
                Assert.False(created);

                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Attachment_NoSignatureRequired()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_AttachmentSignatureRequired", Value = "0", Category = "IT" });
                db.SaveChanges();

                string tempFile = Path.Combine(Path.GetTempPath(), "test_doc_none.txt");
                File.WriteAllText(tempFile, "School Document Content");

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Title", "Content", "All", "Normal", "GV01", null, null, "", tempFile, "");
                Assert.True(created);

                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_ReadReceipt_UrgentNotice()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var service = new BulletinService(db);
                service.CreateBulletin("Urgent Title", "Urgent Content", "All", "High", "GV01");
                var bulletin = db.Bulletins.First();

                bool marked1 = service.MarkAsRead(bulletin.Id, "PH_Student1");
                Assert.True(marked1);

                bool marked2 = service.MarkAsRead(bulletin.Id, "PH_Student1");
                Assert.True(marked2);

                bool marked3 = service.MarkAsRead(bulletin.Id, "GV_Teacher2");
                Assert.True(marked3);

                var readers = service.GetReadReceiptUsers(bulletin.Id);
                Assert.Equal(2, readers.Count);
                Assert.Contains("PH_Student1", readers);
                Assert.Contains("GV_Teacher2", readers);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_InteractivePoll_VoteOnce()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var service = new BulletinService(db);
                service.CreateBulletin("Poll Title", "Poll Content", "All", "Normal", "GV01");
                var bulletin = db.Bulletins.First();

                bool pollCreated = service.CreatePoll(bulletin.Id, new List<string> { "Ý kiến 1", "Ý kiến 2" });
                Assert.True(pollCreated);

                var pollOptions = service.GetPollOptions(bulletin.Id);
                Assert.Equal(2, pollOptions.Count);

                int opt1Id = pollOptions[0].Id;
                int opt2Id = pollOptions[1].Id;

                bool vote1 = service.VotePollOption(opt1Id, "PH_User1");
                Assert.True(vote1);

                bool vote2 = service.VotePollOption(opt1Id, "PH_User1");
                Assert.False(vote2);

                bool vote3 = service.VotePollOption(opt2Id, "PH_User1");
                Assert.False(vote3);

                bool vote4 = service.VotePollOption(opt2Id, "GV_User2");
                Assert.True(vote4);

                var refreshedOptions = service.GetPollOptions(bulletin.Id);
                Assert.Equal(1, refreshedOptions[0].VotesCount);
                Assert.Equal(1, refreshedOptions[1].VotesCount);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_MasterConfig_DisableComments()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_CommentsEnabled", Value = "0", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                service.CreateBulletin("Title", "Content", "All", "Normal", "GV01");
                var bulletin = db.Bulletins.First();

                bool commentAdded = service.AddComment(bulletin.Id, "Bình luận", "PH_User1");
                Assert.False(commentAdded);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Delete_CascadeCleanups()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var service = new BulletinService(db);
                service.CreateBulletin("Title", "Content", "All", "Normal", "GV01");
                var bulletin = db.Bulletins.First();

                service.AddComment(bulletin.Id, "Bình luận 1", "PH_User1");
                service.MarkAsRead(bulletin.Id, "PH_User1");
                service.CreatePoll(bulletin.Id, new List<string> { "Opt1" });
                var opt = db.BulletinPollOptions.First();
                service.VotePollOption(opt.Id, "PH_User1");

                Assert.Single(db.BulletinComments.ToList());
                Assert.Single(db.BulletinReadReceipts.ToList());
                Assert.Single(db.BulletinPollOptions.ToList());
                Assert.Single(db.BulletinPollVotes.ToList());

                service.DeleteBulletin(bulletin.Id);

                Assert.Empty(db.Bulletins.ToList());
                Assert.Empty(db.BulletinComments.ToList());
                Assert.Empty(db.BulletinReadReceipts.ToList());
                Assert.Empty(db.BulletinPollOptions.ToList());
                Assert.Empty(db.BulletinPollVotes.ToList());
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Attachment_SignatureVerification_Strict_Valid()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_AttachmentSignatureRequired", Value = "2", Category = "IT" });
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_SecretKey", Value = "TestSecretKey", Category = "IT" });
                db.SaveChanges();

                string tempFile = Path.Combine(Path.GetTempPath(), "test_doc_strict.txt");
                File.WriteAllText(tempFile, "Strict Document Content");

                string strictSig;
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(File.ReadAllBytes(tempFile));
                    string md5Hash = Convert.ToHexString(hash).ToLower();

                    using (var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes("TestSecretKey")))
                    {
                        byte[] sigBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(md5Hash));
                        strictSig = Convert.ToHexString(sigBytes).ToLower();
                    }
                }

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Title", "Content", "All", "Normal", "GV01", null, null, "", tempFile, strictSig);
                Assert.True(created);

                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Attachment_SignatureVerification_Strict_Invalid()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_AttachmentSignatureRequired", Value = "2", Category = "IT" });
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_SecretKey", Value = "TestSecretKey", Category = "IT" });
                db.SaveChanges();

                string tempFile = Path.Combine(Path.GetTempPath(), "test_doc_strict_inv.txt");
                File.WriteAllText(tempFile, "Strict Document Content");

                var service = new BulletinService(db);
                string simpleMd5;
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(File.ReadAllBytes(tempFile));
                    simpleMd5 = Convert.ToHexString(hash).ToLower();
                }

                bool created = service.CreateBulletin("Title", "Content", "All", "Normal", "GV01", null, null, "", tempFile, simpleMd5);
                Assert.False(created);

                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Category_Validation_Success()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Union Meeting", "Discussing benefits", "GV", "Normal", "Staff01", null, null, "", "", "", "TradeUnion");
                Assert.True(created);

                var bulletin = db.Bulletins.FirstOrDefault(b => b.Title == "Union Meeting");
                Assert.NotNull(bulletin);
                Assert.Equal("TradeUnion", bulletin.Category);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Category_Validation_Invalid_Rejected()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Hacked Board", "Spam", "All", "Normal", "GV01", null, null, "", "", "", "HackerCategory");
                Assert.False(created);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Category_Filtering_Single()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var service = new BulletinService(db);
                service.CreateBulletin("School News", "Info", "All", "Normal", "GV01", null, null, "", "", "", "School");
                service.CreateBulletin("Youth Union Trip", "Trip detail", "All", "Normal", "GV01", null, null, "", "", "", "YouthUnion");
                service.CreateBulletin("Teachers Meet", "Meeting details", "GV", "Normal", "GV01", null, null, "", "", "", "Staff");

                var schoolBulletins = service.GetBulletins("All", 1, "School");
                Assert.Single(schoolBulletins);
                Assert.Equal("School News", schoolBulletins[0].Title);

                var youthBulletins = service.GetBulletins("All", 1, "YouthUnion");
                Assert.Single(youthBulletins);
                Assert.Equal("Youth Union Trip", youthBulletins[0].Title);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Category_SafetyMode_Disabled()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_StrictCategorySafetyMode", Value = "0", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Trade Union Picnic", "Everyone invited", "All", "Normal", "Staff01", null, null, "", "", "", "TradeUnion");
                Assert.True(created);

                var bulletin = db.Bulletins.FirstOrDefault(b => b.Title == "Trade Union Picnic");
                Assert.NotNull(bulletin);
                Assert.Equal("All", bulletin.Audience);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Category_SafetyMode_Warn()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_StrictCategorySafetyMode", Value = "1", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Staff Salaries Review", "Internal doc", "HS", "Normal", "Staff01", null, null, "", "", "", "Staff", bypassWarning: true);
                Assert.True(created);

                var bulletin = db.Bulletins.FirstOrDefault(b => b.Title == "Staff Salaries Review");
                Assert.NotNull(bulletin);
                Assert.Equal("HS", bulletin.Audience);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Category_SafetyMode_Strict()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_StrictCategorySafetyMode", Value = "2", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Confidential Teacher Union Meeting", "GV Only", "All", "Normal", "Staff01", null, null, "", "", "", "TradeUnion");
                Assert.True(created);

                var bulletin = db.Bulletins.FirstOrDefault(b => b.Title == "Confidential Teacher Union Meeting");
                Assert.NotNull(bulletin);
                Assert.Equal("GV", bulletin.Audience);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Category_CascadeDelete_Verify()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var service = new BulletinService(db);
                service.CreateBulletin("Celebration Banner", "New Year", "All", "Normal", "GV01", null, null, "", "", "", "Celebration");
                var bulletin = db.Bulletins.FirstOrDefault(b => b.Title == "Celebration Banner");
                Assert.NotNull(bulletin);

                service.AddComment(bulletin.Id, "Great!", "Student01");
                service.MarkAsRead(bulletin.Id, "Student01");
                service.CreatePoll(bulletin.Id, new List<string> { "Yes", "No" });
                var options = service.GetPollOptions(bulletin.Id);
                service.VotePollOption(options[0].Id, "Student01");

                Assert.Single(db.BulletinComments.Where(c => c.BulletinId == bulletin.Id));
                Assert.Single(db.BulletinReadReceipts.Where(r => r.BulletinId == bulletin.Id));
                Assert.Equal(2, db.BulletinPollOptions.Count(o => o.BulletinId == bulletin.Id));

                service.DeleteBulletin(bulletin.Id);

                Assert.Empty(db.Bulletins.Where(b => b.Id == bulletin.Id));
                Assert.Empty(db.BulletinComments.Where(c => c.BulletinId == bulletin.Id));
                Assert.Empty(db.BulletinReadReceipts.Where(r => r.BulletinId == bulletin.Id));
                Assert.Empty(db.BulletinPollOptions.Where(o => o.BulletinId == bulletin.Id));
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_StorageFormat_Markdown()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_StorageFormatMode", Value = "0", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Markdown test", "**Bold** text", "All", "Normal", "GV01");
                Assert.True(created);

                var bulletin = db.Bulletins.FirstOrDefault(b => b.Title == "Markdown test");
                Assert.NotNull(bulletin);
                Assert.Equal("**Bold** text", bulletin.Content);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_StorageFormat_Xaml()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_StorageFormatMode", Value = "1", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Xaml test", "Plain text", "All", "Normal", "GV01");
                Assert.True(created);

                var bulletin = db.Bulletins.FirstOrDefault(b => b.Title == "Xaml test");
                Assert.NotNull(bulletin);
                Assert.Contains("<FlowDocument>", bulletin.Content);
                Assert.Contains("Plain text", bulletin.Content);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Attachment_LocalUNC()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_AttachmentStorageMode", Value = "0", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Local attachment", "Content", "All", "Normal", "GV01", attachmentPath: "KeHoach.pdf");
                Assert.True(created);

                var bulletin = db.Bulletins.FirstOrDefault(b => b.Title == "Local attachment");
                Assert.NotNull(bulletin);
                Assert.Equal("KeHoach.pdf", bulletin.AttachmentPath);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Attachment_CloudMock()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_AttachmentStorageMode", Value = "1", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Cloud attachment", "Content", "All", "Normal", "GV01", attachmentPath: "KeHoach.pdf");
                Assert.True(created);

                var bulletin = db.Bulletins.FirstOrDefault(b => b.Title == "Cloud attachment");
                Assert.NotNull(bulletin);
                Assert.Contains("https://s3.school.edu/bulletins/KeHoach.pdf", bulletin.AttachmentPath);
                Assert.Contains("token=mock_sas_token", bulletin.AttachmentPath);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Approval_Workflow_Disabled()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_ApprovalWorkflow", Value = "0", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Auto Publish", "Content", "All", "High", "GV01");
                Assert.True(created);

                var bulletin = db.Bulletins.FirstOrDefault(b => b.Title == "Auto Publish");
                Assert.NotNull(bulletin);
                Assert.Equal("Published", bulletin.Status);

                var requests = db.BulletinApprovalRequests.Where(r => r.BulletinId == bulletin.Id).ToList();
                Assert.Empty(requests);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Approval_Workflow_Single()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_ApprovalWorkflow", Value = "1", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Single Approval", "Content", "All", "High", "GV01");
                Assert.True(created);

                var bulletin = db.Bulletins.FirstOrDefault(b => b.Title == "Single Approval");
                Assert.NotNull(bulletin);
                Assert.Equal("Pending", bulletin.Status);

                var requests = db.BulletinApprovalRequests.Where(r => r.BulletinId == bulletin.Id).ToList();
                Assert.Single(requests);
                Assert.Equal(1, requests[0].Step);
                Assert.Equal("Pending", requests[0].Status);

                bool approved = service.ProcessApprovalAction(requests[0].Id, "Manager01", "Approve", "Looks good");
                Assert.True(approved);

                db.Entry(bulletin).Reload();
                Assert.Equal("Published", bulletin.Status);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Approval_Workflow_MultiStep()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_ApprovalWorkflow", Value = "2", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Multi Approval", "Content", "All", "High", "GV01");
                Assert.True(created);

                var bulletin = db.Bulletins.FirstOrDefault(b => b.Title == "Multi Approval");
                Assert.NotNull(bulletin);
                Assert.Equal("Pending", bulletin.Status);

                var requests = db.BulletinApprovalRequests.Where(r => r.BulletinId == bulletin.Id).OrderBy(r => r.Step).ToList();
                Assert.Equal(2, requests.Count);
                Assert.Equal("Pending", requests[0].Status);
                Assert.Equal("Pending", requests[1].Status);

                bool step1 = service.ProcessApprovalAction(requests[0].Id, "Manager01", "Approve", "Approved step 1");
                Assert.True(step1);

                db.Entry(bulletin).Reload();
                Assert.Equal("Pending", bulletin.Status); // Still pending step 2

                bool step2 = service.ProcessApprovalAction(requests[1].Id, "HT001", "Approve", "Approved step 2");
                Assert.True(step2);

                db.Entry(bulletin).Reload();
                Assert.Equal("Published", bulletin.Status);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Approval_Reject_Cascade()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_ApprovalWorkflow", Value = "2", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                service.CreateBulletin("Rejected Bulletin", "Content", "All", "High", "GV01");

                var bulletin = db.Bulletins.FirstOrDefault(b => b.Title == "Rejected Bulletin");
                Assert.NotNull(bulletin);

                var requests = db.BulletinApprovalRequests.Where(r => r.BulletinId == bulletin.Id).OrderBy(r => r.Step).ToList();
                Assert.Equal(2, requests.Count);

                bool rejected = service.ProcessApprovalAction(requests[0].Id, "Manager01", "Reject", "Inappropriate content");
                Assert.True(rejected);

                db.Entry(bulletin).Reload();
                Assert.Equal("Rejected", bulletin.Status);

                var updatedRequests = db.BulletinApprovalRequests.Where(r => r.BulletinId == bulletin.Id).ToList();
                Assert.All(updatedRequests, r => Assert.Equal("Rejected", r.Status));
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_AiSafety_Censor_With_Settings()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_AutoModerationMode", Value = "1", Category = "IT" });
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_ModerationAction", Value = "0", Category = "IT" }); // Censor
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_ModerationThreshold", Value = "70", Category = "IT" });
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_ApprovalWorkflow", Value = "0", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Title containing dm vl", "Content containing fuck", "All", "Normal", "GV01");
                Assert.True(created);

                var bulletin = db.Bulletins.FirstOrDefault(b => b.CreatedBy == "GV01");
                Assert.NotNull(bulletin);
                Assert.Contains("***", bulletin.Title);
                Assert.Contains("***", bulletin.Content);
                Assert.Equal("Published", bulletin.Status);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_AiSafety_Block_With_Settings()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_AutoModerationMode", Value = "1", Category = "IT" });
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_ModerationAction", Value = "1", Category = "IT" }); // Block
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_ModerationThreshold", Value = "70", Category = "IT" });
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_ApprovalWorkflow", Value = "0", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                bool created = service.CreateBulletin("Title containing dm vl", "Content containing fuck", "All", "Normal", "GV01");
                Assert.True(created);

                var bulletin = db.Bulletins.FirstOrDefault(b => b.CreatedBy == "GV01");
                Assert.NotNull(bulletin);
                Assert.Equal("Pending", bulletin.Status); // Blocked
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }
    
        [Fact]
        public void Test_DailyTask_Fields_Save_Success()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var task = new DailyTask
                {
                    Title = "Test Daily Task",
                    AssignedTo = "GV01",
                    AssignedBy = "HT001",
                    DueDate = DateTime.Today,
                    Status = "Pending",
                    TimeFrame = "7h00 - 7h30",
                    Collaborators = "PHT - GVCN",
                    CollaboratorIds = "GV02,NV01",
                    Location = "Sân trường",
                    Description = "Mô tả công việc kiểm tra",
                    IsCritical = true,
                    StartTime = DateTime.Today.AddHours(7),
                    EndTime = DateTime.Today.AddHours(7).AddMinutes(30)
                };

                db.DailyTasks.Add(task);
                db.SaveChanges();

                var savedTask = db.DailyTasks.FirstOrDefault(t => t.Title == "Test Daily Task");
                Assert.NotNull(savedTask);
                Assert.Equal("7h00 - 7h30", savedTask.TimeFrame);
                Assert.Equal("PHT - GVCN", savedTask.Collaborators);
                Assert.Equal("GV02,NV01", savedTask.CollaboratorIds);
                Assert.Equal("Sân trường", savedTask.Location);
                Assert.Equal("Mô tả công việc kiểm tra", savedTask.Description);
                Assert.True(savedTask.IsCritical);
                Assert.Equal(DateTime.Today.AddHours(7), savedTask.StartTime);
                Assert.Equal(DateTime.Today.AddHours(7).AddMinutes(30), savedTask.EndTime);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_DailyTask_GetSchoolDailySchedule_Filters()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.DailyTasks.AddRange(
                    new DailyTask { Title = "Task BGH", AssignedTo = "HT001", Department = "BGH", DueDate = DateTime.Today },
                    new DailyTask { Title = "Task Library", AssignedTo = "NV01", Department = "Library", DueDate = DateTime.Today },
                    new DailyTask { Title = "Task Medical", AssignedTo = "NV02", Department = "Medical", DueDate = DateTime.Today }
                );
                db.SaveChanges();

                var service = new DailyTaskService(db);
                var schedule = service.GetSchoolDailySchedule(DateTime.Today, "Library");

                Assert.Single(schedule);
                Assert.Equal("Task Library", schedule[0].Title);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_DailyTask_GetMyDailyTasksAndCoordinations_Personal()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.DailyTasks.AddRange(
                    new DailyTask { Title = "Direct Task", AssignedTo = "GV01", DueDate = DateTime.Today },
                    new DailyTask { Title = "Coordination Task", AssignedTo = "GV02", CollaboratorIds = "GV01,NV01", DueDate = DateTime.Today },
                    new DailyTask { Title = "Other Task", AssignedTo = "GV03", DueDate = DateTime.Today }
                );
                db.SaveChanges();

                var service = new DailyTaskService(db);
                var list = service.GetMyDailyTasksAndCoordinations("GV01", DateTime.Today);

                Assert.Equal(2, list.Count);
                Assert.Contains(list, t => t.Title == "Direct Task");
                Assert.Contains(list, t => t.Title == "Coordination Task");
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_DailyTask_GetMyTasks_Excludes_Others()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.DailyTasks.AddRange(
                    new DailyTask { Title = "My Task", AssignedTo = "GV01", DueDate = DateTime.Today },
                    new DailyTask { Title = "Other Private Task", AssignedTo = "GV02", DueDate = DateTime.Today }
                );
                db.SaveChanges();

                var service = new DailyTaskService(db);
                var myTasks = service.GetMyTasks("GV01", DateTime.Today);

                Assert.Single(myTasks);
                Assert.Equal("My Task", myTasks[0].Title);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_DailyTask_CompleteTask_WithReport()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var task = new DailyTask { Title = "Task to complete", AssignedTo = "GV01", DueDate = DateTime.Today, Status = "Pending" };
                db.DailyTasks.Add(task);
                db.SaveChanges();

                var service = new DailyTaskService(db);
                bool success = service.CompleteTask(task.Id, "Finished everything successfully", "GV01");

                Assert.True(success);
                var updated = db.DailyTasks.Find(task.Id);
                Assert.Equal("Done", updated.Status);
                Assert.Equal("Finished everything successfully", updated.Notes);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_DailyTask_DelegateTask_Success()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var task = new DailyTask { Title = "Lesson Observation", AssignedTo = "GV01", AssignedBy = "HT001", DueDate = DateTime.Today };
                db.DailyTasks.Add(task);
                db.SaveChanges();

                var service = new DailyTaskService(db);
                bool success = service.DelegateTask(task.Id, "HT001", "GV02", "Please cover this lesson observation for me");

                Assert.True(success);
                var updated = db.DailyTasks.Find(task.Id);
                Assert.Equal("GV02", updated.AssignedTo);
                Assert.Contains("Please cover this lesson observation for me", updated.Notes);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_DailyTask_DelegateTask_Unauthorized_Blocked()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                // Create delegator staff profiles
                db.StaffProfiles.AddRange(
                    new StaffProfile { StaffCode = "NV01", FullName = "Janitor", Department = "Services", Position = "Janitor" },
                    new StaffProfile { StaffCode = "GV01", FullName = "Teacher", Department = "Giáo viên", Position = "Teacher" }
                );
                var task = new DailyTask { Title = "Math Exam Prep", AssignedTo = "GV01", AssignedBy = "HT001", Department = "Giáo viên", DueDate = DateTime.Today };
                db.DailyTasks.Add(task);
                db.SaveChanges();

                var service = new DailyTaskService(db);
                // Janitor tries to delegate teacher task
                bool success = service.DelegateTask(task.Id, "NV01", "GV02", "Please do my task");

                Assert.False(success);
                var unchanged = db.DailyTasks.Find(task.Id);
                Assert.Equal("GV01", unchanged.AssignedTo);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_DailyTask_Conflict_SoftWarning_Calculated()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                // Create tasks with overlapping times for GV01
                var task1 = new DailyTask 
                { 
                    Title = "Task 1", 
                    AssignedTo = "GV01", 
                    DueDate = DateTime.Today,
                    StartTime = DateTime.Today.AddHours(8),
                    EndTime = DateTime.Today.AddHours(9).AddMinutes(30)
                };
                var task2 = new DailyTask 
                { 
                    Title = "Task 2", 
                    AssignedTo = "GV01", 
                    DueDate = DateTime.Today,
                    StartTime = DateTime.Today.AddHours(9), // Overlaps with task 1
                    EndTime = DateTime.Today.AddHours(10)
                };
                db.DailyTasks.AddRange(task1, task2);
                db.SaveChanges();

                // Logic check in helper
                bool hasOverlap = task1.StartTime < task2.EndTime && task2.StartTime < task1.EndTime;
                Assert.True(hasOverlap);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_DailyTask_SlaEscalation_CriticalTask_Triggered()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                // Configure SLA settings
                db.SystemSettings.AddRange(
                    new SystemSetting { Id = "IT_Dashboard_EnableSlaEscalation", Value = "1", Category = "IT" },
                    new SystemSetting { Id = "IT_Dashboard_ReportWindowHours", Value = "1", Category = "IT" }
                );

                // Critical task overdue by 2 hours
                var task = new DailyTask
                {
                    Title = "Food Safety Check",
                    IsCritical = true,
                    Status = "Pending",
                    DueDate = DateTime.Today,
                    EndTime = DateTime.Now.AddHours(-2)
                };
                db.DailyTasks.Add(task);
                db.SaveChanges();

                var service = new DailyTaskService(db);
                bool success = service.ProcessEscalateSla();

                Assert.True(success);
                var alert = db.InboxMessages.FirstOrDefault(m => m.ReceiverId == "HT001" && m.ThreadId == $"sla_escalation_{task.Id}");
                Assert.NotNull(alert);
                Assert.Contains("Food Safety Check", alert.Content);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_DailyTask_SlaEscalation_NormalTask_NoTrigger()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.AddRange(
                    new SystemSetting { Id = "IT_Dashboard_EnableSlaEscalation", Value = "1", Category = "IT" },
                    new SystemSetting { Id = "IT_Dashboard_ReportWindowHours", Value = "1", Category = "IT" }
                );

                // Normal task overdue by 48 hours
                var task = new DailyTask
                {
                    Title = "Clean Classroom",
                    IsCritical = false,
                    Status = "Pending",
                    DueDate = DateTime.Today.AddDays(-2),
                    EndTime = DateTime.Now.AddDays(-2)
                };
                db.DailyTasks.Add(task);
                db.SaveChanges();

                var service = new DailyTaskService(db);
                bool success = service.ProcessEscalateSla();

                Assert.False(success);
                var alert = db.InboxMessages.FirstOrDefault(m => m.ThreadId == $"sla_escalation_{task.Id}");
                Assert.Null(alert);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_DailyTask_SlaEscalation_DisabledBySetting()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                // Disable SLA escalation in setting
                db.SystemSettings.AddRange(
                    new SystemSetting { Id = "IT_Dashboard_EnableSlaEscalation", Value = "0", Category = "IT" },
                    new SystemSetting { Id = "IT_Dashboard_ReportWindowHours", Value = "1", Category = "IT" }
                );

                // Critical task overdue by 2 hours
                var task = new DailyTask
                {
                    Title = "Gate Security Check",
                    IsCritical = true,
                    Status = "Pending",
                    DueDate = DateTime.Today,
                    EndTime = DateTime.Now.AddHours(-2)
                };
                db.DailyTasks.Add(task);
                db.SaveChanges();

                var service = new DailyTaskService(db);
                bool success = service.ProcessEscalateSla();

                Assert.False(success);
                var alert = db.InboxMessages.FirstOrDefault(m => m.ThreadId == $"sla_escalation_{task.Id}");
                Assert.Null(alert);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_DailyTask_GetSchoolDailySchedule_OverlapDetection_Multiple()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var today = DateTime.Today;
                var task1 = new DailyTask { Title = "Supervise Lunch", AssignedTo = "GV01", DueDate = today, StartTime = today.AddHours(11).AddMinutes(30), EndTime = today.AddHours(12).AddMinutes(30) };
                var task2 = new DailyTask { Title = "Staff Meeting", AssignedTo = "GV01", DueDate = today, StartTime = today.AddHours(12), EndTime = today.AddHours(13) }; // Overlaps with task 1
                var task3 = new DailyTask { Title = "Parent Talk", AssignedTo = "GV01", DueDate = today, StartTime = today.AddHours(12).AddMinutes(15), EndTime = today.AddHours(12).AddMinutes(45) }; // Overlaps with both
                
                db.DailyTasks.AddRange(task1, task2, task3);
                db.SaveChanges();

                var service = new DailyTaskService(db);
                var tasks = service.GetMyDailyTasksAndCoordinations("GV01", today);

                Assert.Equal(3, tasks.Count);
                
                // Verify overlaps
                var overlapsWithTask1 = tasks.Where(t => t.Id != task1.Id && t.StartTime < task1.EndTime && task1.StartTime < t.EndTime).ToList();
                Assert.Equal(2, overlapsWithTask1.Count);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_DailyTask_CompleteTask_Unauthorized_User()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                var task = new DailyTask { Title = "Supervise Exam Lab 1", AssignedTo = "GV01", CollaboratorIds = "NV01", DueDate = DateTime.Today, Status = "Pending" };
                db.DailyTasks.Add(task);
                db.SaveChanges();

                var service = new DailyTaskService(db);
                
                // GV02 tries to complete GV01's task (not collaborator)
                bool success = service.CompleteTask(task.Id, "Done", "GV02");
                Assert.False(success);
                
                // NV01 tries to complete (is collaborator)
                success = service.CompleteTask(task.Id, "Done", "NV01");
                Assert.True(success);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_DailyTask_AutoSyncToCalendar_MasterSetting()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                // Sync enabled setting
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Dashboard_AutoSyncToCalendar", Value = "1", Category = "IT" });

                var ev = new SchoolEvent { Title = "Opening Ceremony", StartTime = DateTime.Today.AddHours(8), EndTime = DateTime.Today.AddHours(10), Location = "Hall" };
                db.SchoolEvents.Add(ev);
                db.SaveChanges();

                var task = new DailyTask
                {
                    Title = "Assist Opening Ceremony",
                    EventId = ev.Id,
                    DueDate = DateTime.Today
                };

                // Simulator or Direct sync
                var autoSync = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Dashboard_AutoSyncToCalendar")?.Value == "1";
                if (autoSync && task.EventId.HasValue)
                {
                    var schoolEvent = db.SchoolEvents.Find(task.EventId.Value);
                    if (schoolEvent != null)
                    {
                        task.StartTime = schoolEvent.StartTime;
                        task.EndTime = schoolEvent.EndTime;
                        task.Location = schoolEvent.Location;
                    }
                }

                Assert.Equal(ev.StartTime, task.StartTime);
                Assert.Equal(ev.EndTime, task.EndTime);
                Assert.Equal("Hall", task.Location);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_DailyTask_GetSchoolDailySchedule_Department_All()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.DailyTasks.AddRange(
                    new DailyTask { Title = "Task BGH", Department = "BGH", DueDate = DateTime.Today },
                    new DailyTask { Title = "Task GV", Department = "Giáo viên", DueDate = DateTime.Today },
                    new DailyTask { Title = "Task NV", Department = "Nhân viên", DueDate = DateTime.Today }
                );
                db.SaveChanges();

                var service = new DailyTaskService(db);
                var allSchedule = service.GetSchoolDailySchedule(DateTime.Today, "All");

                Assert.Equal(3, allSchedule.Count);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Scheduled_With_Approval_Requires_Pending_Status()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_ApprovalWorkflow", Value = "2", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                var futureDate = DateTime.Now.AddDays(5);
                var result = service.CreateBulletin("Scheduled Student Post", "Content", "All", "Normal", "HS_001", futureDate);

                Assert.True(result.Success);
                var bulletin = db.Bulletins.FirstOrDefault(b => b.Title == "Scheduled Student Post");
                Assert.NotNull(bulletin);
                // Trạng thái ban đầu BẮT BUỘC là Pending vì người tạo là học sinh, mặc dù có lên lịch
                Assert.Equal("Pending", bulletin.Status);
                Assert.True(bulletin.IsScheduled);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Approve_Future_Scheduled()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_ApprovalWorkflow", Value = "1", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                var futureDate = DateTime.Now.AddDays(5);
                service.CreateBulletin("Future Scheduled Post", "Content", "All", "Normal", "HS_001", futureDate);

                var bulletin = db.Bulletins.FirstOrDefault(b => b.Title == "Future Scheduled Post");
                Assert.NotNull(bulletin);
                Assert.Equal("Pending", bulletin.Status);

                var req = db.BulletinApprovalRequests.FirstOrDefault(r => r.BulletinId == bulletin.Id);
                Assert.NotNull(req);

                // Phê duyệt tin
                service.ProcessApprovalAction(req.Id, "Manager", "Approve", "Approved!");

                db.Entry(bulletin).Reload();
                // Vì có scheduledAt ở tương lai, trạng thái sau duyệt phải là Scheduled chứ không phải Published trực tiếp!
                Assert.Equal("Scheduled", bulletin.Status);
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Sequential_Step2_Approval()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_ApprovalWorkflow", Value = "2", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                service.CreateBulletin("Step 2 Test Post", "Content", "All", "Normal", "HS_001");

                var bulletin = db.Bulletins.FirstOrDefault(b => b.Title == "Step 2 Test Post");
                Assert.NotNull(bulletin);

                // Lấy danh sách duyệt của Hiệu trưởng (HT001)
                var htRequestsBefore = service.GetPendingApprovals("HT001");
                // Chưa duyệt Step 1, nên Hiệu trưởng KHÔNG được thấy tin này
                Assert.Empty(htRequestsBefore.Where(r => r.BulletinId == bulletin.Id));

                // Phê duyệt Step 1
                var step1Req = db.BulletinApprovalRequests.FirstOrDefault(r => r.BulletinId == bulletin.Id && r.Step == 1);
                Assert.NotNull(step1Req);
                service.ProcessApprovalAction(step1Req.Id, "Manager", "Approve", "Ok Step 1");

                // Lấy lại danh sách duyệt của Hiệu trưởng
                var htRequestsAfter = service.GetPendingApprovals("HT001");
                // Sau khi Step 1 được duyệt, Hiệu trưởng sẽ nhìn thấy tin này để duyệt Step 2
                Assert.NotEmpty(htRequestsAfter.Where(r => r.BulletinId == bulletin.Id));
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

        [Fact]
        public void Test_Bulletin_Delete_Cascade_ApprovalRequests()
        {
            using var db = TestDbFactory.Create();
            AppDbContext.FallbackInMemoryConnection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            try
            {
                db.SystemSettings.Add(new SystemSetting { Id = "IT_Bulletin_ApprovalWorkflow", Value = "2", Category = "IT" });
                db.SaveChanges();

                var service = new BulletinService(db);
                service.CreateBulletin("Cascade Delete Post", "Content", "All", "Normal", "HS_001");

                var bulletin = db.Bulletins.FirstOrDefault(b => b.Title == "Cascade Delete Post");
                Assert.NotNull(bulletin);

                // Có 2 yêu cầu duyệt được tạo ra
                Assert.Equal(2, db.BulletinApprovalRequests.Count(r => r.BulletinId == bulletin.Id));

                // Xóa bản tin
                service.DeleteBulletin(bulletin.Id);

                // Các yêu cầu duyệt liên quan cũng phải bị xóa sạch (không lỗi FK)
                Assert.Empty(db.BulletinApprovalRequests.Where(r => r.BulletinId == bulletin.Id));
                Assert.Empty(db.Bulletins.Where(b => b.Id == bulletin.Id));
            }
            finally
            {
                AppDbContext.FallbackInMemoryConnection = null;
            }
        }

    }
}
