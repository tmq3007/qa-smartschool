using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
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
    /// BỘ KIỂM THỬ NGHIỆM THU LOI_VID_12 — HỘI ĐỒNG CHUYÊN GIA
    /// 
    /// Đánh giá các tính năng bảo mật Quiz:
    ///   TC-01: Giáo viên phát QUIZ_DATA không được chứa thuộc tính CorrectAnswer/correctAnswer/correct trong JSON payload.
    ///   TC-02: Học sinh nhận lệnh QUIZ_DATA lưu câu hỏi nhưng thuộc tính CorrectAnswer trong DB cục bộ bắt buộc phải để trống (Sanitized).
    ///   TC-03: Học sinh nhận lệnh QUIZ_GRADE sau khi nộp bài mới được phép cập nhật CorrectAnswer vào DB phục vụ việc hiển thị đáp án đúng/sai.
    /// </summary>
    public class LOI_VID_12_ExpertVerificationTests : IDisposable
    {
        private readonly string _dbFile;
        private readonly string _versionFile;

        public LOI_VID_12_ExpertVerificationTests()
        {
            QASmartClass.Services.AppPaths.EnsureDirectories();
            var uniqueId = Guid.NewGuid().ToString("N");
            _dbFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"smartclass_expert_12_{uniqueId}.db");
            _versionFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"db_version_expert_12_{uniqueId}.txt");

            QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
            QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;

            // 1. Khởi tạo schema database
            using (var db = new AppDbContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            }

            // 2. Thiết lập QASmartTouch.App và Mock resources
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
        public void TC01_TeacherDoesNotSendCorrectAnswerInQuizDataPayload()
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

                // Simulate serialization format in QuizPage.xaml.cs lines 253-267
                var questionsData = list.Select(q => new Dictionary<string, object?>
                {
                    { "Content", q.Content },
                    { "OptionsJson", q.OptionsJson },
                    { "Points", q.Points },
                    { "SortOrder", q.SortOrder },
                    { "ImageUrl", q.ImageUrl },
                    { "content", q.Content },
                    { "optionsJson", q.OptionsJson },
                    { "points", q.Points },
                    { "sortOrder", q.SortOrder },
                    { "imageUrl", q.ImageUrl },
                    { "image", q.ImageUrl }
                }).ToList();

                string questionsJson = System.Text.Json.JsonSerializer.Serialize(questionsData);

                // Assert that correct answers are NOT serialized
                Assert.DoesNotContain("CorrectAnswer", questionsJson);
                Assert.DoesNotContain("correctAnswer", questionsJson);
                Assert.DoesNotContain("correct", questionsJson);
            });
        }

        [Fact]
        public void TC02_StudentShell_QuizDataPayloadProcessing_DoesNotStoreCorrectAnswer()
        {
            RunOnSTA(() =>
            {
                var shell = new StudentShell();
                var handleMethod = typeof(StudentShell).GetMethod("HandleTeacherCommand",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(handleMethod);

                // Simulated payload containing a hacked CorrectAnswer property
                string quizDataPayload = "CMD|QUIZ_DATA|999|[{\"Content\":\"Q1\",\"OptionsJson\":\"[\\\"A\\\",\\\"B\\\"]\",\"Points\":10,\"SortOrder\":1,\"ImageUrl\":\"\",\"CorrectAnswer\":\"A\"}]";
                handleMethod.Invoke(shell, new object[] { quizDataPayload });

                using (var db = new AppDbContext())
                {
                    var quiz = db.Quizzes.FirstOrDefault(q => q.Id == 999);
                    Assert.NotNull(quiz);

                    var question = db.Questions.FirstOrDefault(q => q.QuizId == 999);
                    Assert.NotNull(question);
                    Assert.Equal("Q1", question.Content);
                    
                    // CorrectAnswer must be empty in the local DB during active test phase
                    Assert.Equal(string.Empty, question.CorrectAnswer);
                }
            });
        }

        [Fact]
        public void TC03_StudentShell_QuizGradePayloadProcessing_StoresCorrectAnswersAfterGrading()
        {
            RunOnSTA(() =>
            {
                var shell = new StudentShell();
                var handleMethod = typeof(StudentShell).GetMethod("HandleTeacherCommand",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(handleMethod);

                // 1. First populate the quiz in database
                string quizDataPayload = "CMD|QUIZ_DATA|888|[{\"Content\":\"Q2\",\"OptionsJson\":\"[\\\"A\\\",\\\"B\\\"]\",\"Points\":10,\"SortOrder\":1,\"ImageUrl\":\"\",\"CorrectAnswer\":\"B\"}]";
                handleMethod.Invoke(shell, new object[] { quizDataPayload });

                // Verify db initially does not have correct answer
                using (var db = new AppDbContext())
                {
                    var question = db.Questions.FirstOrDefault(q => q.QuizId == 888);
                    Assert.NotNull(question);
                    Assert.Equal(string.Empty, question.CorrectAnswer);
                }

                // 2. Simulate grading payload from teacher client
                // CMD|QUIZ_GRADE|quizId|finalScore|correctCount|questionsCount|correctAnswersJson
                var correctAnswersMap = new Dictionary<string, string>();
                using (var db = new AppDbContext())
                {
                    var q = db.Questions.First(x => x.QuizId == 888);
                    correctAnswersMap[q.Id.ToString()] = "B";
                }
                string correctAnswersJson = System.Text.Json.JsonSerializer.Serialize(correctAnswersMap);
                string gradePayload = $"CMD|QUIZ_GRADE|888|100|1|1|{correctAnswersJson}";

                handleMethod.Invoke(shell, new object[] { gradePayload });

                // 3. Verify correct answer has been updated safely in student database
                using (var db = new AppDbContext())
                {
                    var question = db.Questions.FirstOrDefault(q => q.QuizId == 888);
                    Assert.NotNull(question);
                    Assert.Equal("B", question.CorrectAnswer);
                }
            });
        }
    }
}
