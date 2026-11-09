using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartTouch.Helpers
{
    /// <summary>
    /// QC_4.2_SHAPE_RECOGNITION: Thuật toán nhận diện nét vẽ tay và nắn thành hình học chuẩn (Line, Rectangle, Ellipse, Triangle).
    /// Hoạt động tức thì sau khi nhấc tay/chuột (MouseUp / TouchUp), độ trễ dưới 1ms.
    /// </summary>
    public static class ShapeRecognizer
    {
        /// <summary>
        /// Thử nhận diện nét vẽ Polyline thành hình học chuẩn. Trả về null nếu nét vẽ tự do không khớp hình cơ bản.
        /// </summary>
        public static UIElement? TryRecognizeShape(Polyline stroke, Color strokeColor, double thickness)
        {
            if (stroke == null || stroke.Points == null || stroke.Points.Count < 5)
                return null;

            var points = stroke.Points;
            int count = points.Count;

            Point start = points[0];
            Point end = points[count - 1];

            // 1. Tính tổng chiều dài nét vẽ và Bounding Box
            double totalLength = 0;
            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;

            for (int i = 0; i < count; i++)
            {
                var pt = points[i];
                if (pt.X < minX) minX = pt.X;
                if (pt.X > maxX) maxX = pt.X;
                if (pt.Y < minY) minY = pt.Y;
                if (pt.Y > maxY) maxY = pt.Y;

                if (i > 0)
                {
                    double dx = pt.X - points[i - 1].X;
                    double dy = pt.Y - points[i - 1].Y;
                    totalLength += Math.Sqrt(dx * dx + dy * dy);
                }
            }

            double width = maxX - minX;
            double height = maxY - minY;
            double maxDim = Math.Max(width, height);

            if (maxDim < 20 || totalLength < 25)
                return null; // Nét quá nhỏ (ví dụ chấm chữ, viết chữ thường) -> không nhận dạng

            double directDist = Math.Sqrt((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y));

            // TH1: Đường thẳng (Line)
            // Nếu khoảng cách giữa 2 đầu mút chiếm > 88% tổng chiều dài nét vẽ
            if (directDist / totalLength > 0.88)
            {
                var line = new Line
                {
                    X1 = start.X,
                    Y1 = start.Y,
                    X2 = end.X,
                    Y2 = end.Y,
                    Stroke = new SolidColorBrush(strokeColor),
                    StrokeThickness = thickness,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                return line;
            }

            // TH2: Hình khép kín (Đầu và đuôi nét vẽ gần chạm nhau)
            bool isClosed = (directDist < maxDim * 0.35) || (directDist < 45);

            if (isClosed)
            {
                // Tính diện tích đa giác vẽ tay (Shoelace formula)
                double area = 0;
                for (int i = 0; i < count; i++)
                {
                    Point p1 = points[i];
                    Point p2 = points[(i + 1) % count];
                    area += (p1.X * p2.Y) - (p2.X * p1.Y);
                }
                double polyArea = Math.Abs(area) / 2.0;
                double boxArea = width * height;

                if (boxArea > 0)
                {
                    double areaRatio = polyArea / boxArea;
                    int corners = DetectCorners(points);

                    // 1. Tam giác (3 góc gãy hoặc diện tích tam giác xấp xỉ 0.5 box)
                    if (corners == 3 || (areaRatio >= 0.35 && areaRatio <= 0.60 && corners <= 4))
                    {
                        var triangle = CreateTriangle(minX, minY, width, height, strokeColor, thickness);
                        return triangle;
                    }

                    // 2. Hình chữ nhật / Hình vuông (4 góc hoặc lấp đầy > 82% bounding box)
                    if (corners == 4 || areaRatio > 0.82)
                    {
                        var rect = new Rectangle
                        {
                            Width = width,
                            Height = height,
                            Stroke = new SolidColorBrush(strokeColor),
                            StrokeThickness = thickness,
                            StrokeLineJoin = PenLineJoin.Miter
                        };
                        System.Windows.Controls.Canvas.SetLeft(rect, minX);
                        System.Windows.Controls.Canvas.SetTop(rect, minY);
                        return rect;
                    }

                    // 3. Hình tròn / Elip (diện tích elip = pi/4 * W * H ≈ 0.785 * boxArea)
                    if (areaRatio >= 0.62 && areaRatio <= 0.88)
                    {
                        // Nếu tỉ lệ chiều rộng/cao gần bằng nhau -> nắn thành hình tròn đều
                        bool isCircle = Math.Abs(width - height) / maxDim < 0.25;
                        double finalW = isCircle ? maxDim : width;
                        double finalH = isCircle ? maxDim : height;
                        double finalLeft = isCircle ? (minX + width / 2.0 - maxDim / 2.0) : minX;
                        double finalTop = isCircle ? (minY + height / 2.0 - maxDim / 2.0) : minY;

                        var ellipse = new Ellipse
                        {
                            Width = finalW,
                            Height = finalH,
                            Stroke = new SolidColorBrush(strokeColor),
                            StrokeThickness = thickness
                        };
                        System.Windows.Controls.Canvas.SetLeft(ellipse, finalLeft);
                        System.Windows.Controls.Canvas.SetTop(ellipse, finalTop);
                        return ellipse;
                    }
                }
            }

            return null; // Không nhận diện được -> giữ nguyên nét vẽ tự do
        }

        private static int DetectCorners(PointCollection points)
        {
            if (points.Count < 10) return 0;
            int step = Math.Max(3, points.Count / 16);
            int corners = 0;

            for (int i = step; i < points.Count - step; i += step)
            {
                Point pPrev = points[i - step];
                Point pCurr = points[i];
                Point pNext = points[i + step];

                double v1x = pCurr.X - pPrev.X;
                double v1y = pCurr.Y - pPrev.Y;
                double v2x = pNext.X - pCurr.X;
                double v2y = pNext.Y - pCurr.Y;

                double dot = v1x * v2x + v1y * v2y;
                double mag1 = Math.Sqrt(v1x * v1x + v1y * v1y);
                double mag2 = Math.Sqrt(v2x * v2x + v2y * v2y);

                if (mag1 > 0 && mag2 > 0)
                {
                    double cosAngle = Math.Clamp(dot / (mag1 * mag2), -1.0, 1.0);
                    double angleDeg = Math.Acos(cosAngle) * (180.0 / Math.PI);
                    if (angleDeg > 50 && angleDeg < 140)
                    {
                        corners++;
                    }
                }
            }

            return corners;
        }

        private static Polygon CreateTriangle(double minX, double minY, double width, double height, Color strokeColor, double thickness)
        {
            var triangle = new Polygon
            {
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = thickness,
                StrokeLineJoin = PenLineJoin.Round
            };

            triangle.Points.Add(new Point(minX + width / 2.0, minY));
            triangle.Points.Add(new Point(minX, minY + height));
            triangle.Points.Add(new Point(minX + width, minY + height));

            return triangle;
        }
    }
}
