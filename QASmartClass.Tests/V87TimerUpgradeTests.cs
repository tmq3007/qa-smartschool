using Xunit;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using QASmartClass.Classroom.ViewModels;

namespace QASmartClass.Tests
{
    public class V87TimerUpgradeTests
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
        public void TimerViewModel_Properties_ShouldInitializeCorrectly()
        {
            var vm = new TimerViewModel();
            Assert.False(vm.IsSessionActive);
            Assert.False(vm.IsTimeUp);
            
            vm.IsSessionActive = true;
            Assert.True(vm.IsSessionActive);
            
            vm.IsTimeUp = true;
            Assert.True(vm.IsTimeUp);
        }

        [Fact]
        public void TextBoxLostFocus_ShouldPadSingleDigitsToDoubleDigits()
        {
            RunOnStaThread(() =>
            {
                var form = new QASmartTouch.Forms.Form2_19_CountdownTimer();
                var method = typeof(QASmartTouch.Forms.Form2_19_CountdownTimer).GetMethod("TextBox_LostFocus",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                var txtBox = new TextBox();

                // Case 1: Empty input -> "00"
                txtBox.Text = "";
                method.Invoke(form, new object[] { txtBox, new RoutedEventArgs() });
                Assert.Equal("00", txtBox.Text);

                // Case 2: Space input -> "00"
                txtBox.Text = "   ";
                method.Invoke(form, new object[] { txtBox, new RoutedEventArgs() });
                Assert.Equal("00", txtBox.Text);

                // Case 3: Single digit input -> pad with zero
                txtBox.Text = "7";
                method.Invoke(form, new object[] { txtBox, new RoutedEventArgs() });
                Assert.Equal("07", txtBox.Text);

                // Case 4: Double digits input -> unchanged
                txtBox.Text = "15";
                method.Invoke(form, new object[] { txtBox, new RoutedEventArgs() });
                Assert.Equal("15", txtBox.Text);
            });
        }

        [Fact]
        public void TextBoxLostFocus_ShouldNormalizeInputTime()
        {
            RunOnStaThread(() =>
            {
                var form = new QASmartTouch.Forms.Form2_19_CountdownTimer();
                var method = typeof(QASmartTouch.Forms.Form2_19_CountdownTimer).GetMethod("TextBox_LostFocus",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                var txtHours = (TextBox)typeof(QASmartTouch.Forms.Form2_19_CountdownTimer)
                    .GetField("txtHours", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(form);
                var txtMinutes = (TextBox)typeof(QASmartTouch.Forms.Form2_19_CountdownTimer)
                    .GetField("txtMinutes", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(form);
                var txtSeconds = (TextBox)typeof(QASmartTouch.Forms.Form2_19_CountdownTimer)
                    .GetField("txtSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(form);

                Assert.NotNull(txtHours);
                Assert.NotNull(txtMinutes);
                Assert.NotNull(txtSeconds);

                // Case 1: Seconds >= 60 (90 seconds -> 1 minute 30 seconds)
                txtHours.Text = "00";
                txtMinutes.Text = "00";
                txtSeconds.Text = "90";
                method.Invoke(form, new object[] { txtSeconds, new RoutedEventArgs() });
                
                Assert.Equal("00", txtHours.Text);
                Assert.Equal("01", txtMinutes.Text);
                Assert.Equal("30", txtSeconds.Text);

                // Case 2: Minutes >= 60 (125 minutes -> 2 hours 5 minutes)
                txtHours.Text = "00";
                txtMinutes.Text = "125";
                txtSeconds.Text = "00";
                method.Invoke(form, new object[] { txtMinutes, new RoutedEventArgs() });
                
                Assert.Equal("02", txtHours.Text);
                Assert.Equal("05", txtMinutes.Text);
                Assert.Equal("00", txtSeconds.Text);

                // Case 3: Cascade (119 minutes 120 seconds -> 2 hours 1 minute 0 seconds)
                txtHours.Text = "00";
                txtMinutes.Text = "119";
                txtSeconds.Text = "120";
                method.Invoke(form, new object[] { txtSeconds, new RoutedEventArgs() });

                Assert.Equal("02", txtHours.Text);
                Assert.Equal("01", txtMinutes.Text);
                Assert.Equal("00", txtSeconds.Text);
            });
        }
    }
}
