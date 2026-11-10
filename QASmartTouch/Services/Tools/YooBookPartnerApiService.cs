using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QASmartTouch.Services.Tools
{
    /// <summary>
    /// Dịch vụ kết nối Kho học liệu 3D & Phòng thí nghiệm ảo YooBook (Eduverse Partner API & Smart Web Mode).
    /// Hỗ trợ:
    /// 1. Lưu trữ cấu hình Tài khoản Demo tự động đăng nhập (Auto-Login) và khóa đối tác X-Api-Key.
    /// 2. Gọi cổng Eduverse B2B Partner API (https://api.eduverse.vn/api/v1/partner/*).
    /// 3. Bộ nhớ đệm (Cache) thông minh giúp tiết kiệm 70%-90% hạn mức resource_launch & api_call.
    /// </summary>
    public sealed class YooBookPartnerApiService
    {
        // =========================================================================
        // CẤU HÌNH MẶC ĐỊNH — BẠN ĐIỀN TÀI KHOẢN DEMO YOOBOOK HOẶC API KEY VÀO ĐÂY
        // =========================================================================
        /// <summary>
        /// Điền Email hoặc Số điện thoại tài khoản Demo YooBook được cấp vào đây để tự động đăng nhập mặc định.
        /// Ví dụ: "demo@yoobook.vn" hoặc "0912345678"
        /// </summary>
        public const string DEFAULT_DEMO_USERNAME = "demogiaoduc03@gmail.com";

        /// <summary>
        /// Điền Mật khẩu tài khoản Demo YooBook được cấp vào đây để tự động đăng nhập mặc định.
        /// </summary>
        public const string DEFAULT_DEMO_PASSWORD = "demogiaoduc03@123#";

        /// <summary>
        /// Điền mã X-Api-Key đối tác (nếu bên YooTek cấp riêng mã dạng "yk_live_..." hoặc "yk_test_...").
        /// Nếu chỉ dùng tài khoản Demo đăng nhập web thì để trống chuỗi này.
        /// </summary>
        public const string DEFAULT_PARTNER_API_KEY = "";

        // =========================================================================
        // HẰNG SỐ ĐỊA CHỈ KHO HỌC LIỆU & PARTNER API
        // =========================================================================
        public const string PartnerApiBaseUrl = "https://api.eduverse.vn/api/v1/partner";
        public const string ExploreBaseUrl = "https://yoobook.vn/explore";
        public const string LoginUrl = "https://yoobook.vn/auth/login";

        // Các phân hệ chuyên mục trong Kho dữ liệu YooBook
        public const string CategoryAllUrl = "https://yoobook.vn/explore";
        public const string CategoryVirtualLabUrl = "https://yoobook.vn/explore?category=virtual_lab";
        public const string Category3DSceneUrl = "https://yoobook.vn/explore?category=teaching_support";
        public const string CategoryNaturalScienceUrl = "https://yoobook.vn/explore?category=natural_science";
        public const string CategorySocialScienceUrl = "https://yoobook.vn/explore?category=social_science";
        public const string CategorySandboxStemUrl = "https://yoobook.vn/explore?category=sandbox";
        public const string CategoryCultureHistoryUrl = "https://yoobook.vn/explore?category=culture_history";

        private static readonly Lazy<YooBookPartnerApiService> _instance =
            new Lazy<YooBookPartnerApiService>(() => new YooBookPartnerApiService());

        public static YooBookPartnerApiService Instance => _instance.Value;

        private readonly HttpClient _httpClient;
        private readonly string _configFilePath;
        private readonly ConcurrentDictionary<long, CachedLaunchToken> _launchTokenCache =
            new ConcurrentDictionary<long, CachedLaunchToken>();

        private JArray? _cachedCurriculumTree;
        private DateTime _curriculumCacheTime = DateTime.MinValue;

        public YooBookConfig CurrentConfig { get; private set; }

        private YooBookPartnerApiService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            string configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QASmartClass",
                "Config");
            Directory.CreateDirectory(configDir);
            _configFilePath = Path.Combine(configDir, "yoobook_config.json");

            CurrentConfig = LoadConfig();
        }

        /// <summary>
        /// Lấy Username tài khoản Demo đang có hiệu lực (ưu tiên cấu hình người dùng lưu, sau đó đến hằng số DEFAULT_DEMO_USERNAME).
        /// </summary>
        public string GetEffectiveDemoUsername()
        {
            if (!string.IsNullOrWhiteSpace(CurrentConfig.DemoUsername))
                return CurrentConfig.DemoUsername.Trim();
            return DEFAULT_DEMO_USERNAME.Trim();
        }

        /// <summary>
        /// Lấy Password tài khoản Demo đang có hiệu lực (ưu tiên cấu hình người dùng lưu, sau đó đến hằng số DEFAULT_DEMO_PASSWORD).
        /// </summary>
        public string GetEffectiveDemoPassword()
        {
            string decrypted = DecryptSecret(CurrentConfig.EncryptedDemoPassword);
            if (!string.IsNullOrEmpty(decrypted))
                return decrypted;
            return DEFAULT_DEMO_PASSWORD;
        }

        /// <summary>
        /// Lấy X-Api-Key đang có hiệu lực.
        /// </summary>
        public string GetEffectiveApiKey()
        {
            string decrypted = DecryptSecret(CurrentConfig.EncryptedApiKey);
            if (!string.IsNullOrWhiteSpace(decrypted))
                return decrypted.Trim();
            return DEFAULT_PARTNER_API_KEY.Trim();
        }

        public bool HasDemoCredentials =>
            !string.IsNullOrWhiteSpace(GetEffectiveDemoUsername()) &&
            !string.IsNullOrEmpty(GetEffectiveDemoPassword());

        public bool HasPartnerApiKey =>
            !string.IsNullOrWhiteSpace(GetEffectiveApiKey());

        /// <summary>
        /// Lưu cấu hình Tài khoản Demo và X-Api-Key xuống ổ cứng (mã hóa DPAPI theo máy Windows hiện tại).
        /// </summary>
        public void SaveCredentials(string demoUsername, string demoPassword, string apiKey, bool strictExploreOnly = true)
        {
            CurrentConfig = new YooBookConfig
            {
                DemoUsername = (demoUsername ?? string.Empty).Trim(),
                EncryptedDemoPassword = EncryptSecret(demoPassword ?? string.Empty),
                EncryptedApiKey = EncryptSecret((apiKey ?? string.Empty).Trim()),
                StrictExploreOnly = strictExploreOnly,
                UpdatedAtUtc = DateTime.UtcNow
            };

            try
            {
                string json = JsonConvert.SerializeObject(CurrentConfig, Formatting.Indented);
                File.WriteAllText(_configFilePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[YooBookPartnerApiService] SaveCredentials error: {ex.Message}");
            }
        }

        private YooBookConfig LoadConfig()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath, Encoding.UTF8);
                    var cfg = JsonConvert.DeserializeObject<YooBookConfig>(json);
                    if (cfg != null)
                        return cfg;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[YooBookPartnerApiService] LoadConfig error: {ex.Message}");
            }

            return new YooBookConfig
            {
                DemoUsername = DEFAULT_DEMO_USERNAME,
                EncryptedDemoPassword = EncryptSecret(DEFAULT_DEMO_PASSWORD),
                EncryptedApiKey = EncryptSecret(DEFAULT_PARTNER_API_KEY),
                StrictExploreOnly = true,
                UpdatedAtUtc = DateTime.UtcNow
            };
        }

        private static string EncryptSecret(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(plainText);
                byte[] protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(protectedBytes);
            }
            catch
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
            }
        }

        private static string DecryptSecret(string? cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText))
                return string.Empty;

            try
            {
                byte[] protectedBytes = Convert.FromBase64String(cipherText);
                byte[] bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                try
                {
                    return Encoding.UTF8.GetString(Convert.FromBase64String(cipherText));
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        // =========================================================================
        // CÁC PHƯƠNG THỨC GỌI CỔNG PARTNER API (https://api.eduverse.vn/api/v1/partner/*)
        // =========================================================================

        /// <summary>
        /// 1. Kiểm tra kết nối cổng Partner API (GET /api/v1/partner/health)
        /// </summary>
        public async Task<(bool IsHealthy, string Message)> CheckHealthAsync(string? overrideApiKey = null, CancellationToken ct = default)
        {
            string apiKey = !string.IsNullOrWhiteSpace(overrideApiKey)
                ? overrideApiKey!.Trim()
                : GetEffectiveApiKey();

            if (string.IsNullOrWhiteSpace(apiKey))
                return (false, "Chưa cấu hình X-Api-Key.");

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, $"{PartnerApiBaseUrl}/health");
                req.Headers.Add("X-Api-Key", apiKey);

                using var res = await _httpClient.SendAsync(req, ct);
                string body = await res.Content.ReadAsStringAsync(ct);

                if (res.IsSuccessStatusCode)
                {
                    var obj = JObject.Parse(body);
                    string status = obj["data"]?["status"]?.ToString() ?? "healthy";
                    string env = obj["data"]?["environment"]?.ToString() ?? "production";
                    string msg = obj["data"]?["message"]?.ToString() ?? "Kết nối thành công";
                    return (true, $"{msg} ({status} - {env})");
                }
                else
                {
                    var errObj = JObject.Parse(body);
                    string code = errObj["error"]?["code"]?.ToString() ?? res.StatusCode.ToString();
                    string msg = errObj["error"]?["message"]?.ToString() ?? "Lỗi xác thực API Key.";
                    return (false, $"[{code}] {msg}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Không thể kết nối tới cổng Partner API: {ex.Message}");
            }
        }

        /// <summary>
        /// 2. Lấy cây chương trình học (GET /api/v1/partner/curriculum-tree) kèm Cache tiết kiệm api_call.
        /// </summary>
        public async Task<JArray?> GetCurriculumTreeAsync(bool forceRefresh = false, CancellationToken ct = default)
        {
            if (!forceRefresh && _cachedCurriculumTree != null && (DateTime.UtcNow - _curriculumCacheTime).TotalHours < 12)
                return _cachedCurriculumTree;

            string apiKey = GetEffectiveApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
                return null;

            using var req = new HttpRequestMessage(HttpMethod.Get, $"{PartnerApiBaseUrl}/curriculum-tree");
            req.Headers.Add("X-Api-Key", apiKey);

            using var res = await _httpClient.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
                return null;

            string body = await res.Content.ReadAsStringAsync(ct);
            var obj = JObject.Parse(body);
            if (obj["data"] is JArray tree)
            {
                _cachedCurriculumTree = tree;
                _curriculumCacheTime = DateTime.UtcNow;
                return tree;
            }

            return null;
        }

        /// <summary>
        /// 3. Tìm kiếm danh sách học liệu 3D / Phòng thí nghiệm ảo (GET /api/v1/partner/resources).
        /// </summary>
        public async Task<JObject?> SearchResourcesAsync(
            int? module = null,
            int? gradeId = null,
            int? subjectId = null,
            int? lessonId = null,
            string? keyword = null,
            int skipCount = 0,
            int maxResultCount = 20,
            CancellationToken ct = default)
        {
            string apiKey = GetEffectiveApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
                return null;

            var queryParams = new List<string>
            {
                $"SkipCount={skipCount}",
                $"MaxResultCount={maxResultCount}"
            };

            if (module.HasValue) queryParams.Add($"Module={module.Value}");
            if (gradeId.HasValue) queryParams.Add($"GradeId={gradeId.Value}");
            if (subjectId.HasValue) queryParams.Add($"SubjectId={subjectId.Value}");
            if (lessonId.HasValue) queryParams.Add($"LessonId={lessonId.Value}");
            if (!string.IsNullOrWhiteSpace(keyword))
                queryParams.Add($"Keyword={Uri.EscapeDataString(keyword.Trim())}");

            string url = $"{PartnerApiBaseUrl}/resources?{string.Join("&", queryParams)}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("X-Api-Key", apiKey);

            using var res = await _httpClient.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
                return null;

            string body = await res.Content.ReadAsStringAsync(ct);
            return JObject.Parse(body)["data"] as JObject;
        }

        /// <summary>
        /// 4. Tạo Launch Token mở mô hình 3D (POST /api/v1/partner/resources/{id}/launch-tokens).
        /// Tự động Cache lại launchUrl cho đến khi hết hạn (expiresAt) để không bị trừ oan lượt resource_launch
        /// khi giáo viên đóng/mở lại cùng 1 bài thí nghiệm trong tiết học.
        /// </summary>
        public async Task<(bool Success, string LaunchUrlOrError)> CreateLaunchTokenAsync(
            long resourceId,
            string externalUserId = "qasmarttouch-board-teacher",
            CancellationToken ct = default)
        {
            // Kiểm tra Cache trước: nếu Token của bài học này còn hạn > 2 phút thì dùng lại luôn (tiết kiệm 1 lượt resource_launch)
            if (_launchTokenCache.TryGetValue(resourceId, out var cached) &&
                cached.ExpiresAtUtc > DateTime.UtcNow.AddMinutes(2) &&
                !string.IsNullOrWhiteSpace(cached.LaunchUrl))
            {
                return (true, cached.LaunchUrl);
            }

            string apiKey = GetEffectiveApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
                return (false, "Chưa cấu hình X-Api-Key đối tác.");

            try
            {
                string idempotencyKey = $"qast-{resourceId}-{DateTime.UtcNow:yyyyMMddHH}";
                var payload = new
                {
                    externalUserId,
                    action = "launch",
                    idempotencyKey
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, $"{PartnerApiBaseUrl}/resources/{resourceId}/launch-tokens");
                req.Headers.Add("X-Api-Key", apiKey);
                req.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                using var res = await _httpClient.SendAsync(req, ct);
                string body = await res.Content.ReadAsStringAsync(ct);
                var json = JObject.Parse(body);

                if (res.IsSuccessStatusCode)
                {
                    string launchUrl = json["data"]?["launchUrl"]?.ToString() ?? string.Empty;
                    string expiresAtStr = json["data"]?["expiresAt"]?.ToString() ?? string.Empty;
                    DateTime expiresAt = DateTime.TryParse(expiresAtStr, out var parsed)
                        ? parsed.ToUniversalTime()
                        : DateTime.UtcNow.AddMinutes(30);

                    if (!string.IsNullOrWhiteSpace(launchUrl))
                    {
                        _launchTokenCache[resourceId] = new CachedLaunchToken
                        {
                            ResourceId = resourceId,
                            LaunchUrl = launchUrl,
                            ExpiresAtUtc = expiresAt
                        };
                        return (true, launchUrl);
                    }
                }

                string errCode = json["error"]?["code"]?.ToString() ?? res.StatusCode.ToString();
                string errMsg = json["error"]?["message"]?.ToString() ?? "Không thể khởi tạo phiên 3D.";
                return (false, $"[{errCode}] {errMsg}");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi gọi API Launch Token: {ex.Message}");
            }
        }
    }

    public sealed class YooBookConfig
    {
        public string DemoUsername { get; set; } = string.Empty;
        public string EncryptedDemoPassword { get; set; } = string.Empty;
        public string EncryptedApiKey { get; set; } = string.Empty;
        public bool StrictExploreOnly { get; set; } = true;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    internal sealed class CachedLaunchToken
    {
        public long ResourceId { get; set; }
        public string LaunchUrl { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }
}

