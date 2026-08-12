using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using QASmartClass.Data;
using Serilog;
using Microsoft.EntityFrameworkCore;

namespace QASmartClass.Helpers
{
    public static class OfflineSyncManager
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static CancellationTokenSource? _cts;
        private static Task? _syncTask;
        private static readonly object _lock = new object();
        private static bool _isRunning = false;

        public static void Start()
        {
            lock (_lock)
            {
                if (_isRunning) return;
                _isRunning = true;
                _cts = new CancellationTokenSource();
                _syncTask = Task.Run(() => SyncLoopAsync(_cts.Token));
                Log.Information("[OfflineSyncManager] Started background sync loop.");
            }
        }

        public static void Stop()
        {
            lock (_lock)
            {
                if (!_isRunning) return;
                _isRunning = false;
                _cts?.Cancel();
                try
                {
                    _syncTask?.Wait(1000);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[OfflineSyncManager] Exception while stopping sync task.");
                }
                _cts?.Dispose();
                _cts = null;
                _syncTask = null;
                Log.Information("[OfflineSyncManager] Stopped background sync loop.");
            }
        }

        public static async Task QueueItemAsync(string actionType, object payload)
        {
            try
            {
                string json = JsonSerializer.Serialize(payload);
                using var db = new AppDbContext();
                var item = new OfflineSyncItem
                {
                    ActionType = actionType,
                    PayloadJson = json,
                    CreatedAt = DateTime.Now,
                    RetryCount = 0,
                    Status = "Pending"
                };
                db.OfflineSyncItems.Add(item);
                await db.SaveChangesAsync();
                Log.Information("[OfflineSyncManager] Queued offline item {Id} for ActionType {ActionType}", item.Id, actionType);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[OfflineSyncManager] Failed to queue offline item");
            }
        }

        private static async Task SyncLoopAsync(CancellationToken token)
        {
            var random = new Random();
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Wait 10 seconds before checking again
                    await Task.Delay(10000, token);

                    // Check if network is available
                    if (!await IsServerOnlineAsync())
                    {
                        Log.Debug("[OfflineSyncManager] Server is offline, skipping sync check.");
                        continue;
                    }

                    using var db = new AppDbContext();
                    // Chronological FIFO processing (ORDER BY CreatedAt ASC)
                    var pendingItems = await db.OfflineSyncItems
                        .Where(x => x.Status == "Pending")
                        .OrderBy(x => x.CreatedAt)
                        .ToListAsync(token);

                    if (!pendingItems.Any()) continue;

                    Log.Information("[OfflineSyncManager] Found {Count} pending items to sync.", pendingItems.Count);

                    foreach (var item in pendingItems)
                    {
                        if (token.IsCancellationRequested) break;

                        bool success = await TrySyncItemAsync(item);
                        if (success)
                        {
                            db.OfflineSyncItems.Remove(item);
                            await db.SaveChangesAsync();
                            Log.Information("[OfflineSyncManager] Sync succeeded for item {Id}. Removed from queue.", item.Id);
                        }
                        else
                        {
                            item.RetryCount++;
                            if (item.RetryCount >= 5)
                            {
                                item.Status = "Failed";
                                Log.Warning("[OfflineSyncManager] Item {Id} failed after 5 retries. Marked as Failed.", item.Id);
                            }
                            else
                            {
                                // Jittered Exponential Back-off delay: base 2^RetryCount * 1000ms + random jitter
                                double baseDelay = Math.Pow(2, item.RetryCount) * 1000;
                                int jitter = random.Next(100, 1000);
                                int totalDelay = (int)baseDelay + jitter;
                                Log.Information("[OfflineSyncManager] Item {Id} sync failed. Retrying in {Delay} ms. (Retry count: {Count})", item.Id, totalDelay, item.RetryCount);
                                await Task.Delay(totalDelay, token);
                            }
                            db.Entry(item).State = EntityState.Modified;
                            await db.SaveChangesAsync();
                            
                            // Stop processing subsequent items to preserve FIFO chronological order
                            break;
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[OfflineSyncManager] Error in sync loop");
                }
            }
        }

        private static async Task<bool> IsServerOnlineAsync()
        {
            string serverIp = "127.0.0.1";
            try
            {
                using var db = new AppDbContext();
                var ipSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "ServerIP" || s.Id == "TeacherIP" || s.Id == "Server_IP");
                if (ipSetting != null && !string.IsNullOrEmpty(ipSetting.Value))
                {
                    serverIp = ipSetting.Value;
                }
            }
            catch
            {
                // Fallback to loopback
            }

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(serverIp, 1000);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<bool> TrySyncItemAsync(OfflineSyncItem item)
        {
            try
            {
                Log.Information("[OfflineSyncManager] Syncing item {Id} (ActionType: {ActionType})", item.Id, item.ActionType);
                
                string serverIp = "127.0.0.1";
                using (var db = new AppDbContext())
                {
                    var ipSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "ServerIP" || s.Id == "TeacherIP" || s.Id == "Server_IP");
                    if (ipSetting != null && !string.IsNullOrEmpty(ipSetting.Value))
                    {
                        serverIp = ipSetting.Value;
                    }
                }

                // Simulate sending to WebAPI/Socket
                string syncUrl = $"http://{serverIp}:9000/api/sync/{item.ActionType.ToLower()}";
                var content = new StringContent(item.PayloadJson, Encoding.UTF8, "application/json");
                
                using var cts = new CancellationTokenSource(2000);
                var response = await _httpClient.PostAsync(syncUrl, content, cts.Token);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                Log.Warning("[OfflineSyncManager] Server returned status code {StatusCode} for sync item {Id}", response.StatusCode, item.Id);
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning("[OfflineSyncManager] Connection failed for sync item {Id}: {Message}", item.Id, ex.Message);
                return false;
            }
        }
    }
}
