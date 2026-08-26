using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartTouch.Helpers;
using QASmartTouch.Services;
using QASmartTouch.Shared;
using Serilog;

namespace QASmartTouch.Managers
{
    /// <summary>
    /// Eraser modes available
    /// </summary>
    public enum EraserMode
    {
        Stroke,     // Erase entire stroke on click/touch
        Point,      // Erase by slicing/cutting strokes under eraser
        Drag,       // Legacy drag mode
        ClearAll    // Clear all objects
    }
    
    /// <summary>
    /// Tracks original elements removed and new sub-elements generated during an eraser gesture session (for batch undo).
    /// </summary>
    public class EraseSession
    {
        private readonly HashSet<UIElement> _originalRemovedElements = new();
        private readonly HashSet<UIElement> _activeGeneratedElements = new();

        public void RegisterRemovedElement(UIElement element)
        {
            if (element == null) return;
            if (_activeGeneratedElements.Contains(element))
            {
                _activeGeneratedElements.Remove(element);
            }
            else
            {
                _originalRemovedElements.Add(element);
            }
        }

        public void RegisterAddedElement(UIElement element)
        {
            if (element == null) return;
            _activeGeneratedElements.Add(element);
        }

        public bool HasChanges => _originalRemovedElements.Count > 0 || _activeGeneratedElements.Count > 0;

        public IReadOnlyCollection<UIElement> OriginalRemovedElements => _originalRemovedElements;
        public IReadOnlyCollection<UIElement> ActiveGeneratedElements => _activeGeneratedElements;
    }
    
    /// <summary>
    /// Handles eraser operations (stroke, drag, clear all)
    /// Manages eraser state, modes, and object removal
    /// </summary>
    public class EraserEngine
    {
        #region Constants
        
        /// <summary>
        /// Minimum eraser size
        /// </summary>
        public const int MIN_ERASER_SIZE = 10;
        
        /// <summary>
        /// Maximum eraser size
        /// </summary>
        public const int MAX_ERASER_SIZE = 100;
        
        /// <summary>
        /// Default eraser size
        /// </summary>
        public const int DEFAULT_ERASER_SIZE = 20;

        /// <summary>
        /// [LOI_VID_51] Hit test margin mặc định (px) — DPI 96.
        /// Sẽ được nhân với hệ số DPI thực tế để đảm bảo consistent trên 4K 150%.
        /// Theo phản biện ThS. Vũ Hoàng Anh: scale = 3.0 * (dpi/96.0)
        /// </summary>
        private const double BASE_HIT_TEST_MARGIN = 3.0;

        /// <summary>
        /// [LOI_VID_51] Ngưỡng tối thiểu kích thước element để không bị coi là ghost artifact (px).
        /// Element < 3x3px sẽ bị loại bỏ sau khi tẩy.
        /// </summary>
        private const double GHOST_ARTIFACT_THRESHOLD = 3.0;
        
        #endregion
        
        #region Fields
        
        private readonly Canvas _canvas;
        private bool _isEraserActive;
        private EraserMode _eraserMode;
        private int _eraserSize;
        private Ellipse? _eraserPreview;
        
        // BUG-1601: Fill bán trong suốt cho Shape rỗng — alpha=1/255, mắt thường không nhìn thấy
        // nhưng đủ để WPF HitTest phát hiện khi ngón tay chạm vào vùng lòng hình vẽ.
        private static readonly Brush _invisibleFill = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
        
        // BUG-1601: Lưu trữ danh sách Shape đã bị gán Fill ẩn kèm Fill gốc để khôi phục sau.
        private readonly List<(Shape shape, Brush? originalFill)> _modifiedShapes = new();
        
        // Session tracking for gesture-based atomic Undo/Redo batching
        private EraseSession? _currentMouseSession;
        private readonly Dictionary<int, EraseSession> _touchSessions = new();
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets the active mouse erase session
        /// </summary>
        public EraseSession? CurrentMouseSession => _currentMouseSession;

        /// <summary>
        /// Gets whether eraser is currently active
        /// </summary>
        public bool IsEraserActive => _isEraserActive;
        
        /// <summary>
        /// Gets the current eraser mode
        /// </summary>
        public EraserMode EraserMode => _eraserMode;
        
        /// <summary>
        /// Gets the current eraser size
        /// </summary>
        public int EraserSize => _eraserSize;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when an object is erased
        /// </summary>
        public event EventHandler<ObjectErasedEventArgs>? ObjectErased;
        
        /// <summary>
        /// Fired when all objects are cleared
        /// </summary>
        public event EventHandler? AllCleared;
        
        /// <summary>
        /// Fired when eraser settings are changed
        /// </summary>
        public event EventHandler<EraserSettingsChangedEventArgs>? SettingsChanged;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of EraserEngine
        /// </summary>
        /// <param name="canvas">The canvas to erase from</param>
        public EraserEngine(Canvas canvas)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _eraserMode = EraserMode.Stroke;
            _eraserSize = DEFAULT_ERASER_SIZE;
            _isEraserActive = false;
        }
        
        #endregion
        
        #region Session Management (Undo/Redo Batching)

        public EraseSession StartMouseSession()
        {
            _currentMouseSession = new EraseSession();
            return _currentMouseSession;
        }

        public EraseSession? EndMouseSession()
        {
            var session = _currentMouseSession;
            _currentMouseSession = null;
            return session;
        }

        public EraseSession StartTouchSession(int touchId)
        {
            var session = new EraseSession();
            _touchSessions[touchId] = session;
            return session;
        }

        public EraseSession? GetTouchSession(int touchId)
        {
            _touchSessions.TryGetValue(touchId, out var session);
            return session;
        }

