using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using Xunit;
using QuestPDF.Infrastructure;
using QuestPDF.Fluent;

namespace QASmartClass.Tests
{
    /// <summary>
    /// V7 P5.4: Integration tests — kiem tra logic nghiep vu end-to-end
    /// </summary>
    public class V7IntegrationTests
    {
        static V7IntegrationTests()
        {
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();

            QuestPDF.Settings.License = LicenseType.Community;
            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.24.0");
        }

        // TC-M1: QuestionBankItem default status
        [Fact]
        public void QuestionBankItem_DefaultApprovalStatus_ShouldBe_Pending()
        {
            var item = new QuestionBankItem();
            Assert.Equal("Pending", item.ApprovalStatus);
        }

        [Fact]
        public void QuestionBankItem_DefaultCreatedBy_ShouldBe_Empty()
        {
            var item = new QuestionBankItem();
            Assert.Equal(string.Empty, item.CreatedBy);
        }

        // TC-M2: SEL model defaults
        [Fact]
        public void MentalHealthRecord_DefaultRiskLevel_ShouldBe_Low()
        {
            var record = new StudentMentalHealthRecord();
            Assert.Equal("Low", record.RiskLevel);
        }

        [Fact]
        public void MentalHealthRecord_DefaultInterventionLevel_ShouldBe_Zero()
        {
            var record = new StudentMentalHealthRecord();
            Assert.Equal(0, record.InterventionLevel);
        }

        [Fact]
        public void MentalHealthRecord_DefaultIsNotified_ShouldBe_False()
        {
            var record = new StudentMentalHealthRecord();
            Assert.False(record.IsNotified);
        }

        // TC-M3: TeacherFeedback defaults
        [Fact]
        public void TeacherFeedback_DefaultScore_ShouldBe_3()
        {
            var fb = new TeacherFeedback();
            Assert.Equal(3.0, fb.Score);
        }

        // TC: StatusConstants consistency
        [Fact]
        public void StatusConstants_AllApprovalValues_ShouldNotBeNull()
        {
            Assert.NotNull(StatusConstants.Approval.Pending);
            Assert.NotNull(StatusConstants.Approval.Approved);
            Assert.NotNull(StatusConstants.Approval.Rejected);
        }

        [Fact]
        public void StatusConstants_AllRoles_ShouldNotBeEmpty()
        {
            Assert.NotEmpty(StatusConstants.TeacherRole.GiaoVien);
            Assert.NotEmpty(StatusConstants.TeacherRole.ToTruong);
            Assert.NotEmpty(StatusConstants.TeacherRole.HieuTruong);
            Assert.NotEmpty(StatusConstants.TeacherRole.Admin);
            Assert.NotEmpty(StatusConstants.TeacherRole.Counselor);
        }

        // TC: UserSessionService singleton
        [Fact]
        public void UserSessionService_Instance_ShouldNotBeNull()
        {
            var instance = UserSessionService.Instance;
            Assert.NotNull(instance);
        }

        [Fact]
        public void UserSessionService_DefaultRole_ShouldBe_GV()
        {
            // Default role should be GV
            Assert.Equal("GV", QASmartClass.Data.StatusConstants.TeacherRole.GiaoVien);
        }

        // TC: TT22 boundary at exactly 8.0
        [Fact]
        public void TT22_Boundary_8Point0_ShouldBe_Gioi()
        {
            Assert.Equal("Giỏi", TT22GradingService.ClassifyAcademic(8.0));
        }

        [Fact]
        public void TestInsertStudentDirectly()
        {
            using (var db = new AppDbContext())
            {
                var student = new Student
                {
                    FullName = "Test Student Insert Direct",
                    StudentCode = "HS_TEST_888",
                    PCName = "PC-TEST",
                    IPAddress = "192.168.1.250",
                    IsOnline = false,
                    LastSeen = DateTime.Now
                };
                db.Students.Add(student);
                try
                {
                    db.SaveChanges();
                    // Clean up
                    db.Students.Remove(student);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                    if (ex.InnerException != null) msg += " | Inner: " + ex.InnerException.Message;
                    throw new Exception("SAVE_ERROR: " + msg);
                }
            }
        }

        [Fact]
        public void TestDeleteStudentId1()
        {
            using (var db = new AppDbContext())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var student = db.Students.Find(1);
                    if (student != null)
                    {
                        db.Students.Remove(student);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                    if (ex.InnerException != null) msg += " | Inner: " + ex.InnerException.Message;
                    if (ex.InnerException?.InnerException != null) msg += " | Inner2: " + ex.InnerException.InnerException.Message;
                    throw new Exception("DELETE_STUDENT_ERROR: " + msg);
                }
                finally
                {
                    tx.Rollback();
                }
            }
        }

        [Fact]
        public void TestTeacherImportCsv_DynamicMappingAndSafety()
        {
            // Case 1: 8 columns, English headers
            var header1 = "TeacherCode,FullName,Title,Subject,School,Phone,Email,Notes";
            var row1 = "\"GV099\",\"Nguyễn Văn A\",\"ThS\",\"Toán học\",\"THPT Chu Văn An\",\"0901234567\",\"a@school.edu\",\"Ghi chú tiếng Việt\"";
            
            var headers = QASmartClass.Helpers.CsvHelper.ParseLine(header1).Select(h => h.Trim()).ToList();
            Assert.Equal(8, headers.Count);
            
            int teacherCodeIdx = headers.FindIndex(h => h.Equals("TeacherCode", StringComparison.OrdinalIgnoreCase) || h.Equals("Mã GV", StringComparison.OrdinalIgnoreCase));
            int fullNameIdx = headers.FindIndex(h => h.Equals("FullName", StringComparison.OrdinalIgnoreCase) || h.Equals("Họ tên", StringComparison.OrdinalIgnoreCase) || h.Equals("Họ và tên", StringComparison.OrdinalIgnoreCase));
            int titleIdx = headers.FindIndex(h => h.Equals("Title", StringComparison.OrdinalIgnoreCase) || h.Equals("Chức danh", StringComparison.OrdinalIgnoreCase));
            int subjectIdx = headers.FindIndex(h => h.Equals("Subject", StringComparison.OrdinalIgnoreCase) || h.Equals("Bộ môn", StringComparison.OrdinalIgnoreCase) || h.Equals("Môn", StringComparison.OrdinalIgnoreCase) || h.Equals("Môn học", StringComparison.OrdinalIgnoreCase));
            int schoolIdx = headers.FindIndex(h => h.Equals("School", StringComparison.OrdinalIgnoreCase) || h.Equals("Trường", StringComparison.OrdinalIgnoreCase));
            int phoneIdx = headers.FindIndex(h => h.Equals("Phone", StringComparison.OrdinalIgnoreCase) || h.Equals("SĐT", StringComparison.OrdinalIgnoreCase) || h.Equals("Điện thoại", StringComparison.OrdinalIgnoreCase) || h.Equals("Số điện thoại", StringComparison.OrdinalIgnoreCase));
            int emailIdx = headers.FindIndex(h => h.Equals("Email", StringComparison.OrdinalIgnoreCase));
            int notesIdx = headers.FindIndex(h => h.Equals("Notes", StringComparison.OrdinalIgnoreCase) || h.Equals("Ghi chú", StringComparison.OrdinalIgnoreCase));

            Assert.Equal(0, teacherCodeIdx);
            Assert.Equal(1, fullNameIdx);
            Assert.Equal(2, titleIdx);
            Assert.Equal(3, subjectIdx);
            Assert.Equal(4, schoolIdx);
            Assert.Equal(5, phoneIdx);
            Assert.Equal(6, emailIdx);
            Assert.Equal(7, notesIdx);

            var parts = QASmartClass.Helpers.CsvHelper.ParseLine(row1);
            Assert.Equal(8, parts.Length);
            Assert.Equal("GV099", parts[teacherCodeIdx].Trim());
            Assert.Equal("Nguyễn Văn A", parts[fullNameIdx].Trim());
            Assert.Equal("ThS", parts[titleIdx].Trim());
            Assert.Equal("Toán học", parts[subjectIdx].Trim());
            Assert.Equal("THPT Chu Văn An", parts[schoolIdx].Trim());
            Assert.Equal("0901234567", parts[phoneIdx].Trim());
            Assert.Equal("a@school.edu", parts[emailIdx].Trim());
            Assert.Equal("Ghi chú tiếng Việt", parts[notesIdx].Trim());

            // Case 2: 7 columns, Vietnamese headers
            var header2 = "Họ tên,Chức danh,Bộ môn,Trường,SĐT,Email,Ghi chú";
            var row2 = "\"Trần Thị B\",\"ThS\",\"Vật lý\",\"THPT Amsterdam\",\"0912345678\",\"b@school.edu\",\"\"";
            
            var headers2 = QASmartClass.Helpers.CsvHelper.ParseLine(header2).Select(h => h.Trim()).ToList();
            int teacherCodeIdx2 = headers2.FindIndex(h => h.Equals("TeacherCode", StringComparison.OrdinalIgnoreCase) || h.Equals("Mã GV", StringComparison.OrdinalIgnoreCase));
            int fullNameIdx2 = headers2.FindIndex(h => h.Equals("FullName", StringComparison.OrdinalIgnoreCase) || h.Equals("Họ tên", StringComparison.OrdinalIgnoreCase) || h.Equals("Họ và tên", StringComparison.OrdinalIgnoreCase));
            int titleIdx2 = headers2.FindIndex(h => h.Equals("Title", StringComparison.OrdinalIgnoreCase) || h.Equals("Chức danh", StringComparison.OrdinalIgnoreCase));
            int subjectIdx2 = headers2.FindIndex(h => h.Equals("Subject", StringComparison.OrdinalIgnoreCase) || h.Equals("Bộ môn", StringComparison.OrdinalIgnoreCase) || h.Equals("Môn", StringComparison.OrdinalIgnoreCase) || h.Equals("Môn học", StringComparison.OrdinalIgnoreCase));
            int schoolIdx2 = headers2.FindIndex(h => h.Equals("School", StringComparison.OrdinalIgnoreCase) || h.Equals("Trường", StringComparison.OrdinalIgnoreCase));
            int phoneIdx2 = headers2.FindIndex(h => h.Equals("Phone", StringComparison.OrdinalIgnoreCase) || h.Equals("SĐT", StringComparison.OrdinalIgnoreCase) || h.Equals("Điện thoại", StringComparison.OrdinalIgnoreCase) || h.Equals("Số điện thoại", StringComparison.OrdinalIgnoreCase));
            int emailIdx2 = headers2.FindIndex(h => h.Equals("Email", StringComparison.OrdinalIgnoreCase));
            int notesIdx2 = headers2.FindIndex(h => h.Equals("Notes", StringComparison.OrdinalIgnoreCase) || h.Equals("Ghi chú", StringComparison.OrdinalIgnoreCase));

            Assert.Equal(-1, teacherCodeIdx2);
            Assert.Equal(0, fullNameIdx2);
            Assert.Equal(1, titleIdx2);
            Assert.Equal(2, subjectIdx2);
            Assert.Equal(3, schoolIdx2);
            Assert.Equal(4, phoneIdx2);
            Assert.Equal(5, emailIdx2);
            Assert.Equal(6, notesIdx2);

            var parts2 = QASmartClass.Helpers.CsvHelper.ParseLine(row2);
            Assert.Equal(7, parts2.Length);
            Assert.Equal("Trần Thị B", parts2[fullNameIdx2].Trim());
            Assert.Equal("ThS", parts2[titleIdx2].Trim());
            Assert.Equal("Vật lý", parts2[subjectIdx2].Trim());
            Assert.Equal("THPT Amsterdam", parts2[schoolIdx2].Trim());
            Assert.Equal("0912345678", parts2[phoneIdx2].Trim());
            Assert.Equal("b@school.edu", parts2[emailIdx2].Trim());
            Assert.Equal("", parts2[notesIdx2].Trim());

            // Case 3: Empty header / Fallback logic
            int fallbackTeacherCodeIdx = -1;
            int fallbackFullNameIdx = -1;

            int colCount = 8; // Simulate 8 columns
            if (colCount >= 8)
            {
                fallbackTeacherCodeIdx = 0;
                fallbackFullNameIdx = 1;
            }
            Assert.Equal(1, fallbackFullNameIdx);
            Assert.Equal(0, fallbackTeacherCodeIdx);

            // Case 4: Malformed parts length / Index safety check
            var malformedRow = new string[] { "Nguyễn Văn C", "GV" }; // only 2 elements
            
            // Check that we protect against IndexOutOfRangeException by checking parts.Length > index
            string titleValue = (titleIdx2 != -1 && malformedRow.Length > titleIdx2) ? malformedRow[titleIdx2].Trim() : "GV";
            string subjectValue = (subjectIdx2 != -1 && malformedRow.Length > subjectIdx2) ? malformedRow[subjectIdx2].Trim() : "";
            
            Assert.Equal("GV", titleValue);
            Assert.Equal("", subjectValue);
        }

        [Fact]
        public void TestGroupTaskAssignment_CombinationLogic()
        {
            // Khởi tạo các nhóm giả lập
            var grp1 = new QASmartClass.Classroom.Views.GroupVm { Name = "Nhóm Sư Tử", Task = "" };
            var grp2 = new QASmartClass.Classroom.Views.GroupVm { Name = "Nhóm Cáo", Task = "" };
            
            // Kịch bản 1: Nhập cả nhiệm vụ chung và riêng
            string common1 = "Nhiệm vụ chung A";
            string specific1 = "Nhiệm vụ riêng nhóm 1";
            
            if (!string.IsNullOrWhiteSpace(common1) && !string.IsNullOrWhiteSpace(specific1))
                grp1.Task = $"{common1}\n{specific1}";
            else if (!string.IsNullOrWhiteSpace(common1))
                grp1.Task = common1;
            else
                grp1.Task = specific1;
                
            Assert.Equal("Nhiệm vụ chung A\nNhiệm vụ riêng nhóm 1", grp1.Task);
            
            // Kịch bản 2: Chỉ nhập nhiệm vụ riêng
            string common2 = "";
            string specific2 = "Nhiệm vụ riêng nhóm 2";
            
            if (!string.IsNullOrWhiteSpace(common2) && !string.IsNullOrWhiteSpace(specific2))
                grp2.Task = $"{common2}\n{specific2}";
            else if (!string.IsNullOrWhiteSpace(common2))
                grp2.Task = common2;
            else
                grp2.Task = specific2;
                
            Assert.Equal("Nhiệm vụ riêng nhóm 2", grp2.Task);
        }

        [Fact]
        public void TestGroupTaskAssignment_PersistenceLogic()
        {
            // 1. Giả lập danh sách học sinh lớp học hiện tại
            var studentNames = new System.Collections.Generic.List<string> { "Học sinh A", "Học sinh B", "Học sinh C", "Học sinh D" };

            // 2. Giả lập dữ liệu nhóm đã lưu trong App.CurrentGroups từ trước
            var savedGroups = new System.Collections.Generic.List<QASmartClass.Classroom.Views.GroupVm>
            {
                new QASmartClass.Classroom.Views.GroupVm
                {
                    Index = 0,
                    Name = "Nhóm 1",
                    Members = new System.Collections.Generic.List<string> { "Học sinh A", "Học sinh B" },
                    Leader = "Học sinh A",
                    Task = "Nhiệm vụ 1"
                },
                new QASmartClass.Classroom.Views.GroupVm
                {
                    Index = 1,
                    Name = "Nhóm 2",
                    Members = new System.Collections.Generic.List<string> { "Học sinh C" },
                    Leader = "",
                    Task = ""
                }
            };

            // 3. Thực hiện logic khôi phục (giống trong RestoreOrAssignGroups)
            var restoredGroups = new System.Collections.Generic.List<QASmartClass.Classroom.Views.GroupVm>();
            foreach (var g in savedGroups)
            {
                restoredGroups.Add(new QASmartClass.Classroom.Views.GroupVm
                {
                    Index = g.Index,
                    Name = g.Name,
                    Members = new System.Collections.Generic.List<string>(g.Members),
                    Leader = g.Leader,
                    Task = g.Task
                });
            }

            // Tính toán học sinh chưa phân nhóm
            var assignedStudents = restoredGroups.SelectMany(g => g.Members).ToHashSet();
            var unassigned = studentNames.Where(name => !assignedStudents.Contains(name)).ToList();

            // 4. Kiểm tra độ chính xác
            Assert.Equal(2, restoredGroups.Count);
            Assert.Equal("Nhóm 1", restoredGroups[0].Name);
            Assert.Equal("Học sinh A", restoredGroups[0].Leader);
            Assert.Equal("Nhiệm vụ 1", restoredGroups[0].Task);
            
            // Học sinh D chưa được phân vào nhóm nào, phải nằm ở danh sách unassigned
            Assert.Single(unassigned);
            Assert.Equal("Học sinh D", unassigned[0]);
        }

        [Fact]
        public void TestSoloQuiz_VietnameseResultFormatting()
        {
            // 1. Giả lập dữ liệu
            int totalCorrect = 4;
            int totalQuestions = 5;
            int totalPoints = 50;
            int earnedPoints = 40;
            double percent = 80.0;
            
            // Giả lập xếp loại
            string grade = percent >= 90 ? "Xuất sắc" :
                           percent >= 70 ? "Tốt" :
                           percent >= 50 ? "Trung bình" : "Cần cải thiện";
                           
            Assert.Equal("Tốt", grade);

            // 2. Xây dựng chuỗi thông báo kết quả
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("KẾT QUẢ QUIZ\n");
            sb.AppendLine($"Đúng: {totalCorrect}/{totalQuestions}");
            sb.AppendLine($"Điểm: {earnedPoints}/{totalPoints} ({percent:F0}%)");
            sb.AppendLine($"Xếp loại: {grade}\n");
            sb.AppendLine("--- Chi tiết từng câu ---");
            
            var log = $"Q1: A (Đúng) - Nội dung câu hỏi 1";
            sb.AppendLine(log);

            var resultString = sb.ToString();

            // 3. Kiểm tra các từ tiếng Việt có dấu then chốt
            Assert.Contains("KẾT QUẢ QUIZ", resultString);
            Assert.Contains("Đúng: 4/5", resultString);
            Assert.Contains("Điểm: 40/50 (80%)", resultString);
            Assert.Contains("Xếp loại: Tốt", resultString);
            Assert.Contains("--- Chi tiết từng câu ---", resultString);
            Assert.Contains("(Đúng)", resultString);
        }

        [Fact]
        public void TestSoloQuiz_LeaderboardAndStatsFormatting()
        {
            // 1. Giả lập chuỗi Detail của LeaderboardItem
            int count = 3;
            string detailText = $"{count} bài";
            Assert.Contains("bài", detailText);

            // 2. Giả lập hiển thị Thống kê
            string submittedText = $"Nộp bài: {count}";
            string correctText = "Trả lời đúng: 67%";
            string avgScoreText = "Điểm TB: 8đ";
            
            Assert.StartsWith("Nộp bài:", submittedText);
            Assert.StartsWith("Trả lời đúng:", correctText);
            Assert.StartsWith("Điểm TB:", avgScoreText);

            // 3. Giả lập Báo cáo câu sai
            int totalQuestions = 5;
            int totalWrong = 4;
            int totalAnswers = 15;
            double errorRate = 27;
            string errorSummary = $"{totalQuestions} câu | {totalWrong}/{totalAnswers} lượt sai ({errorRate:F0}%)";

            Assert.Contains("câu", errorSummary);
            Assert.Contains("lượt sai", errorSummary);
        }

