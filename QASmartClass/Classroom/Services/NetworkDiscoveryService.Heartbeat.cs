using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    // ═══════════════════════════════════════════════════════════
    //  NETWORK DISCOVERY — Heartbeat Watchdog
    //  Tách từ NetworkDiscoveryService.cs (lines 563–593)
    //  Phát hiện học sinh mất kết nối (timeout > 30s)
    //  và chủ động PING nếu không thấy tin trong 12s
    // ═══════════════════════════════════════════════════════════
    public partial class NetworkDiscoveryService
    {
        /// <summary>
        /// Watchdog kiểm tra heartbeat mỗi 5 giây.
        /// - Nếu HS không gửi tin trong 12s → PING
        /// - Nếu không gửi tin trong 30s → đóng kết nối (sẽ kích hoạt StudentDisconnected)
        /// </summary>
        private async Task RunHeartbeatWatchdogAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(5000, ct);
                }
                catch (OperationCanceledException) { break; }

                var now     = DateTime.Now;
                var timeout = TimeSpan.FromSeconds(HEARTBEAT_TIMEOUT_S);

                foreach (var (code, client) in _clients)
                {
                    var timeSinceLastSeen = now - client.LastSeen;

                    // Active ping: nếu > 12s chưa thấy, gửi PING để kiểm tra
                    if (timeSinceLastSeen > TimeSpan.FromSeconds(12) && timeSinceLastSeen <= timeout)
                    {
                        try { _ = SendToStudentDirectAsync(client, "CMD|PING"); }
                        catch { /* ignore */ }
                    }

                    // Passive timeout: nếu > 30s không có tin → ngắt kết nối
                    if (timeSinceLastSeen > timeout)
                    {
                        Log.Warning("Heartbeat timeout: {Name} ({Code})", client.Name, code);
                        client.TcpClient?.Close();
                    }
                }
            }
        }
    }
}
