using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.LearningTools.Views.Math;
using Xunit;

namespace QASmartClass.Tests
{
    public class TrigonometryToolTests
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

        private void InitializeAppAndResources()
        {
            var app = Application.Current;
            if (app == null)
            {
                try
                {
                    app = new Application();
                }
                catch { }
            }

            if (app != null)
            {
                try
                {
                    bool hasTokens = false;
                    foreach (var dict in app.Resources.MergedDictionaries)
                    {
                        if (dict.Source != null && dict.Source.OriginalString.Contains("DesignTokens.xaml"))
                        {
                            hasTokens = true;
                            break;
                        }
                    }

                    if (!hasTokens)
                    {
                        app.Resources.MergedDictionaries.Add(new ResourceDictionary
                        {
                            Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute)
                        });
                        app.Resources.MergedDictionaries.Add(new ResourceDictionary
                        {
                            Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/Styles.xaml", UriKind.Absolute)
                        });
                    }
                }
                catch { }
            }
        }

        [Fact]
        public void TestFormatTrigValues()
        {
            var method = typeof(TrigonometryTool).GetMethod("FormatTrig", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            Assert.Equal("0", method.Invoke(null, new object[] { 0.0 }));
            Assert.Equal("1", method.Invoke(null, new object[] { 1.0 }));
            Assert.Equal("-1", method.Invoke(null, new object[] { -1.0 }));
            Assert.Equal("1/2", method.Invoke(null, new object[] { 0.5 }));
            Assert.Equal("-1/2", method.Invoke(null, new object[] { -0.5 }));
            Assert.Equal("√2/2", method.Invoke(null, new object[] { Math.Sqrt(2) / 2 }));
            Assert.Equal("√3/2", method.Invoke(null, new object[] { Math.Sqrt(3) / 2 }));
            Assert.Equal("√3", method.Invoke(null, new object[] { Math.Sqrt(3) }));
            Assert.Equal("√3/3", method.Invoke(null, new object[] { Math.Sqrt(3) / 3 }));
        }

        [Fact]
        public void TestFormatRadian()
        {
            var method = typeof(TrigonometryTool).GetMethod("FormatRadian", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            Assert.Equal("0", method.Invoke(null, new object[] { 0.0 }));
            Assert.Equal("π/6", method.Invoke(null, new object[] { 30.0 }));
            Assert.Equal("π/4", method.Invoke(null, new object[] { 45.0 }));
            Assert.Equal("π/3", method.Invoke(null, new object[] { 60.0 }));
            Assert.Equal("π/2", method.Invoke(null, new object[] { 90.0 }));
            Assert.Equal("π", method.Invoke(null, new object[] { 180.0 }));
            Assert.Equal("2π", method.Invoke(null, new object[] { 360.0 }));
        }

        [Fact]
        public void TestInteractiveTrigCircleAngleSync()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new TrigonometryTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                var txtAngle = (TextBox)tool.FindName("txtAngle");
                var trigCircle = (InteractiveTrigCircle)tool.FindName("trigCircle");

                Assert.NotNull(txtAngle);
                Assert.NotNull(trigCircle);

                // Default angle should be 45
                Assert.Equal("45", txtAngle.Text);
                Assert.Equal(45.0, trigCircle.Angle);

                // Change angle in textbox, verify sync to trigCircle
                txtAngle.Text = "60";
                var calculateMethod = typeof(TrigonometryTool).GetMethod("CalculateAngle", BindingFlags.NonPublic | BindingFlags.Instance);
                calculateMethod.Invoke(tool, null);
                Assert.Equal(60.0, trigCircle.Angle);

                // Change angle in trigCircle, verify sync to txtAngle
                trigCircle.Angle = 120.0;
                Assert.Equal("120.0", txtAngle.Text);
            });
        }
    }
}
