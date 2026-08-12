using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Windows;
using QASmartClass.LearningTools.Views.Language;
using Xunit;

namespace QASmartClass.Tests
{
    public class VocabularyToolTests
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
                        }
                    }
                    catch { }
                }

                // Force static constructor to run
                var tool = new VocabularyTool();
                Assert.NotNull(tool);

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
    }
}
