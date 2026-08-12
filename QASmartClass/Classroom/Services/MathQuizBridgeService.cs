using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using QASmartClass.LearningTools.Models;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    /// <summary>
    /// Math Quiz Bridge — Kết nối Phase4_Data JSON → WebSocket quiz broadcast.
    ///
    /// Workflow:
    ///   1. GV chọn chapter/exam → GenerateQuiz()
    ///   2. GV nhấn "Bắt đầu" → StartQuiz() → broadcast quiz_start cho HS
    ///   3. HS trả lời → WebSocket nhận quiz_answer → ProcessStudentAnswer()
    ///   4. GV nhấn "Kết thúc" → EndQuiz() → broadcast quiz_end + results
    ///   5. Auto-group A/B/C dựa trên điểm
    ///
    /// Tích hợp:
    ///   - WebSocketBridgeService: broadcast/receive messages
    ///   - MathChapterData: Phase4 JSON models
    ///   - QuizEngineService: leaderboard data sync
    /// </summary>
    public class MathQuizBridgeService
    {
        // ─── State ───
        private readonly WebSocketBridgeService _wsBridge;
        private readonly GamificationService? _gamification;
        private readonly string _dataPath;
        private readonly List<MathChapterData> _chapters = new();
        private readonly List<MockExamData> _mockExams = new();

        // ─── Active Quiz State ───
        private MathQuizSession? _activeSession;
        public MathQuizSession? ActiveSession => _activeSession;
        public bool IsQuizActive => _activeSession?.Status == QuizSessionStatus.Active;

        // ─── Events ───
        public event EventHandler<MathQuizSession>? QuizStarted;
        public event EventHandler<StudentAnswerResult>? AnswerReceived;
        public event EventHandler<MathQuizSummary>? QuizEnded;

        // ═══════════════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════

        public MathQuizBridgeService(WebSocketBridgeService wsBridge, GamificationService? gamification = null)
        {
            _wsBridge = wsBridge;
            _gamification = gamification;
            _dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Resources", "MathData", "Phase4_Data");

            // Listen for quiz answers from students
            wsBridge.WebMessageReceived += OnWebMessage;
        }

        // ═══════════════════════════════════════════════════════════
        //  DATA LOADING
        // ═══════════════════════════════════════════════════════════

        /// <summary>Load all chapter and exam data from Phase4_Data</summary>
        public void LoadMathData()
        {
            _chapters.Clear();
            _mockExams.Clear();

            if (!Directory.Exists(_dataPath))
            {
                Log.Warning("MathQuizBridge: Data path not found: {Path}", _dataPath);
                return;
            }

            // Load chapters
            foreach (var file in Directory.GetFiles(_dataPath, "G??_CH*.json").OrderBy(f => f))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var ch = JsonSerializer.Deserialize<MathChapterData>(json);
                    if (ch?.Metadata != null) _chapters.Add(ch);
                }
                catch (Exception ex) { Log.Warning(ex, "MathQuizBridge: Skip {File}", Path.GetFileName(file)); }
            }

            // Load mock exams
            foreach (var file in Directory.GetFiles(_dataPath, "THPT_Mock*.json").OrderBy(f => f))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var exam = JsonSerializer.Deserialize<MockExamData>(json);
                    if (exam?.Metadata != null) _mockExams.Add(exam);
                }
                catch (Exception ex) { Log.Warning(ex, "MathQuizBridge: Skip exam {File}", Path.GetFileName(file)); }
            }

            Log.Information("MathQuizBridge: Loaded {Ch} chapters, {Ex} exams", _chapters.Count, _mockExams.Count);
        }

        /// <summary>Get chapter list for GV to pick from</summary>
        public List<(string Id, string Name, int Grade, int Problems)> GetChapterList()
        {
            return _chapters.Select(c => (
                c.Metadata.ChapterId,
                c.Metadata.ChapterName,
                c.Metadata.Grade,
                c.Metadata.TotalProblems
            )).ToList();
        }

        /// <summary>Get mock exam list</summary>
        public List<(string Id, string Title, int Questions, int TimeMinutes)> GetExamList()
        {
            return _mockExams.Select(e => (
                e.Metadata.ExamId,
                e.Metadata.Title,
                e.Metadata.TotalQuestions,
                e.Metadata.TimeLimit
            )).ToList();
        }

        // ═══════════════════════════════════════════════════════════
        //  QUIZ GENERATION
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Generate quiz from chapter(s) problems.
        /// GV can filter by chapterId, difficulty, and count.
        /// </summary>
        public MathQuizSession GenerateChapterQuiz(
            string? chapterId = null,
            string? difficulty = null,
            int count = 10,
            int timeLimitSeconds = 600,
            bool shuffle = true)
        {
            // Build problem pool
            var pool = new List<(Problem p, string chName, int grade)>();

            var source = chapterId != null
                ? _chapters.Where(c => c.Metadata.ChapterId == chapterId)
                : _chapters;

            foreach (var ch in source)
            {
                foreach (var sec in ch.Sections)
                {
                    foreach (var p in sec.Problems)
                    {
                        if (p.Options?.Count >= 2 && !string.IsNullOrEmpty(p.GetAnswer()))
                            pool.Add((p, ch.Metadata.ChapterName, ch.Metadata.Grade));
                    }
                }
            }

            // Filter by difficulty
            if (!string.IsNullOrEmpty(difficulty))
                pool = pool.Where(x => x.p.Difficulty?.Equals(difficulty, StringComparison.OrdinalIgnoreCase) == true).ToList();

            // Shuffle
            if (shuffle)
            {
                var rng = new Random();
                pool = pool.OrderBy(_ => rng.Next()).ToList();
            }

            // Take count
            var selected = pool.Take(count).ToList();

            // Build quiz session
            var questions = selected.Select((item, idx) => new MathQuizQuestion
            {
                Index = idx,
                Text = item.p.Question,
                Options = item.p.Options,
                CorrectAnswer = item.p.GetAnswer(),
                Difficulty = item.p.Difficulty ?? "Medium",
                Solution = item.p.Solution,
                ChapterName = item.chName,
                Grade = item.grade,
                Points = item.p.Points > 0 ? item.p.Points : 10,
                QuestionType = item.p.Type ?? "MCQ"
            }).ToList();

            _activeSession = new MathQuizSession
            {
                Id = $"math_quiz_{DateTime.Now:yyyyMMdd_HHmmss}",
                Title = chapterId != null
                    ? $"Quiz — {_chapters.FirstOrDefault(c => c.Metadata.ChapterId == chapterId)?.Metadata.ChapterName ?? chapterId}"
                    : $"Quiz Tổng Hợp ({selected.Count} câu)",
                Questions = questions,
                TimeLimitSeconds = timeLimitSeconds,
                Status = QuizSessionStatus.Ready
            };

            Log.Information("MathQuizBridge: Generated quiz '{Title}' with {Count} questions",
                _activeSession.Title, questions.Count);

            return _activeSession;
        }

        /// <summary>Generate quiz from mock exam</summary>
        public MathQuizSession? GenerateExamQuiz(string examId)
        {
            var exam = _mockExams.FirstOrDefault(e => e.Metadata.ExamId == examId);
            if (exam == null) return null;

            var questions = exam.Questions.Select((q, idx) => new MathQuizQuestion
            {
                Index = idx,
                Text = q.GetQuestion(),
                Options = q.GetOptions(),
                CorrectAnswer = q.GetAnswer(),
                Difficulty = q.GetDifficulty(),
                Solution = q.Solution ?? "",
                ChapterName = q.Topic,
                Grade = 12,
                Points = (int)(exam.Metadata.PointsPerQuestion * 10),
                QuestionType = "MCQ"
            }).ToList();

            _activeSession = new MathQuizSession
            {
                Id = examId,
                Title = exam.Metadata.Title,
                Questions = questions,
                TimeLimitSeconds = exam.Metadata.TimeLimit * 60,
                Status = QuizSessionStatus.Ready
            };

            return _activeSession;
        }

        // ═══════════════════════════════════════════════════════════
        //  QUIZ LIFECYCLE
        // ═══════════════════════════════════════════════════════════

        /// <summary>Broadcast quiz to all connected students</summary>
        public async Task StartQuizAsync()
        {
            if (_activeSession == null || _activeSession.Status != QuizSessionStatus.Ready) return;

            _activeSession.Status = QuizSessionStatus.Active;
            _activeSession.StartTime = DateTime.Now;
            _activeSession.StudentAnswers.Clear();

            // Build quiz_start payload for students
            var payload = JsonSerializer.Serialize(new
            {
                type = "cmd",
                action = "QUIZ_START",
                quizId = _activeSession.Id,
                title = _activeSession.Title,
                timeLimit = _activeSession.TimeLimitSeconds,
                quizType = _activeSession.Questions.Any(q => q.QuestionType != "MCQ" && q.QuestionType != "MultipleChoice") ? "mixed" : "multiple_choice",
                questions = _activeSession.Questions.Select(q => new
                {
                    text = q.Text,
                    options = q.Options,
                    difficulty = q.Difficulty,
                    type = q.QuestionType
                }).ToArray()
            });

            await _wsBridge.BroadcastJsonToWebClients(payload);

            QuizStarted?.Invoke(this, _activeSession);

            Log.Information("MathQuizBridge: Quiz started — '{Title}', {Count} questions, broadcast to {Clients} clients",
                _activeSession.Title, _activeSession.Questions.Count, _wsBridge.ConnectedWebClients);
        }

        /// <summary>End quiz, calculate results, broadcast to students</summary>
        public async Task<MathQuizSummary> EndQuizAsync()
        {
            if (_activeSession == null) return new MathQuizSummary();

            _activeSession.Status = QuizSessionStatus.Finished;
            _activeSession.EndTime = DateTime.Now;

            // Calculate per-student results
            var summary = CalculateSummary();

            // Broadcast quiz_end with results
            var payload = JsonSerializer.Serialize(new
            {
                type = "cmd",
                action = "QUIZ_END",
                results = new
                {
                    score = summary.ClassAvgCorrect,
                    total = _activeSession.Questions.Count,
                    message = $"Điểm trung bình lớp: {summary.ClassAvgScore:F1}/10"
                }
            });

            await _wsBridge.BroadcastJsonToWebClients(payload);

            QuizEnded?.Invoke(this, summary);

            Log.Information("MathQuizBridge: Quiz ended — Avg: {Avg:F1}, A={A} B={B} C={C}",
                summary.ClassAvgScore, summary.GroupA.Count, summary.GroupB.Count, summary.GroupC.Count);

            return summary;
        }

        /// <summary>Broadcast quiz review with correct answers</summary>
        public async Task BroadcastReviewAsync()
        {
            if (_activeSession == null) return;

            var reviewData = JsonSerializer.Serialize(new
            {
                type = "cmd",
                action = "QUIZ_REVIEW",
                data = JsonSerializer.Serialize(new
                {
                    title = _activeSession.Title,
                    questions = _activeSession.Questions.Select((q, idx) => new
                    {
                        text = q.Text,
                        options = q.Options,
                        correctIndex = GetCorrectIndex(q),
                        explanation = q.Solution
                    }).ToArray()
                })
            });

            await _wsBridge.BroadcastJsonToWebClients(reviewData);
            Log.Information("MathQuizBridge: Review broadcast sent");
        }

        // ═══════════════════════════════════════════════════════════
        //  ANSWER PROCESSING
        // ═══════════════════════════════════════════════════════════

        private void OnWebMessage(object? sender, StudentMessageEventArgs e)
        {
            if (_activeSession?.Status != QuizSessionStatus.Active) return;
            if (!e.Message.StartsWith("QUIZ_ANSWER|")) return;

            try
            {
                // Format: QUIZ_ANSWER|quizId|{answers_json}
                var parts = e.Message.Split('|', 3);
                if (parts.Length < 3) return;

                var answersJson = parts[2];
                var answerData = JsonSerializer.Deserialize<JsonElement>(answersJson);

                var answers = answerData.TryGetProperty("answers", out var ans) ? ans : default;
                var duration = answerData.TryGetProperty("duration", out var dur) ? dur.GetInt32() : 0;

                // Score each answer
                int correct = 0, total = _activeSession.Questions.Count;
                var answerMap = new Dictionary<int, int>();

                if (answers.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in answers.EnumerateObject())
                    {
                        if (int.TryParse(prop.Name, out int qIdx) && qIdx >= 0 && qIdx < total)
                        {
                            int selectedOption = prop.Value.GetInt32();
                            answerMap[qIdx] = selectedOption;

                            var q = _activeSession.Questions[qIdx];
                            int correctIdx = GetCorrectIndex(q);
                            if (selectedOption == correctIdx) correct++;
                        }
                    }
                }

                double score = total > 0 ? (double)correct / total * 10.0 : 0;

                var result = new StudentAnswerResult
                {
                    StudentCode = e.StudentCode,
                    Correct = correct,
                    Total = total,
                    Score = Math.Round(score, 1),
                    Duration = duration,
                    Answers = answerMap,
                    Grade = CalculateGrade(score),
                    Group = CalculateGroup(score),
                    SubmittedAt = DateTime.Now
                };

                _activeSession.StudentAnswers[e.StudentCode] = result;

                // Award XP via Gamification engine
                _gamification?.AwardQuizXp(e.StudentCode, correct, total, duration);

                AnswerReceived?.Invoke(this, result);

                Log.Information("MathQuizBridge: Answer from {Code}: {Correct}/{Total} = {Score:F1}",
                    e.StudentCode, correct, total, score);
            }
            catch (Exception ex)
            {
                Log.Warning("MathQuizBridge: Answer parse error: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  ANALYTICS & GROUPING
        // ═══════════════════════════════════════════════════════════

        /// <summary>Calculate quiz summary with A/B/C grouping</summary>
        public MathQuizSummary CalculateSummary()
        {
            if (_activeSession == null) return new MathQuizSummary();

            var answers = _activeSession.StudentAnswers.Values.ToList();
            var summary = new MathQuizSummary
            {
                QuizId = _activeSession.Id,
                Title = _activeSession.Title,
                TotalQuestions = _activeSession.Questions.Count,
                TotalStudents = answers.Count,
                ClassAvgScore = answers.Count > 0 ? answers.Average(a => a.Score) : 0,
                ClassAvgCorrect = answers.Count > 0 ? (int)answers.Average(a => a.Correct) : 0,
                HighestScore = answers.Count > 0 ? answers.Max(a => a.Score) : 0,
                LowestScore = answers.Count > 0 ? answers.Min(a => a.Score) : 0,
                AvgDuration = answers.Count > 0 ? (int)answers.Average(a => a.Duration) : 0,

                // A/B/C Grouping
                GroupA = answers.Where(a => a.Group == "A").Select(a => a.StudentCode).ToList(),
                GroupB = answers.Where(a => a.Group == "B").Select(a => a.StudentCode).ToList(),
                GroupC = answers.Where(a => a.Group == "C").Select(a => a.StudentCode).ToList(),

                // Per-question analysis
                QuestionStats = _activeSession.Questions.Select((q, idx) =>
                {
                    int correctCount = answers.Count(a => a.Answers.TryGetValue(idx, out int sel) && sel == GetCorrectIndex(q));
                    int attemptCount = answers.Count(a => a.Answers.ContainsKey(idx));
                    return new QuestionStat
                    {
                        Index = idx,
                        Text = q.Text.Length > 60 ? q.Text[..60] + "..." : q.Text,
                        CorrectCount = correctCount,
                        AttemptCount = attemptCount,
                        CorrectRate = attemptCount > 0 ? (double)correctCount / attemptCount * 100 : 0,
                        Difficulty = q.Difficulty
                    };
                }).ToList(),

                AllResults = answers.OrderByDescending(a => a.Score).ToList()
            };

            return summary;
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════

        /// <summary>Get 0-based index of correct answer from options</summary>
        private static int GetCorrectIndex(MathQuizQuestion q)
        {
            var answer = q.CorrectAnswer?.Trim() ?? "";

            // True/False special case
            string qType = (q.QuestionType ?? "").ToLower();
            if (qType == "truefalse" || qType == "tf" || answer.Equals("True", StringComparison.OrdinalIgnoreCase) || answer.Equals("False", StringComparison.OrdinalIgnoreCase))
            {
                if (answer.Equals("True", StringComparison.OrdinalIgnoreCase) || answer.Equals("Đúng", StringComparison.OrdinalIgnoreCase))
                    return 0;
                if (answer.Equals("False", StringComparison.OrdinalIgnoreCase) || answer.Equals("Sai", StringComparison.OrdinalIgnoreCase))
                    return 1;
            }

            // Letter-based answer: "A", "B", "C", "D"
            if (answer.Length == 1 && char.IsLetter(answer[0]))
                return char.ToUpper(answer[0]) - 'A';

            // Full text match
            for (int i = 0; i < q.Options.Count; i++)
            {
                var opt = q.Options[i]?.Trim() ?? "";
                // Remove "A. ", "B. " prefix for comparison
                var cleanOpt = opt.Length > 3 && opt[1] == '.' ? opt[3..].Trim() : opt;
                if (opt.Equals(answer, StringComparison.OrdinalIgnoreCase) ||
                    cleanOpt.Equals(answer, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1; // Unknown
        }

        private static string CalculateGrade(double score)
        {
            return score switch
            {
                >= 9.0 => "A+",
                >= 8.0 => "A",
                >= 7.0 => "B+",
                >= 6.5 => "B",
                >= 5.0 => "C",
                >= 3.5 => "D",
                _ => "F"
            };
        }

        /// <summary>
        /// Auto-group A/B/C:
        /// A = ≥ 8.0 (Giỏi)
        /// B = 5.0–7.9 (Khá)
        /// C = < 5.0 (Cần cải thiện)
        /// </summary>
        private static string CalculateGroup(double score)
        {
            if (score >= 8.0) return "A";
            if (score >= 5.0) return "B";
            return "C";
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  DATA MODELS
    // ═══════════════════════════════════════════════════════════

    public enum QuizSessionStatus { Ready, Active, Finished }

    public class MathQuizSession
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public List<MathQuizQuestion> Questions { get; set; } = new();
        public int TimeLimitSeconds { get; set; }
        public QuizSessionStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public ConcurrentDictionary<string, StudentAnswerResult> StudentAnswers { get; } = new();
    }

    public class MathQuizQuestion
    {
        public int Index { get; set; }
        public string Text { get; set; } = "";
        public List<string> Options { get; set; } = new();
        public string CorrectAnswer { get; set; } = "";
        public string Difficulty { get; set; } = "Medium";
        public string Solution { get; set; } = "";
        public string ChapterName { get; set; } = "";
        public int Grade { get; set; }
        public int Points { get; set; } = 10;
        public string QuestionType { get; set; } = "MCQ";
    }

    public class StudentAnswerResult
    {
        public string StudentCode { get; set; } = "";
        public int Correct { get; set; }
        public int Total { get; set; }
        public double Score { get; set; }
        public int Duration { get; set; }
        public Dictionary<int, int> Answers { get; set; } = new();
        public string Grade { get; set; } = "";
        public string Group { get; set; } = ""; // A, B, or C
        public DateTime SubmittedAt { get; set; }
    }

    public class MathQuizSummary
    {
        public string QuizId { get; set; } = "";
        public string Title { get; set; } = "";
        public int TotalQuestions { get; set; }
        public int TotalStudents { get; set; }
        public double ClassAvgScore { get; set; }
        public int ClassAvgCorrect { get; set; }
        public double HighestScore { get; set; }
        public double LowestScore { get; set; }
        public int AvgDuration { get; set; }

        // A/B/C Auto-Grouping
        public List<string> GroupA { get; set; } = new(); // ≥ 8.0
        public List<string> GroupB { get; set; } = new(); // 5.0–7.9
        public List<string> GroupC { get; set; } = new(); // < 5.0

        public List<QuestionStat> QuestionStats { get; set; } = new();
        public List<StudentAnswerResult> AllResults { get; set; } = new();
    }

    public class QuestionStat
    {
        public int Index { get; set; }
        public string Text { get; set; } = "";
        public int CorrectCount { get; set; }
        public int AttemptCount { get; set; }
        public double CorrectRate { get; set; }
        public string Difficulty { get; set; } = "";
    }
}
