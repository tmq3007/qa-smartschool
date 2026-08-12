using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartTouch.Managers;

namespace QASmartTouch.Services.Canvas
{
    /// <summary>
    /// Implementation of stroke management operations
    /// Handles all stroke-related operations and transformations
    /// </summary>
    public class StrokeService : IStrokeService
    {
        #region IStrokeService Implementation
        
        /// <inheritdoc/>
        public Polyline CreateStroke(Color color, double thickness, string? brushType = null)
        {
            var stroke = new Polyline
            {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness,
                StrokeLineJoin = PenLineJoin.Round,   // Round joins = smooth corners
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                UseLayoutRounding = false              // Sub-pixel precision
            };
            
            // Enable high-quality anti-aliasing for smooth edges
            RenderOptions.SetEdgeMode(stroke, EdgeMode.Unspecified);
            RenderOptions.SetBitmapScalingMode(stroke, BitmapScalingMode.HighQuality);
            RenderOptions.SetClearTypeHint(stroke, ClearTypeHint.Enabled);
            
            // Apply brush-specific settings
            if (!string.IsNullOrEmpty(brushType))
            {
                ApplyBrushSettings(stroke, brushType);
            }
            
            System.Diagnostics.Debug.WriteLine($"🖊️ Stroke created: Color={color}, Thickness={thickness}, Brush={brushType ?? "Normal"}");
            
            return stroke;
        }
        
        /// <inheritdoc/>
        public void AddPointToStroke(Polyline stroke, Point point)
        {
            if (stroke == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Cannot add point to null stroke");
                return;
            }
            
            stroke.Points.Add(point);
        }
        
        /// <inheritdoc/>
        public Polyline SmoothStroke(Polyline stroke)
        {
            if (stroke == null || stroke.Points.Count < 3)
            {
                return stroke;
            }
            
            // Create a new smoothed stroke
            var smoothedStroke = new Polyline
            {
                Stroke = stroke.Stroke,
                StrokeThickness = stroke.StrokeThickness,
                StrokeLineJoin = stroke.StrokeLineJoin,
                StrokeStartLineCap = stroke.StrokeStartLineCap,
                StrokeEndLineCap = stroke.StrokeEndLineCap,
                Opacity = stroke.Opacity
            };
            
            // Simple averaging algorithm for smoothing
            var points = stroke.Points.ToList();
            smoothedStroke.Points.Add(points[0]); // Keep first point
            
            for (int i = 1; i < points.Count - 1; i++)
            {
                var prevPoint = points[i - 1];
                var currentPoint = points[i];
                var nextPoint = points[i + 1];
                
                // Average with neighbors
                var smoothedX = (prevPoint.X + currentPoint.X + nextPoint.X) / 3.0;
                var smoothedY = (prevPoint.Y + currentPoint.Y + nextPoint.Y) / 3.0;
                
                smoothedStroke.Points.Add(new Point(smoothedX, smoothedY));
            }
            
            smoothedStroke.Points.Add(points[points.Count - 1]); // Keep last point
            
            System.Diagnostics.Debug.WriteLine($"✨ Stroke smoothed: {points.Count} → {smoothedStroke.Points.Count} points");
            
            return smoothedStroke;
        }
        
        /// <inheritdoc/>
        public void SetStrokeColor(Polyline stroke, Color color)
        {
            if (stroke == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Cannot set color on null stroke");
                return;
            }
            
            stroke.Stroke = new SolidColorBrush(color);
            System.Diagnostics.Debug.WriteLine($"🎨 Stroke color changed to: {color}");
        }
        
        /// <inheritdoc/>
        public void SetStrokeThickness(Polyline stroke, double thickness)
        {
            if (stroke == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Cannot set thickness on null stroke");
                return;
            }
            
            stroke.StrokeThickness = thickness;
            System.Diagnostics.Debug.WriteLine($"📏 Stroke thickness changed to: {thickness}");
        }
        
        /// <inheritdoc/>
        public double GetStrokeLength(Polyline stroke)
        {
            if (stroke == null || stroke.Points.Count < 2)
            {
                return 0;
            }
            
            double totalLength = 0;
            
            for (int i = 1; i < stroke.Points.Count; i++)
            {
                var p1 = stroke.Points[i - 1];
                var p2 = stroke.Points[i];
                
                var dx = p2.X - p1.X;
                var dy = p2.Y - p1.Y;
                
                totalLength += Math.Sqrt(dx * dx + dy * dy);
            }
            
            return totalLength;
        }
        
