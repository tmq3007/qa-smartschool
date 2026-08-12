using System;
using System.Linq;
using QASmartClass.Data;

namespace QASmartClass.Staff.Services
{
    /// <summary>
    /// Seeds sample data for Staff module views so forms are pre-populated
    /// with realistic Vietnamese school data for demonstration purposes.
    /// Safe to call multiple times — checks for existing data first.
    /// </summary>
    public static class StaffDataSeeder
    {
        public static void SeedAll(AppDbContext db)
        {
            try
            {
                // Fast-Check Guard: If Staff data seed version matches current version, skip 38 DB queries
                const string currentVersion = "4.2.0";
                var versionSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_StaffSeedVersion");
                if (versionSetting != null && versionSetting.Value == currentVersion && db.TeacherProfiles.Any())
                {
                    Serilog.Log.Information("[StaffDataSeeder] Already seeded for v{Ver} (Fast-Check passed)", currentVersion);
                    return;
                }

                SeedSecurityLogs(db);
                SeedCleaningTasks(db);
                SeedSchoolMenus(db);
                SeedFoodAllergies(db);
                SeedOfficialDocuments(db);
                SeedPayrollRecords(db);
                SeedDocumentRoutes(db);
                SeedPushMessageLogs(db);
                SeedStudentLeaveRequests(db);
                SeedMobileTokens(db);
                SeedSchoolAssets(db);
                SeedStudents(db);
                SeedDailyTasks(db);
                SeedAdditionalEventLogs(db);
                SeedStaffAccounts(db);
                SeedYouthUnionData(db);

                // --- New seeders for full dashboard test ---
                SeedStudentMentalHealthRecords(db);
                SeedHealthRecords(db);
                SeedEpidemicCases(db);
                SeedFoodSafetyRecords(db);
                SeedStaffProfiles(db);
                SeedContracts(db);
                SeedLeaveRequests(db);
                SeedStaffAttendances(db);
                SeedTuitionRecords(db);
                SeedMedicalSupplies(db);
                SeedEmergencyLogs(db);
                SeedSchoolEvents(db);
                SeedEvaluationRecords(db);
                SeedSystemSettings(db);
                SeedBackupLogs(db);
                SeedAuditLogs(db);
                SeedUsageLogs(db);
                SeedLibraryBooks(db);

                // Mark seed version complete
                if (versionSetting == null)
                {
                    db.SystemSettings.Add(new SystemSetting { Id = "Security_StaffSeedVersion", Value = currentVersion, Category = "System" });
                }
                else
                {
                    versionSetting.Value = currentVersion;
                }

                db.SaveChanges();
                Serilog.Log.Information("[StaffDataSeeder] Seed completed.");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[StaffDataSeeder] Seed failed.");
            }
        }

        private static void SeedSecurityLogs(AppDbContext db)
        {
            if (db.SecurityLogs.Any()) return;
            var now = DateTime.Now;
            db.SecurityLogs.AddRange(
                new SecurityLog { Timestamp = now.AddMinutes(-5), EventType = "CheckIn", Description = "HS Nguyễn Văn A quẹt thẻ vào cổng C1", PersonInvolved = "Nguyễn Văn A - 10A1", GuardName = "Trần Văn Bảo" },
                new SecurityLog { Timestamp = now.AddMinutes(-12), EventType = "CheckIn", Description = "HS Lê Thị B quẹt thẻ vào cổng C1", PersonInvolved = "Lê Thị B - 10A2", GuardName = "Trần Văn Bảo" },
                new SecurityLog { Timestamp = now.AddMinutes(-20), EventType = "Incident", Description = "Phát hiện xe lạ đậu trước cổng C2, đã xác minh CMND", PersonInvolved = "Xe BKS 50H-12345", GuardName = "Nguyễn Quốc Cường" },
                new SecurityLog { Timestamp = now.AddMinutes(-35), EventType = "KeyExchange", Description = "Nhận chìa khóa phòng Lab3 từ GV Toán", PersonInvolved = "GV Phạm Hùng", GuardName = "Trần Văn Bảo" },
                new SecurityLog { Timestamp = now.AddHours(-1), EventType = "CheckOut", Description = "HS Trần Minh C ra cổng (PH đón sớm)", PersonInvolved = "Trần Minh C - 11B", GuardName = "Nguyễn Quốc Cường" },
                new SecurityLog { Timestamp = now.AddHours(-2), EventType = "CheckIn", Description = "Khách: Anh Nguyễn Đức (PH của HS 10A3 Nguyễn Thị D)", PersonInvolved = "Nguyễn Đức - PH", GuardName = "Trần Văn Bảo" },
                new SecurityLog { Timestamp = now.AddHours(-3), EventType = "Incident", Description = "Cửa kính hành lang tầng 2 bị nứt, đã cảnh báo khu vực", PersonInvolved = "Khu B - Hành lang T2", GuardName = "Lê Văn Dũng" }
            );
        }

        private static void SeedStaffAccounts(AppDbContext db)
        {
            var accounts = new[]
            {
                new TeacherProfile { TeacherCode="ADMIN", FullName="Quản trị viên", Role="Admin", PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("admin123") },
                new TeacherProfile { TeacherCode="HT001", FullName="Nguyễn Văn Hùng", Role="HieuTruong", PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("ht2026") },
                new TeacherProfile { TeacherCode="HP001", FullName="Trần Thị Mai", Role="HieuPho", PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("hp2026") },
                new TeacherProfile { TeacherCode="GV001", FullName="Lê Văn Dũng", Role="GV", PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("gv2026") },
                new TeacherProfile { TeacherCode="BV001", FullName="Trần Văn Bảo", Role="BaoVe", PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("bv2026") },
                new TeacherProfile { TeacherCode="YT001", FullName="Phạm Thị Lan", Role="YTe", PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("yt2026") },
                new TeacherProfile { TeacherCode="LC001", FullName="Nguyễn Thị Hoa", Role="LaoCong", PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("lc2026") },
                new TeacherProfile { TeacherCode="BP001", FullName="Trần Văn Tài", Role="Bep", PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("bp2026") },
                new TeacherProfile { TeacherCode="KT001", FullName="Lê Thị Thu", Role="Ketoan", PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("kt2026") },
                new TeacherProfile { TeacherCode="TV001", FullName="Hoàng Minh Tâm", Role="Counselor", PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("tv2026") },
                new TeacherProfile { TeacherCode="TT001", FullName="Hoàng Minh Thủ Thư", Role="Librarian", PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("tt2026") },
            };

            foreach (var account in accounts)
            {
                if (!db.TeacherProfiles.Any(t => t.TeacherCode == account.TeacherCode))
                {
                    db.TeacherProfiles.Add(account);
                }
            }
        }

