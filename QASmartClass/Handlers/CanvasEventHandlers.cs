using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartTouch.Managers;
using QASmartTouch.Services;

namespace QASmartTouch.Handlers
{
    /// <summary>
    /// Handles all canvas mouse and touch events
    /// Responsibilities:
    /// - Route canvas events to appropriate managers
    /// - Handle drawing, erasing, selection, zoom area events
    /// - Coordinate between different tool modes
    /// - Manage mouse capture and cursor states
    /// </summary>
    public class CanvasEventHandlers
    {
        #region Fields

        private readonly Canvas _canvas;
        private readonly DrawingEngine _drawingEngine;
        private readonly EraserEngine _eraserEngine;
        private readonly ZoomManager _zoomManager;
        private readonly SelectionManager _selectionManager;
        private readonly UIStateManager _uiStateManager;

        // Current tool mode
        private string _currentMode = "None"; // None, Drawing, Eraser, Selection, ZoomArea, Pan

        // BUG-1602: Fields cho chế độ "Xóa theo vùng kéo" (Drag Erase)
        // Lưu điểm bắt đầu kéo và hình chữ nhật preview nét đứt đỏ
        private Point? _dragEraseStartPoint;
        private Rectangle? _dragErasePreviewRect;

        #endregion

        #region Events

        /// <summary>
        /// Fired when canvas is clicked (for general purposes)
        /// </summary>
        public event EventHandler<Point> OnCanvasClicked;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize CanvasEventHandlers
        /// </summary>
        public CanvasEventHandlers(
            Canvas canvas,
            DrawingEngine drawingEngine,
            EraserEngine eraserEngine,
            ZoomManager zoomManager,
            SelectionManager selectionManager,
            UIStateManager uiStateManager)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _drawingEngine = drawingEngine;
            _eraserEngine = eraserEngine;
            _zoomManager = zoomManager;
            _selectionManager = selectionManager;
            _uiStateManager = uiStateManager;

            // Wire up canvas events
            _canvas.MouseDown += Canvas_MouseDown;
            _canvas.MouseMove += Canvas_MouseMove;
            _canvas.MouseUp += Canvas_MouseUp;
            _canvas.MouseEnter += Canvas_MouseEnter;
            _canvas.MouseLeave += Canvas_MouseLeave;

            // IMP-1604: Kích hoạt hỗ trợ cảm ứng đa điểm (Multi-touch Manipulation)
            // Cho phép pinch-zoom, xoay và di chuyển đối tượng bằng 2 ngón tay trên IFP
            _canvas.IsManipulationEnabled = true;
            _canvas.ManipulationStarting += Canvas_ManipulationStarting;
            _canvas.ManipulationDelta += Canvas_ManipulationDelta;
            _canvas.ManipulationCompleted += Canvas_ManipulationCompleted;

            System.Diagnostics.Debug.WriteLine("✅ CanvasEventHandlers initialized (multi-touch enabled)");
        }

        #endregion

        #region Public Methods - Mode Management

        /// <summary>
        /// Set current interaction mode
        /// </summary>
        public void SetMode(string mode)
        {
            _currentMode = mode;
            System.Diagnostics.Debug.WriteLine($"🔧 Canvas mode set to: {mode}");
        }

        /// <summary>
        /// Get current mode
        /// </summary>
        public string GetCurrentMode()
        {
            return _currentMode;
        }

        #endregion

        #region Event Handlers - Mouse Events

        /// <summary>
        /// Handle canvas mouse down event
        /// </summary>
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            Point clickPoint = e.GetPosition(_canvas);

            // Hide welcome state when user starts interacting
            _uiStateManager?.HideWelcomeState();

            // Fire general canvas clicked event
            OnCanvasClicked?.Invoke(this, clickPoint);

            // BUG-1601: Khi đang ở chế độ Eraser, chặn tất cả control con
            // (TextBox, Button...) nhận focus/handle sự kiện.
            // Ngăn Routed Event lan truyền xuống TextBox để TextBox không
            // chiếm giữ sự kiện cảm ứng khi GV đang muốn tẩy.
            // ✅ QC_4.2_TABLE_ERASER_CONTROL_FIX (T7): Cho phép nút Bảng (✏️🖐️❌)
            // hoạt động ngay cả khi Eraser mode bật
            if (_currentMode == "Eraser")
            {
                if (!QASmartTouch.Utilities.InputValidationHelper.IsEventFromInteractiveControl(
                        e.OriginalSource, _canvas))
                {
                    e.Handled = true;
                }
            }

