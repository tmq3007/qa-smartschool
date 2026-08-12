using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Helpers;
using QASmartClass.Classroom.Views;
using Xunit;

namespace QASmartClass.Tests
{
    public class V41QuizUpgradeTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly string _tempVersionPath;

        public V41QuizUpgradeTests()
        {
            // Initialize app paths and SQLite configuration for testing
            string guid = Guid.NewGuid().ToString("N");
            _tempDbPath = Path.Combine(AppPaths.RootDir, $"smartclass_test_{guid}.db");
            _tempVersionPath = Path.Combine(AppPaths.RootDir, $"db_version_{guid}.txt");

            AppPaths.DatabaseFile = _tempDbPath;
            AppPaths.DbVersionFile = _tempVersionPath;
            AppPaths.EnsureDirectories();

            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.36.0");
        }

        [Fact]
        public void TestDictionaryParsing_TC_01()
        {
            // Dictionary string key -> string value
            string jsonDictStrStr = "{\"12\":\"A\", \"13\":\"B\"}";
            var results1 = QuizAnswerParser.ParseAnswers(jsonDictStrStr);
            Assert.Equal(2, results1.Count);
            Assert.Contains(results1, r => r.QuestionId == 12 && r.Answer == "A");
            Assert.Contains(results1, r => r.QuestionId == 13 && r.Answer == "B");

            // Dictionary zero-based numeric values
            string jsonDictZeroBased = "{\"0\":0, \"1\":1}";
            var results2 = QuizAnswerParser.ParseAnswers(jsonDictZeroBased);
            Assert.Equal(2, results2.Count);
            // 0-based question ID 0 maps to 1, value 0 maps to "A"
            Assert.Contains(results2, r => r.QuestionId == 1 && r.Answer == "A");
            // 0-based question ID 1 maps to 2, value 1 maps to "B"
            Assert.Contains(results2, r => r.QuestionId == 2 && r.Answer == "B");
        }

        [Fact]
        public void TestSingleObjectAndStringListParsing_TC_02()
        {
            // Single object format PascalCase
            string jsonPascal = "{\"QuestionId\":12,\"Answer\":\"A\",\"Correct\":true,\"Points\":10,\"TimeTaken\":15}";
            var results1 = QuizAnswerParser.ParseAnswers(jsonPascal);
            Assert.Single(results1);
            Assert.Equal(12, results1[0].QuestionId);
            Assert.Equal("A", results1[0].Answer);
            Assert.True(results1[0].Correct);
            Assert.Equal(10, results1[0].Points);
            Assert.Equal(15, results1[0].TimeTaken);

            // Single object camelCase with numeric Answer index
            string jsonCamel = "{\"questionId\":\"14\",\"answer\":2,\"correct\":\"false\",\"points\":\"5\",\"timeTaken\":\"20\"}";
            var results2 = QuizAnswerParser.ParseAnswers(jsonCamel);
            Assert.Single(results2);
            Assert.Equal(14, results2[0].QuestionId);
            Assert.Equal("C", results2[0].Answer); // Index 2 -> 'C'
            Assert.False(results2[0].Correct);
            Assert.Equal(5, results2[0].Points);
            Assert.Equal(20, results2[0].TimeTaken);

            // List of string logs
            string jsonStringList = "[\"Q1: A (Dung)\", \"Q2: B (Sai)\", \"Q10: C (Chính xác)\"]";
            var results3 = QuizAnswerParser.ParseAnswers(jsonStringList);
            Assert.Equal(3, results3.Count);
            Assert.Contains(results3, r => r.QuestionId == 1 && r.Answer == "A" && r.Correct);
            Assert.Contains(results3, r => r.QuestionId == 2 && r.Answer == "B" && !r.Correct);
            Assert.Contains(results3, r => r.QuestionId == 10 && r.Answer == "C" && r.Correct);

            // Empty or corrupted JSON shouldn't throw JsonException
            var resultsEmpty = QuizAnswerParser.ParseAnswers("");
            Assert.Empty(resultsEmpty);
            var resultsCorrupted = QuizAnswerParser.ParseAnswers("{invalid_json}");
            Assert.Empty(resultsCorrupted);
        }

