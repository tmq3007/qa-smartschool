using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.ViewModels;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace QASmartClass.Tests
{
    public class V11IntegrationTests
    {
        static V11IntegrationTests()
        {
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();

            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.24.0");
        }

        [Fact]
        public async Task TestPushNotification_ClassAndGradeTargeting_LogsCorrectly()
        {
            using (var db = new AppDbContext())
            {
                var vm = new PushNotificationCenterViewModel();
                vm.Title = "Class Targeting Test";
                vm.Body = "Body content";
                vm.SelectedType = "Academic";
                vm.SelectedTarget = "ParentClass";
                
                // Let's seed unique class name to test
                string uniqueClass = "V11Class_" + Guid.NewGuid().ToString("N").Substring(0, 4);
                vm.TargetClassList.Add(uniqueClass);
                vm.SelectedTargetClass = uniqueClass;

                await vm.SendPushCommand.ExecuteAsync(null);

                // Verify log was recorded with TargetClass
                var log = await db.PushMessageLogs
                    .Where(l => l.Title == "Class Targeting Test" && l.TargetClass == uniqueClass)
                    .FirstOrDefaultAsync();

                Assert.NotNull(log);
                Assert.Equal("Parent", log.RecipientRole);
                Assert.Equal(uniqueClass, log.TargetClass);
                Assert.Equal("Academic", log.Type);

                if (log != null)
                {
                    db.PushMessageLogs.Remove(log);
                    await db.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public void TestMobileApp_LocalizationMapping_DialsVietnamese()
        {
            var display = new MobileTokenDisplay
            {
                Role = "Parent",
                Platform = "ios",
                IsActive = true
            };

            Assert.Equal("Phụ huynh", display.FriendlyRole);
            Assert.Equal("iOS", display.FriendlyPlatform);
            Assert.Equal("Đang hoạt động", display.FriendlyStatus);

            display.Role = "Student";
            display.Platform = "Android";
            display.IsActive = false;

            Assert.Equal("Học sinh", display.FriendlyRole);
            Assert.Equal("Android", display.FriendlyPlatform);
            Assert.Equal("Đã hủy kết nối", display.FriendlyStatus);
        }

        [Fact]
        public async Task TestIncidentManagement_EditIncident_UpdatesDatabase()
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

                string uniqueDesc = "Original incident description " + Guid.NewGuid().ToString("N");
                var incidentLog = new EventLog
                {
                    EventType = "Incident",
                    Actor = "TestStaffUser",
                    Timestamp = DateTime.Now,
                    Details = System.Text.Json.JsonSerializer.Serialize(new { Severity = "Medium", StudentCode = "HS001", Description = uniqueDesc })
                };
                db.EventLogs.Add(incidentLog);
                await db.SaveChangesAsync();

                try
                {
                    // Mock CurrentUser
                    QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { TeacherCode = "TestStaffUser", FullName = "TestStaffUser", Role = "GV" });

                    var vm = new IncidentManagementViewModel();
                    await vm.LoadIncidentsCommand.ExecuteAsync(null);

                    var item = vm.Incidents.FirstOrDefault(i => i.Description == uniqueDesc);
                    Assert.NotNull(item);

                    // Start editing
                    vm.SelectForEditCommand.Execute(item);
                    Assert.True(vm.IsEditing);
                    Assert.Equal(uniqueDesc, vm.Description);

                    // Update description
                    string updatedDesc = "Updated incident description " + Guid.NewGuid().ToString("N");
                    vm.Description = updatedDesc;

                    // Save
                    await vm.ReportIncidentCommand.ExecuteAsync(null);

                    // Verify it was updated in CSDL
                    var updatedLog = await db.EventLogs.FindAsync(incidentLog.Id);
                    Assert.NotNull(updatedLog);
                    await db.Entry(updatedLog).ReloadAsync();

                    using var doc = System.Text.Json.JsonDocument.Parse(updatedLog.Details);
                    var root = doc.RootElement;
                    string desc = root.GetProperty("Description").GetString() ?? "";
                    Assert.Equal(updatedDesc, desc);
                }
                finally
                {
                    db.EventLogs.Remove(incidentLog);
                    if (createdStudent && student != null)
                    {
                        db.Students.Remove(student);
                    }
                    await db.SaveChangesAsync();
                    QASmartClass.Staff.Services.StaffSession.Logout();
                }
            }
        }

        [Fact]
        public async Task TestIncidentManagement_DeleteIncident_RemovesFromDatabase()
        {
            using (var db = new AppDbContext())
            {
                string uniqueDesc = "Delete incident test " + Guid.NewGuid().ToString("N");
                var incidentLog = new EventLog
                {
                    EventType = "Incident",
                    Actor = "DeleteStaffUser",
                    Timestamp = DateTime.Now,
                    Details = System.Text.Json.JsonSerializer.Serialize(new { Severity = "Low", StudentCode = "HS002", Description = uniqueDesc })
                };
                db.EventLogs.Add(incidentLog);
                await db.SaveChangesAsync();

                try
                {
                    // Mock CurrentUser
                    QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { TeacherCode = "DeleteStaffUser", FullName = "DeleteStaffUser", Role = "GV" });

                    var vm = new IncidentManagementViewModel();
                    await vm.LoadIncidentsCommand.ExecuteAsync(null);

                    var item = vm.Incidents.FirstOrDefault(i => i.Description == uniqueDesc);
                    Assert.NotNull(item);

                    // Trigger Delete
                    await vm.DeleteIncidentCommand.ExecuteAsync(item.Id);

                    // Verify it is gone from CSDL
                    var deletedLog = await db.EventLogs.AsNoTracking().FirstOrDefaultAsync(e => e.Id == incidentLog.Id);
                    Assert.Null(deletedLog);
                }
                finally
                {
                    QASmartClass.Staff.Services.StaffSession.Logout();
                }
            }
        }

        [Fact]
        public async Task TestIncidentManagement_OwnershipValidation_DeniesUnauthorizedUsers()
        {
            using (var db = new AppDbContext())
            {
                string uniqueDesc = "Security ownership test " + Guid.NewGuid().ToString("N");
                var incidentLog = new EventLog
                {
                    EventType = "Incident",
                    Actor = "Teacher A",
                    Timestamp = DateTime.Now,
                    Details = System.Text.Json.JsonSerializer.Serialize(new { Severity = "Low", StudentCode = "HS003", Description = uniqueDesc })
                };
                db.EventLogs.Add(incidentLog);
                await db.SaveChangesAsync();

                Student? testStudent = null;
                bool isNewStudent = false;
                try
                {
                    testStudent = await db.Students.FirstOrDefaultAsync(s => s.StudentCode == "HS003");
                    if (testStudent == null)
                    {
                        testStudent = new Student { StudentCode = "HS003", FullName = "Student HS003", ClassName = "10A1", ClassroomId = 1, Status = "Active" };
                        db.Students.Add(testStudent);
                        await db.SaveChangesAsync();
                        isNewStudent = true;
                    }
                    else
                    {
                        testStudent.ClassName = "10A1";
                        await db.SaveChangesAsync();
                    }

                    // Mock CurrentUser as "Teacher B" (Unauthorized but GVCN of 10A1 so they can view it)
                    QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { TeacherCode = "Teacher B", FullName = "Teacher B", Role = "GV", Notes = "GV chủ nhiệm 10A1" });

                    var vm = new IncidentManagementViewModel();
                    await vm.LoadIncidentsCommand.ExecuteAsync(null);

                    var item = vm.Incidents.FirstOrDefault(i => i.Description == uniqueDesc);
                    Assert.NotNull(item);

                    // Attempt SelectForEdit
                    vm.SelectForEditCommand.Execute(item);
                    Assert.False(vm.IsEditing); // Editing mode should not activate

                    // Attempt Delete
                    await vm.DeleteIncidentCommand.ExecuteAsync(item.Id);

                    // Verify database record still exists (deletion was blocked)
                    var logAfter = await db.EventLogs.FindAsync(incidentLog.Id);
                    Assert.NotNull(logAfter);
                }
                finally
                {
                    db.EventLogs.Remove(incidentLog);
                    if (isNewStudent && testStudent != null)
                    {
                        db.Students.Remove(testStudent);
                    }
                    await db.SaveChangesAsync();
                    QASmartClass.Staff.Services.StaffSession.Logout();
                }
            }
        }

        [Fact]
        public async Task TestCanteenPos_ItemizedTransactionAndPush_LogsOrderDetails()
        {
            using (var db = new AppDbContext())
            {
                string uniqueCode = "HS_V11_C_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                var student = new Student
                {
                    FullName = "Canteen Itemized Test Student",
                    StudentCode = uniqueCode,
                    ClassName = "10A1",
                    IsOnline = false,
                    LastSeen = DateTime.Now,
                    WalletBalance = 200000,
                    Status = "Active"
                };
                db.Students.Add(student);
                await db.SaveChangesAsync();

                try
                {
                    var vm = new CanteenPosViewModel();
                    vm.SearchCode = uniqueCode;
                    await vm.SearchStudentCommand.ExecuteAsync(null);

                    vm.DeductionAmount = 30000;
                    vm.OrderDetails = "Cơm trưa + Sữa hạt";
                    vm.AutoPrintReceipt = false; // Disable popup in unit test environment

                    // Process Payment
                    await vm.ProcessPaymentCommand.ExecuteAsync(null);

                    // Verify transaction was logged with Items
                    var log = await db.EventLogs
                        .Where(l => l.EventType == "WalletTransaction" && l.Actor == uniqueCode)
                        .OrderByDescending(l => l.Timestamp)
                        .FirstOrDefaultAsync();

                    Assert.NotNull(log);
                    using var doc = System.Text.Json.JsonDocument.Parse(log.Details);
                    var root = doc.RootElement;
                    string items = root.GetProperty("Items").GetString() ?? "";
                    Assert.Equal("Cơm trưa + Sữa hạt", items);

                    // Verify push notification contains item details
                    var pushLog = await db.PushMessageLogs
                        .Where(p => p.RecipientId == student.Id && p.RecipientRole == "Parent")
                        .OrderByDescending(p => p.SentAt)
                        .FirstOrDefaultAsync();

                    Assert.NotNull(pushLog);
                    Assert.Contains("Cơm trưa + Sữa hạt", pushLog.Body);

                    if (log != null) db.EventLogs.Remove(log);
                    if (pushLog != null) db.PushMessageLogs.Remove(pushLog);
                    await db.SaveChangesAsync();
                }
                finally
                {
                    db.Students.Remove(student);
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
