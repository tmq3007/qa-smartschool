using System;
using System.Collections.Generic;
using System.Linq;
using QASmartClass.Classroom.Services;
using Serilog;

namespace QASmartClass.Data
{
    /// <summary>
    /// Seed dữ liệu mẫu trường THPT Quang Ân
    /// 6 lớp (10A1, 10A2, 11A1, 11A2, 12A1, 12A2), 10 GV, ~240 HS
    /// </summary>
    public static partial class SampleDataSeeder
    {
        public static bool SeedIfEmpty(AppDbContext db, ClassRosterService rosterService)
        {
            if (db.ClassRosters.Any()) return false; // Đã có dữ liệu

            using var transaction = db.Database.BeginTransaction();
            Log.Information("[Seed] Bắt đầu tạo dữ liệu mẫu trường THPT Quang Ân...");

            // ═══ 1. Giáo viên ═══
            var teachers = new (string Name, string Subject, string Title)[]
            {
                ("Nguyễn Văn Hùng", "Toán", "ThS"),
                ("Trần Thị Mai", "Vật lý", "ThS"),
                ("Lê Hoàng Nam", "Hóa học", "TS"),
                ("Phạm Thị Hương", "Sinh học", "ThS"),
                ("Hoàng Đức Minh", "Ngữ văn", "ThS"),
                ("Vũ Thị Lan", "Tiếng Anh", "ThS"),
                ("Đỗ Quang Trung", "Lịch sử", "GV"),
                ("Ngô Thị Hạnh", "Địa lý", "GV"),
                ("Bùi Minh Đức", "Tin học", "ThS"),
                ("Lý Thị Thu", "GDCD", "GV"),
            };

            // Cập nhật TeacherProfile chính
            var mainTeacher = db.TeacherProfiles.FirstOrDefault();
            if (mainTeacher == null)
            {
                mainTeacher = new TeacherProfile
                {
                    FullName = "Nguyễn Văn Hùng", Subject = "Toán học",
                    School = "THPT Quang Ân", Title = "ThS"
                };
                db.TeacherProfiles.Add(mainTeacher);
                db.SaveChanges();
            }

            // ═══ 2. Tạo Roster + HS ═══
            var classData = new (string Class, string Grade, string Subject, string Teacher, string[][] Students)[]
            {
                ("10A1", "10", "Toán", "Nguyễn Văn Hùng", Gen10A1()),
                ("10A2", "10", "Vật lý", "Trần Thị Mai", Gen10A2()),
                ("11A1", "11", "Hóa học", "Lê Hoàng Nam", Gen11A1()),
                ("11A2", "11", "Ngữ văn", "Hoàng Đức Minh", Gen11A2()),
                ("12A1", "12", "Tiếng Anh", "Vũ Thị Lan", Gen12A1()),
                ("12A2", "12", "Tin học", "Bùi Minh Đức", Gen12A2()),
            };

            foreach (var (cls, grade, subject, teacher, students) in classData)
            {
                var roster = rosterService.CreateRoster(cls, grade, subject, teacher, "2025-2026", "HK2");

                int stt = 0;
                foreach (var s in students)
                {
                    stt++;
                    var student = new Student
                    {
                        FullName = s[0],
                        StudentCode = s[1],
                        ClassName = cls,
                        SchoolName = "THPT Quang Ân",
                        IsOnline = false,
                        LastSeen = DateTime.Now
                    };
                    db.Students.Add(student);
                    db.SaveChanges();

                    rosterService.AddStudentToRoster(roster.Id, student.Id, stt, s.Length > 2 ? s[2] : "");
                }
                Log.Information("[Seed] Lớp {Class}: {Count} HS", cls, students.Length);
            }

            // ═══ 3. Thời khóa biểu + Bài giảng ═══
            SeedTimetableAndLessons(db);

            // ═══ 4. Quiz + Câu hỏi ═══
            SeedQuizzesAndQuestions(db);

            // ═══ 5. Ngân hàng câu hỏi ═══
            SeedQuestionBank(db);

            // ═══ 6. Nhật ký sự kiện ═══
            SeedEventLogs(db);

            transaction.Commit();
            Log.Information("[Seed] Hoàn tất! Đã tạo {Count} lớp + TKB + bài giảng + quiz + NHCH.", classData.Length);
            return true;
        }

        // ═══ Dữ liệu HS từng lớp ═══
        // Mỗi phần tử: [Họ tên, Mã HS, Ghi chú (optional)]

