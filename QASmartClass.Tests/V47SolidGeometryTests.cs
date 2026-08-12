using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.LearningTools.Views.Math;
using Xunit;

namespace QASmartClass.Tests
{
    public class V47SolidGeometryTests
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

        private void InitializeAppResources()
        {
            if (System.Windows.Application.Current == null)
            {
                try { new System.Windows.Application(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V47SolidGeometryTests] Error: {ex.Message}"); }
            }

            if (System.Windows.Application.Current != null)
            {
                if (!System.Windows.Application.Current.Resources.Contains("Gray100"))
                    System.Windows.Application.Current.Resources.Add("Gray100", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)));
            }
        }

        [Fact]
        public void TestSolidGeometryTool_Initialization_AndCalculation()
        {
            RunOnStaThread(() =>
            {
                InitializeAppResources();

                var tool = new SolidGeometryTool();
                Assert.NotNull(tool);

                // Lấy panel kết quả và các panel điều khiển qua Reflection hoặc FindName
                var resultPanel = tool.FindName("resultPanel") as StackPanel;
                Assert.NotNull(resultPanel);

                // Lấy trường _shape để chuyển đổi qua các hình học khác nhau
                var shapeField = typeof(SolidGeometryTool).GetField("_shape", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(shapeField);

                var inputsField = typeof(SolidGeometryTool).GetField("_inputs", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(inputsField);

                var rebuildInputsMethod = typeof(SolidGeometryTool).GetMethod("RebuildInputs", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(rebuildInputsMethod);

                var calcMethod = typeof(SolidGeometryTool).GetMethod("Calc", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(calcMethod);

                // Kiểm thử cho từng loại hình 0 - 7
                for (int shapeIndex = 0; shapeIndex <= 7; shapeIndex++)
                {
                    shapeField.SetValue(tool, shapeIndex);
                    rebuildInputsMethod.Invoke(tool, null);

                    var inputs = inputsField.GetValue(tool) as System.Collections.Generic.List<TextBox>;
                    Assert.NotNull(inputs);

                    // Đảm bảo số lượng input phù hợp với cấu hình của mỗi hình học
                    Assert.True(inputs.Count > 0);

                    // Đặt giá trị cho các TextBox
                    foreach (var input in inputs)
                    {
                        input.Text = "5"; // gán giá trị hợp lệ để tính toán
                    }

                    // Gọi tính toán
                    calcMethod.Invoke(tool, null);

                    // Xác nhận có dòng kết quả được tạo ra trong resultPanel
                    Assert.True(resultPanel.Children.Count > 0);
                }
            });
        }

        [Fact]
        public void TestSolidGeometry_ExactForms()
        {
            var fmtPiMethod = typeof(SolidGeometryTool).GetMethod("FmtExactPi", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(fmtPiMethod);

            var fmtSqrt3Method = typeof(SolidGeometryTool).GetMethod("FmtExactSqrt3", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(fmtSqrt3Method);

            // Test Sphere r=3 volume coefficient: 36 -> 36π
            var r1 = fmtPiMethod.Invoke(null, new object[] { 36.0 }) as string;
            Assert.Equal("36π", r1);

            // Test Sphere r=5 volume: 500/3 -> (500π/3)
            var r2 = fmtPiMethod.Invoke(null, new object[] { 500.0 / 3.0 }) as string;
            Assert.Equal("(500π/3)", r2);

            // Test Triangle a=6 base coefficient: 9 -> 9√3
            var r3 = fmtSqrt3Method.Invoke(null, new object[] { 9.0 }) as string;
            Assert.Equal("9√3", r3);

            // Test Triangle a=2 base coefficient: 1 -> √3
            var r4 = fmtSqrt3Method.Invoke(null, new object[] { 1.0 }) as string;
            Assert.Equal("√3", r4);

            // Test fraction with denominator 2: 1.5 -> (3π/2)
            var r5 = fmtPiMethod.Invoke(null, new object[] { 1.5 }) as string;
            Assert.Equal("(3π/2)", r5);
        }
    }
}
