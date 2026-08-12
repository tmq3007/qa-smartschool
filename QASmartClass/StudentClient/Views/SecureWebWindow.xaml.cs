using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Serilog;

namespace QASmartClass.StudentClient.Views
{
    public partial class SecureWebWindow : Window
    {
        private bool _isWebViewInitialized = false;
        private readonly HashSet<string> _allowedHosts = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _rawAllowedUrls = new();
        private bool _blockTranslators = true;
        private bool _disableContextMenu = true;

        private void LoadSettingsFromDb()
        {
            try
            {
                using var db = new QASmartClass.Data.AppDbContext();
                var blockTranslatorsSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Web_BlockTranslators");
                _blockTranslators = (blockTranslatorsSetting?.Value ?? "Enabled") == "Enabled";

                var disableContextMenuSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Web_DisableContextMenu");
                _disableContextMenu = (disableContextMenuSetting?.Value ?? "Enabled") == "Enabled";
            }
            catch (Exception ex)
            {
                Log.Warning("[SecureWebWindow] Failed to load settings from DB: {Err}", ex.Message);
            }
        }

        public SecureWebWindow(string initialUrl)
        {
            InitializeComponent();
            
            // Add default OAuth providers
            _allowedHosts.Add("accounts.google.com");
            _allowedHosts.Add("login.microsoftonline.com");

            LoadSettingsFromDb();

            AddAllowedUrl(initialUrl);

            this.Loaded += async (s, e) => await InitializeWebViewAsync(initialUrl);
        }

        public void AddAllowedUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            lock (_rawAllowedUrls)
            {
                if (_rawAllowedUrls.Contains(url)) return;
                _rawAllowedUrls.Add(url);
            }

            try
            {
                var uri = new Uri(url);
                string host = uri.Host;
                lock (_allowedHosts)
                {
                    _allowedHosts.Add(host);
                    if (host.StartsWith("www."))
                    {
                        _allowedHosts.Add(host.Substring(4));
                    }
                    else
                    {
                        _allowedHosts.Add("www." + host);
                    }
                }
                Log.Information("[SecureWebWindow] Added host {Host} to allowed list", host);
            }
            catch (Exception ex)
            {
                Log.Warning("[SecureWebWindow] Failed to parse allowed URL {Url}: {Err}", url, ex.Message);
            }
        }

        public void NavigateTo(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            
            AddAllowedUrl(url);
            
            if (_isWebViewInitialized && webView?.CoreWebView2 != null)
            {
                webView.CoreWebView2.Navigate(url);
            }
        }

