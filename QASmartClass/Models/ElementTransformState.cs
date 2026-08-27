using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Snapshot trạng thái hình học & biến đổi (Position, Size, RenderTransform, Points, Line coords, etc.)
    /// của một phần tử UI và SelectableObject để phục vụ Undo/Redo cho các thao tác Move, Resize, Rotate.
    /// </summary>
    public class ElementTransformState
    {
        public Point Position { get; set; }
        public Size Size { get; set; }
        public double RotationAngle { get; set; }

        public double CanvasLeft { get; set; } = double.NaN;
        public double CanvasTop { get; set; } = double.NaN;
        public double Width { get; set; } = double.NaN;
        public double Height { get; set; } = double.NaN;

        public Transform? RenderTransform { get; set; }

        // Dành cho Polyline (nét vẽ tay)
        public Point[]? PolylinePoints { get; set; }

        // Dành cho Polygon (đa giác, hình tam giác, ngôi sao, ...)
        public Point[]? PolygonPoints { get; set; }

        // Dành cho Line (đoạn thẳng)
        public double LineX1 { get; set; }
        public double LineY1 { get; set; }
        public double LineX2 { get; set; }
        public double LineY2 { get; set; }

        /// <summary>
        /// Tạo snapshot từ một SelectableObject
        /// </summary>
        public static ElementTransformState Create(SelectableObject obj)
        {
            if (obj == null) return new ElementTransformState();

            var state = new ElementTransformState
            {
                Position = obj.Position,
                Size = obj.Size,
                RotationAngle = obj.RotationAngle
            };

            if (obj.Element != null)
            {
                double left = Canvas.GetLeft(obj.Element);
                double top = Canvas.GetTop(obj.Element);
                state.CanvasLeft = left;
                state.CanvasTop = top;

                if (obj.Element is FrameworkElement fe)
                {
                    state.Width = fe.Width;
                    state.Height = fe.Height;
                    if (fe.RenderTransform != null && fe.RenderTransform != Transform.Identity)
                    {
                        state.RenderTransform = fe.RenderTransform.Clone();
                    }
                }

                if (obj.Element is Polyline polyline && polyline.Points != null && polyline.Points.Count > 0)
                {
                    state.PolylinePoints = polyline.Points.ToArray();
                    if (polyline.RenderTransform != null && polyline.RenderTransform != Transform.Identity)
                    {
                        state.RenderTransform = polyline.RenderTransform.Clone();
                    }
                }
                else if (obj.Element is Polygon polygon && polygon.Points != null && polygon.Points.Count > 0)
                {
                    state.PolygonPoints = polygon.Points.ToArray();
                    if (polygon.RenderTransform != null && polygon.RenderTransform != Transform.Identity)
                    {
                        state.RenderTransform = polygon.RenderTransform.Clone();
                    }
                }
                else if (obj.Element is Line line)
                {
                    state.LineX1 = line.X1;
                    state.LineY1 = line.Y1;
                    state.LineX2 = line.X2;
                    state.LineY2 = line.Y2;
                    if (line.RenderTransform != null && line.RenderTransform != Transform.Identity)
                    {
                        state.RenderTransform = line.RenderTransform.Clone();
                    }
                }
                else if (obj.Element is Path path)
                {
                    if (path.RenderTransform != null && path.RenderTransform != Transform.Identity)
                    {
                        state.RenderTransform = path.RenderTransform.Clone();
                    }
                }
            }

            return state;
        }

        /// <summary>
        /// Khôi phục trạng thái cho UIElement và SelectableObject
        /// </summary>
        public void ApplyTo(UIElement? element, SelectableObject? obj = null)
        {
            if (element is Polyline polyline)
            {
                if (PolylinePoints != null)
                {
                    polyline.Points = new PointCollection(PolylinePoints);
                }
                polyline.RenderTransform = RenderTransform != null ? RenderTransform.Clone() : Transform.Identity;
            }
            else if (element is Polygon polygon)
            {
                if (PolygonPoints != null)
                {
                    polygon.Points = new PointCollection(PolygonPoints);
                }
                polygon.RenderTransform = RenderTransform != null ? RenderTransform.Clone() : Transform.Identity;
            }
            else if (element is Line line)
            {
                line.X1 = LineX1;
                line.Y1 = LineY1;
                line.X2 = LineX2;
                line.Y2 = LineY2;
                if (!double.IsNaN(CanvasLeft)) Canvas.SetLeft(line, CanvasLeft);
                if (!double.IsNaN(CanvasTop)) Canvas.SetTop(line, CanvasTop);
                line.RenderTransform = RenderTransform != null ? RenderTransform.Clone() : Transform.Identity;
            }
            else if (element is Path path)
            {
                path.RenderTransform = RenderTransform != null ? RenderTransform.Clone() : Transform.Identity;
                if (!double.IsNaN(CanvasLeft)) Canvas.SetLeft(path, CanvasLeft);
                if (!double.IsNaN(CanvasTop)) Canvas.SetTop(path, CanvasTop);
            }
            else if (element is FrameworkElement fe)
            {
                if (!double.IsNaN(CanvasLeft)) Canvas.SetLeft(fe, CanvasLeft);
                if (!double.IsNaN(CanvasTop)) Canvas.SetTop(fe, CanvasTop);
                if (!double.IsNaN(Width) && Width > 0) fe.Width = Width;
                if (!double.IsNaN(Height) && Height > 0) fe.Height = Height;
                fe.RenderTransform = RenderTransform != null ? RenderTransform.Clone() : Transform.Identity;
            }

            if (obj != null)
            {
                obj.Position = Position;
                obj.Size = Size;
                obj.RotationAngle = RotationAngle;
                obj.UpdateBounds();
            }
        }

        /// <summary>
        /// So sánh xem hai snapshot có sự thay đổi đáng kể về mặt hình học hay không (> 0.5px)
        /// </summary>
        public bool IsDifferentFrom(ElementTransformState? other)
        {
            if (other == null) return true;
            if (Math.Abs(Position.X - other.Position.X) > 0.5 || Math.Abs(Position.Y - other.Position.Y) > 0.5) return true;
            if (Math.Abs(Size.Width - other.Size.Width) > 0.5 || Math.Abs(Size.Height - other.Size.Height) > 0.5) return true;
            if (Math.Abs(RotationAngle - other.RotationAngle) > 0.5) return true;

            if (!double.IsNaN(CanvasLeft) && !double.IsNaN(other.CanvasLeft) && Math.Abs(CanvasLeft - other.CanvasLeft) > 0.5) return true;
            if (!double.IsNaN(CanvasTop) && !double.IsNaN(other.CanvasTop) && Math.Abs(CanvasTop - other.CanvasTop) > 0.5) return true;
            if (!double.IsNaN(Width) && !double.IsNaN(other.Width) && Math.Abs(Width - other.Width) > 0.5) return true;
            if (!double.IsNaN(Height) && !double.IsNaN(other.Height) && Math.Abs(Height - other.Height) > 0.5) return true;

            if (PolylinePoints != null && other.PolylinePoints != null)
            {
                if (PolylinePoints.Length != other.PolylinePoints.Length) return true;
                if (PolylinePoints.Length > 0 && other.PolylinePoints.Length > 0)
                {
                    if (Math.Abs(PolylinePoints[0].X - other.PolylinePoints[0].X) > 0.5 || Math.Abs(PolylinePoints[0].Y - other.PolylinePoints[0].Y) > 0.5) return true;
                    int lastIdx = PolylinePoints.Length - 1;
                    if (Math.Abs(PolylinePoints[lastIdx].X - other.PolylinePoints[lastIdx].X) > 0.5 || Math.Abs(PolylinePoints[lastIdx].Y - other.PolylinePoints[lastIdx].Y) > 0.5) return true;
                }
            }
            else if ((PolylinePoints == null) != (other.PolylinePoints == null))
            {
                return true;
            }

            if (PolygonPoints != null && other.PolygonPoints != null)
            {
                if (PolygonPoints.Length != other.PolygonPoints.Length) return true;
                if (PolygonPoints.Length > 0 && other.PolygonPoints.Length > 0)
                {
                    if (Math.Abs(PolygonPoints[0].X - other.PolygonPoints[0].X) > 0.5 || Math.Abs(PolygonPoints[0].Y - other.PolygonPoints[0].Y) > 0.5) return true;
                    int lastIdx = PolygonPoints.Length - 1;
                    if (Math.Abs(PolygonPoints[lastIdx].X - other.PolygonPoints[lastIdx].X) > 0.5 || Math.Abs(PolygonPoints[lastIdx].Y - other.PolygonPoints[lastIdx].Y) > 0.5) return true;
                }
            }
            else if ((PolygonPoints == null) != (other.PolygonPoints == null))
            {
                return true;
            }

            if (Math.Abs(LineX1 - other.LineX1) > 0.5 || Math.Abs(LineY1 - other.LineY1) > 0.5 ||
                Math.Abs(LineX2 - other.LineX2) > 0.5 || Math.Abs(LineY2 - other.LineY2) > 0.5)
            {
                return true;
            }

            return false;
        }
    }
}
