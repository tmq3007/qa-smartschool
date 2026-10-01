using System;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using QASmartTouch.Helpers;

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
            _mainDashboard = this.Owner as Form2_MainDashboard
                ?? (Application.Current as App)?._whiteboardShell
                ?? Application.Current.Windows.OfType<Form2_MainDashboard>().FirstOrDefault()
                ?? Application.Current.MainWindow as Form2_MainDashboard;
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
                    // QC_4.2_IMAGE_UMIND_DRAG: Biến mọi thẻ ảnh trên trang tìm kiếm thành Draggable (UMind Style)
                    function makeDraggable(img) {
                        if (!img || img.__qa_drag_initialized) return;
                        img.__qa_drag_initialized = true;
                        try {
                            img.setAttribute('draggable', 'true');
                            img.style.cursor = 'grab';

                            img.addEventListener('dragstart', function(evt) {
                                var src = img.currentSrc || img.src || img.getAttribute('src') || img.dataset.src;
                                if ((!src || src.startsWith('blob:')) && img.naturalWidth > 0) {
                                    try {
                                        var canvas = document.createElement('canvas');
                                        canvas.width = img.naturalWidth;
                                        canvas.height = img.naturalHeight;
                                        var ctx = canvas.getContext('2d');
                                        ctx.drawImage(img, 0, 0);
                                        src = canvas.toDataURL('image/png');
                                    } catch(e) {}
                                }
                                if (src) {
                                    evt.dataTransfer.setData('text/plain', src);
                                    evt.dataTransfer.setData('text/uri-list', src);
                                    evt.dataTransfer.setData('text/html', '<img src=\'' + src + '\' />');
                                    evt.dataTransfer.effectAllowed = 'copyMove';
                                    console.log('QA SmartClass: Dragging image', src.substring(0, 50));
                                }
                            });
                        } catch(e) {}
                    }

                    // Tự động gán Draggable cho ảnh hiện tại và ảnh tải thêm khi cuộn trang (Infinite scroll)
                    document.querySelectorAll('img').forEach(makeDraggable);
                    try {
                        var observer = new MutationObserver(function(mutations) {
                            document.querySelectorAll('img').forEach(makeDraggable);
                        });
                        if (document.body) {
                            observer.observe(document.body, { childList: true, subtree: true });
                        }
                    } catch(e) {}

                    // Gắn sự kiện click để chọn ảnh thủ công (phục vụ nút 'Chèn vào bảng')
                    document.addEventListener('click', function(e) {
                        var target = e.target;
                        if (target.tagName !== 'IMG') {
                            target = target.closest('img') || target.querySelector('img');
                        }

                        if (target && target.tagName === 'IMG') {
                            document.querySelectorAll('img').forEach(function(img) {
                                img.style.border = '';
                                img.style.boxShadow = '';
                            });

                            target.style.border = '4px solid #3B82F6';
                            target.style.boxShadow = '0 0 12px rgba(59, 130, 246, 0.7)';

                            var imageUrl = target.currentSrc || target.src || target.getAttribute('src') || target.dataset.src;
                            if ((!imageUrl || imageUrl.startsWith('blob:')) && target.naturalWidth > 0) {
                                try {
                                    var canvas = document.createElement('canvas');
                                    canvas.width = target.naturalWidth;
                                    canvas.height = target.naturalHeight;
                                    var ctx = canvas.getContext('2d');
                                    ctx.drawImage(target, 0, 0);
                                    imageUrl = canvas.toDataURL('image/png');
                                } catch(err) {}
                            }

                            if (imageUrl) {
                                window.chrome.webview.postMessage({
                                    type: 'imageSelected',
                                    url: imageUrl,
                                    alt: target.alt || 'Image'
                                });
                            }
                        }
                    }, true);
                })();
            ";

            try
            {
                await webView.CoreWebView2.ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine("✅ Image click and UMind drag handlers injected");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to inject script: {ex.Message}");
            }
        }

        private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var json = e.WebMessageAsJson;
                System.Diagnostics.Debug.WriteLine($"📩 Message received: {json}");

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "imageSelected")
                {
                    if (root.TryGetProperty("url", out var urlProp))
                    {
                        _selectedImageUrl = urlProp.GetString() ?? string.Empty;

                        if (!string.IsNullOrEmpty(_selectedImageUrl))
                        {
                            SelectedImagePanel.Visibility = Visibility.Visible;
                            SelectedImageUrl.Text = _selectedImageUrl.Length > 80 
                                ? _selectedImageUrl.Substring(0, 77) + "..." 
                                : _selectedImageUrl;

                            // Tải ảnh xem trước an toàn qua SmartImageLoader (hỗ trợ base64, https có Referer, webp)
                            try
                            {
                                var previewBitmap = await SmartImageLoader.LoadImageAsync(_selectedImageUrl);
                                if (previewBitmap != null)
                                {
                                    SelectedImagePreview.Source = previewBitmap;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[Preview Load Error] {ex.Message}");
                            }

                            System.Diagnostics.Debug.WriteLine($"✅ Image selected: {_selectedImageUrl}");
                        }
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
                btnInsertImage.IsEnabled = false;
                btnInsertImage.Content = "⏳ Đang tải...";

                // Tải ảnh đa nguồn qua SmartImageLoader: giải quyết triệt để lỗi 'The URI prefix is not recognized'
                var bitmap = await SmartImageLoader.LoadImageAsync(_selectedImageUrl);
                if (bitmap == null)
                {
                    MessageBox.Show("Không thể tải hình ảnh từ nguồn này. Vui lòng thử chọn ảnh khác hoặc kéo thả trực tiếp vào bảng!", "Thông báo",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                InsertImageToCanvas(bitmap);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải hình ảnh:\n{ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Image download error: {ex.Message}");
            }
            finally
            {
                btnInsertImage.IsEnabled = true;
                btnInsertImage.Content = "📥 Chèn vào bảng";
            }
        }

        private void InsertImageToCanvas(BitmapSource bitmap)
        {
            if (_mainDashboard == null)
            {
                MessageBox.Show("Không tìm thấy bảng chính!", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Gọi MainDashboard để chèn ảnh UMind Unified Container vào Canvas
                _mainDashboard.InsertInteractiveImage(bitmap, _selectedImageUrl);

                System.Diagnostics.Debug.WriteLine($"✅ Interactive image inserted to canvas: {_selectedImageUrl}");

                // Đặt lại trạng thái lựa chọn
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

