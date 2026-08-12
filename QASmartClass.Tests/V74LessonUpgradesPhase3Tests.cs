using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QASmartClass.Classroom.Services;
using QASmartClass.Classroom.Views;

namespace QASmartClass.Tests
{
    public class V74LessonUpgradesPhase3Tests
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
        public void TestLessonWordService_GenerateTemplate_UsesTimesNewRoman()
        {
            // Act
            string filePath = LessonWordService.GenerateTemplate("Toán", "10", "Bài test Phase 3");
            Assert.True(File.Exists(filePath));

            try
            {
                // Assert
                using (var doc = WordprocessingDocument.Open(filePath, false))
                {
                    var body = doc.MainDocumentPart?.Document?.Body;
                    Assert.NotNull(body);

                    // Get all run fonts defined in the document runs
                    var runFontsList = body.Descendants<RunFonts>().ToList();
                    Assert.NotEmpty(runFontsList);

                    foreach (var runFonts in runFontsList)
                    {
                        if (runFonts.Ascii != null)
                        {
                            Assert.Equal("Times New Roman", runFonts.Ascii.Value);
                        }
                        if (runFonts.HighAnsi != null)
                        {
                            Assert.Equal("Times New Roman", runFonts.HighAnsi.Value);
                        }
                    }
                }
            }
            finally
            {
                // Cleanup
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        [Fact]
        public void TestStemToolsPage_BoxPlotCollisionAvoidance_AdjustsOffset()
        {
            RunOnStaThread(() =>
            {
                // Arrange
                var page = new StemToolsPage();
                
                // Set chart type to Box Plot (index 3)
                var cboChartType = page.FindName("cboChartType") as ComboBox;
                Assert.NotNull(cboChartType);
                cboChartType.SelectedIndex = 3;

                // Ensure chkShowValues is checked
                var chkShowValues = page.FindName("chkShowValues") as System.Windows.Controls.CheckBox;
                if (chkShowValues != null)
                {
                    chkShowValues.IsChecked = true;
                }

                // Invoke private DrawChart method using reflection
                var drawChartMethod = typeof(StemToolsPage).GetMethod("DrawChart",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(drawChartMethod);

                var names = new System.Collections.Generic.List<string> { "An", "Bình", "Châu", "Dũng", "Em" };
                var values = new System.Collections.Generic.List<double> { 2.0, 2.1, 5.0, 8.0, 12.0 };

                // Act
                drawChartMethod.Invoke(page, new object[] { names, values });

                // Find chartCanvas and inspect children
                var canvas = page.FindName("chartCanvas") as Canvas;
                Assert.NotNull(canvas);

                var textBlocks = canvas.Children.OfType<TextBlock>().ToList();
                
                var minLabel = textBlocks.FirstOrDefault(tb => tb.Text.StartsWith("Min:"));
                var maxLabel = textBlocks.FirstOrDefault(tb => tb.Text.StartsWith("Max:"));
                var q1Label = textBlocks.FirstOrDefault(tb => tb.Text.StartsWith("Q1:"));
                var q3Label = textBlocks.FirstOrDefault(tb => tb.Text.StartsWith("Q3:"));

                Assert.NotNull(minLabel);
                Assert.NotNull(maxLabel);
                Assert.NotNull(q1Label);
                Assert.NotNull(q3Label);

                double centerY = 280 / 2.0 - 15.0; // 125.0

                // In this dataset:
                // Min = 2.0, Q1 = 2.05 (Difference = 0.05, X distance is very small < 55px) -> minOffset should be 18
                // Max = 12.0, Q3 = 10.0 (Difference = 2.0, X distance is 96px >= 55px) -> maxOffset should be -32

                double minTop = Canvas.GetTop(minLabel);
                double maxTop = Canvas.GetTop(maxLabel);
                double q1Top = Canvas.GetTop(q1Label);
                double q3Top = Canvas.GetTop(q3Label);

                Assert.Equal(centerY + 18.0, minTop);  // 143.0 (shifted down to avoid collision)
                Assert.Equal(centerY - 32.0, maxTop);  // 93.0 (normal top placement)
                Assert.Equal(centerY - 44.0, q1Top);   // 81.0
                Assert.Equal(centerY - 44.0, q3Top);   // 81.0
            });
        }
    }
}
