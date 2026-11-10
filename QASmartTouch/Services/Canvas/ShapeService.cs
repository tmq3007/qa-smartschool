﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartTouch.Services.Canvas
{
    /// <summary>
    /// Implementation of shape creation and manipulation
    /// Handles all geometric shape operations
    /// </summary>
    public class ShapeService : IShapeService
    {
        #region IShapeService Implementation
        
        /// <inheritdoc/>
        public Rectangle CreateRectangle(Rect bounds, Color fillColor, Color strokeColor, double strokeThickness)
        {
            var rectangle = new Rectangle
            {
                Width = bounds.Width,
                Height = bounds.Height,
                Fill = new SolidColorBrush(fillColor),
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = strokeThickness
            };
            
            System.Windows.Controls.Canvas.SetLeft(rectangle, bounds.Left);
            System.Windows.Controls.Canvas.SetTop(rectangle, bounds.Top);
            
            System.Diagnostics.Debug.WriteLine($"▭ Rectangle created: {bounds.Width}x{bounds.Height} at ({bounds.Left}, {bounds.Top})");
            
            return rectangle;
        }
        
        /// <inheritdoc/>
        public Ellipse CreateEllipse(Rect bounds, Color fillColor, Color strokeColor, double strokeThickness)
        {
            var ellipse = new Ellipse
            {
                Width = bounds.Width,
                Height = bounds.Height,
                Fill = new SolidColorBrush(fillColor),
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = strokeThickness
            };
            
            System.Windows.Controls.Canvas.SetLeft(ellipse, bounds.Left);
            System.Windows.Controls.Canvas.SetTop(ellipse, bounds.Top);
            
            System.Diagnostics.Debug.WriteLine($"⬭ Ellipse created: {bounds.Width}x{bounds.Height} at ({bounds.Left}, {bounds.Top})");
            
            return ellipse;
        }
        
        /// <inheritdoc/>
        public Line CreateLine(Point start, Point end, Color strokeColor, double strokeThickness)
        {
            var line = new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = strokeThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            
            System.Diagnostics.Debug.WriteLine($"─ Line created: ({start.X}, {start.Y}) → ({end.X}, {end.Y})");
            
            return line;
        }
        
        /// <inheritdoc/>
        public System.Windows.Shapes.Path CreateArrow(Point start, Point end, Color strokeColor, double strokeThickness)
        {
            var arrowPath = new System.Windows.Shapes.Path
            {
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = strokeThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            };
            
            // Create arrow geometry
            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = start };
            
            // Main line
            figure.Segments.Add(new LineSegment(end, true));
            
            // Arrow head
            var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
            var arrowHeadLength = strokeThickness * 5;
            var arrowHeadAngle = Math.PI / 6; // 30 degrees
            
            var arrowPoint1 = new Point(
                end.X - arrowHeadLength * Math.Cos(angle - arrowHeadAngle),
                end.Y - arrowHeadLength * Math.Sin(angle - arrowHeadAngle)
            );
            
            var arrowPoint2 = new Point(
                end.X - arrowHeadLength * Math.Cos(angle + arrowHeadAngle),
                end.Y - arrowHeadLength * Math.Sin(angle + arrowHeadAngle)
            );
            
            // Add arrow head lines
            var arrowFigure1 = new PathFigure { StartPoint = end };
            arrowFigure1.Segments.Add(new LineSegment(arrowPoint1, true));
            
            var arrowFigure2 = new PathFigure { StartPoint = end };
            arrowFigure2.Segments.Add(new LineSegment(arrowPoint2, true));
            
            geometry.Figures.Add(figure);
            geometry.Figures.Add(arrowFigure1);
            geometry.Figures.Add(arrowFigure2);
            
            arrowPath.Data = geometry;
            
            System.Diagnostics.Debug.WriteLine($"→ Arrow created: ({start.X}, {start.Y}) → ({end.X}, {end.Y})");
            
            return arrowPath;
        }
        
        /// <inheritdoc/>
        public void ResizeShape(Shape shape, Rect newBounds)
        {
            if (shape == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Cannot resize null shape");
                return;
            }
            
            shape.Width = newBounds.Width;
            shape.Height = newBounds.Height;
            
            System.Windows.Controls.Canvas.SetLeft(shape, newBounds.Left);
            System.Windows.Controls.Canvas.SetTop(shape, newBounds.Top);
            
            System.Diagnostics.Debug.WriteLine($"↔️ Shape resized to: {newBounds.Width}x{newBounds.Height}");
        }
        
        /// <inheritdoc/>
        public void MoveShape(Shape shape, Point newPosition)
        {
            if (shape == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Cannot move null shape");
                return;
            }
            
            System.Windows.Controls.Canvas.SetLeft(shape, newPosition.X);
            System.Windows.Controls.Canvas.SetTop(shape, newPosition.Y);
            
            System.Diagnostics.Debug.WriteLine($"↔️ Shape moved to: ({newPosition.X}, {newPosition.Y})");
        }
        
        /// <inheritdoc/>
        public void RotateShape(Shape shape, double angleDegrees)
        {
            if (shape == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Cannot rotate null shape");
                return;
            }
            
            var rotateTransform = new RotateTransform(angleDegrees);
            
            // Set rotation center to shape center
            rotateTransform.CenterX = shape.Width / 2;
            rotateTransform.CenterY = shape.Height / 2;
            
            shape.RenderTransform = rotateTransform;
            
            System.Diagnostics.Debug.WriteLine($"↻ Shape rotated: {angleDegrees}°");
        }
        
        /// <inheritdoc/>
        public Rect GetShapeBounds(Shape shape)
        {
            if (shape == null)
            {
                return Rect.Empty;
            }
            
            var left = System.Windows.Controls.Canvas.GetLeft(shape);
            var top = System.Windows.Controls.Canvas.GetTop(shape);
            
            // Handle NaN values
            if (double.IsNaN(left)) left = 0;
            if (double.IsNaN(top)) top = 0;
            
            return new Rect(left, top, shape.Width, shape.Height);
        }
        
        #endregion
    }
}
