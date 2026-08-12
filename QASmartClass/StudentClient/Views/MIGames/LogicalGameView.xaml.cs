using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace QASmartClass.StudentClient.Views.MIGames
{
    public partial class LogicalGameView : UserControl, IMiGameControl
    {
        public event Action<bool, int> OnGameOver;

        private readonly DispatcherTimer _gameTimer;
        private readonly Random _random = new();
        
        private int _currentQuestion = 0;
        private const int TotalQuestions = 10;
        private double _timeLeft = 10.0;
        private double _maxTime = 10.0;
        
        private int _correctAnswerValue = 0;
        private int _score = 0;
        private int _combo = 0;
        private int _totalXp = 0;
        private bool _isGameRunning = false;

        public LogicalGameView()
        {
            InitializeComponent();
            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromMilliseconds(100);
            _gameTimer.Tick += GameTimer_Tick;
        }

        public void StartGame()
        {
            _currentQuestion = 0;
            _score = 0;
            _combo = 0;
            _totalXp = 0;
            _isGameRunning = true;
            BorderCombo.Visibility = Visibility.Collapsed;
            NextQuestion();
        }

        private void NextQuestion()
        {
            if (!_isGameRunning) return;

            _currentQuestion++;
            if (_currentQuestion > TotalQuestions)
            {
                EndGame(true);
                return;
            }

            TxtQuestionNum.Text = $"CÂU HỎI {_currentQuestion}/{TotalQuestions}";

            // Generate question
            // Time limit decreases as question number increases
            _maxTime = Math.Max(3.0, 11.0 - _currentQuestion * 0.8);
            _timeLeft = _maxTime;
            UpdateTimerUi();

            GenerateMathProblem();

            _gameTimer.Start();
        }

        private void GenerateMathProblem()
        {
            int opType = _random.Next(0, 4); // 0: +, 1: -, 2: *, 3: /
            int a, b;
            string opSymbol = "";

            if (opType == 0) // Addition
            {
                a = _random.Next(5, 50);
                b = _random.Next(5, 50);
                _correctAnswerValue = a + b;
                opSymbol = "+";
            }
            else if (opType == 1) // Subtraction
            {
                a = _random.Next(20, 100);
                b = _random.Next(5, a);
                _correctAnswerValue = a - b;
                opSymbol = "-";
            }
            else if (opType == 2) // Multiplication
            {
                a = _random.Next(2, 12);
                b = _random.Next(2, 12);
                _correctAnswerValue = a * b;
                opSymbol = "x";
            }
            else // Division
            {
                b = _random.Next(2, 10);
                _correctAnswerValue = _random.Next(2, 12);
                a = _correctAnswerValue * b;
                opSymbol = ":";
            }

            TxtQuestion.Text = $"{a} {opSymbol} {b} = ?";

            // Generate options
            var options = new List<int> { _correctAnswerValue };
            while (options.Count < 3)
            {
                int wrong = _correctAnswerValue + _random.Next(-10, 11);
                if (wrong != _correctAnswerValue && !options.Contains(wrong) && (wrong >= 0 || _correctAnswerValue < 0))
                {
                    options.Add(wrong);
                }
            }

            // Shuffle options
            for (int i = options.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                int temp = options[i];
                options[i] = options[j];
                options[j] = temp;
            }

            BtnOpt1.Content = options[0].ToString();
            BtnOpt2.Content = options[1].ToString();
            BtnOpt3.Content = options[2].ToString();
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            _timeLeft -= 0.1;
            if (_timeLeft <= 0)
            {
                _timeLeft = 0;
                _gameTimer.Stop();
                // Time's up is counted as incorrect, break combo, move to next
                _combo = 0;
                BorderCombo.Visibility = Visibility.Collapsed;
                NextQuestion();
            }
            UpdateTimerUi();
        }

        private void UpdateTimerUi()
        {
            TxtTime.Text = $"{_timeLeft:0.0}s";
            ProgressTimer.Value = (_timeLeft / _maxTime) * 100;
        }

        private void Answer_Click(object sender, RoutedEventArgs e)
        {
            if (!_isGameRunning) return;

            _gameTimer.Stop();

            if (sender is Button btn && int.TryParse(btn.Content.ToString(), out int selectedAnswer))
            {
                bool isCorrect = selectedAnswer == _correctAnswerValue;
                if (isCorrect)
                {
                    _combo++;
                    _score += 10 + (_combo > 1 ? _combo * 2 : 0);
                    _totalXp += 4 + _combo; // Gain more XP for higher combo

                    TxtScore.Text = $"Score: {_score}";
                    if (_combo > 1)
                    {
                        TxtCombo.Text = $"Combo x{_combo}";
                        BorderCombo.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    _combo = 0;
                    BorderCombo.Visibility = Visibility.Collapsed;
                }
                
                NextQuestion();
            }
        }

        private void EndGame(bool completed)
        {
            _isGameRunning = false;
            _gameTimer.Stop();
            // Game Over callback
            OnGameOver?.Invoke(completed, _totalXp);
        }
    }
}