        public EraseSession? EndTouchSession(int touchId)
        {
            if (_touchSessions.Remove(touchId, out var session))
            {
                return session;
            }
            return null;
        }

        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Enables the eraser with specified mode
        /// </summary>
        /// <param name="mode">The eraser mode to use</param>
        public void EnableEraser(EraserMode mode)
        {
            _isEraserActive = true;
            _eraserMode = mode;
            
            // BUG-1601: Gán Fill ẩn cho Shape rỗng để HitTest phát hiện được
            if (mode == EraserMode.Stroke || mode == EraserMode.Drag)
            {
                SetInvisibleFillForEmptyShapes();
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ Eraser enabled: Mode={mode}");
        }
        
        /// <summary>
        /// Disables the eraser
        /// </summary>
        public void DisableEraser()
        {
            _isEraserActive = false;
            HideEraserPreview();
            
            // BUG-1601: Khôi phục Fill gốc cho các Shape đã bị gán Fill ẩn
            RestoreOriginalFills();
            
            System.Diagnostics.Debug.WriteLine($"❌ Eraser disabled");
        }
        
        /// <summary>
        /// Erases a single stroke/object
        /// </summary>
        /// <param name="element">The element to erase</param>
        /// <returns>True if erased successfully, false otherwise</returns>
        public bool EraseByStroke(UIElement element)
        {
            if (element == null || !_canvas.Children.Contains(element))
                return false;
            
            // BUG-1601: Khôi phục Fill gốc cho Shape trước khi xóa,
            // để khi Undo add lại, Shape hiển thị đúng Fill gốc (null/Transparent)
            // thay vì Fill ẩn alpha=1.
            RestoreOriginalFillForElement(element);
            
            _canvas.Children.Remove(element);
            
            System.Diagnostics.Debug.WriteLine($"🗑️ Erased element: {element.GetType().Name}");
            
            // Fire event
            ObjectErased?.Invoke(this, new ObjectErasedEventArgs(element));
            
            return true;
        }
        
        /// <summary>
        /// Slices/Cuts strokes at given point (Point Erase mode).
        /// Removes parts within radius and creates new sub-strokes for parts outside.
        /// </summary>
        public bool EraseByPoint(Point center, double radius, EraseSession? session = null, Action<UIElement, string>? recordRemove = null, Action<UIElement, string>? recordAdd = null)
        {
            if (!_canvas.IsLoaded && _canvas.ActualWidth == 0) return false;
            
            bool anyErased = false;
            Rect eraserBounds = new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2);

            var children = _canvas.Children.OfType<UIElement>().ToList();
            foreach (var child in children)
            {
                if (IsSystemElement(child)) continue;
                if (!child.IsHitTestVisible) continue;

                if (child is Polyline polyline && polyline.Points != null && polyline.Points.Count >= 2)
                {
                    // Check bounds intersection first
                    var bounds = BoundsHelper.GetAbsoluteBounds(polyline, _canvas);
                    if (!bounds.IsEmpty && !bounds.IntersectsWith(eraserBounds))
                        continue;

                    var canvasPoints = GetPolylinePointsInCanvas(polyline, _canvas);
                    var pieces = SlicePolyline(canvasPoints, center, radius);

                    // Check if points were modified/sliced
                    bool isModified = false;
                    if (pieces.Count != 1 || pieces[0].Count != canvasPoints.Count)
                    {
                        isModified = true;
                    }
                    else
                    {
                        for (int i = 0; i < canvasPoints.Count; i++)
                        {
                            if (canvasPoints[i] != pieces[0][i])
                            {
                                isModified = true;
                                break;
                            }
                        }
                    }

                    if (isModified)
                    {
                        anyErased = true;
                        RestoreOriginalFillForElement(polyline);
                        _canvas.Children.Remove(polyline);
                        session?.RegisterRemovedElement(polyline);
                        recordRemove?.Invoke(polyline, "Point erase (split)");
                        ObjectErased?.Invoke(this, new ObjectErasedEventArgs(polyline));

                        foreach (var piece in pieces)
                        {
                            if (piece.Count < 2) continue;

                            var newPoly = new Polyline
                            {
                                Points = piece,
                                Stroke = polyline.Stroke,
                                StrokeThickness = polyline.StrokeThickness,
                                StrokeLineJoin = polyline.StrokeLineJoin,
                                StrokeStartLineCap = polyline.StrokeStartLineCap,
                                StrokeEndLineCap = polyline.StrokeEndLineCap,
                                Opacity = polyline.Opacity,
                                UseLayoutRounding = false
                            };

                            RenderOptions.SetEdgeMode(newPoly, EdgeMode.Unspecified);
                            RenderOptions.SetBitmapScalingMode(newPoly, BitmapScalingMode.HighQuality);
                            Panel.SetZIndex(newPoly, Panel.GetZIndex(polyline));

                            _canvas.Children.Add(newPoly);
                            session?.RegisterAddedElement(newPoly);
                            recordAdd?.Invoke(newPoly, "Sub-stroke segment");
                        }
                    }
                }
                else if (child is Line line)
                {
                    Point p1 = new Point(line.X1, line.Y1);
                    Point p2 = new Point(line.X2, line.Y2);
                    var pts = new PointCollection { p1, p2 };
                    var pieces = SlicePolyline(pts, center, radius);

                    if (pieces.Count != 1 || pieces[0].Count != 2 || pieces[0][0] != p1 || pieces[0][1] != p2)
                    {
                        anyErased = true;
                        RestoreOriginalFillForElement(line);
                        _canvas.Children.Remove(line);
                        session?.RegisterRemovedElement(line);
                        recordRemove?.Invoke(line, "Point erase Line");
                        ObjectErased?.Invoke(this, new ObjectErasedEventArgs(line));

                        foreach (var piece in pieces)
                        {
                            if (piece.Count < 2) continue;
                            var newLine = new Line
                            {
                                X1 = piece[0].X,
                                Y1 = piece[0].Y,
                                X2 = piece[1].X,
                                Y2 = piece[1].Y,
                                Stroke = line.Stroke,
                                StrokeThickness = line.StrokeThickness,
                                StrokeStartLineCap = line.StrokeStartLineCap,
                                StrokeEndLineCap = line.StrokeEndLineCap,
                                Opacity = line.Opacity
                            };
                            Panel.SetZIndex(newLine, Panel.GetZIndex(line));
                            _canvas.Children.Add(newLine);
                            session?.RegisterAddedElement(newLine);
                            recordAdd?.Invoke(newLine, "Sub-line segment");
                        }
                    }
                }
                else if (child is Path path && path.Data != null)
                {
                    var pointCollections = ExtractPointsFromPath(path, _canvas);
                    bool hit = false;

                    if (pointCollections.Count > 0)
                    {
                        foreach (var pts in pointCollections)
                        {
                            for (int i = 0; i < pts.Count - 1; i++)
                            {
                                if (DistanceFromPointToLineSegment(center, pts[i], pts[i + 1]) <= radius)
                                {
                                    hit = true;
                                    break;
                                }
                            }
                            if (hit) break;
                        }
                    }

                    if (!hit)
                    {
                        try
                        {
                            var pen = new Pen(Brushes.Black, (path.StrokeThickness > 0 ? path.StrokeThickness : 2) + radius * 2);
                            Point localCenter = center;
                            var matrix = GetElementMatrixToCanvas(path, _canvas);
                            if (matrix.HasInverse)
                            {
                                matrix.Invert();
                                localCenter = matrix.Transform(center);
                            }
                            hit = path.Data.StrokeContains(pen, localCenter) || (path.Fill != null && path.Data.FillContains(localCenter));
                        }
                        catch { }
                    }

                    if (hit)
                    {
                        bool isModified = false;
                        var allPieces = new List<PointCollection>();

                        foreach (var pts in pointCollections)
                        {
                            var pieces = SlicePolyline(pts, center, radius);
                            if (pieces.Count != 1 || pieces[0].Count != pts.Count)
                            {
                                isModified = true;
                            }
                            allPieces.AddRange(pieces);
                        }

                        if (isModified || allPieces.Count == 0)
                        {
                            anyErased = true;
                            RestoreOriginalFillForElement(path);
                            _canvas.Children.Remove(path);
                            session?.RegisterRemovedElement(path);
                            recordRemove?.Invoke(path, "Point erase Path (split)");
                            ObjectErased?.Invoke(this, new ObjectErasedEventArgs(path));

                            foreach (var piece in allPieces)
                            {
                                if (piece.Count < 2) continue;
                                var newPoly = new Polyline
                                {
                                    Points = piece,
                                    Stroke = path.Stroke,
                                    StrokeThickness = path.StrokeThickness,
                                    StrokeLineJoin = path.StrokeLineJoin,
                                    StrokeStartLineCap = path.StrokeStartLineCap,
                                    StrokeEndLineCap = path.StrokeEndLineCap,
                                    Opacity = path.Opacity,
                                    UseLayoutRounding = false
                                };
                                RenderOptions.SetEdgeMode(newPoly, EdgeMode.Unspecified);
                                RenderOptions.SetBitmapScalingMode(newPoly, BitmapScalingMode.HighQuality);
                                Panel.SetZIndex(newPoly, Panel.GetZIndex(path));
                                _canvas.Children.Add(newPoly);
                                session?.RegisterAddedElement(newPoly);
                                recordAdd?.Invoke(newPoly, "Sub-stroke from Path");
                            }
                        }
                    }
                }
                else if (child is Shape shape)
                {
                    // Closed shapes: erase if touched
                    var bounds = BoundsHelper.GetAbsoluteBounds(shape, _canvas);
                    if (!bounds.IsEmpty && bounds.IntersectsWith(eraserBounds))
                    {
                        anyErased = true;
                        RestoreOriginalFillForElement(shape);
                        _canvas.Children.Remove(shape);
                        session?.RegisterRemovedElement(shape);
                        recordRemove?.Invoke(shape, $"Point erase {shape.GetType().Name}");
                        ObjectErased?.Invoke(this, new ObjectErasedEventArgs(shape));
                    }
                }
            }

            return anyErased;
        }

