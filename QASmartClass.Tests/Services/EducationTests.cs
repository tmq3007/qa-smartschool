using System;
using System.IO;
using System.Linq;
using Xunit;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;

namespace QASmartClass.Tests.Services
{
    public class LearningAnalyticsTests : IDisposable
    {
        private readonly AppDbContext _db;

        public LearningAnalyticsTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;
            _db = new AppDbContext(options);
            _db.Database.OpenConnection();
            _db.Database.EnsureCreated();

            // Seed student
            _db.Students.Add(new Student { FullName = "Test Student", StudentCode = "HS001" });
            _db.SaveChanges();
        }

        public void Dispose()
        {
            _db.Database.CloseConnection();
            _db.Dispose();
        }

        [Fact]
        public void RecordScore_Saves_To_Database()
        {
            var svc = new LearningAnalyticsService(_db);
            svc.RecordScore(1, "Toán", "Đại số", 8.5, 10.0);

            Assert.Equal(1, _db.LearningAnalytics.Count());
            Assert.Equal(8.5, _db.LearningAnalytics.First().Score);
        }

        [Fact]
        public void GetScoreTimeline_Returns_Ordered()
        {
            var svc = new LearningAnalyticsService(_db);
            svc.RecordScore(1, "Toán", "Đại số", 7, 10);
            svc.RecordScore(1, "Toán", "Hình học", 9, 10);

            var timeline = svc.GetScoreTimeline(1);
            Assert.Equal(2, timeline.Count);
            Assert.True(timeline[0].Date <= timeline[1].Date);
        }

        [Fact]
        public void GetSubjectAverages_Groups_By_Subject()
        {
            var svc = new LearningAnalyticsService(_db);
            svc.RecordScore(1, "Toán", "A", 8, 10);
            svc.RecordScore(1, "Toán", "B", 6, 10);
            svc.RecordScore(1, "Lý", "C", 9, 10);

            var avgs = svc.GetSubjectAverages(1);
            Assert.Equal(2, avgs.Count);
        }

        [Fact]
        public void GetWeakAreas_Finds_Low_Scores()
        {
            var svc = new LearningAnalyticsService(_db);
            svc.RecordScore(1, "Toán", "Đại số", 3, 10); // 30% - weak
            svc.RecordScore(1, "Toán", "Hình học", 9, 10); // 90% - strong

            var weak = svc.GetWeakAreas(1);
            Assert.Single(weak);
            Assert.Equal("Đại số", weak.First().Topic);
        }

        [Fact]
        public void GetTrend_Returns_Status()
        {
            var svc = new LearningAnalyticsService(_db);
            for (int i = 0; i < 5; i++)
                svc.RecordScore(1, "Toán", "T" + i, 5 + i, 10);

            var trend = svc.GetTrend(1);
            Assert.False(string.IsNullOrEmpty(trend));
        }

        [Fact]
        public void FirstQuiz_Awards_Achievement()
        {
            var svc = new LearningAnalyticsService(_db);
            svc.RecordScore(1, "Toán", "A", 8, 10);

            var achievements = svc.GetAchievements(1);
            Assert.Contains(achievements, a => a.AchievementType == "FIRST_QUIZ");
        }

