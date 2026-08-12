using System;
using System.Collections.Generic;
using System.Linq;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Services
{
    /// <summary>
    /// Service phân tích tiến trình học tập cho học sinh.
    /// Ghi analytics khi HS nộp quiz, xem biểu đồ tiến trình.
    /// </summary>
    public class LearningAnalyticsService
    {
        private readonly AppDbContext _db;

        public LearningAnalyticsService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>Ghi kết quả học tập khi HS nộp quiz/bài tập</summary>
        public void RecordScore(int studentId, string subject, string topic,
            double score, double maxScore, int? quizId = null, string type = "Quiz")
        {
            try
            {
                _db.LearningAnalytics.Add(new LearningAnalytics
                {
                    StudentId = studentId,
                    Subject = subject,
                    Topic = topic,
                    Score = score,
                    MaxScore = maxScore,
                    QuizId = quizId,
                    AssessmentType = type,
                    RecordedAt = DateTime.Now
                });
                _db.SaveChanges();

                // Kiểm tra achievement
                CheckAndAwardAchievements(studentId, score, maxScore);

                Log.Information("[Analytics] Recorded: Student {Id}, {Subject}/{Topic} = {Score}/{Max}",
                    studentId, subject, topic, score, maxScore);
            }
            catch (Exception ex)
            {
                Log.Warning("[Analytics] Record failed: {Err}", ex.Message);
            }
        }

        /// <summary>Lấy điểm theo thời gian cho biểu đồ</summary>
        public List<ScorePoint> GetScoreTimeline(int studentId, string? subject = null)
        {
            var query = _db.LearningAnalytics
                .Where(a => a.StudentId == studentId);

            if (!string.IsNullOrEmpty(subject))
                query = query.Where(a => a.Subject == subject);

            return query
                .OrderBy(a => a.RecordedAt)
                .Select(a => new ScorePoint
                {
                    Date = a.RecordedAt,
                    Score = a.Score,
                    MaxScore = a.MaxScore,
                    Percentage = a.MaxScore > 0 ? (a.Score / a.MaxScore * 100) : 0,
                    Subject = a.Subject,
                    Topic = a.Topic
                })
                .ToList();
        }

        /// <summary>Tính điểm trung bình theo môn</summary>
        public List<SubjectAverage> GetSubjectAverages(int studentId)
        {
            return _db.LearningAnalytics
                .Where(a => a.StudentId == studentId)
                .GroupBy(a => a.Subject)
                .Select(g => new SubjectAverage
                {
                    Subject = g.Key,
                    AverageScore = g.Average(a => a.MaxScore > 0 ? a.Score / a.MaxScore * 100 : 0),
                    TotalAssessments = g.Count(),
                    LatestDate = g.Max(a => a.RecordedAt)
                })
                .OrderByDescending(s => s.AverageScore)
                .ToList();
        }

        /// <summary>Nhận diện chủ đề yếu (điểm dưới 50%)</summary>
        public List<WeakArea> GetWeakAreas(int studentId)
        {
            return _db.LearningAnalytics
                .Where(a => a.StudentId == studentId && a.MaxScore > 0)
                .GroupBy(a => new { a.Subject, a.Topic })
                .Select(g => new WeakArea
                {
                    Subject = g.Key.Subject,
                    Topic = g.Key.Topic,
                    AveragePercentage = g.Average(a => a.Score / a.MaxScore * 100),
                    AttemptCount = g.Count()
                })
                .Where(w => w.AveragePercentage < 50)
                .OrderBy(w => w.AveragePercentage)
                .ToList();
        }

        /// <summary>Lấy xu hướng học tập: Improving / Stable / Declining</summary>
        public string GetTrend(int studentId)
        {
            var recent = _db.LearningAnalytics
                .Where(a => a.StudentId == studentId && a.MaxScore > 0)
                .OrderByDescending(a => a.RecordedAt)
                .Take(10)
                .Select(a => a.Score / a.MaxScore * 100)
                .ToList();

            if (recent.Count < 3) return "Chưa đủ dữ liệu";

            var firstHalf = recent.Skip(recent.Count / 2).Average();
            var secondHalf = recent.Take(recent.Count / 2).Average();
            var diff = secondHalf - firstHalf;

            if (diff > 5) return "📈 Tiến bộ";
            if (diff < -5) return "📉 Cần cải thiện";
            return "📊 Ổn định";
        }

        /// <summary>Lấy danh sách achievement</summary>
        public List<StudentAchievement> GetAchievements(int studentId)
        {
            return _db.StudentAchievements
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.EarnedAt)
                .ToList();
        }

        private void CheckAndAwardAchievements(int studentId, double score, double maxScore)
        {
            try
            {
                // Perfect score
                if (maxScore > 0 && score >= maxScore)
                {
                    AwardIfNew(studentId, "PERFECT_SCORE", "Điểm tuyệt đối! 💯",
                        "Đạt điểm tối đa trong bài kiểm tra", "💯");
                }

                // First quiz
                var totalQuizzes = _db.LearningAnalytics.Count(a => a.StudentId == studentId);
                if (totalQuizzes == 1)
                {
                    AwardIfNew(studentId, "FIRST_QUIZ", "Bài đầu tiên! 🎯",
                        "Hoàn thành bài kiểm tra đầu tiên", "🎯");
                }

                // 10 quizzes streak
                if (totalQuizzes == 10)
                {
                    AwardIfNew(studentId, "QUIZ_10", "10 bài! ⭐",
                        "Đã hoàn thành 10 bài kiểm tra", "⭐");
                }
            }
            catch (Exception ex)
            {
                Log.Debug("[Analytics] Achievement check error: {Err}", ex.Message);
            }
        }

        private void AwardIfNew(int studentId, string type, string name, string desc, string icon)
        {
            var exists = _db.StudentAchievements
                .Any(a => a.StudentId == studentId && a.AchievementType == type);

            if (!exists)
            {
                _db.StudentAchievements.Add(new StudentAchievement
                {
                    StudentId = studentId,
                    AchievementType = type,
                    AchievementName = name,
                    Description = desc,
                    Icon = icon,
                    EarnedAt = DateTime.Now
                });
                _db.SaveChanges();
                Log.Information("[Analytics] Achievement awarded: {Name} → Student {Id}", name, studentId);
            }
        }

        /// <summary>Lấy chuỗi ngày học tập liên tục (streak)</summary>
        public int GetStreak(int studentId)
        {
            try
            {
                var dates = _db.LearningAnalytics
                    .Where(a => a.StudentId == studentId)
                    .Select(a => a.RecordedAt.Date)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .ToList();

                if (dates.Count == 0) return 0;

                int streak = 0;
                if (dates[0] != DateTime.Today && dates[0] != DateTime.Today.AddDays(-1))
                {
                    return 0;
                }

                var expectedDate = dates[0];
                foreach (var date in dates)
                {
                    if (date == expectedDate)
                    {
                        streak++;
                        expectedDate = expectedDate.AddDays(-1);
                    }
                    else
                    {
                        break;
                    }
                }
                return streak;
            }
            catch
            {
                return 0;
            }
        }
    }

    // ─── DTOs ────────────────────────────────────────
    public class ScorePoint
    {
        public DateTime Date { get; set; }
        public double Score { get; set; }
        public double MaxScore { get; set; }
        public double Percentage { get; set; }
        public string Subject { get; set; } = "";
        public string Topic { get; set; } = "";
    }

    public class SubjectAverage
    {
        public string Subject { get; set; } = "";
        public double AverageScore { get; set; }
        public int TotalAssessments { get; set; }
        public DateTime LatestDate { get; set; }
    }

    public class WeakArea
    {
        public string Subject { get; set; } = "";
        public string Topic { get; set; } = "";
        public double AveragePercentage { get; set; }
        public int AttemptCount { get; set; }
    }
}