        /// <inheritdoc/>
        public Rect GetStrokeBounds(Polyline stroke)
        {
            if (stroke == null || stroke.Points.Count == 0)
            {
                return Rect.Empty;
            }
            
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            
            foreach (var point in stroke.Points)
            {
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }
            
            // Account for stroke thickness
            var halfThickness = stroke.StrokeThickness / 2.0;
            
            return new Rect(
                minX - halfThickness,
                minY - halfThickness,
                (maxX - minX) + stroke.StrokeThickness,
                (maxY - minY) + stroke.StrokeThickness
            );
        }
        
        #endregion
        
        #region Private Methods

        /// <inheritdoc/>
        public System.Windows.Shapes.Path? ConvertToSmoothPath(Polyline stroke)
        {
            if (stroke == null || stroke.Points.Count < 2)
                return null;

            var points = stroke.Points.ToList();

            // Build StreamGeometry with QuadraticBezierSegments
            // Technique: midpoint quadratic bezier — industry standard for smooth freehand
            // Each original point = bezier CONTROL point
            // Each midpoint between consecutive points = bezier END point
            // Result: smooth G1-continuous curve through all touch points
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(points[0], false, false);

                if (points.Count == 2)
                {
                    // Only 2 points: draw a straight line
                    ctx.LineTo(points[1], true, false);
                }
                else
                {
                    // Move to midpoint between first and second point
                    var firstMid = new Point(
                        (points[0].X + points[1].X) / 2.0,
                        (points[0].Y + points[1].Y) / 2.0);
                    ctx.LineTo(firstMid, true, false);

                    // For each inner point: draw QuadraticBezier
                    // Control = current point, End = midpoint to next point
                    for (int i = 1; i < points.Count - 1; i++)
                    {
                        var mid = new Point(
                            (points[i].X + points[i + 1].X) / 2.0,
                            (points[i].Y + points[i + 1].Y) / 2.0);

                        ctx.QuadraticBezierTo(points[i], mid, true, false);
                    }

                    // Final segment: straight line from last midpoint to last point
                    // (NOT a Bezier back through earlier points — that caused the closing line bug)
                    ctx.LineTo(points[points.Count - 1], true, false);
                }
            }

            geometry.Freeze(); // Freeze for performance

            // Build Path with same visual properties as source Polyline
            var path = new System.Windows.Shapes.Path
            {
                Data = geometry,
                Stroke = stroke.Stroke,
                StrokeThickness = stroke.StrokeThickness,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Opacity = stroke.Opacity,
                UseLayoutRounding = false
            };

            // Copy any Canvas positioning
            System.Windows.Controls.Canvas.SetLeft(path, System.Windows.Controls.Canvas.GetLeft(stroke));
            System.Windows.Controls.Canvas.SetTop(path, System.Windows.Controls.Canvas.GetTop(stroke));

            // High quality rendering
            RenderOptions.SetEdgeMode(path, EdgeMode.Unspecified);
            RenderOptions.SetBitmapScalingMode(path, BitmapScalingMode.HighQuality);

            System.Diagnostics.Debug.WriteLine($"✨ Converted Polyline → SmoothPath: {points.Count} points");

            return path;
        }
        
        /// <summary>
        /// Applies brush-specific settings to a stroke
        /// </summary>
        /// <param name="stroke">The stroke to apply settings to</param>
        /// <param name="brushType">The brush type</param>
        private void ApplyBrushSettings(Polyline stroke, string brushType)
        {
            // Map string brush type to enum (for compatibility with DrawingEngine)
            var mappedType = brushType.ToLowerInvariant() switch
            {
                "normal" => BrushType.Normal,
                "hoc" => BrushType.Hoc,
                "ai" => BrushType.AI,
                "simple" => BrushType.Simple,
                "marker" => BrushType.Marker,
                "maskpen" => BrushType.MaskPen,
                _ => BrushType.Normal
            };
            
            switch (mappedType)
            {
                case BrushType.Normal:
                    // Standard solid stroke
                    break;
                    
                case BrushType.Hoc:
                    // Educational brush - slightly thicker, smoother
                    stroke.StrokeThickness *= 1.2;
                    break;
                    
                case BrushType.AI:
                    // AI brush - smooth with slight opacity
                    stroke.Opacity = 0.9;
                    break;
                    
                case BrushType.Simple:
                    // Simple brush - basic settings
                    break;
                    
                case BrushType.Marker:
                    // Marker - thicker with slight transparency
                    stroke.StrokeThickness *= 1.5;
                    stroke.Opacity = 0.7;
                    break;
                    
                case BrushType.MaskPen:
                    // Mask pen - semi-transparent
                    stroke.Opacity = 0.5;
                    break;
            }
        }
        
        #endregion
    }
}
