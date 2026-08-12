using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.Services;
using QASmartClass.Staff.ViewModels;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;
using System.Windows;

namespace QASmartClass.Tests
{
    public class V35IntegrationTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly string _tempVersionPath;

        public V35IntegrationTests()
        {
            string guid = Guid.NewGuid().ToString("N");
            _tempDbPath = Path.Combine(AppPaths.RootDir, $"smartclass_test_{guid}.db");
            _tempVersionPath = Path.Combine(AppPaths.RootDir, $"db_version_{guid}.txt");

            AppPaths.DatabaseFile = _tempDbPath;
            AppPaths.DbVersionFile = _tempVersionPath;
            AppPaths.EnsureDirectories();

            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.33.0");

            RunOnStaThread(() =>
            {
                if (Application.Current != null && (!(Application.Current is QASmartTouch.App) || Application.Current.Dispatcher.Thread != System.Threading.Thread.CurrentThread))
                {
                    var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (appCreatedField != null)
                    {
                        appCreatedField.SetValue(null, false);
                    }
                    var currentField = typeof(Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (currentField != null)
                    {
                        currentField.SetValue(null, null);
                    }
                }

                if (Application.Current == null)
                {
                    var app = new QASmartTouch.App();
                    var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (dbProperty != null)
                    {
                        dbProperty.SetValue(app, new AppDbContext());
                    }
                }
                else if (Application.Current is QASmartTouch.App app)
                {
                    var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (dbProperty != null)
                    {
                        dbProperty.SetValue(app, new AppDbContext());
                    }
                }

                // Register dummy resources to satisfy static resource lookups in pages/windows
                var keys = new[] { "LightBrush", "BorderLightBrush", "DarkBrush", "MutedBrush", "PrimaryLightBrush", "PrimaryBrush", "PrimaryDarkBrush" };
                foreach (var key in keys)
                {
                    if (!System.Windows.Application.Current.Resources.Contains(key))
                    {
                        System.Windows.Application.Current.Resources[key] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                    }
                }
            });

            AppServices.UIService = new MockUserInterfaceService();
        }

        [Fact]
        public async Task TestSQLiteForeignKeys_PreventsOrphanDeletes()
        {
            using (var db = new AppDbContext())
            {
                // 1. Create a classroom first to satisfy foreign keys if needed
                var classroom = new QASmartClass.Data.Classroom
                {
                    Name = "Class 10A V35",
                    ClassCode = "10A_V35",
                    TeacherName = "GV_V35"
                };
                db.Classrooms.Add(classroom);
                
                // 2. Create a roster
                var roster = new ClassRoster
                {
                    ClassName = "10A V35",
                    IsActive = true,
                    Subject = "Toán",
                    SchoolYear = "2025-2026",
                    Semester = "HK1"
                };
                db.ClassRosters.Add(roster);
                db.SaveChanges();

                // 3. Create a student
                var student = new Student
                {
                    StudentCode = "HS_V35_FK",
                    FullName = "Học sinh khóa ngoại",
                    ClassroomId = classroom.Id,
                    ClassName = "10A V35"
                };
                db.Students.Add(student);
                db.SaveChanges();

                // 4. Create an attendance record linked to the student
                var attRecord = new AttendanceRecord
                {
                    StudentId = student.Id,
                    RosterId = roster.Id,
                    Date = DateTime.Today,
                    Status = "Present"
                };
                db.AttendanceRecords.Add(attRecord);
                db.SaveChanges();

                // 5. Try to delete the student (parent) while the attendance record (child) still exists
                db.Students.Remove(student);

                // Assert that DbUpdateException is thrown due to Foreign Key constraint violation
                await Assert.ThrowsAsync<DbUpdateException>(async () =>
                {
                    await db.SaveChangesAsync();
                });
            }
        }

        [Fact]
        public void TestSerilogObjectDestructuring_MasksComplexJson()
        {
            var formatter = new SensitiveDataMaskingFormatter("{Message}");

            // Simulating destructured JSON structures in logs
            string rawLog = "Student details: {\"StudentCode\":\"HS001\",\"WalletBalance\":1500000.00,\"CitizenId\":\"123456789012\",\"RFID\":\"7E4A8F9C\"}";
            var template = new MessageTemplateParser().Parse(rawLog);
            var properties = new System.Collections.Generic.List<LogEventProperty>();
            var logEvent = new LogEvent(
                DateTimeOffset.Now, 
                LogEventLevel.Information, 
                null, 
                template, 
                properties
            );

            using (var writer = new StringWriter())
            {
                formatter.Format(logEvent, writer);
                string result = writer.ToString();

                // Assertions for JSON destructured values
                Assert.Contains("\"CitizenId\":\"XXXXXXXX9012\"", result); // CCCD masked
                Assert.Contains("\"RFID\":\"7E4AXXXX\"", result);         // RFID masked
                Assert.Contains("\"WalletBalance\":XXXXXXX.XX", result);   // Balance masked
            }
        }

        [Fact]
        public void TestAutoRefreshTasks_TimerTickLoadsData()
        {
            using (var vm = new TaskManagementViewModel())
            {
                var field = typeof(TaskManagementViewModel).GetField("_refreshTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(field);
                
                var timer = field.GetValue(vm) as System.Windows.Threading.DispatcherTimer;
                Assert.NotNull(timer);
                Assert.True(timer.IsEnabled);
                Assert.Equal(TimeSpan.FromSeconds(60), timer.Interval);
            }
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
            var t = new System.Threading.Thread(() =>
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
            t.SetApartmentState(System.Threading.ApartmentState.STA);
            t.Start();
            t.Join();
            if (ex != null) throw ex;
        }

        [Fact]
        public void TestStudentQuizPage_ShowQuizResultsReview_PreservesTextBlockContent()
        {
            RunOnStaThread(() =>
            {
                System.Windows.Window? win = null;
                try
                {
                    // 1. Create instance of StudentQuizPage
                    var page = new QASmartClass.StudentClient.Views.StudentQuizPage();

                    // Put inside window and show to build visual tree
                    win = new System.Windows.Window
                    {
                        Content = page,
                        Width = 800,
                        Height = 600
                    };
                    win.Show();

                    // 2. Locate questionsPanel using reflection
                    var questionsPanelField = typeof(QASmartClass.StudentClient.Views.StudentQuizPage)
                        .GetField("questionsPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(questionsPanelField);
                    var panel = questionsPanelField.GetValue(page) as System.Windows.Controls.StackPanel;
                    Assert.NotNull(panel);

                    // 3. Populate panel with a fake question card
                    var card = new System.Windows.Controls.Border();
                    var stack = new System.Windows.Controls.StackPanel();
                    card.Child = stack;

                    var rb = new System.Windows.Controls.RadioButton
                    {
                        GroupName = "Q1",
                        Tag = "A",
                        IsChecked = true // Set to true to simulate student selection
                    };
                    
                    // Set Content as a TextBlock containing some text (similar to RenderLatexToTextBlock output)
                    var tb = new System.Windows.Controls.TextBlock();
                    tb.Inlines.Add(new System.Windows.Documents.Run("  A.  Đáp án trắc nghiệm"));
                    rb.Content = tb;
                    stack.Children.Add(rb);
                    panel.Children.Add(card);

                    // Force visual tree template generation
                    page.Measure(new System.Windows.Size(800, 600));
                    page.Arrange(new System.Windows.Rect(0, 0, 800, 600));

                    // Pump dispatcher to let visual tree materialize
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                        () => { },
                        System.Windows.Threading.DispatcherPriority.Render);

                    // 4. Call ShowQuizResultsReview via reflection
                    var method = typeof(QASmartClass.StudentClient.Views.StudentQuizPage)
                        .GetMethod("ShowQuizResultsReview", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(method);

                    var questions = new System.Collections.Generic.List<QASmartClass.Data.Question>
                    {
                        new QASmartClass.Data.Question
                        {
                            Id = 1,
                            QuestionType = "MCQ",
                            CorrectAnswer = "A",
                            OptionsJson = "[\"A. Đáp án trắc nghiệm\"]"
                        }
                    };

                    // Invoke the review method
                    var findVisualChildrenMethod = typeof(QASmartClass.StudentClient.Views.StudentQuizPage)
                        .GetMethod("FindVisualChildren", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                        ?.MakeGenericMethod(typeof(System.Windows.Controls.RadioButton));
                    Assert.NotNull(findVisualChildrenMethod);
                    var foundRbs = findVisualChildrenMethod.Invoke(null, new object[] { card }) as System.Collections.Generic.IEnumerable<System.Windows.Controls.RadioButton>;
                    Assert.NotNull(foundRbs);
                    var foundRbsList = foundRbs.ToList();

                    string studentAns = "";
                    var rbsFromPage = findVisualChildrenMethod.Invoke(null, new object[] { panel }) as System.Collections.Generic.IEnumerable<System.Windows.Controls.RadioButton>;
                    foreach (var r in rbsFromPage ?? Enumerable.Empty<System.Windows.Controls.RadioButton>())
                    {
                        if (r.GroupName == "Q1" && r.IsChecked == true)
                            studentAns = r.Tag?.ToString() ?? "";
                    }

                    method.Invoke(page, new object[] { questions });

                    // 5. Assert that the RadioButton's content is STILL a TextBlock, not converted to string "System.Windows.Controls.TextBlock"
                    Assert.IsType<System.Windows.Controls.TextBlock>(rb.Content);
                    var resultTb = rb.Content as System.Windows.Controls.TextBlock;
                    Assert.NotNull(resultTb);

                    // The inlines should contain the correct answer run and correctness badge appended
                    Assert.Equal(3, resultTb.Inlines.Count);
                    
                    var inline1 = resultTb.Inlines.ElementAt(0) as System.Windows.Documents.Run;
                    Assert.NotNull(inline1);
                    Assert.Contains("Đáp án trắc nghiệm", inline1.Text);

                    var inline2 = resultTb.Inlines.ElementAt(1) as System.Windows.Documents.Run;
                    Assert.NotNull(inline2);
                    Assert.Contains("Đáp án đúng", inline2.Text);

                    var inline3 = resultTb.Inlines.ElementAt(2) as System.Windows.Documents.Run;
                    Assert.NotNull(inline3);
                    Assert.Contains("Chính xác", inline3.Text);
                }
                finally
                {
                    win?.Close();
                }
            });
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_tempDbPath)) File.Delete(_tempDbPath);
                if (File.Exists(_tempVersionPath)) File.Delete(_tempVersionPath);
            }
            catch { }
        }
    }
}
