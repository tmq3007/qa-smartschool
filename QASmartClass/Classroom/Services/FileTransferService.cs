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
        public event EventHandler<StudentDeliveryRecord>? DeliveryStatusChanged;
        public event EventHandler<string>? Error;

        // ─── Connected students (from NetworkDiscoveryService) ──
        private readonly NetworkDiscoveryService _networkService;
        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(5);
        public List<string> LastFailedStudentCodes { get; } = new List<string>();
        public ConcurrentDictionary<string, StudentDeliveryRecord> ActiveDeliveries { get; } = new(StringComparer.OrdinalIgnoreCase);

        public FileTransferService(NetworkDiscoveryService networkService)
        {
            _networkService = networkService;
            _networkService.MessageReceived += OnStudentControlMessage;

            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "QA SmartClass");

            _receiveFolder = Path.Combine(baseDir, "Submissions");
            _distributeFolder = Path.Combine(baseDir, "Distribute");

            Directory.CreateDirectory(_receiveFolder);
            Directory.CreateDirectory(_distributeFolder);
        }

        private void OnStudentControlMessage(object? sender, StudentMessageEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.Message)) return;
            // Format: FILE_PROGRESS|<fileId>|<pct>|<status>|<detail>
            // hoặc:   CMD|FILE_PROGRESS|<fileId>|<pct>|<status>|<detail>
            string raw = e.Message.StartsWith("CMD|", StringComparison.OrdinalIgnoreCase)
                ? e.Message.Substring(4)
                : e.Message;

            if (!raw.StartsWith("FILE_PROGRESS|", StringComparison.OrdinalIgnoreCase)) return;

            var parts = raw.Split('|');
            if (parts.Length < 4) return;

            string fileId = parts[1];
            int pct = int.TryParse(parts[2], out var p) ? Math.Clamp(p, 0, 100) : 0;
            string statusToken = parts[3].Trim().ToUpperInvariant();
            string detail = parts.Length >= 5 ? parts[4] : string.Empty;
            string code = e.StudentCode ?? "UNKNOWN";

            string key = $"{fileId}_{code}";
            var studentObj = _networkService.GetConnectedStudents()
                .FirstOrDefault(s => string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase));
            string studentName = studentObj?.Name ?? code;

            string mappedStatus = statusToken switch
            {
                "DONE" or "OK" or "COMPLETED" => "Completed",
                "ERR" or "ERROR" or "FAILED" => "Failed",
                _ => "Downloading"
            };

            string fileName = fileId;
            if (QASmartClass.Services.BroadcastStateService.Instance.SharedFilesById.TryGetValue(fileId, out var desc))
            {
                fileName = desc.FileName;
            }

            var rec = ActiveDeliveries.AddOrUpdate(
                key,
                _ => new StudentDeliveryRecord
                {
                    StudentCode = code,
                    StudentName = studentName,
                    FileId = fileId,
                    FileName = fileName,
                    ProgressPercent = mappedStatus == "Completed" ? 100 : pct,
                    Status = mappedStatus,
                    ErrorDetail = detail,
                    UpdatedAt = DateTime.Now
                },
                (_, existing) =>
                {
                    existing.StudentName = studentName;
                    existing.FileName = fileName;
                    existing.ProgressPercent = mappedStatus == "Completed" ? 100 : pct;
                    existing.Status = mappedStatus;
                    existing.ErrorDetail = detail;
                    existing.UpdatedAt = DateTime.Now;
                    return existing;
                });

            if (mappedStatus == "Failed")
            {
                lock (LastFailedStudentCodes)
                {
                    if (!LastFailedStudentCodes.Contains(code))
                        LastFailedStudentCodes.Add(code);
                }
            }
            else if (mappedStatus == "Completed")
            {
                lock (LastFailedStudentCodes)
                {
                    LastFailedStudentCodes.Remove(code);
                }
            }

            TransferProgress?.Invoke(this, new FileTransferProgressEventArgs
            {
                StudentCode = code,
                FileName = fileName,
                BytesTransferred = rec.ProgressPercent,
                TotalBytes = 100,
                Direction = TransferDirection.Send,
                ProgressPercent = rec.ProgressPercent
            });
            DeliveryStatusChanged?.Invoke(this, rec);
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
        //  Server-Centric HTTP Pull + TCP 29877 Control (Firewall-Proof cho Windows PC & Android)
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Đăng ký file lên HTTP Chunked Stream của Giáo viên và đẩy lệnh CMD|FILE_OFFER qua TCP 29877
        /// để cả Học sinh Windows PC &amp; Android chủ động tải ngầm về thư mục cục bộ (xuyên 100% Firewall).
        /// </summary>
        public async Task<(int TargetCount, string FileId)> OfferFileViaHttpPullAsync(
            string filePath,
            IEnumerable<string>? targetCodes = null,
            bool autoOpenOnStudent = false)
        {
            if (!File.Exists(filePath))
            {
                Error?.Invoke(this, $"File not found: {filePath}");
                return (0, string.Empty);
            }

            lock (LastFailedStudentCodes)
            {
                LastFailedStudentCodes.Clear();
            }

            var bState = QASmartClass.Services.BroadcastStateService.Instance;
            var desc = bState.RegisterSharedFile(filePath, autoOpenOnStudent);
            string token = bState.EnsureBroadcastToken();
            int webPort = _networkService.WebBridge?.WebPort ?? 8080;
            string serverIp = !string.IsNullOrWhiteSpace(_networkService.ServerIP) ? _networkService.ServerIP : "127.0.0.1";
            string downloadUrl = $"http://{serverIp}:{webPort}/api/files/download?id={Uri.EscapeDataString(desc.FileId)}&token={Uri.EscapeDataString(token)}";

            // Sanitize pipe delimiter in filename just in case
            string safeFileName = desc.FileName.Replace('|', '_');
            string autoOpenFlag = autoOpenOnStudent ? "1" : "0";
            string offerCmd = $"CMD|FILE_OFFER|{desc.FileId}|{safeFileName}|{desc.FileSize}|{autoOpenFlag}|{webPort}|{token}|{downloadUrl}";

            var allOnline = _networkService.GetConnectedStudents();
            HashSet<string>? targetSet = targetCodes != null
                ? new HashSet<string>(targetCodes, StringComparer.OrdinalIgnoreCase)
                : null;

            var matchedStudents = targetSet == null
                ? allOnline.ToList()
                : allOnline.Where(s => targetSet.Contains(s.Code)).ToList();

            foreach (var st in matchedStudents)
            {
                string key = $"{desc.FileId}_{st.Code}";
                var rec = new StudentDeliveryRecord
                {
                    StudentCode = st.Code,
                    StudentName = st.Name,
                    FileId = desc.FileId,
                    FileName = desc.FileName,
                    ProgressPercent = 0,
                    Status = "Pending",
                    UpdatedAt = DateTime.Now
                };
                ActiveDeliveries[key] = rec;
                DeliveryStatusChanged?.Invoke(this, rec);
            }

            if (_networkService.IsBroadcasting)
            {
                if (targetSet == null)
                {
                    await _networkService.SendCommandAsync(offerCmd);
                }
                else if (matchedStudents.Count > 0)
                {
                    await _networkService.SendToStudentsAsync(matchedStudents.Select(s => s.Code), offerCmd);
                }
            }

            Log.Information("Offered file via HTTP Pull: {File} ({Size:N0} bytes, Id={Id}, AutoOpen={AutoOpen}) → {Count} online students",
                desc.FileName, desc.FileSize, desc.FileId, autoOpenOnStudent, matchedStudents.Count);

            return (matchedStudents.Count, desc.FileId);
        }

        /// <summary>
        /// Nhận tệp nộp bài từ Học sinh (Windows PC &amp; Android) qua HTTP POST /api/files/upload theo luồng 64KB chunked trực tiếp xuống ổ đĩa (~0 MB RAM).
        /// </summary>
        public async Task<(bool Success, int StatusCode, string Message, string SavedFileName)> SaveIncomingHttpUploadAsync(
            Stream bodyStream,
            byte[] initialBuffer,
            int initialOffset,
            int initialCount,
            long contentLength,
            string studentCode,
            string studentName,
            string originalFilename,
            CancellationToken ct)
        {
            if (QASmartTouch.App.AssessmentState.IsLocked)
            {
                return (false, 403, "Bài tập đã bị khóa bởi giáo viên. Không thể nộp bài.", string.Empty);
            }

            if (contentLength <= 0)
            {
                return (false, 400, "Tệp nộp bài trống (0 byte) hoặc thiếu Content-Length.", string.Empty);
            }

            const long MAX_UPLOAD_BYTES = 500L * 1024 * 1024; // 500 MB max
            if (contentLength > MAX_UPLOAD_BYTES)
            {
                return (false, 413, "Tệp nộp bài vượt quá giới hạn tối đa 500 MB.", string.Empty);
            }

            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(_receiveFolder)) ?? "C");
                long requiredFree = Math.Max(50L * 1024 * 1024, contentLength + 10L * 1024 * 1024);
                if (drive.AvailableFreeSpace < requiredFree)
                {
                    return (false, 507, "Dung lượng bộ nhớ lưu trữ trên máy giáo viên đã đầy.", string.Empty);
                }
            }
            catch (Exception exDrive)
            {
                Log.Warning("Không kiểm tra được dung lượng đĩa khi upload HTTP: {Err}", exDrive.Message);
            }

            string cleanCode = string.IsNullOrWhiteSpace(studentCode) ? "HS00001" : Path.GetFileName(studentCode.Trim());
            string cleanName = string.IsNullOrWhiteSpace(studentName) ? "Hoc_sinh" : studentName.Trim();
            string safeOrigName = Path.GetFileName(string.IsNullOrWhiteSpace(originalFilename) ? "BaiLam.dat" : originalFilename.Trim());
            if (string.IsNullOrWhiteSpace(safeOrigName)) safeOrigName = "BaiLam.dat";

            Directory.CreateDirectory(_receiveFolder);
            string normalizedName = NormalizeFilename(cleanCode, cleanName, safeOrigName);
            string savePath = Path.Combine(_receiveFolder, normalizedName);
            int dupCounter = 1;
            while (File.Exists(savePath))
            {
                string ext = Path.GetExtension(normalizedName);
                string baseNoExt = Path.GetFileNameWithoutExtension(normalizedName);
                normalizedName = $"{baseNoExt}_{dupCounter++}{ext}";
                savePath = Path.Combine(_receiveFolder, normalizedName);
            }

            string tempPartPath = savePath + ".part_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            long received = 0;

            try
            {
                using (var fs = new FileStream(tempPartPath, FileMode.Create, FileAccess.Write, FileShare.None, BUFFER_SIZE, useAsync: true))
                {
                    if (initialCount > 0)
                    {
                        int firstWrite = (int)Math.Min(initialCount, contentLength);
                        await fs.WriteAsync(initialBuffer.AsMemory(initialOffset, firstWrite), ct);
                        received += firstWrite;
                    }

                    var buffer = new byte[BUFFER_SIZE];
                    int lastReportedPct = -1;

                    while (received < contentLength && !ct.IsCancellationRequested)
                    {
                        int toRead = (int)Math.Min(BUFFER_SIZE, contentLength - received);
                        int bytesRead = await bodyStream.ReadAsync(buffer.AsMemory(0, toRead), ct);
                        if (bytesRead <= 0) break;

                        await fs.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                        received += bytesRead;

                        int pct = contentLength > 0 ? (int)(received * 100 / contentLength) : 100;
                        if (pct != lastReportedPct && (pct == 100 || pct - lastReportedPct >= 5))
                        {
                            lastReportedPct = pct;
                            TransferProgress?.Invoke(this, new FileTransferProgressEventArgs
                            {
                                StudentCode = cleanCode,
                                FileName = safeOrigName,
                                BytesTransferred = received,
                                TotalBytes = contentLength,
                                Direction = TransferDirection.Receive,
                                ProgressPercent = pct
                            });
                        }
                    }

                    await fs.FlushAsync(ct);
                }

                if (received < contentLength)
                {
                    try { if (File.Exists(tempPartPath)) File.Delete(tempPartPath); } catch { }
                    return (false, 400, $"Luồng tải lên bị ngắt giữa chừng ({received}/{contentLength} bytes).", string.Empty);
                }

                File.Move(tempPartPath, savePath, overwrite: true);

                if (Path.GetExtension(savePath).Equals(".zip", StringComparison.OrdinalIgnoreCase) && IsZipContentDangerous(savePath))
                {
                    Log.Warning("Cảnh báo: File ZIP nộp bài {File} từ {Code} có chứa tệp thực thi bên trong.", normalizedName, cleanCode);
                }

                Log.Information("HTTP Upload file received: {File} ({Size:N0} bytes) from {Code} ({Name})",
                    normalizedName, received, cleanCode, cleanName);

                FileReceived?.Invoke(this, new FileReceivedEventArgs
                {
                    StudentCode = cleanCode,
                    StudentName = cleanName,
                    OriginalFilename = safeOrigName,
                    NormalizedFilename = normalizedName,
                    SavePath = savePath,
                    FileSize = received
                });

                if (_networkService.IsBroadcasting)
                {
                    _ = _networkService.SendToStudentAsync(cleanCode, $"CMD|FILE_SUBMIT_ACK|{normalizedName}|{received}");
                }

                return (true, 200, "Nộp bài thành công!", normalizedName);
            }
            catch (Exception ex)
            {
                try { if (File.Exists(tempPartPath)) File.Delete(tempPartPath); } catch { }
                Log.Warning("SaveIncomingHttpUploadAsync failed for {Code}: {Err}", cleanCode, ex.Message);
                return (false, 500, $"Lỗi lưu tệp trên máy Giáo viên: {ex.Message}", string.Empty);
            }
        }

        /// <summary>Phát file cho tất cả HS đang online</summary>
        public async Task<int> DistributeFileAsync(string filePath, bool autoOpenOnStudent = false)
        {
            var res = await OfferFileViaHttpPullAsync(filePath, null, autoOpenOnStudent);
            return res.TargetCount;

            /* // [MYTHWARE_LITE] Cơ chế cũ GV kết nối ngược TCP 29879 vào IP máy HS (dễ bị chặn bởi Windows Public Firewall & không tương thích Android):
            if (!File.Exists(filePath))
            {
                Error?.Invoke(this, $"File not found: {filePath}");
                return 0;
            }
            ...
            */
        }

        /// <summary>Phát file cho danh sách HS được chọn</summary>
        public async Task<int> DistributeFileToTargetsAsync(string filePath, IEnumerable<string>? targetCodes, bool autoOpenOnStudent = false)
        {
            var res = await OfferFileViaHttpPullAsync(filePath, targetCodes, autoOpenOnStudent);
            return res.TargetCount;

            /* // [MYTHWARE_LITE] Cơ chế cũ GV kết nối ngược TCP 29879 vào IP máy HS:
            if (targetCodes == null)
            {
                return await DistributeFileAsync(filePath);
            }
            ...
            */
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

        public async Task<bool> SendFileToStudentAsync(string filePath, string studentCode, bool autoOpenOnStudent = false)
        {
            if (!File.Exists(filePath)) return false;
            var res = await OfferFileViaHttpPullAsync(filePath, new[] { studentCode }, autoOpenOnStudent);
            return res.TargetCount > 0;

            /* // [MYTHWARE_LITE] Cơ chế cũ GV kết nối ngược TCP 29879 vào IP máy HS:
            var student = _networkService.GetConnectedStudents().FirstOrDefault(s => s.Code == studentCode);
            if (student == null) return false;
            ...
            */
        }

        public void Dispose()
        {
            _networkService.MessageReceived -= OnStudentControlMessage;
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
    public class StudentDeliveryRecord
    {
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int ProgressPercent { get; set; }
        public string Status { get; set; } = "Pending"; // Pending | Downloading | Completed | Failed
        public string ErrorDetail { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

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
