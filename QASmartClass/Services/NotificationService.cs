using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.Services
{
    public class NotificationMessage
    {
        public string Title   { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Type    { get; set; } = "Info";
    }

    /// <summary>
    /// Service quan ly canh bao, nhac nho va thong bao phu huynh (P1-06 upgrade)
    /// </summary>
    public class NotificationService
    {
        private readonly AppDbContext _db;

        public NotificationService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        // ─── P1-06: Gui thong bao canh bao toi PH → luu DB ───

        /// <summary>
        /// Gui canh bao Early Warning toi phu huynh, luu vao InboxMessages.
        /// ReceiverId = "parent_{studentId}"
        /// </summary>
        public bool SendToParent(int studentId, string subject, string reason)
        {
            try
            {
                var student = _db.Students.FirstOrDefault(s => s.Id == studentId);
                if (student == null) return false;

                string content = $"[CẢNH BÁO] {subject} — {reason} — Học sinh: {student.FullName} ({student.ClassName})";

                _db.InboxMessages.Add(new InboxMessage
                {
                    SenderId    = "SYSTEM",
                    SenderName  = "He thong Canh bao som",
                    ReceiverId  = $"parent_{studentId}",
                    Content     = content,
                    IsRead      = false,
                    CreatedAt   = DateTime.Now,
                    ThreadId    = $"earlywarning_{studentId}"
                });
                _db.SaveChanges();

                Log.Information("[Notification] Sent EarlyWarning to parent of Student {Id}: {Subject}", studentId, subject);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NotificationService] SendToParent failed");
                return false;
            }
        }

        /// <summary>Dem so thong bao chua doc cua phu huynh HS.</summary>
        public int GetUnreadCount(int studentId)
        {
            try
            {
                return _db.InboxMessages
                    .Count(m => m.ReceiverId == $"parent_{studentId}" && !m.IsRead);
            }
            catch { return 0; }
        }

        /// <summary>Lay danh sach inbox thong bao cho phu huynh.</summary>
        public List<InboxMessage> GetParentInbox(int studentId, int take = 20)
        {
            try
            {
                return _db.InboxMessages
                    .Where(m => m.ReceiverId == $"parent_{studentId}")
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(take)
                    .ToList();
            }
            catch { return new List<InboxMessage>(); }
        }

        /// <summary>Danh dau da doc mot thong bao.</summary>
        public void MarkAsRead(int messageId)
        {
            try
            {
                var msg = _db.InboxMessages.Find(messageId);
                if (msg != null) { msg.IsRead = true; _db.SaveChanges(); }
            }
            catch (Exception ex) { Log.Warning("[NotificationService] MarkAsRead: {Err}", ex.Message); }
        }

        // ─── Cac method cu giu nguyen ───────────────────

        public List<TaskItem> GetUpcomingDeadlines(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return new List<TaskItem>();
            try
            {
                DateTime tomorrow = DateTime.Now.AddDays(1);
                return _db.TaskItems
                          .Where(t => t.AssignedTo == userId
                                   && t.Status != "Done"
                                   && t.Deadline > DateTime.Now
                                   && t.Deadline <= tomorrow)
                          .OrderBy(t => t.Deadline)
                          .ToList();
            }
            catch (Exception ex) { Log.Error(ex, "GetUpcomingDeadlines error"); return new List<TaskItem>(); }
        }

        public List<TaskItem> GetOverdueTasks(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return new List<TaskItem>();
            try
            {
                return _db.TaskItems
                          .Where(t => t.AssignedTo == userId
                                   && t.Status != "Done"
                                   && t.Deadline < DateTime.Now)
                          .OrderBy(t => t.Deadline)
                          .ToList();
            }
            catch (Exception ex) { Log.Error(ex, "GetOverdueTasks error"); return new List<TaskItem>(); }
        }

        public bool PushNotification(string userId, NotificationMessage message)
        {
            try
            {
                Log.Information("PUSH to {UserId}: [{Type}] {Title}", userId, message.Type, message.Title);
                return true;
            }
            catch (Exception ex) { Log.Error(ex, "PushNotification error"); return false; }
        }
    }
}

