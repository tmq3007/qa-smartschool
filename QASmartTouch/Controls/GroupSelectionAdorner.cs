using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace QASmartTouch.Controls
{
    /// <summary>
    /// Group Selection Adorner - Hiển thị 1 selection box duy nhất cho nhiều objects
    /// Tính bounding box chung và hiển thị viền nét đứt + handles
    /// </summary>
    public class GroupSelectionAdorner : Adorner
    {
        #region Fields

        private Pen _dashedPen;
        private Brush _handleBrush;
        private Pen _handlePen;
        private const double HandleSize = 10; // ✅ G1.3: Tăng kích thước chấm điều khiển để dễ nắn cảm ứng
        private Rect _groupBounds;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor nhận canvas làm adornedElement và danh sách bounds của các objects
        /// </summary>
        public GroupSelectionAdorner(UIElement adornedElement, Rect groupBounds) : base(adornedElement)
        {
            _groupBounds = groupBounds;

            // ✅ G1.3: Viền nét đứt màu xanh dương (#2196F3 - Material Blue)
            _dashedPen = new Pen(new SolidColorBrush(Color.FromRgb(33, 150, 243)), 2)
            {
                DashStyle = DashStyles.Dash
            };
            _dashedPen.Freeze();

            // ✅ G1.3: Handle màu trắng với viền xanh, bo tròn hơn
            _handleBrush = Brushes.White;
            _handlePen = new Pen(new SolidColorBrush(Color.FromRgb(33, 150, 243)), 2);
            _handlePen.Freeze();

            // Không chặn mouse events
            IsHitTestVisible = false;
        }

        #endregion

        #region Overrides

        protected override void OnRender(DrawingContext drawingContext)
        {
            // Vẽ viền nét đứt xung quanh group bounds
            drawingContext.DrawRectangle(null, _dashedPen, _groupBounds);

            // Vẽ 8 handles: 4 góc + 4 cạnh
            // Góc
            DrawHandle(drawingContext, _groupBounds.TopLeft);
            DrawHandle(drawingContext, _groupBounds.TopRight);
            DrawHandle(drawingContext, _groupBounds.BottomLeft);
            DrawHandle(drawingContext, _groupBounds.BottomRight);

            // Cạnh
            DrawHandle(drawingContext, new Point(
                _groupBounds.Left + _groupBounds.Width / 2,
                _groupBounds.Top)); // Top center

            DrawHandle(drawingContext, new Point(
                _groupBounds.Left + _groupBounds.Width / 2,
                _groupBounds.Bottom)); // Bottom center

            DrawHandle(drawingContext, new Point(
                _groupBounds.Left,
                _groupBounds.Top + _groupBounds.Height / 2)); // Left center

            DrawHandle(drawingContext, new Point(
                _groupBounds.Right,
                _groupBounds.Top + _groupBounds.Height / 2)); // Right center
        }

        #endregion

        #region Private Methods

        private void DrawHandle(DrawingContext dc, Point center)
        {
            Rect handleRect = new Rect(
                center.X - HandleSize / 2,
                center.Y - HandleSize / 2,
                HandleSize,
                HandleSize
            );

            // ✅ G1.3: Vẽ handle hình tròn thay vì vuông (thẩm mỹ sư phạm hơn)
            dc.DrawEllipse(_handleBrush, _handlePen, center, HandleSize / 2, HandleSize / 2);
        }

        #endregion
    }
}
