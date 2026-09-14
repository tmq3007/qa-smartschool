using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartTouch.Models;
using QASmartTouch.Services;

namespace QASmartTouch.Controls
{
    /// <summary>
    /// Selection Box với 4 chốt vuông, Live Scale Tooltip, Smart Angle Snapping và Multi-touch Manipulation
    /// </summary>
    public partial class SelectionBox : UserControl
    {
        #region Fields

        private SelectableObject? _attachedObject;
        private TransformService _transformService;
        private Models.ResizeMode _currentResizeMode = Models.ResizeMode.None;
        private Point _dragStartPoint;
        private Point _lastMovePoint;
        private Point _originalPosition;
        private Size _originalSize;
        private Point _rotationCenter;
        private double _startPointerAngle;
        private double _initialObjectAngle;
        private System.Collections.Generic.Dictionary<SelectableObject, (Point pos, Size size)> _memberSnapshots = new();
        private bool _isDragging = false;
        private bool _isRotating = false;
        private bool _isMoving = false; // [BUG_DRAG_MOVE] Drag-to-move state
        private bool _isManipulating = false;
        private int _activeResizeTouchId = -1;
        private int _activeRotateTouchId = -1;

        private System.Collections.Generic.Dictionary<SelectableObject, ElementTransformState> _activeTransformInitialStates = new();

        /// <summary>
        /// True nếu đang trong quá trình kéo chốt resize hoặc xoay
        /// </summary>
        public bool IsTransforming => _isDragging || _isRotating || _isMoving || _isManipulating;

        /// <summary>
        /// True nếu đang trong quá trình kéo chốt xoay (Rotate)
        /// </summary>
        public bool IsRotating => _isRotating;

        /// <summary>
        /// True nếu đang trong quá trình kéo chốt co dãn (Resize)
        /// </summary>
        public bool IsDragging => _isDragging;

        /// <summary>
        /// Object đang được attach bởi SelectionBox (Object đơn hoặc Group Object)
        /// </summary>
        public SelectableObject? AttachedObject => _attachedObject;

        #endregion

        #region Transform Snapshot Helpers

        /// <summary>
        /// Chụp lại snapshot trạng thái hình học hiện tại của đối tượng đang attach (hoặc toàn bộ thành viên nhóm)
        /// </summary>
        private System.Collections.Generic.Dictionary<SelectableObject, ElementTransformState> CaptureCurrentTransformStates()
        {
            var states = new System.Collections.Generic.Dictionary<SelectableObject, ElementTransformState>();
            if (_attachedObject == null) return states;

            if (_attachedObject.IsGroup || _attachedObject.Type == ObjectType.Group || (_attachedObject.GroupMembers != null && _attachedObject.GroupMembers.Count > 0))
            {
                foreach (var member in _attachedObject.GroupMembers)
                {
                    if (member != null)
                    {
                        states[member] = ElementTransformState.Create(member);
                    }
                }
            }
            else
            {
                states[_attachedObject] = ElementTransformState.Create(_attachedObject);
            }
            return states;
        }

        /// <summary>
        /// Hoàn tất thao tác biến đổi, so sánh snapshot ban đầu và kết thúc. Nếu có sự thay đổi thì phát sinh event TransformCompleted
        /// </summary>
        private void FinishTransformOperation(string description)
        {
            if (_attachedObject == null || _activeTransformInitialStates == null || _activeTransformInitialStates.Count == 0)
            {
                _activeTransformInitialStates?.Clear();
                return;
            }

            var finalStates = CaptureCurrentTransformStates();
            bool hasChanges = false;

            foreach (var kvp in _activeTransformInitialStates)
            {
                var obj = kvp.Key;
                var oldState = kvp.Value;
                if (finalStates.TryGetValue(obj, out var newState))
                {
                    if (oldState.IsDifferentFrom(newState))
                    {
                        hasChanges = true;
                        break;
                    }
                }
                else
                {
                    hasChanges = true;
                    break;
                }
            }

            if (hasChanges)
            {
                TransformCompleted?.Invoke(this, new TransformCompletedEventArgs
                {
                    TargetObject = _attachedObject,
                    InitialStates = new System.Collections.Generic.Dictionary<SelectableObject, ElementTransformState>(_activeTransformInitialStates),
                    FinalStates = finalStates,
                    Description = description
                });
            }

            _activeTransformInitialStates.Clear();
        }

        #endregion

        #region Transform Event Args & Events

