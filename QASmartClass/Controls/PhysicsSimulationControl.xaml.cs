using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using QASmartTouch.Helpers;

namespace QASmartTouch.Controls
{
    public partial class PhysicsSimulationControl : UserControl
    {
        private bool _isResizing = false;
        private Point _resizeStartPoint;
        private double _originalWidth;
        private double _originalHeight;
        private bool _isInitialized = false;

        public string SimulationUrl { get; set; } = "https://physics.local/player.html#id=force-vector&token=0123456789abcdef0123456789abcdef";
        public string SimulationTitle { get; set; } = "Mô phỏng Thí nghiệm Vật lý";

        public event EventHandler? DeleteRequested;
        public event EventHandler<BitmapImage>? SnapshotCaptured;

        public PhysicsSimulationControl()
        {
            InitializeComponent();
            TouchActivationHelper.WireButton(btnCapture);
            TouchActivationHelper.WireButton(btnReload);
            TouchActivationHelper.WireButton(btnDelete);
            Loaded += PhysicsSimulationControl_Loaded;
        }

        public PhysicsSimulationControl(string url, string title) : this()
        {
            SimulationUrl = url;
            SimulationTitle = title;
            txtTitle.Text = title;
        }

        private async void PhysicsSimulationControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                await InitializeWebViewAsync();
            }
        }

        private static string GetPhysicsAssetsDirectory()
        {
            string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Simulations", "Physics");
            if (Directory.Exists(p1) && File.Exists(Path.Combine(p1, "player.html"))) return p1;

            string p2 = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\QASmartClass\Assets\Simulations\Physics"));
            if (Directory.Exists(p2) && File.Exists(Path.Combine(p2, "player.html"))) return p2;

            string p3 = @"D:\Document\_Projects\qa-smartschool\QASmartClass\Assets\Simulations\Physics";
            if (Directory.Exists(p3) && File.Exists(Path.Combine(p3, "player.html"))) return p3;

            return p1;
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                await WebView2Helper.EnsureInitializedAsync(webView);

                if (webView.CoreWebView2 != null)
                {
                    string assetsDir = GetPhysicsAssetsDirectory();
                    if (Directory.Exists(assetsDir))
                    {
                        webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                            "physics.local",
                            assetsDir,
                            CoreWebView2HostResourceAccessKind.Allow);
                    }

                    webView.CoreWebView2.Settings.IsScriptEnabled = true;
                    webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                    webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                    webView.CoreWebView2.Settings.AreDevToolsEnabled = false;

                    webView.CoreWebView2.NavigationCompleted += async (s, e) =>
                    {
                        loadingOverlay.Visibility = Visibility.Collapsed;

                        try
                        {
                            string simId = "";
                            if (!string.IsNullOrEmpty(SimulationUrl) && SimulationUrl.Contains("#id="))
                            {
                                int idx = SimulationUrl.IndexOf("#id=") + 4;
                                int endIdx = SimulationUrl.IndexOf('&', idx);
                                simId = endIdx > idx ? SimulationUrl.Substring(idx, endIdx - idx) : SimulationUrl.Substring(idx);
                            }

                            if (!string.IsNullOrEmpty(simId))
                            {
                                string script = $@"
                                    window.postMessage({{
                                        nbPhysics: 1,
                                        token: '0123456789abcdef0123456789abcdef',
                                        type: 'init',
                                        scene: {{ id: '{simId}', states: {{}}, refs: {{}} }}
                                    }}, '*');
                                    var st = document.getElementById('playerStatus');
                                    if (st) st.style.display = 'none';
                                ";
                                await webView.CoreWebView2.ExecuteScriptAsync(script);
                            }
                        }
                        catch { }
                    };

                    if (!string.IsNullOrEmpty(SimulationUrl))
                    {
                        webView.CoreWebView2.Navigate(SimulationUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PhysicsSimulationControl] Init failed: {ex.Message}");
                loadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Cho phép container cha xử lý kéo thả
            e.Handled = false;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Bạn có chắc chắn muốn đóng mô phỏng \"{SimulationTitle}\"?",
                "Xác nhận đóng",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DeleteRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void btnReload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                loadingOverlay.Visibility = Visibility.Visible;
                webView.CoreWebView2?.Reload();
            }
            catch { }
        }

        private async void btnCapture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (webView.CoreWebView2 == null) return;

                string tempFile = Path.Combine(Path.GetTempPath(), $"physics_snap_{Guid.NewGuid():N}.png");
                using (var fs = File.Create(tempFile))
                {
                    await webView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, fs);
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(tempFile);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                try { File.Delete(tempFile); } catch { }

                SnapshotCaptured?.Invoke(this, bitmap);
                MessageBox.Show("Đã chụp ảnh mô phỏng thành công và chèn vào bảng vẽ!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể chụp ảnh mô phỏng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isResizing = true;
            _resizeStartPoint = e.GetPosition(this.Parent as UIElement);
            _originalWidth = this.ActualWidth > 0 ? this.ActualWidth : this.Width;
            _originalHeight = this.ActualHeight > 0 ? this.ActualHeight : this.Height;

            resizeHandle.CaptureMouse();
            e.Handled = true;
        }

        private void ResizeHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizing)
            {
                _isResizing = false;
                resizeHandle.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizing && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(this.Parent as UIElement);
                double deltaX = currentPoint.X - _resizeStartPoint.X;
                double deltaY = currentPoint.Y - _resizeStartPoint.Y;

                double newWidth = Math.Max(400, _originalWidth + deltaX);
                double newHeight = Math.Max(300, _originalHeight + deltaY);

                this.Width = newWidth;
                this.Height = newHeight;
                e.Handled = true;
            }
        }
    }
}
