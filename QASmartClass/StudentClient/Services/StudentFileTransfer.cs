using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace QASmartClass.StudentClient.Services
{
    /// <summary>
    /// Student-side file transfer:
    ///   - Nhận file từ GV qua HTTP Chunked Pull (/api/files/download) + dự phòng TCP 29879
    ///   - Gửi file nộp bài cho GV qua HTTP Chunked Upload (/api/files/upload) + dự phòng TCP 29879
    /// </summary>
    public class StudentFileTransfer : IDisposable
    {
        public const int FILE_PORT = 29879;
        private const int BUFFER_SIZE = 65536;
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMinutes(10) };

        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private bool _isListening;

        private readonly string _receiveFolder;

        public event EventHandler<StudentFileEventArgs>? FileReceived;
        public event EventHandler<int>? SubmitProgress; // 0-100

        public StudentFileTransfer()
        {
            _receiveFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "QA SmartClass", "ReceivedFiles");
            Directory.CreateDirectory(_receiveFolder);
        }

        // ═══════════════════════════════════════════════════════
        //  LISTEN — Nhận file từ GV
        // ═══════════════════════════════════════════════════════

        public async Task StartListeningAsync()
        {
            if (_isListening) return;
            _cts = new CancellationTokenSource();
            _isListening = true;

            try
            {
                _listener = new TcpListener(IPAddress.Any, FILE_PORT);
                _listener.Start();
                Log.Information("StudentFileTransfer: Listening on :{Port}", FILE_PORT);

                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                        _ = HandleIncomingAsync(client, _cts.Token);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex) when (!_cts.Token.IsCancellationRequested)
                    {
                        Log.Warning("Student file accept error: {Err}", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("StudentFileTransfer listen error: {Err}", ex.Message);
            }
            finally { _isListening = false; }
        }

        private async Task HandleIncomingAsync(TcpClient tcp, CancellationToken ct)
        {
            try
            {
                using var stream = tcp.GetStream();

                // Read header: "FILE_SEND|filename|filesize\n"
                var header = await ReadLineAsync(stream, ct);
                var parts = header.Split('|');

                if (parts.Length < 3 || parts[0] != "FILE_SEND")
                {
                    tcp.Close();
                    return;
                }

                var filename = parts[1];
                var safeFilename = Path.GetFileName(filename);
                var fileSize = long.TryParse(parts[2], out var sz) ? sz : 0;
                var savePath = Path.Combine(_receiveFolder, safeFilename);

                // Avoid overwriting — add counter
                int counter = 1;
                while (File.Exists(savePath))
                {
                    var ext = Path.GetExtension(safeFilename);
                    var name = Path.GetFileNameWithoutExtension(safeFilename);
                    savePath = Path.Combine(_receiveFolder, $"{name}_{counter++}{ext}");
                }

                // Receive data
                long received = 0;
                var buffer = new byte[BUFFER_SIZE];

                using (var fs = new FileStream(savePath, FileMode.Create))
                {
                    while (received < fileSize && !ct.IsCancellationRequested)
                    {
                        var toRead = (int)Math.Min(BUFFER_SIZE, fileSize - received);
                        var read = await stream.ReadAsync(buffer, 0, toRead, ct);
                        if (read == 0) break;
                        await fs.WriteAsync(buffer, 0, read, ct);
                        received += read;
                    }
                }

                Log.Information("Student received file: {File} ({Size:N0} bytes)", safeFilename, received);

                FileReceived?.Invoke(this, new StudentFileEventArgs
                {
                    FileName = safeFilename,
                    SavePath = savePath,
                    FileSize = received,
                    Direction = "Receive"
                });
            }
            catch (Exception ex)
            {
                Log.Warning("Student file receive error: {Err}", ex.Message);
            }
            finally { tcp.Close(); }
        }

        // ═══════════════════════════════════════════════════════
        //  DOWNLOAD — Tải tệp từ GV qua HTTP Chunked Pull (Xuyên 100% Firewall)
        // ═══════════════════════════════════════════════════════

        public async Task<(bool Success, string SavePath, string Message)> DownloadOfferedFileAsync(
            string downloadUrl,
            string suggestedFileName,
            Func<int, Task>? progressCallback = null,
            CancellationToken ct = default)
        {
            string safeFilename = Path.GetFileName(string.IsNullOrWhiteSpace(suggestedFileName) ? "TaiLieu.dat" : suggestedFileName);
            if (string.IsNullOrWhiteSpace(safeFilename)) safeFilename = "TaiLieu.dat";

            Directory.CreateDirectory(_receiveFolder);
            string savePath = Path.Combine(_receiveFolder, safeFilename);
            int counter = 1;
            while (File.Exists(savePath))
            {
                string ext = Path.GetExtension(safeFilename);
                string name = Path.GetFileNameWithoutExtension(safeFilename);
                savePath = Path.Combine(_receiveFolder, $"{name}_{counter++}{ext}");
            }

            string tempPath = savePath + ".part_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    return (false, string.Empty, $"HTTP {(int)resp.StatusCode} ({resp.ReasonPhrase})");
                }

                long totalBytes = resp.Content.Headers.ContentLength ?? 0;
                long received = 0;
                int lastPct = -1;

                using (var netStream = await resp.Content.ReadAsStreamAsync(ct))
                using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, BUFFER_SIZE, useAsync: true))
                {
                    var buffer = new byte[BUFFER_SIZE];
                    int read;
                    while ((read = await netStream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                    {
                        await fs.WriteAsync(buffer.AsMemory(0, read), ct);
                        received += read;

                        if (totalBytes > 0 && progressCallback != null)
                        {
                            int pct = (int)(received * 100 / totalBytes);
                            if (pct >= lastPct + 25 && pct < 100)
                            {
                                lastPct = pct;
                                _ = progressCallback(pct);
                            }
                        }
                    }
                    await fs.FlushAsync(ct);
                }

                File.Move(tempPath, savePath, overwrite: true);
                string finalName = Path.GetFileName(savePath);
                Log.Information("Student downloaded file via HTTP Pull: {File} ({Size:N0} bytes)", finalName, received);

                FileReceived?.Invoke(this, new StudentFileEventArgs
                {
                    FileName = finalName,
                    SavePath = savePath,
                    FileSize = received,
                    Direction = "Receive"
                });

                return (true, savePath, "Tải thành công");
            }
            catch (Exception ex)
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                Log.Warning("DownloadOfferedFileAsync failed ({Url}): {Err}", downloadUrl, ex.Message);
                return (false, string.Empty, ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        //  SUBMIT — Nộp file cho GV (Ưu tiên HTTP Chunked Upload port 8080, dự phòng TCP 29879)
        // ═══════════════════════════════════════════════════════

        private sealed class ProgressStreamContent : HttpContent
        {
            private readonly string _filePath;
            private readonly long _fileLength;
            private readonly Action<int>? _onProgress;

            public ProgressStreamContent(string filePath, long fileLength, Action<int>? onProgress)
            {
                _filePath = filePath;
                _fileLength = fileLength;
                _onProgress = onProgress;
                Headers.ContentLength = fileLength;
                Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            {
                using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BUFFER_SIZE, useAsync: true);
                var buf = new byte[BUFFER_SIZE];
                long sent = 0;
                int lastPct = -1;
                int read;
                while ((read = await fs.ReadAsync(buf.AsMemory(0, buf.Length))) > 0)
                {
                    await stream.WriteAsync(buf.AsMemory(0, read));
                    sent += read;
                    int pct = _fileLength > 0 ? (int)(sent * 100 / _fileLength) : 100;
                    if (pct != lastPct)
                    {
                        lastPct = pct;
                        _onProgress?.Invoke(pct);
                    }
                }
                await stream.FlushAsync();
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _fileLength;
                return true;
            }
        }

        public async Task<(bool Success, string Message, bool RejectedByRule)> SubmitFileViaHttpAsync(
            string filePath,
            string teacherIP,
            int webPort,
            string token,
            string studentCode,
            string studentName)
        {
            var fi = new FileInfo(filePath);
            int port = webPort <= 0 ? 8080 : webPort;
            string uploadUrl =
                $"http://{teacherIP}:{port}/api/files/upload?code={Uri.EscapeDataString(studentCode)}" +
                $"&name={Uri.EscapeDataString(studentName)}" +
                $"&filename={Uri.EscapeDataString(fi.Name)}" +
                $"&token={Uri.EscapeDataString(token ?? string.Empty)}";

            try
            {
                using var content = new ProgressStreamContent(filePath, fi.Length, pct => SubmitProgress?.Invoke(this, pct));
                using var req = new HttpRequestMessage(HttpMethod.Post, uploadUrl) { Content = content };
                using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                string body = await resp.Content.ReadAsStringAsync();

                string msg = resp.IsSuccessStatusCode ? "Nộp bài thành công!" : $"Lỗi HTTP {(int)resp.StatusCode}";
                bool ok = resp.IsSuccessStatusCode;

                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("ok", out var okProp))
                            ok = okProp.GetBoolean();
                        if (doc.RootElement.TryGetProperty("message", out var msgProp) && msgProp.GetString() is string parsedMsg && !string.IsNullOrWhiteSpace(parsedMsg))
                            msg = parsedMsg;
                    }
                    catch { }
                }

                // Nếu bị khóa nộp bài (403) hoặc đầy ổ đĩa (507) / quá dung lượng (413) thì không cần fallback TCP 29879
                bool rejectedByRule = (int)resp.StatusCode is 403 or 413 or 507;
                if (ok)
                {
                    Log.Information("File submitted via HTTP Chunked Upload: {File} ({Size:N0} bytes) → {IP}:{Port}",
                        fi.Name, fi.Length, teacherIP, port);
                }
                return (ok, msg, rejectedByRule);
            }
            catch (Exception ex)
            {
                Log.Debug("SubmitFileViaHttpAsync fallback needed ({Url}): {Err}", uploadUrl, ex.Message);
                return (false, ex.Message, false);
            }
        }

        /// <summary>Nộp file bài làm lên GV: ưu tiên HTTP Chunked Upload (cổng WebBridge 8080), dự phòng TCP 29879</summary>
        public async Task<(bool Success, string Message)> SubmitFileAsync(string filePath, string teacherIP,
            string studentCode, string studentName, int webPort = 8080, string token = "")
        {
            if (!File.Exists(filePath))
            {
                Log.Warning("Submit file not found: {Path}", filePath);
                return (false, "Không tìm thấy file cục bộ.");
            }

            var fiCheck = new FileInfo(filePath);
            if (fiCheck.Length == 0)
            {
                return (false, "File bài làm rỗng (0 byte). Vui lòng chọn file hợp lệ.");
            }

            var httpRes = await SubmitFileViaHttpAsync(filePath, teacherIP, webPort, token, studentCode, studentName);
            if (httpRes.Success || httpRes.RejectedByRule)
            {
                return (httpRes.Success, httpRes.Message);
            }

            try
            {
                var fi = new FileInfo(filePath);
                if (fi.Length == 0)
                {
                    return (false, "File bài làm rỗng (0 byte). Vui lòng chọn file hợp lệ.");
                }

                using var tcp = new TcpClient();
                var connectTask = tcp.ConnectAsync(IPAddress.Parse(teacherIP), FILE_PORT);
                if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                {
                    throw new TimeoutException("Kết nối tới máy Giáo viên quá hạn (5s).");
                }

                using var stream = tcp.GetStream();
                stream.ReadTimeout = 30000;
                stream.WriteTimeout = 30000;

                // 1. Gửi header lên giáo viên
                var header = $"FILE_SUBMIT|{studentCode}|{studentName}|{fi.Name}|{fi.Length}\n";
                await stream.WriteAsync(Encoding.UTF8.GetBytes(header));

                // 2. Chờ nhận phản hồi bắt tay (Handshake Response)
                var handshake = await ReadLineAsync(stream, CancellationToken.None);

                if (handshake.StartsWith("FILE_ERR"))
                {
                    var reason = handshake.Split('|').ElementAtOrDefault(1) ?? "Từ chối không rõ nguyên nhân.";
                    Log.Warning("Nộp bài bị Giáo viên từ chối: {Reason}", reason);
                    return (false, reason);
                }

                if (handshake != "FILE_START")
                {
                    Log.Warning("Phản hồi bắt đầu file không hợp lệ từ GV: {Res}", handshake);
                    return (false, "Phản hồi kết nối không hợp lệ từ Giáo viên.");
                }

                // 3. Tiến hành truyền dữ liệu tệp khi đã được chấp nhận
                long sent = 0;
                var buffer = new byte[BUFFER_SIZE];

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    int read;
                    while ((read = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await stream.WriteAsync(buffer, 0, read);
                        sent += read;

                        int pct = fi.Length > 0 ? (int)(sent * 100 / fi.Length) : 0;
                        SubmitProgress?.Invoke(this, pct);
                    }
                }

                // 4. Chờ nhận gói tin ACK hoàn tất (FILE_OK)
                var ack = await ReadLineAsync(stream, CancellationToken.None);

                if (ack.StartsWith("FILE_OK"))
                {
                    Log.Information("File submitted and confirmed: {File} ({Size:N0} bytes) → {IP}",
                        fi.Name, sent, teacherIP);
                    return (true, "Nộp bài thành công!");
                }
                else
                {
                    var reason = ack.Split('|').ElementAtOrDefault(1) ?? "Lỗi xác nhận hoàn tất.";
                    Log.Warning("Submit ACK error: {Ack}", ack);
                    return (false, reason);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Submit file error: {Err}", ex.Message);
                return (false, $"Lỗi truyền tải mạng: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════

        private static async Task<string> ReadLineAsync(NetworkStream stream, CancellationToken ct)
        {
            var bytes = new System.Collections.Generic.List<byte>();
            var buffer = new byte[1];
            const int MAX_LINE_LENGTH = 4096; // Giới hạn chống tràn bộ nhớ DoS
            
            while (!ct.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer, 0, 1, ct);
                if (read == 0) break;
                if (buffer[0] == '\n') break;
                
                bytes.Add(buffer[0]);
                if (bytes.Count > MAX_LINE_LENGTH)
                {
                    throw new InvalidDataException("Dữ liệu tiêu đề mạng vượt quá giới hạn an toàn cho phép.");
                }
            }
            return Encoding.UTF8.GetString(bytes.ToArray()).TrimEnd('\r');
        }

        public string ReceiveFolder => _receiveFolder;
        public bool IsListening => _isListening;

        public void StopListening()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _isListening = false;
        }

        public void Dispose()
        {
            StopListening();
            _cts?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public class StudentFileEventArgs : EventArgs
    {
        public string FileName { get; set; } = string.Empty;
        public string SavePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Direction { get; set; } = string.Empty;
    }
}
