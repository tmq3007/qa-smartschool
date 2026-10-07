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
    /// Chế độ nhận diện cử chỉ 2 ngón tay (Pha 3): Phân biệt Kéo di chuyển thuần túy (Pan) và Thu phóng (Zoom)
    /// </summary>
    public enum TwoFingerGestureMode
    {
        Pending,    // Vừa chạm 2 ngón, đang theo dõi để xác định ý định người dùng (chưa vượt ngưỡng)
        PanOnly,    // Người dùng đang kéo di chuyển bảng (khóa zoom 100%, lướt êm tuyệt đối không dính zoom)
        PinchZoom   // Người dùng đang cố ý thu phóng (cho phép zoom mượt mà kết hợp pan)
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
        private UIElement? _outerSurface; // Reference to outer surface for touch forwarding
        
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
        private double _currentPenSize = 5;
        private string _currentBrushType = "Normal";
        private bool _isEnabled = true;

        /// <summary>
        /// Delegate to retrieve zone-based student color (Multi-User / Split-Screen mode)
        /// </summary>
        public Func<Point, Color?>? GetColorForPosition { get; set; }

        /// <summary>
        /// QC_4.2_MILESTONE_ZINDEX: Delegate truy vấn tầng Z-Index hiện tại cho nét vẽ hoàn tất
        /// </summary>
        public Func<int>? GetCurrentInkingZIndex { get; set; }

        // Two-Finger Pinch-to-Zoom & Pan fields (Phase 3)
        private bool _isTwoFingerGestureActive = false;
        private bool _suppressDrawingUntilAllReleased = false;
        private int _gestureTouchId1 = -1;
        private int _gestureTouchId2 = -1;
        private double _gestureStartDistance = 0;
        private double _gestureLastDistance = 0;
        private double _gestureFilteredDistance = 0;
        private Point _gestureStartCenter;
        private Point _gestureLastCenter;
        private TwoFingerGestureMode _gestureMode = TwoFingerGestureMode.Pending;
        private DateTime _lastGestureEndTime = DateTime.MinValue;
        private int _singleDrawingTouchId = -1;
        private Dictionary<int, Point> _activeTouchScreenPoints = new Dictionary<int, Point>();
        private Polyline? _preliminaryStroke = null;
        private int _preliminaryTouchId = -1;

        /// <summary>
        /// Xóa sạch toàn bộ trạng thái cảm ứng (chạm, vẽ dở, cử chỉ) khi đổi mode hoặc reset
        /// </summary>
        public void ResetTouchState()
        {
            _isTwoFingerGestureActive = false;
            _suppressDrawingUntilAllReleased = false;
            _singleDrawingTouchId = -1;
            _gestureTouchId1 = -1;
            _gestureTouchId2 = -1;
            _gestureStartDistance = 0;
            _gestureLastDistance = 0;
            _gestureFilteredDistance = 0;
            _gestureMode = TwoFingerGestureMode.Pending;
            _lastGestureEndTime = DateTime.MinValue;
            _activeTouchScreenPoints.Clear();
            _lastTouchPoints.Clear();
            _smoothers.Clear();

            if (_preliminaryStroke != null)
            {
                try
                {
                    if (_canvas.Children.Contains(_preliminaryStroke))
                    {
                        _canvas.Children.Remove(_preliminaryStroke);
                    }
                    if (_preliminaryTouchId != -1)
                    {
                        _touchManager.CompleteStroke(_preliminaryTouchId);
                    }
                }
                catch { }
                _preliminaryStroke = null;
                _preliminaryTouchId = -1;
            }
        }

        /// <summary>
        /// Delegate invoked when two-finger pinch/pan gesture begins (Point canvasCenter)
        /// </summary>
        public Action<Point>? OnTwoFingerPinchPanStarted { get; set; }

        /// <summary>
        /// Delegate invoked during two-finger pinch/pan gesture (Point canvasCenter, double scaleStep, Vector panStep)
        /// </summary>
        public Action<Point, double, Vector>? OnTwoFingerPinchPan { get; set; }

        /// <summary>
        /// Delegate invoked when two-finger pinch/pan gesture ends
        /// </summary>
        public Action? OnTwoFingerPinchPanEnded { get; set; }

        /// <summary>
        /// Callback to check if Multi-User Split Mode is currently active (guards against pinch in 2-user mode)
        /// </summary>
        public Func<bool>? IsMultiUserModeActive { get; set; }

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
        public bool HasActiveTouches => _touchManager.GetActiveTouchCount() > 0 || _lastTouchPoints.Count > 0 || _activeTouchScreenPoints.Count > 0;

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
        /// Đăng ký bề mặt cảm ứng bên ngoài (ScrollViewer) để chuyển tiếp chạm cảm ứng vào Canvas khi zoom nhỏ (Zero Dead Zone for Touch)
        /// </summary>
        public void RegisterOuterTouchSurface(UIElement outerSurface)
        {
            if (outerSurface != null)
            {
                _outerSurface = outerSurface;
                
                // ✅ Sử dụng AddHandler với handledEventsToo=true để bắt sự kiện Touch 
                // ngay cả khi ScrollViewer đã đánh dấu e.Handled=true (chống nuốt sự kiện)
                outerSurface.AddHandler(UIElement.TouchDownEvent, new EventHandler<TouchEventArgs>((s, e) =>
                {
                    if (IsCanvasOrDescendant(e.OriginalSource))
                        return;
                    Canvas_TouchDown(_canvas, e);
                }), true);
                
                outerSurface.AddHandler(UIElement.TouchMoveEvent, new EventHandler<TouchEventArgs>((s, e) =>
                {
                    if (IsCanvasOrDescendant(e.OriginalSource))
                        return;
                    Canvas_TouchMove(_canvas, e);
                }), true);
                
                outerSurface.AddHandler(UIElement.TouchUpEvent, new EventHandler<TouchEventArgs>((s, e) =>
                {
                    if (IsCanvasOrDescendant(e.OriginalSource))
                        return;
                    Canvas_TouchUp(_canvas, e);
                }), true);
                
                outerSurface.AddHandler(UIElement.TouchLeaveEvent, new EventHandler<TouchEventArgs>((s, e) =>
                {
                    if (IsCanvasOrDescendant(e.OriginalSource))
                        return;
                    Canvas_TouchLeave(_canvas, e);
                }), true);
                
                outerSurface.LostTouchCapture += (s, e) =>
                {
                    if (IsCanvasOrDescendant(e.OriginalSource))
                        return;
                    Canvas_LostTouchCapture(_canvas, e);
                };
            }
        }

        /// <summary>
        /// ✅ QC_4.2_FLOWDOCUMENT_TOUCH_FIX: Kiểm tra an toàn xem đối tượng phát sinh sự kiện cảm ứng
        /// có phải là Canvas hoặc bất kỳ phần tử con nào thuộc Canvas hay không.
        /// Xử lý an toàn cả ContentElement (FlowDocument, Paragraph, Run trong RichTextBox) và VisualTree,
        /// tuyệt đối không để VisualTreeHelper ném ngoại lệ InvalidOperationException.
        /// </summary>
        private bool IsCanvasOrDescendant(object? source)
        {
            if (source == null || _canvas == null) return false;
            if (ReferenceEquals(source, _canvas)) return true;

            try
            {
                var element = source as DependencyObject;
                while (element != null)
                {
                    if (ReferenceEquals(element, _canvas)) return true;

                    // FlowDocument, Paragraph, Run là ContentElement, KHÔNG phải Visual
                    // → Dùng LogicalTreeHelper cho ContentElement, VisualTreeHelper cho Visual / Visual3D
                    if (element is Visual || element is System.Windows.Media.Media3D.Visual3D)
                    {
                        element = VisualTreeHelper.GetParent(element);
                    }
                    else
                    {
                        element = LogicalTreeHelper.GetParent(element);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ IsCanvasOrDescendant error: {ex.Message}");
            }

            return false;
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

                // Track screen/window position for robust multi-touch gesture calculation
                UIElement referenceElement = _canvas.Parent as UIElement ?? _canvas;
                Point screenPos = e.GetTouchPoint(referenceElement).Position;
                _activeTouchScreenPoints[touchId] = screenPos;

                // --- SINGLE FINGER / GESTURE MODE (ĐƠN ĐIỂM) ---
                if (CurrentMode == Managers.TouchInteractionMode.SingleFinger && 
                    (IsMultiUserModeActive == null || !IsMultiUserModeActive()))
                {
                    // Tự động giải phóng cờ khóa nếu đã quá 400ms kể từ khi nhấc tay xong hoặc chỉ còn <= 1 ngón
                    if (_suppressDrawingUntilAllReleased)
                    {
                        if ((DateTime.UtcNow - _lastGestureEndTime).TotalMilliseconds > 400 || _activeTouchScreenPoints.Count <= 1)
                        {
                            _suppressDrawingUntilAllReleased = false;
                        }
                        else
                        {
                            _canvas.CaptureTouch(e.TouchDevice);
                            e.Handled = true;
                            return;
                        }
                    }

                    // Khi có từ 2 điểm chạm trở lên trên màn hình:
                    if (_activeTouchScreenPoints.Count >= 2)
                    {
                        // 1. Rollback nét vẽ tạm của ngón 1 (Zero Ghost Ink)
                        if (_singleDrawingTouchId != -1 || _preliminaryStroke != null)
                        {
                            try
                            {
                                if (_preliminaryStroke != null && _canvas.Children.Contains(_preliminaryStroke))
                                {
                                    _canvas.Children.Remove(_preliminaryStroke);
                                }
                                if (_singleDrawingTouchId != -1)
                                {
                                    _touchManager.CompleteStroke(_singleDrawingTouchId);
                                }
                            }
                            catch { }
                            _preliminaryStroke = null;
                            _preliminaryTouchId = -1;
                            _singleDrawingTouchId = -1;
                        }

                        // 2. Kích hoạt Cử chỉ 2 ngón nếu chưa bật
                        if (!_isTwoFingerGestureActive)
                        {
                            var keys = _activeTouchScreenPoints.Keys.ToList();
                            int id1 = keys[0];
                            int id2 = keys[1];
                            Point pt1 = _activeTouchScreenPoints[id1];
                            Point pt2 = _activeTouchScreenPoints[id2];
                            double dist = Math.Max(10.0, (pt1 - pt2).Length);

                            _isTwoFingerGestureActive = true;
                            _gestureTouchId1 = id1;
                            _gestureTouchId2 = id2;
                            _gestureStartDistance = dist;
                            _gestureLastDistance = dist;
                            _gestureFilteredDistance = dist;
                            _gestureStartCenter = new Point((pt1.X + pt2.X) / 2.0, (pt1.Y + pt2.Y) / 2.0);
                            _gestureLastCenter = _gestureStartCenter;
                            _gestureMode = TwoFingerGestureMode.Pending;

                            Point canvasCenter = referenceElement.TranslatePoint(_gestureStartCenter, _canvas);
                            OnTwoFingerPinchPanStarted?.Invoke(canvasCenter);
                        }

                        _canvas.CaptureTouch(e.TouchDevice);
                        e.Handled = true;
                        return; // TUYỆT ĐỐI KHÔNG VẼ KHI CÓ 2+ NGÓN TAY Ở CHẾ ĐỘ ĐƠN ĐIỂM
                    }

                    // Nếu chỉ có đúng 1 ngón tay, ghi nhận ngón này là ngón vẽ duy nhất
                    _singleDrawingTouchId = touchId;
                }

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
                    // Nét vẽ khi đang chạm vẽ live nằm trên tầng ActiveStrokeLayer (3000)
                    Panel.SetZIndex(stroke, QASmartTouch.Helpers.ZIndexConstants.ActiveStrokeLayer);

                    // If this is touch 1 in SingleFinger mode, hold reference for ghost ink rollback if touch 2 arrives
                    if (_activeTouchScreenPoints.Count == 1 && CurrentMode == Managers.TouchInteractionMode.SingleFinger)
                    {
                        _preliminaryStroke = stroke;
                        _preliminaryTouchId = touchId;
                    }

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
                int touchId = e.TouchDevice.Id;

                // --- SINGLE FINGER / GESTURE MODE XỬ LÝ DI CHUYỂN ---
                if (CurrentMode == Managers.TouchInteractionMode.SingleFinger && 
                    (IsMultiUserModeActive == null || !IsMultiUserModeActive()))
                {
                    // 1. Đang trong Cử chỉ 2 ngón (Pinch-to-Zoom & Pan)
                    if (_isTwoFingerGestureActive)
                    {
                        if (touchId == _gestureTouchId1 || touchId == _gestureTouchId2)
                        {
                            UIElement referenceElement = _canvas.Parent as UIElement ?? _canvas;
                            Point screenPos = e.GetTouchPoint(referenceElement).Position;
                            _activeTouchScreenPoints[touchId] = screenPos;

                            if (_activeTouchScreenPoints.TryGetValue(_gestureTouchId1, out Point curPt1) &&
                                _activeTouchScreenPoints.TryGetValue(_gestureTouchId2, out Point curPt2))
                            {
                                double curDist = Math.Max(10.0, (curPt1 - curPt2).Length);
                                Point curCenter = new Point((curPt1.X + curPt2.X) / 2.0, (curPt1.Y + curPt2.Y) / 2.0);

                                Vector panStep = curCenter - _gestureLastCenter;
                                _gestureLastCenter = curCenter;

                                // Tính độ lệch tích lũy so với lúc bắt đầu cử chỉ
                                double totalDistDelta = Math.Abs(curDist - _gestureStartDistance);
                                double totalScaleRatio = _gestureStartDistance > 0 ? (curDist / _gestureStartDistance) : 1.0;
                                double totalCenterShift = (curCenter - _gestureStartCenter).Length;

                                // Phân loại ý định cử chỉ: Kéo di chuyển (Pan) vs Thu phóng (Pinch Zoom)
                                if (_gestureMode == TwoFingerGestureMode.Pending)
                                {
                                    // Nếu khoảng cách thay đổi rõ rệt (>= 22px hoặc >= 7%) -> Người dùng muốn Zoom
                                    if (totalDistDelta >= 22.0 || Math.Abs(totalScaleRatio - 1.0) >= 0.07)
                                    {
                                        _gestureMode = TwoFingerGestureMode.PinchZoom;
                                        _gestureFilteredDistance = curDist;
                                        _gestureLastDistance = curDist;
                                    }
                                    // Nếu trung tâm 2 ngón đã di chuyển (>= 8px) mà khoảng cách không đổi nhiều -> Khóa KÉO BẢNG THUẦN TÚY (Pan Only)
                                    else if (totalCenterShift >= 8.0)
                                    {
                                        _gestureMode = TwoFingerGestureMode.PanOnly;
                                    }
                                }
                                else if (_gestureMode == TwoFingerGestureMode.PanOnly)
                                {
                                    // Nếu đang kéo bảng nhưng người dùng mở rộng hoặc chụm ngón tay rất mạnh (>= 40px hoặc >= 15%)
                                    // thì mở khóa cho phép chuyển sang Zoom
                                    if (totalDistDelta >= 40.0 || Math.Abs(totalScaleRatio - 1.0) >= 0.15)
                                    {
                                        _gestureMode = TwoFingerGestureMode.PinchZoom;
                                        _gestureFilteredDistance = curDist;
                                        _gestureLastDistance = curDist;
                                    }
                                }

                                double scaleStep = 1.0; // Mặc định 1.0 (khóa zoom 100% khi Kéo bảng hoặc Pending)

                                if (_gestureMode == TwoFingerGestureMode.PinchZoom)
                                {
                                    // Lọc làm mượt EMA để triệt tiêu rung chấn của cảm biến màn hình tương tác
                                    _gestureFilteredDistance = 0.35 * curDist + 0.65 * _gestureFilteredDistance;
                                    if (_gestureLastDistance > 0)
                                    {
                                        scaleStep = _gestureFilteredDistance / _gestureLastDistance;
                                    }
                                    _gestureLastDistance = _gestureFilteredDistance;
                                }

                                Point canvasCenter = referenceElement.TranslatePoint(curCenter, _canvas);
                                OnTwoFingerPinchPan?.Invoke(canvasCenter, scaleStep, panStep);
                            }
                        }
                        e.Handled = true;
                        return;
                    }

                    // 2. Khóa an toàn sau khi kết thúc cử chỉ
                    if (_suppressDrawingUntilAllReleased)
                    {
                        e.Handled = true;
                        return;
                    }

                    // 3. RÀNG BUỘC TUYỆT ĐỐI CHẾ ĐỘ ĐƠN ĐIỂM: Chỉ ngón vẽ được cấp phép mới được vẽ
                    if (touchId != _singleDrawingTouchId)
                    {
                        e.Handled = true;
                        return;
                    }
                }

                var touchPoint = e.GetTouchPoint(_canvas);
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

                // Bỏ qua nếu touch bị cướp bởi 1 control khác (vd: Nút bấm).
                // Cho phép nếu được capture bởi Canvas hoặc bề mặt outerSurface (như ScrollViewer khi zoom nhỏ)
                if (e.TouchDevice.Captured != null && e.TouchDevice.Captured != _canvas && e.TouchDevice.Captured != _outerSurface) return;

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
                _activeTouchScreenPoints.Remove(touchId);

                if (CurrentMode == Managers.TouchInteractionMode.SingleFinger && 
                    (IsMultiUserModeActive == null || !IsMultiUserModeActive()))
                {
                    if (_isTwoFingerGestureActive)
                    {
                        _isTwoFingerGestureActive = false;
                        _suppressDrawingUntilAllReleased = true;
                        _gestureMode = TwoFingerGestureMode.Pending;
                        _lastGestureEndTime = DateTime.UtcNow;
                        OnTwoFingerPinchPanEnded?.Invoke();
                    }

                    if (touchId == _singleDrawingTouchId)
                    {
                        _singleDrawingTouchId = -1;
                        _preliminaryStroke = null;
                        _preliminaryTouchId = -1;
                    }

                    if (_activeTouchScreenPoints.Count == 0)
                    {
                        _suppressDrawingUntilAllReleased = false;
                        _gestureTouchId1 = -1;
                        _gestureTouchId2 = -1;
                        _singleDrawingTouchId = -1;
                        _gestureMode = TwoFingerGestureMode.Pending;
                    }
                }
                else
                {
                    if (_isTwoFingerGestureActive)
                    {
                        _isTwoFingerGestureActive = false;
                        _suppressDrawingUntilAllReleased = true;
                        _gestureMode = TwoFingerGestureMode.Pending;
                        OnTwoFingerPinchPanEnded?.Invoke();
                    }

                    if (_activeTouchScreenPoints.Count == 0)
                    {
                        _suppressDrawingUntilAllReleased = false;
                        _gestureTouchId1 = -1;
                        _gestureTouchId2 = -1;
                        _gestureMode = TwoFingerGestureMode.Pending;
                    }
                }

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

                int touchId = e.TouchDevice.Id;
                _activeTouchScreenPoints.Remove(touchId);

                if (CurrentMode == Managers.TouchInteractionMode.SingleFinger && 
                    (IsMultiUserModeActive == null || !IsMultiUserModeActive()))
                {
                    // Kết thúc Two-Finger Gesture nếu đang chạy
                    if (_isTwoFingerGestureActive)
                    {
                        _isTwoFingerGestureActive = false;
                        _suppressDrawingUntilAllReleased = true; // Lockout safety
                        _gestureMode = TwoFingerGestureMode.Pending;
                        _lastGestureEndTime = DateTime.UtcNow;
                        OnTwoFingerPinchPanEnded?.Invoke();

                        if (_activeTouchScreenPoints.Count == 0)
                        {
                            _suppressDrawingUntilAllReleased = false;
                            _gestureTouchId1 = -1;
                            _gestureTouchId2 = -1;
                            _singleDrawingTouchId = -1;
                        }
                        e.Handled = true;
                        return;
                    }

                    // Khóa an toàn: nếu ngón còn lại nhấc sau cử chỉ
                    if (_suppressDrawingUntilAllReleased)
                    {
                        if (_activeTouchScreenPoints.Count == 0)
                        {
                            _suppressDrawingUntilAllReleased = false;
                            _gestureTouchId1 = -1;
                            _gestureTouchId2 = -1;
                            _singleDrawingTouchId = -1;
                            _gestureMode = TwoFingerGestureMode.Pending;
                        }
                        e.Handled = true;
                        return;
                    }

                    if (touchId == _singleDrawingTouchId)
                    {
                        _singleDrawingTouchId = -1;
                    }
                }
                else
                {
                    // End Two-Finger Gesture if active (MultiFinger safety)
                    if (_isTwoFingerGestureActive)
                    {
                        _isTwoFingerGestureActive = false;
                        _suppressDrawingUntilAllReleased = true;
                        _gestureMode = TwoFingerGestureMode.Pending;
                        OnTwoFingerPinchPanEnded?.Invoke();

                        if (_activeTouchScreenPoints.Count == 0)
                        {
                            _suppressDrawingUntilAllReleased = false;
                            _gestureTouchId1 = -1;
                            _gestureTouchId2 = -1;
                        }
                        e.Handled = true;
                        return;
                    }

                    if (_suppressDrawingUntilAllReleased)
                    {
                        if (_activeTouchScreenPoints.Count == 0)
                        {
                            _suppressDrawingUntilAllReleased = false;
                            _gestureTouchId1 = -1;
                            _gestureTouchId2 = -1;
                            _gestureMode = TwoFingerGestureMode.Pending;
                        }
                        e.Handled = true;
                        return;
                    }
                }

                if (touchId == _preliminaryTouchId)
                {
                    _preliminaryStroke = null;
                    _preliminaryTouchId = -1;
                }

                if (_toolMode == TouchToolMode.None || 
                    (_toolMode == TouchToolMode.Eraser && _eraserMode == "ClearAll"))
                    return;
                if (e.TouchDevice.Captured != _canvas) return;

                if (_toolMode == TouchToolMode.Drawing)
                {
                    // --- DRAWING MODE: complete stroke and convert to Bezier ---
                    var stroke = _touchManager.CompleteStroke(touchId);

                    if (stroke != null)
                    {
                        // ✨ DOT FALLBACK: Đảm bảo nếu stroke chỉ có 1 điểm duy nhất thì luôn thêm điểm vi mô thứ 2
                        if (stroke.Points.Count == 1)
                        {
                            Point pt = stroke.Points[0];
                            stroke.Points.Add(new Point(pt.X + 0.01, pt.Y));
                        }

                        if (_currentBrushType == "Laser")
                        {
                            // Bút laser: Tự động tan biến sau 2.5s, không ghi Undo stack
                            AnimateAndRemoveLaserStroke(stroke);
                        }
                        else if (_currentBrushType == "Shape" || _currentBrushType == "Calligraphy")
                        {
                            int inkingZ = GetCurrentInkingZIndex?.Invoke() ?? QASmartTouch.Helpers.ZIndexConstants.UserContentBase;
                            Color color = stroke.Stroke is SolidColorBrush scb ? scb.Color : _currentPenColor;
                            var recognizedShape = QASmartTouch.Helpers.ShapeRecognizer.TryRecognizeShape(stroke, color, stroke.StrokeThickness);
                            if (recognizedShape != null)
                            {
                                _canvas.Children.Remove(stroke);
                                Panel.SetZIndex(recognizedShape, inkingZ);
                                _canvas.Children.Add(recognizedShape);
                                _recordAddAction?.Invoke(recognizedShape, $"Touch recognized shape (ID: {touchId})");
                            }
                            else
                            {
                                var smoothPath = _strokeService.ConvertToSmoothPath(stroke);
                                if (smoothPath != null)
                                {
                                    _canvas.Children.Remove(stroke);
                                    _canvas.Children.Add(smoothPath);
                                    Panel.SetZIndex(smoothPath, inkingZ);
                                    _recordAddAction?.Invoke(smoothPath, $"Touch draw (ID: {touchId})");
                                }
                                else
                                {
                                    Panel.SetZIndex(stroke, inkingZ);
                                    _recordAddAction?.Invoke(stroke, $"Touch draw (ID: {touchId})");
                                }
                            }
                        }
                        else
                        {
                            int currentInkingZ = GetCurrentInkingZIndex?.Invoke() ?? QASmartTouch.Helpers.ZIndexConstants.UserContentBase;
                            var smoothPath = _strokeService.ConvertToSmoothPath(stroke);
                            int zIndex = (_currentBrushType == "Highlighter" || _currentBrushType == "Marker" || _currentBrushType == "Mask" || _currentBrushType == "MaskPen")
                                ? (currentInkingZ > QASmartTouch.Helpers.ZIndexConstants.UserContentBase ? currentInkingZ : QASmartTouch.Helpers.ZIndexConstants.HighlighterLayer)
                                : currentInkingZ;

                            if (smoothPath != null)
                            {
                                _canvas.Children.Remove(stroke);
                                _canvas.Children.Add(smoothPath);
                                // Nét vẽ hoàn thành đưa về tầng nội dung chuẩn
                                Panel.SetZIndex(smoothPath, zIndex);
                                _recordAddAction?.Invoke(smoothPath, $"Touch draw (ID: {touchId})");
                                System.Diagnostics.Debug.WriteLine($"✨ Touch {touchId}: Polyline → SmoothPath ({stroke.Points.Count} pts)");
                            }
                            else
                            {
                                Panel.SetZIndex(stroke, zIndex);
                                _recordAddAction?.Invoke(stroke, $"Touch draw (ID: {touchId})");
                            }
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
                case "Normal" or "Simple" or "AI":
                    // Standard smooth pen
                    stroke.StrokeLineJoin = PenLineJoin.Round;
                    stroke.StrokeStartLineCap = PenLineCap.Round;
                    stroke.StrokeEndLineCap = PenLineCap.Round;
                    stroke.StrokeThickness = _currentPenSize;
                    break;

                case "Calligraphy" or "Hoc" or "Shape":
                    // Educational calligraphy - thicker, round caps
                    stroke.StrokeLineJoin = PenLineJoin.Round;
                    stroke.StrokeStartLineCap = PenLineCap.Round;
                    stroke.StrokeEndLineCap = PenLineCap.Round;
                    stroke.StrokeThickness = _currentPenSize * 1.25;
                    break;

                case "Highlighter" or "Marker" or "Mask" or "MaskPen":
                    // Highlighter - semi-transparent, flat ends, wider
                    stroke.StrokeLineJoin = PenLineJoin.Miter;
                    stroke.StrokeStartLineCap = PenLineCap.Flat;
                    stroke.StrokeEndLineCap = PenLineCap.Flat;
                    stroke.Opacity = 0.4;
                    stroke.StrokeThickness = Math.Max(_currentPenSize * 2.2, 10);
                    break;

                case "Laser":
                    // Laser pen - vibrant glowing indicator
                    stroke.StrokeLineJoin = PenLineJoin.Round;
                    stroke.StrokeStartLineCap = PenLineCap.Round;
                    stroke.StrokeEndLineCap = PenLineCap.Round;
                    stroke.Opacity = 0.95;
                    stroke.StrokeThickness = Math.Max(_currentPenSize * 1.3, 4);
                    if (_currentPenColor == Colors.Black || _currentPenColor == Colors.White)
                    {
                        stroke.Stroke = new SolidColorBrush(Color.FromRgb(255, 59, 48));
                    }
                    break;

                default:
                    // Fallback to normal
                    stroke.StrokeLineJoin = PenLineJoin.Round;
                    stroke.StrokeStartLineCap = PenLineCap.Round;
                    stroke.StrokeEndLineCap = PenLineCap.Round;
                    stroke.StrokeThickness = _currentPenSize;
                    break;
            }
        }

        /// <summary>
        /// Tự động làm mờ và giải phóng nét bút laser sau 2.5 giây
        /// </summary>
        private void AnimateAndRemoveLaserStroke(UIElement stroke)
        {
            if (stroke == null) return;
            
            var anim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = stroke.Opacity,
                To = 0.0,
                BeginTime = TimeSpan.FromSeconds(1.5),
                Duration = TimeSpan.FromSeconds(1.0),
                FillBehavior = System.Windows.Media.Animation.FillBehavior.Stop
            };
            
            anim.Completed += (s, e) =>
            {
                try
                {
                    if (_canvas.Children.Contains(stroke))
                    {
                        _canvas.Children.Remove(stroke);
                    }
                }
                catch { }
            };
            
            stroke.BeginAnimation(UIElement.OpacityProperty, anim);
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
