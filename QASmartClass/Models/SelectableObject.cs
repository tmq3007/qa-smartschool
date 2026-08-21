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
                    Position = new Point(minX, minY);
                    Size = new Size(maxX - minX, maxY - minY);
                    Bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
                    return;
                }
            }

            if (Element != null)
            {
                // Thử lấy Canvas cha để dùng BoundsHelper
                var parentCanvas = FindParentCanvas(Element);
                if (parentCanvas != null)
                {
                    var absoluteBounds = BoundsHelper.GetAbsoluteBounds(Element, parentCanvas);
                    if (!absoluteBounds.IsEmpty)
                    {
                        Bounds = absoluteBounds;
                        Position = new Point(absoluteBounds.X, absoluteBounds.Y);
                        Size = new Size(absoluteBounds.Width, absoluteBounds.Height);
                        return;
                    }
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
                var transformGroup = new TransformGroup();

                double left = System.Windows.Controls.Canvas.GetLeft(Element);
                double top = System.Windows.Controls.Canvas.GetTop(Element);
                bool isCanvasPositioned = !double.IsNaN(left) && !double.IsNaN(top);

                // Rotation
                if (RotationAngle != 0)
                {
                    var rotateTransform = new RotateTransform(RotationAngle);
                    if (isCanvasPositioned)
                    {
                        rotateTransform.CenterX = Size.Width / 2.0;
                        rotateTransform.CenterY = Size.Height / 2.0;
                    }
                    else
                    {
                        rotateTransform.CenterX = Position.X + (Size.Width / 2.0);
                        rotateTransform.CenterY = Position.Y + (Size.Height / 2.0);
                    }
                    transformGroup.Children.Add(rotateTransform);
                }

                // Scale / Flip
                if (Scale != null)
                {
                    if (isCanvasPositioned)
                    {
                        Scale.CenterX = Size.Width / 2.0;
                        Scale.CenterY = Size.Height / 2.0;
                    }
                    else
                    {
                        Scale.CenterX = Position.X + (Size.Width / 2.0);
                        Scale.CenterY = Position.Y + (Size.Height / 2.0);
                    }
                    transformGroup.Children.Add(Scale);
                }

                Element.RenderTransform = transformGroup;
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
