using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartTouch.Utilities;
using QASmartTouch.Services.Canvas;

namespace QASmartTouch.Handlers
{
    /// <summary>
    /// Tool mode for touch interactions
    /// </summary>
    public enum TouchToolMode
    {
        None,       // No tool active — touch does nothing
        Drawing,    // Pen tool — touch creates strokes
        Eraser,     // Eraser tool — touch erases strokes
        Pointer,    // Pointer tool — touch updates pointer arrow position
        Selection   // [BUG_DRAG_MOVE] Selection tool — touch forwards to selection handlers
    }

    /// <summary>
    /// Handles touch interactions for canvas drawing
    /// Integrates TouchManager with WPF Canvas
    /// </summary>
    public class TouchHandler
    {
        private Canvas _canvas;
        private Managers.TouchManager _touchManager;
        private Action<UIElement, string>? _recordAddAction;
        private Action<UIElement, string>? _recordRemoveAction;
        private Action<Managers.EraseSession, string>? _recordEraseSessionAction;
        private Action<Point>? _updateEraserPreview;
        private Action? _hideEraserPreview;
        private Action<Point>? _updatePointerAction;
        private Action? _onCanvasTouchDown; // ✅ Callback to notify MainDashboard of touch on canvas (for closing SubMenus)
        
        // PHASE 3: Input Smoothing — ENABLED for touch parity with mouse
        private Dictionary<int, InputSmoother> _smoothers = new Dictionary<int, InputSmoother>();
        private bool _inputSmoothingEnabled = true;  // ✨ ENABLED: smooth touch input in real-time
        
        // Minimum distance filter (same as mouse path StrokeOptimizer)
        private const double MIN_TOUCH_POINT_DISTANCE = 2.5;  // pixels — matches mouse path setting
        // ENGINE A: Minimum distance between consecutive erase checks (avoid over-erasing)
        private const double TOUCH_ERASE_THROTTLE = 5.0;
        private Dictionary<int, Point> _lastTouchPoints = new Dictionary<int, Point>();
        
        // StrokeService for Bezier conversion on stroke completion
        private readonly StrokeService _strokeService = new StrokeService();
        
        // Tool mode — determines touch behavior (draw vs erase)
        private TouchToolMode _toolMode = TouchToolMode.Drawing;
        
        // Eraser properties
        private int _eraserSize = 20;
        private string _eraserMode = "Stroke";  // "Stroke" or "Point"
        
        private Color _currentPenColor = Colors.White;
        private double _currentPenSize = 2;
        private string _currentBrushType = "Normal";
        private bool _isEnabled = true;

        /// <summary>
        /// Delegate to retrieve zone-based student color (Multi-User / Split-Screen mode)
        /// </summary>
        public Func<Point, Color?>? GetColorForPosition { get; set; }

        public Managers.TouchInteractionMode CurrentMode
        {
            get => _touchManager.CurrentMode;
            set => _touchManager.CurrentMode = value;
        }

        /// <summary>
        /// Returns true if any touch contacts are currently active on canvas (drawing, erasing, etc.).
        /// Used by mouse handlers to detect when mouse events are promoted
        /// from touch input (prevents duplicate strokes/erase sessions on interactive screens).
        /// </summary>
        public bool HasActiveTouches => _touchManager.GetActiveTouchCount() > 0 || _lastTouchPoints.Count > 0;

        public TouchHandler(Canvas canvas)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _touchManager = new Managers.TouchManager();
            
            // ✨ CRITICAL: Do NOT enable IsManipulationEnabled
            // It causes WPF to convert touch events into manipulation (pan/zoom) gestures
            // which conflicts with direct touch drawing, especially at high DPI scaling
            _canvas.IsManipulationEnabled = false;
            
            // Subscribe to touch events
            _canvas.TouchDown += Canvas_TouchDown;
            _canvas.TouchMove += Canvas_TouchMove;
            _canvas.TouchUp += Canvas_TouchUp;
            _canvas.TouchLeave += Canvas_TouchLeave;
            _canvas.LostTouchCapture += Canvas_LostTouchCapture;
            
