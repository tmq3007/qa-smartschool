using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.StudentClient.Services;
using QASmartClass.StudentClient.Views;
using Xunit;

namespace QASmartClass.Tests
{
    public class StudentDailyWorkflowsTests
    {
        static StudentDailyWorkflowsTests()
        {
            QASmartClass.Services.AppServices.UIService = new MockUserInterfaceService();
            AppPaths.EnsureDirectories();
            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.50.0");
        }

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
        public void TestStudentLoginWorkflow_ProfilePersistsAndLoadsCorrectly()
        {
            RunOnStaThread(() =>
            {
                // Ensure Application.Current is set to a QASmartTouch.App instance
                if (System.Windows.Application.Current == null)
                {
                    try { new QASmartTouch.App(); } catch { }
                }
                else if (!(System.Windows.Application.Current is QASmartTouch.App))
                {
                    // If a standard Application was initialized by other tests, we override/mock App
                    System.Diagnostics.Debug.WriteLine("WPF Application.Current already initialized as standard Application");
                }

                using (var db = new AppDbContext())
                using (var tx = db.Database.BeginTransaction())
                {
                    // Clean up student profile path first
                    var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                    var oldProfileText = File.Exists(profilePath) ? SecureProfileHelper.ReadProfileText(profilePath) : null;

                    try
                    {
                        // Clean up student data
                        var existing = db.Students.FirstOrDefault(s => s.StudentCode == "HS_TEST_WORKFLOW");
                        if (existing != null)
                        {
                            db.Students.Remove(existing);
                            db.SaveChanges();
                        }

                        // Add a test student
                        var student = new Student
                        {
                            StudentCode = "HS_TEST_WORKFLOW",
                            FullName = "Trần Học Sinh Test",
                            PCName = "PC_TEST",
                            IPAddress = "127.0.0.1",
                            IsOnline = true,
                            LastSeen = DateTime.Now
                        };
                        db.Students.Add(student);
                        db.SaveChanges();

                        // Write Mock Profile to simulate login selection
                        var mockProfileJson = "{\"StudentCode\":\"HS_TEST_WORKFLOW\",\"StudentName\":\"Trần Học Sinh Test\",\"TeacherIP\":\"127.0.0.1\",\"RememberMe\":true}";
                        SecureProfileHelper.WriteProfileText(profilePath, mockProfileJson);

                        // Initialize StudentIdentityService and load student
                        var identityService = new StudentIdentityService(db);
                        var (id, code, name) = identityService.GetCurrentStudent();

                        Assert.Equal(student.Id, id);
                        Assert.Equal("HS_TEST_WORKFLOW", code);
                        Assert.Equal("Trần Học Sinh Test", name);
                    }
                    finally
                    {
                        // Restore original profile
                        if (oldProfileText != null)
                        {
                            SecureProfileHelper.WriteProfileText(profilePath, oldProfileText);
                        }
                        else if (File.Exists(profilePath))
                        {
                            File.Delete(profilePath);
                        }
                        tx.Rollback();
                    }
                }
            });
        }

        [Fact]
        public void TestStudentDailyWorkflows_PagesInstantiateWithoutErrors()
        {
            RunOnStaThread(() =>
            {
                // Ensure Application.Current is set to a QASmartTouch.App instance
                QASmartTouch.App? appInstance = null;
                if (System.Windows.Application.Current == null)
                {
                    try { appInstance = new QASmartTouch.App(); } catch { }
                }
                else if (System.Windows.Application.Current is QASmartTouch.App existingApp)
                {
                    appInstance = existingApp;
                }

                if (appInstance != null)
                {
                    // Force Database property to be initialized
                    var dbField = typeof(QASmartTouch.App).GetProperty("Database");
                    if (dbField != null)
                    {
                        dbField.SetValue(appInstance, new AppDbContext());
                    }
                }

                // Inject resources and styles to prevent UI styles crashes in unit test environment
                if (System.Windows.Application.Current != null)
                {
                    var resources = System.Windows.Application.Current.Resources;
                    
                    // Fallback basic colors
                    if (!resources.Contains("Gray100"))
                        resources.Add("Gray100", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)));
                    if (!resources.Contains("BrandAccent"))
                        resources.Add("BrandAccent", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 112, 67)));
                    if (!resources.Contains("BrandPrimary"))
                        resources.Add("BrandPrimary", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 150, 136)));
                    
                    // Merge design token and style dictionaries
                    bool hasTheme = resources.MergedDictionaries.Any(d => d.Source != null && d.Source.OriginalString.Contains("Styles"));
                    if (!hasTheme)
                    {
                        try
                        {
                            var designTokens = new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute) };
                            resources.MergedDictionaries.Add(designTokens);
                        }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error loading DesignTokens: " + ex.Message); }

                        try
                        {
                            var styles = new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/Styles.xaml", UriKind.Absolute) };
                            resources.MergedDictionaries.Add(styles);
                        }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error loading Styles: " + ex.Message); }

                        try
                        {
                            var strings = new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/QASmartClass;component/Localization/Strings_vi.xaml", UriKind.Absolute) };
                            resources.MergedDictionaries.Add(strings);
                        }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Error loading Strings: " + ex.Message); }
                    }
                }

                // Ensure student HS001 exists in the database
                using (var db = new AppDbContext())
                {
                    var existingStudent = db.Students.FirstOrDefault(s => s.StudentCode == "HS001");
                    if (existingStudent == null)
                    {
                        var student = new Student
                        {
                            StudentCode = "HS001",
                            FullName = "Học sinh HS001",
                            PCName = "PC01",
                            IPAddress = "127.0.0.1",
                            IsOnline = true,
                            LastSeen = DateTime.Now
                        };
                        db.Students.Add(student);
                        db.SaveChanges();
                    }
                }

                // Set Current Student to HS001 for pages to initialize
                var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                var mockProfileJson = "{\"StudentCode\":\"HS001\",\"StudentName\":\"Học sinh HS001\"}";
                SecureProfileHelper.WriteProfileText(profilePath, mockProfileJson);

                try
                {
                    // 1. Instantiate Dashboard Page
                    var dashboard = new StudentDashboardPage();
                    Assert.NotNull(dashboard);

                    // 2. Instantiate Guide Page
                    var guide = new StudentGuidePage();
                    Assert.NotNull(guide);

                    // 3. Instantiate Local Whiteboard Page
                    var whiteboard = new StudentLocalWhiteboardPage();
                    Assert.NotNull(whiteboard);

                    // 4. Instantiate Settings Page
                    var settings = new StudentSettingsPage();
                    Assert.NotNull(settings);

                    // 5. Instantiate Quiz Page
                    var quiz = new StudentQuizPage();
                    Assert.NotNull(quiz);

                    // 6. Instantiate Submit Page
                    var submit = new StudentSubmitPage();
                    Assert.NotNull(submit);

                    // 7. Instantiate Survey Page
                    var survey = new StudentSurveyPage();
                    Assert.NotNull(survey);

                    // 8. Instantiate Analytics Page
                    var analytics = new AnalyticsPage();
                    Assert.NotNull(analytics);
                }
                finally
                {
                    if (File.Exists(profilePath))
                    {
                        File.Delete(profilePath);
                    }
                }
            });
        }
    }
}
