using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.LearningTools.Views.Science;
using Xunit;

namespace QASmartClass.Tests
{
    public class UnitConverterToolTests
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
        public void TestUnitConverterTool_Initialization()
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
                    try
                    {
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

                var tool = new UnitConverterTool();
                Assert.NotNull(tool);

                // Raise Loaded event
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            });
        }

        [Fact]
        public void TestUnitConverterTool_AbsoluteZeroValidation()
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
                        try
                        {
                            app.Resources.MergedDictionaries.Add(new ResourceDictionary
                            {
                                Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute)
                            });
                        }
                        catch { }
                    }
                }

                var tool = new UnitConverterTool();
                
                // Set _activeType to 🌡️ Nhiệt độ via reflection to simulate selecting the temperature tab
                var activeTypeField = typeof(UnitConverterTool).GetField("_activeType", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(activeTypeField);
                activeTypeField.SetValue(tool, "🌡️ Nhiệt độ");

                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var txtFromValue = (TextBox)tool.FindName("txtFromValue");
                var txtToValue = (TextBox)tool.FindName("txtToValue");
                var txtConvertFormula = (TextBlock)tool.FindName("txtConvertFormula");
                var cboFromUnit = (ComboBox)tool.FindName("cboFromUnit");
                var cboToUnit = (ComboBox)tool.FindName("cboToUnit");

                Assert.NotNull(txtFromValue);
                Assert.NotNull(txtToValue);
                Assert.NotNull(txtConvertFormula);
                Assert.NotNull(cboFromUnit);
                Assert.NotNull(cboToUnit);

                // Scenario 1: Select Kelvin, value = -10 (Below Absolute Zero)
                for (int i = 0; i < cboFromUnit.Items.Count; i++)
                {
                    if (cboFromUnit.Items[i] is ComboBoxItem item && item.Content?.ToString() == "K")
                    {
                        cboFromUnit.SelectedIndex = i;
                        break;
                    }
                }
                for (int i = 0; i < cboToUnit.Items.Count; i++)
                {
                    if (cboToUnit.Items[i] is ComboBoxItem item && item.Content?.ToString() == "°C")
                    {
                        cboToUnit.SelectedIndex = i;
                        break;
                    }
                }

                txtFromValue.Text = "-10";
                
                Assert.Equal(string.Empty, txtToValue.Text);
                Assert.Contains("không thể thấp hơn độ không tuyệt đối", txtConvertFormula.Text);
                Assert.Equal("#FFD32F2F", txtConvertFormula.Foreground.ToString());

                // Scenario 2: Select Celsius, value = -300 (Below Absolute Zero)
                for (int i = 0; i < cboFromUnit.Items.Count; i++)
                {
                    if (cboFromUnit.Items[i] is ComboBoxItem item && item.Content?.ToString() == "°C")
                    {
                        cboFromUnit.SelectedIndex = i;
                        break;
                    }
                }
                txtFromValue.Text = "-300";

                Assert.Equal(string.Empty, txtToValue.Text);
                Assert.Contains("không thể thấp hơn độ không tuyệt đối", txtConvertFormula.Text);
                Assert.Equal("#FFD32F2F", txtConvertFormula.Foreground.ToString());

                // Scenario 3: Valid positive Celsius value (100)
                txtFromValue.Text = "100";
                Assert.NotEqual(string.Empty, txtToValue.Text);
                Assert.Contains("100 °C =", txtConvertFormula.Text);
                Assert.Equal("#FF616161", txtConvertFormula.Foreground.ToString());
            });
        }

        [Fact]
        public void TestUnitConverterTool_FormatVietnamese()
        {
            string formatted = UnitConverterTool.FormatVietnamese(1234567.89);
            // In Vietnamese culture, G10 formatting will produce "1234567,89" or similar
            Assert.Contains(",", formatted);
            Assert.DoesNotContain(".", formatted);
        }

        [Fact]
        public void TestUnitConverterTool_FormatToPedagogicalScientific()
        {
            // Test very large value
            string formattedLarge = UnitConverterTool.FormatToPedagogicalScientific(5.97e24);
            Assert.Contains("5,97", formattedLarge);
            Assert.Contains("\u00B7", formattedLarge); // middle dot
            Assert.Contains("10", formattedLarge);
            Assert.Contains("\u00B2", formattedLarge); // superscript 2
            Assert.Contains("\u2074", formattedLarge); // superscript 4

            // Test very small value
            string formattedSmall = UnitConverterTool.FormatToPedagogicalScientific(1.602e-19);
            Assert.Contains("1,602", formattedSmall);
            Assert.Contains("\u207B", formattedSmall); // superscript minus
            Assert.Contains("\u00B9", formattedSmall); // superscript 1
            Assert.Contains("\u2079", formattedSmall); // superscript 9
        }

        [Fact]
        public void TestUnitConverterTool_PresetsLoadCorrectly()
        {
            RunOnStaThread(() =>
            {
                var app = System.Windows.Application.Current;
                if (app == null)
                {
                    try { app = new System.Windows.Application(); } catch { }
                }

                if (app != null)
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
                    try
                    {
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

                var tool = new UnitConverterTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                // Nạp mẫu chiều dài địa lý (LengthGeo) bằng reflection gọi hàm click nút preset
                var presetMethod = typeof(UnitConverterTool).GetMethod("LoadConversionPreset", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(presetMethod);

                // Nạp: Chiều dài, 1 km -> m
                presetMethod.Invoke(tool, new object[] { "📏 Chiều dài", "km", "m", "1" });

                var txtToValue = (TextBox)tool.FindName("txtToValue");
                var txtConvertFormula = (TextBlock)tool.FindName("txtConvertFormula");

                Assert.NotNull(txtToValue);
                Assert.Equal("1000", txtToValue.Text);
                Assert.Contains("1 km =", txtConvertFormula.Text);
            });
        }

        [Fact]
        public void TestUnitConverterTool_SwapUnits()
        {
            RunOnStaThread(() =>
            {
                var app = System.Windows.Application.Current;
                if (app == null)
                {
                    try { app = new System.Windows.Application(); } catch { }
                }

                var tool = new UnitConverterTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var txtFromValue = (TextBox)tool.FindName("txtFromValue");
                var txtToValue = (TextBox)tool.FindName("txtToValue");
                var cboFromUnit = (ComboBox)tool.FindName("cboFromUnit");
                var cboToUnit = (ComboBox)tool.FindName("cboToUnit");

                // Set active type to Length
                var activeTypeField = typeof(UnitConverterTool).GetField("_activeType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(activeTypeField);
                activeTypeField.SetValue(tool, "📏 Chiều dài");

                // Set From = km (index 3), To = m (index 0)
                cboFromUnit.SelectedIndex = 3; // km
                cboToUnit.SelectedIndex = 0; // m
                txtFromValue.Text = "2.5";

                var convertMethod = typeof(UnitConverterTool).GetMethod("DoConversion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(convertMethod);
                convertMethod.Invoke(tool, null);

                Assert.Equal("2500", txtToValue.Text);

                // Call Swap
                var swapMethod = typeof(UnitConverterTool).GetMethod("SwapUnits_Click", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(swapMethod);
                swapMethod.Invoke(tool, new object[] { null, null });

                // After swap, From should be "m" (index 0), To should be "km" (index 3)
                Assert.Equal(0, cboFromUnit.SelectedIndex);
                Assert.Equal(3, cboToUnit.SelectedIndex);

                // Input value should be "2500"
                Assert.Equal("2500", txtFromValue.Text);

                // Output value should be "2,5" (Vietnamese decimal comma)
                Assert.Equal("2,5", txtToValue.Text);
            });
        }

        [Fact]
        public void TestUnitConverterTool_PressureNegativeValidation()
        {
            RunOnStaThread(() =>
            {
                var app = System.Windows.Application.Current;
                if (app == null)
                {
                    try { app = new System.Windows.Application(); } catch { }
                }

                var tool = new UnitConverterTool();
                var activeTypeField = typeof(UnitConverterTool).GetField("_activeType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(activeTypeField);
                activeTypeField.SetValue(tool, "🔩 Áp suất");

                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var txtFromValue = (TextBox)tool.FindName("txtFromValue");
                var txtToValue = (TextBox)tool.FindName("txtToValue");
                var txtConvertFormula = (TextBlock)tool.FindName("txtConvertFormula");
                var cboFromUnit = (ComboBox)tool.FindName("cboFromUnit");
                var cboToUnit = (ComboBox)tool.FindName("cboToUnit");

                // Set units
                cboFromUnit.SelectedIndex = 0; // Pa
                cboToUnit.SelectedIndex = 1; // kPa
                txtFromValue.Text = "-100";

                var convertMethod = typeof(UnitConverterTool).GetMethod("DoConversion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(convertMethod);
                convertMethod.Invoke(tool, null);

                Assert.Equal(string.Empty, txtToValue.Text);
                Assert.Contains("không thể nhận giá trị âm", txtConvertFormula.Text);
                Assert.Equal("#FFD32F2F", txtConvertFormula.Foreground.ToString());
            });
        }
    }
}
