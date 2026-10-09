using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    public class PeriodTimeConfig
    {
        public int Period { get; set; } // 1-10
        public string Start { get; set; } = ""; // "07:00"
        public string End { get; set; } = "";   // "07:45"
    }

    /// <summary>
    /// Service quản lý khung giờ học các tiết (Giờ mùa hè / mùa đông / tùy chỉnh theo trường)
    /// </summary>
    public class PeriodScheduleService
    {
        private static PeriodScheduleService? _instance;
        public static PeriodScheduleService Instance => _instance ??= new PeriodScheduleService();

        public const string SettingKey = "Timetable_Periods_Config";
        public const string SeasonKey = "Timetable_Season_Preset";

        public event EventHandler? ScheduleChanged;

        private List<PeriodTimeConfig> _currentConfig = new();
        private (int StartH, int StartM, int EndH, int EndM)[] _timeRanges = Array.Empty<(int, int, int, int)>();
        private string[] _periodTimes = Array.Empty<string>();
        private string _activePreset = "Summer";
        private bool _isLoaded = false;

        public string ActivePreset => _activePreset;

        public PeriodScheduleService()
        {
            ApplyConfig(GetPresetSummer(), "Summer");
        }

        public void InvalidateCache() => _isLoaded = false;

        public void EnsureLoaded(AppDbContext db)
        {
            if (_isLoaded) return;

            try
            {
                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == SettingKey);
                var season = db.SystemSettings.FirstOrDefault(s => s.Id == SeasonKey)?.Value ?? "Summer";

                if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
                {
                    var loaded = JsonSerializer.Deserialize<List<PeriodTimeConfig>>(setting.Value);
                    if (loaded != null && loaded.Count == 10)
                    {
                        ApplyConfig(loaded, season);
                        _isLoaded = true;
                        Log.Information("[PeriodScheduleService] Loaded period schedule from DB ({Season}, 10 periods)", season);
                        return;
                    }
                }

                // Nếu chưa có, tự động nhận diện theo tháng: Tháng 11 - Tháng 2 là mùa đông, còn lại mùa hè
                int month = DateTime.Today.Month;
                if (month >= 11 || month <= 2)
                {
                    ApplyConfig(GetPresetWinter(), "Winter");
                    SaveSchedule(db, GetPresetWinter(), "Winter");
                }
                else
                {
                    ApplyConfig(GetPresetSummer(), "Summer");
                    SaveSchedule(db, GetPresetSummer(), "Summer");
                }
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                Log.Warning("[PeriodScheduleService] EnsureLoaded error: {Err}", ex.Message);
                ApplyConfig(GetPresetSummer(), "Summer");
                _isLoaded = true;
            }
        }

        public (int StartH, int StartM, int EndH, int EndM)[] GetPeriodTimeRanges() => _timeRanges;

        public string[] GetPeriodTimes() => _periodTimes;

        public List<PeriodTimeConfig> GetCurrentConfig()
        {
            return _currentConfig.Select(c => new PeriodTimeConfig
            {
                Period = c.Period,
                Start = c.Start,
                End = c.End
            }).ToList();
        }

        public void SaveSchedule(AppDbContext db, List<PeriodTimeConfig> configs, string presetName)
        {
            try
            {
                ApplyConfig(configs, presetName);

                var json = JsonSerializer.Serialize(configs);
                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == SettingKey);
                if (setting == null)
                {
                    db.SystemSettings.Add(new SystemSetting
                    {
                        Id = SettingKey,
                        Value = json,
                        Category = "Timetable",
                        LastUpdated = DateTime.Now
                    });
                }
                else
                {
                    setting.Value = json;
                    setting.LastUpdated = DateTime.Now;
                }

                var seasonSetting = db.SystemSettings.FirstOrDefault(s => s.Id == SeasonKey);
                if (seasonSetting == null)
                {
                    db.SystemSettings.Add(new SystemSetting
                    {
                        Id = SeasonKey,
                        Value = presetName,
                        Category = "Timetable",
                        LastUpdated = DateTime.Now
                    });
                }
                else
                {
                    seasonSetting.Value = presetName;
                    seasonSetting.LastUpdated = DateTime.Now;
                }

                db.SaveChanges();
                _isLoaded = true;
                Log.Information("[PeriodScheduleService] Saved period schedule to DB ({Preset})", presetName);

                ScheduleChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[PeriodScheduleService] SaveSchedule error");
            }
        }

        private void ApplyConfig(List<PeriodTimeConfig> configs, string presetName)
        {
            _currentConfig = configs.OrderBy(c => c.Period).ToList();
            _activePreset = presetName;

            var ranges = new List<(int, int, int, int)>();
            var times = new List<string>();

            foreach (var c in _currentConfig)
            {
                var sParts = c.Start.Split(':');
                var eParts = c.End.Split(':');
                int sh = int.Parse(sParts[0]), sm = int.Parse(sParts[1]);
                int eh = int.Parse(eParts[0]), em = int.Parse(eParts[1]);

                ranges.Add((sh, sm, eh, em));
                times.Add($"{c.Start} – {c.End}");
            }

            _timeRanges = ranges.ToArray();
            _periodTimes = times.ToArray();
        }

        // ═══════════════════════════════════════════════════════════
        //  CÁC CẤU HÌNH MẪU (PRESETS)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Giờ Mùa Hè: Sáng 07:00, Chiều 13:30 (Vào học sớm tránh nắng nóng)
        /// </summary>
        public static List<PeriodTimeConfig> GetPresetSummer()
        {
            return new List<PeriodTimeConfig>
            {
                new() { Period = 1, Start = "07:00", End = "07:45" },
                new() { Period = 2, Start = "07:50", End = "08:35" },
                new() { Period = 3, Start = "08:45", End = "09:30" }, // Giải lao 10p sau tiết 2
                new() { Period = 4, Start = "09:35", End = "10:20" },
                new() { Period = 5, Start = "10:25", End = "11:10" },
                new() { Period = 6, Start = "13:30", End = "14:15" },
                new() { Period = 7, Start = "14:20", End = "15:05" },
                new() { Period = 8, Start = "15:15", End = "16:00" }, // Giải lao 10p sau tiết 7
                new() { Period = 9, Start = "16:05", End = "16:50" },
                new() { Period = 10, Start = "16:55", End = "17:40" }
            };
        }

        /// <summary>
        /// Giờ Mùa Đông: Sáng 07:30, Chiều 13:15 (Lùi 30p buổi sáng vì trời lạnh, vào sớm chiều)
        /// </summary>
        public static List<PeriodTimeConfig> GetPresetWinter()
        {
            return new List<PeriodTimeConfig>
            {
                new() { Period = 1, Start = "07:30", End = "08:15" },
                new() { Period = 2, Start = "08:20", End = "09:05" },
                new() { Period = 3, Start = "09:15", End = "10:00" }, // Giải lao 10p sau tiết 2
                new() { Period = 4, Start = "10:05", End = "10:50" },
                new() { Period = 5, Start = "10:55", End = "11:40" },
                new() { Period = 6, Start = "13:15", End = "14:00" },
                new() { Period = 7, Start = "14:05", End = "14:50" },
                new() { Period = 8, Start = "15:00", End = "15:45" }, // Giải lao 10p sau tiết 7
                new() { Period = 9, Start = "15:50", End = "16:35" },
                new() { Period = 10, Start = "16:40", End = "17:25" }
            };
        }

        /// <summary>
        /// Giờ Chuẩn 07:15: Sáng 07:15, Chiều 13:30 (Khung giờ phổ biến nhiều trường THPT)
        /// </summary>
        public static List<PeriodTimeConfig> GetPresetStandard()
        {
            return new List<PeriodTimeConfig>
            {
                new() { Period = 1, Start = "07:15", End = "08:00" },
                new() { Period = 2, Start = "08:05", End = "08:50" },
                new() { Period = 3, Start = "09:00", End = "09:45" }, // Giải lao 10p sau tiết 2
                new() { Period = 4, Start = "09:50", End = "10:35" },
                new() { Period = 5, Start = "10:40", End = "11:25" },
                new() { Period = 6, Start = "13:30", End = "14:15" },
                new() { Period = 7, Start = "14:20", End = "15:05" },
                new() { Period = 8, Start = "15:15", End = "16:00" }, // Giải lao 10p sau tiết 7
                new() { Period = 9, Start = "16:05", End = "16:50" },
                new() { Period = 10, Start = "16:55", End = "17:40" }
            };
        }

        /// <summary>
        /// Tự động tính toán nhanh 10 tiết dựa trên giờ bắt đầu buổi sáng & buổi chiều
        /// </summary>
        public static List<PeriodTimeConfig> CalculateSchedule(TimeSpan morningStart, TimeSpan afternoonStart, int durationMin = 45, int breakMin = 5, int longBreakMin = 10)
        {
            var list = new List<PeriodTimeConfig>();

            // Buổi sáng: 5 tiết
            var cur = morningStart;
            for (int p = 1; p <= 5; p++)
            {
                var end = cur.Add(TimeSpan.FromMinutes(durationMin));
                list.Add(new PeriodTimeConfig
                {
                    Period = p,
                    Start = $"{(int)cur.TotalHours:D2}:{cur.Minutes:D2}",
                    End = $"{(int)end.TotalHours:D2}:{end.Minutes:D2}"
                });

                int nextBreak = (p == 2) ? longBreakMin : breakMin;
                cur = end.Add(TimeSpan.FromMinutes(nextBreak));
            }

            // Buổi chiều: 5 tiết
            cur = afternoonStart;
            for (int p = 6; p <= 10; p++)
            {
                var end = cur.Add(TimeSpan.FromMinutes(durationMin));
                list.Add(new PeriodTimeConfig
                {
                    Period = p,
                    Start = $"{(int)cur.TotalHours:D2}:{cur.Minutes:D2}",
                    End = $"{(int)end.TotalHours:D2}:{end.Minutes:D2}"
                });

                int nextBreak = (p == 7) ? longBreakMin : breakMin;
                cur = end.Add(TimeSpan.FromMinutes(nextBreak));
            }

            return list;
        }
    }
}