        private static void SeedCleaningTasks(AppDbContext db)
        {
            if (db.CleaningTasks.Any()) return;
            var today = DateTime.Today;
            db.CleaningTasks.AddRange(
                new CleaningTask { Area = "Sân trường khu A", TaskDate = today, Shift = "Morning", JanitorName = "Nguyễn Thị Lan", Status = "Done", Note = "Quét lá xong 7h30" },
                new CleaningTask { Area = "Khu vệ sinh tầng 1", TaskDate = today, Shift = "Morning", JanitorName = "Trần Văn Tám", Status = "Done", Note = "" },
                new CleaningTask { Area = "Hành lang tầng 2-3", TaskDate = today, Shift = "Morning", JanitorName = "Lê Thị Mai", Status = "InProgress", Note = "Đang lau kính" },
                new CleaningTask { Area = "Canteen", TaskDate = today, Shift = "Afternoon", JanitorName = "Phạm Văn Hải", Status = "Pending", Note = "Sau giờ ăn trưa" },
                new CleaningTask { Area = "Phòng Lab STEM", TaskDate = today, Shift = "Afternoon", JanitorName = "Nguyễn Thị Lan", Status = "Pending", Note = "Vệ sinh thiết bị + sàn" },
                new CleaningTask { Area = "Sân bóng rổ", TaskDate = today, Shift = "Evening", JanitorName = "Trần Văn Tám", Status = "Pending", Note = "Thu gom rác sau giờ ngoại khóa" }
            );
        }

        private static void SeedSchoolMenus(AppDbContext db)
        {
            if (db.SchoolMenus.Any()) return;
            var today = DateTime.Today;
            db.SchoolMenus.AddRange(
                new SchoolMenu { Date = today, MealType = "Breakfast", Items = "Phở bò, sữa tươi, trái cây", NutritionInfo = "450 kcal", Allergens = "" },
                new SchoolMenu { Date = today, MealType = "Lunch", Items = "Cơm trắng, cá kho tộ, canh chua, rau muống xào", NutritionInfo = "680 kcal", Allergens = "Cá" },
                new SchoolMenu { Date = today, MealType = "Snack", Items = "Bánh flan, nước cam", NutritionInfo = "200 kcal", Allergens = "Trứng, sữa" },
                new SchoolMenu { Date = today.AddDays(1), MealType = "Lunch", Items = "Cơm trắng, gà rán, canh bí đỏ, salad dưa leo", NutritionInfo = "720 kcal", Allergens = "" }
            );
        }

        private static void SeedFoodAllergies(AppDbContext db)
        {
            if (db.FoodAllergies.Any()) return;
            db.FoodAllergies.AddRange(
                new FoodAllergy { StudentName = "Nguyễn Thị D (10A3)", Allergen = "Đậu phộng", Severity = "Severe", ActionPlan = "TUYỆT ĐỐI không cho ăn. Có EpiPen trong tủ y tế." },
                new FoodAllergy { StudentName = "Trần Minh E (10A1)", Allergen = "Hải sản (tôm, cua)", Severity = "Moderate", ActionPlan = "Thay thế bằng thịt gà khi menu có hải sản." },
                new FoodAllergy { StudentName = "Lê Hoàng F (11B)", Allergen = "Sữa bò", Severity = "Mild", ActionPlan = "Dùng sữa đậu nành thay thế." }
            );
        }

        private static void SeedOfficialDocuments(AppDbContext db)
        {
            if (db.OfficialDocuments.Any()) return;
            db.OfficialDocuments.AddRange(
                new OfficialDocument { Title = "Kế hoạch tổ chức 20/11", DocumentNumber = "CV-2026-001", IssuedDate = DateTime.Today.AddDays(-5), Type = "Internal", Recipient = "Toàn trường", Status = "Approved" },
                new OfficialDocument { Title = "Báo cáo tài chính Q1/2026", DocumentNumber = "BC-2026-015", IssuedDate = DateTime.Today.AddDays(-3), Type = "Outgoing", Recipient = "Sở GD&ĐT", Status = "Pending" },
                new OfficialDocument { Title = "Quyết định khen thưởng HK1", DocumentNumber = "QD-2026-008", IssuedDate = DateTime.Today.AddDays(-1), Type = "Internal", Recipient = "GVCN các lớp", Status = "Approved" },
                new OfficialDocument { Title = "Hướng dẫn thi học kỳ II", DocumentNumber = "HD-2026-003", IssuedDate = DateTime.Today, Type = "Incoming", Recipient = "BGH", Status = "Pending" },
                new OfficialDocument { Title = "Công văn xin cấp thiết bị STEM", DocumentNumber = "CV-2026-042", IssuedDate = DateTime.Today, Type = "Outgoing", Recipient = "Phòng GD Quận", Status = "Draft" }
            );
        }

        private static void SeedPayrollRecords(AppDbContext db)
        {
            if (db.PayrollRecords.Any()) return;
            db.PayrollRecords.AddRange(
                new PayrollRecord { StaffName = "Nguyễn Văn Hùng (Hiệu trưởng)", Month = 5, Year = 2026, BaseSalary = 18000000, Allowance = 5000000, Deduction = 1800000, Tax = 2100000, Status = "Paid" },
                new PayrollRecord { StaffName = "Trần Thị Mai (GV Toán)", Month = 5, Year = 2026, BaseSalary = 12000000, Allowance = 2000000, Deduction = 1200000, Tax = 800000, Status = "Paid" },
                new PayrollRecord { StaffName = "Lê Văn Dũng (Bảo vệ)", Month = 5, Year = 2026, BaseSalary = 7500000, Allowance = 1000000, Deduction = 750000, Tax = 0, Status = "Paid" },
                new PayrollRecord { StaffName = "Phạm Thị Hoa (Kế toán)", Month = 5, Year = 2026, BaseSalary = 11000000, Allowance = 1500000, Deduction = 1100000, Tax = 650000, Status = "Draft" },
                new PayrollRecord { StaffName = "Nguyễn Thị Lan (Lao công)", Month = 5, Year = 2026, BaseSalary = 6500000, Allowance = 500000, Deduction = 650000, Tax = 0, Status = "Draft" },
                new PayrollRecord { StaffName = "Võ Minh Tuấn (GV Lý)", Month = 5, Year = 2026, BaseSalary = 13000000, Allowance = 2500000, Deduction = 1300000, Tax = 1050000, Status = "Pending" }
            );
        }

        private static void SeedDocumentRoutes(AppDbContext db)
        {
            if (db.DocumentRoutes.Any()) return;
            var now = DateTime.Now;
            db.DocumentRoutes.AddRange(
                new DocumentRoute { DocumentTitle = "Tờ trình xin mua bàn ghế mới", Sender = "GV Nguyễn Hoàng", Receiver = "BGH_HieuTruong", SentAt = now.AddHours(-4), Status = "Pending", Notes = "20 bộ bàn ghế cho phòng Lab" },
                new DocumentRoute { DocumentTitle = "Kế hoạch kiểm tra HK2", Sender = "Tổ trưởng Toán", Receiver = "BGH_HieuPho", SentAt = now.AddDays(-1), Status = "Approved", Notes = "Đã duyệt, chuyển PĐT" },
                new DocumentRoute { DocumentTitle = "Đơn xin nghỉ phép 3 ngày", Sender = "GV Lê Thị Hương", Receiver = "BGH_HieuTruong", SentAt = now.AddDays(-2), Status = "Approved", Notes = "" },
                new DocumentRoute { DocumentTitle = "Báo cáo sửa chữa Khu B", Sender = "NV Kỹ thuật", Receiver = "VP_KeToan", SentAt = now.AddHours(-6), Status = "Forwarded", Notes = "Chuyển KT thanh toán" }
            );
        }

