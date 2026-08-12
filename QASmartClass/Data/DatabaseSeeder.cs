using Microsoft.EntityFrameworkCore;
using System;

namespace QASmartClass.Data
{
    /// <summary>
    /// Khởi tạo dữ liệu mẫu để demo khi lần đầu chạy
    /// </summary>
    public static class DatabaseSeeder
    {
        public static void SeedIfEmpty(AppDbContext db)
        {
            // Tạo bảng nếu chưa có
            db.Database.EnsureCreated();

            // Chỉ seed khi DB trống
            if (db.Lessons.Any()) return;

            var now = DateTime.Now;

            // ═══ SEED CLASSROOMS ═══
            var classroom = new Classroom
            {
                Name = "Toán 10A",
                TeacherName = "Nguyễn Văn Thầy",
                ClassCode = "TOAN10A-2026",
                MaxStudents = 1,
                CreatedAt = now
            };
            db.Classrooms.Add(classroom);
            db.SaveChanges();

            // ═══ SEED STUDENTS ═══
            db.Students.Add(new Student
            {
                FullName = "Nguyễn Văn An",
                StudentCode = "HS001",
                ClassroomId = classroom.Id,
                PCName = "PC-01",
                IPAddress = "192.168.1.100",
                IsOnline = false,
                LastSeen = now,
                SchoolName = "THPT QA",
                ClassName = "10A",
                Cohort = "K46",
                Role = "HS",
                Status = "Active",
                ParentName = "PH An",
                ParentPhone = "0901234567"
            });

            // ═══ SEED LESSONS (with Timetable) ═══
            // (title, subject, grade, desc, fav, day, period, class, teacher, subTeacher, subReason, week, semester)
            var lessonData = new (string title, string subject, string grade, string desc, bool fav,
                                   string day, int period, string cls, string teacher, string subTeacher, string subReason, int week, string semester)[]
            {
                // ═══ TOÁN LỚP 10 — Chương trình chuẩn (SGK mới 2022) ═══
                // GV: Nguyễn Văn A — Lớp 10A — THPT QA
                // Mã: C{chương}.{bài} — Tên bài

                // ── CHƯƠNG 1: MỆNH ĐỀ VÀ TẬP HỢP ──
                ("C1.1 — Mệnh đề toán học", "Toán", "Lớp 10",
                 "Mệnh đề, mệnh đề chứa biến, phủ định, kéo theo, tương đương. Bài tập áp dụng.", true,
                 "Thứ 2", 1, "10A", "Nguyễn Văn A", "", "", 1, "HK1"),
                ("C1.2 — Tập hợp và các phép toán", "Toán", "Lớp 10",
                 "Tập hợp con, hợp, giao, hiệu, phần bù. Biểu đồ Venn minh họa.", true,
                 "Thứ 4", 2, "10A", "Nguyễn Văn A", "", "", 2, "HK1"),

                // ── CHƯƠNG 2: BẤT PHƯƠNG TRÌNH BẬC NHẤT HAI ẨN ──
                ("C2.1 — Bất phương trình bậc nhất hai ẩn", "Toán", "Lớp 10",
                 "Miền nghiệm, biểu diễn hình học, hệ bất phương trình.", false,
                 "Thứ 6", 1, "10A", "Nguyễn Văn A", "", "", 3, "HK1"),
                ("C2.2 — Bài toán tối ưu (Quy hoạch tuyến tính)", "Toán", "Lớp 10",
                 "Bài toán tìm GTLN, GTNN của biểu thức tuyến tính trên miền đa giác.", false,
                 "Thứ 2", 1, "10A", "Nguyễn Văn A", "", "", 4, "HK1"),

                // ── CHƯƠNG 3: HÀM SỐ BẬC HAI VÀ ĐỒ THỊ ──
                ("C3.1 — Hàm số và đồ thị hàm số", "Toán", "Lớp 10",
                 "Khái niệm hàm số, tập xác định, tập giá trị, đồ thị hàm số.", true,
                 "Thứ 4", 2, "10A", "Nguyễn Văn A", "", "", 5, "HK1"),
                ("C3.2 — Hàm số bậc nhất y = ax + b", "Toán", "Lớp 10",
                 "Tính chất, đồ thị, hệ số góc, điều kiện song song / cắt nhau.", true,
                 "Thứ 6", 1, "10A", "Nguyễn Văn A", "", "", 6, "HK1"),
                ("C3.3 — Hàm số bậc hai y = ax² + bx + c", "Toán", "Lớp 10",
                 "Parabol, đỉnh, trục đối xứng, bảng biến thiên, vẽ đồ thị.", true,
                 "Thứ 2", 1, "10A", "Nguyễn Văn A", "", "", 7, "HK1"),

                // ── CHƯƠNG 4: PHƯƠNG TRÌNH — HỆ PHƯƠNG TRÌNH ──
                ("C4.1 — Phương trình bậc hai", "Toán", "Lớp 10",
                 "Công thức nghiệm, biệt thức Δ, định lý Viète, phân tích nhân tử.", true,
                 "Thứ 4", 2, "10A", "Nguyễn Văn A", "", "", 8, "HK1"),
                ("C4.2 — Phương trình quy về bậc hai", "Toán", "Lớp 10",
                 "PT trùng phương, PT chứa ẩn ở mẫu, PT vô tỉ.", false,
                 "Thứ 6", 1, "10A", "Nguyễn Văn A", "", "", 9, "HK1"),
                ("C4.3 — Hệ phương trình bậc nhất ba ẩn", "Toán", "Lớp 10",
                 "Giải hệ 3 ẩn bằng phương pháp Gauss, bài toán thực tế.", false,
                 "Thứ 2", 1, "10A", "Nguyễn Văn A", "", "", 10, "HK1"),

                // ── CHƯƠNG 5: HỆ THỨC LƯỢNG TRONG TAM GIÁC ──
                ("C5.1 — Giá trị lượng giác của góc", "Toán", "Lớp 10",
                 "sin, cos, tan, cot; đường tròn lượng giác; góc liên quan đặc biệt.", true,
                 "Thứ 4", 2, "10A", "Nguyễn Văn A", "", "", 20, "HK2"),
                ("C5.2 — Định lý cosin và định lý sin", "Toán", "Lớp 10",
                 "Áp dụng định lý cosin, sin để giải tam giác. Công thức diện tích.", false,
                 "Thứ 6", 1, "10A", "Nguyễn Văn A", "", "", 21, "HK2"),

                // ── CHƯƠNG 6: VECTƠ ──
                ("C6.1 — Khái niệm vectơ và các phép toán", "Toán", "Lớp 10",
                 "Vectơ, cùng phương, cùng hướng, tổng, hiệu, tích với số.", true,
                 "Thứ 2", 1, "10A", "Nguyễn Văn A", "", "", 23, "HK2"),
                ("C6.2 — Tích vô hướng và ứng dụng", "Toán", "Lớp 10",
                 "Tích vô hướng, góc giữa hai vectơ, vuông góc, hình chiếu.", false,
                 "Thứ 4", 2, "10A", "Nguyễn Văn A", "", "", 24, "HK2"),

                // ── CHƯƠNG 7: THỐNG KÊ ──
                ("C7.1 — Số gần đúng và sai số", "Toán", "Lớp 10",
                 "Số gần đúng, sai số tuyệt đối, sai số tương đối, quy tắc làm tròn.", false,
                 "Thứ 6", 1, "10A", "Nguyễn Văn A", "", "", 26, "HK2"),
                ("C7.2 — Bảng tần số và biểu đồ", "Toán", "Lớp 10",
                 "Bảng phân bố tần số, tần suất; biểu đồ cột, đường, tròn; phân tích dữ liệu.", true,
                 "Thứ 2", 1, "10A", "Nguyễn Văn A", "", "", 27, "HK2"),

                // ── CHƯƠNG 8: XÁC SUẤT ──
                ("C8.1 — Phép thử và biến cố", "Toán", "Lớp 10",
                 "Không gian mẫu, biến cố, biến cố đối, biến cố xung khắc, biến cố độc lập.", true,
                 "Thứ 4", 2, "10A", "Nguyễn Văn A", "", "", 29, "HK2"),
                ("C8.2 — Xác suất của biến cố", "Toán", "Lớp 10",
                 "Định nghĩa cổ điển, quy tắc cộng, quy tắc nhân. Bài toán thực tế.", false,
                 "Thứ 6", 1, "10A", "Nguyễn Văn A", "", "", 30, "HK2"),
            };

            var rndLesson = new Random(42);
            var statusCycle = new[] { "Approved", "Approved", "Taught", "Draft", "Approved", "Taught", "Approved", "Draft", "Taught", "Approved", "Approved", "Draft", "Taught", "Approved", "Approved", "Approved", "Taught", "Approved" };
            var typeCycle   = new[] { "Normal", "STEAM", "Normal", "Normal", "Workshop", "Normal", "Normal", "Normal", "Exam", "Normal", "STEAM", "Normal", "Normal", "Workshop", "Normal", "Normal", "Normal", "Normal" };
            int i2 = 0;
            foreach (var ld in lessonData)
            {
                var lesson = new Lesson
                {
                    Title = ld.title,
                    Subject = ld.subject,
                    Grade = ld.grade,
                    Description = ld.desc,
                    IsFavorite = ld.fav,
                    Status = statusCycle[i2 % statusCycle.Length],
                    LessonType = typeCycle[i2 % typeCycle.Length],
                    DurationMinutes = rndLesson.Next(0, 3) == 0 ? 90 : 45,
                    UseCount = statusCycle[i2 % statusCycle.Length] == "Taught" ? rndLesson.Next(1, 8) : 0,
                    LastTaughtAt = statusCycle[i2 % statusCycle.Length] == "Taught" ? now.AddDays(-rndLesson.Next(1, 30)) : null,
                    // Timetable fields
                    DayOfWeek = ld.day,
                    Period = ld.period,
                    ClassName = ld.cls,
                    WeekNumber = ld.week,
                    Semester = ld.semester,
                    TeacherName = ld.teacher,
                    SubstituteTeacher = ld.subTeacher,
                    SubstituteReason = ld.subReason,
                    CreatedAt = now.AddDays(-rndLesson.Next(1, 90)),
                    UpdatedAt = now.AddDays(-rndLesson.Next(0, 14))
                };
                db.Lessons.Add(lesson);
                i2++;
            }
            db.SaveChanges();

            // ═══ SEED LESSON CONTENTS ═══
            SeedLessonContents(db);

            // ═══ SEED QUIZZES ═══
            var quiz1 = new Quiz
            {
                Title = "Kiểm tra nhanh — C3.2 Hàm số bậc nhất",
                QuizType = "Competition",
                TimeLimitSeconds = 300,
                LessonId = 1,
                CreatedAt = now
            };
            var quiz2 = new Quiz
            {
                Title = "Trắc nghiệm — C4.1 Phương trình bậc hai",
                QuizType = "Test",
                TimeLimitSeconds = 600,
                LessonId = 2,
                CreatedAt = now.AddDays(-3)
            };
            var quiz3 = new Quiz
            {
                Title = "Khảo sát — Mức độ hiểu bài",
                QuizType = "Survey",
                TimeLimitSeconds = 120,
                LessonId = 1,
                CreatedAt = now.AddDays(-1)
            };
            db.Quizzes.AddRange(quiz1, quiz2, quiz3);
            db.SaveChanges();

            // ── Quiz 1 Questions ──
            var q1Questions = new[]
            {
                ("Hệ số góc của hàm số y = 2x + 1 là bao nhiêu?", "[\"A. 1\",\"B. 2\",\"C. -1\",\"D. 3\"]", "B", 10, "Easy"),
                ("Hàm số y = ax + b đồng biến khi:", "[\"A. a > 0\",\"B. a < 0\",\"C. b > 0\",\"D. b < 0\"]", "A", 10, "Easy"),
                ("Đồ thị hàm số y = -x + 2 cắt trục Ox tại điểm có tọa độ:", "[\"A. (2, 0)\",\"B. (-2, 0)\",\"C. (0, 2)\",\"D. (1, 0)\"]", "A", 15, "Medium"),
                ("Hàm số y = 3x - 6 bằng 0 khi x =", "[\"A. 1\",\"B. 2\",\"C. 3\",\"D. -2\"]", "B", 15, "Medium"),
                ("Hai đường thẳng y = 2x + 1 và y = 2x - 3 có quan hệ như thế nào?", "[\"A. Cắt nhau\",\"B. Song song\",\"C. Trùng nhau\",\"D. Vuông góc\"]", "B", 20, "Hard"),
            };
            int qOrder = 0;
            foreach (var (content, opts, ans, pts, diff) in q1Questions)
            {
                db.Questions.Add(new Question
                {
                    QuizId = quiz1.Id, Content = content, QuestionType = "MultipleChoice",
                    OptionsJson = opts, CorrectAnswer = ans, Points = pts,
                    Difficulty = diff, SortOrder = qOrder++
                });
            }

            // ── Quiz 2 Questions ──
            var q2Questions = new[]
            {
                ("Biệt thức Δ của PT ax² + bx + c = 0 là:", "[\"A. b² - 4ac\",\"B. b² + 4ac\",\"C. 4ac - b²\",\"D. 2ac - b\"]", "A", 10, "Easy"),
                ("PT x² - 5x + 6 = 0 có nghiệm:", "[\"A. x=2, x=3\",\"B. x=-2, x=-3\",\"C. x=1, x=6\",\"D. x=-1, x=-6\"]", "A", 10, "Easy"),
                ("PT x² + 4x + 4 = 0 có bao nhiêu nghiệm?", "[\"A. 0\",\"B. 1 (kép)\",\"C. 2\",\"D. Vô số\"]", "B", 15, "Medium"),
                ("Theo Viète: tổng 2 nghiệm x₁+x₂ của 2x² - 6x + 1 = 0 là:", "[\"A. 3\",\"B. -3\",\"C. 6\",\"D. 0.5\"]", "A", 15, "Medium"),
                ("PT nào vô nghiệm?", "[\"A. x²-4=0\",\"B. x²+1=0\",\"C. x²-x=0\",\"D. x²-2x+1=0\"]", "B", 20, "Hard"),
            };
            qOrder = 0;
            foreach (var (content, opts, ans, pts, diff) in q2Questions)
            {
                db.Questions.Add(new Question
                {
                    QuizId = quiz2.Id, Content = content, QuestionType = "MultipleChoice",
                    OptionsJson = opts, CorrectAnswer = ans, Points = pts,
                    Difficulty = diff, SortOrder = qOrder++
                });
            }

            // ── Quiz 3 (Survey) Questions ──
            var q3Questions = new[]
            {
                ("Em hiểu bài ở mức nào?", "[\"A. Rất hiểu\",\"B. Hiểu\",\"C. Hiểu một phần\",\"D. Chưa hiểu\"]", "", 0, "Easy"),
                ("Phần nào khó nhất?", "[\"A. Lý thuyết\",\"B. Bài tập\",\"C. Đồ thị\",\"D. Không khó\"]", "", 0, "Easy"),
            };
            qOrder = 0;
            foreach (var (content, opts, ans, pts, diff) in q3Questions)
            {
                db.Questions.Add(new Question
                {
                    QuizId = quiz3.Id, Content = content, QuestionType = "MultipleChoice",
                    OptionsJson = opts, CorrectAnswer = ans, Points = pts,
                    Difficulty = diff, SortOrder = qOrder++
                });
            }
            db.SaveChanges();

            // ═══ SEED QUIZ RESULTS (per-question, with proper AnswersJson) ═══
            var rnd = new Random(99);
            var allStudents = db.Students.ToList();
            var quiz1Questions = db.Questions.Where(q => q.QuizId == quiz1.Id).OrderBy(q => q.SortOrder).ToList();
            var quiz2Questions = db.Questions.Where(q => q.QuizId == quiz2.Id).OrderBy(q => q.SortOrder).ToList();

            // Student answers for Quiz 1: B, A, A, B, B (all correct)
            // Simulate: gets Q1=correct, Q2=correct, Q3=wrong(C), Q4=correct, Q5=correct
            string[] studentAnswersQ1 = { "B", "A", "C", "B", "B" };
            for (int qi = 0; qi < quiz1Questions.Count; qi++)
            {
                var q = quiz1Questions[qi];
                string ans = qi < studentAnswersQ1.Length ? studentAnswersQ1[qi] : "A";
                bool correct = q.CorrectAnswer.Equals(ans, StringComparison.OrdinalIgnoreCase);
                var detail = new { QuestionId = q.Id, Answer = ans, Correct = correct, Points = correct ? q.Points : 0, TimeTaken = rnd.Next(5, 25) };
                db.QuizResults.Add(new QuizResult
                {
                    QuizId = quiz1.Id,
                    StudentId = allStudents.Count > 0 ? allStudents[0].Id : 0,
                    Score = correct ? 100 : 0,
                    TotalPoints = q.Points,
                    CorrectCount = correct ? 1 : 0,
                    TotalQuestions = 1,
                    AnswersJson = System.Text.Json.JsonSerializer.Serialize(detail),
                    TimeSpentSeconds = detail.TimeTaken,
                    SubmittedAt = now.AddMinutes(-rnd.Next(5, 30))
                });
            }

            // Student answers for Quiz 2: A, A, B, C, B
            // Simulate: Q1=correct, Q2=correct, Q3=correct, Q4=wrong(C), Q5=correct
            string[] studentAnswersQ2 = { "A", "A", "B", "C", "B" };
            for (int qi = 0; qi < quiz2Questions.Count; qi++)
            {
                var q = quiz2Questions[qi];
                string ans = qi < studentAnswersQ2.Length ? studentAnswersQ2[qi] : "A";
                bool correct = q.CorrectAnswer.Equals(ans, StringComparison.OrdinalIgnoreCase);
                var detail = new { QuestionId = q.Id, Answer = ans, Correct = correct, Points = correct ? q.Points : 0, TimeTaken = rnd.Next(10, 40) };
                db.QuizResults.Add(new QuizResult
                {
                    QuizId = quiz2.Id,
                    StudentId = allStudents.Count > 0 ? allStudents[0].Id : 0,
                    Score = correct ? 100 : 0,
                    TotalPoints = q.Points,
                    CorrectCount = correct ? 1 : 0,
                    TotalQuestions = 1,
                    AnswersJson = System.Text.Json.JsonSerializer.Serialize(detail),
                    TimeSpentSeconds = detail.TimeTaken,
                    SubmittedAt = now.AddDays(-3).AddMinutes(-rnd.Next(5, 30))
                });
            }
            db.SaveChanges();

            // ═══ SEED FILE TRANSFER RECORDS ═══
            var fileNames = new[] { "BaiTap_Toan10_C3.pdf", "DeThi_GiuaKy.docx", "BangBienThien.jpg" };
            foreach (var fn in fileNames)
            {
                if (allStudents.Count > 0)
                {
                    db.FileTransfers.Add(new FileTransferRecord
                    {
                        FileName = fn,
                        FileSizeBytes = rnd.Next(50000, 5000000),
                        Direction = "TeacherToStudent",
                        StudentId = allStudents[0].Id,
                        Status = "Completed",
                        ProgressPercent = 100,
                        CreatedAt = now.AddDays(-rnd.Next(1, 10))
                    });
                }
            }
            // HS nộp bài
            if (allStudents.Count > 0)
            {
                db.FileTransfers.Add(new FileTransferRecord
                {
                    FileName = "BaiLam_NguyenVanAn.pdf",
                    FileSizeBytes = rnd.Next(100000, 3000000),
                    Direction = "StudentToTeacher",
                    StudentId = allStudents[0].Id,
                    Status = "Completed",
                    ProgressPercent = 100,
                    CreatedAt = now.AddDays(-1)
                });
            }
            db.SaveChanges();

            // ═══ SEED EVENT LOGS ═══
            var logs = new[]
            {
                ("SESSION", "GV Nguyễn A", "Lớp học bắt đầu — Toán 10A (1 HS)"),
                ("LOGIN",   "Nguyễn Văn An", "HS đăng nhập từ PC-01 (192.168.1.100)"),
                ("QUIZ",    "GV Nguyễn A", "Quiz khởi tạo — C3.2 Hàm số bậc nhất — 5 câu, 1 HS tham gia"),
                ("QUIZ",    "GV Nguyễn A", "Quiz kết thúc — Điểm TB: 72/100, Cao nhất: 95"),
                ("POLL",    "GV Nguyễn A", "Poll — 'Em hiểu bài ở mức nào?' — 1 HS trả lời"),
                ("FILE",    "GV Nguyễn A", "Phát bài: BaiTap_Toan10_C3.pdf → 1/1 HS thành công"),
                ("FILE",    "GV Nguyễn A", "Thu bài: 1/1 HS đã nộp"),
                ("LOCK",    "GV Nguyễn A", "Khóa màn hình 1 máy HS"),
                ("UNLOCK",  "GV Nguyễn A", "Mở khóa tất cả máy HS"),
                ("BROADCAST","GV Nguyễn A", "Thông báo: 'Các em mở sách trang 45'"),
                ("ALERT",   "Hệ thống", "Cảnh báo: Nguyễn Văn An — Focus thấp < 30%"),
                ("SESSION", "GV Nguyễn A", "Lớp học kết thúc — Thời lượng: 45 phút"),
            };
            foreach (var (type, actor, details) in logs)
            {
                db.EventLogs.Add(new EventLog
                {
                    EventType = type, Actor = actor, Details = details,
                    Timestamp = now.AddMinutes(-rnd.Next(5, 120))
                });
            }
            db.SaveChanges();

            // ═══ SEED QUESTION BANK ═══
            SeedQuestionBank(db);

            // ═══ SEED TEACHER PROFILE ═══
            if (!db.TeacherProfiles.Any())
            {
                db.TeacherProfiles.Add(new TeacherProfile
                {
                    TeacherCode = "GiaoVien01",
                    FullName = "Nguyễn Văn A",
                    Subject = "Toán",
                    School = "THPT QA",
                    Title = "GV",
                    Role = "GV",
                    PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("PassGiaoVien01"),
                    TeacherPassword = ""
                });
                db.SaveChanges();
            }

            // ═══ SEED LESSON HISTORY ═══
            var taughtLessons = db.Lessons.Where(l => l.Status == "Taught").ToList();
            foreach (var tl in taughtLessons)
            {
                int snapCount = rnd.Next(1, 4);
                for (int s = 1; s <= snapCount; s++)
                {
                    db.LessonHistories.Add(new LessonHistory
                    {
                        LessonId       = tl.Id,
                        TitleSnapshot  = tl.Title,
                        StatusSnapshot = s == snapCount ? "Taught" : "Draft",
                        ContentsJson   = "[]",
                        ChangedBy      = "GV Nguyễn A",
                        ChangeNote     = s == snapCount
                            ? $"Hoàn thành sau tiết dạy lần {tl.UseCount}"
                            : $"Bản nháp lần {s} — {now.AddDays(-s * 3):dd/MM/yyyy}",
                        SavedAt        = now.AddDays(-s * 2).AddHours(-rnd.Next(0, 12))
                    });
                }
            }

            db.SaveChanges();
        }