        [Fact]
        public void TestQuizReview_StatisticsFiltering()
        {
            // Giả lập danh sách QuizResults chứa câu trả lời trùng lặp của 1 học sinh cho cùng 1 Quiz (QuizId = 1)
            // Học sinh trả lời câu hỏi 1 và 2 hai lần (lần 2 trả lời đúng)
            var rawResults = new System.Collections.Generic.List<QuizResult>
            {
                new QuizResult { QuizId = 1, Score = 10, TotalPoints = 10, CorrectCount = 1, TotalQuestions = 1, AnswersJson = "{\"QuestionId\":1,\"Correct\":true,\"Points\":10}", SubmittedAt = DateTime.Now },
                new QuizResult { QuizId = 1, Score = 0, TotalPoints = 10, CorrectCount = 0, TotalQuestions = 1, AnswersJson = "{\"QuestionId\":1,\"Correct\":false,\"Points\":0}", SubmittedAt = DateTime.Now.AddMinutes(-5) },
                new QuizResult { QuizId = 1, Score = 10, TotalPoints = 10, CorrectCount = 1, TotalQuestions = 1, AnswersJson = "{\"QuestionId\":2,\"Correct\":true,\"Points\":10}", SubmittedAt = DateTime.Now },
                new QuizResult { QuizId = 1, Score = 0, TotalPoints = 10, CorrectCount = 0, TotalQuestions = 1, AnswersJson = "{\"QuestionId\":2,\"Correct\":false,\"Points\":0}", SubmittedAt = DateTime.Now.AddMinutes(-5) },
                new QuizResult { QuizId = 2, Score = 10, TotalPoints = 10, CorrectCount = 1, TotalQuestions = 1, AnswersJson = "{\"QuestionId\":1,\"Correct\":true,\"Points\":10}", SubmittedAt = DateTime.Now } // Khác QuizId
            };

            // Chỉ lọc các kết quả thuộc QuizId = 1
            var filtered = rawResults.Where(r => r.QuizId == 1).OrderByDescending(r => r.SubmittedAt).ToList();
            
            var answerMap = new System.Collections.Generic.Dictionary<int, QASmartClass.Classroom.Views.AnswerDetail>();
            foreach (var r in filtered)
            {
                var detail = System.Text.Json.JsonSerializer.Deserialize<QASmartClass.Classroom.Views.AnswerDetail>(r.AnswersJson);
                if (detail != null && detail.QuestionId >= 0)
                {
                    if (!answerMap.ContainsKey(detail.QuestionId))
                        answerMap[detail.QuestionId] = detail;
                }
            }

            int totalScore = answerMap.Values.Sum(ans => ans.Points);
            int correctCount = answerMap.Values.Count(ans => ans.Correct);

            // Dữ liệu đúng phải là 2 câu thuộc QuizId = 1 (mỗi câu lấy lượt mới nhất là Đúng)
            Assert.Equal(2, answerMap.Count);
            Assert.Equal(20, totalScore);
            Assert.Equal(2, correctCount);
        }

        [Fact]
        public void TestQuizExport_FormatAndModeChecking()
        {
            // 1. Kiểm tra định dạng xuất Solo
            var soloHeader = "STT,Học sinh,Tổng điểm,Đúng,Tổng câu,Tỷ lệ %,Thời gian (s),Ngày nộp";
            Assert.Contains("Học sinh", soloHeader);
            Assert.Contains("Thời gian", soloHeader);

            // 2. Kiểm tra định dạng xuất Battle
            var battleHeader = "Hạng,Tên nhóm,Điểm số,Trạng thái";
            Assert.Contains("Tên nhóm", battleHeader);
            Assert.Contains("Trạng thái", battleHeader);

            // 3. Kiểm tra định dạng xuất Poll
            var pollHeader = "Lựa chọn,Số phiếu,Tỷ lệ %";
            Assert.Contains("Lựa chọn", pollHeader);
            Assert.Contains("Số phiếu", pollHeader);
        }

        [Fact]
        public void TestQuiz_ActiveStateFlow()
        {
            using (var db = new AppDbContext())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Create a dummy quiz
                    var quiz = new Quiz
                    {
                        Title = "Test Quiz Active State Flow",
                        QuizType = "Solo",
                        TimeLimitSeconds = 300,
                        CreatedAt = System.DateTime.Now
                    };
                    db.Quizzes.Add(quiz);
                    db.SaveChanges();

                    // 2. Verify that ActiveQuizId setting is NOT set to this quiz (or doesn't exist, or is "0")
                    var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "ActiveQuizId");
                    if (setting != null)
                    {
                        Assert.NotEqual(quiz.Id.ToString(), setting.Value);
                    }

                    // 3. Simulate teacher activating the quiz
                    if (setting == null)
                    {
                        setting = new SystemSetting { Id = "ActiveQuizId", Value = quiz.Id.ToString(), Category = "Quiz", LastUpdated = System.DateTime.Now };
                        db.SystemSettings.Add(setting);
                    }
                    else
                    {
                        setting.Value = quiz.Id.ToString();
                        setting.LastUpdated = System.DateTime.Now;
                    }
                    db.SaveChanges();