        private static void SeedPushMessageLogs(AppDbContext db)
        {
            if (db.PushMessageLogs.Any()) return;
            var now = DateTime.Now;
            db.PushMessageLogs.AddRange(
                new PushMessageLog { RecipientId = 0, RecipientRole = "AllParents", Title = "Lịch họp phụ huynh cuối năm", Body = "Kính mời quý PH tham dự họp ngày 25/05 lúc 14h00", Type = "General", SentAt = now.AddHours(-2), Status = "Delivered" },
                new PushMessageLog { RecipientId = 0, RecipientRole = "AllStudents", Title = "Lịch thi HK2 đã cập nhật", Body = "Thi HK2 bắt đầu từ 01/06. Xem chi tiết trong App.", Type = "Academic", SentAt = now.AddDays(-1), Status = "Sent" },
                new PushMessageLog { RecipientId = 5, RecipientRole = "Parent", Title = "Học phí tháng 5 đã đến hạn", Body = "Vui lòng thanh toán trước 20/05.", Type = "Payment", SentAt = now.AddDays(-3), Status = "Read" },
                new PushMessageLog { RecipientId = 0, RecipientRole = "AllParents", Title = "⚠ Cảnh báo thời tiết nắng nóng", Body = "Nhiệt độ dự báo 40°C. Nhà trường cho HS nghỉ thể dục ngoài trời.", Type = "Emergency", SentAt = now.AddDays(-5), Status = "Delivered" }
            );
        }

        private static void SeedStudentLeaveRequests(AppDbContext db)
        {
            if (db.StudentLeaveRequests.Any()) return;
            var today = DateTime.Today;
            db.StudentLeaveRequests.AddRange(
                new StudentLeaveRequest { StudentId = 1, StudentName = "Nguyễn Văn A", ClassName = "10A1", LeaveDate = today, Reason = "Bị sốt, có giấy bác sĩ", ParentName = "Nguyễn Đức", ParentPhone = "0901234567", Status = "Approved" },
                new StudentLeaveRequest { StudentId = 2, StudentName = "Lê Thị B", ClassName = "10A2", LeaveDate = today.AddDays(1), Reason = "Đi khám răng định kỳ", ParentName = "Lê Hồng", ParentPhone = "0912345678", Status = "Pending" },
                new StudentLeaveRequest { StudentId = 3, StudentName = "Trần Minh C", ClassName = "11B", LeaveDate = today, Reason = "Việc gia đình (đám cưới anh trai)", ParentName = "Trần Văn K", ParentPhone = "0923456789", Status = "Pending" },
                new StudentLeaveRequest { StudentId = 4, StudentName = "Phạm Thùy D", ClassName = "10A3", LeaveDate = today.AddDays(-1), Reason = "Nghỉ ốm (đau bụng)", ParentName = "Phạm Hải", ParentPhone = "0934567890", Status = "Rejected" }
            );
        }

        private static void SeedMobileTokens(AppDbContext db)
        {
            if (db.MobileTokens.Any()) return;
            var now = DateTime.Now;
            db.MobileTokens.AddRange(
                new MobileToken { UserId = 1, Role = "Parent", DeviceToken = "fcm_abc123", DeviceName = "iPhone 15 Pro (Anh Đức)", Platform = "iOS", LastActive = now.AddMinutes(-10), IsActive = true },
                new MobileToken { UserId = 2, Role = "Parent", DeviceToken = "fcm_def456", DeviceName = "Samsung Galaxy S24 (Chị Hồng)", Platform = "Android", LastActive = now.AddHours(-1), IsActive = true },
                new MobileToken { UserId = 3, Role = "Student", DeviceToken = "fcm_ghi789", DeviceName = "Redmi Note 13 (Minh C)", Platform = "Android", LastActive = now.AddDays(-1), IsActive = true },
                new MobileToken { UserId = 5, Role = "Parent", DeviceToken = "fcm_old001", DeviceName = "OPPO A78 (Cũ)", Platform = "Android", LastActive = now.AddDays(-30), IsActive = false }
            );
        }

        private static void SeedSchoolAssets(AppDbContext db)
        {
            if (db.SchoolAssets.Any()) return;
            db.SchoolAssets.AddRange(
                new SchoolAsset { AssetCode = "STB-001", AssetType = "Bảng tương tác - Bảng Tương Tác 86''", Location = "Phòng 10A1", PurchaseDate = new DateTime(2024, 8, 1), NextMaintenanceDate = new DateTime(2026, 8, 1), Status = "Active" },
                new SchoolAsset { AssetCode = "STB-002", AssetType = "Bảng tương tác - Bảng Tương Tác 75''", Location = "Phòng Lab STEM", PurchaseDate = new DateTime(2024, 8, 1), NextMaintenanceDate = new DateTime(2026, 8, 1), Status = "Active" },
                new SchoolAsset { AssetCode = "PRJ-001", AssetType = "Máy chiếu - Máy chiếu Epson EB-X51", Location = "Hội trường A", PurchaseDate = new DateTime(2023, 5, 15), NextMaintenanceDate = new DateTime(2026, 5, 15), Status = "NeedsRepair" },
                new SchoolAsset { AssetCode = "TAB-010", AssetType = "Máy tính bảng - Tablet Samsung Tab A8", Location = "Phòng IT", PurchaseDate = new DateTime(2025, 1, 10), NextMaintenanceDate = new DateTime(2027, 1, 10), Status = "Active" },
                new SchoolAsset { AssetCode = "PC-LAB-05", AssetType = "Thiết bị điện tử - Máy tính Desktop HP Pro", Location = "Phòng Lab 2 - Vị trí 5", PurchaseDate = new DateTime(2022, 9, 1), NextMaintenanceDate = new DateTime(2025, 9, 1), Status = "Broken" },
                new SchoolAsset { AssetCode = "CAM-001", AssetType = "Thiết bị điện tử - Camera an ninh Hikvision", Location = "Cổng C1", PurchaseDate = new DateTime(2024, 3, 20), NextMaintenanceDate = new DateTime(2026, 3, 20), Status = "Active" }
            );
        }
        private static void SeedStudents(AppDbContext db)
        {
            if (db.Students.Any()) return;
            db.Students.AddRange(
                new Student { FullName = "Nguyễn Văn A", StudentCode = "HS001", ClassroomId = 1, SchoolName = "THPT QA", ClassName = "10A1", Role = "HS", Status = "Active" },
                new Student { FullName = "Lê Thị B", StudentCode = "HS002", ClassroomId = 1, SchoolName = "THPT QA", ClassName = "10A1", Role = "HS", Status = "Active" },
                new Student { FullName = "Trần Minh C", StudentCode = "HS003", ClassroomId = 2, SchoolName = "THPT QA", ClassName = "11B", Role = "HS", Status = "Active" },
                new Student { FullName = "Nguyễn Thị D", StudentCode = "HS004", ClassroomId = 3, SchoolName = "THPT QA", ClassName = "10A3", Role = "HS", Status = "Active" },
                new Student { FullName = "Lê Hoàng F", StudentCode = "HS005", ClassroomId = 2, SchoolName = "THPT QA", ClassName = "11B", Role = "HS", Status = "Active" }
            );
        }

