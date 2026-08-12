using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace QASmartTouch.Forms
{
    public partial class Form2_27_LichAmDuong : Window
    {
        public Form2_27_LichAmDuong()
        {
            InitializeComponent();
            _ = InitializeWebView();
        }

        private async System.Threading.Tasks.Task InitializeWebView()
        {
            try
            {
                // Initialize WebView2
                await webView.EnsureCoreWebView2Async(null);
                
                System.Diagnostics.Debug.WriteLine("✅ Lich Am Duong WebView2 initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lich Am Duong WebView2 initialization failed: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("✅ Lich Am Duong loaded successfully");
                
                // Inject ad blocker script
                InjectAdBlockerScript();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ Navigation failed: {e.WebErrorStatus}");
                MessageBox.Show("Không thể tải Lịch Âm Dương.\nVui lòng kiểm tra kết nối internet.",
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
                        console.log('🚫 Ad Blocker: Starting (Refined)...');
                        
                        function hideAds() {
                            // Only hide SPECIFIC ad containers, not generic divs
                            const adSelectors = [
                                // Google Ads iframes
                                'iframe[src*=""ads""]',
                                'iframe[src*=""doubleclick""]',
                                'iframe[src*=""googlesyndication""]',
                                'iframe[src*=""adservice""]',
                                
                                // Google AdSense
                                'ins.adsbygoogle',
                                '.adsbygoogle',
                                
                                // Specific ad containers (not generic)
                                '[id*=""google_ads""]',
                                '[id*=""google-ad""]',
                                '[class*=""google-ad""]',
                                
                                // VietnamNet specific ad containers
                                '.banner-ads',
                                '.ads-container',
                                '.box-category-ads',
                                '#div-gpt-ad',
                                '[id^=""div-gpt-ad""]',
                                
                                // Banner containers
                                '.banner-top',
                                '.banner-bottom',
                                '.banner-sidebar',
                                
                                // Advertisement labels
                                'div[class=""advertisement""]',
                                'div[id=""advertisement""]',
                                
                                // VietnamNet Header & Navigation
                                'header',
                                'nav',
                                '.header',
                                '.navigation',
                                '.main-nav',
                                '.top-nav',
                                '[class*=""header""]',
                                '[class*=""nav-""]',
                                
                                // VietnamNet Footer
                                'footer',
                                '.footer',
                                '[class*=""footer""]',
                                '.site-footer',
                                '.page-footer',
                                '[id*=""footer""]',
                                
                                // Sidebar & Featured Events
                                'aside',
                                '.sidebar',
                                '.side-bar',
                                '[class*=""sidebar""]',
                                '[class*=""featured""]',
                                '.widget',
                                '[class*=""widget""]',
                                '.related',
                                '[class*=""related""]',
                                
                                // Utility buttons (Giá vàng, Thời tiết, etc)
                                '.utility-buttons',
                                '[class*=""utility""]',
                                '.quick-links',
                                '[class*=""quick-link""]',
                                '.box-link',
                                '[class*=""box-link""]',
                                
                                // Video ads and popups
                                'video[autoplay]',
                                '[class*=""video-ad""]',
                                '[id*=""video-ad""]',
                                '[class*=""popup""]',
                                '[id*=""popup""]',
                                '.modal',
                                '[class*=""overlay""]',
                                '[class*=""lightbox""]',
                                
                                // FedEx and other brand ads
                                '[class*=""fedex""]',
                                '[id*=""fedex""]',
                                'a[href*=""fedex""]',
                                '[class*=""sponsor""]',
                                '[id*=""sponsor""]'
                            ];
                            
                            let hiddenCount = 0;
                            adSelectors.forEach(selector => {
                                try {
                                    document.querySelectorAll(selector).forEach(el => {
                                        // Extra check: Don't hide if inside calendar container
                                        const isInCalendar = el.closest('.calendar') || 
                                                           el.closest('.lich-van-nien') ||
                                                           el.closest('[class*=""calendar""]');
                                        
                                        if (!isInCalendar && el.style.display !== 'none') {
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
                        
                        // Re-hide after delay (for lazy-loaded ads)
                        setTimeout(hideAds, 1000);
                        setTimeout(hideAds, 2000);
                        setTimeout(hideAds, 3000);
                        
                        // Monitor DOM changes (with throttling)
                        let timeout;
                        const observer = new MutationObserver(() => {
                            clearTimeout(timeout);
                            timeout = setTimeout(hideAds, 500);
                        });
                        
                        observer.observe(document.body, {
                            childList: true,
                            subtree: true
                        });
                        
                        console.log('🚫 Ad Blocker: Active (Refined mode - Calendar protected)');
                    })();
                ";

                await webView.CoreWebView2.ExecuteScriptAsync(adBlockScript);
                System.Diagnostics.Debug.WriteLine("🚫 Ad blocker script injected successfully (Refined)");
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

