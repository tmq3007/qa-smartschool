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
    public class V24IntegrationTests
    {
        static V24IntegrationTests()
        {
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();

            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.24.0");
        }

        [Fact]
        public async Task TestCanteenPos_AllergyCheckByStudentCode()
        {
            using (var db = new AppDbContext())
            {
                // Create student with StudentCode
                var student = new Student
                {
                    FullName = "HS Allergy Test V24",
                    StudentCode = "HS_ALLERGY_V24",
                    ClassName = "11A1",
                    WalletBalance = 50000m,
                    Status = "Active",
                    IsOnline = false,
                    LastSeen = DateTime.Now
                };
                db.Students.Add(student);
                await db.SaveChangesAsync();

                // Create food allergy linked via StudentCode
                var allergy = new FoodAllergy
                {
                    StudentCode = "HS_ALLERGY_V24",
                    StudentName = "HS Allergy Test V24",
                    Allergen = "Hải sản",
                    Severity = "Severe",
                    ActionPlan = "Sử dụng thuốc kháng histamin nếu vô tình ăn phải"
                };
                db.FoodAllergies.Add(allergy);
                await db.SaveChangesAsync();

                // Create fallback allergy linked via StudentName only (for legacy check)
                var studentFallback = new Student
                {
                    FullName = "HS Allergy Fallback V24",
                    StudentCode = "HS_FALLBACK_V24",
                    ClassName = "11A1",
                    WalletBalance = 50000m,
                    Status = "Active",
                    IsOnline = false,
                    LastSeen = DateTime.Now
                };
                db.Students.Add(studentFallback);
                await db.SaveChangesAsync();

                var allergyFallback = new FoodAllergy
                {
                    StudentCode = string.Empty, // Legacy fallback
                    StudentName = "HS Allergy Fallback V24",
                    Allergen = "Trứng",
                    Severity = "Moderate",
                    ActionPlan = "Tránh xa các món làm từ trứng"
                };
                db.FoodAllergies.Add(allergyFallback);
                await db.SaveChangesAsync();

                try
                {
                    // 1. Test query by StudentCode
                    var vm = new CanteenPosViewModel();
                    vm.SearchCode = "HS_ALLERGY_V24";
                    await vm.SearchStudentCommand.ExecuteAsync(null);

                    Assert.Contains("⚠ CẢNH BÁO DỊ ỨNG", vm.AllergyWarning);
                    Assert.Contains("Hải sản", vm.AllergyWarning);

                    // 2. Test fallback to StudentName (backward compatibility)
                    var vmFallback = new CanteenPosViewModel();
                    vmFallback.SearchCode = "HS_FALLBACK_V24";
                    await vmFallback.SearchStudentCommand.ExecuteAsync(null);

                    Assert.Contains("⚠ CẢNH BÁO DỊ ỨNG", vmFallback.AllergyWarning);
                    Assert.Contains("Trứng", vmFallback.AllergyWarning);
                }
                finally
                {
                    // Cleanup
                    db.FoodAllergies.Remove(allergy);
                    db.FoodAllergies.Remove(allergyFallback);
                    db.Students.Remove(student);
                    db.Students.Remove(studentFallback);
                    await db.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task TestCanteenPos_DoubleSubmitGuard()
        {
            using (var db = new AppDbContext())
            {
                var student = new Student
                {
                    FullName = "HS Canteen POS Guard V24",
                    StudentCode = "HS_GUARD_V24",
                    ClassName = "10A2",
                    WalletBalance = 50000m,
                    Status = "Active",
                    IsOnline = false,
                    LastSeen = DateTime.Now
                };
                db.Students.Add(student);
                await db.SaveChangesAsync();

                try
                {
                    var vm = new CanteenPosViewModel();
                    vm.SearchCode = "HS_GUARD_V24";
                    await vm.SearchStudentCommand.ExecuteAsync(null);

                    // 1. Set deduction amount
                    vm.DeductionAmount = 20000m;
                    
                    // 2. Simulate double submit: Set IsProcessing to true manually before executing
                    vm.IsProcessing = true;
                    await vm.ProcessPaymentCommand.ExecuteAsync(null);

                    // Verify that the wallet balance did NOT change
                    using (var verifyDb = new AppDbContext())
                    {
                        var verified = await verifyDb.Students.FindAsync(student.Id);
                        Assert.NotNull(verified);
                        Assert.Equal(50000m, verified.WalletBalance);
                    }

                    // 3. Reset IsProcessing, execute payment and verify it succeeds
                    vm.IsProcessing = false;
                    await vm.ProcessPaymentCommand.ExecuteAsync(null);

                    using (var verifyDb = new AppDbContext())
                    {
                        var verified = await verifyDb.Students.FindAsync(student.Id);
                        Assert.NotNull(verified);
                        Assert.Equal(30000m, verified.WalletBalance); // Deducted successfully
                    }
                }
                finally
                {
                    db.Students.Remove(student);
                    await db.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task TestTaskManagement_ConflictWarning()
        {
            using (var db = new AppDbContext())
            {
                // 1. Setup teacher profile
                var teacher = new TeacherProfile
                {
                    TeacherCode = "GV_CONFLICT_V24",
                    FullName = "Giáo Viên V24 Conflict",
                    Subject = "Toán",
                    IsActive = true
                };
                db.TeacherProfiles.Add(teacher);
                await db.SaveChangesAsync();

                // 2. Add overlapping entry in Weekly Timetable: Monday (DayOfWeek=2), Period=3
                var existingTimetable = new TimetableEntry
                {
                    TeacherName = "Giáo Viên V24 Conflict",
                    DayOfWeek = 2, // Monday
                    Period = 3,
                    Subject = "Toán Học Chính Khóa",
                    Room = "Phòng 102",
                    RosterId = 1
                };
                db.TimetableEntries.Add(existingTimetable);
                await db.SaveChangesAsync();

                try
                {
                    var vm = new TaskManagementViewModel();
                    vm.TaskTitle = "Công việc trùng lịch V24";
                    vm.Assignee = "Giáo Viên V24 Conflict";
                    vm.DueDate = new DateTime(2026, 6, 8); // Monday
                    vm.SyncPeriod = 3; // Conflicting period
                    vm.SyncRoom = "Phòng Lab V24";

                    var mockUI = QASmartClass.Services.AppServices.UIService as MockUserInterfaceService;
                    Assert.NotNull(mockUI);

                    // 3. Attempting to assign with confirmResult = false (cancel on warning)
                    mockUI.ConfirmResult = false;
                    await vm.AssignCommand.ExecuteAsync(null);

                    // Verify task is NOT created when user rejects warning
                    using (var verifyDb = new AppDbContext())
                    {
                        var createdTask = await verifyDb.DailyTasks.FirstOrDefaultAsync(t => t.Title == "Công việc trùng lịch V24" && t.AssignedTo == "Giáo Viên V24 Conflict");
                        Assert.Null(createdTask);
                    }

                    // 4. Setting confirmResult = true (accept warning) must succeed
                    mockUI.ConfirmResult = true;
                    await vm.AssignCommand.ExecuteAsync(null);

                    // Verify task and timetable synced correctly
                    using (var verifyDb = new AppDbContext())
                    {
                        var createdTask = await verifyDb.DailyTasks.FirstOrDefaultAsync(t => t.Title == "Công việc trùng lịch V24" && t.AssignedTo == "Giáo Viên V24 Conflict");
                        Assert.NotNull(createdTask);

                        var syncedEntry = await verifyDb.TimetableEntries.FirstOrDefaultAsync(e => e.Subject.StartsWith($"[Task#{createdTask.Id}]"));
                        Assert.NotNull(syncedEntry);
                        Assert.Equal(3, syncedEntry.Period); // Saved at period 3 since conflict was overridden

                        // Cleanup in-between task
                        verifyDb.DailyTasks.Remove(createdTask);
                        verifyDb.TimetableEntries.Remove(syncedEntry);
                        await verifyDb.SaveChangesAsync();
                    }
                }
                finally
                {
                    db.TeacherProfiles.Remove(teacher);
                    db.TimetableEntries.Remove(existingTimetable);
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
