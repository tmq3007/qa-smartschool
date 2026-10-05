using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartTouch.Services.Canvas;

namespace QASmartTouch.Managers
{
    /// <summary>
    /// Brush types available for drawing
    /// </summary>
    public enum BrushType
    {
        Normal,         // Standard brush
        Hoc,            // Educational brush
        AI,             // AI-enhanced brush
        Simple,         // Simple brush
        Marker,         // Marker pen
        MaskPen         // Mask pen
    }
    
    /// <summary>
    /// Handles all drawing operations (pen, brush, strokes)
    /// Manages drawing state, brush types, pen settings, and stroke creation
    /// REFACTORED: Now uses internal services (CanvasService, StrokeService, ShapeService)
    /// </summary>
    public class DrawingEngine
    {
        #region Constants
        
        /// <summary>
        /// Minimum pen size
        /// </summary>
        public const int MIN_PEN_SIZE = 1;
        
        /// <summary>
        /// Maximum pen size
        /// </summary>
        public const int MAX_PEN_SIZE = 16;
        
        /// <summary>
        /// Default pen size
        /// </summary>
        public const int DEFAULT_PEN_SIZE = 5;
        
        #endregion
        
        #region Fields
        
        private readonly Canvas _canvas;
        private readonly ICanvasService _canvasService;
        private readonly IStrokeService _strokeService;
        private readonly IShapeService _shapeService;
        
        private bool _isDrawing;
        private Point _lastPoint;
        private Polyline? _currentStroke;
        
        // Drawing settings
        private BrushType _brushType;
        private int _penSize;
        private Color _penColor;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets whether currently drawing
        /// </summary>
        public bool IsDrawing => _isDrawing;
        
        /// <summary>
        /// Gets the current brush type
        /// </summary>
        public BrushType BrushType => _brushType;
        
        /// <summary>
        /// Gets the current pen size
        /// </summary>
        public int PenSize => _penSize;
        
        /// <summary>
        /// Gets the current pen color
        /// </summary>
        public Color PenColor => _penColor;
        
        /// <summary>
        /// Gets the current stroke being drawn
        /// </summary>
        public Polyline? CurrentStroke => _currentStroke;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when a stroke is created
        /// </summary>
        public event EventHandler<StrokeCreatedEventArgs>? StrokeCreated;
        
        /// <summary>
        /// Fired when drawing settings are changed
        /// </summary>
        public event EventHandler<DrawingSettingsChangedEventArgs>? SettingsChanged;
        
        /// <summary>
        /// Fired when drawing starts
        /// </summary>
        public event EventHandler<DrawingEventArgs>? DrawingStarted;
        
        /// <summary>
        /// Fired when drawing ends
        /// </summary>
        public event EventHandler<DrawingEventArgs>? DrawingEnded;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of DrawingEngine
        /// </summary>
        /// <param name="canvas">The canvas to draw on</param>
        /// <param name="canvasService">Optional canvas service (auto-created if null)</param>
        /// <param name="strokeService">Optional stroke service (auto-created if null)</param>
        /// <param name="shapeService">Optional shape service (auto-created if null)</param>
        public DrawingEngine(
            Canvas canvas,
            ICanvasService? canvasService = null,
            IStrokeService? strokeService = null,
            IShapeService? shapeService = null)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            
            // Initialize services (use provided or create new)
            _canvasService = canvasService ?? new CanvasService(canvas);
            _strokeService = strokeService ?? new StrokeService();
            _shapeService = shapeService ?? new ShapeService();
            
            // Initialize with default settings
            _brushType = BrushType.Normal;
            _penSize = DEFAULT_PEN_SIZE;
            _penColor = Colors.Black;
            _isDrawing = false;
            
            System.Diagnostics.Debug.WriteLine("✅ DrawingEngine initialized with internal services");
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Starts a new drawing stroke
        /// </summary>
        /// <param name="startPoint">The starting point of the stroke</param>
        public void StartDrawing(Point startPoint)
        {
            if (_isDrawing)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Already drawing, ending previous stroke");
                EndDrawing();
            }
            
