using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QASmartClass.Data
{
    /// <summary>
    /// Bản ghi phân tích học tập của từng học sinh.
    /// Tự động ghi khi HS nộp quiz, hoàn thành bài tập.
    /// </summary>
    public class LearningAnalytics
    {
        [Key]
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public double Score { get; set; }
        public double MaxScore { get; set; } = 10.0;
        public int? QuizId { get; set; }
        public string AssessmentType { get; set; } = "Quiz"; // Quiz, Homework, Project
        public DateTime RecordedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Huy hiệu / thành tích của học sinh.
    /// Awarded khi đạt streak, perfect score, attendance liên tục.
    /// </summary>
    public class StudentAchievement
    {
        [Key]
        public int Id { get; set; }
        public int StudentId { get; set; }
        /// <summary>Loại: QUIZ_STREAK, PERFECT_SCORE, ATTENDANCE_STREAK, FIRST_QUIZ, TOP_SCORER</summary>
        public string AchievementType { get; set; } = string.Empty;
        public string AchievementName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "🏆";
        public DateTime EarnedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Rubric đánh giá — GV tạo template với nhiều tiêu chí.
    /// </summary>
    public class Rubric
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<RubricCriteria> Criteria { get; set; } = new();
    }

    /// <summary>
    /// Tiêu chí trong rubric — mỗi tiêu chí có 4 mức độ.
    /// </summary>
    public class RubricCriteria
    {
        [Key]
        public int Id { get; set; }
        public int RubricId { get; set; }
        public string CriteriaName { get; set; } = string.Empty;
        public double MaxPoints { get; set; } = 10.0;
        /// <summary>Mô tả mức 1: Chưa đạt (0-25%)</summary>
        public string Level1Desc { get; set; } = "Chưa đạt";
        /// <summary>Mô tả mức 2: Đạt (26-50%)</summary>
        public string Level2Desc { get; set; } = "Đạt";
        /// <summary>Mô tả mức 3: Khá (51-75%)</summary>
        public string Level3Desc { get; set; } = "Khá";
        /// <summary>Mô tả mức 4: Giỏi (76-100%)</summary>
        public string Level4Desc { get; set; } = "Giỏi";
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// Kết quả chấm rubric cho từng HS.
    /// </summary>
    public class RubricGrade
    {
        [Key]
        public int Id { get; set; }
        public int RubricId { get; set; }
        public int StudentId { get; set; }
        public int CriteriaId { get; set; }
        /// <summary>Mức đạt được: 1-4</summary>
        public int Level { get; set; }
        /// <summary>Điểm thực tế</summary>
        public double Points { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public DateTime GradedAt { get; set; } = DateTime.Now;
        public string GradedBy { get; set; } = string.Empty;
    }
}
