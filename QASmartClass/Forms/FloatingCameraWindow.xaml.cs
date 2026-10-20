using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QASmartTouch.Services.Camera;

namespace QASmartTouch.Forms
{
    public partial class FloatingCameraWindow : Window
    {
        private static FloatingCameraWindow? _currentInstance;
        public static FloatingCameraWindow? CurrentInstance => _currentInstance;

        private CameraPreviewService? _cameraService;
        private int _shapeMode = 0; // 0 = Tròn (Circle), 1 = Vuông (Square bo góc), 2 = Chữ nhật (Rectangle 16:9)
        private int _sizeMode = 1;  // 0 = S (180px), 1 = M (240px), 2 = L (320px)
        private bool _isFlipped = false;
        private string _targetCameraId = "0";

        public event Action? CameraClosed;

        public FloatingCameraWindow() : this("0", 0)
        {
        }

        public FloatingCameraWindow(string cameraId, int initialShapeMode = 0)
        {
            InitializeComponent();
            _currentInstance = this;
            _targetCameraId = string.IsNullOrEmpty(cameraId) ? "0" : cameraId;
            _shapeMode = initialShapeMode;

            QASmartTouch.Helpers.TouchActivationHelper.ApplyToWindow(this);
            Loaded += FloatingCameraWindow_Loaded;
            Unloaded += FloatingCameraWindow_Unloaded;
        }

        public static void OpenOrActivate(string cameraId = "0", int shapeMode = 0)
        {
            var existing = Application.Current.Windows.OfType<FloatingCameraWindow>().FirstOrDefault();
            if (existing != null)
            {
                existing._shapeMode = shapeMode;
                existing.ApplyShapeAndSize();
                existing.Show();
                existing.Activate();
                _currentInstance = existing;
                return;
            }

            var win = new FloatingCameraWindow(cameraId, shapeMode);
            win.Show();
            _currentInstance = win;
        }

        public static void ToggleVisibility()
        {
            var existing = Application.Current.Windows.OfType<FloatingCameraWindow>().FirstOrDefault();
            if (existing != null)
            {
                if (existing.Visibility == Visibility.Visible)
                {
                    existing.Hide();
                }
                else
                {
                    existing.Show();
                    existing.Activate();
                }
            }
        }

        private async void FloatingCameraWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Đặt vị trí ban đầu ở góc dưới bên phải màn hình (Picture-in-Picture)
                double screenW = SystemParameters.PrimaryScreenWidth;
                double screenH = SystemParameters.PrimaryScreenHeight;
                if (double.IsNaN(Left) || Left <= 0)
                {
                    Left = screenW - Width - 30;
                }
                if (double.IsNaN(Top) || Top <= 0)
                {
                    Top = screenH - Height - 80;
                }

                ApplyShapeAndSize();

                _cameraService = new CameraPreviewService();
                _cameraService.FrameArrived += OnFrameArrived;
                _cameraService.ErrorOccurred += OnCameraError;

                var profile = new QASmartTouch.Models.Camera.CameraProfile 
                { 
                    SelectedCameraId = _targetCameraId, 
                    Width = 640, 
                    Height = 480, 
                    FPS = 30 
                };
                
                await _cameraService.StartAsync(profile);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting camera: {ex.Message}");
                OnCameraError(ex.Message);
            }
        }

        private void OnCameraError(string error)
        {
            Dispatcher.InvokeAsync(() =>
            {
                LoadingPanel.Visibility = Visibility.Visible;
                txtStatus.Text = $"⚠️ {error}";
            });
        }

        private void FloatingCameraWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_currentInstance == this)
            {
                _currentInstance = null;
            }

            if (_cameraService != null)
            {
                _cameraService.FrameArrived -= OnFrameArrived;
                _cameraService.ErrorOccurred -= OnCameraError;
                _ = _cameraService.StopAsync();
                _cameraService = null;
            }
        }

        private void OnFrameArrived(BitmapSource frame)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (LoadingPanel.Visibility != Visibility.Collapsed)
                {
                    LoadingPanel.Visibility = Visibility.Collapsed;
                }

                CameraBrush.ImageSource = frame;
                UpdateFlipTransform();
            });
        }

        private void UpdateFlipTransform()
        {
            if (_isFlipped)
            {
                CameraBrush.RelativeTransform = new ScaleTransform(-1, 1, 0.5, 0.5);
            }
            else
            {
                CameraBrush.RelativeTransform = null;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Window_TouchDown(object? sender, TouchEventArgs e)
        {
            // Cho phép kéo thả mượt mà trên màn hình cảm ứng
            try
            {
                DragMove();
            }
            catch { }
        }

        private void btnFlip_Click(object sender, RoutedEventArgs e)
        {
            _isFlipped = !_isFlipped;
            UpdateFlipTransform();
        }

        private void btnSize_Click(object sender, RoutedEventArgs e)
        {
            _sizeMode = (_sizeMode + 1) % 3;
            ApplyShapeAndSize();
        }

        private void btnShape_Click(object sender, RoutedEventArgs e)
        {
            _shapeMode = (_shapeMode + 1) % 3;
            ApplyShapeAndSize();
        }

        private void ApplyShapeAndSize()
        {
            double baseDimension = _sizeMode switch
            {
                0 => 180, // S
                1 => 230, // M
                2 => 290, // L
                _ => 230
            };

            btnSize.Content = _sizeMode switch
            {
                0 => "S",
                1 => "M",
                2 => "L",
                _ => "M"
            };

            switch (_shapeMode)
            {
                case 0: // Hình Tròn (Circle)
                    Width = baseDimension;
                    Height = baseDimension;
                    CameraBorder.CornerRadius = new CornerRadius(baseDimension / 2.0);
                    CameraRect.RadiusX = (baseDimension / 2.0) - 3;
                    CameraRect.RadiusY = (baseDimension / 2.0) - 3;
                    btnShape.Content = "Tròn";
                    break;

                case 1: // Hình Vuông Bo Góc (Square)
                    Width = baseDimension;
                    Height = baseDimension;
                    CameraBorder.CornerRadius = new CornerRadius(24);
                    CameraRect.RadiusX = 21;
                    CameraRect.RadiusY = 21;
                    btnShape.Content = "Vuông";
                    break;

                case 2: // Hình Chữ Nhật Bo Góc (Rectangle 16:10 / 16:9)
                    Width = baseDimension * 1.38;
                    Height = baseDimension * 0.88;
                    CameraBorder.CornerRadius = new CornerRadius(18);
                    CameraRect.RadiusX = 15;
                    CameraRect.RadiusY = 15;
                    btnShape.Content = "C.Nhật";
                    break;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            CameraClosed?.Invoke();
            Close();
        }
    }
}
