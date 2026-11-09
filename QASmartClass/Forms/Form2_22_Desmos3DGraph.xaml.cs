using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace QASmartTouch.Forms
{
    public partial class Form2_22_Desmos3DGraph : Window
    {
        public string? GraphUrl { get; private set; }
        public bool WasInserted { get; private set; } = false;

        public Form2_22_Desmos3DGraph()
        {
            InitializeComponent();
            QASmartTouch.Helpers.TouchActivationHelper.ApplyToWindow(this);
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                // Initialize WebView2
                await webView.EnsureCoreWebView2Async(null);
                
                System.Diagnostics.Debug.WriteLine("✅ WebView2 initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ WebView2 initialization failed: {ex.Message}");
                MessageBox.Show($"Lỗi khi khởi tạo WebView2:\n{ex.Message}",
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
                System.Diagnostics.Debug.WriteLine("✅ Desmos 3D loaded successfully");
                
                // Optional: Inject custom CSS to hide Desmos header/footer if needed
                // InjectCustomStyles();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Navigation failed: {e.WebErrorStatus}");
                MessageBox.Show("Không thể tải Desmos 3D Calculator.\nVui lòng kiểm tra kết nối internet.",
                              "Lỗi",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
            }
        }

        private async void InjectCustomStyles()
        {
            try
            {
                // Hide Desmos header to maximize graph area
                string script = @"
                    (function() {
                        const style = document.createElement('style');
                        style.textContent = `
                            .dcg-header { display: none !important; }
                            .dcg-action-bar { display: none !important; }
                        `;
                        document.head.appendChild(style);
                    })();
                ";
                
                await webView.CoreWebView2.ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine("✅ Custom styles injected");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Failed to inject styles: {ex.Message}");
            }
        }

        private async void btnInsert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get current URL (includes graph state)
                GraphUrl = webView.Source?.ToString();
                
                if (string.IsNullOrEmpty(GraphUrl))
                {
                    MessageBox.Show("Không thể lấy URL đồ thị.\nVui lòng thử lại.",
                                  "Lỗi",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                // Optional: Get graph state via JavaScript
                string stateScript = @"
                    (function() {
                        try {
                            return window.location.href;
                        } catch(e) {
                            return null;
                        }
                    })();
                ";
                
                string result = await webView.CoreWebView2.ExecuteScriptAsync(stateScript);
                if (!string.IsNullOrEmpty(result) && result != "null")
                {
                    GraphUrl = result.Trim('"');
                }

                System.Diagnostics.Debug.WriteLine($"📊 Graph URL: {GraphUrl}");

                WasInserted = true;
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Insert failed: {ex.Message}");
                MessageBox.Show($"Lỗi khi chèn đồ thị:\n{ex.Message}",
                              "Lỗi",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            WasInserted = false;
            this.DialogResult = false;
            this.Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            btnCancel_Click(sender, e);
        }
    }
}
