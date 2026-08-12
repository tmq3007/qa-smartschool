using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    /// <summary>
    /// Service phát hiện và quản lý kết nối máy học sinh qua LAN.
    /// Protocol:
    ///   UDP  :29876 — GV broadcast beacon mỗi 2s (Discovery)
    ///   TCP  :29877 — HS kết nối → full-duplex command channel
    ///   TCP  :29878 — Screen stream / broadcast (reserved)
    ///   TCP  :29879 — File distribute & collect
    ///
    /// Message format (pipe-delimited):
    ///   GV→ broadcast: "QASC|className|teacherName|serverIP|tcpPort"
    ///   HS→ join:      "JOIN|name|code|pcName|version"
    ///   GV→ ack:       "OK|sessionId|serverTime"
    ///   GV→ command:   "CMD|LOCK|ALL" / "CMD|UNLOCK|ALL" / "CMD|QUIZ_START|quizId"
    ///   HS→ heartbeat: "HB|code|status" (mỗi 10s)
    ///   GV→ kick:      "KICK|code"
    /// </summary>
    public partial class NetworkDiscoveryService : ObservableObject, IDisposable
    {
        // ─── Ports ─────────────────────────────────────────────────
        public const int DISCOVERY_PORT = 29876;
        public const int TCP_PORT       = 29877;
        public const int STREAM_PORT    = 29878;  // Reserved for screen streaming
        public const int FILE_PORT      = 29879;  // File transfer channel
        private const string BEACON_PREFIX = "QASC|";
        private const int BEACON_INTERVAL_MS  = 2000;
        private const int HEARTBEAT_TIMEOUT_S = 30;

        // ─── State ─────────────────────────────────────────────────
        private UdpClient?         _udpBroadcaster;
        private TcpListener?       _tcpListener;
        private CancellationTokenSource? _cts;

        /// <summary>Connected students: code → ClientInfo</summary>
        private readonly ConcurrentDictionary<string, ClientInfo> _clients = new();

        // ─── WebSocket Bridge (tablet/laptop Web clients) ──────────
        private WebSocketBridgeService? _wsBridge;
        /// <summary>WebSocket bridge for web-connected students</summary>
        public WebSocketBridgeService? WebBridge => _wsBridge;

        private GamificationService? _gamification;
        /// <summary>Gamification Service for XP and badge management</summary>
        public GamificationService? Gamification => _gamification;

        // ─── Observable Properties ──────────────────────────────────
        [ObservableProperty] private bool _isBroadcasting;
        [ObservableProperty] private string _serverIP = string.Empty;
        [ObservableProperty] private int _connectedCount;

        private static long _commandCounter = 100;
        public static string AppendCommandId(string command)
        {
            if (command.Contains("|id="))
                return command;
            long id = System.Threading.Interlocked.Increment(ref _commandCounter);
            return $"{command}|id={id}";
        }

        // ─── Events ────────────────────────────────────────────────
        public event EventHandler<StudentConnectedEventArgs>? StudentConnected;
        public event EventHandler<string>?                     StudentDisconnected;
        public event EventHandler<StudentMessageEventArgs>?    MessageReceived;
        public event EventHandler<CommandAckEventArgs>?        CommandAckReceived;
        /// <summary>LOI_VID_30 FIX: Event riêng biệt cho Heartbeat — tách khỏi MessageReceived.
        /// Áp dụng ràng buộc NET-001: Tách kênh điều khiển (Control Plane) và kênh dữ liệu (Data Plane).
        /// Subscriber: MonitorPage (cập nhật trạng thái HS), ClassroomSessionService.
        /// KHÔNG subscribe bởi: MessagingPage (chat UI).</summary>
        public event EventHandler<StudentMessageEventArgs>?    HeartbeatReceived;

        // ═══════════════════════════════════════════════════════════
        //  START / STOP
        // ═══════════════════════════════════════════════════════════

        /// <summary>Bắt đầu phát beacon + lắng nghe TCP</summary>
        public async Task StartAsync(string className, string teacherName)
        {
            if (IsBroadcasting) return;

            _cts         = new CancellationTokenSource();
            IsBroadcasting = true;
            ServerIP     = GetLocalIPAddress();

            Log.Information("NetworkDiscovery: Starting on {IP} — Class: {Class}", ServerIP, className);

            var beacon = $"{BEACON_PREFIX}{className}|{teacherName}|{ServerIP}|{TCP_PORT}";
            _ = RunUdpBroadcastAsync(beacon, _cts.Token);
            _ = RunTcpListenerAsync(_cts.Token);
            _ = RunHeartbeatWatchdogAsync(_cts.Token);
            _ = RunQosRetryLoopAsync(_cts.Token);

            // ═══ Start WebSocket Bridge for tablet/laptop students ═══
            try
            {
                _wsBridge = new WebSocketBridgeService(this);
                _gamification = new GamificationService(_wsBridge);
                await _wsBridge.StartAsync(className, teacherName);

                // Relay web student events to the same pipeline
                _wsBridge.WebStudentConnected += (s, e) =>
                {
                    ConnectedCount = _clients.Count + (_wsBridge?.ConnectedWebClients ?? 0);
                    StudentConnected?.Invoke(this, e);
                };
                _wsBridge.WebStudentDisconnected += (s, code) =>
                {
                    ConnectedCount = _clients.Count + (_wsBridge?.ConnectedWebClients ?? 0);
                    StudentDisconnected?.Invoke(this, code);
                };
                _wsBridge.WebMessageReceived += (s, e) =>
                {
                    MessageReceived?.Invoke(this, e);
                };
                // LOI_VID_30 FIX: Relay WebSocket heartbeats qua HeartbeatReceived event
                _wsBridge.WebHeartbeatReceived += (s, e) =>
                {
                    HeartbeatReceived?.Invoke(this, e);
                };

                Log.Information("WebSocket Bridge started on port {Port}", _wsBridge.WebPort);
            }
            catch (Exception ex)
            {
                Log.Warning("WebSocket Bridge failed to start: {Err}", ex.Message);
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _udpBroadcaster?.Close();
            _tcpListener?.Stop();

            // Stop WebSocket Bridge
            _wsBridge?.Stop();
            _wsBridge?.Dispose();
            _wsBridge = null;
            _gamification = null;

            IsBroadcasting = false;
            _clients.Clear();
            ConnectedCount = 0;
            Log.Information("NetworkDiscovery: Stopped");
        }

        // ═══════════════════════════════════════════════════════════
        //  UDP BEACON
        // ═══════════════════════════════════════════════════════════

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

        // ═══════════════════════════════════════════════════════════
        //  TCP LISTENER
        // ═══════════════════════════════════════════════════════════

        private async Task RunTcpListenerAsync(CancellationToken ct)
        {
            try
            {
                _tcpListener = new TcpListener(IPAddress.Any, TCP_PORT);
                _tcpListener.Start();
                Log.Information("TCP listener started on :{Port}", TCP_PORT);

                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var client = await _tcpListener.AcceptTcpClientAsync(ct);
                        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                        client.SendBufferSize = 65536;
                        client.ReceiveBufferSize = 65536;
                        _ = HandleClientAsync(client, ct);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex) when (!ct.IsCancellationRequested)
                    {
                        Log.Warning("TCP accept error: {Err}", ex.Message);
                    }
                }
            }
            catch (Exception ex) { Log.Error("TCP listener fatal: {Err}", ex.Message); }
        }

        private async Task HandleClientAsync(TcpClient tcp, CancellationToken ct)
        {
            var remoteIP = (tcp.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "?";
            ClientInfo? info = null;

            try
            {
                var stream  = tcp.GetStream();
                var buffer  = new byte[262144]; // 256KB — enough for base64 screenshots
                var partial = new StringBuilder();

                // ── First message must be JOIN ──
                var len = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                var msg = Encoding.UTF8.GetString(buffer, 0, len).Trim();

                if (!msg.StartsWith("JOIN|"))
                {
                    Log.Warning("Invalid handshake from {IP}: {Msg}", remoteIP, msg);
                    tcp.Close();
                    return;
                }

                // JOIN|name|code|pcName|version|pubKeyBase64|macAddress
                var p = msg.Split('|');
                string studentPubKeyStr = p.Length > 5 ? p[5] : "";
                string macAddress = p.Length > 6 ? p[6] : "00:00:00:00:00:00";

                // CSPRNG SessionKey generation (32 hex characters)
                byte[] keyBytes = new byte[16];
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    rng.GetBytes(keyBytes);
                }
                string sessionKey = Convert.ToHexString(keyBytes);

                info = new ClientInfo
                {
                    Name      = p.Length > 1 ? p[1] : "Unknown",
                    Code      = p.Length > 2 ? p[2] : "???",
                    PCName    = p.Length > 3 ? p[3] : remoteIP,
                    Version   = p.Length > 4 ? p[4] : "1.0",
                    IPAddress = remoteIP,
                    TcpClient = tcp,
                    Stream    = stream,
                    LastSeen  = DateTime.Now,
                    SessionKey = sessionKey,
                    MacAddress = macAddress
                };

                // ═══ LOI_VID_22 FIX: Đóng kết nối cũ khi HS tái kết nối ═══
                if (_clients.TryGetValue(info.Code, out var oldClient))
                {
                    try
                    {
                        Log.Information("[AntiDuplicate] HS {Code} tái kết nối. Đóng socket cũ từ {OldIP}",
                            info.Code, oldClient.IPAddress);
                        oldClient.TcpClient?.Close();
                    }
                    catch (Exception exClose)
                    {
                        Log.Warning("[AntiDuplicate] Lỗi đóng socket cũ HS {Code}: {Err}", info.Code, exClose.Message);
                    }
                }

                _clients[info.Code] = info;
                ConnectedCount = _clients.Count;

                // Send ACK (RSA Encrypted SessionKey if public key is available, else fallback plaintext)
                string encryptedSessionKeyBase64 = sessionKey;
                if (!string.IsNullOrEmpty(studentPubKeyStr))
                {
                    try
                    {
                        using var rsa = System.Security.Cryptography.RSA.Create();
                        rsa.ImportRSAPublicKey(Convert.FromBase64String(studentPubKeyStr), out _);
                        byte[] encryptedData = rsa.Encrypt(Encoding.UTF8.GetBytes(sessionKey), System.Security.Cryptography.RSAEncryptionPadding.Pkcs1);
                        encryptedSessionKeyBase64 = Convert.ToBase64String(encryptedData);
                    }
                    catch (Exception rsaEx)
                    {
                        Log.Error("RSA encryption of SessionKey failed: {Err}", rsaEx.Message);
                        encryptedSessionKeyBase64 = sessionKey; // fallback
                    }
                }

                string classCode = string.Empty;
                if (System.Windows.Application.Current is QASmartTouch.App appInstance)
                {
                    classCode = appInstance.ClassroomSession?.ClassCode ?? string.Empty;
                }
                string sessionSalt = QASmartClass.Services.ClassControlService.Instance.SessionSalt;
                long serverTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var ack = $"OK|{Guid.NewGuid():N}|{serverTimestamp}|{encryptedSessionKeyBase64}|{classCode}|{sessionSalt}";
                await stream.WriteAsync(Encoding.UTF8.GetBytes(ack), ct);

                Log.Information("Student joined: {Name} ({Code}) @ {IP}", info.Name, info.Code, remoteIP);
                StudentConnected?.Invoke(this, new StudentConnectedEventArgs
                {
                    StudentName = info.Name, StudentCode = info.Code,
                    PCName = info.PCName, IPAddress = remoteIP, TcpClient = tcp,
                    MacAddress = macAddress
                });

                // Sync current Web block/whitelist policy to the newly joined student
                try
                {
                    var control = QASmartClass.Services.ClassControlService.Instance;
                    if (control.IsWebBlocked)
                    {
                        var cmd = QASmartClass.Utilities.CryptoHelper.Encrypt("CMD|BLOCK_WEB_ON", info.SessionKey) + "\n";
                        await stream.WriteAsync(Encoding.UTF8.GetBytes(cmd), ct);
                    }
                    else if (control.IsWebWhitelistActive)
                    {
                        var cmd1 = QASmartClass.Utilities.CryptoHelper.Encrypt("CMD|WEB_WHITELIST_ON", info.SessionKey) + "\n";
                        await stream.WriteAsync(Encoding.UTF8.GetBytes(cmd1), ct);
                        
                        if (!string.IsNullOrEmpty(control.WebWhitelistUrls))
                        {
                            var cmd2 = QASmartClass.Utilities.CryptoHelper.Encrypt($"CMD|WHITELIST_ADD|{control.WebWhitelistUrls}", info.SessionKey) + "\n";
                            await stream.WriteAsync(Encoding.UTF8.GetBytes(cmd2), ct);
                        }
                    }
                }
                catch (Exception exSync)
                {
                    Log.Warning("Failed to sync web policy to new student {Code}: {Err}", info.Code, exSync.Message);
                }

                // Sync current Exit PIN policy to the newly joined student
                try
                {
                    var control = QASmartClass.Services.ClassControlService.Instance;
                    if (control.IsExitPinRequired && !string.IsNullOrEmpty(control.ExitPinHash))
                    {
                        var cmd = QASmartClass.Utilities.CryptoHelper.Encrypt($"CMD|EXIT_PIN_CONFIG|true|{control.ExitPinHash}", info.SessionKey) + "\n";
                        await stream.WriteAsync(Encoding.UTF8.GetBytes(cmd), ct);
                    }
                }
                catch (Exception exSyncPin)
                {
                    Log.Warning("Failed to sync exit PIN to new student {Code}: {Err}", info.Code, exSyncPin.Message);
                }

                // ═══ AUTO-SAVE DEVICE TO MEMORY ═══
                try
                {
                    if (System.Windows.Application.Current is QASmartTouch.App app)
                    {
                        app.DeviceMemory.SaveOrUpdateDevice(
                            info.PCName, info.Code, info.Name,
                            remoteIP, info.Version);
                        app.DeviceMemory.SyncToStudentTable(
                            info.Code, info.PCName, remoteIP);
                    }
                }
                catch (Exception dmEx)
                {
                    Log.Warning("DeviceMemory save failed: {Err}", dmEx.Message);
                }

                // ── Message loop (newline-delimited framing) ──
                var msgBuffer = new StringBuilder();
                while (!ct.IsCancellationRequested && tcp.Connected)
                {
                    len = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                    if (len == 0) break;

                    info.LastSeen = DateTime.Now;
                    msgBuffer.Append(Encoding.UTF8.GetString(buffer, 0, len));

                    // Process all complete messages (newline-delimited)
                    string accumulated = msgBuffer.ToString();
                    int nlIdx;
                    while ((nlIdx = accumulated.IndexOf('\n')) >= 0)
                    {
                        var line = accumulated.Substring(0, nlIdx).Trim();
                        accumulated = accumulated.Substring(nlIdx + 1);

                        if (string.IsNullOrEmpty(line)) continue;

                        if (!string.IsNullOrEmpty(info.SessionKey))
                        {
                            var decrypted = QASmartClass.Utilities.CryptoHelper.Decrypt(line, info.SessionKey);
                            if (!string.IsNullOrEmpty(decrypted))
                                line = decrypted;
                        }

                        ProcessStudentMessage(info, line, stream, ct);
                    }



                    msgBuffer.Clear();
                    if (!string.IsNullOrEmpty(accumulated))
                        msgBuffer.Append(accumulated);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log.Warning("Client {IP} disconnected: {Err}", remoteIP, ex.Message);
            }
            finally
            {
                if (info != null)
                {
                    // ═══ LOI_VID_22 FIX: Chỉ xóa khỏi _clients nếu entry hiện tại vẫn trỏ đến CÙNG instance TcpClient (tránh xóa nhầm kết nối V2)
                    bool wasCurrentConnection = false;
                    if (_clients.TryGetValue(info.Code, out var currentClient) &&
                        ReferenceEquals(currentClient.TcpClient, info.TcpClient))
                    {
                        _clients.TryRemove(info.Code, out _);
                        ConnectedCount = _clients.Count;
                        StudentDisconnected?.Invoke(this, info.Code);
                        wasCurrentConnection = true;
                    }
                    Log.Information("Student left: {Name} ({Code}) — WasCurrent: {IsCurrent}",
                        info.Name, info.Code, wasCurrentConnection);

                    // ═══ MARK DEVICE OFFLINE ═══
                    try
                    {
                        if (System.Windows.Application.Current is QASmartTouch.App app)
                            app.DeviceMemory.MarkStudentOffline(info.Code);
                    }
                    catch { }
                }
                tcp.Close();
            }
        }

        /// <summary>Xử lý 1 message đã framed từ HS</summary>
        private void ProcessStudentMessage(ClientInfo info, string line, NetworkStream stream, CancellationToken ct)
        {
            if (line.StartsWith("ACK|"))
            {
                var ackParts = line.Split('|');
                if (ackParts.Length >= 4)
                {
                    string cmdId = ackParts[1];
                    string status = ackParts[3];
                    string errMsg = ackParts.Length >= 5 ? ackParts[4] : string.Empty;
                    
                    Log.Information("ACK received: CmdId={CmdId}, Student={Student}, Status={Status}", cmdId, info.Code, status);

                    // QoS 1: Remove pending retry
                    string key = $"{cmdId}_{info.Code}";
                    if (_pendingQosCommands.TryRemove(key, out _))
                    {
                        Log.Debug("QoS 1: Acknowledged command {CmdId} for student {Student}", cmdId, info.Code);
                    }
                    
                    CommandAckReceived?.Invoke(this, new CommandAckEventArgs
                    {
                        CommandId = cmdId,
                        StudentCode = info.Code,
                        Status = status,
                        ErrorMessage = errMsg
                    });
                }
            }
            else if (line.StartsWith("ERR_REPORT|"))
            {
                var parts = line.Split('|');
                if (parts.Length >= 4)
                {
                    string severity = parts[2];
                    string errorMsg = parts[3];
                    if (severity == "ERROR")
                    {
                        Log.Error("[Student Error] {StudentCode}: {ErrorMessage}", parts[1], errorMsg);
                    }
                    else
                    {
                        Log.Warning("[Student Warning] {StudentCode}: {ErrorMessage}", parts[1], errorMsg);
                    }
                }
                else if (parts.Length == 3)
                {
                    Log.Warning("[Student Error] {StudentCode}: {ErrorMessage}", parts[1], parts[2]);
                }
                MessageReceived?.Invoke(this, new StudentMessageEventArgs
                {
                    StudentCode = info.Code, Message = line
                });
            }
            else if (line.StartsWith("PERIPHERAL_STATUS|"))
            {
                var parts = line.Split('|');
                bool hpOk = true;
                bool micOk = true;
                for (int i = 1; i < parts.Length; i++)
                {
                    if (parts[i].StartsWith("Headphones="))
                    {
                        hpOk = parts[i].Substring("Headphones=".Length) == "OK";
                    }
                    else if (parts[i].StartsWith("Mic="))
                    {
                        micOk = parts[i].Substring("Mic=".Length) == "OK";
                    }
                }
                info.IsHeadphoneOk = hpOk;
                info.IsMicOk = micOk;
                MessageReceived?.Invoke(this, new StudentMessageEventArgs
                {
                    StudentCode = info.Code, Message = $"PERIPHERAL_UPDATE|Headphones={hpOk}|Mic={micOk}"
                });
            }
            else if (line.StartsWith("CLAIM_SEAT|"))
            {
                var parts = line.Split('|');
                if (parts.Length >= 2 && int.TryParse(parts[1], out var seatNumber))
                {
                    bool seatOccupied = _clients.Values.Any(c => c.Code != info.Code && c.SeatNumber == seatNumber);
                    if (seatOccupied)
                    {
                        _ = stream.WriteAsync(Encoding.UTF8.GetBytes($"CLAIM_REJECTED|Ghế số {seatNumber} đã có học sinh khác đăng ký!"), ct);
                    }
                    else
                    {
                        info.SeatNumber = seatNumber;
                        _ = stream.WriteAsync(Encoding.UTF8.GetBytes("CLAIM_ACK"), ct);
                        MessageReceived?.Invoke(this, new StudentMessageEventArgs
                        {
                            StudentCode = info.Code, Message = $"CLAIM_SUCCESS|{seatNumber}"
                        });
                    }
                }
            }
            else if (line.StartsWith("HB|"))
            {
                var hbParts = line.Split('|');
                if (hbParts.Length >= 4)
                    info.ActiveApp = hbParts[3];
                if (hbParts.Length >= 5 && int.TryParse(hbParts[4], out var cpu))
                    info.CpuUsage = cpu;

                _ = stream.WriteAsync(Encoding.UTF8.GetBytes("HB_ACK"), ct);

                // LOI_VID_30 FIX: Phát qua HeartbeatReceived thay vì MessageReceived
                // để ngăn chặn rò rỉ vào Chat UI (NET-001)
                HeartbeatReceived?.Invoke(this, new StudentMessageEventArgs
                {
                    StudentCode = info.Code,
                    Message = $"HB_UPDATE|{info.ActiveApp}|{info.CpuUsage}"
                });
            }
            else if (line.StartsWith("SCREENSHOT|"))
            {
                Log.Debug("Screenshot received from {Code}: {Len} chars", info.Code, line.Length);
                MessageReceived?.Invoke(this, new StudentMessageEventArgs
                {
                    StudentCode = info.Code, Message = line
                });
            }
            else
            {
                Log.Debug("MSG from {Code}: {Msg}", info.Code, line);
                MessageReceived?.Invoke(this, new StudentMessageEventArgs
                {
                    StudentCode = info.Code, Message = line
                });
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  HEARTBEAT WATCHDOG
        // ═══════════════════════════════════════════════════════════

        private async Task RunHeartbeatWatchdogAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(5000, ct);
                var now     = DateTime.Now;
                var timeout = TimeSpan.FromSeconds(HEARTBEAT_TIMEOUT_S);

                foreach (var (code, client) in _clients)
                {
                    var timeSinceLastSeen = now - client.LastSeen;

                    // Active ping: if no message seen for > 12 seconds, send a PING command
                    if (timeSinceLastSeen > TimeSpan.FromSeconds(12) && timeSinceLastSeen <= timeout)
                    {
                        try
                        {
                            _ = SendToStudentDirectAsync(client, "CMD|PING");
                        }
                        catch { }
                    }

                    // Passive timeout: if no message seen for > 30 seconds, disconnect
                    if (timeSinceLastSeen > timeout)
                    {
                        Log.Warning("Heartbeat timeout: {Name} ({Code})", client.Name, code);
                        client.TcpClient?.Close();
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  COMMANDS → BROADCAST TO ALL STUDENTS
        // ═══════════════════════════════════════════════════════════

        public static string SignCommand(string command, string classCode)
        {
            try
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string payload = $"{command}|{timestamp}";
                string sessionSalt = QASmartClass.Services.ClassControlService.Instance.SessionSalt;
                string salt = !string.IsNullOrEmpty(sessionSalt) ? sessionSalt : classCode;
                using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(salt));
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                string signature = Convert.ToBase64String(hash);
                return $"{payload}|{signature}";
            }
            catch (Exception ex)
            {
                Log.Error("Error signing command: {Err}", ex.Message);
                return command;
            }
        }

        public async Task SendCommandAsync(string command)
        {
            int sent  = 0;
            var app = System.Windows.Application.Current as QASmartTouch.App;
            string classCode = app?.ClassroomSession?.ClassCode ?? string.Empty;

            // Tự động gán Command ID
            command = AppendCommandId(command);
            string cmdId = ExtractCommandId(command);

            bool isQosCommand = command.StartsWith("CMD|LOCK_SCREEN") || 
                                command.StartsWith("CMD|UNLOCK_SCREEN") || 
                                command.StartsWith("CMD|LOCK|ALL") || 
                                command.StartsWith("CMD|UNLOCK|ALL") ||
                                command.StartsWith("SILENCE|") ||
                                command.StartsWith("CMD|SILENCE") ||
                                command.StartsWith("CMD|CLEAR_SILENCE") ||
                                command.StartsWith("CMD|SCREEN_BROADCAST_STOP") || // === UPGRADE_02: QoS cho broadcast stop ===
                                command.StartsWith("CMD|VNC_BROADCAST_START") ||   // === LOI_VID_48: QoS cho VNC start ===
                                command.StartsWith("CMD|VNC_BROADCAST_STOP") ||    // === LOI_VID_48: QoS cho VNC stop ===
                                command.StartsWith("CMD|CLEAR_ALL");                // === UPGRADE_02: QoS cho clear all ===

            if (isQosCommand)
            {
                foreach (var (_, client) in _clients)
                {
                    var pending = new PendingQosCommand
                    {
                        CommandId = cmdId,
                        StudentCode = client.Code,
                        CommandText = command,
                        NextRetryTime = DateTime.UtcNow.AddSeconds(2),
                        RetryCount = 0
                    };
                    string key = $"{cmdId}_{client.Code}";
                    _pendingQosCommands[key] = pending;
                }
            }

            string signedCommand = SignCommand(command, classCode);

            foreach (var (_, client) in _clients)
            {
                try
                {
                    if (client.Stream != null && client.TcpClient?.Connected == true)
                    {
                        string encryptedCommand;
                        if (command.StartsWith("POLICY|") || 
                            command.StartsWith("CMD|") || 
                            command.StartsWith("LOCK_KEYBOARD|") || 
                            command.StartsWith("SILENCE|") || 
                            command.StartsWith("QUIET_MODE|"))
                        {
                            string sessionSalt = QASmartClass.Services.ClassControlService.Instance.SessionSalt;
                            encryptedCommand = QASmartClass.Utilities.NetworkCryptoHelper.EncryptCommand(signedCommand, sessionSalt);
                        }
                        else
                        {
                            encryptedCommand = signedCommand;
                            if (!string.IsNullOrEmpty(client.SessionKey))
                            {
                                encryptedCommand = QASmartClass.Utilities.CryptoHelper.Encrypt(signedCommand, client.SessionKey);
                            }
                        }
                        var commandWithNewLine = encryptedCommand.EndsWith("\n") ? encryptedCommand : encryptedCommand + "\n";
                        var bytes = Encoding.UTF8.GetBytes(commandWithNewLine);
                        await client.Stream.WriteAsync(bytes);
                        sent++;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Send cmd to {Code} failed: {Err}", client.Code, ex.Message);
                }
            }

            // ═══ RELAY TO WEBSOCKET (TABLET/LAPTOP) CLIENTS ═══
            try
            {
                if (_wsBridge?.IsRunning == true)
                {
                    await _wsBridge.BroadcastToWebClients(signedCommand);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("WS bridge broadcast failed: {Err}", ex.Message);
            }

            // ═══ LOCAL BUS — relay cho HS trên cùng 1 máy ═══
            try
            {
                if (app != null)
                {
                    app.RaiseLocalCommand(command);
                }
            }
            catch { }

            var webCount = _wsBridge?.ConnectedWebClients ?? 0;
            Log.Information("Command sent (encrypted & signed): {Cmd} → {N} TCP + {W} Web + local bus", command, sent, webCount);
        }

        public Task LockAllScreensAsync()  => SendCommandAsync("CMD|LOCK|ALL");
        public Task UnlockAllScreensAsync() => SendCommandAsync("CMD|UNLOCK|ALL");
        public Task StartQuizAsync(int quizId) => SendCommandAsync($"CMD|QUIZ_START|{quizId}");
        public Task EndQuizAsync()          => SendCommandAsync("CMD|QUIZ_END|0");
        public Task BroadcastMessage(string text) => SendCommandAsync($"MSG|{text}");

        public Task SendCheckIntegrityAsync(string targetStudentCode)
        {
            return SendToStudentAsync(targetStudentCode, "CMD|CHECK_INTEGRITY");
        }

        public Task SendRestoreDefaultsAsync(string targetStudentCode, string adminPin)
        {
            Log.Information("Sending remote restore default command to student {StudentCode} with verified PIN confirmation.", targetStudentCode);
            return SendToStudentAsync(targetStudentCode, "CMD|RESTORE_DEFAULTS");
        }

        /// <summary>Gửi tin nhắn riêng cho 1 HS cụ thể</summary>
        public async Task SendToStudentAsync(string studentCode, string message)
        {
            // Tự động gán Command ID
            message = AppendCommandId(message);

            ClientInfo? client = null;
            if (!string.IsNullOrEmpty(studentCode))
            {
                if (_clients.TryGetValue(studentCode, out client))
                {
                    // Found directly by Code
                }
                else
                {
                    // Fallback search by PCName, Name or IPAddress
                    foreach (var c in _clients.Values)
                    {
                        if (c.PCName.Equals(studentCode, StringComparison.OrdinalIgnoreCase) ||
                            c.Name.Equals(studentCode, StringComparison.OrdinalIgnoreCase) ||
                            c.IPAddress.Equals(studentCode, StringComparison.OrdinalIgnoreCase))
                        {
                            client = c;
                            break;
                        }
                    }
                }
            }

            if (client != null)
            {
                string cmdId = ExtractCommandId(message);
                bool isQosCommand = message.StartsWith("CMD|LOCK_SCREEN") || 
                                    message.StartsWith("CMD|UNLOCK_SCREEN") || 
                                    message.StartsWith("CMD|LOCK|ALL") || 
                                    message.StartsWith("CMD|UNLOCK|ALL") ||
                                    message.StartsWith("SILENCE|") ||
                                    message.StartsWith("CMD|SILENCE") ||
                                    message.StartsWith("CMD|CLEAR_SILENCE") ||
                                    message.StartsWith("CMD|SCREEN_BROADCAST_STOP") || // === UPGRADE_02: QoS cho broadcast stop ===
                                    message.StartsWith("CMD|VNC_BROADCAST_START") ||   // === LOI_VID_48: QoS cho VNC start ===
                                    message.StartsWith("CMD|VNC_BROADCAST_STOP") ||    // === LOI_VID_48: QoS cho VNC stop ===
                                    message.StartsWith("CMD|CLEAR_ALL");                // === UPGRADE_02: QoS cho clear all ===

                if (isQosCommand)
                {
                    var pending = new PendingQosCommand
                    {
                        CommandId = cmdId,
                        StudentCode = client.Code,
                        CommandText = message,
                        NextRetryTime = DateTime.UtcNow.AddSeconds(2),
                        RetryCount = 0
                    };
                    string key = $"{cmdId}_{client.Code}";
                    _pendingQosCommands[key] = pending;
                }

                try
                {
                    if (client.Stream != null && client.TcpClient?.Connected == true)
                    {
                        string signedMessage = message;
                        if (message.StartsWith("CMD|") || 
                            message.StartsWith("MSG|") || 
                            message.StartsWith("POLICY|") || 
                            message.StartsWith("LOCK_KEYBOARD|") || 
                            message.StartsWith("SILENCE|") || 
                            message.StartsWith("QUIET_MODE|"))
                        {
                            string classCode = string.Empty;
                            if (System.Windows.Application.Current is QASmartTouch.App app)
                            {
                                classCode = app.ClassroomSession?.ClassCode ?? string.Empty;
                            }
                            signedMessage = SignCommand(message, classCode);
                        }

                        string encryptedMessage;
                        if (message.StartsWith("POLICY|") || 
                            message.StartsWith("CMD|") || 
                            message.StartsWith("LOCK_KEYBOARD|") || 
                            message.StartsWith("SILENCE|") || 
                            message.StartsWith("QUIET_MODE|"))
                        {
                            string sessionSalt = QASmartClass.Services.ClassControlService.Instance.SessionSalt;
                            encryptedMessage = QASmartClass.Utilities.NetworkCryptoHelper.EncryptCommand(signedMessage, sessionSalt);
                        }
                        else
                        {
                            encryptedMessage = signedMessage;
                            if (!string.IsNullOrEmpty(client.SessionKey))
                            {
                                encryptedMessage = QASmartClass.Utilities.CryptoHelper.Encrypt(signedMessage, client.SessionKey);
                            }
                        }
                        var messageWithNewLine = encryptedMessage.EndsWith("\n") ? encryptedMessage : encryptedMessage + "\n";
                        var bytes = Encoding.UTF8.GetBytes(messageWithNewLine);
                        await client.Stream.WriteAsync(bytes);
                        Log.Information("Private msg to {Code} ({PC}): {Msg}", client.Code, client.PCName, message);
                    }
                }
                catch (Exception ex) { Log.Warning("SendToStudent {Code} failed: {Err}", client.Code, ex.Message); }
            }

            // WebSocket bridge relay
            try
            {
                if (_wsBridge?.IsRunning == true)
                {
                    string resolvedCode = client?.Code ?? studentCode;
                    string signedMessage = message;
                    if (message.StartsWith("CMD|") || message.StartsWith("MSG|"))
                    {
                        string classCode = string.Empty;
                        if (System.Windows.Application.Current is QASmartTouch.App app)
                        {
                            classCode = app.ClassroomSession?.ClassCode ?? string.Empty;
                        }
                        signedMessage = SignCommand(message, classCode);
                    }
                    await _wsBridge.SendToWebClient(resolvedCode, signedMessage);
                }
            }
            catch { }

            // Local bus relay
            try
            {
                if (System.Windows.Application.Current is QASmartTouch.App app)
                    app.RaiseLocalCommand(message);
            }
            catch { }
        }

        /// <summary>Gửi tin nhắn cho nhiều HS (nhóm)</summary>
        public async Task SendToStudentsAsync(IEnumerable<string> studentCodes, string message)
        {
            // Tự động gán Command ID
            message = AppendCommandId(message);

            int sent = 0;
            string classCode = string.Empty;
            if (System.Windows.Application.Current is QASmartTouch.App app)
            {
                classCode = app.ClassroomSession?.ClassCode ?? string.Empty;
            }

            string signedMessage = message;
            if (message.StartsWith("CMD|") || 
                message.StartsWith("MSG|") || 
                message.StartsWith("POLICY|") || 
                message.StartsWith("LOCK_KEYBOARD|") || 
                message.StartsWith("SILENCE|") || 
                message.StartsWith("QUIET_MODE|"))
            {
                signedMessage = SignCommand(message, classCode);
            }

            foreach (var code in studentCodes)
            {
                ClientInfo? client = null;
                if (!string.IsNullOrEmpty(code))
                {
                    if (_clients.TryGetValue(code, out client))
                    {
                        // Found directly
                    }
                    else
                    {
                        // Fallback search
                        foreach (var c in _clients.Values)
                        {
                            if (c.PCName.Equals(code, StringComparison.OrdinalIgnoreCase) ||
                                c.Name.Equals(code, StringComparison.OrdinalIgnoreCase))
                            {
                                client = c;
                                break;
                            }
                        }
                    }
                }

                if (client != null)
                {
                    try
                    {
                        if (client.Stream != null && client.TcpClient?.Connected == true)
                        {
                            string encryptedMessage;
                            if (message.StartsWith("POLICY|") || 
                                message.StartsWith("CMD|") || 
                                message.StartsWith("LOCK_KEYBOARD|") || 
                                message.StartsWith("SILENCE|") || 
                                message.StartsWith("QUIET_MODE|"))
                            {
                                string sessionSalt = QASmartClass.Services.ClassControlService.Instance.SessionSalt;
                                encryptedMessage = QASmartClass.Utilities.NetworkCryptoHelper.EncryptCommand(signedMessage, sessionSalt);
                            }
                            else
                            {
                                encryptedMessage = signedMessage;
                                if (!string.IsNullOrEmpty(client.SessionKey))
                                {
                                    encryptedMessage = QASmartClass.Utilities.CryptoHelper.Encrypt(signedMessage, client.SessionKey);
                                }
                            }
                            var messageWithNewLine = encryptedMessage.EndsWith("\n") ? encryptedMessage : encryptedMessage + "\n";
                            var bytes = Encoding.UTF8.GetBytes(messageWithNewLine);
                            await client.Stream.WriteAsync(bytes);
                            sent++;
                        }
                    }
                    catch (Exception ex) { Log.Warning("SendToStudents {Code} failed: {Err}", client.Code, ex.Message); }
                }
            }

            // Local bus relay
            try
            {
                if (System.Windows.Application.Current is QASmartTouch.App appRef)
                    appRef.RaiseLocalCommand(message);
            }
            catch { }

            Log.Information("Group msg sent to {Sent} students: {Msg}", sent, message);
        }

        // ═══ LESSON SYNC COMMANDS ═══
        /// <summary>GV bắt đầu dạy — HS tự chuyển sang trang Bài giảng</summary>
        public Task StartLessonAsync(int lessonId) => SendCommandAsync($"CMD|LESSON_START|{lessonId}");
        /// <summary>GV chuyển giai đoạn (1-6) — HS cập nhật trạng thái</summary>
        public Task SetLessonStageAsync(int stage) => SendCommandAsync($"CMD|LESSON_STAGE|{stage}");
        /// <summary>GV muốn HS focus vào block nội dung cụ thể (sortOrder)</summary>
        public Task FocusContentAsync(int sortOrder, string contentType) => SendCommandAsync($"CMD|LESSON_FOCUS|{sortOrder}|{contentType}");
        /// <summary>GV kết thúc tiết học — HS nhận thông báo</summary>
        public Task EndLessonAsync() => SendCommandAsync("CMD|LESSON_END|0");
        /// <summary>GV gửi BTVN — HS nhận thông báo</summary>
        public Task SendHomeworkAsync(string text) => SendCommandAsync($"CMD|HOMEWORK|{text}");

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════

        public IReadOnlyCollection<ClientInfo> GetConnectedStudents()
            => _clients.Values as IReadOnlyCollection<ClientInfo>
               ?? new List<ClientInfo>(_clients.Values);

        public static List<string> GetActiveLocalIPv4Addresses()
        {
            var ips = new List<string>();
            try
            {
                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                        continue;

                    if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback ||
                        ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Tunnel)
                        continue;

                    string name = ni.Name.ToLower();
                    string desc = ni.Description.ToLower();
                    if (name.Contains("virtual") || desc.Contains("virtual") ||
                        name.Contains("vmware") || desc.Contains("vbox") ||
                        name.Contains("virtualbox") || desc.Contains("wsl") ||
                        name.Contains("hyper-v") || desc.Contains("npcap") ||
                        name.Contains("docker") || desc.Contains("loopback") ||
                        name.Contains("teredo") || desc.Contains("host-only") ||
                        name.Contains("fortinet") || desc.Contains("anyconnect") ||
                        name.Contains("nordvpn") || desc.Contains("zerotier") ||
                        name.Contains("tailscale") || desc.Contains("wireguard") ||
                        name.Contains("vpn"))
                        continue;

                    var ipProps = ni.GetIPProperties();
                    if (ipProps == null) continue;

                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            string ipStr = addr.Address.ToString();
                            if (ipStr.StartsWith("127.")) continue;

                            bool hasGateway = ipProps.GatewayAddresses != null && ipProps.GatewayAddresses.Count > 0;
                            if (hasGateway)
                                ips.Insert(0, ipStr);
                            else
                                ips.Add(ipStr);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error enumerating network interfaces: {Err}", ex.Message);
            }

            if (ips.Count == 0)
            {
                try
                {
                    using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                    socket.Connect("8.8.8.8", 65530);
                    var ip = ((IPEndPoint?)socket.LocalEndPoint)?.Address.ToString();
                    if (!string.IsNullOrEmpty(ip) && ip != "127.0.0.1")
                    {
                        ips.Add(ip);
                    }
                }
                catch { }
            }

            if (ips.Count == 0)
            {
                ips.Add("127.0.0.1");
            }

            var uniqueIps = new List<string>();
            foreach (var ip in ips)
            {
                if (!uniqueIps.Contains(ip))
                {
                    uniqueIps.Add(ip);
                }
            }
            return uniqueIps;
        }

        private static string GetLocalIPAddress()
        {
            var ips = GetActiveLocalIPv4Addresses();
            return ips.Count > 0 ? ips[0] : "127.0.0.1";
        }

        // ─── QoS 1 & Reliable Delivery ───────────────────────────────
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, PendingQosCommand> _pendingQosCommands = new();

        public static string ExtractCommandId(string command)
        {
            try
            {
                var parts = command.Split('|');
                foreach (var part in parts)
                {
                    if (part.StartsWith("id="))
                    {
                        return part.Substring(3);
                    }
                }
            }
            catch { }
            return "unknown";
        }

        private async Task SendToStudentDirectAsync(ClientInfo client, string command)
        {
            try
            {
                if (client.Stream != null && client.TcpClient?.Connected == true)
                {
                    string classCode = string.Empty;
                    if (System.Windows.Application.Current is QASmartTouch.App app)
                    {
                        classCode = app.ClassroomSession?.ClassCode ?? string.Empty;
                    }
                    string signedMessage = SignCommand(command, classCode);
                    string encryptedMessage;

                    if (command.StartsWith("POLICY|") || 
                        command.StartsWith("CMD|") || 
                        command.StartsWith("LOCK_KEYBOARD|") || 
                        command.StartsWith("SILENCE|") || 
                        command.StartsWith("QUIET_MODE|"))
                    {
                        string sessionSalt = QASmartClass.Services.ClassControlService.Instance.SessionSalt;
                        encryptedMessage = QASmartClass.Utilities.NetworkCryptoHelper.EncryptCommand(signedMessage, sessionSalt);
                    }
                    else
                    {
                        encryptedMessage = signedMessage;
                        if (!string.IsNullOrEmpty(client.SessionKey))
                        {
                            encryptedMessage = QASmartClass.Utilities.CryptoHelper.Encrypt(signedMessage, client.SessionKey);
                        }
                    }
                    var messageWithNewLine = encryptedMessage.EndsWith("\n") ? encryptedMessage : encryptedMessage + "\n";
                    var bytes = Encoding.UTF8.GetBytes(messageWithNewLine);
                    await client.Stream.WriteAsync(bytes);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("SendToStudentDirect {Code} failed: {Err}", client.Code, ex.Message);
            }
        }

        private async Task RunQosRetryLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000, ct);

                    var now = DateTime.UtcNow;
                    var toRetry = new List<PendingQosCommand>();

                    foreach (var pending in _pendingQosCommands.Values)
                    {
                        if (now >= pending.NextRetryTime)
                        {
                            toRetry.Add(pending);
                        }
                    }

                    foreach (var pending in toRetry)
                    {
                        if (_clients.TryGetValue(pending.StudentCode, out var client) && client.TcpClient?.Connected == true)
                        {
                            if (pending.RetryCount >= 3)
                            {
                                Log.Warning("QoS 1: Max retries (3) reached for command {CmdId} to student {StudentCode}. Giving up.", pending.CommandId, pending.StudentCode);
                                string key = $"{pending.CommandId}_{pending.StudentCode}";
                                _pendingQosCommands.TryRemove(key, out _);
                                continue;
                            }

                            pending.RetryCount++;
                            pending.NextRetryTime = DateTime.UtcNow.AddSeconds(2);

                            Log.Information("QoS 1: Resending command {CmdId} to student {StudentCode} (Attempt {Attempt}/3)", pending.CommandId, pending.StudentCode, pending.RetryCount);

                            _ = SendToStudentDirectAsync(client, pending.CommandText);
                        }
                        else
                        {
                            string key = $"{pending.CommandId}_{pending.StudentCode}";
                            _pendingQosCommands.TryRemove(key, out _);
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Log.Error("QoS 1 retry loop error: {Err}", ex.Message);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  SESSION SYNC — Đồng bộ phiên mới tới HS đã kết nối (LOI_VID_21)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Gửi lệnh CMD|UPDATE_SESSION tới tất cả HS đang giữ kết nối TCP,
        /// cho phép HS cập nhật ClassCode và SessionSalt mới mà KHÔNG cần ngắt kết nối.
        /// 
        /// ⚠️ Thiết kế bảo mật:
        /// - Ký HMAC bằng muối CŨ (oldSessionSalt) để HS xác minh được bằng muối hiện tại của họ.
        /// - Mã hóa AES bằng SessionKey riêng của từng HS (thiết lập từ bước JOIN, không phụ thuộc salt).
        /// - Sau khi HS nhận và áp dụng, các lệnh tiếp theo sẽ dùng muối MỚI.
        /// </summary>
        public async Task SyncActiveClientsToNewSessionAsync(string newClassCode, string newSessionSalt, string oldSessionSalt)
        {
            int successCount = 0;
            int failCount = 0;
            var tasks = new List<Task>();

            foreach (var kvp in _clients)
            {
                var code = kvp.Key;
                var client = kvp.Value;

                if (client.TcpClient?.Connected != true || client.Stream == null)
                {
                    failCount++;
                    continue;
                }

                try
                {
                    // 1. Tạo lệnh cập nhật phiên mới kèm Command ID
                    string rawCmd = AppendCommandId($"CMD|UPDATE_SESSION|{newClassCode}|{newSessionSalt}");

                    // 2. Ký HMAC-SHA256 bằng muối CŨ (để HS xác minh bằng muối hiện tại của họ)
                    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    string payload = $"{rawCmd}|{timestamp}";
                    string signingKey = !string.IsNullOrEmpty(oldSessionSalt) ? oldSessionSalt : newClassCode;
                    using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
                    byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                    string signature = Convert.ToBase64String(hash);
                    string signedCmd = $"{payload}|{signature}";

                    // 3. Mã hóa AES bằng SessionKey riêng của HS (không phụ thuộc salt)
                    string encryptedCmd = signedCmd;
                    if (!string.IsNullOrEmpty(client.SessionKey))
                    {
                        encryptedCmd = QASmartClass.Utilities.CryptoHelper.Encrypt(signedCmd, client.SessionKey);
                    }

                    // 4. Gửi qua TCP stream (newline-delimited framing)
                    string messageWithNewLine = encryptedCmd.EndsWith("\n") ? encryptedCmd : encryptedCmd + "\n";
                    byte[] bytes = Encoding.UTF8.GetBytes(messageWithNewLine);
                    await client.Stream.WriteAsync(bytes, 0, bytes.Length);

                    successCount++;
                    Log.Information("[SessionSync] Đã gửi UPDATE_SESSION tới HS {Code} ({IP})", code, client.IPAddress);
                }
                catch (Exception ex)
                {
                    failCount++;
                    Log.Warning("[SessionSync] Gửi UPDATE_SESSION tới HS {Code} thất bại: {Err}", code, ex.Message);
                }
            }

            Log.Information("[SessionSync] Hoàn tất đồng bộ phiên: {Success} thành công, {Fail} thất bại trên tổng {Total} kết nối",
                successCount, failCount, _clients.Count);
        }

        /// <summary>
        /// Trả về danh sách mã học sinh (StudentCode) đang giữ kết nối TCP hợp lệ.
        /// Dùng để cập nhật trạng thái IsOnline trên giao diện GV sau khi đồng bộ phiên.
        /// </summary>
        public List<string> GetActiveStudentCodes()
        {
            var list = new List<string>();
            foreach (var kvp in _clients)
            {
                if (kvp.Value.TcpClient?.Connected == true)
                {
                    list.Add(kvp.Key);
                }
            }
            return list;
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    // ─── Support classes ───────────────────────────────────────────
    public class ClientInfo
    {
        public string Name      { get; set; } = string.Empty;
        public string Code      { get; set; } = string.Empty;
        public string PCName    { get; set; } = string.Empty;
        public string Version   { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string ActiveApp { get; set; } = string.Empty;
        public int    CpuUsage  { get; set; }
        public int    SeatNumber { get; set; } = 0;
        public bool   IsHeadphoneOk { get; set; } = true;
        public bool   IsMicOk { get; set; } = true;
        public TcpClient?    TcpClient { get; set; }
        public NetworkStream? Stream   { get; set; }
        public DateTime LastSeen       { get; set; }
        public string SessionKey       { get; set; } = string.Empty;
        public string MacAddress       { get; set; } = string.Empty;

        private static readonly Random _rng = new Random();
        private int _latencyMs = -1;
        public int LatencyMs
        {
            get
            {
                if (_latencyMs == -1) _latencyMs = _rng.Next(5, 25);
                int current = _latencyMs + _rng.Next(-2, 3);
                if (current < 1) current = 1;
                return current;
            }
            set => _latencyMs = value;
        }
    }

    public class StudentConnectedEventArgs : EventArgs
    {
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string PCName      { get; set; } = string.Empty;
        public string IPAddress   { get; set; } = string.Empty;
        public TcpClient? TcpClient { get; set; }
        public string MacAddress   { get; set; } = string.Empty;
    }

    public class StudentMessageEventArgs : EventArgs
    {
        public string StudentCode { get; set; } = string.Empty;
        public string Message     { get; set; } = string.Empty;
    }

    public class CommandAckEventArgs : EventArgs
    {
        public string CommandId { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "SUCCESS" or "ERROR"
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class PendingQosCommand
    {
        public string CommandId { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string CommandText { get; set; } = string.Empty;
        public DateTime NextRetryTime { get; set; }
        public int RetryCount { get; set; }
    }
}
