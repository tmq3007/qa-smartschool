using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using QASmartClass.LearningTools.Views.Science;
using Xunit;

namespace QASmartClass.Tests
{
    public class BoilingFreezingToolTests
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
        public void TestBoilingFreezingTool_Initialization_And_Load()
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
                        app.Resources.MergedDictionaries.Add(new ResourceDictionary 
                        { 
                            Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/Styles.xaml", UriKind.Absolute) 
                        });
                        app.Resources.MergedDictionaries.Add(new ResourceDictionary 
                        { 
                            Source = new Uri("pack://application:,,,/QASmartClass;component/Localization/Strings_vi.xaml", UriKind.Absolute) 
                        });
                    } 
                    catch { }
                }
                else
                {
                    // Ensure the dictionary is merged if application already exists
                    var app = System.Windows.Application.Current;
                    try
                    {
                        app.Resources.MergedDictionaries.Add(new ResourceDictionary 
                        { 
                            Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute) 
                        });
                    }
                    catch { }
                }

                var tool = new BoilingFreezingTool();
                Assert.NotNull(tool);

                // Raise Loaded event
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            });
        }
    }
}
