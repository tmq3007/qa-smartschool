using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using QASmartClass.Classroom.Services;
using QASmartClass.Classroom.Helpers;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class FileTransferPage : Page
    {
        private readonly List<FileItem> _files = new();
        private bool _sending = false;
        private QASmartClass.Controls.NotificationWindow? _notifyWin;

        /// <summary>
        /// Ghi nhật ký sự kiện vào CSDL SQLite trên luồng nền (v4.4 — tránh đơ UI thread).
        /// </summary>
        private void WriteLogBackground(string eventType, string actor, string details)
        {
            Task.Run(() =>
            {
                try
                {
                    using (var db = new QASmartClass.Data.AppDbContext())
                    {
                        db.EventLogs.Add(new Data.EventLog
                        {
                            EventType = eventType,
                            Actor = actor,
                            Details = details,
                            Timestamp = DateTime.Now
                        });
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error("WriteLogBackground error in FileTransfer: {Err}", ex.Message);
                }
            });
        }

        private System.Collections.Generic.List<QASmartClass.Data.Student>? _cachedStudents;
        private const string DESC_PLACEHOLDER = "Nhập yêu cầu bài tập cho HS...";
        private bool _isDescPlaceholderActive = true;

        public FileTransferPage()
        {
            InitializeComponent();
            Loaded += async (s, ev) =>
            {
                RefreshFileList();
                StartFileListener();
                LoadSubmissions();

                // Load students and groups
                try
                {
                    // → ClassroomAppContext
                    var studentsQuery = ClassroomAppContext.Db?.Students?.AsQueryable();
                    string activeClass = ClassroomAppContext.Session?.CurrentClassName;
                    if (!string.IsNullOrEmpty(activeClass))
                    {
                        studentsQuery = studentsQuery?.Where(s => s.ClassName == activeClass);
                    }
                    _cachedStudents = await Task.Run(() => studentsQuery?.ToList());
                }
                catch (Exception ex) { Log.Warning("Failed to cache students in FileTransferPage: {Err}", ex.Message); }

                Dispatcher.Invoke(() =>
                {
                    var state = QASmartClass.Services.BroadcastStateService.Instance;
                    if (state.TargetSelectionMode == "GROUP")
                    {
                        rbGroup.IsChecked = true;
                        LoadGroupList();
                    }
                    else if (state.TargetSelectionMode == "INDIVIDUAL")
                    {
                        rbIndividual.IsChecked = true;
                        LoadStudentList();
                    }
                    else
                    {
                        rbAllStudents.IsChecked = true;
                    }
                    UpdateRecipientCount();
                });

                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.PreviewKeyDown -= Window_PreviewKeyDown;
                    window.PreviewKeyDown += Window_PreviewKeyDown;
                }

                // Populate deadline Hour/Minute ComboBoxes
                cmbDeadlineHour.Items.Clear();
                for (int i = 0; i < 24; i++)
                {
                    cmbDeadlineHour.Items.Add(i.ToString("D2"));
                }
                cmbDeadlineMinute.Items.Clear();
                for (int i = 0; i < 60; i += 5)
                {
                    cmbDeadlineMinute.Items.Add(i.ToString("D2"));
                }
                if (!cmbDeadlineMinute.Items.Contains("59"))
                {
                    cmbDeadlineMinute.Items.Add("59");
                }

                // Default deadline = today + 1 day
                dpDeadline.SelectedDate = DateTime.Today.AddDays(1);
                cmbDeadlineHour.SelectedValue = "23";
                cmbDeadlineMinute.SelectedValue = "59";

                // Restore assignment desc if any
                // → ClassroomAppContext
                if (!string.IsNullOrEmpty(QASmartTouch.App.AssessmentState.AssignmentDescription))
                {
                    txtAssignmentDesc.Text = QASmartTouch.App.AssessmentState.AssignmentDescription;
                    txtAssignmentDesc.Foreground = System.Windows.Media.Brushes.Black;
                    _isDescPlaceholderActive = false;
                }
                else
                {
                    InitializeDescPlaceholder();
                }
                chkLockSubmission.IsChecked = QASmartTouch.App.AssessmentState.IsLocked;
            };

            Unloaded += (s, ev) =>
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.PreviewKeyDown -= Window_PreviewKeyDown;
                }
            };
        }

        private void InitializeDescPlaceholder()
        {
            txtAssignmentDesc.Text = DESC_PLACEHOLDER;
            txtAssignmentDesc.Foreground = System.Windows.Media.Brushes.Gray;
            _isDescPlaceholderActive = true;
        }

        private void Desc_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_isDescPlaceholderActive)
            {
                txtAssignmentDesc.Text = "";
                txtAssignmentDesc.Foreground = System.Windows.Media.Brushes.Black;
                _isDescPlaceholderActive = false;
            }
        }

        private void Desc_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAssignmentDesc.Text))
            {
                InitializeDescPlaceholder();
            }
        }

        /// <summary>Auto-start FileTransfer listener (TCP 29879) để nhận bài từ HS</summary>
        private void StartFileListener()
        {
            try
            {
                // → ClassroomAppContext
                var ft = ClassroomAppContext.FileTransfer;

                if (ft != null)
                {
                    ft.FileReceived -= OnFileReceived;
                    ft.FileReceived += OnFileReceived;

                    ft.TransferProgress -= OnTransferProgress;
                    ft.TransferProgress += OnTransferProgress;

                    if (!ft.IsListening)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await ft.StartListeningAsync();
                                Dispatcher.Invoke(() =>
                                {
                                    txtStatus.Text = $"📡 Sẵn sàng — Lắng nghe trên port {FileTransferService.FILE_PORT}";
                                    txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(117, 117, 117));
                                });
                            }
                            catch (System.Net.Sockets.SocketException)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    txtStatus.Text = $"❌ Lỗi: Cổng kết nối {FileTransferService.FILE_PORT} bị chiếm dụng. Vui lòng tắt ứng dụng chạy ẩn.";
                                    txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(198, 40, 40));
                                });
                            }
                            catch (Exception ex)
                            {
                                Log.Warning("StartListeningAsync error: {Err}", ex.Message);
                            }
                        });
                    }
                    else
                    {
                        txtStatus.Text = $"📡 Sẵn sàng — Lắng nghe trên port {FileTransferService.FILE_PORT}";
                        txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(117, 117, 117));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("StartFileListener error: {Err}", ex.Message);
            }
        }

        private void OnFileReceived(object? sender, FileReceivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                txtStatus.Text = $"📥 Đã nhận: {e.NormalizedFilename} ({FormatSize(e.FileSize)}) từ {e.StudentName}";
            });

            // Save to DB on background thread to prevent thread conflict and UI freeze
            _ = Task.Run(async () =>
            {
                try
                {
                    using var db = new QASmartClass.Data.AppDbContext();
                    db.EventLogs.Add(new Data.EventLog
                    {
                        EventType = "FILE_RECEIVED",
                        Actor = e.StudentCode,
                        Details = $"Nộp bài: {e.NormalizedFilename} ({FormatSize(e.FileSize)})",
                        Timestamp = DateTime.Now
                    });
                    db.FileTransfers.Add(new Data.FileTransferRecord
                    {
                        FileName = e.NormalizedFilename,
                        FileSizeBytes = e.FileSize,
                        Direction = "StudentToTeacher",
                        Status = "Completed",
                        ProgressPercent = 100,
                        CreatedAt = DateTime.Now
                    });
                    await db.SaveChangesAsync();

                    Dispatcher.Invoke(() =>
                    {
                        LoadSubmissions();
                    });
                }
                catch (Exception ex)
                {
                    Log.Warning("Save file record error: {Err}", ex.Message);
                }
            });

            Log.Information("File received from {Code}: {File}", e.StudentCode, e.NormalizedFilename);
        }

        private void OnTransferProgress(object? sender, FileTransferProgressEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.Value = e.ProgressPercent;
                var dir = e.Direction == TransferDirection.Send ? "📤" : "📥";
                txtStatus.Text = $"{dir} {e.FileName} — {e.ProgressPercent}% ({e.StudentCode})";

                if (_notifyWin != null && _notifyWin.IsLoaded)
                {
                    _notifyWin.UpdateProgress(e.ProgressPercent);
                }
            });
        }

        private void RefreshFileList()
        {
            fileListBox.ItemsSource = null;
            fileListBox.ItemsSource = _files;
            txtFileCount.Text = $"{_files.Count} file";
            btnSend.IsEnabled = _files.Any();
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Chọn bài tập / tài liệu để phát",
                Multiselect = true,
                Filter = "Tất cả|*.pdf;*.docx;*.xlsx;*.pptx;*.jpg;*.png;*.mp4;*.zip|" +
                         "PDF|*.pdf|Word|*.docx|Excel|*.xlsx|PowerPoint|*.pptx|Ảnh|*.jpg;*.png|Video|*.mp4|ZIP|*.zip"
            };
            if (dlg.ShowDialog() == true)
            {
                foreach (var path in dlg.FileNames)
                {
                    if (_files.All(f => f.Path != path))
                    {
                        var info = new FileInfo(path);
                        _files.Add(new FileItem
                        {
                            Name = info.Name,
                            Path = path,
                            SizeText = FormatSize(info.Length),
                            TypeIcon = GetIcon(info.Extension)
                        });
                    }
                }
                RefreshFileList();
                Log.Information("Files added to queue: {Count}", _files.Count);
            }
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is FileItem item)
            {
                _files.Remove(item);
                RefreshFileList();
            }
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            _files.Clear();
            RefreshFileList();
        }

        /// <summary>Phát file qua TCP 29879 cho tất cả HS đang kết nối (+ local copy fallback)</summary>
        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (!_files.Any()) return;
            if (_sending) return;

            _sending = true;
            btnSend.Content = "⏳ Đang phát...";
            btnSend.IsEnabled = false;
            progressBar.Value = 0;
            txtStatus.Text = $"📤 Đang phát {_files.Count} file...";

            // ═══ Save assignment metadata to App shared state ═══
            // → ClassroomAppContext
            var desc = _isDescPlaceholderActive ? "" : (txtAssignmentDesc.Text?.Trim() ?? "");
            QASmartTouch.App.AssessmentState.AssignmentDescription = desc;
            QASmartTouch.App.AssessmentState.AssignmentSentTime = DateTime.Now;

            // Parse deadline
            if (dpDeadline.SelectedDate.HasValue)
            {
                var deadlineDate = dpDeadline.SelectedDate.Value;
                string hourStr = cmbDeadlineHour.SelectedValue?.ToString() ?? "23";
                string minuteStr = cmbDeadlineMinute.SelectedValue?.ToString() ?? "59";
                if (int.TryParse(hourStr, out int hour) && int.TryParse(minuteStr, out int minute))
                {
                    QASmartTouch.App.AssessmentState.AssignmentDeadline = deadlineDate.Date.AddHours(hour).AddMinutes(minute);
                }
                else
                {
                    QASmartTouch.App.AssessmentState.AssignmentDeadline = deadlineDate.Date.AddHours(23).AddMinutes(59);
                }
            }
            else
            {
                QASmartTouch.App.AssessmentState.AssignmentDeadline = null;
            }

            // Send assignment info via network or local command bus
            var targetCodes = GetTargetStudentCodes();
            var deadlineStr = QASmartTouch.App.AssessmentState.AssignmentDeadline?.ToString("yyyy-MM-dd HH:mm") ?? "none";
            var cmdAssignment = $"CMD|ASSIGNMENT|{deadlineStr}|{QASmartTouch.App.AssessmentState.AssignmentDescription}";
            var net = ClassroomAppContext.Network;
            if (net?.IsBroadcasting == true)
            {
                if (targetCodes == null)
                    _ = net.SendCommandAsync(cmdAssignment);
                else
                    _ = net.SendToStudentsAsync(targetCodes, cmdAssignment);
            }
            else
            {
                ClassroomAppContext.DispatchCommand(cmdAssignment);
            }

            try
            {
                // → ClassroomAppContext
                var ft = ClassroomAppContext.FileTransfer;
                int totalSent = 0;
                bool localCopyDone = false;

                // Lấy danh sách học sinh online thực tế (TCP) để theo dõi kết quả
                var onlineStudents = new List<QASmartClass.Controls.NotificationWindow.FailedStudentItem>();
                if (ClassroomAppContext.Network != null)
                {
                    var targetSet = targetCodes != null ? new HashSet<string>(targetCodes, StringComparer.OrdinalIgnoreCase) : null;
                    foreach (var s in ClassroomAppContext.Network.GetConnectedStudents())
                    {
                        if (targetSet == null || targetSet.Contains(s.Code))
                        {
                            onlineStudents.Add(new QASmartClass.Controls.NotificationWindow.FailedStudentItem { Code = s.Code, Name = s.Name });
                        }
                    }
                }

                if (onlineStudents.Count > 0)
                {
                    _notifyWin = new QASmartClass.Controls.NotificationWindow();
                    _notifyWin.SetStatusProcessing($"Đang phát {_files.Count} tài liệu...", $"Đang gửi tới {onlineStudents.Count} học sinh...");
                    _notifyWin.Show();
                }
                else
                {
                    var warnResult = MessageBox.Show(
                        "⚠️ KHÔNG CÓ HỌC SINH ONLINE\n\n" +
                        "Hệ thống phát hiện hiện tại không có học sinh nào đang trực tuyến.\n" +
                        "Bạn có chắc chắn muốn tiếp tục phát bài không?\n" +
                        "(Chức năng sao chép cục bộ vẫn sẽ hoạt động trên máy tính này).",
                        "Xác nhận phát bài",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    if (warnResult != MessageBoxResult.Yes)
                    {
                        _sending = false;
                        btnSend.Content = "📤 Phát bài cho HS";
                        btnSend.IsEnabled = true;
                        txtStatus.Text = "📂 Chọn file → Nhập yêu cầu → Nhấn Phát bài";
                        return;
                    }
                }

                for (int i = 0; i < _files.Count; i++)
                {
                    var file = _files[i];
                    txtStatus.Text = $"📤 Phát file {i + 1}/{_files.Count}: {file.Name}";

                    // Try TCP distribute to remote students
                    var sent = await ft.DistributeFileToTargetsAsync(file.Path, targetCodes);
                    totalSent += sent;

                    // ═══ LOCAL COPY FALLBACK — Khi cùng 1 máy ═══
                    // Copy file trực tiếp vào thư mục nhận của HS
                    try
                    {
                        var studentReceiveFolder = System.IO.Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                            "QA SmartClass", "ReceivedFiles");
                        Directory.CreateDirectory(studentReceiveFolder);

                        var destPath = System.IO.Path.Combine(studentReceiveFolder, file.Name);
                        // Avoid overwrite
                        int counter = 1;
                        while (File.Exists(destPath))
                        {
                            var ext2 = System.IO.Path.GetExtension(file.Name);
                            var name2 = System.IO.Path.GetFileNameWithoutExtension(file.Name);
                            destPath = System.IO.Path.Combine(studentReceiveFolder, $"{name2}_{counter++}{ext2}");
                        }
                        File.Copy(file.Path, destPath);
                        localCopyDone = true;
                        Log.Information("Local copy: {Src} → {Dst}", file.Path, destPath);
                    }
                    catch (Exception exCopy)
                    {
                        Log.Warning("Local copy failed: {Err}", exCopy.Message);
                    }

                    progressBar.Value = (i + 1) * 100.0 / _files.Count;

                    // ═══ Gửi CMD|FILE_BROADCAST cho HS hiển thị nội dung file ═══
                    QASmartClass.Services.BroadcastStateService.Instance.AddBroadcastFile(file.Path);
                    int webPort = 8080;
                    if (ClassroomAppContext.Network?.WebBridge != null)
                    {
                        webPort = ClassroomAppContext.Network.WebBridge.WebPort;
                    }
                    if (string.IsNullOrEmpty(QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken))
                    {
                        QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken = "TK_" + Guid.NewGuid().ToString("N").Substring(0, 16);
                    }
                    var token = QASmartClass.Services.BroadcastStateService.Instance.BroadcastToken;
                    var cmd = $"CMD|FILE_BROADCAST|{file.Path}|{webPort}|{token}";
                    ClassroomAppContext.LessonState.LastTeacherCommand = cmd;
                    ClassroomAppContext.LessonState.LastCommandTime = DateTime.Now;

                    net = ClassroomAppContext.Network;
                    if (net?.IsBroadcasting == true)
                    {
                        if (targetCodes == null)
                            _ = net.SendCommandAsync(cmd);
                        else
                            _ = net.SendToStudentsAsync(targetCodes, cmd);
                    }
                    else
                    {
                        ClassroomAppContext.DispatchCommand(cmd);
                    }
                }

                progressBar.Value = 100;
                int displayCount = totalSent > 0 ? totalSent : (localCopyDone ? 1 : 0);
                string mode = totalSent > 0 ? "TCP" : (localCopyDone ? "cùng máy" : "");
                txtStatus.Text = $"✅ Phát thành công {_files.Count} file → {displayCount} HS ({mode})!";

                if (_notifyWin != null && _notifyWin.IsLoaded)
                {
                    var failedCodes = new HashSet<string>();
                    if (ft != null)
                    {
                        lock (ft.LastFailedStudentCodes)
                        {
                            foreach (var c in ft.LastFailedStudentCodes) failedCodes.Add(c);
                        }
                    }

                    var failedItems = new List<QASmartClass.Controls.NotificationWindow.FailedStudentItem>();
                    foreach (var code in failedCodes)
                    {
                        var stud = onlineStudents.Find(s => s.Code == code);
                        if (stud != null) failedItems.Add(stud);
                    }

                    if (failedItems.Count > 0)
                    {
                        // Ghi nhật ký lỗi kết nối mạng phát file vào SQLite nền (v4.4 — di chuyển khỏi UI thread)
                        var failNames = string.Join(", ", failedItems.Select(f => $"{f.Name} ({f.Code})"));
                        WriteLogBackground("NETWORK_ERROR", "SYSTEM", $"[LỖI MẠNG] Phát tài liệu thất bại tới các học sinh: {failNames}");

                        string failDesc = $"Chỉ có {onlineStudents.Count - failedItems.Count}/{onlineStudents.Count} học sinh nhận được (Thất bại: {failedItems.Count}).";
                        _notifyWin.SetStatusWarning("Phát tài liệu chưa hoàn tất!", failDesc, failedItems, async (failedCode) =>
                        {
                            if (ft != null)
                            {
                                foreach (var f in _files)
                                {
                                    await ft.SendFileToStudentAsync(f.Path, failedCode);
                                }
                            }
                        });

                        // Gửi lại hàng loạt cho toàn bộ danh sách máy lỗi (v4.3)
                        _notifyWin.OnResendAllClicked = async (failedCodesList) =>
                        {
                            if (ft != null)
                            {
                                foreach (var failedCode in failedCodesList)
                                {
                                    foreach (var f in _files)
                                    {
                                        await ft.SendFileToStudentAsync(f.Path, failedCode);
                                    }
                                }
                            }
                        };
                    }
                    else
                    {
                        _notifyWin.SetStatusSuccess("Phát tài liệu thành công!", $"Tất cả {onlineStudents.Count} học sinh đã nhận bài giảng.");
                    }
                }

                // Log to DB nền (v4.4 — di chuyển khỏi UI thread)
                WriteLogBackground("FILE", "GV", $"Phát {_files.Count} file: {string.Join(", ", _files.Select(f => f.Name))} → {displayCount} HS ({mode})");

                Log.Information("FileTransfer distribute complete: {Count} files → {Sent} TCP + local={Local}",
                    _files.Count, totalSent, localCopyDone);
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"❌ Lỗi phát file: {ex.Message}";
                Log.Error(ex, "FileTransfer Send_Click error");
            }
            finally
            {
                _sending = false;
                btnSend.Content = "📤 Phát bài";
                btnSend.IsEnabled = true;
            }
        }

        /// <summary>Mở thư mục nhận bài từ HS (hỗ trợ cùng máy)</summary>
        private void Collect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // → ClassroomAppContext
                var folder = ClassroomAppContext.FileTransfer?.ReceiveFolder;
                Directory.CreateDirectory(folder);

                // Count received files
                var files = Directory.GetFiles(folder);

                if (files.Any())
                {
                    System.Diagnostics.Process.Start("explorer.exe", folder);
                    txtStatus.Text = $"📂 Thư mục thu bài: {files.Length} file đã nhận";

                    // Build detailed file list
                    var fileDetails = files
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .Take(10)
                        .Select(f =>
                        {
                            var fi = new FileInfo(f);
                            return $"  📄 {fi.Name} ({FormatSize(fi.Length)}) — {fi.LastWriteTime:HH:mm dd/MM}";
                        });

                    MessageBox.Show(
                        $"📥 Đã thu {files.Length} bài:\n\nThư mục: {folder}\n\n" +
                        string.Join("\n", fileDetails) +
                        (files.Length > 10 ? $"\n  ... và {files.Length - 10} file khác" : ""),
                        "Thu bài", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ClassroomDialog.Info(
                        $"📂 Thư mục thu bài:\n{folder}\n\n" +
                        $"Chưa có bài nào được nộp.\n\n" +
                        $"💡 HS nộp bài:\n" +
                        $"  • Qua mạng: TCP port {FileTransferService.FILE_PORT}\n" +
                        $"  • Cùng máy: Nhấn \"Nộp bài\" ở giao diện HS", "Thu bài");
                }

                Log.Information("Collect folder opened: {Folder}, files: {Count}", folder, files.Length);
            }
            catch (Exception ex)
            {
                Log.Warning("Collect error: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  SUBMISSION TRACKING — Hiển thị danh sách bài nộp bên panel phải
        // ═══════════════════════════════════════════════════════════

        private void LoadSubmissions()
        {
            try
            {
                submissionList.Children.Clear();
                // → ClassroomAppContext
                var folder = ClassroomAppContext.FileTransfer?.ReceiveFolder;
                Directory.CreateDirectory(folder);

                var files = Directory.GetFiles(folder)
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                txtSubmissionCount.Text = $"{files.Count} bài";

                if (files.Count == 0)
                {
                    submissionList.Children.Add(new TextBlock
                    {
                        Text = "📭 Chưa có bài nào.\n\nKhi HS nộp bài, file sẽ tự động hiện ở đây.",
                        FontSize = 13, Foreground = System.Windows.Media.Brushes.Gray,
                        FontStyle = FontStyles.Italic, TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center, Margin = new Thickness(8, 20, 8, 20)
                    });
                    return;
                }

                // Tối ưu N+1: Đọc tất cả học sinh vào dictionary trong RAM một lần duy nhất qua context cục bộ
                Dictionary<string, string> studentLookup = new(StringComparer.OrdinalIgnoreCase);
                try
                {
                    using var db = new QASmartClass.Data.AppDbContext();
                    studentLookup = db.Students.ToDictionary(s => s.StudentCode, s => s.FullName, StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception exDb)
                {
                    Log.Warning("Không thể tải trước danh sách học sinh từ DB: {Err}", exDb.Message);
                }

                foreach (var filePath in files.Take(20))
                {
                    var fi = new FileInfo(filePath);
                    var row = new Border
                    {
                        Background = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(232, 245, 233)),
                        CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 6, 8, 6),
                        Margin = new Thickness(0, 0, 0, 4), Cursor = System.Windows.Input.Cursors.Hand
                    };
                    row.MouseLeftButtonDown += (_, _) =>
                    {
                        try
                        {
                            var ext = Path.GetExtension(filePath).ToLower();
                            string[] dangerousExtensions = { ".exe", ".bat", ".cmd", ".ps1", ".vbs", ".js", ".scr", ".lnk", ".sys", ".com" };
                            
                            if (dangerousExtensions.Contains(ext))
                            {
                                var result = MessageBox.Show(
                                    $"[CẢNH BÁO BẢO MẬT]\n\nFile nộp bài \"{Path.GetFileName(filePath)}\" chứa phần mở rộng nguy hiểm ({ext}).\n\nĐể đảm bảo an toàn cho máy tính giáo viên:\n- Nhấp 'Yes' nếu bạn chỉ muốn MỞ THƯ MỤC chứa file trong File Explorer (Khuyên dùng).\n- Nhấp 'No' để bỏ qua và không mở file.",
                                    "Cảnh báo Bảo mật Hệ thống",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Warning);

                                if (result == MessageBoxResult.Yes)
                                {
                                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                                }
                                return;
                            }

                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = filePath, UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Không thể mở file nộp bài: {Err}", ex.Message);
                        }
                    };

                    var dock = new DockPanel();
                    dock.Children.Add(new TextBlock
                    {
                        Text = "✅", FontSize = 12, VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 6, 0),
                        FontFamily = new System.Windows.Media.FontFamily("Segoe UI Emoji")
                    });
                    DockPanel.SetDock(dock.Children[0], Dock.Left);

                    var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    var fileName = fi.Name;
                    var partsName = fileName.Split('_');
                    string displayNameText = fileName;
                    
                    if (partsName.Length >= 3)
                    {
                        var code = partsName[0];
                        string fullName = "";

                        // Tra cứu bằng Dictionary trong RAM (O(1))
                        if (studentLookup.TryGetValue(code, out var nameFromDict))
                        {
                            fullName = nameFromDict;
                        }
                        else
                        {
                            // Tra cứu fallback theo mã ngắn
                            string shortCode = code;
                            if (code.StartsWith("HS0000")) shortCode = "HS" + code.Substring(6);
                            else if (code.StartsWith("HS000")) shortCode = "HS" + code.Substring(5);
                            else if (code.StartsWith("HS00")) shortCode = "HS" + code.Substring(4);

                            if (studentLookup.TryGetValue(shortCode, out var nameFromShortDict))
                            {
                                fullName = nameFromShortDict;
                            }
                        }

                        if (string.IsNullOrEmpty(fullName))
                        {
                            fullName = partsName[1].Replace("_", " ");
                        }

                        string origName = fileName;
                        if (partsName.Length >= 4)
                        {
                            var baseNameParts = partsName.Skip(2).Take(partsName.Length - 3);
                            origName = string.Join("_", baseNameParts) + Path.GetExtension(fileName);
                        }

                        displayNameText = $"{fullName} ({code}) — {origName}";
                    }

                    info.Children.Add(new TextBlock
                    {
                        Text = displayNameText, FontSize = 13, FontWeight = FontWeights.Bold,
                        Foreground = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(46, 125, 50)),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    });
                    info.Children.Add(new TextBlock
                    {
                        Text = $"{FormatSize(fi.Length)} · {fi.LastWriteTime:HH:mm dd/MM}",
                        FontSize = 12, Foreground = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(102, 187, 106))
                    });
                    dock.Children.Add(info);
                    row.Child = dock;
                    submissionList.Children.Add(row);
                }
            }
            catch (Exception ex) { Log.Warning("LoadSubmissions error: {Err}", ex.Message); }
        }

        private static string FormatSize(long bytes) =>
            bytes switch { < 1024 => $"{bytes} B", < 1048576 => $"{bytes / 1024.0:F1} KB", _ => $"{bytes / 1048576.0:F1} MB" };

        private static string GetIcon(string ext) => ext.ToLower() switch
        {
            ".pdf" => "📄", ".docx" or ".doc" => "📝", ".xlsx" or ".xls" => "📊",
            ".pptx" or ".ppt" => "📋", ".jpg" or ".png" or ".jpeg" => "🖼️",
            ".mp4" or ".avi" => "🎬", ".zip" or ".rar" => "🗜️", _ => "📁"
        };

        private void LockSubmission_Checked(object sender, RoutedEventArgs e)
        {
            QASmartTouch.App.AssessmentState.IsLocked = true;
            txtStatus.Text = "🔒 Đã khóa nhận bài nộp. Học sinh không thể gửi bài qua mạng LAN nữa.";
            txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(198, 40, 40));
            Log.Information("Giáo viên đã KHÓA nhận bài nộp.");
        }

        private void LockSubmission_Unchecked(object sender, RoutedEventArgs e)
        {
            QASmartTouch.App.AssessmentState.IsLocked = false;
            txtStatus.Text = $"📡 Sẵn sàng — Lắng nghe trên port {FileTransferService.FILE_PORT}";
            txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(117, 117, 117));
            Log.Information("Giáo viên đã MỞ KHÓA nhận bài nộp.");
        }

        public class ParsedFilenameResult
        {
            public string StudentCode { get; set; } = string.Empty;
            public string StudentName { get; set; } = string.Empty;
            public string OriginalName { get; set; } = string.Empty;
        }

        public static ParsedFilenameResult ParseNormalizedFilename(string filename, QASmartClass.Data.AppDbContext db)
        {
            var result = new ParsedFilenameResult();
            var ext = System.IO.Path.GetExtension(filename);
            var nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(filename);

            if (nameWithoutExt.Contains("__"))
            {
                var parts = nameWithoutExt.Split(new[] { "__" }, StringSplitOptions.None);
                if (parts.Length >= 3)
                {
                    result.StudentCode = parts[0];
                    result.StudentName = parts[1].Replace("_", " ");
                    result.OriginalName = parts[2].Replace("__", "_") + ext;
                    return result;
                }
            }

            var firstUnderscore = nameWithoutExt.IndexOf('_');
            if (firstUnderscore > 0)
            {
                var studentCode = nameWithoutExt.Substring(0, firstUnderscore);
                result.StudentCode = studentCode;

                if (studentCode.StartsWith("HS") && studentCode.Length < 7)
                {
                    var numPart = studentCode.Substring(2);
                    if (int.TryParse(numPart, out var num))
                    {
                        studentCode = $"HS{num:D5}";
                    }
                }

                var student = db.Students.FirstOrDefault(s => s.StudentCode == studentCode);
                if (student != null)
                {
                    result.StudentName = student.FullName;
                    var cleanName = CleanStudentName(student.FullName);
                    var prefix = studentCode + "_" + cleanName + "_";
                    if (nameWithoutExt.StartsWith(prefix))
                    {
                        var remaining = nameWithoutExt.Substring(prefix.Length);
                        var lastUnderscore = remaining.LastIndexOf('_');
                        if (lastUnderscore > 0)
                        {
                            result.OriginalName = remaining.Substring(0, lastUnderscore) + ext;
                        }
                        else
                        {
                            result.OriginalName = remaining + ext;
                        }
                    }
                    else
                    {
                        var lastUnderscore = nameWithoutExt.LastIndexOf('_');
                        if (lastUnderscore > firstUnderscore)
                        {
                            result.OriginalName = nameWithoutExt.Substring(firstUnderscore + 1, lastUnderscore - firstUnderscore - 1) + ext;
                        }
                    }
                }
                else
                {
                    var lastUnderscore = nameWithoutExt.LastIndexOf('_');
                    if (lastUnderscore > firstUnderscore)
                    {
                        result.OriginalName = nameWithoutExt.Substring(firstUnderscore + 1, lastUnderscore - firstUnderscore - 1) + ext;
                    }
                }
            }
            return result;
        }

        private static string CleanStudentName(string name)
        {
            var clean = name.Replace(" ", "_").Replace(".", "").Replace(",", "");
            var normalized = clean.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in normalized)
            {
                var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        // ─── RECIPIENTS SELECTOR LOGIC ───
        private void TargetChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (panelGroupPicker == null || panelStudentPicker == null) return;
            panelGroupPicker.Visibility = rbGroup.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            panelStudentPicker.Visibility = rbIndividual.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            if (rbIndividual.IsChecked == true) LoadStudentList();
            if (rbGroup.IsChecked == true) LoadGroupList();
            UpdateRecipientCount();
        }

        private void LoadGroupList()
        {
            try
            {
                groupCheckList.Children.Clear();
                // → ClassroomAppContext

                // Ưu tiên dữ liệu từ GroupPage (ClassroomAppContext.CurrentGroups)
                var state = QASmartClass.Services.BroadcastStateService.Instance;
                var groups = ClassroomAppContext.CurrentGroups;
                if (groups != null && groups.Count > 0)
                {
                    foreach (var g in groups)
                    {
                        bool isChecked = true;
                        if (state.TargetSelectionMode == "GROUP")
                        {
                            isChecked = state.TargetSelectedGroups.Contains(g.Index.ToString());
                        }

                        var cb = new CheckBox
                        {
                            Content = $"{g.Emoji} {g.Name} ({g.Members.Count} HS)",
                            FontSize = 11, IsChecked = isChecked, Margin = new Thickness(0, 0, 0, 4),
                            Tag = g.Index, ToolTip = string.Join(", ", g.Members)
                        };
                        cb.Checked += (_, _) => UpdateRecipientCount();
                        cb.Unchecked += (_, _) => UpdateRecipientCount();
                        groupCheckList.Children.Add(cb);
                    }
                }
                else
                {
                    // Fallback: nhóm theo ClassName trong DB
                    var students = _cachedStudents;
                    var classNames = students?.Select(s => s.ClassName)
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .Distinct().OrderBy(c => c).ToList();

                    if (classNames != null && classNames.Count > 0)
                    {
                        foreach (var cn in classNames)
                        {
                            int cnt = students!.Count(s => s.ClassName == cn);
                            bool isChecked = true;
                            if (state.TargetSelectionMode == "GROUP")
                            {
                                isChecked = state.TargetSelectedGroups.Contains(cn);
                            }

                            var cb = new CheckBox
                            {
                                Content = $"{cn} ({cnt} HS)",
                                FontSize = 11, IsChecked = isChecked, Margin = new Thickness(0, 0, 0, 4),
                                Tag = cn
                            };
                            cb.Checked += (_, _) => UpdateRecipientCount();
                            cb.Unchecked += (_, _) => UpdateRecipientCount();
                            groupCheckList.Children.Add(cb);
                        }
                    }
                    else
                    {
                        groupCheckList.Children.Add(new TextBlock
                        {
                            Text = "Chưa có nhóm. Vào mục 2.4 Nhóm học tập để tạo nhóm.",
                            FontSize = 10, Foreground = Brushes.Gray, FontStyle = FontStyles.Italic,
                            TextWrapping = TextWrapping.Wrap
                        });
                    }
                }
                UpdateRecipientCount();
            }
            catch (Exception ex) { Serilog.Log.Warning("LoadGroupList error: {Err}", ex.Message); }
        }

        private void LoadStudentList()
        {
            try
            {
                studentCheckList.Children.Clear();
                var students = _cachedStudents;
                if (students == null || students.Count == 0)
                {
                    studentCheckList.Children.Add(new TextBlock
                    {
                        Text = "Không có dữ liệu học sinh.",
                        FontSize = 11, Foreground = Brushes.Gray, FontStyle = FontStyles.Italic
                    });
                    UpdateRecipientCount();
                    return;
                }

                var query = txtSearchStudent.Text?.Trim();
                var list = students;
                if (!string.IsNullOrEmpty(query))
                {
                    list = students.Where(s => (s.FullName ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                                            || (s.StudentCode ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                                            || (s.PCName ?? "").Contains(query, StringComparison.OrdinalIgnoreCase))
                                   .ToList();
                }

                if (list.Count == 0)
                {
                    studentCheckList.Children.Add(new TextBlock
                    {
                        Text = "Không tìm thấy học sinh.",
                        FontSize = 11, Foreground = Brushes.Gray, FontStyle = FontStyles.Italic
                    });
                    UpdateRecipientCount();
                    return;
                }

                // Sắp xếp Alphabet theo tên tiếng Việt
                var sortedList = list.OrderBy(s => s.FullName).ToList();

                // → ClassroomAppContext
                var onlineStudents = ClassroomAppContext.Network?.GetConnectedStudents();

                var state = QASmartClass.Services.BroadcastStateService.Instance;

                foreach (var s in sortedList)
                {
                    var client = onlineStudents?.FirstOrDefault(c => c.Code.Equals(s.StudentCode, StringComparison.OrdinalIgnoreCase));
                    bool isOnline = client != null || s.IsOnline;
                    string statusIcon = isOnline ? "🟢" : "⚪";
                    string latencyStr = client != null ? $" ({client.LatencyMs}ms)" : "";
                    string className = string.IsNullOrEmpty(s.ClassName) ? "" : $" ({s.ClassName})";

                    bool isChecked = true;
                    if (state.TargetSelectionMode == "INDIVIDUAL")
                    {
                        isChecked = state.TargetSelectedStudents.Contains(s.StudentCode);
                    }

                    var cb = new CheckBox
                    {
                        Content = $"{statusIcon} {s.FullName}{className}{latencyStr}",
                        FontSize = 11, IsChecked = isChecked, Margin = new Thickness(0, 0, 0, 3),
                        Tag = s.StudentCode, ToolTip = $"Mã: {s.StudentCode} | IP: {s.IPAddress} | PC: {s.PCName}"
                    };
                    cb.Checked += (_, _) => UpdateRecipientCount();
                    cb.Unchecked += (_, _) => UpdateRecipientCount();
                    studentCheckList.Children.Add(cb);
                }

                UpdateRecipientCount();
            }
            catch (Exception ex) { Serilog.Log.Warning("LoadStudentList error: {Err}", ex.Message); }
        }

        private void SearchStudent_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadStudentList();
        }

        private void SelectAllGroups_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (var cb in groupCheckList.Children.OfType<CheckBox>())
                cb.IsChecked = true;
            UpdateRecipientCount();
        }

        private void DeselectAllGroups_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (var cb in groupCheckList.Children.OfType<CheckBox>())
                cb.IsChecked = false;
            UpdateRecipientCount();
        }

        private void SelectAllStudents_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (var cb in studentCheckList.Children.OfType<CheckBox>())
                cb.IsChecked = true;
            UpdateRecipientCount();
        }

        private void DeselectAllStudents_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (var cb in studentCheckList.Children.OfType<CheckBox>())
                cb.IsChecked = false;
            UpdateRecipientCount();
        }

        private void SaveTargetSelectionState()
        {
            try
            {
                var state = QASmartClass.Services.BroadcastStateService.Instance;
                if (rbAllStudents.IsChecked == true) state.TargetSelectionMode = "ALL";
                else if (rbGroup.IsChecked == true) state.TargetSelectionMode = "GROUP";
                else if (rbIndividual.IsChecked == true) state.TargetSelectionMode = "INDIVIDUAL";

                state.TargetSelectedGroups.Clear();
                foreach (var cb in groupCheckList.Children.OfType<CheckBox>())
                {
                    if (cb.IsChecked == true && cb.Tag != null)
                        state.TargetSelectedGroups.Add(cb.Tag.ToString()!);
                }

                state.TargetSelectedStudents.Clear();
                foreach (var cb in studentCheckList.Children.OfType<CheckBox>())
                {
                    if (cb.IsChecked == true && cb.Tag is string code)
                        state.TargetSelectedStudents.Add(code);
                }
            }
            catch (Exception ex) { Serilog.Log.Warning("SaveTargetSelectionState error: {Err}", ex.Message); }
        }

        private void UpdateRecipientCount()
        {
            try
            {
                SaveTargetSelectionState();
                if (txtRecipientCount == null) return;

                if (rbAllStudents.IsChecked == true)
                {
                    txtRecipientCount.Text = "→ Tất cả máy HS sẽ nhận";
                    txtRecipientCount.Foreground = (Brush)new BrushConverter().ConvertFromString("#1565C0");
                    return;
                }

                var targetCodes = GetTargetStudentCodes();
                if (targetCodes == null)
                {
                    txtRecipientCount.Text = "→ Tất cả máy HS sẽ nhận";
                    txtRecipientCount.Foreground = (Brush)new BrushConverter().ConvertFromString("#1565C0");
                }
                else
                {
                    int cnt = targetCodes.Count;
                    if (cnt == 0)
                    {
                        txtRecipientCount.Text = "⚠️ Chưa chọn học sinh nào";
                        txtRecipientCount.Foreground = (Brush)new BrushConverter().ConvertFromString("#C62828");
                    }
                    else
                    {
                        txtRecipientCount.Text = $"→ Gửi tới {cnt} học sinh được chọn";
                        txtRecipientCount.Foreground = (Brush)new BrushConverter().ConvertFromString("#2E7D32");
                    }
                }
            }
            catch { }
        }

        private List<string>? GetTargetStudentCodes()
        {
            if (rbAllStudents.IsChecked == true)
                return null; // All

            var codes = new List<string>();

            if (rbGroup.IsChecked == true)
            {
                // → ClassroomAppContext
                var groups = ClassroomAppContext.CurrentGroups;

                foreach (var cb in groupCheckList.Children.OfType<CheckBox>())
                {
                    if (cb.IsChecked == true)
                    {
                        if (groups != null && groups.Count > 0 && cb.Tag is int index)
                        {
                            var g = groups.FirstOrDefault(x => x.Index == index);
                            if (g != null && g.Members != null)
                            {
                                foreach (var code in g.Members)
                                {
                                    if (!codes.Contains(code)) codes.Add(code);
                                }
                            }
                        }
                        else if (cb.Tag is string className)
                        {
                            // Fallback ClassName
                            var students = _cachedStudents;
                            if (students != null)
                            {
                                foreach (var s in students)
                                {
                                    if (s.ClassName == className && !string.IsNullOrEmpty(s.StudentCode))
                                    {
                                        if (!codes.Contains(s.StudentCode)) codes.Add(s.StudentCode);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (rbIndividual.IsChecked == true)
            {
                foreach (var cb in studentCheckList.Children.OfType<CheckBox>())
                {
                    if (cb.IsChecked == true && cb.Tag is string code && !string.IsNullOrEmpty(code))
                        codes.Add(code);
                }
            }

            return codes;
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.S && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Alt)
            {
                e.Handled = true;
                txtSearchStudent?.Focus();
                return;
            }

            var focusedElement = System.Windows.Input.FocusManager.GetFocusedElement(this);
            bool isTyping = focusedElement is TextBox;

            if (!isTyping)
            {
                if (e.Key == System.Windows.Input.Key.A && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Alt)
                {
                    e.Handled = true;
                    if (rbIndividual.IsChecked == true) SelectAllStudents_Click(this, new RoutedEventArgs());
                    else if (rbGroup.IsChecked == true) SelectAllGroups_Click(this, new RoutedEventArgs());
                    return;
                }
                else if (e.Key == System.Windows.Input.Key.D && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Alt)
                {
                    e.Handled = true;
                    if (rbIndividual.IsChecked == true) DeselectAllStudents_Click(this, new RoutedEventArgs());
                    else if (rbGroup.IsChecked == true) DeselectAllGroups_Click(this, new RoutedEventArgs());
                    return;
                }
            }
        }
    }

    public class FileItem
    {
        public string Name     { get; set; } = string.Empty;
        public string Path     { get; set; } = string.Empty;
        public string SizeText { get; set; } = string.Empty;
        public string TypeIcon { get; set; } = "📁";
    }
}






