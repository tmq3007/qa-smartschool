using System;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace QASmartTouch.Forms
{
    public partial class Form2_23_YouTubeBrowser : Window
    {
        private string _selectedVideoUrl = string.Empty;
        private string _selectedVideoId = string.Empty;
        private string _selectedVideoTitle = string.Empty;
        private Form2_MainDashboard? _mainDashboard;

        public Form2_23_YouTubeBrowser()
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
                
                // Add message handler for video selection
                webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                
                System.Diagnostics.Debug.WriteLine("✅ YouTube WebView2 initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ YouTube WebView2 initialization failed: {ex.Message}");
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
                // Inject JavaScript to handle video clicks
                await InjectVideoClickHandler();
                System.Diagnostics.Debug.WriteLine("✅ YouTube loaded successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Navigation failed: {e.WebErrorStatus}");
                MessageBox.Show("Không thể tải YouTube.\nVui lòng kiểm tra kết nối internet.",
                              "Lỗi",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
            }
        }

        private async System.Threading.Tasks.Task InjectVideoClickHandler()
        {
            string script = @"
                (function() {
                    let clickTimer = null;
                    let clickCount = 0;
                    
                    // Add click listener to video thumbnails
                    document.addEventListener('click', function(e) {
                        // Find the closest video link
                        let target = e.target;
                        let videoLink = null;
                        
                        // Traverse up to find video link
                        for (let i = 0; i < 10 && target; i++) {
                            if (target.tagName === 'A' && target.href && target.href.includes('/watch?v=')) {
                                videoLink = target;
                                break;
                            }
                            target = target.parentElement;
                        }
                        
                        if (videoLink) {
                            clickCount++;
                            
                            if (clickCount === 1) {
                                // Wait to see if it's a double-click
                                clickTimer = setTimeout(() => {
                                    // Single click - let video play normally (do nothing)
                                    clickCount = 0;
                                }, 300);
                            } else if (clickCount === 2) {
                                // Double click - prevent navigation and show embed panel
                                clearTimeout(clickTimer);
                                clickCount = 0;
                                
                                e.preventDefault();
                                e.stopPropagation();
                                
                                // Remove previous highlights
                                document.querySelectorAll('[data-youtube-selected]').forEach(el => {
                                    el.removeAttribute('data-youtube-selected');
                                    el.style.outline = '';
                                    el.style.boxShadow = '';
                                });
                                
                                // Highlight selected video
                                videoLink.setAttribute('data-youtube-selected', 'true');
                                videoLink.style.outline = '4px solid #FF0000';
                                videoLink.style.boxShadow = '0 0 10px rgba(255, 0, 0, 0.5)';
                                
                                // Get video title
                                let title = '';
                                const titleEl = videoLink.querySelector('#video-title');
                                if (titleEl) {
                                    title = titleEl.textContent.trim();
                                }
                                
                                // Send video info to C#
                                window.chrome.webview.postMessage({
                                    type: 'videoSelected',
                                    url: videoLink.href,
                                    title: title || 'YouTube Video'
                                });
                            }
                        }
                    }, true);
                })();
            ";

            try
            {
                await webView.CoreWebView2.ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine("✅ Video click handler injected (single-click = play, double-click = embed)");
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

                // Parse JSON manually
                if (json.Contains("\"type\":\"videoSelected\""))
                {
                    // Extract URL
                    int urlStart = json.IndexOf("\"url\":\"") + 7;
                    int urlEnd = json.IndexOf("\"", urlStart);
                    if (urlStart > 6 && urlEnd > urlStart)
                    {
                        _selectedVideoUrl = json.Substring(urlStart, urlEnd - urlStart);
                        
                        // Extract video ID from URL
                        _selectedVideoId = ExtractVideoId(_selectedVideoUrl);
                        
                        // Extract title
                        int titleStart = json.IndexOf("\"title\":\"") + 9;
                        int titleEnd = json.IndexOf("\"", titleStart);
                        if (titleStart > 8 && titleEnd > titleStart)
                        {
                            _selectedVideoTitle = json.Substring(titleStart, titleEnd - titleStart);
                        }
                        
                        // Update UI
                        Dispatcher.Invoke(() =>
                        {
                            SelectedVideoPanel.Visibility = Visibility.Visible;
                            SelectedVideoTitle.Text = _selectedVideoTitle;
                            SelectedVideoUrl.Text = _selectedVideoUrl;
                        });

                        System.Diagnostics.Debug.WriteLine($"✅ Video selected: {_selectedVideoTitle} ({_selectedVideoId})");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error handling message: {ex.Message}");
            }
        }

        private string ExtractVideoId(string url)
        {
            try
            {
                // Extract video ID from YouTube URL
                // Format: https://www.youtube.com/watch?v=VIDEO_ID
                var match = Regex.Match(url, @"[?&]v=([^&]+)");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            catch { }
            
            return string.Empty;
        }

        private void btnInsertVideo_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedVideoId))
            {
                MessageBox.Show("Vui lòng chọn một video trước!", "Thông báo",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (_mainDashboard == null)
                {
                    MessageBox.Show("Không tìm thấy bảng chính!", "Lỗi",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Insert interactive YouTube video to canvas
                _mainDashboard.InsertInteractiveYouTubeVideo(_selectedVideoId, _selectedVideoTitle);

                System.Diagnostics.Debug.WriteLine($"✅ Interactive YouTube video inserted: {_selectedVideoId}");

                // Reset selection
                SelectedVideoPanel.Visibility = Visibility.Collapsed;
                _selectedVideoUrl = string.Empty;
                _selectedVideoId = string.Empty;
                _selectedVideoTitle = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi chèn video:\n{ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Video insert error: {ex.Message}");
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

