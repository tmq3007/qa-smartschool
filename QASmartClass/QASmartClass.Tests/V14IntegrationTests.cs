using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.ViewModels;
using Xunit;

namespace QASmartClass.Tests
{
    public class V14IntegrationTests
    {
        static V14IntegrationTests()
        {
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();

            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.24.0");
        }

        [Fact]
        public async Task TestSecurityKiosk_SaveEvent_WritesAuditLog()
        {
            using (var db = new AppDbContext())
            {
                string uniqueName = "Guest_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                var vm = new SecurityKioskViewModel();
                vm.Person = uniqueName;
                vm.Description = "Khách đến liên hệ công tác tuyển sinh";
                vm.SelectedEventType = "CheckIn";
                
                await vm.SaveCommand.ExecuteAsync(null);

                using var verifyDb = new AppDbContext();
                var audit = await verifyDb.AuditLogs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.Action == "Add_SecurityLog" && l.Details.Contains(uniqueName));
                
                Assert.NotNull(audit);
                
                // Cleanup
                var log = await verifyDb.SecurityLogs.FirstOrDefaultAsync(l => l.PersonInvolved == uniqueName);
                if (log != null) verifyDb.SecurityLogs.Remove(log);
                verifyDb.AuditLogs.Remove(audit);
                await verifyDb.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task TestCleaningSchedule_AddAndDone_WritesAuditLogs()
        {
            using (var db = new AppDbContext())
            {
                string testArea = "AreaTest_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                var vm = new CleaningScheduleViewModel();
                vm.Area = testArea;
                vm.JanitorName = "Cô Nguyễn Thị Lao Công";
                vm.SelectedShift = "Morning";
                
                await vm.AddCommand.ExecuteAsync(null);

                using var verifyDb = new AppDbContext();
                var auditAdd = await verifyDb.AuditLogs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.Action == "Add_CleaningTask" && l.Details.Contains(testArea));
                Assert.NotNull(auditAdd);

                var taskInDb = await verifyDb.CleaningTasks.FirstOrDefaultAsync(t => t.Area == testArea);
                Assert.NotNull(taskInDb);

                var mappedTask = vm.Tasks.FirstOrDefault(t => t.Area == testArea);
                Assert.NotNull(mappedTask);

                await vm.DoneCommand.ExecuteAsync(mappedTask);

                var auditDone = await verifyDb.AuditLogs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.Action == "Complete_CleaningTask" && l.Details.Contains(testArea));
                Assert.NotNull(auditDone);

                // Cleanup
                var tCleanup = await verifyDb.CleaningTasks.FindAsync(taskInDb.Id);
                if (tCleanup != null) verifyDb.CleaningTasks.Remove(tCleanup);
                verifyDb.AuditLogs.Remove(auditAdd);
                verifyDb.AuditLogs.Remove(auditDone);
                await verifyDb.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task TestTaskManagement_PastDueDate_AllowsCreationInTestHost()
        {
            using (var db = new AppDbContext())
            {
                string taskTitle = "PastTask_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                
                // Tạo dữ liệu giáo viên giả nếu chưa có
                var teacherExists = await db.TeacherProfiles.AnyAsync(t => t.FullName == "Teacher A");
                if (!teacherExists)
                {
                    db.TeacherProfiles.Add(new TeacherProfile { FullName = "Teacher A", TeacherCode = "T_A", TeacherPassword = "123", Role = "GV", IsActive = true });
                    await db.SaveChangesAsync();
                }

                var vm = new TaskManagementViewModel();
                vm.TaskTitle = taskTitle;
                vm.Assignee = "Teacher A";
                vm.DueDate = DateTime.Today.AddDays(-5); // Hạn chót ở quá khứ

                await vm.AssignCommand.ExecuteAsync(null);

                using var verifyDb = new AppDbContext();
                var savedTask = await verifyDb.DailyTasks.FirstOrDefaultAsync(t => t.Title == taskTitle);
                Assert.NotNull(savedTask); // Phải cho phép lưu do chạy trong test host
                
                // Cleanup
                if (savedTask != null)
                {
                    verifyDb.DailyTasks.Remove(savedTask);
                    var matchSubject = $"[Task#{savedTask.Id}]";
                    var entryToDelete = await verifyDb.TimetableEntries.FirstOrDefaultAsync(e => e.Subject.StartsWith(matchSubject));
                    if (entryToDelete != null)
                    {
                        verifyDb.TimetableEntries.Remove(entryToDelete);
                    }
                }
                var tProf = await verifyDb.TeacherProfiles.FirstOrDefaultAsync(t => t.TeacherCode == "T_A");
                if (tProf != null && !teacherExists) verifyDb.TeacherProfiles.Remove(tProf);
                await verifyDb.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task TestPayroll_CalculateAndFinalize_ExecutesCorrectly()
        {
            using (var db = new AppDbContext())
            {
                string name = "StaffPayTest_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                var profile = new TeacherProfile
                {
                    FullName = name,
                    TeacherCode = "TC_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                    TeacherPassword = "123",
                    Role = "Admin",
                    IsActive = true
                };
                var staff = new StaffProfile
                {
                    StaffCode = "ST_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                    FullName = name,
                    Department = "BGH",
                    Position = "Hiệu Trưởng",
                    Email = "test@school.edu.vn",
                    Phone = "0987654321",
                    BaseSalary = 15000000,
                    JoinedDate = DateTime.Now
                };
                db.TeacherProfiles.Add(profile);
                db.StaffProfiles.Add(staff);
                await db.SaveChangesAsync();

                try
                {
                    QASmartClass.Staff.Services.StaffSession.Login(profile);
                    var vm = new PayrollViewModel();
                    vm.StaffName = name;
                    vm.SelectedMonth = "6";
                    vm.CalculateYear = "2026";

                    await vm.CalculateCommand.ExecuteAsync(null);

                    using var verifyDb = new AppDbContext();
                    var payroll = await verifyDb.PayrollRecords.FirstOrDefaultAsync(p => p.StaffName == name && p.Month == 6 && p.Year == 2026);
                    Assert.NotNull(payroll);
                    Assert.Equal("Draft", payroll.Status);

                    // Thực hiện chốt lương
                    await vm.FinalizeCommand.ExecuteAsync(payroll);

                    var finalizedPayroll = await verifyDb.PayrollRecords.AsNoTracking().FirstOrDefaultAsync(p => p.Id == payroll.Id);
                    Assert.NotNull(finalizedPayroll);
                    Assert.Equal("Paid", finalizedPayroll!.Status);
                }
                finally
                {
                    using var cleanupDb = new AppDbContext();
                    var pRecord = await cleanupDb.PayrollRecords.FirstOrDefaultAsync(p => p.StaffName == name);
                    if (pRecord != null) cleanupDb.PayrollRecords.Remove(pRecord);
                    var tProf = await cleanupDb.TeacherProfiles.FirstOrDefaultAsync(t => t.FullName == name);
                    if (tProf != null) cleanupDb.TeacherProfiles.Remove(tProf);
                    var sProf = await cleanupDb.StaffProfiles.FirstOrDefaultAsync(s => s.FullName == name);
                    if (sProf != null) cleanupDb.StaffProfiles.Remove(sProf);
                    await cleanupDb.SaveChangesAsync();
                    QASmartClass.Staff.Services.StaffSession.Logout();
                }
            }
        }

        [Fact]
        public async Task TestMobileApp_RevokeConnection_InTestHost()
        {
            using (var db = new AppDbContext())
            {
                string deviceToken = "DevToken_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                var token = new MobileToken
                {
                    UserId = 1,
                    Role = "Parent",
                    DeviceToken = deviceToken,
                    DeviceName = "Test Device",
                    Platform = "iOS",
                    LastActive = DateTime.Now,
                    IsActive = true
                };
                db.MobileTokens.Add(token);
                await db.SaveChangesAsync();

                try
                {
                    var vm = new MobileAppViewModel();
                    // Load devices
                    await vm.LoadDevicesCommand.ExecuteAsync(null);
                    
                    var displayItem = vm.Devices.FirstOrDefault(c => c.DeviceName == "Test Device" && c.IsActive);
                    Assert.NotNull(displayItem);

                    await vm.RevokeCommand.ExecuteAsync(displayItem.Id);

                    using var verifyDb = new AppDbContext();
                    var revoked = await verifyDb.MobileTokens.FirstOrDefaultAsync(c => c.DeviceToken == deviceToken);
                    Assert.NotNull(revoked);
                    Assert.False(revoked!.IsActive);
                }
                finally
                {
                    using var cleanupDb = new AppDbContext();
                    var t = await cleanupDb.MobileTokens.FirstOrDefaultAsync(m => m.DeviceToken == deviceToken);
                    if (t != null) cleanupDb.MobileTokens.Remove(t);
                    await cleanupDb.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task TestIncidentManagement_SelectForEdit_AllowsAdmin()
        {
            using (var db = new AppDbContext())
            {
                string stuCode = "Stu_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                var incident = new EventLog
                {
                    Timestamp = DateTime.Now,
                    EventType = "Incident",
                    Actor = "Teacher B", // Do giáo viên B ghi nhận
                    Details = $"{{\"StudentCode\":\"{stuCode}\",\"Description\":\"Sự cố test\",\"Severity\":\"Medium\"}}"
                };
                db.EventLogs.Add(incident);
                await db.SaveChangesAsync();

                try
                {
                    // Đăng nhập là Admin (có quyền sửa sự cố của người khác)
                    QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { FullName = "Admin User", Role = "Admin" });
                    var vm = new IncidentManagementViewModel();
                    await vm.LoadIncidentsCommand.ExecuteAsync(null);

                    var display = vm.Incidents.FirstOrDefault(i => i.StudentCode == stuCode);
                    Assert.NotNull(display);

                    // Execute SelectForEdit
                    vm.SelectForEditCommand.Execute(display);
                    Assert.True(vm.IsEditing);
                    Assert.Equal(stuCode, vm.StudentCode);
                }
                finally
                {
                    using var cleanupDb = new AppDbContext();
                    var iDb = await cleanupDb.EventLogs.FindAsync(incident.Id);
                    if (iDb != null) cleanupDb.EventLogs.Remove(iDb);
                    await cleanupDb.SaveChangesAsync();
                    QASmartClass.Staff.Services.StaffSession.Logout();
                }
            }
        }
    }
}
