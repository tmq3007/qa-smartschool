using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    /// <summary>
    /// Service quản lý danh sách lớp học (Class Roster).
    /// Hỗ trợ phòng STEM dùng chung: import/export CSV, chuyển lớp nhanh.
    /// Tích hợp L1 In-Memory Cache chuẩn công nghiệp giúp truy cập 0ms.
    /// </summary>
    public class ClassRosterService
    {
        private readonly AppDbContext _db;
        private ClassRoster? _activeRoster;

        // In-Memory L1 Cache
        private List<ClassRoster>? _cachedRosters = null;
        private List<Student>? _cachedActiveStudents = null;
        private int? _cachedTotalStudentsCount = null;
        private readonly object _cacheLock = new();

        /// <summary>Event khi lớp đang active thay đổi</summary>
        public event EventHandler<ClassRoster?>? ActiveRosterChanged;

        /// <summary>Roster đang được chọn (lớp hiện tại)</summary>
        public ClassRoster? ActiveRoster => _activeRoster;

        public ClassRosterService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>Xóa bộ đệm RAM khi có thay đổi dữ liệu</summary>
        public void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _cachedRosters = null;
                _cachedActiveStudents = null;
                _cachedTotalStudentsCount = null;
            }
        }

        /// <summary>Lấy tổng số học sinh toàn trường (có đệm RAM 0ms)</summary>
        public int GetTotalStudentsCount()
        {
            lock (_cacheLock)
            {
                if (_cachedTotalStudentsCount.HasValue)
                    return _cachedTotalStudentsCount.Value;
            }

            try
            {
                int count = _db.Students.AsNoTracking().Count();
                lock (_cacheLock)
                {
                    _cachedTotalStudentsCount = count;
                }
                return count;
            }
            catch
            {
                return 0;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  CRUD ROSTER
        // ═══════════════════════════════════════════════════════════

        /// <summary>Lấy tất cả roster (đang active, có đệm RAM 0ms)</summary>
        public List<ClassRoster> GetAllRosters(bool activeOnly = true)
        {
            lock (_cacheLock)
            {
                if (_cachedRosters != null)
                {
                    return activeOnly
                        ? _cachedRosters.Where(r => r.IsActive).ToList()
                        : _cachedRosters.ToList();
                }
            }

            try
            {
                var query = _db.ClassRosters.AsNoTracking().AsQueryable();
                var list = query.OrderByDescending(r => r.LastUsedAt).ToList();
                lock (_cacheLock)
                {
                    _cachedRosters = list;
                }
                return activeOnly ? list.Where(r => r.IsActive).ToList() : list;
            }
            catch (Exception ex)
            {
                Log.Warning("GetAllRosters error: {Err}", ex.Message);
                return new List<ClassRoster>();
            }
        }

        /// <summary>Lấy danh sách roster được phân công cho giáo viên phụ trách (có fallback an toàn)</summary>
        public List<ClassRoster> GetRostersForTeacher(string? teacherName, bool activeOnly = true)
        {
            var all = GetAllRosters(activeOnly);
            if (string.IsNullOrWhiteSpace(teacherName))
                return all;

            var trimmedName = teacherName.Trim();
            var myRosters = all.Where(r => 
                !string.IsNullOrWhiteSpace(r.TeacherName) && 
                (string.Equals(r.TeacherName.Trim(), trimmedName, StringComparison.OrdinalIgnoreCase) ||
                 r.TeacherName.Contains(trimmedName, StringComparison.OrdinalIgnoreCase) ||
                 trimmedName.Contains(r.TeacherName.Trim(), StringComparison.OrdinalIgnoreCase))
            ).ToList();

            // Nếu giáo viên có lớp cụ thể thì trả về, nếu chưa được gán lớp thì fallback trả về toàn bộ
            return myRosters.Any() ? myRosters : all;
        }

        /// <summary>Lấy roster theo Id (include students)</summary>
        public ClassRoster? GetRosterById(int rosterId)
        {
            try
            {
                var roster = _db.ClassRosters.Find(rosterId);
                if (roster != null)
                {
                    roster.Students = _db.ClassRosterStudents
                        .Where(rs => rs.RosterId == rosterId)
                        .OrderBy(rs => rs.SeatNumber)
                        .ToList();
                }
                return roster;
            }
            catch (Exception ex)
            {
                Log.Warning("GetRosterById error: {Err}", ex.Message);
                return null;
            }
        }

        /// <summary>Tạo roster mới</summary>
        public ClassRoster CreateRoster(string className, string gradeLevel, string subject,
            string teacherName, string schoolYear = "2025-2026", string semester = "HK2")
        {
            var roster = new ClassRoster
            {
                ClassName = className.Trim(),
                GradeLevel = gradeLevel.Trim(),
                Subject = subject.Trim(),
                TeacherName = teacherName.Trim(),
                SchoolYear = schoolYear,
                Semester = semester,
                CreatedAt = DateTime.Now,
                LastUsedAt = DateTime.Now
            };

            _db.ClassRosters.Add(roster);
            _db.SaveChanges();
            InvalidateCache();
            Log.Information("Created roster: {Name} ({Subject}, GV {Teacher})",
                className, subject, teacherName);
            return roster;
        }

        /// <summary>Cập nhật thông tin roster</summary>
        public bool UpdateRoster(ClassRoster roster)
        {
            try
            {
                var existing = _db.ClassRosters.Find(roster.Id);
                if (existing == null) return false;

                existing.ClassName = roster.ClassName;
                existing.GradeLevel = roster.GradeLevel;
                existing.SchoolYear = roster.SchoolYear;
                existing.Semester = roster.Semester;
                existing.TeacherName = roster.TeacherName;
                existing.Subject = roster.Subject;
                existing.Notes = roster.Notes;
                existing.IsActive = roster.IsActive;

                _db.SaveChanges();
                InvalidateCache();
                Log.Information("Updated roster #{Id}: {Name}", roster.Id, roster.ClassName);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("UpdateRoster error: {Err}", ex.Message);
                return false;
            }
        }

        /// <summary>Xóa roster (và tất cả liên kết HS)</summary>
        public bool DeleteRoster(int rosterId)
        {
            try
            {
                var roster = _db.ClassRosters.Find(rosterId);
                if (roster == null) return false;

                // Xóa liên kết HS
                var links = _db.ClassRosterStudents.Where(rs => rs.RosterId == rosterId).ToList();
                _db.ClassRosterStudents.RemoveRange(links);

                _db.ClassRosters.Remove(roster);
                _db.SaveChanges();
                InvalidateCache();

                // Clear active nếu đang chọn roster này
                if (_activeRoster?.Id == rosterId)
                    SetActiveRoster(null);

                Log.Information("Deleted roster #{Id}: {Name} ({Count} links removed)",
                    rosterId, roster.ClassName, links.Count);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("DeleteRoster error: {Err}", ex.Message);
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  ACTIVE ROSTER (Chuyển lớp nhanh)
        // ═══════════════════════════════════════════════════════════

        /// <summary>Chọn lớp đang active</summary>
        public void SetActiveRoster(ClassRoster? roster)
        {
            lock (_cacheLock)
            {
                _cachedActiveStudents = null;
            }
            _activeRoster = roster;

            if (roster != null)
            {
                // Cập nhật LastUsedAt ngầm trên background task để không làm đơ UI thread
                _ = Task.Run(() =>
                {
                    try
                    {
                        using var bgDb = new AppDbContext();
                        var dbRoster = bgDb.ClassRosters.Find(roster.Id);
                        if (dbRoster != null)
                        {
                            dbRoster.LastUsedAt = DateTime.Now;
                            bgDb.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Background update LastUsedAt error: {Err}", ex.Message);
                    }
                });
                Log.Information("Active roster → {Name} ({Subject})", roster.ClassName, roster.Subject);
            }
            else
            {
                Log.Information("Active roster → (none)");
            }

            ActiveRosterChanged?.Invoke(this, roster);
        }

        /// <summary>Lấy danh sách HS của roster đang active (sắp xếp theo tên VN, có đệm RAM 0ms)</summary>
        public List<Student> GetActiveStudents()
        {
            if (_activeRoster == null) return new List<Student>();

            lock (_cacheLock)
            {
                if (_cachedActiveStudents != null)
                {
                    return _cachedActiveStudents.ToList();
                }
            }

            try
            {
                var activeRosterId = _activeRoster.Id;
                var query = from rs in _db.ClassRosterStudents.AsNoTracking()
                            join s in _db.Students.AsNoTracking() on rs.StudentId equals s.Id
                            where rs.RosterId == activeRosterId
                            select new { Student = s, rs.SeatNumber };

                var results = query.ToList();
                var students = new List<Student>();

                foreach (var r in results)
                {
                    r.Student.SeatNumber = r.SeatNumber;
                    students.Add(r.Student);
                }

                // Sắp xếp theo chuẩn Việt Nam: Tên → Họ → Đệm
                var sorted = VietnameseNameHelper.SortByVietnameseName(students, s => s.FullName);
                lock (_cacheLock)
                {
                    _cachedActiveStudents = sorted;
                }
                return sorted.ToList();
            }
            catch (Exception ex)
            {
                Log.Warning("GetActiveStudents error: {Err}", ex.Message);
                return new List<Student> { new Student { FullName = "ERROR: " + ex.Message, StudentCode = "ERR" } };
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  QUẢN LÝ HS TRONG ROSTER
        // ═══════════════════════════════════════════════════════════

        /// <summary>Thêm HS vào roster</summary>
        public bool AddStudentToRoster(int rosterId, int studentId, int seatNumber = 0, string notes = "")
        {
            try
            {
                // Kiểm tra trùng
                var exists = _db.ClassRosterStudents
                    .Any(rs => rs.RosterId == rosterId && rs.StudentId == studentId);
                if (exists) return false;

                // Kiểm tra sức chứa phòng học (MaxStudents) theo chuẩn QA SmartClass v4.1
                var roster = _db.ClassRosters.Find(rosterId);
                if (roster != null)
                {
                    var classroom = _db.Classrooms.FirstOrDefault(c => c.Name == roster.ClassName);
                    if (classroom != null)
                    {
                        int currentCount = _db.ClassRosterStudents.Count(rs => rs.RosterId == rosterId);
                        if (currentCount >= classroom.MaxStudents)
                        {
                            throw new InvalidOperationException($"Không thể thêm học sinh. Sĩ số lớp hiện tại ({currentCount}) đã đạt giới hạn tối đa của phòng học ({classroom.MaxStudents}).");
                        }
                    }
                }

                _db.ClassRosterStudents.Add(new ClassRosterStudent
                {
                    RosterId = rosterId,
                    StudentId = studentId,
                    SeatNumber = seatNumber,
                    Notes = notes
                });

                // Update student count
                if (roster != null)
                    roster.StudentCount = _db.ClassRosterStudents.Count(rs => rs.RosterId == rosterId) + 1;

                _db.SaveChanges();
                InvalidateCache();
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("AddStudentToRoster error: {Err}", ex.Message);
                if (ex is InvalidOperationException) throw;
                return false;
            }
        }

        /// <summary>Xóa HS khỏi roster</summary>
        public bool RemoveStudentFromRoster(int rosterId, int studentId)
        {
            try
            {
                var link = _db.ClassRosterStudents
                    .FirstOrDefault(rs => rs.RosterId == rosterId && rs.StudentId == studentId);
                if (link == null) return false;

                _db.ClassRosterStudents.Remove(link);

                // Update student count
                var roster = _db.ClassRosters.Find(rosterId);
                if (roster != null)
                    roster.StudentCount = _db.ClassRosterStudents.Count(rs => rs.RosterId == rosterId) - 1;

                _db.SaveChanges();
                InvalidateCache();
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("RemoveStudentFromRoster error: {Err}", ex.Message);
                return false;
            }
        }

        /// <summary>Lấy danh sách HS của 1 roster (kèm thông tin Student)</summary>
        public List<(Student Student, ClassRosterStudent Link)> GetRosterStudents(int rosterId)
        {
            try
            {
                var links = _db.ClassRosterStudents
                    .Where(rs => rs.RosterId == rosterId)
                    .OrderBy(rs => rs.SeatNumber)
                    .ToList();

                var studentIds = links.Select(l => l.StudentId).ToList();
                var students = _db.Students.Where(s => studentIds.Contains(s.Id)).ToDictionary(s => s.Id);

                return links
                    .Where(l => students.ContainsKey(l.StudentId))
                    .Select(l => (students[l.StudentId], l))
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Warning("GetRosterStudents error: {Err}", ex.Message);
                return new List<(Student, ClassRosterStudent)>();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  IMPORT / EXPORT CSV
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Phân tách dòng CSV an toàn chống lệch cột khi có dấu phẩy nằm trong dấu ngoặc kép.
        /// Tuân thủ quy chuẩn RFC 4180.
        /// </summary>
        public static string[] ParseCsvLine(string line, char delimiter = ',')
        {
            if (string.IsNullOrEmpty(line)) return Array.Empty<string>();
            var result = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"'); // Dấu ngoặc kép được escape
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes; // Đảo trạng thái chuỗi
                    }
                }
                else if (c == delimiter && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString().Trim());
            return result.ToArray();
        }

        /// <summary>
        /// Chống lỗ hổng Excel/CSV Injection bằng cách thêm dấu nháy đơn trước các ký tự công thức.
        /// </summary>
        public static string SanitizeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            if (field.StartsWith("=") || field.StartsWith("+") || field.StartsWith("-") || field.StartsWith("@"))
            {
                return "'" + field;
            }
            return field;
        }

        /// <summary>
        /// Import HS từ CSV vào roster.
        /// Format: STT,Họ tên,Mã HS,Ghi chú
        /// </summary>
        public (int imported, int skipped, string message) ImportStudentsFromCsv(
            int rosterId, string csvPath)
        {
            int imported = 0, skipped = 0;

            try
            {
                var lines = File.ReadAllLines(csvPath)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToArray();

                if (lines.Length < 2)
                    return (0, 0, "File CSV không có dữ liệu (cần ít nhất 1 dòng header + 1 dòng dữ liệu).");

                // Lấy mã học sinh tự sinh (HSxxx) lớn nhất hiện tại ngoài vòng lặp (Tránh lỗi SQL N+1)
                int maxStudentNum = 0;
                try
                {
                    var existingCodes = _db.Students
                        .Where(s => s.StudentCode.StartsWith("HS"))
                        .Select(s => s.StudentCode)
                        .ToList();
                    foreach (var c in existingCodes)
                    {
                        if (int.TryParse(c.Substring(2), out int num))
                        {
                            if (num > maxStudentNum) maxStudentNum = num;
                        }
                    }
                }
                catch { }

                // Sử dụng database transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = _db.Database.BeginTransaction())
                {
                    foreach (var line in lines.Skip(1)) // Bỏ qua tiêu đề
                    {
                        var cols = ParseCsvLine(line);
                        if (cols.Length < 2 || string.IsNullOrWhiteSpace(cols[1])) { skipped++; continue; }

                        int stt = 0;
                        if (cols.Length > 0) int.TryParse(cols[0], out stt);
                        string fullName = cols[1];
                        string studentCode = cols.Length > 2 ? cols[2] : "";
                        string notes = cols.Length > 3 ? cols[3] : "";

                        if (string.IsNullOrWhiteSpace(fullName)) { skipped++; continue; }

                        // Tìm hoặc tạo Student
                        var student = !string.IsNullOrWhiteSpace(studentCode)
                            ? _db.Students.FirstOrDefault(s => s.StudentCode == studentCode)
                            : _db.Students.FirstOrDefault(s => s.FullName == fullName);

                        if (student == null)
                        {
                            string generatedCode = string.IsNullOrWhiteSpace(studentCode)
                                ? $"HS{(++maxStudentNum):D3}"
                                : studentCode;
                            // Sinh mật khẩu mặc định an toàn: Hs@ + 4 ký tự cuối mã HS
                            string defaultPwd = $"Hs@{generatedCode.Substring(Math.Max(0, generatedCode.Length - 4))}";
                            
                            student = new Student
                            {
                                FullName = fullName,
                                StudentCode = generatedCode,
                                PasswordHash = QASmartTouch.Services.AuthenticationService.HashPasswordHMACSHA512(defaultPwd),
                                LastSeen = DateTime.Now
                            };
                            _db.Students.Add(student);
                            _db.SaveChanges(); // Lưu để phát sinh Id làm khóa ngoại
                        }

                        // Thêm vào roster (skip nếu đã tồn tại)
                        var exists = _db.ClassRosterStudents
                            .Any(rs => rs.RosterId == rosterId && rs.StudentId == student.Id);
                        if (!exists)
                        {
                            _db.ClassRosterStudents.Add(new ClassRosterStudent
                            {
                                RosterId = rosterId,
                                StudentId = student.Id,
                                SeatNumber = stt > 0 ? stt : (imported + 1),
                                Notes = notes
                            });
                            imported++;
                        }
                        else
                        {
                            skipped++;
                        }
                    }

                    // Cập nhật sĩ số lớp và kiểm tra sức chứa phòng học
                    var roster = _db.ClassRosters.Find(rosterId);
                    if (roster != null)
                    {
                        var classroom = _db.Classrooms.FirstOrDefault(c => c.Name == roster.ClassName);
                        if (classroom != null)
                        {
                            int totalCount = _db.ClassRosterStudents.Count(rs => rs.RosterId == rosterId) + imported;
                            if (totalCount > classroom.MaxStudents)
                            {
                                throw new InvalidOperationException($"Không thể nhập dữ liệu. Tổng sĩ số sau khi nhập ({totalCount}) vượt quá giới hạn tối đa của phòng học ({classroom.MaxStudents}).");
                            }
                        }
                        roster.StudentCount = _db.ClassRosterStudents.Count(rs => rs.RosterId == rosterId) + imported;
                    }

                    _db.SaveChanges();
                    transaction.Commit();
                    InvalidateCache();
                }

                Log.Information("Import CSV → Roster #{Id}: {Imported} imported, {Skipped} skipped",
                    rosterId, imported, skipped);

                return (imported, skipped, $"Đã import {imported} HS, bỏ qua {skipped} dòng.");
            }
            catch (Exception ex)
            {
                Log.Warning("ImportStudentsFromCsv error: {Err}", ex.Message);
                return (imported, skipped, $"Lỗi import: {ex.Message}");
            }
        }

        /// <summary>Xuất danh sách HS của roster ra CSV</summary>
        public bool ExportRosterToCsv(int rosterId, string savePath)
        {
            try
            {
                var roster = GetRosterById(rosterId);
                if (roster == null) return false;

                var rosterStudents = GetRosterStudents(rosterId);

                var lines = new List<string>
                {
                    "STT,Họ tên,Mã HS,Ghi chú"
                };

                foreach (var (student, link) in rosterStudents)
                {
                    var cleanName = SanitizeCsvField(student.FullName);
                    var cleanCode = SanitizeCsvField(student.StudentCode);
                    var cleanNotes = SanitizeCsvField(link.Notes);
                    lines.Add($"{link.SeatNumber},\"{cleanName}\",\"{cleanCode}\",\"{cleanNotes}\"");
                }

                // Ghi tệp kèm UTF-8 BOM rõ ràng để mở bằng Excel hiển thị đúng tiếng Việt
                File.WriteAllLines(savePath, lines, new System.Text.UTF8Encoding(true));
                Log.Information("Exported roster #{Id} to {Path} ({Count} students)",
                    rosterId, savePath, rosterStudents.Count);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("ExportRosterToCsv error: {Err}", ex.Message);
                return false;
            }
        }

        /// <summary>Nhân đôi roster (copy DS HS sang roster mới)</summary>
        public ClassRoster? DuplicateRoster(int sourceRosterId, string newClassName, string newSubject = "")
        {
            try
            {
                var source = GetRosterById(sourceRosterId);
                if (source == null) return null;

                var newRoster = new ClassRoster
                {
                    ClassName = newClassName,
                    GradeLevel = source.GradeLevel,
                    SchoolYear = source.SchoolYear,
                    Semester = source.Semester,
                    TeacherName = source.TeacherName,
                    Subject = string.IsNullOrWhiteSpace(newSubject) ? source.Subject : newSubject,
                    CreatedAt = DateTime.Now,
                    LastUsedAt = DateTime.Now
                };

                _db.ClassRosters.Add(newRoster);
                _db.SaveChanges();

                // Copy student links
                var sourceLinks = _db.ClassRosterStudents
                    .Where(rs => rs.RosterId == sourceRosterId)
                    .ToList();

                foreach (var link in sourceLinks)
                {
                    _db.ClassRosterStudents.Add(new ClassRosterStudent
                    {
                        RosterId = newRoster.Id,
                        StudentId = link.StudentId,
                        SeatNumber = link.SeatNumber,
                        Notes = link.Notes
                    });
                }

                newRoster.StudentCount = sourceLinks.Count;
                _db.SaveChanges();
                InvalidateCache();

                Log.Information("Duplicated roster #{Source} → #{New}: {Name} ({Count} students)",
                    sourceRosterId, newRoster.Id, newClassName, sourceLinks.Count);
                return newRoster;
            }
            catch (Exception ex)
            {
                Log.Warning("DuplicateRoster error: {Err}", ex.Message);
                return null;
            }
        }
    }
}
