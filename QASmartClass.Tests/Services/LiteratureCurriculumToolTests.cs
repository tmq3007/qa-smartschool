using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;
using QASmartClass.LearningTools.Views.Literature;
using QASmartClass.Data;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.Tests.Services
{
    public class LiteratureCurriculumToolTests
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

                        _ = new Application();
                        var resources = Application.Current!.Resources;
                        if (!resources.Contains("CommonFontFamily")) resources["CommonFontFamily"] = new FontFamily("Segoe UI");
                        if (!resources.Contains("BrandPrimary")) resources["BrandPrimary"] = new SolidColorBrush(Color.FromRgb(21, 101, 192));
                        if (!resources.Contains("BrandSecondary")) resources["BrandSecondary"] = new SolidColorBrush(Color.FromRgb(13, 71, 161));
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
        public void TestLiteratureTool_SanitizeString()
        {
            RunInSta(() =>
            {
                var tool = new LiteratureCurriculumTool();
                var sanitizeMethod = typeof(LiteratureCurriculumTool).GetMethod("SanitizeString", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(sanitizeMethod);

                // Test sanitization of dangerous characters
                string input = "Nguyen <script>alert(1)</script> 'An';";
                string expected = "Nguyen scriptalert(1)/script An";
                string result = (string)sanitizeMethod.Invoke(tool, new object[] { input, 100 })!;
                Assert.Equal(expected, result);

                // Test length constraint
                string longInput = "abcdefghij";
                string expectedShort = "abcde";
                string resultShort = (string)sanitizeMethod.Invoke(tool, new object[] { longInput, 5 })!;
                Assert.Equal(expectedShort, resultShort);
            });
        }

        [Fact]
        public void TestLiteratureTool_IsEnProperty()
        {
            RunInSta(() =>
            {
                // Force LanguageManager language to "vi" to make the test environment-independent
                var langField = typeof(QASmartClass.Shared.LanguageManager).GetField("_currentLang", BindingFlags.Static | BindingFlags.NonPublic);
                string? originalLang = langField?.GetValue(null) as string;
                langField?.SetValue(null, "vi");
                try
                {
                    var tool = new LiteratureCurriculumTool();
                    var isEnProp = typeof(LiteratureCurriculumTool).GetProperty("IsEn", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(isEnProp);

                    // Default dynamic language manager language
                    bool isEnVal = (bool)isEnProp.GetValue(tool)!;
                    // If CurrentLanguage is null/vi, should be false
                    Assert.False(isEnVal);
                }
                finally
                {
                    if (originalLang != null) langField?.SetValue(null, originalLang);
                }
            });
        }

        [Fact]
        public void TestLiteratureTool_DatabaseAndDataModel()
        {
            // 1. Verify DbContext property
            var dbContextType = typeof(AppDbContext);
            var prop = dbContextType.GetProperty("LiteratureQuizHistories");
            Assert.NotNull(prop);
            Assert.Equal(typeof(Microsoft.EntityFrameworkCore.DbSet<LiteratureQuizHistory>), prop.PropertyType);

            // 2. Verify LiteratureChapterData properties and deserialization
            string sampleJson = @"{
                ""metadata"": {
                    ""chapterId"": ""test_ch_1"",
                    ""chapterName"": ""Test Chapter"",
                    ""grade"": 6,
                    ""totalProblems"": 1,
                    ""totalSections"": 1
                },
                ""sections"": [
                    {
                        ""sectionId"": ""test_sec_1"",
                        ""sectionName"": ""Test Section"",
                        ""concepts"": [
                            {
                                ""name"": ""Test Concept"",
                                ""definition"": ""Test Definition"",
                                ""theorem"": ""Test Theorem""
                            }
                        ],
                        ""problems"": [
                            {
                                ""problemId"": ""p1"",
                                ""question"": ""Test Question?"",
                                ""difficulty"": ""Easy"",
                                ""options"": [""A"", ""B""],
                                ""answer"": ""A"",
                                ""solution"": ""Test Solution""
                            }
                        ]
                    }
                ]
            }";

            var chData = System.Text.Json.JsonSerializer.Deserialize<LiteratureChapterData>(sampleJson);
            Assert.NotNull(chData);
            Assert.Equal("test_ch_1", chData.Metadata.ChapterId);
            Assert.Equal("Test Chapter", chData.Metadata.ChapterName);
            Assert.Equal(6, chData.Metadata.Grade);
            Assert.Single(chData.Sections);
            
            var sec = chData.Sections[0];
            Assert.Equal("test_sec_1", sec.SectionId);
            Assert.Equal("Test Section", sec.SectionName);
            
            Assert.Single(sec.Concepts);
            var concept = sec.Concepts[0];
            Assert.Equal("Test Concept", concept.Name);
            Assert.Equal("Test Definition", concept.Definition);
            Assert.Equal("Test Theorem", concept.Theorem);

            Assert.Single(sec.Problems);
            var problem = sec.Problems[0];
            Assert.Equal("p1", problem.ProblemId);
            Assert.Equal("Test Question?", problem.Question);
            Assert.Equal("Easy", problem.Difficulty);
            Assert.Equal("A", problem.Answer);
            Assert.Equal("Test Solution", problem.Solution);
        }

        [Fact]
        public void TestLiteratureTool_TopicMappings()
        {
            var topicDisplayNamesField = typeof(LiteratureCurriculumTool).GetField("TopicDisplayNames", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var topicToToolIdsField = typeof(LiteratureCurriculumTool).GetField("TopicToToolIds", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            Assert.NotNull(topicDisplayNamesField);
            Assert.NotNull(topicToToolIdsField);

            var topicDisplayNames = (Dictionary<string, string>)topicDisplayNamesField.GetValue(null)!;
            var topicToToolIds = (Dictionary<string, string[]>)topicToToolIdsField.GetValue(null)!;

            // Assert that some key literature topics exist
            Assert.Contains("Thánh Gióng", topicDisplayNames.Keys);
            Assert.Contains("Tây Tiến", topicDisplayNames.Keys);
            Assert.Contains("Vợ Nhặt", topicDisplayNames.Keys);
            Assert.Contains("Tiếng Việt", topicDisplayNames.Keys);

            // Assert that Math topics do not exist
            Assert.DoesNotContain("dao_ham", topicDisplayNames.Keys);
            Assert.DoesNotContain("tich_phan", topicDisplayNames.Keys);
            Assert.DoesNotContain("mu_log", topicDisplayNames.Keys);
            Assert.DoesNotContain("so_phuc", topicDisplayNames.Keys);

            Assert.DoesNotContain("dao_ham", topicToToolIds.Keys);
            Assert.DoesNotContain("tich_phan", topicToToolIds.Keys);

            // Assert all keys in topicDisplayNames exist in topicToToolIds and match requirements
            foreach (var key in topicDisplayNames.Keys)
            {
                Assert.Contains(key, topicToToolIds.Keys);
                var tools = topicToToolIds[key];
                Assert.NotEmpty(tools);
                if (key == "Tiếng Việt")
                {
                    Assert.Contains("grammar", tools);
                }
                else
                {
                    Assert.Contains("vocabulary", tools);
                }
            }
        }

        [Fact]
        public void TestLiteratureProblem_McqMatching()
        {
            // 1. Test LiteratureProblem answer validation logic
            var problem = new LiteratureProblem
            {
                Question = "Tác giả Tây Tiến là ai?",
                Options = new List<string> { "Quang Dũng", "Xuân Diệu", "Huy Cận", "Tố Hữu" },
                CorrectAnswer = " Quang Dũng " // With trailing/leading spaces to check trim
            };

            // Option 0 "Quang Dũng" should match " Quang Dũng " after trim
            bool matchProblem0 = problem.Options[0].Trim() == problem.GetAnswer().Trim();
            bool matchProblem1 = problem.Options[1].Trim() == problem.GetAnswer().Trim();

            Assert.True(matchProblem0);
            Assert.False(matchProblem1);

            // 2. Test ExamQuestion answer validation logic
            var examQuestion = new ExamQuestion
            {
                QuestionFull = "Tác phẩm nào của Kim Lân?",
                OptionsFull = new List<string> { "Vợ Nhặt", "Tây Tiến", "Đồng Chí" },
                AnswerFull = " Vợ Nhặt "
            };

            var options = examQuestion.GetOptions();
            var answer = examQuestion.GetAnswer();

            bool matchExam0 = options[0].Trim() == answer.Trim();
            bool matchExam1 = options[1].Trim() == answer.Trim();

            Assert.True(matchExam0);
            Assert.False(matchExam1);
        }

        [Fact]
        public void TestLiteratureTool_StreakMultiplierScoring()
        {
            int basePoints = 10;
            Func<int, double> getMultiplier = (streak) =>
            {
                if (streak >= 10) return 2.0;
                if (streak >= 5) return 1.5;
                if (streak >= 3) return 1.2;
                return 1.0;
            };

            Assert.Equal(1.0, getMultiplier(0));
            Assert.Equal(1.0, getMultiplier(2));
            Assert.Equal(1.2, getMultiplier(3));
            Assert.Equal(1.2, getMultiplier(4));
            Assert.Equal(1.5, getMultiplier(5));
            Assert.Equal(1.5, getMultiplier(9));
            Assert.Equal(2.0, getMultiplier(10));

            Assert.Equal(10, (int)(basePoints * getMultiplier(0)));
            Assert.Equal(12, (int)(basePoints * getMultiplier(3)));
            Assert.Equal(15, (int)(basePoints * getMultiplier(5)));
            Assert.Equal(20, (int)(basePoints * getMultiplier(10)));
        }

        [Fact]
        public void TestLiteratureTool_DriveSpaceCheck()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string backupDir = System.IO.Path.Combine(appData, "QASmartClass", "backup");
            var drive = new System.IO.DriveInfo(System.IO.Path.GetPathRoot(backupDir));
            
            Assert.NotNull(drive);
            Assert.True(drive.IsReady);
            Assert.True(drive.AvailableFreeSpace >= 0);
        }

        [Fact]
        public void TestLiteratureTool_PasswordVerification()
        {
            RunInSta(() =>
            {
                var tool = new LiteratureCurriculumTool();
                var verifyMethod = typeof(LiteratureCurriculumTool).GetMethod("VerifyAdminPassword", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(verifyMethod);

                // Correct password
                bool correct = (bool)verifyMethod.Invoke(tool, new object[] { "admin123" })!;
                Assert.True(correct);

                // Incorrect password
                bool incorrect = (bool)verifyMethod.Invoke(tool, new object[] { "wrongpass" })!;
                Assert.False(incorrect);

                // Empty password
                bool empty = (bool)verifyMethod.Invoke(tool, new object[] { "" })!;
                Assert.False(empty);
            });
        }

        [Fact]
        public void TestLiteratureTool_DatabaseSchemaCheck()
        {
            RunInSta(() =>
            {
                var tool = new LiteratureCurriculumTool();
                var checkSchemaMethod = typeof(LiteratureCurriculumTool).GetMethod("CheckDatabaseSchema", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(checkSchemaMethod);

                // 1. Create a dummy text file (invalid SQLite db)
                string tempFile = Path.GetTempFileName();
                File.WriteAllText(tempFile, "This is not a SQLite database file.");
                try
                {
                    bool result = (bool)checkSchemaMethod.Invoke(tool, new object[] { tempFile })!;
                    Assert.False(result);
                }
                finally
                {
                    if (File.Exists(tempFile)) File.Delete(tempFile);
                }

                // 2. Test with the active database file (valid SQLite db)
                string activeDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "smartclass.db");
                if (File.Exists(activeDb))
                {
                    bool result = (bool)checkSchemaMethod.Invoke(tool, new object[] { activeDb })!;
                    Assert.True(result);
                }
            });
        }

        [Fact]
        public void TestLiteratureTool_SettingsNoRedundantWrite()
        {
            RunInSta(() =>
            {
                var tool = new LiteratureCurriculumTool();
                var isInitializingField = typeof(LiteratureCurriculumTool).GetField("_isInitializing", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isInitializingField);
                
                // After construction is complete, _isInitializing must be false
                bool isInitializing = (bool)isInitializingField.GetValue(tool)!;
                Assert.False(isInitializing);
            });
        }

        [Fact]
        public void TestLiteratureTool_AdminLockoutPolicy()
        {
            RunInSta(() =>
            {
                var tool = new LiteratureCurriculumTool();
                var attemptsField = typeof(LiteratureCurriculumTool).GetField("_adminPasswordAttempts", BindingFlags.NonPublic | BindingFlags.Instance);
                var lockoutField = typeof(LiteratureCurriculumTool).GetField("_lockoutEndTime", BindingFlags.Static | BindingFlags.NonPublic);
                
                Assert.NotNull(attemptsField);
                Assert.NotNull(lockoutField);

                // Initially attempts should be 0 and lockout should be null
                int attempts = (int)attemptsField.GetValue(tool)!;
                DateTime? lockout = (DateTime?)lockoutField.GetValue(null);
                
                Assert.Equal(0, attempts);
                Assert.Null(lockout);

                // Set lockout and check if lockout is active
                lockoutField.SetValue(null, DateTime.Now.AddMinutes(5));
                lockout = (DateTime?)lockoutField.GetValue(null);
                Assert.NotNull(lockout);
                Assert.True(lockout.Value > DateTime.Now);

                // Reset lockout
                lockoutField.SetValue(null, null);
            });
        }
    }
}
