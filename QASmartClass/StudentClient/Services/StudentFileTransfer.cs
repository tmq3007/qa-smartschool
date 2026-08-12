using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace QASmartClass.StudentClient.Services
{
    /// <summary>
    /// Student-side file transfer (TCP 29879):
    ///   - Nhận file từ GV (FILE_SEND)
    ///   - Gửi file nộp bài cho GV (FILE_SUBMIT)
    /// </summary>
    public class StudentFileTransfer : IDisposable
    {
        public const int FILE_PORT = 29879;
        private const int BUFFER_SIZE = 65536;

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
        //  SUBMIT — Nộp file cho GV
        // ═══════════════════════════════════════════════════════

        /// <summary>Nộp file bài làm lên GV qua TCP 29879 sử dụng giao thức bắt tay</summary>
        public async Task<(bool Success, string Message)> SubmitFileAsync(string filePath, string teacherIP,
            string studentCode, string studentName)
        {
            if (!File.Exists(filePath))
            {
                Log.Warning("Submit file not found: {Path}", filePath);
                return (false, "Không tìm thấy file cục bộ.");
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
