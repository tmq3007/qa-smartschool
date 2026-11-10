using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using QASmartTouch.Models;
using QASmartTouch.Helpers;

namespace QASmartTouch.Controls
{
    /// <summary>
    /// UMind Unified Container 3D Model Viewer with Touch + Stylus + Mouse Camera & Frame Control
    /// </summary>
    public partial class Model3DControl : UserControl
    {
        private readonly Model3DObject _modelData;
        private readonly Model3DGroup? _model3D;

        public event EventHandler? DeleteRequested;
        public event EventHandler? PositionOrSizeChanged;
        public event EventHandler? BringToFrontRequested;

        public Model3DObject ModelData => _modelData;
        public Model3DGroup? Model3D => _model3D;

        private DispatcherTimer? _autoRotateTimer;
        private bool _isAutoRotating;

        public Model3DControl(string modelPath, Model3DGroup model3D)
        {
            InitializeComponent();

            string displayFileName = string.IsNullOrWhiteSpace(modelPath)
                ? "Mô hình 3D"
                : System.IO.Path.GetFileName(modelPath);

            _modelData = new Model3DObject
            {
                FilePath = modelPath,
                FileName = displayFileName,
                ImportedDate = DateTime.Now,
                Size = new Size(this.Width, this.Height)
            };

            _model3D = model3D;

            // Set model to viewport
            modelVisual.Content = _model3D;

            // Update header title
            txtTitle.Text = displayFileName;

            // Zoom to fit once loaded and immediately
            viewport3D.ZoomExtents();
            this.Loaded += (s, e) =>
            {
                try { viewport3D.ZoomExtents(); } catch { }
            };
            this.Unloaded += (s, e) => StopAutoRotate();

            // Wire 1-touch immediate activation for all header bar buttons (86" IFP standard)
            WireHeaderButtonsForTouch();

            // Enable dragging from unified header bar (Touch + Stylus + Mouse)
            InitializeDragSupport();

            // Enable manual camera control (Touch 1-finger rotate, 2-finger pinch zoom + Mouse)
            InitializeCameraControl();

            ApplyGraphicsSettings();

            System.Diagnostics.Debug.WriteLine($"[Model3DControl] Created unified 3D container for: {_modelData.FileName}");
        }

        private void WireHeaderButtonsForTouch()
        {
            TouchActivationHelper.WireButton(btnTopView);
            TouchActivationHelper.WireButton(btnFrontView);
            TouchActivationHelper.WireButton(btnSideView);
            TouchActivationHelper.WireButton(btnAutoRotate);
            TouchActivationHelper.WireButton(btnResetView);
            TouchActivationHelper.WireButton(btnZoomOut);
            TouchActivationHelper.WireButton(btnZoomIn);
            TouchActivationHelper.WireButton(btnDelete);
        }

        private void ApplyGraphicsSettings()
        {
            int resolutionSetting = QASmartTouch.Services.AppSettings.GetRecommended3DResolution();
            var config = QASmartClass.Services.AppConfig.Load();
            if (resolutionSetting <= 20 || config.Disable3DAntiAliasing || (config.AutoOptimizeForIntegratedGraphics && QASmartClass.Services.AppConfig.IsIntegratedGraphicsDetected))
            {
                RenderOptions.SetEdgeMode(viewport3D, EdgeMode.Aliased);
            }
            else
            {
                RenderOptions.SetEdgeMode(viewport3D, EdgeMode.Unspecified);
            }
        }

        #region Unified Header Bar Drag Support (Touch + Stylus + Mouse)

        private bool _isDragging;
        private Point _dragStartPoint;
        private double _dragStartLeft;
        private double _dragStartTop;
        private int _activeDragTouchId = -1;

