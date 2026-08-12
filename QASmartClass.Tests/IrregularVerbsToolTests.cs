using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using QASmartClass.LearningTools.Views.Language;
using Xunit;

namespace QASmartClass.Tests
{
    public class IrregularVerbsToolTests
    {
        private void RunOnStaThread(Action action)
        {
            Exception ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    try
                    {
                        var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                        if (appCreatedField != null) appCreatedField.SetValue(null, false);
                        var currentField = typeof(Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                        if (currentField != null) currentField.SetValue(null, null);
                    }
                    catch { }

                    var app = new Application();
                    try
                    {
                        app.Resources.MergedDictionaries.Add(new ResourceDictionary 
                        { 
                            Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute) 
                        });
                    }
                    catch { }

                    action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
                finally
                {
                    try
                    {
                        if (Application.Current != null)
                        {
                            Application.Current.Shutdown();
                        }
                    }
                    catch { }
                    try
                    {
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
                    }
                    catch { }
                    try
                    {
                        var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                        if (appCreatedField != null) appCreatedField.SetValue(null, false);
                        var currentField = typeof(Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                        if (currentField != null) currentField.SetValue(null, null);
                    }
                    catch { }
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
        public void TestCheckAnswer_AlternativeSpellings()
        {
            // Get CheckAnswer method using reflection since it is private static
            var method = typeof(IrregularVerbsTool).GetMethod("CheckAnswer", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // Test was/were
            var resWas = (bool)method.Invoke(null, new object[] { "was", "was/were" });
            var resWere = (bool)method.Invoke(null, new object[] { "were", "was/were" });
            var resWrongBe = (bool)method.Invoke(null, new object[] { "been", "was/were" });

            Assert.True(resWas);
            Assert.True(resWere);
            Assert.False(resWrongBe);

            // Test got/gotten
            var resGot = (bool)method.Invoke(null, new object[] { "got", "got/gotten" });
            var resGotten = (bool)method.Invoke(null, new object[] { "gotten", "got/gotten" });
            var resWrongGet = (bool)method.Invoke(null, new object[] { "gets", "got/gotten" });

            Assert.True(resGot);
            Assert.True(resGotten);
            Assert.False(resWrongGet);

            // Test learnt/learned
            var resLearnt = (bool)method.Invoke(null, new object[] { "learnt", "learnt/learned" });
            var resLearned = (bool)method.Invoke(null, new object[] { "learned", "learnt/learned" });

            Assert.True(resLearnt);
            Assert.True(resLearned);
        }

        [Fact]
        public void TestGetVerbPattern_Classification()
        {
            // Get GetVerbPattern method using reflection since it is private static
            var method = typeof(IrregularVerbsTool).GetMethod("GetVerbPattern", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            // AAA: cut -> cut -> cut
            var patternAAA = (string)method.Invoke(null, new object[] { "cut", "cut", "cut" });
            Assert.Equal("AAA", patternAAA);

            // ABA: become -> became -> become
            var patternABA = (string)method.Invoke(null, new object[] { "become", "became", "become" });
            Assert.Equal("ABA", patternABA);

            // ABB: buy -> bought -> bought
            var patternABB = (string)method.Invoke(null, new object[] { "buy", "bought", "bought" });
            Assert.Equal("ABB", patternABB);

            // ABC: go -> went -> gone
            var patternABC = (string)method.Invoke(null, new object[] { "go", "went", "gone" });
            Assert.Equal("ABC", patternABC);

            // ABB with variants: get -> got -> got/gotten
            var patternGet = (string)method.Invoke(null, new object[] { "get", "got", "got/gotten" });
            Assert.Equal("ABB", patternGet);

            // ABB with variants: learn -> learnt/learned -> learnt/learned
            var patternLearn = (string)method.Invoke(null, new object[] { "learn", "learnt/learned", "learnt/learned" });
            Assert.Equal("ABB", patternLearn);

            // AAB: beat -> beat -> beaten
            var patternBeat = (string)method.Invoke(null, new object[] { "beat", "beat", "beaten" });
            Assert.Equal("AAB", patternBeat);
        }

        [Fact]
        public void TestIrregularVerbsTool_Instantiation()
        {
            RunOnStaThread(() =>
            {
                var tool = new IrregularVerbsTool();
                Assert.NotNull(tool);
            });
        }
    }
}
