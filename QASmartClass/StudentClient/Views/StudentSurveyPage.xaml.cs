using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.StudentClient.Views
{
    public partial class StudentSurveyPage : Page
    {
        private bool _isSubmittingSurvey = false;
        private bool _isSubmittingStar = false;
        private DateTime _pageLoadedTime = DateTime.MinValue;

        public StudentSurveyPage()
        {
            InitializeComponent();
            Loaded += (_, _) => {
                _pageLoadedTime = DateTime.Now;
                LoadSurveyHistory();
                RegisterNetworkEvents();
                StartOfflineSync();
            };
            Unloaded += (_, _) => {
                StopConfettiAnimation();
                UnregisterNetworkEvents();
            };
        }

        /// <summary>Load lịch sử khảo sát đã tham gia từ DB</summary>
        private void LoadSurveyHistory()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.Database == null) return;

                // Resolve student identity via StudentIdentityService
                var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(app.Database);
                var (_, currentCode, currentName) = identityService.GetCurrentStudent();

                // Get all responses from the local database
                var localResponses = app.Database.SurveyResponses.ToList();
                
                // Filter in-memory for current student's responses (including their anonymous ones using salted hash)
                var myResponses = localResponses
                    .Where(r => r.StudentCode == currentCode || r.StudentCode == ComputeSha256Hash(currentCode + r.SurveyId + GetLocalSalt()))
                    .OrderByDescending(r => r.SubmittedAt)
                    .Take(10)
                    .ToList();

                if (myResponses.Any())
                {
                    txtCompletedCount.Text = myResponses.Count.ToString();

                    // Show last survey
                    var last = myResponses.First();
                    txtLastSurvey.Text = $"{last.SelectedOptionText} — Hoàn thành lúc {last.SubmittedAt}";
                }
                else
                {
                    txtCompletedCount.Text = "0";
                    txtLastSurvey.Text = "Chưa có khảo sát nào được hoàn thành";
                }

                // Check if they already rated the lecture today
                string todayStarRatingId = "LECTURE_STAR_RATING_" + DateTime.Today.ToString("yyyyMMdd");
                var todayStarResponse = app.Database.SurveyResponses
                    .FirstOrDefault(r => r.SurveyId == todayStarRatingId && r.StudentCode == currentCode);

                if (todayStarResponse != null)
                {
                    int stars = todayStarResponse.SelectedOptionIndex;
                    _isSubmittingStar = true;
                    star1.Foreground = stars >= 1 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) : new SolidColorBrush(Color.FromRgb(223, 228, 234));
                    star2.Foreground = stars >= 2 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) : new SolidColorBrush(Color.FromRgb(223, 228, 234));
                    star3.Foreground = stars >= 3 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) : new SolidColorBrush(Color.FromRgb(223, 228, 234));
                    star4.Foreground = stars >= 4 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) : new SolidColorBrush(Color.FromRgb(223, 228, 234));
                    star5.Foreground = stars >= 5 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) : new SolidColorBrush(Color.FromRgb(223, 228, 234));
                    
                    ScaleStar(star1, stars >= 1 ? 1.1 : 1.0);
                    ScaleStar(star2, stars >= 2 ? 1.1 : 1.0);
                    ScaleStar(star3, stars >= 3 ? 1.1 : 1.0);
                    ScaleStar(star4, stars >= 4 ? 1.1 : 1.0);
                    ScaleStar(star5, stars >= 5 ? 1.1 : 1.0);
                    
                    txtStarResult.Text = $"✅ Đã đánh giá {stars}/5 — Cảm ơn em!";
                }
                else
                {
                    _isSubmittingStar = false;
                    ResetStarsVisual();
                }

                // Check for pending survey from teacher
                var pendingSurvey = app.Database.EventLogs
                    .Where(e => e.EventType == "SURVEY" && e.Actor == "GV")
                    .OrderByDescending(e => e.Timestamp)
                    .FirstOrDefault();

                if (pendingSurvey != null)
                {
                    // Check if the survey is older than 30 minutes (expired)
                    bool isExpired = (DateTime.Now - pendingSurvey.Timestamp).TotalMinutes > 30.0;

                    if (isExpired)
                    {
                        emptyState.Visibility = Visibility.Visible;
                        activeSurveyPanel.Visibility = Visibility.Collapsed;
                        thankYouPanel.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        string targetSurveyId = pendingSurvey.Id.ToString();
                        bool isAnonymousSurvey = false;

                        if (pendingSurvey.Details.Trim().StartsWith("{"))
                        {
                            try
                            {
                                var optionsObj = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                                var msg = System.Text.Json.JsonSerializer.Deserialize<StudentShell.JsonNetworkMessage>(pendingSurvey.Details, optionsObj);
                                if (msg != null && msg.Action == "SURVEY_START")
                                {
                                     var payload = System.Text.Json.JsonSerializer.Deserialize<StudentShell.SurveyStartPayload>(msg.Payload.GetRawText(), optionsObj);
                                     if (payload != null)
                                     {
                                         targetSurveyId = payload.SurveyId;
                                         isAnonymousSurvey = payload.IsAnonymous;
                                     }
                                }
                            }
                            catch { }
                        }

                        // Check if student has already voted (using hashed code check for anonymity, normal code check for public)
                        string checkStudentCode = isAnonymousSurvey ? ComputeSha256Hash(currentCode + targetSurveyId + GetLocalSalt()) : currentCode;
                        bool alreadyVoted = app.Database.SurveyResponses
                            .Any(r => r.SurveyId == targetSurveyId && r.StudentCode == checkStudentCode);

                        if (alreadyVoted)
                        {
                            emptyState.Visibility = Visibility.Collapsed;
                            activeSurveyPanel.Visibility = Visibility.Collapsed;
                            thankYouPanel.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            var question = pendingSurvey.Details?.Replace("Câu hỏi khảo sát: ", "") ?? "Em nghĩ sao về bài giảng?";
                            ShowActiveSurvey(question, pendingSurvey.Id);
                        }
                    }
                }
                else
                {
                    emptyState.Visibility = Visibility.Visible;
                    activeSurveyPanel.Visibility = Visibility.Collapsed;
                    thankYouPanel.Visibility = Visibility.Collapsed;
                }

                Log.Information("Survey page loaded: {Count} completed", myResponses.Count);
            }
            catch (Exception ex) { Log.Warning("LoadSurveyHistory error: {Err}", ex.Message); }
        }

        private string _activeSurveyId = "";
        private System.Collections.Generic.List<string> _activeOptions = new();
        private bool _activeIsAnonymous = false;

        private void ShowActiveSurvey(string details, int logId)
        {
            emptyState.Visibility = Visibility.Collapsed;
            activeSurveyPanel.Visibility = Visibility.Visible;
            panelSurveyOptions.Children.Clear();
            _activeOptions.Clear();

            string question = "";

            if (details.Trim().StartsWith("{"))
            {
                try
                {
                    var optionsObj = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var msg = System.Text.Json.JsonSerializer.Deserialize<StudentShell.JsonNetworkMessage>(details, optionsObj);
                    if (msg != null && msg.Action == "SURVEY_START")
                    {
                        var payload = System.Text.Json.JsonSerializer.Deserialize<StudentShell.SurveyStartPayload>(msg.Payload.GetRawText(), optionsObj);
                        if (payload != null)
                        {
                            question = payload.QuestionText;
                            _activeSurveyId = payload.SurveyId;
                            _activeOptions = payload.Options;
                            _activeIsAnonymous = payload.IsAnonymous;

                            string[] btnColors = { "#4CAF50", "#1976D2", "#FF9800", "#EF5350", "#9C27B0" };
                            for (int i = 0; i < payload.Options.Count; i++)
                            {
                                int idx = i;
                                string optionText = payload.Options[i];
                                var color = (Color)ColorConverter.ConvertFromString(btnColors[i % btnColors.Length]);

                                var btn = new Button
                                {
                                    Content = optionText,
                                    Tag = $"opt{idx}",
                                    Padding = new Thickness(16, 10, 16, 10),
                                    FontSize = 15,
                                    FontWeight = FontWeights.Bold,
                                    Margin = new Thickness(0, 0, 8, 6),
                                    Background = new SolidColorBrush(color),
                                    Foreground = Brushes.White,
                                    BorderThickness = new Thickness(0),
                                    Cursor = Cursors.Hand
                                };

                                var template = new ControlTemplate(typeof(Button));
                                var borderFactory = new FrameworkElementFactory(typeof(Border));
                                borderFactory.Name = "b";
                                borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                                borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
                                borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

                                var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
                                contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                                contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                                borderFactory.AppendChild(contentPresenterFactory);
                                template.VisualTree = borderFactory;

                                var trigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
                                trigger.Setters.Add(new Setter(Border.OpacityProperty, 0.85, "b"));
                                template.Triggers.Add(trigger);

                                btn.Template = template;
                                btn.Click += VoteDynamic_Click;
                                panelSurveyOptions.Children.Add(btn);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("ShowActiveSurvey JSON parse error: {Err}", ex.Message);
                }
            }

            if (string.IsNullOrEmpty(question))
            {
                if (details.Contains(" | "))
                {
                    var parts = details.Split(new[] { " | " }, StringSplitOptions.None);
                    question = parts[0].Replace("Khảo sát (custom): ", "").Replace("Khảo sát (satisfaction): ", "");
                }
                else
                {
                    question = details.Replace("Câu hỏi khảo sát: ", "") ?? "Em nghĩ sao về bài giảng?";
                }

                _activeSurveyId = logId.ToString();
                _activeOptions = new System.Collections.Generic.List<string> { "👍 Đồng ý", "👎 Không đồng ý", "🤔 Có thể" };
                _activeIsAnonymous = false;
                string[] tags = { "yes", "no", "maybe" };
                string[] btnColors = { "#4CAF50", "#EF5350", "#FFB300" };

                for (int i = 0; i < _activeOptions.Count; i++)
                {
                    var btn = new Button
                    {
                        Content = _activeOptions[i],
                        Tag = tags[i],
                        Padding = new Thickness(16, 10, 16, 10),
                        FontSize = 15,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 0, 8, 6),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnColors[i])),
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(0),
                        Cursor = Cursors.Hand
                    };

                    var template = new ControlTemplate(typeof(Button));
                    var borderFactory = new FrameworkElementFactory(typeof(Border));
                    borderFactory.Name = "b";
                    borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                    borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
                    borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

                    var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
                    contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                    contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                    borderFactory.AppendChild(contentPresenterFactory);
                    template.VisualTree = borderFactory;

                    var trigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
                    trigger.Setters.Add(new Setter(Border.OpacityProperty, 0.85, "b"));
                    template.Triggers.Add(trigger);

                    btn.Template = template;
                    btn.Click += Vote_Click;
                    panelSurveyOptions.Children.Add(btn);
                }
            }

            txtSurveyQuestion.Text = question;
            activeSurveyPanel.Tag = logId;
        }

        private void VoteDynamic_Click(object sender, RoutedEventArgs e)
        {
            if (_isSubmittingSurvey) return;
            if (sender is Button btn && btn.Tag is string voteKey)
            {
                _isSubmittingSurvey = true;

                foreach (var child in panelSurveyOptions.Children)
                {
                    if (child is Button b) b.IsEnabled = false;
                }

                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    if (app?.Database == null)
                    {
                        _isSubmittingSurvey = false;
                        return;
                    }

                    var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(app.Database);
                    var (_, currentCode, currentName) = identityService.GetCurrentStudent();
                    string optionText = btn.Content?.ToString() ?? "";

                    // Safe Response Time Calculation
                    var activeTime = QASmartTouch.App.AssessmentState.ActiveSurveyTime;
                    int responseSeconds = 0;
                    if (activeTime > DateTime.MinValue && DateTime.Now > activeTime)
                    {
                        double diff = (DateTime.Now - activeTime).TotalSeconds;
                        responseSeconds = diff > int.MaxValue ? int.MaxValue : (int)diff;
                    }

                    string finalStudentCode = currentCode;
                    string finalStudentName = currentName;
                    
                    // True Anonymity - remove identity fields for network
                    string netStudentCode = currentCode;
                    string netStudentName = currentName;

                    if (_activeIsAnonymous)
                    {
                        finalStudentCode = ComputeSha256Hash(currentCode + _activeSurveyId + GetLocalSalt());
                        finalStudentName = "Ẩn danh";
                        netStudentCode = ""; // Omit for security
                        netStudentName = "Ẩn danh";
                    }

                    var newResponse = new SurveyResponse
                    {
                        Id = Guid.NewGuid().ToString(),
                        SurveyId = _activeSurveyId,
                        StudentCode = finalStudentCode,
                        StudentName = finalStudentName,
                        SelectedOptionIndex = int.TryParse(voteKey.Replace("opt", ""), out int idx) ? idx : 0,
                        SelectedOptionText = optionText,
                        ResponseTimeSeconds = responseSeconds,
                        SubmittedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        IsSynced = 0 // Will set to 1 if network succeeds
                    };

                    var newEvent = new EventLog
                    {
                        EventType = "SURVEY_RESPONSE",
                        Actor = _activeIsAnonymous ? "Student" : currentCode,
                        Details = $"Trả lời khảo sát: {voteKey} — Câu hỏi: {txtSurveyQuestion.Text}",
                        Timestamp = DateTime.Now
                    };

                    var client = app.StudentNetwork;
                    if (client != null && client.IsConnected)
                    {
                        var votePayload = new
                        {
                            SurveyId = _activeSurveyId,
                            StudentCode = netStudentCode,
                            StudentName = netStudentName,
                            SelectedOptionIndex = newResponse.SelectedOptionIndex,
                            SelectedOptionText = optionText
                        };
                        var voteMsg = new
                        {
                            Action = "SURVEY_VOTE",
                            Payload = votePayload
                        };
                        string jsonVote = System.Text.Json.JsonSerializer.Serialize(voteMsg);

                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            int retryCount = 0;
                            bool sent = false;
                            while (retryCount < 3 && !sent)
                            {
                                try
                                {
                                    await client.SendAsync(jsonVote);
                                    sent = true;
                                }
                                catch (Exception ex)
                                {
                                    retryCount++;
                                    Log.Warning("Failed to send survey vote JSON, retry {Count}/3: {Err}", retryCount, ex.Message);
                                    if (retryCount < 3) await System.Threading.Tasks.Task.Delay(1000);
                                }
                            }

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (sent)
                                {
                                    newResponse.IsSynced = 1;
                                }
                                else
                                {
                                    Log.Information("Survey vote offline-saved (will sync later)");
                                }

                                try
                                {
                                    app.Database.SurveyResponses.Add(newResponse);
                                    app.Database.EventLogs.Add(newEvent);
                                    app.Database.SaveChanges();
                                }
                                catch (Exception dbEx) { Log.Error(dbEx, "Failed to save response to DB"); }

                                activeSurveyPanel.Visibility = Visibility.Collapsed;
                                thankYouPanel.Visibility = Visibility.Visible;
                                StartConfettiAnimation();
                                _isSubmittingSurvey = false;
                            });
                        });
                    }
                    else
                    {
                        // Offline mode
                        Log.Information("Survey vote offline-saved (no active network connection)");
                        try
                        {
                            app.Database.SurveyResponses.Add(newResponse);
                            app.Database.EventLogs.Add(newEvent);
                            app.Database.SaveChanges();
                        }
                        catch (Exception dbEx) { Log.Error(dbEx, "Failed to save offline response to DB"); }

                        activeSurveyPanel.Visibility = Visibility.Collapsed;
                        thankYouPanel.Visibility = Visibility.Visible;
                        StartConfettiAnimation();
                        _isSubmittingSurvey = false;
                    }

                    Log.Information("Survey dynamic vote processed: {VoteKey} - {Text}", voteKey, optionText);
                }
                catch (Exception ex)
                {
                    Log.Warning("VoteDynamic error: {Err}", ex.Message);
                    _isSubmittingSurvey = false;
                    foreach (var child in panelSurveyOptions.Children)
                    {
                        if (child is Button b) b.IsEnabled = true;
                    }
                }
            }
        }

        private void Vote_Click(object sender, RoutedEventArgs e)
        {
            if (_isSubmittingSurvey) return;
            if (sender is Button btn && btn.Tag is string vote)
            {
                _isSubmittingSurvey = true;
                foreach (var child in panelSurveyOptions.Children)
                {
                    if (child is Button b) b.IsEnabled = false;
                }
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    if (app?.Database == null)
                    {
                        _isSubmittingSurvey = false;
                        foreach (var child in panelSurveyOptions.Children)
                        {
                            if (child is Button b) b.IsEnabled = true;
                        }
                        return;
                    }

                    var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(app.Database);
                    var (_, currentCode, currentName) = identityService.GetCurrentStudent();
                    string optionText = btn.Content?.ToString() ?? vote;

                    // Safe Response Time Calculation
                    var activeTime = QASmartTouch.App.AssessmentState.ActiveSurveyTime;
                    int responseSeconds = 0;
                    if (activeTime > DateTime.MinValue && DateTime.Now > activeTime)
                    {
                        double diff = (DateTime.Now - activeTime).TotalSeconds;
                        responseSeconds = diff > int.MaxValue ? int.MaxValue : (int)diff;
                    }

                    string finalStudentCode = currentCode;
                    string finalStudentName = currentName;
                    string netStudentName = currentName;

                    if (_activeIsAnonymous)
                    {
                        finalStudentCode = ComputeSha256Hash(currentCode + _activeSurveyId + GetLocalSalt());
                        finalStudentName = "Ẩn danh";
                        netStudentName = "Ẩn danh";
                    }

                    // Map vote to SelectedOptionIndex
                    int optionIdx = 0;
                    if (vote == "no") optionIdx = 1;
                    else if (vote == "maybe") optionIdx = 2;

                    var newResponse = new SurveyResponse
                    {
                        Id = Guid.NewGuid().ToString(),
                        SurveyId = _activeSurveyId,
                        StudentCode = finalStudentCode,
                        StudentName = finalStudentName,
                        SelectedOptionIndex = optionIdx,
                        SelectedOptionText = optionText,
                        ResponseTimeSeconds = responseSeconds,
                        SubmittedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        IsSynced = 0
                    };

                    var newEvent = new EventLog
                    {
                        EventType = "SURVEY_RESPONSE",
                        Actor = _activeIsAnonymous ? "Student" : currentCode,
                        Details = $"Trả lời khảo sát: {vote} — Câu hỏi: {txtSurveyQuestion.Text}",
                        Timestamp = DateTime.Now
                    };

                    var client = app.StudentNetwork;
                    if (client != null && client.IsConnected)
                    {
                        // Fix the legacy network protocol bug: SURVEY_RESPONSE|vote|studentName
                        string netMessage = $"SURVEY_RESPONSE|{vote}|{netStudentName}";

                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            int retryCount = 0;
                            bool sent = false;
                            while (retryCount < 3 && !sent)
                            {
                                try
                                {
                                    await client.SendAsync(netMessage);
                                    sent = true;
                                }
                                catch (Exception ex)
                                {
                                    retryCount++;
                                    Log.Warning("Failed to send survey response, retry {Count}/3: {Err}", retryCount, ex.Message);
                                    if (retryCount < 3) await System.Threading.Tasks.Task.Delay(1000);
                                }
                            }

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (sent)
                                {
                                    newResponse.IsSynced = 1;
                                }
                                else
                                {
                                    Log.Information("Survey legacy response offline-saved (will sync later)");
                                }

                                try
                                {
                                    app.Database.SurveyResponses.Add(newResponse);
                                    app.Database.EventLogs.Add(newEvent);
                                    app.Database.SaveChanges();
                                }
                                catch (Exception dbEx) { Log.Error(dbEx, "Failed to save legacy response to DB"); }

                                activeSurveyPanel.Visibility = Visibility.Collapsed;
                                thankYouPanel.Visibility = Visibility.Visible;
                                StartConfettiAnimation();
                                _isSubmittingSurvey = false;
                            });
                        });
                    }
                    else
                    {
                        // Offline mode for legacy survey
                        Log.Information("Survey legacy response offline-saved (no network connection)");
                        try
                        {
                            app.Database.SurveyResponses.Add(newResponse);
                            app.Database.EventLogs.Add(newEvent);
                            app.Database.SaveChanges();
                        }
                        catch (Exception dbEx) { Log.Error(dbEx, "Failed to save offline legacy response to DB"); }

                        activeSurveyPanel.Visibility = Visibility.Collapsed;
                        thankYouPanel.Visibility = Visibility.Visible;
                        StartConfettiAnimation();
                        _isSubmittingSurvey = false;
                    }

                    Log.Information("Survey vote processed: {Vote}", vote);
                }
                catch (Exception ex)
                {
                    Log.Warning("Vote error: {Err}", ex.Message);
                    _isSubmittingSurvey = false;
                    foreach (var child in panelSurveyOptions.Children)
                    {
                        if (child is Button b) b.IsEnabled = true;
                    }
                }
            }
        }

        private void StarRate_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isSubmittingStar) return;
            if (sender is FrameworkElement fe && fe.Tag is string tag && int.TryParse(tag, out int stars))
            {
                _isSubmittingStar = true;
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    if (app?.Database == null)
                    {
                        _isSubmittingStar = false;
                        return;
                    }

                    var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(app.Database);
                    var (_, currentCode, _) = identityService.GetCurrentStudent();

                    // Update star colors visually immediately to provide snappy feedback
                    star1.Foreground = stars >= 1 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) : new SolidColorBrush(Color.FromRgb(223, 228, 234));
                    star2.Foreground = stars >= 2 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) : new SolidColorBrush(Color.FromRgb(223, 228, 234));
                    star3.Foreground = stars >= 3 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) : new SolidColorBrush(Color.FromRgb(223, 228, 234));
                    star4.Foreground = stars >= 4 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) : new SolidColorBrush(Color.FromRgb(223, 228, 234));
                    star5.Foreground = stars >= 5 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) : new SolidColorBrush(Color.FromRgb(223, 228, 234));

                    ScaleStar(star1, stars >= 1 ? 1.1 : 1.0);
                    ScaleStar(star2, stars >= 2 ? 1.1 : 1.0);
                    ScaleStar(star3, stars >= 3 ? 1.1 : 1.0);
                    ScaleStar(star4, stars >= 4 ? 1.1 : 1.0);
                    ScaleStar(star5, stars >= 5 ? 1.1 : 1.0);

                    // Safe Response Time Calculation
                    int responseSeconds = 0;
                    if (_pageLoadedTime > DateTime.MinValue && DateTime.Now > _pageLoadedTime)
                    {
                        double diff = (DateTime.Now - _pageLoadedTime).TotalSeconds;
                        responseSeconds = diff > int.MaxValue ? int.MaxValue : (int)diff;
                    }

                    var newEvent = new EventLog
                    {
                        EventType = "SURVEY_RESPONSE",
                        Actor = currentCode,
                        Details = $"Đánh giá sao: {stars}/5",
                        Timestamp = DateTime.Now
                    };

                    // Create a survey response for daily star rating
                    var newResponse = new SurveyResponse
                    {
                        Id = Guid.NewGuid().ToString(),
                        SurveyId = "LECTURE_STAR_RATING_" + DateTime.Today.ToString("yyyyMMdd"),
                        StudentCode = currentCode,
                        StudentName = currentCode,
                        SelectedOptionIndex = stars,
                        SelectedOptionText = $"Đánh giá sao: {stars}/5",
                        ResponseTimeSeconds = responseSeconds,
                        SubmittedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        IsSynced = 0
                    };

                    var client = app.StudentNetwork;
                    if (client != null && client.IsConnected)
                    {
                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            int retryCount = 0;
                            bool sent = false;
                            while (retryCount < 3 && !sent)
                            {
                                try
                                {
                                    await client.SendAsync($"SURVEY_STAR|{stars}");
                                    sent = true;
                                }
                                catch (Exception ex)
                                {
                                    retryCount++;
                                    Log.Warning("Failed to send survey star, retry {Count}/3: {Err}", retryCount, ex.Message);
                                    if (retryCount < 3) await System.Threading.Tasks.Task.Delay(1000);
                                }
                            }

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (sent)
                                {
                                    newResponse.IsSynced = 1;
                                    txtStarResult.Text = $"✅ Đã đánh giá {stars}/5 — Cảm ơn em!";
                                }
                                else
                                {
                                    // Keep star visuals but tell them it's saved locally
                                    txtStarResult.Text = $"✅ Đã đánh giá {stars}/5 (Đã ghi nhận offline)";
                                }

                                try
                                {
                                    app.Database.SurveyResponses.Add(newResponse);
                                    app.Database.EventLogs.Add(newEvent);
                                    app.Database.SaveChanges();
                                }
                                catch (Exception dbEx) { Log.Error(dbEx, "Failed to save star rating to DB"); }
                            });
                        });
                    }
                    else
                    {
                        // Offline mode for star rate
                        txtStarResult.Text = $"✅ Đã đánh giá {stars}/5 (Đã ghi nhận offline)";
                        try
                        {
                            app.Database.SurveyResponses.Add(newResponse);
                            app.Database.EventLogs.Add(newEvent);
                            app.Database.SaveChanges();
                        }
                        catch (Exception dbEx) { Log.Error(dbEx, "Failed to save offline star rating to DB"); }
                    }

                    Log.Information("Star rating: {Stars}/5", stars);
                }
                catch (Exception ex)
                {
                    Log.Warning("Star rate error: {Err}", ex.Message);
                    _isSubmittingStar = false;
                    ResetStarsVisual();
                }
            }
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (var sha256Hash = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
                var builder = new System.Text.StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void ResetStarsVisual()
        {
            var greyBrush = new SolidColorBrush(Color.FromRgb(223, 228, 234));
            star1.Foreground = greyBrush;
            star2.Foreground = greyBrush;
            star3.Foreground = greyBrush;
            star4.Foreground = greyBrush;
            star5.Foreground = greyBrush;
            txtStarResult.Text = "";
        }

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

        private System.Collections.Generic.List<ConfettiParticle> _confettiParticles = new();
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
                var colors = new[] { Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Yellow, Brushes.Orange, Brushes.Purple, Brushes.DeepPink, Brushes.Cyan };

                double width = canvasConfetti.ActualWidth;
                if (width <= 0) width = 800; // Realistic default for full-screen overlay

                for (int i = 0; i < 80; i++) // Increased from 40 to 80 for whole screen coverage
                {
                    var rect = new System.Windows.Shapes.Rectangle
                    {
                        Width = rand.Next(6, 12),
                        Height = rand.Next(6, 12),
                        Fill = colors[rand.Next(colors.Length)],
                        RenderTransformOrigin = new Point(0.5, 0.5)
                    };

                    var particle = new ConfettiParticle
                    {
                        Shape = rect,
                        X = rand.NextDouble() * width,
                        Y = -rand.Next(10, 80), // Spawn higher and spread out vertically
                        SpeedY = rand.NextDouble() * 3 + 2,
                        SpeedX = (rand.NextDouble() - 0.5) * 2,
                        Rotation = rand.NextDouble() * 360,
                        RotationSpeed = (rand.NextDouble() - 0.5) * 10
                    };

                    canvasConfetti.Children.Add(rect);
                    _confettiParticles.Add(particle);
                }

                CompositionTarget.Rendering += OnConfettiRendering;

                _confettiCleanupTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                _confettiCleanupTimer.Tick += (s, e) =>
                {
                    StopConfettiAnimation();
                };
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
            if (canvasHeight <= 0) canvasHeight = 600; // Realistic default for full-screen overlay
            double canvasWidth = canvasConfetti.ActualWidth;
            if (canvasWidth <= 0) canvasWidth = 800; // Realistic default for full-screen overlay

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
                    p.Y = -50; // Restart from top of screen
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

            if (canvasConfetti != null)
            {
                canvasConfetti.Children.Clear();
            }
            _confettiParticles.Clear();
        }

        // ═══ CUSTOM UI/UX HOVER EVENTS FOR STAR RATING ═══
        private void Star_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_isSubmittingStar) return;
            if (sender is FrameworkElement fe && int.TryParse(fe.Tag?.ToString(), out int hoveredStar))
            {
                var yellowBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                var greyBrush = new SolidColorBrush(Color.FromRgb(223, 228, 234));

                star1.Foreground = hoveredStar >= 1 ? yellowBrush : greyBrush;
                star2.Foreground = hoveredStar >= 2 ? yellowBrush : greyBrush;
                star3.Foreground = hoveredStar >= 3 ? yellowBrush : greyBrush;
                star4.Foreground = hoveredStar >= 4 ? yellowBrush : greyBrush;
                star5.Foreground = hoveredStar >= 5 ? yellowBrush : greyBrush;

                // Scale up hovered stars and those before it
                ScaleStar(star1, hoveredStar >= 1 ? 1.25 : 1.0);
                ScaleStar(star2, hoveredStar >= 2 ? 1.25 : 1.0);
                ScaleStar(star3, hoveredStar >= 3 ? 1.25 : 1.0);
                ScaleStar(star4, hoveredStar >= 4 ? 1.25 : 1.0);
                ScaleStar(star5, hoveredStar >= 5 ? 1.25 : 1.0);
            }
        }

        private void Star_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isSubmittingStar) return;
            ResetStarsVisual();
            ScaleStar(star1, 1.0);
            ScaleStar(star2, 1.0);
            ScaleStar(star3, 1.0);
            ScaleStar(star4, 1.0);
            ScaleStar(star5, 1.0);
        }

        // ═══ OFFLINE SYNCHRONIZATION LOGIC (THREAD-SAFE via UI Dispatcher) ═══
        private void RegisterNetworkEvents()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.StudentNetwork != null)
                {
                    app.StudentNetwork.Connected += Network_Connected;
                }
            }
            catch (Exception ex) { Log.Warning("RegisterNetworkEvents error: {Err}", ex.Message); }
        }

        private void UnregisterNetworkEvents()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.StudentNetwork != null)
                {
                    app.StudentNetwork.Connected -= Network_Connected;
                }
            }
            catch (Exception ex) { Log.Warning("UnregisterNetworkEvents error: {Err}", ex.Message); }
        }

        private void Network_Connected(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(StartOfflineSync));
        }

        private void StartOfflineSync()
        {
            var app = (QASmartTouch.App)Application.Current;
            if (app?.Database == null || app.StudentNetwork == null || !app.StudentNetwork.IsConnected) return;

            // Fetch unsynced responses on the UI thread to ensure EF DbContext thread safety
            var unsynced = app.Database.SurveyResponses.Where(r => r.IsSynced == 0).ToList();
            if (!unsynced.Any()) return;

            Log.Information("[OfflineSync] Found {Count} unsynced survey responses. Syncing...", unsynced.Count);

            System.Threading.Tasks.Task.Run(async () =>
            {
                var client = app.StudentNetwork;
                var syncedResponses = new System.Collections.Generic.List<SurveyResponse>();
                
                foreach (var response in unsynced)
                {
                    string msgToSend = "";
                    if (response.SurveyId.StartsWith("LECTURE_STAR_RATING"))
                    {
                        msgToSend = $"SURVEY_STAR|{response.SelectedOptionIndex}";
                    }
                    else if (response.SurveyId.StartsWith("STAR_RATING"))
                    {
                        msgToSend = $"SURVEY_STAR|{response.SelectedOptionIndex}";
                    }
                    else if (int.TryParse(response.SurveyId, out _)) // Legacy survey ID is integer
                    {
                        msgToSend = $"SURVEY_RESPONSE|{(response.SelectedOptionIndex == 0 ? "yes" : response.SelectedOptionIndex == 1 ? "no" : "maybe")}|{response.StudentName}";
                    }
                    else // Dynamic JSON survey
                    {
                        var votePayload = new
                        {
                            SurveyId = response.SurveyId,
                            StudentCode = response.StudentCode,
                            StudentName = response.StudentName,
                            SelectedOptionIndex = response.SelectedOptionIndex,
                            SelectedOptionText = response.SelectedOptionText
                        };
                        var voteMsg = new
                        {
                            Action = "SURVEY_VOTE",
                            Payload = votePayload
                        };
                        msgToSend = System.Text.Json.JsonSerializer.Serialize(voteMsg);
                    }

                    try
                    {
                        await client.SendAsync(msgToSend);
                        syncedResponses.Add(response);
                        Log.Information("[OfflineSync] Successfully synced survey response for survey {SurveyId}", response.SurveyId);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("[OfflineSync] Failed to sync survey response for survey {SurveyId}: {Err}", response.SurveyId, ex.Message);
                        break; // Stop sync if connection failed again
                    }
                }

                if (syncedResponses.Any())
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var response in syncedResponses)
                        {
                            response.IsSynced = 1;
                        }
                        try
                        {
                            app.Database.SaveChanges();
                            Log.Information("[OfflineSync] Batched SaveChanges complete for {Count} synced responses.", syncedResponses.Count);
                        }
                        catch (Exception dbEx)
                        {
                            Log.Error(dbEx, "[OfflineSync] Batched SaveChanges failed");
                        }
                    });
                }
            });
        }

        private string GetLocalSalt()
        {
            try
            {
                string machineId = Environment.MachineName;
                string userSid = System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value ?? "DefaultUser";
                return $"QASmartClass_Salt_2026_{machineId}_{userSid}";
            }
            catch { return "QASmartClass_Fallback_Salt_2026_Secure"; }
        }

        private void ScaleStar(TextBlock star, double scale)
        {
            if (star == null) return;
            if (star.RenderTransform is ScaleTransform st)
            {
                st.ScaleX = scale;
                st.ScaleY = scale;
            }
            else
            {
                var newScale = new ScaleTransform(scale, scale);
                star.RenderTransformOrigin = new Point(0.5, 0.5);
                star.RenderTransform = newScale;
            }
        }
    }
}