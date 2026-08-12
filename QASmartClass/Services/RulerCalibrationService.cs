using System;
using System.Windows;
using System.Windows.Media;
using QASmartTouch.Models;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Service for ruler calibration and unit conversion
    /// </summary>
    public class RulerCalibrationService
    {
        #region Constants

        // Standard DPI values
        private const double STANDARD_DPI = 96.0;  // Windows default
        private const double MM_PER_INCH = 25.4;
        private const double CM_PER_INCH = 2.54;

        #endregion

        #region Properties

        /// <summary>
        /// Current calibrated DPI
        /// </summary>
        public double CurrentDPI { get; private set; } = STANDARD_DPI;

        /// <summary>
        /// Scale factor from calibration
        /// </summary>
        public double ScaleFactor { get; private set; } = 1.0;

        /// <summary>
        /// Whether calibration has been performed
        /// </summary>
        public bool IsCalibrated { get; private set; } = false;

        #endregion

        #region Calibration

        /// <summary>
        /// Calibrate the ruler using a known real-world length
        /// </summary>
        /// <param name="realLengthCm">Known length in centimeters (e.g., 21 for A4 width)</param>
        /// <param name="measuredPixels">Measured length in pixels on screen</param>
        public void Calibrate(double realLengthCm, double measuredPixels)
        {
            if (realLengthCm <= 0 || measuredPixels <= 0)
            {
                throw new ArgumentException("Lengths must be positive");
            }

            // Calculate DPI from calibration
            // DPI = (pixels / inches)
            // inches = cm / 2.54
            double inches = realLengthCm / CM_PER_INCH;
            CurrentDPI = measuredPixels / inches;

            // Calculate scale factor relative to standard DPI
            ScaleFactor = CurrentDPI / STANDARD_DPI;

            IsCalibrated = true;
        }

        /// <summary>
        /// Reset calibration to system default
        /// </summary>
        public void ResetCalibration()
        {
            CurrentDPI = GetSystemDPI();
            ScaleFactor = CurrentDPI / STANDARD_DPI;
            IsCalibrated = false;
        }

        /// <summary>
        /// Get system DPI
        /// </summary>
        private double GetSystemDPI()
        {
            try
            {
                // Get DPI from system
                var dpiScale = VisualTreeHelper.GetDpi(Application.Current.MainWindow);
                return dpiScale.PixelsPerInchX;
            }
            catch
            {
                return STANDARD_DPI;
            }
        }

        #endregion

        #region Unit Conversion

        /// <summary>
        /// Convert pixels to specified unit
        /// </summary>
        /// <param name="pixels">Length in pixels</param>
        /// <param name="unit">Target unit</param>
        /// <returns>Length in target unit</returns>
        public double PixelsToUnit(double pixels, RulerUnit unit)
        {
            switch (unit)
            {
                case RulerUnit.PX:
                    return pixels;

                case RulerUnit.MM:
                    // pixels → inches → mm
                    double inches_mm = pixels / CurrentDPI;
                    return inches_mm * MM_PER_INCH;

                case RulerUnit.CM:
                    // pixels → inches → cm
                    double inches_cm = pixels / CurrentDPI;
                    return inches_cm * CM_PER_INCH;

                default:
                    return pixels;
            }
        }

        /// <summary>
        /// Convert from specified unit to pixels
        /// </summary>
        /// <param name="value">Length in source unit</param>
        /// <param name="unit">Source unit</param>
        /// <returns>Length in pixels</returns>
        public double UnitToPixels(double value, RulerUnit unit)
        {
            switch (unit)
            {
                case RulerUnit.PX:
                    return value;

                case RulerUnit.MM:
                    // mm → inches → pixels
                    double inches_mm = value / MM_PER_INCH;
                    return inches_mm * CurrentDPI;

                case RulerUnit.CM:
                    // cm → inches → pixels
                    double inches_cm = value / CM_PER_INCH;
                    return inches_cm * CurrentDPI;

                default:
                    return value;
            }
        }

        /// <summary>
        /// Convert between units
        /// </summary>
        /// <param name="value">Length in source unit</param>
        /// <param name="fromUnit">Source unit</param>
        /// <param name="toUnit">Target unit</param>
        /// <returns>Length in target unit</returns>
        public double ConvertUnit(double value, RulerUnit fromUnit, RulerUnit toUnit)
        {
            if (fromUnit == toUnit) return value;

            // Convert to pixels first, then to target unit
            double pixels = UnitToPixels(value, fromUnit);
            return PixelsToUnit(pixels, toUnit);
        }

        #endregion

        #region Ruler Marks Generation

        /// <summary>
        /// Calculate pixels per unit for ruler marks
        /// </summary>
        /// <param name="unit">Measurement unit</param>
        /// <returns>Pixels per unit</returns>
        public double GetPixelsPerUnit(RulerUnit unit)
        {
            switch (unit)
            {
                case RulerUnit.MM:
                    return CurrentDPI / MM_PER_INCH;

                case RulerUnit.CM:
                    return CurrentDPI / CM_PER_INCH;

                case RulerUnit.PX:
                    return 1.0;

                default:
                    return CurrentDPI / CM_PER_INCH;
            }
        }

        /// <summary>
        /// Get appropriate mark interval for unit
        /// </summary>
        /// <param name="unit">Measurement unit</param>
        /// <returns>Interval between major marks</returns>
        public double GetMajorMarkInterval(RulerUnit unit)
        {
            switch (unit)
            {
                case RulerUnit.MM:
                    return 10; // 10mm = 1cm

                case RulerUnit.CM:
                    return 1; // 1cm

                case RulerUnit.PX:
                    return 100; // 100px

                default:
                    return 1;
            }
        }

        /// <summary>
        /// Get appropriate minor mark interval
        /// </summary>
        /// <param name="unit">Measurement unit</param>
        /// <returns>Interval between minor marks</returns>
        public double GetMinorMarkInterval(RulerUnit unit)
        {
            switch (unit)
            {
                case RulerUnit.MM:
                    return 1; // 1mm

                case RulerUnit.CM:
                    return 0.1; // 1mm

                case RulerUnit.PX:
                    return 10; // 10px

                default:
                    return 0.1;
            }
        }

        #endregion

        #region Formatting

        /// <summary>
        /// Format a value with unit suffix
        /// </summary>
        /// <param name="value">Numeric value</param>
        /// <param name="unit">Unit</param>
        /// <param name="decimals">Number of decimal places</param>
        /// <returns>Formatted string</returns>
        public string FormatWithUnit(double value, RulerUnit unit, int decimals = 2)
        {
            string unitStr = unit switch
            {
                RulerUnit.MM => "mm",
                RulerUnit.CM => "cm",
                RulerUnit.PX => "px",
                _ => ""
            };

            return $"{value.ToString($"F{decimals}")} {unitStr}";
        }

        /// <summary>
        /// Get unit display name
        /// </summary>
        /// <param name="unit">Unit</param>
        /// <returns>Display name</returns>
        public string GetUnitDisplayName(RulerUnit unit)
        {
            return unit switch
            {
                RulerUnit.MM => "Millimeters",
                RulerUnit.CM => "Centimeters",
                RulerUnit.PX => "Pixels",
                _ => "Unknown"
            };
        }

        #endregion

        #region Calibration Info

        /// <summary>
        /// Get calibration information string
        /// </summary>
        /// <returns>Calibration info</returns>
        public string GetCalibrationInfo()
        {
            if (!IsCalibrated)
            {
                return $"System DPI: {CurrentDPI:F1} (Not calibrated)";
            }

            return $"Calibrated DPI: {CurrentDPI:F1} (Scale: {ScaleFactor:F3}x)";
        }

        #endregion
    }
}
