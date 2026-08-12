using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartLibrary.Desktop.Services
{
    public class StudentProgress
    {
        public string SsoUserId { get; set; } = "";
        public int XP { get; set; } = 0;
        public string Level { get; set; } = "Cấp 1: Độc giả Đồng";
        public string EquippedAvatar { get; set; } = "👦";
        public string EquippedBorder { get; set; } = "None"; // "None", "Neon", "Gold", "Mythic"
        public List<string> OwnedAvatars { get; set; } = new() { "👦", "👧" };
        public List<string> OwnedBorders { get; set; } = new() { "None" };
    }

    public static class GamificationService
    {
        private static readonly string DataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartLibrary", "data", "gamification.json");

        private static Dictionary<string, StudentProgress> _progressCache = new();
        private static ApiService? _apiService;

        static GamificationService()
        {
            LoadData();
        }

        public static void Initialize(ApiService? apiService = null)
        {
            _apiService = apiService;
            LoadData();
        }

        private static void LoadData()
        {
            try
            {
                var dir = Path.GetDirectoryName(DataPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (File.Exists(DataPath))
                {
                    var json = File.ReadAllText(DataPath);
                    var list = JsonSerializer.Deserialize<List<StudentProgress>>(json);
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            _progressCache[item.SsoUserId] = item;
                        }
                    }
                }
            }
            catch {}
        }

        private static void SaveData()
        {
            try
            {
                var list = new List<StudentProgress>(_progressCache.Values);
                var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(DataPath, json);
            }
            catch {}
        }

        public static StudentProgress GetProgress(string ssoId)
        {
            if (string.IsNullOrEmpty(ssoId)) return new StudentProgress();

            if (!_progressCache.TryGetValue(ssoId, out var progress))
            {
                int defaultXp = 50;
                if (ssoId == "HS001") defaultXp = 190; // Gần lên cấp Bạc
                else if (ssoId == "HS002") defaultXp = 485; // Gần lên cấp Vàng

                progress = new StudentProgress
                {
                    SsoUserId = ssoId,
                    XP = defaultXp,
                    Level = GetLevelName(defaultXp)
                };
                _progressCache[ssoId] = progress;
                SaveData();
            }

            return progress;
        }

        /// <summary>
        /// Đồng bộ điểm từ máy chủ về chạy ngầm (tránh deadlock)
        /// </summary>
        public static async Task LoadProgressFromServerAsync(string ssoId, Action<StudentProgress> callback)
        {
            if (string.IsNullOrEmpty(ssoId) || _apiService == null || ApiService.IsOfflineMode) return;
            try
            {
                var progressDto = await _apiService.GetAsync<ServerProgressDto>($"/XpShop/xp-progress/{ssoId}", silent: true);
                if (progressDto != null)
                {
                    var progress = new StudentProgress
                    {
                        SsoUserId = ssoId,
                        XP = progressDto.XP,
                        Level = progressDto.Level
                    };
                    _progressCache[ssoId] = progress;
                    SaveData();
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => callback(progress));
                }
            }
            catch {}
        }

        /// <summary>
        /// Đồng bộ điểm lên máy chủ chạy ngầm hoặc đồng bộ trực tiếp (Bản đồng bộ cho code cũ tương thích)
        /// </summary>
        public static (int NewXp, string NewLevel, bool LeveledUp, string OldLevel) AddXp(string ssoId, int amount)
        {
            if (string.IsNullOrEmpty(ssoId)) return (0, "Cấp 1: Độc giả Đồng", false, "Cấp 1: Độc giả Đồng");

            // 1. Cập nhật cục bộ trước (đảm bảo chạy offline mượt mà)
            var progress = GetProgress(ssoId);
            string oldLevel = progress.Level;
            int oldXp = progress.XP;
            int newXp = oldXp + amount;
            string newLevel = GetLevelName(newXp);
            bool leveledUp = oldLevel != newLevel;

            progress.XP = newXp;
            progress.Level = newLevel;
            _progressCache[ssoId] = progress;
            SaveData();

            // 2. Gửi đồng bộ lên máy chủ nếu online chạy ngầm để tránh lock UI thread
            if (_apiService != null && !ApiService.IsOfflineMode)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var payload = new { SsoUserId = ssoId, XpAmount = amount };
                        var res = await _apiService.PostAsync<object, ServerAddXpResponseDto>("/XpShop/add-xp", payload, silent: true);
                        if (res != null)
                        {
                            // Cập nhật lại giá trị chuẩn từ server
                            progress.XP = res.NewXp;
                            progress.Level = res.NewLevel;
                            _progressCache[ssoId] = progress;
                            SaveData();
                        }
                    }
                    catch
                    {
                        // Lỗi kết nối mạng -> Điểm đã lưu offline cục bộ, sẽ tự động được gửi lên ở đợt sau (hoặc coi như đồng bộ sau)
                    }
                });
            }

            return (newXp, newLevel, leveledUp, oldLevel);
        }

        /// <summary>
        /// Đồng bộ điểm lên máy chủ chạy ngầm hoặc đồng bộ trực tiếp
        /// </summary>
        public static async Task<(int NewXp, string NewLevel, bool LeveledUp, string OldLevel)> AddXpAsync(string ssoId, int amount)
        {
            if (string.IsNullOrEmpty(ssoId)) return (0, "Cấp 1: Độc giả Đồng", false, "Cấp 1: Độc giả Đồng");

            // 1. Cập nhật cục bộ trước (đảm bảo chạy offline mượt mà)
            var progress = GetProgress(ssoId);
            string oldLevel = progress.Level;
            int oldXp = progress.XP;
            int newXp = oldXp + amount;
            string newLevel = GetLevelName(newXp);
            bool leveledUp = oldLevel != newLevel;

            progress.XP = newXp;
            progress.Level = newLevel;
            _progressCache[ssoId] = progress;
            SaveData();

            // 2. Gửi đồng bộ lên máy chủ nếu online
            if (_apiService != null && !ApiService.IsOfflineMode)
            {
                try
                {
                    var payload = new { SsoUserId = ssoId, XpAmount = amount };
                    var res = await _apiService.PostAsync<object, ServerAddXpResponseDto>("/XpShop/add-xp", payload, silent: true);
                    if (res != null)
                    {
                        // Cập nhật lại giá trị chuẩn từ server
                        progress.XP = res.NewXp;
                        progress.Level = res.NewLevel;
                        _progressCache[ssoId] = progress;
                        SaveData();
                        return (res.NewXp, res.NewLevel, res.LeveledUp, res.OldLevel);
                    }
                }
                catch
                {
                    // Lỗi kết nối mạng -> Điểm đã lưu offline cục bộ, sẽ tự động được gửi lên ở đợt sau (hoặc coi như đồng bộ sau)
                }
            }

            return (newXp, newLevel, leveledUp, oldLevel);
        }

        public static int GetMaxXpForLevel(int xp)
        {
            if (xp < 200) return 200;
            if (xp < 500) return 500;
            if (xp < 1000) return 1000;
            return 2000;
        }

        private static string GetLevelName(int xp)
        {
            if (xp < 200) return "Cấp 1: Độc giả Đồng";
            if (xp < 500) return "Cấp 2: Độc giả Bạc";
            if (xp < 1000) return "Cấp 3: Độc giả Vàng";
            return "Cấp 4: Độc giả Kim Cương";
        }
    }

    public class ServerProgressDto
    {
        public string SsoUserId { get; set; } = string.Empty;
        public int XP { get; set; }
        public string Level { get; set; } = string.Empty;
    }

    public class ServerAddXpResponseDto
    {
        public string SsoUserId { get; set; } = string.Empty;
        public int NewXp { get; set; }
        public string NewLevel { get; set; } = string.Empty;
        public bool LeveledUp { get; set; }
        public string OldLevel { get; set; } = string.Empty;
    }
}
