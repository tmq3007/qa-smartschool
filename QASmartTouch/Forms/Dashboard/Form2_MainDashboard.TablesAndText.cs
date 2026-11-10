using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using QASmartTouch.Handlers;
using QASmartTouch.Managers;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using System.Windows.Input;
using System.Windows.Ink;
using System.Collections.Generic;
using System.Globalization;
using System.Speech.Synthesis;
using QASmartTouch.Services;
using QASmartTouch.Helpers;
using QASmartTouch.Models;
using QASmartTouch.Controls;
using QASmartTouch.Controllers;
using QASmartTouch.Shared;
using QASmartTouch.Properties;
using System.Windows.Documents;
using QASmartTouch.Services.Tools;
using QASmartTouch.WhiteboardCore.History;
// ══════════════════════════════════════════════════════════════════
// Form2_MainDashboard.TablesAndText.cs — Partial Class
// Quản lý Bảng biểu (Table Creation/Resizer/Cell Editing), Hộp văn bản
// (RichTextBox), Đồ thị và cơ chế đính kèm nét vẽ vào ô Bảng.
// ══════════════════════════════════════════════════════════════════

namespace QASmartTouch.Forms
{
    public partial class Form2_MainDashboard
    {
        #region Tables, TextBoxes, Graphs & Container Attachment

        private void EnableTextTool()
        {
            // Enable text tool mode
            _textToolEnabled = true;
            _drawingEnabled = false; // Disable pen drawing
            
            // Disable eraser with proper cleanup
            if (_eraserEnabled)
                DisableEraserMode();
            
            _shapeDrawingEnabled = false; // Disable shape drawing
            _selectionToolEnabled = false; // Disable selection
            
            // Set TouchHandler to None tool mode to prevent drawing freehand lines during text input
            if (_touchHandler != null)
            {
                _touchHandler.SetToolMode(QASmartTouch.Handlers.TouchToolMode.None);
            }
            
            // Change cursor to IBeam for text input
            MainInteractiveBoard.Cursor = Cursors.IBeam;
        }

        private void EnableSelectionTool()
        {
            // Enable selection tool mode
            _selectionToolEnabled = true;
            _drawingEnabled = false; // Disable pen drawing
            
            // Disable eraser with proper cleanup
            if (_eraserEnabled)
                DisableEraserMode();
            
            _shapeDrawingEnabled = false; // Disable shape drawing
            _textToolEnabled = false; // Disable text tool
            
            // Set TouchHandler to None tool mode to prevent drawing freehand lines during object selection
            if (_touchHandler != null)
            {
                _touchHandler.SetToolMode(QASmartTouch.Handlers.TouchToolMode.None);
            }
            
            // Change cursor to arrow for selection
            MainInteractiveBoard.Cursor = Cursors.Arrow;
        }

        private void SelectElementAt(Point clickPoint)
        {
            System.Diagnostics.Debug.WriteLine($"🔍 SelectElementAt called at ({clickPoint.X:F0}, {clickPoint.Y:F0})");

            // ✅ QC_4.2_SELECTION_BOUNDS_FIX: Làm tươi danh sách đối tượng & bounds trước khi HitTest
            RefreshSelectableObjects();

            // Clear previous selection
            ClearSelection();

            // Hit test to find element at click point
            _selectedElement = null;

            // ────────────────────────────────────────────────────────────────
            // PATH 1: Ưu tiên SelectionManager HitTest (đã có tolerance 15px)
            // ────────────────────────────────────────────────────────────────
            if (_selectionManager != null)
            {
                var selectableObj = _selectionManager.HitTest(clickPoint);
                if (selectableObj != null)
                {
                    _selectedElement = selectableObj.Element;
                    HighlightElement(selectableObj.Element);
                    _selectionManager.SelectObject(selectableObj); // → fire SelectionChanged → _selectionBox.AttachTo()
                    System.Diagnostics.Debug.WriteLine($"✅ Selection Tool: Selected {selectableObj.Type} via SelectionManager HitTest");
                    return;
                }
            }

            // ────────────────────────────────────────────────────────────────
            // PATH 2: Fallback — duyệt Canvas children từ trên xuống dưới
            // ────────────────────────────────────────────────────────────────
            for (int i = MainInteractiveBoard.Children.Count - 1; i >= 0; i--)
            {
                var element = MainInteractiveBoard.Children[i];

                // Skip system UI / preview controls
                if (element == panelWelcomeState || element == _selectionBox || element == _contextToolbar)
                    continue;

                // ✅ Bỏ qua element ẩn & sub-Canvas
                if (element.Visibility != Visibility.Visible) continue;
                if (element is Canvas) continue;

                // Check if click point hits this element
                if (IsPointInElement(element, clickPoint))
                {
                    _selectedElement = element;
                    HighlightElement(element);

                    System.Diagnostics.Debug.WriteLine($"   Element found via Fallback: {element.GetType().Name}");

                    // ✅ Tạo SelectableObject fallback từ BoundsHelper
                    var absBounds = QASmartTouch.Helpers.BoundsHelper.GetAbsoluteBounds(element, MainInteractiveBoard);
                    if (!absBounds.IsEmpty && absBounds.Width > 0 && absBounds.Height > 0)
                    {
                        // Xác định đúng ObjectType theo loại element thực tế
                        ObjectType objType;
                        if (element is Polyline) objType = ObjectType.Stroke;
                        else if (element is System.Windows.Shapes.Path) objType = ObjectType.Drawing;
                        else if (element is System.Windows.Controls.Image) objType = ObjectType.Image;
                        else if (element is System.Windows.Shapes.Line) objType = ObjectType.Shape;
                        else if (element is System.Windows.Controls.TextBlock || element is System.Windows.Controls.TextBox) objType = ObjectType.Text;
                        else if (element is System.Windows.Shapes.Shape) objType = ObjectType.Shape;
                        else objType = ObjectType.Other;

                        var selectableObj = new SelectableObject
                        {
                            Element = element,
                            Type = objType,
                            Bounds = absBounds,
                            Position = new Point(absBounds.Left, absBounds.Top),
                            Size = new Size(absBounds.Width, absBounds.Height),
                            ZIndex = Panel.GetZIndex(element)
                        };

                        if (_selectionManager != null)
                        {
                            _selectionManager.AddObject(selectableObj);
                            _selectionManager.SelectObject(selectableObj); // → fire SelectionChanged → _selectionBox.AttachTo()
                            System.Diagnostics.Debug.WriteLine($"✅ Selection Tool: Selected {objType} via Fallback + SelectObject");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Selection Tool: Element found but BoundsHelper returned empty/zero bounds");
                    }

                    break;
                }
            }

            if (_selectedElement == null)
            {
                System.Diagnostics.Debug.WriteLine($"   No element found at click point");
            }
        }

        private bool IsPointInElement(UIElement element, Point point)
        {
            // Get element bounds
            if (element is FrameworkElement frameworkElement)
            {
                double left = Canvas.GetLeft(element);
                double top = Canvas.GetTop(element);
                
                // Handle NaN values
                if (double.IsNaN(left)) left = 0;
                if (double.IsNaN(top)) top = 0;
                
                double right = left + frameworkElement.ActualWidth;
                double bottom = top + frameworkElement.ActualHeight;
                
                return point.X >= left && point.X <= right && point.Y >= top && point.Y <= bottom;
            }
            
            return false;
        }

        private void HighlightElement(UIElement element)
        {
            // Add visual indicator (e.g., border or background)
            if (element is FrameworkElement frameworkElement)
            {
                // Store original effect
                frameworkElement.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Blue,
                    BlurRadius = 10,
                    ShadowDepth = 0,
                    Opacity = 0.8
                };
            }
        }

        private void ClearSelection()
        {
            if (_selectedElement != null && _selectedElement is FrameworkElement frameworkElement)
            {
                // Remove highlight effect
                frameworkElement.Effect = null;
                _selectedElement = null;
            }
        }

        private void MoveElement(UIElement element, Point currentPoint)
        {
            // Calculate offset from start point
            double offsetX = currentPoint.X - _selectionStartPoint.X;
            double offsetY = currentPoint.Y - _selectionStartPoint.Y;
            
            // Get current position
            double currentLeft = Canvas.GetLeft(element);
            double currentTop = Canvas.GetTop(element);
            
            // Handle NaN values
            if (double.IsNaN(currentLeft)) currentLeft = 0;
            if (double.IsNaN(currentTop)) currentTop = 0;
            
            // Set new position
            Canvas.SetLeft(element, currentLeft + offsetX);
            Canvas.SetTop(element, currentTop + offsetY);
            
            // Update start point for next move
            _selectionStartPoint = currentPoint;
        }

        private void InsertTextBox(Point position)
        {
            // Create a TextBox for user input
            var textBox = new TextBox
            {
                Width = 200,
                MinHeight = 30,
                FontFamily = new System.Windows.Media.FontFamily(_currentFontFamily),
                FontSize = _currentFontSize,
                Foreground = new SolidColorBrush(_currentTextColor),
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Colors.Gray),
                BorderThickness = new Thickness(1),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                Padding = new Thickness(5)
            };

            // Position the TextBox at click location
            Canvas.SetLeft(textBox, position.X);
            Canvas.SetTop(textBox, position.Y);

            // Add to canvas
            MainInteractiveBoard.Children.Add(textBox);

            // Focus the TextBox for immediate typing (QC_4.2_TOUCH: Bọc Dispatcher để hoàn tất layout pass trên IFP)
            Dispatcher.BeginInvoke(new Action(() =>
            {
                textBox.Focus();
                ShowTouchKeyboard();
            }), System.Windows.Threading.DispatcherPriority.Input);

