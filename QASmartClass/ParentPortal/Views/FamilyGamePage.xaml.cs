using QASmartClass.Data;
using QASmartClass.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.ParentPortal.Views
{
    public partial class FamilyGamePage : Page
    {
        private readonly AppDbContext _db;
        private readonly FamilyGameService _gameService;
        private int _parentId = 1;
        private int _studentId = 1;
        
        private List<FamilyQuizQuestion> _questions;
        private int _currentQIndex = 0;
        private int _score = 0;
        private int _selectedOptionIndex = -1;

        public FamilyGamePage(AppDbContext db, int parentId, int studentId)
        {
            InitializeComponent();
            _db = db;
            _gameService = new FamilyGameService(_db);
            _parentId = parentId;
            _studentId = studentId;

            Loaded += (_, __) => 
            {
                LoadLeaderboard();
            };
        }

        private void BtnStartGame_Click(object sender, RoutedEventArgs e)
        {
            GridWelcome.Visibility = Visibility.Collapsed;
            GridQuiz.Visibility = Visibility.Visible;
            StartGame();
        }

        private void LoadLeaderboard()
        {
            var lb = _gameService.GetLeaderboard();
            var items = new List<dynamic>();
            int rank = 1;
            foreach (var item in lb)
            {
                int pid = item.ParentId;
                var student = _db.Students.FirstOrDefault(s => s.Id == pid);
                string name = student != null ? $"PH em {student.FullName}" : $"Phụ huynh #{pid}";
                items.Add(new { ParentName = $"{rank}. {name}", ScoreText = $"{item.HighestScore} điểm" });
                rank++;
            }
            LvLeaderboard.ItemsSource = items;
        }

        private void StartGame()
        {
            _questions = _gameService.GenerateQuizForParent(_parentId, _studentId);
            _currentQIndex = 0;
            _score = 0;
            ShowQuestion();
        }

        private void ShowQuestion()
        {
            if (_currentQIndex >= _questions.Count)
            {
                EndGame();
                return;
            }

            var q = _questions[_currentQIndex];
            TxtQuestion.Text = q.Question;
            TxtProgress.Text = $"Câu {_currentQIndex + 1}/{_questions.Count}";
            _selectedOptionIndex = -1;
            BtnNext.Visibility = Visibility.Collapsed;

            SpOptions.Children.Clear();
            var rnd = new Random();
            var shuffledOptions = q.Options
                .Select((opt, index) => new { Content = opt, OriginalIndex = index })
                .OrderBy(x => rnd.Next())
                .ToList();

            for (int i = 0; i < shuffledOptions.Count; i++)
            {
                var opt = shuffledOptions[i];
                var btn = new Button
                {
                    Content = opt.Content,
                    Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                    Padding = new Thickness(15),
                    Margin = new Thickness(0, 0, 0, 10),
                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.Transparent,
                    FontSize = 16,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Tag = opt.OriginalIndex
                };
                btn.Click += (s, e) => Option_Click(btn, opt.OriginalIndex);
                SpOptions.Children.Add(btn);
            }
        }

        private void Option_Click(Button clickedBtn, int idx)
        {
            // Only allow 1 selection
            if (_selectedOptionIndex != -1) return;
            
            _selectedOptionIndex = idx;
            var q = _questions[_currentQIndex];
            int correctIdx = q.CorrectIndex;

            foreach (Button btn in SpOptions.Children)
            {
                btn.IsHitTestVisible = false; // Disable clicking again
            }

            if (idx == correctIdx)
            {
                clickedBtn.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
                clickedBtn.Foreground = Brushes.White;
                _score += 20; // 20 pts per question
            }
            else
            {
                clickedBtn.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                clickedBtn.Foreground = Brushes.White;
                // highlight correct
                foreach (Button btn in SpOptions.Children)
                {
                    if (btn.Tag is int oIdx && oIdx == correctIdx)
                    {
                        btn.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                        break;
                    }
                }
            }

            BtnNext.Visibility = Visibility.Visible;
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            _currentQIndex++;
            ShowQuestion();
        }

        private void EndGame()
        {
            TxtQuestion.Text = $"Hoàn thành! Bạn được {_score}/100 điểm.";
            SpOptions.Children.Clear();
            BtnNext.Visibility = Visibility.Collapsed;
            TxtProgress.Text = "";

            _gameService.SaveGameSession(_parentId, _studentId, _score);
            LoadLeaderboard();

            var btnReplay = new Button
            {
                Content = "Chơi lại",
                Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Foreground = Brushes.White,
                Padding = new Thickness(20, 10, 20, 10),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 30, 0, 0)
            };
            btnReplay.Click += (_, __) => StartGame();
            SpOptions.Children.Add(btnReplay);
        }
    }
}

