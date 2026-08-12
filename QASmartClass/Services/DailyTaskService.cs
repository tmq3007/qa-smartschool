using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.Services
{
    public class TaskStatsDTO
    {
        public int Total { get; set; }
        public int Done { get; set; }
        public int InProgress { get; set; }
        public int Overdue { get; set; }
        public double CompletionRate => Total > 0 ? (double)Done * 100 / Total : 0;
    }

    /// <summary>
    /// Service quản lý Công việc hàng ngày (DailyTask)
    /// </summary>
    public class DailyTaskService
    {
        private readonly AppDbContext _db;

        public DailyTaskService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>Giao việc / Tạo task mới</summary>
        public DailyTask? CreateTask(DailyTask task)
        {
            if (task == null || string.IsNullOrWhiteSpace(task.Title))
            {
                Log.Warning("DailyTaskService: Tên công việc không hợp lệ.");
                return null;
            }

            try
            {
                task.Title = task.Title.Trim();
                if (string.IsNullOrEmpty(task.Status))
                    task.Status = "Pending";

                _db.DailyTasks.Add(task);
                _db.SaveChanges();
                Log.Information("Created task {Id}: {Title}", task.Id, task.Title);
                return task;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi CreateTask.");
                return null;
            }
        }

        /// <summary>Lấy danh sách việc của một user trong một ngày</summary>
        public List<DailyTask> GetMyTasks(string userId, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new List<DailyTask>();

            try
            {
                var startDate = date.Date;
                var endDate = startDate.AddDays(1);
                return _db.DailyTasks
                          .Where(t => t.AssignedTo == userId && t.DueDate >= startDate && t.DueDate < endDate)
                          .ToList()
                          .OrderBy(t => t.Status == "Done" ? 1 : 0) // Chưa xong lên trước
                          .ThenBy(t => t.StartTime ?? t.DueDate)
                          .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi GetMyTasks.");
                return new List<DailyTask>();
            }
        }

        /// <summary>Đánh dấu hoàn thành task</summary>
        public bool CompleteTask(int taskId, string notes, string userId = "")
        {
            try
            {
                var task = _db.DailyTasks.Find(taskId);
                if (task == null)
                {
                    Log.Warning("DailyTaskService: Task {Id} không tồn tại.", taskId);
                    return false;
                }

                if (!string.IsNullOrEmpty(userId))
                {
                    bool isAuthorized = task.AssignedTo == userId || 
                        (!string.IsNullOrEmpty(task.CollaboratorIds) && 
                         task.CollaboratorIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                             .Select(s => s.Trim())
                                             .Contains(userId));
                    if (!isAuthorized)
                    {
                        Log.Warning("CompleteTask: User {UserId} is not authorized to complete task {TaskId}.", userId, taskId);
                        return false;
                    }
                }

                task.Status = "Done";
                task.Notes = notes?.Trim() ?? string.Empty;
                _db.SaveChanges();
                
                Log.Information("Completed task {Id}", taskId);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi CompleteTask.");
                return false;
            }
        }

        /// <summary>Lấy danh sách task quá hạn</summary>
        public List<DailyTask> GetOverdueTasks()
        {
            try
            {
                var today = DateTime.Today;
                return _db.DailyTasks
                          .Where(t => t.DueDate < today && t.Status != "Done")
                          .OrderBy(t => t.DueDate)
                          .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi GetOverdueTasks.");
                return new List<DailyTask>();
            }
        }

        /// <summary>Lấy thống kê theo phòng ban / tổ chuyên môn</summary>
        public TaskStatsDTO GetTaskStats(string department)
        {
            try
            {
                var query = _db.DailyTasks.AsQueryable();

                if (!string.IsNullOrWhiteSpace(department) && department != "All")
                {
                    query = query.Where(t => t.Department == department);
                }

                var today = DateTime.Today;
                var stats = query.Select(t => new {
                    t.Status,
                    IsOverdue = t.DueDate < today && t.Status != "Done"
                }).ToList();

                var total = stats.Count;
                var done = stats.Count(t => t.Status == "Done");
                var inProgress = stats.Count(t => t.Status == "InProgress" || t.Status == "Pending");
                var overdue = stats.Count(t => t.IsOverdue);

                return new TaskStatsDTO
                {
                    Total = total,
                    Done = done,
                    InProgress = inProgress,
                    Overdue = overdue
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi GetTaskStats.");
                return new TaskStatsDTO();
            }
        }

        /// <summary>Mức 1 & 2: Lấy lịch hoạt động toàn trường hoặc theo bộ phận</summary>
        public List<DailyTask> GetSchoolDailySchedule(DateTime date, string department = "All")
        {
            try
            {
                var startDate = date.Date;
                var endDate = startDate.AddDays(1);
                var query = _db.DailyTasks.Where(t => t.DueDate >= startDate && t.DueDate < endDate);

                if (!string.IsNullOrEmpty(department) && department != "All")
                {
                    query = query.Where(t => t.Department == department);
                }

                return query.ToList()
                            .OrderBy(t => t.StartTime ?? t.DueDate)
                            .ThenBy(t => t.Id)
                            .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi GetSchoolDailySchedule.");
                return new List<DailyTask>();
            }
        }

        /// <summary>Mức 3: Lấy lịch cá nhân bao gồm việc giao trực tiếp & việc phối hợp</summary>
        public List<DailyTask> GetMyDailyTasksAndCoordinations(string userId, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new List<DailyTask>();

            try
            {
                var startDate = date.Date;
                var endDate = startDate.AddDays(1);
                var dailyTasks = _db.DailyTasks
                                    .Where(t => t.DueDate >= startDate && t.DueDate < endDate)
                                    .ToList();

                return dailyTasks
                    .Where(t => t.AssignedTo == userId || 
                               (!string.IsNullOrEmpty(t.CollaboratorIds) && 
                                t.CollaboratorIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(s => s.Trim())
                                                 .Contains(userId)))
                    .OrderBy(t => t.Status == "Done" ? 1 : 0)
                    .ThenBy(t => t.StartTime ?? t.DueDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi GetMyDailyTasksAndCoordinations.");
                return new List<DailyTask>();
            }
        }

        /// <summary>Ủy quyền/Bàn giao công việc</summary>
        public bool DelegateTask(int taskId, string delegatorId, string delegateeId, string comment)
        {
            try
            {
                var task = _db.DailyTasks.Find(taskId);
                if (task == null) return false;

                // Check authorization
                bool isAuthorized = false;
                if (delegatorId == task.AssignedBy || delegatorId == task.AssignedTo || delegatorId == "HT001")
                {
                    isAuthorized = true;
                }
                else
                {
                    var delegatorProfile = _db.StaffProfiles.FirstOrDefault(s => s.StaffCode == delegatorId);
                    if (delegatorProfile != null)
                    {
                        if (delegatorProfile.Position.Contains("Hiệu trưởng") || delegatorProfile.Position.Contains("Principal") ||
                            delegatorProfile.Position.Contains("Trưởng") || delegatorProfile.Position.Contains("Head"))
                        {
                            if (delegatorProfile.Department == "BGH" || delegatorProfile.Department == task.Department)
                            {
                                isAuthorized = true;
                            }
                        }
                    }
                }

                if (!isAuthorized)
                {
                    Log.Warning("DelegateTask: Delegator {DelegatorId} is not authorized to delegate task {TaskId}.", delegatorId, taskId);
                    return false;
                }

                task.AssignedTo = delegateeId;
                task.Notes = (string.IsNullOrEmpty(task.Notes) ? "" : task.Notes + "\n") + 
                             $"[Ủy quyền] Bàn giao từ {delegatorId} sang {delegateeId}. Lý do/Ghi chú: {comment}";
                _db.SaveChanges();
                Log.Information("Delegated task {Id} from {Delegator} to {Delegatee}", taskId, delegatorId, delegateeId);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi DelegateTask.");
                return false;
            }
        }

        /// <summary>Quét quá hạn SLA và tự động gửi tin nhắn leo thang cho Hiệu trưởng</summary>
        public bool ProcessEscalateSla()
        {
            try
            {
                var enableSla = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Dashboard_EnableSlaEscalation")?.Value ?? "1";
                if (enableSla == "0")
                {
                    Log.Information("[SLA Engine] SLA escalation is disabled by IT_Dashboard_EnableSlaEscalation setting.");
                    return false;
                }

                var windowHoursStr = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Dashboard_ReportWindowHours")?.Value ?? "24";
                double windowHours = double.TryParse(windowHoursStr, out var wh) ? wh : 24;

                var threshold = DateTime.Now;
                var overdueCriticalTasks = _db.DailyTasks
                    .Where(t => t.IsCritical && t.Status != "Done" && t.Status != "Completed" && t.EndTime != null)
                    .ToList()
                    .Where(t => t.EndTime.Value.AddHours(windowHours) < threshold)
                    .ToList();

                bool escalatedAny = false;
                foreach (var t in overdueCriticalTasks)
                {
                    var threadId = $"sla_escalation_{t.Id}";
                    bool alreadySent = _db.InboxMessages.Any(m => m.ThreadId == threadId);
                    if (!alreadySent)
                    {
                        _db.InboxMessages.Add(new InboxMessage
                        {
                            SenderId = "SYSTEM",
                            SenderName = "SLA Escalation Engine",
                            ReceiverId = "HT001",
                            Content = $"[RED ALERT] Công việc khẩn cấp quá hạn: \"{t.Title}\" giao cho {t.AssignedTo} ở {t.Location} quá hạn hơn {windowHours} giờ.",
                            IsRead = false,
                            CreatedAt = DateTime.Now,
                            ThreadId = threadId
                        });

                        t.Notes = (string.IsNullOrEmpty(t.Notes) ? "" : t.Notes + "\n") + 
                                  $"[SYSTEM SLA Escalation] Đã leo thang lên BGH lúc {DateTime.Now}.";
                        escalatedAny = true;
                    }
                }

                if (escalatedAny)
                {
                    _db.SaveChanges();
                    Log.Information("[SLA Engine] Escalated critical overdue tasks successfully.");
                }

                return escalatedAny;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi ProcessEscalateSla.");
                return false;
            }
        }
    }
}