        /// <summary>
        /// Seed nội dung chi tiết cho từng bài giảng — giáo viên có thể giảng dạy ngay
        /// </summary>
        private static void SeedLessonContents(AppDbContext db)
        {
            var lessons = db.Lessons.ToList();
            foreach (var lesson in lessons)
            {
                var blocks = GetContentBlocks(lesson.Title);
                int order = 0;
                foreach (var (type, data) in blocks)
                {
                    db.LessonContents.Add(new LessonContent
                    {
                        LessonId = lesson.Id,
                        ContentType = type,
                        Data = data,
                        SortOrder = order++
                    });
                }
            }
            db.SaveChanges();
        }

        private static List<(string type, string data)> GetContentBlocks(string title)
        {
            return title switch
            {
                "C1.1 — Mệnh đề toán học" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Hiểu khái niệm mệnh đề, mệnh đề chứa biến\n• Phân biệt mệnh đề đúng / sai\n• Nắm phép kéo theo, tương đương, phủ định"),
                    ("Text", "📖 I. MỆNH ĐỀ\n\nMệnh đề là câu khẳng định đúng hoặc sai.\n\n✅ VD mệnh đề: \"3 là số nguyên tố\" → ĐÚNG\n❌ Không phải mệnh đề: \"x + 3 = 5\" (chứa biến)"),
                    ("Text", "📖 II. PHÉP TOÁN\n\n🔹 Phủ định ¬P: đảo giá trị\n🔹 Kéo theo P ⇒ Q: sai khi P đúng mà Q sai\n🔹 Tương đương P ⇔ Q: P ⇒ Q và Q ⇒ P"),
                    ("Text", "✍️ BÀI TẬP\n\n1. Xét tính đúng/sai: \"π > 3\", \"√2 là số hữu tỉ\"\n2. Phủ định: \"Mọi số nguyên tố đều lẻ\""),
                },
                "C1.2 — Tập hợp và các phép toán" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Biểu diễn tập hợp, xác định tập con\n• Thực hiện phép hợp, giao, hiệu, phần bù"),
                    ("Text", "📖 CÁC PHÉP TOÁN\n\n• A ∪ B = {x | x ∈ A hoặc x ∈ B}\n• A ∩ B = {x | x ∈ A và x ∈ B}\n• A \\ B = {x | x ∈ A và x ∉ B}"),
                    ("Text", "✍️ BÀI TẬP\n\n1. A = (-2,5], B = [1,8). Tìm A ∪ B, A ∩ B\n2. Vẽ biểu đồ Venn cho 3 tập hợp"),
                },
                "C2.1 — Bất phương trình bậc nhất hai ẩn" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Biểu diễn miền nghiệm trên mặt phẳng tọa độ\n• Giải hệ BPT bậc nhất hai ẩn"),
                    ("Text", "📖 PHƯƠNG PHÁP\n\n1. Vẽ đường thẳng ax + by = c\n2. Thử điểm O(0,0) → xác định nửa MP\n3. Tô miền nghiệm"),
                    ("Text", "✍️ BÀI TẬP\n\n1. Biểu diễn miền nghiệm: 2x + y ≤ 6, x ≥ 0, y ≥ 0"),
                },
                "C2.2 — Bài toán tối ưu (Quy hoạch tuyến tính)" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Tìm GTLN, GTNN của F = ax + by trên miền đa giác\n• Áp dụng vào bài toán thực tế"),
                    ("Text", "📖 PHƯƠNG PHÁP\n\n1. Xác định miền D (đa giác)\n2. Tìm tọa độ đỉnh\n3. Tính F tại mỗi đỉnh → Max, Min"),
                    ("Text", "✍️ BÀI TẬP\n\nTìm Max F = 3x + 2y với x+y ≤ 4, x ≤ 3, y ≤ 3, x,y ≥ 0"),
                },
                "C3.1 — Hàm số và đồ thị hàm số" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Hiểu khái niệm hàm số, tập xác định, tập giá trị\n• Biết cách tìm TXĐ của hàm số"),
                    ("Text", "📖 TÌM TXĐ\n\n• Phân thức: mẫu ≠ 0\n• Căn bậc chẵn: biểu thức ≥ 0\n\nVD: y = √(x-2)/(x+1) → D = [2,+∞)"),
                    ("Text", "✍️ BÀI TẬP\n\n1. Tìm TXĐ: y = √(3-x), y = 1/(x²-4)\n2. Vẽ đồ thị y = |x - 1|"),
                },
                "C3.2 — Hàm số bậc nhất y = ax + b" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Tính chất đồng biến/nghịch biến\n• Vẽ đồ thị, xét vị trí tương đối 2 đường thẳng"),
                    ("Text", "📖 TÍNH CHẤT\n\ny = ax + b (a ≠ 0)\n• a > 0: đồng biến\n• a < 0: nghịch biến\n\nVỊ TRÍ TƯƠNG ĐỐI:\n• Song song: a₁ = a₂, b₁ ≠ b₂\n• Cắt nhau: a₁ ≠ a₂\n• Vuông góc: a₁·a₂ = -1"),
                    ("Text", "✍️ BÀI TẬP\n\n1. Vẽ đồ thị y = 2x+1, y = -x+4\n2. Tìm hàm số qua A(1,3) và B(2,5)"),
                },
                "C3.3 — Hàm số bậc hai y = ax² + bx + c" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Vẽ đồ thị parabol\n• Xác định đỉnh, trục đối xứng\n• Lập bảng biến thiên"),
                    ("Text", "📖 ĐỒ THỊ PARABOL\n\ny = ax² + bx + c (a ≠ 0)\n• Đỉnh I(-b/2a, -Δ/4a)\n• a > 0: mở lên, a < 0: mở xuống\n\nVD: y = x²-4x+3 → Đỉnh I(2,-1)"),
                    ("Text", "✍️ BÀI TẬP\n\n1. Vẽ: y = x²-2x-3\n2. Tìm GTNN: y = 2x²-8x+5"),
                },
                "C4.1 — Phương trình bậc hai" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Giải PT bậc hai ax² + bx + c = 0\n• Áp dụng công thức nghiệm, định lý Viète"),
                    ("Text", "📖 CÔNG THỨC NGHIỆM\n\nΔ = b² - 4ac\n• Δ > 0: x = (-b ± √Δ)/2a\n• Δ = 0: x = -b/2a\n• Δ < 0: vô nghiệm\n\nVIÈTE: x₁+x₂ = -b/a, x₁·x₂ = c/a"),
                    ("Text", "✍️ BÀI TẬP\n\n1. Giải: x²-5x+6 = 0\n2. Tính x₁²+x₂² của 3x²-7x+2 = 0\n3. Lập PT có nghiệm 3 và -2"),
                },
                "C4.2 — Phương trình quy về bậc hai" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Giải PT trùng phương, PT vô tỉ\n• Giải PT chứa ẩn ở mẫu"),
                    ("Text", "📖 PT TRÙNG PHƯƠNG\n\nax⁴ + bx² + c = 0, đặt t = x² ≥ 0\n\nPT VÔ TỈ: √f(x) = g(x)\nĐK: f(x) ≥ 0, g(x) ≥ 0 → bình phương"),
                    ("Text", "✍️ BÀI TẬP\n\n1. x⁴-10x²+9 = 0\n2. √(3x+1) = x+1"),
                },
                "C4.3 — Hệ phương trình bậc nhất ba ẩn" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Giải hệ 3 PT 3 ẩn bằng PP Gauss\n• Áp dụng bài toán thực tế"),
                    ("Text", "📖 PP GAUSS\n\nB1: Khử x ở PT(2), PT(3)\nB2: Khử y → tìm z\nB3: Thế ngược → y, x\n\nVD: x+y+z=6, 2x-y+z=3, x+2y-z=2 → (1,2,3)"),
                    ("Text", "✍️ BÀI TẬP\n\n1. Giải: x+y+z=3, 2x-y+3z=1, x+2y-z=4"),
                },
                "C5.1 — Giá trị lượng giác của góc" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Định nghĩa sin, cos, tan, cot\n• Đường tròn lượng giác, góc đặc biệt"),
                    ("Text", "📖 ĐƯỜNG TRÒN LƯỢNG GIÁC\n\ncosα = hoành độ, sinα = tung độ\n\nGÓC ĐẶC BIỆT:\n30°: sin=1/2, cos=√3/2\n45°: sin=cos=√2/2\n60°: sin=√3/2, cos=1/2\n\nHỆ THỨC: sin²α + cos²α = 1"),
                    ("Text", "✍️ BÀI TẬP\n\n1. Tính sin, cos, tan của 150°, 315°\n2. sinα = 3/5, 0° < α < 90°. Tính cosα"),
                },
                "C5.2 — Định lý cosin và định lý sin" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Áp dụng ĐL cosin, sin giải tam giác\n• Tính diện tích tam giác"),
                    ("Text", "📖 CÔNG THỨC\n\nCOSIN: a² = b²+c²-2bc·cosA\nSIN: a/sinA = b/sinB = c/sinC = 2R\nDIỆN TÍCH: S = ½ab·sinC"),
                    ("Text", "✍️ BÀI TẬP\n\n1. △ABC: b=5, c=7, Â=60°. Tính a, S\n2. Tam giác cạnh 7,8,9. Tính S"),
                },
                "C6.1 — Khái niệm vectơ và các phép toán" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Khái niệm vectơ, vectơ bằng nhau\n• Phép cộng, trừ, nhân số"),
                    ("Text", "📖 PHÉP TOÁN\n\n• Cộng: quy tắc hình bình hành\n• k·a⃗: k>0 cùng hướng, k<0 ngược hướng\n• MA⃗+MB⃗ = 2MI⃗ (I trung điểm AB)"),
                    ("Text", "✍️ BÀI TẬP\n\n1. ABCD hình bình hành. CMR: AB⃗+AD⃗ = AC⃗\n2. G trọng tâm △ABC. CMR: GA⃗+GB⃗+GC⃗ = 0⃗"),
                },
                "C6.2 — Tích vô hướng và ứng dụng" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Tính tích vô hướng\n• Xác định góc giữa hai vectơ"),
                    ("Text", "📖 TÍCH VÔ HƯỚNG\n\na⃗·b⃗ = |a⃗|·|b⃗|·cos(a⃗,b⃗)\nTọa độ: a⃗·b⃗ = x₁x₂+y₁y₂\na⃗ ⊥ b⃗ ⟺ a⃗·b⃗ = 0"),
                    ("Text", "✍️ BÀI TẬP\n\n1. Góc giữa a⃗(1,√3) và b⃗(√3,-1)\n2. Tìm k: a⃗(2,k) ⊥ b⃗(3,-6)"),
                },
                "C7.1 — Số gần đúng và sai số" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Số gần đúng, sai số tuyệt đối/tương đối\n• Quy tắc làm tròn"),
                    ("Text", "📖 SAI SỐ\n\nΔa = |a - ā|\nδa = Δa/|ā| × 100%\n\nVD: π ≈ 3.14 → Δ ≈ 0.0016, δ ≈ 0.05%"),
                    ("Text", "✍️ BÀI TẬP\n\n1. Làm tròn 3.14159 đến phần trăm\n2. Sai số khi dùng √2 ≈ 1.41"),
                },
                "C7.2 — Bảng tần số và biểu đồ" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Lập bảng tần số, tần suất\n• Tính x̄, Me, Mo, S"),
                    ("Text", "📖 SỐ ĐẶC TRƯNG\n\n• x̄ = Σ(xᵢ·nᵢ)/N\n• Me: giá trị giữa\n• Mo: giá trị có tần số max\n• S² = Σnᵢ(xᵢ-x̄)²/N"),
                    ("Text", "✍️ BÀI TẬP\n\nĐiểm: 3,4,5,5,6,6,6,7,7,8\nLập bảng tần số, vẽ biểu đồ, tính x̄, Me, Mo"),
                },
                "C8.1 — Phép thử và biến cố" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Không gian mẫu Ω\n• Phân loại biến cố\n• Quy tắc đếm"),
                    ("Text", "📖 QUY TẮC ĐẾM\n\n• Cộng: m + n cách\n• Nhân: m × n cách\n• Hoán vị: n!\n• Chỉnh hợp: Aⁿₖ = n!/(n-k)!\n• Tổ hợp: Cⁿₖ = n!/[k!(n-k)!]"),
                    ("Text", "✍️ BÀI TẬP\n\n1. Gieo 2 xúc xắc. Tìm |Ω|\n2. Biến cố \"tổng = 7\". Liệt kê\n3. Chọn 3 từ 10 HS. Bao nhiêu cách?"),
                },
                "C8.2 — Xác suất của biến cố" => new()
                {
                    ("Text", "📌 MỤC TIÊU\n• Định nghĩa cổ điển P(A) = |A|/|Ω|\n• Quy tắc cộng, nhân xác suất"),
                    ("Text", "📖 CÔNG THỨC\n\nP(A) = |A|/|Ω|\nP(Ā) = 1 - P(A)\nCộng: P(A∪B) = P(A)+P(B)-P(A∩B)\nĐộc lập: P(A∩B) = P(A)·P(B)"),
                    ("Text", "✍️ BÀI TẬP\n\n1. Gieo 2 xúc xắc. P(tổng=7)?\n2. Hộp 6 trắng 4 đỏ. Lấy 3. P(≥1 đỏ)?"),
                },
                _ => new()
                {
                    ("Text", $"📌 MỤC TIÊU BÀI HỌC\n• Nắm vững kiến thức Toán 10\n• Áp dụng vào bài tập thực hành"),
                    ("Text", $"📖 NỘI DUNG: {title}\n\nGV có thể chỉnh sửa và bổ sung nội dung."),
                    ("Text", "✍️ BÀI TẬP\n\n1. Câu hỏi ôn tập\n2. Bài tập áp dụng"),
                }
            };
        }

        /// <summary>Seed ngân hàng câu hỏi Toán 10</summary>
        private static void SeedQuestionBank(AppDbContext db)
        {
            var cat1 = new QuestionBankCategory
            {
                Name = "Chương 3 — Hàm số bậc nhất",
                Subject = "Toán", Grade = "Lớp 10",
                Description = "Câu hỏi về hàm số y = ax + b"
            };
            var cat2 = new QuestionBankCategory
            {
                Name = "Chương 4 — Phương trình bậc hai",
                Subject = "Toán", Grade = "Lớp 10",
                Description = "Câu hỏi về PT ax² + bx + c = 0"
            };
            var cat3 = new QuestionBankCategory
            {
                Name = "Chương 8 — Xác suất",
                Subject = "Toán", Grade = "Lớp 10",
                Description = "Câu hỏi về xác suất biến cố"
            };
            db.QuestionBankCategories.AddRange(cat1, cat2, cat3);
            db.SaveChanges();

            // ── Cat 1: Hàm số bậc nhất ──
            var bankItems1 = new[]
            {
                ("MCQ", "Đồ thị hàm số y = -2x + 3 cắt trục Oy tại điểm nào?",
                 "[\"A. (0,3)\",\"B. (3,0)\",\"C. (0,-3)\",\"D. (-3,0)\"]", "A", "Xét x=0 → y=3", 10, "Easy"),
                ("MCQ", "Hàm số nào sau đây nghịch biến?",
                 "[\"A. y=3x+1\",\"B. y=-x+2\",\"C. y=x-5\",\"D. y=2x\"]", "B", "a < 0 → nghịch biến", 10, "Easy"),
                ("MCQ", "Hai đường thẳng y = mx + 1 và y = 2x + 3 song song khi m =",
                 "[\"A. 1\",\"B. 2\",\"C. 3\",\"D. -2\"]", "B", "Song song: m = 2, b₁ ≠ b₂", 15, "Medium"),
            };
            foreach (var (qt, c, o, a, ex, p, d) in bankItems1)
                db.QuestionBankItems.Add(new QuestionBankItem
                {
                    CategoryId = cat1.Id, QuestionType = qt, Content = c,
                    OptionsJson = o, CorrectAnswer = a, Explanation = ex,
                    Points = p, Difficulty = d, Subject = "Toán", Grade = "Lớp 10"
                });

            // ── Cat 2: PT bậc hai ──
            var bankItems2 = new[]
            {
                ("MCQ", "PT x² - 7x + 12 = 0 có nghiệm:",
                 "[\"A. x=3,x=4\",\"B. x=-3,x=-4\",\"C. x=2,x=6\",\"D. x=1,x=12\"]", "A", "Δ=1, x=(7±1)/2", 10, "Easy"),
                ("MCQ", "Tổng 2 nghiệm của PT 5x² + 3x - 2 = 0 là:",
                 "[\"A. -3/5\",\"B. 3/5\",\"C. -2/5\",\"D. 2/5\"]", "A", "Viète: S = -b/a = -3/5", 15, "Medium"),
            };
            foreach (var (qt, c, o, a, ex, p, d) in bankItems2)
                db.QuestionBankItems.Add(new QuestionBankItem
                {
                    CategoryId = cat2.Id, QuestionType = qt, Content = c,
                    OptionsJson = o, CorrectAnswer = a, Explanation = ex,
                    Points = p, Difficulty = d, Subject = "Toán", Grade = "Lớp 10"
                });

            // ── Cat 3: Xác suất ──
            var bankItems3 = new[]
            {
                ("MCQ", "Gieo 1 xúc xắc. P(mặt chẵn) =",
                 "[\"A. 1/2\",\"B. 1/3\",\"C. 1/6\",\"D. 2/3\"]", "A", "Mặt chẵn: {2,4,6} → 3/6 = 1/2", 10, "Easy"),
                ("MCQ", "Chọn ngẫu nhiên 2 HS từ 5 HS. Số cách chọn:",
                 "[\"A. 10\",\"B. 20\",\"C. 25\",\"D. 5\"]", "A", "C(5,2) = 10", 10, "Easy"),
            };
            foreach (var (qt, c, o, a, ex, p, d) in bankItems3)
                db.QuestionBankItems.Add(new QuestionBankItem
                {
                    CategoryId = cat3.Id, QuestionType = qt, Content = c,
                    OptionsJson = o, CorrectAnswer = a, Explanation = ex,
                    Points = p, Difficulty = d, Subject = "Toán", Grade = "Lớp 10"
                });

            db.SaveChanges();
        }
    }
}
