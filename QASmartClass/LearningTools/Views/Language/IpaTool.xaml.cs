using QASmartClass.LearningTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Language
{
    public partial class IpaTool : BaseToolControl
    {
        private readonly Random _rng = new();
        private int _qIdx, _qCorrect, _qTotal;
        private List<IpaQ> _quiz = new();
        private bool _qActive;

        public IpaTool()
        {
            InitializeComponent();
            Loaded += (_, _) => {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextTable != null) menuTextTable.Text = isVN ? "Bảng phiên âm IPA" : "IPA Chart";
                if (menuTextQuiz != null) menuTextQuiz.Text = isVN ? "Trắc nghiệm IPA" : "IPA Quiz";
                if (menuTextMinPairs != null) menuTextMinPairs.Text = isVN ? "Cặp âm tối thiểu" : "Minimal Pairs";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                BuildIpa(); 
                BuildMinimalPairs(); 
                LoadPracticalApps(); 
            };
        }

        // ═══ BUILD IPA TABLE (improved) ═══
        private void BuildIpa()
        {
            if (ipaPanel == null) return;
            ipaPanel.Children.Clear();

            ipaPanel.Children.Add(CreateFormulaCard(
                "🗣️", "12 Monophthongs + 8 Diphthongs = 20 Vowels",
                "Nguyên âm chia 2 nhóm: đơn (short/long) và đôi (gliding)",
                "#00695C", "#26A69A"));
            ipaPanel.Children.Add(CreateFormulaCard(
                "🔊", "24 Consonants = Voiced + Voiceless",
                "Phụ âm phân biệt theo: vị trí cấu âm và rung/không rung dây thanh",
                "#4E342E", "#8D6E63"));

            foreach (var (title, bgHex, items) in IpaSections)
            {
                var bgColor = (Color)ColorConverter.ConvertFromString(bgHex);
                ipaPanel.Children.Add(new TextBlock { Text = $"📂 {title} ({items.Length})", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92)), Margin = new Thickness(4, 12, 0, 6) });
                var wrap = new WrapPanel();
                int cellIdx = 0;
                foreach (var (symbol, example, desc) in items)
                {
                    var normalBrush = new SolidColorBrush(bgColor);
                    // Tạo màu hover đậm hơn một chút bằng cách trừ bớt RGB
                    var hoverColor = Color.FromArgb(bgColor.A, (byte)System.Math.Max(0, bgColor.R - 20), (byte)System.Math.Max(0, bgColor.G - 15), (byte)System.Math.Max(0, bgColor.B - 10));
                    var hoverBrush = new SolidColorBrush(hoverColor);

                    // Tạo cell với độ rộng 125 để tránh vỡ chữ
                    var cell = new Border 
                    { 
                        Width = 125, 
                        Background = normalBrush, 
                        CornerRadius = new CornerRadius(8), 
                        Padding = new Thickness(8, 6, 8, 6), 
                        Margin = new Thickness(0), 
                        Cursor = Cursors.Hand, 
                        ToolTip = $"/{symbol}/ — {example}\nClick copy" 
                    };

                    // Hiệu ứng hover
                    cell.MouseEnter += (s, e) => cell.Background = hoverBrush;
                    cell.MouseLeave += (s, e) => cell.Background = normalBrush;

                    var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                    sp.Children.Add(new TextBlock { Text = $"/{symbol}/", FontSize = 20, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, FontFamily = new FontFamily("Segoe UI") });
                    
                    var descText = new TextBlock { Text = desc, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)), HorizontalAlignment = HorizontalAlignment.Center };
                    sp.Children.Add(descText);
                    
                    sp.Children.Add(new TextBlock { Text = example.Split('/')[0].Trim(), FontSize = 13, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0, 77, 64)), HorizontalAlignment = HorizontalAlignment.Center, FontStyle = FontStyles.Italic });
                    cell.Child = sp;
                    
                    string copySym = symbol;
                    cell.MouseLeftButtonDown += (s, e) => 
                    { 
                        try 
                        { 
                            Clipboard.SetText(copySym); 
                            // Phản hồi trực quan khi Copy thành công (chống trùng lặp timer)
                            if (descText.Text != "📋 Đã chép!")
                            {
                                var oldText = descText.Text;
                                descText.Text = "📋 Đã chép!";
                                descText.Foreground = Brushes.DarkGreen;
                                
                                var copyTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                                copyTimer.Tick += (st, et) => 
                                { 
                                    copyTimer.Stop(); 
                                    descText.Text = oldText; 
                                    descText.Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)); 
                                };
                                copyTimer.Start();
                            }
                        } 
                        catch { } 
                    };
                    
                    // Tạo ID duy nhất dựa trên kí tự phiên âm để tránh trùng lặp
                    string cleanSym = symbol.Replace("/", "").Replace("ː", "_long").Replace(" ", "_").ToLowerInvariant();
                    string cellId = $"sym_{cleanSym}_{cellIdx++}";
                    string cellName = $"Âm /{symbol}/ ({desc})";
                    
                    var cellWrapper = WrapWithSectionToolbar(cell, "ipa", cellId, cellName);
                    cellWrapper.Margin = new Thickness(3); // Giữ khoảng cách giữa các thẻ trong WrapPanel
                    wrap.Children.Add(cellWrapper);
                }
                // Wrap section trong Border trắng (hiển thị danh sách thẻ trực tiếp không cần bọc thêm toolbar nhóm)
                var sectionBorder = new Border
                {
                    Background = Brushes.White, CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8), Margin = new Thickness(0, 0, 0, 4),
                    Child = wrap
                };
                ipaPanel.Children.Add(sectionBorder);
            }
        }

        // ═══ MINIMAL PAIRS ═══
        private void BuildMinimalPairs()
        {
            if (minPairsPanel == null) return;
            minPairsPanel.Children.Clear();
            minPairsPanel.Children.Add(new TextBlock { Text = "🔗 Minimal Pairs — Cặp từ phân biệt âm", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92)), Margin = new Thickness(6, 12, 0, 8) });
            minPairsPanel.Children.Add(new TextBlock { Text = "Hai từ chỉ khác nhau ở MỘT âm, giúp luyện phân biệt phát âm chính xác", FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)), Margin = new Thickness(6, 0, 0, 16) });

            foreach (var (w1, ipa1, w2, ipa2, sound) in MinimalPairsData)
            {
                var card = new Border { Background = Brushes.White, CornerRadius = new CornerRadius(10), Padding = new Thickness(20, 12, 20, 12), Margin = new Thickness(0, 0, 0, 8), BorderBrush = new SolidColorBrush(Color.FromRgb(178, 223, 219)), BorderThickness = new Thickness(0, 0, 0, 2), Cursor = Cursors.Hand };
                
                // Hiệu ứng hover cho dòng Minimal Pairs
                card.MouseEnter += (s, e) => card.Background = new SolidColorBrush(Color.FromRgb(240, 247, 246));
                card.MouseLeave += (s, e) => card.Background = Brushes.White;

                var dp = new DockPanel();
                dp.Children.Add(new TextBlock { Text = $"/{sound}/", FontSize = 14, Width = 90, Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92)), FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, FontFamily = new FontFamily("Segoe UI") });
                dp.Children.Add(new TextBlock { Text = $"{w1} {ipa1}", FontSize = 14, FontWeight = FontWeights.SemiBold, Width = 200, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(26, 35, 126)) });
                dp.Children.Add(new TextBlock { Text = "⟷", FontSize = 16, Width = 40, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Foreground = Brushes.Gray });
                dp.Children.Add(new TextBlock { Text = $"{w2} {ipa2}", FontSize = 14, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(173, 20, 87)) });
                card.Child = dp;
                minPairsPanel.Children.Add(card);
            }
        }

        // ═══ IPA QUIZ ═══
        private void IpaQuizStart_Click(object s, RoutedEventArgs e)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 2)
            {
                sideMenu.SelectedIndex = 0;
            }
            _quiz = AllIpaQuizData.OrderBy(_ => _rng.Next()).Take(10).ToList();
            _qIdx = 0; _qCorrect = 0; _qTotal = 0; _qActive = true;
            btnIpaStart.Visibility = Visibility.Collapsed; // Ẩn nút start khi đang làm bài
            ShowIpaQ();
        }

        private void ShowIpaQ()
        {
            if (_qIdx >= _quiz.Count)
            {
                _qActive = false;
                txtIpaQ.Text = $"🎉 Hoàn thành! Kết quả: {_qCorrect}/{_quiz.Count} câu đúng.";
                txtIpaHint.Text = "Hãy thử luyện tập lại để đạt điểm số tối đa nhé!";
                mcIpaPanel.Children.Clear();
                txtIpaFeedback.Text = "";
                btnIpaStart.Content = "🔄 Luyện tập lại";
                btnIpaStart.Visibility = Visibility.Visible; // Hiện lại nút start để chơi lại
                return;
            }
            _qActive = true;
            var q = _quiz[_qIdx];
            txtIpaQ.Text = q.Question;
            txtIpaHint.Text = $"Câu {_qIdx + 1}/{_quiz.Count} — {q.Hint}";
            txtIpaFeedback.Text = "";
            mcIpaPanel.Children.Clear();
            foreach (var opt in q.Options)
            {
                var btn = new Button
                {
                    Content = opt, FontSize = 14, Padding = new Thickness(20, 8, 20, 8),
                    Margin = new Thickness(0, 3, 0, 3), MinWidth = 280,
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(1), Cursor = Cursors.Hand,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                var cap = opt;
                btn.Click += (_, _) => CheckIpaAnswer(btn, cap, q.Answer);
                mcIpaPanel.Children.Add(btn);
            }
        }

        private void CheckIpaAnswer(Button clickedBtn, string sel, string correct)
        {
            if (!_qActive) return;
            _qActive = false; // Chặn double click lập tức
            _qTotal++;

            // Thay đổi giao diện các nút đáp án (không disable để giữ màu nền rõ ràng)
            foreach (var child in mcIpaPanel.Children)
            {
                if (child is Button btn)
                {
                    btn.Cursor = Cursors.Arrow;
                    string btnContent = btn.Content?.ToString();
                    if (btnContent == correct)
                    {
                        btn.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201)); // Xanh lá nhạt
                        btn.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));  // Xanh lá viền
                        btn.Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32));   // Chữ xanh đậm
                    }
                    else if (btn == clickedBtn)
                    {
                        btn.Background = new SolidColorBrush(Color.FromRgb(255, 205, 210)); // Đỏ nhạt
                        btn.BorderBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));  // Đỏ viền
                        btn.Foreground = new SolidColorBrush(Color.FromRgb(183, 28, 28));   // Chữ đỏ đậm
                    }
                }
            }

            if (sel == correct) 
            { 
                _qCorrect++; 
                txtIpaFeedback.Text = "✅ Chính xác!"; 
                txtIpaFeedback.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)); 
            }
            else 
            { 
                txtIpaFeedback.Text = $"❌ Sai rồi! Đáp án là: {correct}"; 
                txtIpaFeedback.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)); 
            }
            
            _qIdx++; 
            txtIpaScore.Text = $"Điểm: {_qCorrect}/{_qTotal}";
            
            // Tăng trì hoãn lên 1.5s để người học kịp đọc đáp án
            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
            t.Tick += (_, _) => { t.Stop(); ShowIpaQ(); }; 
            t.Start();
        }

        // ═══ STATIC DATA ═══
        private record IpaQ(string Question, string Hint, string Answer, string[] Options);

        private static readonly (string Title, string BgHex, (string Symbol, string Example, string Desc)[] Items)[] IpaSections =
        {
            ("Nguyên âm đơn (Monophthongs)", "#E8F5E9", new[] {
                ("iː","see /siː/","i dài"), ("ɪ","sit /sɪt/","i ngắn"), ("e","bed /bed/","e ngắn"), ("æ","cat /kæt/","a bẹt"),
                ("ɑː","car /kɑːr/","a dài"), ("ɒ","hot /hɒt/","o ngắn"), ("ɔː","four /fɔːr/","o dài"), ("ʊ","put /pʊt/","u ngắn"),
                ("uː","food /fuːd/","u dài"), ("ʌ","cup /kʌp/","ă / ơ ngắn"), ("ɜː","bird /bɜːrd/","ơ dài"), ("ə","about /əˈbaʊt/","ơ ngắn (schwa)"),
            }),
            ("Nguyên âm đôi (Diphthongs)", "#E3F2FD", new[] {
                ("eɪ","say /seɪ/","ê-i (ây)"), ("aɪ","my /maɪ/","a-i (ai)"), ("ɔɪ","boy /bɔɪ/","o-i (oi)"), ("aʊ","how /haʊ/","a-u (ao)"),
                ("əʊ","go /ɡəʊ/","ơ-u (âu)"), ("ɪə","near /nɪər/","i-ơ (ia)"), ("eə","hair /heər/","e-ơ (ea)"), ("ʊə","tour /tʊər/","u-ơ (ua)"),
            }),
            ("Phụ âm (Consonants)", "#F3E5F5", new[] {
                ("p","pen /pen/","p (vô thanh)"), ("b","bad /bæd/","b (hữu thanh)"), ("t","tea /tiː/","t (vô thanh)"), ("d","did /dɪd/","d (hữu thanh)"),
                ("k","cat /kæt/","k (vô thanh)"), ("ɡ","get /ɡet/","g (hữu thanh)"), ("f","fall /fɔːl/","f (vô thanh)"), ("v","van /væn/","v (hữu thanh)"),
                ("θ","thin /θɪn/","th (vô thanh)"), ("ð","this /ðɪs/","th (hữu thanh)"), ("s","see /siː/","s (vô thanh)"), ("z","zoo /zuː/","z (hữu thanh)"),
                ("ʃ","she /ʃiː/","sh (vô thanh)"), ("ʒ","vision /ˈvɪʒn/","gi (hữu thanh)"), ("h","hat /hæt/","h (vô thanh)"), ("tʃ","church /tʃɜːrtʃ/","ch (vô thanh)"),
                ("dʒ","judge /dʒʌdʒ/","j (hữu thanh)"), ("m","man /mæn/","m (hữu thanh)"), ("n","no /nəʊ/","n (hữu thanh)"), ("ŋ","sing /sɪŋ/","ng (hữu thanh)"),
                ("l","leg /leɡ/","l (hữu thanh)"), ("r","red /red/","r (hữu thanh)"), ("j","yes /jes/","y (hữu thanh)"), ("w","wet /wet/","w (hữu thanh)"),
            }),
        };

        private static readonly (string W1, string Ipa1, string W2, string Ipa2, string Sound)[] MinimalPairsData =
        {
            ("ship", "/ʃɪp/", "sheep", "/ʃiːp/", "ɪ vs iː"),
            ("bat", "/bæt/", "bet", "/bet/", "æ vs e"),
            ("cat", "/kæt/", "cut", "/kʌt/", "æ vs ʌ"),
            ("full", "/fʊl/", "fool", "/fuːl/", "ʊ vs uː"),
            ("pen", "/pen/", "pan", "/pæn/", "e vs æ"),
            ("sit", "/sɪt/", "seat", "/siːt/", "ɪ vs iː"),
            ("hot", "/hɒt/", "hat", "/hæt/", "ɒ vs æ"),
            ("thin", "/θɪn/", "tin", "/tɪn/", "θ vs t"),
            ("then", "/ðen/", "den", "/den/", "ð vs d"),
            ("van", "/væn/", "fan", "/fæn/", "v vs f"),
            ("rice", "/raɪs/", "lice", "/laɪs/", "r vs l"),
            ("bit", "/bɪt/", "pit", "/pɪt/", "b vs p"),
            ("coat", "/kəʊt/", "goat", "/ɡəʊt/", "k vs ɡ"),
            ("she", "/ʃiː/", "see", "/siː/", "ʃ vs s"),
            ("light", "/laɪt/", "right", "/raɪt/", "l vs r"),
        };

        private static readonly IpaQ[] AllIpaQuizData =
        {
            new("Từ 'cat' có nguyên âm nào?", "Chọn ký hiệu IPA", "/æ/", new[] { "/æ/", "/e/", "/ʌ/", "/ɑː/" }),
            new("Từ 'see' có nguyên âm nào?", "Chọn ký hiệu IPA", "/iː/", new[] { "/ɪ/", "/iː/", "/e/", "/eɪ/" }),
            new("Từ 'cup' có nguyên âm nào?", "Chọn ký hiệu IPA", "/ʌ/", new[] { "/ʊ/", "/ʌ/", "/æ/", "/ɑː/" }),
            new("Từ 'bird' có nguyên âm nào?", "Chọn ký hiệu IPA", "/ɜː/", new[] { "/ɪ/", "/ɜː/", "/ɔː/", "/ə/" }),
            new("Âm /θ/ xuất hiện trong từ nào?", "Chọn từ đúng", "thin", new[] { "this", "thin", "tin", "sin" }),
            new("Âm /ʃ/ xuất hiện trong từ nào?", "Chọn từ đúng", "she", new[] { "see", "she", "set", "sea" }),
            new("Từ 'judge' có phụ âm đầu nào?", "Chọn ký hiệu IPA", "/dʒ/", new[] { "/tʃ/", "/dʒ/", "/ʒ/", "/d/" }),
            new("Từ 'church' có phụ âm đầu nào?", "Chọn ký hiệu IPA", "/tʃ/", new[] { "/tʃ/", "/ʃ/", "/dʒ/", "/k/" }),
            new("Từ 'sing' có phụ âm cuối nào?", "Chọn ký hiệu IPA", "/ŋ/", new[] { "/n/", "/ŋ/", "/ɡ/", "/m/" }),
            new("Từ 'food' có nguyên âm nào?", "Chọn ký hiệu IPA", "/uː/", new[] { "/ʊ/", "/uː/", "/ɔː/", "/ʌ/" }),
            new("/eɪ/ là nguyên âm đôi trong từ nào?", "Diphthong", "say", new[] { "see", "say", "so", "sue" }),
            new("/aɪ/ là nguyên âm đôi trong từ nào?", "Diphthong", "my", new[] { "me", "my", "may", "moo" }),
            new("Âm /ð/ (rung) có trong từ nào?", "Voiced th", "this", new[] { "thin", "this", "think", "three" }),
            new("Từ 'about' bắt đầu bằng nguyên âm nào?", "Schwa", "/ə/", new[] { "/æ/", "/ɑː/", "/ə/", "/eɪ/" }),
            new("Âm /v/ và /f/ khác nhau ở?", "Phân biệt", "Rung dây thanh", new[] { "Vị trí lưỡi", "Rung dây thanh", "Hình miệng", "Luồng hơi" }),
        };

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
                        Icon = "🗣",
                        Title = isVN ? "️ Luyện phát âm chuẩn bản xứ" : "Native Pronunciation Practice",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_ipa_1_{suffix}.png",
                        Description = isVN 
                            ? "Luyện nói theo bảng phiên âm từng nguyên âm và phụ âm giúp sửa triệt để khẩu hình răng-môi-lưỡi, đạt giọng điệu chuẩn xác." 
                            : "Practice speaking based on IPA vowels and consonants to correct mouth posture and achieve a native accent."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📖",
                        Title = isVN ? "Tra từ điển & Tự học" : "Dictionary Lookup & Self-Study",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_ipa_2_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng ký hiệu phiên âm quốc tế để tự học cách phát âm của từ mới từ từ điển chính quy mà không cần người hướng dẫn." 
                            : "Use phonetic symbols to learn how to pronounce new words from dictionaries without a teacher."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🩺",
                        Title = isVN ? "Trị liệu & Chữa phát âm" : "Speech Therapy",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_ipa_3_{suffix}.png",
                        Description = isVN 
                            ? "Ứng dụng trong y tế và giáo dục đặc biệt để chữa các tật phát âm (nói ngọng, lệch khẩu hình cơ môi hoặc răng)." 
                            : "Applied in medicine and special education to correct speech impediments and articulation habits."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌐",
                        Title = isVN ? "Học tập đa ngôn ngữ" : "Multilingual Learning",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_ipa_4_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng ký hiệu IPA như một ngôn ngữ chung để so sánh, đối chiếu và tiếp thu nhanh các ngôn ngữ khác ngoài tiếng Anh." 
                            : "Use IPA symbols as a universal phonetic alphabet to compare, contrast, and rapidly acquire other languages besides English."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🤖",
                        Title = isVN ? "Công nghệ Nhận diện giọng nói" : "Speech Recognition Technology",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_ipa_5_{suffix}.png",
                        Description = isVN 
                            ? "Ứng dụng IPA trong việc dạy AI, máy học để nhận diện giọng nói, phân tích ngữ điệu và xây dựng công cụ tổng hợp giọng nói." 
                            : "Apply IPA to train AI and machine learning models for speech recognition, accent analysis, and speech synthesis systems."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏫",
                        Title = isVN ? "Soạn bài giảng & Sách giáo khoa" : "Lesson Planning & Curriculum Design",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_ipa_6_{suffix}.png",
                        Description = isVN 
                            ? "Giáo viên dùng ký tự IPA để thiết kế giáo án chuẩn, chú thích từ mới trực quan giúp học sinh phổ thông dễ tiếp thu bài học." 
                            : "Teachers use IPA symbols to create standardized lesson plans and visual phonetic annotations, helping students acquire new vocabulary easily."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎙️",
                        Title = isVN ? "Luyện giọng lồng tiếng" : "Voice Acting & Dubbing",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_ipa_7_{suffix}.png",
                        Description = isVN 
                            ? "Áp dụng các ký tự phiên âm quốc tế để điều chỉnh khẩu hình và phát âm chuẩn xác ngữ điệu nhân vật trong điện ảnh." 
                            : "Apply phonetic symbols to adjust mouth shapes and accurately dub character voice inflections in cinema."
                    }
                };

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for IpaTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewTable == null || viewQuiz == null || viewMinPairs == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewTable.Visibility = Visibility.Collapsed;
            viewQuiz.Visibility = Visibility.Collapsed;
            viewMinPairs.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewTable.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewQuiz.Visibility = Visibility.Visible;
                    break;
                case 3:
                    viewMinPairs.Visibility = Visibility.Visible;
                    break;
                case 4:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}