using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartTouch.Helpers;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Base class cho mọi đối tượng có thể chọn trên canvas
    /// </summary>
    public class SelectableObject
    {
        #region Properties

        /// <summary>
        /// ID duy nhất của đối tượng
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// UI Element tương ứng trên canvas
        /// </summary>
        public UIElement? Element { get; set; }

        /// <summary>
        /// Vùng bao quanh đối tượng (bounding box)
        /// </summary>
        public Rect Bounds { get; set; }

        /// <summary>
        /// Đối tượng có đang được chọn không
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Đối tượng có bị khóa không (không thể di chuyển/chỉnh sửa)
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// Thứ tự lớp (ZIndex) để xác định object nào ở trên
        /// </summary>
        public int ZIndex { get; set; }

        /// <summary>
        /// Loại đối tượng
        /// </summary>
        public ObjectType Type { get; set; }

        /// <summary>
        /// Vị trí (góc trên trái)
        /// </summary>
        public Point Position { get; set; }

        /// <summary>
        /// Kích thước
        /// </summary>
        public Size Size { get; set; }

        /// <summary>
        /// Góc xoay (degrees)
        /// </summary>
        public double RotationAngle { get; set; }

        /// <summary>
        /// Scale transform
        /// </summary>
        public ScaleTransform Scale { get; set; }

        /// <summary>
        /// Stroke thickness (độ dày nét)
        /// </summary>
        public double StrokeThickness { get; set; }

        /// <summary>
        /// Stroke color (màu nét)
        /// </summary>
        public Color StrokeColor { get; set; }

        /// <summary>
        /// Fill color (màu tô)
        /// </summary>
        public Color FillColor { get; set; }

        /// <summary>
        /// Text content (nếu là text object)
        /// </summary>
        public string? TextContent { get; set; }

        /// <summary>
        /// Font family (nếu là text)
        /// </summary>
        public string FontFamily { get; set; }

        /// <summary>
        /// Font size (nếu là text)
        /// </summary>
        public double FontSize { get; set; }

        /// <summary>
        /// Group ID - objects with same GroupId belong to same group
        /// </summary>
        public string? GroupId { get; set; }

        /// <summary>
        /// Is this object a group container
        /// </summary>
        public bool IsGroup { get; set; }

        /// <summary>
        /// Child objects if this is a group
        /// </summary>
        public List<SelectableObject> GroupMembers { get; set; }

        /// <summary>
        /// URL liên kết (nếu có) — double-click để mở
        /// </summary>
        public string? LinkUrl { get; set; }

        #endregion

        #region Constructor

        public SelectableObject()
        {
            Id = Guid.NewGuid();
            IsSelected = false;
            IsLocked = false;
            ZIndex = 0;
            RotationAngle = 0;
            Scale = new ScaleTransform(1.0, 1.0);
            StrokeThickness = 2.0;
            StrokeColor = Colors.Black;
            FillColor = Colors.Transparent;
            FontSize = 14;
            FontFamily = "Segoe UI";
            GroupId = null;
            IsGroup = false;
            GroupMembers = new List<SelectableObject>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Kiểm tra xem một điểm có nằm trong object không
        /// </summary>
        public virtual bool HitTest(Point point)
        {
            return Bounds.Contains(point);
        }

        /// <summary>
        /// IMP-1604: Cập nhật bounds sử dụng BoundsHelper khi có Canvas cha.
        /// Fallback về Position/Size nếu element chưa được add vào Canvas.
        /// </summary>
        public virtual void UpdateBounds()
        {
            // ✅ FIX CẢM ỨNG: Cập nhật bounds cho Virtual Group Container từ các thành viên GroupMembers
            if (IsGroup && GroupMembers != null && GroupMembers.Count > 0)
            {
                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;
                foreach (var member in GroupMembers)
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
                    var aabb = new Rect(minX, minY, maxX - minX, maxY - minY);
                    Bounds = aabb;
                    if (RotationAngle == 0 || Size.Width <= 0 || Size.Height <= 0)
                    {
                        Position = new Point(minX, minY);
                        Size = new Size(maxX - minX, maxY - minY);
                    }
                    return;
                }
            }

            if (Element != null)
            {
                // 1. Polyline (nét vẽ tay)
                if (Element is System.Windows.Shapes.Polyline polyline && polyline.Points != null && polyline.Points.Count > 0)
                {
                    double minX = double.MaxValue, minY = double.MaxValue;
                    double maxX = double.MinValue, maxY = double.MinValue;
                    foreach (var pt in polyline.Points)
                    {
                        minX = Math.Min(minX, pt.X); minY = Math.Min(minY, pt.Y);
                        maxX = Math.Max(maxX, pt.X); maxY = Math.Max(maxY, pt.Y);
                    }
                    double pad = Math.Max(polyline.StrokeThickness / 2.0, 2.0);
                    var aabb = new Rect(minX - pad, minY - pad, Math.Max(maxX - minX + pad * 2, 1), Math.Max(maxY - minY + pad * 2, 1));
                    Bounds = aabb;
                    if (RotationAngle == 0 || Size.Width <= 0 || Size.Height <= 0)
                    {
                        Position = new Point(aabb.X, aabb.Y);
                        Size = new Size(aabb.Width, aabb.Height);
                    }
                    return;
                }

                // 2. Polygon (đa giác)
                if (Element is System.Windows.Shapes.Polygon polygon && polygon.Points != null && polygon.Points.Count > 0)
                {
                    double minX = double.MaxValue, minY = double.MaxValue;
                    double maxX = double.MinValue, maxY = double.MinValue;
                    foreach (var pt in polygon.Points)
                    {
                        minX = Math.Min(minX, pt.X); minY = Math.Min(minY, pt.Y);
                        maxX = Math.Max(maxX, pt.X); maxY = Math.Max(maxY, pt.Y);
                    }
                    double pad = Math.Max(polygon.StrokeThickness / 2.0, 2.0);
                    var aabb = new Rect(minX - pad, minY - pad, Math.Max(maxX - minX + pad * 2, 1), Math.Max(maxY - minY + pad * 2, 1));
                    Bounds = aabb;
                    if (RotationAngle == 0 || Size.Width <= 0 || Size.Height <= 0)
                    {
                        Position = new Point(aabb.X, aabb.Y);
                        Size = new Size(aabb.Width, aabb.Height);
                    }
                    return;
                }

                // 3. Line (đoạn thẳng)
                if (Element is System.Windows.Shapes.Line line)
                {
                    double minX = Math.Min(line.X1, line.X2);
                    double minY = Math.Min(line.Y1, line.Y2);
                    double maxX = Math.Max(line.X1, line.X2);
                    double maxY = Math.Max(line.Y1, line.Y2);
                    double pad = Math.Max(line.StrokeThickness / 2.0, 2.0);
                    var aabb = new Rect(minX - pad, minY - pad, Math.Max(maxX - minX + pad * 2, 1), Math.Max(maxY - minY + pad * 2, 1));
                    Bounds = aabb;
                    if (RotationAngle == 0 || Size.Width <= 0 || Size.Height <= 0)
                    {
                        Position = new Point(aabb.X, aabb.Y);
                        Size = new Size(aabb.Width, aabb.Height);
                    }
                    return;
                }

                // 4. Path (nét mượt Bezier / Shape Path / ArrowLine)
                if (Element is System.Windows.Shapes.Path path && path.Data != null)
                {
                    var dataBounds = path.Data.Bounds;
                    Rect transformedBounds = dataBounds;
                    if (path.RenderTransform != null && path.RenderTransform != Transform.Identity)
                    {
                        transformedBounds = path.RenderTransform.TransformBounds(dataBounds);
                    }
                    else
                    {
                        double left = Canvas.GetLeft(path);
                        double top = Canvas.GetTop(path);
                        if (!double.IsNaN(left) && !double.IsNaN(top))
                        {
                            transformedBounds = new Rect(left, top, Math.Max(dataBounds.Width, 1), Math.Max(dataBounds.Height, 1));
                        }
                    }

                    double pad = Math.Max(path.StrokeThickness / 2.0, 2.0);
                    var aabb = new Rect(transformedBounds.X - pad, transformedBounds.Y - pad, Math.Max(transformedBounds.Width + pad * 2, 1), Math.Max(transformedBounds.Height + pad * 2, 1));
                    Bounds = aabb;
                    if (RotationAngle == 0 || Size.Width <= 0 || Size.Height <= 0)
                    {
                        Position = new Point(aabb.X, aabb.Y);
                        Size = new Size(aabb.Width, aabb.Height);
                    }
                    return;
                }

                // 5. FrameworkElement (Image, TextBlock, Border, Rectangle, Ellipse...)
                if (Element is FrameworkElement fe)
                {
                    double left = Canvas.GetLeft(fe);
                    double top = Canvas.GetTop(fe);
                    if (double.IsNaN(left)) left = Position.X;
                    if (double.IsNaN(top)) top = Position.Y;
                    double w = !double.IsNaN(fe.Width) && fe.Width > 0 ? fe.Width : (fe.ActualWidth > 0 ? fe.ActualWidth : Size.Width);
                    double h = !double.IsNaN(fe.Height) && fe.Height > 0 ? fe.Height : (fe.ActualHeight > 0 ? fe.ActualHeight : Size.Height);
                    if (double.IsNaN(w) || w <= 0) w = fe.RenderSize.Width > 0 ? fe.RenderSize.Width : Size.Width;
                    if (double.IsNaN(h) || h <= 0) h = fe.RenderSize.Height > 0 ? fe.RenderSize.Height : Size.Height;

                    // ✅ QC_4.2_CANVAS_SCALE_SYNC: Đồng bộ kích thước cho Canvas khối 3D khi có ScaleTransform
                    if (fe is Canvas)
                    {
                        double scaleX = 1.0, scaleY = 1.0;
                        if (fe.RenderTransform is ScaleTransform st)
                        {
                            scaleX = st.ScaleX;
                            scaleY = st.ScaleY;
                        }
                        else if (fe.RenderTransform is TransformGroup tg)
                        {
                            var foundSt = tg.Children.OfType<ScaleTransform>().FirstOrDefault();
                            if (foundSt != null)
                            {
                                scaleX = foundSt.ScaleX;
                                scaleY = foundSt.ScaleY;
                            }
                        }
                        double baseWidth = !double.IsNaN(fe.Width) && fe.Width > 0 ? fe.Width : (fe.ActualWidth > 0 ? fe.ActualWidth : 200);
                        double baseHeight = !double.IsNaN(fe.Height) && fe.Height > 0 ? fe.Height : (fe.ActualHeight > 0 ? fe.ActualHeight : 200);
                        w = Math.Abs(baseWidth * scaleX);
                        h = Math.Abs(baseHeight * scaleY);
                    }

                    Position = new Point(left, top);
                    Size = new Size(Math.Max(w, 1), Math.Max(h, 1));

                    var parentCanvas = FindParentCanvas(Element);
                    if (parentCanvas != null)
                    {
                        var absoluteBounds = BoundsHelper.GetAbsoluteBounds(Element, parentCanvas);
                        if (!absoluteBounds.IsEmpty && absoluteBounds.Width > 0 && absoluteBounds.Height > 0)
                        {
                            Bounds = absoluteBounds;
                            return;
                        }
                    }
                    Bounds = new Rect(left, top, Size.Width, Size.Height);
                    return;
                }

                // Fallback: dùng Position/Size hiện tại
                Bounds = new Rect(Position.X, Position.Y, Size.Width, Size.Height);
            }
            else
            {
                // Fallback khi Element == null
                Bounds = new Rect(Position.X, Position.Y, Math.Max(0, Size.Width), Math.Max(0, Size.Height));
            }
        }

        /// <summary>
        /// IMP-1604: Tìm Canvas cha gần nhất của element trong visual tree.
        /// </summary>
        private static Canvas? FindParentCanvas(UIElement element)
        {
            var parent = VisualTreeHelper.GetParent(element);
            while (parent != null)
            {
                if (parent is Canvas canvas)
                    return canvas;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        /// <summary>
        /// Tạo bản sao của object
        /// </summary>
        public virtual SelectableObject Clone()
        {
            return new SelectableObject
            {
                Type = this.Type,
                Position = new Point(this.Position.X + 10, this.Position.Y + 10), // Offset để thấy rõ
                Size = this.Size,
                RotationAngle = this.RotationAngle,
                StrokeThickness = this.StrokeThickness,
                StrokeColor = this.StrokeColor,
                FillColor = this.FillColor,
                TextContent = this.TextContent,
                FontFamily = this.FontFamily,
                FontSize = this.FontSize,
                ZIndex = this.ZIndex + 1
            };
        }

        /// <summary>
        /// Apply transform to element
        /// </summary>
        public virtual void ApplyTransform()
        {
            if (Element != null)
            {
                // Bảo toàn TranslateTransform hiện tại của Path nếu có
                TranslateTransform? existingTranslate = null;
                if (Element is System.Windows.Shapes.Path path)
                {
                    if (path.RenderTransform is TransformGroup existingTg)
                    {
                        existingTranslate = existingTg.Children.OfType<TranslateTransform>().FirstOrDefault();
                    }
                    else if (path.RenderTransform is TranslateTransform tt)
                    {
                        existingTranslate = tt;
                    }
                }

                var transformGroup = new TransformGroup();

                double left = System.Windows.Controls.Canvas.GetLeft(Element);
                double top = System.Windows.Controls.Canvas.GetTop(Element);
                bool isCanvasPositioned = !double.IsNaN(left) && !double.IsNaN(top);

                // 1. Existing translation for Path
                if (existingTranslate != null)
                {
                    transformGroup.Children.Add(new TranslateTransform(existingTranslate.X, existingTranslate.Y));
                }

                // 2. Scale / Flip (Thực hiện trước Rotation)
                ScaleTransform? existingScale = null;
                if (Element.RenderTransform is TransformGroup currentTg)
                {
                    existingScale = currentTg.Children.OfType<ScaleTransform>().FirstOrDefault();
                }
                else if (Element.RenderTransform is ScaleTransform st)
                {
                    existingScale = st;
                }

                double scaleFactorX = 1.0, scaleFactorY = 1.0;
                if (Scale != null && (Scale.ScaleX != 1 || Scale.ScaleY != 1))
                {
                    scaleFactorX = Scale.ScaleX;
                    scaleFactorY = Scale.ScaleY;
                    var scaleTransform = new ScaleTransform(Scale.ScaleX, Scale.ScaleY);
                    transformGroup.Children.Add(scaleTransform);
                }
                else if (existingScale != null)
                {
                    scaleFactorX = existingScale.ScaleX;
                    scaleFactorY = existingScale.ScaleY;
                    transformGroup.Children.Add(existingScale.Clone());
                }

                // 3. Rotation (Xoay quanh tâm sau khi đã áp dụng tỉ lệ Scale)
                if (RotationAngle != 0)
                {
                    var rotateTransform = new RotateTransform(RotationAngle);
                    if (isCanvasPositioned && Element is FrameworkElement fe)
                    {
                        double w = !double.IsNaN(fe.Width) && fe.Width > 0 ? fe.Width : (fe.ActualWidth > 0 ? fe.ActualWidth : Size.Width);
                        double h = !double.IsNaN(fe.Height) && fe.Height > 0 ? fe.Height : (fe.ActualHeight > 0 ? fe.ActualHeight : Size.Height);
                        rotateTransform.CenterX = (w * scaleFactorX) / 2.0;
                        rotateTransform.CenterY = (h * scaleFactorY) / 2.0;
                    }
                    else
                    {
                        rotateTransform.CenterX = Position.X + (Size.Width / 2.0);
                        rotateTransform.CenterY = Position.Y + (Size.Height / 2.0);
                    }
                    transformGroup.Children.Add(rotateTransform);
                }

                Element.RenderTransform = transformGroup.Children.Count > 0 ? transformGroup : Transform.Identity;
            }
        }

        #endregion
    }

    /// <summary>
    /// Enum định nghĩa các loại đối tượng
    /// </summary>
    public enum ObjectType
    {
        /// <summary>Nét vẽ bút (InkCanvas stroke)</summary>
        Stroke,
        
        /// <summary>Text box</summary>
        Text,
        
        /// <summary>Hình ảnh</summary>
        Image,
        
        /// <summary>Hình học cơ bản (rectangle, circle, line...)</summary>
        Shape,
        
        /// <summary>Biểu đồ (chart)</summary>
        Chart,
        
        /// <summary>Công thức toán học</summary>
        Formula,
        
        /// <summary>Dụng cụ đo (ruler, protractor, compass...)</summary>
        Tool,
        
        /// <summary>Nét vẽ đã smooth (Bezier Path)</summary>
        Drawing,
        
        /// <summary>Group nhiều objects</summary>
        Group,
        
        /// <summary>Khác</summary>
        Other
    }
}
