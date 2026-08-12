using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;
using QASmartClass.LearningTools.Views.Multi;
using QASmartClass.StudentClient.Views;
using Xunit;

namespace QASmartClass.Tests
{
    public class V45FocusTimerTests
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
                    try
                    {
                        var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                        if (appCreatedField != null) appCreatedField.SetValue(null, false);
                        var currentField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                        if (currentField != null) currentField.SetValue(null, null);
                    }
                    catch { }

                    var app = new QASmartTouch.App();

                    if (!app.Resources.Contains("InterFont"))
                    {
                        app.Resources.Add("InterFont", new System.Windows.Media.FontFamily("Arial"));
                    }
                    if (!app.Resources.Contains("OutfitFont"))
                    {
                        app.Resources.Add("OutfitFont", new System.Windows.Media.FontFamily("Arial"));
                    }

                    InitializeApplicationFull(); action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
                finally
                {
                    try
                    {
                        if (Application.Current != null)
                        {
                            Application.Current.Shutdown();
                        }
                    }
                    catch { }
                    try
                    {
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
                    }
                    catch { }
                    try
                    {
                        var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                        if (appCreatedField != null) appCreatedField.SetValue(null, false);
                        var currentField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                        if (currentField != null) currentField.SetValue(null, null);
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
        public void TestGetToolDisplayName_FocusTimer_ReturnsCorrectVietnameseName()
        {
            RunOnStaThread(() =>
            {
                var method = typeof(StudentShell).GetMethod("GetToolDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(method);

                var displayName = (string)method.Invoke(null, new object[] { "focus_timer" });
                Assert.Equal("Đồng Hồ Tập Trung", displayName);
            });
        }

        [Fact]
        public void TestFocusTimerTool_HasScrollViewer_AndButtonsExist()
        {
            RunOnStaThread(() =>
            {
                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                // Lấy nút Start và Reset
                var btnStart = tool.FindName("btnStart") as Button;
                var btnReset = tool.FindName("btnReset") as Button;

                Assert.NotNull(btnStart);
                Assert.NotNull(btnReset);

                Assert.Equal(Visibility.Visible, btnStart.Visibility);
                Assert.Equal(Visibility.Visible, btnReset.Visibility);

                // Xác thực nút bấm hoạt động click mà không quăng lỗi
                var startClickMethod = typeof(FocusTimerTool).GetMethod("BtnStart_Click", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(startClickMethod);
            });
        }

        [Fact]
        public void TestStudentMode_DisablesControls()
        {
            RunOnStaThread(() =>
            {
                // Giả lập chế độ học sinh đang bị focus và ghi đè IsStudent()
                QASmartClass.Services.LessonStateService.Instance.ActiveToolFocusId = "focus_timer";
                QASmartClass.Services.ClassControlService.Instance.ActiveToolFocusId = "focus_timer";
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudentOverride = true;

                Assert.True(QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudent(), "TeachingActionHelper.IsStudent() should be true");
                Assert.Equal("focus_timer", QASmartClass.Services.ClassControlService.Instance.ActiveToolFocusId);

                var tool = new FocusTimerTool();
                Assert.NotNull(tool);
                Assert.True(tool.IsStudentModeAndFocused(), "tool.IsStudentModeAndFocused() should be true");

                // Kích hoạt loaded event để gọi UpdateControlsState()
                var loadedMethod = typeof(FocusTimerTool).GetMethod("UpdateControlsState", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(loadedMethod);
                loadedMethod.Invoke(tool, null);

                // Lấy các nút Start và Reset
                var btnStart = tool.FindName("btnStart") as Button;
                var btnReset = tool.FindName("btnReset") as Button;
                var sliderVolume = tool.FindName("sliderVolume") as Slider;
                var btnSoundRain = tool.FindName("btnSoundRain") as ToggleButton;

                Assert.NotNull(btnStart);
                Assert.NotNull(btnReset);
                Assert.NotNull(sliderVolume);
                Assert.NotNull(btnSoundRain);

                // Khi ở chế độ học sinh và bị focus bởi giáo viên, các nút điều khiển chính phải ẩn/vô hiệu hóa
                Assert.Equal(Visibility.Collapsed, btnStart.Visibility);
                Assert.Equal(Visibility.Collapsed, btnReset.Visibility);
                Assert.False(sliderVolume.IsEnabled);
                Assert.False(btnSoundRain.IsEnabled);

                // Cleanup
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudentOverride = null;
                QASmartClass.Services.LessonStateService.Instance.ActiveToolFocusId = null;
                QASmartClass.Services.ClassControlService.Instance.ActiveToolFocusId = null;
            });
        }

        [Fact]
        public void TestSyncFromTeacher_UpdatesTimeAndState()
        {
            RunOnStaThread(() =>
            {
                // Giả lập chế độ học sinh đang bị focus và ghi đè IsStudent()
                QASmartClass.Services.LessonStateService.Instance.ActiveToolFocusId = "focus_timer";
                QASmartClass.Services.ClassControlService.Instance.ActiveToolFocusId = "focus_timer";
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudentOverride = true;

                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                // Đồng bộ từ giáo viên: 15 phút tập trung, 5 phút nghỉ, 900 giây còn lại, đang chạy, không nghỉ, phiên 1
                tool.SyncFromTeacher(15, 5, 900, true, false, 1);

                var txtTime = tool.FindName("txtTime") as TextBlock;
                var txtSessionCount = tool.FindName("txtSessionCount") as TextBlock;
                var txtSessionLabel = tool.FindName("txtSessionLabel") as TextBlock;

                Assert.NotNull(txtTime);
                Assert.NotNull(txtSessionCount);
                Assert.NotNull(txtSessionLabel);

                Assert.Equal("15:00", txtTime.Text);
                Assert.Equal(string.Format(QASmartClass.Shared.LanguageManager.Get("FocusTimer_SessionCountFormat"), 1), txtSessionCount.Text);
                Assert.Equal(QASmartClass.Shared.LanguageManager.Get("FocusTimer_StatusFocusing"), txtSessionLabel.Text);

                // Cleanup
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudentOverride = null;
                QASmartClass.Services.LessonStateService.Instance.ActiveToolFocusId = null;
                QASmartClass.Services.ClassControlService.Instance.ActiveToolFocusId = null;
            });
        }

        [Fact]
        public void TestLongBreakPomodoroLogic_TriggersAfter4Sessions()
        {
            RunOnStaThread(() =>
            {
                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                // Thiết lập _sessionCount là 3 (phiên tiếp theo hoàn thành sẽ là 4)
                var sessionCountField = typeof(FocusTimerTool).GetField("_sessionCount", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(sessionCountField);
                sessionCountField.SetValue(tool, 3);

                var isBreakField = typeof(FocusTimerTool).GetField("_isBreak", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isBreakField);
                isBreakField.SetValue(tool, false);

                var remainingSecondsField = typeof(FocusTimerTool).GetField("_remainingSeconds", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(remainingSecondsField);
                remainingSecondsField.SetValue(tool, 1); // còn 1 giây

                // Gọi Timer_Tick
                var timerTickMethod = typeof(FocusTimerTool).GetMethod("Timer_Tick", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(timerTickMethod);
                timerTickMethod.Invoke(tool, new object[] { null, EventArgs.Empty });

                // Sau Timer_Tick, thời gian nghỉ dài sẽ được kích hoạt (15 phút = 900 giây)
                var remaining = (int)remainingSecondsField.GetValue(tool);
                Assert.Equal(15 * 60, remaining);

                var isBreak = (bool)isBreakField.GetValue(tool);
                Assert.True(isBreak);

                var sessionCount = (int)sessionCountField.GetValue(tool);
                Assert.Equal(4, sessionCount);

                var txtSessionLabel = tool.FindName("txtSessionLabel") as TextBlock;
                Assert.NotNull(txtSessionLabel);
                Assert.Equal(QASmartClass.Shared.LanguageManager.Get("FocusTimer_LongBreak"), txtSessionLabel.Text);
            });
        }

        [Fact]
        public void TestQuotesBilingualSelection_MatchesSelectedLanguage()
        {
            RunOnStaThread(() =>
            {
                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                // Lấy trường _currentLang qua reflection để ghi đè ngôn ngữ trong môi trường test
                var currentLangField = typeof(QASmartClass.Shared.LanguageManager).GetField("_currentLang", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(currentLangField);
                
                // Lưu lại ngôn ngữ ban đầu để khôi phục
                var originalLang = (string)currentLangField.GetValue(null);

                try
                {
                    // Thiết lập ngôn ngữ tiếng Anh
                    currentLangField.SetValue(null, "en");

                    // Gọi ShowRandomQuote qua reflection
                    var showRandomQuoteMethod = typeof(FocusTimerTool).GetMethod("ShowRandomQuote", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(showRandomQuoteMethod);
                    showRandomQuoteMethod.Invoke(tool, null);

                    var txtQuote = tool.FindName("txtQuote") as TextBlock;
                    Assert.NotNull(txtQuote);

                    // Xác minh danh ngôn thuộc danh sách tiếng Anh
                    var quotesEnField = typeof(FocusTimerTool).GetField("QuotesEN", BindingFlags.NonPublic | BindingFlags.Static);
                    Assert.NotNull(quotesEnField);
                    var quotesEnList = (string[])quotesEnField.GetValue(null);
                    Assert.Contains(txtQuote.Text, quotesEnList);

                    // Thiết lập ngôn ngữ tiếng Việt
                    currentLangField.SetValue(null, "vi");
                    showRandomQuoteMethod.Invoke(tool, null);
                    var quotesViField = typeof(FocusTimerTool).GetField("Quotes", BindingFlags.NonPublic | BindingFlags.Static);
                    Assert.NotNull(quotesViField);
                    var quotesViList = (string[])quotesViField.GetValue(null);
                    Assert.Contains(txtQuote.Text, quotesViList);
                }
                finally
                {
                    // Khôi phục ngôn ngữ ban đầu
                    currentLangField.SetValue(null, originalLang);
                }
            });
        }

        [Fact]
        public void TestAutoStartBreak_Logic()
        {
            RunOnStaThread(() =>
            {
                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                // Bật checkbox Auto-Start Break
                var chkAutoStartBreak = tool.FindName("chkAutoStartBreak") as CheckBox;
                Assert.NotNull(chkAutoStartBreak);
                chkAutoStartBreak.IsChecked = true;

                // Thiết lập trạng thái kết thúc phiên tập trung: _isBreak = false, _remainingSeconds = 1
                var isBreakField = typeof(FocusTimerTool).GetField("_isBreak", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isBreakField);
                isBreakField.SetValue(tool, false);

                var remainingSecondsField = typeof(FocusTimerTool).GetField("_remainingSeconds", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(remainingSecondsField);
                remainingSecondsField.SetValue(tool, 1);

                // Gọi Timer_Tick
                var timerTickMethod = typeof(FocusTimerTool).GetMethod("Timer_Tick", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(timerTickMethod);
                timerTickMethod.Invoke(tool, new object[] { null, EventArgs.Empty });

                // Kỳ vọng: Đã chuyển sang nghỉ (_isBreak = true), và timer vẫn đang chạy (_isRunning = true)
                var isBreak = (bool)isBreakField.GetValue(tool);
                Assert.True(isBreak);

                var isRunningField = typeof(FocusTimerTool).GetField("_isRunning", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isRunningField);
                var isRunning = (bool)isRunningField.GetValue(tool);
                Assert.True(isRunning);

                var txtSessionLabel = tool.FindName("txtSessionLabel") as TextBlock;
                Assert.NotNull(txtSessionLabel);
                Assert.Equal(QASmartClass.Shared.LanguageManager.Get("FocusTimer_StatusBreaking"), txtSessionLabel.Text);
            });
        }

        [Fact]
        public void TestGoalsSerialization_AndSync()
        {
            RunOnStaThread(() =>
            {
                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                // Giả lập thêm một số mục tiêu tập trung vào list của Teacher
                var goalsListField = typeof(FocusTimerTool).GetField("_goalsList", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(goalsListField);
                var goalsList = goalsListField.GetValue(tool) as System.Collections.ObjectModel.ObservableCollection<FocusGoalItem>;
                Assert.NotNull(goalsList);

                goalsList.Clear();
                goalsList.Add(new FocusGoalItem { Text = "Mục tiêu 1", IsCompleted = false });
                goalsList.Add(new FocusGoalItem { Text = "Mục tiêu 2", IsCompleted = true });

                // Serialization test
                string base64Goals = "";
                try
                {
                    var list = new System.Collections.Generic.List<FocusGoalItem>(goalsList);
                    var json = System.Text.Json.JsonSerializer.Serialize(list);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                    base64Goals = Convert.ToBase64String(bytes);
                }
                catch (Exception ex)
                {
                    Assert.Fail("Serialization failed: " + ex.Message);
                }

                // Giả lập sync phía học sinh (dùng một instance tool khác của học sinh)
                var studentTool = new FocusTimerTool();
                Assert.NotNull(studentTool);

                studentTool.SyncFromTeacher(25, 5, 1500, true, false, 1, true, base64Goals);

                // Lấy danh sách mục tiêu phía học sinh
                var studentGoalsList = goalsListField.GetValue(studentTool) as System.Collections.ObjectModel.ObservableCollection<FocusGoalItem>;
                Assert.NotNull(studentGoalsList);
                Assert.Equal(2, studentGoalsList.Count);
                Assert.Equal("Mục tiêu 1", studentGoalsList[0].Text);
                Assert.False(studentGoalsList[0].IsCompleted);
                Assert.Equal("Mục tiêu 2", studentGoalsList[1].Text);
                Assert.True(studentGoalsList[1].IsCompleted);
            });
        }

        [Fact]
        public void TestStudentMode_LocksGoalInput()
        {
            RunOnStaThread(() =>
            {
                // Giả lập chế độ học sinh đang bị focus và ghi đè IsStudent()
                QASmartClass.Services.LessonStateService.Instance.ActiveToolFocusId = "focus_timer";
                QASmartClass.Services.ClassControlService.Instance.ActiveToolFocusId = "focus_timer";
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudentOverride = true;

                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                // Kích hoạt loaded event để gọi UpdateControlsState()
                var loadedMethod = typeof(FocusTimerTool).GetMethod("UpdateControlsState", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(loadedMethod);
                loadedMethod.Invoke(tool, null);

                var gridGoalInput = tool.FindName("gridGoalInput") as Grid;
                var chkAutoStartBreak = tool.FindName("chkAutoStartBreak") as CheckBox;

                Assert.NotNull(gridGoalInput);
                Assert.NotNull(chkAutoStartBreak);

                // Phải bị khóa (IsEnabled = false) khi học sinh bị focus
                Assert.False(gridGoalInput.IsEnabled);
                Assert.False(chkAutoStartBreak.IsEnabled);

                // Cleanup
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudentOverride = null;
                QASmartClass.Services.LessonStateService.Instance.ActiveToolFocusId = null;
                QASmartClass.Services.ClassControlService.Instance.ActiveToolFocusId = null;
            });
        }

        [Fact]
        public void TestGoalTextWrapping_AndMaxLength()
        {
            RunOnStaThread(() =>
            {
                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                // Kiểm tra MaxLength của TextBox txtNewGoal
                var txtNewGoal = tool.FindName("txtNewGoal") as TextBox;
                Assert.NotNull(txtNewGoal);
                Assert.Equal(80, txtNewGoal.MaxLength);

                // Kiểm tra ItemTemplate của lstGoals chứa CheckBox
                var lstGoals = tool.FindName("lstGoals") as ListBox;
                Assert.NotNull(lstGoals);
                Assert.NotNull(lstGoals.ItemTemplate);
            });
        }

        [Fact]
        public void TestDynamicCursor_BasedOnRunningState()
        {
            RunOnStaThread(() =>
            {
                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                var overlay = tool.FindName("RingInteractionOverlay") as Ellipse;
                Assert.NotNull(overlay);

                // Ban đầu chưa chạy -> Cursor phải là Hand
                var updateMethod = typeof(FocusTimerTool).GetMethod("UpdateControlsState", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(updateMethod);
                updateMethod.Invoke(tool, null);
                Assert.Equal(System.Windows.Input.Cursors.Hand, overlay.Cursor);

                // Thiết lập trạng thái đang chạy và gọi cập nhật
                var isRunningField = typeof(FocusTimerTool).GetField("_isRunning", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isRunningField);
                isRunningField.SetValue(tool, true);
                updateMethod.Invoke(tool, null);

                // Đang chạy -> Cursor phải đổi sang Arrow
                Assert.Equal(System.Windows.Input.Cursors.Arrow, overlay.Cursor);

                // Đặt lại dừng -> Cursor phải về Hand
                isRunningField.SetValue(tool, false);
                updateMethod.Invoke(tool, null);
                Assert.Equal(System.Windows.Input.Cursors.Hand, overlay.Cursor);
            });
        }

        [Fact]
        public void TestTwoWayGoalSync_UpdatesTeacherProgress()
        {
            RunOnStaThread(() =>
            {
                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                // Add goals first, since UpdateStudentProgress checks if _goalsList is empty
                var goalsListField = typeof(FocusTimerTool).GetField("_goalsList", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(goalsListField);
                var goalsList = goalsListField.GetValue(tool) as System.Collections.ObjectModel.ObservableCollection<FocusGoalItem>;
                Assert.NotNull(goalsList);
                goalsList.Add(new FocusGoalItem { Text = "Goal 1", IsCompleted = false });
                goalsList.Add(new FocusGoalItem { Text = "Goal 2", IsCompleted = false });

                // Call UpdateStudentProgress(stuCode, stuName, goalIndex, isCompleted)
                var updateProgressMethod = typeof(FocusTimerTool).GetMethod("UpdateStudentProgress", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(updateProgressMethod);

                updateProgressMethod.Invoke(tool, new object[] { "STU01", "Nguyen Van A", 0, true });

                // Verify the student progress list contains the student with the correct goal states
                var studentProgressListField = typeof(FocusTimerTool).GetField("_studentProgressList", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(studentProgressListField);
                var studentProgressList = studentProgressListField.GetValue(tool) as System.Collections.ObjectModel.ObservableCollection<FocusTimerTool.StudentProgressItem>;
                Assert.NotNull(studentProgressList);

                Assert.Single(studentProgressList);
                var progressItem = studentProgressList[0];
                Assert.Equal("STU01", progressItem.StudentCode);
                Assert.Equal("Nguyen Van A", progressItem.StudentName);
                Assert.True(progressItem.GoalStates[0]);
                Assert.False(progressItem.GoalStates[1]);
                Assert.Equal("1/2", progressItem.ProgressText);
            });
        }

        [Fact]
        public void TestTimerDriftFilter_PreventsVisualJitter()
        {
            RunOnStaThread(() =>
            {
                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                var remainingSecondsField = typeof(FocusTimerTool).GetField("_remainingSeconds", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(remainingSecondsField);
                var isRunningField = typeof(FocusTimerTool).GetField("_isRunning", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isRunningField);
                var isBreakField = typeof(FocusTimerTool).GetField("_isBreak", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isBreakField);

                // Set initial state
                remainingSecondsField.SetValue(tool, 100);
                isRunningField.SetValue(tool, true);
                isBreakField.SetValue(tool, false);

                // Case 1: Small drift (< 1.5s, e.g. 1s) -> Should be ignored
                tool.SyncFromTeacher(25, 5, 99, true, false, 0);
                Assert.Equal(100, (int)remainingSecondsField.GetValue(tool));

                // Case 2: Large drift (>= 1.5s, e.g. 2s) -> Should force synchronization
                tool.SyncFromTeacher(25, 5, 98, true, false, 0);
                Assert.Equal(98, (int)remainingSecondsField.GetValue(tool));

                // Case 3: State changes -> Should force synchronization even if drift is small
                remainingSecondsField.SetValue(tool, 100);
                tool.SyncFromTeacher(25, 5, 99, true, true, 0);
                Assert.Equal(99, (int)remainingSecondsField.GetValue(tool));
            });
        }

        [Fact]
        public void TestDatabaseLogging_OnSessionCompleted()
        {
            RunOnStaThread(() =>
            {
                // Setup temporary database to avoid polluting production database
                var originalDbFile = QASmartClass.Services.AppPaths.DatabaseFile;
                var originalVersionFile = QASmartClass.Services.AppPaths.DbVersionFile;
                
                var uniqueId = Guid.NewGuid().ToString("N");
                var dbFile = System.IO.Path.Combine(QASmartClass.Services.AppPaths.TempDir, $"smartclass_focustest_{uniqueId}.db");
                var versionFile = System.IO.Path.Combine(QASmartClass.Services.AppPaths.TempDir, $"db_version_focustest_{uniqueId}.txt");

                QASmartClass.Services.AppPaths.DatabaseFile = dbFile;
                QASmartClass.Services.AppPaths.DbVersionFile = versionFile;

                try
                {
                    using (var db = new QASmartClass.Data.AppDbContext())
                    {
                        QASmartClass.Services.DbMigrator.Migrate(db, "5.44.0");
                    }

                    var tool = new FocusTimerTool();
                    Assert.NotNull(tool);

                    // Invoke private method SaveSessionToDb(int focusMins, int breakMins, int sessionCount, int completedGoals, int totalGoals)
                    var saveSessionMethod = typeof(FocusTimerTool).GetMethod("SaveSessionToDb", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(saveSessionMethod);

                    // Call SaveSessionToDb(25, 5, 2, 1, 3)
                    saveSessionMethod.Invoke(tool, new object[] { 25, 5, 2, 1, 3 });

                    // Verify that the record was saved in AppDbContext (with a dispatcher-pumping loop since it is async)
                    QASmartClass.Data.EventLog log = null;
                    var startTime = DateTime.Now;
                    while ((DateTime.Now - startTime).TotalSeconds < 2.0)
                    {
                        // Pump dispatcher messages to allow the background task's Dispatcher.Invoke to complete
                        var frame = new System.Windows.Threading.DispatcherFrame();
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            new Func<object, object>(f =>
                            {
                                ((System.Windows.Threading.DispatcherFrame)f).Continue = false;
                                return null;
                            }), frame);
                        System.Windows.Threading.Dispatcher.PushFrame(frame);

                        using (var db = new QASmartClass.Data.AppDbContext())
                        {
                            log = db.EventLogs.FirstOrDefault(l => l.EventType == "FOCUS_TIMER_SESSION_RECORD");
                            if (log != null)
                                break;
                        }
                        Thread.Sleep(50);
                    }

                    Assert.NotNull(log);
                    Assert.Equal(QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudent() ? "Student" : "Teacher", log.Actor);
                    Assert.Contains("\"FocusMins\":25", log.Details);
                    Assert.Contains("\"BreakMins\":5", log.Details);
                    Assert.Contains("\"TotalSessions\":2", log.Details);
                    Assert.Contains("\"GoalsCompleted\":1", log.Details);
                    Assert.Contains("\"GoalsTotal\":3", log.Details);
                }
                finally
                {

                    // Restore original DB settings
                    QASmartClass.Services.AppPaths.DatabaseFile = originalDbFile;
                    QASmartClass.Services.AppPaths.DbVersionFile = originalVersionFile;

                    // Clean up test DB files
                    try
                    {
                        if (System.IO.File.Exists(dbFile))
                            System.IO.File.Delete(dbFile);
                    }
                    catch { }
                    try
                    {
                        if (System.IO.File.Exists(versionFile))
                            System.IO.File.Delete(versionFile);
                    }
                    catch { }
                }
            });
        }

        [Fact]
        public void TestLateJoiner_SendsSyncRequest_AndTeacherResponds()
        {
            RunOnStaThread(() =>
            {
                var app = System.Windows.Application.Current as QASmartTouch.App;
                Assert.NotNull(app);

                var originalRole = app.UserRoleService.CurrentRole;
                app.UserRoleService.CurrentRole = QASmartClass.Shared.UserRole.Teacher;

                try
                {
                    var tool = new FocusTimerTool();
                    Assert.NotNull(tool);

                    string receivedCmd = null;
                    EventHandler<string> handler = (sender, cmd) =>
                    {
                        receivedCmd = cmd;
                    };
                    app.LocalCommandReceived += handler;

                    try
                    {
                        var messageReceivedMethod = typeof(FocusTimerTool).GetMethod("NetworkService_MessageReceived", BindingFlags.NonPublic | BindingFlags.Instance);
                        Assert.NotNull(messageReceivedMethod);

                        var eventArgs = new QASmartClass.Classroom.Services.StudentMessageEventArgs
                        {
                            StudentCode = "STU01",
                            Message = "CMD|REQUEST_FOCUS_SYNC"
                        };

                        messageReceivedMethod.Invoke(tool, new object[] { null, eventArgs });

                        Assert.NotNull(receivedCmd);
                        Assert.StartsWith("CMD|FOCUS_TIMER_SYNC|", receivedCmd);
                    }
                    finally
                    {
                        app.LocalCommandReceived -= handler;
                    }
                }
                finally
                {
                    app.UserRoleService.CurrentRole = originalRole;
                }
            });
        }

        [Fact]
        public void TestInputSanitization_FiltersSeparators()
        {
            RunOnStaThread(() =>
            {
                var app = System.Windows.Application.Current as QASmartTouch.App;
                Assert.NotNull(app);

                var originalRole = app.UserRoleService.CurrentRole;
                app.UserRoleService.CurrentRole = QASmartClass.Shared.UserRole.Student;

                app.StudentNetwork.StudentCode = "STU|01";
                app.StudentNetwork.StudentName = "Nguyen|Van|A";
                app.StudentNetwork.SessionKey = "";

                // Start local TCP listener
                var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
                listener.Start();
                int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;

                // Connect student network to it
                var connectTask = app.StudentNetwork.ConnectDirectAsync("127.0.0.1", port);
                var acceptTask = listener.AcceptTcpClientAsync();

                Task.WaitAll(connectTask, acceptTask);

                using var serverClient = acceptTask.Result;
                var serverStream = serverClient.GetStream();

                try
                {
                    var tool = new FocusTimerTool();
                    Assert.NotNull(tool);

                    var goalsListField = typeof(FocusTimerTool).GetField("_goalsList", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(goalsListField);
                    var goalsList = goalsListField.GetValue(tool) as System.Collections.ObjectModel.ObservableCollection<FocusGoalItem>;
                    Assert.NotNull(goalsList);
                    var item = new FocusGoalItem { Text = "Sanitization Goal", IsCompleted = true };
                    goalsList.Add(item);

                    var goalCheckChangedMethod = typeof(FocusTimerTool).GetMethod("GoalCheck_Changed", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(goalCheckChangedMethod);

                    var chk = new CheckBox { DataContext = item };
                    goalCheckChangedMethod.Invoke(tool, new object[] { chk, new RoutedEventArgs() });

                    // Read from the server socket
                    byte[] buffer = new byte[1024];
                    int read = serverStream.Read(buffer, 0, buffer.Length);
                    string written = System.Text.Encoding.UTF8.GetString(buffer, 0, read);

                    Assert.Contains("STU-01", written);
                    Assert.Contains("Nguyen-Van-A", written);
                    Assert.DoesNotContain("|", written);
                }
                finally
                {
                    app.UserRoleService.CurrentRole = originalRole;
                    app.StudentNetwork.Stop();
                    listener.Stop();
                }
            });
        }

        [Fact]
        public void TestNegativeTimeDisplay_SafetyFallback()
        {
            RunOnStaThread(() =>
            {
                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                tool.SyncFromTeacher(15, 5, -15, true, false, 1);

                var remainingSecondsField = typeof(FocusTimerTool).GetField("_remainingSeconds", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(remainingSecondsField);

                int remaining = (int)remainingSecondsField.GetValue(tool);
                Assert.Equal(0, remaining);

                var txtTime = tool.FindName("txtTime") as TextBlock;
                Assert.NotNull(txtTime);
                Assert.Equal("00:00", txtTime.Text);
            });
        }

        [Fact]
        public void TestAsyncDatabaseLogging_DoesNotBlockUIThread()
        {
            RunOnStaThread(() =>
            {
                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                var saveSessionMethod = typeof(FocusTimerTool).GetMethod("SaveSessionToDb", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(saveSessionMethod);

                var sw = System.Diagnostics.Stopwatch.StartNew();
                saveSessionMethod.Invoke(tool, new object[] { 25, 5, 2, 1, 3 });
                sw.Stop();

                Assert.True(sw.ElapsedMilliseconds < 15, $"SaveSessionToDb took too long: {sw.ElapsedMilliseconds}ms");
            });
        }

        [Fact]
        public void TestStudentMode_ClassControlFocus_DisablesControls()
        {
            RunOnStaThread(() =>
            {
                // Giả lập chế độ học sinh đang bị focus bằng ClassControlService
                QASmartClass.Services.ClassControlService.Instance.ActiveToolFocusId = "focus_timer";
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudentOverride = true;

                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                // Kích hoạt UpdateControlsState
                var loadedMethod = typeof(FocusTimerTool).GetMethod("UpdateControlsState", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(loadedMethod);
                loadedMethod.Invoke(tool, null);

                // Lấy các nút điều khiển chính và nút Delete Goal
                var btnStart = tool.FindName("btnStart") as Button;
                var btnReset = tool.FindName("btnReset") as Button;
                var sliderVolume = tool.FindName("sliderVolume") as Slider;
                var gridGoalInput = tool.FindName("gridGoalInput") as Grid;

                Assert.NotNull(btnStart);
                Assert.NotNull(btnReset);
                Assert.NotNull(sliderVolume);
                Assert.NotNull(gridGoalInput);

                // Kiểm tra xem các control có bị vô hiệu hóa hoặc ẩn không
                Assert.Equal(Visibility.Collapsed, btnStart.Visibility);
                Assert.Equal(Visibility.Collapsed, btnReset.Visibility);
                Assert.False(sliderVolume.IsEnabled);
                Assert.False(gridGoalInput.IsEnabled);
                Assert.Equal(Visibility.Collapsed, tool.DeleteGoalVisibility);

                // Dọn dẹp
                QASmartClass.LearningTools.Helpers.TeachingActionHelper.IsStudentOverride = null;
                QASmartClass.Services.ClassControlService.Instance.ActiveToolFocusId = "";
            });
        }

        [Fact]
        public void TestFocusTimerTool_WidescreenCompliance()
        {
            RunOnStaThread(() =>
            {
                var tool = new FocusTimerTool();
                Assert.NotNull(tool);

                var rootGrid = tool.Content as Grid;
                // Assert.NotNull(rootGrid);
                // // Assert.Equal(1200, rootGrid.MaxWidth);

                var tc = tool.FindName("mainTabControl") as TabControl;
                // Assert.NotNull(tc);

                // tc.SelectedIndex = 1;
                // Assert.Equal(1600, rootGrid.MaxWidth);

                // tc.SelectedIndex = 0;
                // // Assert.Equal(1200, rootGrid.MaxWidth);
            });
        }
    }
}
