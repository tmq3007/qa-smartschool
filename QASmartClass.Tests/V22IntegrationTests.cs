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
    public class V22IntegrationTests
    {
        static V22IntegrationTests()
        {
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();

            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.24.0");
            // QASmartClass.Staff.Services.ServiceRegistration.Initialize();
        }

        [Fact]
        public async Task TestCanteen_BlockInactiveStudent()
        {
            using (var db = new AppDbContext())
            {
                var student = new Student
                {
                    FullName = "HS Inactive Test",
                    StudentCode = "HS_INACTIVE",
                    ClassName = "10A1",
                    WalletBalance = 50000,
                    Status = "Graduated" // Cựu học sinh / Không hoạt động
                };
                db.Students.Add(student);
                try { await db.SaveChangesAsync(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V22IntegrationTests] DB save error: {ex.Message}"); throw; }

                try
                {
                    var vm = new CanteenPosViewModel();
                    vm.SearchCode = "HS_INACTIVE";
                    await vm.SearchStudentCommand.ExecuteAsync(null);

                    vm.DeductionAmount = 20000;
                    await vm.ProcessPaymentCommand.ExecuteAsync(null);

                    // Xác minh giao dịch bị chặn và báo lỗi tài khoản bị khóa
                    Assert.Equal("Vui lòng kiểm tra lại thông tin thẻ tại Văn phòng hỗ trợ", vm.StatusMessage);

                    // Đảm bảo số tiền ví trong database không bị trừ
                    using (var verifyDb = new AppDbContext())
                    {
                        var target = await verifyDb.Students.FindAsync(student.Id);
                        Assert.NotNull(target);
                        Assert.Equal(50000, target.WalletBalance);
                    }
                }
                finally
                {
                    var target = await db.Students.FindAsync(student.Id);
                    if (target != null) db.Students.Remove(target);
                    try { await db.SaveChangesAsync(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V22IntegrationTests] DB save error: {ex.Message}"); throw; }
                }
            }
        }

        [Fact]
        public async Task TestKitchen_BlockPastDateMenu()
        {
            var vm = new KitchenDashboardViewModel();
            vm.MenuDate = DateTime.Today.AddDays(-1); // Ngày hôm qua (Quá khứ)
            vm.MenuItems = "Món ăn quá khứ";
            vm.SelectedMealType = "Lunch";

            await vm.SaveMenuCommand.ExecuteAsync(null);

            // Đảm bảo không có bản ghi thực đơn nào được thêm vào DB cho ngày hôm qua
            using (var db = new AppDbContext())
            {
                var exists = await db.SchoolMenus.AnyAsync(m => m.Items == "Món ăn quá khứ");
                Assert.False(exists);
            }
        }

        [Fact]
        public async Task TestIncident_ClassFiltering()
        {
            var s1 = new Student { StudentCode = "HS_CLASS_1", FullName = "HS Lớp 1", ClassName = "12A1_V22_TEST", Status = "Active" };
            var s2 = new Student { StudentCode = "HS_CLASS_2", FullName = "HS Lớp 2", ClassName = "11B2", Status = "Active" };

            using (var db = new AppDbContext())
            {
                db.Students.AddRange(s1, s2);
                try { await db.SaveChangesAsync(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V22IntegrationTests] DB save error: {ex.Message}"); throw; }
            }

            try
            {
                var vm = new IncidentManagementViewModel();
                vm.SelectedClass = "12A1_V22_TEST"; // Chọn lớp 12A1_V22_TEST

                // Gọi lọc danh sách
                var loadStudentsMethod = typeof(IncidentManagementViewModel)
                    .GetMethod("FilterStudentsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                Assert.NotNull(loadStudentsMethod);
                var task = loadStudentsMethod.Invoke(vm, null) as Task;
                Assert.NotNull(task);
                await task;

                // Đảm bảo danh sách chỉ chứa duy nhất học sinh lớp 12A1
                Assert.Single(vm.StudentsList);
                Assert.Equal("HS_CLASS_1", vm.StudentsList[0].StudentCode);
            }
            finally
            {
                using (var db = new AppDbContext())
                {
                    var stu1 = await db.Students.FirstOrDefaultAsync(s => s.StudentCode == "HS_CLASS_1");
                    if (stu1 != null) db.Students.Remove(stu1);
                    var stu2 = await db.Students.FirstOrDefaultAsync(s => s.StudentCode == "HS_CLASS_2");
                    if (stu2 != null) db.Students.Remove(stu2);
                    try { await db.SaveChangesAsync(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V22IntegrationTests] DB save error: {ex.Message}"); throw; }
                }
            }
        }

        [Fact]
        public async Task TestTaskManagement_SyncsToTimetableDynamic()
        {
            using (var db = new AppDbContext())
            {
                var teacher = new TeacherProfile
                {
                    TeacherCode = "GV_SYNC_DYN",
                    FullName = "Giáo Viên Cấu Hình Động",
                    Subject = "Toán",
                    IsActive = true
                };
                db.TeacherProfiles.Add(teacher);
                try { await db.SaveChangesAsync(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V22IntegrationTests] DB save error: {ex.Message}"); throw; }

                try
                {
                    var vm = new TaskManagementViewModel();
                    vm.TaskTitle = "Task đồng bộ phòng học động";
                    vm.Assignee = "Giáo Viên Cấu Hình Động";
                    vm.DueDate = new DateTime(2026, 6, 8); // Thứ Hai -> 2
                    vm.SyncPeriod = 4; // Tiết 4
                    vm.SyncRoom = "Phòng Hội Thảo 3";

                    await vm.AssignCommand.ExecuteAsync(null);

                    // Xác minh có dòng sự kiện tương ứng trong bảng lịch công tác TimetableEntries với đúng cấu hình
                    using (var verifyDb = new AppDbContext())
                    {
                        var eventInRoster = await verifyDb.TimetableEntries
                            .FirstOrDefaultAsync(e => e.TeacherName == "Giáo Viên Cấu Hình Động" && e.Subject.Contains("Task đồng bộ phòng học động"));
                        
                        Assert.NotNull(eventInRoster);
                        Assert.Equal(2, eventInRoster.DayOfWeek); // Monday -> 2
                        Assert.Equal(4, eventInRoster.Period); // Tiết 4
                        Assert.Equal("Phòng Hội Thảo 3", eventInRoster.Room);

                        // Dọn dẹp
                        verifyDb.TimetableEntries.Remove(eventInRoster);
                        var task = await verifyDb.DailyTasks.FirstOrDefaultAsync(t => t.Title == "Task đồng bộ phòng học động");
                        if (task != null) verifyDb.DailyTasks.Remove(task);
                        try { await verifyDb.SaveChangesAsync(); }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V22IntegrationTests] DB save error: {ex.Message}"); throw; }
                    }
                }
                finally
                {
                    var target = await db.TeacherProfiles.FindAsync(teacher.Id);
                    if (target != null) db.TeacherProfiles.Remove(target);
                    try { await db.SaveChangesAsync(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V22IntegrationTests] DB save error: {ex.Message}"); throw; }
                }
            }
        }

        [Fact]
        public async Task TestDocumentRouting_AuditsStatusChanges()
        {
            StaffSession.Login(new TeacherProfile { TeacherCode = "GV001", FullName = "Giáo viên Hệ thống", Role = "Admin" });
            using (var db = new AppDbContext())
            {
                var doc = new DocumentRoute
                {
                    DocumentTitle = "Công văn kiểm toán V22",
                    Sender = "TC_ADMIN",
                    Receiver = "Ban Giám Hiệu",
                    Notes = "Notes",
                    Status = "Pending",
                    SentAt = DateTime.Now,
                    MaxApprovalLevel = 2,
                    CurrentApprovalLevel = 0
                };
                db.DocumentRoutes.Add(doc);
                try { await db.SaveChangesAsync(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V22IntegrationTests] DB save error: {ex.Message}"); throw; }

                try
                {
                    var vm = new DocumentRoutingViewModel();
                    vm.ApproverComment = "Từ chối văn bản kiểm toán.";
                    await vm.RejectCommand.ExecuteAsync(doc.Id);

                    // Xác minh có nhật ký kiểm toán với hành động Document_Rejected
                    using (var verifyDb = new AppDbContext())
                    {
                        var log = await verifyDb.AuditLogs
                            .FirstOrDefaultAsync(l => l.Action == "Document_Rejected" && l.Details.Contains("Công văn kiểm toán V22"));
                        Assert.NotNull(log);

                        // Dọn dẹp
                        verifyDb.AuditLogs.Remove(log);
                        await verifyDb.SaveChangesAsync();
                    }
                }
                finally
                {
                    StaffSession.Logout();
                    var target = await db.DocumentRoutes.FindAsync(doc.Id);
                    if (target != null) db.DocumentRoutes.Remove(target);
                    try { await db.SaveChangesAsync(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V22IntegrationTests] DB save error: {ex.Message}"); throw; }
                }
            }
        }

        [Fact]
        public void TestIPMulticast_SettingsAndServiceBinding()
        {
            using (var db = new AppDbContext())
            {
                // Lưu cấu hình kiểm thử vào DB
                var settingIp = db.SystemSettings.Find("Network_MulticastAddress");
                if (settingIp == null) db.SystemSettings.Add(new SystemSetting { Id = "Network_MulticastAddress", Value = "239.5.5.5", Category = "Network", LastUpdated = DateTime.Now });
                else settingIp.Value = "239.5.5.5";

                var settingPort = db.SystemSettings.Find("Network_MulticastPort");
                if (settingPort == null) db.SystemSettings.Add(new SystemSetting { Id = "Network_MulticastPort", Value = "1989", Category = "Network", LastUpdated = DateTime.Now });
                else settingPort.Value = "1989";

                var settingTtl = db.SystemSettings.Find("Broadcast_MulticastTTL");
                if (settingTtl == null) db.SystemSettings.Add(new SystemSetting { Id = "Broadcast_MulticastTTL", Value = "16", Category = "Broadcast", LastUpdated = DateTime.Now });
                else settingTtl.Value = "16";

                db.SaveChanges();
            }

            try
            {
                // Ép UdpScreenBroadcastService tải lại cấu hình từ database
                var service = UdpScreenBroadcastService.Instance;
                service.LoadConfigFromDb();

                // Xác minh các thuộc tính được tải động chính xác
                Assert.Equal("239.5.5.5", UdpScreenBroadcastService.MulticastIP);
                Assert.Equal(1989, UdpScreenBroadcastService.MulticastPort);
                Assert.Equal(16, UdpScreenBroadcastService.MulticastTTL);
            }
            finally
            {
                // Khôi phục lại cấu hình mặc định trong DB để tránh ảnh hưởng test case khác
                using (var db = new AppDbContext())
                {
                    var settingIp = db.SystemSettings.Find("Network_MulticastAddress");
                    if (settingIp != null) settingIp.Value = "239.0.0.1";

                    var settingPort = db.SystemSettings.Find("Network_MulticastPort");
                    if (settingPort != null) settingPort.Value = "8088";

                    var settingTtl = db.SystemSettings.Find("Broadcast_MulticastTTL");
                    if (settingTtl != null) db.SystemSettings.Remove(settingTtl);

                    db.SaveChanges();
                }
                
                // Khôi phục lại cấu hình dịch vụ về mặc định
                UdpScreenBroadcastService.Instance.LoadConfigFromDb();
            }
        }

        [Fact]
        public void TestSecureBroadcast_EncryptionDecryption()
        {
            // Thiết lập mảng byte gốc mô phỏng ảnh chụp màn hình
            byte[] originalData = new byte[1024];
            new Random().NextBytes(originalData);

            byte[] cipherData = new byte[1024];
            Buffer.BlockCopy(originalData, 0, cipherData, 0, 1024);

            string testKey = "SECURE_KEY_2026";

            // Bước 1: Gọi phương thức mã hóa gián tiếp thông qua hàm CipherBytesInPlace của lớp mạng
            var methodInfo = typeof(UdpScreenBroadcastService).GetMethod("CipherBytesInPlace", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.NotNull(methodInfo);

            // Tiến hành mã hóa phần payload
            methodInfo.Invoke(null, new object[] { cipherData, 0, 1024, testKey });

            // Xác nhận dữ liệu đã được mã hóa và khác biệt với mảng byte ban đầu
            Assert.False(originalData.SequenceEqual(cipherData));

            // Bước 2: Tiến hành giải mã ngược lại bằng chính khóa mã hóa đó
            methodInfo.Invoke(null, new object[] { cipherData, 0, 1024, testKey });

            // Xác nhận dữ liệu sau giải mã phải trùng khớp 100% với dữ liệu gốc ban đầu
            Assert.True(originalData.SequenceEqual(cipherData));
        }

        [Fact]
        public void TestAppPerformanceMonitor_ResourceTracking()
        {
            var monitor = AppPerformanceMonitor.Instance;
            Assert.NotNull(monitor);

            // Bật bộ giám sát tài nguyên
            monitor.Start();

            // Ghi nhận băng thông giả lập
            monitor.RecordBytesTransmitted(1024 * 1024); // 1 MB

            // Đợi một khoảng thời gian ngắn để kiểm tra không bị ném exception
            Thread.Sleep(500);

            // Tắt bộ giám sát
            monitor.Stop();
        }

        [Fact]
        public void TestFirewallRegistration_NoException()
        {
            // Bước 1: Lấy phương thức RegisterFirewallRulesAsync thông qua reflection
            var methodInfo = typeof(UdpScreenBroadcastService).GetMethod("RegisterFirewallRulesAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(methodInfo);

            // Thực thi phương thức bất đồng bộ để kiểm thử độ an toàn (không crash)
            methodInfo.Invoke(UdpScreenBroadcastService.Instance, null);
            
            // Đợi tác vụ chạy ngầm hoàn tất
            Thread.Sleep(500);
        }
    }
}
