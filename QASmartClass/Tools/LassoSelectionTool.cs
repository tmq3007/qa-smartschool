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

            // 2. Đăng ký Touch Events (Cho ngón tay và bút cảm ứng trên màn hình SMART TOUCH)
            _canvas.TouchDown += Canvas_TouchDown;
            _canvas.TouchMove += Canvas_TouchMove;
            _canvas.TouchUp += Canvas_TouchUp;

            // 3. Vô hiệu hóa Stylus Press-And-Hold để nét vẽ ăn ngay lập tức
            Stylus.SetIsPressAndHoldEnabled(_canvas, false);

            _canvas.Cursor = Cursors.Cross;
            System.Diagnostics.Debug.WriteLine("✅ Lasso Tool activated with Mouse & Touch support");
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

            _canvas.Cursor = Cursors.Arrow;

            ReleaseAllCaptures();
            CleanupVisual();
            System.Diagnostics.Debug.WriteLine("❌ Lasso Tool deactivated");
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

        #endregion

        #region Private Methods - Mouse & Touch Events

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDrawing = true;
            _startPoint = e.GetPosition(_canvas);
            _lassoPoints.Clear();
            _lassoPoints.Add(_startPoint);

            CreateLassoVisual();
            _canvas.CaptureMouse();

            System.Diagnostics.Debug.WriteLine($"🎯 Lasso Mouse started at ({_startPoint.X:F0}, {_startPoint.Y:F0})");
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDrawing) return;

            Point currentPoint = e.GetPosition(_canvas);

            if (_lassoPoints.Count == 0 || GetDistance(_lassoPoints.Last(), currentPoint) > 4)
            {
                _lassoPoints.Add(currentPoint);
                UpdateLassoVisual();
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawing) return;

            _isDrawing = false;
            if (_canvas.IsMouseCaptured)
            {
                _canvas.ReleaseMouseCapture();
            }

            CompleteLassoSelection();
        }

        private void Canvas_TouchDown(object? sender, TouchEventArgs e)
        {
            _isDrawing = true;
            _startPoint = e.GetTouchPoint(_canvas).Position;
            _lassoPoints.Clear();
            _lassoPoints.Add(_startPoint);

            CreateLassoVisual();
            _canvas.CaptureTouch(e.TouchDevice);
            e.Handled = true;

            System.Diagnostics.Debug.WriteLine($"🎯 Lasso Touch started at ({_startPoint.X:F0}, {_startPoint.Y:F0})");
        }

        private void Canvas_TouchMove(object? sender, TouchEventArgs e)
        {
            if (!_isDrawing) return;

            Point currentPoint = e.GetTouchPoint(_canvas).Position;

            if (_lassoPoints.Count == 0 || GetDistance(_lassoPoints.Last(), currentPoint) > 4)
            {
                _lassoPoints.Add(currentPoint);
                UpdateLassoVisual();
            }
            e.Handled = true;
        }

        private void Canvas_TouchUp(object? sender, TouchEventArgs e)
        {
            if (!_isDrawing) return;

            _isDrawing = false;
            try
            {
                if (e.TouchDevice.Captured == _canvas)
                {
                    _canvas.ReleaseTouchCapture(e.TouchDevice);
                }
            }
            catch { }

            CompleteLassoSelection();
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
                Fill = new SolidColorBrush(Color.FromArgb(25, 255, 193, 7)) // Semi-transparent amber fill
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
                
                // Bỏ qua các UI controls của hệ thống và Background layers (nền bảng, lưới)
                if (element is FrameworkElement feCheck)
                {
                    string tagStr = feCheck.Tag?.ToString() ?? "";
                    if (tagStr.Equals("BackgroundLayer", StringComparison.OrdinalIgnoreCase) ||
                        tagStr.Contains("background", StringComparison.OrdinalIgnoreCase) ||
                        tagStr.Contains("Background", StringComparison.OrdinalIgnoreCase) ||
                        tagStr.Contains("grid", StringComparison.OrdinalIgnoreCase) ||
                        tagStr.Contains("Grid", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (QASmartTouch.Services.SelectionManager.IsBackgroundElement(feCheck, Rect.Empty, _canvas.ActualWidth, _canvas.ActualHeight))
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

        private bool IsElementInLasso(UIElement element)
        {
            try
            {
                // ✅ 1. Xử lý trực tiếp cho Polyline (nét vẽ tay)
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

                    // Kiểm tra điểm thực tế trên Canvas
                    foreach (var pt in polyline.Points)
                    {
                        Point canvasPt = transform != null ? transform.Transform(pt) : new Point(pLeft + pt.X, pTop + pt.Y);
                        if (IsPointInPolygon(canvasPt, _lassoPoints))
                            return true;
                    }

                    // Kiểm tra trung điểm các nét nối
                    for (int i = 0; i < polyline.Points.Count - 1; i++)
                    {
                        Point rawMid = new Point((polyline.Points[i].X + polyline.Points[i + 1].X) / 2,
                                                 (polyline.Points[i].Y + polyline.Points[i + 1].Y) / 2);
                        Point canvasMid = transform != null ? transform.Transform(rawMid) : new Point(pLeft + rawMid.X, pTop + rawMid.Y);
                        if (IsPointInPolygon(canvasMid, _lassoPoints))
                            return true;
                    }

                    return false;
                }

                // ✅ 2. Xử lý trực tiếp cho Path (nét vẽ mượt Bezier tạo bởi TouchHandler/DrawingEngine)
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

                    var flattened = path.Data.GetFlattenedPathGeometry();
                    if (flattened != null)
                    {
                        foreach (var figure in flattened.Figures)
                        {
                            Point startPt = transform != null ? transform.Transform(figure.StartPoint) : new Point(pLeft + figure.StartPoint.X, pTop + figure.StartPoint.Y);
                            if (IsPointInPolygon(startPt, _lassoPoints))
                                return true;

                            foreach (var segment in figure.Segments)
                            {
                                if (segment is PolyLineSegment polySeg)
                                {
                                    foreach (var pt in polySeg.Points)
                                    {
                                        Point canvasPt = transform != null ? transform.Transform(pt) : new Point(pLeft + pt.X, pTop + pt.Y);
                                        if (IsPointInPolygon(canvasPt, _lassoPoints))
                                            return true;
                                    }
                                }
                                else if (segment is LineSegment lineSeg)
                                {
                                    Point canvasPt = transform != null ? transform.Transform(lineSeg.Point) : new Point(pLeft + lineSeg.Point.X, pTop + lineSeg.Point.Y);
                                    if (IsPointInPolygon(canvasPt, _lassoPoints))
                                        return true;
                                }
                            }
                        }
                    }
                    return false;
                }

                // ✅ 3. Xử lý trực tiếp cho Line (thước kẻ, đoạn thẳng)
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

                    Point raw1 = new Point(line.X1, line.Y1);
                    Point raw2 = new Point(line.X2, line.Y2);
                    Point rawMid = new Point((line.X1 + line.X2) / 2, (line.Y1 + line.Y2) / 2);

                    Point p1 = transform != null ? transform.Transform(raw1) : new Point(lLeft + line.X1, lTop + line.Y1);
                    Point p2 = transform != null ? transform.Transform(raw2) : new Point(lLeft + line.X2, lTop + line.Y2);
                    Point mid = transform != null ? transform.Transform(rawMid) : new Point(lLeft + rawMid.X, lTop + rawMid.Y);

                    return IsPointInPolygon(p1, _lassoPoints) || 
                           IsPointInPolygon(p2, _lassoPoints) || 
                           IsPointInPolygon(mid, _lassoPoints);
                }

                // ✅ 4. Xử lý các đối tượng khác (Hình vẽ, Khung chữ, Ảnh, 3D controls)
                Rect bounds = GetElementCanvasBounds(element);

                if (bounds.Width <= 0 || bounds.Height <= 0 || bounds.IsEmpty)
                    return false;

                // Kiểm tra tâm, 4 góc và 4 trung điểm cạnh có nằm trong Lasso không
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

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error checking element in lasso: {ex.Message}");
                return false;
            }
        }

        private Rect GetElementCanvasBounds(UIElement element)
        {
            try
            {
                if (element is FrameworkElement fe)
                {
                    // Ưu tiên TransformToAncestor để tính chính xác vị trí hiển thị thị giác trên _canvas
                    try
                    {
                        GeneralTransform transform = fe.TransformToAncestor(_canvas);
                        double w = fe.ActualWidth > 0 ? fe.ActualWidth : (fe.Width > 0 ? fe.Width : fe.RenderSize.Width);
                        double h = fe.ActualHeight > 0 ? fe.ActualHeight : (fe.Height > 0 ? fe.Height : fe.RenderSize.Height);
                        if (w > 0 && h > 0)
                        {
                            Rect visualBounds = transform.TransformBounds(new Rect(0, 0, w, h));
                            if (!visualBounds.IsEmpty && visualBounds.Width > 0 && visualBounds.Height > 0)
                            {
                                return visualBounds;
                            }
                        }
                    }
                    catch { }

                    double left = Canvas.GetLeft(fe);
                    double top = Canvas.GetTop(fe);

                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;

                    if (element is System.Windows.Shapes.Path path && path.Data != null && !path.Data.Bounds.IsEmpty)
                    {
                        var pBounds = path.Data.Bounds;
                        return new Rect(left + pBounds.Left, top + pBounds.Top, pBounds.Width, pBounds.Height);
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
        private bool IsPointInPolygon(Point point, List<Point> polygon)
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
