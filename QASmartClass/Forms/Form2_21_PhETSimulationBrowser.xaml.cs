using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;

namespace QASmartTouch.Forms
{
    /// <summary>
    /// PhET Interactive Simulations Browser
    /// Displays PhET simulations using WebView2 and allows embedding into canvas
    /// </summary>
    public partial class Form2_21_PhETSimulationBrowser : Window
    {
        private Form2_MainDashboard _mainDashboard;
        private bool _isWebViewInitialized = false;
        
        public Form2_21_PhETSimulationBrowser(string initialUrl = null)
        {
            InitializeComponent();
            Topmost = true;
            
            // Set default URL if not provided
            string targetUrl = initialUrl ?? "https://phet.colorado.edu/vi/simulations/browse?type=html";
            
            // Initialize WebView2 asynchronously
            this.Loaded += async (s, e) => await InitializeWebViewAsync(targetUrl);
        }
        
        /// <summary>
        /// Set reference to main dashboard for inserting content
        /// </summary>
        public void SetMainDashboard(Form2_MainDashboard dashboard)
        {
            _mainDashboard = dashboard;
        }
        
        /// <summary>
        /// Initialize WebView2 and navigate to URL
        /// </summary>
        private async Task InitializeWebViewAsync(string url)
        {
            try
            {
                // Ensure WebView2 runtime is initialized
                await webView.EnsureCoreWebView2Async(null);
                
                // Configure WebView2 settings
                webView.CoreWebView2.Settings.IsScriptEnabled = true;
                webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                
                // Subscribe to navigation events
                webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
                webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                
                // Navigate to URL
                webView.CoreWebView2.Navigate(url);
                
                _isWebViewInitialized = true;
                System.Diagnostics.Debug.WriteLine($"✅ WebView2 initialized and navigating to: {url}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ WebView2 initialization error: {ex.Message}");
                MessageBox.Show($"Lỗi khởi tạo trình duyệt:\n{ex.Message}", "Lỗi", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }
        
        /// <summary>
        /// Handle navigation starting event
        /// </summary>
        private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            // Show loading indicator
            loadingPanel.Visibility = Visibility.Visible;
            webView.Visibility = Visibility.Collapsed;
            
            System.Diagnostics.Debug.WriteLine($"🔄 Navigating to: {e.Uri}");
        }
        
        /// <summary>
        /// Handle navigation completed event
        /// </summary>
        private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Hide loading indicator
            loadingPanel.Visibility = Visibility.Collapsed;
            webView.Visibility = Visibility.Visible;
            
            if (e.IsSuccess)
            {
                System.Diagnostics.Debug.WriteLine($"✅ Navigation completed successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Navigation failed: {e.WebErrorStatus}");
                MessageBox.Show($"Không thể tải trang.\nLỗi: {e.WebErrorStatus}", "Lỗi tải trang", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        /// <summary>
        /// Navigate back in browser history
        /// </summary>
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_isWebViewInitialized && webView.CanGoBack)
            {
                webView.GoBack();
            }
        }
        
        /// <summary>
        /// Reload current page
        /// </summary>
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (_isWebViewInitialized)
            {
                webView.Reload();
            }
        }
        
        /// <summary>
        /// Capture screenshot of current simulation
        /// </summary>
        private async void btnCapture_Click(object sender, RoutedEventArgs e)
        {
            if (!_isWebViewInitialized)
            {
                return; // Silently return if not ready
            }
            
            try
            {
                // Capture screenshot using WebView2
                var screenshot = await CaptureWebViewScreenshotAsync();
                
                if (screenshot != null && _mainDashboard != null)
                {
                    // Insert screenshot to canvas
                    _mainDashboard.InsertImageToCanvas(screenshot);
                    System.Diagnostics.Debug.WriteLine("✅ PhET simulation screenshot captured and inserted");
                    
                    // Close browser after successful capture
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Screenshot capture error: {ex.Message}");
                // Silently fail, just log to debug
            }
        }
        
        /// <summary>
        /// Capture screenshot from WebView2
        /// </summary>
        private async Task<BitmapImage> CaptureWebViewScreenshotAsync()
        {
            try
            {
                // Create temporary file for screenshot
                string tempFile = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(), 
                    $"phet_screenshot_{DateTime.Now:yyyyMMddHHmmss}.png"
                );
                
                // Capture screenshot to file
                using (var fs = System.IO.File.Create(tempFile))
                {
                    await webView.CoreWebView2.CapturePreviewAsync(
                        CoreWebView2CapturePreviewImageFormat.Png, 
                        fs
                    );
                }
                
                // Load image from file
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(tempFile);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                
                // Clean up temp file
                try { System.IO.File.Delete(tempFile); } catch { }
                
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Screenshot capture failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Embed current simulation URL into canvas
        /// </summary>
        private void btnEmbed_Click(object sender, RoutedEventArgs e)
        {
            if (!_isWebViewInitialized)
            {
                return; // Silently return if not ready
            }
            
            try
            {
                string currentUrl = webView.Source?.ToString();
                
                if (string.IsNullOrEmpty(currentUrl))
                {
                    return; // Silently return if no URL
                }
                
                // Check if URL is a PhET simulation (not the browse page)
                if (currentUrl.Contains("/sims/") || currentUrl.Contains("/simulations/"))
                {
                    if (_mainDashboard != null)
                    {
                        _mainDashboard.InsertPhETSimulation(currentUrl);
                        System.Diagnostics.Debug.WriteLine($"✅ PhET simulation embedded: {currentUrl}");
                        this.Close();
                    }
                }
                else
                {
                    // Just log, don't show message
                    System.Diagnostics.Debug.WriteLine("⚠️ Not a specific simulation URL");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Embed error: {ex.Message}");
                // Silently fail, just log to debug
            }
        }
        
        /// <summary>
        /// Close the browser window
        /// </summary>
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        /// <summary>
        /// Clean up WebView2 resources on window closing
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (_isWebViewInitialized && webView?.CoreWebView2 != null)
                {
                    webView.CoreWebView2.NavigationStarting -= CoreWebView2_NavigationStarting;
                    webView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Cleanup error: {ex.Message}");
            }
            
            base.OnClosing(e);
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
