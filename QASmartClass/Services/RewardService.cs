using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.Services
{
    /// <summary>P1-02: XP / Huy hieu / Leaderboard</summary>
    public class RewardService
    {
        private readonly AppDbContext _db;

        public RewardService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>Cong XP cho hoc sinh va kiem tra huy hieu moi.</summary>
        public int AddXp(int studentId, int amount, string reason)
        {
            try
            {
                var student = _db.Students.Find(studentId);
                if (student == null) return 0;

                student.TotalXp += amount;
                _db.SaveChanges();
                Log.Information("[Reward] +{Xp} XP for Student {Id}: {Reason}", amount, studentId, reason);

                CheckAndAwardBadges(studentId);
                return student.TotalXp;
            }
            catch (Exception ex) { Log.Error(ex, "AddXp error"); return 0; }
        }

        /// <summary>Kiem tra va trao huy hieu neu du dieu kien.</summary>
        public List<Badge> CheckAndAwardBadges(int studentId)
        {
            var awarded = new List<Badge>();
            try
            {
                var student = _db.Students.Find(studentId);
                if (student == null) return awarded;

                var allBadges = _db.Badges.ToList();
                var existing = _db.UserBadges
                    .Where(ub => ub.UserId == studentId.ToString())
                    .Select(ub => ub.BadgeId)
                    .ToList();

                foreach (var badge in allBadges)
                {
                    if (!existing.Contains(badge.Id) && student.TotalXp >= badge.RequiredPoints)
                    {
                        _db.UserBadges.Add(new UserBadge
                        {
                            UserId = studentId.ToString(),
                            BadgeId = badge.Id,
                            AwardedAt = DateTime.Now
                        });
                        awarded.Add(badge);
                        Log.Information("[Reward] Badge '{Name}' awarded to Student {Id}", badge.Name, studentId);
                    }
                }
                if (awarded.Any()) _db.SaveChanges();
            }
            catch (Exception ex) { Log.Error(ex, "CheckAndAwardBadges error"); }
            return awarded;
        }

        /// <summary>Lay top N hoc sinh theo XP trong lop.</summary>
        public List<LeaderboardEntry> GetLeaderboard(string className, int top = 10)
        {
            try
            {
                var students = _db.Students
                    .Where(s => s.ClassName == className && s.Status == "Active")
                    .OrderByDescending(s => s.TotalXp)
                    .Take(top)
                    .ToList();

                return students.Select((s, i) => new LeaderboardEntry
                {
                    Rank = i + 1,
                    StudentName = s.FullName,
                    TotalXp = s.TotalXp,
                    BadgeCount = _db.UserBadges.Count(ub => ub.UserId == s.Id.ToString())
                }).ToList();
            }
            catch (Exception ex) { Log.Error(ex, "GetLeaderboard error"); return new List<LeaderboardEntry>(); }
        }

        /// <summary>Lay danh sach huy hieu da dat cua HS.</summary>
        public List<Badge> GetStudentBadges(int studentId)
        {
            try
            {
                var badgeIds = _db.UserBadges
                    .Where(ub => ub.UserId == studentId.ToString())
                    .Select(ub => ub.BadgeId).ToList();
                return _db.Badges.Where(b => badgeIds.Contains(b.Id)).ToList();
            }
            catch { return new List<Badge>(); }
        }
    }

    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int TotalXp { get; set; }
        public int BadgeCount { get; set; }
    }
}

