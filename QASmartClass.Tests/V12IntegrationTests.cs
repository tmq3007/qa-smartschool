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
    public class V12IntegrationTests
    {
        static V12IntegrationTests()
        {
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();

            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.24.0");
            QASmartClass.Staff.Services.ServiceRegistration.Initialize();
        }

        [Fact]
        public async Task TestPayroll_AdminCanManage_AllowsCalculation()
        {
            using (var db = new AppDbContext())
            {
                // 1. Setup profile nhân sự & staff để tính lương
                string testStaffName = "GV_PayrollTest_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                var teacher = new TeacherProfile
                {
                    FullName = testStaffName,
                    TeacherCode = "TC_" + Guid.NewGuid().ToString("N").Substring(0, 4),
                    TeacherPassword = "password",
                    Role = "GV",
                    IsActive = true
                };
                var staff = new StaffProfile
                {
                    FullName = testStaffName,
                    StaffCode = teacher.TeacherCode,
                    Department = "ToanTin",
                    Position = "GiaoVien",
                    BaseSalary = 10000000,
                    JoinedDate = DateTime.Now
                };
                db.TeacherProfiles.Add(teacher);
                db.StaffProfiles.Add(staff);
                try { await db.SaveChangesAsync(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V12IntegrationTests] DB save error: {ex.Message}"); throw; }

                try
                {
                    // 2. Đăng nhập với quyền Admin (được phép quản lý lương)
                    QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { FullName = "Admin User", Role = "Admin" });

                    var vm = new PayrollViewModel();
                    vm.StaffName = testStaffName;
                    vm.SelectedMonth = "6";
                    vm.CalculateYear = "2026";

                    // 3. Thực thi tính lương
                    await vm.CalculateCommand.ExecuteAsync(null);

                    // 4. Kiểm tra dữ liệu được thêm vào database qua context mới cô lập
                    using var verifyDb = new AppDbContext();
                    var record = await verifyDb.PayrollRecords
                        .AsNoTracking()
                        .Where(p => p.StaffName == testStaffName && p.Month == 6 && p.Year == 2026)
                        .FirstOrDefaultAsync();

                    Assert.NotNull(record);
                }
                finally
                {
                    using var cleanupDb = new AppDbContext();
                    var teacherInDb = await cleanupDb.TeacherProfiles.FirstOrDefaultAsync(t => t.FullName == testStaffName);
                    if (teacherInDb != null) cleanupDb.TeacherProfiles.Remove(teacherInDb);
                    var staffInDb = await cleanupDb.StaffProfiles.FirstOrDefaultAsync(s => s.FullName == testStaffName);
                    if (staffInDb != null) cleanupDb.StaffProfiles.Remove(staffInDb);
                    var recordInDb = await cleanupDb.PayrollRecords.FirstOrDefaultAsync(p => p.StaffName == testStaffName && p.Month == 6 && p.Year == 2026);
                    if (recordInDb != null) cleanupDb.PayrollRecords.Remove(recordInDb);
                    try { await cleanupDb.SaveChangesAsync(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V12IntegrationTests] DB save error: {ex.Message}"); throw; }
                    QASmartClass.Staff.Services.StaffSession.Logout();
                }
            }
        }

        [Fact]
        public async Task TestPayroll_TeacherCannotManage_DeniesAccess()
        {
            using (var db = new AppDbContext())
            {
                string testStaffName = "GV_Blocked_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                var teacher = new TeacherProfile
                {
                    FullName = testStaffName,
                    TeacherCode = "TC_" + Guid.NewGuid().ToString("N").Substring(0, 4),
                    TeacherPassword = "password",
                    Role = "GV",
                    IsActive = true
                };
                var staff = new StaffProfile
                {
                    FullName = testStaffName,
                    StaffCode = teacher.TeacherCode,
                    Department = "ToanTin",
                    Position = "GiaoVien",
                    BaseSalary = 10000000,
                    JoinedDate = DateTime.Now
                };
                db.TeacherProfiles.Add(teacher);
                db.StaffProfiles.Add(staff);
                try { await db.SaveChangesAsync(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V12IntegrationTests] DB save error: {ex.Message}"); throw; }

                try
                {
                    // 1. Đăng nhập với quyền GV thông thường (Bị chặn quản lý lương)
                    QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { FullName = "Teacher User", Role = "GV" });

                    var vm = new PayrollViewModel();
                    vm.StaffName = testStaffName;
                    vm.SelectedMonth = "6";
                    vm.CalculateYear = "2026";

                    // 2. Thực thi tính lương
                    await vm.CalculateCommand.ExecuteAsync(null);

                    // 3. Xác minh KHÔNG có bản ghi nào được ghi nhận dưới DB qua context mới cô lập
                    using var verifyDb = new AppDbContext();
                    var record = await verifyDb.PayrollRecords
                        .AsNoTracking()
                        .Where(p => p.StaffName == testStaffName && p.Month == 6 && p.Year == 2026)
                        .FirstOrDefaultAsync();

                    Assert.Null(record);
                }
                finally
                {
                    using var cleanupDb = new AppDbContext();
                    var teacherInDb = await cleanupDb.TeacherProfiles.FirstOrDefaultAsync(t => t.FullName == testStaffName);
                    if (teacherInDb != null) cleanupDb.TeacherProfiles.Remove(teacherInDb);
                    var staffInDb = await cleanupDb.StaffProfiles.FirstOrDefaultAsync(s => s.FullName == testStaffName);
                    if (staffInDb != null) cleanupDb.StaffProfiles.Remove(staffInDb);
                    var recordInDb = await cleanupDb.PayrollRecords.FirstOrDefaultAsync(p => p.StaffName == testStaffName && p.Month == 6 && p.Year == 2026);
                    if (recordInDb != null) cleanupDb.PayrollRecords.Remove(recordInDb);
                    try { await cleanupDb.SaveChangesAsync(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V12IntegrationTests] DB save error: {ex.Message}"); throw; }
                    QASmartClass.Staff.Services.StaffSession.Logout();
                }
            }
        }

        [Fact]
        public async Task TestTaskManagement_OwnerCanDelete_RemovesFromDatabase()
        {
            using (var db = new AppDbContext())
            {
                // 1. Đăng nhập giáo viên tạo việc
                string teacherName = "TeacherOwner_" + Guid.NewGuid().ToString("N").Substring(0, 4);
                QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { FullName = teacherName, Role = "GV" });

                // Seed tài khoản người nhận việc để qua được bước xác thực nhân sự tồn tại
                string assigneeCode = "TC_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                var assigneeProfile = new TeacherProfile
                {
                    FullName = "Assignee_TaskTest",
                    TeacherCode = assigneeCode,
                    TeacherPassword = "password",
                    Role = "GV",
                    IsActive = true
                };
                db.TeacherProfiles.Add(assigneeProfile);
                try { await db.SaveChangesAsync(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V12IntegrationTests] DB save error: {ex.Message}"); throw; }

                string taskTitle = "Task_OwnerTest_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                var task = new DailyTask
                {
                    Title = taskTitle,
                    AssignedTo = "Assignee_TaskTest",
                    AssignedBy = teacherName,
                    DueDate = DateTime.Today,
                    Status = "Pending",
                    Department = "ToanTin"
                };
                db.DailyTasks.Add(task);
                await db.SaveChangesAsync();

                try
                {
                    var vm = new TaskManagementViewModel();
                    await vm.LoadTasksCommand.ExecuteAsync(null);

                    var item = vm.Tasks.FirstOrDefault(t => t.Title == taskTitle);
                    Assert.NotNull(item);

                    // 2. Chọn nhiệm vụ và gọi xóa
                    vm.SelectedTask = item;
                    await vm.DeleteTaskCommand.ExecuteAsync(null);

                    // 3. Xác minh nhiệm vụ đã biến mất trong DB thực tế qua context mới cô lập
                    using var verifyDb = new AppDbContext();
                    var deletedTask = await verifyDb.DailyTasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == task.Id);
                    Assert.Null(deletedTask);

                    // 4. Xác minh có log kiểm toán Delete_Task được ghi nhận
                    var auditLog = await verifyDb.AuditLogs
                        .Where(e => e.Action == "Delete_Task" && e.Details.Contains(taskTitle))
                        .FirstOrDefaultAsync();

                    Assert.NotNull(auditLog);
                }
                finally
                {
                    using var cleanupDb = new AppDbContext();
                    var assigneeInDb = await cleanupDb.TeacherProfiles.FirstOrDefaultAsync(t => t.TeacherCode == assigneeCode);
                    if (assigneeInDb != null) cleanupDb.TeacherProfiles.Remove(assigneeInDb);
                    var taskInDb = await cleanupDb.DailyTasks.FindAsync(task.Id);
                    if (taskInDb != null) cleanupDb.DailyTasks.Remove(taskInDb);
                    var auditLogInDb = await cleanupDb.AuditLogs.FirstOrDefaultAsync(e => e.Action == "Delete_Task" && e.Details.Contains(taskTitle));
                    if (auditLogInDb != null) cleanupDb.AuditLogs.Remove(auditLogInDb);
                    try { await cleanupDb.SaveChangesAsync(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V12IntegrationTests] DB save error: {ex.Message}"); throw; }
                    QASmartClass.Staff.Services.StaffSession.Logout();
                }
            }
        }

        [Fact]
        public async Task TestTaskManagement_NonOwnerCannotDelete_BlocksAction()
        {
            using (var db = new AppDbContext())
            {
                string taskTitle = "Task_Blocked_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                var task = new DailyTask
                {
                    Title = taskTitle,
                    AssignedTo = "Assignee_TaskTest",
                    AssignedBy = "Teacher A", // Do Teacher A giao
                    DueDate = DateTime.Today,
                    Status = "Pending",
                    Department = "ToanTin"
                };
                db.DailyTasks.Add(task);
                try { await db.SaveChangesAsync(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V12IntegrationTests] DB save error: {ex.Message}"); throw; }

                try
                {
                    // 1. Đăng nhập là Teacher B (Không có quyền xóa việc của Teacher A)
                    QASmartClass.Staff.Services.StaffSession.Login(new TeacherProfile { FullName = "Teacher B", Role = "GV" });

                    var vm = new TaskManagementViewModel();
                    await vm.LoadTasksCommand.ExecuteAsync(null);

                    var item = vm.Tasks.FirstOrDefault(t => t.Title == taskTitle);
                    Assert.NotNull(item);

                    // 2. Chọn nhiệm vụ và cố gắng xóa
                    vm.SelectedTask = item;
                    await vm.DeleteTaskCommand.ExecuteAsync(null);

                    // 3. Xác minh nhiệm vụ VẪN CÒN trong DB thực tế (Xóa bị chặn) qua context mới cô lập
                    using var verifyDb = new AppDbContext();
                    var taskAfter = await verifyDb.DailyTasks.FindAsync(task.Id);
                    Assert.NotNull(taskAfter);
                }
                finally
                {
                    using var cleanupDb = new AppDbContext();
                    var taskInDb = await cleanupDb.DailyTasks.FindAsync(task.Id);
                    if (taskInDb != null) cleanupDb.DailyTasks.Remove(taskInDb);
                    try { await cleanupDb.SaveChangesAsync(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V12IntegrationTests] DB save error: {ex.Message}"); throw; }
                    QASmartClass.Staff.Services.StaffSession.Logout();
                }
            }
        }

        [Fact]
        public void TestAsset_StatusMapping_ReturnsVietnamese()
        {
            // Trạng thái: Active -> Đang hoạt động, NeedsRepair -> Cần sửa chữa, Broken -> Đã hỏng / Chờ thanh lý
            var activeModel = new AssetDisplayModel { StatusText = "Active" };
            Assert.Equal("Đang hoạt động", activeModel.FriendlyStatus);

            var repairModel = new AssetDisplayModel { StatusText = "NeedsRepair" };
            Assert.Equal("Cần sửa chữa", repairModel.FriendlyStatus);

            var brokenModel = new AssetDisplayModel { StatusText = "Broken" };
            Assert.Equal("Đã hỏng / Chờ thanh lý", brokenModel.FriendlyStatus);

            var unknownModel = new AssetDisplayModel { StatusText = "Custom" };
            Assert.Equal("Custom", unknownModel.FriendlyStatus);
        }

        [Fact]
        public void TestJanitor_ShiftAndStatusMapping_ReturnsVietnamese()
        {
            var taskDisp = new CleaningTaskDisplay
            {
                Shift = "Morning",
                Status = "Pending"
            };

            Assert.Equal("Ca Sáng", taskDisp.FriendlyShift);
            Assert.Equal("Chờ thực hiện", taskDisp.FriendlyStatus);

            taskDisp.Shift = "Afternoon";
            taskDisp.Status = "InProgress";
            Assert.Equal("Ca Chiều", taskDisp.FriendlyShift);
            Assert.Equal("Đang thực hiện", taskDisp.FriendlyStatus);

            taskDisp.Shift = "Evening";
            taskDisp.Status = "Done";
            Assert.Equal("Ca Tối", taskDisp.FriendlyShift);
            Assert.Equal("Đã hoàn thành", taskDisp.FriendlyStatus);
        }
    }
}