            _isDrawing = true;
            _lastPoint = startPoint;
            
            // Create new stroke using StrokeService
            _currentStroke = _strokeService.CreateStroke(_penColor, _penSize, _brushType.ToString());
            _strokeService.AddPointToStroke(_currentStroke, startPoint);
            // ✨ DOT SUPPORT: Thêm điểm vi mô (+0.01px) để WPF render chấm tròn tức thì
            _currentStroke.Points.Add(new Point(startPoint.X + 0.01, startPoint.Y));
            
            // Add to canvas using CanvasService
            _canvasService.AddElement(_currentStroke);
            
            System.Diagnostics.Debug.WriteLine($"🖊️ Drawing started at ({startPoint.X:F0}, {startPoint.Y:F0})");
            
            // Fire event
            DrawingStarted?.Invoke(this, new DrawingEventArgs(startPoint));
        }
        
        /// <summary>
        /// Continues the current drawing stroke
        /// </summary>
        /// <param name="currentPoint">The current point to add to the stroke</param>
        public void ContinueDrawing(Point currentPoint)
        {
            if (!_isDrawing || _currentStroke == null)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Not currently drawing");
                return;
            }
            
            // Nếu mới chỉ có 1 điểm gốc và 1 điểm vi mô preview, thay thế điểm vi mô bằng điểm di chuyển thực tế đầu tiên
            if (_currentStroke.Points.Count == 2 && Math.Abs(_currentStroke.Points[1].X - _currentStroke.Points[0].X - 0.01) < 0.001 && _currentStroke.Points[1].Y == _currentStroke.Points[0].Y)
            {
                _currentStroke.Points[1] = currentPoint;
            }
            else
            {
                // Add point to stroke using StrokeService
                _strokeService.AddPointToStroke(_currentStroke, currentPoint);
            }
            _lastPoint = currentPoint;
        }
        
        /// <summary>
        /// Ends the current drawing stroke and converts it to a smooth Bezier path
        /// </summary>
        /// <returns>The completed Polyline stroke, or null if no stroke was being drawn</returns>
        public Polyline? EndDrawing()
        {
            if (!_isDrawing || _currentStroke == null)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Not currently drawing");
                return null;
            }

            _isDrawing = false;
            var completedStroke = _currentStroke;
            _currentStroke = null;

            // ✨ DOT FALLBACK: Đảm bảo nếu nét chỉ có 1 điểm thì thêm điểm vi mô thứ hai
            if (completedStroke.Points.Count == 1)
            {
                Point pt = completedStroke.Points[0];
                completedStroke.Points.Add(new Point(pt.X + 0.01, pt.Y));
            }

            System.Diagnostics.Debug.WriteLine($"🖊️ Drawing ended: {completedStroke.Points.Count} points");

            // ✨ SMOOTH CONVERSION: Replace jagged Polyline with smooth QuadraticBezier Path
            // Runs instantly when user lifts finger — invisible transition, eliminates zigzag
            var smoothPath = _strokeService.ConvertToSmoothPath(completedStroke);
            if (smoothPath != null)
            {
                _canvasService.RemoveElement(completedStroke);
                _canvasService.AddElement(smoothPath);
                System.Diagnostics.Debug.WriteLine("✨ Polyline → SmoothPath conversion complete!");
            }

            // Fire events
            DrawingEnded?.Invoke(this, new DrawingEventArgs(_lastPoint));
            StrokeCreated?.Invoke(this, new StrokeCreatedEventArgs(completedStroke));

            return completedStroke;
        }
        
        /// <summary>
        /// Sets the brush type
        /// </summary>
        /// <param name="brushType">The brush type to use</param>
        public void SetBrushType(BrushType brushType)
        {
            if (_brushType == brushType)
                return;
            
            var oldBrushType = _brushType;
            _brushType = brushType;
            
            System.Diagnostics.Debug.WriteLine($"✅ Brush type changed: {oldBrushType} → {brushType}");
            
            // Fire event
            SettingsChanged?.Invoke(this, new DrawingSettingsChangedEventArgs(
                brushType, _penSize, _penColor));
        }
        
        /// <summary>
        /// Sets the pen size
        /// </summary>
        /// <param name="size">The pen size (1-16)</param>
        public void SetPenSize(int size)
        {
            // Clamp to valid range
            size = Math.Clamp(size, MIN_PEN_SIZE, MAX_PEN_SIZE);
            
            if (_penSize == size)
                return;
            
            var oldSize = _penSize;
            _penSize = size;
            
            System.Diagnostics.Debug.WriteLine($"✅ Pen size changed: {oldSize} → {size}");
            
            // Fire event
            SettingsChanged?.Invoke(this, new DrawingSettingsChangedEventArgs(
                _brushType, size, _penColor));
        }
        
        /// <summary>
        /// Sets the pen color
        /// </summary>
        /// <param name="color">The pen color</param>
        public void SetPenColor(Color color)
        {
            if (_penColor == color)
                return;
            
            var oldColor = _penColor;
            _penColor = color;
            
            System.Diagnostics.Debug.WriteLine($"✅ Pen color changed: {oldColor} → {color}");
            
            // Fire event
            SettingsChanged?.Invoke(this, new DrawingSettingsChangedEventArgs(
                _brushType, _penSize, color));
        }
        
        /// <summary>
        /// Sets all drawing settings at once
        /// </summary>
        /// <param name="brushType">The brush type</param>
        /// <param name="penSize">The pen size</param>
        /// <param name="penColor">The pen color</param>
        public void SetDrawingSettings(BrushType brushType, int penSize, Color penColor)
        {
            _brushType = brushType;
            _penSize = Math.Clamp(penSize, MIN_PEN_SIZE, MAX_PEN_SIZE);
            _penColor = penColor;
            
            System.Diagnostics.Debug.WriteLine($"✅ Drawing settings updated: {brushType}, Size={penSize}, Color={penColor}");
            
            // Fire event
            SettingsChanged?.Invoke(this, new DrawingSettingsChangedEventArgs(
                brushType, penSize, penColor));
        }
        
        /// <summary>
        /// Cancels the current drawing stroke
        /// </summary>
        public void CancelDrawing()
        {
            if (!_isDrawing || _currentStroke == null)
                return;
            
            // Remove incomplete stroke from canvas using CanvasService
            _canvasService.RemoveElement(_currentStroke);
            
            _isDrawing = false;
            _currentStroke = null;
            
            System.Diagnostics.Debug.WriteLine($"❌ Drawing cancelled");
        }
        
        #endregion
        
        #region Public Methods - Service Access (for advanced scenarios)
        
        /// <summary>
        /// Gets the canvas service for direct canvas operations
        /// </summary>
        public ICanvasService CanvasService => _canvasService;
        
        /// <summary>
        /// Gets the stroke service for direct stroke operations
        /// </summary>
        public IStrokeService StrokeService => _strokeService;
        
        /// <summary>
        /// Gets the shape service for direct shape operations
        /// </summary>
        public IShapeService ShapeService => _shapeService;
        
        #endregion
    }
    
    #region Event Args
    
    /// <summary>
    /// Event arguments for stroke created events
    /// </summary>
    public class StrokeCreatedEventArgs : EventArgs
    {
        public Polyline Stroke { get; }
        
        public StrokeCreatedEventArgs(Polyline stroke)
        {
            Stroke = stroke;
        }
    }
    
    /// <summary>
    /// Event arguments for drawing settings changed events
    /// </summary>
    public class DrawingSettingsChangedEventArgs : EventArgs
    {
        public BrushType BrushType { get; }
        public int PenSize { get; }
        public Color PenColor { get; }
        
        public DrawingSettingsChangedEventArgs(BrushType brushType, int penSize, Color penColor)
        {
            BrushType = brushType;
            PenSize = penSize;
            PenColor = penColor;
        }
    }
    
    /// <summary>
    /// Event arguments for drawing events
    /// </summary>
    public class DrawingEventArgs : EventArgs
    {
        public Point Point { get; }
        
        public DrawingEventArgs(Point point)
        {
            Point = point;
        }
    }
    
    #endregion
}
