using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace QASmartClass.Services
{
    /// <summary>
    /// Offline Cache — lưu dữ liệu cơ bản khi online, đọc lại khi DB fail
    /// </summary>
    public static class OfflineCacheService
    {
        private static string CacheFile => Path.Combine(AppPaths.RootDir, "cache", "offline_data.json");

        /// <summary>Cache dữ liệu ClassRoster + Students t? DB</summary>
        public static void SaveCache(AppDbContext db)
        {
            try
            {
                var dir = Path.GetDirectoryName(CacheFile)!;
                Directory.CreateDirectory(dir);

                var data = new OfflineCacheData
                {
                    CachedAt = DateTime.Now,
                    Rosters = db.ClassRosters.Select(r => new CachedRoster
                    {
                        Id = r.Id, ClassName = r.ClassName, SchoolYear = r.SchoolYear,
                        StudentCount = r.StudentCount
                    }).ToList(),
                    Students = db.Students.Select(s => new CachedStudent
                    {
                        Id = s.Id, FullName = s.FullName, ClassName = s.ClassName,
                        StudentCode = s.StudentCode, Status = s.Status
                    }).ToList()
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(CacheFile, json);
                Log.Information("[OfflineCache] Saved {R} rosters, {S} students",
                    data.Rosters.Count, data.Students.Count);
            }
            catch (Exception ex) { Log.Error(ex, "OfflineCache: Save failed"); }
        }

        /// <summary>Đ?c cache khi DB offline</summary>
        public static OfflineCacheData? LoadCache()
        {
            try
            {
                if (!File.Exists(CacheFile)) return null;
                var json = File.ReadAllText(CacheFile);
                var data = JsonSerializer.Deserialize<OfflineCacheData>(json);
                Log.Information("[OfflineCache] Loaded cache from {Time}", data?.CachedAt);
                return data;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OfflineCache: Load failed");
                return null;
            }
        }

        /// <summary>Ki?m tra cache có t?n tại và không quá cu (24h)</summary>
        public static bool HasValidCache()
        {
            if (!File.Exists(CacheFile)) return false;
            var lastWrite = File.GetLastWriteTime(CacheFile);
            return (DateTime.Now - lastWrite).TotalHours < 24;
        }
    }

    // DTO classes for serialization (avoid EF proxy issues)
    public class OfflineCacheData
    {
        public DateTime CachedAt { get; set; }
        public List<CachedRoster> Rosters { get; set; } = new();
        public List<CachedStudent> Students { get; set; } = new();
    }

    public class CachedRoster
    {
        public int Id { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string SchoolYear { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }

    public class CachedStudent
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

