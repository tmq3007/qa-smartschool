using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
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
        private bool _isCleanedUp = false;

        public Form2_23_YouTubeBrowser()
        {
            InitializeComponent();
            Topmost = true;
            this.Loaded += async (s, e) => await InitializeWebView();
            
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
                // Initialize WebView2 với cấu hình UserDataFolder an toàn từ WebView2Helper
                await QASmartTouch.Helpers.WebView2Helper.EnsureInitializedAsync(webView);
                
                // Add message handler and source changed handler
                webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                webView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
                
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

        private void CoreWebView2_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            CheckCurrentPageForVideo();
        }

        private async void CheckCurrentPageForVideo()
        {
            try
            {
                if (_isCleanedUp || webView?.CoreWebView2 == null) return;

                string currentUrl = webView.Source?.ToString() ?? string.Empty;
                string videoId = ExtractVideoId(currentUrl);
                if (!string.IsNullOrEmpty(videoId))
                {
                    _selectedVideoId = videoId;
                    _selectedVideoUrl = currentUrl;

                    string title = string.Empty;
                    try
                    {
                        string rawTitle = await webView.CoreWebView2.ExecuteScriptAsync("document.title");
                        if (!string.IsNullOrWhiteSpace(rawTitle) && rawTitle != "null")
                        {
                            title = rawTitle.Trim().Trim('"');
                            title = Regex.Unescape(title);
                            if (title.EndsWith(" - YouTube", StringComparison.OrdinalIgnoreCase))
                            {
                                title = title.Substring(0, title.Length - 10).Trim();
                            }
                        }
                    }
                    catch { }

                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        _selectedVideoTitle = title;
                    }
                    else if (string.IsNullOrWhiteSpace(_selectedVideoTitle))
                    {
                        _selectedVideoTitle = "YouTube Video";
                    }

                    Dispatcher.Invoke(() =>
                    {
                        SelectedVideoPanel.Visibility = Visibility.Visible;
                        SelectedVideoTitle.Text = _selectedVideoTitle;
                        SelectedVideoUrl.Text = _selectedVideoUrl;
                    });

                    System.Diagnostics.Debug.WriteLine($"[YouTubeBrowser] Detected video: {_selectedVideoTitle} ({_selectedVideoId})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[YouTubeBrowser CheckCurrentPageForVideo Error] {ex.Message}");
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
                CheckCurrentPageForVideo();
                System.Diagnostics.Debug.WriteLine("✅ YouTube loaded successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Navigation status: {e.WebErrorStatus}");
                // If closing or navigation was canceled, ignore
                if (_isCleanedUp || e.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled)
                {
                    return;
                }

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
                    if (window.__qaSmartTouchYouTubeInjected) return;
                    window.__qaSmartTouchYouTubeInjected = true;

                    // Listen for YouTube SPA navigation finish
                    window.addEventListener('yt-navigate-finish', function() {
                        try {
                            if (window.location.href.includes('/watch?v=')) {
                                window.chrome.webview.postMessage({
                                    type: 'videoSelected',
                                    url: window.location.href,
                                    title: document.title || 'YouTube Video'
                                });
                            }
                        } catch(e) {}
                    });

                    let clickTimer = null;
                    let clickCount = 0;
                    
                    // Add click listener to video thumbnails
                    document.addEventListener('click', function(e) {
                        try {
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
                                        // Single click - let video play normally
                                        clickCount = 0;
                                    }, 300);
                                } else if (clickCount >= 2) {
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
                                    const titleEl = videoLink.querySelector('#video-title') || videoLink.querySelector('h3');
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
                        } catch(err) {}
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

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "videoSelected")
                {
                    string url = root.TryGetProperty("url", out var urlProp) ? urlProp.GetString() ?? string.Empty : string.Empty;
                    string title = root.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? string.Empty : string.Empty;

                    string videoId = ExtractVideoId(url);
                    if (!string.IsNullOrEmpty(videoId))
                    {
                        _selectedVideoUrl = url;
                        _selectedVideoId = videoId;
                        _selectedVideoTitle = string.IsNullOrWhiteSpace(title) ? "YouTube Video" : title;

                        if (_selectedVideoTitle.EndsWith(" - YouTube", StringComparison.OrdinalIgnoreCase))
                        {
                            _selectedVideoTitle = _selectedVideoTitle.Substring(0, _selectedVideoTitle.Length - 10).Trim();
                        }

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
                if (string.IsNullOrWhiteSpace(url)) return string.Empty;

                // Extract video ID from YouTube URL
                // Format: https://www.youtube.com/watch?v=VIDEO_ID
                var match = Regex.Match(url, @"[?&]v=([^&#]+)");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                var matchShort = Regex.Match(url, @"youtu\.be/([^?&#]+)");
                if (matchShort.Success)
                {
                    return matchShort.Groups[1].Value;
                }
                var matchShorts = Regex.Match(url, @"youtube\.com/shorts/([^?&#]+)");
                if (matchShorts.Success)
                {
                    return matchShorts.Groups[1].Value;
                }
                var matchEmbed = Regex.Match(url, @"youtube\.com/embed/([^?&#]+)");
                if (matchEmbed.Success)
                {
                    return matchEmbed.Groups[1].Value;
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

                // Clean up and close this browser window so user can interact with the video on canvas
                StopAndCleanupWebView();
                this.Close();
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
            StopAndCleanupWebView();
            this.Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            StopAndCleanupWebView();
            this.Close();
        }

        private void StopAndCleanupWebView()
        {
            if (_isCleanedUp) return;
            _isCleanedUp = true;

            try
            {
                if (webView != null)
                {
                    if (webView.CoreWebView2 != null)
                    {
                        try
                        {
                            // 1. Mute all audio at the Chromium level immediately
                            webView.CoreWebView2.IsMuted = true;
                        }
                        catch { }

                        try
                        {
                            // 2. Pause and blank all HTML5 audio/video elements
                            _ = webView.CoreWebView2.ExecuteScriptAsync(
                                @"try {
                                    document.querySelectorAll('video, audio').forEach(el => {
                                        try {
                                            el.pause();
                                            el.muted = true;
                                            el.currentTime = 0;
                                            el.src = '';
                                        } catch(e){}
                                    });
                                } catch(e){}"
                            );
                        }
                        catch { }

                        try
                        {
                            // 3. Navigate away to about:blank to destroy DOM media decoders
                            webView.CoreWebView2.Navigate("about:blank");
                        }
                        catch { }

                        try
                        {
                            // 4. Stop any pending network / script execution
                            webView.CoreWebView2.Stop();
                        }
                        catch { }

                        // 5. Unhook CoreWebView2 events
                        try
                        {
                            webView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
                            webView.CoreWebView2.SourceChanged -= CoreWebView2_SourceChanged;
                        }
                        catch { }
                    }

                    // 6. Unhook WPF events
                    try
                    {
                        webView.NavigationCompleted -= WebView_NavigationCompleted;
                    }
                    catch { }

                    // 7. Dispose WebView2 control
                    try
                    {
                        webView.Dispose();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[YouTube WebView Dispose Error] {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[YouTube StopAndCleanupWebView Error] {ex.Message}");
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            StopAndCleanupWebView();
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            StopAndCleanupWebView();
            base.OnClosed(e);
        }
    }
}

