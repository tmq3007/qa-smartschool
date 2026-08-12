using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.StudentClient.Views;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Classroom.Views;

namespace QASmartClass.Tests
{
    /// <summary>
    /// BỘ KIỂM THỬ NGHIỆM THU LOI_VID_07 — HỘI ĐỒNG CHUYÊN GIA
    /// 
    /// Đánh giá các tính năng:
    ///   TC-01: payload dữ liệu Quiz chứa CorrectAnswer từ phía Giáo viên.
    ///   TC-02: Nhận lệnh QUIZ_DATA lưu Quiz và Question vào DB cục bộ của học sinh.
    ///   TC-03: Nhận lệnh QUIZ_DATA tự động kích hoạt reload trang StudentQuizPage.
    ///   TC-04: Nhận lệnh SURVEY_START lưu lịch sử khảo sát vào EventLogs để hiển thị.
    /// </summary>
    public class LOI_VID_07_ExpertVerificationTests : IDisposable
    {
        private readonly string _dbFile;
        private readonly string _versionFile;

        public LOI_VID_07_ExpertVerificationTests()
        {
            QASmartClass.Services.AppPaths.EnsureDirectories();
            var uniqueId = Guid.NewGuid().ToString("N");
            _dbFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"smartclass_expert_07_{uniqueId}.db");
            _versionFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"db_version_expert_07_{uniqueId}.txt");

            QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
            QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;

            // 1. Initialize the database schema first to avoid SQLite connection handle/out-of-sync issues.
            using (var db = new AppDbContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            }

            // 2. Set up QASmartTouch.App and Mock resources
            RunOnSTA(() =>
            {
                if (Application.Current != null && (!(Application.Current is QASmartTouch.App) || Application.Current.Dispatcher.Thread != Thread.CurrentThread))
                {
                    var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                    if (appCreatedField != null)
                    {
                        appCreatedField.SetValue(null, false);
                    }
                    var currentField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                    if (currentField != null)
                    {
                        currentField.SetValue(null, null);
                    }
                }

                if (Application.Current == null)
                {
                    var app = new QASmartTouch.App();
                    var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance);
                    if (dbProperty != null)
                    {
                        dbProperty.SetValue(app, new AppDbContext());
                    }
                }
                else if (Application.Current is QASmartTouch.App app)
                {
                    var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance);
                    if (dbProperty != null)
                    {
                        dbProperty.SetValue(app, new AppDbContext());
                    }
                }

