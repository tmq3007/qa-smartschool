using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using QASmartClass.Data;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Multi
{
    public partial class MindmapTool : BaseToolControl
    {
        // -----------------------------------------------------------
        //  DATA MODEL — Cây node
        // -----------------------------------------------------------

        private class MindNode
        {
            public string Text { get; set; } = "Chủ đề";
            public double X { get; set; }
            public double Y { get; set; }
            public Color NodeColor { get; set; } = Color.FromRgb(25, 118, 210);
            public MindNode? Parent { get; set; }
            public List<MindNode> Children { get; } = new();
            public Border? Visual { get; set; }
            public Line? ConnectorLine { get; set; }
            public int Level { get; set; }
        }

        public enum LayoutType { Radial, Horizontal, Vertical }
        public enum LineStyleType { SolidNormal, SolidThick, DashedNormal, DashedThick }

        private LayoutType _currentLayout = LayoutType.Radial;
        private LineStyleType _currentLineStyle = LineStyleType.SolidNormal;

        private readonly List<MindNode> _allNodes = new();
        private MindNode? _selectedNode;
        private MindNode? _dragNode;
        private Point _dragOffset;
        private Action? _commitEditAction;

        // Bảng màu phân cấp (8 màu cho nhánh cấp 1)
        private static readonly Color[] BranchColors = {
            Color.FromRgb(25, 118, 210),   // Blue
            Color.FromRgb(46, 125, 50),    // Green
            Color.FromRgb(230, 81, 0),     // Orange
            Color.FromRgb(123, 31, 162),   // Purple
            Color.FromRgb(198, 40, 40),    // Red
            Color.FromRgb(0, 137, 123),    // Teal
            Color.FromRgb(255, 143, 0),    // Amber
            Color.FromRgb(69, 90, 100),    // BlueGrey
        };

        private readonly Stack<string> _undoStack = new();
        private readonly Stack<string> _redoStack = new();
        private bool _isApplyingState = false;
        private string? _dragStartStateJson;
        private readonly ScaleTransform _canvasScaleTransform = new ScaleTransform(1.0, 1.0);
        private Button? _btnQuickEdit;

        public MindmapTool()
        {
            InitializeComponent();
            this.Loaded += MindmapTool_Loaded;
            this.Unloaded += MindmapTool_Unloaded;
            mindmapCanvas.LayoutTransform = _canvasScaleTransform;
            InitializeQuickEditButton();
            InitializeAutoSave();
            InitializeEmojiPanel();
            UpdateUndoRedoButtonsState();
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
                ApplyZoom(zoomFactor, e.GetPosition(mindmapScrollViewer), e.GetPosition(mindmapCanvas));
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
            Point viewPoint = viewPosition ?? new Point(mindmapScrollViewer.ViewportWidth / 2, mindmapScrollViewer.ViewportHeight / 2);
            Point canvasPoint = canvasPosition ?? mindmapCanvas.TransformToVisual(mindmapScrollViewer).Inverse.Transform(viewPoint);

            _canvasScaleTransform.ScaleX = newScaleX;
            _canvasScaleTransform.ScaleY = newScaleX;

            // Update UI layout immediately so scrollviewer knows new canvas bounds
            mindmapScrollViewer.UpdateLayout();

            // Adjust scroll offsets
            double newHoriz = canvasPoint.X * newScaleX - viewPoint.X;
            double newVert = canvasPoint.Y * newScaleX - viewPoint.Y;

            mindmapScrollViewer.ScrollToHorizontalOffset(newHoriz);
            mindmapScrollViewer.ScrollToVerticalOffset(newVert);

            // Update zoom percentage text
            if (txtZoomPercent != null)
            {
                txtZoomPercent.Text = $"{(int)System.Math.Round(newScaleX * 100)}%";
            }

            // Apply Inverse Scale to Quick Edit Button
            UpdateQuickEditButtonScale();
        }

        private void UpdateQuickEditButtonScale()
        {
            if (_btnQuickEdit != null)
            {
                double scale = _canvasScaleTransform.ScaleX;
                _btnQuickEdit.RenderTransform = new ScaleTransform(1.0 / scale, 1.0 / scale);
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

        private void MindmapTool_Loaded(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.PreviewKeyDown -= ParentWindow_PreviewKeyDown;
                parentWindow.PreviewKeyDown += ParentWindow_PreviewKeyDown;
            }
            CheckAndRestoreDraft();
            LoadPracticalApps();

            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Ghi chú" : "Study Guide & Notes";
            if (menuTextMindmap != null) menuTextMindmap.Text = isVN ? "Bản đồ tư duy" : "Mindmap Canvas";
            if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

            if (sideMenu != null)
            {
                sideMenu.SelectedIndex = 0; // Default to mindmap canvas
            }
        }

        private void MindmapTool_Unloaded(object sender, RoutedEventArgs e)
        {
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
                if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    Undo();
                    e.Handled = true;
                }
                else if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    Redo();
                    e.Handled = true;
                }
                else if (e.Key == Key.OemPlus || e.Key == Key.Add)
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

        private void InitializeQuickEditButton()
        {
            _btnQuickEdit = new Button
            {
                Content = "✏️",
                Width = 32,
                Height = 32,
                Padding = new Thickness(0),
                FontSize = 14,
                Cursor = Cursors.Hand,
                Visibility = Visibility.Collapsed,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                BorderThickness = new Thickness(1.5),
                RenderTransformOrigin = new Point(0.5, 0.5),
                ToolTip = "Sửa nội dung nhanh"
            };

            // Circular style template in code
            var style = new Style(typeof(Button));
            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "border";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(16));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));

            var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentPresenterFactory);
            template.VisualTree = borderFactory;
            style.Setters.Add(new Setter(Button.TemplateProperty, template));
            _btnQuickEdit.Style = style;

            _btnQuickEdit.Click += (s, e) =>
            {
                if (_selectedNode != null)
                {
                    EditNodeText(_selectedNode);
                }
                e.Handled = true;
            };

            mindmapCanvas.Children.Add(_btnQuickEdit);
            Canvas.SetZIndex(_btnQuickEdit, 1000);
            UpdateQuickEditButtonScale();
        }

        // -----------------------------------------------------------
        //  NODE CREATION & RENDERING
        // -----------------------------------------------------------

        private MindNode CreateNode(string text, double x, double y, MindNode? parent, Color? color = null)
        {
            int level = parent == null ? 0 : parent.Level + 1;
            Color nodeColor;
            if (color.HasValue)
                nodeColor = color.Value;
            else if (parent == null)
                nodeColor = Color.FromRgb(33, 33, 33); // Root = dark
            else if (level == 1)
                nodeColor = BranchColors[parent.Children.Count % BranchColors.Length];
            else
                nodeColor = parent.NodeColor; // Inherit parent color

            var node = new MindNode
            {
                Text = text, X = x, Y = y,
                Parent = parent, NodeColor = nodeColor, Level = level
            };

            // Visual border
            double fontSize = level == 0 ? 20 : level == 1 ? 16 : 14;
            double padH = level == 0 ? 20 : 14;
            double padV = level == 0 ? 12 : 8;
            double cornerR = level == 0 ? 14 : 10;

            double luminance = (0.299 * nodeColor.R + 0.587 * nodeColor.G + 0.114 * nodeColor.B);
            Brush foregroundBrush = luminance > 130 ? new SolidColorBrush(Color.FromRgb(33, 33, 33)) : Brushes.White;

            var textBlock = new TextBlock
            {
                Text = text, FontSize = fontSize,
                FontWeight = level == 0 ? FontWeights.Bold : level == 1 ? FontWeights.Bold : FontWeights.SemiBold,
                Foreground = foregroundBrush,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = level == 0 ? 260 : 200,
                TextAlignment = TextAlignment.Center,
                FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI")
            };

            var border = new Border
            {
                Background = new SolidColorBrush(nodeColor),
                CornerRadius = new CornerRadius(cornerR),
                Padding = new Thickness(padH, padV, padH, padV),
                Cursor = Cursors.Hand,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 6, ShadowDepth = 2, Opacity = 0.2, Color = Colors.Black
                },
                Child = textBlock
            };

            border.MouseRightButtonDown += (s, e) => { ShowNodeMenu(node, e); e.Handled = true; };
            border.MouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed && _dragNode == node)
                {
                    var pos = e.GetPosition(mindmapCanvas);
                    double nx = pos.X - _dragOffset.X;
                    double ny = pos.Y - _dragOffset.Y;
                    if (nx < 20) nx = 20;
                    if (ny < 20) ny = 20;

                    if (_dragStartStateJson != null)
                    {
                        if (!_isApplyingState)
                        {
                            _undoStack.Push(_dragStartStateJson);
                            _redoStack.Clear();
                        }
                        _dragStartStateJson = null;
                    }

                    node.X = nx;
                    node.Y = ny;
                    Canvas.SetLeft(border, node.X);
                    Canvas.SetTop(border, node.Y);
                    UpdateConnectors(node);
                    UpdateCanvasSize();

                    // Update quick edit button position
                    if (_btnQuickEdit != null && _selectedNode == node)
                    {
                        double nw = border.ActualWidth > 0 ? border.ActualWidth : 120;
                        Canvas.SetLeft(_btnQuickEdit, node.X + nw - 10);
                        Canvas.SetTop(_btnQuickEdit, node.Y - 10);
                    }
                }
            };
            border.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    EditNodeText(node);
                    e.Handled = true;
                    return;
                }
                SelectNode(node);
                _dragStartStateJson = GetCurrentStateJson();
                _dragNode = node;
                _dragOffset = e.GetPosition(border);
                border.CaptureMouse();
                e.Handled = true;
            };
            border.MouseLeftButtonUp += (s, e) =>
            {
                _dragNode = null;
                border.ReleaseMouseCapture();
                _dragStartStateJson = null;
            };

            node.Visual = border;

            // Connector line to parent
            if (parent != null)
            {
                var line = new Line
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(120, nodeColor.R, nodeColor.G, nodeColor.B)),
                    StrokeThickness = level == 1 ? 3 : 2,
                    StrokeDashArray = level > 2 ? new DoubleCollection(new[] { 4.0, 2.0 }) : null
                };
                node.ConnectorLine = line;
                mindmapCanvas.Children.Add(line);
                Canvas.SetZIndex(line, -1);
                parent.Children.Add(node);
            }

            Canvas.SetLeft(border, x);
            Canvas.SetTop(border, y);
            mindmapCanvas.Children.Add(border);

            border.SizeChanged += (s, e) =>
            {
                UpdateConnectors(node);
                if (_btnQuickEdit != null && _selectedNode == node)
                {
                    double nw = border.ActualWidth > 0 ? border.ActualWidth : 120;
                    Canvas.SetLeft(_btnQuickEdit, node.X + nw - 10);
                    Canvas.SetTop(_btnQuickEdit, node.Y - 10);
                }
            };

            _allNodes.Add(node);
            UpdateConnectors(node);
            UpdateEmptyState();
            UpdateCanvasSize();
            return node;
        }

        private void UpdateConnectors(MindNode node)
        {
            // Update line from this node to parent
            if (node.ConnectorLine != null && node.Parent?.Visual != null && node.Visual != null)
            {
                double pw = node.Parent.Visual.ActualWidth > 0 ? node.Parent.Visual.ActualWidth : 100;
                double ph = node.Parent.Visual.ActualHeight > 0 ? node.Parent.Visual.ActualHeight : 40;
                double nw = node.Visual.ActualWidth > 0 ? node.Visual.ActualWidth : 80;
                double nh = node.Visual.ActualHeight > 0 ? node.Visual.ActualHeight : 30;

                node.ConnectorLine.X1 = node.Parent.X + pw / 2;
                node.ConnectorLine.Y1 = node.Parent.Y + ph / 2;
                node.ConnectorLine.X2 = node.X + nw / 2;
                node.ConnectorLine.Y2 = node.Y + nh / 2;

                // Apply dynamic styles
                node.ConnectorLine.Stroke = new SolidColorBrush(Color.FromArgb(120, node.NodeColor.R, node.NodeColor.G, node.NodeColor.B));
                switch (_currentLineStyle)
                {
                    case LineStyleType.SolidNormal:
                        node.ConnectorLine.StrokeThickness = 2;
                        node.ConnectorLine.StrokeDashArray = null;
                        break;
                    case LineStyleType.SolidThick:
                        node.ConnectorLine.StrokeThickness = 5;
                        node.ConnectorLine.StrokeDashArray = null;
                        break;
                    case LineStyleType.DashedNormal:
                        node.ConnectorLine.StrokeThickness = 2;
                        node.ConnectorLine.StrokeDashArray = new DoubleCollection(new[] { 4.0, 4.0 });
                        break;
                    case LineStyleType.DashedThick:
                        node.ConnectorLine.StrokeThickness = 4;
                        node.ConnectorLine.StrokeDashArray = new DoubleCollection(new[] { 4.0, 4.0 });
                        break;
                }
            }

            // Also update children connectors
            foreach (var child in node.Children)
                UpdateConnectors(child);
        }

        private void SelectNode(MindNode node)
        {
            if (_commitEditAction != null)
            {
                _commitEditAction.Invoke();
                _commitEditAction = null;
            }

            // Deselect old
            if (_selectedNode?.Visual != null)
            {
                _selectedNode.Visual.BorderBrush = null;
                _selectedNode.Visual.BorderThickness = new Thickness(0);
            }

            _selectedNode = node;
            if (node.Visual != null)
            {
                node.Visual.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                node.Visual.BorderThickness = new Thickness(3);
            }

            btnAddChild.IsEnabled = true;
            btnDeleteNode.IsEnabled = node.Parent != null; // Enable for all non-root nodes

            if (_btnQuickEdit != null && node.Visual != null)
            {
                node.Visual.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!mindmapCanvas.Children.Contains(_btnQuickEdit))
                    {
                        mindmapCanvas.Children.Add(_btnQuickEdit);
                    }
                    double nw = node.Visual.ActualWidth > 0 ? node.Visual.ActualWidth : 120;
                    Canvas.SetLeft(_btnQuickEdit, node.X + nw - 10);
                    Canvas.SetTop(_btnQuickEdit, node.Y - 10);
                    UpdateQuickEditButtonScale();
                    _btnQuickEdit.Visibility = Visibility.Visible;
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void ShowNodeMenu(MindNode node, MouseButtonEventArgs e)
        {
            var menu = new ContextMenu();

            var editItem = new MenuItem { Header = "📝 Sửa nội dung" };
            editItem.Click += (_, _) => EditNodeText(node);
            menu.Items.Add(editItem);

            var colorItem = new MenuItem { Header = "🎨 Đổi màu" };
            foreach (var c in BranchColors)
            {
                var mi = new MenuItem
                {
                    Header = new Border
                    {
                        Background = new SolidColorBrush(c),
                        Width = 60, Height = 16, CornerRadius = new CornerRadius(3)
                    }
                };
                var captured = c;
                mi.Click += (_, _) => ChangeNodeColor(node, captured);
                colorItem.Items.Add(mi);
            }
            menu.Items.Add(colorItem);

            if (node.Parent != null) // Don't delete root
            {
                menu.Items.Add(new Separator());
                var deleteItem = new MenuItem { Header = "🗑️ Xóa nhánh này" };
                deleteItem.Click += (_, _) => DeleteNode(node);
                menu.Items.Add(deleteItem);
            }

            menu.IsOpen = true;
        }

        private void EditNodeText(MindNode node)
        {
            if (_commitEditAction != null)
            {
                _commitEditAction.Invoke();
                _commitEditAction = null;
            }

            if (node.Visual == null) return;
            var border = (Border)node.Visual;
            var tb = border.Child as TextBlock;
            if (tb == null) return;

            var editBox = new TextBox
            {
                Text = node.Text,
                FontSize = tb.FontSize,
                FontWeight = tb.FontWeight,
                FontFamily = tb.FontFamily,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Attach Touch Keyboard
            QASmartClass.LearningTools.Controls.TouchTextPad.Attach(editBox, mode: "text");

            Action commit = () =>
            {
                if (border.Child != tb)
                {
                    if (node.Text != editBox.Text)
                    {
                        PushState();
                        node.Text = editBox.Text;
                        tb.Text = editBox.Text;
                    }
                    border.Child = tb;
                }
            };
            
            _commitEditAction = commit;

            editBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter) 
                {
                    commit();
                    _commitEditAction = null;
                }
                else if (e.Key == Key.Escape)
                {
                    border.Child = tb;
                    _commitEditAction = null;
                }
            };

            editBox.LostFocus += (s, e) =>
            {
                editBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var focused = Keyboard.FocusedElement as DependencyObject;
                    bool isPopupFocused = false;
                    var parent = focused;
                    while (parent != null)
                    {
                        if (parent.GetType().Name.Contains("Popup") || parent is System.Windows.Controls.Primitives.Popup)
                        {
                            isPopupFocused = true;
                            break;
                        }
                        DependencyObject? nextParent = null;
                        if (parent is Visual || parent is System.Windows.Media.Media3D.Visual3D)
                        {
                            nextParent = VisualTreeHelper.GetParent(parent);
                        }
                        parent = nextParent ?? LogicalTreeHelper.GetParent(parent);
                    }

                    if (!isPopupFocused)
                    {
                        if (_commitEditAction != null)
                        {
                            commit();
                            _commitEditAction = null;
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            };

            border.Child = editBox;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() =>
            {
                editBox.Focus();
                editBox.SelectAll();
            }));
        }

        private void ChangeNodeColor(MindNode node, Color color)
        {
            if (node.NodeColor != color)
            {
                PushState();
                ChangeNodeColorInternal(node, color);
            }
        }

        private void ChangeNodeColorInternal(MindNode node, Color color)
        {
            node.NodeColor = color;
            if (node.Visual != null)
            {
                node.Visual.Background = new SolidColorBrush(color);
                double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
                Brush fg = luminance > 130 ? new SolidColorBrush(Color.FromRgb(33, 33, 33)) : Brushes.White;
                if (node.Visual.Child is TextBlock tb)
                {
                    tb.Foreground = fg;
                }
                else if (node.Visual.Child is TextBox tbox)
                {
                    tbox.Foreground = fg;
                }
            }
            if (node.ConnectorLine != null)
                node.ConnectorLine.Stroke = new SolidColorBrush(Color.FromArgb(120, color.R, color.G, color.B));
        }

        private void DeleteNode(MindNode node)
        {
            // Delete children recursively
            foreach (var child in node.Children.ToList())
                DeleteNode(child);

            // Remove visuals
            if (node.Visual != null) mindmapCanvas.Children.Remove(node.Visual);
            if (node.ConnectorLine != null) mindmapCanvas.Children.Remove(node.ConnectorLine);

            // Remove from parent
            node.Parent?.Children.Remove(node);

            _allNodes.Remove(node);
            if (_selectedNode == node)
            {
                _selectedNode = null;
                btnAddChild.IsEnabled = false;
                btnDeleteNode.IsEnabled = false;

                if (_btnQuickEdit != null)
                {
                    _btnQuickEdit.Visibility = Visibility.Collapsed;
                }
            }
            UpdateEmptyState();
        }

        private void UpdateEmptyState()
        {
            emptyState.Visibility = _allNodes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateCanvasSize()
        {
            double maxX = 0;
            double maxY = 0;
            foreach (var node in _allNodes)
            {
                double w = node.Visual != null && node.Visual.ActualWidth > 0 ? node.Visual.ActualWidth : 120;
                double h = node.Visual != null && node.Visual.ActualHeight > 0 ? node.Visual.ActualHeight : 40;
                if (node.X + w > maxX) maxX = node.X + w;
                if (node.Y + h > maxY) maxY = node.Y + h;
            }
            
            double viewWidth = mindmapCanvas.Parent is FrameworkElement parent ? parent.ActualWidth : 800;
            double viewHeight = mindmapCanvas.Parent is FrameworkElement parent2 ? parent2.ActualHeight : 600;
            if (viewWidth == 0) viewWidth = 800;
            if (viewHeight == 0) viewHeight = 600;

            mindmapCanvas.Width = System.Math.Max(viewWidth, maxX + 200);
            mindmapCanvas.Height = System.Math.Max(viewHeight, maxY + 200);
        }

        private void ToggleLayout_Click(object sender, RoutedEventArgs e)
        {
            PushState();
            _currentLayout = (LayoutType)(((int)_currentLayout + 1) % 3);
            RelayoutAll();
        }

        private void ToggleStyle_Click(object sender, RoutedEventArgs e)
        {
            PushState();
            _currentLineStyle = (LineStyleType)(((int)_currentLineStyle + 1) % 4);
            foreach (var node in _allNodes)
                UpdateConnectors(node);
        }

        private void ChangeColor_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            foreach (var c in BranchColors)
            {
                var mi = new MenuItem
                {
                    Header = new Border
                    {
                        Background = new SolidColorBrush(c),
                        Width = 100, Height = 20, CornerRadius = new CornerRadius(4)
                    }
                };
                var captured = c;
                mi.Click += (_, _) => 
                {
                    if (_selectedNode != null)
                    {
                        ChangeNodeColor(_selectedNode, captured);
                        UpdateConnectors(_selectedNode);
                    }
                    else
                    {
                        // Change color for all nodes
                        if (_allNodes.Any(n => n.NodeColor != captured))
                        {
                            PushState();
                            foreach (var n in _allNodes)
                            {
                                ChangeNodeColorInternal(n, captured);
                                UpdateConnectors(n);
                            }
                        }
                    }
                };
                menu.Items.Add(mi);
            }
            
            var btn = sender as Button;
            menu.PlacementTarget = btn;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private bool _isAnimatingLayout = false;
        private DateTime _animationStartTime;
        private double _animationDurationMs = 300;

        private void StartLayoutAnimation()
        {
            if (_isAnimatingLayout) return;
            _isAnimatingLayout = true;
            _animationStartTime = DateTime.Now;
            CompositionTarget.Rendering += OnCompositionTargetRendering;
        }

        private void OnCompositionTargetRendering(object? sender, EventArgs e)
        {
            var elapsed = (DateTime.Now - _animationStartTime).TotalMilliseconds;
            
            foreach (var node in _allNodes)
            {
                UpdateConnectors(node);
            }
            UpdateCanvasSize();

            if (elapsed >= _animationDurationMs)
            {
                CompositionTarget.Rendering -= OnCompositionTargetRendering;
                _isAnimatingLayout = false;
                
                // Finalize layout by clearing animations and setting local values to restore drag ability
                foreach (var node in _allNodes)
                {
                    if (node.Visual != null)
                    {
                        double finalX = Canvas.GetLeft(node.Visual);
                        double finalY = Canvas.GetTop(node.Visual);
                        node.Visual.BeginAnimation(Canvas.LeftProperty, null);
                        node.Visual.BeginAnimation(Canvas.TopProperty, null);
                        Canvas.SetLeft(node.Visual, finalX);
                        Canvas.SetTop(node.Visual, finalY);
                    }
                    UpdateConnectors(node);
                }
                UpdateCanvasSize();
            }
        }

        private void RelayoutAll()
        {
            if (_allNodes.Count == 0) return;
            var roots = _allNodes.Where(n => n.Parent == null).ToList();
            if (roots.Count == 0) return;

            var root = roots[0];
            double cx = mindmapCanvas.ActualWidth / 2 - 60;
            double cy = mindmapCanvas.ActualHeight / 2 - 20;
            if (cx <= 0) cx = 400;
            if (cy <= 0) cy = 300;

            // 1. Save old positions
            var oldPositions = _allNodes.ToDictionary(n => n, n => new Point(n.X, n.Y));

            // 2. Compute new positions in n.X, n.Y
            if (_currentLayout == LayoutType.Radial)
            {
                root.X = cx; root.Y = cy;
                RelayoutRadial(root, cx, cy, 180, 0, 2 * System.Math.PI);
            }
            else if (_currentLayout == LayoutType.Horizontal)
            {
                root.X = 100; root.Y = cy;
                RelayoutHorizontal(root, 100, cy, 220);
            }
            else if (_currentLayout == LayoutType.Vertical)
            {
                root.X = cx; root.Y = 50;
                RelayoutVertical(root, cx, 50, 150);
            }

            // 3. Animate each node from old positions to new positions
            StartLayoutAnimation();
            foreach (var n in _allNodes)
            {
                if (n.Visual != null)
                {
                    double startX = oldPositions.ContainsKey(n) ? oldPositions[n].X : n.X;
                    double startY = oldPositions.ContainsKey(n) ? oldPositions[n].Y : n.Y;
                    
                    Canvas.SetLeft(n.Visual, startX);
                    Canvas.SetTop(n.Visual, startY);

                    var animX = new DoubleAnimation(startX, n.X, TimeSpan.FromMilliseconds(_animationDurationMs))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };
                    var animY = new DoubleAnimation(startY, n.Y, TimeSpan.FromMilliseconds(_animationDurationMs))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };

                    n.Visual.BeginAnimation(Canvas.LeftProperty, animX);
                    n.Visual.BeginAnimation(Canvas.TopProperty, animY);
                }
            }
        }

        private void RelayoutRadial(MindNode parent, double px, double py, double radius, double startAngle, double sweepAngle)
        {
            int count = parent.Children.Count;
            if (count == 0) return;
            double step = sweepAngle / count;
            for (int i = 0; i < count; i++)
            {
                var child = parent.Children[i];
                double angle = startAngle + i * step + (sweepAngle == 2 * System.Math.PI ? 0 : step / 2);
                child.X = px + radius * System.Math.Cos(angle);
                child.Y = py + radius * System.Math.Sin(angle);
                RelayoutRadial(child, child.X, child.Y, radius * 0.8, angle - System.Math.PI / 4, System.Math.PI / 2);
            }
        }

        private double CalculateSubtreeHeight(MindNode node)
        {
            if (node.Children.Count == 0)
            {
                return 60; // Chiều cao mặc định của một khối nút bao gồm khoảng cách an toàn
            }
            double total = 0;
            foreach (var child in node.Children)
            {
                total += CalculateSubtreeHeight(child);
            }
            return System.Math.Max(total, 60);
        }

        private double CalculateSubtreeWidth(MindNode node)
        {
            if (node.Children.Count == 0)
            {
                return 150; // Chiều rộng mặc định của một khối nút bao gồm khoảng cách an toàn
            }
            double total = 0;
            foreach (var child in node.Children)
            {
                total += CalculateSubtreeWidth(child);
            }
            return System.Math.Max(total, 150);
        }

        private void RelayoutHorizontal(MindNode parent, double px, double py, double offsetX)
        {
            int count = parent.Children.Count;
            if (count == 0) return;

            double totalChildrenHeight = 0;
            double[] childHeights = new double[count];
            for (int i = 0; i < count; i++)
            {
                childHeights[i] = CalculateSubtreeHeight(parent.Children[i]);
                totalChildrenHeight += childHeights[i];
            }

            double parentHeight = parent.Visual != null && parent.Visual.ActualHeight > 0 ? parent.Visual.ActualHeight : 40;
            double parentCenterY = py + parentHeight / 2;

            double startY = parentCenterY - totalChildrenHeight / 2;
            double currentY = startY;

            for (int i = 0; i < count; i++)
            {
                var child = parent.Children[i];
                double childHeight = parentHeight;
                if (child.Visual != null && child.Visual.ActualHeight > 0)
                    childHeight = child.Visual.ActualHeight;

                child.X = px + offsetX;
                child.Y = currentY + (childHeights[i] - childHeight) / 2;

                RelayoutHorizontal(child, child.X, currentY, offsetX * 0.9);
                currentY += childHeights[i];
            }
        }

        private void RelayoutVertical(MindNode parent, double px, double py, double offsetY)
        {
            int count = parent.Children.Count;
            if (count == 0) return;

            double totalChildrenWidth = 0;
            double[] childWidths = new double[count];
            for (int i = 0; i < count; i++)
            {
                childWidths[i] = CalculateSubtreeWidth(parent.Children[i]);
                totalChildrenWidth += childWidths[i];
            }

            double parentWidth = parent.Visual != null && parent.Visual.ActualWidth > 0 ? parent.Visual.ActualWidth : 120;
            double parentCenterX = px + parentWidth / 2;

            double startX = parentCenterX - totalChildrenWidth / 2;
            double currentX = startX;

            for (int i = 0; i < count; i++)
            {
                var child = parent.Children[i];
                double childWidth = parentWidth;
                if (child.Visual != null && child.Visual.ActualWidth > 0)
                    childWidth = child.Visual.ActualWidth;

                child.X = currentX + (childWidths[i] - childWidth) / 2;
                child.Y = py + offsetY;

                RelayoutVertical(child, currentX, child.Y, offsetY * 0.9);
                currentX += childWidths[i];
            }
        }

        // -----------------------------------------------------------
        //  TOOLBAR EVENTS
        // -----------------------------------------------------------

        private void AddCenter_Click(object sender, RoutedEventArgs e)
        {
            // Chỉ cho phép 1 root
            if (_allNodes.Any(n => n.Parent == null))
            {
                MessageBox.Show("Đã có chủ đề trung tâm.\nHãy chọn node và nhấn '➕ Thêm nhánh' để mở rộng.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            PushState();
            double cx = mindmapCanvas.ActualWidth / 2 - 60;
            double cy = mindmapCanvas.ActualHeight / 2 - 20;
            var root = CreateNode("Chủ đề chính", cx, cy, null);
            SelectNode(root);

            // Schedule connector update after layout
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
            {
                foreach (var n in _allNodes) UpdateConnectors(n);
            });
        }

        private void AddChild_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNode == null) return;

            PushState();
            int childCount = _selectedNode.Children.Count;
            double angle = -System.Math.PI / 3 + childCount * (System.Math.PI / 4);
            double dist = _selectedNode.Level == 0 ? 180 : 140;
            double cx = _selectedNode.X + dist * System.Math.Cos(angle);
            double cy = _selectedNode.Y + dist * System.Math.Sin(angle);

            var child = CreateNode($"Nhánh {childCount + 1}", cx, cy, _selectedNode);
            SelectNode(child);

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
            {
                foreach (var n in _allNodes) UpdateConnectors(n);
            });
        }

        private void DeleteNode_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNode != null && _selectedNode.Parent != null)
            {
                PushState();
                DeleteNode(_selectedNode);
            }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_commitEditAction != null)
            {
                _commitEditAction.Invoke();
                _commitEditAction = null;
            }

            // Deselect when clicking empty area
            if (_selectedNode != null)
            {
                if (_selectedNode.Visual != null)
                {
                    _selectedNode.Visual.BorderBrush = null;
                    _selectedNode.Visual.BorderThickness = new Thickness(0);
                }
                _selectedNode = null;
                btnAddChild.IsEnabled = false;
                btnDeleteNode.IsEnabled = false;

                if (_btnQuickEdit != null)
                {
                    _btnQuickEdit.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (_allNodes.Count == 0) return;
            var result = MessageBox.Show("Bạn chắc chắn muốn xóa toàn bộ sơ đồ?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                PushState();
                mindmapCanvas.Children.Clear();
                _allNodes.Clear();
                _selectedNode = null;
                btnAddChild.IsEnabled = false;
                btnDeleteNode.IsEnabled = false;
                UpdateEmptyState();
                mindmapCanvas.Width = double.NaN;
                mindmapCanvas.Height = double.NaN;

                InitializeQuickEditButton();
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (_allNodes.Count == 0)
            {
                MessageBox.Show("Sơ đồ trống. Hãy tạo nội dung trước khi export.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG Image|*.png|SVG Vector Image|*.svg",
                    FileName = "SoDoTuDuy_" + DateTime.Now.ToString("yyyyMMdd_HHmm"),
                    DefaultExt = ".png"
                };
                if (dlg.ShowDialog() == true)
                {
                    string ext = System.IO.Path.GetExtension(dlg.FileName).ToLower();
                    if (ext == ".svg")
                    {
                        var result = MessageBox.Show("Bạn có muốn xuất ảnh SVG với nền trong suốt (không có nền trắng) không?\n\n- Chọn 'Yes' để làm trong suốt nền.\n- Chọn 'No' để giữ nền trắng mặc định.", 
                                                     "Tùy chọn xuất ảnh Vector SVG", 
                                                     MessageBoxButton.YesNo, 
                                                     MessageBoxImage.Question);
                        bool isTransparent = (result == MessageBoxResult.Yes);
                        ExportToSvg(dlg.FileName, isTransparent);
                    }
                    else
                    {
                        var bounds = VisualTreeHelper.GetDescendantBounds(mindmapCanvas);
                        double dpi = 144;
                        var rtb = new RenderTargetBitmap(
                            (int)(bounds.Width * dpi / 96), (int)(bounds.Height * dpi / 96),
                            dpi, dpi, PixelFormats.Pbgra32);

                        var dv = new DrawingVisual();
                        using (var ctx = dv.RenderOpen())
                        {
                            ctx.DrawRectangle(Brushes.White, null, new Rect(bounds.Size));
                            var vb = new VisualBrush(mindmapCanvas)
                            {
                                ViewboxUnits = BrushMappingMode.Absolute,
                                Viewbox = bounds
                            };
                            ctx.DrawRectangle(vb, null, new Rect(bounds.Size));
                        }
                        rtb.Render(dv);

                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(rtb));
                        using var fs = File.OpenWrite(dlg.FileName);
                        encoder.Save(fs);
                    }

                    MessageBox.Show($"Đã lưu sơ đồ tại:\n{dlg.FileName}",
                        "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi export: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private void ExportToSvg(string fileName, bool isTransparent = false)
        {
            if (_allNodes.Count == 0) return;

            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            foreach (var node in _allNodes)
            {
                double nw = node.Visual?.ActualWidth ?? 140;
                double nh = node.Visual?.ActualHeight ?? 50;

                if (node.X < minX) minX = node.X;
                if (node.Y < minY) minY = node.Y;
                if (node.X + nw > maxX) maxX = node.X + nw;
                if (node.Y + nh > maxY) maxY = node.Y + nh;
            }

            double padding = 40;
            minX -= padding;
            minY -= padding;
            double width = (maxX - minX) + padding;
            double height = (maxY - minY) + padding;

            if (width < 100) width = 800;
            if (height < 100) height = 600;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine($"<svg width=\"{width}\" height=\"{height}\" viewBox=\"{minX} {minY} {width} {height}\" xmlns=\"http://www.w3.org/2000/svg\">");
            sb.AppendLine("  <defs>");
            sb.AppendLine("    <style type=\"text/css\">");
            sb.AppendLine("      @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;700&amp;display=swap');");
            sb.AppendLine("      text {");
            sb.AppendLine("        font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;");
            sb.AppendLine("        font-weight: bold;");
            sb.AppendLine("      }");
            sb.AppendLine("    </style>");
            sb.AppendLine("  </defs>");
            if (!isTransparent)
            {
                sb.AppendLine($"  <rect x=\"{minX}\" y=\"{minY}\" width=\"{width}\" height=\"{height}\" fill=\"#FFFFFF\"/>");
            }

            foreach (var node in _allNodes)
            {
                if (node.ConnectorLine != null)
                {
                    double x1 = node.ConnectorLine.X1;
                    double y1 = node.ConnectorLine.Y1;
                    double x2 = node.ConnectorLine.X2;
                    double y2 = node.ConnectorLine.Y2;

                    string strokeColor = "#B0BEC5";
                    if (node.ConnectorLine.Stroke is SolidColorBrush scb)
                    {
                        strokeColor = ColorToHex(scb.Color);
                    }

                    double strokeThickness = node.ConnectorLine.StrokeThickness;
                    if (strokeThickness <= 0) strokeThickness = 2;

                    string dashArray = "";
                    if (node.ConnectorLine.StrokeDashArray != null && node.ConnectorLine.StrokeDashArray.Count > 0)
                    {
                        dashArray = " stroke-dasharray=\"5,5\"";
                    }

                    sb.AppendLine($"  <line x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y2}\" stroke=\"{strokeColor}\" stroke-width=\"{strokeThickness}\"{dashArray}/>");
                }
            }

            foreach (var node in _allNodes)
            {
                double nw = node.Visual?.ActualWidth ?? 140;
                double nh = node.Visual?.ActualHeight ?? 50;

                string bgColor = ColorToHex(node.NodeColor);
                
                Color strokeC = Color.FromRgb(
                    (byte)System.Math.Max(0, node.NodeColor.R * 0.8),
                    (byte)System.Math.Max(0, node.NodeColor.G * 0.8),
                    (byte)System.Math.Max(0, node.NodeColor.B * 0.8)
                );
                string strokeColor = ColorToHex(strokeC);

                double luminance = (0.299 * node.NodeColor.R + 0.587 * node.NodeColor.G + 0.114 * node.NodeColor.B);
                string textColor = luminance > 130 ? "#212121" : "#FFFFFF";

                sb.AppendLine($"  <rect x=\"{node.X}\" y=\"{node.Y}\" width=\"{nw}\" height=\"{nh}\" rx=\"10\" ry=\"10\" fill=\"{bgColor}\" stroke=\"{strokeColor}\" stroke-width=\"1.5\"/>");

                double textX = node.X + nw / 2;
                double textY = node.Y + nh / 2;

                int fontSize = 12;
                if (node.Level == 0) fontSize = 16;
                else if (node.Level == 1) fontSize = 14;

                string escapedText = System.Security.SecurityElement.Escape(node.Text ?? "");

                sb.AppendLine($"  <text x=\"{textX}\" y=\"{textY}\" font-family=\"Inter, sans-serif\" font-size=\"{fontSize}px\" font-weight=\"bold\" fill=\"{textColor}\" text-anchor=\"middle\" dominant-baseline=\"central\">{escapedText}</text>");
            }

            sb.AppendLine("</svg>");

            File.WriteAllText(fileName, sb.ToString(), System.Text.Encoding.UTF8);
        }

        // -----------------------------------------------------------
        //  TEMPLATES - Mẫu có sẵn cho GV (Legacy 6 nút nhanh)
        // -----------------------------------------------------------

        private void Template_Literature(object sender, RoutedEventArgs e) =>
            LoadTemplateSimple("Phân tích tác phẩm", new[]
            { "Tác giả & Hoàn cảnh", "Nội dung chính", "Nghệ thuật", "Ý nghĩa & Thông điệp" });

        private void Template_STEM(object sender, RoutedEventArgs e) =>
            LoadTemplateSimple("Bài học STEM", new[]
            { "Khoa học (S)", "Công nghệ (T)", "Kỹ thuật (E)", "Toán học (M)", "Ứng dụng thực tế" });

        private void Template_Project(object sender, RoutedEventArgs e) =>
            LoadTemplateSimple("Dự án học tập", new[]
            { "Mục tiêu", "Thành viên", "Kế hoạch", "Tài liệu", "Đánh giá" });

        private void Template_History(object sender, RoutedEventArgs e) =>
            LoadTemplateSimple("Sự kiện lịch sử", new[]
            { "Hoàn cảnh", "Nguyên nhân", "Diễn biến", "Kết quả", "Ý nghĩa" });

        private void Template_Math(object sender, RoutedEventArgs e) =>
            LoadTemplateSimple("Toán học", new[]
            { "Đại số", "Hình học", "Lượng giác", "Giải tích", "Thống kê" });

        private void Template_Weekly(object sender, RoutedEventArgs e) =>
            LoadTemplateSimple("Kế hoạch tuần", new[]
            { "Thứ 2 - Thứ 3", "Thứ 4 - Thứ 5", "Thứ 6 - Thứ 7", "Mục tiêu", "Ghi chú" });

        /// <summary>Legacy: Load mẫu 1 cấp (giữ lại tương thích ngược).</summary>
        private void LoadTemplateSimple(string centerText, string[] branches)
        {
            PushState();
            mindmapCanvas.Children.Clear();
            _allNodes.Clear();
            _selectedNode = null;

            double cx = mindmapCanvas.ActualWidth / 2 - 60;
            double cy = mindmapCanvas.ActualHeight / 2 - 20;
            var root = CreateNode(centerText, cx, cy, null);

            double angleStep = 2 * System.Math.PI / branches.Length;
            for (int i = 0; i < branches.Length; i++)
            {
                double angle = -System.Math.PI / 2 + i * angleStep;
                double bx = cx + 200 * System.Math.Cos(angle);
                double by = cy + 150 * System.Math.Sin(angle);
                CreateNode(branches[i], bx, by, root);
            }

            SelectNode(root);
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
            { foreach (var n in _allNodes) UpdateConnectors(n); });
        }

        // -----------------------------------------------------------
        //  TEMPLATE GALLERY - Thư viện 20 mẫu đa cấp
        // -----------------------------------------------------------

        /// <summary>Mở popup danh sách 20 mẫu để người dùng chọn.</summary>
        private void OpenTemplateGallery_Click(object sender, RoutedEventArgs e)
        {
            var templates = MindmapTemplates.GetAll();
            var win = new Window
            {
                Title = "📂 Thư viện Mẫu Bản đồ Tư duy (20 mẫu)",
                Width = 680, Height = 520,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI")
            };
            try { win.Owner = Window.GetWindow(this); } catch { }

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Header
            var header = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                Padding = new Thickness(16, 12, 16, 12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            var headerText = new TextBlock
            {
                Text = "Chọn một mẫu bên dưới để tạo nhanh sơ đồ tư duy. Bạn có thể tự do chỉnh sửa nội dung sau khi nạp.",
                FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)),
                TextWrapping = TextWrapping.Wrap
            };
            header.Child = headerText;
            Grid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            // Template list
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(12) };
            var wrapPanel = new WrapPanel { Orientation = Orientation.Horizontal };

            string lastCat = "";
            foreach (var t in templates)
            {
                // Category separator
                if (t.Category != lastCat)
                {
                    lastCat = t.Category;
                    var catLabel = new TextBlock
                    {
                        Text = t.Category.ToUpper(),
                        FontSize = 12, FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                        Margin = new Thickness(8, 12, 600, 4),
                        Width = 640
                    };
                    wrapPanel.Children.Add(catLabel);
                }

                var card = new Border
                {
                    Width = 195, Height = 70, Margin = new Thickness(6),
                    CornerRadius = new CornerRadius(8),
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10, 8, 10, 8),
                    Cursor = Cursors.Hand,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    { BlurRadius = 4, ShadowDepth = 1, Opacity = 0.08 }
                };

                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = $"{t.Icon} {t.Name}",
                    FontSize = 13, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                int nodeCount = CountNodes(t.Root);
                sp.Children.Add(new TextBlock
                {
                    Text = $"{nodeCount} nhánh • {t.Category}",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                    Margin = new Thickness(0, 3, 0, 0)
                });
                card.Child = sp;

                var captured = t;
                card.MouseLeftButtonDown += (_, _) =>
                {
                    LoadTemplateMultiLevel(captured);
                    win.Close();
                };
                card.MouseEnter += (_, _) =>
                    card.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                card.MouseLeave += (_, _) =>
                    card.Background = Brushes.White;

                wrapPanel.Children.Add(card);
            }

            scroll.Content = wrapPanel;
            Grid.SetRow(scroll, 1);
            mainGrid.Children.Add(scroll);

            win.Content = mainGrid;
            win.ShowDialog();
        }

        /// <summary>Load mẫu đa cấp từ MindmapTemplates (Deep Copy).</summary>
        private void LoadTemplateMultiLevel(MindmapTemplates.Template template)
        {
            PushState();
            mindmapCanvas.Children.Clear();
            _allNodes.Clear();
            _selectedNode = null;

            double cx = mindmapCanvas.ActualWidth / 2 - 60;
            double cy = mindmapCanvas.ActualHeight / 2 - 20;

            // Tạo root
            var root = CreateNode(template.Root.Text, cx, cy, null);

            // Tạo nhánh cấp 1 + đệ quy con cháu
            double angleStep = 2 * System.Math.PI / System.Math.Max(template.Root.Children.Count, 1);
            for (int i = 0; i < template.Root.Children.Count; i++)
            {
                double angle = -System.Math.PI / 2 + i * angleStep;
                double bx = cx + 200 * System.Math.Cos(angle);
                double by = cy + 150 * System.Math.Sin(angle);

                var branch = CreateNode(template.Root.Children[i].Text, bx, by, root);
                BuildChildrenRecursive(template.Root.Children[i], branch, bx, by, angle, 120);
            }

            SelectNode(root);
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
            { foreach (var n in _allNodes) UpdateConnectors(n); });
        }

        private void BuildChildrenRecursive(MindmapTemplates.TemplateNode templateNode, MindNode parentNode,
            double px, double py, double parentAngle, double dist)
        {
            int count = templateNode.Children.Count;
            if (count == 0) return;

            double spread = System.Math.PI / 3;
            double startAngle = parentAngle - spread / 2;
            double step = count > 1 ? spread / (count - 1) : 0;

            for (int i = 0; i < count; i++)
            {
                double angle = count == 1 ? parentAngle : startAngle + i * step;
                double nx = px + dist * System.Math.Cos(angle);
                double ny = py + dist * System.Math.Sin(angle);

                var child = CreateNode(templateNode.Children[i].Text, nx, ny, parentNode);
                BuildChildrenRecursive(templateNode.Children[i], child, nx, ny, angle, dist * 0.85);
            }
        }

        private static int CountNodes(MindmapTemplates.TemplateNode node)
        {
            int count = 1;
            foreach (var c in node.Children) count += CountNodes(c);
            return count;
        }

        // ===========================================================
        //  PERSISTENCE & SERIALIZATION (SQLite & JSON Schema)
        // ===========================================================

        private class MindmapNodeDto
        {
            public int Id { get; set; }
            public string Text { get; set; } = "";
            public double X { get; set; }
            public double Y { get; set; }
            public string ColorHex { get; set; } = "";
            public int? ParentId { get; set; }
        }

        private class MindmapDataDto
        {
            public string Title { get; set; } = "";
            public string Layout { get; set; } = "Radial";
            public string LineStyle { get; set; } = "SolidNormal";
            public List<MindmapNodeDto> Nodes { get; set; } = new();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_allNodes.Count == 0)
            {
                MessageBox.Show("Sơ đồ trống. Hãy tạo nội dung trước khi lưu.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var root = _allNodes.FirstOrDefault(n => n.Parent == null);
            string defaultTitle = root != null ? root.Text : "Chủ đề chính";
            string? title = PromptForTitle(defaultTitle);
            if (string.IsNullOrEmpty(title)) return;

            try
            {
                var nodeToId = new Dictionary<MindNode, int>();
                for (int i = 0; i < _allNodes.Count; i++)
                {
                    nodeToId[_allNodes[i]] = i + 1;
                }

                var nodesDto = _allNodes.Select(node => new MindmapNodeDto
                {
                    Id = nodeToId[node],
                    Text = node.Text,
                    X = node.X,
                    Y = node.Y,
                    ColorHex = $"#{node.NodeColor.A:X2}{node.NodeColor.R:X2}{node.NodeColor.G:X2}{node.NodeColor.B:X2}",
                    ParentId = node.Parent != null ? nodeToId[node.Parent] : null
                }).ToList();

                var data = new MindmapDataDto
                {
                    Title = title,
                    Layout = _currentLayout.ToString(),
                    LineStyle = _currentLineStyle.ToString(),
                    Nodes = nodesDto
                };

                string json = System.Text.Json.JsonSerializer.Serialize(data);

                using (var db = new AppDbContext())
                {
                    var existing = db.StudentMindmaps.FirstOrDefault(m => m.Title == title);
                    if (existing != null)
                    {
                        var overwrite = MessageBox.Show($"Sơ đồ '{title}' đã tồn tại. Bạn có muốn ghi đè?", "Xác nhận ghi đè", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (overwrite == MessageBoxResult.No)
                            return;

                        existing.DataJson = json;
                        existing.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        db.Update(existing);
                    }
                    else
                    {
                        var newMap = new StudentMindmap
                        {
                            Title = title,
                            DataJson = json,
                            UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        db.StudentMindmaps.Add(newMap);
                    }
                    db.SaveChanges();
                }

                MessageBox.Show("Đã lưu sơ đồ tư duy thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu sơ đồ: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string? PromptForTitle(string defaultTitle)
        {
            var win = new Window
            {
                Title = "Lưu Sơ đồ tư duy",
                Width = 400,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI")
            };
            try { win.Owner = Window.GetWindow(this); } catch { }

            var grid = new Grid { Margin = new Thickness(16) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock { Text = "Nhập tên sơ đồ tư duy của bạn:", Margin = new Thickness(0, 0, 0, 8), FontSize = 13, FontWeight = FontWeights.SemiBold };
            Grid.SetRow(lbl, 0);
            grid.Children.Add(lbl);

            var txt = new TextBox { Text = defaultTitle, Height = 28, VerticalContentAlignment = VerticalAlignment.Center, FontSize = 13, Padding = new Thickness(4, 0, 4, 0) };
            Grid.SetRow(txt, 1);
            grid.Children.Add(txt);
            QASmartClass.LearningTools.Controls.TouchTextPad.Attach(txt, mode: "text");

            var bp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            var btnOk = new Button { Content = "Lưu", Width = 80, Height = 28, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
            var btnCancel = new Button { Content = "Hủy", Width = 80, Height = 28, IsCancel = true };
            bp.Children.Add(btnOk);
            bp.Children.Add(btnCancel);
            Grid.SetRow(bp, 2);
            grid.Children.Add(bp);

            win.Content = grid;

            string? result = null;
            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt.Text))
                {
                    MessageBox.Show("Tên sơ đồ không được để trống.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                result = txt.Text.Trim();
                win.DialogResult = true;
                win.Close();
            };

            if (win.ShowDialog() == true)
            {
                return result;
            }
            return null;
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            List<StudentMindmap> savedMaps;
            try
            {
                using (var db = new AppDbContext())
                {
                    savedMaps = db.StudentMindmaps.OrderByDescending(m => m.UpdatedAt).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi truy cập cơ sở dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (savedMaps.Count == 0)
            {
                MessageBox.Show("Không tìm thấy sơ đồ nào đã lưu trong cơ sở dữ liệu.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new Window
            {
                Title = "📂 Mở Sơ đồ tư duy đã lưu",
                Width = 500, Height = 450,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI")
            };
            try { win.Owner = Window.GetWindow(this); } catch { }

            var grid = new Grid { Margin = new Thickness(16) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Label
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Search Box [NEW]
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 2: ListBox
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Buttons

            var lbl = new TextBlock { Text = "Chọn sơ đồ tư duy để nạp:", Margin = new Thickness(0, 0, 0, 8), FontSize = 13, FontWeight = FontWeights.SemiBold };
            Grid.SetRow(lbl, 0);
            grid.Children.Add(lbl);

            // TextBox lọc tìm kiếm [NEW]
            var txtFilter = new TextBox 
            { 
                Margin = new Thickness(0, 0, 0, 8), 
                Padding = new Thickness(6, 4, 6, 4), 
                FontSize = 12,
                ToolTip = "Nhập tên sơ đồ cần tìm..."
            };
            TouchTextPad.Attach(txtFilter, "text");
            Grid.SetRow(txtFilter, 1);
            grid.Children.Add(txtFilter);

            var listBox = new ListBox { FontSize = 13, Margin = new Thickness(0, 0, 0, 16) };
            
            // Hàm tải danh sách sơ đồ theo lọc
            Action refreshList = () =>
            {
                listBox.Items.Clear();
                string query = txtFilter.Text.Trim().ToLower();
                foreach (var map in savedMaps)
                {
                    if (string.IsNullOrEmpty(query) || map.Title.ToLower().Contains(query))
                    {
                        listBox.Items.Add(new ListBoxItem
                        {
                            Content = $"{map.Title} (Cập nhật: {map.UpdatedAt})",
                            Tag = map
                        });
                    }
                }
            };

            txtFilter.TextChanged += (s, ev) => refreshList();
            refreshList(); // Nạp lần đầu

            Grid.SetRow(listBox, 2);
            grid.Children.Add(listBox);

            var bp = new Grid();
            bp.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bp.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bp.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var btnDelete = new Button { Content = "🗑️ Xóa bản lưu", Width = 110, Height = 30, Foreground = Brushes.Red };
            Grid.SetColumn(btnDelete, 0);
            bp.Children.Add(btnDelete);

            var rightBp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnOpen = new Button { Content = "Mở", Width = 80, Height = 30, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
            var btnCancel = new Button { Content = "Hủy", Width = 80, Height = 30, IsCancel = true };
            rightBp.Children.Add(btnOpen);
            rightBp.Children.Add(btnCancel);
            Grid.SetColumn(rightBp, 2);
            bp.Children.Add(rightBp);

            Grid.SetRow(bp, 3);
            grid.Children.Add(bp);

            win.Content = grid;

            StudentMindmap? selectedMap = null;

            btnOpen.Click += (s, ev) =>
            {
                if (listBox.SelectedItem is ListBoxItem item && item.Tag is StudentMindmap map)
                {
                    selectedMap = map;
                    win.DialogResult = true;
                    win.Close();
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn một sơ đồ để mở.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            btnDelete.Click += (s, ev) =>
            {
                if (listBox.SelectedItem is ListBoxItem item && item.Tag is StudentMindmap map)
                {
                    var confirm = MessageBox.Show($"Bạn chắc chắn muốn xóa vĩnh viễn sơ đồ '{map.Title}'?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (confirm == MessageBoxResult.Yes)
                    {
                        try
                        {
                            using (var db = new AppDbContext())
                            {
                                var toDelete = db.StudentMindmaps.Find(map.Id);
                                if (toDelete != null)
                                {
                                    db.StudentMindmaps.Remove(toDelete);
                                    db.SaveChanges();
                                }
                            }
                            listBox.Items.Remove(item);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn một sơ đồ để xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            if (win.ShowDialog() == true && selectedMap != null)
            {
                LoadMindmapFromJson(selectedMap.DataJson);
                _undoStack.Clear();
                _redoStack.Clear();
                UpdateUndoRedoButtonsState();
            }
        }

        private void LoadMindmapFromJson(string json)
        {
            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<MindmapDataDto>(json);
                if (data == null || data.Nodes == null) return;

                mindmapCanvas.Children.Clear();
                _allNodes.Clear();
                _selectedNode = null;
                btnAddChild.IsEnabled = false;
                btnDeleteNode.IsEnabled = false;

                if (Enum.TryParse<LayoutType>(data.Layout, out var ltype))
                    _currentLayout = ltype;
                if (Enum.TryParse<LineStyleType>(data.LineStyle, out var stype))
                    _currentLineStyle = stype;

                if (data.Nodes.Count == 0)
                {
                    UpdateEmptyState();
                    return;
                }

                var idToNode = new Dictionary<int, MindNode>();
                
                var rootDto = data.Nodes.FirstOrDefault(n => n.ParentId == null) ?? data.Nodes.FirstOrDefault();
                if (rootDto == null)
                {
                    UpdateEmptyState();
                    return;
                }

                Color rootColor = (Color)System.Windows.Media.ColorConverter.ConvertFromString(rootDto.ColorHex);
                var root = CreateNode(rootDto.Text, rootDto.X, rootDto.Y, null, rootColor);
                idToNode[rootDto.Id] = root;

                var remaining = data.Nodes.Where(n => n.Id != rootDto.Id).ToList();
                int lastCount = -1;
                while (remaining.Count > 0 && remaining.Count != lastCount)
                {
                    lastCount = remaining.Count;
                    for (int i = remaining.Count - 1; i >= 0; i--)
                    {
                        var dto = remaining[i];
                        if (dto.ParentId.HasValue && idToNode.ContainsKey(dto.ParentId.Value))
                        {
                            var parent = idToNode[dto.ParentId.Value];
                            Color col = (Color)System.Windows.Media.ColorConverter.ConvertFromString(dto.ColorHex);
                            var child = CreateNode(dto.Text, dto.X, dto.Y, parent, col);
                            idToNode[dto.Id] = child;
                            remaining.RemoveAt(i);
                        }
                        else if (!dto.ParentId.HasValue)
                        {
                            Color col = (Color)System.Windows.Media.ColorConverter.ConvertFromString(dto.ColorHex);
                            var child = CreateNode(dto.Text, dto.X, dto.Y, root, col);
                            idToNode[dto.Id] = child;
                            remaining.RemoveAt(i);
                        }
                    }
                }

                SelectNode(root);
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
                {
                    foreach (var n in _allNodes) UpdateConnectors(n);
                    UpdateCanvasSize();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nạp sơ đồ: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ===========================================================
        //  UNDO / REDO ENGINE
        // ===========================================================

        private void PushState()
        {
            if (_isApplyingState) return;
            string state = GetCurrentStateJson();
            _undoStack.Push(state);
            if (_undoStack.Count > 50)
            {
                var list = _undoStack.ToList();
                list.RemoveAt(list.Count - 1);
                _undoStack.Clear();
                for (int i = list.Count - 1; i >= 0; i--)
                    _undoStack.Push(list[i]);
            }
            _redoStack.Clear();
            TriggerAutoSave();
            UpdateUndoRedoButtonsState();
        }

        private string GetCurrentStateJson()
        {
            var nodeToId = new Dictionary<MindNode, int>();
            for (int i = 0; i < _allNodes.Count; i++)
            {
                nodeToId[_allNodes[i]] = i + 1;
            }

            var nodesDto = _allNodes.Select(node => new MindmapNodeDto
            {
                Id = nodeToId[node],
                Text = node.Text,
                X = node.X,
                Y = node.Y,
                ColorHex = $"#{node.NodeColor.A:X2}{node.NodeColor.R:X2}{node.NodeColor.G:X2}{node.NodeColor.B:X2}",
                ParentId = node.Parent != null ? nodeToId[node.Parent] : null
            }).ToList();

            var data = new MindmapDataDto
            {
                Title = "",
                Layout = _currentLayout.ToString(),
                LineStyle = _currentLineStyle.ToString(),
                Nodes = nodesDto
            };

            return System.Text.Json.JsonSerializer.Serialize(data);
        }

        private void Undo()
        {
            if (_undoStack.Count == 0) return;

            string currentState = GetCurrentStateJson();
            _redoStack.Push(currentState);

            string prevState = _undoStack.Pop();
            ApplyStateJson(prevState);
            UpdateUndoRedoButtonsState();
        }

        private void Redo()
        {
            if (_redoStack.Count == 0) return;

            string currentState = GetCurrentStateJson();
            _undoStack.Push(currentState);

            string nextState = _redoStack.Pop();
            ApplyStateJson(nextState);
            UpdateUndoRedoButtonsState();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            Redo();
        }

        private void UpdateUndoRedoButtonsState()
        {
            if (btnUndo != null)
            {
                btnUndo.IsEnabled = _undoStack.Count > 0;
            }
            if (btnRedo != null)
            {
                btnRedo.IsEnabled = _redoStack.Count > 0;
            }
        }

        private void ApplyStateJson(string json)
        {
            _isApplyingState = true;
            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<MindmapDataDto>(json);
                if (data == null) return;

                mindmapCanvas.Children.Clear();
                _allNodes.Clear();
                _selectedNode = null;
                btnAddChild.IsEnabled = false;
                btnDeleteNode.IsEnabled = false;

                if (Enum.TryParse<LayoutType>(data.Layout, out var ltype))
                    _currentLayout = ltype;
                if (Enum.TryParse<LineStyleType>(data.LineStyle, out var stype))
                    _currentLineStyle = stype;

                if (data.Nodes == null || data.Nodes.Count == 0)
                {
                    UpdateEmptyState();
                    return;
                }

                var idToNode = new Dictionary<int, MindNode>();
                var rootDto = data.Nodes.FirstOrDefault(n => n.ParentId == null) ?? data.Nodes.FirstOrDefault();
                if (rootDto == null)
                {
                    UpdateEmptyState();
                    return;
                }

                Color rootColor = (Color)System.Windows.Media.ColorConverter.ConvertFromString(rootDto.ColorHex);
                var root = CreateNode(rootDto.Text, rootDto.X, rootDto.Y, null, rootColor);
                idToNode[rootDto.Id] = root;

                var remaining = data.Nodes.Where(n => n.Id != rootDto.Id).ToList();
                int lastCount = -1;
                while (remaining.Count > 0 && remaining.Count != lastCount)
                {
                    lastCount = remaining.Count;
                    for (int i = remaining.Count - 1; i >= 0; i--)
                    {
                        var dto = remaining[i];
                        if (dto.ParentId.HasValue && idToNode.ContainsKey(dto.ParentId.Value))
                        {
                            var parent = idToNode[dto.ParentId.Value];
                            Color col = (Color)System.Windows.Media.ColorConverter.ConvertFromString(dto.ColorHex);
                            var child = CreateNode(dto.Text, dto.X, dto.Y, parent, col);
                            idToNode[dto.Id] = child;
                            remaining.RemoveAt(i);
                        }
                        else if (!dto.ParentId.HasValue)
                        {
                            Color col = (Color)System.Windows.Media.ColorConverter.ConvertFromString(dto.ColorHex);
                            var child = CreateNode(dto.Text, dto.X, dto.Y, root, col);
                            idToNode[dto.Id] = child;
                            remaining.RemoveAt(i);
                        }
                    }
                }

                SelectNode(root);
                
                foreach (var n in _allNodes) UpdateConnectors(n);
                UpdateCanvasSize();
            }
            finally
            {
                _isApplyingState = false;
            }
        }

        // ===========================================================
        //  AUTO-SAVE DRAFT & EMOJI PANEL & TOUCH MANIPULATION
        // ===========================================================

        private System.Windows.Threading.DispatcherTimer? _autoSaveTimer;

        private void InitializeAutoSave()
        {
            _autoSaveTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
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

        private void SaveDraft()
        {
            try
            {
                if (_allNodes.Count == 0) return;
                string dir = global::System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QASmartClass", "Drafts");
                if (!global::System.IO.Directory.Exists(dir))
                {
                    global::System.IO.Directory.CreateDirectory(dir);
                }
                string file = global::System.IO.Path.Combine(dir, "mindmap_draft.json");
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
                string file = global::System.IO.Path.Combine(dir, "mindmap_draft.json");
                if (global::System.IO.File.Exists(file))
                {
                    var result = MessageBox.Show(
                        "Hệ thống phát hiện bản nháp sơ đồ tư duy chưa được lưu chính thức. Bạn có muốn khôi phục không?",
                        "Khôi phục bản nháp",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        string json = global::System.IO.File.ReadAllText(file, System.Text.Encoding.UTF8);
                        LoadMindmapFromJson(json);
                        _undoStack.Clear();
                        _redoStack.Clear();
                        UpdateUndoRedoButtonsState();
                    }
                    global::System.IO.File.Delete(file);
                }
            }
            catch { }
        }

        // -----------------------------------------------------------
        //  EMOJI PANEL SEARCH & FILTER (PHASE 5 MICRO-OPTIMIZATIONS)
        // -----------------------------------------------------------

        private struct EmojiItem
        {
            public string Character { get; set; }
            public string Name { get; set; }
            public string Category { get; set; }
        }

        private static readonly List<EmojiItem> EmojisList = new()
        {
            new EmojiItem { Character = "📖", Name = "sach vo hoc tap doc doc book", Category = "Study" },
            new EmojiItem { Character = "🔬", Name = "kinh hien vi khoa hoc stem nhom microscope", Category = "Study" },
            new EmojiItem { Character = "🧪", Name = "hoa chat ong nghiem stem thi nghiem chemistry", Category = "Study" },
            new EmojiItem { Character = "🎨", Name = "my thuat ve co tranh art paint", Category = "Study" },
            new EmojiItem { Character = "📝", Name = "ghi chep viet vo but note write", Category = "Study" },
            new EmojiItem { Character = "💡", Name = "y tuong den sang tao bong den idea light", Category = "Study" },
            new EmojiItem { Character = "🍎", Name = "tao hoc tap qua trai cay apple fruit", Category = "Study" },
            new EmojiItem { Character = "🏫", Name = "truong hoc lop hoc truong school class", Category = "Study" },
            new EmojiItem { Character = "💻", Name = "may tinh cong nghe laptop computer technology", Category = "Study" },
            new EmojiItem { Character = "🚀", Name = "ten lua tien bo phong nhanh rocket fly", Category = "Study" },

            new EmojiItem { Character = "⭐", Name = "ngoi sao diem khen xuat sac star gold", Category = "Award" },
            new EmojiItem { Character = "🏆", Name = "cup giai thuong chien thang vo dich trophy win", Category = "Award" },
            new EmojiItem { Character = "👍", Name = "thich tot dong y tuyet voi like ok good", Category = "Award" },
            new EmojiItem { Character = "🎯", Name = "muc tieu dich ban trung target goal", Category = "Award" },
            new EmojiItem { Character = "🍀", Name = "co 4 la may man tot lanh clover luck", Category = "Award" },
            new EmojiItem { Character = "🔥", Name = "lua chuyen gia tuyet voi nong chay fire hot", Category = "Award" },

            new EmojiItem { Character = "😊", Name = "vui cuoi hanh phuc than thien smile happy", Category = "Face" },
            new EmojiItem { Character = "🤔", Name = "suy nghi dan do tu hoi nghi ngo think question", Category = "Face" },
            new EmojiItem { Character = "😮", Name = "ngac nghien bat ngo ha hoc wow surprise", Category = "Face" },
            new EmojiItem { Character = "🥳", Name = "an mung tiec chung mung party celebrate", Category = "Face" }
        };

        private string _currentEmojiTab = "All";

        private void InitializeEmojiPanel()
        {
            if (txtEmojiSearch != null)
            {
                txtEmojiSearch.TextChanged += TxtEmojiSearch_TextChanged;
                TouchTextPad.Attach(txtEmojiSearch, "text");
            }
            PopulateEmojis("All", "");
        }

        private void PopulateEmojis(string category, string searchStr)
        {
            if (wpEmojis == null) return;
            wpEmojis.Children.Clear();

            string query = searchStr.Trim().ToLower();

            foreach (var item in EmojisList)
            {
                if (category != "All" && !item.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(query) && !item.Name.ToLower().Contains(query) && !item.Character.Contains(query))
                    continue;

                var btn = new Button
                {
                    Content = item.Character,
                    FontSize = 18,
                    Width = 32,
                    Height = 32,
                    Margin = new Thickness(2),
                    Cursor = Cursors.Hand,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    ToolTip = item.Name.Split(' ')[0]
                };

                btn.MouseEnter += (s, e) => btn.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                btn.MouseLeave += (s, e) => btn.Background = Brushes.Transparent;

                btn.Click += (s, e) =>
                {
                    AddEmojiToSelectedNode(item.Character);
                    if (btnEmojiToggle != null) btnEmojiToggle.IsChecked = false;
                };

                wpEmojis.Children.Add(btn);
            }
        }

        private void TxtEmojiSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            PopulateEmojis(_currentEmojiTab, txtEmojiSearch.Text);
        }

        private void EmojiTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tab)
            {
                _currentEmojiTab = tab;
                
                if (btnTabAll != null) { btnTabAll.Background = Brushes.Transparent; btnTabAll.BorderThickness = new Thickness(1); btnTabAll.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)); }
                if (btnTabStudy != null) { btnTabStudy.Background = Brushes.Transparent; btnTabStudy.BorderThickness = new Thickness(1); btnTabStudy.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)); }
                if (btnTabAward != null) { btnTabAward.Background = Brushes.Transparent; btnTabAward.BorderThickness = new Thickness(1); btnTabAward.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)); }
                if (btnTabFace != null) { btnTabFace.Background = Brushes.Transparent; btnTabFace.BorderThickness = new Thickness(1); btnTabFace.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)); }

                btn.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                btn.BorderBrush = new SolidColorBrush(Color.FromRgb(144, 202, 249));

                PopulateEmojis(_currentEmojiTab, txtEmojiSearch != null ? txtEmojiSearch.Text : "");
            }
        }

        private void AddEmojiToSelectedNode(string emoji)
        {
            if (_selectedNode == null)
            {
                MessageBox.Show("Vui lòng chọn một nhánh để chèn biểu tượng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_selectedNode.Visual is Border border && border.Child is TextBlock tb)
            {
                string currentText = _selectedNode.Text ?? "";
                if (currentText.StartsWith(emoji)) return;

                PushState();
                _selectedNode.Text = emoji + " " + currentText.TrimStart();
                tb.Text = _selectedNode.Text;
                TriggerAutoSave();
            }
        }

        private void Emoji_Click(object sender, RoutedEventArgs e)
        {
            // Keeping for backward compatibility signature
            if (sender is Button btn && btn.Content is string emoji)
            {
                AddEmojiToSelectedNode(emoji);
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
                e.Handled = true;
            }
        }

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new System.Collections.Generic.List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "📋",
                    Title = isVN ? "Lập Kế Hoạch & Quản Lý Dự Án" : "Project Planning & Management",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_mindmap_1_{suffix}.png",
                    Description = isVN 
                        ? "Các nhóm dự án và học sinh sử dụng sơ đồ tư duy để lập cấu trúc phân rã công việc (WBS), phân bổ vai trò thành viên, xác định các mốc thời gian (milestones) và kết nối các nhiệm vụ con một cách trực quan." 
                        : "Project teams and students use mind maps to build work breakdown structures (WBS), assign member roles, define milestones, and visually connect sub-tasks."
                },
                new PracticalAppItem
                {
                    Icon = "📝",
                    Title = isVN ? "Động Não Sáng Tạo & Lập Dàn Ý Viết Lách" : "Creative Brainstorming & Outlining",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_mindmap_2_{suffix}.png",
                    Description = isVN 
                        ? "Các nhà văn, nhà biên kịch và người sáng tạo nội dung phác thảo sơ đồ tư duy để kết nối các tuyến nhân vật, xây dựng tiến trình cốt truyện, phát triển chủ đề phụ và sắp xếp các lập luận trước khi bắt tay viết chi tiết." 
                        : "Writers, screenwriters, and content creators draft mind maps to link character arcs, outline plots, develop sub-themes, and organize arguments before detailed writing."
                },
                new PracticalAppItem
                {
                    Icon = "🎓",
                    Title = isVN ? "Hệ Thống Kiến Thức & Ôn Thi" : "Structured Study & Exam Prep",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_mindmap_3_{suffix}.png",
                    Description = isVN 
                        ? "Học sinh tóm tắt toàn bộ chương học bằng cách vẽ sơ đồ tư duy: từ khóa trung tâm nối với các công thức, định nghĩa và ví dụ cốt lõi. Giúp ghi nhớ sâu bằng bán cầu não trái (logic) và bán cầu não phải (màu sắc, hình ảnh)." 
                        : "Students summarize entire chapters by drawing mind maps: connecting the central keyword with core formulas, definitions, and examples. Aids memory retention by engaging both left (logic) and right (color, visual) brain hemispheres."
                }
            };

            try
            {
                if (practicalAppViewer != null) practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for MindmapTool: {Err}", ex.Message);
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