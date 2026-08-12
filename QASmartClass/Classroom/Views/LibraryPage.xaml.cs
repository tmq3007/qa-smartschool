using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Wpf;
using Serilog;
using System.Threading.Tasks;

namespace QASmartClass.Classroom.Views
{
    public partial class LibraryPage : Page, INavigatedPage, IDisposable
    {
        private bool _isInitialized = false;
        private List<LibraryItem> _allFiles = new();
        private string _currentFilter = "all";
        private LibraryItem? _selectedFile;
        private bool _isVideoPlaying;

        // Image zoom & pan state
        private bool _isDragging;
        private Point _dragStart;
        private double _imgZoom = 1.0;
        private double _pdfZoom = 1.0;

        // Folder navigation
        private string _rootPath = string.Empty;
        private string _currentFolder = string.Empty;

        // Video timer & state
        private System.Windows.Threading.DispatcherTimer? _videoTimer;
        private bool _isUserDraggingSlider = false;

        public LibraryPage()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                if (!_isInitialized)
                {
                    LoadLibrary();
                    _isInitialized = true;
                }
            };

            // Khởi tạo timer cập nhật thanh tua video
            _videoTimer = new System.Windows.Threading.DispatcherTimer();
            _videoTimer.Interval = TimeSpan.FromMilliseconds(200);
            _videoTimer.Tick += VideoTimer_Tick;
        }

        // ═══════════════════════════════════════════════════════════
        //  LOAD & FILTER
        // ═══════════════════════════════════════════════════════════

        private void LoadLibrary()
        {
            _rootPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "QA SmartClass", "Library");
            Directory.CreateDirectory(_rootPath);

            // Create default example subfolders if library is empty
            if (!Directory.EnumerateFileSystemEntries(_rootPath).Any())
            {
                var subfolders = new[] { "Bài giảng", "Hình ảnh", "Video", "Đề thi", "Tài liệu" };
                foreach (var sf in subfolders)
                    Directory.CreateDirectory(Path.Combine(_rootPath, sf));
            }

            if (string.IsNullOrEmpty(_currentFolder))
                _currentFolder = _rootPath;

            NavigateToFolder(_currentFolder);
        }

        private void NavigateToFolder(string folderPath)
        {
            _currentFolder = folderPath;
            _selectedFile = null;

            _allFiles = new List<LibraryItem>();

            if (Directory.Exists(folderPath))
            {
                // Add subfolders first
                foreach (var dir in Directory.GetDirectories(folderPath))
                {
                    var di = new DirectoryInfo(dir);
                    var childCount = 0;
                    try { childCount = di.GetFileSystemInfos().Length; } catch { }
                    _allFiles.Add(new LibraryItem
                    {
                        Name = di.Name,
                        Path = dir,
                        Size = $"{childCount} mục",
                        SizeBytes = childCount,
                        LastModified = di.LastWriteTime,
                        Icon = "📁",
                        Type = "folder",
                        IsFolder = true,
                        Extension = ""
                    });
                }

                // Add files
                foreach (var f in Directory.GetFiles(folderPath))
                {
                    var fi = new FileInfo(f);
                    _allFiles.Add(new LibraryItem
                    {
                        Name = fi.Name,
                        Path = f,
                        Size = FormatSize(fi.Length),
                        SizeBytes = fi.Length,
                        LastModified = fi.LastWriteTime,
                        Icon = GetIcon(fi.Extension),
                        Type = GetFileType(fi.Extension),
                        Extension = fi.Extension.ToLower()
                    });
                }
            }

            // Update info & breadcrumb
            var relPath = _currentFolder.Replace(_rootPath, "").TrimStart(Path.DirectorySeparatorChar);
            txtLibInfo.Text = string.IsNullOrEmpty(relPath)
                ? $"📁 Thư viện gốc — {_allFiles.Count} mục"
                : $"📁 {relPath} — {_allFiles.Count} mục";

            RenderBreadcrumb();
            ApplyFilter();
        }

        private void RenderBreadcrumb()
        {
            breadcrumbPanel.Children.Clear();

            // Root button
            var rootBtn = new Button
            {
                Content = "🏠 Thư viện", FontSize = 12, Padding = new Thickness(4, 2, 4, 2),
                Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand, Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210))
            };
            rootBtn.Click += (s, e) => NavigateToFolder(_rootPath);
            breadcrumbPanel.Children.Add(rootBtn);

            // Sub-segments
            if (_currentFolder != _rootPath)
            {
                var rel = _currentFolder.Replace(_rootPath, "").TrimStart(Path.DirectorySeparatorChar);
                var parts = rel.Split(Path.DirectorySeparatorChar);
                var pathSoFar = _rootPath;

                foreach (var part in parts)
                {
                    pathSoFar = Path.Combine(pathSoFar, part);
                    breadcrumbPanel.Children.Add(new TextBlock
                    {
                        Text = " ▸ ", FontSize = 12, Foreground = Brushes.Gray,
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    var segPath = pathSoFar;
                    var isLast = pathSoFar == _currentFolder;
                    var segBtn = new Button
                    {
                        Content = part, FontSize = 12, Padding = new Thickness(4, 2, 4, 2),
                        Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                        Cursor = Cursors.Hand,
                        Foreground = isLast ? Brushes.Black : new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                        FontWeight = isLast ? FontWeights.SemiBold : FontWeights.Normal
                    };
                    if (!isLast) segBtn.Click += (s, e) => NavigateToFolder(segPath);
                    breadcrumbPanel.Children.Add(segBtn);
                }
            }
        }

        private void ApplyFilter()
        {
            if (fileListPanel == null) return;

            var filtered = _allFiles.AsEnumerable();

            // Type filter (folders always shown)
            if (_currentFilter != "all")
                filtered = filtered.Where(f => f.IsFolder || f.Type == _currentFilter);

            // Search
            var q = txtSearch?.Text?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(q) && !q.StartsWith("🔍"))
                filtered = filtered.Where(f => f.Name.Contains(q, StringComparison.OrdinalIgnoreCase));

            // Sort
            var sortIdx = cmbSort?.SelectedIndex ?? 0;
            filtered = sortIdx switch
            {
                0 => filtered.OrderByDescending(f => f.IsFolder).ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase),
                1 => filtered.OrderByDescending(f => f.IsFolder).ThenByDescending(f => f.Name, StringComparer.OrdinalIgnoreCase),
                2 => filtered.OrderByDescending(f => f.IsFolder).ThenBy(f => f.SizeBytes),
                3 => filtered.OrderByDescending(f => f.IsFolder).ThenByDescending(f => f.SizeBytes),
                4 => filtered.OrderByDescending(f => f.IsFolder).ThenByDescending(f => f.LastModified),
                5 => filtered.OrderByDescending(f => f.IsFolder).ThenBy(f => f.Type).ThenBy(f => f.Name),
                _ => filtered
            };

            var list = filtered.ToList();
            txtFileCount.Text = list.Count.ToString();
            RenderFileList(list);
        }

        // ═══════════════════════════════════════════════════════════
        //  RENDER FILE LIST (left panel)
        // ═══════════════════════════════════════════════════════════

        private void RenderFileList(List<LibraryItem> files)
        {
            fileListPanel.Children.Clear();

            // "Go up" row if not at root
            if (_currentFolder != _rootPath)
            {
                var upRow = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 249, 196)),
                    CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 5, 8, 5),
                    Margin = new Thickness(0, 0, 0, 3), Cursor = Cursors.Hand,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(255, 235, 59)),
                    BorderThickness = new Thickness(0.5)
                };
                var upSp = new StackPanel { Orientation = Orientation.Horizontal };
                upSp.Children.Add(new TextBlock { Text = "⬆️", FontSize = 14, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
                upSp.Children.Add(new TextBlock { Text = "Quay lại thư mục cha", FontSize = 11, FontStyle = FontStyles.Italic, Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)), VerticalAlignment = VerticalAlignment.Center });
                upRow.Child = upSp;
                upRow.MouseLeftButtonDown += (s, e) =>
                {
                    var parent = Directory.GetParent(_currentFolder)?.FullName;
                    if (!string.IsNullOrEmpty(parent) && parent.StartsWith(_rootPath))
                        NavigateToFolder(parent);
                    else
                        NavigateToFolder(_rootPath);
                };
                upRow.MouseEnter += (s, e) => upRow.Background = new SolidColorBrush(Color.FromRgb(255, 241, 118));
                upRow.MouseLeave += (s, e) => upRow.Background = new SolidColorBrush(Color.FromRgb(255, 249, 196));
                fileListPanel.Children.Add(upRow);
            }

            foreach (var file in files)
            {
                var isSelected = file == _selectedFile;
                var row = new Border
                {
                    Background = isSelected ? new SolidColorBrush(Color.FromRgb(227, 242, 253))
                        : file.IsFolder ? new SolidColorBrush(Color.FromRgb(255, 253, 231)) : Brushes.White,
                    CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 5, 8, 5),
                    Margin = new Thickness(0, 0, 0, 2), Cursor = Cursors.Hand,
                    BorderBrush = isSelected ? new SolidColorBrush(Color.FromRgb(25, 118, 210))
                        : new SolidColorBrush(Color.FromRgb(238, 238, 238)),
                    BorderThickness = new Thickness(isSelected ? 1.5 : 0.5),
                    ToolTip = file.IsFolder ? $"{file.Name}\n{file.Size}"
                        : $"{file.Name}\n{file.Size}\n📅 {file.LastModified:dd/MM/yyyy HH:mm}"
                };

                var dp = new DockPanel();

                // Size/count right
                dp.Children.Add(new TextBlock
                {
                    Text = file.Size, FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                    VerticalAlignment = VerticalAlignment.Center
                });
                DockPanel.SetDock(dp.Children[0], Dock.Right);

                // Icon + Name left (Sử dụng DockPanel để tự động co giãn theo GridSplitter)
                var leftSp = new DockPanel { LastChildFill = true };
                var iconTb = new TextBlock
                {
                    Text = file.Icon, FontSize = file.IsFolder ? 16 : 18,
                    Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(iconTb, Dock.Left);
                leftSp.Children.Add(iconTb);

                var nameTb = new TextBlock
                {
                    Text = file.Name, FontSize = 13,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = isSelected || file.IsFolder ? FontWeights.SemiBold : FontWeights.Normal
                };
                leftSp.Children.Add(nameTb);
                dp.Children.Add(leftSp);
                row.Child = dp;

                var captured = file;
                row.MouseLeftButtonDown += (s, e) =>
                {
                    if (e.ClickCount == 2 && captured.IsFolder)
                        NavigateToFolder(captured.Path);
                    else if (e.ClickCount == 2 && !captured.IsFolder)
                        ShowFullscreen(captured);
                    else if (!captured.IsFolder)
                        SelectFile(captured);
                };
                row.MouseEnter += (s, e) =>
                {
                    if (captured != _selectedFile)
                        row.Background = captured.IsFolder
                            ? new SolidColorBrush(Color.FromRgb(235, 243, 251)) // Xanh dương pastel dịu mắt
                            : new SolidColorBrush(Color.FromRgb(245, 245, 245));
                };
                row.MouseLeave += (s, e) =>
                {
                    if (captured != _selectedFile)
                        row.Background = captured.IsFolder
                            ? new SolidColorBrush(Color.FromRgb(255, 253, 231))
                            : Brushes.White;
                };

                // Context menu for folders
                if (file.IsFolder)
                {
                    var ctx = new ContextMenu();
                    var miOpen = new MenuItem { Header = "📂 Mở thư mục" };
                    miOpen.Click += (s, e) => NavigateToFolder(captured.Path);
                    ctx.Items.Add(miOpen);
                    var miRename = new MenuItem { Header = "✏️ Đổi tên" };
                    miRename.Click += (s, e) => RenameFolder(captured);
                    ctx.Items.Add(miRename);
                    ctx.Items.Add(new Separator());
                    var miDel = new MenuItem { Header = "🗑️ Xóa thư mục" };
                    miDel.Click += (s, e) => DeleteFolder(captured);
                    ctx.Items.Add(miDel);
                    row.ContextMenu = ctx;
                }
                else
                {
                    // Context menu for files
                    var ctx = new ContextMenu();
                    var miOpen = new MenuItem { Header = "📂 Mở bằng ứng dụng" };
                    miOpen.Click += (s, e) => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(captured.Path) { UseShellExecute = true }); } catch { } };
                    ctx.Items.Add(miOpen);
                    var miMove = new MenuItem { Header = "📦 Di chuyển đến..." };
                    miMove.Click += (s, e) => MoveFile(captured);
                    ctx.Items.Add(miMove);
                    ctx.Items.Add(new Separator());
                    var miDel = new MenuItem { Header = "🗑️ Xóa file" };
                    miDel.Click += (s, e) =>
                    {
                        var r = MessageBox.Show($"Xóa file '{captured.Name}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (r == MessageBoxResult.Yes)
                        {
                            try { File.Delete(captured.Path); NavigateToFolder(_currentFolder); }
                            catch (Exception ex) { MessageBox.Show(ex.Message); }
                        }
                    };
                    ctx.Items.Add(miDel);
                    row.ContextMenu = ctx;
                }

                fileListPanel.Children.Add(row);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  SELECT FILE & PREVIEW
        // ═══════════════════════════════════════════════════════════

        private void SelectFile(LibraryItem file)
        {
            _selectedFile = file;
            txtPreviewTitle.Text = $"{file.Icon} {file.Name}  ({file.Size})";

            // Stop video if playing
            StopVideo();
            // Disable annotation mode and clear strokes when switching files
            DisableAnnotation();
            inkOverlay.Strokes.Clear();
            pdfInkOverlay.Strokes.Clear();

            // Hide all previews
            emptyPreview.Visibility = Visibility.Collapsed;
            imagePreview.Visibility = Visibility.Collapsed;
            videoPreview.Visibility = Visibility.Collapsed;
            textPreview.Visibility = Visibility.Collapsed;
            pdfPreviewContainer.Visibility = Visibility.Collapsed;

            bool hasRealFile = !string.IsNullOrEmpty(file.Path) && File.Exists(file.Path);

            if (file.Type == "image" && hasRealFile)
            {
                ShowImagePreview(file.Path);
            }
            else if (file.Type == "video" && hasRealFile)
            {
                ShowVideoPreview(file.Path);
            }
            else if (file.Type == "pdf" && hasRealFile)
            {
                _ = ShowPdfPreview(file.Path);
            }
            else
            {
                // Show file info as text
                ShowFileInfo(file, hasRealFile);
            }

            // Refresh file list to highlight selected
            ApplyFilter();
        }

        private void ShowImagePreview(string path)
        {
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(path, UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                previewImage.Source = bi;
                // Reset zoom & pan
                _imgZoom = 1.0;
                imgScale.ScaleX = 1; imgScale.ScaleY = 1;
                imgTranslate.X = 0; imgTranslate.Y = 0;
                UpdateZoomLabel();
                imagePreview.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ShowErrorText($"Không thể mở hình ảnh:\n{ex.Message}");
            }
        }

        private void ShowVideoPreview(string path)
        {
            try
            {
                previewVideo.Source = new Uri(path, UriKind.Absolute);
                videoPreview.Visibility = Visibility.Visible;
                previewVideo.Play();
                _isVideoPlaying = true;
                btnVideoPlay.Content = "⏸";

                // Chạy timer cập nhật thanh tua
                _videoTimer?.Start();
            }
            catch (Exception ex)
            {
                ShowErrorText($"Không thể phát video:\n{ex.Message}");
            }
        }

        private async Task ShowPdfPreview(string path)
        {
            try
            {
                pdfPreviewContainer.Visibility = Visibility.Visible;
                _pdfZoom = 1.0;
                txtPdfZoom.Text = "100%";
                if (pdfPreview.CoreWebView2 == null)
                    await pdfPreview.EnsureCoreWebView2Async();
                string folderPath = System.IO.Path.GetDirectoryName(path) ?? "";
                string fileName = System.IO.Path.GetFileName(path) ?? "";

                if (!string.IsNullOrEmpty(folderPath) && !string.IsNullOrEmpty(fileName))
                {
                    pdfPreview.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "smartclass.assets",
                        folderPath,
                        Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

                    string escapedFile = Uri.EscapeDataString(fileName);
                    pdfPreview.CoreWebView2.Navigate($"https://smartclass.assets/{escapedFile}");
                }
                else
                {
                    pdfPreview.CoreWebView2.Navigate(new Uri(path).AbsoluteUri);
                }
            }
            catch (Exception ex)
            {
                pdfPreviewContainer.Visibility = Visibility.Collapsed;
                ShowErrorText($"Không thể mở PDF:\n{ex.Message}");
            }
        }

        private void ShowFileInfo(LibraryItem file, bool hasRealFile)
        {
            var info = $"📄 {file.Name}\n\n" +
                       $"📁 Loại: {file.Type.ToUpper()}\n" +
                       $"💾 Kích thước: {file.Size}\n" +
                       $"📝 Đuôi file: {file.Extension}\n\n";

            if (hasRealFile)
            {
                var fi = new FileInfo(file.Path);
                info += $"📆 Ngày tạo: {fi.CreationTime:dd/MM/yyyy HH:mm}\n" +
                        $"📆 Sửa lần cuối: {fi.LastWriteTime:dd/MM/yyyy HH:mm}\n\n" +
                        $"📂 Đường dẫn:\n{file.Path}\n\n" +
                        "💡 Double-click hoặc nhấn 'Mở ngoài' để xem bằng ứng dụng mặc định.\n" +
                        "💡 Nhấn 'Toàn màn hình' để xem full-screen.";
            }
            else
            {
                info += "ℹ️ File mẫu (demo) — không tồn tại trên đĩa.\n\n" +
                        "💡 Bạn có thể thêm file thật bằng nút '📥 Thêm file'\n" +
                        "   hoặc copy trực tiếp vào thư mục thư viện.";
            }

            previewText.Text = info;
            textPreview.Visibility = Visibility.Visible;
        }

        private void ShowErrorText(string msg)
        {
            previewText.Text = msg;
            textPreview.Visibility = Visibility.Visible;
        }

        private void StopVideo()
        {
            try { previewVideo.Stop(); previewVideo.Source = null; } catch { }
            _isVideoPlaying = false;
            btnVideoPlay.Content = "▶";

            // Dừng timer cập nhật thanh tua video
            _videoTimer?.Stop();
            if (sliderVideoProgress != null) sliderVideoProgress.Value = 0;
            if (txtVideoCurrentTime != null) txtVideoCurrentTime.Text = "00:00";
        }

        // ═══════════════════════════════════════════════════════════
        //  VIDEO CONTROLS
        // ═══════════════════════════════════════════════════════════

        private void VideoPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_isVideoPlaying)
            {
                previewVideo.Pause();
                _isVideoPlaying = false;
                btnVideoPlay.Content = "▶";
                _videoTimer?.Stop();
            }
            else
            {
                previewVideo.Play();
                _isVideoPlaying = true;
                btnVideoPlay.Content = "⏸";
                _videoTimer?.Start();
            }
        }

        private void VideoRewind_Click(object sender, RoutedEventArgs e)
        {
            if (previewVideo.NaturalDuration.HasTimeSpan)
            {
                var pos = previewVideo.Position - TimeSpan.FromSeconds(10);
                previewVideo.Position = pos < TimeSpan.Zero ? TimeSpan.Zero : pos;
            }
        }

        private void VideoForward_Click(object sender, RoutedEventArgs e)
        {
            if (previewVideo.NaturalDuration.HasTimeSpan)
            {
                var pos = previewVideo.Position + TimeSpan.FromSeconds(10);
                var max = previewVideo.NaturalDuration.TimeSpan;
                previewVideo.Position = pos > max ? max : pos;
            }
        }
        // ═══════════════════════════════════════════════════════════
        //  IMAGE ZOOM & PAN
        // ═══════════════════════════════════════════════════════════

        private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double factor = e.Delta > 0 ? 1.15 : 1 / 1.15;
            _imgZoom *= factor;
            _imgZoom = Math.Max(0.1, Math.Min(20.0, _imgZoom));

            // Zoom toward mouse position
            var pos = e.GetPosition(imageContainer);
            var centerX = pos.X / imageContainer.ActualWidth;
            var centerY = pos.Y / imageContainer.ActualHeight;

            imgScale.ScaleX = _imgZoom;
            imgScale.ScaleY = _imgZoom;

            // Adjust translate to zoom toward pointer
            if (_imgZoom > 1.0)
            {
                imgTranslate.X -= (factor - 1) * (pos.X - imageContainer.ActualWidth / 2);
                imgTranslate.Y -= (factor - 1) * (pos.Y - imageContainer.ActualHeight / 2);
            }
            else
            {
                imgTranslate.X = 0;
                imgTranslate.Y = 0;
            }

            UpdateZoomLabel();
            e.Handled = true;
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStart = e.GetPosition(imageContainer);
            imageContainer.CaptureMouse();
            imageContainer.Cursor = Cursors.ScrollAll;
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            imageContainer.ReleaseMouseCapture();
            imageContainer.Cursor = Cursors.Hand;
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            var pos = e.GetPosition(imageContainer);
            imgTranslate.X += pos.X - _dragStart.X;
            imgTranslate.Y += pos.Y - _dragStart.Y;
            _dragStart = pos;
        }

        private void ImgZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _imgZoom = Math.Min(20.0, _imgZoom * 1.25);
            imgScale.ScaleX = _imgZoom; imgScale.ScaleY = _imgZoom;
            UpdateZoomLabel();
        }

        private void ImgZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _imgZoom = Math.Max(0.1, _imgZoom / 1.25);
            imgScale.ScaleX = _imgZoom; imgScale.ScaleY = _imgZoom;
            if (_imgZoom <= 1.0) { imgTranslate.X = 0; imgTranslate.Y = 0; }
            UpdateZoomLabel();
        }

        private void ImgFit_Click(object sender, RoutedEventArgs e)
        {
            _imgZoom = 1.0;
            imgScale.ScaleX = 1; imgScale.ScaleY = 1;
            imgTranslate.X = 0; imgTranslate.Y = 0;
            UpdateZoomLabel();
        }

        private void ImgOriginal_Click(object sender, RoutedEventArgs e)
        {
            // Show at actual pixel size
            if (previewImage.Source is BitmapSource src && imageContainer.ActualWidth > 0)
            {
                _imgZoom = src.PixelWidth / imageContainer.ActualWidth;
                _imgZoom = Math.Max(0.1, Math.Min(20.0, _imgZoom));
            }
            else _imgZoom = 1.0;
            imgScale.ScaleX = _imgZoom; imgScale.ScaleY = _imgZoom;
            imgTranslate.X = 0; imgTranslate.Y = 0;
            UpdateZoomLabel();
        }

        private void ImgReset_Click(object sender, RoutedEventArgs e) => ImgFit_Click(sender, e);

        private void UpdateZoomLabel()
        {
            if (txtZoomLevel != null)
                txtZoomLevel.Text = $"{(int)(_imgZoom * 100)}%";
        }

        // ═══════════════════════════════════════════════════════════
        //  PDF ZOOM CONTROLS
        // ═══════════════════════════════════════════════════════════

        private void PdfZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _pdfZoom = Math.Min(5.0, _pdfZoom + 0.25);
            ApplyPdfZoom();
        }

        private void PdfZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _pdfZoom = Math.Max(0.25, _pdfZoom - 0.25);
            ApplyPdfZoom();
        }

        private void PdfZoomReset_Click(object sender, RoutedEventArgs e)
        {
            _pdfZoom = 1.0;
            ApplyPdfZoom();
        }

        private void ApplyPdfZoom()
        {
            if (pdfPreview.CoreWebView2 != null)
                pdfPreview.ZoomFactor = _pdfZoom;
            txtPdfZoom.Text = $"{(int)(_pdfZoom * 100)}%";
        }

        // ═══════════════════════════════════════════════════════════
        //  ANNOTATION TOOLS (shared toolbar for Image & PDF)
        // ═══════════════════════════════════════════════════════════

        private bool _annotationMode;
        private Color _currentInkColor = Colors.Red;
        private double _currentInkSize = 3;

        private InkCanvas? ActiveInkCanvas =>
            imagePreview.Visibility == Visibility.Visible ? inkOverlay :
            pdfPreviewContainer.Visibility == Visibility.Visible ? pdfInkOverlay : null;

        private void ToggleAnnotation_Click(object sender, RoutedEventArgs e)
        {
            _annotationMode = !_annotationMode;
            var ink = ActiveInkCanvas;
            if (ink == null) { _annotationMode = false; return; }

            bool isPdf = pdfPreviewContainer.Visibility == Visibility.Visible;

            if (_annotationMode)
            {
                annotationToolbar.Visibility = Visibility.Visible;

                if (isPdf)
                {
                    // Chụp ảnh PDF màn hình → ẩn WebView2 → hiện Viewbox và InkCanvas
                    _ = CapturePdfForAnnotation();
                }
                else
                {
                    ink.Visibility = Visibility.Visible;
                }

                ink.IsHitTestVisible = true;
                ink.EditingMode = InkCanvasEditingMode.Ink;
                SetAnnotTool("pen");
            }
            else
            {
                annotationToolbar.Visibility = Visibility.Collapsed;
                ink.IsHitTestVisible = false;
                ink.EditingMode = InkCanvasEditingMode.None;

                if (isPdf)
                {
                    // Trả lại WebView2, ẩn Viewbox chứa ảnh
                    pdfAnnotViewbox.Visibility = Visibility.Collapsed;
                    pdfPreview.Visibility = Visibility.Visible;
                }
                else
                {
                    ink.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async Task CapturePdfForAnnotation()
        {
            try
            {
                if (pdfPreview.CoreWebView2 == null) return;

                using var ms = new MemoryStream();
                await pdfPreview.CoreWebView2.CapturePreviewAsync(
                    Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, ms);
                ms.Position = 0;

                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.StreamSource = ms;
                bi.EndInit();
                bi.Freeze();

                pdfCapturedImage.Source = bi;
                
                // Đồng bộ lưới thiết kế với kích thước ảnh gốc chụp được
                pdfAnnotGrid.Width = bi.PixelWidth;
                pdfAnnotGrid.Height = bi.PixelHeight;

                pdfAnnotViewbox.Visibility = Visibility.Visible;
                pdfPreview.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to capture PDF for annotation");
            }
        }

        private void SetAnnotTool(string tool)
        {
            var active = new SolidColorBrush(Color.FromRgb(25, 118, 210));
            var inactive = Brushes.Transparent;
            btnPen.Background = tool == "pen" ? active : inactive;
            btnHighlight.Background = tool == "highlight" ? active : inactive;
            btnEraser.Background = tool == "eraser" ? active : inactive;

            var ink = ActiveInkCanvas;
            if (ink == null) return;

            if (tool == "pen")
            {
                ink.EditingMode = InkCanvasEditingMode.Ink;
                ink.DefaultDrawingAttributes = new DrawingAttributes
                {
                    Color = _currentInkColor, Width = _currentInkSize, Height = _currentInkSize,
                    StylusTip = StylusTip.Ellipse, IsHighlighter = false
                };
            }
            else if (tool == "highlight")
            {
                ink.EditingMode = InkCanvasEditingMode.Ink;
                ink.DefaultDrawingAttributes = new DrawingAttributes
                {
                    Color = _currentInkColor, Width = _currentInkSize * 4, Height = _currentInkSize * 2,
                    StylusTip = StylusTip.Rectangle, IsHighlighter = true
                };
            }
            else if (tool == "eraser")
            {
                ink.EditingMode = InkCanvasEditingMode.EraseByStroke;
            }
        }

        private void AnnotPen_Click(object sender, RoutedEventArgs e) => SetAnnotTool("pen");
        private void AnnotHighlight_Click(object sender, RoutedEventArgs e) => SetAnnotTool("highlight");
        private void AnnotEraser_Click(object sender, RoutedEventArgs e) => SetAnnotTool("eraser");

        private void AnnotColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                _currentInkColor = (Color)ColorConverter.ConvertFromString(hex);
                var ink = ActiveInkCanvas;
                if (ink == null) return;
                if (ink.EditingMode == InkCanvasEditingMode.EraseByStroke)
                    SetAnnotTool("pen");
                else
                {
                    var da = ink.DefaultDrawingAttributes.Clone();
                    da.Color = _currentInkColor;
                    ink.DefaultDrawingAttributes = da;
                }
            }
        }

        private void AnnotSize_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string sizeStr && double.TryParse(sizeStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sz))
            {
                _currentInkSize = sz;
                var ink = ActiveInkCanvas;
                if (ink == null) return;
                var da = ink.DefaultDrawingAttributes.Clone();
                if (da.IsHighlighter) { da.Width = sz * 4; da.Height = sz * 2; }
                else { da.Width = sz; da.Height = sz; }
                ink.DefaultDrawingAttributes = da;
            }
        }

        private void AnnotUndo_Click(object sender, RoutedEventArgs e)
        {
            var ink = ActiveInkCanvas;
            if (ink != null && ink.Strokes.Count > 0)
                ink.Strokes.RemoveAt(ink.Strokes.Count - 1);
        }

        private void AnnotClear_Click(object sender, RoutedEventArgs e)
        {
            var ink = ActiveInkCanvas;
            if (ink != null && ink.Strokes.Count > 0)
            {
                var r = MessageBox.Show("Xóa tất cả ghi chú?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes) ink.Strokes.Clear();
            }
        }

        private void AnnotSave_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFile == null) return;
            FrameworkElement? target = imagePreview.Visibility == Visibility.Visible ? imagePreview :
                pdfPreviewContainer.Visibility == Visibility.Visible ? pdfAnnotGrid : null;
            if (target == null) return;
            try
            {
                double width = target.Width;
                double height = target.Height;
                if (double.IsNaN(width) || width <= 0) width = target.ActualWidth;
                if (double.IsNaN(height) || height <= 0) height = target.ActualHeight;

                var sz = new Size(width, height);
                target.Measure(sz); target.Arrange(new Rect(sz));
                var rtb = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(target);
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Lưu ảnh có ghi chú",
                    FileName = Path.GetFileNameWithoutExtension(_selectedFile.Name) + "_annotated.png",
                    Filter = "PNG|*.png|JPEG|*.jpg"
                };
                if (dlg.ShowDialog() == true)
                {
                    BitmapEncoder enc = dlg.FilterIndex == 2 ? new JpegBitmapEncoder() : new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(rtb));
                    using var fs = new FileStream(dlg.FileName, FileMode.Create);
                    enc.Save(fs);
                    MessageBox.Show($"✅ Đã lưu: {Path.GetFileName(dlg.FileName)}", "Lưu thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi lưu: {ex.Message}"); }
        }

        private void DisableAnnotation()
        {
            _annotationMode = false;
            annotationToolbar.Visibility = Visibility.Collapsed;
            inkOverlay.IsHitTestVisible = false;
            inkOverlay.EditingMode = InkCanvasEditingMode.None;
            pdfInkOverlay.IsHitTestVisible = false;
            pdfInkOverlay.EditingMode = InkCanvasEditingMode.None;
            
            // Trả lại giao diện PDF nguyên bản
            pdfAnnotViewbox.Visibility = Visibility.Collapsed;
            pdfCapturedImage.Source = null;
            pdfPreview.Visibility = Visibility.Visible;
        }

        // ═══════════════════════════════════════════════════════════
        //  FULLSCREEN VIEW
        // ═══════════════════════════════════════════════════════════

        private void Fullscreen_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFile != null) ShowFullscreen(_selectedFile);
        }

        private void ShowFullscreen(LibraryItem file)
        {
            bool hasRealFile = !string.IsNullOrEmpty(file.Path) && File.Exists(file.Path);
            if (!hasRealFile)
            {
                MessageBox.Show("File mẫu không tồn tại trên đĩa.\nHãy thêm file thật để xem toàn màn hình.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var wnd = new Window
            {
                Title = $"📄 {file.Name}",
                WindowState = WindowState.Maximized,
                WindowStyle = WindowStyle.None,
                Background = Brushes.Black,
                Topmost = true
            };

            // Close on Escape
            wnd.KeyDown += (s, e) => { if (e.Key == Key.Escape) wnd.Close(); };

            var mainGrid = new Grid();

            // Close button overlay
            var closeBtn = new Button
            {
                Content = "✕ ESC", FontSize = 13, Padding = new Thickness(14, 8, 14, 8),
                Background = new SolidColorBrush(Color.FromArgb(180, 30, 30, 30)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 16, 24, 0)
            };
            closeBtn.Click += (s, e) => wnd.Close();
            Panel.SetZIndex(closeBtn, 100);

            if (file.Type == "image")
            {
                var img = new Image { Stretch = Stretch.Uniform };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                var bi = new BitmapImage();
                bi.BeginInit(); bi.UriSource = new Uri(file.Path); bi.CacheOption = BitmapCacheOption.OnLoad; bi.EndInit();
                img.Source = bi;
                mainGrid.Children.Add(img);
            }
            else if (file.Type == "video")
            {
                StopVideo(); // Stop inline preview

                var me = new MediaElement { LoadedBehavior = MediaState.Manual, Stretch = Stretch.Uniform };
                me.Source = new Uri(file.Path);
                mainGrid.Children.Add(me);
                me.Play();

                // Video controls
                var controlBar = new StackPanel
                {
                    Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 40)
                };
                bool fsPlaying = true;
                var playBtn = new Button
                {
                    Content = "⏸", FontSize = 22, Width = 50, Height = 50,
                    Background = new SolidColorBrush(Color.FromArgb(200, 25, 118, 210)),
                    Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand
                };
                playBtn.Click += (s, e) =>
                {
                    if (fsPlaying) { me.Pause(); playBtn.Content = "▶"; } else { me.Play(); playBtn.Content = "⏸"; }
                    fsPlaying = !fsPlaying;
                };
                controlBar.Children.Add(playBtn);
                Panel.SetZIndex(controlBar, 99);
                mainGrid.Children.Add(controlBar);

                wnd.Closed += (s, e) => { me.Stop(); me.Source = null; };
            }
            else if (file.Type == "pdf")
            {
                var fsWebView = new WebView2();
                mainGrid.Children.Add(fsWebView);
                wnd.Background = Brushes.White;
                wnd.Loaded += async (s2, e2) =>
                {
                    try
                    {
                        await fsWebView.EnsureCoreWebView2Async();
                        fsWebView.CoreWebView2.Navigate(new Uri(file.Path).AbsoluteUri);
                    }
                    catch { }
                };
                wnd.Closed += (s2, e2) => { try { fsWebView.Dispose(); } catch { } };
            }
            else
            {
                // Open externally for unsupported types
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file.Path) { UseShellExecute = true }); }
                catch { }
                return;
            }

            mainGrid.Children.Add(closeBtn);
            wnd.Content = mainGrid;
            wnd.ShowDialog();
        }

        // ═══════════════════════════════════════════════════════════
        //  TOOLBAR ACTIONS
        // ═══════════════════════════════════════════════════════════

        private void OpenExternal_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFile == null) return;
            if (!string.IsNullOrEmpty(_selectedFile.Path) && File.Exists(_selectedFile.Path))
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_selectedFile.Path) { UseShellExecute = true }); }
                catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
            }
            else
            {
                MessageBox.Show("File mẫu — không tồn tại trên đĩa.", "Thông báo");
            }
        }

        private async void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFile == null) { MessageBox.Show("Chọn file trước!"); return; }
            if (string.IsNullOrEmpty(_selectedFile.Path) || !File.Exists(_selectedFile.Path))
            {
                _allFiles.Remove(_selectedFile);
                _selectedFile = null;
                ResetPreview();
                ApplyFilter();
                return;
            }
            var r = MessageBox.Show($"Xóa file:\n\"{_selectedFile.Name}\"?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r == MessageBoxResult.Yes)
            {
                try
                {
                    StopVideo();
                    
                    // Giải phóng WebView2 nếu đang mở PDF để tránh lỗi khóa tệp
                    if (_selectedFile.Type == "pdf" && pdfPreview.CoreWebView2 != null)
                    {
                        pdfPreview.CoreWebView2.Navigate("about:blank");
                        await Task.Delay(250); // Đợi WebView2 chuyển hướng
                    }

                    string pathToDelete = _selectedFile.Path;
                    _selectedFile = null;
                    ResetPreview();

                    // Vòng lặp xóa tệp tin có thử lại
                    bool deleted = false;
                    for (int i = 0; i < 5; i++)
                    {
                        try
                        {
                            if (File.Exists(pathToDelete))
                            {
                                File.Delete(pathToDelete);
                            }
                            deleted = true;
                            break;
                        }
                        catch (IOException)
                        {
                            await Task.Delay(100); // Thử lại sau 100ms
                        }
                    }

                    if (!deleted)
                    {
                        MessageBox.Show("Tệp đang được mở bởi một chương trình khác. Vui lòng thử lại sau.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    NavigateToFolder(_currentFolder);
                }
                catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
            }
        }

        private void ResetPreview()
        {
            emptyPreview.Visibility = Visibility.Visible;
            imagePreview.Visibility = Visibility.Collapsed;
            videoPreview.Visibility = Visibility.Collapsed;
            textPreview.Visibility = Visibility.Collapsed;
            pdfPreviewContainer.Visibility = Visibility.Collapsed;
            txtPreviewTitle.Text = "📄 Chọn file để xem trước";

            // Dọn dẹp các nét vẽ cũ giải phóng bộ nhớ đồ họa
            if (inkOverlay != null) inkOverlay.Strokes.Clear();
            if (pdfInkOverlay != null) pdfInkOverlay.Strokes.Clear();
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                _currentFilter = tag;
                ApplyFilter();
            }
        }

        private void Sort_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void Search_Changed(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(_currentFolder);
            System.Diagnostics.Process.Start("explorer.exe", _currentFolder);
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Thêm tài nguyên vào thư viện",
                Multiselect = true,
                Filter = "Tất cả|*.pdf;*.docx;*.pptx;*.xlsx;*.jpg;*.png;*.mp4;*.zip|Mọi file|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                Directory.CreateDirectory(_currentFolder);
                foreach (var src in dlg.FileNames)
                {
                    var dest = Path.Combine(_currentFolder, Path.GetFileName(src));
                    if (File.Exists(dest))
                    {
                        var r = MessageBox.Show($"Tệp '{Path.GetFileName(src)}' đã tồn tại trong thư mục hiện tại.\n\nBạn có muốn ghi đè lên tệp cũ không?", 
                            "Cảnh báo trùng tên", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                        
                        if (r == MessageBoxResult.Cancel) continue; // Bỏ qua tệp này
                        if (r == MessageBoxResult.No)
                        {
                            // Tự động đổi tên mới bằng cách thêm hậu tố số thứ tự
                            string ext = Path.GetExtension(dest);
                            string nameNoExt = Path.GetFileNameWithoutExtension(dest);
                            int count = 1;
                            while (File.Exists(dest))
                            {
                                dest = Path.Combine(_currentFolder, $"{nameNoExt} ({count}){ext}");
                                count++;
                            }
                        }
                    }
                    File.Copy(src, dest, overwrite: true);
                }
                NavigateToFolder(_currentFolder);
                MessageBox.Show($"✅ Đã thêm {dlg.FileNames.Length} file vào thư mục hiện tại!",
                    "Thêm tài nguyên", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  FOLDER MANAGEMENT
        // ═══════════════════════════════════════════════════════════

        private void NewFolder_Click(object sender, RoutedEventArgs e)
        {
            var name = ShowInputDialog("Tạo thư mục mới", "Nhập tên thư mục:", "Thư mục mới");
            if (string.IsNullOrWhiteSpace(name)) return;
            name = name.Trim();

            if (!IsValidFileName(name))
            {
                MessageBox.Show("Tên thư mục không hợp lệ hoặc chứa các ký tự cấm!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var newPath = Path.Combine(_currentFolder, name);
                if (Directory.Exists(newPath))
                {
                    MessageBox.Show($"Thư mục '{name}' đã tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Directory.CreateDirectory(newPath);
                NavigateToFolder(_currentFolder);
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        private void RenameFolder(LibraryItem folder)
        {
            var newName = ShowInputDialog("Đổi tên thư mục", $"Tên mới cho '{folder.Name}':", folder.Name);
            if (string.IsNullOrWhiteSpace(newName) || newName.Trim() == folder.Name) return;
            newName = newName.Trim();

            if (!IsValidFileName(newName))
            {
                MessageBox.Show("Tên thư mục không hợp lệ hoặc chứa các ký tự cấm!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var newPath = Path.Combine(Path.GetDirectoryName(folder.Path)!, newName);
                if (Directory.Exists(newPath))
                {
                    MessageBox.Show($"Thư mục '{newName}' đã tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Directory.Move(folder.Path, newPath);
                NavigateToFolder(_currentFolder);
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        private void DeleteFolder(LibraryItem folder)
        {
            var r = MessageBox.Show($"Xóa thư mục '{folder.Name}' và toàn bộ nội dung?\n\n⚠️ Hành động này không thể hoàn tác!",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (r == MessageBoxResult.Yes)
            {
                try
                {
                    Directory.Delete(folder.Path, recursive: true);
                    NavigateToFolder(_currentFolder);
                }
                catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
            }
        }

        private void MoveFile(LibraryItem file)
        {
            // Show folder picker within this library to move file
            var subfolders = GetAllSubfolders(_rootPath);
            if (subfolders.Count == 0)
            {
                MessageBox.Show("Chưa có thư mục con nào. Hãy tạo thư mục trước!", "Thông báo");
                return;
            }

            var dlg = new Window
            {
                Title = $"Di chuyển '{file.Name}'",
                Width = 350, Height = 380,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250))
            };

            var sp = new StackPanel { Margin = new Thickness(16) };
            sp.Children.Add(new TextBlock { Text = "📦 Chọn thư mục đích:", FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });

            var lb = new ListBox { Height = 240, FontSize = 12 };
            lb.Items.Add("🏠 Thư viện (gốc)");
            foreach (var sf in subfolders)
            {
                var rel = sf.Replace(_rootPath, "").TrimStart(Path.DirectorySeparatorChar);
                lb.Items.Add($"📁 {rel}");
            }
            sp.Children.Add(lb);

            var btnMove = new Button
            {
                Content = "✅ Di chuyển", Margin = new Thickness(0, 10, 0, 0),
                Padding = new Thickness(12, 6, 12, 6), FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand
            };
            btnMove.Click += (s, e) =>
            {
                if (lb.SelectedIndex < 0) { MessageBox.Show("Chọn thư mục đích!"); return; }
                var destFolder = lb.SelectedIndex == 0 ? _rootPath : subfolders[lb.SelectedIndex - 1];
                var destPath = Path.Combine(destFolder, file.Name);

                if (File.Exists(destPath))
                {
                    MessageBox.Show($"File đã tồn tại tại thư mục đích!", "Lỗi");
                    return;
                }
                try
                {
                    File.Move(file.Path, destPath);
                    dlg.Close();
                    NavigateToFolder(_currentFolder);
                    MessageBox.Show($"✅ Đã di chuyển '{file.Name}' thành công!", "Di chuyển", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
            };
            sp.Children.Add(btnMove);

            dlg.Content = sp;
            dlg.ShowDialog();
        }

        private List<string> GetAllSubfolders(string rootPath)
        {
            var result = new List<string>();
            try
            {
                foreach (var dir in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories))
                    result.Add(dir);
            }
            catch { }
            return result;
        }

        private string? ShowInputDialog(string title, string prompt, string defaultValue = "")
        {
            var dlg = new Window
            {
                Title = title, Width = 360, Height = 170,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250))
            };
            var sp = new StackPanel { Margin = new Thickness(16) };
            sp.Children.Add(new TextBlock { Text = prompt, FontSize = 12, Margin = new Thickness(0, 0, 0, 8) });
            var tb = new TextBox { Text = defaultValue, FontSize = 13, Padding = new Thickness(6, 4, 6, 4) };
            sp.Children.Add(tb);

            string? result = null;
            var btnSp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var btnCancel = new Button { Content = "Hủy", Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(0, 0, 6, 0), Cursor = Cursors.Hand };
            btnCancel.Click += (s, e) => dlg.Close();
            btnSp.Children.Add(btnCancel);

            var btnOk = new Button
            {
                Content = "✅ Xác nhận", Padding = new Thickness(12, 5, 12, 5), Cursor = Cursors.Hand,
                Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)), Foreground = Brushes.White, BorderThickness = new Thickness(0)
            };
            btnOk.Click += (s, e) => { result = tb.Text; dlg.Close(); };
            btnSp.Children.Add(btnOk);
            sp.Children.Add(btnSp);

            dlg.Content = sp;
            tb.Focus(); tb.SelectAll();
            dlg.ShowDialog();
            return result;
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════

        private static string FormatSize(long bytes) =>
            bytes < 1024 ? $"{bytes} B" : bytes < 1048576 ? $"{bytes / 1024.0:F1} KB" : $"{bytes / 1048576.0:F1} MB";

        private static string GetIcon(string ext) => ext.ToLower() switch
        {
            ".pdf" => "📄", ".docx" or ".doc" => "📝", ".xlsx" or ".xls" => "📊",
            ".pptx" or ".ppt" => "📋", ".jpg" or ".png" or ".jpeg" or ".bmp" => "🖼️",
            ".mp4" or ".avi" or ".mov" => "🎬", ".zip" or ".rar" => "🗜️", _ => "📄"
        };

        private static string GetFileType(string ext) => ext.ToLower() switch
        {
            ".pdf" => "pdf", ".jpg" or ".png" or ".jpeg" or ".bmp" => "image",
            ".mp4" or ".avi" or ".mov" => "video",
            ".docx" or ".doc" or ".xlsx" or ".xls" or ".pptx" or ".ppt" or ".txt" => "document",
            _ => "other"
        };

        // ═══════════════════════════════════════════════════════════
        //  NEW HELPERS FOR UPDATED FEATURES
        // ═══════════════════════════════════════════════════════════

        private bool IsValidFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (name.Any(c => invalidChars.Contains(c))) return false;
            if (name.Contains("..") || name.Contains("/") || name.Contains("\\")) return false;
            return true;
        }

        private void VideoTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isUserDraggingSlider && previewVideo.NaturalDuration.HasTimeSpan)
            {
                double current = previewVideo.Position.TotalSeconds;
                double total = previewVideo.NaturalDuration.TimeSpan.TotalSeconds;
                sliderVideoProgress.Maximum = total;
                sliderVideoProgress.Value = current;
                txtVideoCurrentTime.Text = previewVideo.Position.ToString(@"mm\:ss");
                txtVideoTotalTime.Text = previewVideo.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
            }
        }

        private void SliderVideo_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isUserDraggingSlider = true;
        }

        private void SliderVideo_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isUserDraggingSlider = false;
            
            // Đảm bảo video đã tải xong thời lượng trước khi tua
            if (previewVideo.NaturalDuration.HasTimeSpan)
            {
                previewVideo.Position = TimeSpan.FromSeconds(sliderVideoProgress.Value);
            }
        }

        private void SliderVideo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUserDraggingSlider)
            {
                txtVideoCurrentTime.Text = TimeSpan.FromSeconds(sliderVideoProgress.Value).ToString(@"mm\:ss");
            }
        }

        public async Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
            {
                LoadLibrary();
                _isInitialized = true;
            }
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            StopVideo();
            if (_videoTimer != null)
            {
                _videoTimer.Stop();
                _videoTimer = null;
            }
            Log.Information("LibraryPage disposed successfully");
        }
    }

    public class LibraryItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime LastModified { get; set; } = DateTime.MinValue;
        public string Icon { get; set; } = "📁";
        public string Type { get; set; } = "other";
        public string Extension { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
    }
}
