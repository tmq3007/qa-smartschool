using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.StudentClient.Services;
using QASmartClass.StudentClient.Views.MIGames;
using Xunit;

namespace QASmartClass.Tests
{
    public class V37GameHubTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly string _tempVersionPath;

        public V37GameHubTests()
        {
            QASmartClass.Staff.Services.ServiceRegistration.Initialize();
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
            _tempDbPath = Path.Combine(AppPaths.RootDir, $"smartclass_test_v37_{guid}.db");
            _tempVersionPath = Path.Combine(AppPaths.RootDir, $"db_version_v37_{guid}.txt");

            AppPaths.DatabaseFile = _tempDbPath;
            AppPaths.DbVersionFile = _tempVersionPath;
            AppPaths.EnsureDirectories();

            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.36.0");
        }

        [Fact]
        public void TestDailyStreakCalculation_InitializesTo1()
        {
            using (var db = new AppDbContext())
            {
                // Create a student with null streak info
                var student = new Student
                {
                    StudentCode = "HS_STREAK_1",
                    FullName = "Streak Student 1",
                    DailyStreak = 0,
                    LastActiveDate = null
                };
                db.Students.Add(student);
                db.SaveChanges();

                // Seed activity today to qualify for streak
                db.LearningDiaries.Add(new LearningDiary
                {
                    StudentId = student.Id,
                    Date = DateTime.Today,
                    Content = "Test activity"
                });
                db.SaveChanges();

                // Setup profile text to match this student code
                string path = AppPaths.StudentProfileFile;
                string profileJson = "{\"StudentCode\":\"HS_STREAK_1\",\"StudentName\":\"Streak Student 1\"}";
                // Write the profile encrypted with DPAPI
                var plainBytes = System.Text.Encoding.UTF8.GetBytes(profileJson);
                var encryptedBytes = System.Security.Cryptography.ProtectedData.Protect(plainBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                File.WriteAllBytes(path, encryptedBytes);

                // Run Identity Service resolution which updates streak
                var service = new StudentIdentityService(db);
                var resolved = service.GetCurrentStudent();

                // Retrieve from DB to verify streak is 1 and LastActiveDate is today
                var dbStudent = db.Students.First(s => s.StudentCode == "HS_STREAK_1");
                Assert.Equal(1, dbStudent.DailyStreak);
                Assert.NotNull(dbStudent.LastActiveDate);
                Assert.Equal(DateTime.Today, dbStudent.LastActiveDate.Value.Date);
            }
        }

        [Fact]
        public void TestDailyStreakCalculation_IncrementsOnConsecutiveDays()
        {
            using (var db = new AppDbContext())
            {
                // Yesterday
                var yesterday = DateTime.Today.AddDays(-1);
                
                var student = new Student
                {
                    StudentCode = "HS_STREAK_2",
                    FullName = "Streak Student 2",
                    DailyStreak = 3,
                    LastActiveDate = yesterday
                };
                db.Students.Add(student);
                db.SaveChanges();

                // Seed activity today to qualify for streak increment
                db.LearningDiaries.Add(new LearningDiary
                {
                    StudentId = student.Id,
                    Date = DateTime.Today,
                    Content = "Test activity"
                });
                db.SaveChanges();

                // Setup profile text
                string path = AppPaths.StudentProfileFile;
                string profileJson = "{\"StudentCode\":\"HS_STREAK_2\",\"StudentName\":\"Streak Student 2\"}";
                var plainBytes = System.Text.Encoding.UTF8.GetBytes(profileJson);
                var encryptedBytes = System.Security.Cryptography.ProtectedData.Protect(plainBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                File.WriteAllBytes(path, encryptedBytes);

                var service = new StudentIdentityService(db);
                var resolved = service.GetCurrentStudent();

                var dbStudent = db.Students.First(s => s.StudentCode == "HS_STREAK_2");
                Assert.Equal(4, dbStudent.DailyStreak); // 3 + 1
                Assert.Equal(DateTime.Today, dbStudent.LastActiveDate?.Date);
            }
        }

        [Fact]
        public void TestDailyStreakCalculation_ResetsOnGap()
        {
            using (var db = new AppDbContext())
            {
                // Active 3 days ago
                var olderDate = DateTime.Today.AddDays(-3);
                
                var student = new Student
                {
                    StudentCode = "HS_STREAK_3",
                    FullName = "Streak Student 3",
                    DailyStreak = 5,
                    LastActiveDate = olderDate
                };
                db.Students.Add(student);
                db.SaveChanges();

                // Seed activity today to qualify for streak reset calculation
                db.LearningDiaries.Add(new LearningDiary
                {
                    StudentId = student.Id,
                    Date = DateTime.Today,
                    Content = "Test activity"
                });
                db.SaveChanges();

                // Setup profile text
                string path = AppPaths.StudentProfileFile;
                string profileJson = "{\"StudentCode\":\"HS_STREAK_3\",\"StudentName\":\"Streak Student 3\"}";
                var plainBytes = System.Text.Encoding.UTF8.GetBytes(profileJson);
                var encryptedBytes = System.Security.Cryptography.ProtectedData.Protect(plainBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                File.WriteAllBytes(path, encryptedBytes);

                var service = new StudentIdentityService(db);
                var resolved = service.GetCurrentStudent();

                var dbStudent = db.Students.First(s => s.StudentCode == "HS_STREAK_3");
                Assert.Equal(1, dbStudent.DailyStreak); // Reset to 1
                Assert.Equal(DateTime.Today, dbStudent.LastActiveDate?.Date);
            }
        }

        [Fact]
        public void TestReflectionLoader_LoadsLogicalAndMusicalGames()
        {
            var t = new System.Threading.Thread(() =>
            {
                var assembly = typeof(LogicalGameView).Assembly;
                
                // Resolve LogicalGameView
                var logicType = assembly.GetType("QASmartClass.StudentClient.Views.MIGames.LogicalGameView");
                Assert.NotNull(logicType);
                
                var logicInstance = Activator.CreateInstance(logicType) as IMiGameControl;
                Assert.NotNull(logicInstance);

                // Resolve MusicalGameView
                var musicalType = assembly.GetType("QASmartClass.StudentClient.Views.MIGames.MusicalGameView");
                Assert.NotNull(musicalType);
                
                var musicalInstance = Activator.CreateInstance(musicalType) as IMiGameControl;
                Assert.NotNull(musicalInstance);
            });
            t.SetApartmentState(System.Threading.ApartmentState.STA);
            t.Start();
            t.Join();
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_tempDbPath)) File.Delete(_tempDbPath);
                if (File.Exists(_tempVersionPath)) File.Delete(_tempVersionPath);
                
                string path = AppPaths.StudentProfileFile;
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
        }
    }
}