        private static void SeedDailyTasks(AppDbContext db)
        {
            if (db.DailyTasks.Any()) return;
            var today = DateTime.Today;
            db.DailyTasks.AddRange(
                new DailyTask { Title = "Kiểm tra hệ thống điện", AssignedTo = "NV Kỹ thuật", AssignedBy = "BGH_HieuTruong", DueDate = today.AddDays(1), Status = "Pending", Department = "KyThuat", Notes = "Kiểm tra các phòng Lab" },
                new DailyTask { Title = "Dọn vệ sinh sân trường", AssignedTo = "Tổ lao công", AssignedBy = "VP_KeToan", DueDate = today, Status = "InProgress", Department = "LaoCong", Notes = "" },
                new DailyTask { Title = "Phát thông báo họp PH", AssignedTo = "GVCN các lớp", AssignedBy = "BGH_HieuPho", DueDate = today.AddDays(2), Status = "Pending", Department = "GiaoVien", Notes = "Thông báo họp cuối năm" },
                new DailyTask { Title = "Thu tiền ăn bán trú tháng 5", AssignedTo = "Kế toán", AssignedBy = "BGH_HieuTruong", DueDate = today.AddDays(-1), Status = "Done", Department = "VanPhong", Notes = "" }
            );
        }

        private static void SeedAdditionalEventLogs(AppDbContext db)
        {
            // Chỉ thêm nếu chưa có dữ liệu WalletTransaction
            if (!db.EventLogs.Any(l => l.EventType == "WalletTransaction"))
            {
                var now = DateTime.Now;
                db.EventLogs.AddRange(
                    new EventLog { EventType = "WalletTransaction", Actor = "HS001", Timestamp = now.AddMinutes(-5), Details = "{\"Type\":\"Canteen_Deduction\", \"Amount\":35000, \"BalanceAfter\":165000}" },
                    new EventLog { EventType = "WalletTransaction", Actor = "HS002", Timestamp = now.AddMinutes(-15), Details = "{\"Type\":\"Canteen_Deduction\", \"Amount\":20000, \"BalanceAfter\":480000}" },
                    new EventLog { EventType = "GateCheckIn", Actor = "HS001", Timestamp = now.AddHours(-3), Details = "Cổng C1" },
                    new EventLog { EventType = "GateCheckIn", Actor = "HS002", Timestamp = now.AddHours(-3).AddMinutes(5), Details = "Cổng C1" },
                    new EventLog { EventType = "GateCheckIn", Actor = "HS003", Timestamp = now.AddHours(-3).AddMinutes(10), Details = "Cổng C2" },
                    new EventLog { EventType = "GateCheckOut", Actor = "HS003", Timestamp = now.AddMinutes(-2), Details = "Cổng C1" },
                    new EventLog { EventType = "Incident", Actor = "HS004", Timestamp = now.AddDays(-1), Details = "{\"Severity\": \"High\", \"StudentCode\": \"HS004\", \"Description\": \"Cãi nhau trong giờ ra chơi.\"}" },
                    new EventLog { EventType = "Incident", Actor = "StaffUser", Timestamp = now.AddDays(-2), Details = "{\"Severity\": \"Medium\", \"StudentCode\": \"\", \"Description\": \"Hỏng quạt trần phòng 10A1\"}" }
                );
            }
        }
        private static void SeedStudentMentalHealthRecords(AppDbContext db)
        {
            if (db.StudentMentalHealthRecords.Any()) return;
            db.StudentMentalHealthRecords.AddRange(
                new StudentMentalHealthRecord { StudentId = 1, MoodScore = 3, Notes = "Thường xuyên buồn bã, có dấu hiệu bị bắt nạt", RiskLevel = "High", DetectedKeywords = "buồn bã, bắt nạt", RecordedAt = DateTime.Now.AddDays(-2) },
                new StudentMentalHealthRecord { StudentId = 2, MoodScore = 6, Notes = "Áp lực thi cử", RiskLevel = "Medium", DetectedKeywords = "áp lực", RecordedAt = DateTime.Now.AddDays(-1) },
                new StudentMentalHealthRecord { StudentId = 3, MoodScore = 8, Notes = "Tâm trạng ổn định", RiskLevel = "Low", DetectedKeywords = "", RecordedAt = DateTime.Now }
            );
        }

        private static void SeedHealthRecords(AppDbContext db)
        {
            if (db.HealthRecords.Any()) return;
            db.HealthRecords.AddRange(
                new HealthRecord { StudentId = 1, StudentName = "Nguyễn Văn A", ClassName = "10A1", Height = 165, Weight = 55, VisionLeft = 10, VisionRight = 10, ChronicConditions = "None", ExamDate = DateTime.Today.AddMonths(-3) },
                new HealthRecord { StudentId = 2, StudentName = "Lê Thị B", ClassName = "10A1", Height = 155, Weight = 48, VisionLeft = 8, VisionRight = 9, ChronicConditions = "None", ExamDate = DateTime.Today.AddMonths(-1) },
                new HealthRecord { StudentId = 3, StudentName = "Trần Minh C", ClassName = "11B", Height = 172, Weight = 65, VisionLeft = 10, VisionRight = 10, ChronicConditions = "None", ExamDate = DateTime.Today.AddMonths(-2) }
            );
        }

        private static void SeedEpidemicCases(AppDbContext db)
        {
            if (db.EpidemicCases.Any()) return;
            db.EpidemicCases.AddRange(
                new EpidemicCase { StudentId = 1, StudentName = "Nguyễn Văn A", ClassName = "10A1", Disease = "Sốt xuất huyết", OnsetDate = DateTime.Today.AddDays(-5), Status = "Active", IsolatedAt = "Home" },
                new EpidemicCase { StudentId = 4, StudentName = "Nguyễn Thị D", ClassName = "10A3", Disease = "Thủy đậu", OnsetDate = DateTime.Today.AddDays(-10), Status = "Recovered", IsolatedAt = "Home" }
            );
        }

        private static void SeedFoodSafetyRecords(AppDbContext db)
        {
            if (db.FoodSafetyRecords.Any()) return;
            db.FoodSafetyRecords.AddRange(
                new FoodSafetyRecord { Date = DateTime.Today, Inspector = "Nguyễn Y Tế", Result = "Pass", MenuItems = "Bữa trưa" },
                new FoodSafetyRecord { Date = DateTime.Today.AddDays(-7), Inspector = "Trần Thanh Tra", Result = "Pass", SampleKept = true }
            );
        }

        private static void SeedStaffProfiles(AppDbContext db)
        {
            if (db.StaffProfiles.Any()) return;
            db.StaffProfiles.AddRange(
                new StaffProfile { StaffCode = "GV001", FullName = "Lê Văn Dũng", Department = "Toán", Position = "GV", JoinedDate = new DateTime(2020, 8, 1) },
                new StaffProfile { StaffCode = "HT001", FullName = "Nguyễn Văn Hùng", Department = "BGH", Position = "Hiệu Trưởng", JoinedDate = new DateTime(2015, 8, 1) }
            );
        }

