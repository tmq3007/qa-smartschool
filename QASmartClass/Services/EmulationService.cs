using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.Services
{
    public class ClassRankDTO
    {
        public int Rank { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public double ConductScore { get; set; }
        public double AcademicScore { get; set; }
        public double TotalScore { get; set; }
    }

    public class TeacherEmulationDTO
    {
        public string TeacherName { get; set; } = string.Empty;
        public double OnTimeTaskRate { get; set; }
        public int CompletedLessons { get; set; }
        public double ObservationScore { get; set; }
        public double TotalScore { get; set; }
    }

    // ===== WI-15 DTOs =====
    public class StudentEmulationDTO
    {
        public int Rank { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public double GPA { get; set; }
        public int ConductScore { get; set; }
        public string ConductClassification { get; set; } = string.Empty;
        public string AcademicClassification { get; set; } = string.Empty;
        /// <summary>HSG (H?c sinh Giỏi), HSTT (Tiên ti?n), HSTB (Ti?n b?), Chưa đạt</summary>
        public string EmulationTitle { get; set; } = string.Empty;
        public double TotalScore { get; set; }
    }

    public class ClassEmulationDTO
    {
        public int Rank { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public double AvgGPA { get; set; }
        public double AvgConduct { get; set; }
        public double AttendanceRate { get; set; }
        public int HSGCount { get; set; }
        public int HSTTCount { get; set; }
        public double TotalScore { get; set; }
        /// <summary>L?p Xuất sắc / Lớp Tiên tiến / Lớp Hoàn thành / Chưa đạt</summary>
        public string ClassTitle { get; set; } = string.Empty;
    }

    public class EmulationCompareDTO
    {
        public string Label { get; set; } = string.Empty;
        public double ValueHK1 { get; set; }
        public double ValueHK2 { get; set; }
        public double Delta { get; set; }
        public string Trend { get; set; } = "Flat"; // Up / Down / Flat
    }

    public class EmulationService
    {
        private readonly AppDbContext _db;

        public EmulationService(AppDbContext db)
        {
            _db = db;
        }

        // ------------------------------------------------------------
        //  EXISTING: CalcClassRanking + CalcTeacherEmulation (monthly)
        // ------------------------------------------------------------

        public List<ClassRankDTO> CalcClassRanking(int month, int year)
        {
            try
            {
                var rosters = _db.ClassRosters.ToList();
                if (rosters.Count == 0) return GetMockClassRanking();

                var result = new List<ClassRankDTO>();
                foreach (var roster in rosters)
                {
                    var totalRecords = _db.AttendanceRecords
                        .Count(a => a.RosterId == roster.Id && a.Date.Month == month && a.Date.Year == year);
                    var presentRecords = _db.AttendanceRecords
                        .Count(a => a.RosterId == roster.Id && a.Date.Month == month && a.Date.Year == year && a.Status == "Present");
                    double conductScore = totalRecords > 0 ? (double)presentRecords / totalRecords * 100 : 90;

                    var studentIds = _db.Students.Where(s => s.ClassName == roster.ClassName).Select(s => s.Id).ToList();
                    var avgGrade = studentIds.Count > 0
                        ? _db.StudentGrades.Where(g => studentIds.Contains(g.StudentId)).Select(g => g.Score).DefaultIfEmpty(7.0).Average()
                        : 7.0;

                    double total = conductScore * 0.4 + avgGrade * 10 * 0.6;
                    result.Add(new ClassRankDTO
                    {
                        ClassName = roster.ClassName,
                        ConductScore = Math.Round(conductScore, 1),
                        AcademicScore = Math.Round(avgGrade, 1),
                        TotalScore = Math.Round(total, 1)
                    });
                }

                return result.OrderByDescending(r => r.TotalScore)
                    .Select((r, i) => { r.Rank = i + 1; return r; }).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi tính toán Class Ranking");
                return GetMockClassRanking();
            }
        }

        public List<TeacherEmulationDTO> CalcTeacherEmulation(int month, int year)
        {
            try
            {
                var plans = _db.LessonPlans.Where(p => p.CreatedAt.Month == month && p.CreatedAt.Year == year).ToList();
                if (plans.Count == 0) return GetMockTeacherEmulation();

                var grouped = plans.GroupBy(p => p.TeacherId).Select(g => new TeacherEmulationDTO
                {
                    TeacherName = g.Key ?? "N/A",
                    CompletedLessons = g.Count(p => p.Status == "Done"),
                    OnTimeTaskRate = g.Count() > 0 ? Math.Round((double)g.Count(p => p.Status == "Done") / g.Count() * 100, 1) : 0,
                    ObservationScore = 8.0 + (g.Count(p => p.Status == "Done") > 10 ? 1.5 : g.Count(p => p.Status == "Done") * 0.15),
                    TotalScore = 0
                }).ToList();

                foreach (var t in grouped)
                    t.TotalScore = Math.Round(t.OnTimeTaskRate * 0.3 + t.ObservationScore * 10 * 0.4 + t.CompletedLessons * 0.3, 1);

                return grouped.OrderByDescending(t => t.TotalScore).Take(5).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi tính toán Teacher Emulation");
                return GetMockTeacherEmulation();
            }
        }

        // ------------------------------------------------------------
        //  WI-15: CalcStudentEmulation — HSG/HSTT/HSTB/Chưa đạt
        //  FIX C-02/C-03: Batch preload — giảm t? ~4000 queries xu?ng ~5
        // ------------------------------------------------------------

        /// <summary>
        /// WI-15: T?ng h?p thi dua HS theo học kỳ.
        /// HSG: GPA = 8.0 + H?nh ki?m T?t
        /// HSTT: GPA = 6.5 + H?nh ki?m Khá tr? lên
        /// HSTB: GPA = 5.0 + H?nh ki?m TB tr? lên
        /// Chưa đạt: Còn lại
        /// </summary>
        public List<StudentEmulationDTO> CalcStudentEmulation(string semester, string schoolYear)
        {
            try
            {
                var students = _db.Students.Where(s => s.Status == "Active").ToList();
                if (!students.Any()) return new List<StudentEmulationDTO>();

                // FIX C-02/C-03: Preload T?T C? data c?n thi?t trong 4 queries thay v́ N+1
                var semesterRosters = _db.ClassRosters
                    .Where(r => r.SchoolYear == schoolYear && r.Semester == semester && r.IsActive)
                    .ToList();
                var semesterRosterIds = semesterRosters.Select(r => r.Id).ToList();

                var allRosterStudents = _db.ClassRosterStudents
                    .Where(crs => semesterRosterIds.Contains(crs.RosterId))
                    .ToList();

                var allGrades = _db.StudentGrades
                    .Where(g => semesterRosterIds.Contains(g.RosterId))
                    .ToList();

                // Build lookup: StudentId ? List<RosterId>
                var studentRosterMap = allRosterStudents
                    .GroupBy(crs => crs.StudentId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.RosterId).ToList());

                // Build lookup: (StudentId, RosterId) ? List<StudentGrade>
                var gradeMap = allGrades
                    .GroupBy(g => (g.StudentId, g.RosterId))
                    .ToDictionary(g => g.Key, g => g.ToList());

                var result = new List<StudentEmulationDTO>();

                foreach (var student in students)
                {
                    // Calculate GPA in-memory (no DB queries in this loop!)
                    var subjectAvgs = new List<double>();

                    if (studentRosterMap.TryGetValue(student.Id, out var rosterIds))
                    {
                        foreach (var rosterId in rosterIds)
                        {
                            if (gradeMap.TryGetValue((student.Id, rosterId), out var grades) && grades.Any())
                            {
                                // TT22 weighted average (in-memory)
                                double totalWeighted = 0;
                                int totalWeight = 0;
                                foreach (var g in grades)
                                {
                                    int weight = GetGradeWeight(g.GradeTypeId);
                                    totalWeighted += g.Score * weight;
                                    totalWeight += weight;
                                }
                                if (totalWeight > 0)
                                    subjectAvgs.Add(Math.Round(totalWeighted / totalWeight, 2));
                            }
                        }
                    }

                    double gpa = subjectAvgs.Any() ? Math.Round(subjectAvgs.Average(), 2) : 5.0;
                    string academicClass = TT22GradingService.ClassifyAcademic(gpa);
                    string conductClass = TT22GradingService.ClassifyConduct(student.ConductScore);

                    // Determine emulation title
                    string title = ClassifyEmulation(gpa, student.ConductScore);

                    // Total score = GPA * 60% + ConductScore/10 * 40%
                    double totalScore = Math.Round(gpa * 0.6 + (double)student.ConductScore / 10.0 * 0.4, 2);

                    result.Add(new StudentEmulationDTO
                    {
                        StudentId = student.Id,
                        StudentName = student.FullName,
                        ClassName = student.ClassName,
                        GPA = gpa,
                        ConductScore = student.ConductScore,
                        ConductClassification = conductClass,
                        AcademicClassification = academicClass,
                        EmulationTitle = title,
                        TotalScore = totalScore
                    });
                }

                return result.OrderByDescending(r => r.TotalScore)
                    .Select((r, i) => { r.Rank = i + 1; return r; }).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[EmulationService] Lỗi CalcStudentEmulation");
                return new List<StudentEmulationDTO>();
            }
        }

        // ------------------------------------------------------------
        //  WI-15: CalcClassEmulation — L?p Xuất sắc / Tiên ti?n / etc.
        // ------------------------------------------------------------

        /// <summary>
        /// WI-15: T?ng h?p thi dua L?p theo học kỳ.
        /// L?p Xuất sắc: Avg GPA = 7.5 + Avg Conduct = 85 + HSG = 30%
        /// Lớp Tiên tiến: Avg GPA = 6.5 + Avg Conduct = 75
        /// Lớp Hoàn thành: Avg GPA = 5.0
        /// Chưa đạt: Còn lại
        /// </summary>
        public List<ClassEmulationDTO> CalcClassEmulation(string semester, string schoolYear)
        {
            try
            {
                var classNames = _db.Students.Where(s => s.Status == "Active")
                    .Select(s => s.ClassName).Distinct().ToList();

                if (!classNames.Any()) return new List<ClassEmulationDTO>();

                var studentEmulations = CalcStudentEmulation(semester, schoolYear);
                var result = new List<ClassEmulationDTO>();

                foreach (var className in classNames)
                {
                    var classStudents = studentEmulations.Where(s => s.ClassName == className).ToList();
                    if (!classStudents.Any()) continue;

                    int count = classStudents.Count;
                    double avgGPA = Math.Round(classStudents.Average(s => s.GPA), 2);
                    double avgConduct = Math.Round(classStudents.Average(s => s.ConductScore), 1);
                    int hsgCount = classStudents.Count(s => s.EmulationTitle == "HSG");
                    int hsttCount = classStudents.Count(s => s.EmulationTitle == "HSTT");

                    // Attendance rate for the semester
                    var studentIds = classStudents.Select(s => s.StudentId).ToList();
                    var rosterIds = _db.ClassRosters
                        .Where(r => r.SchoolYear == schoolYear && r.Semester == semester && r.IsActive)
                        .Select(r => r.Id).ToList();
                    int totalAtt = _db.AttendanceRecords
                        .Count(a => studentIds.Contains(a.StudentId) && rosterIds.Contains(a.RosterId));
                    int presentAtt = _db.AttendanceRecords
                        .Count(a => studentIds.Contains(a.StudentId) && rosterIds.Contains(a.RosterId)
                            && (a.Status == "Có m?t" || a.Status == "Present" || a.Status == "Tr?"));
                    double attRate = totalAtt > 0 ? Math.Round((double)presentAtt / totalAtt * 100, 1) : 95.0;

                    // Class emulation title
                    double hsgPercent = count > 0 ? (double)hsgCount / count * 100 : 0;
                    string classTitle = ClassifyClassEmulation(avgGPA, avgConduct, hsgPercent);

                    double totalScore = Math.Round(avgGPA * 0.5 + avgConduct / 10.0 * 0.3 + attRate / 10.0 * 0.2, 2);

                    result.Add(new ClassEmulationDTO
                    {
                        ClassName = className,
                        StudentCount = count,
                        AvgGPA = avgGPA,
                        AvgConduct = avgConduct,
                        AttendanceRate = attRate,
                        HSGCount = hsgCount,
                        HSTTCount = hsttCount,
                        TotalScore = totalScore,
                        ClassTitle = classTitle
                    });
                }

                return result.OrderByDescending(r => r.TotalScore)
                    .Select((r, i) => { r.Rank = i + 1; return r; }).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[EmulationService] Lỗi CalcClassEmulation");
                return new List<ClassEmulationDTO>();
            }
        }

        // ------------------------------------------------------------
        //  WI-15: CompareEmulationBySemester — HK1 vs HK2
        // ------------------------------------------------------------

        /// <summary>
        /// WI-15: So sánh thi dua HS gi?a HK1 và HK2 (dùng ComparativeAnalyticsService concept).
        /// </summary>
        public List<EmulationCompareDTO> CompareEmulationBySemester(string schoolYear)
        {
            try
            {
                var hk1Students = CalcStudentEmulation("HK1", schoolYear);
                var hk2Students = CalcStudentEmulation("HK2", schoolYear);

                var comparisons = new List<EmulationCompareDTO>();

                // HSG count
                int hsg1 = hk1Students.Count(s => s.EmulationTitle == "HSG");
                int hsg2 = hk2Students.Count(s => s.EmulationTitle == "HSG");
                comparisons.Add(CreateCompare("H?c sinh Giỏi", hsg1, hsg2));

                // HSTT count
                int hstt1 = hk1Students.Count(s => s.EmulationTitle == "HSTT");
                int hstt2 = hk2Students.Count(s => s.EmulationTitle == "HSTT");
                comparisons.Add(CreateCompare("HS Tiên ti?n", hstt1, hstt2));

                // Average GPA
                double avgGpa1 = hk1Students.Any() ? Math.Round(hk1Students.Average(s => s.GPA), 2) : 0;
                double avgGpa2 = hk2Students.Any() ? Math.Round(hk2Students.Average(s => s.GPA), 2) : 0;
                comparisons.Add(CreateCompare("GPA trung bình", avgGpa1, avgGpa2));

                // Average Conduct
                double avgC1 = hk1Students.Any() ? Math.Round(hk1Students.Average(s => s.ConductScore), 1) : 0;
                double avgC2 = hk2Students.Any() ? Math.Round(hk2Students.Average(s => s.ConductScore), 1) : 0;
                comparisons.Add(CreateCompare("H?nh ki?m TB", avgC1, avgC2));

                // Chưa đạt count
                int fail1 = hk1Students.Count(s => s.EmulationTitle == "Chưa đạt");
                int fail2 = hk2Students.Count(s => s.EmulationTitle == "Chưa đạt");
                comparisons.Add(CreateCompare("Chưa đạt", fail1, fail2));

                return comparisons;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[EmulationService] Lỗi CompareEmulationBySemester");
                return new List<EmulationCompareDTO>();
            }
        }

        // ------------------------------------------------------------
        //  HELPERS
        // ------------------------------------------------------------

        /// <summary>
        /// X?p lo?i thi dua HS:
        /// HSG: GPA = 8.0 + H?nh ki?m T?t (=80)
        /// HSTT: GPA = 6.5 + H?nh ki?m Khá (=65)
        /// HSTB: GPA = 5.0 + H?nh ki?m TB (=50)
        /// Chưa đạt: Còn lại
        /// </summary>
        public static string ClassifyEmulation(double gpa, int conductScore)
        {
            string conduct = TT22GradingService.ClassifyConduct(conductScore);
            if (gpa >= 8.0 && conduct == "T?t") return "HSG";
            if (gpa >= 6.5 && (conduct == "T?t" || conduct == "Khá")) return "HSTT";
            if (gpa >= 5.0 && (conduct == "T?t" || conduct == "Khá" || conduct == "Trung bình")) return "HSTB";
            return "Chưa đạt";
        }

        /// <summary>
        /// X?p lo?i thi dua Lớp:
        /// L?p Xuất sắc: AvgGPA = 7.5 + AvgConduct = 85 + HSG = 30%
        /// Lớp Tiên tiến: AvgGPA = 6.5 + AvgConduct = 75
        /// Lớp Hoàn thành: AvgGPA = 5.0
        /// Chưa đạt: Còn lại
        /// </summary>
        public static string ClassifyClassEmulation(double avgGPA, double avgConduct, double hsgPercent)
        {
            if (avgGPA >= 7.5 && avgConduct >= 85 && hsgPercent >= 30) return "L?p Xuất sắc";
            if (avgGPA >= 6.5 && avgConduct >= 75) return "Lớp Tiên tiến";
            if (avgGPA >= 5.0) return "Lớp Hoàn thành";
            return "Chưa đạt";
        }

        /// <summary>
        /// H? s? di?m theo TT22 (gi?ng TT22GradingService.GetWeight).
        /// TX=1, GK=2, CK=3
        /// </summary>
        private static int GetGradeWeight(int gradeTypeId)
        {
            return gradeTypeId switch
            {
                1 => 1, // Thu?ng xuyên
                2 => 2, // Gi?a k?
                3 => 3, // Cu?i k?
                _ => 1  // Default
            };
        }

        private static EmulationCompareDTO CreateCompare(string label, double v1, double v2)
        {
            double delta = Math.Round(v2 - v1, 2);
            return new EmulationCompareDTO
            {
                Label = label,
                ValueHK1 = v1,
                ValueHK2 = v2,
                Delta = delta,
                Trend = delta > 0.05 ? "Up" : (delta < -0.05 ? "Down" : "Flat")
            };
        }

        private List<ClassRankDTO> GetMockClassRanking() => new()
        {
            new() { Rank = 1, ClassName = "10A1", ConductScore = 95, AcademicScore = 8.5, TotalScore = 90.2 },
            new() { Rank = 2, ClassName = "11A3", ConductScore = 90, AcademicScore = 8.8, TotalScore = 88.5 },
            new() { Rank = 3, ClassName = "12A1", ConductScore = 85, AcademicScore = 9.0, TotalScore = 88.0 },
        };

        private List<TeacherEmulationDTO> GetMockTeacherEmulation() => new()
        {
            new() { TeacherName = "GV. Nguy?n Van A", OnTimeTaskRate = 98.5, CompletedLessons = 16, ObservationScore = 9.2, TotalScore = 94.5 },
            new() { TeacherName = "GV. Tr?n Th? B", OnTimeTaskRate = 95.0, CompletedLessons = 14, ObservationScore = 8.8, TotalScore = 91.0 },
        };
    }
}

