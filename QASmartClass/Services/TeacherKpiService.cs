using QASmartClass.Data;
using System;
using System.Linq;
using System.Collections.Generic;

namespace QASmartClass.Services
{
    public class TeacherKpiResult
    {
        public int TeacherId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        
        // 1. Task Completion
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public double TaskCompletionRate => TotalTasks == 0 ? 0 : Math.Round((double)CompletedTasks / TotalTasks * 100, 1);
        
        // 2. Class Observation Score (Dự giờ)
        public double AverageObservationScore { get; set; } // Out of 4.0
        
        // 3. V7 M3: Value-Added Model (VAM) - Giá trị tăng thêm
        public double VamScore { get; set; }
        
        // 4. Student Feedback (Anonymous Query)
        public double StudentFeedbackScore { get; set; } // Out of 5.0
        
        // FINAL SCORE
        public double FinalRating { get; set; } // Out of 100
        public string RatingLabel { get; set; } = "Cần cố gắng"; // Xuất sắc, Tốt, Khá, Đạt
    }

    public static class TeacherKpiService
    {
        public static List<TeacherKpiResult> CalculateAllKpis()
        {
            using var db = new AppDbContext();
            var results = new List<TeacherKpiResult>();
            
            var teachers = db.TeacherProfiles.Where(t => t.IsActive).ToList();
            if (!teachers.Any())
                return results;
            
            var teacherCodes = teachers.Select(t => t.TeacherCode).ToList();
            var teacherIds = teachers.Select(t => t.Id).ToList();
            var teacherNames = teachers.Select(t => t.FullName).ToList();

            // V7 P4.3: Batch load — only load active teachers' data to optimize RAM
            var allTasks = db.TaskItems.Where(t => teacherCodes.Contains(t.AssignedTo)).ToList();
            var tasksByTeacher = allTasks.GroupBy(t => t.AssignedTo).ToDictionary(g => g.Key, g => g.ToList());

            var allObservations = db.ClassObservations.Where(o => teacherIds.Contains(o.TeacherId)).ToList();
            var obsByTeacher = allObservations.GroupBy(o => o.TeacherId).ToDictionary(g => g.Key, g => g.ToList());

            var allRosters = db.ClassRosters.Where(r => teacherNames.Contains(r.TeacherName)).ToList();
            var rosterIds = allRosters.Select(r => r.Id).ToList();

            var allGrades = db.StudentGrades.Where(g => rosterIds.Contains(g.RosterId)).ToList();

            var allFeedbacks = db.TeacherFeedbacks.Where(f => teacherIds.Contains(f.TeacherId)).ToList();
            var fbByTeacher = allFeedbacks.GroupBy(f => f.TeacherId).ToDictionary(g => g.Key, g => g.ToList());
            
            foreach (var teacher in teachers)
            {
                var kpi = new TeacherKpiResult
                {
                    TeacherId = teacher.Id,
                    FullName = teacher.FullName,
                    Subject = teacher.Subject
                };
                
                // 1. Task Completion (from pre-loaded data)
                var tasks = tasksByTeacher.GetValueOrDefault(teacher.TeacherCode, new List<Data.TaskItem>());
                kpi.TotalTasks = tasks.Count;
                kpi.CompletedTasks = tasks.Count(t => t.Status == "Done");
                
                // 2. Class Observation (from pre-loaded data)
                var observations = obsByTeacher.GetValueOrDefault(teacher.Id, new List<Data.ClassObservation>());
                if (observations.Any())
                {
                    double avgPlan = observations.Average(o => o.LessonPlanScore);
                    double avgDel = observations.Average(o => o.DeliveryScore);
                    double avgEng = observations.Average(o => o.StudentEngagementScore);
                    kpi.AverageObservationScore = Math.Round((avgPlan + avgDel + avgEng) / 3.0, 2);
                }
                else
                {
                    kpi.AverageObservationScore = 3.0; // Default if no data
                }
                
                // 3. VAM Score (V7 P3.3 + P4.3: from pre-loaded data)
                var rosters = allRosters.Where(r => r.TeacherName == teacher.FullName).ToList();
                if (rosters.Any())
                {
                    var localRosterIds = rosters.Select(r => r.Id).ToHashSet();
                    var grades = allGrades.Where(g => localRosterIds.Contains(g.RosterId)).ToList();
                    
                    if (grades.Count > 5)
                    {
                        // VAM thuc te: So sanh diem GK (GradeTypeId=2) vs CK (GradeTypeId=3)
                        var midTermGrades = grades.Where(g => g.GradeTypeId == 2).ToList();
                        var finalGrades = grades.Where(g => g.GradeTypeId == 3).ToList();
                        
                        if (midTermGrades.Any() && finalGrades.Any())
                        {
                            double avgMid = midTermGrades.Average(g => g.Score);
                            double avgFinal = finalGrades.Average(g => g.Score);
                            kpi.VamScore = Math.Round(avgFinal - avgMid, 2); // Duong = HS tien bo
                        }
                        else
                        {
                            // Fallback: dung DTB chung so voi baseline 6.5
                            double avgGrade = grades.Average(g => g.Score);
                            kpi.VamScore = Math.Round((avgGrade - 6.5) * 1.5, 2);
                        }
                    }
                    else
                    {
                        kpi.VamScore = 0; // Chua du du lieu
                    }
                }
                
                // 4. Student Feedback (V7 P3.4 + P4.3: from pre-loaded data)
                var feedbacks = fbByTeacher.GetValueOrDefault(teacher.Id, new List<Data.TeacherFeedback>());
                if (feedbacks.Any())
                    kpi.StudentFeedbackScore = Math.Round(feedbacks.Average(f => f.Score), 1);
                else
                    kpi.StudentFeedbackScore = 3.0; // Default neu chua co feedback
                
                // TÍNH ĐIỂM TỔNG (Final Rating - Out of 100)
                // Trọng số: Task (20%), Observation (30%), VAM (40%), Feedback (10%)
                double taskScore = kpi.TaskCompletionRate; // out of 100
                double obsScore = (kpi.AverageObservationScore / 4.0) * 100; // convert 4.0 to 100
                double vamScoreNorm = 50 + (kpi.VamScore * 10); // normalize VAM: 0 VAM = 50pts. Max 100.
                if (vamScoreNorm > 100) vamScoreNorm = 100;
                if (vamScoreNorm < 0) vamScoreNorm = 0;
                double fbScore = (kpi.StudentFeedbackScore / 5.0) * 100;

                kpi.FinalRating = Math.Round(
                    (taskScore * 0.20) + 
                    (obsScore * 0.30) + 
                    (vamScoreNorm * 0.40) + 
                    (fbScore * 0.10), 1);
                
                if (kpi.FinalRating >= 90) kpi.RatingLabel = "🌟 Xuất sắc";
                else if (kpi.FinalRating >= 80) kpi.RatingLabel = "✅ Tốt";
                else if (kpi.FinalRating >= 65) kpi.RatingLabel = "👍 Khá";
                else if (kpi.FinalRating >= 50) kpi.RatingLabel = "⚠ Đạt";
                else kpi.RatingLabel = "🔴 Cần cải thiện";

                results.Add(kpi);
            }
            
            return results.OrderByDescending(r => r.FinalRating).ToList();
        }
    }
}

