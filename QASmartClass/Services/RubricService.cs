using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Services
{
    /// <summary>
    /// Service quản lý Rubric đánh giá.
    /// GV tạo rubric → chấm bài HS → HS xem feedback chi tiết.
    /// </summary>
    public class RubricService
    {
        private readonly AppDbContext _db;

        public RubricService(AppDbContext db)
        {
            _db = db;
        }

        // ─── Tạo / Sửa Rubric ───

        /// <summary>Tạo rubric mới với tiêu chí</summary>
        public Rubric CreateRubric(string title, string subject, string grade, string createdBy,
            List<(string Name, double MaxPoints, string L1, string L2, string L3, string L4)> criteria)
        {
            var rubric = new Rubric
            {
                Title = title,
                Subject = subject,
                Grade = grade,
                CreatedBy = createdBy,
                CreatedAt = DateTime.Now
            };

            for (int i = 0; i < criteria.Count; i++)
            {
                var c = criteria[i];
                rubric.Criteria.Add(new RubricCriteria
                {
                    CriteriaName = c.Name,
                    MaxPoints = c.MaxPoints,
                    Level1Desc = c.L1,
                    Level2Desc = c.L2,
                    Level3Desc = c.L3,
                    Level4Desc = c.L4,
                    SortOrder = i
                });
            }

            _db.Rubrics.Add(rubric);
            _db.SaveChanges();
            Log.Information("[Rubric] Created: {Title} ({Count} criteria)", title, criteria.Count);
            return rubric;
        }

        /// <summary>Lấy rubric kèm criteria</summary>
        public Rubric? GetRubric(int rubricId)
        {
            return _db.Rubrics
                .Include(r => r.Criteria)
                .FirstOrDefault(r => r.Id == rubricId);
        }

        /// <summary>Lấy tất cả rubric theo môn</summary>
        public List<Rubric> GetRubricsBySubject(string? subject = null)
        {
            var query = _db.Rubrics.Include(r => r.Criteria).AsQueryable();
            if (!string.IsNullOrEmpty(subject))
                query = query.Where(r => r.Subject == subject);
            return query.OrderByDescending(r => r.CreatedAt).ToList();
        }

        // ─── Chấm điểm ───

        /// <summary>GV chấm điểm HS theo rubric</summary>
        public void GradeStudent(int rubricId, int studentId, string gradedBy,
            List<(int CriteriaId, int Level, double Points, string Feedback)> grades)
        {
            // Xóa điểm cũ nếu có (re-grade)
            var existing = _db.RubricGrades
                .Where(g => g.RubricId == rubricId && g.StudentId == studentId)
                .ToList();
            if (existing.Any())
                _db.RubricGrades.RemoveRange(existing);

            foreach (var g in grades)
            {
                _db.RubricGrades.Add(new RubricGrade
                {
                    RubricId = rubricId,
                    StudentId = studentId,
                    CriteriaId = g.CriteriaId,
                    Level = g.Level,
                    Points = g.Points,
                    Feedback = g.Feedback,
                    GradedAt = DateTime.Now,
                    GradedBy = gradedBy
                });
            }

            _db.SaveChanges();
            Log.Information("[Rubric] Graded: Student {SId} on Rubric {RId}, {Count} criteria",
                studentId, rubricId, grades.Count);
        }

        /// <summary>HS xem kết quả rubric chi tiết</summary>
        public RubricResult? GetStudentResult(int rubricId, int studentId)
        {
            var rubric = GetRubric(rubricId);
            if (rubric == null) return null;

            var grades = _db.RubricGrades
                .Where(g => g.RubricId == rubricId && g.StudentId == studentId)
                .ToList();

            if (!grades.Any()) return null;

            return new RubricResult
            {
                RubricTitle = rubric.Title,
                Subject = rubric.Subject,
                CriteriaResults = rubric.Criteria
                    .OrderBy(c => c.SortOrder)
                    .Select(c =>
                    {
                        var grade = grades.FirstOrDefault(g => g.CriteriaId == c.Id);
                        return new CriteriaResult
                        {
                            CriteriaName = c.CriteriaName,
                            MaxPoints = c.MaxPoints,
                            Points = grade?.Points ?? 0,
                            Level = grade?.Level ?? 0,
                            LevelDescription = GetLevelDesc(c, grade?.Level ?? 0),
                            Feedback = grade?.Feedback ?? ""
                        };
                    })
                    .ToList(),
                TotalPoints = grades.Sum(g => g.Points),
                MaxTotalPoints = rubric.Criteria.Sum(c => c.MaxPoints),
                GradedAt = grades.Max(g => g.GradedAt)
            };
        }

        /// <summary>Lấy tổng hợp điểm rubric cho tất cả HS</summary>
        public List<RubricSummary> GetClassSummary(int rubricId)
        {
            var rubric = GetRubric(rubricId);
            if (rubric == null) return new();

            var maxTotal = rubric.Criteria.Sum(c => c.MaxPoints);

            return _db.RubricGrades
                .Where(g => g.RubricId == rubricId)
                .GroupBy(g => g.StudentId)
                .Select(g => new RubricSummary
                {
                    StudentId = g.Key,
                    TotalPoints = g.Sum(x => x.Points),
                    MaxTotalPoints = maxTotal,
                    Percentage = maxTotal > 0 ? g.Sum(x => x.Points) / maxTotal * 100 : 0,
                    CriteriaCount = g.Count()
                })
                .OrderByDescending(s => s.TotalPoints)
                .ToList();
        }

        private static string GetLevelDesc(RubricCriteria c, int level) => level switch
        {
            1 => c.Level1Desc,
            2 => c.Level2Desc,
            3 => c.Level3Desc,
            4 => c.Level4Desc,
            _ => "—"
        };
    }

    // ─── DTOs ────────────────────────────────────────
    public class RubricResult
    {
        public string RubricTitle { get; set; } = "";
        public string Subject { get; set; } = "";
        public List<CriteriaResult> CriteriaResults { get; set; } = new();
        public double TotalPoints { get; set; }
        public double MaxTotalPoints { get; set; }
        public DateTime GradedAt { get; set; }
        public double Percentage => MaxTotalPoints > 0 ? TotalPoints / MaxTotalPoints * 100 : 0;
    }

    public class CriteriaResult
    {
        public string CriteriaName { get; set; } = "";
        public double MaxPoints { get; set; }
        public double Points { get; set; }
        public int Level { get; set; }
        public string LevelDescription { get; set; } = "";
        public string Feedback { get; set; } = "";
    }

    public class RubricSummary
    {
        public int StudentId { get; set; }
        public double TotalPoints { get; set; }
        public double MaxTotalPoints { get; set; }
        public double Percentage { get; set; }
        public int CriteriaCount { get; set; }
    }
}