        /// <summary>
        /// Event args chứa thông tin snapshot trước và sau khi hoàn tất một thao tác biến đổi
        /// </summary>
        public class TransformCompletedEventArgs : EventArgs
        {
            public SelectableObject? TargetObject { get; set; }
            public System.Collections.Generic.Dictionary<SelectableObject, ElementTransformState> InitialStates { get; set; } = new();
            public System.Collections.Generic.Dictionary<SelectableObject, ElementTransformState> FinalStates { get; set; } = new();
            public string Description { get; set; } = "Transform object";
        }

        /// <summary>
        /// Event khi hoàn thành thao tác transform (move/resize/rotate) để lưu vào Undo/Redo
        /// </summary>
        public event EventHandler<TransformCompletedEventArgs>? TransformCompleted;

        /// <summary>
        /// Event khi object bị transform (resize/rotate)
        /// </summary>
        public event EventHandler<SelectableObject>? ObjectTransformed;

        /// <summary>
        /// Event khi người dùng nhấp đúp vào đối tượng TextBlock đã chọn — yêu cầu chỉnh sửa chữ trực tiếp
        /// </summary>
        public event EventHandler<SelectableObject>? TextEditRequested;

        /// <summary>
        /// Event khi người dùng bắt đầu kéo chốt xoay (để tạm ẩn ContextToolbar)
        /// </summary>
        public event EventHandler<SelectableObject>? RotateStarted;

        /// <summary>
        /// Event khi người dùng kết thúc xoay (để hiển thị lại ContextToolbar)
        /// </summary>
        public event EventHandler<SelectableObject>? RotateCompleted;

        #endregion

        #region Constructor

        public SelectionBox()
        {
            InitializeComponent();
            // ✅ QC_4.2_TOUCH_SELECTION_BOX_FIX: Gỡ bỏ ApplyTouchIsolation khỏi SelectionBox
            // để cho phép cảm ứng tương tác trực tiếp với SelectionBorder và các chốt kéo/xoay
            _transformService = new TransformService();
            this.Visibility = Visibility.Collapsed;

            // C1-FIX: Đăng ký sự kiện nhấp đúp trên SelectionBorder để kích hoạt In-Place Text Editing
            SelectionBorder.MouseLeftButtonDown += SelectionBorder_MouseLeftButtonDown;

            // [BUG_DRAG_MOVE] Đăng ký mouse drag-to-move trên body selection box
            SelectionBorder.MouseMove += SelectionBorder_MouseMove;
            SelectionBorder.MouseLeftButtonUp += SelectionBorder_MouseLeftButtonUp;

            // ✅ TOUCH FIX: Bắt thêm sự kiện nhấc bút/ngón tay để dọn dẹp trạng thái
            // Đề phòng Windows "nuốt" mất MouseUp trên màn hình cảm ứng hồng ngoại
            SelectionBorder.TouchUp += (s, e) => ResetMoveState();
            SelectionBorder.StylusUp += (s, e) => ResetMoveState();
        }

        private DateTime _lastClickTime = DateTime.MinValue;
        private void SelectionBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Phát hiện Double-Click thủ công (interval ≤ 350ms)
            var now = DateTime.Now;
            if ((now - _lastClickTime).TotalMilliseconds <= 350)
            {
                // Double-click detected — kiểm tra nếu đối tượng đang chọn là TextBlock
                if (_attachedObject?.Element is TextBlock)
                {
                    TextEditRequested?.Invoke(this, _attachedObject);
                    e.Handled = true;
                }
            }
            _lastClickTime = now;

            // [BUG_DRAG_MOVE] Bắt đầu drag-to-move nếu không phải double-click và không trong phiên resize/rotate chốt
            if (_attachedObject != null && !_attachedObject.IsLocked && !_isDragging && !_isRotating && _activeResizeTouchId == -1 && _activeRotateTouchId == -1)
            {
                _isMoving = true;
                _dragStartPoint = e.GetPosition(this.Parent as UIElement);
                _lastMovePoint = _dragStartPoint;
                _originalPosition = _attachedObject.Position;
                _activeTransformInitialStates = CaptureCurrentTransformStates();
                SelectionBorder.CaptureMouse();
                e.Handled = true; // Ngăn sự kiện lọt xuống Canvas nền
            }
        }

