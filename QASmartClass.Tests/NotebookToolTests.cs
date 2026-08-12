using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using Xunit;
using QASmartClass.LearningTools.Views.Multi;

namespace QASmartClass.Tests
{
    public class NotebookToolTests
    {
        private void RunTest(Action action)
        {
            Exception? ex = null;
            var uniqueId = Guid.NewGuid().ToString("N");
            var dbFile = System.IO.Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"smartclass_test_nb_{uniqueId}.db");
            var versionFile = System.IO.Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"db_version_test_nb_{uniqueId}.txt");

            var t = new System.Threading.Thread(() =>
            {
                try
                {
                    QASmartClass.Services.AppPaths.DatabaseFile = dbFile;
                    QASmartClass.Services.AppPaths.DbVersionFile = versionFile;

                    // Reset the static Application instance to prevent cross-thread owner issues in tests
                    try
                    {
                        var appInstanceField = typeof(Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                        if (appInstanceField != null)
                        {
                            appInstanceField.SetValue(null, null);
                        }
                        var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                        if (appCreatedField != null)
                        {
                            appCreatedField.SetValue(null, false);
                        }
                    }
                    catch { }

                    try
                    {
                        var app = new QASmartTouch.App();
                        var resources = app.Resources;
                        bool hasTokens = false;
                        foreach (var dict in resources.MergedDictionaries)
                        {
                            if (dict.Source != null && dict.Source.OriginalString.Contains("DesignTokens.xaml"))
                            {
                                hasTokens = true;
                                break;
                            }
                        }
                        if (!hasTokens)
                        {
                            resources.MergedDictionaries.Add(new ResourceDictionary
                            {
                                Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute)
                            });
                            resources.MergedDictionaries.Add(new ResourceDictionary
                            {
                                Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/Styles.xaml", UriKind.Absolute)
                            });
                        }
                    }
                    catch { }

                    if (Application.Current != null)
                    {
                        lock (Application.Current.Resources)
                        {
                            if (!Application.Current.Resources.Contains("Gray100"))
                            {
                                Application.Current.Resources.Add("Gray100", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray));
                            }
                            if (!Application.Current.Resources.Contains("BrandPrimary"))
                            {
                                Application.Current.Resources.Add("BrandPrimary", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Purple));
                            }
                            if (!Application.Current.Resources.Contains("BrandAccent"))
                            {
                                Application.Current.Resources.Add("BrandAccent", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange));
                            }
                        }
                    }
                    QASmartClass.LearningTools.Views.Multi.NotebookTool.MessageBoxShowHandler = (msg, cap, btn, img) => {
                        System.Console.WriteLine($"[GLOBAL MOCK MSGBOX] {msg}");
                        return MessageBoxResult.OK;
                    };

                    action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
                finally
                {
                    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                    try { if (System.IO.File.Exists(dbFile)) System.IO.File.Delete(dbFile); } catch { }
                    try { if (System.IO.File.Exists(versionFile)) System.IO.File.Delete(versionFile); } catch { }
                    try { if (System.IO.File.Exists(System.IO.Path.ChangeExtension(dbFile, ".key"))) System.IO.File.Delete(System.IO.Path.ChangeExtension(dbFile, ".key")); } catch { }

                    try
                    {
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
                    }
                    catch { }

                    // Reset again after test run to avoid leaking to other test classes
                    try
                    {
                        var appInstanceField = typeof(Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                        if (appInstanceField != null)
                        {
                            appInstanceField.SetValue(null, null);
                        }
                        var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                        if (appCreatedField != null)
                        {
                            appCreatedField.SetValue(null, false);
                        }
                    }
                    catch { }
                }
            });
            t.SetApartmentState(System.Threading.ApartmentState.STA);
            t.Start();
            t.Join();
            if (ex != null)
            {
                throw ex;
            }
        }

        [Fact]
        public void Test_NotebookItem_DefaultProperties()
        {
            RunTest(() =>
            {
                var item = new NotebookItem();
                Assert.NotNull(item.Id);
                Assert.Equal("Sổ tay không tên", item.Title);
                Assert.Equal("Smooth", item.CoverType);
                Assert.Equal(Colors.White, item.PaperColor);
                Assert.Equal(0, item.ThemeIndex);
                Assert.Equal("1200x848", item.PaperSizeTag);
                Assert.Equal(0, item.CurrentPageIndex);
                Assert.NotNull(item.Strokes);
            });
        }

        [Fact]
        public void Test_NotebookTool_Instantiation()
        {
            RunTest(() =>
            {
                var tool = new NotebookTool();
                Assert.NotNull(tool);
                tool.Dispose();
            });
        }

        [Fact]
        public void Test_NotebookTool_AutoSaveOnVisibilityChanged()
        {
            RunTest(() =>
            {
                var tool = new NotebookTool();
                Assert.NotNull(tool);
                tool.Visibility = Visibility.Collapsed;
                tool.Dispose();
            });
        }

        [Fact]
        public void Test_NotebookTool_TextBoxAdaptiveBackground()
        {
            RunTest(() =>
            {
                var tool = new NotebookTool();
                Assert.NotNull(tool);

                var methodMode = typeof(NotebookTool).GetMethod("Mode_Click", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(methodMode);
                
                var btn = new System.Windows.Controls.Button { Tag = "Text" };
                methodMode.Invoke(tool, new object[] { btn, new RoutedEventArgs() });
                
                var methodMouseDown = typeof(NotebookTool).GetMethod("DrawingCanvas_MouseDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(methodMouseDown);
                
                var mouseEvent = new System.Windows.Input.MouseButtonEventArgs(
                    System.Windows.Input.InputManager.Current.PrimaryMouseDevice,
                    0, System.Windows.Input.MouseButton.Left)
                {
                    RoutedEvent = UIElement.MouseDownEvent
                };
                
                var canvas = tool.FindName("DrawingCanvas") as System.Windows.Controls.InkCanvas;
                Assert.NotNull(canvas);
                canvas.Children.Clear();
                
                methodMouseDown.Invoke(tool, new object[] { canvas, mouseEvent });
                
                var tb = canvas.Children.OfType<System.Windows.Controls.TextBox>().FirstOrDefault();
                Assert.NotNull(tb);
                Assert.NotNull(tb.Background);
                
                tool.Dispose();
            });
        }
    }
}
