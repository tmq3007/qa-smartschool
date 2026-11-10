using QASmartTouch.Services;
﻿using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace QASmartTouch.Controls
{
    /// <summary>
    /// Adorner hiển thị viền nét đứt và resize handles cho object được chọn
    /// Adorner là một layer overlay nằm trên UIElement để vẽ decoration
    /// </summary>
    public class SelectionAdorner : Adorner
    {
        #region Fields

        private Pen _dashedPen;
        private Brush _handleBrush;
        private Pen _handlePen;
        private const double HandleSize = 8;

        #endregion

        #region Constructor

        private SelectionManager? _manager;

        public SelectionAdorner(UIElement adornedElement, SelectionManager? manager = null) : base(adornedElement)
        {
            _manager = manager;

            // Viền nét đứt màu xanh dương (#2196F3 - Material Blue)
            _dashedPen = new Pen(new SolidColorBrush(Color.FromRgb(33, 150, 243)), 2)
            {
                DashStyle = DashStyles.Dash
            };
            _dashedPen.Freeze(); // Freeze để tối ưu performance

            // Handle màu trắng với viền xanh
            _handleBrush = Brushes.White;
            _handlePen = new Pen(new SolidColorBrush(Color.FromRgb(33, 150, 243)), 2);
            _handlePen.Freeze();

            // Kích hoạt hỗ trợ cảm ứng đa điểm (Multi-touch Manipulation) trên khung chọn
            IsHitTestVisible = true;
            IsManipulationEnabled = true;

            ManipulationStarting += SelectionAdorner_ManipulationStarting;
            ManipulationDelta += SelectionAdorner_ManipulationDelta;
        }

        private void SelectionAdorner_ManipulationStarting(object sender, System.Windows.Input.ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = VisualTreeHelper.GetParent(this) as UIElement;
            e.Mode = System.Windows.Input.ManipulationModes.All;
            e.Handled = true;
        }

        private void SelectionAdorner_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            var element = AdornedElement as FrameworkElement;
            if (element == null)
                return;

            var delta = e.DeltaManipulation;

            // === 1. Di chuyển (Translation) ===
            double currentLeft = System.Windows.Controls.Canvas.GetLeft(element);
            double currentTop = System.Windows.Controls.Canvas.GetTop(element);
            if (double.IsNaN(currentLeft)) currentLeft = 0;
            if (double.IsNaN(currentTop)) currentTop = 0;

            double newLeft = currentLeft + delta.Translation.X;
            double newTop = currentTop + delta.Translation.Y;

            System.Windows.Controls.Canvas.SetLeft(element, newLeft);
            System.Windows.Controls.Canvas.SetTop(element, newTop);

            // === 2. Co giãn (Scale) và Xoay (Rotation) ===
            TransformGroup transformGroup;
            if (element.RenderTransform is TransformGroup existingGroup)
            {
                transformGroup = existingGroup;
            }
            else
            {
                transformGroup = new TransformGroup();
                if (element.RenderTransform != null && element.RenderTransform != Transform.Identity)
                {
                    transformGroup.Children.Add(element.RenderTransform);
                }
                element.RenderTransform = transformGroup;
            }

            ScaleTransform? scaleTransform = null;
            foreach (var child in transformGroup.Children)
            {
                if (child is ScaleTransform st)
                {
                    scaleTransform = st;
                    break;
                }
            }
            if (scaleTransform == null)
            {
                scaleTransform = new ScaleTransform(1, 1);
                transformGroup.Children.Add(scaleTransform);
            }

            scaleTransform.ScaleX *= delta.Scale.X;
            scaleTransform.ScaleY *= delta.Scale.Y;

            RotateTransform? rotateTransform = null;
            foreach (var child in transformGroup.Children)
            {
                if (child is RotateTransform rt)
                {
                    rotateTransform = rt;
                    break;
                }
            }
            if (rotateTransform == null)
            {
                rotateTransform = new RotateTransform(0);
                transformGroup.Children.Add(rotateTransform);
            }

            rotateTransform.Angle += delta.Rotation;

            double centerX = element.ActualWidth / 2;
            double centerY = element.ActualHeight / 2;
            scaleTransform.CenterX = centerX;
            scaleTransform.CenterY = centerY;
            rotateTransform.CenterX = centerX;
            rotateTransform.CenterY = centerY;

            // === 3. Cập nhật SelectableObject & Rebuild QuadTree ===
            var selectableObj = _manager?.GetSelectableObjectForElement(element);
            if (selectableObj != null)
            {
                selectableObj.Position = new Point(newLeft, newTop);
                selectableObj.RotationAngle = rotateTransform.Angle;
                selectableObj.Scale = new ScaleTransform(scaleTransform.ScaleX, scaleTransform.ScaleY);
                selectableObj.UpdateBounds();

                _manager?.RebuildQuadTree();
            }

            e.Handled = true;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Vẽ adorner (viền và handles)
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect adornedElementRect = new Rect(AdornedElement.RenderSize);

            // Vẽ viền nét đứt xung quanh element
            drawingContext.DrawRectangle(null, _dashedPen, adornedElementRect);

            // Vẽ 8 handles: 4 góc + 4 cạnh
            // Góc
            DrawHandle(drawingContext, adornedElementRect.TopLeft);
            DrawHandle(drawingContext, adornedElementRect.TopRight);
            DrawHandle(drawingContext, adornedElementRect.BottomLeft);
            DrawHandle(drawingContext, adornedElementRect.BottomRight);

            // Cạnh
            DrawHandle(drawingContext, new Point(
                adornedElementRect.Left + adornedElementRect.Width / 2,
                adornedElementRect.Top)); // Top center

            DrawHandle(drawingContext, new Point(
                adornedElementRect.Left + adornedElementRect.Width / 2,
                adornedElementRect.Bottom)); // Bottom center

            DrawHandle(drawingContext, new Point(
                adornedElementRect.Left,
                adornedElementRect.Top + adornedElementRect.Height / 2)); // Left center

            DrawHandle(drawingContext, new Point(
                adornedElementRect.Right,
                adornedElementRect.Top + adornedElementRect.Height / 2)); // Right center
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Vẽ một resize handle tại vị trí center
        /// </summary>
        private void DrawHandle(DrawingContext dc, Point center)
        {
            Rect handleRect = new Rect(
                center.X - HandleSize / 2,
                center.Y - HandleSize / 2,
                HandleSize,
                HandleSize
            );

            // Vẽ hình vuông trắng với viền xanh
            dc.DrawRectangle(_handleBrush, _handlePen, handleRect);
        }

        #endregion
    }
}