        /// <summary>
        /// Erases entire strokes/objects touched by the eraser (Stroke Erase mode).
        /// </summary>
        public bool EraseByStrokeAtPoint(Point center, double radius, EraseSession? session = null, Action<UIElement, string>? recordRemove = null)
        {
            if (!_canvas.IsLoaded && _canvas.ActualWidth == 0) return false;

            bool anyErased = false;
            Rect eraserBounds = new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2);

            var children = _canvas.Children.OfType<UIElement>().ToList();
            foreach (var child in children)
            {
                if (IsSystemElement(child)) continue;
                if (!child.IsHitTestVisible) continue;

                bool shouldRemove = false;

                if (child is Polyline polyline && polyline.Points != null && polyline.Points.Count > 0)
                {
                    var bounds = BoundsHelper.GetAbsoluteBounds(polyline, _canvas);
                    if (bounds.IsEmpty || bounds.IntersectsWith(eraserBounds))
                    {
                        var canvasPoints = GetPolylinePointsInCanvas(polyline, _canvas);
                        for (int i = 0; i < canvasPoints.Count - 1; i++)
                        {
                            double dist = DistanceFromPointToLineSegment(center, canvasPoints[i], canvasPoints[i + 1]);
                            if (dist <= radius)
                            {
                                shouldRemove = true;
                                break;
                            }
                        }
                    }
                }
                else if (child is Line line)
                {
                    double dist = DistanceFromPointToLineSegment(center, new Point(line.X1, line.Y1), new Point(line.X2, line.Y2));
                    if (dist <= radius)
                    {
                        shouldRemove = true;
                    }
                }
                else if (child is Path path && path.Data != null)
                {
                    var pointCollections = ExtractPointsFromPath(path, _canvas);
                    if (pointCollections.Count > 0)
                    {
                        foreach (var pts in pointCollections)
                        {
                            for (int i = 0; i < pts.Count - 1; i++)
                            {
                                if (DistanceFromPointToLineSegment(center, pts[i], pts[i + 1]) <= radius)
                                {
                                    shouldRemove = true;
                                    break;
                                }
                            }
                            if (shouldRemove) break;
                        }
                    }

                    if (!shouldRemove)
                    {
                        try
                        {
                            var pen = new Pen(Brushes.Black, (path.StrokeThickness > 0 ? path.StrokeThickness : 2) + radius * 2);
                            Point localCenter = center;
                            var matrix = GetElementMatrixToCanvas(path, _canvas);
                            if (matrix.HasInverse)
                            {
                                matrix.Invert();
                                localCenter = matrix.Transform(center);
                            }
                            shouldRemove = path.Data.StrokeContains(pen, localCenter) || (path.Fill != null && path.Data.FillContains(localCenter));
                        }
                        catch { }
                    }
                }
                else if (child is Shape shape)
                {
                    var bounds = BoundsHelper.GetAbsoluteBounds(shape, _canvas);
                    if (!bounds.IsEmpty && bounds.IntersectsWith(eraserBounds))
                    {
                        shouldRemove = true;
                    }
                }

                if (shouldRemove)
                {
                    anyErased = true;
                    RestoreOriginalFillForElement(child);
                    _canvas.Children.Remove(child);
                    session?.RegisterRemovedElement(child);
                    recordRemove?.Invoke(child, $"Stroke erase {child.GetType().Name}");
                    ObjectErased?.Invoke(this, new ObjectErasedEventArgs(child));
                }
            }

