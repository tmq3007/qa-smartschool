using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Serilog;
using QASmartClass.StudentClient.Services;

namespace QASmartClass.StudentClient.Views
{
    public partial class StudentSubmitPage : Page
    {
        private string _activeTab = "received"; // received | submitted | editor
        private DispatcherTimer? _deadlineTimer;
        private DispatcherTimer? _autosaveTimer;
        private const string EDITOR_PLACEHOLDER = "Viết bài làm tại đây...";
        private bool _isPlaceholderActive = true;
        private bool _isSubmitting = false;
        private bool _isSyncing = false;

        public StudentSubmitPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            StartFileListener();
            LoadAssignmentInfo();
            LoadReceivedFiles();
            UpdateStats();
            LoadActivity();
            SetupEditorWordCount();
            SetupPlaceholder();
            StartAutosaveTimer();
            CheckAndRestoreDraft();
            UpdateSyncOfflineButtonVisibility();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            try { _ = ForceAutosaveAsync(); } catch { }
            _deadlineTimer?.Stop();
            _autosaveTimer?.Stop();

            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var sft = app.StudentFileTransfer;
                if (sft != null)
                {
                    sft.FileReceived -= OnFileFromTeacher;
                    sft.SubmitProgress -= OnSubmitProgress;
                }
            }
            catch { }
        }

        // ═══════════════════════════════════════════════════════════
        //  FILE LISTENER — Nhận file từ GV qua TCP
        // ═══════════════════════════════════════════════════════════

        private void StartFileListener()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var sft = app.StudentFileTransfer;

                if (sft != null)
                {
                    sft.FileReceived -= OnFileFromTeacher;
                    sft.FileReceived += OnFileFromTeacher;

                    sft.SubmitProgress -= OnSubmitProgress;
                    sft.SubmitProgress += OnSubmitProgress;

                    if (!sft.IsListening)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await sft.StartListeningAsync();
                                Dispatcher.Invoke(() =>
                                {
                                    txtStatus.Text = "📡 Sẵn sàng · Kéo thả file hoặc nhấn nút để nộp bài";
                                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117));
                                });
                            }
                            catch (System.Net.Sockets.SocketException)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    txtStatus.Text = $"❌ Lỗi: Cổng kết nối {StudentFileTransfer.FILE_PORT} bị chiếm dụng. Vui lòng tắt ứng dụng chạy ẩn.";
                                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
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
                        txtStatus.Text = "📡 Sẵn sàng · Kéo thả file hoặc nhấn nút để nộp bài";
                        txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117));
                    }
                }
            }
            catch (Exception ex) { Log.Warning("StartFileListener error: {Err}", ex.Message); }
        }

        private void OnFileFromTeacher(object? sender, Services.StudentFileEventArgs e)
        {
            // Save to DB on background thread to keep thread safety
            _ = Task.Run(async () =>
            {
                try
                {
                    using var db = new QASmartClass.Data.AppDbContext();
                    string studentCode = "Anonymous";
                    try
                    {
                        var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(db);
                        var (_, currentCode, _) = identityService.GetCurrentStudent();
                        if (!string.IsNullOrEmpty(currentCode))
                        {
                            studentCode = currentCode;
                        }
                    }
                    catch { }

                    db.EventLogs.Add(new Data.EventLog
                    {
                        EventType = "FILE_RECEIVED",
                        Actor = studentCode,
                        Details = $"Nhận file từ GV: {e.FileName} ({e.FileSize / 1024.0:F1} KB)",
                        Timestamp = DateTime.Now
                    });
                    await db.SaveChangesAsync();
                }
                catch (Exception exDb)
                {
                    Log.Warning("Save event log on receive failed: {Err}", exDb.Message);
                }
            });

            Dispatcher.Invoke(() =>
            {
                _ = ForceAutosaveAsync();
                txtStatus.Text = $"📥 Đã nhận file: {e.FileName}";
                progressBar.Value = 100;
                _activeTab = "received";
                LoadAssignmentInfo(); // Refresh assignment info when file is received
                LoadReceivedFiles();
                UpdateStats();
                LoadActivity();

                var win = new Window
                {
                    Title = "📥 Nhận file từ Giáo viên",
                    Width = 450,
                    Height = 260,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                    Topmost = true
                };

                var mainStack = new StackPanel { Margin = new Thickness(20) };

                // Tiêu đề
                mainStack.Children.Add(new TextBlock
                {
                    Text = "📥 Đã nhận tài liệu mới từ Giáo viên!",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                    Margin = new Thickness(0, 0, 0, 12)
                });

                // Thông tin file
                var fileInfoBorder = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 16)
                };

                var fileInfoStack = new StackPanel();
                fileInfoStack.Children.Add(new TextBlock
                {
                    Text = $"📄 Tên file: {e.FileName}",
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                    Margin = new Thickness(0, 0, 0, 4),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                fileInfoStack.Children.Add(new TextBlock
                {
                    Text = $"💾 Dung lượng: {e.FileSize / 1024.0:F1} KB",
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 0, 0, 4)
                });
                fileInfoStack.Children.Add(new TextBlock
                {
                    Text = $"📂 Thư mục: {System.IO.Path.GetDirectoryName(e.SavePath)}",
                    FontSize = 11,
                    Foreground = Brushes.Gray,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                fileInfoBorder.Child = fileInfoStack;
                mainStack.Children.Add(fileInfoBorder);

                // Nút bấm
                var btnDock = new DockPanel();

                var btnClose = new Button
                {
                    Content = "Đóng",
                    Width = 90,
                    Height = 32,
                    Background = new SolidColorBrush(Color.FromRgb(238, 238, 238)),
                    Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    FontWeight = FontWeights.SemiBold
                };
                var closeTemplate = new ControlTemplate(typeof(Button));
                var closeBorder = new FrameworkElementFactory(typeof(Border));
                closeBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                closeBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
                var closePresenter = new FrameworkElementFactory(typeof(ContentPresenter));
                closePresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                closePresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                closeBorder.AppendChild(closePresenter);
                closeTemplate.VisualTree = closeBorder;
                btnClose.Template = closeTemplate;
                btnClose.Click += (s, ev) => win.Close();
                DockPanel.SetDock(btnClose, Dock.Right);
                btnDock.Children.Add(btnClose);

                var btnOpenFolder = new Button
                {
                    Content = "📁 Mở thư mục",
                    Width = 140,
                    Height = 32,
                    Background = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                var openTemplate = new ControlTemplate(typeof(Button));
                var openBorder = new FrameworkElementFactory(typeof(Border));
                openBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                openBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
                var openPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
                openPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                openPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                openBorder.AppendChild(openPresenter);
                openTemplate.VisualTree = openBorder;
                btnOpenFolder.Template = openTemplate;

                btnOpenFolder.Click += (s, ev) =>
                {
                    var savePath = e.SavePath;
                    Task.Run(() =>
                    {
                        try
                        {
                            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{savePath}\"");
                        }
                        catch (Exception ex)
                        {
                            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show($"Không thể mở thư mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                        }
                    });
                    try
                    {
                        win.Close();
                    }
                    catch { }
                };
                DockPanel.SetDock(btnOpenFolder, Dock.Left);
                btnDock.Children.Add(btnOpenFolder);

                mainStack.Children.Add(btnDock);
                win.Content = mainStack;

                try
                {
                    var parentWin = Window.GetWindow(this);
                    if (parentWin != null)
                    {
                        win.Owner = parentWin;
                        win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    }
                }
                catch { }

                win.Show();
            });
        }

        private void OnSubmitProgress(object? sender, int percent)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.Value = percent;
                txtStatus.Text = $"📤 Đang nộp... {percent}%";
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  ASSIGNMENT INFO — Hiển thị yêu cầu + deadline từ GV
        // ═══════════════════════════════════════════════════════════

        private void LoadAssignmentInfo()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;

                if (!string.IsNullOrEmpty(QASmartTouch.App.AssessmentState.AssignmentDescription)
                    && QASmartTouch.App.AssessmentState.AssignmentSentTime > DateTime.MinValue)
                {
                    assignmentPanel.Visibility = Visibility.Visible;
                    txtAssignment.Text = QASmartTouch.App.AssessmentState.AssignmentDescription;

                    if (QASmartTouch.App.AssessmentState.AssignmentDeadline.HasValue)
                    {
                        txtDeadline.Text = $"⏰ Hạn: {QASmartTouch.App.AssessmentState.AssignmentDeadline:HH:mm dd/MM}";
                        StartDeadlineCountdown(QASmartTouch.App.AssessmentState.AssignmentDeadline.Value);
                    }
                    else
                    {
                        txtDeadline.Text = "Không có hạn nộp";
                        txtCountdown.Text = "";
                    }
                }
                else
                {
                    assignmentPanel.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex) { Log.Warning("LoadAssignmentInfo error: {Err}", ex.Message); }
        }

        private void StartDeadlineCountdown(DateTime deadline)
        {
            _deadlineTimer?.Stop();
            
            if (deadline.Year >= 2090)
            {
                var greenBrush = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                txtDeadline.Text = "Hạn nộp: Không giới hạn";
                txtDeadline.Foreground = greenBrush;
                txtCountdown.Text = "☘️ Bài tập ôn tập tự do";
                txtCountdown.Foreground = greenBrush;
                return;
            }

            _deadlineTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            
            Action updateAction = () =>
            {
                var remaining = deadline - DateTime.Now;
                if (remaining.TotalSeconds <= 0)
                {
                    var redBrush = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                    txtCountdown.Text = "⚠️ HẾT HẠN NỘP BÀI!";
                    txtCountdown.Foreground = redBrush;
                    txtDeadline.Foreground = redBrush;
                    _deadlineTimer?.Stop();
                }
                else if (remaining.TotalHours < 2) // Under 2 hours -> Red alert
                {
                    var redBrush = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                    txtCountdown.Text = remaining.TotalMinutes < 60
                        ? $"⏱️ Còn {remaining.Minutes} phút {remaining.Seconds} giây"
                        : $"⏱️ Còn {(int)remaining.TotalHours} giờ {remaining.Minutes} phút";
                    txtCountdown.Foreground = redBrush;
                    txtDeadline.Foreground = redBrush;
                }
                else if (remaining.TotalHours < 24) // Under 24 hours -> Orange warning
                {
                    var orangeBrush = new SolidColorBrush(Color.FromRgb(230, 81, 0));
                    txtCountdown.Text = $"⏱️ Còn {(int)remaining.TotalHours} giờ {remaining.Minutes} phút";
                    txtCountdown.Foreground = orangeBrush;
                    txtDeadline.Foreground = orangeBrush;
                }
                else // Over 24 hours -> Green safe
                {
                    var greenBrush = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    txtCountdown.Text = $"⏱️ Còn {(int)remaining.TotalDays} ngày {remaining.Hours} giờ";
                    txtCountdown.Foreground = greenBrush;
                    txtDeadline.Foreground = greenBrush;
                }
            };
            
            _deadlineTimer.Tick += (s, ev) => updateAction();
            _deadlineTimer.Start();
            updateAction(); // Trigger immediately
        }

        // ═══════════════════════════════════════════════════════════
        //  DRAG & DROP — Kéo thả file để nộp bài
        // ═══════════════════════════════════════════════════════════

        private void Page_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                dropOverlay.Visibility = Visibility.Visible;
                e.Effects = DragDropEffects.Copy;
            }
            e.Handled = true;
        }

        private void Page_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void Page_DragLeave(object sender, DragEventArgs e)
        {
            dropOverlay.Visibility = Visibility.Collapsed;
        }

        private async void Page_Drop(object sender, DragEventArgs e)
        {
            dropOverlay.Visibility = Visibility.Collapsed;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    await SubmitFilesAsync(files);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  LOAD FILES
        // ═══════════════════════════════════════════════════════════

        private void LoadReceivedFiles()
        {
            fileListPanel.Children.Clear();
            editorPanel.Visibility = _activeTab == "editor" ? Visibility.Visible : Visibility.Collapsed;

            if (_activeTab == "editor") { UpdateTabStyles(); return; }

            if (_activeTab == "received")
            {
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    var folder = app.StudentFileTransfer.ReceiveFolder;
                    
                    // Thêm thông báo chờ nhẹ nhàng
                    AddEmptyMessage("⏳ Đang tải danh sách tài liệu...");

                    Task.Run(() =>
                    {
                        try
                        {
                            if (Directory.Exists(folder))
                            {
                                var filesData = Directory.GetFiles(folder)
                                    .Select(f =>
                                    {
                                        try
                                        {
                                            var fi = new FileInfo(f);
                                            return new { Path = f, Name = fi.Name, Length = fi.Length, LastWriteTime = fi.LastWriteTime };
                                        }
                                        catch
                                        {
                                            return null;
                                        }
                                    })
                                    .Where(f => f != null)
                                    .OrderByDescending(f => f.LastWriteTime)
                                    .ToList();

                                Dispatcher.Invoke(() =>
                                {
                                    fileListPanel.Children.Clear();
                                    if (filesData.Count == 0)
                                    {
                                        AddEmptyMessage("📥 Chưa nhận tài liệu nào từ GV.\n\nKhi GV phát bài, file sẽ tự động hiện ở đây.");
                                    }
                                    else
                                    {
                                        foreach (var fd in filesData)
                                        {
                                            AddFileRowFromPath(fd.Path, fd.Name, fd.Length, fd.LastWriteTime);
                                        }
                                    }
                                });
                            }
                            else
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    fileListPanel.Children.Clear();
                                    AddEmptyMessage("📥 Thư mục nhận file chưa tồn tại.");
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                fileListPanel.Children.Clear();
                                AddEmptyMessage($"❌ Lỗi: {ex.Message}");
                            });
                        }
                    });
                }
                catch (Exception exOuter)
                {
                    AddEmptyMessage($"❌ Lỗi: {exOuter.Message}");
                }
            }
            else // submitted
            {
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    using var db = new QASmartClass.Data.AppDbContext();
                    var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(db);
                    var (currentId, _, _) = identityService.GetCurrentStudent();

                    var records = db.FileTransfers
                        .Where(r => r.StudentId == currentId && r.Direction == "StudentToTeacher")
                        .OrderByDescending(r => r.CreatedAt).ToList();

                    if (records.Count == 0)
                        AddEmptyMessage("📤 Chưa nộp bài nào.\n\nNhấn \"Nộp file\", \"Viết bài\", hoặc kéo thả file vào đây.");
                    else
                        foreach (var r in records)
                        {
                            var icon = r.Status == "Completed" ? "✅" : "⏳";
                            var bg = r.Status == "Completed" ? "#E8F5E9" : "#FFF3E0";
                            var fg = r.Status == "Completed" ? "#2E7D32" : "#E65100";
                            AddSubmittedRow(r.FileName, FormatSize(r.FileSizeBytes),
                                $"{icon} {(r.Status == "Completed" ? "Đã nộp" : "Chờ")} · {r.CreatedAt:HH:mm dd/MM}", bg, fg);
                        }
                }
                catch (Exception ex) { AddEmptyMessage($"❌ Lỗi: {ex.Message}"); }
            }
            UpdateTabStyles();
        }

        // ═══════════════════════════════════════════════════════════
        //  BUILD UI ROWS
        // ═══════════════════════════════════════════════════════════

        private void AddFileRowFromPath(string filePath, string fileName, long fileLength, DateTime lastWriteTime)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(250, 251, 252)),
                CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 4),
                BorderBrush = new SolidColorBrush(Color.FromRgb(232, 232, 232)), BorderThickness = new Thickness(1)
            };
            border.MouseEnter += (_, _) => border.Background = new SolidColorBrush(Color.FromRgb(232, 245, 255));
            border.MouseLeave += (_, _) => border.Background = new SolidColorBrush(Color.FromRgb(250, 251, 252));

            var dock = new DockPanel();

            // Open button
            var openBtn = new Button
            {
                Content = "📂 Mở", Padding = new Thickness(8, 4, 8, 4), FontSize = 10,
                Background = new SolidColorBrush(Color.FromRgb(227, 242, 253)),
                Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand
            };
            var path = filePath;
            openBtn.Click += (_, _) =>
            {
                Task.Run(() =>
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true }); }
                    catch (Exception ex)
                    {
                        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"Không mở được: {ex.Message}");
                        });
                    }
                });
            };
            DockPanel.SetDock(openBtn, Dock.Right);
            dock.Children.Add(openBtn);

            var iconBlock = new TextBlock
            {
                Text = GetIcon(System.IO.Path.GetExtension(filePath)), FontSize = 16, FontFamily = new FontFamily("Segoe UI Emoji"),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0)
            };
            DockPanel.SetDock(iconBlock, Dock.Left);
            dock.Children.Add(iconBlock);

            var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            info.Children.Add(new TextBlock
            {
                Text = fileName, FontSize = 13, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            info.Children.Add(new TextBlock
            {
                Text = $"{FormatSize(fileLength)}  •  Nhận lúc {lastWriteTime:HH:mm dd/MM}",
                FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158))
            });
            dock.Children.Add(info);

            border.Child = dock;
            fileListPanel.Children.Add(border);
        }

        private void AddSubmittedRow(string name, string size, string info, string bgHex, string fgHex)
        {
            var bg = (Color)ColorConverter.ConvertFromString(bgHex);
            var fg = (Color)ColorConverter.ConvertFromString(fgHex);
            var border = new Border
            {
                Background = new SolidColorBrush(bg), CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(0, 0, 0, 4)
            };
            var dock = new DockPanel();
            var icon = new TextBlock
            {
                Text = info.Contains("✅") ? "✅" : "⏳", FontSize = 14,
                FontFamily = new FontFamily("Segoe UI Emoji"),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0)
            };
            DockPanel.SetDock(icon, Dock.Left);
            dock.Children.Add(icon);

            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            infoStack.Children.Add(new TextBlock
            {
                Text = name, FontSize = 13, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(fg), TextTrimming = TextTrimming.CharacterEllipsis
            });
            infoStack.Children.Add(new TextBlock
            {
                Text = $"{size}  •  {info}", FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromArgb(180, fg.R, fg.G, fg.B))
            });
            dock.Children.Add(infoStack);
            border.Child = dock;
            fileListPanel.Children.Add(border);
        }

        private void AddEmptyMessage(string message) => fileListPanel.Children.Add(new TextBlock
        {
            Text = message, FontSize = 13, Foreground = Brushes.Gray, FontStyle = FontStyles.Italic,
            TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center,
            Margin = new Thickness(16, 30, 16, 30)
        });

        // ═══════════════════════════════════════════════════════════
        //  TABS
        // ═══════════════════════════════════════════════════════════

        private void TabReceived_Click(object s, RoutedEventArgs e)
        {
            _ = ForceAutosaveAsync();
            _activeTab = "received";
            LoadReceivedFiles();
        }
        private void TabSubmitted_Click(object s, RoutedEventArgs e)
        {
            _ = ForceAutosaveAsync();
            _activeTab = "submitted";
            LoadReceivedFiles();
        }
        private void TabEditor_Click(object s, RoutedEventArgs e)
        {
            _ = ForceAutosaveAsync();
            _activeTab = "editor";
            LoadReceivedFiles();
        }

        private void UpdateTabStyles()
        {
            SetTabStyle(tabReceived, _activeTab == "received", "#E3F2FD", "#1565C0");
            SetTabStyle(tabSubmitted, _activeTab == "submitted", "#E8F5E9", "#2E7D32");
            SetTabStyle(tabEditor, _activeTab == "editor", "#F3E5F5", "#7B1FA2");
        }

        private void SetTabStyle(Button btn, bool active, string activeBg, string activeFg)
        {
            btn.Background = active
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(activeBg))
                : new SolidColorBrush(Color.FromRgb(245, 245, 245));
            btn.Foreground = active
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(activeFg))
                : new SolidColorBrush(Color.FromRgb(102, 102, 102));

            btn.FontWeight = active ? FontWeights.Bold : FontWeights.Normal;
            btn.BorderBrush = active
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(activeFg))
                : Brushes.Transparent;
        }

        // ═══════════════════════════════════════════════════════════
        //  INLINE EDITOR
        // ═══════════════════════════════════════════════════════════

        private void WriteInline_Click(object sender, RoutedEventArgs e)
        {
            _activeTab = "editor";
            LoadReceivedFiles();
        }

        private void SetupEditorWordCount()
        {
            txtEditorContent.TextChanged += (_, _) =>
            {
                if (_isPlaceholderActive) return;
                UpdateWordProgress();
            };
        }

        private void WordTarget_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isPlaceholderActive) return;
            UpdateWordProgress();
        }

        private int GetSelectedWordTarget()
        {
            if (cboWordTarget == null || cboWordTarget.SelectedIndex <= 0) return 0;
            var item = cboWordTarget.SelectedItem as ComboBoxItem;
            if (item == null) return 0;
            var content = item.Content.ToString(); // e.g. "50 từ"
            var parts = content.Split(' ');
            if (parts.Length > 0 && int.TryParse(parts[0], out var val))
                return val;
            return 0;
        }

        private void UpdateWordProgress()
        {
            var text = txtEditorContent.Text ?? "";
            var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            txtWordCount.Text = $"{words} từ · {text.Length} ký tự";

            int targetWords = GetSelectedWordTarget();
            if (targetWords <= 0)
            {
                pbWordProgress.Visibility = Visibility.Collapsed;
                txtTargetPercent.Visibility = Visibility.Collapsed;
            }
            else
            {
                pbWordProgress.Visibility = Visibility.Visible;
                txtTargetPercent.Visibility = Visibility.Visible;
                int percent = (words * 100) / targetWords;
                pbWordProgress.Value = Math.Min(100, percent);
                if (percent >= 100)
                {
                    pbWordProgress.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                    txtTargetPercent.Text = "🎉 Đạt mục tiêu!";
                    txtTargetPercent.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                }
                else
                {
                    pbWordProgress.Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Blue
                    txtTargetPercent.Text = $"{percent}%";
                    txtTargetPercent.Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192));
                }
            }
        }

        private void SetupPlaceholder()
        {
            txtEditorContent.Text = EDITOR_PLACEHOLDER;
            txtEditorContent.Foreground = Brushes.Gray;
            _isPlaceholderActive = true;
            txtWordCount.Text = "0 từ · 0 ký tự";
            if (txtAutosaveStatus != null) txtAutosaveStatus.Text = "";
            if (cboWordTarget != null) cboWordTarget.SelectedIndex = 0;
            if (pbWordProgress != null)
            {
                pbWordProgress.Visibility = Visibility.Collapsed;
                pbWordProgress.Value = 0;
            }
            if (txtTargetPercent != null)
            {
                txtTargetPercent.Visibility = Visibility.Collapsed;
                txtTargetPercent.Text = "";
            }
        }

        private string GetDraftFilePath()
        {
            string studentCode = "Anonymous";
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.Database != null)
                {
                    var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(app.Database);
                    var (_, currentCode, _) = identityService.GetCurrentStudent();
                    if (!string.IsNullOrEmpty(currentCode))
                    {
                        studentCode = currentCode;
                    }
                }
                else if (app?.StudentNetwork != null)
                {
                    string currentCode = app.StudentNetwork.StudentCode;
                    if (!string.IsNullOrEmpty(currentCode))
                    {
                        studentCode = currentCode;
                    }
                }
            }
            catch { }
            return Path.Combine(QASmartClass.Services.AppPaths.DocumentsDir, $"autosave_draft_{studentCode}.tmp");
        }

        private bool _isSavingDraft = false;
        private async System.Threading.Tasks.Task ForceAutosaveAsync()
        {
            if (_activeTab == "editor" && !_isPlaceholderActive && !string.IsNullOrWhiteSpace(txtEditorContent.Text))
            {
                if (_isSavingDraft) return;
                _isSavingDraft = true;
                try
                {
                    var title = txtEditorTitle.Text;
                    var content = txtEditorContent.Text;
                    var draftPath = GetDraftFilePath();
                    Directory.CreateDirectory(Path.GetDirectoryName(draftPath)!);
                    
                    var draftData = $"{title}\n---SPLIT_DRAFT---\n{content}";
                    var bytes = System.Text.Encoding.UTF8.GetBytes(draftData);
                    var base64Data = Convert.ToBase64String(bytes);
                    await File.WriteAllTextAsync(draftPath, base64Data);
                    Log.Information("Đã tự động lưu nháp bài làm cưỡng bức.");
                    Dispatcher.Invoke(() =>
                    {
                        if (txtAutosaveStatus != null)
                        {
                            txtAutosaveStatus.Text = $"✓ Đã lưu nháp lúc {DateTime.Now:HH:mm:ss}";
                        }
                    });
                }
                catch (Exception ex) { Log.Warning("Force auto-save failed: {Err}", ex.Message); }
                finally
                {
                    _isSavingDraft = false;
                }
            }
        }

        private void StartAutosaveTimer()
        {
            _autosaveTimer?.Stop();
            _autosaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
            _autosaveTimer.Tick += async (s, e) =>
            {
                await ForceAutosaveAsync();
            };
            _autosaveTimer.Start();
        }

        private void CheckAndRestoreDraft()
        {
            try
            {
                var draftPath = GetDraftFilePath();
                if (File.Exists(draftPath))
                {
                    var fileContent = File.ReadAllText(draftPath).Trim();
                    if (!string.IsNullOrWhiteSpace(fileContent))
                    {
                        string decodedContent;
                        try
                        {
                            var base64Bytes = Convert.FromBase64String(fileContent);
                            decodedContent = System.Text.Encoding.UTF8.GetString(base64Bytes);
                        }
                        catch (FormatException)
                        {
                            decodedContent = fileContent;
                        }

                        var parts = decodedContent.Split(new[] { "\n---SPLIT_DRAFT---\n" }, StringSplitOptions.None);
                        if (parts.Length == 2)
                        {
                            var result = MessageBox.Show(
                                "Phần mềm phát hiện một bản thảo soạn bài chưa được gửi trước đó.\nBạn có muốn khôi phục lại không?",
                                "Khôi phục bài làm", MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                txtEditorTitle.Text = parts[0];
                                txtEditorContent.Text = parts[1];
                                txtEditorContent.Foreground = Brushes.Black;
                                _isPlaceholderActive = false;
                                
                                // Chuyển tab sang editor để học sinh thấy ngay
                                _activeTab = "editor";
                                LoadReceivedFiles();
                            }
                            else
                            {
                                File.Delete(draftPath); // Học sinh không khôi phục thì xóa file nháp đi
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Log.Warning("Restore draft failed: {Err}", ex.Message); }
        }

        private void DeleteDraftFile()
        {
            try
            {
                var draftPath = GetDraftFilePath();
                if (File.Exists(draftPath))
                {
                    File.Delete(draftPath);
                    Log.Information("Đã xóa file bản thảo nháp sau khi nộp thành công.");
                }
            }
            catch { }
        }

        private void EditorContent_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_isPlaceholderActive)
            {
                txtEditorContent.Text = "";
                txtEditorContent.Foreground = Brushes.Black;
                _isPlaceholderActive = false;
            }
        }

        private void EditorContent_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtEditorContent.Text))
            {
                SetupPlaceholder();
            }
        }

        private async void SubmitInline_Click(object sender, RoutedEventArgs e)
        {
            var content = txtEditorContent.Text?.Trim() ?? "";
            var titleInput = txtEditorTitle.Text?.Trim() ?? "BaiLam";

            if (string.IsNullOrWhiteSpace(content) || _isPlaceholderActive)
            {
                MessageBox.Show("Vui lòng nhập nội dung bài làm!", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int targetWords = GetSelectedWordTarget();
            if (targetWords > 0)
            {
                var words = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                if (words < targetWords)
                {
                    var res = MessageBox.Show(
                        $"Bài làm của em mới đạt {words}/{targetWords} từ, chưa hoàn thành mục tiêu yêu cầu.\nEm có chắc chắn muốn nộp bài luôn không?",
                        "Xác nhận nộp bài",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    if (res == MessageBoxResult.No)
                        return; // Cancel submission
                }
            }

            // Sanitize title to prevent Path Traversal or illegal characters in filename
            var invalidChars = Path.GetInvalidFileNameChars();
            var title = string.Join("_", titleInput.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).Trim();
            if (string.IsNullOrEmpty(title)) title = "BaiLam";

            // Create temp .txt file
            var tempFolder = Path.Combine(Path.GetTempPath(), "QASmartClass_Submit");
            Directory.CreateDirectory(tempFolder);
            var fileName = $"{title}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var filePath = Path.Combine(tempFolder, fileName);
            await File.WriteAllTextAsync(filePath, content);

            // Submit
            await SubmitFilesAsync(new[] { filePath });

            // Xóa file tạm thời sau khi đã nộp hoặc sao lưu ngoại tuyến thành công
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Log.Information("Đã dọn dẹp file tạm thời sau khi nộp: {Path}", filePath);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Không thể dọn dẹp file tạm thời: {Err}", ex.Message);
            }

            // Clear editor draft file
            DeleteDraftFile();

            // Clear editor
            txtEditorContent.Text = "";
            SetupPlaceholder();
            _activeTab = "submitted";
            LoadReceivedFiles();
        }

        // ═══════════════════════════════════════════════════════════
        //  SUBMIT FILES — Core submit logic
        // ═══════════════════════════════════════════════════════════

        private async void SubmitFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Chọn file bài làm để nộp",
                Filter = "Tất cả file|*.*|Word|*.docx;*.doc|PDF|*.pdf|Excel|*.xlsx|Ảnh|*.jpg;*.png",
                Multiselect = true
            };
            if (dlg.ShowDialog() == true)
                await SubmitFilesAsync(dlg.FileNames);
        }

        private async System.Threading.Tasks.Task<string> CompressImageIfApplicableAsync(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".bmp")
            {
                return filePath;
            }

            try
            {
                return await System.Threading.Tasks.Task.Run(() =>
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), "qas_submit_" + Guid.NewGuid().ToString("N") + ".jpg");
                    using (var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var sourceImg = System.Drawing.Image.FromStream(inputStream))
                    {
                        int targetWidth = sourceImg.Width;
                        int targetHeight = sourceImg.Height;
                        if (targetWidth > 1920)
                        {
                            targetHeight = (int)(targetHeight * (1920.0 / targetWidth));
                            targetWidth = 1920;
                        }

                        // Dynamic Quality Scaling
                        int quality = 80;
                        bool done = false;
                        byte[]? finalBytes = null;

                        while (quality >= 50 && !done)
                        {
                            using (var resizedImg = new System.Drawing.Bitmap(targetWidth, targetHeight))
                            using (var g = System.Drawing.Graphics.FromImage(resizedImg))
                            {
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                                g.DrawImage(sourceImg, 0, 0, targetWidth, targetHeight);

                                using (var ms = new MemoryStream())
                                {
                                    var codecs = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
                                    System.Drawing.Imaging.ImageCodecInfo? encoder = null;
                                    foreach (var codec in codecs)
                                    {
                                        if (codec.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid)
                                        {
                                            encoder = codec;
                                            break;
                                        }
                                    }

                                    if (encoder != null)
                                    {
                                        var encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
                                        encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);
                                        resizedImg.Save(ms, encoder, encoderParams);
                                    }
                                    else
                                    {
                                        resizedImg.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                                    }

                                    finalBytes = ms.ToArray();
                                    if (finalBytes.Length <= 800 * 1024 || quality <= 50)
                                    {
                                        done = true;
                                    }
                                    else
                                    {
                                        quality -= 10;
                                    }
                                }
                            }
                        }

                        // If quality is 50 but size is still > 800KB, scale down width
                        if (finalBytes != null && finalBytes.Length > 800 * 1024)
                        {
                            targetWidth = (int)(targetWidth * 0.75);
                            targetHeight = (int)(targetHeight * 0.75);
                            using (var resizedImg = new System.Drawing.Bitmap(targetWidth, targetHeight))
                            using (var g = System.Drawing.Graphics.FromImage(resizedImg))
                            {
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                                g.DrawImage(sourceImg, 0, 0, targetWidth, targetHeight);
                                using (var ms = new MemoryStream())
                                {
                                    var codecs = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
                                    System.Drawing.Imaging.ImageCodecInfo? encoder = null;
                                    foreach (var codec in codecs)
                                    {
                                        if (codec.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid)
                                        {
                                            encoder = codec;
                                            break;
                                        }
                                    }

                                    if (encoder != null)
                                    {
                                        var encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
                                        encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 65L);
                                        resizedImg.Save(ms, encoder, encoderParams);
                                    }
                                    else
                                    {
                                        resizedImg.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                                    }
                                    finalBytes = ms.ToArray();
                                }
                            }
                        }

                        if (finalBytes != null)
                        {
                            File.WriteAllBytes(tempPath, finalBytes);
                            Log.Information("[Compression] Image compressed from {OrigSize} to {NewSize} bytes. Path: {Path}", 
                                new FileInfo(filePath).Length, finalBytes.Length, tempPath);
                            return tempPath;
                        }
                    }
                    return filePath;
                });
            }
            catch (Exception ex)
            {
                Log.Warning("[Compression] Failed to compress image {Path}: {Err}", filePath, ex.Message);
                return filePath;
            }
        }

        private async System.Threading.Tasks.Task SubmitFilesAsync(string[] filePaths)
        {
            if (_isSubmitting) return;
            _isSubmitting = true;
            var originalCursor = this.Cursor;
            this.Cursor = System.Windows.Input.Cursors.Wait;

            try
            {
                _ = ForceAutosaveAsync();
                var app = (QASmartTouch.App)Application.Current;
            var client = app.StudentNetwork;
            var sft = app.StudentFileTransfer;

            string studentCode = "HS00001";
            string studentName = "Học sinh";
            int studentId = 0;

            try
            {
                using var db = new QASmartClass.Data.AppDbContext();
                var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(db);
                var (currentId, currentCode, currentName) = identityService.GetCurrentStudent();
                studentCode = currentCode;
                studentName = currentName;
                studentId = currentId;
            }
            catch (Exception ex)
            {
                Log.Warning("Resolve student identity via local DB failed: {Err}", ex.Message);
                if (app.StudentNetwork != null)
                {
                    studentCode = app.StudentNetwork.StudentCode ?? studentCode;
                    studentName = app.StudentNetwork.StudentName ?? studentName;
                }
            }

            var validFiles = new List<string>();
            var zeroByteFiles = new List<string>();
            foreach (var fp in filePaths)
            {
                if (!File.Exists(fp)) continue;
                var fi = new FileInfo(fp);
                if (fi.Length == 0)
                    zeroByteFiles.Add(Path.GetFileName(fp));
                else
                    validFiles.Add(fp);
            }

            if (zeroByteFiles.Count > 0)
            {
                var fileListStr = string.Join("\n• ", zeroByteFiles);
                MessageBox.Show($"Các file sau đây bị trống (0 byte) và không thể nộp bài:\n• {fileListStr}\n\nVui lòng kiểm tra lại!", 
                    "Cảnh báo tệp tin trống", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (validFiles.Count == 0) return;

            int successCount = 0, totalFiles = validFiles.Count;
            string errorReason = "Nộp bài thất bại.";

            var filesToClean = new List<string>();
            try
            {
                foreach (var originalPath in validFiles)
                {
                    string filePath = originalPath;
                    var ext = Path.GetExtension(originalPath).ToLower();
                    bool isImage = ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp";

                    if (isImage)
                    {
                        Dispatcher.Invoke(() => txtStatus.Text = $"⚙️ Đang nén ảnh: {Path.GetFileName(originalPath)}...");
                        filePath = await CompressImageIfApplicableAsync(originalPath);
                        if (filePath != originalPath)
                        {
                            filesToClean.Add(filePath);
                        }
                    }

                    var fi = new FileInfo(filePath);
                    var fileSize = fi.Length;

                    // Enforce strict 1MB size limit
                    if (fileSize > 1024 * 1024)
                    {
                        MessageBox.Show($"File \"{Path.GetFileName(originalPath)}\" vượt quá giới hạn 1MB (kích thước sau nén: {fileSize / 1024.0:F1} KB).\n\nKhông thể nộp file này!", 
                            "Chặn file quá dung lượng", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }

                    var fileName = Path.GetFileName(filePath);
                    bool success = false;
                    progressBar.Value = 0;
                    txtStatus.Text = $"📤 Đang nộp: {fileName}...";

                    // Try TCP
                    if (client.IsConnected)
                    {
                        try
                        {
                            var teacherIP = client.ConnectedIP;
                            if (!string.IsNullOrEmpty(teacherIP))
                            {
                                var submitRes = await sft.SubmitFileAsync(filePath, teacherIP, studentCode, studentName);
                                success = submitRes.Success;
                                errorReason = submitRes.Message;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("TCP submit failed: {Err}", ex.Message);
                            errorReason = $"Không thể kết nối tới Giáo viên: {ex.Message}";
                        }
                    }

                    // Local copy fallback only if teaching server is on the same machine (localhost testing)
                    bool isLocalhost = string.IsNullOrEmpty(client.ConnectedIP) || 
                                       client.ConnectedIP == "127.0.0.1" || 
                                       client.ConnectedIP == "localhost";
                    if (!success && isLocalhost)
                    {
                        try
                        {
                            var submitFolder = Path.Combine(
                                QASmartClass.Services.AppPaths.DocumentsDir, "Submissions");
                            Directory.CreateDirectory(submitFolder);
                            var normalizedName = Classroom.Services.FileTransferService
                                .NormalizeFilename(studentCode, studentName, fileName);
                            File.Copy(filePath, Path.Combine(submitFolder, normalizedName), true);
                            success = true;
                            errorReason = "Nộp bài thành công cục bộ (Chế độ chạy cùng máy).";
                        }
                        catch (Exception ex) { Log.Warning("Local copy failed: {Err}", ex.Message); }
                    }

                    if (!success)
                    {
                        try
                        {
                            var pendingFolder = GetPendingSyncFolder();
                            Directory.CreateDirectory(pendingFolder);
                            var uniqueFileName = $"{studentCode}_{DateTime.Now:yyyyMMddHHmmss}_{fileName}";
                            var targetPath = Path.Combine(pendingFolder, uniqueFileName);
                            File.Copy(filePath, targetPath, true);
                            Log.Information("Đã lưu bài làm ngoại tuyến để đồng bộ sau: {Path}", targetPath);
                        }
                        catch (Exception exSync)
                        {
                            Log.Warning("Không thể lưu file vào hàng đợi ngoại tuyến: {Err}", exSync.Message);
                        }
                    }

                    if (success) successCount++;
                    if (totalFiles > 0)
                    {
                        progressBar.Value = (successCount * 100.0) / totalFiles;
                    }

                    // DB record
                    try
                    {
                        using var db = new QASmartClass.Data.AppDbContext();
                        db.FileTransfers.Add(new Data.FileTransferRecord
                        {
                            FileName = fileName, FileSizeBytes = fileSize,
                            Direction = "StudentToTeacher",
                            StudentId = studentId,
                            Status = success ? "Completed" : "Pending",
                            ProgressPercent = success ? 100 : 0, CreatedAt = DateTime.Now
                        });
                        db.EventLogs.Add(new Data.EventLog
                        {
                            EventType = "FILE", Actor = studentCode,
                            Details = $"Nộp bài: {fileName} ({FormatSize(fileSize)}) — {(success ? "OK" : "Thất bại: " + errorReason)}",
                            Timestamp = DateTime.Now
                        });
                        await db.SaveChangesAsync();

                        try
                        {
                            ((StudentShell)Application.Current.MainWindow)?.UpdateDynamicBadges();
                        }
                        catch (Exception exBadge)
                        {
                            Log.Warning("Failed to update badges after assignment submit: {Err}", exBadge.Message);
                        }
                    }
                    catch (Exception exDb)
                    {
                        Log.Warning("Save submission log failed: {Err}", exDb.Message);
                    }
                }
            }
            finally
            {
                foreach (var tempF in filesToClean)
                {
                    try
                    {
                        if (File.Exists(tempF)) File.Delete(tempF);
                    }
                    catch { }
                }
            }

            progressBar.Value = 100;
            var msg = successCount == totalFiles
                ? $"✅ Nộp thành công {totalFiles} file!" 
                : $"⚠️ Gửi bài thất bại: {errorReason}";
            txtStatus.Text = msg;
            
            UpdateSyncOfflineButtonVisibility();

            MessageBox.Show(msg, "Nộp bài", MessageBoxButton.OK,
                successCount == totalFiles ? MessageBoxImage.Information : MessageBoxImage.Warning);

            _activeTab = "submitted";
            LoadReceivedFiles();
            UpdateStats();
            LoadActivity();
            }
            finally
            {
                this.Cursor = originalCursor;
                _isSubmitting = false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  STATS + ACTIVITY
        // ═══════════════════════════════════════════════════════════

        private void UpdateStats()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var folder = app.StudentFileTransfer.ReceiveFolder;

                Task.Run(async () =>
                {
                    await QASmartClass.Data.AppDbContext.BackgroundDbSemaphore.WaitAsync();
                    try
                    {
                        int received = Directory.Exists(folder) ? Directory.GetFiles(folder).Length : 0;

                        using (var db = new QASmartClass.Data.AppDbContext())
                        {
                            var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(db);
                            var (currentId, _, _) = identityService.GetCurrentStudent();

                            int sub = db.FileTransfers.Count(r => r.StudentId == currentId && r.Direction == "StudentToTeacher" && r.Status == "Completed");
                            int pen = db.FileTransfers.Count(r => r.StudentId == currentId && r.Direction == "StudentToTeacher" && r.Status == "Pending");

                            Dispatcher.Invoke(() =>
                            {
                                if (txtReceivedCount != null) txtReceivedCount.Text = received.ToString();
                                if (txtSubmittedCount != null) txtSubmittedCount.Text = sub.ToString();
                                if (txtPendingCount != null) txtPendingCount.Text = pen.ToString();
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Failed to update stats asynchronously: {Err}", ex.Message);
                    }
                    finally
                    {
                        QASmartClass.Data.AppDbContext.BackgroundDbSemaphore.Release();
                    }
                });
            }
            catch (Exception exOuter)
            {
                Log.Warning("UpdateStats outer error: {Err}", exOuter.Message);
            }
        }

        private void LoadActivity()
        {
            try
            {
                activityList.Children.Clear();
                using var db = new QASmartClass.Data.AppDbContext();
                var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(db);
                var (currentId, currentCode, _) = identityService.GetCurrentStudent();

                var logs = db.EventLogs
                    .Where(l => l.Actor == currentCode && (l.EventType == "FILE" || l.EventType == "FILE_RECEIVED"))
                    .OrderByDescending(l => l.Timestamp).Take(10).ToList();

                if (logs.Count == 0)
                {
                    activityList.Children.Add(new TextBlock
                    {
                        Text = "Chưa có hoạt động nào", FontSize = 12, Foreground = Brushes.Gray,
                        FontStyle = FontStyles.Italic, Margin = new Thickness(0, 10, 0, 0)
                    });
                    return;
                }

                foreach (var log in logs)
                {
                    var icon = log.EventType == "FILE_RECEIVED" ? "📥" : "📤";
                    var row = new TextBlock
                    {
                        Text = $"{icon} {log.Timestamp:HH:mm} — {log.Details}",
                        FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                        TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4)
                    };
                    activityList.Children.Add(row);
                }
            }
            catch { }
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════

        private void OpenFolder_Click(object s, RoutedEventArgs e)
        {
            try
            {
                var folder = ((QASmartTouch.App)Application.Current).StudentFileTransfer.ReceiveFolder;
                Directory.CreateDirectory(folder);
                Task.Run(() =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start("explorer.exe", folder);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Failed to open explorer asynchronously: {Err}", ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to prepare folder: {Err}", ex.Message);
            }
        }

        private string GetPendingSyncFolder() => Path.Combine(QASmartClass.Services.AppPaths.DocumentsDir, "PendingSync");

        private void UpdateSyncOfflineButtonVisibility()
        {
            try
            {
                var folder = GetPendingSyncFolder();
                int pendingCount = Directory.Exists(folder) ? Directory.GetFiles(folder).Length : 0;
                if (pendingCount > 0)
                {
                    btnSyncOffline.Content = $"🔄 Đồng bộ ngoại tuyến ({pendingCount} file)";
                    btnSyncOffline.Visibility = Visibility.Visible;
                }
                else
                {
                    btnSyncOffline.Visibility = Visibility.Collapsed;
                }
            }
            catch { btnSyncOffline.Visibility = Visibility.Collapsed; }
        }

        private async void SyncOffline_Click(object sender, RoutedEventArgs e)
        {
            if (_isSyncing) return;
            _isSyncing = true;
            if (btnSyncOffline != null) btnSyncOffline.IsEnabled = false;
            var originalCursor = this.Cursor;
            this.Cursor = System.Windows.Input.Cursors.Wait;

            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var client = app.StudentNetwork;
                var sft = app.StudentFileTransfer;

                if (client == null || !client.IsConnected)
                {
                    MessageBox.Show("Vẫn không thể kết nối tới Giáo viên. Vui lòng kiểm tra lại kết nối mạng!",
                        "Lỗi kết nối", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var folder = GetPendingSyncFolder();
                if (!Directory.Exists(folder)) return;

                var files = Directory.GetFiles(folder);
                if (files.Length == 0) return;

                string studentCode = "HS00001";
                string studentName = "Học sinh";
                int studentId = 0;

                if (app.Database != null)
                {
                    var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(app.Database);
                    var (currentId, currentCode, currentName) = identityService.GetCurrentStudent();
                    studentCode = currentCode;
                    studentName = currentName;
                    studentId = currentId;
                }
                else if (app.StudentNetwork != null)
                {
                    studentCode = app.StudentNetwork.StudentCode ?? studentCode;
                    studentName = app.StudentNetwork.StudentName ?? studentName;
                }

                int successCount = 0;
                progressBar.Value = 0;
                txtStatus.Text = "🔄 Đang đồng bộ các bài làm ngoại tuyến...";

                var teacherIP = client.ConnectedIP;

                foreach (var filePath in files)
                {
                    var fileName = Path.GetFileName(filePath);
                    
                    if (!fileName.StartsWith(studentCode + "_"))
                    {
                        Log.Information("Bỏ qua file offline {File} vì không thuộc sở hữu của học sinh hiện tại {Code}", fileName, studentCode);
                        continue;
                    }

                    string originalFileName = fileName;
                    var parts = fileName.Split('_');
                    if (parts.Length >= 3)
                    {
                        originalFileName = string.Join("_", parts.Skip(2));
                    }
                    else
                    {
                        originalFileName = fileName.Substring(studentCode.Length + 1);
                    }

                    txtStatus.Text = "📤 Đang gửi: " + originalFileName + "...";

                    bool success = false;
                    try
                    {
                        if (!string.IsNullOrEmpty(teacherIP))
                        {
                            var submitRes = await sft.SubmitFileAsync(filePath, teacherIP, studentCode, studentName);
                            success = submitRes.Success;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Đồng bộ offline TCP failed: {Err}", ex.Message);
                    }

                    if (success)
                    {
                        successCount++;
                        try
                        {
                            File.Delete(filePath);
                            
                            using var db = new QASmartClass.Data.AppDbContext();
                            var record = db.FileTransfers
                                .Where(r => r.StudentId == studentId && r.Direction == "StudentToTeacher" && r.FileName == originalFileName && r.Status == "Pending")
                                .OrderByDescending(r => r.CreatedAt)
                                .FirstOrDefault();
                            if (record != null)
                            {
                                record.Status = "Completed";
                                record.ProgressPercent = 100;
                                
                                db.EventLogs.Add(new Data.EventLog
                                {
                                    EventType = "FILE", Actor = studentCode,
                                    Details = "Đồng bộ bài làm ngoại tuyến thành công: " + originalFileName,
                                    Timestamp = DateTime.Now
                                });
                                await db.SaveChangesAsync();
                            }
                        }
                        catch (Exception exDb)
                        {
                            Log.Warning("Update offline sync DB record failed: {Err}", exDb.Message);
                        }
                    }
                }

                progressBar.Value = 100;
                UpdateSyncOfflineButtonVisibility();
                _activeTab = "submitted";
                LoadReceivedFiles();
                UpdateStats();
                LoadActivity();

                if (successCount == files.Length)
                {
                    MessageBox.Show("✅ Đồng bộ thành công " + successCount + " bài làm ngoại tuyến lên máy Giáo viên!", 
                        "Đồng bộ thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("⚠️ Chỉ đồng bộ thành công " + successCount + "/" + files.Length + " bài làm. Vui lòng thử lại sau!", 
                        "Kết quả đồng bộ", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            finally
            {
                this.Cursor = originalCursor;
                if (btnSyncOffline != null) btnSyncOffline.IsEnabled = true;
                _isSyncing = false;
            }
        }

        private static string FormatSize(long b) => b switch
        { < 1024 => $"{b} B", < 1048576 => $"{b / 1024.0:F1} KB", _ => $"{b / 1048576.0:F1} MB" };

        private static string GetIcon(string ext) => ext.ToLower() switch
        {
            ".pdf" => "📄", ".docx" or ".doc" => "📝", ".xlsx" or ".xls" => "📊",
            ".pptx" or ".ppt" => "📋", ".jpg" or ".png" or ".jpeg" => "🖼️",
            ".mp4" or ".avi" => "🎬", ".txt" => "📃", _ => "📁"
        };
    }
}




