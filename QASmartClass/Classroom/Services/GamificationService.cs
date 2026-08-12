using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    /// <summary>
    /// Gamification Engine — Phase 6
    /// 
    /// Tracks student XP, badges, and leaderboard across all activities:
    ///   - Quiz completion, correct answers, streaks
    ///   - Attendance, hand raises, homework submission
    ///   - Tool exploration, learning milestones
    ///
    /// Broadcasts live leaderboard updates via WebSocket.
    /// </summary>
    public class GamificationService
    {
        private readonly WebSocketBridgeService? _wsBridge;

        // Student profiles: Code → Profile
        private readonly ConcurrentDictionary<string, StudentGamificationProfile> _profiles = new();

        // Badge definitions
        public static readonly List<BadgeDefinition> AllBadges = new()
        {
            // ─── Quiz Badges ───
            new("quiz_first",     "🎯 First Quiz",       "Hoàn thành quiz đầu tiên",         50,  "quiz",  1),
            new("quiz_5",         "📝 Quiz Regular",      "Hoàn thành 5 quiz",                100, "quiz",  5),
            new("quiz_20",        "🏆 Quiz Master",       "Hoàn thành 20 quiz",               300, "quiz",  20),
            new("quiz_perfect",   "💎 Perfect Score",     "Đạt 10/10 trong 1 quiz",           200, "quiz",  0),
            new("streak_3",       "🔥 Streak 3",         "Trả lời đúng 3 câu liên tiếp",     50,  "quiz",  0),
            new("streak_5",       "🔥🔥 Streak 5",       "Trả lời đúng 5 câu liên tiếp",     100, "quiz",  0),
            new("streak_10",      "🔥🔥🔥 Streak 10",    "Trả lời đúng 10 câu liên tiếp",    250, "quiz",  0),
            new("speed_demon",    "⚡ Speed Demon",      "Hoàn thành quiz dưới 3 phút",       150, "quiz",  0),

            // ─── Attendance Badges ───
            new("attend_first",   "📌 First Day",        "Tham gia lớp học lần đầu",         30,  "attend", 1),
            new("attend_5",       "🗓️ Regular",          "Tham gia 5 buổi học",              100, "attend", 5),
            new("attend_20",      "📅 Dedicated",        "Tham gia 20 buổi học",             300, "attend", 20),
            new("early_bird",     "🐦 Early Bird",       "Kết nối trước GV 2 phút",          50,  "attend", 0),

            // ─── Learning Badges ───
            new("tool_explorer",  "🔧 Tool Explorer",    "Sử dụng 5 công cụ khác nhau",     100, "learn",  5),
            new("tool_master",    "🛠️ Tool Master",      "Sử dụng 15 công cụ khác nhau",    250, "learn",  15),
            new("chapter_1",      "📖 First Chapter",    "Hoàn thành 1 chương Toán",         100, "learn",  1),
            new("chapter_5",      "📚 Bookworm",         "Hoàn thành 5 chương Toán",         300, "learn",  5),
            new("homework_1",     "📄 First Homework",   "Nộp bài tập đầu tiên",             50,  "learn",  1),
            new("homework_10",    "📋 HW Pro",           "Nộp 10 bài tập",                   200, "learn",  10),

            // ─── Social Badges ───
            new("hand_raise_1",   "✋ Brave",             "Giơ tay phát biểu lần đầu",       30,  "social", 1),
            new("hand_raise_10",  "🙋 Active",            "Giơ tay 10 lần",                  100, "social", 10),
            new("chat_helpful",   "💬 Helpful",           "Gửi 5 tin nhắn hỗ trợ bạn",      100, "social", 5),
            new("group_a",        "🟢 Group A",           "Đạt nhóm A sau quiz",             150, "social", 0),

            // ─── Milestone Badges ───
            new("xp_100",         "⭐ Rising Star",      "Đạt 100 XP",                       0,  "milestone", 100),
            new("xp_500",         "🌟 Scholar",           "Đạt 500 XP",                      0,  "milestone", 500),
            new("xp_1000",        "💫 Expert",            "Đạt 1000 XP",                     0,  "milestone", 1000),
            new("xp_2500",        "🏅 Legend",            "Đạt 2500 XP",                     0,  "milestone", 2500),
            new("level_5",        "📊 Level 5",           "Đạt Level 5",                     0,  "milestone", 0),
            new("level_10",       "🎖️ Level 10",         "Đạt Level 10",                    0,  "milestone", 0),
        };

        // ─── Events ───
        public event EventHandler<XpEarnedEventArgs>? XpEarned;
        public event EventHandler<BadgeEarnedEventArgs>? BadgeEarned;
        public event EventHandler? LeaderboardUpdated;

        // ═══════════════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════

        public GamificationService(WebSocketBridgeService? wsBridge = null)
        {
            _wsBridge = wsBridge;
        }

        // ═══════════════════════════════════════════════════════════
        //  XP MANAGEMENT
        // ═══════════════════════════════════════════════════════════

        /// <summary>Award XP to a student</summary>
        public StudentGamificationProfile AwardXp(string studentCode, int xp, string reason)
        {
            var profile = GetOrCreateProfile(studentCode);

            profile.TotalXp += xp;
            profile.Level = CalculateLevel(profile.TotalXp);
            profile.LevelProgress = CalculateLevelProgress(profile.TotalXp);
            profile.XpHistory.Add(new XpEvent
            {
                Amount = xp,
                Reason = reason,
                Timestamp = DateTime.Now
            });

            // Keep only last 50 XP events
            if (profile.XpHistory.Count > 50)
                profile.XpHistory.RemoveRange(0, profile.XpHistory.Count - 50);

            XpEarned?.Invoke(this, new XpEarnedEventArgs
            {
                StudentCode = studentCode,
                Amount = xp,
                Reason = reason,
                TotalXp = profile.TotalXp,
                Level = profile.Level
            });

            // Check milestone badges
            CheckMilestoneBadges(profile);

            Log.Debug("Gamification: {Code} +{XP} XP ({Reason}) → Total: {Total}, Level: {Level}",
                studentCode, xp, reason, profile.TotalXp, profile.Level);

            return profile;
        }

        /// <summary>Award XP for quiz result</summary>
        public void AwardQuizXp(string studentCode, int correct, int total, int durationSecs)
        {
            var profile = GetOrCreateProfile(studentCode);
            profile.QuizCount++;

            // Base XP: 10 per correct answer
            int baseXp = correct * 10;

            // Perfect bonus
            if (correct == total && total > 0)
            {
                baseXp += 50;
                TryAwardBadge(profile, "quiz_perfect");
            }

            // Speed bonus (< 3 min for 10-question quiz)
            if (durationSecs < 180 && total >= 10)
            {
                baseXp += 30;
                TryAwardBadge(profile, "speed_demon");
            }

            // Streak tracking
            if (correct > 0)
            {
                profile.CurrentStreak += correct;
                if (profile.CurrentStreak >= 10) TryAwardBadge(profile, "streak_10");
                else if (profile.CurrentStreak >= 5) TryAwardBadge(profile, "streak_5");
                else if (profile.CurrentStreak >= 3) TryAwardBadge(profile, "streak_3");
            }
            else
            {
                profile.CurrentStreak = 0;
            }

            // Quiz count badges
            if (profile.QuizCount >= 20) TryAwardBadge(profile, "quiz_20");
            else if (profile.QuizCount >= 5) TryAwardBadge(profile, "quiz_5");
            else if (profile.QuizCount >= 1) TryAwardBadge(profile, "quiz_first");

            // Group A badge
            double score = total > 0 ? (double)correct / total * 10 : 0;
            if (score >= 8.0) TryAwardBadge(profile, "group_a");

            AwardXp(studentCode, baseXp, $"Quiz: {correct}/{total}");
        }

        /// <summary>Award XP for attendance</summary>
        public void AwardAttendanceXp(string studentCode, bool isEarlyBird = false)
        {
            var profile = GetOrCreateProfile(studentCode);
            profile.AttendanceCount++;

            int xp = 20; // Base attendance XP

            if (isEarlyBird)
            {
                xp += 10;
                TryAwardBadge(profile, "early_bird");
            }

            if (profile.AttendanceCount >= 20) TryAwardBadge(profile, "attend_20");
            else if (profile.AttendanceCount >= 5) TryAwardBadge(profile, "attend_5");
            else if (profile.AttendanceCount >= 1) TryAwardBadge(profile, "attend_first");

            AwardXp(studentCode, xp, "Attendance");
        }

        /// <summary>Award XP for tool usage</summary>
        public void AwardToolExploreXp(string studentCode, string toolId)
        {
            var profile = GetOrCreateProfile(studentCode);
            if (!profile.ToolsUsed.Contains(toolId))
            {
                profile.ToolsUsed.Add(toolId);

                if (profile.ToolsUsed.Count >= 15) TryAwardBadge(profile, "tool_master");
                else if (profile.ToolsUsed.Count >= 5) TryAwardBadge(profile, "tool_explorer");

                AwardXp(studentCode, 15, $"Tool explore: {toolId}");
            }
        }

        /// <summary>Award XP for homework submission</summary>
        public void AwardHomeworkXp(string studentCode)
        {
            var profile = GetOrCreateProfile(studentCode);
            profile.HomeworkCount++;

            if (profile.HomeworkCount >= 10) TryAwardBadge(profile, "homework_10");
            else if (profile.HomeworkCount >= 1) TryAwardBadge(profile, "homework_1");

            AwardXp(studentCode, 25, "Homework submitted");
        }

        /// <summary>Award XP for hand raise</summary>
        public void AwardHandRaiseXp(string studentCode)
        {
            var profile = GetOrCreateProfile(studentCode);
            profile.HandRaiseCount++;

            if (profile.HandRaiseCount >= 10) TryAwardBadge(profile, "hand_raise_10");
            else if (profile.HandRaiseCount >= 1) TryAwardBadge(profile, "hand_raise_1");

            AwardXp(studentCode, 10, "Hand raise");
        }

        // ═══════════════════════════════════════════════════════════
        //  BADGES
        // ═══════════════════════════════════════════════════════════

        private void TryAwardBadge(StudentGamificationProfile profile, string badgeId)
        {
            if (profile.EarnedBadges.Any(b => b.BadgeId == badgeId)) return;

            var badge = AllBadges.FirstOrDefault(b => b.Id == badgeId);
            if (badge == null) return;

            var earned = new EarnedBadge
            {
                BadgeId = badge.Id,
                Name = badge.Name,
                Description = badge.Description,
                XpReward = badge.XpReward,
                EarnedAt = DateTime.Now
            };

            profile.EarnedBadges.Add(earned);

            // Award badge XP (without re-checking milestone to avoid recursion)
            if (badge.XpReward > 0)
            {
                profile.TotalXp += badge.XpReward;
                profile.Level = CalculateLevel(profile.TotalXp);
                profile.LevelProgress = CalculateLevelProgress(profile.TotalXp);
            }

            BadgeEarned?.Invoke(this, new BadgeEarnedEventArgs
            {
                StudentCode = profile.StudentCode,
                Badge = earned
            });

            Log.Information("Gamification: {Code} earned badge '{Badge}' (+{XP} XP)",
                profile.StudentCode, badge.Name, badge.XpReward);
        }

        private void CheckMilestoneBadges(StudentGamificationProfile profile)
        {
            if (profile.TotalXp >= 2500) TryAwardBadge(profile, "xp_2500");
            else if (profile.TotalXp >= 1000) TryAwardBadge(profile, "xp_1000");
            else if (profile.TotalXp >= 500) TryAwardBadge(profile, "xp_500");
            else if (profile.TotalXp >= 100) TryAwardBadge(profile, "xp_100");

            if (profile.Level >= 10) TryAwardBadge(profile, "level_10");
            else if (profile.Level >= 5) TryAwardBadge(profile, "level_5");
        }

        // ═══════════════════════════════════════════════════════════
        //  LEADERBOARD
        // ═══════════════════════════════════════════════════════════

        /// <summary>Get sorted leaderboard</summary>
        public List<LeaderboardEntry> GetLeaderboard()
        {
            return _profiles.Values
                .OrderByDescending(p => p.TotalXp)
                .Select((p, idx) => new LeaderboardEntry
                {
                    Rank = idx + 1,
                    StudentCode = p.StudentCode,
                    StudentName = p.StudentName,
                    TotalXp = p.TotalXp,
                    Level = p.Level,
                    LevelProgress = p.LevelProgress,
                    BadgeCount = p.EarnedBadges.Count,
                    QuizCount = p.QuizCount,
                    CurrentStreak = p.CurrentStreak
                }).ToList();
        }

        /// <summary>Broadcast leaderboard update to all students</summary>
        public async Task BroadcastLeaderboardAsync()
        {
            if (_wsBridge == null) return;

            var leaderboard = GetLeaderboard().Take(20);

            var payload = JsonSerializer.Serialize(new
            {
                type = "cmd",
                action = "LEADERBOARD_UPDATE",
                data = JsonSerializer.Serialize(new
                {
                    entries = leaderboard.Select(e => new
                    {
                        rank = e.Rank,
                        code = e.StudentCode,
                        name = e.StudentName,
                        xp = e.TotalXp,
                        level = e.Level,
                        badges = e.BadgeCount,
                        streak = e.CurrentStreak
                    }).ToArray()
                })
            });

            await _wsBridge.BroadcastJsonToWebClients(payload);
            LeaderboardUpdated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Broadcast badge notification to a specific student</summary>
        public async Task NotifyBadgeEarnedAsync(string studentCode, EarnedBadge badge)
        {
            if (_wsBridge == null) return;

            var payload = JsonSerializer.Serialize(new
            {
                type = "cmd",
                action = "BADGE_EARNED",
                data = JsonSerializer.Serialize(new
                {
                    badgeId = badge.BadgeId,
                    name = badge.Name,
                    description = badge.Description,
                    xpReward = badge.XpReward
                })
            });

            await _wsBridge.SendToWebClient(studentCode, $"CMD|BADGE_EARNED|{studentCode}|{payload}");
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════

        public StudentGamificationProfile GetOrCreateProfile(string studentCode)
        {
            return _profiles.GetOrAdd(studentCode, code => new StudentGamificationProfile
            {
                StudentCode = code,
                CreatedAt = DateTime.Now
            });
        }

        public void SetStudentName(string code, string name)
        {
            var profile = GetOrCreateProfile(code);
            profile.StudentName = name;
        }

        /// <summary>
        /// Level calculation: XP thresholds increase progressively.
        /// Level 1: 0 XP, Level 2: 100 XP, Level 3: 250 XP, Level 4: 450 XP...
        /// Formula: XP_needed(n) = 50 * n * (n + 1) / 2
        /// </summary>
        private static int CalculateLevel(int totalXp)
        {
            int level = 1;
            int accumulated = 0;
            while (true)
            {
                int needed = 50 * level;
                if (accumulated + needed > totalXp) break;
                accumulated += needed;
                level++;
            }
            return level;
        }

        private static double CalculateLevelProgress(int totalXp)
        {
            int level = 1;
            int accumulated = 0;
            while (true)
            {
                int needed = 50 * level;
                if (accumulated + needed > totalXp)
                    return (double)(totalXp - accumulated) / needed;
                accumulated += needed;
                level++;
            }
        }

        /// <summary>Get all profiles for export/reporting</summary>
        public IReadOnlyCollection<StudentGamificationProfile> GetAllProfiles()
            => _profiles.Values.ToList().AsReadOnly();

        /// <summary>Reset all gamification data (for new semester)</summary>
        public void ResetAll()
        {
            _profiles.Clear();
            Log.Information("Gamification: All data reset");
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  DATA MODELS
    // ═══════════════════════════════════════════════════════════

    public class StudentGamificationProfile
    {
        public string StudentCode { get; set; } = "";
        public string StudentName { get; set; } = "";
        public int TotalXp { get; set; }
        public int Level { get; set; } = 1;
        public double LevelProgress { get; set; }

        // Activity counters
        public int QuizCount { get; set; }
        public int AttendanceCount { get; set; }
        public int HomeworkCount { get; set; }
        public int HandRaiseCount { get; set; }
        public int CurrentStreak { get; set; }

        // Collections
        public List<EarnedBadge> EarnedBadges { get; set; } = new();
        public List<XpEvent> XpHistory { get; set; } = new();
        public HashSet<string> ToolsUsed { get; set; } = new();

        public DateTime CreatedAt { get; set; }
    }

    public class EarnedBadge
    {
        [JsonPropertyName("badgeId")] public string BadgeId { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("description")] public string Description { get; set; } = "";
        [JsonPropertyName("xpReward")] public int XpReward { get; set; }
        [JsonPropertyName("earnedAt")] public DateTime EarnedAt { get; set; }
    }

    public class XpEvent
    {
        public int Amount { get; set; }
        public string Reason { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    public class BadgeDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int XpReward { get; }
        public string Category { get; }
        public int Threshold { get; }

        public BadgeDefinition(string id, string name, string desc, int xp, string category, int threshold)
        {
            Id = id; Name = name; Description = desc;
            XpReward = xp; Category = category; Threshold = threshold;
        }
    }

    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public string StudentCode { get; set; } = "";
        public string StudentName { get; set; } = "";
        public int TotalXp { get; set; }
        public int Level { get; set; }
        public double LevelProgress { get; set; }
        public int BadgeCount { get; set; }
        public int QuizCount { get; set; }
        public int CurrentStreak { get; set; }
    }

    // ─── Events ───
    public class XpEarnedEventArgs : EventArgs
    {
        public string StudentCode { get; set; } = "";
        public int Amount { get; set; }
        public string Reason { get; set; } = "";
        public int TotalXp { get; set; }
        public int Level { get; set; }
    }

    public class BadgeEarnedEventArgs : EventArgs
    {
        public string StudentCode { get; set; } = "";
        public EarnedBadge Badge { get; set; } = new();
    }
}
