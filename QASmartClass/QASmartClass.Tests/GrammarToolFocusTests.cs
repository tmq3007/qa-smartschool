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
                    catch { }
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
                    catch { }
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
