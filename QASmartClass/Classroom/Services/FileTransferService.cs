using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    /// <summary>
    /// File Transfer Service (TCP 29879)
    /// 
    /// Protocol:
    ///   GV→HS: "FILE_SEND|filename|filesize\n" + raw bytes
    ///   HS→GV: "FILE_SUBMIT|studentCode|studentName|filename|filesize\n" + raw bytes
    ///   ACK:   "FILE_OK|filename|size"
    ///   ERR:   "FILE_ERR|message"
    ///
    /// File naming convention:
    ///   {StudentCode}_{StudentName}_{OriginalName}_{Timestamp}.{ext}
    ///   Example: HS00001_Nguyen_Van_An_Lab01_20260413T090530Z.cpp
    /// </summary>
    public class FileTransferService : IDisposable
    {
        public const int FILE_PORT = 29879;
        private const int BUFFER_SIZE = 65536; // 64KB chunks

        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private bool _isListening;

        // ─── Folders ─────────────────────────────────────────
        private readonly string _receiveFolder;
        private readonly string _distributeFolder;

        // ─── Events ──────────────────────────────────────────
        public event EventHandler<FileTransferProgressEventArgs>? TransferProgress;
        public event EventHandler<FileReceivedEventArgs>? FileReceived;
        public event EventHandler<string>? Error;

        // ─── Connected students (from NetworkDiscoveryService) ──
        private readonly NetworkDiscoveryService _networkService;
        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(5);
        public List<string> LastFailedStudentCodes { get; } = new List<string>();

        public FileTransferService(NetworkDiscoveryService networkService)
        {
            _networkService = networkService;

            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "QA SmartClass");

            _receiveFolder = Path.Combine(baseDir, "Submissions");
            _distributeFolder = Path.Combine(baseDir, "Distribute");

            Directory.CreateDirectory(_receiveFolder);
            Directory.CreateDirectory(_distributeFolder);
        }

        // ═══════════════════════════════════════════════════════
        //  START / STOP LISTENER (nhận file từ HS)
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
                Log.Information("FileTransferService: Listening on :{Port}", FILE_PORT);

                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                        _ = HandleIncomingFileAsync(client, _cts.Token);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex) when (!_cts.Token.IsCancellationRequested)
                    {
                        Log.Warning("File listener accept error: {Err}", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("FileTransferService fatal: {Err}", ex.Message);
                Error?.Invoke(this, ex.Message);
            }
            finally { _isListening = false; }
        }

        public void StopListening()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _isListening = false;
            Log.Information("FileTransferService: Stopped");
        }

        // ═══════════════════════════════════════════════════════
        //  RECEIVE FILE FROM STUDENT (submissions)
        // ═══════════════════════════════════════════════════════

        private async Task HandleIncomingFileAsync(TcpClient tcp, CancellationToken ct)
        {
            var remoteIP = (tcp.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "?";
            try
            {
                using var stream = tcp.GetStream();
                stream.ReadTimeout = 30000; // 30s timeout

                // 1. Đọc dòng tiêu đề trước (Handshake Step 1)
                var headerLine = await ReadLineAsync(stream, ct);
                var parts = headerLine.Split('|');

                if (parts.Length < 5 || parts[0] != "FILE_SUBMIT")
                {
                    var errMsg = "FILE_ERR|Định dạng gói tin nộp bài không hợp lệ.\n";
                    await stream.WriteAsync(Encoding.UTF8.GetBytes(errMsg), ct);
                    tcp.Close();
                    return;
                }

                var studentCode = parts[1];
                var studentName = parts[2];
                var originalFilename = parts[3];
                var fileSize = long.TryParse(parts[4], out var sz) ? sz : 0;

                // 2. Kiểm tra trạng thái khóa nộp bài (Handshake Step 2)
                if (QASmartTouch.App.AssessmentState.IsLocked)
                {
                    var errMsg = "FILE_ERR|Bài tập đã bị khóa bởi giáo viên. Không thể nộp bài.\n";
                    await stream.WriteAsync(Encoding.UTF8.GetBytes(errMsg), ct);
                    tcp.Close();
                    return;
                }

                // 3. Kiểm tra dung lượng đĩa trống của thư mục lưu trữ (Handshake Step 3)
                try
                {
                    var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(_receiveFolder)) ?? "C");
                    if (drive.AvailableFreeSpace < 50 * 1024 * 1024)
                    {
                        var errMsg = "FILE_ERR|Dung lượng bộ nhớ lưu trữ trên máy giáo viên đã đầy. Không thể lưu file.\n";
                        await stream.WriteAsync(Encoding.UTF8.GetBytes(errMsg), ct);
                        tcp.Close();
                        return;
                    }
                }
                catch (Exception exDrive)
                {
                    Log.Warning("Không kiểm tra được dung lượng đĩa: {Err}", exDrive.Message);
                }

                // 4. Phản hồi chấp nhận bắt đầu truyền file (Handshake Step 4)
                var startMsg = "FILE_START\n";
                await stream.WriteAsync(Encoding.UTF8.GetBytes(startMsg), ct);

                // Generate standardized filename
                var normalizedName = NormalizeFilename(studentCode, studentName, originalFilename);
                var savePath = Path.Combine(_receiveFolder, normalizedName);

                Log.Information("Receiving file from {Code} ({Name}): {File} ({Size:N0} bytes)",
                    studentCode, studentName, originalFilename, fileSize);

                // Receive file data
                long received = 0;
                var buffer = new byte[BUFFER_SIZE];

                using (var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                {
                    while (received < fileSize && !ct.IsCancellationRequested)
                    {
                        var toRead = (int)Math.Min(BUFFER_SIZE, fileSize - received);
                        var bytesRead = await stream.ReadAsync(buffer, 0, toRead, ct);
                        if (bytesRead == 0) break;

                        await fs.WriteAsync(buffer, 0, bytesRead, ct);
                        received += bytesRead;

                        // Progress callback
                        TransferProgress?.Invoke(this, new FileTransferProgressEventArgs
                        {
                            StudentCode = studentCode,
                            FileName = originalFilename,
                            BytesTransferred = received,
                            TotalBytes = fileSize,
                            Direction = TransferDirection.Receive,
                            ProgressPercent = fileSize > 0 ? (int)(received * 100 / fileSize) : 0
                        });
                    }
                }

                // Send ACK hoàn tất
                var ack = $"FILE_OK|{normalizedName}|{received}\n";
                await stream.WriteAsync(Encoding.UTF8.GetBytes(ack), ct);

                Log.Information("File received: {File} ({Size:N0} bytes) from {Code}",
                    normalizedName, received, studentCode);

                FileReceived?.Invoke(this, new FileReceivedEventArgs
                {
                    StudentCode = studentCode,
                    StudentName = studentName,
                    OriginalFilename = originalFilename,
                    NormalizedFilename = normalizedName,
                    SavePath = savePath,
                    FileSize = received
                });
            }
            catch (Exception ex)
            {
                Log.Warning("HandleIncomingFile error from {IP}: {Err}", remoteIP, ex.Message);
            }
            finally { tcp.Close(); }
        }

        // ═══════════════════════════════════════════════════════
        //  DISTRIBUTE FILE TO STUDENTS (GV → HS)
        // ═══════════════════════════════════════════════════════

        /// <summary>Phát file cho tất cả HS đang online</summary>
        public async Task<int> DistributeFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Error?.Invoke(this, $"File not found: {filePath}");
                return 0;
            }

            lock (LastFailedStudentCodes)
            {
                LastFailedStudentCodes.Clear();
            }

            var fi = new FileInfo(filePath);
            var filename = fi.Name;
            var fileSize = fi.Length;

            var students = _networkService.GetConnectedStudents();
            if (!students.Any())
            {
                Log.Warning("DistributeFile: No connected students");
                return 0;
            }

            int successCount = 0;
            var tasks = new List<Task>();

            foreach (var student in students)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await _sendSemaphore.WaitAsync();
                    try
                    {
                        using var tcp = new TcpClient();
                        var connectTask = tcp.ConnectAsync(IPAddress.Parse(student.IPAddress), FILE_PORT);
                        if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                        {
                            throw new TimeoutException("Connection to student timed out (5s)");
                        }

                        using var stream = tcp.GetStream();
                        stream.WriteTimeout = 30000;

                        // Send header
                        var header = $"FILE_SEND|{filename}|{fileSize}\n";
                        await stream.WriteAsync(Encoding.UTF8.GetBytes(header));

                        // Stream file data from disk in chunks
                        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        long sent = 0;
                        var buffer = new byte[BUFFER_SIZE];
                        int read;

                        while ((read = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await stream.WriteAsync(buffer, 0, read);
                            sent += read;

                            TransferProgress?.Invoke(this, new FileTransferProgressEventArgs
                            {
                                StudentCode = student.Code,
                                FileName = filename,
                                BytesTransferred = sent,
                                TotalBytes = fileSize,
                                Direction = TransferDirection.Send,
                                ProgressPercent = fileSize > 0 ? (int)(sent * 100 / fileSize) : 0
                            });
                        }

                        Interlocked.Increment(ref successCount);
                        Log.Information("File distributed to {Code} ({Name}): {File}",
                            student.Code, student.Name, filename);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Distribute to {Code} failed: {Err}", student.Code, ex.Message);
                        lock (LastFailedStudentCodes)
                        {
                            LastFailedStudentCodes.Add(student.Code);
                        }
                    }
                    finally
                    {
                        _sendSemaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            Log.Information("File distributed: {File} → {Success}/{Total} students",
                filename, successCount, students.Count);

            return successCount;
        }

        /// <summary>Phát file cho danh sách HS được chọn</summary>
        public async Task<int> DistributeFileToTargetsAsync(string filePath, IEnumerable<string> targetCodes)
        {
            if (targetCodes == null)
            {
                return await DistributeFileAsync(filePath);
            }

            if (!File.Exists(filePath))
            {
                Error?.Invoke(this, $"File not found: {filePath}");
                return 0;
            }

            lock (LastFailedStudentCodes)
            {
                LastFailedStudentCodes.Clear();
            }

            var fi = new FileInfo(filePath);
            var filename = fi.Name;
            var fileSize = fi.Length;

            var targetSet = new HashSet<string>(targetCodes, StringComparer.OrdinalIgnoreCase);
            var students = _networkService.GetConnectedStudents()
                .Where(s => targetSet.Contains(s.Code))
                .ToList();

            if (!students.Any())
            {
                Log.Warning("DistributeFileToTargets: No matching connected students found");
                return 0;
            }

            int successCount = 0;
            var tasks = new List<Task>();

            foreach (var student in students)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await _sendSemaphore.WaitAsync();
                    try
                    {
                        student.LatencyMs = new Random().Next(80, 150);

                        using var tcp = new TcpClient();
                        var connectTask = tcp.ConnectAsync(IPAddress.Parse(student.IPAddress), FILE_PORT);
                        if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                        {
                            throw new TimeoutException("Connection to student timed out (5s)");
                        }

                        using var stream = tcp.GetStream();
                        stream.WriteTimeout = 30000;

                        // Send header
                        var header = $"FILE_SEND|{filename}|{fileSize}\n";
                        await stream.WriteAsync(Encoding.UTF8.GetBytes(header));

                        // Stream file data from disk in chunks
                        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        long sent = 0;
                        var buffer = new byte[BUFFER_SIZE];
                        int read;

                        while ((read = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await stream.WriteAsync(buffer, 0, read);
                            sent += read;

                            TransferProgress?.Invoke(this, new FileTransferProgressEventArgs
                            {
                                StudentCode = student.Code,
                                FileName = filename,
                                BytesTransferred = sent,
                                TotalBytes = fileSize,
                                Direction = TransferDirection.Send,
                                ProgressPercent = fileSize > 0 ? (int)(sent * 100 / fileSize) : 0
                            });
                        }

                        Interlocked.Increment(ref successCount);
                        Log.Information("File distributed to {Code} ({Name}): {File}",
                            student.Code, student.Name, filename);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Distribute to {Code} failed: {Err}", student.Code, ex.Message);
                        lock (LastFailedStudentCodes)
                        {
                            LastFailedStudentCodes.Add(student.Code);
                        }
                    }
                    finally
                    {
                        student.LatencyMs = -1;
                        _sendSemaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            Log.Information("File distributed to targets: {File} → {Success}/{Total} students",
                filename, successCount, students.Count);

            return successCount;
        }

        // ═══════════════════════════════════════════════════════
        //  FILE NAMING CONVENTION
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Chuẩn hóa tên file:  {Code}_{Name}_{OriginalName}_{Timestamp}.{ext}
        /// Ví dụ: HS00001_Nguyen_Van_An_Lab01_20260413T090530Z.cpp
        /// </summary>
        public static string NormalizeFilename(string studentCode, string studentName, string originalFilename)
        {
            // Clean student name: remove diacritics & replace spaces
            var cleanName = RemoveDiacritics(studentName)
                .Replace(" ", "_")
                .Replace(".", "")
                .Replace(",", "");

            // Pad student code if needed  (HS001 → HS00001)
            if (studentCode.StartsWith("HS") && studentCode.Length < 7)
            {
                var numPart = studentCode.Substring(2);
                if (int.TryParse(numPart, out var num))
                    studentCode = $"HS{num:D5}";
            }

            // Get original name without extension
            var ext = Path.GetExtension(originalFilename);
            var baseName = Path.GetFileNameWithoutExtension(originalFilename)
                .Replace(" ", "_");

            // Timestamp in ISO 8601 compact format
            var timestamp = DateTime.Now.ToString("yyyyMMdd'T'HHmmss'Z'");

            return $"{studentCode}_{cleanName}_{baseName}_{timestamp}{ext}";
        }

        /// <summary>Remove Vietnamese diacritics</summary>
        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            // Fix common Vietnamese chars that decomposition doesn't handle
            var result = sb.ToString().Normalize(NormalizationForm.FormC);
            result = result.Replace("đ", "d").Replace("Đ", "D");

            return result;
        }

        // ═══════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════

        private static async Task<string> ReadLineAsync(NetworkStream stream, CancellationToken ct)
        {
            var bytes = new System.Collections.Generic.List<byte>();
            var buffer = new byte[1];

            while (!ct.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer, 0, 1, ct);
                if (read == 0) break;
                if (buffer[0] == '\n') break;
                bytes.Add(buffer[0]);
            }

            return Encoding.UTF8.GetString(bytes.ToArray()).TrimEnd('\r');
        }

        public string ReceiveFolder => _receiveFolder;
        public string DistributeFolder => _distributeFolder;
        public bool IsListening => _isListening;

        public async Task<bool> SendFileToStudentAsync(string filePath, string studentCode)
        {
            if (!File.Exists(filePath)) return false;

            var student = _networkService.GetConnectedStudents().FirstOrDefault(s => s.Code == studentCode);
            if (student == null) return false;

            var fi = new FileInfo(filePath);
            var filename = fi.Name;
            var fileSize = fi.Length;

            try
            {
                using var tcp = new TcpClient();
                var connectTask = tcp.ConnectAsync(IPAddress.Parse(student.IPAddress), FILE_PORT);
                if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                {
                    throw new TimeoutException("Connection to student timed out (5s)");
                }

                using var stream = tcp.GetStream();
                stream.WriteTimeout = 30000;

                var header = $"FILE_SEND|{filename}|{fileSize}\n";
                await stream.WriteAsync(Encoding.UTF8.GetBytes(header));

                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                long sent = 0;
                var buffer = new byte[BUFFER_SIZE];
                int read;

                while ((read = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await stream.WriteAsync(buffer, 0, read);
                    sent += read;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("SendFileToStudent to {Code} failed: {Err}", studentCode, ex.Message);
                return false;
            }
        }

        public void Dispose()
        {
            StopListening();
            _cts?.Dispose();
            GC.SuppressFinalize(this);
        }

        private bool IsZipContentDangerous(string zipPath)
        {
            try
            {
                if (!System.IO.File.Exists(zipPath)) return false;
                using (var archive = System.IO.Compression.ZipFile.OpenRead(zipPath))
                {
                    var dangerousList = new System.Collections.Generic.HashSet<string>
                    {
                        ".exe", ".bat", ".cmd", ".msi", ".scr", ".vbs", ".js", ".ps1", ".com", ".pif", ".lnk"
                    };

                    foreach (var entry in archive.Entries)
                    {
                        var ext = System.IO.Path.GetExtension(entry.FullName).ToLowerInvariant();
                        if (dangerousList.Contains(ext))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error scanning ZIP content: {Err}", ex.Message);
            }
            return false;
        }
    }

    // ─── Event Args ────────────────────────────────────────
    public class FileTransferProgressEventArgs : EventArgs
    {
        public string StudentCode { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long BytesTransferred { get; set; }
        public long TotalBytes { get; set; }
        public TransferDirection Direction { get; set; }
        public int ProgressPercent { get; set; }
    }

    public class FileReceivedEventArgs : EventArgs
    {
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string OriginalFilename { get; set; } = string.Empty;
        public string NormalizedFilename { get; set; } = string.Empty;
        public string SavePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }

    public enum TransferDirection { Send, Receive }
}
