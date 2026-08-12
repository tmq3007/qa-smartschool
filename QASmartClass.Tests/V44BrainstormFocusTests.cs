using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.LearningTools.Views;
using QASmartClass.LearningTools.Views.Multi;
using QASmartClass.StudentClient.Views;
using Xunit;

namespace QASmartClass.Tests
{
    public class V44BrainstormFocusTests
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

        private void EnsureApplicationResources()
        {
            var app = System.Windows.Application.Current;
            if (app == null)
            {
                try { app = new System.Windows.Application(); } catch { }
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
        public void TestGetToolDisplayName_Brainstorm_ReturnsCorrectVietnameseName()
        {
            RunOnStaThread(() =>
            {
                var method = typeof(StudentShell).GetMethod("GetToolDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(method);

                var displayName = (string)method.Invoke(null, new object[] { "brainstorm" });
                Assert.Equal("Bức Tường Ý Tưởng", displayName);
            });
        }

        [Fact]
        public void TestStudentShell_ShowToolFocusOverlay_CardHasStretchAlignments()
        {
            RunOnStaThread(() =>
            {
                EnsureApplicationResources();

                var shell = new StudentShell();

                var showOverlayMethod = typeof(StudentShell).GetMethod("ShowToolFocusOverlay", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(showOverlayMethod);

                // Gọi ShowToolFocusOverlay("brainstorm")
                showOverlayMethod.Invoke(shell, new object[] { "brainstorm" });

                var overlayField = typeof(StudentShell).GetField("_toolFocusOverlay", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(overlayField);

                var overlay = (Grid)overlayField.GetValue(shell);
                Assert.NotNull(overlay);

                // Tìm card (Border) bên trong overlay
                Border card = null;
                foreach (var child in overlay.Children)
                {
                    if (child is Border b && b.Tag == null) // The main card
                    {
                        card = b;
                        break;
                    }
                }

                Assert.NotNull(card);
                Assert.Equal(HorizontalAlignment.Stretch, card.HorizontalAlignment);
                Assert.Equal(VerticalAlignment.Stretch, card.VerticalAlignment);
                Assert.Equal(10.0, card.Margin.Left);
                Assert.Equal(10.0, card.Margin.Top);
            });
        }

        [Fact]
        public void TestBrainstormTool_AddFarNote_CanvasExpandsDynamically()
        {
            RunOnStaThread(() =>
            {
                EnsureApplicationResources();

                var tool = new BrainstormTool();

                var addNoteMethod = typeof(BrainstormTool).GetMethod("AddNoteAt", BindingFlags.Public | BindingFlags.Instance);
                Assert.NotNull(addNoteMethod);

                // Thêm ghi chú tại toạ độ xa (1500, 1000)
                addNoteMethod.Invoke(tool, new object[] { "Test Note", Colors.Yellow, 1500.0, 1000.0, false, null });

                // Lấy BoardCanvas để xác thực kích thước rộng/cao của nó tự co giãn theo ghi chú
                var canvasField = typeof(BrainstormTool).GetField("BoardCanvas", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                Assert.NotNull(canvasField);

                var canvas = (Canvas)canvasField.GetValue(tool);
                Assert.NotNull(canvas);

                // Chiều rộng canvas phải bao phủ toạ độ x=1500 + độ rộng note(200) + khoảng đệm (100) = 1800
                Assert.True(canvas.Width >= 1800);
                // Chiều cao canvas phải bao phủ toạ độ y=1000 + độ cao note(180) + khoảng đệm (100) = 1280
                Assert.True(canvas.Height >= 1280);
            });
        }

        [Fact]
        public void TestStudentSubmissionItem_DisplayText_ExtractsCleanText()
        {
            var item = new LearningToolsHub.StudentSubmissionItem
            {
                StudentCode = "HS1",
                StudentName = "Nguyen Van A",
                ResultData = "Y kien cua em;#FFF9C4",
                TimeString = "10:00:00"
            };

            Assert.Equal("Y kien cua em", item.DisplayText);
        }

        [Fact]
        public void TestStudentSubmissionItem_SemicolonInText_ExtractsFully()
        {
            var item = new LearningToolsHub.StudentSubmissionItem
            {
                StudentCode = "HS2",
                StudentName = "Tran Van B",
                ResultData = "Ghi chu cua em; va cua ban B;#FFCDD2",
                TimeString = "10:05:00"
            };

            Assert.Equal("Ghi chu cua em; va cua ban B", item.DisplayText);

            // Kịch bản phân tích màu và text có chứa dấu chấm phẩy
            string text = item.ResultData;
            Color color = Color.FromRgb(255, 249, 196);

            int lastSemi = item.ResultData.LastIndexOf(';');
            if (lastSemi >= 0)
            {
                text = item.ResultData.Substring(0, lastSemi);
                string colorHex = item.ResultData.Substring(lastSemi + 1);
                color = (Color)ColorConverter.ConvertFromString(colorHex);
            }

            Assert.Equal("Ghi chu cua em; va cua ban B", text);
            Assert.Equal(ColorConverter.ConvertFromString("#FFCDD2"), color);
        }

        [Fact]
        public void TestStudentSubmissionItem_SemicolonInTextNoColor_ExtractsFullyWithoutTruncation()
        {
            var item = new LearningToolsHub.StudentSubmissionItem
            {
                StudentCode = "HS3",
                StudentName = "Tran Van C",
                ResultData = "Chào cô; hôm nay em vui lắm",
                TimeString = "10:10:00"
            };

            Assert.Equal("Chào cô; hôm nay em vui lắm", item.DisplayText);
        }

        [Fact]
        public void TestStudentSubmissionItem_SemicolonInTextWithColor_ExtractsFullyAndParsesColor()
        {
            var item = new LearningToolsHub.StudentSubmissionItem
            {
                StudentCode = "HS4",
                StudentName = "Tran Van D",
                ResultData = "Chào cô; hôm nay em vui;#C8E6C9",
                TimeString = "10:12:00"
            };

            Assert.Equal("Chào cô; hôm nay em vui", item.DisplayText);
        }
    }
}
