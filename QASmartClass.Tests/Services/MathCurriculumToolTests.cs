using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;
using QASmartClass.LearningTools;
using QASmartClass.LearningTools.Views.Math;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.Tests.Services
{
    public class MathCurriculumToolTests
    {
        private void RunInSta(Action action)
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
            Exception? exception = null;
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    lock (typeof(System.Windows.Application))
                    {
                        try
                        {
                            var appField = typeof(Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                            var createdField = typeof(Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                            if (appField != null) appField.SetValue(null, null);
                            if (createdField != null) createdField.SetValue(null, false);
                        }
                        catch {}

                        var app = new Application();
                        var resources = app.Resources;
                        if (!resources.Contains("CommonFontFamily")) resources["CommonFontFamily"] = new FontFamily("Segoe UI");
                        if (!resources.Contains("BrandPrimary")) resources["BrandPrimary"] = new SolidColorBrush(Color.FromRgb(21, 101, 192));
                        if (!resources.Contains("BrandSecondary")) resources["BrandSecondary"] = new SolidColorBrush(Color.FromRgb(13, 71, 161));
                        if (!resources.Contains("BrandPrimaryColor")) resources["BrandPrimaryColor"] = Color.FromRgb(21, 101, 192);
                        if (!resources.Contains("BrandSecondaryColor")) resources["BrandSecondaryColor"] = Color.FromRgb(13, 71, 161);
                        if (!resources.Contains("BrandAccent")) resources["BrandAccent"] = new SolidColorBrush(Color.FromRgb(230, 81, 0));
                        if (!resources.Contains("Gray50")) resources["Gray50"] = new SolidColorBrush(Color.FromRgb(250, 250, 250));
                        if (!resources.Contains("Gray100")) resources["Gray100"] = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                        if (!resources.Contains("Gray200")) resources["Gray200"] = new SolidColorBrush(Color.FromRgb(238, 238, 238));
                    }
                    InitializeApplicationFull(); action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    try
                    {
                        var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                        if (appCreatedField != null) appCreatedField.SetValue(null, false);
                        var currentField = typeof(Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                        if (currentField != null) currentField.SetValue(null, null);
                    }
                    catch {}
                }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (exception != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
            }
        }

        [Fact]
        public void TestMathTool_LaTeXRenderMixedContent()
        {
            RunInSta(() =>
            {
                // Test 1: Plain Text
                var plainEl = UI.RenderMixedContent("Đây là văn bản thường", 14);
                Assert.IsType<TextBlock>(plainEl);
                Assert.Equal("Đây là văn bản thường", ((TextBlock)plainEl).Text);

                // Test 2: Mixed LaTeX
                var mixedEl = UI.RenderMixedContent("Cho phương trình $x^2 = 4$ để giải", 14);
                Assert.IsType<WrapPanel>(mixedEl);
                var wp = (WrapPanel)mixedEl;
                Assert.Equal(3, wp.Children.Count);
                Assert.IsType<TextBlock>(wp.Children[0]);
                Assert.Equal("Cho phương trình ", ((TextBlock)wp.Children[0]).Text);
                Assert.IsType<WpfMath.Controls.FormulaControl>(wp.Children[1]);
                Assert.Equal("x^2 = 4", ((WpfMath.Controls.FormulaControl)wp.Children[1]).Formula);
                Assert.IsType<TextBlock>(wp.Children[2]);
                Assert.Equal(" để giải", ((TextBlock)wp.Children[2]).Text);

                // Test 3: Pure LaTeX
                var pureEl = UI.RenderMixedContent("\\int_{a}^{b} f(x) dx", 14);
                Assert.IsType<WpfMath.Controls.FormulaControl>(pureEl);
                Assert.Equal("\\int_{a}^{b} f(x) dx", ((WpfMath.Controls.FormulaControl)pureEl).Formula);
            });
        }

        [Fact]
        public void TestMathTool_LeaderboardParsing()
        {
            RunInSta(() =>
            {
                var tool = new MathCurriculumTool();
                
                // Trigger student network message for leaderboard
                var handlerMethod = typeof(MathCurriculumTool).GetMethod("OnStudentNetworkMessage", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(handlerMethod);

                // Create dummy StudentMessageEventArgs
                var argsType = typeof(QASmartClass.Classroom.Services.NetworkDiscoveryService)
                    .Assembly.GetType("QASmartClass.Classroom.Services.StudentMessageEventArgs");
                Assert.NotNull(argsType);
                
                var args = Activator.CreateInstance(argsType)!;
                argsType.GetProperty("StudentCode")!.SetValue(args, "HS001");
                argsType.GetProperty("Message")!.SetValue(args, "QUIZ_PROGRESS|HS001|Nguyễn Văn An|80|8|10|5");

                // Invoke handler
                handlerMethod.Invoke(tool, new object[] { null!, args });

                // Inspect private studentScores dictionary
                var scoresField = typeof(MathCurriculumTool).GetField("_studentScores", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(scoresField);
                
                var scoresDict = (System.Collections.IDictionary)scoresField.GetValue(tool)!;
                Assert.True(scoresDict.Contains("HS001"));
                
                var progress = scoresDict["HS001"]!;
                Assert.Equal("Nguyễn Văn An", progress.GetType().GetProperty("Name")!.GetValue(progress));
                Assert.Equal(80, progress.GetType().GetProperty("Score")!.GetValue(progress));
                Assert.Equal(5, progress.GetType().GetProperty("Streak")!.GetValue(progress));
            });
        }
    }
}
