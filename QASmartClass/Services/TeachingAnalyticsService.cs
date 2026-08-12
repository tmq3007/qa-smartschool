using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Services
{
    /// <summary>
    /// Teaching Analytics Service — T? d?ng ghi log ho?t d?ng gi?ng d?y.
    /// Capture: th?i gian d?y, công c? s? d?ng, s? HS tham gia, bài gi?ng hoàn thành.
    /// </summary>
    public class TeachingAnalyticsService
    {
        private static TeachingAnalyticsService? _instance;
        public static TeachingAnalyticsService Instance => _instance ??= new TeachingAnalyticsService();

        // --- Session Tracking ---
        private DateTime? _sessionStart;
        private string _currentTeacherId = "";
        private string _currentClassId = "";
        private string _currentLessonId = "";
        private readonly List<ToolUsageEntry> _toolUsages = new();

        // -------------------------------------------------------
        //  SESSION LIFECYCLE
        // -------------------------------------------------------

        /// <summary>B?t d?u phiên gi?ng d?y</summary>
        public void LogSessionStart(string teacherId, string classId, string lessonId = "")
        {
            _sessionStart = DateTime.Now;
            _currentTeacherId = teacherId;
            _currentClassId = classId;
            _currentLessonId = lessonId;
            _toolUsages.Clear();

            Log.Information("[TeachingAnalytics] Session started: Teacher={Teacher}, Class={Class}, Lesson={Lesson}",
                teacherId, classId, lessonId);

            // Ghi event log
            LogEvent("SessionStart", $"GV {teacherId} b?t d?u d?y l?p {classId}");
        }

        /// <summary>K?t thúc phiên gi?ng d?y</summary>
        public async Task LogSessionEndAsync()
        {
            if (_sessionStart == null) return;

            var duration = DateTime.Now - _sessionStart.Value;
            Log.Information("[TeachingAnalytics] Session ended: Duration={Duration:hh\\:mm\\:ss}, Tools={ToolCount}",
                duration, _toolUsages.Count);

            // Luu phiên d?y vào DB
            try
            {
                using var db = new AppDbContext();
                db.EventLogs.Add(new EventLog
                {
                    EventType = "TeachingSession",
                    Details = $"GV: {_currentTeacherId} | Lớp: {_currentClassId} | " +
                              $"Th?i lu?ng: {duration:hh\\:mm\\:ss} | " +
                              $"Công c?: {_toolUsages.Count} lo?i | " +
                              $"Bài: {_currentLessonId}",
                    Timestamp = DateTime.Now,
                    Actor = _currentTeacherId
                });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[TeachingAnalytics] Failed to save session");
            }

            // Reset
            _sessionStart = null;
            _currentTeacherId = "";
            _currentClassId = "";
            _currentLessonId = "";
            _toolUsages.Clear();
        }

        // -------------------------------------------------------
        //  TOOL USAGE TRACKING
        // -------------------------------------------------------

        /// <summary>Ghi nh?n s? d?ng công c?</summary>
        public void LogToolUsage(string toolName, TimeSpan? duration = null)
        {
            var existing = _toolUsages.FirstOrDefault(t => t.ToolName == toolName);
            if (existing != null)
            {
                existing.UseCount++;
                existing.TotalDuration += duration ?? TimeSpan.Zero;
            }
            else
            {
                _toolUsages.Add(new ToolUsageEntry
                {
                    ToolName = toolName,
                    UseCount = 1,
                    TotalDuration = duration ?? TimeSpan.Zero,
                    FirstUsed = DateTime.Now
                });
            }

            Log.Debug("[TeachingAnalytics] Tool used: {Tool}", toolName);
        }

        /// <summary>Ghi nh?n chuy?n trang (Navigation)</summary>
        public void LogPageVisit(string pageId)
        {
            LogToolUsage($"Page:{pageId}");
        }

        /// <summary>Ghi nh?n Quiz dă th?c hi?n</summary>
        public void LogQuizCompleted(string quizId, int studentCount, double avgScore)
        {
            LogEvent("QuizCompleted", $"Quiz: {quizId} | HS: {studentCount} | TB: {avgScore:F1}d");
        }

        // -------------------------------------------------------
        //  REPORTING
        // -------------------------------------------------------

        /// <summary>Th?ng kê tu?n cho 1 GV</summary>
        public async Task<TeachingReport> GenerateWeeklyReportAsync(string teacherId)
        {
            var report = new TeachingReport { TeacherId = teacherId };

            try
            {
                using var db = new AppDbContext();
                var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
                var weekEnd = weekStart.AddDays(7);

                var sessions = await db.EventLogs
                    .Where(l => l.EventType == "TeachingSession"
                             && l.Actor == teacherId
                             && l.Timestamp >= weekStart
                             && l.Timestamp < weekEnd)
                    .ToListAsync();

                report.TotalSessions = sessions.Count;
                report.WeekStart = weekStart;
                report.WeekEnd = weekEnd;

                // Parse durations from Details
                foreach (var session in sessions)
                {
                    var details = session.Details ?? "";
                    if (details.Contains("Th?i lu?ng:"))
                    {
                        var durationPart = details.Split("Th?i lu?ng:").LastOrDefault()?.Split('|').FirstOrDefault()?.Trim();
                        if (TimeSpan.TryParse(durationPart, out var dur))
                            report.TotalTeachingTime += dur;
                    }
                }

                // Quizzes this week
                var quizzes = await db.EventLogs
                    .Where(l => l.EventType == "QuizCompleted"
                             && l.Actor == teacherId
                             && l.Timestamp >= weekStart
                             && l.Timestamp < weekEnd)
                    .CountAsync();
                report.QuizzesGiven = quizzes;

                Log.Information("[TeachingAnalytics] Weekly report generated for {Teacher}: {Sessions} sessions",
                    teacherId, report.TotalSessions);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[TeachingAnalytics] Report generation failed for {Teacher}", teacherId);
            }

            return report;
        }

        // -------------------------------------------------------
        //  HELPERS
        // -------------------------------------------------------

        private static void LogEvent(string eventType, string details)
        {
            try
            {
                using var db = new AppDbContext();
                db.EventLogs.Add(new EventLog
                {
                    EventType = eventType,
                    Details = details,
                    Timestamp = DateTime.Now
                });
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[TeachingAnalytics] LogEvent failed: {Type}", eventType);
            }
        }

        /// <summary>Phiên hi?n tại có dang active?</summary>
        public bool IsSessionActive => _sessionStart != null;

        /// <summary>Th?i lu?ng phiên hi?n tại</summary>
        public TimeSpan CurrentSessionDuration =>
            _sessionStart.HasValue ? DateTime.Now - _sessionStart.Value : TimeSpan.Zero;
    }

    // -------------------------------------------------------
    //  DATA MODELS
    // -------------------------------------------------------

    /// <summary>Entry theo dơi s? d?ng công c?</summary>
    public class ToolUsageEntry
    {
        public string ToolName { get; set; } = "";
        public int UseCount { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public DateTime FirstUsed { get; set; }
    }

    /// <summary>Báo cáo gi?ng d?y tu?n</summary>
    public class TeachingReport
    {
        public string TeacherId { get; set; } = "";
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public int TotalSessions { get; set; }
        public TimeSpan TotalTeachingTime { get; set; }
        public int QuizzesGiven { get; set; }
        public List<ToolUsageEntry> TopTools { get; set; } = new();
    }
}

