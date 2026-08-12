using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace QASmartClass.Services
{
    public class NonCompliantTeacherDto
    {
        public string TeacherName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int Period { get; set; }
        public string Room { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class ComplianceTrendDto
    {
        public string WeekName { get; set; } = string.Empty;
        public double ComplianceRate { get; set; }
    }

    /// <summary>
    /// WI-07: Service ki?m tra và dánh giá t? l? tuân th? th?i khóa bi?u c?a giáo viên.
    /// So sánh TimetableEntries (k? ho?ch) và UsageLogs (ho?t d?ng th?c t?).
    /// </summary>
    public class TimetableComplianceService
    {
        private readonly AppDbContext _db;

        public TimetableComplianceService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// So sánh TimetableEntries và UsageLogs trong m?t ngày d? tính t? l? tuân th?.
        /// Tr? v? DTO ch?a: t?ng s? ti?t, s? ti?t h?p l?, danh sách vi ph?m.
        /// </summary>
        public (double complianceRate, int totalScheduled, int compliantCount, List<NonCompliantTeacherDto> nonCompliantTeachers) CheckCompliance(DateTime date)
        {
            try
            {
                // DayOfWeek in MOET style: 2 = Monday, 3 = Tuesday, ..., 7 = Saturday
                int dayOfWeekInt = GetMoetDayOfWeek(date);
                
                // L?y TKB c?a ngày dó
                var timetable = _db.TimetableEntries
                    .Where(t => t.DayOfWeek == dayOfWeekInt)
                    .ToList();

                if (!timetable.Any())
                {
                    return (100.0, 0, 0, new List<NonCompliantTeacherDto>());
                }

                // L?y UsageLogs cùng ngày có s? ki?n d?y h?c ho?c ho?t d?ng c?a giáo viên
                var logs = _db.UsageLogs
                    .Where(l => l.Timestamp.Date == date.Date && 
                               (l.EventType == "TEACHING_SESSION" || l.EventType == "TEACHING_START" || l.EventType == "SESSION_START" || l.EventType == "PAGE_NAVIGATED"))
                    .ToList();

                int compliantCount = 0;
                var nonCompliantTeachers = new List<NonCompliantTeacherDto>();

                foreach (var entry in timetable)
                {
                    bool isCompliant = false;

                    foreach (var log in logs)
                    {
                        // Ki?m tra xem log này có kh?p v?i TKB không
                        if (IsLogMatchEntry(log, entry))
                        {
                            isCompliant = true;
                            break;
                        }
                    }

                    if (isCompliant)
                    {
                        compliantCount++;
                    }
                    else
                    {
                        nonCompliantTeachers.Add(new NonCompliantTeacherDto
                        {
                            TeacherName = entry.TeacherName,
                            Subject = entry.Subject,
                            Period = entry.Period,
                            Room = entry.Room,
                            Reason = "V?ng d?y (Không t́m th?y log ho?t d?ng d?y h?c)"
                        });
                    }
                }

                double rate = (double)compliantCount / timetable.Count * 100.0;
                return (Math.Round(rate, 2), timetable.Count, compliantCount, nonCompliantTeachers);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "TimetableComplianceService: CheckCompliance failed");
                return (0.0, 0, 0, new List<NonCompliantTeacherDto>());
            }
        }

        /// <summary>
        /// L?y t? l? tuân th? th?i khóa bi?u trung bình c?a c? tháng.
        /// </summary>
        public double GetComplianceRate(int month, int year)
        {
            try
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                int totalScheduled = 0;
                int totalCompliant = 0;

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    // B? qua Ch? Nh?t
                    if (date.DayOfWeek == DayOfWeek.Sunday) continue;

                    var (_, scheduled, compliant, _) = CheckCompliance(date);
                    totalScheduled += scheduled;
                    totalCompliant += compliant;
                }

                if (totalScheduled == 0) return 100.0;
                return Math.Round((double)totalCompliant / totalScheduled * 100.0, 2);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "TimetableComplianceService: GetComplianceRate failed");
                return 0.0;
            }
        }

        /// <summary>
        /// L?y danh sách giáo viên không tuân th? th?i khóa bi?u trong tháng.
        /// </summary>
        public List<NonCompliantTeacherDto> GetNonCompliantTeachers(int month, int year)
        {
            var nonCompliantList = new List<NonCompliantTeacherDto>();
            try
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    if (date.DayOfWeek == DayOfWeek.Sunday) continue;

                    var (_, _, _, dailyNonCompliant) = CheckCompliance(date);
                    foreach (var item in dailyNonCompliant)
                    {
                        item.Reason = $"{date:dd/MM/yyyy}: Ti?t {item.Period} môn {item.Subject} pḥng {item.Room}";
                        nonCompliantList.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "TimetableComplianceService: GetNonCompliantTeachers failed");
            }
            return nonCompliantList;
        }

        /// <summary>
        /// L?y xu hu?ng tuân th? theo các tu?n g?n dây.
        /// </summary>
        public List<ComplianceTrendDto> GetComplianceTrend(int weeks)
        {
            var trend = new List<ComplianceTrendDto>();
            try
            {
                var today = DateTime.Today;
                for (int i = weeks - 1; i >= 0; i--)
                {
                    var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1 - (7 * i)); // Th? Hai
                    var endOfWeek = startOfWeek.AddDays(5); // Th? B?y

                    int totalScheduled = 0;
                    int totalCompliant = 0;

                    for (var date = startOfWeek; date <= endOfWeek; date = date.AddDays(1))
                    {
                        var (_, scheduled, compliant, _) = CheckCompliance(date);
                        totalScheduled += scheduled;
                        totalCompliant += compliant;
                    }

                    double rate = totalScheduled == 0 ? 100.0 : (double)totalCompliant / totalScheduled * 100.0;
                    trend.Add(new ComplianceTrendDto
                    {
                        WeekName = $"Tu?n {GetWeekOfYear(startOfWeek)}",
                        ComplianceRate = Math.Round(rate, 2)
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "TimetableComplianceService: GetComplianceTrend failed");
            }
            return trend;
        }

        private int GetMoetDayOfWeek(DateTime date)
        {
            if (date.DayOfWeek == DayOfWeek.Sunday) return 1; // Ch? Nh?t là 1
            return (int)date.DayOfWeek + 1; // Th? Hai = 2, ..., Th? B?y = 7
        }

        private int GetWeekOfYear(DateTime time)
        {
            var dfi = System.Globalization.DateTimeFormatInfo.CurrentInfo;
            var cal = dfi.Calendar;
            return cal.GetWeekOfYear(time, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
        }

        private bool IsLogMatchEntry(UsageLog log, TimetableEntry entry)
        {
            if (string.IsNullOrEmpty(log.EventData)) return false;

            // Cách 1: Parse JSON n?u là JSON
            try
            {
                var trimmed = log.EventData.Trim();
                if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
                {
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(log.EventData);
                    if (data != null)
                    {
                        bool teacherMatch = false;
                        if (data.TryGetValue("TeacherName", out var tNameObj) && tNameObj != null)
                        {
                            teacherMatch = tNameObj.ToString()?.Equals(entry.TeacherName, StringComparison.OrdinalIgnoreCase) ?? false;
                        }
                        else if (data.TryGetValue("TeacherCode", out var tCodeObj) && tCodeObj != null)
                        {
                            teacherMatch = tCodeObj.ToString()?.Equals(entry.TeacherName, StringComparison.OrdinalIgnoreCase) ?? false;
                        }

                        bool subjectMatch = true;
                        if (data.TryGetValue("Subject", out var subObj) && subObj != null)
                        {
                            subjectMatch = subObj.ToString()?.Equals(entry.Subject, StringComparison.OrdinalIgnoreCase) ?? false;
                        }

                        bool periodMatch = true;
                        if (data.TryGetValue("Period", out var periodObj) && periodObj != null)
                        {
                            if (int.TryParse(periodObj.ToString(), out int p))
                            {
                                periodMatch = (p == entry.Period);
                            }
                        }

                        if (teacherMatch && subjectMatch && periodMatch) return true;
                    }
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON, dùng substring search
            }

            // Cách 2: Substring search (fallback)
            string dataLower = log.EventData.ToLower();
            string teacherLower = entry.TeacherName.ToLower();
            string subjectLower = entry.Subject.ToLower();

            // N?u log ch?a tên GV và môn h?c
            if (dataLower.Contains(teacherLower) && dataLower.Contains(subjectLower))
            {
                return true;
            }

            // Ho?c n?u log du?c th?c hi?n b?i chính GV dó (n?u UserRole ch?a TeacherName/TeacherCode)
            if (log.UserRole.Equals(entry.TeacherName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}

