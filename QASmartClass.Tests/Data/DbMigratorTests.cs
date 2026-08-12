using System;
using System.IO;
using Xunit;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Tests.Data
{
    public class DbMigratorTests : IDisposable
    {
        private readonly string _tempDir;

        public DbMigratorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"qa_dbtest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            // SQLite có thể giữ file lock → bỏ qua lỗi cleanup
            try
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, true);
            }
            catch { /* Ignore cleanup errors in tests */ }
        }

        private AppDbContext CreateTestDb(string name = "test.db")
        {
            var dbPath = Path.Combine(_tempDir, name);
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            return new AppDbContext(optionsBuilder.Options);
        }

        [Fact]
        public void FreshInstall_Creates_Database()
        {
            using var db = CreateTestDb("fresh.db");
            db.Database.EnsureCreated();
            Assert.True(File.Exists(Path.Combine(_tempDir, "fresh.db")));
        }

        [Fact]
        public void EnsureCreated_Creates_Students_Table()
        {
            using var db = CreateTestDb("tables.db");
            db.Database.EnsureCreated();

            // Verify table exists by querying
            var count = db.Students.Count();
            Assert.True(count >= 0); // No exception = table exists
        }

        [Fact]
        public void EnsureCreated_Creates_Lessons_Table()
        {
            using var db = CreateTestDb("lessons.db");
            db.Database.EnsureCreated();
            var count = db.Lessons.Count();
            Assert.True(count >= 0);
        }

        [Fact]
        public void EnsureCreated_Creates_Quizzes_Table()
        {
            using var db = CreateTestDb("quizzes.db");
            db.Database.EnsureCreated();
            var count = db.Quizzes.Count();
            Assert.True(count >= 0);
        }

        [Fact]
        public void AddStudent_Persists_Data()
        {
            using var db = CreateTestDb("crud.db");
            db.Database.EnsureCreated();

            db.Students.Add(new QASmartClass.Data.Student
            {
                FullName = "Nguyen Van A",
                StudentCode = "HS001"
            });
            db.SaveChanges();

            Assert.Equal(1, db.Students.Count());
            Assert.Equal("Nguyen Van A", db.Students.First().FullName);
        }

        [Fact]
        public void AddLesson_Persists_Data()
        {
            using var db = CreateTestDb("lesson_crud.db");
            db.Database.EnsureCreated();

            db.Lessons.Add(new QASmartClass.Data.Lesson
            {
                Title = "Bai hoc 1",
                Subject = "Toan"
            });
            db.SaveChanges();

            Assert.Equal(1, db.Lessons.Count());
        }

        [Fact]
        public void AddQuiz_With_Questions()
        {
            using var db = CreateTestDb("quiz_crud.db");
            db.Database.EnsureCreated();

            var quiz = new QASmartClass.Data.Quiz
            {
                Title = "Quiz Test"
            };
            db.Quizzes.Add(quiz);
            db.SaveChanges();

            Assert.True(quiz.Id > 0);
        }

        [Fact]
        public void DeleteStudent_Removes_Data()
        {
            using var db = CreateTestDb("delete.db");
            db.Database.EnsureCreated();

            var student = new QASmartClass.Data.Student
            {
                FullName = "To Delete",
                StudentCode = "DEL001"
            };
            db.Students.Add(student);
            db.SaveChanges();
            Assert.Equal(1, db.Students.Count());

            db.Students.Remove(student);
            db.SaveChanges();
            Assert.Equal(0, db.Students.Count());
        }
    }
}
