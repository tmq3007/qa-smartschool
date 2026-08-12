using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Views.Thinking;
using QASmartClass.LearningTools.Helpers;
using Xunit;

namespace QASmartClass.Tests
{
    public class V42MentalMathKeyboardTests
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
        public void TestMentalMathTool_KeyboardPlacement_IsRight()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V42MentalMathKeyboardTests] Error: {ex.Message}"); }
                }

                // Reset các trường tĩnh để tránh lỗi Thread Affinity
                var popupField = typeof(TouchNumPad).GetField("_popup", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(popupField);
                popupField.SetValue(null, null);

                var targetField = typeof(TouchNumPad).GetField("_currentTarget", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(targetField);
                targetField.SetValue(null, null);

                // Add missing static resources to prevent XamlParseException
                if (System.Windows.Application.Current != null)
                {
                    if (!System.Windows.Application.Current.Resources.Contains("Gray100"))
                        System.Windows.Application.Current.Resources.Add("Gray100", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)));
                    if (!System.Windows.Application.Current.Resources.Contains("BrandAccent"))
                        System.Windows.Application.Current.Resources.Add("BrandAccent", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 112, 67)));
                    if (!System.Windows.Application.Current.Resources.Contains("BrandPrimary"))
                        System.Windows.Application.Current.Resources.Add("BrandPrimary", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 150, 136)));
                }

                var tool = new MentalMathTool();
                var txtAnswer = (TextBox)tool.FindName("txtAnswer");
                Assert.NotNull(txtAnswer);

                // Kích hoạt focus để ShowPopup chạy
                txtAnswer.Focus();

                var popup = (Popup)popupField.GetValue(null);
                Assert.NotNull(popup);

                // Xác nhận Placement mặc định cho MentalMathTool là Right
                Assert.Equal(PlacementMode.Right, popup.Placement);
            });
        }

        [Fact]
        public void TestTouchNumPad_RemembersDraggedPosition()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V42MentalMathKeyboardTests] Error: {ex.Message}"); }
                }

                // Reset các trường tĩnh để tránh lỗi Thread Affinity
                var popupField = typeof(TouchNumPad).GetField("_popup", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(popupField);
                popupField.SetValue(null, null);

                var targetField = typeof(TouchNumPad).GetField("_currentTarget", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(targetField);
                targetField.SetValue(null, null);

                var tb1 = new TextBox();
                TouchNumPad.Attach(tb1, step: 1, placement: PlacementMode.Bottom);

                // Kích hoạt hiển thị
                tb1.Focus();

                var popup = (Popup)popupField.GetValue(null);
                Assert.NotNull(popup);

                // Giả lập kéo thả bàn phím ảo bằng cách gán offset và thiết lập biến lưu trữ
                popup.HorizontalOffset = 150;
                popup.VerticalOffset = 250;

                var savedHorzField = typeof(TouchNumPad).GetField("_savedHorizontalOffset", BindingFlags.NonPublic | BindingFlags.Static);
                var savedVertField = typeof(TouchNumPad).GetField("_savedVerticalOffset", BindingFlags.NonPublic | BindingFlags.Static);
                var hasSavedField = typeof(TouchNumPad).GetField("_hasSavedOffset", BindingFlags.NonPublic | BindingFlags.Static);

                Assert.NotNull(savedHorzField);
                Assert.NotNull(savedVertField);
                Assert.NotNull(hasSavedField);

                savedHorzField.SetValue(null, 150.0);
                savedVertField.SetValue(null, 250.0);
                hasSavedField.SetValue(null, true);

                // Gọi lại ShowPopup (giả lập focus lại cùng TextBox)
                var showMethod = typeof(TouchNumPad).GetMethod("ShowPopup", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.NotNull(showMethod);

                showMethod.Invoke(null, new object[] { tb1 });

                // Khẳng định offset không bị reset về 0 mà giữ nguyên giá trị đã lưu
                Assert.Equal(150, popup.HorizontalOffset);
                Assert.Equal(250, popup.VerticalOffset);

                // Giả lập chuyển sang một TextBox khác (tb2) cùng PlacementMode.Bottom
                var tb2 = new TextBox();
                TouchNumPad.Attach(tb2, step: 1, placement: PlacementMode.Bottom);
                
                showMethod.Invoke(null, new object[] { tb2 });

                // Khẳng định offset bị reset về 0 do chuyển đổi sang TextBox khác
                Assert.Equal(0, popup.HorizontalOffset);
                Assert.Equal(0, popup.VerticalOffset);
                Assert.False((bool)hasSavedField.GetValue(null));
            });
        }

        [Fact]
        public void TestTouchNumPad_MathKeysVisibility()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[V42MentalMathKeyboardTests] Error: {ex.Message}"); }
                }

                var popupField = typeof(TouchNumPad).GetField("_popup", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(popupField);
                popupField.SetValue(null, null);

                var targetField = typeof(TouchNumPad).GetField("_currentTarget", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(targetField);
                targetField.SetValue(null, null);

                var mathPanelField = typeof(TouchNumPad).GetField("_mathPanel", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(mathPanelField);
                mathPanelField.SetValue(null, null);

                // Attach with enableMathKeys = true
                var tb1 = new TextBox();
                TouchNumPad.Attach(tb1, step: 1, enableMathKeys: true);
                tb1.Focus();

                var mathPanel = (System.Windows.FrameworkElement)mathPanelField.GetValue(null);
                Assert.NotNull(mathPanel);
                Assert.Equal(System.Windows.Visibility.Visible, mathPanel.Visibility);

                // Attach with enableMathKeys = false (default)
                var tb2 = new TextBox();
                TouchNumPad.Attach(tb2, step: 1, enableMathKeys: false);
                tb2.Focus();

                mathPanel = (System.Windows.FrameworkElement)mathPanelField.GetValue(null);
                Assert.NotNull(mathPanel);
                Assert.Equal(System.Windows.Visibility.Collapsed, mathPanel.Visibility);
            });
        }

        [Fact]
        public void TestDbManager_GetAllProgress()
        {
            DbManager.Initialize();
            string gameName = "TestGetAll_" + Guid.NewGuid().ToString();
            var progress1 = new UserProgress
            {
                GameName = gameName,
                Score = 10,
                Total = 30,
                DurationSeconds = 15.0,
                Difficulty = "Dễ",
                CreatedAt = DateTime.Now.AddMinutes(-5)
            };
            var progress2 = new UserProgress
            {
                GameName = gameName,
                Score = 20,
                Total = 30,
                DurationSeconds = 12.0,
                Difficulty = "Khó",
                CreatedAt = DateTime.Now
            };
            DbManager.SaveProgress(progress1);
            DbManager.SaveProgress(progress2);

            // Chờ DB ghi xong
            Thread.Sleep(500);

            var list = DbManager.GetAllProgress(gameName);
            Assert.NotNull(list);
            Assert.Equal(2, list.Count);
            Assert.True(list[0].CreatedAt >= list[1].CreatedAt);
            Assert.Equal("Khó", list[0].Difficulty);
            Assert.Equal("Dễ", list[1].Difficulty);
        }

        [Fact]
        public void TestMentalMathTool_DynamicStreakColors()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch { }
                }

                // Add resources
                if (System.Windows.Application.Current != null)
                {
                    if (!System.Windows.Application.Current.Resources.Contains("Gray100"))
                        System.Windows.Application.Current.Resources.Add("Gray100", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)));
                    if (!System.Windows.Application.Current.Resources.Contains("BrandAccent"))
                        System.Windows.Application.Current.Resources.Add("BrandAccent", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 112, 67)));
                    if (!System.Windows.Application.Current.Resources.Contains("BrandPrimary"))
                        System.Windows.Application.Current.Resources.Add("BrandPrimary", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 150, 136)));
                }

                var originalLang = QASmartClass.Shared.LanguageManager.CurrentLanguage;
                try
                {
                    QASmartClass.Shared.LanguageManager.SetLanguage("vi");

                    var tool = new MentalMathTool();
                    var txtStreak = (TextBlock)tool.FindName("txtStreak");
                    Assert.NotNull(txtStreak);

                    var fieldStreak = typeof(MentalMathTool).GetField("_streak", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(fieldStreak);

                    var methodUpdateScore = typeof(MentalMathTool).GetMethod("UpdateScoreDisplay", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(methodUpdateScore);

                    // Streak < 5
                    fieldStreak.SetValue(tool, 3);
                    methodUpdateScore.Invoke(tool, null);
                    Assert.Equal("🔥 3", txtStreak.Text);

                    // Streak >= 5
                    fieldStreak.SetValue(tool, 7);
                    methodUpdateScore.Invoke(tool, null);
                    Assert.Contains("Nóng bỏng!", txtStreak.Text);
                    var colorBrush = txtStreak.Foreground as System.Windows.Media.SolidColorBrush;
                    Assert.NotNull(colorBrush);
                    Assert.Equal(System.Windows.Media.Color.FromRgb(255, 152, 0), colorBrush.Color);

                    // Streak >= 10
                    fieldStreak.SetValue(tool, 12);
                    methodUpdateScore.Invoke(tool, null);
                    Assert.Contains("Siêu cấp!", txtStreak.Text);
                    colorBrush = txtStreak.Foreground as System.Windows.Media.SolidColorBrush;
                    Assert.NotNull(colorBrush);
                    Assert.Equal(System.Windows.Media.Color.FromRgb(244, 67, 54), colorBrush.Color);
                }
                finally
                {
                    QASmartClass.Shared.LanguageManager.SetLanguage(originalLang);
                }
            });
        }

        [Fact]
        public void TestMentalMathTool_MedalSelection()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch { }
                }

                // Add resources
                if (System.Windows.Application.Current != null)
                {
                    if (!System.Windows.Application.Current.Resources.Contains("Gray100"))
                        System.Windows.Application.Current.Resources.Add("Gray100", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)));
                    if (!System.Windows.Application.Current.Resources.Contains("BrandAccent"))
                        System.Windows.Application.Current.Resources.Add("BrandAccent", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 112, 67)));
                    if (!System.Windows.Application.Current.Resources.Contains("BrandPrimary"))
                        System.Windows.Application.Current.Resources.Add("BrandPrimary", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 150, 136)));
                }

                var tool = new MentalMathTool();
                var txtMedal = (TextBlock)tool.FindName("txtMedal");
                Assert.NotNull(txtMedal);

                var fieldScore = typeof(MentalMathTool).GetField("_score", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(fieldScore);

                var methodFinishGame = typeof(MentalMathTool).GetMethod("FinishGame", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(methodFinishGame);

                // 30 câu đúng -> 🏆
                fieldScore.SetValue(tool, 30);
                methodFinishGame.Invoke(tool, null);
                Assert.Equal(System.Windows.Visibility.Visible, txtMedal.Visibility);
                Assert.Equal("🏆", txtMedal.Text);

                // 25 câu đúng -> 🥇
                fieldScore.SetValue(tool, 25);
                methodFinishGame.Invoke(tool, null);
                Assert.Equal("🥇", txtMedal.Text);

                // 22 câu đúng -> 🥈
                fieldScore.SetValue(tool, 22);
                methodFinishGame.Invoke(tool, null);
                Assert.Equal("🥈", txtMedal.Text);

                // 17 câu đúng -> 📚
                fieldScore.SetValue(tool, 17);
                methodFinishGame.Invoke(tool, null);
                Assert.Equal("📚", txtMedal.Text);

                // 10 câu đúng -> 💪
                fieldScore.SetValue(tool, 10);
                methodFinishGame.Invoke(tool, null);
                Assert.Equal("💪", txtMedal.Text);

                // Bấm reset -> Collapsed
                var btnReset = (Button)tool.FindName("btnReset");
                Assert.NotNull(btnReset);
                btnReset.RaiseEvent(new System.Windows.RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(System.Windows.Visibility.Collapsed, txtMedal.Visibility);
                Assert.Equal("", txtMedal.Text);
            });
        }

        [Fact]
        public void TestMentalMathTool_TextBoxInputFilters()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch { }
                }

                var tool = new MentalMathTool();
                var txtAnswer = (TextBox)tool.FindName("txtAnswer");
                Assert.NotNull(txtAnswer);

                var methodValidation = typeof(MentalMathTool).GetMethod("NumberValidationTextBox", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(methodValidation);

                // Test gõ chữ cái "a" -> e.Handled phải là true
                var composition = new System.Windows.Input.TextComposition(System.Windows.Input.InputManager.Current, txtAnswer, "a");
                var args = new System.Windows.Input.TextCompositionEventArgs(System.Windows.Input.InputManager.Current.PrimaryKeyboardDevice, composition);
                args.RoutedEvent = UIElement.PreviewTextInputEvent;
                methodValidation.Invoke(tool, new object[] { txtAnswer, args });
                Assert.True(args.Handled);

                // Test gõ số "5" -> e.Handled phải là false
                var compositionNum = new System.Windows.Input.TextComposition(System.Windows.Input.InputManager.Current, txtAnswer, "5");
                var argsNum = new System.Windows.Input.TextCompositionEventArgs(System.Windows.Input.InputManager.Current.PrimaryKeyboardDevice, compositionNum);
                argsNum.RoutedEvent = UIElement.PreviewTextInputEvent;
                methodValidation.Invoke(tool, new object[] { txtAnswer, argsNum });
                Assert.False(argsNum.Handled);

                // Test gõ "." khi trống -> e.Handled phải là false
                var compositionDot = new System.Windows.Input.TextComposition(System.Windows.Input.InputManager.Current, txtAnswer, ".");
                var argsDot = new System.Windows.Input.TextCompositionEventArgs(System.Windows.Input.InputManager.Current.PrimaryKeyboardDevice, compositionDot);
                argsDot.RoutedEvent = UIElement.PreviewTextInputEvent;
                methodValidation.Invoke(tool, new object[] { txtAnswer, argsDot });
                Assert.False(argsDot.Handled);

                // Test gõ "-" khi trống -> e.Handled phải là false
                var compositionMinus = new System.Windows.Input.TextComposition(System.Windows.Input.InputManager.Current, txtAnswer, "-");
                var argsMinus = new System.Windows.Input.TextCompositionEventArgs(System.Windows.Input.InputManager.Current.PrimaryKeyboardDevice, compositionMinus);
                argsMinus.RoutedEvent = UIElement.PreviewTextInputEvent;
                methodValidation.Invoke(tool, new object[] { txtAnswer, argsMinus });
                Assert.False(argsMinus.Handled);

                // Giả lập TextBox đang có text là "2.5"
                txtAnswer.Text = "2.5";
                txtAnswer.SelectionStart = 3;

                // Test gõ thêm "." -> e.Handled phải là true (chặn vì đã có ".")
                var compositionSecondDot = new System.Windows.Input.TextComposition(System.Windows.Input.InputManager.Current, txtAnswer, ".");
                var argsSecondDot = new System.Windows.Input.TextCompositionEventArgs(System.Windows.Input.InputManager.Current.PrimaryKeyboardDevice, compositionSecondDot);
                argsSecondDot.RoutedEvent = UIElement.PreviewTextInputEvent;
                methodValidation.Invoke(tool, new object[] { txtAnswer, argsSecondDot });
                Assert.True(argsSecondDot.Handled);

                // Test gõ thêm "," -> e.Handled phải là true (chặn vì đã có ".")
                var compositionComma = new System.Windows.Input.TextComposition(System.Windows.Input.InputManager.Current, txtAnswer, ",");
                var argsComma = new System.Windows.Input.TextCompositionEventArgs(System.Windows.Input.InputManager.Current.PrimaryKeyboardDevice, compositionComma);
                argsComma.RoutedEvent = UIElement.PreviewTextInputEvent;
                methodValidation.Invoke(tool, new object[] { txtAnswer, argsComma });
                Assert.True(argsComma.Handled);
            });
        }

        [Fact]
        public void TestMentalMathTool_LeaderboardRendering()
        {
            RunOnStaThread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    try { new System.Windows.Application(); } catch { }
                }

                // Add resources
                if (System.Windows.Application.Current != null)
                {
                    if (!System.Windows.Application.Current.Resources.Contains("Gray100"))
                        System.Windows.Application.Current.Resources.Add("Gray100", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)));
                    if (!System.Windows.Application.Current.Resources.Contains("BrandAccent"))
                        System.Windows.Application.Current.Resources.Add("BrandAccent", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 112, 67)));
                    if (!System.Windows.Application.Current.Resources.Contains("BrandPrimary"))
                        System.Windows.Application.Current.Resources.Add("BrandPrimary", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 150, 136)));
                }

                var tool = new MentalMathTool();
                var brdLeaderboard = (Border)tool.FindName("brdLeaderboard");
                Assert.NotNull(brdLeaderboard);
                
                var gridLeaderboard = (Grid)tool.FindName("gridLeaderboard");
                Assert.NotNull(gridLeaderboard);

                var txtEmptyLeaderboard = (TextBlock)tool.FindName("txtEmptyLeaderboard");
                Assert.NotNull(txtEmptyLeaderboard);

                // Call LoadLeaderboard via reflection
                var methodLoad = typeof(MentalMathTool).GetMethod("LoadLeaderboard", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(methodLoad);
                
                // Invoke. It shouldn't throw.
                var ex = Record.Exception(() => methodLoad.Invoke(tool, null));
                Assert.Null(ex);

                // Test Visibility sync in Reset and Start
                var btnStart = (Button)tool.FindName("btnStart");
                var btnReset = (Button)tool.FindName("btnReset");
                Assert.NotNull(btnStart);
                Assert.NotNull(btnReset);

                // Reset Click -> Leaderboard visible
                btnReset.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(Visibility.Visible, brdLeaderboard.Visibility);

                // Start Click -> Leaderboard collapsed
                btnStart.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(Visibility.Collapsed, brdLeaderboard.Visibility);
            });
        }
    }
}
