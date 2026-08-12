using Xunit;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Helpers;
using QASmartClass.Data;

namespace QASmartClass.Tests
{
    public class V41QuizUpgradeTests
    {
        [Fact]
        public void TestDictionaryParsing_TC_01()
        {
            // 1. Standard dictionary mapping questionId -> selectedAnswer
            string json1 = "{\"12\":\"A\", \"13\":\"C\"}";
            var results1 = QuizAnswerParser.ParseAnswers(json1);
            Assert.Equal(2, results1.Count);
            
            var item12 = results1.FirstOrDefault(x => x.QuestionId == 12);
            Assert.NotNull(item12);
            Assert.Equal("A", item12.Answer);

            var item13 = results1.FirstOrDefault(x => x.QuestionId == 13);
            Assert.NotNull(item13);
            Assert.Equal("C", item13.Answer);

            // 2. Web zero-based index mapping
            string json2 = "{\"0\":0, \"1\":2}";
            var results2 = QuizAnswerParser.ParseAnswers(json2);
            Assert.Equal(2, results2.Count);

            var item1 = results2.FirstOrDefault(x => x.QuestionId == 1); // 0 + 1
            Assert.NotNull(item1);
            Assert.Equal("A", item1.Answer); // index 0 -> A

            var item2 = results2.FirstOrDefault(x => x.QuestionId == 2); // 1 + 1
            Assert.NotNull(item2);
            Assert.Equal("C", item2.Answer); // index 2 -> C
        }

        [Fact]
        public void TestSingleObjectAndStringListParsing_TC_02()
        {
            // 1. Single object format (both PascalCase/camelCase support)
            string json1 = "{\"QuestionId\": 42, \"Answer\": \"B\", \"Correct\": true, \"Points\": 10, \"TimeTaken\": 5}";
            var results1 = QuizAnswerParser.ParseAnswers(json1);
            Assert.Single(results1);
            var item1 = results1[0];
            Assert.Equal(42, item1.QuestionId);
            Assert.Equal("B", item1.Answer);
            Assert.True(item1.Correct);
            Assert.Equal(10, item1.Points);
            Assert.Equal(5, item1.TimeTaken);

            // 2. String list format
            string json2 = "[\"Q1: A (Dung) — Content\", \"Q2: C (Sai) — Content\"]";
            var results2 = QuizAnswerParser.ParseAnswers(json2);
            Assert.Equal(2, results2.Count);

            var itemQ1 = results2.FirstOrDefault(x => x.QuestionId == 1);
            Assert.NotNull(itemQ1);
            Assert.Equal("A", itemQ1.Answer);
            Assert.True(itemQ1.Correct);

            var itemQ2 = results2.FirstOrDefault(x => x.QuestionId == 2);
            Assert.NotNull(itemQ2);
            Assert.Equal("C", itemQ2.Answer);
            Assert.False(itemQ2.Correct);
        }

        [Fact]
        public async Task TestSQLiteAsyncConcurrency_TC_03()
        {
            using var db = TestDbFactory.Create();
            
            // Seed a quiz
            var quiz = new Quiz
            {
                Title = "Test Quiz",
                QuizType = "Standard",
                TimeLimitSeconds = 600,
                CreatedAt = DateTime.Now
            };
            db.Quizzes.Add(quiz);
            await db.SaveChangesAsync();

            // Simulate concurrent writes of QuizResults
            int studentCount = 50;
            var tasks = new Task[studentCount];

            var connection = db.Database.GetDbConnection();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var lockObj = new object();
            for (int i = 1; i <= studentCount; i++)
            {
                int studentId = i;
                tasks[i - 1] = Task.Run(() =>
                {
                    AppDbContext threadDb;
                    lock (lockObj)
                    {
                        threadDb = new AppDbContext(options);
                    }
                    using (threadDb)
                    {
                        var result = new QuizResult
                        {
                            QuizId = quiz.Id,
                            StudentId = studentId,
                            Score = 80,
                            TotalPoints = 100,
                            CorrectCount = 8,
                            TotalQuestions = 10,
                            TimeSpentSeconds = 50,
                            AnswersJson = "{}",
                            SubmittedAt = DateTime.Now
                        };
                        threadDb.QuizResults.Add(result);
                        lock (lockObj)
                        {
                            threadDb.SaveChanges();
                        }
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Verify
            int count = db.QuizResults.Count();
            Assert.Equal(studentCount, count);
        }

        [Fact]
        public void TestQuizDto_ShouldContainNoAnswerKey()
        {
            // Verify that StudentQuizQuestionDto does not leak correct answers via properties
            var properties = typeof(QASmartClass.StudentClient.Views.StudentShell.StudentQuizQuestionDto).GetProperties();
            
            foreach (var prop in properties)
            {
                Assert.NotEqual("CorrectAnswer", prop.Name, StringComparer.OrdinalIgnoreCase);
                Assert.NotEqual("IsCorrect", prop.Name, StringComparer.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void TestMasterSettings_LoadAndSave_SystemSettingsCorrectly()
        {
            using var db = TestDbFactory.Create();

            // 1. Seed or assert default setup is empty/non-existent
            var timeoutSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Broadcast_UdpHeartbeatTimeout");
            Assert.Null(timeoutSetting);

            // 2. Add settings to DB
            db.SystemSettings.Add(new SystemSetting 
            { 
                Id = "Broadcast_UdpHeartbeatTimeout", 
                Value = "20", 
                Category = "Broadcast", 
                LastUpdated = DateTime.Now 
            });
            db.SystemSettings.Add(new SystemSetting 
            { 
                Id = "Broadcast_EnableScreenExclusion", 
                Value = "false", 
                Category = "Broadcast", 
                LastUpdated = DateTime.Now 
            });
            db.SaveChanges();

            // 3. Load settings via AppSettings and verify
            QASmartTouch.Services.AppSettings.LoadFromDatabase(db);
            
            Assert.Equal(20, QASmartTouch.Services.AppSettings.Broadcast_UdpHeartbeatTimeout);
            Assert.False(QASmartTouch.Services.AppSettings.Broadcast_EnableScreenExclusion);

            // 4. Update setting in DB and reload
            var timeout = db.SystemSettings.FirstOrDefault(s => s.Id == "Broadcast_UdpHeartbeatTimeout");
            Assert.NotNull(timeout);
            timeout.Value = "15";
            db.SaveChanges();

            QASmartTouch.Services.AppSettings.LoadFromDatabase(db);
            Assert.Equal(15, QASmartTouch.Services.AppSettings.Broadcast_UdpHeartbeatTimeout);
        }
    }
}
