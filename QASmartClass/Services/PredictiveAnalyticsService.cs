using QASmartClass.Data;
using System;
using System.Linq;
using System.Collections.Generic;

namespace QASmartClass.Services
{
    /// <summary>
    /// Service phân tích dữ liệu học tập (Early Warning System)
    /// </summary>
    public static class PredictiveAnalyticsService
    {
        public static void RunDailyAnalysis()
        {
            try
            {
                using var db = new AppDbContext();
                var students = db.Students.Where(s => s.Status == "Active").ToList();
                var studentIds = students.Select(s => s.Id).ToList();
                var cutoffDate = DateTime.Now.AddDays(-30);

                // Load all grades, grouping by StudentId and taking 3 most recent in memory
                var allGrades = db.StudentGrades
                    .Where(g => studentIds.Contains(g.StudentId))
                    .ToList();
                var gradesMap = allGrades
                    .GroupBy(g => g.StudentId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.UpdatedAt).Take(3).ToList());

                // Load all leave requests in 30 days
                var allLeaves = db.StudentLeaveRequests
                    .Where(l => studentIds.Contains(l.StudentId) && l.Status == "Approved" && l.CreatedAt >= cutoffDate)
                    .ToList();
                var leavesMap = allLeaves
                    .GroupBy(l => l.StudentId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Load all mental records, keeping only the most recent in memory
                var allMentalRecords = db.StudentMentalHealthRecords
                    .Where(r => studentIds.Contains(r.StudentId))
                    .ToList();
                var mentalMap = allMentalRecords
                    .GroupBy(r => r.StudentId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.RecordedAt).FirstOrDefault());

                foreach (var student in students)
                {
                    bool isAtRisk = false;
                    List<string> riskReasons = new List<string>();

                    // 1. Phân tích điểm số (3 bài gần nhất) từ RAM
                    if (!gradesMap.TryGetValue(student.Id, out var recentGrades))
                    {
                        recentGrades = new List<StudentGrade>();
                    }

                    if (recentGrades.Count >= 2 && recentGrades.All(g => g.Score < 5.0))
                    {
                        isAtRisk = true;
                        riskReasons.Add("Điểm dưới trung bình liên tiếp");
                    }

                    // 2. Phân tích chuyên cần từ RAM
                    if (!leavesMap.TryGetValue(student.Id, out var recentLeaves))
                    {
                        recentLeaves = new List<StudentLeaveRequest>();
                    }

                    if (recentLeaves.Count >= 3)
                    {
                        isAtRisk = true;
                        riskReasons.Add("Vắng mặt quá nhiều trong tháng");
                    }

                    // 3. V7 M2: Phân tích Tâm lý học đường (SEL) từ RAM
                    mentalMap.TryGetValue(student.Id, out var lastRecord);

                    if (lastRecord != null)
                    {
                        // V7 P3.2: Mo rong danh sach keywords nhay cam (30+)
                        string[] sensitiveKeywords = {
                            "buồn bã", "đánh nhau", "tự kỷ", "bắt nạt", "trầm cảm",
                            "tự tử", "muốn chết", "bị cô lập", "tự hại", "cắt tay",
                            "bỏ nhà", "sợ hãi", "lo lắng", "mất ngủ", "không muốn đi học",
                            "bị đe dọa", "bạo lực gia đình", "nghiện game", "khóc nhiều",
                            "thu mình", "không ăn", "hung hăng", "sử dụng chất", "rượu bia",
                            "bị xâm hại", "chán nản", "không có bạn", "giảm cân đột ngột",
                            "uống thuốc", "bỏ học", "stress", "bị đánh"
                        };
                        bool hasSensitiveKeywords = sensitiveKeywords.Any(kw => lastRecord.Notes.Contains(kw, StringComparison.OrdinalIgnoreCase));
                        
                        if (lastRecord.MoodScore <= 3 || hasSensitiveKeywords)
                        {
                            isAtRisk = true;
                            lastRecord.RiskLevel = "High";
                            string detected = string.Join(", ", sensitiveKeywords.Where(kw => lastRecord.Notes.Contains(kw, StringComparison.OrdinalIgnoreCase)));
                            lastRecord.DetectedKeywords = detected;
                            riskReasons.Add($"Cảnh báo tâm lý (Mood: {lastRecord.MoodScore}/10" + (string.IsNullOrEmpty(detected) ? ")" : $", Keywords: {detected})"));

                            // V7 P6.1: Gui thong bao cho bo phan Tu van tam ly
                            if (!lastRecord.IsNotified)
                            {
                                var notifService = new NotificationService(db);
                                notifService.PushNotification("counseling_dept", new NotificationMessage
                                {
                                    Type = "SEL_Alert",
                                    Title = $"Cảnh báo tâm lý mức cao: {student.FullName}",
                                    Content = $"Phát hiện nguy cơ tâm lý. Score: {lastRecord.MoodScore}, Keywords: {detected}"
                                });
                                lastRecord.IsNotified = true;
                                lastRecord.NotifiedAt = DateTime.Now;
                            }
                        }
                    }

                    // Cập nhật Database
                    if (isAtRisk)
                    {
                        student.IsAtRisk = true;
                        student.RiskReason = string.Join(" | ", riskReasons);
                    }
                    else
                    {
                        student.IsAtRisk = false;
                        student.RiskReason = string.Empty;
                    }
                }

                db.SaveChanges();
                AuditHelper.Log(db, "AI_Analysis", "System", "Hoàn tất quét hệ thống cảnh báo sớm");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[PredictiveAnalytics] Error running analysis");
            }
        }
    }
}

