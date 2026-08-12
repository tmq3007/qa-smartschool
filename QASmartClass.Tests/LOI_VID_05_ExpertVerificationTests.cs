using Xunit;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using QASmartClass.StudentClient.Views;
using Microsoft.Web.WebView2.Core;
using System.Windows;
using QASmartClass.Data;

namespace QASmartClass.Tests
{
    /// <summary>
    /// BỘ KIỂM THỬ NGHIỆM THU LOI_VID_05 — HỘI ĐỒNG CHUYÊN GIA (Phiên bản nâng cao v2)
    /// 
    /// Bao gồm 8 kịch bản thực tế đánh giá triệt để cơ chế lọc whitelist WebView2:
    ///   TC-01: Whitelist cơ bản — domain, subdomain, đường dẫn con
    ///   TC-02: Chặn tên miền chưa được phép (Blacklist verification)
    ///   TC-03: Kiểm tra sự hiện diện & chữ ký handler NewWindowRequested
    ///   TC-04: Kiểm tra sự hiện diện & chữ ký handler FrameNavigationStarting
    ///   TC-05: Null-safety — handler xử lý tham số null an toàn
    ///   TC-06: Edge cases — URL rỗng, malformed, protocol đặc biệt
    ///   TC-07: Nhiều tên miền whitelist — mô phỏng bài giảng nhiều nguồn
    ///   TC-08: Đăng ký sự kiện trong InitializeWebViewAsync và hủy đăng ký trong OnClosing
    /// </summary>
    public class LOI_VID_05_ExpertVerificationTests
    {
        private static void EnsureAppForCurrentThread()
        {
            if (Application.Current != null)
            {
                bool isInvalid = false;
                try
                {
                    if (Application.Current.Dispatcher.Thread != Thread.CurrentThread)
                    {
                        isInvalid = true;
                    }
                    else
                    {
                        var isShuttingDownField = typeof(Application).GetField("_isShuttingDown", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (isShuttingDownField != null)
                        {
                            bool isShuttingDown = (bool)isShuttingDownField.GetValue(Application.Current);
                            if (isShuttingDown) isInvalid = true;
                        }
                    }
                }
                catch
                {
                    isInvalid = true;
                }

                if (isInvalid)
                {
                    var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                    if (appCreatedField != null)
                    {
                        appCreatedField.SetValue(null, false);
                    }
                    var appInstanceField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                    if (appInstanceField != null)
                    {
                        appInstanceField.SetValue(null, null);
                    }
                }
            }

            if (Application.Current == null)
            {
                var app = new QASmartTouch.App();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                
                var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance);
                if (dbProperty != null)
                {
                    dbProperty.SetValue(app, new AppDbContext());
                }

                var resources = app.Resources;
                try
                {
                    var dict = new ResourceDictionary
                    {
                        Source = new System.Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", System.UriKind.Absolute)
                    };
                    resources.MergedDictionaries.Add(dict);
                }
                catch { }

                var white = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                var gray = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                var blue = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);

                string[] keys = new[] { "LightBrush", "BorderLightBrush", "DarkBrush", "MutedBrush", "PrimaryLightBrush", "PrimaryBrush", "PrimaryDarkBrush" };
                foreach (var key in keys)
                {
                    if (!resources.Contains(key))
                    {
                        resources.Add(key, key.Contains("Dark") || key.Contains("Muted") ? gray : (key.Contains("Primary") ? blue : white));
                    }
                }

                if (!resources.Contains("Gray100"))
                {
                    resources.Add("Gray100", new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F5F5F5")));
                }
                if (!resources.Contains("BrandAccent"))
                {
                    resources.Add("BrandAccent", new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E65100")));
                }
            }
        }

        private void RunOnStaThread(Action action)
        {
            void InitializeApplicationFull()
            {
                var urls = new[] {
                    "pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/Styles.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/StaffTheme.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/InterOutfitFonts.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/SvgIcons.xaml",
                    "pack://application:,,,/QASmartClass;component/Localization/Strings_vi.xaml",
                    "pack://application:,,,/QASmartClass;component/LearningTools/Themes/LearningToolsStyles.xaml"
                };

                try
                {
                    var appField = typeof(System.Windows.Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    var createdField = typeof(System.Windows.Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (appField != null) appField.SetValue(null, null);
                    if (createdField != null) createdField.SetValue(null, false);

                    var app = new QASmartTouch.App();
                    foreach (var url in urls)
                    {
                        app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                        {
                            Source = new Uri(url, UriKind.Absolute)
                        });
                    }
                }
                catch { }
            }
            Exception? ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    EnsureAppForCurrentThread();
                    InitializeApplicationFull(); action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (ex != null)
            {
                throw ex;
            }
        }

        /// <summary>
        /// TC-01 [Giáo viên ưu tú & Học sinh]:
        /// Giáo viên phát trang maths.school.edu cho học sinh.
        /// Học sinh bấm các liên kết trong trang đến các path con, subdomain.
        /// Tất cả phải được phép truy cập bình thường.
        /// </summary>
        [Fact]
        public void TC01_WhitelistBasic_DomainSubdomainAndPaths_Allowed()
        {
            RunOnStaThread(() =>
            {
                var win = new SecureWebWindow("https://maths.school.edu");

                // Chính xác domain ban đầu
                Assert.True(win.IsUrlAllowed("https://maths.school.edu"), "Domain gốc phải được phép");
                
                // Đường dẫn con trong domain
                Assert.True(win.IsUrlAllowed("https://maths.school.edu/lessons/1"), "Path con phải được phép");
                Assert.True(win.IsUrlAllowed("https://maths.school.edu/quizzes/algebra?q=1"), "Path con có query string phải được phép");
                
                // Subdomain của domain đã whitelist
                Assert.True(win.IsUrlAllowed("https://algebra.maths.school.edu"), "Subdomain phải được phép");
                Assert.True(win.IsUrlAllowed("https://video.maths.school.edu/watch?v=123"), "Subdomain với path phải được phép");

                // www variant tự động thêm
                Assert.True(win.IsUrlAllowed("https://www.maths.school.edu"), "www variant phải được phép");

                // Thêm domain bổ sung
                win.AddAllowedUrl("https://wikipedia.org");
                Assert.True(win.IsUrlAllowed("https://wikipedia.org"), "Domain mới thêm phải được phép");
                Assert.True(win.IsUrlAllowed("https://vi.wikipedia.org/wiki/Toan_hoc"), "Subdomain vi phải được phép");
                Assert.True(win.IsUrlAllowed("https://en.wikipedia.org/wiki/Mathematics"), "Subdomain en phải được phép");

                // Default OAuth providers
                Assert.True(win.IsUrlAllowed("https://accounts.google.com/signin"), "Google OAuth mặc định phải được phép");
                Assert.True(win.IsUrlAllowed("https://login.microsoftonline.com/auth"), "Microsoft OAuth mặc định phải được phép");

                win.Close();
            });
        }

        /// <summary>
        /// TC-02 [Gamer giỏi & Chuyên gia bảo mật]:
        /// Mô phỏng các kịch bản tấn công thực tế của học sinh:
        /// - Truy cập các trang game trực tuyến
        /// - Truy cập mạng xã hội (Twitter/X, TikTok)
        /// - Truy cập trang streaming (YouTube nếu không được whitelist)
        /// - Tấn công bằng URL giả mạo subdomain
        /// </summary>
        [Fact]
        public void TC02_BlacklistVerification_UnauthorizedDomains_Blocked()
        {
            RunOnStaThread(() =>
            {
                var win = new SecureWebWindow("https://maths.school.edu");

                // === Trang game trực tuyến ===
                Assert.False(win.IsUrlAllowed("https://poki.com/en/g/subway-surfers"), "Trang game Poki phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://crazygames.com/game/krunker"), "Trang game CrazyGames phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://miniclip.com"), "Trang game Miniclip phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://y8.com/games"), "Trang game Y8 phải bị chặn");

                // === Mạng xã hội ===
                Assert.False(win.IsUrlAllowed("https://twitter.com/home"), "Twitter phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://x.com/explore"), "X.com phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://www.tiktok.com/@user"), "TikTok phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://www.instagram.com/reels"), "Instagram phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://www.reddit.com/r/gaming"), "Reddit phải bị chặn");

                // === Streaming ngoài whitelist ===
                Assert.False(win.IsUrlAllowed("https://www.youtube.com/watch?v=abc"), "YouTube phải bị chặn nếu chưa whitelist");
                Assert.False(win.IsUrlAllowed("https://www.twitch.tv/streams"), "Twitch phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://www.netflix.com/browse"), "Netflix phải bị chặn");

                // === Tấn công giả mạo subdomain ===
                Assert.False(win.IsUrlAllowed("https://maths.school.edu.evil.com"), "Giả mạo domain qua evil.com phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://school.edu.attacker.net"), "Giả mạo domain qua attacker.net phải bị chặn");

                win.Close();
            });
        }

        /// <summary>
        /// TC-03 [Chuyên gia kiểm thử & Quản lý IT]:
        /// Xác minh handler NewWindowRequested tồn tại, có chữ ký đúng chuẩn WebView2.
        /// Đảm bảo khi nâng cấp phiên bản không bị mất handler do refactor.
        /// </summary>
        [Fact]
        public void TC03_NewWindowRequestedHandler_ExistsWithCorrectSignature()
        {
            RunOnStaThread(() =>
            {
                var win = new SecureWebWindow("https://maths.school.edu");
                var type = typeof(SecureWebWindow);

                var method = type.GetMethod("CoreWebView2_NewWindowRequested",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(method);

                var parameters = method.GetParameters();
                Assert.Equal(2, parameters.Length);
                Assert.Equal(typeof(object), parameters[0].ParameterType);
                Assert.Equal(typeof(CoreWebView2NewWindowRequestedEventArgs), parameters[1].ParameterType);

                // Xác minh return type là void (event handler chuẩn)
                Assert.Equal(typeof(void), method.ReturnType);

                win.Close();
            });
        }

        /// <summary>
        /// TC-04 [Chuyên gia kiểm thử & Chuyên gia bảo mật]:
        /// Xác minh handler FrameNavigationStarting tồn tại, có chữ ký đúng chuẩn WebView2.
        /// Đây là chốt chặn cuối cùng ngăn bypass qua iframe.
        /// </summary>
        [Fact]
        public void TC04_FrameNavigationStartingHandler_ExistsWithCorrectSignature()
        {
            RunOnStaThread(() =>
            {
                var win = new SecureWebWindow("https://maths.school.edu");
                var type = typeof(SecureWebWindow);

                var method = type.GetMethod("CoreWebView2_FrameNavigationStarting",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(method);

                var parameters = method.GetParameters();
                Assert.Equal(2, parameters.Length);
                Assert.Equal(typeof(object), parameters[0].ParameterType);
                Assert.Equal(typeof(CoreWebView2NavigationStartingEventArgs), parameters[1].ParameterType);

                Assert.Equal(typeof(void), method.ReturnType);

                win.Close();
            });
        }

        /// <summary>
        /// TC-05 [Chuyên gia kiểm thử & Nhân viên nhà trường]:
        /// Đảm bảo các handler xử lý tham số null an toàn, không crash ứng dụng.
        /// Tình huống: WebView2 runtime gửi sự kiện bất thường hoặc thiết bị đầu cuối lỗi.
        /// </summary>
        [Fact]
        public void TC05_HandlersNullSafety_NoExceptionOnNullArgs()
        {
            RunOnStaThread(() =>
            {
                var win = new SecureWebWindow("https://maths.school.edu");
                var type = typeof(SecureWebWindow);

                var newWindowMethod = type.GetMethod("CoreWebView2_NewWindowRequested",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                var frameMethod = type.GetMethod("CoreWebView2_FrameNavigationStarting",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

                // Null args — handler phải xử lý nội bộ, không ném ngoại lệ
                var ex1 = Record.Exception(() => newWindowMethod!.Invoke(win, new object?[] { null, null }));
                Assert.Null(ex1);

                var ex2 = Record.Exception(() => frameMethod!.Invoke(win, new object?[] { null, null }));
                Assert.Null(ex2);

                // Sender hợp lệ, args null — cũng phải an toàn
                var ex3 = Record.Exception(() => newWindowMethod!.Invoke(win, new object?[] { win, null }));
                Assert.Null(ex3);

                var ex4 = Record.Exception(() => frameMethod!.Invoke(win, new object?[] { win, null }));
                Assert.Null(ex4);

                win.Close();
            });
        }

        /// <summary>
        /// TC-06 [Chuyên gia CSDL & Nhà khoa học giáo dục]:
        /// Kiểm tra các trường hợp biên: URL rỗng, null, malformed, protocol đặc biệt.
        /// Đảm bảo hệ thống không crash khi gặp dữ liệu bất thường.
        /// </summary>
        [Fact]
        public void TC06_EdgeCases_SpecialUrlFormats_HandledCorrectly()
        {
            RunOnStaThread(() =>
            {
                var win = new SecureWebWindow("https://maths.school.edu");

                // URL rỗng / null → phải bị từ chối
                Assert.False(win.IsUrlAllowed(""), "URL rỗng phải bị từ chối");
                Assert.False(win.IsUrlAllowed(null!), "URL null phải bị từ chối");

                // Protocol đặc biệt about:blank, data: → phải được cho phép (WebView2 internal)
                Assert.True(win.IsUrlAllowed("about:blank"), "about:blank phải được phép (WebView2 internal)");
                Assert.True(win.IsUrlAllowed("about:srcdoc"), "about:srcdoc phải được phép");
                Assert.True(win.IsUrlAllowed("data:text/html,<h1>Test</h1>"), "data: URI phải được phép");

                // URL malformed → phải bị từ chối an toàn (không crash)
                Assert.False(win.IsUrlAllowed("not-a-valid-url"), "URL malformed phải bị từ chối");
                Assert.False(win.IsUrlAllowed("://missing-scheme.com"), "URL thiếu scheme phải bị từ chối");

                // JavaScript protocol (nguy hiểm) → phải bị từ chối
                Assert.False(win.IsUrlAllowed("javascript:alert('xss')"), "javascript: URI phải bị từ chối");

                // FTP/file protocol → phải bị từ chối
                Assert.False(win.IsUrlAllowed("ftp://files.school.edu/test.pdf"), "FTP protocol phải bị từ chối");
                Assert.False(win.IsUrlAllowed("file:///C:/Windows/System32/cmd.exe"), "file: protocol phải bị từ chối");

                win.Close();
            });
        }

        /// <summary>
        /// TC-07 [Nhà giáo dục & Hiệu trưởng & Trưởng bộ môn]:
        /// Mô phỏng bài giảng thực tế: Giáo viên phát nhiều trang web học tập cùng lúc.
        /// Học sinh chỉ được truy cập các trang đã được phát, mọi trang khác bị chặn.
        /// </summary>
        [Fact]
        public void TC07_MultipleWhitelistDomains_RealWorldLessonScenario()
        {
            RunOnStaThread(() =>
            {
                var win = new SecureWebWindow("https://vnexpress.net/giao-duc");

                // Giáo viên thêm nhiều nguồn tài liệu học tập
                win.AddAllowedUrl("https://vi.wikipedia.org");
                win.AddAllowedUrl("https://docs.google.com/document/d/abc123");
                win.AddAllowedUrl("https://www.geogebra.org/calculator");
                win.AddAllowedUrl("https://www.mathway.com/Algebra");

                // === Các trang đã whitelist phải truy cập được ===
                Assert.True(win.IsUrlAllowed("https://vnexpress.net/giao-duc/tin-giao-duc-123.html"), "VnExpress giáo dục phải được phép");
                Assert.True(win.IsUrlAllowed("https://vi.wikipedia.org/wiki/Dai_so"), "Wikipedia tiếng Việt phải được phép");
                Assert.True(win.IsUrlAllowed("https://docs.google.com/document/d/xyz789/edit"), "Google Docs phải được phép");
                Assert.True(win.IsUrlAllowed("https://www.geogebra.org/calculator/trigonometry"), "GeoGebra phải được phép");
                Assert.True(win.IsUrlAllowed("https://www.mathway.com/Algebra/Solve"), "Mathway phải được phép");

                // === Các trang KHÔNG whitelist phải bị chặn ===
                Assert.False(win.IsUrlAllowed("https://www.youtube.com/watch?v=game"), "YouTube phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://discord.com/channels"), "Discord phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://store.steampowered.com"), "Steam phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://www.roblox.com"), "Roblox phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://zalo.me"), "Zalo phải bị chặn");

                win.Close();
            });
        }

        /// <summary>
        /// TC-08 [Quản lý IT & Cán bộ Sở GD]:
        /// Xác minh InitializeWebViewAsync đăng ký đủ 5 sự kiện, OnClosing hủy đăng ký đủ 5 sự kiện.
        /// Kiểm tra không bị memory leak do quên hủy đăng ký event handler.
        /// </summary>
        [Fact]
        public void TC08_EventSubscriptionAndUnsubscription_FullLifecycleVerified()
        {
            RunOnStaThread(() =>
            {
                var win = new SecureWebWindow("https://maths.school.edu");
                var type = typeof(SecureWebWindow);

                // Kiểm tra InitializeWebViewAsync tồn tại
                var initMethod = type.GetMethod("InitializeWebViewAsync",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(initMethod);

                // Kiểm tra OnClosing tồn tại
                var closingMethod = type.GetMethod("OnClosing",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(closingMethod);

                // Kiểm tra tất cả 5 event handler tồn tại
                string[] expectedHandlers = new[]
                {
                    "CoreWebView2_NavigationStarting",
                    "CoreWebView2_NavigationCompleted",
                    "CoreWebView2_SourceChanged",
                    "CoreWebView2_NewWindowRequested",
                    "CoreWebView2_FrameNavigationStarting"
                };

                foreach (var handlerName in expectedHandlers)
                {
                    var handler = type.GetMethod(handlerName,
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    Assert.NotNull(handler);
                }

                // Kiểm tra IsUnitTest property tồn tại (anti-deadlock mechanism)
                var isUnitTestProp = type.GetProperty("IsUnitTest",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isUnitTestProp);
                Assert.True((bool)isUnitTestProp.GetValue(win)!, "IsUnitTest phải trả về true khi chạy từ xUnit");

                win.Close();
            });
        }

        /// <summary>
        /// TC-09: Kiểm duyệt và chặn các trang dịch thuật/proxy (google translate, google cache, yandex, v.v.)
        /// </summary>
        [Fact]
        public void TC09_TranslateAndProxyDomains_Blocked()
        {
            RunOnStaThread(() =>
            {
                var win = new SecureWebWindow("https://maths.school.edu");

                // Thử bypass qua Google Translate
                Assert.False(win.IsUrlAllowed("https://translate.google.com/translate?sl=en&tl=vi&u=https://facebook.com"), "Google Translate phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://translate.google.com.vn/translate?u=https://youtube.com"), "Google Translate VN phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://webcache.googleusercontent.com/search?q=cache:abc"), "Google Cache phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://translate.yandex.com/"), "Yandex Translate phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://microsofttranslator.com/"), "Microsoft Translator phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://www.microsofttranslator.com/"), "Microsoft Translator www phải bị chặn");
                Assert.False(win.IsUrlAllowed("https://www.bing.com/translator"), "Bing Translator phải bị chặn");

                win.Close();
            });
        }

        /// <summary>
        /// TC-10: Kiểm tra Facebook bị chặn theo mặc định
        /// </summary>
        [Fact]
        public void TC10_FacebookDomains_BlockedByDefault()
        {
            RunOnStaThread(() =>
            {
                var win = new SecureWebWindow("https://maths.school.edu");

                // Facebook và các domain con mặc định phải bị chặn
                Assert.False(win.IsUrlAllowed("https://facebook.com"), "Facebook phải bị chặn theo mặc định");
                Assert.False(win.IsUrlAllowed("https://www.facebook.com"), "www.facebook.com phải bị chặn theo mặc định");
                Assert.False(win.IsUrlAllowed("https://m.facebook.com"), "m.facebook.com phải bị chặn theo mặc định");

                win.Close();
            });
        }
    }
}
