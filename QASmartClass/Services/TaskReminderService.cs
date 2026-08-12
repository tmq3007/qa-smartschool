using System;
using System.Linq;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class TaskReminderService
    {
        private readonly AppDbContext _db;
        private readonly MobileApiService _mobileApi;

        public TaskReminderService(AppDbContext dbContext)
        {
            _db = dbContext;
            _mobileApi = new MobileApiService(_db);
        }

        public void RunDeadlineCheck()
        {
            var tomorrow = DateTime.Now.Date.AddDays(1);
            
            // T́m các Task chua hoàn thành và s? d?n h?n vào ngày mai
            var dueTomorrowTasks = _db.DailyTasks
                .Where(t => t.Status != "Done" && t.DueDate == tomorrow)
                .ToList();

            foreach (var task in dueTomorrowTasks)
            {
                // G?i Push Notification (Mock cho Staff/Teacher)
                _mobileApi.SendPushNotification(
                    recipientId: 0, 
                    role: "Staff", 
                    title: "? Nh?c nh? Công vi?c: " + task.Title, 
                    body: $"Công vi?c c?a b?n s? d?n h?n vào ngày mai. Vui ḷng hoàn thành dúng ti?n d?.", 
                    type: "General"
                );
            }
        }
    }
}

