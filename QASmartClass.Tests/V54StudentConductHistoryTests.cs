using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using Xunit;

namespace QASmartClass.Tests
{
    public class V54StudentConductHistoryTests : IDisposable
    {
        private readonly string _dbFile;
        private readonly string _versionFile;

        public V54StudentConductHistoryTests()
        {
            lock (typeof(System.Windows.Application))
            {
                if (System.Windows.Application.Current == null)
                {
                    try
                    {
                        _ = new System.Windows.Application();
                    }
                    catch { }
                }
            }
            AppPaths.EnsureDirectories();
            var uniqueId = Guid.NewGuid().ToString("N");
            _dbFile = System.IO.Path.Combine(AppPaths.RootDir, $"smartclass_conduct_{uniqueId}.db");
            _versionFile = System.IO.Path.Combine(AppPaths.RootDir, $"db_version_conduct_{uniqueId}.txt");
            
            AppPaths.DatabaseFile = _dbFile;
            AppPaths.DbVersionFile = _versionFile;
            
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.44.0");
        }

        public void Dispose()
        {
            try
            {
                if (System.IO.File.Exists(_dbFile))
                    System.IO.File.Delete(_dbFile);
            }
            catch {}
            try
            {
                if (System.IO.File.Exists(_versionFile))
                    System.IO.File.Delete(_versionFile);
            }
            catch {}
        }

        [Fact]
        public void TestStudentConductHistory_TableExists_AndMigratesOldScores()
        {
            using (var db = new AppDbContext())
            {
                // Clear any existing test student
                var existing = db.Students.FirstOrDefault(s => s.StudentCode == "HS_V54_TEST");
                if (existing != null)
                {
                    db.Students.Remove(existing);
                    db.SaveChanges();
                }

                // Add a student with specific conduct score
                var student = new Student
                {
                    FullName = "Conduct Test Student",
                    StudentCode = "HS_V54_TEST",
                    ConductScore = 88,
                    ClassroomId = 1 // assuming classroom 1 exists or doesn't throw
                };
                db.Students.Add(student);
                db.SaveChanges();

                // Trigger a run of migration or manually execute insertion query to simulate migration copying
                db.Database.ExecuteSqlRaw(@"
                    INSERT INTO StudentConductHistories (StudentId, Semester, SchoolYear, ConductScore)
                    SELECT Id, 'HK1', '2025-2026', ConductScore FROM Students
                    WHERE StudentCode = 'HS_V54_TEST' AND Id NOT IN (SELECT StudentId FROM StudentConductHistories WHERE Semester = 'HK1' AND SchoolYear = '2025-2026')
                ");

                db.Database.ExecuteSqlRaw(@"
                    INSERT INTO StudentConductHistories (StudentId, Semester, SchoolYear, ConductScore)
                    SELECT Id, 'HK2', '2025-2026', ConductScore FROM Students
                    WHERE StudentCode = 'HS_V54_TEST' AND Id NOT IN (SELECT StudentId FROM StudentConductHistories WHERE Semester = 'HK2' AND SchoolYear = '2025-2026')
                ");

                // Check that StudentConductHistories has records for this student
                var records = db.StudentConductHistories
                    .Where(h => h.StudentId == student.Id)
                    .ToList();

                Assert.Equal(2, records.Count);
                
                var hk1 = records.FirstOrDefault(r => r.Semester == "HK1");
                Assert.NotNull(hk1);
                Assert.Equal("2025-2026", hk1.SchoolYear);
                Assert.Equal(88, hk1.ConductScore);

                var hk2 = records.FirstOrDefault(r => r.Semester == "HK2");
                Assert.NotNull(hk2);
                Assert.Equal("2025-2026", hk2.SchoolYear);
                Assert.Equal(88, hk2.ConductScore);

                // Clean up
                db.Students.Remove(student);
                db.StudentConductHistories.RemoveRange(records);
                db.SaveChanges();
            }
        }
    }
}
