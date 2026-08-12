using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.Services
{
    /// <summary>
    /// Service qu?n lư Nhi?m v? nâng cao (Task Management Hub)
    /// H? tr? giao vi?c, sub-tasks, b́nh lu?n và theo dơi ti?n d? (Phase 6)
    /// </summary>
    public class TaskManagementService
    {
        private readonly AppDbContext _db;

        public TaskManagementService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>T?o m?i m?t TaskItem</summary>
        public TaskItem? CreateTask(TaskItem task)
        {
            if (task == null || string.IsNullOrWhiteSpace(task.Title))
            {
                Log.Warning("TaskManagementService: Task title r?ng.");
                return null;
            }

            try
            {
                task.Title = task.Title.Trim();
                if (string.IsNullOrEmpty(task.Status)) task.Status = "Todo";
                if (string.IsNullOrEmpty(task.Priority)) task.Priority = "Medium";
                
                task.CreatedAt = DateTime.Now;

                _db.TaskItems.Add(task);
                _db.SaveChanges();
                Log.Information("Created TaskItem {Id}: {Title}", task.Id, task.Title);
                return task;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi CreateTask.");
                return null;
            }
        }

        /// <summary>Phân công Task cho m?t user</summary>
        public bool AssignTask(int taskId, string userId)
        {
            try
            {
                var task = _db.TaskItems.Find(taskId);
                if (task == null) return false;

                task.AssignedTo = userId;
                _db.SaveChanges();
                Log.Information("Assigned Task {Id} to {UserId}", taskId, userId);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi AssignTask.");
                return false;
            }
        }

        /// <summary>C?p nh?t tr?ng thái Task (kéo th? Kanban)</summary>
        public bool UpdateStatus(int taskId, string newStatus)
        {
            try
            {
                var task = _db.TaskItems.Find(taskId);
                if (task == null) return false;

                task.Status = newStatus;
                
                if (newStatus == "Done")
                {
                    task.CompletedAt = DateTime.Now;
                }
                else
                {
                    task.CompletedAt = null;
                }

                _db.SaveChanges();
                Log.Information("Updated Task {Id} status to {Status}", taskId, newStatus);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi UpdateStatus.");
                return false;
            }
        }

        /// <summary>L?y danh sách Task c?a m?t User</summary>
        public List<TaskItem> GetTasksByUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return new List<TaskItem>();

            try
            {
                return _db.TaskItems.ToList()
                          .Where(t => t.AssignedTo == userId)
                          .OrderBy(t => t.Deadline)
                          .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi GetTasksByUser.");
                return new List<TaskItem>();
            }
        }

        /// <summary>L?y danh sách Task theo b? ph?n (T? chuyên môn)</summary>
        public List<TaskItem> GetTasksByDepartment(string department)
        {
            if (string.IsNullOrWhiteSpace(department)) return new List<TaskItem>();

            try
            {
                var all = _db.TaskItems.ToList();
                if (department == "All") return all.OrderBy(t => t.Deadline).ToList();

                return all.Where(t => t.Department == department)
                          .OrderBy(t => t.Deadline)
                          .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi GetTasksByDepartment.");
                return new List<TaskItem>();
            }
        }

        /// <summary>Thêm b́nh lu?n vào Task</summary>
        public TaskComment? AddComment(int taskId, string author, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;

            try
            {
                var task = _db.TaskItems.Find(taskId);
                if (task == null) return null;

                var comment = new TaskComment
                {
                    TaskId = taskId,
                    Author = author,
                    Content = content,
                    CreatedAt = DateTime.Now
                };

                _db.TaskComments.Add(comment);
                _db.SaveChanges();
                Log.Information("Added comment to Task {Id} by {Author}", taskId, author);
                return comment;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi AddComment.");
                return null;
            }
        }
    }
}