        /// <summary>
        /// [BUG_DRAG_MOVE] Mouse drag-to-move khi kéo body selection box (Ủy quyền cho _transformService.MoveBy)
        /// </summary>
        private void SelectionBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isMoving || _attachedObject == null || _attachedObject.IsLocked)
                return;

            // Nếu đang trong phiên kéo chốt góc bằng Touch/Mouse, lập tức ngắt di chuyển body
            if (_isDragging || _isRotating || _activeResizeTouchId != -1 || _activeRotateTouchId != -1)
            {
                _isMoving = false;
                if (SelectionBorder.IsMouseCaptured)
                    SelectionBorder.ReleaseMouseCapture();
                return;
            }

            // CRITICAL FIX: Nếu SelectionBorder đã mất mouse capture (bị MainInteractiveBoard cướp),
            // reset _isMoving ngay lập tức để tránh state leak gây kẹt chế độ di chuyển vĩnh viễn.
            if (!SelectionBorder.IsMouseCaptured)
            {
                _isMoving = false;
                return;
            }

            // ✅ QC_4.2_GRID_PROTECT (G-4): Không di chuyển background elements
            if (QASmartTouch.Services.SelectionManager.IsBackgroundElement(_attachedObject.Element))
            {
                _isMoving = false;
                SelectionBorder.ReleaseMouseCapture();
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                // Mouse released outside → cancel move
                _isMoving = false;
                SelectionBorder.ReleaseMouseCapture();
                return;
            }

            Point currentPoint = e.GetPosition(this.Parent as UIElement);
            Vector delta = new Vector(currentPoint.X - _lastMovePoint.X, currentPoint.Y - _lastMovePoint.Y);

            if (delta.X != 0 || delta.Y != 0)
            {
                // Di chuyển đồng bộ đối tượng đơn hoặc toàn bộ các đối tượng trong Nhóm (Group)
                _transformService.MoveBy(_attachedObject, delta);
                _lastMovePoint = currentPoint;

                // Cập nhật lại vị trí SelectionBox
                UpdatePosition();

                ObjectTransformed?.Invoke(this, _attachedObject);
            }
            e.Handled = true; // Ngăn sự kiện lọt xuống Canvas nền
        }

        /// <summary>
        /// [BUG_DRAG_MOVE] Kết thúc drag-to-move
        /// </summary>
        private void SelectionBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isMoving)
            {
                _isMoving = false;

                // Cập nhật bounds trong QuadTree
                _attachedObject?.UpdateBounds();
                FinishTransformOperation("Move object");
            }

            if (SelectionBorder.IsMouseCaptured)
            {
                SelectionBorder.ReleaseMouseCapture();
            }

            // [FIX] Luôn chặn MouseUp bubble lên Canvas để tránh mất chọn (Deselect) 
            // khi màn hình cảm ứng phát sinh sự kiện kép TouchUp/MouseUp
            e.Handled = true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Attach selection box to object
        /// </summary>
        public void AttachTo(SelectableObject obj)
        {
            _attachedObject = obj;
            // ✅ REVIEW-FIX #2: Khởi tạo _originalSize khi attach để Pinch-to-Zoom
            // không bị Infinity% nếu giáo viên Pinch trước khi kéo chốt góc
            _originalSize = obj.Size;
            UpdatePosition();
            this.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Detach and hide
        /// </summary>
        public void Detach()
        {
            // Reset tất cả trạng thái kéo rê/biến đổi trước khi ẩn
            _isMoving = false;
            _isDragging = false;
            _isRotating = false;
            _activeResizeTouchId = -1;
            _activeRotateTouchId = -1;
            if (SelectionBorder.IsMouseCaptured)
                SelectionBorder.ReleaseMouseCapture();

            _activeTransformInitialStates.Clear();
            _attachedObject = null;
            this.RenderTransform = Transform.Identity;
            ScaleTooltip.Visibility = Visibility.Collapsed;
            AngleTooltip.Visibility = Visibility.Collapsed;
            SnapLine.Visibility = Visibility.Collapsed;
            this.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Reset trạng thái di chuyển — gọi từ MainDashboard khi cần đảm bảo
        /// SelectionBox không còn giữ trạng thái kéo rê cũ sau khi mouse capture bị cướp.
        /// </summary>
        public void ResetMoveState()
        {
            // Bảo vệ phiên kéo/xoay chốt bằng Touch đang diễn ra
            if (_activeResizeTouchId != -1 || _activeRotateTouchId != -1)
                return;

            bool wasRotating = _isRotating;
            if (_isMoving) FinishTransformOperation("Move object");
            if (_isDragging) FinishTransformOperation("Resize object");
            if (_isRotating) FinishTransformOperation("Rotate object");
            if (_isManipulating) FinishTransformOperation("Transform object");

            _isMoving = false;
            _isManipulating = false;
            
            if (_isDragging)
            {
                _isDragging = false;
                _currentResizeMode = Models.ResizeMode.None;
                ScaleTooltip.Visibility = Visibility.Collapsed;
            }
            
            if (_isRotating)
            {
                _isRotating = false;
                AngleTooltip.Visibility = Visibility.Collapsed;
                SnapLine.Visibility = Visibility.Collapsed;
            }
            
            if (SelectionBorder.IsMouseCaptured)
                SelectionBorder.ReleaseMouseCapture();

            _activeTransformInitialStates.Clear();

            if (wasRotating && _attachedObject != null)
            {
                RotateCompleted?.Invoke(this, _attachedObject);
            }
        }

        /// <summary>
        /// Update position based on attached object
        /// </summary>
        public void UpdatePosition()
        {
            if (_attachedObject == null)
                return;

            // Set size và position của selection box
            this.Width = Math.Max(20, _attachedObject.Size.Width + 4);  // +4 for border
            this.Height = Math.Max(20, _attachedObject.Size.Height + 4);

            Canvas.SetLeft(this, _attachedObject.Position.X - 2);
            Canvas.SetTop(this, _attachedObject.Position.Y - 2);

            // Update rotate handle line
            RotateConnectionLine.X1 = this.Width / 2;
            RotateConnectionLine.X2 = this.Width / 2;

            // Đồng bộ góc xoay cho SelectionBox
            if (_attachedObject.RotationAngle != 0)
            {
                this.RenderTransform = new RotateTransform(_attachedObject.RotationAngle, this.Width / 2.0, this.Height / 2.0);
            }
            else
            {
                this.RenderTransform = Transform.Identity;
            }
        }

        #endregion

        #region Resize Handle Events (Live Scaling Tooltip)

        private void ResizeHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Bỏ qua sự kiện chuột ảo nếu đang có ngón tay cảm ứng tương tác
            if (_activeResizeTouchId != -1)
            {
                e.Handled = true;
                return;
            }

            if (_attachedObject == null || _attachedObject.IsLocked)
                return;

            var handle = sender as FrameworkElement;
            if (handle == null)
                return;

            // Determine resize mode from Tag
            string tag = handle.Tag?.ToString() ?? "";
            _currentResizeMode = tag switch
            {
                "TopLeft" => Models.ResizeMode.TopLeft,
                "TopRight" => Models.ResizeMode.TopRight,
                "BottomLeft" => Models.ResizeMode.BottomLeft,
                "BottomRight" => Models.ResizeMode.BottomRight,
                _ => Models.ResizeMode.None
            };

            if (_currentResizeMode != Models.ResizeMode.None)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(this.Parent as UIElement);
                _originalPosition = _attachedObject.Position;
                _originalSize = _attachedObject.Size;
                _activeTransformInitialStates = CaptureCurrentTransformStates();

                _memberSnapshots.Clear();
                if (_attachedObject.GroupMembers != null && _attachedObject.GroupMembers.Count > 0)
                {
                    foreach (var member in _attachedObject.GroupMembers)
                    {
                        if (member != null)
                        {
                            _memberSnapshots[member] = (member.Position, member.Size);
                        }
                    }
                }

                ScaleTooltip.Visibility = Visibility.Visible;
                UpdateScaleTooltipText();

                handle.CaptureMouse();
                e.Handled = true;
            }
        }

        private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            // Bỏ qua sự kiện chuột ảo nếu đang có ngón tay cảm ứng tương tác
            if (_activeResizeTouchId != -1)
            {
                e.Handled = true;
                return;
            }

            if (!_isDragging || _attachedObject == null || _currentResizeMode == Models.ResizeMode.None)
                return;

            Point currentPoint = e.GetPosition(this.Parent as UIElement);
            _transformService.ResizeFromHandle(_attachedObject, _currentResizeMode, currentPoint, _dragStartPoint, _originalSize, _originalPosition, _memberSnapshots, _activeTransformInitialStates);

            UpdatePosition();
            UpdateScaleTooltipText();

            ObjectTransformed?.Invoke(this, _attachedObject);
        }

        private void ResizeHandle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_activeResizeTouchId != -1)
            {
                e.Handled = true;
                return;
            }

            if (_isDragging)
            {
                _isDragging = false;
                _currentResizeMode = Models.ResizeMode.None;
                ScaleTooltip.Visibility = Visibility.Collapsed;
                _attachedObject?.UpdateBounds();
                UpdatePosition();
                FinishTransformOperation("Resize object");
            }

            var handle = sender as FrameworkElement;
            handle?.ReleaseMouseCapture();

            // [FIX] Luôn chặn MouseUp bubble lên Canvas để tránh mất chọn (Deselect)
            e.Handled = true;
        }

        private void ResizeHandle_TouchDown(object sender, TouchEventArgs e)
        {
            if (_attachedObject == null || _attachedObject.IsLocked)
                return;

            var handle = sender as FrameworkElement;
            if (handle == null)
                return;

            // Chủ động dọn dẹp mọi cờ kéo di chuyển cũ bị kẹt do chuột ảo
            if (_isMoving)
            {
                _isMoving = false;
                if (SelectionBorder.IsMouseCaptured)
                    SelectionBorder.ReleaseMouseCapture();
            }

            if (_activeResizeTouchId != -1)
            {
                e.Handled = true;
                return;
            }

            string tag = handle.Tag?.ToString() ?? "";
            _currentResizeMode = tag switch
            {
                "TopLeft" => Models.ResizeMode.TopLeft,
                "TopRight" => Models.ResizeMode.TopRight,
                "BottomLeft" => Models.ResizeMode.BottomLeft,
                "BottomRight" => Models.ResizeMode.BottomRight,
                _ => Models.ResizeMode.None
            };

            if (_currentResizeMode != Models.ResizeMode.None)
            {
                _isDragging = true;
                _activeResizeTouchId = e.TouchDevice.Id;
                _dragStartPoint = e.GetTouchPoint(this.Parent as UIElement).Position;
                _originalPosition = _attachedObject.Position;
                _originalSize = _attachedObject.Size;
                _activeTransformInitialStates = CaptureCurrentTransformStates();

                _memberSnapshots.Clear();
                if (_attachedObject.GroupMembers != null && _attachedObject.GroupMembers.Count > 0)
                {
                    foreach (var member in _attachedObject.GroupMembers)
                    {
                        if (member != null)
                        {
                            _memberSnapshots[member] = (member.Position, member.Size);
                        }
                    }
                }

                ScaleTooltip.Visibility = Visibility.Visible;
                UpdateScaleTooltipText();

                handle.CaptureTouch(e.TouchDevice);
                e.Handled = true;
            }
        }

        private void ResizeHandle_TouchMove(object sender, TouchEventArgs e)
        {
            if (!_isDragging || _attachedObject == null || _currentResizeMode == Models.ResizeMode.None)
                return;

            if (e.TouchDevice.Id != _activeResizeTouchId)
                return;

            Point currentPoint = e.GetTouchPoint(this.Parent as UIElement).Position;
            _transformService.ResizeFromHandle(_attachedObject, _currentResizeMode, currentPoint, _dragStartPoint, _originalSize, _originalPosition, _memberSnapshots, _activeTransformInitialStates);

            UpdatePosition();
            UpdateScaleTooltipText();

            ObjectTransformed?.Invoke(this, _attachedObject);
            e.Handled = true;
        }

        private void ResizeHandle_TouchUp(object sender, TouchEventArgs e)
        {
            var handle = sender as FrameworkElement;
            if (handle != null && e.TouchDevice.Captured == handle)
            {
                handle.ReleaseTouchCapture(e.TouchDevice);
            }
            if (handle != null && handle.IsMouseCaptured)
            {
                handle.ReleaseMouseCapture();
            }

            if (_isDragging)
            {
                _isDragging = false;
                _currentResizeMode = Models.ResizeMode.None;
                ScaleTooltip.Visibility = Visibility.Collapsed;
                _attachedObject?.UpdateBounds();
                UpdatePosition();
                FinishTransformOperation("Resize object");
            }

            _activeResizeTouchId = -1;
            e.Handled = true;
        }

        private void UpdateScaleTooltipText()
        {
            if (_attachedObject == null || _originalSize.Width <= 0) return;
            double pct = Math.Round((_attachedObject.Size.Width / _originalSize.Width) * 100, 0);
            txtScaleTooltip.Text = $"{pct}% ({_attachedObject.Size.Width:F0} x {_attachedObject.Size.Height:F0} px)";
        }

        #endregion

        #region Rotate Handle Events (Smart Angle Snapping & Double-Click Reset 0°)

        private void RotateHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Bỏ qua chuột ảo nếu ngón tay cảm ứng đang thao tác
            if (_activeRotateTouchId != -1)
            {
                e.Handled = true;
                return;
            }

            if (_attachedObject == null || _attachedObject.IsLocked)
                return;

            // ✅ MODULE 2 UPGRADE: Double-Click Reset góc xoay về 0° ngay lập tức
            if (e.ClickCount == 2)
            {
                _activeTransformInitialStates = CaptureCurrentTransformStates();
                _originalPosition = _attachedObject.Position;
                _originalSize = _attachedObject.Size;
                _rotationCenter = new Point(_originalPosition.X + (_originalSize.Width / 2.0), _originalPosition.Y + (_originalSize.Height / 2.0));
                _transformService.RotateFromInitialState(_attachedObject, _rotationCenter, 0, _activeTransformInitialStates);
                _attachedObject.RotationAngle = 0;
                _attachedObject.Position = _originalPosition;
                _attachedObject.Size = _originalSize;
                _attachedObject.UpdateBounds();
                UpdatePosition();
                ObjectTransformed?.Invoke(this, _attachedObject);
                AngleTooltip.Visibility = Visibility.Collapsed;
                SnapLine.Visibility = Visibility.Collapsed;
                FinishTransformOperation("Reset rotation to 0°");
                RotateCompleted?.Invoke(this, _attachedObject);
                e.Handled = true;
                return;
            }

            _isRotating = true;
            _dragStartPoint = e.GetPosition(this.Parent as UIElement);
            _originalPosition = _attachedObject.Position;
            _originalSize = _attachedObject.Size;
            _rotationCenter = new Point(_originalPosition.X + (_originalSize.Width / 2.0), _originalPosition.Y + (_originalSize.Height / 2.0));
            _startPointerAngle = _transformService.CalculateRotationAngle(_rotationCenter, _dragStartPoint);
            _initialObjectAngle = _attachedObject.RotationAngle;
            _activeTransformInitialStates = CaptureCurrentTransformStates();

            var handle = sender as FrameworkElement;
            handle?.CaptureMouse();

            // Hiển thị AngleTooltip live khi bắt đầu xoay
            ScaleTooltip.Visibility = Visibility.Collapsed;
            txtAngleTooltip.Text = $"{Math.Round(_attachedObject.RotationAngle, 0)}°";
            AngleTooltip.Visibility = Visibility.Visible;

            // ✅ Tạm ẩn ContextToolbar khi bắt đầu xoay để giữ tầm nhìn thoáng đãng và không che nét vẽ
            RotateStarted?.Invoke(this, _attachedObject);

            e.Handled = true;
        }

        private void RotateHandle_MouseMove(object sender, MouseEventArgs e)
        {
            // Bỏ qua chuột ảo nếu ngón tay cảm ứng đang thao tác
            if (_activeRotateTouchId != -1)
            {
                e.Handled = true;
                return;
            }

            if (!_isRotating || _attachedObject == null)
                return;

            Point currentPoint = e.GetPosition(this.Parent as UIElement);

            // Tính góc chuột hiện tại so với tâm bất biến đã lưu lúc MouseDown
            double currentPointerAngle = _transformService.CalculateRotationAngle(_rotationCenter, currentPoint);
            double pointerDelta = currentPointerAngle - _startPointerAngle;

            double targetAngle = (_initialObjectAngle + pointerDelta) % 360;
            if (targetAngle < 0) targetAngle += 360;

            // Smart Angle Snapping (Toán học & Lượng giác: 0, 30, 45, 60, 90, 120, 135, 150, 180, ...)
            double snappedAngle = ApplySmartAngleSnappingExtended(targetAngle);

            _transformService.RotateFromInitialState(_attachedObject, _rotationCenter, snappedAngle, _activeTransformInitialStates);
            _attachedObject.RotationAngle = snappedAngle;
            _attachedObject.Position = _originalPosition;
            _attachedObject.Size = _originalSize;

            UpdatePosition();

            // Update live degree tooltip
            txtAngleTooltip.Text = $"{Math.Round(snappedAngle, 0)}°";
            AngleTooltip.Visibility = Visibility.Visible;

            ObjectTransformed?.Invoke(this, _attachedObject);
            e.Handled = true;
        }

        private double ApplySmartAngleSnappingExtended(double rawAngle)
        {
            double normalizedAngle = (rawAngle % 360 + 360) % 360;
            double[] snapTargets = new double[]
            {
                0, 30, 45, 60, 90, 120, 135, 150, 180, 210, 225, 240, 270, 300, 315, 330, 360
            };

            foreach (double target in snapTargets)
            {
                if (Math.Abs(normalizedAngle - target) <= 2.5)
                {
                    SnapLine.Visibility = Visibility.Visible;
                    return target == 360 ? 0 : target;
                }
            }

            SnapLine.Visibility = Visibility.Collapsed;
            return rawAngle;
        }

        private void RotateHandle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_activeRotateTouchId != -1)
            {
                e.Handled = true;
                return;
            }

            if (_isRotating)
            {
                _isRotating = false;
                SnapLine.Visibility = Visibility.Collapsed;
                AngleTooltip.Visibility = Visibility.Collapsed;
                _attachedObject?.UpdateBounds();
                UpdatePosition();
                FinishTransformOperation("Rotate object");
                if (_attachedObject != null)
                {
                    RotateCompleted?.Invoke(this, _attachedObject);
                }
            }

            var handle = sender as FrameworkElement;
            handle?.ReleaseMouseCapture();

            // [FIX] Luôn chặn MouseUp bubble lên Canvas để tránh mất chọn (Deselect)
            e.Handled = true;
        }

        private void RotateHandle_TouchDown(object sender, TouchEventArgs e)
        {
            if (_attachedObject == null || _attachedObject.IsLocked)
                return;

            var handle = sender as FrameworkElement;
            if (handle == null)
                return;

            // Chủ động dọn dẹp mọi cờ kéo di chuyển cũ bị kẹt do chuột ảo
            if (_isMoving)
            {
                _isMoving = false;
                if (SelectionBorder.IsMouseCaptured)
                    SelectionBorder.ReleaseMouseCapture();
            }

            if (_activeRotateTouchId != -1)
            {
                e.Handled = true;
                return;
            }

            _isRotating = true;
            _activeRotateTouchId = e.TouchDevice.Id;
            _dragStartPoint = e.GetTouchPoint(this.Parent as UIElement).Position;
            _originalPosition = _attachedObject.Position;
            _originalSize = _attachedObject.Size;
            _rotationCenter = new Point(_originalPosition.X + (_originalSize.Width / 2.0), _originalPosition.Y + (_originalSize.Height / 2.0));
            _startPointerAngle = _transformService.CalculateRotationAngle(_rotationCenter, _dragStartPoint);
            _initialObjectAngle = _attachedObject.RotationAngle;
            _activeTransformInitialStates = CaptureCurrentTransformStates();

            handle.CaptureTouch(e.TouchDevice);

            ScaleTooltip.Visibility = Visibility.Collapsed;
            txtAngleTooltip.Text = $"{Math.Round(_attachedObject.RotationAngle, 0)}°";
            AngleTooltip.Visibility = Visibility.Visible;

            RotateStarted?.Invoke(this, _attachedObject);
            e.Handled = true;
        }

        private void RotateHandle_TouchMove(object sender, TouchEventArgs e)
        {
            if (!_isRotating || _attachedObject == null || e.TouchDevice.Id != _activeRotateTouchId)
                return;

            Point currentPoint = e.GetTouchPoint(this.Parent as UIElement).Position;
            double currentPointerAngle = _transformService.CalculateRotationAngle(_rotationCenter, currentPoint);
            double pointerDelta = currentPointerAngle - _startPointerAngle;

            double targetAngle = (_initialObjectAngle + pointerDelta) % 360;
            if (targetAngle < 0) targetAngle += 360;

            double snappedAngle = ApplySmartAngleSnappingExtended(targetAngle);

            _transformService.RotateFromInitialState(_attachedObject, _rotationCenter, snappedAngle, _activeTransformInitialStates);
            _attachedObject.RotationAngle = snappedAngle;
            _attachedObject.Position = _originalPosition;
            _attachedObject.Size = _originalSize;

            UpdatePosition();

            txtAngleTooltip.Text = $"{Math.Round(snappedAngle, 0)}°";
            AngleTooltip.Visibility = Visibility.Visible;

            ObjectTransformed?.Invoke(this, _attachedObject);
            e.Handled = true;
        }

        private void RotateHandle_TouchUp(object sender, TouchEventArgs e)
        {
            var handle = sender as FrameworkElement;
            if (handle != null && e.TouchDevice.Captured == handle)
            {
                handle.ReleaseTouchCapture(e.TouchDevice);
            }
            if (handle != null && handle.IsMouseCaptured)
            {
                handle.ReleaseMouseCapture();
            }

            if (_isRotating)
            {
                _isRotating = false;
                SnapLine.Visibility = Visibility.Collapsed;
                AngleTooltip.Visibility = Visibility.Collapsed;
                _attachedObject?.UpdateBounds();
                UpdatePosition();
                FinishTransformOperation("Rotate object");
                if (_attachedObject != null)
                {
                    RotateCompleted?.Invoke(this, _attachedObject);
                }
            }

            _activeRotateTouchId = -1;
            e.Handled = true;
        }

        private void Handle_LostTouchCapture(object sender, TouchEventArgs e)
        {
            var handle = sender as FrameworkElement;
            if (handle != null && handle.IsMouseCaptured)
            {
                handle.ReleaseMouseCapture();
            }

            if (e.TouchDevice.Id == _activeResizeTouchId || _activeResizeTouchId != -1)
            {
                if (_isDragging)
                {
                    _isDragging = false;
                    _currentResizeMode = Models.ResizeMode.None;
                    ScaleTooltip.Visibility = Visibility.Collapsed;
                    _attachedObject?.UpdateBounds();
                    UpdatePosition();
                    FinishTransformOperation("Resize object");
                }
                _activeResizeTouchId = -1;
            }
            if (e.TouchDevice.Id == _activeRotateTouchId || _activeRotateTouchId != -1)
            {
                if (_isRotating)
                {
                    _isRotating = false;
                    SnapLine.Visibility = Visibility.Collapsed;
                    AngleTooltip.Visibility = Visibility.Collapsed;
                    _attachedObject?.UpdateBounds();
                    UpdatePosition();
                    FinishTransformOperation("Rotate object");
                    if (_attachedObject != null)
                    {
                        RotateCompleted?.Invoke(this, _attachedObject);
                    }
                }
                _activeRotateTouchId = -1;
            }
        }

        #endregion

        #region Multi-touch Manipulation Gestures (Pinch-to-Zoom)

        private void SelectionBorder_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            // Nếu đang trong phiên kéo chốt resize hoặc xoay, hủy bỏ Manipulation để tránh MoveBy can thiệp gây giật khung
            if (_attachedObject == null || _attachedObject.IsLocked || _isDragging || _isRotating || _activeResizeTouchId != -1 || _activeRotateTouchId != -1)
            {
                _isManipulating = false;
                e.Cancel();
                e.Handled = true;
                return;
            }

            e.ManipulationContainer = this.Parent as IInputElement;

            // ✅ REVIEW-FIX #2: Ghi nhận kích thước gốc khi bắt đầu Pinch gesture
            _originalSize = _attachedObject.Size;
            _activeTransformInitialStates = CaptureCurrentTransformStates();
            _isManipulating = true;

            e.Handled = true;
        }

        private void SelectionBorder_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            // Tuyệt đối không can thiệp di chuyển MoveBy khi người dùng đang kéo chốt resize/rotate
            if (_attachedObject == null || _attachedObject.IsLocked || _isDragging || _isRotating || _activeResizeTouchId != -1 || _activeRotateTouchId != -1)
            {
                _isManipulating = false;
                e.Complete();
                e.Handled = true;
                return;
            }

            // [BUG_DRAG_MOVE] Handle 1-finger Translation (di chuyển)
            double transX = e.DeltaManipulation.Translation.X;
            double transY = e.DeltaManipulation.Translation.Y;

            if (Math.Abs(transX) > 0.5 || Math.Abs(transY) > 0.5)
            {
                // ✅ QC_4.2_GRID_PROTECT (G-4): Không di chuyển background elements bằng touch manipulation
                if (QASmartTouch.Services.SelectionManager.IsBackgroundElement(_attachedObject.Element))
                    return;

                // ✅ QC_4.2_TOUCH_MOVE_FIX: Ủy quyền cho _transformService.MoveBy thay vì gán thủ công Canvas.SetLeft/Top!
                // Giúp di chuyển đồng bộ mọi loại đối tượng: Group, Polyline (nét vẽ), Path (nét mượt), Line, Shape, Image...
                Vector delta = new Vector(transX, transY);
                _transformService.MoveBy(_attachedObject, delta);

                UpdatePosition();
                ObjectTransformed?.Invoke(this, _attachedObject);
            }

            // Handle 2-finger Pinch-to-Zoom gesture
            double scaleFactor = (e.DeltaManipulation.Scale.X + e.DeltaManipulation.Scale.Y) / 2.0;

            if (scaleFactor != 1.0 && scaleFactor > 0.1 && scaleFactor < 5.0)
            {
                double newWidth = Math.Max(20, _attachedObject.Size.Width * scaleFactor);
                double newHeight = Math.Max(20, _attachedObject.Size.Height * scaleFactor);

                // ✅ AUD-06 FIX: Gọi TransformService.Resize() thay vì gán Size trực tiếp
                _transformService.Resize(_attachedObject, new Size(newWidth, newHeight));

                UpdatePosition();
                UpdateScaleTooltipText();
                ScaleTooltip.Visibility = Visibility.Visible;

                ObjectTransformed?.Invoke(this, _attachedObject);
            }

            e.Handled = true;
        }

        private void SelectionBorder_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            // Bỏ qua dọn dẹp nếu phiên kéo chốt vẫn đang diễn ra
            if (_isDragging || _isRotating || _activeResizeTouchId != -1 || _activeRotateTouchId != -1)
            {
                _isManipulating = false;
                e.Handled = true;
                return;
            }

            ScaleTooltip.Visibility = Visibility.Collapsed;

            // Cập nhật bounds cho QuadTree spatial index sau khi di chuyển/zoom bằng touch
            if (_attachedObject != null)
            {
                _attachedObject.UpdateBounds();
                ObjectTransformed?.Invoke(this, _attachedObject);
            }

            FinishTransformOperation("Transform object");
            ResetMoveState();

            e.Handled = true;
        }

        #endregion
    }
}
