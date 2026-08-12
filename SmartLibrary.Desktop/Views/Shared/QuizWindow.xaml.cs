using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SmartLibrary.Desktop.Views.Shared;

public partial class QuizWindow : Window
{
    private readonly ClientQuizDto _quiz;
    private DispatcherTimer? _timer;
    private int _remainingSeconds = 60;

    public int QuizId => _quiz.QuizId;
    public int[] Answers { get; } = new int[] { -1, -1, -1, -1, -1 }; // -1 nghĩa là chưa chọn

    public QuizWindow(ClientQuizDto quiz)
    {
        InitializeComponent();
        _quiz = quiz;
        TxtBookTitle.Text = $"Sách: {quiz.BookTitle}";

        RenderQuestions();
        StartTimer();
        
        int totalQuestions = quiz.Questions.Count;
        int requiredCorrect = (int)Math.Ceiling(totalQuestions * 0.8);
        TxtWarning.Text = $"⚠️ Trả lời đúng tối thiểu {requiredCorrect}/{totalQuestions} câu để nhận 20 XP";
        
        Loaded += (s, e) => Focus();
    }

    private void RenderQuestions()
    {
        QuestionsPanel.Children.Clear();
        var optionForeground = (System.Windows.Media.Brush)Application.Current.FindResource("TextPrimaryBrush");

        for (int i = 0; i < _quiz.Questions.Count; i++)
        {
            var q = _quiz.Questions[i];

            var qStack = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };

            var qText = new TextBlock
            {
                Text = $"Câu {i + 1}: {q.QuestionText}",
                Foreground = optionForeground,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
            qStack.Children.Add(qText);

            for (int j = 0; j < q.Options.Length; j++)
            {
                var rb = new RadioButton
                {
                    Content = q.Options[j],
                    GroupName = $"Q_{i}",
                    Foreground = optionForeground,
                    Margin = new Thickness(8, 4, 0, 4),
                    Tag = new int[] { i, j }
                };
                rb.Checked += Rb_Checked;
                qStack.Children.Add(rb);
            }

            QuestionsPanel.Children.Add(qStack);
        }
    }

    private void Rb_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is int[] tag)
        {
            int questionIdx = tag[0];
            int optionIdx = tag[1];
            if (questionIdx >= 0 && questionIdx < Answers.Length)
            {
                Answers[questionIdx] = optionIdx;
            }
        }
    }

    private void StartTimer()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) =>
        {
            _remainingSeconds--;
            if (_remainingSeconds <= 0)
            {
                _timer.Stop();
                TxtTimer.Text = "00:00";
                MessageBox.Show("⏰ Hết giờ làm bài! Hệ thống tự động nộp bài.", "Hết giờ", MessageBoxButton.OK, MessageBoxImage.Warning);
                SubmitAndClose();
            }
            else
            {
                int min = _remainingSeconds / 60;
                int sec = _remainingSeconds % 60;
                TxtTimer.Text = $"{min:00}:{sec:00}";
            }
        };
        _timer.Start();
    }

    private void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        // Kiểm tra xem đã làm hết chưa
        for (int i = 0; i < Answers.Length; i++)
        {
            if (Answers[i] == -1)
            {
                var res = MessageBox.Show($"Bạn chưa trả lời câu hỏi {i + 1}. Bạn có chắc chắn muốn nộp bài luôn không?", "Cảnh báo", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.No) return;
            }
        }

        SubmitAndClose();
    }

    private void SubmitAndClose()
    {
        _timer?.Stop();
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        _timer?.Stop();
        DialogResult = false;
        Close();
    }

    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
        {
            this.DragMove();
        }
    }
}

public class ClientQuizDto
{
    public int QuizId { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public List<ClientQuestionDto> Questions { get; set; } = new();
}

public class ClientQuestionDto
{
    public int QuestionIndex { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string[] Options { get; set; } = Array.Empty<string>();
}