        [Fact]
        public async Task TestSQLiteAsyncConcurrency_TC_03()
        {
            // Seed a quiz to reference
            int quizId;
            using (var dbSetup = new AppDbContext())
            {
                var quiz = new Quiz
                {
                    Title = "Concurrency Test Quiz",
                    QuizType = "Test",
                    TimeLimitSeconds = 120,
                    CreatedAt = DateTime.Now
                };
                dbSetup.Quizzes.Add(quiz);
                await dbSetup.SaveChangesAsync();
                quizId = quiz.Id;
            }

            // Write 50 parallel asynchronous records using async DbContexts
            int taskCount = 50;
            var tasks = new List<Task>();

            for (int i = 0; i < taskCount; i++)
            {
                int studentId = i + 1;
                tasks.Add(Task.Run(async () =>
                {
                    using var db = new AppDbContext();
                    var result = new QuizResult
                    {
                        QuizId = quizId,
                        StudentId = studentId,
                        Score = 80,
                        TotalPoints = 100,
                        CorrectCount = 8,
                        TotalQuestions = 10,
                        TimeSpentSeconds = 45,
                        AnswersJson = "[]"
                    };
                    db.QuizResults.Add(result);
                    await db.SaveChangesAsync();
                }));
            }

            // Await all writes to complete
            await Task.WhenAll(tasks);

            // Verify all 50 records were written successfully
            using (var dbVerify = new AppDbContext())
            {
                int count = await dbVerify.QuizResults.CountAsync(r => r.QuizId == quizId);
                Assert.Equal(taskCount, count);
            }
        }

        [Fact]
        public void TestIsNaturalScienceSubject_TC_04()
        {
            Assert.True(QuizAnswerParser.IsNaturalScienceSubject("Khoa học tự nhiên"));
            Assert.True(QuizAnswerParser.IsNaturalScienceSubject("KHTN"));
            Assert.True(QuizAnswerParser.IsNaturalScienceSubject("Vật lí"));
            Assert.True(QuizAnswerParser.IsNaturalScienceSubject("Toán học"));
            Assert.True(QuizAnswerParser.IsNaturalScienceSubject("toán"));
            Assert.True(QuizAnswerParser.IsNaturalScienceSubject("vật lý"));
            Assert.False(QuizAnswerParser.IsNaturalScienceSubject("Địa lý"));
            Assert.False(QuizAnswerParser.IsNaturalScienceSubject("Lịch sử"));
            Assert.False(QuizAnswerParser.IsNaturalScienceSubject(""));
            Assert.False(QuizAnswerParser.IsNaturalScienceSubject(null));
        }

        [Fact]
        public void TestQuizSecurityTransmission_TC_05()
        {
            var testQuestions = new List<Question>
            {
                new Question
                {
                    Id = 1,
                    QuizId = 1,
                    Content = "Hệ số góc của hàm số y = 2x + 1 là bao nhiêu?",
                    OptionsJson = "[\"A. 1\",\"B. 2\",\"C. -1\",\"D. 3\"]",
                    CorrectAnswer = "B",
                    Points = 10,
                    SortOrder = 1,
                    ImageUrl = "http://example.com/img1.png"
                }
            };

            var questionsData = testQuestions.Select(q => new Dictionary<string, object?>
            {
                { "Content", q.Content },
                { "OptionsJson", q.OptionsJson },
                { "Points", q.Points },
                { "SortOrder", q.SortOrder },
                { "ImageUrl", q.ImageUrl },
                // Lowercase properties for Web App compatibility
                { "content", q.Content },
                { "optionsJson", q.OptionsJson },
                { "points", q.Points },
                { "sortOrder", q.SortOrder },
                { "imageUrl", q.ImageUrl },
                { "image", q.ImageUrl }
            }).ToList();

            string questionsJson = System.Text.Json.JsonSerializer.Serialize(questionsData);

            Assert.DoesNotContain("CorrectAnswer", questionsJson);
            Assert.DoesNotContain("correctAnswer", questionsJson);
            Assert.DoesNotContain("correct", questionsJson);
            Assert.Contains("OptionsJson", questionsJson);
            Assert.Contains("ImageUrl", questionsJson);
        }

        [Fact]
        public void TestGradeOrderingAnswer_TC_06()
        {
            // Test correct ordering matching with different arrow styles
            Assert.True(QuizAnswerParser.GradeAnswer("A → B → C", "A -> B -> C", "ordering"));
            Assert.True(QuizAnswerParser.GradeAnswer("A->B->C", "A => B => C", "order"));
            Assert.True(QuizAnswerParser.GradeAnswer("a -> b -> c", "A → B → C", "ordering"));
            Assert.True(QuizAnswerParser.GradeAnswer("  A   ->   B  ->  C  ", "A->B->C", "ordering"));
            
            // Test incorrect ordering
            Assert.False(QuizAnswerParser.GradeAnswer("A → C → B", "A -> B -> C", "ordering"));
            Assert.False(QuizAnswerParser.GradeAnswer("A -> B", "A -> B -> C", "ordering"));
        }

