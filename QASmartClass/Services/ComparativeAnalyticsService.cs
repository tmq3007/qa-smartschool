using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.Services
{
    public class AcademicComparisonDto
    {
        public double AverageScore1 { get; set; }
        public double AverageScore2 { get; set; }
        public double ScoreDelta { get; set; }
        public string ScoreTrend { get; set; } = "Flat"; // Up, Down, Flat
        
        public int PassCount1 { get; set; }
        public int PassCount2 { get; set; }
        public double PassRate1 { get; set; }
        public double PassRate2 { get; set; }
        public double PassRateDelta { get; set; }
        public string PassRateTrend { get; set; } = "Flat";

        public Dictionary<string, double> ClassificationPercentage1 { get; set; } = new();
        public Dictionary<string, double> ClassificationPercentage2 { get; set; } = new();
    }

    public class AttendanceComparisonDto
    {
        public double AttendanceRate1 { get; set; }
        public double AttendanceRate2 { get; set; }
        public double RateDelta { get; set; }
        public string RateTrend { get; set; } = "Flat"; // Up, Down, Flat
        
        public int TotalPresent1 { get; set; }
        public int TotalPresent2 { get; set; }
        public int TotalAbsent1 { get; set; }
        public int TotalAbsent2 { get; set; }
    }

    public class EmulationComparisonDto
    {
        public double AverageConductDelta1 { get; set; }
        public double AverageConductDelta2 { get; set; }
        public double ConductDeltaDiff { get; set; }
        public string ConductTrend { get; set; } = "Flat";

        public int CommendationCount1 { get; set; }
        public int CommendationCount2 { get; set; }
        public int DisciplineCount1 { get; set; }
        public int DisciplineCount2 { get; set; }
    }

    /// <summary>
    /// WI-08: Service phân tích so sánh dữ liệu học tập, chuyên cần, thi đua giữa các kỳ hoặc tháng.
    /// Giúp Hiệu trưởng đánh giá sự phát triển và biến động chất lượng giáo dục.
    /// </summary>
    public class ComparativeAnalyticsService
    {
        private readonly AppDbContext _db;

        public ComparativeAnalyticsService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// So sánh kết quả học tập của học sinh giữa hai kỳ trong một năm học.
        /// </summary>
        public AcademicComparisonDto CompareAcademicResults(string semester1, string semester2, string schoolYear)
        {
            var dto = new AcademicComparisonDto();
            try
            {
                // Lấy tất cả rosters trong kỳ 1 và kỳ 2 của năm học
                var rosters1 = _db.ClassRosters.Where(r => r.Semester == semester1 && r.SchoolYear == schoolYear).Select(r => r.Id).ToList();
                var rosters2 = _db.ClassRosters.Where(r => r.Semester == semester2 && r.SchoolYear == schoolYear).Select(r => r.Id).ToList();

                var grades1 = _db.StudentGrades.Where(g => rosters1.Contains(g.RosterId)).ToList();
                var grades2 = _db.StudentGrades.Where(g => rosters2.Contains(g.RosterId)).ToList();

                // Kỳ 1
                if (grades1.Any())
                {
                    dto.AverageScore1 = Math.Round(grades1.Average(g => g.Score), 2);
                    dto.PassCount1 = grades1.Count(g => g.Score >= 5.0);
                    dto.PassRate1 = Math.Round((double)dto.PassCount1 / grades1.Count * 100.0, 2);
                    dto.ClassificationPercentage1 = CalculateClassificationDistribution(grades1);
                }
                else
                {
                    dto.AverageScore1 = 0.0;
                    dto.PassRate1 = 0.0;
                }

                // Kỳ 2
                if (grades2.Any())
                {
                    dto.AverageScore2 = Math.Round(grades2.Average(g => g.Score), 2);
                    dto.PassCount2 = grades2.Count(g => g.Score >= 5.0);
                    dto.PassRate2 = Math.Round((double)dto.PassCount2 / grades2.Count * 100.0, 2);
                    dto.ClassificationPercentage2 = CalculateClassificationDistribution(grades2);
                }
                else
                {
                    dto.AverageScore2 = 0.0;
                    dto.PassRate2 = 0.0;
                }

                // Delta & Trends (chống chia cho 0 hoặc lỗi nếu không có dữ liệu)
                dto.ScoreDelta = Math.Round(dto.AverageScore2 - dto.AverageScore1, 2);
                dto.ScoreTrend = dto.ScoreDelta > 0.05 ? "Up" : (dto.ScoreDelta < -0.05 ? "Down" : "Flat");

                dto.PassRateDelta = Math.Round(dto.PassRate2 - dto.PassRate1, 2);
                dto.PassRateTrend = dto.PassRateDelta > 0.5 ? "Up" : (dto.PassRateDelta < -0.5 ? "Down" : "Flat");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ComparativeAnalyticsService: CompareAcademicResults failed");
            }
            return dto;
        }

        /// <summary>
        /// So sánh chuyên cần của học sinh giữa hai kỳ trong một năm học.
        /// </summary>
        public AttendanceComparisonDto CompareAttendance(string semester1, string semester2, string schoolYear)
        {
            var dto = new AttendanceComparisonDto();
            try
            {
                var rosters1 = _db.ClassRosters.Where(r => r.Semester == semester1 && r.SchoolYear == schoolYear).Select(r => r.Id).ToList();
                var rosters2 = _db.ClassRosters.Where(r => r.Semester == semester2 && r.SchoolYear == schoolYear).Select(r => r.Id).ToList();

                var att1 = _db.AttendanceRecords.Where(a => rosters1.Contains(a.RosterId)).ToList();
                var att2 = _db.AttendanceRecords.Where(a => rosters2.Contains(a.RosterId)).ToList();

                // Kỳ 1
                if (att1.Any())
                {
                    dto.TotalPresent1 = att1.Count(a => a.Status.Equals("present", StringComparison.OrdinalIgnoreCase) || a.Status.Equals("late", StringComparison.OrdinalIgnoreCase));
                    dto.TotalAbsent1 = att1.Count(a => a.Status.Equals("absent", StringComparison.OrdinalIgnoreCase));
                    dto.AttendanceRate1 = Math.Round((double)dto.TotalPresent1 / att1.Count * 100.0, 2);
                }

                // Kỳ 2
                if (att2.Any())
                {
                    dto.TotalPresent2 = att2.Count(a => a.Status.Equals("present", StringComparison.OrdinalIgnoreCase) || a.Status.Equals("late", StringComparison.OrdinalIgnoreCase));
                    dto.TotalAbsent2 = att2.Count(a => a.Status.Equals("absent", StringComparison.OrdinalIgnoreCase));
                    dto.AttendanceRate2 = Math.Round((double)dto.TotalPresent2 / att2.Count * 100.0, 2);
                }

                dto.RateDelta = Math.Round(dto.AttendanceRate2 - dto.AttendanceRate1, 2);
                dto.RateTrend = dto.RateDelta > 0.1 ? "Up" : (dto.RateDelta < -0.1 ? "Down" : "Flat");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ComparativeAnalyticsService: CompareAttendance failed");
            }
            return dto;
        }

        /// <summary>
        /// So sánh chuyên cần của học sinh giữa hai tháng.
        /// </summary>
        public AttendanceComparisonDto CompareAttendanceByMonths(int month1, int year1, int month2, int year2)
        {
            var dto = new AttendanceComparisonDto();
            try
            {
                var att1 = _db.AttendanceRecords.Where(a => a.Date.Month == month1 && a.Date.Year == year1).ToList();
                var att2 = _db.AttendanceRecords.Where(a => a.Date.Month == month2 && a.Date.Year == year2).ToList();

                // Tháng 1
                if (att1.Any())
                {
                    dto.TotalPresent1 = att1.Count(a => a.Status.Equals("present", StringComparison.OrdinalIgnoreCase) || a.Status.Equals("late", StringComparison.OrdinalIgnoreCase));
                    dto.TotalAbsent1 = att1.Count(a => a.Status.Equals("absent", StringComparison.OrdinalIgnoreCase));
                    dto.AttendanceRate1 = Math.Round((double)dto.TotalPresent1 / att1.Count * 100.0, 2);
                }

                // Tháng 2
                if (att2.Any())
                {
                    dto.TotalPresent2 = att2.Count(a => a.Status.Equals("present", StringComparison.OrdinalIgnoreCase) || a.Status.Equals("late", StringComparison.OrdinalIgnoreCase));
                    dto.TotalAbsent2 = att2.Count(a => a.Status.Equals("absent", StringComparison.OrdinalIgnoreCase));
                    dto.AttendanceRate2 = Math.Round((double)dto.TotalPresent2 / att2.Count * 100.0, 2);
                }

                dto.RateDelta = Math.Round(dto.AttendanceRate2 - dto.AttendanceRate1, 2);
                dto.RateTrend = dto.RateDelta > 0.1 ? "Up" : (dto.RateDelta < -0.1 ? "Down" : "Flat");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ComparativeAnalyticsService: CompareAttendanceByMonths failed");
            }
            return dto;
        }

        /// <summary>
        /// So sánh kết quả thi đua và rèn luyện (hạnh kiểm, khen thưởng, kỷ luật) giữa hai kỳ.
        /// </summary>
        public EmulationComparisonDto CompareEmulation(string semester1, string semester2, string schoolYear)
        {
            var dto = new EmulationComparisonDto();
            try
            {
                var rosters1 = _db.ClassRosters.Where(r => r.Semester == semester1 && r.SchoolYear == schoolYear).Select(r => r.Id).ToList();
                var rosters2 = _db.ClassRosters.Where(r => r.Semester == semester2 && r.SchoolYear == schoolYear).Select(r => r.Id).ToList();

                var conduct1 = _db.ConductRecords.Where(c => rosters1.Contains(c.RosterId)).ToList();
                var conduct2 = _db.ConductRecords.Where(c => rosters2.Contains(c.RosterId)).ToList();

                // Khen thưởng
                var awards1 = _db.AwardRecords.Where(a => a.Semester == semester1 && a.SchoolYear == schoolYear && a.Status == "Approved").ToList();
                var awards2 = _db.AwardRecords.Where(a => a.Semester == semester2 && a.SchoolYear == schoolYear && a.Status == "Approved").ToList();

                // Kỷ luật
                var disciplines = _db.DisciplineRecords.Where(d => d.Status == "Approved").ToList();
                
                // Kỳ 1
                if (conduct1.Any())
                {
                    dto.AverageConductDelta1 = Math.Round(conduct1.Average(c => c.PointsDelta), 2);
                }
                dto.CommendationCount1 = awards1.Count;
                dto.DisciplineCount1 = disciplines.Count(d => d.Status == "Approved" && IsInSemesterPeriod(d.Date, semester1));

                // Kỳ 2
                if (conduct2.Any())
                {
                    dto.AverageConductDelta2 = Math.Round(conduct2.Average(c => c.PointsDelta), 2);
                }
                dto.CommendationCount2 = awards2.Count;
                dto.DisciplineCount2 = disciplines.Count(d => d.Status == "Approved" && IsInSemesterPeriod(d.Date, semester2));

                dto.ConductDeltaDiff = Math.Round(dto.AverageConductDelta2 - dto.AverageConductDelta1, 2);
                dto.ConductTrend = dto.ConductDeltaDiff > 0.1 ? "Up" : (dto.ConductDeltaDiff < -0.1 ? "Down" : "Flat");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ComparativeAnalyticsService: CompareEmulation failed");
            }
            return dto;
        }

        private bool IsInSemesterPeriod(DateTime date, string semester)
        {
            int month = date.Month;
            if (semester == "HK1")
            {
                return month >= 8 || month <= 1; // HK1 thường từ tháng 8 đến tháng 1
            }
            else // HK2
            {
                return month >= 1 && month <= 7; // HK2 thường từ tháng 1 đến tháng 7
            }
        }

        private Dictionary<string, double> CalculateClassificationDistribution(List<StudentGrade> grades)
        {
            var dict = new Dictionary<string, double>
            {
                { "Giỏi", 0.0 },
                { "Khá", 0.0 },
                { "Trung bình", 0.0 },
                { "Yếu", 0.0 },
                { "Kém", 0.0 }
            };

            if (!grades.Any()) return dict;

            int gioi = 0, kha = 0, tb = 0, yeu = 0, kem = 0;
            foreach (var g in grades)
            {
                double s = g.Score;
                if (s >= 8.0) gioi++;
                else if (s >= 6.5) kha++;
                else if (s >= 5.0) tb++;
                else if (s >= 3.5) yeu++;
                else kem++;
            }

            int total = grades.Count;
            dict["Giỏi"] = Math.Round((double)gioi / total * 100.0, 2);
            dict["Khá"] = Math.Round((double)kha / total * 100.0, 2);
            dict["Trung bình"] = Math.Round((double)tb / total * 100.0, 2);
            dict["Yếu"] = Math.Round((double)yeu / total * 100.0, 2);
            dict["Kém"] = Math.Round((double)kem / total * 100.0, 2);

            return dict;
        }
    }
}

