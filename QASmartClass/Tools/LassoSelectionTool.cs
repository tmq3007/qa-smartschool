using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartTouch.Tools
{
    /// <summary>
    /// Công cụ chọn vùng tự do (Lasso Selection)
    /// Cho phép người dùng vẽ đường tự do để chọn nhiều đối tượng cùng lúc
    /// </summary>
    public class LassoSelectionTool
    {
        #region Fields

        private Canvas _canvas;
        private List<Point> _lassoPoints;
        private Polyline? _lassoVisual;
        private bool _isDrawing;
        private Point _startPoint;

        private bool _isProcessingCompletion;

        #endregion

        #region Events

        /// <summary>
        /// Event được trigger khi hoàn thành việc chọn vùng
        /// </summary>
        public event EventHandler<List<UIElement>>? SelectionCompleted;

        #endregion

        #region Properties

        /// <summary>
        /// Trạng thái kích hoạt của lasso tool
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Trạng thái đang vẽ nét khoanh vùng Lasso
        /// </summary>
        public bool IsDrawing => _isDrawing;

        /// <summary>
        /// Delegate kiểm tra xem có được phép bắt đầu vẽ Lasso tại vị trí point / source này không.
        /// Trả về false nếu chạm vào SelectionBox, Adorner, Toolbar,... để nhường quyền tương tác kéo di chuyển/xoay/co giãn.
        /// </summary>
        public Func<Point, object, bool>? CanStartSelectionPredicate { get; set; }

        /// <summary>
        /// Action được gọi ngay trước khi bắt đầu vẽ Lasso mới (khi CanStartSelectionPredicate trả về true).
        /// Dùng để Deselect vùng chọn cũ nếu người dùng bắt đầu khoanh vùng mới ở chỗ trống.
        /// </summary>
        public Action? BeforeSelectionStarted { get; set; }

        #endregion

        #region Constructor

        public LassoSelectionTool(Canvas canvas)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _lassoPoints = new List<Point>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Kích hoạt lasso mode
        /// </summary>
        public void Activate()
        {
            if (IsActive) return;

            IsActive = true;
            _isDrawing = false;
            _isProcessingCompletion = false;
            
            // 1. Đăng ký Mouse Events
            _canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            _canvas.MouseMove += Canvas_MouseMove;
            _canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;

            // 2. Đăng ký Touch Events (Cho ngón tay trên màn hình SMART TOUCH)
            _canvas.TouchDown += Canvas_TouchDown;
            _canvas.TouchMove += Canvas_TouchMove;
            _canvas.TouchUp += Canvas_TouchUp;

            // 3. Đăng ký Stylus Events (Cho Bút cảm ứng tương tác phòng học)
            _canvas.StylusDown += Canvas_StylusDown;
            _canvas.StylusMove += Canvas_StylusMove;
            _canvas.StylusUp += Canvas_StylusUp;

            // 4. Vô hiệu hóa Stylus Press-And-Hold để nét vẽ ăn ngay lập tức không bị trễ
            Stylus.SetIsPressAndHoldEnabled(_canvas, false);

            _canvas.Cursor = Cursors.Cross;
            System.Diagnostics.Debug.WriteLine("✅ Lasso Tool activated with Mouse, Touch & Stylus support");
        }

        /// <summary>
        /// Tắt lasso mode
        /// </summary>
        public void Deactivate()
        {
            if (!IsActive) return;

            IsActive = false;
            
            _canvas.MouseLeftButtonDown -= Canvas_MouseLeftButtonDown;
            _canvas.MouseMove -= Canvas_MouseMove;
            _canvas.MouseLeftButtonUp -= Canvas_MouseLeftButtonUp;

            _canvas.TouchDown -= Canvas_TouchDown;
            _canvas.TouchMove -= Canvas_TouchMove;
            _canvas.TouchUp -= Canvas_TouchUp;

            _canvas.StylusDown -= Canvas_StylusDown;
            _canvas.StylusMove -= Canvas_StylusMove;
            _canvas.StylusUp -= Canvas_StylusUp;

            _canvas.Cursor = Cursors.Arrow;

            ReleaseAllCaptures();
            CleanupVisual();
            System.Diagnostics.Debug.WriteLine("❌ Lasso Tool deactivated");
        }

        /// <summary>
        /// Kiểm tra xem có thể bắt đầu vẽ Lasso tại tọa độ và nguồn sự kiện được chỉ định không
        /// </summary>
        public bool CanStart(Point point, object? originalSource)
        {
            if (!IsActive) return false;
            if (CanStartSelectionPredicate != null && !CanStartSelectionPredicate(point, originalSource))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Khởi tạo nét vẽ Lasso tại tọa độ bắt đầu (Hỗ trợ gọi trực tiếp từ tầng Preview Hardware)
        /// </summary>
        public void StartLasso(Point startPoint)
        {
            BeforeSelectionStarted?.Invoke();

            _isDrawing = true;
            _startPoint = startPoint;
            _lassoPoints.Clear();
            _lassoPoints.Add(_startPoint);

            CreateLassoVisual();
            System.Diagnostics.Debug.WriteLine($"🎯 Lasso started at ({_startPoint.X:F0}, {_startPoint.Y:F0})");
        }

        /// <summary>
        /// Cập nhật tọa độ nét vẽ Lasso khi di chuyển (Hỗ trợ gọi trực tiếp từ tầng Preview Hardware)
        /// </summary>
        public void MoveLasso(Point currentPoint)
        {
            if (!_isDrawing) return;

            if (_lassoPoints.Count == 0 || GetDistance(_lassoPoints.Last(), currentPoint) > 4)
            {
                _lassoPoints.Add(currentPoint);
                UpdateLassoVisual();
            }
        }

        /// <summary>
        /// Hoàn tất nét vẽ Lasso và thực hiện gom chọn đối tượng (Hỗ trợ gọi trực tiếp từ tầng Preview Hardware)
        /// </summary>
        public void EndLasso()
        {
            if (!_isDrawing) return;

            _isDrawing = false;
            CompleteLassoSelection();
        }

        private void ReleaseAllCaptures()
        {
            try
            {
                if (_canvas.IsMouseCaptured)
                {
                    _canvas.ReleaseMouseCapture();
                }
                _canvas.ReleaseAllTouchCaptures();
            }
            catch { /* Safe release */ }
        }

        /// <summary>
        /// Lấy bản sao danh sách các điểm đỉnh đa giác của vùng chọn Lasso vừa vẽ
        /// </summary>
        public List<Point> GetLassoPoints()
        {
            return new List<Point>(_lassoPoints);
        }

        #endregion

        #region Private Methods - Mouse & Touch Events

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPoint = e.GetPosition(_canvas);

            if (CanStartSelectionPredicate != null && !CanStartSelectionPredicate(clickPoint, e.OriginalSource))
            {
                return;
            }

            StartLasso(clickPoint);
            _canvas.CaptureMouse();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDrawing) return;
            Point currentPoint = e.GetPosition(_canvas);
            MoveLasso(currentPoint);
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawing) return;

            if (_canvas.IsMouseCaptured)
            {
                _canvas.ReleaseMouseCapture();
            }

            EndLasso();
        }

        private void Canvas_TouchDown(object? sender, TouchEventArgs e)
        {
            Point clickPoint = e.GetTouchPoint(_canvas).Position;

            if (CanStartSelectionPredicate != null && !CanStartSelectionPredicate(clickPoint, e.OriginalSource))
            {
                return;
            }

            StartLasso(clickPoint);
            _canvas.CaptureTouch(e.TouchDevice);
            e.Handled = true;
        }

        private void Canvas_TouchMove(object? sender, TouchEventArgs e)
        {
            if (!_isDrawing) return;
            Point currentPoint = e.GetTouchPoint(_canvas).Position;
            MoveLasso(currentPoint);
            e.Handled = true;
        }

        private void Canvas_TouchUp(object? sender, TouchEventArgs e)
        {
            if (!_isDrawing) return;

            try
            {
                if (e.TouchDevice.Captured == _canvas)
                {
                    _canvas.ReleaseTouchCapture(e.TouchDevice);
                }
            }
            catch { }

            EndLasso();
            e.Handled = true;
        }

        private void Canvas_StylusDown(object sender, StylusDownEventArgs e)
        {
            Point clickPoint = e.GetPosition(_canvas);

            if (CanStartSelectionPredicate != null && !CanStartSelectionPredicate(clickPoint, e.OriginalSource))
            {
                return;
            }

            StartLasso(clickPoint);
            _canvas.CaptureStylus();
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine($"🎯 Lasso Stylus started at ({clickPoint.X:F0}, {clickPoint.Y:F0})");
        }

        private void Canvas_StylusMove(object sender, StylusEventArgs e)
        {
            if (!_isDrawing) return;
            Point currentPoint = e.GetPosition(_canvas);
            MoveLasso(currentPoint);
            e.Handled = true;
        }

        private void Canvas_StylusUp(object sender, StylusEventArgs e)
        {
            if (!_isDrawing) return;

            try
            {
                if (_canvas.IsStylusCaptured)
                {
                    _canvas.ReleaseStylusCapture();
                }
            }
            catch { }

            EndLasso();
            e.Handled = true;
        }

        private void CompleteLassoSelection()
        {
            // Chống thực thi trùng lặp (Double-trigger guard)
            if (_isProcessingCompletion) return;
            _isProcessingCompletion = true;

            try
            {
                // Kiểm tra lasso có đủ lớn không (tránh click nhầm)
                var bounds = GetLassoBounds();
                if (bounds.Width < 5 || bounds.Height < 5)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Lasso too small, ignoring");
                    CleanupVisual();
                    return;
                }

                // Đóng polygon
                if (_lassoPoints.Count > 2)
                {
                    _lassoPoints.Add(_startPoint);
                    UpdateLassoVisual();

                    // Tìm các đối tượng trong vùng lasso
                    var selectedElements = FindElementsInLasso();

                    System.Diagnostics.Debug.WriteLine($"✅ Lasso completed: Found {selectedElements.Count} elements");

                    // Trigger event
                    SelectionCompleted?.Invoke(this, selectedElements);
                }
            }
            finally
            {
                // Xóa visual và reset cờ xử lý sau khiDispatcher xử lý xong
                System.Threading.Tasks.Task.Delay(150).ContinueWith(_ =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CleanupVisual();
                        _isProcessingCompletion = false;
                    });
                });
            }
        }

        #endregion

        #region Private Methods - Visual Management

        private void CreateLassoVisual()
        {
            _lassoVisual = new Polyline
            {
                // ✅ G1.1: Vàng Hổ Phách (#FFC107) nổi bật trên mọi màu mực & nền bảng
                Stroke = new SolidColorBrush(Color.FromRgb(255, 193, 7)), // #FFC107 - Amber Gold
                StrokeThickness = 2.5,
                StrokeDashArray = new DoubleCollection { 6, 3 },
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Fill = new SolidColorBrush(Color.FromArgb(25, 255, 193, 7)), // Semi-transparent amber fill
                IsHitTestVisible = false // 🛑 NGĂN CHẶN CƯỚP QUYỀN HIT-TEST TRÊN MÀN HÌNH TƯƠNG TÁC
            };

            // ✅ G1.1: Marching Ants animation — nét đứt chuyển động
            var marchingAnts = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 18, // StrokeDashArray cycle: (6 + 3) * 2 = 18
                Duration = TimeSpan.FromSeconds(0.8),
                RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
            };
            _lassoVisual.BeginAnimation(Polyline.StrokeDashOffsetProperty, marchingAnts);

            Panel.SetZIndex(_lassoVisual, 10000); // Hiển thị trên cùng
            _canvas.Children.Add(_lassoVisual);
        }

        private void UpdateLassoVisual()
        {
            if (_lassoVisual != null)
            {
                _lassoVisual.Points.Clear();
                foreach (var point in _lassoPoints)
                {
                    _lassoVisual.Points.Add(point);
                }
            }
        }

        private void CleanupVisual()
        {
            if (_lassoVisual != null && _canvas.Children.Contains(_lassoVisual))
            {
                _canvas.Children.Remove(_lassoVisual);
                _lassoVisual = null;
            }
            _lassoPoints.Clear();
        }

        #endregion

        #region Private Methods - Element Detection

        private List<UIElement> FindElementsInLasso()
        {
            var selectedElements = new List<UIElement>();

            foreach (UIElement element in _canvas.Children)
            {
                // Bỏ qua lasso visual
                if (element == _lassoVisual)
                    continue;

                if (element.Visibility != Visibility.Visible)
                    continue;

                var typeName = element.GetType().Name;

                // 🛑 LOẠI TRỪ 1: Tuyệt đối không chọn InkPresenter (lớp vẽ mực trực tiếp của hệ thống)
                if (element is InkPresenter || typeName.Equals("InkPresenter", StringComparison.OrdinalIgnoreCase) || typeName.Contains("InkPresenter"))
                    continue;
                
                // 🛑 LOẠI TRỪ 2: Bỏ qua các UI controls của hệ thống và Background layers (nền bảng, lưới)
                if (element is FrameworkElement feCheck)
                {
                    string tagStr = feCheck.Tag?.ToString() ?? "";
                    if (tagStr.Equals("BackgroundLayer", StringComparison.OrdinalIgnoreCase) ||
                        tagStr.Contains("background", StringComparison.OrdinalIgnoreCase) ||
                        tagStr.Contains("Background", StringComparison.OrdinalIgnoreCase) ||
                        tagStr.Contains("grid", StringComparison.OrdinalIgnoreCase) ||
                        tagStr.Contains("Grid", StringComparison.OrdinalIgnoreCase))
                        continue;

                    Rect elBounds = GetElementCanvasBounds(feCheck);
                    if (QASmartTouch.Services.SelectionManager.IsBackgroundElement(feCheck, elBounds, _canvas.ActualWidth, _canvas.ActualHeight))
                        continue;
                }

                if (typeName.Contains("Adorner") ||
                    typeName.Contains("Toolbar") ||
                    typeName.Contains("ContextToolbar") ||
                    typeName.Contains("SelectionBox") ||
                    typeName.Contains("ThicknessPicker") ||
                    typeName.Contains("ColorPicker") ||
                    typeName.Contains("MoreMenu") ||
                    typeName.Contains("Picker") ||
                    typeName.Contains("Menu") ||
                    typeName.Contains("Preview") ||
                    typeName.Contains("Cursor"))
                    continue;

                // Kiểm tra element có nằm trong lasso không
                if (IsElementInLasso(element))
                {
                    selectedElements.Add(element);
                    System.Diagnostics.Debug.WriteLine($"   ✓ Selected: {typeName}");
                }
            }

            return selectedElements;
        }

        private static Point TransformToCanvasPoint(Point pt, GeneralTransform? transform, double offsetX, double offsetY)
        {
            if (transform != null)
            {
                try
                {
                    return transform.Transform(pt);
                }
                catch { }
            }
            return new Point(offsetX + pt.X, offsetY + pt.Y);
        }

        private static bool DoSegmentsIntersect(Point p1, Point p2, Point q1, Point q2)
        {
            static double Cross(Point a, Point b, Point c) =>
                (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

            static bool OnSegment(Point a, Point b, Point p) =>
                p.X >= Math.Min(a.X, b.X) - 1e-6 && p.X <= Math.Max(a.X, b.X) + 1e-6 &&
                p.Y >= Math.Min(a.Y, b.Y) - 1e-6 && p.Y <= Math.Max(a.Y, b.Y) + 1e-6;

            double d1 = Cross(q1, q2, p1);
            double d2 = Cross(q1, q2, p2);
            double d3 = Cross(p1, p2, q1);
            double d4 = Cross(p1, p2, q2);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
                return true;

            if (Math.Abs(d1) <= 1e-6 && OnSegment(q1, q2, p1)) return true;
            if (Math.Abs(d2) <= 1e-6 && OnSegment(q1, q2, p2)) return true;
            if (Math.Abs(d3) <= 1e-6 && OnSegment(p1, p2, q1)) return true;
            if (Math.Abs(d4) <= 1e-6 && OnSegment(p1, p2, q2)) return true;

            return false;
        }

        private bool IsSegmentInOrIntersectingLasso(Point p1, Point p2)
        {
            if (_lassoPoints.Count < 3) return false;

            if (IsPointInPolygon(p1, _lassoPoints) || IsPointInPolygon(p2, _lassoPoints))
                return true;

            Point mid = new Point((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5);
            if (IsPointInPolygon(mid, _lassoPoints))
                return true;

            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len > 12.0)
            {
                int steps = Math.Min(64, Math.Max(2, (int)Math.Ceiling(len / 12.0)));
                for (int s = 1; s < steps; s++)
                {
                    double t = (double)s / steps;
                    Point sample = new Point(p1.X + dx * t, p1.Y + dy * t);
                    if (IsPointInPolygon(sample, _lassoPoints))
                        return true;
                }
            }

            for (int i = 0; i < _lassoPoints.Count - 1; i++)
            {
                if (DoSegmentsIntersect(p1, p2, _lassoPoints[i], _lassoPoints[i + 1]))
                    return true;
            }
            if (DoSegmentsIntersect(p1, p2, _lassoPoints[_lassoPoints.Count - 1], _lassoPoints[0]))
                return true;

            return false;
        }

        private bool IsElementInLasso(UIElement element)
        {
            try
            {
                // 1. Direct check for Polyline (freehand strokes)
                if (element is Polyline polyline && polyline.Points.Count > 0)
                {
                    GeneralTransform? transform = null;
                    try
                    {
                        transform = polyline.TransformToAncestor(_canvas);
                    }
                    catch { }

                    double pLeft = Canvas.GetLeft(polyline);
                    double pTop = Canvas.GetTop(polyline);
                    if (double.IsNaN(pLeft)) pLeft = 0;
                    if (double.IsNaN(pTop)) pTop = 0;

                    if (polyline.Points.Count == 1)
                    {
                        Point pt0 = TransformToCanvasPoint(polyline.Points[0], transform, pLeft, pTop);
                        return IsPointInPolygon(pt0, _lassoPoints);
                    }

                    for (int i = 0; i < polyline.Points.Count - 1; i++)
                    {
                        Point c1 = TransformToCanvasPoint(polyline.Points[i], transform, pLeft, pTop);
                        Point c2 = TransformToCanvasPoint(polyline.Points[i + 1], transform, pLeft, pTop);
                        if (IsSegmentInOrIntersectingLasso(c1, c2))
                            return true;
                    }

                    return false;
                }

                // 1b. Direct check for Polygon (2D polygons: Triangle, Star, Pentagon, etc.)
                if (element is Polygon polygon && polygon.Points.Count > 0)
                {
                    GeneralTransform? transform = null;
                    try
                    {
                        transform = polygon.TransformToAncestor(_canvas);
                    }
                    catch { }

                    double pLeft = Canvas.GetLeft(polygon);
                    double pTop = Canvas.GetTop(polygon);
                    if (double.IsNaN(pLeft)) pLeft = 0;
                    if (double.IsNaN(pTop)) pTop = 0;

                    for (int i = 0; i < polygon.Points.Count; i++)
                    {
                        Point c1 = TransformToCanvasPoint(polygon.Points[i], transform, pLeft, pTop);
                        Point c2 = TransformToCanvasPoint(polygon.Points[(i + 1) % polygon.Points.Count], transform, pLeft, pTop);
                        if (IsSegmentInOrIntersectingLasso(c1, c2))
                            return true;
                    }
                    // Fall through to step 4 to also check center & bounding box points like 3D shapes
                }

                // 2. Direct check for Path (2D geometry shapes & smooth Bezier strokes)
                if (element is System.Windows.Shapes.Path path && path.Data != null)
                {
                    GeneralTransform? transform = null;
                    try
                    {
                        transform = path.TransformToAncestor(_canvas);
                    }
                    catch { }

                    double pLeft = Canvas.GetLeft(path);
                    double pTop = Canvas.GetTop(path);
                    if (double.IsNaN(pLeft)) pLeft = 0;
                    if (double.IsNaN(pTop)) pTop = 0;

                    bool hasClosedFigure = false;
                    var flattened = path.Data.GetFlattenedPathGeometry();
                    if (flattened != null)
                    {
                        foreach (var figure in flattened.Figures)
                        {
                            if (figure.IsClosed) hasClosedFigure = true;

                            Point startPt = TransformToCanvasPoint(figure.StartPoint, transform, pLeft, pTop);
                            if (IsPointInPolygon(startPt, _lassoPoints))
                                return true;

                            Point prevPt = startPt;
                            foreach (var segment in figure.Segments)
                            {
                                if (segment is PolyLineSegment polySeg)
                                {
                                    foreach (var pt in polySeg.Points)
                                    {
                                        Point currPt = TransformToCanvasPoint(pt, transform, pLeft, pTop);
                                        if (IsSegmentInOrIntersectingLasso(prevPt, currPt))
                                            return true;
                                        prevPt = currPt;
                                    }
                                }
                                else if (segment is LineSegment lineSeg)
                                {
                                    Point currPt = TransformToCanvasPoint(lineSeg.Point, transform, pLeft, pTop);
                                    if (IsSegmentInOrIntersectingLasso(prevPt, currPt))
                                        return true;
                                    prevPt = currPt;
                                }
                            }

                            if (figure.IsClosed && IsSegmentInOrIntersectingLasso(prevPt, startPt))
                                return true;
                        }
                    }

                    bool isShapePath = !string.IsNullOrEmpty(path.Tag?.ToString()) || hasClosedFigure;
                    if (!isShapePath)
                        return false;
                }

                // 3. Direct check for Line (2D lines: mLine, mDashedLine, ruler lines)
                if (element is System.Windows.Shapes.Line line)
                {
                    GeneralTransform? transform = null;
                    try
                    {
                        transform = line.TransformToAncestor(_canvas);
                    }
                    catch { }

                    double lLeft = Canvas.GetLeft(line);
                    double lTop = Canvas.GetTop(line);
                    if (double.IsNaN(lLeft)) lLeft = 0;
                    if (double.IsNaN(lTop)) lTop = 0;

                    Point p1 = TransformToCanvasPoint(new Point(line.X1, line.Y1), transform, lLeft, lTop);
                    Point p2 = TransformToCanvasPoint(new Point(line.X2, line.Y2), transform, lLeft, lTop);

                    return IsSegmentInOrIntersectingLasso(p1, p2);
                }

                // 4. Check bounding box reference points & edges (2D/3D shapes, text boxes, images, controls)
                Rect bounds = GetElementCanvasBounds(element);

                if (bounds.Width <= 0 || bounds.Height <= 0 || bounds.IsEmpty)
                    return false;

                if (_canvas.ActualWidth > 100 && _canvas.ActualHeight > 100)
                {
                    if (bounds.Width >= _canvas.ActualWidth * 0.8 && bounds.Height >= _canvas.ActualHeight * 0.8)
                        return false;
                }

                Point[] testPoints = new Point[]
                {
                    new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2),
                    bounds.TopLeft,
                    bounds.TopRight,
                    bounds.BottomLeft,
                    bounds.BottomRight,
                    new Point(bounds.Left + bounds.Width / 2, bounds.Top),
                    new Point(bounds.Left + bounds.Width / 2, bounds.Bottom),
                    new Point(bounds.Left, bounds.Top + bounds.Height / 2),
                    new Point(bounds.Right, bounds.Top + bounds.Height / 2)
                };

                if (testPoints.Any(point => IsPointInPolygon(point, _lassoPoints)))
                    return true;

                if (IsSegmentInOrIntersectingLasso(bounds.TopLeft, bounds.TopRight) ||
                    IsSegmentInOrIntersectingLasso(bounds.TopRight, bounds.BottomRight) ||
                    IsSegmentInOrIntersectingLasso(bounds.BottomRight, bounds.BottomLeft) ||
                    IsSegmentInOrIntersectingLasso(bounds.BottomLeft, bounds.TopLeft))
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Error checking element in lasso: {ex.Message}");
                return false;
            }
        }

        private Rect GetElementCanvasBounds(UIElement element)
        {
            try
            {
                Rect absBounds = QASmartTouch.Helpers.BoundsHelper.GetAbsoluteBounds(element, _canvas);
                if (!absBounds.IsEmpty && absBounds.Width > 0 && absBounds.Height > 0)
                {
                    return absBounds;
                }

                if (element is FrameworkElement fe)
                {
                    double left = Canvas.GetLeft(fe);
                    double top = Canvas.GetTop(fe);

                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;

                    if (element is System.Windows.Shapes.Path path && path.Data != null && !path.Data.Bounds.IsEmpty)
                    {
                        var pBounds = path.Data.Bounds;
                        return new Rect(left + pBounds.Left, top + pBounds.Top, Math.Max(1, pBounds.Width), Math.Max(1, pBounds.Height));
                    }

                    double width = fe.ActualWidth > 0 ? fe.ActualWidth : fe.Width;
                    double height = fe.ActualHeight > 0 ? fe.ActualHeight : fe.Height;

                    if (double.IsNaN(width) || width <= 0) width = fe.RenderSize.Width > 0 ? fe.RenderSize.Width : 20;
                    if (double.IsNaN(height) || height <= 0) height = fe.RenderSize.Height > 0 ? fe.RenderSize.Height : 20;

                    return new Rect(left, top, width, height);
                }
            }
            catch { }
            return Rect.Empty;
        }
        /// <summary>
        /// Ray Casting Algorithm - kiểm tra điểm có nằm trong polygon không
        /// Thuật toán: Vẽ một tia từ điểm test đi về phía phải, đếm số lần cắt polygon
        /// Nếu số lần cắt là lẻ -> điểm nằm trong polygon
        /// </summary>
        public static bool IsPointInPolygon(Point point, List<Point> polygon)
        {
            if (polygon.Count < 3) return false;

            bool inside = false;
            int j = polygon.Count - 1;

            for (int i = 0; i < polygon.Count; i++)
            {
                if ((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y) &&
                    point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) /
                    (polygon[j].Y - polygon[i].Y) + polygon[i].X)
                {
                    inside = !inside;
                }
                j = i;
            }

            return inside;
        }

        #endregion

        #region Private Methods - Utilities

        private double GetDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        private Rect GetLassoBounds()
        {
            if (_lassoPoints.Count == 0)
                return Rect.Empty;

            double minX = _lassoPoints.Min(p => p.X);
            double minY = _lassoPoints.Min(p => p.Y);
            double maxX = _lassoPoints.Max(p => p.X);
            double maxY = _lassoPoints.Max(p => p.Y);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        #endregion
    }
}
