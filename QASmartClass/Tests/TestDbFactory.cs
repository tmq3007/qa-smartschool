using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Tests
{
    /// <summary>
    /// Factory t?o InMemory SQLite DB cho tests — m?i test 1 DB riêng
    /// Usage: using var db = TestDbFactory.Create();
    /// </summary>
    public static class TestDbFactory
    {
        /// <summary>T?o InMemory SQLite DB s?ch, dă EnsureCreated</summary>
        public static AppDbContext Create()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            var db = new AppDbContext(options);
            db.Database.OpenConnection();
            using (var cmd = db.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "PRAGMA busy_timeout = 5000;";
                cmd.ExecuteNonQuery();
            }
            db.Database.EnsureCreated();
            return db;
        }

        /// <summary>T?o InMemory DB có seed data co b?n cho testing</summary>
        public static AppDbContext CreateWithSeed()
        {
            var db = Create();

            // Seed test data
            db.Students.Add(new Student
            {
                FullName = "Nguy?n Van Test",
                StudentCode = "HS001",
                ClassName = "10A1",
                Status = "Active",
                ConductScore = 85,
                PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("Abc12345")
            });

            db.TeacherProfiles.Add(new TeacherProfile
            {
                FullName = "GV. Tr?n Th? Test",
                TeacherCode = "GV001",
                Subject = "Toán",
                PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword("Abc12345")
            });

            db.ClassRosters.Add(new ClassRoster
            {
                ClassName = "10A1",
                SchoolYear = "2025-2026",
                StudentCount = 35
            });

            db.StudentGrades.AddRange(
                new StudentGrade { StudentId = 1, RosterId = 1, GradeTypeId = 1, Score = 8.5, Attempt = 1 },
                new StudentGrade { StudentId = 1, RosterId = 1, GradeTypeId = 2, Score = 6.0, Attempt = 1 },
                new StudentGrade { StudentId = 1, RosterId = 1, GradeTypeId = 1, Score = 9.0, Attempt = 2 }
            );

            db.SaveChanges();
            return db;
        }
    }
}

