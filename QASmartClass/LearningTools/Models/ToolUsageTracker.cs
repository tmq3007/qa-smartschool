using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace QASmartClass.LearningTools.Models
{
    /// <summary>
    /// Quản lý Favorites, Recent Tools, và Usage Stats.
    /// Dữ liệu được lưu vào file JSON cục bộ.
    /// </summary>
    public static class ToolUsageTracker
    {
        private static readonly string DataFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QASmartClass", "tool_usage.json");

        private static UsageData _data = new();
        private static bool _loaded;

        // ═══ PUBLIC API ═══

        /// <summary>Ghi nhận 1 lần sử dụng tool</summary>
        public static void RecordUsage(string toolId)
        {
            EnsureLoaded();
            if (!_data.UsageCount.ContainsKey(toolId))
                _data.UsageCount[toolId] = 0;
            _data.UsageCount[toolId]++;

            // Update recent (max 10)
            _data.RecentTools.Remove(toolId);
            _data.RecentTools.Insert(0, toolId);
            if (_data.RecentTools.Count > 10)
                _data.RecentTools.RemoveRange(10, _data.RecentTools.Count - 10);

            Save();
        }

        /// <summary>Toggle yêu thích</summary>
        public static bool ToggleFavorite(string toolId)
        {
            EnsureLoaded();
            if (_data.Favorites.Contains(toolId))
            {
                _data.Favorites.Remove(toolId);
                Save();
                return false;
            }
            _data.Favorites.Add(toolId);
            Save();
            return true;
        }

        /// <summary>Kiểm tra tool có là favorite không</summary>
        public static bool IsFavorite(string toolId)
        {
            EnsureLoaded();
            return _data.Favorites.Contains(toolId);
        }

        /// <summary>Lấy danh sách favorite tool IDs</summary>
        public static List<string> GetFavorites()
        {
            EnsureLoaded();
            return new List<string>(_data.Favorites);
        }

        /// <summary>Lấy danh sách recent tool IDs (max 10)</summary>
        public static List<string> GetRecent()
        {
            EnsureLoaded();
            return new List<string>(_data.RecentTools);
        }

        /// <summary>Lấy số lần sử dụng của tool</summary>
        public static int GetUsageCount(string toolId)
        {
            EnsureLoaded();
            return _data.UsageCount.TryGetValue(toolId, out int count) ? count : 0;
        }

        /// <summary>Lấy top N tools nhiều lần sử dụng nhất</summary>
        public static List<(string ToolId, int Count)> GetTopUsed(int n = 5)
        {
            EnsureLoaded();
            return _data.UsageCount
                .OrderByDescending(kv => kv.Value)
                .Take(n)
                .Select(kv => (kv.Key, kv.Value))
                .ToList();
        }

        /// <summary>Tổng số lần sử dụng tất cả tools</summary>
        public static int GetTotalUsage()
        {
            EnsureLoaded();
            return _data.UsageCount.Values.Sum();
        }

        // ═══ PERSISTENCE ═══

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                if (File.Exists(DataFilePath))
                {
                    var json = File.ReadAllText(DataFilePath);
                    _data = JsonSerializer.Deserialize<UsageData>(json) ?? new UsageData();
                }
            }
            catch
            {
                _data = new UsageData();
            }
        }

        private static void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(DataFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(DataFilePath, json);
            }
            catch { /* fail silently */ }
        }

        // ═══ DATA MODEL ═══

        private class UsageData
        {
            public List<string> Favorites { get; set; } = new();
            public List<string> RecentTools { get; set; } = new();
            public Dictionary<string, int> UsageCount { get; set; } = new();
        }
    }
}