        private static void SeedContracts(AppDbContext db)
        {
            if (db.Contracts.Any()) return;
            db.Contracts.AddRange(
                new Contract { StaffId = 1, ContractNumber = "HD-2020-001", ContractType = "Không xác định thời hạn", StartDate = new DateTime(2020, 8, 1), Status = "Active" },
                new Contract { StaffId = 2, ContractNumber = "HD-2015-001", ContractType = "Không xác định thời hạn", StartDate = new DateTime(2015, 8, 1), Status = "Active" }
            );
        }

        private static void SeedLeaveRequests(AppDbContext db)
        {
            if (db.LeaveRequests.Any()) return;
            db.LeaveRequests.AddRange(
                new LeaveRequest { StaffId = 1, LeaveType = "Nghỉ ốm", StartDate = DateTime.Today.AddDays(-2), EndDate = DateTime.Today.AddDays(-1), Reason = "Sốt cao", Status = "Approved" },
                new LeaveRequest { StaffId = 2, LeaveType = "Việc riêng", StartDate = DateTime.Today.AddDays(5), EndDate = DateTime.Today.AddDays(6), Reason = "Giải quyết việc gia đình", Status = "Pending" }
            );
        }

        private static void SeedStaffAttendances(AppDbContext db)
        {
            if (db.StaffAttendances.Any()) return;
            db.StaffAttendances.AddRange(
                new StaffAttendance { StaffId = 1, Date = DateTime.Today, CheckIn = new TimeSpan(7, 15, 0), CheckOut = new TimeSpan(17, 30, 0), Status = "Present", WorkingHours = 8.5 },
                new StaffAttendance { StaffId = 2, Date = DateTime.Today, CheckIn = new TimeSpan(7, 0, 0), Status = "Working", WorkingHours = 4 }
            );
        }

        private static void SeedTuitionRecords(AppDbContext db)
        {
            if (db.TuitionRecords.Any()) return;
            db.TuitionRecords.AddRange(
                new TuitionRecord { StudentId = 1, StudentName = "Nguyễn Văn A", ClassName = "10A1", Amount = 5000000, PaidDate = DateTime.Today.AddDays(-5), Status = "Paid", DueDate = DateTime.Today.AddMonths(-1) },
                new TuitionRecord { StudentId = 2, StudentName = "Lê Thị B", ClassName = "10A2", Amount = 5000000, Status = "Partial", DueDate = DateTime.Today.AddDays(10) },
                new TuitionRecord { StudentId = 3, StudentName = "Trần Minh C", ClassName = "11B", Amount = 5000000, Status = "Unpaid", DueDate = DateTime.Today.AddDays(10) }
            );
        }

        private static void SeedMedicalSupplies(AppDbContext db)
        {
            if (db.MedicalSupplies.Any()) return;
            db.MedicalSupplies.AddRange(
                new MedicalSupply { Name = "Paracetamol 500mg", Quantity = 150, Unit = "Viên", ExpiryDate = DateTime.Today.AddYears(1) },
                new MedicalSupply { Name = "Băng cá nhân", Quantity = 500, Unit = "Miếng", ExpiryDate = DateTime.Today.AddYears(2) },
                new MedicalSupply { Name = "Cồn y tế", Quantity = 5, Unit = "Chai", ExpiryDate = DateTime.Today.AddMonths(6) }
            );
        }

        private static void SeedEmergencyLogs(AppDbContext db)
        {
            if (db.EmergencyLogs.Any()) return;
            db.EmergencyLogs.AddRange(
                new EmergencyLog { Timestamp = DateTime.Now.AddDays(-3), StudentName = "Nguyễn Văn A", IncidentType = "Injury", Description = "HS ngã trật khớp cổ chân tại sân thể dục", FirstAidApplied = "Sơ cứu và gọi phụ huynh", Status = "Resolved" }
            );
        }

        private static void SeedSchoolEvents(AppDbContext db)
        {
            if (db.SchoolEvents.Any()) return;
            db.SchoolEvents.AddRange(
                new SchoolEvent { Title = "Họp phụ huynh đầu năm", StartTime = DateTime.Today.AddDays(5).AddHours(8), EndTime = DateTime.Today.AddDays(5).AddHours(11), Location = "Các lớp học", Description = "Phổ biến kế hoạch học tập", Organizer = "BGH" },
                new SchoolEvent { Title = "Khai giảng năm học mới", StartTime = DateTime.Today.AddMonths(3).AddHours(7), EndTime = DateTime.Today.AddMonths(3).AddHours(9), Location = "Sân trường", Description = "Lễ khai giảng toàn trường", Organizer = "Đoàn trường" }
            );
        }

        private static void SeedEvaluationRecords(AppDbContext db)
        {
            if (db.EvaluationRecords.Any()) return;
            db.EvaluationRecords.AddRange(
                new EvaluationRecord { EvaluatorId = "HT001", TargetId = "GV001", RoleRelation = "Manager", Rating = 9, Comments = "Hoàn thành xuất sắc nhiệm vụ giảng dạy", EvaluatedAt = DateTime.Today.AddMonths(-1) }
            );
        }

