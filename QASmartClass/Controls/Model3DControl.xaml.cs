using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using QASmartTouch.Models;

namespace QASmartTouch.Controls
{
    /// <summary>
    /// Pure Graph 3D Style - Simple, intuitive 3D model viewer with manual camera control
    /// </summary>
    public partial class Model3DControl : UserControl
    {
        private Model3DObject _modelData;
        private Model3DGroup? _model3D;
        
        public event EventHandler? DeleteRequested;
        
        public Model3DObject ModelData => _modelData;
        public Model3DGroup? Model3D => _model3D;
        
        public Model3DControl(string modelPath, Model3DGroup model3D)
        {
            InitializeComponent();
            
            _modelData = new Model3DObject
            {
                FilePath = modelPath,
                FileName = System.IO.Path.GetFileName(modelPath),
                ImportedDate = DateTime.Now,
                Size = new Size(this.Width, this.Height)
            };
            
            _model3D = model3D;
            
            // Set model to viewport
            modelVisual.Content = _model3D;
            
            // Update title
            txtTitle.Text = $"📦 {_modelData.FileName}";
            
            // Zoom to fit
            viewport3D.ZoomExtents();
            
            // Enable dragging from title bar
            InitializeDragSupport();
            
            // Enable manual camera control
            InitializeCameraControl();
            
            ApplyGraphicsSettings();

            System.Diagnostics.Debug.WriteLine($"[Model3DControl] Created for: {_modelData.FileName}");
        }

        private void ApplyGraphicsSettings()
        {
            int resolutionSetting = QASmartTouch.Services.AppSettings.GetRecommended3DResolution();
            var config = QASmartClass.Services.AppConfig.Load();
            if (resolutionSetting <= 20 || config.Disable3DAntiAliasing || (config.AutoOptimizeForIntegratedGraphics && QASmartClass.Services.AppConfig.IsIntegratedGraphicsDetected))
            {
                System.Windows.Media.RenderOptions.SetEdgeMode(viewport3D, System.Windows.Media.EdgeMode.Aliased);
            }
            else
            {
                System.Windows.Media.RenderOptions.SetEdgeMode(viewport3D, System.Windows.Media.EdgeMode.Unspecified);
            }
        }
        
        #region Drag Support (from Title Bar)
        
        private bool _isDragging;
        private Point _dragStartPoint;
        
