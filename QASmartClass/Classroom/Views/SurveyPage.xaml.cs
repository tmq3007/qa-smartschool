using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using QASmartClass.Data;
using QASmartClass.Classroom.Helpers;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class SurveyPage : Page
    {
        // ═══ Quick Poll ═══
        private int _yes, _no, _slow, _fast;

        // ═══ Survey Results ═══
        private readonly Dictionary<string, int> _voteCounts = new();
        private List<string> _currentOptions = new();
        private string _activeQuestion = "";
        private string _activeSurveyType = ""; // "custom", "satisfaction"
        private int _responseCount;
        private bool _listenersWired;

        // ═══ Countdown Timer ═══
        private DispatcherTimer? _countdownTimer;
        private int _remainingSeconds;

        public SurveyPage()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                WireSurveyResponseListeners();
                LoadClassList();
                LoadTemplates();
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  TAB SWITCHING
        // ═══════════════════════════════════════════════════════════

        private void Tab_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string tag)
            {
                // Reset all tabs
                tabQuickPoll.Background = Brushes.White; tabQuickPoll.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                tabCustom.Background = Brushes.White; tabCustom.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                tabSatisfaction.Background = Brushes.White; tabSatisfaction.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));

                // Highlight active tab
                border.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210));

                // Show/hide panels
                panelQuickPoll.Visibility = tag == "quick" ? Visibility.Visible : Visibility.Collapsed;
                panelCustom.Visibility = tag == "custom" ? Visibility.Visible : Visibility.Collapsed;
                panelSatisfaction.Visibility = tag == "satisfaction" ? Visibility.Visible : Visibility.Collapsed;

                // Show/hide target class selection panel
                panelTargetSelection.Visibility = tag == "quick" ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TARGET SELECTION LOGIC
        // ═══════════════════════════════════════════════════════════

        private void LoadClassList()
        {
            try
            {
                classCheckList.Children.Clear();
                // → ClassroomAppContext

                // Lấy danh sách ClassName duy nhất từ DB
                var students = ClassroomAppContext.Db?.Students?.ToList();
                var classNames = students?.Select(s => s.ClassName)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct().OrderBy(c => c).ToList();

                if (classNames != null && classNames.Count > 0)
                {
                    foreach (var cn in classNames)
                    {
                        int cnt = students!.Count(s => s.ClassName == cn);
                        var cb = new CheckBox
                        {
                            Content = $"{cn} ({cnt} HS)",
                            FontSize = 11,
                            IsChecked = true,
                            Margin = new Thickness(0, 0, 12, 6),
                            Tag = cn
                        };
                        classCheckList.Children.Add(cb);
                    }
                }
                else
                {
                    classCheckList.Children.Add(new TextBlock
                    {
                        Text = "Không tìm thấy lớp học nào.",
                        FontSize = 10,
                        Foreground = Brushes.Gray,
                        FontStyle = FontStyles.Italic
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Warning("LoadClassList error: {Err}", ex.Message);
            }
        }

        private void TargetChanged(object sender, RoutedEventArgs e)
        {
            if (panelClassPicker == null) return;
            panelClassPicker.Visibility = rbSelectClasses.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SelectAllClasses_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cb in classCheckList.Children.OfType<CheckBox>())
                cb.IsChecked = true;
        }

        private void DeselectAllClasses_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cb in classCheckList.Children.OfType<CheckBox>())
                cb.IsChecked = false;
        }

        private string GetTargetClasses()
        {
            if (rbAllStudents.IsChecked == true)
            {
                // → ClassroomAppContext
                if (ClassroomAppContext.ClassRoster?.ActiveRoster != null)
                {
                    return ClassroomAppContext.ClassRoster.ActiveRoster.ClassName;
                }
                return "ALL";
            }
            else
            {
                var selectedClasses = classCheckList.Children.OfType<CheckBox>()
                    .Where(cb => cb.IsChecked == true && cb.Tag is string)
                    .Select(cb => (string)cb.Tag!)
                    .ToList();
                return string.Join(",", selectedClasses);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  QUICK POLL (GV click thủ công)
        // ═══════════════════════════════════════════════════════════

        private void QuickPoll_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string tag)
            {
                switch (tag)
                {
                    case "yes":  _yes++;  txtYes.Text  = _yes.ToString();  break;
                    case "no":   _no++;   txtNo.Text   = _no.ToString();   break;
                    case "slow": _slow++; txtSlow.Text = _slow.ToString(); break;
                    case "fast": _fast++; txtFast.Text = _fast.ToString(); break;
                }
                SaveEventLog("POLL", "GV", $"Quick Poll: {tag} — Yes={_yes} No={_no} Slow={_slow} Fast={_fast}");
            }
        }

        private void ResetPoll_Click(object sender, RoutedEventArgs e)
        {
            _yes = _no = _slow = _fast = 0;
            txtYes.Text = txtNo.Text = txtSlow.Text = txtFast.Text = "0";
        }

        // ═══════════════════════════════════════════════════════════
        //  STAR RATING (GV tự đánh giá)
        // ═══════════════════════════════════════════════════════════

        private void Star_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string tag && int.TryParse(tag, out int stars))
            {
                txtStarResult.Text = $"⭐ {stars}/5 — {stars switch { 5 => "Xuất sắc!", 4 => "Rất tốt!", 3 => "Tạm được", 2 => "Cần cải thiện", _ => "Khó hiểu" }}";
                SaveEventLog("RATING", "GV", $"Star rating: {stars}/5");

                // Update visual star colors
                gvStar1.Foreground = stars >= 1 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) : new SolidColorBrush(Color.FromRgb(223, 228, 234));
                gvStar2.Foreground = stars >= 2 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) : new SolidColorBrush(Color.FromRgb(223, 228, 234));
                gvStar3.Foreground = stars >= 3 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) : new SolidColorBrush(Color.FromRgb(223, 228, 234));
                gvStar4.Foreground = stars >= 4 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) : new SolidColorBrush(Color.FromRgb(223, 228, 234));
                gvStar5.Foreground = stars >= 5 ? new SolidColorBrush(Color.FromRgb(255, 193, 7)) : new SolidColorBrush(Color.FromRgb(223, 228, 234));
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  CUSTOM SURVEY — Gửi câu hỏi + phương án cho HS
        // ═══════════════════════════════════════════════════════════

        private async void SendCustomSurvey_Click(object sender, RoutedEventArgs e)
        {
            var question = txtQuestion.Text.Trim();
            if (string.IsNullOrWhiteSpace(question)) { ShowToast("⚠️ Vui lòng nhập câu hỏi!", "#E65100"); return; }

            // Collect non-empty options
            var options = new List<string>();
            if (!string.IsNullOrWhiteSpace(txtOptA.Text)) options.Add(txtOptA.Text.Trim());
            if (!string.IsNullOrWhiteSpace(txtOptB.Text)) options.Add(txtOptB.Text.Trim());
            if (!string.IsNullOrWhiteSpace(txtOptC.Text)) options.Add(txtOptC.Text.Trim());
            if (!string.IsNullOrWhiteSpace(txtOptD.Text)) options.Add(txtOptD.Text.Trim());

            if (options.Count < 2) { ShowToast("⚠️ Cần ít nhất 2 phương án trả lời!", "#E65100"); return; }

            // Validate classes checked if selecting classes
            if (rbSelectClasses.IsChecked == true)
            {
                var selectedClasses = classCheckList.Children.OfType<CheckBox>()
                    .Where(cb => cb.IsChecked == true && cb.Tag is string)
                    .Select(cb => (string)cb.Tag!)
                    .ToList();
                if (selectedClasses.Count == 0)
                {
                    ShowToast("⚠️ Vui lòng chọn ít nhất một lớp nhận!", "#E65100");
                    return;
                }
            }

            // Get time limit
            int timeSec = GetSelectedTime(cboTimeLimit);

            // Get target classes
            string targetClasses = GetTargetClasses();

            var surveyId = Guid.NewGuid().ToString();
            var optionsJson = System.Text.Json.JsonSerializer.Serialize(options);

            try
            {
                // → ClassroomAppContext
                if (ClassroomAppContext.Db != null)
                {
                    var newSurvey = new Survey
                    {
                        Id = surveyId,
                        Title = "Khảo sát tùy chỉnh",
                        Description = "",
                        SurveyType = "custom",
                        QuestionText = question,
                        OptionsJson = optionsJson,
                        TimeLimitSeconds = timeSec,
                        IsAnonymous = 1,
                        TargetClasses = targetClasses,
                        CreatedByTeacher = "GV",
                        CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    ClassroomAppContext.Db.Surveys.Add(newSurvey);
                    ClassroomAppContext.Db.SaveChanges();
                }
            }
            catch (Exception exDb) { Log.Warning("Save Survey to DB error: {Err}", exDb.Message); }

            var startPayload = new
            {
                SurveyId = surveyId,
                Title = "Khảo sát tùy chỉnh",
                SurveyType = "custom",
                QuestionText = question,
                Options = options,
                TimeLimitSeconds = timeSec,
                IsAnonymous = true,
                TargetClasses = targetClasses
            };
            var startMsg = new
            {
                Action = "SURVEY_START",
                Payload = startPayload
            };
            var command = System.Text.Json.JsonSerializer.Serialize(startMsg);

            await SendSurveyCommand(command, targetClasses, question, options, timeSec, "custom", surveyId);
        }

        private void LoadTemplates()
        {
            try
            {
                // → ClassroomAppContext
                if (ClassroomAppContext.Db == null) return;

                var templates = ClassroomAppContext.Db.SurveyTemplates.ToList();
                cboSavedTemplates.Items.Clear();

                var defaultItem = new ComboBoxItem
                {
                    Content = "-- Chọn câu hỏi mẫu --",
                    Tag = null
                };
                cboSavedTemplates.Items.Add(defaultItem);
                cboSavedTemplates.SelectedItem = defaultItem;

                foreach (var t in templates)
                {
                    cboSavedTemplates.Items.Add(new ComboBoxItem
                    {
                        Content = t.Title,
                        Tag = t
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Warning("LoadTemplates error: {Err}", ex.Message);
            }
        }

        private void SavedTemplates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboSavedTemplates.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is SurveyTemplate t)
            {
                txtQuestion.Text = t.QuestionText;
                
                txtOptA.Text = "";
                txtOptB.Text = "";
                txtOptC.Text = "";
                txtOptD.Text = "";

                try
                {
                    var options = System.Text.Json.JsonSerializer.Deserialize<List<string>>(t.OptionsJson);
                    if (options != null)
                    {
                        if (options.Count > 0) txtOptA.Text = options[0];
                        if (options.Count > 1) txtOptB.Text = options[1];
                        if (options.Count > 2) txtOptC.Text = options[2];
                        if (options.Count > 3) txtOptD.Text = options[3];
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Deserialize OptionsJson error: {Err}", ex.Message);
                }
            }
        }

        private void SaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            var question = txtQuestion.Text.Trim();
            if (string.IsNullOrWhiteSpace(question))
            {
                ShowToast("⚠️ Vui lòng nhập câu hỏi!", "#E65100");
                return;
            }

            var options = new List<string>();
            if (!string.IsNullOrWhiteSpace(txtOptA.Text)) options.Add(txtOptA.Text.Trim());
            if (!string.IsNullOrWhiteSpace(txtOptB.Text)) options.Add(txtOptB.Text.Trim());
            if (!string.IsNullOrWhiteSpace(txtOptC.Text)) options.Add(txtOptC.Text.Trim());
            if (!string.IsNullOrWhiteSpace(txtOptD.Text)) options.Add(txtOptD.Text.Trim());

            if (options.Count < 2)
            {
                ShowToast("⚠️ Cần ít nhất 2 phương án trả lời!", "#E65100");
                return;
            }

            try
            {
                // → ClassroomAppContext
                if (ClassroomAppContext.Db == null) return;

                var existing = ClassroomAppContext.Db.SurveyTemplates.FirstOrDefault(t => t.QuestionText == question);
                if (existing != null)
                {
                    var result = MessageBox.Show(
                        $"Câu hỏi này đã tồn tại trong danh sách mẫu với tên \"{existing.Title}\". Bạn có muốn ghi đè (cập nhật) không?",
                        "Xác nhận ghi đè mẫu",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        existing.OptionsJson = System.Text.Json.JsonSerializer.Serialize(options);
                        existing.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        ClassroomAppContext.Db.SaveChanges();
                        ShowToast("✅ Cập nhật mẫu câu hỏi thành công!", "#2E7D32");
                        LoadTemplates();
                    }
                    return;
                }

                string title = question.Length > 40 ? question.Substring(0, 37) + "..." : question;

                var newTemplate = new SurveyTemplate
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = title,
                    Description = "",
                    SurveyType = "custom",
                    QuestionText = question,
                    OptionsJson = System.Text.Json.JsonSerializer.Serialize(options),
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                ClassroomAppContext.Db.SurveyTemplates.Add(newTemplate);
                ClassroomAppContext.Db.SaveChanges();

                ShowToast("✅ Lưu câu hỏi mẫu thành công!", "#2E7D32");
                LoadTemplates();
            }
            catch (Exception ex)
            {
                Log.Warning("SaveTemplate error: {Err}", ex.Message);
                ShowToast("⚠️ Lỗi lưu mẫu câu hỏi!", "#C62828");
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  SATISFACTION SURVEY — Gửi đánh giá hài lòng
        // ═══════════════════════════════════════════════════════════

        private async void SendSatisfaction_Click(object sender, RoutedEventArgs e)
        {
            // Validate classes checked if selecting classes
            if (rbSelectClasses.IsChecked == true)
            {
                var selectedClasses = classCheckList.Children.OfType<CheckBox>()
                    .Where(cb => cb.IsChecked == true && cb.Tag is string)
                    .Select(cb => (string)cb.Tag!)
                    .ToList();
                if (selectedClasses.Count == 0)
                {
                    ShowToast("⚠️ Vui lòng chọn ít nhất một lớp nhận!", "#E65100");
                    return;
                }
            }

            int timeSec = GetSelectedTime(cboSatisfactionTime);
            string question = "Đánh giá mức độ hài lòng chung về buổi học hôm nay";
            var options = new List<string> { "⭐ 1 sao — Chưa hài lòng", "⭐⭐ 2 sao — Tạm được", "⭐⭐⭐ 3 sao — Bình thường", "⭐⭐⭐⭐ 4 sao — Hài lòng", "⭐⭐⭐⭐⭐ 5 sao — Rất hài lòng" };

            // Get target classes
            string targetClasses = GetTargetClasses();

            var surveyId = Guid.NewGuid().ToString();
            var optionsJson = System.Text.Json.JsonSerializer.Serialize(options);

            try
            {
                // → ClassroomAppContext
                if (ClassroomAppContext.Db != null)
                {
                    var newSurvey = new Survey
                    {
                        Id = surveyId,
                        Title = "Đánh giá mức độ hài lòng",
                        Description = "",
                        SurveyType = "satisfaction",
                        QuestionText = question,
                        OptionsJson = optionsJson,
                        TimeLimitSeconds = timeSec,
                        IsAnonymous = 1,
                        TargetClasses = targetClasses,
                        CreatedByTeacher = "GV",
                        CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    ClassroomAppContext.Db.Surveys.Add(newSurvey);
                    ClassroomAppContext.Db.SaveChanges();
                }
            }
            catch (Exception exDb) { Log.Warning("Save Survey to DB error: {Err}", exDb.Message); }

            var startPayload = new
            {
                SurveyId = surveyId,
                Title = "Đánh giá mức độ hài lòng",
                SurveyType = "satisfaction",
                QuestionText = question,
                Options = options,
                TimeLimitSeconds = timeSec,
                IsAnonymous = true,
                TargetClasses = targetClasses
            };
            var startMsg = new
            {
                Action = "SURVEY_START",
                Payload = startPayload
            };
            var command = System.Text.Json.JsonSerializer.Serialize(startMsg);

            await SendSurveyCommand(command, targetClasses, question, options, timeSec, "satisfaction", surveyId);
        }

        // ═══════════════════════════════════════════════════════════
        //  SHARED: Send survey command + setup results panel
        // ═══════════════════════════════════════════════════════════

        private string _activeSurveyId = "";
        private int _studentStarCount = 0;
        private int _studentStarSum = 0;

        private async System.Threading.Tasks.Task SendSurveyCommand(string command, string targetClasses, string question, List<string> options, int timeSec, string surveyType, string surveyId)
        {
            try
            {
                // → ClassroomAppContext
                _activeSurveyId = surveyId;

                // Set shared state
                QASmartTouch.App.AssessmentState.ActiveSurveyQuestion = command; // Full JSON string
                QASmartTouch.App.AssessmentState.ActiveSurveyTime = DateTime.Now;
                QASmartTouch.App.AssessmentState.SurveyAnswered = false;
                QASmartTouch.App.AssessmentState.ActivePollId = surveyId;

                // Save to DB
                SaveEventLog("SURVEY", "GV", command);

                // Setup results panel
                _activeQuestion = question;
                _activeSurveyType = surveyType;
                _currentOptions = options;
                _responseCount = 0;
                _voteCounts.Clear();
                for (int i = 0; i < options.Count; i++)
                    _voteCounts[$"opt{i}"] = 0;

                // Build UI
                surveyResultsPanel.Visibility = Visibility.Visible;
                txtActiveQuestion.Text = question;
                txtTotalVotes.Text = "0";

                // Setup student star ratings summary
                panelStudentStarRating.Visibility = surveyType == "satisfaction" ? Visibility.Visible : Visibility.Collapsed;
                _studentStarCount = 0;
                _studentStarSum = 0;
                txtStudentStarAvg.Text = "0.0 / 5.0";
                txtStudentStarCount.Text = "(0 lượt đánh giá)";

                BuildResultBars(options);
                surveyResponseLog.Children.Clear();
                surveyResponseLog.Children.Add(new TextBlock { Text = "⏳ Đang chờ phản hồi từ học sinh...", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(189, 189, 189)), FontStyle = FontStyles.Italic, HorizontalAlignment = HorizontalAlignment.Center });

                // Start countdown
                StartCountdown(timeSec);

                // Toast
                ShowToast($"✅ Đã gửi khảo sát: \"{question}\"", "#2E7D32");

                // Send command
                if (ClassroomAppContext.Network?.IsBroadcasting == true)
                    await ClassroomAppContext.Network.SendCommandAsync(command);
                else
                    Dispatcher.BeginInvoke(new Action(() => ClassroomAppContext.DispatchCommand(command)), DispatcherPriority.Background);

                Log.Information("Survey sent (JSON): {Type} — {Q} — Target: {Target} — {N} options — {T}s", surveyType, question, targetClasses, options.Count, timeSec);
            }
            catch (Exception ex) { Log.Warning("SendSurveyCommand error: {Err}", ex.Message); }
        }

        // ═══════════════════════════════════════════════════════════
        //  DYNAMIC RESULT BARS
        // ═══════════════════════════════════════════════════════════

        private readonly string[] _barColors = { "#4CAF50", "#1976D2", "#FF9800", "#EF5350", "#9C27B0" };
        private readonly string[] _barBgColors = { "#E8F5E9", "#E3F2FD", "#FFF3E0", "#FFEBEE", "#F3E5F5" };
        private readonly string[] _optLabels = { "A", "B", "C", "D", "E" };

        private void BuildResultBars(List<string> options)
        {
            resultBarsPanel.Children.Clear();
            for (int i = 0; i < options.Count; i++)
            {
                var color = (Color)ColorConverter.ConvertFromString(_barColors[i % _barColors.Length]);
                var bgColor = (Color)ColorConverter.ConvertFromString(_barBgColors[i % _barBgColors.Length]);

                var dock = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };

                // Label
                var labelBorder = new Border { Background = new SolidColorBrush(bgColor), CornerRadius = new CornerRadius(6), Width = 28, Height = 28, Margin = new Thickness(0, 0, 8, 0) };
                labelBorder.Child = new TextBlock { Text = _optLabels[i % _optLabels.Length], FontSize = 12, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(color), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                dock.Children.Add(labelBorder);

                // Option text
                var optText = new TextBlock { Text = options[i], FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)), Width = 180, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
                dock.Children.Add(optText);

                // Count
                var countText = new TextBlock { Text = "0", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(color), Width = 30, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
                countText.Tag = $"count_{i}";
                DockPanel.SetDock(countText, Dock.Right);
                dock.Children.Add(countText);

                // Percent
                var pctText = new TextBlock { Text = "0%", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)), Width = 45, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
                pctText.Tag = $"pct_{i}";
                DockPanel.SetDock(pctText, Dock.Right);
                dock.Children.Add(pctText);

                // Responsive Bar using Grid
                var gridBar = new Grid { Margin = new Thickness(6, 0, 0, 0), Height = 20 };
                gridBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                gridBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(98, GridUnitType.Star) });

                var barFill = new Border { CornerRadius = new CornerRadius(5), Background = new SolidColorBrush(color) };
                barFill.Tag = $"fill_{i}";
                Grid.SetColumn(barFill, 0);
                gridBar.Children.Add(barFill);

                var barEmpty = new Border { CornerRadius = new CornerRadius(5), Background = new SolidColorBrush(bgColor) };
                Grid.SetColumn(barEmpty, 1);
                gridBar.Children.Add(barEmpty);

                dock.Children.Add(gridBar);

                resultBarsPanel.Children.Add(dock);
            }
        }

        private void UpdateResultBars()
        {
            int total = _voteCounts.Values.Sum();
            txtTotalVotes.Text = total.ToString();

            for (int i = 0; i < _currentOptions.Count; i++)
            {
                string key = $"opt{i}";
                int count = _voteCounts.ContainsKey(key) ? _voteCounts[key] : 0;
                double pct = total > 0 ? (double)count / total * 100 : 0;

                // Find UI elements by Tag
                foreach (var child in resultBarsPanel.Children.OfType<DockPanel>())
                {
                    foreach (var el in child.Children.OfType<TextBlock>())
                    {
                        if (el.Tag is string tag)
                        {
                            if (tag == $"count_{i}") el.Text = count.ToString();
                            if (tag == $"pct_{i}") el.Text = $"{pct:F0}%";
                        }
                    }
                    foreach (var el in child.Children.OfType<Grid>())
                    {
                        var fillBorder = el.Children.OfType<Border>().FirstOrDefault(b => b.Tag is string t && t == $"fill_{i}");
                        if (fillBorder != null)
                        {
                            double fillPct = Math.Max(2, pct);
                            double emptyPct = Math.Max(2, 100 - pct);
                            el.ColumnDefinitions[0].Width = new GridLength(fillPct, GridUnitType.Star);
                            el.ColumnDefinitions[1].Width = new GridLength(emptyPct, GridUnitType.Star);
                        }
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  COUNTDOWN TIMER
        // ═══════════════════════════════════════════════════════════

        private void StartCountdown(int seconds)
        {
            _countdownTimer?.Stop();
            if (seconds <= 0) { timerBadge.Visibility = Visibility.Collapsed; return; }

            _remainingSeconds = seconds;
            timerBadge.Visibility = Visibility.Visible;
            txtCountdown.Text = FormatTime(seconds);

            _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _countdownTimer.Tick += (s, ev) =>
            {
                _remainingSeconds--;
                txtCountdown.Text = FormatTime(_remainingSeconds);

                if (_remainingSeconds <= 10)
                    txtCountdown.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47)); // Red

                if (_remainingSeconds <= 0)
                {
                    _countdownTimer.Stop();
                    txtCountdown.Text = "⏰ Hết giờ!";
                    ShowToast("⏰ Hết thời gian khảo sát!", "#C62828");

                    // Send end command
                    try
                    {
                        // → ClassroomAppContext
                        var endCmd = "CMD|SURVEY_END|0";
                        if (ClassroomAppContext.Network?.IsBroadcasting == true)
                            _ = ClassroomAppContext.Network.SendCommandAsync(endCmd);
                        else
                            ClassroomAppContext.DispatchCommand(endCmd);
                        QASmartTouch.App.AssessmentState.ActiveSurveyQuestion = ""; // Clear shared state
                    }
                    catch { }
                }
            };
            _countdownTimer.Start();
        }

        private string FormatTime(int seconds) => $"{seconds / 60:D2}:{seconds % 60:D2}";

        // ═══════════════════════════════════════════════════════════
        //  RESPONSE LISTENERS (from students)
        // ═══════════════════════════════════════════════════════════

        private void WireSurveyResponseListeners()
        {
            if (_listenersWired) return;
            _listenersWired = true;

            try
            {
                // → ClassroomAppContext

                // Local command bus
                ((QASmartTouch.App)System.Windows.Application.Current).LocalCommandReceived += (s, cmd) =>
                {
                    if (cmd.StartsWith("CMD|SURVEY_RESPONSE"))
                    {
                        var msgPart = cmd.Substring(4); // Extract SURVEY_RESPONSE|...
                        Dispatcher.Invoke(() => ProcessNetworkMessage(msgPart));
                    }
                    else if (cmd.Trim().StartsWith("{"))
                    {
                        Dispatcher.Invoke(() => ProcessNetworkMessage(cmd));
                    }
                };

                // Network
                if (ClassroomAppContext.Network != null)
                {
                    ClassroomAppContext.Network.MessageReceived += (s, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Message))
                        {
                            Dispatcher.Invoke(() => ProcessNetworkMessage(args.Message));
                        }
                    };
                }

                Log.Information("SurveyPage: response listeners wired (once)");
            }
            catch (Exception ex) { Log.Warning("WireSurveyResponseListeners error: {Err}", ex.Message); }
        }

        private void ProcessNetworkMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            string trimmed = message.Trim();

            try
            {
                if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(trimmed);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("Action", out var actProp) && actProp.GetString() == "SURVEY_VOTE")
                    {
                        if (root.TryGetProperty("Payload", out var payload))
                        {
                            int selectedIdx = payload.TryGetProperty("SelectedOptionIndex", out var si) ? si.GetInt32() : 0;
                            string studentName = payload.TryGetProperty("StudentName", out var sn) ? sn.GetString() ?? "Học sinh" : "Học sinh";
                            HandleStudentVote($"opt{selectedIdx}", studentName);
                        }
                    }
                    else
                    {
                        string voteKey = root.TryGetProperty("voteKey", out var vProp) ? vProp.GetString() ?? "" : "";
                        string studentName = root.TryGetProperty("studentName", out var nProp) ? nProp.GetString() ?? "Học sinh" : "Học sinh";
                        if (!string.IsNullOrEmpty(voteKey))
                        {
                            HandleStudentVote(voteKey, studentName);
                        }
                    }
                }
                else if (trimmed.StartsWith("SURVEY_RESPONSE|"))
                {
                    var parts = trimmed.Split('|');
                    if (parts.Length >= 3)
                    {
                        string voteKey = parts[1];
                        string studentName = parts[2];
                        HandleStudentVote(voteKey, studentName);
                    }
                }
                else if (trimmed.StartsWith("SURVEY_STAR|"))
                {
                    var parts = trimmed.Split('|');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int stars))
                    {
                        HandleStudentStarRating(stars);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("ProcessNetworkMessage error: {Err}. Msg: {Msg}", ex.Message, message);
            }
        }

        private void HandleStudentStarRating(int stars)
        {
            _studentStarCount++;
            _studentStarSum += stars;
            double avg = (double)_studentStarSum / _studentStarCount;
            txtStudentStarAvg.Text = $"{avg:F1} / 5.0";
            txtStudentStarCount.Text = $"({_studentStarCount} lượt đánh giá)";
        }

        private void HandleStudentVote(string voteKey, string studentName)
        {
            if (_voteCounts.ContainsKey(voteKey))
                _voteCounts[voteKey]++;
            _responseCount++;

            // Update bars
            UpdateResultBars();

            // Add log entry
            string optionLabel = "";
            if (voteKey.StartsWith("opt") && int.TryParse(voteKey.Substring(3), out int idx) && idx < _currentOptions.Count)
                optionLabel = _currentOptions[idx];

            if (_responseCount == 1)
                surveyResponseLog.Children.Clear();

            var logColors = new[] { Color.FromRgb(232, 245, 233), Color.FromRgb(227, 242, 253), Color.FromRgb(255, 243, 224), Color.FromRgb(255, 235, 238), Color.FromRgb(243, 229, 245) };
            int colorIdx = 0;
            if (voteKey.StartsWith("opt") && int.TryParse(voteKey.Substring(3), out int ci)) colorIdx = ci;

            var entry = new Border
            {
                Background = new SolidColorBrush(logColors[colorIdx % logColors.Length]),
                CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(0, 0, 0, 3)
            };
            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            stack.Children.Add(new TextBlock { Text = $"📩 {studentName}", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)), Margin = new Thickness(0, 0, 6, 0) });
            stack.Children.Add(new TextBlock { Text = $"→ {optionLabel}", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)) });
            stack.Children.Add(new TextBlock { Text = $" — {DateTime.Now:HH:mm:ss}", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(189, 189, 189)) });
            entry.Child = stack;
            surveyResponseLog.Children.Insert(0, entry);

            Log.Information("Student vote: {Key} from {Name}", voteKey, studentName);
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════

        private int GetSelectedTime(ComboBox cbo)
        {
            if (cbo.SelectedItem is ComboBoxItem item && item.Tag is string tag && int.TryParse(tag, out int sec))
                return sec;
            return 0;
        }

        private void SaveEventLog(string type, string actor, string details)
        {
            try
            {
                // → ClassroomAppContext
                if (ClassroomAppContext.Db == null) return;
                ClassroomAppContext.Db.EventLogs.Add(new EventLog { EventType = type, Actor = actor, Details = details, Timestamp = DateTime.Now });
                ClassroomAppContext.Db.SaveChanges();
            }
            catch (Exception ex) { Log.Warning("SaveEventLog error: {Err}", ex.Message); }
        }

        private void ShowToast(string message, string colorHex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorHex);
                var toast = new Border
                {
                    Background = new SolidColorBrush(color), CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16, 10, 16, 10), Margin = new Thickness(0, 8, 0, 0)
                };
                toast.Child = new TextBlock { Text = message, FontSize = 12, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };

                if (surveyResultsPanel.Parent is StackPanel mainStack)
                {
                    int idx = mainStack.Children.IndexOf(surveyResultsPanel);
                    if (idx < 0) idx = mainStack.Children.Count;
                    mainStack.Children.Insert(idx, toast);
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                    timer.Tick += (s, ev) => { timer.Stop(); mainStack.Children.Remove(toast); };
                    timer.Start();
                }
            }
            catch { }
        }
    }
}





