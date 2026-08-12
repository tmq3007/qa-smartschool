using System;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Represents a measured segment between two markers
    /// </summary>
    public class RulerSegment
    {
        #region Properties

        /// <summary>
        /// Unique identifier
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Start marker (A)
        /// </summary>
        public RulerMarker MarkerA { get; set; }

        /// <summary>
        /// End marker (B)
        /// </summary>
        public RulerMarker MarkerB { get; set; }

        /// <summary>
        /// Length in real-world units (cm/mm)
        /// </summary>
        public double LengthReal { get; set; }

        /// <summary>
        /// Length in pixels
        /// </summary>
        public double LengthPixels { get; set; }

        /// <summary>
        /// Angle in degrees
        /// </summary>
        public double AngleDegrees { get; set; }

        /// <summary>
        /// Measurement unit used
        /// </summary>
        public RulerUnit Unit { get; set; }

        /// <summary>
        /// Timestamp when segment was created
        /// </summary>
        public DateTime Created { get; set; } = DateTime.Now;

        /// <summary>
        /// The Line object on canvas (if created)
        /// </summary>
        public Line? CanvasLine { get; set; }

        /// <summary>
        /// Whether this segment is visible on canvas
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Custom metadata
        /// </summary>
        public string? Metadata { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public RulerSegment()
        {
            MarkerA = new RulerMarker { Id = "A" };
            MarkerB = new RulerMarker { Id = "B" };
        }

        /// <summary>
        /// Constructor with markers
        /// </summary>
        public RulerSegment(RulerMarker markerA, RulerMarker markerB)
        {
            MarkerA = markerA;
            MarkerB = markerB;
            CalculateProperties();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Calculate length and angle from markers
        /// </summary>
        public void CalculateProperties()
        {
            if (MarkerA == null || MarkerB == null) return;

            // Calculate pixel length
            double dx = MarkerB.CanvasPosition.X - MarkerA.CanvasPosition.X;
            double dy = MarkerB.CanvasPosition.Y - MarkerA.CanvasPosition.Y;
            LengthPixels = Math.Sqrt(dx * dx + dy * dy);

            // Calculate angle
            AngleDegrees = Math.Atan2(dy, dx) * 180 / Math.PI;
            if (AngleDegrees < 0) AngleDegrees += 360;

            // Calculate real length (from ruler positions)
            LengthReal = Math.Abs(MarkerB.RulerPosition - MarkerA.RulerPosition);
            Unit = MarkerA.Unit;
        }

        /// <summary>
        /// Create a Line object for canvas
        /// </summary>
        public Line CreateCanvasLine(Brush stroke, double thickness = 2.0)
        {
            CanvasLine = new Line
            {
                X1 = MarkerA.CanvasPosition.X,
                Y1 = MarkerA.CanvasPosition.Y,
                X2 = MarkerB.CanvasPosition.X,
                Y2 = MarkerB.CanvasPosition.Y,
                Stroke = stroke,
                StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Tag = this.Id // Store segment ID in Tag
            };

            return CanvasLine;
        }

        /// <summary>
        /// String representation
        /// </summary>
        public override string ToString()
        {
            return $"Segment {Id:N}: {LengthReal:F2} {Unit} ({LengthPixels:F0}px) at {AngleDegrees:F1}°";
        }

        #endregion
    }
}
