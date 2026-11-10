using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using QASmartTouch.Helpers;
using QASmartTouch.Services.Tools;

namespace QASmartTouch.Forms
{
    /// <summary>
    /// Cửa sổ Kho Học Liệu 3D & Phòng Thí Nghiệm Ảo YooBook (Form2_26_YooBookBrowser).
    /// Tích hợp:
    /// 1. Chế độ "Chỉ hiển thị Kho dữ liệu" (Strict Explore-Only Mode): tự động ẩn Header/Footer/Bảng giá của website.
    /// 2. Tự động đăng nhập mặc định (Silent Auto-Login) vào tài khoản Demo YooBook khi đã cấu hình.
    /// 3. Cổng kết nối Eduverse Partner API (X-Api-Key) qua YooBookPartnerApiService.
    /// 4. Chụp mô hình 3D đưa thẳng vào bảng viết hoặc nhúng trực tiếp khung 3D lên bảng viết.
    /// </summary>
    public partial class Form2_26_YooBookBrowser : Window
    {
        private Form2_MainDashboard? _mainDashboard;
        private bool _isWebViewInitialized = false;
        private bool _hasAttemptedAutoLoginInSession = false;
        private string _targetExploreUrl = YooBookPartnerApiService.ExploreBaseUrl;

        public Form2_26_YooBookBrowser(string? initialUrl = null)
        {
            InitializeComponent();
            Topmost = true;

            if (!string.IsNullOrWhiteSpace(initialUrl))
            {
                _targetExploreUrl = initialUrl!;
            }

            // Kích hoạt cảm ứng 1-chạm cho màn hình tương tác 86 inch
            TouchActivationHelper.Apply(this);
            WireCloseButtonTouch();

            this.Loaded += async (s, e) =>
            {
                WireAllButtons(this);
                UpdateStatusBadge();
                HighlightCategoryButton(btnCatAll);
                await InitializeWebViewAsync(_targetExploreUrl);
            };
        }

        public void SetMainDashboard(Form2_MainDashboard dashboard)
        {
            _mainDashboard = dashboard;
        }

        private void WireCloseButtonTouch()
        {
            if (btnClose == null) return;
            Stylus.SetIsPressAndHoldEnabled(btnClose, false);

            btnClose.PreviewTouchDown += (s, e) =>
            {
                e.TouchDevice.Capture(btnClose);
                e.Handled = true;
            };
            btnClose.PreviewTouchUp += (s, e) =>
            {
                if (e.TouchDevice.Captured == btnClose)
                    btnClose.ReleaseTouchCapture(e.TouchDevice);
                e.Handled = true;
                this.Close();
            };
            btnClose.PreviewStylusDown += (s, e) =>
            {
                e.StylusDevice.Capture(btnClose);
                e.Handled = true;
            };
            btnClose.PreviewStylusUp += (s, e) =>
            {
                if (e.StylusDevice.Captured == btnClose)
                    btnClose.ReleaseStylusCapture();
                e.Handled = true;
                this.Close();
            };
        }

