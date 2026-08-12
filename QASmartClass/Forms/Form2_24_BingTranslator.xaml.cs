using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace QASmartTouch.Forms
{
    public partial class Form2_24_BingTranslator : Window
    {
        public Form2_24_BingTranslator() : this(string.Empty)
        {
        }

        public Form2_24_BingTranslator(string initialText)
        {
            try
            {
                // Check if WebView2 runtime is installed to prevent crash
                string version = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
                System.Diagnostics.Debug.WriteLine($"WebView2 Runtime found: {version}");
            }
            catch (Exception)
            {
                MessageBox.Show("Hệ thống chưa cài đặt Microsoft Edge WebView2 Runtime.\n\nVui lòng cài đặt WebView2 Runtime từ Microsoft để sử dụng tính năng này.",
                              "Yêu cầu hệ thống (WebView2)",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                this.Loaded += (s, e) => this.Close();
                return;
            }

            InitializeComponent();
            _ = InitializeWebView();

            if (!string.IsNullOrWhiteSpace(initialText))
            {
                string encodedText = Uri.EscapeDataString(initialText);
                string url = $"https://www.bing.com/translator?from=auto&to=vi&isTTRefreshQuery=1&text={encodedText}";
                this.Loaded += (s, e) =>
                {
                    if (webView != null)
                    {
                        webView.Source = new Uri(url);
                    }
                };
            }
        }

        private async System.Threading.Tasks.Task InitializeWebView()
        {
            try
            {
                // Initialize WebView2
                await webView.EnsureCoreWebView2Async(null);
                
                System.Diagnostics.Debug.WriteLine("✅ Bing Translator WebView2 initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Bing Translator WebView2 initialization failed: {ex.Message}");
                MessageBox.Show($"Lỗi khi khởi tạo trình duyệt:\n{ex.Message}",
                              "Lỗi",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                this.Close();
            }
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Hide loading panel
            LoadingPanel.Visibility = Visibility.Collapsed;

            if (e.IsSuccess)
            {
                System.Diagnostics.Debug.WriteLine("✅ Bing Translator loaded successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Navigation failed: {e.WebErrorStatus}");
                MessageBox.Show("Không thể tải Bing Translator.\nVui lòng kiểm tra kết nối internet.",
                              "Lỗi",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
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

