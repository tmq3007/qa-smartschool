using System;
using System.Linq;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Services
{
    /// <summary>
    /// Service xác th?c ph? huynh.
    /// Ph? huynh dang nh?p b?ng Mă HS + SĐT ph? huynh dă dang kư.
    /// </summary>
    public class ParentAuthService
    {
        private readonly AppDbContext _db;

        public ParentAuthService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Đang nh?p b?ng mă h?c sinh + SĐT ph? huynh.
        /// Tr? v? Student n?u thành công, null n?u sai thông tin.
        /// </summary>
        public Student? Login(string studentCode, string parentPhone)
        {
            if (string.IsNullOrWhiteSpace(studentCode) || string.IsNullOrWhiteSpace(parentPhone))
                return null;

            var student = _db.Students.FirstOrDefault(s =>
                s.StudentCode == studentCode.Trim() &&
                s.ParentPhone == parentPhone.Trim() &&
                s.Status == "Active");

            if (student != null)
            {
                Log.Information("ParentAuth: Login OK for {Code} ({Parent})",
                    studentCode, student.ParentName);
            }
            else
            {
                Log.Warning("ParentAuth: Login FAILED for {Code} + {Phone}",
                    studentCode, parentPhone);
            }

            return student;
        }

        /// <summary>L?y t?ng h?p nhanh cho dashboard PH</summary>
        public ParentDashboardSummary GetSummary(int studentId)
        {
            var student = _db.Students.FirstOrDefault(s => s.Id == studentId);
            if (student == null) return new ParentDashboardSummary();

            // Đi?m trung bình
            double avgScore = 0;
            var grades = _db.StudentGrades.Where(g => g.StudentId == studentId).ToList();
            if (grades.Any()) avgScore = grades.Average(g => g.Score);

            // Điểm danh tháng này
            var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var attendance = _db.AttendanceRecords
                .Where(a => a.StudentId == studentId && a.Date >= monthStart)
                .ToList();
            int totalDays = attendance.Count;

            var caseSetting = _db.SystemSettings.FirstOrDefault(s => s.Id == "ParentPortal_AttendanceCase")?.Value ?? "CaseInsensitive";
            bool isStrict = caseSetting.Equals("Strict", StringComparison.OrdinalIgnoreCase);

            int presentDays = isStrict
                ? attendance.Count(a => a.Status == "Present")
                : attendance.Count(a => a.Status.Equals("present", StringComparison.OrdinalIgnoreCase));

            int absentDays = isStrict
                ? attendance.Count(a => a.Status == "Absent" || a.Status == "Excused")
                : attendance.Count(a => a.Status.Equals("absent", StringComparison.OrdinalIgnoreCase) || a.Status.Equals("excused", StringComparison.OrdinalIgnoreCase));

            int lateDays = isStrict
                ? attendance.Count(a => a.Status == "Late")
                : attendance.Count(a => a.Status.Equals("late", StringComparison.OrdinalIgnoreCase));

            // H?nh ki?m
            int conductScore = student.ConductScore;

            // Bài t?p chua n?p
            var rosterIds = _db.ClassRosterStudents
                .Where(r => r.StudentId == studentId)
                .Select(r => r.RosterId)
                .ToList();
            var pendingAssignments = _db.Assignments
                .Where(a => rosterIds.Contains(a.RosterId) && a.Deadline >= DateTime.Now)
                .Count(a => !_db.FileTransfers.Any(f =>
                    f.AssignmentId == a.Id && f.StudentId == studentId && f.Status == "Completed"));

            return new ParentDashboardSummary
            {
                StudentName = student.FullName,
                StudentCode = student.StudentCode,
                ClassName = student.ClassName,
                ParentName = student.ParentName,
                AverageScore = avgScore,
                ConductScore = conductScore,
                TotalAttendanceDays = totalDays,
                PresentDays = presentDays,
                AbsentDays = absentDays,
                LateDays = lateDays,
                PendingAssignments = pendingAssignments
            };
        }
    }

    /// <summary>DTO t?ng h?p dữ liệu cho Parent Dashboard</summary>
    public class ParentDashboardSummary
    {
        public string StudentName { get; set; } = "";
        public string StudentCode { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string ParentName { get; set; } = "";
        public double AverageScore { get; set; }
        public int ConductScore { get; set; } = 100;
        public int TotalAttendanceDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public int PendingAssignments { get; set; }
    }
}

