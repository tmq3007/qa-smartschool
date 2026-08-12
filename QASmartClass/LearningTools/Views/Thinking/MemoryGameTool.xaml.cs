using QASmartClass.LearningTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Helpers;

namespace QASmartClass.LearningTools.Views.Thinking
{
    public partial class MemoryGameTool : BaseToolControl
    {
        private readonly Random _rng = new();
        private List<(string Front, string Back, int PairId)> _cards = new();
        private Border? _firstCard, _secondCard;
        private int _firstIdx = -1, _secondIdx = -1;
        private int _moves, _pairsFound, _totalPairs;
        private bool _isLocked;
        private DispatcherTimer? _flipTimer;
        private DispatcherTimer? _gameTimer;
        private DateTime _startTime;
        private int _elapsedSeconds;

        public MemoryGameTool()
        {
            InitializeComponent();
            DbManager.Initialize();

            // Khởi tạo các Timer một lần duy nhất tại Constructor
            _gameTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _gameTimer.Tick += (s, e) =>
            {
                _elapsedSeconds = (int)(DateTime.Now - _startTime).TotalSeconds;
                if (txtTimer != null) txtTimer.Text = $"⏱️ Thời gian: {_elapsedSeconds}s";
            };

            _flipTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
            _flipTimer.Tick += (s, e) =>
            {
                _flipTimer.Stop();
                if (_firstCard != null && _secondCard != null)
                {
                    HideCard(_firstCard);
                    HideCard(_secondCard);
                }
                _firstCard = _secondCard = null;
                _isLocked = false;
            };

            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn Học" : "Study Guide";
                if (menuTextPlay != null) menuTextPlay.Text = isVN ? "Trò Chơi Lật Thẻ" : "Card Matching";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                var settings = GameSettingsManager.Load();
                if (cboTheme != null)
                {
                    cboTheme.SelectedIndex = settings.MemoryTheme;
                }
                NewGame();
                LoadPracticalApps();
            };
            Unloaded += (_, _) => StopTimers();
        }

        private void StopTimers()
        {
            _flipTimer?.Stop();
            _gameTimer?.Stop();
        }

        private void NewGame_Click(object sender, MouseButtonEventArgs e) => NewGame();
        private void Theme_Changed(object sender, EventArgs e)
        {
            if (IsLoaded)
            {
                var settings = GameSettingsManager.Load();
                settings.MemoryTheme = cboTheme.SelectedIndex;
                GameSettingsManager.Save(settings);
                NewGame();
            }
        }

