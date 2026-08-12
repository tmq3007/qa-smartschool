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
    public class V21IntegrationTests
    {
        static V21IntegrationTests()
        {
            QASmartClass.Staff.Services.ServiceRegistration.Initialize();
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();

            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.24.0");
        }

        [Fact]
        public async Task TestCanteen_QuickPayMode_DeductsAutomatically()
        {
            using (var db = new AppDbContext())
            {
                var student = new Student
                {
                    FullName = "HS Quick POS Test",
                    StudentCode = "HS_QUICK_POS",
                    ClassName = "10A1",
                    WalletBalance = 50000,
                    Status = "Active"
                };
                db.Students.Add(student);
                await db.SaveChangesAsync();

                try
                 {
                    var vm = new CanteenPosViewModel();
                    vm.IsQuickPayMode = true;
                    vm.QuickPayAmount = 25000;
                    vm.SearchCode = "HS_QUICK_POS";

                    await vm.SearchStudentCommand.ExecuteAsync(null);

                    // Xác minh ví đã bị trừ trực tiếp 25.000đ sau khi quẹt thẻ (mà không cần thủ quỹ click thanh toán)
                    using (var verifyDb = new AppDbContext())
                    {
                        var target = await verifyDb.Students.FindAsync(student.Id);
                        Assert.NotNull(target);
                        Assert.Equal(25000, target.WalletBalance);
                    }
                }
                finally
                {
                    var target = await db.Students.FindAsync(student.Id);
                    if (target != null) db.Students.Remove(target);
                    await db.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task TestCanteen_DeductionLimit()
        {
            using (var db = new AppDbContext())
            {
                var student = new Student
                {
                    FullName = "HS Limit Test",
                    StudentCode = "HS_LIMIT",
                    ClassName = "10A1",
                    WalletBalance = 200000,
                    Status = "Active"
                };
                db.Students.Add(student);
                await db.SaveChangesAsync();

                try
                {
                    var vm = new CanteenPosViewModel();
                    vm.SearchCode = "HS_LIMIT";
                    await vm.SearchStudentCommand.ExecuteAsync(null);

                    vm.DeductionAmount = 120000; // Vượt quá hạn mức 100k
                    await vm.ProcessPaymentCommand.ExecuteAsync(null);

                    Assert.Equal("Vượt quá hạn mức 100.000đ/giao dịch.", vm.StatusMessage);

                    // Đảm bảo số tiền ví trong database không hề bị trừ
                    using (var verifyDb = new AppDbContext())
                    {
                        var target = await verifyDb.Students.FindAsync(student.Id);
                        Assert.NotNull(target);
                        Assert.Equal(200000, target.WalletBalance);
                    }
                }
                finally
                {
                    var target = await db.Students.FindAsync(student.Id);
                    if (target != null) db.Students.Remove(target);
                    await db.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task TestCanteen_LowBalanceAlert()
        {
            using (var db = new AppDbContext())
            {
                var student = new Student
                {
                    FullName = "HS Low Bal Test",
                    StudentCode = "HS_LOW_BAL",
                    ClassName = "10A1",
                    WalletBalance = 40000,
                    Status = "Active"
                };
                db.Students.Add(student);
                await db.SaveChangesAsync();

                try
                {
                    var vm = new CanteenPosViewModel();
                    vm.SearchCode = "HS_LOW_BAL";
                    await vm.SearchStudentCommand.ExecuteAsync(null);

                    vm.DeductionAmount = 20000; // Số dư sau giao dịch còn 20.000đ < 30.000đ
                    await vm.ProcessPaymentCommand.ExecuteAsync(null);

                    // Đảm bảo có tin nhắn thông báo đẩy cho ví thấp được lưu
                    using (var verifyDb = new AppDbContext())
                    {
                        var logs = await verifyDb.PushMessageLogs
                            .Where(l => l.RecipientId == student.Id && l.Title == "Cảnh báo số dư ví thấp")
                            .ToListAsync();
                        Assert.NotEmpty(logs);
                        
                        // Dọn dẹp log
                        verifyDb.PushMessageLogs.RemoveRange(logs);
                        await verifyDb.SaveChangesAsync();
                    }
                }
                finally
                {
                    var target = await db.Students.FindAsync(student.Id);
                    if (target != null) db.Students.Remove(target);
                    await db.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task TestTaskManagement_SyncsToTimetable()
        {
            using (var db = new AppDbContext())
            {
                var teacher = new TeacherProfile
                {
                    TeacherCode = "GV_SYNC_TEST",
                    FullName = "Giáo Viên Đồng Bộ",
                    Subject = "Toán",
                    IsActive = true
                };
                db.TeacherProfiles.Add(teacher);
                await db.SaveChangesAsync();

                try
                {
                    var vm = new TaskManagementViewModel();
                    vm.TaskTitle = "Thiết kế giáo án nâng cao";
                    vm.Assignee = "Giáo Viên Đồng Bộ";
                    vm.DueDate = new DateTime(2026, 6, 8); // Monday (Thứ Hai -> 2)
                    
                    await vm.AssignCommand.ExecuteAsync(null);

                    // Xác minh có dòng sự kiện tương ứng trong bảng lịch công tác TimetableEntries
                    using (var verifyDb = new AppDbContext())
                    {
                        var eventInRoster = await verifyDb.TimetableEntries
                            .FirstOrDefaultAsync(e => e.TeacherName == "Giáo Viên Đồng Bộ" && e.Subject.Contains("Thiết kế giáo án nâng cao"));
                        
                        Assert.NotNull(eventInRoster);
                        Assert.Equal(2, eventInRoster.DayOfWeek); // Monday -> 2

                        // Dọn dẹp
                        verifyDb.TimetableEntries.Remove(eventInRoster);
                        
                        var task = await verifyDb.DailyTasks.FirstOrDefaultAsync(t => t.Title == "Thiết kế giáo án nâng cao");
                        if (task != null) verifyDb.DailyTasks.Remove(task);
                        
                        await verifyDb.SaveChangesAsync();
                    }
                }
                finally
                {
                    var target = await db.TeacherProfiles.FindAsync(teacher.Id);
                    if (target != null) db.TeacherProfiles.Remove(target);
                    await db.SaveChangesAsync();
                }
            }
        }

        [Fact]
        public async Task TestIncident_HomeroomStudentsSortedFirst()
        {
            var teacher = new TeacherProfile
            {
                TeacherCode = "GV_CN_99A9",
                FullName = "GVCN Lớp Chín Chín",
                Notes = "GV chủ nhiệm 99A9",
                IsActive = true
            };

            var s1 = new Student { StudentCode = "HS_SORT_1", FullName = "Nguyễn An", ClassName = "99A9", Status = "Active" };
            var s2 = new Student { StudentCode = "HS_SORT_2", FullName = "Trần Bình", ClassName = "11B2", Status = "Active" };

            using (var db = new AppDbContext())
            {
                db.TeacherProfiles.Add(teacher);
                db.Students.AddRange(s1, s2);
                await db.SaveChangesAsync();
            }

            try
            {
                // Giả lập đăng nhập bằng GVCN lớp 12A1
                QASmartClass.Staff.Services.StaffSession.Login(teacher);

                var vm = new IncidentManagementViewModel();
                var loadStudentsMethod = typeof(IncidentManagementViewModel)
                    .GetMethod("LoadStudentsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                Assert.NotNull(loadStudentsMethod);
                var task = loadStudentsMethod.Invoke(vm, null) as Task;
                Assert.NotNull(task);
                await task;

                // Học sinh lớp 12A1 (s1) phải nằm ở đầu danh sách gợi ý
                Assert.Equal("HS_SORT_1", vm.StudentsList[0].StudentCode);
            }
            finally
            {
                QASmartClass.Staff.Services.StaffSession.Logout();
                using (var db = new AppDbContext())
                {
                    var t = await db.TeacherProfiles.FindAsync(teacher.Id);
                    if (t != null) db.TeacherProfiles.Remove(t);
                    var stu1 = await db.Students.FirstOrDefaultAsync(s => s.StudentCode == "HS_SORT_1");
                    if (stu1 != null) db.Students.Remove(stu1);
                    var stu2 = await db.Students.FirstOrDefaultAsync(s => s.StudentCode == "HS_SORT_2");
                    if (stu2 != null) db.Students.Remove(stu2);
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