        private static void SeedSystemSettings(AppDbContext db)
        {
            if (db.SystemSettings.Any()) return;
            db.SystemSettings.AddRange(
                new SystemSetting { Id = "SchoolName", Value = "THPT QA SmartSchool", Category = "General" },
                new SystemSetting { Id = "SchoolYear", Value = "2025-2026", Category = "General" },
                new SystemSetting { Id = "MaxAbsenceDays", Value = "45", Category = "Rules" },
                new SystemSetting { Id = "EmergencyHotline", Value = "1900-6789", Category = "General" },
                new SystemSetting { Id = "Asset_AutoCancelBookingsOnRepair", Value = "1", Category = "Asset" },
                new SystemSetting { Id = "Principal_SignatureVerificationMode", Value = "0", Category = "Principal" },
                new SystemSetting { Id = "YouthUnion_VoteEncryptionMode", Value = "0", Category = "YouthUnion" },
                new SystemSetting { Id = "Library_OverdueFineMode", Value = "0", Category = "Library" },
                new SystemSetting { Id = "Security_SmartPickupSystem", Value = "0", Category = "Security" },
                new SystemSetting { Id = "Principal_MultiSignatureWorkflow", Value = "0", Category = "Principal" },
                new SystemSetting { Id = "Tuition_ReconciliationMode", Value = "0", Category = "Tuition" },
                new SystemSetting { Id = "Asset_DepreciationMode", Value = "0", Category = "Asset" },
                new SystemSetting { Id = "Document_RoutingMode", Value = "0", Category = "Document" },
                new SystemSetting { Id = "Kitchen_WasteOptimizationMode", Value = "0", Category = "Kitchen" },
                new SystemSetting { Id = "Library_ReservationHoldHours", Value = "24", Category = "Library" },
                new SystemSetting { Id = "Library_ReservationBlockMode", Value = "0", Category = "Library" },
                new SystemSetting { Id = "Library_ReservationBlockDays", Value = "3", Category = "Library" },
                new SystemSetting { Id = "Library_ReservationConductDeduction", Value = "5", Category = "Library" },
                new SystemSetting { Id = "Library_DamagedBookBlockMode", Value = "0", Category = "Library" },
                new SystemSetting { Id = "Library_DamagedBookBlockDays", Value = "3", Category = "Library" },
                new SystemSetting { Id = "Library_DamagedBookConductDeduction", Value = "5", Category = "Library" },
                new SystemSetting { Id = "Library_StreakThreshold", Value = "3", Category = "Library" },
                new SystemSetting { Id = "Library_StreakXpReward", Value = "10", Category = "Library" },
                new SystemSetting { Id = "Library_MinConditionScoreThreshold", Value = "4", Category = "Library" },
                new SystemSetting { Id = "Library_AutoDisposeThreshold", Value = "2", Category = "Library" },
                new SystemSetting { Id = "Library_LexileBlockMode", Value = "1", Category = "Library" },
                new SystemSetting { Id = "Library_LexileMaxOffset", Value = "250", Category = "Library" },
                new SystemSetting { Id = "Medical_PE_RestrictionMode", Value = "1", Category = "Medical" },
                new SystemSetting { Id = "Medical_Disposal_Workflow", Value = "1", Category = "Medical" },
                new SystemSetting { Id = "Security_StaggeredDismissalMode", Value = "1", Category = "Security" },
                new SystemSetting { Id = "Security_CongestionVehicleThreshold", Value = "50", Category = "Security" },
                new SystemSetting { Id = "Security_CongestionCooldownMinutes", Value = "20", Category = "Security" },
                new SystemSetting { Id = "Security_AlarmResponseLimitMinutes", Value = "5", Category = "Security" },
                new SystemSetting { Id = "Security_AlarmEscalationTarget", Value = "1", Category = "Security" },
                new SystemSetting { Id = "Canteen_SupplierBlockMode", Value = "0", Category = "Canteen" },
                new SystemSetting { Id = "Canteen_VegetableWasteMaxRatio", Value = "0.25", Category = "Canteen" },
                new SystemSetting { Id = "Canteen_NutritionAdjustMode", Value = "1", Category = "Canteen" },
                new SystemSetting { Id = "IT_License_AutoRevokeMode", Value = "0", Category = "IT" },
                new SystemSetting { Id = "IT_Sync_ConflictResolutionMode", Value = "0", Category = "IT" },
                new SystemSetting { Id = "IT_License_BackupBeforeRevokeMode", Value = "0", Category = "IT" },
                new SystemSetting { Id = "IT_License_HistoryLoggingMode", Value = "0", Category = "IT" },
                new SystemSetting { Id = "IT_Sync_BatchProcessingMode", Value = "0", Category = "IT" },
                new SystemSetting { Id = "IT_Avatar_StudentAllowedChange", Value = "1", Category = "IT" },
                new SystemSetting { Id = "IT_Avatar_ModerationMode", Value = "0", Category = "IT" },
                new SystemSetting { Id = "IT_Avatar_BatchRecursiveMode", Value = "0", Category = "IT" },
                new SystemSetting { Id = "IT_Avatar_BackupOldOnOverwrite", Value = "0", Category = "IT" },
                new SystemSetting { Id = "IT_Monitor_HeartbeatTimeoutMinutes", Value = "15", Category = "IT" },
                new SystemSetting { Id = "IT_Monitor_ForbiddenAppsList", Value = "lol.exe,genshin.exe,cheatengine.exe,roblox.exe", Category = "IT" },
                new SystemSetting { Id = "IT_Monitor_AutoActionOnAnomaly", Value = "1", Category = "IT" },
                new SystemSetting { Id = "IT_Monitor_AutoResolveSoftwareErrors", Value = "1", Category = "IT" },
                new SystemSetting { Id = "IT_Monitor_CpuThresholdPercentage", Value = "90.0", Category = "IT" },
                new SystemSetting { Id = "IT_Monitor_MemoryThresholdPercentage", Value = "95.0", Category = "IT" },
                new SystemSetting { Id = "IT_Monitor_DiskThresholdPercentage", Value = "90.0", Category = "IT" },
                new SystemSetting { Id = "IT_Monitor_NotificationReceiver", Value = "teacher", Category = "IT" },
                new SystemSetting { Id = "IT_Monitor_AutoResolveSyncErrorTypes", Value = "1", Category = "IT" },
                new SystemSetting { Id = "Principal_Override_ValidationMode", Value = "0", Category = "IT" },
                new SystemSetting { Id = "Principal_Override_MaxActiveSlotLimit", Value = "5", Category = "IT" },
                new SystemSetting { Id = "IT_Audit_LoggingLevel", Value = "2", Category = "IT" },
                new SystemSetting { Id = "IT_Audit_AnomalousQueryCountThreshold", Value = "20", Category = "IT" },
                new SystemSetting { Id = "Principal_StudyLoadAdjustment_AutoApply", Value = "2", Category = "IT" },
                new SystemSetting { Id = "Principal_StudyLoadAdjustment_DurationDays", Value = "14", Category = "IT" },
                new SystemSetting { Id = "Principal_StudyLoadAdjustment_DefaultReductionRatio", Value = "0.2", Category = "IT" },
                new SystemSetting { Id = "IT_Audit_AutoLockSessionOnAnomaly", Value = "0", Category = "IT" },
                new SystemSetting { Id = "Principal_Override_PriorityRequirement", Value = "0", Category = "IT" },
                new SystemSetting { Id = "IT_Location_TrackingTechnology", Value = "2", Category = "IT" },
                new SystemSetting { Id = "IT_Gate_OfflineFallbackMode", Value = "1", Category = "IT" },
                new SystemSetting { Id = "IT_Gate_AntiPassbackAction", Value = "2", Category = "IT" },
                new SystemSetting { Id = "IT_Gate_OfflineVerificationActive", Value = "1", Category = "IT" },
                new SystemSetting { Id = "IT_Location_RssiThreshold", Value = "-75.0", Category = "IT" },
                new SystemSetting { Id = "IT_Gate_FaceMatchRequirement", Value = "2", Category = "IT" },
                new SystemSetting { Id = "IT_Substitute_MaxWeeklyPeriods", Value = "20", Category = "IT" },
                new SystemSetting { Id = "IT_Chemical_HazardousList", Value = "H2SO4, HNO3, HCl, Na, K", Category = "IT" },
                new SystemSetting { Id = "IT_Payroll_BaseOvertimeRate", Value = "150000", Category = "IT" },
                new SystemSetting { Id = "IT_Payroll_SecretKey", Value = "SmartClass_Secret_Key_2026", Category = "IT" },
                new SystemSetting { Id = "IT_Maintenance_SlaHoursHigh", Value = "4", Category = "IT" },
                new SystemSetting { Id = "IT_Maintenance_SlaHoursCritical", Value = "2", Category = "IT" },
                new SystemSetting { Id = "IT_Security_LockdownPin", Value = "9999", Category = "IT" },
                new SystemSetting { Id = "IT_Security_LockdownMode", Value = "2", Category = "IT" },
                new SystemSetting { Id = "IT_Kitchen_MaxSafeTemp", Value = "4.0", Category = "IT" },
                new SystemSetting { Id = "IT_Kitchen_TempAlarmThresholdMinutes", Value = "30", Category = "IT" },
                new SystemSetting { Id = "IT_Medical_CarePlanVerificationMode", Value = "1", Category = "IT" },
                new SystemSetting { Id = "IT_Janitor_GreenBonusThresholdKg", Value = "100.0", Category = "IT" },
                new SystemSetting { Id = "IT_Janitor_ResourceLeakThreshold", Value = "1.30", Category = "IT" },
                new SystemSetting { Id = "IT_Gate_RFIDConnectionProtocol", Value = "0", Category = "IT" },
                new SystemSetting { Id = "IT_Gate_QrEncryptionKeyStorageMode", Value = "0", Category = "IT" },
                new SystemSetting { Id = "IT_Bulletin_CommentsEnabled", Value = "1", Category = "IT" },
                new SystemSetting { Id = "IT_Bulletin_AutoModerationMode", Value = "1", Category = "IT" },
                new SystemSetting { Id = "IT_Bulletin_ModerationAction", Value = "0", Category = "IT" }, // 0 = Censor/Replace, 1 = Block/Pending
                new SystemSetting { Id = "IT_Bulletin_AttachmentSignatureRequired", Value = "0", Category = "IT" }, // 0 = None, 1 = MD5, 2 = Strict
                new SystemSetting { Id = "IT_Bulletin_MaxSlideDuration", Value = "15", Category = "IT" },
                new SystemSetting { Id = "IT_Bulletin_StrictCategorySafetyMode", Value = "2", Category = "IT" }, // 0 = Flexible, 1 = Warn, 2 = Strict Auto-correct
                new SystemSetting { Id = "IT_Bulletin_StorageFormatMode", Value = "0", Category = "IT" }, // 0 = Markdown, 1 = XAML
                new SystemSetting { Id = "IT_Bulletin_AttachmentStorageMode", Value = "0", Category = "IT" }, // 0 = Local/UNC, 1 = Cloud
                new SystemSetting { Id = "IT_Bulletin_ApprovalWorkflow", Value = "2", Category = "IT" }, // 0 = Auto-publish, 1 = Single step, 2 = Multi-level Step
                new SystemSetting { Id = "IT_Bulletin_ModerationThreshold", Value = "70", Category = "IT" },
                new SystemSetting { Id = "IT_Dashboard_AutoSyncToCalendar", Value = "1", Category = "IT" },
                new SystemSetting { Id = "IT_Dashboard_ReportWindowHours", Value = "24", Category = "IT" },
                new SystemSetting { Id = "OTA_SecureIntegrityMode", Value = "High", Category = "IT" },
                new SystemSetting { Id = "OTA_DefaultRolloutStrategy", Value = "Staged", Category = "IT" },
                new SystemSetting { Id = "IT_Diagnostics_ReportingLevel", Value = "WarningOrAbove", Category = "IT" },
                new SystemSetting { Id = "IT_Diagnostics_AutoNotification", Value = "Disabled", Category = "IT" }
            );
        }

