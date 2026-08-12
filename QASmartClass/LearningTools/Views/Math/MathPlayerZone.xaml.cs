using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using QASmartClass.LearningTools.Controls;
using QASmartClass.Data;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class MathPlayerZone : UserControl
    {
        private static readonly Random Rnd = new Random();

        public enum PlayMode { SelfCalc, SelfComp, TeacherVerify }
        public enum MathOp { Add, Sub, Mix }

        private int _maxQuestions = 10;
        private int _currentQuestion = 0;
        private int _score = 0;
        private int _streak = 0;
        private int _attemptsThisQuestion = 0;

        private PlayMode _mode;
        private MathOp _operation;
        private int _rangeMax;

        private int _expectedResult;
        private string _expectedComp; // "<", "=", ">"

        public MathPlayerZone()
        {
            InitializeComponent();
            Loaded += (_, _) => {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextGame != null) menuTextGame.Text = isVN ? "Trò chơi" : "Game";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                LoadPracticalApps();
            };
        }

        public void StartGame(string playerName, Color themeColor, int rangeMax, MathOp op, PlayMode mode, int maxQuestions)
        {
            txtPlayerName.Text = playerName;
            playerBadge.Background = new SolidColorBrush(themeColor);
            mainBorder.BorderBrush = new SolidColorBrush(themeColor);
            
            _rangeMax = rangeMax;
            _operation = op;
            _mode = mode;
            _maxQuestions = maxQuestions;

            _currentQuestion = 0;
            _score = 0;
            _streak = 0;
            _attemptsThisQuestion = 0;

            panelVictory.Visibility = Visibility.Collapsed;
            panelExpression.Visibility = Visibility.Visible;

            SetupUIForMode();
            NextQuestion();
        }

        private void SetupUIForMode()
        {
            inputSelfCalc.Visibility = Visibility.Collapsed;
            inputSelfComp.Visibility = Visibility.Collapsed;
            inputTeacherVerify.Visibility = Visibility.Collapsed;

            txtCalcExpr.Visibility = Visibility.Collapsed;
            panelCompExpr.Visibility = Visibility.Collapsed;

            if (_mode == PlayMode.SelfCalc)
            {
                inputSelfCalc.Visibility = Visibility.Visible;
                txtCalcExpr.Visibility = Visibility.Visible;
            }
            else if (_mode == PlayMode.SelfComp)
            {
                inputSelfComp.Visibility = Visibility.Visible;
                panelCompExpr.Visibility = Visibility.Visible;
            }
            else if (_mode == PlayMode.TeacherVerify)
            {
                inputTeacherVerify.Visibility = Visibility.Visible;
                txtCalcExpr.Visibility = Visibility.Visible;
            }
        }

        private void NextQuestion()
        {
            if (_currentQuestion >= _maxQuestions && _maxQuestions > 0)
            {
                ShowVictory();
                return;
            }

            _currentQuestion++;
            _attemptsThisQuestion = 0;

            txtProgress.Text = _maxQuestions > 0 ? $"Câu {_currentQuestion}/{_maxQuestions}" : $"Câu {_currentQuestion}";
            txtScore.Text = $"⭐ {_score}";
            txtAnswer.Text = "";
            txtCompSign.Text = "?";

            // Reset colors to default
            txtCalcExpr.Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)); // Gray900
            txtCompLeft.Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33));
            txtCompRight.Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33));
            txtCompSign.Foreground = DS.Brush(DS.BrandAccent);

            bool isAdd = _operation == MathOp.Add;
            if (_operation == MathOp.Mix) isAdd = Rnd.Next(2) == 0;

            int a, b;
            if (isAdd)
            {
                a = Rnd.Next(0, _rangeMax + 1);
                b = Rnd.Next(0, _rangeMax - a + 1);
                _expectedResult = a + b;
            }
            else
            {
                a = Rnd.Next(0, _rangeMax + 1);
                b = Rnd.Next(0, a + 1);
                _expectedResult = a - b;
            }

            string opStr = isAdd ? "+" : "-";

            if (_mode == PlayMode.SelfCalc)
            {
                txtCalcExpr.Text = $"{a} {opStr} {b} = ?";
            }
            else if (_mode == PlayMode.TeacherVerify)
            {
                txtCalcExpr.Text = $"{a} {opStr} {b} = ?";
                txtTeacherHint.Text = $"Đáp án: {_expectedResult}";
            }
            else if (_mode == PlayMode.SelfComp)
            {
                txtCompLeft.Text = $"{a} {opStr} {b}";
                int c = _expectedResult;
                int r = Rnd.Next(100);
                if (r < 33) { /* c = res */ }
                else if (r < 66) c = _expectedResult + Rnd.Next(1, 6);
                else c = System.Math.Max(0, _expectedResult - Rnd.Next(1, 6));

                txtCompRight.Text = c.ToString();

                if (_expectedResult < c) _expectedComp = "<";
                else if (_expectedResult > c) _expectedComp = ">";
                else _expectedComp = "=";
            }
        }

        private async void ProcessAnswer(bool isCorrect, string actualAnswer = "")
        {
            // Disable inputs during animation/transition
            SetInputsEnabled(false);

            if (isCorrect)
            {
                _score += 10 + (_streak * 2);
                _streak++;
                ShowFeedback("Chính xác!", true);
                
                RevealCorrectAnswer(true);
                txtScore.Text = $"⭐ {_score}";
                
                // Add Scale/Bounce Animation
                var scaleTrans = txtScore.RenderTransform as ScaleTransform;
                if (scaleTrans == null)
                {
                    scaleTrans = new ScaleTransform(1, 1, 0.5, 0.5);
                    txtScore.RenderTransformOrigin = new Point(0.5, 0.5);
                    txtScore.RenderTransform = scaleTrans;
                }
                var bounceAnim = new DoubleAnimation(1.0, 1.5, new Duration(TimeSpan.FromSeconds(0.15))) { AutoReverse = true };
                scaleTrans.BeginAnimation(ScaleTransform.ScaleXProperty, bounceAnim);
                scaleTrans.BeginAnimation(ScaleTransform.ScaleYProperty, bounceAnim);

                // Flash Border Green
                var oldBrush = mainBorder.BorderBrush;
                mainBorder.BorderBrush = DS.Brush(DS.ResultSuccess);

                await Task.Delay(1500);
                mainBorder.BorderBrush = oldBrush;
                NextQuestion();
            }
            else
            {
                _attemptsThisQuestion++;
                int maxAttempts = (_mode == PlayMode.SelfComp) ? 1 : 2;

                if (_attemptsThisQuestion >= maxAttempts)
                {
                    _streak = 0;
                    ShowFeedback("Hết lượt thử!", false);
                    
                    RevealCorrectAnswer(false);
                    var oldBrush = mainBorder.BorderBrush;
                    mainBorder.BorderBrush = DS.Brush(DS.ResultDanger);
                    
                    await Task.Delay(2000);
                    mainBorder.BorderBrush = oldBrush;
                    NextQuestion();
                }
                else
                {
                    _streak = 0;
                    ShowFeedback("Sai rồi! Hãy thử lại", false);
                    var oldBrush = mainBorder.BorderBrush;
                    mainBorder.BorderBrush = DS.Brush(DS.ResultDanger);
                    
                    await Task.Delay(1000);
                    mainBorder.BorderBrush = oldBrush;
                    
                    // Re-enable inputs for retry
                    SetInputsEnabled(true);
                }
            }
        }

        private void RevealCorrectAnswer(bool correctStatus)
        {
            if (_mode == PlayMode.SelfCalc || _mode == PlayMode.TeacherVerify)
            {
                string opStr = _operation == MathOp.Add ? "+" : (_operation == MathOp.Sub ? "-" : "?");
                if (_operation == MathOp.Mix)
                {
                    opStr = txtCalcExpr.Text.Contains("+") ? "+" : "-";
                }
                
                string[] parts = txtCalcExpr.Text.Split(' ');
                if (parts.Length >= 3)
                {
                    txtCalcExpr.Text = $"{parts[0]} {parts[1]} {parts[2]} = {_expectedResult}";
                    txtCalcExpr.Foreground = DS.Brush(correctStatus ? DS.ResultSuccess : DS.ResultDanger);
                }
            }
            else if (_mode == PlayMode.SelfComp)
            {
                txtCompSign.Text = _expectedComp;
                txtCompSign.Foreground = DS.Brush(correctStatus ? DS.ResultSuccess : DS.ResultDanger);
            }
        }

        private void SetInputsEnabled(bool isEnabled)
        {
            inputSelfCalc.IsEnabled = isEnabled;
            inputSelfComp.IsEnabled = isEnabled;
            inputTeacherVerify.IsEnabled = isEnabled;
        }

        private void ShowFeedback(string msg, bool isSuccess)
        {
            txtFeedback.Text = msg;
            txtFeedback.Foreground = DS.Brush(isSuccess ? DS.ResultSuccess : DS.ResultDanger);
            
            DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.2));
            fadeIn.AutoReverse = true;
            fadeIn.Duration = TimeSpan.FromSeconds(0.8);
            txtFeedback.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void ShowVictory()
        {
            SaveGameHistoryToDb();

            panelExpression.Visibility = Visibility.Collapsed;
            inputSelfCalc.Visibility = Visibility.Collapsed;
            inputSelfComp.Visibility = Visibility.Collapsed;
            inputTeacherVerify.Visibility = Visibility.Collapsed;
            
            panelVictory.Visibility = Visibility.Visible;
            txtFinalScore.Text = $"Tổng điểm: {_score}";
        }

        public void ForceSaveOnStop()
        {
            if (_currentQuestion > 0)
            {
                SaveGameHistoryToDb();
            }
        }

        private void SaveGameHistoryToDb()
        {
            try
            {
                var app = Application.Current as QASmartTouch.App;
                if (app != null)
                {
                    var db = app.Database;
                    if (db != null)
                    {
                        string difficulty = "Easy";
                        if (_rangeMax > 100) difficulty = "Medium";
                        if (_rangeMax > 1000) difficulty = "Hard";

                        string quizType = "Practice";
                        if (_maxQuestions > 0) quizType = "CustomQuiz";

                        string studentCode = "GV";
                        if (txtPlayerName.Text.Contains("Người chơi"))
                        {
                            studentCode = "HS_" + txtPlayerName.Text.Replace("Người chơi ", "");
                        }

                        var history = new MathQuizHistory
                        {
                            StudentCode = studentCode,
                            StudentName = txtPlayerName.Text,
                            Grade = "Tiểu học",
                            ChapterId = "TOAN_TH_BASIC",
                            ChapterName = $"Toán cơ bản (Phạm vi {_rangeMax})",
                            Difficulty = difficulty,
                            TotalQuestions = _maxQuestions > 0 ? _maxQuestions : _currentQuestion,
                            CorrectAnswers = (int)System.Math.Round((double)_score / 10.0),
                            TimeSpentSeconds = 0,
                            QuizType = quizType,
                            CompletedAt = DateTime.Now,
                            ProblemResultsJson = "[]"
                        };

                        db.MathQuizHistories.Add(history);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi lưu database: {ex.Message}");
            }
        }

        // --- EVENT HANDLERS ---
        private void NumKey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string num = btn.Content.ToString();
                if (txtAnswer.Text.Length < 4)
                {
                    txtAnswer.Text += num;
                }
            }
        }

        private void BackspaceKey_Click(object sender, RoutedEventArgs e)
        {
            if (txtAnswer.Text.Length > 0)
            {
                txtAnswer.Text = txtAnswer.Text.Substring(0, txtAnswer.Text.Length - 1);
            }
        }

        private void ClearKey_Click(object sender, RoutedEventArgs e)
        {
            txtAnswer.Text = "";
        }

        private void BtnSubmitCalc_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtAnswer.Text, out int ans))
            {
                ProcessAnswer(ans == _expectedResult);
            }
        }

        private void BtnComp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string choice = btn.Content.ToString();
                ProcessAnswer(choice == _expectedComp);
            }
        }

        private void BtnTeacherCorrect_Click(object sender, RoutedEventArgs e)
        {
            ProcessAnswer(true);
        }

        private void BtnTeacherIncorrect_Click(object sender, RoutedEventArgs e)
        {
            ProcessAnswer(false);
        }

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new System.Collections.Generic.List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "🎮",
                    Title = isVN ? "Học qua chơi & Đấu trí" : "Gamified Math Battles",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_mathplayerzone_1_{suffix}.png",
                    Description = isVN 
                        ? "Học sinh tham gia giải các câu đố tính toán hoặc so sánh số học nhanh dưới dạng trò chơi tương tác, rèn luyện phản xạ tính nhẩm và tư duy nhanh nhạy." 
                        : "Students solve calculation puzzles or fast arithmetic comparisons in an interactive game format, training mental math reflexes and quick thinking."
                },
                new PracticalAppItem
                {
                    Icon = "🧠",
                    Title = isVN ? "Rèn luyện tư duy logic" : "Logical Reasoning Training",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_mathplayerzone_2_{suffix}.png",
                    Description = isVN 
                        ? "Rèn luyện khả năng lập luận toán học và nhận diện quy luật số học thông qua việc giải quyết các bài toán so sánh lớn hơn, nhỏ hơn hoặc bằng một cách trực quan." 
                        : "Train mathematical reasoning and pattern recognition by solving visual comparison problems (greater than, less than, or equal)."
                },
                new PracticalAppItem
                {
                    Icon = "🏆",
                    Title = isVN ? "Bảng xếp hạng & Danh hiệu" : "Class Leaderboard & Medals",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_mathplayerzone_3_{suffix}.png",
                    Description = isVN 
                        ? "Khích lệ học sinh tự học thông qua tích lũy điểm số, đạt chuỗi thắng liên tiếp và vinh danh trên bảng vàng thi đua của lớp học." 
                        : "Encourage students' self-learning by accumulating points, achieving win streaks, and getting recognized on the class leaderboard."
                }
            };

            try
            {
                if (practicalAppViewer != null) practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for MathPlayerZone: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewGame == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewGame.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewGame.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