            return anyErased;
        }

        /// <summary>
        /// Retrieves the Matrix transform from element coordinate space to canvas coordinate space.
        /// Accumulates RenderTransform and Canvas.Left/Top reliably across all environments.
        /// </summary>
        public static Matrix GetElementMatrixToCanvas(UIElement element, Canvas canvas)
        {
            var matrix = Matrix.Identity;
            if (element == null || canvas == null) return matrix;

            UIElement? current = element;
            while (current != null && current != canvas)
            {
                if (current.RenderTransform != null && current.RenderTransform != Transform.Identity)
                {
                    matrix.Append(current.RenderTransform.Value);
                }

                double left = Canvas.GetLeft(current); if (double.IsNaN(left)) left = 0;
                double top = Canvas.GetTop(current); if (double.IsNaN(top)) top = 0;
                if (left != 0 || top != 0)
                {
                    matrix.Translate(left, top);
                }

                current = VisualTreeHelper.GetParent(current) as UIElement ?? (current as FrameworkElement)?.Parent as UIElement;
            }

            return matrix;
        }

        /// <summary>
        /// Extracts point collections from a Polyline element in Canvas coordinates.
        /// </summary>
        public static PointCollection GetPolylinePointsInCanvas(Polyline polyline, Canvas canvas)
        {
            if (polyline?.Points == null) return new PointCollection();
            var matrix = GetElementMatrixToCanvas(polyline, canvas);
            if (matrix.IsIdentity) return polyline.Points;

            var transPts = new PointCollection(polyline.Points.Count);
            foreach (var pt in polyline.Points)
            {
                transPts.Add(matrix.Transform(pt));
            }
            return transPts;
        }

        /// <summary>
        /// Extracts point collections from a Path element.
        /// First checks path.Tag (which preserves original stroke points), then parses PathGeometry segments.
        /// If canvas is provided, transforms all extracted points into Canvas coordinate space.
        /// </summary>
        public static List<PointCollection> ExtractPointsFromPath(Path path, Canvas? canvas = null)
        {
            var rawResult = new List<PointCollection>();
            if (path == null) return rawResult;

            if (path.Tag is PointCollection tagPoints && tagPoints.Count >= 2)
            {
                rawResult.Add(new PointCollection(tagPoints));
            }
            else if (path.Data != null)
            {
                try
                {
                    var pathGeom = PathGeometry.CreateFromGeometry(path.Data);
                    if (pathGeom != null && pathGeom.Figures.Count > 0)
                    {
                        foreach (var figure in pathGeom.Figures)
                        {
                            var pts = new PointCollection();
                            Point currentPoint = figure.StartPoint;
                            pts.Add(currentPoint);

                            foreach (var seg in figure.Segments)
                            {
                                if (seg is LineSegment ls)
                                {
                                    pts.Add(ls.Point);
                                    currentPoint = ls.Point;
                                }
                                else if (seg is PolyLineSegment pls)
                                {
                                    foreach (var pt in pls.Points)
                                    {
                                        pts.Add(pt);
                                        currentPoint = pt;
                                    }
                                }
                                else if (seg is QuadraticBezierSegment qbs)
                                {
                                    for (int step = 1; step <= 8; step++)
                                    {
                                        double t = step / 8.0;
                                        double u = 1 - t;
                                        double x = u * u * currentPoint.X + 2 * u * t * qbs.Point1.X + t * t * qbs.Point2.X;
                                        double y = u * u * currentPoint.Y + 2 * u * t * qbs.Point1.Y + t * t * qbs.Point2.Y;
                                        pts.Add(new Point(x, y));
                                    }
                                    currentPoint = qbs.Point2;
                                }
                                else if (seg is PolyQuadraticBezierSegment pqbs)
                                {
                                    for (int i = 0; i < pqbs.Points.Count - 1; i += 2)
                                    {
                                        Point p1 = pqbs.Points[i];     // control point
                                        Point p2 = pqbs.Points[i + 1]; // end point
                                        for (int step = 1; step <= 8; step++)
                                        {
                                            double t = step / 8.0;
                                            double u = 1 - t;
                                            double x = u * u * currentPoint.X + 2 * u * t * p1.X + t * t * p2.X;
                                            double y = u * u * currentPoint.Y + 2 * u * t * p1.Y + t * t * p2.Y;
                                            pts.Add(new Point(x, y));
                                        }
                                        currentPoint = p2;
                                    }
                                }
                                else if (seg is BezierSegment bs)
                                {
                                    for (int step = 1; step <= 10; step++)
                                    {
                                        double t = step / 10.0;
                                        double u = 1 - t;
                                        double x = u * u * u * currentPoint.X + 3 * u * u * t * bs.Point1.X + 3 * u * t * t * bs.Point2.X + t * t * t * bs.Point3.X;
                                        double y = u * u * u * currentPoint.Y + 3 * u * u * t * bs.Point1.Y + 3 * u * t * t * bs.Point2.Y + t * t * t * bs.Point3.Y;
                                        pts.Add(new Point(x, y));
                                    }
                                    currentPoint = bs.Point3;
                                }
                                else if (seg is PolyBezierSegment pbs)
                                {
                                    for (int i = 0; i < pbs.Points.Count - 2; i += 3)
                                    {
                                        Point p1 = pbs.Points[i];
                                        Point p2 = pbs.Points[i + 1];
                                        Point p3 = pbs.Points[i + 2];
                                        for (int step = 1; step <= 10; step++)
                                        {
                                            double t = step / 10.0;
                                            double u = 1 - t;
                                            double x = u * u * u * currentPoint.X + 3 * u * u * t * p1.X + 3 * u * t * t * p2.X + t * t * t * p3.X;
                                            double y = u * u * u * currentPoint.Y + 3 * u * u * t * p1.Y + 3 * u * t * t * p2.Y + t * t * t * p3.Y;
                                            pts.Add(new Point(x, y));
                                        }
                                        currentPoint = p3;
                                    }
                                }
                            }

                            if (pts.Count >= 2)
                            {
                                rawResult.Add(pts);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ExtractPointsFromPath] error: {ex.Message}");
                }
            }

            if (canvas == null) return rawResult;

            var matrix = GetElementMatrixToCanvas(path, canvas);
            if (matrix.IsIdentity) return rawResult;

            var transformedResult = new List<PointCollection>(rawResult.Count);
            foreach (var pts in rawResult)
            {
                var transPts = new PointCollection(pts.Count);
                foreach (var pt in pts)
                {
                    transPts.Add(matrix.Transform(pt));
                }
                transformedResult.Add(transPts);
            }
            return transformedResult;
        }

        /// <summary>
        /// Slices a polyline by removing points/segments inside circle (center, radius).
        /// Returns a list of remaining continuous sub-polylines.
        /// </summary>
        public static List<PointCollection> SlicePolyline(PointCollection points, Point center, double radius)
        {
            var result = new List<PointCollection>();
            if (points == null || points.Count == 0) return result;

            double rSq = radius * radius;
            PointCollection currentPiece = new PointCollection();

            for (int i = 0; i < points.Count - 1; i++)
            {
                Point p1 = points[i];
                Point p2 = points[i + 1];

                double d1Sq = (p1.X - center.X) * (p1.X - center.X) + (p1.Y - center.Y) * (p1.Y - center.Y);
                double d2Sq = (p2.X - center.X) * (p2.X - center.X) + (p2.Y - center.Y) * (p2.Y - center.Y);

                bool p1Inside = d1Sq <= rSq;
                bool p2Inside = d2Sq <= rSq;

                double dx = p2.X - p1.X;
                double dy = p2.Y - p1.Y;
                double fx = p1.X - center.X;
                double fy = p1.Y - center.Y;

                double a = dx * dx + dy * dy;
                double b = 2 * (fx * dx + fy * dy);
                double c = fx * fx + fy * fy - rSq;

                double discriminant = b * b - 4 * a * c;

                if (a < 1e-9)
                {
                    if (!p1Inside)
                    {
                        if (currentPiece.Count == 0) currentPiece.Add(p1);
                    }
                    continue;
                }

                if (discriminant < 0)
                {
                    if (!p1Inside)
                    {
                        if (currentPiece.Count == 0) currentPiece.Add(p1);
                        currentPiece.Add(p2);
                    }
                    else
                    {
                        if (currentPiece.Count >= 2) result.Add(currentPiece);
                        currentPiece = new PointCollection();
                    }
                }
                else
                {
                    double sqrtDisc = Math.Sqrt(discriminant);
                    double t1 = (-b - sqrtDisc) / (2 * a);
                    double t2 = (-b + sqrtDisc) / (2 * a);

                    bool hasT1 = t1 >= 0 && t1 <= 1;
                    bool hasT2 = t2 >= 0 && t2 <= 1;

                    if (!p1Inside && !p2Inside)
                    {
                        if (hasT1 && hasT2 && t1 < t2)
                        {
                            Point entryPt = new Point(p1.X + t1 * dx, p1.Y + t1 * dy);
                            Point exitPt = new Point(p1.X + t2 * dx, p1.Y + t2 * dy);

                            if (currentPiece.Count == 0) currentPiece.Add(p1);
                            currentPiece.Add(entryPt);
                            if (currentPiece.Count >= 2) result.Add(currentPiece);

                            currentPiece = new PointCollection();
                            currentPiece.Add(exitPt);
                            currentPiece.Add(p2);
                        }
                        else
                        {
                            if (currentPiece.Count == 0) currentPiece.Add(p1);
                            currentPiece.Add(p2);
                        }
                    }
                    else if (!p1Inside && p2Inside)
                    {
                        double t = hasT1 ? t1 : (hasT2 ? t2 : 0);
                        Point entryPt = new Point(p1.X + t * dx, p1.Y + t * dy);

                        if (currentPiece.Count == 0) currentPiece.Add(p1);
                        currentPiece.Add(entryPt);
                        if (currentPiece.Count >= 2) result.Add(currentPiece);

                        currentPiece = new PointCollection();
                    }
                    else if (p1Inside && !p2Inside)
                    {
                        double t = hasT2 ? t2 : (hasT1 ? t1 : 1);
                        Point exitPt = new Point(p1.X + t * dx, p1.Y + t * dy);

                        if (currentPiece.Count >= 2) result.Add(currentPiece);
                        currentPiece = new PointCollection();
                        currentPiece.Add(exitPt);
                        currentPiece.Add(p2);
                    }
                    else
                    {
                        if (currentPiece.Count >= 2) result.Add(currentPiece);
                        currentPiece = new PointCollection();
                    }
                }
            }

            if (currentPiece.Count >= 2)
            {
                result.Add(currentPiece);
            }

            return result;
        }

        /// <summary>
        /// Calculates shortest distance from a point to a line segment.
        /// </summary>
        public static double DistanceFromPointToLineSegment(Point point, Point lineStart, Point lineEnd)
        {
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;
            double lenSq = dx * dx + dy * dy;
            if (lenSq < 1e-9)
            {
                double px = point.X - lineStart.X;
                double py = point.Y - lineStart.Y;
                return Math.Sqrt(px * px + py * py);
            }
            double t = Math.Clamp(((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lenSq, 0.0, 1.0);
            double projX = lineStart.X + t * dx;
            double projY = lineStart.Y + t * dy;
            double distSq = (point.X - projX) * (point.X - projX) + (point.Y - projY) * (point.Y - projY);
            return Math.Sqrt(distSq);
        }

        /// <summary>
        /// BUG-1601: Trả về eraser preview element để CanvasEventHandlers
        /// loại trừ khỏi HitTest (tránh tẩy nhầm chính preview).
        /// </summary>
        public UIElement? GetEraserPreview() => _eraserPreview;
        
        /// <summary>
        /// [LOI_VID_51] Erases all objects within a rectangular area.
        /// Đã nâng cấp:
        /// - DPI-aware hit test margin
        /// - Lọc bỏ micro-elements (ghost artifacts) < 3x3px
        /// - Batch remove + InvalidateVisual có điều kiện
        /// - Stopwatch performance logging
        /// </summary>
        /// <param name="area">The area to erase within</param>
        /// <returns>List of erased elements</returns>
        public List<UIElement> EraseByDrag(Rect area)
        {
            var sw = Stopwatch.StartNew();
            var erasedElements = new List<UIElement>();
            var elementsToRemove = new List<UIElement>();
            var ghostArtifacts = new List<UIElement>();

            // [LOI_VID_51] DPI-aware hit test margin
            double dpiScale = GetDpiScale();
            double hitMargin = BASE_HIT_TEST_MARGIN * dpiScale;

            // Mở rộng vùng tẩy thêm hit margin
            var expandedArea = new Rect(
                area.X - hitMargin,
                area.Y - hitMargin,
                area.Width + hitMargin * 2,
                area.Height + hitMargin * 2);
            
            foreach (UIElement child in _canvas.Children)
            {
                // Skip system UI elements
                if (IsSystemElement(child))
                    continue;
                
                // Check if element intersects with expanded erase area
                if (ElementIntersectsArea(child, expandedArea))
                {
                    elementsToRemove.Add(child);
                }
            }
            
            // [LOI_VID_51] Batch remove elements
            foreach (var element in elementsToRemove)
            {
                // BUG-1602: Khôi phục Fill gốc trước khi xóa (đồng bộ với BUG-1601)
                RestoreOriginalFillForElement(element);
                
                _canvas.Children.Remove(element);
                erasedElements.Add(element);
                
                // Fire event for each erased element
                ObjectErased?.Invoke(this, new ObjectErasedEventArgs(element));
            }

            // [LOI_VID_51] Quét và loại bỏ ghost artifacts (micro-elements < 3x3px)
            // sau khi tẩy xong — những mảnh vụn nhỏ còn sót lại
            foreach (UIElement child in _canvas.Children)
            {
                if (IsSystemElement(child)) continue;

                var bounds = GetElementBounds(child);
                if (bounds.Width < GHOST_ARTIFACT_THRESHOLD 
                    && bounds.Height < GHOST_ARTIFACT_THRESHOLD
                    && bounds.Width > 0 && bounds.Height > 0)
                {
                    // Nằm trong hoặc gần vùng tẩy → ghost artifact
                    if (expandedArea.IntersectsWith(bounds))
                    {
                        ghostArtifacts.Add(child);
                    }
                }
            }

            // Xóa ghost artifacts
            foreach (var ghost in ghostArtifacts)
            {
                RestoreOriginalFillForElement(ghost);
                _canvas.Children.Remove(ghost);
                erasedElements.Add(ghost);
            }

            // [LOI_VID_51] InvalidateVisual chỉ khi có ghost artifacts được xóa
            // (theo phản biện ThS. Phạm Quốc Đạt: không gọi mỗi lần tẩy)
            if (ghostArtifacts.Count > 0)
            {
                _canvas.InvalidateVisual();
                Log.Debug("[LOI_VID_51] Cleaned {Count} ghost artifacts, InvalidateVisual called",
                    ghostArtifacts.Count);
            }
            
            sw.Stop();
            if (erasedElements.Count > 0)
            {
                Log.Information(
                    "[LOI_VID_51] EraseByDrag: {Count} elements (incl. {Ghosts} ghosts) in {Ms}ms " +
                    "(area={W:F0}x{H:F0}, dpi={Dpi:F1}, margin={Margin:F1}px)",
                    erasedElements.Count, ghostArtifacts.Count, sw.ElapsedMilliseconds,
                    area.Width, area.Height, dpiScale, hitMargin);
            }
            
            return erasedElements;
        }
        
        /// <summary>
        /// Clears all objects from the canvas
        /// </summary>
        /// <returns>List of cleared elements</returns>
        public List<UIElement> ClearAll()
        {
            var clearedElements = new List<UIElement>();
            var elementsToRemove = new List<UIElement>();
            
            foreach (UIElement child in _canvas.Children)
            {
                // Skip system UI elements
                if (IsSystemElement(child))
                    continue;
                
                elementsToRemove.Add(child);
            }
            
            // Remove elements
            foreach (var element in elementsToRemove)
            {
                _canvas.Children.Remove(element);
                clearedElements.Add(element);
            }
            
            System.Diagnostics.Debug.WriteLine($"🗑️ Cleared all: {clearedElements.Count} elements");
            
            // Fire event
            AllCleared?.Invoke(this, EventArgs.Empty);
            
            return clearedElements;
        }
        
        /// <summary>
        /// Sets the eraser size
        /// </summary>
        /// <param name="size">The eraser size (10-100)</param>
        public void SetEraserSize(int size)
        {
            size = Math.Clamp(size, MIN_ERASER_SIZE, MAX_ERASER_SIZE);
            
            if (_eraserSize == size)
                return;
            
            var oldSize = _eraserSize;
            _eraserSize = size;
            
            // Update preview if visible
            if (_eraserPreview != null)
            {
                _eraserPreview.Width = size;
                _eraserPreview.Height = size;
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ Eraser size changed: {oldSize} → {size}");
            
            // Fire event
            SettingsChanged?.Invoke(this, new EraserSettingsChangedEventArgs(_eraserMode, size));
        }
        
        /// <summary>
        /// Shows the eraser cursor preview at specified position
        /// </summary>
        /// <param name="position">The position to show the preview</param>
        public void ShowEraserPreview(Point position)
        {
            if (_eraserPreview == null)
            {
                CreateEraserPreview();
            }
            
            if (_eraserPreview != null)
            {
                Canvas.SetLeft(_eraserPreview, position.X - _eraserSize / 2);
                Canvas.SetTop(_eraserPreview, position.Y - _eraserSize / 2);
                _eraserPreview.Visibility = Visibility.Visible;
            }
        }
        
        /// <summary>
        /// Hides the eraser cursor preview
        /// </summary>
        public void HideEraserPreview()
        {
            if (_eraserPreview != null)
            {
                _eraserPreview.Visibility = Visibility.Collapsed;
            }
        }
        
        /// <summary>
        /// Updates the eraser preview position
        /// </summary>
        /// <param name="position">The new position</param>
        public void UpdateEraserPreview(Point position)
        {
            if (_eraserPreview == null)
            {
                CreateEraserPreview();
            }
            if (_eraserPreview != null)
            {
                Canvas.SetLeft(_eraserPreview, position.X - _eraserSize / 2.0);
                Canvas.SetTop(_eraserPreview, position.Y - _eraserSize / 2.0);
                if (_eraserPreview.Visibility != Visibility.Visible)
                {
                    _eraserPreview.Visibility = Visibility.Visible;
                }
            }
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// BUG-1601: Đặt Fill bán trong suốt cho các Shape rỗng (Fill=null hoặc Transparent)
        /// để WPF HitTest có thể phát hiện khi ngón tay chạm vào vùng lòng của hình vẽ.
        /// Fill này có alpha=1/255 (0.39% opacity), mắt thường hoàn toàn không nhìn thấy
        /// kể cả trên màn hình IFP 500 nits.
        /// </summary>
        /// <summary>
        /// BUG-1601: Đặt Fill bán trong suốt cho các Shape rỗng (Fill=null hoặc Transparent)
        /// để WPF HitTest có thể phát hiện khi ngón tay chạm vào vùng lòng của hình vẽ.
        /// Fill này có alpha=1/255 (0.39% opacity), mắt thường hoàn toàn không nhìn thấy
        /// kể cả trên màn hình IFP 500 nits.
        /// </summary>
        private void SetInvisibleFillForEmptyShapes()
        {
            _modifiedShapes.Clear();
            
            foreach (UIElement child in _canvas.Children)
            {
                if (IsSystemElement(child))
                    continue;

                if (child is Shape shape && shape != _eraserPreview)
                {
                    bool isEmpty = shape.Fill == null
                                || (shape.Fill is SolidColorBrush scb && scb.Color.A == 0);
                    
                    if (isEmpty)
                    {
                        _modifiedShapes.Add((shape, shape.Fill));
                        shape.Fill = _invisibleFill;
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"🎨 BUG-1601: Set invisible fill for {_modifiedShapes.Count} empty shapes");
        }
        
        /// <summary>
        /// BUG-1601: Khôi phục Fill gốc cho tất cả Shape đã bị gán Fill ẩn.
        /// Được gọi khi: (a) tắt Eraser, (b) trước Save/Export canvas.
        /// </summary>
        public void RestoreOriginalFills()
        {
            foreach (var (shape, originalFill) in _modifiedShapes)
            {
                // Chỉ khôi phục cho Shape chưa bị xóa khỏi Canvas
                if (_canvas.Children.Contains(shape))
                {
                    shape.Fill = originalFill;
                }
            }
            
            var count = _modifiedShapes.Count;
            _modifiedShapes.Clear();
            
            if (count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"🎨 BUG-1601: Restored original fill for {count} shapes");
            }
        }
        
        /// <summary>
        /// BUG-1601: Khôi phục Fill gốc cho một Shape cụ thể trước khi xóa nó.
        /// Đảm bảo khi Undo add lại Shape, Fill hiển thị đúng (null/Transparent)
        /// thay vì Fill ẩn alpha=1.
        /// </summary>
        private void RestoreOriginalFillForElement(UIElement element)
        {
            if (element is Shape shape)
            {
                for (int i = _modifiedShapes.Count - 1; i >= 0; i--)
                {
                    if (_modifiedShapes[i].shape == shape)
                    {
                        shape.Fill = _modifiedShapes[i].originalFill;
                        _modifiedShapes.RemoveAt(i);
                        System.Diagnostics.Debug.WriteLine(
                            $"🎨 BUG-1601: Restored fill for {shape.GetType().Name} before erase");
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Creates the eraser cursor preview
        /// </summary>
        private void CreateEraserPreview()
        {
            _eraserPreview = new Ellipse
            {
                Width = _eraserSize,
                Height = _eraserSize,
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0)),
                IsHitTestVisible = false,
                Visibility = Visibility.Collapsed
            };
            
            _canvas.Children.Add(_eraserPreview);
            Canvas.SetZIndex(_eraserPreview, 10000); // Always on top
        }
        
        /// <summary>
        /// Checks if an element is a system UI element or background layer (should not be erased)
        /// </summary>
        /// <param name="element">The element to check</param>
        /// <returns>True if system element, false otherwise</returns>
        private bool IsSystemElement(UIElement element)
        {
            if (element == null) return true;

            // Skip eraser preview
            if (element == _eraserPreview)
                return true;

            // ✅ QC_4.2_BACKGROUND_ERASER_PROTECT: Bảo vệ các lớp nền (màu nền, lưới ô ly, watermark, background canvas)
            if (SelectionManager.IsBackgroundElement(element, Rect.Empty, _canvas.ActualWidth, _canvas.ActualHeight))
                return true;

            // ✅ QC_4.2_SYSTEM_UI_PROTECT: Bỏ qua toàn bộ control hệ thống (SelectionBox, ContextToolbar, Picker, Keyboard, Spotlight, v.v.)
            if (Panel.GetZIndex(element) >= ZIndexConstants.SystemUIBase)
                return true;

            if (element is FrameworkElement fe)
            {
                // Kiểm tra theo Tag
                if (fe.Tag is string tag)
                {
                    if (tag == "BackgroundLayer" || 
                        tag == "SelectionBox" || 
                        tag == "ContextToolbar" || 
                        tag == "ThicknessPicker" || 
                        tag == "ColorPicker" || 
                        tag == "MoreMenu" || 
                        tag == "FloatingTouchKeyboard" || 
                        tag == "SmartStatusBadge" ||
                        tag == "DragHandle" ||
                        tag == "ResizeHandle" ||
                        tag == "TableContainer" || 
                        tag == "TextBoxContainer")
                    {
                        return true;
                    }
                }

                // Kiểm tra theo Name
                if (!string.IsNullOrEmpty(fe.Name))
                {
                    if (fe.Name.Contains("Background", StringComparison.OrdinalIgnoreCase) ||
                        fe.Name.Contains("Selection", StringComparison.OrdinalIgnoreCase) ||
                        fe.Name.Contains("Toolbar", StringComparison.OrdinalIgnoreCase) ||
                        fe.Name.Contains("Welcome", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            // BUG-1602: Skip drag erase preview rectangle
            if (element is Rectangle rect && rect.IsHitTestVisible == false 
                && rect.StrokeDashArray != null && rect.StrokeDashArray.Count > 0)
            {
                return true;
            }

            // ✅ QC_4.2_TABLE_ERASER_PROTECT (T13): Bảo vệ Table container khỏi bị Tẩy xóa
            if (IsTableContainer(element))
                return true;

            return false;
        }

        /// <summary>
        /// ✅ QC_4.2_WIDGET_ERASER_PROTECT (T13+TB-5): Kiểm tra element có thuộc Widget container hay không.
        /// Bảng + Hộp Văn Bản được bảo vệ khỏi Eraser — GV phải dùng nút ❌ hoặc Selection+Delete để xóa.
        /// </summary>
        /// <param name="element">Phần tử cần kiểm tra</param>
        /// <returns>true nếu element là hoặc thuộc Widget container được bảo vệ</returns>
        private bool IsTableContainer(UIElement element)
        {
            // Kiểm tra trực tiếp: Tag = "TableContainer" hoặc "TextBoxContainer"
            if (element is FrameworkElement fe)
            {
                // ✅ QC_4.2_WIDGET_ERASER_PROTECT: Bảo vệ TẤT CẢ Canvas Widget có DragHandle
                if (fe.Tag is string tag && (tag == "TableContainer" || tag == "TextBoxContainer"))
                    return true;
            }

            // Kiểm tra parent: element con bên trong Widget (Border, Grid, TextBlock...)
            if (element is System.Windows.DependencyObject depObj)
            {
                var parent = System.Windows.Media.VisualTreeHelper.GetParent(depObj);
                while (parent != null && !(parent is Canvas))
                {
                    if (parent is FrameworkElement parentFe 
                        && parentFe.Tag is string parentTag 
                        && (parentTag == "TableContainer" || parentTag == "TextBoxContainer"))
                    {
                        return true;
                    }
                    parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
                }
            }

            return false;
        }
        
        /// <summary>
        /// Checks if an element intersects with an area
        /// </summary>
        /// <param name="element">The element to check</param>
        /// <param name="area">The area to check intersection with</param>
        /// <returns>True if intersects, false otherwise</returns>
        private bool ElementIntersectsArea(UIElement element, Rect area)
        {
            // Get element bounds
            var elementBounds = GetElementBounds(element);
            
            // Check intersection
            return area.IntersectsWith(elementBounds);
        }
        
        /// <summary>
        /// IMP-1604: Tính bounds chính xác sử dụng BoundsHelper utility.
        /// Thay thế logic inline từ BUG-1602 bằng hàm tổng quát.
        /// </summary>
        /// <param name="element">The element to get bounds for</param>
        /// <returns>The bounding rectangle in Canvas coordinates</returns>
        private Rect GetElementBounds(UIElement element)
        {
            return BoundsHelper.GetAbsoluteBounds(element, _canvas);
        }
        
        #endregion

        // ═══════════════════════════════════════════════════════
        //  [LOI_VID_51] DPI UTILITIES
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// [LOI_VID_51] Lấy hệ số DPI scale hiện tại.
        /// DPI 96 = 1.0, DPI 144 (150%) = 1.5, DPI 192 (200%) = 2.0
        /// </summary>
        private double GetDpiScale()
        {
            try
            {
                var source = PresentationSource.FromVisual(_canvas);
                if (source?.CompositionTarget != null)
                {
                    return source.CompositionTarget.TransformToDevice.M11;
                }
            }
            catch { /* fallback */ }
            return 1.0; // Fallback: DPI 96 (100%)
        }
    }
    
    #region Event Args
    
    /// <summary>
    /// Event arguments for object erased events
    /// </summary>
    public class ObjectErasedEventArgs : EventArgs
    {
        public UIElement Element { get; }
        
        public ObjectErasedEventArgs(UIElement element)
        {
            Element = element;
        }
    }
    
    /// <summary>
    /// Event arguments for eraser settings changed events
    /// </summary>
    public class EraserSettingsChangedEventArgs : EventArgs
    {
        public EraserMode Mode { get; }
        public int Size { get; }
        
        public EraserSettingsChangedEventArgs(EraserMode mode, int size)
        {
            Mode = mode;
            Size = size;
        }
    }
    
    #endregion
}
