using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;
using QASmartClass.LearningTools.Views.Science;

namespace QASmartClass.Tests.Services
{
    public class ElectronConfigToolTests
    {
        private void RunInSta(Action action)
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
            Exception? exception = null;
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    if (Application.Current == null)
                    {
                        _ = new Application();
                    }
                    var resources = Application.Current!.Resources;
                    if (!resources.Contains("Gray100"))
                    {
                        resources["Gray100"] = new SolidColorBrush(Color.FromRgb(248, 249, 250));
                    }
                    InitializeApplicationFull(); action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (exception != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
            }
        }

        [Fact]
        public void TestElectronConfigTool_NormalCalculation()
        {
            RunInSta(() =>
            {
                var tool = new ElectronConfigTool();

                var txtZ = tool.FindName("txtZ") as TextBox;
                var resultPanel = tool.FindName("resultPanel") as StackPanel;

                Assert.NotNull(txtZ);
                Assert.NotNull(resultPanel);

                // Set Fe (Z=26)
                txtZ.Text = "26";

                // Manually trigger Calc using reflection since control is not visually loaded in headless tests
                var calcMethod = typeof(ElectronConfigTool).GetMethod("Calc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(calcMethod);
                calcMethod.Invoke(tool, null);

                // Check that correct configuration is computed and displayed
                bool foundConfig = false;
                foreach (var child in resultPanel.Children)
                {
                    if (child is Border border)
                    {
                        string text = GetBorderText(border);
                        if (text.Contains("Cấu hình e nguyên tử:") && text.Contains("3d⁶ 4s²"))
                        {
                            foundConfig = true;
                            break;
                        }
                    }
                }
                Assert.True(foundConfig, "Fe Z=26 electron configuration should contain '3d⁶ 4s²'.");
            });
        }

        [Fact]
        public void TestElectronConfigTool_AnomalyChromium()
        {
            RunInSta(() =>
            {
                var tool = new ElectronConfigTool();

                var txtZ = tool.FindName("txtZ") as TextBox;
                var resultPanel = tool.FindName("resultPanel") as StackPanel;

                Assert.NotNull(txtZ);
                Assert.NotNull(resultPanel);

                // Set Cr (Z=24) - Exception / Anomaly
                txtZ.Text = "24";

                // Manually trigger Calc
                var calcMethod = typeof(ElectronConfigTool).GetMethod("Calc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(calcMethod);
                calcMethod.Invoke(tool, null);

                bool foundConfig = false;
                foreach (var child in resultPanel.Children)
                {
                    if (child is Border border)
                    {
                        string text = GetBorderText(border);
                        if (text.Contains("Cấu hình e nguyên tử:") && text.Contains("3d⁵ 4s¹"))
                        {
                            foundConfig = true;
                            break;
                        }
                    }
                }
                Assert.True(foundConfig, "Cr Z=24 electron configuration should be the correct anomaly configuration: '3d⁵ 4s¹'.");
            });
        }

        [Fact]
        public void TestElectronConfigTool_AnomalyCopper()
        {
            RunInSta(() =>
            {
                var tool = new ElectronConfigTool();

                var txtZ = tool.FindName("txtZ") as TextBox;
                var resultPanel = tool.FindName("resultPanel") as StackPanel;

                Assert.NotNull(txtZ);
                Assert.NotNull(resultPanel);

                // Set Cu (Z=29) - Exception / Anomaly
                txtZ.Text = "29";

                // Manually trigger Calc
                var calcMethod = typeof(ElectronConfigTool).GetMethod("Calc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(calcMethod);
                calcMethod.Invoke(tool, null);

                bool foundConfig = false;
                foreach (var child in resultPanel.Children)
                {
                    if (child is Border border)
                    {
                        string text = GetBorderText(border);
                        if (text.Contains("Cấu hình e nguyên tử:") && text.Contains("3d¹⁰ 4s¹"))
                        {
                            foundConfig = true;
                            break;
                        }
                    }
                }
                Assert.True(foundConfig, "Cu Z=29 electron configuration should be the correct anomaly configuration: '3d¹⁰ 4s¹'.");
            });
        }


        private static string GetBorderText(Border border)
        {
            if (border.Child is Grid grid && grid.Children.Count > 0)
            {
                var contentElement = grid.Children[0];
                if (contentElement is TextBlock tb) return tb.Text;
                if (contentElement is WrapPanel wp)
                {
                    var parts = new System.Collections.Generic.List<string>();
                    foreach (var child in wp.Children)
                    {
                        if (child is TextBlock childTb) parts.Add(childTb.Text);
                        else if (child.GetType().Name == "FormulaControl")
                        {
                            var formulaProp = child.GetType().GetProperty("Formula");
                            if (formulaProp != null) parts.Add($"${formulaProp.GetValue(child)}$");
                        }
                    }
                    return string.Join("", parts);
                }
                if (contentElement.GetType().Name == "FormulaControl")
                {
                    var formulaProp = contentElement.GetType().GetProperty("Formula");
                    if (formulaProp != null) return $"${formulaProp.GetValue(contentElement)}$";
                }
            }
            else if (border.Child is TextBlock textBlock)
            {
                return textBlock.Text;
            }
            return string.Empty;
        }
    }
}