            // When user finishes editing (loses focus), add to undo stack
            textBox.LostFocus += (s, e) =>
            {
                // Only add to undo stack if text is not empty
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    // Convert TextBox to TextBlock for permanent display
                    var textBlock = new TextBlock
                    {
                        Text = textBox.Text,
                        Width = textBox.ActualWidth,
                        FontFamily = textBox.FontFamily,
                        FontSize = textBox.FontSize,
                        Foreground = textBox.Foreground,
                        Background = Brushes.Transparent,
                        TextWrapping = TextWrapping.Wrap,
                        Padding = new Thickness(5)
                    };

                    // Position at same location
                    Canvas.SetLeft(textBlock, Canvas.GetLeft(textBox));
                    Canvas.SetTop(textBlock, Canvas.GetTop(textBox));

                    // Replace TextBox with TextBlock
                    MainInteractiveBoard.Children.Remove(textBox);
                    MainInteractiveBoard.Children.Add(textBlock);

                    // ✅ QC_4.2_TEXTBOX_SELECTION_FIX: Đăng ký TextBlock mới với SelectionManager
                    // để giáo viên có thể Chọn, Di chuyển, Thay đổi kích thước/phông chữ ngay lập tức
                    RegisterNewObjectWithSelectionManager(textBlock);

                    // Add to undo stack
                    RecordAddAction(textBlock, "Draw");
                }
                else
                {
                    // Remove empty TextBox
                    MainInteractiveBoard.Children.Remove(textBox);
                }
            };
        }

                public void InsertGraphToCanvas(BitmapImage graphImage, object? graphConfig = null)
        {
            try
            {
                // Create Grid container
                var graphContainer = new Grid
                {
                    Width = 600,
                    Height = 450,
                    Cursor = Cursors.Hand
                };
                
                // Store config in Tag for later editing
                graphContainer.Tag = graphConfig;
                
                // Create Image control
                var image = new Image
                {
                    Source = graphImage,
                    Stretch = Stretch.Uniform,
                    Width = 600,
                    Height = 450
                };
                graphContainer.Children.Add(image);
                
                bool is3D = graphConfig is Graph3DConfiguration || 
                            (graphConfig is GraphConfiguration gc && gc.GraphType == "3D");

                // Create Top Drag Handle Bar
                var dragHandleBar = new Border
                {
                    Height = 34,
                    Background = new SolidColorBrush(Color.FromRgb(41, 128, 185)),
                    CornerRadius = new CornerRadius(6, 6, 0, 0),
                    Cursor = Cursors.SizeAll,
                    Tag = "DragHandle",
                    ToolTip = is3D ? "Nhấn giữ để di chuyển Đồ thị 3D" : "Nhấn giữ để di chuyển Đồ thị 2D"
                };
                Stylus.SetIsPressAndHoldEnabled(dragHandleBar, false);
                Stylus.SetIsFlicksEnabled(dragHandleBar, false);
                Stylus.SetIsTapFeedbackEnabled(dragHandleBar, false);
                Stylus.SetIsTouchFeedbackEnabled(dragHandleBar, false);

                var dragGrid = new Grid();
                var titleText = new TextBlock
                {
                    Text = is3D ? "🎲 Đồ thị 3D - Kéo để di chuyển" : "📈 Đồ thị 2D - Kéo để di chuyển",
                    Foreground = Brushes.White,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                dragGrid.Children.Add(titleText);

                // Create button panel (top-right corner)
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 4, 0)
                };
                
                // Edit button
                var btnEdit = new Button
                {
                    Content = "✏️",
                    Width = 26,
                    Height = 26,
                    Margin = new Thickness(2, 0, 2, 0),
                    ToolTip = "Sửa đồ thị",
                    Background = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
                    Foreground = Brushes.Black,
                    BorderBrush = Brushes.Transparent,
                    FontSize = 13,
                    Cursor = Cursors.Hand
                };
                btnEdit.Click += (s, e) => EditGraph(graphContainer);
                TouchActivationHelper.WireButton(btnEdit);
                buttonPanel.Children.Add(btnEdit);
                
                // Delete button
                var btnDelete = new Button
                {
                    Content = "❌",
                    Width = 26,
                    Height = 26,
                    Margin = new Thickness(2, 0, 2, 0),
                    ToolTip = "Xóa đồ thị",
                    Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                    Foreground = Brushes.White,
                    BorderBrush = Brushes.Transparent,
                    FontSize = 13,
                    Cursor = Cursors.Hand
                };
                TouchActivationHelper.WireButton(btnDelete);
                
                dragGrid.Children.Add(buttonPanel);
                dragHandleBar.Child = dragGrid;

                var finalGraphContainer = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Width = 600
                };
                finalGraphContainer.Children.Add(dragHandleBar);
                finalGraphContainer.Children.Add(graphContainer);

                btnDelete.Click += (s, e) => DeleteGraph(finalGraphContainer);
                buttonPanel.Children.Add(btnDelete);

                // Enable Drag-to-Move on dragHandleBar (Touch & Mouse strictly isolated)
                bool isMouseDragging = false;
                int? activeTouchId = null;
                Point dragStartPoint = new Point();

                dragHandleBar.MouseLeftButtonDown += (s, e) =>
                {
                    if (activeTouchId.HasValue || e.StylusDevice != null ||
                        QASmartTouch.Utilities.InputValidationHelper.IsEventFromButton(e.OriginalSource, dragHandleBar) ||
                        (e.OriginalSource is DependencyObject dep && IsDescendantOf(dep, buttonPanel)))
                        return;

                    _isDrawing = false;
                    _currentStroke = null;
                    isMouseDragging = true;
                    dragStartPoint = e.GetPosition(MainInteractiveBoard);
                    dragHandleBar.CaptureMouse();
                    e.Handled = true;
                };

                dragHandleBar.MouseMove += (s, e) =>
                {
                    if (isMouseDragging && !activeTouchId.HasValue && e.StylusDevice == null)
                    {
                        Point currentPoint = e.GetPosition(MainInteractiveBoard);
                        double left = Canvas.GetLeft(finalGraphContainer) + (currentPoint.X - dragStartPoint.X);
                        double top = Canvas.GetTop(finalGraphContainer) + (currentPoint.Y - dragStartPoint.Y);

                        left = Math.Max(BoundsHelper.VirtualBoardBounds.Left, Math.Min(left, BoundsHelper.VirtualBoardBounds.Right - 600));
                        top = Math.Max(BoundsHelper.VirtualBoardBounds.Top, Math.Min(top, BoundsHelper.VirtualBoardBounds.Bottom - 484));

                        Canvas.SetLeft(finalGraphContainer, left);
                        Canvas.SetTop(finalGraphContainer, top);
                        dragStartPoint = currentPoint;
                        e.Handled = true;
                    }
                };

                dragHandleBar.MouseLeftButtonUp += (s, e) =>
                {
                    if (isMouseDragging && !activeTouchId.HasValue && e.StylusDevice == null)
                    {
                        isMouseDragging = false;
                        dragHandleBar.ReleaseMouseCapture();
                        RefreshSelectableObjects();
                        e.Handled = true;
                    }
                };

                dragHandleBar.PreviewTouchDown += (s, e) =>
                {
                    if (QASmartTouch.Utilities.InputValidationHelper.IsEventFromButton(e.OriginalSource, dragHandleBar) ||
                        (e.OriginalSource is DependencyObject dep && IsDescendantOf(dep, buttonPanel)))
                        return;
                    if (activeTouchId.HasValue && activeTouchId.Value != e.TouchDevice.Id)
                        return;

                    _isDrawing = false;
                    _currentStroke = null;
                    activeTouchId = e.TouchDevice.Id;
                    dragStartPoint = e.GetTouchPoint(MainInteractiveBoard).Position;
                    dragHandleBar.CaptureTouch(e.TouchDevice);
                    e.Handled = true;
                };

                dragHandleBar.PreviewTouchMove += (s, e) =>
                {
                    if (activeTouchId.HasValue && activeTouchId.Value == e.TouchDevice.Id)
                    {
                        Point currentPoint = e.GetTouchPoint(MainInteractiveBoard).Position;
                        double left = Canvas.GetLeft(finalGraphContainer) + (currentPoint.X - dragStartPoint.X);
                        double top = Canvas.GetTop(finalGraphContainer) + (currentPoint.Y - dragStartPoint.Y);

                        left = Math.Max(BoundsHelper.VirtualBoardBounds.Left, Math.Min(left, BoundsHelper.VirtualBoardBounds.Right - 600));
                        top = Math.Max(BoundsHelper.VirtualBoardBounds.Top, Math.Min(top, BoundsHelper.VirtualBoardBounds.Bottom - 484));

                        Canvas.SetLeft(finalGraphContainer, left);
                        Canvas.SetTop(finalGraphContainer, top);
                        dragStartPoint = currentPoint;
                        e.Handled = true;
                    }
                };

                dragHandleBar.PreviewTouchUp += (s, e) =>
                {
                    if (activeTouchId.HasValue && activeTouchId.Value == e.TouchDevice.Id)
                    {
                        activeTouchId = null;
                        dragHandleBar.ReleaseTouchCapture(e.TouchDevice);
                        RefreshSelectableObjects();
                        e.Handled = true;
                    }
                };

                dragHandleBar.LostTouchCapture += (s, e) =>
                {
                    if (activeTouchId.HasValue && activeTouchId.Value == e.TouchDevice.Id)
                    {
                        activeTouchId = null;
                        RefreshSelectableObjects();
                    }
                };

                // Position container at center of canvas
                double leftPos = (MainInteractiveBoard.ActualWidth - 600) / 2;
                double topPos = (MainInteractiveBoard.ActualHeight - 484) / 2;

                Canvas.SetLeft(finalGraphContainer, BoundsHelper.ClampToVirtualBoard(leftPos, topPos, 600, 484).Left);
                Canvas.SetTop(finalGraphContainer, BoundsHelper.ClampToVirtualBoard(leftPos, topPos, 600, 484).Top);

                // Add to canvas (Cấp phát Z-Index tương tự như biểu đồ khác để đè lên nét vẽ cũ)
                int zIndex = AllocateImageZIndex();
                Panel.SetZIndex(finalGraphContainer, zIndex);
                MainInteractiveBoard.Children.Add(finalGraphContainer);

                // Add to undo stack
                RecordAddAction(finalGraphContainer, "Graph");

                System.Diagnostics.Debug.WriteLine($"✅ Graph inserted with DragHandle at ({leftPos:F0}, {topPos:F0}) with ZIndex {zIndex}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi chèn đồ thị: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ InsertGraphToCanvas error: {ex.Message}");
            }
        }
        
        private void EditGraph(Grid graphContainer)
        {
            try
            {
                var config = graphContainer.Tag;
                if (config == null)
                {
                    MessageBox.Show("Đồ thị không có thông tin cấu hình để chỉnh sửa.", "Thông báo",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                // If it's a 3D Graph Configuration from Form2_Graph3DEditor
                if (config is Graph3DConfiguration config3D)
                {
                    var graph3DEditor = new Form2_Graph3DEditor(config3D);
                    bool? result3D = WindowHelper.ShowChildDialog(graph3DEditor, this);
                    if (result3D == true && graph3DEditor.ExportedGraphImage != null)
                    {
                        var image = graphContainer.Children.OfType<Image>().FirstOrDefault();
                        if (image != null)
                        {
                            image.Source = graph3DEditor.ExportedGraphImage;
                            graphContainer.Tag = graph3DEditor.ExportedConfig;
                            System.Diagnostics.Debug.WriteLine("✅ 3D Graph updated");
                        }
                    }
                    return;
                }

                // Open editor with existing config (Unified 2D / 3D)
                var graphEditor = new Form2_GraphEditorUnified(config);
                bool? result = WindowHelper.ShowChildDialog(graphEditor, this);
                if (result == true && graphEditor.ExportedGraphImage != null)
                {
                    // Update image
                    var image = graphContainer.Children.OfType<Image>().FirstOrDefault();
                    if (image != null)
                    {
                        image.Source = graphEditor.ExportedGraphImage;
                        graphContainer.Tag = graphEditor.ExportedConfig;
                        System.Diagnostics.Debug.WriteLine("✅ Graph updated");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi chỉnh sửa đồ thị: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void DeleteGraph(FrameworkElement graphContainer)
        {
            var result = MessageBox.Show("Bạn có chắc muốn xóa đồ thị này?", "Xác nhận",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                MainInteractiveBoard.Children.Remove(graphContainer);
                RecordRemoveAction(graphContainer, "Graph");
                MainInteractiveBoard.ReleaseAllTouchCaptures();
                MainScrollViewer?.ReleaseAllTouchCaptures();
                System.Diagnostics.Debug.WriteLine("✅ Graph deleted");
            }
        }

        #region Container Drag & Element Attachment Helpers

        /// <summary>
        /// QC_4.2_TABLE_OWNERSHIP: Quản lý danh sách nét vẽ thuộc về từng Bảng dữ liệu.
        /// Chỉ những nét vẽ thực sự được vẽ vào ô bảng mới được đính kèm và di chuyển theo bảng.
        /// </summary>
        private readonly Dictionary<FrameworkElement, HashSet<UIElement>> _tableAttachedStrokes = new();
        private readonly Dictionary<FrameworkElement, HashSet<Stroke>> _tableAttachedNativeStrokes = new();

        /// <summary>
        /// QC_4.2_TABLE_OWNERSHIP: Kiểm tra nếu nét vẽ Native Stroke vừa hoàn thành nằm trong thân một Bảng dữ liệu,
        /// tự động gắn nét vẽ đó vào danh sách thuộc Bảng để trôi theo khi kéo di chuyển bảng.
        /// <summary>
        /// QC_4.2_CONTAINER_OWNED_INKING: Điều hướng nét vẽ hoàn thành vào đúng Container học liệu
        /// (Bảng dữ liệu TableContainer hoặc Ảnh tương tác InteractiveImage / InsertedImage).
        /// Nét vẽ được chuyển đổi tọa độ tương đối sang lớp InkPresenter con của Container đó,
        /// đảm bảo:
        /// 1. Luôn nổi trên mặt Bảng / Ảnh (Z-Index tự nhiên theo Container).
        /// 2. Tự động di chuyển theo Bảng / Ảnh khi kéo thả mà không bị lệch hay trôi.
        /// 3. Ảnh mới chèn che lấp hoàn toàn nét chữ nền cũ trên bảng trắng (Z=100).
        /// </summary>
        public bool TryRouteNativeStrokeToContainer(Stroke stroke)
        {
            if (stroke == null || MainInteractiveBoard == null) return false;

            Rect elemRect = stroke.GetBounds();
            if (elemRect.IsEmpty) return false;

            Point strokeCenter = new Point(elemRect.X + elemRect.Width / 2.0, elemRect.Y + elemRect.Height / 2.0);

            // 1. Kiểm tra các Bảng dữ liệu (TableContainer - Z=500)
            foreach (UIElement child in MainInteractiveBoard.Children)
            {
                if (child is StackPanel sp && sp.Tag is string tag && tag == "TableContainer")
                {
                    double left = Canvas.GetLeft(sp);
                    double top = Canvas.GetTop(sp);
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;
                    double width = sp.ActualWidth > 0 ? sp.ActualWidth : sp.Width;
                    double height = sp.ActualHeight > 0 ? sp.ActualHeight : sp.Height;

                    // Thân bảng (bên dưới thanh tiêu đề 34px)
                    Rect tableBodyRect = new Rect(left, top + 34, width, Math.Max(10, height - 34));

                    if (tableBodyRect.Contains(elemRect) || tableBodyRect.Contains(strokeCenter))
                    {
                        var tableInkPresenter = sp.Children.OfType<Grid>().FirstOrDefault()?.Children.OfType<InkPresenter>().FirstOrDefault();
                        if (tableInkPresenter != null)
                        {
                            // Chuyển đổi tọa độ nét vẽ sang hệ tọa độ cục bộ của thân bảng
                            double dx = -left;
                            double dy = -(top + 34);
                            stroke.Transform(new Matrix(1, 0, 0, 1, dx, dy), false);

                            tableInkPresenter.Strokes.Add(stroke);
                            RecordAddStrokeAction(stroke, "Draw stroke on Table", tableInkPresenter);
                            System.Diagnostics.Debug.WriteLine($"🏷️ [Container Inking] Attached native stroke to Table at ({left:F0}, {top:F0})");
                            return true;
                        }
                    }
                }
            }

            // 2. Kiểm tra các Ảnh tương tác (InteractiveImage / InsertedImage)
            foreach (UIElement child in MainInteractiveBoard.Children)
            {
                if (child is Border b && b.Tag is string tag && (tag == "InteractiveImage" || tag == "InsertedImage"))
                {
                    double left = Canvas.GetLeft(b);
                    double top = Canvas.GetTop(b);
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;
                    double width = b.ActualWidth > 0 ? b.ActualWidth : b.Width;
                    double height = b.ActualHeight > 0 ? b.ActualHeight : b.Height;

                    double headerHeight = (tag == "InteractiveImage") ? 36 : 0;
                    Rect imageBodyRect = new Rect(left, top + headerHeight, width, Math.Max(10, height - headerHeight));

                    if (imageBodyRect.Contains(elemRect) || imageBodyRect.Contains(strokeCenter))
                    {
                        var grid = b.Child as Grid;
                        var imgInkPresenter = grid?.Children.OfType<InkPresenter>().FirstOrDefault();
                        if (imgInkPresenter != null)
                        {
                            // Chuyển đổi tọa độ nét vẽ sang hệ tọa độ cục bộ của thân ảnh
                            double dx = -left;
                            double dy = -(top + headerHeight);
                            stroke.Transform(new Matrix(1, 0, 0, 1, dx, dy), false);

                            imgInkPresenter.Strokes.Add(stroke);
                            RecordAddStrokeAction(stroke, "Draw stroke on Image", imgInkPresenter);
                            System.Diagnostics.Debug.WriteLine($"🏷️ [Container Inking] Attached native stroke to Image at ({left:F0}, {top:F0})");
                            return true;
                        }
                    }
                }
            }

            // 3. Kiểm tra các Biểu đồ tương tác (Interactive Charts - Z=500)
            foreach (UIElement child in MainInteractiveBoard.Children)
            {
                if (child is FrameworkElement feChart && InteractiveChartHelper.IsChartContainer(feChart))
                {
                    double left = Canvas.GetLeft(feChart);
                    double top = Canvas.GetTop(feChart);
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;
                    double width = feChart.ActualWidth > 0 ? feChart.ActualWidth : feChart.Width;
                    double height = feChart.ActualHeight > 0 ? feChart.ActualHeight : feChart.Height;

                    double headerHeight = InteractiveChartHelper.HeaderBarHeight;
                    Rect chartBodyRect = new Rect(left, top + headerHeight, width, Math.Max(10, height - headerHeight));

                    if (chartBodyRect.Contains(elemRect) || chartBodyRect.Contains(strokeCenter))
                    {
                        var chartInkPresenter = InteractiveChartHelper.GetChartInkPresenter(feChart);
                        if (chartInkPresenter != null)
                        {
                            // Chuyển đổi tọa độ nét vẽ sang hệ tọa độ cục bộ của thân biểu đồ
                            double dx = -left;
                            double dy = -(top + headerHeight);
                            stroke.Transform(new Matrix(1, 0, 0, 1, dx, dy), false);

                            chartInkPresenter.Strokes.Add(stroke);
                            RecordAddStrokeAction(stroke, "Draw stroke on Chart", chartInkPresenter);
                            System.Diagnostics.Debug.WriteLine($"🏷️ [Container Inking] Attached native stroke to Chart at ({left:F0}, {top:F0})");
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tẩy xóa nét vẽ trên các InkPresenter nhúng trong Container (Table / Image / Chart)
        /// </summary>
        public void EraseContainerEmbeddedStrokesAt(Point point, bool isPointMode, Point? lastPoint = null)
        {
            var eraserTool = _whiteboardManager?.ToolManager.EraserTool;
            if (eraserTool == null || MainInteractiveBoard == null) return;

            var session = eraserTool.GetSession("mouse");

            // 1. Kiểm tra Bảng dữ liệu
            foreach (UIElement child in MainInteractiveBoard.Children)
            {
                if (child is StackPanel sp && sp.Tag is string tag && tag == "TableContainer")
                {
                    double left = Canvas.GetLeft(sp);
                    double top = Canvas.GetTop(sp);
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;
                    double width = sp.ActualWidth > 0 ? sp.ActualWidth : sp.Width;
                    double height = sp.ActualHeight > 0 ? sp.ActualHeight : sp.Height;

                    Rect tableBodyRect = new Rect(left, top + 34, width, Math.Max(10, height - 34));
                    if (tableBodyRect.Contains(point))
                    {
                        var tableInkPresenter = sp.Children.OfType<Grid>().FirstOrDefault()?.Children.OfType<InkPresenter>().FirstOrDefault();
                        if (tableInkPresenter?.Strokes != null && tableInkPresenter.Strokes.Count > 0)
                        {
                            Point localPt = new Point(point.X - left, point.Y - (top + 34));
                            Point? localLastPt = lastPoint.HasValue ? new Point(lastPoint.Value.X - left, lastPoint.Value.Y - (top + 34)) : null;

                            if (isPointMode)
                            {
                                eraserTool.EraseByPointAt(tableInkPresenter.Strokes, localPt, localLastPt, _eraserSize, session);
                            }
                            else
                            {
                                eraserTool.EraseByStrokeAt(tableInkPresenter.Strokes, localPt, _eraserSize, session);
                            }
                        }
                    }
                }
                else if (child is Border b && b.Tag is string imgTag && (imgTag == "InteractiveImage" || imgTag == "InsertedImage"))
                {
                    double left = Canvas.GetLeft(b);
                    double top = Canvas.GetTop(b);
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;
                    double width = b.ActualWidth > 0 ? b.ActualWidth : b.Width;
                    double height = b.ActualHeight > 0 ? b.ActualHeight : b.Height;

                    double headerHeight = (imgTag == "InteractiveImage") ? 36 : 0;
                    Rect imageBodyRect = new Rect(left, top + headerHeight, width, Math.Max(10, height - headerHeight));
                    if (imageBodyRect.Contains(point))
                    {
                        var grid = b.Child as Grid;
                        var imgInkPresenter = grid?.Children.OfType<InkPresenter>().FirstOrDefault();
                        if (imgInkPresenter?.Strokes != null && imgInkPresenter.Strokes.Count > 0)
                        {
                            Point localPt = new Point(point.X - left, point.Y - (top + headerHeight));
                            Point? localLastPt = lastPoint.HasValue ? new Point(lastPoint.Value.X - left, lastPoint.Value.Y - (top + headerHeight)) : null;

                            if (isPointMode)
                            {
                                eraserTool.EraseByPointAt(imgInkPresenter.Strokes, localPt, localLastPt, _eraserSize, session);
                            }
                            else
                            {
                                eraserTool.EraseByStrokeAt(imgInkPresenter.Strokes, localPt, _eraserSize, session);
                            }
                        }
                    }
                }
                else if (child is FrameworkElement feChart && InteractiveChartHelper.IsChartContainer(feChart))
                {
                    double left = Canvas.GetLeft(feChart);
                    double top = Canvas.GetTop(feChart);
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;
                    double width = feChart.ActualWidth > 0 ? feChart.ActualWidth : feChart.Width;
                    double height = feChart.ActualHeight > 0 ? feChart.ActualHeight : feChart.Height;

                    double headerHeight = InteractiveChartHelper.HeaderBarHeight;
                    Rect chartBodyRect = new Rect(left, top + headerHeight, width, Math.Max(10, height - headerHeight));
                    if (chartBodyRect.Contains(point))
                    {
                        var chartInkPresenter = InteractiveChartHelper.GetChartInkPresenter(feChart);
                        if (chartInkPresenter?.Strokes != null && chartInkPresenter.Strokes.Count > 0)
                        {
                            Point localPt = new Point(point.X - left, point.Y - (top + headerHeight));
                            Point? localLastPt = lastPoint.HasValue ? new Point(lastPoint.Value.X - left, lastPoint.Value.Y - (top + headerHeight)) : null;

                            if (isPointMode)
                            {
                                eraserTool.EraseByPointAt(chartInkPresenter.Strokes, localPt, localLastPt, _eraserSize, session);
                            }
                            else
                            {
                                eraserTool.EraseByStrokeAt(chartInkPresenter.Strokes, localPt, _eraserSize, session);
                            }
                        }
                    }
                }
            }
        }

        /// </summary>
        private void CheckAndAttachNativeStrokeToTable(Stroke stroke)
        {
            if (stroke == null || MainInteractiveBoard == null) return;

            Rect elemRect = stroke.GetBounds();
            if (elemRect.IsEmpty) return;

            Point strokeCenter = new Point(elemRect.X + elemRect.Width / 2.0, elemRect.Y + elemRect.Height / 2.0);

            foreach (UIElement child in MainInteractiveBoard.Children)
            {
                if (child is StackPanel sp && sp.Tag is string tag && tag == "TableContainer")
                {
                    double left = Canvas.GetLeft(sp);
                    double top = Canvas.GetTop(sp);
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;
                    double width = sp.ActualWidth > 0 ? sp.ActualWidth : sp.Width;
                    double height = sp.ActualHeight > 0 ? sp.ActualHeight : sp.Height;

                    // Thân bảng (outerBorder bên dưới thanh tiêu đề cao 34px)
                    Rect tableBodyRect = new Rect(left, top + 34, width, Math.Max(10, height - 34));

                    if (tableBodyRect.Contains(elemRect) || tableBodyRect.Contains(strokeCenter))
                    {
                        if (!_tableAttachedNativeStrokes.TryGetValue(sp, out var set))
                        {
                            set = new HashSet<Stroke>();
                            _tableAttachedNativeStrokes[sp] = set;
                        }
                        set.Add(stroke);
                        System.Diagnostics.Debug.WriteLine($"🏷️ [Table Ownership] Attached native stroke to TableContainer at ({left:F0}, {top:F0})");
                        break;
                    }
                }
            }
        }

        private List<Stroke> GetTableAttachedNativeStrokes(FrameworkElement tableContainer)
        {
            if (tableContainer != null && _tableAttachedNativeStrokes.TryGetValue(tableContainer, out var set))
            {
                set.RemoveWhere(s => MainInkPresenter?.Strokes == null || !MainInkPresenter.Strokes.Contains(s));
                return set.ToList();
            }
            return new List<Stroke>();
        }

        private void MoveAttachedNativeStrokes(List<Stroke> strokes, double deltaX, double deltaY)
        {
            if (strokes == null || strokes.Count == 0 || (deltaX == 0 && deltaY == 0)) return;
            var matrix = new Matrix(1, 0, 0, 1, deltaX, deltaY);
            foreach (var stroke in strokes)
            {
                if (stroke != null && MainInkPresenter?.Strokes != null && MainInkPresenter.Strokes.Contains(stroke))
                {
                    stroke.Transform(matrix, false);
                }
            }
        }

        /// <summary>
        /// QC_4.2_TABLE_OWNERSHIP: Kiểm tra nếu nét vẽ vừa hoàn thành nằm trọn trong thân một Bảng dữ liệu,
        /// tự động gắn nét vẽ đó vào danh sách thuộc Bảng.
        /// </summary>
        private void CheckAndAttachStrokeToTable(UIElement element)
        {
            if (element == null || MainInteractiveBoard == null) return;

            // Chỉ xử lý nét vẽ tay (Polyline hoặc Path nét vẽ từ Touch)
            Rect elemRect = Rect.Empty;
            if (element is Polyline polyline)
            {
                if (polyline.Points == null || polyline.Points.Count == 0) return;
                if (Panel.GetZIndex(polyline) >= ZIndexConstants.SystemUIBase) return;

                double minX = double.MaxValue, maxX = double.MinValue;
                double minY = double.MaxValue, maxY = double.MinValue;
                foreach (var pt in polyline.Points)
                {
                    if (pt.X < minX) minX = pt.X;
                    if (pt.X > maxX) maxX = pt.X;
                    if (pt.Y < minY) minY = pt.Y;
                    if (pt.Y > maxY) maxY = pt.Y;
                }
                elemRect = new Rect(minX, minY, Math.Max(1, maxX - minX), Math.Max(1, maxY - minY));
            }
            else if (element is System.Windows.Shapes.Path path)
            {
                // Chỉ nhận nét vẽ viết tay (có lưu PointCollection trong Tag từ Touch)
                // Tuyệt đối không nhận các hình học Path (hình trụ, hình khối 3D...)
                if (path.Tag is PointCollection pts && pts.Count > 0)
                {
                    if (Panel.GetZIndex(path) >= ZIndexConstants.SystemUIBase) return;

                    double minX = double.MaxValue, maxX = double.MinValue;
                    double minY = double.MaxValue, maxY = double.MinValue;
                    foreach (var pt in pts)
                    {
                        if (pt.X < minX) minX = pt.X;
                        if (pt.X > maxX) maxX = pt.X;
                        if (pt.Y < minY) minY = pt.Y;
                        if (pt.Y > maxY) maxY = pt.Y;
                    }
                    elemRect = new Rect(minX, minY, Math.Max(1, maxX - minX), Math.Max(1, maxY - minY));
                }
                else if (path.Data != null && !path.Data.Bounds.IsEmpty)
                {
                    var b = path.Data.Bounds;
                    double pLeft = double.IsNaN(Canvas.GetLeft(path)) ? 0 : Canvas.GetLeft(path);
                    double pTop = double.IsNaN(Canvas.GetTop(path)) ? 0 : Canvas.GetTop(path);
                    if (path.RenderTransform is TranslateTransform tt)
                    {
                        pLeft += tt.X;
                        pTop += tt.Y;
                    }
                    elemRect = new Rect(pLeft + b.X, pTop + b.Y, b.Width, b.Height);
                }
            }

            if (elemRect.IsEmpty) return;

            Point strokeCenter = new Point(elemRect.X + elemRect.Width / 2.0, elemRect.Y + elemRect.Height / 2.0);

            foreach (UIElement child in MainInteractiveBoard.Children)
            {
                if (child is StackPanel sp && sp.Tag is string tag && tag == "TableContainer")
                {
                    double left = Canvas.GetLeft(sp);
                    double top = Canvas.GetTop(sp);
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;
                    double width = sp.ActualWidth > 0 ? sp.ActualWidth : sp.Width;
                    double height = sp.ActualHeight > 0 ? sp.ActualHeight : sp.Height;

                    // Thân bảng (outerBorder bên dưới thanh tiêu đề cao 34px)
                    Rect tableBodyRect = new Rect(left, top + 34, width, Math.Max(10, height - 34));

                    // Đính kèm nét vẽ nếu toàn bộ nét vẽ nằm trong Bảng HOẶC tâm nét vẽ nằm trong Bảng
                    if (tableBodyRect.Contains(elemRect) || tableBodyRect.Contains(strokeCenter))
                    {
                        if (!_tableAttachedStrokes.TryGetValue(sp, out var set))
                        {
                            set = new HashSet<UIElement>();
                            _tableAttachedStrokes[sp] = set;
                        }
                        set.Add(element);
                        // ✅ Nâng Z-Index cho nét vẽ thuộc Bảng để nổi trên mặt bảng (TableContainer = 500)
                        Panel.SetZIndex(element, ZIndexConstants.TableContainer + 1);
                        System.Diagnostics.Debug.WriteLine($"🏷️ [Table Ownership] Attached stroke to TableContainer at ({left:F0}, {top:F0})");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// QC_4.2_TABLE_OWNERSHIP: Lấy danh sách các nét vẽ thực sự thuộc về Bảng dữ liệu này.
        /// Triệt tiêu 100% lỗi kéo trôi hoặc xóa nhầm đối tượng nền có sẵn trên bảng.
        /// </summary>
        private List<UIElement> GetTableAttachedStrokes(FrameworkElement tableContainer)
        {
            if (tableContainer != null && _tableAttachedStrokes.TryGetValue(tableContainer, out var set))
            {
                set.RemoveWhere(el => MainInteractiveBoard == null || !MainInteractiveBoard.Children.Contains(el));
                return set.ToList();
            }
            return new List<UIElement>();
        }

        /// <summary>
        /// Retrieves all canvas UI elements located inside or overlapping the bounding box of a container (e.g. Table, Text Box)
        /// </summary>
        private List<UIElement> GetElementsInsideContainer(FrameworkElement container)
        {
            var list = new List<UIElement>();
            if (MainInteractiveBoard == null || container == null) return list;

            double cLeft = Canvas.GetLeft(container);
            double cTop = Canvas.GetTop(container);
            if (double.IsNaN(cLeft)) cLeft = 0;
            if (double.IsNaN(cTop)) cTop = 0;
            double cWidth = container.ActualWidth > 0 ? container.ActualWidth : container.Width;
            double cHeight = container.ActualHeight > 0 ? container.ActualHeight : container.Height;

            if (cWidth <= 0) cWidth = 200;
            if (cHeight <= 0) cHeight = 100;

            Rect cRect = new Rect(cLeft, cTop, cWidth, cHeight);
            cRect.Inflate(15, 15); // Slight margin to capture strokes on borders

            foreach (UIElement child in MainInteractiveBoard.Children)
            {
                if (child == container) continue;
                if (child == _zoomAreaOverlay) continue;

                // ✅ QC_4.2_GRID_PROTECT (GR-1): KHÔNG BAO GIỜ đính kèm background elements
                // bgRect/patternRect phủ toàn Canvas → luôn intersect → PHẢI skip
                if (child is FrameworkElement bgCheck && 
                    SelectionManager.IsBackgroundElement(bgCheck))
                    continue;

                // Skip child elements that are inside container
                if (child is FrameworkElement feChild && IsDescendantOf(feChild, container)) continue;

                Rect elemRect = Rect.Empty;

                if (child is Polyline polyline)
                {
                    if (polyline.Points == null || polyline.Points.Count == 0) continue;
                    // Skip selection rectangle or lasso visuals
                    if (Panel.GetZIndex(polyline) >= ZIndexConstants.SystemUIBase) continue;

                    double minX = double.MaxValue, maxX = double.MinValue;
                    double minY = double.MaxValue, maxY = double.MinValue;
                    foreach (var pt in polyline.Points)
                    {
                        if (pt.X < minX) minX = pt.X;
                        if (pt.X > maxX) maxX = pt.X;
                        if (pt.Y < minY) minY = pt.Y;
                        if (pt.Y > maxY) maxY = pt.Y;
                    }
                    elemRect = new Rect(minX, minY, Math.Max(1, maxX - minX), Math.Max(1, maxY - minY));
                }
                else if (child is System.Windows.Shapes.Path path)
                {
                    var bounds = path.RenderedGeometry?.Bounds ?? Rect.Empty;
                    if (!bounds.IsEmpty)
                    {
                        double pLeft = double.IsNaN(Canvas.GetLeft(path)) ? 0 : Canvas.GetLeft(path);
                        double pTop = double.IsNaN(Canvas.GetTop(path)) ? 0 : Canvas.GetTop(path);
                        if (path.RenderTransform is TranslateTransform tt)
                        {
                            pLeft += tt.X;
                            pTop += tt.Y;
                        }
                        elemRect = new Rect(pLeft + bounds.X, pTop + bounds.Y, bounds.Width, bounds.Height);
                    }
                }
                else if (child is FrameworkElement fe)
                {
                    double feLeft = double.IsNaN(Canvas.GetLeft(fe)) ? 0 : Canvas.GetLeft(fe);
                    double feTop = double.IsNaN(Canvas.GetTop(fe)) ? 0 : Canvas.GetTop(fe);
                    double feW = fe.ActualWidth > 0 ? fe.ActualWidth : fe.Width;
                    double feH = fe.ActualHeight > 0 ? fe.ActualHeight : fe.Height;
                    elemRect = new Rect(feLeft, feTop, Math.Max(1, feW), Math.Max(1, feH));
                }

                if (!elemRect.IsEmpty && cRect.IntersectsWith(elemRect))
                {
                    list.Add(child);
                }
            }

            return list;
        }

        private bool IsDescendantOf(DependencyObject element, DependencyObject parent)
        {
            while (element != null)
            {
                if (element == parent) return true;
                // Safe: ContentElement dùng LogicalTreeHelper
                element = (element is System.Windows.Media.Visual || element is System.Windows.Media.Media3D.Visual3D)
                    ? VisualTreeHelper.GetParent(element)
                    : LogicalTreeHelper.GetParent(element);
            }
            return false;
        }

        /// <summary>
        /// Moves attached elements synchronously by deltaX and deltaY
        /// </summary>
        private void MoveAttachedElements(List<UIElement> elements, double deltaX, double deltaY)
        {
            if (elements == null || elements.Count == 0 || (deltaX == 0 && deltaY == 0)) return;

            foreach (var elem in elements)
            {
                // ✅ QC_4.2_GRID_PROTECT (GR-2): Guard cuối cùng — chặn di chuyển background elements
                if (elem is FrameworkElement bgElem && SelectionManager.IsBackgroundElement(bgElem))
                {
                    System.Diagnostics.Debug.WriteLine($"🛑 MoveAttachedElements BLOCKED: {bgElem.GetType().Name} Tag={bgElem.Tag}");
                    continue;
                }

                if (elem is Polyline polyline && polyline.Points != null)
                {
                    for (int i = 0; i < polyline.Points.Count; i++)
                    {
                        Point pt = polyline.Points[i];
                        polyline.Points[i] = new Point(pt.X + deltaX, pt.Y + deltaY);
                    }
                }
                else if (elem is System.Windows.Shapes.Path path)
                {
                    if (path.RenderTransform is TranslateTransform tt)
                    {
                        tt.X += deltaX;
                        tt.Y += deltaY;
                    }
                    else
                    {
                        path.RenderTransform = new TranslateTransform(deltaX, deltaY);
                    }

                    if (path.Tag is PointCollection pts)
                    {
                        for (int i = 0; i < pts.Count; i++)
                        {
                            pts[i] = new Point(pts[i].X + deltaX, pts[i].Y + deltaY);
                        }
                    }
                }
                else if (elem is FrameworkElement fe)
                {
                    double curL = double.IsNaN(Canvas.GetLeft(fe)) ? 0 : Canvas.GetLeft(fe);
                    double curT = double.IsNaN(Canvas.GetTop(fe)) ? 0 : Canvas.GetTop(fe);
                    Canvas.SetLeft(fe, curL + deltaX);
                    Canvas.SetTop(fe, curT + deltaY);
                }
            }
        }

        #endregion

        /// <summary>
        /// Insert table to canvas from TableData with optional specific coordinate placement
        /// </summary>
        public void InsertTableToCanvas(Models.TableData tableData, Point? position = null)
        {
            try
            {
                // Create Grid container for the table cells
                var tableGrid = new Grid
                {
                    Background = Brushes.White,
                    Cursor = Cursors.Arrow
                };

                // Store TableData in Tag for later editing
                tableGrid.Tag = tableData;

                // Create column definitions
                for (int c = 0; c < tableData.ColumnCount; c++)
                {
                    tableGrid.ColumnDefinitions.Add(new ColumnDefinition
                    {
                        Width = new GridLength(tableData.Columns[c].Width)
                    });
                }

                // Create row definitions
                for (int r = 0; r < tableData.RowCount; r++)
                {
                    tableGrid.RowDefinitions.Add(new RowDefinition
                    {
                        Height = new GridLength(tableData.Rows[r].Height)
                    });
                }

                // Create cells
                for (int r = 0; r < tableData.RowCount; r++)
                {
                    for (int c = 0; c < tableData.ColumnCount; c++)
                    {
                        var cellBorder = CreateTableCellBorder(tableData, r, c);
                        tableGrid.Children.Add(cellBorder);
                    }
                }

                // Create outer border for table grid
                var outerBorder = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(tableData.BorderColor)),
                    BorderThickness = new Thickness(tableData.BorderThickness),
                    Child = tableGrid,
                    Background = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                // Main Container for Table + Drag Handle Bar
                var finalContainer = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Background = Brushes.Transparent,
                    // ✅ QC_4.2_TABLE_TAG_FIX (T8): Gán Tag để IsDragHandleOrEmbeddedWidget nhận diện
                    Tag = "TableContainer"
                };

                // Create Top Drag Bar (Header Handle)
                var dragHandleBar = new Border
                {
                    Height = 34,
                    Background = new SolidColorBrush(Color.FromRgb(41, 128, 185)), // QA Brand Primary Blue
                    CornerRadius = new CornerRadius(6, 6, 0, 0),
                    Cursor = Cursors.SizeAll,
                    // ✅ QC_4.2_TABLE_TAG_FIX (T8): Gán Tag để IsDragHandleOrEmbeddedWidget nhận diện
                    Tag = "DragHandle",
                    ToolTip = "Nhấn giữ để di chuyển Bảng trên màn hình"
                };

                var dragHandleGrid = new Grid();
                var titleText = new TextBlock
                {
                    Text = "📊 Bảng",
                    Foreground = Brushes.White,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                dragHandleGrid.Children.Add(titleText);

                // Button panel with Function Buttons: + Hàng, + Cột, ⚙️ More, Move 🖐️, Delete ❌
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 4, 0)
                };

                // 1. Nút Thêm Hàng ➕ Hàng
                var btnAddRow = new Button
                {
                    Content = "+ Hàng",
                    Width = 54,
                    Height = 26,
                    Margin = new Thickness(2, 0, 2, 0),
                    ToolTip = "Thêm một hàng mới vào cuối bảng",
                    Background = new SolidColorBrush(Color.FromRgb(39, 174, 96)), // Emerald Green
                    Foreground = Brushes.White,
                    BorderBrush = Brushes.Transparent,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Cursor = Cursors.Hand
                };
                // ✅ QC_4.2_TABLE_DOUBLE_TAP_FIX: 1 cú chạm sinh cả TouchUp lẫn Click → chỉ thực thi 1 lần
                WireTableTapButton(btnAddRow, () => AddTableRow(finalContainer));
                buttonPanel.Children.Add(btnAddRow);

                // 2. Nút Thêm Cột ➕ Cột
                var btnAddCol = new Button
                {
                    Content = "+ Cột",
                    Width = 52,
                    Height = 26,
                    Margin = new Thickness(2, 0, 2, 0),
                    ToolTip = "Thêm một cột mới vào bên phải bảng",
                    Background = new SolidColorBrush(Color.FromRgb(22, 160, 133)), // Teal
                    Foreground = Brushes.White,
                    BorderBrush = Brushes.Transparent,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Cursor = Cursors.Hand
                };
                WireTableTapButton(btnAddCol, () => AddTableColumn(finalContainer));
                buttonPanel.Children.Add(btnAddCol);

                // 3. Nút Tùy chọn ⚙️ (Bớt hàng, bớt cột, chỉnh sửa chi tiết)
                var btnMore = new Button
                {
                    Content = "⚙️",
                    Width = 26,
                    Height = 26,
                    Margin = new Thickness(2, 0, 2, 0),
                    ToolTip = "Tùy chọn hàng/cột và chỉnh sửa chi tiết",
                    Background = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
                    Foreground = Brushes.Black,
                    BorderBrush = Brushes.Transparent,
                    FontSize = 12,
                    Cursor = Cursors.Hand
                };
                var moreMenu = new ContextMenu();
                var miAddRow = new MenuItem { Header = "➕ Thêm 1 hàng mới" };
                miAddRow.Click += (s, e) => AddTableRow(finalContainer);
                var miAddCol = new MenuItem { Header = "➕ Thêm 1 cột mới" };
                miAddCol.Click += (s, e) => AddTableColumn(finalContainer);
                var miDelRow = new MenuItem { Header = "➖ Bớt 1 hàng cuối" };
                miDelRow.Click += (s, e) => DeleteLastTableRow(finalContainer);
                var miDelCol = new MenuItem { Header = "➖ Bớt 1 cột cuối" };
                miDelCol.Click += (s, e) => DeleteLastTableColumn(finalContainer);
                var miDistributeCols = new MenuItem { Header = "⚖️ Chia đều độ rộng các cột" };
                miDistributeCols.Click += (s, e) => DistributeColumnsEvenly(finalContainer);
                var miDistributeRows = new MenuItem { Header = "⚖️ Chia đều chiều cao các hàng" };
                miDistributeRows.Click += (s, e) => DistributeRowsEvenly(finalContainer);
                var miEditFull = new MenuItem { Header = "✏️ Tùy chỉnh chi tiết (Màu sắc, kích cỡ)..." };
                miEditFull.Click += (s, e) => EditTable(outerBorder);

                moreMenu.Items.Add(miAddRow);
                moreMenu.Items.Add(miAddCol);
                moreMenu.Items.Add(new Separator());
                moreMenu.Items.Add(miDelRow);
                moreMenu.Items.Add(miDelCol);
                moreMenu.Items.Add(new Separator());
                moreMenu.Items.Add(miDistributeCols);
                moreMenu.Items.Add(miDistributeRows);
                moreMenu.Items.Add(new Separator());
                moreMenu.Items.Add(miEditFull);

                btnMore.Click += (s, e) =>
                {
                    e.Handled = true;
                    moreMenu.PlacementTarget = btnMore;
                    moreMenu.IsOpen = true;
                };
                btnMore.PreviewTouchUp += (s, e) =>
                {
                    e.Handled = true;
                    moreMenu.PlacementTarget = btnMore;
                    moreMenu.IsOpen = true;
                };
                buttonPanel.Children.Add(btnMore);

                // 4. Nút Di chuyển 🖐️ (Nút chuyên biệt để di chuyển Bảng)
                var btnMove = new Button
                {
                    Content = "🖐️",
                    Width = 28,
                    Height = 26,
                    Margin = new Thickness(2, 0, 2, 0),
                    ToolTip = "Click giữ hoặc chạm giữ nút này để di chuyển Bảng",
                    Background = new SolidColorBrush(Color.FromRgb(52, 152, 219)), // QA Primary Blue
                    Foreground = Brushes.White,
                    BorderBrush = Brushes.Transparent,
                    FontSize = 13,
                    Cursor = Cursors.SizeAll
                };
                buttonPanel.Children.Add(btnMove);

                // 5. Nút Xóa ❌
                var btnDelete = new Button
                {
                    Content = "❌",
                    Width = 28,
                    Height = 26,
                    Margin = new Thickness(2, 0, 2, 0),
                    ToolTip = "Xóa bảng khỏi màn hình",
                    Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                    Foreground = Brushes.White,
                    BorderBrush = Brushes.Transparent,
                    FontSize = 13,
                    Cursor = Cursors.Hand
                };
                // ✅ QC_4.2_TABLE_TOUCHUP_FIX (T5): Dùng TouchUp thay TouchDown
                // tránh vô tình chọn "Có" khi nhấc tay khỏi MessageBox
                WireTableTapButton(btnDelete, () => DeleteTable(finalContainer));
                buttonPanel.Children.Add(btnDelete);

                dragHandleGrid.Children.Add(buttonPanel);
                dragHandleBar.Child = dragHandleGrid;

                // ✨ CONTAINER-OWNED INKING: Bọc outerBorder trong Grid và đặt TableInkPresenter nổi trên mặt bảng
                var tableBodyGrid = new Grid();
                tableBodyGrid.Children.Add(outerBorder);

                var tableInkPresenter = new InkPresenter
                {
                    IsHitTestVisible = false,
                    Tag = "TableInkPresenter"
                };
                tableBodyGrid.Children.Add(tableInkPresenter);

                finalContainer.Children.Add(dragHandleBar);
                finalContainer.Children.Add(tableBodyGrid);

                // Calculate size (đảm bảo độ rộng tối thiểu 280 để vừa thanh công cụ)
                double totalWidth = Math.Max(tableData.GetTotalWidth() + (tableData.BorderThickness * 2), 280);
                double totalHeight = tableData.GetTotalHeight() + (tableData.BorderThickness * 2) + 34;

                finalContainer.Width = totalWidth;
                finalContainer.Height = totalHeight;

                // Enable Drag-to-Move when pressing & holding btnMove (or dragHandleBar)
                bool isDragging = false;
                Point dragStartPoint = new Point();
                List<UIElement> attachedElements = new List<UIElement>();
                List<Stroke> attachedNativeStrokes = new List<Stroke>();

                Action<Point> startMove = (startPt) =>
                {
                    _isDrawing = false;
                    _currentStroke = null;
                    isDragging = true;
                    dragStartPoint = startPt;
                    attachedElements = GetTableAttachedStrokes(finalContainer);
                    attachedNativeStrokes = GetTableAttachedNativeStrokes(finalContainer);
                };

                Action<Point> processMove = (currentPoint) =>
                {
                    if (isDragging)
                    {
                        double oldLeft = Canvas.GetLeft(finalContainer);
                        double oldTop = Canvas.GetTop(finalContainer);

                        double newLeft = oldLeft + (currentPoint.X - dragStartPoint.X);
                        double newTop = oldTop + (currentPoint.Y - dragStartPoint.Y);

                        // Keep within canvas bounds
                        newLeft = Math.Max(BoundsHelper.VirtualBoardBounds.Left, Math.Min(newLeft, BoundsHelper.VirtualBoardBounds.Right - totalWidth));
                        newTop = Math.Max(BoundsHelper.VirtualBoardBounds.Top, Math.Min(newTop, BoundsHelper.VirtualBoardBounds.Bottom - totalHeight));

                        double actualDeltaX = newLeft - oldLeft;
                        double actualDeltaY = newTop - oldTop;

                        Canvas.SetLeft(finalContainer, newLeft);
                        Canvas.SetTop(finalContainer, newTop);

                        MoveAttachedElements(attachedElements, actualDeltaX, actualDeltaY);
                        MoveAttachedNativeStrokes(attachedNativeStrokes, actualDeltaX, actualDeltaY);

                        dragStartPoint = currentPoint;
                        _selectionBox?.UpdatePosition();
                    }
                };

                Action endMove = () =>
                {
                    if (isDragging)
                    {
                        isDragging = false;
                        attachedElements.Clear();
                        attachedNativeStrokes.Clear();
                        RefreshSelectableObjects();
                    }
                };

                // ============================================================
                // Hook Move Events on btnMove & dragHandleBar
                // QC_4.2_TABLE_MOVE_FIX_V2: Tách biệt độc lập Touch và Mouse
                // ============================================================
                int? activeTableTouchId = null;

                // --- btnMove: Mouse events ---
                btnMove.MouseLeftButtonDown += (s, e) =>
                {
                    if (activeTableTouchId.HasValue || e.StylusDevice != null) return;
                    startMove(e.GetPosition(MainInteractiveBoard));
                    btnMove.CaptureMouse();
                    e.Handled = true;
                };

                btnMove.MouseMove += (s, e) =>
                {
                    if (isDragging && !activeTableTouchId.HasValue && e.StylusDevice == null)
                    {
                        processMove(e.GetPosition(MainInteractiveBoard));
                        e.Handled = true; // ✅ Issue #5: Chặn event bubble khi đang kéo
                    }
                };

                btnMove.MouseLeftButtonUp += (s, e) =>
                {
                    if (activeTableTouchId.HasValue || e.StylusDevice != null) return;
                    // ✅ Issue #2: Luôn giải phóng capture bất kể isDragging
                    endMove();
                    if (btnMove.IsMouseCaptured)
                        btnMove.ReleaseMouseCapture();
                    e.Handled = true;
                };

                // ✅ Issue #2: Xử lý mất capture bất ngờ (Alt+Tab, cửa sổ hệ thống)
                btnMove.LostMouseCapture += (s, e) =>
                {
                    if (!activeTableTouchId.HasValue)
                        endMove();
                };

                // --- btnMove: Touch events ---
                btnMove.PreviewTouchDown += (s, e) =>
                {
                    if (activeTableTouchId.HasValue && activeTableTouchId.Value != e.TouchDevice.Id) return;
                    activeTableTouchId = e.TouchDevice.Id;
                    startMove(e.GetTouchPoint(MainInteractiveBoard).Position);
                    btnMove.CaptureTouch(e.TouchDevice);
                    e.Handled = true;
                };

                btnMove.PreviewTouchMove += (s, e) =>
                {
                    if (isDragging && activeTableTouchId == e.TouchDevice.Id)
                    {
                        processMove(e.GetTouchPoint(MainInteractiveBoard).Position);
                        e.Handled = true;
                    }
                };

                btnMove.PreviewTouchUp += (s, e) =>
                {
                    if (activeTableTouchId == e.TouchDevice.Id)
                    {
                        activeTableTouchId = null;
                        // ✅ Issue #2: Luôn giải phóng capture bất kể isDragging
                        endMove();
                        if (e.TouchDevice.Captured == btnMove)
                            btnMove.ReleaseTouchCapture(e.TouchDevice);
                        e.Handled = true;
                    }
                };

                // ✅ Issue #2: Xử lý mất touch capture bất ngờ
                btnMove.LostTouchCapture += (s, e) =>
                {
                    if (activeTableTouchId == e.TouchDevice.Id)
                    {
                        activeTableTouchId = null;
                        endMove();
                    }
                };

                // --- dragHandleBar: Mouse events ---
                dragHandleBar.MouseLeftButtonDown += (s, e) =>
                {
                    if (activeTableTouchId.HasValue || e.StylusDevice != null) return;
                    if (e.OriginalSource is DependencyObject depObj && IsDescendantOf(depObj, buttonPanel)) return;
                    startMove(e.GetPosition(MainInteractiveBoard));
                    dragHandleBar.CaptureMouse();
                    e.Handled = true;
                };

                dragHandleBar.MouseMove += (s, e) =>
                {
                    if (isDragging && !activeTableTouchId.HasValue && e.StylusDevice == null)
                    {
                        processMove(e.GetPosition(MainInteractiveBoard));
                        e.Handled = true; // ✅ Issue #5: Chặn event bubble khi đang kéo
                    }
                };

                dragHandleBar.MouseLeftButtonUp += (s, e) =>
                {
                    if (activeTableTouchId.HasValue || e.StylusDevice != null) return;
                    // ✅ Issue #2 & #6: Luôn giải phóng capture + e.Handled
                    endMove();
                    if (dragHandleBar.IsMouseCaptured)
                        dragHandleBar.ReleaseMouseCapture();
                    e.Handled = true;
                };

                // ✅ Issue #2: Xử lý mất capture bất ngờ
                dragHandleBar.LostMouseCapture += (s, e) =>
                {
                    if (!activeTableTouchId.HasValue)
                        endMove();
                };

                // --- dragHandleBar: Touch events --- (✅ Issue #4: Bổ sung Touch cho dragHandleBar)
                dragHandleBar.PreviewTouchDown += (s, e) =>
                {
                    if (e.OriginalSource is DependencyObject depObj && IsDescendantOf(depObj, buttonPanel)) return;
                    if (activeTableTouchId.HasValue && activeTableTouchId.Value != e.TouchDevice.Id) return;
                    activeTableTouchId = e.TouchDevice.Id;
                    startMove(e.GetTouchPoint(MainInteractiveBoard).Position);
                    dragHandleBar.CaptureTouch(e.TouchDevice);
                    e.Handled = true;
                };

                dragHandleBar.PreviewTouchMove += (s, e) =>
                {
                    if (isDragging && activeTableTouchId == e.TouchDevice.Id)
                    {
                        processMove(e.GetTouchPoint(MainInteractiveBoard).Position);
                        e.Handled = true;
                    }
                };

                dragHandleBar.PreviewTouchUp += (s, e) =>
                {
                    if (activeTableTouchId == e.TouchDevice.Id)
                    {
                        activeTableTouchId = null;
                        endMove();
                        if (e.TouchDevice.Captured == dragHandleBar)
                            dragHandleBar.ReleaseTouchCapture(e.TouchDevice);
                        e.Handled = true;
                    }
                };

                // ✅ Issue #2: Xử lý mất touch capture bất ngờ
                dragHandleBar.LostTouchCapture += (s, e) =>
                {
                    if (activeTableTouchId == e.TouchDevice.Id)
                    {
                        activeTableTouchId = null;
                        endMove();
                    }
                };

                // Position at specified position or center of canvas
                double leftPos = position?.X ?? ((MainInteractiveBoard.ActualWidth - totalWidth) / 2);
                double topPos = position?.Y ?? ((MainInteractiveBoard.ActualHeight - totalHeight) / 2);

                // Keep within canvas bounds
                leftPos = Math.Max(BoundsHelper.VirtualBoardBounds.Left, Math.Min(leftPos, BoundsHelper.VirtualBoardBounds.Right - totalWidth));
                topPos = Math.Max(BoundsHelper.VirtualBoardBounds.Top, Math.Min(topPos, BoundsHelper.VirtualBoardBounds.Bottom - totalHeight));

                Canvas.SetLeft(finalContainer, leftPos);
                Canvas.SetTop(finalContainer, topPos);

                // Add to canvas
                MainInteractiveBoard.Children.Add(finalContainer);
                // ✅ QC_4.2_TABLE_ZINDEX (T9): Đặt Z-Index cho Bảng TRÊN nét vẽ, DƯỚI Widget
                Panel.SetZIndex(finalContainer, QASmartTouch.Helpers.ZIndexConstants.TableContainer);

                // Add to undo stack
                RecordAddAction(finalContainer, "Table");

                // ✅ N21 FIX: Register finalContainer with SelectionManager so clicks do not hit-test null and deselect/remove
                RefreshSelectableObjects();

                // Setup interactive column/row resizers and corner scale grip
                RefreshTableResizers(finalContainer);

                System.Diagnostics.Debug.WriteLine($"✅ Table inserted with DragHandle at ({leftPos:F0}, {topPos:F0}), size: {totalWidth:F0}x{totalHeight:F0}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi chèn bảng: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ InsertTableToCanvas error: {ex.Message}");
            }
        }

        /// <summary>
        /// Tạo Border và TextBlock cho một cell bảng kèm hỗ trợ sửa chữ trực tiếp khi chạm/nhấp đúp
        /// </summary>
        private Border CreateTableCellBorder(Models.TableData tableData, int r, int c)
        {
            var cell = tableData.GetCell(r, c);

            var cellBorder = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(tableData.BorderColor)),
                BorderThickness = new Thickness(tableData.BorderThickness),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(cell.BackgroundColor)),
                Padding = new Thickness(tableData.CellPadding),
                Tag = cell,
                Cursor = Cursors.Arrow
            };

            var textBlock = new TextBlock
            {
                Text = string.IsNullOrEmpty(cell.Content) ? " " : cell.Content,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(cell.ForegroundColor)),
                FontFamily = new FontFamily(cell.FontFamily),
                FontSize = cell.FontSize,
                FontWeight = cell.IsBold ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = cell.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = GetTextAlignment(cell.HorizontalAlignment),
                VerticalAlignment = GetVerticalAlignment(cell.VerticalAlignment)
            };

            if (cell.IsUnderline)
            {
                textBlock.TextDecorations = TextDecorations.Underline;
            }

            cellBorder.Child = textBlock;

            // Double click (Mouse) to edit cell inline
            cellBorder.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    if (_drawingEnabled || _eraserEnabled || _shapeDrawingEnabled) return;

                    EnableInlineCellEditing(cellBorder, textBlock, cell, tableData);
                    e.Handled = true;
                }
            };

            // Double tap (Touch) to edit cell inline on IFP touch screens
            DateTime lastTouch = DateTime.MinValue;
            cellBorder.PreviewTouchDown += (s, e) =>
            {
                // QC_4.2_TABLE_PEN_PROTECTION: Khi đang ở chế độ Bút vẽ/Tẩy/Hình khối -> KHÓA hoàn toàn để giáo viên viết tay & chấm mực thoải mái
                if (_drawingEnabled || _eraserEnabled || _shapeDrawingEnabled) return;

                var now = DateTime.Now;
                if ((now - lastTouch).TotalMilliseconds < 450)
                {
                    EnableInlineCellEditing(cellBorder, textBlock, cell, tableData);
                    e.Handled = true;
                }
                lastTouch = now;
            };

            Grid.SetRow(cellBorder, r);
            Grid.SetColumn(cellBorder, c);

            return cellBorder;
        }

        /// <summary>
        /// Gắn hành động cho nút trên thanh công cụ Bảng, hỗ trợ cả chuột lẫn cảm ứng.
        /// Trên màn hình tương tác, một cú chạm sinh ra cả PreviewTouchUp lẫn Click (mouse/stylus promotion);
        /// hàm này chỉ thực thi 1 lần cho mỗi cú bấm bằng cách bỏ qua sự kiện trùng đến từ nguồn kia trong 250ms.
        /// Các cú bấm liên tiếp cùng một nguồn vẫn luôn được xử lý.
        /// </summary>
        private static void WireTableTapButton(Button button, Action action)
        {
            const long DuplicateWindowMs = 250;
            long lastTouchTick = long.MinValue / 2;
            long lastClickTick = long.MinValue / 2;

            button.PreviewTouchUp += (s, e) =>
            {
                e.Handled = true;
                long now = Environment.TickCount64;
                bool isDuplicate = now - lastClickTick < DuplicateWindowMs;
                lastTouchTick = now;
                if (!isDuplicate) action();
                lastTouchTick = Environment.TickCount64; // mốc sau action (action có thể là hộp thoại modal)
            };

            button.Click += (s, e) =>
            {
                e.Handled = true;
                long now = Environment.TickCount64;
                bool isDuplicate = now - lastTouchTick < DuplicateWindowMs;
                lastClickTick = now;
                if (!isDuplicate) action();
                lastClickTick = Environment.TickCount64;
            };
        }

        /// <summary>
        /// Thêm 1 hàng mới vào cuối bảng động mà không cần load lại bảng
        /// </summary>
        public void AddTableRow(StackPanel finalContainer)
        {
            try
            {
                var tableBodyGrid = finalContainer.Children.OfType<Grid>().FirstOrDefault();
                if (tableBodyGrid == null) return;

                var outerBorder = tableBodyGrid.Children.OfType<Border>().FirstOrDefault();
                if (outerBorder == null) return;

                var tableGrid = outerBorder.Child as Grid;
                var tableData = tableGrid?.Tag as Models.TableData;
                if (tableGrid == null || tableData == null) return;

                // 1. Cập nhật Model TableData
                tableData.AddRow();
                int newRowIndex = tableData.RowCount - 1;
                double rowHeight = tableData.Rows[newRowIndex].Height;

                // 2. Thêm RowDefinition vào UI Grid
                tableGrid.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(rowHeight)
                });

                // 3. Tạo cells cho hàng mới
                for (int c = 0; c < tableData.ColumnCount; c++)
                {
                    var cellBorder = CreateTableCellBorder(tableData, newRowIndex, c);
                    tableGrid.Children.Add(cellBorder);
                }

                // 4. Cập nhật chiều cao Container
                double totalHeight = tableData.GetTotalHeight() + (tableData.BorderThickness * 2) + 34;
                finalContainer.Height = totalHeight;

                // Đảm bảo không tràn đáy màn hình
                double top = Canvas.GetTop(finalContainer);
                if (top + totalHeight > BoundsHelper.VirtualBoardBounds.Bottom && top > BoundsHelper.VirtualBoardBounds.Top + 50)
                {
                    Canvas.SetTop(finalContainer, Math.Max(BoundsHelper.VirtualBoardBounds.Top, BoundsHelper.VirtualBoardBounds.Bottom - totalHeight - 20));
                }

                _selectionBox?.UpdatePosition();
                RefreshTableResizers(finalContainer);
                System.Diagnostics.Debug.WriteLine($"✅ Đã thêm hàng mới: Tổng số hàng = {tableData.RowCount}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ AddTableRow error: {ex.Message}");
            }
        }

        /// <summary>
        /// Thêm 1 cột mới vào bên phải bảng động mà không cần load lại bảng
        /// </summary>
        public void AddTableColumn(StackPanel finalContainer)
        {
            try
            {
                var tableBodyGrid = finalContainer.Children.OfType<Grid>().FirstOrDefault();
                if (tableBodyGrid == null) return;

                var outerBorder = tableBodyGrid.Children.OfType<Border>().FirstOrDefault();
                if (outerBorder == null) return;

                var tableGrid = outerBorder.Child as Grid;
                var tableData = tableGrid?.Tag as Models.TableData;
                if (tableGrid == null || tableData == null) return;

                // 1. Cập nhật Model TableData
                tableData.AddColumn();
                int newColIndex = tableData.ColumnCount - 1;
                double colWidth = tableData.Columns[newColIndex].Width;

                // 2. Thêm ColumnDefinition vào UI Grid
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(colWidth)
                });

                // 3. Tạo cells cho cột mới cho toàn bộ các hàng
                for (int r = 0; r < tableData.RowCount; r++)
                {
                    var cellBorder = CreateTableCellBorder(tableData, r, newColIndex);
                    tableGrid.Children.Add(cellBorder);
                }

                // 4. Cập nhật chiều rộng Container
                double totalWidth = Math.Max(tableData.GetTotalWidth() + (tableData.BorderThickness * 2), 280);
                finalContainer.Width = totalWidth;

                // Đảm bảo không tràn lề phải màn hình
                double left = Canvas.GetLeft(finalContainer);
                if (left + totalWidth > BoundsHelper.VirtualBoardBounds.Right && left > BoundsHelper.VirtualBoardBounds.Left + 50)
                {
                    Canvas.SetLeft(finalContainer, Math.Max(BoundsHelper.VirtualBoardBounds.Left, BoundsHelper.VirtualBoardBounds.Right - totalWidth - 20));
                }

                _selectionBox?.UpdatePosition();
                RefreshTableResizers(finalContainer);
                System.Diagnostics.Debug.WriteLine($"✅ Đã thêm cột mới: Tổng số cột = {tableData.ColumnCount}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ AddTableColumn error: {ex.Message}");
            }
        }

        /// <summary>
        /// Xóa hàng cuối cùng của bảng
        /// </summary>
        public void DeleteLastTableRow(StackPanel finalContainer)
        {
            try
            {
                var tableBodyGrid = finalContainer.Children.OfType<Grid>().FirstOrDefault();
                if (tableBodyGrid == null) return;

                var outerBorder = tableBodyGrid.Children.OfType<Border>().FirstOrDefault();
                if (outerBorder == null) return;

                var tableGrid = outerBorder.Child as Grid;
                var tableData = tableGrid?.Tag as Models.TableData;
                if (tableGrid == null || tableData == null || tableData.RowCount <= 1)
                {
                    return; // Giữ lại ít nhất 1 hàng
                }

                int lastRowIndex = tableData.RowCount - 1;

                // Xóa các cell thuộc hàng cuối
                var cellsToRemove = tableGrid.Children.OfType<Border>()
                    .Where(b => Grid.GetRow(b) == lastRowIndex)
                    .ToList();
                foreach (var cell in cellsToRemove)
                {
                    tableGrid.Children.Remove(cell);
                }

                // Xóa RowDefinition cuối
                if (tableGrid.RowDefinitions.Count > lastRowIndex)
                {
                    tableGrid.RowDefinitions.RemoveAt(lastRowIndex);
                }

                // Cập nhật model
                tableData.DeleteRow(lastRowIndex);

                // Cập nhật chiều cao
                double totalHeight = tableData.GetTotalHeight() + (tableData.BorderThickness * 2) + 34;
                finalContainer.Height = totalHeight;

                _selectionBox?.UpdatePosition();
                RefreshTableResizers(finalContainer);
                System.Diagnostics.Debug.WriteLine($"✅ Đã bớt 1 hàng: Còn {tableData.RowCount} hàng");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ DeleteLastTableRow error: {ex.Message}");
            }
        }

        /// <summary>
        /// Xóa cột cuối cùng của bảng
        /// </summary>
        public void DeleteLastTableColumn(StackPanel finalContainer)
        {
            try
            {
                var tableBodyGrid = finalContainer.Children.OfType<Grid>().FirstOrDefault();
                if (tableBodyGrid == null) return;

                var outerBorder = tableBodyGrid.Children.OfType<Border>().FirstOrDefault();
                if (outerBorder == null) return;

                var tableGrid = outerBorder.Child as Grid;
                var tableData = tableGrid?.Tag as Models.TableData;
                if (tableGrid == null || tableData == null || tableData.ColumnCount <= 1)
                {
                    return; // Giữ lại ít nhất 1 cột
                }

                int lastColIndex = tableData.ColumnCount - 1;

                // Xóa các cell thuộc cột cuối
                var cellsToRemove = tableGrid.Children.OfType<Border>()
                    .Where(b => Grid.GetColumn(b) == lastColIndex)
                    .ToList();
                foreach (var cell in cellsToRemove)
                {
                    tableGrid.Children.Remove(cell);
                }

                // Xóa ColumnDefinition cuối
                if (tableGrid.ColumnDefinitions.Count > lastColIndex)
                {
                    tableGrid.ColumnDefinitions.RemoveAt(lastColIndex);
                }

                // Cập nhật model
                tableData.DeleteColumn(lastColIndex);

                // Cập nhật chiều rộng
                double totalWidth = Math.Max(tableData.GetTotalWidth() + (tableData.BorderThickness * 2), 280);
                finalContainer.Width = totalWidth;

                _selectionBox?.UpdatePosition();
                RefreshTableResizers(finalContainer);
                System.Diagnostics.Debug.WriteLine($"✅ Đã bớt 1 cột: Còn {tableData.ColumnCount} cột");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ DeleteLastTableColumn error: {ex.Message}");
            }
        }

        #region Table Resizing Engine (Smart Whiteboard Table Resizer)

        /// <summary>
        /// Khởi tạo và đồng bộ hóa các tay nắm chia độ rộng cột, chiều cao hàng và thu phóng tỷ lệ bảng (Mouse & Touch IFP)
        /// </summary>
        public void RefreshTableResizers(StackPanel finalContainer)
        {
            try
            {
                if (finalContainer == null) return;
                var tableBodyGrid = finalContainer.Children.OfType<Grid>().FirstOrDefault();
                if (tableBodyGrid == null) return;

                var outerBorder = tableBodyGrid.Children.OfType<Border>().FirstOrDefault();
                var tableGrid = outerBorder?.Child as Grid;
                var tableData = tableGrid?.Tag as Models.TableData;
                if (tableGrid == null || tableData == null) return;

                // Tìm hoặc khởi tạo resizerCanvas
                var resizerCanvas = tableBodyGrid.Children.OfType<Canvas>().FirstOrDefault(c => c.Tag as string == "TableResizerCanvas");
                if (resizerCanvas == null)
                {
                    resizerCanvas = new Canvas
                    {
                        Tag = "TableResizerCanvas",
                        ClipToBounds = false,
                        IsHitTestVisible = true
                    };
                    tableBodyGrid.Children.Add(resizerCanvas);
                }
                resizerCanvas.Children.Clear();

                double borderThick = tableData.BorderThickness;
                double totalTableW = tableData.GetTotalWidth() + (borderThick * 2);
                double totalTableH = tableData.GetTotalHeight() + (borderThick * 2);

                // 1. Tooltip Badge nổi hiển thị kích thước trực quan khi kéo
                var sizeBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(235, 33, 47, 61)), // Deep slate
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(8, 3, 8, 3),
                    Visibility = Visibility.Collapsed,
                    IsHitTestVisible = false
                };
                var sizeBadgeText = new TextBlock
                {
                    Foreground = Brushes.White,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold
                };
                sizeBadge.Child = sizeBadgeText;
                resizerCanvas.Children.Add(sizeBadge);
                Panel.SetZIndex(sizeBadge, 200);

                // Action hiển thị Badge kích thước
                Action<string, double, double> showBadge = (text, x, y) =>
                {
                    sizeBadgeText.Text = text;
                    sizeBadge.Visibility = Visibility.Visible;
                    Canvas.SetLeft(sizeBadge, Math.Max(0, x - 35));
                    Canvas.SetTop(sizeBadge, Math.Max(-28, y - 28));
                };

                Action hideBadge = () =>
                {
                    sizeBadge.Visibility = Visibility.Collapsed;
                };

                // 2. TẠO CÁC ĐƯỜNG PHÂN CÁCH CỘT (VERTICAL COLUMN DIVIDERS)
                // Cho tất cả các cột c từ 0 đến ColumnCount - 1 (vạch biên bên phải của cột c)
                double currentX = borderThick;
                for (int c = 0; c < tableData.ColumnCount; c++)
                {
                    int colIndex = c;
                    double colW = tableData.Columns[colIndex].Width;
                    currentX += colW;
                    double dividerX = currentX;

                    var colHandle = new Border
                    {
                        Width = 14, // Vùng chạm 14px thuận tiện cho cảm ứng màn hình tương tác
                        Height = Math.Max(totalTableH, 40),
                        Background = Brushes.Transparent,
                        Cursor = Cursors.SizeWE,
                        Tag = colIndex,
                        ToolTip = $"Kéo để chỉnh độ rộng Cột {colIndex + 1}"
                    };

                    // Thanh vạch phát sáng màu xanh khi di chuột hoặc chạm
                    var guideLine = new Border
                    {
                        Width = 2,
                        Background = new SolidColorBrush(Color.FromRgb(52, 152, 219)), // Primary Blue
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Opacity = 0.0
                    };
                    colHandle.Child = guideLine;

                    Canvas.SetLeft(colHandle, dividerX - 7);
                    Canvas.SetTop(colHandle, 0);

                    // Hiệu ứng Hover
                    colHandle.MouseEnter += (s, e) => guideLine.Opacity = 0.85;
                    colHandle.MouseLeave += (s, e) =>
                    {
                        if (!colHandle.IsMouseCaptured) guideLine.Opacity = 0.0;
                    };

                    // Drag to Resize Column
                    bool isDraggingCol = false;
                    Point startColPt = new Point();
                    double initialColW = 0;

                    Action<Point> startColDrag = (pt) =>
                    {
                        isDraggingCol = true;
                        startColPt = pt;
                        initialColW = tableData.Columns[colIndex].Width;
                        guideLine.Opacity = 1.0;
                        guideLine.Width = 3;
                        guideLine.Background = new SolidColorBrush(Color.FromRgb(41, 128, 185));
                        showBadge($"↔ Cột {colIndex + 1}: {initialColW:F0} px", Canvas.GetLeft(colHandle) + 7, 0);
                    };

                    Action<Point> processColDrag = (pt) =>
                    {
                        if (!isDraggingCol) return;
                        double deltaX = pt.X - startColPt.X;
                        double minW = tableData.Columns[colIndex].MinWidth;
                        double newW = Math.Max(minW, initialColW + deltaX);

                        // Giới hạn không vượt quá mép phải canvas
                        double leftOnCanvas = Canvas.GetLeft(finalContainer);
                        if (double.IsNaN(leftOnCanvas)) leftOnCanvas = 0;
                        double otherColsW = tableData.GetTotalWidth() - tableData.Columns[colIndex].Width;
                        double maxAllowedW = BoundsHelper.VirtualBoardBounds.Right - leftOnCanvas - otherColsW - (borderThick * 2) - 20;
                        if (maxAllowedW > minW) newW = Math.Min(newW, maxAllowedW);

                        tableData.Columns[colIndex].Width = newW;
                        if (tableGrid.ColumnDefinitions.Count > colIndex)
                        {
                            tableGrid.ColumnDefinitions[colIndex].Width = new GridLength(newW);
                        }

                        double updatedTotalW = Math.Max(tableData.GetTotalWidth() + (borderThick * 2), 280);
                        finalContainer.Width = updatedTotalW;

                        // Đồng bộ tức thời vị trí các vạch chia cột khác
                        double curX = borderThick;
                        for (int i = 0; i < tableData.ColumnCount; i++)
                        {
                            curX += tableData.Columns[i].Width;
                            var handle = resizerCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag is int idx && idx == i && b.Cursor == Cursors.SizeWE);
                            if (handle != null)
                            {
                                Canvas.SetLeft(handle, curX - 7);
                            }
                        }
                        var grip = resizerCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Cursor == Cursors.SizeNWSE);
                        if (grip != null)
                        {
                            Canvas.SetLeft(grip, (tableData.GetTotalWidth() + borderThick * 2) - 20);
                        }
                        foreach (var rh in resizerCanvas.Children.OfType<Border>().Where(b => b.Cursor == Cursors.SizeNS))
                        {
                            rh.Width = Math.Max(tableData.GetTotalWidth() + (borderThick * 2), 280);
                        }

                        showBadge($"↔ Cột {colIndex + 1}: {newW:F0} px", Canvas.GetLeft(colHandle) + 7, 0);
                        _selectionBox?.UpdatePosition();
                    };

                    Action endColDrag = () =>
                    {
                        if (isDraggingCol)
                        {
                            isDraggingCol = false;
                            guideLine.Opacity = 0.0;
                            guideLine.Width = 2;
                            guideLine.Background = new SolidColorBrush(Color.FromRgb(52, 152, 219));
                            hideBadge();
                            RefreshTableResizers(finalContainer);
                            _selectionBox?.UpdatePosition();
                        }
                    };

                    // Col Mouse Events
                    colHandle.MouseLeftButtonDown += (s, e) =>
                    {
                        startColDrag(e.GetPosition(MainInteractiveBoard));
                        colHandle.CaptureMouse();
                        e.Handled = true;
                    };

                    colHandle.MouseMove += (s, e) =>
                    {
                        if (isDraggingCol)
                        {
                            processColDrag(e.GetPosition(MainInteractiveBoard));
                            e.Handled = true;
                        }
                    };

                    colHandle.MouseLeftButtonUp += (s, e) =>
                    {
                        endColDrag();
                        if (colHandle.IsMouseCaptured) colHandle.ReleaseMouseCapture();
                        e.Handled = true;
                    };

                    colHandle.LostMouseCapture += (s, e) => endColDrag();

                    // Col Touch Events
                    colHandle.PreviewTouchDown += (s, e) =>
                    {
                        if (isDraggingCol) return;
                        startColDrag(e.GetTouchPoint(MainInteractiveBoard).Position);
                        colHandle.CaptureTouch(e.TouchDevice);
                        e.Handled = true;
                    };

                    colHandle.TouchMove += (s, e) =>
                    {
                        if (isDraggingCol)
                        {
                            processColDrag(e.GetTouchPoint(MainInteractiveBoard).Position);
                            e.Handled = true;
                        }
                    };

                    colHandle.TouchUp += (s, e) =>
                    {
                        endColDrag();
                        if (e.TouchDevice.Captured == colHandle) colHandle.ReleaseTouchCapture(e.TouchDevice);
                        e.Handled = true;
                    };

                    colHandle.LostTouchCapture += (s, e) => endColDrag();

                    resizerCanvas.Children.Add(colHandle);
                    Panel.SetZIndex(colHandle, 10);
                }

                // 3. TẠO CÁC ĐƯỜNG PHÂN CÁCH HÀNG (HORIZONTAL ROW DIVIDERS)
                // Cho tất cả các hàng r từ 0 đến RowCount - 1 (vạch biên dưới của hàng r)
                double currentY = borderThick;
                for (int r = 0; r < tableData.RowCount; r++)
                {
                    int rowIndex = r;
                    double rowH = tableData.Rows[rowIndex].Height;
                    currentY += rowH;
                    double dividerY = currentY;

                    var rowHandle = new Border
                    {
                        Height = 14, // Vùng chạm 14px
                        Width = Math.Max(totalTableW, 280),
                        Background = Brushes.Transparent,
                        Cursor = Cursors.SizeNS,
                        Tag = rowIndex,
                        ToolTip = $"Kéo để chỉnh chiều cao Hàng {rowIndex + 1}"
                    };

                    var guideLine = new Border
                    {
                        Height = 2,
                        Background = new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Opacity = 0.0
                    };
                    rowHandle.Child = guideLine;

                    Canvas.SetLeft(rowHandle, 0);
                    Canvas.SetTop(rowHandle, dividerY - 7);

                    rowHandle.MouseEnter += (s, e) => guideLine.Opacity = 0.85;
                    rowHandle.MouseLeave += (s, e) =>
                    {
                        if (!rowHandle.IsMouseCaptured) guideLine.Opacity = 0.0;
                    };

                    // Drag to Resize Row
                    bool isDraggingRow = false;
                    Point startRowPt = new Point();
                    double initialRowH = 0;

                    Action<Point> startRowDrag = (pt) =>
                    {
                        isDraggingRow = true;
                        startRowPt = pt;
                        initialRowH = tableData.Rows[rowIndex].Height;
                        guideLine.Opacity = 1.0;
                        guideLine.Height = 3;
                        guideLine.Background = new SolidColorBrush(Color.FromRgb(41, 128, 185));
                        showBadge($"↕ Hàng {rowIndex + 1}: {initialRowH:F0} px", 0, Canvas.GetTop(rowHandle) + 7);
                    };

                    Action<Point> processRowDrag = (pt) =>
                    {
                        if (!isDraggingRow) return;
                        double deltaY = pt.Y - startRowPt.Y;
                        double minH = tableData.Rows[rowIndex].MinHeight;
                        double newH = Math.Max(minH, initialRowH + deltaY);

                        // Giới hạn không vượt quá mép dưới canvas
                        double topOnCanvas = Canvas.GetTop(finalContainer);
                        if (double.IsNaN(topOnCanvas)) topOnCanvas = 0;
                        double otherRowsH = tableData.GetTotalHeight() - tableData.Rows[rowIndex].Height;
                        double maxAllowedH = BoundsHelper.VirtualBoardBounds.Bottom - topOnCanvas - otherRowsH - (borderThick * 2) - 34 - 20;
                        if (maxAllowedH > minH) newH = Math.Min(newH, maxAllowedH);

                        tableData.Rows[rowIndex].Height = newH;
                        if (tableGrid.RowDefinitions.Count > rowIndex)
                        {
                            tableGrid.RowDefinitions[rowIndex].Height = new GridLength(newH);
                        }

                        double updatedTotalH = tableData.GetTotalHeight() + (borderThick * 2) + 34;
                        finalContainer.Height = updatedTotalH;

                        // Đồng bộ tức thời vị trí các vạch chia hàng khác
                        double curY = borderThick;
                        for (int j = 0; j < tableData.RowCount; j++)
                        {
                            curY += tableData.Rows[j].Height;
                            var handle = resizerCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag is int idx && idx == j && b.Cursor == Cursors.SizeNS);
                            if (handle != null)
                            {
                                Canvas.SetTop(handle, curY - 7);
                            }
                        }
                        var grip = resizerCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Cursor == Cursors.SizeNWSE);
                        if (grip != null)
                        {
                            Canvas.SetTop(grip, (tableData.GetTotalHeight() + borderThick * 2) - 20);
                        }
                        foreach (var ch in resizerCanvas.Children.OfType<Border>().Where(b => b.Cursor == Cursors.SizeWE))
                        {
                            ch.Height = Math.Max(tableData.GetTotalHeight() + (borderThick * 2), 40);
                        }

                        showBadge($"↕ Hàng {rowIndex + 1}: {newH:F0} px", 0, Canvas.GetTop(rowHandle) + 7);
                        _selectionBox?.UpdatePosition();
                    };

                    Action endRowDrag = () =>
                    {
                        if (isDraggingRow)
                        {
                            isDraggingRow = false;
                            guideLine.Opacity = 0.0;
                            guideLine.Height = 2;
                            guideLine.Background = new SolidColorBrush(Color.FromRgb(52, 152, 219));
                            hideBadge();
                            RefreshTableResizers(finalContainer);
                            _selectionBox?.UpdatePosition();
                        }
                    };

                    // Row Mouse Events
                    rowHandle.MouseLeftButtonDown += (s, e) =>
                    {
                        startRowDrag(e.GetPosition(MainInteractiveBoard));
                        rowHandle.CaptureMouse();
                        e.Handled = true;
                    };

                    rowHandle.MouseMove += (s, e) =>
                    {
                        if (isDraggingRow)
                        {
                            processRowDrag(e.GetPosition(MainInteractiveBoard));
                            e.Handled = true;
                        }
                    };

                    rowHandle.MouseLeftButtonUp += (s, e) =>
                    {
                        endRowDrag();
                        if (rowHandle.IsMouseCaptured) rowHandle.ReleaseMouseCapture();
                        e.Handled = true;
                    };

                    rowHandle.LostMouseCapture += (s, e) => endRowDrag();

                    // Row Touch Events
                    rowHandle.PreviewTouchDown += (s, e) =>
                    {
                        startRowDrag(e.GetTouchPoint(MainInteractiveBoard).Position);
                        rowHandle.CaptureTouch(e.TouchDevice);
                        e.Handled = true;
                    };

                    rowHandle.TouchMove += (s, e) =>
                    {
                        if (isDraggingRow)
                        {
                            processRowDrag(e.GetTouchPoint(MainInteractiveBoard).Position);
                            e.Handled = true;
                        }
                    };

                    rowHandle.TouchUp += (s, e) =>
                    {
                        endRowDrag();
                        if (e.TouchDevice.Captured == rowHandle) rowHandle.ReleaseTouchCapture(e.TouchDevice);
                        e.Handled = true;
                    };

                    rowHandle.LostTouchCapture += (s, e) => endRowDrag();

                    resizerCanvas.Children.Add(rowHandle);
                    Panel.SetZIndex(rowHandle, 10);
                }

                // 4. TẠO TAY NẮM THU PHÓNG TỶ LỆ Ở GÓC DƯỚI BÊN PHẢI (CORNER SCALE GRIP ⤡)
                var cornerGrip = new Border
                {
                    Width = 20,
                    Height = 20,
                    Background = new SolidColorBrush(Color.FromArgb(210, 41, 128, 185)),
                    CornerRadius = new CornerRadius(5, 0, 4, 0),
                    Cursor = Cursors.SizeNWSE,
                    ToolTip = "Kéo để thu phóng tỷ lệ toàn bộ bảng"
                };
                var gripIcon = new TextBlock
                {
                    Text = "⤡",
                    Foreground = Brushes.White,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, -2, 0, 0)
                };
                cornerGrip.Child = gripIcon;

                Canvas.SetLeft(cornerGrip, totalTableW - 20);
                Canvas.SetTop(cornerGrip, totalTableH - 20);

                cornerGrip.MouseEnter += (s, e) => cornerGrip.Background = new SolidColorBrush(Color.FromRgb(52, 152, 219));
                cornerGrip.MouseLeave += (s, e) =>
                {
                    if (!cornerGrip.IsMouseCaptured)
                        cornerGrip.Background = new SolidColorBrush(Color.FromArgb(210, 41, 128, 185));
                };

                bool isScalingTable = false;
                Point startGripPt = new Point();
                double[] initColWs = null;
                double[] initRowHs = null;
                double initTotalColW = 0;
                double initTotalRowH = 0;

                Action<Point> startScale = (pt) =>
                {
                    isScalingTable = true;
                    startGripPt = pt;
                    initColWs = tableData.Columns.Select(c => c.Width).ToArray();
                    initRowHs = tableData.Rows.Select(r => r.Height).ToArray();
                    initTotalColW = initColWs.Sum();
                    initTotalRowH = initRowHs.Sum();
                    cornerGrip.Background = new SolidColorBrush(Color.FromRgb(230, 126, 34)); // Warm orange when dragging
                    showBadge($"⤡ {finalContainer.Width:F0} × {finalContainer.Height:F0} px", Canvas.GetLeft(cornerGrip), Canvas.GetTop(cornerGrip));
                };

                Action<Point> processScale = (pt) =>
                {
                    if (!isScalingTable || initColWs == null || initRowHs == null || initTotalColW <= 0 || initTotalRowH <= 0) return;
                    double deltaX = pt.X - startGripPt.X;
                    double deltaY = pt.Y - startGripPt.Y;

                    double targetTotalColW = Math.Max(tableData.ColumnCount * 35, initTotalColW + deltaX);
                    double targetTotalRowH = Math.Max(tableData.RowCount * 25, initTotalRowH + deltaY);

                    // Kiểm tra biên canvas
                    double curL = Canvas.GetLeft(finalContainer);
                    if (double.IsNaN(curL)) curL = 0;
                    double curT = Canvas.GetTop(finalContainer);
                    if (double.IsNaN(curT)) curT = 0;

                    double maxTableW = BoundsHelper.VirtualBoardBounds.Right - curL - 20;
                    double maxTableH = BoundsHelper.VirtualBoardBounds.Bottom - curT - 34 - 20;

                    if (targetTotalColW + (borderThick * 2) > maxTableW && maxTableW > tableData.ColumnCount * 35)
                        targetTotalColW = maxTableW - (borderThick * 2);
                    if (targetTotalRowH + (borderThick * 2) > maxTableH && maxTableH > tableData.RowCount * 25)
                        targetTotalRowH = maxTableH - (borderThick * 2);

                    double scaleX = targetTotalColW / initTotalColW;
                    double scaleY = targetTotalRowH / initTotalRowH;

                    for (int i = 0; i < tableData.ColumnCount; i++)
                    {
                        double nw = Math.Max(initColWs[i] * scaleX, tableData.Columns[i].MinWidth);
                        tableData.Columns[i].Width = nw;
                        if (tableGrid.ColumnDefinitions.Count > i)
                        {
                            tableGrid.ColumnDefinitions[i].Width = new GridLength(nw);
                        }
                    }

                    for (int j = 0; j < tableData.RowCount; j++)
                    {
                        double nh = Math.Max(initRowHs[j] * scaleY, tableData.Rows[j].MinHeight);
                        tableData.Rows[j].Height = nh;
                        if (tableGrid.RowDefinitions.Count > j)
                        {
                            tableGrid.RowDefinitions[j].Height = new GridLength(nh);
                        }
                    }

                    double newContW = Math.Max(tableData.GetTotalWidth() + (borderThick * 2), 280);
                    double newContH = tableData.GetTotalHeight() + (borderThick * 2) + 34;

                    finalContainer.Width = newContW;
                    finalContainer.Height = newContH;

                    // Đồng bộ overlay
                    double curX = borderThick;
                    for (int i = 0; i < tableData.ColumnCount; i++)
                    {
                        curX += tableData.Columns[i].Width;
                        var handle = resizerCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag is int idx && idx == i && b.Cursor == Cursors.SizeWE);
                        if (handle != null)
                        {
                            Canvas.SetLeft(handle, curX - 7);
                            handle.Height = Math.Max(tableData.GetTotalHeight() + (borderThick * 2), 40);
                        }
                    }
                    double curY = borderThick;
                    for (int j = 0; j < tableData.RowCount; j++)
                    {
                        curY += tableData.Rows[j].Height;
                        var handle = resizerCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag is int idx && idx == j && b.Cursor == Cursors.SizeNS);
                        if (handle != null)
                        {
                            Canvas.SetTop(handle, curY - 7);
                            handle.Width = Math.Max(tableData.GetTotalWidth() + (borderThick * 2), 280);
                        }
                    }
                    var grip = resizerCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Cursor == Cursors.SizeNWSE);
                    if (grip != null)
                    {
                        Canvas.SetLeft(grip, (tableData.GetTotalWidth() + borderThick * 2) - 20);
                        Canvas.SetTop(grip, (tableData.GetTotalHeight() + borderThick * 2) - 20);
                    }

                    showBadge($"⤡ {newContW:F0} × {newContH:F0} px", Canvas.GetLeft(cornerGrip), Canvas.GetTop(cornerGrip));
                    _selectionBox?.UpdatePosition();
                };

                Action endScale = () =>
                {
                    if (isScalingTable)
                    {
                        isScalingTable = false;
                        cornerGrip.Background = new SolidColorBrush(Color.FromArgb(210, 41, 128, 185));
                        hideBadge();
                        RefreshTableResizers(finalContainer);
                        _selectionBox?.UpdatePosition();
                    }
                };

                cornerGrip.MouseLeftButtonDown += (s, e) =>
                {
                    startScale(e.GetPosition(MainInteractiveBoard));
                    cornerGrip.CaptureMouse();
                    e.Handled = true;
                };

                cornerGrip.MouseMove += (s, e) =>
                {
                    if (isScalingTable)
                    {
                        processScale(e.GetPosition(MainInteractiveBoard));
                        e.Handled = true;
                    }
                };

                cornerGrip.MouseLeftButtonUp += (s, e) =>
                {
                    endScale();
                    if (cornerGrip.IsMouseCaptured) cornerGrip.ReleaseMouseCapture();
                    e.Handled = true;
                };

                cornerGrip.LostMouseCapture += (s, e) => endScale();

                cornerGrip.PreviewTouchDown += (s, e) =>
                {
                    startScale(e.GetTouchPoint(MainInteractiveBoard).Position);
                    cornerGrip.CaptureTouch(e.TouchDevice);
                    e.Handled = true;
                };

                cornerGrip.TouchMove += (s, e) =>
                {
                    if (isScalingTable)
                    {
                        processScale(e.GetTouchPoint(MainInteractiveBoard).Position);
                        e.Handled = true;
                    }
                };

                cornerGrip.TouchUp += (s, e) =>
                {
                    endScale();
                    if (e.TouchDevice.Captured == cornerGrip) cornerGrip.ReleaseTouchCapture(e.TouchDevice);
                    e.Handled = true;
                };

                cornerGrip.LostTouchCapture += (s, e) => endScale();

                resizerCanvas.Children.Add(cornerGrip);
                Panel.SetZIndex(cornerGrip, 20);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ RefreshTableResizers error: {ex.Message}");
            }
        }

        /// <summary>
        /// Chia đều độ rộng tất cả các cột
        /// </summary>
        public void DistributeColumnsEvenly(StackPanel finalContainer)
        {
            try
            {
                var tableBodyGrid = finalContainer.Children.OfType<Grid>().FirstOrDefault();
                var outerBorder = tableBodyGrid?.Children.OfType<Border>().FirstOrDefault();
                var tableGrid = outerBorder?.Child as Grid;
                var tableData = tableGrid?.Tag as Models.TableData;
                if (tableGrid == null || tableData == null || tableData.ColumnCount <= 0) return;

                tableData.DistributeColumnsEvenly();

                for (int c = 0; c < tableData.ColumnCount; c++)
                {
                    if (tableGrid.ColumnDefinitions.Count > c)
                    {
                        tableGrid.ColumnDefinitions[c].Width = new GridLength(tableData.Columns[c].Width);
                    }
                }

                double totalWidth = Math.Max(tableData.GetTotalWidth() + (tableData.BorderThickness * 2), 280);
                finalContainer.Width = totalWidth;

                RefreshTableResizers(finalContainer);
                _selectionBox?.UpdatePosition();
                RefreshSelectableObjects();
                System.Diagnostics.Debug.WriteLine($"✅ Đã chia đều {tableData.ColumnCount} cột: {tableData.Columns[0].Width:F0}px/cột");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ DistributeColumnsEvenly error: {ex.Message}");
            }
        }

        /// <summary>
        /// Chia đều chiều cao tất cả các hàng
        /// </summary>
        public void DistributeRowsEvenly(StackPanel finalContainer)
        {
            try
            {
                var tableBodyGrid = finalContainer.Children.OfType<Grid>().FirstOrDefault();
                var outerBorder = tableBodyGrid?.Children.OfType<Border>().FirstOrDefault();
                var tableGrid = outerBorder?.Child as Grid;
                var tableData = tableGrid?.Tag as Models.TableData;
                if (tableGrid == null || tableData == null || tableData.RowCount <= 0) return;

                tableData.DistributeRowsEvenly();

                for (int r = 0; r < tableData.RowCount; r++)
                {
                    if (tableGrid.RowDefinitions.Count > r)
                    {
                        tableGrid.RowDefinitions[r].Height = new GridLength(tableData.Rows[r].Height);
                    }
                }

                double totalHeight = tableData.GetTotalHeight() + (tableData.BorderThickness * 2) + 34;
                finalContainer.Height = totalHeight;

                RefreshTableResizers(finalContainer);
                _selectionBox?.UpdatePosition();
                RefreshSelectableObjects();
                System.Diagnostics.Debug.WriteLine($"✅ Đã chia đều {tableData.RowCount} hàng: {tableData.Rows[0].Height:F0}px/hàng");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ DistributeRowsEvenly error: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Kích hoạt chế độ kéo thả vẽ khung bảng trực tiếp trên Canvas
        /// </summary>
        public void EnableTableDrawingMode()
        {
            DeactivateAllTools();
            _isTableDrawingMode = true;
            _activeShapeTouchId = -1;
            _drawingEnabled = false;
            _isDrawing = false;
            _currentStroke = null;
            if (_eraserEnabled) DisableEraserMode();
            _shapeDrawingEnabled = false;
            _textToolEnabled = false;
            _selectionToolEnabled = false;
            _objectSelectionMode = false;

            _whiteboardManager?.ToolManager.DeactivateAll();

            if (_touchHandler != null)
            {
                _touchHandler.SetToolMode(QASmartTouch.Handlers.TouchToolMode.None);
            }

            MainInteractiveBoard.Cursor = Cursors.Cross;
            MainScrollViewer.Cursor = Cursors.Cross;

            RegisterRecentTool(
                "insert:TableDraw",
                "Kéo vẽ bảng",
                QASmartTouch.Services.Tools.RecentToolsManager.TableIconPath,
                Color.FromRgb(0x25, 0x63, 0xEB),
                () => EnableTableDrawingMode(),
                "Dùng lại: Kéo vẽ bảng trực tiếp trên trang");
        }

        /// <summary>
        /// Khởi tạo khung xem trước khi kéo vẽ bảng (dùng chung cho cả Mouse, Touch, Stylus)
        /// </summary>
        private void StartTableDrawing(Point startPoint)
        {
            _tableDrawStartPoint = startPoint;
            _isDrawingTable = true;
            _isDrawing = false;
            _currentStroke = null;

            if (_tablePreviewBorder != null && MainInteractiveBoard.Children.Contains(_tablePreviewBorder))
            {
                MainInteractiveBoard.Children.Remove(_tablePreviewBorder);
            }

            _tablePreviewBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235)), // Modern Blue #2563EB
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(Color.FromArgb(28, 37, 99, 235)),
                CornerRadius = new CornerRadius(4),
                IsHitTestVisible = false
            };

            var previewRoot = new Grid();

            var gridLines = new Grid
            {
                Name = "TablePreviewGridLines",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            previewRoot.Children.Add(gridLines);

            var badgeBorder = new Border
            {
                Name = "TablePreviewBadge",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 8, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // Slate 900
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 4, 10, 4),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Opacity = 0.35,
                    BlurRadius = 8,
                    ShadowDepth = 2
                }
            };

            var badgeText = new TextBlock
            {
                Name = "TablePreviewBadgeText",
                Text = "📊 2 hàng x 2 cột",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            badgeBorder.Child = badgeText;
            previewRoot.Children.Add(badgeBorder);

            _tablePreviewBorder.Child = previewRoot;
            _tablePreviewBorder.Tag = new Point(2, 2); // Initial (rows, cols)

            Canvas.SetLeft(_tablePreviewBorder, _tableDrawStartPoint.X);
            Canvas.SetTop(_tablePreviewBorder, _tableDrawStartPoint.Y);
            _tablePreviewBorder.Width = 0;
            _tablePreviewBorder.Height = 0;

            Panel.SetZIndex(_tablePreviewBorder, QASmartTouch.Helpers.ZIndexConstants.SelectionPreview);
            MainInteractiveBoard.Children.Add(_tablePreviewBorder);
        }

        /// <summary>
        /// Cập nhật tọa độ và kích thước xem trước bảng trong khi kéo
        /// </summary>
        private void UpdateTableDrawing(Point currentPoint)
        {
            if (!_isTableDrawingMode || !_isDrawingTable || _tablePreviewBorder == null) return;

            double left = Math.Min(_tableDrawStartPoint.X, currentPoint.X);
            double top = Math.Min(_tableDrawStartPoint.Y, currentPoint.Y);
            double width = Math.Abs(currentPoint.X - _tableDrawStartPoint.X);
            double height = Math.Abs(currentPoint.Y - _tableDrawStartPoint.Y);

            Canvas.SetLeft(_tablePreviewBorder, left);
            Canvas.SetTop(_tablePreviewBorder, top);
            _tablePreviewBorder.Width = width;
            _tablePreviewBorder.Height = height;

            // Calculate dynamic rows & cols based on width & height
            int cols = Math.Max(2, Math.Min(8, (int)Math.Max(2, Math.Round(width / 95.0))));
            int rows = Math.Max(2, Math.Min(8, (int)Math.Max(2, Math.Round((height - 30.0) / 45.0))));

            // Update Grid Lines and Badge Text in real-time
            UpdateTableDrawPreview(rows, cols, width, height);
        }

        /// <summary>
        /// Cập nhật lưới xem trước hàng/cột và nhãn hiển thị số hàng/cột trong khi kéo chuột/tay
        /// </summary>
        private void UpdateTableDrawPreview(int rows, int cols, double width, double height)
        {
            if (_tablePreviewBorder?.Child is Grid previewRoot)
            {
                // 1. Cập nhật nhãn đếm hàng x cột
                var badgeBorder = previewRoot.Children.OfType<Border>()
                    .FirstOrDefault(b => b.Name == "TablePreviewBadge");
                var badgeText = badgeBorder?.Child as TextBlock;

                if (badgeText != null)
                {
                    badgeText.Text = $"📊 {rows} hàng x {cols} cột ({width:F0} × {height:F0})";
                }

                // 2. Chỉ tái tạo đường kẻ nếu số hàng hoặc số cột thay đổi
                if (_tablePreviewBorder.Tag is Point prevSize && (int)prevSize.X == rows && (int)prevSize.Y == cols)
                {
                    return; // Đã khớp số hàng/cột, không cần vẽ lại grid
                }

                _tablePreviewBorder.Tag = new Point(rows, cols);

                var gridLines = previewRoot.Children.OfType<Grid>()
                    .FirstOrDefault(g => g.Name == "TablePreviewGridLines");

                if (gridLines != null)
                {
                    gridLines.RowDefinitions.Clear();
                    gridLines.ColumnDefinitions.Clear();
                    gridLines.Children.Clear();

                    for (int r = 0; r < rows; r++)
                    {
                        gridLines.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    }
                    for (int c = 0; c < cols; c++)
                    {
                        gridLines.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    }

                    var cellBorderBrush = new SolidColorBrush(Color.FromArgb(90, 37, 99, 235));

                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            var cell = new Border
                            {
                                BorderBrush = cellBorderBrush,
                                BorderThickness = new Thickness(0.8),
                                Background = Brushes.Transparent
                            };
                            Grid.SetRow(cell, r);
                            Grid.SetColumn(cell, c);
                            gridLines.Children.Add(cell);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Hoàn tất vẽ bảng sau khi nhả chuột/tay
        /// </summary>
        private void FinalizeTableDrawing()
        {
            _isDrawingTable = false;
            _activeShapeTouchId = -1;
            if (_tablePreviewBorder != null)
            {
                double left = Canvas.GetLeft(_tablePreviewBorder);
                double top = Canvas.GetTop(_tablePreviewBorder);
                double width = _tablePreviewBorder.Width;
                double height = _tablePreviewBorder.Height;

                // Lấy chính xác số hàng và cột từ Tag mà người dùng đã xem trước
                int rows = 3;
                int cols = 3;
                if (_tablePreviewBorder.Tag is Point p && p.X >= 2 && p.Y >= 2)
                {
                    rows = (int)p.X;
                    cols = (int)p.Y;
                }

                MainInteractiveBoard.Children.Remove(_tablePreviewBorder);
                _tablePreviewBorder = null;

                if (double.IsNaN(width) || width < 80) width = 360;
                if (double.IsNaN(height) || height < 60) height = 180;

                var tableData = new Models.TableData(rows, cols);
                double colW = width / cols;
                double rowH = Math.Max(35, (height - 34) / rows);
                foreach (var col in tableData.Columns) col.Width = colW;
                foreach (var row in tableData.Rows) row.Height = rowH;

                InsertTableToCanvas(tableData, new Point(left, top));
                System.Diagnostics.Debug.WriteLine($"✅ Đã tạo bảng từ thao tác kéo vẽ: {rows} hàng x {cols} cột, kích thước {width:F0}x{height:F0}");
            }

            _isTableDrawingMode = false;
            if (MainInteractiveBoard.IsMouseCaptured)
                MainInteractiveBoard.ReleaseMouseCapture();

            MainInteractiveBoard.Cursor = Cursors.Arrow;
            MainScrollViewer.Cursor = Cursors.Arrow;
            EnableDrawingMode(_currentBrushType, _currentPenSize, _currentPenColor);
        }

        private void EnableInlineCellEditing(Border cellBorder, TextBlock textBlock, Models.TableCell cell, Models.TableData tableData)
        {
            try
            {
                var textBox = new TextBox
                {
                    Text = cell.Content,
                    FontFamily = textBlock.FontFamily,
                    FontSize = textBlock.FontSize,
                    FontWeight = textBlock.FontWeight,
                    FontStyle = textBlock.FontStyle,
                    Foreground = textBlock.Foreground,
                    Background = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Padding = new Thickness(2)
                };

                cellBorder.Child = textBox;
                textBox.Focus();
                textBox.SelectAll();

                // Trigger Virtual Keyboard for Teacher
                ShowTouchKeyboard();

                void SaveCellContent()
                {
                    cell.Content = textBox.Text;
                    textBlock.Text = string.IsNullOrEmpty(cell.Content) ? " " : cell.Content;
                    cellBorder.Child = textBlock;
                }

                textBox.LostFocus += (s, e) => SaveCellContent();
                textBox.KeyDown += (s, e) =>
                {
                    if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift))
                    {
                        SaveCellContent();
                        e.Handled = true;
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ EnableInlineCellEditing error: {ex.Message}");
            }
        }

        public static void ShowTouchKeyboard()
        {
            // ✅ QC_4.2_TOUCH_KEYBOARD_FIX: Chuyển sang TouchKeyboardHelper kiểm tra hiển thị thực tế
            // thay vì chặn cứng khi tiến trình TabTip.exe đang chạy ngầm, đảm bảo luôn hiển thị lại ở các lần mở tiếp theo.
            QASmartTouch.Helpers.TouchKeyboardHelper.ShowTouchKeyboard();
        }

        public static void HideTouchKeyboard()
        {
            QASmartTouch.Helpers.TouchKeyboardHelper.HideTouchKeyboard();
        }

        private TextAlignment GetTextAlignment(string alignment)
        {
            return alignment switch
            {
                "Left" => TextAlignment.Left,
                "Center" => TextAlignment.Center,
                "Right" => TextAlignment.Right,
                _ => TextAlignment.Left
            };
        }

        private VerticalAlignment GetVerticalAlignment(string alignment)
        {
            return alignment switch
            {
                "Top" => VerticalAlignment.Top,
                "Center" => VerticalAlignment.Center,
                "Bottom" => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Center
            };
        }

        private void EditTable(Border tableContainer)
        {
            try
            {
                // Extract TableData from Tag
                var grid = tableContainer.Child as Grid;
                var tableData = grid?.Tag as Models.TableData;

                if (tableData == null)
                {
                    MessageBox.Show("Không tìm thấy dữ liệu bảng để chỉnh sửa.", "Thông báo",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Open editor with existing data
                var tableDialog = new Form2_TableEditorDialog(tableData);
                tableDialog.Owner = this;
                bool? result = tableDialog.ShowDialog();

                if (result == true && tableDialog.ResultTableData != null)
                {
                    // ✅ QC_4.2_TABLE_EDIT_FIX: Sửa ép kiểu — parent thực tế là StackPanel (có thể bọc qua tableBodyGrid)
                    var parent = (tableContainer.Parent as StackPanel) ?? (tableContainer.Parent as FrameworkElement)?.Parent as StackPanel;
                    if (parent != null)
                    {
                        // Get position of old table
                        double left = Canvas.GetLeft(parent);
                        double top = Canvas.GetTop(parent);

                        // Record Undo for old table removal
                        RecordRemoveAction(parent, "Table");

                        // Remove old table from Canvas
                        MainInteractiveBoard.Children.Remove(parent);

                        // Re-insert updated table (this will create new visuals)
                        InsertTableToCanvas(tableDialog.ResultTableData);

                        // ✅ QC_4.2_TABLE_EDIT_FIX: Tìm container mới — InsertTableToCanvas tạo StackPanel
                        var newContainer = MainInteractiveBoard.Children.OfType<StackPanel>().LastOrDefault();
                        if (newContainer != null)
                        {
                            Canvas.SetLeft(newContainer, left);
                            Canvas.SetTop(newContainer, top);
                        }

                        // ✅ N21 FIX: Đăng ký lại với SelectionManager
                        RefreshSelectableObjects();

                        System.Diagnostics.Debug.WriteLine("✅ Table updated successfully at position: " + left + ", " + top);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("❌ EditTable: parent is not StackPanel — type = " + tableContainer.Parent?.GetType().Name);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi sửa bảng: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ EditTable error: {ex.Message}");
            }
        }

        private void DeleteTable(FrameworkElement tableContainer)
        {
            try
            {
                var result = MessageBox.Show(
                    "Bạn có chắc chắn muốn xóa Bảng dữ liệu này cùng toàn bộ nội dung/nét vẽ bên trong không?",
                    "Xác nhận xóa Bảng dữ liệu",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    FrameworkElement targetToDelete = tableContainer;
                    if (tableContainer != null && tableContainer.Parent is FrameworkElement parentFE && !(parentFE is Canvas))
                    {
                        targetToDelete = parentFE;
                    }

                    if (targetToDelete != null && MainInteractiveBoard.Children.Contains(targetToDelete))
                    {
                        var attached = GetTableAttachedStrokes(targetToDelete);
                        // ✅ QC_4.2_TABLE_UNDO_FIX (T6): Ghi từng attached element vào Undo stack
                        // TRƯỚC khi xóa, để Ctrl+Z khôi phục cả nét vẽ đính kèm
                        foreach (var elem in attached)
                        {
                            RecordRemoveAction(elem, "TableAttachedStroke");
                        }
                        foreach (var elem in attached)
                        {
                            MainInteractiveBoard.Children.Remove(elem);
                        }

                        _tableAttachedStrokes.Remove(targetToDelete);

                        // Xóa các nét vẽ Native Stroke đính kèm
                        var attachedNative = GetTableAttachedNativeStrokes(targetToDelete);
                        foreach (var s in attachedNative)
                        {
                            RecordRemoveStrokeAction(s, "TableAttachedStroke");
                            MainInkPresenter?.Strokes.Remove(s);
                        }
                        _tableAttachedNativeStrokes.Remove(targetToDelete);

                        MainInteractiveBoard.Children.Remove(targetToDelete);
                        RecordRemoveAction(targetToDelete, "Table");
                        RefreshSelectableObjects();

                        MainInteractiveBoard.ReleaseAllTouchCaptures();
                        MainScrollViewer?.ReleaseAllTouchCaptures();

                        System.Diagnostics.Debug.WriteLine("✅ Table deleted successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ DeleteTable error: {ex.Message}");
            }
        }

        /// <summary>
        /// Insert rich text box to canvas
        /// </summary>
        public void InsertTextBoxToCanvas(Controls.RichTextBoxControl sourceTextBoxControl)
        {
            try
            {
                // Create NEW text box control (can't reuse from dialog due to WPF parent restrictions)
                var newTextBox = new Controls.RichTextBoxControl
                {
                    MinWidth = sourceTextBoxControl.MinWidth,
                    MinHeight = sourceTextBoxControl.MinHeight
                };

                // Copy the FlowDocument content from source to new control
                var sourceRange = new TextRange(
                    sourceTextBoxControl.Document.ContentStart,
                    sourceTextBoxControl.Document.ContentEnd);
                
                var targetRange = new TextRange(
                    newTextBox.Document.ContentStart,
                    newTextBox.Document.ContentEnd);

                using (var stream = new System.IO.MemoryStream())
                {
                    sourceRange.Save(stream, DataFormats.XamlPackage);
                    stream.Seek(0, System.IO.SeekOrigin.Begin);
                    targetRange.Load(stream, DataFormats.XamlPackage);
                }

                // Explicitly clear placeholder flag on the newly populated text box
                newTextBox.ClearPlaceholder();

                // Create container for text box with drag handles
                var container = new Grid
                {
                    Width = newTextBox.MinWidth + 20,
                    Height = newTextBox.MinHeight + 40,
                    // ✅ QC_4.2_TEXTBOX_TAG_FIX (TB-1): Gán Tag để IsSystemElement nhận diện
                    Tag = "TextBoxContainer"
                };

                // Add NEW text box control
                newTextBox.Margin = new Thickness(10, 30, 10, 10);
                newTextBox.GotFocus += (s, e) => ShowTouchKeyboard();
                newTextBox.PreviewMouseLeftButtonDown += (s, e) => ShowTouchKeyboard();
                newTextBox.PreviewTouchDown += (s, e) => ShowTouchKeyboard();
                container.Children.Add(newTextBox);

                // Create drag handle (top bar)
                var dragHandle = new Border
                {
                    Height = 30,
                    Background = new SolidColorBrush(Color.FromRgb(46, 134, 222)),
                    VerticalAlignment = VerticalAlignment.Top,
                    Cursor = Cursors.SizeAll,
                    // ✅ QC_4.2_TEXTBOX_TAG_FIX (TB-1): Gán Tag để IsDragHandleOrEmbeddedWidget nhận diện
                    Tag = "DragHandle"
                };

                var dragHandleText = new TextBlock
                {
                    Text = "📝 Hộp văn bản - Kéo để di chuyển",
                    Foreground = Brushes.White,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                dragHandle.Child = dragHandleText;
                container.Children.Add(dragHandle);

                // Add delete button
                var btnDelete = new Button
                {
                    Content = "❌",
                    Width = 28,
                    Height = 28,
                    Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    FontSize = 14,
                    Cursor = Cursors.Hand,
                    ToolTip = "Xóa hộp văn bản",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 1, 1, 0)
                };
                btnDelete.Click += (s, e) => { e.Handled = true; Dispatcher.BeginInvoke(new Action(() => DeleteTextBox(container))); };
                // ✅ QC_4.2_TEXTBOX_TOUCHUP_FIX (TB-2): Dùng Dispatcher.BeginInvoke để tránh kẹt TouchCapture khi bật MessageBox
                btnDelete.PreviewTouchUp += (s, e) => { e.Handled = true; Dispatcher.BeginInvoke(new Action(() => DeleteTextBox(container))); };
                container.Children.Add(btnDelete);

                // Enable dragging (Touch & Mouse strictly isolated)
                bool isDragging = false;
                int? activeTextTouchId = null;
                Point dragStartPoint = new Point();

                Action endMove = () =>
                {
                    if (isDragging)
                    {
                        isDragging = false;
                        RefreshSelectableObjects();
                    }
                };

                Action<Point> startMove = (startPt) =>
                {
                    _isDrawing = false;
                    _currentStroke = null;
                    isDragging = true;
                    dragStartPoint = startPt;
                };

                dragHandle.MouseLeftButtonDown += (s, e) =>
                {
                    if (activeTextTouchId.HasValue || e.StylusDevice != null) return;
                    startMove(e.GetPosition(MainInteractiveBoard));
                    dragHandle.CaptureMouse();
                    e.Handled = true;
                };

                dragHandle.MouseMove += (s, e) =>
                {
                    if (isDragging && !activeTextTouchId.HasValue && e.StylusDevice == null)
                    {
                        Point currentPoint = e.GetPosition(MainInteractiveBoard);
                        double oldLeft = Canvas.GetLeft(container);
                        double oldTop = Canvas.GetTop(container);

                        double newLeft = oldLeft + (currentPoint.X - dragStartPoint.X);
                        double newTop = oldTop + (currentPoint.Y - dragStartPoint.Y);

                        // Keep within canvas bounds
                        newLeft = Math.Max(BoundsHelper.VirtualBoardBounds.Left, Math.Min(newLeft, BoundsHelper.VirtualBoardBounds.Right - container.Width));
                        newTop = Math.Max(BoundsHelper.VirtualBoardBounds.Top, Math.Min(newTop, BoundsHelper.VirtualBoardBounds.Bottom - container.Height));

                        Canvas.SetLeft(container, newLeft);
                        Canvas.SetTop(container, newTop);

                        dragStartPoint = currentPoint;
                        e.Handled = true; // ✅ Chặn event bubble khi đang kéo
                    }
                };

                dragHandle.MouseLeftButtonUp += (s, e) =>
                {
                    if (activeTextTouchId.HasValue || e.StylusDevice != null) return;
                    // ✅ QC_4.2_CAPTURE_FIX: Luôn giải phóng capture bất kể isDragging
                    endMove();
                    if (dragHandle.IsMouseCaptured)
                        dragHandle.ReleaseMouseCapture();
                    e.Handled = true;
                };

                // ✅ QC_4.2: Xử lý mất mouse capture bất ngờ (Alt+Tab, OS dialog)
                dragHandle.LostMouseCapture += (s, e) =>
                {
                    if (!activeTextTouchId.HasValue)
                        endMove();
                };

                // Add Touch support for kiosk touch displays
                dragHandle.PreviewTouchDown += (s, e) =>
                {
                    if (activeTextTouchId.HasValue && activeTextTouchId.Value != e.TouchDevice.Id) return;
                    activeTextTouchId = e.TouchDevice.Id;
                    startMove(e.GetTouchPoint(MainInteractiveBoard).Position);
                    dragHandle.CaptureTouch(e.TouchDevice);
                    e.Handled = true;
                };

                dragHandle.PreviewTouchMove += (s, e) =>
                {
                    if (isDragging && activeTextTouchId == e.TouchDevice.Id)
                    {
                        Point currentPoint = e.GetTouchPoint(MainInteractiveBoard).Position;
                        double oldLeft = Canvas.GetLeft(container);
                        double oldTop = Canvas.GetTop(container);

                        double newLeft = oldLeft + (currentPoint.X - dragStartPoint.X);
                        double newTop = oldTop + (currentPoint.Y - dragStartPoint.Y);

                        newLeft = Math.Max(BoundsHelper.VirtualBoardBounds.Left, Math.Min(newLeft, BoundsHelper.VirtualBoardBounds.Right - container.Width));
                        newTop = Math.Max(BoundsHelper.VirtualBoardBounds.Top, Math.Min(newTop, BoundsHelper.VirtualBoardBounds.Bottom - container.Height));

                        Canvas.SetLeft(container, newLeft);
                        Canvas.SetTop(container, newTop);

                        dragStartPoint = currentPoint;
                        e.Handled = true;
                    }
                };

                dragHandle.PreviewTouchUp += (s, e) =>
                {
                    if (activeTextTouchId == e.TouchDevice.Id)
                    {
                        activeTextTouchId = null;
                        // ✅ QC_4.2_CAPTURE_FIX: Luôn giải phóng touch capture bất kể isDragging
                        endMove();
                        if (e.TouchDevice.Captured == dragHandle)
                            dragHandle.ReleaseTouchCapture(e.TouchDevice);
                        e.Handled = true;
                    }
                };

                // ✅ QC_4.2: Xử lý mất touch capture bất ngờ
                dragHandle.LostTouchCapture += (s, e) =>
                {
                    if (activeTextTouchId == e.TouchDevice.Id)
                    {
                        activeTextTouchId = null;
                        endMove();
                    }
                };

                // Position at center of canvas
                double left = (MainInteractiveBoard.ActualWidth - container.Width) / 2;
                double top = (MainInteractiveBoard.ActualHeight - container.Height) / 2;

                Canvas.SetLeft(container, left);
                Canvas.SetTop(container, top);

                // Add to canvas
                MainInteractiveBoard.Children.Add(container);
                // ✅ QC_4.2_TEXTBOX_ZINDEX (TB-3): Đặt Z-Index TRÊN nét vẽ, DƯỚI Widget
                Panel.SetZIndex(container, QASmartTouch.Helpers.ZIndexConstants.TableContainer);

                // Add to undo stack
                RecordAddAction(container, "Text Box");

                // ✅ N20 FIX: Register container with SelectionManager so clicks do not hit-test null and deselect/remove
                RefreshSelectableObjects();

                // ✅ QC_4.2_SMART_TOUCH_TEXTBOX_UNLOCK: Dọn dẹp trạng thái modal lock & gỡ bỏ capture dư thừa
                _selectedTool = null;
                CloseAllSubmenus();
                MainInteractiveBoard.ReleaseAllTouchCaptures();
                MainScrollViewer.ReleaseAllTouchCaptures();

                // Gán focus cho Hộp văn bản và bật bàn phím ảo
                newTextBox.Focus();
                ShowTouchKeyboard();

                System.Diagnostics.Debug.WriteLine($"✅ Text box inserted at ({left:F0}, {top:F0}), size: {container.Width:F0}x{container.Height:F0}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi chèn hộp văn bản: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ InsertTextBoxToCanvas error: {ex.Message}");
            }
        }

        private void DeleteTextBox(Grid textBoxContainer)
        {
            var result = MessageBox.Show("Bạn có chắc muốn xóa hộp văn bản này?", "Xác nhận",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                MainInteractiveBoard.Children.Remove(textBoxContainer);
                RecordRemoveAction(textBoxContainer, "Text Box");
                RefreshSelectableObjects();
                // ✅ QC_4.2_SMART_TOUCH_TEXTBOX_DELETE_FIX: Giải phóng toàn bộ Touch Capture sau khi xóa
                MainInteractiveBoard.ReleaseAllTouchCaptures();
                MainScrollViewer.ReleaseAllTouchCaptures();
                System.Diagnostics.Debug.WriteLine("✅ Text box deleted");
            }
        }

        #endregion
    }
}
