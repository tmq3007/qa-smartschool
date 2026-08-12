using System;
using System.Linq;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.WebSockets;
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
        private CancellationTokenSource? _cts;
        private readonly ConcurrentDictionary<string, WebSocketClient> _wsClients = new();
        private readonly ConcurrentDictionary<string, System.Collections.Generic.List<DateTime>> _clientRequestTimes = new();

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

        /// <summary>Bắt đầu HTTP + WebSocket server</summary>
        public async Task StartAsync(string className, string teacherName)
        {
            if (IsRunning) return;

            _className = className;
            _teacherName = teacherName;
            _cts = new CancellationTokenSource();

            // Detect LAN IPs for fallback binding
            var localIPs = NetworkDiscoveryService.GetActiveLocalIPv4Addresses();
            var lanIP = localIPs.Count > 0 ? localIPs[0] : "";

            int port = 8080;
            bool success = false;
            while (port <= 9000 && !success)
            {
                try
                {
                    // Ensure firewall and urlacl if running as administrator
                    EnsureFirewallRulesAndUrlAcl(port);

                    _httpListener = new HttpListener();
                    _httpListener.Prefixes.Add($"http://+:{port}/");
                    _httpListener.Start();
                    WebPort = port;
                    success = true;
                    IsRunning = true;

                    Log.Information("WebSocketBridge: Started on ALL interfaces :{Port} — WebRoot: {Path}",
                        port, _webRootPath);

                    _ = AcceptLoopAsync(_cts.Token);
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 5)
                {
                    // Access denied on + (wildcard) — requires admin or netsh urlacl
                    Log.Warning("WebSocketBridge: Access denied on +:{Port}. Trying specific IPs...", port);

                    try
                    {
                        _httpListener?.Close();
                        _httpListener = new HttpListener();

                        bool addedAnyPhysical = false;
                        foreach (var ip in localIPs)
                        {
                            if (!string.IsNullOrEmpty(ip) && ip != "127.0.0.1")
                            {
                                try
                                {
                                    _httpListener.Prefixes.Add($"http://{ip}:{port}/");
                                    Log.Information("WebSocketBridge: Adding prefix http://{IP}:{Port}/", ip, port);
                                    addedAnyPhysical = true;
                                }
                                catch (Exception exPrefix)
                                {
                                    Log.Warning("WebSocketBridge: Failed to add prefix for {IP}:{Port}: {Err}", ip, port, exPrefix.Message);
                                }
                            }
                        }

                        _httpListener.Prefixes.Add($"http://localhost:{port}/");
                        _httpListener.Prefixes.Add($"http://127.0.0.1:{port}/");

                        try
                        {
                            _httpListener.Start();
                            WebPort = port;
                            success = true;
                            IsRunning = true;

                            var boundTo = addedAnyPhysical ? (string.Join(", ", localIPs) + " + localhost") : "localhost";
                            Log.Information("WebSocketBridge: Started on {Bound}:{Port}", boundTo, port);
                            _ = AcceptLoopAsync(_cts.Token);
                        }
                        catch (HttpListenerException exStart) when (exStart.ErrorCode == 5)
                        {
                            Log.Warning("WebSocketBridge: Access denied starting with specific IPs on port {Port}. Retrying with loopback only...", port);
                            _httpListener?.Close();
                            _httpListener = new HttpListener();
                            _httpListener.Prefixes.Add($"http://localhost:{port}/");
                            _httpListener.Prefixes.Add($"http://127.0.0.1:{port}/");
                            _httpListener.Start();
                            WebPort = port;
                            success = true;
                            IsRunning = true;
                            Log.Information("WebSocketBridge: Started on loopback only :{Port}", port);
                            _ = AcceptLoopAsync(_cts.Token);
                        }
                    }
                    catch (HttpListenerException ex2)
                    {
                        if (ex2.ErrorCode == 32 || ex2.ErrorCode == 183 || ex2.Message.Contains("in use") || ex2.Message.Contains("already"))
                        {
                            Log.Warning("WebSocketBridge: Port {Port} is in use on specific IP, trying next...", port);
                            port++;
                            _httpListener?.Close();
                            _httpListener = null;
                        }
                        else
                        {
                            Log.Error("WebSocketBridge: Cannot bind to {IPs}:{Port} — Error: {Err}",
                                string.Join(", ", localIPs), port, ex2.Message);
                            break;
                        }
                    }
                }
                catch (HttpListenerException ex)
                {
                    if (ex.ErrorCode == 32 || ex.ErrorCode == 183 || ex.Message.Contains("in use") || ex.Message.Contains("already"))
                    {
                        Log.Warning("WebSocketBridge: Port {Port} is in use on wildcard, trying next...", port);
                        port++;
                        _httpListener?.Close();
                        _httpListener = null;
                    }
                    else
                    {
                        Log.Error("WebSocketBridge: Failed to start on wildcard port {Port}: {Err}", port, ex.Message);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("WebSocketBridge: Unexpected error on port {Port}: {Err}", port, ex.Message);
                    break;
                }
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

        private void ServeStaticFile(HttpListenerContext context)
        {
            var urlPath = context.Request.Url?.AbsolutePath ?? "/";
            string clientIP = context.Request.RemoteEndPoint?.Address.ToString() ?? "unknown";

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
            WebSocketContext? wsContext = null;
            WebSocketClient? client = null;

            try
            {
                wsContext = await httpContext.AcceptWebSocketAsync(null);
                var ws = wsContext.WebSocket;
                var remoteIP = httpContext.Request.RemoteEndPoint?.Address.ToString() ?? "?";

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
