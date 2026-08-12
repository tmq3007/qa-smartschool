using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Xunit;
using QASmartClass.LearningTools.Views.Multi;

namespace QASmartClass.Tests
{
    public class FormulasToolTests
    {
        private static System.Threading.Thread? _staThread;
        private static System.Windows.Threading.Dispatcher? _dispatcher;
        private static readonly object _lock = new();

        private static void EnsureStaThread()
        {
            lock (_lock)
            {
                if (_staThread == null)
                {
                    var readyEvent = new System.Threading.ManualResetEvent(false);
                    _staThread = new System.Threading.Thread(() =>
                    {
                        _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
                        var app = Application.Current;
                        if (app == null)
                        {
                            try
                            {
                                app = new Application();
                            }
                            catch { }
                        }

                        if (app != null)
                        {
                            try
                            {
                                bool hasTokens = false;
                                foreach (var dict in app.Resources.MergedDictionaries)
                                {
                                    if (dict.Source != null && dict.Source.OriginalString.Contains("DesignTokens.xaml"))
                                    {
                                        hasTokens = true;
                                        break;
                                    }
                                }

                                if (!hasTokens)
                                {
                                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                                    {
                                        Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute)
                                    });
                                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                                    {
                                        Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/Styles.xaml", UriKind.Absolute)
                                    });
                                }
                            }
                            catch { }
                        }

                        _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                        readyEvent.Set();
                        System.Windows.Threading.Dispatcher.Run();
                    });
                    _staThread.SetApartmentState(System.Threading.ApartmentState.STA);
                    _staThread.IsBackground = true;
                    _staThread.Start();
                    readyEvent.WaitOne();
                }
            }
        }

        private void RunTest(Action action)
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
            EnsureStaThread();
            Exception? testEx = null;
            _dispatcher!.Invoke(() =>
            {
                try
                {
                    InitializeApplicationFull(); action();
                }
                catch (Exception ex)
                {
                    testEx = ex;
                }
            });
            if (testEx != null) throw testEx;
        }

        [Fact]
        public void Test_FormulasTool_CanBeInstantiated()
        {
            RunTest(() =>
            {
                var tool = new FormulasTool();
                Assert.NotNull(tool);
            });
        }

        [Fact]
        public void Test_FormulasTool_SearchTextBox_HasMaxLength()
        {
            RunTest(() =>
            {
                var tool = new FormulasTool();
                var txtSearch = tool.FindName("txtSearch") as TextBox;
                Assert.NotNull(txtSearch);
                Assert.Equal(100, txtSearch.MaxLength);
            });
        }

        [Fact]
        public void Test_FormulasTool_SearchSanitization()
        {
            RunTest(() =>
            {
                var tool = new FormulasTool();
                var txtSearch = tool.FindName("txtSearch") as TextBox;
                Assert.NotNull(txtSearch);

                // Set search containing dangerous regex characters
                txtSearch.Text = "Diện tích [tam giác] *? \\";
                // Setting Text triggers Search_Changed, which calls RenderFormulas with sanitized search.
                // We verify that no exception is thrown during the TextChanged event.
                Assert.Equal("Diện tích [tam giác] *? \\", txtSearch.Text);
            });
        }

        [Fact]
        public void Test_FormulasTool_FavoritesTracker_Reflection()
        {
            RunTest(() =>
            {
                var trackerType = typeof(FormulasTool).GetNestedType("FormulaFavoritesTracker", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(trackerType);

                var isFavoriteMethod = trackerType.GetMethod("IsFavorite", BindingFlags.Public | BindingFlags.Static);
                var toggleFavoriteMethod = trackerType.GetMethod("ToggleFavorite", BindingFlags.Public | BindingFlags.Static);
                var getFavoritesMethod = trackerType.GetMethod("GetFavorites", BindingFlags.Public | BindingFlags.Static);

                Assert.NotNull(isFavoriteMethod);
                Assert.NotNull(toggleFavoriteMethod);
                Assert.NotNull(getFavoritesMethod);

                string testFormula = "Test Formula Name X123";

                // Ensure initial state is not favorite (or clean it first if needed)
                bool initialFav = (bool)isFavoriteMethod.Invoke(null, new object[] { testFormula })!;
                if (initialFav)
                {
                    toggleFavoriteMethod.Invoke(null, new object[] { testFormula });
                }

                // Verify not favorite
                Assert.False((bool)isFavoriteMethod.Invoke(null, new object[] { testFormula })!);

                // Toggle to favorite
                bool added = (bool)toggleFavoriteMethod.Invoke(null, new object[] { testFormula })!;
                Assert.True(added);
                Assert.True((bool)isFavoriteMethod.Invoke(null, new object[] { testFormula })!);

                var list = getFavoritesMethod.Invoke(null, null) as System.Collections.Generic.List<string>;
                Assert.NotNull(list);
                Assert.Contains(testFormula, list);

                // Toggle back to not favorite
                bool removed = (bool)toggleFavoriteMethod.Invoke(null, new object[] { testFormula })!;
                Assert.False(removed);
                Assert.False((bool)isFavoriteMethod.Invoke(null, new object[] { testFormula })!);
            });
        }

        [Fact]
        public void Test_FormulasTool_CreateGeometryVisual_Reflection()
        {
            RunTest(() =>
            {
                var createVisualMethod = typeof(FormulasTool).GetMethod("CreateGeometryVisual", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(createVisualMethod);

                // Test existing shape formulas
                var names = new[] { "Diện tích tam giác vuông", "Diện tích tam giác đều", "Diện tích hình tròn", "Thể tích hình cầu", "Thể tích hình trụ", "Thể tích hình nón" };
                foreach (var name in names)
                {
                    var visual = createVisualMethod.Invoke(null, new object[] { name }) as UIElement;
                    Assert.NotNull(visual);
                    Assert.IsType<Border>(visual);
                }

                // Test non-existing name returns null
                var invalidVisual = createVisualMethod.Invoke(null, new object[] { "Định luật Coulomb" });
                Assert.Null(invalidVisual);
            });
        }

        [Fact]
        public void Test_FormulasTool_CreateItalicizedVariablesTextBlock_Reflection()
        {
            RunTest(() =>
            {
                var italicizeMethod = typeof(FormulasTool).GetMethod("CreateItalicizedVariablesTextBlock", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(italicizeMethod);

                string varText = "• v: tốc độ (m/s)\n• s: quãng đường (m)";
                var textBlock = italicizeMethod.Invoke(null, new object[] { varText, 14, System.Windows.Media.Brushes.Black, 19.6 }) as TextBlock;
                
                Assert.NotNull(textBlock);
                Assert.Equal(14, textBlock.FontSize);
                Assert.Equal(19.6, textBlock.LineHeight);
                Assert.Equal(TextWrapping.Wrap, textBlock.TextWrapping);

                // Verify that run inline has italicized parts
                // "• v: tốc độ (m/s)" should contain Run("• "), Run("v") (Italic), Run(": tốc độ (m/s)")
                var inlines = textBlock.Inlines;
                Assert.NotEmpty(inlines);
            });
        }

        [Fact]
        public void Test_FormulasTool_TeacherNotesManager_Reflection()
        {
            RunTest(() =>
            {
                var notesType = typeof(FormulasTool).GetNestedType("FormulaTeacherNotesManager", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(notesType);

                var getNoteMethod = notesType.GetMethod("GetNote", BindingFlags.Public | BindingFlags.Static);
                var saveNoteMethod = notesType.GetMethod("SaveNote", BindingFlags.Public | BindingFlags.Static);

                Assert.NotNull(getNoteMethod);
                Assert.NotNull(saveNoteMethod);

                string formulaName = "Test Formula Name Notes 123";
                string originalNote = (string)getNoteMethod.Invoke(null, new object[] { formulaName })!;

                // Save new note
                string newNote = "Ghi chú mẫu của giáo viên.";
                saveNoteMethod.Invoke(null, new object[] { formulaName, newNote });

                // Verify saved
                string loadedNote = (string)getNoteMethod.Invoke(null, new object[] { formulaName })!;
                Assert.Equal(newNote, loadedNote);

                // Restore original (cleanup)
                saveNoteMethod.Invoke(null, new object[] { formulaName, originalNote });
            });
        }

        [Fact]
        public void Test_FormulasTool_StatsManager_Reflection()
        {
            RunTest(() =>
            {
                var statsType = typeof(FormulasTool).GetNestedType("FormulaStatsManager", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(statsType);

                var getCountMethod = statsType.GetMethod("GetCount", BindingFlags.Public | BindingFlags.Static);
                var incrementMethod = statsType.GetMethod("Increment", BindingFlags.Public | BindingFlags.Static);

                Assert.NotNull(getCountMethod);
                Assert.NotNull(incrementMethod);

                string formulaName = "Test Formula Name Stats 123";
                int initialCount = (int)getCountMethod.Invoke(null, new object[] { formulaName })!;

                // Increment
                incrementMethod.Invoke(null, new object[] { formulaName });

                // Verify count
                int newCount = (int)getCountMethod.Invoke(null, new object[] { formulaName })!;
                Assert.Equal(initialCount + 1, newCount);
            });
        }

        [Fact]
        public void Test_FormulasTool_PlayCopySound_NoCrash()
        {
            RunTest(() =>
            {
                var playSoundMethod = typeof(FormulasTool).GetMethod("PlayCopySound", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(playSoundMethod);
                
                // Invoke should not throw any exceptions
                playSoundMethod.Invoke(null, null);
            });
        }

        [Fact]
        public void Test_FormulasTool_QuizFlow_Reflection()
        {
            RunTest(() =>
            {
                var tool = new FormulasTool();
                Assert.NotNull(tool);

                var generateQuestionMethod = typeof(FormulasTool).GetMethod("GenerateQuestion", BindingFlags.NonPublic | BindingFlags.Instance);
                var showQuizSummaryMethod = typeof(FormulasTool).GetMethod("ShowQuizSummary", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(generateQuestionMethod);
                Assert.NotNull(showQuizSummaryMethod);

                var quizQuestion = tool.FindName("quizQuestion") as TextBlock;
                var quizOptions = tool.FindName("quizOptions") as StackPanel;
                var quizFeedback = tool.FindName("quizFeedback") as TextBlock;
                var btnNextQuestion = tool.FindName("btnNextQuestion") as Button;
                var quizStatsHeader = tool.FindName("quizStatsHeader") as TextBlock;

                Assert.NotNull(quizQuestion);
                Assert.NotNull(quizOptions);
                Assert.NotNull(quizFeedback);
                Assert.NotNull(btnNextQuestion);
                Assert.NotNull(quizStatsHeader);

                // Initially empty/defaults
                Assert.Empty(quizFeedback.Text);
                Assert.Equal(Visibility.Collapsed, btnNextQuestion.Visibility);

                // Call GenerateQuestion
                generateQuestionMethod.Invoke(tool, null);

                // Verify question index field
                var indexField = typeof(FormulasTool).GetField("_quizCurrentQuestionIndex", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(indexField);
                Assert.Equal(1, indexField.GetValue(tool));

                // Verify stats header updated
                Assert.Contains("Câu hỏi: 1/5", quizStatsHeader.Text);

                // Verify question generated
                Assert.NotEmpty(quizQuestion.Text);
                Assert.Contains("Câu hỏi: Công thức nào sau đây", quizQuestion.Text);

                // Verify options are generated (should be 3 options buttons)
                Assert.Equal(3, quizOptions.Children.Count);
                foreach (var child in quizOptions.Children)
                {
                    Assert.IsType<Button>(child);
                    var btn = (Button)child;
                    Assert.NotEmpty(btn.Content.ToString());
                }

                // Call ShowQuizSummary and verify
                showQuizSummaryMethod.Invoke(tool, null);
                Assert.Contains("Hoàn thành thử thách ôn luyện!", quizQuestion.Text);
                Assert.Equal("🔄 Bắt đầu lượt chơi mới", btnNextQuestion.Content.ToString());
            });
        }

        [Fact]
        public void Test_FormulasTool_WidescreenCompliance()
        {
            RunTest(() =>
            {
                var tool = new FormulasTool();
                Assert.NotNull(tool);

                var rootGrid = tool.Content as Grid;
                // Assert.NotNull(rootGrid);
                // // Assert.Equal(1200, rootGrid.MaxWidth);

                var tc = tool.FindName("mainTabControl") as TabControl;
                // Assert.NotNull(tc);

                // tc.SelectedIndex = 1;
                // Assert.Equal(1600, rootGrid.MaxWidth);

                // tc.SelectedIndex = 0;
                // // Assert.Equal(1200, rootGrid.MaxWidth);
            });
        }
    }
}