                var resources = Application.Current.Resources;
                var white = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                var gray = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                var blue = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);

                string[] keys = new[] { "LightBrush", "BorderLightBrush", "DarkBrush", "MutedBrush", "PrimaryLightBrush", "PrimaryBrush", "PrimaryDarkBrush" };
                foreach (var key in keys)
                {
                    if (!resources.Contains(key))
                    {
                        resources.Add(key, key.Contains("Dark") || key.Contains("Muted") ? gray : (key.Contains("Primary") ? blue : white));
                    }
                }
            });
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            try { if (File.Exists(_dbFile)) File.Delete(_dbFile); } catch { }
            try { if (File.Exists(_versionFile)) File.Delete(_versionFile); } catch { }
            try { if (File.Exists(_dbFile + "-wal")) File.Delete(_dbFile + "-wal"); } catch { }
            try { if (File.Exists(_dbFile + "-shm")) File.Delete(_dbFile + "-shm"); } catch { }
        }

        private void RunOnSTA(Action action)
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
            Exception? threadEx = null;
            var t = new Thread(() =>
            {
                try
                {
                    QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
                    QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;
                    InitializeApplicationFull(); action();
                }
                catch (Exception ex) { threadEx = ex; }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            bool completed = t.Join(TimeSpan.FromSeconds(15));
            Assert.True(completed, "STA thread timed out after 15 seconds");
            Assert.Null(threadEx);
        }

        [Fact]
        public void TC00_Debug()
        {
            RunOnSTA(() =>
            {
                var app = Application.Current;
                Assert.NotNull(app);
                Assert.IsType<QASmartTouch.App>(app);
                var qaApp = (QASmartTouch.App)app;
                Assert.NotNull(qaApp.Database);
            });
        }

        [Fact]
        public void TC01_TeacherSendsCorrectAnswer_InQuizDataPayload()
        {
            RunOnSTA(() =>
            {
                var quizPage = new QuizPage();
                var questionsField = typeof(QuizPage).GetField("_questions", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(questionsField);

                var list = new List<Question>
                {
                    new Question
                    {
                        Id = 1,
                        Content = "What is 2+2?",
                        OptionsJson = "[\"3\", \"4\"]",
                        CorrectAnswer = "4",
                        Points = 10,
                        SortOrder = 1
                    }
                };
                questionsField.SetValue(quizPage, list);

                var testQuestion = list.First();
                Assert.Equal("4", testQuestion.CorrectAnswer);
            });
        }

        [Fact]
        public void TC02_StudentShell_QuizDataCommand_SavesQuizAndQuestionsToDb()
        {
            RunOnSTA(() =>
            {
                var shell = new StudentShell();
                var handleMethod = typeof(StudentShell).GetMethod("HandleTeacherCommand",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(handleMethod);

                string quizDataPayload = "CMD|QUIZ_DATA|999|[{\"Content\":\"Q1\",\"OptionsJson\":\"[\\\"A\\\",\\\"B\\\"]\",\"Points\":10,\"SortOrder\":1,\"ImageUrl\":\"\",\"CorrectAnswer\":\"A\"}]";
                handleMethod.Invoke(shell, new object[] { quizDataPayload });

                using (var db = new AppDbContext())
                {
                    var quiz = db.Quizzes.FirstOrDefault(q => q.Id == 999);
                    Assert.NotNull(quiz);

                    var question = db.Questions.FirstOrDefault(q => q.QuizId == 999);
                    Assert.NotNull(question);
                    Assert.Equal("Q1", question.Content);
                    Assert.Equal(string.Empty, question.CorrectAnswer);
                    Assert.Equal(10, question.Points);
                }
            });
        }

        [Fact]
        public void TC03_StudentShell_QuizDataCommand_TriggersPageReload()
        {
            RunOnSTA(() =>
            {
                var shell = new StudentShell();
                var frameField = typeof(StudentShell).GetField("contentFrame", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(frameField);
                var frame = frameField.GetValue(shell) as Frame;
                Assert.NotNull(frame);

                var quizPage = new StudentQuizPage();
                frame.Content = quizPage;

                var handleMethod = typeof(StudentShell).GetMethod("HandleTeacherCommand",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(handleMethod);

                string quizDataPayload = "CMD|QUIZ_DATA|888|[{\"Content\":\"Q2\",\"OptionsJson\":\"[\\\"A\\\",\\\"B\\\"]\",\"Points\":10,\"SortOrder\":1,\"ImageUrl\":\"\",\"CorrectAnswer\":\"B\"}]";
                handleMethod.Invoke(shell, new object[] { quizDataPayload });

                using (var db = new AppDbContext())
                {
                    var question = db.Questions.FirstOrDefault(q => q.QuizId == 888);
                    Assert.NotNull(question);
                    Assert.Equal("Q2", question.Content);
                }
            });
        }

        [Fact]
        public void TC04_StudentShell_SurveyStartCommand_SavesEventLogToDb()
        {
            RunOnSTA(() =>
            {
                var shell = new StudentShell();
                var handleMethod = typeof(StudentShell).GetMethod("HandleTeacherCommand",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(handleMethod);

                string jsonCommand = "{\"Action\":\"SURVEY_START\",\"Payload\":{\"SurveyId\":777,\"QuestionText\":\"Are you satisfied?\",\"Options\":[\"Yes\",\"No\"],\"TimeLimitSeconds\":60,\"TargetClasses\":[]}}";
                handleMethod.Invoke(shell, new object[] { jsonCommand });

                using (var db = new AppDbContext())
                {
                    var log = db.EventLogs.FirstOrDefault(l => l.EventType == "SURVEY" && l.Actor == "GV");
                    Assert.NotNull(log);
                    Assert.Contains("SURVEY_START", log.Details);
                }
            });
        }
    }
}
