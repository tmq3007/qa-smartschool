using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    /// <summary>
    /// Engine điều khiển quiz/thi đua/khảo sát trong lớp học
    /// </summary>
    public partial class QuizEngineService : ObservableObject
    {
        private readonly AppDbContext _db;
        private CancellationTokenSource? _timerCts;

        [ObservableProperty] private bool _isQuizRunning = false;
        [ObservableProperty] private int _currentQuestionIndex = 0;
        [ObservableProperty] private int _remainingSeconds = 0;
        [ObservableProperty] private int _submittedCount = 0;
        [ObservableProperty] private int _totalParticipants = 0;

        public Quiz? CurrentQuiz { get; private set; }
        public Question? CurrentQuestion => CurrentQuiz?.Questions.ElementAtOrDefault(CurrentQuestionIndex);

        public ObservableCollection<QuizLeaderboardEntry> Leaderboard { get; } = new();

        public event EventHandler<Question>? QuestionChanged;
        public event EventHandler? QuizEnded;

        public QuizEngineService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Bắt đầu 1 quiz
        /// </summary>
        public async Task StartQuizAsync(int quizId, int participantCount)
        {
            CurrentQuiz = _db.Quizzes.FirstOrDefault(q => q.Id == quizId);
            if (CurrentQuiz == null) return;

            // Load questions
            CurrentQuiz.Questions = _db.Questions
                .Where(q => q.QuizId == quizId)
                .OrderBy(q => q.SortOrder)
                .ToList();

            TotalParticipants = participantCount;
            CurrentQuestionIndex = 0;
            SubmittedCount = 0;
            IsQuizRunning = true;
            Leaderboard.Clear();

            Log.Information("Quiz started: {Title}, {Count} questions, {Participants} participants",
                CurrentQuiz.Title, CurrentQuiz.Questions.Count, participantCount);

            await ShowCurrentQuestionAsync();
        }

        /// <summary>
        /// Hiển thị câu hỏi hiện tại và bắt đầu đếm ngược
        /// </summary>
        public async Task ShowCurrentQuestionAsync()
        {
            if (CurrentQuestion == null) return;

            SubmittedCount = 0;
            RemainingSeconds = CurrentQuiz?.TimeLimitSeconds ?? 30;
            QuestionChanged?.Invoke(this, CurrentQuestion);

            // Timer đếm ngược
            _timerCts?.Cancel();
            _timerCts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                while (RemainingSeconds > 0 && !_timerCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000, _timerCts.Token);
                    RemainingSeconds--;
                }

                if (RemainingSeconds <= 0)
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(NextQuestion);
            }, _timerCts.Token);

            await Task.CompletedTask;
        }

        /// <summary>
        /// HS nộp câu trả lời
        /// </summary>
        public void SubmitAnswer(int studentId, string answer, double timeSpent)
        {
            if (CurrentQuestion == null || CurrentQuiz == null) return;

            var isCorrect = answer.Equals(CurrentQuestion.CorrectAnswer, StringComparison.OrdinalIgnoreCase);
            var points = isCorrect ? CurrentQuestion.Points : 0;

            // Cập nhật leaderboard
            var entry = Leaderboard.FirstOrDefault(e => e.StudentId == studentId);
            if (entry == null)
            {
                entry = new QuizLeaderboardEntry { StudentId = studentId };
                System.Windows.Application.Current?.Dispatcher.Invoke(() => Leaderboard.Add(entry));
            }

            entry.TotalScore += points;
            entry.CorrectCount += isCorrect ? 1 : 0;
            entry.TotalAnswered++;

            SubmittedCount++;

            Log.Debug("Answer submitted: Student {Id}, Correct: {Correct}, Score: {Score}", studentId, isCorrect, points);
        }

        /// <summary>
        /// Chuyển sang câu tiếp theo
        /// </summary>
        public void NextQuestion()
        {
            _timerCts?.Cancel();

            if (CurrentQuiz == null) return;

            CurrentQuestionIndex++;
            if (CurrentQuestionIndex >= CurrentQuiz.Questions.Count)
            {
                EndQuiz();
                return;
            }

            _ = ShowCurrentQuestionAsync();
        }

        /// <summary>
        /// Kết thúc quiz, tính điểm
        /// </summary>
        public void EndQuiz()
        {
            _timerCts?.Cancel();
            IsQuizRunning = false;

            // Sắp xếp leaderboard theo điểm giảm dần
            var sorted = Leaderboard.OrderByDescending(e => e.TotalScore).ToList();
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                Leaderboard.Clear();
                for (int i = 0; i < sorted.Count; i++)
                {
                    sorted[i].Rank = i + 1;
                    Leaderboard.Add(sorted[i]);
                }
            });

            QuizEnded?.Invoke(this, EventArgs.Empty);
            Log.Information("Quiz ended: {Title}, Top score: {Score}",
                CurrentQuiz?.Title, sorted.FirstOrDefault()?.TotalScore ?? 0);
        }
    }

    public partial class QuizLeaderboardEntry : ObservableObject
    {
        [ObservableProperty] private int _studentId;
        [ObservableProperty] private string _studentName = string.Empty;
        [ObservableProperty] private int _rank;
        [ObservableProperty] private int _totalScore;
        [ObservableProperty] private int _correctCount;
        [ObservableProperty] private int _totalAnswered;
    }
}
