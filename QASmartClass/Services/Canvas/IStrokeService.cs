using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartTouch.Services.Canvas
{
    /// <summary>
    /// Interface for stroke management operations
    /// Handles stroke creation, modification, and properties
    /// </summary>
    public interface IStrokeService
    {
        /// <summary>
        /// Creates a new stroke with the specified properties
        /// </summary>
        /// <param name="color">The stroke color</param>
        /// <param name="thickness">The stroke thickness</param>
        /// <param name="brushType">The brush type (optional)</param>
        /// <returns>A new Polyline stroke</returns>
        Polyline CreateStroke(Color color, double thickness, string? brushType = null);
        
        /// <summary>
        /// Adds a point to an existing stroke
        /// </summary>
        /// <param name="stroke">The stroke to add point to</param>
        /// <param name="point">The point to add</param>
        void AddPointToStroke(Polyline stroke, Point point);
        
        /// <summary>
        /// Smooths a stroke using spline interpolation
        /// </summary>
        /// <param name="stroke">The stroke to smooth</param>
        /// <returns>A smoothed version of the stroke</returns>
        Polyline SmoothStroke(Polyline stroke);
        
        /// <summary>
        /// Changes the color of an existing stroke
        /// </summary>
        /// <param name="stroke">The stroke to modify</param>
        /// <param name="color">The new color</param>
        void SetStrokeColor(Polyline stroke, Color color);
        
        /// <summary>
        /// Changes the thickness of an existing stroke
        /// </summary>
        /// <param name="stroke">The stroke to modify</param>
        /// <param name="thickness">The new thickness</param>
        void SetStrokeThickness(Polyline stroke, double thickness);
        
        /// <summary>
        /// Gets the total length of a stroke
        /// </summary>
        /// <param name="stroke">The stroke to measure</param>
        /// <returns>The length in pixels</returns>
        double GetStrokeLength(Polyline stroke);
        
        /// <summary>
        /// Gets the bounding box of a stroke
        /// </summary>
        /// <param name="stroke">The stroke to get bounds for</param>
        /// <returns>The bounding rectangle</returns>
        Rect GetStrokeBounds(Polyline stroke);

        /// <summary>
        /// Converts a Polyline stroke to a smooth Path using QuadraticBezier curves
        /// Call this AFTER stroke is complete — replaces jagged polyline with smooth curves
        /// </summary>
        /// <param name="stroke">The source Polyline stroke</param>
        /// <returns>A Path with smooth QuadraticBezier geometry, or null if fewer than 2 points</returns>
        System.Windows.Shapes.Path? ConvertToSmoothPath(Polyline stroke);
    }
}
