using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using QASmartClass.LearningTools.Views.Workplace;
using Xunit;

namespace QASmartClass.Tests
{
    public class V40FiveSToolTests
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
                try
                {
                    app = new System.Windows.Application();
                }
                catch { }
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
        public void TestFiveSTool_Initialization_NoException()
        {
            RunOnStaThread(() =>
            {
                EnsureApplicationResources();

                // Attempt to instantiate the control, which triggers layout and SizeChanged events.
                // It should compile and instantiate without throwing KeyNotFoundException.
                var tool = new FiveSTool();
                Assert.NotNull(tool);

                // Manually trigger UpdateChart using reflection to ensure it executes without errors.
                var method = typeof(FiveSTool).GetMethod("UpdateChart", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                // Invoking UpdateChart should not throw KeyNotFoundException
                method.Invoke(tool, null);
            });
        }

        [Fact]
        public void TestFiveSTool_UpgradeFeatures()
        {
            RunOnStaThread(() =>
            {
                EnsureApplicationResources();

                var tool = new FiveSTool();
                Assert.NotNull(tool);

                // 1. Verify txtSubject MaxLength constraint
                var txtSubject = (System.Windows.Controls.TextBox)tool.FindName("txtSubject");
                Assert.NotNull(txtSubject);
                Assert.Equal(50, txtSubject.MaxLength);

                // 2. Verify confettiCanvas presence and click transparency
                var confettiCanvas = (System.Windows.Controls.Canvas)tool.FindName("confettiCanvas");
                Assert.NotNull(confettiCanvas);
                Assert.False(confettiCanvas.IsHitTestVisible);

                // 3. Verify txtAdvice presence
                var txtAdvice = (System.Windows.Controls.TextBlock)tool.FindName("txtAdvice");
                Assert.NotNull(txtAdvice);

                // 4. Verify Zoom operations via reflection
                var zoomInMethod = typeof(FiveSTool).GetMethod("ZoomIn_Click", BindingFlags.NonPublic | BindingFlags.Instance);
                var zoomOutMethod = typeof(FiveSTool).GetMethod("ZoomOut_Click", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(zoomInMethod);
                Assert.NotNull(zoomOutMethod);

                // Zoom in and verify font changes
                var panelGuideText = (System.Windows.Controls.StackPanel)tool.FindName("panelGuideText");
                Assert.NotNull(panelGuideText);
                double initialSize = (double)panelGuideText.GetValue(System.Windows.Documents.TextElement.FontSizeProperty);

                zoomInMethod.Invoke(tool, new object[] { null, null });
                double sizeAfterZoomIn = (double)panelGuideText.GetValue(System.Windows.Documents.TextElement.FontSizeProperty);
                Assert.Equal(initialSize + 1.5, sizeAfterZoomIn);

                zoomOutMethod.Invoke(tool, new object[] { null, null });
                double sizeAfterZoomOut = (double)panelGuideText.GetValue(System.Windows.Documents.TextElement.FontSizeProperty);
                Assert.Equal(initialSize, sizeAfterZoomOut);
            });
        }

        [Fact]
        public void TestFiveSTool_RatingButtonTags()
        {
            RunOnStaThread(() =>
            {
                EnsureApplicationResources();

                // Clear database state to prevent test pollution
                QASmartClass.LearningTools.Helpers.DbManager.SaveWorkplaceState("five_s", "");

                var tool = new FiveSTool();
                Assert.NotNull(tool);

                // Force Loaded events
                var loadedMethod = typeof(FrameworkElement).GetMethod("OnLoaded", BindingFlags.NonPublic | BindingFlags.Instance);
                loadedMethod?.Invoke(tool, new object[] { new RoutedEventArgs(FrameworkElement.LoadedEvent) });

                var checklistPanel = (System.Windows.Controls.StackPanel)tool.FindName("checklistPanel");
                Assert.NotNull(checklistPanel);

                // Verify rating buttons are generated
                Assert.NotEmpty(checklistPanel.Children);
                var firstRow = (System.Windows.Controls.Border)checklistPanel.Children[0];
                var grid = (System.Windows.Controls.Grid)firstRow.Child;
                var starPanel = (System.Windows.Controls.StackPanel)grid.Children[1];
                Assert.Equal(5, starPanel.Children.Count);

                // Since we load default checklists, first question should have score 0 (all buttons should have Tag = "Normal")
                foreach (System.Windows.Controls.Button btn in starPanel.Children)
                {
                    Assert.Equal("Normal", btn.Tag);
                }

                // Click the 4th button (index 3) and verify it gets selected
                var btn4 = (System.Windows.Controls.Button)starPanel.Children[3];
                btn4.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

                // Re-fetch the layout since it re-renders
                firstRow = (System.Windows.Controls.Border)checklistPanel.Children[0];
                grid = (System.Windows.Controls.Grid)firstRow.Child;
                starPanel = (System.Windows.Controls.StackPanel)grid.Children[1];

                Assert.Equal("Selected", ((System.Windows.Controls.Button)starPanel.Children[3]).Tag);
                Assert.Equal("Normal", ((System.Windows.Controls.Button)starPanel.Children[0]).Tag);
            });
        }

        [Fact]
        public void TestDbManager_SaveFiveSHistory_NoException()
        {
            // Verify writing to database history doesn't throw exceptions
            var ex = Record.Exception(() =>
            {
                QASmartClass.LearningTools.Helpers.DbManager.SaveFiveSHistory("Test Room", 25.0, "[[5,5,5],[5,5,5],[5,5,5],[5,5,5],[5,5,5]]");
            });
            Assert.Null(ex);
        }
    }
}
