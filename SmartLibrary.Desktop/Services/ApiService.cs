using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartLibrary.Desktop.Services
{
    public class ApiService
    {
        public static bool IsOfflineMode { get; set; } = false;
        private readonly HttpClient _httpClient;
        public static string BaseUrl = "https://localhost:7081/api"; // Lấy từ Mockup/kế hoạch

        public ApiService()
        {
            try
            {
                string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (System.IO.File.Exists(configPath))
                {
                    string jsonContent = System.IO.File.ReadAllText(configPath);
                    using var doc = JsonDocument.Parse(jsonContent);
                    if (doc.RootElement.TryGetProperty("ApiService", out var apiProp) &&
                        apiProp.TryGetProperty("BaseUrl", out var urlProp) &&
                        !string.IsNullOrEmpty(urlProp.GetString()))
                    {
                        BaseUrl = urlProp.GetString()!;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Cảnh báo: Không tìm thấy tệp cấu hình appsettings.json. Hệ thống sẽ sử dụng cấu hình API mặc định (localhost).");
                }
            }
            catch { }

            var handler = new HttpClientHandler();
#if DEBUG
            // Chỉ bỏ qua chứng chỉ SSL lỗi ở local dev để đảm bảo an toàn
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
#endif
            
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        private void SetAuthorizationHeader()
        {
            if (!string.IsNullOrEmpty(AuthService.CurrentToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthService.CurrentToken);
            }
        }

        public HttpClient GetHttpClient()
        {
            SetAuthorizationHeader();
            return _httpClient;
        }

        public event EventHandler<string>? OnConnectionError;

        private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, bool silent = false)
        {
            if (IsOfflineMode)
            {
                throw new HttpRequestException("Running in offline mode (simulated).");
            }
            int maxRetries = 3;
            int delayMs = 2000;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is System.Net.Sockets.SocketException)
                {
                    if (i == maxRetries - 1)
                    {
                        if (!silent)
                        {
                            OnConnectionError?.Invoke(this, "Mất kết nối máy chủ. Vui lòng kiểm tra lại mạng.");
                        }
                        throw;
                    }
                    await Task.Delay(delayMs);
                    delayMs *= 2; // Exponential backoff (2s, 4s, 8s)
                }
            }
            return default!;
        }

        public async Task<T> GetAsync<T>(string endpoint, bool silent = false)
        {
            SetAuthorizationHeader();
            return await ExecuteWithRetryAsync(async () => {
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>();
            }, silent);
        }

        public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, bool silent = false)
        {
            SetAuthorizationHeader();
            return await ExecuteWithRetryAsync(async () => {
                var response = await _httpClient.PostAsJsonAsync(endpoint, data);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TResponse>();
            }, silent);
        }

        public async Task PostAsync<TRequest>(string endpoint, TRequest data, bool silent = false)
        {
            SetAuthorizationHeader();
            await ExecuteWithRetryAsync<bool>(async () => {
                var response = await _httpClient.PostAsJsonAsync(endpoint, data);
                response.EnsureSuccessStatusCode();
                return true;
            }, silent);
        }

        public async Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            SetAuthorizationHeader();
            return await ExecuteWithRetryAsync(async () => {
                var response = await _httpClient.PutAsJsonAsync(endpoint, data);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TResponse>();
            });
        }

        public async Task DeleteAsync(string endpoint)
        {
            SetAuthorizationHeader();
            await ExecuteWithRetryAsync<bool>(async () => {
                var response = await _httpClient.DeleteAsync(endpoint);
                response.EnsureSuccessStatusCode();
                return true;
            });
        }
    }
}