        private void NewGame()
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 1)
            {
                sideMenu.SelectedIndex = 0;
            }
            StopTimers();
            _moves = 0; _pairsFound = 0; _isLocked = false; _elapsedSeconds = 0;
            _firstCard = _secondCard = null;
            var pairs = GetPairs(cboTheme?.SelectedIndex ?? 0);
            _totalPairs = pairs.Count;

            _cards = new List<(string, string, int)>();
            for (int i = 0; i < pairs.Count; i++)
            {
                _cards.Add((pairs[i].Q, pairs[i].A, i));
                _cards.Add((pairs[i].A, pairs[i].Q, i));
            }
            _cards = _cards.OrderBy(_ => _rng.Next()).ToList();

            BuildCards();
            UpdateUI();
            if (txtStatus != null) txtStatus.Text = "🎮 Lật 2 thẻ để tìm cặp!";

            _startTime = DateTime.Now;
            if (txtTimer != null) txtTimer.Text = "⏱️ Thời gian: 0s";
            _gameTimer?.Start();
        }

        private void BuildCards()
        {
            if (cardsPanel == null) return;
            cardsPanel.Children.Clear();
            for (int i = 0; i < _cards.Count; i++)
            {
                int idx = i;
                var card = new Border
                {
                    Width = 150, Height = 110, Margin = new Thickness(6),
                    CornerRadius = new CornerRadius(10), Cursor = Cursors.Hand,
                    Background = new SolidColorBrush(Color.FromRgb(27, 94, 32)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                    BorderThickness = new Thickness(1),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 4, ShadowDepth = 1, Opacity = 0.15 }
                };

                // Add Hover Effect
                card.MouseEnter += (s, e) =>
                {
                    if ((string)card.Tag == "hidden")
                    {
                        card.Background = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                        card.BorderBrush = new SolidColorBrush(Color.FromRgb(129, 199, 132));
                    }
                };
                card.MouseLeave += (s, e) =>
                {
                    if ((string)card.Tag == "hidden")
                    {
                        card.Background = new SolidColorBrush(Color.FromRgb(27, 94, 32));
                        card.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
                    }
                };

                card.Child = new TextBlock
                {
                    Text = "❓", FontSize = 28, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White,
                    FontFamily = new FontFamily("Segoe UI")
                };
                card.MouseLeftButtonDown += (_, _) => FlipCard(idx, card);
                card.Tag = "hidden";
                cardsPanel.Children.Add(card);
            }
        }

        private bool IsMatchingPair(string val1, string val2)
        {
            var themePairs = GetPairs(cboTheme?.SelectedIndex ?? 0);
            return themePairs.Any(p => 
                (p.Q == val1 && p.A == val2) || 
                (p.Q == val2 && p.A == val1)
            );
        }

        private void FlipCard(int idx, Border card)
        {
            if (_isLocked) return;
            if ((string)card.Tag == "revealed" || (string)card.Tag == "matched") return;
            if (card == _firstCard) return;

            // Show card with auto-scaling font size based on text length
            string text = _cards[idx].Front;
            int len = text.Length;
            int cardFontSize = len <= 3 ? 26 : len <= 8 ? 22 : len <= 15 ? 18 : 15;

            card.Background = Brushes.White;
            card.BorderBrush = new SolidColorBrush(Color.FromRgb(27, 94, 32));
            card.BorderThickness = new Thickness(2);

            SetCardFormattedText((TextBlock)card.Child, text, cardFontSize);
            card.Tag = "revealed";

            // Speak English words if theme matches
            try
            {
                int theme = cboTheme?.SelectedIndex ?? 0;
                if (theme == 2)
                {
                    var themePairs = GetPairs(2);
                    if (themePairs.Any(p => p.Q == text))
                    {
                        TtsHelper.SpeakEnglish(text);
                    }
                }
                else if (theme == 1)
                {
                    var themePairs = GetPairs(1);
                    if (themePairs.Any(p => p.A == text))
                    {
                        TtsHelper.SpeakEnglish(text);
                    }
                }
            }
            catch { }

            if (_firstCard == null)
            {
                _firstCard = card;
                _firstIdx = idx;
            }
            else
            {
                _secondCard = card;
                _secondIdx = idx;
                _moves++;
                _isLocked = true;

                // Check match dynamically using PairId
                if (_cards[_firstIdx].PairId == _cards[_secondIdx].PairId)
                {
                    // Match!
                    SoundHelper.Play(true);
                    _pairsFound++;
                    _firstCard.Background = new SolidColorBrush(Color.FromArgb(40, 46, 125, 50));
                    _secondCard.Background = new SolidColorBrush(Color.FromArgb(40, 46, 125, 50));
                    _firstCard.Tag = "matched";
                    _secondCard.Tag = "matched";
                    _firstCard = _secondCard = null;
                    _isLocked = false;

                    if (_pairsFound == _totalPairs)
                    {
                        _gameTimer?.Stop();
                        txtStatus.Text = $"🎉 Hoàn thành! {_moves} lượt trong {_elapsedSeconds} giây!";
                        try
                        {
                            string themeName = (cboTheme?.SelectedIndex ?? 0) switch
                            {
                                0 => "Phép tính",
                                1 => "Nguyên tố",
                                2 => "Từ vựng",
                                3 => "Công thức",
                                _ => "Mặc định"
                            };
                            DbManager.SaveProgress(new UserProgress
                            {
                                GameName = "Memory Game",
                                Score = _moves,
                                Total = _totalPairs,
                                DurationSeconds = _elapsedSeconds,
                                Difficulty = themeName,
                                CreatedAt = DateTime.Now
                            });
                        }
                        catch { }
                    }
                }
                else
                {
                    // No match — flip back after delay
                    SoundHelper.Play(false);
                    _flipTimer?.Start();
                }
                UpdateUI();
            }
        }

        private static void HideCard(Border card)
        {
            card.Background = new SolidColorBrush(Color.FromRgb(27, 94, 32));
            card.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
            card.BorderThickness = new Thickness(1);
            ((TextBlock)card.Child).Text = "❓";
            ((TextBlock)card.Child).FontSize = 28;
            ((TextBlock)card.Child).FontWeight = FontWeights.Bold;
            ((TextBlock)card.Child).Foreground = Brushes.White;
            card.Tag = "hidden";
        }

        private void UpdateUI()
        {
            if (txtMoves != null) txtMoves.Text = $"📊 Lượt: {_moves}";
            if (txtPairs != null) txtPairs.Text = $"✅ Cặp: {_pairsFound}/{_totalPairs}";
        }

        // ═══ CARD DATA ═══
        private List<(string Q, string A)> GetPairs(int theme)
        {
            return theme switch
            {
                0 => new() // Phép tính
                {
                    ("7 × 8", "56"), ("9 × 6", "54"), ("12 × 5", "60"), ("15 × 3", "45"),
                    ("8 × 4", "32"), ("11 × 7", "77"), ("6 × 8", "48"), ("13 × 4", "52"),
                },
                1 => new() // Nguyên tố (Chuẩn hóa IUPAC tiếng Anh)
                {
                    ("H", "Hydrogen"), ("O", "Oxygen"), ("Fe", "Iron"), ("Au", "Gold"),
                    ("Na", "Sodium"), ("Cl", "Chlorine"), ("Ca", "Calcium"), ("C", "Carbon"),
                },
                2 => new() // Từ vựng
                {
                    ("school", "trường"), ("book", "sách"), ("teacher", "giáo viên"), ("student", "học sinh"),
                    ("water", "nước"), ("friend", "bạn bè"), ("family", "gia đình"), ("house", "nhà"),
                },
                3 => new() // Công thức (Chuẩn hóa SGK Việt Nam)
                {
                    ("S = v×t", "Quãng đường"), ("F = m×a", "Lực"), ("P = U×I", "Công suất"), ("E = m×c²", "Năng lượng"),
                    ("U = I×R", "Hiệu điện thế"), ("D = m/V", "Khối lượng riêng"), ("a² + b² = c²", "Định lý Pythagore"), ("pH", "−log[H⁺]"),
                },
                _ => new()
            };
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || contentGuide == null || contentPlay == null || viewPractical == null)
                return;

            contentGuide.Visibility = Visibility.Collapsed;
            contentPlay.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            var fadeAnimation = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    contentGuide.Visibility = Visibility.Visible;
                    contentGuide.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
                    break;
                case 1:
                    contentPlay.Visibility = Visibility.Visible;
                    contentPlay.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    viewPractical.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
                    break;
            }
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
                        Icon = "📚",
                        Title = isVN ? "Học Từ Vựng" : "Brain Training & Memory",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_memory_game_1_{suffix}.png",
                        Description = isVN 
                            ? "Học từ vựng tiếng Anh/ngoại ngữ nhanh gấp 2 lần bằng phương pháp ghi nhớ mặt chữ và nghĩa từ song ngữ." 
                            : "Stimulate neural activity and improve short-term memory capacity by playing card-matching memory games."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧪",
                        Title = isVN ? "Ký Hiệu Hóa Học" : "Focus & Attention Span",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_memory_game_2_{suffix}.png",
                        Description = isVN 
                            ? "Ghi nhớ ký hiệu hóa học và tên gọi IUPAC tiếng Anh chuẩn xác theo sách giáo khoa mới GDPT 2018." 
                            : "Enhance children's ability to focus and resist distractions by encouraging them to pay attention to card locations."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📐",
                        Title = isVN ? "Công Thức Lý Hóa" : "Visual Recognition Skills",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_memory_game_3_{suffix}.png",
                        Description = isVN 
                            ? "Học thuộc lòng các công thức Toán học, Vật lý, Hóa học và liên hệ ý nghĩa thực tiễn của từng đại lượng." 
                            : "Develop visual shape and color recognition skills in early childhood education using icon-matching cards."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📜",
                        Title = isVN ? "Sự Kiện Lịch Sử" : "Language Vocabulary Practice",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_memory_game_4_{suffix}.png",
                        Description = isVN 
                            ? "Kết hợp các mốc thời gian với tên sự kiện lịch sử quan trọng giúp ghi nhớ tiến trình lịch sử trực quan." 
                            : "Learn vocabulary by matching foreign words with corresponding pictures, improving retention through association."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎵",
                        Title = isVN ? "Ký Hiệu Nhạc Lý" : "Rehabilitation Therapy",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_memory_game_5_{suffix}.png",
                        Description = isVN 
                            ? "Nhớ nhanh tên nốt nhạc trên khuông, ký hiệu hợp âm guitar/piano cơ bản một cách tự nhiên." 
                            : "Support cognitive recovery in stroke patients or elderly individuals using simple matching tasks to rebuild neural paths."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧬",
                        Title = isVN ? "Thuật Ngữ Sinh Học" : "Stress Relief & Relaxation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_memory_game_6_{suffix}.png",
                        Description = isVN 
                            ? "Liên kết hình ảnh giải phẫu, cơ cấu tế bào với tên gọi thuật ngữ sinh học tương ứng." 
                            : "Provide a relaxing gaming break to reduce stress and anxiety while maintaining light cognitive engagement."
                    }
            };

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for MemoryGameTool: {Err}", ex.Message);
            }
        }

        private void SetCardFormattedText(TextBlock tb, string text, double defaultFontSize)
        {
            if (tb == null) return;
            tb.Inlines.Clear();
            tb.FontSize = defaultFontSize;
            tb.FontWeight = FontWeights.Bold;
            tb.TextWrapping = TextWrapping.Wrap;
            tb.TextAlignment = TextAlignment.Center;
            tb.Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32));

            int len = text.Length;
            for (int i = 0; i < len; i++)
            {
                char c = text[i];
                if (c == '²' || c == '⁺')
                {
                    var run = new Run(c.ToString())
                    {
                        FontSize = defaultFontSize * 1.25, // To hơn 25% để rõ nét
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0)), // Cam nổi bật (#E65100)
                        BaselineAlignment = BaselineAlignment.Superscript
                    };
                    tb.Inlines.Add(run);
                }
                else
                {
                    tb.Inlines.Add(new Run(c.ToString()));
                }
            }
        }
    }
}