        private static string[][] Gen10A1() => new[]
        {
            // Sắp xếp theo chuẩn VN: Tên → Họ → Đệm
            new[]{"Nguyễn Văn An","10A1-001","Lớp trưởng"},
            new[]{"Trần Thị Bích","10A1-002","Lớp phó HT"},
            new[]{"Trịnh Văn Bảo","10A1-003"},
            new[]{"Võ Thị Cẩm","10A1-004"},
            new[]{"Lê Hoàng Cường","10A1-005"},
            new[]{"Phạm Thị Dung","10A1-006"},
            new[]{"Dương Minh Đạt","10A1-007"},
            new[]{"Hoàng Văn Đức","10A1-008","Tổ trưởng 1"},
            new[]{"Lê Thị Giang","10A1-009"},
            new[]{"Đỗ Quang Hải","10A1-010"},
            new[]{"Vũ Thị Hoa","10A1-011"},
            new[]{"Trần Đức Hùng","10A1-012"},
            new[]{"Bùi Đức Khang","10A1-013"},
            new[]{"Nguyễn Thị Khánh","10A1-014"},
            new[]{"Lý Thị Lan","10A1-015","Tổ trưởng 2"},
            new[]{"Phạm Văn Long","10A1-016"},
            new[]{"Hoàng Thị Mai","10A1-017"},
            new[]{"Mai Văn Minh","10A1-018"},
            new[]{"Đỗ Văn Nam","10A1-019"},
            new[]{"Đinh Thị Ngọc","10A1-020"},
            new[]{"Vũ Thị Oanh","10A1-021"},
            new[]{"Trương Quốc Phong","10A1-022"},
            new[]{"Ngô Thị Phương","10A1-023"},
            new[]{"Bùi Quốc Phúc","10A1-024"},
            new[]{"Đặng Thị Quỳnh","10A1-025"},
            new[]{"Lý Thị Quyên","10A1-026"},
            new[]{"Hà Sĩ Ren","10A1-027","Tổ trưởng 3"},
            new[]{"Cao Thị Sen","10A1-028"},
            new[]{"Mai Văn Sơn","10A1-029"},
            new[]{"Đinh Thị Thanh","10A1-030","Lớp phó LĐ"},
            new[]{"Hà Minh Trí","10A1-031","Bí thư Đoàn"},
            new[]{"Tô Văn Tuấn","10A1-032"},
            new[]{"Cao Văn Uy","10A1-033"},
            new[]{"Phan Thị Uyên","10A1-034"},
            new[]{"Phan Thị Vân","10A1-035"},
            new[]{"Lương Văn Vũ","10A1-036"},
            new[]{"Ngô Đức Xuân A","10A1-037"},
            new[]{"Châu Thị Xuân B","10A1-038","Tổ trưởng 4"},
            new[]{"Đặng Thị Yên","10A1-039"},
            new[]{"Nguyễn Hoàng Yến","10A1-040"},
        };

        private static string[][] Gen10A2() => new[]
        {
            // Sắp xếp theo chuẩn VN: Tên → Họ → Đệm
            new[]{"Hoàng Đức Anh A","10A2-001"},
            new[]{"Lê Văn Anh B","10A2-002","Lớp trưởng"},
            new[]{"Nguyễn Thị Bình","10A2-003","Lớp phó HT"},
            new[]{"Vũ Văn Cảnh","10A2-004"},
            new[]{"Phạm Hoàng Châu","10A2-005"},
            new[]{"Trần Thị Diệu","10A2-006"},
            new[]{"Bùi Thị Duyên","10A2-007"},
            new[]{"Hoàng Văn Em","10A2-008","Tổ trưởng 1"},
            new[]{"Lý Văn Hà","10A2-009"},
            new[]{"Vũ Thị Hằng","10A2-010"},
            new[]{"Mai Thị Hạnh","10A2-011"},
            new[]{"Đỗ Minh Hiếu","10A2-012"},
            new[]{"Đinh Văn Hoàn","10A2-013","Lớp phó LĐ"},
            new[]{"Ngô Thị Huệ","10A2-014"},
            new[]{"Cao Thị Hương","10A2-015"},
            new[]{"Phan Văn Khải","10A2-016"},
            new[]{"Bùi Văn Kiên","10A2-017"},
            new[]{"Ngô Thị Kim","10A2-018"},
            new[]{"Đặng Văn Lâm","10A2-019"},
            new[]{"Hà Thị Lệ","10A2-020"},
            new[]{"Lý Thị Liên","10A2-021","Tổ trưởng 2"},
            new[]{"Mai Đức Lộc","10A2-022"},
            new[]{"Trương Văn Mạnh","10A2-023"},
            new[]{"Đinh Thị Mỹ","10A2-024"},
            new[]{"Lê Thị Nga","10A2-025","Bí thư Đoàn"},
            new[]{"Đỗ Thị Bảo Ngọc","10A2-026"},
            new[]{"Trương Văn Nghĩa","10A2-027"},
            new[]{"Đặng Thị Như","10A2-028"},
            new[]{"Hà Văn Phát","10A2-029","Tổ trưởng 3"},
            new[]{"Cao Thị Phượng","10A2-030"},
            new[]{"Tô Đức Quân","10A2-031"},
            new[]{"Phan Thị Quyên","10A2-032"},
            new[]{"Lương Văn Sang","10A2-033"},
            new[]{"Châu Thị Thảo","10A2-034","Tổ trưởng 4"},
            new[]{"Nguyễn Minh Thành","10A2-035"},
            new[]{"Trịnh Thị Thu","10A2-036"},
            new[]{"Võ Văn Tiến","10A2-037"},
            new[]{"Dương Thị Trang","10A2-038"},
            new[]{"Lê Văn Trung","10A2-039"},
            new[]{"Trần Thị Tuyết","10A2-040"},
            new[]{"Nguyễn Văn Vinh","10A2-041"},
            new[]{"Phạm Thị Xuân","10A2-042"},
        };

