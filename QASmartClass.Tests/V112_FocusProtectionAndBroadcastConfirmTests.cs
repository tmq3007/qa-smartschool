using Xunit;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using QASmartClass.Classroom.Views;
using QASmartClass.StudentClient.Views;
using QASmartClass.Services;
using System.Linq;

namespace QASmartClass.Tests
{
    public class V112_FocusProtectionAndBroadcastConfirmTests
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

        private void EnsureApplication()
        {
            if (System.Windows.Application.Current == null)
            {
                try
                {
                    var app = new QASmartTouch.App();
                    if (!app.Resources.Contains("FolderIcon"))
                    {
                        app.Resources["FolderIcon"] = System.Windows.Media.Geometry.Parse("M 0 0");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[V112Tests] App creation error: {ex.Message}");
                }
            }
            else
            {
                var app = System.Windows.Application.Current;
                if (!app.Resources.Contains("FolderIcon"))
                {
                    app.Resources["FolderIcon"] = System.Windows.Media.Geometry.Parse("M 0 0");
                }
            }
        }

        [Fact]
        public void Test_StudentDismissedBroadcast_FlagFlow()
        {
            RunOnStaThread(() =>
            {
                EnsureApplication();
                var shell = new StudentShell();
                Assert.NotNull(shell);

                // Reflection to verify and test _userDismissedBroadcast field
                var field = typeof(StudentShell).GetField("_userDismissedBroadcast", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                Assert.NotNull(field);
                
                // Default value should be false
                bool defaultValue = (bool)field.GetValue(shell)!;
                Assert.False(defaultValue);

                // Set value to true
                field.SetValue(shell, true);
                bool updatedValue = (bool)field.GetValue(shell)!;
                Assert.True(updatedValue);
            });
        }

        [Fact]
        public void Test_BroadcastPage_ConfirmationLogicStructure()
        {
            RunOnStaThread(() =>
            {
                EnsureApplication();
                var page = new BroadcastPage();
                Assert.NotNull(page);

                // Verify UI elements exist for selections
                var rbAllStudents = page.FindName("rbAllStudents") as RadioButton;
                var rbGroup = page.FindName("rbGroup") as RadioButton;
                var rbIndividual = page.FindName("rbIndividual") as RadioButton;

                Assert.NotNull(rbAllStudents);
                Assert.NotNull(rbGroup);
                Assert.NotNull(rbIndividual);
            });
        }
    }
}
