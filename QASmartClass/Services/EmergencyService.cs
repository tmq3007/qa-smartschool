﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class EmergencyService
    {
        private readonly AppDbContext _db;
        private readonly NotificationService _notificationService;

        public EmergencyService(AppDbContext db, NotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }

        public EmergencyLog AddEmergency(EmergencyLog log)
        {
            log.Timestamp = DateTime.Now;
            _db.EmergencyLogs.Add(log);
            _db.SaveChanges();

            // Kích hoạt thông báo khẩn cấp cho GVCN và Phụ huynh
            if (log.Status != "Resolved")
            {
                var notifMsg = new NotificationMessage {
                    Type = "Emergency",
                    Title = $"Cảnh báo Y tế: {log.StudentName} gặp sự cố ({log.IncidentType}).",
                    Content = $"Chi tiết: {log.Description}. Đang xử lý: {log.FirstAidApplied}"
                };
                _notificationService.PushNotification("all_staff", notifMsg);
            }

            return log;
        }

        public List<EmergencyLog> GetRecentEmergencies(int count = 50)
        {
            return _db.EmergencyLogs
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToList();
        }

        public bool UpdateEmergencyStatus(int id, string newStatus)
        {
            var log = _db.EmergencyLogs.Find(id);
            if (log != null)
            {
                log.Status = newStatus;
                _db.SaveChanges();
                return true;
            }
            return false;
        }
    }
}

