using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Classroom.Views;
using QASmartClass.Services;
using Xunit;

namespace QASmartClass.Tests
{
    public class V20BroadcastTests
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
            Exception? ex = null;
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

        private void EnsureApplication()
        {
            if (System.Windows.Application.Current == null)
            {
                try
                {
                    var app = new QASmartTouch.App();
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/QASmartClass;component/App.xaml", UriKind.Absolute)
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[V20BroadcastTests] App creation error: {ex.Message}");
                }
            }
        }

        [Fact]
        public void Test_BroadcastToken_BasicFlow()
        {
            var service = BroadcastStateService.Instance;
            Assert.NotNull(service);

            // Test setting token
            service.BroadcastToken = "test_token_123";
            Assert.Equal("test_token_123", service.BroadcastToken);

            // Test Reset clears token
            service.Reset();
            Assert.Equal(string.Empty, service.BroadcastToken);
        }

        [Fact]
        public void Test_BroadcastPage_QualityComboBox()
        {
            RunOnStaThread(() =>
            {
                EnsureApplication();
                var page = new BroadcastPage();
                Assert.NotNull(page);

                // Find cmbQuality ComboBox
                var cmbQuality = page.FindName("cmbQuality") as ComboBox;
                Assert.NotNull(cmbQuality);

                // Default SelectedIndex should be 1 (Bình thường - 65%)
                Assert.Equal(1, cmbQuality.SelectedIndex);

                // It should have 3 items: Thấp (40%), Bình thường (65%), Cao (85%)
                Assert.Equal(3, cmbQuality.Items.Count);

                // Verify item tags
                var item0 = cmbQuality.Items[0] as ComboBoxItem;
                var item1 = cmbQuality.Items[1] as ComboBoxItem;
                var item2 = cmbQuality.Items[2] as ComboBoxItem;

                Assert.NotNull(item0);
                Assert.NotNull(item1);
                Assert.NotNull(item2);

                Assert.Equal("40", item0.Tag?.ToString());
                Assert.Equal("65", item1.Tag?.ToString());
                Assert.Equal("85", item2.Tag?.ToString());
            });
        }

        [Fact]
        public void Test_BroadcastPage_ModeSwitchingUI()
        {
            RunOnStaThread(() =>
            {
                EnsureApplication();
                var page = new BroadcastPage();
                Assert.NotNull(page);

                var btnBroadcast = page.FindName("btnBroadcast") as Button;
                var btnTurboBroadcast = page.FindName("btnTurboBroadcast") as Button;
                var btnTextBroadcast = page.FindName("btnTextBroadcast") as Button;
                var btnMulticastBroadcast = page.FindName("btnMulticastBroadcast") as Button;

                Assert.NotNull(btnBroadcast);
                Assert.NotNull(btnTurboBroadcast);
                Assert.NotNull(btnTextBroadcast);
                Assert.NotNull(btnMulticastBroadcast);

                // Trạng thái ban đầu: Tất cả các nút đều hoạt động
                Assert.True(btnBroadcast.IsEnabled);
                Assert.True(btnTurboBroadcast.IsEnabled);
                Assert.True(btnTextBroadcast.IsEnabled);
                Assert.True(btnMulticastBroadcast.IsEnabled);

                // Giả lập click nút "Phát Chuyên Chữ"
                btnTextBroadcast.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                
                // Xác minh trạng thái UI sau khi chuyển sang chế độ Chuyên Chữ: Khóa các nút khác
                Assert.True(btnTextBroadcast.IsEnabled);
                Assert.False(btnBroadcast.IsEnabled);
                Assert.False(btnTurboBroadcast.IsEnabled);
                Assert.False(btnMulticastBroadcast.IsEnabled);

                // Click lại nút "Phát Chuyên Chữ" để dừng phát
                btnTextBroadcast.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                // Xác minh UI phục hồi trạng thái mở khóa ban đầu
                Assert.True(btnBroadcast.IsEnabled);
                Assert.True(btnTurboBroadcast.IsEnabled);
                Assert.True(btnTextBroadcast.IsEnabled);
                Assert.True(btnMulticastBroadcast.IsEnabled);

                // Giả lập click nút "Phát Đa Thiết Bị" (Multicast)
                btnMulticastBroadcast.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                // Xác minh trạng thái UI sau khi chuyển sang chế độ Multicast: Khóa các nút khác
                Assert.True(btnMulticastBroadcast.IsEnabled);
                Assert.False(btnBroadcast.IsEnabled);
                Assert.False(btnTurboBroadcast.IsEnabled);
                Assert.False(btnTextBroadcast.IsEnabled);

                // Click lại nút "Phát Đa Thiết Bị" để dừng phát
                btnMulticastBroadcast.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                // Xác minh UI phục hồi trạng thái mở khóa ban đầu
                Assert.True(btnBroadcast.IsEnabled);
                Assert.True(btnTurboBroadcast.IsEnabled);
                Assert.True(btnTextBroadcast.IsEnabled);
                Assert.True(btnMulticastBroadcast.IsEnabled);
            });
        }
    }
}
