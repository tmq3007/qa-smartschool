using System;
using System.Windows;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Represents the complete state of the Ruler Tool
    /// </summary>
    public class RulerState
    {
        #region Properties

        /// <summary>
        /// Current measurement unit
        /// </summary>
        public RulerUnit Unit { get; set; } = RulerUnit.CM;

        /// <summary>
        /// Current rotation angle in degrees (0-360)
        /// </summary>
        public double AngleDegrees { get; set; } = 0;

        /// <summary>
        /// Current length in pixels
        /// </summary>
        public double LengthPixels { get; set; } = 960;

        /// <summary>
        /// Current scale factor (zoom)
        /// </summary>
        public double Scale { get; set; } = 1.0;

        /// <summary>
        /// Whether the ruler is flipped
        /// </summary>
        public bool IsFlipped { get; set; } = false;

        /// <summary>
        /// Whether the ruler is locked (cannot move/rotate)
        /// </summary>
        public bool IsLocked { get; set; } = false;

        /// <summary>
        /// Current position (center point)
        /// </summary>
        public Point Position { get; set; }

        /// <summary>
        /// Current opacity (0.2 - 1.0)
        /// </summary>
        public double Opacity { get; set; } = 1.0;

        /// <summary>
        /// Whether angle snap is enabled
        /// </summary>
        public bool AngleSnapEnabled { get; set; } = true;

        /// <summary>
        /// Whether object snap is enabled
        /// </summary>
        public bool ObjectSnapEnabled { get; set; } = true;

        /// <summary>
        /// Whether grid snap is enabled
        /// </summary>
        public bool GridSnapEnabled { get; set; } = false;

        /// <summary>
        /// Calibration DPI (dots per inch)
        /// </summary>
        public double CalibrationDPI { get; set; } = 96; // Default Windows DPI

        /// <summary>
        /// Whether the ruler has been calibrated
        /// </summary>
        public bool IsCalibrated { get; set; } = false;

        /// <summary>
        /// Timestamp of last modification
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.Now;

        #endregion

        #region Methods

        /// <summary>
        /// Creates a deep copy of this state
        /// </summary>
        public RulerState Clone()
        {
            return new RulerState
            {
                Unit = this.Unit,
                AngleDegrees = this.AngleDegrees,
                LengthPixels = this.LengthPixels,
                Scale = this.Scale,
                IsFlipped = this.IsFlipped,
                IsLocked = this.IsLocked,
                Position = this.Position,
                Opacity = this.Opacity,
                AngleSnapEnabled = this.AngleSnapEnabled,
                ObjectSnapEnabled = this.ObjectSnapEnabled,
                GridSnapEnabled = this.GridSnapEnabled,
                CalibrationDPI = this.CalibrationDPI,
                IsCalibrated = this.IsCalibrated,
                LastModified = DateTime.Now
            };
        }

        /// <summary>
        /// String representation for debugging
        /// </summary>
        public override string ToString()
        {
            return $"Ruler: {AngleDegrees:F1}°, {LengthPixels}px, {Unit}, " +
                   $"Scale: {Scale:F2}, Locked: {IsLocked}";
        }

        #endregion
    }

    /// <summary>
    /// Measurement unit enumeration
    /// </summary>
    public enum RulerUnit
    {
        /// <summary>
        /// Millimeters
        /// </summary>
        MM,

        /// <summary>
        /// Centimeters (default)
        /// </summary>
        CM,

        /// <summary>
        /// Pixels (screen units)
        /// </summary>
        PX
    }
}