        private static string[][] Gen11A1() => new[]
        {
            // Sắp xếp theo chuẩn VN: Tên → Họ → Đệm
            new[]{"Vũ Thị Ngọc Anh","11A1-001"},
            new[]{"Hà Quốc Bảo","11A1-002","Tổ trưởng 3"},
            new[]{"Cao Thị Kim Chi","11A1-003"},
            new[]{"Vũ Đức Dũng","11A1-004"},
            new[]{"Phan Thị Mỹ Duyên","11A1-005"},
            new[]{"Tô Minh Đăng","11A1-006"},
            new[]{"Ngô Thị Thu Hà","11A1-007"},
            new[]{"Cao Thị Ngọc Hân","11A1-008"},
            new[]{"Lê Thị Thanh Hương","11A1-009"},
            new[]{"Lương Đức Huy","11A1-010"},
            new[]{"Bùi Minh Khôi","11A1-011"},
            new[]{"Ngô Thị Diệu Linh","11A1-012","Bí thư Đoàn"},
            new[]{"Lý Thị Phương Linh","11A1-013","Tổ trưởng 2"},
            new[]{"Bùi Thị Hồng Loan","11A1-014"},
            new[]{"Nguyễn Hoàng Long","11A1-015"},
            new[]{"Phan Đức Lợi","11A1-016"},
            new[]{"Trịnh Thị Thanh Mai","11A1-017"},
            new[]{"Nguyễn Thị Hà My","11A1-018","Lớp phó HT"},
            new[]{"Đinh Thị Bích Ngọc","11A1-019"},
            new[]{"Châu Thị Ánh Nguyệt","11A1-020","Tổ trưởng 4"},
            new[]{"Võ Minh Nhật","11A1-021"},
            new[]{"Đặng Thị Hồng Nhung","11A1-022"},
            new[]{"Lý Văn Phong","11A1-023"},
            new[]{"Trương Hoàng Phú","11A1-024"},
            new[]{"Trần Minh Quân","11A1-025","Lớp trưởng"},
            new[]{"Phạm Thị Diễm Quỳnh","11A1-026"},
            new[]{"Lê Đức Tâm","11A1-027"},
            new[]{"Dương Thị Phương Thảo","11A1-028"},
            new[]{"Nguyễn Văn Thắng","11A1-029"},
            new[]{"Hoàng Văn Thiện","11A1-030","Tổ trưởng 1"},
            new[]{"Phạm Đức Toàn","11A1-031"},
            new[]{"Mai Thị Thu Trang","11A1-032"},
            new[]{"Hoàng Minh Trí","11A1-033"},
            new[]{"Mai Đức Trường","11A1-034"},
            new[]{"Đỗ Thị Cẩm Tú","11A1-035"},
            new[]{"Đinh Quốc Việt","11A1-036","Lớp phó LĐ"},
            new[]{"Đỗ Quang Vinh","11A1-037"},
            new[]{"Trần Thị Hải Yến","11A1-038"},
        };

