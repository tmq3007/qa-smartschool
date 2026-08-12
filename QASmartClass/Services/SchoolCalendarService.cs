using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.Services
{
    /// <summary>
    /// Service quản lý Lịch sự kiện toàn trường (SchoolEvent)
    /// </summary>
    public class SchoolCalendarService
    {
        private readonly AppDbContext _db;

        public SchoolCalendarService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>Tạo sự kiện mới</summary>
        public SchoolEvent? CreateEvent(SchoolEvent schoolEvent)
        {
            if (schoolEvent == null || string.IsNullOrWhiteSpace(schoolEvent.Title))
            {
                Log.Warning("SchoolCalendarService: Tên sự kiện không hợp lệ.");
                return null;
            }

            if (schoolEvent.StartTime >= schoolEvent.EndTime)
            {
                Log.Warning("SchoolCalendarService: Giờ kết thúc phải sau giờ bắt đầu.");
                return null;
            }

            try
            {
                schoolEvent.Title = schoolEvent.Title.Trim();
                schoolEvent.CreatedAt = DateTime.Now;
                _db.SchoolEvents.Add(schoolEvent);
                _db.SaveChanges();
                Log.Information("Created event {Id}: {Title}", schoolEvent.Id, schoolEvent.Title);
                return schoolEvent;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi tạo SchoolEvent.");
                return null;
            }
        }

        /// <summary>Lấy sự kiện theo ngày</summary>
        public List<SchoolEvent> GetEventsByDate(DateTime date)
        {
            try
            {
                var startDate = date.Date;
                var endDate = startDate.AddDays(1);
                return _db.SchoolEvents
                          .Where(e => e.StartTime >= startDate && e.StartTime < endDate)
                          .OrderBy(e => e.StartTime)
                          .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi GetEventsByDate.");
                return new List<SchoolEvent>();
            }
        }

        /// <summary>Lấy sự kiện trong một tuần (bắt đầu từ Thứ 2)</summary>
        public List<SchoolEvent> GetEventsByWeek(DateTime dateInWeek)
        {
            try
            {
                // Tính ngày Monday của tuần chứa dateInWeek
                int diff = (7 + (dateInWeek.DayOfWeek - DayOfWeek.Monday)) % 7;
                DateTime monday = dateInWeek.AddDays(-diff).Date;
                DateTime nextMonday = monday.AddDays(7);

                return _db.SchoolEvents
                          .Where(e => e.StartTime >= monday && e.StartTime < nextMonday)
                          .OrderBy(e => e.StartTime)
                          .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi GetEventsByWeek.");
                return new List<SchoolEvent>();
            }
        }

        /// <summary>Lấy sự kiện theo bộ phận / tổ chức</summary>
        public List<SchoolEvent> GetEventsByDepartment(string department)
        {
            if (string.IsNullOrWhiteSpace(department))
                return new List<SchoolEvent>();

            try
            {
                if (department == "All")
                {
                    return _db.SchoolEvents.OrderBy(e => e.StartTime).ToList();
                }

                return _db.SchoolEvents
                          .Where(e => e.Department == department || e.Department == "All")
                          .OrderBy(e => e.StartTime)
                          .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi GetEventsByDepartment.");
                return new List<SchoolEvent>();
            }
        }

        /// <summary>Cập nhật sự kiện</summary>
        public bool UpdateEvent(SchoolEvent updatedEvent)
        {
            if (updatedEvent == null || string.IsNullOrWhiteSpace(updatedEvent.Title))
                return false;

            if (updatedEvent.StartTime >= updatedEvent.EndTime)
                return false;

            try
            {
                var existing = _db.SchoolEvents.Find(updatedEvent.Id);
                if (existing == null)
                    return false;

                existing.Title = updatedEvent.Title.Trim();
                existing.Description = updatedEvent.Description;
                existing.StartTime = updatedEvent.StartTime;
                existing.EndTime = updatedEvent.EndTime;
                existing.Location = updatedEvent.Location;
                existing.Organizer = updatedEvent.Organizer;
                existing.Department = updatedEvent.Department;
                existing.Status = updatedEvent.Status;

                _db.SaveChanges();
                Log.Information("Updated event {Id}", updatedEvent.Id);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi UpdateEvent.");
                return false;
            }
        }

        /// <summary>Xóa sự kiện</summary>
        public bool DeleteEvent(int eventId)
        {
            try
            {
                var existing = _db.SchoolEvents.Find(eventId);
                if (existing == null)
                    return false;

                _db.SchoolEvents.Remove(existing);
                _db.SaveChanges();
                Log.Information("Deleted event {Id}", eventId);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi DeleteEvent.");
                return false;
            }
        }
    }
}

