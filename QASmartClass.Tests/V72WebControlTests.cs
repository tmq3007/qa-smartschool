using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using QASmartClass.StudentClient.Views;
using QASmartClass.Classroom.Services;

namespace QASmartClass.Tests
{
    public class V72WebControlTests
    {
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
            Exception ex = null;
            var t = new Thread(() =>
            {
                try
                {
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

        [Fact]
        public void ConvertTcpToJson_WebControlCommands_Verify()
        {
            var method = typeof(WebSocketBridgeService).GetMethod("ConvertTcpToJson", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.NotNull(method);
            
            // BLOCK_WEB_ON
            var jsonBlockOn = (string)method.Invoke(null, new object[] { "CMD|BLOCK_WEB_ON" });
            Assert.Contains("BLOCK_WEB_ON", jsonBlockOn);
            
            // BLOCK_WEB_OFF
            var jsonBlockOff = (string)method.Invoke(null, new object[] { "CMD|BLOCK_WEB_OFF" });
            Assert.Contains("BLOCK_WEB_OFF", jsonBlockOff);
            
            // WEB_WHITELIST_ON
            var jsonWhitelistOn = (string)method.Invoke(null, new object[] { "CMD|WEB_WHITELIST_ON" });
            Assert.Contains("WEB_WHITELIST_ON", jsonWhitelistOn);
            
            // WEB_WHITELIST_OFF
            var jsonWhitelistOff = (string)method.Invoke(null, new object[] { "CMD|WEB_WHITELIST_OFF" });
            Assert.Contains("WEB_WHITELIST_OFF", jsonWhitelistOff);
            
            // WHITELIST_ADD
            var jsonWhitelistAdd = (string)method.Invoke(null, new object[] { "CMD|WHITELIST_ADD|https://google.com,https://bing.com" });
            Assert.Contains("WHITELIST_ADD", jsonWhitelistAdd);
            Assert.Contains("https://google.com,https://bing.com", jsonWhitelistAdd);
        }

        [Fact]
        public void SecureWebWindow_UrlMatching_Verify()
        {
            RunOnStaThread(() =>
            {
                var win = new SecureWebWindow("https://google.com");
                
                // Add allowed URL
                win.AddAllowedUrl("https://example.com/some/path");
                
                // Detailed assertions with custom exceptions
                if (!win.IsUrlAllowed("https://example.com/another/page"))
                    throw new Exception("Assertion failed: exact domain (example.com) should be allowed");
                
                if (!win.IsUrlAllowed("https://google.com"))
                    throw new Exception("Assertion failed: initial domain (google.com) should be allowed");
                
                if (!win.IsUrlAllowed("https://sub.example.com"))
                    throw new Exception("Assertion failed: subdomain (sub.example.com) should be allowed");
                
                if (win.IsUrlAllowed("https://malicious.com"))
                    throw new Exception("Assertion failed: unauthorized domain (malicious.com) should NOT be allowed");
                
                if (!win.IsUrlAllowed("https://accounts.google.com/signin"))
                    throw new Exception("Assertion failed: OAuth Google domain should be allowed by default");
                
                if (!win.IsUrlAllowed("https://login.microsoftonline.com"))
                    throw new Exception("Assertion failed: OAuth Microsoft domain should be allowed by default");
                
                win.Close();
            });
        }
    }
}
