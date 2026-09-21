using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Shapes;
using QASmartTouch.Models;
using QASmartTouch.Controls;
using CanvasControl = System.Windows.Controls.Canvas;
using System.Windows.Media;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Service quản lý việc chọn và thao tác với objects trên canvas
    /// </summary>
    public class SelectionManager
    {
        #region Fields

        private CanvasControl _canvas;
        private SelectionState _state;
        private List<SelectableObject> _allObjects;
        private SelectableObject? _clipboard; // Store copied object (single)
        private List<SelectableObject>? _clipboardList; // ✅ GĐ3-FIX: Store copied group
        private Point _clipboardOrigin; // ✅ GĐ3-FIX: Top-left origin of copied group
        private Dictionary<UIElement, SelectionAdorner> _adorners; // Lasso selection adorners (individual)
        private GroupSelectionAdorner? _groupAdorner; // Group selection adorner (single box for multiple objects)
        private Dictionary<string, SelectableGroup> _groups; // Group management (GroupId -> SelectableGroup)
        private QASmartTouch.Helpers.QuadTree? _quadTree; // QuadTree for spatial indexing

        #endregion

        #region Events

        /// <summary>
        /// Event khi selection thay đổi
        /// </summary>
        public event EventHandler<SelectableObject?>? SelectionChanged;

        /// <summary>
        /// Event khi multi-selection thay đổi
        /// </summary>
        public event EventHandler<List<SelectableObject>>? MultiSelectionChanged;

        #endregion

        #region Constructor

        public SelectionManager(CanvasControl canvas)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _state = new SelectionState();
            _allObjects = new List<SelectableObject>();
            _adorners = new Dictionary<UIElement, SelectionAdorner>();
            _groups = new Dictionary<string, SelectableGroup>();
            _quadTree = new QASmartTouch.Helpers.QuadTree(0, new Rect(0, 0, 10000, 10000));
        }

        #endregion

        #region Properties

        /// <summary>
        /// Trạng thái selection hiện tại
        /// </summary>
        public SelectionState State => _state;

        /// <summary>
        /// Danh sách tất cả objects
        /// </summary>
        public IReadOnlyList<SelectableObject> AllObjects => _allObjects.AsReadOnly();

        /// <summary>
        /// Object đang được chọn (single selection)
        /// </summary>
        public SelectableObject? SelectedObject => _state.PrimarySelectedObject;

        /// <summary>
        /// Danh sách objects đang được chọn (multi selection)
        /// </summary>
        public IReadOnlyList<SelectableObject> SelectedObjects => _state.SelectedObjects.AsReadOnly();

        #endregion

        #region Public Methods - Selection Queries

        /// <summary>
        /// Lấy danh sách tất cả objects đang được chọn
        /// </summary>
        public List<SelectableObject> GetSelectedObjects()
        {
            return _state.SelectedObjects.ToList();
        }

        #endregion

        #region Public Methods - Object Management

        /// <summary>
        /// Thêm object vào danh sách quản lý
        /// </summary>
        public void AddObject(SelectableObject obj)
        {
            if (obj == null)
                return;

            // Reject background elements (grid, background layers, system overlays)
            if (IsBackgroundElement(obj.Element, obj.Bounds, _canvas?.ActualWidth ?? 0, _canvas?.ActualHeight ?? 0))
            {
                System.Diagnostics.Debug.WriteLine($"⏭️ Skipped background element from SelectionManager: {obj.Element?.GetType().Name}");
                return;
            }

            // Check if Element already registered (prevent duplicates)
            if (obj.Element != null && _allObjects.Any(o => o.Element == obj.Element))
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Element already registered, skipping duplicate");
                return;
            }

            _allObjects.Add(obj);
            _quadTree?.Insert(obj);
            
            if (obj.Element != null && !_canvas.Children.Contains(obj.Element))
            {
                _canvas.Children.Add(obj.Element);
                Panel.SetZIndex(obj.Element, obj.ZIndex);
            }
        }

        /// <summary>
        /// Remove object khỏi danh sách
        /// </summary>
        public void RemoveObject(SelectableObject obj)
        {
            if (obj == null)
                return;

            _allObjects.Remove(obj);
            _quadTree?.Remove(obj);
            
            if (obj.Element != null && _canvas.Children.Contains(obj.Element))
            {
                _canvas.Children.Remove(obj.Element);
            }

            if (obj.IsSelected)
            {
                _state.RemoveFromSelection(obj);
            }
        }

        /// <summary>
        /// Xóa đối tượng quản lý logic dựa trên UIElement tương ứng (dành cho Undo/Redo)
        /// </summary>
        public void RemoveObjectByElement(UIElement element)
        {
            if (element == null) return;
            var obj = _allObjects.Find(o => o.Element == element);
            if (obj != null)
            {
                RemoveObject(obj);
            }
            else
            {
                // Fallback nếu không có đối tượng logic
                if (_canvas.Children.Contains(element))
                {
                    _canvas.Children.Remove(element);
                }
            }
        }

        /// <summary>
        /// Xóa tất cả objects
        /// </summary>
        /// <summary>
        /// Clear only internal registration metadata without removing Canvas children.
        /// Used by RefreshSelectableObjects to prevent stale entries.
        /// </summary>
        public void ClearRegistrations()
        {
            _allObjects.Clear();
            _quadTree?.Clear();
        }

        public void ClearAllObjects()
        {
            foreach (var obj in _allObjects.ToList())
            {
                RemoveObject(obj);
            }
            _allObjects.Clear();
            _quadTree?.Clear();
            _state.ClearSelection();
        }

        /// <summary>
        /// Get all registered objects (for debugging)
        /// </summary>
        public List<SelectableObject> GetAllObjects()
        {
            return new List<SelectableObject>(_allObjects);
        }

        #endregion

        #region Public Methods - Selection

        /// <summary>
        /// Xử lý click chuột - tìm và chọn object
        /// </summary>
        public void HandleMouseDown(Point clickPoint, bool isCtrlPressed = false)
        {
            System.Diagnostics.Debug.WriteLine($"🖱️ HandleMouseDown: Click at ({clickPoint.X:F0}, {clickPoint.Y:F0}), Ctrl={isCtrlPressed}");
            System.Diagnostics.Debug.WriteLine($"   Total objects: {_allObjects.Count}");
            
            // Hit test để tìm object tại vị trí click
            SelectableObject? hitObject = HitTest(clickPoint);

            if (hitObject != null && !hitObject.IsLocked)
            {
                System.Diagnostics.Debug.WriteLine($"✅ HitTest found: {hitObject.Type} at Bounds=({hitObject.Bounds.Left:F0},{hitObject.Bounds.Top:F0},{hitObject.Bounds.Width:F0},{hitObject.Bounds.Height:F0})");
                
                if (isCtrlPressed)
                {
                    // Multi-selection: Toggle selection
                    _state.ToggleSelection(hitObject);
                    MultiSelectionChanged?.Invoke(this, _state.SelectedObjects);
                }
                else
                {
                    // Single selection
                    SelectObject(hitObject);
                }
            }
            else
            {
                // Click vào vùng trống - deselect ngay (trừ khi giữ Ctrl)
                if (!isCtrlPressed)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ HitTest found NOTHING - deselecting all");
                    DeselectAll();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Click on empty space with Ctrl - keeping selection");
                }
            }
        }

        /// <summary>
        /// Toggle selection status của một object (dùng cho Ctrl+Click)
        /// </summary>
        public void ToggleSelection(SelectableObject obj)
        {
            if (obj == null) return;
            _state.ToggleSelection(obj);
            if (_state.SelectedObjects.Count > 1)
            {
                ShowMultiSelectionAdorners();
                MultiSelectionChanged?.Invoke(this, _state.SelectedObjects);
            }
            else if (_state.SelectedObjects.Count == 1)
            {
                SelectObject(_state.SelectedObjects[0]);
            }
            else
            {
                DeselectAll();
            }
        }

        /// <summary>
        /// Chọn một object
        /// Nếu object thuộc group → chọn cả group
        /// </summary>
        public void SelectObject(SelectableObject obj)
        {
            if (obj == null || obj.IsLocked)
                return;

            // ✅ CRITICAL FIX: Ẩn tất cả Group/Individual Adorners cũ trước khi chuyển sang chọn 1 đối tượng đơn lẻ
            HideAllAdorners();

            // Kiểm tra xem object có thuộc group không
            if (!string.IsNullOrEmpty(obj.GroupId))
            {
                var group = GetGroup(obj.GroupId);
                if (group != null && group.Count > 1)
                {
                    // Chọn toàn bộ group
                    SelectMultiple(group.Members.ToList());
                    System.Diagnostics.Debug.WriteLine($"✅ Selected group: {obj.GroupId} ({group.Count} objects)");
                    return;
                }
            }

            // Nếu không thuộc group hoặc group chỉ có 1 member → chọn bình thường
            _state.SelectSingle(obj);

            // Trigger event
            SelectionChanged?.Invoke(this, obj);
        }

        /// <summary>
        /// Bỏ chọn tất cả
        /// </summary>
        public void DeselectAll()
        {
            if (!_state.HasSelection)
                return;

            // Ẩn adorners trước khi clear selection
            HideAllAdorners();

            _state.ClearSelection();
            SelectionChanged?.Invoke(this, null);
        }

        /// <summary>
        /// Thêm object vào selection (multi-select)
        /// </summary>
        public void AddToSelection(SelectableObject obj)
        {
            if (obj == null || obj.IsLocked)
                return;

            _state.AddToSelection(obj);
            MultiSelectionChanged?.Invoke(this, _state.SelectedObjects);
        }

        /// <summary>
        /// Remove object khỏi selection
        /// </summary>
        public void RemoveFromSelection(SelectableObject obj)
        {
            _state.RemoveFromSelection(obj);
            MultiSelectionChanged?.Invoke(this, _state.SelectedObjects);
        }

        /// <summary>
        /// NG-1 Fix: Get all objects inside a rectangle
        /// </summary>
        public List<SelectableObject> GetObjectsInRect(Rect rect)
        {
            var objectsInRect = new List<SelectableObject>();

            // ✅ Cập nhật bounds mới nhất cho toàn bộ objects trước khi kiểm tra vùng chọn
            foreach (var obj in _allObjects)
            {
                if (obj.IsLocked)
                    continue;

                obj.UpdateBounds();

                // Kiểm tra nếu đối tượng nằm trong hoặc giao nhau với hình chữ nhật khoanh chọn
                if (!obj.Bounds.IsEmpty && rect.IntersectsWith(obj.Bounds))
                {
                    objectsInRect.Add(obj);
                }
            }

            return objectsInRect;
        }

        /// <summary>
        /// NG-1 Fix: Select multiple objects at once
        /// </summary>
        public void SelectMultiple(List<SelectableObject> objects)
        {
            if (objects == null || objects.Count == 0)
                return;

            if (objects.Count == 1)
            {
                SelectObject(objects[0]);
                return;
            }
            
            // Clear current selection & adorners
            HideAllAdorners();
            _state.ClearSelection();
            
            // Add all objects to selection
            foreach (var obj in objects)
            {
                if (!obj.IsLocked)
                {
                    _state.AddToSelection(obj);
                }
            }
            
            // Display adorner / selection box for group
            ShowMultiSelectionAdorners();

            // Trigger event
            MultiSelectionChanged?.Invoke(this, _state.SelectedObjects);
            
            System.Diagnostics.Debug.WriteLine($"✅ Selected {_state.SelectedObjects.Count} object(s)");
        }

        #endregion

        #region Public Methods - Object Operations

        /// <summary>
        /// Copy object đã chọn
        /// </summary>
        public SelectableObject? CopySelectedObject()
        {
            System.Diagnostics.Debug.WriteLine($"🔍 CopySelectedObject called - PrimarySelectedObject: {(_state.PrimarySelectedObject != null ? _state.PrimarySelectedObject.Type.ToString() : "NULL")}");
            
            if (_state.PrimarySelectedObject == null || _state.PrimarySelectedObject.Element == null)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Copy failed: No object selected or Element is null");
                return null;
            }

            // ✅ GĐ3-FIX: Copy tất cả objects trong multi-selection
            if (_state.SelectedObjects.Count > 1)
            {
                _clipboardList = _state.SelectedObjects.ToList();
                
                // Tính origin (top-left của group bounds)
                double minX = double.MaxValue, minY = double.MaxValue;
                foreach (var obj in _clipboardList)
                {
                    if (obj.Element == null) continue;
                    minX = Math.Min(minX, obj.Position.X);
                    minY = Math.Min(minY, obj.Position.Y);
                }
                _clipboardOrigin = new Point(minX, minY);
                
                _clipboard = _state.PrimarySelectedObject; // backward compat
                System.Diagnostics.Debug.WriteLine(
                    $"✅ Copied {_clipboardList.Count} objects to clipboard (origin: {_clipboardOrigin})");
                return _clipboard;
            }

            // Single object — giữ nguyên logic cũ
            var original = _state.PrimarySelectedObject;
            _clipboard = original;
            _clipboardList = null; // Clear group clipboard
            
            System.Diagnostics.Debug.WriteLine($"✅ Copied to clipboard: {original.Type} (Element: {original.Element?.GetType().Name}) - Click to place");
            
            return _clipboard;
        }

        /// <summary>
        /// QC_4.2_POSITION_GUARD: Định vị chính xác cho phần tử được nhân bản/dán.
        /// - Đối với các hình học dạng vector/điểm (Polyline, Polygon, Line, Path):
        ///   Các tọa độ điểm (Points, X1/Y1/X2/Y2) hoặc TranslateTransform đã được cộng sẵn độ lệch (offset) mới trong CloneSingleElement.
        ///   Do đó BẮT BUỘC xóa Canvas.Left và Canvas.Top (set về double.NaN) để tránh WPF cộng dồn vị trí 2 lần làm hình bay ra góc xa màn hình.
        /// - Đối với các phần tử dạng hộp (Rectangle, Ellipse, TextBlock, Border, Image, Canvas 3D):
        ///   Định vị chính xác bằng CanvasControl.SetLeft/SetTop theo newPos.
        /// </summary>
        private void PositionClonedElement(UIElement clonedElement, Point newPos)
        {
            if (clonedElement is Polyline || clonedElement is Polygon || 
                clonedElement is System.Windows.Shapes.Line || clonedElement is System.Windows.Shapes.Path)
            {
                CanvasControl.SetLeft(clonedElement, double.NaN);
                CanvasControl.SetTop(clonedElement, double.NaN);
            }
            else if (clonedElement is FrameworkElement fe)
            {
                CanvasControl.SetLeft(fe, newPos.X);
                CanvasControl.SetTop(fe, newPos.Y);
            }
        }

        /// <summary>
        /// QC_4.2_INSTANT_DUPLICATE: Nhân bản tức thì các đối tượng đang được chọn (đơn hoặc đa đối tượng)
        /// Tạo bản sao với độ lệch (offsetX, offsetY), thêm vào Canvas, tự động chọn bản sao và trả về danh sách đối tượng mới.
        /// </summary>
        public List<SelectableObject> DuplicateSelectedObjects(double offsetX = 30.0, double offsetY = 30.0)
        {
            var duplicatedObjects = new List<SelectableObject>();
            var targets = GetSelectedObjects();
            if (targets.Count == 0 && _state.PrimarySelectedObject != null)
            {
                targets = new List<SelectableObject> { _state.PrimarySelectedObject };
            }

            if (targets.Count == 0)
                return duplicatedObjects;

            double canvasWidth = _canvas?.ActualWidth > 0 ? _canvas.ActualWidth : 1920;
            double canvasHeight = _canvas?.ActualHeight > 0 ? _canvas.ActualHeight : 1080;

            foreach (var original in targets)
            {
                if (original.Element == null) continue;

                var newPos = new Point(original.Position.X + offsetX, original.Position.Y + offsetY);
                if (newPos.X + original.Size.Width > canvasWidth) newPos.X = Math.Max(10, original.Position.X - offsetX);
                if (newPos.Y + original.Size.Height > canvasHeight) newPos.Y = Math.Max(10, original.Position.Y - offsetY);

                UIElement? clonedElement = CloneSingleElement(original, newPos);
                if (clonedElement == null) continue;

                var clone = original.Clone();
                clone.Element = clonedElement;
                clone.Position = newPos;

                PositionClonedElement(clonedElement, newPos);

                if (clonedElement is Polyline pastedPolyline && pastedPolyline.Points.Count > 0)
                {
                    double pMinX = double.MaxValue, pMinY = double.MaxValue;
                    double pMaxX = double.MinValue, pMaxY = double.MinValue;
                    foreach (var point in pastedPolyline.Points)
                    {
                        pMinX = Math.Min(pMinX, point.X); pMinY = Math.Min(pMinY, point.Y);
                        pMaxX = Math.Max(pMaxX, point.X); pMaxY = Math.Max(pMaxY, point.Y);
                    }
                    double w = pMaxX - pMinX, h = pMaxY - pMinY;
                    double pad = pastedPolyline.StrokeThickness;
                    pMinX -= pad; pMinY -= pad; w += pad * 2; h += pad * 2;
                    if (w < 10) w = 10; if (h < 10) h = 10;
                    clone.Position = new Point(pMinX, pMinY);
                    clone.Size = new Size(w, h);
                    clone.Bounds = new Rect(pMinX, pMinY, w, h);
                }
                else if (clonedElement is Polygon pastedPolygon && pastedPolygon.Points.Count > 0)
                {
                    double pMinX = double.MaxValue, pMinY = double.MaxValue;
                    double pMaxX = double.MinValue, pMaxY = double.MinValue;
                    foreach (var point in pastedPolygon.Points)
                    {
                        pMinX = Math.Min(pMinX, point.X); pMinY = Math.Min(pMinY, point.Y);
                        pMaxX = Math.Max(pMaxX, point.X); pMaxY = Math.Max(pMaxY, point.Y);
                    }
                    double w = pMaxX - pMinX, h = pMaxY - pMinY;
                    double pad = Math.Max(pastedPolygon.StrokeThickness / 2.0, 2.0);
                    pMinX -= pad; pMinY -= pad; w += pad * 2; h += pad * 2;
                    if (w < 10) w = 10; if (h < 10) h = 10;
                    clone.Position = new Point(pMinX, pMinY);
                    clone.Size = new Size(w, h);
                    clone.Bounds = new Rect(pMinX, pMinY, w, h);
                }
                else if (clonedElement is System.Windows.Shapes.Line pastedLine)
                {
                    double pMinX = Math.Min(pastedLine.X1, pastedLine.X2);
                    double pMinY = Math.Min(pastedLine.Y1, pastedLine.Y2);
                    double pMaxX = Math.Max(pastedLine.X1, pastedLine.X2);
                    double pMaxY = Math.Max(pastedLine.Y1, pastedLine.Y2);
                    double w = Math.Max(pMaxX - pMinX, 5);
                    double h = Math.Max(pMaxY - pMinY, 5);
                    double pad = Math.Max(pastedLine.StrokeThickness / 2.0, 2.0);
                    pMinX -= pad; pMinY -= pad; w += pad * 2; h += pad * 2;
                    clone.Position = new Point(pMinX, pMinY);
                    clone.Size = new Size(w, h);
                    clone.Bounds = new Rect(pMinX, pMinY, w, h);
                }
                else
                {
                    clone.UpdateBounds();
                }

                int newZ = Panel.GetZIndex(original.Element) + 1;
                clone.ZIndex = newZ;

                AddObject(clone);
                duplicatedObjects.Add(clone);
            }

            if (duplicatedObjects.Count > 1)
            {
                SelectMultiple(duplicatedObjects);
                ShowMultiSelectionAdorners();
            }
            else if (duplicatedObjects.Count == 1)
            {
                SelectObject(duplicatedObjects[0]);
            }

            System.Diagnostics.Debug.WriteLine($"✅ DuplicateSelectedObjects: Successfully duplicated {duplicatedObjects.Count} objects");
            return duplicatedObjects;
        }

        /// <summary>
        /// Paste object from clipboard at specified position (NG-2 Fix: Support all object types)
        /// </summary>
        public SelectableObject? PasteAtPosition(Point position)
        {
            // ✅ GĐ3-FIX: Paste nhóm nếu có _clipboardList
            if (_clipboardList != null && _clipboardList.Count > 1)
            {
                return PasteGroupAtPosition(position);
            }
            
            if (_clipboard == null)
                return null;

            var original = _clipboard;
            
            // Clone the UIElement at new position
            UIElement? clonedElement = null;
            
            // NG-2: Support Polyline (freehand drawing)
            if (original.Element is Polyline polyline)
            {
                var newPolyline = new Polyline
                {
                    Stroke = polyline.Stroke,
                    StrokeThickness = polyline.StrokeThickness,
                    StrokeLineJoin = polyline.StrokeLineJoin,
                    StrokeStartLineCap = polyline.StrokeStartLineCap,
                    StrokeEndLineCap = polyline.StrokeEndLineCap
                };
                
                // Calculate offset from original position to new position
                if (polyline.Points.Count > 0)
                {
                    double offsetX = position.X - polyline.Points[0].X;
                    double offsetY = position.Y - polyline.Points[0].Y;
                    
                    // Copy points with offset to new position
                    foreach (var point in polyline.Points)
                    {
                        newPolyline.Points.Add(new Point(point.X + offsetX, point.Y + offsetY));
                    }
                }
                
                clonedElement = newPolyline;
            }
            // Support Polygon (Triangle, Star, Arrow, Pentagon, etc.)
            else if (original.Element is Polygon polygon)
            {
                var newPolygon = new Polygon
                {
                    Stroke = polygon.Stroke,
                    StrokeThickness = polygon.StrokeThickness,
                    StrokeLineJoin = polygon.StrokeLineJoin,
                    StrokeStartLineCap = polygon.StrokeStartLineCap,
                    StrokeEndLineCap = polygon.StrokeEndLineCap,
                    Fill = polygon.Fill,
                    Opacity = polygon.Opacity
                };

                if (polygon.Points.Count > 0)
                {
                    double offsetX = position.X - polygon.Points[0].X;
                    double offsetY = position.Y - polygon.Points[0].Y;

                    foreach (var point in polygon.Points)
                    {
                        newPolygon.Points.Add(new Point(point.X + offsetX, point.Y + offsetY));
                    }
                }

                clonedElement = newPolygon;
            }
            // NG-2: Support Rectangle (shapes, flowchart)
            else if (original.Element is System.Windows.Shapes.Rectangle rectangle)
            {
                var newRectangle = new System.Windows.Shapes.Rectangle
                {
                    Width = rectangle.Width,
                    Height = rectangle.Height,
                    Fill = rectangle.Fill,
                    Stroke = rectangle.Stroke,
                    StrokeThickness = rectangle.StrokeThickness,
                    RadiusX = rectangle.RadiusX,
                    RadiusY = rectangle.RadiusY,
                    StrokeDashArray = rectangle.StrokeDashArray?.Clone(),
                    Opacity = rectangle.Opacity
                };
                
                clonedElement = newRectangle;
            }
            // NG-2: Support Ellipse (circle, oval shapes)
            else if (original.Element is System.Windows.Shapes.Ellipse ellipse)
            {
                var newEllipse = new System.Windows.Shapes.Ellipse
                {
                    Width = ellipse.Width,
                    Height = ellipse.Height,
                    Fill = ellipse.Fill,
                    Stroke = ellipse.Stroke,
                    StrokeThickness = ellipse.StrokeThickness,
                    StrokeDashArray = ellipse.StrokeDashArray?.Clone(),
                    Opacity = ellipse.Opacity
                };
                
                clonedElement = newEllipse;
            }
            // NG-2: Support Line (straight lines, arrows, dashed lines)
            else if (original.Element is System.Windows.Shapes.Line line)
            {
                double minX = Math.Min(line.X1, line.X2);
                double minY = Math.Min(line.Y1, line.Y2);
                double offsetX = position.X - minX;
                double offsetY = position.Y - minY;
                
                var newLine = new System.Windows.Shapes.Line
                {
                    X1 = line.X1 + offsetX,
                    Y1 = line.Y1 + offsetY,
                    X2 = line.X2 + offsetX,
                    Y2 = line.Y2 + offsetY,
                    Stroke = line.Stroke,
                    StrokeThickness = line.StrokeThickness,
                    StrokeDashArray = line.StrokeDashArray?.Clone(),
                    StrokeStartLineCap = line.StrokeStartLineCap,
                    StrokeEndLineCap = line.StrokeEndLineCap,
                    Opacity = line.Opacity
                };
                
                clonedElement = newLine;
            }
            // NG-2: Support TextBlock (text labels, annotations)
            else if (original.Element is System.Windows.Controls.TextBlock textBlock)
            {
                var newTextBlock = new System.Windows.Controls.TextBlock
                {
                    Text = textBlock.Text,
                    FontFamily = textBlock.FontFamily,
                    FontSize = textBlock.FontSize,
                    FontWeight = textBlock.FontWeight,
                    FontStyle = textBlock.FontStyle,
                    Foreground = textBlock.Foreground,
                    Background = textBlock.Background,
                    TextAlignment = textBlock.TextAlignment,
                    TextWrapping = textBlock.TextWrapping,
                    Width = textBlock.Width,
                    Height = textBlock.Height,
                    Opacity = textBlock.Opacity
                };
                
                clonedElement = newTextBlock;
            }
            // NG-2: Support Border (container with border)
            else if (original.Element is System.Windows.Controls.Border border)
            {
                var newBorder = new System.Windows.Controls.Border
                {
                    Width = border.Width,
                    Height = border.Height,
                    Background = border.Background,
                    BorderBrush = border.BorderBrush,
                    BorderThickness = border.BorderThickness,
                    CornerRadius = border.CornerRadius,
                    Padding = border.Padding,
                    Opacity = border.Opacity
                };
                
                // Clone child if exists
                if (border.Child != null && border.Child is FrameworkElement childElement)
                {
                    // Simple clone for TextBlock child (most common case)
                    if (childElement is System.Windows.Controls.TextBlock childText)
                    {
                        newBorder.Child = new System.Windows.Controls.TextBlock
                        {
                            Text = childText.Text,
                            FontFamily = childText.FontFamily,
                            FontSize = childText.FontSize,
                            FontWeight = childText.FontWeight,
                            Foreground = childText.Foreground,
                            TextAlignment = childText.TextAlignment
                        };
                    }
                }
                
                clonedElement = newBorder;
            }
            // NG-2: Support Image (pictures, icons)
            else if (original.Element is System.Windows.Controls.Image image)
            {
                var newImage = new System.Windows.Controls.Image
                {
                    Width = image.Width,
                    Height = image.Height,
                    Source = image.Source, // Reuse same ImageSource (no need to clone)
                    Stretch = image.Stretch,
                    Opacity = image.Opacity
                };
                
                clonedElement = newImage;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Paste not supported for {original.Element?.GetType().Name ?? "null"}");
                return null;
            }
            
            if (clonedElement == null)
                return null;
            
            // Create SelectableObject for cloned element
            var clone = original.Clone();
            clone.Element = clonedElement;
            clone.Position = position;
            
            PositionClonedElement(clonedElement, position);
            
            // BUGFIX: Calculate bounds properly for Polyline and Polygon
            if (clonedElement is Polyline pastedPolyline && pastedPolyline.Points.Count > 0)
            {
                // Calculate bounds from Points
                double minX = double.MaxValue;
                double minY = double.MaxValue;
                double maxX = double.MinValue;
                double maxY = double.MinValue;

                foreach (var point in pastedPolyline.Points)
                {
                    minX = Math.Min(minX, point.X);
                    minY = Math.Min(minY, point.Y);
                    maxX = Math.Max(maxX, point.X);
                    maxY = Math.Max(maxY, point.Y);
                }

                double width = maxX - minX;
                double height = maxY - minY;
                
                // Add padding for stroke thickness
                double padding = pastedPolyline.StrokeThickness;
                minX -= padding;
                minY -= padding;
                width += padding * 2;
                height += padding * 2;
                
                // Ensure minimum size for selection
                if (width < 10) width = 10;
                if (height < 10) height = 10;

                // Update clone with calculated bounds
                clone.Position = new Point(minX, minY);
                clone.Size = new Size(width, height);
                clone.Bounds = new Rect(minX, minY, width, height);
                
                System.Diagnostics.Debug.WriteLine($"   Calculated Polyline bounds: ({minX:F0},{minY:F0},{width:F0},{height:F0})");
            }
            else if (clonedElement is Polygon pastedPolygon && pastedPolygon.Points.Count > 0)
            {
                // Calculate bounds from Points
                double minX = double.MaxValue;
                double minY = double.MaxValue;
                double maxX = double.MinValue;
                double maxY = double.MinValue;

                foreach (var point in pastedPolygon.Points)
                {
                    minX = Math.Min(minX, point.X);
                    minY = Math.Min(minY, point.Y);
                    maxX = Math.Max(maxX, point.X);
                    maxY = Math.Max(maxY, point.Y);
                }

                double width = maxX - minX;
                double height = maxY - minY;
                
                // Add padding for stroke thickness
                double padding = Math.Max(pastedPolygon.StrokeThickness / 2.0, 2.0);
                minX -= padding;
                minY -= padding;
                width += padding * 2;
                height += padding * 2;
                
                // Ensure minimum size for selection
                if (width < 10) width = 10;
                if (height < 10) height = 10;

                // Update clone with calculated bounds
                clone.Position = new Point(minX, minY);
                clone.Size = new Size(width, height);
                clone.Bounds = new Rect(minX, minY, width, height);
                
                System.Diagnostics.Debug.WriteLine($"   Calculated Polygon bounds: ({minX:F0},{minY:F0},{width:F0},{height:F0})");
            }
            else if (clonedElement is System.Windows.Shapes.Line pastedLine)
            {
                double minX = Math.Min(pastedLine.X1, pastedLine.X2);
                double minY = Math.Min(pastedLine.Y1, pastedLine.Y2);
                double maxX = Math.Max(pastedLine.X1, pastedLine.X2);
                double maxY = Math.Max(pastedLine.Y1, pastedLine.Y2);

                double width = Math.Max(maxX - minX, 5);
                double height = Math.Max(maxY - minY, 5);
                double padding = Math.Max(pastedLine.StrokeThickness / 2.0, 2.0);

                minX -= padding;
                minY -= padding;
                width += padding * 2;
                height += padding * 2;

                clone.Position = new Point(minX, minY);
                clone.Size = new Size(width, height);
                clone.Bounds = new Rect(minX, minY, width, height);

                System.Diagnostics.Debug.WriteLine($"   Calculated Line bounds: ({minX:F0},{minY:F0},{width:F0},{height:F0})");
            }
            else
            {
                // For other element types, update bounds normally
                clone.UpdateBounds();
            }
            
            // Add to canvas and selection manager
            AddObject(clone);
            SelectObject(clone);

            System.Diagnostics.Debug.WriteLine($"✅ Pasted {original.Type} at ({position.X:F0}, {position.Y:F0})");

            return clone;
        }

        /// <summary>
        /// Check if clipboard has content
        /// </summary>
        public bool HasClipboardContent()
        {
            return _clipboard != null || (_clipboardList != null && _clipboardList.Count > 0);
        }

        /// <summary>
        /// ✅ GĐ3-FIX: Paste nhóm objects từ clipboard, giữ khoảng cách tương đối
        /// </summary>
        private SelectableObject? PasteGroupAtPosition(Point position)
        {
            if (_clipboardList == null || _clipboardList.Count == 0) return null;

            // Tính offset từ origin cũ sang position mới
            var offsetX = position.X - _clipboardOrigin.X;
            var offsetY = position.Y - _clipboardOrigin.Y;

            var pastedObjects = new List<SelectableObject>();

            foreach (var original in _clipboardList)
            {
                if (original.Element == null) continue;

                // Tính vị trí mới = vị trí gốc + offset
                var newPos = new Point(
                    original.Position.X + offsetX,
                    original.Position.Y + offsetY);

                // Clone UIElement
                UIElement? clonedElement = CloneSingleElement(original, newPos);
                if (clonedElement == null) continue;

                // Create SelectableObject for cloned element
                var clone = original.Clone();
                clone.Element = clonedElement;
                clone.Position = newPos;

                PositionClonedElement(clonedElement, newPos);

                // Calculate bounds properly for Polyline
                if (clonedElement is Polyline pastedPolyline && pastedPolyline.Points.Count > 0)
                {
                    double pMinX = double.MaxValue, pMinY = double.MaxValue;
                    double pMaxX = double.MinValue, pMaxY = double.MinValue;
                    foreach (var point in pastedPolyline.Points)
                    {
                        pMinX = Math.Min(pMinX, point.X); pMinY = Math.Min(pMinY, point.Y);
                        pMaxX = Math.Max(pMaxX, point.X); pMaxY = Math.Max(pMaxY, point.Y);
                    }
                    double w = pMaxX - pMinX, h = pMaxY - pMinY;
                    double pad = pastedPolyline.StrokeThickness;
                    pMinX -= pad; pMinY -= pad; w += pad * 2; h += pad * 2;
                    if (w < 10) w = 10; if (h < 10) h = 10;
                    clone.Position = new Point(pMinX, pMinY);
                    clone.Size = new Size(w, h);
                    clone.Bounds = new Rect(pMinX, pMinY, w, h);
                }
                else
                {
                    clone.UpdateBounds();
                }

                // Add to canvas
                AddObject(clone);
                pastedObjects.Add(clone);
            }

            // Select tất cả objects vừa paste
            if (pastedObjects.Count > 1)
            {
                SelectMultiple(pastedObjects);
                ShowMultiSelectionAdorners();
            }
            else if (pastedObjects.Count == 1)
            {
                SelectObject(pastedObjects[0]);
            }

            System.Diagnostics.Debug.WriteLine(
                $"✅ Pasted group: {pastedObjects.Count}/{_clipboardList.Count} objects at ({position.X:F0}, {position.Y:F0})");
            return pastedObjects.FirstOrDefault();
        }

        /// <summary>
        /// ✅ GĐ3-FIX: Clone 1 UIElement từ original object tại vị trí mới
        /// Trích xuất từ PasteAtPosition để tái sử dụng cho group paste
        /// </summary>
        private UIElement? CloneSingleElement(SelectableObject original, Point newPosition)
        {
            if (original.Element == null) return null;

            if (original.Element is Polyline polyline)
            {
                var newPolyline = new Polyline
                {
                    Stroke = polyline.Stroke,
                    StrokeThickness = polyline.StrokeThickness,
                    StrokeLineJoin = polyline.StrokeLineJoin,
                    StrokeStartLineCap = polyline.StrokeStartLineCap,
                    StrokeEndLineCap = polyline.StrokeEndLineCap
                };
                if (polyline.Points.Count > 0)
                {
                    double oX = newPosition.X - original.Position.X;
                    double oY = newPosition.Y - original.Position.Y;
                    foreach (var p in polyline.Points)
                    {
                        newPolyline.Points.Add(new Point(p.X + oX, p.Y + oY));
                    }
                }
                return newPolyline;
            }
            else if (original.Element is Polygon polygon)
            {
                var newPolygon = new Polygon
                {
                    Stroke = polygon.Stroke,
                    StrokeThickness = polygon.StrokeThickness,
                    StrokeLineJoin = polygon.StrokeLineJoin,
                    StrokeStartLineCap = polygon.StrokeStartLineCap,
                    StrokeEndLineCap = polygon.StrokeEndLineCap,
                    Fill = polygon.Fill,
                    Opacity = polygon.Opacity
                };
                if (polygon.Points.Count > 0)
                {
                    double oX = newPosition.X - original.Position.X;
                    double oY = newPosition.Y - original.Position.Y;
                    foreach (var p in polygon.Points)
                    {
                        newPolygon.Points.Add(new Point(p.X + oX, p.Y + oY));
                    }
                }
                return newPolygon;
            }
            else if (original.Element is System.Windows.Shapes.Rectangle rectangle)
            {
                return new System.Windows.Shapes.Rectangle
                {
                    Width = rectangle.Width, Height = rectangle.Height,
                    Fill = rectangle.Fill, Stroke = rectangle.Stroke,
                    StrokeThickness = rectangle.StrokeThickness,
                    RadiusX = rectangle.RadiusX, RadiusY = rectangle.RadiusY,
                    StrokeDashArray = rectangle.StrokeDashArray?.Clone(),
                    Opacity = rectangle.Opacity
                };
            }
            else if (original.Element is System.Windows.Shapes.Ellipse ellipse)
            {
                return new System.Windows.Shapes.Ellipse
                {
                    Width = ellipse.Width, Height = ellipse.Height,
                    Fill = ellipse.Fill, Stroke = ellipse.Stroke,
                    StrokeThickness = ellipse.StrokeThickness,
                    StrokeDashArray = ellipse.StrokeDashArray?.Clone(),
                    Opacity = ellipse.Opacity
                };
            }
            else if (original.Element is System.Windows.Shapes.Line line)
            {
                double minX = Math.Min(line.X1, line.X2);
                double minY = Math.Min(line.Y1, line.Y2);
                double oX = newPosition.X - minX;
                double oY = newPosition.Y - minY;

                return new System.Windows.Shapes.Line
                {
                    X1 = line.X1 + oX,
                    Y1 = line.Y1 + oY,
                    X2 = line.X2 + oX,
                    Y2 = line.Y2 + oY,
                    Stroke = line.Stroke,
                    StrokeThickness = line.StrokeThickness,
                    StrokeDashArray = line.StrokeDashArray?.Clone(),
                    StrokeStartLineCap = line.StrokeStartLineCap,
                    StrokeEndLineCap = line.StrokeEndLineCap,
                    Opacity = line.Opacity
                };
            }
            else if (original.Element is System.Windows.Controls.TextBlock textBlock)
            {
                return new System.Windows.Controls.TextBlock
                {
                    Text = textBlock.Text, FontFamily = textBlock.FontFamily,
                    FontSize = textBlock.FontSize, FontWeight = textBlock.FontWeight,
                    FontStyle = textBlock.FontStyle, Foreground = textBlock.Foreground,
                    Background = textBlock.Background,
                    TextAlignment = textBlock.TextAlignment,
                    TextWrapping = textBlock.TextWrapping,
                    Width = textBlock.Width, Height = textBlock.Height,
                    Opacity = textBlock.Opacity
                };
            }
            else if (original.Element is System.Windows.Controls.Border border)
            {
                var newBorder = new System.Windows.Controls.Border
                {
                    Width = border.Width, Height = border.Height,
                    Background = border.Background, BorderBrush = border.BorderBrush,
                    BorderThickness = border.BorderThickness,
                    CornerRadius = border.CornerRadius,
                    Padding = border.Padding, Opacity = border.Opacity
                };
                if (border.Child is System.Windows.Controls.TextBlock childText)
                {
                    newBorder.Child = new System.Windows.Controls.TextBlock
                    {
                        Text = childText.Text, FontFamily = childText.FontFamily,
                        FontSize = childText.FontSize, FontWeight = childText.FontWeight,
                        Foreground = childText.Foreground,
                        TextAlignment = childText.TextAlignment
                    };
                }
                return newBorder;
            }
            else if (original.Element is System.Windows.Shapes.Path path && path.Data != null)
            {
                var newPath = new System.Windows.Shapes.Path
                {
                    Data = path.Data.Clone(),
                    Stroke = path.Stroke?.Clone(),
                    StrokeThickness = path.StrokeThickness,
                    StrokeLineJoin = path.StrokeLineJoin,
                    StrokeStartLineCap = path.StrokeStartLineCap,
                    StrokeEndLineCap = path.StrokeEndLineCap,
                    Fill = path.Fill?.Clone(),
                    Opacity = path.Opacity
                };
                double oX = newPosition.X - original.Position.X;
                double oY = newPosition.Y - original.Position.Y;
                if (Math.Abs(oX) > 0.001 || Math.Abs(oY) > 0.001)
                {
                    var group = new TransformGroup();
                    if (path.RenderTransform != null && path.RenderTransform != Transform.Identity)
                    {
                        group.Children.Add(path.RenderTransform.Clone());
                    }
                    group.Children.Add(new TranslateTransform(oX, oY));
                    newPath.RenderTransform = group;
                }
                return newPath;
            }
            else if (original.Element is System.Windows.Controls.Image image)
            {
                return new System.Windows.Controls.Image
                {
                    Width = image.Width, Height = image.Height,
                    Source = image.Source, Stretch = image.Stretch,
                    Opacity = image.Opacity
                };
            }
            else if (original.Element is CanvasControl canvas)
            {
                // ✅ Hỗ trợ sao chép Hình khối 3D và biểu đồ STEM Canvas
                var newCanvas = new CanvasControl
                {
                    Width = canvas.Width,
                    Height = canvas.Height,
                    Background = canvas.Background?.Clone(),
                    ClipToBounds = canvas.ClipToBounds,
                    Tag = canvas.Tag
                };

                foreach (UIElement child in canvas.Children)
                {
                    if (child is System.Windows.Shapes.Shape shp)
                    {
                        var clonedShp = CloneShapeElement(shp);
                        if (clonedShp != null) newCanvas.Children.Add(clonedShp);
                    }
                    else if (child is System.Windows.Controls.TextBlock tb)
                    {
                        var clonedTb = new System.Windows.Controls.TextBlock
                        {
                            Text = tb.Text, FontSize = tb.FontSize, FontWeight = tb.FontWeight,
                            FontFamily = tb.FontFamily, Foreground = tb.Foreground?.Clone(),
                            Background = tb.Background?.Clone(), TextWrapping = tb.TextWrapping,
                            Width = tb.Width, Height = tb.Height
                        };
                        CanvasControl.SetLeft(clonedTb, CanvasControl.GetLeft(tb));
                        CanvasControl.SetTop(clonedTb, CanvasControl.GetTop(tb));
                        newCanvas.Children.Add(clonedTb);
                    }
                    else if (child is System.Windows.Controls.Border b)
                    {
                        var clonedB = new System.Windows.Controls.Border
                        {
                            Width = b.Width, Height = b.Height, Background = b.Background?.Clone(),
                            BorderBrush = b.BorderBrush?.Clone(), BorderThickness = b.BorderThickness,
                            CornerRadius = b.CornerRadius, Padding = b.Padding, Opacity = b.Opacity
                        };
                        if (b.Child is System.Windows.Controls.TextBlock bTb)
                        {
                            clonedB.Child = new System.Windows.Controls.TextBlock
                            {
                                Text = bTb.Text, FontFamily = bTb.FontFamily, FontSize = bTb.FontSize,
                                FontWeight = bTb.FontWeight, Foreground = bTb.Foreground?.Clone()
                            };
                        }
                        CanvasControl.SetLeft(clonedB, CanvasControl.GetLeft(b));
                        CanvasControl.SetTop(clonedB, CanvasControl.GetTop(b));
                        newCanvas.Children.Add(clonedB);
                    }
                }
                return newCanvas;
            }

            // Fallback: Xaml clone cho các loại đối tượng FrameworkElement phức tạp khác
            try
            {
                if (original.Element is FrameworkElement fe)
                {
                    string xaml = System.Windows.Markup.XamlWriter.Save(fe);
                    if (System.Windows.Markup.XamlReader.Parse(xaml) is UIElement parsed)
                    {
                        return parsed;
                    }
                }
            }
            catch { }

            System.Diagnostics.Debug.WriteLine($"⚠️ Clone not supported: {original.Element?.GetType().Name}");
            return null;
        }

        /// <summary>
        /// Clone Shape element helper
        /// </summary>
        private System.Windows.Shapes.Shape? CloneShapeElement(System.Windows.Shapes.Shape source)
        {
            if (source is System.Windows.Shapes.Line line)
            {
                return new System.Windows.Shapes.Line
                {
                    X1 = line.X1, Y1 = line.Y1, X2 = line.X2, Y2 = line.Y2,
                    Stroke = line.Stroke?.Clone(), StrokeThickness = line.StrokeThickness,
                    StrokeDashArray = line.StrokeDashArray?.Clone(), Opacity = line.Opacity
                };
            }
            else if (source is Polygon polygon)
            {
                var p = new Polygon
                {
                    Stroke = polygon.Stroke?.Clone(), StrokeThickness = polygon.StrokeThickness,
                    Fill = polygon.Fill?.Clone(), Opacity = polygon.Opacity
                };
                foreach (var pt in polygon.Points) p.Points.Add(pt);
                return p;
            }
            else if (source is Polyline polyline)
            {
                var pl = new Polyline
                {
                    Stroke = polyline.Stroke?.Clone(), StrokeThickness = polyline.StrokeThickness,
                    Opacity = polyline.Opacity
                };
                foreach (var pt in polyline.Points) pl.Points.Add(pt);
                return pl;
            }
            else if (source is System.Windows.Shapes.Rectangle rect)
            {
                return new System.Windows.Shapes.Rectangle
                {
                    Width = rect.Width, Height = rect.Height,
                    Stroke = rect.Stroke?.Clone(), Fill = rect.Fill?.Clone(),
                    StrokeThickness = rect.StrokeThickness, RadiusX = rect.RadiusX, RadiusY = rect.RadiusY,
                    Opacity = rect.Opacity
                };
            }
            else if (source is System.Windows.Shapes.Ellipse ellipse)
            {
                return new System.Windows.Shapes.Ellipse
                {
                    Width = ellipse.Width, Height = ellipse.Height,
                    Stroke = ellipse.Stroke?.Clone(), Fill = ellipse.Fill?.Clone(),
                    StrokeThickness = ellipse.StrokeThickness, Opacity = ellipse.Opacity
                };
            }
            else if (source is System.Windows.Shapes.Path path && path.Data != null)
            {
                return new System.Windows.Shapes.Path
                {
                    Data = path.Data.Clone(), Stroke = path.Stroke?.Clone(),
                    StrokeThickness = path.StrokeThickness, Fill = path.Fill?.Clone(),
                    Opacity = path.Opacity
                };
            }
            return null;
        }

        /// <summary>
        /// NG-3: Check if any objects are selected
        /// </summary>
        public bool HasSelectedObjects()
        {
            return _state.SelectedObjects.Count > 0;
        }

        /// <summary>
        /// Clear clipboard
        /// </summary>
        public void ClearClipboard()
        {
            _clipboard = null;
            _clipboardList = null;
        }

        /// <summary>
        /// Xóa object đã chọn
        /// </summary>
        public void DeleteSelectedObjects()
        {
            var objectsToDelete = _state.SelectedObjects.ToList();
            
            // Clear selection FIRST so DeselectAll fires events and hides selection box/toolbar UI
            DeselectAll();

            foreach (var obj in objectsToDelete)
            {
                RemoveObject(obj);
            }
        }

        /// <summary>
        /// Lock/Unlock object
        /// </summary>
        public void ToggleLockSelectedObject()
        {
            if (_state.PrimarySelectedObject == null)
                return;

            _state.PrimarySelectedObject.IsLocked = !_state.PrimarySelectedObject.IsLocked;
            
            // Nếu lock -> deselect
            if (_state.PrimarySelectedObject.IsLocked)
            {
                DeselectAll();
            }
        }

        /// <summary>
        /// Bring to front (đưa lên trên cùng)
        /// </summary>
        public void BringToFront(SelectableObject? obj = null)
        {
            obj ??= _state.PrimarySelectedObject;
            
            if (obj == null)
                return;

            int maxZIndex = _allObjects.Max(o => o.ZIndex);
            obj.ZIndex = maxZIndex + 1;

            if (obj.Element != null)
            {
                Panel.SetZIndex(obj.Element, obj.ZIndex);
            }
        }

        /// <summary>
        /// Send to back (đưa xuống dưới cùng)
        /// </summary>
        public void SendToBack(SelectableObject? obj = null)
        {
            obj ??= _state.PrimarySelectedObject;
            
            if (obj == null)
                return;

            // Set ZIndex = 0, shift các objects khác lên
            foreach (var other in _allObjects.Where(o => o != obj))
            {
                other.ZIndex++;
                if (other.Element != null)
                {
                    Panel.SetZIndex(other.Element, other.ZIndex);
                }
            }

            obj.ZIndex = 0;
            if (obj.Element != null)
            {
                Panel.SetZIndex(obj.Element, 0);
            }
        }

        /// <summary>
        /// Rotate object 90 degrees
        /// </summary>
        public void RotateSelectedObject90()
        {
            if (_state.PrimarySelectedObject == null)
                return;

            var obj = _state.PrimarySelectedObject;
            obj.RotationAngle = (obj.RotationAngle + 90) % 360;
            obj.ApplyTransform();

            // N24-QT FIX: Cập nhật QuadTree sau khi xoay để hit-test chính xác với bounds mới
            RebuildQuadTree();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Hit test - tìm object tại điểm click
        /// </summary>
        /// <summary>
        /// NG-1 Fix: Public HitTest method for Form to check if clicking on object
        /// </summary>
        /// <summary>
        /// Public HitTest method with 15px touch tolerance for interactive whiteboard selection.
        /// <summary>
        /// Public HitTest method with customizable touch tolerance (default 15px) for interactive whiteboard selection.
        /// </summary>
        public SelectableObject? HitTest(Point point)
        {
            return HitTest(point, 15.0);
        }

        /// <summary>
        /// Public HitTest method with specified touch tolerance for interactive whiteboard selection.
        /// </summary>
        public SelectableObject? HitTest(Point point, double tolerance)
        {
            if (_allObjects.Count == 0)
                return null;

            double touchTolerance = tolerance > 0 ? tolerance : 15.0; // Touch tolerance for fingers/stylus on touch screens
            var queryRect = new Rect(point.X - touchTolerance, point.Y - touchTolerance, touchTolerance * 2, touchTolerance * 2);
            
            List<SelectableObject> candidates;
            if (_quadTree != null)
            {
                candidates = _quadTree.Retrieve(queryRect);
                // Fallback to _allObjects if QuadTree returned empty but objects exist
                if (candidates.Count == 0 && _allObjects.Count > 0)
                {
                    candidates = _allObjects;
                }
            }
            else
            {
                candidates = _allObjects;
            }

            // Sort by ZIndex descending (top to bottom), filtering out elements with IsHitTestVisible = false
            var sortedObjects = candidates
                .Where(o => !o.IsLocked && (o.Element == null || o.Element.IsHitTestVisible))
                .OrderByDescending(o => o.ZIndex)
                .ToList();

            // Pass 1: Direct bounds containment
            foreach (var obj in sortedObjects)
            {
                // For unfilled shapes (like Compa circles with Fill == null), check stroke edge proximity instead of entire interior
                if (obj.Element is System.Windows.Shapes.Ellipse ellipse && ellipse.Fill == null)
                {
                    var center = new Point(obj.Bounds.X + obj.Bounds.Width / 2, obj.Bounds.Y + obj.Bounds.Height / 2);
                    double dist = Math.Sqrt(Math.Pow(point.X - center.X, 2) + Math.Pow(point.Y - center.Y, 2));
                    double radius = Math.Max(obj.Bounds.Width, obj.Bounds.Height) / 2.0;
                    
                    // Only hit if within tolerance of edge stroke
                    if (Math.Abs(dist - radius) <= touchTolerance)
                    {
                        System.Diagnostics.Debug.WriteLine($"   ✅ HIT (Unfilled Ellipse Edge)! Type: {obj.Type}");
                        return obj;
                    }
                    continue;
                }

                if (obj.Bounds.Contains(point))
                {
                    System.Diagnostics.Debug.WriteLine($"   ✅ HIT (Direct Bounds)! Type: {obj.Type}");
                    return obj;
                }
            }

            // Pass 2: Expanded bounds with touch tolerance (for thin strokes)
            foreach (var obj in sortedObjects)
            {
                var expandedBounds = obj.Bounds;
                expandedBounds.Inflate(touchTolerance, touchTolerance);
                if (expandedBounds.Contains(point))
                {
                    System.Diagnostics.Debug.WriteLine($"   ✅ HIT (Touch Tolerance {touchTolerance}px)! Type: {obj.Type}");
                    return obj;
                }
            }

            System.Diagnostics.Debug.WriteLine($"   ❌ No hit found for point ({point.X:F0}, {point.Y:F0})");
            return null;
        }

        /// <summary>
        /// Get bounding box for multiple selected objects
        /// </summary>
        public Rect GetMultiSelectionBounds()
        {
            if (_state.SelectedObjects.Count == 0)
                return Rect.Empty;
            
            if (_state.SelectedObjects.Count == 1)
                return _state.SelectedObjects[0].Bounds;
            
            // Calculate bounding box that encompasses all selected objects
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            
            foreach (var obj in _state.SelectedObjects)
            {
                minX = Math.Min(minX, obj.Position.X);
                minY = Math.Min(minY, obj.Position.Y);
                maxX = Math.Max(maxX, obj.Position.X + obj.Size.Width);
                maxY = Math.Max(maxY, obj.Position.Y + obj.Size.Height);
            }
            
            if (minX < 0) minX = 0;
            if (minY < 0) minY = 0;
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        #endregion

        #region Lasso Selection Support

        /// <summary>
        /// Select multiple objects from lasso (tích hợp với LassoSelectionTool)
        /// </summary>
        public void SelectFromLasso(List<UIElement> elements)
        {
            if (elements == null || elements.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Lasso selection: No elements found");
                return;
            }

            // Tìm SelectableObjects tương ứng với UIElements
            var selectableObjects = new List<SelectableObject>();

            foreach (var element in elements)
            {
                var obj = _allObjects.FirstOrDefault(o => 
                    o.Element == element || 
                    (element is FrameworkElement fe1 && o.Element is FrameworkElement fe2 && 
                     (fe1 == fe2 || fe1.IsAncestorOf(fe2) || fe2.IsAncestorOf(fe1))));

                if (obj == null && element is FrameworkElement fe)
                {
                    double left = System.Windows.Controls.Canvas.GetLeft(fe);
                    double top = System.Windows.Controls.Canvas.GetTop(fe);
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;
                    double w = fe.ActualWidth > 0 ? fe.ActualWidth : fe.Width;
                    double h = fe.ActualHeight > 0 ? fe.ActualHeight : fe.Height;
                    if (double.IsNaN(w) || w <= 0) w = fe.RenderSize.Width > 0 ? fe.RenderSize.Width : 20;
                    if (double.IsNaN(h) || h <= 0) h = fe.RenderSize.Height > 0 ? fe.RenderSize.Height : 20;

                    obj = new SelectableObject
                    {
                        Element = fe,
                        Type = ObjectType.Shape,
                        Position = new Point(left, top),
                        Size = new Size(w, h)
                    };
                    AddObject(obj);
                    // ✅ BUG-FIX: Nếu AddObject reject (ví dụ Background element), bỏ qua không cho vào selection
                    if (!_allObjects.Contains(obj))
                    {
                        obj = null;
                    }
                }

                if (obj != null && !obj.IsLocked && !selectableObjects.Contains(obj))
                {
                    obj.UpdateBounds(); // B.2.2: Cập nhật bounds mới nhất
                    selectableObjects.Add(obj);
                }
            }

            if (selectableObjects.Count > 0)
            {
                SelectMultiple(selectableObjects);
                ShowMultiSelectionAdorners();
                
                // ✅ REFACTOR: KHÔNG auto-group khi chọn bằng Lasso nữa!
                // Lasso chỉ chọn nhiều đối tượng (Multi-Selection), gom nhóm chỉ thực hiện khi GV bấm nút "Gom nhóm"
                RebuildQuadTree();
                System.Diagnostics.Debug.WriteLine($"✅ Lasso selected {selectableObjects.Count} object(s) (Multi-selection without auto-grouping)");
                
                System.Diagnostics.Debug.WriteLine($"✅ Lasso selected {selectableObjects.Count} object(s)");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ No selectable objects found in lasso");
            }
        }

        /// <summary>
        /// Hiển thị adorner cho tất cả objects được chọn
        /// Nếu chọn nhiều objects -> hiển thị 1 group adorner duy nhất
        /// Nếu chọn 1 object -> hiển thị adorner riêng
        /// </summary>
        private void ShowMultiSelectionAdorners()
        {
            // SelectionBox (SelectionBox.xaml) trên Form2_MainDashboard chịu trách nhiệm hiển thị duy nhất
            // tất cả giao diện chọn (khung nét đứt, 4 chốt vuông, chốt xoay, toolbar).
            // Do đó ta gỡ bỏ hoàn toàn AdornerLayer trùng lặp để tránh hiện ô nét đứt ma bên trong hình.
            HideAllAdorners();
            System.Diagnostics.Debug.WriteLine($"✅ MultiSelection updated for {_state.SelectedObjects.Count} object(s)");
        }

        /// <summary>
        /// IMP-1604: Làm mới vị trí/kích thước adorner sau khi multi-touch manipulation.
        /// Cập nhật Bounds cho tất cả đối tượng đang chọn rồi hiển thị lại adorner.
        /// </summary>
        public void RefreshSelectionAdorner()
        {
            if (_state.SelectedObjects.Count == 0)
                return;

            // Cập nhật bounds cho mỗi đối tượng đang chọn
            foreach (var obj in _state.SelectedObjects)
            {
                obj.UpdateBounds();
            }

            // Ẩn adorner cũ rồi hiển thị lại
            HideAllAdorners();
            ShowMultiSelectionAdorners();
            
            System.Diagnostics.Debug.WriteLine(
                $"🔄 IMP-1604: Refreshed adorner for {_state.SelectedObjects.Count} selected object(s)");
        }

        /// <summary>
        /// ✅ GĐ1-FIX: Làm mới GroupAdorner khi kéo nhóm đối tượng
        /// Recalculate group bounds và hiển thị lại adorner
        /// </summary>
        public void RefreshGroupAdorner()
        {
            if (_state.SelectedObjects.Count <= 1)
                return;

            HideAllAdorners();
            var groupBounds = CalculateGroupBounds(_state.SelectedObjects);
            ShowGroupAdorner(groupBounds);
        }

        /// <summary>
        /// Tính bounding box chung cho nhiều objects
        /// Chỉ tính từ objects có kích thước hợp lý (bỏ qua background rectangles)
        /// </summary>
        private Rect CalculateGroupBounds(List<SelectableObject> objects)
        {
            if (objects.Count == 0)
                return Rect.Empty;

            // Filter: Chỉ lấy objects hợp lệ, bỏ qua các ô nền background
            var validObjects = objects.Where(obj => 
                !obj.Bounds.IsEmpty &&
                obj.Bounds.Width > 0 && obj.Bounds.Height > 0 &&
                !(obj.Element is FrameworkElement fe && fe.Tag?.ToString()?.Contains("Background") == true)
            ).ToList();

            if (validObjects.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ No valid objects for group bounds (all objects too large)");
                return Rect.Empty;
            }

            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            foreach (var obj in validObjects)
            {
                var bounds = obj.Bounds;
                minX = System.Math.Min(minX, bounds.Left);
                minY = System.Math.Min(minY, bounds.Top);
                maxX = System.Math.Max(maxX, bounds.Right);
                maxY = System.Math.Max(maxY, bounds.Bottom);

                System.Diagnostics.Debug.WriteLine($"   Including {obj.Type}: Bounds=({bounds.Left:F0},{bounds.Top:F0},{bounds.Width:F0},{bounds.Height:F0})");
            }

            // Thêm padding nhỏ để box không sát quá
            const double padding = 12; // G2.2: Padding 12px de khung khong de len ky hieu do thi
            minX -= padding;
            minY -= padding;
            maxX += padding;
            maxY += padding;

            if (minX < 0) minX = 0;
            if (minY < 0) minY = 0;
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Hiển thị group adorner trên canvas
        /// </summary>
        private void ShowGroupAdorner(Rect groupBounds)
        {
            // SelectionBox trên Form2_MainDashboard chịu trách nhiệm hiển thị duy nhất
            HideAllAdorners();
        }

        /// <summary>
        /// Ẩn group adorner
        /// </summary>
        private void HideGroupAdorner()
        {
            _groupAdorner = null;
        }

        /// <summary>
        /// Hiển thị adorner cho một element
        /// </summary>
        private void ShowAdorner(UIElement element)
        {
            // SelectionBox trên Form2_MainDashboard chịu trách nhiệm hiển thị duy nhất
            HideAllAdorners();
        }

        /// <summary>
        /// Ẩn adorner của một element
        /// </summary>
        private void HideAdorner(UIElement element)
        {
            _adorners.Remove(element);
        }

        /// <summary>
        /// Ẩn tất cả adorners (quét sạch mọi GroupSelectionAdorner và SelectionAdorner kẹt trên AdornerLayer)
        /// </summary>
        public void HideAllAdorners()
        {
            try
            {
                if (_canvas != null)
                {
                    var adornerLayer = AdornerLayer.GetAdornerLayer(_canvas);
                    if (adornerLayer != null)
                    {
                        var adorners = adornerLayer.GetAdorners(_canvas);
                        if (adorners != null)
                        {
                            foreach (var adorner in adorners)
                            {
                                if (adorner is GroupSelectionAdorner || adorner is SelectionAdorner)
                                {
                                    adornerLayer.Remove(adorner);
                                }
                            }
                        }
                    }

                    // Quét các phần tử con trên canvas để dọn dẹp triệt để
                    foreach (UIElement child in _canvas.Children)
                    {
                        var childLayer = AdornerLayer.GetAdornerLayer(child);
                        if (childLayer != null)
                        {
                            var childAdorners = childLayer.GetAdorners(child);
                            if (childAdorners != null)
                            {
                                foreach (var adorner in childAdorners)
                                {
                                    if (adorner is SelectionAdorner || adorner is GroupSelectionAdorner)
                                    {
                                        childLayer.Remove(adorner);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ HideAllAdorners exception: {ex.Message}");
            }

            _adorners.Clear();
            _groupAdorner = null;
        }

        #endregion

        #region Group Management

        /// <summary>
        /// Tạo group từ các objects đang được chọn
        /// </summary>
        public SelectableGroup? CreateGroupFromSelection()
        {
            var selectedObjects = GetSelectedObjects();
            
            if (selectedObjects.Count < 2)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Need at least 2 objects to create a group");
                return null;
            }

            var group = new SelectableGroup(selectedObjects);
            _groups[group.GroupId] = group;

            System.Diagnostics.Debug.WriteLine($"✅ Group created: {group.GroupId} with {selectedObjects.Count} objects");
            return group;
        }

        /// <summary>
        /// Lấy group theo ID
        /// </summary>
        public SelectableGroup? GetGroup(string groupId)
        {
            return _groups.TryGetValue(groupId, out var group) ? group : null;
        }

        /// <summary>
        /// Lấy group chứa object
        /// </summary>
        public SelectableGroup? GetGroupContaining(SelectableObject obj)
        {
            if (string.IsNullOrEmpty(obj.GroupId))
                return null;

            return GetGroup(obj.GroupId);
        }

        /// <summary>
        /// Ungroup - xóa group và clear GroupId của các members
        /// </summary>
        public void UngroupSelection()
        {
            var selectedObjects = GetSelectedObjects();
            
            foreach (var obj in selectedObjects)
            {
                if (!string.IsNullOrEmpty(obj.GroupId))
                {
                    var group = GetGroup(obj.GroupId);
                    if (group != null)
                    {
                        group.Ungroup();
                        _groups.Remove(obj.GroupId);
                        System.Diagnostics.Debug.WriteLine($"❌ Group {obj.GroupId} ungrouped");
                    }
                }
            }
        }

        /// <summary>
        /// Chọn toàn bộ group khi click vào 1 member
        /// </summary>
        public void SelectGroup(string groupId)
        {
            var group = GetGroup(groupId);
            if (group == null)
                return;

            // Chọn tất cả members trong group
            SelectMultiple(group.Members.ToList());
            System.Diagnostics.Debug.WriteLine($"✅ Selected entire group: {groupId} ({group.Count} objects)");
        }

        /// <summary>
        /// Lấy tất cả objects trong group (bao gồm cả object được click)
        /// </summary>
        public List<SelectableObject> GetGroupMembers(SelectableObject obj)
        {
            var group = GetGroupContaining(obj);
            if (group != null)
            {
                return group.Members.ToList();
            }
            
            // Nếu không thuộc group nào, trả về chỉ object đó
            return new List<SelectableObject> { obj };
        }

        /// <summary>
        /// Xây dựng lại QuadTree từ danh sách tất cả các đối tượng hiện có.
        /// Thường gọi sau khi kết thúc kéo thả, thay đổi kích thước, xoay, hoặc nhóm đối tượng.
        /// </summary>
        public SelectableObject? GetSelectableObjectForElement(UIElement element)
        {
            return _allObjects.FirstOrDefault(o => o.Element == element);
        }

        public void RebuildQuadTree()
        {
            _quadTree?.Clear();
            foreach (var obj in _allObjects)
            {
                _quadTree?.Insert(obj);
            }
            System.Diagnostics.Debug.WriteLine("🌳 SelectionManager: QuadTree rebuilt successfully.");
        }

        /// <summary>
        /// Kiểm tra an toàn tuyệt đối xem một UIElement có phải là Nền bảng (Background Layer/Grid/Overlay) hay không.
        /// Ngăn chặn 100% tình huống chọn nhầm nền bảng.
        /// </summary>
        public static bool IsBackgroundElement(UIElement? element, Rect bounds = default, double boardWidth = 0, double boardHeight = 0)
        {
            if (element == null) return false;

            if (element is FrameworkElement fe)
            {
                // 1. Kiểm tra Tag
                string tagStr = fe.Tag?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(tagStr))
                {
                    if (tagStr.Equals("BackgroundLayer", StringComparison.OrdinalIgnoreCase) ||
                        tagStr.Contains("background", StringComparison.OrdinalIgnoreCase) ||
                        tagStr.Contains("Background", StringComparison.OrdinalIgnoreCase) ||
                        tagStr.Contains("grid", StringComparison.OrdinalIgnoreCase) ||
                        tagStr.Contains("Grid", StringComparison.OrdinalIgnoreCase) ||
                        tagStr.Contains("MultiUser", StringComparison.OrdinalIgnoreCase) ||
                        tagStr.Equals("system", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                // 2. Kiểm tra Name
                string elName = fe.Name ?? string.Empty;
                if (!string.IsNullOrEmpty(elName))
                {
                    if (elName.Contains("background", StringComparison.OrdinalIgnoreCase) ||
                        elName.Contains("Background", StringComparison.OrdinalIgnoreCase) ||
                        elName.Contains("welcome", StringComparison.OrdinalIgnoreCase) ||
                        elName.Contains("panel", StringComparison.OrdinalIgnoreCase) ||
                        elName.Contains("overlay", StringComparison.OrdinalIgnoreCase) ||
                        elName.Contains("grid", StringComparison.OrdinalIgnoreCase) ||
                        elName.Contains("Grid", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            // 3. Kiểm tra diện tích nếu phủ >= 80% kích thước bảng
            if (boardWidth > 100 && boardHeight > 100 && !bounds.IsEmpty)
            {
                if (bounds.Width >= boardWidth * 0.8 && bounds.Height >= boardHeight * 0.8)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
