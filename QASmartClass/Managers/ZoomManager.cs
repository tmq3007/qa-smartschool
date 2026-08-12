using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QASmartTouch.Managers
{
    /// <summary>
    /// Manages zoom and pan operations for the canvas
    /// Responsibilities:
    /// - Zoom in/out operations
    /// - Zoom to specific level
    /// - Zoom to area functionality
    /// - Pan (drag canvas) functionality
    /// - Zoom level management (0.5x - 4.0x)
    /// </summary>
    public class ZoomManager
    {
        #region Fields

        private readonly Canvas _canvas;
        private double _currentZoomLevel = 1.0;
        private TranslateTransform _panTransform;
        private Point _panStartPoint;
        private bool _isPanning = false;

        // Zoom limits
        private const double MIN_ZOOM = 0.5;
        private const double MAX_ZOOM = 4.0;
        private const double ZOOM_INCREMENT = 0.25;

        #endregion

        #region Properties

        /// <summary>
        /// Current zoom level (1.0 = 100%)
        /// </summary>
        public double CurrentZoomLevel
        {
            get => _currentZoomLevel;
            private set
            {
                if (_currentZoomLevel != value)
                {
                    _currentZoomLevel = value;
                    OnZoomChanged?.Invoke(this, new ZoomChangedEventArgs(_currentZoomLevel));
                }
            }
        }

        /// <summary>
        /// Minimum zoom level (0.5 = 50%)
        /// </summary>
        public double MinZoom => MIN_ZOOM;

        /// <summary>
        /// Maximum zoom level (4.0 = 400%)
        /// </summary>
        public double MaxZoom => MAX_ZOOM;

        /// <summary>
        /// Is panning mode active
        /// </summary>
        public bool IsPanning => _isPanning;

        #endregion

        #region Events

        /// <summary>
        /// Fired when zoom level changes
        /// </summary>
        public event EventHandler<ZoomChangedEventArgs> OnZoomChanged;

        /// <summary>
        /// Fired when pan position changes
        /// </summary>
        public event EventHandler<PanChangedEventArgs> OnPanChanged;

        /// <summary>
        /// Fired when zoom is reset to 100%
        /// </summary>
        public event EventHandler OnZoomReset;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize ZoomManager with canvas reference
        /// </summary>
        /// <param name="canvas">Canvas to manage zoom/pan for</param>
        public ZoomManager(Canvas canvas)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            System.Diagnostics.Debug.WriteLine("✅ ZoomManager initialized");
        }

        #endregion

        #region Public Methods - Zoom Operations

        /// <summary>
        /// Zoom in by increment (default 0.25)
        /// </summary>
        public void ZoomIn(double increment = ZOOM_INCREMENT)
        {
            double newZoomLevel = _currentZoomLevel + increment;
            if (newZoomLevel > MAX_ZOOM)
                newZoomLevel = MAX_ZOOM;

            ApplyZoom(newZoomLevel);
            System.Diagnostics.Debug.WriteLine($"➕ Zoom In: {_currentZoomLevel}x → {newZoomLevel}x");
        }

        /// <summary>
        /// Zoom out by increment (default 0.25)
        /// </summary>
        public void ZoomOut(double increment = ZOOM_INCREMENT)
        {
            double newZoomLevel = _currentZoomLevel - increment;
            if (newZoomLevel < MIN_ZOOM)
                newZoomLevel = MIN_ZOOM;

            ApplyZoom(newZoomLevel);
            System.Diagnostics.Debug.WriteLine($"➖ Zoom Out: {_currentZoomLevel}x → {newZoomLevel}x");
        }

        /// <summary>
        /// Zoom to specific level
        /// </summary>
        /// <param name="level">Zoom level (0.5 - 4.0)</param>
        public void ZoomToLevel(double level)
        {
            if (level < MIN_ZOOM || level > MAX_ZOOM)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Zoom level {level} out of range ({MIN_ZOOM} - {MAX_ZOOM})");
                return;
            }

            ApplyZoom(level);
            System.Diagnostics.Debug.WriteLine($"📌 Zoom to level: {level}x");
        }

        /// <summary>
        /// Reset zoom to 100% (1.0x)
        /// </summary>
        public void ResetZoom()
        {
            ApplyZoom(1.0);
            System.Diagnostics.Debug.WriteLine("🔄 Zoom reset to 100%");
            OnZoomReset?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Get current zoom percentage (e.g., 100, 150, 200)
        /// </summary>
        public int GetZoomPercentage()
        {
            return (int)(_currentZoomLevel * 100);
        }

        #endregion

        #region Public Methods - Pan Operations

        /// <summary>
        /// Enable pan mode (allows dragging canvas)
        /// </summary>
        public void EnablePan()
        {
            if (_currentZoomLevel > 1.0)
            {
                _canvas.Cursor = Cursors.Hand;
                System.Diagnostics.Debug.WriteLine("🖐️ Pan mode enabled");
            }
        }

        /// <summary>
        /// Disable pan mode
        /// </summary>
        public void DisablePan()
        {
            _isPanning = false;
            _canvas.Cursor = Cursors.Arrow;
            System.Diagnostics.Debug.WriteLine("🖐️ Pan mode disabled");
        }

        /// <summary>
        /// Start panning operation
        /// </summary>
        /// <param name="startPoint">Starting point in window coordinates</param>
        public void StartPan(Point startPoint)
        {
            if (_currentZoomLevel <= 1.0 || _panTransform == null)
                return;

            _isPanning = true;
            _panStartPoint = startPoint;
            _canvas.Cursor = Cursors.SizeAll;
            _canvas.CaptureMouse();
            System.Diagnostics.Debug.WriteLine($"🖐️ Pan started at ({startPoint.X:F0}, {startPoint.Y:F0})");
        }

        /// <summary>
        /// Continue panning operation
        /// </summary>
        /// <param name="currentPoint">Current point in window coordinates</param>
        public void ContinuePan(Point currentPoint)
        {
            if (!_isPanning || _panTransform == null)
                return;

            double deltaX = currentPoint.X - _panStartPoint.X;
            double deltaY = currentPoint.Y - _panStartPoint.Y;

            _panTransform.X += deltaX;
            _panTransform.Y += deltaY;

            _panStartPoint = currentPoint;

            OnPanChanged?.Invoke(this, new PanChangedEventArgs(_panTransform.X, _panTransform.Y));
        }

        /// <summary>
        /// End panning operation
        /// </summary>
        public void EndPan()
        {
            if (!_isPanning)
                return;

            _isPanning = false;
            _canvas.Cursor = Cursors.Hand;
            _canvas.ReleaseMouseCapture();
            System.Diagnostics.Debug.WriteLine("🖐️ Pan ended");
        }

        /// <summary>
        /// Pan by specific offset
        /// </summary>
        /// <param name="deltaX">Horizontal offset</param>
        /// <param name="deltaY">Vertical offset</param>
        public void Pan(double deltaX, double deltaY)
        {
            if (_panTransform == null)
                return;

            _panTransform.X += deltaX;
            _panTransform.Y += deltaY;

            OnPanChanged?.Invoke(this, new PanChangedEventArgs(_panTransform.X, _panTransform.Y));
            System.Diagnostics.Debug.WriteLine($"🖐️ Pan by ({deltaX:F0}, {deltaY:F0})");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Apply zoom transformation to canvas
        /// </summary>
        private void ApplyZoom(double newZoomLevel)
        {
            double oldZoomLevel = _currentZoomLevel;
            CurrentZoomLevel = newZoomLevel;

            // If zoom is 1.0, reset all transforms
            if (Math.Abs(newZoomLevel - 1.0) < 0.001)
            {
                _canvas.LayoutTransform = null;
                _canvas.RenderTransform = null;
                _panTransform = null;

                // Reset canvas size to auto
                _canvas.Width = double.NaN;
                _canvas.Height = double.NaN;

                _canvas.Cursor = Cursors.Arrow;
                System.Diagnostics.Debug.WriteLine("✅ Canvas RESET to original view (1x)");
            }
            else
            {
                // Get current canvas actual size
                double canvasWidth = _canvas.ActualWidth;
                double canvasHeight = _canvas.ActualHeight;

                // Set canvas size explicitly for proper zoom behavior
                if (double.IsNaN(_canvas.Width))
                {
                    _canvas.Width = canvasWidth;
                    _canvas.Height = canvasHeight;
                }

                // Create transform group with scale and translate
                var transformGroup = new TransformGroup();

                // Scale transform (zoom)
                var scaleTransform = new ScaleTransform(newZoomLevel, newZoomLevel);
                transformGroup.Children.Add(scaleTransform);

                // Translate transform (pan) - initialize or preserve existing
                if (_panTransform == null)
                {
                    _panTransform = new TranslateTransform(0, 0);
                }
                transformGroup.Children.Add(_panTransform);

                // Apply combined transform to RenderTransform
                _canvas.RenderTransform = transformGroup;

                // Update RenderTransformOrigin to center (0.5, 0.5)
                _canvas.RenderTransformOrigin = new Point(0.5, 0.5);

                // Change cursor to indicate panning is available
                _canvas.Cursor = Cursors.Hand;

                System.Diagnostics.Debug.WriteLine($"✅ Canvas zoomed to {newZoomLevel}x");
            }
        }

        #endregion

        #region Public Methods - Utility

        /// <summary>
        /// Check if zoom level is at default (100%)
        /// </summary>
        public bool IsDefaultZoom()
        {
            return Math.Abs(_currentZoomLevel - 1.0) < 0.001;
        }

        /// <summary>
        /// Check if can zoom in further
        /// </summary>
        public bool CanZoomIn()
        {
            return _currentZoomLevel < MAX_ZOOM;
        }

        /// <summary>
        /// Check if can zoom out further
        /// </summary>
        public bool CanZoomOut()
        {
            return _currentZoomLevel > MIN_ZOOM;
        }

        #endregion
    }

    #region Event Args

    /// <summary>
    /// Event args for zoom changed event
    /// </summary>
    public class ZoomChangedEventArgs : EventArgs
    {
        public double ZoomLevel { get; }
        public int ZoomPercentage => (int)(ZoomLevel * 100);

        public ZoomChangedEventArgs(double zoomLevel)
        {
            ZoomLevel = zoomLevel;
        }
    }

    /// <summary>
    /// Event args for pan changed event
    /// </summary>
    public class PanChangedEventArgs : EventArgs
    {
        public double OffsetX { get; }
        public double OffsetY { get; }

        public PanChangedEventArgs(double offsetX, double offsetY)
        {
            OffsetX = offsetX;
            OffsetY = offsetY;
        }
    }

    #endregion
}
