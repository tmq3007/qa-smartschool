using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Services
{
    /// <summary>
    /// DTO đồng bộ điểm số chuẩn hóa gửi lên Sở Giáo dục.
    /// </summary>
    public class StudentScoreSyncDto
    {
        public string SchoolCode { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public double Score { get; set; }
        public double MaxScore { get; set; }
        public string AssessmentType { get; set; } = "Quiz";
        public DateTime RecordedAt { get; set; }
    }

    /// <summary>
    /// Dịch vụ đồng bộ điểm số an toàn lên Sở Giáo dục qua RESTful API HTTPS.
    /// Chạy tập trung từ máy chủ trường (TeacherHub/Server trường).
    /// </summary>
    public class ScoreSyncService
    {
        private readonly AppDbContext _db;
        private static readonly HttpClient _httpClient = new HttpClient();

        public ScoreSyncService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Đồng bộ danh sách điểm số của trường lên API HTTPS của Sở GD.
        /// Sử dụng kênh truyền HTTPS, xác thực bằng JWT Bearer Token.
        /// </summary>
        public async Task<bool> SyncScoresToDepartmentApiAsync(string schoolCode, string departmentApiUrl, string jwtToken)
        {
            try
            {
                Log.Information("[ScoreSync] Starting score synchronization for school {SchoolCode}...", schoolCode);

                // Lấy tất cả lịch sử điểm số của học sinh trong DB trường
                var scores = (from la in _db.LearningAnalytics
                             join s in _db.Students on la.StudentId equals s.Id
                             select new StudentScoreSyncDto
                             {
                                 SchoolCode = schoolCode,
                                 StudentCode = s.StudentCode,
                                 StudentName = s.FullName,
                                 ClassName = s.ClassName ?? "Chưa rõ",
                                 SubjectName = la.Subject,
                                 TopicName = la.Topic,
                                 Score = la.Score,
                                 MaxScore = la.MaxScore,
                                 AssessmentType = la.AssessmentType,
                                 RecordedAt = la.RecordedAt
                             }).ToList();

                if (scores.Count == 0)
                {
                    Log.Information("[ScoreSync] No learning scores found to synchronize.");
                    return true;
                }

                // Đóng gói JSON payload
                var json = JsonSerializer.Serialize(scores);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // Cấu hình Request và Header bảo mật
                var request = new HttpRequestMessage(HttpMethod.Post, departmentApiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                request.Content = content;

                // Gửi qua HTTPS SSL/TLS 1.3
                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Log.Information("[ScoreSync] Successfully synchronized {Count} scores to Department API.", scores.Count);
                    return true;
                }
                else
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    Log.Warning("[ScoreSync] Synchronization failed. Server returned Status: {Status}, Details: {Details}",
                        response.StatusCode, errorDetails);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[ScoreSync] Error during score synchronization");
                return false;
            }
        }
    }
}
