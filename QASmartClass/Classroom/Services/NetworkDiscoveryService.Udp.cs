using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    // ═══════════════════════════════════════════════════════════
    //  NETWORK DISCOVERY — UDP Beacon
    //  Tách từ NetworkDiscoveryService.cs (lines 159–189)
    // ═══════════════════════════════════════════════════════════
    public partial class NetworkDiscoveryService
    {
        /// <summary>
        /// Phát UDP beacon mỗi 2 giây để máy học sinh tìm thấy server.
        /// Format: "QASC|className|teacherName|serverIP|tcpPort"
        /// </summary>
        private async Task RunUdpBroadcastAsync(string beacon, CancellationToken ct)
        {
            var bytes    = Encoding.UTF8.GetBytes(beacon);
            var endpoint = new IPEndPoint(IPAddress.Broadcast, DISCOVERY_PORT);

            try
            {
                _udpBroadcaster = new UdpClient { EnableBroadcast = true };
                Log.Information("UDP beacon active on :{Port}", DISCOVERY_PORT);

                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        await _udpBroadcaster.SendAsync(bytes, bytes.Length, endpoint);
                        await Task.Delay(BEACON_INTERVAL_MS, ct);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex) when (!ct.IsCancellationRequested)
                    {
                        Log.Warning("UDP send error: {Err}", ex.Message);
                        await Task.Delay(5000, ct);
                    }
                }
            }
            catch (Exception ex) { Log.Error("UDP broadcaster fatal: {Err}", ex.Message); }
        }
    }
}