        [Fact]
        public void PerfectScore_Awards_Achievement()
        {
            var svc = new LearningAnalyticsService(_db);
            svc.RecordScore(1, "Toán", "A", 10, 10);

            var achievements = svc.GetAchievements(1);
            Assert.Contains(achievements, a => a.AchievementType == "PERFECT_SCORE");
        }
    }

    public class RubricServiceTests : IDisposable
    {
        private readonly AppDbContext _db;

        public RubricServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;
            _db = new AppDbContext(options);
            _db.Database.OpenConnection();
            _db.Database.EnsureCreated();

            _db.Students.Add(new Student { FullName = "HS 1", StudentCode = "HS001" });
            _db.Students.Add(new Student { FullName = "HS 2", StudentCode = "HS002" });
            _db.SaveChanges();
        }

        public void Dispose()
        {
            _db.Database.CloseConnection();
            _db.Dispose();
        }

        [Fact]
        public void CreateRubric_Saves_With_Criteria()
        {
            var svc = new RubricService(_db);
            var criteria = new System.Collections.Generic.List<(string, double, string, string, string, string)>
            {
                ("Nội dung", 10, "Sai", "Thiếu", "Đủ", "Xuất sắc"),
                ("Trình bày", 5, "Lộn xộn", "TB", "Rõ ràng", "Chuyên nghiệp")
            };

            var rubric = svc.CreateRubric("Bài kiểm tra 1", "Toán", "10", "GV Nguyễn", criteria);

            Assert.True(rubric.Id > 0);
            Assert.Equal(2, rubric.Criteria.Count);
        }

        [Fact]
        public void GetRubric_Returns_WithCriteria()
        {
            var svc = new RubricService(_db);
            var criteria = new System.Collections.Generic.List<(string, double, string, string, string, string)>
            {
                ("Nội dung", 10, "Sai", "Thiếu", "Đủ", "Xuất sắc")
            };
            var created = svc.CreateRubric("Test", "Lý", "11", "GV", criteria);

            var loaded = svc.GetRubric(created.Id);
            Assert.NotNull(loaded);
            Assert.Single(loaded!.Criteria);
        }

        [Fact]
        public void GradeStudent_Saves_Grades()
        {
            var svc = new RubricService(_db);
            var criteria = new System.Collections.Generic.List<(string, double, string, string, string, string)>
            {
                ("Nội dung", 10, "Sai", "Thiếu", "Đủ", "Xuất sắc")
            };
            var rubric = svc.CreateRubric("Test", "Toán", "10", "GV", criteria);
            var criteriaId = rubric.Criteria.First().Id;

            svc.GradeStudent(rubric.Id, 1, "GV Nguyễn", new()
            {
                (criteriaId, 3, 7.5, "Làm tốt")
            });

            Assert.Equal(1, _db.RubricGrades.Count());
            Assert.Equal(7.5, _db.RubricGrades.First().Points);
        }

        [Fact]
        public void GetStudentResult_Returns_Detailed()
        {
            var svc = new RubricService(_db);
            var criteria = new System.Collections.Generic.List<(string, double, string, string, string, string)>
            {
                ("Nội dung", 10, "Sai", "Thiếu", "Đủ", "Xuất sắc"),
                ("Trình bày", 5, "Lộn xộn", "TB", "Rõ ràng", "Chuyên nghiệp")
            };
            var rubric = svc.CreateRubric("KT1", "Toán", "10", "GV", criteria);

            svc.GradeStudent(rubric.Id, 1, "GV", new()
            {
                (rubric.Criteria[0].Id, 4, 10, "Giỏi!"),
                (rubric.Criteria[1].Id, 3, 4, "Khá")
            });

            var result = svc.GetStudentResult(rubric.Id, 1);
            Assert.NotNull(result);
            Assert.Equal(14, result!.TotalPoints);
            Assert.Equal(15, result.MaxTotalPoints);
            Assert.Equal(2, result.CriteriaResults.Count);
        }

        [Fact]
        public void GetClassSummary_Returns_All_Students()
        {
            var svc = new RubricService(_db);
            var criteria = new System.Collections.Generic.List<(string, double, string, string, string, string)>
            {
                ("Nội dung", 10, "Sai", "Thiếu", "Đủ", "Xuất sắc")
            };
            var rubric = svc.CreateRubric("KT", "Toán", "10", "GV", criteria);
            var cId = rubric.Criteria.First().Id;

            svc.GradeStudent(rubric.Id, 1, "GV", new() { (cId, 4, 9, "") });
            svc.GradeStudent(rubric.Id, 2, "GV", new() { (cId, 2, 5, "") });

            var summary = svc.GetClassSummary(rubric.Id);
            Assert.Equal(2, summary.Count);
            Assert.Equal(9, summary.First().TotalPoints); // Sorted descending
        }

        [Fact]
        public void ReGrade_Replaces_OldGrades()
        {
            var svc = new RubricService(_db);
            var criteria = new System.Collections.Generic.List<(string, double, string, string, string, string)>
            {
                ("Nội dung", 10, "Sai", "Thiếu", "Đủ", "Xuất sắc")
            };
            var rubric = svc.CreateRubric("Test", "Toán", "10", "GV", criteria);
            var cId = rubric.Criteria.First().Id;

            svc.GradeStudent(rubric.Id, 1, "GV", new() { (cId, 2, 5, "Lần 1") });
            svc.GradeStudent(rubric.Id, 1, "GV", new() { (cId, 4, 9, "Lần 2") });

            Assert.Equal(1, _db.RubricGrades.Count(g => g.StudentId == 1));
            Assert.Equal(9, _db.RubricGrades.First(g => g.StudentId == 1).Points);
        }
    }

    public class LearningToolsSearchTests
    {
        [Fact]
        public void RemoveAccents_CorrectlyStripsDiacritics()
        {
            string input = "Đại số & Giải tích 12, đường tròn lượng giác, Sinh học phân tử";
            string expected = "Dai so & Giai tich 12, duong tron luong giac, Sinh hoc phan tu";
            string actual = QASmartClass.LearningTools.Helpers.ParsingHelper.RemoveAccents(input);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void RemoveAccents_HandlesNullAndEmpty()
        {
            Assert.Equal(string.Empty, QASmartClass.LearningTools.Helpers.ParsingHelper.RemoveAccents(null!));
            Assert.Equal(string.Empty, QASmartClass.LearningTools.Helpers.ParsingHelper.RemoveAccents(string.Empty));
        }

        [Fact]
        public void Search_IsAccentAndCaseInsensitive()
        {
            var results1 = QASmartClass.LearningTools.Models.ToolRegistry.Search("toan");
            Assert.NotEmpty(results1);
            Assert.Contains(results1, t => t.Name.Contains("Toán"));

            var results2 = QASmartClass.LearningTools.Models.ToolRegistry.Search("tOáN");
            Assert.NotEmpty(results2);
            Assert.Contains(results2, t => t.Name.Contains("Toán"));

            var results3 = QASmartClass.LearningTools.Models.ToolRegistry.Search("vat ly");
            Assert.NotEmpty(results3);
            Assert.Contains(results3, t => t.Name.Contains("Vật Lý") || t.Description.Contains("Vật lý") || t.Tags.Any(tag => tag.Contains("vật lý")));
        }
    }
}
