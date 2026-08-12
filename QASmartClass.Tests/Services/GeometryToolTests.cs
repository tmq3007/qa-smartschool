using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using Xunit;
using QASmartClass.LearningTools.Views.Math;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace QASmartClass.Tests.Services
{
    public class GeometryToolTests
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
                    var app = Application.Current;
                    if (app == null)
                    {
                        try
                        {
                            app = new Application();
                        }
                        catch (InvalidOperationException)
                        {
                            app = Application.Current;
                        }
                    }
                    if (app != null)
                    {
                        app.Resources["Gray100"] = new SolidColorBrush(Color.FromRgb(248, 249, 250));
                        app.Resources["CatMathBgColor"] = Color.FromRgb(227, 242, 253);
                        app.Resources["CatMath"] = new SolidColorBrush(Color.FromRgb(21, 101, 192));
                        app.Resources["CatMathBg"] = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                        app.Resources["BrandPrimary"] = new SolidColorBrush(Color.FromRgb(21, 101, 192));
                        app.Resources["BrandPrimaryColor"] = Color.FromRgb(21, 101, 192);
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

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) throw new Exception($"Field {fieldName} not found");
            field.SetValue(obj, value);
        }

        private object GetPrivateField(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) throw new Exception($"Field {fieldName} not found");
            return field.GetValue(obj)!;
        }

        private void InvokePrivateMethod(object obj, string methodName, params object[] args)
        {
            var method = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null) throw new Exception($"Method {methodName} not found");
            method.Invoke(obj, args);
        }

        [Fact]
        public void TestTriangle_ValidAndInvalidInputs()
        {
            RunInSta(() =>
            {
                var tool = new GeometryTool();
                var inputBoxes = (Dictionary<string, TextBox>)GetPrivateField(tool, "_inputBoxes");
                var outputPanel = (StackPanel)tool.FindName("outputPanel");

                // Test TRI_01: a=6, h=4
                SetPrivateField(tool, "_selectedShape", "triangle");
                inputBoxes.Clear();
                inputBoxes["a"] = new TextBox { Text = "6" };
                inputBoxes["h"] = new TextBox { Text = "4" };
                InvokePrivateMethod(tool, "Calculate");

                string outputText = string.Join("\n", outputPanel.Children.Cast<FrameworkElement>()
                    .Select(c => c is TextBlock tb ? tb.Text : (c is Border b && b.Child is TextBlock tbb ? tbb.Text : "")));
                outputText = outputText.Replace(',', '.');

                Assert.Contains("Diện tích: 12 đơn vị²", outputText);

                // Test TRI_03: a=6, h=4, b=3 (b < h error)
                inputBoxes["b"] = new TextBox { Text = "3" };
                InvokePrivateMethod(tool, "Calculate");
                
                outputText = string.Join("\n", outputPanel.Children.Cast<FrameworkElement>()
                    .Select(c => c is TextBlock tb ? tb.Text : (c is Border b && b.Child is TextBlock tbb ? tbb.Text : "")));
                outputText = outputText.Replace(',', '.');
                
                Assert.Contains("Cạnh bên b không thể nhỏ hơn chiều cao h.", outputText);

                // Test TRI_05: a=3, b=4, c=5, h=2 (inconsistent height error)
                inputBoxes.Clear();
                inputBoxes["a"] = new TextBox { Text = "3" };
                inputBoxes["b"] = new TextBox { Text = "4" };
                inputBoxes["c"] = new TextBox { Text = "5" };
                inputBoxes["h"] = new TextBox { Text = "2" };
                InvokePrivateMethod(tool, "Calculate");

                outputText = string.Join("\n", outputPanel.Children.Cast<FrameworkElement>()
                    .Select(c => c is TextBlock tb ? tb.Text : (c is Border b && b.Child is TextBlock tbb ? tbb.Text : "")));
                outputText = outputText.Replace(',', '.');

                Assert.Contains("không nhất quán với 3 cạnh", outputText);
            });
        }

        [Fact]
        public void TestTrapezoid_ValidAndInvalidInputs()
        {
            RunInSta(() =>
            {
                var tool = new GeometryTool();
                var inputBoxes = (Dictionary<string, TextBox>)GetPrivateField(tool, "_inputBoxes");
                var outputPanel = (StackPanel)tool.FindName("outputPanel");

                // Test TRAP_01: a=10, b=4, h=5
                SetPrivateField(tool, "_selectedShape", "trapezoid");
                inputBoxes.Clear();
                inputBoxes["a"] = new TextBox { Text = "10" };
                inputBoxes["b"] = new TextBox { Text = "4" };
                inputBoxes["h"] = new TextBox { Text = "5" };
                InvokePrivateMethod(tool, "Calculate");

                 string outputText = string.Join("\n", outputPanel.Children.Cast<FrameworkElement>()
                    .Select(c => c is TextBlock tb ? tb.Text : (c is Border b && b.Child is TextBlock tbb ? tbb.Text : "")));
                outputText = outputText.Replace(',', '.');

                Assert.Contains("Diện tích: 35 đơn vị²", outputText);

                // Test TRAP_03: a=10, b=2, h=4, c=5, d=5 (sides too short)
                inputBoxes["c"] = new TextBox { Text = "5" };
                inputBoxes["d"] = new TextBox { Text = "5" };
                InvokePrivateMethod(tool, "Calculate");

                outputText = string.Join("\n", outputPanel.Children.Cast<FrameworkElement>()
                    .Select(c => c is TextBlock tb ? tb.Text : (c is Border b && b.Child is TextBlock tbb ? tbb.Text : "")));
                outputText = outputText.Replace(',', '.');

                Assert.Contains("quá ngắn để tạo thành hình thang", outputText);
            });
        }

        [Fact]
        public void TestTriangularPyramid_Calculations()
        {
            RunInSta(() =>
            {
                var tool = new GeometryTool();
                var inputBoxes = (Dictionary<string, TextBox>)GetPrivateField(tool, "_inputBoxes");
                var outputPanel = (StackPanel)tool.FindName("outputPanel");

                // Test TRIP_01: a=6, h=4
                SetPrivateField(tool, "_selectedShape", "tri_pyramid");
                inputBoxes.Clear();
                inputBoxes["a"] = new TextBox { Text = "6" };
                inputBoxes["h"] = new TextBox { Text = "4" };
                InvokePrivateMethod(tool, "Calculate");

                string outputText = string.Join("\n", outputPanel.Children.Cast<FrameworkElement>()
                    .Select(c => c is TextBlock tb ? tb.Text : (c is Border b && b.Child is TextBlock tbb ? tbb.Text : "")));
                outputText = outputText.Replace(',', '.');

                Assert.Contains("Diện tích đáy: 15.5885 đơn vị²", outputText);
                Assert.Contains("Thể tích: 20.7846 đơn vị³", outputText);
                Assert.Contains("Trung đoạn d: 4.3589 đơn vị", outputText);
                Assert.Contains("Diện tích xung quanh: 39.2301 đơn vị²", outputText);
                Assert.Contains("Diện tích toàn phần: 54.8185 đơn vị²", outputText);
            });
        }
    }
}
