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
        /// <summary>Beacon hiện tại, trả lời cho truy vấn WHO| qua TCP (client không nhận được UDP broadcast).</summary>
        private volatile string _currentBeacon = "";

        /// <summary>Cập nhật nội dung Beacon động khi Giáo viên đổi Kênh / Tên Phòng / Khóa PIN</summary>
        public void UpdateBeaconInfo(int channelId, string className, string teacherName, bool requirePin = false, string roomPin = "")
        {
            ChannelId = Math.Clamp(channelId, 1, 20);
            CurrentClassName = string.IsNullOrWhiteSpace(className) ? $"Phòng máy {ChannelId:D2}" : className.Trim();
            CurrentTeacherName = string.IsNullOrWhiteSpace(teacherName) ? "Giáo viên" : teacherName.Trim();
            RequireRoomPin = requirePin;
            if (!string.IsNullOrWhiteSpace(roomPin))
                RoomPin = roomPin.Trim();

            if (string.IsNullOrEmpty(ServerIP))
                ServerIP = GetLocalIPAddress();

            // Tự động tách địa chỉ UDP Multicast theo từng Kênh (239.0.0.1:8088 .. 239.0.0.20:8107)
            QASmartClass.Services.UdpScreenBroadcastService.MulticastIP = $"239.0.0.{ChannelId}";
            QASmartClass.Services.UdpScreenBroadcastService.MulticastPort = 8087 + ChannelId;

            _currentBeacon = $"{BEACON_PREFIX}{CurrentClassName}|{CurrentTeacherName}|{ServerIP}|{TCP_PORT}|CH={ChannelId}|PIN={(RequireRoomPin ? "1" : "0")}";
            Log.Information("[MythwareBeacon] Updated beacon: {Beacon} (Multicast: {MIP}:{MPort})",
                _currentBeacon,
                QASmartClass.Services.UdpScreenBroadcastService.MulticastIP,
                QASmartClass.Services.UdpScreenBroadcastService.MulticastPort);
        }

        /// <summary>
        /// Phát UDP beacon mỗi 2 giây để máy học sinh tìm thấy server.
        /// Format: "QASC|className|teacherName|serverIP|tcpPort|CH=1|PIN=0"
        /// </summary>
        private async Task RunUdpBroadcastAsync(string beacon, CancellationToken ct)
        {
            _currentBeacon = beacon;
            var endpoint = new IPEndPoint(IPAddress.Broadcast, DISCOVERY_PORT);

            try
            {
                _udpBroadcaster = new UdpClient { EnableBroadcast = true };
                Log.Information("UDP beacon active on :{Port}", DISCOVERY_PORT);

                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var bytes = Encoding.UTF8.GetBytes(_currentBeacon);
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
