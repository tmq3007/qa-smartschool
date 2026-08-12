using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class TelemetryService
    {
        private static TelemetryService? _instance;
        private static readonly object _lock = new object();
        private readonly SemaphoreSlim _dbSemaphore = new SemaphoreSlim(1, 1);
        private Stopwatch _sessionStopwatch;

        private TelemetryService()
        {
            _sessionStopwatch = new Stopwatch();
        }

        public static TelemetryService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new TelemetryService();
                        }
                    }
                }
                return _instance;
            }
        }

        public void StartSession()
        {
            _sessionStopwatch.Restart();
            TrackAsync("SESSION_START", "App started").ConfigureAwait(false);
        }

        public void EndSession()
        {
            _sessionStopwatch.Stop();
            // Block to ensure it writes before exit
            TrackAsync("SESSION_END", "App closing", _sessionStopwatch.ElapsedMilliseconds).GetAwaiter().GetResult();
        }

        public async Task TrackAsync(string eventType, object? eventData = null, long durationMs = 0)
        {
            try
            {
                string dataStr = string.Empty;
                if (eventData != null)
                {
                    if (eventData is string str) dataStr = str;
                    else dataStr = JsonSerializer.Serialize(eventData);
                }

                var log = new UsageLog
                {
                    EventType = eventType,
                    EventData = dataStr,
                    UserRole = "Admin", // For now hardcode or get from auth
                    Timestamp = DateTime.Now,
                    DurationMs = durationMs
                };

                await _dbSemaphore.WaitAsync();
                try
                {
                    using var db = new AppDbContext();
                    db.UsageLogs.Add(log);
                    await db.SaveChangesAsync();
                }
                finally
                {
                    _dbSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                // Fallback to file log so we don't crash the app
                Serilog.Log.Error($"Telemetry error: {ex.Message}");
            }
        }

        // Fire-and-forget helper
        public void Track(string eventType, object? eventData = null, long durationMs = 0)
        {
            _ = TrackAsync(eventType, eventData, durationMs);
        }
    }
}
