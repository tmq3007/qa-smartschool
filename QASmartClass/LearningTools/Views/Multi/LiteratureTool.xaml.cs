using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Multi
{
    public partial class LiteratureTool : BaseToolControl
    {
        private class MindmapNode
        {
            public string Id { get; set; }
            public Border Visual { get; set; }
            public Point Position { get; set; }
            public string NodeType { get; set; } // Root, Event
        }

        private class MindmapLink
        {
            public string FromId { get; set; }
            public string ToId { get; set; }
            public System.Windows.Shapes.Path Visual { get; set; }
        }

        private List<MindmapNode> _nodes = new List<MindmapNode>();
        private List<MindmapLink> _links = new List<MindmapLink>();

        private MindmapNode _draggedNode = null;
        private Point _dragStartPoint;
        private Point _nodeStartPos;

        private MindmapNode _linkSourceNode = null;
        private bool _isDrawingLink = false;

        private DispatcherTimer _tutorialTimer;
        private System.Windows.Shapes.Path PreviewLink;
        private readonly ScaleTransform _canvasScaleTransform = new ScaleTransform(1.0, 1.0);
        private TextBox _focusedTextBox = null;

        public LiteratureTool()
        {
            InitializeComponent();

            // Khởi tạo PreviewLink động để tránh lỗi Logical Parent của WPF
            PreviewLink = new System.Windows.Shapes.Path
            {
                Stroke = DS.Brush((Color)ColorConverter.ConvertFromString("#BA68C8")),
                StrokeThickness = 3,
                StrokeDashArray = new DoubleCollection(new double[] { 4, 4 }),
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
            Canvas.SetZIndex(PreviewLink, 1);
            MindmapCanvas.Children.Add(PreviewLink);
            this.RegisterName("PreviewLink", PreviewLink);

            Loaded += LiteratureTool_Loaded;
            Unloaded += LiteratureTool_Unloaded;
            MindmapCanvas.LayoutTransform = _canvasScaleTransform;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
                ApplyZoom(zoomFactor, e.GetPosition(literatureScrollViewer), e.GetPosition(MindmapCanvas));
                e.Handled = true; // Prevent vertical scrolling while zooming
            }
        }

        private void ApplyZoom(double zoomFactor, Point? viewPosition = null, Point? canvasPosition = null)
        {
            double newScaleX = _canvasScaleTransform.ScaleX * zoomFactor;

            // Limit zoom scale between 0.5 and 2.0 (50% to 200%)
            if (newScaleX < 0.5) newScaleX = 0.5;
            if (newScaleX > 2.0) newScaleX = 2.0;

            if (newScaleX == _canvasScaleTransform.ScaleX) return;

            // Save old coordinates if we want to zoom towards a target point
            Point viewPoint = viewPosition ?? new Point(literatureScrollViewer.ViewportWidth / 2, literatureScrollViewer.ViewportHeight / 2);
            Point canvasPoint = canvasPosition ?? MindmapCanvas.TransformToVisual(literatureScrollViewer).Inverse.Transform(viewPoint);

            _canvasScaleTransform.ScaleX = newScaleX;
            _canvasScaleTransform.ScaleY = newScaleX;

            // Update UI layout immediately so scrollviewer knows new canvas bounds
            literatureScrollViewer.UpdateLayout();

            // Adjust scroll offsets
            double newHoriz = canvasPoint.X * newScaleX - viewPoint.X;
            double newVert = canvasPoint.Y * newScaleX - viewPoint.Y;

            literatureScrollViewer.ScrollToHorizontalOffset(newHoriz);
            literatureScrollViewer.ScrollToVerticalOffset(newVert);

            // Update zoom percentage text
            if (txtZoomPercent != null)
            {
                txtZoomPercent.Text = $"{(int)System.Math.Round(newScaleX * 100)}%";
            }
        }

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ApplyZoom(1.1);
        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ApplyZoom(0.9);
        }

        private void BtnZoomReset_Click(object sender, RoutedEventArgs e)
        {
            double currentScale = _canvasScaleTransform.ScaleX;
            if (currentScale != 1.0)
            {
                ApplyZoom(1.0 / currentScale);
            }
        }

        private void LiteratureTool_Loaded(object sender, RoutedEventArgs e)
        {
            _tutorialTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
            _tutorialTimer.Tick += TutorialTimer_Tick;
            _tutorialTimer.Start();

            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.PreviewKeyDown -= ParentWindow_PreviewKeyDown;
                parentWindow.PreviewKeyDown += ParentWindow_PreviewKeyDown;
            }
            InitializeAutoSave();
            CheckAndRestoreDraft();
            LoadPracticalApps();

            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
            if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Sơ đồ tác phẩm" : "Mindmap Canvas";
            if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

            if (sideMenu != null)
            {
                sideMenu.SelectedIndex = 0;
            }
        }

        private void LiteratureTool_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_tutorialTimer != null)
            {
                _tutorialTimer.Stop();
                _tutorialTimer.Tick -= TutorialTimer_Tick;
            }

            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.PreviewKeyDown -= ParentWindow_PreviewKeyDown;
            }
            if (_autoSaveTimer != null)
            {
                _autoSaveTimer.Stop();
            }
        }

        private void ParentWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!this.IsVisible) return;

            // Do not capture keyboard shortcuts if the user is typing in a TextBox
            if (Keyboard.FocusedElement is TextBox)
                return;

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.OemPlus || e.Key == Key.Add)
                {
                    ApplyZoom(1.1);
                    e.Handled = true;
                }
                else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
                {
                    ApplyZoom(0.9);
                    e.Handled = true;
                }
            }
        }

        private void TutorialTimer_Tick(object sender, EventArgs e)
        {
            _tutorialTimer?.Stop();
            HideTutorial();
        }

        private void BtnCloseTutorial_Click(object sender, RoutedEventArgs e)
        {
            HideTutorial();
        }

        private void HideTutorial()
        {
            if (TutorialTooltip.Visibility == Visibility.Visible)
            {
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.5));
                fadeOut.Completed += (s, args) => TutorialTooltip.Visibility = Visibility.Collapsed;
                TutorialTooltip.BeginAnimation(OpacityProperty, fadeOut);
            }
        }

        private void BtnAddNode_Click(object sender, RoutedEventArgs e)
        {
            HideTutorial();
            string type = (sender as Button)?.Tag?.ToString() ?? "Event";
            CreateNode(type, new Point(200 + _nodes.Count * 20, 100 + _nodes.Count * 20));
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            // Lọc xóa tất cả các con ngoại trừ PreviewLink và GhostPreview
            var itemsToRemove = MindmapCanvas.Children
                .Cast<UIElement>()
                .Where(item => item != PreviewLink && item != GhostPreview)
                .ToList();

            foreach (var item in itemsToRemove)
            {
                MindmapCanvas.Children.Remove(item);
            }

            _nodes.Clear();
            _links.Clear();
            _linkSourceNode = null;
            _draggedNode = null;
            _isDrawingLink = false;
            PreviewLink.Visibility = Visibility.Collapsed;
            GhostPreview.Visibility = Visibility.Collapsed;
        }

        private void CreateNode(string type, Point position)
        {
            CreateNodeInternal(type, position, null, null);
        }

        private void CreateNodeInternal(string type, Point position, string? customId, string? customText)
        {
            string id = customId ?? Guid.NewGuid().ToString();
            
            var border = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Background = type == "Root" ? DS.Brush((Color)ColorConverter.ConvertFromString("#FCE4EC")) : DS.Brush((Color)ColorConverter.ConvertFromString("#E8EAF6")),
                BorderBrush = type == "Root" ? DS.Brush((Color)ColorConverter.ConvertFromString("#D81B60")) : DS.Brush((Color)ColorConverter.ConvertFromString("#3949AB")),
                BorderThickness = new Thickness(2),
                Tag = id,
                Cursor = Cursors.Hand
            };

            var shadow = new DropShadowEffect
            {
                BlurRadius = 8, ShadowDepth = 2, Opacity = 0.2, Color = Colors.Black
            };
            border.Effect = shadow;

            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new TextBlock 
            { 
                Text = type == "Root" ? "👑 " : "📜 ", 
                FontSize = 16 
            });
            var editBox = new TextBox
            {
                Text = customText ?? (type == "Root" ? "Nhân vật chính" : "Sự kiện"),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = type == "Root" ? DS.Brush((Color)ColorConverter.ConvertFromString("#AD1457")) : DS.Brush((Color)ColorConverter.ConvertFromString("#283593")),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            editBox.GotFocus += (s, e) => _focusedTextBox = editBox;
            editBox.TextChanged += (s, e) => TriggerAutoSave();
            sp.Children.Add(editBox);

            // The link connector button
            var linkBtn = new Border
            {
                Width = 16, Height = 16,
                CornerRadius = new CornerRadius(8),
                Background = type == "Root" ? DS.Brush((Color)ColorConverter.ConvertFromString("#D81B60")) : DS.Brush((Color)ColorConverter.ConvertFromString("#3949AB")),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, -20, 0),
                Cursor = Cursors.Cross
            };
            linkBtn.Child = new TextBlock { Text = "+", Foreground = Brushes.White, FontSize = 10, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            
            // Mouse events on connector
            linkBtn.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                _linkSourceNode = _nodes.FirstOrDefault(n => n.Id == id);
                _isDrawingLink = true;
                PreviewLink.Visibility = Visibility.Visible;
                UpdatePreviewLink(position); // Start at node center
            };

            var mainPanel = new Grid();
            mainPanel.Children.Add(sp);
            mainPanel.Children.Add(linkBtn);

            border.Child = mainPanel;

            border.SizeChanged += (s, e) => UpdateLinks();

            Canvas.SetLeft(border, position.X);
            Canvas.SetTop(border, position.Y);
            Canvas.SetZIndex(border, 10);
            
            border.MouseLeftButtonDown += Node_MouseLeftButtonDown;

            MindmapCanvas.Children.Add(border);

            var node = new MindmapNode
            {
                Id = id,
                Visual = border,
                Position = position,
                NodeType = type
            };
            _nodes.Add(node);
        }

        private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawingLink) return;

            var border = sender as Border;
            if (border != null)
            {
                border.CaptureMouse();
                _draggedNode = _nodes.FirstOrDefault(n => n.Visual == border);
                _dragStartPoint = e.GetPosition(MindmapCanvas);
                _nodeStartPos = _draggedNode.Position;
                
                Canvas.SetZIndex(border, 100);

                // Show ghost preview
                GhostPreview.Visibility = Visibility.Visible;
                Canvas.SetLeft(GhostPreview, _nodeStartPos.X);
                Canvas.SetTop(GhostPreview, _nodeStartPos.Y);
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Optional: Implement canvas panning or deselecting
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point currentPos = e.GetPosition(MindmapCanvas);

            if (_isDrawingLink && _linkSourceNode != null)
            {
                UpdatePreviewLink(currentPos);
            }
            else if (_draggedNode != null)
            {
                double dx = currentPos.X - _dragStartPoint.X;
                double dy = currentPos.Y - _dragStartPoint.Y;

                double nx = _nodeStartPos.X + dx;
                double ny = _nodeStartPos.Y + dy;
                if (nx < 20) nx = 20;
                if (ny < 20) ny = 20;
                Point newPos = new Point(nx, ny);
                
                // Update Ghost
                Canvas.SetLeft(GhostPreview, newPos.X);
                Canvas.SetTop(GhostPreview, newPos.Y);

                // Update real node
                _draggedNode.Position = newPos;
                Canvas.SetLeft(_draggedNode.Visual, newPos.X);
                Canvas.SetTop(_draggedNode.Visual, newPos.Y);

                UpdateLinks();
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawingLink)
            {
                // Find target node
                Point currentPos = e.GetPosition(MindmapCanvas);
                var targetNode = _nodes.FirstOrDefault(n => 
                    n != _linkSourceNode &&
                    currentPos.X >= n.Position.X && currentPos.X <= n.Position.X + n.Visual.ActualWidth &&
                    currentPos.Y >= n.Position.Y && currentPos.Y <= n.Position.Y + n.Visual.ActualHeight);

                if (targetNode != null)
                {
                    CreateLink(_linkSourceNode, targetNode);
                }

                _isDrawingLink = false;
                _linkSourceNode = null;
                PreviewLink.Visibility = Visibility.Collapsed;
            }

            if (_draggedNode != null)
            {
                _draggedNode.Visual.ReleaseMouseCapture();
                Canvas.SetZIndex(_draggedNode.Visual, 10);
                _draggedNode = null;
                GhostPreview.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdatePreviewLink(Point endPoint)
        {
            if (_linkSourceNode == null) return;
            
            double fw = _linkSourceNode.Visual.ActualWidth == 0 ? 150 : _linkSourceNode.Visual.ActualWidth;
            double fh = _linkSourceNode.Visual.ActualHeight == 0 ? 50 : _linkSourceNode.Visual.ActualHeight;

            Point startPoint;
            if (endPoint.X < _linkSourceNode.Position.X)
            {
                startPoint = new Point(_linkSourceNode.Position.X, _linkSourceNode.Position.Y + fh / 2);
            }
            else
            {
                startPoint = new Point(_linkSourceNode.Position.X + fw, _linkSourceNode.Position.Y + fh / 2);
            }

            PreviewLink.Data = CreateBezierPath(startPoint, endPoint);
            
            // Re-attach to canvas to ensure it's visible on top
            if (!MindmapCanvas.Children.Contains(PreviewLink))
            {
                MindmapCanvas.Children.Add(PreviewLink);
            }
        }

        private void CreateLink(MindmapNode from, MindmapNode to)
        {
            // Avoid duplicates
            if (_links.Any(l => l.FromId == from.Id && l.ToId == to.Id)) return;

            var path = new System.Windows.Shapes.Path
            {
                Stroke = DS.Brush((Color)ColorConverter.ConvertFromString("#BA68C8")),
                StrokeThickness = 3,
                IsHitTestVisible = false
            };

            Canvas.SetZIndex(path, 1);
            MindmapCanvas.Children.Add(path);

            var link = new MindmapLink
            {
                FromId = from.Id,
                ToId = to.Id,
                Visual = path
            };

            _links.Add(link);
            UpdateLinks();
        }

        private void UpdateLinks()
        {
            foreach (var link in _links)
            {
                var fromNode = _nodes.FirstOrDefault(n => n.Id == link.FromId);
                var toNode = _nodes.FirstOrDefault(n => n.Id == link.ToId);

                if (fromNode != null && toNode != null)
                {
                    double fw = fromNode.Visual.ActualWidth == 0 ? 150 : fromNode.Visual.ActualWidth;
                    double fh = fromNode.Visual.ActualHeight == 0 ? 50 : fromNode.Visual.ActualHeight;
                    double tw = toNode.Visual.ActualWidth == 0 ? 150 : toNode.Visual.ActualWidth;
                    double th = toNode.Visual.ActualHeight == 0 ? 50 : toNode.Visual.ActualHeight;

                    Point start = new Point(fromNode.Position.X + fw, fromNode.Position.Y + fh / 2);
                    Point end = new Point(toNode.Position.X, toNode.Position.Y + th / 2);

                    // If 'to' is on the left of 'from', adjust points
                    if (start.X > end.X + tw)
                    {
                        start = new Point(fromNode.Position.X, fromNode.Position.Y + fh / 2);
                        end = new Point(toNode.Position.X + tw, toNode.Position.Y + th / 2);
                    }

                    link.Visual.Data = CreateBezierPath(start, end);
                }
            }
        }

        private Geometry CreateBezierPath(Point start, Point end)
        {
            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure { StartPoint = start };

            double midX = (start.X + end.X) / 2;
            BezierSegment bezier = new BezierSegment(
                new Point(midX, start.Y),
                new Point(midX, end.Y),
                end,
                true);

            figure.Segments.Add(bezier);
            geometry.Figures.Add(figure);
            return geometry;
        }

        private void Emoji_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content != null && _focusedTextBox != null)
            {
                string emoji = btn.Content.ToString() ?? "";
                int caretIndex = _focusedTextBox.CaretIndex;
                _focusedTextBox.Text = _focusedTextBox.Text.Insert(caretIndex, emoji);
                _focusedTextBox.CaretIndex = caretIndex + emoji.Length;
                _focusedTextBox.Focus();
                TriggerAutoSave();
            }
        }

        private void ScrollViewer_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = sender as UIElement;
            e.Mode = ManipulationModes.Scale | ManipulationModes.Translate;
        }

        private void ScrollViewer_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null) return;

            // Xử lý zoom bằng co giãn 2 ngón tay
            if (e.Manipulators.Count() >= 2)
            {
                double scaleFactor = e.DeltaManipulation.Scale.X;
                if (scaleFactor > 0 && scaleFactor != 1.0)
                {
                    Point viewPosition = e.ManipulationOrigin;
                    UIElement? canvas = scrollViewer.Content as UIElement;
                    if (canvas != null)
                    {
                        Point canvasPosition = canvas.TransformToVisual(scrollViewer).Inverse.Transform(viewPosition);
                        ApplyZoom(scaleFactor, viewPosition, canvasPosition);
                    }
                }
            }
            // Xử lý cuộn bằng kéo thả 1 hoặc nhiều ngón tay
            else if (e.DeltaManipulation.Translation.X != 0 || e.DeltaManipulation.Translation.Y != 0)
            {
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.DeltaManipulation.Translation.X);
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.DeltaManipulation.Translation.Y);
            }
            e.Handled = true;
        }

        // ===========================================================
        //  AUTO-SAVE DRAFT FOR LITERATURE TOOL
        // ===========================================================

        private class LitNodeDto
        {
            public string Id { get; set; } = "";
            public double X { get; set; }
            public double Y { get; set; }
            public string NodeType { get; set; } = "";
            public string Text { get; set; } = "";
        }

        private class LitLinkDto
        {
            public string FromId { get; set; } = "";
            public string ToId { get; set; } = "";
        }

        private class LitDataDto
        {
            public List<LitNodeDto> Nodes { get; set; } = new();
            public List<LitLinkDto> Links { get; set; } = new();
        }

        private DispatcherTimer? _autoSaveTimer;

        private void InitializeAutoSave()
        {
            _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _autoSaveTimer.Tick += (s, e) =>
            {
                _autoSaveTimer.Stop();
                SaveDraft();
            };
        }

        private void TriggerAutoSave()
        {
            if (_autoSaveTimer != null)
            {
                _autoSaveTimer.Stop();
                _autoSaveTimer.Start();
            }
        }

        private string GetCurrentStateJson()
        {
            var nodesDto = new List<LitNodeDto>();
            foreach (var node in _nodes)
            {
                string text = "";
                if (node.Visual.Child is Grid grid && grid.Children[0] is StackPanel sp && sp.Children[1] is TextBox tb)
                {
                    text = tb.Text;
                }
                nodesDto.Add(new LitNodeDto
                {
                    Id = node.Id,
                    X = node.Position.X,
                    Y = node.Position.Y,
                    NodeType = node.NodeType,
                    Text = text
                });
            }

            var linksDto = _links.Select(l => new LitLinkDto
            {
                FromId = l.FromId,
                ToId = l.ToId
            }).ToList();

            var data = new LitDataDto { Nodes = nodesDto, Links = linksDto };
            return System.Text.Json.JsonSerializer.Serialize(data);
        }

        private void SaveDraft()
        {
            try
            {
                if (_nodes.Count == 0) return;
                string dir = global::System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QASmartClass", "Drafts");
                if (!global::System.IO.Directory.Exists(dir))
                {
                    global::System.IO.Directory.CreateDirectory(dir);
                }
                string file = global::System.IO.Path.Combine(dir, "literature_draft.json");
                string json = GetCurrentStateJson();
                global::System.IO.File.WriteAllText(file, json, System.Text.Encoding.UTF8);
            }
            catch { }
        }

        private void CheckAndRestoreDraft()
        {
            try
            {
                string dir = global::System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QASmartClass", "Drafts");
                string file = global::System.IO.Path.Combine(dir, "literature_draft.json");
                if (global::System.IO.File.Exists(file))
                {
                    var result = MessageBox.Show(
                        "Phần mềm phát hiện bản nháp sơ đồ tư duy Văn học chưa được lưu chính thức. Bạn có muốn khôi phục không?",
                        "Khôi phục bản nháp",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        string json = global::System.IO.File.ReadAllText(file, System.Text.Encoding.UTF8);
                        LoadFromJson(json);
                    }
                    global::System.IO.File.Delete(file);
                }
            }
            catch { }
        }

        private void LoadFromJson(string json)
        {
            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<LitDataDto>(json);
                if (data == null) return;

                var toRemove = MindmapCanvas.Children.Cast<UIElement>()
                    .Where(c => c != PreviewLink && c != GhostPreview).ToList();
                foreach (var c in toRemove) MindmapCanvas.Children.Remove(c);
                
                _nodes.Clear();
                _links.Clear();

                foreach (var nodeDto in data.Nodes)
                {
                    CreateNodeInternal(nodeDto.NodeType, new Point(nodeDto.X, nodeDto.Y), nodeDto.Id, nodeDto.Text);
                }

                foreach (var linkDto in data.Links)
                {
                    var fromNode = _nodes.FirstOrDefault(n => n.Id == linkDto.FromId);
                    var toNode = _nodes.FirstOrDefault(n => n.Id == linkDto.ToId);
                    if (fromNode != null && toNode != null)
                    {
                        CreateLink(fromNode, toNode);
                    }
                }
            }
            catch { }
        }

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new System.Collections.Generic.List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "🎬",
                    Title = isVN ? "Sân khấu hóa & Chuyển thể Điện ảnh" : "Theatre & Film Adaptation",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_literature_1_{suffix}.png",
                    Description = isVN 
                        ? "Các đạo diễn và biên kịch chuyển thể các tác phẩm văn học kinh điển (như Truyện Kiều, Chí Phèo) thành kịch bản phim, kịch nói sân khấu, giúp tác phẩm tiếp cận công chúng qua góc nhìn nghệ thuật thị giác hiện đại." 
                        : "Directors and screenwriters adapt classic literary works (such as The Tale of Kieu, Chi Pheo) into film scripts and theater plays, bringing the works to the public through modern visual arts."
                },
                new PracticalAppItem
                {
                    Icon = "✍️",
                    Title = isVN ? "Marketing Nội dung & Kể chuyện Thương hiệu" : "Content Marketing & Storytelling",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_literature_2_{suffix}.png",
                    Description = isVN 
                        ? "Nhà sáng tạo nội dung áp dụng các kết cấu tự sự, nghệ thuật xây dựng nhân vật và thông điệp nhân văn từ các tác phẩm văn học để viết bài quảng cáo, kịch bản truyền thông và xây dựng câu chuyện thương hiệu chạm đến cảm xúc." 
                        : "Content creators apply narrative structures, character development techniques, and humanistic messages from literature to write advertisements, media scripts, and build emotionally resonant brand stories."
                },
                new PracticalAppItem
                {
                    Icon = "🧠",
                    Title = isVN ? "Phân tích Tâm lý & Bản đồ Nhân vật" : "Psychoanalysis & Character Mapping",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_literature_3_{suffix}.png",
                    Description = isVN 
                        ? "Các chuyên gia tâm lý học hành vi và biên kịch sử dụng phương pháp lập sơ đồ mối quan hệ, phân tích động cơ hành vi của các nhân vật văn học để hiểu sâu hơn về tâm lý con người trong thực tế cuộc sống." 
                        : "Behavioral psychologists and writers map relationships and analyze the behavioral motivations of literary characters to gain a deeper understanding of human psychology in real-life scenarios."
                }
            };

            try
            {
                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for LiteratureTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewPractice == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewPractice.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewPractice.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
