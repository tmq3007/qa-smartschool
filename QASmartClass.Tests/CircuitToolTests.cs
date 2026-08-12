using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.LearningTools.Views.Science;
using Xunit;

namespace QASmartClass.Tests
{
    public class CircuitToolTests
    {
        public CircuitToolTests()
        {
            CircuitTool.BypassSessionSaveLoad = true;
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
        public void TestCircuitTool_Initialization_And_Load()
        {
            RunOnStaThread(() =>
            {
                EnsureApplicationResources();

                var tool = new CircuitTool();
                Assert.NotNull(tool);

                // Raise Loaded event
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var sideMenu = (ListBox)tool.FindName("sideMenu");
                Assert.NotNull(sideMenu);
                Assert.Equal(5, sideMenu.Items.Count);
            });
        }

        [Fact]
        public void TestCircuitTool_OhmLaw_Calculations()
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

                    var tool = new CircuitTool();
                    tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                    // Use reflection to load sample circuit
                    var loadSampleMethod = typeof(CircuitTool).GetMethod("LoadSampleCircuit", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(loadSampleMethod);

                    // Load "1" (1 Pin + 1 Bulb)
                    loadSampleMethod.Invoke(tool, new object[] { "1" });

                    // Verify stats panel detail contains localized comma value "2,00 A" or "2,00" or similar
                    var txtStatsDetail = (TextBlock)tool.FindName("txtStatsDetail");
                    Assert.NotNull(txtStatsDetail);
                    Assert.Contains("2,00 A", txtStatsDetail.Text);
                    Assert.Contains("24,00 W", txtStatsDetail.Text);
                    Assert.Contains("12,00 V", txtStatsDetail.Text);
                    Assert.Contains("6,00 Ω", txtStatsDetail.Text);
                }
                finally
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture = originalCulture;
                    System.Threading.Thread.CurrentThread.CurrentUICulture = originalUiCulture;
                }
            });
        }

        [Fact]
        public void TestCircuitTool_ShortCircuit()
        {
            RunOnStaThread(() =>
            {
                EnsureApplicationResources();

                var tool = new CircuitTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var loadSampleMethod = typeof(CircuitTool).GetMethod("LoadSampleCircuit", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(loadSampleMethod);

                // Load "6" (Short circuit)
                loadSampleMethod.Invoke(tool, new object[] { "6" });

                var txtStatsDetail = (TextBlock)tool.FindName("txtStatsDetail");
                Assert.NotNull(txtStatsDetail);
                Assert.Contains("NGUY HIỂM", txtStatsDetail.Text);
            });
        }

        [Fact]
        public void TestCircuitTool_PresetsLoadCorrectly()
        {
            RunOnStaThread(() =>
            {
                EnsureApplicationResources();
                var tool = new CircuitTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var cboPresets = (ComboBox)tool.FindName("cboCircuitPresets");
                Assert.NotNull(cboPresets);

                // Kích hoạt nạp mạch nối tiếp (Mục 2 trong ComboBox)
                cboPresets.SelectedIndex = 2;

                // Sử dụng reflection để đọc trạng thái linh kiện và dây nối
                var compsField = typeof(CircuitTool).GetField("_components", BindingFlags.NonPublic | BindingFlags.Instance);
                var wiresField = typeof(CircuitTool).GetField("_wires", BindingFlags.NonPublic | BindingFlags.Instance);

                var components = (System.Collections.IList)compsField.GetValue(tool);
                var wires = (System.Collections.IList)wiresField.GetValue(tool);

                // Mạch nối tiếp gồm 1 pin, 2 đèn, 1 công tắc -> tổng 4 linh kiện, 4 dây nối
                Assert.Equal(4, components.Count);
                Assert.Equal(4, wires.Count);
            });
        }
    }
}
