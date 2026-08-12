using Xunit;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using QASmartClass.LearningTools.Views.Thinking;
using QASmartClass.LearningTools.Helpers;

namespace QASmartClass.Tests
{
    public class V92ThinkingToolsPhase2Tests
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
                    if (Application.Current == null)
                    {
                        try
                        {
                            new Application();
                        }
                        catch { }
                    }
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
        public void Test_GameSettingsManager_SaveAndLoad()
        {
            var originalSettings = GameSettingsManager.Load();
            
            var testSettings = new GameSettings
            {
                IqLevel = 2,
                MentalMathLevel = 3,
                MemoryTheme = 1,
                SudokuSize = 6
            };
            
            GameSettingsManager.Save(testSettings);
            var loadedSettings = GameSettingsManager.Load();
            
            Assert.Equal(2, loadedSettings.IqLevel);
            Assert.Equal(3, loadedSettings.MentalMathLevel);
            Assert.Equal(1, loadedSettings.MemoryTheme);
            Assert.Equal(6, loadedSettings.SudokuSize);
            
            // Restore original settings
            GameSettingsManager.Save(originalSettings);
        }

        [Fact]
        public void Test_SoundHelper_DoesNotThrow()
        {
            // Verify sound playing handles cases smoothly and doesn't crash even if no device
            var ex = Record.Exception(() =>
            {
                SoundHelper.Play(true);
                SoundHelper.Play(false);
            });
            Assert.Null(ex);
        }

        [Fact]
        public void Test_IqQuizTool_SettingsAndConstraints()
        {
            RunOnStaThread(() =>
            {
                // Save specific level setting first
                var originalSettings = GameSettingsManager.Load();
                var testSettings = new GameSettings { IqLevel = 2 };
                GameSettingsManager.Save(testSettings);

                var tool = new IqQuizTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                Assert.NotNull(tool);

                // Verify Level restored from settings
                var cbo = (ComboBox)tool.FindName("cboIqLevel");
                Assert.NotNull(cbo);
                Assert.Equal(2, cbo.SelectedIndex);

                // Verify MaxLength constraint
                var txtAns = (TextBox)tool.FindName("txtIqAnswer");
                Assert.NotNull(txtAns);
                Assert.Equal(30, txtAns.MaxLength);

                // Restore
                GameSettingsManager.Save(originalSettings);
            });
        }

        [Fact]
        public void Test_MentalMathTool_SettingsAndConstraints()
        {
            RunOnStaThread(() =>
            {
                // Save specific level setting first
                var originalSettings = GameSettingsManager.Load();
                var testSettings = new GameSettings { MentalMathLevel = 1 };
                GameSettingsManager.Save(testSettings);

                var tool = new MentalMathTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                Assert.NotNull(tool);

                // Verify Level restored from settings
                var cbo = (ComboBox)tool.FindName("cboLevel");
                Assert.NotNull(cbo);
                Assert.Equal(1, cbo.SelectedIndex);

                // Verify MaxLength constraint
                var txtAns = (TextBox)tool.FindName("txtAnswer");
                Assert.NotNull(txtAns);
                Assert.Equal(6, txtAns.MaxLength);

                // Restore
                GameSettingsManager.Save(originalSettings);
            });
        }

        [Fact]
        public void Test_MemoryGameTool_Settings()
        {
            RunOnStaThread(() =>
            {
                // Save specific level setting first
                var originalSettings = GameSettingsManager.Load();
                var testSettings = new GameSettings { MemoryTheme = 2 };
                GameSettingsManager.Save(testSettings);

                var tool = new MemoryGameTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                Assert.NotNull(tool);

                // Verify Theme restored from settings
                var cbo = (ComboBox)tool.FindName("cboTheme");
                Assert.NotNull(cbo);
                Assert.Equal(2, cbo.SelectedIndex);

                // Restore
                GameSettingsManager.Save(originalSettings);
            });
        }

        [Fact]
        public void Test_SudokuTool_SettingsAndConfetti()
        {
            RunOnStaThread(() =>
            {
                // Save specific size setting first
                var originalSettings = GameSettingsManager.Load();
                var testSettings = new GameSettings { SudokuSize = 6 };
                GameSettingsManager.Save(testSettings);

                var tool = new SudokuTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                Assert.NotNull(tool);

                // Verify size restored from settings
                var rb6 = (RadioButton)tool.FindName("rb6");
                Assert.NotNull(rb6);
                Assert.True(rb6.IsChecked);

                // Verify canvas exists
                var canvas = (Canvas)tool.FindName("confettiCanvas");
                Assert.NotNull(canvas);

                // Trigger confetti and ensure it runs without throwing
                var startConfettiMethod = typeof(SudokuTool).GetMethod("StartConfetti", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(startConfettiMethod);
                
                var ex = Record.Exception(() => startConfettiMethod.Invoke(tool, null));
                Assert.Null(ex);

                // Stop confetti and ensure it works
                var stopConfettiMethod = typeof(SudokuTool).GetMethod("StopConfetti", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(stopConfettiMethod);
                
                ex = Record.Exception(() => stopConfettiMethod.Invoke(tool, null));
                Assert.Null(ex);

                // Restore
                GameSettingsManager.Save(originalSettings);
            });
        }

        [Fact]
        public void Test_SudokuTool_UniqueSolutionAndValidation()
        {
            RunOnStaThread(() =>
            {
                var tool = new SudokuTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                Assert.NotNull(tool);

                // Verify CountSolutions logic via reflection
                var countSolutionsMethod = typeof(SudokuTool).GetMethod("CountSolutions", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(countSolutionsMethod);

                // Create a solved grid (4x4)
                int[,] solved4 = new int[4, 4]
                {
                    { 1, 2, 3, 4 },
                    { 3, 4, 1, 2 },
                    { 2, 1, 4, 3 },
                    { 4, 3, 2, 1 }
                };

                // Full board should have exactly 1 solution
                int solCount1 = (int)countSolutionsMethod.Invoke(tool, new object[] { solved4, 4, 2 });
                Assert.Equal(1, solCount1);

                // Board with an empty cell that can only be filled by 1 number should still have 1 solution
                int[,] partialSolved = (int[,])solved4.Clone();
                partialSolved[0, 0] = 0; // leaves a blank cell that must be 1
                int solCount2 = (int)countSolutionsMethod.Invoke(tool, new object[] { partialSolved, 4, 2 });
                Assert.Equal(1, solCount2);

                // Board with multiple empty cells that can lead to multiple solutions
                int[,] multiEmpty = new int[4, 4]
                {
                    { 0, 0, 3, 4 },
                    { 3, 4, 1, 2 },
                    { 2, 1, 4, 3 },
                    { 4, 3, 0, 0 }
                };
                int solCount3 = (int)countSolutionsMethod.Invoke(tool, new object[] { multiEmpty, 4, 2 });
                Assert.True(solCount3 >= 1);
            });
        }
    }
}
