using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Serilog;

namespace QASmartClass.StudentClient.Views
{
    public partial class StudentSurveyPage : Page
    {
        private readonly List<string> _activeOptions = new();
        private string _activeQuestion = string.Empty;
        private string _activeTitle = "KHẢO SÁT NHANH TRONG LỚP";
        private string _renderedPollId = string.Empty;
        private int _selectedOptionIdx = -1;
        private bool _isSubmittingSurvey = false;

        public StudentSurveyPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            QASmartTouch.App.AssessmentState.StateChanged -= OnAssessmentStateChanged;
            QASmartTouch.App.AssessmentState.StateChanged += OnAssessmentStateChanged;
            RefreshFromEphemeralState();
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            QASmartTouch.App.AssessmentState.StateChanged -= OnAssessmentStateChanged;
            StopConfettiAnimation();
        }

        private void OnAssessmentStateChanged(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(RefreshFromEphemeralState));
        }

        /// <summary>
        /// Đọc 100% từ trạng thái RAM tạm thời của phiên lớp học hiện tại (QASmartTouch.App.AssessmentState).
        /// Tuyệt đối không đọc hay ghi SQLite Database. Khi GV kết thúc khảo sát hoặc rời lớp -> tự xóa sạch.
        /// </summary>
        public void RefreshFromEphemeralState()
        {
            try
            {
                var app = Application.Current as QASmartTouch.App;
                var state = QASmartTouch.App.AssessmentState;
                string rawSurvey = state.ActiveSurveyQuestion?.Trim() ?? string.Empty;
                bool isConnected = app?.StudentNetwork?.IsConnected == true;

                // Nếu không có khảo sát nào đang mở trên RAM
                if (string.IsNullOrWhiteSpace(rawSurvey))
                {
                    _renderedPollId = string.Empty;
                    _activeQuestion = string.Empty;
                    _activeOptions.Clear();
                    _selectedOptionIdx = -1;
                    _isSubmittingSurvey = false;

                    activeSurveyPanel.Visibility = Visibility.Collapsed;
                    thankYouPanel.Visibility = Visibility.Collapsed;
                    badgeLiveStatus.Visibility = Visibility.Collapsed;
                    emptyState.Visibility = Visibility.Visible;

                    if (!isConnected)
                    {
                        txtEmptyIcon.Text = "🔌";
                        txtEmptyTitle.Text = "Chưa vào lớp học trực tiếp";
                        txtEmptyDesc.Text = "Khảo sát nhanh chỉ hoạt động khi máy Học sinh đang kết nối trực tiếp vào lớp của Giáo viên.";
                        txtHeaderSubtitle.Text = "Hãy kết nối vào lớp học để nhận khảo sát trực tiếp";
                    }
                    else
                    {
                        txtEmptyIcon.Text = "📋";
                        txtEmptyTitle.Text = "Hiện chưa có khảo sát nào đang mở";
                        txtEmptyDesc.Text = "Khi Giáo viên phát khảo sát nhanh trên lớp, câu hỏi và các phương án lựa chọn sẽ hiển thị ngay tại đây.";
                        txtHeaderSubtitle.Text = $"Đang kết nối lớp {app?.StudentNetwork?.ClassName} — dữ liệu tự xóa sạch khi đóng khảo sát";
                    }
                    return;
                }

                // Giải mã thông tin khảo sát đang mở trên RAM
                string pollId = state.ActivePollId ?? rawSurvey;
                string parsedTitle = "KHẢO SÁT NHANH TỪ GIÁO VIÊN";
                string parsedQuestion = string.Empty;
                var parsedOptions = new List<string>();

                if (rawSurvey.StartsWith("{"))
                {
                    try
                    {
                        var jsonOpts = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var msg = System.Text.Json.JsonSerializer.Deserialize<StudentShell.JsonNetworkMessage>(rawSurvey, jsonOpts);
                        if (msg != null && msg.Action == "SURVEY_START")
                        {
                            var payload = System.Text.Json.JsonSerializer.Deserialize<StudentShell.SurveyStartPayload>(msg.Payload.GetRawText(), jsonOpts);
                            if (payload != null)
                            {
                                if (!string.IsNullOrWhiteSpace(payload.SurveyId))
                                    pollId = payload.SurveyId;
                                if (!string.IsNullOrWhiteSpace(payload.Title))
                                    parsedTitle = payload.Title.ToUpperInvariant();
                                parsedQuestion = payload.QuestionText ?? string.Empty;
                                if (payload.Options != null)
                                    parsedOptions.AddRange(payload.Options);
                            }
                        }
                    }
                    catch (Exception exJson)
                    {
                        Log.Warning("RefreshFromEphemeralState JSON parse error: {Err}", exJson.Message);
                    }
                }

                if (string.IsNullOrWhiteSpace(parsedQuestion))
                {
                    parsedQuestion = rawSurvey;
                    if (state.ActivePollOptions != null && state.ActivePollOptions.Length > 0)
                        parsedOptions.AddRange(state.ActivePollOptions);
                    else
                        parsedOptions.AddRange(new[] { "Đồng ý", "Không đồng ý" });
                }

                // Nếu là khảo sát mới khác với khảo sát đang chọn dở -> reset lựa chọn
                if (_renderedPollId != pollId)
                {
                    _renderedPollId = pollId;
                    _selectedOptionIdx = state.SurveyAnswered ? state.SelectedSurveyOptionIndex : -1;
                    _isSubmittingSurvey = false;
                }
                else if (state.SurveyAnswered && state.SelectedSurveyOptionIndex >= 0)
                {
                    _selectedOptionIdx = state.SelectedSurveyOptionIndex;
                }

                _activeTitle = parsedTitle;
                _activeQuestion = parsedQuestion;
                _activeOptions.Clear();
                _activeOptions.AddRange(parsedOptions);

                emptyState.Visibility = Visibility.Collapsed;
                activeSurveyPanel.Visibility = Visibility.Visible;
                badgeLiveStatus.Visibility = Visibility.Visible;

                txtSurveyTitle.Text = _activeTitle;
                txtSurveyQuestion.Text = _activeQuestion;

                bool answered = state.SurveyAnswered;
                if (answered)
                {
                    txtLiveStatus.Text = "✓ ĐÃ PHẢN HỒI";
                    txtLiveStatus.Foreground = new SolidColorBrush(Color.FromRgb(5, 150, 105));
                    badgeLiveStatus.Background = new SolidColorBrush(Color.FromRgb(236, 253, 245));
                    badgeLiveStatus.BorderBrush = new SolidColorBrush(Color.FromRgb(167, 243, 208));

                    btnSubmitSurvey.Content = "Đã gửi phản hồi tới Giáo viên";
                    btnSubmitSurvey.Background = new SolidColorBrush(Color.FromRgb(100, 116, 139));
                    btnSubmitSurvey.IsEnabled = false;

                    thankYouPanel.Visibility = Visibility.Visible;
                    txtSelectedSummary.Text = !string.IsNullOrWhiteSpace(state.SelectedSurveyOptionText)
                        ? $"Phương án bạn đã chọn: {state.SelectedSurveyOptionText}"
                        : "Phản hồi của bạn đã được ghi nhận trong phiên khảo sát này.";
                }
                else
                {
                    txtLiveStatus.Text = "● ĐANG DIỄN RA";
                    txtLiveStatus.Foreground = new SolidColorBrush(Color.FromRgb(26, 115, 232));
                    badgeLiveStatus.Background = new SolidColorBrush(Color.FromRgb(232, 240, 254));
                    badgeLiveStatus.BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254));

                    btnSubmitSurvey.Content = "Gửi phản hồi khảo sát";
                    btnSubmitSurvey.Background = new SolidColorBrush(Color.FromRgb(26, 115, 232));
                    btnSubmitSurvey.IsEnabled = true;

                    thankYouPanel.Visibility = Visibility.Collapsed;
                }

                RenderOptionCards(answered);
            }
            catch (Exception ex)
            {
                Log.Warning("RefreshFromEphemeralState error: {Err}", ex.Message);
            }
        }

        private void RenderOptionCards(bool isLocked)
        {
            panelSurveyOptions.Children.Clear();
            string[] badgeLetters = { "A", "B", "C", "D", "E", "F" };

            for (int i = 0; i < _activeOptions.Count; i++)
            {
                int idx = i;
                string optText = _activeOptions[i];
                bool isSelected = (_selectedOptionIdx == idx);

                var card = new Border
                {
                    Background = isSelected
                        ? new SolidColorBrush(Color.FromRgb(232, 240, 254))
                        : Brushes.White,
                    BorderBrush = isSelected
                        ? new SolidColorBrush(Color.FromRgb(26, 115, 232))
                        : new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    BorderThickness = new Thickness(isSelected ? 2 : 1.5),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(16, 13, 16, 13),
                    Margin = new Thickness(0, 0, 0, 10),
                    Cursor = isLocked ? Cursors.Arrow : Cursors.Hand
                };

                var row = new DockPanel();

                var letterBadge = new Border
                {
                    Width = 30,
                    Height = 30,
                    CornerRadius = new CornerRadius(15),
                    Background = isSelected
                        ? new SolidColorBrush(Color.FromRgb(26, 115, 232))
                        : new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    Margin = new Thickness(0, 0, 12, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                letterBadge.Child = new TextBlock
                {
                    Text = badgeLetters[idx % badgeLetters.Length],
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = isSelected
                        ? Brushes.White
                        : new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                row.Children.Add(letterBadge);

                if (isSelected)
                {
                    var checkIcon = new TextBlock
                    {
                        Text = "✓",
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(26, 115, 232)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(8, 0, 0, 0)
                    };
                    DockPanel.SetDock(checkIcon, Dock.Right);
                    row.Children.Add(checkIcon);
                }

                var lbl = new TextBlock
                {
                    Text = optText,
                    FontSize = 15,
                    FontWeight = isSelected ? FontWeights.Bold : FontWeights.Medium,
                    Foreground = isSelected
                        ? new SolidColorBrush(Color.FromRgb(23, 78, 166))
                        : new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };
                row.Children.Add(lbl);

                card.Child = row;

                if (!isLocked)
                {
                    card.MouseLeftButtonDown += (_, _) =>
                    {
                        _selectedOptionIdx = idx;
                        RenderOptionCards(false);
                    };
                }

                panelSurveyOptions.Children.Add(card);
            }
        }

        private void SubmitSurvey_Click(object sender, RoutedEventArgs e)
        {
            if (_isSubmittingSurvey || QASmartTouch.App.AssessmentState.SurveyAnswered)
                return;

            if (_selectedOptionIdx < 0 || _selectedOptionIdx >= _activeOptions.Count)
            {
                MessageBox.Show("Vui lòng chạm chọn một phương án phản hồi trước khi gửi!", "Khảo sát nhanh", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _isSubmittingSurvey = true;
            string selectedText = _activeOptions[_selectedOptionIdx];

            try
            {
                if (Window.GetWindow(this) is StudentShell shell)
                {
                    shell.SubmitSurveyVoteFromPage(_selectedOptionIdx, _activeQuestion, selectedText);
                }
                else
                {
                    var app = Application.Current as QASmartTouch.App;
                    string studentName = !string.IsNullOrWhiteSpace(app?.StudentNetwork?.StudentName)
                        ? app.StudentNetwork.StudentName.Trim()
                        : Environment.MachineName;

                    QASmartTouch.App.AssessmentState.SurveyAnswered = true;
                    QASmartTouch.App.AssessmentState.SelectedSurveyOptionIndex = _selectedOptionIdx;
                    QASmartTouch.App.AssessmentState.SelectedSurveyOptionText = selectedText;
                    QASmartTouch.App.AssessmentState.NotifyChanged();

                    string voteMsg = $"SURVEY_RESPONSE|opt{_selectedOptionIdx}|{studentName}";
                    if (app?.StudentNetwork?.IsConnected == true)
                    {
                        _ = app.StudentNetwork.SendAsync(voteMsg);
                    }
                    else
                    {
                        app?.RaiseLocalCommand("CMD|" + voteMsg);
                    }
                }

                StartConfettiAnimation();
                RefreshFromEphemeralState();
            }
            catch (Exception ex)
            {
                Log.Warning("SubmitSurvey_Click error: {Err}", ex.Message);
            }
            finally
            {
                _isSubmittingSurvey = false;
            }
        }

        #if false // [MYTHWARE_LITE] Giữ nguyên mã nguồn cũ (lưu DB SQLite, đánh giá sao theo ngày, đồng bộ Offline) để tham khảo khi cần khôi phục
        private void LoadSurveyHistory()
        {
            var app = (QASmartTouch.App)Application.Current;
            if (app?.Database == null) return;
            var localResponses = app.Database.SurveyResponses.ToList();
            var pendingSurvey = app.Database.EventLogs
                .Where(e => e.EventType == "SURVEY" && e.Actor == "GV")
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefault();
        }
        private void StarRate_Click(object sender, MouseButtonEventArgs e) { }
        private void Star_MouseEnter(object sender, MouseEventArgs e) { }
        private void Star_MouseLeave(object sender, MouseEventArgs e) { }
        private void StartOfflineSync() { }
        #endif

        private class ConfettiParticle
        {
            public System.Windows.Shapes.Rectangle Shape { get; set; } = null!;
            public double X { get; set; }
            public double Y { get; set; }
            public double SpeedY { get; set; }
            public double SpeedX { get; set; }
            public double Rotation { get; set; }
            public double RotationSpeed { get; set; }
        }

        private readonly List<ConfettiParticle> _confettiParticles = new();
        private DispatcherTimer? _confettiCleanupTimer;
        private bool _isConfettiRunning = false;

        private void StartConfettiAnimation()
        {
            try
            {
                StopConfettiAnimation();

                _isConfettiRunning = true;
                canvasConfetti.Children.Clear();
                _confettiParticles.Clear();

                var rand = new Random();
                var colors = new[] { Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Orange, Brushes.Purple, Brushes.DeepPink, Brushes.Cyan };

                double width = canvasConfetti.ActualWidth;
                if (width <= 0) width = 800;

                for (int i = 0; i < 60; i++)
                {
                    var rect = new System.Windows.Shapes.Rectangle
                    {
                        Width = rand.Next(6, 11),
                        Height = rand.Next(6, 11),
                        Fill = colors[rand.Next(colors.Length)],
                        RenderTransformOrigin = new Point(0.5, 0.5)
                    };

                    var particle = new ConfettiParticle
                    {
                        Shape = rect,
                        X = rand.NextDouble() * width,
                        Y = -rand.Next(10, 80),
                        SpeedY = rand.NextDouble() * 3 + 2,
                        SpeedX = (rand.NextDouble() - 0.5) * 2,
                        Rotation = rand.NextDouble() * 360,
                        RotationSpeed = (rand.NextDouble() - 0.5) * 10
                    };

                    canvasConfetti.Children.Add(rect);
                    _confettiParticles.Add(particle);
                }

                CompositionTarget.Rendering += OnConfettiRendering;

                _confettiCleanupTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
                _confettiCleanupTimer.Tick += (_, _) => StopConfettiAnimation();
                _confettiCleanupTimer.Start();
            }
            catch (Exception ex)
            {
                Log.Warning("StartConfettiAnimation error: {Err}", ex.Message);
            }
        }

        private void OnConfettiRendering(object? sender, EventArgs e)
        {
            if (!_isConfettiRunning) return;

            double canvasHeight = canvasConfetti.ActualHeight;
            if (canvasHeight <= 0) canvasHeight = 600;
            double canvasWidth = canvasConfetti.ActualWidth;
            if (canvasWidth <= 0) canvasWidth = 800;

            var rand = new Random();

            foreach (var p in _confettiParticles)
            {
                p.Y += p.SpeedY;
                p.X += p.SpeedX;
                p.Rotation += p.RotationSpeed;

                if (p.X < 0) p.X = canvasWidth;
                if (p.X > canvasWidth) p.X = 0;

                if (p.Y > canvasHeight)
                {
                    p.Y = -50;
                    p.X = rand.NextDouble() * canvasWidth;
                    p.SpeedY = rand.NextDouble() * 3 + 2;
                }

                Canvas.SetLeft(p.Shape, p.X);
                Canvas.SetTop(p.Shape, p.Y);
                p.Shape.RenderTransform = new RotateTransform(p.Rotation);
            }
        }

        private void StopConfettiAnimation()
        {
            if (_isConfettiRunning)
            {
                CompositionTarget.Rendering -= OnConfettiRendering;
                _isConfettiRunning = false;
            }

            if (_confettiCleanupTimer != null)
            {
                _confettiCleanupTimer.Stop();
                _confettiCleanupTimer = null;
            }

            canvasConfetti?.Children.Clear();
            _confettiParticles.Clear();
        }
    }
}
