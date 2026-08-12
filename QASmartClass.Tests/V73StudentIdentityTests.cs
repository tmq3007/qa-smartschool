using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.StudentClient.Services;
using Xunit;

namespace QASmartClass.Tests
{
    public class V73StudentIdentityTests : IDisposable
    {
        private readonly string _dbFile;
        private readonly string _versionFile;

        public V73StudentIdentityTests()
        {
            QASmartClass.Services.AppPaths.EnsureDirectories();
            var uniqueId = Guid.NewGuid().ToString("N");
            _dbFile = System.IO.Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"smartclass_test_v73_{uniqueId}.db");
            _versionFile = System.IO.Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"db_version_test_v73_{uniqueId}.txt");
            
            QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
            QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;
            
            using var db = new AppDbContext();
            QASmartClass.Services.DbMigrator.Migrate(db, "5.44.0");
        }

        public void Dispose()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
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
        public void StudentIdentityService_ShouldReturnCorrectStudent()
        {
            using (var db = new AppDbContext())
            using (var tx = db.Database.BeginTransaction())
            {
                // Backup existing profile
                var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                var profileDir = Path.GetDirectoryName(profilePath);
                if (!string.IsNullOrEmpty(profileDir)) Directory.CreateDirectory(profileDir);

                string oldProfileText = null;
                if (File.Exists(profilePath))
                {
                    try { oldProfileText = SecureProfileHelper.ReadProfileText(profilePath); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V73StudentIdentityTests] Error: {ex.Message}"); }
                }

                try
                {
                    // Clean up any existing online students for clean test run
                    var existingOnline = db.Students.Where(s => s.IsOnline).ToList();
                    foreach (var s in existingOnline) s.IsOnline = false;
                    db.SaveChanges();

                    // Create test student 1 (offline)
                    var s1 = new Student
                    {
                        FullName = "Học sinh Một",
                        StudentCode = "HS_TEST_ID_1",
                        PCName = "PC01",
                        IPAddress = "192.168.1.10",
                        IsOnline = false,
                        LastSeen = DateTime.Now
                    };
                    
                    // Create test student 2 (online)
                    var s2 = new Student
                    {
                        FullName = "Học sinh Hai",
                        StudentCode = "HS_TEST_ID_2",
                        PCName = "PC02",
                        IPAddress = "192.168.1.11",
                        IsOnline = true,
                        LastSeen = DateTime.Now
                    };

                    db.Students.Add(s1);
                    db.Students.Add(s2);
                    try { db.SaveChanges(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V73StudentIdentityTests] DB save error: {ex.Message}"); throw; }

                    // Mock the profile file content
                    var mockProfileJson = "{\"StudentCode\":\"HS_TEST_ID_2\",\"StudentName\":\"Học sinh Hai\"}";
                    SecureProfileHelper.WriteProfileText(profilePath, mockProfileJson);

                    var service = new StudentIdentityService(db);
                    var (id, code, name) = service.GetCurrentStudent();

                    // Should resolve to the online student (s2)
                    Assert.Equal(s2.Id, id);
                    Assert.Equal("HS_TEST_ID_2", code);
                    Assert.Equal("Học sinh Hai", name);
                }
                finally
                {
                    // Restore old profile
                    try
                    {
                        if (oldProfileText != null)
                            SecureProfileHelper.WriteProfileText(profilePath, oldProfileText);
                        else if (File.Exists(profilePath))
                            File.Delete(profilePath);
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V73StudentIdentityTests] Error: {ex.Message}"); }

                    tx.Rollback();
                }
            }
        }

        [Fact]
        public void DashboardQuery_ShouldFilterStatsByStudentId()
        {
            using (var db = new AppDbContext())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var existingOnline = db.Students.Where(s => s.IsOnline).ToList();
                    foreach (var s in existingOnline) s.IsOnline = false;
                    db.SaveChanges();

                    // Create two students
                    var s1 = new Student 
                    { 
                        FullName = "Học sinh Một", 
                        StudentCode = "HS_TEST_ID_1", 
                        IsOnline = true,
                        PCName = "PC01",
                        IPAddress = "192.168.1.10",
                        LastSeen = DateTime.Now 
                    };
                    var s2 = new Student 
                    { 
                        FullName = "Học sinh Hai", 
                        StudentCode = "HS_TEST_ID_2", 
                        IsOnline = false,
                        PCName = "PC02",
                        IPAddress = "192.168.1.11",
                        LastSeen = DateTime.Now 
                    };
                    db.Students.AddRange(s1, s2);
                    try { db.SaveChanges(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V73StudentIdentityTests] DB save error: {ex.Message}"); throw; }

                    // Add quiz results
                    var r1 = new QuizResult { QuizId = 1, StudentId = s1.Id, Score = 80, TotalPoints = 100, CorrectCount = 8, TotalQuestions = 10, SubmittedAt = DateTime.Now };
                    var r2 = new QuizResult { QuizId = 1, StudentId = s1.Id, Score = 90, TotalPoints = 100, CorrectCount = 9, TotalQuestions = 10, SubmittedAt = DateTime.Now };
                    var r3 = new QuizResult { QuizId = 1, StudentId = s2.Id, Score = 40, TotalPoints = 100, CorrectCount = 4, TotalQuestions = 10, SubmittedAt = DateTime.Now };
                    db.QuizResults.AddRange(r1, r2, r3);
                    db.SaveChanges();

                    // Query stats for s1
                    var s1SubmitCount = db.QuizResults.Count(r => r.StudentId == s1.Id);
                    var s1AvgScore = db.QuizResults.Where(r => r.StudentId == s1.Id).Average(r => (double)r.Score);

                    // Query stats for s2
                    var s2SubmitCount = db.QuizResults.Count(r => r.StudentId == s2.Id);
                    var s2AvgScore = db.QuizResults.Where(r => r.StudentId == s2.Id).Average(r => (double)r.Score);

                    Assert.Equal(2, s1SubmitCount);
                    Assert.Equal(85.0, s1AvgScore);

                    Assert.Equal(1, s2SubmitCount);
                    Assert.Equal(40.0, s2AvgScore);
                }
                finally
                {
                    tx.Rollback();
                }
            }
        }

        [Fact]
        public void FileTransferQuery_ShouldFilterByStudentId()
        {
            using (var db = new AppDbContext())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var existingOnline = db.Students.Where(s => s.IsOnline).ToList();
                    foreach (var s in existingOnline) s.IsOnline = false;
                    db.SaveChanges();

                    var s1 = new Student 
                    { 
                        FullName = "Học sinh Một", 
                        StudentCode = "HS_TEST_ID_1", 
                        IsOnline = true,
                        PCName = "PC01",
                        IPAddress = "192.168.1.10",
                        LastSeen = DateTime.Now 
                    };
                    var s2 = new Student 
                    { 
                        FullName = "Học sinh Hai", 
                        StudentCode = "HS_TEST_ID_2", 
                        IsOnline = false,
                        PCName = "PC02",
                        IPAddress = "192.168.1.11",
                        LastSeen = DateTime.Now 
                    };
                    db.Students.AddRange(s1, s2);
                    try { db.SaveChanges(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V73StudentIdentityTests] DB save error: {ex.Message}"); throw; }

                    var f1 = new FileTransferRecord { FileName = "bai1.txt", StudentId = s1.Id, Direction = "StudentToTeacher", Status = "Completed", CreatedAt = DateTime.Now };
                    var f2 = new FileTransferRecord { FileName = "bai2.txt", StudentId = s1.Id, Direction = "StudentToTeacher", Status = "Pending", CreatedAt = DateTime.Now };
                    var f3 = new FileTransferRecord { FileName = "bai3.txt", StudentId = s2.Id, Direction = "StudentToTeacher", Status = "Completed", CreatedAt = DateTime.Now };
                    db.FileTransfers.AddRange(f1, f2, f3);
                    db.SaveChanges();

                    var s1SubCount = db.FileTransfers.Count(r => r.StudentId == s1.Id && r.Direction == "StudentToTeacher" && r.Status == "Completed");
                    var s1PenCount = db.FileTransfers.Count(r => r.StudentId == s1.Id && r.Direction == "StudentToTeacher" && r.Status == "Pending");

                    var s2SubCount = db.FileTransfers.Count(r => r.StudentId == s2.Id && r.Direction == "StudentToTeacher" && r.Status == "Completed");
                    var s2PenCount = db.FileTransfers.Count(r => r.StudentId == s2.Id && r.Direction == "StudentToTeacher" && r.Status == "Pending");

                    Assert.Equal(1, s1SubCount);
                    Assert.Equal(1, s1PenCount);
                    Assert.Equal(1, s2SubCount);
                    Assert.Equal(0, s2PenCount);
                }
                finally
                {
                    tx.Rollback();
                }
            }
        }

        [Fact]
        public void InlineEditorTitle_ShouldBeSanitizedForFilename()
        {
            var titleInput = "Bài làm\\../Chương3:*?\"<>|";
            var invalidChars = Path.GetInvalidFileNameChars();
            var title = string.Join("_", titleInput.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).Trim();
            
            // Should remove backslash, slash, colon, asterisk, question mark, etc.
            Assert.Contains("Bài làm", title);
            Assert.Contains("Chương3", title);
            Assert.DoesNotContain("\\", title);
            Assert.DoesNotContain("/", title);
            Assert.DoesNotContain(":", title);
            Assert.DoesNotContain("*", title);
            Assert.DoesNotContain("?", title);
            Assert.DoesNotContain("\"", title);
            Assert.DoesNotContain("<", title);
            Assert.DoesNotContain(">", title);
            Assert.DoesNotContain("|", title);
        }

        [Fact]
        public void LoginInput_ShouldBeSanitizedAndLengthLimited()
        {
            var rawCode = "<b>HS003</b>\0\x1f";
            var rawName = "<script>alert('XSS')</script>Trần Văn B với tên rất dài dài dài dài dài dài dài dài dài dài dài dài dài dài dài dài dài dài";

            var cleanCode = QASmartClass.LearningTools.Helpers.PathHelper.SanitizeInput(rawCode, 20);
            var cleanName = QASmartClass.LearningTools.Helpers.PathHelper.SanitizeInput(rawName, 50);

            // Verify HTML tags stripped
            Assert.Equal("HS003", cleanCode);
            Assert.DoesNotContain("<script>", cleanName);
            Assert.DoesNotContain("</script>", cleanName);
            Assert.Contains("Trần Văn B", cleanName);
            
            // Verify lengths
            Assert.True(cleanCode.Length <= 20);
            Assert.True(cleanName.Length <= 50);
        }
    }
}
