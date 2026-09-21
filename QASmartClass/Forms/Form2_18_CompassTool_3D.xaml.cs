using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using QASmartTouch.Services;
using HelixToolkit.Wpf;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

// Aliases to resolve conflict with custom Point3D/Vector3D in Form2_6_3DCylinderEditor
using WpfPoint3D = System.Windows.Media.Media3D.Point3D;
using WpfVector3D = System.Windows.Media.Media3D.Vector3D;
using WpfSlider = System.Windows.Controls.Slider;

namespace QASmartTouch.Forms
{
    public partial class Form2_18_CompassTool_3D : Window
    {
        private Compass3DBuilder _compassBuilder;
        private Compass3DState _state;
        private bool _isAnimating = false;

        public Form2_18_CompassTool_3D()
        {
            InitializeComponent();
            // QC_4.2_TOUCH_PIPELINE: STEM Window — WPF tự cô lập, KHÔNG cần ApplyTouchIsolation
            QASmartTouch.Helpers.TouchActivationHelper.Apply(this); // QC_4.2_TOUCH_ACTIVATION: Fix "nhấn 2 lần mới kéo được" trên IFP
            
            _compassBuilder = new Compass3DBuilder();
            _state = new Compass3DState();
            ApplyGraphicsSettings();
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Build initial compass model
            BuildCompassModel();
            
            // Set initial camera position
            SetCameraView(CameraView.Isometric);
        }

        private void BuildCompassModel()
        {
            try
            {
                // Update builder with current state
                _compassBuilder.OpeningAngle = _state.OpeningAngle;
                _compassBuilder.Radius = _state.Radius;

                // Build the 3D model
                var compassModel = _compassBuilder.Build();

                // Clear existing model
                compassModelVisual.Content = null;

                // Add new model
                compassModelVisual.Content = compassModel;

                System.Diagnostics.Debug.WriteLine($"Compass built: Angle={_state.OpeningAngle}°, Radius={_state.Radius}mm");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error building compass: {ex.Message}", "Build Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Slider Controls

        private void SliderAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            _state.OpeningAngle = e.NewValue;
            txtAngleValue.Text = $"{e.NewValue:F0}°";
            
            BuildCompassModel();
        }

        private void SliderRadius_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            _state.Radius = e.NewValue;
            txtRadiusValue.Text = $"{e.NewValue:F0} mm";
            
            BuildCompassModel();
        }

        #endregion

        #region Camera Controls

        private enum CameraView
        {
            Top,
            Front,
            Side,
            Isometric
        }

        private void SetCameraView(CameraView view)
        {
            var camera = viewport3D.Camera as PerspectiveCamera;
            if (camera == null) return;

            WpfPoint3D position;
            WpfVector3D lookDirection;
            WpfVector3D upDirection;

            switch (view)
            {
                case CameraView.Top:
                    position = new WpfPoint3D(0, 0, 200);
                    lookDirection = new WpfVector3D(0, 0, -1);
                    upDirection = new WpfVector3D(0, 1, 0);
                    break;

                case CameraView.Front:
                    position = new WpfPoint3D(0, -200, 0);
                    lookDirection = new WpfVector3D(0, 1, 0);
                    upDirection = new WpfVector3D(0, 0, 1);
                    break;

                case CameraView.Side:
                    position = new WpfPoint3D(200, 0, 0);
                    lookDirection = new WpfVector3D(-1, 0, 0);
                    upDirection = new WpfVector3D(0, 0, 1);
                    break;

                case CameraView.Isometric:
                default:
                    position = new WpfPoint3D(-150, -150, 100);
                    lookDirection = new WpfVector3D(150, 150, -100);
                    upDirection = new WpfVector3D(0, 0, 1);
                    break;
            }

            AnimateCamera(camera, position, lookDirection, upDirection);
        }