            System.Diagnostics.Debug.WriteLine("✅ TouchHandler initialized (ManipulationEnabled=false)");
            System.Diagnostics.Debug.WriteLine("✅ Input smoothing enabled (Phase 3)");
        }

        /// <summary>
        /// Set callback for recording undo actions
        /// </summary>
        public void SetRecordAddAction(Action<UIElement, string> recordAction)
        {
            _recordAddAction = recordAction;
        }

        /// <summary>
        /// Set callback for recording remove/erase actions
        /// </summary>
        public void SetRecordRemoveAction(Action<UIElement, string> recordAction)
        {
            _recordRemoveAction = recordAction;
        }

        /// <summary>
        /// Set callback for recording atomic erase session batch actions
        /// </summary>
        public void SetRecordEraseSessionAction(Action<Managers.EraseSession, string> recordAction)
        {
            _recordEraseSessionAction = recordAction;
        }

        /// <summary>
        /// Set callback for updating pointer arrow position during touch pointer mode.
        /// </summary>
        public void SetUpdatePointerAction(Action<Point> updateAction)
        {
            _updatePointerAction = updateAction;
        }

        /// <summary>
        /// Set callback for updating eraser cursor preview position during touch erasing.
        /// Without this, touch events (e.Handled=true) prevent MouseMove from firing,
        /// so the eraser preview circle would not follow the touch point.
        /// </summary>
        public void SetUpdateEraserPreviewAction(Action<Point> updateAction)
        {
            _updateEraserPreview = updateAction;
        }

        /// <summary>
        /// Set callback for hiding eraser cursor preview when touch ends.
        /// </summary>
        public void SetHideEraserPreviewAction(Action hideAction)
        {
            _hideEraserPreview = hideAction;
        }
        
        /// <summary>
        /// Set callback invoked when user touches the canvas (used by MainDashboard to close open SubMenus)
        /// </summary>
        public void SetOnCanvasTouchDownAction(Action onTouchDown)
        {
            _onCanvasTouchDown = onTouchDown;
        }

        /// <summary>
        /// Set current drawing properties
        /// </summary>
        public void SetDrawingProperties(Color color, double size, string brushType)
        {
            _currentPenColor = color;
            _currentPenSize = size;
            _currentBrushType = brushType;
            
            _touchManager.SetDefaultDrawingProperties(color, size, brushType);
        }

        /// <summary>
        /// Set current eraser properties
        /// </summary>
        public void SetEraserProperties(double size, string mode)
        {
            _eraserSize = (int)size;
            _eraserMode = mode;
        }

