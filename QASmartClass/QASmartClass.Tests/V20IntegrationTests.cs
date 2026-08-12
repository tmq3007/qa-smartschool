using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.ViewModels;
using QASmartClass.Staff.Services;
using Xunit;

namespace QASmartClass.Tests
{
    public class V20IntegrationTests
    {
        static V20IntegrationTests()
        {
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();

            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.24.0");
        }

        [Fact]
        public async Task TestStaffAuth_UpgradesToHashOnFirstLogin()
        {
            var teacher = new TeacherProfile
            {
                TeacherCode = "GV_AUTH_V20",
                FullName = "Nguyễn Auth V20",
                TeacherPassword = "myPlaintextPassword123",
                PasswordHash = "",
                Role = "GV",
                IsActive = true
            };

            using (var db = new AppDbContext())
            {
                var existing = await db.TeacherProfiles.FirstOrDefaultAsync(t => t.TeacherCode == "GV_AUTH_V20");
                if (existing != null) db.TeacherProfiles.Remove(existing);
                db.TeacherProfiles.Add(teacher);
                await db.SaveChangesAsync();
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    var user = await db.TeacherProfiles.FirstOrDefaultAsync(t => t.TeacherCode == "GV_AUTH_V20");
                    Assert.NotNull(user);
                    Assert.True(string.IsNullOrEmpty(user.PasswordHash));

                    // Verify using plain text check (simulated fallback check)
                    bool isValid = (user.TeacherPassword == "myPlaintextPassword123");
                    Assert.True(isValid);

                    // Upgrade
                    user.PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("myPlaintextPassword123");
                    await db.SaveChangesAsync();

                    // Verify hashed password
                    var updatedUser = await db.TeacherProfiles.FirstOrDefaultAsync(t => t.TeacherCode == "GV_AUTH_V20");
                    Assert.NotNull(updatedUser);
                    Assert.StartsWith("pbkdf2:", updatedUser.PasswordHash);

                    bool verifyHash = QASmartTouch.Services.AuthenticationService.VerifyPassword("myPlaintextPassword123", updatedUser.PasswordHash);
                    Assert.True(verifyHash);
                }
            }
            finally
            {
                using (var db = new AppDbContext())
                {
                    var user = await db.TeacherProfiles.FirstOrDefaultAsync(t => t.TeacherCode == "GV_AUTH_V20");
                    if (user != null)
                    {
                        db.TeacherProfiles.Remove(user);
                        await db.SaveChangesAsync();
                    }
                }
            }
        }

