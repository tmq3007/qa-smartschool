using System;
using System.IO;
using System.Collections.Generic;
using Xunit;
using QASmartClass.Services;
using QASmartClass.Data;
using Microsoft.EntityFrameworkCore;

namespace QASmartClass.Tests.Services
{
    public class PdfExportServiceTests : IDisposable
    {
        private readonly string _testDir;
        private readonly PdfExportService _svc;
        private readonly AppDbContext _db;

        public PdfExportServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;
            _db = new AppDbContext(options);
            _db.Database.OpenConnection();
            _db.Database.EnsureCreated();

            _testDir = Path.Combine(Path.GetTempPath(), "QASmartClass_Test_PdfExports");
            if (!Directory.Exists(_testDir))
                Directory.CreateDirectory(_testDir);

            _svc = new PdfExportService(_db);
        }

        public void Dispose()
        {
            _db.Database.CloseConnection();
            _db.Dispose();
            if (Directory.Exists(_testDir))
            {
                try { Directory.Delete(_testDir, true); } catch { }
            }
        }

        [Fact]
        public void ExportAttendanceReport_Creates_File()
        {
            var allStudents = new List<Student>
            {
                new Student { Id = 1, FullName = "Nguyen Van A", StudentCode = "HS001" },
                new Student { Id = 2, FullName = "Tran Thi B", StudentCode = "HS002" }
            };
            var presentIds = new List<int> { 1 };

            string outPath = _svc.ExportAttendanceReport("Lớp 10A1", DateTime.Now, allStudents, presentIds);

            Assert.False(string.IsNullOrEmpty(outPath));
            Assert.True(File.Exists(outPath));
        }

        [Fact]
        public void ExportGradeReport_Creates_File()
        {
            var data = new List<StudentGradeDto>
            {
                new StudentGradeDto { StudentCode = "HS001", FullName = "Nguyen Van A", Score = 8.5 },
                new StudentGradeDto { StudentCode = "HS002", FullName = "Tran Thi B", Score = 9.0 }
            };

            string outPath = _svc.ExportGradeReport("Lớp 10A1", "Toán", data);

            Assert.False(string.IsNullOrEmpty(outPath));
            Assert.True(File.Exists(outPath));
        }

        [Fact]
        public void ExportQuizSummaryReport_Creates_File()
        {
            var data = new QuizSummaryDto
            {
                QuizTitle = "Kiểm tra 1 tiết",
                QuizType = "Solo",
                ClassName = "10A1",
                TotalQuestions = 20,
                TimeLimitSeconds = 900,
                HeldAt = DateTime.Now,
                Results = new List<QuizResultRowDto>
                {
                    new QuizResultRowDto { FullName = "A", CorrectCount = 18, TotalQuestions = 20, ScorePercent = 90, TimeSpentSeconds = 600 }
                }
            };

            string outPath = _svc.ExportQuizSummaryReport(data);

            Assert.False(string.IsNullOrEmpty(outPath));
            Assert.True(File.Exists(outPath));
        }

        [Fact]
        public void ExportUsageReport_Creates_File()
        {
            var data = new UsageReportDto
            {
                FromDate = DateTime.Now.AddDays(-30),
                ToDate = DateTime.Now,
                TotalSessions = 150,
                TotalHours = 120.5,
                TotalQuizzes = 10,
                TotalFileTransfers = 5,
                TopEvents = new List<EventSummaryDto>
                {
                    new EventSummaryDto { EventType = "LOGIN", Count = 150, AvgDurationMs = 100 }
                },
                DailyActivity = new List<DailyActivityDto>
                {
                    new DailyActivityDto { Date = DateTime.Now, EventCount = 50, TotalHours = 10 }
                }
            };

            string outPath = _svc.ExportUsageReport(data);

            Assert.False(string.IsNullOrEmpty(outPath));
            Assert.True(File.Exists(outPath));
        }

        [Fact]
        public void ExportWithEmptyData_DoesNotCrash()
        {
            var emptyStudents = new List<Student>();
            var emptyIds = new List<int>();
            
            var exception = Record.Exception(() => 
                _svc.ExportAttendanceReport("Lớp Trống", DateTime.Now, emptyStudents, emptyIds));
                
            Assert.Null(exception);
        }
    }
}