        private bool IsUnitTest => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.StartsWith("xunit", StringComparison.OrdinalIgnoreCase));

        private async Task InitializeWebViewAsync(string url)
        {
            try
            {
                AddAllowedUrl(url);

                // Ensure WebView2 environment is created
                await webView.EnsureCoreWebView2Async(null);

                // Settings configuration
                webView.CoreWebView2.Settings.IsScriptEnabled = true;
                webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                if (_disableContextMenu)
                {
                    webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                }

                // Subscribe navigation starting & completed
                webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
                webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                webView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
                webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
                webView.CoreWebView2.FrameNavigationStarting += CoreWebView2_FrameNavigationStarting;

                webView.CoreWebView2.Navigate(url);
                _isWebViewInitialized = true;
                Log.Information("[SecureWebWindow] WebView2 initialized and navigating to: {Url}", url);
            }
            catch (Exception ex)
            {
                Log.Error("[SecureWebWindow] WebView2 init error: {Err}", ex.Message);
                if (!IsUnitTest)
                {
                    MessageBox.Show($"Lỗi khởi tạo trình duyệt:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                this.Close();
            }
        }

        public bool IsUrlAllowed(string targetUrl)
        {
            if (string.IsNullOrEmpty(targetUrl)) return false;
            if (targetUrl.StartsWith("about:") || targetUrl.StartsWith("data:")) return true;

            try
            {
                var uri = new Uri(targetUrl);
                string host = uri.Host;

                if (_blockTranslators)
                {
                    if (host.StartsWith("translate.google.", StringComparison.OrdinalIgnoreCase) ||
                        host.Equals("webcache.googleusercontent.com", StringComparison.OrdinalIgnoreCase) ||
                        host.Equals("translate.yandex.com", StringComparison.OrdinalIgnoreCase) ||
                        host.Equals("microsofttranslator.com", StringComparison.OrdinalIgnoreCase) ||
                        host.Equals("www.microsofttranslator.com", StringComparison.OrdinalIgnoreCase) ||
                        targetUrl.Contains("bing.com/translator", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Warning("[SecureWebWindow] Blocked proxy/translation URL: {Url}", targetUrl);
                        return false;
                    }
                }

                lock (_allowedHosts)
                {
                    return _allowedHosts.Contains(host) || _allowedHosts.Any(allowed => host.EndsWith("." + allowed));
                }
            }
            catch
            {
                return false;
            }
        }

        private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            // Show loading indicators
            loadingPanel.Visibility = Visibility.Visible;
            webView.Visibility = Visibility.Collapsed;

            string targetUrl = e.Uri;
            
            if (targetUrl.StartsWith("about:") || targetUrl.StartsWith("data:"))
            {
                return;
            }

            try
            {
                bool isAllowed = IsUrlAllowed(targetUrl);

                if (!isAllowed)
                {
                    // Block navigation!
                    e.Cancel = true;
                    loadingPanel.Visibility = Visibility.Collapsed;
                    webView.Visibility = Visibility.Visible;
                    
                    var uri = new Uri(targetUrl);
                    string host = uri.Host;
                    if (!IsUnitTest)
                    {
                        MessageBox.Show(
                            $"Truy cập bị chặn!\n\nWebsite '{host}' không nằm trong danh sách website được giáo viên phát.", 
                            "QA SmartClass - Trình duyệt an toàn", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Warning);
                    }
                    
                    Log.Warning("[SecureWebWindow] Blocked unauthorized navigation to {Url}", targetUrl);
                }
                else
                {
                    Log.Information("[SecureWebWindow] Navigating to allowed URL: {Url}", targetUrl);
                }
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                loadingPanel.Visibility = Visibility.Collapsed;
                webView.Visibility = Visibility.Visible;
                Log.Warning("[SecureWebWindow] Navigation validation failed: {Err}", ex.Message);
            }
        }

        private void CoreWebView2_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            if (e == null) return;
            string targetUrl = e.Uri;
            
            // Set Handled to true to cancel opening in system browser / new window
            e.Handled = true;

            try
            {
                bool isAllowed = IsUrlAllowed(targetUrl);
                if (isAllowed)
                {
                    Log.Information("[SecureWebWindow] Redirecting allowed new window request to current WebView2: {Url}", targetUrl);
                    webView.CoreWebView2.Navigate(targetUrl);
                }
                else
                {
                    Log.Warning("[SecureWebWindow] Blocked unauthorized new window request to {Url}", targetUrl);
                    if (!IsUnitTest)
                    {
                        var uri = new Uri(targetUrl);
                        string host = uri.Host;
                        MessageBox.Show(
                            $"Truy cập bị chặn!\n\nWebsite '{host}' không nằm trong danh sách website được giáo viên phát.", 
                            "QA SmartClass - Trình duyệt an toàn", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[SecureWebWindow] NewWindowRequested validation failed: {Err}", ex.Message);
            }
        }

        private void CoreWebView2_FrameNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (e == null) return;
            string targetUrl = e.Uri;
            
            if (targetUrl.StartsWith("about:") || targetUrl.StartsWith("data:"))
            {
                return;
            }

            try
            {
                bool isAllowed = IsUrlAllowed(targetUrl);
                if (!isAllowed)
                {
                    e.Cancel = true;
                    Log.Warning("[SecureWebWindow] Blocked unauthorized subframe navigation to {Url}", targetUrl);
                }
                else
                {
                    Log.Information("[SecureWebWindow] Frame navigating to allowed URL: {Url}", targetUrl);
                }
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                Log.Warning("[SecureWebWindow] Frame navigation validation failed: {Err}", ex.Message);
            }
        }

        private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            loadingPanel.Visibility = Visibility.Collapsed;
            webView.Visibility = Visibility.Visible;
        }

        private void CoreWebView2_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            if (webView?.Source != null)
            {
                txtCurrentUrl.Text = webView.Source.ToString();
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_isWebViewInitialized && webView.CanGoBack)
            {
                webView.GoBack();
            }
        }

        private void btnForward_Click(object sender, RoutedEventArgs e)
        {
            if (_isWebViewInitialized && webView.CanGoForward)
            {
                webView.GoForward();
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (_isWebViewInitialized)
            {
                webView.Reload();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Check modifier keys
            bool isCtrlPressed = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control;
            bool isShiftPressed = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) == System.Windows.Input.ModifierKeys.Shift;
            bool isAltPressed = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) == System.Windows.Input.ModifierKeys.Alt;

            // 1. Block insecure keys (DevTools, Print, Save, New Window)
            if (e.Key == System.Windows.Input.Key.F12 || 
                (isCtrlPressed && isShiftPressed && e.Key == System.Windows.Input.Key.I) ||
                (isCtrlPressed && e.Key == System.Windows.Input.Key.N) ||
                (isCtrlPressed && e.Key == System.Windows.Input.Key.P) ||
                (isCtrlPressed && e.Key == System.Windows.Input.Key.S))
            {
                e.Handled = true; // Block event propagation
                Log.Warning("[SecureWebWindow] Blocked restricted keyboard shortcut: {Key} (Ctrl: {Ctrl}, Shift: {Shift}, Alt: {Alt})", e.Key, isCtrlPressed, isShiftPressed, isAltPressed);
                return;
            }

            // 2. Map safe navigation shortcuts
            if (e.Key == System.Windows.Input.Key.F5)
            {
                e.Handled = true;
                btnRefresh_Click(null!, null!);
            }
            else if (isAltPressed && e.Key == System.Windows.Input.Key.Left)
            {
                e.Handled = true;
                btnBack_Click(null!, null!);
            }
            else if (isAltPressed && e.Key == System.Windows.Input.Key.Right)
            {
                e.Handled = true;
                btnForward_Click(null!, null!);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (_isWebViewInitialized && webView?.CoreWebView2 != null)
                {
                    webView.CoreWebView2.NavigationStarting -= CoreWebView2_NavigationStarting;
                    webView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
                    webView.CoreWebView2.SourceChanged -= CoreWebView2_SourceChanged;
                    webView.CoreWebView2.NewWindowRequested -= CoreWebView2_NewWindowRequested;
                    webView.CoreWebView2.FrameNavigationStarting -= CoreWebView2_FrameNavigationStarting;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[SecureWebWindow] Cleanup error: {Err}", ex.Message);
            }

            base.OnClosing(e);
        }
    }
}
