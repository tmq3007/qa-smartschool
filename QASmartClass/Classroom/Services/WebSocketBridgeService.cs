using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    /// <summary>
    /// WebSocket Bridge Service — Kết nối tablet/laptop học sinh (web browser)
    /// với hệ thống TCP nội bộ của GV SmartClass.
    ///
    /// Kiến trúc:
    ///   - HTTP server port 8080: serve Student Web App static files
    ///   - WebSocket endpoint /ws: full-duplex JSON messaging
    ///   - Bridge: chuyển đổi giữa TCP protocol ↔ WebSocket JSON
    ///
    /// Tích hợp:
    ///   - Khi GV bắt đầu phiên lớp học → auto-start bridge
    ///   - WebSocket client (HS trên tablet) ↔ bridge ↔ NetworkDiscoveryService
    /// </summary>
    public class WebSocketBridgeService : IDisposable
    {
        // ─── Config ───
        public const int WEB_PORT = 8080;
        public int WebPort { get; private set; } = 8080;
        private readonly string _webRootPath;

        // ─── State ───
        private HttpListener? _httpListener;
        private TcpListener? _tcpListener;
        private CancellationTokenSource? _cts;
        private readonly ConcurrentDictionary<string, WebSocketClient> _wsClients = new();
        private readonly ConcurrentDictionary<string, System.Collections.Generic.List<DateTime>> _clientRequestTimes = new();
        private readonly SemaphoreSlim _fileStreamLimiter = new(8, 8);

        // ─── Reference to classroom network ───
        private readonly NetworkDiscoveryService _networkService;
        private string _className = "";
        private string _teacherName = "";

        // ─── Events ───
        public event EventHandler<StudentConnectedEventArgs>? WebStudentConnected;
        public event EventHandler<string>? WebStudentDisconnected;
        public event EventHandler<StudentMessageEventArgs>? WebMessageReceived;
        /// <summary>LOI_VID_30 FIX: Event riêng biệt cho Heartbeat từ WebSocket students.
        /// Tương đương HeartbeatReceived trong NetworkDiscoveryService.
        /// Áp dụng ràng buộc NET-001.</summary>
        public event EventHandler<StudentMessageEventArgs>? WebHeartbeatReceived;

        public event EventHandler<BoardCommandEventArgs>? BoardCommandReceived;

        public class BoardCommandEventArgs : EventArgs
        {
            public string Action { get; set; } = "";
            public string Data { get; set; } = "";
            public string ClientId { get; set; } = "";
            public double BoardWidth { get; set; }
            public double BoardHeight { get; set; }
        }

        public int ConnectedWebClients => _wsClients.Count;
        public bool IsRunning { get; private set; }

        /// <summary>Expose current web client list for MonitorPage sync</summary>
        public IReadOnlyCollection<WebSocketClient> GetConnectedWebStudents()
            => _wsClients.Values.ToList().AsReadOnly();

        // ═══════════════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════

        public WebSocketBridgeService(NetworkDiscoveryService networkService)
        {
            _networkService = networkService;

            // Web root = StudentWebApp folder relative to exe
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            _webRootPath = Path.Combine(exeDir, "StudentWebApp");

            // Development fallback: bin/Debug/net8.0-windows → go up 3 levels → project root
            if (!Directory.Exists(_webRootPath))
            {
                var projectDir = Path.GetDirectoryName(
                    Path.GetDirectoryName(
                        Path.GetDirectoryName(exeDir)));
                if (projectDir != null)
                {
                    var devPath = Path.Combine(projectDir, "StudentWebApp");
                    if (Directory.Exists(devPath))
                        _webRootPath = devPath;
                }
            }

            Log.Information("WebSocketBridge: WebRoot resolved to {Path} (exists={Exists})",
                _webRootPath, Directory.Exists(_webRootPath));
        }

        // ═══════════════════════════════════════════════════════════
        //  START / STOP
        // ═══════════════════════════════════════════════════════════

        /// <summary>Bắt đầu HTTP + WebSocket server (User-Mode TcpListener 0.0.0.0 — không cần Admin/URLACL, thông suốt LAN & QA SmartTouch Android)</summary>
        public async Task StartAsync(string className, string teacherName)
        {
            if (IsRunning) return;

            _className = className;
            _teacherName = teacherName;
            _cts = new CancellationTokenSource();

            var localIPs = NetworkDiscoveryService.GetActiveLocalIPv4Addresses();

            int port = 8080;
            bool success = false;
            while (port <= 9000 && !success)
            {
                try
                {
                    EnsureFirewallRulesAndUrlAcl(port);

                    // Dùng trực tiếp User-Mode TcpListener(IPAddress.Any, port) thuộc tiến trình QASmartClass.exe:
                    // 1) Không bị HTTP.SYS chặn ErrorCode=5 (Access Denied) khi chạy asInvoker (không Admin), không rớt về loopback 127.0.0.1.
                    // 2) Được hưởng trực tiếp rule Windows Firewall của QASmartClass.exe (giống TCP 29877 & 29879), không bị kẹt ở System (PID 4).
                    // 3) Lắng nghe đồng thời cả 127.0.0.1 (Android Emulator) và mọi card mạng LAN/Wi-Fi/OPS Bridge (QA SmartTouch Android).
                    _tcpListener = new TcpListener(IPAddress.Any, port);
                    _tcpListener.Start();

                    WebPort = port;
                    success = true;
                    IsRunning = true;

                    Log.Information("WebSocketBridge: Started User-Mode TcpListener on ALL interfaces 0.0.0.0:{Port} (LAN IPs: {IPs}) — WebRoot: {Path}",
                        port, string.Join(", ", localIPs), _webRootPath);

                    _ = AcceptTcpLoopAsync(_cts.Token);
                }
                catch (SocketException sockEx) when (
                    sockEx.SocketErrorCode == SocketError.AddressAlreadyInUse ||
                    sockEx.SocketErrorCode == SocketError.AccessDenied)
                {
                    Log.Warning("WebSocketBridge: TCP port {Port} unavailable ({Code}), trying next...", port, sockEx.SocketErrorCode);
                    try { _tcpListener?.Stop(); } catch { }
                    _tcpListener = null;
                    port++;
                }
                catch (Exception ex)
                {
                    Log.Error("WebSocketBridge: Unexpected error starting TcpListener on port {Port}: {Err}", port, ex.Message);
                    try { _tcpListener?.Stop(); } catch { }
                    _tcpListener = null;
                    break;
                }

                /* // [MYTHWARE_LITE] Giữ lại khối HttpListener cũ (HTTP.SYS yêu cầu quyền Admin/urlacl nên tự hạ về loopback 127.0.0.1 làm thiết bị Android LAN không xem được màn hình chia sẻ)
                try
                {
                    _httpListener = new HttpListener();
                    _httpListener.Prefixes.Add($"http://+:{port}/");
                    _httpListener.Start();
                    WebPort = port;
                    success = true;
                    IsRunning = true;
                    _ = AcceptLoopAsync(_cts.Token);
                }
                catch (HttpListenerException) { }
                */
            }

            if (!success)
            {
                IsRunning = false;
                Log.Error("WebSocketBridge: Failed to start server after scanning ports 8080-9000");
            }
            await Task.CompletedTask;
        }

        public void Stop()
        {
            _cts?.Cancel();

            // Close all WebSocket connections
            foreach (var (_, client) in _wsClients)
            {
                try
                {
                    client.Socket?.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Server stopping",
                        CancellationToken.None);
                }
                catch { }
            }
            _wsClients.Clear();

            if (_tcpListener != null)
            {
                try
                {
                    _tcpListener.Stop();
                }
                catch (Exception ex)
                {
                    Log.Warning("WebSocketBridge: Error stopping tcp listener: {Err}", ex.Message);
                }
                _tcpListener = null;
            }

            if (_httpListener != null)
            {
                try
                {
                    _httpListener.Stop();
                }
                catch (ObjectDisposedException) { }
                catch (Exception ex)
                {
                    Log.Warning("WebSocketBridge: Error stopping http listener: {Err}", ex.Message);
                }

                try
                {
                    _httpListener.Close();
                }
                catch (ObjectDisposedException) { }
                catch (Exception ex)
                {
                    Log.Warning("WebSocketBridge: Error closing http listener: {Err}", ex.Message);
                }

                _httpListener = null;
            }
            IsRunning = false;

            Log.Information("WebSocketBridge: Stopped");
        }

        // ═══════════════════════════════════════════════════════════
        //  USER-MODE TCP HTTP/1.1 + WEBSOCKET SERVER (LAN & ANDROID)
        // ═══════════════════════════════════════════════════════════

        private async Task AcceptTcpLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var listener = _tcpListener;
                    if (listener == null) break;

                    TcpClient tcpClient;
                    try
                    {
                        tcpClient = await listener.AcceptTcpClientAsync(ct);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (ObjectDisposedException) { break; }
                    catch (SocketException) when (ct.IsCancellationRequested) { break; }
                    catch (Exception ex)
                    {
                        if (!ct.IsCancellationRequested)
                            Log.Warning("WebSocketBridge: TCP accept error: {Err}", ex.Message);
                        break;
                    }

                    _ = Task.Run(() => HandleTcpHttpConnectionAsync(tcpClient, ct), ct);
                }
            }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                    Log.Error("WebSocketBridge: Unexpected error in AcceptTcpLoop: {Err}", ex.Message);
            }
        }

        private async Task HandleTcpHttpConnectionAsync(TcpClient tcpClient, CancellationToken ct)
        {
            bool ownershipTransferred = false;
            try
            {
                tcpClient.NoDelay = true;
                string clientIP = (tcpClient.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "unknown";
                var stream = tcpClient.GetStream();

                // Đọc HTTP header tối đa 16KB với timeout 10s
                var headerBuf = new byte[16384];
                int totalRead = 0;
                int headerEndIdx = -1;
                using var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                readCts.CancelAfter(TimeSpan.FromSeconds(10));

                while (totalRead < headerBuf.Length)
                {
                    int n = await stream.ReadAsync(headerBuf.AsMemory(totalRead, headerBuf.Length - totalRead), readCts.Token);
                    if (n <= 0) return;
                    totalRead += n;
                    headerEndIdx = FindHeaderTerminator(headerBuf, totalRead);
                    if (headerEndIdx >= 0) break;
                }

                if (headerEndIdx < 0)
                {
                    SendSimpleTcpHttpResponse(stream, 400, "Bad Request", "text/plain; charset=utf-8", Encoding.UTF8.GetBytes("Header too large"));
                    return;
                }

                string rawHeaders = Encoding.ASCII.GetString(headerBuf, 0, headerEndIdx);
                string[] lines = rawHeaders.Split(new[] { "\r\n" }, StringSplitOptions.None);
                if (lines.Length == 0 || string.IsNullOrWhiteSpace(lines[0])) return;

                string[] reqLineParts = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (reqLineParts.Length < 2) return;

                string method = reqLineParts[0].ToUpperInvariant();
                string rawTarget = reqLineParts[1];

                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 1; i < lines.Length; i++)
                {
                    int colon = lines[i].IndexOf(':');
                    if (colon > 0)
                    {
                        string hName = lines[i].Substring(0, colon).Trim();
                        string hVal = lines[i].Substring(colon + 1).Trim();
                        headers[hName] = hVal;
                    }
                }

                if (method == "OPTIONS")
                {
                    SendSimpleTcpHttpResponse(stream, 204, "No Content", "text/plain", Array.Empty<byte>());
                    return;
                }

                // Tách path & query string
                string urlPath = rawTarget;
                var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                int qIdx = rawTarget.IndexOf('?');
                if (qIdx >= 0)
                {
                    urlPath = rawTarget.Substring(0, qIdx);
                    string qs = rawTarget.Substring(qIdx + 1);
                    foreach (var pair in qs.Split('&', StringSplitOptions.RemoveEmptyEntries))
                    {
                        int eq = pair.IndexOf('=');
                        if (eq > 0)
                        {
                            string k = Uri.UnescapeDataString(pair.Substring(0, eq).Replace('+', ' '));
                            string v = Uri.UnescapeDataString(pair.Substring(eq + 1).Replace('+', ' '));
                            query[k] = v;
                        }
                        else
                        {
                            query[Uri.UnescapeDataString(pair)] = "";
                        }
                    }
                }
                urlPath = Uri.UnescapeDataString(string.IsNullOrEmpty(urlPath) ? "/" : urlPath);

                // 1. Kiểm tra nâng cấp WebSocket (RFC 6455)
                if (headers.TryGetValue("Upgrade", out var upgradeVal) &&
                    upgradeVal.Contains("websocket", StringComparison.OrdinalIgnoreCase) &&
                    headers.TryGetValue("Sec-WebSocket-Key", out var secWsKey) &&
                    !string.IsNullOrWhiteSpace(secWsKey))
                {
                    string acceptKey = Convert.ToBase64String(
                        SHA1.HashData(Encoding.ASCII.GetBytes(secWsKey.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));

                    byte[] wsHandshake = Encoding.ASCII.GetBytes(
                        "HTTP/1.1 101 Switching Protocols\r\n" +
                        "Upgrade: websocket\r\n" +
                        "Connection: Upgrade\r\n" +
                        $"Sec-WebSocket-Accept: {acceptKey}\r\n\r\n");

                    await stream.WriteAsync(wsHandshake, ct);
                    await stream.FlushAsync(ct);

                    using var ws = WebSocket.CreateFromStream(
                        stream,
                        new WebSocketCreationOptions
                        {
                            IsServer = true,
                            KeepAliveInterval = TimeSpan.FromSeconds(30)
                        });

                    await RunWebSocketSessionAsync(ws, clientIP, ct);
                    return;
                }

                // 2. Endpoint kiểm tra nhanh trạng thái kết nối từ trình duyệt / thiết bị Android QA SmartTouch
                if (urlPath.Equals("/api/status", StringComparison.OrdinalIgnoreCase))
                {
                    var bState = QASmartClass.Services.BroadcastStateService.Instance;
                    byte[] statusJson = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
                    {
                        ok = true,
                        port = WebPort,
                        className = _className,
                        teacherName = _teacherName,
                        isBroadcasting = bState.IsScreenBroadcastActive,
                        isAudioActive = LiveBroadcastMediaService.Instance.IsAnyAudioActive,
                        isCameraActive = LiveBroadcastMediaService.Instance.IsCameraEnabled,
                        clientIP
                    }));
                    SendSimpleTcpHttpResponse(stream, 200, "OK", "application/json; charset=utf-8", statusJson);
                    return;
                }

                // 3. Endpoint đẩy khung hình tốc độ cao /api/broadcast_stream (dành cho Android Emulator, QA SmartTouch IFP & LAN)
                if (urlPath.Equals("/api/broadcast_stream", StringComparison.OrdinalIgnoreCase))
                {
                    string streamToken = query.TryGetValue("token", out var stTok) ? stTok : "";
                    string streamServerToken = QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken;
                    if (!string.IsNullOrEmpty(streamServerToken) && streamToken != streamServerToken)
                    {
                        SendSimpleTcpHttpResponse(stream, 401, "Unauthorized", "text/plain; charset=utf-8",
                            Encoding.UTF8.GetBytes("401 Unauthorized - Invalid Broadcast Token"));
                        return;
                    }

                    ownershipTransferred = true;
                    _ = Task.Factory.StartNew(
                        () => StreamBroadcastFramesTcp(tcpClient, stream, clientIP, ct),
                        TaskCreationOptions.LongRunning);
                    return;
                }

                // 3b. Endpoint đẩy luồng âm thanh độ trễ thấp /api/audio_stream (24kHz 16-bit PCM Mono — Hệ thống + Micro Giáo viên)
                if (urlPath.Equals("/api/audio_stream", StringComparison.OrdinalIgnoreCase))
                {
                    string audioToken = query.TryGetValue("token", out var auTok) ? auTok : "";
                    string audioServerToken = QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken;
                    if (!string.IsNullOrEmpty(audioServerToken) && audioToken != audioServerToken)
                    {
                        SendSimpleTcpHttpResponse(stream, 401, "Unauthorized", "text/plain; charset=utf-8",
                            Encoding.UTF8.GetBytes("401 Unauthorized - Invalid Broadcast Token"));
                        return;
                    }

                    ownershipTransferred = true;
                    _ = Task.Factory.StartNew(
                        () => StreamBroadcastAudioTcp(tcpClient, stream, clientIP, ct),
                        TaskCreationOptions.LongRunning);
                    return;
                }

                // 4. Endpoint lấy ảnh đơn /api/broadcast_image (HTTP Pull fallback cho Android & Windows PC)
                if (urlPath.Equals("/api/broadcast_image", StringComparison.OrdinalIgnoreCase))
                {
                    string clientToken = query.TryGetValue("token", out var imgTok) ? imgTok : "";
                    string serverToken = QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken;
                    if (!string.IsNullOrEmpty(serverToken) && clientToken != serverToken)
                    {
                        SendSimpleTcpHttpResponse(stream, 401, "Unauthorized", "text/plain; charset=utf-8",
                            Encoding.UTF8.GetBytes("401 Unauthorized - Invalid Broadcast Token"));
                        Log.Warning("WebSocketBridge: Unauthorized request from IP {IP} for /api/broadcast_image (invalid token)", clientIP);
                        return;
                    }

                    var nowTime = DateTime.Now;
                    var limitTime = nowTime.AddMinutes(-1);
                    var requestList = _clientRequestTimes.GetOrAdd(clientIP, _ => new List<DateTime>());
                    lock (requestList)
                    {
                        requestList.RemoveAll(t => t < limitTime);
                        if (requestList.Count >= 1800)
                        {
                            SendSimpleTcpHttpResponse(stream, 429, "Too Many Requests", "text/plain; charset=utf-8",
                                Encoding.UTF8.GetBytes("429 Too Many Requests"));
                            return;
                        }
                        requestList.Add(nowTime);
                    }

                    byte[] content = QASmartClass.Services.BroadcastStateService.Instance.ScreenCaptureBytes;
                    if (content == null || content.Length == 0)
                    {
                        string path = QASmartClass.Services.BroadcastStateService.Instance.ScreenCapturePath;
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            content = File.ReadAllBytes(path);
                        }
                    }

                    if (content != null && content.Length > 0)
                    {
                        SendSimpleTcpHttpResponse(stream, 200, "OK", "image/jpeg", content);
                    }
                    else
                    {
                        SendSimpleTcpHttpResponse(stream, 404, "Not Found", "text/plain; charset=utf-8",
                            Encoding.UTF8.GetBytes("No active screen broadcast"));
                    }
                    return;
                }

                // 5a. Endpoint tải tệp luồng 64KB chunked /api/files/download (GV → HS Windows PC & Android, hỗ trợ HTTP Range Resume, ~0 MB RAM)
                if (urlPath.Equals("/api/files/download", StringComparison.OrdinalIgnoreCase))
                {
                    string clientToken = query.TryGetValue("token", out var dlTok) ? dlTok : "";
                    string serverToken = QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken;
                    if (!string.IsNullOrEmpty(serverToken) && clientToken != serverToken)
                    {
                        SendSimpleTcpHttpResponse(stream, 401, "Unauthorized", "text/plain; charset=utf-8",
                            Encoding.UTF8.GetBytes("401 Unauthorized - Invalid File Token"));
                        return;
                    }

                    string fileId = query.TryGetValue("id", out var fid) ? fid : "";
                    string reqName = query.TryGetValue("name", out var fnm) ? fnm : "";
                    if (!QASmartClass.Services.BroadcastStateService.Instance.TryResolveSharedFile(fileId, reqName, out var desc) || desc == null)
                    {
                        SendSimpleTcpHttpResponse(stream, 404, "Not Found", "text/plain; charset=utf-8",
                            Encoding.UTF8.GetBytes("404 File Not Found"));
                        return;
                    }

                    string? rangeHeader = headers.TryGetValue("Range", out var rng) ? rng : null;
                    await StreamFileDownloadHttpAsync(stream, desc.FilePath, desc.FileName, asAttachment: true, rangeHeader, ct);
                    return;
                }

                // 5b. Endpoint nhận tệp nộp bài luồng 64KB chunked /api/files/upload (HS Windows PC & Android → GV, ~0 MB RAM)
                if (urlPath.Equals("/api/files/upload", StringComparison.OrdinalIgnoreCase) && method == "POST")
                {
                    int bodyStartOffset = headerEndIdx + 4;
                    int bodyBytesAlreadyRead = Math.Max(0, totalRead - bodyStartOffset);
                    await HandleHttpFileUploadAsync(stream, headers, query, headerBuf, bodyStartOffset, bodyBytesAlreadyRead, clientIP, ct);
                    return;
                }

                // 5. Endpoint phát file /api/file_broadcast (nâng cấp sang 64KB FileStream chunked để tránh tràn RAM file lớn)
                if (urlPath.Equals("/api/file_broadcast", StringComparison.OrdinalIgnoreCase))
                {
                    string clientToken = query.TryGetValue("token", out var fTok) ? fTok : "";
                    string serverToken = QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken;
                    if (!string.IsNullOrEmpty(serverToken) && clientToken != serverToken)
                    {
                        SendSimpleTcpHttpResponse(stream, 401, "Unauthorized", "text/plain; charset=utf-8",
                            Encoding.UTF8.GetBytes("401 Unauthorized - Invalid Broadcast Token"));
                        return;
                    }

                    string path = QASmartClass.Services.BroadcastStateService.Instance.FileBroadcastPath;
                    string reqName = query.TryGetValue("name", out var nm) ? nm : "";
                    if (!string.IsNullOrEmpty(reqName))
                    {
                        var filesDict = QASmartClass.Services.BroadcastStateService.Instance.ActiveBroadcastFiles;
                        lock (filesDict)
                        {
                            if (filesDict.TryGetValue(reqName, out var matchedPath))
                            {
                                path = matchedPath;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        // [MYTHWARE_LITE] byte[] content = File.ReadAllBytes(path);
                        // [MYTHWARE_LITE] SendSimpleTcpHttpResponse(stream, 200, "OK", GetMimeType(path), content);
                        string? rangeHeader = headers.TryGetValue("Range", out var rng2) ? rng2 : null;
                        await StreamFileDownloadHttpAsync(stream, path, Path.GetFileName(path), asAttachment: false, rangeHeader, ct);
                    }
                    else
                    {
                        SendSimpleTcpHttpResponse(stream, 404, "Not Found", "text/plain; charset=utf-8",
                            Encoding.UTF8.GetBytes("No active file broadcast"));
                    }
                    return;
                }

                // 6. Phục vụ file tĩnh StudentWebApp
                if (urlPath == "/") urlPath = "/index.html";
                string filePath = Path.Combine(_webRootPath, urlPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                string fullRoot = Path.GetFullPath(_webRootPath);
                if (!fullRoot.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    fullRoot += Path.DirectorySeparatorChar;

                string fullTarget = Path.GetFullPath(filePath);
                if (!fullTarget.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                {
                    SendSimpleTcpHttpResponse(stream, 403, "Forbidden", "text/plain; charset=utf-8",
                        Encoding.UTF8.GetBytes("403 Forbidden"));
                    return;
                }

                if (File.Exists(fullTarget))
                {
                    byte[] content = File.ReadAllBytes(fullTarget);
                    SendSimpleTcpHttpResponse(stream, 200, "OK", GetMimeType(fullTarget), content);
                }
                else
                {
                    SendSimpleTcpHttpResponse(stream, 404, "Not Found", "text/plain; charset=utf-8",
                        Encoding.UTF8.GetBytes("404 Not Found"));
                }
            }
            catch (Exception ex) when (ex is IOException || ex is SocketException || ex is OperationCanceledException || ex is ObjectDisposedException)
            {
                // Kết nối đóng tự nhiên từ phía client
            }
            catch (Exception ex)
            {
                Log.Debug("WebSocketBridge: TCP HTTP connection error: {Err}", ex.Message);
            }
            finally
            {
                if (!ownershipTransferred)
                {
                    try { tcpClient.Close(); } catch { }
                    try { tcpClient.Dispose(); } catch { }
                }
            }
        }

        private static int FindHeaderTerminator(byte[] buf, int length)
        {
            for (int i = 0; i <= length - 4; i++)
            {
                if (buf[i] == (byte)'\r' &&
                    buf[i + 1] == (byte)'\n' &&
                    buf[i + 2] == (byte)'\r' &&
                    buf[i + 3] == (byte)'\n')
                {
                    return i;
                }
            }
            return -1;
        }

        private static void SendSimpleTcpHttpResponse(NetworkStream stream, int statusCode, string statusText, string contentType, byte[] body)
        {
            string header =
                $"HTTP/1.1 {statusCode} {statusText}\r\n" +
                $"Content-Type: {contentType}\r\n" +
                $"Content-Length: {body.Length}\r\n" +
                "Access-Control-Allow-Origin: *\r\n" +
                "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n" +
                "Access-Control-Allow-Headers: *\r\n" +
                "Cache-Control: no-cache, no-store\r\n" +
                "Connection: close\r\n\r\n";

            byte[] headerBytes = Encoding.ASCII.GetBytes(header);
            stream.Write(headerBytes, 0, headerBytes.Length);
            if (body.Length > 0)
            {
                stream.Write(body, 0, body.Length);
            }
            stream.Flush();
        }

        private async Task StreamFileDownloadHttpAsync(
            NetworkStream stream,
            string filePath,
            string downloadFileName,
            bool asAttachment,
            string? rangeHeader,
            CancellationToken ct)
        {
            await _fileStreamLimiter.WaitAsync(ct);
            try
            {
                var fi = new FileInfo(filePath);
                if (!fi.Exists)
                {
                    SendSimpleTcpHttpResponse(stream, 404, "Not Found", "text/plain; charset=utf-8",
                        Encoding.UTF8.GetBytes("404 File Not Found"));
                    return;
                }

                long totalLength = fi.Length;
                long start = 0;
                long end = Math.Max(0, totalLength - 1);
                bool isPartial = false;

                if (!string.IsNullOrWhiteSpace(rangeHeader) &&
                    rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase) &&
                    totalLength > 0)
                {
                    string spec = rangeHeader.Substring(6).Trim();
                    int dash = spec.IndexOf('-');
                    if (dash >= 0)
                    {
                        string startPart = spec.Substring(0, dash).Trim();
                        string endPart = spec.Substring(dash + 1).Trim();
                        if (long.TryParse(startPart, out long sVal) && sVal >= 0 && sVal < totalLength)
                        {
                            start = sVal;
                            if (long.TryParse(endPart, out long eVal) && eVal >= start && eVal < totalLength)
                                end = eVal;
                            isPartial = true;
                        }
                    }
                }

                long contentLength = totalLength == 0 ? 0 : (end - start + 1);
                string mimeType = GetMimeType(filePath);
                string dispType = asAttachment ? "attachment" : "inline";
                string safeAsciiName = string.Concat(downloadFileName.Select(ch => ch >= 32 && ch <= 126 && ch != '"' && ch != '\\' ? ch : '_'));
                string encodedUtf8Name = Uri.EscapeDataString(downloadFileName);

                var sb = new StringBuilder(512);
                sb.Append(isPartial ? "HTTP/1.1 206 Partial Content\r\n" : "HTTP/1.1 200 OK\r\n");
                sb.Append($"Content-Type: {mimeType}\r\n");
                sb.Append($"Content-Length: {contentLength}\r\n");
                sb.Append("Accept-Ranges: bytes\r\n");
                if (isPartial)
                {
                    sb.Append($"Content-Range: bytes {start}-{end}/{totalLength}\r\n");
                }
                sb.Append($"Content-Disposition: {dispType}; filename=\"{safeAsciiName}\"; filename*=UTF-8''{encodedUtf8Name}\r\n");
                sb.Append("Access-Control-Allow-Origin: *\r\n");
                sb.Append("Cache-Control: no-cache, no-store\r\n");
                sb.Append("Connection: close\r\n\r\n");

                byte[] headerBytes = Encoding.ASCII.GetBytes(sb.ToString());
                await stream.WriteAsync(headerBytes, ct);

                if (contentLength > 0)
                {
                    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536, useAsync: true);
                    if (start > 0) fs.Seek(start, SeekOrigin.Begin);

                    var buf = new byte[65536];
                    long remaining = contentLength;
                    while (remaining > 0 && !ct.IsCancellationRequested)
                    {
                        int toRead = (int)Math.Min(buf.Length, remaining);
                        int read = await fs.ReadAsync(buf.AsMemory(0, toRead), ct);
                        if (read <= 0) break;
                        await stream.WriteAsync(buf.AsMemory(0, read), ct);
                        remaining -= read;
                    }
                }
                await stream.FlushAsync(ct);
            }
            finally
            {
                _fileStreamLimiter.Release();
            }
        }

        private async Task HandleHttpFileUploadAsync(
            NetworkStream stream,
            Dictionary<string, string> headers,
            Dictionary<string, string> query,
            byte[] headerBuf,
            int bodyStartOffset,
            int bodyBytesAlreadyRead,
            string clientIP,
            CancellationToken ct)
        {
            await _fileStreamLimiter.WaitAsync(ct);
            try
            {
                string studentCode = query.TryGetValue("code", out var qc) ? qc
                    : (headers.TryGetValue("X-Student-Code", out var hc) ? hc : "HS00001");
                string studentName = query.TryGetValue("name", out var qn) ? qn
                    : (headers.TryGetValue("X-Student-Name", out var hn) ? Uri.UnescapeDataString(hn) : "Hoc_sinh");
                string fileName = query.TryGetValue("filename", out var qf) ? qf
                    : (headers.TryGetValue("X-File-Name", out var hf) ? Uri.UnescapeDataString(hf) : "BaiLam.dat");
                string clientToken = query.TryGetValue("token", out var qt) ? qt
                    : (headers.TryGetValue("X-Class-Token", out var ht) ? ht : "");

                string serverToken = QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken;
                bool isLocal = clientIP == "127.0.0.1" || clientIP == "::1";
                bool isConnectedStudent = _networkService.GetConnectedStudents().Any(s =>
                    string.Equals(s.Code, studentCode, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s.IPAddress, clientIP, StringComparison.OrdinalIgnoreCase));

                if (!isLocal && !isConnectedStudent && !string.IsNullOrEmpty(serverToken) && clientToken != serverToken)
                {
                    byte[] unauthBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
                    {
                        ok = false,
                        message = "Chưa xác thực kết nối lớp học. Vui lòng kiểm tra kết nối tới máy Giáo viên."
                    }));
                    SendSimpleTcpHttpResponse(stream, 401, "Unauthorized", "application/json; charset=utf-8", unauthBody);
                    return;
                }

                long contentLength = 0;
                if (headers.TryGetValue("Content-Length", out var clStr))
                {
                    long.TryParse(clStr, out contentLength);
                }

                var ftService = (System.Windows.Application.Current as QASmartTouch.App)?.FileTransfer;
                if (ftService == null)
                {
                    byte[] errBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
                    {
                        ok = false,
                        message = "Dịch vụ nhận bài của Giáo viên chưa sẵn sàng."
                    }));
                    SendSimpleTcpHttpResponse(stream, 503, "Service Unavailable", "application/json; charset=utf-8", errBody);
                    return;
                }

                stream.ReadTimeout = 120000; // Cho phép tối đa 120s cho file lớn qua Wi-Fi
                var result = await ftService.SaveIncomingHttpUploadAsync(
                    stream,
                    headerBuf,
                    bodyStartOffset,
                    bodyBytesAlreadyRead,
                    contentLength,
                    studentCode,
                    studentName,
                    fileName,
                    ct);

                string statusText = result.StatusCode switch
                {
                    200 => "OK",
                    400 => "Bad Request",
                    403 => "Forbidden",
                    413 => "Payload Too Large",
                    507 => "Insufficient Storage",
                    _ => "Internal Server Error"
                };

                byte[] jsonResp = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
                {
                    ok = result.Success,
                    savedAs = result.SavedFileName,
                    message = result.Message
                }));
                SendSimpleTcpHttpResponse(stream, result.StatusCode, statusText, "application/json; charset=utf-8", jsonResp);
            }
            finally
            {
                _fileStreamLimiter.Release();
            }
        }

        private void StreamBroadcastFramesTcp(TcpClient tcpClient, NetworkStream stream, string clientIP, CancellationToken ct)
        {
            try
            {
                tcpClient.NoDelay = true;
                tcpClient.SendBufferSize = 262144;
                stream.WriteTimeout = 8000;

                Log.Information("WebSocketBridge: Client {IP} connected to /api/broadcast_stream", clientIP);

                byte[] respHeader = Encoding.ASCII.GetBytes(
                    "HTTP/1.1 200 OK\r\n" +
                    "Content-Type: application/octet-stream\r\n" +
                    "Transfer-Encoding: chunked\r\n" +
                    "Cache-Control: no-cache, no-store\r\n" +
                    "Access-Control-Allow-Origin: *\r\n" +
                    "Connection: close\r\n\r\n");

                stream.Write(respHeader, 0, respHeader.Length);
                stream.Flush();

                long lastVersion = -1;
                var prefixBuf = new byte[32];
                byte[] crlf = new byte[] { (byte)'\r', (byte)'\n' };
                byte[] endChunk = Encoding.ASCII.GetBytes("0\r\n\r\n");
                var idleSince = DateTime.UtcNow;
                var lastSentUtc = DateTime.MinValue;

                while (!ct.IsCancellationRequested)
                {
                    var state = QASmartClass.Services.BroadcastStateService.Instance;
                    var now = DateTime.UtcNow;
                    double sinceLastSentMs = (now - lastSentUtc).TotalMilliseconds;

                    bool hasNextSequential = state.TryGetNextSequentialFrame(ref lastVersion, out byte[] frame, out _, maxAllowedLag: 30);
                    bool needKeepAlive = !hasNextSequential && state.IsScreenBroadcastActive && sinceLastSentMs >= 1500;
                    if (needKeepAlive)
                    {
                        frame = state.ScreenCaptureBytes;
                    }

                    if ((hasNextSequential || needKeepAlive) && frame != null && frame.Length > 0)
                    {
                        idleSince = now;
                        int payloadLen = 4 + frame.Length;
                        string chunkHex = payloadLen.ToString("X");
                        int pIdx = 0;
                        for (int i = 0; i < chunkHex.Length; i++)
                        {
                            prefixBuf[pIdx++] = (byte)chunkHex[i];
                        }
                        prefixBuf[pIdx++] = (byte)'\r';
                        prefixBuf[pIdx++] = (byte)'\n';
                        prefixBuf[pIdx++] = (byte)(frame.Length >> 24);
                        prefixBuf[pIdx++] = (byte)(frame.Length >> 16);
                        prefixBuf[pIdx++] = (byte)(frame.Length >> 8);
                        prefixBuf[pIdx++] = (byte)frame.Length;

                        stream.Write(prefixBuf, 0, pIdx);
                        stream.Write(frame, 0, frame.Length);
                        stream.Write(crlf, 0, 2);
                        stream.Flush();

                        lastSentUtc = DateTime.UtcNow;
                        Thread.Sleep(4);
                        continue;
                    }

                    if (!state.IsScreenBroadcastActive && (now - idleSince).TotalSeconds > 5) break;
                    if ((now - idleSince).TotalSeconds > 20) break;
                    Thread.Sleep(4);
                }

                stream.Write(endChunk, 0, endChunk.Length);
                stream.Flush();
            }
            catch (Exception ex)
            {
                Log.Debug("WebSocketBridge: broadcast_stream closed for {IP}: {Err}", clientIP, ex.Message);
            }
            finally
            {
                try { tcpClient.Close(); } catch { }
                try { tcpClient.Dispose(); } catch { }
            }
        }

        private void StreamBroadcastAudioTcp(TcpClient tcpClient, NetworkStream stream, string clientIP, CancellationToken ct)
        {
            try
            {
                tcpClient.NoDelay = true;
                tcpClient.SendBufferSize = 32768;
                stream.WriteTimeout = 8000;

                Log.Information("WebSocketBridge: Client {IP} connected to /api/audio_stream (24kHz PCM16 Mono)", clientIP);

                byte[] respHeader = Encoding.ASCII.GetBytes(
                    "HTTP/1.1 200 OK\r\n" +
                    "Content-Type: application/octet-stream\r\n" +
                    "Transfer-Encoding: chunked\r\n" +
                    "Cache-Control: no-cache, no-store\r\n" +
                    "Access-Control-Allow-Origin: *\r\n" +
                    "Connection: close\r\n\r\n");

                stream.Write(respHeader, 0, respHeader.Length);
                stream.Flush();

                long audioCursorSeq = -1;
                var prefixBuf = new byte[16];
                byte[] crlf = new byte[] { (byte)'\r', (byte)'\n' };
                byte[] endChunk = Encoding.ASCII.GetBytes("0\r\n\r\n");
                byte[] keepAliveSilence = new byte[LiveBroadcastMediaService.BYTES_PER_PACKET];
                var stoppedSince = DateTime.UtcNow;
                var lastSentUtc = DateTime.UtcNow;

                while (!ct.IsCancellationRequested)
                {
                    var state = QASmartClass.Services.BroadcastStateService.Instance;
                    var now = DateTime.UtcNow;

                    if (state.IsScreenBroadcastActive)
                    {
                        stoppedSince = now;
                    }
                    else if ((now - stoppedSince).TotalSeconds > 4)
                    {
                        break;
                    }

                    bool hasPacket = LiveBroadcastMediaService.Instance.TryGetNextAudioPacket(ref audioCursorSeq, out byte[] pcm);
                    bool needKeepAlive = !hasPacket && state.IsScreenBroadcastActive && (now - lastSentUtc).TotalMilliseconds >= 1500;
                    if (needKeepAlive)
                    {
                        pcm = keepAliveSilence;
                    }

                    if ((hasPacket || needKeepAlive) && pcm != null && pcm.Length > 0)
                    {
                        int payloadLen = 2 + pcm.Length;
                        string chunkHex = payloadLen.ToString("X");
                        int pIdx = 0;
                        for (int i = 0; i < chunkHex.Length; i++)
                        {
                            prefixBuf[pIdx++] = (byte)chunkHex[i];
                        }
                        prefixBuf[pIdx++] = (byte)'\r';
                        prefixBuf[pIdx++] = (byte)'\n';
                        prefixBuf[pIdx++] = (byte)((pcm.Length >> 8) & 0xFF);
                        prefixBuf[pIdx++] = (byte)(pcm.Length & 0xFF);

                        stream.Write(prefixBuf, 0, pIdx);
                        stream.Write(pcm, 0, pcm.Length);
                        stream.Write(crlf, 0, 2);
                        stream.Flush();

                        lastSentUtc = DateTime.UtcNow;
                        continue;
                    }

                    Thread.Sleep(5);
                }

                stream.Write(endChunk, 0, endChunk.Length);
                stream.Flush();
            }
            catch (Exception ex)
            {
                Log.Debug("WebSocketBridge: audio_stream closed for {IP}: {Err}", clientIP, ex.Message);
            }
            finally
            {
                try { tcpClient.Close(); } catch { }
                try { tcpClient.Dispose(); } catch { }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  HTTP REQUEST HANDLER
        // ═══════════════════════════════════════════════════════════

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    HttpListener? listener = _httpListener;
                    if (listener == null) break;

                    try
                    {
                        if (!listener.IsListening) break;
                    }
                    catch (ObjectDisposedException) { break; }

                    try
                    {
                        var context = await listener.GetContextAsync();

                        if (context.Request.IsWebSocketRequest)
                        {
                            _ = HandleWebSocketAsync(context, ct);
                        }
                        else
                        {
                            _ = Task.Run(() => ServeStaticFile(context), ct);
                        }
                    }
                    catch (ObjectDisposedException) { break; }
                    catch (HttpListenerException) { break; }
                    catch (Exception ex) when (!ct.IsCancellationRequested)
                    {
                        Log.Warning("WebSocketBridge: Accept error: {Err}", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("WebSocketBridge: Unexpected error in AcceptLoop: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  STATIC FILE SERVER
        // ═══════════════════════════════════════════════════════════

        private void StreamBroadcastFrames(HttpListenerContext context, string clientIP)
        {
            var response = context.Response;
            try
            {
                response.StatusCode = 200;
                response.SendChunked = true;
                response.ContentType = "application/octet-stream";
                response.Headers.Add("Cache-Control", "no-cache");
                response.Headers.Add("Access-Control-Allow-Origin", "*");

                var output = response.OutputStream;
                long lastVersion = -1;
                var header = new byte[4];
                var idleSince = DateTime.UtcNow;
                var lastSentUtc = DateTime.MinValue;

                while (true)
                {
                    var state = QASmartClass.Services.BroadcastStateService.Instance;
                    var now = DateTime.UtcNow;
                    double sinceLastSentMs = (now - lastSentUtc).TotalMilliseconds;

                    // Lấy tuần tự TỪNG KHUNG HÌNH trong Ring Buffer; nếu 1 máy Wi-Fi yếu tụt lại > 30 khung (~1s) thì tự bắt kịp êm tới Keyframe siêu nét mới nhất
                    bool hasNextSequential = state.TryGetNextSequentialFrame(ref lastVersion, out byte[] frame, out _, maxAllowedLag: 30);
                    bool needKeepAlive = !hasNextSequential && state.IsScreenBroadcastActive && sinceLastSentMs >= 1500;
                    if (needKeepAlive)
                    {
                        frame = state.ScreenCaptureBytes;
                    }

                    if ((hasNextSequential || needKeepAlive) && frame != null && frame.Length > 0)
                    {
                        idleSince = now;
                        header[0] = (byte)(frame.Length >> 24);
                        header[1] = (byte)(frame.Length >> 16);
                        header[2] = (byte)(frame.Length >> 8);
                        header[3] = (byte)frame.Length;
                        output.Write(header, 0, 4);
                        output.Write(frame, 0, frame.Length);
                        output.Flush();

                        lastSentUtc = DateTime.UtcNow;
                        // Nhịp đẩy tốc độ cao (hỗ trợ 60 FPS - 120 FPS mượt mà theo từng frame trong Ring Buffer)
                        Thread.Sleep(4);
                        continue;
                    }

                    // Không còn phát và không có khung mới trong 20s -> đóng luồng
                    if (!state.IsScreenBroadcastActive && (now - idleSince).TotalSeconds > 5) break;
                    if ((now - idleSince).TotalSeconds > 20) break;
                    Thread.Sleep(4);
                }
            }
            catch (Exception ex)
            {
                // Client ngắt kết nối là bình thường
                Log.Debug("WebSocketBridge: broadcast_stream closed for {IP}: {Err}", clientIP, ex.Message);
            }
            finally
            {
                try { response.Close(); } catch { }
            }
        }

        private void ServeStaticFile(HttpListenerContext context)
        {
            var urlPath = context.Request.Url?.AbsolutePath ?? "/";
            string clientIP = context.Request.RemoteEndPoint?.Address.ToString() ?? "unknown";

            // === API Route: Broadcast Stream (đẩy khung liên tục, độ trễ thấp cho Android) ===
            // Khung: [4 byte độ dài big-endian][dữ liệu JPEG], lặp lại mỗi khi có khung mới.
            if (urlPath.Equals("/api/broadcast_stream", StringComparison.OrdinalIgnoreCase))
            {
                string streamToken = context.Request.QueryString["token"] ?? "";
                string streamServerToken = QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken;
                if (!string.IsNullOrEmpty(streamServerToken) && streamToken != streamServerToken)
                {
                    context.Response.StatusCode = 401;
                    context.Response.Close();
                    return;
                }

                // Giữ kết nối dài: chạy trên thread riêng để không làm cạn ThreadPool khi lớp đông
                _ = Task.Factory.StartNew(() => StreamBroadcastFrames(context, clientIP), TaskCreationOptions.LongRunning);
                return;
            }

            // === API Route: Broadcast Image ===
            if (urlPath.Equals("/api/broadcast_image", StringComparison.OrdinalIgnoreCase))
            {
                var apiResponse = context.Response;
                
                // Rate Limiting Check (Max 1200 requests per minute per client IP to support up to 15 FPS)
                var nowTime = DateTime.Now;
                var limitTime = nowTime.AddMinutes(-1);

                // Token Verification Check
                string clientToken = context.Request.QueryString["token"] ?? "";
                string serverToken = QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken;
                if (!string.IsNullOrEmpty(serverToken) && clientToken != serverToken)
                {
                    apiResponse.StatusCode = 401; // Unauthorized
                    apiResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                    byte[] unauthorized = Encoding.UTF8.GetBytes("401 Unauthorized - Invalid Broadcast Token");
                    apiResponse.OutputStream.Write(unauthorized, 0, unauthorized.Length);
                    apiResponse.Close();
                    Log.Warning("WebSocketBridge: Unauthorized request from IP {IP} for /api/broadcast_image (invalid token)", clientIP);
                    return;
                }

                var requestList = _clientRequestTimes.GetOrAdd(clientIP, _ => new System.Collections.Generic.List<DateTime>());
                lock (requestList)
                {
                    requestList.RemoveAll(t => t < limitTime);
                    if (requestList.Count >= 1200)
                    {
                        apiResponse.StatusCode = 429; // Too Many Requests
                        apiResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                        byte[] tooMany = Encoding.UTF8.GetBytes("429 Too Many Requests - Rate Limit Exceeded");
                        apiResponse.OutputStream.Write(tooMany, 0, tooMany.Length);
                        apiResponse.Close();
                        Log.Warning("WebSocketBridge: Rate limit exceeded for IP {IP} on /api/broadcast_image", clientIP);
                        return;
                    }
                    requestList.Add(nowTime);
                }

                try
                {
                    byte[] content = QASmartClass.Services.BroadcastStateService.Instance.ScreenCaptureBytes;
                    if (content == null || content.Length == 0)
                    {
                        string path = QASmartClass.Services.BroadcastStateService.Instance.ScreenCapturePath;
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            content = File.ReadAllBytes(path);
                        }
                    }

                    if (content != null && content.Length > 0)
                    {
                        apiResponse.ContentType = "image/jpeg";
                        apiResponse.ContentLength64 = content.Length;
                        apiResponse.StatusCode = 200;
                        apiResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                        apiResponse.Headers.Add("Cache-Control", "no-cache");
                        apiResponse.OutputStream.Write(content, 0, content.Length);
                    }
                    else
                    {
                        apiResponse.StatusCode = 404;
                        byte[] notFound = Encoding.UTF8.GetBytes("No active screen broadcast");
                        apiResponse.OutputStream.Write(notFound, 0, notFound.Length);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("API broadcast image error: {Err}", ex.Message);
                    apiResponse.StatusCode = 500;
                }
                finally
                {
                    apiResponse.Close();
                }
                return;
            }

            // === API Route: File Broadcast ===
            if (urlPath.Equals("/api/file_broadcast", StringComparison.OrdinalIgnoreCase))
            {
                var apiResponse = context.Response;
                try
                {
                    // Token Verification Check
                    string clientToken = context.Request.QueryString["token"] ?? "";
                    string serverToken = QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken;
                    if (!string.IsNullOrEmpty(serverToken) && clientToken != serverToken)
                    {
                        apiResponse.StatusCode = 401; // Unauthorized
                        apiResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                        byte[] unauthorized = Encoding.UTF8.GetBytes("401 Unauthorized - Invalid Broadcast Token");
                        apiResponse.OutputStream.Write(unauthorized, 0, unauthorized.Length);
                        apiResponse.Close();
                        Log.Warning("WebSocketBridge: Unauthorized file broadcast request from IP {IP} (invalid token)", clientIP);
                        return;
                    }

                    string path = QASmartClass.Services.BroadcastStateService.Instance.FileBroadcastPath;
                    string reqName = context.Request.QueryString["name"] ?? "";
                    if (!string.IsNullOrEmpty(reqName))
                    {
                        var filesDict = QASmartClass.Services.BroadcastStateService.Instance.ActiveBroadcastFiles;
                        lock (filesDict)
                        {
                            if (filesDict.TryGetValue(reqName, out var matchedPath))
                            {
                                path = matchedPath;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        byte[] content = File.ReadAllBytes(path);
                        apiResponse.ContentType = GetMimeType(path);
                        apiResponse.ContentLength64 = content.Length;
                        apiResponse.StatusCode = 200;
                        apiResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                        apiResponse.Headers.Add("Cache-Control", "no-cache");
                        apiResponse.OutputStream.Write(content, 0, content.Length);
                    }
                    else
                    {
                        apiResponse.StatusCode = 404;
                        byte[] notFound = Encoding.UTF8.GetBytes("No active file broadcast");
                        apiResponse.OutputStream.Write(notFound, 0, notFound.Length);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("API file broadcast error: {Err}", ex.Message);
                    apiResponse.StatusCode = 500;
                }
                finally
                {
                    apiResponse.Close();
                }
                return;
            }

            if (urlPath == "/") urlPath = "/index.html";

            var filePath = Path.Combine(_webRootPath, urlPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            var response = context.Response;

            // === Security Check: Directory Traversal Protection ===
            try
            {
                string fullRoot = Path.GetFullPath(_webRootPath);
                if (!fullRoot.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    fullRoot += Path.DirectorySeparatorChar;

                string fullTarget = Path.GetFullPath(filePath);
                if (!fullTarget.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                {
                    response.StatusCode = 403;
                    byte[] forbidden = Encoding.UTF8.GetBytes("403 Forbidden - Access Denied");
                    response.OutputStream.Write(forbidden, 0, forbidden.Length);
                    response.Close();
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Path normalization error: {Err}", ex.Message);
                response.StatusCode = 400;
                response.Close();
                return;
            }

            try
            {
                if (File.Exists(filePath))
                {
                    var content = File.ReadAllBytes(filePath);
                    response.ContentType = GetMimeType(filePath);
                    response.ContentLength64 = content.Length;
                    response.StatusCode = 200;

                    // CORS headers for LAN access
                    response.Headers.Add("Access-Control-Allow-Origin", "*");
                    response.Headers.Add("Cache-Control", "no-cache");

                    response.OutputStream.Write(content, 0, content.Length);
                }
                else
                {
                    response.StatusCode = 404;
                    var notFound = Encoding.UTF8.GetBytes("404 Not Found");
                    response.OutputStream.Write(notFound, 0, notFound.Length);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Static file error: {Path} — {Err}", urlPath, ex.Message);
                response.StatusCode = 500;
            }
            finally
            {
                response.Close();
            }
        }

        private static string GetMimeType(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".html" => "text/html; charset=utf-8",
                ".css" => "text/css; charset=utf-8",
                ".js" => "application/javascript; charset=utf-8",
                ".json" => "application/json; charset=utf-8",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",
                ".woff" => "font/woff",
                ".woff2" => "font/woff2",
                ".ttf" => "font/ttf",
                _ => "application/octet-stream"
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  WEBSOCKET HANDLER
        // ═══════════════════════════════════════════════════════════

        private async Task HandleWebSocketAsync(HttpListenerContext httpContext, CancellationToken ct)
        {
            try
            {
                var wsContext = await httpContext.AcceptWebSocketAsync(null);
                var remoteIP = httpContext.Request.RemoteEndPoint?.Address.ToString() ?? "?";
                await RunWebSocketSessionAsync(wsContext.WebSocket, remoteIP, ct);
            }
            catch (Exception ex)
            {
                Log.Warning("WebSocket accept error: {Err}", ex.Message);
            }
        }

        private async Task RunWebSocketSessionAsync(WebSocket ws, string remoteIP, CancellationToken ct)
        {
            WebSocketClient? client = null;

            try
            {
                Log.Information("WebSocket client connected from {IP}", remoteIP);

                var buffer = new byte[524288]; // 512KB — enough for screenshot base64 (was 128KB)

                // ── First message must be JOIN ──
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                if (result.MessageType == WebSocketMessageType.Close) return;

                var joinMsg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var joinData = JsonSerializer.Deserialize<JsonElement>(joinMsg);

                if (joinData.GetProperty("type").GetString() != "join")
                {
                    Log.Warning("WebSocket: Invalid handshake from {IP}", remoteIP);
                    await ws.CloseAsync(WebSocketCloseStatus.ProtocolError, "Expected JOIN", ct);
                    return;
                }

                // Parse student info
                client = new WebSocketClient
                {
                    Socket = ws,
                    Name = joinData.TryGetProperty("name", out var n) ? n.GetString() ?? "Unknown" : "Unknown",
                    Code = joinData.TryGetProperty("code", out var c) ? c.GetString() ?? "HS" : "HS",
                    Device = joinData.TryGetProperty("device", out var d) ? d.GetString() ?? "Web" : "Web",
                    IPAddress = remoteIP,
                    ConnectedAt = DateTime.Now
                };

                _wsClients[client.Code] = client;

                // Send ACK
                var ack = JsonSerializer.Serialize(new
                {
                    type = "ack",
                    sessionId = Guid.NewGuid().ToString("N"),
                    serverTime = DateTime.Now.ToString("HH:mm:ss"),
                    className = _className,
                    teacherName = _teacherName
                });
                await SendToClient(ws, ack, ct);

                // Sync current Web block/whitelist policy to the newly joined student
                try
                {
                    var control = QASmartClass.Services.ClassControlService.Instance;
                    if (control.IsWebBlocked)
                    {
                        await SendToClient(ws, JsonSerializer.Serialize(new { type = "cmd", action = "BLOCK_WEB_ON" }), ct);
                    }
                    else if (control.IsWebWhitelistActive)
                    {
                        await SendToClient(ws, JsonSerializer.Serialize(new { type = "cmd", action = "WEB_WHITELIST_ON" }), ct);
                        if (!string.IsNullOrEmpty(control.WebWhitelistUrls))
                        {
                            await SendToClient(ws, JsonSerializer.Serialize(new { type = "cmd", action = "WHITELIST_ADD", target = control.WebWhitelistUrls }), ct);
                        }
                    }
                }
                catch (Exception exSync)
                {
                    Log.Warning("Failed to sync web policy to WS student: {Err}", exSync.Message);
                }

                Log.Information("WebSocket student joined: {Name} ({Code}) — {Device} @ {IP}",
                    client.Name, client.Code, client.Device, remoteIP);

                // Notify
                WebStudentConnected?.Invoke(this, new StudentConnectedEventArgs
                {
                    StudentName = client.Name,
                    StudentCode = client.Code,
                    PCName = $"Web-{client.Device}",
                    IPAddress = remoteIP
                });

                // ═══ FIX: proactively request a screenshot immediately on connect ═══
                await Task.Delay(1500); // Give student app 1.5s to finish loading
                await SendToClient(ws, "{\"type\":\"cmd\",\"action\":\"REQUEST_SCREENSHOT\"}", ct);

                // ── Message loop (supports multi-frame messages) ──
                var msgAccumulator = new StringBuilder();
                while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        msgAccumulator.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                        // Only process when we have the complete message
                        if (result.EndOfMessage)
                        {
                            var msgText = msgAccumulator.ToString();
                            msgAccumulator.Clear();
                            ProcessWebSocketMessage(client, msgText, ws, ct);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException ex)
            {
                Log.Debug("WebSocket connection ended: {Err}", ex.Message);
            }
            catch (Exception ex)
            {
                Log.Warning("WebSocket handler error: {Err}", ex.Message);
            }
            finally
            {
                if (client != null)
                {
                    _wsClients.TryRemove(client.Code, out _);
                    WebStudentDisconnected?.Invoke(this, client.Code);
                    Log.Information("WebSocket student left: {Name} ({Code})", client.Name, client.Code);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  MESSAGE PROCESSING — WebSocket JSON ↔ TCP Protocol
        // ═══════════════════════════════════════════════════════════

        private void ProcessWebSocketMessage(WebSocketClient client, string json,
            WebSocket ws, CancellationToken ct)
        {
            try
            {
                var msg = JsonSerializer.Deserialize<JsonElement>(json);
                var type = msg.GetProperty("type").GetString() ?? "";

                client.LastSeen = DateTime.Now;

                switch (type)
                {
                    case "heartbeat":
                        // Send ACK back
                        _ = SendToClient(ws, "{\"type\":\"hb_ack\"}", ct);

                        // Relay as TCP-compatible HB
                        var code = msg.TryGetProperty("code", out var hbCode) ? hbCode.GetString() : client.Code;
                        var activeApp = msg.TryGetProperty("activeApp", out var app) ? app.GetString() : "Web";
                        // LOI_VID_30 FIX: Phát qua WebHeartbeatReceived thay vì WebMessageReceived
                        // để ngăn chặn rò rỉ vào Chat UI (NET-001)
                        WebHeartbeatReceived?.Invoke(this, new StudentMessageEventArgs
                        {
                            StudentCode = client.Code,
                            Message = $"HB_UPDATE|{activeApp}|0"
                        });
                        break;

                    case "hand_raise":
                        var raised = msg.TryGetProperty("raised", out var r) && r.GetBoolean();
                        var reason = msg.TryGetProperty("reason", out var re) ? re.GetString() : "Phát biểu";
                        WebMessageReceived?.Invoke(this, new StudentMessageEventArgs
                        {
                            StudentCode = client.Code,
                            Message = $"HAND_RAISE|raised={raised}|reason={reason}"
                        });
                        break;

                    case "chat":
                        var text = msg.TryGetProperty("text", out var t) ? t.GetString() : "";
                        var channel = msg.TryGetProperty("channel", out var ch) ? ch.GetString() : "ALL";
                        WebMessageReceived?.Invoke(this, new StudentMessageEventArgs
                        {
                            StudentCode = client.Code,
                            Message = $"CHAT|{client.Code}|{channel}|{text}"
                        });
                        break;

                    case "quiz_answer":
                        var quizId = msg.TryGetProperty("quizId", out var qi) ? qi.GetInt32() : 0;
                        var answers = msg.TryGetProperty("answers", out var ans) ? ans.GetRawText() : "{}";
                        WebMessageReceived?.Invoke(this, new StudentMessageEventArgs
                        {
                            StudentCode = client.Code,
                            Message = $"QUIZ_ANSWER|{quizId}|{answers}"
                        });
                        break;

                    case "student_question":
                        var qText = msg.TryGetProperty("text", out var qt) ? qt.GetString() : "";
                        WebMessageReceived?.Invoke(this, new StudentMessageEventArgs
                        {
                            StudentCode = client.Code,
                            Message = $"STUDENT_QUESTION|text={qText}"
                        });
                        break;

                    case "screenshot":
                        var ssCode = msg.TryGetProperty("code", out var sc) ? sc.GetString() : client.Code;
                        var ssData = msg.TryGetProperty("data", out var sd) ? sd.GetString() : "";
                        if (!string.IsNullOrEmpty(ssData))
                        {
                            // Relay as TCP-compatible: SCREENSHOT|code|base64
                            WebMessageReceived?.Invoke(this, new StudentMessageEventArgs
                            {
                                StudentCode = client.Code,
                                Message = $"SCREENSHOT|{ssCode}|{ssData}"
                            });
                            Log.Debug("Screenshot relayed from web student {Code}: {Len} chars",
                                client.Code, ssData.Length);
                        }
                        break;

                    case "homework_submit":
                        var hwId = msg.TryGetProperty("homeworkId", out var hi) ? hi.GetString() : "";
                        var hwText = msg.TryGetProperty("text", out var ht) ? ht.GetString() : "";
                        var hwFiles = msg.TryGetProperty("files", out var hf) ? hf.GetRawText() : "[]";
                        WebMessageReceived?.Invoke(this, new StudentMessageEventArgs
                        {
                            StudentCode = client.Code,
                            Message = $"HOMEWORK_SUBMIT|{hwId}|{hwText}|{hwFiles}"
                        });
                        Log.Information("Homework submitted from web student {Code}: {HwId}", client.Code, hwId);
                        break;

                    case "survey_vote":
                        {
                            if (msg.TryGetProperty("Payload", out var payload))
                            {
                                var studentName = payload.TryGetProperty("StudentName", out var sn) ? sn.GetString() ?? "Học sinh" : "Học sinh";
                                var selectedIdx = payload.TryGetProperty("SelectedOptionIndex", out var si) ? si.GetInt32() : 0;
                                
                                var voteRelay = JsonSerializer.Serialize(new
                                {
                                    voteKey = $"opt{selectedIdx}",
                                    studentName = studentName
                                });
                                
                                WebMessageReceived?.Invoke(this, new StudentMessageEventArgs
                                {
                                    StudentCode = client.Code,
                                    Message = voteRelay
                                });
                                Log.Information("Survey vote relayed from web student {Code}: opt{Index}", client.Code, selectedIdx);
                            }
                        }
                        break;

                    case "survey_answer":
                        {
                            var answerIndex = msg.TryGetProperty("answerIndex", out var ai) ? ai.GetInt32() : 0;
                            var answerText = msg.TryGetProperty("answerText", out var at) ? at.GetString() ?? "" : "";
                            
                            var voteRelay = JsonSerializer.Serialize(new
                            {
                                voteKey = $"opt{answerIndex}",
                                studentName = client.Name
                            });
                            
                            WebMessageReceived?.Invoke(this, new StudentMessageEventArgs
                            {
                                StudentCode = client.Code,
                                Message = voteRelay
                            });
                            Log.Information("Survey answer relayed from web student {Code}: opt{Index} ({Text})", client.Code, answerIndex, answerText);
                        }
                        break;

                    case "lesson_rating":
                        {
                            var stars = msg.TryGetProperty("stars", out var st) ? st.GetInt32() : 0;
                            WebMessageReceived?.Invoke(this, new StudentMessageEventArgs
                            {
                                StudentCode = client.Code,
                                Message = $"SURVEY_STAR|{stars}"
                            });
                            Log.Information("Lesson rating relayed from web student {Code}: {Stars} stars", client.Code, stars);
                        }
                        break;

                    case "tool_submit":
                        {
                            var targetTool = msg.TryGetProperty("toolId", out var ti) ? ti.GetString() ?? "" : "";
                            var resData = msg.TryGetProperty("resultData", out var rd) ? rd.GetString() ?? "" : "";

                            WebMessageReceived?.Invoke(this, new StudentMessageEventArgs
                            {
                                StudentCode = client.Code,
                                Message = $"STUDENT_SUBMISSION|{targetTool}|{client.Code}|{client.Name}|{resData}"
                            });

                            Log.Information("Tool submission relayed from web student {Code} to tool {Tool}: {Data}",
                                client.Code, targetTool, resData);
                        }
                        break;

                    case "cmd_ack":
                        {
                            var ackCmdId = msg.TryGetProperty("commandId", out var ciVal) ? ciVal.GetString() ?? "" : "";
                            var ackStudentCode = msg.TryGetProperty("studentCode", out var scVal) ? scVal.GetString() ?? client.Code : client.Code;
                            var ackStatus = msg.TryGetProperty("status", out var stVal) ? stVal.GetString() ?? "SUCCESS" : "SUCCESS";

                            WebMessageReceived?.Invoke(this, new StudentMessageEventArgs
                            {
                                StudentCode = ackStudentCode,
                                Message = $"ACK|{ackCmdId}|{ackStudentCode}|{ackStatus}"
                            });
                            Log.Information("WS ACK relayed: CmdId={CmdId}, Student={Student}, Status={Status}", ackCmdId, ackStudentCode, ackStatus);
                        }
                        break;

                    // ═══════════════════════════════════════════════════════
                    // BOARD-05: Reverse Command Handler (Board → Laptop)
                    // Handles interactive commands sent from Board SMART TOUCH
                    // Reference: eraser_tool_specification.md v2.1 Mục V.5
                    // ═══════════════════════════════════════════════════════
                    case "board_command":
                        {
                            var boardAction = msg.TryGetProperty("action", out var bAction) ? bAction.GetString() ?? "" : "";
                            var boardParts = boardAction.Split('|');
                            if (boardParts.Length >= 2 && boardParts[0] == "BOARD")
                            {
                                var cmdAction = boardParts[1];
                                var cmdData = boardParts.Length > 2 ? string.Join("|", boardParts.Skip(2)) : "";
                                
                                BoardCommandReceived?.Invoke(this, new BoardCommandEventArgs
                                {
                                    Action = cmdAction,
                                    Data = cmdData,
                                    ClientId = client.Code
                                });
                                
                                Log.Debug("📥 Board reverse command: {Action} from {ClientId}", cmdAction, client.Code);
                            }
                        }
                        break;

                    default:
                        Log.Debug("Unknown WS message type: {Type}", type);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("WS message parse error: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  BROADCAST TO WEB CLIENTS — Called by NetworkDiscovery
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Chuyển đổi lệnh TCP sang JSON và gửi cho tất cả Web clients.
        /// Được gọi khi GV gửi command qua NetworkDiscoveryService.
        /// </summary>
        public async Task BroadcastToWebClients(string tcpCommand)
        {
            var json = ConvertTcpToJson(tcpCommand);
            if (string.IsNullOrEmpty(json)) return;

            var failedCodes = new System.Collections.Generic.List<string>();

            foreach (var (code, client) in _wsClients)
            {
                try
                {
                    if (client.Socket?.State == WebSocketState.Open)
                    {
                        await SendToClient(client.Socket, json, CancellationToken.None);
                    }
                    else
                    {
                        failedCodes.Add(code);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("WS broadcast to {Code} failed: {Err}", code, ex.Message);
                    failedCodes.Add(code);
                }
            }

            // Cleanup disconnected
            foreach (var code in failedCodes)
                _wsClients.TryRemove(code, out _);
        }

        /// <summary>
        /// Gửi JSON trực tiếp cho tất cả Web clients (không qua TCP→JSON convert).
        /// Dùng cho payload lớn như lesson content data.
        /// </summary>
        public async Task BroadcastJsonToWebClients(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            var failedCodes = new System.Collections.Generic.List<string>();

            foreach (var (code, client) in _wsClients)
            {
                try
                {
                    if (client.Socket?.State == WebSocketState.Open)
                    {
                        await SendToClient(client.Socket, json, CancellationToken.None);
                    }
                    else
                    {
                        failedCodes.Add(code);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("WS JSON broadcast to {Code} failed: {Err}", code, ex.Message);
                    failedCodes.Add(code);
                }
            }

            foreach (var code in failedCodes)
                _wsClients.TryRemove(code, out _);
        }

        /// <summary>Gửi cho 1 Web client cụ thể</summary>
        public async Task SendToWebClient(string studentCode, string tcpCommand)
        {
            var json = ConvertTcpToJson(tcpCommand);
            if (string.IsNullOrEmpty(json)) return;

            if (_wsClients.TryGetValue(studentCode, out var client) &&
                client.Socket?.State == WebSocketState.Open)
            {
                await SendToClient(client.Socket, json, CancellationToken.None);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TCP ↔ JSON CONVERSION
        // ═══════════════════════════════════════════════════════════
        private static string StripSignature(string tcp)
        {
            if (string.IsNullOrEmpty(tcp)) return tcp;
            try
            {
                var parts = tcp.Split('|');
                if (parts.Length >= 3)
                {
                    string timestampStr = parts[parts.Length - 2];
                    string signatureStr = parts[parts.Length - 1];

                    if (long.TryParse(timestampStr, out long ts) && ts > 1700000000 && signatureStr.Length >= 20)
                    {
                        bool isBase64 = true;
                        try { Convert.FromBase64String(signatureStr); } catch { isBase64 = false; }
                        
                        if (isBase64)
                        {
                            int lastPipe = tcp.LastIndexOf('|');
                            if (lastPipe > 0)
                            {
                                string temp = tcp.Substring(0, lastPipe);
                                int secondLastPipe = temp.LastIndexOf('|');
                                if (secondLastPipe > 0)
                                {
                                    return temp.Substring(0, secondLastPipe);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return tcp;
        }

        /// <summary>Chuyển TCP command string → WebSocket JSON</summary>
        private static string ConvertTcpToJson(string tcp)
        {
            if (string.IsNullOrEmpty(tcp)) return "";

            tcp = StripSignature(tcp);
            var parts = tcp.Split('|');

            // Trích xuất id lệnh (QoS v4.2)
            string cmdId = "";
            var cleanedPartsList = new System.Collections.Generic.List<string>();
            foreach (var part in parts)
            {
                if (part.StartsWith("id="))
                {
                    cmdId = part.Substring(3);
                }
                else
                {
                    cleanedPartsList.Add(part);
                }
            }
            parts = cleanedPartsList.ToArray();

            // CMD|ACTION  or  CMD|ACTION|TARGET  or  CMD|ACTION|TARGET|data...
            if (parts[0] == "CMD" && parts.Length >= 2)
            {
                var action = parts[1];
                var target = parts.Length > 2 ? parts[2] : "";
                var data   = parts.Length > 3 ? parts[3] : "";

                if (action == "WHITEBOARD_DRAW" && parts.Length >= 5)
                {
                    return JsonSerializer.Serialize(new
                    {
                        type = "cmd",
                        action = action,
                        color = parts[2],
                        width = parts[3],
                        points = parts[4],
                        teacherWidth = parts.Length > 5 ? parts[5] : "",
                        teacherHeight = parts.Length > 6 ? parts[6] : "",
                        id = cmdId
                    });
                }
                if (action == "WHITEBOARD_CLEAR")
                {
                    return JsonSerializer.Serialize(new
                    {
                        type = "cmd",
                        action = action,
                        id = cmdId
                    });
                }
                if (action == "WHITEBOARD_SHAPE" && parts.Length >= 7)
                {
                    return JsonSerializer.Serialize(new
                    {
                        type = "cmd",
                        action = action,
                        shapeType = parts[2],
                        coords = parts[3],
                        color = parts[4],
                        strokeWidth = parts[5],
                        teacherWidth = parts.Length > 6 ? parts[6] : "",
                        teacherHeight = parts.Length > 7 ? parts[7] : "",
                        id = cmdId
                    });
                }
                if (action == "WHITEBOARD_TEXT" && parts.Length >= 6)
                {
                    return JsonSerializer.Serialize(new
                    {
                        type = "cmd",
                        action = action,
                        text = parts[2],
                        position = parts[3],
                        color = parts[4],
                        fontSize = parts[5],
                        teacherWidth = parts.Length > 6 ? parts[6] : "",
                        teacherHeight = parts.Length > 7 ? parts[7] : "",
                        id = cmdId
                    });
                }

                // Intercept and rewrite local file paths to relative HTTP URLs for web clients
                if (action == "SCREEN_BROADCAST" || action == "SCREEN_BROADCAST_START" || action == "SCREEN_BROADCAST_UPDATE")
                {
                    var serverToken = QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken;
                    var tokenParam = !string.IsNullOrEmpty(serverToken) ? $"&token={serverToken}" : "";
                    var imgUrl = $"/api/broadcast_image?t={DateTime.Now.Ticks}" + tokenParam;
                    return JsonSerializer.Serialize(new
                    {
                        type   = "cmd",
                        action = action,
                        target = imgUrl,
                        data   = imgUrl,
                        imageUrl = imgUrl,
                        id = cmdId
                    });
                }
                else if (action == "FILE_BROADCAST" || action == "FILE_BROADCAST_START")
                {
                    var originalPath = target;
                    var fileName = Path.GetFileName(originalPath);
                    var fUrl = $"/api/file_broadcast?name={Uri.EscapeDataString(fileName)}";
                    var ext = Path.GetExtension(fileName).ToLower();
                    var fType = ext switch
                    {
                        ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" => "IMAGE",
                        ".mp4" => "VIDEO",
                        ".pdf" => "PDF",
                        _ => "FILE"
                    };
                    return JsonSerializer.Serialize(new
                    {
                        type   = "cmd",
                        action = action,
                        target = fUrl,
                        data   = fUrl,
                        fileUrl = fUrl,
                        fileName = fileName,
                        fileType = fType,
                        id = cmdId
                    });
                }

                return JsonSerializer.Serialize(new
                {
                    type   = "cmd",
                    action = action,
                    target = target,
                    data   = data,
                    id = cmdId
                });
            }            // MSG|text
            if (parts[0] == "MSG" && parts.Length >= 2)
            {
                return JsonSerializer.Serialize(new
                {
                    type = "message",
                    text = string.Join("|", parts, 1, parts.Length - 1),
                    id = cmdId
                });
            }

            // CHAT relay from another student
            if (parts[0] == "CHAT" && parts.Length >= 4)
            {
                return JsonSerializer.Serialize(new
                {
                    type = "chat",
                    sender = parts[1],
                    channel = parts[2],
                    text = string.Join("|", parts, 3, parts.Length - 3),
                    id = cmdId
                });
            }

            // Generic fallback
            return JsonSerializer.Serialize(new
            {
                type = "data",
                raw = tcp,
                id = cmdId
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════

        private static async Task SendToClient(WebSocket ws, string json, CancellationToken ct)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true, ct);
        }

        private static void EnsureFirewallRulesAndUrlAcl(int port)
        {
            try
            {
                bool isAdmin;
                using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
                {
                    var principal = new System.Security.Principal.WindowsPrincipal(identity);
                    isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                }

                if (!isAdmin)
                {
                    Log.Debug("WebSocketBridge: Not running as administrator; skipping automatic firewall/URL ACL registration.");
                    return;
                }

                string exePath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(exePath)) return;

                Log.Information("WebSocketBridge: Running as administrator. Ensuring Windows Firewall rules and URL ACL for port {Port}...", port);

                RunCommand("netsh", $"advfirewall firewall delete rule name=\"QA SmartClass Server\"");
                RunCommand("netsh", $"advfirewall firewall add rule name=\"QA SmartClass Server\" dir=in action=allow program=\"{exePath}\" enable=yes profile=any");
                RunCommand("netsh", $"advfirewall firewall delete rule name=\"QA SmartClass Broadcast TCP\"");
                RunCommand("netsh", $"advfirewall firewall add rule name=\"QA SmartClass Broadcast TCP\" dir=in action=allow protocol=TCP localport={port},29877,29879 enable=yes profile=any");

                RunCommand("netsh", $"http delete urlacl url=http://+:{port}/");
                RunCommand("netsh", $"http add urlacl url=http://+:{port}/ user=Everyone");
            }
            catch (Exception ex)
            {
                Log.Warning("WebSocketBridge: Failed to configure firewall or URL ACL: {Err}", ex.Message);
            }
        }

        private static void RunCommand(string filename, string arguments)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filename,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var proc = System.Diagnostics.Process.Start(psi);
                proc?.WaitForExit(3000);
            }
            catch { }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    // ─── WebSocket Client Info ───
    public class WebSocketClient
    {
        public WebSocket? Socket { get; set; }
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public string Device { get; set; } = "";
        public string IPAddress { get; set; } = "";
        public DateTime ConnectedAt { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
