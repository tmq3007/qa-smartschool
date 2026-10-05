using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.Data;
using QASmartClass.Classroom.Services;
using QASmartClass.Classroom.Helpers;
using QASmartTouch.Services;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class LessonEditorPage : Page
    {
        private int _blockCount = 0;
        private readonly Stack<System.Windows.UIElement> _undoStack = new();
        private readonly Stack<System.Windows.UIElement> _redoStack = new();
        private int _currentLessonId = 0; // 0 = new lesson
        public bool HasUnsavedChanges { get; set; } = false;
        private System.Windows.Threading.DispatcherTimer? _autoSaveTimer;

        public LessonEditorPage(int lessonId = 0)
        {
            InitializeComponent();
            UpdateUndoRedoButtonsState();
            _currentLessonId = lessonId;
            if (lessonId > 0) LoadLesson(lessonId);
            Loaded += (_, _) => LoadAiSuggestions();

            // Wire up dirty state tracking
            txtTitle.TextChanged += (_, _) => HasUnsavedChanges = true;
            txtDescription.TextChanged += (_, _) => HasUnsavedChanges = true;
            cmbSubject.SelectionChanged += (_, _) => HasUnsavedChanges = true;
            cmbGrade.SelectionChanged += (_, _) => HasUnsavedChanges = true;

            // Phase 2: Khởi tạo AutoSave Timer (mỗi 3 phút)
            _autoSaveTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(3)
            };
            _autoSaveTimer.Tick += AutoSave_Tick;
            _autoSaveTimer.Start();
        }


        // ═══════════════════════════════════════════════════════════
        //  RICH TEXT RENDERING — Hiển thị nội dung với highlight & format
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Render một content block đã lưu trong DB lên editor với rich formatting
        /// </summary>
        private void RenderContentBlock(string contentType, string data)
        {
            _blockCount++;
            int currentSort = _blockCount; // capture for closures

            var (label, bgHex, fgHex) = contentType switch
            {
                "Text"       => ("🔤 Văn bản",      "#E3F2FD", "#1565C0"),
                "Image"      => ("🖼️ Hình ảnh",     "#E8F5E9", "#2E7D32"),
                "Video"      => ("🎬 Video",         "#FFF3E0", "#E65100"),
                "Simulation" => ("🔬 Mô phỏng PhET", "#F3E5F5", "#7B1FA2"),
                "PDF"        => ("📄 Tài liệu PDF",  "#FFEBEE", "#C62828"),
                "Quiz"       => ("❓ Câu hỏi Quiz",  "#E0F2F1", "#00695C"),
                _            => ("📝 Nội dung",      "#F5F5F5", "#424242")
            };

            var bg = (SolidColorBrush)new BrushConverter().ConvertFrom(bgHex)!;
            var fg = (SolidColorBrush)new BrushConverter().ConvertFrom(fgHex)!;

            var blockBorder = new Border
            {
                Background = Brushes.White, CornerRadius = new CornerRadius(10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(16, 14, 16, 14),
                Tag = currentSort // â† track sort order
            };

            var outerStack = new StackPanel();

            // ── Badge row ──
            var headerRow = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            var badge = new Border { Background = bg, CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 4, 8, 4) };
            badge.Child = new TextBlock { Text = label, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = fg };
            DockPanel.SetDock(badge, Dock.Left);
            headerRow.Children.Add(badge);

            // ── Action buttons panel (RIGHT side) ──
            var actionsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            // 📡 Focus HS — broadcast to all students
            var focusBtn = new Button
            {
                Content = "🎯 Focus HS", FontSize = 10, Padding = new Thickness(8, 3, 8, 3),
                Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 0), ToolTip = "Yêu cầu tất cả HS tập trung vào nội dung này"
            };
            focusBtn.Click += async (s, e2) =>
            {
                try
                {
                    // app → ClassroomAppContext (refactored)
                    var net = ClassroomAppContext.Network;
                    QASmartTouch.App.FocusState.ActiveFocusSort = currentSort;
                    QASmartTouch.App.FocusState.ActiveFocusType = contentType;

                    // Gửi qua network hoặc local bus
                    if (net?.IsBroadcasting == true)
                    {
                        await net.FocusContentAsync(currentSort, contentType);

                        // ═══ ALSO send rich content JSON to web students ═══
                        try
                        {
                            var bridge = net.WebBridge;
                            if (bridge?.IsRunning == true)
                            {
                                var lessonTitle = txtTitle.Text ?? "";
                                var lessonSubject = (cmbSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                                var contentPayload = System.Text.Json.JsonSerializer.Serialize(new
                                {
                                    type = "lesson_content",
                                    action = "LESSON_FOCUS",
                                    sortOrder = currentSort,
                                    contentType = contentType,
                                    content = data,
                                    lessonTitle = lessonTitle,
                                    lessonSubject = lessonSubject
                                });
                                _ = bridge.BroadcastJsonToWebClients(contentPayload);
                                Log.Information("EditorPage: Sent lesson content to web students: sort={Sort}, type={Type}, len={Len}",
                                    currentSort, contentType, data?.Length ?? 0);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("EditorPage: Failed to send lesson content to web: {Err}", ex.Message);
                        }
                    }
                    else
                        ClassroomAppContext.DispatchCommand($"CMD|LESSON_FOCUS|{currentSort}|{contentType}");

                    // ═══ GV SIDE: Zoom block focus + mờ các block khác (từ AppSettings) ═══
                    var zoomScale = AppSettings.FocusZoomScale;
                    var dimOpacity = AppSettings.FocusDimOpacity;

                    foreach (var child in blocksPanel.Children)
                    {
                        if (child is Border b && b.Tag is int bTag)
                        {
                            if (bTag == currentSort)
                            {
                                b.BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                                b.BorderThickness = new Thickness(4);
                                b.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                                b.Opacity = 1.0;
                                b.Padding = new Thickness(28, 22, 28, 22);
                                b.Margin = new Thickness(0, 20, 0, 20);
                                b.LayoutTransform = new ScaleTransform(zoomScale, zoomScale);
                                b.Effect = new System.Windows.Media.Effects.DropShadowEffect
                                {
                                    BlurRadius = 30, ShadowDepth = 5,
                                    Color = Color.FromRgb(25, 118, 210), Opacity = 0.45
                                };
                                b.BringIntoView();
                            }
                            else
                            {
                                b.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                                b.BorderThickness = new Thickness(1);
                                b.Background = Brushes.White;
                                b.Opacity = dimOpacity;
                                b.Effect = null;
                                b.LayoutTransform = null;
                                b.Padding = new Thickness(16, 14, 16, 14);
                                b.Margin = new Thickness(0, 0, 0, 10);
                            }
                        }
                    }

                    focusBtn.Content = "✅ Đang Focus";
                    focusBtn.IsEnabled = false;
                }
                catch (Exception ex) { Log.Warning("Focus broadcast error: {Err}", ex.Message); }
            };
            actionsPanel.Children.Add(focusBtn);

            // ❌ Unfocus — bỏ focus, khôi phục tất cả block
            var unfocusBtn = new Button
            {
                Content = "❌ Unfocus", FontSize = 10, Padding = new Thickness(8, 3, 8, 3),
                Background = new SolidColorBrush(Color.FromRgb(255, 235, 238)),
                Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 0), ToolTip = "Bỏ Focus — khôi phục hiển thị bình thường"
            };
            unfocusBtn.Click += (s, e2) =>
            {
                try
                {
                    // Gửi lệnh UNFOCUS cho HS
                    // app → ClassroomAppContext (refactored)
                    var net = ClassroomAppContext.Network;
                    var cmd = "CMD|LESSON_UNFOCUS";
                    ClassroomAppContext.LessonState.LastTeacherCommand = cmd;
                    ClassroomAppContext.LessonState.LastCommandTime = DateTime.Now;
                    QASmartTouch.App.FocusState.ActiveFocusSort = -1;
                    QASmartTouch.App.FocusState.ActiveFocusType = string.Empty;
                    if (net?.IsBroadcasting == true)
                        _ = net.SendCommandAsync(cmd);
                    else
                        ClassroomAppContext.DispatchCommand(cmd);

                    // ═══ Khôi phục tất cả block GV ═══
                    ResetAllBlocksFocus();

                    Log.Information("Teacher unfocused all blocks");
                }
                catch (Exception ex) { Log.Warning("Unfocus error: {Err}", ex.Message); }
            };
            actionsPanel.Children.Add(unfocusBtn);

            // 🖊️ Chuyển bảng trắng — chụp block focus & dán lên SmartScreen
            var boardBtn = new Button
            {
                Content = "🖊️ Bảng trắng", FontSize = 10, Padding = new Thickness(8, 3, 8, 3),
                Background = new SolidColorBrush(Color.FromRgb(243, 229, 245)),
                Foreground = new SolidColorBrush(Color.FromRgb(123, 31, 162)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 0), ToolTip = "Chụp nội dung này & dán lên Bảng trắng SmartScreen"
            };
            boardBtn.Click += (s, e2) =>
            {
                try
                {
                    // 1. Tìm SmartScreen window & canvas
                    Window? targetWin = null;
                    foreach (Window win in Application.Current.Windows)
                    {
                        var typeName = win.GetType().Name;
                        if (typeName.Contains("MainDashboard") || typeName.Contains("Form2"))
                        {
                            targetWin = win;
                            break;
                        }
                    }

                    if (targetWin == null)
                    {
                        ClassroomDialog.Info("Chưa mở SmartScreen. Hãy mở SmartScreen trước.", "Bảng Trắng");
                        return;
                    }

                    var canvas = targetWin.FindName("MainInteractiveBoard") as Canvas;
                    if (canvas == null)
                    {
                        ClassroomDialog.Warn("Không tìm thấy canvas SmartScreen.", "Bảng Trắng");
                        return;
                    }

                    // ═══ SPECIAL HANDLING: Image blocks → load actual image file ═══
                    if (contentType == "Image" && System.IO.File.Exists(data))
                    {
                        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(data, UriKind.Absolute);
                        bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        // Tính kích thước phù hợp canvas
                        double maxW = canvas.ActualWidth * 0.7;
                        double maxH = canvas.ActualHeight * 0.7;
                        double imgW = bitmap.PixelWidth;
                        double imgH = bitmap.PixelHeight;
                        double ratio = Math.Min(maxW / imgW, maxH / imgH);
                        if (ratio < 1) { imgW *= ratio; imgH *= ratio; }

                        var container = BuildImageOnCanvas(bitmap, imgW, imgH, canvas);

                        double left = Math.Max(20, (canvas.ActualWidth - imgW) / 2);
                        double top = Math.Max(40, (canvas.ActualHeight - imgH) / 2);
                        Canvas.SetLeft(container, left);
                        Canvas.SetTop(container, top);
                        canvas.Children.Add(container);

                        targetWin.Show();
                        targetWin.Activate();
                        targetWin.WindowState = WindowState.Maximized;

                        Log.Information("Image block placed on SmartScreen: {Path}", data);
                        return;
                    }

                    // ═══ SPECIAL HANDLING: PDF blocks → WebView2 viewer ═══
                    if (contentType == "PDF" && System.IO.File.Exists(data))
                    {
                        double viewerW = Math.Min(canvas.ActualWidth * 0.8, 900);
                        double viewerH = Math.Min(canvas.ActualHeight * 0.9, 700);
                        var pdfViewer = BuildPdfViewerOnCanvas(data, viewerW, viewerH, canvas);
                        double left = Math.Max(10, (canvas.ActualWidth - viewerW) / 2);
                        double top = Math.Max(10, (canvas.ActualHeight - viewerH) / 2);
                        Canvas.SetLeft(pdfViewer, left);
                        Canvas.SetTop(pdfViewer, top);
                        canvas.Children.Add(pdfViewer);

                        targetWin.Show();
                        targetWin.Activate();
                        targetWin.WindowState = WindowState.Maximized;
                        Log.Information("PDF viewer placed on SmartScreen: {Path}", data);
                        return;
                    }

                    // ═══ SPECIAL HANDLING: Simulation blocks → WebView2 viewer ═══
                    if (contentType == "Simulation" && !string.IsNullOrEmpty(data) && data.StartsWith("http"))
                    {
                        double viewerW = Math.Min(canvas.ActualWidth * 0.85, 1000);
                        double viewerH = Math.Min(canvas.ActualHeight * 0.9, 750);
                        var simViewer = BuildSimViewerOnCanvas(data, "PhET Simulation", viewerW, viewerH, canvas);
                        double left = Math.Max(10, (canvas.ActualWidth - viewerW) / 2);
                        double top = Math.Max(10, (canvas.ActualHeight - viewerH) / 2);
                        Canvas.SetLeft(simViewer, left);
                        Canvas.SetTop(simViewer, top);
                        canvas.Children.Add(simViewer);

                        targetWin.Show();
                        targetWin.Activate();
                        targetWin.WindowState = WindowState.Maximized;
                        Log.Information("Simulation viewer placed on SmartScreen: {Url}", data);
                        return;
                    }

                    // ═══ SPECIAL HANDLING: Quiz blocks → Interactive quiz card ═══
                    if (contentType == "Quiz" && !string.IsNullOrEmpty(data) && data.Contains(";;"))
                    {
                        double cardW = Math.Min(canvas.ActualWidth * 0.6, 700);
                        var quizCard = BuildQuizCardOnCanvas(data, cardW, canvas);
                        double left = Math.Max(20, (canvas.ActualWidth - cardW) / 2);
                        double top = Math.Max(20, (canvas.ActualHeight - 500) / 2);
                        Canvas.SetLeft(quizCard, left);
                        Canvas.SetTop(quizCard, top);
                        canvas.Children.Add(quizCard);

                        targetWin.Show();
                        targetWin.Activate();
                        targetWin.WindowState = WindowState.Maximized;
                        Log.Information("Quiz card placed on SmartScreen");
                        return;
                    }

                    // ═══ DEFAULT: Render text/other content as bitmap ═══
                    double renderWidth = Math.Min(900, canvas.ActualWidth * 0.75);
                    var fullContent = BuildWhiteboardContent(contentType, data, renderWidth);

                    fullContent.Measure(new Size(renderWidth, double.PositiveInfinity));
                    fullContent.Arrange(new Rect(0, 0, fullContent.DesiredSize.Width, fullContent.DesiredSize.Height));
                    fullContent.UpdateLayout();

                    var actualW = fullContent.ActualWidth;
                    var actualH = fullContent.ActualHeight;
                    if (actualW < 1 || actualH < 1) return;

                    double scale = 2.0;
                    var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)(actualW * scale), (int)(actualH * scale),
                        96 * scale, 96 * scale, PixelFormats.Pbgra32);
                    rtb.Render(fullContent);

                    double outW = Math.Min(actualW, canvas.ActualWidth * 0.8);
                    double outH = actualH * (outW / actualW);

                    var textContainer = BuildInteractiveImageContainer(rtb, outW, outH, canvas);

                    double tleft = Math.Max(20, (canvas.ActualWidth - outW) / 2);
                    double ttop = Math.Max(40, (canvas.ActualHeight - outH) / 2);
                    Canvas.SetLeft(textContainer, tleft);
                    Canvas.SetTop(textContainer, ttop);
                    canvas.Children.Add(textContainer);

                    targetWin.Show();
                    targetWin.Activate();
                    targetWin.WindowState = WindowState.Maximized;

                    Log.Information("Block #{Sort} full content placed on SmartScreen", currentSort);
                }
                catch (Exception ex)
                {
                    Log.Warning("Board capture error: {Err}", ex.Message);
                    ClassroomDialog.Warn($"Lỗi: {ex.Message}", "Bảng Trắng");
                }
            };
            actionsPanel.Children.Add(boardBtn);

            // 📺 Trình chiếu — broadcast to SmartScreen
            var screenBtn = new Button
            {
                Content = "📺 Chiếu", FontSize = 10, Padding = new Thickness(8, 3, 8, 3),
                Background = new SolidColorBrush(Color.FromRgb(227, 242, 253)),
                Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 0), ToolTip = "Phát nội dung này lên SmartScreen"
            };
            screenBtn.Click += (s, e2) =>
            {
                ClassroomDialog.Info($"📺 Đã phát nội dung [{label}] lên SmartScreen!\n\nHS sẽ thấy nội dung này trên màn hình lớn.", "Trình chiếu");
                Log.Information("Content broadcast to SmartScreen: Block {Sort} ({Type})", currentSort, contentType);
            };
            actionsPanel.Children.Add(screenBtn);

            // ✏️ Edit — chỉnh sửa nội dung block trực tiếp
            string currentData = data; // mutable copy for editing
            var editBtn = new Button
            {
                Content = "✏️ Sửa", FontSize = 10, Padding = new Thickness(8, 3, 8, 3),
                Background = new SolidColorBrush(Color.FromRgb(255, 249, 196)),
                Foreground = new SolidColorBrush(Color.FromRgb(245, 127, 23)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 0), ToolTip = "Chỉnh sửa nội dung block này"
            };
            // Done button — hidden until editing
            var doneBtn = new Button
            {
                Content = "✅ Xong", FontSize = 10, Padding = new Thickness(8, 3, 8, 3),
                Background = new SolidColorBrush(Color.FromRgb(200, 230, 201)),
                Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 0), Visibility = Visibility.Collapsed,
                ToolTip = "Lưu chỉnh sửa"
            };
            // Cancel edit button
            var cancelEditBtn = new Button
            {
                Content = "↩️ Hủy", FontSize = 10, Padding = new Thickness(8, 3, 8, 3),
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 0), Visibility = Visibility.Collapsed,
                ToolTip = "Hủy chỉnh sửa"
            };

            TextBox? editTextBox = null;
            UIElement? savedRichContent = null;

            editBtn.Click += (s, e2) =>
            {
                // Chuyển sang chế độ sửa: ẩn rich content → hiện TextBox
                if (outerStack.Children.Count > 1)
                {
                    savedRichContent = outerStack.Children[outerStack.Children.Count - 1];
                    outerStack.Children.RemoveAt(outerStack.Children.Count - 1);
                }

                editTextBox = new TextBox
                {
                    Text = currentData.Replace("\\n", "\n"),
                    AcceptsReturn = true, AcceptsTab = true,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13, FontFamily = new FontFamily("Segoe UI"),
                    MinHeight = 120, MaxHeight = 500,
                    Padding = new Thickness(12, 10, 12, 10),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                    BorderThickness = new Thickness(2),
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = new SolidColorBrush(Color.FromRgb(250, 252, 255))
                };
                // Round corners for edit box
                editTextBox.Loaded += (_, _) =>
                {
                    editTextBox.Focus();
                    editTextBox.SelectAll();
                };

                outerStack.Children.Add(editTextBox);

                editBtn.Visibility = Visibility.Collapsed;
                doneBtn.Visibility = Visibility.Visible;
                cancelEditBtn.Visibility = Visibility.Visible;
                blockBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                blockBorder.BorderThickness = new Thickness(2);
                blockBorder.Background = new SolidColorBrush(Color.FromRgb(250, 252, 255));
            };

            doneBtn.Click += (s, e2) =>
            {
                if (editTextBox == null) return;

                // Lấy nội dung mới
                currentData = editTextBox.Text.Replace("\r\n", "\n");

                // Xóa TextBox, render lại rich content
                outerStack.Children.Remove(editTextBox);
                var newRichContent = BuildRichTextContent(currentData);
                outerStack.Children.Add(newRichContent);

                // Ẩn Done/Cancel, hiện Edit
                editBtn.Visibility = Visibility.Visible;
                doneBtn.Visibility = Visibility.Collapsed;
                cancelEditBtn.Visibility = Visibility.Collapsed;
                blockBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                blockBorder.BorderThickness = new Thickness(1);
                blockBorder.Background = Brushes.White;

                // Cập nhật DB
                try
                {
                    // app → ClassroomAppContext (refactored)
                    if (_currentLessonId > 0)
                    {
                        var dbContent = ClassroomAppContext.Db.LessonContents
                            .Where(c => c.LessonId == _currentLessonId && c.SortOrder == (currentSort - 1))
                            .FirstOrDefault();
                        if (dbContent != null)
                        {
                            dbContent.Data = currentData;
                            ClassroomAppContext.Db.SaveChanges();
                            Log.Information("Block #{Sort} content updated in DB", currentSort);
                        }
                    }
                }
                catch (Exception ex) { Log.Warning("Save block edit error: {Err}", ex.Message); }

                editTextBox = null;
                savedRichContent = null;
            };

            cancelEditBtn.Click += (s, e2) =>
            {
                // Hủy sửa: xóa TextBox, khôi phục rich content cũ
                if (editTextBox != null)
                    outerStack.Children.Remove(editTextBox);

                if (savedRichContent != null)
                    outerStack.Children.Add(savedRichContent);

                editBtn.Visibility = Visibility.Visible;
                doneBtn.Visibility = Visibility.Collapsed;
                cancelEditBtn.Visibility = Visibility.Collapsed;
                blockBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                blockBorder.BorderThickness = new Thickness(1);
                blockBorder.Background = Brushes.White;
                editTextBox = null;
                savedRichContent = null;
            };

            actionsPanel.Children.Add(editBtn);
            actionsPanel.Children.Add(doneBtn);
            actionsPanel.Children.Add(cancelEditBtn);

            // 🗑️ Delete
            var delBtn = new Button
            {
                Content = "🗑️", FontSize = 14, Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Top, Padding = new Thickness(4),
                ToolTip = "Xóa block"
            };
            delBtn.Click += (s, e2) => { blocksPanel.Children.Remove(blockBorder); _blockCount--; if (_blockCount == 0) emptyPlaceholder.Visibility = Visibility.Visible; };
            actionsPanel.Children.Add(delBtn);

            DockPanel.SetDock(actionsPanel, Dock.Right);
            headerRow.Children.Add(actionsPanel);
            outerStack.Children.Add(headerRow);

            // ── Content area — Rich formatted text ──
            var textContent = data.Replace("\\n", "\n");
            var richContent = BuildRichTextContent(textContent);
            outerStack.Children.Add(richContent);

            blockBorder.Child = outerStack;
            blocksPanel.Children.Add(blockBorder);
            OnBlockAdded();
        }

        /// <summary>Khôi phục tất cả block về trạng thái bình thường (bỏ focus)</summary>
        private void ResetAllBlocksFocus()
        {
            foreach (var child in blocksPanel.Children)
            {
                if (child is Border b && b.Tag is int)
                {
                    b.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                    b.BorderThickness = new Thickness(1);
                    b.Background = Brushes.White;
                    b.Opacity = 1.0;
                    b.Effect = null;
                    b.LayoutTransform = null;
                    b.Padding = new Thickness(16, 14, 16, 14);
                    b.Margin = new Thickness(0, 0, 0, 10);

                    // Re-enable Focus buttons inside this block
                    if (b.Child is StackPanel outerStack)
                    {
                        foreach (var row in outerStack.Children)
                        {
                            if (row is DockPanel dp)
                            {
                                foreach (var el in dp.Children)
                                {
                                    if (el is StackPanel ap)
                                    {
                                        foreach (var btn in ap.Children)
                                        {
                                            if (btn is Button fb && fb.Content?.ToString()?.Contains("Focus") == true)
                                            {
                                                fb.Content = "🎯 Focus HS";
                                                fb.IsEnabled = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Render nội dung đầy đủ off-screen cho chụp ảnh bảng trắng
        /// </summary>
        private static Border BuildWhiteboardContent(string contentType, string data, double maxWidth)
        {
            var textContent = data.Replace("\\n", "\n");

            var (label, bgHex, fgHex) = contentType switch
            {
                "Text"       => ("📄 Văn bản",       "#E3F2FD", "#1565C0"),
                "Image"      => ("🖼️ Hình ảnh",      "#E8F5E9", "#2E7D32"),
                "Video"      => ("🎬 Video",          "#FFF3E0", "#E65100"),
                "Simulation" => ("🔬 Mô phỏng PhET",  "#F3E5F5", "#7B1FA2"),
                "PDF"        => ("📄 Tài liệu PDF",   "#FFEBEE", "#C62828"),
                "Quiz"       => ("❓ Câu hỏi Quiz",   "#E0F2F1", "#00695C"),
                _            => ("📝 Nội dung",       "#F5F5F5", "#424242")
            };

            var headerBg = (SolidColorBrush)new BrushConverter().ConvertFrom(bgHex)!;
            var headerFg = (SolidColorBrush)new BrushConverter().ConvertFrom(fgHex)!;

            var headerBorder = new Border
            {
                Background = headerBg, CornerRadius = new CornerRadius(6, 6, 0, 0),
                Padding = new Thickness(16, 10, 16, 10)
            };
            headerBorder.Child = new TextBlock
            {
                Text = label, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = headerFg
            };

            var lines = textContent.Split('\n');
            var contentStack = new StackPanel { Margin = new Thickness(20, 16, 20, 20) };

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var tb = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8),
                    MaxWidth = maxWidth - 60
                };

                if (line == line.ToUpper() && line.Length > 3)
                {
                    tb.FontSize = 20; tb.FontWeight = FontWeights.Bold;
                    tb.Foreground = new SolidColorBrush(Color.FromRgb(26, 35, 126));
                    tb.Margin = new Thickness(0, 12, 0, 8);
                }
                else if (line.StartsWith("•") || line.StartsWith("-") || line.StartsWith("✔") || line.StartsWith("→"))
                {
                    tb.FontSize = 16; tb.Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79));
                    tb.Margin = new Thickness(16, 0, 0, 6);
                }
                else
                {
                    tb.FontSize = 17; tb.Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33));
                }

                tb.Text = line;
                contentStack.Children.Add(tb);
            }

            var outerStack = new StackPanel();
            outerStack.Children.Add(headerBorder);
            outerStack.Children.Add(contentStack);

            return new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(2),
                Width = maxWidth,
                Child = outerStack
            };
        }

        /// <summary>
        /// Bọc ảnh trong container có toolbar: ✋ Di chuyển, 📋 Copy, 🗑 Xóa
        /// </summary>
        private static Grid BuildInteractiveImageContainer(
            System.Windows.Media.Imaging.RenderTargetBitmap rtb,
            double imgWidth, double imgHeight, Canvas canvas)
        {
            var imgElement = new System.Windows.Controls.Image
            {
                Source = rtb, Width = imgWidth, Height = imgHeight,
                Stretch = System.Windows.Media.Stretch.Uniform
            };

            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 8, 0), Opacity = 0
            };

            var btnStyle = new Style(typeof(Button));
            btnStyle.Setters.Add(new Setter(Button.FontSizeProperty, 12.0));
            btnStyle.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(8, 4, 8, 4)));
            btnStyle.Setters.Add(new Setter(Button.CursorProperty, Cursors.Hand));
            btnStyle.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(0)));
            btnStyle.Setters.Add(new Setter(Button.MarginProperty, new Thickness(2, 0, 2, 0)));

            var moveBtn = new Button { Content = "✋ Di chuyển", Background = new SolidColorBrush(Color.FromArgb(220, 33, 150, 243)), Foreground = Brushes.White, Style = btnStyle };
            var copyBtn = new Button { Content = "📋 Copy", Background = new SolidColorBrush(Color.FromArgb(220, 76, 175, 80)), Foreground = Brushes.White, Style = btnStyle };
            var delBtn = new Button { Content = "🗑 Xóa", Background = new SolidColorBrush(Color.FromArgb(220, 229, 57, 53)), Foreground = Brushes.White, Style = btnStyle };

            toolbar.Children.Add(moveBtn);
            toolbar.Children.Add(copyBtn);
            toolbar.Children.Add(delBtn);

            var container = new Grid { Width = imgWidth, Tag = "WhiteboardImage" };
            container.Children.Add(imgElement);
            container.Children.Add(toolbar);

            bool isDragging = false;
            Point dragStart = new Point();

            container.MouseEnter += (s, e) => { toolbar.Opacity = 1; container.Cursor = Cursors.SizeAll; };
            container.MouseLeave += (s, e) => { if (!isDragging) toolbar.Opacity = 0; container.Cursor = Cursors.Arrow; };

            container.MouseLeftButtonDown += (s, e) => { isDragging = true; dragStart = e.GetPosition(canvas); container.CaptureMouse(); e.Handled = true; };
            container.MouseMove += (s, e) =>
            {
                if (!isDragging) return;
                var pos = e.GetPosition(canvas);
                double curLeft = Canvas.GetLeft(container); if (double.IsNaN(curLeft)) curLeft = 0;
                double curTop = Canvas.GetTop(container); if (double.IsNaN(curTop)) curTop = 0;
                Canvas.SetLeft(container, curLeft + pos.X - dragStart.X);
                Canvas.SetTop(container, curTop + pos.Y - dragStart.Y);
                dragStart = pos; e.Handled = true;
            };
            container.MouseLeftButtonUp += (s, e) => { isDragging = false; container.ReleaseMouseCapture(); e.Handled = true; };

            copyBtn.Click += (s, e) =>
            {
                var clone = BuildInteractiveImageContainer(rtb, imgWidth, imgHeight, canvas);
                double cl = Canvas.GetLeft(container); if (double.IsNaN(cl)) cl = 100;
                double ct = Canvas.GetTop(container); if (double.IsNaN(ct)) ct = 100;
                Canvas.SetLeft(clone, cl + 30); Canvas.SetTop(clone, ct + 30);
                canvas.Children.Add(clone); e.Handled = true;
            };

            delBtn.Click += (s, e) => { canvas.Children.Remove(container); e.Handled = true; };

            return container;
        }

        /// <summary>
        /// Tạo container ảnh thật trên SmartScreen canvas — di chuyển, phóng to/thu nhỏ, copy, xóa
        /// </summary>
        private static Grid BuildImageOnCanvas(
            System.Windows.Media.Imaging.BitmapSource bitmapSource,
            double imgWidth, double imgHeight, Canvas canvas)
        {
            var imgElement = new System.Windows.Controls.Image
            {
                Source = bitmapSource, Width = imgWidth, Height = imgHeight,
                Stretch = System.Windows.Media.Stretch.Uniform
            };

            // ── Toolbar (hiện khi hover) ──
            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 8, 0), Opacity = 0
            };

            var btnStyle = new Style(typeof(Button));
            btnStyle.Setters.Add(new Setter(Button.FontSizeProperty, 12.0));
            btnStyle.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(8, 4, 8, 4)));
            btnStyle.Setters.Add(new Setter(Button.CursorProperty, Cursors.Hand));
            btnStyle.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(0)));
            btnStyle.Setters.Add(new Setter(Button.MarginProperty, new Thickness(2, 0, 2, 0)));

            var zoomInBtn  = new Button { Content = "🔍+", Background = new SolidColorBrush(Color.FromArgb(220, 255, 152, 0)), Foreground = Brushes.White, Style = btnStyle, ToolTip = "Phóng to" };
            var zoomOutBtn = new Button { Content = "🔍−", Background = new SolidColorBrush(Color.FromArgb(220, 255, 152, 0)), Foreground = Brushes.White, Style = btnStyle, ToolTip = "Thu nhỏ" };
            var moveBtn = new Button { Content = "✋ Di chuyển", Background = new SolidColorBrush(Color.FromArgb(220, 33, 150, 243)), Foreground = Brushes.White, Style = btnStyle };
            var copyBtn = new Button { Content = "📋 Copy", Background = new SolidColorBrush(Color.FromArgb(220, 76, 175, 80)), Foreground = Brushes.White, Style = btnStyle };
            var delBtn = new Button { Content = "🗑 Xóa", Background = new SolidColorBrush(Color.FromArgb(220, 229, 57, 53)), Foreground = Brushes.White, Style = btnStyle };

            toolbar.Children.Add(zoomInBtn);
            toolbar.Children.Add(zoomOutBtn);
            toolbar.Children.Add(moveBtn);
            toolbar.Children.Add(copyBtn);
            toolbar.Children.Add(delBtn);

            // ── Border viền ảnh ──
            var imgBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 25, 118, 210)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                Background = Brushes.White,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 12, ShadowDepth = 3, Opacity = 0.25,
                    Color = Colors.Black
                },
                Child = imgElement
            };

            var container = new Grid { Width = imgWidth + 4, Tag = "WhiteboardImage" };
            container.Children.Add(imgBorder);
            container.Children.Add(toolbar);

            // ── Zoom state ──
            double currentScale = 1.0;
            const double zoomStep = 0.15;
            const double minScale = 0.2;
            const double maxScale = 5.0;

            void ApplyZoom(double newScale)
            {
                currentScale = Math.Max(minScale, Math.Min(maxScale, newScale));
                container.LayoutTransform = new ScaleTransform(currentScale, currentScale);
            }

            zoomInBtn.Click += (s, e) => { ApplyZoom(currentScale + zoomStep); e.Handled = true; };
            zoomOutBtn.Click += (s, e) => { ApplyZoom(currentScale - zoomStep); e.Handled = true; };

            // Mouse wheel zoom (Ctrl + scroll)
            container.MouseWheel += (s, e) =>
            {
                double delta = e.Delta > 0 ? zoomStep : -zoomStep;
                ApplyZoom(currentScale + delta);
                e.Handled = true;
            };

            // ── Drag to move ──
            bool isDragging = false;
            Point dragStart = new Point();

            container.MouseEnter += (s, e) =>
            {
                toolbar.Opacity = 1;
                container.Cursor = Cursors.SizeAll;
                imgBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(180, 25, 118, 210));
            };
            container.MouseLeave += (s, e) =>
            {
                if (!isDragging) toolbar.Opacity = 0;
                container.Cursor = Cursors.Arrow;
                imgBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(80, 25, 118, 210));
            };

            container.MouseLeftButtonDown += (s, e) =>
            {
                isDragging = true;
                dragStart = e.GetPosition(canvas);
                container.CaptureMouse();
                e.Handled = true;
            };
            container.MouseMove += (s, e) =>
            {
                if (!isDragging) return;
                var pos = e.GetPosition(canvas);
                double curLeft = Canvas.GetLeft(container); if (double.IsNaN(curLeft)) curLeft = 0;
                double curTop = Canvas.GetTop(container); if (double.IsNaN(curTop)) curTop = 0;
                Canvas.SetLeft(container, curLeft + pos.X - dragStart.X);
                Canvas.SetTop(container, curTop + pos.Y - dragStart.Y);
                dragStart = pos;
                e.Handled = true;
            };
            container.MouseLeftButtonUp += (s, e) =>
            {
                isDragging = false;
                container.ReleaseMouseCapture();
                e.Handled = true;
            };

            // ── Copy ──
            copyBtn.Click += (s, e) =>
            {
                var clone = BuildImageOnCanvas(bitmapSource, imgWidth, imgHeight, canvas);
                double cl = Canvas.GetLeft(container); if (double.IsNaN(cl)) cl = 100;
                double ct = Canvas.GetTop(container); if (double.IsNaN(ct)) ct = 100;
                Canvas.SetLeft(clone, cl + 30);
                Canvas.SetTop(clone, ct + 30);
                canvas.Children.Add(clone);
                e.Handled = true;
            };

            // ── Delete ──
            delBtn.Click += (s, e) =>
            {
                canvas.Children.Remove(container);
                e.Handled = true;
            };

            AddResizeGrips(container, canvas);
            return container;
        }

        /// <summary>
        /// Tạo PDF Viewer trên SmartScreen canvas sử dụng WebView2
        /// Giống giao diện Thư viện tài nguyên: phân trang, zoom, tìm kiếm
        /// Có thể di chuyển, phóng to/thu nhỏ, xóa
        /// </summary>
        private static Grid BuildPdfViewerOnCanvas(string pdfPath, double viewerWidth, double viewerHeight, Canvas canvas)
        {
            var fileName = System.IO.Path.GetFileName(pdfPath);

            // ── WebView2 PDF viewer ──
            var webView = new Microsoft.Web.WebView2.Wpf.WebView2
            {
                Width = viewerWidth,
                Height = viewerHeight - 40 // trừ toolbar
            };

            // ── Top toolbar ──
            var toolbar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(40, 44, 52)),
                Height = 40, CornerRadius = new CornerRadius(6, 6, 0, 0)
            };
            var toolbarStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var tbStyle = new Style(typeof(Button));
            tbStyle.Setters.Add(new Setter(Button.FontSizeProperty, 12.0));
            tbStyle.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(8, 4, 8, 4)));
            tbStyle.Setters.Add(new Setter(Button.CursorProperty, Cursors.Hand));
            tbStyle.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(0)));
            tbStyle.Setters.Add(new Setter(Button.MarginProperty, new Thickness(2, 0, 2, 0)));
            tbStyle.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
            tbStyle.Setters.Add(new Setter(Button.BackgroundProperty, Brushes.Transparent));

            // 📄 File name label
            toolbarStack.Children.Add(new TextBlock
            {
                Text = $"📄 {fileName}", FontSize = 11, Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 16, 0), MaxWidth = viewerWidth * 0.35,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            // ✋ Di chuyển
            var moveBtn = new Button { Content = "✋ Di chuyển", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 33, 150, 243)), ToolTip = "Kéo thả di chuyển" };
            toolbarStack.Children.Add(moveBtn);

            // 🔍+ Phóng to
            var zoomInBtn = new Button { Content = "🔍+", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 255, 152, 0)), ToolTip = "Phóng to" };
            toolbarStack.Children.Add(zoomInBtn);

            // 🔍− Thu nhỏ
            var zoomOutBtn = new Button { Content = "🔍−", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 255, 152, 0)), ToolTip = "Thu nhỏ" };
            toolbarStack.Children.Add(zoomOutBtn);

            // Zoom label
            var zoomLabel = new TextBlock
            {
                Text = "100%", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(189, 189, 189)),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 8, 0)
            };
            toolbarStack.Children.Add(zoomLabel);

            // 📋 Copy
            var copyBtn = new Button { Content = "📋 Copy", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 76, 175, 80)) };
            toolbarStack.Children.Add(copyBtn);

            // ✂️ Chụp vùng (Snipping Tool)
            var snipBtn = new Button { Content = "✂️ Chụp vùng", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(200, 156, 39, 176)), ToolTip = "Chụp vùng nội dung PDF → đặt lên bảng trắng" };
            toolbarStack.Children.Add(snipBtn);

            // 🗑 Xóa
            var delBtn = new Button { Content = "🗑 Đóng", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 229, 57, 53)) };
            toolbarStack.Children.Add(delBtn);

            toolbar.Child = toolbarStack;

            // ── WebView border ──
            var webBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(40, 44, 52)),
                BorderThickness = new Thickness(2, 0, 2, 2),
                CornerRadius = new CornerRadius(0, 0, 6, 6),
                Child = webView
            };

            // ── Container layout ──
            var innerStack = new StackPanel();
            innerStack.Children.Add(toolbar);
            innerStack.Children.Add(webBorder);

            var container = new Grid
            {
                Width = viewerWidth + 4,
                Tag = "WhiteboardPdf",
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 20, ShadowDepth = 5, Opacity = 0.35, Color = Colors.Black
                }
            };
            container.Children.Add(innerStack);

            // ── Init WebView2 & navigate to PDF ──
            webView.Loaded += async (s, e) =>
            {
                try
                {
                    await webView.EnsureCoreWebView2Async();
                    webView.CoreWebView2.Navigate(new Uri(pdfPath).AbsoluteUri);
                }
                catch (Exception ex)
                {
                    Log.Warning("WebView2 PDF load error: {Err}", ex.Message);
                }
            };

            // ── Zoom state ──
            double currentZoom = 1.0;
            const double zoomStep = 0.25;

            void ApplyZoom(double newZoom)
            {
                currentZoom = Math.Max(0.25, Math.Min(5.0, newZoom));
                if (webView.CoreWebView2 != null)
                    webView.ZoomFactor = currentZoom;
                zoomLabel.Text = $"{(int)(currentZoom * 100)}%";
            }

            zoomInBtn.Click += (s, e) => { ApplyZoom(currentZoom + zoomStep); e.Handled = true; };
            zoomOutBtn.Click += (s, e) => { ApplyZoom(currentZoom - zoomStep); e.Handled = true; };

            // ── Drag to move ──
            bool isDragging = false;
            Point dragStart = new Point();

            moveBtn.PreviewMouseLeftButtonDown += (s, e) =>
            {
                isDragging = true;
                dragStart = e.GetPosition(canvas);
                container.CaptureMouse();
                e.Handled = true;
            };
            container.MouseMove += (s, e) =>
            {
                if (!isDragging) return;
                var pos = e.GetPosition(canvas);
                double curLeft = Canvas.GetLeft(container); if (double.IsNaN(curLeft)) curLeft = 0;
                double curTop = Canvas.GetTop(container); if (double.IsNaN(curTop)) curTop = 0;
                Canvas.SetLeft(container, curLeft + pos.X - dragStart.X);
                Canvas.SetTop(container, curTop + pos.Y - dragStart.Y);
                dragStart = pos;
                e.Handled = true;
            };
            container.MouseLeftButtonUp += (s, e) =>
            {
                if (isDragging) { isDragging = false; container.ReleaseMouseCapture(); e.Handled = true; }
            };

            // ── Copy ──
            copyBtn.Click += (s, e) =>
            {
                var clone = BuildPdfViewerOnCanvas(pdfPath, viewerWidth, viewerHeight, canvas);
                double cl = Canvas.GetLeft(container); if (double.IsNaN(cl)) cl = 50;
                double ct = Canvas.GetTop(container); if (double.IsNaN(ct)) ct = 50;
                Canvas.SetLeft(clone, cl + 40); Canvas.SetTop(clone, ct + 40);
                canvas.Children.Add(clone);
                e.Handled = true;
            };

            // ── ✂️ Snipping Tool — Chụp vùng nội dung PDF ──
            snipBtn.Click += async (s, e) =>
            {
                e.Handled = true;
                try
                {
                    if (webView.CoreWebView2 == null) return;

                    // 1) Chụp toàn bộ WebView2 thành bitmap
                    using var ms = new MemoryStream();
                    await webView.CoreWebView2.CapturePreviewAsync(
                        Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, ms);
                    ms.Position = 0;

                    var fullBitmap = new System.Windows.Media.Imaging.BitmapImage();
                    fullBitmap.BeginInit();
                    fullBitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    fullBitmap.StreamSource = ms;
                    fullBitmap.EndInit();
                    fullBitmap.Freeze();

                    // 2) Tạo overlay chọn vùng trên TOÀN BỘ canvas
                    var overlayCanvas = new Canvas
                    {
                        Background = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)),
                        Width = canvas.ActualWidth,
                        Height = canvas.ActualHeight,
                        Cursor = Cursors.Cross
                    };

                    // Hướng dẫn
                    var guide = new TextBlock
                    {
                        Text = "✂️ Kéo chuột để chọn vùng chụp  |  ESC để hủy",
                        FontSize = 16, Foreground = Brushes.White,
                        Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                        Padding = new Thickness(16, 8, 16, 8),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    Canvas.SetLeft(guide, (canvas.ActualWidth - 400) / 2);
                    Canvas.SetTop(guide, 20);
                    overlayCanvas.Children.Add(guide);

                    // Hiển thị preview ảnh PDF đã chụp ở vị trí WebView
                    var previewImg = new System.Windows.Controls.Image
                    {
                        Source = fullBitmap,
                        Width = webView.ActualWidth,
                        Height = webView.ActualHeight,
                        Stretch = System.Windows.Media.Stretch.Fill
                    };
                    // Lấy vị trí WebView relative to canvas
                    var webViewOrigin = webView.TranslatePoint(new Point(0, 0), canvas);
                    Canvas.SetLeft(previewImg, webViewOrigin.X);
                    Canvas.SetTop(previewImg, webViewOrigin.Y);
                    overlayCanvas.Children.Add(previewImg);

                    // Rectangle chọn vùng
                    var selRect = new System.Windows.Shapes.Rectangle
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection(new[] { 5.0, 3.0 }),
                        Fill = new SolidColorBrush(Color.FromArgb(40, 33, 150, 243)),
                        Visibility = Visibility.Collapsed
                    };
                    overlayCanvas.Children.Add(selRect);

                    canvas.Children.Add(overlayCanvas);

                    // 3) Drag logic
                    bool isSelecting = false;
                    Point selStart = new Point();
                    Rect selRegion = Rect.Empty;

                    overlayCanvas.MouseLeftButtonDown += (ss, ee) =>
                    {
                        isSelecting = true;
                        selStart = ee.GetPosition(overlayCanvas);
                        Canvas.SetLeft(selRect, selStart.X);
                        Canvas.SetTop(selRect, selStart.Y);
                        selRect.Width = 0; selRect.Height = 0;
                        selRect.Visibility = Visibility.Visible;
                        overlayCanvas.CaptureMouse();
                        ee.Handled = true;
                    };

                    overlayCanvas.MouseMove += (ss, ee) =>
                    {
                        if (!isSelecting) return;
                        var cur = ee.GetPosition(overlayCanvas);
                        double x = Math.Min(selStart.X, cur.X);
                        double y = Math.Min(selStart.Y, cur.Y);
                        double w = Math.Abs(cur.X - selStart.X);
                        double h = Math.Abs(cur.Y - selStart.Y);
                        Canvas.SetLeft(selRect, x); Canvas.SetTop(selRect, y);
                        selRect.Width = w; selRect.Height = h;
                        selRegion = new Rect(x, y, w, h);
                        ee.Handled = true;
                    };

                    overlayCanvas.MouseLeftButtonUp += (ss, ee) =>
                    {
                        if (!isSelecting) return;
                        isSelecting = false;
                        overlayCanvas.ReleaseMouseCapture();
                        ee.Handled = true;

                        // Tối thiểu 20x20 pixel
                        if (selRegion.Width < 20 || selRegion.Height < 20)
                        {
                            canvas.Children.Remove(overlayCanvas);
                            return;
                        }

                        // 4) Crop vùng chọn từ ảnh preview (tính tọa độ relative to previewImg)
                        try
                        {
                            double imgLeft = webViewOrigin.X;
                            double imgTop = webViewOrigin.Y;
                            double scaleX = fullBitmap.PixelWidth / webView.ActualWidth;
                            double scaleY = fullBitmap.PixelHeight / webView.ActualHeight;

                            int cropX = (int)((selRegion.X - imgLeft) * scaleX);
                            int cropY = (int)((selRegion.Y - imgTop) * scaleY);
                            int cropW = (int)(selRegion.Width * scaleX);
                            int cropH = (int)(selRegion.Height * scaleY);

                            // Clamp to bitmap bounds
                            cropX = Math.Max(0, cropX);
                            cropY = Math.Max(0, cropY);
                            cropW = Math.Min(cropW, fullBitmap.PixelWidth - cropX);
                            cropH = Math.Min(cropH, fullBitmap.PixelHeight - cropY);

                            if (cropW > 10 && cropH > 10)
                            {
                                var cropped = new System.Windows.Media.Imaging.CroppedBitmap(
                                    fullBitmap, new Int32Rect(cropX, cropY, cropW, cropH));
                                cropped.Freeze();

                                // 5) Đặt ảnh lên canvas
                                double dispW = Math.Min(selRegion.Width * 1.2, canvas.ActualWidth * 0.6);
                                double dispH = dispW * cropH / cropW;

                                var imgContainer = BuildImageOnCanvas(cropped, dispW, dispH, canvas);
                                Canvas.SetLeft(imgContainer, selRegion.X);
                                Canvas.SetTop(imgContainer, selRegion.Y);
                                canvas.Children.Add(imgContainer);

                                Log.Information("PDF snip captured: {W}x{H} → placed on canvas", cropW, cropH);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("PDF snip crop error: {Err}", ex.Message);
                        }

                        canvas.Children.Remove(overlayCanvas);
                    };

                    // ESC to cancel
                    overlayCanvas.KeyDown += (ss, ee) =>
                    {
                        if (ee.Key == System.Windows.Input.Key.Escape)
                        {
                            isSelecting = false;
                            overlayCanvas.ReleaseMouseCapture();
                            canvas.Children.Remove(overlayCanvas);
                            ee.Handled = true;
                        }
                    };
                    overlayCanvas.Focusable = true;
                    overlayCanvas.Focus();
                }
                catch (Exception ex)
                {
                    Log.Warning("PDF snip error: {Err}", ex.Message);
                    ClassroomDialog.Warn($"Không thể chụp vùng: {ex.Message}", "Lỗi");
                }
            };

            // ── Delete ──
            delBtn.Click += (s, e) =>
            {
                try { webView.Dispose(); } catch { }
                canvas.Children.Remove(container);
                e.Handled = true;
            };

            AddResizeGrips(container, canvas);
            return container;
        }

        /// <summary>
        /// Tạo Simulation Viewer (PhET) trên SmartScreen canvas sử dụng WebView2
        /// Có di chuyển, zoom, snip, copy, đóng
        /// </summary>
        private static Grid BuildSimViewerOnCanvas(string simUrl, string simTitle, double viewerWidth, double viewerHeight, Canvas canvas)
        {
            var webView = new Microsoft.Web.WebView2.Wpf.WebView2
            {
                Width = viewerWidth, Height = viewerHeight - 44
            };

            // ── Toolbar ──
            var toolbar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(74, 20, 140)), // deep purple
                Height = 44, CornerRadius = new CornerRadius(8, 8, 0, 0)
            };
            var toolbarStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var tbStyle = new Style(typeof(Button));
            tbStyle.Setters.Add(new Setter(Button.FontSizeProperty, 12.0));
            tbStyle.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(8, 4, 8, 4)));
            tbStyle.Setters.Add(new Setter(Button.CursorProperty, Cursors.Hand));
            tbStyle.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(0)));
            tbStyle.Setters.Add(new Setter(Button.MarginProperty, new Thickness(2, 0, 2, 0)));
            tbStyle.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
            tbStyle.Setters.Add(new Setter(Button.BackgroundProperty, Brushes.Transparent));

            toolbarStack.Children.Add(new TextBlock
            {
                Text = $"🔬 {simTitle}", FontSize = 12, Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 16, 0), MaxWidth = viewerWidth * 0.3,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            var moveBtn = new Button { Content = "✋ Di chuyển", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 33, 150, 243)) };
            var zoomInBtn = new Button { Content = "🔍+", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 255, 152, 0)), ToolTip = "Phóng to" };
            var zoomOutBtn = new Button { Content = "🔍−", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 255, 152, 0)), ToolTip = "Thu nhỏ" };
            var zoomLabel = new TextBlock { Text = "100%", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(206, 147, 216)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 8, 0) };
            var snipBtn = new Button { Content = "✂️ Chụp vùng", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(200, 156, 39, 176)), ToolTip = "Chụp vùng nội dung mô phỏng" };
            var copyBtn = new Button { Content = "📋 Copy", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 76, 175, 80)) };
            var delBtn = new Button { Content = "🗑 Đóng", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 229, 57, 53)) };

            toolbarStack.Children.Add(moveBtn);
            toolbarStack.Children.Add(zoomInBtn);
            toolbarStack.Children.Add(zoomOutBtn);
            toolbarStack.Children.Add(zoomLabel);
            toolbarStack.Children.Add(snipBtn);
            toolbarStack.Children.Add(copyBtn);
            toolbarStack.Children.Add(delBtn);
            toolbar.Child = toolbarStack;

            var webBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(74, 20, 140)),
                BorderThickness = new Thickness(2, 0, 2, 2),
                CornerRadius = new CornerRadius(0, 0, 8, 8),
                Child = webView
            };

            var innerStack = new StackPanel();
            innerStack.Children.Add(toolbar);
            innerStack.Children.Add(webBorder);

            var container = new Grid
            {
                Width = viewerWidth + 4, Tag = "WhiteboardSim",
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 24, ShadowDepth = 6, Opacity = 0.4, Color = Color.FromRgb(74, 20, 140) }
            };
            container.Children.Add(innerStack);

            // ── Init WebView2 ──
            webView.Loaded += async (s, e) =>
            {
                try
                {
                    await webView.EnsureCoreWebView2Async();
                    webView.CoreWebView2.Navigate(simUrl);
                }
                catch (Exception ex) { Log.Warning("WebView2 sim load error: {Err}", ex.Message); }
            };

            // ── Zoom ──
            double currentZoom = 1.0;
            void ApplyZoom(double nz) { currentZoom = Math.Max(0.25, Math.Min(5.0, nz)); if (webView.CoreWebView2 != null) webView.ZoomFactor = currentZoom; zoomLabel.Text = $"{(int)(currentZoom * 100)}%"; }
            zoomInBtn.Click += (s, e) => { ApplyZoom(currentZoom + 0.25); e.Handled = true; };
            zoomOutBtn.Click += (s, e) => { ApplyZoom(currentZoom - 0.25); e.Handled = true; };

            // ── Drag ──
            bool isDragging = false; Point dragStart = new Point();
            moveBtn.PreviewMouseLeftButtonDown += (s, e) => { isDragging = true; dragStart = e.GetPosition(canvas); container.CaptureMouse(); e.Handled = true; };
            container.MouseMove += (s, e) =>
            {
                if (!isDragging) return;
                var pos = e.GetPosition(canvas);
                double cl = Canvas.GetLeft(container); if (double.IsNaN(cl)) cl = 0;
                double ct = Canvas.GetTop(container); if (double.IsNaN(ct)) ct = 0;
                Canvas.SetLeft(container, cl + pos.X - dragStart.X);
                Canvas.SetTop(container, ct + pos.Y - dragStart.Y);
                dragStart = pos; e.Handled = true;
            };
            container.MouseLeftButtonUp += (s, e) => { if (isDragging) { isDragging = false; container.ReleaseMouseCapture(); e.Handled = true; } };

            // ── Copy ──
            copyBtn.Click += (s, e) =>
            {
                var clone = BuildSimViewerOnCanvas(simUrl, simTitle, viewerWidth, viewerHeight, canvas);
                double l = Canvas.GetLeft(container); if (double.IsNaN(l)) l = 50;
                double t = Canvas.GetTop(container); if (double.IsNaN(t)) t = 50;
                Canvas.SetLeft(clone, l + 40); Canvas.SetTop(clone, t + 40);
                canvas.Children.Add(clone); e.Handled = true;
            };

            // ── ✂️ Snipping Tool ──
            snipBtn.Click += async (s, e) =>
            {
                e.Handled = true;
                try
                {
                    if (webView.CoreWebView2 == null) return;
                    using var ms = new MemoryStream();
                    await webView.CoreWebView2.CapturePreviewAsync(
                        Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, ms);
                    ms.Position = 0;
                    var fullBitmap = new System.Windows.Media.Imaging.BitmapImage();
                    fullBitmap.BeginInit();
                    fullBitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    fullBitmap.StreamSource = ms;
                    fullBitmap.EndInit();
                    fullBitmap.Freeze();

                    var overlayCanvas = new Canvas
                    {
                        Background = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)),
                        Width = canvas.ActualWidth, Height = canvas.ActualHeight, Cursor = Cursors.Cross
                    };
                    var guide = new TextBlock
                    {
                        Text = "✂️ Kéo chuột để chọn vùng chụp mô phỏng  |  ESC để hủy",
                        FontSize = 16, Foreground = Brushes.White,
                        Background = new SolidColorBrush(Color.FromArgb(180, 74, 20, 140)),
                        Padding = new Thickness(16, 8, 16, 8)
                    };
                    Canvas.SetLeft(guide, (canvas.ActualWidth - 450) / 2); Canvas.SetTop(guide, 20);
                    overlayCanvas.Children.Add(guide);

                    var previewImg = new System.Windows.Controls.Image
                    {
                        Source = fullBitmap, Width = webView.ActualWidth, Height = webView.ActualHeight,
                        Stretch = System.Windows.Media.Stretch.Fill
                    };
                    var wvOrigin = webView.TranslatePoint(new Point(0, 0), canvas);
                    Canvas.SetLeft(previewImg, wvOrigin.X); Canvas.SetTop(previewImg, wvOrigin.Y);
                    overlayCanvas.Children.Add(previewImg);

                    var selRect = new System.Windows.Shapes.Rectangle
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb(156, 39, 176)), StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection(new[] { 5.0, 3.0 }),
                        Fill = new SolidColorBrush(Color.FromArgb(40, 156, 39, 176)), Visibility = Visibility.Collapsed
                    };
                    overlayCanvas.Children.Add(selRect);
                    canvas.Children.Add(overlayCanvas);

                    bool isSel = false; Point selSt = new Point(); Rect selRg = Rect.Empty;
                    overlayCanvas.MouseLeftButtonDown += (ss, ee) =>
                    {
                        isSel = true; selSt = ee.GetPosition(overlayCanvas);
                        Canvas.SetLeft(selRect, selSt.X); Canvas.SetTop(selRect, selSt.Y);
                        selRect.Width = 0; selRect.Height = 0; selRect.Visibility = Visibility.Visible;
                        overlayCanvas.CaptureMouse(); ee.Handled = true;
                    };
                    overlayCanvas.MouseMove += (ss, ee) =>
                    {
                        if (!isSel) return;
                        var cur = ee.GetPosition(overlayCanvas);
                        double x = Math.Min(selSt.X, cur.X), y = Math.Min(selSt.Y, cur.Y);
                        double w = Math.Abs(cur.X - selSt.X), h = Math.Abs(cur.Y - selSt.Y);
                        Canvas.SetLeft(selRect, x); Canvas.SetTop(selRect, y);
                        selRect.Width = w; selRect.Height = h;
                        selRg = new Rect(x, y, w, h); ee.Handled = true;
                    };
                    overlayCanvas.MouseLeftButtonUp += (ss, ee) =>
                    {
                        if (!isSel) return; isSel = false; overlayCanvas.ReleaseMouseCapture(); ee.Handled = true;
                        if (selRg.Width < 20 || selRg.Height < 20) { canvas.Children.Remove(overlayCanvas); return; }
                        try
                        {
                            double sX = fullBitmap.PixelWidth / webView.ActualWidth, sY = fullBitmap.PixelHeight / webView.ActualHeight;
                            int cX = Math.Max(0, (int)((selRg.X - wvOrigin.X) * sX));
                            int cY = Math.Max(0, (int)((selRg.Y - wvOrigin.Y) * sY));
                            int cW = Math.Min((int)(selRg.Width * sX), fullBitmap.PixelWidth - cX);
                            int cH = Math.Min((int)(selRg.Height * sY), fullBitmap.PixelHeight - cY);
                            if (cW > 10 && cH > 10)
                            {
                                var cropped = new System.Windows.Media.Imaging.CroppedBitmap(fullBitmap, new Int32Rect(cX, cY, cW, cH));
                                cropped.Freeze();
                                double dW = Math.Min(selRg.Width * 1.2, canvas.ActualWidth * 0.6);
                                double dH = dW * cH / cW;
                                var ic = BuildImageOnCanvas(cropped, dW, dH, canvas);
                                Canvas.SetLeft(ic, selRg.X); Canvas.SetTop(ic, selRg.Y);
                                canvas.Children.Add(ic);
                                Log.Information("Sim snip captured: {W}x{H}", cW, cH);
                            }
                        }
                        catch (Exception ex) { Log.Warning("Sim snip error: {Err}", ex.Message); }
                        canvas.Children.Remove(overlayCanvas);
                    };
                    overlayCanvas.KeyDown += (ss, ee) =>
                    {
                        if (ee.Key == System.Windows.Input.Key.Escape) { isSel = false; overlayCanvas.ReleaseMouseCapture(); canvas.Children.Remove(overlayCanvas); ee.Handled = true; }
                    };
                    overlayCanvas.Focusable = true; overlayCanvas.Focus();
                }
                catch (Exception ex) { Log.Warning("Sim snip error: {Err}", ex.Message); }
            };

            // ── Delete ──
            delBtn.Click += (s, e) => { try { webView.Dispose(); } catch { } canvas.Children.Remove(container); e.Handled = true; };

            AddResizeGrips(container, canvas);
            return container;
        }

        /// <summary>
        /// Tạo Quiz card trên canvas — hiển thị câu hỏi + 4 đáp án + nút hiện đáp án
        /// Data format: "question;;A||B||C||D;;correctIdx;;points;;difficulty"
        /// </summary>
        private static Grid BuildQuizCardOnCanvas(string quizData, double cardWidth, Canvas canvas)
        {
            // Parse quiz data
            var parts = quizData.Split(";;");
            string question = parts.Length > 0 ? parts[0] : "Câu hỏi?";
            string[] options = parts.Length > 1 ? parts[1].Split("||") : new[] { "A", "B", "C", "D" };
            int correctIdx = parts.Length > 2 && int.TryParse(parts[2], out var ci) ? ci : 0;
            string points = parts.Length > 3 ? parts[3] : "10";
            string difficulty = parts.Length > 4 ? parts[4] : "Trung bình";

            var labels = new[] { "A", "B", "C", "D" };
            var optColors = new[] {
                Color.FromRgb(227, 242, 253), Color.FromRgb(232, 245, 233),
                Color.FromRgb(255, 243, 224), Color.FromRgb(252, 228, 236)
            };
            var correctColor = Color.FromRgb(76, 175, 80);

            // ── Toolbar ──
            var toolbar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0, 105, 92)),
                Height = 44, CornerRadius = new CornerRadius(10, 10, 0, 0)
            };
            var toolbarStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var tbStyle = new Style(typeof(Button));
            tbStyle.Setters.Add(new Setter(Button.FontSizeProperty, 12.0));
            tbStyle.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(8, 4, 8, 4)));
            tbStyle.Setters.Add(new Setter(Button.CursorProperty, Cursors.Hand));
            tbStyle.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(0)));
            tbStyle.Setters.Add(new Setter(Button.MarginProperty, new Thickness(2, 0, 2, 0)));
            tbStyle.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
            tbStyle.Setters.Add(new Setter(Button.BackgroundProperty, Brushes.Transparent));

            toolbarStack.Children.Add(new TextBlock
            {
                Text = $"❓ Câu hỏi Quiz  •  {points} điểm  •  {difficulty}",
                FontSize = 13, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 20, 0)
            });

            var moveBtn = new Button { Content = "✋ Di chuyển", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 33, 150, 243)) };
            var copyBtn = new Button { Content = "📋 Copy", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 76, 175, 80)) };
            var delBtn = new Button { Content = "🗑 Đóng", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 229, 57, 53)) };

            toolbarStack.Children.Add(moveBtn);
            toolbarStack.Children.Add(copyBtn);
            toolbarStack.Children.Add(delBtn);
            toolbar.Child = toolbarStack;

            // ── Question card body ──
            var bodyPanel = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(0, 0, 10, 10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 105, 92)),
                BorderThickness = new Thickness(2, 0, 2, 2),
                Padding = new Thickness(24, 20, 24, 20)
            };
            var bodyStack = new StackPanel();

            // Question text
            bodyStack.Children.Add(new TextBlock
            {
                Text = question, FontSize = 20, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 20)
            });

            // Options
            var optionBorders = new Border[4];
            for (int i = 0; i < Math.Min(4, options.Length); i++)
            {
                var optBorder = new Border
                {
                    Background = new SolidColorBrush(optColors[i]),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16, 12, 16, 12),
                    Margin = new Thickness(0, 0, 0, 8),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)),
                    BorderThickness = new Thickness(1)
                };
                optionBorders[i] = optBorder;

                var optRow = new StackPanel { Orientation = Orientation.Horizontal };
                // Label badge
                var badge = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                    CornerRadius = new CornerRadius(6), Width = 36, Height = 36,
                    Margin = new Thickness(0, 0, 14, 0)
                };
                badge.Child = new TextBlock
                {
                    Text = labels[i], FontSize = 18, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                optRow.Children.Add(badge);

                optRow.Children.Add(new TextBlock
                {
                    Text = options[i], FontSize = 16,
                    Foreground = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = cardWidth - 120
                });

                optBorder.Child = optRow;
                bodyStack.Children.Add(optBorder);
            }

            // ── "Hiện đáp án" button ──
            var revealBtn = new Button
            {
                Content = "👁 Hiện đáp án", FontSize = 14, Padding = new Thickness(20, 10, 20, 10),
                Background = new SolidColorBrush(Color.FromRgb(0, 105, 92)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 0)
            };
            bool isRevealed = false;
            revealBtn.Click += (s, e) =>
            {
                if (!isRevealed)
                {
                    // Highlight correct answer
                    if (correctIdx >= 0 && correctIdx < optionBorders.Length && optionBorders[correctIdx] != null)
                    {
                        optionBorders[correctIdx].Background = new SolidColorBrush(Color.FromRgb(200, 230, 201));
                        optionBorders[correctIdx].BorderBrush = new SolidColorBrush(correctColor);
                        optionBorders[correctIdx].BorderThickness = new Thickness(3);

                        // Add ✅ icon to correct answer
                        if (optionBorders[correctIdx].Child is StackPanel sp)
                        {
                            sp.Children.Add(new TextBlock
                            {
                                Text = " ✅", FontSize = 18, VerticalAlignment = VerticalAlignment.Center,
                                Foreground = new SolidColorBrush(correctColor)
                            });
                        }
                    }
                    revealBtn.Content = "🔒 Ẩn đáp án";
                    revealBtn.Background = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                    isRevealed = true;
                }
                else
                {
                    // Reset all options
                    for (int i = 0; i < optionBorders.Length; i++)
                    {
                        if (optionBorders[i] == null) continue;
                        optionBorders[i].Background = new SolidColorBrush(optColors[i]);
                        optionBorders[i].BorderBrush = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0));
                        optionBorders[i].BorderThickness = new Thickness(1);

                        if (optionBorders[i].Child is StackPanel sp2 && sp2.Children.Count > 2)
                        {
                            sp2.Children.RemoveAt(sp2.Children.Count - 1);
                        }
                    }
                    revealBtn.Content = "👁 Hiện đáp án";
                    revealBtn.Background = new SolidColorBrush(Color.FromRgb(0, 105, 92));
                    isRevealed = false;
                }
                e.Handled = true;
            };
            bodyStack.Children.Add(revealBtn);
            bodyPanel.Child = bodyStack;

            // ── Container ──
            var innerStack = new StackPanel();
            innerStack.Children.Add(toolbar);
            innerStack.Children.Add(bodyPanel);

            var container = new Grid
            {
                Width = cardWidth, Tag = "WhiteboardQuiz",
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 20, ShadowDepth = 5, Opacity = 0.35, Color = Color.FromRgb(0, 77, 64) }
            };
            container.Children.Add(innerStack);

            // ── Drag ──
            bool isDragging = false; Point dragStart = new Point();
            moveBtn.PreviewMouseLeftButtonDown += (s, e) => { isDragging = true; dragStart = e.GetPosition(canvas); container.CaptureMouse(); e.Handled = true; };
            container.MouseMove += (s, e) =>
            {
                if (!isDragging) return;
                var pos = e.GetPosition(canvas);
                double cl = Canvas.GetLeft(container); if (double.IsNaN(cl)) cl = 0;
                double ct = Canvas.GetTop(container); if (double.IsNaN(ct)) ct = 0;
                Canvas.SetLeft(container, cl + pos.X - dragStart.X);
                Canvas.SetTop(container, ct + pos.Y - dragStart.Y);
                dragStart = pos; e.Handled = true;
            };
            container.MouseLeftButtonUp += (s, e) => { if (isDragging) { isDragging = false; container.ReleaseMouseCapture(); e.Handled = true; } };

            // ── Copy ──
            copyBtn.Click += (s, e) =>
            {
                var clone = BuildQuizCardOnCanvas(quizData, cardWidth, canvas);
                double l = Canvas.GetLeft(container); if (double.IsNaN(l)) l = 50;
                double t = Canvas.GetTop(container); if (double.IsNaN(t)) t = 50;
                Canvas.SetLeft(clone, l + 40); Canvas.SetTop(clone, t + 40);
                canvas.Children.Add(clone); e.Handled = true;
            };

            // ── Delete ──
            delBtn.Click += (s, e) => { canvas.Children.Remove(container); e.Handled = true; };

            AddResizeGrips(container, canvas);
            return container;
        }

        /// <summary>
        /// Thêm resize grips vào container — kéo viền để Scale/Zoom nội dung
        /// (giống nút 🔍+/🔍−, không phải thay đổi Width/Height)
        /// </summary>
        private static void AddResizeGrips(Grid container, Canvas canvas)
        {
            const double gripSize = 10;
            const double minScale = 0.2;
            const double maxScale = 5.0;

            var gripBrush = new SolidColorBrush(Color.FromArgb(180, 25, 118, 210));
            var gripHover = new SolidColorBrush(Color.FromArgb(255, 33, 150, 243));

            // Viền highlight khi hover
            var resizeBorder = new Border
            {
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(6),
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            container.Children.Add(resizeBorder);

            // 4 góc grip
            Border MakeCorner(HorizontalAlignment ha, VerticalAlignment va)
            {
                var grip = new Border
                {
                    Width = gripSize, Height = gripSize,
                    Background = gripBrush,
                    HorizontalAlignment = ha, VerticalAlignment = va,
                    Cursor = Cursors.SizeNWSE,
                    CornerRadius = new CornerRadius(3),
                    Opacity = 0, Tag = "ResizeGrip",
                    ToolTip = "Kéo để phóng to / thu nhỏ"
                };
                grip.MouseEnter += (s, e) => { grip.Opacity = 1; grip.Background = gripHover; };
                grip.MouseLeave += (s, e) => { grip.Opacity = 0; grip.Background = gripBrush; };
                return grip;
            }

            var cornerTL = MakeCorner(HorizontalAlignment.Left, VerticalAlignment.Top);
            var cornerTR = MakeCorner(HorizontalAlignment.Right, VerticalAlignment.Top);
            var cornerBL = MakeCorner(HorizontalAlignment.Left, VerticalAlignment.Bottom);
            var cornerBR = MakeCorner(HorizontalAlignment.Right, VerticalAlignment.Bottom);
            var corners = new[] { cornerTL, cornerTR, cornerBL, cornerBR };
            foreach (var c in corners) container.Children.Add(c);

            // Hiện/ẩn grips + viền khi hover
            container.MouseEnter += (s, e) =>
            {
                foreach (var c in corners) c.Opacity = 0.7;
                resizeBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(100, 33, 150, 243));
            };
            container.MouseLeave += (s, e) =>
            {
                foreach (var c in corners) c.Opacity = 0;
                resizeBorder.BorderBrush = Brushes.Transparent;
            };

            // Scale logic — kéo khoảng cách = scale
            foreach (var grip in corners)
            {
                bool isScaling = false;
                Point scaleStart = new Point();
                double startScale = 1.0;

                grip.MouseLeftButtonDown += (s, e) =>
                {
                    isScaling = true;
                    scaleStart = e.GetPosition(canvas);
                    if (container.LayoutTransform is ScaleTransform st)
                        startScale = st.ScaleX;
                    else
                        startScale = 1.0;
                    grip.CaptureMouse();
                    e.Handled = true;
                };

                grip.MouseMove += (s, e) =>
                {
                    if (!isScaling) return;
                    var pos = e.GetPosition(canvas);
                    double dx = pos.X - scaleStart.X;
                    double dy = pos.Y - scaleStart.Y;
                    double delta = (dx + dy) / 300.0;
                    double newScale = Math.Max(minScale, Math.Min(maxScale, startScale + delta));
                    container.LayoutTransform = new ScaleTransform(newScale, newScale);
                    e.Handled = true;
                };

                grip.MouseLeftButtonUp += (s, e) =>
                {
                    isScaling = false;
                    grip.ReleaseMouseCapture();
                    e.Handled = true;
                };
            }
        }


        /// <summary>
        /// Parse text thành RichTextBox với formatting: tiêu đề bold lớn, bullet points, highlights
        /// </summary>
        private static RichTextBox BuildRichTextContent(string text)
        {
            var rtb = new RichTextBox
            {
                BorderThickness = new Thickness(0),
                IsReadOnly = false,
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13.5
            };

            var doc = new FlowDocument
            {
                PagePadding = new Thickness(0),
                LineStackingStrategy = LineStackingStrategy.MaxHeight
            };

            var lines = text.Split('\n');
            Paragraph? currentPara = null;

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r');

                if (string.IsNullOrWhiteSpace(line))
                {
                    // Empty line → new paragraph break
                    if (currentPara != null) { doc.Blocks.Add(currentPara); currentPara = null; }
                    continue;
                }

                // ── Detect line type and format accordingly ──
                if (IsHeading(line))
                {
                    // Flush previous paragraph
                    if (currentPara != null) { doc.Blocks.Add(currentPara); currentPara = null; }

                    var headingPara = new Paragraph { Margin = new Thickness(0, 8, 0, 4) };

                    if (line.StartsWith("📌") || line.StartsWith("✍️"))
                    {
                        // Section title — large, bold, colored
                        headingPara.Inlines.Add(new Run(line)
                        {
                            FontSize = 16, FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210))
                        });
                    }
                    else if (line.StartsWith("📖"))
                    {
                        // Chapter heading — medium-large, dark
                        headingPara.Inlines.Add(new Run(line)
                        {
                            FontSize = 15, FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
                        });
                    }
                    else if (line.StartsWith("📝"))
                    {
                        // Example — medium, green
                        headingPara.Inlines.Add(new Run(line)
                        {
                            FontSize = 14, FontWeight = FontWeights.SemiBold,
                            Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50))
                        });
                    }
                    else if (line.StartsWith("⚡"))
                    {
                        // Advanced — orange
                        headingPara.Inlines.Add(new Run(line)
                        {
                            FontSize = 14, FontWeight = FontWeights.SemiBold,
                            Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0))
                        });
                    }
                    else
                    {
                        headingPara.Inlines.Add(new Run(line)
                        {
                            FontSize = 15, FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
                        });
                    }
                    doc.Blocks.Add(headingPara);
                }
                else if (line.TrimStart().StartsWith("•") || line.TrimStart().StartsWith("🔹") || line.TrimStart().StartsWith("🔵") ||
                         line.TrimStart().StartsWith("🟢") || line.TrimStart().StartsWith("🟡") || line.TrimStart().StartsWith("🔴") ||
                         line.TrimStart().StartsWith("✅") || line.TrimStart().StartsWith("❌") || line.TrimStart().StartsWith("🎯"))
                {
                    // Bullet point — each on its own paragraph
                    if (currentPara != null) { doc.Blocks.Add(currentPara); currentPara = null; }
                    var bulletPara = new Paragraph { Margin = new Thickness(12, 1, 0, 1) };
                    bulletPara.Inlines.Add(new Run(line)
                    {
                        FontSize = 13.5,
                        Foreground = new SolidColorBrush(Color.FromRgb(55, 55, 55))
                    });
                    doc.Blocks.Add(bulletPara);
                }
                else if (line.TrimStart().StartsWith("→") || line.TrimStart().StartsWith("   "))
                {
                    // Indented continuation
                    if (currentPara != null) { doc.Blocks.Add(currentPara); currentPara = null; }
                    var indentPara = new Paragraph { Margin = new Thickness(24, 0, 0, 1) };
                    indentPara.Inlines.Add(new Run(line.TrimStart())
                    {
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)),
                        FontStyle = FontStyles.Italic
                    });
                    doc.Blocks.Add(indentPara);
                }
                else if (IsFormula(line))
                {
                    // Math formula / code — monospace, highlighted background
                    if (currentPara != null) { doc.Blocks.Add(currentPara); currentPara = null; }
                    var formulaPara = new Paragraph
                    {
                        Margin = new Thickness(16, 4, 16, 4),
                        Padding = new Thickness(12, 6, 12, 6),
                        Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    };
                    formulaPara.Inlines.Add(new Run(line.Trim())
                    {
                        FontSize = 14, FontWeight = FontWeights.SemiBold,
                        FontFamily = new FontFamily("Cambria Math, Segoe UI"),
                        Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92))
                    });
                    doc.Blocks.Add(formulaPara);
                }
                else if (IsNumberedItem(line))
                {
                    // Numbered exercise items
                    if (currentPara != null) { doc.Blocks.Add(currentPara); currentPara = null; }
                    var numPara = new Paragraph { Margin = new Thickness(8, 2, 0, 2) };
                    numPara.Inlines.Add(new Run(line)
                    {
                        FontSize = 13.5,
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
                    });
                    doc.Blocks.Add(numPara);
                }
                else
                {
                    // Normal text — accumulate into paragraph
                    if (currentPara == null)
                        currentPara = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
                    else
                        currentPara.Inlines.Add(new LineBreak());

                    currentPara.Inlines.Add(new Run(line)
                    {
                        FontSize = 13.5,
                        Foreground = new SolidColorBrush(Color.FromRgb(55, 55, 55))
                    });
                }
            }
            if (currentPara != null) doc.Blocks.Add(currentPara);

            rtb.Document = doc;
            return rtb;
        }

        private static bool IsHeading(string line) =>
            line.StartsWith("📌") || line.StartsWith("📖") || line.StartsWith("✍️") ||
            line.StartsWith("📝") || line.StartsWith("⚡") || line.StartsWith("🏁");

        private static bool IsFormula(string line)
        {
            var trimmed = line.TrimStart();
            return (trimmed.StartsWith("y =") || trimmed.StartsWith("x =") || trimmed.StartsWith("a²") ||
                    trimmed.StartsWith("S =") || trimmed.StartsWith("E =") || trimmed.StartsWith("u =") ||
                    trimmed.StartsWith("∫") || trimmed.StartsWith("Δ") || trimmed.StartsWith("lim") ||
                    trimmed.StartsWith("cos") || trimmed.StartsWith("sin") || trimmed.StartsWith("v =") ||
                    trimmed.StartsWith("ω =") || trimmed.StartsWith("f =") || trimmed.StartsWith("λ =") ||
                    trimmed.StartsWith("b² =") || trimmed.StartsWith("c² =") || trimmed.StartsWith("E⃗")) &&
                   trimmed.Length < 80;
        }

        private static bool IsNumberedItem(string line)
        {
            var trimmed = line.TrimStart();
            return (trimmed.Length > 2 && char.IsDigit(trimmed[0]) && (trimmed[1] == '.' || (char.IsDigit(trimmed[1]) && trimmed[2] == '.')));
        }

        // ═══════════════════════════════════════════════════════════
        //  TAB SWITCHING
        // ═══════════════════════════════════════════════════════════

        private void TabContent_Click(object sender, MouseButtonEventArgs e) => SwitchTab("content");
        private void TabQuiz_Click(object sender, MouseButtonEventArgs e) => SwitchTab("quiz");
        private void TabResources_Click(object sender, MouseButtonEventArgs e) => SwitchTab("resources");

        private void SwitchTab(string tab)
        {
            var active = new SolidColorBrush(Color.FromRgb(25, 118, 210));
            var inactive = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            tabContent.Background = tab == "content" ? active : inactive;
            tabQuiz.Background = tab == "quiz" ? active : inactive;
            tabResources.Background = tab == "resources" ? active : inactive;

            // Update tab text foreground
            ((TextBlock)((Border)tabContent).Child).Foreground = tab == "content" ? Brushes.White : new SolidColorBrush(Color.FromRgb(117, 117, 117));
            ((TextBlock)((Border)tabQuiz).Child).Foreground = tab == "quiz" ? Brushes.White : new SolidColorBrush(Color.FromRgb(117, 117, 117));
            ((TextBlock)((Border)tabResources).Child).Foreground = tab == "resources" ? Brushes.White : new SolidColorBrush(Color.FromRgb(117, 117, 117));

            // Show/Hide panels
            contentToolbar.Visibility = tab == "content" ? Visibility.Visible : Visibility.Collapsed;
            contentTabPanel.Visibility = tab == "content" ? Visibility.Visible : Visibility.Collapsed;
            quizTabPanel.Visibility = tab == "quiz" ? Visibility.Visible : Visibility.Collapsed;
            resourcesTabPanel.Visibility = tab == "resources" ? Visibility.Visible : Visibility.Collapsed;
        }

        // ═══════════════════════════════════════════════════════════
        //  QUIZ TAB — Thêm câu hỏi trắc nghiệm
        // ═══════════════════════════════════════════════════════════

        private int _quizItemCount = 0;

        private void AddQuizItem_Click(object sender, RoutedEventArgs e)
        {
            quizEmptyPlaceholder.Visibility = Visibility.Collapsed;
            _quizItemCount++;

            var card = new Border
            {
                Background = Brushes.White, CornerRadius = new CornerRadius(10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 105, 92)),
                BorderThickness = new Thickness(1.5), Margin = new Thickness(0, 0, 0, 12), Padding = new Thickness(16, 14, 16, 14)
            };
            var mainStack = new StackPanel();
            int qNum = _quizItemCount;

            // ═══ ROW 1: Header — Badge + Question Type + Delete ═══
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };

            // Delete button
            var delBtn = new Button
            {
                Content = "🗑️", FontSize = 14, Background = Brushes.Transparent,
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Padding = new Thickness(4), HorizontalAlignment = HorizontalAlignment.Right, ToolTip = "Xóa câu hỏi"
            };
            delBtn.Click += (s, e2) =>
            {
                quizItemsPanel.Children.Remove(card);
                _quizItemCount--;
                txtQuizCount.Text = $"{_quizItemCount} câu hỏi";
                if (_quizItemCount == 0) quizEmptyPlaceholder.Visibility = Visibility.Visible;
            };
            DockPanel.SetDock(delBtn, Dock.Right);
            header.Children.Add(delBtn);

            // Badge + Type selector in left area
            var leftHeader = new StackPanel { Orientation = Orientation.Horizontal };
            var badge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(224, 242, 241)),
                CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center
            };
            badge.Child = new TextBlock
            {
                Text = $"Câu {qNum}", FontSize = 12, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 77, 64))
            };
            leftHeader.Children.Add(badge);

            // Question Type ComboBox
            leftHeader.Children.Add(new TextBlock
            {
                Text = "Loại:", FontSize = 11, Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0)
            });
            var cmbType = new ComboBox { FontSize = 11, Padding = new Thickness(6, 3, 6, 3), SelectedIndex = 0, MinWidth = 140 };
            cmbType.Items.Add(new ComboBoxItem { Content = "📋 Trắc nghiệm", Tag = "MCQ" });
            cmbType.Items.Add(new ComboBoxItem { Content = "✅ Đúng / Sai", Tag = "TF" });
            cmbType.Items.Add(new ComboBoxItem { Content = "✍️ Điền khuyết", Tag = "FIB" });
            cmbType.Items.Add(new ComboBoxItem { Content = "🔗 Nối phương án", Tag = "MATCH" });
            cmbType.Items.Add(new ComboBoxItem { Content = "📝 Tự luận ngắn", Tag = "SHORT" });
            cmbType.Items.Add(new ComboBoxItem { Content = "🔢 Sắp xếp thứ tự", Tag = "ORDER" });
            leftHeader.Children.Add(cmbType);

            DockPanel.SetDock(leftHeader, Dock.Left);
            header.Children.Add(leftHeader);
            mainStack.Children.Add(header);

            // ═══ ROW 2: Mini formatting toolbar + Image insert ═══
            var qToolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };

            // Quick format buttons
            var fmtBtns = new (string txt, string tip, string tag)[]
            {
                ("B", "In đậm", "qBold"), ("I", "In nghiêng", "qItalic"), ("U", "Gạch chân", "qUnderline"),
            };
            foreach (var (txt, tip, tag) in fmtBtns)
            {
                var b = new Button
                {
                    Content = txt, ToolTip = tip, Tag = tag, Width = 26, Height = 24, FontSize = 11,
                    FontWeight = tag == "qBold" ? FontWeights.Bold : FontWeights.Normal,
                    FontStyle = tag == "qItalic" ? FontStyles.Italic : FontStyles.Normal,
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    BorderThickness = new Thickness(1), Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 2, 0), Padding = new Thickness(0)
                };
                qToolbar.Children.Add(b);
            }

            // Separator
            qToolbar.Children.Add(new Border { Width = 1, Height = 18, Background = new SolidColorBrush(Color.FromRgb(210, 210, 210)), Margin = new Thickness(4, 3, 4, 3) });

            // Font size
            qToolbar.Children.Add(new TextBlock { Text = "Cỡ:", FontSize = 10, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 3, 0) });
            var cmbFontSize = new ComboBox { FontSize = 10, Padding = new Thickness(3, 1, 3, 1), SelectedIndex = 2, MinWidth = 48 };
            foreach (var sz in new[] { "10", "11", "13", "14", "16", "18", "20", "24" })
                cmbFontSize.Items.Add(new ComboBoxItem { Content = sz });
            qToolbar.Children.Add(cmbFontSize);

            // Separator
            qToolbar.Children.Add(new Border { Width = 1, Height = 18, Background = new SolidColorBrush(Color.FromRgb(210, 210, 210)), Margin = new Thickness(4, 3, 4, 3) });

            // Color dots
            var qColors = new (byte r, byte g, byte b, string tip)[]
            {
                (33, 33, 33, "Đen"), (198, 40, 40, "Đỏ"), (25, 118, 210, "Xanh"), (46, 125, 50, "Lá")
            };
            foreach (var (r, g, b, tip) in qColors)
            {
                var dot = new System.Windows.Shapes.Ellipse
                {
                    Width = 12, Height = 12, Fill = new SolidColorBrush(Color.FromRgb(r, g, b)),
                    Stroke = new SolidColorBrush(Color.FromRgb(180, 180, 180)), StrokeThickness = 1
                };
                var cb = new Button
                {
                    Content = dot, ToolTip = $"Chữ {tip}", Width = 22, Height = 22,
                    Background = Brushes.White, BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 1, 0), Padding = new Thickness(0)
                };
                qToolbar.Children.Add(cb);
            }

            // Separator
            qToolbar.Children.Add(new Border { Width = 1, Height = 18, Background = new SolidColorBrush(Color.FromRgb(210, 210, 210)), Margin = new Thickness(4, 3, 4, 3) });

            // Insert Image button
            var btnImg = new Button
            {
                Content = "🖼️ Ảnh", FontSize = 10, Padding = new Thickness(6, 3, 6, 3),
                Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 2, 0),
                ToolTip = "Chèn hình ảnh vào câu hỏi"
            };
            qToolbar.Children.Add(btnImg);

            mainStack.Children.Add(qToolbar);

            // ═══ ROW 3: Question RichTextBox ═══
            var rtbQuestion = new RichTextBox
            {
                MinHeight = 60, BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Padding = new Thickness(8), FontFamily = new FontFamily("Segoe UI"), FontSize = 13,
                Margin = new Thickness(0, 0, 0, 4)
            };
            rtbQuestion.Document.Blocks.Clear();
            var placeholder = new Paragraph();
            placeholder.Inlines.Add(new Run($"Nhập nội dung câu hỏi {qNum}...") { Foreground = Brushes.LightGray });
            rtbQuestion.Document.Blocks.Add(placeholder);
            rtbQuestion.GotFocus += (s, e2) =>
            {
                var text = new TextRange(rtbQuestion.Document.ContentStart, rtbQuestion.Document.ContentEnd).Text.Trim();
                if (text.StartsWith("Nhập nội dung"))
                {
                    rtbQuestion.Document.Blocks.Clear();
                    rtbQuestion.Document.Blocks.Add(new Paragraph());
                }
            };

            // Wire format buttons to RichTextBox
            foreach (var child in qToolbar.Children)
            {
                if (child is Button fbtn && fbtn.Tag?.ToString()?.StartsWith("q") == true)
                {
                    var capturedTag = fbtn.Tag.ToString()!;
                    fbtn.Click += (s, e2) =>
                    {
                        var sel = rtbQuestion.Selection;
                        if (sel.IsEmpty) return;
                        switch (capturedTag)
                        {
                            case "qBold":
                                var w = sel.GetPropertyValue(TextElement.FontWeightProperty);
                                sel.ApplyPropertyValue(TextElement.FontWeightProperty, w is FontWeight fw && fw == FontWeights.Bold ? FontWeights.Normal : FontWeights.Bold);
                                break;
                            case "qItalic":
                                var st = sel.GetPropertyValue(TextElement.FontStyleProperty);
                                sel.ApplyPropertyValue(TextElement.FontStyleProperty, st is FontStyle fst && fst == FontStyles.Italic ? FontStyles.Normal : FontStyles.Italic);
                                break;
                            case "qUnderline":
                                var dec = sel.GetPropertyValue(Inline.TextDecorationsProperty);
                                sel.ApplyPropertyValue(Inline.TextDecorationsProperty, dec == TextDecorations.Underline ? null : TextDecorations.Underline);
                                break;
                        }
                        rtbQuestion.Focus();
                    };
                }
            }

            // Wire font size
            cmbFontSize.SelectionChanged += (s, e2) =>
            {
                if (cmbFontSize.SelectedItem is ComboBoxItem ci && double.TryParse(ci.Content?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sz))
                {
                    if (!rtbQuestion.Selection.IsEmpty)
                        rtbQuestion.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, sz);
                }
            };

            // Wire image insert
            btnImg.Click += (s, e2) =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Chọn hình ảnh cho câu hỏi", Filter = "Hình ảnh|*.png;*.jpg;*.jpeg;*.gif;*.bmp"
                };
                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        var bmp = new System.Windows.Media.Imaging.BitmapImage(new Uri(dlg.FileName));
                        var img = new System.Windows.Controls.Image
                        {
                            Source = bmp, MaxWidth = 400, MaxHeight = 250, Stretch = Stretch.Uniform,
                            Margin = new Thickness(0, 4, 0, 4)
                        };
                        var container = new InlineUIContainer(img, rtbQuestion.CaretPosition);
                    }
                    catch (Exception ex3) { Log.Warning("Insert quiz image error: {Err}", ex3.Message); }
                }
            };

            mainStack.Children.Add(rtbQuestion);

            // Image preview area
            var imgPreviewArea = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
            mainStack.Children.Add(imgPreviewArea);

            // ═══ ROW 4: Answer area — changes based on question type ═══
            var answerContainer = new StackPanel();
            mainStack.Children.Add(answerContainer);

            // Build default MCQ answers
            BuildMCQAnswers(answerContainer, qNum);

            // Listen for type change
            cmbType.SelectionChanged += (s, e2) =>
            {
                answerContainer.Children.Clear();
                var selectedTag = (cmbType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "MCQ";
                switch (selectedTag)
                {
                    case "MCQ":   BuildMCQAnswers(answerContainer, qNum); break;
                    case "TF":    BuildTrueFalseAnswers(answerContainer, qNum); break;
                    case "FIB":   BuildFillInBlankAnswers(answerContainer); break;
                    case "MATCH": BuildMatchingAnswers(answerContainer); break;
                    case "SHORT": BuildShortAnswers(answerContainer); break;
                    case "ORDER": BuildOrderingAnswers(answerContainer); break;
                }
            };

            // ═══ ROW 5: Score + Difficulty + Explanation ═══
            var sep = new Border { Height = 1, Background = new SolidColorBrush(Color.FromRgb(230, 230, 230)), Margin = new Thickness(0, 8, 0, 8) };
            mainStack.Children.Add(sep);

            var metaRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            metaRow.Children.Add(new TextBlock { Text = "Điểm:", FontSize = 11, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) });
            metaRow.Children.Add(new TextBox
            {
                Text = "10", Width = 50, FontSize = 12, Padding = new Thickness(6, 4, 6, 4), TextAlignment = TextAlignment.Center,
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)), BorderThickness = new Thickness(1)
            });

            metaRow.Children.Add(new TextBlock { Text = "Độ khó:", FontSize = 11, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 4, 0) });
            var cmbDiff = new ComboBox { FontSize = 12, Padding = new Thickness(6, 3, 6, 3), SelectedIndex = 0 };
            cmbDiff.Items.Add(new ComboBoxItem { Content = "🟢 Dễ" });
            cmbDiff.Items.Add(new ComboBoxItem { Content = "🟡 Trung bình" });
            cmbDiff.Items.Add(new ComboBoxItem { Content = "🔴 Khó" });
            metaRow.Children.Add(cmbDiff);

            metaRow.Children.Add(new TextBlock { Text = "Thời gian:", FontSize = 11, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 4, 0) });
            metaRow.Children.Add(new TextBox
            {
                Text = "60", Width = 45, FontSize = 12, Padding = new Thickness(6, 4, 6, 4), TextAlignment = TextAlignment.Center,
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)), BorderThickness = new Thickness(1), ToolTip = "Giây"
            });
            metaRow.Children.Add(new TextBlock { Text = "s", FontSize = 10, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 0, 0) });
            mainStack.Children.Add(metaRow);

            // Explanation field
            mainStack.Children.Add(new TextBlock { Text = "💡 Giải thích đáp án (tùy chọn):", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 4, 0, 4) });
            var txtExplain = new TextBox
            {
                FontSize = 12, Padding = new Thickness(8, 6, 8, 6), AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap, MinHeight = 40, MaxHeight = 80,
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)), BorderThickness = new Thickness(1),
                Text = "Nhập giải thích cho đáp án đúng...", Foreground = Brushes.LightGray
            };
            txtExplain.GotFocus += (s, e2) => { if (txtExplain.Text.StartsWith("Nhập giải thích")) { txtExplain.Text = ""; txtExplain.Foreground = Brushes.Black; } };
            txtExplain.LostFocus += (s, e2) => { if (string.IsNullOrWhiteSpace(txtExplain.Text)) { txtExplain.Text = "Nhập giải thích cho đáp án đúng..."; txtExplain.Foreground = Brushes.LightGray; } };
            mainStack.Children.Add(txtExplain);

            // Save to Question Bank button
            var btnSaveToBank = new Button
            {
                Content = "💾 Lưu vào Ngân hàng câu hỏi", FontSize = 11, Padding = new Thickness(12, 6, 12, 6),
                Background = new SolidColorBrush(Color.FromRgb(255, 243, 224)),
                Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0),
                ToolTip = "Lưu câu hỏi này vào ngân hàng để tái sử dụng"
            };
            btnSaveToBank.Click += (s, e2) =>
            {
                var qContent = new TextRange(rtbQuestion.Document.ContentStart, rtbQuestion.Document.ContentEnd).Text.Trim();
                var qType = (cmbType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "MCQ";
                var explanation = txtExplain.Text.StartsWith("Nhập giải thích") ? "" : txtExplain.Text;
                SaveQuizItemToBank(qContent, qType, "[]", "", explanation, 10, "Easy", 60);
            };
            mainStack.Children.Add(btnSaveToBank);

            card.Child = mainStack;
            quizItemsPanel.Children.Add(card);
            txtQuizCount.Text = $"{_quizItemCount} câu hỏi";
            Log.Information("Quiz item added: #{Num}", qNum);
        }

        // ── Question Type Builders ──────────────────────────────────

        /// <summary>Trắc nghiệm A/B/C/D</summary>
        private static void BuildMCQAnswers(StackPanel container, int qNum)
        {
            container.Children.Add(new TextBlock { Text = "📋 Đáp án trắc nghiệm:", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 6, 0, 4) });
            var labels = new[] { "A", "B", "C", "D" };
            var colors = new[] {
                Color.FromRgb(227, 242, 253), Color.FromRgb(232, 245, 233),
                Color.FromRgb(255, 243, 224), Color.FromRgb(243, 229, 245)
            };
            for (int i = 0; i < 4; i++)
            {
                var row = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
                var rb = new RadioButton { GroupName = $"Quiz{qNum}", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0), IsChecked = i == 0, ToolTip = "Đáp án đúng" };
                DockPanel.SetDock(rb, Dock.Left); row.Children.Add(rb);
                var lb = new Border { Background = new SolidColorBrush(colors[i]), CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
                lb.Child = new TextBlock { Text = labels[i], FontWeight = FontWeights.Bold, FontSize = 11 };
                DockPanel.SetDock(lb, Dock.Left); row.Children.Add(lb);
                row.Children.Add(new TextBox { Text = $"Đáp án {labels[i]}...", FontSize = 12, Padding = new Thickness(8, 5, 8, 5), BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)), BorderThickness = new Thickness(1) });
                container.Children.Add(row);
            }
        }

        /// <summary>Đúng / Sai</summary>
        private static void BuildTrueFalseAnswers(StackPanel container, int qNum)
        {
            container.Children.Add(new TextBlock { Text = "✅ Chọn đáp án đúng:", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 6, 0, 6) });
            var tfPanel = new StackPanel();
            var rbTrue = new RadioButton { Content = "  ✅  Đúng (True)", GroupName = $"TF{qNum}", IsChecked = true, FontSize = 13, Margin = new Thickness(0, 0, 0, 6), Padding = new Thickness(8, 6, 8, 6) };
            var rbFalse = new RadioButton { Content = "  ❌  Sai (False)", GroupName = $"TF{qNum}", FontSize = 13, Padding = new Thickness(8, 6, 8, 6) };
            tfPanel.Children.Add(rbTrue);
            tfPanel.Children.Add(rbFalse);
            container.Children.Add(tfPanel);
        }

        /// <summary>Điền khuyết — Fill in the Blank</summary>
        private static void BuildFillInBlankAnswers(StackPanel container)
        {
            container.Children.Add(new TextBlock { Text = "✏️ Đáp án điền khuyết:", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 6, 0, 4) });
            container.Children.Add(new TextBlock { Text = "Gợi ý: Dùng [___] trong câu hỏi để đánh dấu chỗ trống", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)), Margin = new Thickness(0, 0, 0, 6) });

            for (int i = 1; i <= 3; i++)
            {
                var row = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
                var lb = new TextBlock { Text = $"Chỗ trống {i}:", FontSize = 11, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Width = 80 };
                DockPanel.SetDock(lb, Dock.Left); row.Children.Add(lb);
                row.Children.Add(new TextBox { Text = "", FontSize = 12, Padding = new Thickness(8, 5, 8, 5), BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)), BorderThickness = new Thickness(1) });
                container.Children.Add(row);
            }
            container.Children.Add(new CheckBox { Content = " Không phân biệt hoa/thường", IsChecked = true, FontSize = 11, Margin = new Thickness(0, 4, 0, 0) });
        }

        /// <summary>Nối phương án — Matching</summary>
        private static void BuildMatchingAnswers(StackPanel container)
        {
            container.Children.Add(new TextBlock { Text = "🔗 Nối phương án (trái → phải):", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 6, 0, 6) });

            for (int i = 1; i <= 4; i++)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40, GridUnitType.Pixel) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var left = new TextBox { Text = $"Vế trái {i}...", FontSize = 12, Padding = new Thickness(8, 5, 8, 5), BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderThickness = new Thickness(1) };
                Grid.SetColumn(left, 0); row.Children.Add(left);

                var arrow = new TextBlock { Text = "→", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(arrow, 1); row.Children.Add(arrow);

                var right = new TextBox { Text = $"Vế phải {i}...", FontSize = 12, Padding = new Thickness(8, 5, 8, 5), BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderThickness = new Thickness(1) };
                Grid.SetColumn(right, 2); row.Children.Add(right);

                container.Children.Add(row);
            }
        }

        /// <summary>Tự luận ngắn — Short Answer</summary>
        private static void BuildShortAnswers(StackPanel container)
        {
            container.Children.Add(new TextBlock { Text = "📝 Đáp án gợi ý (HS tự viết):", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 6, 0, 4) });
            container.Children.Add(new TextBox
            {
                FontSize = 12, Padding = new Thickness(10, 8, 10, 8), AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap, MinHeight = 60, Text = "Nhập đáp án mẫu...",
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderThickness = new Thickness(1)
            });
            container.Children.Add(new TextBlock { Text = "Số từ tối thiểu:", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 6, 0, 4) });
            container.Children.Add(new TextBox
            {
                Text = "20", Width = 60, FontSize = 12, Padding = new Thickness(6, 4, 6, 4),
                TextAlignment = TextAlignment.Center, BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Left
            });
        }

        /// <summary>Sắp xếp thứ tự — Ordering</summary>
        private static void BuildOrderingAnswers(StackPanel container)
        {
            container.Children.Add(new TextBlock { Text = "🔢 Sắp xếp đúng thứ tự (từ trên xuống):", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 6, 0, 6) });

            for (int i = 1; i <= 5; i++)
            {
                var row = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
                var numBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(227, 242, 253)),
                    CornerRadius = new CornerRadius(12), Width = 24, Height = 24,
                    Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center
                };
                numBadge.Child = new TextBlock { Text = i.ToString(), FontSize = 11, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                DockPanel.SetDock(numBadge, Dock.Left); row.Children.Add(numBadge);

                row.Children.Add(new TextBox
                {
                    Text = $"Bước {i}...", FontSize = 12, Padding = new Thickness(8, 5, 8, 5),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)), BorderThickness = new Thickness(1)
                });
                container.Children.Add(row);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  QUESTION BANK MANAGEMENT
        // ═══════════════════════════════════════════════════════════

        /// <summary>📚 Quản lý danh mục + câu hỏi trong ngân hàng</summary>
        private void ManageQuestionBank_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new Window
            {
                Title = "📚 Quản lý Ngân hàng câu hỏi",
                Width = 900, Height = 620,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };

            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // ── LEFT: Categories list ──
            var leftPanel = new Border { Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)), Padding = new Thickness(12) };
            var leftStack = new StackPanel();
            leftStack.Children.Add(new TextBlock { Text = "📂 Danh mục", FontSize = 14, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });

            var catList = new ListBox { MinHeight = 400, BorderThickness = new Thickness(0), Background = Brushes.Transparent };
            var questionsList = new StackPanel(); // right side

            // Load categories
            using (var db = new Data.AppDbContext())
            {
                db.Database.EnsureCreated();
                var cats = db.QuestionBankCategories.ToList();
                foreach (var cat in cats)
                {
                    catList.Items.Add(new ListBoxItem
                    {
                        Content = $"📁 {cat.Name} ({cat.Subject} {cat.Grade})",
                        Tag = cat.Id, FontSize = 12, Padding = new Thickness(8, 6, 8, 6)
                    });
                }
            }

            catList.SelectionChanged += (s2, e2) =>
            {
                if (catList.SelectedItem is ListBoxItem item && item.Tag is int catId)
                    LoadBankQuestions(questionsList, catId);
            };

            leftStack.Children.Add(catList);

            // Add category buttons
            var addCatRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            var txtNewCat = new TextBox { Width = 160, FontSize = 12, Padding = new Thickness(6, 4, 6, 4), Text = "Tên danh mục mới..." };
            txtNewCat.GotFocus += (s2, e2) => { if (txtNewCat.Text.StartsWith("Tên")) txtNewCat.Text = ""; };
            addCatRow.Children.Add(txtNewCat);

            var btnAddCat = new Button
            {
                Content = "➕", FontSize = 14, Padding = new Thickness(8, 4, 8, 4),
                Background = new SolidColorBrush(Color.FromRgb(0, 105, 92)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand, Margin = new Thickness(4, 0, 0, 0)
            };
            btnAddCat.Click += (s2, e2) =>
            {
                var name = txtNewCat.Text.Trim();
                if (string.IsNullOrWhiteSpace(name) || name.StartsWith("Tên")) return;

                var subject = (cmbSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Toán";
                var grade = (cmbGrade.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Lớp 10";

                using var db = new Data.AppDbContext();
                var cat = new Data.QuestionBankCategory { Name = name, Subject = subject, Grade = grade };
                db.QuestionBankCategories.Add(cat);
                db.SaveChanges();

                catList.Items.Add(new ListBoxItem
                {
                    Content = $"📁 {name} ({subject} {grade})",
                    Tag = cat.Id, FontSize = 12, Padding = new Thickness(8, 6, 8, 6)
                });
                txtNewCat.Text = "Tên danh mục mới...";
                Log.Information("Question bank category created: {Name}", name);
            };
            addCatRow.Children.Add(btnAddCat);

            var btnDelCat = new Button
            {
                Content = "🗑️", FontSize = 14, Padding = new Thickness(8, 4, 8, 4),
                Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand, Margin = new Thickness(4, 0, 0, 0), ToolTip = "Xóa danh mục đang chọn"
            };
            btnDelCat.Click += (s2, e2) =>
            {
                if (catList.SelectedItem is ListBoxItem item && item.Tag is int catId)
                {
                    if (MessageBox.Show($"Xóa danh mục này và tất cả câu hỏi bên trong?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        using var db = new Data.AppDbContext();
                        var cat = db.QuestionBankCategories.Find(catId);
                        if (cat != null) { db.QuestionBankCategories.Remove(cat); db.SaveChanges(); }
                        catList.Items.Remove(item);
                        questionsList.Children.Clear();
                    }
                }
            };
            addCatRow.Children.Add(btnDelCat);
            leftStack.Children.Add(addCatRow);
            leftPanel.Child = leftStack;
            Grid.SetColumn(leftPanel, 0);
            mainGrid.Children.Add(leftPanel);

            // ── RIGHT: Questions in selected category ──
            var rightPanel = new Border { Padding = new Thickness(12) };
            var rightScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var rightStack = new StackPanel();
            rightStack.Children.Add(new TextBlock { Text = "📋 Câu hỏi trong danh mục", FontSize = 14, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
            rightStack.Children.Add(questionsList);
            rightScroll.Content = rightStack;
            rightPanel.Child = rightScroll;
            Grid.SetColumn(rightPanel, 1);
            mainGrid.Children.Add(rightPanel);

            wnd.Content = mainGrid;
            wnd.ShowDialog();
        }

        private void LoadBankQuestions(StackPanel panel, int categoryId)
        {
            panel.Children.Clear();
            using var db = new Data.AppDbContext();
            var items = db.QuestionBankItems.Where(q => q.CategoryId == categoryId && q.ApprovalStatus == "Approved").OrderBy(q => q.Id).ToList();

            if (items.Count == 0)
            {
                panel.Children.Add(new TextBlock { Text = "Chưa có câu hỏi nào trong danh mục này.\nLưu câu hỏi vào NH khi soạn quiz bằng checkbox '💾 Lưu vào NH'", FontSize = 12, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap });
                return;
            }

            int idx = 0;
            foreach (var q in items)
            {
                idx++;
                var card = new Border
                {
                    Background = Brushes.White, CornerRadius = new CornerRadius(6),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 6), Padding = new Thickness(10, 8, 10, 8)
                };
                var sp = new StackPanel();

                var headerRow = new DockPanel();
                var typeBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(224, 242, 241)),
                    CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 2, 6, 2)
                };
                typeBadge.Child = new TextBlock { Text = $"#{idx} Â· {q.QuestionType}", FontSize = 10, FontWeight = FontWeights.Bold };
                DockPanel.SetDock(typeBadge, Dock.Left);
                headerRow.Children.Add(typeBadge);

                var diffBadge = new TextBlock
                {
                    Text = q.Difficulty == "Easy" ? "🟢" : q.Difficulty == "Hard" ? "🔴" : "🟡",
                    FontSize = 12, HorizontalAlignment = HorizontalAlignment.Right
                };
                DockPanel.SetDock(diffBadge, Dock.Right);
                headerRow.Children.Add(diffBadge);
                sp.Children.Add(headerRow);

                sp.Children.Add(new TextBlock
                {
                    Text = q.Content.Length > 120 ? q.Content.Substring(0, 120) + "..." : q.Content,
                    FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0)
                });

                card.Child = sp;
                panel.Children.Add(card);
            }
        }

        /// <summary>📥 Import câu hỏi từ ngân hàng vào quiz hiện tại</summary>
        private void ImportFromBank_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new Window
            {
                Title = "📥 Chọn câu hỏi từ Ngân hàng",
                Width = 750, Height = 550,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };

            var stack = new StackPanel { Margin = new Thickness(16) };

            // Category selector
            stack.Children.Add(new TextBlock { Text = "📂 Chọn danh mục:", FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            var cmbCat = new ComboBox { FontSize = 12, Padding = new Thickness(8, 5, 8, 5), Margin = new Thickness(0, 0, 0, 12) };

            using (var db = new Data.AppDbContext())
            {
                db.Database.EnsureCreated();
                foreach (var cat in db.QuestionBankCategories.ToList())
                    cmbCat.Items.Add(new ComboBoxItem { Content = $"📁 {cat.Name} ({cat.Subject} {cat.Grade}) — {db.QuestionBankItems.Count(q => q.CategoryId == cat.Id)} câu", Tag = cat.Id });
            }
            if (cmbCat.Items.Count > 0) cmbCat.SelectedIndex = 0;
            stack.Children.Add(cmbCat);

            // Questions with checkboxes
            var questionsScroll = new ScrollViewer { MaxHeight = 350, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var questionsStack = new StackPanel();
            var checkboxes = new List<(CheckBox cb, Data.QuestionBankItem item)>();

            void LoadQuestions()
            {
                questionsStack.Children.Clear();
                checkboxes.Clear();
                if (cmbCat.SelectedItem is ComboBoxItem ci && ci.Tag is int catId)
                {
                    using var db = new Data.AppDbContext();
                    var items = db.QuestionBankItems.Where(q => q.CategoryId == catId && q.ApprovalStatus == "Approved").ToList();
                    int idx = 0;
                    foreach (var q in items)
                    {
                        idx++;
                        var row = new Border
                        {
                            Background = Brushes.White, CornerRadius = new CornerRadius(6),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                            BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 4), Padding = new Thickness(10, 6, 10, 6)
                        };
                        var dp = new DockPanel();
                        var cb = new CheckBox { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
                        DockPanel.SetDock(cb, Dock.Left); dp.Children.Add(cb);

                        var info = new TextBlock
                        {
                            Text = $"[{q.QuestionType}] {q.Content}",
                            FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center
                        };
                        dp.Children.Add(info);
                        row.Child = dp;
                        questionsStack.Children.Add(row);
                        checkboxes.Add((cb, q));
                    }
                    if (items.Count == 0)
                        questionsStack.Children.Add(new TextBlock { Text = "Không có câu hỏi nào trong danh mục này", FontSize = 12, Foreground = Brushes.Gray });
                }
            }
            LoadQuestions();
            cmbCat.SelectionChanged += (s, e2) => LoadQuestions();
            questionsScroll.Content = questionsStack;
            stack.Children.Add(questionsScroll);

            // Select All + Import button
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
            var chkAll = new CheckBox { Content = " Chọn tất cả", FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0) };
            chkAll.Checked += (s, e2) => { foreach (var (cb, _) in checkboxes) cb.IsChecked = true; };
            chkAll.Unchecked += (s, e2) => { foreach (var (cb, _) in checkboxes) cb.IsChecked = false; };
            btnRow.Children.Add(chkAll);

            var btnImport = new Button
            {
                Content = "📥 Import câu hỏi đã chọn", FontSize = 13, Padding = new Thickness(20, 10, 20, 10),
                Background = new SolidColorBrush(Color.FromRgb(0, 105, 92)), Foreground = Brushes.White,
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand
            };
            btnImport.Click += (s, e2) =>
            {
                int count = 0;
                foreach (var (cb, item) in checkboxes)
                {
                    if (cb.IsChecked == true)
                    {
                        ImportBankItemToQuiz(item);
                        count++;
                    }
                }
                if (count > 0)
                {
                    ClassroomDialog.Info($"Đã import {count} câu hỏi vào quiz!", "Thành công");
                    wnd.Close();
                }
                else
                    ClassroomDialog.Warn("Vui lòng chọn ít nhất 1 câu hỏi!", "Thông báo");
            };
            btnRow.Children.Add(btnImport);
            stack.Children.Add(btnRow);

            wnd.Content = stack;
            wnd.ShowDialog();
        }

        /// <summary>Import 1 câu hỏi bank vào quiz panel</summary>
        private void ImportBankItemToQuiz(Data.QuestionBankItem item)
        {
            quizEmptyPlaceholder.Visibility = Visibility.Collapsed;
            _quizItemCount++;
            int qNum = _quizItemCount;

            var card = new Border
            {
                Background = Brushes.White, CornerRadius = new CornerRadius(10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                BorderThickness = new Thickness(1.5), Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(16, 12, 16, 12)
            };
            var stack = new StackPanel();

            // Header
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            var badge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(227, 242, 253)),
                CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 3, 8, 3)
            };
            badge.Child = new TextBlock { Text = $"Câu {qNum} · 📥 từ NH", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)) };
            DockPanel.SetDock(badge, Dock.Left); header.Children.Add(badge);

            var typeBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(224, 242, 241)),
                CornerRadius = new CornerRadius(4), Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(6, 0, 0, 0)
            };
            typeBadge.Child = new TextBlock { Text = item.QuestionType, FontSize = 10, FontWeight = FontWeights.Bold };
            DockPanel.SetDock(typeBadge, Dock.Left); header.Children.Add(typeBadge);

            var delBtn = new Button { Content = "🗑️", FontSize = 14, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Padding = new Thickness(4), HorizontalAlignment = HorizontalAlignment.Right };
            delBtn.Click += (s, e2) => { quizItemsPanel.Children.Remove(card); _quizItemCount--; txtQuizCount.Text = $"{_quizItemCount} câu hỏi"; if (_quizItemCount == 0) quizEmptyPlaceholder.Visibility = Visibility.Visible; };
            DockPanel.SetDock(delBtn, Dock.Right); header.Children.Add(delBtn);
            stack.Children.Add(header);

            // Question content
            stack.Children.Add(new TextBlock { Text = item.Content, FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) });

            // Answers from OptionsJson
            try
            {
                var options = System.Text.Json.JsonSerializer.Deserialize<string[]>(item.OptionsJson) ?? Array.Empty<string>();
                if (options.Length > 0)
                {
                    var labels = new[] { "A", "B", "C", "D", "E", "F" };
                    for (int i = 0; i < options.Length && i < labels.Length; i++)
                    {
                        var row = new DockPanel { Margin = new Thickness(0, 0, 0, 3) };
                        var lb = new Border { Background = new SolidColorBrush(i == 0 ? Color.FromRgb(200, 230, 201) : Color.FromRgb(240, 240, 240)), CornerRadius = new CornerRadius(3), Padding = new Thickness(5, 1, 5, 1), Margin = new Thickness(0, 0, 6, 0) };
                        lb.Child = new TextBlock { Text = labels[i], FontWeight = FontWeights.Bold, FontSize = 11 };
                        DockPanel.SetDock(lb, Dock.Left); row.Children.Add(lb);
                        row.Children.Add(new TextBlock { Text = options[i], FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
                        stack.Children.Add(row);
                    }
                }
            }
            catch { }

            // Meta row
            var meta = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
            meta.Children.Add(new TextBlock { Text = $"Điểm: {item.Points}  ·  {(item.Difficulty == "Easy" ? "🟢 Dễ" : item.Difficulty == "Hard" ? "🔴 Khó" : "🟡 TB")}  ·  ⏱ {item.TimeLimitSeconds}s", FontSize = 11, Foreground = Brushes.Gray });
            stack.Children.Add(meta);

            if (!string.IsNullOrWhiteSpace(item.Explanation) && !item.Explanation.StartsWith("Nhập"))
            {
                stack.Children.Add(new TextBlock { Text = $"💡 {item.Explanation}", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)), Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap });
            }

            card.Child = stack;
            quizItemsPanel.Children.Add(card);
            txtQuizCount.Text = $"{_quizItemCount} câu hỏi";
        }

        /// <summary>📤 Export / Import ngân hàng câu hỏi JSON</summary>
        private void ExportQuestionBank_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("📤 Export hay 📥 Import file ngân hàng?\n\nYes = Export (xuất ra file)\nNo = Import (nhập từ file)", "Export / Import ngân hàng", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // EXPORT
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Xuất ngân hàng câu hỏi",
                    Filter = "JSON|*.json", FileName = $"QuestionBank_{DateTime.Now:yyyyMMdd}.json"
                };
                if (dlg.ShowDialog() == true)
                {
                    using var db = new Data.AppDbContext();
                    var categories = db.QuestionBankCategories.ToList();
                    var items = db.QuestionBankItems.ToList();
                    var export = new { Categories = categories, Items = items, ExportedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), Version = "1.0" };
                    var json = System.Text.Json.JsonSerializer.Serialize(export, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(dlg.FileName, json);
                    MessageBox.Show($"✅ Đã xuất {categories.Count} danh mục, {items.Count} câu hỏi!", "Export thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    Log.Information("Question bank exported: {Cats} categories, {Items} items", categories.Count, items.Count);
                }
            }
            else if (result == MessageBoxResult.No)
            {
                // IMPORT
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Nhập ngân hàng câu hỏi từ file JSON",
                    Filter = "JSON|*.json"
                };
                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        var json = System.IO.File.ReadAllText(dlg.FileName);
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        using var db = new Data.AppDbContext();
                        int catCount = 0, itemCount = 0;

                        if (root.TryGetProperty("Categories", out var catsEl))
                        {
                            foreach (var catEl in catsEl.EnumerateArray())
                            {
                                var cat = new Data.QuestionBankCategory
                                {
                                    Name = catEl.GetProperty("Name").GetString() ?? "",
                                    Subject = catEl.TryGetProperty("Subject", out var s) ? s.GetString() ?? "" : "",
                                    Grade = catEl.TryGetProperty("Grade", out var g) ? g.GetString() ?? "" : "",
                                    Description = catEl.TryGetProperty("Description", out var d) ? d.GetString() ?? "" : ""
                                };
                                db.QuestionBankCategories.Add(cat);
                                db.SaveChanges();

                                // Import items for this category
                                int oldCatId = catEl.GetProperty("Id").GetInt32();
                                if (root.TryGetProperty("Items", out var itemsEl))
                                {
                                    foreach (var itemEl in itemsEl.EnumerateArray())
                                    {
                                        if (itemEl.GetProperty("CategoryId").GetInt32() == oldCatId)
                                        {
                                            db.QuestionBankItems.Add(new Data.QuestionBankItem
                                            {
                                                CategoryId = cat.Id,
                                                QuestionType = itemEl.TryGetProperty("QuestionType", out var qt) ? qt.GetString() ?? "MCQ" : "MCQ",
                                                Content = itemEl.GetProperty("Content").GetString() ?? "",
                                                OptionsJson = itemEl.TryGetProperty("OptionsJson", out var oj) ? oj.GetString() ?? "[]" : "[]",
                                                CorrectAnswer = itemEl.TryGetProperty("CorrectAnswer", out var ca) ? ca.GetString() ?? "" : "",
                                                Explanation = itemEl.TryGetProperty("Explanation", out var ex) ? ex.GetString() ?? "" : "",
                                                Points = itemEl.TryGetProperty("Points", out var pts) ? pts.GetInt32() : 10,
                                                Difficulty = itemEl.TryGetProperty("Difficulty", out var diff) ? diff.GetString() ?? "Easy" : "Easy",
                                                TimeLimitSeconds = itemEl.TryGetProperty("TimeLimitSeconds", out var tls) ? tls.GetInt32() : 60,
                                                Subject = cat.Subject, Grade = cat.Grade
                                            });
                                            itemCount++;
                                        }
                                    }
                                }
                                db.SaveChanges();
                                catCount++;
                            }
                        }
                        MessageBox.Show($"✅ Đã import {catCount} danh mục, {itemCount} câu hỏi!", "Import thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        Log.Information("Question bank imported: {Cats} categories, {Items} items", catCount, itemCount);
                    }
                    catch (Exception ex)
                    {
                        ClassroomDialog.Error($"Lỗi import: {ex.Message}", "Lỗi");
                    }
                }
            }
        }

        /// <summary>Lưu 1 câu hỏi vào ngân hàng (reusable)</summary>
        private void SaveQuizItemToBank(string questionContent, string questionType, string optionsJson, string correctAnswer, string explanation, int points, string difficulty, int timeLimit)
        {
            // Pick or create category
            var wnd = new Window
            {
                Title = "💾 Lưu vào Ngân hàng câu hỏi", Width = 400, Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this), ResizeMode = ResizeMode.NoResize
            };
            var sp = new StackPanel { Margin = new Thickness(16) };
            sp.Children.Add(new TextBlock { Text = "Chọn danh mục để lưu:", FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });

            var cmbCat = new ComboBox { FontSize = 12, Padding = new Thickness(8, 5, 8, 5), Margin = new Thickness(0, 0, 0, 8) };
            using (var db = new Data.AppDbContext())
            {
                db.Database.EnsureCreated();
                foreach (var cat in db.QuestionBankCategories.ToList())
                    cmbCat.Items.Add(new ComboBoxItem { Content = $"📁 {cat.Name}", Tag = cat.Id });
            }
            if (cmbCat.Items.Count > 0) cmbCat.SelectedIndex = 0;
            sp.Children.Add(cmbCat);

            sp.Children.Add(new TextBlock { Text = "hoặc tạo danh mục mới:", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 4, 0, 4) });
            var txtNewCat = new TextBox { FontSize = 12, Padding = new Thickness(8, 5, 8, 5) };
            sp.Children.Add(txtNewCat);

            var btnSave = new Button
            {
                Content = "💾 Lưu vào ngân hàng", FontSize = 13, Padding = new Thickness(0, 10, 0, 10),
                Background = new SolidColorBrush(Color.FromRgb(0, 105, 92)), Foreground = Brushes.White,
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Margin = new Thickness(0, 12, 0, 0)
            };
            btnSave.Click += (s, e2) =>
            {
                using var db = new Data.AppDbContext();
                int catId;
                if (!string.IsNullOrWhiteSpace(txtNewCat.Text))
                {
                    var subject = (cmbSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                    var grade = (cmbGrade.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                    var newCat = new Data.QuestionBankCategory { Name = txtNewCat.Text, Subject = subject, Grade = grade };
                    db.QuestionBankCategories.Add(newCat);
                    db.SaveChanges();
                    catId = newCat.Id;
                }
                else if (cmbCat.SelectedItem is ComboBoxItem ci && ci.Tag is int id)
                    catId = id;
                else { MessageBox.Show("Vui lòng chọn hoặc tạo danh mục!", "Thông báo"); return; }

                db.QuestionBankItems.Add(new Data.QuestionBankItem
                {
                    CategoryId = catId, QuestionType = questionType, Content = questionContent,
                    OptionsJson = optionsJson, CorrectAnswer = correctAnswer, Explanation = explanation,
                    Points = points, Difficulty = difficulty, TimeLimitSeconds = timeLimit,
                    Subject = (cmbSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
                    Grade = (cmbGrade.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? ""
                });
                db.SaveChanges();
                ClassroomDialog.Info("✅ Đã lưu câu hỏi vào ngân hàng!", "Thành công");
                wnd.Close();
            };
            sp.Children.Add(btnSave);
            wnd.Content = sp;
            wnd.ShowDialog();
        }

        // ═══════════════════════════════════════════════════════════
        //  RESOURCES TAB — Thêm file / link tài nguyên
        // ═══════════════════════════════════════════════════════════

        private int _resourceCount = 0;

        private void AddResourceFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Chọn file tài nguyên",
                Filter = "Tất cả file|*.*|PDF|*.pdf|Hình ảnh|*.png;*.jpg;*.jpeg;*.gif|Video|*.mp4;*.avi;*.wmv|Word|*.docx;*.doc|PowerPoint|*.pptx;*.ppt|Excel|*.xlsx;*.xls",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                foreach (var filePath in dlg.FileNames)
                {
                    AddResourceCard(filePath, null);
                }
            }
        }

        private void AddResourceLink_Click(object sender, RoutedEventArgs e)
        {
            // Simple input via a small modal
            var linkWnd = new Window
            {
                Title = "🔗 Thêm đường link tài nguyên",
                Width = 450, Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize
            };

            var sp = new StackPanel { Margin = new Thickness(20) };
            sp.Children.Add(new TextBlock { Text = "Tiêu đề:", FontSize = 12, Margin = new Thickness(0, 0, 0, 4) });
            var txtLinkTitle = new TextBox { FontSize = 13, Padding = new Thickness(8, 6, 8, 6), Text = "Video bài giảng tham khảo" };
            sp.Children.Add(txtLinkTitle);

            sp.Children.Add(new TextBlock { Text = "URL:", FontSize = 12, Margin = new Thickness(0, 12, 0, 4) });
            var txtUrl = new TextBox { FontSize = 13, Padding = new Thickness(8, 6, 8, 6), Text = "https://" };
            sp.Children.Add(txtUrl);

            var btnAdd = new Button
            {
                Content = "✅ Thêm link", FontSize = 13, Padding = new Thickness(0, 10, 0, 10),
                Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand, Margin = new Thickness(0, 16, 0, 0)
            };
            btnAdd.Click += (s2, e2) =>
            {
                if (!string.IsNullOrWhiteSpace(txtUrl.Text) && txtUrl.Text != "https://")
                {
                    AddResourceCard(null, txtUrl.Text, txtLinkTitle.Text);
                    linkWnd.Close();
                }
                else
                {
                    ClassroomDialog.Warn("Vui lòng nhập URL hợp lệ!", "Thiếu thông tin");
                }
            };
            sp.Children.Add(btnAdd);
            linkWnd.Content = sp;
            linkWnd.ShowDialog();
        }

        private void AddResourceCard(string? filePath, string? url, string title = "")
        {
            resourcesEmptyPlaceholder.Visibility = Visibility.Collapsed;
            _resourceCount++;

            var card = new Border
            {
                Background = Brushes.White, CornerRadius = new CornerRadius(10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(14, 10, 14, 10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            // Icon
            string icon;
            string displayName;
            string meta;
            if (filePath != null)
            {
                var ext = System.IO.Path.GetExtension(filePath).ToLower();
                icon = ext switch
                {
                    ".pdf" => "📄", ".doc" or ".docx" => "📝",
                    ".ppt" or ".pptx" => "📊", ".xls" or ".xlsx" => "📈",
                    ".png" or ".jpg" or ".jpeg" or ".gif" => "🖼️",
                    ".mp4" or ".avi" or ".wmv" => "🎬",
                    ".mp3" or ".wav" => "🎵",
                    _ => "📁"
                };
                displayName = System.IO.Path.GetFileName(filePath);
                var fileInfo = new System.IO.FileInfo(filePath);
                meta = fileInfo.Exists ? $"{fileInfo.Length / 1024.0:N0} KB Â· {ext.TrimStart('.')}" : ext.TrimStart('.');
            }
            else
            {
                icon = "🔗";
                displayName = string.IsNullOrWhiteSpace(title) ? url! : title;
                meta = url ?? "";
            }

            var iconTb = new TextBlock { Text = icon, FontSize = 24, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
            Grid.SetColumn(iconTb, 0);
            grid.Children.Add(iconTb);

            var infoSp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            infoSp.Children.Add(new TextBlock { Text = displayName, FontSize = 13, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
            infoSp.Children.Add(new TextBlock { Text = meta, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)), TextTrimming = TextTrimming.CharacterEllipsis });
            Grid.SetColumn(infoSp, 1);
            grid.Children.Add(infoSp);

            var delBtn = new Button
            {
                Content = "🗑️", FontSize = 14, Background = Brushes.Transparent,
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(4)
            };
            delBtn.Click += (s, e2) =>
            {
                resourcesListPanel.Children.Remove(card);
                _resourceCount--;
                txtResourceCount.Text = $"{_resourceCount} tài nguyên";
                if (_resourceCount == 0) resourcesEmptyPlaceholder.Visibility = Visibility.Visible;
            };
            Grid.SetColumn(delBtn, 2);
            grid.Children.Add(delBtn);

            card.Child = grid;
            resourcesListPanel.Children.Add(card);
            txtResourceCount.Text = $"{_resourceCount} tài nguyên";
            Log.Information("Resource added: {Name}, total: {Count}", displayName, _resourceCount);
        }

        // ═══════════════════════════════════════════════════════════
        //  PREVIEW — Xem trước bài giảng
        // ═══════════════════════════════════════════════════════════

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            if (_blockCount == 0)
            {
                ClassroomDialog.Info("Bài giảng chưa có nội dung để xem trước.\nHãy thêm block hoặc nhấn \"Bắt đầu soạn bài\".", "Chưa có nội dung");
                return;
            }

            // Create preview window
            var previewWnd = new Window
            {
                Title = $"👁️ Xem trước: {txtTitle.Text}",
                Width = 800, Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250))
            };

            var mainStack = new StackPanel { Margin = new Thickness(32, 24, 32, 24) };
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

            // Title header
            mainStack.Children.Add(new TextBlock
            {
                Text = txtTitle.Text, FontSize = 24, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                Margin = new Thickness(0, 0, 0, 4)
            });
            var subject = (cmbSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            var grade = (cmbGrade.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            mainStack.Children.Add(new TextBlock
            {
                Text = $"{subject} — {grade}", FontSize = 13,
                Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 4)
            });
            if (!string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                mainStack.Children.Add(new TextBlock
                {
                    Text = txtDescription.Text, FontSize = 12, Foreground = Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12)
                });
            }
            mainStack.Children.Add(new Border { Height = 1, Background = Brushes.LightGray, Margin = new Thickness(0, 4, 0, 16) });

            // Extract content from each block
            foreach (var child in blocksPanel.Children)
            {
                if (child is Border blockBorder && blockBorder != emptyPlaceholder)
                {
                    var rtb = FindChild<RichTextBox>(blockBorder);
                    if (rtb != null)
                    {
                        var text = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd).Text.Trim();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            var previewRtb = new RichTextBox
                            {
                                IsReadOnly = true, BorderThickness = new Thickness(0),
                                Background = Brushes.Transparent, Padding = new Thickness(0),
                                FontFamily = new FontFamily("Segoe UI"), FontSize = 14, Margin = new Thickness(0, 0, 0, 12)
                            };
                            previewRtb.Document.Blocks.Clear();
                            FormatRichContent(previewRtb.Document, text);
                            mainStack.Children.Add(previewRtb);
                        }
                    }
                    else
                    {
                        // Non-text block — show tag info
                        var tag = blockBorder.Tag?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(tag))
                        {
                            var parts = tag.Split('|', 2);
                            mainStack.Children.Add(new Border
                            {
                                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                                CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8),
                                Margin = new Thickness(0, 0, 0, 8),
                                Child = new TextBlock
                                {
                                    Text = $"[{parts[0]}] {(parts.Length > 1 ? System.IO.Path.GetFileName(parts[1]) : "")}",
                                    FontSize = 12, Foreground = Brushes.Gray
                                }
                            });
                        }
                    }
                }
            }

            // Footer
            mainStack.Children.Add(new Border { Height = 1, Background = Brushes.LightGray, Margin = new Thickness(0, 12, 0, 8) });
            mainStack.Children.Add(new TextBlock
            {
                Text = $"📊 Tổng: {_blockCount} blocks · Xem trước lúc {DateTime.Now:HH:mm dd/MM/yyyy}",
                FontSize = 11, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center
            });

            scroll.Content = mainStack;
            previewWnd.Content = scroll;
            previewWnd.ShowDialog();
        }

        // ═══════════════════════════════════════════════════════════
        //  START TEMPLATE — Tạo bài giảng mẫu theo Môn/Lớp
        // ═══════════════════════════════════════════════════════════

        private void StartTemplate_Click(object sender, MouseButtonEventArgs e)
        {
            var subject = (cmbSubject?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Toán";
            var grade = (cmbGrade?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Lớp 10";
            var title = txtTitle?.Text?.Trim() ?? "";

            // Generate template blocks
            var templateBlocks = GetLessonTemplate(subject, grade, title);

            // Auto-fill title if user hasn't typed one
            if (string.IsNullOrWhiteSpace(title) || title == "Hàm số bậc nhất y = ax + b")
            {
                txtTitle.Text = templateBlocks[0].title;
                txtDescription.Text = templateBlocks[0].description;
            }

            // Add each block
            emptyPlaceholder.Visibility = Visibility.Collapsed;
            foreach (var block in templateBlocks)
            {
                foreach (var content in block.blocks)
                {
                    AddEditableTextBlock(content);
                }
            }

            Log.Information("Template generated: {Subject} / {Grade} — {BlockCount} blocks", subject, grade, templateBlocks.Sum(b => b.blocks.Count));
            UpdateStats();
        }

        /// <summary>
        /// Trả về danh sách template blocks theo Môn + Lớp.
        /// Mỗi môn/lớp có cấu trúc: Mục tiêu → Kiến thức → Bài tập → Ghi chú GV
        /// </summary>
        private List<(string title, string description, List<string> blocks)> GetLessonTemplate(string subject, string grade, string currentTitle)
        {
            var result = new List<(string title, string description, List<string> blocks)>();
            var key = $"{subject}|{grade}";

            var (title, desc, blocks) = key switch
            {
                // ════════════════════════ TOÁN ════════════════════════
                "Toán|Lớp 10" => (
                    "Hàm số và đồ thị",
                    "Khái niệm hàm số, tính chất, đồ thị hàm số bậc nhất và bậc hai",
                    new List<string>
                    {
                        "📌 MỤC TIÊU BÀI HỌC\n• Nắm được khái niệm hàm số, tập xác định, tập giá trị\n• Vẽ được đồ thị hàm số bậc nhất y = ax + b\n• Xác định tính đồng biến, nghịch biến\n• Áp dụng vào bài toán thực tế",
                        "📖 I. KIẾN THỨC LÝ THUYẾT\n\n1. Khái niệm hàm số:\n   • Hàm số là quy tắc ứng mỗi giá trị x ∈ D với một giá trị y duy nhất\n   • Tập xác định D: tập hợp các giá trị x\n   • Tập giá trị: tập hợp các giá trị y\n\n2. Tính chất:\n   • Đồng biến: x₁ < x₂ → f(x₁) < f(x₂)\n   • Nghịch biến: x₁ < x₂ → f(x₁) > f(x₂)\n\n📌 GV ghi chú: Cho HS xem đồ thị trực quan trên bảng tương tác",
                        "📖 II. VÍ DỤ MINH HỌA\n\nVí dụ 1: Vẽ đồ thị y = 2x + 1\n• Bước 1: Cho x = 0 → y = 1 → A(0, 1)\n• Bước 2: Cho y = 0 → x = -1/2 → B(-0.5, 0)\n• Bước 3: Nối A, B → đường thẳng đi lên\n\nVí dụ 2: Vẽ đồ thị y = -x + 3\n• A(0, 3) và B(3, 0)\n• Đường thẳng đi xuống (a = -1 < 0 → nghịch biến)",
                        "✍️ BÀI TẬP LUYỆN TẬP\n\n📝 Mức cơ bản:\n1. Vẽ đồ thị: y = 3x - 2\n2. Xác định a, b: y = -2x + 5\n3. Tìm giao điểm với trục Ox, Oy: y = 4x - 8\n\n📝 Mức nâng cao:\n4. Tìm hàm bậc nhất biết đồ thị qua A(1,3) và B(2,5)\n5. Chứng minh 3 đường thẳng đồng quy\n\n⏰ Thời gian làm bài: 10 phút",
                        "📋 GHI CHÚ GIÁO VIÊN\n\n✅ Chuẩn bị: Máy chiếu, bảng tương tác, phần mềm vẽ đồ thị\n✅ Phương pháp: Giảng giải → Minh họa → Thực hành → Kiểm tra\n✅ Đánh giá: Quiz trắc nghiệm 5 câu cuối giờ\n✅ BTVN: Bài 1-5 trang 45 SGK\n✅ Lưu ý: HS thường nhầm dấu khi tính giao điểm"
                    }),
                "Toán|Lớp 11" => (
                    "Giới hạn và liên tục",
                    "Giới hạn dãy số, giới hạn hàm số, hàm số liên tục",
                    new List<string>
                    {
                        "📌 MỤC TIÊU BÀI HỌC\n• Hiểu khái niệm giới hạn của dãy số\n• Tính giới hạn bằng các quy tắc\n• Phân biệt dãy hội tụ và phân kỳ\n• Liên hệ với ứng dụng thực tế (lãi suất ngân hàng, dân số)",
                        "📖 I. GIỚI HẠN CỦA DÃY SỐ\n\nĐịnh nghĩa: Dãy (uₙ) có giới hạn L khi n → ∞\n\n📌 Các giới hạn cơ bản:\n• lim(1/n) = 0\n• lim(1/n²) = 0\n• lim(c) = c\n• lim(qⁿ) = 0 khi |q| < 1\n\n📌 Quy tắc:\n• lim(uₙ ± vₙ) = lim(uₙ) ± lim(vₙ)\n• lim(uₙ · vₙ) = lim(uₙ) · lim(vₙ)\n• lim(uₙ / vₙ) = lim(uₙ) / lim(vₙ)",
                        "📖 II. VÍ DỤ\n\nTính: lim(3n² + 1)/(n² + 2)\n= lim(3 + 1/n²)/(1 + 2/n²)\n= 3/1 = 3\n\nTính: lim(2n + 1)/(5n - 3)\n= lim(2 + 1/n)/(5 - 3/n)\n= 2/5",
                        "✍️ BÀI TẬP\n\n1. Tính: lim(n² + 3n)/(2n² - 1)\n2. Tính: lim(√(n²+1) - n)\n3. Dãy uₙ = (-1)ⁿ/n có hội tụ không? Vì sao?\n4. Tìm giới hạn: lim(1 + 1/n)ⁿ\n\n⏰ 8 phút thảo luận nhóm",
                        "📋 GHI CHÚ GIÁO VIÊN\n\n✅ Phương pháp: Chia bậc tử/mẫu → đưa về 1/n\n✅ HS hay sai: Quên chia cả tử và mẫu cho nⁿ cao nhất\n✅ Mở rộng: Liên hệ e = lim(1+1/n)ⁿ ≈ 2.718...\n✅ BTVN: Bài 1-6 trang 132 SGK"
                    }),
                "Toán|Lớp 12" => (
                    "Tích phân và ứng dụng",
                    "Nguyên hàm, tích phân, tính diện tích hình phẳng",
                    new List<string>
                    {
                        "📌 MỤC TIÊU BÀI HỌC\n• Tìm nguyên hàm các hàm số cơ bản\n• Áp dụng công thức Newton-Leibniz\n• Tính diện tích hình phẳng bằng tích phân",
                        "📖 I. NGUYÊN HÀM\n\n∫xⁿdx = xⁿ⁺¹/(n+1) + C\n∫sin(x)dx = -cos(x) + C\n∫cos(x)dx = sin(x) + C\n∫eˣdx = eˣ + C\n∫(1/x)dx = ln|x| + C",
                        "📖 II. TÍCH PHÂN\n\nCông thức Newton-Leibniz:\n    ∫ₐᵇ f(x)dx = F(b) - F(a)\n\nVí dụ: ∫₀² (x² + 1)dx = [x³/3 + x]₀² = 14/3",
                        "✍️ BÀI TẬP\n\n1. Tính: ∫₁³ (2x + 3)dx\n2. Tính diện tích giới hạn bởi y = x², Ox, x = 0, x = 2\n3. Tính diện tích giữa y = x² và y = x",
                        "📋 GHI CHÚ GIÁO VIÊN\n\n✅ Sử dụng GeoGebra minh họa diện tích\n✅ HS thường quên trị tuyệt đối khi tính diện tích\n✅ BTVN: Bài 1-8 trang 108 SGK"
                    }),

                // ════════════════════════ VẬT LÝ ════════════════════════
                "Vật lý|Lớp 10" => (
                    "Chuyển động thẳng đều",
                    "Vận tốc, quãng đường, phương trình chuyển động thẳng đều",
                    new List<string>
                    {
                        "📌 MỤC TIÊU BÀI HỌC\n• Nêu được định nghĩa chuyển động thẳng đều\n• Viết được phương trình chuyển động\n• Vẽ được đồ thị x-t, v-t\n• Giải được bài toán hai xe gặp nhau",
                        "📖 I. KIẾN THỨC\n\n🔹 Chuyển động thẳng đều: tốc độ không đổi theo thời gian\n🔹 Phương trình: x = x₀ + v·t\n🔹 Quãng đường: s = v·t\n🔹 Đồ thị x-t: đường thẳng xiên\n🔹 Đồ thị v-t: đường nằm ngang\n\n📌 Lưu ý chiều dương và gốc thời gian",
                        "📖 II. VÍ DỤ\n\nXe A xuất phát từ gốc O, v = 40 km/h\nXe B xuất phát sau 1h từ O, v = 60 km/h\n→ Hai xe gặp nhau khi: 40t = 60(t-1) → t = 3h, x = 120 km",
                        "✍️ BÀI TẬP\n\n1. Ô tô đi 120 km trong 2h. Tính v trung bình\n2. Viết phương trình x(t) cho xe xuất phát từ x₀ = 10 km, v = 30 km/h\n3. Bài toán hai xe: A đi từ HN, B đi từ HP, gặp nhau lúc nào?",
                        "📋 GHI CHÚ GIÁO VIÊN\n\n✅ Dùng mô phỏng PhET 'Moving Man'\n✅ Cho HS vẽ đồ thị trên bảng tương tác\n✅ BTVN: SBT trang 15-16"
                    }),
                "Vật lý|Lớp 11" => (
                    "Điện trường",
                    "Cường độ điện trường, đường sức điện, nguyên lý chồng chất",
                    new List<string>
                    {
                        "📌 MỤC TIÊU\n• Hiểu khái niệm điện trường\n• Tính cường độ điện trường E\n• Vẽ đường sức điện\n• Áp dụng nguyên lý chồng chất",
                        "📖 I. CƯỜNG ĐỘ ĐIỆN TRƯỜNG\n\nE = k·|Q|/r²\nk = 9·10⁹ N·m²/C²\n\n📖 II. ĐƯỜNG SỨC ĐIỆN\n• Đi ra từ (+), đi vào (-)\n• Nơi E mạnh → dày, E yếu → thưa",
                        "✍️ BÀI TẬP\n\n1. Tính E cách Q = 4·10⁻⁸ C khoảng 0.3 m\n2. Tìm E tại trung điểm 2 điện tích\n3. Vẽ đường sức cho hệ 2 điện tích cùng dấu",
                        "📋 GHI CHÚ GIÁO VIÊN\n\n✅ Dùng mô phỏng PhET 'Charges and Fields'\n✅ BTVN: Bài 4-7 trang 21 SGK"
                    }),
                "Vật lý|Lớp 12" => (
                    "Sóng cơ và giao thoa",
                    "Sóng ngang, sóng dọc, giao thoa sóng, ứng dụng",
                    new List<string>
                    {
                        "📌 MỤC TIÊU\n• Phân biệt sóng ngang/sóng dọc\n• Nắm các đại lượng: A, T, f, λ, v\n• Hiểu giao thoa sóng",
                        "📖 I. KIẾN THỨC\n\n🔹 Sóng ngang: dao động ⊥ truyền sóng (dây, mặt nước)\n🔹 Sóng dọc: dao động ∥ truyền sóng (âm thanh)\n🔹 λ = v·T = v/f\n🔹 Phương trình: u = A·cos(2πt/T - 2πx/λ)",
                        "✍️ BÀI TẬP\n\n1. f = 50 Hz, λ = 2 cm → Tính v\n2. v = 340 m/s, f = 680 Hz → Tính λ\n3. Tính số cực đại giao thoa",
                        "📋 GHI CHÚ GV: Dùng PhET 'Wave Interference' để minh họa"
                    }),

                // ════════════════════════ HÓA HỌC ════════════════════════
                "Hóa học|Lớp 10" => (
                    "Liên kết hóa học",
                    "Liên kết ion, cộng hóa trị, quy tắc octet",
                    new List<string>
                    {
                        "📌 MỤC TIÊU\n• Phân biệt liên kết ion vs cộng hóa trị\n• Giải thích sự hình thành liên kết\n• Xác định loại liên kết dựa vào ΔEN",
                        "📖 I. LIÊN KẾT ION\n• KL nhường e → ion (+), PK nhận e → ion (-)\n• ΔEN ≥ 1.7\n• VD: NaCl: Na → Na⁺ + e; Cl + e → Cl⁻\n\n📖 II. LIÊN KẾT CỘNG HÓA TRỊ\n• Dùng chung cặp electron\n• ΔEN < 1.7\n• Không cực (H₂, O₂) / Có cực (HCl, H₂O)",
                        "✍️ BÀI TẬP\n\n1. Xác định loại LK: MgO, CO₂, N₂, CaCl₂\n2. Vẽ sơ đồ Lewis: H₂O, NH₃, CH₄\n3. So sánh t° nóng chảy: NaCl vs H₂O vs kim cương",
                        "📋 GHI CHÚ GV: Dùng mô hình phân tử 3D → HS dễ hình dung"
                    }),

                // ════════════════════════ SINH HỌC ════════════════════════
                "Sinh học|Lớp 10" => (
                    "Cấu trúc tế bào",
                    "Tế bào nhân sơ, nhân thực, các bào quan",
                    new List<string>
                    {
                        "📌 MỤC TIÊU\n• Phân biệt tế bào nhân sơ và nhân thực\n• Mô tả chức năng các bào quan\n• So sánh tế bào động vật và thực vật",
                        "📖 I. TẾ BÀO NHÂN SƠ\n• Vi khuẩn: không có nhân hoàn chỉnh\n• Kích thước: 1-5 μm\n• Cấu trúc: thành TB, màng sinh chất, tế bào chất, vùng nhân\n\n📖 II. TẾ BÀO NHÂN THỰC\n• Có nhân hoàn chỉnh (2 lớp màng)\n• Các bào quan: ti thể, lưới nội chất, bộ máy Golgi\n• Thực vật: thêm lục lạp, thành xenlulozo, không bào lớn",
                        "✍️ BÀI TẬP\n\n1. Lập bảng so sánh TB nhân sơ vs nhân thực\n2. Tại sao ti thể được gọi là 'nhà máy năng lượng'?\n3. TB nào có lục lạp? Chức năng?",
                        "📋 GHI CHÚ GV: Cho xem hình ảnh kính hiển vi thực tế"
                    }),

                // ════════════════════════ NGỮ VĂN ════════════════════════
                "Ngữ văn|Lớp 10" => (
                    "Đọc hiểu văn bản",
                    "Phương pháp đọc hiểu, phân tích văn bản văn học",
                    new List<string>
                    {
                        "📌 MỤC TIÊU\n• Nắm phương pháp đọc hiểu văn bản\n• Xác định thể loại, PTBĐ, ngôi kể\n• Phân tích nội dung và nghệ thuật\n• Rèn kỹ năng viết đoạn văn nghị luận",
                        "📖 I. PHƯƠNG PHÁP ĐỌC HIỂU\n\n🔹 Bước 1: Đọc lướt → xác định thể loại, bố cục\n🔹 Bước 2: Đọc kỹ → tìm từ khóa, chi tiết quan trọng\n🔹 Bước 3: Phân tích → nội dung, nghệ thuật, ý nghĩa\n🔹 Bước 4: Đánh giá → giá trị tác phẩm, liên hệ bản thân\n\n📌 Các PTBĐ: Tự sự, Miêu tả, Biểu cảm, Nghị luận, Thuyết minh",
                        "📖 II. THỰC HÀNH ĐỌC HIỂU\n\nCho đoạn trích: [GV chọn đoạn trích phù hợp]\n\nCâu hỏi gợi ý:\n1. Xác định PTBĐ chính?\n2. Nội dung chính đoạn trích?\n3. Biện pháp tu từ nào được sử dụng?\n4. Hiệu quả nghệ thuật?",
                        "✍️ BÀI TẬP\n\nViết đoạn văn (150-200 chữ) nghị luận xã hội:\n'Suy nghĩ về tầm quan trọng của việc đọc sách trong thời đại công nghệ số'\n\n⏰ 15 phút viết → 5 phút trình bày",
                        "📋 GHI CHÚ GV\n\n✅ Chuẩn bị: Phiếu học tập, đoạn trích in sẵn\n✅ Hoạt động nhóm: 4 HS/nhóm, mỗi nhóm phân tích 1 đoạn\n✅ BTVN: Đọc trước tác phẩm tuần sau"
                    }),
                "Ngữ văn|Lớp 11" => (
                    "Nghị luận văn học",
                    "Phân tích tác phẩm thơ/truyện, viết bài nghị luận",
                    new List<string>
                    {
                        "📌 MỤC TIÊU\n• Phân tích được tác phẩm văn học theo yêu cầu\n• Viết bài nghị luận văn học hoàn chỉnh\n• Liên hệ, so sánh với tác phẩm cùng chủ đề",
                        "📖 I. CẤU TRÚC BÀI NGHỊ LUẬN VĂN HỌC\n\n🔹 Mở bài: Giới thiệu tác giả, tác phẩm, vấn đề NL\n🔹 Thân bài:\n   • Luận điểm 1 + dẫn chứng + phân tích\n   • Luận điểm 2 + dẫn chứng + phân tích\n   • Đánh giá nghệ thuật\n🔹 Kết bài: Khẳng định giá trị, liên hệ",
                        "✍️ BÀI TẬP\n\nĐề: Phân tích [tên bài thơ/truyện trong chương trình]\n\n📝 Yêu cầu:\n• Viết 600-800 chữ\n• Có ít nhất 3 luận điểm\n• Sử dụng dẫn chứng cụ thể từ văn bản",
                        "📋 GHI CHÚ GV: HS Lớp 11 cần rèn kỹ năng lập dàn ý trước khi viết"
                    }),
                "Ngữ văn|Lớp 12" => (
                    "Phân tích tác phẩm văn xuôi",
                    "Phương pháp phân tích nhân vật, cốt truyện, giá trị tác phẩm",
                    new List<string>
                    {
                        "📌 MỤC TIÊU\n• Phân tích nhân vật trong tác phẩm tự sự\n• Đánh giá giá trị hiện thực và nhân đạo\n• Viết bài nghị luận văn học đạt yêu cầu thi THPT",
                        "📖 I. PHƯƠNG PHÁP PHÂN TÍCH NHÂN VẬT\n\n🔹 Ngoại hình: dáng vẻ, cách ăn mặc\n🔹 Hành động: việc làm, quyết định\n🔹 Ngôn ngữ: lời nói, cách giao tiếp\n🔹 Tâm lý: suy nghĩ, cảm xúc, biến chuyển nội tâm\n🔹 Mối quan hệ: với nhân vật khác\n\n📌 Luôn gắn nhân vật với hoàn cảnh xã hội",
                        "📖 II. THỰC HÀNH\n\n[GV thay bằng tác phẩm đang dạy]\n\nGợi ý phân tích:\n1. Nhân vật xuất hiện trong hoàn cảnh nào?\n2. Chi tiết nào thể hiện tính cách rõ nhất?\n3. Nhân vật có sự thay đổi/biến chuyển không?\n4. Ý nghĩa tư tưởng qua nhân vật?",
                        "✍️ BÀI TẬP THI THPT\n\nĐề: Phân tích nhân vật [tên] trong tác phẩm [tên]\nThời gian: 60 phút\nYêu cầu: 800-1000 chữ, lập luận chặt chẽ",
                        "📋 GHI CHÚ GV\n\n✅ Lớp 12: Tập trung vào dạng đề thi THPT\n✅ Cho HS luyện viết tại lớp + chấm chéo\n✅ BTVN: Viết hoàn chỉnh 1 bài NL"
                    }),

                // ════════════════════════ TIẾNG ANH ════════════════════════
                "Tiếng Anh|Lớp 10" => (
                    "Reading & Vocabulary",
                    "Reading comprehension, vocabulary building, grammar practice",
                    new List<string>
                    {
                        "📌 LESSON OBJECTIVES\n• Read and understand a passage about [topic]\n• Learn 10+ new vocabulary words\n• Practice grammar: [tense/structure]\n• Develop reading strategies: skimming & scanning",
                        "📖 I. VOCABULARY\n\n🔹 Word 1 (n/v/adj): meaning — example sentence\n🔹 Word 2 (n/v/adj): meaning — example sentence\n🔹 Word 3 (n/v/adj): meaning — example sentence\n\n📌 GV: Cho HS đoán nghĩa từ ngữ cảnh trước khi giải thích",
                        "📖 II. READING PASSAGE\n\n[GV dán đoạn đọc hiểu vào đây]\n\n📝 Comprehension Questions:\n1. What is the main idea?\n2. True or False: ...\n3. Find the word in the passage that means...",
                        "📖 III. GRAMMAR FOCUS\n\n[GV chọn điểm ngữ pháp phù hợp]\n\nExamples:\n• ...\n• ...\n\nPractice: Complete the sentences",
                        "✍️ EXERCISES\n\n1. Fill in the blanks with the correct word\n2. Rewrite using the grammar structure\n3. Speaking: Discuss with partner about [topic]\n4. Writing: Write a paragraph (80-100 words)\n\n📋 HOMEWORK: Workbook page ..., exercises ..."
                    }),
                "Tiếng Anh|Lớp 11" or "Tiếng Anh|Lớp 12" => (
                    "Integrated Skills",
                    "Reading, listening, speaking and writing practice",
                    new List<string>
                    {
                        "📌 LESSON OBJECTIVES\n• Improve all 4 skills: R/L/S/W\n• Master grammar: [point]\n• Build topic vocabulary\n• Prepare for exam format",
                        "📖 I. WARM-UP & VOCABULARY (5 min)\n\n🎯 Discussion: What do you know about [topic]?\n\n📖 II. READING (15 min)\n[Passage here]\nTasks: T/F, MC, Gap-fill\n\n📖 III. GRAMMAR (10 min)\n[Grammar point + exercises]\n\n📖 IV. SPEAKING (10 min)\nRole-play / Discussion / Presentation\n\n📖 V. WRITING (5 min homework)\nWrite 120-150 words about [topic]",
                        "âœï¸ PRACTICE EXERCISES\n\n1. Reading comprehension (5 questions)\n2. Grammar exercises (10 sentences)\n3. Vocabulary matching\n4. Writing task",
                        "📋 TEACHER NOTES\n\n✅ Time allocation: 45-minute lesson\n✅ Materials: audio file, handouts\n✅ Differentiation: extra tasks for advanced students\n✅ HW: Workbook Unit X"
                    }),

                // ════════════════════════ LỊCH SỬ ════════════════════════
                "Lịch sử|Lớp 10" or "Lịch sử|Lớp 11" or "Lịch sử|Lớp 12" => (
                    $"Bài học Lịch sử {grade}",
                    "Tìm hiểu sự kiện, nhân vật, ý nghĩa lịch sử",
                    new List<string>
                    {
                        "📌 MỤC TIÊU BÀI HỌC\n• Trình bày được diễn biến sự kiện lịch sử\n• Phân tích nguyên nhân và kết quả\n• Đánh giá ý nghĩa lịch sử\n• Rút bài học kinh nghiệm",
                        "📖 I. BỐI CẢNH LỊCH SỬ\n\n[GV điền bối cảnh thời đại]\n\n📖 II. DIỄN BIẾN CHÍNH\n\n🔹 Giai đoạn 1: ...\n🔹 Giai đoạn 2: ...\n🔹 Giai đoạn 3: ...\n\n📖 III. KẾT QUẢ — Ý NGHĨA\n\n• Kết quả: ...\n• Ý nghĩa: ...\n• Bài học: ...",
                        "✍️ CÂU HỎI ÔN TẬP\n\n1. Nêu nguyên nhân sự kiện?\n2. Lập bảng niên biểu diễn biến chính\n3. Đánh giá vai trò của [nhân vật]\n4. So sánh với sự kiện [khác]\n\nTLN: 4 nhóm, mỗi nhóm 1 luận điểm",
                        "📋 GHI CHÚ GV\n\n✅ Dùng timeline trực quan trên bảng\n✅ Chiếu hình ảnh/tư liệu lịch sử\n✅ BTVN: Đọc trước bài mới + sưu tầm tư liệu"
                    }),

                // ════════════════════════ ĐỊA LÝ ════════════════════════
                "Địa lý|Lớp 10" or "Địa lý|Lớp 11" or "Địa lý|Lớp 12" => (
                    $"Bài học Địa lý {grade}",
                    "Tìm hiểu đặc điểm tự nhiên, kinh tế - xã hội",
                    new List<string>
                    {
                        "📌 MỤC TIÊU\n• Nêu được đặc điểm [vùng/quốc gia/hiện tượng]\n• Phân tích bản đồ, biểu đồ, số liệu\n• Giải thích mối quan hệ nhân — quả\n• Liên hệ thực tiễn Việt Nam",
                        "📖 I. KIẾN THỨC\n\n🔹 Vị trí địa lý: ...\n🔹 Đặc điểm tự nhiên: địa hình, khí hậu, thủy văn\n🔹 Dân cư — xã hội: ...\n🔹 Kinh tế: nông nghiệp, công nghiệp, dịch vụ\n\n📌 GV: Cho HS xác định vị trí trên bản đồ",
                        "✍️ BÀI TẬP\n\n1. Xác định vị trí trên bản đồ\n2. Vẽ biểu đồ từ bảng số liệu\n3. Giải thích hiện tượng [cụ thể]\n4. Liên hệ: ảnh hưởng đến Việt Nam?",
                        "📋 GHI CHÚ GV: Dùng Google Earth / bản đồ số để trực quan hóa"
                    }),

                // ════════════════════════ TIN HỌC ════════════════════════
                "Tin học|Lớp 10" or "Tin học|Lớp 11" or "Tin học|Lớp 12" => (
                    $"Bài học Tin học {grade}",
                    "Thuật toán, lập trình, ứng dụng CNTT",
                    new List<string>
                    {
                        "📌 MỤC TIÊU\n• Hiểu [khái niệm/thuật toán]\n• Viết được chương trình [ngôn ngữ]\n• Debug và kiểm thử\n• Áp dụng vào bài toán thực tế",
                        "📖 I. LÝ THUYẾT\n\n🔹 Khái niệm: ...\n🔹 Cú pháp: ...\n🔹 Ví dụ minh họa:\n\n```\n[Code mẫu ở đây]\n```\n\n📌 GV thao tác trực tiếp trên máy cho HS xem",
                        "✍️ BÀI TẬP THỰC HÀNH\n\n1. Viết chương trình [yêu cầu 1]\n2. Sửa lỗi chương trình cho sẵn\n3. Mở rộng: thêm tính năng [gợi ý]\n\n⏰ 20 phút thực hành trên máy",
                        "📋 GHI CHÚ GV\n\n✅ Chuẩn bị: File code mẫu, IDE cài sẵn\n✅ Chia nhóm 2 HS/máy nếu thiếu máy\n✅ BTVN: Làm thêm bài tập trên hệ thống"
                    }),

                // ════════════════════════ DEFAULT ════════════════════════
                _ => (
                    $"Bài giảng {subject} — {grade}",
                    $"Nội dung bài giảng {subject} dành cho {grade}",
                    new List<string>
                    {
                        $"📌 MỤC TIÊU BÀI HỌC\n• Mục tiêu 1: [GV điền mục tiêu kiến thức]\n• Mục tiêu 2: [GV điền mục tiêu kỹ năng]\n• Mục tiêu 3: [GV điền mục tiêu thái độ]\n• Yêu cầu cần đạt theo CT {grade}",
                        $"📖 I. KIẾN THỨC LÝ THUYẾT\n\n🔹 Khái niệm: [GV điền khái niệm chính]\n🔹 Tính chất / Đặc điểm: ...\n🔹 Công thức / Quy tắc: ...\n\n📌 Lưu ý: Môn {subject} {grade} cần chú trọng [điểm trọng tâm]",
                        "📖 II. VÍ DỤ MINH HỌA\n\nVí dụ 1: [GV thêm ví dụ cụ thể]\n→ Hướng dẫn giải: ...\n\nVí dụ 2: [GV thêm ví dụ nâng cao]\n→ Phân tích: ...",
                        "✍️ BÀI TẬP LUYỆN TẬP\n\n📝 Mức cơ bản:\n1. [Câu hỏi 1]\n2. [Câu hỏi 2]\n\n📝 Mức nâng cao:\n3. [Câu hỏi 3]\n4. [Câu hỏi 4]\n\n⏰ Thời gian: 10-15 phút",
                        $"📋 GHI CHÚ GIÁO VIÊN\n\n✅ Chuẩn bị: [danh sách TBDH]\n✅ Phương pháp: Thuyết trình + Thảo luận nhóm + Thực hành\n✅ Đánh giá: [hình thức KT]\n✅ BTVN: [giao bài]\n✅ Rút kinh nghiệm: [ghi sau tiết dạy]"
                    })
            };

            // Use current title if user has typed one
            if (!string.IsNullOrWhiteSpace(currentTitle) && currentTitle != "Hàm số bậc nhất y = ax + b")
                title = currentTitle;

            result.Add((title, desc, blocks));
            return result;
        }

        // ═══════════════════════════════════════════════════════════
        //  ADD CONTENT BLOCKS (mới — editable)
        // ═══════════════════════════════════════════════════════════

        private void AddTextBlock_Click(object sender, RoutedEventArgs e) => AddEditableTextBlock();

        private void AddImageBlock_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Chọn hình ảnh cho bài giảng",
                Filter = "Hình ảnh|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|Tất cả|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            emptyPlaceholder.Visibility = Visibility.Collapsed;
            _blockCount++;
            var filePath = dlg.FileName;
            var fileName = System.IO.Path.GetFileName(filePath);

            var blockBorder = CreateBlockContainer("🖼️ Hình ảnh", "#E8F5E9", "#2E7D32");
            var outerStack = (StackPanel)blockBorder.Child;

            // Image preview
            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.DecodePixelWidth = 600;
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                var img = new Image
                {
                    Source = bitmap, MaxHeight = 300, Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 4, 0, 4)
                };
                outerStack.Children.Add(img);
            }
            catch { /* If preview fails, just skip */ }

            // File info bar
            var infoBar = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };
            infoBar.Children.Add(new TextBlock
            {
                Text = $"📎 {fileName}", FontSize = 11, Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center
            });

            // Caption textbox
            var captionBox = new TextBox
            {
                Text = "", FontSize = 12, Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 4, 0, 0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 230, 201)),
                BorderThickness = new Thickness(1), Background = Brushes.White
            };
            captionBox.GotFocus += (s, e2) => { if (captionBox.Text == "") captionBox.Text = ""; };
            var captionLabel = new TextBlock { Text = "Chú thích ảnh (tùy chọn):", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 6, 0, 2) };
            outerStack.Children.Add(infoBar);
            outerStack.Children.Add(captionLabel);
            outerStack.Children.Add(captionBox);

            // Tag for save
            blockBorder.Tag = $"Image|{filePath}";
            blocksPanel.Children.Add(blockBorder);
            OnBlockAdded();
            Log.Information("Image block added: {File}", fileName);
        }

        private void AddVideoBlock_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Chọn video cho bài giảng",
                Filter = "Video|*.mp4;*.avi;*.mkv;*.mov;*.wmv|Tất cả|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            emptyPlaceholder.Visibility = Visibility.Collapsed;
            _blockCount++;
            var filePath = dlg.FileName;
            var fileName = System.IO.Path.GetFileName(filePath);
            var fileSize = new System.IO.FileInfo(filePath).Length;

            var blockBorder = CreateBlockContainer("🎬 Video", "#FFF3E0", "#E65100");
            var outerStack = (StackPanel)blockBorder.Child;

            // Video info panel
            var videoPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 243, 224)),
                CornerRadius = new CornerRadius(8), Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 4, 0, 4)
            };
            var vpStack = new StackPanel();
            vpStack.Children.Add(new TextBlock
            {
                Text = "🎬", FontSize = 36, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 8)
            });
            vpStack.Children.Add(new TextBlock
            {
                Text = fileName, FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            vpStack.Children.Add(new TextBlock
            {
                Text = $"Kích thước: {fileSize / 1024.0 / 1024.0:F1} MB",
                FontSize = 11, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            });
            videoPanel.Child = vpStack;
            outerStack.Children.Add(videoPanel);

            // Description textbox
            outerStack.Children.Add(new TextBlock { Text = "Mô tả nội dung video:", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 4, 0, 2) });
            var descBox = new TextBox
            {
                FontSize = 12, Padding = new Thickness(8, 6, 8, 6), Height = 50,
                AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
                BorderBrush = new SolidColorBrush(Color.FromRgb(255, 224, 178)),
                BorderThickness = new Thickness(1)
            };
            outerStack.Children.Add(descBox);

            blockBorder.Tag = $"Video|{filePath}";
            blocksPanel.Children.Add(blockBorder);
            OnBlockAdded();
            Log.Information("Video block added: {File}", fileName);
        }

        private void AddSimBlock_Click(object sender, RoutedEventArgs e)
        {
            emptyPlaceholder.Visibility = Visibility.Collapsed;
            _blockCount++;
            int currentSort = _blockCount;

            var blockBorder = CreateBlockContainer("🔬 Mô phỏng PhET", "#F3E5F5", "#7B1FA2");
            var outerStack = (StackPanel)blockBorder.Child;

            // PhET icon + description
            var simPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(243, 229, 245)),
                CornerRadius = new CornerRadius(8), Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 4, 0, 4)
            };
            var simStack = new StackPanel();
            simStack.Children.Add(new TextBlock
            {
                Text = "🔬 PhET Interactive Simulations", FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(123, 31, 162)),
                Margin = new Thickness(0, 0, 0, 4)
            });
            simStack.Children.Add(new TextBlock
            {
                Text = "Dán link mô phỏng từ phet.colorado.edu vào bên dưới",
                FontSize = 11, Foreground = Brushes.Gray
            });
            simPanel.Child = simStack;
            outerStack.Children.Add(simPanel);

            // URL input
            outerStack.Children.Add(new TextBlock { Text = "Link mô phỏng PhET:", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 6, 0, 2) });
            var urlBox = new TextBox
            {
                Text = "https://phet.colorado.edu/sims/html/", FontSize = 12,
                Padding = new Thickness(8, 6, 8, 6),
                BorderBrush = new SolidColorBrush(Color.FromRgb(206, 147, 216)),
                BorderThickness = new Thickness(1)
            };
            urlBox.GotFocus += (s, e2) =>
            {
                if (urlBox.Text == "https://phet.colorado.edu/sims/html/")
                    urlBox.SelectAll();
            };
            outerStack.Children.Add(urlBox);

            // Title input
            outerStack.Children.Add(new TextBlock { Text = "Tên mô phỏng:", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 6, 0, 2) });
            var titleBox = new TextBox
            {
                Text = "", FontSize = 12, Padding = new Thickness(8, 6, 8, 6),
                BorderBrush = new SolidColorBrush(Color.FromRgb(206, 147, 216)),
                BorderThickness = new Thickness(1)
            };
            outerStack.Children.Add(titleBox);

            // ═══ Action buttons bar ═══
            var actionsBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Right };

            // ✅ Xác nhận
            var confirmBtn = new Button
            {
                Content = "✅ Xác nhận thêm mô phỏng", FontSize = 11, Padding = new Thickness(12, 6, 12, 6),
                Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 4, 0)
            };
            confirmBtn.Click += (s, e2) =>
            {
                var url = urlBox.Text.Trim();
                if (string.IsNullOrEmpty(url) || url == "https://phet.colorado.edu/sims/html/")
                {
                    ClassroomDialog.Warn("Vui lòng nhập link mô phỏng!", "Mô phỏng");
                    return;
                }
                try
                {
                    blockBorder.Tag = $"Simulation|{url}";
                    if (_currentLessonId > 0)
                    {
                        // app → ClassroomAppContext (refactored)
                        ClassroomAppContext.Db.LessonContents.Add(new Data.LessonContent
                        {
                            LessonId = _currentLessonId,
                            ContentType = "Simulation",
                            Data = url,
                            SortOrder = currentSort - 1
                        });
                        ClassroomAppContext.Db.SaveChanges();
                    }
                    confirmBtn.Content = "✅ Đã lưu";
                    confirmBtn.IsEnabled = false;
                    Log.Information("Simulation confirmed: {Url}", url);
                }
                catch (Exception ex)
                {
                    Log.Warning("Simulation save error: {Err}", ex.Message);
                    ClassroomDialog.Warn($"Lỗi lưu: {ex.Message}", "Mô phỏng");
                }
            };
            actionsBar.Children.Add(confirmBtn);

            // 🎯 Focus HS
            var focusBtn = new Button
            {
                Content = "🎯 Focus HS", FontSize = 10, Padding = new Thickness(8, 3, 8, 3),
                Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 4, 0)
            };
            focusBtn.Click += async (s, e2) =>
            {
                try
                {
                    // app → ClassroomAppContext (refactored)
                    var net = ClassroomAppContext.Network;
                    QASmartTouch.App.FocusState.ActiveFocusSort = currentSort;
                    QASmartTouch.App.FocusState.ActiveFocusType = "Simulation";
                    if (net?.IsBroadcasting == true)
                        await net.FocusContentAsync(currentSort, "Simulation");
                    else
                        ClassroomAppContext.DispatchCommand($"CMD|LESSON_FOCUS|{currentSort}|Simulation");
                    focusBtn.Content = "✅ Đang Focus";
                    focusBtn.IsEnabled = false;
                }
                catch (Exception ex) { Log.Warning("Focus error: {Err}", ex.Message); }
            };
            actionsBar.Children.Add(focusBtn);

            // ❌ Unfocus
            var unfocusBtn = new Button
            {
                Content = "❌ Unfocus", FontSize = 10, Padding = new Thickness(8, 3, 8, 3),
                Background = new SolidColorBrush(Color.FromRgb(255, 235, 238)),
                Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 4, 0)
            };
            unfocusBtn.Click += (s, e2) =>
            {
                ResetAllBlocksFocus();
                focusBtn.Content = "🎯 Focus HS";
                focusBtn.IsEnabled = true;
            };
            actionsBar.Children.Add(unfocusBtn);

            // 🖊️ Bảng trắng
            var boardBtn = new Button
            {
                Content = "🖊️ Bảng trắng", FontSize = 10, Padding = new Thickness(8, 3, 8, 3),
                Background = new SolidColorBrush(Color.FromRgb(243, 229, 245)),
                Foreground = new SolidColorBrush(Color.FromRgb(123, 31, 162)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 4, 0),
                ToolTip = "Mở mô phỏng trên Bảng trắng SmartScreen"
            };
            boardBtn.Click += (s, e2) =>
            {
                try
                {
                    var url = urlBox.Text.Trim();
                    if (string.IsNullOrEmpty(url) || url == "https://phet.colorado.edu/sims/html/")
                    {
                        ClassroomDialog.Warn("Vui lòng nhập link mô phỏng trước!", "Mô phỏng");
                        return;
                    }
                    Window? targetWin = null;
                    foreach (Window win in Application.Current.Windows)
                    {
                        var typeName = win.GetType().Name;
                        if (typeName.Contains("MainDashboard") || typeName.Contains("Form2"))
                        { targetWin = win; break; }
                    }
                    if (targetWin == null) { ClassroomDialog.Info("Chưa mở SmartScreen.", "Bảng Trắng"); return; }
                    var canvas = targetWin.FindName("MainInteractiveBoard") as Canvas;
                    if (canvas == null) { ClassroomDialog.Warn("Không tìm thấy canvas SmartScreen.", "Bảng Trắng"); return; }

                    var simTitle = string.IsNullOrWhiteSpace(titleBox.Text) ? "PhET Simulation" : titleBox.Text.Trim();
                    double viewerW = Math.Min(canvas.ActualWidth * 0.85, 1000);
                    double viewerH = Math.Min(canvas.ActualHeight * 0.9, 750);

                    var simViewer = BuildSimViewerOnCanvas(url, simTitle, viewerW, viewerH, canvas);
                    double left = Math.Max(10, (canvas.ActualWidth - viewerW) / 2);
                    double top = Math.Max(10, (canvas.ActualHeight - viewerH) / 2);
                    Canvas.SetLeft(simViewer, left);
                    Canvas.SetTop(simViewer, top);
                    canvas.Children.Add(simViewer);

                    targetWin.Show(); targetWin.Activate(); targetWin.WindowState = WindowState.Maximized;
                    Log.Information("Simulation rendered on SmartScreen: {Url}", url);
                }
                catch (Exception ex) { Log.Warning("Sim board error: {Err}", ex.Message); }
            };
            actionsBar.Children.Add(boardBtn);

            // 🗑️ Xóa
            var delBtn = new Button
            {
                Content = "🗑️", FontSize = 14, Background = Brushes.Transparent,
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Padding = new Thickness(4)
            };
            delBtn.Click += (s, e2) => { blocksPanel.Children.Remove(blockBorder); _blockCount--; if (_blockCount == 0) emptyPlaceholder.Visibility = Visibility.Visible; };
            actionsBar.Children.Add(delBtn);

            outerStack.Children.Add(actionsBar);
            blockBorder.Tag = "Simulation|";
            blocksPanel.Children.Add(blockBorder);
            OnBlockAdded();
            Log.Information("Simulation block added");
        }

        private void AddPdfBlock_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Chọn tài liệu PDF",
                Filter = "PDF|*.pdf|Word|*.docx;*.doc|PowerPoint|*.pptx;*.ppt|Tất cả|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            emptyPlaceholder.Visibility = Visibility.Collapsed;
            _blockCount++;
            int currentSort = _blockCount;
            var filePath = dlg.FileName;
            var fileName = System.IO.Path.GetFileName(filePath);
            var fileSize = new System.IO.FileInfo(filePath).Length;
            var ext = System.IO.Path.GetExtension(filePath).ToLower();

            var bg = new SolidColorBrush(Color.FromRgb(255, 235, 238));
            var fg = new SolidColorBrush(Color.FromRgb(198, 40, 40));

            var blockBorder = new Border
            {
                Background = Brushes.White, CornerRadius = new CornerRadius(10),
                BorderBrush = fg, BorderThickness = new Thickness(1.5),
                Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(16, 14, 16, 14),
                Tag = currentSort
            };

            var outerStack = new StackPanel();

            // ── Header row: Badge + Action buttons ──
            var headerRow = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            var badge = new Border { Background = bg, CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 4, 8, 4) };
            badge.Child = new TextBlock { Text = "📄 Tài liệu", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = fg };
            DockPanel.SetDock(badge, Dock.Left);
            headerRow.Children.Add(badge);

            // Action buttons panel
            var actionsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            // 🎯 Focus HS
            var focusBtn = new Button
            {
                Content = "🎯 Focus HS", FontSize = 10, Padding = new Thickness(8, 3, 8, 3),
                Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 0), ToolTip = "Focus HS vào tài liệu này"
            };
            focusBtn.Click += async (s, e2) =>
            {
                try
                {
                    // app → ClassroomAppContext (refactored)
                    var net = ClassroomAppContext.Network;
                    QASmartTouch.App.FocusState.ActiveFocusSort = currentSort;
                    QASmartTouch.App.FocusState.ActiveFocusType = "PDF";
                    if (net?.IsBroadcasting == true)
                        await net.FocusContentAsync(currentSort, "PDF");
                    else
                        ClassroomAppContext.DispatchCommand($"CMD|LESSON_FOCUS|{currentSort}|PDF");
                    focusBtn.Content = "✅ Đang Focus";
                    focusBtn.IsEnabled = false;
                }
                catch (Exception ex) { Log.Warning("Focus error: {Err}", ex.Message); }
            };
            actionsPanel.Children.Add(focusBtn);

            // ❌ Unfocus
            var unfocusBtn = new Button
            {
                Content = "❌ Unfocus", FontSize = 10, Padding = new Thickness(8, 3, 8, 3),
                Background = new SolidColorBrush(Color.FromRgb(255, 235, 238)),
                Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 0)
            };
            unfocusBtn.Click += (s, e2) =>
            {
                ResetAllBlocksFocus();
                focusBtn.Content = "🎯 Focus HS";
                focusBtn.IsEnabled = true;
            };
            actionsPanel.Children.Add(unfocusBtn);

            // 🖊️ Bảng trắng — hiển thị PDF lên canvas SmartScreen
            var boardBtn = new Button
            {
                Content = "🖊️ Bảng trắng", FontSize = 10, Padding = new Thickness(8, 3, 8, 3),
                Background = new SolidColorBrush(Color.FromRgb(243, 229, 245)),
                Foreground = new SolidColorBrush(Color.FromRgb(123, 31, 162)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 0), ToolTip = "Mở tài liệu trên Bảng trắng SmartScreen"
            };
            boardBtn.Click += (s, e2) =>
            {
                try
                {
                    Window? targetWin = null;
                    foreach (Window win in Application.Current.Windows)
                    {
                        var typeName = win.GetType().Name;
                        if (typeName.Contains("MainDashboard") || typeName.Contains("Form2"))
                        { targetWin = win; break; }
                    }
                    if (targetWin == null)
                    {
                        ClassroomDialog.Info("Chưa mở SmartScreen. Hãy mở SmartScreen trước.", "Bảng Trắng");
                        return;
                    }
                    var canvas = targetWin.FindName("MainInteractiveBoard") as Canvas;
                    if (canvas == null)
                    {
                        ClassroomDialog.Warn("Không tìm thấy canvas SmartScreen.", "Bảng Trắng");
                        return;
                    }

                    // Hiển thị PDF trên canvas bằng WebView2 (giống Thư viện tài nguyên)
                    double viewerW = Math.Min(canvas.ActualWidth * 0.8, 900);
                    double viewerH = Math.Min(canvas.ActualHeight * 0.9, 700);

                    var pdfViewer = BuildPdfViewerOnCanvas(filePath, viewerW, viewerH, canvas);

                    double left = Math.Max(10, (canvas.ActualWidth - viewerW) / 2);
                    double top = Math.Max(10, (canvas.ActualHeight - viewerH) / 2);
                    Canvas.SetLeft(pdfViewer, left);
                    Canvas.SetTop(pdfViewer, top);
                    canvas.Children.Add(pdfViewer);

                    targetWin.Show();
                    targetWin.Activate();
                    targetWin.WindowState = WindowState.Maximized;

                    Log.Information("PDF rendered on SmartScreen: {File}", fileName);
                }
                catch (Exception ex)
                {
                    Log.Warning("PDF board error: {Err}", ex.Message);
                    ClassroomDialog.Warn($"Lỗi: {ex.Message}", "Bảng Trắng");
                }
            };
            actionsPanel.Children.Add(boardBtn);

            // 🗑️ Xóa
            var delBtn = new Button
            {
                Content = "🗑️", FontSize = 14, Background = Brushes.Transparent,
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Padding = new Thickness(4)
            };
            delBtn.Click += (s, e2) =>
            {
                blocksPanel.Children.Remove(blockBorder);
                _blockCount--;
                if (_blockCount == 0) emptyPlaceholder.Visibility = Visibility.Visible;
            };
            actionsPanel.Children.Add(delBtn);

            DockPanel.SetDock(actionsPanel, Dock.Right);
            headerRow.Children.Add(actionsPanel);
            outerStack.Children.Add(headerRow);

            // ── Document info panel ──
            var docIcon = ext switch { ".pdf" => "📄", ".docx" or ".doc" => "📝", ".pptx" or ".ppt" => "📊", _ => "📁" };
            var docPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 235, 238)),
                CornerRadius = new CornerRadius(8), Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 4, 0, 4)
            };
            var dpStack = new StackPanel { Orientation = Orientation.Horizontal };
            dpStack.Children.Add(new TextBlock { Text = docIcon, FontSize = 32, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) });
            var infoStack = new StackPanel();
            infoStack.Children.Add(new TextBlock
            {
                Text = fileName, FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40))
            });
            infoStack.Children.Add(new TextBlock
            {
                Text = $"Loại: {ext.ToUpper().TrimStart('.')} · Kích thước: {fileSize / 1024.0:F0} KB",
                FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 2, 0, 0)
            });
            dpStack.Children.Add(infoStack);
            docPanel.Child = dpStack;
            outerStack.Children.Add(docPanel);

            // ── Notes ──
            outerStack.Children.Add(new TextBlock { Text = "Ghi chú tài liệu:", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 6, 0, 2) });
            var noteBox = new TextBox
            {
                FontSize = 12, Padding = new Thickness(8, 6, 8, 6), Height = 50,
                AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
                BorderBrush = new SolidColorBrush(Color.FromRgb(239, 154, 154)),
                BorderThickness = new Thickness(1)
            };
            outerStack.Children.Add(noteBox);

            // ── Confirm button ──
            var confirmBtn = new Button
            {
                Content = "✅ Xác nhận thêm tài liệu", FontSize = 12, FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(0, 10, 0, 10), Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand
            };
            confirmBtn.Click += (s, e2) =>
            {
                confirmBtn.Content = "✅ Đã xác nhận";
                confirmBtn.IsEnabled = false;
                confirmBtn.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201));
                confirmBtn.Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32));
                blockBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));

                // Lưu vào DB nếu đang edit bài cụ thể
                try
                {
                    if (_currentLessonId > 0)
                    {
                        // app → ClassroomAppContext (refactored)
                        ClassroomAppContext.Db.LessonContents.Add(new Data.LessonContent
                        {
                            LessonId = _currentLessonId,
                            ContentType = "PDF",
                            Data = filePath,
                            SortOrder = currentSort - 1
                        });
                        ClassroomAppContext.Db.SaveChanges();
                        Log.Information("PDF block saved to DB: {File}", fileName);
                    }
                }
                catch (Exception ex) { Log.Warning("Save PDF block error: {Err}", ex.Message); }
            };
            outerStack.Children.Add(confirmBtn);

            blockBorder.Child = outerStack;
            blockBorder.Tag = $"PDF|{filePath}";
            blocksPanel.Children.Add(blockBorder);
            OnBlockAdded();
            Log.Information("Document block added: {File}", fileName);
        }

        // ═══════════════════════════════════════════════════════════
        //  📄 WORD TEMPLATE — Tải mẫu Word bài giảng
        // ═══════════════════════════════════════════════════════════

        private void DownloadWordTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var subject = (cmbSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Toán";
                var grade = (cmbGrade.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Lớp 10";
                var title = string.IsNullOrWhiteSpace(txtTitle.Text) ? $"Bài giảng {subject} {grade}" : txtTitle.Text;

                var filePath = Services.LessonWordService.GenerateTemplate(subject, grade, title);

                var result = MessageBox.Show(
                    $"✅ Đã tạo file Word mẫu thành công!\n\n📄 {System.IO.Path.GetFileName(filePath)}\n📁 {System.IO.Path.GetDirectoryName(filePath)}\n\n" +
                    "Hướng dẫn:\n" +
                    "1. Mở file Word → Điền nội dung vào từng phần\n" +
                    "2. KHÔNG xóa các dòng [TÊN SECTION]\n" +
                    "3. Lưu file → Dùng nút \"📥 Import Word\" để nhập\n\n" +
                    "Bạn có muốn mở file ngay không?",
                    "📄 Tải mẫu bài giảng Word",
                    MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Warning("DownloadWordTemplate error: {Err}", ex.Message);
                ClassroomDialog.Warn($"Lỗi tạo file Word: {ex.Message}", "Lỗi");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  📥 IMPORT WORD — Nhập bài giảng từ file Word
        // ═══════════════════════════════════════════════════════════

        private void ImportWordFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Word Documents|*.docx|All Files|*.*",
                    Title = "📥 Import bài giảng từ file Word"
                };

                if (dlg.ShowDialog() != true) return;

                var (title, subject, grade, blocks) = Services.LessonWordService.ImportFromWord(dlg.FileName);

                if (blocks.Count == 0)
                {
                    ClassroomDialog.Warn("Không tìm thấy nội dung bài giảng trong file Word.\n\n" +
                        "Hãy đảm bảo file có cấu trúc [SECTION MARKER] đúng format.\n" +
                        "Dùng nút \"📄 Tải mẫu Word\" để lấy file mẫu.", "Không có nội dung");
                    return;
                }

                // Ask user to confirm
                var confirm = MessageBox.Show(
                    $"📥 File: {System.IO.Path.GetFileName(dlg.FileName)}\n\n" +
                    $"📌 Tiêu đề: {(string.IsNullOrEmpty(title) ? "(không xác định)" : title)}\n" +
                    $"📚 Môn: {(string.IsNullOrEmpty(subject) ? "(không xác định)" : subject)}\n" +
                    $"🎓 Lớp: {(string.IsNullOrEmpty(grade) ? "(không xác định)" : grade)}\n" +
                    $"📝 Số block nội dung: {blocks.Count}\n\n" +
                    "Bạn có muốn import vào bài giảng hiện tại?",
                    "📥 Import Word",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                // Update fields if available
                if (!string.IsNullOrWhiteSpace(title)) txtTitle.Text = title;
                if (!string.IsNullOrWhiteSpace(subject))
                {
                    for (int i = 0; i < cmbSubject.Items.Count; i++)
                        if ((cmbSubject.Items[i] as ComboBoxItem)?.Content?.ToString() == subject)
                        { cmbSubject.SelectedIndex = i; break; }
                }
                if (!string.IsNullOrWhiteSpace(grade))
                {
                    for (int i = 0; i < cmbGrade.Items.Count; i++)
                        if ((cmbGrade.Items[i] as ComboBoxItem)?.Content?.ToString() == grade)
                        { cmbGrade.SelectedIndex = i; break; }
                }

                // Render blocks
                emptyPlaceholder.Visibility = Visibility.Collapsed;
                foreach (var block in blocks)
                {
                    RenderContentBlock(block.ContentType, block.Data);
                }

                ClassroomDialog.Info(
                    $"✅ Import thành công!\n\n" +
                    $"Đã thêm {blocks.Count} block nội dung vào bài giảng.\n" +
                    "Nhấn \"💾 Lưu bài giảng\" để lưu vào hệ thống.", "Import Word thành công");

                Log.Information("Word imported: {File} → {Count} blocks", dlg.FileName, blocks.Count);
            }
            catch (Exception ex)
            {
                Log.Warning("ImportWordFile error: {Err}", ex.Message);
                ClassroomDialog.Warn($"Lỗi import file Word: {ex.Message}", "Lỗi");
            }
        }

        private void AddQuizBlock_Click(object sender, RoutedEventArgs e)
        {
            emptyPlaceholder.Visibility = Visibility.Collapsed;
            _blockCount++;
            int currentSort = _blockCount;

            var blockBorder = CreateBlockContainer("❓ Câu hỏi Quiz", "#E0F2F1", "#00695C");
            var outerStack = (StackPanel)blockBorder.Child;

            // Question input
            outerStack.Children.Add(new TextBlock { Text = "Câu hỏi:", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92)), Margin = new Thickness(0, 4, 0, 4) });
            var questionBox = new TextBox
            {
                FontSize = 13, Padding = new Thickness(10, 8, 10, 8), Height = 50,
                AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
                BorderBrush = new SolidColorBrush(Color.FromRgb(128, 203, 196)),
                BorderThickness = new Thickness(1.5), Text = "Nhập câu hỏi tại đây..."
            };
            questionBox.GotFocus += (s, e2) => { if (questionBox.Text == "Nhập câu hỏi tại đây...") questionBox.Clear(); };
            outerStack.Children.Add(questionBox);

            // Answer options
            outerStack.Children.Add(new TextBlock { Text = "Đáp án:", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92)), Margin = new Thickness(0, 8, 0, 4) });

            var answersPanel = new StackPanel();
            var labels = new[] { "A", "B", "C", "D" };
            var optColors = new[] { "#E3F2FD", "#E8F5E9", "#FFF3E0", "#FCE4EC" };
            var optBorders = new[] { "#90CAF9", "#A5D6A7", "#FFCC80", "#F48FB1" };
            var radioButtons = new RadioButton[4];
            var answerBoxes = new TextBox[4];

            for (int i = 0; i < 4; i++)
            {
                var optRow = new DockPanel { Margin = new Thickness(0, 2, 0, 2) };
                var radio = new RadioButton
                {
                    GroupName = $"quiz_{_blockCount}", VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0), IsChecked = i == 0, ToolTip = "Đáp án đúng"
                };
                radioButtons[i] = radio;
                DockPanel.SetDock(radio, Dock.Left);
                optRow.Children.Add(radio);

                var labelBadge = new Border
                {
                    Background = (SolidColorBrush)new BrushConverter().ConvertFrom(optColors[i])!,
                    CornerRadius = new CornerRadius(4), Width = 28, Height = 28,
                    Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center
                };
                labelBadge.Child = new TextBlock { Text = labels[i], FontSize = 13, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                DockPanel.SetDock(labelBadge, Dock.Left);
                optRow.Children.Add(labelBadge);

                var ansBox = new TextBox
                {
                    FontSize = 12, Padding = new Thickness(8, 5, 8, 5),
                    BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(optBorders[i])!,
                    BorderThickness = new Thickness(1), Text = $"Đáp án {labels[i]}..."
                };
                ansBox.GotFocus += (s, e2) => { var tb = s as TextBox; if (tb != null && tb.Text.StartsWith("Đáp án ")) tb.Clear(); };
                answerBoxes[i] = ansBox;
                optRow.Children.Add(ansBox);
                answersPanel.Children.Add(optRow);
            }
            outerStack.Children.Add(answersPanel);

            // Points + Difficulty
            var metaRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            metaRow.Children.Add(new TextBlock { Text = "Điểm:", FontSize = 11, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) });
            var pointsBox = new TextBox { Text = "10", Width = 50, FontSize = 12, Padding = new Thickness(6, 4, 6, 4), BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1) };
            metaRow.Children.Add(pointsBox);
            metaRow.Children.Add(new TextBlock { Text = "Độ khó:", FontSize = 11, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 4, 0) });
            var diffCombo = new ComboBox { FontSize = 11, SelectedIndex = 1, Width = 100 };
            diffCombo.Items.Add(new ComboBoxItem { Content = "Dễ" });
            diffCombo.Items.Add(new ComboBoxItem { Content = "Trung bình" });
            diffCombo.Items.Add(new ComboBoxItem { Content = "Khó" });
            metaRow.Children.Add(diffCombo);
            outerStack.Children.Add(metaRow);

            // Helper: build quiz data string  → "question;;A||B||C||D;;correctIdx;;points;;difficulty"
            string BuildQuizData()
            {
                int correctIdx = 0;
                for (int i = 0; i < 4; i++) { if (radioButtons[i].IsChecked == true) { correctIdx = i; break; } }
                var q = questionBox.Text.Trim();
                var opts = string.Join("||", answerBoxes.Select(b => b.Text.Trim()));
                return $"{q};;{opts};;{correctIdx};;{pointsBox.Text};;{(diffCombo.SelectedItem as ComboBoxItem)?.Content}";
            }

            // ═══ Action buttons bar ═══
            var actionsBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Right };

            // ✅ Xác nhận
            var confirmBtn = new Button
            {
                Content = "✅ Xác nhận câu hỏi", FontSize = 11, Padding = new Thickness(12, 6, 12, 6),
                Background = new SolidColorBrush(Color.FromRgb(224, 242, 241)),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 4, 0)
            };
            confirmBtn.Click += (s, e2) =>
            {
                var q = questionBox.Text.Trim();
                if (string.IsNullOrEmpty(q) || q == "Nhập câu hỏi tại đây...")
                { ClassroomDialog.Warn("Vui lòng nhập câu hỏi!", "Quiz"); return; }
                try
                {
                    var data = BuildQuizData();
                    blockBorder.Tag = $"Quiz|{data}";
                    if (_currentLessonId > 0)
                    {
                        // app → ClassroomAppContext (refactored)
                        ClassroomAppContext.Db.LessonContents.Add(new Data.LessonContent
                        {
                            LessonId = _currentLessonId, ContentType = "Quiz",
                            Data = data, SortOrder = currentSort - 1
                        });
                        ClassroomAppContext.Db.SaveChanges();
                    }
                    confirmBtn.Content = "✅ Đã lưu"; confirmBtn.IsEnabled = false;
                    Log.Information("Quiz confirmed: {Q}", q);
                }
                catch (Exception ex) { Log.Warning("Quiz save error: {Err}", ex.Message); }
            };
            actionsBar.Children.Add(confirmBtn);

            // 🎯 Focus HS
            var focusBtn = new Button
            {
                Content = "🎯 Focus HS", FontSize = 10, Padding = new Thickness(8, 3, 8, 3),
                Background = new SolidColorBrush(Color.FromRgb(224, 242, 241)),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 4, 0)
            };
            focusBtn.Click += async (s, e2) =>
            {
                try
                {
                    // app → ClassroomAppContext (refactored)
                    var net = ClassroomAppContext.Network;
                    QASmartTouch.App.FocusState.ActiveFocusSort = currentSort; QASmartTouch.App.FocusState.ActiveFocusType = "Quiz";
                    if (net?.IsBroadcasting == true) await net.FocusContentAsync(currentSort, "Quiz");
                    else ClassroomAppContext.DispatchCommand($"CMD|LESSON_FOCUS|{currentSort}|Quiz");
                    focusBtn.Content = "✅ Đang Focus"; focusBtn.IsEnabled = false;
                }
                catch (Exception ex) { Log.Warning("Focus error: {Err}", ex.Message); }
            };
            actionsBar.Children.Add(focusBtn);

            // ❌ Unfocus
            var unfocusBtn = new Button
            {
                Content = "❌ Unfocus", FontSize = 10, Padding = new Thickness(8, 3, 8, 3),
                Background = new SolidColorBrush(Color.FromRgb(255, 235, 238)),
                Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 4, 0)
            };
            unfocusBtn.Click += (s, e2) => { ResetAllBlocksFocus(); focusBtn.Content = "🎯 Focus HS"; focusBtn.IsEnabled = true; };
            actionsBar.Children.Add(unfocusBtn);

            // 🖊️ Bảng trắng
            var boardBtn = new Button
            {
                Content = "🖊️ Bảng trắng", FontSize = 10, Padding = new Thickness(8, 3, 8, 3),
                Background = new SolidColorBrush(Color.FromRgb(224, 242, 241)),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 4, 0),
                ToolTip = "Hiển thị câu hỏi trên Bảng trắng SmartScreen"
            };
            boardBtn.Click += (s, e2) =>
            {
                try
                {
                    var q = questionBox.Text.Trim();
                    if (string.IsNullOrEmpty(q) || q == "Nhập câu hỏi tại đây...")
                    { ClassroomDialog.Warn("Vui lòng nhập câu hỏi trước!", "Quiz"); return; }
                    Window? targetWin = null;
                    foreach (Window win in Application.Current.Windows)
                    {
                        var tn = win.GetType().Name;
                        if (tn.Contains("MainDashboard") || tn.Contains("Form2")) { targetWin = win; break; }
                    }
                    if (targetWin == null) { ClassroomDialog.Info("Chưa mở SmartScreen.", "Bảng Trắng"); return; }
                    var canvas = targetWin.FindName("MainInteractiveBoard") as Canvas;
                    if (canvas == null) { ClassroomDialog.Warn("Không tìm thấy canvas.", "Bảng Trắng"); return; }

                    var data = BuildQuizData();
                    double cardW = Math.Min(canvas.ActualWidth * 0.6, 700);
                    var quizCard = BuildQuizCardOnCanvas(data, cardW, canvas);
                    Canvas.SetLeft(quizCard, Math.Max(20, (canvas.ActualWidth - cardW) / 2));
                    Canvas.SetTop(quizCard, Math.Max(20, (canvas.ActualHeight - 500) / 2));
                    canvas.Children.Add(quizCard);
                    targetWin.Show(); targetWin.Activate(); targetWin.WindowState = WindowState.Maximized;
                    Log.Information("Quiz card placed on SmartScreen");
                }
                catch (Exception ex) { Log.Warning("Quiz board error: {Err}", ex.Message); }
            };
            actionsBar.Children.Add(boardBtn);

            // 🗑️ Xóa
            var delBtn = new Button { Content = "🗑️", FontSize = 14, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Padding = new Thickness(4) };
            delBtn.Click += (s, e2) => { blocksPanel.Children.Remove(blockBorder); _blockCount--; if (_blockCount == 0) emptyPlaceholder.Visibility = Visibility.Visible; };
            actionsBar.Children.Add(delBtn);

            outerStack.Children.Add(actionsBar);
            blockBorder.Tag = "Quiz|inline";
            blocksPanel.Children.Add(blockBorder);
            OnBlockAdded();
            Log.Information("Quiz block added");
        }

        /// <summary>
        /// Helper: Táº¡o block container chung vá»›i header + delete button
        /// </summary>
        private Border CreateBlockContainer(string label, string bgHex, string fgHex)
        {
            var bg = (SolidColorBrush)new BrushConverter().ConvertFrom(bgHex)!;
            var fg = (SolidColorBrush)new BrushConverter().ConvertFrom(fgHex)!;

            var blockBorder = new Border
            {
                Background = Brushes.White, CornerRadius = new CornerRadius(10),
                BorderBrush = fg, BorderThickness = new Thickness(1.5),
                Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(16, 14, 16, 14)
            };

            var outerStack = new StackPanel();

            // Header row
            var headerRow = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            var badge = new Border
            {
                Background = bg, CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4, 8, 4)
            };
            badge.Child = new TextBlock { Text = label, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = fg };
            DockPanel.SetDock(badge, Dock.Left);
            headerRow.Children.Add(badge);

            var delBtn = new Button
            {
                Content = "🗑️", FontSize = 14, Background = Brushes.Transparent,
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Padding = new Thickness(4), HorizontalAlignment = HorizontalAlignment.Right
            };
            delBtn.Click += (s, e) =>
            {
                blocksPanel.Children.Remove(blockBorder);
                _blockCount--;
                if (_blockCount == 0) emptyPlaceholder.Visibility = Visibility.Visible;
            };
            DockPanel.SetDock(delBtn, Dock.Right);
            headerRow.Children.Add(delBtn);
            outerStack.Children.Add(headerRow);

            blockBorder.Child = outerStack;
            return blockBorder;
        }

        /// <summary>
        /// Thêm block văn bản mới — RichTextBox editable
        /// </summary>
        private void AddEditableTextBlock() => AddEditableTextBlock(string.Empty);

        private void AddEditableTextBlock(string initialText)
        {
            emptyPlaceholder.Visibility = Visibility.Collapsed;
            _blockCount++;

            var blockBorder = new Border
            {
                Background = Brushes.White, CornerRadius = new CornerRadius(10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                BorderThickness = new Thickness(1.5), Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(16, 14, 16, 14)
            };

            var outerStack = new StackPanel();

            // Header
            var headerRow = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            var badge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(227, 242, 253)),
                CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 4, 8, 4)
            };
            badge.Child = new TextBlock { Text = "🔤 Văn bản (mới)", FontSize = 11, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)) };
            DockPanel.SetDock(badge, Dock.Left);
            headerRow.Children.Add(badge);

            var delBtn = new Button { Content = "🗑️", FontSize = 14, Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand, Padding = new Thickness(4), HorizontalAlignment = HorizontalAlignment.Right };
            delBtn.Click += (s, e) => { blocksPanel.Children.Remove(blockBorder); _blockCount--; if (_blockCount == 0) emptyPlaceholder.Visibility = Visibility.Visible; };
            DockPanel.SetDock(delBtn, Dock.Right);
            headerRow.Children.Add(delBtn);
            outerStack.Children.Add(headerRow);

            // Editing toolbar
            var toolbar = CreateMiniToolbar();
            outerStack.Children.Add(toolbar);

            // Editable RichTextBox
            var rtb = new RichTextBox
            {
                MinHeight = 80,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                Padding = new Thickness(8),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                AcceptsReturn = true
            };
            rtb.Document.Blocks.Clear();
            if (!string.IsNullOrWhiteSpace(initialText))
            {
                // ═══ Rich Template Rendering ═══
                FormatRichContent(rtb.Document, initialText);
            }
            else
            {
                var p = new Paragraph();
                p.Inlines.Add(new Run("Nhập nội dung bài giảng tại đây...") { Foreground = Brushes.LightGray });
                rtb.Document.Blocks.Add(p);
                rtb.GotFocus += (s, e) =>
                {
                    var text = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd).Text.Trim();
                    if (text == "Nhập nội dung bài giảng tại đây...")
                    {
                        rtb.Document.Blocks.Clear();
                        rtb.Document.Blocks.Add(new Paragraph());
                    }
                };
            }

            // Wire toolbar buttons to RichTextBox
            WireMiniToolbar(toolbar, rtb);

            outerStack.Children.Add(rtb);
            blockBorder.Child = outerStack;
            blocksPanel.Children.Add(blockBorder);
            OnBlockAdded();

            // Scroll to new block
            rtb.Focus();
            Log.Information("New editable text block added, total: {Count}", _blockCount);
        }

        // ── Toolbar Methods → LessonEditorPage.Toolbar.cs ────────────────────
        // CreateMiniToolbar, MakeToolbarBtn, MakeColorBtn, MakeHighlightBtn,
        // MakeClearFormatBtn, MakeSeparator, WireMiniToolbar




        // ═══════════════════════════════════════════════════════════
        //  ACTIONS
        // ═══════════════════════════════════════════════════════════

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            var actualBlocks = blocksPanel.Children.Cast<UIElement>().Where(c => c != emptyPlaceholder).ToList();
            if (actualBlocks.Count == 0) return;
            
            var last = actualBlocks[actualBlocks.Count - 1];
            blocksPanel.Children.Remove(last);
            _redoStack.Push(last);
            
            _blockCount = Math.Max(0, _blockCount - 1);
            if (_blockCount == 0) emptyPlaceholder.Visibility = Visibility.Visible;
            
            UpdateStats();
            UpdateUndoRedoButtonsState();
            Log.Information("Undo: removed block, remaining: {Count}", _blockCount);
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (_redoStack.Count == 0) return;
            var block = _redoStack.Pop();
            emptyPlaceholder.Visibility = Visibility.Collapsed;
            blocksPanel.Children.Add(block);
            
            _blockCount++;
            UpdateStats();
            UpdateUndoRedoButtonsState();
            Log.Information("Redo: restored block, total: {Count}", _blockCount);
        }


        /// <summary>
        /// Tìm child element theo type trong visual tree
        /// </summary>
        private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result) return result;
                var found = FindChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// Cập nhật Stats panel bên phải
        /// </summary>
        private void UpdateStats()
        {
            try
            {
                var blockCountActual = blocksPanel.Children.Cast<UIElement>()
                    .Count(c => c is Border b && b != emptyPlaceholder);
                txtStats.Text = $"📝 Blocks: {blockCountActual}\n🕐 Tạo lúc: {DateTime.Now:dd/MM/yyyy}\n✏️ Sửa lần cuối: Vừa xong";
                LoadAiSuggestions(); // Refresh AI status text too
            }
            catch { }
        }

        /// <summary>
        /// Cập nhật trạng thái hiển thị khả dụng của nút bấm Undo/Redo
        /// </summary>
        private void UpdateUndoRedoButtonsState()
        {
            try
            {
                var actualBlockCount = blocksPanel.Children.Cast<UIElement>().Count(c => c != emptyPlaceholder);
                if (btnUndo != null)
                {
                    btnUndo.IsEnabled = actualBlockCount > 0;
                }
                if (btnRedo != null)
                {
                    btnRedo.IsEnabled = _redoStack.Count > 0;
                }
            }
            catch { }
        }

        private void OnBlockAdded()
        {
            _redoStack.Clear();
            UpdateUndoRedoButtonsState();
        }

        /// <summary>
        /// Mở bài giảng trên Bảng vẽ — truyền nội dung lesson lên CanvasPage
        /// </summary>
        private void OpenOnWhiteboard_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLessonId <= 0)
            {
                ClassroomDialog.Warn("Vui lòng lưu bài giảng trước khi mở trên bảng!", "Thông báo");
                return;
            }

            if (Window.GetWindow(this) is ClassroomShell shell)
                shell.NavigateToCanvas(_currentLessonId);
        }

        // ═══════════════════════════════════════════════════════════
        //  SUBMIT FOR APPROVAL
        // ═══════════════════════════════════════════════════════════

        private void SubmitApproval_Click(object sender, RoutedEventArgs e)
        {
            // Auto-save first
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                ClassroomDialog.Warn("Vui lòng nhập tiêu đề trước khi gửi phê duyệt!", "Thiếu thông tin");
                return;
            }

            // Save and set status = PendingApproval
            try
            {
                // app → ClassroomAppContext (refactored)
                Lesson lesson;
                if (_currentLessonId > 0)
                    lesson = ClassroomAppContext.Db.Lessons.Find(_currentLessonId) ?? new Lesson();
                else
                {
                    lesson = new Lesson { CreatedAt = DateTime.Now };
                    ClassroomAppContext.Db.Lessons.Add(lesson);
                }

                lesson.Title       = txtTitle.Text.Trim();
                lesson.Subject     = (cmbSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Toán";
                lesson.Grade       = (cmbGrade.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Lớp 10";
                lesson.Description = txtDescription.Text.Trim();
                lesson.UpdatedAt   = DateTime.Now;
                lesson.Status      = "PendingApproval";

                ClassroomAppContext.Db.SaveChanges();
                _currentLessonId = lesson.Id;

                UpdateStatusBadge("PendingApproval");

                ClassroomDialog.Info(
                    $"📤 Đã gửi bài giảng \"{lesson.Title}\" lên Tổ Chuyên Môn!\n\n" +
                    "• Tổ trưởng sẽ nhận thông báo và review trong 3-5 ngày\n" +
                    "• Trạng thái: Chờ phê duyệt ⏳", "Gửi Phê Duyệt");

                Log.Information("Lesson submitted for approval: {Title} (ID={Id})", lesson.Title, lesson.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SubmitApproval error");
                ClassroomDialog.Error($"Lỗi: {ex.Message}", "Lỗi");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  START TEACHING DIRECTLY
        // ═══════════════════════════════════════════════════════════

        private void StartTeaching_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLessonId <= 0)
            {
                var result = MessageBox.Show("Bài giảng chưa được lưu. Lưu ngay và bắt đầu dạy?",
                    "Chưa lưu", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
                Save_Click(sender, e);
                if (_currentLessonId <= 0) return; // save failed
            }

            if (Window.GetWindow(this) is ClassroomShell shell)
                shell.NavigateToRunner(_currentLessonId);
        }

        // ═══════════════════════════════════════════════════════════
        //  STATUS BADGE
        // ═══════════════════════════════════════════════════════════

        private void UpdateStatusBadge(string status)
        {
            try
            {
                var (bg, fg, label) = status switch
                {
                    "Draft"           => ("#FFF9C4", "#F57F17", "📝 Bản nháp"),
                    "PendingApproval" => ("#FFF3E0", "#E65100", "⏳ Chờ phê duyệt"),
                    "Approved"        => ("#E8F5E9", "#2E7D32", "✅ Đã phê duyệt"),
                    "Taught"          => ("#E3F2FD", "#1565C0", "📚 Đã dạy"),
                    _                 => ("#F5F5F5", "#757575", status)
                };
                statusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)!);
                txtStatus.Foreground   = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg)!);
                txtStatus.Text         = label;
            }
            catch { /* ignore color parse errors */ }
        }

        // ═══════════════════════════════════════════════════════════
        //  🤖 AI SUGGESTIONS (Data-driven, no API needed)
        // ═══════════════════════════════════════════════════════════

        private static readonly List<AiSuggestionItem> _aiSuggestionBank = new()
        {
            new() { Title="🎮 Thêm trò chơi Kahoot", Detail="Chèn block Game vào cuối bài để HS ôn tập tương tác", Action="AddGame",   Bg="#F3E5F5", Fg="#6A1B9A" },
            new() { Title="❓ Thêm câu hỏi kiểm tra", Detail="Chèn 3 câu hỏi trắc nghiệm ngắn cuối bài giảng",    Action="AddQuiz",   Bg="#E3F2FD", Fg="#1565C0" },
            new() { Title="🖼️ Thêm hình minh họa",  Detail="Chèn block hình ảnh để trực quan hóa khái niệm",      Action="AddImage",  Bg="#E8F5E9", Fg="#2E7D32" },
            new() { Title="⏱️ Điều chỉnh thời lượng", Detail="Bài hiện tại ước tính > 45 phút — nên rút gọn",     Action="FixTime",   Bg="#FFF3E0", Fg="#E65100" },
            new() { Title="📊 Thêm sơ đồ tư duy",   Detail="Chèn Mind Map tóm tắt cuối bài giảng",               Action="AddMindmap",Bg="#E8EAF6", Fg="#283593" },
            new() { Title="📹 Thêm video minh họa",  Detail="Chèn block Video ngắn (2-3 phút) để dẫn vào bài",    Action="AddVideo",  Bg="#FCE4EC", Fg="#880E4F" },
            new() { Title="✍️ Thêm bài tập thực hành", Detail="HS cần luyện tập — thêm block bài tập tự làm",    Action="AddExercise",Bg="#E0F7FA", Fg="#006064" },
            new() { Title="🌟 Thêm câu mở đầu hấp dẫn", Detail="Bắt đầu bài bằng 1 tình huống thực tế thú vị",  Action="AddHook",   Bg="#FFFDE7", Fg="#F57F17" },
            new() { Title="📌 Thêm mục tiêu bài học", Detail="Block đầu tiên nên là mục tiêu rõ ràng cho HS",     Action="AddGoals",  Bg="#F9FBE7", Fg="#558B2F" },
            new() { Title="💬 Thêm hoạt động thảo luận", Detail="Chèn prompt thảo luận nhóm 5 phút",             Action="AddDiscuss",Bg="#E8F5E9", Fg="#1B5E20" },
            new() { Title="📝 Giao bài về nhà",      Detail="Thêm phần BTVN rõ ràng cuối bài giảng",             Action="AddHW",     Bg="#EFEBE9", Fg="#4E342E" },
            new() { Title="🔗 Liên kết tài nguyên",   Detail="Thêm đường link video YouTube / tài liệu liên quan",Action="AddLink",   Bg="#E3F2FD", Fg="#0D47A1" },
        };

        private void LoadAiSuggestions()
        {
            // Pick 4 random suggestions based on current content count
            int blockCount = _blockCount;
            var rnd = new Random();
            var selected = _aiSuggestionBank
                .OrderBy(_ => rnd.Next())
                .Take(4)
                .ToList();

            aiSuggestionsList.ItemsSource = selected;
            txtAiStatus.Text = blockCount switch
            {
                0     => "📭 Bài trống — hãy thêm nội dung!",
                <= 2  => $"📄 {blockCount} block — nên thêm thêm nội dung",
                <= 5  => $"✅ {blockCount} block — bài giảng đang tốt",
                _     => $"🌟 {blockCount} block — bài giảng phong phú!"
            };
        }

        private void RefreshAiSuggestions_Click(object sender, RoutedEventArgs e) => LoadAiSuggestions();

        private void AiSuggestion_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is AiSuggestionItem item)
            {
                string insertedContent = item.Action switch
                {
                    "AddGame"     => "🎮 HOẠT ĐỘNG GAME\n\nKahoot / Quizziz:\n• Link game: [GV nhập link]\n• Thời gian: 5-7 phút\n• Mục đích: Ôn tập kiến thức vừa học",
                    "AddQuiz"     => "❓ CÂU HỎI KIỂM TRA NHANH\n\n1. [Câu hỏi 1] — Đáp án: ___\n2. [Câu hỏi 2] — Đáp án: ___\n3. [Câu hỏi 3] — Đáp án: ___\n\nThời gian: 3 phút",
                    "AddMindmap"  => "🧠 SƠ ĐỒ TƯ DUY\n\n[Chủ đề chính]\n   ├── Nhánh 1: ...\n   ├── Nhánh 2: ...\n   ├── Nhánh 3: ...\n   └── Nhánh 4: ...",
                    "AddExercise" => "✍️ BÀI TẬP THỰC HÀNH\n\nBài 1: [Đề bài]\nBài 2: [Đề bài]\nBài 3: [Đề bài — nâng cao]\n\nThời gian: 10 phút",
                    "AddHook"     => "🌟 KHỞI ĐỘNG\n\nTình huống thực tế:\n[GV đặt vấn đề hấp dẫn liên quan đến bài học]\n\nCâu hỏi dẫn dắt:\n• Theo em, ... ?\n• Tại sao ... ?",
                    "AddGoals"    => "📌 MỤC TIÊU BÀI HỌC\n\nSau bài học này, HS có thể:\n✅ Hiểu được: [kiến thức 1]\n✅ Biết cách: [kỹ năng 1]\n✅ Vận dụng: [vào thực tế]",
                    "AddDiscuss"  => "💬 HOẠT ĐỘNG THẢO LUẬN\n\nCho HS thảo luận nhóm 2-3 người:\n\nCâu hỏi: [GV nhập câu hỏi]\n\nThời gian: 5 phút\nBáo cáo: Đại diện nhóm trình bày",
                    "AddHW"       => "📝 BÀI TẬP VỀ NHÀ\n\n1. [Bài tập cơ bản]\n2. [Bài tập vận dụng]\n3. [Bài tập nâng cao — tự chọn]\n\nNộp bài: Tiết học sau",
                    "AddLink"     => "🔗 TÀI NGUYÊN THAM KHẢO\n\n• Video: [YouTube link]\n• Bài đọc: [Link tài liệu]\n• Bài tập online: [Link thực hành]",
                    "FixTime"     => "⏱️ KẾ HOẠCH THỜI GIAN\n\n• Khởi động: 5 phút\n• Bài mới: 20 phút\n• Luyện tập: 15 phút\n• Củng cố: 5 phút\nTổng: 45 phút",
                    "AddImage"    => "🖼️ HÌNH MINH HỌA\n\n[Chèn hình ảnh tại đây]\n\nChú thích: [Mô tả hình ảnh]",
                    "AddVideo"    => "📹 VIDEO BÀI GIẢNG\n\nLink: [YouTube / Vimeo]\nThời lượng: [x phút]\nNội dung: [Tóm tắt ngắn về video]",
                    _             => $"📌 {item.Title}\n\n[Nội dung được thêm bởi AI Suggestions]"
                };

                // Insert as a text block
                AddEditableTextBlock(insertedContent);
                LoadAiSuggestions(); // Refresh suggestions after insert
                Log.Information("AI Suggestion applied: {Action}", item.Action);
            }
        }

        private void ViewHistory_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is ClassroomShell shell)
                shell.NavigateTo("F25");
        }

        // ═══════════════════════════════════════════════════════════
        //  RICH TEXT FORMATTER — Tự động format nội dung bài giảng
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Parse template text và render thành rich FlowDocument với
        /// màu sắc, cỡ chữ, bold/italic, nền highlight theo từng loại dòng
        /// </summary>
        private void FormatRichContent(FlowDocument doc, string rawText)
        {
            var lines = rawText.Split('\n');

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue; // Skip blank lines

                var para = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };

                // ═══ HEADING LINES: 📌 📖 ✍️ 📋 — Large, Bold, Colored ═══
                if (trimmed.StartsWith("📌"))
                {
                    // Major heading — Objective block: White on Blue
                    para.Background = new SolidColorBrush(Color.FromRgb(25, 118, 210));   // #1976D2
                    para.Padding = new Thickness(12, 8, 12, 8);
                    para.Margin = new Thickness(0, 4, 0, 6);
                    para.Inlines.Add(new Run(trimmed)
                    {
                        FontSize = 15, FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    });
                }
                else if (trimmed.StartsWith("📖"))
                {
                    // Section heading — Knowledge: Dark teal on light teal
                    para.Background = new SolidColorBrush(Color.FromRgb(224, 242, 241));   // #E0F2F1
                    para.Padding = new Thickness(10, 6, 10, 6);
                    para.Margin = new Thickness(0, 8, 0, 4);
                    para.Inlines.Add(new Run(trimmed)
                    {
                        FontSize = 15, FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0, 77, 64))    // #004D40
                    });
                }
                else if (trimmed.StartsWith("✏️"))
                {
                    // Exercise heading — Orange on light orange
                    para.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));   // #FFF3E0
                    para.Padding = new Thickness(10, 6, 10, 6);
                    para.Margin = new Thickness(0, 8, 0, 4);
                    para.Inlines.Add(new Run(trimmed)
                    {
                        FontSize = 15, FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0))   // #E65100
                    });
                }
                else if (trimmed.StartsWith("📋"))
                {
                    // Teacher notes heading — Purple on light purple
                    para.Background = new SolidColorBrush(Color.FromRgb(243, 229, 245));   // #F3E5F5
                    para.Padding = new Thickness(10, 6, 10, 6);
                    para.Margin = new Thickness(0, 8, 0, 4);
                    para.Inlines.Add(new Run(trimmed)
                    {
                        FontSize = 15, FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(106, 27, 154))  // #6A1B9A
                    });
                }
                // ═══ SUB-HEADINGS: numbered sections like "1.", "2." or "Ví dụ 1:" ═══
                else if ((trimmed.Length > 2 && char.IsDigit(trimmed[0]) && trimmed[1] == '.')
                      || trimmed.StartsWith("Ví dụ")
                      || trimmed.StartsWith("Định nghĩa")
                      || trimmed.StartsWith("Công thức"))
                {
                    para.Margin = new Thickness(4, 6, 0, 2);
                    para.Inlines.Add(new Run(trimmed)
                    {
                        FontSize = 14, FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
                    });
                }
                // ═══ BULLETS: •, 🔹, 🔵, etc — Colored list items ═══
                else if (trimmed.StartsWith("•") || trimmed.StartsWith("🔹") || trimmed.StartsWith("🔵") ||
                         trimmed.StartsWith("🟢") || trimmed.StartsWith("🟡") || trimmed.StartsWith("🔴"))
                {
                    para.Margin = new Thickness(16, 1, 0, 1);
                    para.Inlines.Add(new Run(trimmed)
                    {
                        FontSize = 13.5,
                        Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79))    // #37474F
                    });
                }
                // ═══ CHECKMARK ITEMS: ✅ ❌ — Teacher notes bullets ═══
                else if (trimmed.StartsWith("✅") || trimmed.StartsWith("❌"))
                {
                    para.Margin = new Thickness(12, 1, 0, 1);
                    para.Inlines.Add(new Run(trimmed)
                    {
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50))    // #2E7D32
                    });
                }
                // ═══ EXERCISES: numbered items "1.", "2." under ✍️ ═══
                else if (trimmed.Length > 2 && char.IsDigit(trimmed[0]) && trimmed[1] == '.')
                {
                    para.Margin = new Thickness(12, 2, 0, 2);
                    para.Inlines.Add(new Run(trimmed)
                    {
                        FontSize = 13.5,
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
                    });
                }
                // ═══ FORMULAS & MATH: lines with =, ∫, →, starting with spaces ═══
                else if (trimmed.StartsWith("∫") || trimmed.StartsWith("→") ||
                         (trimmed.Contains("=") && (trimmed.Contains("x") || trimmed.Contains("y") || trimmed.Contains("n"))) ||
                         line.StartsWith("    "))
                {
                    para.Background = new SolidColorBrush(Color.FromRgb(252, 252, 252)); // Very light gray
                    para.Padding = new Thickness(12, 4, 12, 4);
                    para.Margin = new Thickness(20, 2, 20, 2);
                    para.BorderBrush = new SolidColorBrush(Color.FromRgb(189, 189, 189));
                    para.BorderThickness = new Thickness(0, 0, 0, 0);
                    para.Inlines.Add(new Run(trimmed)
                    {
                        FontSize = 14, FontWeight = FontWeights.SemiBold,
                        FontFamily = new FontFamily("Cambria Math, Segoe UI"),
                        Foreground = new SolidColorBrush(Color.FromRgb(183, 28, 28))     // #B71C1C — dark red
                    });
                }
                // ═══ TIME/NOTES: ⏰ 📝 🎯 ═══
                else if (trimmed.StartsWith("⏰") || trimmed.StartsWith("🎯"))
                {
                    para.Background = new SolidColorBrush(Color.FromRgb(255, 249, 196)); // #FFF9C4 — Light yellow
                    para.Padding = new Thickness(10, 4, 10, 4);
                    para.Margin = new Thickness(0, 6, 0, 2);
                    para.Inlines.Add(new Run(trimmed)
                    {
                        FontSize = 13, FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(245, 127, 23))    // #F57F17
                    });
                }
                else if (trimmed.StartsWith("📝"))
                {
                    para.Margin = new Thickness(8, 6, 0, 2);
                    para.Inlines.Add(new Run(trimmed)
                    {
                        FontSize = 13.5, FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192))  // #1565C0
                    });
                }
                // ═══ DEFAULT — Normal text ═══
                else
                {
                    para.Margin = new Thickness(8, 1, 0, 1);
                    para.Inlines.Add(new Run(trimmed)
                    {
                        FontSize = 13.5,
                        Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66))     // #424242
                    });
                }

                doc.Blocks.Add(para);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  SUB-FLOW NAVIGATION
        // ═══════════════════════════════════════════════════════════

        private void GoBackToList_Click(object sender, RoutedEventArgs e)
        {
            // Tự động lưu nếu có LessonId
            if (_currentLessonId > 0)
            {
                Save_Click(sender, e);
            }
            
            // Điều hướng về LessonListPage (F2) thông qua ClassroomShell
            var shell = Window.GetWindow(this) as ClassroomShell;
            if (shell != null)
            {
                Log.Information("LessonEditor sub-flow: Navigating back to LessonList (F2)");
                shell.NavigateTo("F2", addToStack: false); // Không thêm F2 vào stack nếu F2 đã nằm ngay dưới F3
            }
        }

        private void StartRunner_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLessonId <= 0)
            {
                MessageBox.Show("Vui lòng lưu bài giảng (Lưu Nháp) trước khi trình chiếu.", "Trình chiếu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Tự động lưu trước khi trình chiếu
            Save_Click(sender, e);

            // Chuyá»ƒn sang LessonRunnerPage
            var shell = Window.GetWindow(this) as ClassroomShell;
            if (shell != null)
            {
                Log.Information("LessonEditor sub-flow: Starting LessonRunner for Lesson {Id}", _currentLessonId);
                shell.NavigateToRunner(_currentLessonId);
            }
        }
    }

    // ─── AI Suggestion Model ─────────────────────────────────────
    public class AiSuggestionItem
    {
        public string Title  { get; set; } = "";
        public string Detail { get; set; } = "";
        public string Action { get; set; } = "";
        public string Bg     { get; set; } = "#F5F5F5";
        public string Fg     { get; set; } = "#333";
    }
}





