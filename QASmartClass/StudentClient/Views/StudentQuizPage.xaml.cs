using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using QASmartTouch.Services;
using Serilog;
using System.Runtime.InteropServices;

namespace QASmartClass.StudentClient.Views
{
    public partial class StudentQuizPage : Page
    {
        private DispatcherTimer? _timer;
        private int _remainingSeconds = 0;
        private int _quizId = 0;
        private bool _isRestoringDraft = false;
        private System.Windows.Media.Animation.Storyboard? _streakGlowStoryboard;
        private bool _isReviewMode = false;

        // Các trường mới cho Phase 5
        private int _cheatingCount = 0;
        private TimeSpan _timeOffset = TimeSpan.Zero;
        private bool _isTimeSynced = false;
        private System.Speech.Synthesis.SpeechSynthesizer? _synthesizer;
        private readonly object _speechLock = new object();
        private int _ttsVolume = 80;
        private double _globalFontSizeMultiplier = 1.0;
        private bool _isTimerBlinking = false;

        public StudentQuizPage()
        {
            InitializeComponent();
            PreviewKeyDown += StudentQuizPage_PreviewKeyDown;
            Loaded += (s, e) => {
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        CleanupOldDrafts();
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Asynchronous CleanupOldDrafts failed: {Err}", ex.Message);
                    }

                    try
                    {
                        CleanupFailedSubmissions();
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Asynchronous CleanupFailedSubmissions failed: {Err}", ex.Message);
                    }
                });

                System.Threading.Tasks.Task.Run(async () =>
                {
                    _timeOffset = await FetchNtpOffsetAsync();
                });

                CheckForQuiz();
                
                // Đăng ký sự kiện đồng bộ hàng đợi ngoại tuyến khi mạng LAN kết nối lại
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    if (app?.StudentNetwork != null)
                    {
                        app.StudentNetwork.Connected += StudentNetwork_Connected;
                        if (app.StudentNetwork.IsConnected)
                        {
                            _ = SyncOfflineSubmissionsAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Failed to initialize network sync events: {Err}", ex.Message);
                }
            };

