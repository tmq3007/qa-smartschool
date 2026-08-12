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
            Exception ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    action();
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
                try { new System.Windows.Application(); } catch { }
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
    }
}
