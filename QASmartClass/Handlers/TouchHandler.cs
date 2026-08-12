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
        private Action<Point>? _updateEraserPreview;
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
        private string _eraserMode = "Stroke";  // "Stroke" or "Drag"
        
        // ENGINE A: Drag Erase support for Touch
        private Point? _dragTouchStartPoint;
        private Point? _dragTouchLastPosition;
        private System.Windows.Shapes.Rectangle? _dragTouchPreviewRect;

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
        /// Returns true if any touch strokes are currently being drawn.
        /// Used by mouse handlers to detect when mouse events are promoted
        /// from touch input (prevents duplicate strokes on interactive screens).
        /// </summary>
        public bool HasActiveTouches => _touchManager.GetActiveTouchCount() > 0;

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
                    if (_eraserMode == "Drag")
                    {
                        // --- DRAG ERASE MODE (Touch) ---
                        _canvas.CaptureTouch(e.TouchDevice);
                        _dragTouchStartPoint = position;
                        _dragTouchPreviewRect = new System.Windows.Shapes.Rectangle
                        {
                            Stroke = System.Windows.Media.Brushes.Red,
                            StrokeThickness = 2,
                            StrokeDashArray = new System.Windows.Media.DoubleCollection { 4, 4 },
                            Fill = new System.Windows.Media.SolidColorBrush(
                                System.Windows.Media.Color.FromArgb(25, 255, 0, 0)),
                            IsHitTestVisible = false,
                            Width = 0,
                            Height = 0
                        };
                        Canvas.SetLeft(_dragTouchPreviewRect, position.X);
                        Canvas.SetTop(_dragTouchPreviewRect, position.Y);
                        Canvas.SetZIndex(_dragTouchPreviewRect, 9999);
                        _canvas.Children.Add(_dragTouchPreviewRect);
                        System.Diagnostics.Debug.WriteLine(
                            $"🧹 Touch Drag erase started at ({position.X:F0}, {position.Y:F0})");
                    }
                    else
                    {
                        // --- STROKE ERASE MODE (existing) ---
                        _lastTouchPoints[touchId] = position;
                        _updateEraserPreview?.Invoke(position);
                        EraseAtPoint(position);
                        System.Diagnostics.Debug.WriteLine($"🧹 Touch {touchId} ERASE at ({position.X:F0}, {position.Y:F0})");
                    }
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
                    _updateEraserPreview?.Invoke(position);
                    if (_eraserEngine != null)
                    {
                        _eraserEngine.UpdateEraserPreview(position);
                    }

                    if (_eraserMode == "Drag" && _dragTouchStartPoint.HasValue && _dragTouchPreviewRect != null)
                    {
                        // --- DRAG ERASE PREVIEW UPDATE ---
                        _dragTouchLastPosition = position;
                        double x = Math.Min(_dragTouchStartPoint.Value.X, position.X);
                        double y = Math.Min(_dragTouchStartPoint.Value.Y, position.Y);
                        double w = Math.Abs(position.X - _dragTouchStartPoint.Value.X);
                        double h = Math.Abs(position.Y - _dragTouchStartPoint.Value.Y);
                        Canvas.SetLeft(_dragTouchPreviewRect, x);
                        Canvas.SetTop(_dragTouchPreviewRect, y);
                        _dragTouchPreviewRect.Width = w;
                        _dragTouchPreviewRect.Height = h;
                    }
                    else if (_eraserMode == "Stroke" && e.TouchDevice.Captured == _canvas)
                    {
                        // --- STROKE ERASE MODE ---
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
                        EraseAtPoint(position);
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

        /// <summary>
        /// ✅ QC_4.2_SMART_TOUCH_ERASER_FIX: Thu dọn và thực thi xóa vùng Drag Erase an toàn 100% trên IFP
        /// </summary>
        private void CompleteDragTouchErase(Point? endPosition = null)
        {
            if (_dragTouchStartPoint.HasValue)
            {
                var startPt = _dragTouchStartPoint.Value;
                var endPt = endPosition ?? _dragTouchLastPosition ?? startPt;

                double x = Math.Min(startPt.X, endPt.X);
                double y = Math.Min(startPt.Y, endPt.Y);
                double w = Math.Abs(endPt.X - startPt.X);
                double h = Math.Abs(endPt.Y - startPt.Y);

                if (_dragTouchPreviewRect != null)
                {
                    _canvas.Children.Remove(_dragTouchPreviewRect);
                    _dragTouchPreviewRect = null;
                }
                _dragTouchStartPoint = null;
                _dragTouchLastPosition = null;

                if (w >= 5 && h >= 5)
                {
                    var eraseRect = new Rect(x, y, w, h);
                    
                    // ✅ QC_4.2_SMART_TOUCH_DRAG_ERASE_FIX: Đảm bảo EraserEngine không bao giờ null khi Drag Erase trên màn hình cảm ứng
                    _eraserEngine ??= new Managers.EraserEngine(_canvas);

                    var erased = _eraserEngine.EraseByDrag(eraseRect);
                    System.Diagnostics.Debug.WriteLine(
                        $"🧹 [SMART TOUCH ERASE FIX] Drag erase completed: ({x:F0},{y:F0}) {w:F0}x{h:F0} — {erased.Count} elements erased");

                    if (erased != null && erased.Count > 0)
                    {
                        foreach (var el in erased)
                        {
                            _recordRemoveAction?.Invoke(el, "Touch drag erase");
                        }
                    }
                    else
                    {
                        // Fallback xóa trực tiếp trên Canvas đối với các phần tử đặc biệt
                        var elementsToRemove = new List<UIElement>();
                        foreach (UIElement child in _canvas.Children)
                        {
                            if (child is FrameworkElement fe && fe.Tag?.ToString() == "BackgroundLayer") continue;
                            if (!child.IsHitTestVisible) continue;
                            if (child is Rectangle rect && rect.IsHitTestVisible == false && rect.StrokeDashArray != null) continue;

                            Rect bounds = QASmartTouch.Helpers.BoundsHelper.GetAbsoluteBounds(child, _canvas);
                            if (!bounds.IsEmpty && eraseRect.IntersectsWith(bounds))
                            {
                                elementsToRemove.Add(child);
                            }
                        }

                        foreach (var element in elementsToRemove)
                        {
                            _canvas.Children.Remove(element);
                            _recordRemoveAction?.Invoke(element, "Touch drag erase fallback");
                            System.Diagnostics.Debug.WriteLine($"🧹 [SMART TOUCH ERASE FALLBACK] Erased element: {element.GetType().Name}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("🧹 [SMART TOUCH ERASE FIX] Drag erase cancelled (area too small)");
                }
            }
            else if (_dragTouchPreviewRect != null)
            {
                _canvas.Children.Remove(_dragTouchPreviewRect);
                _dragTouchPreviewRect = null;
            }
        }

        private void Canvas_TouchLeave(object sender, TouchEventArgs e)
        {
            if (_dragTouchStartPoint.HasValue || _dragTouchPreviewRect != null)
            {
                CompleteDragTouchErase();
            }
            else
            {
                Canvas_TouchUp(sender, e);
            }
        }

        private void Canvas_LostTouchCapture(object sender, TouchEventArgs e)
        {
            if (_dragTouchStartPoint.HasValue || _dragTouchPreviewRect != null)
            {
                CompleteDragTouchErase();
            }
        }

        private void Canvas_TouchUp(object sender, TouchEventArgs e)
        {
            try
            {
                if (!_isEnabled) return;

                // ✅ QC_4.2_SMART_TOUCH_ERASER_FIX: FOR DRAG ERASE, DO NOT early-return on Captured != _canvas!
                // Driver màn hình cảm ứng Smart Touch (IFP) giải phóng TouchCapture trước khi dispatch TouchUp.
                if (_toolMode == TouchToolMode.Eraser && _eraserMode == "Drag" && _dragTouchStartPoint.HasValue)
                {
                    Point upPos = e.GetTouchPoint(_canvas).Position;
                    CompleteDragTouchErase(upPos);
                    e.Handled = true;
                    return;
                }

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
                    // --- STROKE ERASE MODE cleanup ---
                    _touchManager.CompleteStroke(e.TouchDevice.Id);
                    System.Diagnostics.Debug.WriteLine($"🧹 Touch {e.TouchDevice.Id} erase completed");
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
        // Thuật toán: Hình học thủ công 8 nhánh (Polyline/Path/Line/Rect/Ellipse/Polygon/Image/TextBlock)
        // Bán kính: _eraserSize (mặc định 20px)
        // Reference: eraser_tool_specification.md v2.1 Mục III.B
        // ═══════════════════════════════════════════════════════
        private void EraseAtPoint(Point point)
        {
            var elementsToRemove = new List<UIElement>();

            foreach (UIElement element in _canvas.Children)
            {
                // Skip non-removable elements
                if (element is FrameworkElement fwElement && fwElement.Tag?.ToString() == "BackgroundLayer")
                    continue;
                
                // ✅ Phase 0.3: Skip non-hittestable elements (e.g., eraser preview cursor)
                if (!element.IsHitTestVisible)
                    continue;

                bool shouldRemove = false;

                // Check Polyline (raw pen strokes)
                if (element is Polyline polyline)
                {
                    foreach (Point p in polyline.Points)
                    {
                        double dx = p.X - point.X;
                        double dy = p.Y - point.Y;
                        if (dx * dx + dy * dy <= _eraserSize * _eraserSize)
                        {
                            shouldRemove = true;
                            break;
                        }
                    }
                }
                // Check Path (smooth Bezier strokes created by ConvertToSmoothPath)
                else if (element is System.Windows.Shapes.Path path && path.Data != null)
                {
                    // Check if point is within the path's rendered bounds + eraser radius
                    var bounds = path.Data.Bounds;
                    var inflated = new Rect(
                        bounds.X - _eraserSize,
                        bounds.Y - _eraserSize,
                        bounds.Width + _eraserSize * 2,
                        bounds.Height + _eraserSize * 2);

                    if (inflated.Contains(point))
                    {
                        // More precise check: use path geometry hit test
                        var pen = new Pen(Brushes.Black, (path.StrokeThickness > 0 ? path.StrokeThickness : 2) + _eraserSize * 2);
                        bool hit = path.Data.StrokeContains(pen, point);
                        if (hit)
                        {
                            shouldRemove = true;
                        }
                    }
                }
                // Check Line
                else if (element is Line line)
                {
                    double dist = DistanceFromPointToLine(point, new Point(line.X1, line.Y1), new Point(line.X2, line.Y2));
                    if (dist <= _eraserSize)
                    {
                        shouldRemove = true;
                    }
                }
                // ✅ Phase 0.3: Check Rectangle
                else if (element is Rectangle rectangle)
                {
                    double left = Canvas.GetLeft(rectangle);
                    double top = Canvas.GetTop(rectangle);
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;
                    
                    double right = left + rectangle.ActualWidth;
                    double bottom = top + rectangle.ActualHeight;
                    
                    // Hit if point is near any edge or inside the rectangle
                    bool hitInside = point.X >= left && point.X <= right && point.Y >= top && point.Y <= bottom;
                    bool hitEdge = (Math.Abs(point.Y - top) <= _eraserSize && point.X >= left - _eraserSize && point.X <= right + _eraserSize)
                                || (Math.Abs(point.Y - bottom) <= _eraserSize && point.X >= left - _eraserSize && point.X <= right + _eraserSize)
                                || (Math.Abs(point.X - left) <= _eraserSize && point.Y >= top - _eraserSize && point.Y <= bottom + _eraserSize)
                                || (Math.Abs(point.X - right) <= _eraserSize && point.Y >= top - _eraserSize && point.Y <= bottom + _eraserSize);
                    
                    if (hitEdge || hitInside)
                    {
                        shouldRemove = true;
                    }
                }
                // ✅ Phase 0.3: Check Ellipse
                else if (element is Ellipse ellipse)
                {
                    double left = Canvas.GetLeft(ellipse);
                    double top = Canvas.GetTop(ellipse);
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;
                    
                    double cx = left + ellipse.ActualWidth / 2;
                    double cy = top + ellipse.ActualHeight / 2;
                    double rx = ellipse.ActualWidth / 2;
                    double ry = ellipse.ActualHeight / 2;
                    
                    if (rx > 0 && ry > 0)
                    {
                        // Normalized distance from center (1.0 = on edge)
                        double ndx = (point.X - cx) / rx;
                        double ndy = (point.Y - cy) / ry;
                        double dist = Math.Sqrt(ndx * ndx + ndy * ndy);
                        
                        bool isFilled = ellipse.Fill != null && ellipse.Fill != Brushes.Transparent;
                        if (isFilled && dist <= 1.0 + (_eraserSize / Math.Min(rx * 2, ry * 2)))
                        {
                            shouldRemove = true;
                        }
                        else if (!isFilled && Math.Abs(dist - 1.0) * Math.Min(rx * 2, ry * 2) / 2 <= _eraserSize)
                        {
                            shouldRemove = true;
                        }
                    }
                }
                // ✅ Phase 0.3: Check Polygon
                else if (element is Polygon polygon)
                {
                    double offsetX = Canvas.GetLeft(polygon);
                    double offsetY = Canvas.GetTop(polygon);
                    if (double.IsNaN(offsetX)) offsetX = 0;
                    if (double.IsNaN(offsetY)) offsetY = 0;
                    
                    foreach (Point p in polygon.Points)
                    {
                        Point actual = new Point(p.X + offsetX, p.Y + offsetY);
                        double dx = actual.X - point.X;
                        double dy = actual.Y - point.Y;
                        if (dx * dx + dy * dy <= _eraserSize * _eraserSize)
                        {
                            shouldRemove = true;
                            break;
                        }
                    }
                }
                // ✅ Phase 0.3: Catch-all for FrameworkElement (Image, Grid, etc.)
                else if (element is FrameworkElement genericFe)
                {
                    double feLeft = Canvas.GetLeft(genericFe);
                    double feTop = Canvas.GetTop(genericFe);
                    if (double.IsNaN(feLeft)) feLeft = 0;
                    if (double.IsNaN(feTop)) feTop = 0;
                    
                    var feBounds = new Rect(feLeft, feTop, genericFe.ActualWidth, genericFe.ActualHeight);
                    feBounds.Inflate(_eraserSize, _eraserSize);
                    if (feBounds.Contains(point))
                    {
                        shouldRemove = true;
                    }
                }

                if (shouldRemove)
                {
                    elementsToRemove.Add(element);
                }
            }

            // Remove elements from canvas
            foreach (var element in elementsToRemove)
            {
                _canvas.Children.Remove(element);
                _recordRemoveAction?.Invoke(element, "Touch erase");
                System.Diagnostics.Debug.WriteLine($"🧹 Touch erased element: {element.GetType().Name}");
            }
        }

        /// <summary>
        /// Calculate distance from point to line segment
        /// </summary>
        private static double DistanceFromPointToLine(Point p, Point a, Point b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double lengthSq = dx * dx + dy * dy;

            if (lengthSq == 0)
                return Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y));

            double t = Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSq));
            double projX = a.X + t * dx;
            double projY = a.Y + t * dy;

            return Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
        }

        /// <summary>
        /// Cleanup and unsubscribe events
        /// </summary>
        public void Dispose()
        {
            _canvas.TouchDown -= Canvas_TouchDown;
            _canvas.TouchMove -= Canvas_TouchMove;
            _canvas.TouchUp -= Canvas_TouchUp;
            _canvas.TouchLeave -= Canvas_TouchUp; // ✅ QC_4.2: Unsubscribe TouchLeave để tránh GC leak
            
            _touchManager.ClearAllStrokes();
            _smoothers.Clear();
            
            System.Diagnostics.Debug.WriteLine("🧹 TouchHandler disposed");
        }
    }
}
