using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.Services;
using QASmartClass.Staff.ViewModels;
using Xunit;

namespace QASmartClass.Tests
{
    public class V23IntegrationTests
    {
        static V23IntegrationTests()
        {
            QASmartClass.Staff.Services.ServiceRegistration.Initialize();
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();

            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.24.0");
        }

        [Fact]
        public async Task TestCanteen_QuickPayBypassesOnAllergy()
        {
            using (var db = new AppDbContext())
            {
                var student = new Student
                {
                    FullName = "HS Allergy Test V23",
                    StudentCode = "HS_ALLERGY_V23",
                    ClassName = "10A1",
                    WalletBalance = 50000,
                    Status = "Active",
                    IsOnline = false,
                    LastSeen = DateTime.Now
                };
                db.Students.Add(student);
                await db.SaveChangesAsync();

                var allergy = new FoodAllergy
                {
                    StudentName = "HS Allergy Test V23",
                    Allergen = "Đậu phộng",
                    Severity = "Severe",
                    ActionPlan = "Tránh xa đậu phộng"
                };
                db.FoodAllergies.Add(allergy);
                await db.SaveChangesAsync();

                try
                {
                    var vm = new CanteenPosViewModel();
                    vm.IsQuickPayMode = true;
                    vm.QuickPayAmount = 25000;
                    vm.SearchCode = "HS_ALLERGY_V23";

                    await vm.SearchStudentCommand.ExecuteAsync(null);

                    // Check that warning was shown
                    Assert.Contains("⚠ CẢNH BÁO DỊ ỨNG", vm.AllergyWarning);
                    Assert.Contains("Giao dịch bán nhanh tạm dừng do có cảnh báo dị ứng", vm.StatusMessage);

                    // Verify that the wallet was NOT automatically deducted
                    using (var verifyDb = new AppDbContext())
                    {
                        var verified = await verifyDb.Students.FindAsync(student.Id);
                        Assert.NotNull(verified);
                        Assert.Equal(50000, verified.WalletBalance);
                    }
                }
                finally
                {
                    var targetAllergy = await db.FoodAllergies.FirstOrDefaultAsync(a => a.StudentName == "HS Allergy Test V23");
                    if (targetAllergy != null) db.FoodAllergies.Remove(targetAllergy);
                    var targetStudent = await db.Students.FindAsync(student.Id);
                    if (targetStudent != null) db.Students.Remove(targetStudent);
                    await db.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task TestTaskManagement_CascadingUpdateDelete()
        {
            using (var db = new AppDbContext())
            {
                var teacher = new TeacherProfile
                {
                    TeacherCode = "GV_SYNC_V23",
                    FullName = "Giáo Viên V23 Sync",
                    Subject = "Toán",
                    IsActive = true
                };
                db.TeacherProfiles.Add(teacher);
                await db.SaveChangesAsync();

                int taskId = 0;

                try
                {
                    var vm = new TaskManagementViewModel();
                    vm.TaskTitle = "Task đồng bộ V23";
                    vm.Assignee = "Giáo Viên V23 Sync";
                    vm.DueDate = new DateTime(2026, 6, 8); // Monday -> 2
                    vm.SyncPeriod = 3;
                    vm.SyncRoom = "Phòng Lab V23";

                    await vm.AssignCommand.ExecuteAsync(null);

                    // 1. Verify task creation and timetable entry creation
                    using (var verifyDb = new AppDbContext())
                    {
                        var task = await verifyDb.DailyTasks.FirstOrDefaultAsync(t => t.Title == "Task đồng bộ V23" && t.AssignedTo == "Giáo Viên V23 Sync");
                        Assert.NotNull(task);
                        taskId = task.Id;

                        var entry = await verifyDb.TimetableEntries.FirstOrDefaultAsync(e => e.Subject.StartsWith($"[Task#{task.Id}]"));
                        Assert.NotNull(entry);
                        Assert.Equal($"[Task#{task.Id}] Task đồng bộ V23", entry.Subject);
                        Assert.Equal(3, entry.Period);
                        Assert.Equal("Phòng Lab V23", entry.Room);
                        
                        // Set SelectedTask
                        vm.SelectedTask = task;
                    }

                    // 2. Test Cascade Update
                    vm.IsEditing = true;
                    vm.TaskTitle = "Task đồng bộ V23 Đã Sửa";
                    vm.SyncPeriod = 5;
                    vm.SyncRoom = "Phòng Lab V23 Sửa";
                    await vm.AssignCommand.ExecuteAsync(null);

                    using (var verifyDb = new AppDbContext())
                    {
                        var entry = await verifyDb.TimetableEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Subject.StartsWith($"[Task#{taskId}]"));
                        Assert.NotNull(entry);
                        Assert.Equal($"[Task#{taskId}] Task đồng bộ V23 Đã Sửa", entry.Subject);
                        Assert.Equal(5, entry.Period);
                        Assert.Equal("Phòng Lab V23 Sửa", entry.Room);
                        
                        // Set SelectedTask for deletion
                        var task = await verifyDb.DailyTasks.FindAsync(taskId);
                        vm.SelectedTask = task;
                    }

                    // 3. Test Cascade Delete
                    await vm.DeleteTaskCommand.ExecuteAsync(null);

                    using (var verifyDb = new AppDbContext())
                    {
                        var deletedTask = await verifyDb.DailyTasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
                        Assert.Null(deletedTask);

                        var deletedEntry = await verifyDb.TimetableEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Subject.StartsWith($"[Task#{taskId}]"));
                        Assert.Null(deletedEntry);
                    }
                }
                finally
                {
                    var targetTeacher = await db.TeacherProfiles.FindAsync(teacher.Id);
                    if (targetTeacher != null) db.TeacherProfiles.Remove(targetTeacher);
                    if (taskId > 0)
                    {
                        var task = await db.DailyTasks.FindAsync(taskId);
                        if (task != null) db.DailyTasks.Remove(task);
                        var entry = await db.TimetableEntries.FirstOrDefaultAsync(e => e.Subject.StartsWith($"[Task#{taskId}]"));
                        if (entry != null) db.TimetableEntries.Remove(entry);
                    }
                    await db.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public void TestIncident_IndependentClassFilters()
        {
            var vm = new IncidentManagementViewModel();
            Assert.Equal("Tất cả", vm.SelectedClassIncident);
            Assert.Equal("Tất cả", vm.SelectedClassGoodDeed);

            vm.SelectedClassIncident = "10A1";
            Assert.Equal("10A1", vm.SelectedClassIncident);
            Assert.Equal("Tất cả", vm.SelectedClassGoodDeed); // Should remain independent

            vm.SelectedClassGoodDeed = "10A2";
            Assert.Equal("10A1", vm.SelectedClassIncident);
            Assert.Equal("10A2", vm.SelectedClassGoodDeed);
        }

        [Fact]
        public async Task TestLeaveRequest_RejectDoesNotForceAbsent()
        {
            var prevUser = QASmartClass.Staff.Services.StaffSession.CurrentUser;
            QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { FullName = "Teacher Log", Role = "GV", IsActive = true });

            using (var db = new AppDbContext())
            {
                var student = new Student
                {
                    FullName = "Học sinh V23 Leave",
                    StudentCode = "HS_LEAVE_V23_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                    ClassName = "12A3",
                    Status = "Active",
                    IsOnline = false,
                    LastSeen = DateTime.Now
                };
                db.Students.Add(student);
                await db.SaveChangesAsync();

                var roster = new ClassRoster
                {
                    ClassName = "12A3",
                    GradeLevel = "12",
                    Subject = "Toán",
                    TeacherName = "Giáo Viên V23 Leave",
                    IsActive = true
                };
                db.ClassRosters.Add(roster);
                await db.SaveChangesAsync();

                var rosterStudent = new ClassRosterStudent
                {
                    RosterId = roster.Id,
                    StudentId = student.Id,
                    SeatNumber = 1
                };
                db.ClassRosterStudents.Add(rosterStudent);
                await db.SaveChangesAsync();

                var req = new StudentLeaveRequest
                {
                    StudentId = student.Id,
                    StudentName = student.FullName,
                    ClassName = student.ClassName,
                    LeaveDate = DateTime.Today,
                    Reason = "Nghỉ mát V23",
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };
                db.StudentLeaveRequests.Add(req);
                await db.SaveChangesAsync();

                try
                {
                    var vm = new LeaveRequestManagementViewModel();
                    vm.SelectedClassFilter = "Tất cả các lớp";
                    await vm.LoadRequestsCommand.ExecuteAsync(null);

                    vm.RejectReason = "Không đồng ý nghỉ phép du lịch.";
                    await vm.RejectCommand.ExecuteAsync(req.Id);

                    // Check DB status of request
                    using (var verifyDb = new AppDbContext())
                    {
                        var verifiedReq = await verifyDb.StudentLeaveRequests.FindAsync(req.Id);
                        Assert.NotNull(verifiedReq);
                        Assert.Equal("Rejected", verifiedReq.Status);

                        // V23 Verification: Verify NO absent attendance record is created
                        var attendance = await verifyDb.AttendanceRecords
                            .FirstOrDefaultAsync(a => a.StudentId == student.Id && a.Date.Date == DateTime.Today);
                        Assert.Null(attendance);
                    }
                }
                finally
                {
                    if (prevUser != null)
                        QASmartClass.Staff.Services.StaffSession.Login(prevUser);
                    else
                        QASmartClass.Staff.Services.StaffSession.Logout();

                    using (var cleanupDb = new AppDbContext())
                    {
                        cleanupDb.Students.Remove(student);
                        cleanupDb.StudentLeaveRequests.Remove(req);

                        var rosterStudentToRemove = await cleanupDb.ClassRosterStudents.FirstOrDefaultAsync(crs => crs.StudentId == student.Id);
                        if (rosterStudentToRemove != null) cleanupDb.ClassRosterStudents.Remove(rosterStudentToRemove);

                        var rosterToRemove = await cleanupDb.ClassRosters.FirstOrDefaultAsync(cr => cr.Id == roster.Id);
                        if (rosterToRemove != null) cleanupDb.ClassRosters.Remove(rosterToRemove);

                        await cleanupDb.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
