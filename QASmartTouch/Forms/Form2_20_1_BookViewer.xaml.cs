using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using QASmartTouch.Modules.InteractiveBooks.Models;

namespace QASmartTouch.Forms
{
    public partial class Form2_20_1_BookViewer : Window
    {
        private readonly Book _book;
        private string _volumeBaseUrl = "";
        private int _startPage = 1;
        private int _totalPages = 100;
        private int _currentPage = 1;
        private bool _isSliderDragging = false;

        public Form2_20_1_BookViewer(Book book)
        {
            InitializeComponent();
            Topmost = true;
            _book = book;
            
            // Set book info in header
            txtBookIcon.Text = book.Icon;
            txtBookTitle.Text = book.Name;
            txtBookSubtitle.Text = $"{book.Subject} • Lớp {book.Grade} • {book.Publisher}";
            
            // Parse URL and extract volume/page information
            _totalPages = book.PageCount > 0 ? book.PageCount : 100;
            ParseBookUrl(book.Url);
            
            // Set slider max
            pageSlider.Maximum = _totalPages;
            
            // Add keyboard shortcuts
            this.KeyDown += BookViewer_KeyDown;
            
            // Initialize WebView2
            _ = InitializeAsync();
        }

        private void ParseBookUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                _volumeBaseUrl = "";
                _startPage = 1;
                return;
            }

