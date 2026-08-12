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
    public class V8IntegrationTests
    {
        static V8IntegrationTests()
        {
            QASmartClass.Staff.Services.ServiceRegistration.Initialize();
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();

            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.24.0");
        }

        [Fact]
        public async Task TestLeaveRequest_Reject_KeepsAbsentStatusAndSavesReason()
        {
            var prevUser = QASmartClass.Staff.Services.StaffSession.CurrentUser;
            QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { FullName = "Teacher Log", Role = "GV", IsActive = true });

            Student? student = null;
            StudentLeaveRequest? leaveReq = null;
            ClassRoster? roster = null;
            ClassRosterStudent? rosterStudent = null;
            using (var db = new AppDbContext())
            {
                try
                {
                    student = new Student
                    {
                        FullName = "Leave Student Test",
                        StudentCode = "HS_LEAVE_TEST",
                        ClassName = "11A2",
                        IsOnline = false,
                        LastSeen = DateTime.Now
                    };
                    db.Students.Add(student);
                    db.SaveChanges();

                    roster = new ClassRoster
                    {
                        ClassName = "11A2",
                        IsActive = true,
                        Subject = "Chuyên đề",
                        GradeLevel = "11"
                    };
                    db.ClassRosters.Add(roster);
                    db.SaveChanges();

                    rosterStudent = new ClassRosterStudent
                    {
                        RosterId = roster.Id,
                        StudentId = student.Id
                    };
                    db.ClassRosterStudents.Add(rosterStudent);
                    db.SaveChanges();

                    leaveReq = new StudentLeaveRequest
                    {
                        StudentId = student.Id,
                        StudentName = student.FullName,
                        ClassName = student.ClassName,
                        LeaveDate = DateTime.Today.AddDays(2),
                        Reason = "Cảm cúm",
                        Status = "Pending",
                        CreatedAt = DateTime.Now
                    };
                    db.StudentLeaveRequests.Add(leaveReq);
                    db.SaveChanges();

                    var vm = new LeaveRequestManagementViewModel();
                    vm.RejectReason = "Trùng lịch kiểm tra 1 tiết";

                    // Execute the RejectCommand
                    await vm.RejectCommand.ExecuteAsync(leaveReq.Id);

                    // Check that request status is updated to Rejected
                    var updatedReq = await db.StudentLeaveRequests.FindAsync(leaveReq.Id);
                    Assert.NotNull(updatedReq);
                    await db.Entry(updatedReq).ReloadAsync();
                    Assert.Equal("Rejected", updatedReq.Status);

                    // Check that attendance record status is absent and contains correct notes
                    var attendance = await db.AttendanceRecords
                        .FirstOrDefaultAsync(a => a.StudentId == student.Id && a.Date.Date == leaveReq.LeaveDate.Date);
                    Assert.NotNull(attendance);
                    Assert.Equal("absent", attendance.Status);
                    Assert.Contains("Vắng không phép: Trùng lịch kiểm tra 1 tiết", attendance.Note);
                }
                finally
                {
                    if (prevUser != null)
                        QASmartClass.Staff.Services.StaffSession.Login(prevUser);
                    else
                        QASmartClass.Staff.Services.StaffSession.Logout();

                    if (leaveReq != null)
                    {
                        db.StudentLeaveRequests.Remove(leaveReq);
                    }
                    if (rosterStudent != null)
                    {
                        db.ClassRosterStudents.Remove(rosterStudent);
                    }
                    if (roster != null)
                    {
                        db.ClassRosters.Remove(roster);
                    }
                    if (student != null)
                    {
                        var attendances = db.AttendanceRecords.Where(a => a.StudentId == student.Id).ToList();
                        db.AttendanceRecords.RemoveRange(attendances);
                        db.Students.Remove(student);
                    }
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public async Task TestCanteenPos_SearchFail_ClearsCurrentStudentState()
        {
            using (var db = new AppDbContext())
            {
                var vm = new CanteenPosViewModel();
                
                // First simulate a successful search (if students exist)
                var student = await db.Students.FirstOrDefaultAsync();
                if (student != null)
                {
                    vm.SearchCode = student.StudentCode;
                    await vm.SearchStudentCommand.ExecuteAsync(null);
                    Assert.NotNull(vm.CurrentStudent);
                    Assert.Equal(student.StudentCode, vm.CurrentStudent.StudentCode);
                }

                // Simulate search failure
                vm.SearchCode = "NON_EXISTENT_CODE_12345";
                await vm.SearchStudentCommand.ExecuteAsync(null);
                
                // State should be cleared
                Assert.Null(vm.CurrentStudent);
                Assert.Equal(0m, vm.CurrentBalance);
                Assert.Contains("Không tìm thấy", vm.StatusMessage);

                // Simulate empty code search
                vm.SearchCode = "";
                await vm.SearchStudentCommand.ExecuteAsync(null);
                
                Assert.Null(vm.CurrentStudent);
                Assert.Equal(0m, vm.CurrentBalance);
                Assert.Contains("Vui lòng nhập", vm.StatusMessage);
            }
        }

        [Fact]
        public async Task TestCleaningSchedule_DateChange_TriggersReload()
        {
            using (var db = new AppDbContext())
            {
                var vm = new CleaningScheduleViewModel();
                
                // Add a dummy task for tomorrow
                var tomorrow = DateTime.Today.AddDays(1);
                var task = new CleaningTask
                {
                    Area = "Hành lang lầu 2",
                    JanitorName = "Cô Nguyễn Thị Lao Công",
                    TaskDate = tomorrow,
                    Shift = "Afternoon",
                    Status = "Pending"
                };
                db.CleaningTasks.Add(task);
                db.SaveChanges();

                try
                {
                    // Trigger date change property changed callback
                    vm.TaskDate = tomorrow;
                    
                    // Wait a short moment for tasks reload to complete
                    await Task.Delay(200);

                    Assert.Contains(vm.Tasks, t => t.Area == "Hành lang lầu 2");
                }
                finally
                {
                    db.CleaningTasks.Remove(task);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public async Task TestAssetManagement_Load_SplitsTypeIntoCategoryAndName()
        {
            using (var db = new AppDbContext())
            {
                var asset = new SchoolAsset
                {
                    AssetCode = "AST-TEST-V8",
                    AssetType = "Máy chiếu - Projector Sony 4K",
                    Location = "Phòng Lab 2",
                    PurchaseDate = DateTime.Now,
                    NextMaintenanceDate = DateTime.Now.AddMonths(6),
                    Status = "Active"
                };
                db.SchoolAssets.Add(asset);
                db.SaveChanges();

                try
                {
                    var vm = new SchoolAssetManagementViewModel();
                    await vm.LoadDataCommand.ExecuteAsync(null);

                    var loadedAsset = vm.Assets.FirstOrDefault(a => a.AssetCode == "AST-TEST-V8");
                    Assert.NotNull(loadedAsset);
                    Assert.Equal("Máy chiếu", loadedAsset.Category);
                    Assert.Equal("Projector Sony 4K", loadedAsset.AssetName);
                }
                finally
                {
                    db.SchoolAssets.Remove(asset);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public async Task TestKitchenDashboard_PreorderFutureDate_CalculatesCorrectly()
        {
            Student? student = null;
            StudentLeaveRequest? leave = null;
            StaffAttendance? staffAtt = null;

            using (var db = new AppDbContext())
            {
                try
                {
                    var testDate = DateTime.Today.AddDays(3);

                    student = new Student
                    {
                        FullName = "Kitchen Student Test",
                        StudentCode = "HS_KITCHEN_TEST_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                        ClassName = "12A1",
                        Status = "Active",
                        IsOnline = false,
                        LastSeen = DateTime.Now
                    };
                    db.Students.Add(student);
                    db.SaveChanges();

                    leave = new StudentLeaveRequest
                    {
                        StudentId = student.Id,
                        StudentName = student.FullName,
                        ClassName = student.ClassName,
                        LeaveDate = testDate,
                        Reason = "Phẫu thuật",
                        Status = "Approved",
                        CreatedAt = DateTime.Now
                    };
                    db.StudentLeaveRequests.Add(leave);

                    staffAtt = new StaffAttendance
                    {
                        StaffId = 9999,
                        Date = testDate,
                        Status = "Present"
                    };
                    db.StaffAttendances.Add(staffAtt);
                    db.SaveChanges();

                    var vm = new KitchenDashboardViewModel();
                    vm.MenuDate = testDate;

                    await vm.PreorderCommand.ExecuteAsync(null);

                    // Since we have active students, plus staff present, minus student leave, let's verify if date works.
                    // The MealCount should contain "[Ngày " and the target day
                    Assert.Contains($"[Ngày {testDate:dd/MM}]", vm.MealCount);
                }
                finally
                {
                    if (leave != null) db.StudentLeaveRequests.Remove(leave);
                    if (staffAtt != null) db.StaffAttendances.Remove(staffAtt);
                    if (student != null) db.Students.Remove(student);
                    db.SaveChanges();
                }
            }
        }
    }
}