            Unloaded += (s, e) => {
                try
                {
                    UnregisterAntiCheating(); // Hủy giám sát Alt+Tab để tránh rò rỉ bộ nhớ
                    var app = (QASmartTouch.App)Application.Current;
                    if (app?.StudentNetwork != null)
                    {
                        app.StudentNetwork.Connected -= StudentNetwork_Connected;
                    }

                    lock (_speechLock)
                    {
                        if (_synthesizer != null)
                        {
                            _synthesizer.Dispose();
                            _synthesizer = null;
                        }
                    }
                }
                catch { }
            };
        }

        private void StudentNetwork_Connected(object? sender, EventArgs e)
        {
            _ = SyncOfflineSubmissionsAsync();
        }

        private void CleanupOldDrafts()
        {
            try
            {
                string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QASmartClass");
                if (System.IO.Directory.Exists(dir))
                {
                    // 1. Dọn dẹp nháp thi cũ hơn 7 ngày
                    var files = System.IO.Directory.GetFiles(dir, "quiz_draft_*.json");
                    foreach (var file in files)
                    {
                        var fi = new System.IO.FileInfo(file);
                        // Bỏ qua bản nháp của kỳ thi hiện hành
                        if (fi.Name.StartsWith($"quiz_draft_{_quizId}_", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        if (DateTime.Now - fi.LastWriteTime > TimeSpan.FromDays(7))
                        {
                            System.IO.File.Delete(file);
                            Log.Information("Auto-cleanup: Deleted old quiz draft {FileName}", fi.Name);
                        }
                    }

                    // 2. Giới hạn dung lượng tối đa 100MB (Phase 4)
                    files = System.IO.Directory.GetFiles(dir, "quiz_draft_*.json");
                    long totalSize = files.Select(f => new System.IO.FileInfo(f).Length).Sum();
                    long maxSizeBytes = 100 * 1024 * 1024; // 100MB

                    if (totalSize > maxSizeBytes)
                    {
                        Log.Information("Draft directory size ({Size} bytes) exceeds 100MB limit. Cleaning up oldest drafts...", totalSize);
                        var sortedFiles = files.Select(f => new System.IO.FileInfo(f))
                                               .Where(f => !f.Name.StartsWith($"quiz_draft_{_quizId}_", StringComparison.OrdinalIgnoreCase))
                                               .OrderBy(f => f.LastWriteTime)
                                               .ToList();

                        long currentSize = totalSize;
                        long targetSize = 80 * 1024 * 1024; // Dọn dẹp về mức 80MB
                        for (int i = 0; i < sortedFiles.Count && currentSize > targetSize; i++)
                        {
                            long fileSize = sortedFiles[i].Length;
                            sortedFiles[i].Delete();
                            currentSize -= fileSize;
                            Log.Information("Deleted old draft file to save disk space: {Name} ({Bytes} bytes)", sortedFiles[i].Name, fileSize);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Auto-cleanup drafts error: {Err}", ex.Message);
            }
        }

        private static bool ContainsLatex(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.Contains("$") || text.Contains("\\(") || text.Contains("\\[") || text.Contains("\\begin");
        }

        internal void CheckForQuiz()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.Database == null) return;

                string className = app.StudentNetwork?.ClassName ?? "";
                var quiz = (from q in app.Database.Quizzes
                            join l in app.Database.Lessons on q.LessonId equals l.Id
                            where string.IsNullOrEmpty(className) || l.ClassName == className || l.Grade == className
                            orderby q.CreatedAt descending
                            select q).FirstOrDefault();

                if (quiz != null)
                {
                    // Resolve student identity via StudentIdentityService
                    var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(app.Database);
                    var (currentId, _, _) = identityService.GetCurrentStudent();
                    int studentId = currentId;

                    var existingResult = app.Database.QuizResults
                        .FirstOrDefault(r => r.QuizId == quiz.Id && r.StudentId == studentId);

                    if (existingResult != null)
                    {
                        LoadQuizForReview(quiz, existingResult);
                    }
                    else
                    {
                        LoadQuiz(quiz);
                    }
                }
            }
            catch (Exception ex) { Log.Warning("Quiz check error: {Err}", ex.Message); }
        }

        private void LoadQuizForReview(Data.Quiz quiz, Data.QuizResult result)
        {
            _isReviewMode = true;
            _quizId = quiz.Id;
            var app = (QASmartTouch.App)Application.Current;
            if (app?.Database == null) return;

            var questions = app.Database.Questions
                .Where(q => q.QuizId == quiz.Id)
                .OrderBy(q => q.SortOrder)
                .ToList();

            txtQuizTitle.Text = quiz.Title;
            if (result.Score != -1)
            {
                txtQuizInfo.Text = $"✅ Bạn đã nộp bài thi này! Điểm số: {result.Score}/100";
                txtQuizInfo.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            }
            else
            {
                txtQuizInfo.Text = "⌛ Bài làm của bạn đang chờ chấm điểm từ GV...";
                txtQuizInfo.Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210));
            }

            emptyState.Visibility = Visibility.Collapsed;
            quizScroll.Visibility = Visibility.Visible;
            btnSubmitQuiz.Visibility = Visibility.Visible;
            btnSubmitQuiz.IsEnabled = false;
            if (result.Score != -1)
            {
                btnSubmitQuiz.Content = $"✅ Đã nộp ({result.Score}đ)";
            }
            else
            {
                var sNet = app.StudentNetwork;
                if (sNet != null && sNet.IsConnected)
                {
                    btnSubmitQuiz.Content = "⌛ Đang chấm điểm...";
                }
                else
                {
                    btnSubmitQuiz.Content = "⚠️ Chờ kết nối";
                }
            }
            timerBorder.Visibility = Visibility.Collapsed;

            // Xây dựng câu hỏi
            questionsPanel.Children.Clear();
            for (int i = 0; i < questions.Count; i++)
            {
                questionsPanel.Children.Add(BuildQuestionCard(i + 1, questions[i]));
            }

            // Điền lại các câu trả lời học sinh đã chọn
            try
            {
                var answers = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(result.AnswersJson ?? "{}");
                if (answers != null)
                {
                    // Điền RadioButton
                    var rbs = FindVisualChildren<RadioButton>(questionsPanel).ToList();
                    foreach (var rb in rbs)
                    {
                        if (string.IsNullOrEmpty(rb.GroupName) || !rb.GroupName.StartsWith("Q") || rb.GroupName.Length < 2)
                        {
                            continue;
                        }
                        string qIdStr = rb.GroupName.Substring(1);
                        if (answers.TryGetValue(qIdStr, out var selectedAns) && rb.Tag?.ToString() == selectedAns)
                        {
                            rb.IsChecked = true;
                        }
                    }

                    // Điền TextBox
                    var tbs = FindVisualChildren<TextBox>(questionsPanel).ToList();
                    foreach (var tb in tbs)
                    {
                        if (tb.Tag != null && tb.Tag.ToString()!.StartsWith("Answer_"))
                        {
                            string qIdStr = tb.Tag.ToString()!.Substring("Answer_".Length);
                            if (answers.TryGetValue(qIdStr, out var typedAns))
                            {
                                tb.Text = typedAns;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Log.Warning("Restore answers error: {Err}", ex.Message); }

            // Hiển thị kết quả đánh giá chấm điểm trực quan
            if (result.Score != -1)
            {
                ShowQuizResultsReview(questions);
            }
        }

        private void LoadQuiz(Data.Quiz quiz)
        {
            _quizId = quiz.Id;

            // Explicitly load questions (avoid lazy loading issues)
            var app = (QASmartTouch.App)Application.Current;
            if (app?.Database == null) return;

            var questions = app.Database.Questions
                .Where(q => q.QuizId == quiz.Id)
                .OrderBy(q => q.SortOrder)
                .ToList();

            txtQuizTitle.Text = quiz.Title;
            txtQuizInfo.Text = $"{quiz.QuizType}  •  {questions.Count} câu  •  Tạo lúc {quiz.CreatedAt:HH:mm}";

            // Show UI
            emptyState.Visibility = Visibility.Collapsed;
            quizScroll.Visibility = Visibility.Visible;
            btnSubmitQuiz.Visibility = Visibility.Visible;
            timerBorder.Visibility = Visibility.Visible;

            // Khởi tạo các trạng thái đếm gian lận và font chữ về mặc định (Phase 5)
            _cheatingCount = 0;
            _globalFontSizeMultiplier = 1.0;
            _isTimerBlinking = false;
            timerBorder.BeginAnimation(UIElement.OpacityProperty, null);
            timerBorder.Opacity = 1.0;
            txtTimer.FontSize = 16;
            txtTimer.FontWeight = FontWeights.Bold;
            txtTimer.Foreground = (Brush)FindResource("PrimaryBrush");

            // Timer
            _remainingSeconds = quiz.TimeLimitSeconds;
            txtTimer.Text = FormatTime(_remainingSeconds);
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Build questions UI
            questionsPanel.Children.Clear();
            for (int i = 0; i < questions.Count; i++)
            {
                questionsPanel.Children.Add(BuildQuestionCard(i + 1, questions[i]));
            }

            // Restore draft cache if exists
            RestoreDraft(quiz.Id);

            // Đăng ký giám sát Alt+Tab
            RegisterAntiCheating();

            Log.Information("Quiz loaded: {Title}, {Count} questions", quiz.Title, questions.Count);
        }

        private Border BuildQuestionCard(int number, Data.Question question)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                Tag = $"QuestionCard_{question.Id}"
            };

            var stack = new StackPanel();

            // Question header
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
            var badge = new Border
            {
                Width = 32, Height = 32, CornerRadius = new CornerRadius(16),
                Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                Child = new TextBlock
                {
                    Text = number.ToString(), FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            header.Children.Add(badge);

            var diffBadge = new Border
            {
                Background = question.Difficulty == "Easy" ? new SolidColorBrush(Color.FromRgb(232, 245, 233)) :
                             question.Difficulty == "Hard" ? new SolidColorBrush(Color.FromRgb(255, 235, 238)) :
                             new SolidColorBrush(Color.FromRgb(255, 243, 224)),
                CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 3, 8, 3),
                HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center
            };
            diffBadge.Child = new TextBlock
            {
                Text = question.Difficulty == "Easy" ? "Dễ" : question.Difficulty == "Hard" ? "Khó" : "TB",
                FontSize = 10, Foreground = question.Difficulty == "Easy" ? new SolidColorBrush(Color.FromRgb(46, 125, 50)) :
                                              question.Difficulty == "Hard" ? new SolidColorBrush(Color.FromRgb(198, 40, 40)) :
                                              new SolidColorBrush(Color.FromRgb(230, 81, 0))
            };
            DockPanel.SetDock(diffBadge, Dock.Right);
            header.Children.Add(diffBadge);

            var qText = RenderLatexToTextBlock(question.Content, 14);
            qText.Margin = new Thickness(10, 0, 0, 0);
            header.Children.Add(qText);

            // Grid container cho Loa và Slider Volume (Phase 6)
            var ttsGrid = new Grid { Margin = new Thickness(6, 0, 0, 0) };
            ttsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ttsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var btnSpeak = new Button
            {
                Content = "🔊",
                Width = 28, Height = 28, FontSize = 12,
                Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Đọc câu hỏi bằng giọng nói (TTS)"
            };
            Grid.SetColumn(btnSpeak, 0);
            ttsGrid.Children.Add(btnSpeak);

            var sliderContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(4, 2, 4, 2),
                Margin = new Thickness(4, 0, 0, 0),
                Visibility = Visibility.Collapsed,
                VerticalAlignment = VerticalAlignment.Center
            };

            var volSlider = new Slider
            {
                Width = 60,
                Minimum = 0,
                Maximum = 100,
                Value = _ttsVolume,
                TickFrequency = 10,
                IsSnapToTickEnabled = false,
                VerticalAlignment = VerticalAlignment.Center
            };
            sliderContainer.Child = volSlider;
            Grid.SetColumn(sliderContainer, 1);
            ttsGrid.Children.Add(sliderContainer);

            // Xử lý hover ẩn hiện cả Grid
            ttsGrid.MouseEnter += (sE, eE) => sliderContainer.Visibility = Visibility.Visible;
            ttsGrid.MouseLeave += (sE, eE) => sliderContainer.Visibility = Visibility.Collapsed;

            volSlider.ValueChanged += (sV, eV) =>
            {
                _ttsVolume = (int)volSlider.Value;
            };

            btnSpeak.Click += (sS, eS) => SpeakText(question.Content);
            header.Children.Add(ttsGrid);

            stack.Children.Add(header);

            // Image Url (if available)
            if (!string.IsNullOrEmpty(question.ImageUrl))
            {
                var questionImageControl = new Image
                {
                    Stretch = Stretch.Uniform,
                    MaxWidth = 600,
                    Margin = new Thickness(42, 8, 0, 12),
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                // Trình xử lý lỗi fallback khi tải ảnh thất bại (mất mạng LAN, link 404)
                questionImageControl.ImageFailed += (s, e) =>
                {
                    var errorPanel = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(255, 235, 235)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(12),
                        Margin = new Thickness(42, 8, 0, 12),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    errorPanel.Child = new TextBlock
                    {
                        Text = "⚠️ Không thể tải ảnh minh họa - Vui lòng kiểm tra kết nối LAN hoặc báo giáo viên.",
                        Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                        FontSize = 12,
                        FontWeight = FontWeights.Medium
                    };
                    
                    var parent = questionImageControl.Parent as Panel;
                    if (parent != null)
                    {
                        int idx = parent.Children.IndexOf(questionImageControl);
                        if (idx >= 0)
                        {
                            parent.Children.RemoveAt(idx);
                            parent.Children.Insert(idx, errorPanel);
                        }
                    }
                };

                try
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(question.ImageUrl, UriKind.RelativeOrAbsolute);
                    bitmap.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.DelayCreation;
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    questionImageControl.Source = bitmap;

                    // Phóng to ảnh khi click chuột (Phase 4 & Phase 5)
                    questionImageControl.Cursor = System.Windows.Input.Cursors.Hand;
                    questionImageControl.MouseDown += (s, ev) =>
                    {
                        if (ev.ChangedButton == System.Windows.Input.MouseButton.Left)
                        {
                            try
                            {
                                var previewWin = new Window
                                {
                                    Title = "Phóng to ảnh minh họa câu hỏi",
                                    Width = 900,
                                    Height = 700,
                                    WindowStyle = WindowStyle.None,
                                    AllowsTransparency = true,
                                    Background = Brushes.Transparent,
                                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                                    Owner = Window.GetWindow(this)
                                };

                                previewWin.PreviewKeyDown += (sK, eK) =>
                                {
                                    if (eK.Key == System.Windows.Input.Key.Escape)
                                    {
                                        previewWin.Close();
                                    }
                                };

                                // Main Border bo tròn 12px
                                var mainBorder = new Border
                                {
                                    Background = new SolidColorBrush(Color.FromRgb(245, 247, 250)),
                                    BorderBrush = new SolidColorBrush(Color.FromRgb(218, 224, 233)),
                                    BorderThickness = new Thickness(2),
                                    CornerRadius = new CornerRadius(12),
                                    Padding = new Thickness(0)
                                };

                                // Grid chia làm 2 phần: Header (chứa Title, nút đóng, nút zoom) và Content (chứa ảnh zoom)
                                var mainGrid = new Grid();
                                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) }); // Header
                                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content

                                // Header Border
                                var headerBorder = new Border
                                {
                                    Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)), // Màu xanh dương chuyên nghiệp
                                    CornerRadius = new CornerRadius(10, 10, 0, 0), // Bo tròn phía trên
                                    Padding = new Thickness(15, 0, 15, 0)
                                };

                                var headerGrid = new Grid();
                                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Title
                                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) }); // Control buttons

                                // Title Text
                                var titleText = new TextBlock
                                {
                                    Text = "Phóng to ảnh minh họa câu hỏi",
                                    Foreground = Brushes.White,
                                    FontSize = 15,
                                    FontWeight = FontWeights.Bold,
                                    VerticalAlignment = VerticalAlignment.Center
                                };
                                Grid.SetColumn(titleText, 0);
                                headerGrid.Children.Add(titleText);

                                // Toolbar Panel chứa các nút Zoom In, Zoom Out, Close
                                var toolbarPanel = new StackPanel
                                {
                                    Orientation = Orientation.Horizontal,
                                    HorizontalAlignment = HorizontalAlignment.Right,
                                    VerticalAlignment = VerticalAlignment.Center
                                };

                                var scaleTransform = new ScaleTransform(1.0, 1.0);

                                var btnZoomIn = new Button
                                {
                                    Content = "🔍+ Phóng to",
                                    Width = 90,
                                    Height = 30,
                                    Margin = new Thickness(0, 0, 8, 0),
                                    Background = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                                    Foreground = Brushes.White,
                                    BorderThickness = new Thickness(0),
                                    FontWeight = FontWeights.SemiBold,
                                    Cursor = System.Windows.Input.Cursors.Hand
                                };
                                btnZoomIn.MouseEnter += (s2, e2) => btnZoomIn.Background = new SolidColorBrush(Color.FromRgb(13, 71, 161));
                                btnZoomIn.MouseLeave += (s2, e2) => btnZoomIn.Background = new SolidColorBrush(Color.FromRgb(21, 101, 192));

                                var btnZoomOut = new Button
                                {
                                    Content = "🔍- Thu nhỏ",
                                    Width = 90,
                                    Height = 30,
                                    Margin = new Thickness(0, 0, 8, 0),
                                    Background = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                                    Foreground = Brushes.White,
                                    BorderThickness = new Thickness(0),
                                    FontWeight = FontWeights.SemiBold,
                                    Cursor = System.Windows.Input.Cursors.Hand
                                };
                                btnZoomOut.MouseEnter += (s2, e2) => btnZoomOut.Background = new SolidColorBrush(Color.FromRgb(13, 71, 161));
                                btnZoomOut.MouseLeave += (s2, e2) => btnZoomOut.Background = new SolidColorBrush(Color.FromRgb(21, 101, 192));

                                var btnClose = new Button
                                {
                                    Content = "❌ Đóng",
                                    Width = 70,
                                    Height = 30,
                                    Background = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                                    Foreground = Brushes.White,
                                    BorderThickness = new Thickness(0),
                                    FontWeight = FontWeights.Bold,
                                    Cursor = System.Windows.Input.Cursors.Hand
                                };
                                btnClose.MouseEnter += (s2, e2) => btnClose.Background = new SolidColorBrush(Color.FromRgb(183, 28, 28));
                                btnClose.MouseLeave += (s2, e2) => btnClose.Background = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                                btnClose.Click += (s2, e2) => previewWin.Close();

                                toolbarPanel.Children.Add(btnZoomIn);
                                toolbarPanel.Children.Add(btnZoomOut);
                                toolbarPanel.Children.Add(btnClose);

                                Grid.SetColumn(toolbarPanel, 1);
                                headerGrid.Children.Add(toolbarPanel);

                                headerBorder.Child = headerGrid;
                                Grid.SetRow(headerBorder, 0);
                                mainGrid.Children.Add(headerBorder);

                                // Hỗ trợ kéo thả di chuyển Window khi nhấn vào Header
                                headerBorder.MouseDown += (s2, e2) =>
                                {
                                    if (e2.ChangedButton == System.Windows.Input.MouseButton.Left)
                                    {
                                        previewWin.DragMove();
                                    }
                                };

                                // Content ScrollViewer
                                var scroll = new ScrollViewer
                                {
                                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    Background = new SolidColorBrush(Color.FromRgb(235, 239, 245)),
                                    Margin = new Thickness(8)
                                };

                                var largeImg = new Image
                                {
                                    Source = questionImageControl.Source,
                                    Stretch = Stretch.Uniform,
                                    LayoutTransform = scaleTransform, // Dùng LayoutTransform để ScrollViewer tự động nhận diện kích thước mới
                                    Margin = new Thickness(20)
                                };
                                RenderOptions.SetBitmapScalingMode(largeImg, BitmapScalingMode.HighQuality);

                                scroll.Content = largeImg;
                                Grid.SetRow(scroll, 1);
                                mainGrid.Children.Add(scroll);

                                mainBorder.Child = mainGrid;
                                previewWin.Content = mainBorder;

                                // Hàm cập nhật zoom
                                void Zoom(double delta)
                                {
                                    double nextScale = scaleTransform.ScaleX + delta;
                                    if (nextScale >= 0.5 && nextScale <= 3.0)
                                    {
                                        scaleTransform.ScaleX = nextScale;
                                        scaleTransform.ScaleY = nextScale;
                                    }
                                }

                                btnZoomIn.Click += (s2, e2) => Zoom(0.1);
                                btnZoomOut.Click += (s2, e2) => Zoom(-0.1);

                                // Sự kiện cuộn chuột để zoom ảnh
                                scroll.PreviewMouseWheel += (s2, e2) =>
                                {
                                    e2.Handled = true;
                                    double delta = e2.Delta > 0 ? 0.1 : -0.1;
                                    Zoom(delta);
                                };

                                previewWin.ShowDialog();
                            }
                            catch (Exception ex)
                            {
                                Log.Warning("Failed to open image preview window: {Err}", ex.Message);
                            }
                        }
                    };

                    stack.Children.Add(questionImageControl);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to load question image: {Url}", question.ImageUrl);
                }
            }

            // Answer options
            string qType = (question.QuestionType ?? "").ToLower();
            if (qType == "multiplechoice" || qType == "mcq")
            {
                try
                {
                    var options = System.Text.Json.JsonSerializer.Deserialize<string[]>(question.OptionsJson ?? "[]") ?? Array.Empty<string>();
                    var labels = new[] { "A", "B", "C", "D", "E", "F" };
                    for (int i = 0; i < options.Length; i++)
                    {
                        string cleanOption = options[i];
                        if (cleanOption.Length > 3 && cleanOption[1] == '.' && cleanOption[2] == ' ')
                        {
                            cleanOption = cleanOption.Substring(3);
                        }
                        var rb = new RadioButton
                        {
                            Content = RenderLatexToTextBlock($"  {labels[i]}.  {cleanOption}", 13),
                            GroupName = $"Q{question.Id}",
                            Margin = new Thickness(42, 4, 0, 4),
                            Tag = labels[i], Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                            Cursor = System.Windows.Input.Cursors.Hand
                        };
                        rb.Checked += (s, ev) =>
                        {
                            if (rb.IsEnabled)
                            {
                                rb.Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                                rb.FontWeight = FontWeights.Bold;
                                OnAnswerInputChanged();
                            }
                        };
                        rb.Unchecked += (s, ev) =>
                        {
                            if (rb.IsEnabled)
                            {
                                rb.Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66));
                                rb.FontWeight = FontWeights.Normal;
                            }
                        };
                        stack.Children.Add(rb);
                    }
                }
                catch { }
            }
            else if (qType == "truefalse" || qType == "tf")
            {
                var rbTrue = new RadioButton { Content = "  Đúng", GroupName = $"Q{question.Id}", FontSize = 13, Margin = new Thickness(42, 4, 0, 4), Tag = "True", Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)), Cursor = System.Windows.Input.Cursors.Hand };
                var rbFalse = new RadioButton { Content = "  Sai", GroupName = $"Q{question.Id}", FontSize = 13, Margin = new Thickness(42, 4, 0, 4), Tag = "False", Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)), Cursor = System.Windows.Input.Cursors.Hand };
                
                rbTrue.Checked += (s, ev) =>
                {
                    if (rbTrue.IsEnabled)
                    {
                        rbTrue.Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                        rbTrue.FontWeight = FontWeights.Bold;
                        OnAnswerInputChanged();
                    }
                };
                rbTrue.Unchecked += (s, ev) =>
                {
                    if (rbTrue.IsEnabled)
                    {
                        rbTrue.Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66));
                        rbTrue.FontWeight = FontWeights.Normal;
                    }
                };
                
                rbFalse.Checked += (s, ev) =>
                {
                    if (rbFalse.IsEnabled)
                    {
                        rbFalse.Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                        rbFalse.FontWeight = FontWeights.Bold;
                        OnAnswerInputChanged();
                    }
                };
                rbFalse.Unchecked += (s, ev) =>
                {
                    if (rbFalse.IsEnabled)
                    {
                        rbFalse.Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66));
                        rbFalse.FontWeight = FontWeights.Normal;
                    }
                };
                
                stack.Children.Add(rbTrue);
                stack.Children.Add(rbFalse);
            }
            else if (qType == "fillblank" || qType == "fib" || qType == "fill_blank")
            {
                try
                {
                    var options = System.Text.Json.JsonSerializer.Deserialize<string[]>(question.OptionsJson ?? "[]") ?? Array.Empty<string>();
                    var spBlanks = new StackPanel { Margin = new Thickness(42, 6, 0, 6) };
                    for (int i = 0; i < options.Length; i++)
                    {
                        var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
                        var lbl = new TextBlock
                        {
                            Text = $"Chỗ trống {i + 1}:  ",
                            FontSize = ContainsLatex(options[i]) ? 13 * 1.10 : 13, VerticalAlignment = VerticalAlignment.Center,
                            Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                            MinWidth = 90
                        };
                        DockPanel.SetDock(lbl, Dock.Left);
                        row.Children.Add(lbl);

                        var tbBlank = new TextBox
                        {
                            FontSize = 13, Padding = new Thickness(8, 5, 8, 5),
                            Tag = $"Answer_FIB_{question.Id}_{i}",
                            Background = Brushes.White
                        };
                        tbBlank.TextChanged += (s, ev) => OnAnswerInputChanged();
                        row.Children.Add(tbBlank);
                        spBlanks.Children.Add(row);
                    }
                    stack.Children.Add(spBlanks);
                }
                catch { }
            }
            else if (qType == "matching" || qType == "match")
            {
                try
                {
                    var pairs = System.Text.Json.JsonSerializer.Deserialize<List<MatchPairItem>>(question.OptionsJson ?? "[]") ?? new();
                    var rightOptions = pairs.Select(p => p.Right).ToList();
                    
                    var spMatch = new StackPanel { Margin = new Thickness(42, 6, 0, 6) };
                    for (int i = 0; i < pairs.Count; i++)
                    {
                        var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
                        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30, GridUnitType.Pixel) });
                        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        var leftLbl = RenderLatexToTextBlock(pairs[i].Left, 13);
                        leftLbl.Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66));
                        Grid.SetColumn(leftLbl, 0); row.Children.Add(leftLbl);

                        var arrow = new TextBlock
                        {
                            Text = "➔", FontSize = 14, FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(arrow, 1); row.Children.Add(arrow);

                        var cboRight = new ComboBox
                        {
                            FontSize = 13, Padding = new Thickness(8, 6, 8, 6),
                            MinHeight = 36,
                            Tag = $"Answer_MATCH_{question.Id}_{i}",
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        cboRight.Items.Add("-- Chọn vế khớp --");
                        foreach (var ro in rightOptions) cboRight.Items.Add(ro);
                        cboRight.SelectedIndex = 0;
                        cboRight.SelectionChanged += (s, ev) => OnAnswerInputChanged();
                        Grid.SetColumn(cboRight, 2); row.Children.Add(cboRight);

                        spMatch.Children.Add(row);
                    }
                    stack.Children.Add(spMatch);
                }
                catch { }
            }
            else if (qType == "ordering" || qType == "order")
            {
                try
                {
                    var steps = System.Text.Json.JsonSerializer.Deserialize<string[]>(question.OptionsJson ?? "[]") ?? Array.Empty<string>();
                    var orderPanel = new StackPanel { Tag = $"Answer_ORDER_PANEL_{question.Id}", Margin = new Thickness(42, 6, 0, 6) };

                    for (int i = 0; i < steps.Length; i++)
                    {
                        var itemBorder = new Border
                        {
                            Background = Brushes.White, BorderThickness = new Thickness(1),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                            CornerRadius = new CornerRadius(5), Margin = new Thickness(0, 2, 0, 2),
                            Padding = new Thickness(10, 6, 10, 6)
                        };

                        var grid = new Grid();
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24, GridUnitType.Pixel) });
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        var numTxt = new TextBlock
                        {
                            Text = (i + 1).ToString(), FontSize = 13, FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                            VerticalAlignment = VerticalAlignment.Center, Tag = "Index"
                        };
                        Grid.SetColumn(numTxt, 0); grid.Children.Add(numTxt);

                        var valTxt = RenderLatexToTextBlock(steps[i], 13);
                        valTxt.Tag = "OptionText";
                        Grid.SetColumn(valTxt, 1); grid.Children.Add(valTxt);

                        var btnSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                        var btnUp = new Button
                        {
                            Content = "▲", Width = 36, Height = 32, FontSize = 11, Padding = new Thickness(0), Margin = new Thickness(0, 0, 2, 0),
                            Cursor = System.Windows.Input.Cursors.Hand
                        };
                        var btnDown = new Button
                        {
                            Content = "▼", Width = 36, Height = 32, FontSize = 11, Padding = new Thickness(0),
                            Cursor = System.Windows.Input.Cursors.Hand
                        };

                        btnUp.Click += (s, ev) => MoveOrderItem(orderPanel, itemBorder, -1);
                        btnDown.Click += (s, ev) => MoveOrderItem(orderPanel, itemBorder, 1);

                        btnSp.Children.Add(btnUp); btnSp.Children.Add(btnDown);
                        Grid.SetColumn(btnSp, 2); grid.Children.Add(btnSp);

                        itemBorder.Child = grid;
                        orderPanel.Children.Add(itemBorder);
                    }
                    stack.Children.Add(orderPanel);
                    UpdateOrderIndices(orderPanel);
                }
                catch { }
            }
            else
            {
                var tb = new TextBox
                {
                    Margin = new Thickness(42, 4, 0, 4),
                    Padding = new Thickness(10, 8, 10, 8),
                    FontSize = 13, Tag = $"Answer_SHORT_{question.Id}",
                    Background = Brushes.White,
                    AcceptsReturn = true, MinHeight = 60, MaxHeight = 120,
                    TextWrapping = TextWrapping.Wrap
                };
                tb.TextChanged += (s, ev) => OnAnswerInputChanged();
                stack.Children.Add(tb);
            }

            // Points
            stack.Children.Add(new TextBlock
            {
                Text = $"({question.Points} điểm)",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                Margin = new Thickness(42, 6, 0, 0)
            });

            // Video & Thí nghiệm ảo (Phase 6 & 7)
            if (!string.IsNullOrEmpty(question.VideoUrl))
            {
                var btnOpenMedia = new Button
                {
                    Content = "🎬 Xem tư liệu thực hành",
                    Height = 32,
                    Width = 200,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(42, 8, 0, 8),
                    Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                
                btnOpenMedia.MouseEnter += (s, e) => btnOpenMedia.Background = new SolidColorBrush(Color.FromRgb(56, 142, 60));
                btnOpenMedia.MouseLeave += (s, e) => btnOpenMedia.Background = new SolidColorBrush(Color.FromRgb(46, 125, 50));

                var brdMediaContainer = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Height = 350,
                    Margin = new Thickness(42, 4, 0, 8),
                    Visibility = Visibility.Collapsed,
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245))
                };

                var mediaGrid = new Grid();
                brdMediaContainer.Child = mediaGrid;

                // Khai báo hàm load media cục bộ
                async System.Threading.Tasks.Task LoadMediaAsync(string url)
                {
                    mediaGrid.Children.Clear();
                    mediaGrid.RowDefinitions.Clear();

                    if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uriResult) || 
                        (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps && uriResult.Scheme != Uri.UriSchemeFile))
                    {
                        ShowErrorPanel("⚠️ Đường dẫn tư liệu không hợp lệ hoặc không an toàn. Vui lòng báo lại giáo viên bộ môn.");
                        return;
                    }

                    if (url.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                    {
                        mediaGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                        mediaGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                        var me = new MediaElement
                        {
                            LoadedBehavior = MediaState.Manual,
                            UnloadedBehavior = MediaState.Stop,
                            Source = new Uri(url, UriKind.RelativeOrAbsolute),
                            Margin = new Thickness(4)
                        };
                        Grid.SetRow(me, 0);
                        mediaGrid.Children.Add(me);

                        me.MediaFailed += (sm, em) =>
                        {
                            ShowErrorPanel("⚠️ Không thể phát video tư liệu (Lỗi định dạng hoặc tệp không tồn tại).");
                        };

                        var controlPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 4, 0, 4)
                        };
                        Grid.SetRow(controlPanel, 1);

                        var btnPlay = new Button { Content = "▶ Phát", Width = 70, Height = 26, Margin = new Thickness(4), Cursor = System.Windows.Input.Cursors.Hand };
                        var btnPause = new Button { Content = "⏸ Tạm dừng", Width = 80, Height = 26, Margin = new Thickness(4), Cursor = System.Windows.Input.Cursors.Hand };
                        var btnFs = new Button { Content = "🖥️ Toàn màn hình", Width = 120, Height = 26, Margin = new Thickness(4), Cursor = System.Windows.Input.Cursors.Hand };

                        btnPlay.Click += (sp, ep) => me.Play();
                        btnPause.Click += (sp, ep) => me.Pause();
                        btnFs.Click += (sp, ep) =>
                        {
                            try
                            {
                                var fsWin = new Window
                                {
                                    WindowStyle = WindowStyle.None,
                                    WindowState = WindowState.Maximized,
                                    Background = Brushes.Black,
                                    Owner = Window.GetWindow(card)
                                };
                                var fsGrid = new Grid();
                                fsWin.Content = fsGrid;

                                controlPanel.Children.Remove(btnFs);
                                mediaGrid.Children.Remove(me);
                                fsGrid.Children.Add(me);

                                var btnCloseFs = new Button
                                {
                                    Content = "🗗 Thu nhỏ (ESC)",
                                    Width = 130, Height = 30,
                                    HorizontalAlignment = HorizontalAlignment.Right,
                                    VerticalAlignment = VerticalAlignment.Top,
                                    Margin = new Thickness(15),
                                    Background = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                                    Foreground = Brushes.White,
                                    FontWeight = FontWeights.Bold,
                                    Cursor = System.Windows.Input.Cursors.Hand
                                };
                                fsGrid.Children.Add(btnCloseFs);

                                void CloseFs()
                                {
                                    try
                                    {
                                        fsGrid.Children.Remove(me);
                                        fsGrid.Children.Remove(btnCloseFs);
                                        mediaGrid.Children.Add(me);
                                        controlPanel.Children.Add(btnFs);
                                        fsWin.Close();
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Warning("Lỗi đóng Fullscreen video: {Err}", ex.Message);
                                    }
                                }

                                btnCloseFs.Click += (sf, ef) => CloseFs();
                                fsWin.PreviewKeyDown += (sf, ef) => { if (ef.Key == System.Windows.Input.Key.Escape) CloseFs(); };
                                fsWin.ShowDialog();
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Lỗi phóng to video");
                            }
                        };

                        controlPanel.Children.Add(btnPlay);
                        controlPanel.Children.Add(btnPause);
                        controlPanel.Children.Add(btnFs);
                        mediaGrid.Children.Add(controlPanel);

                        me.Play();
                    }
                    else
                    {
                        mediaGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                        mediaGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                        var wv = new Microsoft.Web.WebView2.Wpf.WebView2 { Margin = new Thickness(4) };
                        Grid.SetRow(wv, 0);
                        mediaGrid.Children.Add(wv);

                        var controlPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 4, 0, 4)
                        };
                        Grid.SetRow(controlPanel, 1);

                        var btnFs = new Button { Content = "🖥️ Toàn màn hình", Width = 120, Height = 26, Margin = new Thickness(4), Cursor = System.Windows.Input.Cursors.Hand };
                        controlPanel.Children.Add(btnFs);
                        mediaGrid.Children.Add(controlPanel);

                        btnFs.Click += (sp, ep) =>
                        {
                            try
                            {
                                var fsWin = new Window
                                {
                                    WindowStyle = WindowStyle.None,
                                    WindowState = WindowState.Maximized,
                                    Background = Brushes.Black,
                                    Owner = Window.GetWindow(card)
                                };
                                var fsGrid = new Grid();
                                fsWin.Content = fsGrid;

                                var fsWv = new Microsoft.Web.WebView2.Wpf.WebView2 { Margin = new Thickness(4) };
                                fsGrid.Children.Add(fsWv);

                                var btnCloseFs = new Button
                                {
                                    Content = "🗗 Thu nhỏ (ESC)",
                                    Width = 130, Height = 30,
                                    HorizontalAlignment = HorizontalAlignment.Right,
                                    VerticalAlignment = VerticalAlignment.Top,
                                    Margin = new Thickness(15),
                                    Background = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                                    Foreground = Brushes.White,
                                    FontWeight = FontWeights.Bold,
                                    Cursor = System.Windows.Input.Cursors.Hand
                                };
                                fsGrid.Children.Add(btnCloseFs);

                                void CloseFs()
                                {
                                    try
                                    {
                                        var currentUri = fsWv.Source;
                                        fsWv.Dispose();
                                        fsWin.Close();
                                        if (currentUri != null)
                                        {
                                            wv.Source = currentUri;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Warning("Lỗi đồng bộ WebView2 sau fullscreen: {Err}", ex.Message);
                                    }
                                }

                                btnCloseFs.Click += (sf, ef) => CloseFs();
                                fsWin.PreviewKeyDown += (sf, ef) => { if (ef.Key == System.Windows.Input.Key.Escape) CloseFs(); };
                                
                                fsWin.Loaded += async (sf, ef) =>
                                {
                                    try
                                    {
                                        await fsWv.EnsureCoreWebView2Async();
                                        fsWv.Source = new Uri(url);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex, "Failed to load WebView2 in fullscreen");
                                    }
                                };

                                fsWin.ShowDialog();
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Lỗi phóng to WebView2");
                            }
                        };

                        try
                        {
                            await wv.EnsureCoreWebView2Async();
                            wv.Source = new Uri(url);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Failed to initialize WebView2 for url: {Url}", url);
                            ShowErrorPanel("⚠️ Lỗi khởi tạo WebView2 (Kiểm tra WebView2 Runtime đã được cài đặt chưa).");
                        }
                    }
                }

                void ShowErrorPanel(string errMsg)
                {
                    mediaGrid.Children.Clear();
                    mediaGrid.RowDefinitions.Clear();

                    var errorBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(255, 235, 235)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(15),
                        Margin = new Thickness(10)
                    };

                    var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    sp.Children.Add(new TextBlock
                    {
                        Text = errMsg,
                        Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        TextWrapping = TextWrapping.Wrap,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    });

                    var btnReload = new Button
                    {
                        Content = "🔄 Tải lại tư liệu",
                        Width = 130, Height = 28,
                        Margin = new Thickness(0, 10, 0, 0),
                        Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        Cursor = System.Windows.Input.Cursors.Hand
                    };
                    btnReload.Click += async (sR, eR) =>
                    {
                        btnReload.IsEnabled = false;
                        var oldContent = btnReload.Content;
                        btnReload.Content = "🔄 Đang nạp lại...";
                        try
                        {
                            await LoadMediaAsync(question.VideoUrl);
                        }
                        finally
                        {
                            await System.Threading.Tasks.Task.Delay(2000);
                            btnReload.Content = oldContent;
                            btnReload.IsEnabled = true;
                        }
                    };
                    sp.Children.Add(btnReload);

                    errorBorder.Child = sp;
                    mediaGrid.Children.Add(errorBorder);
                }

                void CloseMedia()
                {
                    brdMediaContainer.Visibility = Visibility.Collapsed;
                    btnOpenMedia.Content = "🎬 Xem tư liệu thực hành";
                    btnOpenMedia.Background = new SolidColorBrush(Color.FromRgb(46, 125, 50));

                    foreach (UIElement child in mediaGrid.Children)
                    {
                        if (child is Microsoft.Web.WebView2.Wpf.WebView2 wv)
                        {
                            wv.Dispose();
                        }
                        else if (child is MediaElement me)
                        {
                            me.Stop();
                            me.Source = null;
                        }
                    }
                    mediaGrid.Children.Clear();
                    mediaGrid.RowDefinitions.Clear();
                }

                btnOpenMedia.Click += async (s, e) =>
                {
                    if (brdMediaContainer.Visibility == Visibility.Collapsed)
                    {
                        brdMediaContainer.Visibility = Visibility.Visible;
                        btnOpenMedia.Content = "❌ Đóng tư liệu thực hành";
                        btnOpenMedia.Background = new SolidColorBrush(Color.FromRgb(198, 40, 40));

                        await LoadMediaAsync(question.VideoUrl);
                    }
                    else
                    {
                        CloseMedia();
                    }
                };

                card.Unloaded += (s, e) =>
                {
                    foreach (UIElement child in mediaGrid.Children)
                    {
                        if (child is Microsoft.Web.WebView2.Wpf.WebView2 wv)
                        {
                            wv.Dispose();
                        }
                        else if (child is MediaElement me)
                        {
                            me.Stop();
                            me.Source = null;
                        }
                    }
                    mediaGrid.Children.Clear();
                    mediaGrid.RowDefinitions.Clear();
                };

                stack.Children.Add(btnOpenMedia);
                stack.Children.Add(brdMediaContainer);
            }

            card.Child = stack;
            return card;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _remainingSeconds--;
            txtTimer.Text = FormatTime(_remainingSeconds);

            if (_remainingSeconds <= 60)
            {
                if (!_isTimerBlinking)
                {
                    _isTimerBlinking = true;
                    txtTimer.FontSize = 22;
                    txtTimer.FontWeight = FontWeights.Bold;
                    txtTimer.Foreground = Brushes.Red;

                    var blinkAnim = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        From = 1.0,
                        To = 0.2,
                        Duration = TimeSpan.FromSeconds(0.75),
                        AutoReverse = true,
                        RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
                    };
                    timerBorder.BeginAnimation(UIElement.OpacityProperty, blinkAnim);
                }
            }

            if (_remainingSeconds <= 0)
            {
                _timer?.Stop();
                Log.Warning("Quiz time limit reached. Auto-submitting quiz responses prior to showing modal alert.");
                DoSubmit();
                MessageBox.Show("Hết thời gian làm bài! Bài làm của em đã được nộp tự động thành công.", "Hết giờ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SubmitQuiz_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Bạn có chắc muốn nộp bài?", "Xác nhận nộp", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes) DoSubmit();
        }

        private void DoSubmit()
        {
            _isReviewMode = true;
            _timer?.Stop();
            UnregisterAntiCheating(); // Hủy giám sát Alt+Tab khi đã nộp bài
            btnSubmitQuiz.IsEnabled = false;

            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.Database != null)
                {
                    // Lấy thông tin Student hiện tại qua StudentIdentityService
                    var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(app.Database);
                    var (studentId, _, _) = identityService.GetCurrentStudent();
                    
                    var quiz = app.Database.Quizzes.Find(_quizId);
                    if (quiz == null) return;
                    
                    var questions = app.Database.Questions.Where(q => q.QuizId == _quizId).ToList();
                    
                    int totalPoints = 0;
                    var answersDict = new System.Collections.Generic.Dictionary<string, string>();

                    foreach (var q in questions)
                    {
                        totalPoints += q.Points;
                        string studentAns = GetStudentAnswerFromUI(q);
                        answersDict[q.Id.ToString()] = studentAns;
                    }
                    
                    // Ghi nhận số lần Alt+Tab vào gói tin nộp bài (Phase 5)
                    answersDict["__cheating_count"] = _cheatingCount.ToString();

                    // Save a pending QuizResult with score = -1 (graded by Teacher app)
                    var result = new Data.QuizResult
                    {
                        QuizId = _quizId,
                        StudentId = studentId,
                        Score = -1, // pending grading
                        TotalPoints = totalPoints,
                        CorrectCount = 0,
                        TotalQuestions = questions.Count,
                        TimeSpentSeconds = quiz.TimeLimitSeconds - Math.Max(0, _remainingSeconds),
                        AnswersJson = System.Text.Json.JsonSerializer.Serialize(answersDict),
                        SubmittedAt = DateTime.Now
                    };
                    app.Database.QuizResults.Add(result);
                    app.Database.SaveChanges();

                    try
                    {
                        ((StudentShell)Application.Current.MainWindow)?.UpdateDynamicBadges();
                    }
                    catch (Exception exBadge)
                    {
                        Log.Warning("Failed to update badges after quiz submit: {Err}", exBadge.Message);
                    }

                    // Gửi kết quả thi qua TCP socket lên trạm giáo viên
                    if (studentId > 0 && app.StudentNetwork != null)
                    {
                        string answersJson = System.Text.Json.JsonSerializer.Serialize(answersDict);
                        if (app.StudentNetwork.IsConnected)
                        {
                            _ = app.StudentNetwork.SendQuizAnswer(_quizId, answersJson);
                            Log.Information("Sent Quiz Answer over socket: QuizId={QuizId}", _quizId);

                            txtQuizInfo.Text = "⌛ Đang chấm điểm... Vui lòng đợi phản hồi từ giáo viên.";
                            txtQuizInfo.Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                            btnSubmitQuiz.Content = "⌛ Đang chấm điểm...";
                        }
                        else
                        {
                            QueueOfflineSubmission(_quizId, studentId, 0, answersJson);

                            txtQuizInfo.Text = "⚠️ Đang ngoại tuyến. Bài làm đã được lưu cục bộ và sẽ tự động gửi khi kết nối lại.";
                            txtQuizInfo.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0));
                            btnSubmitQuiz.Content = "⚠️ Chờ kết nối";
                        }
                    }

                    // Delete draft file on successful submit
                    try
                    {
                        string path = GetDraftPath(_quizId);
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                            Log.Information("Deleted quiz draft file for QuizId={QuizId}", _quizId);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Failed to delete quiz draft file: {Err}", ex.Message);
                    }
                }
            }
            catch (Exception ex) { Log.Warning("Quiz result save error: {Err}", ex.Message); }

            Log.Information("Quiz submitted: {Id}", _quizId);
        }

        private class MatchPairItem
        {
            public string Left { get; set; } = "";
            public string Right { get; set; } = "";
        }

        private void MoveOrderItem(StackPanel orderPanel, Border item, int direction)
        {
            int idx = orderPanel.Children.IndexOf(item);
            int target = idx + direction;
            if (target >= 0 && target < orderPanel.Children.Count)
            {
                orderPanel.Children.Remove(item);
                orderPanel.Children.Insert(target, item);
                UpdateOrderIndices(orderPanel);
                OnAnswerInputChanged();
            }
        }

        private void UpdateOrderIndices(StackPanel orderPanel)
        {
            for (int i = 0; i < orderPanel.Children.Count; i++)
            {
                if (orderPanel.Children[i] is Border border && border.Child is Grid grid)
                {
                    var numTxt = FindVisualChildren<TextBlock>(grid).FirstOrDefault(t => t.Tag?.ToString() == "Index");
                    if (numTxt != null) numTxt.Text = (i + 1).ToString();

                    var btns = FindVisualChildren<Button>(grid).ToList();
                    if (btns.Count == 2)
                    {
                        btns[0].IsEnabled = (i > 0);
                        btns[1].IsEnabled = (i < orderPanel.Children.Count - 1);
                    }
                }
            }
        }

        private string GetStudentAnswerFromUI(Data.Question q)
        {
            int questionId = q.Id;
            string qType = (q.QuestionType ?? "").ToLower();

            if (qType == "multiplechoice" || qType == "mcq" || qType == "truefalse" || qType == "tf")
            {
                var rbs = FindVisualChildren<RadioButton>(questionsPanel);
                foreach (var rb in rbs)
                {
                    if (rb.GroupName == $"Q{questionId}" && rb.IsChecked == true)
                        return rb.Tag?.ToString() ?? "";
                }
                return "";
            }
            else if (qType == "fillblank" || qType == "fib" || qType == "fill_blank")
            {
                var tbs = FindVisualChildren<TextBox>(questionsPanel);
                var list = new System.Collections.Generic.List<string>();
                int i = 0;
                while (true)
                {
                    var tb = tbs.FirstOrDefault(t => t.Tag?.ToString() == $"Answer_FIB_{questionId}_{i}");
                    if (tb == null) break;
                    list.Add(tb.Text.Trim());
                    i++;
                }
                return string.Join(";", list);
            }
            else if (qType == "matching" || qType == "match")
            {
                var cbos = FindVisualChildren<ComboBox>(questionsPanel);
                System.Collections.Generic.List<MatchPairItem> pairs = new();
                try
                {
                    pairs = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<MatchPairItem>>(q.OptionsJson ?? "[]") ?? new();
                }
                catch { }
                var list = new System.Collections.Generic.List<string>();
                for (int i = 0; i < pairs.Count; i++)
                {
                    var cbo = cbos.FirstOrDefault(c => c.Tag?.ToString() == $"Answer_MATCH_{questionId}_{i}");
                    string matched = cbo?.SelectedItem?.ToString() ?? "";
                    if (matched == "-- Chọn vế khớp --") matched = "";
                    list.Add($"{pairs[i].Left}->{matched}");
                }
                return "MATCH:" + string.Join(",", list);
            }
            else if (qType == "ordering" || qType == "order")
            {
                var panels = FindVisualChildren<StackPanel>(questionsPanel);
                var orderPanel = panels.FirstOrDefault(p => p.Tag?.ToString() == $"Answer_ORDER_PANEL_{questionId}");
                if (orderPanel != null)
                {
                    var steps = new System.Collections.Generic.List<string>();
                    foreach (var child in orderPanel.Children)
                    {
                        if (child is Border itemBorder)
                        {
                            var tbs = FindVisualChildren<TextBlock>(itemBorder);
                            var tb = tbs.FirstOrDefault(t => t.Tag?.ToString() == "OptionText");
                            if (tb != null) steps.Add(tb.Text);
                        }
                    }
                    return string.Join(" → ", steps);
                }
                return "";
            }
            else
            {
                var tbs = FindVisualChildren<TextBox>(questionsPanel);
                var tb = tbs.FirstOrDefault(t => t.Tag?.ToString() == $"Answer_SHORT_{questionId}");
                return tb?.Text.Trim() ?? "";
            }
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                bool isVisual = depObj is System.Windows.Media.Visual || depObj is System.Windows.Media.Media3D.Visual3D;
                int visualChildrenCount = isVisual ? VisualTreeHelper.GetChildrenCount(depObj) : 0;
                if (visualChildrenCount > 0)
                {
                    for (int i = 0; i < visualChildrenCount; i++)
                    {
                        DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                        if (child != null && child is T)
                            yield return (T)child;

                        foreach (T childOfChild in FindVisualChildren<T>(child))
                            yield return childOfChild;
                    }
                }
                else
                {
                    foreach (object logicalChild in LogicalTreeHelper.GetChildren(depObj))
                    {
                        if (logicalChild is DependencyObject depChild)
                        {
                            if (depChild is T target)
                                yield return target;

                            foreach (T childOfChild in FindVisualChildren<T>(depChild))
                                yield return childOfChild;
                        }
                    }
                }
            }
        }

        private static string FormatTime(int secs) =>
            secs >= 0 ? $"{secs / 60:D2}:{secs % 60:D2}" : "00:00";

        // ═══════════════════════════════════════════════════════════
        //  🎯 QUIZ FOCUS — GV yêu cầu HS tập trung vào câu hỏi cụ thể
        // ═══════════════════════════════════════════════════════════

        private Border? _currentFocusedCard;

        /// <summary>
        /// GV gửi lệnh QUIZ_FOCUS → highlight câu hỏi được focus + mờ các câu khác.
        /// Áp dụng cùng visual pattern với FocusBlock trong StudentLessonPage.
        /// </summary>
        public void FocusQuestion(int questionNumber)
        {
            try
            {
                // Show focus indicator banner
                focusIndicator.Visibility = Visibility.Visible;
                if (btnCloseFocus != null) btnCloseFocus.Visibility = Visibility.Collapsed;
                txtFocusTitle.Text = $"🎯 GV yêu cầu tập trung — Câu {questionNumber}";
                txtFocusDetail.Text = $"Hãy đọc kỹ câu hỏi #{questionNumber} được đánh dấu bên dưới";

                _currentFocusedCard = null;
                var zoomScale = AppSettings.FocusZoomScale;
                var fontScale = AppSettings.FocusFontScale;

                for (int i = 0; i < questionsPanel.Children.Count; i++)
                {
                    if (questionsPanel.Children[i] is Border card)
                    {
                        if (i + 1 == questionNumber)
                        {
                            // ══ CÂU ĐƯỢC FOCUS: highlight + zoom + enable ══
                            card.IsEnabled = true;
                            card.BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                            card.BorderThickness = new Thickness(5);
                            card.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                            card.Opacity = 1.0;
                            card.Padding = new Thickness(28, 20, 28, 20);
                            card.Margin = new Thickness(0, 16, 0, 16);
                            card.LayoutTransform = new ScaleTransform(zoomScale, zoomScale);
                            card.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                            ScaleQuestionFontSize(card, fontScale);
                            card.Effect = new System.Windows.Media.Effects.DropShadowEffect
                            {
                                BlurRadius = 40, ShadowDepth = 8,
                                Color = Color.FromRgb(25, 118, 210), Opacity = 0.5
                            };
                            _currentFocusedCard = card;
                            card.BringIntoView();
                        }
                        else
                        {
                            // ══ CÂU KHÔNG FOCUS: mờ + disable ══
                            card.IsEnabled = false;
                            card.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                            card.BorderThickness = new Thickness(1);
                            card.Background = new SolidColorBrush(Color.FromRgb(250, 250, 250));
                            card.Opacity = AppSettings.FocusDimOpacity;
                            card.Effect = null;
                            card.LayoutTransform = null;
                            card.Padding = new Thickness(20);
                            card.Margin = new Thickness(0, 0, 0, 12);
                            ScaleQuestionFontSize(card, 1.0);
                        }
                    }
                }

                if (_currentFocusedCard != null)
                    Log.Information("Student quiz focused on question {Num}", questionNumber);
                else
                    Log.Warning("Quiz focus question {Num} not found", questionNumber);
            }
            catch (Exception ex)
            {
                Log.Warning("FocusQuestion error: {Err}", ex.Message);
            }
        }

        /// <summary>Khôi phục tất cả câu hỏi về trạng thái bình thường</summary>
        public void UnfocusAll()
        {
            focusIndicator.Visibility = Visibility.Collapsed;
            if (btnCloseFocus != null) btnCloseFocus.Visibility = Visibility.Visible;

            foreach (var child in questionsPanel.Children)
            {
                if (child is Border card)
                {
                    card.IsEnabled = true;
                    card.Opacity = 1.0;
                    card.Effect = null;
                    card.LayoutTransform = null;
                    card.Padding = new Thickness(20);
                    card.Margin = new Thickness(0, 0, 0, 12);
                    ScaleQuestionFontSize(card, 1.0);

                    // CHỈ KHÔI PHỤC MÀU XÁM MẶC ĐỊNH NẾU KHÔNG Ở CHẾ ĐỘ XEM LẠI BÀI THI
                    if (!_isReviewMode)
                    {
                        card.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                        card.BorderThickness = new Thickness(1);
                        card.Background = new SolidColorBrush(Color.FromRgb(250, 250, 250));
                    }
                }
            }
            _currentFocusedCard = null;
            Log.Information("Student: all quiz questions unfocused");
        }

        /// <summary>Scale font size cho tất cả TextBlock bên trong question card</summary>
        private void ScaleQuestionFontSize(Border card, double scale)
        {
            if (card.Child is Panel panel)
                ScaleQuizPanelChildren(panel, scale);
        }

        /// <summary>Đệ quy scale font cho tất cả children</summary>
        private void ScaleQuizPanelChildren(Panel panel, double scale)
        {
            foreach (var el in panel.Children)
            {
                if (el is TextBlock tb)
                {
                    double baseSize = 13;
                    if (tb.FontWeight == FontWeights.Bold) baseSize = 14;
                    else if (tb.FontWeight == FontWeights.SemiBold && tb.FontSize >= 13) baseSize = 13;
                    else if (tb.FontSize <= 12) baseSize = tb.FontSize; // badges, points, explanation
                    tb.FontSize = baseSize * scale;
                }
                else if (el is RadioButton rb)
                {
                    rb.FontSize = 13 * scale;
                }
                else if (el is TextBox txb)
                {
                    txb.FontSize = 13 * scale;
                }
                else if (el is Border innerBorder && innerBorder.Child is Panel innerPanel)
                {
                    ScaleQuizPanelChildren(innerPanel, scale);
                }
                else if (el is Border innerBorder2 && innerBorder2.Child is TextBlock)
                {
                    // Badge children — scale via parent
                }
                else if (el is DockPanel dock)
                {
                    ScaleQuizPanelChildren(dock, scale);
                }
                else if (el is StackPanel sp)
                {
                    ScaleQuizPanelChildren(sp, scale);
                }
            }
        }

        /// <summary>Đóng banner focus indicator</summary>
        private void CloseFocus_Click(object sender, RoutedEventArgs e) => UnfocusAll();

        private void ShowQuizResultsReview(System.Collections.Generic.List<Data.Question> questions)
        {
            try
            {
                int currentStreak = 0;
                int maxStreak = 0;

                string subject = "";
                var sApp = (QASmartTouch.App)Application.Current;
                var sDb = sApp?.Database;
                if (sDb != null)
                {
                    var quiz = sDb.Quizzes.Find(_quizId);
                    if (quiz != null)
                    {
                        var lesson = sDb.Lessons.Find(quiz.LessonId);
                        if (lesson != null)
                        {
                            subject = lesson.Subject ?? "";
                        }
                    }
                }

                // questionsPanel chứa các Border card câu hỏi
                for (int i = 0; i < questions.Count; i++)
                {
                    if (i >= questionsPanel.Children.Count) break;
                    var card = questionsPanel.Children[i] as Border;
                    if (card == null) continue;

                    var q = questions[i];
                    var stack = card.Child as StackPanel;
                    if (stack == null) continue;

                    string studentAns = GetStudentAnswerFromUI(q);
                    bool isCorrect = QASmartClass.Helpers.QuizAnswerParser.GradeAnswer(studentAns, q.CorrectAnswer, q.QuestionType, subject);

                    if (isCorrect)
                    {
                        currentStreak++;
                        if (currentStreak > maxStreak) maxStreak = currentStreak;
                    }
                    else
                    {
                        currentStreak = 0;
                    }

                    // 1. Vô hiệu hóa tất cả input controls để không cho chọn lại (sử dụng IsHitTestVisible/Focusable để tránh gray-out màu sắc)
                    var rbs = FindVisualChildren<RadioButton>(card).ToList();
                    foreach (var rb in rbs)
                    {
                        rb.IsHitTestVisible = false;
                        rb.Focusable = false;
                    }
                    var tbs = FindVisualChildren<TextBox>(card).ToList();
                    foreach (var tb in tbs)
                    {
                        tb.IsReadOnly = true;
                        tb.Focusable = false;
                    }
                    var cbos = FindVisualChildren<ComboBox>(card).ToList();
                    foreach (var cbo in cbos)
                    {
                        cbo.IsHitTestVisible = false;
                        cbo.Focusable = false;
                    }
                    var btns = FindVisualChildren<Button>(card).ToList();
                    foreach (var btn in btns)
                    {
                        btn.IsHitTestVisible = false;
                        btn.Focusable = false;
                    }

                    // 1.1 Xóa các control dynamic cũ nếu có
                    var toRemove = stack.Children.OfType<FrameworkElement>().Where(c => c.Tag?.ToString() == "DynamicFeedback").ToList();
                    foreach (var child in toRemove) stack.Children.Remove(child);

                    // 2. Tô màu card câu hỏi
                    if (isCorrect)
                    {
                        card.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)); // Xanh lá nhạt
                        card.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));  // Xanh lá viền
                        card.BorderThickness = new Thickness(2);
                    }
                    else
                    {
                        card.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238)); // Đỏ nhạt
                        card.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 83, 80));   // Đỏ viền
                        card.BorderThickness = new Thickness(2);
                        ShakeElement(card);
                    }

                    // 3. Highlight câu trả lời đúng/sai
                    if (q.QuestionType == "MultipleChoice" || q.QuestionType == "MCQ" || q.QuestionType == "TrueFalse" || q.QuestionType == "TF")
                    {
                        foreach (var rb in rbs)
						{
							var rbTag = rb.Tag?.ToString() ?? "";
							string rawContent = "";
							TextBlock? tb = rb.Content as TextBlock;
							if (tb != null)
							{
								rawContent = tb.Text;
								if (string.IsNullOrEmpty(rawContent) && tb.Inlines.Count > 0)
								{
									rawContent = string.Concat(tb.Inlines.Select(il => (il as Run)?.Text ?? ""));
								}
							}
							else
							{
								rawContent = rb.Content?.ToString() ?? "";
							}

							// Loại bỏ các hậu tố cũ nếu có để tránh cộng dồn
							rawContent = rawContent.Replace(" 🟢 (Đáp án đúng)", "").Replace(" 🔴 (Bạn chọn)", "").Replace(" 🌟 (Chính xác)", "");

							// Dọn dẹp các run chứa hậu tố cũ trong TextBlock
							if (tb != null)
							{
								for (int idx = tb.Inlines.Count - 1; idx >= 0; idx--)
								{
									if (tb.Inlines.ElementAt(idx) is Run r &&
										(r.Text.Contains(" 🟢 (Đáp án đúng)") || r.Text.Contains(" 🔴 (Bạn chọn)") || r.Text.Contains(" 🌟 (Chính xác)")))
									{
										tb.Inlines.Remove(r);
									}
								}
							}

							if (rbTag.Equals(q.CorrectAnswer, StringComparison.OrdinalIgnoreCase))
							{
								// Đây là đáp án đúng -> Bôi đậm + chữ xanh lá
								rb.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
								rb.FontWeight = FontWeights.Bold;
								if (tb != null)
								{
									tb.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
									tb.FontWeight = FontWeights.Bold;
									tb.Inlines.Add(new Run(" 🟢 (Đáp án đúng)"));
								}
								else
								{
									rb.Content = rawContent + " 🟢 (Đáp án đúng)";
								}
							}
							else
							{
								if (tb != null)
								{
									tb.Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66));
									tb.FontWeight = FontWeights.Normal;
								}
								else
								{
									rb.Content = rawContent; // Trả về nội dung gốc nếu không phải đáp án đúng
								}
							}
							
							if (rbTag.Equals(studentAns, StringComparison.OrdinalIgnoreCase))
							{
								if (!isCorrect)
								{
									// Học sinh chọn sai -> Chữ đỏ đậm
									rb.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
									rb.FontWeight = FontWeights.Bold;
									if (tb != null)
									{
										tb.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
										tb.FontWeight = FontWeights.Bold;
										tb.Inlines.Add(new Run(" 🔴 (Bạn chọn)"));
									}
									else
									{
										rb.Content = rawContent + " 🔴 (Bạn chọn)";
									}
								}
								else
								{
									if (tb != null)
									{
										tb.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
										tb.FontWeight = FontWeights.Bold;
										tb.Inlines.Add(new Run(" 🌟 (Chính xác)"));
									}
									else
									{
										rb.Content = rawContent + " 🌟 (Chính xác)";
									}
								}
							}
						}

                        // Add feedback text block
                        var feedbackText = new TextBlock
                        {
                            Margin = new Thickness(42, 10, 0, 0),
                            FontSize = 13,
                            FontWeight = FontWeights.SemiBold,
                            Tag = "DynamicFeedback"
                        };
                        if (isCorrect)
                        {
                            feedbackText.Text = $"🎉 Chính xác! Bạn được {q.Points} điểm.";
                            feedbackText.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                        }
                        else
                        {
                            string displaySelected = string.IsNullOrEmpty(studentAns) ? "(Không trả lời)" : studentAns;
                            feedbackText.Text = $"❌ Sai rồi! Đáp án của bạn: {displaySelected} — Đáp án đúng: {q.CorrectAnswer}";
                            feedbackText.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                        }
                        stack.Children.Add(feedbackText);
                    }
                    else
                    {
                        string qType = (q.QuestionType ?? "").ToLower();

                        if (qType == "fillblank" || qType == "fib" || qType == "fill_blank")
                        {
                            var correctParts = (q.CorrectAnswer ?? "").Split(';');
                            foreach (var tb in tbs)
                            {
                                string tagStr = tb.Tag?.ToString() ?? "";
                                if (tagStr.StartsWith($"Answer_FIB_{q.Id}_"))
                                {
                                    if (int.TryParse(tagStr.Substring($"Answer_FIB_{q.Id}_".Length), out int index))
                                    {
                                        string correctVal = (index >= 0 && index < correctParts.Length) ? correctParts[index].Trim() : "";
                                        bool isBlankCorrect = tb.Text.Trim().Equals(correctVal, StringComparison.OrdinalIgnoreCase);
                                        if (isBlankCorrect)
                                        {
                                            tb.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201)); // Xanh lá
                                            tb.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                                        }
                                        else
                                        {
                                            tb.Background = new SolidColorBrush(Color.FromRgb(255, 205, 210)); // Đỏ
                                            tb.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                                        }
                                    }
                                }
                            }
                        }
                        else if (qType == "matching" || qType == "match")
                        {
                            string correctAnsStr = q.CorrectAnswer ?? "";
                            if (correctAnsStr.StartsWith("MATCH:"))
                            {
                                var correctPairs = correctAnsStr.Substring("MATCH:".Length).Split(',');
                                foreach (var cbo in cbos)
                                {
                                    string tagStr = cbo.Tag?.ToString() ?? "";
                                    if (tagStr.StartsWith($"Answer_MATCH_{q.Id}_"))
                                    {
                                        if (int.TryParse(tagStr.Substring($"Answer_MATCH_{q.Id}_".Length), out int index))
                                        {
                                            string correctVal = "";
                                            if (index >= 0 && index < correctPairs.Length)
                                            {
                                                var parts = correctPairs[index].Split(new[] { "->" }, StringSplitOptions.None);
                                                if (parts.Length == 2) correctVal = parts[1].Trim();
                                            }

                                            string selectedVal = cbo.SelectedItem?.ToString() ?? "";
                                            if (selectedVal == "-- Chọn vế khớp --") selectedVal = "";

                                            bool isMatchCorrect = selectedVal.Equals(correctVal, StringComparison.OrdinalIgnoreCase);
                                            if (isMatchCorrect)
                                            {
                                                cbo.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201));
                                            }
                                            else
                                            {
                                                cbo.Background = new SolidColorBrush(Color.FromRgb(255, 205, 210));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (qType == "ordering" || qType == "order")
                        {
                            var panels = FindVisualChildren<StackPanel>(card).ToList();
                            var orderPanel = panels.FirstOrDefault(p => p.Tag?.ToString() == $"Answer_ORDER_PANEL_{q.Id}");
                            if (orderPanel != null)
                            {
                                foreach (var item in orderPanel.Children)
                                {
                                    if (item is Border border)
                                    {
                                        border.Background = isCorrect 
                                            ? new SolidColorBrush(Color.FromRgb(232, 245, 233)) 
                                            : new SolidColorBrush(Color.FromRgb(255, 235, 238));
                                    }
                                }
                            }
                        }
                        else // SHORT
                        {
                            foreach (var tb in tbs)
                            {
                                if (tb.Tag?.ToString() == $"Answer_SHORT_{q.Id}")
                                {
                                    if (isCorrect)
                                    {
                                        tb.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201)); // Xanh lá
                                        tb.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                                    }
                                    else
                                    {
                                        tb.Background = new SolidColorBrush(Color.FromRgb(255, 205, 210)); // Đỏ
                                        tb.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                                    }
                                }
                            }
                        }

                        // Add feedback text block
                        var feedbackText = new TextBlock
                        {
                            Margin = new Thickness(42, 10, 0, 0),
                            FontSize = 13,
                            FontWeight = FontWeights.SemiBold,
                            Tag = "DynamicFeedback"
                        };
                        if (isCorrect)
                        {
                            feedbackText.Text = $"🎉 Chính xác! Bạn được {q.Points} điểm.";
                            feedbackText.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                        }
                        else
                        {
                            string displaySelected = string.IsNullOrEmpty(studentAns) ? "(Không trả lời)" : $"\"{studentAns}\"";
                            feedbackText.Text = $"❌ Chưa chính xác! Đáp án của bạn: {displaySelected} — Đáp án đúng của hệ thống: \"{q.CorrectAnswer}\"";
                            feedbackText.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                        }
                        stack.Children.Add(feedbackText);
                    }

                    if (!isCorrect)
                    {
                        string lessonTitle = "nội dung bài học liên quan";
                        try
                        {
                            var app = (QASmartTouch.App)Application.Current;
                            var db = app?.Database;
                            if (db != null)
                            {
                                var quiz = db.Quizzes.Find(_quizId);
                                if (quiz != null)
                                {
                                    var lesson = db.Lessons.Find(quiz.LessonId);
                                    if (lesson != null && !string.IsNullOrEmpty(lesson.Title))
                                    {
                                        lessonTitle = $"bài học \"{lesson.Title}\"";
                                    }
                                }
                            }
                        }
                        catch { }

                        var hintBlock = new TextBlock
                        {
                            Text = $"💡 Gợi ý: Em nên xem lại {lessonTitle} để nắm vững kiến thức nhé!",
                            FontSize = 13,
                            FontStyle = FontStyles.Italic,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x1B, 0x5E, 0x20)),
                            Margin = new Thickness(42, 6, 0, 0),
                            TextWrapping = TextWrapping.Wrap,
                            Tag = "DynamicFeedback"
                        };
                        stack.Children.Add(hintBlock);

                        // Hiển thị giải thích học thuật chi tiết nếu có
                        if (!string.IsNullOrEmpty(q.Explanation))
                        {
                            var explanationBorder = new Border
                            {
                                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                                BorderThickness = new Thickness(1, 1, 1, 3),
                                CornerRadius = new CornerRadius(6),
                                Padding = new Thickness(12),
                                Margin = new Thickness(42, 12, 0, 4),
                                Tag = "DynamicFeedback"
                            };
                            var explanationText = new TextBlock
                            {
                                Text = $"💡 Giải thích chi tiết: {q.Explanation}",
                                FontSize = 13,
                                FontStyle = FontStyles.Italic,
                                Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                                TextWrapping = TextWrapping.Wrap
                            };
                            explanationBorder.Child = explanationText;

                            // Hiệu ứng hoạt họa Fade-in (Phase 4)
                            explanationBorder.Opacity = 0;
                            explanationBorder.Loaded += (s, ev) =>
                            {
                                var animation = new System.Windows.Media.Animation.DoubleAnimation
                                {
                                    From = 0.0,
                                    To = 1.0,
                                    Duration = new Duration(TimeSpan.FromMilliseconds(350)),
                                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                                };
                                explanationBorder.BeginAnimation(UIElement.OpacityProperty, animation);
                            };

                            stack.Children.Add(explanationBorder);
                        }
                    }
                }

                // Hiển thị streak combo
                if (maxStreak >= 3)
                {
                    txtQuizInfo.Text = $"✅ Đã nộp bài thành công! 🔥 Combo Streak: {maxStreak} câu đúng liên tiếp!";
                    txtQuizInfo.Foreground = new SolidColorBrush(Color.FromRgb(224, 86, 36));

                    // Stop any existing storyboard first
                    if (_streakGlowStoryboard != null)
                    {
                        _streakGlowStoryboard.Stop();
                        _streakGlowStoryboard = null;
                    }

                    // Show custom streak popup
                    txtStreakMessage.Text = $"Chúc mừng em đã đạt chuỗi trả lời đúng liên tiếp {maxStreak} câu hỏi!";
                    
                    if (maxStreak >= 5)
                    {
                        // Enable glowing rainbow effect
                        var glowBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                        streakPopupBorder.BorderBrush = glowBrush;
                        streakPopupEffect.Color = Color.FromRgb(0xEF, 0x44, 0x44);
                        streakPopupEffect.BlurRadius = 30;

                        var storyboard = new System.Windows.Media.Animation.Storyboard();
                        var duration = TimeSpan.FromSeconds(3.0);

                        var shadowColorAnim = new System.Windows.Media.Animation.ColorAnimationUsingKeyFrames
                        {
                            Duration = new Duration(duration),
                            RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
                        };
                        shadowColorAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearColorKeyFrame(Color.FromRgb(0xEF, 0x44, 0x44), TimeSpan.FromSeconds(0.0)));
                        shadowColorAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearColorKeyFrame(Color.FromRgb(0xF9, 0x73, 0x16), TimeSpan.FromSeconds(0.5)));
                        shadowColorAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearColorKeyFrame(Color.FromRgb(0xEa, 0xB8, 0x06), TimeSpan.FromSeconds(1.0)));
                        shadowColorAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearColorKeyFrame(Color.FromRgb(0x22, 0xC5, 0x5E), TimeSpan.FromSeconds(1.5)));
                        shadowColorAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearColorKeyFrame(Color.FromRgb(0x3B, 0x82, 0xF6), TimeSpan.FromSeconds(2.0)));
                        shadowColorAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearColorKeyFrame(Color.FromRgb(0xA8, 0x55, 0xF7), TimeSpan.FromSeconds(2.5)));
                        shadowColorAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearColorKeyFrame(Color.FromRgb(0xEF, 0x44, 0x44), TimeSpan.FromSeconds(3.0)));

                        System.Windows.Media.Animation.Storyboard.SetTarget(shadowColorAnim, streakPopupEffect);
                        System.Windows.Media.Animation.Storyboard.SetTargetProperty(shadowColorAnim, new PropertyPath(System.Windows.Media.Effects.DropShadowEffect.ColorProperty));
                        storyboard.Children.Add(shadowColorAnim);

                        var borderColorAnim = new System.Windows.Media.Animation.ColorAnimationUsingKeyFrames
                        {
                            Duration = new Duration(duration),
                            RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
                        };
                        borderColorAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearColorKeyFrame(Color.FromRgb(0xEF, 0x44, 0x44), TimeSpan.FromSeconds(0.0)));
                        borderColorAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearColorKeyFrame(Color.FromRgb(0xF9, 0x73, 0x16), TimeSpan.FromSeconds(0.5)));
                        borderColorAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearColorKeyFrame(Color.FromRgb(0xEa, 0xB8, 0x06), TimeSpan.FromSeconds(1.0)));
                        borderColorAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearColorKeyFrame(Color.FromRgb(0x22, 0xC5, 0x5E), TimeSpan.FromSeconds(1.5)));
                        borderColorAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearColorKeyFrame(Color.FromRgb(0x3B, 0x82, 0xF6), TimeSpan.FromSeconds(2.0)));
                        borderColorAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearColorKeyFrame(Color.FromRgb(0xA8, 0x55, 0xF7), TimeSpan.FromSeconds(2.5)));
                        borderColorAnim.KeyFrames.Add(new System.Windows.Media.Animation.LinearColorKeyFrame(Color.FromRgb(0xEF, 0x44, 0x44), TimeSpan.FromSeconds(3.0)));

                        System.Windows.Media.Animation.Storyboard.SetTarget(borderColorAnim, glowBrush);
                        System.Windows.Media.Animation.Storyboard.SetTargetProperty(borderColorAnim, new PropertyPath(SolidColorBrush.ColorProperty));
                        storyboard.Children.Add(borderColorAnim);

                        _streakGlowStoryboard = storyboard;
                        storyboard.Begin();
                    }
                    else
                    {
                        // Reset to standard appearance
                        streakPopupBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                        streakPopupEffect.Color = Color.FromRgb(0xF5, 0x9E, 0x0B);
                        streakPopupEffect.BlurRadius = 30;
                    }

                    streakPopup.Visibility = Visibility.Visible;
                    PopElementElastic(streakPopup);
                }
                else
                {
                    txtQuizInfo.Text = "✅ Đã nộp bài thành công!";
                    txtQuizInfo.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                }
            }
            catch (Exception ex)
            {
                Log.Warning("ShowQuizResultsReview error: {Err}", ex.Message);
            }
        }

        private void CloseStreakPopup_Click(object sender, RoutedEventArgs e)
        {
            if (_streakGlowStoryboard != null)
            {
                _streakGlowStoryboard.Stop();
                _streakGlowStoryboard = null;
            }
            streakPopup.Visibility = Visibility.Collapsed;
        }

        private void OnAnswerInputChanged()
        {
            if (_isRestoringDraft) return;
            SaveDraft();
            // Đã loại bỏ PlayFeedbackSound(false) để tránh ô nhiễm tiếng ồn click khi học sinh gõ văn bản tự luận.
        }

        private void SaveDraft()
        {
            if (_quizId == 0) return;
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.Database == null) return;

                var questions = app.Database.Questions
                    .Where(q => q.QuizId == _quizId)
                    .ToList();

                var draftDict = new System.Collections.Generic.Dictionary<string, string>();
                foreach (var q in questions)
                {
                    string studentAns = GetStudentAnswerFromUI(q);
                    if (!string.IsNullOrEmpty(studentAns))
                    {
                        draftDict[q.Id.ToString()] = studentAns;
                    }
                }
                // Lưu cheating count cục bộ (Phase 5)
                draftDict["__cheating_count"] = _cheatingCount.ToString();

                string json = System.Text.Json.JsonSerializer.Serialize(draftDict);
                byte[] rawBytes = System.Text.Encoding.UTF8.GetBytes(json);
                byte[] encryptedBytes = System.Security.Cryptography.ProtectedData.Protect(rawBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);

                string path = GetDraftPath(_quizId);
                string? dir = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                System.IO.File.WriteAllBytes(path, encryptedBytes);
                ShowAutoSaveFeedback();
            }
            catch (Exception ex)
            {
                Log.Warning("Save quiz draft failed: {Err}", ex.Message);
            }
        }

        private void ShowAutoSaveFeedback()
        {
            try
            {
                TxtAutoSaveStatus.Text = $"💾 Đã tự động lưu nháp lúc {DateTime.Now:HH:mm:ss}";
                TxtAutoSaveStatus.Opacity = 1.0;

                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(1500),
                    BeginTime = TimeSpan.FromMilliseconds(500)
                };

                TxtAutoSaveStatus.BeginAnimation(UIElement.OpacityProperty, animation);
            }
            catch { }
        }

        private void RestoreDraft(int quizId)
        {
            try
            {
                string path = GetDraftPath(quizId);
                if (!System.IO.File.Exists(path)) return;

                byte[] fileBytes = System.IO.File.ReadAllBytes(path);
                string json = "";
                try
                {
                    byte[] decryptedBytes = System.Security.Cryptography.ProtectedData.Unprotect(fileBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                    json = System.Text.Encoding.UTF8.GetString(decryptedBytes);
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    // Fallback to reading as plaintext json for backward compatibility
                    json = System.Text.Encoding.UTF8.GetString(fileBytes);
                }
                var draft = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(json);
                if (draft == null) return;

                // Khôi phục số lần Alt+Tab (Phase 5)
                if (draft.TryGetValue("__cheating_count", out string? cVal) && int.TryParse(cVal, out int cCount))
                {
                    _cheatingCount = cCount;
                    Log.Information("Restored cheating count from draft: {Count}", _cheatingCount);
                }

                _isRestoringDraft = true;
                try
                {
                    // Restore RadioButtons (MultipleChoice, TrueFalse)
                    var rbs = FindVisualChildren<RadioButton>(questionsPanel).ToList();
                    foreach (var rb in rbs)
                    {
                        if (!string.IsNullOrEmpty(rb.GroupName) && rb.GroupName.StartsWith("Q") && rb.GroupName.Length > 1)
                        {
                            string qIdStr = rb.GroupName.Substring(1);
                            if (draft.TryGetValue(qIdStr, out var selectedAns) && rb.Tag?.ToString() == selectedAns)
                            {
                                rb.IsChecked = true;
                            }
                        }
                    }

                    // Restore TextBoxes (FIB, SHORT)
                    var tbs = FindVisualChildren<TextBox>(questionsPanel).ToList();
                    foreach (var tb in tbs)
                    {
                        if (tb.Tag != null)
                        {
                            string tagStr = tb.Tag.ToString()!;
                            if (tagStr.StartsWith("Answer_SHORT_"))
                            {
                                string qIdStr = tagStr.Substring("Answer_SHORT_".Length);
                                if (draft.TryGetValue(qIdStr, out var typedAns))
                                {
                                    tb.Text = typedAns;
                                }
                            }
                            else if (tagStr.StartsWith("Answer_FIB_"))
                            {
                                // Format: Answer_FIB_{questionId}_{i}
                                var parts = tagStr.Substring("Answer_FIB_".Length).Split('_');
                                if (parts.Length == 2)
                                {
                                    string qIdStr = parts[0];
                                    if (int.TryParse(parts[1], out int index) && draft.TryGetValue(qIdStr, out var typedAns))
                                    {
                                        var items = typedAns.Split(';');
                                        if (index >= 0 && index < items.Length)
                                        {
                                            tb.Text = items[index];
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Restore ComboBoxes (MATCH)
                    var cbos = FindVisualChildren<ComboBox>(questionsPanel).ToList();
                    foreach (var cbo in cbos)
                    {
                        if (cbo.Tag != null)
                        {
                            string tagStr = cbo.Tag.ToString()!;
                            if (tagStr.StartsWith("Answer_MATCH_"))
                            {
                                // Format: Answer_MATCH_{questionId}_{i}
                                var parts = tagStr.Substring("Answer_MATCH_".Length).Split('_');
                                if (parts.Length == 2)
                                {
                                    string qIdStr = parts[0];
                                    if (int.TryParse(parts[1], out int index) && draft.TryGetValue(qIdStr, out var typedAns))
                                    {
                                        if (typedAns.StartsWith("MATCH:"))
                                        {
                                            var pairsStr = typedAns.Substring("MATCH:".Length).Split(',');
                                            if (index >= 0 && index < pairsStr.Length)
                                            {
                                                var pairParts = pairsStr[index].Split(new[] { "->" }, StringSplitOptions.None);
                                                if (pairParts.Length == 2)
                                                {
                                                    string matchedVal = pairParts[1];
                                                    foreach (var item in cbo.Items)
                                                    {
                                                        if (item?.ToString() == matchedVal)
                                                        {
                                                            cbo.SelectedItem = item;
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Restore Ordering (ORDER)
                    var panels = FindVisualChildren<StackPanel>(questionsPanel).ToList();
                    foreach (var orderPanel in panels)
                    {
                        if (orderPanel.Tag != null)
                        {
                            string tagStr = orderPanel.Tag.ToString()!;
                            if (tagStr.StartsWith("Answer_ORDER_PANEL_"))
                            {
                                string qIdStr = tagStr.Substring("Answer_ORDER_PANEL_".Length);
                                if (draft.TryGetValue(qIdStr, out var typedAns) && !string.IsNullOrEmpty(typedAns))
                                {
                                    var orderedSteps = typedAns.Split(new[] { " → " }, StringSplitOptions.None);
                                    var childrenList = orderPanel.Children.OfType<Border>().ToList();
                                    orderPanel.Children.Clear();
                                    foreach (var step in orderedSteps)
                                    {
                                        var matchBorder = childrenList.FirstOrDefault(b =>
                                        {
                                            if (b.Child is Grid grid)
                                            {
                                                var optTxt = FindVisualChildren<TextBlock>(grid).FirstOrDefault(t => t.Tag?.ToString() == "OptionText");
                                                return optTxt?.Text == step;
                                            }
                                            return false;
                                        });
                                        if (matchBorder != null)
                                        {
                                            orderPanel.Children.Add(matchBorder);
                                            childrenList.Remove(matchBorder);
                                        }
                                    }
                                    foreach (var remaining in childrenList)
                                    {
                                        orderPanel.Children.Add(remaining);
                                    }
                                    UpdateOrderIndices(orderPanel);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    _isRestoringDraft = false;
                }
                Log.Information("Restored quiz draft successfully for QuizId={QuizId}", quizId);
            }
            catch (Exception ex)
            {
                Log.Warning("Restore quiz draft failed: {Err}", ex.Message);
            }
        }

        private void PlayFeedbackSound(bool success)
        {
            try
            {
                string soundPath = success 
                    ? @"C:\Windows\Media\Windows Feed Discovered.wav"
                    : @"C:\Windows\Media\Windows Navigation Start.wav";

                if (System.IO.File.Exists(soundPath))
                {
                    using (var player = new System.Media.SoundPlayer(soundPath))
                    {
                        player.Play();
                    }
                }
                else
                {
                    System.Media.SystemSounds.Asterisk.Play();
                }
            }
            catch { }
        }

        private void ShakeElement(UIElement element)
        {
            var transform = new TranslateTransform();
            element.RenderTransform = transform;

            var animation = new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromMilliseconds(400)
            };

            var easeOut = new System.Windows.Media.Animation.CubicEase
            {
                EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
            };

            animation.KeyFrames.Add(new System.Windows.Media.Animation.EasingDoubleKeyFrame(-12, TimeSpan.FromMilliseconds(80), easeOut));
            animation.KeyFrames.Add(new System.Windows.Media.Animation.EasingDoubleKeyFrame(10, TimeSpan.FromMilliseconds(160), easeOut));
            animation.KeyFrames.Add(new System.Windows.Media.Animation.EasingDoubleKeyFrame(-6, TimeSpan.FromMilliseconds(240), easeOut));
            animation.KeyFrames.Add(new System.Windows.Media.Animation.EasingDoubleKeyFrame(3, TimeSpan.FromMilliseconds(320), easeOut));
            animation.KeyFrames.Add(new System.Windows.Media.Animation.EasingDoubleKeyFrame(0, TimeSpan.FromMilliseconds(400), easeOut));

            transform.BeginAnimation(TranslateTransform.XProperty, animation);
        }

        private void QueueOfflineSubmission(int quizId, int studentId, int score, string answersJson)
        {
            try
            {
                string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QASmartClass", "offline_quiz_answers");
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }

                string path = System.IO.Path.Combine(dir, $"pending_quiz_{quizId}_{studentId}.json");
                var queueItem = new
                {
                    QuizId = quizId,
                    StudentId = studentId,
                    AnswersJson = answersJson,
                    CreatedAt = DateTime.Now,
                    RetryCount = 0 // Bổ sung trường đếm số lần thử lại (Phase 4)
                };
                string json = System.Text.Json.JsonSerializer.Serialize(queueItem);
                System.IO.File.WriteAllText(path, json);
                Log.Information("Queued quiz answer offline: QuizId={QuizId}, StudentId={StudentId}", quizId, studentId);
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to queue offline quiz answer: {Err}", ex.Message);
            }
        }

        private async Task SyncOfflineSubmissionsAsync()
        {
            try
            {
                string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QASmartClass", "offline_quiz_answers");
                if (!System.IO.Directory.Exists(dir)) return;

                var files = System.IO.Directory.GetFiles(dir, "pending_quiz_*.json");
                if (files.Length == 0) return;

                var app = (QASmartTouch.App)Application.Current;
                if (app?.StudentNetwork == null || !app.StudentNetwork.IsConnected) return;

                string failedDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QASmartClass", "failed_quiz_answers");
                System.IO.Directory.CreateDirectory(failedDir);

                Log.Information("Syncing {Count} offline quiz submissions...", files.Length);

                foreach (var file in files)
                {
                    int quizId = 0;
                    int studentId = 0;
                    string answersJson = "";
                    int retryCount = 0;

                    try
                    {
                        string content = await System.IO.File.ReadAllTextAsync(file);
                        using (var doc = System.Text.Json.JsonDocument.Parse(content))
                        {
                            var root = doc.RootElement;
                            quizId = root.GetProperty("QuizId").GetInt32();
                            if (root.TryGetProperty("StudentId", out var stdIdProp))
                            {
                                studentId = stdIdProp.GetInt32();
                            }
                            answersJson = root.GetProperty("AnswersJson").GetString() ?? "";
                            if (root.TryGetProperty("RetryCount", out var retryProp))
                            {
                                retryCount = retryProp.GetInt32();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Corrupted offline file {File}, moving to failed_quiz_answers. Err: {Err}", file, ex.Message);
                        string dest = System.IO.Path.Combine(failedDir, System.IO.Path.GetFileName(file));
                        if (System.IO.File.Exists(dest)) System.IO.File.Delete(dest);
                        System.IO.File.Move(file, dest);
                        continue;
                    }

                    // Nếu số lần thử lại đã đạt đến giới hạn (>= 5), chuyển sang thư mục failed
                    if (retryCount >= 5)
                    {
                        Log.Warning("Offline quiz submission {File} failed after 5 retries. Moving to failed folder.", System.IO.Path.GetFileName(file));
                        string dest = System.IO.Path.Combine(failedDir, System.IO.Path.GetFileName(file));
                        if (System.IO.File.Exists(dest)) System.IO.File.Delete(dest);
                        System.IO.File.Move(file, dest);
                        continue;
                    }

                    if (app.StudentNetwork.IsConnected)
                    {
                        try
                        {
                            await app.StudentNetwork.SendQuizAnswer(quizId, answersJson);
                            System.IO.File.Delete(file);
                            Log.Information("Synced offline quiz submission: {File}", System.IO.Path.GetFileName(file));
                            await Task.Delay(150); // Giãn cách tránh nghẽn socket LAN
                        }
                        catch (Exception sendEx)
                        {
                            retryCount++;
                            Log.Warning("Failed to send offline answer via LAN, retryCount={Count}. Err: {Err}", retryCount, sendEx.Message);

                            // Ghi lại file với số lần thử được tăng thêm
                            try
                            {
                                var updatedItem = new
                                {
                                    QuizId = quizId,
                                    StudentId = studentId,
                                    AnswersJson = answersJson,
                                    CreatedAt = DateTime.Now,
                                    RetryCount = retryCount
                                };
                                string updatedJson = System.Text.Json.JsonSerializer.Serialize(updatedItem);
                                await System.IO.File.WriteAllTextAsync(file, updatedJson);
                            }
                            catch (Exception writeEx)
                            {
                                Log.Warning("Failed to update retry count for file {File}: {Err}", file, writeEx.Message);
                            }
                            break; // Dừng vòng lặp đồng bộ vì mất mạng LAN
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("SyncOfflineSubmissionsAsync error: {Err}", ex.Message);
            }
        }

        // Win32 Keyboard Hook declarations and structures
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        private static IntPtr _hookId = IntPtr.Zero;
        private static LowLevelKeyboardProc? _proc;

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                var kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                bool alt = (kb.flags & 0x20) != 0;
                bool tab = kb.vkCode == 0x09;
                bool esc = kb.vkCode == 0x1B;
                bool lwin = kb.vkCode == 0x5B;
                bool rwin = kb.vkCode == 0x5C;
                bool f4 = kb.vkCode == 0x73;

                // Block Alt+Tab, Alt+F4, Windows keys, and Windows key combos
                if ((alt && tab) || (alt && f4) || lwin || rwin || (alt && esc))
                {
                    Log.Information("[Security] Blocked key combo: Alt={Alt}, KeyCode={KeyCode}", alt, kb.vkCode);
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private void RegisterKeyboardHook()
        {
            try
            {
                if (_hookId == IntPtr.Zero)
                {
                    _proc = HookCallback;
                    using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
                    using (var curModule = curProcess.MainModule)
                    {
                        if (curModule != null && curModule.ModuleName != null)
                        {
                            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
                            Log.Information("Low-Level Keyboard Hook registered successfully, HookID: {HookID}", _hookId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to register Low-Level Keyboard Hook: {Err}", ex.Message);
            }
        }

        private void UnregisterKeyboardHook()
        {
            try
            {
                if (_hookId != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookId);
                    _hookId = IntPtr.Zero;
                    _proc = null;
                    Log.Information("Low-Level Keyboard Hook unregistered successfully.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to unregister Low-Level Keyboard Hook: {Err}", ex.Message);
            }
        }

        private void StudentQuizPage_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void RegisterAntiCheating()
        {
            try
            {
                var win = Window.GetWindow(this);
                if (win != null)
                {
                    win.Deactivated -= Window_Deactivated;
                    win.Deactivated += Window_Deactivated;
                    Log.Information("Anti-cheating window deactivated tracking registered.");
                }

                RegisterKeyboardHook();

                PreviewMouseRightButtonDown -= StudentQuizPage_PreviewMouseRightButtonDown;
                PreviewMouseRightButtonDown += StudentQuizPage_PreviewMouseRightButtonDown;
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to register anti-cheating: {Err}", ex.Message);
            }
        }

        private void UnregisterAntiCheating()
        {
            try
            {
                var win = Window.GetWindow(this);
                if (win != null)
                {
                    win.Deactivated -= Window_Deactivated;
                    Log.Information("Anti-cheating window deactivated tracking unregistered.");
                }

                UnregisterKeyboardHook();

                PreviewMouseRightButtonDown -= StudentQuizPage_PreviewMouseRightButtonDown;
            }
            catch { }
        }

        private void Window_Deactivated(object? sender, EventArgs e)
        {
            if (_timer != null && _timer.IsEnabled && _remainingSeconds > 0 && !_isReviewMode)
            {
                try
                {
                    IntPtr hwnd = GetForegroundWindow();
                    if (hwnd != IntPtr.Zero)
                    {
                        uint processId;
                        GetWindowThreadProcessId(hwnd, out processId);
                        int currentProcId = System.Diagnostics.Process.GetCurrentProcess().Id;
                        if (processId == currentProcId)
                        {
                            return; // Cùng tiến trình của app -> Bỏ qua
                        }

                        using (var proc = System.Diagnostics.Process.GetProcessById((int)processId))
                        {
                            string procName = proc.ProcessName.ToLower();

                            // 1. Cho phép các tiến trình IME và hệ thống
                            if (procName == "unikey" || procName == "evkey" || procName == "shellexperiencehost")
                            {
                                return;
                            }

                            // 2. Phân loại explorer (chỉ phạt nếu là cửa sổ thư mục Windows Explorer, cho phép taskbar/startmenu)
                            if (procName == "explorer")
                            {
                                var classNameBuilder = new System.Text.StringBuilder(256);
                                GetClassName(hwnd, classNameBuilder, classNameBuilder.Capacity);
                                string className = classNameBuilder.ToString();
                                
                                // Taskbar hoặc Desktop hoặc StartMenu -> Cho phép
                                if (className == "Shell_TrayWnd" || className == "Shell_SecondaryTrayWnd" || className == "Progman" || className == "WorkerW")
                                {
                                    return;
                                }
                            }

                            // TẤT CẢ các tiến trình khác (không thuộc whitelist trên) đều bị coi là cấm/gian lận
                            // Điều này ngăn học sinh chuyển sang Brave, Opera, Discord, Zalo, Notepad, Acrobat Reader...
                            Log.Warning("[Security] Forbidden app detected in foreground: {ProcName}", procName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Error checking foreground process during focus loss: {Err}", ex.Message);
                    return; // Tránh cảnh báo oan nếu lỗi
                }

                try
                {
                    _cheatingCount++;
                    SaveDraft(); // Lưu ngay nháp để bảo vệ số lần gian lận cục bộ
                    
                    // Hiển thị banner cảnh báo đỏ trực tiếp trên UI học sinh (Phase 4)
                    borderCheatingAlert.Visibility = Visibility.Visible;

                    // Tự động ẩn banner sau 5 giây
                    var hideTimer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(5)
                    };
                    hideTimer.Tick += (s, ev) =>
                    {
                        borderCheatingAlert.Visibility = Visibility.Collapsed;
                        hideTimer.Stop();
                    };
                    hideTimer.Start();

                    // Gửi gói tin lên Server giáo viên (luồng cũ)
                    var app = (QASmartTouch.App)Application.Current;
                    if (app?.StudentNetwork != null && app.StudentNetwork.IsConnected)
                    {
                        _ = app.StudentNetwork.SendCheatingAlert(_quizId, "Học sinh rời khỏi màn hình kiểm tra (Alt+Tab hoặc chuyển App)");
                        Log.Warning("Student left exam window. Alert sent for QuizId={QuizId}", _quizId);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Failed to handle cheating alert: {Err}", ex.Message);
                }
            }
        }

        private string GetDraftPath(int quizId)
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.Database != null)
                {
                    var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(app.Database);
                    var (_, studentCode, _) = identityService.GetCurrentStudent();
                    string safeCode = string.IsNullOrEmpty(studentCode) ? "default" : studentCode.Trim();
                    
                    // Xóa các ký tự cấm đặt tên tệp trong Windows để tránh lỗi IO
                    string cleanCode = string.Concat(safeCode.Split(System.IO.Path.GetInvalidFileNameChars()));
                    
                    return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QASmartClass", $"quiz_draft_{quizId}_{cleanCode}.json");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to resolve draft path with student identity: {Err}", ex.Message);
            }
            return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QASmartClass", $"quiz_draft_{quizId}_default.json");
        }

        // ==========================================
        // PHASE 5: CÁC HÀM HELPER NÂNG CẤP CHUYÊN SÂU
        // ==========================================

        private async System.Threading.Tasks.Task<TimeSpan> FetchNtpOffsetAsync()
        {
            try
            {
                string ntpServer = "pool.ntp.org";
                var ipAddresses = await System.Net.Dns.GetHostAddressesAsync(ntpServer);
                if (ipAddresses.Length == 0) return TimeSpan.Zero;
                
                var ipEndPoint = new System.Net.IPEndPoint(ipAddresses[0], 123);
                var ntpData = new byte[48];
                ntpData[0] = 0x1B; // LeapIndicator = 0, VersionNum = 3, Mode = 3

                using (var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp))
                {
                    socket.Connect(ipEndPoint);
                    socket.ReceiveTimeout = 1500;
                    socket.Send(ntpData);
                    
                    var receiveTask = System.Threading.Tasks.Task.Run(() => {
                        try {
                            socket.Receive(ntpData);
                            return true;
                        } catch { return false; }
                    });

                    if (await System.Threading.Tasks.Task.WhenAny(receiveTask, System.Threading.Tasks.Task.Delay(1500)) == receiveTask)
                    {
                        if (receiveTask.Result)
                        {
                            ulong intPart = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | ntpData[43];
                            ulong fractPart = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | ntpData[47];
                            
                            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000UL);
                            var networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((double)milliseconds);
                            
                            _isTimeSynced = true;
                            Log.Information("NTP time synchronization successful. Offset: {Offset}", networkDateTime - DateTime.UtcNow);
                            return networkDateTime - DateTime.UtcNow;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to fetch NTP time offset: {Err}", ex.Message);
            }
            _isTimeSynced = false;
            return TimeSpan.Zero;
        }

        private void SpeakText(string text)
        {
            try
            {
                lock (_speechLock)
                {
                    if (_synthesizer != null)
                    {
                        _synthesizer.Dispose();
                        _synthesizer = null;
                    }
                    
                    _synthesizer = new System.Speech.Synthesis.SpeechSynthesizer();
                    _synthesizer.Volume = _ttsVolume;
                    var voices = _synthesizer.GetInstalledVoices();
                    var viVoice = voices.FirstOrDefault(v => v.VoiceInfo.Culture.Name.StartsWith("vi") || v.VoiceInfo.Description.ToLower().Contains("viet"));
                    if (viVoice != null)
                    {
                        _synthesizer.SelectVoice(viVoice.VoiceInfo.Name);
                    }
                    
                    string cleanText = text.Replace("$", "").Replace("\\frac", "").Replace("{", "").Replace("}", "");
                    _synthesizer.SpeakAsync(cleanText);
                    Log.Information("Started playing TTS for text: {Text}", cleanText.Length > 30 ? cleanText.Substring(0, 30) + "..." : cleanText);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("TTS Speak failed: {Err}", ex.Message);
            }
        }

        private TextBlock RenderLatexToTextBlock(string text, double baseFontSize)
        {
            var tb = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = baseFontSize * _globalFontSizeMultiplier,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                VerticalAlignment = VerticalAlignment.Center,
                LineHeight = 22,
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight
            };

            if (string.IsNullOrEmpty(text)) return tb;

            if (!ContainsLatex(text))
            {
                tb.Text = text;
                return tb;
            }

            try
            {
                string[] parts = text.Split('$');
                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i];
                    if (string.IsNullOrEmpty(part)) continue;

                    if (i % 2 == 0)
                    {
                        tb.Inlines.Add(new Run(part));
                    }
                    else
                    {
                        string cleanLatex = part;
                        cleanLatex = cleanLatex.Replace("\\alpha", "α")
                                               .Replace("\\beta", "β")
                                               .Replace("\\gamma", "γ")
                                               .Replace("\\pi", "π")
                                               .Replace("\\infty", "∞")
                                               .Replace("\\pm", "±")
                                               .Replace("\\times", "×")
                                               .Replace("\\div", "÷")
                                               .Replace("\\leq", "≤")
                                               .Replace("\\le", "≤")
                                               .Replace("\\geq", "≥")
                                               .Replace("\\ge", "≥")
                                               .Replace("\\Delta", "Δ")
                                               .Replace("\\theta", "θ")
                                               .Replace("\\to", "→")
                                               .Replace("\\rightarrow", "→")
                                               .Replace("\\neq", "≠")
                                               .Replace("\\ne", "≠")
                                               .Replace("\\cdot", "·")
                                               .Replace("\\approx", "≈")
                                               .Replace("\\lambda", "λ")
                                               .Replace("\\mu", "μ")
                                               .Replace("\\sigma", "σ")
                                               .Replace("\\omega", "ω")
                                               .Replace("\\phi", "φ")
                                               .Replace("\\degree", "°")
                                               .Replace("^\\circ", "°");

                        while (cleanLatex.Contains("\\frac{"))
                        {
                            int idx = cleanLatex.IndexOf("\\frac{");
                            int open1 = idx + 5;
                            int close1 = FindMatchingBrace(cleanLatex, open1);
                            if (close1 == -1) break;

                            int open2 = close1 + 1;
                            if (open2 >= cleanLatex.Length || cleanLatex[open2] != '{') break;
                            int close2 = FindMatchingBrace(cleanLatex, open2);
                            if (close2 == -1) break;

                            string num = cleanLatex.Substring(open1 + 1, close1 - open1 - 1);
                            string den = cleanLatex.Substring(open2 + 1, close2 - open2 - 1);
                            
                            cleanLatex = cleanLatex.Substring(0, idx) + $"({num})/({den})" + cleanLatex.Substring(close2 + 1);
                        }

                        while (cleanLatex.Contains("\\sqrt{"))
                        {
                            int idx = cleanLatex.IndexOf("\\sqrt{");
                            int open = idx + 5;
                            int close = FindMatchingBrace(cleanLatex, open);
                            if (close == -1) break;

                            string inner = cleanLatex.Substring(open + 1, close - open - 1);
                            cleanLatex = cleanLatex.Substring(0, idx) + $"√({inner})" + cleanLatex.Substring(close + 1);
                        }

                        int pos = 0;
                        while (pos < cleanLatex.Length)
                        {
                            char c = cleanLatex[pos];
                            if (c == '^' || c == '_')
                            {
                                pos++;
                                if (pos >= cleanLatex.Length) break;

                                string scriptText = "";
                                if (cleanLatex[pos] == '{')
                                {
                                    int closeIdx = FindMatchingBrace(cleanLatex, pos);
                                    if (closeIdx != -1)
                                    {
                                        scriptText = cleanLatex.Substring(pos + 1, closeIdx - pos - 1);
                                        pos = closeIdx + 1;
                                    }
                                    else
                                    {
                                        scriptText = cleanLatex.Substring(pos);
                                        pos = cleanLatex.Length;
                                    }
                                }
                                else
                                {
                                    scriptText = cleanLatex[pos].ToString();
                                    pos++;
                                }

                                var runScript = new Run(scriptText)
                                {
                                    BaselineAlignment = (c == '^') ? BaselineAlignment.Superscript : BaselineAlignment.Subscript,
                                    FontSize = baseFontSize * _globalFontSizeMultiplier * 0.75,
                                    FontStyle = FontStyles.Italic,
                                    FontFamily = new FontFamily("Cambria Math, Times New Roman")
                                };
                                tb.Inlines.Add(runScript);
                            }
                            else
                            {
                                int nextSpecial = cleanLatex.IndexOfAny(new[] { '^', '_' }, pos);
                                string textChunk = "";
                                if (nextSpecial == -1)
                                {
                                    textChunk = cleanLatex.Substring(pos);
                                    pos = cleanLatex.Length;
                                }
                                else
                                {
                                    textChunk = cleanLatex.Substring(pos, nextSpecial - pos);
                                    pos = nextSpecial;
                                }

                                var runNormal = new Run(textChunk)
                                {
                                    FontStyle = FontStyles.Italic,
                                    FontFamily = new FontFamily("Cambria Math, Times New Roman")
                                };
                                tb.Inlines.Add(runNormal);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("RenderLatexToTextBlock error: {Err}, fallback to raw", ex.Message);
                tb.Inlines.Clear();
                tb.Text = text;
            }

            return tb;
        }

        private static int FindMatchingBrace(string text, int openBraceIdx)
        {
            int count = 0;
            for (int i = openBraceIdx; i < text.Length; i++)
            {
                if (text[i] == '{') count++;
                else if (text[i] == '}')
                {
                    count--;
                    if (count == 0) return i;
                }
            }
            return -1;
        }

        private void CleanupFailedSubmissions()
        {
            try
            {
                string failedDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QASmartClass", "failed_quiz_answers");
                if (!System.IO.Directory.Exists(failedDir)) return;
                
                var files = System.IO.Directory.GetFiles(failedDir, "pending_quiz_*.json");
                var threshold = DateTime.Now.AddDays(-30);
                foreach (var file in files)
                {
                    var info = new System.IO.FileInfo(file);
                    if (info.LastWriteTime < threshold)
                    {
                        info.Delete();
                        Log.Information("Cleaned up expired failed quiz submission: {File}", info.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("CleanupFailedSubmissions error: {Err}", ex.Message);
            }
        }

        private void PopElementElastic(UIElement element)
        {
            var transform = new ScaleTransform(0.0, 0.0);
            element.RenderTransform = transform;
            element.RenderTransformOrigin = new Point(0.5, 0.5);

            var animation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(800),
                EasingFunction = new System.Windows.Media.Animation.ElasticEase
                {
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut,
                    Oscillations = 3,
                    Springiness = 3
                }
            };

            transform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        private void UpdateLayoutFontSize()
        {
            foreach (var tb in FindVisualChildren<TextBlock>(questionsPanel))
            {
                if (tb.Tag?.ToString() == "OptionText" || tb.Parent is Border || tb.Parent is DockPanel || tb.Parent is Grid)
                {
                    double baseSize = 13;
                    if (tb.Parent is DockPanel) baseSize = 14; // Đề bài
                    tb.FontSize = baseSize * _globalFontSizeMultiplier;
                }
            }
        }

        private void ZoomInFont_Click(object sender, RoutedEventArgs e)
        {
            if (_globalFontSizeMultiplier < 1.5)
            {
                _globalFontSizeMultiplier += 0.1;
                UpdateLayoutFontSize();
                Log.Information("Font size multiplier increased to: {Multiplier}", _globalFontSizeMultiplier);
            }
        }
        
        private void ZoomOutFont_Click(object sender, RoutedEventArgs e)
        {
            if (_globalFontSizeMultiplier > 0.8)
            {
                _globalFontSizeMultiplier -= 0.1;
                UpdateLayoutFontSize();
                Log.Information("Font size multiplier decreased to: {Multiplier}", _globalFontSizeMultiplier);
            }
        }

        private void StudentQuizPage_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                if (e.Key == System.Windows.Input.Key.OemPlus || e.Key == System.Windows.Input.Key.Add)
                {
                    ZoomInFont_Click(null, null);
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.OemMinus || e.Key == System.Windows.Input.Key.Subtract)
                {
                    ZoomOutFont_Click(null, null);
                    e.Handled = true;
                }
            }
        }
    }
}