using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class JanitorService
    {
        private readonly AppDbContext _db;

        public JanitorService(AppDbContext db)
        {
            _db = db;
        }

        public CleaningTask AddTask(CleaningTask task)
        {
            _db.CleaningTasks.Add(task);
            _db.SaveChanges();
            return task;
        }

        public List<CleaningTask> GetTasksByDate(DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);
            return _db.CleaningTasks
                .Where(t => t.TaskDate >= startDate && t.TaskDate < endDate)
                .OrderBy(t => t.Shift)
                .ThenBy(t => t.Area)
                .ToList();
        }

        public async Task<List<CleaningTask>> GetTasksByDateAsync(DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);
            return await _db.CleaningTasks
                .Where(t => t.TaskDate >= startDate && t.TaskDate < endDate)
                .OrderBy(t => t.Shift)
                .ThenBy(t => t.Area)
                .ToListAsync();
        }

        public bool UpdateTaskStatus(int taskId, string status, string note = "")
        {
            var task = _db.CleaningTasks.Find(taskId);
            if (task != null)
            {
                task.Status = status;
                if (!string.IsNullOrWhiteSpace(note))
                {
                    task.Note = note;
                }
                _db.SaveChanges();
                return true;
            }
            return false;
        }

        public bool DeleteTask(int taskId)
        {
            var task = _db.CleaningTasks.Find(taskId);
            if (task != null)
            {
                _db.CleaningTasks.Remove(task);
                _db.SaveChanges();
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateTaskStatusAsync(int taskId, string status, string note = "")
        {
            var task = await _db.CleaningTasks.FindAsync(taskId);
            if (task != null)
            {
                task.Status = status;
                if (!string.IsNullOrWhiteSpace(note))
                {
                    task.Note = note;
                }
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            var task = await _db.CleaningTasks.FindAsync(taskId);
            if (task != null)
            {
                _db.CleaningTasks.Remove(task);
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}

