using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

using System.Linq;

namespace QASmartClass.Services
{
    public class UdpScreenBroadcastService : IDisposable
    {
        public static string MulticastIP { get; set; } = "239.0.0.1";
        public static int MulticastPort { get; set; } = 8088;
        public static int MulticastTTL { get; set; } = 1;
        public static string EncryptionKey { get; set; } = "";

        public static UdpScreenBroadcastService Instance { get; } = new UdpScreenBroadcastService();
        private const int MaxSliceSize = 1400; // Optimized: 1400 + 8 header = 1408 < MTU 1472

        private UdpClient? _udpSender;
        private UdpClient? _udpReceiver;
        private CancellationTokenSource? _receiverCts;
        private int _frameIndex = 0;
        private IPEndPoint _multicastEP;

        // Assembly state on receiver
        private readonly ConcurrentDictionary<int, FrameAssembly> _activeAssemblies = new();
        private int _lastAssembledFrameIndex = -1;

        public event Action<byte[]>? FrameReceived;

        private UdpScreenBroadcastService()
        {
            LoadConfigFromDb();
            _multicastEP = new IPEndPoint(IPAddress.Parse(MulticastIP), MulticastPort);
            
            // Tự động đăng ký luật tường lửa Windows (V2.2.6)
            RegisterFirewallRulesAsync();
        }

        public void LoadConfigFromDb()
        {
            try
            {
                using (var db = new QASmartClass.Data.AppDbContext())
                {
                    var ipSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Network_MulticastAddress");
                    var portSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Network_MulticastPort");
                    if (ipSetting != null && !string.IsNullOrEmpty(ipSetting.Value))
                    {
                        MulticastIP = ipSetting.Value;
                    }
                    if (portSetting != null && int.TryParse(portSetting.Value, out int port))
                    {
                        MulticastPort = port;
                    }

                    var ttlSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Broadcast_MulticastTTL");
                    if (ttlSetting != null && int.TryParse(ttlSetting.Value, out int ttl))
                    {
                        MulticastTTL = ttl;
                    }
                }
                _multicastEP = new IPEndPoint(IPAddress.Parse(MulticastIP), MulticastPort);
            }
            catch (Exception ex)
            {
                Log.Warning("[UdpBroadcast] LoadConfigFromDb error: {Err}", ex.Message);
                _multicastEP = new IPEndPoint(IPAddress.Parse(MulticastIP), MulticastPort);
            }
        }

