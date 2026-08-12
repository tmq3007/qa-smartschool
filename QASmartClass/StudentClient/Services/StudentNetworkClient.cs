using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using QASmartClass.Utilities;
            using QASmartClass.Network;

namespace QASmartClass.StudentClient.Services
{
    /// <summary>
    /// Client mạng phía Học sinh:
    ///   1. Lắng nghe UDP beacon từ GV (port 29876)
    ///   2. Kết nối TCP tới GV (port 29877)
    ///   3. Gửi JOIN, heartbeat, hand-raise, question
    ///   4. Nhận CMD từ GV (lock, quiz, broadcast)
    /// </summary>
    public partial class StudentNetworkClient : ObservableObject, IDisposable
    {
        public const int DISCOVERY_PORT = 29878;
        public const int TCP_PORT       = 29877;
        public const int FILE_PORT      = 29879;
        private const int HEARTBEAT_INTERVAL_MS = 10_000;

        // ─── State ─────────────────────────────────────────
        private UdpClient?   _udpListener;
        private TcpClient?   _tcpClient;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;
        private readonly System.Collections.Generic.List<DateTime> _errorReportTimes = new();
        private readonly object _errorLock = new object();
        private readonly CircuitBreaker _circuitBreaker;

        [ObservableProperty] private bool _isConnected;
        [ObservableProperty] private string _teacherName = "";
        [ObservableProperty] private string _className = "";
        [ObservableProperty] private string _serverIP = "";
        [ObservableProperty] private bool _isHandRaised;

        private long _lastReceivedPacketTicks = DateTime.UtcNow.Ticks;

        /// <summary>IP GV đã kết nối — dùng cho File Transfer</summary>
        public string ConnectedIP => ServerIP;

        // ─── Student identity ──────────────────────────────
        public string StudentName { get; set; } = "Học sinh";
        public string StudentCode { get; set; } = "HS001";
        public string PCName { get; set; } = Environment.MachineName;
        public string SessionKey { get; set; } = string.Empty;
        public string ClassCode { get; set; } = string.Empty;
        public string SessionSalt { get; set; } = string.Empty;
        public long ClockDrift { get; set; } = 0;
        public string LastRaiseReason { get; set; } = "Phát biểu";
        public bool IsExamActive { get; set; } = false;

        // ─── Events ────────────────────────────────────────
        public event EventHandler<string>? CommandReceived;
        public event EventHandler<string>? MessageReceived;
        public event EventHandler? Connected;
        public event EventHandler? Disconnected;

        private INetworkSocket? _customSocket;

        public StudentNetworkClient()
        {
            Connected += async (sender, args) =>
            {
                await ProcessOfflineQueueAsync();
            };
            _circuitBreaker = new CircuitBreaker();
        }

        public StudentNetworkClient(INetworkSocket customSocket) : this()
        {
            _customSocket = customSocket;
            IsConnected = customSocket.Connected;
        }

        public void TriggerConnectionLoss()
        {
            if (IsConnected)
            {
                IsConnected = false;
                Disconnected?.Invoke(this, EventArgs.Empty);
                Log.Information("[Test] Sudden connection loss triggered.");
            }
        }

        // ═══════════════════════════════════════════════════
        //  START — Auto-discover & connect
        // ═══════════════════════════════════════════════════

        /// <summary>Bắt đầu tìm GV qua UDP beacon, tự động kết nối TCP</summary>
        public async Task StartAsync()
        {
            if (IsConnected) return;
            _cts = new CancellationTokenSource();

            Log.Information("StudentNetworkClient: Starting discovery and reconnect loops...");
            _ = DiscoverTeacherAsync(_cts.Token);
            _ = DirectReconnectLoopAsync(_cts.Token);
            await Task.CompletedTask;
        }

        private async Task DirectReconnectLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (IsConnected)
                {
                    try
                    {
                        await Task.Delay(1000, ct);
                    }
                    catch (OperationCanceledException) { break; }
                    continue;
                }

