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
            lock (typeof(System.Windows.Application))
            {
                try
                {
                    var appField = typeof(Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    var createdField = typeof(Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (appField != null) appField.SetValue(null, null);
                    if (createdField != null) createdField.SetValue(null, false);
                }
                catch {}

                try { _ = new System.Windows.Application(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V46LiteratureMindmapTests] Error: {ex.Message}"); }

                if (System.Windows.Application.Current != null)
                {
                    if (!System.Windows.Application.Current.Resources.Contains("Gray100"))
                        System.Windows.Application.Current.Resources.Add("Gray100", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)));
                }
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
                var createNodeMethod = typeof(LiteratureTool).GetMethod("CreateNode", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(string), typeof(Point) }, null);
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