        public void StartSender()
        {
            try
            {
                LoadConfigFromDb();
                if (_udpSender == null)
                {
                    var localIPs = QASmartClass.Classroom.Services.NetworkDiscoveryService.GetActiveLocalIPv4Addresses();
                    var localIP = localIPs.Count > 0 ? localIPs[0] : null;

                    if (!string.IsNullOrEmpty(localIP) && localIP != "127.0.0.1")
                    {
                        _udpSender = new UdpClient(new IPEndPoint(IPAddress.Parse(localIP), 0));
                        _udpSender.JoinMulticastGroup(IPAddress.Parse(MulticastIP), MulticastTTL);
                        Log.Information("[UdpBroadcast] Sender bound to physical interface {LocalIP}. Target multicast: {IP}:{Port} (TTL: {TTL})", localIP, MulticastIP, MulticastPort, MulticastTTL);
                    }
                    else
                    {
                        _udpSender = new UdpClient();
                        _udpSender.JoinMulticastGroup(IPAddress.Parse(MulticastIP), MulticastTTL);
                        Log.Information("[UdpBroadcast] Sender initialized on default interface. Target: {IP}:{Port} (TTL: {TTL})", MulticastIP, MulticastPort, MulticastTTL);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UdpBroadcast] Failed to start sender");
            }
        }

        public async Task SendFrameAsync(byte[] imgBytes)
        {
            if (_udpSender == null)
            {
                StartSender();
            }

            if (_udpSender == null || imgBytes == null || imgBytes.Length == 0) return;

            try
            {
                int currentFrame = Interlocked.Increment(ref _frameIndex);
                int totalSlices = (int)Math.Ceiling((double)imgBytes.Length / MaxSliceSize);
                Log.Debug("[UdpBroadcast] Gửi Frame #{FrameIndex}: Kích thước={Size} bytes, Số phân mảnh={SlicesCount}, Khóa nén={Key}", 
                    currentFrame, imgBytes.Length, totalSlices, EncryptionKey);

                for (int i = 0; i < totalSlices; i++)
                {
                    int offset = i * MaxSliceSize;
                    int size = Math.Min(MaxSliceSize, imgBytes.Length - offset);

                    // Packet format: FrameIndex (4) | SliceIndex (2) | TotalSlices (2) | Timestamp (8) | Payload
                    byte[] packet = new byte[16 + size];
                    Buffer.BlockCopy(BitConverter.GetBytes(currentFrame), 0, packet, 0, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes((short)i), 0, packet, 4, 2);
                    Buffer.BlockCopy(BitConverter.GetBytes((short)totalSlices), 0, packet, 6, 2);
                    long timestamp = DateTime.UtcNow.Ticks;
                    Buffer.BlockCopy(BitConverter.GetBytes(timestamp), 0, packet, 8, 8);
                    Buffer.BlockCopy(imgBytes, offset, packet, 16, size);
                    CipherBytesInPlace(packet, 16, size, EncryptionKey);

                    await _udpSender.SendAsync(packet, packet.Length, _multicastEP);
                    AppPerformanceMonitor.Instance.RecordBytesTransmitted(packet.Length);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[UdpBroadcast] SendFrameAsync error: {Msg}", ex.Message);
            }
        }

        public void StartReceiver()
        {
            if (_udpReceiver != null) return;

            try
            {
                // LoadConfigFromDb(); // Cấu hình động từ GV qua SESSION_CONFIG, tránh ghi đè từ DB local của HS
                _udpReceiver = new UdpClient();
                _udpReceiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                
                // Bind to local wildcard port
                _udpReceiver.Client.Bind(new IPEndPoint(IPAddress.Any, MulticastPort));

                var localIPs = QASmartClass.Classroom.Services.NetworkDiscoveryService.GetActiveLocalIPv4Addresses();
                Log.Information("[UdpBroadcast] Bắt đầu khởi động bộ thu. Cấu hình cổng={Port}, IP Multicast={IP}, Khóa={Key}. Các Interfaces mạng tìm thấy: {Interfaces}", 
                    MulticastPort, MulticastIP, EncryptionKey, string.Join(", ", localIPs));
                bool joinedAny = false;
                foreach (var ip in localIPs)
                {
                    if (!string.IsNullOrEmpty(ip) && ip != "127.0.0.1")
                    {
                        try
                        {
                            _udpReceiver.JoinMulticastGroup(IPAddress.Parse(MulticastIP), IPAddress.Parse(ip));
                            Log.Information("[UdpBroadcast] Joined multicast group on interface {IP}", ip);
                            joinedAny = true;
                        }
                        catch (Exception exJoin)
                        {
                            Log.Warning("[UdpBroadcast] Failed to join multicast group on interface {IP}: {Err}", ip, exJoin.Message);
                        }
                    }
                }

                if (!joinedAny)
                {
                    _udpReceiver.JoinMulticastGroup(IPAddress.Parse(MulticastIP));
                    Log.Information("[UdpBroadcast] Joined multicast group on default interface");
                }

                _receiverCts = new CancellationTokenSource();
                _ = Task.Run(() => ReceiveLoopAsync(_receiverCts.Token));
                _ = Task.Run(() => CleanupLoopAsync(_receiverCts.Token));

                Log.Information("[UdpBroadcast] Receiver started on port {Port}, joined multicast group {IP}", MulticastPort, MulticastIP);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UdpBroadcast] Failed to start receiver");
                StopReceiver();
            }
        }

        // === UPGRADE_02: Fix zombie socket — DropMulticastGroup trước Close ===
        public void StopReceiver()
        {
            try
            {
                _receiverCts?.Cancel();

                // BƯỚC QUAN TRỌNG: Leave multicast group TRƯỚC khi close socket
                // RFC 3376 (IGMPv3): Host phải gửi Leave Group Message trước khi close
                // Trên card WiFi Realtek/Intel, nếu không gọi DropMulticastGroup() trước Close(),
                // OS kernel vẫn giữ IGMP membership → socket tiếp tục nhận multicast packets
                if (_udpReceiver != null)
                {
                    try
                    {
                        _udpReceiver.DropMulticastGroup(IPAddress.Parse(MulticastIP));
                        Log.Information("[UdpBroadcast] Dropped multicast group {IP}", MulticastIP);
                    }
                    catch (Exception exDrop)
                    {
                        // Không crash nếu đã leave hoặc socket đã đóng
                        Log.Debug("[UdpBroadcast] DropMulticastGroup skipped: {Msg}", exDrop.Message);
                    }
                }

                _receiverCts?.Dispose();
                _receiverCts = null;

                _udpReceiver?.Close();
                _udpReceiver?.Dispose();
                _udpReceiver = null;

                _activeAssemblies.Clear();
                _lastAssembledFrameIndex = -1;
                Log.Information("[UdpBroadcast] Receiver FULLY stopped — socket closed, multicast left");
            }
            catch (Exception ex)
            {
                Log.Warning("[UdpBroadcast] StopReceiver error: {Msg}", ex.Message);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpReceiver!.ReceiveAsync(token);
                    byte[] data = result.Buffer;
                    AppPerformanceMonitor.Instance.RecordBytesTransmitted(data.Length);

                    if (data.Length < 16) continue;

                    int frameIdx = BitConverter.ToInt32(data, 0);
                    short sliceIdx = BitConverter.ToInt16(data, 4);
                    short totalSlices = BitConverter.ToInt16(data, 6);
                    long timestamp = BitConverter.ToInt64(data, 8);

                    // Skip older frame slices
                    if (frameIdx <= _lastAssembledFrameIndex) continue;

                    var assembly = _activeAssemblies.GetOrAdd(frameIdx, _ => new FrameAssembly(totalSlices, timestamp));
                    
                    int payloadSize = data.Length - 16;
                    CipherBytesInPlace(data, 16, payloadSize, EncryptionKey);
                    byte[] payload = new byte[payloadSize];
                    Buffer.BlockCopy(data, 16, payload, 0, payloadSize);

                    if (assembly.AddSlice(sliceIdx, payload))
                    {
                        // Assembly complete!
                        byte[] reassembled = assembly.Assemble();
                        _activeAssemblies.TryRemove(frameIdx, out _);
                        
                        if (frameIdx > _lastAssembledFrameIndex)
                        {
                            _lastAssembledFrameIndex = frameIdx;
                            FrameReceived?.Invoke(reassembled);

                            // Record telemetry metrics
                            double latencyMs = (DateTime.UtcNow.Ticks - assembly.Timestamp) / 10000.0;
                            if (latencyMs < 0) latencyMs = 0;
                            StreamTelemetryCollector.Instance.RecordFrameReceived(frameIdx, totalSlices, assembly.ReceivedSlicesCount, latencyMs);
                        }

                        // Remove older assembly records
                        foreach (var key in _activeAssemblies.Keys)
                        {
                            if (key < frameIdx)
                            {
                                if (_activeAssemblies.TryRemove(key, out var oldAssembly))
                                {
                                    StreamTelemetryCollector.Instance.RecordFrameDropped(key, oldAssembly.TotalSlices, oldAssembly.ReceivedSlicesCount);
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Log.Warning("[UdpBroadcast] ReceiveLoopAsync error: {Msg}", ex.Message);
                    }
                }
            }
        }

        private async Task CleanupLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000, token);
                    var now = DateTime.UtcNow;
                    foreach (var kvp in _activeAssemblies)
                    {
                        // Discard frame assemblies older than 3000ms
                        if ((now - kvp.Value.CreatedAt).TotalMilliseconds > 3000)
                        {
                            if (_activeAssemblies.TryRemove(kvp.Key, out var assembly))
                            {
                                StreamTelemetryCollector.Instance.RecordFrameDropped(kvp.Key, assembly.TotalSlices, assembly.ReceivedSlicesCount);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch { }
            }
        }

        public void Dispose()
        {
            StopReceiver();
            _udpSender?.Close();
            _udpSender?.Dispose();
            _udpSender = null;
        }

        private static void CipherBytesInPlace(byte[] data, int offset, int length, string key)
        {
            if (string.IsNullOrEmpty(key) || length <= 0) return;
            int keyLen = key.Length;
            for (int i = 0; i < length; i++)
            {
                data[offset + i] ^= (byte)key[i % keyLen];
            }
        }

        private void RegisterFirewallRulesAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    string vncDir = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "vnctool");
                    if (!System.IO.Directory.Exists(vncDir) || !System.IO.File.Exists(System.IO.Path.Combine(vncDir, "vnctool-server.exe")))
                    {
                        vncDir = @"D:\JOB\vnctool";
                    }
                    string vncServerPath = System.IO.Path.Combine(vncDir, "vnctool-server.exe");
                    string vncClientPath = System.IO.Path.Combine(vncDir, "vnctool-client.exe");

                    RunNetshCommand("advfirewall firewall delete rule name=\"QA SmartClass - VNC Server\"");
                    RunNetshCommand($"advfirewall firewall add rule name=\"QA SmartClass - VNC Server\" dir=in action=allow program=\"{vncServerPath}\" enable=yes profile=any protocol=TCP localport=5900-6000 description=\"Allow VNC Server\"");

                    RunNetshCommand("advfirewall firewall delete rule name=\"QA SmartClass - VNC Client\"");
                    RunNetshCommand($"advfirewall firewall add rule name=\"QA SmartClass - VNC Client\" dir=in action=allow program=\"{vncClientPath}\" enable=yes profile=any protocol=TCP description=\"Allow VNC Client\"");

                    RunNetshCommand("advfirewall firewall delete rule name=\"QA SmartClass - UDP Multicast\"");
                    RunNetshCommand("advfirewall firewall add rule name=\"QA SmartClass - UDP Multicast\" dir=in action=allow protocol=UDP localport=8088 enable=yes profile=any description=\"Allow UDP Multicast\"");
                    
                    Log.Information("[Firewall] Tự động đăng ký luật tường lửa thành công.");
                }
                catch (Exception ex)
                {
                    Log.Warning("[Firewall] Đăng ký luật tường lửa tự động lỗi: {Msg}", ex.Message);
                }
            });
        }

        private void RunNetshCommand(string args)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                using (var proc = System.Diagnostics.Process.Start(startInfo))
                {
                    proc?.WaitForExit(3000);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Firewall] RunNetshCommand error: {Msg}", ex.Message);
            }
        }

        private class FrameAssembly
        {
            public DateTime CreatedAt { get; } = DateTime.UtcNow;
            public long Timestamp { get; }
            public short TotalSlices { get; }
            private readonly byte[][] _slices;
            private int _slicesReceived = 0;

            public FrameAssembly(short totalSlices, long timestamp)
            {
                TotalSlices = totalSlices;
                Timestamp = timestamp;
                _slices = new byte[totalSlices][];
            }

            public int ReceivedSlicesCount => _slicesReceived;

            public bool AddSlice(short sliceIndex, byte[] data)
            {
                if (sliceIndex < 0 || sliceIndex >= TotalSlices) return false;
                
                lock (_slices)
                {
                    if (_slices[sliceIndex] == null)
                    {
                        _slices[sliceIndex] = data;
                        _slicesReceived++;
                    }
                    return _slicesReceived == TotalSlices;
                }
            }

            public byte[] Assemble()
            {
                lock (_slices)
                {
                    int totalLength = 0;
                    for (int i = 0; i < TotalSlices; i++)
                    {
                        totalLength += _slices[i]?.Length ?? 0;
                    }

                    byte[] result = new byte[totalLength];
                    int offset = 0;
                    for (int i = 0; i < TotalSlices; i++)
                    {
                        if (_slices[i] != null)
                        {
                            Buffer.BlockCopy(_slices[i], 0, result, offset, _slices[i].Length);
                            offset += _slices[i].Length;
                        }
                    }
                    return result;
                }
            }
        }
    }
}
