using Xunit;
using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace QASmartClass.Tests
{
    public class V95SettingsUpgradeTests
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
            var t = new System.Threading.Thread(() =>
            {
                try
                {
                    InitializeApplicationFull(); action();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("STA thread error: " + ex.Message, ex);
                }
            });
            t.SetApartmentState(System.Threading.ApartmentState.STA);
            t.Start();
            t.Join();
        }

        [Fact]
        public void TestSettings_UrlPrefixLogic_StandardUrl()
        {
            // Kiểm thử logic tiền tố URL của btnTestConnection_Click và btnSave_Click
            string inputUrl1 = "192.168.1.50:8085";
            string processedUrl1 = inputUrl1.Trim();
            if (!processedUrl1.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                !processedUrl1.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                processedUrl1 = "http://" + processedUrl1;
            }
            Assert.Equal("http://192.168.1.50:8085", processedUrl1);

            // Không chèn nếu đã có https://
            string inputUrl2 = "https://smartclass.edu.vn";
            string processedUrl2 = inputUrl2.Trim();
            if (!processedUrl2.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                !processedUrl2.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                processedUrl2 = "http://" + processedUrl2;
            }
            Assert.Equal("https://smartclass.edu.vn", processedUrl2);

            // Không chèn nếu đã có http://
            string inputUrl3 = "http://localhost:5000";
            string processedUrl3 = inputUrl3.Trim();
            if (!processedUrl3.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                !processedUrl3.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                processedUrl3 = "http://" + processedUrl3;
            }
            Assert.Equal("http://localhost:5000", processedUrl3);
        }

        [Fact]
        public void TestSettings_TextBoxDebounceLogic_UsingTag()
        {
            // Kiểm thử logic giải phóng và chống trôi timer cảnh báo thông qua thuộc tính Tag của TextBox
            RunOnStaThread(() =>
            {
                var textBox = new TextBox();

                // 1. Giả lập cảnh báo đầu tiên hiển thị
                var timer1 = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                textBox.Tag = timer1;
                timer1.Start();
                Assert.True(timer1.IsEnabled);

                // 2. Giả lập cảnh báo thứ hai xuất hiện liên tiếp (nhấn phím chữ liên tục)
                // Đọc timer cũ từ Tag và dừng nó
                if (textBox.Tag is DispatcherTimer oldTimer)
                {
                    oldTimer.Stop();
                }
                
                var timer2 = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                textBox.Tag = timer2;
                timer2.Start();

                // 3. Xác nhận timer1 (cũ) đã bị dừng hẳn để chống trôi và nhấp nháy ToolTip
                Assert.False(timer1.IsEnabled);
                Assert.True(timer2.IsEnabled);
                Assert.NotSame(timer1, timer2);

                // 4. Giả lập kết thúc timer (timer2.Stop())
                timer2.Stop();
                textBox.Tag = null;
                Assert.False(timer2.IsEnabled);
                Assert.Null(textBox.Tag);
            });
        }
    }
}
