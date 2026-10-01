using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartTouch.Managers
{
    /// <summary>
    /// Touch interaction mode
    /// </summary>
    public enum TouchInteractionMode
    {
        SingleFinger,  // 1 finger = Draw, 2+ fingers = Pan/Zoom
        MultiFinger    // All fingers = Draw (up to 30 touch points)
    }

    /// <summary>
    /// Manages touch interactions for drawing on canvas
    /// </summary>
    public class TouchManager
    {
        private TouchInteractionMode _currentMode;
        private Dictionary<int, Polyline> _activeStrokes;
        private Dictionary<int, Color> _touchColors;
        private Color _defaultColor;
        private double _defaultThickness;
        private string _defaultBrushType;

        // Pan/Zoom state (for Single Finger mode)
        private Point _lastPanPoint;
        private double _lastPinchDistance;
        private bool _isPanning;
        private bool _isPinching;

        public TouchInteractionMode CurrentMode
        {
            get => _currentMode;
            set
            {
                _currentMode = value;
                System.Diagnostics.Debug.WriteLine($"🔄 Touch mode changed to: {value}");
            }
        }

        public TouchManager()
        {
            _currentMode = TouchInteractionMode.MultiFinger; // Default to multi-finger
            _activeStrokes = new Dictionary<int, Polyline>();
            _touchColors = new Dictionary<int, Color>();
            _defaultColor = Colors.White;
            _defaultThickness = 5;
            _defaultBrushType = "Normal";
        }

        /// <summary>
        /// Set default drawing properties
        /// </summary>
        public void SetDefaultDrawingProperties(Color color, double thickness, string brushType)
        {
            _defaultColor = color;
            _defaultThickness = thickness;
            _defaultBrushType = brushType;
        }

        /// <summary>
        /// Get active touch count
        /// </summary>
        public int GetActiveTouchCount()
        {
            return _activeStrokes.Count;
        }

        /// <summary>
        /// Check if a touch is currently active
        /// </summary>
        public bool IsTouchActive(int touchId)
        {
            return _activeStrokes.ContainsKey(touchId);
        }

        /// <summary>
        /// Get stroke for a specific touch
        /// </summary>
        public Polyline? GetStroke(int touchId)
        {
            return _activeStrokes.ContainsKey(touchId) ? _activeStrokes[touchId] : null;
        }

        /// <summary>
        /// Create a new stroke for a touch point
        /// </summary>
        public Polyline CreateStroke(int touchId, Point startPoint, Color? color = null)
        {
            Color strokeColor = color ?? _defaultColor;
            
            var stroke = new Polyline
            {
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = _defaultThickness,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            stroke.Points.Add(startPoint);
            // ✨ DOT SUPPORT: Thêm điểm vi mô (+0.01px) để WPF render chấm tròn tức thì khi chạm
            stroke.Points.Add(new Point(startPoint.X + 0.01, startPoint.Y));
            
            _activeStrokes[touchId] = stroke;
            _touchColors[touchId] = strokeColor;

            System.Diagnostics.Debug.WriteLine($"✏️ Touch {touchId} started at ({startPoint.X:F0}, {startPoint.Y:F0})");
            
            return stroke;
        }

        /// <summary>
        /// Add point to existing stroke
        /// </summary>
        public void AddPointToStroke(int touchId, Point point)
        {
            if (_activeStrokes.ContainsKey(touchId))
            {
                var stroke = _activeStrokes[touchId];
                // Nếu mới chỉ có 1 điểm gốc và 1 điểm vi mô preview, thay thế điểm vi mô bằng điểm di chuyển thực tế đầu tiên
                if (stroke.Points.Count == 2 && Math.Abs(stroke.Points[1].X - stroke.Points[0].X - 0.01) < 0.001 && stroke.Points[1].Y == stroke.Points[0].Y)
                {
                    stroke.Points[1] = point;
                }
                else
                {
                    stroke.Points.Add(point);
                }
            }
        }

        /// <summary>
        /// Complete a stroke (touch up)
        /// </summary>
        public Polyline? CompleteStroke(int touchId)
        {
            if (_activeStrokes.ContainsKey(touchId))
            {
                var stroke = _activeStrokes[touchId];
                _activeStrokes.Remove(touchId);
                _touchColors.Remove(touchId);
                
                System.Diagnostics.Debug.WriteLine($"✅ Touch {touchId} completed ({stroke.Points.Count} points)");
                
                return stroke;
            }
            
            return null;
        }

        /// <summary>
        /// Clear all active strokes
        /// </summary>
        public void ClearAllStrokes()
        {
            _activeStrokes.Clear();
            _touchColors.Clear();
            System.Diagnostics.Debug.WriteLine("🧹 All active strokes cleared");
        }

        /// <summary>
        /// Get statistics
        /// </summary>
        public string GetStatistics()
        {
            return $"Active touches: {_activeStrokes.Count}, Mode: {_currentMode}";
        }
    }
}
