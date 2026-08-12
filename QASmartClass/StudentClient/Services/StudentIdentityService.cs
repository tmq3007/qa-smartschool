using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.StudentClient.Services
{
    /// <summary>
    /// Service nhận diện học sinh hiện tại từ profile cache file.
    /// Dùng chung cho tất cả Student pages — thay thế code trùng lặp ở
    /// StudentDashboardPage, StudentSubmitPage, AnalyticsPage.
    /// </summary>
    public class StudentIdentityService
    {
        private readonly AppDbContext _db;
        private int? _cachedStudentId;
        private string? _cachedStudentCode;
        private string? _cachedStudentName;

        public StudentIdentityService(AppDbContext db)
        {
            _db = db;
        }

        private string ReadSecureProfileText(string path)
        {
            return SecureProfileHelper.ReadProfileText(path);
        }

        private void WriteSecureProfileText(string path, string text)
        {
            SecureProfileHelper.WriteProfileText(path, text);
        }

        /// <summary>
        /// Lấy thông tin học sinh hiện tại.
        /// Đọc từ profile file, tra cứu DB, cache kết quả.
        /// </summary>
        /// <returns>Tuple (Id, Code, Name) của học sinh hiện tại</returns>
        public (int Id, string Code, string Name) GetCurrentStudent()
        {
            if (_cachedStudentId.HasValue)
                return (_cachedStudentId.Value, _cachedStudentCode!, _cachedStudentName!);

            string code = "HS001", name = "Học sinh";
            try
            {
                var path = QASmartClass.Services.AppPaths.StudentProfileFile;
                if (File.Exists(path))
                {
                    var json = ReadSecureProfileText(path);
                    var profile = JsonSerializer.Deserialize<StudentProfileDto>(json);
                    if (profile != null && !string.IsNullOrWhiteSpace(profile.StudentCode))
                    {
                        code = profile.StudentCode.Trim();
                        name = profile.StudentName?.Trim() ?? "Học sinh";
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("StudentIdentityService: Cannot read profile file: {Err}", ex.Message);
            }

            // Tra cứu DB bằng StudentCode — so sánh case-insensitive + trim
            var student = _db.Students.FirstOrDefault(s => s.StudentCode == code)
                       ?? _db.Students.FirstOrDefault(s => s.StudentCode.Trim().ToLower() == code.ToLower());

            if (student != null)
            {
                // Tìm thấy student trong DB → dùng thông tin từ DB (chính xác hơn)
                _cachedStudentId = student.Id;
                _cachedStudentCode = student.StudentCode;
                _cachedStudentName = student.FullName;

                // Calculate Daily Streak
                try
                {
                    var today = DateTime.Today;
                    var tomorrow = today.AddDays(1);
                    var hasActivity = _db.QuizResults.Any(q => q.StudentId == student.Id && q.SubmittedAt >= today && q.SubmittedAt < tomorrow)
                                      || _db.LearningDiaries.Any(d => d.StudentId == student.Id && d.Date >= today && d.Date < tomorrow);

                    if (hasActivity)
                    {
                        if (student.LastActiveDate == null)
                        {
                            student.DailyStreak = 1;
                            student.LastActiveDate = today;
                            _db.SaveChanges();
                        }
                        else
                        {
                            var lastActive = student.LastActiveDate.Value.Date;
                            if (lastActive != today)
                            {
                                if (lastActive == today.AddDays(-1))
                                {
                                    student.DailyStreak += 1;
                                }
                                else
                                {
                                    student.DailyStreak = 1;
                                }
                                student.LastActiveDate = today;
                                _db.SaveChanges();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to update daily streak for student {Code}", student.StudentCode);
                }

                // Auto-update profile nếu tên trong file rỗng/sai so với DB
                if (name != student.FullName)
                {
                    try
                    {
                        var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                        if (File.Exists(profilePath))
                        {
                            var profileJson = ReadSecureProfileText(profilePath);
                            // Cập nhật StudentName trong JSON profile
                            var updatedProfile = JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonObject>(profileJson);
                            if (updatedProfile != null)
                            {
                                updatedProfile["StudentName"] = student.FullName;
                                WriteSecureProfileText(profilePath, updatedProfile.ToJsonString());
                                Log.Information("StudentIdentityService: Auto-updated profile name from '{Old}' to '{New}'", name, student.FullName);
                            }
                        }
                    }
                    catch (Exception ex2) { Log.Warning("Auto-update profile failed: {Err}", ex2.Message); }
                }
            }
            else
            {
                // Không tìm thấy trong DB → giữ nguyên tên từ profile login
                // KHÔNG fallback sang Students.FirstOrDefault() để tránh hiển thị sai tên
                _cachedStudentId = 0;
                _cachedStudentCode = code;
                _cachedStudentName = name;
                Log.Warning("StudentIdentityService: Student code '{Code}' not found in DB, using profile name '{Name}'", code, name);
            }

            Log.Information("StudentIdentityService: Resolved student {Code} ({Name}), Id={Id}", _cachedStudentCode, _cachedStudentName, _cachedStudentId);
            return (_cachedStudentId.Value, _cachedStudentCode!, _cachedStudentName!);
        }

        /// <summary>Xóa cache — dùng khi đổi tài khoản HS</summary>
        public void ClearCache()
        {
            _cachedStudentId = null;
            _cachedStudentCode = null;
            _cachedStudentName = null;
        }

        /// <summary>DTO cho file profile JSON</summary>
        private class StudentProfileDto
        {
            public string StudentCode { get; set; } = "";
            public string StudentName { get; set; } = "";
        }
    }
}

