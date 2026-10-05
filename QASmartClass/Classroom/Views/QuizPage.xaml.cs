using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Classroom.Services;
using QASmartClass.Classroom.Helpers;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class QuizPage : Page
    {
        // ─── State ──────────────────────────────────────────────
        private enum QuizMode { Solo, Battle, Poll }
        private QuizMode _mode = QuizMode.Solo;

        // Solo
        private DispatcherTimer? _timer;
        private int _timeLeft = 30;
        private int _currentQuestion = 0;
        private int _totalCorrect = 0;
        private List<Question> _questions = new();
        private bool _quizActive = false;
        public bool IsQuizActive => _quizActive;
        private List<string> _answerLog = new();

        // Battle
        private int _battleQuestion = 0;
        private ObservableCollection<BattleGroup> _groups = new();
        private DispatcherTimer? _battleTimer;
        private int _battleTimeLeft = 45;
        private readonly HashSet<string> _battleSubmittedStudents = new();

        // Poll
        private DispatcherTimer? _pollRefreshTimer;
        private int _pollTick = 0;

        // Leaderboard model
        private List<LeaderboardItem> _leaderboard = new();

        // ─── Constructor ────────────────────────────────────────
        public QuizPage()
        {
            InitializeComponent();
            Loaded += async (_, _) => { await LoadQuizFromDB(); InitBattleGroups(); RefreshLeaderboard(force: true); };
        }

        // ═══════════════════════════════════════════════════════
        //  DATA LOADING
        // ═══════════════════════════════════════════════════════

        private async Task LoadQuizFromDB()
        {
            try
            {
                // app → ClassroomAppContext (refactored)
                var quiz = ClassroomAppContext.Db.Quizzes.FirstOrDefault();
                if (quiz != null)
                {
                    _questions = ClassroomAppContext.Db.Questions
                        .Where(q => q.QuizId == quiz.Id)
                        .OrderBy(q => q.SortOrder)
                        .ToList();
                    Log.Information("Quiz loaded: {Title} â€” {Count} questions", quiz.Title, _questions.Count);
                }
            }
            catch (Exception ex) { Log.Warning("LoadQuiz error: {Err}", ex.Message); }

            if (_questions.Count == 0)
            {
                _questions = new List<Question>
                {
                    new() { Content = "H\u1EC7 s\u1ED1 g\u00F3c c\u1EE7a h\u00E0m s\u1ED1 y = 2x + 1 l\u00E0 bao nhi\u00EAu?", OptionsJson = "[\"A. 1\",\"B. 2\",\"C. -1\",\"D. 3\"]", CorrectAnswer = "B", Points = 10 },
                    new() { Content = "H\u00E0m s\u1ED1 y = ax + b \u0111\u1ED3ng bi\u1EBFn khi:", OptionsJson = "[\"A. a > 0\",\"B. a < 0\",\"C. b > 0\",\"D. b < 0\"]", CorrectAnswer = "A", Points = 10 },
                    new() { Content = "\u0110\u1ED3 th\u1ECB h\u00E0m s\u1ED1 y = -x + 2 c\u1EAFt tr\u1EE5c Ox t\u1EA1i \u0111i\u1EC3m c\u00F3 t\u1ECDa \u0111\u1ED9:", OptionsJson = "[\"A. (2, 0)\",\"B. (-2, 0)\",\"C. (0, 2)\",\"D. (1, 0)\"]", CorrectAnswer = "A", Points = 15 },
                    new() { Content = "H\u00E0m s\u1ED1 y = 3x - 6 b\u1EB1ng 0 khi x =", OptionsJson = "[\"A. 1\",\"B. 2\",\"C. 3\",\"D. -2\"]", CorrectAnswer = "B", Points = 15 },
                    new() { Content = "Hai \u0111\u01B0\u1EDDng th\u1EB3ng y = 2x + 1 v\u00E0 y = 2x - 3 c\u00F3 quan h\u1EC7 nh\u01B0 th\u1EBF n\u00E0o?", OptionsJson = "[\"A. C\u1EAFt nhau\",\"B. Song song\",\"C. Tr\u00F9ng nhau\",\"D. Vu\u00F4ng g\u00F3c\"]", CorrectAnswer = "B", Points = 20 },
                };
            }

            // Fix old corrupted data once
            await FixCorruptedQuizData();
        }

        /// <summary>
        /// Delete QuizResults with QuestionId=0 (corrupted by old bug).
        /// These records are unrecoverable because all answers overwrote
        /// each other at key 0 in the answerMap.
        /// </summary>
        private async Task FixCorruptedQuizData()
        {
            try
            {
                // app → ClassroomAppContext (refactored)
                var db = ClassroomAppContext.Db;
                var corrupted = db.QuizResults
                    .Where(r => r.AnswersJson.Contains("\"QuestionId\":0"))
                    .ToList();

                if (corrupted.Any())
                {
                    db.QuizResults.RemoveRange(corrupted);
                    await db.SaveChangesAsync();
                    Log.Information("FixCorruptedQuizData: deleted {Count} corrupted records (QuestionId=0)", corrupted.Count);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("FixCorruptedQuizData error: {Err}", ex.Message);
            }
        }

        private void InitBattleGroups()
        {
            _groups = new ObservableCollection<BattleGroup>
            {
                new() { Name = "Nhóm 1", Score = 0, Status = "Chờ trả lời...",
                    BgColor = "#FFEBEE", FgColor = "#B71C1C" },
                new() { Name = "Nhóm 2", Score = 0, Status = "Chờ trả lời...",
                    BgColor = "#E3F2FD", FgColor = "#0D47A1" },
                new() { Name = "Nhóm 3", Score = 0, Status = "Chờ trả lời...",
                    BgColor = "#E8F5E9", FgColor = "#1B5E20" },
                new() { Name = "Nhóm 4", Score = 0, Status = "Chờ trả lời...",
                    BgColor = "#FFFDE7", FgColor = "#F57F17" },
            };
            groupAnswerPanel.ItemsSource = _groups;
        }

        // ═══════════════════════════════════════════════════════
        //  TAB NAVIGATION
        // ═══════════════════════════════════════════════════════

        private void TabSolo_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => SwitchMode(QuizMode.Solo);
        private void TabBattle_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => SwitchMode(QuizMode.Battle);
        private void TabPoll_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => SwitchMode(QuizMode.Poll);

        private void GoToQuestionBank_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Điều hướng sang QuestionBankPage (F14)
            var shell = Window.GetWindow(this) as ClassroomShell;
            if (shell != null)
            {
                Log.Information("Quiz sub-flow: Navigating to QuestionBank (F14)");
                shell.NavigateTo("F14");
            }
        }

        private void SwitchMode(QuizMode mode)
        {
            _mode = mode;
            StopAll();

            panelSolo.Visibility   = mode == QuizMode.Solo   ? Visibility.Visible : Visibility.Collapsed;
            panelBattle.Visibility = mode == QuizMode.Battle ? Visibility.Visible : Visibility.Collapsed;
            panelPoll.Visibility   = mode == QuizMode.Poll   ? Visibility.Visible : Visibility.Collapsed;

            // Update tab highlight
            tabSolo.Background   = mode == QuizMode.Solo   ? new SolidColorBrush(Color.FromRgb(25, 118, 210)) : Brushes.Transparent;
            tabBattle.Background = mode == QuizMode.Battle ? new SolidColorBrush(Color.FromRgb(123, 31, 162)) : Brushes.Transparent;
            tabPoll.Background   = mode == QuizMode.Poll   ? new SolidColorBrush(Color.FromRgb(0, 137, 123))  : Brushes.Transparent;

            var white = Brushes.White;
            var grey  = new SolidColorBrush(Color.FromRgb(97, 97, 97));

            foreach (var (tab, active) in new[] { (tabSolo, mode == QuizMode.Solo), (tabBattle, mode == QuizMode.Battle), (tabPoll, mode == QuizMode.Poll) })
                if (tab.Child is TextBlock tb) tb.Foreground = active ? white : grey;

            btnStartQuiz.Content = "Bắt đầu";
            btnStartQuiz.Visibility = mode == QuizMode.Poll ? Visibility.Collapsed : Visibility.Visible;
            txtTimer.Visibility = mode == QuizMode.Poll ? Visibility.Collapsed : Visibility.Visible;
            txtProgress.Text = mode switch
            {
                QuizMode.Solo   => $"Sẵn sàng — {_questions.Count} câu",
                QuizMode.Battle => "Sẵn sàng — 4 nhóm thi đua",
                QuizMode.Poll   => "Nhập câu hỏi và nhấn Phát",
                _               => ""
            };
        }

        private void StopAll()
        {
            _timer?.Stop(); _battleTimer?.Stop(); _pollRefreshTimer?.Stop();
            _quizActive = false;

            // app → ClassroomAppContext (refactored)
            if (ClassroomAppContext.Network != null)
            {
                ClassroomAppContext.Network.MessageReceived -= OnNetworkMessageReceived;
            }
        }

        // ═══════════════════════════════════════════════════════
        //  SOLO QUIZ
        // ═══════════════════════════════════════════════════════

        private async void StartQuiz_Click(object sender, RoutedEventArgs e)
        {
            if (_quizActive)
            {
                if (_mode == QuizMode.Battle)
                {
                    await EndBattle();
                }
                else
                {
                    StopQuiz();
                }
                return;
            }

            if (_mode == QuizMode.Battle) { _ = StartBattle(); return; }
            if (_questions.Count == 0) { MessageBox.Show("Chua co cau hoi!"); return; }

            _quizActive = true;
            _currentQuestion = 0;
            _totalCorrect = 0;
            _answerLog.Clear();
            btnStartQuiz.Content = "Dừng";

            _timeLeft = 30;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            ShowQuestion(_currentQuestion);

            // Send quiz to students via network
            try
            {
                // app → ClassroomAppContext (refactored)
                var net = ClassroomAppContext.Network;
                if (net != null)
                {
                    net.MessageReceived -= OnNetworkMessageReceived;
                    net.MessageReceived += OnNetworkMessageReceived;
                }
                var quiz = ClassroomAppContext.Db.Quizzes.FirstOrDefault();
                int quizId = quiz?.Id ?? 0;

                if (net != null && net.IsBroadcasting)
                {
                    await net.StartQuizAsync(quizId);
                    Log.Information("Quiz QUIZ_START sent to all students: quizId={Id}", quizId);

                    var questionsData = _questions.Select(q => new Dictionary<string, object?>
                    {
                        { "Content", q.Content },
                        { "OptionsJson", q.OptionsJson },
                        { "Points", q.Points },
                        { "SortOrder", q.SortOrder },
                        { "ImageUrl", q.ImageUrl },
                        // Lowercase properties for Web App compatibility
                        { "content", q.Content },
                        { "optionsJson", q.OptionsJson },
                        { "points", q.Points },
                        { "sortOrder", q.SortOrder },
                        { "imageUrl", q.ImageUrl },
                        { "image", q.ImageUrl }
                    }).ToList();
                    string questionsJson = System.Text.Json.JsonSerializer.Serialize(questionsData);
                    await net.SendCommandAsync($"CMD|QUIZ_DATA|{quizId}|{questionsJson}");
                    Log.Information("Quiz data sent: {Count} questions, {Len} bytes", _questions.Count, questionsJson.Length);
                }

                // Local command bus
                ClassroomAppContext.DispatchCommand($"CMD|QUIZ_START|{quizId}");

                // Log event
                ClassroomAppContext.Db.EventLogs.Add(new EventLog
                {
                    EventType = "QUIZ_START",
                    Actor = "GV",
                    Details = $"Bat dau Quiz: {quiz?.Title ?? "Demo"} â€” {_questions.Count} cau, {_questions.Sum(q => q.Points)} diem",
                    Timestamp = DateTime.Now
                });
                await ClassroomAppContext.Db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Warning("StartQuiz network error: {Err}", ex.Message);
            }

            Log.Information("Solo quiz started â€” {Count} questions", _questions.Count);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _timeLeft--;
            txtTimer.Text = $"\u23F1 {_timeLeft}s";
            txtTimer.Foreground = _timeLeft <= 5
                ? new SolidColorBrush(Color.FromRgb(244, 67, 54))
                : new SolidColorBrush(Color.FromRgb(26, 35, 126));
            if (_timeLeft <= 0) NextQuestion();
        }

        private void ShowQuestion(int index)
        {
            if (index >= _questions.Count) { _ = ShowResults(); return; }
            var q = _questions[index];
            _timeLeft = 30;
            txtTimer.Text = "\u23F1 30s";
            txtTimer.Foreground = new SolidColorBrush(Color.FromRgb(26, 35, 126));
            txtQuestionNum.Text = $"Câu {index + 1}/{_questions.Count}";
            txtQuestionContent.Text = q.Content;
            try
            {
                var opts = System.Text.Json.JsonSerializer.Deserialize<string[]>(q.OptionsJson);
                if (opts != null)
                {
                    optionA.Content = opts.Length > 0 ? opts[0] : "\u2014";
                    optionB.Content = opts.Length > 1 ? opts[1] : "\u2014";
                    optionC.Content = opts.Length > 2 ? opts[2] : "\u2014";
                    optionD.Content = opts.Length > 3 ? opts[3] : "\u2014";
                }
            }
            catch { }
            foreach (var btn in new[] { optionA, optionB, optionC, optionD })
            {
                btn.Background = Brushes.White;
                btn.IsEnabled = true;
            }
            txtProgress.Text = $"{_totalCorrect} đúng / {index} đã qua";

            // Send current question to student
            try
            {
                // app → ClassroomAppContext (refactored)
                ClassroomAppContext.DispatchCommand($"CMD|QUIZ_QUESTION|{index}|{q.Content}");
            }
            catch { }
        }

        private void OptionA_Click(object sender, RoutedEventArgs e) => CheckAnswer("A", optionA);
        private void OptionB_Click(object sender, RoutedEventArgs e) => CheckAnswer("B", optionB);
        private void OptionC_Click(object sender, RoutedEventArgs e) => CheckAnswer("C", optionC);
        private void OptionD_Click(object sender, RoutedEventArgs e) => CheckAnswer("D", optionD);

        private async void CheckAnswer(string answer, Button selected)
        {
            if (_currentQuestion >= _questions.Count) return;
            var q = _questions[_currentQuestion];
            bool correct = q.CorrectAnswer.Equals(answer, StringComparison.OrdinalIgnoreCase);
            foreach (var btn in new[] { optionA, optionB, optionC, optionD }) btn.IsEnabled = false;

            selected.Background = correct
                ? new SolidColorBrush(Color.FromRgb(200, 230, 201))
                : new SolidColorBrush(Color.FromRgb(255, 205, 210));

            var correctBtn = q.CorrectAnswer.ToUpper() switch
            { "A" => optionA, "B" => optionB, "C" => optionC, "D" => optionD, _ => null };
            if (correctBtn != null)
                correctBtn.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201));

            if (correct) _totalCorrect++;

            _answerLog.Add($"Q{_currentQuestion + 1}: {answer} ({(correct ? "Đúng" : "Sai")}) — {q.Content}");

            TrySaveResult(q, correct, answer);

            var delay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1400) };
            delay.Tick += (s, e2) => { delay.Stop(); NextQuestion(); };
            delay.Start();

            UpdateLiveStats();
        }

        private void TrySaveResult(Question q, bool correct, string answer = "")
        {
            try
            {
                // app → ClassroomAppContext (refactored)

                int studentId = 0; // Giáo viên thực hiện trên bảng tương tác -> gán ID mặc định 0

                var result = new QuizResult
                {
                    QuizId = q.QuizId,
                    StudentId = studentId,
                    Score = correct ? q.Points : 0,
                    TotalPoints = q.Points,
                    CorrectCount = correct ? 1 : 0,
                    TotalQuestions = 1,
                    AnswersJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        QuestionId = q.Id,
                        Correct = correct,
                        Points = correct ? q.Points : 0,
                        Answer = answer,
                        TimeTaken = 30 - _timeLeft
                    }),
                    TimeSpentSeconds = 30 - _timeLeft,
                    SubmittedAt = DateTime.Now
                };
                ClassroomAppContext.Db.QuizResults.Add(result);
                ClassroomAppContext.Db.SaveChanges();
            }
            catch { /* non-critical */ }
        }

        private void NextQuestion()
        {
            _currentQuestion++;
            if (_currentQuestion >= _questions.Count) _ = ShowResults();
            else ShowQuestion(_currentQuestion);
        }

        private async Task ShowResults()
        {
            StopQuiz();
            int total = _questions.Sum(q => q.Points);
            int earned = _questions.Count > 0 ? (int)((double)_totalCorrect / _questions.Count * total) : 0;
            double percent = _questions.Count > 0 ? _totalCorrect * 100.0 / _questions.Count : 0;

            string grade = percent >= 90 ? "XUẤT SẮC" :
                           percent >= 70 ? "Tốt" :
                           percent >= 50 ? "Trung bình" : "Cần cải thiện";

            var sb = new StringBuilder();
            sb.AppendLine("KẾT QUẢ QUIZ\n");
            sb.AppendLine($"Đúng: {_totalCorrect}/{_questions.Count}");
            sb.AppendLine($"Điểm: {earned}/{total} ({percent:F0}%)");
            sb.AppendLine($"Xếp loại: {grade}\n");
            sb.AppendLine("--- Chi tiết từng câu ---");
            foreach (var log in _answerLog)
                sb.AppendLine(log);

            ClassroomDialog.Info(sb.ToString(), "Kết quả Quiz");

            // Send QUIZ_END to students
            try
            {
                // app → ClassroomAppContext (refactored)
                var net = ClassroomAppContext.Network;
                if (net != null && net.IsBroadcasting)
                {
                    await net.EndQuizAsync();
                    Log.Information("QUIZ_END sent to students");
                }

                ClassroomAppContext.DispatchCommand("CMD|QUIZ_END|0");

                ClassroomAppContext.Db.EventLogs.Add(new EventLog
                {
                    EventType = "QUIZ_END",
                    Actor = "GV",
                    Details = $"Quiz kết thúc — Đúng: {_totalCorrect}/{_questions.Count} — Điểm: {earned}/{total} — {grade}",
                    Timestamp = DateTime.Now
                });

                var quiz = ClassroomAppContext.Db.Quizzes.FirstOrDefault();
                ClassroomAppContext.Db.QuizResults.Add(new QuizResult
                {
                    QuizId = quiz?.Id ?? 0,
                    StudentId = 0,
                    Score = earned,
                    TotalPoints = total,
                    CorrectCount = _totalCorrect,
                    TotalQuestions = _questions.Count,
                    AnswersJson = System.Text.Json.JsonSerializer.Serialize(_answerLog),
                    TimeSpentSeconds = 30 * _questions.Count,
                    SubmittedAt = DateTime.Now
                });

                await ClassroomAppContext.Db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Warning("ShowResults network/DB error: {Err}", ex.Message);
            }

            RefreshLeaderboard(force: true);
        }

        private void StopQuiz()
        {
            _timer?.Stop();
            _quizActive = false;
            btnStartQuiz.Content = "Bắt đầu";

            // app → ClassroomAppContext (refactored)
            if (ClassroomAppContext.Network != null)
            {
                ClassroomAppContext.Network.MessageReceived -= OnNetworkMessageReceived;
            }
        }

        // ═══════════════════════════════════════════════════════
        //  BATTLE MODE
        // ═══════════════════════════════════════════════════════

        private async Task StartBattle()
        {
            _battleQuestion = 0;
            foreach (var g in _groups) { g.Score = 0; g.Status = "Cho tra loi..."; }
            ShowBattleQuestion();
            btnStartQuiz.Content = "Dung Battle";

            try
            {
                // app → ClassroomAppContext (refactored)
                var net = ClassroomAppContext.Network;
                var quiz = ClassroomAppContext.Db.Quizzes.FirstOrDefault();
                if (net != null && net.IsBroadcasting)
                {
                    await net.StartQuizAsync(quiz?.Id ?? 0);
                }
                ClassroomAppContext.DispatchCommand($"CMD|QUIZ_START|{quiz?.Id ?? 0}");

                if (net != null)
                {
                    net.MessageReceived -= OnNetworkMessageReceived;
                    net.MessageReceived += OnNetworkMessageReceived;
                }
                _quizActive = true;

                ClassroomAppContext.Db.EventLogs.Add(new EventLog
                {
                    EventType = "QUIZ_BATTLE_START",
                    Actor = "GV",
                    Details = $"Quiz Battle bat dau â€” {_questions.Count} cau, 4 nhom",
                    Timestamp = DateTime.Now
                });
                await ClassroomAppContext.Db.SaveChangesAsync();
            }
            catch (Exception ex) { Log.Warning("StartBattle network error: {Err}", ex.Message); }

            Log.Information("Quiz Battle started");
        }

        private void ShowBattleQuestion()
        {
            if (_battleQuestion >= _questions.Count)
            {
                _ = EndBattle();
                return;
            }

            _battleSubmittedStudents.Clear();

            var q = _questions[_battleQuestion];
            txtBattleQuestion.Text = $"Q{_battleQuestion + 1}: {q.Content}";
            txtBattleQNum.Text = $"Cau {_battleQuestion + 1}/{_questions.Count}";
            txtBattleSubmitted.Text = "Da nop: 0/4 nhom";

            foreach (var g in _groups) g.Status = "Dang tra loi...";

            _battleTimeLeft = 20;
            _battleTimer?.Stop();
            _battleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _battleTimer.Tick += (s, e) =>
            {
                _battleTimeLeft--;
                txtTimer.Text = $"\u23F1 {_battleTimeLeft}s";

                if (_battleTimeLeft <= 0)
                {
                    _battleTimer?.Stop();
                    foreach (var g in _groups)
                    {
                        if (g.Status.Contains("Dang tra loi") || g.Status.Contains("Cho tra loi"))
                        {
                            g.Status = "Tre gio";
                        }
                    }
                }
            };
            _battleTimer.Start();
        }

        private void GroupAnswer_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is BattleGroup g)
            {
                if (g.Status.Contains("Dung") || g.Status.Contains("Sai")) return;
                int q_points = _battleQuestion < _questions.Count ? _questions[_battleQuestion].Points : 10;
                int targetScore = g.Score + q_points + _battleTimeLeft;
                g.Status = $"Dung! +{q_points + _battleTimeLeft}d";
                _ = AnimateGroupScoreAsync(g, targetScore);
                UpdateBattleLeaderboard();
            }
        }

        private async Task AnimateGroupScoreAsync(BattleGroup group, int targetScore)
        {
            int startScore = group.Score;
            if (startScore == targetScore)
            {
                return;
            }

            int steps = 15;
            int stepDelayMs = 40;

            for (int i = 1; i <= steps; i++)
            {
                double progress = (double)i / steps;
                progress = progress * (2 - progress); // EaseOutQuad
                group.Score = (int)Math.Round(startScore + (targetScore - startScore) * progress);
                
                await Task.Delay(stepDelayMs);
            }
            group.Score = targetScore;
        }

        private void NextBattleQuestion_Click(object sender, RoutedEventArgs e)
        {
            _battleTimer?.Stop();
            _battleQuestion++;
            ShowBattleQuestion();
        }

        private async Task EndBattle()
        {
            _battleTimer?.Stop();
            UpdateBattleLeaderboard();
            await SaveBattleResults();

            // app → ClassroomAppContext (refactored)
            if (ClassroomAppContext.Network != null)
            {
                ClassroomAppContext.Network.MessageReceived -= OnNetworkMessageReceived;
            }
            _quizActive = false;
            btnStartQuiz.Content = "Bắt đầu";

            var winner = _groups.OrderByDescending(g => g.Score).First();
            var sb = new StringBuilder();
            sb.AppendLine("BATTLE KET THUC!\n");
            sb.AppendLine($"{winner.Name} chien thang voi {winner.Score} diem!\n");
            sb.AppendLine("--- Bang xep hang ---");
            foreach (var (g, i) in _groups.OrderByDescending(g => g.Score).Select((g, i) => (g, i)))
                sb.AppendLine($"  {i + 1}. {g.Name}: {g.Score}d");

            ClassroomDialog.Info(sb.ToString(), "Quiz Battle");

            try
            {
                var net = ClassroomAppContext.Network;
                if (net != null && net.IsBroadcasting)
                    await net.EndQuizAsync();
                ClassroomAppContext.DispatchCommand("CMD|QUIZ_END|0");
            }
            catch (Exception ex) { Log.Warning("EndBattle network error: {Err}", ex.Message); }

            Log.Information("Quiz Battle ended, winner: {Name} {Score}", winner.Name, winner.Score);
        }

        /// <summary>Save battle group scores as QuizResults + EventLog</summary>
        private async Task SaveBattleResults()
        {
            try
            {
                var db = ClassroomAppContext.Db;
                var quiz = db.Quizzes.FirstOrDefault();
                int quizId = quiz?.Id ?? 0;

                var details = string.Join(" | ",
                    _groups.OrderByDescending(g => g.Score)
                           .Select((g, i) => $"{i + 1}. {g.Name}: {g.Score}d"));
                db.EventLogs.Add(new EventLog
                {
                    EventType = "QUIZ_BATTLE_END",
                    Actor     = "GV",
                    Details   = $"Quiz Battle ket thuc â€” {details}",
                    Timestamp = DateTime.Now
                });

                foreach (var g in _groups)
                {
                    db.QuizResults.Add(new QuizResult
                    {
                        QuizId         = quizId,
                        StudentId      = 0,
                        Score          = g.Score,
                        TotalPoints    = _questions.Sum(q => q.Points),
                        CorrectCount   = _groups.Count(x => x.Score > 0),
                        TotalQuestions = _questions.Count,
                        AnswersJson    = System.Text.Json.JsonSerializer.Serialize(new { g.Name, g.Score }),
                        SubmittedAt    = DateTime.Now
                    });
                }

                await db.SaveChangesAsync();
                Log.Information("Battle results saved to DB: {Count} groups", _groups.Count);
            }
            catch (Exception ex) { Log.Warning("SaveBattleResults error: {Err}", ex.Message); }
        }

        private void OnNetworkMessageReceived(object? sender, StudentMessageEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Message)) return;

            if (_quizActive && e.Message.StartsWith("QUIZ_ANSWER|"))
            {
                if (_mode == QuizMode.Battle)
                {
                    Dispatcher.Invoke(() =>
                    {
                        ProcessIncomingAnswer(e.StudentCode, e.Message);
                    });
                }
                else if (_mode == QuizMode.Solo)
                {
                    Dispatcher.Invoke(async () =>
                    {
                        await ProcessSoloIncomingAnswerAsync(e.StudentCode, e.Message);
                    });
                }
            }
        }

        private async Task ProcessSoloIncomingAnswerAsync(string studentCode, string message)
        {
            try
            {
                var parts = message.Split('|', 3);
                if (parts.Length < 3) return;

                if (!int.TryParse(parts[1], out int quizId)) return;
                string answersJson = parts[2];

                // app → ClassroomAppContext (refactored)
                if (ClassroomAppContext.Db == null) return;

                var quiz = ClassroomAppContext.Db.Quizzes.Find(quizId);
                if (quiz == null) return;

                string subject = "";
                var lesson = ClassroomAppContext.Db.Lessons.Find(quiz.LessonId);
                if (lesson != null)
                {
                    subject = lesson.Subject ?? "";
                }

                var details = QASmartClass.Helpers.QuizAnswerParser.ParseAnswers(answersJson);

                int correctCount = 0;
                int totalPoints = 0;
                int earnedPoints = 0;

                var correctAnswersMap = new Dictionary<string, string>();

                foreach (var q in _questions)
                {
                    totalPoints += q.Points;
                    correctAnswersMap[q.Id.ToString()] = q.CorrectAnswer ?? string.Empty;

                    var detail = details.FirstOrDefault(d => d.QuestionId == q.Id);
                    if (detail != null)
                    {
                        bool correct = QASmartClass.Helpers.QuizAnswerParser.GradeAnswer(detail.Answer, q.CorrectAnswer, q.QuestionType, subject);
                        if (correct)
                        {
                            correctCount++;
                            earnedPoints += q.Points;
                        }
                    }
                }

                int finalScore = totalPoints > 0 ? (int)Math.Round((double)earnedPoints / totalPoints * 100) : 0;

                // Find student ID on teacher side
                var student = ClassroomAppContext.Db.Students.FirstOrDefault(s => s.StudentCode == studentCode);
                int studentId = student?.Id ?? 0;

                if (studentId > 0)
                {
                    // Save to QuizResults on teacher side
                    var existingResult = ClassroomAppContext.Db.QuizResults.FirstOrDefault(r => r.QuizId == quizId && r.StudentId == studentId);
                    if (existingResult != null)
                    {
                        existingResult.Score = finalScore;
                        existingResult.CorrectCount = correctCount;
                        existingResult.TotalQuestions = _questions.Count;
                        existingResult.TotalPoints = totalPoints;
                        existingResult.AnswersJson = answersJson;
                        existingResult.SubmittedAt = DateTime.Now;
                    }
                    else
                    {
                        var result = new QuizResult
                        {
                            QuizId = quizId,
                            StudentId = studentId,
                            Score = finalScore,
                            TotalPoints = totalPoints,
                            CorrectCount = correctCount,
                            TotalQuestions = _questions.Count,
                            TimeSpentSeconds = 0,
                            AnswersJson = answersJson,
                            SubmittedAt = DateTime.Now
                        };
                        ClassroomAppContext.Db.QuizResults.Add(result);
                    }

                    // Save to StudentGrades on teacher side
                    string className = student.ClassName ?? "";
                    var roster = ClassroomAppContext.Db.ClassRosters.FirstOrDefault(r => r.IsActive && r.ClassName == className);
                    if (roster != null)
                    {
                        var gradeType = ClassroomAppContext.Db.GradeTypeMasters.FirstOrDefault(g => g.Code == "Quiz" || g.ShortName == "15p") 
                                     ?? ClassroomAppContext.Db.GradeTypeMasters.FirstOrDefault();

                        if (gradeType != null)
                        {
                            double scale10 = totalPoints > 0 ? Math.Round((double)earnedPoints / totalPoints * 10.0, 1) : 0;
                            
                            bool isConfirmed = true;
                            bool ignoreAccents = QASmartClass.Helpers.QuizAnswerParser.IsNaturalScienceSubject(subject);
                            if (!ignoreAccents)
                            {
                                if (_questions.Any(q => (q.QuestionType ?? "").ToLower() == "short" || 
                                                       (q.QuestionType ?? "").ToLower() == "shortanswer" || 
                                                       (q.QuestionType ?? "").ToLower() == "short_answer"))
                                {
                                    isConfirmed = false;
                                }
                            }

                            ClassroomAppContext.Db.StudentGrades.Add(new Data.StudentGrade
                            {
                                StudentId = studentId,
                                RosterId = roster.Id,
                                GradeTypeId = gradeType.Id,
                                Attempt = 1,
                                Score = scale10,
                                Notes = $"Auto-graded từ Quiz '{quiz.Title}' ({earnedPoints}/{totalPoints}đ)" + (isConfirmed ? "" : " [Chờ GV duyệt tự luận]"),
                                IsConfirmed = isConfirmed
                            });
                        }
                    }

                    await ClassroomAppContext.Db.SaveChangesAsync();
                }

                // Send the private grade payload back to student client
                string correctAnswersJson = System.Text.Json.JsonSerializer.Serialize(correctAnswersMap);
                var net = ClassroomAppContext.Network;
                if (net != null)
                {
                    await net.SendToStudentAsync(studentCode, $"CMD|QUIZ_GRADE|{quizId}|{finalScore}|{correctCount}|{_questions.Count}|{correctAnswersJson}");
                    Log.Information("Processed Solo Quiz Answer for student {Code}: Score {Score}, CorrectCount {CorrectCount}", studentCode, finalScore, correctCount);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error in ProcessSoloIncomingAnswerAsync: {Err}", ex.Message);
            }
        }

        private void ProcessIncomingAnswer(string studentCode, string message)
        {
            if (!_quizActive || _mode != QuizMode.Battle) return;
            if (_battleQuestion >= _questions.Count) return;

            if (_battleSubmittedStudents.Contains(studentCode)) return;

            var parts = message.Split('|');
            if (parts.Length < 3) return;
            string answersJson = parts[2];

            var q = _questions[_battleQuestion];
            var details = QASmartClass.Helpers.QuizAnswerParser.ParseAnswers(answersJson);
            var detail = details.FirstOrDefault(d => d.QuestionId == q.Id || d.QuestionId == (_battleQuestion + 1));

            if (detail != null)
            {
                _battleSubmittedStudents.Add(studentCode);

                int groupIndex = Math.Abs(studentCode.GetHashCode()) % 4;
                var g = _groups[groupIndex];

                if (g.Status.Contains("Dang tra loi") || g.Status.Contains("Cho tra loi"))
                {
                    bool correct = QASmartClass.Helpers.QuizAnswerParser.GradeAnswer(detail.Answer, q.CorrectAnswer, q.QuestionType);
                    int points = correct ? (q.Points + Math.Max(0, _battleTimeLeft)) : 0;

                    int targetScore = g.Score + points;
                    g.Status = correct ? $"Dung! +{points}d" : "Sai";

                    int submittedCount = _groups.Count(x => !x.Status.Contains("Dang tra loi") && !x.Status.Contains("Cho tra loi"));
                    txtBattleSubmitted.Text = $"Da nop: {submittedCount}/4 nhom";

                    _ = AnimateGroupScoreAsync(g, targetScore);
                    UpdateBattleLeaderboard();

                    if (submittedCount >= 4)
                    {
                        _battleTimer?.Stop();
                    }
                }
            }
        }

        private void UpdateBattleLeaderboard()
        {
            var sorted = _groups.OrderByDescending(g => g.Score).ToList();
            var medals = new[] { "\U0001F947", "\U0001F948", "\U0001F949", "4" };
            var bgs = new[] { "#FFF8E1", "#ECEFF1", "#EFEBE9", "#F5F5F5" };
            _leaderboard = sorted.Select((g, i) => new LeaderboardItem
            {
                Name      = g.Name,
                Medal     = medals[i],
                ScoreText = $"{g.Score}d",
                Detail    = $"Nhom {i + 1}",
                CardBg    = bgs[i],
                ScoreFg   = "#F57F17"
            }).ToList();
            leaderboardList.ItemsSource = null;
            leaderboardList.ItemsSource = _leaderboard;
        }

        // ═══════════════════════════════════════════════════════
        //  QUICK POLL
        // ═══════════════════════════════════════════════════════

        private string _activePollId = "";
        private string[] _activePollOpts = Array.Empty<string>();

        private async void LaunchPoll_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPollQuestion.Text)) return;

            pollResultsPanel.Visibility = Visibility.Visible;
            _pollTick = 0;

            string question = txtPollQuestion.Text;
            string opt1 = pollOpt1.Text, opt2 = pollOpt2.Text, opt3 = pollOpt3.Text, opt4 = pollOpt4.Text;
            _activePollOpts = new[] { opt1, opt2, opt3, opt4 };

            // Generate unique poll ID
            _activePollId = $"POLL_{DateTime.Now:yyyyMMdd_HHmmss}";

            // Save poll state to App for students to read
            // app → ClassroomAppContext (refactored)
            string targetClasses = "ALL";
            if (ClassroomAppContext.ClassRoster?.ActiveRoster != null)
            {
                targetClasses = ClassroomAppContext.ClassRoster.ActiveRoster.ClassName;
            }

            QASmartTouch.App.AssessmentState.ActivePollId = _activePollId;
            QASmartTouch.App.AssessmentState.ActivePollOptions = _activePollOpts;
            QASmartTouch.App.AssessmentState.SurveyAnswered = false;
            QASmartTouch.App.AssessmentState.ActiveSurveyQuestion = $"CMD|SURVEY_CUSTOM|0|{targetClasses}|{question}|{opt1}|{opt2}|{opt3}|{opt4}";
            QASmartTouch.App.AssessmentState.ActiveSurveyTime = DateTime.Now;

            // Send Poll to students via network + local bus
            try
            {
                var net = ClassroomAppContext.Network;
                if (net != null && net.IsBroadcasting)
                {
                    await net.SendCommandAsync($"CMD|SURVEY_CUSTOM|0|{targetClasses}|{question}|{opt1}|{opt2}|{opt3}|{opt4}");
                    Log.Information("Poll sent to students via network: {Q} | Target: {Target}", question, targetClasses);
                }

                ClassroomAppContext.DispatchCommand($"CMD|SURVEY_CUSTOM|0|{targetClasses}|{question}|{opt1}|{opt2}|{opt3}|{opt4}");

                // Log poll start
                ClassroomAppContext.Db.EventLogs.Add(new EventLog
                {
                    EventType = "POLL_START",
                    Actor = "GV",
                    Details = $"PollId:{_activePollId}|Q:{question}|{opt1}|{opt2}|{opt3}|{opt4}",
                    Timestamp = DateTime.Now
                });
                await ClassroomAppContext.Db.SaveChangesAsync();
            }
            catch (Exception ex) { Log.Warning("LaunchPoll network error: {Err}", ex.Message); }

            // Init result items
            var results = _activePollOpts.Select(o => new PollResult { Label = o, Votes = 0, Percent = 0, BarWidth = 0 }).ToList();
            pollResultsList.ItemsSource = results;

            // Get max voters count
            int maxVoters = 0;
            try
            {
                var students = ClassroomAppContext.Network?.GetConnectedStudents();
                maxVoters = students?.Count ?? 0;
            }
            catch { }
            if (maxVoters <= 0)
            {
                maxVoters = ClassroomAppContext.Db.Students.Count();
                if (maxVoters <= 0) maxVoters = 35;
            }

            int localMaxVoters = maxVoters;

            // Timer reads REAL votes from DB every 1.5 seconds
            _pollRefreshTimer?.Stop();
            _pollRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
            _pollRefreshTimer.Tick += async (s, ev) =>
            {
                _pollTick++;
                try
                {
                    // Read POLL_VOTE events from DB that match this poll session
                    var db = ClassroomAppContext.Db;
                    var votes = await db.EventLogs
                        .Where(el => el.EventType == "POLL_VOTE" && el.Details.Contains($"PollId:{_activePollId}"))
                        .ToListAsync();

                    int totalVotes = votes.Count;

                    // Count votes per option
                    for (int i = 0; i < results.Count; i++)
                    {
                        string optKey = $"opt{i}";
                        results[i].Votes = votes.Count(v => v.Details.Contains($"|{optKey}|"));
                    }

                    // Calculate percentages and bar widths
                    for (int i = 0; i < results.Count; i++)
                    {
                        results[i].Percent = totalVotes > 0 ? results[i].Votes * 100 / totalVotes : 0;
                        results[i].BarWidth = totalVotes > 0 ? Math.Max(4, results[i].Votes * 240 / totalVotes) : 4;
                    }

                    pollResultsList.ItemsSource = null;
                    pollResultsList.ItemsSource = results;
                    txtStatSubmitted.Text = $"Da binh chon: {totalVotes}/{localMaxVoters} HS";

                    // Auto-stop when all voted or timeout (90 ticks = ~2.25 min)
                    if (totalVotes >= localMaxVoters || _pollTick > 90)
                    {
                        _pollRefreshTimer?.Stop();
                        await SavePollToEventLog(question, results, totalVotes);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Poll refresh error: {Err}", ex.Message);
                }
            };
            _pollRefreshTimer.Start();

            ClassroomDialog.Info(
                $"Poll da phat toi tat ca HS!\n\nCau hoi: {question}\n\n" +
                $"Tong so HS: {localMaxVoters}\n" +
                $"Poll ID: {_activePollId}\n\n" +
                $"Ket qua se tu dong cap nhat khi HS tra loi.", "Quick Poll");
            Log.Information("Poll launched: {Q}, PollId={Id}, MaxVoters={Max}", question, _activePollId, localMaxVoters);
        }

        /// <summary>Persist final poll results to EventLog</summary>
        private async Task SavePollToEventLog(string question, List<PollResult> results, int totalVotes)
        {
            try
            {
                var db = ClassroomAppContext.Db;
                var sb = new StringBuilder();
                sb.AppendLine($"Poll: {question}");
                sb.AppendLine($"PollId: {_activePollId}");
                foreach (var r in results)
                    sb.AppendLine($"  {r.Label}: {r.Votes} phieu ({r.Percent}%)");
                sb.AppendLine($"Tong: {totalVotes} HS");

                db.EventLogs.Add(new EventLog
                {
                    EventType = "POLL_END",
                    Actor     = "GV",
                    Details   = sb.ToString(),
                    Timestamp = DateTime.Now
                });
                await db.SaveChangesAsync();

                // Clear active poll state
                // app → ClassroomAppContext (refactored)
                QASmartTouch.App.AssessmentState.ActivePollId = string.Empty;
                QASmartTouch.App.AssessmentState.ActivePollOptions = Array.Empty<string>();

                Log.Information("Poll results saved: {Question}, {Total} votes", question, totalVotes);
            }
            catch (Exception ex) { Log.Warning("SavePollToEventLog error: {Err}", ex.Message); }
        }

        // ═══════════════════════════════════════════════════════
        //  LEADERBOARD (from DB QuizResults)
        // ═══════════════════════════════════════════════════════

        private void RefreshLeaderboard_Click(object sender, RoutedEventArgs e) => RefreshLeaderboard(force: true);

        private async void RefreshLeaderboard(bool force = false)
        {
            if (_mode == QuizMode.Battle) return;
            try
            {
                // app → ClassroomAppContext (refactored)
                var db  = ClassroomAppContext.Db;

                var top = await db.QuizResults
                    .GroupBy(r => r.StudentId)
                    .Select(g => new 
                    { 
                        StudentId = g.Key, 
                        Total = g.Sum(r => r.Score), 
                        Count = g.Count(),
                        StudentName = db.Students.Where(s => s.Id == g.Key).Select(s => s.FullName).FirstOrDefault()
                    })
                    .OrderByDescending(x => x.Total)
                    .Take(8)
                    .ToListAsync();

                var medals = new[] { "\U0001F947", "\U0001F948", "\U0001F949", "4", "5", "6", "7", "8" };
                var bgs    = new[] { "#FFF8E1", "#ECEFF1", "#EFEBE9", "#F5F5F5",
                                     "#F5F5F5", "#F5F5F5", "#F5F5F5", "#F5F5F5" };

                _leaderboard = top.Select((x, i) =>
                {
                    return new LeaderboardItem
                    {
                        StudentId = x.StudentId,
                        Name      = x.StudentName ?? $"HS #{x.StudentId}",
                        Medal     = medals[i],
                        ScoreText = $"{x.Total}d",
                        Detail    = $"{x.Count} bai",
                        CardBg    = bgs[i],
                        ScoreFg   = "#F57F17"
                    };
                }).ToList();

                if (!_leaderboard.Any())
                {
                    _leaderboard = new List<LeaderboardItem>
                    {
                        new() { Name="Nguyen Van An", Medal="\U0001F947", ScoreText="95d", Detail="5/5", CardBg="#FFF8E1", ScoreFg="#F57F17"},
                        new() { Name="Tran Thi Binh",  Medal="\U0001F948", ScoreText="80d", Detail="4/5", CardBg="#ECEFF1", ScoreFg="#607D8B"},
                        new() { Name="Le Hoang Cuong", Medal="\U0001F949", ScoreText="65d", Detail="3/5", CardBg="#EFEBE9", ScoreFg="#795548"},
                    };
                }

                leaderboardList.ItemsSource = null;
                leaderboardList.ItemsSource = _leaderboard;

                // Stats
                int totalResults = db.QuizResults.Count();
                int passing = db.QuizResults.Count(r => r.Score > 0);
                int avgScore = totalResults > 0 ? (int)db.QuizResults.Average(r => r.Score) : 0;
                txtStatSubmitted.Text = $"Nop bai: {totalResults}";
                txtStatCorrect.Text   = totalResults > 0 ? $"Tra loi dung: {passing * 100 / totalResults}%" : "\u2014";
                txtStatTime.Text      = totalResults > 0 ? $"Diem TB: {avgScore}d" : "\u2014";

                // Also refresh error report
                RefreshErrorReport();
            }
            catch (Exception ex)
            {
                Log.Warning("RefreshLeaderboard error: {Err}", ex.Message);
            }
        }

        private void UpdateLiveStats()
        {
            try
            {
                int total = _questions.Count;
                int done  = _currentQuestion + 1;
                txtStatSubmitted.Text = $"Tien do: {done}/{total}";
                txtStatCorrect.Text   = done > 0 ? $"Dung: {_totalCorrect * 100 / done}%" : "\u2014";
                txtStatTime.Text      = $"Con: {_timeLeft}s";
            }
            catch { }
        }

        // ═══════════════════════════════════════════════════════
        //  BAO CAO CAU SAI â€” Error Report (sorted most wrong first)
        // ═══════════════════════════════════════════════════════

        private void RefreshErrorReport_Click(object sender, RoutedEventArgs e) => RefreshErrorReport();

        private async void RefreshErrorReport()
        {
            try
            {
                // app → ClassroomAppContext (refactored)
                var db = ClassroomAppContext.Db;

                var allResults = await db.QuizResults.ToListAsync();
                if (!allResults.Any())
                {
                    txtErrorSummary.Text = "Chua co du lieu bai lam";
                    errorReportList.ItemsSource = null;
                    return;
                }

                var allQuestions = await db.Questions.ToListAsync();
                string subject = "";
                try
                {
                    var quiz = db.Quizzes.FirstOrDefault();
                    if (quiz != null)
                    {
                        var lesson = db.Lessons.Find(quiz.LessonId);
                        if (lesson != null) subject = lesson.Subject ?? "";
                    }
                }
                catch { }

                // Parse all answers and group by question index
                var questionStats = new Dictionary<int, (int Total, int Wrong, string Content, string Correct, string QuestionType)>();

                foreach (var r in allResults)
                {
                    try
                    {
                        var details = QASmartClass.Helpers.QuizAnswerParser.ParseAnswers(r.AnswersJson);
                        foreach (var detail in details)
                        {
                            int qIdx = detail.QuestionId;
                            if (qIdx <= 0) continue;

                            if (!questionStats.ContainsKey(qIdx))
                            {
                                // Try to get question content from DB or from local list
                                string content = "";
                                string correctAns = "";
                                string qType = "MultipleChoice";
                                var dbQ = allQuestions.FirstOrDefault(q => q.Id == qIdx);
                                if (dbQ != null)
                                {
                                    content = dbQ.Content;
                                    correctAns = dbQ.CorrectAnswer;
                                    qType = dbQ.QuestionType;
                                }
                                else if (qIdx <= _questions.Count)
                                {
                                    content = _questions[qIdx - 1].Content;
                                    correctAns = _questions[qIdx - 1].CorrectAnswer;
                                    qType = _questions[qIdx - 1].QuestionType;
                                }
                                questionStats[qIdx] = (0, 0, content, correctAns, qType);
                            }

                            var stat = questionStats[qIdx];
                            int newTotal = stat.Total + 1;
                            
                            // Re-evaluate correctness to be safe using unified GradeAnswer
                            bool isCorrect = QASmartClass.Helpers.QuizAnswerParser.GradeAnswer(detail.Answer, stat.Correct, stat.QuestionType, subject);
                            int newWrong = stat.Wrong + (isCorrect ? 0 : 1);
                            
                            questionStats[qIdx] = (newTotal, newWrong, stat.Content, stat.Correct, stat.QuestionType);
                        }
                    }
                    catch { }
                }

                if (!questionStats.Any())
                {
                    txtErrorSummary.Text = "Chua co du lieu phan tich";
                    errorReportList.ItemsSource = null;
                    return;
                }

                // Sort by wrong count descending (most errors first)
                var sorted = questionStats
                    .OrderByDescending(kv => kv.Value.Wrong)
                    .ThenByDescending(kv => questionStats.Count > 0
                        ? (double)kv.Value.Wrong / Math.Max(kv.Value.Total, 1) : 0)
                    .ToList();

                int totalQuestions = sorted.Count;
                int totalAnswers = sorted.Sum(kv => kv.Value.Total);
                int totalWrong = sorted.Sum(kv => kv.Value.Wrong);
                double overallErrorRate = totalAnswers > 0 ? (double)totalWrong * 100 / totalAnswers : 0;

                txtErrorSummary.Text = $"{totalQuestions} cau | {totalWrong}/{totalAnswers} luot sai ({overallErrorRate:F0}%)";

                // Color scale for rank badges
                string[] rankBgs = { "#C62828", "#D32F2F", "#E53935", "#EF5350", "#EF9A9A",
                                     "#FF9800", "#FFA726", "#FFB74D", "#FFE0B2", "#E0E0E0" };

                double maxBarWidth = 120;
                int maxWrong = sorted.Any() ? sorted.Max(kv => kv.Value.Wrong) : 1;

                var items = sorted.Select((kv, idx) =>
                {
                    int qIdx = kv.Key;
                    var stat = kv.Value;
                    double errorRate = stat.Total > 0 ? (double)stat.Wrong * 100 / stat.Total : 0;
                    double barW = maxWrong > 0 ? (double)stat.Wrong / maxWrong * maxBarWidth : 0;

                    // Truncate content
                    string label = stat.Content.Length > 0
                        ? $"C{qIdx}: {(stat.Content.Length > 25 ? stat.Content.Substring(0, 25) + "..." : stat.Content)}"
                        : $"Cau {qIdx}";

                    // Color based on error rate
                    string barColor, cardBg, borderColor, percentColor;
                    if (errorRate >= 70) { barColor = "#C62828"; cardBg = "#FFEBEE"; borderColor = "#FFCDD2"; percentColor = "#C62828"; }
                    else if (errorRate >= 50) { barColor = "#E65100"; cardBg = "#FFF3E0"; borderColor = "#FFE0B2"; percentColor = "#E65100"; }
                    else if (errorRate >= 30) { barColor = "#F9A825"; cardBg = "#FFFDE7"; borderColor = "#FFF9C4"; percentColor = "#F57F17"; }
                    else { barColor = "#66BB6A"; cardBg = "#E8F5E9"; borderColor = "#C8E6C9"; percentColor = "#2E7D32"; }

                    string rkBg = idx < rankBgs.Length ? rankBgs[idx] : "#BDBDBD";

                    return new QuestionErrorItem
                    {
                        Rank = $"{idx + 1}",
                        RankBg = rkBg,
                        QuestionLabel = label,
                        ErrorPercent = $"{errorRate:F0}%",
                        DetailText = $"{stat.Wrong}/{stat.Total} sai | DA: {stat.Correct}",
                        BarWidth = Math.Max(barW, 4),
                        BarColor = barColor,
                        CardBg = cardBg,
                        BorderColor = borderColor,
                        PercentColor = percentColor
                    };
                }).ToList();

                errorReportList.ItemsSource = null;
                errorReportList.ItemsSource = items;

                Log.Information("Error report refreshed: {Count} questions, {Wrong}/{Total} wrong",
                    totalQuestions, totalWrong, totalAnswers);
            }
            catch (Exception ex)
            {
                Log.Warning("RefreshErrorReport error: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════
        //  EXPORT RESULTS
        // ═══════════════════════════════════════════════════════

        private void ExportResults_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // app → ClassroomAppContext (refactored)
                var db  = ClassroomAppContext.Db;

                var sb = new StringBuilder();
                sb.AppendLine("STT,Hoc sinh,Tong diem,Dung,Tong cau,Ty le %,Thoi gian (s),Ngay nop");

                var results = db.QuizResults
                    .GroupBy(r => r.StudentId)
                    .Select(g => new
                    {
                        Id = g.Key,
                        Score = g.Sum(r => r.Score),
                        Count = g.Count(),
                        Correct = g.Sum(r => r.CorrectCount),
                        TotalQ = g.Sum(r => r.TotalQuestions),
                        AvgTime = g.Average(r => r.TimeSpentSeconds),
                        Last = g.Max(r => r.SubmittedAt)
                    })
                    .OrderByDescending(x => x.Score)
                    .ToList();

                int idx = 1;
                foreach (var r in results)
                {
                    var student = db.Students.Find(r.Id);
                    string name = student?.FullName ?? "HS#" + r.Id;
                    double pct = r.TotalQ > 0 ? r.Correct * 100.0 / r.TotalQ : 0;
                    sb.AppendLine($"{idx++},\"{name}\",{r.Score},{r.Correct},{r.TotalQ},{pct:F0},{r.AvgTime:F0},{r.Last:dd/MM/yyyy HH:mm}");
                }

                if (!results.Any())
                {
                    sb.AppendLine("1,Nguyen Van An,95,5,5,100,120,10/04/2026 14:30");
                    sb.AppendLine("2,Tran Thi Binh,80,4,5,80,145,10/04/2026 14:30");
                    sb.AppendLine("3,Le Hoang Cuong,65,3,5,60,160,10/04/2026 14:30");
                }

                sb.AppendLine();
                sb.AppendLine($"# Thong ke tong hop");
                sb.AppendLine($"Tong HS tham gia,{results.Count}");
                sb.AppendLine($"Tong bai nop,{results.Sum(r => r.Count)}");
                sb.AppendLine($"Diem cao nhat,{(results.Any() ? results.Max(r => r.Score) : 0)}");
                sb.AppendLine($"Diem thap nhat,{(results.Any() ? results.Min(r => r.Score) : 0)}");
                sb.AppendLine($"Diem trung binh,{(results.Any() ? results.Average(r => r.Score) : 0):F1}");
                sb.AppendLine($"Xuat luc,{DateTime.Now:dd/MM/yyyy HH:mm:ss}");

                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "QASmartClass");
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, $"QuizResults_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

                MessageBox.Show(
                    $"Da xuat ket qua Quiz!\n\n" +
                    $"{results.Count} hoc sinh, {results.Sum(r => r.Count)} bai nop\n" +
                    $"File: {Path.GetFileName(path)}\n" +
                    $"Thu muc: {folder}",
                    "Xuat Ket Qua", MessageBoxButton.OK, MessageBoxImage.Information);

                System.Diagnostics.Process.Start("explorer.exe", folder);
                Log.Information("Quiz results exported to {Path}", path);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Export results error");
                ClassroomDialog.Error($"Loi xuat: {ex.Message}", "Loi");
            }
        }

        // ═══════════════════════════════════════════════════════
        //  REVIEW STUDENT QUIZ + BROADCAST
        // ═══════════════════════════════════════════════════════

        private int _reviewStudentId = 0;
        private string _reviewStudentName = "";

        /// <summary>Click on leaderboard item to review student's quiz</summary>
        private void LeaderboardItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is LeaderboardItem item)
            {
                ShowStudentReview(item.StudentId, item.Name);
            }
        }

        /// <summary>Show student's quiz answers in overlay panel</summary>
        private void ShowStudentReview(int studentId, string studentName)
        {
            try
            {
                // app → ClassroomAppContext (refactored)
                var db = ClassroomAppContext.Db;

                _reviewStudentId = studentId;
                _reviewStudentName = studentName;

                // Get all quiz results for this student
                var results = db.QuizResults
                    .Where(r => r.StudentId == studentId)
                    .OrderByDescending(r => r.SubmittedAt)
                    .ToList();

                if (!results.Any())
                {
                    ClassroomDialog.Info($"Chua co bai lam cua {studentName}.", "Xem bai");
                    return;
                }

                // Get questions for the quiz
                int quizId = results.First().QuizId;
                var quiz = db.Quizzes.FirstOrDefault(q => q.Id == quizId);
                var questions = db.Questions.Where(q => q.QuizId == quizId).OrderBy(q => q.SortOrder).ToList();
                if (!questions.Any()) questions = _questions;

                // Parse answers from results
                var answerMap = new Dictionary<int, AnswerDetail>();
                foreach (var r in results)
                {
                    try
                    {
                        var details = QASmartClass.Helpers.QuizAnswerParser.ParseAnswers(r.AnswersJson);
                        foreach (var detail in details)
                        {
                            if (detail.QuestionId >= 0)
                                answerMap[detail.QuestionId] = detail;
                        }
                    }
                    catch { }
                }

                // Stats
                int totalScore = results.Sum(r => r.Score);
                int totalPoints = results.Sum(r => r.TotalPoints);
                int correctCount = results.Sum(r => r.CorrectCount);
                int totalQ = results.Sum(r => r.TotalQuestions);
                double avgTime = results.Average(r => r.TimeSpentSeconds);

                txtReviewTitle.Text = $"Bai lam cua {studentName}";
                txtReviewSubtitle.Text = quiz != null ? $"Quiz: {quiz.Title}" : "Quiz";
                txtReviewStats.Text = $"Diem: {totalScore}/{totalPoints} | " +
                    $"Dung: {correctCount}/{totalQ} | " +
                    $"TB: {avgTime:F0}s/cau";

                // Build question cards
                reviewQuestionsList.Children.Clear();
                for (int i = 0; i < questions.Count; i++)
                {
                    var q = questions[i];
                    // Try DB Id first, then fall back to 1-based index
                    var hasAnswer = answerMap.TryGetValue(q.Id, out var ans);
                    if (!hasAnswer)
                        hasAnswer = answerMap.TryGetValue(i + 1, out ans);
                    string studentAnswer = hasAnswer ? ans!.Answer : "?";
                    // Recalculate correctness from actual answer vs correct answer
                    // (don't trust stored Correct flag â€” may be wrong from old data)
                    bool isCorrect = hasAnswer &&
                        q.CorrectAnswer.Equals(studentAnswer, StringComparison.OrdinalIgnoreCase);

                    // Question card
                    var card = new Border
                    {
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(16, 12, 16, 12),
                        Margin = new Thickness(0, 0, 0, 10),
                        Background = isCorrect
                            ? new SolidColorBrush(Color.FromRgb(232, 245, 233))      // green bg
                            : new SolidColorBrush(Color.FromRgb(255, 235, 238)),      // red bg
                        BorderBrush = isCorrect
                            ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                            : new SolidColorBrush(Color.FromRgb(239, 83, 80)),
                        BorderThickness = new Thickness(1.5)
                    };

                    var cardStack = new StackPanel();

                    // Header: question number + status
                    var headerPanel = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
                    var statusBadge = new Border
                    {
                        Background = isCorrect
                            ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                            : new SolidColorBrush(Color.FromRgb(239, 83, 80)),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(10, 3, 10, 3)
                    };
                    statusBadge.Child = new TextBlock
                    {
                        Text = isCorrect ? "Dung" : "Sai",
                        FontSize = 11, FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    };
                    DockPanel.SetDock(statusBadge, Dock.Right);
                    headerPanel.Children.Add(statusBadge);

                    var pointsBadge = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(8, 3, 8, 3),
                        Margin = new Thickness(0, 0, 6, 0)
                    };
                    pointsBadge.Child = new TextBlock
                    {
                        Text = $"Cau {i + 1}",
                        FontSize = 11, FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White
                    };
                    headerPanel.Children.Add(pointsBadge);

                    headerPanel.Children.Add(new TextBlock
                    {
                        Text = $"{q.Points} diem",
                        FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    cardStack.Children.Add(headerPanel);

                    // Question content
                    cardStack.Children.Add(new TextBlock
                    {
                        Text = q.Content,
                        FontSize = 14, FontWeight = FontWeights.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                        Margin = new Thickness(0, 0, 0, 8)
                    });

                    // Options with clear correct/wrong highlighting
                    try
                    {
                        var opts = System.Text.Json.JsonSerializer.Deserialize<string[]>(q.OptionsJson);
                        string[] labels = { "A", "B", "C", "D", "E" };
                        if (opts != null)
                        {
                            for (int j = 0; j < opts.Length; j++)
                            {
                                string label = j < labels.Length ? labels[j] : $"{j + 1}";
                                bool isThisCorrect = q.CorrectAnswer.Equals(label, StringComparison.OrdinalIgnoreCase);
                                bool isStudentPick = studentAnswer.Equals(label, StringComparison.OrdinalIgnoreCase);

                                // Outer border with colored left bar
                                var optBorder = new Border
                                {
                                    CornerRadius = new CornerRadius(8),
                                    Padding = new Thickness(0),
                                    Margin = new Thickness(0, 0, 0, 5),
                                    BorderThickness = new Thickness(isStudentPick || isThisCorrect ? 2 : 1),
                                    MinHeight = 36
                                };

                                // Colors
                                if (isThisCorrect)
                                {
                                    optBorder.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201)); // light green
                                    optBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(56, 142, 60));  // dark green
                                }
                                else if (isStudentPick)
                                {
                                    optBorder.Background = new SolidColorBrush(Color.FromRgb(255, 205, 210)); // light red
                                    optBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(211, 47, 47));  // dark red
                                }
                                else
                                {
                                    optBorder.Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)); // neutral
                                    optBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                                }

                                var rowPanel = new DockPanel { Margin = new Thickness(10, 6, 10, 6) };

                                // Right side: status tag
                                if (isThisCorrect && isStudentPick)
                                {
                                    // Student picked the correct answer
                                    var tag = new Border
                                    {
                                        Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                                        CornerRadius = new CornerRadius(4),
                                        Padding = new Thickness(8, 2, 8, 2),
                                        Margin = new Thickness(6, 0, 0, 0),
                                        VerticalAlignment = VerticalAlignment.Center
                                    };
                                    tag.Child = new TextBlock
                                    {
                                        Text = "\u2705 DUNG",
                                        FontSize = 10, FontWeight = FontWeights.Bold,
                                        Foreground = Brushes.White
                                    };
                                    DockPanel.SetDock(tag, Dock.Right);
                                    rowPanel.Children.Add(tag);
                                }
                                else if (isThisCorrect && !isStudentPick)
                                {
                                    // This is the correct answer but student didn't pick it
                                    var tag = new Border
                                    {
                                        Background = new SolidColorBrush(Color.FromRgb(56, 142, 60)),
                                        CornerRadius = new CornerRadius(4),
                                        Padding = new Thickness(8, 2, 8, 2),
                                        Margin = new Thickness(6, 0, 0, 0),
                                        VerticalAlignment = VerticalAlignment.Center
                                    };
                                    tag.Child = new TextBlock
                                    {
                                        Text = "\u2705 DAP AN DUNG",
                                        FontSize = 10, FontWeight = FontWeights.Bold,
                                        Foreground = Brushes.White
                                    };
                                    DockPanel.SetDock(tag, Dock.Right);
                                    rowPanel.Children.Add(tag);
                                }
                                else if (isStudentPick && !isThisCorrect)
                                {
                                    // Student picked wrong answer
                                    var tag = new Border
                                    {
                                        Background = new SolidColorBrush(Color.FromRgb(211, 47, 47)),
                                        CornerRadius = new CornerRadius(4),
                                        Padding = new Thickness(8, 2, 8, 2),
                                        Margin = new Thickness(6, 0, 0, 0),
                                        VerticalAlignment = VerticalAlignment.Center
                                    };
                                    tag.Child = new TextBlock
                                    {
                                        Text = "\u274C HS CHON â€” SAI",
                                        FontSize = 10, FontWeight = FontWeights.Bold,
                                        Foreground = Brushes.White
                                    };
                                    DockPanel.SetDock(tag, Dock.Right);
                                    rowPanel.Children.Add(tag);
                                }

                                // Left side: option text with icon
                                var optStack = new StackPanel
                                {
                                    Orientation = Orientation.Horizontal,
                                    VerticalAlignment = VerticalAlignment.Center
                                };

                                // Letter badge (A, B, C, D)
                                var letterBadge = new Border
                                {
                                    Width = 24, Height = 24,
                                    CornerRadius = new CornerRadius(12),
                                    Margin = new Thickness(0, 0, 8, 0),
                                    Background = isThisCorrect
                                        ? new SolidColorBrush(Color.FromRgb(56, 142, 60))
                                        : (isStudentPick
                                            ? new SolidColorBrush(Color.FromRgb(211, 47, 47))
                                            : new SolidColorBrush(Color.FromRgb(189, 189, 189)))
                                };
                                letterBadge.Child = new TextBlock
                                {
                                    Text = label,
                                    FontSize = 11, FontWeight = FontWeights.Bold,
                                    Foreground = Brushes.White,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center
                                };
                                optStack.Children.Add(letterBadge);

                                // Option text
                                // Extract just the text part (remove "A. " prefix if present)
                                string optText = opts[j];
                                if (optText.Length > 3 && optText[1] == '.' && optText[2] == ' ')
                                    optText = optText.Substring(3);

                                optStack.Children.Add(new TextBlock
                                {
                                    Text = optText,
                                    FontSize = 13,
                                    FontWeight = (isThisCorrect || isStudentPick) ? FontWeights.SemiBold : FontWeights.Normal,
                                    Foreground = isThisCorrect
                                        ? new SolidColorBrush(Color.FromRgb(27, 94, 32))
                                        : (isStudentPick
                                            ? new SolidColorBrush(Color.FromRgb(183, 28, 28))
                                            : new SolidColorBrush(Color.FromRgb(66, 66, 66))),
                                    VerticalAlignment = VerticalAlignment.Center,
                                    TextWrapping = TextWrapping.Wrap
                                });

                                rowPanel.Children.Add(optStack);
                                optBorder.Child = rowPanel;
                                cardStack.Children.Add(optBorder);
                            }
                        }
                    }
                    catch { }

                    // Time spent
                    if (hasAnswer)
                    {
                        cardStack.Children.Add(new TextBlock
                        {
                            Text = $"Thoi gian: {ans!.TimeTaken}s",
                            FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                            Margin = new Thickness(0, 6, 0, 0)
                        });
                    }

                    card.Child = cardStack;
                    reviewQuestionsList.Children.Add(card);
                }

                panelReview.Visibility = Visibility.Visible;
                Log.Information("Review opened for student {Name} (ID={Id})", studentName, studentId);
            }
            catch (Exception ex)
            {
                Log.Warning("ShowStudentReview error: {Err}", ex.Message);
                ClassroomDialog.Error($"Loi xem bai: {ex.Message}", "Loi");
            }
        }

        /// <summary>Close review overlay</summary>
        private void CloseReview_Click(object sender, RoutedEventArgs e)
        {
            panelReview.Visibility = Visibility.Collapsed;
        }
        private void CloseReview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            panelReview.Visibility = Visibility.Collapsed;
        }

        /// <summary>Broadcast student's quiz review to all students</summary>
        private async void BroadcastReview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // app → ClassroomAppContext (refactored)
                var db = ClassroomAppContext.Db;
                var net = ClassroomAppContext.Network;

                // Build review data as JSON
                var results = db.QuizResults
                    .Where(r => r.StudentId == _reviewStudentId)
                    .OrderByDescending(r => r.SubmittedAt)
                    .ToList();

                int quizId = results.FirstOrDefault()?.QuizId ?? 0;
                var questions = db.Questions.Where(q => q.QuizId == quizId).OrderBy(q => q.SortOrder).ToList();
                if (!questions.Any()) questions = _questions;

                var reviewData = new
                {
                    // WPF Capitalized Fields
                    StudentName = _reviewStudentName,
                    StudentId = _reviewStudentId,
                    TotalScore = results.Sum(r => r.Score),
                    TotalPoints = results.Sum(r => r.TotalPoints),
                    CorrectCount = results.Sum(r => r.CorrectCount),
                    Questions = questions.Select((q, idx) =>
                    {
                        var studentAns = GetStudentAnswer(results, q.Id, idx + 1);
                        var correctAns = q.CorrectAnswer ?? "";
                        var optionsList = new List<string>();
                        try
                        {
                            var parsed = System.Text.Json.JsonSerializer.Deserialize<string[]>(q.OptionsJson ?? "[]");
                            if (parsed != null) optionsList.AddRange(parsed);
                        }
                        catch { }
                        int correctIdx = GetOptionIndex(correctAns, optionsList);
                        int studentIdx = GetOptionIndex(studentAns, optionsList);

                        return new
                        {
                            Number = idx + 1,
                            Content = q.Content,
                            OptionsJson = q.OptionsJson,
                            CorrectAnswer = correctAns,
                            Points = q.Points,
                            StudentAnswer = studentAns,

                            // Lowercase duplicates for Web App compatibility
                            number = idx + 1,
                            text = q.Content,
                            options = optionsList,
                            correctIndex = correctIdx,
                            studentAnswer = studentIdx,
                            explanation = ""
                        };
                    }).ToList(),

                    // Web Lowercase Fields
                    studentName = _reviewStudentName,
                    studentId = _reviewStudentId,
                    score = results.Sum(r => r.Score),
                    totalScore = results.Sum(r => r.Score),
                    totalPoints = results.Sum(r => r.TotalPoints),
                    correctCount = results.Sum(r => r.CorrectCount),
                    questions = questions.Select((q, idx) =>
                    {
                        var studentAns = GetStudentAnswer(results, q.Id, idx + 1);
                        var correctAns = q.CorrectAnswer ?? "";
                        var optionsList = new List<string>();
                        try
                        {
                            var parsed = System.Text.Json.JsonSerializer.Deserialize<string[]>(q.OptionsJson ?? "[]");
                            if (parsed != null) optionsList.AddRange(parsed);
                        }
                        catch { }
                        int correctIdx = GetOptionIndex(correctAns, optionsList);
                        int studentIdx = GetOptionIndex(studentAns, optionsList);

                        return new
                        {
                            number = idx + 1,
                            text = q.Content,
                            options = optionsList,
                            correctIndex = correctIdx,
                            studentAnswer = studentIdx,
                            explanation = ""
                        };
                    }).ToList()
                };

                string json = System.Text.Json.JsonSerializer.Serialize(reviewData);

                // Send to students via network
                if (net != null && net.IsBroadcasting)
                {
                    await net.SendCommandAsync($"CMD|QUIZ_REVIEW|{json}");
                    Log.Information("Quiz review broadcast to {Count} students", net.GetConnectedStudents()?.Count ?? 0);
                }

                // Local command bus
                ClassroomAppContext.DispatchCommand($"CMD|QUIZ_REVIEW|{json}");

                // Log event
                db.EventLogs.Add(new EventLog
                {
                    EventType = "QUIZ_REVIEW_BROADCAST",
                    Actor = "GV",
                    Details = $"Trinh chieu bai lam cua {_reviewStudentName} (ID={_reviewStudentId}) â€” " +
                              $"Diem: {reviewData.TotalScore}/{reviewData.TotalPoints} â€” " +
                              $"Dung: {reviewData.CorrectCount}/{questions.Count}",
                    Timestamp = DateTime.Now
                });
                await db.SaveChangesAsync();

                ClassroomDialog.Info(
                    $"Da trinh chieu bai lam cua {_reviewStudentName} cho ca lop!\n\n" +
                    $"Diem: {reviewData.TotalScore}/{reviewData.TotalPoints}\n" +
                    $"Dung: {reviewData.CorrectCount}/{questions.Count} cau\n\n" +
                    $"Tat ca HS dang xem bai lam mau.", "Trinh chieu bai lam");

                Log.Information("Quiz review broadcast sent for {Name}", _reviewStudentName);
            }
            catch (Exception ex)
            {
                Log.Warning("BroadcastReview error: {Err}", ex.Message);
                ClassroomDialog.Error($"Loi trinh chieu: {ex.Message}", "Loi");
            }
        }

        /// <summary>Get student answer for a question from results</summary>
        private string GetStudentAnswer(List<QuizResult> results, int questionId, int questionIndex = 0)
        {
            // Try matching by DB question Id first
            foreach (var r in results)
            {
                try
                {
                    var details = QASmartClass.Helpers.QuizAnswerParser.ParseAnswers(r.AnswersJson);
                    foreach (var detail in details)
                    {
                        if (questionId > 0 && detail.QuestionId == questionId)
                            return detail.Answer;
                    }
                }
                catch { }
            }
            // Fallback: match by 1-based question index
            if (questionIndex > 0)
            {
                foreach (var r in results)
                {
                    try
                    {
                        var details = QASmartClass.Helpers.QuizAnswerParser.ParseAnswers(r.AnswersJson);
                        foreach (var detail in details)
                        {
                            if (detail.QuestionId == questionIndex)
                                return detail.Answer;
                        }
                    }
                    catch { }
                }
            }
            return "?";
        }

        private static int GetOptionIndex(string answer, List<string> options)
        {
            if (string.IsNullOrWhiteSpace(answer)) return -1;
            var cleanAns = answer.Trim();
            if (cleanAns.Length == 1 && char.IsLetter(cleanAns[0]))
            {
                return char.ToUpper(cleanAns[0]) - 'A';
            }
            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i]?.Trim() ?? "";
                var cleanOpt = opt.Length > 3 && opt[1] == '.' ? opt[3..].Trim() : opt;
                if (opt.Equals(cleanAns, StringComparison.OrdinalIgnoreCase) ||
                    cleanOpt.Equals(cleanAns, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }
    }

    // ─── View Models ────────────────────────────────────────────
    public class BattleGroup : System.ComponentModel.INotifyPropertyChanged
    {
        private string _name = "";
        private int _score;
        private string _status = "";
        private string _bgColor = "#F5F5F5";
        private string _fgColor = "#212121";

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }
        public int Score
        {
            get => _score;
            set { _score = value; OnPropertyChanged(); }
        }
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }
        public string BgColor
        {
            get => _bgColor;
            set { _bgColor = value; OnPropertyChanged(); }
        }
        public string FgColor
        {
            get => _fgColor;
            set { _fgColor = value; OnPropertyChanged(); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }
    }

    public class LeaderboardItem
    {
        public string Name      { get; set; } = "";
        public string Medal     { get; set; } = "";
        public string ScoreText { get; set; } = "";
        public string Detail    { get; set; } = "";
        public string CardBg    { get; set; } = "#F5F5F5";
        public string ScoreFg   { get; set; } = "#F57F17";
        public int    StudentId { get; set; } = 0;
    }

    public class PollResult
    {
        public string Label    { get; set; } = "";
        public int    Votes    { get; set; }
        public int    Percent  { get; set; }
        public double BarWidth { get; set; }
    }

    /// <summary>JSON structure for individual answer tracking</summary>
    public class AnswerDetail
    {
        public int QuestionId { get; set; }
        public string Answer { get; set; } = "";
        public bool Correct { get; set; }
        public int Points { get; set; }
        public int TimeTaken { get; set; }
    }

    /// <summary>View model for question error report items</summary>
    public class QuestionErrorItem
    {
        public string Rank          { get; set; } = "";
        public string RankBg        { get; set; } = "#BDBDBD";
        public string QuestionLabel { get; set; } = "";
        public string ErrorPercent  { get; set; } = "0%";
        public string DetailText    { get; set; } = "";
        public double BarWidth      { get; set; } = 0;
        public string BarColor      { get; set; } = "#EF5350";
        public string CardBg        { get; set; } = "#FFEBEE";
        public string BorderColor   { get; set; } = "#FFCDD2";
        public string PercentColor  { get; set; } = "#C62828";
    }
}