                if (!string.IsNullOrEmpty(ServerIP))
                {
                    int reconnectIntervalSec = 3;
                    try
                    {
                        using var db = new QASmartClass.Data.AppDbContext();
                        var intervalSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Network_DirectReconnectIntervalSec");
                        if (intervalSetting != null && int.TryParse(intervalSetting.Value, out var val))
                        {
                            reconnectIntervalSec = val;
                        }
                    }
                    catch { }

                    try
                    {
                        int port = TCP_PORT;
                        var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                        if (System.IO.File.Exists(profilePath))
                        {
                            try
                            {
                                var json = QASmartClass.StudentClient.Services.SecureProfileHelper.ReadProfileText(profilePath);
                                var profile = System.Text.Json.JsonSerializer.Deserialize<QASmartClass.StudentClient.Models.StudentProfileCache>(json);
                                if (profile != null) port = profile.NetworkPort;
                            }
                            catch { }
                        }

                        Log.Information("[Reconnect] Direct reconnect loop: attempting connection to last known teacher IP: {IP}:{Port}", ServerIP, port);
                        
                        using (var tempClient = new TcpClient())
                        {
                            using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2.5)))
                            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token))
                            {
                                await tempClient.ConnectAsync(IPAddress.Parse(ServerIP), port, linkedCts.Token);
                                if (tempClient.Connected)
                                {
                                    Log.Information("[Reconnect] Direct reconnect loop: connection succeeded! Initializing TCP connection...");
                                    _udpListener?.Close();
                                    _udpListener = null;
                                    await ConnectTcpAsync(ServerIP, port, ct);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("[Reconnect] Direct reconnect loop attempt to {IP} failed: {Err}", ServerIP, ex.Message);
                    }
                }

                try
                {
                    int reconnectIntervalSec = 3;
                    try
                    {
                        using var db = new QASmartClass.Data.AppDbContext();
                        var intervalSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Network_DirectReconnectIntervalSec");
                        if (intervalSetting != null && int.TryParse(intervalSetting.Value, out var val))
                        {
                            reconnectIntervalSec = val;
                        }
                    }
                    catch { }
                    await Task.Delay(reconnectIntervalSec * 1000, ct);
                }
                catch (OperationCanceledException) { break; }
            }
        }

        /// <summary>Kết nối trực tiếp đến IP GV (không cần discovery)</summary>
        public async Task ConnectDirectAsync(string teacherIP, int port = TCP_PORT)
        {
            // ═══ LOI_VID_22 FIX: Dọn kết nối cũ trước khi mở kết nối mới ═══
            if (IsConnected || _tcpClient != null)
            {
                Log.Information("[ReconnectSafe] Đóng kết nối cũ trước khi kết nối trực tiếp đến {IP}:{Port}", teacherIP, port);
                Stop();
            }
            _cts = new CancellationTokenSource();
            ServerIP = teacherIP;
            await ConnectTcpAsync(teacherIP, port, _cts.Token);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _stream?.Close();
            _tcpClient?.Close();
            _udpListener?.Close();
            _udpListener = null;
            IsConnected = false;
            Log.Information("StudentNetworkClient: Stopped");
        }

        // ═══════════════════════════════════════════════════
        //  UDP DISCOVERY — Lắng nghe beacon từ GV
        // ═══════════════════════════════════════════════════

        private async Task DiscoverTeacherAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (IsConnected)
                {
                    try
                    {
                        await Task.Delay(1000, ct);
                    }
                    catch (OperationCanceledException) { break; }
                    continue;
                }

                try
                {
                    if (_udpListener == null)
                    {
                        _udpListener = new UdpClient(DISCOVERY_PORT);
                        _udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        Log.Information("UDP discovery listening on :{Port}", DISCOVERY_PORT);
                    }

                    var result = await _udpListener.ReceiveAsync(ct);
                    var msg = Encoding.UTF8.GetString(result.Buffer);

                    // Format 1: "QASC|className|teacherName|serverIP|tcpPort"
                    // Format 2: "QA_TEACHER_IP:192.168.1.100|CLASS_CODE:T102"
                    if (msg.StartsWith("QA_TEACHER_IP:"))
                    {
                        var parts = msg.Split('|');
                        string ip = "";
                        string classCode = "";
                        foreach (var part in parts)
                        {
                            if (part.StartsWith("QA_TEACHER_IP:"))
                            {
                                ip = part.Substring("QA_TEACHER_IP:".Length);
                            }
                            else if (part.StartsWith("CLASS_CODE:"))
                            {
                                classCode = part.Substring("CLASS_CODE:".Length);
                            }
                        }

                        if (!string.IsNullOrEmpty(ip))
                        {
                            if (string.IsNullOrEmpty(ClassCode) || ClassCode.Equals(classCode, StringComparison.OrdinalIgnoreCase))
                            {
                                ServerIP = ip;
                                ClassName = classCode;
                                TeacherName = "Giáo viên";

                                Log.Information("UDP Broadcast matched ClassCode '{ClassCode}' @ {IP}", classCode, ip);

                                _udpListener?.Close();
                                _udpListener = null;

                                // JSON structured logging setup
                                Log.Logger = new LoggerConfiguration()
                                    .MinimumLevel.Information()
                                    .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(),
                                        "logs/qasmarttouch-.json",
                                        rollingInterval: RollingInterval.Day,
                                        retainedFileCountLimit: 30)
                                    .CreateLogger();

                                int port = TCP_PORT;
                                var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                                if (System.IO.File.Exists(profilePath))
                                {
                                    try
                                    {
                                        var json = QASmartClass.StudentClient.Services.SecureProfileHelper.ReadProfileText(profilePath);
                                        var profile = System.Text.Json.JsonSerializer.Deserialize<QASmartClass.StudentClient.Models.StudentProfileCache>(json);
                                        if (profile != null) port = profile.NetworkPort;
                                    }
                                    catch { }
                                }

                                await ConnectTcpAsync(ServerIP, port, ct);
                            }
                            else
                            {
                                Log.Debug("UDP Broadcast ClassCode '{ClassCode}' mismatch. Expected: '{Expected}'", classCode, ClassCode);
                            }
                        }
                    }
                    else if (msg.StartsWith("QASC|"))
                    {
                        var parts = msg.Split('|');
                        if (parts.Length >= 5)
                        {
                            ClassName = parts[1];
                            TeacherName = parts[2];
                            ServerIP = parts[3];
                            int port = int.TryParse(parts[4], out var p) ? p : TCP_PORT;

                            Log.Information("Beacon detected: {Class} — {Teacher} @ {IP}:{Port}",
                                ClassName, TeacherName, ServerIP, port);

                            _udpListener?.Close();
                            _udpListener = null;

                            await ConnectTcpAsync(ServerIP, port, ct);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!ct.IsCancellationRequested)
                    {
                        Log.Warning("UDP discovery or TCP connect error: {Err}", ex.Message);
                        _udpListener?.Close();
                        _udpListener = null;
                        try
                        {
                            await Task.Delay(5000, ct);
                        }
                        catch (OperationCanceledException) { break; }
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════
        //  TCP CONNECTION — Full-duplex command channel
        // ═══════════════════════════════════════════════════

        private async Task ConnectTcpAsync(string ip, int port, CancellationToken ct)
        {
            try
            {
                _tcpClient = new TcpClient();
                _tcpClient.SendBufferSize = 65536;
                _tcpClient.ReceiveBufferSize = 65536;
                await _circuitBreaker.ExecuteAsync(async () => { await _tcpClient.ConnectAsync(IPAddress.Parse(ip), port, ct); return true; });
                _stream = _tcpClient.GetStream();

                // Generate RSA key pair for this connection
                using var rsa = System.Security.Cryptography.RSA.Create(2048);
                string pubKeyBase64 = Convert.ToBase64String(rsa.ExportRSAPublicKey());

                // Send JOIN with public key and MAC Address
                var mac = GetMacAddress();
                var joinMsg = $"JOIN|{StudentName}|{StudentCode}|{PCName}|1.0|{pubKeyBase64}|{mac}";
                await _stream.WriteAsync(Encoding.UTF8.GetBytes(joinMsg), ct);

                // Read ACK
                var buffer = new byte[2048]; // larger buffer to accommodate encrypted RSA base64
                var len = await _stream.ReadAsync(buffer, 0, buffer.Length, ct);
                var ack = Encoding.UTF8.GetString(buffer, 0, len);

                if (ack.StartsWith("OK|"))
                {
                    var ackParts = ack.Split('|');
                    if (ackParts.Length >= 4)
                    {
                        string encryptedSessionKeyBase64 = ackParts[3];
                        try
                        {
                            byte[] encryptedBytes = Convert.FromBase64String(encryptedSessionKeyBase64);
                            byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, System.Security.Cryptography.RSAEncryptionPadding.Pkcs1);
                            SessionKey = Encoding.UTF8.GetString(decryptedBytes);
                        }
                        catch (Exception decryptEx)
                        {
                            Log.Warning("Decryption failed, falling back to plaintext SessionKey: {Err}", decryptEx.Message);
                            SessionKey = encryptedSessionKeyBase64;
                        }
                    }
                    if (ackParts.Length >= 5)
                    {
                        ClassCode = ackParts[4];
                    }
                    if (ackParts.Length >= 6)
                    {
                        SessionSalt = ackParts[5];
                    }
                    if (ackParts.Length >= 3 && long.TryParse(ackParts[2], out long serverTimestamp))
                    {
                        ClockDrift = serverTimestamp - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        Log.Information("Clock drift relative to server: {Drift} seconds", ClockDrift);
                    }
                    IsConnected = true;
                    Log.Information("TCP connected to {IP}:{Port} — ACK: {Ack}", ip, port, ack);
                    Connected?.Invoke(this, EventArgs.Empty);

                    // Start message loop + heartbeat
                    _ = ReceiveLoopAsync(ct);
                    _ = HeartbeatLoopAsync(ct);
                }
                else
                {
                    Log.Warning("Unexpected ACK: {Ack}", ack);
                    _tcpClient?.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("TCP connect error: {Err}", ex.Message);
                IsConnected = false;
                IsHandRaised = false;
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            var buffer = new byte[4096];
            var msgBuffer = new System.Text.StringBuilder();
            try
            {
                while (!ct.IsCancellationRequested && _tcpClient?.Connected == true)
                {
                    var len = await _stream!.ReadAsync(buffer, 0, buffer.Length, ct);
                    if (len == 0) break;

                    Interlocked.Exchange(ref _lastReceivedPacketTicks, DateTime.UtcNow.Ticks);

                    msgBuffer.Append(Encoding.UTF8.GetString(buffer, 0, len));

                    string accumulated = msgBuffer.ToString();
                    int nlIdx;
                    while ((nlIdx = accumulated.IndexOf('\n')) >= 0)
                    {
                        var line = accumulated.Substring(0, nlIdx).Trim();
                        accumulated = accumulated.Substring(nlIdx + 1);

                        if (string.IsNullOrEmpty(line)) continue;

                        // 🔐 Giải mã AES-256 nếu có tiền tố ENC_CMD| (Giai đoạn 6)
                        if (line.StartsWith("ENC_CMD|"))
                        {
                            var decrypted = NetworkCryptoHelper.DecryptCommand(line, SessionSalt);
                            if (!string.IsNullOrEmpty(decrypted))
                                line = decrypted;
                        }
                        else if (!string.IsNullOrEmpty(SessionKey))
                        {
                            var decrypted = CryptoHelper.Decrypt(line, SessionKey);
                            if (!string.IsNullOrEmpty(decrypted))
                                line = decrypted;
                        }

                        if (line == "HB_ACK")
                            continue; // Heartbeat ack — ignore

                        if (line.StartsWith("CMD|") || 
                            line.StartsWith("MSG|") || 
                            line.StartsWith("POLICY|") || 
                            line.StartsWith("LOCK_KEYBOARD|") || 
                            line.StartsWith("SILENCE|") || 
                            line.StartsWith("QUIET_MODE|"))
                        {
                            if (VerifyCommandSignature(line, out string cleanCmd))
                            {
                                string cmdId = ExtractCommandId(cleanCmd);
                                Log.Information("Command received and verified: {Cmd}", cleanCmd);

                                if (cleanCmd.StartsWith("CMD|QUIZ_START"))
                                {
                                    IsExamActive = true;
                                }
                                else if (cleanCmd.StartsWith("CMD|QUIZ_END"))
                                {
                                    IsExamActive = false;
                                }

                                // 📡 Gửi ACK phản hồi về cho giáo viên (Giai đoạn 6 & 7)
                                if (cleanCmd.StartsWith("CMD|PING"))
                                {
                                    await SendAsync($"ACK|{cmdId}|{PCName}|SUCCESS|PONG");
                                }
                                else if (cleanCmd.StartsWith("CMD|UPDATE_SESSION"))
                                {
                                    // ═══ LOI_VID_21 FIX: Cập nhật ClassCode & SessionSalt mới ═══
                                    // Xử lý ngay trong luồng nhận để đảm bảo tính nguyên tử:
                                    // các lệnh tiếp theo sẽ được xác minh bằng muối mới
                                    var updateParts = cleanCmd.Split('|');
                                    if (updateParts.Length >= 4)
                                    {
                                        string newClassCode = updateParts[2];
                                        string newSessionSalt = updateParts[3];
                                        ClassCode = newClassCode;
                                        SessionSalt = newSessionSalt;
                                        Log.Information("[SessionSync] Đã cập nhật phiên mới — ClassCode: {Code}, SessionSalt đã thay đổi", newClassCode);
                                    }
                                    else
                                    {
                                        Log.Warning("[SessionSync] Lệnh UPDATE_SESSION thiếu tham số: {Cmd}", cleanCmd);
                                    }
                                    await SendAsync($"ACK|{cmdId}|{PCName}|SUCCESS");
                                    // Kích hoạt sự kiện để UI (StudentShell) có thể phản ứng
                                    CommandReceived?.Invoke(this, cleanCmd);
                                }
                                else if (cleanCmd.StartsWith("CMD|CHECK_INTEGRITY"))
                                {
                                    string integrityStatus = PerformIntegrityCheck();
                                    await SendAsync($"ACK|{cmdId}|{PCName}|SUCCESS|{integrityStatus}");
                                }
                                else if (cleanCmd.StartsWith("CMD|RESTORE_DEFAULTS"))
                                {
                                    if (IsExamActive)
                                    {
                                        Log.Warning("Yêu cầu khôi phục cài đặt bị từ chối do chế độ thi cử đang hoạt động!");
                                        await SendAsync($"ACK|{cmdId}|{PCName}|ERROR|EXAM_ACTIVE_LOCK");
                                        continue;
                                    }

                                    BackupStudentData();
                                    
                                    // Gửi gói tin mạng khẩn cấp RESTORE_ALERT chứa Base64 dữ liệu bài làm cuối cùng lên máy giáo viên
                                    try
                                    {
                                        string lastWorkBase64 = "";
                                        if (System.IO.File.Exists(QASmartClass.Services.AppPaths.DatabaseFile))
                                        {
                                            byte[] dbBytes = System.IO.File.ReadAllBytes(QASmartClass.Services.AppPaths.DatabaseFile);
                                            lastWorkBase64 = Convert.ToBase64String(dbBytes);
                                        }
                                        await SendAsync($"RESTORE_ALERT|{StudentCode}|{lastWorkBase64}");
                                        await Task.Delay(300);
                                    }
                                    catch (Exception alertEx)
                                    {
                                        Log.Warning("Không thể phát tin khẩn RESTORE_ALERT: {Err}", alertEx.Message);
                                    }

                                    await SendAsync($"ACK|{cmdId}|{PCName}|SUCCESS|RESTORED");
                                    await Task.Delay(500); // Đợi gửi xong ACK

                                    try
                                    {
                                        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                                        
                                        if (System.IO.File.Exists(QASmartClass.Services.AppPaths.ClassroomSettingsFile))
                                        {
                                            System.IO.File.Delete(QASmartClass.Services.AppPaths.ClassroomSettingsFile);
                                        }
                                        
                                        if (System.IO.Directory.Exists(QASmartClass.Services.AppPaths.SettingsDir))
                                        {
                                            foreach (var file in System.IO.Directory.GetFiles(QASmartClass.Services.AppPaths.SettingsDir, "student_profile_*.json"))
                                            {
                                                try { System.IO.File.Delete(file); } catch { }
                                            }
                                        }

                                        if (System.IO.File.Exists(QASmartClass.Services.AppPaths.DatabaseFile))
                                        {
                                            System.IO.File.Delete(QASmartClass.Services.AppPaths.DatabaseFile);
                                        }
                                        if (System.IO.File.Exists(QASmartClass.Services.AppPaths.DefaultDatabaseTemplate))
                                        {
                                            System.IO.File.Copy(QASmartClass.Services.AppPaths.DefaultDatabaseTemplate, QASmartClass.Services.AppPaths.DatabaseFile, true);
                                        }
                                    }
                                    catch (Exception restoreEx)
                                    {
                                        Log.Error(restoreEx, "Lỗi khi khôi phục cài đặt gốc.");
                                    }

                                    bool isUnitTest = AppDomain.CurrentDomain.GetAssemblies().Any(a => 
                                        a.FullName.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) || 
                                        a.FullName.Contains("TestPlatform") || 
                                        a.FullName.StartsWith("Microsoft.VisualStudio.TestPlatform", StringComparison.OrdinalIgnoreCase));
                                    if (!isUnitTest)
                                    {
                                        string? appPath = Environment.ProcessPath;
                                        if (string.IsNullOrEmpty(appPath))
                                        {
                                            appPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QASmartClass.exe");
                                        }
                                        if (System.IO.File.Exists(appPath))
                                        {
                                            System.Diagnostics.Process.Start(appPath);
                                        }
                                        System.Environment.Exit(0);
                                    }
                                }
                                else
                                {
                                    await SendAsync($"ACK|{cmdId}|{PCName}|SUCCESS");

                                    if (cleanCmd.StartsWith("CMD|"))
                                    {
                                        CommandReceived?.Invoke(this, cleanCmd);
                                    }
                                    else if (cleanCmd.StartsWith("MSG|"))
                                    {
                                        var text = cleanCmd.Substring(4);
                                        MessageReceived?.Invoke(this, text);
                                    }
                                    else if (cleanCmd.StartsWith("POLICY|"))
                                    {
                                        CommandReceived?.Invoke(this, "CMD|" + cleanCmd);
                                    }
                                    else if (cleanCmd.StartsWith("LOCK_KEYBOARD|"))
                                    {
                                        CommandReceived?.Invoke(this, "CMD|" + cleanCmd);
                                    }
                                    else if (cleanCmd.StartsWith("SILENCE|"))
                                    {
                                        bool isEnabled = cleanCmd.Contains("enabled=true");
                                        if (isEnabled)
                                        {
                                            CommandReceived?.Invoke(this, $"CMD|SILENCE|id={cmdId}");
                                        }
                                        else
                                        {
                                            CommandReceived?.Invoke(this, $"CMD|CLEAR_SILENCE|id={cmdId}");
                                        }
                                    }
                                    else if (cleanCmd.StartsWith("QUIET_MODE|"))
                                    {
                                        CommandReceived?.Invoke(this, "CMD|" + cleanCmd);
                                    }
                                }
                            }
                            else
                            {
                                Log.Warning("Command signature verification failed or expired! Command: {Cmd}", line);
                            }
                        }
                        else
                        {
                            // V4.1-FIX LOI_VID_30: Lọc tin nhắn hệ thống trước khi gửi vào chat
                            // Nếu giải mã thất bại, tin nhắn gốc (ENC_CMD|...) sẽ rơi vào đây
                            var guard = QASmartClass.Utilities.MessageGuard.Evaluate(line);
                            if (guard.IsAllowedInChat)
                            {
                                Log.Debug("Chat message received: {Data}", line);
                                MessageReceived?.Invoke(this, line);
                            }
                            else
                            {
                                Log.Debug("Blocked non-chat message from display: [{Category}] {Data}", guard.Category, line.Length > 60 ? line.Substring(0, 60) + "..." : line);
                            }
                        }
                    }



                    msgBuffer.Clear();
                    if (!string.IsNullOrEmpty(accumulated))
                        msgBuffer.Append(accumulated);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log.Warning("Receive loop error: {Err}", ex.Message);
                SendErrorReport(ex);
            }
            finally
            {
                DisconnectAndCleanup();
            }
        }

        private readonly object _disconnectLock = new object();

        private void DisconnectAndCleanup()
        {
            lock (_disconnectLock)
            {
                if (!IsConnected) return;
                IsConnected = false;
                IsHandRaised = false;
                try
                {
                    _stream?.Close();
                    _tcpClient?.Close();
                }
                catch { }
                Disconnected?.Invoke(this, EventArgs.Empty);
                Log.Information("Disconnected from teacher");
            }
        }

        private string ExtractCommandId(string cleanCmd)
        {
            try
            {
                var parts = cleanCmd.Split('|');
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

        private async Task HeartbeatLoopAsync(CancellationToken ct)
        {
            Interlocked.Exchange(ref _lastReceivedPacketTicks, DateTime.UtcNow.Ticks);
            int secondsPassed = 0;

            int maxIdleSeconds = 30;
            try
            {
                using var db = new QASmartClass.Data.AppDbContext();
                var timeoutSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Network_KeepAliveTimeout");
                if (timeoutSetting != null && int.TryParse(timeoutSetting.Value, out var val))
                {
                    maxIdleSeconds = val;
                }
            }
            catch { }

            while (!ct.IsCancellationRequested && IsConnected)
            {
                try
                {
                    var lastReceived = new DateTime(Interlocked.Read(ref _lastReceivedPacketTicks));
                    var idleTime = DateTime.UtcNow - lastReceived;
                    if (idleTime.TotalSeconds > maxIdleSeconds)
                    {
                        Log.Warning("[Split-Brain Fix] No communication from teacher for {Idle} seconds (Max allowed: {Max}). Disconnecting.", 
                            (int)idleTime.TotalSeconds, maxIdleSeconds);
                        DisconnectAndCleanup();
                        break;
                    }

                    if (secondsPassed >= 10)
                    {
                        if (_stream != null && _tcpClient?.Connected == true)
                        {
                            var hb = $"HB|{StudentCode}|online";
                            await _stream.WriteAsync(Encoding.UTF8.GetBytes(hb), ct);
                            bool hpOk = CheckHeadphonesConnected();
                            bool micOk = CheckMicConnected();
                            var periMsg = $"PERIPHERAL_STATUS|Headphones={(hpOk ? "OK" : "Error")}|Mic={(micOk ? "OK" : "Error")}";
                            await _stream.WriteAsync(Encoding.UTF8.GetBytes(periMsg), ct);
                        }
                        secondsPassed = 0;
                    }

                    await Task.Delay(1000, ct);
                    secondsPassed++;
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Log.Warning("Heartbeat loop error: {Err}", ex.Message);
                    DisconnectAndCleanup();
                    break;
                }
            }
        }

        // ═══════════════════════════════════════════════════
        //  SEND — HS gửi dữ liệu lên GV
        // ═══════════════════════════════════════════════════

        /// <summary>Gửi message lên server GV</summary>
        public async Task SendAsync(string message)
        {
            if (!IsConnected || _stream == null) return;
            try
            {
                var cleanMessage = message.EndsWith("\n") ? message.Substring(0, message.Length - 1) : message;
                string encryptedMessage = cleanMessage;
                if (!string.IsNullOrEmpty(SessionKey))
                {
                    encryptedMessage = CryptoHelper.Encrypt(cleanMessage, SessionKey);
                }
                var messageWithNewLine = encryptedMessage.EndsWith("\n") ? encryptedMessage : encryptedMessage + "\n";
                await _stream.WriteAsync(Encoding.UTF8.GetBytes(messageWithNewLine));
                Log.Debug("Sent (encrypted): {Msg}", cleanMessage);
            }
            catch (Exception ex)
            {
                Log.Warning("Send error: {Err}", ex.Message);
                if (IsConnected && !message.StartsWith("ERR_REPORT|"))
                {
                    SendErrorReport(ex);
                }
            }
        }

        private void SendErrorReport(Exception ex)
        {
            try
            {
                if (!IsConnected) return;
                string severity = (ex is SocketException || ex is System.IO.IOException) ? "ERROR" : "WARN";
                lock (_errorLock)
                {
                    var now = DateTime.UtcNow;
                    _errorReportTimes.RemoveAll(t => (now - t).TotalSeconds > 30);
                    if (_errorReportTimes.Count >= 3)
                    {
                        Log.Warning("Throttled error report: {Msg} (Severity: {Severity})", ex.Message, severity);
                        return;
                    }
                    _errorReportTimes.Add(now);
                }
                _ = SendAsync($"ERR_REPORT|{StudentCode}|{severity}|{ex.Message}");
            }
            catch { }
        }

        public Task SendHandRaise(bool raised) => SendAsync($"HAND_RAISE|raised={raised}");
        [System.Runtime.InteropServices.DllImport("winmm.dll")]
        private static extern int waveOutGetNumDevs();

        [System.Runtime.InteropServices.DllImport("winmm.dll")]
        private static extern int waveInGetNumDevs();

        private bool CheckHeadphonesConnected()
        {
            try { return waveOutGetNumDevs() > 0; }
            catch { return false; }
        }

        private bool CheckMicConnected()
        {
            try { return waveInGetNumDevs() > 0; }
            catch { return false; }
        }
        public Task SendHandRaiseWithReason(bool raised, string reason)
        {
            LastRaiseReason = reason;
            return SendAsync($"HAND_RAISE|raised={raised}|reason={reason}");
        }
        public Task SendQuestion(string text) => SendAsync($"STUDENT_QUESTION|text={text}");
        public Task SendQuizAnswer(int quizId, string answersJson) => SendAsync($"QUIZ_ANSWER|{quizId}|{answersJson}");
        public Task SendCheatingAlert(int quizId, string reason) => SendAsync($"CHEATING_ALERT|{quizId}|{reason}");
        public Task SendChat(string text, string channel = "ALL") => SendAsync($"CHAT|{StudentCode}|{channel}|{text}");

        private string GetMacAddress()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up && 
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback && 
                        ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    {
                        var mac = ni.GetPhysicalAddress().ToString();
                        if (!string.IsNullOrEmpty(mac))
                        {
                            return string.Join(":", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2)));
                        }
                    }
                }
            }
            catch { }
            return "00:00:00:00:00:00";
        }

        private bool VerifyCommandSignature(string cmd, out string cleanCmd)
        {
            cleanCmd = cmd;
            try
            {
                var parts = cmd.Split('|');
                if (parts.Length < 3) return false;

                string signature = parts[parts.Length - 1];
                string timestampStr = parts[parts.Length - 2];

                if (!long.TryParse(timestampStr, out long timestamp))
                {
                    return false;
                }

                // Check signature first
                int lastPipeIndex = cmd.LastIndexOf('|');
                if (lastPipeIndex < 0) return false;
                string payload = cmd.Substring(0, lastPipeIndex);

                string salt = !string.IsNullOrEmpty(SessionSalt) ? SessionSalt : ClassCode;

                // === UPGRADE_10: Sửa lỗi bootstrapping chữ ký khi bắt đầu lớp học (UPDATE_SESSION) ===
                if (payload.StartsWith("CMD|UPDATE_SESSION|") && string.IsNullOrEmpty(SessionSalt))
                {
                    var cmdParts = payload.Split('|');
                    if (cmdParts.Length >= 4)
                    {
                        string newClassCode = cmdParts[2];
                        salt = newClassCode;
                    }
                }

                using (var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(salt)))
                {
                    byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                    string computedSignature = Convert.ToBase64String(hash);

                    if (computedSignature != signature)
                    {
                        Log.Warning("Command signature mismatch!");
                        return false;
                    }
                }

                // Check timestamp replay attack (within 5 seconds, adjusted for clock drift)
                long adjustedLocalTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ClockDrift;
                if (Math.Abs(timestamp - adjustedLocalTime) > 5)
                {
                    long newDrift = timestamp - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    Log.Warning("Command timestamp drift detected. Old Drift: {Old}s, New Drift: {New}s. Updating offset...", ClockDrift, newDrift);
                    ClockDrift = newDrift;
                }

                int secondToLastPipeIndex = payload.LastIndexOf('|');
                if (secondToLastPipeIndex < 0) return false;
                cleanCmd = payload.Substring(0, secondToLastPipeIndex);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Error verifying command signature: {Err}", ex.Message);
                return false;
            }
        }

        public async Task SaveOfflineMessageAsync(string msgType, string payload)
        {
            await QASmartClass.Data.AppDbContext.BackgroundDbSemaphore.WaitAsync();
            try
            {
                var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
                var connStr = $"Data Source={dbPath};Foreign Keys=True;Default Timeout=5";

                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection(connStr))
                {
                    await conn.OpenAsync();
                    
                    using (var pragmaCmd = conn.CreateCommand())
                    {
                        pragmaCmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
                        await pragmaCmd.ExecuteNonQueryAsync();
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS OfflineMessageQueue (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                MessageType TEXT NOT NULL,
                                Payload TEXT NOT NULL,
                                CreatedAt TEXT NOT NULL,
                                IsSent INTEGER DEFAULT 0
                            );";
                        await cmd.ExecuteNonQueryAsync();
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO OfflineMessageQueue (MessageType, Payload, CreatedAt) VALUES ($type, $payload, $created);";
                        cmd.Parameters.AddWithValue("$type", msgType);
                        cmd.Parameters.AddWithValue("$payload", payload);
                        cmd.Parameters.AddWithValue("$created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        await cmd.ExecuteNonQueryAsync();
                    }

                    Log.Information("Saved offline message ({Type}) to SQLite local queue", msgType);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to save offline message: {Err}", ex.Message);
            }
            finally
            {
                QASmartClass.Data.AppDbContext.BackgroundDbSemaphore.Release();
            }
        }

        private async Task ProcessOfflineQueueAsync()
        {
            await QASmartClass.Data.AppDbContext.BackgroundDbSemaphore.WaitAsync();
            try
            {
                var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
                var connStr = $"Data Source={dbPath};Foreign Keys=True;Default Timeout=5";

                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection(connStr))
                {
                    await conn.OpenAsync();

                    using (var pragmaCmd = conn.CreateCommand())
                    {
                        pragmaCmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
                        await pragmaCmd.ExecuteNonQueryAsync();
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS OfflineMessageQueue (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                MessageType TEXT NOT NULL,
                                Payload TEXT NOT NULL,
                                CreatedAt TEXT NOT NULL,
                                IsSent INTEGER DEFAULT 0
                            );";
                        await cmd.ExecuteNonQueryAsync();
                    }

                    var pending = new System.Collections.Generic.List<(int Id, string MsgType, string Payload, string CreatedAt)>();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Id, MessageType, Payload, CreatedAt FROM OfflineMessageQueue WHERE IsSent = 0 ORDER BY CreatedAt ASC;";
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                pending.Add((
                                    reader.GetInt32(0),
                                    reader.GetString(1),
                                    reader.GetString(2),
                                    reader.GetString(3)
                                ));
                            }
                        }
                    }

                    if (pending.Count == 0) return;

                    Log.Information("Processing {Count} offline messages from queue...", pending.Count);

                    foreach (var item in pending)
                    {
                        if (!IsConnected) break;

                        if (DateTime.TryParse(item.CreatedAt, out var createdAt))
                        {
                            if ((DateTime.Now - createdAt).TotalMinutes > 10)
                            {
                                Log.Warning("Offline message {Id} ({Type}) expired (TTL > 10m). Deleting.", item.Id, item.MsgType);
                                using (var delCmd = conn.CreateCommand())
                                {
                                    delCmd.CommandText = "DELETE FROM OfflineMessageQueue WHERE Id = $id;";
                                    delCmd.Parameters.AddWithValue("$id", item.Id);
                                    await delCmd.ExecuteNonQueryAsync();
                                }
                                continue;
                            }
                        }

                        bool sent = false;
                        try
                        {
                            if (item.MsgType == "QUESTION")
                            {
                                await SendQuestion(item.Payload);
                                sent = true;
                            }
                            else if (item.MsgType == "HAND_RAISE")
                            {
                                bool raised = item.Payload == "True";
                                await SendHandRaise(raised);
                                sent = true;
                            }
                        }
                        catch (Exception sendEx)
                        {
                            Log.Warning("Failed to resend offline message {Id}: {Err}", item.Id, sendEx.Message);
                            break;
                        }

                        if (sent)
                        {
                            using (var delCmd = conn.CreateCommand())
                            {
                                delCmd.CommandText = "DELETE FROM OfflineMessageQueue WHERE Id = $id;";
                                delCmd.Parameters.AddWithValue("$id", item.Id);
                                await delCmd.ExecuteNonQueryAsync();
                            }
                            Log.Information("Resent offline message {Id} ({Type}) successfully", item.Id, item.MsgType);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error in ProcessOfflineQueueAsync: {Err}", ex.Message);
            }
            finally
            {
                QASmartClass.Data.AppDbContext.BackgroundDbSemaphore.Release();
            }
        }

        public static async Task CleanupOldQueueLogsAsync()
        {
            await QASmartClass.Data.AppDbContext.BackgroundDbSemaphore.WaitAsync();
            try
            {
                var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
                var connStr = $"Data Source={dbPath};Foreign Keys=True;Default Timeout=5";
                
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection(connStr))
                {
                    await conn.OpenAsync();
                    
                    using (var pragmaCmd = conn.CreateCommand())
                    {
                        pragmaCmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
                        await pragmaCmd.ExecuteNonQueryAsync();
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS OfflineMessageQueue (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                MessageType TEXT NOT NULL,
                                Payload TEXT NOT NULL,
                                CreatedAt TEXT NOT NULL,
                                IsSent INTEGER DEFAULT 0
                            );";
                        await cmd.ExecuteNonQueryAsync();
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM OfflineMessageQueue WHERE CreatedAt < date('now', '-7 days');";
                        int rows = await cmd.ExecuteNonQueryAsync();
                        if (rows > 0)
                        {
                            Log.Information("Cleaned up {Count} expired offline queue logs from SQLite", rows);
                            cmd.CommandText = "VACUUM;";
                            await cmd.ExecuteNonQueryAsync();
                            Log.Information("SQLite database vacuumed successfully.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to cleanup SQLite database: {Err}", ex.Message);
            }
            finally
            {
                QASmartClass.Data.AppDbContext.BackgroundDbSemaphore.Release();
            }
        }

        private string PerformIntegrityCheck()
        {
            string dbStatus = "OK";
            string configStatus = "OK";
            string version = "4.2.3";

            try
            {
                var keyBytes = QASmartClass.Data.DbEncryptionKeyManager.GetOrInitializeKey();
                var hexKey = Convert.ToHexString(keyBytes);
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={QASmartClass.Services.AppPaths.DatabaseFile};Password={hexKey};Foreign Keys=False"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA integrity_check;";
                        var result = cmd.ExecuteScalar()?.ToString();
                        if (result != "ok")
                        {
                            dbStatus = $"CORRUPTED:{result}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                dbStatus = $"ERROR:{ex.Message}";
            }

            try
            {
                string classroomSettingsFile = QASmartClass.Services.AppPaths.ClassroomSettingsFile;
                if (System.IO.File.Exists(classroomSettingsFile))
                {
                    var json = System.IO.File.ReadAllText(classroomSettingsFile);
                    using (var doc = System.Text.Json.JsonDocument.Parse(json)) { }
                }
            }
            catch (Exception ex)
            {
                configStatus = $"CORRUPTED:{ex.Message}";
            }

            try
            {
                version = typeof(StudentNetworkClient).Assembly.GetName().Version?.ToString() ?? "4.2.3";
            }
            catch { }

            return $"db_status={dbStatus},config_status={configStatus},app_version={version}";
        }

        private void BackupStudentData()
        {
            try
            {
                var backupDir = QASmartClass.Services.AppPaths.SystemRestoreBackupDir;
                System.IO.Directory.CreateDirectory(backupDir);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string sessionBackupPath = System.IO.Path.Combine(backupDir, $"backup_{timestamp}");
                System.IO.Directory.CreateDirectory(sessionBackupPath);

                if (System.IO.File.Exists(QASmartClass.Services.AppPaths.DatabaseFile))
                {
                    System.IO.File.Copy(QASmartClass.Services.AppPaths.DatabaseFile, System.IO.Path.Combine(sessionBackupPath, "smartclass.db"), true);
                }

                if (System.IO.Directory.Exists(QASmartClass.Services.AppPaths.SettingsDir))
                {
                    var configBackupDir = System.IO.Path.Combine(sessionBackupPath, "Settings");
                    System.IO.Directory.CreateDirectory(configBackupDir);
                    foreach (var file in System.IO.Directory.GetFiles(QASmartClass.Services.AppPaths.SettingsDir))
                    {
                        try
                        {
                            System.IO.File.Copy(file, System.IO.Path.Combine(configBackupDir, System.IO.Path.GetFileName(file)), true);
                        }
                        catch { }
                    }
                }

                if (System.IO.Directory.Exists(QASmartClass.Services.AppPaths.SubmittedFilesDir))
                {
                    var subBackupDir = System.IO.Path.Combine(sessionBackupPath, "Submissions");
                    System.IO.Directory.CreateDirectory(subBackupDir);
                    foreach (var file in System.IO.Directory.GetFiles(QASmartClass.Services.AppPaths.SubmittedFilesDir))
                    {
                        try
                        {
                            System.IO.File.Copy(file, System.IO.Path.Combine(subBackupDir, System.IO.Path.GetFileName(file)), true);
                        }
                        catch { }
                    }
                }
                
                Log.Information("Backup complete: {Path}", sessionBackupPath);
            }
            catch (Exception ex)
            {
                Log.Error("Backup failed: {Err}", ex.Message);
            }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