        private void InitializeDragSupport()
        {
            // Mouse Dragging
            titleBar.MouseLeftButtonDown += TitleBar_MouseLeftButtonDown;
            titleBar.MouseMove += TitleBar_MouseMove;
            titleBar.MouseLeftButtonUp += TitleBar_MouseLeftButtonUp;

            // Touch Dragging (86" IFP Multi-Touch Priority)
            titleBar.PreviewTouchDown += TitleBar_PreviewTouchDown;
            titleBar.PreviewTouchMove += TitleBar_PreviewTouchMove;
            titleBar.PreviewTouchUp += TitleBar_PreviewTouchUp;
            titleBar.LostTouchCapture += TitleBar_LostTouchCapture;

            // Stylus Dragging
            titleBar.PreviewStylusDown += TitleBar_PreviewStylusDown;
            titleBar.PreviewStylusMove += TitleBar_PreviewStylusMove;
            titleBar.PreviewStylusUp += TitleBar_PreviewStylusUp;
        }

        private bool IsHeaderButtonSource(object originalSource)
        {
            return QASmartTouch.Utilities.InputValidationHelper.IsEventFromButton(originalSource, titleBar);
        }

        private void BeginHeaderDrag(Point startPointInParent)
        {
            _isDragging = true;
            _dragStartPoint = startPointInParent;
            _dragStartLeft = Canvas.GetLeft(this);
            _dragStartTop = Canvas.GetTop(this);
            if (double.IsNaN(_dragStartLeft)) _dragStartLeft = 0;
            if (double.IsNaN(_dragStartTop)) _dragStartTop = 0;

            BringToFrontRequested?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateHeaderDrag(Point currentPointInParent)
        {
            if (!_isDragging) return;

            double deltaX = currentPointInParent.X - _dragStartPoint.X;
            double deltaY = currentPointInParent.Y - _dragStartPoint.Y;

            double elemW = this.ActualWidth > 0 ? this.ActualWidth : (this.Width > 0 ? this.Width : 720);
            double elemH = this.ActualHeight > 0 ? this.ActualHeight : (this.Height > 0 ? this.Height : 500);
            var (newLeft, newTop) = QASmartTouch.Helpers.BoundsHelper.ClampToVirtualBoard(_dragStartLeft + deltaX, _dragStartTop + deltaY, elemW, elemH);

            Canvas.SetLeft(this, newLeft);
            Canvas.SetTop(this, newTop);
        }

        private void EndHeaderDrag()
        {
            if (!_isDragging) return;

            _isDragging = false;
            _activeDragTouchId = -1;

            var position = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
            UpdateModelData(position);
            PositionOrSizeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TitleBar_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            if (IsHeaderButtonSource(e.OriginalSource)) return;
            if (this.Parent is not UIElement parent) return;

            _activeDragTouchId = e.TouchDevice.Id;
            BeginHeaderDrag(e.GetTouchPoint(parent).Position);
            titleBar.CaptureTouch(e.TouchDevice);
            e.Handled = true;
        }

        private void TitleBar_PreviewTouchMove(object sender, TouchEventArgs e)
        {
            if (!_isDragging || e.TouchDevice.Id != _activeDragTouchId) return;
            if (this.Parent is not UIElement parent) return;

            UpdateHeaderDrag(e.GetTouchPoint(parent).Position);
            e.Handled = true;
        }

        private void TitleBar_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            if (!_isDragging || e.TouchDevice.Id != _activeDragTouchId) return;

            titleBar.ReleaseTouchCapture(e.TouchDevice);
            EndHeaderDrag();
            e.Handled = true;
        }

        private void TitleBar_LostTouchCapture(object sender, TouchEventArgs e)
        {
            if (_isDragging && e.TouchDevice.Id == _activeDragTouchId)
            {
                EndHeaderDrag();
            }
        }

        private void TitleBar_PreviewStylusDown(object sender, StylusDownEventArgs e)
        {
            if (e.StylusDevice?.TabletDevice?.Type != TabletDeviceType.Stylus || _activeDragTouchId != -1) return;
            if (IsHeaderButtonSource(e.OriginalSource)) return;
            if (this.Parent is not UIElement parent) return;

            BeginHeaderDrag(e.GetPosition(parent));
            Stylus.Capture(titleBar);
            e.Handled = true;
        }

        private void TitleBar_PreviewStylusMove(object sender, StylusEventArgs e)
        {
            if (e.StylusDevice?.TabletDevice?.Type != TabletDeviceType.Stylus || !_isDragging || _activeDragTouchId != -1) return;
            if (this.Parent is not UIElement parent) return;

            UpdateHeaderDrag(e.GetPosition(parent));
            e.Handled = true;
        }