        private void WireAllButtons(DependencyObject parent)
        {
            if (parent == null) return;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is Button btn && btn != btnClose)
                {
                    TouchActivationHelper.WireButton(btn);
                }
                WireAllButtons(child);
            }
        }

        private void UpdateStatusBadge()
        {
            var svc = YooBookPartnerApiService.Instance;
            if (svc.HasDemoCredentials)
            {
                txtStatusBadge.Text = $"Tự động đăng nhập: {svc.GetEffectiveDemoUsername()}";
            }
            else if (svc.HasPartnerApiKey)
            {
                txtStatusBadge.Text = "Cổng Partner API: Đã cấu hình Key";
            }
            else
            {
                txtStatusBadge.Text = "Chế độ: Chỉ hiển thị Kho dữ liệu";
            }
        }

        /// <summary>
        /// Khởi tạo WebView2, gắn bộ lọc "Chỉ hiển thị Kho dữ liệu" và kịch bản Tự động đăng nhập tài khoản Demo.
        /// </summary>
        private async Task InitializeWebViewAsync(string url)
        {
            try
            {
                WebView2Helper.InitializeEnvironment();
                var initTask = WebView2Helper.EnsureInitializedAsync(webView);
                if (await Task.WhenAny(initTask, Task.Delay(TimeSpan.FromSeconds(30))) != initTask)
                {
                    throw new TimeoutException("Trình duyệt WebView2 không khởi tạo được trong 30 giây. Vui lòng kiểm tra Microsoft Edge WebView2 Runtime.");
                }
                await initTask;

                var core = webView.CoreWebView2;
                core.Settings.IsScriptEnabled = true;
                core.Settings.AreDefaultScriptDialogsEnabled = true;
                core.Settings.IsWebMessageEnabled = true;
                core.Settings.AreDevToolsEnabled = false;
                core.Settings.AreDefaultContextMenusEnabled = false;

                // Đăng ký script chạy ngay khi tài liệu vừa khởi tạo để ẩn tức thì Header/Footer của web yoobook.vn
                await RegisterExploreOnlyScriptAsync(core);

                core.NavigationStarting += CoreWebView2_NavigationStarting;
                core.NavigationCompleted += CoreWebView2_NavigationCompleted;
                core.WebMessageReceived += CoreWebView2_WebMessageReceived;
                core.NewWindowRequested += CoreWebView2_NewWindowRequested;

                core.Navigate(url);
                _isWebViewInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Form2_26_YooBookBrowser] WebView2 init error: {ex.Message}");
                MessageBox.Show($"Lỗi khởi tạo Kho học liệu YooBook:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        /// <summary>
        /// Tiêm CSS & JS ngay từ lúc trang web bắt đầu tạo DOM:
        /// 1. Ẩn hoàn toàn Header điều hướng ("Trang chủ | Bảng giá | API..."), khoảng trống h-16 trên đỉnh và Footer cuối trang.
        /// 2. Chỉ giữ lại đúng phần ruột Kho dữ liệu (/explore) và khung tương tác 3D.
        /// </summary>
        private static async Task RegisterExploreOnlyScriptAsync(CoreWebView2 core)
        {
            const string script = @"
(function() {
    function injectExploreOnlyStyle() {
        if (document.getElementById('qa-smarttouch-yoobook-style')) return;
        var style = document.createElement('style');
        style.id = 'qa-smarttouch-yoobook-style';
        style.textContent = `
            /* Ẩn thanh Header menu trên cùng (Trang chủ, Bảng giá, API, Đội ngũ cố vấn...) */
            header.yb-app,
            header.yb-glass-nav,
            body > div > header,
            body > div > header + div.h-16 {
                display: none !important;
                height: 0 !important;
                overflow: hidden !important;
            }
            /* Ẩn toàn bộ chân trang Footer thông tin công ty & chính sách */
            footer,
            body > div > footer {
                display: none !important;
            }
            /* Đẩy nội dung Kho học liệu sát lên trên cùng, toàn màn hình */
            main {
                padding-top: 4px !important;
                margin-top: 0 !important;
            }
        `;
        (document.head || document.documentElement).appendChild(style);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', injectExploreOnlyStyle);
    } else {
        injectExploreOnlyStyle();
    }
    new MutationObserver(injectExploreOnlyStyle).observe(document.documentElement, { childList: true, subtree: true });
})();";
            await core.AddScriptToExecuteOnDocumentCreatedAsync(script);
        }

        /// <summary>
        /// Chặn mọi liên kết mở tab mới (như bấm vào bài 3D mở sang tab mới) và giữ lại ngay trong cửa sổ hiện tại.
        /// </summary>
        private void CoreWebView2_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            if (!string.IsNullOrWhiteSpace(e.Uri) && webView?.CoreWebView2 != null)
            {
                webView.CoreWebView2.Navigate(e.Uri);
            }
        }

        /// <summary>
        /// Khóa điều hướng (Navigation Guard):
        /// Chỉ cho phép hiển thị trang Kho dữ liệu (/explore), trang chạy mô phỏng 3D và trang đăng nhập ngầm (/auth/login).
        /// Nếu trang web tự chuyển về Trang chủ (/), Bảng giá (/pricing), API (/api), Tin tức (/news)... thì tự động đưa về Kho dữ liệu (/explore).
        /// </summary>
        private void CoreWebView2_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            try
            {
                if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
                {
                    string host = uri.Host.ToLowerInvariant();
                    string path = uri.AbsolutePath.TrimEnd('/').ToLowerInvariant();

                    if (host.Contains("yoobook.vn"))
                    {
                        bool isBlockedMarketingPage =
                            string.IsNullOrEmpty(path) || // Trang chủ "/"
                            path.StartsWith("/pricing") ||
                            path.StartsWith("/api") ||
                            path.StartsWith("/consulting-team") ||
                            path.StartsWith("/news") ||
                            path.StartsWith("/policies") ||
                            path == "/teachers";

                        if (isBlockedMarketingPage)
                        {
                            e.Cancel = true;
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    webView?.CoreWebView2?.Navigate(_targetExploreUrl);
                                }
                                catch { }
                            }));
                            return;
                        }
                    }
                }
            }
            catch { }

            txtLoadingTitle.Text = "Đang tải Kho dữ liệu 3D YooBook...";
            txtLoadingSubtitle.Text = "Vui lòng đợi trong giây lát...";
            loadingPanel.Visibility = Visibility.Visible;
        }

        private async void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                if (webView?.CoreWebView2 == null)
                {
                    loadingPanel.Visibility = Visibility.Collapsed;
                    return;
                }

                string currentUrl = webView.Source?.ToString() ?? string.Empty;

                // Áp dụng lại bộ lọc ẩn Header/Footer và kiểm tra trạng thái Tự động đăng nhập Demo
                await ApplyExploreCleanUpAndAutoLoginAsync(currentUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Form2_26_YooBookBrowser] NavigationCompleted error: {ex.Message}");
            }
            finally
            {
                loadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái đăng nhập trên yoobook.vn và tự động đăng nhập bằng Tài khoản Demo nếu chưa đăng nhập.
        /// </summary>
        private async Task ApplyExploreCleanUpAndAutoLoginAsync(string currentUrl)
        {
            if (webView?.CoreWebView2 == null) return;

            var svc = YooBookPartnerApiService.Instance;
            string demoUser = svc.GetEffectiveDemoUsername();
            string demoPass = svc.GetEffectiveDemoPassword();

            if (string.IsNullOrWhiteSpace(demoUser) || string.IsNullOrEmpty(demoPass))
                return;

            string userJson = JsonConvert.ToString(demoUser);
            string passJson = JsonConvert.ToString(demoPass);
            string targetExploreJson = JsonConvert.ToString(_targetExploreUrl);

            // Trường hợp 1: Đang ở trang /auth/login -> Tự động điền Email/SĐT + Mật khẩu và bấm nút Đăng nhập
            if (currentUrl.IndexOf("/auth/login", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                txtLoadingTitle.Text = "Đang tự động đăng nhập tài khoản Demo YooBook...";
                txtLoadingSubtitle.Text = $"Tài khoản: {demoUser}";
                loadingPanel.Visibility = Visibility.Visible;

                string autoFillScript = $@"
(function() {{
    if (window.__qaAutoLoginRunning) return;
    window.__qaAutoLoginRunning = true;

    function setReactInputValue(el, val) {{
        if (!el) return false;
        var nativeInputValueSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
        nativeInputValueSetter.call(el, val);
        el.dispatchEvent(new Event('input', {{ bubbles: true }}));
        el.dispatchEvent(new Event('change', {{ bubbles: true }}));
        return true;
    }}

    var attempts = 0;
    var timer = setInterval(function() {{
        attempts++;
        var pwdInput = document.querySelector('input[type=""password""]');
        var allInputs = Array.from(document.querySelectorAll('input:not([type=""hidden""]):not([type=""password""]):not([type=""checkbox""]):not([type=""radio""])'));
        var userInput = allInputs.find(function(i) {{
            var p = (i.placeholder || '' + i.name || '' + i.id || '').toLowerCase();
            return p.indexOf('email') >= 0 || p.indexOf('điện thoại') >= 0 || p.indexOf('phone') >= 0 || p.indexOf('tài khoản') >= 0 || p.indexOf('user') >= 0;
        }}) || allInputs[0];

        if (userInput && pwdInput) {{
            clearInterval(timer);
            setReactInputValue(userInput, {userJson});
            setReactInputValue(pwdInput, {passJson});

            setTimeout(function() {{
                var submitBtn = document.querySelector('button[type=""submit""]') ||
                    Array.from(document.querySelectorAll('button')).find(function(b) {{
                        var t = (b.textContent || '').trim().toLowerCase();
                        return t === 'đăng nhập' || t === 'login' || t === 'sign in';
                    }});
                if (submitBtn) {{
                    submitBtn.click();
                }}
            }}, 250);

            // Theo dõi khi đăng nhập thành công (URL đổi khỏi /auth/login) để nhảy thẳng về Kho dữ liệu (/explore)
            var redirectWatcher = setInterval(function() {{
                if (window.location.pathname.indexOf('/auth/login') < 0) {{
                    clearInterval(redirectWatcher);
                    if (window.location.pathname.indexOf('/explore') < 0) {{
                        window.location.href = {targetExploreJson};
                    }}
                }}
            }}, 300);
        }} else if (attempts > 25) {{
            clearInterval(timer);
        }}
    }}, 200);
}})();";
                await webView.CoreWebView2.ExecuteScriptAsync(autoFillScript);
                return;
            }

            // Trường hợp 2: Đang ở trang /explore nhưng chưa đăng nhập lần nào trong phiên -> Kiểm tra xem web đã có nút/phiên đăng nhập chưa
            if (!_hasAttemptedAutoLoginInSession && currentUrl.IndexOf("yoobook.vn", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string checkAuthScript = @"
(function() {
    try {
        // Kiểm tra xem trong header (dù đang bị CSS ẩn) có nút 'Đăng nhập' hay không
        var headerBtns = Array.from(document.querySelectorAll('header button, header a'));
        var hasLoginBtn = headerBtns.some(function(b) {
            var t = (b.textContent || '').trim().toLowerCase();
            return t === 'đăng nhập' || t === 'login';
        });
        if (hasLoginBtn) {
            window.chrome.webview.postMessage('NEED_AUTO_LOGIN');
        } else {
            window.chrome.webview.postMessage('ALREADY_LOGGED_IN');
        }
    } catch (e) {}
})();";
                await webView.CoreWebView2.ExecuteScriptAsync(checkAuthScript);
            }
        }

        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string msg = e.TryGetWebMessageAsString();
                if (msg == "NEED_AUTO_LOGIN" && !_hasAttemptedAutoLoginInSession)
                {
                    _hasAttemptedAutoLoginInSession = true;
                    if (YooBookPartnerApiService.Instance.HasDemoCredentials && webView?.CoreWebView2 != null)
                    {
                        txtLoadingTitle.Text = "Đang tự động đăng nhập tài khoản Demo YooBook...";
                        loadingPanel.Visibility = Visibility.Visible;
                        webView.CoreWebView2.Navigate(YooBookPartnerApiService.LoginUrl);
                    }
                }
                else if (msg == "ALREADY_LOGGED_IN")
                {
                    _hasAttemptedAutoLoginInSession = true;
                    txtStatusBadge.Text = "Đã đăng nhập tài khoản YooBook";
                }
            }
            catch { }
        }

        // =========================================================================
        // XỬ LÝ CHUYỂN NHANH CHUYÊN MỤC KHO DỮ LIỆU
        // =========================================================================

        private void NavigateToCategory(string categoryUrl, Button activeButton)
        {
            _targetExploreUrl = categoryUrl;
            HighlightCategoryButton(activeButton);

            if (_isWebViewInitialized && webView?.CoreWebView2 != null)
            {
                webView.CoreWebView2.Navigate(categoryUrl);
            }
        }

        private void HighlightCategoryButton(Button activeBtn)
        {
            Button[] allButtons =
            {
                btnCatAll, btnCatVirtualLab, btnCat3DScene,
                btnCatNaturalSci, btnCatSocialSci, btnCatSandbox, btnCatCulture
            };

            var activeBg = new SolidColorBrush(Color.FromRgb(15, 118, 110));      // #0F766E
            var activeFg = Brushes.White;
            var activeBorder = new SolidColorBrush(Color.FromRgb(13, 148, 136));  // #0D9488

            var normalBg = new SolidColorBrush(Color.FromRgb(241, 245, 249));     // #F1F5F9
            var normalFg = new SolidColorBrush(Color.FromRgb(51, 65, 85));        // #334155
            var normalBorder = new SolidColorBrush(Color.FromRgb(203, 213, 225)); // #CBD5E1

            foreach (var btn in allButtons)
            {
                if (btn == null) continue;
                if (ReferenceEquals(btn, activeBtn))
                {
                    btn.Background = activeBg;
                    btn.Foreground = activeFg;
                    btn.BorderBrush = activeBorder;
                }
                else
                {
                    btn.Background = normalBg;
                    btn.Foreground = normalFg;
                    btn.BorderBrush = normalBorder;
                }
            }
        }

        private void btnCatAll_Click(object sender, RoutedEventArgs e) =>
            NavigateToCategory(YooBookPartnerApiService.CategoryAllUrl, btnCatAll);

        private void btnCatVirtualLab_Click(object sender, RoutedEventArgs e) =>
            NavigateToCategory(YooBookPartnerApiService.CategoryVirtualLabUrl, btnCatVirtualLab);

        private void btnCat3DScene_Click(object sender, RoutedEventArgs e) =>
            NavigateToCategory(YooBookPartnerApiService.Category3DSceneUrl, btnCat3DScene);

        private void btnCatNaturalSci_Click(object sender, RoutedEventArgs e) =>
            NavigateToCategory(YooBookPartnerApiService.CategoryNaturalScienceUrl, btnCatNaturalSci);

        private void btnCatSocialSci_Click(object sender, RoutedEventArgs e) =>
            NavigateToCategory(YooBookPartnerApiService.CategorySocialScienceUrl, btnCatSocialSci);

        private void btnCatSandbox_Click(object sender, RoutedEventArgs e) =>
            NavigateToCategory(YooBookPartnerApiService.CategorySandboxStemUrl, btnCatSandbox);

        private void btnCatCulture_Click(object sender, RoutedEventArgs e) =>
            NavigateToCategory(YooBookPartnerApiService.CategoryCultureHistoryUrl, btnCatCulture);

        // =========================================================================
        // HỘP THOẠI CẤU HÌNH TÀI KHOẢN DEMO & PARTNER API KEY
        // =========================================================================

        private void btnConfigAccount_Click(object sender, RoutedEventArgs e)
        {
            var svc = YooBookPartnerApiService.Instance;
            txtDemoUsername.Text = svc.GetEffectiveDemoUsername();
            pwdDemoPassword.Password = svc.GetEffectiveDemoPassword();
            txtPartnerApiKey.Text = svc.GetEffectiveApiKey();
            txtApiTestResult.Visibility = Visibility.Collapsed;
            configOverlay.Visibility = Visibility.Visible;
        }

        private void btnCancelConfig_Click(object sender, RoutedEventArgs e)
        {
            configOverlay.Visibility = Visibility.Collapsed;
        }

        private async void btnTestApiKey_Click(object sender, RoutedEventArgs e)
        {
            string key = txtPartnerApiKey.Text.Trim();
            txtApiTestResult.Visibility = Visibility.Visible;
            txtApiTestResult.Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105));
            txtApiTestResult.Text = "Đang kiểm tra kết nối tới cổng https://api.eduverse.vn/api/v1/partner/health ...";

            var (isHealthy, message) = await YooBookPartnerApiService.Instance.CheckHealthAsync(key);
            if (isHealthy)
            {
                txtApiTestResult.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                txtApiTestResult.Text = $"✅ {message}";
            }
            else
            {
                txtApiTestResult.Foreground = new SolidColorBrush(Color.FromRgb(225, 29, 72));
                txtApiTestResult.Text = $"⚠️ {message}";
            }
        }

        private void btnSaveAndLogin_Click(object sender, RoutedEventArgs e)
        {
            string user = txtDemoUsername.Text.Trim();
            string pass = pwdDemoPassword.Password;
            string apiKey = txtPartnerApiKey.Text.Trim();

            YooBookPartnerApiService.Instance.SaveCredentials(user, pass, apiKey, strictExploreOnly: true);
            UpdateStatusBadge();
            configOverlay.Visibility = Visibility.Collapsed;

            if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrEmpty(pass) && _isWebViewInitialized && webView?.CoreWebView2 != null)
            {
                _hasAttemptedAutoLoginInSession = true;
                txtLoadingTitle.Text = "Đang tự động đăng nhập tài khoản Demo YooBook...";
                loadingPanel.Visibility = Visibility.Visible;
                webView.CoreWebView2.Navigate(YooBookPartnerApiService.LoginUrl);
            }
        }

        // =========================================================================
        // ĐIỀU HƯỚNG & CHỤP / NHÚNG MÔ HÌNH 3D VÀO BẢNG VIẾT
        // =========================================================================

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_isWebViewInitialized && webView.CanGoBack)
            {
                webView.GoBack();
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (_isWebViewInitialized)
            {
                webView.Reload();
            }
        }

        private void btnHomeExplore_Click(object sender, RoutedEventArgs e)
        {
            NavigateToCategory(YooBookPartnerApiService.CategoryAllUrl, btnCatAll);
        }

        /// <summary>
        /// Nhúng trực tiếp khung tương tác 3D / thí nghiệm hiện tại lên mặt bảng viết QASmartTouch.
        /// </summary>
        private void btnEmbed_Click(object sender, RoutedEventArgs e)
        {
            if (!_isWebViewInitialized || webView?.Source == null)
                return;

            try
            {
                string currentUrl = webView.Source.ToString();
                if (!string.IsNullOrWhiteSpace(currentUrl) && _mainDashboard != null)
                {
                    string? docTitle = webView.CoreWebView2?.DocumentTitle;
                    _mainDashboard.InsertYooBookSimulation(currentUrl, docTitle, _targetExploreUrl);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Form2_26_YooBookBrowser] Embed error: {ex.Message}");
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (_isWebViewInitialized && webView?.CoreWebView2 != null)
                {
                    webView.CoreWebView2.NavigationStarting -= CoreWebView2_NavigationStarting;
                    webView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
                    webView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
                    webView.CoreWebView2.NewWindowRequested -= CoreWebView2_NewWindowRequested;
                }
            }
            catch { }

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
                System.Diagnostics.Debug.WriteLine($"[Form2_26_YooBookBrowser] Cleanup error: {ex.Message}");
            }

            base.OnClosed(e);
        }
    }
}

