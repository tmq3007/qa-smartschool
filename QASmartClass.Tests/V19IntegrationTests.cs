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
    public class V19IntegrationTests
    {
        static V19IntegrationTests()
        {
            QASmartClass.Staff.Services.ServiceRegistration.Initialize();
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();

            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.24.0");
        }

        [Fact]
        public async Task TestIncident_ReportAndSecurityAudit_UsesTeacherCode()
        {
            var teacher = new TeacherProfile
            {
                TeacherCode = "TC_INC_19",
                FullName = "Nguyễn Sự Cố V19",
                TeacherPassword = "123",
                Role = "GV",
                IsActive = true,
                Subject = "Toán"
            };

            using (var db = new AppDbContext())
            {
                db.TeacherProfiles.Add(teacher);
                await db.SaveChangesAsync();
            }

            var prevUser = StaffSession.CurrentUser;
            StaffSession.Login(teacher);

            try
            {
                var vm = new IncidentManagementViewModel();
                vm.Description = "Học sinh sử dụng điện thoại trong lớp học Toán";
                
                await vm.ReportIncidentCommand.ExecuteAsync(null);

                using (var verifyDb = new AppDbContext())
                {
                    var log = await verifyDb.EventLogs
                        .FirstOrDefaultAsync(l => l.EventType == "Incident" && l.Actor == "TC_INC_19");
                    
                    Assert.NotNull(log);
                    var doc = System.Text.Json.JsonDocument.Parse(log.Details);
                    string desc = doc.RootElement.GetProperty("Description").GetString() ?? "";
                    Assert.Equal("Học sinh sử dụng điện thoại trong lớp học Toán", desc);

                    // Verify mapping on LoadIncidents
                    var incidentVm = new IncidentManagementViewModel();
                    await incidentVm.LoadIncidentsCommand.ExecuteAsync(null);
                    
                    var display = incidentVm.Incidents.FirstOrDefault(i => i.Actor == "TC_INC_19");
                    Assert.NotNull(display);
                    Assert.Equal("Nguyễn Sự Cố V19", display.DisplayActor);

                    // Cleanup
                    if (log != null) verifyDb.EventLogs.Remove(log);
                    await verifyDb.SaveChangesAsync();
                }
            }
            finally
            {
                if (prevUser != null)
                    StaffSession.Login(prevUser);
                else
                    StaffSession.Logout();

                using (var cleanupDb = new AppDbContext())
                {
                    cleanupDb.TeacherProfiles.Remove(teacher);
                    await cleanupDb.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task TestIncident_ResetForm_ClearsValidationState()
        {
            var vm = new IncidentManagementViewModel();
            vm.StudentCode = "HS01";
            vm.Description = "Sự cố test reset";
            vm.StudentNameDisplay = "Học sinh: Test Reset";
            vm.ErrorMessage = "Đã có lỗi xảy ra";

            vm.CancelEditCommand.Execute(null);

            Assert.Equal("Học sinh: --", vm.StudentNameDisplay);
            Assert.Equal("", vm.ErrorMessage);
            Assert.Equal("", vm.StudentCode);
            Assert.Equal("", vm.Description);
        }

        [Fact]
        public async Task TestCleaningSchedule_JanitorUniqueCode()
        {
            var janitor = new TeacherProfile
            {
                TeacherCode = "LC_TEST_19",
                FullName = "Bác Trực Nhật V19",
                Role = "LaoCong",
                IsActive = true
            };

            using (var db = new AppDbContext())
            {
                db.TeacherProfiles.Add(janitor);
                await db.SaveChangesAsync();
            }

            var prevUser = StaffSession.CurrentUser;
            StaffSession.Login(janitor);

            try
            {
                var vm = new CleaningScheduleViewModel();
                var loadJanitorsMethod = typeof(CleaningScheduleViewModel)
                    .GetMethod("LoadJanitorsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(loadJanitorsMethod);
                var t = loadJanitorsMethod.Invoke(vm, null) as Task;
                Assert.NotNull(t);
                await t;

                var lcProfile = vm.JanitorProfiles.FirstOrDefault(j => j.TeacherCode == "LC_TEST_19");
                Assert.NotNull(lcProfile);

                vm.SelectedJanitor = lcProfile;
                vm.Area = "Tầng 2 - Phòng STEM";
                vm.SelectedShift = "Morning";
                vm.Note = "Lau dọn tủ thiết bị";

                await vm.AddCommand.ExecuteAsync(null);

                using (var verifyDb = new AppDbContext())
                {
                    var task = await verifyDb.CleaningTasks
                        .FirstOrDefaultAsync(t => t.JanitorCode == "LC_TEST_19" && t.Area == "Tầng 2 - Phòng STEM");
                    
                    Assert.NotNull(task);
                    Assert.Equal("Bác Trực Nhật V19", task.JanitorName);
                    Assert.Equal("Morning", task.Shift);

                    // Cleanup
                    if (task != null) verifyDb.CleaningTasks.Remove(task);
                    await verifyDb.SaveChangesAsync();
                }
            }
            finally
            {
                if (prevUser != null)
                    StaffSession.Login(prevUser);
                else
                    StaffSession.Logout();

                using (var cleanupDb = new AppDbContext())
                {
                    cleanupDb.TeacherProfiles.Remove(janitor);
                    await cleanupDb.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task TestGateMonitor_EarlyLeaveList_CalculatesAndLoadsCorrectly()
        {
            using (var db = new AppDbContext())
            {
                var student = new Student
                {
                    FullName = "Học sinh Về Sớm V19",
                    StudentCode = "HS_VS_19_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                    ClassName = "11B2",
                    Status = "Active",
                    IsOnline = false,
                    LastSeen = DateTime.Now
                };
                db.Students.Add(student);
                await db.SaveChangesAsync();

                var req = new StudentLeaveRequest
                {
                    StudentId = student.Id,
                    StudentName = student.FullName,
                    ClassName = student.ClassName,
                    LeaveDate = DateTime.Today,
                    Reason = "Đau răng cần đi khám",
                    Status = "Approved",
                    CreatedAt = DateTime.Now
                };
                db.StudentLeaveRequests.Add(req);
                await db.SaveChangesAsync();

                try
                {
                    var vm = new GateMonitorViewModel();
                    await vm.RefreshDataCommand.ExecuteAsync(null);

                    Assert.Contains(vm.EarlyLeaveRequests, r => r.StudentName == student.FullName);
                    var record = vm.EarlyLeaveRequests.First(r => r.StudentName == student.FullName);
                    Assert.Equal("Approved", record.Status);
                    Assert.Equal("Đau răng cần đi khám", record.Reason);
                }
                finally
                {
                    using (var cleanupDb = new AppDbContext())
                    {
                        cleanupDb.Students.Remove(student);
                        cleanupDb.StudentLeaveRequests.Remove(req);
                        await cleanupDb.SaveChangesAsync();
                    }
                }
            }
        }

        [Fact]
        public async Task TestTaskManagement_AssigneeComboBoxFiltersByDept()
        {
            var teacherA = new TeacherProfile
            {
                TeacherCode = "TC_DEPT_A",
                FullName = "Thầy Toán V19",
                Subject = "Môn Toán",
                IsActive = true,
                Role = "GV"
            };
            var teacherB = new TeacherProfile
            {
                TeacherCode = "TC_DEPT_B",
                FullName = "Cô Sử V19",
                Subject = "Môn Lịch sử",
                IsActive = true,
                Role = "GV"
            };

            using (var db = new AppDbContext())
            {
                db.TeacherProfiles.AddRange(teacherA, teacherB);
                await db.SaveChangesAsync();
            }

            try
            {
                var vm = new TaskManagementViewModel();
                var loadTaskMethod = typeof(TaskManagementViewModel)
                    .GetMethod("LoadStaffNamesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(loadTaskMethod);
                
                // Select Toán phòng ban
                vm.SelectedDepartment = "ToanTin";
                var t1 = loadTaskMethod.Invoke(vm, null) as Task;
                Assert.NotNull(t1);
                await t1;
                
                Assert.Contains("Thầy Toán V19", vm.StaffNames);
                Assert.DoesNotContain("Cô Sử V19", vm.StaffNames);

                // Select Sử Địa phòng ban
                vm.SelectedDepartment = "VanSuDia";
                var t2 = loadTaskMethod.Invoke(vm, null) as Task;
                Assert.NotNull(t2);
                await t2;

                Assert.Contains("Cô Sử V19", vm.StaffNames);
                Assert.DoesNotContain("Thầy Toán V19", vm.StaffNames);
            }
            finally
            {
                using (var cleanupDb = new AppDbContext())
                {
                    cleanupDb.TeacherProfiles.RemoveRange(teacherA, teacherB);
                    await cleanupDb.SaveChangesAsync();
                }
            }
        }
    }
}
