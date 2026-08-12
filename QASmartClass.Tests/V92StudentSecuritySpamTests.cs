using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using QASmartClass.StudentClient.Views;
using Xunit;

namespace QASmartClass.Tests
{
    public class V92StudentSecuritySpamTests
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

        [Fact]
        public void TestWhiteboardPage_IsSubmittingField_ExistsAndDefaultsToFalse()
        {
            RunOnStaThread(() =>
            {
                var page = new StudentLocalWhiteboardPage();
                Assert.NotNull(page);

                var field = typeof(StudentLocalWhiteboardPage).GetField("_isSubmitting", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                Assert.NotNull(field);
                bool val = (bool)field.GetValue(page)!;
                Assert.False(val);
            });
        }

        [Fact]
        public void TestHandRaisePage_IsSendingQuestionField_ExistsAndDefaultsToFalse()
        {
            RunOnStaThread(() =>
            {
                var page = new StudentHandRaisePage();
                Assert.NotNull(page);

                var field = typeof(StudentHandRaisePage).GetField("_isSendingQuestion", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                Assert.NotNull(field);
                bool val = (bool)field.GetValue(page)!;
                Assert.False(val);
            });
        }

        [Fact]
        public void TestSubmitPage_SpamFlags_ExistAndDefaultToFalse()
        {
            RunOnStaThread(() =>
            {
                var page = new StudentSubmitPage();
                Assert.NotNull(page);

                var fieldSubmitting = typeof(StudentSubmitPage).GetField("_isSubmitting", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var fieldSyncing = typeof(StudentSubmitPage).GetField("_isSyncing", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                Assert.NotNull(fieldSubmitting);
                Assert.NotNull(fieldSyncing);

                bool valSubmitting = (bool)fieldSubmitting.GetValue(page)!;
                bool valSyncing = (bool)fieldSyncing.GetValue(page)!;

                Assert.False(valSubmitting);
                Assert.False(valSyncing);
            });
        }

        [Fact]
        public void TestLessonPage_IsResettingNetworkField_ExistsAndDefaultsToFalse()
        {
            RunOnStaThread(() =>
            {
                var page = new StudentLessonPage();
                Assert.NotNull(page);

                var field = typeof(StudentLessonPage).GetField("_isResettingNetwork", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                Assert.NotNull(field);
                bool val = (bool)field.GetValue(page)!;
                Assert.False(val);
            });
        }

        [Fact]
        public void TestShell_NetworkAndDebounceFields_ExistAndDefaultToExpectedValues()
        {
            RunOnStaThread(() =>
            {
                // Ensure Application.Current is set to a QASmartTouch.App instance
                if (System.Windows.Application.Current == null)
                {
                    try { new QASmartTouch.App(); } catch { }
                }

                var shell = new StudentShell();
                Assert.NotNull(shell);

                var fieldReset = typeof(StudentShell).GetField("_isResettingNetwork", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var fieldRaiseTime = typeof(StudentShell).GetField("_lastQuickRaiseTime", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                Assert.NotNull(fieldReset);
                Assert.NotNull(fieldRaiseTime);

                bool valReset = (bool)fieldReset.GetValue(shell)!;
                DateTime valRaiseTime = (DateTime)fieldRaiseTime.GetValue(shell)!;

                Assert.False(valReset);
                Assert.Equal(DateTime.MinValue, valRaiseTime);

                // Clean up shell window resource leaks
                try { shell.Close(); } catch { }
            });
        }
    }
}
