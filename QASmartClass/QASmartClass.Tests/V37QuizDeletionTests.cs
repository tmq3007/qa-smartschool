using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using Xunit;

namespace QASmartClass.Tests
{
    public class V37QuizDeletionTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly string _tempVersionPath;

        public V37QuizDeletionTests()
        {
            if (System.Windows.Application.Current == null)
            {
                try
                {
                    new System.Windows.Application();
                }
                catch { }
            }
            AppServices.UIService = new MockUserInterfaceService();

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
        public void TestQuizCascadeDelete()
        {
            using (var db = new AppDbContext())
            {
                // 1. Tạo dữ liệu mẫu Quiz, Question, QuizResult
                var quiz = new Quiz
                {
                    Title = "Test Quiz Delete",
                    QuizType = "Test",
                    TimeLimitSeconds = 300,
                    CreatedAt = DateTime.Now
                };
                db.Quizzes.Add(quiz);
                db.SaveChanges();

                var question = new Question
                {
                    QuizId = quiz.Id,
                    Content = "Sample Question?",
                    QuestionType = "MultipleChoice",
                    OptionsJson = "[\"A\",\"B\"]",
                    CorrectAnswer = "A",
                    Points = 10,
                    Difficulty = "Easy",
                    SortOrder = 1
                };
                db.Questions.Add(question);

                var quizResult = new QuizResult
                {
                    QuizId = quiz.Id,
                    StudentId = 1,
                    Score = 100,
                    TotalPoints = 10,
                    CorrectCount = 1,
                    TotalQuestions = 1,
                    TimeSpentSeconds = 30,
                    AnswersJson = "{\"1\":\"A\"}"
                };
                db.QuizResults.Add(quizResult);
                db.SaveChanges();

                // Kiểm tra dữ liệu đã vào db
                Assert.True(db.Quizzes.Any(q => q.Id == quiz.Id));
                Assert.True(db.Questions.Any(q => q.QuizId == quiz.Id));
                Assert.True(db.QuizResults.Any(r => r.QuizId == quiz.Id));

                // 2. Thực hiện xóa cascade tương tự logic trong Code-behind
                var targetQuiz = db.Quizzes.FirstOrDefault(q => q.Id == quiz.Id);
                Assert.NotNull(targetQuiz);

                // Xóa questions liên quan
                var relatedQuestions = db.Questions.Where(q => q.QuizId == quiz.Id).ToList();
                db.Questions.RemoveRange(relatedQuestions);

                // Xóa results liên quan
                var relatedResults = db.QuizResults.Where(r => r.QuizId == quiz.Id).ToList();
                db.QuizResults.RemoveRange(relatedResults);

                // Xóa chính quiz
                db.Quizzes.Remove(targetQuiz);
                db.SaveChanges();

                // 3. Xác nhận đã xóa sạch
                Assert.False(db.Quizzes.Any(q => q.Id == quiz.Id));
                Assert.False(db.Questions.Any(q => q.QuizId == quiz.Id));
                Assert.False(db.QuizResults.Any(r => r.QuizId == quiz.Id));
            }
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
