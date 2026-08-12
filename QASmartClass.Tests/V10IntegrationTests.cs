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
    public class V10IntegrationTests
    {
        static V10IntegrationTests()
        {
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();

            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.24.0");
            QASmartClass.Staff.Services.ServiceRegistration.Initialize();
        }

        [Fact]
        public async Task TestCanteenPos_CaseInsensitiveSearch_FindsStudent()
        {
            using (var db = new AppDbContext())
            {
                string uniqueCodeUpper = "HS_V10_C_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                string uniqueCodeLower = uniqueCodeUpper.ToLower();

                var student = new Student
                {
                    FullName = "Canteen Case Insensitive Test",
                    StudentCode = uniqueCodeUpper,
                    ClassName = "12A1",
                    IsOnline = false,
                    LastSeen = DateTime.Now,
                    WalletBalance = 100000,
                    Status = "Active"
                };
                db.Students.Add(student);
                try { db.SaveChanges(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V10IntegrationTests] DB save error: {ex.Message}"); throw; }

                try
                {
                    var vm = new CanteenPosViewModel();
                    
                    // Search using lowercase code
                    vm.SearchCode = uniqueCodeLower;
                    await vm.SearchStudentCommand.ExecuteAsync(null);

                    Assert.NotNull(vm.CurrentStudent);
                    Assert.Equal(student.FullName, vm.CurrentStudent.FullName);
                    Assert.Equal(100000, vm.CurrentBalance);
                }
                finally
                {
                    db.Students.Remove(student);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public async Task TestIncidentManagement_CaseInsensitiveLookup_FindsStudent()
        {
            using (var db = new AppDbContext())
            {
                string uniqueCodeUpper = "HS_V10_I_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                string uniqueCodeLower = uniqueCodeUpper.ToLower();

                var student = new Student
                {
                    FullName = "Incident Case Insensitive Test",
                    StudentCode = uniqueCodeUpper,
                    ClassName = "12A2",
                    IsOnline = false,
                    LastSeen = DateTime.Now,
                    Status = "Active"
                };
                db.Students.Add(student);
                try { db.SaveChanges(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V10IntegrationTests] DB save error: {ex.Message}"); throw; }

                try
                {
                    var vm = new IncidentManagementViewModel();
                    
                    // Trigger search using lowercase code
                    vm.StudentCode = uniqueCodeLower;
                    await Task.Delay(200); // Allow async look-up to run

                    Assert.Contains(student.FullName, vm.StudentNameDisplay);
                    Assert.Contains(student.ClassName, vm.StudentNameDisplay);
                }
                finally
                {
                    db.Students.Remove(student);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public async Task TestDocumentRouting_RejectionRequiresComment()
        {
            var prevUser = QASmartClass.Staff.Services.StaffSession.CurrentUser;
            QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { TeacherCode = "ADMIN_V10", FullName = "Admin", Role = "Admin", IsActive = true });

            using (var db = new AppDbContext())
            {
                var doc = new DocumentRoute
                {
                    DocumentTitle = "Test Rejection Requires Comment",
                    Sender = "Admin",
                    Receiver = "BGH",
                    Notes = "Some notes",
                    Status = "Pending",
                    SentAt = DateTime.Now,
                    MaxApprovalLevel = 1,
                    CurrentApprovalLevel = 0
                };
                db.DocumentRoutes.Add(doc);
                try { db.SaveChanges(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V10IntegrationTests] DB save error: {ex.Message}"); throw; }

                try
                {
                    var vm = new DocumentRoutingViewModel();
                    
                    // Attempt rejection without comment (empty)
                    vm.ApproverComment = "";
                    await vm.RejectCommand.ExecuteAsync(doc.Id);

                    // Verify document is still Pending (rejection should fail)
                    var checkDoc = await db.DocumentRoutes.FindAsync(doc.Id);
                    Assert.NotNull(checkDoc);
                    await db.Entry(checkDoc).ReloadAsync();
                    Assert.Equal("Pending", checkDoc.Status);

                    // Set short comment (less than 5 chars)
                    vm.ApproverComment = "Ngắn";
                    await vm.RejectCommand.ExecuteAsync(doc.Id);

                    // Verify still Pending
                    await db.Entry(checkDoc).ReloadAsync();
                    Assert.Equal("Pending", checkDoc.Status);

                    // Set valid comment and reject
                    vm.ApproverComment = "Ý kiến từ chối đầy đủ hơn.";
                    await vm.RejectCommand.ExecuteAsync(doc.Id);

                    // Verify Rejected
                    await db.Entry(checkDoc).ReloadAsync();
                    Assert.Equal("Rejected", checkDoc.Status);
                }
                finally
                {
                    db.DocumentRoutes.Remove(doc);
                    try { db.SaveChanges(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V10IntegrationTests] DB save error: {ex.Message}"); throw; }

                    if (prevUser != null)
                        QASmartClass.Staff.Services.StaffSession.Login(prevUser);
                    else
                        QASmartClass.Staff.Services.StaffSession.Logout();
                }
            }
        }

        [Fact]
        public async Task TestDocumentRouting_SearchFilter_ReturnsCorrectDocs()
        {
            using (var db = new AppDbContext())
            {
                string uniqueTitle1 = "Unique Doc Title One - " + Guid.NewGuid().ToString("N").Substring(0, 6);
                string uniqueTitle2 = "Unique Doc Title Two - " + Guid.NewGuid().ToString("N").Substring(0, 6);

                var doc1 = new DocumentRoute
                {
                    DocumentTitle = uniqueTitle1,
                    Sender = "SenderOne",
                    Receiver = "ReceiverOne",
                    Notes = "Notes",
                    Status = "Pending",
                    SentAt = DateTime.Now
                };
                var doc2 = new DocumentRoute
                {
                    DocumentTitle = uniqueTitle2,
                    Sender = "SenderTwo",
                    Receiver = "ReceiverTwo",
                    Notes = "Notes",
                    Status = "Pending",
                    SentAt = DateTime.Now
                };
                db.DocumentRoutes.AddRange(doc1, doc2);
                try { db.SaveChanges(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V10IntegrationTests] DB save error: {ex.Message}"); throw; }

                try
                {
                    var vm = new DocumentRoutingViewModel();
                    
                    // Filter by title 1 keyword
                    vm.SearchText = uniqueTitle1.Substring(0, 18);
                    await vm.LoadDocsCommand.ExecuteAsync(null);

                    Assert.Contains(vm.Documents, d => d.DocumentTitle == uniqueTitle1);
                    Assert.DoesNotContain(vm.Documents, d => d.DocumentTitle == uniqueTitle2);

                    // Filter by SenderTwo keyword
                    vm.SearchText = "SenderTwo";
                    await vm.LoadDocsCommand.ExecuteAsync(null);

                    Assert.Contains(vm.Documents, d => d.DocumentTitle == uniqueTitle2);
                    Assert.DoesNotContain(vm.Documents, d => d.DocumentTitle == uniqueTitle1);
                }
                finally
                {
                    db.DocumentRoutes.Remove(doc1);
                    db.DocumentRoutes.Remove(doc2);
                    try { db.SaveChanges(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V10IntegrationTests] DB save error: {ex.Message}"); throw; }
                }
            }
        }

        [Fact]
        public async Task TestLeaveRequest_ClassFilter_FiltersRequestsCorrectly()
        {
            using (var db = new AppDbContext())
            {
                string uniqueClass1 = "V10_" + Guid.NewGuid().ToString("N").Substring(0, 4);
                string uniqueClass2 = "V10_" + Guid.NewGuid().ToString("N").Substring(0, 4);

                var student1 = new Student
                {
                    FullName = "Student One Filter Class",
                    StudentCode = "HS_V10_F1_" + Guid.NewGuid().ToString("N").Substring(0, 4),
                    ClassName = uniqueClass1,
                    IsOnline = false,
                    LastSeen = DateTime.Now,
                    Status = "Active"
                };
                var student2 = new Student
                {
                    FullName = "Student Two Filter Class",
                    StudentCode = "HS_V10_F2_" + Guid.NewGuid().ToString("N").Substring(0, 4),
                    ClassName = uniqueClass2,
                    IsOnline = false,
                    LastSeen = DateTime.Now,
                    Status = "Active"
                };
                db.Students.AddRange(student1, student2);
                try { db.SaveChanges(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V10IntegrationTests] DB save error: {ex.Message}"); throw; }

                var req1 = new StudentLeaveRequest
                {
                    StudentId = student1.Id,
                    StudentName = student1.FullName,
                    ClassName = student1.ClassName,
                    LeaveDate = DateTime.Today,
                    Reason = "Cảm sốt",
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };
                var req2 = new StudentLeaveRequest
                {
                    StudentId = student2.Id,
                    StudentName = student2.FullName,
                    ClassName = student2.ClassName,
                    LeaveDate = DateTime.Today,
                    Reason = "Đi du lịch",
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };
                db.StudentLeaveRequests.AddRange(req1, req2);
                try { db.SaveChanges(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V10IntegrationTests] DB save error: {ex.Message}"); throw; }

                try
                {
                    var vm = new LeaveRequestManagementViewModel();
                    
                    // Default filter is "Tất cả các lớp"
                    vm.SelectedClassFilter = "Tất cả các lớp";
                    await vm.LoadRequestsCommand.ExecuteAsync(null);
                    Assert.Contains(vm.Requests, r => r.StudentName == student1.FullName);
                    Assert.Contains(vm.Requests, r => r.StudentName == student2.FullName);

                    // Filter by class 1
                    vm.SelectedClassFilter = uniqueClass1;
                    await vm.LoadRequestsCommand.ExecuteAsync(null);
                    Assert.Contains(vm.Requests, r => r.StudentName == student1.FullName);
                    Assert.DoesNotContain(vm.Requests, r => r.StudentName == student2.FullName);
                }
                finally
                {
                    db.StudentLeaveRequests.Remove(req1);
                    db.StudentLeaveRequests.Remove(req2);
                    db.Students.Remove(student1);
                    db.Students.Remove(student2);
                    try { db.SaveChanges(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V10IntegrationTests] DB save error: {ex.Message}"); throw; }
                }
            }
        }

        [Fact]
        public void TestDocumentManager_LocalizationProperties_TranslateCorrectly()
        {
            var display = new OfficialDocumentDisplay
            {
                Type = "Incoming",
                Status = "Pending"
            };

            Assert.Equal("Công văn đến", display.FriendlyType);
            Assert.Equal("Chờ xử lý", display.FriendlyStatus);

            display.Type = "Outgoing";
            display.Status = "Approved";

            Assert.Equal("Công văn đi", display.FriendlyType);
            Assert.Equal("Đã duyệt", display.FriendlyStatus);

            display.Type = "Internal";
            Assert.Equal("Nội bộ", display.FriendlyType);
        }
    }
}
