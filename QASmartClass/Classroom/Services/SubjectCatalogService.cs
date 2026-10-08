using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    /// <summary>
    /// Dịch vụ quản lý Danh mục Môn học toàn trường.
    /// Lưu trữ bền vững trong Database SQLite (Subjects table) và nạp 1 lần duy nhất lên In-Memory Cache (RAM)
    /// để tối ưu hóa hiệu năng vẽ lưới thời khóa biểu và tra cứu màu sắc (0ms latency).
    /// </summary>
    public class SubjectCatalogService
    {
        private static SubjectCatalogService? _instance;
        public static SubjectCatalogService Instance => _instance ??= new SubjectCatalogService();

        private readonly List<Subject> _cachedSubjects = new();
        private bool _isLoaded = false;
        private readonly object _lock = new();

        /// <summary>
        /// Sự kiện phát ra khi danh mục môn học có thay đổi (Thêm, Sửa, Xóa)
        /// để TimetablePage và các màn hình khác cập nhật lại giao diện ngay lập tức.
        /// </summary>
        public event EventHandler? SubjectsChanged;

        /// <summary>
        /// Nạp toàn bộ danh mục môn học từ SQLite Database lên bộ nhớ đệm (In-Memory Cache).
        /// Chỉ chạy 1 lần duy nhất trong suốt vòng đời ứng dụng trừ khi được yêu cầu nạp lại.
        /// </summary>
        public void EnsureLoaded(AppDbContext? customDb = null, bool forceReload = false)
        {
            if (_isLoaded && !forceReload) return;

            lock (_lock)
            {
                if (_isLoaded && !forceReload) return;

                try
                {
                    using var localDb = customDb == null ? new AppDbContext() : null;
                    var db = customDb ?? localDb!;

                    // Đảm bảo bảng Subjects đã tồn tại và có dữ liệu khởi tạo
                    QASmartClass.Services.DbMigrator.EnsureSubjectsTable(db);

                    var list = db.Subjects
                        .OrderBy(s => s.DisplayOrder)
                        .ThenBy(s => s.Name)
                        .ToList();

                    _cachedSubjects.Clear();
                    _cachedSubjects.AddRange(list);
                    _isLoaded = true;

                    Log.Information("[SubjectCatalogService] Đã nạp thành công {Count} môn học từ Database vào Cache.", _cachedSubjects.Count);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[SubjectCatalogService] Lỗi khi nạp danh mục môn học từ Database");

                    // Fallback bộ nhớ tạm thời nếu DB gặp lỗi đột xuất
                    if (_cachedSubjects.Count == 0)
                    {
                        _cachedSubjects.AddRange(GetFallbackDefaults());
                        _isLoaded = true;
                    }
                }
            }
        }

        /// <summary>
        /// Lấy toàn bộ danh sách môn học từ cache bộ nhớ.
        /// </summary>
        public IReadOnlyList<Subject> GetAllSubjects()
        {
            EnsureLoaded();
            lock (_lock)
            {
                return _cachedSubjects.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name).ToList();
            }
        }

        /// <summary>
        /// Tìm môn học theo Tên đầy đủ hoặc Tên viết tắt (không phân biệt hoa thường).
        /// </summary>
        public Subject? GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            EnsureLoaded();

            string clean = name.Trim();
            lock (_lock)
            {
                return _cachedSubjects.FirstOrDefault(s =>
                    string.Equals(s.Name, clean, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s.ShortName, clean, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Lấy mã màu HEX (#RRGGBB) của môn học bất kỳ từ cache (0ms).
        /// Nếu không tìm thấy, trả về màu mặc định #546E7A.
        /// </summary>
        public string GetColorForSubject(string subjectName)
        {
            var subj = GetByName(subjectName);
            if (subj != null && !string.IsNullOrWhiteSpace(subj.ColorHex))
            {
                return subj.ColorHex;
            }
            return "#546E7A"; // Fallback slate gray
        }

        /// <summary>
        /// Thêm môn học mới: Lưu xuống DB và cập nhật Cache bộ nhớ đồng thời.
        /// </summary>
        public bool AddSubject(Subject subject, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(subject.Name))
            {
                error = "Tên môn học không được để trống.";
                return false;
            }

            subject.Name = subject.Name.Trim();
            subject.ShortName = string.IsNullOrWhiteSpace(subject.ShortName) ? subject.Name : subject.ShortName.Trim();

            EnsureLoaded();

            lock (_lock)
            {
                if (_cachedSubjects.Any(s => string.Equals(s.Name, subject.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    error = $"Môn học '{subject.Name}' đã tồn tại trong danh mục.";
                    return false;
                }

                try
                {
                    using var db = new AppDbContext();
                    subject.DisplayOrder = _cachedSubjects.Count > 0 ? _cachedSubjects.Max(s => s.DisplayOrder) + 1 : 1;
                    
                    db.Subjects.Add(subject);
                    db.SaveChanges();

                    // Cập nhật Cache bộ nhớ ngay lập tức
                    _cachedSubjects.Add(subject);

                    Log.Information("[SubjectCatalogService] Đã thêm môn học mới vào DB: {Name} (Id: {Id})", subject.Name, subject.Id);
                    SubjectsChanged?.Invoke(this, EventArgs.Empty);
                    return true;
                }
                catch (Exception ex)
                {
                    error = $"Lỗi khi lưu vào CSDL: {ex.Message}";
                    Log.Error(ex, "[SubjectCatalogService] AddSubject error");
                    return false;
                }
            }
        }

        /// <summary>
        /// Cập nhật môn học: Lưu xuống DB và cập nhật Cache bộ nhớ.
        /// </summary>
        public bool UpdateSubject(Subject subject, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(subject.Name))
            {
                error = "Tên môn học không được để trống.";
                return false;
            }

            EnsureLoaded();

            lock (_lock)
            {
                if (_cachedSubjects.Any(s => s.Id != subject.Id && string.Equals(s.Name, subject.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    error = $"Tên môn học '{subject.Name}' bị trùng với môn khác.";
                    return false;
                }

                try
                {
                    using var db = new AppDbContext();
                    var existing = db.Subjects.FirstOrDefault(s => s.Id == subject.Id);
                    if (existing == null)
                    {
                        error = "Không tìm thấy môn học cần sửa trong CSDL.";
                        return false;
                    }

                    existing.Name = subject.Name.Trim();
                    existing.ShortName = string.IsNullOrWhiteSpace(subject.ShortName) ? subject.Name : subject.ShortName.Trim();
                    existing.ColorHex = subject.ColorHex;
                    existing.Icon = subject.Icon;
                    existing.DefaultRoom = subject.DefaultRoom;
                    existing.WeeklyPeriods = subject.WeeklyPeriods;
                    existing.DisplayOrder = subject.DisplayOrder;

                    db.SaveChanges();

                    // Cập nhật Cache bộ nhớ
                    int cacheIdx = _cachedSubjects.FindIndex(s => s.Id == subject.Id);
                    if (cacheIdx >= 0)
                    {
                        _cachedSubjects[cacheIdx] = existing;
                    }

                    Log.Information("[SubjectCatalogService] Đã cập nhật môn học trong DB: {Name} (Id: {Id})", existing.Name, existing.Id);
                    SubjectsChanged?.Invoke(this, EventArgs.Empty);
                    return true;
                }
                catch (Exception ex)
                {
                    error = $"Lỗi cập nhật CSDL: {ex.Message}";
                    Log.Error(ex, "[SubjectCatalogService] UpdateSubject error");
                    return false;
                }
            }
        }

        /// <summary>
        /// Xóa môn học khỏi CSDL và Cache bộ nhớ.
        /// </summary>
        public bool DeleteSubject(int id, out string error)
        {
            error = string.Empty;
            EnsureLoaded();

            lock (_lock)
            {
                try
                {
                    using var db = new AppDbContext();
                    var item = db.Subjects.FirstOrDefault(s => s.Id == id);
                    if (item == null)
                    {
                        error = "Không tìm thấy môn học cần xóa.";
                        return false;
                    }

                    db.Subjects.Remove(item);
                    db.SaveChanges();

                    // Cập nhật Cache bộ nhớ
                    _cachedSubjects.RemoveAll(s => s.Id == id);

                    Log.Information("[SubjectCatalogService] Đã xóa môn học khỏi DB: {Name} (Id: {Id})", item.Name, id);
                    SubjectsChanged?.Invoke(this, EventArgs.Empty);
                    return true;
                }
                catch (Exception ex)
                {
                    error = $"Lỗi khi xóa khỏi CSDL: {ex.Message}";
                    Log.Error(ex, "[SubjectCatalogService] DeleteSubject error");
                    return false;
                }
            }
        }

        /// <summary>
        /// Khôi phục danh mục 20 môn chuẩn GDPT 2018 vào CSDL và nạp lại Cache.
        /// </summary>
        public void ResetToDefaults()
        {
            lock (_lock)
            {
                try
                {
                    using var db = new AppDbContext();
                    db.Subjects.RemoveRange(db.Subjects);
                    db.SaveChanges();

                    var defaults = GetFallbackDefaults();
                    db.Subjects.AddRange(defaults);
                    db.SaveChanges();

                    _cachedSubjects.Clear();
                    _cachedSubjects.AddRange(defaults);

                    Log.Information("[SubjectCatalogService] Đã khôi phục 20 môn chuẩn GDPT 2018 vào DB.");
                    SubjectsChanged?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[SubjectCatalogService] ResetToDefaults error");
                }
            }
        }

        private static List<Subject> GetFallbackDefaults()
        {
            return new List<Subject>
            {
                new Subject { Id = 1, Name = "Toán học", ShortName = "Toán", ColorHex = "#1976D2", Icon = "📐", DefaultRoom = "P.Học", WeeklyPeriods = 4, IsSystem = true, DisplayOrder = 1 },
                new Subject { Id = 2, Name = "Ngữ văn", ShortName = "Văn", ColorHex = "#7B1FA2", Icon = "📖", DefaultRoom = "P.Học", WeeklyPeriods = 4, IsSystem = true, DisplayOrder = 2 },
                new Subject { Id = 3, Name = "Tiếng Anh", ShortName = "Anh", ColorHex = "#00838F", Icon = "🌐", DefaultRoom = "P.Ngoại ngữ", WeeklyPeriods = 3, IsSystem = true, DisplayOrder = 3 },
                new Subject { Id = 4, Name = "Vật lý", ShortName = "Lý", ColorHex = "#E64A19", Icon = "⚡", DefaultRoom = "P.Thực hành Lý", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 4 },
                new Subject { Id = 5, Name = "Hóa học", ShortName = "Hóa", ColorHex = "#2E7D32", Icon = "🧪", DefaultRoom = "P.Thực hành Hóa", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 5 },
                new Subject { Id = 6, Name = "Sinh học", ShortName = "Sinh", ColorHex = "#AD1457", Icon = "🧬", DefaultRoom = "P.Thực hành Sinh", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 6 },
                new Subject { Id = 7, Name = "Khoa học tự nhiên", ShortName = "KHTN", ColorHex = "#00897B", Icon = "🔬", DefaultRoom = "P.Thực hành KHTN", WeeklyPeriods = 4, IsSystem = true, DisplayOrder = 7 },
                new Subject { Id = 8, Name = "Lịch sử", ShortName = "Sử", ColorHex = "#5D4037", Icon = "🏛️", DefaultRoom = "P.Học", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 8 },
                new Subject { Id = 9, Name = "Địa lý", ShortName = "Địa", ColorHex = "#00695C", Icon = "🌍", DefaultRoom = "P.Học", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 9 },
                new Subject { Id = 10, Name = "Lịch sử & Địa lý", ShortName = "Sử-Địa", ColorHex = "#4E342E", Icon = "🗺️", DefaultRoom = "P.Học", WeeklyPeriods = 3, IsSystem = true, DisplayOrder = 10 },
                new Subject { Id = 11, Name = "GD Kinh tế & Pháp luật", ShortName = "GDKT&PL", ColorHex = "#F9A825", Icon = "⚖️", DefaultRoom = "P.Học", WeeklyPeriods = 1, IsSystem = true, DisplayOrder = 11 },
                new Subject { Id = 12, Name = "Giáo dục công dân", ShortName = "GDCD", ColorHex = "#F57F17", Icon = "🤝", DefaultRoom = "P.Học", WeeklyPeriods = 1, IsSystem = true, DisplayOrder = 12 },
                new Subject { Id = 13, Name = "Tin học", ShortName = "Tin", ColorHex = "#0277BD", Icon = "💻", DefaultRoom = "P.Tin học", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 13 },
                new Subject { Id = 14, Name = "Công nghệ", ShortName = "CN", ColorHex = "#558B2F", Icon = "⚙️", DefaultRoom = "P.Công nghệ", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 14 },
                new Subject { Id = 15, Name = "Giáo dục thể chất", ShortName = "GDTC", ColorHex = "#EF6C00", Icon = "⚽", DefaultRoom = "Sân thể dục", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 15 },
                new Subject { Id = 16, Name = "Âm nhạc", ShortName = "Nhạc", ColorHex = "#8E24AA", Icon = "🎵", DefaultRoom = "P.Âm nhạc", WeeklyPeriods = 1, IsSystem = true, DisplayOrder = 16 },
                new Subject { Id = 17, Name = "Mỹ thuật", ShortName = "MT", ColorHex = "#D81B60", Icon = "🎨", DefaultRoom = "P.Mỹ thuật", WeeklyPeriods = 1, IsSystem = true, DisplayOrder = 17 },
                new Subject { Id = 18, Name = "HĐ Trải nghiệm & Hướng nghiệp", ShortName = "HĐTN", ColorHex = "#FB8C00", Icon = "🌟", DefaultRoom = "Hội trường", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 18 },
                new Subject { Id = 19, Name = "STEM & Robotics", ShortName = "STEM", ColorHex = "#1565C0", Icon = "🤖", DefaultRoom = "P.STEM", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 19 },
                new Subject { Id = 20, Name = "GD Quốc phòng & An ninh", ShortName = "GDQP", ColorHex = "#33691E", Icon = "🎖️", DefaultRoom = "Sân trường", WeeklyPeriods = 1, IsSystem = true, DisplayOrder = 20 }
            };
        }
    }
}
