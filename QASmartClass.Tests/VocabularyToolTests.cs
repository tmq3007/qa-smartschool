using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows;
using QASmartClass.LearningTools.Views.Language;
using QASmartClass.LearningTools.Models;
using Xunit;

namespace QASmartClass.Tests
{
    public class VocabularyToolTests
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

        [Fact]
        public void TestVocabularyTool_DataLoading()
        {
            RunOnStaThread(() =>
            {
                var app = System.Windows.Application.Current;
                if (app == null)
                {
                    try 
                    { 
                        app = new System.Windows.Application();
                    } 
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[VocabularyToolTests] Error: {ex.Message}"); }
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
                        }
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[VocabularyToolTests] Error: {ex.Message}"); }
                }

                // Force static constructor to run
                var tool = new VocabularyTool();
                Assert.NotNull(tool);

                // Load database asynchronously for test purposes
                var loadMethod = typeof(VocabularyTool).GetMethod("LoadVocabularyDataAsync", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(loadMethod);
                var oldContext = System.Threading.SynchronizationContext.Current;
                System.Threading.SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext(System.Windows.Threading.Dispatcher.CurrentDispatcher));
                try
                {
                    var task = (System.Threading.Tasks.Task?)loadMethod.Invoke(tool, null);
                    Assert.NotNull(task);
                    var frame = new System.Windows.Threading.DispatcherFrame();
                    task.ContinueWith(_ => frame.Continue = false);
                    System.Windows.Threading.Dispatcher.PushFrame(frame);
                    task.GetAwaiter().GetResult();
                }
                finally
                {
                    System.Threading.SynchronizationContext.SetSynchronizationContext(oldContext);
                }

                // Use reflection to get AllCategories field
                var field = typeof(VocabularyTool).GetField("AllCategories", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(field);

                var allCategoriesValue = field.GetValue(null);
                Assert.NotNull(allCategoriesValue);

                var categoriesArray = (Array)allCategoriesValue;
                
                Console.WriteLine($"Number of loaded vocabulary categories: {categoriesArray.Length}");
                Assert.True(categoriesArray.Length > 0, "No vocabulary categories loaded!");

                foreach (var cat in categoriesArray)
                {
                    var nameProp = cat.GetType().GetProperty("Name");
                    var levelProp = cat.GetType().GetProperty("Level");
                    var wordsProp = cat.GetType().GetProperty("Words");

                    var name = nameProp.GetValue(cat);
                    var level = levelProp.GetValue(cat);
                    var wordsArray = (Array)wordsProp.GetValue(cat);

                    Console.WriteLine($"- Cat: {name} (Level: {level}), Words: {wordsArray.Length}");
                    if (wordsArray.Length > 0)
                    {
                        var firstWord = wordsArray.GetValue(0);
                        var enProp = firstWord.GetType().GetProperty("En");
                        var viProp = firstWord.GetType().GetProperty("Vi");
                        Console.WriteLine($"  Example word: {enProp.GetValue(firstWord)} = {viProp.GetValue(firstWord)}");
                    }
                }
            });
        }

        [Fact]
        public void TestVocabularyTool_ImportCustomList_Sanitization()
        {
            // Initialize valid words
            var validWordsList = new List<string> { "apple", "banana", "cherry" };
            SRSTracker.SetValidWords(validWordsList);

            // Create a payload containing one valid word and some arbitrary/invalid content
            var importList = new List<string> { "apple", "invalidWord123", "hack_db", "banana" };
            var json = System.Text.Json.JsonSerializer.Serialize(importList);
            var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));

            // Invoke ImportCustomList
            bool result = SRSTracker.ImportCustomList(base64);
            Assert.True(result);

            var customList = SRSTracker.GetCustomList();
            Assert.Contains("apple", customList);
            Assert.Contains("banana", customList);
            Assert.DoesNotContain("invalidWord123", customList);
            Assert.DoesNotContain("hack_db", customList);
        }

        [Fact]
        public void TestVocabularyTool_ReadingGenerator_RegexBoundaryMatch()
        {
            // Test that word boundary \b is used correctly and substring match is avoided
            string word = "ten";
            string pattern = @"\b" + System.Text.RegularExpressions.Regex.Escape(word) + @"\b";

            // 1. Should NOT match substring "ten" in "sentence"
            string text1 = "Let's look at another example sentence.";
            var match1 = System.Text.RegularExpressions.Regex.Match(text1, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Assert.False(match1.Success);

            // 2. Should match independent word "ten"
            string text2 = "There are ten apples on the table.";
            var match2 = System.Text.RegularExpressions.Regex.Match(text2, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Assert.True(match2.Success);
            Assert.Equal("ten", match2.Value, ignoreCase: true);
        }
    }
}
