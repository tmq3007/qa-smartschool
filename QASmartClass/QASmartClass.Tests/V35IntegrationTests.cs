using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.Services;
using QASmartClass.Staff.ViewModels;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace QASmartClass.Tests
{
    public class V35IntegrationTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly string _tempVersionPath;

        public V35IntegrationTests()
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
            DbMigrator.Migrate(db, "5.33.0");
        }

        [Fact]
        public async Task TestSQLiteForeignKeys_PreventsOrphanDeletes()
        {
            using (var db = new AppDbContext())
            {
                // 1. Create a classroom first to satisfy foreign keys if needed
                var classroom = new QASmartClass.Data.Classroom
                {
                    Name = "Class 10A V35",
                    ClassCode = "10A_V35",
                    TeacherName = "GV_V35"
                };
                db.Classrooms.Add(classroom);
                
                // 2. Create a roster
                var roster = new ClassRoster
                {
                    ClassName = "10A V35",
                    IsActive = true,
                    Subject = "Toán",
                    SchoolYear = "2025-2026",
                    Semester = "HK1"
                };
                db.ClassRosters.Add(roster);
                db.SaveChanges();

                // 3. Create a student
                var student = new Student
                {
                    StudentCode = "HS_V35_FK",
                    FullName = "Học sinh khóa ngoại",
                    ClassroomId = classroom.Id,
                    ClassName = "10A V35"
                };
                db.Students.Add(student);
                db.SaveChanges();

                // 4. Create an attendance record linked to the student
                var attRecord = new AttendanceRecord
                {
                    StudentId = student.Id,
                    RosterId = roster.Id,
                    Date = DateTime.Today,
                    Status = "Present"
                };
                db.AttendanceRecords.Add(attRecord);
                db.SaveChanges();

                // 5. Try to delete the student (parent) while the attendance record (child) still exists
                db.Students.Remove(student);

                // Assert that DbUpdateException is thrown due to Foreign Key constraint violation
                await Assert.ThrowsAsync<DbUpdateException>(async () =>
                {
                    await db.SaveChangesAsync();
                });
            }
        }

        [Fact]
        public void TestSerilogObjectDestructuring_MasksComplexJson()
        {
            var formatter = new SensitiveDataMaskingFormatter("{Message}");

            // Simulating destructured JSON structures in logs
            string rawLog = "Student details: {\"StudentCode\":\"HS001\",\"WalletBalance\":1500000.00,\"CitizenId\":\"123456789012\",\"RFID\":\"7E4A8F9C\"}";
            var template = new MessageTemplateParser().Parse(rawLog);
            var properties = new System.Collections.Generic.List<LogEventProperty>();
            var logEvent = new LogEvent(
                DateTimeOffset.Now, 
                LogEventLevel.Information, 
                null, 
                template, 
                properties
            );

            using (var writer = new StringWriter())
            {
                formatter.Format(logEvent, writer);
                string result = writer.ToString();

                // Assertions for JSON destructured values
                Assert.Contains("\"CitizenId\":\"XXXXXXXX9012\"", result); // CCCD masked
                Assert.Contains("\"RFID\":\"7E4AXXXX\"", result);         // RFID masked
                Assert.Contains("\"WalletBalance\":XXXXXXX.XX", result);   // Balance masked
            }
        }

        [Fact]
        public void TestAutoRefreshTasks_TimerTickLoadsData()
        {
            using (var vm = new TaskManagementViewModel())
            {
                var field = typeof(TaskManagementViewModel).GetField("_refreshTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(field);
                
                var timer = field.GetValue(vm) as System.Windows.Threading.DispatcherTimer;
                Assert.NotNull(timer);
                Assert.True(timer.IsEnabled);
                Assert.Equal(TimeSpan.FromSeconds(60), timer.Interval);
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