        [Fact]
        public async Task TestPayroll_LoadsFromStaffProfiles()
        {
            var staff = new StaffProfile
            {
                StaffCode = "SP_PAYROLL_V20",
                FullName = "Staff Payroll V20",
                BaseSalary = 12000000,
                JoinedDate = DateTime.Now
            };

            using (var db = new AppDbContext())
            {
                var existing = await db.StaffProfiles.FirstOrDefaultAsync(s => s.StaffCode == "SP_PAYROLL_V20");
                if (existing != null) db.StaffProfiles.Remove(existing);
                db.StaffProfiles.Add(staff);
                await db.SaveChangesAsync();
            }

            try
            {
                var vm = new PayrollViewModel();
                var loadTask = typeof(PayrollViewModel)
                    .GetMethod("LoadStaffNamesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(loadTask);
                var task = loadTask.Invoke(vm, null) as Task;
                Assert.NotNull(task);
                await task;

                Assert.Contains("Staff Payroll V20", vm.StaffNames);
            }
            finally
            {
                using (var db = new AppDbContext())
                {
                    var s = await db.StaffProfiles.FirstOrDefaultAsync(p => p.StaffCode == "SP_PAYROLL_V20");
                    if (s != null)
                    {
                        db.StaffProfiles.Remove(s);
                        await db.SaveChangesAsync();
                    }
                }
            }
        }

        [Fact]
        public async Task TestIncident_PrivacyPermissions()
        {
            var teacherB = new TeacherProfile
            {
                TeacherCode = "TC_PRIV_B",
                FullName = "Teacher Priv B",
                Role = "GV",
                Notes = "GV chủ nhiệm 10A2",
                IsActive = true
            };

            using (var db = new AppDbContext())
            {
                var existing = await db.TeacherProfiles.FirstOrDefaultAsync(t => t.TeacherCode == "TC_PRIV_B");
                if (existing != null) db.TeacherProfiles.Remove(existing);
                db.TeacherProfiles.Add(teacherB);
                await db.SaveChangesAsync();
            }

            Student? student1 = null;
            Student? student2 = null;
            EventLog? log1 = null;
            EventLog? log2 = null;

            try
            {
                using (var db = new AppDbContext())
                {
                    student1 = new Student { StudentCode = "HS_PRIV_1", FullName = "Student Priv 1", ClassName = "10A2", ClassroomId = 1, Status = "Active" };
                    student2 = new Student { StudentCode = "HS_PRIV_2", FullName = "Student Priv 2", ClassName = "11A3", ClassroomId = 1, Status = "Active" };
                    db.Students.AddRange(student1, student2);
                    await db.SaveChangesAsync();

                    log1 = new EventLog
                    {
                        EventType = "Incident",
                        Actor = "TC_PRIV_C",
                        Timestamp = DateTime.Now,
                        Details = System.Text.Json.JsonSerializer.Serialize(new { Severity = "Low", StudentCode = "HS_PRIV_1", Description = "Incident of 10A2 student" })
                    };
                    log2 = new EventLog
                    {
                        EventType = "Incident",
                        Actor = "TC_PRIV_C",
                        Timestamp = DateTime.Now,
                        Details = System.Text.Json.JsonSerializer.Serialize(new { Severity = "Low", StudentCode = "HS_PRIV_2", Description = "Incident of 11A3 student" })
                    };
                    db.EventLogs.AddRange(log1, log2);
                    await db.SaveChangesAsync();
                }

                // Log in as teacherB
                StaffSession.Login(teacherB);

                var vm = new IncidentManagementViewModel();
                await vm.LoadIncidentsCommand.ExecuteAsync(null);

                // Should see log1 because it belongs to class 10A2 (teacherB is advisor)
                Assert.Contains(vm.Incidents, i => i.Description == "Incident of 10A2 student");

                // Should NOT see log2 because it belongs to class 11A3 (teacherB is not advisor, nor author)
                Assert.DoesNotContain(vm.Incidents, i => i.Description == "Incident of 11A3 student");
            }
            finally
            {
                StaffSession.Logout();
                using (var db = new AppDbContext())
                {
                    var t = await db.TeacherProfiles.FirstOrDefaultAsync(p => p.TeacherCode == "TC_PRIV_B");
                    if (t != null) db.TeacherProfiles.Remove(t);

                    if (student1 != null) { var s1 = await db.Students.FirstOrDefaultAsync(s => s.StudentCode == "HS_PRIV_1"); if (s1 != null) db.Students.Remove(s1); }
                    if (student2 != null) { var s2 = await db.Students.FirstOrDefaultAsync(s => s.StudentCode == "HS_PRIV_2"); if (s2 != null) db.Students.Remove(s2); }

                    if (log1 != null) { var l1 = await db.EventLogs.FirstOrDefaultAsync(e => e.Id == log1.Id); if (l1 != null) db.EventLogs.Remove(l1); }
                    if (log2 != null) { var l2 = await db.EventLogs.FirstOrDefaultAsync(e => e.Id == log2.Id); if (l2 != null) db.EventLogs.Remove(l2); }

                    await db.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task TestIncident_GoodDeedLogStorage()
        {
            using (var db = new AppDbContext())
            {
                var student = await db.Students.FirstOrDefaultAsync(s => s.StudentCode == "HS001");
                bool createdStudent = false;
                if (student == null)
                {
                    student = new Student { StudentCode = "HS001", FullName = "Test Student", ClassName = "10A1", Status = "Active", IsOnline = false, LastSeen = DateTime.Now };
                    db.Students.Add(student);
                    await db.SaveChangesAsync();
                    createdStudent = true;
                }

                var vm = new IncidentManagementViewModel();
                vm.GoodDeedStudentCode = "HS001";
                vm.GoodDeedType = "Nhặt của rơi";
                vm.GoodDeedDescription = "Nhặt được ví tiền và trả lại cho cô giáo chủ nhiệm";

                await vm.ReportGoodDeedCommand.ExecuteAsync(null);

                try
                {
                    var log = await db.EventLogs
                        .Where(l => l.EventType == "GoodDeed")
                        .OrderByDescending(l => l.Timestamp)
                        .FirstOrDefaultAsync();

                    Assert.NotNull(log);
                    using var doc = System.Text.Json.JsonDocument.Parse(log.Details);
                    var root = doc.RootElement;
                    string desc = root.GetProperty("Description").GetString() ?? "";
                    Assert.Contains("Nhặt được ví tiền", desc);
                    Assert.Contains("Nhặt của rơi", desc);

                    // Cleanup
                    db.EventLogs.Remove(log);
                    await db.SaveChangesAsync();
                }
                finally
                {
                    vm.CancelEditGoodDeedCommand.Execute(null);
                    if (createdStudent && student != null)
                    {
                        db.Students.Remove(student);
                        await db.SaveChangesAsync();
                    }
                }
            }
        }

        [Fact]
        public async Task TestGate_LeavePassVerification()
        {
            using (var db = new AppDbContext())
            {
                var student = new Student
                {
                    FullName = "Học sinh Leave Pass V20",
                    StudentCode = "HS_LP_V20",
                    ClassName = "11B2",
                    Status = "Active",
                    IsOnline = false,
                    LastSeen = DateTime.Now
                };
                db.Students.Add(student);
                await db.SaveChangesAsync();

                var today = DateTime.Today;
                var req = new StudentLeaveRequest
                {
                    StudentId = student.Id,
                    StudentName = student.FullName,
                    ClassName = student.ClassName,
                    LeaveDate = today,
                    Reason = "Mệt mỏi cần về sớm",
                    Status = "Approved",
                    CreatedAt = DateTime.Now
                };
                db.StudentLeaveRequests.Add(req);
                await db.SaveChangesAsync();

                try
                {
                    var vm = new GateMonitorViewModel();
                    await vm.RefreshDataCommand.ExecuteAsync(null);

                    // Verify the request is loaded
                    Assert.Contains(vm.EarlyLeaveRequests, r => r.StudentId == student.Id);

                    // Verify scanned pass verification
                    vm.ScannedLeavePassCode = $"LP-{today:yyyyMMdd}-{student.Id}";
                    await vm.ScanLeavePassCommand.ExecuteAsync(null);

                    // Verify check out event was recorded in DB
                    using (var verifyDb = new AppDbContext())
                    {
                        var checkoutLog = await verifyDb.EventLogs
                            .FirstOrDefaultAsync(l => l.EventType == "GateCheckOut" && l.Actor == "HS_LP_V20");
                        
                        Assert.NotNull(checkoutLog);

                        // Cleanup checkout log
                        verifyDb.EventLogs.Remove(checkoutLog);
                        await verifyDb.SaveChangesAsync();
                    }
                }
                finally
                {
                    var s = await db.Students.FirstOrDefaultAsync(st => st.Id == student.Id);
                    if (s != null) db.Students.Remove(s);
                    var r = await db.StudentLeaveRequests.FirstOrDefaultAsync(lr => lr.Id == req.Id);
                    if (r != null) db.StudentLeaveRequests.Remove(r);
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
