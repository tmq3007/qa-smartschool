using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartTouch.Services.Camera;
using QASmartTouch.Models.Camera;
using QASmartTouch.Helpers.Camera;

namespace QASmartTouch.Forms
{
    public partial class Form3_3_CameraConfiguration : Window
    {
        // Services
        private readonly CameraDiscoveryService _discoveryService;
        private readonly CameraConfigService _configService;
        private CameraPreviewService? _previewService;
        private FrameRateCounter? _fpsCounter;
        
        // State
        private List<CameraDevice> _availableCameras = new();
        private CameraDevice? _selectedCamera;
        private bool _isPreviewActive = false;
        private bool _isStopping = false;

        public Form3_3_CameraConfiguration()
        {
            InitializeComponent();
            
            // Initialize services
            _discoveryService = new CameraDiscoveryService();
            _configService = new CameraConfigService();
            _fpsCounter = new FrameRateCounter();
            
            // Load on startup
            Loaded += Form3_3_CameraConfiguration_Loaded;
            Closing += Form3_3_CameraConfiguration_Closing;
        }

        private async void Form3_3_CameraConfiguration_Loaded(object sender, RoutedEventArgs e)
        {
            // Hide the internal placeholder of CameraPreviewControl (we have our own)
            if (cameraPreview.PlaceholderPanel != null)
            {
                cameraPreview.PlaceholderPanel.Visibility = Visibility.Collapsed;
            }
            
            await DiscoverCamerasAsync();
            LoadSavedConfiguration();
            UpdateWizardStep(1);
        }

        private async void Form3_3_CameraConfiguration_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Stop preview before closing (prevent memory leaks and crashes)
            if (_isPreviewActive && !_isStopping)
            {
                e.Cancel = true; // Block closing temporarily
                _isStopping = true;
                this.IsEnabled = false; // Disable UI
                await StopPreview();
                _isStopping = false;
                this.Close(); // Call close again now that preview is stopped safely
            }
        }

