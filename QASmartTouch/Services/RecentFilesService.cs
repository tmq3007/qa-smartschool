using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Quản lý danh sách file đã mở gần đây (Recent Files)
    /// Lưu trữ tối đa 15 file, ghi vào JSON trong AppData
    /// </summary>
    public class RecentFilesService
    {
        private static RecentFilesService? _instance;
        private static readonly object _lock = new();
        private const int MAX_RECENT = 15;
        private const string FILE_NAME = "recent_files.json";

        private List<RecentFileEntry> _recentFiles = new();
        private readonly string _filePath;

        public static RecentFilesService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock) { _instance ??= new RecentFilesService(); }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Danh sách file gần đây (readonly)
        /// </summary>
        public IReadOnlyList<RecentFileEntry> RecentFiles => _recentFiles.AsReadOnly();

        /// <summary>
        /// Event khi danh sách thay đổi
        /// </summary>
        public event EventHandler? RecentFilesChanged;

        private RecentFilesService()
        {
            var appDataDir = QASmartClass.Services.AppPaths.RootDir;
            Directory.CreateDirectory(appDataDir);
            _filePath = Path.Combine(appDataDir, FILE_NAME);
            Load();
        }

        /// <summary>
        /// Thêm file vào danh sách gần đây
        /// </summary>
        public void AddFile(string filePath, string? displayName = null)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            // Xóa nếu đã tồn tại (để đưa lên đầu)
            _recentFiles.RemoveAll(f => f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

            // Thêm vào đầu danh sách
            _recentFiles.Insert(0, new RecentFileEntry
            {
                FilePath = filePath,
                DisplayName = displayName ?? Path.GetFileNameWithoutExtension(filePath),
                LastOpened = DateTime.Now,
                FileType = Path.GetExtension(filePath).ToLowerInvariant()
            });

            // Giới hạn số lượng
            if (_recentFiles.Count > MAX_RECENT)
                _recentFiles = _recentFiles.Take(MAX_RECENT).ToList();

            Save();
            RecentFilesChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Xóa 1 file khỏi danh sách
        /// </summary>
        public void RemoveFile(string filePath)
        {
            _recentFiles.RemoveAll(f => f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
            Save();
            RecentFilesChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Xóa toàn bộ danh sách
        /// </summary>
        public void Clear()
        {
            _recentFiles.Clear();
            Save();
            RecentFilesChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Xóa các file không còn tồn tại trên ổ đĩa
        /// </summary>
        public void CleanupMissing()
        {
            int removed = _recentFiles.RemoveAll(f => !File.Exists(f.FilePath));
            if (removed > 0)
            {
                Save();
                RecentFilesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    _recentFiles = JsonSerializer.Deserialize<List<RecentFileEntry>>(json) ?? new();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RecentFiles] Load error: {ex.Message}");
                _recentFiles = new();
            }
        }

        private void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(_recentFiles, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RecentFiles] Save error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Entry cho 1 file gần đây
    /// </summary>
    public class RecentFileEntry
    {
        [JsonPropertyName("filePath")]
        public string FilePath { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("lastOpened")]
        public DateTime LastOpened { get; set; }

        [JsonPropertyName("fileType")]
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// Icon emoji dựa trên loại file
        /// </summary>
        [JsonIgnore]
        public string Icon => FileType switch
        {
            ".qst" => "📋",     // QA SmartTouch board
            ".png" => "🖼️",
            ".jpg" or ".jpeg" => "📷",
            ".pdf" => "📄",
            ".pptx" => "📊",
            ".mp4" or ".webm" => "🎥",
            _ => "📁"
        };

        /// <summary>
        /// Thời gian hiển thị thân thiện
        /// </summary>
        [JsonIgnore]
        public string TimeAgo
        {
            get
            {
                var diff = DateTime.Now - LastOpened;
                if (diff.TotalMinutes < 1) return "Vừa xong";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
                if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} ngày trước";
                return LastOpened.ToString("dd/MM/yyyy");
            }
        }
    }
}