            string cleanUrl = url.TrimEnd('/');
            var match = Regex.Match(cleanUrl, @"^(.*)/(\d+)$");
            if (match.Success && int.TryParse(match.Groups[2].Value, out int startPage))
            {
                _volumeBaseUrl = match.Groups[1].Value;
                _startPage = startPage;
            }
            else
            {
                _volumeBaseUrl = cleanUrl;
                _startPage = 1;
            }
            System.Diagnostics.Debug.WriteLine($"Parsed URL: VolumeBase={_volumeBaseUrl}, StartPage={_startPage}");
        }

        private async System.Threading.Tasks.Task InitializeAsync()
        {
            try
            {
                await webView.EnsureCoreWebView2Async(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Không thể khởi tạo WebView2: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void WebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                // Configure WebView2 settings
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;

                ConfigureWebViewSecurity(webView.CoreWebView2);

                // Navigate to book URL
                webView.CoreWebView2.Navigate(_book.Url);
                
                System.Diagnostics.Debug.WriteLine($"✅ WebView2 initialized, navigating to: {_book.Url}");
            }
            else
            {
                MessageBox.Show(
                    $"Lỗi khởi tạo WebView2: {e.InitializationException?.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ConfigureWebViewSecurity(CoreWebView2 webView2)
        {
            webView2.NewWindowRequested += (s, e) =>
            {
                e.Handled = true; // Block popup windows
                MessageBox.Show("Tính năng mở cửa sổ mới đã bị vô hiệu hóa để đảm bảo an toàn học tập.", 
                                "Cảnh báo Bảo mật", MessageBoxButton.OK, MessageBoxImage.Warning);
            };
        }

        private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            string url = e.Uri;
            if (!string.IsNullOrEmpty(url) && !url.StartsWith("about:blank") && !url.StartsWith("javascript:"))
            {
                try
                {
                    Uri uri = new Uri(url);
                    string host = uri.Host.ToLower();
                    if (host != "hoc10.vn" && !host.EndsWith(".hoc10.vn"))
                    {
                        e.Cancel = true; // Block external navigation
                        MessageBox.Show("Truy cập trang web bên ngoài đã bị chặn để đảm bảo an toàn học tập.", 
                                        "Cảnh báo Bảo mật", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error validating URL: {ex.Message}");
                }
            }
            loadingPanel.Visibility = Visibility.Visible;
        }

        private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            loadingPanel.Visibility = Visibility.Collapsed;
            
            if (e.IsSuccess)
            {
                // Extract page number from URL
                string currentUrl = webView.CoreWebView2.Source;
                int detectedPage = ExtractPageNumberFromUrl(currentUrl);
                
                // Update controls if page changed
                if (detectedPage > 0 && detectedPage != _currentPage)
                {
                    _currentPage = detectedPage;
                    UpdatePageControls();
                }
                
                // Inject CSS to hide unwanted elements
                await InjectCustomStyles();
                
                System.Diagnostics.Debug.WriteLine($"✅ Navigation completed: Page {_currentPage}/{_totalPages}");
            }
        }

        private async System.Threading.Tasks.Task InjectCustomStyles()
        {
            // CSS to hide common ad containers and unwanted elements
            string customCss = @"
                /* Hide common ad containers */
                .advertisement, .ad-container, .ads, .ad-banner, 
                [class*='advertisement'], [class*='ad-'], [id*='ad-'],
                iframe[src*='doubleclick'], iframe[src*='googlesyndication'],
                iframe[src*='advertising'], .google-ad, .adsense,
                .header-ads, .sidebar-ads, .footer-ads,
                .url-bar, .address-bar, .navigation-bar {
                    display: none !important;
                    visibility: hidden !important;
                    opacity: 0 !important;
                    height: 0 !important;
                    width: 0 !important;
                }
                
                /* Clean up the page appearance */
                body {
                    overflow-x: hidden !important;
                }
                
                /* Ensure content is centered and readable */
                .content, .main-content, article {
                    max-width: 100% !important;
                }
            ";

            string script = $@"
                (function() {{
                    var style = document.createElement('style');
                    style.textContent = `{customCss}`;
                    document.head.appendChild(style);
                    
                    // Remove elements with ad-related attributes
                    setTimeout(function() {{
                        var adElements = document.querySelectorAll('[class*=""ad-""], [id*=""ad-""], .advertisement, .ads');
                        adElements.forEach(function(el) {{
                            el.remove();
                        }});
                    }}, 1000);
                }})();
            ";

            try
            {
                await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error injecting styles: {ex.Message}");
            }
        }

        // ===== PAGE NAVIGATION METHODS =====
        
        private void NavigateToPage(int pageNumber)
        {
            if (pageNumber < 1 || pageNumber > _totalPages)
                return;
                
            int targetPageInVolume = _startPage + pageNumber - 1;
            string pageUrl = $"{_volumeBaseUrl}/{targetPageInVolume}/";
            webView?.CoreWebView2?.Navigate(pageUrl);
            
            System.Diagnostics.Debug.WriteLine($"📄 Navigating to page {pageNumber} (Volume page {targetPageInVolume}): {pageUrl}");
        }

        private int ExtractPageNumberFromUrl(string url)
        {
            // Extract page number from URL
            var match = Regex.Match(url, @"/(\d+)/?$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int pageInVolume))
            {
                int internalPage = pageInVolume - _startPage + 1;
                if (internalPage >= 1 && internalPage <= _totalPages)
                    return internalPage;
            }
            return 0;
        }

        private void UpdatePageControls()
        {
            // Update slider without triggering event
            pageSlider.ValueChanged -= PageSlider_ValueChanged;
            pageSlider.Value = _currentPage;
            pageSlider.ValueChanged += PageSlider_ValueChanged;
            
            // Update page info display
            txtPageInfo.Text = $"{_currentPage} / {_totalPages}";
            
            // Enable/disable navigation buttons
            btnPrevPage.IsEnabled = _currentPage > 1;
            btnNextPage.IsEnabled = _currentPage < _totalPages;
        }

        // ===== EVENT HANDLERS =====

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdatePageControls();
                NavigateToPage(_currentPage);
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                UpdatePageControls();
                NavigateToPage(_currentPage);
            }
        }

        private void PageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int newPage = (int)Math.Round(e.NewValue);
            
            if (newPage != _currentPage)
            {
                _currentPage = newPage;
                
                // Update display immediately
                txtPageInfo.Text = $"{_currentPage} / {_totalPages}";
                
                // Update button states
                btnPrevPage.IsEnabled = _currentPage > 1;
                btnNextPage.IsEnabled = _currentPage < _totalPages;
                
                // Only navigate if not dragging (to avoid lag)
                if (!_isSliderDragging)
                {
                    NavigateToPage(_currentPage);
                }
            }
        }

        private void Slider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _isSliderDragging = true;
        }

        private void Slider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            _isSliderDragging = false;
            NavigateToPage(_currentPage);
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            webView?.Reload();
        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            UpdatePageControls();
            webView?.CoreWebView2?.Navigate(_book.Url);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // ===== KEYBOARD SHORTCUTS =====

        private void BookViewer_KeyDown(object sender, KeyEventArgs e)
        {
            // Only handle if not typing in a textbox
            if (e.OriginalSource is System.Windows.Controls.TextBox)
                return;

            switch (e.Key)
            {
                case Key.Left:
                    PrevPage_Click(null!, null!);
                    e.Handled = true;
                    break;

                case Key.Right:
                    NextPage_Click(null!, null!);
                    e.Handled = true;
                    break;

                case Key.Home:
                    _currentPage = 1;
                    UpdatePageControls();
                    NavigateToPage(_currentPage);
                    e.Handled = true;
                    break;

                case Key.End:
                    _currentPage = _totalPages;
                    UpdatePageControls();
                    NavigateToPage(_currentPage);
                    e.Handled = true;
                    break;

                case Key.PageUp:
                    _currentPage = Math.Max(1, _currentPage - 10);
                    UpdatePageControls();
                    NavigateToPage(_currentPage);
                    e.Handled = true;
                    break;

                case Key.PageDown:
                    _currentPage = Math.Min(_totalPages, _currentPage + 10);
                    UpdatePageControls();
                    NavigateToPage(_currentPage);
                    e.Handled = true;
                    break;

                case Key.F5:
                    BtnRefresh_Click(null!, null!);
                    e.Handled = true;
                    break;

                case Key.Escape:
                    this.Close();
                    e.Handled = true;
                    break;
            }
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


