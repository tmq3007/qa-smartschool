using Xunit;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Reflection;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Linq;
using QASmartClass.LearningTools.Views.Thinking;
using QASmartClass.LearningTools.Helpers;

namespace QASmartClass.Tests
{
    public class V93ThinkingToolsPhase3Tests
    {
        private void RunOnStaThread(Action action)
        {
            Exception ex = null;
            var uniqueId = Guid.NewGuid().ToString("N");
            var dbFile = System.IO.Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"smartclass_test_v39_{uniqueId}.db");
            var versionFile = System.IO.Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"db_version_test_v39_{uniqueId}.txt");

            var t = new Thread(() =>
            {
                try
                {
                    QASmartClass.Services.AppPaths.DatabaseFile = dbFile;
                    QASmartClass.Services.AppPaths.DbVersionFile = versionFile;

                    // Reset the static Application instance to prevent cross-thread owner issues in tests
                    try
                    {
                        var appInstanceField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                        if (appInstanceField != null)
                        {
                            appInstanceField.SetValue(null, null);
                        }
                        var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                        if (appCreatedField != null)
                        {
                            appCreatedField.SetValue(null, false);
                        }
                    }
                    catch { }

                    try
                    {
                        var app = new QASmartTouch.App();
                        var resources = app.Resources;
                        bool hasTokens = false;
                        foreach (var dict in resources.MergedDictionaries)
                        {
                            if (dict.Source != null && dict.Source.OriginalString.Contains("DesignTokens.xaml"))
                            {
                                hasTokens = true;
                                break;
                            }
                        }
                        if (!hasTokens)
                        {
                            resources.MergedDictionaries.Add(new ResourceDictionary
                            {
                                Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute)
                            });
                            resources.MergedDictionaries.Add(new ResourceDictionary
                            {
                                Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/Styles.xaml", UriKind.Absolute)
                            });
                        }
                    }
                    catch { }

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
                    QASmartClass.LearningTools.Views.Multi.NotebookTool.MessageBoxShowHandler = (msg, cap, btn, img) => {
                        System.Console.WriteLine($"[GLOBAL MOCK MSGBOX] {msg}");
                        return MessageBoxResult.OK;
                    };

                    action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
                finally
                {
                    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                    try { if (System.IO.File.Exists(dbFile)) System.IO.File.Delete(dbFile); } catch { }
                    try { if (System.IO.File.Exists(versionFile)) System.IO.File.Delete(versionFile); } catch { }
                    try { if (System.IO.File.Exists(System.IO.Path.ChangeExtension(dbFile, ".key"))) System.IO.File.Delete(System.IO.Path.ChangeExtension(dbFile, ".key")); } catch { }

                    try
                    {
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
                    }
                    catch { }

                    // Reset again after test run to avoid leaking to other test classes
                    try
                    {
                        var appInstanceField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                        if (appInstanceField != null)
                        {
                            appInstanceField.SetValue(null, null);
                        }
                        var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                        if (appCreatedField != null)
                        {
                            appCreatedField.SetValue(null, false);
                        }
                    }
                    catch { }
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
        public void Test_IqQuizTool_AdaptiveDifficulty()
        {
            RunOnStaThread(() =>
            {
                var tool = new IqQuizTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                
                var cbo = (ComboBox)tool.FindName("cboIqLevel");
                Assert.NotNull(cbo);
                
                Func<object, string> getAnswer = (qObj) =>
                {
                    var prop = qObj.GetType().GetProperty("Answer", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    return prop != null ? (string)prop.GetValue(qObj) : string.Empty;
                };
                
                // Set level to Medium (1)
                cbo.SelectedIndex = 1;
                
                // Trigger Start Click to init
                var startBtn = (Button)tool.FindName("btnIqStart");
                Assert.NotNull(startBtn);
                startBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                
                // Get private fields for consecutive count using reflection
                var fieldConsecCorrect = typeof(IqQuizTool).GetField("_consecutiveCorrect", BindingFlags.NonPublic | BindingFlags.Instance);
                var fieldConsecWrong = typeof(IqQuizTool).GetField("_consecutiveWrong", BindingFlags.NonPublic | BindingFlags.Instance);
                var fieldActive = typeof(IqQuizTool).GetField("_active", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(fieldConsecCorrect);
                Assert.NotNull(fieldConsecWrong);
                Assert.NotNull(fieldActive);
                
                // Verify initial values are 0
                Assert.Equal(0, (int)fieldConsecCorrect.GetValue(tool));
                Assert.Equal(0, (int)fieldConsecWrong.GetValue(tool));

                // Answer correctly 3 times: call CheckMcAnswer directly
                var methodCheckMcAnswer = typeof(IqQuizTool).GetMethod("CheckMcAnswer", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(methodCheckMcAnswer);
                
                // Get correct answer for the current question
                var questionsField = typeof(IqQuizTool).GetField("_questions", BindingFlags.NonPublic | BindingFlags.Instance);
                var idxField = typeof(IqQuizTool).GetField("_idx", BindingFlags.NonPublic | BindingFlags.Instance);
                var questions = (System.Collections.IList)questionsField.GetValue(tool);
                int idx = (int)idxField.GetValue(tool);
                string correctAnswer = getAnswer(questions[idx]);
                
                // 1st Correct answer
                fieldActive.SetValue(tool, true);
                methodCheckMcAnswer.Invoke(tool, new object[] { correctAnswer, correctAnswer });
                Assert.Equal(1, (int)fieldConsecCorrect.GetValue(tool));
                
                // 2nd Correct answer
                idx = (int)idxField.GetValue(tool);
                if (idx >= questions.Count)
                {
                    var showQMethod = typeof(IqQuizTool).GetMethod("ShowIqQ", BindingFlags.NonPublic | BindingFlags.Instance);
                    showQMethod.Invoke(tool, null);
                }
                correctAnswer = getAnswer(questions[idx]);
                fieldActive.SetValue(tool, true);
                methodCheckMcAnswer.Invoke(tool, new object[] { correctAnswer, correctAnswer });
                Assert.Equal(2, (int)fieldConsecCorrect.GetValue(tool));
                
                // 3rd Correct answer -> should increase Index to 2 (Hard)
                idx = (int)idxField.GetValue(tool);
                if (idx >= questions.Count)
                {
                    var showQMethod = typeof(IqQuizTool).GetMethod("ShowIqQ", BindingFlags.NonPublic | BindingFlags.Instance);
                    showQMethod.Invoke(tool, null);
                }
                correctAnswer = getAnswer(questions[idx]);
                fieldActive.SetValue(tool, true);
                methodCheckMcAnswer.Invoke(tool, new object[] { correctAnswer, correctAnswer });
                
                // Level index must have increased to 2 (Hard)
                Assert.Equal(2, cbo.SelectedIndex);
                Assert.Equal(0, (int)fieldConsecCorrect.GetValue(tool)); // reset to 0
                
                // Now answer incorrectly 2 times -> should drop level back to 1 (Medium)
                idx = (int)idxField.GetValue(tool);
                if (idx >= questions.Count)
                {
                    var showQMethod = typeof(IqQuizTool).GetMethod("ShowIqQ", BindingFlags.NonPublic | BindingFlags.Instance);
                    showQMethod.Invoke(tool, null);
                }
                correctAnswer = getAnswer(questions[idx]);
                fieldActive.SetValue(tool, true);
                methodCheckMcAnswer.Invoke(tool, new object[] { "incorrect_answer_val", correctAnswer });
                Assert.Equal(1, (int)fieldConsecWrong.GetValue(tool));
                
                idx = (int)idxField.GetValue(tool);
                if (idx >= questions.Count)
                {
                    var showQMethod = typeof(IqQuizTool).GetMethod("ShowIqQ", BindingFlags.NonPublic | BindingFlags.Instance);
                    showQMethod.Invoke(tool, null);
                }
                correctAnswer = getAnswer(questions[idx]);
                fieldActive.SetValue(tool, true);
                methodCheckMcAnswer.Invoke(tool, new object[] { "incorrect_answer_val", correctAnswer });
                
                // Level index must have decreased back to 1 (Medium)
                Assert.Equal(1, cbo.SelectedIndex);
                Assert.Equal(0, (int)fieldConsecWrong.GetValue(tool)); // reset to 0
            });
        }

        [Fact]
        public void Test_DbManager_InitializeAndSave()
        {
            DbManager.Initialize();
            
            var progress = new UserProgress
            {
                GameName = "Test Game",
                Score = 5,
                Total = 10,
                DurationSeconds = 12.5,
                Difficulty = "Bình thường",
                CreatedAt = DateTime.Now
            };
            
            DbManager.SaveProgress(progress);
            
            // Wait a moment for the background Thread/Task to write
            Thread.Sleep(500);
            
            // Query DB to verify
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "QASmartClass", "smartclass.db");
            
            Assert.True(File.Exists(dbPath));
            
            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                string sql = "SELECT COUNT(*) FROM UserProgress WHERE GameName = 'Test Game' AND Score = 5";
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    long count = (long)cmd.ExecuteScalar();
                    Assert.True(count > 0);
                }
            }
        }

        [Fact]
        public void Test_TtsHelper_SpeakDoesNotThrow()
        {
            var ex = Record.Exception(() =>
            {
                TtsHelper.SpeakEnglish("hello");
                TtsHelper.SpeakEnglish("world");
            });
            Assert.Null(ex);
        }

        [Fact]
        public void Test_IqQuizTool_CorrectOptionsCount()
        {
            var fieldOddOneOut = typeof(IqQuizTool).GetProperty("OddOneOut", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(fieldOddOneOut);
            var list = (System.Collections.IEnumerable)fieldOddOneOut.GetValue(null);
            Assert.NotNull(list);
            
            foreach (var item in list)
            {
                var optionsProp = item.GetType().GetProperty("Options", BindingFlags.Public | BindingFlags.Instance);
                var options = (string[])optionsProp.GetValue(item);
                Assert.NotNull(options);
                Assert.Contains(options.Length, new[] { 4, 5 });
            }
        }

        [Fact]
        public void Test_IqQuizTool_KittyTranslation()
        {
            var fieldAnalogies = typeof(IqQuizTool).GetProperty("Analogies", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(fieldAnalogies);
            var list = (System.Collections.IEnumerable)fieldAnalogies.GetValue(null);
            Assert.NotNull(list);
            
            bool foundMeo = false;
            bool foundMiao = false;
            foreach (var item in list)
            {
                var displayProp = item.GetType().GetProperty("Display", BindingFlags.Public | BindingFlags.Instance);
                string display = (string)displayProp.GetValue(item);
                if (display.Contains("Meo")) foundMeo = true;
                if (display.Contains("Miao")) foundMiao = true;
            }
            Assert.True(foundMeo);
            Assert.False(foundMiao);
        }

        [Fact]
        public void Test_IqQuizTool_DurationSecondsRecorded()
        {
            RunOnStaThread(() =>
            {
                var tool = new IqQuizTool();
                
                var fieldStartTime = typeof(IqQuizTool).GetField("_startTime", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(fieldStartTime);
                fieldStartTime.SetValue(tool, DateTime.Now.AddSeconds(-5));
                
                // Set a unique score to identify this test run's record without deleting database rows
                var fieldCorrect = typeof(IqQuizTool).GetField("_correct", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(fieldCorrect);
                int uniqueScore = new Random().Next(10000, 99999);
                fieldCorrect.SetValue(tool, uniqueScore);
                
                string dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "QASmartClass", "smartclass.db");
                
                var saveMethod = typeof(IqQuizTool).GetMethod("SaveGameProgressToDb", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(saveMethod);
                saveMethod.Invoke(tool, null);
                
                // Use a retry loop to wait for the background thread to write to database
                object val = null;
                for (int i = 0; i < 25; i++)
                {
                    try
                    {
                        using (var conn = new SqliteConnection($"Data Source={dbPath}"))
                        {
                            conn.Open();
                            string sql = "SELECT DurationSeconds FROM UserProgress WHERE GameName = 'Luyện IQ & Logic' AND Score = " + uniqueScore + " LIMIT 1";
                            using (var cmd = new SqliteCommand(sql, conn))
                            {
                                val = cmd.ExecuteScalar();
                            }
                        }
                    }
                    catch { }
                    if (val != null) break;
                    Thread.Sleep(200);
                }
                
                Assert.NotNull(val);
                double duration = Convert.ToDouble(val);
                Assert.True(duration >= 3.0 && duration <= 8.0);
            });
        }

        [Fact]
        public void Test_MemoryGameTool_DynamicTtsSelection()
        {
            RunOnStaThread(() =>
            {
                var tool = new MemoryGameTool();
                var getPairsMethod = typeof(MemoryGameTool).GetMethod("GetPairs", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(getPairsMethod);
                
                var listTheme2 = (System.Collections.IList)getPairsMethod.Invoke(tool, new object[] { 2 });
                Assert.NotNull(listTheme2);
                
                var firstPair = listTheme2[0];
                var qField = firstPair.GetType().GetField("Item1", BindingFlags.Public | BindingFlags.Instance);
                var aField = firstPair.GetType().GetField("Item2", BindingFlags.Public | BindingFlags.Instance);
                Assert.NotNull(qField);
                Assert.NotNull(aField);
                
                string qVal = (string)qField.GetValue(firstPair);
                string aVal = (string)aField.GetValue(firstPair);
                
                bool qMatches = false;
                bool aMatches = false;
                foreach (object p in listTheme2)
                {
                    string pQ = (string)qField.GetValue(p);
                    if (pQ == qVal) qMatches = true;
                    if (pQ == aVal) aMatches = true;
                }
                Assert.True(qMatches);
                Assert.False(aMatches);
                
                var listTheme1 = (System.Collections.IList)getPairsMethod.Invoke(tool, new object[] { 1 });
                Assert.NotNull(listTheme1);
                
                var elementPair = listTheme1[0];
                string elementQ = (string)qField.GetValue(elementPair);
                string elementA = (string)aField.GetValue(elementPair);
                
                bool elementQMatches = false;
                bool elementAMatches = false;
                foreach (object p in listTheme1)
                {
                    string pA = (string)aField.GetValue(p);
                    if (pA == elementQ) elementQMatches = true;
                    if (pA == elementA) elementAMatches = true;
                }
                Assert.False(elementQMatches);
                Assert.True(elementAMatches);
            });
        }

        [Fact]
        public void Test_IqQuizTool_EndlessModeLogic()
        {
            RunOnStaThread(() =>
            {
                var tool = new IqQuizTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var cboMode = (ComboBox)tool.FindName("cboIqMode");
                Assert.NotNull(cboMode);
                cboMode.SelectedIndex = 1; // Endless Mode

                var startBtn = (Button)tool.FindName("btnIqStart");
                Assert.NotNull(startBtn);
                startBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                var fieldIsEndless = typeof(IqQuizTool).GetField("_isEndless", BindingFlags.NonPublic | BindingFlags.Instance);
                var fieldLives = typeof(IqQuizTool).GetField("_lives", BindingFlags.NonPublic | BindingFlags.Instance);
                var fieldActive = typeof(IqQuizTool).GetField("_active", BindingFlags.NonPublic | BindingFlags.Instance);
                var txtLives = (TextBlock)tool.FindName("txtIqLives");

                Assert.NotNull(fieldIsEndless);
                Assert.NotNull(fieldLives);
                Assert.NotNull(fieldActive);
                Assert.NotNull(txtLives);

                Assert.True((bool)fieldIsEndless.GetValue(tool));
                Assert.Equal(3, (int)fieldLives.GetValue(tool));
                Assert.Equal("❤️❤️❤️", txtLives.Text);

                var methodCheckMcAnswer = typeof(IqQuizTool).GetMethod("CheckMcAnswer", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(methodCheckMcAnswer);

                // 1st Wrong Answer
                fieldActive.SetValue(tool, true);
                methodCheckMcAnswer.Invoke(tool, new object[] { "wrong_ans", "correct_ans" });
                Assert.Equal(2, (int)fieldLives.GetValue(tool));
                Assert.Equal("❤️❤️🖤", txtLives.Text);

                // 2nd Wrong Answer
                fieldActive.SetValue(tool, true);
                methodCheckMcAnswer.Invoke(tool, new object[] { "wrong_ans", "correct_ans" });
                Assert.Equal(1, (int)fieldLives.GetValue(tool));
                Assert.Equal("❤️🖤🖤", txtLives.Text);

                // 3rd Wrong Answer -> Game Over
                fieldActive.SetValue(tool, true);
                methodCheckMcAnswer.Invoke(tool, new object[] { "wrong_ans", "correct_ans" });
                Assert.Equal(0, (int)fieldLives.GetValue(tool));
                Assert.Equal("🖤🖤🖤", txtLives.Text);

                // Check game over logic inside ShowIqQ
                var showQMethod = typeof(IqQuizTool).GetMethod("ShowIqQ", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(showQMethod);
                showQMethod.Invoke(tool, null);

                Assert.False((bool)fieldActive.GetValue(tool));
            });
        }

        [Fact]
        public void Test_IqQuizTool_DbSchemaUpgrade()
        {
            DbManager.Initialize();

            int randScore = new Random().Next(20000, 30000);
            var progress = new UserProgress
            {
                GameName = "Luyện IQ & Logic Test Endless",
                Score = randScore,
                Total = 25,
                DurationSeconds = 15.0,
                Difficulty = "Khó",
                CreatedAt = DateTime.Now,
                IsEndless = 1
            };

            DbManager.SaveProgress(progress);

            // Wait for background DB task
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "QASmartClass", "smartclass.db");
            Assert.True(File.Exists(dbPath));

            object valEndless = null;
            for (int i = 0; i < 25; i++)
            {
                try
                {
                    using (var conn = new SqliteConnection($"Data Source={dbPath}"))
                    {
                        conn.Open();
                        string sql = "SELECT IsEndless FROM UserProgress WHERE GameName = 'Luyện IQ & Logic Test Endless' AND Score = " + randScore + " LIMIT 1";
                        using (var cmd = new SqliteCommand(sql, conn))
                        {
                            valEndless = cmd.ExecuteScalar();
                        }
                    }
                }
                catch { }
                if (valEndless != null) break;
                Thread.Sleep(200);
            }

            Assert.NotNull(valEndless);
            Assert.Equal(1L, Convert.ToInt64(valEndless));
        }

        [Fact]
        public void Test_IqQuizTool_LevelHistoryTracking()
        {
            RunOnStaThread(() =>
            {
                var tool = new IqQuizTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var startBtn = (Button)tool.FindName("btnIqStart");
                Assert.NotNull(startBtn);
                startBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                var fieldLevelHistory = typeof(IqQuizTool).GetField("_levelHistory", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(fieldLevelHistory);

                var list = (System.Collections.IList)fieldLevelHistory.GetValue(tool);
                Assert.NotNull(list);
                Assert.True(list.Count > 0);
            });
        }

        [Fact]
        public void Test_IqQuizTool_ToastAnimationDoesNotThrow()
        {
            RunOnStaThread(() =>
            {
                var tool = new IqQuizTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var showToastMethod = typeof(IqQuizTool).GetMethod("ShowToastNotification", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(showToastMethod);

                var ex = Record.Exception(() =>
                {
                    showToastMethod.Invoke(tool, new object[] { "📈", "Tăng độ khó!", true });
                    showToastMethod.Invoke(tool, new object[] { "📉", "Giảm độ khó!", false });
                });
                Assert.Null(ex);
            });
        }

        [Fact]
        public void Test_IqQuizTool_LocalLeaderboardQuery()
        {
            RunOnStaThread(() =>
            {
                DbManager.Initialize();
                
                int maxDbScore = 0;
                try
                {
                    var existing = DbManager.GetTopProgress("Luyện IQ & Logic", 1);
                    if (existing != null && existing.Count > 0)
                    {
                        maxDbScore = existing[0].Score;
                    }
                }
                catch {}
                
                int uniqueScore = System.Math.Max(99999999, maxDbScore) + new Random().Next(1000, 9000);
                var progress = new UserProgress
                {
                    GameName = "Luyện IQ & Logic",
                    Score = uniqueScore,
                    Total = 15,
                    DurationSeconds = 8.5,
                    Difficulty = "Chuyên gia",
                    CreatedAt = DateTime.Now
                };
                
                DbManager.SaveProgress(progress);
                
                System.Collections.Generic.List<UserProgress> list = null;
                for (int i = 0; i < 25; i++)
                {
                    list = DbManager.GetTopProgress("Luyện IQ & Logic", 5);
                    if (list != null && list.Exists(x => x.Score == uniqueScore))
                    {
                        break;
                    }
                    Thread.Sleep(200);
                }

                Assert.NotNull(list);
                Assert.True(list.Count > 0);
                Assert.Contains(list, x => x.Score == uniqueScore);

                var tool = new IqQuizTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var loadTopScoresMethod = typeof(IqQuizTool).GetMethod("LoadTopScores", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(loadTopScoresMethod);

                var ex = Record.Exception(() =>
                {
                    loadTopScoresMethod.Invoke(tool, null);
                });
                Assert.Null(ex);

                var lsvLeaderboard = (ListView)tool.FindName("lsvIqLeaderboard");
                Assert.NotNull(lsvLeaderboard);
                Assert.NotNull(lsvLeaderboard.ItemsSource);
            });
        }

        [Fact]
        public void Test_IqQuizTool_PdfExport_CreatesFile()
        {
            RunOnStaThread(() =>
            {
                DbManager.Initialize();

                var tool = new IqQuizTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                string tempDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QASmartClass", "Temp");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

                string testPng = Path.Combine(tempDir, "chart_temp.png");

                var savePngMethod = typeof(IqQuizTool).GetMethod("SaveVisualToPng", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(savePngMethod);

                var border = (Border)tool.FindName("brdIqChart");
                Assert.NotNull(border);

                var exPng = Record.Exception(() =>
                {
                    savePngMethod.Invoke(tool, new object[] { border, testPng });
                });
                Assert.Null(exPng);
                Assert.True(File.Exists(testPng));

                if (File.Exists(testPng)) File.Delete(testPng);
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task Test_CloudSync_OfflineFallback()
        {
            var initialPending = DbManager.GetPendingSyncs();
            foreach (var p in initialPending)
            {
                DbManager.DeletePendingSync(p.Id);
            }

            var progress = new UserProgress
            {
                GameName = "Luyện IQ & Logic",
                Score = 5,
                Total = 15,
                DurationSeconds = 12.4,
                Difficulty = "Khó",
                CreatedAt = DateTime.Now,
                IsEndless = 0
            };

            bool result = await CloudSyncService.SyncProgressAsync(progress);
            Assert.False(result);

            var pendingQueue = DbManager.GetPendingSyncs();
            Assert.NotNull(pendingQueue);
            Assert.Contains(pendingQueue, x => x.Score == 5 && x.Difficulty == "Khó");

            var item = pendingQueue.FirstOrDefault(x => x.Score == 5);
            if (item != null)
            {
                DbManager.DeletePendingSync(item.Id);
            }
        }

        [Fact]
        public void Test_IqQuizTool_Chart_SlidingWindow_LimitTo30()
        {
            RunOnStaThread(() =>
            {
                var tool = new IqQuizTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var fieldLevelHistory = typeof(IqQuizTool).GetField("_levelHistory", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(fieldLevelHistory);

                var list = (System.Collections.Generic.List<int>)fieldLevelHistory.GetValue(tool);
                Assert.NotNull(list);
                list.Clear();

                // Mock 35 questions played
                for (int i = 0; i < 35; i++)
                {
                    list.Add(i % 4); // alternate between 0, 1, 2, 3
                }

                // Draw chart
                var drawChartMethod = typeof(IqQuizTool).GetMethod("DrawAdaptiveChart", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(drawChartMethod);

                var cvsChart = (Canvas)tool.FindName("cvsIqChart");
                Assert.NotNull(cvsChart);

                drawChartMethod.Invoke(tool, null);

                // Verify that only 30 dots (Ellipses) are added to the Canvas
                int dotsCount = cvsChart.Children.OfType<System.Windows.Shapes.Ellipse>().Count();
                Assert.Equal(30, dotsCount);

                // Verify X-axis ticks (TextBlocks with text starting with 'C')
                var textBlocks = cvsChart.Children.OfType<TextBlock>().ToList();
                var cTicks = textBlocks.Where(tb => tb.Text.StartsWith("C")).Select(tb => tb.Text).ToList();
                
                // First tick should be C6 (startIndex = 35 - 30 = 5 -> C6)
                // C1 to C5 should not be in the ticks
                Assert.Contains("C6", cTicks);
                Assert.Contains("C33", cTicks);
                Assert.DoesNotContain("C1", cTicks);
                Assert.DoesNotContain("C5", cTicks);
            });
        }

        [Fact]
        public void Test_IsGameTool_ExcludeMentalMath()
        {
            Assert.False(TeachingActionHelper.IsGameTool("iq_quiz"));
            Assert.False(TeachingActionHelper.IsGameTool("mental_math"));
            Assert.False(TeachingActionHelper.IsGameTool("sudoku"));
            Assert.False(TeachingActionHelper.IsGameTool("memory_game"));
            Assert.False(TeachingActionHelper.IsGameTool("any_other_tool"));
        }

        [Fact]
        public void Test_IqQuizTool_Implements_IWhiteboardCaptureProvider()
        {
            RunOnStaThread(() =>
            {
                var tool = new IqQuizTool();
                Assert.True(tool is QASmartClass.LearningTools.Models.IWhiteboardCaptureProvider);
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task Test_SendToWhiteboardAsync_NullControl_OrInvalidSize_ReturnsFalse()
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
            var thread = new Thread(() =>
            {
                try
                {
                    if (Application.Current == null)
                    {
                        try
                        {
                            var app = new QASmartTouch.App();
                            var urls = new[] {
                                "pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml",
                                "pack://application:,,,/QASmartClass;component/Resources/Styles.xaml",
                                "pack://application:,,,/QASmartClass;component/Resources/StaffTheme.xaml",
                                "pack://application:,,,/QASmartClass;component/Resources/InterOutfitFonts.xaml",
                                "pack://application:,,,/QASmartClass;component/Resources/SvgIcons.xaml",
                                "pack://application:,,,/QASmartClass;component/Localization/Strings_vi.xaml",
                                "pack://application:,,,/QASmartClass;component/LearningTools/Themes/LearningToolsStyles.xaml"
                            };
                            foreach (var url in urls)
                            {
                                app.Resources.MergedDictionaries.Add(new ResourceDictionary
                                {
                                    Source = new Uri(url, UriKind.Absolute)
                                });
                            }
                        }
                        catch { }
                    }
                    var tool = new IqQuizTool();
                    tool.Width = 0;
                    tool.Height = 0;
                    var task = TeachingActionHelper.SendToWhiteboardAsync(tool, "Test Iq Quiz");
                    task.ContinueWith(t =>
                    {
                        tcs.SetResult(t.Result);
                    });
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            bool result = await tcs.Task;
            Assert.False(result);
        }

        [Fact]
        public void Test_TeachingActionHelper_FocusUnfocus_UpdatesLessonState()
        {
            RunOnStaThread(() =>
            {
                TeachingActionHelper.IsStudentOverride = false;
                try
                {
                    var state = QASmartClass.Services.LessonStateService.Instance;
                    
                    // Clear state first
                    state.ActiveToolFocusId = null;

                    // Focus a tool
                    TeachingActionHelper.FocusStudents("iq_quiz");
                    Assert.Equal("iq_quiz", state.ActiveToolFocusId);

                    // Unfocus
                    TeachingActionHelper.UnfocusStudents();
                    Assert.Null(state.ActiveToolFocusId);
                }
                finally
                {
                    TeachingActionHelper.IsStudentOverride = null;
                }
            });
        }

        [Fact]
        public void Test_LearningToolsHub_Unloaded_ClearsActiveToolFocusId()
        {
            RunOnStaThread(() =>
            {
                TeachingActionHelper.IsStudentOverride = false;
                try
                {
                    var state = QASmartClass.Services.LessonStateService.Instance;
                    state.ActiveToolFocusId = "iq_quiz";

                    var hub = new QASmartClass.LearningTools.Views.LearningToolsHub();
                    
                    // Raise Unloaded event directly to trigger the handler
                    hub.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

                    Assert.Null(state.ActiveToolFocusId);
                }
                finally
                {
                    TeachingActionHelper.IsStudentOverride = null;
                }
            });
        }

        [Fact]
        public void Test_LearningToolsHub_OpenTool_RestoresFocusButtonState()
        {
            RunOnStaThread(() =>
            {
                TeachingActionHelper.IsStudentOverride = false;
                try
                {
                    var state = QASmartClass.Services.LessonStateService.Instance;
                    state.ActiveToolFocusId = "iq_quiz";

                    var hub = new QASmartClass.LearningTools.Views.LearningToolsHub();
                    
                    // Get the iq_quiz tool definition
                    var tool = QASmartClass.LearningTools.Models.ToolRegistry.GetById("iq_quiz");
                    Assert.NotNull(tool);

                    // Use reflection to call OpenTool(tool)
                    var method = typeof(QASmartClass.LearningTools.Views.LearningToolsHub)
                        .GetMethod("OpenTool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(method);
                    method.Invoke(hub, new object[] { tool });

                    // Verify that teachingActionsPanel has the focus button, and it is disabled
                    var panel = (StackPanel)hub.FindName("teachingActionsPanel");
                    Assert.NotNull(panel);

                    // Find the Focus HS button
                    var focusBtn = panel.Children.OfType<Button>().FirstOrDefault(b => b.Content.ToString() == "✅ Đang Focus");
                    Assert.NotNull(focusBtn);
                    Assert.False(focusBtn.IsEnabled);

                    // Clean up state
                    state.ActiveToolFocusId = null;
                }
                finally
                {
                    TeachingActionHelper.IsStudentOverride = null;
                }
            });
        }

        [Fact]
        public void Test_StudentShell_WatchdogTimer_Initialized()
        {
            RunOnStaThread(() =>
            {
                var shell = new QASmartClass.StudentClient.Views.StudentShell();
                var timerField = typeof(QASmartClass.StudentClient.Views.StudentShell)
                    .GetField("_focusWatchdogTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(timerField);
                var timer = timerField.GetValue(shell) as System.Windows.Threading.DispatcherTimer;
                Assert.NotNull(timer);
                Assert.True(timer.IsEnabled);
                Assert.Equal(TimeSpan.FromSeconds(5), timer.Interval);
                
                // Close the shell to avoid window leaks
                shell.Close();
            });
        }

        [Fact]
        public void Test_IqQuizTool_ScratchPad_UndoRedo()
        {
            RunOnStaThread(() =>
            {
                var tool = new IqQuizTool();
                var undoStackField = typeof(IqQuizTool).GetField("_scratchUndoStack", BindingFlags.NonPublic | BindingFlags.Instance);
                var redoStackField = typeof(IqQuizTool).GetField("_scratchRedoStack", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(undoStackField);
                Assert.NotNull(redoStackField);

                var undoStack = (System.Collections.ICollection)undoStackField.GetValue(tool);
                var redoStack = (System.Collections.ICollection)redoStackField.GetValue(tool);

                // Initial state has 1 item
                Assert.Equal(1, undoStack.Count);
                Assert.Equal(0, redoStack.Count);

                // Add a stroke
                var stroke = new System.Windows.Ink.Stroke(new System.Windows.Input.StylusPointCollection(new[] { new System.Windows.Point(0, 0) }));
                var scratchCanvas = (InkCanvas)tool.FindName("scratchCanvas");
                Assert.NotNull(scratchCanvas);
                scratchCanvas.Strokes.Add(stroke);

                Assert.Equal(2, undoStack.Count);
                Assert.Equal(0, redoStack.Count);

                // Perform Undo
                var performUndo = typeof(IqQuizTool).GetMethod("PerformScratchUndo", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(performUndo);
                performUndo.Invoke(tool, null);

                Assert.Equal(1, undoStack.Count);
                Assert.Equal(1, redoStack.Count);
                Assert.Empty(scratchCanvas.Strokes);

                // Perform Redo
                var performRedo = typeof(IqQuizTool).GetMethod("PerformScratchRedo", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(performRedo);
                performRedo.Invoke(tool, null);

                Assert.Equal(2, undoStack.Count);
                Assert.Equal(0, redoStack.Count);
                Assert.Single(scratchCanvas.Strokes);
            });
        }

        [Fact]
        public void Test_MentalMathTool_ScratchPad_UndoRedo()
        {
            RunOnStaThread(() =>
            {
                var tool = new MentalMathTool();
                var undoStackField = typeof(MentalMathTool).GetField("_scratchUndoStack", BindingFlags.NonPublic | BindingFlags.Instance);
                var redoStackField = typeof(MentalMathTool).GetField("_scratchRedoStack", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(undoStackField);
                Assert.NotNull(redoStackField);

                var undoStack = (System.Collections.ICollection)undoStackField.GetValue(tool);
                var redoStack = (System.Collections.ICollection)redoStackField.GetValue(tool);

                // Initial state has 1 item
                Assert.Equal(1, undoStack.Count);
                Assert.Equal(0, redoStack.Count);

                // Add a stroke
                var stroke = new System.Windows.Ink.Stroke(new System.Windows.Input.StylusPointCollection(new[] { new System.Windows.Point(0, 0) }));
                var scratchCanvas = (InkCanvas)tool.FindName("scratchCanvas");
                Assert.NotNull(scratchCanvas);
                scratchCanvas.Strokes.Add(stroke);

                Assert.Equal(2, undoStack.Count);
                Assert.Equal(0, redoStack.Count);

                // Perform Undo
                var performUndo = typeof(MentalMathTool).GetMethod("PerformScratchUndo", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(performUndo);
                performUndo.Invoke(tool, null);

                Assert.Equal(1, undoStack.Count);
                Assert.Equal(1, redoStack.Count);
                Assert.Empty(scratchCanvas.Strokes);

                // Perform Redo
                var performRedo = typeof(MentalMathTool).GetMethod("PerformScratchRedo", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(performRedo);
                performRedo.Invoke(tool, null);

                Assert.Equal(2, undoStack.Count);
                Assert.Equal(0, redoStack.Count);
                Assert.Single(scratchCanvas.Strokes);
            });
        }

        [Fact]
        public void Test_NotebookTool_DbIndexExists()
        {
            RunOnStaThread(() =>
            {
                var tool = new QASmartClass.LearningTools.Views.Multi.NotebookTool();
                string dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
                Assert.True(File.Exists(dbPath));

                var keyBytes = QASmartClass.Data.DbEncryptionKeyManager.GetOrInitializeKey();
                var hexKey = Convert.ToHexString(keyBytes);
                using (var conn = new SqliteConnection($"Data Source={dbPath};Password={hexKey};Default Timeout=5;"))
                {
                    conn.Open();
                    string sql = "SELECT name FROM sqlite_master WHERE type='index' AND name='idx_notebook_pages';";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        var result = cmd.ExecuteScalar() as string;
                        Assert.Equal("idx_notebook_pages", result);
                    }
                }
            });
        }

        [Fact]
        public void Test_NotebookTool_ResetModeOnPageChange()
        {
            RunOnStaThread(() =>
            {
                var tool = new QASmartClass.LearningTools.Views.Multi.NotebookTool();
                var fieldMode = typeof(QASmartClass.LearningTools.Views.Multi.NotebookTool)
                    .GetField("_currentMode", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(fieldMode);
                
                // Set to Rectangle mode
                fieldMode.SetValue(tool, "Rectangle");
                Assert.Equal("Rectangle", fieldMode.GetValue(tool));

                // Create a temporary mock notebook
                var nbItem = new QASmartClass.LearningTools.Views.Multi.NotebookItem
                {
                    Id = "test_reset_nb",
                    Title = "TestResetNb",
                    CoverColor = System.Windows.Media.Colors.Blue,
                    PaperColor = System.Windows.Media.Colors.White,
                    CoverType = "Smooth",
                    CurrentPageIndex = 0
                };
                
                var fieldCurrentNotebook = typeof(QASmartClass.LearningTools.Views.Multi.NotebookTool)
                    .GetField("_currentNotebook", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(fieldCurrentNotebook);
                fieldCurrentNotebook.SetValue(tool, nbItem);

                // Call LoadPage using reflection
                var methodLoadPage = typeof(QASmartClass.LearningTools.Views.Multi.NotebookTool)
                    .GetMethod("LoadPage", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(methodLoadPage);
                
                // Load page 0 (without saving first to avoid database mocks)
                methodLoadPage.Invoke(tool, new object[] { 0, false });

                // Verify it reset to Ink
                Assert.Equal("Ink", fieldMode.GetValue(tool));
            });
        }

        [Fact]
        public void Test_NotebookTool_ExportFilenameFormat()
        {
            RunOnStaThread(() =>
            {
                var tool = new QASmartClass.LearningTools.Views.Multi.NotebookTool();
                
                tool.Measure(new System.Windows.Size(1280, 800));
                tool.Arrange(new System.Windows.Rect(0, 0, 1280, 800));
                tool.UpdateLayout();

                // Trigger Export_Click
                var exportBtn = (Button)tool.FindName("btnExport");
                    Assert.NotNull(exportBtn);
                    
                    // Create a temporary mock notebook to ensure we can test names
                    var nbItem = new QASmartClass.LearningTools.Views.Multi.NotebookItem
                    {
                        Id = "test_export_nb",
                        Title = "TestExportNb",
                        CoverColor = System.Windows.Media.Colors.Blue,
                        PaperColor = System.Windows.Media.Colors.White,
                        CoverType = "Smooth",
                        CurrentPageIndex = 0
                    };
                    
                    var fieldCurrentNotebook = typeof(QASmartClass.LearningTools.Views.Multi.NotebookTool)
                        .GetField("_currentNotebook", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(fieldCurrentNotebook);
                    fieldCurrentNotebook.SetValue(tool, nbItem);

                    string errorMsg = null;
                    var originalHandler = QASmartClass.LearningTools.Views.Multi.NotebookTool.MessageBoxShowHandler;
                    QASmartClass.LearningTools.Views.Multi.NotebookTool.MessageBoxShowHandler = (msg, cap, btn, img) => {
                        System.Console.WriteLine($"[MOCK MSGBOX] {msg}");
                        if (cap == "Lỗi" || msg.Contains("Lỗi")) {
                            errorMsg = msg;
                        }
                        return MessageBoxResult.OK;
                    };
                    try
                    {
                        // Run the click
                        exportBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    finally
                    {
                        QASmartClass.LearningTools.Views.Multi.NotebookTool.MessageBoxShowHandler = originalHandler;
                    }

                    Assert.Null(errorMsg);

                    // Find the exported file on Desktop
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    var files = Directory.GetFiles(desktopPath, "*_Notebook_TestExportNb_Page_1_*.png");
                    
                    Assert.NotEmpty(files);
                    
                    // Clean up
                    foreach (var file in files)
                    {
                        try { File.Delete(file); } catch { }
                    }
            });
        }

        [Fact]
        public void Test_NotebookTool_AutoSaveOnUnload()
        {
            RunOnStaThread(() =>
            {
                var tool = new QASmartClass.LearningTools.Views.Multi.NotebookTool();
                
                // Set up a mock notebook
                var nbItem = new QASmartClass.LearningTools.Views.Multi.NotebookItem
                {
                    Id = "test_unload_nb",
                    Title = "TestUnloadNb",
                    CoverColor = System.Windows.Media.Colors.Blue,
                    PaperColor = System.Windows.Media.Colors.White,
                    CoverType = "Smooth",
                    CurrentPageIndex = 0
                };
                
                var fieldCurrentNotebook = typeof(QASmartClass.LearningTools.Views.Multi.NotebookTool)
                    .GetField("_currentNotebook", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(fieldCurrentNotebook);
                fieldCurrentNotebook.SetValue(tool, nbItem);

                // Save notebook metadata to DB first to satisfy foreign key constraint on the page save
                var methodSaveMeta = typeof(QASmartClass.LearningTools.Views.Multi.NotebookTool)
                    .GetMethod("SaveNotebookMetadata", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(methodSaveMeta);
                methodSaveMeta.Invoke(tool, new object[] { nbItem });

                // Add a unique stroke to the canvas
                var stroke = new System.Windows.Ink.Stroke(new System.Windows.Input.StylusPointCollection(new[] { new System.Windows.Point(5, 5) }));
                var drawingCanvas = tool.FindName("DrawingCanvas") as InkCanvas;
                Assert.NotNull(drawingCanvas);
                drawingCanvas.Strokes.Add(stroke);

                // Invoke NotebookTool_Unloaded directly to avoid WPF subclassing crash in headless unit tests
                var method = typeof(QASmartClass.LearningTools.Views.Multi.NotebookTool)
                    .GetMethod("NotebookTool_Unloaded", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                var originalHandler = QASmartClass.LearningTools.Views.Multi.NotebookTool.MessageBoxShowHandler;
                string errorMsg = null;
                QASmartClass.LearningTools.Views.Multi.NotebookTool.MessageBoxShowHandler = (msg, cap, btn, img) => {
                    errorMsg = msg;
                    return MessageBoxResult.OK;
                };

                try
                {
                    method.Invoke(tool, new object[] { null, null });
                }
                catch (Exception ex)
                {
                    errorMsg = ex.ToString();
                }
                finally
                {
                    QASmartClass.LearningTools.Views.Multi.NotebookTool.MessageBoxShowHandler = originalHandler;
                }

                Assert.Null(errorMsg);

                // Verify that database page data now contains the strokes we added
                string dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
                Assert.True(File.Exists(dbPath));

                var keyBytes = QASmartClass.Data.DbEncryptionKeyManager.GetOrInitializeKey();
                var hexKey = Convert.ToHexString(keyBytes);
                byte[] strokeBytes = null;
                using (var conn = new SqliteConnection($"Data Source={dbPath};Password={hexKey};Default Timeout=5;"))
                {
                    conn.Open();
                    string sql = "SELECT StrokeData FROM NotebookPages WHERE NotebookId = 'test_unload_nb' AND PageIndex = 0";
                    using (var cmd = new SqliteCommand(sql, conn))
                    {
                        strokeBytes = cmd.ExecuteScalar() as byte[];
                    }
                }
                
                Assert.NotNull(strokeBytes);
                Assert.True(strokeBytes.Length > 0);

                // Clean up database
                using (var conn = new SqliteConnection($"Data Source={dbPath};Password={hexKey};Default Timeout=5;"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM NotebookPages WHERE NotebookId = 'test_unload_nb'; DELETE FROM Notebooks WHERE Id = 'test_unload_nb';";
                        cmd.ExecuteNonQuery();
                    }
                }
            });
        }

        [Fact]
        public void Test_NotebookTool_DarkThemeHeaderContrast()
        {
            RunOnStaThread(() =>
            {
                var tool = new QASmartClass.LearningTools.Views.Multi.NotebookTool();
                
                var methodApplyTheme = typeof(QASmartClass.LearningTools.Views.Multi.NotebookTool)
                    .GetMethod("ApplyTheme", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(methodApplyTheme);

                // Apply Dark Mode (themeIndex = 5)
                methodApplyTheme.Invoke(tool, new object[] { 5 });

                // Find header TextBlock and verify their foreground is a light color
                var lblPaperDate = (TextBlock)tool.FindName("lblPaperDate");
                Assert.NotNull(lblPaperDate);
                var brush = lblPaperDate.Foreground as System.Windows.Media.SolidColorBrush;
                Assert.NotNull(brush);
                Assert.Equal(System.Windows.Media.Color.FromRgb(240, 240, 240), brush.Color);

                // Apply Lined Mode (themeIndex = 0) and verify foreground is reset to dark color
                methodApplyTheme.Invoke(tool, new object[] { 0 });
                brush = lblPaperDate.Foreground as System.Windows.Media.SolidColorBrush;
                Assert.NotNull(brush);
                Assert.Equal(System.Windows.Media.Color.FromRgb(66, 66, 66), brush.Color);
            });
        }

        [Fact]
        public void Test_Scratchpad_UndoAfterClear()
        {
            RunOnStaThread(() =>
            {
                var tool = new IqQuizTool();
                var undoStackField = typeof(IqQuizTool).GetField("_scratchUndoStack", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(undoStackField);
                var undoStack = (System.Collections.ICollection)undoStackField.GetValue(tool);

                // Initial state has 1 item
                Assert.Equal(1, undoStack.Count);

                // Add a stroke
                var stroke = new System.Windows.Ink.Stroke(new System.Windows.Input.StylusPointCollection(new[] { new System.Windows.Point(10, 20) }));
                var scratchCanvas = (InkCanvas)tool.FindName("scratchCanvas");
                Assert.NotNull(scratchCanvas);
                scratchCanvas.Strokes.Add(stroke);
                Assert.Equal(2, undoStack.Count);

                // Trigger Clear click with a mock MessageBoxResult.Yes
                var originalHandler = QASmartClass.LearningTools.Views.Multi.NotebookTool.MessageBoxShowHandler;
                QASmartClass.LearningTools.Views.Multi.NotebookTool.MessageBoxShowHandler = (msg, cap, btn, img) => MessageBoxResult.Yes;
                try
                {
                    tool.BtnClearScratch_Click(null, null);
                }
                finally
                {
                    QASmartClass.LearningTools.Views.Multi.NotebookTool.MessageBoxShowHandler = originalHandler;
                }

                // Verify that canvas is cleared
                Assert.Empty(scratchCanvas.Strokes);
                // Stack should have 3 items (initial, after stroke, and save before clear)
                Assert.Equal(3, undoStack.Count);

                // Perform Undo
                var performUndo = typeof(IqQuizTool).GetMethod("PerformScratchUndo", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(performUndo);
                performUndo.Invoke(tool, null);

                // Strokes must be restored!
                Assert.Single(scratchCanvas.Strokes);
                var restoredStroke = scratchCanvas.Strokes[0];
                Assert.Equal(new System.Windows.Point(10, 20), restoredStroke.StylusPoints[0].ToPoint());
            });
        }

        [Fact]
        public void Test_IqQuizTool_TimerCancelledOnRestart()
        {
            RunOnStaThread(() =>
            {
                var tool = new IqQuizTool();
                var btnStart = (Button)tool.FindName("btnIqStart");
                var cboMode = (ComboBox)tool.FindName("cboIqMode");
                var timerField = typeof(IqQuizTool).GetField("_answerDelayTimer", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(timerField);

                // Set mode to 15-question and click start
                cboMode.SelectedIndex = 0;
                btnStart.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                // Verify game started and ComboBoxes are disabled
                var activeField = typeof(IqQuizTool).GetField("_active", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.True((bool)activeField.GetValue(tool));
                Assert.False(cboMode.IsEnabled);

                // Mock timer initialization
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(2);
                timer.Start();
                timerField.SetValue(tool, timer);

                // Restart the game by clicking "Chơi lại"
                btnStart.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                // Verify timer was stopped and field is reset/cancelled
                var activeTimer = (System.Windows.Threading.DispatcherTimer)timerField.GetValue(tool);
                Assert.Null(activeTimer);
                Assert.False(timer.IsEnabled);
            });
        }

        [Fact]
        public void Test_IqQuizTool_LeaderboardSeparation()
        {
            RunOnStaThread(() =>
            {
                // Clear old DB records for a clean test context using SQLite command
                string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QASmartClass", "smartclass.db");
                string connectionString = $"Data Source={dbPath};Default Timeout=5;";
                DbManager.Initialize();
                try
                {
                    using (var conn = new SqliteConnection(connectionString))
                    {
                        conn.Open();
                        using (var cmd = new SqliteCommand("DELETE FROM UserProgress WHERE GameName = 'Luyện IQ & Logic'", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch { }

                // Save some fake progress
                var pNormal1 = new UserProgress { GameName = "Luyện IQ & Logic", Score = 10, Total = 15, DurationSeconds = 50.5, Difficulty = "Trung bình", CreatedAt = DateTime.Now, IsEndless = 0 };
                var pNormal2 = new UserProgress { GameName = "Luyện IQ & Logic", Score = 12, Total = 15, DurationSeconds = 45.0, Difficulty = "Khó", CreatedAt = DateTime.Now, IsEndless = 0 };
                var pEndless1 = new UserProgress { GameName = "Luyện IQ & Logic", Score = 25, Total = 25, DurationSeconds = 120.0, Difficulty = "Chuyên gia", CreatedAt = DateTime.Now, IsEndless = 1 };

                DbManager.SaveProgress(pNormal1);
                DbManager.SaveProgress(pNormal2);
                DbManager.SaveProgress(pEndless1);

                // Query for Normal mode (IsEndless = 0)
                var normalList = DbManager.GetTopProgress("Luyện IQ & Logic", 5, 0);
                Assert.Equal(2, normalList.Count);
                Assert.All(normalList, item => Assert.Equal(0, item.IsEndless));
                // Verify ordering (Score DESC, DurationSeconds ASC)
                Assert.Equal(12, normalList[0].Score);
                Assert.Equal(10, normalList[1].Score);

                // Query for Endless mode (IsEndless = 1)
                var endlessList = DbManager.GetTopProgress("Luyện IQ & Logic", 5, 1);
                Assert.Single(endlessList);
                Assert.Equal(1, endlessList[0].IsEndless);
                Assert.Equal(25, endlessList[0].Score);
            });
        }

        [Fact]
        public void Test_IqQuizTool_Numpad_InputsText()
        {
            RunOnStaThread(() =>
            {
                var tool = new IqQuizTool();
                tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var txtAnswer = (TextBox)tool.FindName("txtIqAnswer");
                Assert.NotNull(txtAnswer);
                txtAnswer.Text = "";

                // Set _active to true so numpad clicks are processed
                var activeField = typeof(IqQuizTool).GetField("_active", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(activeField);
                activeField.SetValue(tool, true);

                // Find a numpad button, e.g. the button for '7'
                var numpadGrid = (UniformGrid)tool.FindName("grdIqNumpad");
                Assert.NotNull(numpadGrid);

                var btn7 = numpadGrid.Children.OfType<Button>().FirstOrDefault(b => b.Tag?.ToString() == "7");
                Assert.NotNull(btn7);

                // Simulate click on '7'
                btn7.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal("7", txtAnswer.Text);

                // Simulate click on '8'
                var btn8 = numpadGrid.Children.OfType<Button>().FirstOrDefault(b => b.Tag?.ToString() == "8");
                Assert.NotNull(btn8);
                btn8.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal("78", txtAnswer.Text);

                // Simulate backspace
                var btnBs = numpadGrid.Children.OfType<Button>().FirstOrDefault(b => b.Tag?.ToString() == "Backspace");
                Assert.NotNull(btnBs);
                btnBs.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal("7", txtAnswer.Text);
            });
        }

        [Fact]
        public void Test_IqQuizTool_Localization_EnglishQuestions()
        {
            RunOnStaThread(() =>
            {
                // Save current language
                string originalLanguage = QASmartClass.Shared.LanguageManager.CurrentLanguage;
                try
                {
                    // Set language to English
                    QASmartClass.Shared.LanguageManager.SetLanguage("en");

                    var tool = new IqQuizTool();
                    tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                    // Verify dynamic questions return English content
                    var fieldAnalogies = typeof(IqQuizTool).GetProperty("Analogies", BindingFlags.Static | BindingFlags.NonPublic);
                    Assert.NotNull(fieldAnalogies);
                    var analogiesList = (System.Collections.IEnumerable)fieldAnalogies.GetValue(null);
                    Assert.NotNull(analogiesList);

                    // Grab first analogy question
                    var analogiesArray = analogiesList.Cast<object>().ToList();
                    Assert.NotEmpty(analogiesArray);
                    var firstAnalogy = analogiesArray.FirstOrDefault();
                    Assert.NotNull(firstAnalogy);
                    
                    var questionProp = firstAnalogy.GetType().GetProperty("Question", BindingFlags.Public | BindingFlags.Instance);
                    Assert.NotNull(questionProp);
                    string questionText = (string)questionProp.GetValue(firstAnalogy);

                    // Analogy question in English starts with "Analogy:"
                    Assert.StartsWith("Analogy:", questionText);

                    // Set back to default mode and check other titles
                    var titleText = (TextBlock)tool.FindName("txtIqTitle");
                    if (titleText != null)
                    {
                        Assert.Equal("🧠 IQ & Logic Practice", titleText.Text);
                    }
                }
                finally
                {
                    // Restore original language
                    QASmartClass.Shared.LanguageManager.SetLanguage(originalLanguage);
                }
            });
        }

        [Fact]
        public void Test_IqQuizTool_Leaderboard_DifficultyLocalization()
        {
            RunOnStaThread(() =>
            {
                DbManager.Initialize();
                string originalLanguage = QASmartClass.Shared.LanguageManager.CurrentLanguage;
                
                try
                {
                    // Save a test progress with difficulty "Khó"
                    int uniqueScore = new Random().Next(1000000, 9999999);
                    var progress = new UserProgress
                    {
                        GameName = "Luyện IQ & Logic",
                        Score = uniqueScore,
                        Total = 15,
                        DurationSeconds = 12.3,
                        Difficulty = "Khó",
                        CreatedAt = DateTime.Now,
                        IsEndless = 0
                    };
                    DbManager.SaveProgress(progress);

                    // Set language to English
                    QASmartClass.Shared.LanguageManager.SetLanguage("en");

                    var tool = new IqQuizTool();
                    tool.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                    var loadTopScoresMethod = typeof(IqQuizTool).GetMethod("LoadTopScores", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(loadTopScoresMethod);
                    loadTopScoresMethod.Invoke(tool, null);

                    var lsvLeaderboard = (ListView)tool.FindName("lsvIqLeaderboard");
                    Assert.NotNull(lsvLeaderboard);

                    var items = lsvLeaderboard.ItemsSource as System.Collections.IEnumerable;
                    Assert.NotNull(items);

                    var bindItems = items.Cast<object>().ToList();
                    Assert.NotEmpty(bindItems);

                    // Find our unique score item
                    var testItem = bindItems.FirstOrDefault(o =>
                    {
                        var scoreProp = o.GetType().GetProperty("ScoreDisplay", BindingFlags.Public | BindingFlags.Instance);
                        return scoreProp != null && ((string)scoreProp.GetValue(o)).StartsWith(uniqueScore.ToString());
                    });
                    Assert.NotNull(testItem);

                    var diffProp = testItem.GetType().GetProperty("Difficulty", BindingFlags.Public | BindingFlags.Instance);
                    Assert.NotNull(diffProp);
                    string diffVal = (string)diffProp.GetValue(testItem);

                    // Under English, "Khó" should be translated to "Hard"
                    Assert.Equal("Hard", diffVal);
                }
                finally
                {
                    QASmartClass.Shared.LanguageManager.SetLanguage(originalLanguage);
                }
            });
        }

        [Fact]
        public void Test_IqQuizTool_Leaderboard_DateTimeLocalization()
        {
            RunOnStaThread(() =>
            {
                DbManager.Initialize();
                string originalLanguage = QASmartClass.Shared.LanguageManager.CurrentLanguage;
                
                try
                {
                    int uniqueScore = new Random().Next(1000000, 9999999);
                    // Choose a specific test date
                    DateTime testDate = new DateTime(2026, 6, 20, 14, 30, 0);
                    var progress = new UserProgress
                    {
                        GameName = "Luyện IQ & Logic",
                        Score = uniqueScore,
                        Total = 15,
                        DurationSeconds = 15.0,
                        Difficulty = "Dễ",
                        CreatedAt = testDate,
                        IsEndless = 0
                    };
                    DbManager.SaveProgress(progress);

                    // 1. Test English Mode
                    QASmartClass.Shared.LanguageManager.SetLanguage("en");
                    var toolEn = new IqQuizTool();
                    toolEn.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                    var loadTopScoresMethod = typeof(IqQuizTool).GetMethod("LoadTopScores", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(loadTopScoresMethod);
                    loadTopScoresMethod.Invoke(toolEn, null);

                    var lsvEn = (ListView)toolEn.FindName("lsvIqLeaderboard");
                    Assert.NotNull(lsvEn);
                    var itemsEn = (lsvEn.ItemsSource as System.Collections.IEnumerable).Cast<object>().ToList();
                    var itemEn = itemsEn.FirstOrDefault(o =>
                    {
                        var scoreProp = o.GetType().GetProperty("ScoreDisplay", BindingFlags.Public | BindingFlags.Instance);
                        return scoreProp != null && ((string)scoreProp.GetValue(o)).StartsWith(uniqueScore.ToString());
                    });
                    Assert.NotNull(itemEn);
                    var datePropEn = itemEn.GetType().GetProperty("DateDisplay", BindingFlags.Public | BindingFlags.Instance);
                    Assert.NotNull(datePropEn);
                    string dateValEn = (string)datePropEn.GetValue(itemEn);

                    // English format MM/dd/yyyy hh:mm tt -> 06/20/2026 02:30 PM (or 2:30 PM depending on format)
                    Assert.Contains("06/20/2026", dateValEn);
                    Assert.Contains("PM", dateValEn);

                    // 2. Test Vietnamese Mode
                    QASmartClass.Shared.LanguageManager.SetLanguage("vi");
                    var toolVi = new IqQuizTool();
                    toolVi.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                    loadTopScoresMethod.Invoke(toolVi, null);

                    var lsvVi = (ListView)toolVi.FindName("lsvIqLeaderboard");
                    Assert.NotNull(lsvVi);
                    var itemsVi = (lsvVi.ItemsSource as System.Collections.IEnumerable).Cast<object>().ToList();
                    var itemVi = itemsVi.FirstOrDefault(o =>
                    {
                        var scoreProp = o.GetType().GetProperty("ScoreDisplay", BindingFlags.Public | BindingFlags.Instance);
                        return scoreProp != null && ((string)scoreProp.GetValue(o)).StartsWith(uniqueScore.ToString());
                    });
                    Assert.NotNull(itemVi);
                    var datePropVi = itemVi.GetType().GetProperty("DateDisplay", BindingFlags.Public | BindingFlags.Instance);
                    Assert.NotNull(datePropVi);
                    string dateValVi = (string)datePropVi.GetValue(itemVi);

                    // Vietnamese format dd/MM/yyyy HH:mm -> 20/06/2026 14:30
                    Assert.Equal("20/06/2026 14:30", dateValVi);
                }
                finally
                {
                    QASmartClass.Shared.LanguageManager.SetLanguage(originalLanguage);
                }
            });
        }
    }
}
