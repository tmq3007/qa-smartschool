using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation; // For smooth transitions
using Microsoft.Win32;
using QASmartTouch.Services;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_20_3DModelSelector : Window
    {
        private Model3DImporter _importer;
        private Model3DGroup? _loadedModel;
        
        // Camera control state
        private double _currentZoom = 1.0;
        private double _currentRotationX = 0.0;
        private double _currentRotationY = -30.0;
        private double _currentRotationZ = 0.0;
        private const double ROTATION_STEP = 15.0; // degrees
        private const double ZOOM_STEP = 0.1;
        
        public string? SelectedModelPath { get; private set; }
        public Model3DGroup? SelectedModel => _loadedModel;
        
        public Form2_20_3DModelSelector()
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            _importer = new Model3DImporter();
            InitializeAutoRotate();
            ApplyGraphicsSettings();
        }

        private void ApplyGraphicsSettings()
        {
            int resolutionSetting = QASmartTouch.Services.AppSettings.GetRecommended3DResolution();
            var config = QASmartClass.Services.AppConfig.Load();
            if (resolutionSetting <= 20 || config.Disable3DAntiAliasing || (config.AutoOptimizeForIntegratedGraphics && QASmartClass.Services.AppConfig.IsIntegratedGraphicsDetected))
            {
                System.Windows.Media.RenderOptions.SetEdgeMode(previewViewport, System.Windows.Media.EdgeMode.Aliased);
            }
            else
            {
                System.Windows.Media.RenderOptions.SetEdgeMode(previewViewport, System.Windows.Media.EdgeMode.Unspecified);
            }
        }
        
        private void btnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Chọn file mô hình 3D",
                Filter = _importer.GetFileFilter(),
                FilterIndex = 1,
                CheckFileExists = true,
                CheckPathExists = true
            };
            
            if (dialog.ShowDialog() == true)
            {
                LoadModel(dialog.FileName);
            }
        }
        
        private void LoadModel(string filePath)
        {
            try
            {
                // Show loading state
                placeholderPanel.Visibility = Visibility.Collapsed;
                loadingPanel.Visibility = Visibility.Visible;
                previewViewport.Visibility = Visibility.Collapsed;
                modelInfoPanel.Visibility = Visibility.Collapsed;
                cameraPresetsPanel.Visibility = Visibility.Collapsed;
                btnInsert.IsEnabled = false;
                
                // Load model on UI thread (WPF 3D objects must be created on UI thread)
                _loadedModel = _importer.LoadModel(filePath);
                
                if (_loadedModel != null)
                {
                    // Update preview
                    previewModel.Content = _loadedModel;
                    
                    // Show viewport
                    loadingPanel.Visibility = Visibility.Collapsed;
                    previewViewport.Visibility = Visibility.Visible;
                    modelInfoPanel.Visibility = Visibility.Visible;
                    cameraPresetsPanel.Visibility = Visibility.Visible;
                    
                    // Update model info
                    var fileInfo = new FileInfo(filePath);
                    txtFileName.Text = $"📄 {fileInfo.Name}";
                    txtFileSize.Text = $"💾 {FormatFileSize(fileInfo.Length)}";
                    txtVertexCount.Text = $"🔺 {CountVertices(_loadedModel)} vertices";
                    
                    // Store selected path
                    SelectedModelPath = filePath;
                    
                    // Enable insert button
                    btnInsert.IsEnabled = true;
                    
                    // Enable export button (Phase 3)
                    btnExportPNG.IsEnabled = true;
                    
                    // Zoom to fit
                    previewViewport.ZoomExtents();
                    
                    // Initialize camera with current settings
                    UpdateCamera();
                    
                    // Setup custom lighting (Phase 2)
                    SetupCustomLighting();
                    
                    System.Diagnostics.Debug.WriteLine($"[3DModelSelector] Model loaded successfully: {filePath}");
                }
                else
                {
                    // Show error
                    loadingPanel.Visibility = Visibility.Collapsed;
                    placeholderPanel.Visibility = Visibility.Visible;
                    
                    MessageBox.Show(
                        "Không thể tải mô hình 3D.\nVui lòng kiểm tra định dạng file và thử lại.",
                        "Lỗi tải mô hình",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    
                    System.Diagnostics.Debug.WriteLine($"[3DModelSelector] Failed to load model: {filePath}");
                }
            }
            catch (Exception ex)
            {
                // Show error
                loadingPanel.Visibility = Visibility.Collapsed;
                placeholderPanel.Visibility = Visibility.Visible;
                
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi tải mô hình:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                
                System.Diagnostics.Debug.WriteLine($"[3DModelSelector] Exception: {ex}");
            }
        }
        
        #region Camera Controls
        
        private void UpdateCamera()
        {
            if (previewViewport?.Camera is PerspectiveCamera camera)
            {
                // Convert angles to radians
                double radX = _currentRotationX * Math.PI / 180.0;
                double radY = _currentRotationY * Math.PI / 180.0;
                double radZ = _currentRotationZ * Math.PI / 180.0;
                
                // Calculate camera position using spherical coordinates
                double distance = 20.0 * _currentZoom;
                
                double x = distance * Math.Cos(radY) * Math.Sin(radX);
                double y = distance * Math.Sin(radY);
                double z = distance * Math.Cos(radY) * Math.Cos(radX);
                
                camera.Position = new System.Windows.Media.Media3D.Point3D(x, y, z);
                camera.LookDirection = new System.Windows.Media.Media3D.Vector3D(-x, -y, -z);
                
                // Apply Rotation Z (roll/tilt) to UpDirection
                // Default up is (0, 0, 1), rotate it around the look direction
                var defaultUp = new System.Windows.Media.Media3D.Vector3D(0, 0, 1);
                
                if (Math.Abs(_currentRotationZ) > 0.01) // Only calculate if rotation Z is not zero
                {
                    // Normalize look direction
                    var lookDir = camera.LookDirection;
                    lookDir.Normalize();
                    
                    // Rotate defaultUp around lookDir by radZ angle
                    // Using Rodrigues' rotation formula
                    var cosZ = Math.Cos(radZ);
                    var sinZ = Math.Sin(radZ);
                    
                    var rotatedUp = new System.Windows.Media.Media3D.Vector3D(
                        defaultUp.X * cosZ + 
                        (lookDir.Y * defaultUp.Z - lookDir.Z * defaultUp.Y) * sinZ +
                        lookDir.X * (lookDir.X * defaultUp.X + lookDir.Y * defaultUp.Y + lookDir.Z * defaultUp.Z) * (1 - cosZ),
                        
                        defaultUp.Y * cosZ + 
                        (lookDir.Z * defaultUp.X - lookDir.X * defaultUp.Z) * sinZ +
                        lookDir.Y * (lookDir.X * defaultUp.X + lookDir.Y * defaultUp.Y + lookDir.Z * defaultUp.Z) * (1 - cosZ),
                        
                        defaultUp.Z * cosZ + 
                        (lookDir.X * defaultUp.Y - lookDir.Y * defaultUp.X) * sinZ +
                        lookDir.Z * (lookDir.X * defaultUp.X + lookDir.Y * defaultUp.Y + lookDir.Z * defaultUp.Z) * (1 - cosZ)
                    );
                    
                    camera.UpDirection = rotatedUp;
                }
                else
                {
                    camera.UpDirection = defaultUp;
                }
                
                System.Diagnostics.Debug.WriteLine($"[Camera] Zoom: {_currentZoom:F2}, RotX: {_currentRotationX:F0}°, RotY: {_currentRotationY:F0}°, RotZ: {_currentRotationZ:F0}°");
            }
        }
        
        #region Mouse Interaction
        
        private bool _isLeftDragging = false;
        private bool _isRightDragging = false;
        private System.Windows.Point _lastMousePosition;
        private const double MOUSE_ROTATION_SENSITIVITY = 0.5; // degrees per pixel
        private const double MOUSE_PAN_SENSITIVITY = 0.05; // units per pixel
        
        private void PreviewViewport_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                _isLeftDragging = true;
                _lastMousePosition = e.GetPosition(previewViewport);
                previewViewport.CaptureMouse();
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine("[Mouse] Left drag started");
            }
            else if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                _isRightDragging = true;
                _lastMousePosition = e.GetPosition(previewViewport);
                previewViewport.CaptureMouse();
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine("[Mouse] Right drag started");
            }
        }
        
        private void PreviewViewport_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isLeftDragging && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(previewViewport);
                var delta = currentPosition - _lastMousePosition;
                
                // Update rotation based on mouse movement
                // Horizontal movement -> Rotation X (left/right)
                // Vertical movement -> Rotation Y (up/down)
                _currentRotationX += delta.X * MOUSE_ROTATION_SENSITIVITY;
                _currentRotationY -= delta.Y * MOUSE_ROTATION_SENSITIVITY; // Inverted for natural feel
                
                // Clamp rotations to slider ranges
                _currentRotationX = Math.Max(-180, Math.Min(180, _currentRotationX));
                _currentRotationY = Math.Max(-180, Math.Min(180, _currentRotationY));
                
                // Update sliders to reflect mouse changes
                rotationXSlider.Value = _currentRotationX;
                rotationYSlider.Value = _currentRotationY;
                
                // UpdateCamera() will be called by slider ValueChanged events
                
                _lastMousePosition = currentPosition;
                e.Handled = true;
            }
            else if (_isRightDragging && e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(previewViewport);
                var delta = currentPosition - _lastMousePosition;
                
                // Implement camera panning
                if (previewViewport?.Camera is PerspectiveCamera camera)
                {
                    // Get camera's right and up vectors
                    var lookDir = camera.LookDirection;
                    lookDir.Normalize();
                    
                    var upDir = camera.UpDirection;
                    upDir.Normalize();
                    
                    // Calculate right vector (perpendicular to look and up)
                    var rightDir = System.Windows.Media.Media3D.Vector3D.CrossProduct(lookDir, upDir);
                    rightDir.Normalize();
                    
                    // Recalculate up vector to ensure orthogonality
                    upDir = System.Windows.Media.Media3D.Vector3D.CrossProduct(rightDir, lookDir);
                    upDir.Normalize();
                    
                    // Pan camera
                    double panSpeed = MOUSE_PAN_SENSITIVITY * _currentZoom;
                    var panOffset = rightDir * (-delta.X * panSpeed) + upDir * (delta.Y * panSpeed);
                    
                    camera.Position += panOffset;
                    
                    System.Diagnostics.Debug.WriteLine($"[Mouse] Pan: {delta.X:F1}, {delta.Y:F1}");
                }
                
                _lastMousePosition = currentPosition;
                e.Handled = true;
            }
        }
        
        private void PreviewViewport_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isLeftDragging)
            {
                _isLeftDragging = false;
                previewViewport.ReleaseMouseCapture();
                System.Diagnostics.Debug.WriteLine("[Mouse] Left drag ended");
            }
            else if (_isRightDragging)
            {
                _isRightDragging = false;
                previewViewport.ReleaseMouseCapture();
                System.Diagnostics.Debug.WriteLine("[Mouse] Right drag ended");
            }
        }
        
        private void PreviewViewport_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            // Zoom in/out with mouse wheel
            double zoomDelta = e.Delta > 0 ? -0.1 : 0.1; // Inverted for natural feel
            _currentZoom = Math.Max(0.2, Math.Min(5.0, _currentZoom + zoomDelta));
            
            zoomSlider.Value = _currentZoom;
            // UpdateCamera() will be called by slider ValueChanged event
            
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine($"[Mouse] Wheel zoom: {_currentZoom:F2}");
        }
        
        #endregion
        
        #region Auto-Rotate
        
        private System.Windows.Threading.DispatcherTimer? _autoRotateTimer;
        private const double AUTO_ROTATE_SPEED = 0.5; // degrees per tick
        private const int AUTO_ROTATE_INTERVAL_MS = 16; // ~60 FPS
        
        private void InitializeAutoRotate()
        {
            _autoRotateTimer = new System.Windows.Threading.DispatcherTimer();
            _autoRotateTimer.Interval = TimeSpan.FromMilliseconds(AUTO_ROTATE_INTERVAL_MS);
            _autoRotateTimer.Tick += AutoRotateTimer_Tick;
        }
        
        private void AutoRotateTimer_Tick(object? sender, EventArgs e)
        {
            // Get speed multiplier from slider
            double speedMultiplier = autoRotateSpeedSlider?.Value ?? 0.5;
            double speed = speedMultiplier * AUTO_ROTATE_SPEED;
            
            // Pause auto-rotate in Top/Bottom views (RotY = ±90°) only for X rotation
            if (rbRotateX.IsChecked == true && Math.Abs(Math.Abs(_currentRotationY) - 90) < 1)
            {
                System.Diagnostics.Debug.WriteLine("[AutoRotate] Paused in Top/Bottom view (X rotation)");
                return;
            }
            
            // Determine which axis to rotate based on radio button selection
            if (rbRotateX.IsChecked == true)
            {
                _currentRotationX += speed;
                if (_currentRotationX > 180)
                    _currentRotationX -= 360;
                rotationXSlider.Value = _currentRotationX;
            }
            else if (rbRotateY.IsChecked == true)
            {
                _currentRotationY += speed;
                if (_currentRotationY > 180)
                    _currentRotationY -= 360;
                rotationYSlider.Value = _currentRotationY;
            }
            else if (rbRotateZ.IsChecked == true)
            {
                _currentRotationZ += speed;
                if (_currentRotationZ > 180)
                    _currentRotationZ -= 360;
                rotationZSlider.Value = _currentRotationZ;
            }
        }
        
        private void AutoRotateSpeedSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtAutoRotateSpeed != null)
            {
                txtAutoRotateSpeed.Text = $"{e.NewValue:F1}x";
                System.Diagnostics.Debug.WriteLine($"[AutoRotate] Speed changed to {e.NewValue:F1}x");
            }
        }
        
        private void ChkAutoRotate_Changed(object sender, RoutedEventArgs e)
        {
            if (chkAutoRotate.IsChecked == true)
            {
                // Show rotation axis selection
                rotationAxisPanel.Visibility = Visibility.Visible;
                
                _autoRotateTimer?.Start();
                System.Diagnostics.Debug.WriteLine("[AutoRotate] Started");
            }
            else
            {
                // Hide rotation axis selection
                rotationAxisPanel.Visibility = Visibility.Collapsed;
                
                _autoRotateTimer?.Stop();
                System.Diagnostics.Debug.WriteLine("[AutoRotate] Stopped");
            }
        }
        
        #endregion
        
        // Zoom Controls
        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            zoomSlider.Value = Math.Max(zoomSlider.Minimum, zoomSlider.Value - ZOOM_STEP);
        }
        
        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            zoomSlider.Value = Math.Min(zoomSlider.Maximum, zoomSlider.Value + ZOOM_STEP);
        }
        
        private void ZoomSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            _currentZoom = e.NewValue;
            UpdateCamera();
        }
        
        // Rotation X Controls
        private void BtnRotateLeft_Click(object sender, RoutedEventArgs e)
        {
            rotationXSlider.Value -= ROTATION_STEP;
        }
        
        private void BtnRotateRight_Click(object sender, RoutedEventArgs e)
        {
            rotationXSlider.Value += ROTATION_STEP;
        }
        
        private void RotationXSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            _currentRotationX = e.NewValue;
            UpdateCamera();
        }
        
        // Rotation Y Controls
        private void BtnRotateUp_Click(object sender, RoutedEventArgs e)
        {
            rotationYSlider.Value += ROTATION_STEP;
        }
        
        private void BtnRotateDown_Click(object sender, RoutedEventArgs e)
        {
            rotationYSlider.Value -= ROTATION_STEP;
        }
        
        private void RotationYSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            _currentRotationY = e.NewValue;
            UpdateCamera();
        }
        
        // Rotation Z Controls
        private void BtnRotateClockwise_Click(object sender, RoutedEventArgs e)
        {
            rotationZSlider.Value += ROTATION_STEP;
        }
        
        private void BtnRotateCounterClockwise_Click(object sender, RoutedEventArgs e)
        {
            rotationZSlider.Value -= ROTATION_STEP;
        }
        
        private void RotationZSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            _currentRotationZ = e.NewValue;
            UpdateCamera();
        }
        
        // Reset Camera
        private void BtnResetCamera_Click(object sender, RoutedEventArgs e)
        {
            // Animate to default view
            AnimateSlider(zoomSlider, 1.0);
            AnimateSlider(rotationXSlider, 0.0);
            AnimateSlider(rotationYSlider, -30.0);
            AnimateSlider(rotationZSlider, 0.0);
            
            System.Diagnostics.Debug.WriteLine("[Camera] Animating to default view");
        }
        
        // Camera Presets
        private void BtnTopView_Click(object sender, RoutedEventArgs e)
        {
            // Animate to Top View
            AnimateSlider(rotationXSlider, 0);
            AnimateSlider(rotationYSlider, 90);
            AnimateSlider(rotationZSlider, 0);
            AnimateSlider(zoomSlider, 1.0);
            
            System.Diagnostics.Debug.WriteLine("[Camera] Animating to Top View");
        }
        
        private void BtnFrontView_Click(object sender, RoutedEventArgs e)
        {
            // Animate to Front View
            AnimateSlider(rotationXSlider, 0);
            AnimateSlider(rotationYSlider, 0);
            AnimateSlider(rotationZSlider, 0);
            AnimateSlider(zoomSlider, 1.0);
            
            System.Diagnostics.Debug.WriteLine("[Camera] Animating to Front View");
        }
        
        private void BtnSideView_Click(object sender, RoutedEventArgs e)
        {
            // Animate to Side View
            AnimateSlider(rotationXSlider, 90);
            AnimateSlider(rotationYSlider, 0);
            AnimateSlider(rotationZSlider, 0);
            AnimateSlider(zoomSlider, 1.0);
            
            System.Diagnostics.Debug.WriteLine("[Camera] Animating to Side View");
        }
        
        // Smooth transition helper
        private void AnimateSlider(System.Windows.Controls.Slider slider, double targetValue, int durationMs = 500)
        {
            var animation = new DoubleAnimation
            {
                From = slider.Value,
                To = targetValue,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            
            slider.BeginAnimation(System.Windows.Controls.Slider.ValueProperty, animation);
        }
        
        // Toggle Control Panel
        private void BtnTogglePanel_Click(object sender, RoutedEventArgs e)
        {
            if (controlPanel.Visibility == Visibility.Visible)
            {
                controlPanel.Visibility = Visibility.Collapsed;
                btnTogglePanel.Content = "▶ Hiện";
                System.Diagnostics.Debug.WriteLine("[UI] Control panel hidden");
            }
            else
            {
                controlPanel.Visibility = Visibility.Visible;
                btnTogglePanel.Content = "◀ Ẩn";
                System.Diagnostics.Debug.WriteLine("[UI] Control panel shown");
            }
        }
        
        #endregion
        
        #region Phase 2: Visual Enhancements
        
        // Lighting fields
        private AmbientLight? _ambientLight;
        private DirectionalLight? _keyLight;
        private DirectionalLight? _fillLight;
        private ModelVisual3D? _customLightingVisual;
        
        // Grid field
        private HelixToolkit.Wpf.GridLinesVisual3D? _gridLines;
        
        // Current material properties
        private System.Windows.Media.Color _currentModelColor = System.Windows.Media.Color.FromRgb(204, 204, 204);
        private double _currentOpacity = 1.0;
        private double _currentShininess = 40.0;
        
        private void SetupCustomLighting()
        {
            // Remove default lights first
            var defaultLights = previewViewport.Children.OfType<HelixToolkit.Wpf.DefaultLights>().ToList();
            foreach (var light in defaultLights)
            {
                previewViewport.Children.Remove(light);
            }
            
            var lightGroup = new Model3DGroup();
            
            // Ambient Light
            _ambientLight = new AmbientLight(System.Windows.Media.Color.FromArgb(
                (byte)(255 * ambientSlider.Value), 255, 255, 255
            ));
            lightGroup.Children.Add(_ambientLight);
            
            // Key Light (main directional)
            _keyLight = new DirectionalLight(System.Windows.Media.Color.FromArgb(
                (byte)(255 * keyLightSlider.Value), 255, 255, 255
            ), new System.Windows.Media.Media3D.Vector3D(5, 5, 10));
            lightGroup.Children.Add(_keyLight);
            
            // Fill Light (softer, from back)
            _fillLight = new DirectionalLight(System.Windows.Media.Color.FromArgb(
                (byte)(255 * fillLightSlider.Value), 255, 255, 255
            ), new System.Windows.Media.Media3D.Vector3D(-5, -5, -10));
            lightGroup.Children.Add(_fillLight);
            
            // Add to viewport
            _customLightingVisual = new ModelVisual3D { Content = lightGroup };
            previewViewport.Children.Add(_customLightingVisual);
            
            System.Diagnostics.Debug.WriteLine("[Lighting] Custom lighting setup complete");
        }
        
        // Lighting Controls
        private void AmbientSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (_ambientLight != null)
            {
                byte intensity = (byte)(255 * e.NewValue);
                _ambientLight.Color = System.Windows.Media.Color.FromArgb(intensity, 255, 255, 255);
                System.Diagnostics.Debug.WriteLine($"[Lighting] Ambient: {e.NewValue:F2}");
            }
        }
        
        private void KeyLightSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (_keyLight != null)
            {
                byte intensity = (byte)(255 * e.NewValue);
                _keyLight.Color = System.Windows.Media.Color.FromArgb(intensity, 255, 255, 255);
                System.Diagnostics.Debug.WriteLine($"[Lighting] Key Light: {e.NewValue:F2}");
            }
        }
        
        private void FillLightSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (_fillLight != null)
            {
                byte intensity = (byte)(255 * e.NewValue);
                _fillLight.Color = System.Windows.Media.Color.FromArgb(intensity, 255, 255, 255);
                System.Diagnostics.Debug.WriteLine($"[Lighting] Fill Light: {e.NewValue:F2}");
            }
        }
        
        // Material Controls
        private void BtnColorPicker_Click(object sender, RoutedEventArgs e)
        {
            // Cycle through predefined colors
            var colors = new[]
            {
                System.Windows.Media.Colors.Gray,
                System.Windows.Media.Colors.Red,
                System.Windows.Media.Colors.Blue,
                System.Windows.Media.Colors.Green,
                System.Windows.Media.Colors.Yellow,
                System.Windows.Media.Colors.Orange,
                System.Windows.Media.Colors.Purple,
                System.Windows.Media.Colors.Brown,
                System.Windows.Media.Colors.White,
                System.Windows.Media.Colors.Black
            };
            
            // Find current color index
            int currentIndex = -1;
            for (int i = 0; i < colors.Length; i++)
            {
                if (colors[i] == _currentModelColor)
                {
                    currentIndex = i;
                    break;
                }
            }
            
            // Move to next color
            int nextIndex = (currentIndex + 1) % colors.Length;
            _currentModelColor = colors[nextIndex];
            
            colorPreview.Background = new System.Windows.Media.SolidColorBrush(_currentModelColor);
            ApplyMaterialToModel();
            
            System.Diagnostics.Debug.WriteLine($"[Material] Color changed to #{_currentModelColor.R:X2}{_currentModelColor.G:X2}{_currentModelColor.B:X2}");
        }
        
        private void OpacitySlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            _currentOpacity = e.NewValue;
            ApplyMaterialToModel();
            System.Diagnostics.Debug.WriteLine($"[Material] Opacity: {e.NewValue:F2}");
        }
        
        private void ShininessSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            _currentShininess = e.NewValue;
            ApplyMaterialToModel();
            System.Diagnostics.Debug.WriteLine($"[Material] Shininess: {e.NewValue:F0}");
        }
        
        private void ApplyMaterialToModel()
        {
            if (_loadedModel == null) return;
            
            foreach (var child in _loadedModel.Children)
            {
                if (child is GeometryModel3D geometryModel)
                {
                    var materialGroup = new MaterialGroup();
                    
                    // Diffuse material
                    var diffuseBrush = new System.Windows.Media.SolidColorBrush(_currentModelColor) 
                    { 
                        Opacity = _currentOpacity 
                    };
                    materialGroup.Children.Add(new DiffuseMaterial(diffuseBrush));
                    
                    // Specular material
                    var specularBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                    materialGroup.Children.Add(new SpecularMaterial(specularBrush, _currentShininess));
                    
                    geometryModel.Material = materialGroup;
                    geometryModel.BackMaterial = materialGroup;
                }
            }
        }
        
        // Display Options
        private void ChkShowGrid_Changed(object sender, RoutedEventArgs e)
        {
            if (chkShowGrid.IsChecked == true)
            {
                if (_gridLines == null)
                {
                    _gridLines = new HelixToolkit.Wpf.GridLinesVisual3D
                    {
                        Center = new System.Windows.Media.Media3D.Point3D(0, 0, 0),
                        MinorDistance = 1,
                        MajorDistance = 5,
                        Width = 20,
                        Length = 20,
                        Thickness = 0.02,
                        Fill = System.Windows.Media.Brushes.LightGray
                    };
                    previewViewport.Children.Add(_gridLines);
                }
                _gridLines.Visible = true;
                System.Diagnostics.Debug.WriteLine("[Display] Grid shown");
            }
            else
            {
                if (_gridLines != null)
                {
                    _gridLines.Visible = false;
                    System.Diagnostics.Debug.WriteLine("[Display] Grid hidden");
                }
            }
        }
        
        private void ChkWireframe_Changed(object sender, RoutedEventArgs e)
        {
            if (_loadedModel == null) return;
            
            bool isWireframe = chkWireframe.IsChecked == true;
            
            foreach (var child in _loadedModel.Children)
            {
                if (child is GeometryModel3D geometryModel)
                {
                    if (isWireframe)
                    {
                        // Create wireframe material (thin lines)
                        var wireframeBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                        var wireframeMaterial = new DiffuseMaterial(wireframeBrush);
                        geometryModel.Material = wireframeMaterial;
                        geometryModel.BackMaterial = wireframeMaterial;
                    }
                    else
                    {
                        // Restore normal material
                        ApplyMaterialToModel();
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[Display] Wireframe: {isWireframe}");
        }
        
        private void BtnBgColorPicker_Click(object sender, RoutedEventArgs e)
        {
            // Cycle through predefined background colors
            var bgColors = new[]
            {
                System.Windows.Media.Color.FromRgb(249, 249, 249), // Light gray (default)
                System.Windows.Media.Colors.White,
                System.Windows.Media.Color.FromRgb(240, 240, 240), // Very light gray
                System.Windows.Media.Color.FromRgb(220, 230, 240), // Light blue
                System.Windows.Media.Color.FromRgb(240, 240, 230), // Light yellow
                System.Windows.Media.Color.FromRgb(230, 240, 230), // Light green
                System.Windows.Media.Color.FromRgb(50, 50, 50),    // Dark gray
                System.Windows.Media.Colors.Black
            };
            
            var currentBg = previewViewport.Background as System.Windows.Media.SolidColorBrush;
            var currentColor = currentBg?.Color ?? bgColors[0];
            
            // Find current color index
            int currentIndex = -1;
            for (int i = 0; i < bgColors.Length; i++)
            {
                if (bgColors[i] == currentColor)
                {
                    currentIndex = i;
                    break;
                }
            }
            
            // Move to next color
            int nextIndex = (currentIndex + 1) % bgColors.Length;
            var newColor = bgColors[nextIndex];
            
            bgColorPreview.Background = new System.Windows.Media.SolidColorBrush(newColor);
            previewViewport.Background = new System.Windows.Media.SolidColorBrush(newColor);
            
            System.Diagnostics.Debug.WriteLine($"[Display] Background color changed to #{newColor.R:X2}{newColor.G:X2}{newColor.B:X2}");
        }
        
        #endregion
        
        #region Phase 3: Advanced Features - Export
        
        private void BtnExportPNG_Click(object sender, RoutedEventArgs e)
        {
            if (_loadedModel == null)
            {
                MessageBox.Show(
                    "Chưa có mô hình 3D để xuất.\nVui lòng tải mô hình trước.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }
            
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Xuất ảnh PNG",
                Filter = "PNG Image|*.png",
                DefaultExt = ".png",
                FileName = $"3DModel_{DateTime.Now:yyyyMMdd_HHmmss}.png"
            };
            
            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    ExportViewportToPNG(saveDialog.FileName);
                    
                    MessageBox.Show(
                        $"Đã xuất ảnh thành công!\n\nĐường dẫn:\n{saveDialog.FileName}",
                        "Thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    
                    System.Diagnostics.Debug.WriteLine($"[Export] PNG saved to: {saveDialog.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Lỗi khi xuất ảnh:\n{ex.Message}",
                        "Lỗi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    
                    System.Diagnostics.Debug.WriteLine($"[Export] Error: {ex}");
                }
            }
        }
        
        private void ExportViewportToPNG(string filePath)
        {
            // Get the viewport's actual size
            var width = (int)previewViewport.ActualWidth;
            var height = (int)previewViewport.ActualHeight;
            
            if (width <= 0 || height <= 0)
            {
                throw new InvalidOperationException("Viewport has invalid dimensions");
            }
            
            // Create a render target bitmap
            var renderBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                width,
                height,
                96, // DPI X
                96, // DPI Y
                System.Windows.Media.PixelFormats.Pbgra32
            );
            
            // Render the viewport
            renderBitmap.Render(previewViewport);
            
            // Create PNG encoder
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(renderBitmap));
            
            // Save to file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
            
            System.Diagnostics.Debug.WriteLine($"[Export] Exported {width}x{height} PNG");
        }
        
        #endregion
        
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
        
        private int CountVertices(Model3DGroup modelGroup)
        {
            int count = 0;
            
            foreach (var model in modelGroup.Children)
            {
                if (model is GeometryModel3D geometryModel &&
                    geometryModel.Geometry is MeshGeometry3D mesh)
                {
                    count += mesh.Positions.Count;
                }
            }
            
            return count;
        }
        
        private void btnInsert_Click(object sender, RoutedEventArgs e)
        {
            if (_loadedModel != null && !string.IsNullOrEmpty(SelectedModelPath))
            {
                DialogResult = true;
                Close();
            }
        }
        
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
