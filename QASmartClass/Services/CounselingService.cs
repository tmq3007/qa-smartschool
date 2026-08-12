using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Services
{
    public class CounselingDashboardStats
    {
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ScheduledRequests { get; set; }
        public int InProgressRequests { get; set; }
        public int CompletedRequests { get; set; }
        public Dictionary<string, int> IssueDistribution { get; set; } = new();
    }

    /// <summary>
    /// Phase 5 (WI-17): Service x? lư logic cho phân h? Tu v?n h?c du?ng (CounselingHub).
    /// H? tr? HTTV10 - HTTV43.
    /// </summary>
    public class CounselingService
    {
        private readonly AppDbContext _db;

        public CounselingService(AppDbContext db)
        {
            _db = db;
        }

        // ==========================================
        // 1. QU?N LƯ YÊU C?U / H? SO TU V?N (PROFILES)
        // ==========================================

        public async Task<List<CounselingProfile>> GetProfilesAsync()
        {
            try
            {
                return await _db.CounselingProfiles.OrderByDescending(p => p.CreatedAt).ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CounselingService: Error getting profiles");
                return new List<CounselingProfile>();
            }
        }

        public async Task<CounselingProfile?> GetProfileByIdAsync(int id)
        {
            return await _db.CounselingProfiles.FindAsync(id);
        }

        public async Task<CounselingProfile> CreateProfileAsync(CounselingProfile profile)
        {
            try
            {
                profile.CreatedAt = DateTime.Now;
                profile.Status = StatusConstants.CounselingStatus.Pending;
                _db.CounselingProfiles.Add(profile);
                await _db.SaveChangesAsync();
                return profile;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CounselingService: Error creating profile");
                throw;
            }
        }

        public async Task UpdateProfileStatusAsync(int id, string newStatus, DateTime? scheduledAt = null)
        {
            try
            {
                var profile = await _db.CounselingProfiles.FindAsync(id);
                if (profile != null)
                {
                    profile.Status = newStatus;
                    if (scheduledAt.HasValue)
                    {
                        profile.ScheduledAt = scheduledAt;
                    }
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CounselingService: Error updating profile status");
                throw;
            }
        }

        // ==========================================
        // 2. QU?N LƯ PHIÊN LÀM VI?C (SESSIONS)
        // ==========================================

        public async Task<List<CounselingSession>> GetSessionsByProfileIdAsync(int profileId)
        {
            try
            {
                return await _db.CounselingSessions
                    .Where(s => s.ProfileId == profileId)
                    .OrderByDescending(s => s.SessionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CounselingService: Error getting sessions");
                return new List<CounselingSession>();
            }
        }

        public async Task<CounselingSession> CreateSessionAsync(CounselingSession session)
        {
            try
            {
                _db.CounselingSessions.Add(session);
                await _db.SaveChangesAsync();
                return session;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CounselingService: Error creating session");
                throw;
            }
        }

        // ==========================================
        // 3. TH?NG KÊ (DASHBOARD)
        // ==========================================

        public async Task<CounselingDashboardStats> GetDashboardStatsAsync()
        {
            try
            {
                var allProfiles = await _db.CounselingProfiles.ToListAsync();
                
                var stats = new CounselingDashboardStats
                {
                    TotalRequests = allProfiles.Count,
                    PendingRequests = allProfiles.Count(p => p.Status == StatusConstants.CounselingStatus.Pending),
                    ScheduledRequests = allProfiles.Count(p => p.Status == StatusConstants.CounselingStatus.Scheduled),
                    InProgressRequests = allProfiles.Count(p => p.Status == StatusConstants.CounselingStatus.InProgress),
                    CompletedRequests = allProfiles.Count(p => p.Status == StatusConstants.CounselingStatus.Completed)
                };

                stats.IssueDistribution = allProfiles
                    .GroupBy(p => p.ProblemType)
                    .ToDictionary(g => string.IsNullOrWhiteSpace(g.Key) ? "Khác" : g.Key, g => g.Count());

                return stats;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CounselingService: Error getting stats");
                return new CounselingDashboardStats();
            }
        }
    }
}
