using System;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;

namespace QASmartTouch.Forms
{
    public partial class Form2_26_GoogleSearchBrowser : Window
    {
        private string _selectedImageUrl = string.Empty;
        private Form2_MainDashboard? _mainDashboard;

        public Form2_26_GoogleSearchBrowser()
        {
            InitializeComponent();
            Topmost = true;
            _ = InitializeWebView();
            
            // Get reference to MainDashboard
            _mainDashboard = Application.Current.MainWindow as Form2_MainDashboard;
        }

        private async System.Threading.Tasks.Task InitializeWebView()
        {
            try
            {
                // Initialize WebView2
                await webView.EnsureCoreWebView2Async(null);
                
                // Add message handler for image selection
                webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                
                System.Diagnostics.Debug.WriteLine("✅ Google Search WebView2 initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Google Search WebView2 initialization failed: {ex.Message}");
                MessageBox.Show($"Lỗi khi khởi tạo trình duyệt:\n{ex.Message}",
                              "Lỗi",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                this.Close();
            }
        }

        private async void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Hide loading panel
            LoadingPanel.Visibility = Visibility.Collapsed;

            if (e.IsSuccess)
            {
                // Inject JavaScript to handle image clicks
                await InjectImageClickHandler();
                System.Diagnostics.Debug.WriteLine("✅ Google Search loaded successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Navigation failed: {e.WebErrorStatus}");
                MessageBox.Show("Không thể tải Google Search.\nVui lòng kiểm tra kết nối internet.",
                              "Lỗi",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
            }
        }

        private async System.Threading.Tasks.Task InjectImageClickHandler()
        {
            string script = @"
                (function() {
                    // Remove existing listeners
                    document.querySelectorAll('img').forEach(img => {
                        img.style.cursor = 'pointer';
                    });

                    // Add click listener to all images
                    document.addEventListener('click', function(e) {
                        if (e.target.tagName === 'IMG') {
                            // Remove previous highlights
                            document.querySelectorAll('img').forEach(img => {
                                img.style.border = '';
                                img.style.boxShadow = '';
                            });

                            // Highlight selected image
                            e.target.style.border = '4px solid #4CAF50';
                            e.target.style.boxShadow = '0 0 10px rgba(76, 175, 80, 0.5)';

                            // Send image URL to C#
                            var imageUrl = e.target.src || e.target.dataset.src || e.target.getAttribute('data-src');
                            if (imageUrl) {
                                window.chrome.webview.postMessage({
                                    type: 'imageSelected',
                                    url: imageUrl,
                                    alt: e.target.alt || 'Image'
                                });
                            }
                        }
                    }, true);
                })();
            ";

            try
            {
                await webView.CoreWebView2.ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine("✅ Image click handler injected");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to inject script: {ex.Message}");
            }
        }

        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var json = e.WebMessageAsJson;
                System.Diagnostics.Debug.WriteLine($"📩 Message received: {json}");

                // Parse JSON manually (simple approach)
                if (json.Contains("\"type\":\"imageSelected\""))
                {
                    // Extract URL from JSON
                    int urlStart = json.IndexOf("\"url\":\"") + 7;
                    int urlEnd = json.IndexOf("\"", urlStart);
                    if (urlStart > 6 && urlEnd > urlStart)
                    {
                        _selectedImageUrl = json.Substring(urlStart, urlEnd - urlStart);
                        
                        // Update UI
                        Dispatcher.Invoke(() =>
                        {
                            SelectedImagePanel.Visibility = Visibility.Visible;
                            SelectedImageUrl.Text = _selectedImageUrl;
                            
                            // Load preview
                            try
                            {
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.UriSource = new Uri(_selectedImageUrl);
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.EndInit();
                                SelectedImagePreview.Source = bitmap;
                            }
                            catch
                            {
                                // Preview loading failed, but we still have the URL
                            }
                        });

                        System.Diagnostics.Debug.WriteLine($"✅ Image selected: {_selectedImageUrl}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error handling message: {ex.Message}");
            }
        }

        private async void btnInsertImage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedImageUrl))
            {
                MessageBox.Show("Vui lòng chọn một hình ảnh trước!", "Thông báo",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Download and insert image to canvas
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_selectedImageUrl);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                // Wait for image to load
                if (bitmap.IsDownloading)
                {
                    bitmap.DownloadCompleted += (s, args) =>
                    {
                        Dispatcher.Invoke(() => InsertImageToCanvas(bitmap));
                    };
                }
                else
                {
                    InsertImageToCanvas(bitmap);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải hình ảnh:\n{ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Image download error: {ex.Message}");
            }
        }

        private void InsertImageToCanvas(BitmapImage bitmap)
        {
            if (_mainDashboard == null)
            {
                MessageBox.Show("Không tìm thấy bảng chính!", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Call the MainDashboard method to insert interactive image
                _mainDashboard.InsertInteractiveImage(bitmap, _selectedImageUrl);

                System.Diagnostics.Debug.WriteLine($"✅ Interactive image inserted to canvas: {_selectedImageUrl}");

                // Reset selection
                SelectedImagePanel.Visibility = Visibility.Collapsed;
                _selectedImageUrl = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi chèn hình ảnh:\n{ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Canvas insert error: {ex.Message}");
            }
        }

        private void btnDone_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                if (webView != null)
                {
                    webView.Source = null;
                    webView.CoreWebView2?.Stop();
                    webView.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WebView Cleanup Error] {ex.Message}");
            }
            base.OnClosed(e);
        }
}
}

