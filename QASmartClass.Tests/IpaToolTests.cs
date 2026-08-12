using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Linq;
using QASmartClass.LearningTools.Views.Language;
using Xunit;

namespace QASmartClass.Tests
{
    public class IpaToolTests
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
        public void TestIpaTool_Instantiation()
        {
            RunOnStaThread(() =>
            {
                var app = Application.Current;
                if (app == null)
                {
                    try 
                    { 
                        app = new Application();
                    } 
                    catch (Exception) { }
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
                    catch (Exception) { }
                }

                var tool = new IpaTool();
                Assert.NotNull(tool);
            });
        }

        [Fact]
        public void TestIpaSections_DataConsistency()
        {
            var fieldInfo = typeof(IpaTool).GetField("IpaSections", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(fieldInfo);

            var ipaSections = fieldInfo.GetValue(null) as Array;
            Assert.NotNull(ipaSections);
            Assert.True(ipaSections.Length > 0);

            foreach (var section in ipaSections)
            {
                // Each element in IpaSections is a tuple: (string Title, string BgHex, (string Symbol, string Example, string Desc)[] Items)
                var title = section.GetType().GetField("Item1").GetValue(section) as string;
                var bgHex = section.GetType().GetField("Item2").GetValue(section) as string;
                var items = section.GetType().GetField("Item3").GetValue(section) as Array;

                Assert.False(string.IsNullOrWhiteSpace(title));
                Assert.False(string.IsNullOrWhiteSpace(bgHex));
                Assert.NotNull(items);
                Assert.True(items.Length > 0);

                foreach (var item in items)
                {
                    var symbol = item.GetType().GetField("Item1").GetValue(item) as string;
                    var example = item.GetType().GetField("Item2").GetValue(item) as string;
                    var desc = item.GetType().GetField("Item3").GetValue(item) as string;

                    Assert.False(string.IsNullOrWhiteSpace(symbol));
                    Assert.False(string.IsNullOrWhiteSpace(example));
                    Assert.False(string.IsNullOrWhiteSpace(desc));
                }
            }
        }

        [Fact]
        public void TestMinimalPairs_DataConsistency()
        {
            var fieldInfo = typeof(IpaTool).GetField("MinimalPairsData", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(fieldInfo);

            var pairs = fieldInfo.GetValue(null) as Array;
            Assert.NotNull(pairs);
            Assert.True(pairs.Length > 0);

            foreach (var pair in pairs)
            {
                // (string W1, string Ipa1, string W2, string Ipa2, string Sound)
                var w1 = pair.GetType().GetField("Item1").GetValue(pair) as string;
                var ipa1 = pair.GetType().GetField("Item2").GetValue(pair) as string;
                var w2 = pair.GetType().GetField("Item3").GetValue(pair) as string;
                var ipa2 = pair.GetType().GetField("Item4").GetValue(pair) as string;
                var sound = pair.GetType().GetField("Item5").GetValue(pair) as string;

                Assert.False(string.IsNullOrWhiteSpace(w1));
                Assert.False(string.IsNullOrWhiteSpace(ipa1));
                Assert.False(string.IsNullOrWhiteSpace(w2));
                Assert.False(string.IsNullOrWhiteSpace(ipa2));
                Assert.False(string.IsNullOrWhiteSpace(sound));

                // Confirm that we no longer use the slang "dis"
                Assert.NotEqual("dis", w1);
                Assert.NotEqual("dis", w2);
            }
        }

        [Fact]
        public void TestAllIpaQuizData_Validity()
        {
            var fieldInfo = typeof(IpaTool).GetField("AllIpaQuizData", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(fieldInfo);

            var quizData = fieldInfo.GetValue(null) as Array;
            Assert.NotNull(quizData);
            Assert.True(quizData.Length > 0);

            foreach (var quiz in quizData)
            {
                // IpaQ: (string Question, string Hint, string Answer, string[] Options)
                var question = quiz.GetType().GetProperty("Question").GetValue(quiz) as string;
                var hint = quiz.GetType().GetProperty("Hint").GetValue(quiz) as string;
                var answer = quiz.GetType().GetProperty("Answer").GetValue(quiz) as string;
                var options = quiz.GetType().GetProperty("Options").GetValue(quiz) as string[];

                Assert.False(string.IsNullOrWhiteSpace(question));
                Assert.False(string.IsNullOrWhiteSpace(hint));
                Assert.False(string.IsNullOrWhiteSpace(answer));
                Assert.NotNull(options);
                Assert.True(options.Length > 0);

                // Verify that the correct answer is indeed one of the options
                Assert.Contains(answer, options);
            }
        }
    }
}
