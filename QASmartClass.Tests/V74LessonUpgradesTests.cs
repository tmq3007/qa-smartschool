using Xunit;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading;
using QASmartClass.Classroom.Views;

namespace QASmartClass.Tests
{
    public class V74LessonUpgradesTests
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
            Exception? ex = null;
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

        /*
        [Fact]
        public void TestLessonRunnerPage_ToggleSidebarButton_WidthAndFunctionality()
        {
            RunOnStaThread(() =>
            {
                var page = new LessonRunnerPage();
                var button = page.FindName("btnToggleSidebar") as Button;
                Assert.NotNull(button);
                Assert.Equal(30, button.Width);

                var col = page.FindName("colSidebar") as ColumnDefinition;
                Assert.NotNull(col);
                Assert.Equal(220, col.Width.Value);

                // Test click logic
                var method = typeof(LessonRunnerPage).GetMethod("ToggleSidebar_Click",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(method);

                method.Invoke(page, new object[] { null!, null! });
                Assert.Equal(0, col.Width.Value);
                Assert.Equal("▶", button.Content);

                method.Invoke(page, new object[] { null!, null! });
                Assert.Equal(220, col.Width.Value);
                Assert.Equal("◀", button.Content);
            });
        }

        [Fact]
        public void TestLessonEditorPage_FAB_ContainsAllButtons()
        {
            RunOnStaThread(() =>
            {
                var page = new LessonEditorPage();
                var mainGrid = page.Content as Grid;
                Assert.NotNull(mainGrid);

                var editorColGrid = mainGrid.Children[0] as Grid;
                Assert.NotNull(editorColGrid);

                // Find the Border that has Grid.Row = 2 and VerticalAlignment = Bottom
                Border? fabBorder = null;
                foreach (var child in editorColGrid.Children)
                {
                    if (child is Border border && Grid.GetRow(border) == 2 && border.VerticalAlignment == VerticalAlignment.Bottom)
                    {
                        fabBorder = border;
                        break;
                    }
                }
                Assert.NotNull(fabBorder);

                var stackPanel = fabBorder.Child as StackPanel;
                Assert.NotNull(stackPanel);
                Assert.Equal(7, stackPanel.Children.Count);

                var btnTexts = stackPanel.Children.Cast<Button>().Select(b => b.Content.ToString()).ToList();
                Assert.Contains("➕ Văn bản", btnTexts);
                Assert.Contains("➕ Hình ảnh", btnTexts);
                Assert.Contains("➕ Video", btnTexts);
                Assert.Contains("➕ Mô phỏng", btnTexts);
                Assert.Contains("➕ PDF", btnTexts);
                Assert.Contains("➕ Quiz", btnTexts);
                Assert.Contains("➕ Audio", btnTexts);
            });
        }

        [Fact]
        public void TestLessonEditorPage_StatusBadgeIsClickableButton()
        {
            RunOnStaThread(() =>
            {
                var page = new LessonEditorPage();
                var badgeButton = page.FindName("statusBadge") as Button;
                Assert.NotNull(badgeButton);
                Assert.True(badgeButton.Cursor == System.Windows.Input.Cursors.Hand);
                
                // Confirm txtStatus is accessible directly in the logical tree of the page
                var textStatus = page.FindName("txtStatus") as TextBlock;
                Assert.NotNull(textStatus);
                Assert.Equal("📝 Bản nháp", textStatus.Text);

                var brush = textStatus.Foreground as SolidColorBrush;
                Assert.NotNull(brush);
                Assert.Equal(ColorConverter.ConvertFromString("#B75300"), brush.Color);
            });
        }

        [Fact]
        public void TestLessonEditorPage_txtDescription_HasMaxLengthLimit()
        {
            RunOnStaThread(() =>
            {
                var page = new LessonEditorPage();
                var txtDesc = page.FindName("txtDescription") as TextBox;
                Assert.NotNull(txtDesc);
                Assert.Equal(500, txtDesc.MaxLength);
            });
        }
        */

        /*
        [Fact]
        public void TestLessonEditorPage_SanitizeInput_EscapesHtmlCharacters()
        {
            var dirty = "<script>alert('XSS')</script>Chào bạn & \"hello\"";
            var clean = LessonEditorPage.SanitizeInput(dirty);
            Assert.Equal("Chào bạn &amp; &quot;hello&quot;", clean);
        }
        */
    }
}
