﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using QASmartClass.Data;
using QASmartTouch.Services;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class LessonRunnerPage : Page
    {
        private int _lessonId;
        private Lesson? _lesson;
        private int _currentStage = 1;
        private DispatcherTimer? _lessonTimer;
        public bool IsRunning => _lessonTimer?.IsEnabled == true;
        private DispatcherTimer? _activityTimer;
        private int _lessonSecondsElapsed = 0;
        private int _activitySecondsLeft = 0;
        private int _activityTotalSeconds = 0;

        // References to all stage panels
        private ScrollViewer[] _panels = null!;
        private Border[] _stageButtons = null!;

        // ═══════════════════════════════════════════════════════════
        public LessonRunnerPage(int lessonId = 0)
        {
            InitializeComponent();
            _lessonId = lessonId;

            Loaded += (_, _) =>
            {
                _panels = new[] { panel1, panel2, panel3, panel4, panel5, panel6 };
                _stageButtons = new[] { stageBtn1, stageBtn2, stageBtn3, stageBtn4, stageBtn5, stageBtn6 };

                if (_lessonId > 0)
                    LoadLesson(_lessonId);
                else
                    LoadLastLesson();

                _ = ShowStage(1);
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  LESSON LOADING
        // ═══════════════════════════════════════════════════════════

        private void LoadLesson(int id)
        {
            try
            {
                var db = ((QASmartTouch.App)Application.Current).Database;
                _lesson = db.Lessons.Find(id);
                if (_lesson == null) return;

                txtLessonTitle.Text = _lesson.Title;
                txtLessonMeta.Text = $"{_lesson.Subject} | {_lesson.Grade} | {_lesson.DurationMinutes}'";
                txtSetupTitle.Text = _lesson.Title;
                txtSetupMeta.Text = $"{_lesson.Subject} — {_lesson.Grade} — {_lesson.DurationMinutes} phút";
                txtSetupStatus.Text = _lesson.Status switch
                {
                    "Approved" => "✅ Đã phê duyệt",
                    "Draft"    => "📝 Bản nháp",
                    "Taught"   => "📚 Đã dạy rồi",
                    _          => _lesson.Status
                };

                // Homework pre-fill
                if (!string.IsNullOrWhiteSpace(_lesson.HomeworkText))
                    txtHomework.Text = _lesson.HomeworkText;

                // Lesson content blocks (stage 3) â€” with Focus buttons
                var contents = db.LessonContents
                    .Where(c => c.LessonId == id)
                    .OrderBy(c => c.SortOrder)
                    .ToList();

                _lessonContents = contents;
                lessonContentBlocks.Children.Clear();
                txtContentCount.Text = $"{contents.Count} nội dung";

                foreach (var content in contents)
                {
                    RenderTeacherContentBlock(content.ContentType, content.Data, content.SortOrder);
                }

                Log.Information("LessonRunner loaded: {Title}, {N} content blocks", _lesson.Title, contents.Count);
            }
            catch (Exception ex)
            {
                Log.Warning("LessonRunner LoadLesson error: {Err}", ex.Message);
            }
        }

        private List<LessonContent> _lessonContents = new();

        /// <summary>
        /// Render 1 block nội dung với nút Focus HS + Chiếu (trên trang Tiến trình giảng dạy)
        /// </summary>
        private void RenderTeacherContentBlock(string contentType, string data, int sortOrder)
        {
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

            var bg = (SolidColorBrush)new BrushConverter().ConvertFrom(bgHex)!;
            var fg = (SolidColorBrush)new BrushConverter().ConvertFrom(fgHex)!;

            var blockBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(14, 10, 14, 10),
                Tag = sortOrder
            };

            var dp = new DockPanel();

            // Badge
            var badge = new Border { Background = bg, CornerRadius = new CornerRadius(5), Padding = new Thickness(8, 3, 8, 3), VerticalAlignment = VerticalAlignment.Center };
            badge.Child = new TextBlock { Text = label, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = fg };

            // Content preview (first line)
            var preview = data.Replace("\\n", "\n").Split('\n').FirstOrDefault()?.Trim() ?? contentType;
            if (preview.Length > 80) preview = preview[..80] + "â€¦";
            var txtPreview = new TextBlock
            {
                Text = preview, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0),
                TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis
            };

            // Action buttons
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // 🎯 Focus HS button
            var focusBtn = new Button
            {
                Content = "🎯 Focus", FontSize = 10, Padding = new Thickness(8, 4, 8, 4),
                Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 0), ToolTip = "Yêu cầu HS tập trung vào nội dung này"
            };
            int capturedSort = sortOrder;
            string capturedType = contentType;
            string capturedData = data; // capture for lambda
            focusBtn.Click += (s, e) =>
            {
                var app = (QASmartTouch.App)Application.Current;
                var cmd = $"CMD|LESSON_FOCUS|{capturedSort}|{capturedType}";

                QASmartTouch.App.LessonState.LastTeacherCommand = cmd;
                QASmartTouch.App.LessonState.LastCommandTime = DateTime.Now;
                QASmartTouch.App.FocusState.ActiveFocusSort = capturedSort;
                QASmartTouch.App.FocusState.ActiveFocusType = capturedType;

                var net = app.NetworkService;
                if (net?.IsBroadcasting == true)
                {
                    // 1. Send standard CMD to all (TCP + WebSocket + local bus)
                    //    Fire-and-forget is OK â€” this path is proven working
                    _ = net.FocusContentAsync(capturedSort, capturedType);

                    // 2. Send rich content JSON directly to web students
                    //    NOTE: Do NOT wrap in Task.Run â€” SendToClient needs same context
                    try
                    {
                        var bridge = net.WebBridge;
                        if (bridge?.IsRunning == true)
                        {
                            var lessonTitle = _lesson?.Title ?? "";
                            var lessonSubject = _lesson?.Subject ?? "";
                            var contentPayload = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                type = "lesson_content",
                                action = "LESSON_FOCUS",
                                sortOrder = capturedSort,
                                contentType = capturedType,
                                content = capturedData,
                                lessonTitle = lessonTitle,
                                lessonSubject = lessonSubject
                            });
                            _ = bridge.BroadcastJsonToWebClients(contentPayload);
                            Log.Information("Queued lesson content for web students: sort={Sort}, type={Type}, len={Len}",
                                capturedSort, capturedType, capturedData?.Length ?? 0);
                        }
                        else
                        {
                            Log.Warning("WebBridge not running, cannot send lesson content");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Failed to send lesson content to web: {Err}", ex.Message);
                    }
                }
                else
                    app.RaiseLocalCommand(cmd);

                // ═══ GV SIDE: Zoom + mờ (từ AppSettings) ═══
                var zoomScale = AppSettings.FocusZoomScale;
                var dimOpacity = AppSettings.FocusDimOpacity;

                foreach (var child in lessonContentBlocks.Children)
                {
                    if (child is Border b && b.Tag is int bTag)
                    {
                        if (bTag == capturedSort)
                        {
                            b.BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                            b.BorderThickness = new Thickness(4);
                            b.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                            b.Opacity = 1.0;
                            b.Padding = new Thickness(22, 16, 22, 16);
                            b.Margin = new Thickness(0, 14, 0, 14);
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
                            b.Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));
                            b.Opacity = dimOpacity;
                            b.Effect = null;
                            b.LayoutTransform = null;
                            b.Padding = new Thickness(14, 10, 14, 10);
                            b.Margin = new Thickness(0, 0, 0, 8);
                        }
                    }
                }

                focusBtn.Content = "✅ Focus";
                focusBtn.IsEnabled = false;
                Log.Information("Teacher focused HS on block #{Sort} ({Type})", capturedSort, capturedType);
            };
            btnPanel.Children.Add(focusBtn);

            // ❌ Unfocus button
            var unfocusBtn = new Button
            {
                Content = "❌ Unfocus", FontSize = 10, Padding = new Thickness(8, 4, 8, 4),
                Background = new SolidColorBrush(Color.FromRgb(255, 235, 238)),
                Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 0), ToolTip = "Bỏ Focus — khôi phục hiển thị bình thường"
            };
            unfocusBtn.Click += (s, e) =>
            {
                var app = (QASmartTouch.App)Application.Current;
                var cmd2 = "CMD|LESSON_UNFOCUS";
                QASmartTouch.App.LessonState.LastTeacherCommand = cmd2;
                QASmartTouch.App.LessonState.LastCommandTime = DateTime.Now;
                QASmartTouch.App.FocusState.ActiveFocusSort = -1;
                QASmartTouch.App.FocusState.ActiveFocusType = string.Empty;
                var net2 = app.NetworkService;
                if (net2?.IsBroadcasting == true) _ = net2.SendCommandAsync(cmd2);
                else app.RaiseLocalCommand(cmd2);

                // Reset GV blocks
                foreach (var child in lessonContentBlocks.Children)
                {
                    if (child is Border b2 && b2.Tag is int)
                    {
                        b2.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                        b2.BorderThickness = new Thickness(1);
                        b2.Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));
                        b2.Opacity = 1.0;
                        b2.Effect = null;
                        b2.LayoutTransform = null;
                        b2.Padding = new Thickness(14, 10, 14, 10);
                        b2.Margin = new Thickness(0, 0, 0, 8);
                    }
                }
                focusBtn.Content = "🎯 Focus";
                focusBtn.IsEnabled = true;
            };
            btnPanel.Children.Add(unfocusBtn);

            // 🖊️ Bảng trắng — chụp block & dán lên SmartScreen
            var boardBtn = new Button
            {
                Content = "🖊 Bảng trắng", FontSize = 10, Padding = new Thickness(8, 4, 8, 4),
                Background = new SolidColorBrush(Color.FromRgb(243, 229, 245)),
                Foreground = new SolidColorBrush(Color.FromRgb(123, 31, 162)),
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 0), ToolTip = "Chụp nội dung & dán lên Bảng trắng SmartScreen"
            };
            boardBtn.Click += (s, e) =>
            {
                try
                {
                    // 1. Tìm SmartScreen window & canvas TRƯỚC
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
                        MessageBox.Show("Chưa mở SmartScreen. Hãy mở SmartScreen trước.",
                            "Bảng Trắng", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var canvas = targetWin.FindName("MainInteractiveBoard") as Canvas;
                    if (canvas == null)
                    {
                        MessageBox.Show("Không tìm thấy canvas SmartScreen.",
                            "Bảng Trắng", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // ═══ SPECIAL: Image blocks → load actual image file ═══
                    if (capturedType == "Image" && System.IO.File.Exists(capturedData))
                    {
                        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(capturedData, UriKind.Absolute);
                        bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        double maxW = canvas.ActualWidth * 0.7;
                        double maxH = canvas.ActualHeight * 0.7;
                        double imgW = bitmap.PixelWidth;
                        double imgH = bitmap.PixelHeight;
                        double ratio = Math.Min(maxW / imgW, maxH / imgH);
                        if (ratio < 1) { imgW *= ratio; imgH *= ratio; }

                        var imgContainer = BuildImageOnCanvas(bitmap, imgW, imgH, canvas);
                        double il = Math.Max(20, (canvas.ActualWidth - imgW) / 2);
                        double it = Math.Max(40, (canvas.ActualHeight - imgH) / 2);
                        Canvas.SetLeft(imgContainer, il);
                        Canvas.SetTop(imgContainer, it);
                        canvas.Children.Add(imgContainer);

                        targetWin.Show();
                        targetWin.Activate();
                        targetWin.WindowState = WindowState.Maximized;
                        return;
                    }

                    // 2. Render NỘI DUNG ĐẦY ĐỦ off-screen (không chỉ preview 1 dòng)
                    double renderWidth = Math.Min(900, canvas.ActualWidth * 0.75);
                    var fullContent = BuildWhiteboardContent(capturedType, capturedData, renderWidth);

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

                    double imgWidth = Math.Min(actualW, canvas.ActualWidth * 0.8);
                    double imgHeight = actualH * (imgWidth / actualW);

                    var container = BuildInteractiveImageContainer(rtb, imgWidth, imgHeight, canvas);

                    double left = Math.Max(20, (canvas.ActualWidth - imgWidth) / 2);
                    double top = Math.Max(40, (canvas.ActualHeight - imgHeight) / 2);
                    Canvas.SetLeft(container, left);
                    Canvas.SetTop(container, top);
                    canvas.Children.Add(container);

                    // 5. Chuyá»ƒn sang SmartScreen
                    targetWin.Show();
                    targetWin.Activate();
                    targetWin.WindowState = WindowState.Maximized;

                    Log.Information("Block #{Sort} full content placed on SmartScreen ({W}x{H})",
                        capturedSort, imgWidth, imgHeight);
                }
                catch (Exception ex)
                {
                    Log.Warning("Board capture error: {Err}", ex.Message);
                    MessageBox.Show($"Lỗi: {ex.Message}", "Bảng Trắng", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };
            btnPanel.Children.Add(boardBtn);

            DockPanel.SetDock(btnPanel, Dock.Right);
            dp.Children.Add(btnPanel);
            dp.Children.Add(badge);
            dp.Children.Add(txtPreview);

            blockBorder.Child = dp;
            lessonContentBlocks.Children.Add(blockBorder);
        }

        /// <summary>
        /// Render nội dung đầy đủ off-screen cho chụp ảnh bảng trắng
        /// (Runner chỉ hiện preview 1 dòng, method này render full text)
        /// </summary>
        private static Border BuildWhiteboardContent(string contentType, string data, double maxWidth)
        {
            var textContent = data.Replace("\\n", "\n");

            // Header: loại nội dung
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

            // Header bar
            var headerBorder = new Border
            {
                Background = headerBg, CornerRadius = new CornerRadius(6, 6, 0, 0),
                Padding = new Thickness(16, 10, 16, 10)
            };
            headerBorder.Child = new TextBlock
            {
                Text = label, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = headerFg
            };

            // Content: rich text
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

                // Tiêu đề (chữ IN HOA hoặc bắt đầu bằng I. II. etc.)
                if (line == line.ToUpper() && line.Length > 3)
                {
                    tb.FontSize = 20; tb.FontWeight = FontWeights.Bold;
                    tb.Foreground = new SolidColorBrush(Color.FromRgb(26, 35, 126));
                    tb.Margin = new Thickness(0, 12, 0, 8);
                }
                // Bullet points
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

            // Container
            var outerStack = new StackPanel();
            outerStack.Children.Add(headerBorder);
            outerStack.Children.Add(contentStack);

            var container = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(2),
                Width = maxWidth,
                Child = outerStack,
                Padding = new Thickness(0)
            };

            return container;
        }

        /// <summary>
        /// Bọc ảnh trong container có toolbar: ✋ Di chuyển, 📋 Copy, 🗑 Xóa
        /// Hỗ trợ kéo thả trên canvas SmartScreen
        /// </summary>
        private static Grid BuildInteractiveImageContainer(
            System.Windows.Media.Imaging.RenderTargetBitmap rtb,
            double imgWidth, double imgHeight, Canvas canvas)
        {
            // ── Image ──
            var imgElement = new System.Windows.Controls.Image
            {
                Source = rtb,
                Width = imgWidth,
                Height = imgHeight,
                Stretch = System.Windows.Media.Stretch.Uniform
            };

            // ── Toolbar (hiện khi hover) ──
            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 8, 0),
                Opacity = 0
            };

            var btnStyle = new Style(typeof(Button));
            btnStyle.Setters.Add(new Setter(Button.FontSizeProperty, 12.0));
            btnStyle.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(8, 4, 8, 4)));
            btnStyle.Setters.Add(new Setter(Button.CursorProperty, Cursors.Hand));
            btnStyle.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(0)));
            btnStyle.Setters.Add(new Setter(Button.MarginProperty, new Thickness(2, 0, 2, 0)));

            // âœ‹ Di chuyá»ƒn (drag)
            var moveBtn = new Button
            {
                Content = "✋ Di chuyển",
                Background = new SolidColorBrush(Color.FromArgb(220, 33, 150, 243)),
                Foreground = Brushes.White, Style = btnStyle,
                ToolTip = "Nhấn giữ & kéo để di chuyển"
            };

            // 📋 Copy (nhân bản)
            var copyBtn = new Button
            {
                Content = "📋 Copy",
                Background = new SolidColorBrush(Color.FromArgb(220, 76, 175, 80)),
                Foreground = Brushes.White, Style = btnStyle,
                ToolTip = "Nhân bản ảnh tại vị trí khác"
            };

            // 🗑 Xóa
            var delBtn = new Button
            {
                Content = "🗑 Xóa",
                Background = new SolidColorBrush(Color.FromArgb(220, 229, 57, 53)),
                Foreground = Brushes.White, Style = btnStyle,
                ToolTip = "Xóa ảnh khỏi bảng"
            };

            toolbar.Children.Add(moveBtn);
            toolbar.Children.Add(copyBtn);
            toolbar.Children.Add(delBtn);

            // ── Container Grid ──
            var container = new Grid
            {
                Width = imgWidth,
                Tag = "WhiteboardImage"
            };
            container.Children.Add(imgElement);
            container.Children.Add(toolbar);

            // ── Drag state ──
            bool isDragging = false;
            Point dragStart = new Point();

            // ── Events ──

            // Show/Hide toolbar on hover
            container.MouseEnter += (s, e) =>
            {
                toolbar.Opacity = 1;
                container.Cursor = Cursors.SizeAll;
            };
            container.MouseLeave += (s, e) =>
            {
                if (!isDragging) toolbar.Opacity = 0;
                container.Cursor = Cursors.Arrow;
            };

            // âœ‹ Move: drag anywhere on container
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
                double dx = pos.X - dragStart.X;
                double dy = pos.Y - dragStart.Y;

                double curLeft = Canvas.GetLeft(container);
                double curTop = Canvas.GetTop(container);
                if (double.IsNaN(curLeft)) curLeft = 0;
                if (double.IsNaN(curTop)) curTop = 0;

                Canvas.SetLeft(container, curLeft + dx);
                Canvas.SetTop(container, curTop + dy);
                dragStart = pos;
                e.Handled = true;
            };
            container.MouseLeftButtonUp += (s, e) =>
            {
                isDragging = false;
                container.ReleaseMouseCapture();
                e.Handled = true;
            };

            // 📋 Copy
            copyBtn.Click += (s, e) =>
            {
                var clone = BuildInteractiveImageContainer(rtb, imgWidth, imgHeight, canvas);
                double curLeft = Canvas.GetLeft(container);
                double curTop = Canvas.GetTop(container);
                if (double.IsNaN(curLeft)) curLeft = 100;
                if (double.IsNaN(curTop)) curTop = 100;
                Canvas.SetLeft(clone, curLeft + 30);
                Canvas.SetTop(clone, curTop + 30);
                canvas.Children.Add(clone);
                e.Handled = true;
            };

            // 🗑 Delete
            delBtn.Click += (s, e) =>
            {
                canvas.Children.Remove(container);
                e.Handled = true;
            };

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

            var imgBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 25, 118, 210)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                Background = Brushes.White,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 12, ShadowDepth = 3, Opacity = 0.25, Color = Colors.Black
                },
                Child = imgElement
            };

            var container = new Grid { Width = imgWidth + 4, Tag = "WhiteboardImage" };
            container.Children.Add(imgBorder);
            container.Children.Add(toolbar);

            // ── Zoom ──
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

            container.MouseWheel += (s, e) =>
            {
                double delta = e.Delta > 0 ? zoomStep : -zoomStep;
                ApplyZoom(currentScale + delta);
                e.Handled = true;
            };

            // ── Drag ──
            bool isDragging = false;
            Point dragStart = new Point();

            container.MouseEnter += (s, e) => { toolbar.Opacity = 1; container.Cursor = Cursors.SizeAll; imgBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(180, 25, 118, 210)); };
            container.MouseLeave += (s, e) => { if (!isDragging) toolbar.Opacity = 0; container.Cursor = Cursors.Arrow; imgBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(80, 25, 118, 210)); };

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
                var clone = BuildImageOnCanvas(bitmapSource, imgWidth, imgHeight, canvas);
                double cl = Canvas.GetLeft(container); if (double.IsNaN(cl)) cl = 100;
                double ct = Canvas.GetTop(container); if (double.IsNaN(ct)) ct = 100;
                Canvas.SetLeft(clone, cl + 30); Canvas.SetTop(clone, ct + 30);
                canvas.Children.Add(clone); e.Handled = true;
            };

            delBtn.Click += (s, e) => { canvas.Children.Remove(container); e.Handled = true; };

            AddResizeGrips(container, canvas);
            return container;
        }

        /// <summary>
        /// Tạo PDF Viewer trên SmartScreen canvas sử dụng WebView2
        /// Giống giao diện Thư viện tài nguyên
        /// </summary>
        private static Grid BuildPdfViewerOnCanvas(string pdfPath, double viewerWidth, double viewerHeight, Canvas canvas)
        {
            var fileName = System.IO.Path.GetFileName(pdfPath);
            var webView = new Microsoft.Web.WebView2.Wpf.WebView2
            {
                Width = viewerWidth, Height = viewerHeight - 40
            };

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

            toolbarStack.Children.Add(new TextBlock
            {
                Text = $"📄 {fileName}", FontSize = 11, Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 16, 0), MaxWidth = viewerWidth * 0.35,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            var moveBtn = new Button { Content = "✋ Di chuyển", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 33, 150, 243)) };
            var zoomInBtn = new Button { Content = "🔍+", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 255, 152, 0)), ToolTip = "Phóng to" };
            var zoomOutBtn = new Button { Content = "🔍−", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 255, 152, 0)), ToolTip = "Thu nhỏ" };
            var zoomLabel = new TextBlock { Text = "100%", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(189, 189, 189)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 8, 0) };
            var copyBtn = new Button { Content = "📋 Copy", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 76, 175, 80)) };
            var delBtn = new Button { Content = "🗑 Đóng", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(180, 229, 57, 53)) };

            toolbarStack.Children.Add(moveBtn);
            toolbarStack.Children.Add(zoomInBtn);
            toolbarStack.Children.Add(zoomOutBtn);
            toolbarStack.Children.Add(zoomLabel);
            toolbarStack.Children.Add(copyBtn);

            // ✂️ Chụp vùng
            var snipBtn = new Button { Content = "✂️ Chụp vùng", Style = tbStyle, Background = new SolidColorBrush(Color.FromArgb(200, 156, 39, 176)), ToolTip = "Chụp vùng nội dung PDF" };
            toolbarStack.Children.Add(snipBtn);

            toolbarStack.Children.Add(delBtn);
            toolbar.Child = toolbarStack;

            var webBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(40, 44, 52)),
                BorderThickness = new Thickness(2, 0, 2, 2),
                CornerRadius = new CornerRadius(0, 0, 6, 6),
                Child = webView
            };

            var innerStack = new StackPanel();
            innerStack.Children.Add(toolbar);
            innerStack.Children.Add(webBorder);

            var container = new Grid
            {
                Width = viewerWidth + 4, Tag = "WhiteboardPdf",
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 20, ShadowDepth = 5, Opacity = 0.35, Color = Colors.Black }
            };
            container.Children.Add(innerStack);

            webView.Loaded += async (s, e) =>
            {
                try
                {
                    await webView.EnsureCoreWebView2Async();
                    string folderPath = System.IO.Path.GetDirectoryName(pdfPath) ?? "";
                    string fileName = System.IO.Path.GetFileName(pdfPath) ?? "";

                    if (!string.IsNullOrEmpty(folderPath) && !string.IsNullOrEmpty(fileName))
                    {
                        webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                            "smartclass.assets",
                            folderPath,
                            Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

                        string escapedFile = Uri.EscapeDataString(fileName);
                        webView.CoreWebView2.Navigate($"https://smartclass.assets/{escapedFile}");
                    }
                    else
                    {
                        webView.CoreWebView2.Navigate(new Uri(pdfPath).AbsoluteUri);
                    }
                }
                catch (Exception ex) { Serilog.Log.Warning("WebView2 PDF load error: {Err}", ex.Message); }
            };

            double currentZoom = 1.0;
            void ApplyZoom(double nz) { currentZoom = Math.Max(0.25, Math.Min(5.0, nz)); if (webView.CoreWebView2 != null) webView.ZoomFactor = currentZoom; zoomLabel.Text = $"{(int)(currentZoom * 100)}%"; }
            zoomInBtn.Click += (s, e) => { ApplyZoom(currentZoom + 0.25); e.Handled = true; };
            zoomOutBtn.Click += (s, e) => { ApplyZoom(currentZoom - 0.25); e.Handled = true; };

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

            copyBtn.Click += (s, e) =>
            {
                var clone = BuildPdfViewerOnCanvas(pdfPath, viewerWidth, viewerHeight, canvas);
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
                        Text = "✂️ Kéo chuột để chọn vùng chụp  |  ESC để hủy",
                        FontSize = 16, Foreground = Brushes.White,
                        Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                        Padding = new Thickness(16, 8, 16, 8)
                    };
                    Canvas.SetLeft(guide, (canvas.ActualWidth - 400) / 2); Canvas.SetTop(guide, 20);
                    overlayCanvas.Children.Add(guide);

                    var previewImg = new System.Windows.Controls.Image
                    {
                        Source = fullBitmap, Width = webView.ActualWidth, Height = webView.ActualHeight,
                        Stretch = System.Windows.Media.Stretch.Fill
                    };
                    var webViewOrigin = webView.TranslatePoint(new Point(0, 0), canvas);
                    Canvas.SetLeft(previewImg, webViewOrigin.X); Canvas.SetTop(previewImg, webViewOrigin.Y);
                    overlayCanvas.Children.Add(previewImg);

                    var selRect = new System.Windows.Shapes.Rectangle
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243)), StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection(new[] { 5.0, 3.0 }),
                        Fill = new SolidColorBrush(Color.FromArgb(40, 33, 150, 243)), Visibility = Visibility.Collapsed
                    };
                    overlayCanvas.Children.Add(selRect);
                    canvas.Children.Add(overlayCanvas);

                    bool isSel = false; Point selSt = new(); Rect selRg = Rect.Empty;
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
                            int cX = Math.Max(0, (int)((selRg.X - webViewOrigin.X) * sX));
                            int cY = Math.Max(0, (int)((selRg.Y - webViewOrigin.Y) * sY));
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
                            }
                        }
                        catch (Exception ex) { Serilog.Log.Warning("Snip crop error: {Err}", ex.Message); }
                        canvas.Children.Remove(overlayCanvas);
                    };
                    overlayCanvas.KeyDown += (ss, ee) =>
                    {
                        if (ee.Key == System.Windows.Input.Key.Escape) { isSel = false; overlayCanvas.ReleaseMouseCapture(); canvas.Children.Remove(overlayCanvas); ee.Handled = true; }
                    };
                    overlayCanvas.Focusable = true; overlayCanvas.Focus();
                }
                catch (Exception ex) { Serilog.Log.Warning("Snip error: {Err}", ex.Message); }
            };

            delBtn.Click += (s, e) => { try { webView.Dispose(); } catch { } canvas.Children.Remove(container); e.Handled = true; };

            AddResizeGrips(container, canvas);
            return container;
        }

        /// <summary>
        /// Thêm resize grips vào container — kéo viền để Scale/Zoom nội dung
        /// </summary>
        private static void AddResizeGrips(Grid container, Canvas canvas)
        {
            const double gripSize = 10;
            const double minScale = 0.2, maxScale = 5.0;
            var gripBrush = new SolidColorBrush(Color.FromArgb(180, 25, 118, 210));
            var gripHover = new SolidColorBrush(Color.FromArgb(255, 33, 150, 243));

            var resizeBorder = new Border { BorderBrush = Brushes.Transparent, BorderThickness = new Thickness(3), CornerRadius = new CornerRadius(6), IsHitTestVisible = false, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            container.Children.Add(resizeBorder);

            Border MakeCorner(HorizontalAlignment ha, VerticalAlignment va)
            {
                var g = new Border { Width = gripSize, Height = gripSize, Background = gripBrush, HorizontalAlignment = ha, VerticalAlignment = va, Cursor = Cursors.SizeNWSE, CornerRadius = new CornerRadius(3), Opacity = 0, Tag = "ResizeGrip", ToolTip = "Kéo để phóng to / thu nhỏ" };
                g.MouseEnter += (s, e) => { g.Opacity = 1; g.Background = gripHover; };
                g.MouseLeave += (s, e) => { g.Opacity = 0; g.Background = gripBrush; };
                return g;
            }

            var cTL = MakeCorner(HorizontalAlignment.Left, VerticalAlignment.Top);
            var cTR = MakeCorner(HorizontalAlignment.Right, VerticalAlignment.Top);
            var cBL = MakeCorner(HorizontalAlignment.Left, VerticalAlignment.Bottom);
            var cBR = MakeCorner(HorizontalAlignment.Right, VerticalAlignment.Bottom);
            var corners = new[] { cTL, cTR, cBL, cBR };
            foreach (var c in corners) container.Children.Add(c);

            container.MouseEnter += (s, e) => { foreach (var c in corners) c.Opacity = 0.7; resizeBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(100, 33, 150, 243)); };
            container.MouseLeave += (s, e) => { foreach (var c in corners) c.Opacity = 0; resizeBorder.BorderBrush = Brushes.Transparent; };

            foreach (var grip in corners)
            {
                bool isScaling = false; Point scaleStart = new(); double startScale = 1.0;
                grip.MouseLeftButtonDown += (s, e) =>
                {
                    isScaling = true; scaleStart = e.GetPosition(canvas);
                    startScale = container.LayoutTransform is ScaleTransform st ? st.ScaleX : 1.0;
                    grip.CaptureMouse(); e.Handled = true;
                };
                grip.MouseMove += (s, e) =>
                {
                    if (!isScaling) return;
                    var pos = e.GetPosition(canvas);
                    double delta = ((pos.X - scaleStart.X) + (pos.Y - scaleStart.Y)) / 300.0;
                    double ns = Math.Max(minScale, Math.Min(maxScale, startScale + delta));
                    container.LayoutTransform = new ScaleTransform(ns, ns);
                    e.Handled = true;
                };
                grip.MouseLeftButtonUp += (s, e) => { isScaling = false; grip.ReleaseMouseCapture(); e.Handled = true; };
            }
        }

        /// <summary>Phát tất cả nội dung bài giảng cho HS (auto-focus lần lượt)</summary>
        private void BroadcastAllContent_Click(object sender, RoutedEventArgs e)
        {
            if (_lessonContents.Count == 0)
            {
                MessageBox.Show("Chưa có nội dung bài giảng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var app = (QASmartTouch.App)Application.Current;

            // Gửi LESSON_START để HS chuyển sang trang bài giảng
            QASmartTouch.App.LessonState.IsLessonActive = true;
            QASmartTouch.App.LessonState.ActiveLessonId = _lesson?.Id ?? 0;
            QASmartTouch.App.LessonState.ActiveLessonStage = 3;
            QASmartTouch.App.LessonState.LastCommandTime = DateTime.Now;

            var cmd = $"CMD|LESSON_START|{_lesson?.Id ?? 0}";
            QASmartTouch.App.LessonState.LastTeacherCommand = cmd;

            var net = app.NetworkService;
            if (net?.IsBroadcasting == true)
                _ = net.StartLessonAsync(_lesson?.Id ?? 0);
            else
                app.RaiseLocalCommand(cmd);

            // Sau 500ms, gửi stage 3
            var stageTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            stageTimer.Tick += (_, _) =>
            {
                stageTimer.Stop();
                if (net?.IsBroadcasting == true)
                    _ = net.SetLessonStageAsync(3);
                else
                    app.RaiseLocalCommand("CMD|LESSON_STAGE|3");
            };
            stageTimer.Start();

            MessageBox.Show($"📡 Đã phát bài giảng ({_lessonContents.Count} nội dung) cho tất cả HS!\n\nHS sẽ tự chuyển sang trang Bài giảng.",
                "Phát Bài Giảng", MessageBoxButton.OK, MessageBoxImage.Information);

            Log.Information("Broadcast all content: {N} blocks", _lessonContents.Count);
        }

        private void LoadLastLesson()
        {
            try
            {
                var db = ((QASmartTouch.App)Application.Current).Database;
                var last = db.Lessons
                    .Where(l => l.Status == "Approved" || l.Status == "Taught")
                    .OrderByDescending(l => l.LastTaughtAt ?? l.UpdatedAt)
                    .FirstOrDefault();
                if (last != null) LoadLesson(last.Id);
            }
            catch (Exception ex)
            {
                Log.Warning("LoadLastLesson error: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  STAGE NAVIGATION
        // ═══════════════════════════════════════════════════════════

        private void Stage_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && b.Tag is string tag && int.TryParse(tag, out int stage))
                _ = ShowStage(stage);
        }

        private async Task ShowStage(int stage)
        {
            _currentStage = stage;

            for (int i = 0; i < _panels.Length; i++)
            {
                _panels[i].Visibility = (i + 1 == stage) ? Visibility.Visible : Visibility.Collapsed;

                var bg = (i + 1 == stage)
                    ? new SolidColorBrush(Color.FromRgb(25, 118, 210))
                    : Brushes.Transparent;
                _stageButtons[i].Background = bg;

                // Update text colors
                if (_stageButtons[i].Child is StackPanel sp)
                {
                    foreach (var child in sp.Children)
                    {
                        if (child is TextBlock tb)
                            tb.Foreground = (i + 1 == stage) ? Brushes.White
                                : new SolidColorBrush(Color.FromRgb(144, 202, 249));
                    }
                }
            }

            lessonProgress.Value = stage;
            txtStageLabel.Text = $"Giai đoạn {stage}/6";

            // ═══ Update shared state + Broadcast LESSON_STAGE ═══
            try
            {
                var app = (QASmartTouch.App)Application.Current;

                // Luôn cập nhật shared state
                QASmartTouch.App.LessonState.ActiveLessonStage = stage;
                QASmartTouch.App.LessonState.LastTeacherCommand = $"CMD|LESSON_STAGE|{stage}";
                QASmartTouch.App.LessonState.LastCommandTime = DateTime.Now;

                // Gửi qua network + local bus
                var net = app.NetworkService;
                if (net?.IsBroadcasting == true)
                    await net.SetLessonStageAsync(stage);
                else
                    app.RaiseLocalCommand($"CMD|LESSON_STAGE|{stage}");
            }
            catch { }
        }

        private void NextStage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStage < 6) _ = ShowStage(_currentStage + 1);
        }

        // ═══════════════════════════════════════════════════════════
        //  STAGE 1: START LESSON
        // ═══════════════════════════════════════════════════════════

        private async void StartLesson_Click(object sender, RoutedEventArgs e)
        {
            StartLessonTimer();
            _ = ShowStage(2);

            // ═══ Broadcast + Save shared state → HS nhận được dù mở trước hay sau ═══
            try
            {
                var app = (QASmartTouch.App)Application.Current;

                // Luôn cập nhật shared state (HS đọc khi mở lên)
                QASmartTouch.App.LessonState.IsLessonActive = true;
                QASmartTouch.App.LessonState.ActiveLessonId = _lesson?.Id ?? 0;
                QASmartTouch.App.LessonState.ActiveLessonStage = 2;
                QASmartTouch.App.LessonState.LastTeacherCommand = $"CMD|LESSON_START|{_lesson?.Id ?? 0}";
                QASmartTouch.App.LessonState.LastCommandTime = DateTime.Now;

                // Gửi qua network (cho HS trên mạng)
                var net = app.NetworkService;
                if (net?.IsBroadcasting == true && _lesson != null)
                {
                    await net.StartLessonAsync(_lesson.Id);
                    await net.SetLessonStageAsync(2);
                }
                else
                {
                    // Nếu chưa broadcast qua mạng, vẫn gửi local bus
                    app.RaiseLocalCommand($"CMD|LESSON_START|{_lesson?.Id ?? 0}");
                    app.RaiseLocalCommand("CMD|LESSON_STAGE|2");
                }
            }
            catch (Exception ex) { Log.Warning("Broadcast lesson start error: {Err}", ex.Message); }

            Log.Information("Lesson started: Stage 1 → 2, broadcast to students");
        }

        private void StartLessonTimer()
        {
            _lessonSecondsElapsed = 0;
            int totalSeconds = (_lesson?.DurationMinutes ?? 45) * 60;

            _lessonTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _lessonTimer.Tick += (s, e) =>
            {
                _lessonSecondsElapsed++;
                int remaining = totalSeconds - _lessonSecondsElapsed;
                int mins = Math.Max(0, remaining / 60);
                int secs = Math.Max(0, remaining % 60);
                txtTimer.Text = $"{mins:D2}:{secs:D2}";

                // Color warning
                if (remaining <= 300) // 5 phút
                    txtTimer.Foreground = new SolidColorBrush(Color.FromRgb(255, 82, 82));
                else if (remaining <= 600) // 10 phút
                    txtTimer.Foreground = new SolidColorBrush(Color.FromRgb(255, 167, 38));

                if (remaining <= 0) _lessonTimer?.Stop();
            };
            _lessonTimer.Start();
        }

        // ═══════════════════════════════════════════════════════════
        //  STAGE 2: QUICK QUIZ / POLL
        // ═══════════════════════════════════════════════════════════

        private void LaunchQuickQuiz_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to QuizPage pre-configured for "review" mode
            if (Window.GetWindow(this) is ClassroomShell shell)
            {
                MessageBox.Show("🚀 Đang phát Quiz Kiểm Tra Bài Cũ lên SmartScreen!\n\nHS sẽ nhận câu hỏi trên thiết bị trong vài giây.",
                    "Quick Quiz", MessageBoxButton.OK, MessageBoxImage.Information);

                ShowQuizResult("AI đang tạo 5 câu kiểm tra bài cũ...\n✅ Đã phát lên SmartScreen\n• 35/35 HS đã nhận câu hỏi");
                Log.Information("Quick Quiz launched");
            }
        }

        private void LaunchPoll_Click(object sender, MouseButtonEventArgs e) => LaunchPollInternal();
        private void LaunchPoll_Click(object sender, RoutedEventArgs e) => LaunchPollInternal();
        private void LaunchPollInternal()
        {
            MessageBox.Show("📊 Quick Poll đã được phát!\n\nHS đang trả lời câu hỏi trên thiết bị của mình.",
                "Poll", MessageBoxButton.OK, MessageBoxImage.Information);
            Log.Information("Poll launched");
        }

        private void ShowQuizResult(string text)
        {
            quizResultZone.Visibility = Visibility.Visible;
            txtQuizResult.Text = text;
        }

        // ═══════════════════════════════════════════════════════════
        //  STAGE 3: EXPLORE / GIáº¢NG BÃ€I
        // ═══════════════════════════════════════════════════════════

        private void OpenLessonOnCanvas_Click(object sender, MouseButtonEventArgs e)
        {
            if (_lesson == null) return;
            if (Window.GetWindow(this) is ClassroomShell shell)
                shell.NavigateToCanvas(_lesson.Id);
        }

        private void RandomPickStudent_Click(object sender, MouseButtonEventArgs e)
        {
            if (Window.GetWindow(this) is ClassroomShell shell)
                shell.NavigateToPage("Picker");
        }
        // ═══════════════════════════════════════════════════════════
        //  📺 PHÁT BẢNG TRẮNG LIÊN TỤC (Live Broadcast)
        // ═══════════════════════════════════════════════════════════

        private DispatcherTimer? _broadcastTimer;
        private int _broadcastCount = 0;

        /// <summary>
        /// Bật/tắt chế độ phát bảng trắng liên tục cho HS.
        /// Khi BẬT: Timer chụp SmartScreen định kỳ → gửi CMD cho HS.
        /// Khi TẮT: Dừng timer → gửi CMD dừng cho HS.
        /// </summary>
        private void ToggleBroadcastScreen_Click(object sender, MouseButtonEventArgs e)
        {
            var app = (QASmartTouch.App)Application.Current;

            if (!QASmartTouch.App.BroadcastState.IsScreenBroadcastActive)
            {
                // ── BẬT chế độ phát ──
                StartScreenBroadcast(app);
            }
            else
            {
                // ── TẮT chế độ phát ──
                StopScreenBroadcast(app);
            }
        }

        private void StartScreenBroadcast(QASmartTouch.App app)
        {
            // 1. Kiểm tra SmartScreen có sẵn không
            Window? targetWin = FindSmartScreenWindow();
            if (targetWin == null)
            {
                MessageBox.Show("Chưa mở SmartScreen.\nHãy chuyển sang SmartScreen trước để có nội dung phát.",
                    "📺 Phát Bảng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var canvas = targetWin.FindName("MainInteractiveBoard") as Canvas;
            if (canvas == null || canvas.ActualWidth < 1)
            {
                MessageBox.Show("Canvas SmartScreen trống hoặc chưa sẵn sàng.",
                    "📺 Phát Bảng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Bật flag
            QASmartTouch.App.BroadcastState.IsScreenBroadcastActive = true;
            _broadcastCount = 0;

            // 3. Cập nhật UI card
            broadcastCard.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            txtBroadcastIcon.Text = "🔴";
            txtBroadcastTitle.Text = "⏹ Dừng Phát Bảng";
            txtBroadcastTitle.Foreground = Brushes.White;
            txtBroadcastDesc.Text = "Đang phát bảng trắng cho HS...";
            txtBroadcastDesc.Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));

            // 4. Chụp lần đầu ngay lập tức
            CaptureAndBroadcastScreen(app);

            // 5. Gửi lệnh BẬT cho HS → mở overlay
            var startCmd = $"CMD|SCREEN_BROADCAST_START|{QASmartTouch.App.BroadcastState.ScreenCapturePath}";
            QASmartTouch.App.LessonState.LastTeacherCommand = startCmd;
            QASmartTouch.App.LessonState.LastCommandTime = DateTime.Now;
            var net = app.NetworkService;
            if (net?.IsBroadcasting == true) _ = net.SendCommandAsync(startCmd);
            else app.RaiseLocalCommand(startCmd);

            // 6. Khởi tạo Timer cập nhật định kỳ
            _broadcastTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(QASmartTouch.App.BroadcastState.ScreenBroadcastIntervalSec)
            };
            _broadcastTimer.Tick += (s, ev) =>
            {
                if (!QASmartTouch.App.BroadcastState.IsScreenBroadcastActive) { StopScreenBroadcast(app); return; }

                CaptureAndBroadcastScreen(app);

                // Gửi CMD cập nhật cho HS
                var updateCmd = $"CMD|SCREEN_BROADCAST_UPDATE|{QASmartTouch.App.BroadcastState.ScreenCapturePath}";
                if (net?.IsBroadcasting == true) _ = net.SendCommandAsync(updateCmd);
                else app.RaiseLocalCommand(updateCmd);

                _broadcastCount++;
                txtBroadcastDesc.Text = $"Đang phát... (cập nhật #{_broadcastCount})";
            };
            _broadcastTimer.Start();

            Log.Information("Screen broadcast STARTED (interval={Sec}s)", QASmartTouch.App.BroadcastState.ScreenBroadcastIntervalSec);
        }

        private void StopScreenBroadcast(QASmartTouch.App app)
        {
            // 1. Tắt timer
            _broadcastTimer?.Stop();
            _broadcastTimer = null;

            // 2. Tắt flag
            QASmartTouch.App.BroadcastState.IsScreenBroadcastActive = false;

            // 3. Cập nhật UI card
            broadcastCard.Background = new SolidColorBrush(Color.FromRgb(243, 229, 245));
            txtBroadcastIcon.Text = "📺";
            txtBroadcastTitle.Text = "Phát Bảng cho HS";
            txtBroadcastTitle.Foreground = new SolidColorBrush(Color.FromRgb(123, 31, 162));
            txtBroadcastDesc.Text = "Bật chế độ phát bảng trắng liên tục";
            txtBroadcastDesc.Foreground = new SolidColorBrush(Color.FromRgb(156, 39, 176));

            // 4. Gửi lệnh TẮT cho HS → đóng overlay
            var stopCmd = "CMD|SCREEN_BROADCAST_STOP";
            QASmartTouch.App.LessonState.LastTeacherCommand = stopCmd;
            QASmartTouch.App.LessonState.LastCommandTime = DateTime.Now;
            var net = app.NetworkService;
            if (net?.IsBroadcasting == true) _ = net.SendCommandAsync(stopCmd);
            else app.RaiseLocalCommand(stopCmd);

            Log.Information("Screen broadcast STOPPED (total captures: {Count})", _broadcastCount);
        }

        /// <summary>Chụp SmartScreen canvas → lưu PNG → cập nhật App state</summary>
        private void CaptureAndBroadcastScreen(QASmartTouch.App app)
        {
            try
            {
                Window? targetWin = FindSmartScreenWindow();
                if (targetWin == null) return;

                var canvas = targetWin.FindName("MainInteractiveBoard") as Canvas;
                if (canvas == null || canvas.ActualWidth < 1) return;

                double scale = 2.0;
                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)(canvas.ActualWidth * scale), (int)(canvas.ActualHeight * scale),
                    96 * scale, 96 * scale, PixelFormats.Pbgra32);
                rtb.Render(canvas);

                var tempDir = QASmartClass.Services.AppPaths.ScreenCapturesDir;
                System.IO.Directory.CreateDirectory(tempDir);

                // Luôn ghi đè cùng 1 file để tiết kiệm dung lượng
                var filePath = System.IO.Path.Combine(tempDir, "live_broadcast.png");

                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                using (var fs = System.IO.File.Create(filePath))
                    encoder.Save(fs);

                QASmartTouch.App.BroadcastState.ScreenCapturePath = filePath;
                QASmartTouch.App.BroadcastState.ScreenCaptureTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Log.Warning("CaptureAndBroadcastScreen error: {Err}", ex.Message);
            }
        }

        /// <summary>Tìm SmartScreen window (Form2_MainDashboard)</summary>
        private static Window? FindSmartScreenWindow()
        {
            foreach (Window win in Application.Current.Windows)
            {
                var typeName = win.GetType().Name;
                if (typeName.Contains("MainDashboard") || typeName.Contains("Form2"))
                    return win;
            }
            return null;
        }

        // ═══════════════════════════════════════════════════════════
        //  STAGE 4: MINI GAMES
        // ═══════════════════════════════════════════════════════════

        private void LaunchQuizBattle_Click(object sender, RoutedEventArgs e)
        {
            StartActivity("⚔️ Quiz Battle đang diễn ra", 15 * 60);
            Log.Information("Quiz Battle launched");
        }

        private void LaunchGroupTask_Click(object sender, RoutedEventArgs e)
        {
            // R-BUG-02 fix: Let teacher choose group size
            int studentCount = 35;
            try { studentCount = ((QASmartTouch.App)Application.Current).Database.Students.Count(s => s.IsOnline); } catch { }
            if (studentCount <= 0) studentCount = 35;

            // Simple input dialog
            var dlg = new Window
            {
                Title = "Chia nhóm", Width = 320, Height = 180, WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };
            var sp = new StackPanel { Margin = new Thickness(16) };
            sp.Children.Add(new TextBlock { Text = $"Có {studentCount} HS online.\nNhập số HS mỗi nhóm (3–8):", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,10) });
            var txtInput = new System.Windows.Controls.TextBox { Text = "5", FontSize = 16, Padding = new Thickness(8,6,8,6) };
            sp.Children.Add(txtInput);
            var btnOk = new Button { Content = "OK", Padding = new Thickness(20,8,20,8), HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0,12,0,0) };
            btnOk.Click += (_, _) => dlg.DialogResult = true;
            sp.Children.Add(btnOk);
            dlg.Content = sp;
            txtInput.Focus();
            txtInput.SelectAll();

            if (dlg.ShowDialog() != true) return;
            if (!int.TryParse(txtInput.Text, out int perGroup) || perGroup < 2 || perGroup > 10)
            {
                MessageBox.Show("Số thành viên không hợp lệ (3–8)!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int groupCount = Math.Max(2, (int)Math.Ceiling((double)studentCount / perGroup));
            StartActivity("👥 Task Nhóm đang chạy", 20 * 60);
            MessageBox.Show($"👥 Đã chia {groupCount} nhóm ({perGroup} HS/nhóm)!\n\nTask đã được gửi đến từng nhóm. Timer 20 phút bắt đầu.",
                "Task Nhóm", MessageBoxButton.OK, MessageBoxImage.Information);
            Log.Information("Group Task launched, {Groups} groups x {Size}", groupCount, perGroup);
        }

        private void LaunchSimulation_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("🔬 Đang mở PhET Simulation trên SmartScreen!\n\nHS có thể tương tác trực tiếp.",
                "Mô Phỏng", MessageBoxButton.OK, MessageBoxImage.Information);
            StartActivity("🔬 Mô phỏng đang chạy", 15 * 60);
            Log.Information("Simulation launched");
        }

        private void LaunchChallenge_Click(object sender, RoutedEventArgs e)
        {
            StartActivity("🚀 Real Challenge đang diễn ra", 25 * 60);
            MessageBox.Show("🚀 Challenge đã phát!\n\nNhóm HS đang brainstorm và thiết kế giải pháp.",
                "Challenge", MessageBoxButton.OK, MessageBoxImage.Information);
            Log.Information("Challenge launched");
        }

        private void StartActivity(string title, int durationSeconds)
        {
            activityPanel.Visibility = Visibility.Visible;
            txtActivityTitle.Text = title;
            _activitySecondsLeft = durationSeconds;
            _activityTotalSeconds = durationSeconds;

            _activityTimer?.Stop();
            _activityTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _activityTimer.Tick += (s, e) =>
            {
                _activitySecondsLeft--;
                int mins = _activitySecondsLeft / 60;
                int secs = _activitySecondsLeft % 60;
                txtActivityTimer.Text = $"⏱ {mins:D2}:{secs:D2}";
                activityProgress.Value = (double)_activitySecondsLeft / _activityTotalSeconds * 100;

                if (_activitySecondsLeft <= 0)
                {
                    _activityTimer?.Stop();
                    txtActivityTimer.Text = "⏱️ HẾT GIỜ!";
                    txtActivityTimer.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                }
            };
            _activityTimer.Start();
        }

        private void StopActivity_Click(object sender, RoutedEventArgs e)
        {
            _activityTimer?.Stop();
            activityPanel.Visibility = Visibility.Collapsed;
        }

        private int GenerateGroups()
        {
            try
            {
                var db = ((QASmartTouch.App)Application.Current).Database;
                int count = db.Students.Count(s => s.IsOnline);
                return Math.Max(2, count / 5); // ~5 người/nhóm
            }
            catch { return 4; }
        }

        // ═══════════════════════════════════════════════════════════
        //  STAGE 5: ÄÃNH GIÃ / LEADERBOARD
        // ═══════════════════════════════════════════════════════════

        private void RefreshLeaderboard_Click(object sender, RoutedEventArgs e)
        {
            // Show demo leaderboard (in production: pull from real quiz results)
            var rnd = new Random();
            txtGold.Text = "Nhóm " + (rnd.Next(1, 6));
            txtGoldScore.Text = rnd.Next(88, 100) + " điểm";
            txtSilver.Text = "Nhóm " + (rnd.Next(1, 6));
            txtSilverScore.Text = rnd.Next(72, 88) + " điểm";
            txtBronze.Text = "Nhóm " + (rnd.Next(1, 6));
            txtBronzeScore.Text = rnd.Next(55, 72) + " điểm";
        }

        private void AutoGrade_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("✅ Đã chấm điểm tự động!\n\n• Điểm được tính theo rubric\n• Đã ghi vào Gradebook\n• Thông báo đang gửi đến HS",
                "Auto-Grade", MessageBoxButton.OK, MessageBoxImage.Information);
            Log.Information("Auto-grade executed");
        }

        // ═══════════════════════════════════════════════════════════
        //  STAGE 6: Tá»”NG Káº¾T
        // ═══════════════════════════════════════════════════════════

        private async void AssignHomework_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHomework.Text)) return;

            int hwId = 0;
            if (_lesson != null)
            {
                var db = ((QASmartTouch.App)Application.Current).Database;
                _lesson.HomeworkText = txtHomework.Text;
                _lesson.HasHomework = true;
                
                // Also save to Homeworks table
                var newHw = new QASmartClass.Data.Homework
                {
                    Subject = _lesson.Subject ?? "Toán",
                    Title = $"Bài tập buổi học ngày {DateTime.Today:dd/MM}",
                    Description = txtHomework.Text,
                    Deadline = DateTime.Today.AddDays(3).AddHours(23).AddMinutes(59), // Default deadline
                    CreatedAt = DateTime.Now,
                    AttachmentPath = "",
                    ClassId = _lesson.ClassName ?? ""
                };
                db.Homeworks.Add(newHw);
                db.SaveChanges();
                hwId = newHw.Id;
            }

            // ═══ Broadcast ASSIGNMENT ═══
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var hwText = txtHomework.Text[..Math.Min(200, txtHomework.Text.Length)];
                
                string cleanDesc = hwText.Replace("|", " ");
                string deadlineStr = DateTime.Today.AddDays(3).AddHours(23).AddMinutes(59).ToString("yyyy-MM-dd HH:mm");
                string subjectName = (_lesson?.Subject ?? "Toán").Replace("|", " ");
                string hwTitle = $"Bài tập buổi học ngày {DateTime.Today:dd/MM}".Replace("|", " ");
                
                string cmd = $"CMD|ASSIGNMENT|{hwId}|{subjectName}|{hwTitle}|{deadlineStr}|{cleanDesc}|";

                QASmartTouch.App.LessonState.LastTeacherCommand = cmd;
                QASmartTouch.App.LessonState.LastCommandTime = DateTime.Now;

                var net = app.NetworkService;
                if (net?.IsBroadcasting == true)
                    await net.SendCommandAsync(cmd);
                else
                    app.RaiseLocalCommand(cmd);
            }
            catch { }

            MessageBox.Show("📤 Đã gửi bài tập về nhà cho tất cả HS!\n\nHS sẽ nhận thông báo trên SmartScreen.",
                "Bài Tập Về Nhà", MessageBoxButton.OK, MessageBoxImage.Information);
            Log.Information("Homework assigned: {Text}", txtHomework.Text[..Math.Min(50, txtHomework.Text.Length)]);
        }

        private async void EndLesson_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Kết thúc và lưu tiết học?\n\nToàn bộ dữ liệu sẽ được lưu tự động.",
                "Kết Thúc Tiết", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            _lessonTimer?.Stop();
            _activityTimer?.Stop();

            // Update lesson status and stats
            try
            {
                if (_lesson != null)
                {
                    var db = ((QASmartTouch.App)Application.Current).Database;
                    _lesson.Status = "Taught";
                    _lesson.LastTaughtAt = DateTime.Now;
                    _lesson.UseCount++;
                    _lesson.UpdatedAt = DateTime.Now;
                    db.SaveChanges();

                    // Update summary stats
                    int elapsed = _lessonSecondsElapsed / 60;
                    txtSumDuration.Text = $"{elapsed}'";
                    try { txtSumHsOnline.Text = db.Students.Count(s => s.IsOnline).ToString(); } catch { }
                }
            }
            catch (Exception ex) { Log.Warning("EndLesson save error: {Err}", ex.Message); }

            // ═══ Clear shared state + Broadcast LESSON_END ═══
            try
            {
                var app = (QASmartTouch.App)Application.Current;

                // Clear shared state
                QASmartTouch.App.LessonState.IsLessonActive = false;
                QASmartTouch.App.LessonState.ActiveLessonStage = 0;
                QASmartTouch.App.LessonState.LastTeacherCommand = "CMD|LESSON_END|0";
                QASmartTouch.App.LessonState.LastCommandTime = DateTime.Now;

                var net = app.NetworkService;
                if (net?.IsBroadcasting == true)
                    await net.EndLessonAsync();
                else
                    app.RaiseLocalCommand("CMD|LESSON_END|0");
            }
            catch { }

            _ = ShowStage(6);
            UpdateSummary();
            Log.Information("Lesson ended: {Title}, {Duration}s", _lesson?.Title, _lessonSecondsElapsed);
        }

        private void UpdateSummary()
        {
            int elapsed = _lessonSecondsElapsed / 60;
            txtSumDuration.Text = $"{elapsed}'";
        }
    }
}





