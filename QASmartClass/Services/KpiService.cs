using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;

namespace QASmartClass.Services
{
    public class SchoolKpiDTO
    {
        public int TotalStudents { get; set; }
        public double AttendanceRate { get; set; }
        public double AvgScore { get; set; }
        public double TaskCompletionRate { get; set; }
        public string EmulationLeader { get; set; } = string.Empty;
    }

    /// <summary>
    /// Service tính toán KPI toàn tru?ng (Phase 7)
    /// </summary>
    public class KpiService
    {
        private readonly AppDbContext _db;

        public KpiService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public SchoolKpiDTO GetSchoolKpi()
        {
            try
            {
                var dto = new SchoolKpiDTO();
                
                // 1. Si s?
                dto.TotalStudents = _db.Students.Count();

                // 2. Chuyên cần (Mock logic ho?c l?y t? AttendanceRecord)
                var records = _db.AttendanceRecords.ToList();
                if (records.Count > 0)
                {
                    int present = records.Count(r => r.Status == "present");
                    dto.AttendanceRate = Math.Round((double)present * 100 / records.Count, 1);
                }
                else
                {
                    dto.AttendanceRate = 96.8; // Default mock
                }

                // 3. Đi?m trung bình (Mock ho?c t? RubricGrade)
                var grades = _db.RubricGrades.ToList();
                if (grades.Count > 0)
                {
                    dto.AvgScore = Math.Round(grades.Average(g => g.Points), 1);
                }
                else
                {
                    dto.AvgScore = 7.8;
                }

                // 4. T? l? hoàn thành công vi?c
                var tasks = _db.TaskItems.ToList();
                if (tasks.Count > 0)
                {
                    int done = tasks.Count(t => t.Status == "Done");
                    dto.TaskCompletionRate = Math.Round((double)done * 100 / tasks.Count, 1);
                }
                else
                {
                    dto.TaskCompletionRate = 85.0;
                }

                dto.EmulationLeader = "T? Toán-Tin";
                return dto;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi GetSchoolKpi.");
                return new SchoolKpiDTO { TotalStudents = 0, AttendanceRate = 0, AvgScore = 0, TaskCompletionRate = 0 };
            }
        }
    }
}

