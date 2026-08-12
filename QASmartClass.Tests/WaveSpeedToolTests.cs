using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.LearningTools.Views.Science;
using Xunit;

namespace QASmartClass.Tests
{
    public class WaveSpeedToolTests
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
        public void TestWaveSpeedTool_Initialization()
        {
            RunOnStaThread(() =>
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

                var tool = new WaveSpeedTool();
                Assert.NotNull(tool);
            });
        }

        [Fact]
        public void TestWaveSpeedTool_FormatVietnamese()
        {
            RunOnStaThread(() =>
            {
                string res1 = WaveSpeedTool.FormatVietnamese(7.874);
                Assert.Contains(",", res1);
                Assert.Equal("7,874", res1);

                string res2 = WaveSpeedTool.FormatVietnamese(0.005);
                Assert.Equal("0,005", res2);
            });
        }

        [Fact]
        public void TestWaveSpeedTool_FormatToPedagogicalScientific()
        {
            RunOnStaThread(() =>
            {
                // Normal values should use normal vi-VN format
                string resNormal = WaveSpeedTool.FormatToPedagogicalScientific(5960);
                Assert.Equal("5960", resNormal);

                // Very small values should use superscript format: 1.000e-7 -> 1,0000 · 10⁻⁷ or similar
                string resSmall = WaveSpeedTool.FormatToPedagogicalScientific(0.0000001);
                Assert.Contains("\u00B7 10", resSmall);
                Assert.Contains("\u207B", resSmall); // minus superscript
                Assert.Contains("\u2077", resSmall); // 7 superscript
            });
        }

        [Fact]
        public void TestWaveSpeedTool_FormatToPedagogicalScientific_LargeSpeed()
        {
            RunOnStaThread(() =>
            {
                // Speed of light 299792458 should use standard pedagogical scientific notation: 2,9979 · 10⁸
                string resLight = WaveSpeedTool.FormatToPedagogicalScientific(299792458);
                Assert.Contains("\u00B7 10", resLight);
                Assert.Contains("\u2078", resLight); // 8 superscript
                Assert.Contains("2,9979", resLight);
            });
        }
    }
}
