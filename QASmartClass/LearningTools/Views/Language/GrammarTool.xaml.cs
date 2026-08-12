using QASmartClass.LearningTools.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Language
{
    public partial class GrammarTool : BaseToolControl
    {
        private readonly Random _rng = new();
        private List<GramQ> _gQuiz = new();
        private bool _gActive;
        private List<GramQ> _mcQuiz = new();
        private bool _mcActive;

        // DB Data lists
        private List<QASmartClass.Data.GrammarTense> _dbTenses = new();
        private List<QASmartClass.Data.GrammarQuestion> _dbQuestions = new();
        
        // Interactive Timeline fields
        private List<TenseData> _orderedTenses = new();
        private int _selectedTenseIndex = 0;

        private class UserQuizAttempt
        {
            public int QuestionIndex { get; set; }
            public string QuestionText { get; set; } = "";
            public string UserAnswer { get; set; } = "";
            public string CorrectAnswer { get; set; } = "";
            public bool IsCorrect { get; set; }
            public string PlayerName { get; set; } = "";
        }
        private readonly List<UserQuizAttempt> _currentAttempts = new();

        private int _selectedPlayerCount = 1;
        private readonly string[] _playerNames = new string[3];
        private readonly int[] _playerScores = new int[3];

        // Concurrent split-screen fields
        private readonly List<PlayerBoard> _activeBoards = new();

        // Fullscreen fields
        private Grid? _fullscreenOriginalParent;
        private UIElement? _fullscreenElement;

        private class PlayerBoard
        {
            public int PlayerIndex { get; set; }
            public string Name { get; set; } = "";
            public int Score { get; set; }
            public int QuestionIndex { get; set; }
            public List<GramQ> Questions { get; set; } = new();
            public List<UserQuizAttempt> Attempts { get; set; } = new();
            public bool IsFinished { get; set; }
            
            // UI elements for Fill-in-blank
            public Border? BoardBorder { get; set; }
            public TextBlock? HeaderText { get; set; }
            public StackPanel? ProgressPanel { get; set; }
            public TextBlock? SentenceText { get; set; }
            public TextBlock? HintText { get; set; }
            public TextBox? AnswerTextBox { get; set; }
            public TextBlock? FeedbackText { get; set; }
            public StackPanel? KeyboardContainer { get; set; }
            
            // UI elements for MCQ
            public Border? McBoardBorder { get; set; }
            public TextBlock? McHeaderText { get; set; }
            public StackPanel? McProgressPanel { get; set; }
            public TextBlock? McSentenceText { get; set; }
            public TextBlock? McHintText { get; set; }
            public StackPanel? McOptionsPanel { get; set; }
            public TextBlock? McFeedbackText { get; set; }
            
            // Timer for MCQ
            public int McTimeLeft { get; set; }
            public DispatcherTimer? BoardTimer { get; set; }
            public TextBlock? McTimerText { get; set; }
            public ProgressBar? McTimerBar { get; set; }
            
            // Transition timer
            public DispatcherTimer? TransitionTimer { get; set; }
        }

        private class TenseData
        {
            public string Name { get; set; } = "";
            public string NameVi { get; set; } = "";
            public string Structure { get; set; } = "";
            public string Example { get; set; } = "";
            public string Signal { get; set; } = "";
            public string ColorHex { get; set; } = "";
            public string SectionId { get; set; } = "";
            public string Purpose { get; set; } = "";
            public string Usage { get; set; } = "";
        }

        // Timer and Quiz stats
        private DateTime _quizStartTime;
        private string? _focusedSectionId;
        private bool _timerPausedByHelp;

        public GrammarTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextTable != null) menuTextTable.Text = isVN ? "Bảng ngữ pháp" : "Grammar Chart";
                if (menuTextFillBlank != null) menuTextFillBlank.Text = isVN ? "Quiz điền từ" : "Fill-blank Quiz";
                if (menuTextMcq != null) menuTextMcq.Text = isVN ? "Trắc nghiệm thì" : "MCQ Quiz";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                LoadDataFromDb();
                BuildGrammar();
                PopulateTenseFilters();
                TouchTextPad.Attach(txtGramPlayer1, mode: "text");
                TouchTextPad.Attach(txtGramPlayer2, mode: "text");
                TouchTextPad.Attach(txtGramPlayer3, mode: "text");
                TouchTextPad.Attach(txtMcPlayer1, mode: "text");
                TouchTextPad.Attach(txtMcPlayer2, mode: "text");
                TouchTextPad.Attach(txtMcPlayer3, mode: "text");
                LoadPracticalApps();
                
                // Tự động kiểm tra trạng thái focus từ ClassControl khi load
                try
                {
                    var app = Application.Current as QASmartTouch.App;
                    if (app != null && QASmartTouch.App.ClassControl != null)
                    {
                        if (QASmartTouch.App.ClassControl.ActiveToolFocusId == "grammar" && 
                            !string.IsNullOrEmpty(QASmartTouch.App.ClassControl.ActiveToolSectionId))
                        {
                            _focusedSectionId = QASmartTouch.App.ClassControl.ActiveToolSectionId;
                            ApplySectionFocusFilter();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning("GrammarTool self-focus error: {Err}", ex.Message);
                }
            };
            Unloaded += (_, _) =>
            {
                ResetActiveQuizzes();
            };
        }

        private void LoadPracticalApps()
        {
            try
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                string suffix = isVN ? "VN" : "EN";

                var items = new List<PracticalAppItem>
                {
                    new PracticalAppItem
                    {
                        Icon = "📝",
                        Title = isVN ? "Viết luận & Email" : "Essay & Email Writing",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_grammar_1_{suffix}.png",
                        Description = isVN 
                            ? "Hỗ trợ viết email, bài luận học thuật và báo cáo khoa học một cách chính xác, trang trọng và logic." 
                            : "Write academic essays, professional emails, and scientific reports accurately and professionally."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💬",
                        Title = isVN ? "Giao tiếp hàng ngày" : "Daily Conversations",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_grammar_2_{suffix}.png",
                        Description = isVN 
                            ? "Diễn đạt chính xác thời gian xảy ra sự việc, giúp cuộc đối thoại tự nhiên và tránh hiểu lầm." 
                            : "Express the exact timing of events, making conversations flow naturally and avoiding misunderstandings."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💼",
                        Title = isVN ? "Phỏng vấn xin việc" : "Job Interviews",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_grammar_3_{suffix}.png",
                        Description = isVN 
                            ? "Thể hiện kinh nghiệm quá khứ (Past Simple) và kết quả đạt được (Present Perfect) một cách thuyết phục." 
                            : "Confidently present your past roles (Past Simple) and recent achievements (Present Perfect)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎓",
                        Title = isVN ? "Kỳ thi quốc tế" : "International Exams",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_grammar_4_{suffix}.png",
                        Description = isVN 
                            ? "Chinh phục các chứng chỉ quốc tế như IELTS, TOEFL, TOEIC với số điểm ngữ pháp tuyệt đối." 
                            : "Achieve high scores in IELTS, TOEFL, TOEIC by mastering complex sentence structures and tenses."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎤",
                        Title = isVN ? "Thuyết trình dự án" : "Project Presentations",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_grammar_5_{suffix}.png",
                        Description = isVN 
                            ? "Giúp báo cáo dự án sinh động, mạch lạc khi kể về quá trình phát triển và định hướng tương lai." 
                            : "Keep your project updates coherent and structured when presenting past milestones and future goals."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📰",
                        Title = isVN ? "Đọc báo & Tin tức" : "Reading News & Media",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_grammar_6_{suffix}.png",
                        Description = isVN 
                            ? "Nắm bắt nhanh thông tin sự kiện trên báo chí quốc tế nhờ hiểu đúng ngữ cảnh và thời gian." 
                            : "Quickly grasp international news updates by correctly interpreting grammar nuances and timelines."
                    }
                ,
                    new PracticalAppItem
                    {
                        Icon = "💻",
                        Title = isVN ? "Lập trình ngôn ngữ tự nhiên (NLP)" : "Natural Language Processing (NLP)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_grammar_7_{suffix}.png",
                        Description = isVN 
                            ? "Xây dựng mô hình ngữ pháp và cấu trúc câu để huấn luyện trợ lý ảo trí tuệ nhân tạo (AI) hiểu ngôn ngữ con người." 
                            : "Build grammar and syntactic models to train AI virtual assistants to understand human language."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎮",
                        Title = isVN ? "Viết kịch bản trò chơi" : "Game Scripting",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_grammar_8_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng cấu trúc câu điều kiện và phân nhánh hội thoại để thiết kế cốt truyện phi tuyến tính trong game nhập vai." 
                            : "Use conditional sentence structures and branching dialogues to design non-linear storylines in RPG games."
                    }};

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for GrammarTool: {Err}", ex.Message);
            }
        }

        private void LoadDataFromDb()
        {
            try
            {
                using var db = new QASmartClass.Data.AppDbContext();
                _dbTenses = db.GrammarTenses.ToList();
                _dbQuestions = db.GrammarQuestions.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading grammar data: " + ex.Message);
            }
        }

        private GramQ MapQuestion(QASmartClass.Data.GrammarQuestion q)
        {
            try
            {
                var answers = !string.IsNullOrEmpty(q.AnswersJson)
                    ? System.Text.Json.JsonSerializer.Deserialize<string[]>(q.AnswersJson)
                    : Array.Empty<string>();
                var options = !string.IsNullOrEmpty(q.OptionsJson) 
                    ? System.Text.Json.JsonSerializer.Deserialize<string[]>(q.OptionsJson) 
                    : null;
                return new GramQ(q.QuestionText, q.Hint, answers ?? Array.Empty<string>(), options);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error("Lỗi giải mã JSON câu hỏi ID {Id} trong DB: {Err}", q.Id, ex.Message);
                // Fallback an toàn để không làm sập ứng dụng chính
                return new GramQ(q.QuestionText, q.Hint, new[] { "error" }, null);
            }
        }

        // ═══ RESET ═══
        private void ResetAll_Click(object sender, MouseButtonEventArgs e)
        {
            ResetActiveQuizzes();
        }



        private void ResetActiveQuizzes()
        {
            _gActive = false;
            _mcActive = false;
            
            // Stop and clear all board timers
            foreach (var board in _activeBoards)
            {
                if (board.BoardTimer != null)
                {
                    board.BoardTimer.Stop();
                }
                if (board.TransitionTimer != null)
                {
                    board.TransitionTimer.Stop();
                }
            }
            _activeBoards.Clear();

            _timerPausedByHelp = false;

            if (btnGramStart != null)
            {
                btnGramStart.Visibility = Visibility.Visible;
                btnGramStart.Content = "✏️ Bắt đầu";
            }
            if (btnMcGramStart != null)
            {
                btnMcGramStart.Visibility = Visibility.Visible;
                btnMcGramStart.Content = "✏️ Bắt đầu";
            }

            if (fillBlankSetupArea != null) fillBlankSetupArea.Visibility = Visibility.Visible;
            if (fillBlankPlayGrid != null) fillBlankPlayGrid.Visibility = Visibility.Collapsed;
            if (fillBlankResultCard != null) fillBlankResultCard.Visibility = Visibility.Collapsed;
            
            if (mcSetupArea != null) mcSetupArea.Visibility = Visibility.Visible;
            if (mcPlayGrid != null) mcPlayGrid.Visibility = Visibility.Collapsed;
            if (mcResultCard != null) mcResultCard.Visibility = Visibility.Collapsed;

            if (fillBlankP1KeyboardContainer != null) fillBlankP1KeyboardContainer.Children.Clear();
            if (fillBlankP2KeyboardContainer != null) fillBlankP2KeyboardContainer.Children.Clear();
            if (fillBlankP3KeyboardContainer != null) fillBlankP3KeyboardContainer.Children.Clear();
        }

        private void PopulateTenseFilters()
        {
            if (cbGramTenseFilter == null || cbMcTenseFilter == null) return;
            
            cbGramTenseFilter.Items.Clear();
            cbMcTenseFilter.Items.Clear();

            cbGramTenseFilter.Items.Add("Tất cả các thì");
            cbMcTenseFilter.Items.Add("Tất cả các thì");

            var list = _dbTenses.Count > 0 
                ? _dbTenses.Select(t => t.NameVi).ToList()
                : Tenses.Select(t => t.NameVi).ToList();

            foreach (var tenseVi in list)
            {
                cbGramTenseFilter.Items.Add(tenseVi);
                cbMcTenseFilter.Items.Add(tenseVi);
            }

            cbGramTenseFilter.SelectedIndex = 0;
            cbMcTenseFilter.SelectedIndex = 0;
        }

        private void GramPlayerCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fillBlankNamesPanel == null || txtGramPlayer3 == null) return;
            var selectedItem = cbGramPlayerCount.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;
            int count = int.Parse(selectedItem.Tag.ToString()!);
            
            if (count > 1)
            {
                fillBlankNamesPanel.Visibility = Visibility.Visible;
                txtGramPlayer3.Visibility = count == 3 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                fillBlankNamesPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void McPlayerCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mcNamesPanel == null || txtMcPlayer3 == null) return;
            var selectedItem = cbMcPlayerCount.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;
            int count = int.Parse(selectedItem.Tag.ToString()!);
            
            if (count > 1)
            {
                mcNamesPanel.Visibility = Visibility.Visible;
                txtMcPlayer3.Visibility = count == 3 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                mcNamesPanel.Visibility = Visibility.Collapsed;
            }
        }

        private List<GramQ> PrepareQuizQuestions(string category, string tenseFilter, int count)
        {
            var rawList = _dbQuestions.Count > 0 
                ? _dbQuestions.Where(x => x.Category == category).Select(MapQuestion).ToList()
                : (category == "FillBlank" ? FillBlankQuestions.ToList() : TenseQuestions.ToList());

            if (!string.IsNullOrEmpty(tenseFilter) && tenseFilter != "Tất cả các thì")
            {
                var filtered = category == "FillBlank"
                    ? rawList.Where(q => q.Hint.Contains(tenseFilter)).ToList()
                    : rawList.Where(q => q.Answers.Length > 0 && q.Answers[0] == tenseFilter).ToList();

                if (filtered.Count == 0)
                {
                    MessageBox.Show($"Không tìm thấy câu hỏi mẫu nào cho thì \"{tenseFilter}\". Hệ thống sẽ trộn ngẫu nhiên tất cả các thì.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    rawList = filtered;
                }
            }

            return rawList.Count > 0
                ? rawList.OrderBy(_ => _rng.Next()).Take(count).ToList()
                : new List<GramQ>();
        }

        private void RenderMultiplayerLeaderboard(Panel container)
        {
            if (container == null) return;
            container.Children.Clear();

            var list = new List<(string Name, int Score)>();
            for (int i = 0; i < _selectedPlayerCount; i++)
            {
                string displayName = _playerNames[i];
                bool isDuplicate = false;
                for (int j = 0; j < _selectedPlayerCount; j++)
                {
                    if (i != j && _playerNames[i] == _playerNames[j])
                    {
                        isDuplicate = true;
                        break;
                    }
                }
                if (isDuplicate)
                {
                    displayName = $"{_playerNames[i]} (HS {i + 1})";
                }
                list.Add((displayName, _playerScores[i]));
            }

            var sorted = list.OrderByDescending(p => p.Score).ToList();

            var title = new TextBlock
            {
                Text = "🏆 BẢNG XẾP HẠNG CHUNG CUỘC",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            container.Children.Add(title);

            string[] ranks = { "🥇 Hạng 1", "🥈 Hạng 2", "🥉 Hạng 3" };
            string[] colors = { "#FFF9C4", "#F5F5F5", "#FFE0B2" }; // Gold, Silver, Bronze background
            string[] borderColors = { "#FBC02D", "#BDBDBD", "#F57C00" };

            for (int i = 0; i < sorted.Count; i++)
            {
                var p = sorted[i];
                var border = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i])),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderColors[i])),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 4, 0, 4),
                    MaxWidth = 450,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

                var txtRank = new TextBlock { Text = ranks[i], FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(33,33,33)) };
                Grid.SetColumn(txtRank, 0);
                grid.Children.Add(txtRank);

                var txtName = new TextBlock { Text = p.Name, FontSize = 14, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Left, Foreground = new SolidColorBrush(Color.FromRgb(33,33,33)), Margin = new Thickness(10, 0, 10, 0) };
                Grid.SetColumn(txtName, 1);
                grid.Children.Add(txtName);

                var txtScore = new TextBlock { Text = $"{p.Score} điểm", FontSize = 14, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right, Foreground = new SolidColorBrush(Color.FromRgb(21,101,192)) };
                Grid.SetColumn(txtScore, 2);
                grid.Children.Add(txtScore);

                border.Child = grid;
                container.Children.Add(border);
            }
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            if (field.StartsWith("=") || field.StartsWith("+") || field.StartsWith("-") || field.StartsWith("@"))
            {
                field = "'" + field;
            }
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                field = "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }



        private static (string Purpose, string Usage) GetTenseMetadata(string name)
        {
            string n = name.ToLowerInvariant().Replace(".", "").Trim();
            if (n.Contains("past perfect cont"))
                return ("Diễn tả hành động bắt đầu trước một hành động khác trong quá khứ và kéo dài liên tục đến thời điểm đó.", 
                        "Nhấn mạnh khoảng thời gian kéo dài liên tục của hành động quá khứ trước khi một sự kiện khác xảy ra.");
            if (n.Contains("past perfect"))
                return ("Diễn tả một hành động đã xảy ra và hoàn thành trước một hành động khác hoặc một mốc thời điểm trong quá khứ.", 
                        "Thiết lập trình tự thời gian trước-sau của các sự kiện đã kết thúc hoàn toàn trong quá khứ.");
            if (n.Contains("past continuous"))
                return ("Diễn tả hành động đang xảy ra tại một thời điểm cụ thể xác định trong quá khứ.", 
                        "Tạo ngữ cảnh nền cho một hành động khác xen vào, hoặc mô tả hai hành động diễn ra song song cùng lúc.");
            if (n.Contains("past simple") || n.Contains("simple past"))
                return ("Diễn tả hành động đã xảy ra và kết thúc hoàn toàn trong quá khứ, có thời gian xác định rõ ràng.", 
                        "Kể lại chuỗi sự kiện, hành động lịch sử hoặc thói quen, trạng thái đã chấm dứt trong quá khứ.");
            
            if (n.Contains("present perfect cont"))
                return ("Diễn tả hành động bắt đầu ở quá khứ, kéo dài liên tục đến hiện tại và có thể tiếp tục ở tương lai.", 
                        "Nhấn mạnh tính liên tục, khoảng thời gian kéo dài của hành động và để lại dấu vết/kết quả rõ rệt ở hiện tại.");
            if (n.Contains("present perfect"))
                return ("Diễn tả hành động đã hoàn thành trong quá khứ nhưng kết quả hoặc ảnh hưởng vẫn liên quan đến hiện tại.", 
                        "Kết nối quá khứ với hiện tại, nhấn mạnh trải nghiệm tích lũy, số lượng hoặc kết quả đạt được tính đến nay.");
            if (n.Contains("present continuous"))
                return ("Diễn tả hành động đang xảy ra ngay tại thời điểm nói hoặc xung quanh thời điểm nói.", 
                        "Nhấn mạnh tính tạm thời, sự tiếp diễn của hành động hoặc một xu hướng đang thay đổi trong xã hội.");
            if (n.Contains("present simple") || n.Contains("simple present"))
                return ("Diễn tả sự thật hiển nhiên, chân lý, thói quen hoặc hành động lặp đi lặp lại ở hiện tại.", 
                        "Xác lập các quy luật, sự kiện lịch trình cố định hoặc trạng thái cảm xúc, nhận thức thời điểm hiện tại.");
            
            if (n.Contains("future simple") || n.Contains("simple future"))
                return ("Diễn tả hành động sẽ xảy ra trong tương lai được quyết định ngay lúc nói, hoặc đưa ra lời dự đoán.", 
                        "Thể hiện ý định bộc phát, lời hứa, lời đe dọa hoặc nhận định chủ quan về tương lai không có căn cứ chắc chắn.");
            if (n.Contains("near future"))
                return ("Diễn tả một kế hoạch, dự định cụ thể trong tương lai hoặc một dự đoán có căn cứ xác thực ở hiện tại.", 
                        "Được sử dụng khi người nói muốn nhấn mạnh tính chắc chắn và sự chuẩn bị trước của hành động sắp xảy ra.");
            if (n.Contains("future continuous"))
                return ("Diễn tả hành động đang xảy ra tại một thời điểm cụ thể hoặc trong một khoảng thời gian ở tương lai.", 
                        "Nhấn mạnh tiến trình, sự liên tục của hành động sẽ diễn ra vào thời điểm xác định sắp tới.");
            if (n.Contains("future perfect cont"))
                return ("Diễn tả hành động bắt đầu ở quá khứ/hiện tại và kéo dài liên tục đến một thời điểm trong tương lai.", 
                        "Nhấn mạnh độ dài, tính liên tục của hành động tính đến một mốc thời gian xác định ở tương lai.");
            if (n.Contains("future perfect"))
                return ("Diễn tả hành động sẽ hoàn thành trước một thời điểm hoặc một hành động khác trong tương lai.", 
                        "Nhấn mạnh kết quả hoàn thành, thời hạn kết thúc của hành động tại mốc tương lai xác định.");

            return ("Diễn tả công thức ngữ pháp của thì.", "Sử dụng chính xác trong các ngữ cảnh phù hợp.");
        }

        private void RenderProgressIndicator(Panel panel, int activeIdx, List<UserQuizAttempt> attempts, int totalCount)
        {
            if (panel == null) return;
            panel.Children.Clear();

            for (int i = 0; i < totalCount; i++)
            {
                var border = new Border
                {
                    Width = 24, Height = 24, CornerRadius = new CornerRadius(12),
                    Margin = new Thickness(3, 0, 3, 0),
                    BorderThickness = new Thickness(1.5),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var txt = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Xác định trạng thái màu sắc
                var attempt = attempts.FirstOrDefault(a => a.QuestionIndex == (i + 1));
                if (attempt != null)
                {
                    if (attempt.IsCorrect)
                    {
                        border.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)); // Success Green
                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(129, 199, 132));
                        txt.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    }
                    else
                    {
                        border.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238)); // Error Red
                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(229, 115, 115));
                        txt.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                    }
                }
                else if (i == activeIdx)
                {
                    border.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253)); // Active Blue
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(21, 101, 192));
                    txt.Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192));
                }
                else
                {
                    border.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // Neutral Gray
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                    txt.Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158));
                }

                border.Child = txt;
                panel.Children.Add(border);
            }
        }

        private void ShowQuizSummary(bool isMc)
        {
            double timeSpent = (DateTime.Now - _quizStartTime).TotalSeconds;
            
            // Set up scores for leaderboard
            for (int i = 0; i < _selectedPlayerCount; i++)
            {
                var board = _activeBoards.FirstOrDefault(b => b.PlayerIndex == i);
                _playerScores[i] = board != null ? board.Score : 0;
            }

            if (isMc)
            {
                mcPlayGrid.Visibility = Visibility.Collapsed;
                mcResultCard.Visibility = Visibility.Visible;
                btnMcGramStart.Visibility = Visibility.Visible;
                btnMcGramStart.Content = "🔄 Chơi lại";
                
                if (_selectedPlayerCount > 1)
                {
                    mcSinglePlayerScorePanel.Visibility = Visibility.Collapsed;
                    mcMultiplayerScorePanel.Visibility = Visibility.Visible;
                    RenderMultiplayerLeaderboard(mcMultiplayerScorePanel);
                }
                else
                {
                    mcSinglePlayerScorePanel.Visibility = Visibility.Visible;
                    mcMultiplayerScorePanel.Visibility = Visibility.Collapsed;
                    int correct = _playerScores[0];
                    int total = _activeBoards[0].Questions.Count;
                    double accuracy = total > 0 ? (double)correct / total * 100.0 : 0;
                    
                    txtMcFbScore.Text = $"{correct}/{total}";
                    txtMcFbTime.Text = $"{(int)timeSpent}s";
                    txtMcFbAccuracy.Text = $"{accuracy:F0}%";
                }

                // Gather all attempts from all boards
                var allAttempts = _activeBoards.SelectMany(b => b.Attempts).OrderBy(a => a.QuestionIndex).ToList();
                RenderMistakesList(mcMistakesList, allAttempts);
                
                SaveQuizHistory("MultipleChoice", _playerScores[0], _activeBoards[0].Questions.Count);
            }
            else
            {
                fillBlankPlayGrid.Visibility = Visibility.Collapsed;
                fillBlankResultCard.Visibility = Visibility.Visible;
                btnGramStart.Visibility = Visibility.Visible;
                btnGramStart.Content = "🔄 Chơi lại";
                
                if (_selectedPlayerCount > 1)
                {
                    fillBlankSinglePlayerScorePanel.Visibility = Visibility.Collapsed;
                    fillBlankMultiplayerScorePanel.Visibility = Visibility.Visible;
                    RenderMultiplayerLeaderboard(fillBlankMultiplayerScorePanel);
                }
                else
                {
                    fillBlankSinglePlayerScorePanel.Visibility = Visibility.Visible;
                    fillBlankMultiplayerScorePanel.Visibility = Visibility.Collapsed;
                    int correct = _playerScores[0];
                    int total = _activeBoards[0].Questions.Count;
                    double accuracy = total > 0 ? (double)correct / total * 100.0 : 0;
                    
                    txtFbScore.Text = $"{correct}/{total}";
                    txtFbTime.Text = $"{(int)timeSpent}s";
                    txtFbAccuracy.Text = $"{accuracy:F0}%";
                }

                var allAttempts = _activeBoards.SelectMany(b => b.Attempts).OrderBy(a => a.QuestionIndex).ToList();
                RenderMistakesList(fillBlankMistakesList, allAttempts);
                
                SaveQuizHistory("FillBlank", _playerScores[0], _activeBoards[0].Questions.Count);
            }
        }

        private void RenderMistakesList(Panel containerPanel, List<UserQuizAttempt> attempts)
        {
            if (containerPanel == null) return;
            containerPanel.Children.Clear();

            var mistakes = attempts.Where(a => !a.IsCorrect).ToList();
            if (mistakes.Count == 0)
            {
                containerPanel.Children.Add(new TextBlock
                {
                    Text = "🎉 Tuyệt vời! Bạn không làm sai câu nào.",
                    FontSize = 13, FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                    Margin = new Thickness(0, 4, 0, 4)
                });
                return;
            }

            foreach (var m in mistakes)
            {
                var border = new Border
                {
                    Background = Brushes.White,
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(238, 238, 238)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 0, 0, 6)
                };

                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = $"Câu {m.QuestionIndex}: {m.QuestionText}",
                    FontSize = 13, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                    TextWrapping = TextWrapping.Wrap
                });

                var detailsSp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
                if (!string.IsNullOrEmpty(m.PlayerName))
                {
                    detailsSp.Children.Add(new TextBlock
                    {
                        Text = $"({m.PlayerName}) ",
                        FontSize = 12, FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(92, 107, 115)),
                        Margin = new Thickness(0, 0, 8, 0)
                    });
                }
                detailsSp.Children.Add(new TextBlock
                {
                    Text = $"Bạn chọn: {m.UserAnswer}",
                    FontSize = 12, FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                    Margin = new Thickness(0, 0, 16, 0)
                });
                detailsSp.Children.Add(new TextBlock
                {
                    Text = $"Đáp án đúng: {m.CorrectAnswer}",
                    FontSize = 12, FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50))
                });

                sp.Children.Add(detailsSp);
                border.Child = sp;
                containerPanel.Children.Add(border);
            }
        }

        private void BuildGrammar()
        {
            if (grammarPanel == null) return;
            grammarPanel.Children.Clear();

            _orderedTenses.Clear();
            var rawTenses = new List<TenseData>();
            if (_dbTenses.Count > 0)
            {
                foreach (var t in _dbTenses)
                {
                    var meta = GetTenseMetadata(t.Name);
                    rawTenses.Add(new TenseData
                    {
                        Name = t.Name,
                        NameVi = t.NameVi,
                        Structure = t.Structure,
                        Example = t.Example,
                        Signal = t.Signal,
                        ColorHex = t.Color,
                        SectionId = t.Name.Replace(" ", "_").Replace(".", "").ToLowerInvariant(),
                        Purpose = meta.Purpose,
                        Usage = meta.Usage
                    });
                }
            }
            else
            {
                foreach (var (name, nameVi, structure, example, signal, colorHex) in Tenses)
                {
                    var meta = GetTenseMetadata(name);
                    rawTenses.Add(new TenseData
                    {
                        Name = name,
                        NameVi = nameVi,
                        Structure = structure,
                        Example = example,
                        Signal = signal,
                        ColorHex = colorHex,
                        SectionId = name.Replace(" ", "_").Replace(".", "").ToLowerInvariant(),
                        Purpose = meta.Purpose,
                        Usage = meta.Usage
                    });
                }
            }

            // Sort chronologically
            _orderedTenses = rawTenses.OrderBy(t => GetChronologicalOrder(t.Name)).ToList();

            // Default to Simple Present if it exists, otherwise the first index
            int simplePresentIdx = _orderedTenses.FindIndex(t => t.Name.ToLowerInvariant().Contains("simple present"));
            _selectedTenseIndex = simplePresentIdx >= 0 ? simplePresentIdx : 0;

            ApplySectionFocusFilter();
        }

        private int GetChronologicalOrder(string name)
        {
            string n = name.ToLowerInvariant().Replace(".", "").Trim();
            if (n.Contains("past perfect cont")) return 1;
            if (n.Contains("past perfect")) return 2;
            if (n.Contains("past continuous")) return 3;
            if (n.Contains("past simple") || n.Contains("simple past")) return 4;
            
            if (n.Contains("present perfect cont")) return 5;
            if (n.Contains("present perfect")) return 6;
            if (n.Contains("present continuous")) return 7;
            if (n.Contains("present simple") || n.Contains("simple present")) return 8;
            
            if (n.Contains("future simple") || n.Contains("simple future")) return 9;
            if (n.Contains("near future")) return 10;
            if (n.Contains("future continuous")) return 11;
            if (n.Contains("future perfect cont")) return 13;
            if (n.Contains("future perfect")) return 12;
            
            return 99;
        }

        private void RenderTimeline()
        {
            if (timelineItemsContainer == null) return;
            timelineItemsContainer.Children.Clear();
            timelineItemsContainer.Columns = _orderedTenses.Count;

            for (int i = 0; i < _orderedTenses.Count; i++)
            {
                int index = i;
                var tense = _orderedTenses[i];
                var color = (Color)ColorConverter.ConvertFromString(tense.ColorHex);
                
                var itemGrid = new Grid { Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 0, 5) };
                
                // Vẽ đường nối ngang responsive nằm sau vòng tròn số
                var lineGrid = new Grid { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 25, 0, 0), Height = 4 };
                lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                if (index > 0)
                {
                    var leftLine = new Border { Background = new SolidColorBrush(Color.FromRgb(207, 216, 220)) };
                    Grid.SetColumn(leftLine, 0);
                    lineGrid.Children.Add(leftLine);
                }
                if (index < _orderedTenses.Count - 1)
                {
                    var rightLine = new Border { Background = new SolidColorBrush(Color.FromRgb(207, 216, 220)) };
                    Grid.SetColumn(rightLine, 1);
                    lineGrid.Children.Add(rightLine);
                }
                itemGrid.Children.Add(lineGrid);

                var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                
                var circleBorder = new Border
                {
                    Width = 54, Height = 54, CornerRadius = new CornerRadius(27),
                    BorderThickness = new Thickness(3),
                    BorderBrush = new SolidColorBrush(color),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                
                var txtNum = new TextBlock
                {
                    Text = (index + 1).ToString(),
                    FontSize = 22, FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                if (index == _selectedTenseIndex)
                {
                    circleBorder.Background = new SolidColorBrush(color);
                    txtNum.Foreground = Brushes.White;
                    circleBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = color, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7
                    };
                }
                else
                {
                    circleBorder.Background = Brushes.White;
                    txtNum.Foreground = new SolidColorBrush(color);
                    circleBorder.Effect = null;
                }
                
                circleBorder.Child = txtNum;
                sp.Children.Add(circleBorder);
                
                var txtName = new TextBlock
                {
                    Text = tense.Name,
                    FontSize = 13,
                    FontWeight = index == _selectedTenseIndex ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = index == _selectedTenseIndex ? new SolidColorBrush(color) : new SolidColorBrush(Color.FromRgb(97, 97, 97)),
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Width = 135,
                    Margin = new Thickness(0, 6, 0, 0)
                };
                sp.Children.Add(txtName);
                
                // Nhãn phụ tiếng Việt hiển thị song song ngay bên dưới
                var txtNameVi = new TextBlock
                {
                    Text = tense.NameVi,
                    FontSize = 10,
                    FontWeight = index == _selectedTenseIndex ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = index == _selectedTenseIndex ? new SolidColorBrush(color) : new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Width = 135,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                sp.Children.Add(txtNameVi);
                
                itemGrid.Children.Add(sp);
                itemGrid.MouseDown += (s, e) =>
                {
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        SelectTense(index);
                    }
                };
                
                timelineItemsContainer.Children.Add(itemGrid);
            }
        }

        private void SelectTense(int index)
        {
            if (index < 0 || index >= _orderedTenses.Count) return;
            _selectedTenseIndex = index;
            RenderTimeline();
            RenderTenseDetail(_orderedTenses[index]);
        }

        private void RenderTenseDetail(TenseData tense)
        {
            if (grammarPanel == null) return;
            grammarPanel.Children.Clear();

            var color = (Color)ColorConverter.ConvertFromString(tense.ColorHex);
            
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(24, 16, 24, 16),
                Margin = new Thickness(0, 10, 0, 10),
                BorderBrush = new SolidColorBrush(color),
                BorderThickness = new Thickness(5, 0, 0, 0),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black, BlurRadius = 15, ShadowDepth = 2, Opacity = 0.1
                }
            };
            
            var sp = new StackPanel();
            
            // 1. Tense Name (FontSize 30)
            var titleText = new TextBlock
            {
                Text = $"📌 {tense.Name} — {tense.NameVi}",
                FontSize = 30,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(color),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            };
            sp.Children.Add(titleText);
            
            // 2. Grammar Structure (FontSize 28)
            var structBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(25, color.R, color.G, color.B)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20, 12, 20, 12),
                Margin = new Thickness(0, 6, 0, 12)
            };
            
            var structText = new TextBlock
            {
                Text = $"🔧 Cấu trúc: {tense.Structure}",
                FontSize = 28,
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                TextWrapping = TextWrapping.Wrap
            };
            structBorder.Child = structText;
            sp.Children.Add(structBorder);

            // 2.5 Grid 2 cột: Mục đích (Trái) & Ý nghĩa (Phải)
            var infoGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            // Cột trái: Mục đích
            var purposeSp = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
            purposeSp.Children.Add(new TextBlock
            {
                Text = "🎯 Mục đích:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(color),
                Margin = new Thickness(0, 0, 0, 4)
            });
            purposeSp.Children.Add(new TextBlock
            {
                Text = tense.Purpose,
                FontSize = 14.5,
                Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            });
            Grid.SetColumn(purposeSp, 0);
            infoGrid.Children.Add(purposeSp);
            
            // Cột phải: Ý nghĩa / Cách dùng
            var usageSp = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
            usageSp.Children.Add(new TextBlock
            {
                Text = "📖 Ý nghĩa & Cách dùng:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(color),
                Margin = new Thickness(0, 0, 0, 4)
            });
            usageSp.Children.Add(new TextBlock
            {
                Text = tense.Usage,
                FontSize = 14.5,
                Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            });
            Grid.SetColumn(usageSp, 1);
            infoGrid.Children.Add(usageSp);
            
            sp.Children.Add(infoGrid);
            
            // 3. Examples (FontSize 24)
            var exampleLabel = new TextBlock
            {
                Text = "💬 Ví dụ minh họa:",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                Margin = new Thickness(0, 6, 0, 4)
            };
            sp.Children.Add(exampleLabel);
            
            var exampleText = new TextBlock
            {
                Text = $"\"{tense.Example}\"",
                FontSize = 24,
                FontStyle = FontStyles.Italic,
                Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(15, 0, 0, 12)
            };
            sp.Children.Add(exampleText);
            
            // 4. Signal words (FontSize 18)
            var signalText = new TextBlock
            {
                Text = $"🔑 Dấu hiệu nhận biết: {tense.Signal}",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            };
            sp.Children.Add(signalText);
            
            card.Child = sp;
            
            var wrapper = WrapWithSectionToolbar(card, "grammar", tense.SectionId, $"{tense.Name} — {tense.NameVi}");
            wrapper.Tag = tense.SectionId;
            
            grammarPanel.Children.Add(wrapper);
        }

        // ═══ FOCUS / UNFOCUS BỘ LỌC THẺ NGỮ PHÁP ═══
        public void FilterFocusedSection(string sectionId)
        {
            _focusedSectionId = sectionId;
            ApplySectionFocusFilter();
        }

        public void ResetFocus()
        {
            _focusedSectionId = null;
            ApplySectionFocusFilter();
        }

        private void ApplySectionFocusFilter()
        {
            if (sideMenu != null)
            {
                if (_focusedSectionId != null)
                {
                    sideMenu.SelectedIndex = 0; // Chọn "Bảng ngữ pháp"
                    for (int i = 0; i < sideMenu.Items.Count; i++)
                    {
                        if (i != 1 && sideMenu.Items[i] is ListBoxItem item)
                        {
                            item.Visibility = Visibility.Collapsed;
                        }
                    }
                    if (timelineGrid != null)
                    {
                        timelineGrid.Visibility = Visibility.Collapsed;
                    }
                    int idx = _orderedTenses.FindIndex(t => t.SectionId == _focusedSectionId);
                    if (idx >= 0)
                    {
                        _selectedTenseIndex = idx;
                    }
                }
                else
                {
                    for (int i = 0; i < sideMenu.Items.Count; i++)
                    {
                        if (sideMenu.Items[i] is ListBoxItem item)
                        {
                            item.Visibility = Visibility.Visible;
                        }
                    }
                    if (timelineGrid != null)
                    {
                        timelineGrid.Visibility = Visibility.Visible;
                    }
                }
            }

            if (_orderedTenses.Count > 0)
            {
                if (_selectedTenseIndex < 0 || _selectedTenseIndex >= _orderedTenses.Count)
                {
                    _selectedTenseIndex = 0;
                }
                RenderTimeline();
                RenderTenseDetail(_orderedTenses[_selectedTenseIndex]);
            }
        }

        // ═══ MULTIPLAYER SPLIT-SCREEN LOGIC ═══
        private void InitializePlayerColumns(bool isMc)
        {
            if (isMc)
            {
                if (_selectedPlayerCount == 1)
                {
                    colMcPlayer1.Width = new GridLength(1, GridUnitType.Star);
                    colMcSplit1.Width = new GridLength(0);
                    colMcPlayer2.Width = new GridLength(0);
                    colMcSplit2.Width = new GridLength(0);
                    colMcPlayer3.Width = new GridLength(0);
                    
                    borderMcPlayer1.Visibility = Visibility.Visible;
                    borderMcPlayer2.Visibility = Visibility.Collapsed;
                    borderMcPlayer3.Visibility = Visibility.Collapsed;
                    splitMc1.Visibility = Visibility.Collapsed;
                    splitMc2.Visibility = Visibility.Collapsed;
                }
                else if (_selectedPlayerCount == 2)
                {
                    colMcPlayer1.Width = new GridLength(1, GridUnitType.Star);
                    colMcSplit1.Width = GridLength.Auto;
                    colMcPlayer2.Width = new GridLength(1, GridUnitType.Star);
                    colMcSplit2.Width = new GridLength(0);
                    colMcPlayer3.Width = new GridLength(0);
                    
                    borderMcPlayer1.Visibility = Visibility.Visible;
                    borderMcPlayer2.Visibility = Visibility.Visible;
                    borderMcPlayer3.Visibility = Visibility.Collapsed;
                    splitMc1.Visibility = Visibility.Visible;
                    splitMc2.Visibility = Visibility.Collapsed;
                }
                else // 3 players
                {
                    colMcPlayer1.Width = new GridLength(1, GridUnitType.Star);
                    colMcSplit1.Width = GridLength.Auto;
                    colMcPlayer2.Width = new GridLength(1, GridUnitType.Star);
                    colMcSplit2.Width = GridLength.Auto;
                    colMcPlayer3.Width = new GridLength(1, GridUnitType.Star);
                    
                    borderMcPlayer1.Visibility = Visibility.Visible;
                    borderMcPlayer2.Visibility = Visibility.Visible;
                    borderMcPlayer3.Visibility = Visibility.Visible;
                    splitMc1.Visibility = Visibility.Visible;
                    splitMc2.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (_selectedPlayerCount == 1)
                {
                    colGramPlayer1.Width = new GridLength(1, GridUnitType.Star);
                    colGramSplit1.Width = new GridLength(0);
                    colGramPlayer2.Width = new GridLength(0);
                    colGramSplit2.Width = new GridLength(0);
                    colGramPlayer3.Width = new GridLength(0);
                    
                    borderGramPlayer1.Visibility = Visibility.Visible;
                    borderGramPlayer2.Visibility = Visibility.Collapsed;
                    borderGramPlayer3.Visibility = Visibility.Collapsed;
                    splitGram1.Visibility = Visibility.Collapsed;
                    splitGram2.Visibility = Visibility.Collapsed;
                }
                else if (_selectedPlayerCount == 2)
                {
                    colGramPlayer1.Width = new GridLength(1, GridUnitType.Star);
                    colGramSplit1.Width = GridLength.Auto;
                    colGramPlayer2.Width = new GridLength(1, GridUnitType.Star);
                    colGramSplit2.Width = new GridLength(0);
                    colGramPlayer3.Width = new GridLength(0);
                    
                    borderGramPlayer1.Visibility = Visibility.Visible;
                    borderGramPlayer2.Visibility = Visibility.Visible;
                    borderGramPlayer3.Visibility = Visibility.Collapsed;
                    splitGram1.Visibility = Visibility.Visible;
                    splitGram2.Visibility = Visibility.Collapsed;
                }
                else // 3 players
                {
                    colGramPlayer1.Width = new GridLength(1, GridUnitType.Star);
                    colGramSplit1.Width = GridLength.Auto;
                    colGramPlayer2.Width = new GridLength(1, GridUnitType.Star);
                    colGramSplit2.Width = GridLength.Auto;
                    colGramPlayer3.Width = new GridLength(1, GridUnitType.Star);
                    
                    borderGramPlayer1.Visibility = Visibility.Visible;
                    borderGramPlayer2.Visibility = Visibility.Visible;
                    borderGramPlayer3.Visibility = Visibility.Visible;
                    splitGram1.Visibility = Visibility.Visible;
                    splitGram2.Visibility = Visibility.Visible;
                }
            }
        }

        // ═══ FILL-IN-BLANK QUIZ ═══
        private void GramQuizStart_Click(object s, RoutedEventArgs e)
        {
            if (fillBlankResultCard.Visibility == Visibility.Visible || fillBlankPlayGrid.Visibility == Visibility.Visible)
            {
                ResetActiveQuizzes();
                return;
            }

            string tenseFilter = cbGramTenseFilter.SelectedItem?.ToString() ?? "Tất cả các thì";
            
            var countItem = cbGramQuestionCount.SelectedItem as ComboBoxItem;
            int count = countItem != null ? int.Parse(countItem.Tag.ToString()!) : 10;

            var playerItem = cbGramPlayerCount.SelectedItem as ComboBoxItem;
            _selectedPlayerCount = playerItem != null ? int.Parse(playerItem.Tag.ToString()!) : 1;

            if (_selectedPlayerCount > 1)
            {
                _playerNames[0] = string.IsNullOrWhiteSpace(txtGramPlayer1.Text) ? "Học sinh 1" : txtGramPlayer1.Text.Trim();
                _playerNames[1] = string.IsNullOrWhiteSpace(txtGramPlayer2.Text) ? "Học sinh 2" : txtGramPlayer2.Text.Trim();
                if (_selectedPlayerCount == 3)
                {
                    _playerNames[2] = string.IsNullOrWhiteSpace(txtGramPlayer3.Text) ? "Học sinh 3" : txtGramPlayer3.Text.Trim();
                }
            }
            else
            {
                _playerNames[0] = "Học sinh";
            }

            _gQuiz = PrepareQuizQuestions("FillBlank", tenseFilter, count);
            if (_gQuiz.Count == 0) return;

            _gActive = true;
            _activeBoards.Clear();
            _quizStartTime = DateTime.Now;
            _currentAttempts.Clear();

            fillBlankSetupArea.Visibility = Visibility.Collapsed;
            fillBlankPlayGrid.Visibility = Visibility.Visible;
            fillBlankResultCard.Visibility = Visibility.Collapsed;
            btnGramStart.Visibility = Visibility.Collapsed;

            InitializePlayerColumns(isMc: false);

            // Create boards
            for (int i = 0; i < _selectedPlayerCount; i++)
            {
                var board = new PlayerBoard
                {
                    PlayerIndex = i,
                    Name = _playerNames[i],
                    Score = 0,
                    QuestionIndex = 0,
                    Questions = _gQuiz.ToList(),
                    IsFinished = false
                };

                if (i == 0)
                {
                    board.BoardBorder = borderGramPlayer1;
                    board.HeaderText = txtGramP1Header;
                    board.ProgressPanel = fillBlankP1ProgressPanel;
                    board.SentenceText = txtGramP1Sentence;
                    board.HintText = txtGramP1Hint;
                    board.AnswerTextBox = txtGramP1Answer;
                    board.FeedbackText = txtGramP1Feedback;
                    board.KeyboardContainer = fillBlankP1KeyboardContainer;
                }
                else if (i == 1)
                {
                    board.BoardBorder = borderGramPlayer2;
                    board.HeaderText = txtGramP2Header;
                    board.ProgressPanel = fillBlankP2ProgressPanel;
                    board.SentenceText = txtGramP2Sentence;
                    board.HintText = txtGramP2Hint;
                    board.AnswerTextBox = txtGramP2Answer;
                    board.FeedbackText = txtGramP2Feedback;
                    board.KeyboardContainer = fillBlankP2KeyboardContainer;
                }
                else if (i == 2)
                {
                    board.BoardBorder = borderGramPlayer3;
                    board.HeaderText = txtGramP3Header;
                    board.ProgressPanel = fillBlankP3ProgressPanel;
                    board.SentenceText = txtGramP3Sentence;
                    board.HintText = txtGramP3Hint;
                    board.AnswerTextBox = txtGramP3Answer;
                    board.FeedbackText = txtGramP3Feedback;
                    board.KeyboardContainer = fillBlankP3KeyboardContainer;
                }

                _activeBoards.Add(board);
                ShowPlayerGramQ(board);
            }
        }

        private void ShowPlayerGramQ(PlayerBoard board)
        {
            if (board.QuestionIndex >= board.Questions.Count)
            {
                board.IsFinished = true;
                if (board.AnswerTextBox != null)
                {
                    board.AnswerTextBox.IsEnabled = false;
                    board.AnswerTextBox.Text = "Hoàn thành!";
                }
                if (board.SentenceText != null)
                {
                    board.SentenceText.Text = "🎉 Bạn đã hoàn thành tất cả câu hỏi!";
                }
                if (board.HintText != null)
                {
                    board.HintText.Text = "";
                }
                if (board.FeedbackText != null)
                {
                    board.FeedbackText.Text = "";
                }
                if (board.KeyboardContainer != null)
                {
                    board.KeyboardContainer.Children.Clear();
                }
                
                // Check if all players finished
                if (_activeBoards.All(b => b.IsFinished))
                {
                    _gActive = false;
                    ShowQuizSummary(false);
                }
                return;
            }

            RenderProgressIndicator(board.ProgressPanel!, board.QuestionIndex, board.Attempts, board.Questions.Count);

            var q = board.Questions[board.QuestionIndex];
            if (board.SentenceText != null) board.SentenceText.Text = q.Question;
            if (board.HintText != null) board.HintText.Text = $"Câu {board.QuestionIndex + 1}/{board.Questions.Count} — {q.Hint}";
            if (board.AnswerTextBox != null)
            {
                board.AnswerTextBox.Text = "";
                board.AnswerTextBox.IsEnabled = true;
                board.AnswerTextBox.Focus();
            }
            if (board.FeedbackText != null) board.FeedbackText.Text = "";
            
            if (board.HeaderText != null)
            {
                board.HeaderText.Text = $"{board.Name}: {board.Score} điểm";
            }

            if (board.KeyboardContainer != null)
            {
                board.KeyboardContainer.IsEnabled = true;
                BuildEmbeddedKeyboard(board);
            }
        }

        private void SubmitPlayerGramAnswer(int playerIndex)
        {
            var board = _activeBoards.FirstOrDefault(b => b.PlayerIndex == playerIndex);
            if (board == null || board.IsFinished || board.QuestionIndex >= board.Questions.Count) return;

            if (board.AnswerTextBox == null) return;
            board.AnswerTextBox.IsEnabled = false;
            if (board.KeyboardContainer != null)
            {
                board.KeyboardContainer.IsEnabled = false;
            }

            var q = board.Questions[board.QuestionIndex];
            string ans = board.AnswerTextBox.Text.Trim();
            
            string normalizedAns = System.Text.RegularExpressions.Regex.Replace(ans.ToLowerInvariant(), @"\s+", " ");
            bool correct = q.Answers.Any(a => {
                string normalizedTarget = System.Text.RegularExpressions.Regex.Replace(a.Trim().ToLowerInvariant(), @"\s+", " ");
                return normalizedTarget == normalizedAns;
            });

            // Log attempt
            var attempt = new UserQuizAttempt
            {
                QuestionIndex = board.QuestionIndex + 1,
                QuestionText = q.Question,
                UserAnswer = string.IsNullOrEmpty(ans) ? "(Để trống)" : ans,
                CorrectAnswer = q.Answers[0],
                IsCorrect = correct,
                PlayerName = board.Name
            };
            board.Attempts.Add(attempt);

            if (correct)
            {
                board.Score++;
                if (board.FeedbackText != null)
                {
                    board.FeedbackText.Text = "✅ Đúng!";
                    board.FeedbackText.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                }
                PlaySoundFeedback(true);
            }
            else
            {
                if (board.FeedbackText != null)
                {
                    board.FeedbackText.Text = $"❌ Đáp án: {q.Answers[0]}";
                    board.FeedbackText.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                }
                PlaySoundFeedback(false);
            }

            RenderProgressIndicator(board.ProgressPanel!, board.QuestionIndex, board.Attempts, board.Questions.Count);
            
            if (board.HeaderText != null)
            {
                board.HeaderText.Text = $"{board.Name}: {board.Score} điểm";
            }

            board.QuestionIndex++;

            if (board.TransitionTimer != null) board.TransitionTimer.Stop();
            board.TransitionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
            board.TransitionTimer.Tick += (s, e) => {
                board.TransitionTimer.Stop();
                board.TransitionTimer = null;
                ShowPlayerGramQ(board);
            };
            board.TransitionTimer.Start();
        }

        private void GramP1Answer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SubmitPlayerGramAnswer(0);
        }

        private void GramP2Answer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SubmitPlayerGramAnswer(1);
        }

        private void GramP3Answer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SubmitPlayerGramAnswer(2);
        }

        private void GramExit_Click(object sender, RoutedEventArgs e)
        {
            ExitFullscreen();
            ResetActiveQuizzes();
        }

        // ═══ MC TENSE QUIZ ═══
        private void McTenseQuizStart_Click(object s, RoutedEventArgs e)
        {
            if (mcResultCard.Visibility == Visibility.Visible || mcPlayGrid.Visibility == Visibility.Visible)
            {
                ResetActiveQuizzes();
                return;
            }

            string tenseFilter = cbMcTenseFilter.SelectedItem?.ToString() ?? "Tất cả các thì";
            
            var countItem = cbMcQuestionCount.SelectedItem as ComboBoxItem;
            int count = countItem != null ? int.Parse(countItem.Tag.ToString()!) : 10;

            var playerItem = cbMcPlayerCount.SelectedItem as ComboBoxItem;
            _selectedPlayerCount = playerItem != null ? int.Parse(playerItem.Tag.ToString()!) : 1;

            if (_selectedPlayerCount > 1)
            {
                _playerNames[0] = string.IsNullOrWhiteSpace(txtMcPlayer1.Text) ? "Học sinh 1" : txtMcPlayer1.Text.Trim();
                _playerNames[1] = string.IsNullOrWhiteSpace(txtMcPlayer2.Text) ? "Học sinh 2" : txtMcPlayer2.Text.Trim();
                if (_selectedPlayerCount == 3)
                {
                    _playerNames[2] = string.IsNullOrWhiteSpace(txtMcPlayer3.Text) ? "Học sinh 3" : txtMcPlayer3.Text.Trim();
                }
            }
            else
            {
                _playerNames[0] = "Học sinh";
            }

            _mcQuiz = PrepareQuizQuestions("MultipleChoice", tenseFilter, count);
            if (_mcQuiz.Count == 0) return;

            _mcActive = true;
            _activeBoards.Clear();
            _quizStartTime = DateTime.Now;
            _currentAttempts.Clear();

            mcSetupArea.Visibility = Visibility.Collapsed;
            mcPlayGrid.Visibility = Visibility.Visible;
            mcResultCard.Visibility = Visibility.Collapsed;
            btnMcGramStart.Visibility = Visibility.Collapsed;

            InitializePlayerColumns(isMc: true);

            // Create boards
            for (int i = 0; i < _selectedPlayerCount; i++)
            {
                var board = new PlayerBoard
                {
                    PlayerIndex = i,
                    Name = _playerNames[i],
                    Score = 0,
                    QuestionIndex = 0,
                    Questions = _mcQuiz.ToList(),
                    IsFinished = false
                };

                if (i == 0)
                {
                    board.McBoardBorder = borderMcPlayer1;
                    board.McHeaderText = txtMcP1Header;
                    board.McProgressPanel = mcP1ProgressPanel;
                    board.McSentenceText = txtMcP1Sentence;
                    board.McHintText = txtMcP1Hint;
                    board.McOptionsPanel = mcP1OptionsPanel;
                    board.McFeedbackText = txtMcP1Feedback;
                    board.McTimerText = txtMcP1TimerText;
                    board.McTimerBar = pbMcP1Timer;
                }
                else if (i == 1)
                {
                    board.McBoardBorder = borderMcPlayer2;
                    board.McHeaderText = txtMcP2Header;
                    board.McProgressPanel = mcP2ProgressPanel;
                    board.McSentenceText = txtMcP2Sentence;
                    board.McHintText = txtMcP2Hint;
                    board.McOptionsPanel = mcP2OptionsPanel;
                    board.McFeedbackText = txtMcP2Feedback;
                    board.McTimerText = txtMcP2TimerText;
                    board.McTimerBar = pbMcP2Timer;
                }
                else if (i == 2)
                {
                    board.McBoardBorder = borderMcPlayer3;
                    board.McHeaderText = txtMcP3Header;
                    board.McProgressPanel = mcP3ProgressPanel;
                    board.McSentenceText = txtMcP3Sentence;
                    board.McHintText = txtMcP3Hint;
                    board.McOptionsPanel = mcP3OptionsPanel;
                    board.McFeedbackText = txtMcP3Feedback;
                    board.McTimerText = txtMcP3TimerText;
                    board.McTimerBar = pbMcP3Timer;
                }

                _activeBoards.Add(board);
                ShowPlayerMcQ(board);
            }
        }

        private void ShowPlayerMcQ(PlayerBoard board)
        {
            if (board.QuestionIndex >= board.Questions.Count)
            {
                board.IsFinished = true;
                if (board.McTimerText != null) board.McTimerText.Text = "";
                if (board.McTimerBar != null) board.McTimerBar.Value = 0;
                if (board.McSentenceText != null)
                {
                    board.McSentenceText.Text = "🎉 Bạn đã hoàn thành tất cả câu hỏi!";
                }
                if (board.McHintText != null) board.McHintText.Text = "";
                if (board.McFeedbackText != null) board.McFeedbackText.Text = "";
                if (board.McOptionsPanel != null) board.McOptionsPanel.Children.Clear();
                
                if (board.BoardTimer != null) board.BoardTimer.Stop();

                // Check if all players finished
                if (_activeBoards.All(b => b.IsFinished))
                {
                    _mcActive = false;
                    ShowQuizSummary(true);
                }
                return;
            }

            RenderProgressIndicator(board.McProgressPanel!, board.QuestionIndex, board.Attempts, board.Questions.Count);

            board.McTimeLeft = 15;
            if (board.McTimerText != null) board.McTimerText.Text = "15s";
            if (board.McTimerBar != null) board.McTimerBar.Value = 15;

            // Start timer
            if (board.BoardTimer != null) board.BoardTimer.Stop();
            board.BoardTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            board.BoardTimer.Tick += (s, e) => {
                board.McTimeLeft--;
                if (board.McTimeLeft <= 0)
                {
                    board.BoardTimer.Stop();
                    if (board.McTimerText != null) board.McTimerText.Text = "Hết giờ!";
                    if (board.McTimerBar != null) board.McTimerBar.Value = 0;
                    SubmitPlayerMcAnswer(board, string.Empty);
                }
                else
                {
                    if (board.McTimerText != null) board.McTimerText.Text = $"{board.McTimeLeft}s";
                    if (board.McTimerBar != null) board.McTimerBar.Value = board.McTimeLeft;
                }
            };
            board.BoardTimer.Start();

            var q = board.Questions[board.QuestionIndex];
            if (board.McSentenceText != null) board.McSentenceText.Text = q.Question;
            if (board.McHintText != null) board.McHintText.Text = $"Câu {board.QuestionIndex + 1}/{board.Questions.Count} — Chọn thì đúng";
            if (board.McFeedbackText != null) board.McFeedbackText.Text = "";
            
            if (board.McOptionsPanel != null)
            {
                board.McOptionsPanel.Children.Clear();
                var options = q.Options ?? Array.Empty<string>();
                var shuffledOptions = options.OrderBy(_ => _rng.Next()).ToArray();
                
                foreach (var opt in shuffledOptions)
                {
                    var btn = new Button
                    {
                        Content = opt, FontSize = 15, Padding = new Thickness(16, 6, 16, 6),
                        Margin = new Thickness(0, 3, 0, 3), MinWidth = 240,
                        Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                        BorderThickness = new Thickness(1), Cursor = Cursors.Hand,
                        HorizontalContentAlignment = HorizontalAlignment.Left
                    };
                    string cap = opt;
                    btn.Click += (s, e) => SubmitPlayerMcAnswer(board, cap);
                    board.McOptionsPanel.Children.Add(btn);
                }
            }

            if (board.McHeaderText != null)
            {
                board.McHeaderText.Text = $"{board.Name}: {board.Score} điểm";
            }
        }

        private void SubmitPlayerMcAnswer(PlayerBoard board, string selectedOption)
        {
            if (board.BoardTimer != null) board.BoardTimer.Stop();

            var q = board.Questions[board.QuestionIndex];
            string correct = q.Answers[0];
            bool correctChoice = (selectedOption == correct);

            // Log attempt
            var attempt = new UserQuizAttempt
            {
                QuestionIndex = board.QuestionIndex + 1,
                QuestionText = q.Question,
                UserAnswer = string.IsNullOrEmpty(selectedOption) ? "(Hết giờ)" : selectedOption,
                CorrectAnswer = correct,
                IsCorrect = correctChoice,
                PlayerName = board.Name
            };
            board.Attempts.Add(attempt);

            // Colorize options
            if (board.McOptionsPanel != null)
            {
                foreach (var child in board.McOptionsPanel.Children)
                {
                    if (child is Button btn)
                    {
                        btn.IsEnabled = false;
                        string content = btn.Content?.ToString() ?? "";
                        if (content == correct)
                        {
                            btn.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                            btn.BorderBrush = new SolidColorBrush(Color.FromRgb(129, 199, 132));
                            btn.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                            btn.Content = $"✅ {content}";
                        }
                        else if (content == selectedOption && !correctChoice)
                        {
                            btn.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                            btn.BorderBrush = new SolidColorBrush(Color.FromRgb(229, 115, 115));
                            btn.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                            btn.Content = $"❌ {content}";
                        }
                        else
                        {
                            btn.Opacity = 0.5;
                        }
                    }
                }
            }

            if (correctChoice)
            {
                board.Score++;
                if (board.McFeedbackText != null)
                {
                    board.McFeedbackText.Text = "✅ Đúng!";
                    board.McFeedbackText.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                }
                PlaySoundFeedback(true);
            }
            else
            {
                if (board.McFeedbackText != null)
                {
                    board.McFeedbackText.Text = string.IsNullOrEmpty(selectedOption) ? $"❌ Hết giờ! Đáp án: {correct}" : "❌ Sai rồi!";
                    board.McFeedbackText.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                }
                PlaySoundFeedback(false);
            }

            RenderProgressIndicator(board.McProgressPanel!, board.QuestionIndex, board.Attempts, board.Questions.Count);

            if (board.McHeaderText != null)
            {
                board.McHeaderText.Text = $"{board.Name}: {board.Score} điểm";
            }

            board.QuestionIndex++;

            if (board.TransitionTimer != null) board.TransitionTimer.Stop();
            board.TransitionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
            board.TransitionTimer.Tick += (s, e) => {
                board.TransitionTimer.Stop();
                board.TransitionTimer = null;
                ShowPlayerMcQ(board);
            };
            board.TransitionTimer.Start();
        }

        private void McGramExit_Click(object sender, RoutedEventArgs e)
        {
            ExitFullscreen();
            ResetActiveQuizzes();
        }

        // ═══ FULLSCREEN OVERLAY LOGIC ═══
        private void GramFullscreen_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullscreen(fillBlankPlayGrid, viewFillBlank);
        }

        private void McFullscreen_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullscreen(mcPlayGrid, viewMcq);
        }

        private void ExitFullscreen_Click(object sender, RoutedEventArgs e)
        {
            ExitFullscreen();
        }

        private void ToggleFullscreen(UIElement element, Grid originalParent)
        {
            if (element == null || originalParent == null) return;
            
            // Ngắt khỏi parent gốc
            originalParent.Children.Remove(element);
            
            // Xóa nội dung host cũ (đề phòng)
            fullscreenContentHost.Children.Clear();
            
            // Thêm vào fullscreen host
            fullscreenContentHost.Children.Add(element);
            
            // Thiết lập vị trí
            Grid.SetRow(element, 0);
            Grid.SetColumn(element, 0);
            
            _fullscreenElement = element;
            _fullscreenOriginalParent = originalParent;
            
            fullscreenOverlay.Visibility = Visibility.Visible;
            if (sideMenu != null) sideMenu.IsEnabled = false;
        }

        private void ExitFullscreen()
        {
            if (_fullscreenElement == null || _fullscreenOriginalParent == null) return;
            
            // Ngắt khỏi fullscreen host
            fullscreenContentHost.Children.Remove(_fullscreenElement);
            
            // Trả về parent gốc
            _fullscreenOriginalParent.Children.Add(_fullscreenElement);
            
            // Phục hồi vị trí hàng 1
            Grid.SetRow(_fullscreenElement, 1);
            
            fullscreenOverlay.Visibility = Visibility.Collapsed;
            if (sideMenu != null) sideMenu.IsEnabled = true;
            
            _fullscreenElement = null;
            _fullscreenOriginalParent = null;
        }

        private void PlaySoundFeedback(bool correct)
        {
            QASmartClass.LearningTools.Helpers.SoundHelper.Play(correct);
        }

        private void SaveQuizHistory(string quizType, int correct, int total)
        {
            try
            {
                double timeSpent = (DateTime.Now - _quizStartTime).TotalSeconds;
                string studentCode = "HS_TEST";
                string studentName = "Học sinh thử nghiệm";
                string grade = "Khối 6";
                
                var app = Application.Current as QASmartTouch.App;
                if (app != null && app.UserRoleService != null)
                {
                    // Tương lai có thể lấy trực tiếp từ hệ thống nếu cần
                }

                using var db = new QASmartClass.Data.AppDbContext();
                var record = new QASmartClass.Data.GrammarQuizHistory
                {
                    StudentCode = studentCode,
                    StudentName = studentName,
                    Grade = grade,
                    QuizType = quizType,
                    CorrectAnswers = correct,
                    TotalQuestions = total,
                    TimeSpentSeconds = timeSpent,
                    CompletedAt = DateTime.Now
                };
                db.GrammarQuizHistories.Add(record);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error saving quiz history: " + ex.Message);
            }
        }

        private void ExportHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new QASmartClass.Data.AppDbContext();
                var historyList = db.GrammarQuizHistories.OrderByDescending(h => h.CompletedAt).ToList();
                if (historyList.Count == 0)
                {
                    MessageBox.Show("Chưa có lịch sử làm bài để xuất!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV File (*.csv)|*.csv",
                    FileName = $"LichSu_NguPhap_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (sfd.ShowDialog() == true)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.Append('\uFEFF'); // UTF-8 BOM
                    sb.AppendLine("ID,Mã Học Sinh,Tên Học Sinh,Khối,Loại Đề,Số Câu Đúng,Tổng Số Câu,Điểm Số,Thời Gian (Giây),Ngày Hoàn Thành");

                    foreach (var h in historyList)
                    {
                        string quizTypeVi = h.QuizType == "FillBlank" ? "Điền từ" : "Trắc nghiệm";
                        sb.AppendLine($"{h.Id},{EscapeCsvField(h.StudentCode)},{EscapeCsvField(h.StudentName)},{EscapeCsvField(h.Grade)},{EscapeCsvField(quizTypeVi)},{h.CorrectAnswers},{h.TotalQuestions},{h.Score:F1},{h.TimeSpentSeconds:F1},{h.CompletedAt:yyyy-MM-dd HH:mm:ss}");
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                    MessageBox.Show("Xuất lịch sử làm bài thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất lịch sử: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            if (helpOverlay != null) helpOverlay.Visibility = Visibility.Visible;
            
            // Pause all board timers and disable input
            foreach (var board in _activeBoards)
            {
                if (board.BoardTimer != null && board.BoardTimer.IsEnabled)
                {
                    board.BoardTimer.Stop();
                }
                if (board.AnswerTextBox != null)
                {
                    board.AnswerTextBox.IsEnabled = false;
                }
            }
            _timerPausedByHelp = true;
        }

        private void CloseHelp_Click(object sender, RoutedEventArgs e)
        {
            if (helpOverlay != null) helpOverlay.Visibility = Visibility.Collapsed;
            
            if (_timerPausedByHelp)
            {
                foreach (var board in _activeBoards)
                {
                    if (board.BoardTimer != null && !board.IsFinished)
                    {
                        board.BoardTimer.Start();
                    }
                    if (board.AnswerTextBox != null && !board.IsFinished)
                    {
                        board.AnswerTextBox.IsEnabled = true;
                    }
                }
                _timerPausedByHelp = false;
                
                // Focus back on the first active board's TextBox
                var activeBoard = _activeBoards.FirstOrDefault(b => !b.IsFinished && b.AnswerTextBox != null);
                if (activeBoard != null && activeBoard.AnswerTextBox != null)
                {
                    activeBoard.AnswerTextBox.Focus();
                }
            }
        }

        // ═══ DATA ═══
        private record GramQ(string Question, string Hint, string[] Answers, string[]? Options = null);

        private static readonly (string Name, string NameVi, string Structure, string Example, string Signal, string Color)[] Tenses =
        {
            ("Simple Present", "Hiện tại đơn", "Verb: S + V(s/es) / Be: S + am/is/are + adj/noun", "I go to school every day. / She is a student.", "always, usually, every day, often", "#1565C0"),
            ("Present Continuous", "Hiện tại tiếp diễn", "S + am/is/are + V-ing", "She is studying now.", "now, at the moment, look!, listen!", "#1976D2"),
            ("Present Perfect", "Hiện tại hoàn thành", "S + have/has + V3/ed", "I have lived here for 5 years.", "since, for, already, yet, ever, never", "#1E88E5"),
            ("Present Perfect Cont.", "Hiện tại hoàn thành tiếp diễn", "S + have/has been + V-ing", "He has been working all day.", "all day, all week, since, for", "#42A5F5"),
            ("Simple Past", "Quá khứ đơn", "Verb: S + V2/ed / Be: S + was/were + adj/noun", "I went to Hanoi last year. / He was happy yesterday.", "yesterday, last night, ago, in 2025", "#2E7D32"),
            ("Past Continuous", "Quá khứ tiếp diễn", "S + was/were + V-ing", "They were playing when I came.", "when, while, at 8 PM yesterday", "#388E3C"),
            ("Past Perfect", "Quá khứ hoàn thành", "S + had + V3/ed", "She had left before I arrived.", "before, after, by the time", "#43A047"),
            ("Past Perfect Cont.", "Quá khứ hoàn thành tiếp diễn", "S + had been + V-ing", "He had been waiting for 2 hours.", "for, since, before", "#66BB6A"),
            ("Simple Future", "Tương lai đơn", "S + will + V_inf", "I will call you tomorrow.", "tomorrow, next week, in the future", "#E65100"),
            ("Near Future", "Tương lai gần", "S + am/is/are + going to + V_inf", "We are going to buy a new car next week.", "next week, next month, soon, tonight", "#EF6C00"),
            ("Future Continuous", "Tương lai tiếp diễn", "S + will be + V-ing", "At 8 PM tomorrow, I will be studying.", "at this time tomorrow, at 8 PM tomorrow", "#EF6C00"),
            ("Future Perfect", "Tương lai hoàn thành", "S + will have + V3/ed", "By 2030, I will have graduated.", "by, by the time, by end of", "#F57C00"),
            ("Future Perfect Cont.", "Tương lai hoàn thành tiếp diễn", "S + will have been + V-ing", "By June, I will have been working here for 3 years.", "by, for", "#FB8C00"),
        };

        private static readonly GramQ[] FillBlankQuestions =
        {
            new("I ___ (go) to school every day.", "Chia động từ - Hiện tại đơn", new[] { "go" }),
            new("She ___ (be) cooking now.", "Chia động từ - Hiện tại tiếp diễn", new[] { "is" }),
            new("They ___ (already/finish) their homework.", "Chia động từ - Hiện tại hoàn thành", new[] { "have already finished", "already have finished" }),
            new("He ___ (go) to Hanoi last year.", "Chia động từ - Quá khứ đơn", new[] { "went" }),
            new("I ___ (be) watching TV when you called.", "Chia động từ - Quá khứ tiếp diễn", new[] { "was" }),
            new("She ___ (leave) before I arrived.", "Chia động từ - Quá khứ hoàn thành", new[] { "had left" }),
            new("I ___ (call) you tomorrow.", "Chia động từ - Tương lai đơn", new[] { "will call" }),
            new("He ___ (play) football every Sunday.", "Chia động từ - Hiện tại đơn ngôi thứ 3 số ít", new[] { "plays" }),
            new("We ___ (study) English for 3 years.", "Chia động từ - Hiện tại hoàn thành", new[] { "have studied", "have been studying" }),
            new("She ___ (read) a book right now.", "Chia động từ - Hiện tại tiếp diễn", new[] { "is reading" }),
            new("They ___ (visit) Paris last summer.", "Chia động từ - Quá khứ đơn", new[] { "visited" }),
            new("I ___ (not/see) him since Monday.", "Chia động từ phủ định - Hiện tại hoàn thành", new[] { "haven't seen", "have not seen" }),
            new("She ___ (work) here since 2020.", "Chia động từ - Hiện tại hoàn thành tiếp diễn", new[] { "has worked", "has been working" }),
            new("By 2030, I ___ (graduate) from university.", "Chia động từ - Tương lai hoàn thành", new[] { "will have graduated" }),
            new("He always ___ (wake) up early.", "Chia động từ - Hiện tại đơn ngôi thứ 3 số ít", new[] { "wakes" }),
            new("We ___ (buy) a new car next week.", "Chia động từ - Tương lai gần", new[] { "are going to buy" }),
            new("Look at those black clouds! It ___ (rain) soon.", "Chia động từ - Tương lai gần", new[] { "is going to rain" }),
        };
 
        private static readonly GramQ[] TenseQuestions =
        {
            new("\"I go to school every day.\"", "Xác định thì phù hợp", new[] { "Hiện tại đơn" }, new[] { "Hiện tại đơn", "Hiện tại tiếp diễn", "Quá khứ đơn", "Tương lai đơn" }),
            new("\"She is studying now.\"", "Xác định thì phù hợp", new[] { "Hiện tại tiếp diễn" }, new[] { "Hiện tại đơn", "Hiện tại tiếp diễn", "Quá khứ tiếp diễn", "Hiện tại hoàn thành" }),
            new("\"I have lived here for 5 years.\"", "Xác định thì phù hợp", new[] { "Hiện tại hoàn thành" }, new[] { "Quá khứ đơn", "Hiện tại hoàn thành", "Quá khứ hoàn thành", "Hiện tại đơn" }),
            new("\"I went to Hanoi last year.\"", "Xác định thì phù hợp", new[] { "Quá khứ đơn" }, new[] { "Hiện tại đơn", "Quá khứ đơn", "Quá khứ tiếp diễn", "Hiện tại hoàn thành" }),
            new("\"They were playing when I came.\"", "Xác định thì phù hợp", new[] { "Quá khứ tiếp diễn" }, new[] { "Quá khứ đơn", "Quá khứ tiếp diễn", "Hiện tại tiếp diễn", "Quá khứ hoàn thành" }),
            new("\"She had left before I arrived.\"", "Xác định thì phù hợp", new[] { "Quá khứ hoàn thành" }, new[] { "Quá khứ đơn", "Quá khứ tiếp diễn", "Quá khứ hoàn thành", "Hiện tại hoàn thành" }),
            new("\"I will call you tomorrow.\"", "Xác định thì phù hợp", new[] { "Tương lai đơn" }, new[] { "Hiện tại đơn", "Tương lai đơn", "Tương lai tiếp diễn", "Tương lai hoàn thành" }),
            new("\"He has been working all day.\"", "Xác định thì phù hợp", new[] { "Hiện tại hoàn thành tiếp diễn" }, new[] { "Hiện tại tiếp diễn", "Hiện tại hoàn thành", "Hiện tại hoàn thành tiếp diễn", "Quá khứ tiếp diễn" }),
            new("\"At 8 PM, I will be studying.\"", "Xác định thì phù hợp", new[] { "Tương lai tiếp diễn" }, new[] { "Tương lai đơn", "Tương lai tiếp diễn", "Hiện tại tiếp diễn", "Tương lai hoàn thành" }),
            new("\"By 2030, I will have graduated.\"", "Xác định thì phù hợp", new[] { "Tương lai hoàn thành" }, new[] { "Tương lai đơn", "Tương lai tiếp diễn", "Tương lai hoàn thành", "Hiện tại hoàn thành" }),
            new("\"He had been waiting for 2 hours.\"", "Xác định thì phù hợp", new[] { "Quá khứ hoàn thành tiếp diễn" }, new[] { "Quá khứ tiếp diễn", "Quá khứ hoàn thành", "Quá khứ hoàn thành tiếp diễn", "Hiện tại hoàn thành tiếp diễn" }),
            new("\"She plays tennis on Sundays.\"", "Xác định thì phù hợp", new[] { "Hiện tại đơn" }, new[] { "Hiện tại đơn", "Hiện tại tiếp diễn", "Quá khứ đơn", "Tương lai đơn" }),
            new("\"They are going to visit their grandparents tonight.\"", "Xác định thì phù hợp", new[] { "Tương lai gần" }, new[] { "Tương lai đơn", "Tương lai gần", "Hiện tại tiếp diễn", "Tương lai tiếp diễn" }),
        };

        private void BuildEmbeddedKeyboard(PlayerBoard board)
        {
            if (board.KeyboardContainer == null || board.AnswerTextBox == null) return;
            board.KeyboardContainer.Children.Clear();

            var mainStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

            // Rows layout
            string[] rows = { "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };
            foreach (var rowStr in rows)
            {
                var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 2) };
                foreach (char c in rowStr)
                {
                    var btn = CreateKey(c.ToString(), board.AnswerTextBox);
                    rowPanel.Children.Add(btn);
                }
                mainStack.Children.Add(rowPanel);
            }

            // Action Row
            var actionRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 2) };

            // Backspace
            var btnBack = CreateActionKey("⌫", 50, Color.FromRgb(198, 40, 40), () => {
                var tb = board.AnswerTextBox;
                if (tb == null) return;
                string txt = tb.Text ?? "";
                if (txt.Length > 0)
                {
                    if (tb.IsFocused)
                    {
                        int caret = tb.CaretIndex;
                        if (caret > 0)
                        {
                            tb.Text = txt.Remove(caret - 1, 1);
                            tb.CaretIndex = caret - 1;
                        }
                    }
                    else
                    {
                        tb.Text = txt.Remove(txt.Length - 1, 1);
                    }
                }
            });
            actionRow.Children.Add(btnBack);

            // Space
            var btnSpace = CreateActionKey("Dấu cách", 110, Color.FromRgb(70, 73, 82), () => {
                var tb = board.AnswerTextBox;
                if (tb == null) return;
                string txt = tb.Text ?? "";
                if (tb.IsFocused)
                {
                    int caret = tb.CaretIndex;
                    tb.Text = txt.Insert(caret, " ");
                    tb.CaretIndex = caret + 1;
                }
                else
                {
                    tb.Text = txt + " ";
                }
            });
            actionRow.Children.Add(btnSpace);

            // Clear
            var btnClear = CreateActionKey("C", 35, Color.FromRgb(255, 143, 0), () => {
                var tb = board.AnswerTextBox;
                if (tb != null) tb.Text = "";
            });
            actionRow.Children.Add(btnClear);

            // Enter
            var btnEnter = CreateActionKey("Enter ✓", 65, Color.FromRgb(46, 125, 50), () => {
                SubmitPlayerGramAnswer(board.PlayerIndex);
            });
            actionRow.Children.Add(btnEnter);

            mainStack.Children.Add(actionRow);
            board.KeyboardContainer.Children.Add(mainStack);
        }

        private Button CreateKey(string text, TextBox target)
        {
            var btn = new Button
            {
                Content = text,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = Brushes.White,
                Width = 28,
                Height = 32,
                Margin = new Thickness(1),
                Cursor = Cursors.Hand,
                Focusable = false, // Critical to avoid TextBox losing focus!
            };
            btn.Style = CreateKeyStyle(Color.FromRgb(60, 63, 72));
            btn.Click += (s, e) => {
                var tb = target;
                string key = text.ToLowerInvariant();
                if (tb.IsFocused)
                {
                    int caret = tb.CaretIndex;
                    tb.Text = (tb.Text ?? "").Insert(caret, key);
                    tb.CaretIndex = caret + 1;
                }
                else
                {
                    tb.Text = (tb.Text ?? "") + key;
                }
            };
            return btn;
        }

        private Button CreateActionKey(string text, double width, Color bgColor, Action action)
        {
            var btn = new Button
            {
                Content = text,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = Brushes.White,
                Width = width,
                Height = 32,
                Margin = new Thickness(1.5),
                Cursor = Cursors.Hand,
                Focusable = false, // Critical to avoid TextBox losing focus!
            };
            btn.Style = CreateKeyStyle(bgColor);
            btn.Click += (s, e) => action();
            return btn;
        }

        private Style CreateKeyStyle(Color normalColor)
        {
            var hoverColor = Color.FromRgb(
                (byte)System.Math.Min(255, normalColor.R + 25),
                (byte)System.Math.Min(255, normalColor.G + 25),
                (byte)System.Math.Min(255, normalColor.B + 25));

            var style = new Style(typeof(Button));
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(normalColor));
            border.Name = "bd";

            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(cp);
            template.VisualTree = border;

            var trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter
            {
                TargetName = "bd",
                Property = Border.BackgroundProperty,
                Value = new SolidColorBrush(hoverColor)
            });
            template.Triggers.Add(trigger);

            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewTable == null || viewFillBlank == null || viewMcq == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewTable.Visibility = Visibility.Collapsed;
            viewFillBlank.Visibility = Visibility.Collapsed;
            viewMcq.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            ResetActiveQuizzes();

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewTable.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewFillBlank.Visibility = Visibility.Visible;
                    break;
                case 3:
                    viewMcq.Visibility = Visibility.Visible;
                    break;
                case 4:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}