        private static void SeedLibraryBooks(AppDbContext db)
        {
            if (db.LibraryBooks.Any()) return;
            db.LibraryBooks.AddRange(
                new LibraryBook { BookCode = "BOOK_MATH_01", Title = "Toán 6 Tập 1", Subject = "Toán học", BookLexileLevel = 350, BookConditionScore = 10, Status = "Active" },
                new LibraryBook { BookCode = "BOOK_MATH_02", Title = "Giải tích 12 nâng cao", Subject = "Toán học", BookLexileLevel = 750, BookConditionScore = 10, Status = "Active" },
                new LibraryBook { BookCode = "BOOK_LIT_01", Title = "Ngữ văn lớp 10", Subject = "Ngữ văn", BookLexileLevel = 500, BookConditionScore = 10, Status = "Active" },
                new LibraryBook { BookCode = "BOOK_SCI_01", Title = "Vật lý vui", Subject = "Vật lý", BookLexileLevel = 450, BookConditionScore = 10, Status = "Active" },
                new LibraryBook { BookCode = "BOOK_SCI_02", Title = "Hóa học hữu cơ chuyên sâu", Subject = "Hóa học", BookLexileLevel = 800, BookConditionScore = 10, Status = "Active" }
            );
        }

        private static void SeedBackupLogs(AppDbContext db)
        {
            if (db.BackupLogs.Any()) return;
            db.BackupLogs.AddRange(
                new BackupLog { CreatedAt = DateTime.Today.AddDays(-1), FileName = "db_20250517.bak", FileSizeBytes = 1024500, Status = "Success" },
                new BackupLog { CreatedAt = DateTime.Today.AddDays(-2), FileName = "db_20250516.bak", FileSizeBytes = 1024000, Status = "Success" }
            );
        }

        private static void SeedAuditLogs(AppDbContext db)
        {
            if (db.AuditLogs.Any()) return;
            db.AuditLogs.AddRange(
                new AuditLog { Timestamp = DateTime.Now.AddHours(-1), Action = "Login", ActorName = "HT001", Details = "IP: 192.168.1.10" },
                new AuditLog { Timestamp = DateTime.Now.AddHours(-2), Action = "UpdateSetting", ActorName = "ADMIN", Details = "Changed SchoolYear to 2025-2026" }
            );
        }

        private static void SeedUsageLogs(AppDbContext db)
        {
            if (db.UsageLogs.Any()) return;
            db.UsageLogs.AddRange(
                new UsageLog { EventType = "AppOpen", Timestamp = DateTime.Now.AddHours(-5), UserRole = "Staff", EventData = "MachineName: PC-01" },
                new UsageLog { EventType = "LessonStart", Timestamp = DateTime.Now.AddHours(-4), UserRole = "Teacher", EventData = "LessonId: 101" }
            );
        }