        private static string[][] Gen11A2() => new[]
        {
            // Sắp xếp theo chuẩn VN: Tên → Họ → Đệm
            new[]{"Đỗ Thị Phương Anh A","11A2-001"},
            new[]{"Lương Hoàng Anh B","11A2-002"},
            new[]{"Lý Minh Châu","11A2-003"},
            new[]{"Võ Quốc Cường","11A2-004"},
            new[]{"Bùi Thị Ngọc Diệp","11A2-005"},
            new[]{"Phạm Văn Đại","11A2-006"},
            new[]{"Tô Văn Đức","11A2-007"},
            new[]{"Ngô Thị Hải Đường","11A2-008"},
            new[]{"Mai Thị Hương Giang","11A2-009"},
            new[]{"Đặng Thị Ngọc Hà","11A2-010"},
            new[]{"Cao Thị Mỹ Hạnh","11A2-011"},
            new[]{"Lê Minh Hiếu","11A2-012"},
            new[]{"Trần Thị Bích Huyền","11A2-013"},
            new[]{"Mai Quốc Hưng","11A2-014"},
            new[]{"Đinh Hoàng Khải","11A2-015"},
            new[]{"Bùi Văn Khoa","11A2-016"},
            new[]{"Nguyễn Thị Thuỳ Linh","11A2-017","Lớp phó HT"},
            new[]{"Hoàng Đức Mạnh","11A2-018","Tổ trưởng 1"},
            new[]{"Trần Thị Kim Ngân","11A2-019"},
            new[]{"Dương Thị Thanh Ngọc","11A2-020"},
            new[]{"Đinh Thị Minh Nguyệt","11A2-021"},
            new[]{"Phan Thị Yến Nhi","11A2-022"},
            new[]{"Phan Văn Nghĩa","11A2-023","Lớp phó LĐ"},
            new[]{"Phạm Thị Hồng Nhung A","11A2-024"},
            new[]{"Trịnh Thị Tuyết Nhung B","11A2-025"},
            new[]{"Trương Đức Phát","11A2-026"},
            new[]{"Lê Hoàng Phúc","11A2-027","Lớp trưởng"},
            new[]{"Châu Thị Bích Phượng","11A2-028","Tổ trưởng 4"},
            new[]{"Đỗ Minh Quang","11A2-029"},
            new[]{"Đặng Minh Quân","11A2-030"},
            new[]{"Nguyễn Hoàng Sơn","11A2-031"},
            new[]{"Hoàng Văn Tài","11A2-032"},
            new[]{"Ngô Thị Thanh Tâm","11A2-033"},
            new[]{"Nguyễn Đức Thịnh","11A2-034"},
            new[]{"Cao Thị Hoài Thu","11A2-035"},
            new[]{"Lý Thị Thanh Thúy","11A2-036","Tổ trưởng 2"},
            new[]{"Vũ Đức Toàn","11A2-037"},
            new[]{"Vũ Thị Bảo Trâm","11A2-038"},
            new[]{"Hà Thị Huyền Trâm","11A2-039","Bí thư Đoàn"},
            new[]{"Hà Minh Tuấn","11A2-040","Tổ trưởng 3"},
        };

        private static string[][] Gen12A1() => new[]
        {
            // Sắp xếp theo chuẩn VN: Tên → Họ → Đệm
            new[]{"Lê Đức Anh","12A1-001"},
            new[]{"Cao Thị Ngọc Ánh","12A1-002","Lớp phó LĐ"},
            new[]{"Trương Hoàng Bách","12A1-003"},
            new[]{"Đỗ Thị Minh Châu","12A1-004"},
            new[]{"Tô Đức Chính","12A1-005"},
            new[]{"Ngô Thị Kim Cúc","12A1-006"},
            new[]{"Mai Thị Phương Dung","12A1-007"},
            new[]{"Hoàng Quốc Dũng","12A1-008","Tổ trưởng 1"},
            new[]{"Nguyễn Hoàng Duy","12A1-009"},
            new[]{"Lý Minh Đạt","12A1-010"},
            new[]{"Đỗ Văn Hào","12A1-011"},
            new[]{"Hà Thị Thu Hiền","12A1-012"},
            new[]{"Bùi Minh Hiển","12A1-013"},
            new[]{"Đặng Thị Quỳnh Hoa","12A1-014"},
            new[]{"Bùi Thị Diệu Hương","12A1-015"},
            new[]{"Phan Thị Ngọc Huyền","12A1-016"},
            new[]{"Lương Minh Khải","12A1-017"},
            new[]{"Dương Thị Ngọc Lan","12A1-018"},
            new[]{"Trần Thị Bích Liên","12A1-019"},
            new[]{"Hà Văn Lực","12A1-020","Tổ trưởng 3"},
            new[]{"Lý Thị Tuyết Mai","12A1-021","Tổ trưởng 2"},
            new[]{"Võ Đức Mạnh","12A1-022"},
            new[]{"Vũ Thị Diễm My","12A1-023"},
            new[]{"Lê Thị Bảo Ngân","12A1-024","Bí thư Đoàn"},
            new[]{"Lê Quang Nghị","12A1-025"},
            new[]{"Trịnh Thị Cẩm Nhung","12A1-026"},
            new[]{"Ngô Thị Kim Oanh","12A1-027"},
            new[]{"Nguyễn Thị Hồng Phúc","12A1-028","Lớp phó HT"},
            new[]{"Trần Thị Thu Phương","12A1-029"},
            new[]{"Nguyễn Văn Quốc","12A1-030"},
            new[]{"Đặng Văn Sỹ","12A1-031"},
            new[]{"Đinh Hoàng Thái","12A1-032"},
            new[]{"Hoàng Đức Thắng","12A1-033"},
            new[]{"Mai Đức Thuận","12A1-034"},
            new[]{"Phạm Thị Hương Trà","12A1-035"},
            new[]{"Phạm Minh Triết","12A1-036","Lớp trưởng"},
            new[]{"Phan Đức Trung","12A1-037"},
            new[]{"Vũ Quốc Tuấn","12A1-038"},
            new[]{"Trương Minh Tú","12A1-039"},
            new[]{"Đinh Thị Ánh Tuyết","12A1-040"},
            new[]{"Châu Thị Bích Vân","12A1-041","Tổ trưởng 4"},
            new[]{"Cao Thị Thanh Xuân","12A1-042"},
        };

