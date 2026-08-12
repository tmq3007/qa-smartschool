using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace QASmartTouch.Forms
{
    public partial class Form2_25_WikipediaBrowser : Window
    {
        public Form2_25_WikipediaBrowser()
        {
            InitializeComponent();
            Topmost = true;
            _ = InitializeWebView();
        }

        private async System.Threading.Tasks.Task InitializeWebView()
        {
            try
            {
                // Initialize WebView2
                await webView.EnsureCoreWebView2Async(null);
                
                System.Diagnostics.Debug.WriteLine("✅ Wikipedia WebView2 initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Wikipedia WebView2 initialization failed: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("✅ Wikipedia loaded successfully");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Navigation failed: {e.WebErrorStatus}");
                MessageBox.Show("Không thể tải Wikipedia.\nVui lòng kiểm tra kết nối internet.",
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

