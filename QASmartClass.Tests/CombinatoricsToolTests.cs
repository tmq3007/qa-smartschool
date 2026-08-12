using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.LearningTools.Views.Math;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;
using Xunit;

namespace QASmartClass.Tests
{
    public class CombinatoricsToolTests
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

        private void InitializeAppAndResources()
        {
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
        }

        [Fact]
        public void TestCombinatoricsTool_LoadsPracticalApps()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new CombinatoricsTool();

                // Raise Loaded event so the handler runs
                var loadedMethod = typeof(FrameworkElement).GetMethod("OnLoaded", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(loadedMethod);
                loadedMethod.Invoke(tool, new object[] { new RoutedEventArgs(FrameworkElement.LoadedEvent) });

                // Find contentApp control
                var contentApp = (PracticalAppViewer)tool.FindName("contentApp");
                Assert.NotNull(contentApp);

                // Find lstPracticalItems ListBox inside contentApp
                var lstPracticalItems = (ListBox)contentApp.FindName("lstPracticalItems");
                Assert.NotNull(lstPracticalItems);

                var items = lstPracticalItems.ItemsSource as System.Collections.IEnumerable;
                Assert.NotNull(items);

                var itemList = new List<PracticalAppItem>();
                foreach (var item in items)
                {
                    itemList.Add((PracticalAppItem)item);
                }

                Assert.Equal(6, itemList.Count);

                var titleVN = itemList[0].Title;
                Assert.True(titleVN == "Thiết Kế Game" || titleVN == "Game Design");
            });
        }
    }
}
