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
    public class V13IntegrationTests
    {
        static V13IntegrationTests()
        {
            QASmartClass.Staff.Services.ServiceRegistration.Initialize();
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();

            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.24.0");
        }

        [Fact]
        public async Task TestDocumentRouting_AuthorizedUser_AllowsApproval()
        {
            using (var db = new AppDbContext())
            {
                // Setup document
                string docTitle = "Doc_Auth_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                var doc = new DocumentRoute
                {
                    DocumentTitle = docTitle,
                    Sender = "Staff A",
                    Receiver = "Ban Giám Hiệu",
                    Notes = "Test notes",
                    Status = "Pending",
                    SentAt = DateTime.Now,
                    MaxApprovalLevel = 1,
                    CurrentApprovalLevel = 0
                };
                db.DocumentRoutes.Add(doc);
                await db.SaveChangesAsync();

                try
                {
                    // Đăng nhập là Hiệu trưởng (Hợp lệ cho Ban Giám Hiệu)
                    QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { FullName = "Principal User", Role = "HieuTruong" });

                    var vm = new DocumentRoutingViewModel();
                    await vm.ApproveCommand.ExecuteAsync(doc.Id);

                    using var verifyDb = new AppDbContext();
                    var approvedDoc = await verifyDb.DocumentRoutes.FindAsync(doc.Id);
                    Assert.Equal("Approved", approvedDoc!.Status);
                }
                finally
                {
                    using var cleanupDb = new AppDbContext();
                    var inDb = await cleanupDb.DocumentRoutes.FindAsync(doc.Id);
                    if (inDb != null) cleanupDb.DocumentRoutes.Remove(inDb);
                    await cleanupDb.SaveChangesAsync();
                    QASmartClass.Staff.Services.StaffSession.Logout();
                }
            }
        }

        [Fact]
        public async Task TestDocumentRouting_UnauthorizedUser_BlocksAction()
        {
            using (var db = new AppDbContext())
            {
                // Setup document
                string docTitle = "Doc_Unauth_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                var doc = new DocumentRoute
                {
                    DocumentTitle = docTitle,
                    Sender = "Staff A",
                    Receiver = "Ban Giám Hiệu",
                    Notes = "Test notes",
                    Status = "Pending",
                    SentAt = DateTime.Now,
                    MaxApprovalLevel = 1,
                    CurrentApprovalLevel = 0
                };
                db.DocumentRoutes.Add(doc);
                await db.SaveChangesAsync();

                try
                {
                    // Đăng nhập là Lao công (Không có quyền duyệt cho Ban Giám Hiệu)
                    QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { FullName = "Janitor User", Role = "LaoCong" });

                    var vm = new DocumentRoutingViewModel();
                    await vm.ApproveCommand.ExecuteAsync(doc.Id);

                    using var verifyDb = new AppDbContext();
                    var approvedDoc = await verifyDb.DocumentRoutes.FindAsync(doc.Id);
                    Assert.Equal("Pending", approvedDoc!.Status); // Blocked
                }
                finally
                {
                    using var cleanupDb = new AppDbContext();
                    var inDb = await cleanupDb.DocumentRoutes.FindAsync(doc.Id);
                    if (inDb != null) cleanupDb.DocumentRoutes.Remove(inDb);
                    await cleanupDb.SaveChangesAsync();
                    QASmartClass.Staff.Services.StaffSession.Logout();
                }
            }
        }

        [Fact]
        public async Task TestCanteenPos_AllergyWarning_PopulatesWarning()
        {
            using (var db = new AppDbContext())
            {
                string studentName = "StudentAllergy_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                string studentCode = "SC_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                var student = new Student
                {
                    FullName = studentName,
                    StudentCode = studentCode,
                    WalletBalance = 200000m,
                    Status = "Active"
                };
                var allergy = new FoodAllergy
                {
                    StudentName = studentName,
                    Allergen = "Đậu phộng",
                    Severity = "Nghiêm trọng",
                    ActionPlan = "Dùng bút tiêm EpiPen khẩn cấp"
                };
                db.Students.Add(student);
                db.FoodAllergies.Add(allergy);
                await db.SaveChangesAsync();

                try
                {
                    var vm = new CanteenPosViewModel();
                    vm.SearchCode = studentCode;
                    await vm.SearchStudentCommand.ExecuteAsync(null);

                    Assert.Contains("Đậu phộng", vm.AllergyWarning);
                    Assert.Contains("Nghiêm trọng", vm.AllergyWarning);
                }
                finally
                {
                    using var cleanupDb = new AppDbContext();
                    var sInDb = await cleanupDb.Students.FirstOrDefaultAsync(s => s.StudentCode == studentCode);
                    if (sInDb != null) cleanupDb.Students.Remove(sInDb);
                    var aInDb = await cleanupDb.FoodAllergies.FirstOrDefaultAsync(a => a.StudentName == studentName);
                    if (aInDb != null) cleanupDb.FoodAllergies.Remove(aInDb);
                    await cleanupDb.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task TestLeaveRequest_Approve_SavesExcusedStatus()
        {
            var prevUser = QASmartClass.Staff.Services.StaffSession.CurrentUser;
            QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { FullName = "Teacher Log", Role = "GV", IsActive = true });

            using (var db = new AppDbContext())
            {
                string studentName = "StudentLeave_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                string studentCode = "SC_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                
                var student = new Student
                {
                    FullName = studentName,
                    StudentCode = studentCode,
                    Status = "Active"
                };
                db.Students.Add(student);
                await db.SaveChangesAsync();

                var roster = new ClassRoster
                {
                    ClassName = "10A1",
                    IsActive = true,
                    SchoolYear = "2025-2026"
                };
                db.ClassRosters.Add(roster);
                await db.SaveChangesAsync();

                var crs = new ClassRosterStudent
                {
                    RosterId = roster.Id,
                    StudentId = student.Id
                };
                db.ClassRosterStudents.Add(crs);
                await db.SaveChangesAsync();

                var req = new StudentLeaveRequest
                {
                    StudentId = student.Id,
                    StudentName = studentName,
                    LeaveDate = DateTime.Today,
                    Reason = "Bị sốt",
                    Status = "Pending"
                };
                db.StudentLeaveRequests.Add(req);
                await db.SaveChangesAsync();

                try
                {
                    var vm = new LeaveRequestManagementViewModel();
                    await vm.ApproveCommand.ExecuteAsync(req.Id);

                    using var verifyDb = new AppDbContext();
                    var attendance = await verifyDb.AttendanceRecords
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.StudentId == student.Id && a.Date == DateTime.Today);
                    
                    Assert.NotNull(attendance);
                    Assert.Equal("excused", attendance!.Status);
                }
                finally
                {
                    if (prevUser != null)
                        QASmartClass.Staff.Services.StaffSession.Login(prevUser);
                    else
                        QASmartClass.Staff.Services.StaffSession.Logout();

                    using var cleanupDb = new AppDbContext();
                    var rInDb = await cleanupDb.StudentLeaveRequests.FindAsync(req.Id);
                    if (rInDb != null) cleanupDb.StudentLeaveRequests.Remove(rInDb);
                    
                    var crsInDb = await cleanupDb.ClassRosterStudents.FirstOrDefaultAsync(c => c.RosterId == roster.Id && c.StudentId == student.Id);
                    if (crsInDb != null) cleanupDb.ClassRosterStudents.Remove(crsInDb);

                    var rosterInDb = await cleanupDb.ClassRosters.FindAsync(roster.Id);
                    if (rosterInDb != null) cleanupDb.ClassRosters.Remove(rosterInDb);

                    var studentInDb = await cleanupDb.Students.FindAsync(student.Id);
                    if (studentInDb != null) cleanupDb.Students.Remove(studentInDb);

                    var aInDb = await cleanupDb.AttendanceRecords.FirstOrDefaultAsync(a => a.StudentId == student.Id && a.Date == DateTime.Today);
                    if (aInDb != null) cleanupDb.AttendanceRecords.Remove(aInDb);

                    await cleanupDb.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task TestLeaveRequest_ApproveAndReject_WritesAuditLogs()
        {
            using (var db = new AppDbContext())
            {
                string studentName = "StudentAudit_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                var req1 = new StudentLeaveRequest { StudentName = studentName, LeaveDate = DateTime.Today, Reason = "Sick", Status = "Pending" };
                var req2 = new StudentLeaveRequest { StudentName = studentName, LeaveDate = DateTime.Today.AddDays(1), Reason = "Travel", Status = "Pending" };
                db.StudentLeaveRequests.AddRange(req1, req2);
                await db.SaveChangesAsync();

                try
                {
                    QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { FullName = "Teacher Log", Role = "GV", TeacherCode = "TC_GV_13" });

                    var vm = new LeaveRequestManagementViewModel();
                    await vm.ApproveCommand.ExecuteAsync(req1.Id);
                    vm.RejectReason = "Không đồng ý phép lý do này";
                    await vm.RejectCommand.ExecuteAsync(req2.Id);

                    using var verifyDb = new AppDbContext();
                    var logs = await verifyDb.AuditLogs
                        .AsNoTracking()
                        .Where(l => l.Details.Contains(studentName))
                        .ToListAsync();

                    Assert.Contains(logs, l => l.Action == "Approve_LeaveRequest");
                    Assert.Contains(logs, l => l.Action == "Reject_LeaveRequest" && l.Details.Contains("Không đồng ý"));
                }
                finally
                {
                    using var cleanupDb = new AppDbContext();
                    var r1 = await cleanupDb.StudentLeaveRequests.FindAsync(req1.Id); if (r1 != null) cleanupDb.StudentLeaveRequests.Remove(r1);
                    var r2 = await cleanupDb.StudentLeaveRequests.FindAsync(req2.Id); if (r2 != null) cleanupDb.StudentLeaveRequests.Remove(r2);
                    var logs = await cleanupDb.AuditLogs.Where(l => l.Details.Contains(studentName)).ToListAsync();
                    cleanupDb.AuditLogs.RemoveRange(logs);
                    await cleanupDb.SaveChangesAsync();
                    QASmartClass.Staff.Services.StaffSession.Logout();
                }
            }
        }

        [Fact]
        public async Task TestOverview_CanteenDeductionRevenue_CalculatesCorrectly()
        {
            using (var db = new AppDbContext())
            {
                var today = DateTime.Today;
                var existingTodayLogs = db.EventLogs.Where(l => l.EventType == "WalletTransaction" && l.Timestamp >= today).ToList();
                db.EventLogs.RemoveRange(existingTodayLogs);
                await db.SaveChangesAsync();

                // Setup logs
                string idSuffix = Guid.NewGuid().ToString("N").Substring(0, 4);
                var log1 = new EventLog
                {
                    Timestamp = DateTime.Now,
                    EventType = "WalletTransaction",
                    Actor = "Canteen",
                    Details = "{\"Type\":\"Canteen_Deduction\",\"Amount\":50000.0}"
                };
                var log2 = new EventLog
                {
                    Timestamp = DateTime.Now,
                    EventType = "WalletTransaction",
                    Actor = "Canteen",
                    Details = "{\"Type\":\"Wallet_TopUp\",\"Amount\":100000.0}" // Should be ignored
                };
                db.EventLogs.AddRange(log1, log2);
                await db.SaveChangesAsync();

                try
                {
                    var vm = new StaffOverviewViewModel();
                    await vm.LoadDataCommand.ExecuteAsync(null);

                    // Doanh thu chỉ được cộng dồn từ Canteen_Deduction (50000), bỏ qua TopUp
                    Assert.Equal("50K", vm.TodayRevenue);
                }
                finally
                {
                    using var cleanupDb = new AppDbContext();
                    var logs = await cleanupDb.EventLogs
                        .Where(l => l.Details.Contains("Canteen_Deduction") || l.Details.Contains("Wallet_TopUp"))
                        .ToListAsync();
                    cleanupDb.EventLogs.RemoveRange(logs);
                    await cleanupDb.SaveChangesAsync();
                }
            }
        }
    }
}