        private void AnimateCamera(PerspectiveCamera camera, WpfPoint3D newPosition, 
            WpfVector3D newLookDirection, WpfVector3D newUpDirection)
        {
            var duration = TimeSpan.FromMilliseconds(500);

            // Animate Position
            var positionAnimation = new Point3DAnimation
            {
                From = camera.Position,
                To = newPosition,
                Duration = duration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            // Animate LookDirection
            var lookAnimation = new Vector3DAnimation
            {
                From = camera.LookDirection,
                To = newLookDirection,
                Duration = duration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            // Animate UpDirection
            var upAnimation = new Vector3DAnimation
            {
                From = camera.UpDirection,
                To = newUpDirection,
                Duration = duration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            camera.BeginAnimation(PerspectiveCamera.PositionProperty, positionAnimation);
            camera.BeginAnimation(PerspectiveCamera.LookDirectionProperty, lookAnimation);
            camera.BeginAnimation(PerspectiveCamera.UpDirectionProperty, upAnimation);
        }

        private void BtnViewTop_Click(object sender, RoutedEventArgs e)
        {
            SetCameraView(CameraView.Top);
        }

        private void BtnViewFront_Click(object sender, RoutedEventArgs e)
        {
            SetCameraView(CameraView.Front);
        }

        private void BtnViewSide_Click(object sender, RoutedEventArgs e)
        {
            SetCameraView(CameraView.Side);
        }

        private void BtnViewIso_Click(object sender, RoutedEventArgs e)
        {
            SetCameraView(CameraView.Isometric);
        }

        private void BtnViewReset_Click(object sender, RoutedEventArgs e)
        {
            sliderAngle.Value = 60;
            sliderRadius.Value = 50;
            SetCameraView(CameraView.Isometric);
        }

        #endregion

        #region Action Buttons

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg|All Files|*.*",
                    DefaultExt = "png",
                    FileName = $"Compass3D_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    ExportViewportToImage(dialog.FileName);
                    MessageBox.Show($"Exported successfully to:\n{dialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportViewportToImage(string filePath)
        {
            var viewport = viewport3D.Viewport;
            var renderBitmap = new RenderTargetBitmap(
                (int)viewport.ActualWidth,
                (int)viewport.ActualHeight,
                96, 96,
                PixelFormats.Pbgra32);

            renderBitmap.Render(viewport);

            BitmapEncoder encoder;
            if (filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            {
                encoder = new JpegBitmapEncoder { QualityLevel = 95 };
            }
            else
            {
                encoder = new PngBitmapEncoder();
            }

            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using (var stream = System.IO.File.Create(filePath))
            {
                encoder.Save(stream);
            }
        }

        private void BtnAnimate_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimating)
            {
                StopAnimation();
                return;
            }

            _isAnimating = true;
            AnimateCompassOpening();
        }

        private void AnimateCompassOpening()
        {
            var duration = TimeSpan.FromSeconds(3);

            // Animate opening angle from 10° to 150° and back
            var angleAnimation = new DoubleAnimationUsingKeyFrames
            {
                Duration = duration,
                RepeatBehavior = RepeatBehavior.Forever,
                AutoReverse = true
            };

            angleAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(10, KeyTime.FromPercent(0)));
            angleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(150, KeyTime.FromPercent(1))
            {
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });

            sliderAngle.BeginAnimation(WpfSlider.ValueProperty, angleAnimation);
        }

        private void StopAnimation()
        {
            _isAnimating = false;
            sliderAngle.BeginAnimation(WpfSlider.ValueProperty, null);
        }

        private void BtnInfo_Click(object sender, RoutedEventArgs e)
        {
            var info = $@"🔧 Professional Compass Tool - 3D Demo

📐 Technical Specifications:
• Opening Angle: 10° - 180°
• Drawing Radius: 1cm - 20cm
• Components: 22 precision zones
• Material: Steel, Aluminum, Brass

🎨 Design Features:
• Group A: Pencil Assembly (Zones 1-11)
  - Eraser (red rubber)
  - Metal ferrule
  - Yellow pencil body
  - Wood tip with graphite lead
  
• Group B: Needle Assembly (Zones 12-18)
  - Precision needle tip
  - Oval compass arm
  - Adjustable joints
  
• Group C: Center Joint (Zones 19-22)
  - Rotating discs
  - Central axis
  - Smooth operation

🎮 Controls:
• Drag: Rotate view (trackball mode)
• Scroll: Zoom in/out
• Sliders: Adjust angle and radius
• Camera buttons: Preset views

Current Settings:
• Angle: {_state.OpeningAngle:F1}°
• Radius: {_state.Radius:F1} mm
• Rendering: Real-time 3D

Built with HelixToolkit.Wpf
© 2025 QA Education Technology";

            MessageBox.Show(info, "Compass Tool Information", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Window Controls

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized 
                    ? WindowState.Normal 
                    : WindowState.Maximized;
            }
            else
            {
                DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            StopAnimation();
            Close();
        }

        #endregion
    }
}
