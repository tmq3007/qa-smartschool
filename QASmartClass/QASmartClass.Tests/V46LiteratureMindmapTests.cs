using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.LearningTools.Views.Multi;
using Xunit;

namespace QASmartClass.Tests
{
    public class V46LiteratureMindmapTests
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
        public void TestLiteratureTool_HasNodesAndPathInCanvas()
        {
            RunOnStaThread(() =>
            {
                InitializeAppResources();

                var tool = new LiteratureTool();
                Assert.NotNull(tool);

                // Kiểm tra PreviewLink và GhostPreview trong Canvas
                var canvas = tool.FindName("MindmapCanvas") as Canvas;
                Assert.NotNull(canvas);

                var previewLink = tool.FindName("PreviewLink") as System.Windows.Shapes.Path;
                Assert.NotNull(previewLink);

                var ghostPreview = tool.FindName("GhostPreview") as Border;
                Assert.NotNull(ghostPreview);

                // Xác nhận PreviewLink và GhostPreview đã được gán logical parent là Canvas
                Assert.Equal(canvas, previewLink.Parent);
                Assert.Equal(canvas, ghostPreview.Parent);
            });
        }

        [Fact]
        public void TestLiteratureTool_ClearCanvas_KeepsStaticControls()
        {
            RunOnStaThread(() =>
            {
                InitializeAppResources();

                var tool = new LiteratureTool();
                Assert.NotNull(tool);

                var canvas = tool.FindName("MindmapCanvas") as Canvas;
                Assert.NotNull(canvas);

                var previewLink = tool.FindName("PreviewLink") as System.Windows.Shapes.Path;
                Assert.NotNull(previewLink);

                var ghostPreview = tool.FindName("GhostPreview") as Border;
                Assert.NotNull(ghostPreview);

                // Tạo 1 node giả
                var createNodeMethod = typeof(LiteratureTool).GetMethod("CreateNode", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(createNodeMethod);
                createNodeMethod.Invoke(tool, new object[] { "Event", new Point(100, 100) });

                // Xác nhận số phần tử con trong canvas tăng lên
                int beforeCount = canvas.Children.Count;
                Assert.True(beforeCount > 2); // Ít nhất có PreviewLink, GhostPreview, và Node mới tạo

                // Gọi nút Clear
                var btnClearMethod = typeof(LiteratureTool).GetMethod("BtnClear_Click", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(btnClearMethod);
                btnClearMethod.Invoke(tool, new object[] { null, null });

                // Xác nhận Canvas chỉ còn giữ lại PreviewLink và GhostPreview
                Assert.Equal(2, canvas.Children.Count);
                Assert.True(canvas.Children.Contains(previewLink));
                Assert.True(canvas.Children.Contains(ghostPreview));
            });
        }
    }
}