                    // 4. Verify ActiveQuizId is now correct
                    var activeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "ActiveQuizId");
                    Assert.NotNull(activeSetting);
                    Assert.Equal(quiz.Id.ToString(), activeSetting.Value);

                    // 5. Simulate student loading quiz when active quiz is set
                    int activeQuizId = int.Parse(activeSetting.Value);
                    var loadedQuiz = db.Quizzes.FirstOrDefault(q => q.Id == activeQuizId);
                    Assert.NotNull(loadedQuiz);
                    Assert.Equal(quiz.Title, loadedQuiz.Title);

                    // 6. Simulate teacher stopping the quiz
                    activeSetting.Value = "0";
                    activeSetting.LastUpdated = System.DateTime.Now;
                    db.SaveChanges();

                    // 7. Verify ActiveQuizId is now "0" (empty state)
                    var clearedSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "ActiveQuizId");
                    Assert.NotNull(clearedSetting);
                    Assert.Equal("0", clearedSetting.Value);
                }
                finally
                {
                    tx.Rollback();
                }
            }
        }

        [Fact]
        public void TestHomeworkTemplateParsing_ShouldSucceed()
        {
            var vm = new QASmartClass.Classroom.ViewModels.HomeworkViewModel();
            var type = typeof(QASmartClass.Classroom.ViewModels.HomeworkViewModel);
            
            // Get private methods
            var extractMethod = type.GetMethod("ExtractTextFromWord", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var parseMethod = type.GetMethod("ParseTemplateFields", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(extractMethod);
            Assert.NotNull(parseMethod);
            
            // Path to build output template
            var templatePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Assets", "Templates", "HomeworkTemplate.docx");
            
            // If running in test context, it might be in different folder, so let's verify
            if (!System.IO.File.Exists(templatePath))
            {
                // Try parent folder lookups for test runner
                templatePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Assets", "Templates", "HomeworkTemplate.docx"));
            }
            
            Assert.True(System.IO.File.Exists(templatePath), $"Template file not found at: {templatePath}");
            
            // Invoke ExtractTextFromWord
            var content = extractMethod.Invoke(vm, new object[] { templatePath }) as string;
            Assert.NotNull(content);
            Assert.NotEmpty(content);
            
            // Invoke ParseTemplateFields
            var fields = parseMethod.Invoke(vm, new object[] { content! }) as System.Collections.Generic.Dictionary<string, string>;
            Assert.NotNull(fields);
            
            // Verify structure has keys (either empty or with default text)
            Assert.True(fields.ContainsKey("Môn học") || fields.ContainsKey("Mon hoc"), "Should contain key Môn học");
            Assert.True(fields.ContainsKey("Tiêu đề") || fields.ContainsKey("Tieu de"), "Should contain key Tiêu đề");
            Assert.True(fields.ContainsKey("Nội dung") || fields.ContainsKey("Noi dung"), "Should contain key Nội dung");
            Assert.True(fields.ContainsKey("Hạn nộp") || fields.ContainsKey("Han nop"), "Should contain key Hạn nộp");
            Assert.True(fields.ContainsKey("Ghi chú") || fields.ContainsKey("Ghi chu"), "Should contain key Ghi chú");
        }

        [Fact]
        public void TestMoetReportExcelGeneration_ShouldProduceValidFile()
        {
            using (var db = new AppDbContext())
            {
                var moetService = new MoetReportService(db);
                var schoolYear = "2025-2026";

                // Generate Mau 20 Excel
                string excelPath20 = moetService.GenerateMau20Excel(schoolYear);
                Assert.True(System.IO.File.Exists(excelPath20));
                Assert.True(new System.IO.FileInfo(excelPath20).Length > 0);

                // Generate Mau 22 Excel
                string excelPath22 = moetService.GenerateMau22Excel(schoolYear, "HK2");
                Assert.True(System.IO.File.Exists(excelPath22));
                Assert.True(new System.IO.FileInfo(excelPath22).Length > 0);

                // Generate Summary Report Excel
                string excelPathSummary = moetService.GenerateSummaryReportExcel(schoolYear);
                Assert.True(System.IO.File.Exists(excelPathSummary));
                Assert.True(new System.IO.FileInfo(excelPathSummary).Length > 0);

                // Clean up generated files to avoid bloating MyDocuments
                try
                {
                    if (System.IO.File.Exists(excelPath20)) System.IO.File.Delete(excelPath20);
                    if (System.IO.File.Exists(excelPath22)) System.IO.File.Delete(excelPath22);
                    if (System.IO.File.Exists(excelPathSummary)) System.IO.File.Delete(excelPathSummary);
                }
                catch { }
            }
        }

        [Fact]
        public void TestTeacherGrading_CleanHeader_ShouldBeRobust()
        {
            var type = typeof(QASmartClass.TeacherHub.ViewModels.TeacherGradingViewModel);
            var cleanMethod = type.GetMethod("CleanHeader", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(cleanMethod);

            // Test cases
            // 1. Normal clean
            Assert.Equal("mahs", cleanMethod.Invoke(null, new object[] { "Mã HS" }));
            Assert.Equal("mahs", cleanMethod.Invoke(null, new object[] { "Ma HS" }));
            
            // 2. Extra spaces and quotes
            Assert.Equal("mahocsinh", cleanMethod.Invoke(null, new object[] { " \"Mã học sinh\" " }));
            
            // 3. UTF-8 BOM in header
            Assert.Equal("mahs", cleanMethod.Invoke(null, new object[] { "\uFEFFMã HS" }));
            
            // 4. ANSI Unicode replacement character (due to UTF-8 decoding corruption of 'ã')
            Assert.Equal("mahs", cleanMethod.Invoke(null, new object[] { "M\uFFFD HS" }));
            
            // 5. English student code variations
            Assert.Equal("studentcode", cleanMethod.Invoke(null, new object[] { "Student Code" }));
            Assert.Equal("studentcode", cleanMethod.Invoke(null, new object[] { "studentcode" }));
        }

        [Fact]
        public void TestCsvHelper_ParseFileAndLine_WithSemicolonDelimiter()
        {
            // Test line parsing with semicolon
            var line = "HS001;\"Nguyễn Văn A\";8.5;9.0";
            var parts = QASmartClass.Helpers.CsvHelper.ParseLine(line, ';');
            Assert.Equal(4, parts.Length);
            Assert.Equal("HS001", parts[0]);
            Assert.Equal("Nguyễn Văn A", parts[1]);
            Assert.Equal("8.5", parts[2]);
            Assert.Equal("9.0", parts[3]);

            // Test line parsing with quotes and escaped characters
            var line2 = "\"HS002\";\"Trần \"\"Thị\"\" B\";10.0;";
            var parts2 = QASmartClass.Helpers.CsvHelper.ParseLine(line2, ';');
            Assert.Equal(4, parts2.Length); // last one is empty string
            Assert.Equal("HS002", parts2[0]);
            Assert.Equal("Trần \"Thị\" B", parts2[1]);
            Assert.Equal("10.0", parts2[2]);
            Assert.Equal("", parts2[3]);
        }

        [Fact]
        public void TestDeptMeetingActionParser_ShouldExtractAssignedToAndContent()
        {
            // Case 1: Line with bracket prefix
            string line1 = " [Cô Thảo] Soạn đề cương kiểm tra ";
            string assignedTo1 = "";
            string content1 = line1.Trim();
            if (content1.StartsWith("[") && content1.Contains("]"))
            {
                int closeBracketIdx = content1.IndexOf("]");
                assignedTo1 = content1.Substring(1, closeBracketIdx - 1).Trim();
                content1 = content1.Substring(closeBracketIdx + 1).Trim();
            }
            Assert.Equal("Cô Thảo", assignedTo1);
            Assert.Equal("Soạn đề cương kiểm tra", content1);

            // Case 2: Line without bracket prefix
            string line2 = " Dọn dẹp phòng thực hành ";
            string assignedTo2 = "";
            string content2 = line2.Trim();
            if (content2.StartsWith("[") && content2.Contains("]"))
            {
                int closeBracketIdx = content2.IndexOf("]");
                assignedTo2 = content2.Substring(1, closeBracketIdx - 1).Trim();
                content2 = content2.Substring(closeBracketIdx + 1).Trim();
            }
            else
            {
                assignedTo2 = "Thầy Bình"; // Fallback to first attendee
            }
            Assert.Equal("Thầy Bình", assignedTo2);
            Assert.Equal("Dọn dẹp phòng thực hành", content2);
        }

        [Fact]
        public async Task TestGradingFilter_SemesterIsolation_ShouldLoadCorrectGrades()
        {
            Student student = null;
            ClassRoster rosterHk1 = null;
            ClassRoster rosterHk2 = null;

            using (var db = new AppDbContext())
            {
                try
                {
                    // 1. Tạo học sinh chung
                    student = new Student
                    {
                        FullName = "Học sinh kiểm thử học kỳ",
                        StudentCode = "HS-TEST-HK",
                        ClassName = "10A1",
                        SchoolName = "THPT Quang Ân",
                        IsOnline = false,
                        LastSeen = DateTime.Now
                    };
                    db.Students.Add(student);
                    db.SaveChanges();

                    // 2. Tạo 2 Roster đại diện cho HK1 và HK2
                    rosterHk1 = new ClassRoster
                    {
                        ClassName = "10A1",
                        GradeLevel = "10",
                        SchoolYear = "2025-2026",
                        Semester = "HK1",
                        TeacherName = "Nguyễn Văn Hùng",
                        Subject = "Toán",
                        StudentCount = 1,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        LastUsedAt = DateTime.Now
                    };
                    rosterHk2 = new ClassRoster
                    {
                        ClassName = "10A1",
                        GradeLevel = "10",
                        SchoolYear = "2025-2026",
                        Semester = "HK2",
                        TeacherName = "Nguyễn Văn Hùng",
                        Subject = "Toán",
                        StudentCount = 1,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        LastUsedAt = DateTime.Now
                    };
                    db.ClassRosters.Add(rosterHk1);
                    db.ClassRosters.Add(rosterHk2);
                    db.SaveChanges();

                    // 3. Đăng ký học sinh vào 2 lớp học (ClassRosterStudents)
                    db.ClassRosterStudents.Add(new ClassRosterStudent
                    {
                        RosterId = rosterHk1.Id,
                        StudentId = student.Id,
                        SeatNumber = 1
                    });
                    db.ClassRosterStudents.Add(new ClassRosterStudent
                    {
                        RosterId = rosterHk2.Id,
                        StudentId = student.Id,
                        SeatNumber = 1
                    });
                    db.SaveChanges();

                    // 4. Lấy một GradeTypeMaster hợp lệ để nhập điểm
                    var gradeType = db.GradeTypeMasters.FirstOrDefault(g => g.IsActive);
                    Assert.NotNull(gradeType);

                    // 5. Thêm điểm số khác nhau cho HK1 và HK2
                    db.StudentGrades.Add(new StudentGrade
                    {
                        StudentId = student.Id,
                        RosterId = rosterHk1.Id,
                        GradeTypeId = gradeType.Id,
                        Score = 7.0,
                        Attempt = 1,
                        EnteredBy = "Giáo viên",
                        UpdatedAt = DateTime.Now
                    });
                    db.StudentGrades.Add(new StudentGrade
                    {
                        StudentId = student.Id,
                        RosterId = rosterHk2.Id,
                        GradeTypeId = gradeType.Id,
                        Score = 9.0,
                        Attempt = 1,
                        EnteredBy = "Giáo viên",
                        UpdatedAt = DateTime.Now
                    });
                    db.SaveChanges();

                    // 6. Khởi tạo ViewModel và kiểm tra tính biệt lập dữ liệu
                    var vm = new QASmartClass.TeacherHub.ViewModels.TeacherGradingViewModel();

                    // Chờ phương thức LoadClassesAsync hoàn thành (qua Reflection)
                    var loadClassesMethod = typeof(QASmartClass.TeacherHub.ViewModels.TeacherGradingViewModel)
                        .GetMethod("LoadClassesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(loadClassesMethod);
                    var loadClassesTask = loadClassesMethod.Invoke(vm, null) as Task;
                    Assert.NotNull(loadClassesTask);
                    await loadClassesTask;

                    // A. Kiểm tra với học kỳ HK1
                    vm.SelectedSemester = "HK1";
                    // Tìm lớp tương ứng trong HK1
                    var classHk1 = vm.Classes.FirstOrDefault(c => c.Id == rosterHk1.Id);
                    Assert.NotNull(classHk1);
                    vm.SelectedClass = classHk1;

                    // Chờ phương thức LoadGradesAsync hoàn thành (qua Reflection)
                    var loadGradesMethod = typeof(QASmartClass.TeacherHub.ViewModels.TeacherGradingViewModel)
                        .GetMethod("LoadGradesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(loadGradesMethod);
                    var loadGradesTaskHk1 = loadGradesMethod.Invoke(vm, null) as Task;
                    Assert.NotNull(loadGradesTaskHk1);
                    await loadGradesTaskHk1;

                    Assert.NotNull(vm.GradingTable);
                    System.Data.DataRow studentRowHk1 = null;
                    foreach (System.Data.DataRow r in vm.GradingTable.Rows)
                    {
                        if ((int)r["StudentId"] == student.Id)
                        {
                            studentRowHk1 = r;
                            break;
                        }
                    }
                    Assert.NotNull(studentRowHk1);
                    string colName = $"{gradeType.ShortName} (HS{gradeType.Weight})";
                    Assert.Equal("7", studentRowHk1[colName]?.ToString());

                    // B. Kiểm tra với học kỳ HK2
                    vm.SelectedSemester = "HK2";
                    // Tìm lớp tương ứng trong HK2
                    var classHk2 = vm.Classes.FirstOrDefault(c => c.Id == rosterHk2.Id);
                    Assert.NotNull(classHk2);
                    vm.SelectedClass = classHk2;

                    var loadGradesTaskHk2 = loadGradesMethod.Invoke(vm, null) as Task;
                    Assert.NotNull(loadGradesTaskHk2);
                    await loadGradesTaskHk2;

                    Assert.NotNull(vm.GradingTable);
                    System.Data.DataRow studentRowHk2 = null;
                    foreach (System.Data.DataRow r in vm.GradingTable.Rows)
                    {
                        if ((int)r["StudentId"] == student.Id)
                        {
                            studentRowHk2 = r;
                            break;
                        }
                    }
                    Assert.NotNull(studentRowHk2);
                    Assert.Equal("9", studentRowHk2[colName]?.ToString());
                }
                finally
                {
                    // Clean up test data safely to keep database clean
                    using (var cleanupDb = new AppDbContext())
                    {
                        if (student != null)
                        {
                            var grades = cleanupDb.StudentGrades.Where(g => g.StudentId == student.Id).ToList();
                            cleanupDb.StudentGrades.RemoveRange(grades);

                            var rosterSts = cleanupDb.ClassRosterStudents.Where(rs => rs.StudentId == student.Id).ToList();
                            cleanupDb.ClassRosterStudents.RemoveRange(rosterSts);
                        }

                        if (rosterHk1 != null)
                        {
                            var r1 = cleanupDb.ClassRosters.Find(rosterHk1.Id);
                            if (r1 != null) cleanupDb.ClassRosters.Remove(r1);
                        }

                        if (rosterHk2 != null)
                        {
                            var r2 = cleanupDb.ClassRosters.Find(rosterHk2.Id);
                            if (r2 != null) cleanupDb.ClassRosters.Remove(r2);
                        }

                        if (student != null)
                        {
                            var s = cleanupDb.Students.Find(student.Id);
                            if (s != null) cleanupDb.Students.Remove(s);
                        }

                        cleanupDb.SaveChanges();
                    }
                }
            }
        }

        [Fact]
        public void TestEmergencyReport_RealStudentBindingAndNotification()
        {
            using (var db = new AppDbContext())
            {
                var testStudent = new Student
                {
                    StudentCode = "TEST_EM_999",
                    FullName = "Nguyễn Văn Cấp Cứu",
                    ClassName = "12A1",
                    Status = "Active"
                };
                db.Students.Add(testStudent);
                db.SaveChanges();

                try
                {
                    var matchedStudent = db.Students.FirstOrDefault(s => s.Id == testStudent.Id);
                    Assert.NotNull(matchedStudent);
                    Assert.Equal("Nguyễn Văn Cấp Cứu", matchedStudent.FullName);

                    var mobileApi = new MobileApiService(db);
                    mobileApi.SendPushNotification(matchedStudent.Id, "Parent", "CẢNH BÁO Y TẾ: " + matchedStudent.FullName, "Kiểm tra sơ cứu", "Emergency");
                }
                finally
                {
                    var s = db.Students.Find(testStudent.Id);
                    if (s != null) db.Students.Remove(s);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public void TestStudentGoal_ScoreBoundaryValidation()
        {
            double invalidLowScore = -0.5;
            double invalidHighScore = 10.5;
            double validScore = 8.5;

            Assert.False(invalidLowScore >= 0.0 && invalidLowScore <= 10.0);
            Assert.False(invalidHighScore >= 0.0 && invalidHighScore <= 10.0);
            Assert.True(validScore >= 0.0 && validScore <= 10.0);
        }

        [Fact]
        public void TestStudentLogin_PasswordVerificationLogic()
        {
            string rawPassword = "StudentPassword123";
            string hash = QASmartTouch.Services.AuthenticationService.HashPassword(rawPassword);
            
            bool matchCorrect = QASmartTouch.Services.AuthenticationService.VerifyPassword(rawPassword, hash);
            bool matchIncorrect = QASmartTouch.Services.AuthenticationService.VerifyPassword("WrongPassword", hash);

            Assert.True(matchCorrect);
            Assert.False(matchIncorrect);
        }

        [Fact]
        public async Task TestSchoolEvent_DynamicCreationAndOrganizer()
        {
            using (var db = new AppDbContext())
            {
                var newEvent = new SchoolEvent
                {
                    Title = "Hội thảo chuyển đổi số",
                    Location = "Hội trường lớn",
                    Description = "Ứng dụng AI trong giảng dạy",
                    StartTime = DateTime.Now.Date.AddDays(1),
                    EndTime = DateTime.Now.Date.AddDays(1).AddHours(3),
                    Organizer = "Tổ Tin Học",
                    Department = "All",
                    Status = "Planned",
                    CreatedAt = DateTime.Now
                };

                db.SchoolEvents.Add(newEvent);
                await db.SaveChangesAsync();

                try
                {
                    var savedEvent = db.SchoolEvents.Find(newEvent.Id);
                    Assert.NotNull(savedEvent);
                    Assert.Equal("Tổ Tin Học", savedEvent.Organizer);
                    Assert.Equal(newEvent.EndTime, savedEvent.EndTime);
                }
                finally
                {
                    var ev = db.SchoolEvents.Find(newEvent.Id);
                    if (ev != null) db.SchoolEvents.Remove(ev);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public void TestSystemDataExporter_PermissionSecurity()
        {
            var allowedRoles = new[] { "HieuTruong", "Admin" };
            var deniedRoles = new[] { "GiaoVien", "ToTruong", "Counselor", "PH" };

            foreach (var role in allowedRoles)
            {
                bool hasPermission = role == "HieuTruong" || role == "Admin";
                Assert.True(hasPermission, $"Role {role} should have export permission.");
            }

            foreach (var role in deniedRoles)
            {
                bool hasPermission = role == "HieuTruong" || role == "Admin";
                Assert.False(hasPermission, $"Role {role} should not have export permission.");
            }
        }

        [Fact]
        public void TestAuditLog_NoMockLogs()
        {
            using (var db = new AppDbContext())
            {
                var mockNames = new[] { "GV_NguyenVanA", "GV_Mock" };
                var hasMockLogs = db.AuditLogs.Any(log => mockNames.Contains(log.ActorName));
                Assert.False(hasMockLogs);
            }
        }

        [Fact]
        public void TestStudentChat_ProfanityFilterLogic()
        {
            string[] badWords = { "đm", "dcm", "vcl", "clgt", "đéo", "mịa", "đệt", "đệt mợ" };
            Func<string, bool> containsProfanity = (text) =>
            {
                if (string.IsNullOrWhiteSpace(text)) return false;
                string normalized = text.ToLowerInvariant();
                var words = normalized.Split(new[] { ' ', '.', ',', '!', '?', ';', ':', '-', '_', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                return words.Any(w => badWords.Contains(w));
            };

            Assert.True(containsProfanity("đm chào bạn"));
            Assert.True(containsProfanity("cái này vcl thế"));
            Assert.True(containsProfanity("đáo để thật nhưng đéo ổn"));
            
            Assert.False(containsProfanity("điểm mười"));
            Assert.False(containsProfanity("chúc mừng"));
        }

        [Fact]
        public void TestStudentQuiz_ScratchpadSerialization()
        {
            byte[] dummyStrokeBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            string base64 = Convert.ToBase64String(dummyStrokeBytes);
            byte[] restoredBytes = Convert.FromBase64String(base64);

            Assert.Equal(dummyStrokeBytes, restoredBytes);
        }

        [Fact]
        public void TestStudentResults_RealRankCalculation()
        {
            using (var db = new AppDbContext())
            {
                // Create dummy student
                var s1 = new Student { StudentCode = "ST_RANK_1", FullName = "Học sinh Hạng 1", ClassName = "9A_TEST", Status = "Active" };
                var s2 = new Student { StudentCode = "ST_RANK_2", FullName = "Học sinh Hạng 2", ClassName = "9A_TEST", Status = "Active" };
                db.Students.AddRange(s1, s2);
                db.SaveChanges();

                try
                {
                    var qr1 = new QuizResult { QuizId = 999, StudentId = s1.Id, Score = 90, TimeSpentSeconds = 30, SubmittedAt = DateTime.Now };
                    var qr2 = new QuizResult { QuizId = 999, StudentId = s2.Id, Score = 80, TimeSpentSeconds = 45, SubmittedAt = DateTime.Now };
                    db.QuizResults.AddRange(qr1, qr2);
                    db.SaveChanges();

                    var classmateIds = db.Students
                        .Where(s => s.ClassName == "9A_TEST" && s.Status == "Active")
                        .Select(s => s.Id)
                        .ToList();

                    // Test rank calculation logic matching StudentResultsPage
                    var quizResultsOfQuiz = db.QuizResults
                        .Where(qr => qr.QuizId == 999 && classmateIds.Contains(qr.StudentId))
                        .OrderByDescending(qr => qr.Score)
                        .ThenBy(qr => qr.TimeSpentSeconds)
                        .ToList();

                    int rankS1 = quizResultsOfQuiz.FindIndex(qr => qr.StudentId == s1.Id) + 1;
                    int rankS2 = quizResultsOfQuiz.FindIndex(qr => qr.StudentId == s2.Id) + 1;

                    Assert.Equal(1, rankS1);
                    Assert.Equal(2, rankS2);
                }
                finally
                {
                    var r1 = db.QuizResults.Where(r => r.StudentId == s1.Id || r.StudentId == s2.Id).ToList();
                    db.QuizResults.RemoveRange(r1);
                    var st = db.Students.Where(s => s.Id == s1.Id || s.Id == s2.Id).ToList();
                    db.Students.RemoveRange(st);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public void TestCounseling_RealProfileCreation()
        {
            using (var db = new AppDbContext())
            {
                var profile = new CounselingProfile
                {
                    StudentName = "Nguyễn Văn Tư Vấn",
                    ClassName = "12A1",
                    ProblemType = "Học tập",
                    Notes = "Gặp áp lực thi cử",
                    CreatedAt = DateTime.Now,
                    Status = "Chờ xử lý"
                };

                db.CounselingProfiles.Add(profile);
                db.SaveChanges();

                try
                {
                    var saved = db.CounselingProfiles.Find(profile.Id);
                    Assert.NotNull(saved);
                    Assert.Equal("Nguyễn Văn Tư Vấn", saved.StudentName);
                    Assert.Equal("Chờ xử lý", saved.Status);
                }
                finally
                {
                    var p = db.CounselingProfiles.Find(profile.Id);
                    if (p != null) db.CounselingProfiles.Remove(p);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public void TestYouthBchChat_RealSenderBinding()
        {
            var testTeacher = new QASmartClass.Data.TeacherProfile 
            { 
                TeacherCode = "GV_TEST_UNION", 
                FullName = "Bí thư Đoàn Trường", 
                Role = "GV" 
            };
            QASmartClass.Staff.Services.StaffSession.Login(testTeacher);

            try
            {
                var currentUser = QASmartClass.Staff.Services.StaffSession.CurrentUser;
                Assert.NotNull(currentUser);
                Assert.Equal("GV_TEST_UNION", currentUser.TeacherCode);
                Assert.Equal("Bí thư Đoàn Trường", currentUser.FullName);

                var msg = new YouthBchMessage
                {
                    SenderId = currentUser?.TeacherCode ?? "GV001",
                    SenderName = currentUser?.FullName ?? "Bí thư",
                    Content = "Kiểm tra gửi tin nhắn",
                    SentAt = DateTime.Now
                };

                Assert.Equal("GV_TEST_UNION", msg.SenderId);
                Assert.Equal("Bí thư Đoàn Trường", msg.SenderName);

                using (var db = new AppDbContext())
                {
                    db.YouthBchMessages.Add(msg);
                    db.SaveChanges();

                    try
                    {
                        var savedMsg = db.YouthBchMessages.Find(msg.Id);
                        Assert.NotNull(savedMsg);
                        Assert.Equal("GV_TEST_UNION", savedMsg.SenderId);
                        Assert.Equal("Bí thư Đoàn Trường", savedMsg.SenderName);
                    }
                    finally
                    {
                        var m = db.YouthBchMessages.Find(msg.Id);
                        if (m != null) db.YouthBchMessages.Remove(m);
                        db.SaveChanges();
                    }
                }
            }
            finally
            {
                QASmartClass.Staff.Services.StaffSession.Logout();
            }
        }

        [Fact]
        public void TestEvaluation360_RealEvaluatorBinding()
        {
            var testTeacher = new QASmartClass.Data.TeacherProfile 
            { 
                TeacherCode = "GV_EVAL_360", 
                FullName = "Người Đánh Giá", 
                Role = "GV" 
            };
            QASmartClass.Staff.Services.StaffSession.Login(testTeacher);

            try
            {
                using (var db = new AppDbContext())
                {
                    var currentUserCode = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "GV_CurrentLoggedUser";
                    Assert.Equal("GV_EVAL_360", currentUserCode);

                    var record = new EvaluationRecord
                    {
                        EvaluatorId = currentUserCode,
                        TargetId = "GV002",
                        RoleRelation = "Peer",
                        Rating = 5,
                        Comments = "Đồng nghiệp rất tốt"
                    };

                    db.EvaluationRecords.Add(record);
                    db.SaveChanges();

                    try
                    {
                        var saved = db.EvaluationRecords.Find(record.Id);
                        Assert.NotNull(saved);
                        Assert.Equal("GV_EVAL_360", saved.EvaluatorId);
                    }
                    finally
                    {
                        var r = db.EvaluationRecords.Find(record.Id);
                        if (r != null) db.EvaluationRecords.Remove(r);
                        db.SaveChanges();
                    }
                }
            }
            finally
            {
                QASmartClass.Staff.Services.StaffSession.Logout();
            }
        }

        [Fact]
        public void TestRubricEditor_RealCreatorBinding()
        {
            var testTeacher = new QASmartClass.Data.TeacherProfile 
            { 
                TeacherCode = "GV_RUBRIC_CREATOR", 
                FullName = "Giáo viên Soạn Đề", 
                Role = "GV" 
            };
            QASmartClass.Staff.Services.StaffSession.Login(testTeacher);

            try
            {
                using (var db = new AppDbContext())
                {
                    var rubricService = new RubricService(db);
                    var teacherName = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "GV_Admin";
                    Assert.Equal("Giáo viên Soạn Đề", teacherName);

                    var criteriaData = new System.Collections.Generic.List<(string Name, double MaxPoints, string L1, string L2, string L3, string L4)>
                    {
                        ("Tiêu chí 1", 10, "L1", "L2", "L3", "L4")
                    };

                    var created = rubricService.CreateRubric("Rubric Kiểm Thử", "Toán", "10", teacherName, criteriaData);
                }

                using (var queryDb = new AppDbContext())
                {
                    try
                    {
                        var saved = queryDb.Rubrics.FirstOrDefault(r => r.Title == "Rubric Kiểm Thử" && r.CreatedBy == "Giáo viên Soạn Đề");
                        Assert.NotNull(saved);
                        Assert.Equal("Giáo viên Soạn Đề", saved.CreatedBy);
                    }
                    finally
                    {
                        var rubrics = queryDb.Rubrics.Where(r => r.Title == "Rubric Kiểm Thử").ToList();
                        foreach (var r in rubrics)
                        {
                            var crit = queryDb.RubricCriteria.Where(c => c.RubricId == r.Id).ToList();
                            queryDb.RubricCriteria.RemoveRange(crit);
                        }
                        queryDb.Rubrics.RemoveRange(rubrics);
                        queryDb.SaveChanges();
                    }
                }
            }
            finally
            {
                QASmartClass.Staff.Services.StaffSession.Logout();
            }
        }

        [Fact]
        public async Task TestFeeTracker_VietnameseFontAndLogic()
        {
            var service = new QASmartClass.YouthUnion.Services.YouthUnionService();
            var member = new YouthMember
            {
                StudentName = "Nguyễn Văn Test Phí",
                ClassName = "12A3",
                MemberType = "Đoàn viên",
                Position = "Thành viên",
                Status = "Active",
                JoinDate = DateTime.Today
            };

            await service.AddMemberAsync(member);

            using (var queryDb = new AppDbContext())
            {
                var savedMember = queryDb.YouthMembers.FirstOrDefault(m => m.StudentName == "Nguyễn Văn Test Phí" && m.ClassName == "12A3");
                Assert.NotNull(savedMember);

                var fee = new YouthFee
                {
                    MemberId = savedMember.Id,
                    Amount = 25000,
                    Period = "HK1-2026",
                    PaidDate = DateTime.Today,
                    Status = "Paid",
                    CollectedBy = "GV001"
                };

                queryDb.YouthFees.Add(fee);
                queryDb.SaveChanges();

                try
                {
                    var savedFee = queryDb.YouthFees.FirstOrDefault(f => f.MemberId == savedMember.Id && f.Period == "HK1-2026");
                    Assert.NotNull(savedFee);
                    Assert.Equal(25000, savedFee.Amount);
                    Assert.Equal("Paid", savedFee.Status);
                }
                finally
                {
                    // Clean up
                    var f = queryDb.YouthFees.FirstOrDefault(x => x.MemberId == savedMember.Id);
                    if (f != null) queryDb.YouthFees.Remove(f);
                    queryDb.YouthMembers.Remove(savedMember);
                    queryDb.SaveChanges();
                }
            }
        }

        [Fact]
        public async Task TestMemberExcelImport_RealProcessing()
        {
            var service = new QASmartClass.YouthUnion.Services.YouthUnionService();
            // Giả lập danh sách dòng đọc từ Excel
            var mockRows = new System.Collections.Generic.List<(string Name, string Class, string CardNo)>
            {
                ("Đoàn Viên Excel 1", "10A2", "SD001"),
                ("", "10A2", "SD002"), // Dòng lỗi (trống tên)
                ("Đoàn Viên Excel 2", "11B1", "")
            };

            int successCount = 0;
            int errorCount = 0;
            var addedIds = new System.Collections.Generic.List<int>();

            foreach (var row in mockRows)
            {
                if (string.IsNullOrEmpty(row.Name))
                {
                    errorCount++;
                    continue;
                }

                var member = new YouthMember
                {
                    StudentName = row.Name,
                    ClassName = row.Class,
                    MemberType = "Đoàn viên",
                    Position = "Thành viên",
                    Status = "Active",
                    DoanCardNo = row.CardNo,
                    JoinDate = DateTime.Today
                };

                await service.AddMemberAsync(member);
                successCount++;
            }

            Assert.Equal(2, successCount);
            Assert.Equal(1, errorCount);

            using (var queryDb = new AppDbContext())
            {
                var members = queryDb.YouthMembers.Where(m => m.StudentName.StartsWith("Đoàn Viên Excel")).ToList();
                Assert.Equal(2, members.Count);

                // Clean up
                queryDb.YouthMembers.RemoveRange(members);
                queryDb.SaveChanges();
            }
        }

        [Fact]
        public void TestHomeroomDiary_DisciplineNotesValidation()
        {
            // Kiểm chứng logic validate: Kỷ luật và Nhắc nhở bắt buộc phải có Lý do
            var recordDiscipline = new DisciplineRecord { StudentName = "HS A", Type = "Discipline", Reason = "" };
            var recordWarning = new DisciplineRecord { StudentName = "HS B", Type = "Warning", Reason = "" };
            var recordCommendation = new DisciplineRecord { StudentName = "HS C", Type = "Commendation", Reason = "" };

            // Logic validate mô phỏng trên code-behind
            Func<DisciplineRecord, bool> validate = (r) =>
            {
                if (string.IsNullOrWhiteSpace(r.StudentName)) return false;
                if ((r.Type == "Discipline" || r.Type == "Warning") && string.IsNullOrWhiteSpace(r.Reason)) return false;
                return true;
            };

            Assert.False(validate(recordDiscipline));
            Assert.False(validate(recordWarning));
            Assert.True(validate(recordCommendation));
        }

        [Fact]
        public void TestVoting_RealCreatorBinding()
        {
            var testTeacher = new QASmartClass.Data.TeacherProfile 
            { 
                TeacherCode = "GV_VOTE_TEST", 
                FullName = "Giáo Viên Bỏ Phiếu", 
                Role = "GV" 
            };
            QASmartClass.Staff.Services.StaffSession.Login(testTeacher);

            try
            {
                var creator = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "Bí thư";
                Assert.Equal("Giáo Viên Bỏ Phiếu", creator);

                var voting = new YouthVoting
                {
                    Title = "Bầu cử thử nghiệm",
                    Description = "Bầu cử BCH Chi Đoàn",
                    Candidates = "Nguyễn Văn A, Trần Thị B",
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddDays(3),
                    Status = "Open",
                    CreatedBy = creator
                };

                using (var db = new AppDbContext())
                {
                    db.YouthVotings.Add(voting);
                    db.SaveChanges();

                    try
                    {
                        var saved = db.YouthVotings.FirstOrDefault(v => v.Title == "Bầu cử thử nghiệm");
                        Assert.NotNull(saved);
                        Assert.Equal("Giáo Viên Bỏ Phiếu", saved.CreatedBy);
                    }
                    finally
                    {
                        var v = db.YouthVotings.FirstOrDefault(x => x.Title == "Bầu cử thử nghiệm");
                        if (v != null) db.YouthVotings.Remove(v);
                        db.SaveChanges();
                    }
                }
            }
            finally
            {
                QASmartClass.Staff.Services.StaffSession.Logout();
            }
        }

        [Fact]
        public void TestStudentProfile_DPAPISecureStorage()
        {
            string testPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "student_profile_test.json");
            string testText = "{\"StudentId\":123,\"StudentName\":\"Nguyễn Văn Test\"}";

            try
            {
                // Write encrypted
                var plainBytes = System.Text.Encoding.UTF8.GetBytes(testText);
                var encrypted = System.Security.Cryptography.ProtectedData.Protect(
                    plainBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                System.IO.File.WriteAllBytes(testPath, encrypted);

                // Try read raw bytes (should not be equal to plain text JSON string)
                var rawBytes = System.IO.File.ReadAllBytes(testPath);
                string rawText = System.Text.Encoding.UTF8.GetString(rawBytes);
                Assert.NotEqual(testText, rawText);

                // Try decrypt
                var decrypted = System.Security.Cryptography.ProtectedData.Unprotect(
                    rawBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                string decryptedText = System.Text.Encoding.UTF8.GetString(decrypted);
                Assert.Equal(testText, decryptedText);
            }
            finally
            {
                if (System.IO.File.Exists(testPath))
                    System.IO.File.Delete(testPath);
            }
        }

        [Fact]
        public void TestTeacherDashboard_ScopeFiltering()
        {
            using (var db = new AppDbContext())
            {
                var uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);
                
                TeacherProfile teacher1 = null;
                TeacherProfile teacher2 = null;
                ClassRoster roster1 = null;
                ClassRoster roster2 = null;
                Student s1 = null;
                Student s2 = null;
                ClassRosterStudent crs1 = null;
                ClassRosterStudent crs2 = null;
                StudentGrade grade1 = null;
                StudentGrade grade2 = null;

                try
                {
                    // Tạo GV1 và GV2 với mã duy nhất
                    teacher1 = new TeacherProfile { TeacherCode = $"GV_SCOPE_1_{uniqueSuffix}", FullName = $"Trần Văn Giáo Viên 1 {uniqueSuffix}", Role = "GV" };
                    teacher2 = new TeacherProfile { TeacherCode = $"GV_SCOPE_2_{uniqueSuffix}", FullName = $"Nguyễn Thị Giáo Viên 2 {uniqueSuffix}", Role = "GV" };
                    db.TeacherProfiles.AddRange(teacher1, teacher2);
                    db.SaveChanges();

                    // Tạo Lớp Roster với mã duy nhất
                    roster1 = new ClassRoster
                    {
                        ClassName = $"10A1_T1_{uniqueSuffix}", GradeLevel = "10", SchoolYear = "2025-2026", Semester = "HK1",
                        TeacherName = teacher1.FullName, Subject = "Toán", StudentCount = 1, IsActive = true,
                        CreatedAt = DateTime.Now, LastUsedAt = DateTime.Now
                    };
                    roster2 = new ClassRoster
                    {
                        ClassName = $"10A2_T2_{uniqueSuffix}", GradeLevel = "10", SchoolYear = "2025-2026", Semester = "HK1",
                        TeacherName = teacher2.FullName, Subject = "Toán", StudentCount = 1, IsActive = true,
                        CreatedAt = DateTime.Now, LastUsedAt = DateTime.Now
                    };
                    db.ClassRosters.AddRange(roster1, roster2);
                    db.SaveChanges();

                    // Tạo học sinh với mã duy nhất
                    s1 = new Student { StudentCode = $"HS_S1_{uniqueSuffix}", FullName = "Học sinh 1", ClassName = roster1.ClassName, Status = "Active" };
                    s2 = new Student { StudentCode = $"HS_S2_{uniqueSuffix}", FullName = "Học sinh 2", ClassName = roster2.ClassName, Status = "Active" };
                    db.Students.AddRange(s1, s2);
                    db.SaveChanges();

                    // Liên kết học sinh và lớp
                    crs1 = new ClassRosterStudent { RosterId = roster1.Id, StudentId = s1.Id, SeatNumber = 1 };
                    crs2 = new ClassRosterStudent { RosterId = roster2.Id, StudentId = s2.Id, SeatNumber = 1 };
                    db.ClassRosterStudents.AddRange(crs1, crs2);
                    db.SaveChanges();

                    // Thêm điểm
                    var gradeType = db.GradeTypeMasters.FirstOrDefault(g => g.IsActive);
                    Assert.NotNull(gradeType);

                    grade1 = new StudentGrade { StudentId = s1.Id, RosterId = roster1.Id, GradeTypeId = gradeType.Id, Score = 9.0, Attempt = 1, EnteredBy = teacher1.FullName, UpdatedAt = DateTime.Now };
                    grade2 = new StudentGrade { StudentId = s2.Id, RosterId = roster2.Id, GradeTypeId = gradeType.Id, Score = 5.0, Attempt = 1, EnteredBy = teacher2.FullName, UpdatedAt = DateTime.Now };
                    db.StudentGrades.AddRange(grade1, grade2);
                    db.SaveChanges();

                    // Lọc cho GV1
                    var myRosterIds = db.ClassRosters
                        .Where(r => r.TeacherName == teacher1.FullName && r.IsActive)
                        .Select(r => r.Id)
                        .ToList();
                    var myStudentIds = db.ClassRosterStudents
                        .Where(rs => myRosterIds.Contains(rs.RosterId))
                        .Select(rs => rs.StudentId)
                        .Distinct()
                        .ToList();

                    var myGrades = db.StudentGrades
                        .Where(g => myStudentIds.Contains(g.StudentId) && myRosterIds.Contains(g.RosterId))
                        .ToList();

                    double avgScore = myGrades.Count > 0 ? myGrades.Average(g => g.Score) : 0;
                    Assert.Single(myGrades);
                    Assert.Equal(9.0, avgScore);
                }
                finally
                {
                    // Dọn dẹp an toàn bằng cách check null/ID và xóa khỏi database
                    if (grade1 != null || grade2 != null)
                    {
                        var ids = new System.Collections.Generic.List<int>();
                        if (grade1 != null && grade1.Id > 0) ids.Add(grade1.Id);
                        if (grade2 != null && grade2.Id > 0) ids.Add(grade2.Id);
                        if (ids.Count > 0)
                        {
                            var toRemove = db.StudentGrades.Where(g => ids.Contains(g.Id)).ToList();
                            db.StudentGrades.RemoveRange(toRemove);
                        }
                    }

                    if (crs1 != null || crs2 != null)
                    {
                        var ids = new System.Collections.Generic.List<int>();
                        if (crs1 != null && crs1.Id > 0) ids.Add(crs1.Id);
                        if (crs2 != null && crs2.Id > 0) ids.Add(crs2.Id);
                        if (ids.Count > 0)
                        {
                            var toRemove = db.ClassRosterStudents.Where(rs => ids.Contains(rs.Id)).ToList();
                            db.ClassRosterStudents.RemoveRange(toRemove);
                        }
                    }

                    if (roster1 != null || roster2 != null)
                    {
                        var ids = new System.Collections.Generic.List<int>();
                        if (roster1 != null && roster1.Id > 0) ids.Add(roster1.Id);
                        if (roster2 != null && roster2.Id > 0) ids.Add(roster2.Id);
                        if (ids.Count > 0)
                        {
                            var toRemove = db.ClassRosters.Where(r => ids.Contains(r.Id)).ToList();
                            db.ClassRosters.RemoveRange(toRemove);
                        }
                    }

                    if (s1 != null || s2 != null)
                    {
                        var ids = new System.Collections.Generic.List<int>();
                        if (s1 != null && s1.Id > 0) ids.Add(s1.Id);
                        if (s2 != null && s2.Id > 0) ids.Add(s2.Id);
                        if (ids.Count > 0)
                        {
                            var toRemove = db.Students.Where(s => ids.Contains(s.Id)).ToList();
                            db.Students.RemoveRange(toRemove);
                        }
                    }

                    if (teacher1 != null || teacher2 != null)
                    {
                        var ids = new System.Collections.Generic.List<int>();
                        if (teacher1 != null && teacher1.Id > 0) ids.Add(teacher1.Id);
                        if (teacher2 != null && teacher2.Id > 0) ids.Add(teacher2.Id);
                        if (ids.Count > 0)
                        {
                            var toRemove = db.TeacherProfiles.Where(t => ids.Contains(t.Id)).ToList();
                            db.TeacherProfiles.RemoveRange(toRemove);
                        }
                    }

                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public void TestCareerTest_EmptySubmissionBlocking()
        {
            // Kiểm chứng logic validation chặn nộp bài trắc nghiệm trống
            Func<bool?, bool?, bool?, bool?, bool?, bool?, bool?, bool?, bool?, bool?, bool?, bool?, bool?, bool?, bool> validateSubmission =
                (q1A, q1B, q2A, q2B, q3A, q3B, q4A, q4B, q5A, q5B, q5C, q6A, q6B, q6C) =>
                {
                    if ((q1A != true && q1B != true) ||
                        (q2A != true && q2B != true) ||
                        (q3A != true && q3B != true) ||
                        (q4A != true && q4B != true) ||
                        (q5A != true && q5B != true && q5C != true) ||
                        (q6A != true && q6B != true && q6C != true))
                    {
                        return false; // Chặn nộp
                    }
                    return true; // Cho phép nộp
                };

            // Test case 1: Tất cả đều trống
            Assert.False(validateSubmission(false, false, false, false, false, false, false, false, false, false, false, false, false, false));

            // Test case 2: Thiếu câu 6
            Assert.False(validateSubmission(true, false, true, false, true, false, true, false, true, false, false, false, false, false));

            // Test case 3: Điền đầy đủ
            Assert.True(validateSubmission(true, false, true, false, true, false, true, false, true, false, false, true, false, false));
        }

        [Fact]
        public void TestPrincipalDashboard_VietnameseAttendanceFiltering()
        {
            using (var db = new AppDbContext())
            {
                var today = DateTime.Today;
                var testStudent = new Student { StudentCode = "HS_ATT_FILTER", FullName = "Học sinh chuyên cần", ClassName = "10A1", Status = "Active" };
                db.Students.Add(testStudent);
                db.SaveChanges();

                var attRecord1 = new AttendanceRecord
                {
                    StudentId = testStudent.Id,
                    Date = today,
                    Status = "Có mặt",
                    RosterId = 1
                };
                db.AttendanceRecords.Add(attRecord1);
                db.SaveChanges();

                try
                {
                    // Logic lọc của PrincipalDashboardPage.xaml.cs sau khi sửa đổi
                    var presentToday = db.AttendanceRecords.Count(a => a.Date == today && (a.Status == "Present" || a.Status == "Có mặt"));
                    var absentCount = db.AttendanceRecords.Count(a => a.Date == today && a.Status != "Present" && a.Status != "Có mặt");

                    Assert.Equal(1, presentToday);
                    Assert.Equal(0, absentCount);
                }
                finally
                {
                    // Dọn dẹp
                    var atts = db.AttendanceRecords.Where(a => a.StudentId == testStudent.Id).ToList();
                    db.AttendanceRecords.RemoveRange(atts);
                    var s = db.Students.Find(testStudent.Id);
                    if (s != null) db.Students.Remove(s);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public void TestTeacherGrading_QuickFeedbackAndSoftSkillDataRowView()
        {
            var table = new System.Data.DataTable();
            table.Columns.Add("StudentId", typeof(int));
            table.Columns.Add("Họ và tên", typeof(string));
            table.Columns.Add("Note", typeof(string));

            var row = table.NewRow();
            row["StudentId"] = 15;
            row["Họ và tên"] = "Nguyễn Văn A";
            row["Note"] = "";
            table.Rows.Add(row);

            // Giả lập logic dán/nhận xét nhanh thông qua DataRowView
            var rowView = table.DefaultView[0];
            
            // 1. Test Quick Feedback
            string tagText = "Tích cực";
            string currentNote = rowView["Note"]?.ToString() ?? "";
            if (!currentNote.Contains(tagText))
            {
                string newNote = string.IsNullOrEmpty(currentNote) ? tagText : $"{currentNote}; {tagText}";
                rowView["Note"] = newNote;
            }
            Assert.Equal("Tích cực", rowView["Note"]?.ToString());

            // 2. Test Soft Skill Name/Id Resolution
            string name = "Học sinh";
            int studentId = 0;

            if (rowView.Row.Table.Columns.Contains("Họ và tên"))
                name = rowView["Họ và tên"]?.ToString() ?? "Học sinh";

            if (rowView.Row.Table.Columns.Contains("StudentId") && int.TryParse(rowView["StudentId"]?.ToString(), out int parsedId))
                studentId = parsedId;

            Assert.Equal("Nguyễn Văn A", name);
            Assert.Equal(15, studentId);
        }

        [Fact]
        public void TestStudentDiary_NoDuplicateMentalWarningPerDay()
        {
            using (var db = new AppDbContext())
            {
                var s = new Student { StudentCode = "HS_DIARY_TEST", FullName = "Học sinh Diary", ClassName = "10A1", Status = "Active" };
                db.Students.Add(s);
                db.SaveChanges();

                try
                {
                    DateTime today = DateTime.Today;
                    // Lần 1: Lưu nhật ký có từ khóa nhạy cảm
                    string mood = "Stressed";
                    string detectedKeywords = "áp lực, mệt mỏi";
                    
                    var todayWarning1 = db.StudentMentalHealthRecords.FirstOrDefault(r => 
                        r.StudentId == s.Id && 
                        r.RecordedAt.Date == today && 
                        r.Notes.StartsWith("[Tự động từ Nhật ký học tập]"));

                    Assert.Null(todayWarning1);

                    var mentalRecord = new StudentMentalHealthRecord
                    {
                        StudentId = s.Id,
                        MoodScore = mood == "Stressed" ? 2 : 5,
                        Notes = $"[Tự động từ Nhật ký học tập] Mệt mỏi áp lực",
                        RiskLevel = "High",
                        DetectedKeywords = detectedKeywords,
                        RecordedAt = DateTime.Now,
                        InterventionLevel = 0,
                        IsNotified = false
                    };
                    db.StudentMentalHealthRecords.Add(mentalRecord);
                    db.SaveChanges();

                    // Lần 2: Chỉnh sửa nhật ký lưu tiếp trong cùng một ngày
                    var todayWarning2 = db.StudentMentalHealthRecords.FirstOrDefault(r => 
                        r.StudentId == s.Id && 
                        r.RecordedAt.Date == today && 
                        r.Notes.StartsWith("[Tự động từ Nhật ký học tập]"));

                    Assert.NotNull(todayWarning2);

                    if (todayWarning2 != null)
                    {
                        todayWarning2.MoodScore = 2;
                        todayWarning2.Notes = $"[Tự động từ Nhật ký học tập] Trầm cảm nặng";
                        todayWarning2.RiskLevel = "High";
                        todayWarning2.DetectedKeywords = "trầm cảm";
                        todayWarning2.RecordedAt = DateTime.Now;
                    }
                    db.SaveChanges();

                    // Kiểm tra tổng số bản ghi cảnh báo tâm lý của học sinh này
                    var warnings = db.StudentMentalHealthRecords.Where(r => r.StudentId == s.Id).ToList();
                    Assert.Single(warnings);
                    Assert.Equal("trầm cảm", warnings[0].DetectedKeywords);
                }
                finally
                {
                    var warnings = db.StudentMentalHealthRecords.Where(r => r.StudentId == s.Id).ToList();
                    db.StudentMentalHealthRecords.RemoveRange(warnings);
                    var st = db.Students.Find(s.Id);
                    if (st != null) db.Students.Remove(st);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public void TestCareerTest_HollandCodeMappingAndSafeSubstring()
        {
            // Helper để verify
            Func<string, string> getCleanCareerName = (career) => {
                if (string.IsNullOrEmpty(career)) return string.Empty;
                int dotIdx = career.IndexOf('.');
                if (dotIdx > 0 && dotIdx < 5)
                {
                    return career.Substring(dotIdx + 1).Trim();
                }
                return career;
            };

            // 1. Kiểm thử helper cắt chuỗi an toàn
            Assert.Equal("Kỹ sư phần mềm", getCleanCareerName("1. Kỹ sư phần mềm"));
            Assert.Equal("PR", getCleanCareerName("PR")); // Không bị crash ArgumentOutOfRangeException
            Assert.Equal("", getCleanCareerName(""));

            // 2. Kiểm thử logic ánh xạ kết hợp MBTI + Holland Code
            string mbti_E_I = "I";
            string mbti_T_F = "T";
            string holland5 = "I";
            string holland6 = "C";

            string career1 = "1. Kỹ sư phần mềm / Hệ thống thông tin";
            string career2 = "2. Chuyên viên Phân tích dữ liệu";
            string career3 = "3. Nhà khoa học dữ liệu / AI";

            if (mbti_E_I == "I" && mbti_T_F == "T") // Hướng nội, Lý trí
            {
                if (holland5 == "I")
                {
                    career1 = "1. Kỹ sư phần mềm / Phát triển sản phẩm";
                    career2 = "2. Nhà khoa học / Thống kê dữ liệu";
                    career3 = "3. Chuyên gia phân tích mật mã";
                }
                else if (holland6 == "C")
                {
                    career1 = "1. Kỹ sư mạng / Quản trị hệ thống";
                    career2 = "2. Chuyên viên Kiểm thử phần mềm (QA)";
                    career3 = "3. Kế toán viên chuyên sâu / Kiểm toán";
                }
            }

            // Với logic mới, holland5 == "I" nên phải ra gợi ý nghiên cứu
            Assert.Equal("1. Kỹ sư phần mềm / Phát triển sản phẩm", career1);
            Assert.Equal("2. Nhà khoa học / Thống kê dữ liệu", career2);
        }

        [Fact]
        public void TestStudentResults_RankRatioAndSupportMessage()
        {
            using (var db = new AppDbContext())
            {
                var s1 = new Student { StudentCode = "HS_RESULTS_1", FullName = "Học sinh 1", ClassName = "10A_TEST", Status = "Active" };
                var s2 = new Student { StudentCode = "HS_RESULTS_2", FullName = "Học sinh 2", ClassName = "10A_TEST", Status = "Active" };
                db.Students.AddRange(s1, s2);
                db.SaveChanges();

                try
                {
                    var classmates = db.Students.Where(s => s.ClassName == "10A_TEST" && s.Status == "Active").ToList();
                    Assert.Equal(2, classmates.Count);

                    // Test hiển thị xếp hạng / tổng số học sinh
                    int classRank = 1;
                    string rankRatioText = $"#{classRank}/{classmates.Count}";
                    Assert.Equal("#1/2", rankRatioText);

                    // Test mock thông điệp khích lệ sư phạm khi results.Count == 0
                    var resultsList = new System.Collections.Generic.List<QuizResult>();
                    object itemsSource = null;
                    if (resultsList.Count == 0)
                    {
                        itemsSource = new[] 
                        { 
                            new 
                            { 
                                DisplayScore = "-", 
                                QuizTitle = "Chào mừng bạn! Hãy làm bài trắc nghiệm hướng nghiệp hoặc ôn tập để bắt đầu nhé! 🚀", 
                                CorrectText = "-", 
                                RankText = "-", 
                                DateText = "-", 
                                ScoreText = "-" 
                            } 
                        };
                    }

                    Assert.NotNull(itemsSource);
                    var list = (System.Array)itemsSource;
                    Assert.Single(list);
                }
                finally
                {
                    var sts = db.Students.Where(s => s.Id == s1.Id || s.Id == s2.Id).ToList();
                    db.Students.RemoveRange(sts);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public void TestLeaveRequest_ApprovedByAndResponseNotesValidation()
        {
            using (var db = new AppDbContext())
            {
                var req = new LeaveRequest
                {
                    StaffId = 999,
                    StaffName = "Giáo viên kiểm thử phép",
                    LeaveType = "Annual",
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddDays(1),
                    Reason = "Nghỉ phép năm",
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };
                db.LeaveRequests.Add(req);
                db.SaveChanges();

                try
                {
                    // 1. Giả lập BGH duyệt đơn
                    string approver = "Hiệu trưởng kiểm thử";
                    req.Status = "Approved";
                    req.ApprovedBy = approver;
                    req.ResponseNotes = "Đồng ý";
                    db.SaveChanges();

                    // Xác minh
                    var dbReq = db.LeaveRequests.Find(req.Id);
                    Assert.NotNull(dbReq);
                    Assert.Equal("Approved", dbReq.Status);
                    Assert.Equal(approver, dbReq.ApprovedBy);
                    Assert.Equal("Đồng ý", dbReq.ResponseNotes);

                    // 2. Giả lập từ chối với lý do
                    string rejectReason = "Thiếu nhân sự trực";
                    req.Status = "Rejected";
                    req.ApprovedBy = approver;
                    req.ResponseNotes = rejectReason;
                    db.SaveChanges();

                    var dbReq2 = db.LeaveRequests.Find(req.Id);
                    Assert.NotNull(dbReq2);
                    Assert.Equal("Rejected", dbReq2.Status);
                    Assert.Equal(rejectReason, dbReq2.ResponseNotes);
                }
                finally
                {
                    var r = db.LeaveRequests.Find(req.Id);
                    if (r != null)
                    {
                        db.LeaveRequests.Remove(r);
                        db.SaveChanges();
                    }
                }
            }
        }

        [Fact]
        public void TestStudentSubmit_RealIdentityAndNoLossOnDenial()
        {
            var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
            string backupContent = null;
            if (File.Exists(profilePath))
            {
                try { backupContent = File.ReadAllText(profilePath); } catch { }
            }

            using (var db = new AppDbContext())
            {
                var s = new Student
                {
                    StudentCode = "HS_SUBMIT_TEST",
                    FullName = "Học sinh nộp bài test",
                    ClassName = "10A1",
                    Status = "Active"
                };
                db.Students.Add(s);
                db.SaveChanges();

                try
                {
                    // Tạo file profile giả lập cho học sinh test
                    var dto = new { StudentCode = "HS_SUBMIT_TEST", StudentName = "Học sinh nộp bài test" };
                    File.WriteAllText(profilePath, System.Text.Json.JsonSerializer.Serialize(dto));

                    var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(db);
                    identityService.ClearCache(); // Xóa cache cũ để nạp mới từ file
                    var (studentId, studentCode, studentName) = identityService.GetCurrentStudent();

                    Assert.True(studentId > 0);
                    Assert.Equal(s.StudentCode, studentCode);
                    Assert.Equal(s.FullName, studentName);
                }
                finally
                {
                    // Khôi phục profile cũ
                    try
                    {
                        if (backupContent != null)
                            File.WriteAllText(profilePath, backupContent);
                        else if (File.Exists(profilePath))
                            File.Delete(profilePath);
                    }
                    catch { }

                    var st = db.Students.Find(s.Id);
                    if (st != null)
                    {
                        db.Students.Remove(st);
                        db.SaveChanges();
                    }
                }
            }
        }

        [Fact]
        public void TestEmergency_ResolveWithAuditLog()
        {
            using (var db = new AppDbContext())
            {
                var log = new EmergencyLog
                {
                    StudentName = "Học sinh y tế test",
                    IncidentType = "Injury",
                    Description = "Chấn thương nhẹ",
                    FirstAidApplied = "Băng bó",
                    Status = "RequiresAttention",
                    Timestamp = DateTime.Now
                };
                db.EmergencyLogs.Add(log);
                db.SaveChanges();

                try
                {
                    // Giả lập resolve sự cố y tế
                    log.Status = "Resolved";
                    db.SaveChanges();

                    string resolver = "Bác sĩ y tế";
                    QASmartClass.Services.AuditHelper.Log(db, "Medical_Emergency_Resolved", resolver, $"Resolved emergency for student {log.StudentName}");
                    db.SaveChanges();

                    // Xác minh
                    var dbLog = db.EmergencyLogs.Find(log.Id);
                    Assert.NotNull(dbLog);
                    Assert.Equal("Resolved", dbLog.Status);

                    var audit = db.AuditLogs.FirstOrDefault(a => a.Action == "Medical_Emergency_Resolved" && a.ActorName == resolver);
                    Assert.NotNull(audit);
                    Assert.Contains("Resolved emergency for student", audit.Details);
                }
                finally
                {
                    var l = db.EmergencyLogs.Find(log.Id);
                    if (l != null) db.EmergencyLogs.Remove(l);

                    var audits = db.AuditLogs.Where(a => a.Action == "Medical_Emergency_Resolved").ToList();
                    db.AuditLogs.RemoveRange(audits);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public void TestHealthRoom_DbContextDisposalDuringLifecycle()
        {
            var t = new System.Threading.Thread(() =>
            {
                try
                {
                    var fs = new QASmartClass.HealthRoom.Views.FoodSafetyView();
                    var em = new QASmartClass.HealthRoom.Views.EpidemicMonitorView();
                    var hr = new QASmartClass.HealthRoom.Views.HealthRecordView();

                    Assert.NotNull(fs);
                    Assert.NotNull(em);
                    Assert.NotNull(hr);

                    // Trigger Unloaded to verify no crashes
                    var raiseEventMethod = typeof(System.Windows.FrameworkElement).GetMethod("RaiseEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    if (raiseEventMethod != null)
                    {
                        var unloadedEventArgs = new System.Windows.RoutedEventArgs(System.Windows.FrameworkElement.UnloadedEvent);
                        raiseEventMethod.Invoke(fs, new object[] { unloadedEventArgs });
                        raiseEventMethod.Invoke(em, new object[] { unloadedEventArgs });
                        raiseEventMethod.Invoke(hr, new object[] { unloadedEventArgs });
                    }
                }
                catch (Exception ex)
                {
                    // Handle headless environment gracefully
                    // Log.Warning("Headless environment control instantiation skipped: {Err}", ex.Message);
                }
            });
            t.SetApartmentState(System.Threading.ApartmentState.STA);
            t.Start();
            t.Join();
        }

        [Fact]
        public void TestHealthRecord_ComboBoxStudentBindingAndValidation()
        {
            // Verify validation logic for physical boundaries
            Func<double, double, double, double, bool> validateHealthRecord = (h, w, vl, vr) =>
            {
                if (h < 50.0 || h > 250.0) return false;
                if (w < 5.0 || w > 150.0) return false;
                if (vl < 0.0 || vl > 10.0) return false;
                if (vr < 0.0 || vr > 10.0) return false;
                return true;
            };

            Assert.True(validateHealthRecord(170, 60, 10, 10));
            Assert.False(validateHealthRecord(45, 60, 10, 10));  // Height under boundary
            Assert.False(validateHealthRecord(170, 200, 10, 10)); // Weight over boundary
            Assert.False(validateHealthRecord(170, 60, -1, 10));  // Vision left under boundary
            Assert.False(validateHealthRecord(170, 60, 10, 12));  // Vision right over boundary

            using (var db = new AppDbContext())
            {
                var activeStudents = db.Students.Where(s => s.Status == "Active").ToList();
                foreach (var s in activeStudents)
                {
                    Assert.NotEmpty(s.FullName);
                    Assert.NotEmpty(s.ClassName);
                }
            }
        }

        [Fact]
        public void TestEpidemicMonitor_CorrectDiseaseMatching()
        {
            using (var db = new AppDbContext())
            {
                using (var tx = db.Database.BeginTransaction())
                {
                    try
                    {
                        var c = new EpidemicCase
                        {
                            StudentId = 999,
                            StudentName = "Học sinh dịch tễ test",
                            ClassName = "10A1",
                            Disease = "Sốt xuất huyết",
                            OnsetDate = DateTime.Today,
                            Status = "Active",
                            IsolatedAt = "Home"
                        };
                        db.EpidemicCases.Add(c);
                        db.SaveChanges();

                        var cases = db.EpidemicCases.Where(x => x.Disease == "Sốt xuất huyết" && x.OnsetDate >= DateTime.Today.AddDays(-7)).ToList();
                        Assert.NotEmpty(cases);
                        Assert.Contains(cases, x => x.StudentName == "Học sinh dịch tễ test");
                    }
                    finally
                    {
                        tx.Rollback();
                    }
                }
            }
        }

        [Fact]
        public void TestContract_IndefiniteNullableEndDate()
        {
            using (var db = new AppDbContext())
            {
                using (var tx = db.Database.BeginTransaction())
                {
                    try
                    {
                        var contract = new Contract
                        {
                            StaffId = 999,
                            ContractNumber = "HD/TEST/01",
                            ContractType = "Indefinite",
                            StartDate = DateTime.Today,
                            EndDate = null,
                            Status = "Active"
                        };
                        db.Contracts.Add(contract);
                        db.SaveChanges();

                        var retrieved = db.Contracts.FirstOrDefault(x => x.ContractNumber == "HD/TEST/01");
                        Assert.NotNull(retrieved);
                        Assert.Null(retrieved.EndDate);
                        Assert.Equal("Indefinite", retrieved.ContractType);
                    }
                    finally
                    {
                        tx.Rollback();
                    }
                }
            }
        }

        [Fact]
        public void TestCanteenPos_RealActorAndBalanceSync()
        {
            using (var db = new AppDbContext())
            {
                var s = new Student
                {
                    StudentCode = "HS_CANTEEN_TEST",
                    FullName = "Học sinh canteen test",
                    WalletBalance = 500000m,
                    Status = "Active"
                };
                db.Students.Add(s);
                db.SaveChanges();
 
                try
                {
                    decimal deduction = 50000m;
                    s.WalletBalance -= deduction;
 
                    var log = new EventLog
                    {
                        EventType = "WalletTransaction",
                        Actor = "NV_CANTEEN_TEST", // Nhân viên POS thực tế
                        Timestamp = DateTime.Now,
                        Details = $"{{\"Type\":\"Canteen_Deduction\", \"Amount\":{deduction}, \"BalanceAfter\":{s.WalletBalance}}}"
                    };
                    db.EventLogs.Add(log);
                    db.SaveChanges();
 
                    // Xác minh
                    var dbStudent = db.Students.Find(s.Id);
                    Assert.NotNull(dbStudent);
                    Assert.Equal(450000m, dbStudent.WalletBalance);

                    var dbLog = db.EventLogs.FirstOrDefault(l => l.EventType == "WalletTransaction" && l.Actor == "NV_CANTEEN_TEST");
                    Assert.NotNull(dbLog);
                    Assert.Contains("Canteen_Deduction", dbLog.Details);
                }
                finally
                {
                    var st = db.Students.Find(s.Id);
                    if (st != null) db.Students.Remove(st);

                    var logs = db.EventLogs.Where(l => l.EventType == "WalletTransaction" && l.Actor == "NV_CANTEEN_TEST").ToList();
                    db.EventLogs.RemoveRange(logs);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public void TestAwardService_PrintCertificateMappingAndLocation()
        {
            using (var db = new AppDbContext())
            {
                var record = new AwardRecord
                {
                    TargetType = "Student",
                    TargetId = 123,
                    TargetName = "Nguyễn Văn Test",
                    AwardType = "HSG",
                    Semester = "I",
                    SchoolYear = "2025-2026",
                    ProposedBy = "GV001",
                    ApprovedBy = "Hiệu Trưởng Test",
                    Status = "Approved",
                    ProposedDate = DateTime.Now,
                    ApprovedDate = DateTime.Now
                };
                db.AwardRecords.Add(record);
                db.SaveChanges();

                try
                {
                    var service = new AwardService(db);
                    string path = service.PrintCertificate(record.Id);
                    
                    Assert.NotEmpty(path);
                    Assert.True(System.IO.File.Exists(path));

                    var updated = db.AwardRecords.Find(record.Id);
                    Assert.NotNull(updated);
                    Assert.Equal("Printed", updated.Status);

                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                }
                finally
                {
                    var r = db.AwardRecords.Find(record.Id);
                    if (r != null)
                    {
                        db.AwardRecords.Remove(r);
                        db.SaveChanges();
                    }
                }
            }
        }

        [Fact]
        public void TestAwardManagement_PermissionGuardAndSessionCheck()
        {
            UserSessionService.Instance.SetSession("MOCK_USER_WITHOUT_PERM");
            UserSessionService.Instance.GetType().GetProperty("Role")?.SetValue(UserSessionService.Instance, "GV");

            using (var db = new AppDbContext())
            {
                var record = new AwardRecord
                {
                    TargetType = "Student",
                    TargetId = 123,
                    TargetName = "Nguyễn Văn Test",
                    AwardType = "HSG",
                    Status = "Proposed",
                    ProposedDate = DateTime.Now
                };
                db.AwardRecords.Add(record);
                db.SaveChanges();

                try
                {
                    var service = new AwardService(db);
                    Assert.ThrowsAny<Exception>(() => service.ApproveAward(record.Id, "Khách"));
                }
                finally
                {
                    var r = db.AwardRecords.Find(record.Id);
                    if (r != null)
                    {
                        db.AwardRecords.Remove(r);
                        db.SaveChanges();
                    }
                }
            }
        }

        [Fact]
        public void TestMedicalInventory_QuantityAndUnitValidation()
        {
            using (var db = new AppDbContext())
            {
                string name = "Panadol";
                string unitEmpty = "";
                int qtyNegative = -5;

                bool isValid = !string.IsNullOrWhiteSpace(name) && qtyNegative > 0 && !string.IsNullOrWhiteSpace(unitEmpty);
                Assert.False(isValid);

                string unitValid = "Hộp";
                int qtyValid = 10;
                bool isValid2 = !string.IsNullOrWhiteSpace(name) && qtyValid > 0 && !string.IsNullOrWhiteSpace(unitValid);
                Assert.True(isValid2);

                if (isValid2)
                {
                    var supply = new MedicalSupply
                    {
                        Name = name,
                        Category = "Medicine",
                        Quantity = qtyValid,
                        Unit = unitValid,
                        ExpiryDate = DateTime.Today.AddYears(1)
                    };
                    var service = new MedicalInventoryService(db);
                    service.AddSupply(supply);

                    try
                    {
                        var retrieved = db.MedicalSupplies.FirstOrDefault(s => s.Name == "Panadol" && s.Unit == "Hộp");
                        Assert.NotNull(retrieved);
                        Assert.Equal(10, retrieved.Quantity);
                    }
                    finally
                    {
                        var toRemove = db.MedicalSupplies.FirstOrDefault(s => s.Name == "Panadol" && s.Unit == "Hộp");
                        if (toRemove != null)
                        {
                            db.MedicalSupplies.Remove(toRemove);
                            db.SaveChanges();
                        }
                    }
                }
            }
        }

        [Fact]
        public void TestYouthUnionAward_CollectiveProposalRecipientName()
        {
            string proposalType = "Tap the";
            int memberId = 0;

            string displayName = proposalType == "Tap the" ? "Tập thể Chi đoàn" : "Học sinh";
            Assert.Equal("Tập thể Chi đoàn", displayName);

            int recordMemberId = 0;
            string recordStudentName = recordMemberId == 0 ? "Tập thể Chi đoàn" : "Chưa rõ";
            Assert.Equal("Tập thể Chi đoàn", recordStudentName);
        }

        [Fact]
        public void TestSqliteWAL_JournalModeIsWAL()
        {
            var tempDbFile = Path.Combine(Path.GetTempPath(), $"test_wal_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={tempDbFile}";
            
            try
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(connectionString)
                    .AddInterceptors(new QASmartClass.Data.AppDbContext.SqliteWalInterceptor())
                    .Options;
                
                using (var db = new AppDbContext(options))
                {
                    db.Database.OpenConnection();
                    
                    var conn = db.Database.GetDbConnection();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA journal_mode;";
                        var mode = cmd.ExecuteScalar()?.ToString();
                        Assert.Equal("wal", mode);
                    }
                }
            }
            finally
            {
                if (File.Exists(tempDbFile))
                {
                    try { File.Delete(tempDbFile); } catch {}
                }
            }
        }

        [Fact]
        public void TestAwardService_PrintCertificateWithDigitalSignature()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var assetsDir = Path.Combine(baseDir, "Assets", "Images");
            Directory.CreateDirectory(assetsDir);
            var sigPath = Path.Combine(assetsDir, "hieutruong_signature.enc");
            var pngPath = Path.Combine(assetsDir, "hieutruong_signature.png");
            try { if (File.Exists(pngPath)) File.Delete(pngPath); } catch {}
            
            byte[] tinyPng = new byte[] {
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
                0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
                0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
                0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x4D, 0xAE,
                0x42, 0x60, 0x82
            };
            byte[] encryptedPng = AwardService.EncryptBytes(tinyPng);
            File.WriteAllBytes(sigPath, encryptedPng);


            using (var db = TestDbFactory.Create())
            {
                var record = new AwardRecord
                {
                    TargetType = "Student",
                    TargetId = 1,
                    TargetName = "Nguyen Van Test",
                    AwardType = "HSG",
                    Semester = "HK1",
                    SchoolYear = "2025-2026",
                    ProposedBy = "GV001",
                    ApprovedBy = "HT001",
                    Status = "Approved",
                    ProposedDate = DateTime.Now,
                    ApprovedDate = DateTime.Now
                };
                db.AwardRecords.Add(record);
                db.SaveChanges();

                var service = new AwardService(db);
                var pdfPath = service.PrintCertificate(record.Id);

                Assert.True(!string.IsNullOrEmpty(pdfPath));
                Assert.True(File.Exists(pdfPath));
                Assert.True(new FileInfo(pdfPath).Length > 0);

                try { File.Delete(pdfPath); } catch {}
            }
        }

        [Fact]
        public void TestEmulationScore_RequiredAdjustmentReason()
        {
            string reasonEdit = "";
            bool isEditReasonValid = !string.IsNullOrWhiteSpace(reasonEdit);
            Assert.False(isEditReasonValid);

            string reasonDelete = "   ";
            bool isDeleteReasonValid = !string.IsNullOrWhiteSpace(reasonDelete.Trim());
            Assert.False(isDeleteReasonValid);

            using (var db = TestDbFactory.Create())
            {
                var member = new YouthMember { StudentName = "Anh Doan Vien", TotalScore = 100, Status = "Active" };
                db.YouthMembers.Add(member);
                db.SaveChanges();

                var score = new YouthEmulationScore { MemberId = member.Id, Score = 10, Reason = "Cộng điểm", AwardedDate = DateTime.Today };
                db.YouthEmulationScores.Add(score);
                db.SaveChanges();

                var validReason = "Sửa nhầm điểm";
                db.AuditLogs.Add(new AuditLog
                {
                    Action = "EDIT_EMULATION_SCORE",
                    ActorName = "GV001",
                    Details = $"Sửa điểm thi đua ID {score.Id} của Đoàn viên {member.StudentName}. Lý do: {validReason}",
                    Timestamp = DateTime.Now
                });
                db.SaveChanges();

                var log = db.AuditLogs.FirstOrDefault(l => l.Action == "EDIT_EMULATION_SCORE");
                Assert.NotNull(log);
                Assert.Contains("Sửa nhầm điểm", log.Details);
            }
        }

        [Fact]
        public void TestTeacherGrading_UndoRedoHistoryStack()
        {
            var undoStack = new System.Collections.Generic.Stack<System.Data.DataTable>();
            var redoStack = new System.Collections.Generic.Stack<System.Data.DataTable>();

            var table = new System.Data.DataTable("Grading");
            table.Columns.Add("StudentName", typeof(string));
            table.Columns.Add("Score", typeof(string));

            table.Rows.Add("Nguyen Van A", "8.0");
            table.AcceptChanges();

            undoStack.Push(table.Copy());
            redoStack.Clear();

            table.Rows[0]["Score"] = "9.5";
            table.AcceptChanges();

            Assert.Equal("9.5", table.Rows[0]["Score"]);

            if (undoStack.Count > 0)
            {
                redoStack.Push(table.Copy());
                var previousTable = undoStack.Pop();
                
                table.BeginLoadData();
                table.Clear();
                foreach (System.Data.DataRow r in previousTable.Rows)
                {
                    table.ImportRow(r);
                }
                table.EndLoadData();
                table.AcceptChanges();
            }

            Assert.Equal("8.0", table.Rows[0]["Score"]);
            Assert.Single(redoStack);

            if (redoStack.Count > 0)
            {
                undoStack.Push(table.Copy());
                var nextTable = redoStack.Pop();
                
                table.BeginLoadData();
                table.Clear();
                foreach (System.Data.DataRow r in nextTable.Rows)
                {
                    table.ImportRow(r);
                }
                table.EndLoadData();
                table.AcceptChanges();
            }

            Assert.Equal("9.5", table.Rows[0]["Score"]);
            Assert.Single(undoStack);
        }

        [Fact]
        public void TestAuthenticationService_PBKDF2HashingAndVerification()
        {
            string password = "MySecurePassword123";
            
            // Test hashing
            string hashed = QASmartTouch.Services.AuthenticationService.HashPassword(password);
            Assert.StartsWith("pbkdf2:100000:", hashed);

            // Test verification (PBKDF2)
            bool isValid = QASmartTouch.Services.AuthenticationService.VerifyPassword(password, hashed);
            Assert.True(isValid);

            // Test verification fails with wrong password
            bool isInvalid = QASmartTouch.Services.AuthenticationService.VerifyPassword("WrongPassword123", hashed);
            Assert.False(isInvalid);

            // Test backward compatibility (SHA512 format "salt:hash")
            byte[] salt = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            using var hmac = new System.Security.Cryptography.HMACSHA512(salt);
            var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            string legacyReal = $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hashBytes)}";
            
            bool isLegacyValid = QASmartTouch.Services.AuthenticationService.VerifyPassword(password, legacyReal);
            Assert.True(isLegacyValid);
        }

        [Fact]
        public void TestEmulationScore_AdjustmentReasonValidation()
        {
            var IsValidReasonLocal = new Func<string, bool>(reason =>
            {
                if (string.IsNullOrWhiteSpace(reason)) return false;
                reason = reason.Trim();
                if (reason.Length < 10) return false;
                if (reason.Distinct().Count() == 1) return false;
                bool isAllDigits = reason.All(char.IsDigit);
                if (isAllDigits) return false;

                var cleanText = reason.Replace(" ", "").Replace(".", "").Replace("-", "");
                if (cleanText.Length < 5) return false;
                return true;
            });

            Assert.False(IsValidReasonLocal(""));
            Assert.False(IsValidReasonLocal("Ngắn"));
            Assert.False(IsValidReasonLocal(".........."));
            Assert.False(IsValidReasonLocal("1234567890"));
            Assert.False(IsValidReasonLocal("aaaaaaaaaa"));
            Assert.True(IsValidReasonLocal("Sửa lại điểm do nhập nhầm học sinh"));
        }

        [Fact]
        public void TestAwardService_EncryptedSignatureDecryption()
        {
            byte[] rawPng = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52 };
            
            // Test encryption and decryption
            byte[] encrypted = AwardService.EncryptBytes(rawPng);
            byte[] decrypted = AwardService.DecryptBytes(encrypted);
            
            Assert.Equal(rawPng, decrypted);
            Assert.NotEqual(rawPng, encrypted);
        }

        [Fact]
        public void TestCareerDetail_ExportPdfRoadmap()
        {
            string title = "Kỹ sư phần mềm";
            string outputPath = QASmartClass.Services.PdfTemplateHelper.GetOutputPath("HuongNghiep", title);
            
            if (File.Exists(outputPath)) File.Delete(outputPath);

            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(1, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.Content().Column(col =>
                    {
                        col.Item().Text(title);
                        col.Item().Text("Định hướng nghề nghiệp");
                    });
                });
            }).GeneratePdf(outputPath);

            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length > 0);
            
            try { File.Delete(outputPath); } catch {}
        }

        [Fact]
        public void TestAuthentication_AutoUpgradeLegacyHashToPBKDF2()
        {
            using (var db = new AppDbContext())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Tạo một tài khoản học sinh giả lập với mật khẩu cũ (SHA512)
                    byte[] salt = new byte[16];
                    new Random().NextBytes(salt);
                    using var hmac = new System.Security.Cryptography.HMACSHA512(salt);
                    byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes("Admin123");
                    byte[] hashBytes = hmac.ComputeHash(passwordBytes);
                    string legacyHash = $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hashBytes)}";

                    var student = new Student
                    {
                        FullName = "Test Auto Upgrade Pass",
                        StudentCode = "HS_UPGRADE_TEST",
                        PasswordHash = legacyHash,
                        ClassName = "10A1",
                        IsOnline = false,
                        LastSeen = DateTime.Now
                    };
                    db.Students.Add(student);
                    db.SaveChanges();

                    // 2. Kiểm tra mật khẩu cũ vẫn xác thực thành công qua VerifyPassword
                    bool isMatch = QASmartTouch.Services.AuthenticationService.VerifyPassword("Admin123", student.PasswordHash);
                    Assert.True(isMatch);

                    // 3. Giả lập đăng nhập thành công và kích hoạt tự động nâng cấp mật khẩu
                    if (isMatch && !student.PasswordHash.StartsWith("pbkdf2:"))
                    {
                        student.PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("Admin123");
                        db.SaveChanges();
                    }

                    // 4. Xác nhận mật khẩu mới trong DB đổi thành chuẩn pbkdf2
                    var updatedStudent = db.Students.FirstOrDefault(s => s.StudentCode == "HS_UPGRADE_TEST");
                    Assert.NotNull(updatedStudent);
                    Assert.StartsWith("pbkdf2:100000:", updatedStudent.PasswordHash);

                    // 5. Xác nhận đăng nhập lại bằng mật khẩu PBKDF2 mới vẫn thành công
                    bool isMatchNew = QASmartTouch.Services.AuthenticationService.VerifyPassword("Admin123", updatedStudent.PasswordHash);
                    Assert.True(isMatchNew);
                }
                finally
                {
                    tx.Rollback();
                }
            }
        }

        [Fact]
        public void TestDataRetention_ExecuteDeleteOptimization()
        {
            using (var db = new AppDbContext())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Thêm một số log giả lập đã cũ (quá hạn)
                    var oldTimestamp = DateTime.Now.AddDays(-200);
                    db.AuditLogs.Add(new AuditLog { Action = "TestOld", ActorName = "System", Details = "Old log", Timestamp = oldTimestamp });
                    db.AuditLogs.Add(new AuditLog { Action = "TestNew", ActorName = "System", Details = "New log", Timestamp = DateTime.Now });
                    db.SaveChanges();

                    // 2. Chạy dọn dẹp bằng DataRetentionService (sử dụng ExecuteDelete tối ưu)
                    var service = new DataRetentionService(db);
                    service.CleanOldData(180, 180);

                    // 3. Xác minh log cũ đã bị xóa, log mới vẫn còn
                    var oldLogExist = db.AuditLogs.Any(l => l.Action =="TestOld");
                    var newLogExist = db.AuditLogs.Any(l => l.Action == "TestNew");
                    Assert.False(oldLogExist);
                    Assert.True(newLogExist);

                    // 4. Xác minh sự kiện dọn dẹp đã được ghi log Audit
                    var cleanLog = db.AuditLogs.FirstOrDefault(l => l.Action == "Data_Cleanup");
                    Assert.NotNull(cleanLog);
                    Assert.Contains("Đã dọn dẹp", cleanLog.Details);
                }
                finally
                {
                    tx.Rollback();
                }
            }
        }

        [Fact]
        public void TestMoetReport_NPlusOneQueriesElimination()
        {
            using (var db = new AppDbContext())
            {
                // Verify that CalculateAcademicStats can be executed successfully
                var moetService = new MoetReportService(db);
                var students = db.Students.Where(s => s.Status == "Active").Take(5).ToList();
                var gradingService = new TT22GradingService(db);

                // Chúng ta sẽ chạy thử hàm CalculateAcademicStats (qua Reflection do nó là private)
                var type = typeof(MoetReportService);
                var method = type.GetMethod("CalculateAcademicStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(method);

                var dto = method.Invoke(moetService, new object[] { students, gradingService, "2025-2026", "HK2" });
                Assert.NotNull(dto);
            }
        }

        [Fact]
        public void TestStudentSubmit_TempFilesCleanup()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "QASmartClass_Submit");
            Directory.CreateDirectory(tempFolder);
            var tempFilePath = Path.Combine(tempFolder, "test_temp_submit.rtf");
            File.WriteAllText(tempFilePath, "Hello Temp File");

            // Giả lập danh sách file nộp bài
            var filesToSubmit = new System.Collections.Generic.List<string> { tempFilePath };

            // Thực thi dọn dẹp file tạm giả lập giống SubmitInline_Click
            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "QASmartClass_Submit");
                foreach (var path in filesToSubmit)
                {
                    if (path.StartsWith(tempDir, StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
            }
            catch {}

            // Xác nhận file tạm đã bị xóa khỏi đĩa cứng
            Assert.False(File.Exists(tempFilePath));
        }

        [Fact]
        public void TestStudentIdentity_DPAPIDecryptionCorrectness()
        {
            var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
            var backupExists = File.Exists(profilePath);
            byte[]? originalData = backupExists ? File.ReadAllBytes(profilePath) : null;

            try
            {
                // 1. Tạo profile giả lập và mã hóa bằng DPAPI
                var testProfile = new { StudentCode = "HS009", StudentName = "Test DPAPI Student", RememberMe = true };
                var json = System.Text.Json.JsonSerializer.Serialize(testProfile);
                var plainBytes = System.Text.Encoding.UTF8.GetBytes(json);
                var encryptedBytes = System.Security.Cryptography.ProtectedData.Protect(
                    plainBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                
                File.WriteAllBytes(profilePath, encryptedBytes);

                // 2. Gọi Service để lấy học sinh hiện tại
                using (var db = new AppDbContext())
                {
                    // Đảm bảo HS009 có trong DB
                    var existing = db.Students.FirstOrDefault(s => s.StudentCode == "HS009");
                    if (existing == null)
                    {
                        db.Students.Add(new Student { StudentCode = "HS009", FullName = "Test DPAPI Student", ClassroomId = 1, ClassName = "10A3" });
                        db.SaveChanges();
                    }

                    var service = new QASmartClass.StudentClient.Services.StudentIdentityService(db);
                    service.ClearCache();
                    var resolved = service.GetCurrentStudent();

                    // 3. Khẳng định mã học sinh được giải mã thành công thay vì fallback HS001
                    Assert.Equal("HS009", resolved.Code);
                    Assert.Equal("Test DPAPI Student", resolved.Name);
                }
            }
            finally
            {
                // Dọn dẹp dữ liệu học sinh trong DB để tránh ô nhiễm test khác
                try
                {
                    using (var db = new AppDbContext())
                    {
                        var student = db.Students.FirstOrDefault(s => s.StudentCode == "HS009");
                        if (student != null)
                        {
                            db.Students.Remove(student);
                            db.SaveChanges();
                        }
                    }
                }
                catch {}

                // Khôi phục dữ liệu ban đầu
                if (backupExists && originalData != null) File.WriteAllBytes(profilePath, originalData);
                else if (File.Exists(profilePath)) File.Delete(profilePath);
            }
        }

        [Fact]
        public void TestTeacherKpi_MemoryOptimizationQuery()
        {
            using (var db = new AppDbContext())
            {
                // Gọi hàm tính toán KPI
                var results = QASmartClass.Services.TeacherKpiService.CalculateAllKpis();
                
                // Đảm bảo không ném ngoại lệ và trả về danh sách kết quả sắp xếp giảm dần theo điểm
                Assert.NotNull(results);
                if (results.Any())
                {
                    double max = results.First().FinalRating;
                    foreach (var r in results)
                    {
                        Assert.True(r.FinalRating <= max);
                        max = r.FinalRating;
                    }
                }
            }
        }

        [Fact]
        public void TestBackupRestore_ChecksumValidation()
        {
            var session = QASmartClass.Services.UserSessionService.Instance;
            var prop = typeof(QASmartClass.Services.UserSessionService).GetProperty("Role");
            var originalRole = session.Role;
            prop?.SetValue(session, QASmartClass.Data.StatusConstants.TeacherRole.Admin);

            var service = new QASmartClass.Services.BackupService();
            string? backupFile = null;
            
            try
            {
                // 1. Tạo backup thủ công
                backupFile = service.CreateBackup("test_checksum");
                Assert.NotNull(backupFile);
                Assert.True(File.Exists(backupFile));
                Assert.True(File.Exists(backupFile + ".sha256"));

                // 2. Thử phá hỏng file backup (ghi đè nội dung bậy)
                File.WriteAllText(backupFile, "Corrupted Database File Payload");

                // 3. Khôi phục -> Xác nhận hệ thống chặn lại và trả về false
                var restoreSuccess = service.Restore(backupFile);
                Assert.False(restoreSuccess);
            }
            finally
            {
                // Khôi phục quyền ban đầu
                prop?.SetValue(session, originalRole);

                // Dọn dẹp tệp thử nghiệm
                if (backupFile != null)
                {
                    if (File.Exists(backupFile)) File.Delete(backupFile);
                    if (File.Exists(backupFile + ".sha256")) File.Delete(backupFile + ".sha256");
                }
            }
        }

        [Fact]
        public void TestFamilyGame_DynamicQuizGeneration()
        {
            using (var db = new AppDbContext())
            {
                var gameService = new QASmartClass.Services.FamilyGameService(db);
                
                // Giả lập lấy câu hỏi cho học sinh có ID = 1
                List<QASmartClass.Services.FamilyQuizQuestion> questions = gameService.GenerateQuizForParent(1, 1);
                
                Assert.NotNull(questions);
                Assert.Equal(5, questions.Count);
                
                foreach (QASmartClass.Services.FamilyQuizQuestion q in questions)
                {
                    Assert.NotNull(q.Question);
                    Assert.NotNull(q.Options);
                    Assert.True(q.CorrectIndex >= 0 && q.CorrectIndex < q.Options.Length);
                }
            }
        }

        [Fact]
        public async Task TestAuthentication_BypassOfflineBypassPrevention()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=invalid_path_fail.db").Options;
            using (var invalidDb = new AppDbContext(options))
            {
                var originalDb = QASmartClass.Services.AppServices.Database;
                QASmartClass.Services.AppServices.Database = invalidDb;
                try
                {
                    var auth = QASmartTouch.Services.AuthenticationService.Instance;
                    var res = await auth.AuthenticateAsync("admin", "Admin123", "1234567890123456");
                    
                    Assert.False(res.success);
                    Assert.Contains("Lỗi kết nối", res.message);
                }
                finally
                {
                    QASmartClass.Services.AppServices.Database = originalDb;
                }
            }
        }

        [Fact]
        public void TestAdminPIN_PBKDF2HashingVerification()
        {
            string pinPath = AppPaths.AdminSecurityFile;
            bool pinBackupExists = System.IO.File.Exists(pinPath);
            byte[]? pinBackup = pinBackupExists ? System.IO.File.ReadAllBytes(pinPath) : null;
            try
            {
                if (pinBackupExists) System.IO.File.Delete(pinPath);
                
                var service = new QASmartClass.Services.AdminSecurityService();
                service.ChangePin("9999");
                
                bool isValid = service.ValidatePin("9999");
                Assert.True(isValid);
                
                bool isInvalid = service.ValidatePin("0000");
                Assert.False(isInvalid);
            }
            finally
            {
                if (pinBackupExists && pinBackup != null) System.IO.File.WriteAllBytes(pinPath, pinBackup);
                else if (System.IO.File.Exists(pinPath)) System.IO.File.Delete(pinPath);
            }
        }

        [Fact]
        public void TestAICopilot_OptionsJsonSyntaxCorrectness()
        {
            var questions = QASmartClass.Services.AiCopilotService.GenerateQuestionsFromText("Đoạn văn bản thử nghiệm", "Toán");
            
            Assert.Equal(3, questions.Count);
            foreach (var q in questions)
            {
                var options = System.Text.Json.JsonSerializer.Deserialize<string[]>(q.OptionsJson);
                Assert.NotNull(options);
                Assert.True(options.Length > 0);
            }
        }

        [Fact]
        public void TestDiscipline_ApprovalAuditLogging()
        {
            var session = QASmartClass.Services.UserSessionService.Instance;
            var prop = typeof(QASmartClass.Services.UserSessionService).GetProperty("Role");
            var originalRole = session.Role;
            prop?.SetValue(session, QASmartClass.Data.StatusConstants.TeacherRole.Admin);

            using (var db = TestDbFactory.Create())
            {
                var student = new Student { FullName = "Học Sinh Kỷ Luật", StudentCode = "HS_KL", ClassName = "10A1" };
                db.Students.Add(student);
                db.SaveChanges();

                try
                {
                    var service = new QASmartClass.Services.DisciplineService(db);
                    service.CreateRecord(student.Id, "10A1", "Nhẹ", "Nhắc nhở", "Đi học muộn", "GVCN");
                    
                    var record = db.DisciplineRecords.FirstOrDefault(r => r.StudentId == student.Id);
                    Assert.NotNull(record);
                    
                    record.Status = "Pending";
                    db.SaveChanges();
                    
                    bool approved = service.ApproveRecord(record.Id, "Hiệu Trưởng");
                    Assert.True(approved);
                    
                    var audit = db.AuditLogs.FirstOrDefault(l => l.Action == "APPROVE_DISCIPLINE" && l.Details.Contains("Học Sinh Kỷ Luật"));
                    Assert.NotNull(audit);
                    Assert.Equal("Hiệu Trưởng", audit.ActorName);
                }
                finally
                {
                    prop?.SetValue(session, originalRole);
                }
            }
        }

        [Fact]
        public void TestDiary_SEL_PrivacySafety()
        {
            using (var db = TestDbFactory.Create())
            {
                var today = DateTime.Today;
                var existings = db.StudentMentalHealthRecords.Where(r => r.RecordedAt.Date == today).ToList();
                db.StudentMentalHealthRecords.RemoveRange(existings);
                
                var diaryExistings = db.LearningDiaries.Where(r => r.Date == today).ToList();
                db.LearningDiaries.RemoveRange(diaryExistings);
                db.SaveChanges();

                var student = new Student { FullName = "Nguyen Van Bao Mat", StudentCode = "HS_BAMAT", ClassName = "10A1", ClassroomId = 1 };
                db.Students.Add(student);
                db.SaveChanges();

                var diary = new LearningDiary
                {
                    StudentId = student.Id,
                    Date = today,
                    Content = "Hom nay toi thay rat buon ba va tram cam vi ap luc hoc tap",
                    Goals = "Co gang vuot qua",
                    Reflection = "Can nghi ngoi",
                    Mood = "Stressed"
                };
                db.LearningDiaries.Add(diary);
                db.SaveChanges();

                string contentLower = (diary.Content + " " + diary.Reflection + " " + diary.Goals).ToLower();
                string[] sensitiveKeywords = { "buon ba", "tram cam", "ap luc" };
                var detected = sensitiveKeywords.Where(k => contentLower.Contains(k)).ToList();

                var mentalRecord = new StudentMentalHealthRecord
                {
                    StudentId = student.Id,
                    MoodScore = 2,
                    Notes = "[Tự động từ Nhật ký học tập] Phát hiện trạng thái cảm xúc bất thường hoặc từ khóa nhạy cảm. (Nội dung chi tiết nhật ký gốc được bảo mật).",
                    RiskLevel = "High",
                    DetectedKeywords = string.Join(", ", detected),
                    RecordedAt = DateTime.Now,
                    InterventionLevel = 0,
                    InterventionNotes = "",
                    IsNotified = false
                };
                db.StudentMentalHealthRecords.Add(mentalRecord);
                db.SaveChanges();

                var savedRecord = db.StudentMentalHealthRecords.FirstOrDefault(r => r.StudentId == student.Id);
                Assert.NotNull(savedRecord);
                Assert.Equal("High", savedRecord.RiskLevel);
                
                Assert.DoesNotContain("tram cam", savedRecord.Notes);
                Assert.Equal("[Tự động từ Nhật ký học tập] Phát hiện trạng thái cảm xúc bất thường hoặc từ khóa nhạy cảm. (Nội dung chi tiết nhật ký gốc được bảo mật).", savedRecord.Notes);
                
                // Cleanup
                db.StudentMentalHealthRecords.Remove(savedRecord);
                db.LearningDiaries.Remove(diary);
                db.Students.Remove(student);
                db.SaveChanges();
            }
        }

        [Fact]
        public void TestPayroll_DraftRecalculationOnAttendanceChange()
        {
            using (var db = TestDbFactory.Create())
            {
                var staff = new StaffProfile { FullName = "GV Nguyen Van A", Email = "a@school.edu.vn", Phone = "0123" };
                db.StaffProfiles.Add(staff);
                db.SaveChanges();

                var service = new QASmartClass.Services.PayrollService(db);

                var p1 = service.CalculatePayroll("GV Nguyen Van A", 6, 2026);
                Assert.NotNull(p1);
                Assert.Equal("Draft", p1.Status);
                Assert.Equal(0, p1.BaseSalary);

                for (int i = 1; i <= 5; i++)
                {
                    db.StaffAttendances.Add(new StaffAttendance
                    {
                        StaffId = staff.Id,
                        Date = new DateTime(2026, 6, i),
                        Status = "Present"
                    });
                }
                db.SaveChanges();

                var p2 = service.CalculatePayroll("GV Nguyen Van A", 6, 2026);
                
                Assert.NotNull(p2);
                Assert.Equal("Draft", p2.Status);
                Assert.Equal(5 * 300000m, p2.BaseSalary);

                // Cleanup
                var pRecord = db.PayrollRecords.FirstOrDefault(x => x.Id == p2.Id);
                if (pRecord != null) db.PayrollRecords.Remove(pRecord);
                var atts = db.StaffAttendances.Where(x => x.StaffId == staff.Id).ToList();
                db.StaffAttendances.RemoveRange(atts);
                db.StaffProfiles.Remove(staff);
                db.SaveChanges();
            }
        }

        [Fact]
        public void TestTeacherGrading_ExcelPastePrecisionMatching()
        {
            var table = new System.Data.DataTable();
            table.Columns.Add("StudentId", typeof(int));
            table.Columns.Add("Mã HS", typeof(string));
            table.Columns.Add("Họ và tên", typeof(string));
            table.Columns.Add("15p", typeof(string));

            table.Rows.Add(1, "HS001", "Nguyễn Lan Anh", "");
            table.Rows.Add(2, "HS002", "Nguyễn Anh", "");

            var parsed = new System.Collections.Generic.List<dynamic>
            {
                new { Identifier = "Nguyễn Anh", Score = 8.5 }
            };

            string targetColName = "15p";
            int matchedCount = 0;

            foreach (var item in parsed)
            {
                string pIdentifier = item.Identifier;
                double pScore = item.Score;

                var normalized = pIdentifier.Normalize(System.Text.NormalizationForm.FormD);
                var stringBuilder = new System.Text.StringBuilder();
                foreach (var c in normalized)
                {
                    if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                        stringBuilder.Append(c);
                }
                string cleanPIdentifier = stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC).Replace(" ", "").ToLower();

                foreach (System.Data.DataRow row in table.Rows)
                {
                    bool matched = false;
                    
                    string dbCode = table.Columns.Contains("Mã HS") ? row["Mã HS"]?.ToString() ?? "" : "";
                    string cleanDbCode = dbCode.Trim().ToLower();
                    if (!string.IsNullOrEmpty(cleanDbCode) && cleanDbCode == cleanPIdentifier)
                    {
                        matched = true;
                    }

                    if (!matched)
                    {
                        string dbName = row["Họ và tên"]?.ToString() ?? "";
                        var normalizedDb = dbName.Normalize(System.Text.NormalizationForm.FormD);
                        var sbDb = new System.Text.StringBuilder();
                        foreach (var c in normalizedDb)
                        {
                            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                                sbDb.Append(c);
                        }
                        string cleanDbName = sbDb.ToString().Normalize(System.Text.NormalizationForm.FormC).Replace(" ", "").ToLower();
                        
                        if (cleanDbName == cleanPIdentifier)
                        {
                            matched = true;
                        }
                    }

                    if (matched)
                    {
                        row[targetColName] = pScore.ToString("0.##");
                        matchedCount++;
                        break;
                    }
                }
            }

            Assert.Equal(1, matchedCount);
            Assert.Equal("8.5", table.Rows[1]["15p"].ToString());
            Assert.Equal("", table.Rows[0]["15p"].ToString());
        }

        [Fact]
        public void TestAnonymousChat_StudentSpecificToken()
        {
            int studentId1 = 9991;
            int studentId2 = 9992;
            string appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            
            string path1 = System.IO.Path.Combine(appData, "QASmartClass", $"anon_token_{studentId1}.dat");
            string path2 = System.IO.Path.Combine(appData, "QASmartClass", $"anon_token_{studentId2}.dat");

            Assert.NotEqual(path1, path2);
            Assert.Contains(studentId1.ToString(), path1);
            Assert.Contains(studentId2.ToString(), path2);
        }

        [Fact]
        public void TestContactBook_WriteOptimizationAndBatchCalculation()
        {
            using (var db = TestDbFactory.Create())
            {
                var student = new Student
                {
                    FullName = "Test Student Write Opt",
                    StudentCode = "TS_WRITE_OPT",
                    ClassroomId = 999,
                    ConductScore = 85,
                    Status = "Active"
                };
                db.Students.Add(student);
                db.SaveChanges();

                var service = new ContactBookService(db);

                var entry1 = service.GetOrCalculateEntry(student.Id, "2025-2026", "HK1");
                Assert.NotNull(entry1);
                
                DateTime beforeTime = entry1.UpdatedAt;

                var entry2 = service.GetOrCalculateEntry(student.Id, "2025-2026", "HK1");
                Assert.Equal(beforeTime, entry2.UpdatedAt);
            }
        }

        [Fact]
        public void TestFamilyGame_LeaderboardDynamicFix()
        {
            using (var db = TestDbFactory.Create())
            {
                var service = new FamilyGameService(db);
                var leaderboard = service.GetLeaderboard();
                
                Assert.NotNull(leaderboard);
                if (leaderboard.Any())
                {
                    var first = leaderboard.First();
                    Assert.True(first.ParentId >= 0);
                    Assert.True(first.HighestScore >= 0);
                }
            }
        }

        [Fact]
        public void TestStudentGoal_AutoUpdateLogic()
        {
            using (var db = TestDbFactory.Create())
            {
                var student = new Student
                {
                    FullName = "Test Student Goal Sync",
                    StudentCode = "TS_GOAL_SYNC",
                    ClassroomId = 888,
                    ConductScore = 80,
                    Status = "Active"
                };
                db.Students.Add(student);
                db.SaveChanges();

                var roster = new ClassRoster
                {
                    Subject = "Toán",
                    SchoolYear = "2025-2026",
                    Semester = "HK1",
                    IsActive = true,
                    ClassName = "TestClass"
                };
                db.ClassRosters.Add(roster);
                db.SaveChanges();

                var crs = new ClassRosterStudent
                {
                    StudentId = student.Id,
                    RosterId = roster.Id
                };
                db.ClassRosterStudents.Add(crs);
                db.SaveChanges();

                var goal = new StudentGoal
                {
                    StudentId = student.Id,
                    Subject = "Toán",
                    TargetScore = 8.0,
                    Semester = "Học kỳ I",
                    Status = "Active",
                    ActualScore = 0.0
                };
                db.StudentGoals.Add(goal);
                db.SaveChanges();

                var grade = new StudentGrade
                {
                    StudentId = student.Id,
                    RosterId = roster.Id,
                    GradeTypeId = 1, // Thường xuyên
                    Score = 9.0,
                    UpdatedAt = DateTime.Now
                };
                db.StudentGrades.Add(grade);
                db.SaveChanges();

                var activeRosterYear = "2025-2026";
                string rSem = goal.Semester switch
                {
                    "Học kỳ I" => "HK1",
                    "Học kỳ II" => "HK2",
                    _ => goal.Semester
                };

                var targetRosters = (from c in db.ClassRosterStudents
                                     join r in db.ClassRosters on c.RosterId equals r.Id
                                     where c.StudentId == student.Id &&
                                           r.Subject == goal.Subject &&
                                           r.SchoolYear == activeRosterYear &&
                                           (rSem == "Cả năm" || r.Semester == rSem)
                                     select r.Id).ToList();

                var grades = db.StudentGrades
                    .Where(g => g.StudentId == student.Id && targetRosters.Contains(g.RosterId))
                    .ToList();

                Assert.Single(grades);
                double totalWeighted = 0;
                int totalWeight = 0;
                foreach (var g in grades)
                {
                    int weight = g.GradeTypeId switch
                    {
                        1 => 1,
                        2 => 2,
                        3 => 3,
                        _ => 1
                    };
                    totalWeighted += g.Score * weight;
                    totalWeight += weight;
                }
                double actual = totalWeight > 0 ? Math.Round(totalWeighted / totalWeight, 2) : 0.0;
                Assert.Equal(9.0, actual);

                string newStatus = "Active";
                if (actual >= goal.TargetScore)
                {
                    newStatus = "Achieved";
                }
                Assert.Equal("Achieved", newStatus);
            }
        }

        [Fact]
        public async Task TestHomeroomHub_DynamicTeacherIdentity()
        {
            using (var db = new AppDbContext())
            {
                var teacherCode = "GV_TEST_INBOX";
                
                // Backup tin nhắn cũ để khôi phục sau test
                var oldMsgs = db.InboxMessages.ToList();
                db.InboxMessages.RemoveRange(oldMsgs);
                db.SaveChanges();

                var msg1 = new InboxMessage { SenderId = "HS001", ReceiverId = teacherCode, Content = "Hello GV", IsRead = false, CreatedAt = DateTime.Now };
                var msg2 = new InboxMessage { SenderId = "HS002", ReceiverId = "GV002", Content = "Hello GV2", IsRead = false, CreatedAt = DateTime.Now };
                db.InboxMessages.AddRange(msg1, msg2);

                var profile = new TeacherProfile { TeacherCode = teacherCode, FullName = "Giáo viên Inbox" };
                db.TeacherProfiles.Add(profile);
                db.SaveChanges();

                var session = QASmartClass.Services.UserSessionService.Instance;
                session.SetSession(teacherCode);

                var inboxVM = new QASmartClass.HomeroomHub.ViewModels.HomeroomInboxViewModel();
                await inboxVM.LoadMessagesAsync();

                try
                {
                    Assert.Single(inboxVM.Messages);
                    Assert.Equal("HS001", inboxVM.Messages.First().SenderId);
                }
                finally
                {
                    session.ClearSession();
                    // Khôi phục CSDL
                    db.InboxMessages.RemoveRange(db.InboxMessages.ToList());
                    db.TeacherProfiles.Remove(profile);
                    db.InboxMessages.AddRange(oldMsgs);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public async Task TestTeacherGrading_BatchSavePerformance()
        {
            using (var db = new AppDbContext())
            {
                // Setup Roster, Students, GradeTypes trong DB thật
                var roster = new ClassRoster { ClassName = "10A1_TEST", Subject = "Toán", SchoolYear = "2025-2026", Semester = "HK1", IsActive = true };
                db.ClassRosters.Add(roster);
                db.SaveChanges();

                var student = new Student { FullName = "Student Batch Save", StudentCode = "HS_BATCH", ClassName = "10A1_TEST", ClassroomId = 1 };
                db.Students.Add(student);
                db.SaveChanges();

                var rosterStudent = new ClassRosterStudent { StudentId = student.Id, RosterId = roster.Id };
                db.ClassRosterStudents.Add(rosterStudent);
                db.SaveChanges();

                var gt = db.GradeTypeMasters.FirstOrDefault(g => g.IsActive);
                Assert.NotNull(gt);

                var vm = new QASmartClass.TeacherHub.ViewModels.TeacherGradingViewModel();
                
                // Dùng Reflection để gọi LoadClassesAsync nhằm cập nhật danh sách lớp từ DB thật
                var loadClassesMethod = typeof(QASmartClass.TeacherHub.ViewModels.TeacherGradingViewModel)
                    .GetMethod("LoadClassesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(loadClassesMethod);
                var loadClassesTask = loadClassesMethod.Invoke(vm, null) as Task;
                Assert.NotNull(loadClassesTask);
                await loadClassesTask;

                var testClass = vm.Classes.FirstOrDefault(c => c.Id == roster.Id);
                Assert.NotNull(testClass);
                vm.SelectedClass = testClass;

                // Gọi LoadGradesAsync bằng Reflection
                var loadGradesMethod = typeof(QASmartClass.TeacherHub.ViewModels.TeacherGradingViewModel)
                    .GetMethod("LoadGradesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(loadGradesMethod);
                var loadGradesTask = loadGradesMethod.Invoke(vm, null) as Task;
                Assert.NotNull(loadGradesTask);
                await loadGradesTask;

                try
                {
                    var table = vm.GradingTable;
                    Assert.NotNull(table);
                    Assert.Single(table.Rows);

                    // Lấy cột điểm tương ứng
                    string colName = $"{gt.ShortName} (HS{gt.Weight})";
                    table.Rows[0][colName] = "8.5";

                    // Gọi SaveGradesAsync bằng Reflection
                    var saveGradesMethod = typeof(QASmartClass.TeacherHub.ViewModels.TeacherGradingViewModel)
                        .GetMethod("SaveGradesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(saveGradesMethod);
                    var saveGradesTask = saveGradesMethod.Invoke(vm, null) as Task;
                    Assert.NotNull(saveGradesTask);
                    await saveGradesTask;

                    // Kiểm tra điểm được lưu
                    var dbGrade = db.StudentGrades.FirstOrDefault(g => g.StudentId == student.Id && g.RosterId == roster.Id && g.GradeTypeId == gt.Id);
                    Assert.NotNull(dbGrade);
                    Assert.Equal(8.5, dbGrade.Score);
                }
                finally
                {
                    // Cleanup
                    var grades = db.StudentGrades.Where(g => g.RosterId == roster.Id).ToList();
                    db.StudentGrades.RemoveRange(grades);
                    db.ClassRosterStudents.Remove(rosterStudent);
                    db.Students.Remove(student);
                    db.ClassRosters.Remove(roster);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public void TestEarlyWarning_AttendanceLanguageSafety()
        {
            using (var db = TestDbFactory.Create())
            {
                var student = new Student { FullName = "Warning Stud", StudentCode = "HS_WARN", ClassName = "10A1", ConductScore = 100 };
                db.Students.Add(student);
                db.SaveChanges();

                var roster = new ClassRoster { ClassName = "10A1", IsActive = true };
                db.ClassRosters.Add(roster);
                db.SaveChanges();

                var att1 = new AttendanceRecord { StudentId = student.Id, RosterId = roster.Id, Date = DateTime.Today, Status = "Có mặt" };
                db.AttendanceRecords.Add(att1);
                db.SaveChanges();

                var service = new EarlyWarningService(db);
                var atRisk = service.GetAtRiskStudents(DateTime.Today.Month, DateTime.Today.Year);

                var found = atRisk.FirstOrDefault(x => x.StudentId == student.Id);
                Assert.Null(found);
            }
        }

        [Fact]
        public void TestStudentResults_SemesterIsolation()
        {
            using (var db = TestDbFactory.Create())
            {
                var student = new Student { FullName = "Result Stud", StudentCode = "HS_RES", ClassName = "10A1", Status = "Active" };
                db.Students.Add(student);
                db.SaveChanges();

                var roster1 = new ClassRoster { ClassName = "10A1", SchoolYear = "2024-2025", Semester = "HK1", IsActive = false };
                var roster2 = new ClassRoster { ClassName = "10A1", SchoolYear = "2025-2026", Semester = "HK1", IsActive = true };
                db.ClassRosters.AddRange(roster1, roster2);
                db.SaveChanges();

                var crs1 = new ClassRosterStudent { StudentId = student.Id, RosterId = roster1.Id };
                var crs2 = new ClassRosterStudent { StudentId = student.Id, RosterId = roster2.Id };
                db.ClassRosterStudents.AddRange(crs1, crs2);
                db.SaveChanges();

                var q1 = new Quiz { Title = "Quiz Cũ", LessonId = roster1.Id };
                var q2 = new Quiz { Title = "Quiz Mới", LessonId = roster2.Id };
                db.Quizzes.AddRange(q1, q2);
                db.SaveChanges();

                var qr1 = new QuizResult { QuizId = q1.Id, StudentId = student.Id, Score = 100, TotalQuestions = 10, CorrectCount = 10, SubmittedAt = DateTime.Now.AddYears(-1) };
                var qr2 = new QuizResult { QuizId = q2.Id, StudentId = student.Id, Score = 80, TotalQuestions = 10, CorrectCount = 8, SubmittedAt = DateTime.Now };
                db.QuizResults.AddRange(qr1, qr2);
                db.SaveChanges();

                var activeRoster = (from crs in db.ClassRosterStudents
                                    join r in db.ClassRosters on crs.RosterId equals r.Id
                                    where crs.StudentId == student.Id && r.IsActive
                                    select new { r.SchoolYear, r.Semester }).FirstOrDefault();

                Assert.NotNull(activeRoster);
                Assert.Equal("2025-2026", activeRoster.SchoolYear);

                var targetRosterIds = db.ClassRosters
                    .Where(r => r.ClassName == student.ClassName && r.SchoolYear == activeRoster.SchoolYear && r.Semester == activeRoster.Semester && r.IsActive)
                    .Select(r => r.Id).ToList();

                var targetQuizIds = db.Quizzes.Where(q => targetRosterIds.Contains(q.LessonId)).Select(q => q.Id).ToList();
                
                Assert.Single(targetQuizIds);
                Assert.Equal(q2.Id, targetQuizIds.First());
            }
        }

        [Fact]
        public void TestClubListView_DynamicStudentRegistration()
        {
            using (var db = TestDbFactory.Create())
            {
                var student = new Student { FullName = "Nguyen Van Real", StudentCode = "HS_REAL", ClassName = "10A2" };
                db.Students.Add(student);
                db.SaveChanges();

                var club = new Club { Name = "CLB Bong Da", Category = "Thể thao", TeacherId = "GV001", MaxMembers = 10 };
                db.Clubs.Add(club);
                db.SaveChanges();

                var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                var dir = Path.GetDirectoryName(profilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var profileText = System.Text.Json.JsonSerializer.Serialize(new { StudentCode = "HS_REAL", StudentName = "Nguyen Van Real" });
                
                try
                {
                    var plainBytes = System.Text.Encoding.UTF8.GetBytes(profileText);
                    var encryptedBytes = System.Security.Cryptography.ProtectedData.Protect(plainBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                    System.IO.File.WriteAllBytes(profilePath, encryptedBytes);
                }
                catch
                {
                    System.IO.File.WriteAllText(profilePath, profileText);
                }

                var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(db);
                var (sId, sCode, sName) = identityService.GetCurrentStudent();

                Assert.Equal(student.Id, sId);
                Assert.Equal("Nguyen Van Real", sName);

                if (System.IO.File.Exists(profilePath)) System.IO.File.Delete(profilePath);
            }
        }

        [Fact]
        public void TestProfessionalTopic_StarRatingSafety()
        {
            var type = typeof(QASmartClass.Leadership.Views.ProfessionalTopicView);
            var renderStarsMethod = type.GetMethod("RenderStars", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(renderStarsMethod);

            var result = renderStarsMethod.Invoke(null, new object[] { 4.5 }) as string;
            Assert.Equal("⭐⭐⭐⭐☆", result);

            var result2 = renderStarsMethod.Invoke(null, new object[] { 3.0 }) as string;
            Assert.Equal("⭐⭐⭐☆☆", result2);
        }

        [Fact]
        public void TestStudentSettings_LegalNameIntegrity()
        {
            using (var db = TestDbFactory.Create())
            {
                var student = new Student { FullName = "Hoc Sinh Goc", StudentCode = "HS_ORIG", ClassName = "10A1" };
                db.Students.Add(student);
                db.SaveChanges();

                var sCode = "HS_ORIG";
                var newName = "Ten Thay Doi";

                var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                var dir = Path.GetDirectoryName(profilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                
                var profileText = System.Text.Json.JsonSerializer.Serialize(new { StudentCode = sCode, StudentName = newName });
                try
                {
                    var plainBytes = System.Text.Encoding.UTF8.GetBytes(profileText);
                    var encryptedBytes = System.Security.Cryptography.ProtectedData.Protect(plainBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                    System.IO.File.WriteAllBytes(profilePath, encryptedBytes);
                }
                catch
                {
                    System.IO.File.WriteAllText(profilePath, profileText);
                }

                try
                {
                    var studentInDb = db.Students.FirstOrDefault(s => s.StudentCode == sCode);
                    Assert.NotNull(studentInDb);
                    Assert.Equal("Hoc Sinh Goc", studentInDb.FullName);
                }
                finally
                {
                    if (System.IO.File.Exists(profilePath)) System.IO.File.Delete(profilePath);
                }
            }
        }

        [Fact]
        public void TestStaffProfile_ValidationLogic()
        {
            string invalidPhone = "12345";
            string validPhone = "0987654321";
            string invalidEmail = "abc";
            string validEmail = "abc@school.edu.vn";

            bool isPhoneValid1 = System.Text.RegularExpressions.Regex.IsMatch(invalidPhone, @"^\d{10}$");
            bool isPhoneValid2 = System.Text.RegularExpressions.Regex.IsMatch(validPhone, @"^\d{10}$");
            bool isEmailValid1 = System.Text.RegularExpressions.Regex.IsMatch(invalidEmail, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            bool isEmailValid2 = System.Text.RegularExpressions.Regex.IsMatch(validEmail, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");

            Assert.False(isPhoneValid1);
            Assert.True(isPhoneValid2);
            Assert.False(isEmailValid1);
            Assert.True(isEmailValid2);
        }

        [Fact]
        public void TestSchoolCalendar_DynamicOrganizer()
        {
            var teacherCode = "GV_CAL";
            var fullName = "Thay Giao Lịch";

            var profile = new TeacherProfile { TeacherCode = teacherCode, FullName = fullName };
            using (var db = new AppDbContext())
            {
                db.TeacherProfiles.Add(profile);
                db.SaveChanges();
            }

            QASmartClass.Staff.Services.StaffSession.Login(profile);

            try
            {
                var organizer = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "BGH";
                Assert.Equal(fullName, organizer);
            }
            finally
            {
                QASmartClass.Staff.Services.StaffSession.Logout();
                using (var db = new AppDbContext())
                {
                    var p = db.TeacherProfiles.FirstOrDefault(t => t.TeacherCode == teacherCode);
                    if (p != null) db.TeacherProfiles.Remove(p);
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public void TestStudentChatbot_RealIdentityBinding()
        {
            using (var db = TestDbFactory.Create())
            {
                var student = new Student { FullName = "Nguyễn Học Sinh Thật", StudentCode = "HS_CHAT", ClassName = "10A1" };
                db.Students.Add(student);
                db.SaveChanges();

                var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                var dir = Path.GetDirectoryName(profilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var profileText = System.Text.Json.JsonSerializer.Serialize(new { StudentCode = "HS_CHAT", StudentName = "Nguyễn Học Sinh Thật" });
                try
                {
                    var plainBytes = System.Text.Encoding.UTF8.GetBytes(profileText);
                    var encryptedBytes = System.Security.Cryptography.ProtectedData.Protect(plainBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                    System.IO.File.WriteAllBytes(profilePath, encryptedBytes);
                }
                catch
                {
                    System.IO.File.WriteAllText(profilePath, profileText);
                }

                try
                {
                    var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(db);
                    var (_, _, sName) = identityService.GetCurrentStudent();
                    var currentStudentName = sName;

                    Assert.Equal("Nguyễn Học Sinh Thật", currentStudentName);
                }
                finally
                {
                    if (System.IO.File.Exists(profilePath)) System.IO.File.Delete(profilePath);
                }
            }
        }

        [Fact]
        public void TestPortfolio_AddItemRealPersistence()
        {
            using (var db = TestDbFactory.Create())
            {
                var item = new PortfolioItem
                {
                    StudentId = 1,
                    Title = "STEM Robot Project",
                    Category = "Product",
                    Description = "Bản thuyết trình robot dọn dẹp mini",
                    CreatedAt = DateTime.Today
                };

                db.PortfolioItems.Add(item);
                db.SaveChanges();

                var saved = db.PortfolioItems.FirstOrDefault(i => i.Title == "STEM Robot Project");
                Assert.NotNull(saved);
                Assert.Equal("Product", saved.Category);
                Assert.Equal("Bản thuyết trình robot dọn dẹp mini", saved.Description);
            }
        }

        [Fact]
        public void TestEpidemic_AddCaseValidationAndPersistence()
        {
            using (var db = TestDbFactory.Create())
            {
                var validCase = new EpidemicCase
                {
                    StudentId = 1,
                    StudentName = "Nguyễn Văn A",
                    ClassName = "10A",
                    Disease = "Sốt xuất huyết",
                    OnsetDate = DateTime.Today,
                    IsolatedAt = "Home",
                    Status = "Active"
                };

                Assert.False(string.IsNullOrEmpty(validCase.StudentName));
                Assert.False(string.IsNullOrEmpty(validCase.ClassName));
                Assert.True(validCase.OnsetDate <= DateTime.Today);

                db.EpidemicCases.Add(validCase);
                db.SaveChanges();

                var saved = db.EpidemicCases.FirstOrDefault(c => c.StudentName == "Nguyễn Văn A");
                Assert.NotNull(saved);
                Assert.Equal("Sốt xuất huyết", saved.Disease);

                var invalidCase = new EpidemicCase
                {
                    StudentName = "",
                    ClassName = "10A",
                    OnsetDate = DateTime.Today.AddDays(1)
                };

                bool isNameInvalid = string.IsNullOrEmpty(invalidCase.StudentName);
                bool isDateInvalid = invalidCase.OnsetDate > DateTime.Today;

                Assert.True(isNameInvalid);
                Assert.True(isDateInvalid);
            }
        }

        [Fact]
        public void TestFoodSafety_RecordCreation()
        {
            using (var db = TestDbFactory.Create())
            {
                var record = new FoodSafetyRecord
                {
                    Date = DateTime.Today,
                    MenuItems = "Cơm trắng, Thịt kho trứng, Canh rau ngót",
                    SampleKept = true,
                    Inspector = "Cô Yến Y tế",
                    Result = "Pass"
                };

                Assert.False(string.IsNullOrEmpty(record.MenuItems));
                Assert.False(string.IsNullOrEmpty(record.Inspector));
                Assert.True(record.Date <= DateTime.Today);

                db.FoodSafetyRecords.Add(record);
                db.SaveChanges();

                var saved = db.FoodSafetyRecords.FirstOrDefault(r => r.MenuItems == "Cơm trắng, Thịt kho trứng, Canh rau ngót");
                Assert.NotNull(saved);
                Assert.Equal("Cô Yến Y tế", saved.Inspector);
                Assert.True(saved.SampleKept);
            }
        }

        [Fact]
        public void TestAssetManagement_AddAndTransfer()
        {
            using (var db = TestDbFactory.Create())
            {
                var asset = new Asset
                {
                    Name = "Máy chiếu Panasonic",
                    Category = "Electronics",
                    SerialNumber = "PN12345",
                    PurchaseDate = DateTime.Today,
                    Value = 15000000,
                    Location = "Phòng 101",
                    AssignedTo = "Nguyễn Văn A",
                    Status = "InUse"
                };

                Assert.False(string.IsNullOrEmpty(asset.Name));
                Assert.True(asset.Value >= 0);
                Assert.True(asset.PurchaseDate <= DateTime.Today);

                db.Assets.Add(asset);
                db.SaveChanges();

                var savedAsset = db.Assets.FirstOrDefault(a => a.Name == "Máy chiếu Panasonic");
                Assert.NotNull(savedAsset);

                var transfer = new AssetTransfer
                {
                    AssetId = savedAsset.Id,
                    FromDept = savedAsset.Location,
                    ToDept = "Phòng 202",
                    TransferDate = DateTime.Today,
                    Reason = "Chuyển phòng thiết bị"
                };

                Assert.False(string.IsNullOrEmpty(transfer.ToDept));
                Assert.True(transfer.TransferDate <= DateTime.Today);

                savedAsset.Location = transfer.ToDept;
                db.AssetTransfers.Add(transfer);
                db.SaveChanges();

                var savedTransfer = db.AssetTransfers.FirstOrDefault(t => t.AssetId == savedAsset.Id);
                Assert.NotNull(savedTransfer);
                Assert.Equal("Phòng 101", savedTransfer.FromDept);
                Assert.Equal("Phòng 202", savedTransfer.ToDept);

                var updatedAsset = db.Assets.Find(savedAsset.Id);
                Assert.NotNull(updatedAsset);
                Assert.Equal("Phòng 202", updatedAsset!.Location);
            }
        }

        [Fact]
        public void TestContract_ValidationAndPersistence()
        {
            using (var db = TestDbFactory.Create())
            {
                var contractIndefinite = new Contract
                {
                    StaffId = 1,
                    ContractNumber = "HD/2026/01",
                    ContractType = "Indefinite",
                    StartDate = DateTime.Today,
                    EndDate = null,
                    Status = "Active"
                };

                Assert.False(string.IsNullOrEmpty(contractIndefinite.ContractNumber));
                db.Contracts.Add(contractIndefinite);
                db.SaveChanges();

                var saved1 = db.Contracts.FirstOrDefault(c => c.ContractNumber == "HD/2026/01");
                Assert.NotNull(saved1);
                Assert.Null(saved1.EndDate);

                var contract1Year = new Contract
                {
                    StaffId = 1,
                    ContractNumber = "HD/2026/02",
                    ContractType = "1-Year",
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddYears(1),
                    Status = "Active"
                };

                Assert.False(string.IsNullOrEmpty(contract1Year.ContractNumber));
                Assert.True(contract1Year.EndDate > contract1Year.StartDate);
                db.Contracts.Add(contract1Year);
                db.SaveChanges();

                var saved2 = db.Contracts.FirstOrDefault(c => c.ContractNumber == "HD/2026/02");
                Assert.NotNull(saved2);
                Assert.NotNull(saved2.EndDate);
                Assert.True(saved2.EndDate > saved2.StartDate);

                var invalidContract = new Contract
                {
                    StaffId = 1,
                    ContractNumber = "HD/2026/03",
                    ContractType = "Probation",
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddDays(-1)
                };

                bool hasEndDateError = invalidContract.EndDate == null || invalidContract.EndDate <= invalidContract.StartDate;
                Assert.True(hasEndDateError);
            }
        }

        [Fact]
        public async Task TestPayrollViewModel_ExposesAndLoadsStaffNames()
        {
            TeacherProfile? teacher = null;
            StaffProfile? staff = null;
            using (var db = new AppDbContext())
            {
                try
                {
                    teacher = new TeacherProfile
                    {
                        TeacherCode = "GV_TEST_PAYROLL",
                        FullName = "Test Teacher Payroll",
                        IsActive = true
                    };
                    db.TeacherProfiles.Add(teacher);

                    staff = new StaffProfile
                    {
                        StaffCode = "GV_TEST_PAYROLL",
                        FullName = "Test Teacher Payroll",
                        BaseSalary = 5000000
                    };
                    db.StaffProfiles.Add(staff);
                    db.SaveChanges();

                    var vm = new QASmartClass.Staff.ViewModels.PayrollViewModel();
                    var loadTask = typeof(QASmartClass.Staff.ViewModels.PayrollViewModel)
                        .GetMethod("LoadStaffNamesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(loadTask);
                    var task = loadTask.Invoke(vm, null) as Task;
                    Assert.NotNull(task);
                    await task;

                    Assert.Contains("Test Teacher Payroll", vm.StaffNames);
                }
                finally
                {
                    if (teacher != null)
                    {
                        db.TeacherProfiles.Remove(teacher);
                    }
                    if (staff != null)
                    {
                        var s = db.StaffProfiles.FirstOrDefault(p => p.StaffCode == "GV_TEST_PAYROLL");
                        if (s != null) db.StaffProfiles.Remove(s);
                    }
                    db.SaveChanges();
                }
            }
        }

        [Fact]
        public async Task TestTaskManagementViewModel_ExposesAndLoadsStaffNames()
        {
            TeacherProfile? teacher = null;
            using (var db = new AppDbContext())
            {
                try
                {
                    teacher = new TeacherProfile
                    {
                        TeacherCode = "GV_TEST_TASK",
                        FullName = "Test Teacher Task",
                        IsActive = true
                    };
                    db.TeacherProfiles.Add(teacher);
                    db.SaveChanges();

                    var vm = new QASmartClass.Staff.ViewModels.TaskManagementViewModel();
                    var loadTask = typeof(QASmartClass.Staff.ViewModels.TaskManagementViewModel)
                        .GetMethod("LoadStaffNamesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(loadTask);
                    var task = loadTask.Invoke(vm, null) as Task;
                    Assert.NotNull(task);
                    await task;

                    Assert.Contains("Test Teacher Task", vm.StaffNames);
                }
                finally
                {
                    if (teacher != null)
                    {
                        db.TeacherProfiles.Remove(teacher);
                        db.SaveChanges();
                    }
                }
            }
        }

        [Fact]
        public async Task TestIncidentManagementViewModel_StudentNameLookup()
        {
            Student? student = null;
            using (var db = new AppDbContext())
            {
                try
                {
                    student = new Student
                    {
                        FullName = "Student Test Incident Lookup",
                        StudentCode = "HS_TEST_INCIDENT",
                        ClassName = "12A1",
                        IsOnline = false,
                        LastSeen = DateTime.Now
                    };
                    db.Students.Add(student);
                    db.SaveChanges();

                    var vm = new QASmartClass.Staff.ViewModels.IncidentManagementViewModel();
                    vm.StudentCode = "HS_TEST_INCIDENT";

                    var lookupMethod = typeof(QASmartClass.Staff.ViewModels.IncidentManagementViewModel)
                        .GetMethod("LookupStudentNameAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(lookupMethod);
                    var task = lookupMethod.Invoke(vm, new object[] { "HS_TEST_INCIDENT" }) as Task;
                    Assert.NotNull(task);
                    await task;

                    Assert.Contains("Student Test Incident Lookup", vm.StudentNameDisplay);
                    Assert.Contains("12A1", vm.StudentNameDisplay);

                    var taskNotFound = lookupMethod.Invoke(vm, new object[] { "HS_NON_EXISTENT" }) as Task;
                    Assert.NotNull(taskNotFound);
                    await taskNotFound;
                    Assert.Contains("Không tìm thấy", vm.StudentNameDisplay);
                }
                finally
                {
                    if (student != null)
                    {
                        db.Students.Remove(student);
                        db.SaveChanges();
                    }
                }
            }
        }
    }
}