        private void InitializeDragSupport()
        {
            titleBar.MouseLeftButtonDown += TitleBar_MouseLeftButtonDown;
            titleBar.MouseMove += TitleBar_MouseMove;
            titleBar.MouseLeftButtonUp += TitleBar_MouseLeftButtonUp;
        }
        
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this.Parent as UIElement);
            titleBar.CaptureMouse();
            e.Handled = true;
        }
        
        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPoint = e.GetPosition(this.Parent as UIElement);
                var offset = currentPoint - _dragStartPoint;
                
                var left = Canvas.GetLeft(this);
                var top = Canvas.GetTop(this);
                
                Canvas.SetLeft(this, left + offset.X);
                Canvas.SetTop(this, top + offset.Y);
                
                _dragStartPoint = currentPoint;
                e.Handled = true;
            }
        }
        
        private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                titleBar.ReleaseMouseCapture();
                
                // Update model data with new position
                var position = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
                UpdateModelData(position);
                
                e.Handled = true;
            }
        }
        
        #endregion
        
        #region Manual Camera Control (Trackball Rotation)
        
        private bool _isRotating;
        private Point _lastMousePosition;
        private Point3D _cameraPosition;
        private Vector3D _cameraLookDirection;
        private Vector3D _cameraUpDirection;
        
        private void InitializeCameraControl()
        {
            // Get initial camera state
            var camera = viewport3D.Camera as PerspectiveCamera;
            if (camera != null)
            {
                _cameraPosition = camera.Position;
                _cameraLookDirection = camera.LookDirection;
                _cameraUpDirection = camera.UpDirection;
            }
            
            // Attach mouse events to viewport
            viewport3D.MouseLeftButtonDown += Viewport_MouseLeftButtonDown;
            viewport3D.MouseMove += Viewport_MouseMove;
            viewport3D.MouseLeftButtonUp += Viewport_MouseLeftButtonUp;
            viewport3D.MouseWheel += Viewport_MouseWheel;
        }
        
        private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isRotating = true;
            _lastMousePosition = e.GetPosition(viewport3D);
            viewport3D.CaptureMouse();
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine("[Model3DControl] Started camera rotation");
        }
        
        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isRotating && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(viewport3D);
                var delta = currentPosition - _lastMousePosition;
                
                // Rotate camera based on mouse movement
                RotateCamera(delta.X, delta.Y);
                
                _lastMousePosition = currentPosition;
                e.Handled = true;
            }
        }
        
        private void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isRotating)
            {
                _isRotating = false;
                viewport3D.ReleaseMouseCapture();
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine("[Model3DControl] Stopped camera rotation");
            }
        }
        
        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var camera = viewport3D.Camera as PerspectiveCamera;
            if (camera != null)
            {
                // Zoom by moving camera closer/farther
                var zoomFactor = e.Delta > 0 ? 0.9 : 1.1;
                var direction = camera.LookDirection;
                direction.Normalize();
                
                camera.Position = new Point3D(
                    camera.Position.X + direction.X * (1 - zoomFactor) * 2,
                    camera.Position.Y + direction.Y * (1 - zoomFactor) * 2,
                    camera.Position.Z + direction.Z * (1 - zoomFactor) * 2
                );
                
                e.Handled = true;
            }
        }
        
        private void RotateCamera(double deltaX, double deltaY)
        {
            var camera = viewport3D.Camera as PerspectiveCamera;
            if (camera == null) return;
            
            // Sensitivity
            double sensitivity = 0.5;
            double angleX = deltaY * sensitivity;
            double angleY = deltaX * sensitivity;
            
            // Get current camera vectors
            var position = camera.Position;
            var lookDirection = camera.LookDirection;
            var upDirection = camera.UpDirection;
            
            // Calculate target point (where camera is looking)
            var target = new Point3D(
                position.X + lookDirection.X,
                position.Y + lookDirection.Y,
                position.Z + lookDirection.Z
            );
            
            // Rotate around target
            var toCamera = new Vector3D(
                position.X - target.X,
                position.Y - target.Y,
                position.Z - target.Z
            );
            
            // Horizontal rotation (around up axis)
            var rotationY = new AxisAngleRotation3D(upDirection, angleY);
            var transformY = new RotateTransform3D(rotationY);
            toCamera = transformY.Transform(toCamera);
            
            // Vertical rotation (around right axis)
            var rightAxis = Vector3D.CrossProduct(lookDirection, upDirection);
            rightAxis.Normalize();
            var rotationX = new AxisAngleRotation3D(rightAxis, angleX);
            var transformX = new RotateTransform3D(rotationX);
            toCamera = transformX.Transform(toCamera);
            upDirection = transformX.Transform(upDirection);
            
            // Update camera
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
        
        #region Camera View Presets (Graph 3D Style)
        
        /// <summary>
        /// Camera preset: Top view (looking down Z axis)
        /// </summary>
        private void BtnTopView_Click(object sender, RoutedEventArgs e)
        {
            var camera = viewport3D.Camera as PerspectiveCamera;
            if (camera != null)
            {
                camera.Position = new Point3D(0, 0, 15);
                camera.LookDirection = new Vector3D(0, 0, -15);
                camera.UpDirection = new Vector3D(0, 1, 0);
                System.Diagnostics.Debug.WriteLine("[Model3DControl] Switched to Top View");
            }
        }
        
        /// <summary>
        /// Camera preset: Front view (looking along Y axis)
        /// </summary>
        private void BtnFrontView_Click(object sender, RoutedEventArgs e)
        {
            var camera = viewport3D.Camera as PerspectiveCamera;
            if (camera != null)
            {
                camera.Position = new Point3D(0, -15, 3);
                camera.LookDirection = new Vector3D(0, 15, -3);
                camera.UpDirection = new Vector3D(0, 0, 1);
                System.Diagnostics.Debug.WriteLine("[Model3DControl] Switched to Front View");
            }
        }
        
        /// <summary>
        /// Camera preset: Side view (looking along X axis)
        /// </summary>
        private void BtnSideView_Click(object sender, RoutedEventArgs e)
        {
            var camera = viewport3D.Camera as PerspectiveCamera;
            if (camera != null)
            {
                camera.Position = new Point3D(15, 0, 3);
                camera.LookDirection = new Vector3D(-15, 0, -3);
                camera.UpDirection = new Vector3D(0, 0, 1);
                System.Diagnostics.Debug.WriteLine("[Model3DControl] Switched to Side View");
            }
        }
        
        /// <summary>
        /// Camera preset: Reset to default isometric view
        /// </summary>
        private void BtnResetView_Click(object sender, RoutedEventArgs e)
        {
            var camera = viewport3D.Camera as PerspectiveCamera;
            if (camera != null)
            {
                camera.Position = new Point3D(10, 10, 10);
                camera.LookDirection = new Vector3D(-10, -10, -10);
                camera.UpDirection = new Vector3D(0, 0, 1);
                
                // Also zoom to fit
                viewport3D.ZoomExtents();
                
                System.Diagnostics.Debug.WriteLine("[Model3DControl] Reset to Isometric View");
            }
        }
        
        #endregion
        
        #region Delete
        
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Bạn có chắc muốn xóa mô hình 3D này?\n\n{_modelData.FileName}",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );
            
            if (result == MessageBoxResult.Yes)
            {
                DeleteRequested?.Invoke(this, EventArgs.Empty);
                System.Diagnostics.Debug.WriteLine($"[Model3DControl] Delete requested for: {_modelData.FileName}");
            }
        }
        
        #endregion
        
        #region Resize
        
        private void resizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newWidth = this.Width + e.HorizontalChange;
            double newHeight = this.Height + e.VerticalChange;
            
            // Minimum size constraints
            if (newWidth >= 500 && newWidth <= 1600)
            {
                this.Width = newWidth;
            }
            
            if (newHeight >= 400 && newHeight <= 1200)
            {
                this.Height = newHeight;
            }
            
            _modelData.Size = new Size(this.Width, this.Height);
            System.Diagnostics.Debug.WriteLine($"[Model3DControl] Resized to: {this.Width}x{this.Height}");
        }
        
        #endregion
        
        #region Selection State
        
        public void SetSelected(bool isSelected)
        {
            if (isSelected)
            {
                mainBorder.BorderBrush = System.Windows.Media.Brushes.Blue;
                mainBorder.BorderThickness = new Thickness(3);
                resizeThumb.Visibility = Visibility.Visible;
            }
            else
            {
                mainBorder.BorderBrush = System.Windows.Media.Brushes.LightGray;
                mainBorder.BorderThickness = new Thickness(1);
                resizeThumb.Visibility = Visibility.Collapsed;
            }
        }
        
        #endregion
        
        #region Data Management
        
        public void UpdateModelData(Point position)
        {
            _modelData.Position = position;
            _modelData.Size = new Size(this.Width, this.Height);
            
            // Get camera data
            var camera = viewport3D.Camera as PerspectiveCamera;
            if (camera != null)
            {
                var distanceVector = new Vector3D(camera.Position.X, camera.Position.Y, camera.Position.Z);
                _modelData.CameraDistance = distanceVector.Length;
            }
        }
        
        #endregion
    }
}
