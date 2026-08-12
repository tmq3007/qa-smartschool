using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class SecurityService
    {
        private readonly AppDbContext _db;

        public SecurityService(AppDbContext db)
        {
            _db = db;
        }

        public SecurityLog AddLog(SecurityLog log)
        {
            log.Timestamp = DateTime.Now;
            _db.SecurityLogs.Add(log);
            _db.SaveChanges();
            return log;
        }

        public List<SecurityLog> GetRecentLogs(int count = 50)
        {
            return _db.SecurityLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToList();
        }

        public List<SecurityLog> GetLogsByDate(DateTime date)
        {
            var nextDate = date.Date.AddDays(1);
            return _db.SecurityLogs
                .Where(l => l.Timestamp >= date.Date && l.Timestamp < nextDate)
                .OrderByDescending(l => l.Timestamp)
                .ToList();
        }
 
        public async Task<List<SecurityLog>> GetLogsByDateAsync(DateTime date)
        {
            var nextDate = date.Date.AddDays(1);
            return await _db.SecurityLogs
                .Where(l => l.Timestamp >= date.Date && l.Timestamp < nextDate)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
        
        public List<SecurityLog> SearchLogs(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return GetRecentLogs();
            var kw = keyword.ToLower();
            return _db.SecurityLogs
                .Where(l => l.Description.ToLower().Contains(kw) || l.PersonInvolved.ToLower().Contains(kw) || l.GuardName.ToLower().Contains(kw))
                .OrderByDescending(l => l.Timestamp)
                .ToList();
        }

        public async Task<SecurityLog> AddLogAsync(SecurityLog log)
        {
            log.Timestamp = DateTime.Now;
            _db.SecurityLogs.Add(log);
            await _db.SaveChangesAsync();
            return log;
        }

        public async Task<List<SecurityLog>> SearchLogsAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return await _db.SecurityLogs
                    .OrderByDescending(l => l.Timestamp)
                    .Take(50)
                    .ToListAsync();
            }
            var kw = keyword.ToLower();
            
            // Băm SHA-256 / HMAC số CCCD nếu từ khóa tìm kiếm trông giống CCCD
            string cccdHash = string.Empty;
            if (keyword.Trim().All(char.IsDigit) && (keyword.Trim().Length == 9 || keyword.Trim().Length == 12))
            {
                cccdHash = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(keyword.Trim());
            }

            return await _db.SecurityLogs
                .Where(l => l.Description.ToLower().Contains(kw) 
                         || l.PersonInvolved.ToLower().Contains(kw) 
                         || l.GuardName.ToLower().Contains(kw)
                         || (!string.IsNullOrEmpty(cccdHash) && l.CccdHash == cccdHash))
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
    }
}