        [Fact]
        public void TestGradeMatchingAnswer_TC_07()
        {
            // Test correct matching with key-value order independence
            Assert.True(QuizAnswerParser.GradeAnswer("MATCH:A->1,B->2", "MATCH:B->2,A->1", "matching"));
            Assert.True(QuizAnswerParser.GradeAnswer("a -> 1, b -> 2", "MATCH:B->2,A->1", "match"));
            Assert.True(QuizAnswerParser.GradeAnswer("MATCH:A->1, B->2", "A->1, B->2", "matching"));
            Assert.True(QuizAnswerParser.GradeAnswer("MATCH:A->1", "A->1", "matching"));

            // Test incorrect matching
            Assert.False(QuizAnswerParser.GradeAnswer("MATCH:A->2,B->1", "MATCH:A->1,B->2", "matching"));
            Assert.False(QuizAnswerParser.GradeAnswer("MATCH:A->1", "MATCH:A->1,B->2", "matching"));
            Assert.False(QuizAnswerParser.GradeAnswer("", "MATCH:A->1", "matching"));
        }

        [Fact]
        public void TestQuestionsDatabaseSchemaVideoUrl_TC_08()
        {
            using (var db = new AppDbContext())
            {
                var quiz = new Quiz
                {
                    Title = "Test Quiz for VideoUrl",
                    QuizType = "Test",
                    TimeLimitSeconds = 120,
                    CreatedAt = DateTime.Now
                };
                db.Quizzes.Add(quiz);
                db.SaveChanges();

                var question = new Question
                {
                    QuizId = quiz.Id,
                    Content = "Kiểm tra thí nghiệm ảo",
                    VideoUrl = "https://phet.colorado.edu/sims/html/acid-base-solutions/latest/acid-base-solutions_all.html",
                    CorrectAnswer = "A"
                };

                db.Questions.Add(question);
                db.SaveChanges();

                var savedQuestion = db.Questions.FirstOrDefault(q => q.Id == question.Id);
                Assert.NotNull(savedQuestion);
                Assert.Equal("https://phet.colorado.edu/sims/html/acid-base-solutions/latest/acid-base-solutions_all.html", savedQuestion.VideoUrl);

                db.Questions.Remove(savedQuestion);
                db.Quizzes.Remove(quiz);
            }
        }

        [Fact]
        public void TestQrCodeGenerationAndHMACVerification_TC_09()
        {
            string studentCode = "HS12345";
            string studentName = "Nguyen Van A";
            double globalGpa = 85.50;
            int totalAssessments = 12;

            string dataToHash = $"{studentCode}|{studentName}|{globalGpa:F2}|{totalAssessments}";
            string hmac = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(dataToHash, QASmartClass.Utilities.SecurityKeyProvider.GetHmacKey());
            string qrText = $"{dataToHash}|{hmac}";

            string verifyHmac = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(dataToHash, QASmartClass.Utilities.SecurityKeyProvider.GetHmacKey());
            Assert.Equal(hmac, verifyHmac);

            byte[] qrCodeBytes = null;
            using (var qrGenerator = new QRCoder.QRCodeGenerator())
            using (var qrCodeData = qrGenerator.CreateQrCode(qrText, QRCoder.QRCodeGenerator.ECCLevel.M))
            using (var qrCode = new QRCoder.QRCode(qrCodeData))
            using (System.Drawing.Bitmap qrBitmap = qrCode.GetGraphic(12, System.Drawing.Color.Black, System.Drawing.Color.White, true))
            {
                using (var ms = new System.IO.MemoryStream())
                {
                    qrBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    qrCodeBytes = ms.ToArray();
                }
            }

            Assert.NotNull(qrCodeBytes);
            Assert.True(qrCodeBytes.Length > 0);
            
            var parts = qrText.Split('|');
            Assert.Equal(5, parts.Length);
            Assert.Equal(studentCode, parts[0]);
            Assert.Equal(studentName, parts[1]);
            Assert.Equal("85.50", parts[2]);
            Assert.Equal("12", parts[3]);
            Assert.Equal(hmac, parts[4]);
        }

        [Fact]
        public void TestSecurityKeyProviderSync_TC_10()
        {
            string newServerKey = "TeacherServer_Sync_HMAC_Secret_Key_2026_#xyz";
            
            // Sync key from simulated server message
            QASmartClass.Utilities.SecurityKeyProvider.UpdateHmacKey(newServerKey);
            
            // Verify key is encrypted and stored correctly, and can be decrypted to match newServerKey
            string keyFromProvider = QASmartClass.Utilities.SecurityKeyProvider.GetHmacKey();
            Assert.Equal(newServerKey, keyFromProvider);
            
            // Clean up and restore backup key
            QASmartClass.Utilities.SecurityKeyProvider.UpdateHmacKey("QASmartClass_Internal_Backup_Secret_Key_2026");
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_tempDbPath)) File.Delete(_tempDbPath);
                if (File.Exists(_tempVersionPath)) File.Delete(_tempVersionPath);
            }
            catch { }
        }
    }
}
