using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using QASmartClass.LearningTools.Views.Science;
using Xunit;

namespace QASmartClass.Tests
{
    public class BoilingFreezingToolTests
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

        private void EnsureApplicationResources()
        {
            var app = System.Windows.Application.Current;
            if (app == null)
            {
                try
                {
                    app = new System.Windows.Application();
                }
                catch { }
            }

            if (app != null)
            {
                try
                {
                    bool hasTokens = false;
                    bool hasStyles = false;
                    foreach (var dict in app.Resources.MergedDictionaries)
                    {
                        if (dict.Source != null)
                        {
                            if (dict.Source.OriginalString.Contains("DesignTokens.xaml")) hasTokens = true;
                            if (dict.Source.OriginalString.Contains("Styles.xaml")) hasStyles = true;
                        }
                    }

                    if (!hasTokens)
                    {
                        app.Resources.MergedDictionaries.Add(new ResourceDictionary
                        {
                            Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute)
                        });
                    }
                    if (!hasStyles)
                    {
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
        public void TestBoilingFreezingTool_Initialization_And_Load()
        {
            RunOnStaThread(() =>
            {
                EnsureApplicationResources();

                var tool = new BoilingFreezingTool();
                Assert.NotNull(tool);

                // Raise Loaded event
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            });
        }

        [Fact]
        public void TestBoilingFreezingTool_ConversionLogic_And_AbsoluteZeroWarning()
        {
            RunOnStaThread(() =>
            {
                var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
                var originalUiCulture = System.Globalization.CultureInfo.CurrentUICulture;
                try
                {
                    System.Globalization.CultureInfo viCulture = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");
                    System.Threading.Thread.CurrentThread.CurrentCulture = viCulture;
                    System.Threading.Thread.CurrentThread.CurrentUICulture = viCulture;

                    EnsureApplicationResources();

                    var tool = new BoilingFreezingTool();
                    tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                    var txtC = (System.Windows.Controls.TextBox)tool.FindName("txtC");
                    var txtF = (System.Windows.Controls.TextBox)tool.FindName("txtF");
                    var txtK = (System.Windows.Controls.TextBox)tool.FindName("txtK");
                    var brdWarning = (System.Windows.Controls.Border)tool.FindName("brdWarning");

                    Assert.NotNull(txtC);
                    Assert.NotNull(txtF);
                    Assert.NotNull(txtK);
                    Assert.NotNull(brdWarning);

                    // Scenario 1: Celsius set to 100
                    txtC.Text = "100";
                    
                    double cVal = double.Parse(txtC.Text);
                    double fVal = double.Parse(txtF.Text);
                    double kVal = double.Parse(txtK.Text);

                    Assert.Equal(100.0, cVal, 2);
                    Assert.Equal(212.0, fVal, 2);
                    Assert.Equal(373.15, kVal, 2);
                    Assert.Equal(Visibility.Collapsed, brdWarning.Visibility);

                    // Scenario 2: Celsius set to -300 (Below Absolute Zero)
                    txtC.Text = "-300";
                    
                    cVal = double.Parse(txtC.Text);
                    fVal = double.Parse(txtF.Text);
                    kVal = double.Parse(txtK.Text);

                    Assert.Equal(-273.15, cVal, 2);
                    Assert.Equal(-459.67, fVal, 2);
                    Assert.Equal(0.00, kVal, 2);
                    Assert.Equal(Visibility.Visible, brdWarning.Visibility);
                }
                finally
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture = originalCulture;
                    System.Threading.Thread.CurrentThread.CurrentUICulture = originalUiCulture;
                }
            });
        }
    }
}
