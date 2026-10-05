using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QASmartTouch.Services.Camera;

namespace QASmartTouch.Forms
{
    public partial class FloatingCameraWindow : Window
    {
        private CameraPreviewService? _cameraService;
        private int _shapeMode = 0; // 0 = Tròn (Circle), 1 = Vuông (Square bo góc), 2 = Chữ nhật (Rectangle)
        private bool _isFlipped = false;

        public event Action? CameraClosed;

        public FloatingCameraWindow()
        {
            InitializeComponent();
            QASmartTouch.Helpers.TouchActivationHelper.ApplyToWindow(this);
            Loaded += FloatingCameraWindow_Loaded;
            Unloaded += FloatingCameraWindow_Unloaded;
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

                ApplyShape();

                _cameraService = new CameraPreviewService();
                _cameraService.FrameArrived += OnFrameArrived;
                _cameraService.ErrorOccurred += OnCameraError;
                
                // Đọc cấu hình camera đã lưu hoặc mặc định
                string cameraId = "0";
                try
                {
                    var configService = new CameraConfigService();
                    var saved = configService.Load();
                    if (!string.IsNullOrEmpty(saved.SelectedCameraId))
                    {
                        cameraId = saved.SelectedCameraId;
                    }
                }
                catch { }

                var profile = new QASmartTouch.Models.Camera.CameraProfile 
                { 
                    SelectedCameraId = cameraId, 
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
            DragMove();
        }

        private void btnFlip_Click(object sender, RoutedEventArgs e)
        {
            _isFlipped = !_isFlipped;
            UpdateFlipTransform();
        }

        private void btnShape_Click(object sender, RoutedEventArgs e)
        {
            _shapeMode = (_shapeMode + 1) % 3;
            ApplyShape();
        }

        private void ApplyShape()
        {
            switch (_shapeMode)
            {
                case 0: // Hình Tròn (Circle)
                    Width = 220;
                    Height = 220;
                    CameraBorder.CornerRadius = new CornerRadius(110);
                    CameraRect.RadiusX = 107;
                    CameraRect.RadiusY = 107;
                    btnShape.Content = "Tròn";
                    break;

                case 1: // Hình Vuông Bo Góc (Square)
                    Width = 220;
                    Height = 220;
                    CameraBorder.CornerRadius = new CornerRadius(24);
                    CameraRect.RadiusX = 21;
                    CameraRect.RadiusY = 21;
                    btnShape.Content = "Vuông";
                    break;

                case 2: // Hình Chữ Nhật Bo Góc (Rectangle 16:10 / 16:9)
                    Width = 320;
                    Height = 210;
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
