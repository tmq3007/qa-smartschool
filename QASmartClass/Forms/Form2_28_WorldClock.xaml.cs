using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace QASmartTouch.Forms
{
    public partial class Form2_28_WorldClock : Window
    {
        public Form2_28_WorldClock()
        {
            InitializeComponent();
            this.Loaded += async (s, e) => await InitializeWebView();
        }

        private async System.Threading.Tasks.Task InitializeWebView()
        {
            try
            {
                // Initialize WebView2 với cấu hình UserDataFolder an toàn từ WebView2Helper
                await QASmartTouch.Helpers.WebView2Helper.EnsureInitializedAsync(webView);
                
                System.Diagnostics.Debug.WriteLine("✅ World Clock WebView2 initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ World Clock WebView2 initialization failed: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("✅ World Clock loaded successfully");
                
                // Inject ad blocker script
                InjectAdBlockerScript();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Navigation failed: {e.WebErrorStatus}");
                MessageBox.Show("Không thể tải Thời Gian Thực.\nVui lòng kiểm tra kết nối internet.",
                              "Lỗi",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
            }
        }

        private async void InjectAdBlockerScript()
        {
            try
            {
                string adBlockScript = @"
                    (function() {
                        console.log('🚫 Ad Blocker: Starting for World Clock...');
                        
                        function hideAds() {
                            const adSelectors = [
                                // Google Ads
                                'iframe[src*=""ads""]',
                                'iframe[src*=""doubleclick""]',
                                'iframe[src*=""googlesyndication""]',
                                'ins.adsbygoogle',
                                '.adsbygoogle',
                                
                                // Video ads and popups
                                'video[autoplay]',
                                '[class*=""video-ad""]',
                                '[id*=""video-ad""]',
                                '[class*=""popup""]',
                                '[id*=""popup""]',
                                '.modal',
                                '[class*=""overlay""]',
                                '[class*=""lightbox""]',
                                
                                // FedEx and brand ads
                                '[class*=""fedex""]',
                                '[id*=""fedex""]',
                                'a[href*=""fedex""]',
                                '[class*=""sponsor""]',
                                '[id*=""sponsor""]',
                                
                                // Generic ad containers
                                '[class*=""advertisement""]',
                                '[id*=""advertisement""]',
                                '.banner',
                                '[class*=""banner""]'
                            ];
                            
                            let hiddenCount = 0;
                            adSelectors.forEach(selector => {
                                try {
                                    document.querySelectorAll(selector).forEach(el => {
                                        if (el && el.style.display !== 'none') {
                                            el.style.display = 'none';
                                            el.style.visibility = 'hidden';
                                            el.style.opacity = '0';
                                            el.style.height = '0';
                                            el.style.width = '0';
                                            el.style.overflow = 'hidden';
                                            hiddenCount++;
                                        }
                                    });
                                } catch (e) {
                                    console.warn('Selector failed:', selector, e);
                                }
                            });
                            
                            console.log('🚫 Ad Blocker: Hidden ' + hiddenCount + ' ad elements');
                            return hiddenCount;
                        }
                        
                        // Initial hide
                        hideAds();
                        
                        // Re-hide after delay
                        setTimeout(hideAds, 1000);
                        setTimeout(hideAds, 2000);
                        setTimeout(hideAds, 3000);
                        
                        // Monitor DOM changes
                        let timeout;
                        const observer = new MutationObserver(() => {
                            clearTimeout(timeout);
                            timeout = setTimeout(hideAds, 500);
                        });
                        
                        observer.observe(document.body, {
                            childList: true,
                            subtree: true
                        });
                        
                        console.log('🚫 Ad Blocker: Active for World Clock');
                    })();
                ";

                await webView.CoreWebView2.ExecuteScriptAsync(adBlockScript);
                System.Diagnostics.Debug.WriteLine("🚫 Ad blocker script injected successfully for World Clock");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to inject ad blocker: {ex.Message}");
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

