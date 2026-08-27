using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartTouch.Helpers
{
    /// <summary>
    /// IMP-1604: Static utility class cho việc tính toán Bounding Box chính xác
    /// của mọi loại UIElement so với Canvas cha.
    /// 
    /// Thay thế các phương thức Canvas.GetLeft/Top (gặp lỗi NaN → 0)
    /// bằng thuật toán TransformToVisual tổng quát.
    /// 
    /// Sử dụng bởi: EraserEngine.GetElementBounds, SelectableObject.UpdateBounds,
    /// SelectionManager.HitTest, Form2_MainDashboard.RegisterNewObject.
    /// </summary>
    public static class BoundsHelper
    {
        /// <summary>
        /// Tính toán Bounding Box tuyệt đối của một UIElement so với Canvas cha.
        /// Xử lý chính xác cho tất cả loại phần tử: Polyline, Line, Path, Shape,
        /// TextBlock, TextBox, Border, Image, và bất kỳ FrameworkElement nào.
        /// 
        /// Thuật toán:
        /// 1. Polyline → tính từ Points collection
        /// 2. Line → tính từ X1/Y1/X2/Y2
        /// 3. Path → dùng path.Data.Bounds
        /// 4. Tất cả khác → dùng TransformToVisual(parentCanvas)
        /// </summary>
        /// <param name="element">Phần tử cần tính bounds</param>
        /// <param name="parentCanvas">Canvas cha chứa phần tử</param>
        /// <returns>Rect bounds trong hệ tọa độ Canvas, hoặc Rect.Empty nếu không tính được</returns>
        public static Rect GetAbsoluteBounds(UIElement element, Canvas parentCanvas)
        {
            if (element == null || parentCanvas == null)
                return Rect.Empty;

            // === 🚀 ƯU TIÊN 1: Lấy vị trí và Bounding Box thị giác chính xác 100% qua TransformToAncestor ===
            try
            {
                GeneralTransform transform = element.TransformToAncestor(parentCanvas);

                // Đối với Polyline (nét vẽ tay đã bị di chuyển)
                if (element is Polyline polylineCheck && polylineCheck.Points.Count > 0)
                {
                    double minX = double.MaxValue, minY = double.MaxValue;
                    double maxX = double.MinValue, maxY = double.MinValue;
                    foreach (var pt in polylineCheck.Points)
                    {
                        if (pt.X < minX) minX = pt.X;
                        if (pt.Y < minY) minY = pt.Y;
                        if (pt.X > maxX) maxX = pt.X;
                        if (pt.Y > maxY) maxY = pt.Y;
                    }
                    Rect polyLocal = new Rect(minX, minY, Math.Max(maxX - minX, 1), Math.Max(maxY - minY, 1));
                    Rect polyTransformed = transform.TransformBounds(polyLocal);
                    if (!polyTransformed.IsEmpty && polyTransformed.Width > 0 && polyTransformed.Height > 0)
                    {
                        return polyTransformed;
                    }
                }

                // Đối với Polygon (hình tam giác, đa giác, ngôi sao, v.v.)
                if (element is Polygon polygonCheck && polygonCheck.Points.Count > 0)
                {
                    double minX = double.MaxValue, minY = double.MaxValue;
                    double maxX = double.MinValue, maxY = double.MinValue;
                    foreach (var pt in polygonCheck.Points)
                    {
                        if (pt.X < minX) minX = pt.X;
                        if (pt.Y < minY) minY = pt.Y;
                        if (pt.X > maxX) maxX = pt.X;
                        if (pt.Y > maxY) maxY = pt.Y;
                    }
                    double pad = Math.Max(polygonCheck.StrokeThickness / 2, 2);
                    Rect polyLocal = new Rect(minX, minY, Math.Max(maxX - minX, 1), Math.Max(maxY - minY, 1));
                    Rect polyTransformed = transform.TransformBounds(polyLocal);
                    if (!polyTransformed.IsEmpty && polyTransformed.Width > 0 && polyTransformed.Height > 0)
                    {
                        return new Rect(polyTransformed.X - pad, polyTransformed.Y - pad, polyTransformed.Width + pad * 2, polyTransformed.Height + pad * 2);
                    }
                }

                // Đối với Path (nét vẽ mượt Bezier, hình học dạng Path)
                if (element is Path pathCheck && pathCheck.Data != null)
                {
                    var dataBounds = pathCheck.Data.Bounds;
                    if (!dataBounds.IsEmpty && dataBounds.Width > 0 && dataBounds.Height > 0)
                    {
                        Rect pathTransformed = transform.TransformBounds(dataBounds);
                        if (!pathTransformed.IsEmpty && pathTransformed.Width > 0 && pathTransformed.Height > 0)
                        {
                            return pathTransformed;
                        }
                    }
                }

                // Đối với Line (nét vẽ đoạn thẳng)
                if (element is Line lineCheck)
                {
                    double minX = Math.Min(lineCheck.X1, lineCheck.X2);
                    double minY = Math.Min(lineCheck.Y1, lineCheck.Y2);
                    double maxX = Math.Max(lineCheck.X1, lineCheck.X2);
                    double maxY = Math.Max(lineCheck.Y1, lineCheck.Y2);
                    double pad = Math.Max(lineCheck.StrokeThickness / 2, 2);
                    Rect lineLocal = new Rect(minX, minY, Math.Max(maxX - minX, 1), Math.Max(maxY - minY, 1));
                    Rect lineTransformed = transform.TransformBounds(lineLocal);
                    if (!lineTransformed.IsEmpty && lineTransformed.Width > 0 && lineTransformed.Height > 0)
                    {
                        return new Rect(lineTransformed.X - pad, lineTransformed.Y - pad, lineTransformed.Width + pad * 2, lineTransformed.Height + pad * 2);
                    }
                }

                // Đối với các FrameworkElement khác (Image, TextBlock, Border, Rectangle, Ellipse)
                if (element is FrameworkElement feTrans)
                {
                    double w = feTrans.ActualWidth > 0 ? feTrans.ActualWidth : feTrans.Width;
                    double h = feTrans.ActualHeight > 0 ? feTrans.ActualHeight : feTrans.Height;
                    if (double.IsNaN(w) || w <= 0) w = feTrans.RenderSize.Width;
                    if (double.IsNaN(h) || h <= 0) h = feTrans.RenderSize.Height;

                    if (w > 0 && h > 0 && !(parentCanvas.ActualWidth > 0 && w >= parentCanvas.ActualWidth))
                    {
                        Rect localRect = new Rect(0, 0, w, h);
                        Rect transformed = transform.TransformBounds(localRect);
                        if (!transformed.IsEmpty && transformed.Width > 0 && transformed.Height > 0)
                        {
                            return transformed;
                        }
                    }
                }
            }
            catch { }

            // === Fallback 2: Polyline / Polygon tính từ Points + Canvas.GetLeft/Top ===
            if (element is Polyline polyline && polyline.Points.Count > 0)
            {
                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;

                foreach (var pt in polyline.Points)
                {
                    if (pt.X < minX) minX = pt.X;
                    if (pt.Y < minY) minY = pt.Y;
                    if (pt.X > maxX) maxX = pt.X;
                    if (pt.Y > maxY) maxY = pt.Y;
                }

                double width = Math.Max(maxX - minX, 1);
                double height = Math.Max(maxY - minY, 1);
                double pad = Math.Max(polyline.StrokeThickness / 2, 2);

                double cX = Canvas.GetLeft(polyline);
                double cY = Canvas.GetTop(polyline);
                if (double.IsNaN(cX)) cX = 0;
                if (double.IsNaN(cY)) cY = 0;

                return new Rect(cX + minX - pad, cY + minY - pad, width + pad * 2, height + pad * 2);
            }

            if (element is Polygon polygon && polygon.Points.Count > 0)
            {
                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;

                foreach (var pt in polygon.Points)
                {
                    if (pt.X < minX) minX = pt.X;
                    if (pt.Y < minY) minY = pt.Y;
                    if (pt.X > maxX) maxX = pt.X;
                    if (pt.Y > maxY) maxY = pt.Y;
                }

                double width = Math.Max(maxX - minX, 1);
                double height = Math.Max(maxY - minY, 1);
                double pad = Math.Max(polygon.StrokeThickness / 2, 2);

                double cX = Canvas.GetLeft(polygon);
                double cY = Canvas.GetTop(polygon);
                if (double.IsNaN(cX)) cX = 0;
                if (double.IsNaN(cY)) cY = 0;

                return new Rect(cX + minX - pad, cY + minY - pad, width + pad * 2, height + pad * 2);
            }

            // === Fallback 3: Line ===
            if (element is Line line)
            {
                double minX = Math.Min(line.X1, line.X2);
                double minY = Math.Min(line.Y1, line.Y2);
                double maxX = Math.Max(line.X1, line.X2);
                double maxY = Math.Max(line.Y1, line.Y2);

                double width = Math.Max(maxX - minX, Math.Max(line.StrokeThickness, 2));
                double height = Math.Max(maxY - minY, Math.Max(line.StrokeThickness, 2));
                double pad = Math.Max(line.StrokeThickness / 2, 2);

                double cX = Canvas.GetLeft(line);
                double cY = Canvas.GetTop(line);
                if (double.IsNaN(cX)) cX = 0;
                if (double.IsNaN(cY)) cY = 0;

                return new Rect(cX + minX - pad, cY + minY - pad, width + pad * 2, height + pad * 2);
            }

            // === Fallback 4: Path ===
            if (element is Path path && path.Data != null)
            {
                var dataBounds = path.Data.Bounds;
                if (!dataBounds.IsEmpty && dataBounds.Width > 0 && dataBounds.Height > 0)
                {
                    double cX = Canvas.GetLeft(path);
                    double cY = Canvas.GetTop(path);
                    if (double.IsNaN(cX)) cX = 0;
                    if (double.IsNaN(cY)) cY = 0;
                    return new Rect(cX + dataBounds.X, cY + dataBounds.Y, dataBounds.Width, dataBounds.Height);
                }
            }

            // === Fallback 5: FrameworkElement ===
            if (element is FrameworkElement fe)
            {
                if (!fe.IsMeasureValid)
                {
                    fe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                }

                double width = fe.ActualWidth > 0 ? fe.ActualWidth : fe.Width;
                double height = fe.ActualHeight > 0 ? fe.ActualHeight : fe.Height;
                if (double.IsNaN(width) || width <= 0) width = fe.RenderSize.Width;
                if (double.IsNaN(height) || height <= 0) height = fe.RenderSize.Height;
                if (width <= 0) width = 10;
                if (height <= 0) height = 10;

                double left = Canvas.GetLeft(fe);
                double top = Canvas.GetTop(fe);
                if (double.IsNaN(left)) left = 0;
                if (double.IsNaN(top)) top = 0;

                return new Rect(left, top, width, height);
            }

            return Rect.Empty;
        }

        /// <summary>
        /// IMP-1604: Kiểm tra nhanh xem một điểm có nằm trong bounds mở rộng
        /// (thêm tolerance cho cảm ứng IFP) không.
        /// </summary>
        /// <param name="bounds">Bounds gốc của đối tượng</param>
        /// <param name="point">Điểm chạm cần kiểm tra</param>
        /// <param name="tolerance">Dung sai pixel (mặc định 15px cho IFP)</param>
        /// <returns>True nếu điểm nằm trong bounds mở rộng</returns>
        public static bool HitTestWithTolerance(Rect bounds, Point point, double tolerance = 15)
        {
            var expanded = new Rect(
                bounds.X - tolerance,
                bounds.Y - tolerance,
                bounds.Width + tolerance * 2,
                bounds.Height + tolerance * 2);

            return expanded.Contains(point);
        }
    }
}