        private static string[][] Gen12A2() => new[]
        {
            // Sắp xếp theo chuẩn VN: Tên → Họ → Đệm
            new[]{"Đỗ Quốc Anh","12A2-001"},
            new[]{"Cao Thị Nguyệt Ánh","12A2-002"},
            new[]{"Đỗ Thị Hải Châu A","12A2-003"},
            new[]{"Phan Thị Bảo Châu B","12A2-004"},
            new[]{"Lê Minh Công","12A2-005"},
            new[]{"Đặng Thị Kim Dung","12A2-006"},
            new[]{"Lý Hoàng Đạt","12A2-007"},
            new[]{"Võ Hoàng Đức","12A2-008"},
            new[]{"Trương Đức Hải","12A2-009"},
            new[]{"Trịnh Thị Thanh Hằng","12A2-010"},
            new[]{"Ngô Thị Bích Hạnh","12A2-011"},
            new[]{"Hoàng Minh Hiếu","12A2-012","Tổ trưởng 1"},
            new[]{"Mai Thị Diễm Hương","12A2-013"},
            new[]{"Vũ Thị Lan Hương","12A2-014"},
            new[]{"Trần Quốc Hưng","12A2-015","Lớp trưởng"},
            new[]{"Đặng Hoàng Khang","12A2-016","Bí thư Đoàn"},
            new[]{"Đinh Văn Khánh","12A2-017"},
            new[]{"Bùi Văn Lâm","12A2-018"},
            new[]{"Đinh Thị Mỹ Lệ","12A2-019"},
            new[]{"Bùi Thị Kim Liên","12A2-020"},
            new[]{"Lương Đức Long","12A2-021"},
            new[]{"Cao Thị Bảo Ly","12A2-022"},
            new[]{"Tô Minh Nhân","12A2-023"},
            new[]{"Ngô Thị Minh Nguyệt","12A2-024"},
            new[]{"Lê Thị Hồng Nhung","12A2-025"},
            new[]{"Dương Thị Kiều Oanh","12A2-026"},
            new[]{"Nguyễn Đức Phú","12A2-027"},
            new[]{"Mai Hoàng Phong","12A2-028"},
            new[]{"Hoàng Văn Quý","12A2-029"},
            new[]{"Vũ Minh Tâm","12A2-030"},
            new[]{"Phan Quốc Thái","12A2-031","Lớp phó LĐ"},
            new[]{"Nguyễn Văn Thành","12A2-032"},
            new[]{"Lý Thị Thu Thảo","12A2-033","Tổ trưởng 2"},
            new[]{"Phạm Thị Ngọc Trâm","12A2-034"},
            new[]{"Trần Thị Huyền Trang","12A2-035"},
            new[]{"Phạm Đức Tùng","12A2-036"},
            new[]{"Châu Thị Thanh Tuyền","12A2-037","Tổ trưởng 4"},
            new[]{"Nguyễn Thị Phương Uyên","12A2-038","Lớp phó HT"},
            new[]{"Hà Quang Vinh","12A2-039","Tổ trưởng 3"},
        };
    }
}
