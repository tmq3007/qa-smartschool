using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace QASmartClass.LearningTools.Helpers
{
    public static class CloudSyncService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        private const string SyncUrl = "http://localhost:5000/api/progress";
        private static System.Timers.Timer? _retryTimer;

        static CloudSyncService()
        {
            try
            {
                _retryTimer = new System.Timers.Timer { Interval = TimeSpan.FromMinutes(2).TotalMilliseconds };
                _retryTimer.Elapsed += async (s, e) =>
                {
                    if (_retryTimer == null) return;
                    _retryTimer.Stop(); // Tạm dừng để tránh chạy chồng chéo luồng khi đồng bộ chậm
                    try
                    {
                        await ProcessPendingSyncQueueAsync();
                    }
                    finally
                    {
                        _retryTimer.Start();
                    }
                };
                _retryTimer.Start();
            }
            catch
            {
                // Safety first
            }
        }

        public static void Initialize()
        {
            // Trình kích hoạt hàm khởi tạo tĩnh
        }

        public static async Task<bool> SyncProgressAsync(UserProgress progress)
        {
            try
            {
                var payload = new
                {
                    progress.GameName,
                    progress.Score,
                    progress.Total,
                    progress.DurationSeconds,
                    progress.Difficulty,
                    CreatedAt = progress.CreatedAt.ToString("o"),
                    progress.IsEndless
                };

                string json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(SyncUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                // Lỗi mạng hoặc server không hoạt động
            }

            // Ghi nhận lưu offline khi lỗi đồng bộ
            DbManager.SavePendingSync(progress);
            return false;
        }

        public static async Task ProcessPendingSyncQueueAsync()
        {
            try
            {
                var pendings = DbManager.GetPendingSyncs();
                if (pendings == null || pendings.Count == 0) return;

                foreach (var item in pendings)
                    try
                    {
                        var payload = new
                        {
                            item.GameName,
                            item.Score,
                            item.Total,
                            item.DurationSeconds,
                            item.Difficulty,
                            CreatedAt = item.CreatedAt.ToString("o"),
                            item.IsEndless
                        };

                        string json = JsonSerializer.Serialize(payload);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await _httpClient.PostAsync(SyncUrl, content);
                        if (response.IsSuccessStatusCode)
                        {
                            DbManager.DeletePendingSync(item.Id);
                        }
                    }
                    catch
                    {
                        // Vẫn chưa kết nối được mạng, tạm dừng thử lại lượt này
                        break;
                    }
            }
            catch
            {
                // Tránh lỗi ném ra luồng chính
            }
        }
    }
}