        private void TitleBar_PreviewStylusUp(object sender, StylusEventArgs e)
        {
            if (e.StylusDevice?.TabletDevice?.Type != TabletDeviceType.Stylus || !_isDragging || _activeDragTouchId != -1) return;

            Stylus.Capture(null);
            EndHeaderDrag();
            e.Handled = true;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_activeDragTouchId != -1 || e.StylusDevice != null) return;
            if (IsHeaderButtonSource(e.OriginalSource)) return;
            if (_isDragging) return;
            if (this.Parent is not UIElement parent) return;

            BeginHeaderDrag(e.GetPosition(parent));
            titleBar.CaptureMouse();
            e.Handled = true;
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _activeDragTouchId != -1 || e.StylusDevice != null) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (this.Parent is not UIElement parent) return;

            UpdateHeaderDrag(e.GetPosition(parent));
            e.Handled = true;
        }

        private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_activeDragTouchId != -1 || e.StylusDevice != null) return;
            if (_isDragging)
            {
                titleBar.ReleaseMouseCapture();
                EndHeaderDrag();
                e.Handled = true;
            }
        }

        #endregion

        #region Camera Control (Touch 1-Finger Orbit + 2-Finger Pinch Zoom + Mouse)

        private bool _isRotating;
        private Point _lastMousePosition;
        private readonly Dictionary<int, Point> _activeViewportTouches = new();
        private double _lastPinchDistance;

        private void InitializeCameraControl()
        {
            // Mouse events on viewport
            viewport3D.MouseLeftButtonDown += Viewport_MouseLeftButtonDown;
            viewport3D.MouseMove += Viewport_MouseMove;
            viewport3D.MouseLeftButtonUp += Viewport_MouseLeftButtonUp;
            viewport3D.MouseWheel += Viewport_MouseWheel;

            // Touch events on viewport (1-finger orbit rotation, 2-finger pinch zoom)
            viewport3D.PreviewTouchDown += Viewport_PreviewTouchDown;
            viewport3D.PreviewTouchMove += Viewport_PreviewTouchMove;
            viewport3D.PreviewTouchUp += Viewport_PreviewTouchUp;
            viewport3D.LostTouchCapture += Viewport_LostTouchCapture;

            // Auto-rotate timer (smooth ~40fps orbit)
            _autoRotateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(25)
            };
            _autoRotateTimer.Tick += (s, e) =>
            {
                if (_isAutoRotating && !_isRotating && _activeViewportTouches.Count == 0)
                {
                    RotateCamera(1.4, 0);
                }
            };
        }

        private void Viewport_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            BringToFrontRequested?.Invoke(this, EventArgs.Empty);

            Point pt = e.GetTouchPoint(viewport3D).Position;
            _activeViewportTouches[e.TouchDevice.Id] = pt;
            viewport3D.CaptureTouch(e.TouchDevice);

            if (_activeViewportTouches.Count == 2)
            {
                var pts = _activeViewportTouches.Values.ToArray();
                _lastPinchDistance = (pts[0] - pts[1]).Length;
            }

            e.Handled = true;
        }

        private void Viewport_PreviewTouchMove(object sender, TouchEventArgs e)
        {
            if (!_activeViewportTouches.TryGetValue(e.TouchDevice.Id, out Point prevPt))
                return;

            Point currentPt = e.GetTouchPoint(viewport3D).Position;
            _activeViewportTouches[e.TouchDevice.Id] = currentPt;

            if (_activeViewportTouches.Count == 1)
            {
                var delta = currentPt - prevPt;
                if (Math.Abs(delta.X) > 0.2 || Math.Abs(delta.Y) > 0.2)
                {
                    RotateCamera(delta.X, delta.Y);
                }
            }
            else if (_activeViewportTouches.Count >= 2)
            {
                var pts = _activeViewportTouches.Values.Take(2).ToArray();
                double currentDist = (pts[0] - pts[1]).Length;
                if (_lastPinchDistance > 10 && currentDist > 10)
                {
                    double ratio = _lastPinchDistance / currentDist;
                    // Clamp per-frame zoom ratio for smooth pinch control
                    ratio = Math.Max(0.92, Math.Min(1.08, ratio));
                    ZoomCameraByFactor(ratio);
                }
                _lastPinchDistance = currentDist;
            }

            e.Handled = true;
        }

        private void Viewport_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            if (_activeViewportTouches.Remove(e.TouchDevice.Id))
            {
                viewport3D.ReleaseTouchCapture(e.TouchDevice);
            }
            if (_activeViewportTouches.Count < 2)
            {
                _lastPinchDistance = 0;
            }
            e.Handled = true;
        }

        private void Viewport_LostTouchCapture(object sender, TouchEventArgs e)
        {
            _activeViewportTouches.Remove(e.TouchDevice.Id);
            if (_activeViewportTouches.Count < 2)
            {
                _lastPinchDistance = 0;
            }
        }

        private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_activeViewportTouches.Count > 0 || e.StylusDevice != null) return;

            BringToFrontRequested?.Invoke(this, EventArgs.Empty);
            _isRotating = true;
            _lastMousePosition = e.GetPosition(viewport3D);
            viewport3D.CaptureMouse();
            e.Handled = true;
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (_activeViewportTouches.Count > 0 || e.StylusDevice != null) return;

            if (_isRotating && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(viewport3D);
                var delta = currentPosition - _lastMousePosition;

                RotateCamera(delta.X, delta.Y);

                _lastMousePosition = currentPosition;
                e.Handled = true;
            }
        }

        private void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_activeViewportTouches.Count > 0 || e.StylusDevice != null) return;
            if (_isRotating)
            {
                _isRotating = false;
                viewport3D.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = e.Delta > 0 ? 0.88 : 1.12;
            ZoomCameraByFactor(zoomFactor);
            e.Handled = true;
        }

        private void ZoomCameraByFactor(double zoomFactor)
        {
            if (viewport3D.Camera is not PerspectiveCamera camera) return;

            var target = new Point3D(
                camera.Position.X + camera.LookDirection.X,
                camera.Position.Y + camera.LookDirection.Y,
                camera.Position.Z + camera.LookDirection.Z
            );

            var newLook = new Vector3D(
                camera.LookDirection.X * zoomFactor,
                camera.LookDirection.Y * zoomFactor,
                camera.LookDirection.Z * zoomFactor
            );

            // Prevent zooming too close or too far
            double dist = newLook.Length;
            if (dist < 1.5 || dist > 500) return;

            camera.Position = new Point3D(
                target.X - newLook.X,
                target.Y - newLook.Y,
                target.Z - newLook.Z
            );
            camera.LookDirection = newLook;
        }

        private void RotateCamera(double deltaX, double deltaY)
        {
            if (viewport3D.Camera is not PerspectiveCamera camera) return;

            double sensitivity = 0.5;
            double angleX = deltaY * sensitivity;
            double angleY = -deltaX * sensitivity;

            var position = camera.Position;
            var lookDirection = camera.LookDirection;
            var upDirection = camera.UpDirection;

            var target = new Point3D(
                position.X + lookDirection.X,
                position.Y + lookDirection.Y,
                position.Z + lookDirection.Z
            );

            var toCamera = new Vector3D(
                position.X - target.X,
                position.Y - target.Y,
                position.Z - target.Z
            );

            // Horizontal rotation around Up axis
            var rotationY = new AxisAngleRotation3D(upDirection, angleY);
            var transformY = new RotateTransform3D(rotationY);
            toCamera = transformY.Transform(toCamera);

            // Vertical rotation around Right axis
            var rightAxis = Vector3D.CrossProduct(lookDirection, upDirection);
            if (rightAxis.LengthSquared > 0.0001)
            {
                rightAxis.Normalize();
                var rotationX = new AxisAngleRotation3D(rightAxis, angleX);
                var transformX = new RotateTransform3D(rotationX);
                toCamera = transformX.Transform(toCamera);
                upDirection = transformX.Transform(upDirection);
            }

            camera.Position = new Point3D(
                target.X + toCamera.X,
                target.Y + toCamera.Y,
                target.Z + toCamera.Z
            );

            camera.LookDirection = new Vector3D(
                -toCamera.X,
                -toCamera.Y,
                -toCamera.Z
            );

            camera.UpDirection = upDirection;
        }

        #endregion

        #region Header Quick Actions (Camera View Presets, Auto-Rotate, Zoom, Close)

        private void BtnTopView_Click(object sender, RoutedEventArgs e)
        {
            if (viewport3D.Camera is PerspectiveCamera camera)
            {
                camera.Position = new Point3D(0, 0, 15);
                camera.LookDirection = new Vector3D(0, 0, -15);
                camera.UpDirection = new Vector3D(0, 1, 0);
                viewport3D.ZoomExtents();
            }
        }

        private void BtnFrontView_Click(object sender, RoutedEventArgs e)
        {
            if (viewport3D.Camera is PerspectiveCamera camera)
            {
                camera.Position = new Point3D(0, -15, 3);
                camera.LookDirection = new Vector3D(0, 15, -3);
                camera.UpDirection = new Vector3D(0, 0, 1);
                viewport3D.ZoomExtents();
            }
        }

        private void BtnSideView_Click(object sender, RoutedEventArgs e)
        {
            if (viewport3D.Camera is PerspectiveCamera camera)
            {
                camera.Position = new Point3D(15, 0, 3);
                camera.LookDirection = new Vector3D(-15, 0, -3);
                camera.UpDirection = new Vector3D(0, 0, 1);
                viewport3D.ZoomExtents();
            }
        }

        private void BtnAutoRotate_Click(object sender, RoutedEventArgs e)
        {
            _isAutoRotating = !_isAutoRotating;
            if (_isAutoRotating)
            {
                btnAutoRotate.Background = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // #8B5CF6
                _autoRotateTimer?.Start();
            }
            else
            {
                StopAutoRotate();
            }
        }

        public void StopAutoRotate()
        {
            _isAutoRotating = false;
            _autoRotateTimer?.Stop();
            if (btnAutoRotate != null)
            {
                btnAutoRotate.Background = new SolidColorBrush(Color.FromRgb(44, 44, 46)); // #2C2C2E
            }
        }

        private void BtnResetView_Click(object sender, RoutedEventArgs e)
        {
            StopAutoRotate();
            if (viewport3D.Camera is PerspectiveCamera camera)
            {
                camera.Position = new Point3D(10, 10, 10);
                camera.LookDirection = new Vector3D(-10, -10, -10);
                camera.UpDirection = new Vector3D(0, 0, 1);
                viewport3D.ZoomExtents();
            }
        }

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ScaleContainerSize(1.15);
        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ScaleContainerSize(1.0 / 1.15);
        }

        private void ScaleContainerSize(double factor)
        {
            double newWidth = Math.Max(420, Math.Min(1600, this.Width * factor));
            double newHeight = Math.Max(300, Math.Min(1200, this.Height * factor));

            this.Width = newWidth;
            this.Height = newHeight;
            _modelData.Size = new Size(this.Width, this.Height);

            PositionOrSizeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            StopAutoRotate();
            DeleteRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Corner Resize Thumb

        private void resizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newWidth = Math.Max(420, Math.Min(1600, this.Width + e.HorizontalChange));
            double newHeight = Math.Max(300, Math.Min(1200, this.Height + e.VerticalChange));

            this.Width = newWidth;
            this.Height = newHeight;
            _modelData.Size = new Size(this.Width, this.Height);

            PositionOrSizeChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Selection State & Data Management

        public void SetSelected(bool isSelected)
        {
            if (isSelected)
            {
                mainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)); // #3B82F6
                mainBorder.BorderThickness = new Thickness(3);
            }
            else
            {
                mainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246)); // #8B5CF6
                mainBorder.BorderThickness = new Thickness(2);
            }
        }

        public void UpdateModelData(Point position)
        {
            _modelData.Position = position;
            _modelData.Size = new Size(this.Width, this.Height);

            if (viewport3D.Camera is PerspectiveCamera camera)
            {
                var distanceVector = new Vector3D(camera.Position.X, camera.Position.Y, camera.Position.Z);
                _modelData.CameraDistance = distanceVector.Length;
            }
        }

        #endregion
    }
}
