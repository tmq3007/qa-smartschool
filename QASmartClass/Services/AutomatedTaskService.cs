using System;
using System.Linq;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class AutomatedTaskService
    {
        private readonly AppDbContext _db;

        public AutomatedTaskService(AppDbContext dbContext)
        {
            _db = dbContext;
        }

        public void GenerateRecurringTasks()
        {
            var today = DateTime.Now.Date;
            
            // Example 1: Weekly Kitchen Cleaning Task (Every Friday)
            if (today.DayOfWeek == DayOfWeek.Friday)
            {
                string title = $"Vệ sinh bếp ăn cuối tuần ({today:dd/MM})";
                if (!_db.DailyTasks.Any(t => t.Title == title && t.Department == "Kitchen"))
                {
                    // Tìm kiếm bếp trưởng hoặc nhân sự bếp trong CSDL
                    var chef = _db.TeacherProfiles.FirstOrDefault(t => t.Role == "Bep" || t.Notes.Contains("Bếp"))?.FullName ?? "Chef_BếpTrưởng";

                    _db.DailyTasks.Add(new DailyTask
                    {
                        Title = title,
                        AssignedTo = chef,
                        AssignedBy = "System",
                        DueDate = today,
                        Status = "Pending",
                        Department = "Kitchen",
                        Notes = "Tổng vệ sinh toàn bộ khu vực bếp ăn và khử khuẩn."
                    });
                }
            }

            // Example 2: Monthly Teacher Meeting Task (First Monday of the month)
            if (today.DayOfWeek == DayOfWeek.Monday && today.Day <= 7)
            {
                string title = $"Nộp biên bản sinh hoạt tổ chuyên môn Tháng {today.Month}";
                if (!_db.DailyTasks.Any(t => t.Title == title && t.Department == "ToanTin"))
                {
                    // Tìm kiếm tổ trưởng hoặc giáo viên bộ môn Toán trong CSDL
                    var leader = _db.TeacherProfiles.FirstOrDefault(t => t.Role == "ToTruong" || t.Notes.Contains("Tổ trưởng") || t.Subject == "Toán")?.FullName ?? "GV_ToTruong";

                    _db.DailyTasks.Add(new DailyTask
                    {
                        Title = title,
                        AssignedTo = leader,
                        AssignedBy = "System",
                        DueDate = today.AddDays(5),
                        Status = "Pending",
                        Department = "ToanTin",
                        Notes = "Nộp báo cáo sinh hoạt chuyên môn đầu tháng."
                    });
                }
            }
            
            _db.SaveChanges();
        }
    }
}

