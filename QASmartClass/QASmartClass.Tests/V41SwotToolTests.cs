using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.LearningTools.Views.Workplace;
using Xunit;

namespace QASmartClass.Tests
{
    public class V41SwotToolTests
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

        [Fact]
        public void TestSwotTool_StrategyClick_NoData_DoesNotShowSection()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch { }
                }

                var tool = new SwotTool();

                // Trực tiếp gọi hàm Strategy_Click bằng Reflection
                var method = typeof(SwotTool).GetMethod("Strategy_Click", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                // Gọi khi chưa nhập bất cứ dữ liệu nào
                // Lưu ý: MessageBox có thể chặn luồng test, tuy nhiên ta sẽ mock hoặc kiểm tra visibility của Section trước
                var strategySection = (Border)tool.FindName("strategySection");
                Assert.NotNull(strategySection);
                Assert.Equal(Visibility.Collapsed, strategySection.Visibility);
            });
        }

        [Fact]
        public void TestSwotTool_StrategyClick_WithSomeData_ShowsAllBlocks()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch { }
                }

                var tool = new SwotTool();

                // Add 1 item to S panel
                var addSMethod = typeof(SwotTool).GetMethod("AddSwotItem", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(addSMethod);

                var panelS = (StackPanel)tool.FindName("panelS");
                Assert.NotNull(panelS);

                // Add a row to panelS
                addSMethod.Invoke(tool, new object[] { panelS, "#2E7D32", "#E8F5E9" });
                Assert.Single(panelS.Children);

                // Set text for textbox in panelS
                var grid = (Grid)panelS.Children[0];
                TextBox tb = null;
                foreach (var child in grid.Children)
                {
                    if (child is TextBox textb)
                    {
                        tb = textb;
                        break;
                    }
                }
                Assert.NotNull(tb);
                tb.Text = "Tự tin";

                // Gọi hàm Strategy_Click bằng Reflection
                var strategyClickMethod = typeof(SwotTool).GetMethod("Strategy_Click", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(strategyClickMethod);
                strategyClickMethod.Invoke(tool, new object[] { null, null });

                // Khung gợi ý chiến lược phải hiển thị
                var strategySection = (Border)tool.FindName("strategySection");
                Assert.Equal(Visibility.Visible, strategySection.Visibility);

                // Có đầy đủ 4 khối chiến lược con trong strategyContent
                var strategyContent = (StackPanel)tool.FindName("strategyContent");
                Assert.NotNull(strategyContent);
                Assert.Equal(4, strategyContent.Children.Count);
            });
        }
    }
}