        private async System.Threading.Tasks.Task DiscoverCamerasAsync()
        {
            try
            {
                UpdateStatus("Đang quét tìm thiết bị camera...", "#FFA500");
                
                // Discover cameras
                _availableCameras = await System.Threading.Tasks.Task.Run(() => 
                    _discoveryService.GetAllCameras());
                
                // Populate camera dropdown
                cmbCameraDevice.Items.Clear();
                foreach (var camera in _availableCameras)
                {
                    cmbCameraDevice.Items.Add(camera.FriendlyName);
                }
                
                if (cmbCameraDevice.Items.Count > 0)
                {
                    cmbCameraDevice.SelectedIndex = 0;
                    UpdateStatus($"Tìm thấy {_availableCameras.Count} thiết bị camera", "#4CAF50");
                }
                else
                {
                    UpdateStatus("Không tìm thấy camera nào", "#F44336");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi: {ex.Message}", "#F44336");
                MessageBox.Show($"Không thể quét thiết bị camera:\n{ex.Message}", 
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSavedConfiguration()
        {
            try
            {
                var profile = _configService.Load();
                if (profile != null && _availableCameras.Count > 0)
                {
                    // Find saved camera
                    var savedCamera = _availableCameras.FirstOrDefault(c => c.DeviceId == profile.SelectedCameraId);
                    if (savedCamera != null)
                    {
                        var index = _availableCameras.IndexOf(savedCamera);
                        if (index >= 0)
                        {
                            cmbCameraDevice.SelectedIndex = index;

                            // Restore resolution
                            string savedRes = $"{profile.Width}x{profile.Height}";
                            int resIndex = cmbResolution.Items.Cast<string>().ToList().IndexOf(savedRes);
                            if (resIndex >= 0)
                            {
                                cmbResolution.SelectedIndex = resIndex;
                            }

                            // Restore frame rate
                            string savedFps = $"{profile.FPS} FPS";
                            int fpsIndex = cmbFrameRate.Items.Cast<string>().ToList().IndexOf(savedFps);
                            if (fpsIndex >= 0)
                            {
                                cmbFrameRate.SelectedIndex = fpsIndex;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load configuration: {ex.Message}");
            }
        }

        private void cmbCameraDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCameraDevice.SelectedIndex >= 0 && cmbCameraDevice.SelectedIndex < _availableCameras.Count)
            {
                _selectedCamera = _availableCameras[cmbCameraDevice.SelectedIndex];
                LoadDefaultCapabilities();
                UpdateWizardStep(2);
            }
        }

        private void LoadDefaultCapabilities()
        {
            // Add default resolution options
            cmbResolution.Items.Clear();
            cmbResolution.Items.Add("1920x1080");
            cmbResolution.Items.Add("1280x720");
            cmbResolution.Items.Add("640x480");
            cmbResolution.SelectedIndex = 1; // Default to 720p

            // Add default frame rate options
            cmbFrameRate.Items.Clear();
            cmbFrameRate.Items.Add("30 FPS");
            cmbFrameRate.Items.Add("25 FPS");
            cmbFrameRate.Items.Add("15 FPS");
            cmbFrameRate.SelectedIndex = 0; // Default to 30 FPS
        }

        private void cmbResolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Resolution changed - will apply on next preview start
            UpdateWizardStep(2);
        }

        private void cmbFrameRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Frame rate changed - will apply on next preview start
            UpdateWizardStep(2);
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await DiscoverCamerasAsync();
        }

        private async void btnTest_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCamera == null)
            {
                MessageBox.Show("Vui lòng chọn thiết bị camera trước!", 
                    "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await StartPreview();
        }

        private async void btnStop_Click(object sender, RoutedEventArgs e)
        {
            await StopPreview();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCamera == null)
            {
                MessageBox.Show("Vui lòng chọn thiết bị camera trước!", 
                    "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Parse resolution safely
                int width = 1280;
                int height = 720;
                var resolutionText = cmbResolution.SelectedItem?.ToString() ?? "1280x720";
                if (resolutionText.Contains("x"))
                {
                    var resParts = resolutionText.Split('x');
                    if (resParts.Length == 2 && int.TryParse(resParts[0], out int w) && int.TryParse(resParts[1], out int h))
                    {
                        width = w;
                        height = h;
                    }
                }

                // Parse frame rate safely
                int fps = 30;
                var fpsText = cmbFrameRate.SelectedItem?.ToString() ?? "30 FPS";
                if (fpsText.Contains(" "))
                {
                    var fpsParts = fpsText.Split(' ');
                    if (fpsParts.Length > 0 && int.TryParse(fpsParts[0], out int f))
                    {
                        fps = f;
                    }
                }

                // Create profile
                var profile = new CameraProfile
                {
                    SelectedCameraId = _selectedCamera.DeviceId,
                    Width = width,
                    Height = height,
                    FPS = fps,
                    AutoStartOnLaunch = false
                };

                // Save configuration
                _configService.Save(profile);

                UpdateStatus("Đã lưu cấu hình thành công", "#4CAF50");
                UpdateWizardStep(4);
                MessageBox.Show($"Đã lưu cấu hình camera thành công!\n\nCamera: {_selectedCamera.FriendlyName}\nĐộ phân giải: {width}x{height}\nTốc độ khung hình: {fps} FPS", 
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lưu thất bại: {ex.Message}", "#F44336");
                MessageBox.Show($"Không thể lưu cấu hình:\n{ex.Message}", 
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task StartPreview()
        {
            if (_selectedCamera == null || _isPreviewActive) return;

            try
            {
                UpdateStatus("Đang khởi động xem trước...", "#FFA500");

                // Parse settings safely
                int width = 1280;
                int height = 720;
                var resolutionText = cmbResolution.SelectedItem?.ToString() ?? "1280x720";
                if (resolutionText.Contains("x"))
                {
                    var resParts = resolutionText.Split('x');
                    if (resParts.Length == 2 && int.TryParse(resParts[0], out int w) && int.TryParse(resParts[1], out int h))
                    {
                        width = w;
                        height = h;
                    }
                }

                int fps = 30;
                var fpsText = cmbFrameRate.SelectedItem?.ToString() ?? "30 FPS";
                if (fpsText.Contains(" "))
                {
                    var fpsParts = fpsText.Split(' ');
                    if (fpsParts.Length > 0 && int.TryParse(fpsParts[0], out int f))
                    {
                        fps = f;
                    }
                }

                // Create profile
                var profile = new CameraProfile
                {
                    SelectedCameraId = _selectedCamera.DeviceId,
                    Width = width,
                    Height = height,
                    FPS = fps
                };

                // Create preview service
                _previewService = new CameraPreviewService();
                _previewService.FrameArrived += OnFrameArrived;
                _previewService.ErrorOccurred += OnPreviewError;

                // Start preview
                await _previewService.StartAsync(profile);
                
                _isPreviewActive = true;
                placeholderPanel.Visibility = Visibility.Collapsed;
                
                // Update UI
                btnTest.IsEnabled = false;
                btnStop.IsEnabled = true;
                UpdateStatus("Xem trước đang hoạt động", "#4CAF50");
                UpdateWizardStep(3);

                // Start FPS counter
                _fpsCounter?.Reset();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi xem trước: {ex.Message}", "#F44336");
                MessageBox.Show($"Không thể khởi động xem trước camera:\n{ex.Message}", 
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task StopPreview()
        {
            if (!_isPreviewActive || _previewService == null) return;

            try
            {
                UpdateStatus("Đang dừng xem trước...", "#FFA500");

                await _previewService.StopAsync();
                _previewService.Dispose();
                _previewService = null;
                
                _isPreviewActive = false;
                placeholderPanel.Visibility = Visibility.Visible;
                cameraPreview.ClearPreview();
                
                // Update UI
                btnTest.IsEnabled = true;
                btnStop.IsEnabled = false;
                UpdateStatus("Xem trước đã dừng", "#4CAF50");
                UpdateFPS(0);

                // Update wizard step back to Step 2
                UpdateWizardStep(2);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi dừng: {ex.Message}", "#F44336");
            }
        }

        private void OnFrameArrived(System.Windows.Media.Imaging.BitmapSource frame)
        {
            Dispatcher.Invoke(() =>
            {
                if (_isPreviewActive)
                {
                    cameraPreview.UpdateFrame(frame);
                    
                    // Update FPS
                    _fpsCounter?.FrameArrived();
                    var fps = _fpsCounter?.CurrentFPS ?? 0;
                    UpdateFPS(fps);
                }
            });
        }

        private void OnPreviewError(string error)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatus($"Lỗi: {error}", "#F44336");
            });
        }

        private void UpdateStatus(string message, string color)
        {
            txtStatus.Text = $"Trạng thái: {message}";
            txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
        }

        private void UpdateFPS(double fps)
        {
            txtFPS.Text = $"Khung hình/giây (FPS): {Math.Round(fps)}";
        }

        private void UpdateWizardStep(int stepIndex)
        {
            if (tblStep1 == null || tblStep2 == null || tblStep3 == null || tblStep4 == null) return;

            // Reset all
            tblStep1.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#57606F"));
            tblStep1.FontWeight = FontWeights.Normal;
            tblStep1.Opacity = 0.5;
            tblStep2.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#57606F"));
            tblStep2.FontWeight = FontWeights.Normal;
            tblStep2.Opacity = 0.5;
            tblStep3.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#57606F"));
            tblStep3.FontWeight = FontWeights.Normal;
            tblStep3.Opacity = 0.5;
            tblStep4.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#57606F"));
            tblStep4.FontWeight = FontWeights.Normal;
            tblStep4.Opacity = 0.5;

            // Set active
            var activeColor = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0D47A1"));
            switch (stepIndex)
            {
                case 1:
                    tblStep1.Foreground = activeColor;
                    tblStep1.FontWeight = FontWeights.Bold;
                    tblStep1.Opacity = 1.0;
                    break;
                case 2:
                    tblStep2.Foreground = activeColor;
                    tblStep2.FontWeight = FontWeights.Bold;
                    tblStep2.Opacity = 1.0;
                    break;
                case 3:
                    tblStep3.Foreground = activeColor;
                    tblStep3.FontWeight = FontWeights.Bold;
                    tblStep3.Opacity = 1.0;
                    break;
                case 4:
                    tblStep4.Foreground = activeColor;
                    tblStep4.FontWeight = FontWeights.Bold;
                    tblStep4.Opacity = 1.0;
                    break;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwndSource = System.Windows.PresentationSource.FromVisual(this) as System.Windows.Interop.HwndSource;
            if (hwndSource != null)
            {
                hwndSource.AddHook(WndProc);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            var hwndSource = System.Windows.PresentationSource.FromVisual(this) as System.Windows.Interop.HwndSource;
            if (hwndSource != null)
            {
                hwndSource.RemoveHook(WndProc);
            }
            base.OnClosed(e);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_DEVICECHANGE = 0x0219;
            const int DBT_DEVNODES_CHANGED = 0x0007;
            if (msg == WM_DEVICECHANGE && (int)wParam == DBT_DEVNODES_CHANGED)
            {
                Dispatcher.Invoke(async () => await HandleDeviceChangeAsync());
            }
            return IntPtr.Zero;
        }

        private async System.Threading.Tasks.Task HandleDeviceChangeAsync()
        {
            if (_isPreviewActive)
            {
                var currentCameras = await System.Threading.Tasks.Task.Run(() => _discoveryService.GetAllCameras());
                if (_selectedCamera != null && !currentCameras.Any(c => c.DeviceId == _selectedCamera.DeviceId))
                {
                    await StopPreview();
                    UpdateStatus("Thiết bị camera đã bị rút kết nối vật lý!", "#F44336");
                    MessageBox.Show("Cảnh báo: Thiết bị camera đang thử nghiệm đã bị rút kết nối vật lý đột ngột!", 
                        "Lỗi thiết bị", MessageBoxButton.OK, MessageBoxImage.Warning);
                    await DiscoverCamerasAsync();
                }
            }
            else
            {
                await DiscoverCamerasAsync();
            }
        }
    }
}