        private static void SeedYouthUnionData(AppDbContext db)
        {
            if (db.YouthMembers.Any()) return; // Already seeded
            if (!db.Students.Any()) return; // No students to create members from

            using var tx = db.Database.BeginTransaction();
            try
            {
                // 1. Members from Students
                var students = db.Students.ToList();
                int idx = 0;
                foreach (var st in students)
                {
                    db.YouthMembers.Add(new YouthMember
                    {
                        StudentId = st.Id,
                        StudentName = st.FullName,
                        ClassName = string.IsNullOrEmpty(st.ClassName) ? "10A1" : st.ClassName,
                        MemberType = idx % 3 == 0 ? "Đội viên" : "Đoàn viên",
                        Status = "Active",
                        JoinDate = DateTime.Today.AddYears(-1),
                        Position = idx == 0 ? "Bí thư" : idx == 1 ? "Phó BT" : idx == 2 ? "UV BCH" : "Thành viên",
                        TotalScore = 80 + (idx % 40)
                    });
                    idx++;
                }
                db.SaveChanges();

                var members = db.YouthMembers.ToList();

                // 2. Activities
                var titles = new[] { "Chiến dịch Mùa hè Xanh", "Hội thảo 26/3", "Cuộc thi Nét đẹp Đội viên",
                    "Thăm mẹ VNAH", "Chiến dịch hiến máu nhân đạo", "Ngày hội sách",
                    "Cuộc thi sáng tạo KHKT", "Đại hội cháu ngoan Bác Hồ", "Tình nguyện dọn biển",
                    "Giao lưu văn nghệ Đoàn", "Hội thi rung chuông vàng", "Cuộc thi vẽ tranh Đội" };
                var statuses = new[] { "Completed", "Approved", "Completed", "Draft", "Approved", "Completed",
                    "Draft", "Completed", "Approved", "Completed", "Approved", "Completed" };
                for (int i = 0; i < titles.Length; i++)
                    db.YouthActivities.Add(new YouthActivity { Title = titles[i], Date = DateTime.Today.AddDays(-i * 5), Participants = $"Khối {10 + i % 3}", Points = 10 + i * 2, Evidence = i % 2 == 0 ? $"minh_chung_{i + 1}.jpg" : "", Status = statuses[i], Location = i % 2 == 0 ? "Sân trường" : "Hội trường", Description = $"Mô tả {titles[i]}", Budget = 500000 + (i * 200000) });
                db.SaveChanges();

                // 3. Recruitments (use real student IDs)
                var recruitNames = new[] { "Nguyễn Thị Lan", "Trần Đức Huy", "Lê Minh Châu", "Phạm Quốc Bảo", "Hoàng Thu Hà" };
                var recruitSts = new[] { "Applied", "Reviewing", "Approved", "Rejected", "Applied" };
                for (int i = 0; i < recruitNames.Length; i++)
                    db.YouthRecruitments.Add(new YouthRecruitment { StudentName = recruitNames[i], ClassName = $"10A{i % 3 + 1}", ApplyDate = DateTime.Today.AddDays(-i * 3), Status = recruitSts[i], Note = recruitSts[i] == "Approved" ? "Đã duyệt" : "Đang xem xét", ApprovedBy = recruitSts[i] == "Approved" ? "GV001" : "", Reason = $"Mong muốn cống hiến cho Đoàn ({i + 1})" });

                // 4. Attendances
                for (int i = 0; i < Math.Min(12, members.Count); i++)
                    db.YouthAttendances.Add(new YouthAttendance { MemberId = members[i % members.Count].Id, SessionDate = DateTime.Today.AddDays(-i * 7), SessionTitle = $"Sinh hoạt chuyên đề {i + 1}", SessionType = "Chi Đoàn", IsPresent = i % 5 != 0, Note = i % 5 == 0 ? "Vắng có phép" : "" });

                // 5. Event Registrations
                var actIds = db.YouthActivities.Select(a => a.Id).Take(5).ToList();
                if (actIds.Count > 0)
                {
                    var roles = new[] { "Participant", "Volunteer", "Leader" };
                    for (int i = 0; i < Math.Min(12, members.Count); i++)
                        db.YouthEventRegistrations.Add(new YouthEventRegistration { MemberId = members[i % members.Count].Id, ActivityId = actIds[i % actIds.Count], RegisteredAt = DateTime.Today.AddDays(-i * 4), Role = roles[i % roles.Length], AttendedAt = i % 3 != 0 ? DateTime.Today.AddDays(-i * 4 + 1) : null });
                }

                // 6. Emulation Scores (use null ActivityId instead of 0)
                for (int i = 0; i < Math.Min(12, members.Count); i++)
                {
                    int score = (i % 4 == 3) ? -5 : (10 + i);
                    db.YouthEmulationScores.Add(new YouthEmulationScore { MemberId = members[i % members.Count].Id, Score = score, Category = score < 0 ? "Discipline" : "Activity", Reason = $"Lý do cộng/trừ điểm {i + 1}", AwardedDate = DateTime.Today.AddDays(-i * 3), AwardedBy = "GV001" });
                }

                // 7. Award Proposals
                for (int i = 0; i < Math.Min(10, members.Count); i++)
                    db.YouthAwardProposals.Add(new YouthAwardProposal { MemberId = members[i % members.Count].Id, ProposalType = i % 2 == 0 ? "Cá nhân" : "Tập thể", Reason = $"Thành tích thi đua HK{i % 2 + 1} ({i + 1})", CreatedAt = DateTime.Today.AddDays(-i * 5), Status = i % 3 == 0 ? "Approved" : "Pending", ProposedBy = "GV001", ApprovedBy = i % 3 == 0 ? "BGH" : "" });

                // 8. Fees
                foreach (var period in new[] { "HK1-2026", "HK2-2026" })
                    for (int i = 0; i < Math.Min(members.Count, 10); i++)
                    {
                        bool paid = i % 3 != 0;
                        db.YouthFees.Add(new YouthFee { MemberId = members[i].Id, Amount = 20000, Period = period, PaidDate = paid ? DateTime.Today.AddDays(-(i + 1) * 5) : null, Status = paid ? "Paid" : "Unpaid", CollectedBy = paid ? "GV001" : "" });
                    }

                // 9. Votings
                for (int i = 0; i < 5; i++)
                    db.YouthVotings.Add(new YouthVoting { Title = $"Đại hội Đoàn lần {i + 1}", Description = $"Bầu BCH mới nhiệm kỳ {i + 1}", StartDate = DateTime.Today.AddDays(-i * 10), EndDate = DateTime.Today.AddDays(-i * 10 + 5), Status = i < 2 ? "Open" : "Closed", Candidates = "UCV 1, UCV 2, UCV 3" });

                // 10. Documents
                for (int i = 0; i < 5; i++)
                    db.YouthDocuments.Add(new YouthDocument { Title = $"Văn bản chỉ đạo số {i + 1}/HD-TD", Category = i % 2 == 0 ? "Quy chế" : "Hướng dẫn", FilePath = $"docs/youth_{i + 1}.pdf", UploadedAt = DateTime.Today.AddDays(-i * 15), UploadedBy = "GV001" });

                // 11. Plans
                for (int i = 0; i < 5; i++)
                    db.YouthPlans.Add(new YouthPlan { Title = $"Kế hoạch tháng {i + 1}", StartDate = DateTime.Today.AddMonths(i - 3), EndDate = DateTime.Today.AddMonths(i - 2), PeriodType = i < 4 ? "Month" : "Year", Status = i % 2 == 0 ? "Approved" : "Draft", CreatedBy = "GV001", CreatedAt = DateTime.Today.AddDays(-i * 5) });

                // 12. Budgets
                var budgets = new[] {
                    ("Thu đoàn phí HK1", "Income", 8500000m), ("Tài trợ từ Hội PH", "Income", 5000000m),
                    ("Chi khen thưởng Hội thảo", "Expense", 1500000m), ("Chi VPP Đại hội", "Expense", 350000m),
                    ("Thu đoàn phí HK2", "Income", 7200000m) };
                for (int i = 0; i < budgets.Length; i++)
                    db.YouthBudgets.Add(new YouthBudget { Title = budgets[i].Item1, Type = budgets[i].Item2, Amount = budgets[i].Item3, Date = DateTime.Today.AddDays(-i * 7), Category = budgets[i].Item2 == "Income" ? "Đoàn phí" : "Chi phí", Note = $"Nội dung {i + 1}", CreatedBy = "GV001" });

                db.SaveChanges();
                tx.Commit();
                Serilog.Log.Information("[StaffDataSeeder] Youth Union data seeded successfully.");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                Serilog.Log.Error(ex, "[StaffDataSeeder] Youth Union seed failed, rolled back.");
            }
        }
    }
}

