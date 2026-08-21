using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private System.Collections.Generic.Dictionary<SelectableObject, (Point pos, Size size)> _memberSnapshots = new();
        private bool _isDragging = false;
        private bool _isRotating = false;
        private bool _isMoving = false; // [BUG_DRAG_MOVE] Drag-to-move state
        private bool _isManipulating = false;

        private const double SnapAngleThresholdDegrees = 3.5;

        /// <summary>
        /// True nếu đang trong quá trình kéo chốt resize hoặc xoay
        /// </summary>
        public bool IsTransforming => _isDragging || _isRotating || _isMoving || _isManipulating;

        /// <summary>
        /// Object đang được attach bởi SelectionBox (Object đơn hoặc Group Object)
        /// </summary>
        public SelectableObject? AttachedObject => _attachedObject;

        #endregion

        #region Events

        /// <summary>
        /// Event khi object bị transform (resize/rotate)
        /// </summary>
        public event EventHandler<SelectableObject>? ObjectTransformed;

        /// <summary>
        /// Event khi người dùng nhấp đúp vào đối tượng TextBlock đã chọn — yêu cầu chỉnh sửa chữ trực tiếp
        /// </summary>
        public event EventHandler<SelectableObject>? TextEditRequested;

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
            // QC_4.2_TOUCH_RESIZE_GUARD: Đảm bảo khi kéo chốt vuông mở rộng/thu nhỏ (Resize) bằng ngón tay hoặc bút cảm ứng, không bị phát nét vẽ theo
            this.Loaded += (s, e) =>
            {
                Rectangle[] handles = new Rectangle[] { TopLeftHandle, TopRightHandle, BottomLeftHandle, BottomRightHandle };
                foreach (var h in handles)
                {
                    if (h != null)
                    {
                        h.PreviewTouchDown += (snd, touchArgs) => { touchArgs.Handled = true; };
                        h.PreviewStylusDown += (snd, stylusArgs) => { stylusArgs.Handled = true; };
                    }
                }
            };
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

            // [BUG_DRAG_MOVE] Bắt đầu drag-to-move nếu không phải double-click
            if (_attachedObject != null && !_attachedObject.IsLocked && !_isDragging && !_isRotating)
            {
                _isMoving = true;
                _dragStartPoint = e.GetPosition(this.Parent as UIElement);
                _lastMovePoint = _dragStartPoint;
                _originalPosition = _attachedObject.Position;
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
            if (SelectionBorder.IsMouseCaptured)
                SelectionBorder.ReleaseMouseCapture();

            _attachedObject = null;
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
        }

        /// <summary>
        /// Update position based on attached object
        /// </summary>
        public void UpdatePosition()
        {
            if (_attachedObject == null)
                return;

            // Set size và position của selection box
            this.Width = _attachedObject.Size.Width + 4;  // +4 for border
            this.Height = _attachedObject.Size.Height + 4;

            Canvas.SetLeft(this, _attachedObject.Position.X - 2);
            Canvas.SetTop(this, _attachedObject.Position.Y - 2);

            // Update rotate handle line
            RotateConnectionLine.X1 = this.Width / 2;
            RotateConnectionLine.X2 = this.Width / 2;
        }

        #endregion

        #region Resize Handle Events (Live Scaling Tooltip)

        private void ResizeHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_attachedObject == null || _attachedObject.IsLocked)
                return;

            var handle = sender as Rectangle;
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
            if (!_isDragging || _attachedObject == null || _currentResizeMode == Models.ResizeMode.None)
                return;

            Point currentPoint = e.GetPosition(this.Parent as UIElement);
            _transformService.ResizeFromHandle(_attachedObject, _currentResizeMode, currentPoint, _dragStartPoint, _originalSize, _originalPosition, _memberSnapshots);

            UpdatePosition();
            UpdateScaleTooltipText();

            ObjectTransformed?.Invoke(this, _attachedObject);
        }

        private void ResizeHandle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _currentResizeMode = Models.ResizeMode.None;
                ScaleTooltip.Visibility = Visibility.Collapsed;
            }

            var handle = sender as Rectangle;
            handle?.ReleaseMouseCapture();

            // [FIX] Luôn chặn MouseUp bubble lên Canvas để tránh mất chọn (Deselect)
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
            if (_attachedObject == null || _attachedObject.IsLocked)
                return;

            // ✅ MODULE 2 UPGRADE: Double-Click Reset góc xoay về 0° ngay lập tức
            if (e.ClickCount == 2)
            {
                _transformService.Rotate(_attachedObject, 0);
                UpdatePosition();
                ObjectTransformed?.Invoke(this, _attachedObject);
                AngleTooltip.Visibility = Visibility.Collapsed;
                SnapLine.Visibility = Visibility.Collapsed;
                e.Handled = true;
                return;
            }

            _isRotating = true;
            _dragStartPoint = e.GetPosition(this.Parent as UIElement);

            var handle = sender as Ellipse;
            handle?.CaptureMouse();

            // Hiển thị AngleTooltip live khi bắt đầu xoay
            ScaleTooltip.Visibility = Visibility.Collapsed;
            txtAngleTooltip.Text = $"{Math.Round(_attachedObject.RotationAngle, 0)}°";
            AngleTooltip.Visibility = Visibility.Visible;

            e.Handled = true;
        }

        private void RotateHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isRotating || _attachedObject == null)
                return;

            Point currentPoint = e.GetPosition(this.Parent as UIElement);
            Point center = _transformService.GetCenter(_attachedObject);

            // Calculate raw rotation angle
            double angle = _transformService.CalculateRotationAngle(center, currentPoint);

            // Smart Angle Snapping (Toán học & Lượng giác: 0, 30, 45, 60, 90, 120, 135, 150, 180, ...)
            double snappedAngle = ApplySmartAngleSnappingExtended(angle);

            _transformService.Rotate(_attachedObject, snappedAngle);

            // Update live degree tooltip
            txtAngleTooltip.Text = $"{Math.Round(snappedAngle, 0)}°";
            AngleTooltip.Visibility = Visibility.Visible;

            ObjectTransformed?.Invoke(this, _attachedObject);
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
            if (_isRotating)
            {
                _isRotating = false;
                SnapLine.Visibility = Visibility.Collapsed;
                AngleTooltip.Visibility = Visibility.Collapsed;
            }

            var handle = sender as Ellipse;
            handle?.ReleaseMouseCapture();

            // [FIX] Luôn chặn MouseUp bubble lên Canvas để tránh mất chọn (Deselect)
            e.Handled = true;
        }

        #endregion

        #region Multi-touch Manipulation Gestures (Pinch-to-Zoom)

        private void SelectionBorder_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            if (_attachedObject == null || _attachedObject.IsLocked) return;
            e.ManipulationContainer = this.Parent as IInputElement;

            // ✅ REVIEW-FIX #2: Ghi nhận kích thước gốc khi bắt đầu Pinch gesture
            _originalSize = _attachedObject.Size;
            _isManipulating = true;

            e.Handled = true;
        }

        private void SelectionBorder_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (_attachedObject == null || _attachedObject.IsLocked) return;

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
            ScaleTooltip.Visibility = Visibility.Collapsed;

            // QC_4.2_TOUCH_SELECTION_FIX: Reset trạng thái khi touch manipulation kết thúc
            // Tránh _isMoving bị kẹt true → con trỏ bị giữ ở chế độ kéo vĩnh viễn trên cảm ứng
            // Khi nhấc tay khỏi màn hình, phải giải phóng hoàn toàn để cho phép chuyển tool
            ResetMoveState();

            // Cập nhật bounds cho QuadTree spatial index sau khi di chuyển/zoom bằng touch
            if (_attachedObject != null)
            {
                _attachedObject.UpdateBounds();
                ObjectTransformed?.Invoke(this, _attachedObject);
            }

            e.Handled = true;
        }

        #endregion
    }
}
