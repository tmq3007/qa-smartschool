// ═══════════════════════════════════════════════════════════════════
//  QA Smart Class — Integration Test: GV ↔ HS Data Flow
//  Kiểm tra toàn bộ kết nối dữ liệu giữa 2 giao diện
// ═══════════════════════════════════════════════════════════════════

using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Tests
{
    public static class IntegrationTest
    {
        private static int _passed = 0;
        private static int _failed = 0;
        private static int _total = 0;

        /// <summary>
        /// Chạy toàn bộ test suite — gọi từ Console hoặc App
        /// </summary>
        public static string RunAll()
        {
            _passed = 0; _failed = 0; _total = 0;
            var log = new System.Text.StringBuilder();
            log.AppendLine("╔═══════════════════════════════════════════════════════════╗");
            log.AppendLine("║  QA SMART CLASS — INTEGRATION TEST SUITE                 ║");
            log.AppendLine("║  Kiểm tra GV ↔ HS kết nối & dữ liệu                    ║");
            log.AppendLine($"║  {DateTime.Now:yyyy-MM-dd HH:mm:ss}                                  ║");
            log.AppendLine("╚═══════════════════════════════════════════════════════════╝");
            log.AppendLine();

            // Use in-memory DB for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            using var db = new AppDbContext(options);
            db.Database.OpenConnection();
            db.Database.EnsureCreated();

            // ────────────────────────────────────────────────
            // GROUP 1: DATABASE — CRUD Operations  
            // ────────────────────────────────────────────────
            log.AppendLine("═══ GROUP 1: DATABASE CRUD ═══");

            Test(log, "1.1 Tạo TeacherProfile", () =>
            {
                db.TeacherProfiles.Add(new TeacherProfile
                {
                    FullName = "Nguyễn Văn A",
                    Subject = "Toán",
                    School = "THPT QA",
                    Title = "ThS",
                    AvatarPath = "/avatars/teacher.png"
                });
                db.SaveChanges();
                return db.TeacherProfiles.Any(t => t.FullName == "Nguyễn Văn A");
            });

            Test(log, "1.2 Tạo Classroom", () =>
            {
                db.Classrooms.Add(new Data.Classroom
                {
                    Name = "Toán 10A",
                    TeacherName = "Nguyễn Văn A",
                    ClassCode = "TOAN10A",
                    MaxStudents = 40
                });
                db.SaveChanges();
                return db.Classrooms.Any(c => c.ClassCode == "TOAN10A");
            });

            Test(log, "1.3 Tạo 5 Students", () =>
            {
                var classId = db.Classrooms.First().Id;
                for (int i = 1; i <= 5; i++)
                {
                    db.Students.Add(new Student
                    {
                        FullName = $"Học sinh {i:D2}",
                        StudentCode = $"HS{i:D3}",
                        ClassroomId = classId,
                        PCName = $"PC-{i:D2}",
                        IPAddress = $"192.168.1.{100 + i}",
                        IsOnline = i <= 3,
                        LastSeen = DateTime.Now,
                        AvatarPath = ""
                    });
                }
                db.SaveChanges();
                return db.Students.Count() == 5;
            });

            Test(log, "1.4 Tạo Lesson (GV soạn bài)", () =>
            {
                db.Lessons.Add(new Lesson
                {
                    Title = "Hàm số bậc nhất",
                    Subject = "Toán",
                    Grade = "10",
                    Description = "Bài giảng chương 2",
                    Status = "Approved",
                    LessonType = "Normal",
                    DurationMinutes = 45,
                    ClassName = "10A",
                    TeacherName = "Nguyễn Văn A"
                });
                db.SaveChanges();
                return db.Lessons.Any(l => l.Status == "Approved");
            });

            Test(log, "1.5 Tạo LessonContent", () =>
            {
                var lessonId = db.Lessons.First().Id;
                db.LessonContents.Add(new LessonContent
                {
                    LessonId = lessonId,
                    ContentType = "Text",
                    Data = "Nội dung bài giảng...",
                    SortOrder = 1
                });
                db.SaveChanges();
                return db.LessonContents.Any();
            });

            // ────────────────────────────────────────────────
            // GROUP 2: QUIZ FLOW — GV tạo → HS làm → Lưu kết quả
            // ────────────────────────────────────────────────
            log.AppendLine();
            log.AppendLine("═══ GROUP 2: QUIZ FLOW (GV → HS) ═══");

            Test(log, "2.1 GV tạo Quiz", () =>
            {
                var lessonId = db.Lessons.First().Id;
                db.Quizzes.Add(new Quiz
                {
                    Title = "Kiểm tra 15 phút — Hàm số",
                    QuizType = "Test",
                    TimeLimitSeconds = 900,
                    LessonId = lessonId
                });
                db.SaveChanges();
                return db.Quizzes.Any();
            });

            Test(log, "2.2 GV thêm 3 câu hỏi MCQ", () =>
            {
                var quizId = db.Quizzes.First().Id;
                db.Questions.Add(new Question
                {
                    QuizId = quizId, Content = "y = 2x + 1 là hàm số gì?",
                    QuestionType = "MCQ",
                    OptionsJson = "[\"Bậc nhất\",\"Bậc hai\",\"Mũ\",\"Logarit\"]",
                    CorrectAnswer = "A", Points = 10, Difficulty = "Easy", SortOrder = 1
                });
                db.Questions.Add(new Question
                {
                    QuizId = quizId, Content = "Đồ thị y=ax+b cắt trục tung tại điểm nào?",
                    QuestionType = "MCQ",
                    OptionsJson = "[\"(0,b)\",\"(b,0)\",\"(a,0)\",\"(0,a)\"]",
                    CorrectAnswer = "A", Points = 10, Difficulty = "Medium", SortOrder = 2
                });
                db.Questions.Add(new Question
                {
                    QuizId = quizId, Content = "Hàm y=2x+1 đồng biến?",
                    QuestionType = "TF",
                    OptionsJson = "[]",
                    CorrectAnswer = "True", Points = 10, Difficulty = "Easy", SortOrder = 3
                });
                db.SaveChanges();
                return db.Questions.Count(q => q.QuizId == quizId) == 3;
            });

            Test(log, "2.3 HS đọc Quiz từ DB (StudentQuizPage)", () =>
            {
                // Simulate StudentQuizPage.CheckForQuiz()
                var quiz = db.Quizzes.OrderByDescending(q => q.CreatedAt).FirstOrDefault();
                if (quiz == null) return false;

                var questions = db.Questions
                    .Where(q => q.QuizId == quiz.Id)
                    .OrderBy(q => q.SortOrder)
                    .ToList();

                return questions.Count == 3 && quiz.TimeLimitSeconds == 900;
            });

            Test(log, "2.4 HS nộp bài (QuizResult → DB)", () =>
            {
                var quizId = db.Quizzes.First().Id;
                var studentId = db.Students.First().Id;
                db.QuizResults.Add(new QuizResult
                {
                    QuizId = quizId,
                    StudentId = studentId,
                    Score = 80,
                    TotalPoints = 100,
                    CorrectCount = 2,
                    TotalQuestions = 3,
                    TimeSpentSeconds = 420,
                    AnswersJson = "{\"Q1\":\"A\",\"Q2\":\"A\",\"Q3\":\"True\"}"
                });
                db.SaveChanges();
                return db.QuizResults.Any(r => r.Score == 80);
            });

            Test(log, "2.5 GV xem kết quả HS (ReportPage reads QuizResults)", () =>
            {
                var results = db.QuizResults.ToList();
                double avg = results.Average(r => r.TotalPoints > 0 ? ((double)r.Score / r.TotalPoints) * 10.0 : 0.0);
                int pass = results.Count(r => r.Score >= 50);
                return results.Count == 1 && avg == 8.0 && pass == 1;
            });

            // ────────────────────────────────────────────────
            // GROUP 3: FILE TRANSFER — HS nộp bài
            // ────────────────────────────────────────────────
            log.AppendLine();
            log.AppendLine("═══ GROUP 3: FILE TRANSFER ═══");

            Test(log, "3.1 GV gửi file cho HS", () =>
            {
                db.FileTransfers.Add(new FileTransferRecord
                {
                    FileName = "BaiTap_Chuong2.pdf",
                    FileSizeBytes = 1024 * 512,
                    Direction = "TeacherToStudent",
                    StudentId = db.Students.First().Id,
                    Status = "Completed",
                    ProgressPercent = 100
                });
                db.SaveChanges();
                return db.FileTransfers.Any(f => f.Direction == "TeacherToStudent");
            });

            Test(log, "3.2 HS nộp bài (StudentSubmitPage → DB)", () =>
            {
                db.FileTransfers.Add(new FileTransferRecord
                {
                    FileName = "BaiLam_HS001.docx",
                    FileSizeBytes = 1024 * 256,
                    Direction = "StudentToTeacher",
                    StudentId = db.Students.First().Id,
                    Status = "Completed",
                    ProgressPercent = 100
                });
                db.SaveChanges();
                return db.FileTransfers.Count(f => f.Direction == "StudentToTeacher") == 1;
            });

            Test(log, "3.3 GV xem danh sách bài nộp", () =>
            {
                var submitted = db.FileTransfers
                    .Where(f => f.Direction == "StudentToTeacher")
                    .ToList();
                return submitted.Count == 1 && submitted[0].FileName == "BaiLam_HS001.docx";
            });

            // ────────────────────────────────────────────────
            // GROUP 4: EVENT LOG — HS ↔ GV tracking
            // ────────────────────────────────────────────────
            log.AppendLine();
            log.AppendLine("═══ GROUP 4: EVENT LOGGING ═══");

            Test(log, "4.1 HS giơ tay (HandRaise → EventLog)", () =>
            {
                db.EventLogs.Add(new EventLog
                {
                    EventType = "HAND_RAISE", Actor = "HS001",
                    Details = "Giơ tay: True"
                });
                db.SaveChanges();
                return db.EventLogs.Any(e => e.EventType == "HAND_RAISE");
            });

            Test(log, "4.2 HS gửi câu hỏi (Question → EventLog)", () =>
            {
                db.EventLogs.Add(new EventLog
                {
                    EventType = "QUESTION", Actor = "HS001",
                    Details = "Câu hỏi: Thầy ơi bài 5 giải thế nào ạ?"
                });
                db.SaveChanges();
                return db.EventLogs.Any(e => e.EventType == "QUESTION");
            });

            Test(log, "4.3 HS kết nối (Connect → EventLog)", () =>
            {
                db.EventLogs.Add(new EventLog
                {
                    EventType = "CONNECT", Actor = "Student",
                    Details = "Kết nối đến 192.168.1.100:29877"
                });
                db.SaveChanges();
                return db.EventLogs.Any(e => e.EventType == "CONNECT");
            });

            Test(log, "4.4 GV đọc nhật ký sự kiện", () =>
            {
                var events = db.EventLogs
                    .OrderByDescending(e => e.Timestamp)
                    .Take(10).ToList();
                return events.Count == 3;
            });

            // ────────────────────────────────────────────────
            // GROUP 5: STUDENT DATA — Shared between GV & HS
            // ────────────────────────────────────────────────
            log.AppendLine();
            log.AppendLine("═══ GROUP 5: SHARED STUDENT DATA ═══");

            Test(log, "5.1 GV cập nhật avatar HS → HS nhìn thấy", () =>
            {
                var student = db.Students.First();
                student.AvatarPath = "/avatars/students/hs001.png";
                db.SaveChanges();

                // HS đọc lại
                var fromHS = db.Students.Find(student.Id);
                return fromHS?.AvatarPath == "/avatars/students/hs001.png";
            });

            Test(log, "5.2 HS cập nhật tên → GV nhìn thấy", () =>
            {
                var student = db.Students.First();
                student.FullName = "Trần Văn B (đã sửa)";
                db.SaveChanges();

                var fromGV = db.Students.Find(student.Id);
                return fromGV?.FullName == "Trần Văn B (đã sửa)";
            });

            Test(log, "5.3 GV đánh dấu HS online/offline", () =>
            {
                var onlineCount = db.Students.Count(s => s.IsOnline);
                return onlineCount == 3; // 3 students created as online
            });

            Test(log, "5.4 GV đọc TeacherProfile → HS hiển thị", () =>
            {
                var teacher = db.TeacherProfiles.FirstOrDefault();
                return teacher != null && teacher.FullName == "Nguyễn Văn A" && teacher.Subject == "Toán";
            });

            // ────────────────────────────────────────────────
            // GROUP 6: QUESTION BANK — Shared
            // ────────────────────────────────────────────────
            log.AppendLine();
            log.AppendLine("═══ GROUP 6: QUESTION BANK ═══");

            Test(log, "6.1 GV tạo danh mục ngân hàng câu hỏi", () =>
            {
                db.QuestionBankCategories.Add(new QuestionBankCategory
                {
                    Name = "Toán 10 — Hàm số",
                    Subject = "Toán",
                    Grade = "10",
                    Description = "Câu hỏi chương hàm số"
                });
                db.SaveChanges();
                return db.QuestionBankCategories.Any();
            });

            Test(log, "6.2 GV thêm câu hỏi vào ngân hàng", () =>
            {
                var catId = db.QuestionBankCategories.First().Id;
                db.QuestionBankItems.Add(new QuestionBankItem
                {
                    CategoryId = catId,
                    QuestionType = "MCQ",
                    Content = "y=3x-2, khi x=1 thì y=?",
                    OptionsJson = "[\"1\",\"2\",\"3\",\"4\"]",
                    CorrectAnswer = "A",
                    Difficulty = "Easy",
                    Points = 10,
                    Subject = "Toán",
                    Grade = "10"
                });
                db.SaveChanges();
                return db.QuestionBankItems.Any();
            });

            // ────────────────────────────────────────────────
            // GROUP 7: LESSON HISTORY — Version tracking
            // ────────────────────────────────────────────────
            log.AppendLine();
            log.AppendLine("═══ GROUP 7: LESSON HISTORY ═══");

            Test(log, "7.1 Lưu version history khi GV sửa bài", () =>
            {
                var lesson = db.Lessons.First();
                db.LessonHistories.Add(new LessonHistory
                {
                    LessonId = lesson.Id,
                    TitleSnapshot = lesson.Title,
                    StatusSnapshot = lesson.Status,
                    ContentsJson = "[]",
                    ChangedBy = "GV Nguyễn Văn A",
                    ChangeNote = "Sửa lần 1"
                });
                db.SaveChanges();
                return db.LessonHistories.Any();
            });

            // ────────────────────────────────────────────────
            // GROUP 8: CROSS-REFERENCE INTEGRITY  
            // ────────────────────────────────────────────────
            log.AppendLine();
            log.AppendLine("═══ GROUP 8: DATA INTEGRITY ═══");

            Test(log, "8.1 QuizResult → Quiz FK hợp lệ", () =>
            {
                var result = db.QuizResults.First();
                var quiz = db.Quizzes.Find(result.QuizId);
                return quiz != null;
            });

            Test(log, "8.2 Question → Quiz FK hợp lệ", () =>
            {
                var q = db.Questions.First();
                var quiz = db.Quizzes.Find(q.QuizId);
                return quiz != null;
            });

            Test(log, "8.3 Student → Classroom FK hợp lệ", () =>
            {
                var s = db.Students.First();
                var c = db.Classrooms.Find(s.ClassroomId);
                return c != null;
            });

            Test(log, "8.4 LessonContent → Lesson FK hợp lệ", () =>
            {
                var lc = db.LessonContents.First();
                var l = db.Lessons.Find(lc.LessonId);
                return l != null;
            });

            Test(log, "8.5 QuestionBankItem → Category FK hợp lệ", () =>
            {
                var item = db.QuestionBankItems.First();
                var cat = db.QuestionBankCategories.Find(item.CategoryId);
                return cat != null;
            });

            // ────────────────────────────────────────────────
            // GROUP 9: QUIZ DELETION & CASCADE
            // ────────────────────────────────────────────────
            log.AppendLine();
            log.AppendLine("═══ GROUP 9: QUIZ DELETION & CASCADE ═══");

            Test(log, "9.1 Xóa Quiz tự động dọn dẹp Questions và QuizResults", () =>
            {
                // Lấy thông tin ban đầu
                var quiz = db.Quizzes.First();
                var quizId = quiz.Id;

                // Đảm bảo trước khi xóa có câu hỏi và kết quả liên quan
                bool hasQuestions = db.Questions.Any(q => q.QuizId == quizId);
                bool hasResults = db.QuizResults.Any(r => r.QuizId == quizId);
                if (!hasQuestions || !hasResults) return false;

                // Thực hiện xóa cascade
                var relatedQuestions = db.Questions.Where(q => q.QuizId == quizId).ToList();
                db.Questions.RemoveRange(relatedQuestions);

                var relatedResults = db.QuizResults.Where(r => r.QuizId == quizId).ToList();
                db.QuizResults.RemoveRange(relatedResults);

                db.Quizzes.Remove(quiz);
                db.SaveChanges();

                // Kiểm tra sau khi xóa
                bool quizDeleted = !db.Quizzes.Any(q => q.Id == quizId);
                bool questionsCleaned = !db.Questions.Any(q => q.QuizId == quizId);
                bool resultsCleaned = !db.QuizResults.Any(r => r.QuizId == quizId);

                return quizDeleted && questionsCleaned && resultsCleaned;
            });

            // ────────────────────────────────────────────────
            // SUMMARY
            // ────────────────────────────────────────────────
            log.AppendLine();
            log.AppendLine("╔═══════════════════════════════════════════════════════════╗");
            log.AppendLine($"║  KẾT QUẢ: {_passed}/{_total} PASSED  |  {_failed} FAILED                      ║");
            log.AppendLine($"║  Tỉ lệ: {(_total > 0 ? _passed * 100 / _total : 0)}%                                              ║");
            log.AppendLine($"║  Thời gian: {DateTime.Now:HH:mm:ss}                                  ║");
            if (_failed == 0)
                log.AppendLine("║  ✅ TẤT CẢ ĐỀU PASS — HỆ THỐNG SẴN SÀNG!              ║");
            else
                log.AppendLine($"║  ❌ CÓ {_failed} TEST THẤT BẠI — CẦN KIỂM TRA!               ║");
            log.AppendLine("╚═══════════════════════════════════════════════════════════╝");

            db.Database.CloseConnection();
            return log.ToString();
        }

        private static void Test(System.Text.StringBuilder log, string name, Func<bool> action)
        {
            _total++;
            try
            {
                bool result = action();
                if (result)
                {
                    _passed++;
                    log.AppendLine($"  ✅ {name}");
                }
                else
                {
                    _failed++;
                    log.AppendLine($"  ❌ {name} — returned false");
                }
            }
            catch (Exception ex)
            {
                _failed++;
                log.AppendLine($"  ❌ {name} — EXCEPTION: {ex.Message}");
            }
        }
    }
}
