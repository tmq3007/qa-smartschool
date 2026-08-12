using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.LearningTools.Views.Language;
using Xunit;

namespace QASmartClass.Tests
{
    public class GrammarToolFocusTests
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
        public void TestGrammarTool_FocusAndReset()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try 
                    { 
                        var app = new System.Windows.Application();
                        app.Resources.MergedDictionaries.Add(new ResourceDictionary 
                        { 
                            Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute) 
                        });
                    } 
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[GrammarToolFocusTests] Error: {ex.Message}"); }
                }
                else
                {
                    try
                    {
                        if (!System.Windows.Application.Current.Resources.MergedDictionaries.Any(d => d.Source != null && d.Source.OriginalString.Contains("DesignTokens.xaml")))
                        {
                            System.Windows.Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary 
                            { 
                                Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute) 
                            });
                        }
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[GrammarToolFocusTests] Error: {ex.Message}"); }
                }

                var tool = new GrammarTool();
                Assert.NotNull(tool);

                // Thử gọi phương thức FilterFocusedSection
                string targetSection = "simple_present";
                
                // Gọi FilterFocusedSection
                tool.FilterFocusedSection(targetSection);

                // Xác minh các TabItem khác bị ẩn
                var tabControl = typeof(GrammarTool).GetField("grammarTabControl", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(tool) as TabControl;
                if (tabControl != null)
                {
                    Assert.Equal(0, tabControl.SelectedIndex);
                    for (int i = 1; i < tabControl.Items.Count; i++)
                    {
                        var tabItem = tabControl.Items[i] as TabItem;
                        if (tabItem != null)
                        {
                            Assert.Equal(Visibility.Collapsed, tabItem.Visibility);
                        }
                    }
                }

                // Gọi ResetFocus
                tool.ResetFocus();

                // Xác minh các TabItem đã trở lại Visible
                if (tabControl != null)
                {
                    for (int i = 0; i < tabControl.Items.Count; i++)
                    {
                        var tabItem = tabControl.Items[i] as TabItem;
                        if (tabItem != null)
                        {
                            Assert.Equal(Visibility.Visible, tabItem.Visibility);
                        }
                    }
                }
            });
        }
    }
}
