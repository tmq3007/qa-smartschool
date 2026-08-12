using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace QASmartTouch.Helpers
{
    /// <summary>
    /// ✨ STROKE OPTIMIZER - Optimizes drawing performance for interactive boards
    /// Solves jitter/shake issues when writing on HDMI-connected touch displays
    /// </summary>
    public class StrokeOptimizer
    {
        #region Configuration

        /// <summary>
        /// Minimum distance (in pixels) between points to add a new point
        /// Higher value = smoother but less detailed
        /// Recommended: 2-5 for interactive boards, 1-2 for tablets
        /// </summary>
        public double MinPointDistance { get; set; } = 3.0;

        /// <summary>
        /// Enable Ramer-Douglas-Peucker algorithm for point decimation
        /// Reduces number of points while preserving shape
        /// </summary>
        public bool EnableDecimation { get; set; } = true;

        /// <summary>
        /// Epsilon value for RDP algorithm (tolerance)
        /// Higher value = more aggressive decimation
        /// Recommended: 0.5-2.0
        /// </summary>
        public double DecimationEpsilon { get; set; } = 1.0;

        /// <summary>
        /// Enable Bezier smoothing for final stroke
        /// Makes strokes look more natural
        /// </summary>
        public bool EnableSmoothing { get; set; } = true;

        /// <summary>
        /// Smoothing factor (0.0 - 1.0)
        /// 0.0 = no smoothing, 1.0 = maximum smoothing
        /// </summary>
        public double SmoothingFactor { get; set; } = 0.3;

        #endregion

        #region Point Filtering

        /// <summary>
        /// Check if a new point should be added based on distance from last point
        /// Prevents adding too many points when hand is shaking
        /// </summary>
        public bool ShouldAddPoint(Point newPoint, Point lastPoint)
        {
            double distance = CalculateDistance(newPoint, lastPoint);
            return distance >= MinPointDistance;
        }

        /// <summary>
        /// Calculate Euclidean distance between two points
        /// </summary>
        private double CalculateDistance(Point p1, Point p2)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        #endregion

        #region Point Decimation (Ramer-Douglas-Peucker Algorithm)

        /// <summary>
        /// Reduce number of points in a stroke while preserving shape
        /// Uses Ramer-Douglas-Peucker algorithm
        /// </summary>
        public List<Point> DecimatePoints(List<Point> points)
        {
            if (!EnableDecimation || points.Count <= 2)
                return points;

            return RamerDouglasPeucker(points, DecimationEpsilon);
        }

        /// <summary>
        /// Ramer-Douglas-Peucker algorithm implementation
        /// Recursively removes points that don't significantly affect the shape
        /// </summary>
        private List<Point> RamerDouglasPeucker(List<Point> points, double epsilon)
        {
            if (points.Count < 3)
                return points;

            // Find the point with maximum distance from line segment
            double maxDistance = 0;
            int maxIndex = 0;

            Point start = points[0];
            Point end = points[points.Count - 1];

            for (int i = 1; i < points.Count - 1; i++)
            {
                double distance = PerpendicularDistance(points[i], start, end);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    maxIndex = i;
                }
            }

            // If max distance is greater than epsilon, recursively simplify
            if (maxDistance > epsilon)
            {
                // Recursive call
                var leftSegment = RamerDouglasPeucker(points.Take(maxIndex + 1).ToList(), epsilon);
                var rightSegment = RamerDouglasPeucker(points.Skip(maxIndex).ToList(), epsilon);

                // Combine results (remove duplicate middle point)
                var result = leftSegment.Take(leftSegment.Count - 1).ToList();
                result.AddRange(rightSegment);
                return result;
            }
            else
            {
                // All points between start and end can be removed
                return new List<Point> { start, end };
            }
        }

        /// <summary>
        /// Calculate perpendicular distance from point to line segment
        /// </summary>
        private double PerpendicularDistance(Point point, Point lineStart, Point lineEnd)
        {
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;

            // Normalize
            double mag = Math.Sqrt(dx * dx + dy * dy);
            if (mag > 0.0)
            {
                dx /= mag;
                dy /= mag;
            }

            double pvx = point.X - lineStart.X;
            double pvy = point.Y - lineStart.Y;

            // Get dot product (project point onto line)
            double pvdot = dx * pvx + dy * pvy;

            // Scale line direction vector
            double dsx = pvdot * dx;
            double dsy = pvdot * dy;

            // Get vector from point to line
            double ax = pvx - dsx;
            double ay = pvy - dsy;

            return Math.Sqrt(ax * ax + ay * ay);
        }

        #endregion

        #region Smoothing (Bezier-based)

        /// <summary>
        /// Smooth a stroke using Catmull-Rom spline interpolation
        /// Produces naturally smooth curves through all input points
        /// Much better than simple weighted average for large touch displays
        /// </summary>
        public List<Point> SmoothPoints(List<Point> points)
        {
            if (!EnableSmoothing || points.Count < 3)
                return points;

            // Number of interpolated sub-points between each pair
            // Higher = smoother curves but more points (2-4 is ideal)
            int subdivisions = 3;

            List<Point> smoothed = new List<Point>();
            smoothed.Add(points[0]); // Keep first point

            for (int i = 0; i < points.Count - 1; i++)
            {
                // Catmull-Rom requires 4 control points: p0, p1, p2, p3
                Point p0 = points[Math.Max(0, i - 1)];
                Point p1 = points[i];
                Point p2 = points[i + 1];
                Point p3 = points[Math.Min(points.Count - 1, i + 2)];

                // Generate intermediate points between p1 and p2
                for (int j = 1; j <= subdivisions; j++)
                {
                    double t = (double)j / (subdivisions + 1);
                    Point interpolated = CatmullRomInterpolate(p0, p1, p2, p3, t);

                    // Apply additional weighted smoothing with SmoothingFactor
                    if (SmoothingFactor > 0 && smoothed.Count > 0)
                    {
                        var last = smoothed[smoothed.Count - 1];
                        interpolated = new Point(
                            interpolated.X * (1 - SmoothingFactor * 0.3) + last.X * (SmoothingFactor * 0.3),
                            interpolated.Y * (1 - SmoothingFactor * 0.3) + last.Y * (SmoothingFactor * 0.3)
                        );
                    }

                    smoothed.Add(interpolated);
                }

                smoothed.Add(p2); // Keep original point p2
            }

            smoothed.Add(points[points.Count - 1]); // Keep last point

            return smoothed;
        }

        /// <summary>
        /// Catmull-Rom spline interpolation between p1 and p2
        /// t = 0 = p1, t = 1 = p2
        /// Uses p0 and p3 as tension control points
        /// </summary>
        private Point CatmullRomInterpolate(Point p0, Point p1, Point p2, Point p3, double t)
        {
            double t2 = t * t;
            double t3 = t2 * t;

            // Catmull-Rom matrix coefficients (alpha = 0.5)
            double x = 0.5 * (
                (2 * p1.X) +
                (-p0.X + p2.X) * t +
                (2 * p0.X - 5 * p1.X + 4 * p2.X - p3.X) * t2 +
                (-p0.X + 3 * p1.X - 3 * p2.X + p3.X) * t3
            );

            double y = 0.5 * (
                (2 * p1.Y) +
                (-p0.Y + p2.Y) * t +
                (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2 +
                (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * t3
            );

            return new Point(x, y);
        }

        #endregion

        #region Preset Configurations

        /// <summary>
        /// Apply preset configuration for 86" interactive board (touch display)
        /// Optimized for: smooth curves, reduced jitter, natural handwriting feel
        /// Uses Catmull-Rom spline smoothing with moderate parameters
        /// </summary>
        public void ApplyInteractiveBoardPreset()
        {
            MinPointDistance = 2.0;      // Fine-grained capture for natural feel
            EnableDecimation = false;    // Disable decimation — let Catmull-Rom handle smoothing
            DecimationEpsilon = 1.0;     // Kept for compatibility
            EnableSmoothing = true;
            SmoothingFactor = 0.35;      // Moderate: smooth without losing stroke shape
        }

        /// <summary>
        /// Apply preset configuration for tablet/stylus input
        /// Balanced between precision and performance
        /// </summary>
        public void ApplyTabletPreset()
        {
            MinPointDistance = 2.0;
            EnableDecimation = true;
            DecimationEpsilon = 0.8;
            EnableSmoothing = true;
            SmoothingFactor = 0.2;
        }

        /// <summary>
        /// Apply preset configuration for high-precision drawing
        /// Minimal optimization, maximum detail
        /// </summary>
        public void ApplyPrecisionPreset()
        {
            MinPointDistance = 1.0;
            EnableDecimation = false;
            DecimationEpsilon = 0.5;
            EnableSmoothing = false;
            SmoothingFactor = 0.1;
        }

        /// <summary>
        /// 🔥 AGGRESSIVE ANTI-JITTER PRESET
        /// For severe jitter cases on HDMI-connected interactive boards
        /// Use this if InteractiveBoardPreset doesn't solve the problem
        /// Trade-off: Less detail but much smoother
        /// </summary>
        public void ApplyAggressiveAntiJitterPreset()
        {
            MinPointDistance = 7.0;      // Very aggressive filtering (almost 2x normal)
            EnableDecimation = true;
            DecimationEpsilon = 2.5;     // Aggressive decimation
            EnableSmoothing = true;
            SmoothingFactor = 0.6;       // Strong smoothing
        }

        #endregion

        #region Diagnostics

        /// <summary>
        /// Get optimization statistics for debugging
        /// </summary>
        public string GetOptimizationStats(int originalPoints, int optimizedPoints)
        {
            double reduction = originalPoints > 0 
                ? (1.0 - (double)optimizedPoints / originalPoints) * 100.0 
                : 0.0;

            return $"Points: {originalPoints} → {optimizedPoints} (reduced {reduction:F1}%)";
        }

        #endregion
    }
}
