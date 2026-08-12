using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class DataRetentionService
    {
        private readonly AppDbContext _db;

        public DataRetentionService(AppDbContext dbContext)
        {
            _db = dbContext;
        }

        public void CleanOldData(int logRetentionDays, int pushRetentionDays)
        {
            var logCutoff = DateTime.Now.AddDays(-logRetentionDays);
            var pushCutoff = DateTime.Now.AddDays(-pushRetentionDays);

            // Clean Audit Logs
            int deletedLogs = _db.AuditLogs.Where(l => l.Timestamp < logCutoff).ExecuteDelete();

            // Clean Push Message Logs
            int deletedPush = _db.PushMessageLogs.Where(p => p.SentAt < pushCutoff).ExecuteDelete();

            // Clean old Resolved Emergencies older than 180 days
            var emergencyCutoff = DateTime.Now.AddDays(-180);
            int deletedEmergencies = _db.EmergencyLogs.Where(e => e.Status == "Resolved" && e.Timestamp < emergencyCutoff).ExecuteDelete();
            
            // Log this action
            _db.AuditLogs.Add(new AuditLog
            {
                Action = "Data_Cleanup",
                ActorName = "System",
                Details = $"Đã dọn dẹp {deletedLogs} AuditLogs, {deletedPush} PushLogs, {deletedEmergencies} EmergencyLogs.",
                Timestamp = DateTime.Now
            });
            _db.SaveChanges();
        }
    }
}

