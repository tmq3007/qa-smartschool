using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.LearningTools.Views.Math;
using Xunit;

namespace QASmartClass.Tests
{
    public class LinearSystemToolTests
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

        private void SetInputsAndSolve(LinearSystemTool tool, string a1, string b1, string c1, string a2, string b2, string c2)
        {
            // Get TextBox fields via reflection or by name
            var txtA1 = (TextBox)tool.FindName("txtA1");
            var txtB1 = (TextBox)tool.FindName("txtB1");
            var txtC1 = (TextBox)tool.FindName("txtC1");
            var txtA2 = (TextBox)tool.FindName("txtA2");
            var txtB2 = (TextBox)tool.FindName("txtB2");
            var txtC2 = (TextBox)tool.FindName("txtC2");

            Assert.NotNull(txtA1);
            Assert.NotNull(txtB1);
            Assert.NotNull(txtC1);
            Assert.NotNull(txtA2);
            Assert.NotNull(txtB2);
            Assert.NotNull(txtC2);

            txtA1.Text = a1;
            txtB1.Text = b1;
            txtC1.Text = c1;
            txtA2.Text = a2;
            txtB2.Text = b2;
            txtC2.Text = c2;

            // Trigger Solve method via reflection
            var solveMethod = typeof(LinearSystemTool).GetMethod("Solve", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(solveMethod);
            solveMethod.Invoke(tool, null);
        }

        [Fact]
        public void Solve_TC001_UniqueSolution()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new LinearSystemTool();
                SetInputsAndSolve(tool, "2", "3", "8", "1", "-1", "1");

                var txtResult = (TextBlock)tool.FindName("txtResult");
                var txtClassify = (TextBlock)tool.FindName("txtClassify");
                Assert.NotNull(txtResult);
                Assert.NotNull(txtClassify);

                Assert.Contains("x = 2.2", txtResult.Text);
                Assert.Contains("y = 1.2", txtResult.Text);
                Assert.Contains("nghiệm duy nhất", txtClassify.Text);
            });
        }

        [Fact]
        public void Solve_TC002_NoSolutionParallel()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new LinearSystemTool();
                SetInputsAndSolve(tool, "1", "2", "3", "2", "4", "5");

                var txtResult = (TextBlock)tool.FindName("txtResult");
                var txtClassify = (TextBlock)tool.FindName("txtClassify");
                Assert.NotNull(txtResult);
                Assert.NotNull(txtClassify);

                Assert.Contains("vô nghiệm", txtResult.Text.ToLower());
                Assert.Contains("song song", txtClassify.Text.ToLower());
            });
        }

        [Fact]
        public void Solve_TC003_InfiniteSolutionsCoincident()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new LinearSystemTool();
                SetInputsAndSolve(tool, "1", "2", "3", "2", "4", "6");

                var txtResult = (TextBlock)tool.FindName("txtResult");
                var txtClassify = (TextBlock)tool.FindName("txtClassify");
                Assert.NotNull(txtResult);
                Assert.NotNull(txtClassify);

                Assert.Contains("vô số nghiệm", txtResult.Text.ToLower());
                Assert.Contains("trùng nhau", txtClassify.Text.ToLower());
            });
        }

        [Fact]
        public void Solve_TC004_DegenerateNoSolution()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new LinearSystemTool();
                SetInputsAndSolve(tool, "0", "0", "3", "0", "0", "4");

                var txtResult = (TextBlock)tool.FindName("txtResult");
                var txtClassify = (TextBlock)tool.FindName("txtClassify");
                Assert.NotNull(txtResult);
                Assert.NotNull(txtClassify);

                Assert.Contains("vô nghiệm", txtResult.Text.ToLower());
                Assert.Contains("phương trình vô lý", txtClassify.Text.ToLower());
            });
        }

        [Fact]
        public void Solve_TC005_DegenerateInfiniteAllZero()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new LinearSystemTool();
                SetInputsAndSolve(tool, "0", "0", "0", "0", "0", "0");

                var txtResult = (TextBlock)tool.FindName("txtResult");
                var txtClassify = (TextBlock)tool.FindName("txtClassify");
                Assert.NotNull(txtResult);
                Assert.NotNull(txtClassify);

                Assert.Contains("vô số nghiệm", txtResult.Text.ToLower());
                Assert.Contains("mọi cặp x, y", txtClassify.Text.ToLower());
            });
        }

        [Fact]
        public void Solve_TC006_DegenerateInfiniteOneZero()
        {
            RunOnStaThread(() =>
            {
                InitializeAppAndResources();
                var tool = new LinearSystemTool();
                SetInputsAndSolve(tool, "0", "0", "0", "2", "3", "8");

                var txtResult = (TextBlock)tool.FindName("txtResult");
                var txtClassify = (TextBlock)tool.FindName("txtClassify");
                Assert.NotNull(txtResult);
                Assert.NotNull(txtClassify);

                Assert.Contains("vô số nghiệm", txtResult.Text.ToLower());
                Assert.Contains("đường thẳng còn lại", txtClassify.Text.ToLower());
            });
        }
    }
}
