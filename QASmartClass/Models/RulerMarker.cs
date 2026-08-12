using System;
using System.Windows;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Represents a marker point on the ruler (A or B)
    /// </summary>
    public class RulerMarker
    {
        #region Properties

        /// <summary>
        /// Marker identifier ("A" or "B")
        /// </summary>
        public string Id { get; set; } = "A";

        /// <summary>
        /// Position on canvas (absolute coordinates)
        /// </summary>
        public Point CanvasPosition { get; set; }

        /// <summary>
        /// Position on ruler in current unit (e.g., 5.5 cm)
        /// </summary>
        public double RulerPosition { get; set; }

        /// <summary>
        /// Current measurement unit
        /// </summary>
        public RulerUnit Unit { get; set; }

        /// <summary>
        /// Timestamp when marker was placed
        /// </summary>
        public DateTime Created { get; set; } = DateTime.Now;

        /// <summary>
        /// Whether this marker is visible
        /// </summary>
        public bool IsVisible { get; set; } = true;

        #endregion

        #region Methods

        /// <summary>
        /// String representation
        /// </summary>
        public override string ToString()
        {
            return $"Marker {Id}: {RulerPosition:F2} {Unit} at ({CanvasPosition.X:F0}, {CanvasPosition.Y:F0})";
        }

        #endregion
    }
}
