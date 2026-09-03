using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartTouch.Models;
using CanvasControl = System.Windows.Controls.Canvas;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Service xử lý các transform operations (Move, Resize, Rotate)
    /// </summary>
    public class TransformService
    {
        #region Public Methods - Move

        /// <summary>
        /// Di chuyển object đến vị trí mới
        /// </summary>
        public void Move(SelectableObject obj, Point newPosition)
        {
            if (obj == null || obj.IsLocked)
                return;

            Vector delta = new Vector(newPosition.X - obj.Position.X, newPosition.Y - obj.Position.Y);
            MoveBy(obj, delta);
        }

        /// <summary>
        /// Di chuyển object theo delta (Hỗ trợ Nhóm đối tượng, Nét vẽ Polyline, Polygon, Line & Path)
        /// </summary>
        public void MoveBy(SelectableObject obj, Vector delta)
        {
            if (obj == null || obj.IsLocked || (delta.X == 0 && delta.Y == 0))
                return;

            // ✅ QC_4.2_GRID_PROTECT (G-3): CHẶN TUYỆT ĐỐI di chuyển background elements
            if (obj.Element is FrameworkElement fe && SelectionManager.IsBackgroundElement(fe))
            {
                System.Diagnostics.Debug.WriteLine($"🛑 MoveBy BLOCKED: Attempted to move background element {fe.GetType().Name}");
                return;
            }

            // 1. Xử lý nhóm đối tượng (Group / Multi-Selection / Lasso Selection)
            if (obj.IsGroup || obj.Type == ObjectType.Group || (obj.GroupMembers != null && obj.GroupMembers.Count > 0))
            {
                foreach (var member in obj.GroupMembers)
                {
                    if (member == null || member.IsLocked) continue;
                    MoveBy(member, delta);
                }
                if (obj.RotationAngle == 0)
                {
                    RecalculateGroupBounds(obj);
                }
                else
                {
                    obj.Position = new Point(obj.Position.X + delta.X, obj.Position.Y + delta.Y);
                    obj.Bounds = new Rect(obj.Bounds.X + delta.X, obj.Bounds.Y + delta.Y, obj.Bounds.Width, obj.Bounds.Height);
                }
                return;
            }

            // 2. Xử lý đối tượng đơn lẻ
            obj.Position = new Point(obj.Position.X + delta.X, obj.Position.Y + delta.Y);

            if (obj.Element is System.Windows.Shapes.Polyline polyline)
            {
                if (polyline.Points != null && polyline.Points.Count > 0)
                {
                    for (int i = 0; i < polyline.Points.Count; i++)
                    {
                        var pt = polyline.Points[i];
                        polyline.Points[i] = new Point(pt.X + delta.X, pt.Y + delta.Y);
                    }
                }
                if (!double.IsNaN(CanvasControl.GetLeft(polyline))) CanvasControl.SetLeft(polyline, double.NaN);
                if (!double.IsNaN(CanvasControl.GetTop(polyline))) CanvasControl.SetTop(polyline, double.NaN);
            }
            else if (obj.Element is System.Windows.Shapes.Polygon polygon)
            {
                if (polygon.Points != null && polygon.Points.Count > 0)
                {
                    for (int i = 0; i < polygon.Points.Count; i++)
                    {
                        var pt = polygon.Points[i];
                        polygon.Points[i] = new Point(pt.X + delta.X, pt.Y + delta.Y);
                    }
                }
                if (!double.IsNaN(CanvasControl.GetLeft(polygon))) CanvasControl.SetLeft(polygon, double.NaN);
                if (!double.IsNaN(CanvasControl.GetTop(polygon))) CanvasControl.SetTop(polygon, double.NaN);
            }
            else if (obj.Element is System.Windows.Shapes.Line line)
            {
                line.X1 += delta.X; line.Y1 += delta.Y;
                line.X2 += delta.X; line.Y2 += delta.Y;
                if (!double.IsNaN(CanvasControl.GetLeft(line))) CanvasControl.SetLeft(line, double.NaN);
                if (!double.IsNaN(CanvasControl.GetTop(line))) CanvasControl.SetTop(line, double.NaN);
            }
            else if (obj.Element is System.Windows.Shapes.Path path && path.Data != null)
            {
                if (path.RenderTransform is TransformGroup tg)
                {
                    var tt = tg.Children.OfType<TranslateTransform>().FirstOrDefault();
                    if (tt == null)
                    {
                        tt = new TranslateTransform();
                        tg.Children.Add(tt);
                    }
                    tt.X += delta.X;
                    tt.Y += delta.Y;
                }
                else if (path.RenderTransform is TranslateTransform tt)
                {
                    tt.X += delta.X;
                    tt.Y += delta.Y;
                }
                else
                {
                    var tgNew = new TransformGroup();
                    if (path.RenderTransform != null && path.RenderTransform != Transform.Identity)
                        tgNew.Children.Add(path.RenderTransform);
                    tgNew.Children.Add(new TranslateTransform(delta.X, delta.Y));
                    path.RenderTransform = tgNew;
                }
            }
            else if (obj.Element != null)
            {
                double currentLeft = CanvasControl.GetLeft(obj.Element);
                double currentTop = CanvasControl.GetTop(obj.Element);
                if (double.IsNaN(currentLeft)) currentLeft = obj.Position.X - delta.X;
                if (double.IsNaN(currentTop)) currentTop = obj.Position.Y - delta.Y;

                CanvasControl.SetLeft(obj.Element, currentLeft + delta.X);
                CanvasControl.SetTop(obj.Element, currentTop + delta.Y);
            }

            obj.UpdateBounds();
        }

        #endregion

        #region Public Methods - Resize

        /// <summary>
        /// NG-5: Resize object (support all types including Group/Multi-Selection)
        /// ✅ AUD-04: Có recursion depth guard chống StackOverflow cho nested groups
        /// ✅ AUD-05: Scale StrokeThickness tỉ lệ cho Polyline/Path
        /// </summary>
        public void Resize(SelectableObject obj, Size newSize, int depth = 0)
        {
            if (obj == null || obj.IsLocked)
                return;

            // ✅ AUD-04 FIX: Chống StackOverflow cho nested groups
            if (depth > 10)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ TransformService.Resize: Max recursion depth reached (depth={depth})");
                return;
            }

            // Validate size
            newSize.Width = Math.Max(15, newSize.Width);
            newSize.Height = Math.Max(15, newSize.Height);

            // ✅ FIX: Hỗ trợ Resize cho Nhóm đối tượng (Group / Multi-Selection)
            if (obj.IsGroup || obj.Type == ObjectType.Group || (obj.GroupMembers != null && obj.GroupMembers.Count > 0))
            {
                double oldWidth = obj.Size.Width;
                double oldHeight = obj.Size.Height;

                if (oldWidth > 0 && oldHeight > 0)
                {
                    double scaleX = newSize.Width / oldWidth;
                    double scaleY = newSize.Height / oldHeight;

                    Point groupOrigin = obj.Position;

                    foreach (var member in obj.GroupMembers)
                    {
                        if (member == null || member.IsLocked) continue;

                        // Tính lại vị trí tương đối của member theo góc groupOrigin
                        double relX = member.Position.X - groupOrigin.X;
                        double relY = member.Position.Y - groupOrigin.Y;

                        double newMemX = groupOrigin.X + relX * scaleX;
                        double newMemY = groupOrigin.Y + relY * scaleY;

                        member.Position = new Point(newMemX, newMemY);

                        // Tính lại kích thước thành viên theo tỉ lệ scale
                        Size newMemSize = new Size(
                            Math.Max(5, member.Size.Width * scaleX),
                            Math.Max(5, member.Size.Height * scaleY)
                        );

                        // Co giãn đệ quy cho thành viên (cập nhật Polyline.Points, Path, Shape...)
                        Resize(member, newMemSize, depth + 1);
                    }
                }

                obj.Size = newSize;
                obj.UpdateBounds();
                return;
            }

            obj.Size = newSize;

            // NG-5: Handle different element types
            if (obj.Element is System.Windows.Shapes.Line line)
            {
                double minX = Math.Min(line.X1, line.X2);
                double minY = Math.Min(line.Y1, line.Y2);
                double maxX = Math.Max(line.X1, line.X2);
                double maxY = Math.Max(line.Y1, line.Y2);
                double oldW = maxX - minX;
                double oldH = maxY - minY;
                double scaleX = oldW > 0 ? newSize.Width / oldW : 1.0;
                double scaleY = oldH > 0 ? newSize.Height / oldH : 1.0;

                line.X1 = obj.Position.X + (line.X1 - minX) * scaleX;
                line.Y1 = obj.Position.Y + (line.Y1 - minY) * scaleY;
                line.X2 = obj.Position.X + (line.X2 - minX) * scaleX;
                line.Y2 = obj.Position.Y + (line.Y2 - minY) * scaleY;

                if (!double.IsNaN(CanvasControl.GetLeft(line))) CanvasControl.SetLeft(line, double.NaN);
                if (!double.IsNaN(CanvasControl.GetTop(line))) CanvasControl.SetTop(line, double.NaN);
                System.Diagnostics.Debug.WriteLine($"🔧 Resize Line: ({line.X1:F0},{line.Y1:F0}) → ({line.X2:F0},{line.Y2:F0})");
            }
            else if (obj.Element is System.Windows.Shapes.Polyline polyline)
            {
                // ✅ CRITICAL BUG-FIX: Bắt buộc đứt Fill = null đối với nét vẽ Polyline
                // Tránh tình trạng lòng nét chữ vẽ tay bị tô kín mảng màu trắng đục khi kéo Resize!
                polyline.Fill = null;

                // Polyline (drawn handwriting stroke): Scale all points proportionally
                if (polyline.Points != null && polyline.Points.Count > 0)
                {
                    double minX = double.MaxValue, minY = double.MaxValue;
                    double maxX = double.MinValue, maxY = double.MinValue;
                    
                    foreach (var pt in polyline.Points)
                    {
                        minX = Math.Min(minX, pt.X);
                        minY = Math.Min(minY, pt.Y);
                        maxX = Math.Max(maxX, pt.X);
                        maxY = Math.Max(maxY, pt.Y);
                    }
                    
                    double oldWidth = maxX - minX;
                    double oldHeight = maxY - minY;
                    
                    double scaleX = oldWidth > 0 ? newSize.Width / oldWidth : 1.0;
                    double scaleY = oldHeight > 0 ? newSize.Height / oldHeight : 1.0;
                    
                    for (int i = 0; i < polyline.Points.Count; i++)
                    {
                        var pt = polyline.Points[i];
                        double newX = oldWidth > 0 ? obj.Position.X + (pt.X - minX) * scaleX : obj.Position.X;
                        double newY = oldHeight > 0 ? obj.Position.Y + (pt.Y - minY) * scaleY : obj.Position.Y;
                        polyline.Points[i] = new Point(newX, newY);
                    }

                    if (!double.IsNaN(CanvasControl.GetLeft(polyline))) CanvasControl.SetLeft(polyline, double.NaN);
                    if (!double.IsNaN(CanvasControl.GetTop(polyline))) CanvasControl.SetTop(polyline, double.NaN);

                    System.Diagnostics.Debug.WriteLine($"🔧 Resize Polyline: {polyline.Points.Count} points scaled");
                }
            }
            else if (obj.Element is System.Windows.Shapes.Polygon polygon)
            {
                // Polygon (2D shapes - Triangle, Star, Arrow, Diamond...): Scale all points proportionally
                if (polygon.Points != null && polygon.Points.Count > 0)
                {
                    double minX = double.MaxValue, minY = double.MaxValue;
                    double maxX = double.MinValue, maxY = double.MinValue;
                    
                    foreach (var pt in polygon.Points)
                    {
                        minX = Math.Min(minX, pt.X);
                        minY = Math.Min(minY, pt.Y);
                        maxX = Math.Max(maxX, pt.X);
                        maxY = Math.Max(maxY, pt.Y);
                    }
                    
                    double oldWidth = maxX - minX;
                    double oldHeight = maxY - minY;
                    
                    double scaleX = oldWidth > 0 ? newSize.Width / oldWidth : 1.0;
                    double scaleY = oldHeight > 0 ? newSize.Height / oldHeight : 1.0;
                    
                    for (int i = 0; i < polygon.Points.Count; i++)
                    {
                        var pt = polygon.Points[i];
                        double newX = oldWidth > 0 ? obj.Position.X + (pt.X - minX) * scaleX : obj.Position.X;
                        double newY = oldHeight > 0 ? obj.Position.Y + (pt.Y - minY) * scaleY : obj.Position.Y;
                        polygon.Points[i] = new Point(newX, newY);
                    }

                    if (!double.IsNaN(CanvasControl.GetLeft(polygon))) CanvasControl.SetLeft(polygon, double.NaN);
                    if (!double.IsNaN(CanvasControl.GetTop(polygon))) CanvasControl.SetTop(polygon, double.NaN);

                    System.Diagnostics.Debug.WriteLine($"🔧 Resize Polygon: {polygon.Points.Count} points scaled");
                }
            }
            else if (obj.Element is System.Windows.Shapes.Path path && path.Data != null)
            {
                // ✅ CRITICAL BUG-FIX: Nếu đối tượng là nét chữ/nét vẽ tay (Stroke / Drawing), ngắt Fill tô lòng
                if (obj.Type == ObjectType.Stroke || obj.Type == ObjectType.Drawing)
                {
                    path.Fill = null;
                }

                // Path (smooth Bezier handwriting stroke or shape): Apply RenderTransform
                var bounds = path.Data.Bounds;
                if (!bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0)
                {
                    double scaleX = newSize.Width / bounds.Width;
                    double scaleY = newSize.Height / bounds.Height;
                    
                    var tg = new TransformGroup();
                    tg.Children.Add(new ScaleTransform(scaleX, scaleY, bounds.X, bounds.Y));
                    tg.Children.Add(new TranslateTransform(obj.Position.X - bounds.X, obj.Position.Y - bounds.Y));
                    path.RenderTransform = tg;

                    System.Diagnostics.Debug.WriteLine($"🔧 Resize Path stroke: scale=({scaleX:F2},{scaleY:F2})");
                }
            }
            else if (obj.Element is CanvasControl canvas)
            {
                double baseW = !double.IsNaN(canvas.Width) && canvas.Width > 0 ? canvas.Width : (canvas.ActualWidth > 0 ? canvas.ActualWidth : 200);
                double baseH = !double.IsNaN(canvas.Height) && canvas.Height > 0 ? canvas.Height : (canvas.ActualHeight > 0 ? canvas.ActualHeight : 200);

                CanvasControl.SetLeft(canvas, obj.Position.X);
                CanvasControl.SetTop(canvas, obj.Position.Y);

                if (baseW > 0 && baseH > 0)
                {
                    double scaleX = newSize.Width / baseW;
                    double scaleY = newSize.Height / baseH;
                    var tg = new TransformGroup();
                    tg.Children.Add(new ScaleTransform(scaleX, scaleY, 0, 0));
                    if (obj.RotationAngle != 0)
                    {
                        tg.Children.Add(new RotateTransform(obj.RotationAngle, (baseW * scaleX) / 2.0, (baseH * scaleY) / 2.0));
                    }
                    canvas.RenderTransform = tg;
                    System.Diagnostics.Debug.WriteLine($"🔧 Resize Canvas: scale=({scaleX:F2},{scaleY:F2})");
                }
            }
            else if (obj.Element is FrameworkElement element)
            {
                // Rectangle, Ellipse, TextBlock, Border, Image
                CanvasControl.SetLeft(element, obj.Position.X);
                CanvasControl.SetTop(element, obj.Position.Y);
                element.Width = newSize.Width;
                element.Height = newSize.Height;
                if (obj.RotationAngle != 0)
                {
                    element.RenderTransform = new RotateTransform(obj.RotationAngle, newSize.Width / 2.0, newSize.Height / 2.0);
                }
                
                System.Diagnostics.Debug.WriteLine($"🔧 Resize {obj.Type}: {newSize.Width:F0}x{newSize.Height:F0}");
            }

            obj.UpdateBounds();
        }

        /// <summary>
        /// NG-5: Resize từ một góc/cạnh cụ thể dựa trên kích thước & vị trí gốc ban đầu
        /// Hỗ trợ snapshot ban đầu cho Line, DashedLine, Path (ArrowLine), Polygon, Polyline và Groups.
        /// </summary>
        public void ResizeFromHandle(
            SelectableObject obj, 
            Models.ResizeMode mode, 
            Point currentPoint, 
            Point startPoint, 
            Size originalSize, 
            Point originalPosition,
            System.Collections.Generic.Dictionary<SelectableObject, (Point pos, Size size)>? memberSnapshots = null,
            System.Collections.Generic.Dictionary<SelectableObject, ElementTransformState>? initialStates = null)
        {
            if (obj == null || obj.IsLocked)
                return;

            Vector delta = currentPoint - startPoint;

            double newWidth = originalSize.Width;
            double newHeight = originalSize.Height;
            Point newPosition = originalPosition;

            switch (mode)
            {
                case Models.ResizeMode.TopLeft:
                    newPosition.X = originalPosition.X + delta.X;
                    newPosition.Y = originalPosition.Y + delta.Y;
                    newWidth = originalSize.Width - delta.X;
                    newHeight = originalSize.Height - delta.Y;
                    break;

                case Models.ResizeMode.TopRight:
                    newPosition.Y = originalPosition.Y + delta.Y;
                    newWidth = originalSize.Width + delta.X;
                    newHeight = originalSize.Height - delta.Y;
                    break;

                case Models.ResizeMode.BottomLeft:
                    newPosition.X = originalPosition.X + delta.X;
                    newWidth = originalSize.Width - delta.X;
                    newHeight = originalSize.Height + delta.Y;
                    break;

                case Models.ResizeMode.BottomRight:
                    newWidth = originalSize.Width + delta.X;
                    newHeight = originalSize.Height + delta.Y;
                    break;

                case Models.ResizeMode.Top:
                    newPosition.Y = originalPosition.Y + delta.Y;
                    newHeight = originalSize.Height - delta.Y;
                    break;

                case Models.ResizeMode.Bottom:
                    newHeight = originalSize.Height + delta.Y;
                    break;

                case Models.ResizeMode.Left:
                    newPosition.X = originalPosition.X + delta.X;
                    newWidth = originalSize.Width - delta.X;
                    break;

                case Models.ResizeMode.Right:
                    newWidth = originalSize.Width + delta.X;
                    break;
            }

            // Clamp minimum size TRƯỚC khi gán vị trí & kích thước mới
            const double MIN_SIZE = 15;
            if (newWidth < MIN_SIZE)
            {
                if (mode == Models.ResizeMode.TopLeft || mode == Models.ResizeMode.BottomLeft || mode == Models.ResizeMode.Left)
                {
                    newPosition.X = originalPosition.X + (originalSize.Width - MIN_SIZE);
                }
                newWidth = MIN_SIZE;
            }
            if (newHeight < MIN_SIZE)
            {
                if (mode == Models.ResizeMode.TopLeft || mode == Models.ResizeMode.TopRight || mode == Models.ResizeMode.Top)
                {
                    newPosition.Y = originalPosition.Y + (originalSize.Height - MIN_SIZE);
                }
                newHeight = MIN_SIZE;
            }

            // 1. QC_4.2_GROUP_RESIZE_FIX: Hỗ trợ ResizeFromHandle cho Nhóm đối tượng (Group / Multi-Selection)
            if (obj.IsGroup || obj.Type == ObjectType.Group || (obj.GroupMembers != null && obj.GroupMembers.Count > 0))
            {
                if (originalSize.Width > 0 && originalSize.Height > 0)
                {
                    double scaleX = newWidth / originalSize.Width;
                    double scaleY = newHeight / originalSize.Height;

                    foreach (var member in obj.GroupMembers)
                    {
                        if (member == null || member.IsLocked) continue;

                        ElementTransformState? mState = null;
                        initialStates?.TryGetValue(member, out mState);

                        double origMemX = mState != null ? mState.Position.X : member.Position.X;
                        double origMemY = mState != null ? mState.Position.Y : member.Position.Y;
                        double origMemW = mState != null ? mState.Size.Width : member.Size.Width;
                        double origMemH = mState != null ? mState.Size.Height : member.Size.Height;

                        if (memberSnapshots != null && memberSnapshots.TryGetValue(member, out var snap))
                        {
                            origMemX = snap.pos.X;
                            origMemY = snap.pos.Y;
                            origMemW = snap.size.Width;
                            origMemH = snap.size.Height;
                        }

                        double relX = origMemX - originalPosition.X;
                        double relY = origMemY - originalPosition.Y;

                        Point newMemPos = new Point(newPosition.X + relX * scaleX, newPosition.Y + relY * scaleY);
                        Size newMemSize = new Size(
                            Math.Max(5, origMemW * scaleX),
                            Math.Max(5, origMemH * scaleY)
                        );

                        ResizeSingleFromInitialState(member, newMemPos, newMemSize, origMemPos: new Point(origMemX, origMemY), origMemSize: new Size(origMemW, origMemH), mState);
                    }
                }

                obj.Position = newPosition;
                obj.Size = new Size(newWidth, newHeight);
                obj.UpdateBounds();
                return;
            }

            // 2. Xử lý đối tượng đơn lẻ trực tiếp từ initial state
            ElementTransformState? selfState = null;
            initialStates?.TryGetValue(obj, out selfState);

            ResizeSingleFromInitialState(obj, newPosition, new Size(newWidth, newHeight), originalPosition, originalSize, selfState);
        }

        private void ResizeSingleFromInitialState(
            SelectableObject obj,
            Point newPosition,
            Size newSize,
            Point origMemPos,
            Size origMemSize,
            ElementTransformState? state)
        {
            obj.Position = newPosition;
            obj.Size = newSize;

            // 1. Line & DashedLine: Bảo toàn 100% hướng dốc và tính chất ngang/dọc
            if (obj.Element is System.Windows.Shapes.Line line)
            {
                double origX1 = state != null ? state.LineX1 : line.X1;
                double origY1 = state != null ? state.LineY1 : line.Y1;
                double origX2 = state != null ? state.LineX2 : line.X2;
                double origY2 = state != null ? state.LineY2 : line.Y2;

                double origMinX = Math.Min(origX1, origX2);
                double origMinY = Math.Min(origY1, origY2);
                double origMaxX = Math.Max(origX1, origX2);
                double origMaxY = Math.Max(origY1, origY2);
                double origSpanW = origMaxX - origMinX;
                double origSpanH = origMaxY - origMinY;

                double u1 = origSpanW > 0.001 ? (origX1 - origMinX) / origSpanW : 0.5;
                double v1 = origSpanH > 0.001 ? (origY1 - origMinY) / origSpanH : 0.5;
                double u2 = origSpanW > 0.001 ? (origX2 - origMinX) / origSpanW : 0.5;
                double v2 = origSpanH > 0.001 ? (origY2 - origMinY) / origSpanH : 0.5;

                if (origSpanH <= 0.001)
                {
                    // Đoạn thẳng nằm ngang: Giữ nguyên y1 = y2 phẳng tuyệt đối
                    line.X1 = newPosition.X + u1 * newSize.Width;
                    line.X2 = newPosition.X + u2 * newSize.Width;
                    line.Y1 = newPosition.Y + newSize.Height / 2.0;
                    line.Y2 = newPosition.Y + newSize.Height / 2.0;
                }
                else if (origSpanW <= 0.001)
                {
                    // Đoạn thẳng đứng: Giữ nguyên x1 = x2 thẳng đứng tuyệt đối
                    line.X1 = newPosition.X + newSize.Width / 2.0;
                    line.X2 = newPosition.X + newSize.Width / 2.0;
                    line.Y1 = newPosition.Y + v1 * newSize.Height;
                    line.Y2 = newPosition.Y + v2 * newSize.Height;
                }
                else
                {
                    // Đoạn thẳng xiên: Giữ nguyên hướng và độ dốc theo tỷ lệ
                    line.X1 = newPosition.X + u1 * newSize.Width;
                    line.Y1 = newPosition.Y + v1 * newSize.Height;
                    line.X2 = newPosition.X + u2 * newSize.Width;
                    line.Y2 = newPosition.Y + v2 * newSize.Height;
                }

                if (!double.IsNaN(CanvasControl.GetLeft(line))) CanvasControl.SetLeft(line, double.NaN);
                if (!double.IsNaN(CanvasControl.GetTop(line))) CanvasControl.SetTop(line, double.NaN);
            }
            // 2. Polygon (2D shapes, Block Arrows): Scale từ snapshot điểm gốc
            else if (obj.Element is System.Windows.Shapes.Polygon polygon)
            {
                if (state != null && state.PolygonPoints != null && state.PolygonPoints.Length > 0 && origMemSize.Width > 0 && origMemSize.Height > 0)
                {
                    double scaleX = newSize.Width / origMemSize.Width;
                    double scaleY = newSize.Height / origMemSize.Height;

                    var newPoints = new PointCollection(state.PolygonPoints.Length);
                    for (int i = 0; i < state.PolygonPoints.Length; i++)
                    {
                        var pt = state.PolygonPoints[i];
                        double relX = pt.X - origMemPos.X;
                        double relY = pt.Y - origMemPos.Y;
                        newPoints.Add(new Point(newPosition.X + relX * scaleX, newPosition.Y + relY * scaleY));
                    }
                    polygon.Points = newPoints;
                }
                else
                {
                    Resize(obj, newSize);
                    return;
                }

                if (!double.IsNaN(CanvasControl.GetLeft(polygon))) CanvasControl.SetLeft(polygon, double.NaN);
                if (!double.IsNaN(CanvasControl.GetTop(polygon))) CanvasControl.SetTop(polygon, double.NaN);
            }
            // 3. Polyline (Handwriting strokes): Scale từ snapshot nét vẽ gốc
            else if (obj.Element is System.Windows.Shapes.Polyline polyline)
            {
                polyline.Fill = null;

                if (state != null && state.PolylinePoints != null && state.PolylinePoints.Length > 0 && origMemSize.Width > 0 && origMemSize.Height > 0)
                {
                    double scaleX = newSize.Width / origMemSize.Width;
                    double scaleY = newSize.Height / origMemSize.Height;

                    var newPoints = new PointCollection(state.PolylinePoints.Length);
                    for (int i = 0; i < state.PolylinePoints.Length; i++)
                    {
                        var pt = state.PolylinePoints[i];
                        double relX = pt.X - origMemPos.X;
                        double relY = pt.Y - origMemPos.Y;
                        newPoints.Add(new Point(newPosition.X + relX * scaleX, newPosition.Y + relY * scaleY));
                    }
                    polyline.Points = newPoints;
                }
                else
                {
                    Resize(obj, newSize);
                    return;
                }

                if (!double.IsNaN(CanvasControl.GetLeft(polyline))) CanvasControl.SetLeft(polyline, double.NaN);
                if (!double.IsNaN(CanvasControl.GetTop(polyline))) CanvasControl.SetTop(polyline, double.NaN);
            }
            // 4. Path (ArrowLine, DoubleArrowLine, Bezier strokes): ScaleTransform + TranslateTransform
            else if (obj.Element is System.Windows.Shapes.Path path && path.Data != null)
            {
                if (obj.Type == ObjectType.Stroke || obj.Type == ObjectType.Drawing)
                {
                    path.Fill = null;
                }

                var bounds = path.Data.Bounds;
                if (!bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0)
                {
                    double scaleX = newSize.Width / bounds.Width;
                    double scaleY = newSize.Height / bounds.Height;

                    var tg = new TransformGroup();
                    tg.Children.Add(new ScaleTransform(scaleX, scaleY, bounds.X, bounds.Y));
                    tg.Children.Add(new TranslateTransform(newPosition.X - bounds.X, newPosition.Y - bounds.Y));
                    path.RenderTransform = tg;
                }
            }
            // 5. Canvas Container (3D Shape, STEM diagrams, etc.): Áp dụng ScaleTransform vector mượt mà
            else if (obj.Element is CanvasControl canvas)
            {
                CanvasControl.SetLeft(canvas, newPosition.X);
                CanvasControl.SetTop(canvas, newPosition.Y);

                double baseW = !double.IsNaN(canvas.Width) && canvas.Width > 0 ? canvas.Width : (canvas.ActualWidth > 0 ? canvas.ActualWidth : (state != null && !double.IsNaN(state.Width) && state.Width > 0 ? state.Width : 200));
                double baseH = !double.IsNaN(canvas.Height) && canvas.Height > 0 ? canvas.Height : (canvas.ActualHeight > 0 ? canvas.ActualHeight : (state != null && !double.IsNaN(state.Height) && state.Height > 0 ? state.Height : 200));

                if (baseW > 0 && baseH > 0)
                {
                    double scaleX = newSize.Width / baseW;
                    double scaleY = newSize.Height / baseH;

                    var tg = new TransformGroup();
                    tg.Children.Add(new ScaleTransform(scaleX, scaleY, 0, 0));
                    if (obj.RotationAngle != 0)
                    {
                        tg.Children.Add(new RotateTransform(obj.RotationAngle, (baseW * scaleX) / 2.0, (baseH * scaleY) / 2.0));
                    }
                    canvas.RenderTransform = tg;
                }
                else
                {
                    canvas.Width = newSize.Width;
                    canvas.Height = newSize.Height;
                }
            }
            // 6. FrameworkElement (Rectangle, Ellipse, TextBlock, Image, Border)
            else if (obj.Element is FrameworkElement element)
            {
                CanvasControl.SetLeft(element, newPosition.X);
                CanvasControl.SetTop(element, newPosition.Y);
                element.Width = newSize.Width;
                element.Height = newSize.Height;
                if (obj.RotationAngle != 0)
                {
                    element.RenderTransform = new RotateTransform(obj.RotationAngle, newSize.Width / 2.0, newSize.Height / 2.0);
                }
            }

            obj.UpdateBounds();
        }

        #endregion

        #region Public Methods - Rotate

        /// <summary>
        /// Xoay đối tượng hoặc nhóm đối tượng từ trạng thái snapshot ban đầu quanh tâm bất biến.
        /// Đảm bảo tính toán chính xác 100%, không bị tích lũy sai số trôi tọa độ và SelectionBox luôn đồng bộ hoàn toàn với đối tượng.
        /// </summary>
        public void RotateFromInitialState(
            SelectableObject obj,
            Point center,
            double targetAngleDegrees,
            System.Collections.Generic.Dictionary<SelectableObject, ElementTransformState> initialStates)
        {
            if (obj == null || obj.IsLocked || initialStates == null || initialStates.Count == 0) return;

            double initialAngle = 0;
            if (initialStates.TryGetValue(obj, out var selfState))
            {
                initialAngle = selfState.RotationAngle;
            }

            double deltaAngleDegrees = targetAngleDegrees - initialAngle;
            double rad = deltaAngleDegrees * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);

            // 1. Nhóm đối tượng (Group / Multi-Selection)
            if (obj.IsGroup || obj.Type == ObjectType.Group || (obj.GroupMembers != null && obj.GroupMembers.Count > 0))
            {
                foreach (var member in obj.GroupMembers)
                {
                    if (member == null || member.IsLocked) continue;
                    if (!initialStates.TryGetValue(member, out var mState)) continue;

                    RotateMemberFromState(member, mState, center, deltaAngleDegrees, cos, sin);
                }
                return;
            }

            // 2. Đối tượng đơn lẻ
            if (initialStates.TryGetValue(obj, out var singleState))
            {
                RotateSingleFromState(obj, singleState, center, targetAngleDegrees, deltaAngleDegrees, cos, sin);
            }
        }

        private void RotateMemberFromState(
            SelectableObject member,
            ElementTransformState state,
            Point center,
            double deltaAngleDegrees,
            double cos,
            double sin)
        {
            if (member.Element is System.Windows.Shapes.Polyline polyline && state.PolylinePoints != null)
            {
                var newPoints = new PointCollection(state.PolylinePoints.Length);
                for (int i = 0; i < state.PolylinePoints.Length; i++)
                {
                    var pt = state.PolylinePoints[i];
                    double dx = pt.X - center.X;
                    double dy = pt.Y - center.Y;
                    newPoints.Add(new Point(center.X + (dx * cos - dy * sin), center.Y + (dx * sin + dy * cos)));
                }
                polyline.Points = newPoints;
                polyline.RenderTransform = Transform.Identity;
                member.UpdateBounds();
            }
            else if (member.Element is System.Windows.Shapes.Polygon polygon && state.PolygonPoints != null)
            {
                var newPoints = new PointCollection(state.PolygonPoints.Length);
                for (int i = 0; i < state.PolygonPoints.Length; i++)
                {
                    var pt = state.PolygonPoints[i];
                    double dx = pt.X - center.X;
                    double dy = pt.Y - center.Y;
                    newPoints.Add(new Point(center.X + (dx * cos - dy * sin), center.Y + (dx * sin + dy * cos)));
                }
                polygon.Points = newPoints;
                polygon.RenderTransform = Transform.Identity;
                member.UpdateBounds();
            }
            else if (member.Element is System.Windows.Shapes.Line line)
            {
                double dx1 = state.LineX1 - center.X;
                double dy1 = state.LineY1 - center.Y;
                line.X1 = center.X + (dx1 * cos - dy1 * sin);
                line.Y1 = center.Y + (dx1 * sin + dy1 * cos);

                double dx2 = state.LineX2 - center.X;
                double dy2 = state.LineY2 - center.Y;
                line.X2 = center.X + (dx2 * cos - dy2 * sin);
                line.Y2 = center.Y + (dx2 * sin + dy2 * cos);

                line.RenderTransform = Transform.Identity;
                member.UpdateBounds();
            }
            else if (member.Element is System.Windows.Shapes.Path path && path.Data != null)
            {
                var tg = new TransformGroup();
                if (state.RenderTransform != null && state.RenderTransform != Transform.Identity)
                {
                    tg.Children.Add(state.RenderTransform.Clone());
                }
                tg.Children.Add(new RotateTransform(deltaAngleDegrees, center.X, center.Y));
                path.RenderTransform = tg;
                member.UpdateBounds();
            }
            else if (member.Element is CanvasControl canvas)
            {
                double memCenterX = state.Position.X + (state.Size.Width / 2.0);
                double memCenterY = state.Position.Y + (state.Size.Height / 2.0);

                double dx = memCenterX - center.X;
                double dy = memCenterY - center.Y;

                double newCenterX = center.X + (dx * cos - dy * sin);
                double newCenterY = center.Y + (dx * sin + dy * cos);

                double newLeft = newCenterX - (state.Size.Width / 2.0);
                double newTop = newCenterY - (state.Size.Height / 2.0);

                CanvasControl.SetLeft(canvas, newLeft);
                CanvasControl.SetTop(canvas, newTop);

                member.Position = new Point(newLeft, newTop);
                member.RotationAngle = (state.RotationAngle + deltaAngleDegrees) % 360;

                double baseW = !double.IsNaN(canvas.Width) && canvas.Width > 0 ? canvas.Width : (canvas.ActualWidth > 0 ? canvas.ActualWidth : 200);
                double baseH = !double.IsNaN(canvas.Height) && canvas.Height > 0 ? canvas.Height : (canvas.ActualHeight > 0 ? canvas.ActualHeight : 200);

                double currentW = state.Size.Width > 0 ? state.Size.Width : baseW;
                double currentH = state.Size.Height > 0 ? state.Size.Height : baseH;

                double scaleX = currentW / baseW;
                double scaleY = currentH / baseH;

                var tg = new TransformGroup();
                tg.Children.Add(new ScaleTransform(scaleX, scaleY, 0, 0));
                if (member.RotationAngle != 0)
                {
                    tg.Children.Add(new RotateTransform(member.RotationAngle, (baseW * scaleX) / 2.0, (baseH * scaleY) / 2.0));
                }
                canvas.RenderTransform = tg;
                member.UpdateBounds();
            }
            else if (member.Element is FrameworkElement fe)
            {
                double memCenterX = state.Position.X + (state.Size.Width / 2.0);
                double memCenterY = state.Position.Y + (state.Size.Height / 2.0);

                double dx = memCenterX - center.X;
                double dy = memCenterY - center.Y;

                double newCenterX = center.X + (dx * cos - dy * sin);
                double newCenterY = center.Y + (dx * sin + dy * cos);

                double newLeft = newCenterX - (state.Size.Width / 2.0);
                double newTop = newCenterY - (state.Size.Height / 2.0);

                CanvasControl.SetLeft(fe, newLeft);
                CanvasControl.SetTop(fe, newTop);

                member.Position = new Point(newLeft, newTop);
                member.RotationAngle = (state.RotationAngle + deltaAngleDegrees) % 360;

                double w = !double.IsNaN(fe.Width) && fe.Width > 0 ? fe.Width : (fe.ActualWidth > 0 ? fe.ActualWidth : state.Size.Width);
                double h = !double.IsNaN(fe.Height) && fe.Height > 0 ? fe.Height : (fe.ActualHeight > 0 ? fe.ActualHeight : state.Size.Height);
                var rotateTransform = new RotateTransform(member.RotationAngle, w / 2.0, h / 2.0);
                fe.RenderTransform = rotateTransform;
                member.UpdateBounds();
            }
        }

        private void RotateSingleFromState(
            SelectableObject obj,
            ElementTransformState state,
            Point center,
            double targetAngleDegrees,
            double deltaAngleDegrees,
            double cos,
            double sin)
        {
            if (obj.Element is System.Windows.Shapes.Polyline polyline && state.PolylinePoints != null)
            {
                var newPoints = new PointCollection(state.PolylinePoints.Length);
                for (int i = 0; i < state.PolylinePoints.Length; i++)
                {
                    var pt = state.PolylinePoints[i];
                    double dx = pt.X - center.X;
                    double dy = pt.Y - center.Y;
                    newPoints.Add(new Point(center.X + (dx * cos - dy * sin), center.Y + (dx * sin + dy * cos)));
                }
                polyline.Points = newPoints;
                polyline.RenderTransform = Transform.Identity;
            }
            else if (obj.Element is System.Windows.Shapes.Polygon polygon && state.PolygonPoints != null)
            {
                var newPoints = new PointCollection(state.PolygonPoints.Length);
                for (int i = 0; i < state.PolygonPoints.Length; i++)
                {
                    var pt = state.PolygonPoints[i];
                    double dx = pt.X - center.X;
                    double dy = pt.Y - center.Y;
                    newPoints.Add(new Point(center.X + (dx * cos - dy * sin), center.Y + (dx * sin + dy * cos)));
                }
                polygon.Points = newPoints;
                polygon.RenderTransform = Transform.Identity;
            }
            else if (obj.Element is System.Windows.Shapes.Line line)
            {
                double dx1 = state.LineX1 - center.X;
                double dy1 = state.LineY1 - center.Y;
                line.X1 = center.X + (dx1 * cos - dy1 * sin);
                line.Y1 = center.Y + (dx1 * sin + dy1 * cos);

                double dx2 = state.LineX2 - center.X;
                double dy2 = state.LineY2 - center.Y;
                line.X2 = center.X + (dx2 * cos - dy2 * sin);
                line.Y2 = center.Y + (dx2 * sin + dy2 * cos);

                line.RenderTransform = Transform.Identity;
            }
            else if (obj.Element is System.Windows.Shapes.Path path && path.Data != null)
            {
                var tg = new TransformGroup();
                if (state.RenderTransform != null && state.RenderTransform != Transform.Identity)
                {
                    tg.Children.Add(state.RenderTransform.Clone());
                }
                tg.Children.Add(new RotateTransform(deltaAngleDegrees, center.X, center.Y));
                path.RenderTransform = tg;
            }
            else if (obj.Element is CanvasControl canvas)
            {
                obj.RotationAngle = targetAngleDegrees;

                double baseW = !double.IsNaN(canvas.Width) && canvas.Width > 0 ? canvas.Width : (canvas.ActualWidth > 0 ? canvas.ActualWidth : 200);
                double baseH = !double.IsNaN(canvas.Height) && canvas.Height > 0 ? canvas.Height : (canvas.ActualHeight > 0 ? canvas.ActualHeight : 200);

                double currentW = state.Size.Width > 0 ? state.Size.Width : baseW;
                double currentH = state.Size.Height > 0 ? state.Size.Height : baseH;

                double scaleX = currentW / baseW;
                double scaleY = currentH / baseH;

                var tg = new TransformGroup();
                tg.Children.Add(new ScaleTransform(scaleX, scaleY, 0, 0));
                if (targetAngleDegrees != 0)
                {
                    tg.Children.Add(new RotateTransform(targetAngleDegrees, (baseW * scaleX) / 2.0, (baseH * scaleY) / 2.0));
                }
                canvas.RenderTransform = tg;
            }
            else if (obj.Element is FrameworkElement fe)
            {
                obj.RotationAngle = targetAngleDegrees;
                double w = !double.IsNaN(fe.Width) && fe.Width > 0 ? fe.Width : (fe.ActualWidth > 0 ? fe.ActualWidth : state.Size.Width);
                double h = !double.IsNaN(fe.Height) && fe.Height > 0 ? fe.Height : (fe.ActualHeight > 0 ? fe.ActualHeight : state.Size.Height);
                var rotateTransform = new RotateTransform(targetAngleDegrees, w / 2.0, h / 2.0);
                fe.RenderTransform = rotateTransform;
            }
        }

        /// <summary>
        /// Rotate object đến góc cụ thể (Hỗ trợ cả đối tượng đơn lẻ và Group)
        /// </summary>
        public void Rotate(SelectableObject obj, double angleDegrees)
        {
            if (obj == null || obj.IsLocked)
                return;

            double normalizedAngle = (angleDegrees % 360 + 360) % 360;

            // 1. Nhóm đối tượng (Group / Multi-Selection)
            if (obj.IsGroup || obj.Type == ObjectType.Group || (obj.GroupMembers != null && obj.GroupMembers.Count > 0))
            {
                double deltaAngle = normalizedAngle - obj.RotationAngle;
                RotateInPlace(obj, deltaAngle);
                obj.RotationAngle = normalizedAngle;
                return;
            }

            // 2. Nét vẽ Polyline / Polygon / Line: Xoay các điểm trực tiếp theo delta quanh tâm
            if (obj.Element is System.Windows.Shapes.Polyline || obj.Element is System.Windows.Shapes.Polygon || obj.Element is System.Windows.Shapes.Line)
            {
                double deltaAngle = normalizedAngle - obj.RotationAngle;
                RotateInPlace(obj, deltaAngle);
                obj.RotationAngle = normalizedAngle;
                return;
            }

            // 3. FrameworkElement / Path: Sử dụng RenderTransform RotateTransform
            obj.RotationAngle = normalizedAngle;
            obj.ApplyTransform();
            obj.UpdateBounds();
        }

        /// <summary>
        /// Rotate object theo delta angle
        /// </summary>
        public void RotateBy(SelectableObject obj, double deltaAngle)
        {
            if (obj == null || obj.IsLocked)
                return;

            double newAngle = (obj.RotationAngle + deltaAngle) % 360;
            Rotate(obj, newAngle);
        }

        /// <summary>
        /// ✅ EXCEL ROTATE FIX: Xoay đối tượng hoặc nhóm đối tượng tại chỗ quanh Tâm Bất Biến (Invariant Center)
        /// Theo đúng nguyên lý MS Excel: Tâm hình học C của đối tượng/nhóm đối tượng giữ nguyên 100% tọa độ.
        /// </summary>
        public void RotateInPlace(SelectableObject obj, double deltaAngleDegrees)
        {
            if (obj == null || obj.IsLocked) return;

            double rad = deltaAngleDegrees * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);

            // 1. Xử lý Nhóm đối tượng (Group / Multi-Selection)
            if (obj.IsGroup || obj.Type == ObjectType.Group || (obj.GroupMembers != null && obj.GroupMembers.Count > 0))
            {
                double groupCenterX = obj.Position.X + (obj.Size.Width / 2.0);
                double groupCenterY = obj.Position.Y + (obj.Size.Height / 2.0);

                foreach (var member in obj.GroupMembers)
                {
                    if (member == null || member.IsLocked) continue;

                    if (member.Element is System.Windows.Shapes.Polyline polyline && polyline.Points != null && polyline.Points.Count > 0)
                    {
                        for (int i = 0; i < polyline.Points.Count; i++)
                        {
                            var pt = polyline.Points[i];
                            double pdx = pt.X - groupCenterX;
                            double pdy = pt.Y - groupCenterY;
                            polyline.Points[i] = new Point(groupCenterX + (pdx * cos - pdy * sin), groupCenterY + (pdx * sin + pdy * cos));
                        }
                        polyline.RenderTransform = Transform.Identity;
                        member.RotationAngle = 0;
                        member.UpdateBounds();
                    }
                    else if (member.Element is System.Windows.Shapes.Polygon polygon && polygon.Points != null && polygon.Points.Count > 0)
                    {
                        for (int i = 0; i < polygon.Points.Count; i++)
                        {
                            var pt = polygon.Points[i];
                            double pdx = pt.X - groupCenterX;
                            double pdy = pt.Y - groupCenterY;
                            polygon.Points[i] = new Point(groupCenterX + (pdx * cos - pdy * sin), groupCenterY + (pdx * sin + pdy * cos));
                        }
                        polygon.RenderTransform = Transform.Identity;
                        member.RotationAngle = 0;
                        member.UpdateBounds();
                    }
                    else if (member.Element is System.Windows.Shapes.Line line)
                    {
                        double dx1 = line.X1 - groupCenterX;
                        double dy1 = line.Y1 - groupCenterY;
                        line.X1 = groupCenterX + (dx1 * cos - dy1 * sin);
                        line.Y1 = groupCenterY + (dx1 * sin + dy1 * cos);

                        double dx2 = line.X2 - groupCenterX;
                        double dy2 = line.Y2 - groupCenterY;
                        line.X2 = groupCenterX + (dx2 * cos - dy2 * sin);
                        line.Y2 = groupCenterY + (dx2 * sin + dy2 * cos);

                        line.RenderTransform = Transform.Identity;
                        member.RotationAngle = 0;
                        member.UpdateBounds();
                    }
                    else if (member.Element is System.Windows.Shapes.Path path && path.Data != null)
                    {
                        // Path sử dụng TransformGroup - xoay vòng quanh group center
                        var tg = path.RenderTransform as TransformGroup ?? new TransformGroup();
                        if (path.RenderTransform is not TransformGroup)
                        {
                            tg = new TransformGroup();
                            if (path.RenderTransform != null && path.RenderTransform != Transform.Identity)
                                tg.Children.Add(path.RenderTransform);
                        }
                        // Thêm rotate quanh groupCenter
                        tg.Children.Add(new RotateTransform(deltaAngleDegrees, groupCenterX, groupCenterY));
                        path.RenderTransform = tg;
                        member.UpdateBounds();
                    }
                    else if (member.Element is FrameworkElement fe)
                    {
                        double memCenterX = member.Position.X + (member.Size.Width / 2.0);
                        double memCenterY = member.Position.Y + (member.Size.Height / 2.0);

                        double dx = memCenterX - groupCenterX;
                        double dy = memCenterY - groupCenterY;

                        double newCenterX = groupCenterX + (dx * cos - dy * sin);
                        double newCenterY = groupCenterY + (dx * sin + dy * cos);

                        member.RotationAngle = (member.RotationAngle + deltaAngleDegrees) % 360;
                        member.Position = new Point(
                            newCenterX - (member.Size.Width / 2.0),
                            newCenterY - (member.Size.Height / 2.0)
                        );
                        CanvasControl.SetLeft(fe, member.Position.X);
                        CanvasControl.SetTop(fe, member.Position.Y);
                        member.ApplyTransform();
                        member.UpdateBounds();
                    }
                }

                // ✅ FIX: Recalculate group bounds từ members
                obj.RotationAngle = 0;
                RecalculateGroupBounds(obj);
                System.Diagnostics.Debug.WriteLine($"🔄 Group RotateInPlace: {obj.GroupMembers?.Count} members processed, new Bounds={obj.Bounds}");
                return;
            }

            // 2. Xử lý Đối tượng Đơn lẻ
            double centerX = obj.Position.X + (obj.Size.Width / 2.0);
            double centerY = obj.Position.Y + (obj.Size.Height / 2.0);

            if (obj.Element is System.Windows.Shapes.Polyline singlePolyline && singlePolyline.Points != null && singlePolyline.Points.Count > 0)
            {
                for (int i = 0; i < singlePolyline.Points.Count; i++)
                {
                    var pt = singlePolyline.Points[i];
                    double dx = pt.X - centerX;
                    double dy = pt.Y - centerY;
                    singlePolyline.Points[i] = new Point(centerX + (dx * cos - dy * sin), centerY + (dx * sin + dy * cos));
                }
                singlePolyline.RenderTransform = Transform.Identity;
                obj.RotationAngle = 0;
                obj.UpdateBounds();
            }
            else if (obj.Element is System.Windows.Shapes.Polygon singlePolygon && singlePolygon.Points != null && singlePolygon.Points.Count > 0)
            {
                for (int i = 0; i < singlePolygon.Points.Count; i++)
                {
                    var pt = singlePolygon.Points[i];
                    double dx = pt.X - centerX;
                    double dy = pt.Y - centerY;
                    singlePolygon.Points[i] = new Point(centerX + (dx * cos - dy * sin), centerY + (dx * sin + dy * cos));
                }
                singlePolygon.RenderTransform = Transform.Identity;
                obj.RotationAngle = 0;
                obj.UpdateBounds();
            }
            else if (obj.Element is System.Windows.Shapes.Line singleLine)
            {
                double dx1 = singleLine.X1 - centerX;
                double dy1 = singleLine.Y1 - centerY;
                singleLine.X1 = centerX + (dx1 * cos - dy1 * sin);
                singleLine.Y1 = centerY + (dx1 * sin + dy1 * cos);

                double dx2 = singleLine.X2 - centerX;
                double dy2 = singleLine.Y2 - centerY;
                singleLine.X2 = centerX + (dx2 * cos - dy2 * sin);
                singleLine.Y2 = centerY + (dx2 * sin + dy2 * cos);

                singleLine.RenderTransform = Transform.Identity;
                obj.RotationAngle = 0;
                obj.UpdateBounds();
            }
            else
            {
                obj.RotationAngle = (obj.RotationAngle + deltaAngleDegrees) % 360;
                obj.ApplyTransform();
                obj.UpdateBounds();
            }
        }

        /// <summary>
        /// Calculate rotation angle từ mouse position so với đỉnh trên (Top Handle = 0°)
        /// </summary>
        public double CalculateRotationAngle(Point center, Point currentPoint)
        {
            Vector vector = currentPoint - center;
            double angleRadians = Math.Atan2(vector.Y, vector.X);
            double angleDegrees = angleRadians * 180.0 / Math.PI;
            
            // Chốt xoay nằm ở đỉnh trên (12h, vector = (0, -1)).
            // Atan2(0, -1) = -90° (270°). Thêm 90° để quy chuẩn đỉnh trên là 0°.
            double angleFromTop = (angleDegrees + 90.0) % 360.0;
            if (angleFromTop < 0)
                angleFromTop += 360.0;

            return angleFromTop;
        }

        #endregion

        #region Public Methods - Flip

        /// <summary>
        /// Flip horizontal (lật ngang) quanh tâm bất biến của đối tượng hoặc nhóm đối tượng
        /// </summary>
        public void FlipHorizontal(SelectableObject obj)
        {
            if (obj == null || obj.IsLocked)
                return;

            double groupCenterX = obj.Position.X + (obj.Size.Width / 2.0);

            if (obj.IsGroup || obj.Type == ObjectType.Group || (obj.GroupMembers != null && obj.GroupMembers.Count > 0))
            {
                foreach (var member in obj.GroupMembers)
                {
                    if (member == null || member.IsLocked) continue;

                    if (member.Element is System.Windows.Shapes.Polyline polyline && polyline.Points != null && polyline.Points.Count > 0)
                    {
                        for (int i = 0; i < polyline.Points.Count; i++)
                        {
                            var pt = polyline.Points[i];
                            polyline.Points[i] = new Point(2 * groupCenterX - pt.X, pt.Y);
                        }
                        polyline.RenderTransform = Transform.Identity;
                        if (!double.IsNaN(CanvasControl.GetLeft(polyline))) CanvasControl.SetLeft(polyline, double.NaN);
                        if (!double.IsNaN(CanvasControl.GetTop(polyline))) CanvasControl.SetTop(polyline, double.NaN);
                        member.UpdateBounds();
                    }
                    else if (member.Element is System.Windows.Shapes.Polygon polygon && polygon.Points != null && polygon.Points.Count > 0)
                    {
                        for (int i = 0; i < polygon.Points.Count; i++)
                        {
                            var pt = polygon.Points[i];
                            polygon.Points[i] = new Point(2 * groupCenterX - pt.X, pt.Y);
                        }
                        polygon.RenderTransform = Transform.Identity;
                        if (!double.IsNaN(CanvasControl.GetLeft(polygon))) CanvasControl.SetLeft(polygon, double.NaN);
                        if (!double.IsNaN(CanvasControl.GetTop(polygon))) CanvasControl.SetTop(polygon, double.NaN);
                        member.UpdateBounds();
                    }
                    else if (member.Element is System.Windows.Shapes.Line line)
                    {
                        line.X1 = 2 * groupCenterX - line.X1;
                        line.X2 = 2 * groupCenterX - line.X2;
                        line.RenderTransform = Transform.Identity;
                        if (!double.IsNaN(CanvasControl.GetLeft(line))) CanvasControl.SetLeft(line, double.NaN);
                        if (!double.IsNaN(CanvasControl.GetTop(line))) CanvasControl.SetTop(line, double.NaN);
                        member.UpdateBounds();
                    }
                    else if (member.Element is System.Windows.Shapes.Path path && path.Data != null)
                    {
                        // Path: thêm ScaleTransform flip quanh groupCenterX
                        var tg = path.RenderTransform as TransformGroup ?? new TransformGroup();
                        if (path.RenderTransform is not TransformGroup)
                        {
                            tg = new TransformGroup();
                            if (path.RenderTransform != null && path.RenderTransform != Transform.Identity)
                                tg.Children.Add(path.RenderTransform);
                        }
                        tg.Children.Add(new ScaleTransform(-1, 1, groupCenterX, 0));
                        path.RenderTransform = tg;
                        member.UpdateBounds();
                    }
                    else
                    {
                        if (member.Scale == null)
                            member.Scale = new ScaleTransform(1, 1);
                        member.Scale.ScaleX *= -1;

                        double newLeft = 2 * groupCenterX - (member.Position.X + member.Size.Width);
                        member.Position = new Point(newLeft, member.Position.Y);
                        if (member.Element != null)
                        {
                            System.Windows.Controls.Canvas.SetLeft(member.Element, newLeft);
                        }
                        member.ApplyTransform();
                        member.UpdateBounds();
                    }
                }
                // ✅ FIX: Recalculate group bounds từ members
                RecalculateGroupBounds(obj);
                System.Diagnostics.Debug.WriteLine($"↔️ Group FlipH: {obj.GroupMembers?.Count} members processed, new Bounds={obj.Bounds}");
                return;
            }

            // Đối tượng đơn lẻ
            double centerX = groupCenterX;

            if (obj.Element is System.Windows.Shapes.Polyline singlePolyline && singlePolyline.Points != null && singlePolyline.Points.Count > 0)
            {
                for (int i = 0; i < singlePolyline.Points.Count; i++)
                {
                    var pt = singlePolyline.Points[i];
                    singlePolyline.Points[i] = new Point(2 * centerX - pt.X, pt.Y);
                }
                singlePolyline.RenderTransform = Transform.Identity;
                if (!double.IsNaN(CanvasControl.GetLeft(singlePolyline))) CanvasControl.SetLeft(singlePolyline, double.NaN);
                if (!double.IsNaN(CanvasControl.GetTop(singlePolyline))) CanvasControl.SetTop(singlePolyline, double.NaN);
                obj.UpdateBounds();
                return;
            }

            if (obj.Element is System.Windows.Shapes.Polygon singlePolygon && singlePolygon.Points != null && singlePolygon.Points.Count > 0)
            {
                for (int i = 0; i < singlePolygon.Points.Count; i++)
                {
                    var pt = singlePolygon.Points[i];
                    singlePolygon.Points[i] = new Point(2 * centerX - pt.X, pt.Y);
                }
                singlePolygon.RenderTransform = Transform.Identity;
                if (!double.IsNaN(CanvasControl.GetLeft(singlePolygon))) CanvasControl.SetLeft(singlePolygon, double.NaN);
                if (!double.IsNaN(CanvasControl.GetTop(singlePolygon))) CanvasControl.SetTop(singlePolygon, double.NaN);
                obj.UpdateBounds();
                return;
            }

            if (obj.Element is System.Windows.Shapes.Line singleLine)
            {
                singleLine.X1 = 2 * centerX - singleLine.X1;
                singleLine.X2 = 2 * centerX - singleLine.X2;
                singleLine.RenderTransform = Transform.Identity;
                if (!double.IsNaN(CanvasControl.GetLeft(singleLine))) CanvasControl.SetLeft(singleLine, double.NaN);
                if (!double.IsNaN(CanvasControl.GetTop(singleLine))) CanvasControl.SetTop(singleLine, double.NaN);
                obj.UpdateBounds();
                return;
            }

            if (obj.Scale == null)
                obj.Scale = new ScaleTransform(1, 1);

            obj.Scale.ScaleX *= -1;
            obj.ApplyTransform();
            obj.UpdateBounds();
        }

        /// <summary>
        /// Flip vertical (lật dọc) quanh tâm bất biến của đối tượng hoặc nhóm đối tượng
        /// </summary>
        public void FlipVertical(SelectableObject obj)
        {
            if (obj == null || obj.IsLocked)
                return;

            double groupCenterY = obj.Position.Y + (obj.Size.Height / 2.0);

            if (obj.IsGroup || obj.Type == ObjectType.Group || (obj.GroupMembers != null && obj.GroupMembers.Count > 0))
            {
                foreach (var member in obj.GroupMembers)
                {
                    if (member == null || member.IsLocked) continue;

                    if (member.Element is System.Windows.Shapes.Polyline polyline && polyline.Points != null && polyline.Points.Count > 0)
                    {
                        for (int i = 0; i < polyline.Points.Count; i++)
                        {
                            var pt = polyline.Points[i];
                            polyline.Points[i] = new Point(pt.X, 2 * groupCenterY - pt.Y);
                        }
                        polyline.RenderTransform = Transform.Identity;
                        if (!double.IsNaN(CanvasControl.GetLeft(polyline))) CanvasControl.SetLeft(polyline, double.NaN);
                        if (!double.IsNaN(CanvasControl.GetTop(polyline))) CanvasControl.SetTop(polyline, double.NaN);
                        member.UpdateBounds();
                    }
                    else if (member.Element is System.Windows.Shapes.Polygon polygon && polygon.Points != null && polygon.Points.Count > 0)
                    {
                        for (int i = 0; i < polygon.Points.Count; i++)
                        {
                            var pt = polygon.Points[i];
                            polygon.Points[i] = new Point(pt.X, 2 * groupCenterY - pt.Y);
                        }
                        polygon.RenderTransform = Transform.Identity;
                        if (!double.IsNaN(CanvasControl.GetLeft(polygon))) CanvasControl.SetLeft(polygon, double.NaN);
                        if (!double.IsNaN(CanvasControl.GetTop(polygon))) CanvasControl.SetTop(polygon, double.NaN);
                        member.UpdateBounds();
                    }
                    else if (member.Element is System.Windows.Shapes.Line line)
                    {
                        line.Y1 = 2 * groupCenterY - line.Y1;
                        line.Y2 = 2 * groupCenterY - line.Y2;
                        line.RenderTransform = Transform.Identity;
                        if (!double.IsNaN(CanvasControl.GetLeft(line))) CanvasControl.SetLeft(line, double.NaN);
                        if (!double.IsNaN(CanvasControl.GetTop(line))) CanvasControl.SetTop(line, double.NaN);
                        member.UpdateBounds();
                    }
                    else if (member.Element is System.Windows.Shapes.Path path && path.Data != null)
                    {
                        // Path: thêm ScaleTransform flip quanh groupCenterY
                        var tg = path.RenderTransform as TransformGroup ?? new TransformGroup();
                        if (path.RenderTransform is not TransformGroup)
                        {
                            tg = new TransformGroup();
                            if (path.RenderTransform != null && path.RenderTransform != Transform.Identity)
                                tg.Children.Add(path.RenderTransform);
                        }
                        tg.Children.Add(new ScaleTransform(1, -1, 0, groupCenterY));
                        path.RenderTransform = tg;
                        member.UpdateBounds();
                    }
                    else
                    {
                        if (member.Scale == null)
                            member.Scale = new ScaleTransform(1, 1);
                        member.Scale.ScaleY *= -1;

                        double newTop = 2 * groupCenterY - (member.Position.Y + member.Size.Height);
                        member.Position = new Point(member.Position.X, newTop);
                        if (member.Element != null)
                        {
                            System.Windows.Controls.Canvas.SetTop(member.Element, newTop);
                        }
                        member.ApplyTransform();
                        member.UpdateBounds();
                    }
                }
                // ✅ FIX: Recalculate group bounds từ members
                RecalculateGroupBounds(obj);
                System.Diagnostics.Debug.WriteLine($"↕️ Group FlipV: {obj.GroupMembers?.Count} members processed, new Bounds={obj.Bounds}");
                return;
            }

            // Đối tượng đơn lẻ
            double centerY = groupCenterY;

            if (obj.Element is System.Windows.Shapes.Polyline singlePolyline && singlePolyline.Points != null && singlePolyline.Points.Count > 0)
            {
                for (int i = 0; i < singlePolyline.Points.Count; i++)
                {
                    var pt = singlePolyline.Points[i];
                    singlePolyline.Points[i] = new Point(pt.X, 2 * centerY - pt.Y);
                }
                singlePolyline.RenderTransform = Transform.Identity;
                if (!double.IsNaN(CanvasControl.GetLeft(singlePolyline))) CanvasControl.SetLeft(singlePolyline, double.NaN);
                if (!double.IsNaN(CanvasControl.GetTop(singlePolyline))) CanvasControl.SetTop(singlePolyline, double.NaN);
                obj.UpdateBounds();
                return;
            }

            if (obj.Element is System.Windows.Shapes.Polygon singlePolygon && singlePolygon.Points != null && singlePolygon.Points.Count > 0)
            {
                for (int i = 0; i < singlePolygon.Points.Count; i++)
                {
                    var pt = singlePolygon.Points[i];
                    singlePolygon.Points[i] = new Point(pt.X, 2 * centerY - pt.Y);
                }
                singlePolygon.RenderTransform = Transform.Identity;
                if (!double.IsNaN(CanvasControl.GetLeft(singlePolygon))) CanvasControl.SetLeft(singlePolygon, double.NaN);
                if (!double.IsNaN(CanvasControl.GetTop(singlePolygon))) CanvasControl.SetTop(singlePolygon, double.NaN);
                obj.UpdateBounds();
                return;
            }

            if (obj.Element is System.Windows.Shapes.Line singleLine)
            {
                singleLine.Y1 = 2 * centerY - singleLine.Y1;
                singleLine.Y2 = 2 * centerY - singleLine.Y2;
                singleLine.RenderTransform = Transform.Identity;
                if (!double.IsNaN(CanvasControl.GetLeft(singleLine))) CanvasControl.SetLeft(singleLine, double.NaN);
                if (!double.IsNaN(CanvasControl.GetTop(singleLine))) CanvasControl.SetTop(singleLine, double.NaN);
                obj.UpdateBounds();
                return;
            }

            if (obj.Scale == null)
                obj.Scale = new ScaleTransform(1, 1);

            obj.Scale.ScaleY *= -1;
            obj.ApplyTransform();
            obj.UpdateBounds();
        }

        #endregion

        #region Public Methods - Scale

        /// <summary>
        /// Scale object (uniform)
        /// </summary>
        public void Scale(SelectableObject obj, double scaleFactor)
        {
            if (obj == null || obj.IsLocked)
                return;

            scaleFactor = Math.Max(0.1, Math.Min(10, scaleFactor)); // Clamp 0.1 - 10x

            Size newSize = new Size(
                obj.Size.Width * scaleFactor,
                obj.Size.Height * scaleFactor
            );

            Resize(obj, newSize);
        }

        /// <summary>
        /// Scale object (non-uniform)
        /// </summary>
        public void Scale(SelectableObject obj, double scaleX, double scaleY)
        {
            if (obj == null || obj.IsLocked)
                return;

            scaleX = Math.Max(0.1, Math.Min(10, scaleX));
            scaleY = Math.Max(0.1, Math.Min(10, scaleY));

            Size newSize = new Size(
                obj.Size.Width * scaleX,
                obj.Size.Height * scaleY
            );

            Resize(obj, newSize);
        }

        #endregion

        #region Public Methods - Batch Transform

        /// <summary>
        /// Apply transform to multiple objects
        /// </summary>
        public void BatchMove(System.Collections.Generic.List<SelectableObject> objects, Vector delta)
        {
            foreach (var obj in objects)
            {
                MoveBy(obj, delta);
            }
        }

        /// <summary>
        /// Batch rotate
        /// </summary>
        public void BatchRotate(System.Collections.Generic.List<SelectableObject> objects, double deltaAngle)
        {
            foreach (var obj in objects)
            {
                RotateBy(obj, deltaAngle);
            }
        }

        /// <summary>
        /// Batch scale
        /// </summary>
        public void BatchScale(System.Collections.Generic.List<SelectableObject> objects, double scaleFactor)
        {
            foreach (var obj in objects)
            {
                Scale(obj, scaleFactor);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get center point của object
        /// </summary>
        public Point GetCenter(SelectableObject obj)
        {
            return new Point(
                obj.Position.X + obj.Size.Width / 2,
                obj.Position.Y + obj.Size.Height / 2
            );
        }

        /// <summary>
        /// Snap to grid
        /// </summary>
        public Point SnapToGrid(Point point, double gridSize = 10)
        {
            return new Point(
                Math.Round(point.X / gridSize) * gridSize,
                Math.Round(point.Y / gridSize) * gridSize
            );
        }

        #endregion

        #region Private Helper - Group Bounds

        /// <summary>
        /// ✅ Tính lại bounds cho virtual group container từ bounds của tất cả members.
        /// Cần thiết vì groupContainer.Element == null nên UpdateBounds() không hoạt động.
        /// </summary>
        private void RecalculateGroupBounds(SelectableObject group)
        {
            if (group?.GroupMembers == null || group.GroupMembers.Count == 0)
            {
                group?.UpdateBounds();
                return;
            }

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var member in group.GroupMembers)
            {
                if (member == null) continue;
                var b = member.Bounds;
                if (!b.IsEmpty && b.Width > 0 && b.Height > 0)
                {
                    minX = Math.Min(minX, b.Left);
                    minY = Math.Min(minY, b.Top);
                    maxX = Math.Max(maxX, b.Right);
                    maxY = Math.Max(maxY, b.Bottom);
                }
            }

            if (minX < double.MaxValue && minY < double.MaxValue)
            {
                group.Position = new Point(minX, minY);
                group.Size = new Size(maxX - minX, maxY - minY);
                group.Bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
            }
        }

        #endregion
    }
}
