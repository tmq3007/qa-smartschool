using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.Services
{
    public class AtRiskStudentDTO
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = "Medium"; // Low, Medium, High
    }

    /// <summary>
    /// Early Warning System â€” phÃ¡t hiá»‡n HS cáº§n can thiá»‡p sá»›m
    /// TiÃªu chÃ­: Váº¯ng >5 buá»•i/thÃ¡ng HOáº¶C TB <5.0 HOáº¶C háº¡nh kiá»ƒm <70
    /// </summary>
    public class EarlyWarningService
    {
        private readonly AppDbContext _db;

        public EarlyWarningService(AppDbContext db) { _db = db; }

        public List<AtRiskStudentDTO> GetAtRiskStudents(int month, int year)
        {
            var result = new List<AtRiskStudentDTO>();
            try
            {
                var v1 = "vắng".Normalize(System.Text.NormalizationForm.FormC);
                var v2 = "vắng".Normalize(System.Text.NormalizationForm.FormD);
                var v3 = "VẮNG".Normalize(System.Text.NormalizationForm.FormC);
                var v4 = "VẮNG".Normalize(System.Text.NormalizationForm.FormD);
                var v5 = "Vắng".Normalize(System.Text.NormalizationForm.FormC);
                var v6 = "Vắng".Normalize(System.Text.NormalizationForm.FormD);

                var attendanceLookup = _db.AttendanceRecords
                    .Where(a => a.Date.Month == month && a.Date.Year == year && 
                                (a.Status != null && (a.Status.ToLower() == "absent" || 
                                                     a.Status.ToLower() == v1 || a.Status.ToLower() == v2 ||
                                                     a.Status == v3 || a.Status == v4 ||
                                                     a.Status == v5 || a.Status == v6)))
                    .GroupBy(a => a.StudentId)
                    .Select(g => new { StudentId = g.Key, AbsentCount = g.Count() })
                    .ToDictionary(x => x.StudentId, x => x.AbsentCount);

                var activeRosterIds = _db.ClassRosters.Where(r => r.IsActive).Select(r => r.Id).ToList();

                var gradeLookup = _db.StudentGrades
                    .Where(g => activeRosterIds.Contains(g.RosterId))
                    .GroupBy(g => g.StudentId)
                    .Select(g => new { StudentId = g.Key, AvgScore = g.Average(x => x.Score) })
                    .ToDictionary(x => x.StudentId, x => x.AvgScore);

                var students = _db.Students.ToList();

                foreach (var s in students)
                {
                    var reasons = new List<string>();

                    // 1. Vắng >5 buổi trong tháng
                    if (attendanceLookup.TryGetValue(s.Id, out int absentCount) && absentCount > 5)
                        reasons.Add($"Vắng {absentCount} buổi");

                    // 2. Điểm TB < 5.0
                    if (gradeLookup.TryGetValue(s.Id, out double avg) && avg < 5.0)
                        reasons.Add($"Điểm TB {avg:F1}/10");

                    // 3. Hạnh kiểm < 70
                    if (s.ConductScore < 70)
                        reasons.Add($"Hạnh kiểm {s.ConductScore}");

                    if (reasons.Any())
                    {
                        result.Add(new AtRiskStudentDTO
                        {
                            StudentId = s.Id,
                            StudentName = s.FullName,
                            ClassName = s.ClassName,
                            Reason = string.Join(" | ", reasons),
                            RiskLevel = reasons.Count >= 2 ? "High" : "Medium"
                        });
                    }
                }

                Log.Information("[EarlyWarning] Found {Count} at-risk students for {Month}/{Year}",
                    result.Count, month, year);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "EarlyWarning: Error analyzing students");
                throw;
            }

            return result.OrderByDescending(r => r.RiskLevel == "High" ? 1 : 0).ToList();
        }
    }
}