        /// <summary>
        /// Enable or disable touch drawing
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            if (!enabled)
            {
                _smoothers.Clear();
                _lastTouchPoints.Clear();
            }
        }

        /// <summary>
        /// Set active tool mode (Drawing, Eraser, Pointer, None)
        /// </summary>
        public void SetToolMode(TouchToolMode mode)
        {
            _toolMode = mode;
            System.Diagnostics.Debug.WriteLine($"👆 TouchHandler tool mode changed to: {mode}");
        }

        /// <summary>
        /// Set the EraserEngine reference for Drag Erase support
        /// </summary>
        private Managers.EraserEngine? _eraserEngine;
        public void SetEraserEngine(Managers.EraserEngine engine)
        {
            _eraserEngine = engine;
        }

        /// <summary>
        /// Get current statistics
        /// </summary>
        public string GetStatistics()
        {
            return _touchManager.GetStatistics();
        }

        private void Canvas_TouchDown(object sender, TouchEventArgs e)
        {
            if (!_isEnabled) return;

            // Allow touch promotion to mouse events when tool mode is None/Selection
            // OR when in Eraser tool mode but the active eraser mode is Drag (marquee select) or ClearAll
            // [BUG_DRAG_MOVE] Selection mode: pass through so SelectionBox ManipulationDelta can handle drag
            if (_toolMode == TouchToolMode.None || 
                _toolMode == TouchToolMode.Selection ||
                (_toolMode == TouchToolMode.Eraser && _eraserMode == "ClearAll"))
                return;

            try
            {
                // ✅ QC_4.2_SUBMENU_AUTO_CLOSE: Đóng bất kỳ SubMenu nào đang mở khi GV chạm vào bất kỳ đâu
                _onCanvasTouchDown?.Invoke();

                // ✅ QC_4.2_SMART_TOUCH_ERASER_TEXTBOX_FIX: Trong chế độ Tẩy (Eraser Mode), KHÔNG ĐƯỢC bỏ qua TouchDown!
                // Cho phép GV tẩy nét vẽ nằm đè lên Hộp văn bản (TextBoxContainer) hoặc Bảng (TableContainer).
                // Chỉ bỏ qua đối với các tool mode khác (Drawing, Pointer) để tương tác nhập dữ liệu.
                bool isEraserMode = (_toolMode == TouchToolMode.Eraser);
                if (!isEraserMode && InputValidationHelper.IsEventFromInteractiveControl(e.OriginalSource, _canvas))
                {
                    System.Diagnostics.Debug.WriteLine("👆 Touch on interactive control — skipping Canvas TouchDown for non-Eraser modes");
                    return;
                }

                var touchPoint = e.GetTouchPoint(_canvas);
                int touchId = e.TouchDevice.Id;
                Point position = touchPoint.Position;

                // ✨ CRITICAL: Capture touch to prevent ScrollViewer interception
                _canvas.CaptureTouch(e.TouchDevice);

                if (_toolMode == TouchToolMode.Pointer)
                {
                    // --- POINTER MODE ---
                    _updatePointerAction?.Invoke(position);
                    System.Diagnostics.Debug.WriteLine($"📌 Touch {touchId} POINTER at ({position.X:F0}, {position.Y:F0})");
                }
                else if (_toolMode == TouchToolMode.Drawing)
                {
                    // --- DRAWING MODE ---
                    if (_inputSmoothingEnabled && !_smoothers.ContainsKey(touchId))
                    {
                        _smoothers[touchId] = new InputSmoother(4, InputSmoother.SmoothingMode.Weighted);
                    }
                    Color strokeColor = _currentPenColor;
                    if (GetColorForPosition != null)
                    {
                        Color? zoneColor = GetColorForPosition.Invoke(position);
                        if (zoneColor.HasValue)
                        {
                            strokeColor = zoneColor.Value;
                        }
                    }

                    var stroke = _touchManager.CreateStroke(touchId, position, strokeColor);
                    ApplyBrushStyle(stroke);
                    _canvas.Children.Add(stroke);
                    // ✅ QC_4.2_STROKE_ABOVE_TABLE (NV-2a): Touch nét vẽ TRÊN Table/TextBox
                    Panel.SetZIndex(stroke, QASmartTouch.Helpers.ZIndexConstants.UserContentMax);

                    System.Diagnostics.Debug.WriteLine($"👆 Touch {touchId} DRAW at ({position.X:F0}, {position.Y:F0})");
                }
                else if (_toolMode == TouchToolMode.Eraser)
                {
                    _lastTouchPoints[touchId] = position;
                    if (_updateEraserPreview != null)
                    {
                        _updateEraserPreview.Invoke(position);
                    }
                    else
                    {
                        _eraserEngine?.UpdateEraserPreview(position);
                    }
                    _eraserEngine?.StartTouchSession(touchId);

                    if (_eraserMode == "Point" || _eraserMode == "Drag")
                    {
                        EraseByPointAt(position, touchId);
                    }
                    else
                    {
                        EraseAtPoint(position, touchId);
                    }
                    System.Diagnostics.Debug.WriteLine($"🧹 Touch {touchId} ERASE at ({position.X:F0}, {position.Y:F0})");
                }
                // None mode: do nothing but still capture touch
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ TouchDown error: {ex.Message}");
            }

            // ✅ QC_4.2_SMART_TOUCH_ERASER_TEXTBOX_FIX: Trong Eraser Mode, luôn đặt Handled = true để chặn TextBox nhận focus
            if (_toolMode == TouchToolMode.Eraser || !InputValidationHelper.IsEventFromInteractiveControl(e.OriginalSource, _canvas))
            {
                e.Handled = true;
            }
        }

        private void Canvas_TouchMove(object sender, TouchEventArgs e)
        {
            if (!_isEnabled) return;
            if (_toolMode == TouchToolMode.None || 
                (_toolMode == TouchToolMode.Eraser && _eraserMode == "ClearAll"))
                return;

            try
            {
                var touchPoint = e.GetTouchPoint(_canvas);
                int touchId = e.TouchDevice.Id;
                Point position = touchPoint.Position;

                // ✅ QC_4.2_SMART_TOUCH_ERASER_PREVIEW_FIX: Luôn cập nhật vị trí vệt tẩy bám sát 100% điểm chạm bút cảm ứng
                if (_toolMode == TouchToolMode.Eraser)
                {
                    if (_updateEraserPreview != null)
                    {
                        _updateEraserPreview.Invoke(position);
                    }
                    else
                    {
                        _eraserEngine?.UpdateEraserPreview(position);
                    }

                    if (_lastTouchPoints.TryGetValue(touchId, out var lastPt))
                    {
                        double dx = position.X - lastPt.X;
                        double dy = position.Y - lastPt.Y;
                        double dist = Math.Sqrt(dx * dx + dy * dy);
                        if (dist < TOUCH_ERASE_THROTTLE) // Minimum move distance for erase
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                    _lastTouchPoints[touchId] = position;

                    if (_eraserMode == "Point" || _eraserMode == "Drag")
                    {
                        EraseByPointAt(position, touchId);
                    }
                    else
                    {
                        EraseAtPoint(position, touchId);
                    }

                    e.Handled = true;
                    return;
                }

                if (e.TouchDevice.Captured != _canvas) return;

                if (_toolMode == TouchToolMode.Pointer)
                {
                    _updatePointerAction?.Invoke(position);
                }
                else if (_toolMode == TouchToolMode.Drawing)
                {
                    // --- DRAWING MODE ---
                    Point smoothedPosition = position;
                    if (_inputSmoothingEnabled && _smoothers.ContainsKey(touchId))
                    {
                        smoothedPosition = _smoothers[touchId].SmoothPoint(position);
                    }

                    if (_lastTouchPoints.TryGetValue(touchId, out var lastPt))
                    {
                        double dx = smoothedPosition.X - lastPt.X;
                        double dy = smoothedPosition.Y - lastPt.Y;
                        double dist = Math.Sqrt(dx * dx + dy * dy);
                        if (dist < MIN_TOUCH_POINT_DISTANCE)
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                    _lastTouchPoints[touchId] = smoothedPosition;
                    _touchManager.AddPointToStroke(touchId, smoothedPosition);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ TouchMove error: {ex.Message}");
            }

            e.Handled = true;
        }

        private void Canvas_TouchLeave(object sender, TouchEventArgs e)
        {
            Canvas_TouchUp(sender, e);

            if (_toolMode == TouchToolMode.Eraser)
            {
                _eraserEngine?.HideEraserPreview();
                _hideEraserPreview?.Invoke();
            }
        }

        private void Canvas_LostTouchCapture(object sender, TouchEventArgs e)
        {
            if (e.TouchDevice != null)
            {
                int touchId = e.TouchDevice.Id;
                _lastTouchPoints.Remove(touchId);

                // Clean up smoother for this touch ID to prevent memory leaks
                if (_smoothers.ContainsKey(touchId))
                {
                    _smoothers[touchId].Clear();
                    _smoothers.Remove(touchId);
                }

                // If in drawing mode and capture was abruptly lost, complete and discard the stroke
                if (_toolMode == TouchToolMode.Drawing)
                {
                    try
                    {
                        var stroke = _touchManager.CompleteStroke(touchId);
                        if (stroke != null && _canvas.Children.Contains(stroke))
                        {
                            _canvas.Children.Remove(stroke);
                        }
                    }
                    catch { }
                }
            }

            if (_toolMode == TouchToolMode.Eraser)
            {
                _eraserEngine?.HideEraserPreview();
                _hideEraserPreview?.Invoke();
            }
        }

        private void Canvas_TouchUp(object sender, TouchEventArgs e)
        {
            try
            {
                if (!_isEnabled) return;

                if (_toolMode == TouchToolMode.None || 
                    (_toolMode == TouchToolMode.Eraser && _eraserMode == "ClearAll"))
                    return;
                if (e.TouchDevice.Captured != _canvas) return;

                int touchId = e.TouchDevice.Id;

                if (_toolMode == TouchToolMode.Drawing)
                {
                    // --- DRAWING MODE: complete stroke and convert to Bezier ---
                    var stroke = _touchManager.CompleteStroke(touchId);

                    if (stroke != null)
                    {
                        var smoothPath = _strokeService.ConvertToSmoothPath(stroke);
                        if (smoothPath != null)
                        {
                            _canvas.Children.Remove(stroke);
                            _canvas.Children.Add(smoothPath);
                            // ✅ QC_4.2_STROKE_ABOVE_TABLE (NV-2b): Touch smoothPath TRÊN Table/TextBox
                            Panel.SetZIndex(smoothPath, QASmartTouch.Helpers.ZIndexConstants.UserContentMax);
                            _recordAddAction?.Invoke(smoothPath, $"Touch draw (ID: {touchId})");
                            System.Diagnostics.Debug.WriteLine($"✨ Touch {touchId}: Polyline → SmoothPath ({stroke.Points.Count} pts)");
                        }
                        else
                        {
                            _recordAddAction?.Invoke(stroke, $"Touch draw (ID: {touchId})");
                        }
                    }
                }
                else if (_toolMode == TouchToolMode.Eraser)
                {
                    // --- STROKE / POINT ERASE MODE cleanup & atomic batch record ---
                    _touchManager.CompleteStroke(touchId);
                    var session = _eraserEngine?.EndTouchSession(touchId);
                    if (session != null && session.HasChanges)
                    {
                        _recordEraseSessionAction?.Invoke(session, $"Touch erase (ID: {touchId})");
                    }
                    _eraserEngine?.HideEraserPreview();
                    _hideEraserPreview?.Invoke();
                    System.Diagnostics.Debug.WriteLine($"🧹 Touch {touchId} erase completed");
                }

                // Clean up smoother and last-point tracker for this touch
                if (_smoothers.ContainsKey(touchId))
                {
                    _smoothers[touchId].Clear();
                    _smoothers.Remove(touchId);
                }
                _lastTouchPoints.Remove(touchId);

                e.Handled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ TouchUp error: {ex.Message}");
            }
            finally
            {
                // ✅ Luôn giải phóng TouchCapture bất chấp mọi exception hoặc early return
                if (e.TouchDevice.Captured == _canvas)
                {
                    _canvas.ReleaseTouchCapture(e.TouchDevice);
                }
            }
        }

        private void ApplyBrushStyle(Polyline stroke)
        {
            // =====================================================
            // ✨ HIGH-QUALITY RENDERING — matches mouse stroke quality
            // Anti-aliased + sub-pixel precision = smooth curves
            // =====================================================
            RenderOptions.SetEdgeMode(stroke, EdgeMode.Unspecified);    // ✨ Anti-aliased (was Aliased → jagged)
            RenderOptions.SetBitmapScalingMode(stroke, BitmapScalingMode.HighQuality);
            RenderOptions.SetCachingHint(stroke, CachingHint.Cache);   // GPU cache for performance
            stroke.SnapsToDevicePixels = false;  // ✨ Sub-pixel precision (was true → staircase)
            stroke.UseLayoutRounding = false;    // ✨ No rounding (was true → staircase)
            
            // Apply different styles based on brush type
            switch (_currentBrushType)
            {
                case "Normal":
                    // Standard smooth pen
                    stroke.StrokeLineJoin = PenLineJoin.Round;
                    stroke.StrokeStartLineCap = PenLineCap.Round;
                    stroke.StrokeEndLineCap = PenLineCap.Round;
                    break;

                case "Hoc":
                    // Educational pen - thicker, more visible
                    stroke.StrokeLineJoin = PenLineJoin.Round;
                    stroke.StrokeStartLineCap = PenLineCap.Round;
                    stroke.StrokeEndLineCap = PenLineCap.Round;
                    stroke.StrokeThickness = _currentPenSize * 1.5;
                    break;

                case "Marker":
                    // Marker style - flat ends
                    stroke.StrokeLineJoin = PenLineJoin.Miter;
                    stroke.StrokeStartLineCap = PenLineCap.Flat;
                    stroke.StrokeEndLineCap = PenLineCap.Flat;
                    stroke.Opacity = 0.7;
                    break;

                case "Highlighter":
                    // Highlighter - transparent and wide
                    stroke.StrokeLineJoin = PenLineJoin.Round;
                    stroke.StrokeStartLineCap = PenLineCap.Round;
                    stroke.StrokeEndLineCap = PenLineCap.Round;
                    stroke.Opacity = 0.3;
                    stroke.StrokeThickness = _currentPenSize * 2;
                    break;

                default:
                    // Fallback to normal
                    stroke.StrokeLineJoin = PenLineJoin.Round;
                    stroke.StrokeStartLineCap = PenLineCap.Round;
                    stroke.StrokeEndLineCap = PenLineCap.Round;
                    break;
            }
        }
        /// <summary>
        /// Erase elements at the given point (within eraser radius)
        /// Mirrors the logic in Form2_MainDashboard.EraseStrokeAt for mouse eraser
        /// </summary>
        // ═══════════════════════════════════════════════════════
        // ENGINE A: SMART TOUCH MODULE — Touch Eraser
        /// <summary>
        /// Erases entire stroke/object at given point (Stroke Erase mode).
        /// </summary>
        private void EraseAtPoint(Point point, int touchId = -1)
        {
            _eraserEngine ??= new Managers.EraserEngine(_canvas);
            var session = touchId >= 0 ? _eraserEngine.GetTouchSession(touchId) : null;
            _eraserEngine.EraseByStrokeAtPoint(point, _eraserSize, session);
        }

        /// <summary>
        /// Slices/cuts strokes under touch point (Point Erase mode).
        /// </summary>
        private void EraseByPointAt(Point point, int touchId = -1)
        {
            _eraserEngine ??= new Managers.EraserEngine(_canvas);
            var session = touchId >= 0 ? _eraserEngine.GetTouchSession(touchId) : null;
            _eraserEngine.EraseByPoint(point, _eraserSize, session);
        }

        /// <summary>
        /// Cleanup and unsubscribe events
        /// </summary>
        public void Dispose()
        {
            _canvas.TouchDown -= Canvas_TouchDown;
            _canvas.TouchMove -= Canvas_TouchMove;
            _canvas.TouchUp -= Canvas_TouchUp;
            _canvas.TouchLeave -= Canvas_TouchLeave;
            _canvas.LostTouchCapture -= Canvas_LostTouchCapture;
            
            _touchManager.ClearAllStrokes();
            _smoothers.Clear();
            _lastTouchPoints.Clear();
            
            System.Diagnostics.Debug.WriteLine("🧹 TouchHandler disposed");
        }
    }
}