            // Route to appropriate handler based on current mode
            switch (_currentMode)
            {
                case "Drawing":
                    HandleDrawingMouseDown(clickPoint, e);
                    break;

                case "Eraser":
                    HandleEraserMouseDown(clickPoint, e);
                    break;

                case "Selection":
                    HandleSelectionMouseDown(clickPoint, e);
                    break;

                case "ZoomArea":
                    HandleZoomAreaMouseDown(clickPoint, e);
                    break;

                case "Pan":
                    HandlePanMouseDown(clickPoint, e);
                    break;

                default:
                    System.Diagnostics.Debug.WriteLine($"⚠️ No handler for mode: {_currentMode}");
                    break;
            }
        }

        /// <summary>
        /// Handle canvas mouse move event
        /// </summary>
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point currentPoint = e.GetPosition(_canvas);

            // Route to appropriate handler based on current mode
            switch (_currentMode)
            {
                case "Drawing":
                    HandleDrawingMouseMove(currentPoint, e);
                    break;

                case "Eraser":
                    HandleEraserMouseMove(currentPoint, e);
                    break;

                case "Selection":
                    HandleSelectionMouseMove(currentPoint, e);
                    break;

                case "ZoomArea":
                    HandleZoomAreaMouseMove(currentPoint, e);
                    break;

                case "Pan":
                    HandlePanMouseMove(currentPoint, e);
                    break;
            }
        }

        /// <summary>
        /// Handle canvas mouse up event
        /// </summary>
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Point releasePoint = e.GetPosition(_canvas);

            // Route to appropriate handler based on current mode
            switch (_currentMode)
            {
                case "Drawing":
                    HandleDrawingMouseUp(releasePoint, e);
                    break;

                case "Eraser":
                    HandleEraserMouseUp(releasePoint, e);
                    break;

                case "Selection":
                    HandleSelectionMouseUp(releasePoint, e);
                    break;

                case "ZoomArea":
                    HandleZoomAreaMouseUp(releasePoint, e);
                    break;

                case "Pan":
                    HandlePanMouseUp(releasePoint, e);
                    break;
            }
        }

        /// <summary>
        /// Handle canvas mouse enter event
        /// </summary>
        private void Canvas_MouseEnter(object sender, MouseEventArgs e)
        {
            // Show eraser preview when entering canvas in eraser mode
            if (_currentMode == "Eraser" && _eraserEngine != null)
            {
                Point point = e.GetPosition(_canvas);
                _eraserEngine.ShowEraserPreview(point);
            }
        }

        /// <summary>
        /// Handle canvas mouse leave event
        /// </summary>
        private void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            // Hide eraser preview when leaving canvas
            if (_currentMode == "Eraser" && _eraserEngine != null)
            {
                _eraserEngine.HideEraserPreview();
            }
        }

        #endregion

        #region Private Methods - Drawing Mode Handlers

        private void HandleDrawingMouseDown(Point point, MouseButtonEventArgs e)
        {
            if (_drawingEngine == null)
                return;

            _drawingEngine.StartDrawing(point);
            _canvas.CaptureMouse();
            System.Diagnostics.Debug.WriteLine($"🖊️ Drawing started at ({point.X:F0}, {point.Y:F0})");
        }

        private void HandleDrawingMouseMove(Point point, MouseEventArgs e)
        {
            if (_drawingEngine == null || e.LeftButton != MouseButtonState.Pressed)
                return;

            if (_drawingEngine.IsDrawing)
            {
                _drawingEngine.ContinueDrawing(point);
            }
        }

        private void HandleDrawingMouseUp(Point point, MouseButtonEventArgs e)
        {
            if (_drawingEngine == null)
                return;

            if (_drawingEngine.IsDrawing)
            {
                _drawingEngine.EndDrawing();
                _canvas.ReleaseMouseCapture();
                System.Diagnostics.Debug.WriteLine("🖊️ Drawing ended");
            }
        }

        #endregion

        // ═══════════════════════════════════════════════════════
        // ENGINE A: SMART TOUCH MODULE — Mouse Eraser
        // Thuật toán: VisualTreeHelper.HitTest với EllipseGeometry (25px)
        // Reference: eraser_tool_specification.md v2.1 Mục III.A
        // ═══════════════════════════════════════════════════════
        #region Private Methods - Eraser Mode Handlers

        private void HandleEraserMouseDown(Point point, MouseButtonEventArgs e)
        {
            if (_eraserEngine == null)
                return;

            if (_eraserEngine.EraserMode == EraserMode.Drag)
            {
                // BUG-1602: Drag mode — lưu điểm bắt đầu và tạo preview rectangle nét đứt đỏ
                _canvas.CaptureMouse();
                _dragEraseStartPoint = point;
                
                // Tạo khung preview hình chữ nhật nét đứt màu đỏ
                _dragErasePreviewRect = new Rectangle
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 4 },
                    Fill = new SolidColorBrush(Color.FromArgb(25, 255, 0, 0)), // Đỏ mờ 10%
                    IsHitTestVisible = false,
                    Width = 0,
                    Height = 0
                };
                Canvas.SetLeft(_dragErasePreviewRect, point.X);
                Canvas.SetTop(_dragErasePreviewRect, point.Y);
                Canvas.SetZIndex(_dragErasePreviewRect, 9999); // Ngay dưới eraser preview
                _canvas.Children.Add(_dragErasePreviewRect);
                
                System.Diagnostics.Debug.WriteLine(
                    $"🧹 BUG-1602: Drag erase started at ({point.X:F0}, {point.Y:F0})");
            }
            else if (_eraserEngine.EraserMode == EraserMode.Stroke)
            {
                // Stroke mode - capture mouse so we can erase while dragging
                _canvas.CaptureMouse();

                // Erase stroke under cursor at initial click point
                EraseStrokeAtPoint(point);
                System.Diagnostics.Debug.WriteLine($"🧹 Eraser stroke started at ({point.X:F0}, {point.Y:F0})");
            }
        }

        private void HandleEraserMouseMove(Point point, MouseEventArgs e)
        {
            if (_eraserEngine == null)
                return;

            // Update eraser preview position (always)
            _eraserEngine.UpdateEraserPreview(point);

            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            if (_eraserEngine.EraserMode == EraserMode.Drag)
            {
                // BUG-1602: Drag mode — cập nhật kích thước và vị trí khung preview
                // KHÔNG gọi EraseByDrag ở đây! Chỉ vẽ khung preview.
                if (_dragEraseStartPoint.HasValue && _dragErasePreviewRect != null)
                {
                    var start = _dragEraseStartPoint.Value;
                    double x = Math.Min(start.X, point.X);
                    double y = Math.Min(start.Y, point.Y);
                    double width = Math.Abs(point.X - start.X);
                    double height = Math.Abs(point.Y - start.Y);
                    
                    Canvas.SetLeft(_dragErasePreviewRect, x);
                    Canvas.SetTop(_dragErasePreviewRect, y);
                    _dragErasePreviewRect.Width = width;
                    _dragErasePreviewRect.Height = height;
                }
            }
            else if (_eraserEngine.EraserMode == EraserMode.Stroke)
            {
                // Stroke mode: erase entire stroke wherever cursor moves (hold & drag)
                EraseStrokeAtPoint(point);
            }
        }

        private void HandleEraserMouseUp(Point point, MouseButtonEventArgs e)
        {
            if (_eraserEngine == null)
                return;

            if (_eraserEngine.EraserMode == EraserMode.Drag)
            {
                // BUG-1602: Drag mode — tính Rect cuối cùng và gọi EraseByDrag MỘT LẦN
                if (_dragEraseStartPoint.HasValue)
                {
                    var start = _dragEraseStartPoint.Value;
                    double x = Math.Min(start.X, point.X);
                    double y = Math.Min(start.Y, point.Y);
                    double width = Math.Abs(point.X - start.X);
                    double height = Math.Abs(point.Y - start.Y);
                    
                    // Chỉ xóa nếu vùng kéo đủ lớn (tránh click đơn xóa nhầm)
                    if (width > 5 && height > 5)
                    {
                        var finalRect = new Rect(x, y, width, height);
                        var erased = _eraserEngine.EraseByDrag(finalRect);
                        System.Diagnostics.Debug.WriteLine(
                            $"🧹 BUG-1602: Drag erase completed — {erased.Count} elements erased in rect ({x:F0},{y:F0},{width:F0},{height:F0})");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "🧹 BUG-1602: Drag erase cancelled — area too small (< 5x5px)");
                    }
                }
                
                // Xóa khung preview ra khỏi Canvas
                if (_dragErasePreviewRect != null)
                {
                    _canvas.Children.Remove(_dragErasePreviewRect);
                    _dragErasePreviewRect = null;
                }
                _dragEraseStartPoint = null;
                
                _canvas.ReleaseMouseCapture();
                System.Diagnostics.Debug.WriteLine("🧹 BUG-1602: Drag erase ended");
            }
            else if (_eraserEngine.EraserMode == EraserMode.Stroke)
            {
                _canvas.ReleaseMouseCapture();
                System.Diagnostics.Debug.WriteLine("🧹 Eraser stroke ended");
            }
        }

        /// <summary>
        /// BUG-1601 REWRITE: Tẩy đối tượng tại điểm chạm.
        /// Sử dụng VisualTreeHelper.HitTest với EllipseGeometry bán kính 25px để:
        /// 1. Phát hiện Shape rỗng (nhờ Fill ẩn đã gán ở EraserEngine)
        /// 2. Phát hiện TextBlock/TextBox/Border (HitTest ưu tiên hơn Routed Event)
        /// 3. Phát hiện nét vẽ mỏng (nhờ bán kính 25px)
        /// </summary>
        private void EraseStrokeAtPoint(Point point)
        {
            UIElement? toErase = null;

            // BUG-1601: Tăng bán kính dung sai từ 10→25px cho màn hình IFP lớn
            const double ERASER_HIT_RADIUS = 25;
            var hitGeometry = new EllipseGeometry(point, ERASER_HIT_RADIUS, ERASER_HIT_RADIUS);

            VisualTreeHelper.HitTest(
                _canvas,
                filterCallback: (potentialHit) =>
                {
                    // BUG-1601: Bỏ qua eraser preview và các system elements
                    if (potentialHit is UIElement uie && _eraserEngine != null)
                    {
                        // Không HitTest vào chính eraser preview
                        if (uie == _eraserEngine.GetEraserPreview())
                            return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
                    }
                    return HitTestFilterBehavior.Continue;
                },
                resultCallback: result =>
                {
                    // Walk up visual tree to find the direct canvas child
                    var directChild = GetDirectCanvasChild(result.VisualHit as DependencyObject);
                    if (directChild != null)
                    {
                        toErase = directChild;
                        return HitTestResultBehavior.Stop;
                    }
                    return HitTestResultBehavior.Continue;
                },
                hitTestParameters: new GeometryHitTestParameters(hitGeometry));

            if (toErase != null)
            {
                _eraserEngine.EraseByStroke(toErase);
                System.Diagnostics.Debug.WriteLine(
                    $"🧹 BUG-1601: Erased {toErase.GetType().Name} at ({point.X:F0},{point.Y:F0})");
            }
        }

        /// <summary>
        /// Walk up visual tree to find the direct child of the canvas.
        /// Needed because HitTest may return a sub-element (e.g. PathGeometry)
        /// but we need the top-level canvas child to remove.
        /// </summary>
        private UIElement? GetDirectCanvasChild(DependencyObject? element)
        {
            if (element == null || element == _canvas)
                return null;

            var parent = VisualTreeHelper.GetParent(element);

            // Found: this element's parent IS the canvas
            if (parent == _canvas && element is UIElement uiElement)
                return uiElement;

            // Recurse up
            return GetDirectCanvasChild(parent);
        }

        #endregion

        #region Private Methods - Selection Mode Handlers

        private void HandleSelectionMouseDown(Point point, MouseButtonEventArgs e)
        {
            if (_selectionManager == null)
                return;

            bool isCtrlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            _selectionManager.HandleMouseDown(point, isCtrlPressed);
            
            System.Diagnostics.Debug.WriteLine($"🎯 Selection click at ({point.X:F0}, {point.Y:F0}), Ctrl={isCtrlPressed}");
        }

        private void HandleSelectionMouseMove(Point point, MouseEventArgs e)
        {
            if (_selectionManager == null)
                return;

            // Selection manager handles its own mouse move logic
            // This could be extended to handle drag operations
        }

        private void HandleSelectionMouseUp(Point point, MouseButtonEventArgs e)
        {
            if (_selectionManager == null)
                return;

            // Selection manager handles its own mouse up logic
            System.Diagnostics.Debug.WriteLine("🎯 Selection mouse up");
        }

        #endregion

        #region Private Methods - Zoom Area Mode Handlers

        private Point _zoomAreaStartPoint;
        private bool _isSelectingZoomArea = false;

        private void HandleZoomAreaMouseDown(Point point, MouseButtonEventArgs e)
        {
            _zoomAreaStartPoint = point;
            _isSelectingZoomArea = true;
            _canvas.CaptureMouse();
            _canvas.Cursor = Cursors.Cross;
            
            System.Diagnostics.Debug.WriteLine($"🔍 Zoom area selection started at ({point.X:F0}, {point.Y:F0})");
        }

        private void HandleZoomAreaMouseMove(Point point, MouseEventArgs e)
        {
            if (!_isSelectingZoomArea || e.LeftButton != MouseButtonState.Pressed)
                return;

            // Update zoom area preview (this would need to be implemented)
            // For now, just log the current selection
            double width = Math.Abs(point.X - _zoomAreaStartPoint.X);
            double height = Math.Abs(point.Y - _zoomAreaStartPoint.Y);
            
            if (width > 10 && height > 10) // Only log if area is significant
            {
                System.Diagnostics.Debug.WriteLine($"🔍 Zoom area: {width:F0}x{height:F0}");
            }
        }

        private void HandleZoomAreaMouseUp(Point point, MouseButtonEventArgs e)
        {
            if (!_isSelectingZoomArea)
                return;

            _isSelectingZoomArea = false;
            _canvas.ReleaseMouseCapture();
            _canvas.Cursor = Cursors.Arrow;

            // Calculate selected area
            double left = Math.Min(_zoomAreaStartPoint.X, point.X);
            double top = Math.Min(_zoomAreaStartPoint.Y, point.Y);
            double width = Math.Abs(point.X - _zoomAreaStartPoint.X);
            double height = Math.Abs(point.Y - _zoomAreaStartPoint.Y);

            // Only zoom if area is large enough
            if (width > 50 && height > 50)
            {
                System.Diagnostics.Debug.WriteLine($"🔍 Zoom to area: ({left:F0}, {top:F0}) {width:F0}x{height:F0}");
                // This would call a zoom to area method on the zoom manager
                // _zoomManager?.ZoomToArea(left, top, width, height);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("🔍 Zoom area too small, cancelled");
            }
        }

        #endregion

        #region Private Methods - Pan Mode Handlers

        private void HandlePanMouseDown(Point point, MouseButtonEventArgs e)
        {
            if (_zoomManager == null)
                return;

            Point windowPoint = e.GetPosition(_canvas.Parent as UIElement);
            _zoomManager.StartPan(windowPoint);
            System.Diagnostics.Debug.WriteLine($"🖐️ Pan started at ({windowPoint.X:F0}, {windowPoint.Y:F0})");
        }

        private void HandlePanMouseMove(Point point, MouseEventArgs e)
        {
            if (_zoomManager == null || e.LeftButton != MouseButtonState.Pressed)
                return;

            if (_zoomManager.IsPanning)
            {
                Point windowPoint = e.GetPosition(_canvas.Parent as UIElement);
                _zoomManager.ContinuePan(windowPoint);
            }
        }

        private void HandlePanMouseUp(Point point, MouseButtonEventArgs e)
        {
            if (_zoomManager == null)
                return;

            _zoomManager.EndPan();
            System.Diagnostics.Debug.WriteLine("🖐️ Pan ended");
        }

        #endregion

        #region IMP-1604: Multi-touch Manipulation Handlers

        /// <summary>
        /// IMP-1604: Thiết lập container và chế độ manipulation khi bắt đầu chạm đa điểm.
        /// </summary>
        private void Canvas_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            // Chỉ cho phép multi-touch khi đang ở chế độ Selection
            if (_currentMode != "Selection")
            {
                e.Cancel();
                return;
            }

            // Thiết lập container là Canvas cha
            e.ManipulationContainer = _canvas;
            
            // Cho phép tất cả thao tác: zoom, xoay, dịch chuyển
            e.Mode = ManipulationModes.All;
            
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine("\U0001f91a IMP-1604: Multi-touch manipulation starting");
        }

        /// <summary>
        /// IMP-1604: Xử lý thao tác đa điểm (pinch-zoom, xoay, di chuyển).
        /// Áp dụng Scale, Rotation, Translation vào các đối tượng đang được chọn.
        /// </summary>
        private void Canvas_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (_selectionManager == null || _currentMode != "Selection")
                return;

            var selectedObjects = _selectionManager.GetSelectedObjects();
            if (selectedObjects == null || selectedObjects.Count == 0)
                return;

            var delta = e.DeltaManipulation;
            
            foreach (var selObj in selectedObjects)
            {
                if (selObj.Element == null || selObj.IsLocked)
                    continue;

                var element = selObj.Element as FrameworkElement;
                if (element == null)
                    continue;

                // === 1. Di chuyển (Translation) ===
                double currentLeft = Canvas.GetLeft(element);
                double currentTop = Canvas.GetTop(element);
                if (double.IsNaN(currentLeft)) currentLeft = 0;
                if (double.IsNaN(currentTop)) currentTop = 0;
                
                Canvas.SetLeft(element, currentLeft + delta.Translation.X);
                Canvas.SetTop(element, currentTop + delta.Translation.Y);

                // === 2. Co giãn (Scale) và Xoay (Rotation) ===
                // Lấy hoặc tạo TransformGroup cho element
                TransformGroup transformGroup;
                if (element.RenderTransform is TransformGroup existingGroup)
                {
                    transformGroup = existingGroup;
                }
                else
                {
                    transformGroup = new TransformGroup();
                    if (element.RenderTransform != null && element.RenderTransform != Transform.Identity)
                    {
                        transformGroup.Children.Add(element.RenderTransform);
                    }
                    element.RenderTransform = transformGroup;
                }

                // Tìm hoặc tạo ScaleTransform
                var scaleTransform = FindOrAddTransform<ScaleTransform>(transformGroup);
                scaleTransform.ScaleX *= delta.Scale.X;
                scaleTransform.ScaleY *= delta.Scale.Y;

                // Tìm hoặc tạo RotateTransform
                var rotateTransform = FindOrAddTransform<RotateTransform>(transformGroup);
                rotateTransform.Angle += delta.Rotation;

                // Đặt tâm xoay/zoom tại trung tâm đối tượng
                double centerX = element.ActualWidth / 2;
                double centerY = element.ActualHeight / 2;
                scaleTransform.CenterX = centerX;
                scaleTransform.CenterY = centerY;
                rotateTransform.CenterX = centerX;
                rotateTransform.CenterY = centerY;

                // === 3. Cập nhật SelectableObject ===
                selObj.Position = new Point(
                    currentLeft + delta.Translation.X,
                    currentTop + delta.Translation.Y);
                selObj.RotationAngle = rotateTransform.Angle;
                selObj.Scale = new ScaleTransform(scaleTransform.ScaleX, scaleTransform.ScaleY);
                selObj.UpdateBounds();
            }

            e.Handled = true;
        }

        /// <summary>
        /// IMP-1604: Kết thúc thao tác đa điểm.
        /// </summary>
        private void Canvas_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (_currentMode != "Selection")
                return;

            System.Diagnostics.Debug.WriteLine(
                $"\U0001f91a IMP-1604: Manipulation completed " +
                $"TotalTranslation=({e.TotalManipulation.Translation.X:F0},{e.TotalManipulation.Translation.Y:F0}) " +
                $"TotalScale=({e.TotalManipulation.Scale.X:F2},{e.TotalManipulation.Scale.Y:F2}) " +
                $"TotalRotation={e.TotalManipulation.Rotation:F1}°");
            
            // Cập nhật lại adorner cho các đối tượng đang chọn
            _selectionManager?.RefreshSelectionAdorner();
            
            e.Handled = true;
        }

        /// <summary>
        /// IMP-1604: Tìm hoặc thêm mới một loại Transform trong TransformGroup.
        /// </summary>
        private T FindOrAddTransform<T>(TransformGroup group) where T : Transform, new()
        {
            foreach (var child in group.Children)
            {
                if (child is T found)
                    return found;
            }
            var newTransform = new T();
            group.Children.Add(newTransform);
            return newTransform;
        }

        #endregion

        #region Public Methods - Cleanup

        /// <summary>
        /// Cleanup and unsubscribe from events
        /// </summary>
        public void Cleanup()
        {
            _canvas.MouseDown -= Canvas_MouseDown;
            _canvas.MouseMove -= Canvas_MouseMove;
            _canvas.MouseUp -= Canvas_MouseUp;
            _canvas.MouseEnter -= Canvas_MouseEnter;
            _canvas.MouseLeave -= Canvas_MouseLeave;

            // IMP-1604: Unsubscribe multi-touch events
            _canvas.ManipulationStarting -= Canvas_ManipulationStarting;
            _canvas.ManipulationDelta -= Canvas_ManipulationDelta;
            _canvas.ManipulationCompleted -= Canvas_ManipulationCompleted;

            System.Diagnostics.Debug.WriteLine("🧹 CanvasEventHandlers cleaned up");
        }

        #endregion
    }
}
