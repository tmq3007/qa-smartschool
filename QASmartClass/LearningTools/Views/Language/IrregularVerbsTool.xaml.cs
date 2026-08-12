using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Speech.Synthesis;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Language
{
    public partial class IrregularVerbsTool : BaseToolControl
    {
        private readonly Random _rng = new();
        private int _cardIndex;
        private bool _cardRevealed;
        private SpeechSynthesizer? _synth;
        private string _selectedFilter = "All";

        // Quiz state
        private int _quizIdx, _quizCorrect, _quizTotal;
        private readonly List<int> _quizOrder = new();
        private bool _quizActive;
        private System.Windows.Threading.DispatcherTimer? _nextQuestionTimer;

        // Thẻ công thức quy luật nhóm động từ
        private Border? _cardAAA;
        private Border? _cardABB;
        private Border? _cardABA;
        private Border? _cardABC;

        public IrregularVerbsTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextTable != null) menuTextTable.Text = isVN ? "Bảng tra cứu" : "Verb Chart";
                if (menuTextFlashcard != null) menuTextFlashcard.Text = isVN ? "Thẻ ghi nhớ" : "Flashcard";
                if (menuTextQuiz != null) menuTextQuiz.Text = isVN ? "Trắc nghiệm" : "Quiz";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                try
                {
                    _synth = new SpeechSynthesizer();
                    _synth.SelectVoiceByHints(VoiceGender.Neutral, VoiceAge.Adult, 0, new System.Globalization.CultureInfo("en-US"));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to initialize SpeechSynthesizer: " + ex.Message);
                }

                BuildFormulaCards();
                ApplyFilterAndSearch();
                ShowCard(0);
                TouchTextPad.Attach(txtVerbSearch, mode: "text");
                TouchTextPad.Attach(txtVerbQuizPast, mode: "text");
                TouchTextPad.Attach(txtVerbQuizPP, mode: "text");

                // Đăng ký sự kiện Enter cho TextBox Quiz
                txtVerbQuizPast.KeyDown += VerbQuizPast_KeyDown;
                txtVerbQuizPP.KeyDown += VerbQuizPP_KeyDown;

                LoadPracticalApps();
            };

            Unloaded += (_, _) =>
            {
                _synth?.Dispose();
                _nextQuestionTimer?.Stop();
                _nextQuestionTimer = null;
            };
        }

        /// <summary>Builds formula cards for verb pattern groups.</summary>
        private void BuildFormulaCards()
        {
            if (formulaCardsPanel == null) return;

            _cardAAA = CreateFormulaCard(
                "🔄", "AAA: cut → cut → cut",
                "Cả 3 dạng giống nhau (hit, put, let, shut, set...)",
                "#C62828", "#E53935");
            _cardAAA.Margin = new Thickness(0, 0, 6, 12);

            _cardABB = CreateFormulaCard(
                "🔀", "ABB: buy → bought → bought",
                "Dạng quá khứ (V2) giống phân từ (V3), khác nguyên thể (V1)",
                "#1565C0", "#1976D2");
            _cardABB.Margin = new Thickness(6, 0, 6, 12);

            _cardABA = CreateFormulaCard(
                "🔁", "ABA: run → ran → run",
                "Dạng nguyên thể (V1) giống phân từ (V3), khác quá khứ (V2)",
                "#7B1FA2", "#8E24AA");
            _cardABA.Margin = new Thickness(6, 0, 6, 12);

            _cardABC = CreateFormulaCard(
                "🔃", "ABC: go → went → gone",
                "Cả 3 dạng hoàn toàn khác nhau (see, take, write, give...)",
                "#2E7D32", "#388E3C");
            _cardABC.Margin = new Thickness(6, 0, 0, 12);

            formulaCardsPanel.Children.Add(_cardAAA);
            formulaCardsPanel.Children.Add(_cardABB);
            formulaCardsPanel.Children.Add(_cardABA);
            formulaCardsPanel.Children.Add(_cardABC);
        }

        // ═══════════════════════════════════════════════════════════
        //  VERB DATA — 120+ động từ
        // ═══════════════════════════════════════════════════════════

        private static readonly (string Base, string Past, string PP, string Vi)[] Verbs =
        {
            ("be","was/were","been","thì, là, ở"),
            ("beat","beat","beaten","đánh, đánh bại"),
            ("become","became","become","trở thành"),
            ("begin","began","begun","bắt đầu"),
            ("bend","bent","bent","uốn cong, cúi xuống"),
            ("bet","bet","bet","đánh cược, cá cược"),
            ("bite","bit","bitten","cắn, ngoạm"),
            ("bleed","bled","bled","chảy máu, đổ máu"),
            ("blow","blew","blown","thổi"),
            ("break","broke","broken","vỡ, gãy"),
            ("breed","bred","bred","nuôi dưỡng, nhân giống, sinh sản"),
            ("bring","brought","brought","mang đến"),
            ("broadcast","broadcast","broadcast","phát sóng"),
            ("build","built","built","xây dựng"),
            ("burn","burnt/burned","burnt/burned","đốt cháy, thiêu rụi"),
            ("burst","burst","burst","nổ tung"),
            ("buy","bought","bought","mua"),
            ("catch","caught","caught","bắt"),
            ("choose","chose","chosen","chọn"),
            ("come","came","come","đến"),
            ("cost","cost","cost","tốn, có giá"),
            ("cut","cut","cut","cắt"),
            ("deal","dealt","dealt","giải quyết, giao dịch, đối phó"),
            ("dig","dug","dug","đào, bới"),
            ("do","did","done","làm"),
            ("draw","drew","drawn","vẽ"),
            ("dream","dreamt/dreamed","dreamt/dreamed","mơ, mơ thấy"),
            ("drink","drank","drunk","uống"),
            ("drive","drove","driven","lái xe"),
            ("eat","ate","eaten","ăn"),
            ("fall","fell","fallen","rơi, ngã"),
            ("feed","fed","fed","cho ăn, nuôi nấng"),
            ("feel","felt","felt","cảm thấy"),
            ("fight","fought","fought","chiến đấu"),
            ("find","found","found","tìm thấy"),
            ("fly","flew","flown","bay"),
            ("forbid","forbade","forbidden","cấm"),
            ("forget","forgot","forgotten","quên"),
            ("forgive","forgave","forgiven","tha thứ"),
            ("freeze","froze","frozen","đóng băng"),
            ("get","got","got/gotten","lấy, nhận, có được"),
            ("give","gave","given","cho, tặng"),
            ("go","went","gone","đi"),
            ("grow","grew","grown","trồng, lớn lên"),
            ("hang","hung","hung","treo, móc"),
            ("have","had","had","có"),
            ("hear","heard","heard","nghe"),
            ("hide","hid","hidden","giấu"),
            ("hit","hit","hit","đánh, đập"),
            ("hold","held","held","giữ, cầm"),
            ("hurt","hurt","hurt","làm đau"),
            ("keep","kept","kept","giữ"),
            ("kneel","knelt","knelt","quỳ gối, quỳ xuống"),
            ("know","knew","known","biết"),
            ("lay","laid","laid","đặt, để"),
            ("lead","led","led","dẫn dắt, dẫn đầu, lãnh đạo"),
            ("learn","learnt/learned","learnt/learned","học, học hỏi"),
            ("leave","left","left","rời đi"),
            ("lend","lent","lent","cho mượn"),
            ("let","let","let","cho phép, để cho"),
            ("lie","lay","lain","nằm"),
            ("light","lit","lit","thắp sáng"),
            ("lose","lost","lost","mất, thua"),
            ("make","made","made","làm, chế tạo"),
            ("mean","meant","meant","có nghĩa là, muốn nói"),
            ("meet","met","met","gặp"),
            ("overcome","overcame","overcome","vượt qua"),
            ("pay","paid","paid","trả tiền"),
            ("put","put","put","đặt, để"),
            ("quit","quit","quit","từ bỏ, bỏ cuộc, nghỉ"),
            ("read","read","read","đọc"),
            ("ride","rode","ridden","cưỡi, đi xe"),
            ("ring","rang","rung","rung, reo, gọi điện"),
            ("rise","rose","risen","mọc, tăng lên, đứng dậy"),
            ("run","ran","run","chạy"),
            ("say","said","said","nói"),
            ("see","saw","seen","nhìn thấy"),
            ("seek","sought","sought","tìm kiếm, mưu cầu"),
            ("sell","sold","sold","bán"),
            ("send","sent","sent","gửi"),
            ("set","set","set","đặt, để, thiết lập"),
            ("shake","shook","shaken","lắc, rung, bắt tay"),
            ("shine","shone","shone","tỏa sáng"),
            ("shoot","shot","shot","bắn"),
            ("show","showed","shown/showed","cho xem, trình bày"),
            ("shut","shut","shut","đóng, khép lại"),
            ("sing","sang","sung","hát"),
            ("sink","sank","sunk","chìm, đắm"),
            ("sit","sat","sat","ngồi"),
            ("sleep","slept","slept","ngủ"),
            ("slide","slid","slid","trượt, lướt"),
            ("smell","smelt/smelled","smelt/smelled","ngửi, ngửi thấy"),
            ("speak","spoke","spoken","nói"),
            ("spend","spent","spent","tiêu, dành"),
            ("spill","spilt/spilled","spilt/spilled","làm tràn, đổ ra"),
            ("split","split","split","chia ra, tách ra, chẻ"),
            ("spread","spread","spread","lan truyền, trải ra, lan rộng"),
            ("stand","stood","stood","đứng"),
            ("steal","stole","stolen","ăn cắp"),
            ("stick","stuck","stuck","dính"),
            ("sting","stung","stung","đốt, chích (côn trùng)"),
            ("strike","struck","struck","đánh, đình công"),
            ("swear","swore","sworn","thề, chửi thề"),
            ("sweep","swept","swept","quét"),
            ("swim","swam","swum","bơi"),
            ("swing","swung","swung","đung đưa, xoay"),
            ("take","took","taken","lấy"),
            ("teach","taught","taught","dạy"),
            ("tear","tore","torn","xé, làm rách"),
            ("tell","told","told","kể, nói, bảo"),
            ("think","thought","thought","nghĩ"),
            ("throw","threw","thrown","ném"),
            ("understand","understood","understood","hiểu"),
            ("wake","woke","woken","thức dậy"),
            ("wear","wore","worn","mặc"),
            ("win","won","won","thắng"),
            ("wind","wound","wound","cuộn, cuốn, lượn quanh"),
            ("withdraw","withdrew","withdrawn","rút lui, rút tiền, rút khỏi"),
            ("write","wrote","written","viết"),
        };

        // ═══════════════════════════════════════════════════════════
        //  BẢNG TRA CỨU & BỘ LỌC NHÓM
        // ═══════════════════════════════════════════════════════════

        private void VerbSearch_Changed(object sender, TextChangedEventArgs e)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 1)
            {
                sideMenu.SelectedIndex = 0;
            }
            ApplyFilterAndSearch();
        }

        private void Filter_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                _selectedFilter = rb.Name switch
                {
                    "radFilterAAA" => "AAA",
                    "radFilterABB" => "ABB",
                    "radFilterABA" => "ABA",
                    "radFilterABC" => "ABC",
                    _ => "All"
                };
                ApplyFilterAndSearch();
            }
        }

        private void ApplyFilterAndSearch()
        {
            if (txtVerbSearch == null) return;
            string q = txtVerbSearch.Text?.Trim().ToLowerInvariant() ?? "";

            var filtered = Verbs.Where(v =>
            {
                // 1. Kiểm tra tìm kiếm từ khóa
                bool matchQuery = string.IsNullOrEmpty(q) ||
                                  v.Base.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                  v.Past.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                  v.PP.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                  v.Vi.Contains(q, StringComparison.OrdinalIgnoreCase);

                if (!matchQuery) return false;
                if (_selectedFilter == "All") return true;

                // 2. Lọc theo nhóm cấu trúc từ
                string pattern = GetVerbPattern(v.Base, v.Past, v.PP);
                return pattern == _selectedFilter;
            }).ToArray();

            RenderVerbTable(filtered);
            UpdateFormulaCardsVisibility();
        }

        private void UpdateFormulaCardsVisibility()
        {
            if (_cardAAA == null || _cardABB == null || _cardABA == null || _cardABC == null) return;

            _cardAAA.Visibility = (_selectedFilter == "All" || _selectedFilter == "AAA") ? Visibility.Visible : Visibility.Collapsed;
            _cardABB.Visibility = (_selectedFilter == "All" || _selectedFilter == "ABB") ? Visibility.Visible : Visibility.Collapsed;
            _cardABA.Visibility = (_selectedFilter == "All" || _selectedFilter == "ABA") ? Visibility.Visible : Visibility.Collapsed;
            _cardABC.Visibility = (_selectedFilter == "All" || _selectedFilter == "ABC") ? Visibility.Visible : Visibility.Collapsed;
        }

        private static string GetVerbPattern(string baseForm, string pastForm, string ppForm)
        {
            string p = pastForm.Split('/')[0].Trim().ToLowerInvariant();
            string pp = ppForm.Split('/')[0].Trim().ToLowerInvariant();
            string b = baseForm.Trim().ToLowerInvariant();

            if (b == p && b == pp) return "AAA";
            if (b == p && b != pp) return "AAB"; // beat -> beat -> beaten
            if (b == pp && b != p) return "ABA";
            if (p == pp && b != p) return "ABB";
            return "ABC";
        }

        private void RenderVerbTable((string Base, string Past, string PP, string Vi)[] verbs)
        {
            if (verbTablePanel == null) return;
            verbTablePanel.Children.Clear();

            // Header
            verbTablePanel.Children.Add(MakeVerbRow("Nguyên thể (V1)", "Quá khứ đơn (V2)", "Quá khứ phân từ (V3)", "Ý nghĩa", true));

            for (int i = 0; i < verbs.Length; i++)
            {
                var v = verbs[i];
                var row = MakeVerbRow(v.Base, v.Past, v.PP, v.Vi, false);
                row.Background = i % 2 == 0 ? Brushes.White : new SolidColorBrush(Color.FromRgb(245, 248, 255));
                verbTablePanel.Children.Add(row);
            }

            verbTablePanel.Children.Add(new TextBlock
            {
                Text = $"Tổng: {verbs.Length} động từ",
                FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                Margin = new Thickness(8, 12, 0, 12)
            });
        }

        private Border MakeVerbRow(string c1, string c2, string c3, string c4, bool isHeader)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.5, GridUnitType.Star) }); // Cột Loa phát âm

            var texts = new[] { c1, c2, c3, c4 };
            var colors = isHeader
                ? new[] { "#1565C0", "#1565C0", "#1565C0", "#1565C0" }
                : new[] { "#1A237E", "#C62828", "#2E7D32", "#424242" };

            // Render 4 cột nội dung text
            for (int i = 0; i < 4; i++)
            {
                var color = (Color)ColorConverter.ConvertFromString(colors[i]);
                var tb = new TextBlock
                {
                    Text = texts[i],
                    FontSize = isHeader ? 16 : 15, // Đảm bảo to rõ ràng cho lớp học
                    FontWeight = isHeader || i == 0 ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = isHeader ? Brushes.White : new SolidColorBrush(color),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(12, 0, 0, 0),
                    TextWrapping = TextWrapping.Wrap // Chống tràn văn bản nghĩa tiếng Việt dài
                };
                Grid.SetColumn(tb, i);
                grid.Children.Add(tb);
            }

            // Render cột thứ 5: Loa phát âm (TTS)
            if (isHeader)
            {
                var tbHeader = new TextBlock
                {
                    Text = "Nghe",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(tbHeader, 4);
                grid.Children.Add(tbHeader);
            }
            else
            {
                var btnSpeak = new Button
                {
                    Content = "🔊",
                    FontSize = 14,
                    Padding = new Thickness(4, 2, 4, 2),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Nghe cách phát âm cả 3 dạng",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                // Dùng cụm phát âm: "Base, Past, PP" để tạo liên kết âm thanh sư phạm
                string pronunciationText = $"{c1}, {c2.Split('/')[0]}, {c3.Split('/')[0]}";
                btnSpeak.Click += (s, e) => Speak(pronunciationText);
                Grid.SetColumn(btnSpeak, 4);
                grid.Children.Add(btnSpeak);
            }

            return new Border
            {
                Child = grid,
                Background = isHeader ? new SolidColorBrush(Color.FromRgb(25, 118, 210)) : Brushes.Transparent,
                Padding = new Thickness(0, 10, 0, 10), // Tăng khoảng cách dòng
                CornerRadius = isHeader ? new CornerRadius(8, 8, 0, 0) : new CornerRadius(0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  HỖ TRỢ TTS PHÁT ÂM
        // ═══════════════════════════════════════════════════════════

        private void Speak(string text)
        {
            if (_synth == null || string.IsNullOrEmpty(text)) return;
            try
            {
                _synth.SpeakAsyncCancelAll();

                // Tách các cụm từ theo dấu phẩy trước để bảo vệ các cột từ
                var parts = text.Split(',');
                var cleanedParts = new List<string>();
                foreach (var part in parts)
                {
                    // Lấy dạng đầu tiên trước dấu '/' nếu có phương án thay thế
                    var firstOpt = part.Split('/')[0]
                                       .Replace("Quá khứ (V2):", "")
                                       .Replace("Phân từ II (V3):", "")
                                       .Replace("Past:", "")
                                       .Replace("PP:", "")
                                       .Trim();
                    if (!string.IsNullOrEmpty(firstOpt))
                    {
                        cleanedParts.Add(firstOpt);
                    }
                }
                string cleanText = string.Join(", ", cleanedParts);
                _synth.SpeakAsync(cleanText);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("TTS Speak error: " + ex.Message);
            }
        }

        private void SpeakCard_Click(object sender, RoutedEventArgs e)
        {
            var v = Verbs[_cardIndex];
            if (_cardRevealed)
                Speak($"{v.Base}, {v.Past}, {v.PP}");
            else
                Speak(v.Base);
            e.Handled = true;
        }

        private void SpeakQuiz_Click(object sender, RoutedEventArgs e)
        {
            if (!_quizActive || _quizIdx >= _quizOrder.Count) return;
            var v = Verbs[_quizOrder[_quizIdx]];
            Speak(v.Base);
            e.Handled = true;
        }

        // ═══════════════════════════════════════════════════════════
        //  FLASH CARDS
        // ═══════════════════════════════════════════════════════════

        private void ShowCard(int index)
        {
            _cardIndex = ((index % Verbs.Length) + Verbs.Length) % Verbs.Length;
            _cardRevealed = false;
            var v = Verbs[_cardIndex];
            txtVerbBase.Text = v.Base;
            txtVerbMeaning.Text = v.Vi;
            txtVerbPast.Text = "Quá khứ (V2): ???";
            txtVerbPast.Foreground = Brushes.Gray;
            txtVerbPast.FontWeight = FontWeights.Normal;
            txtVerbPP.Text = "Phân từ II (V3): ???";
            txtVerbPP.Foreground = Brushes.Gray;
            txtVerbPP.FontWeight = FontWeights.Normal;
        }

        private void VerbCard_Click(object sender, MouseButtonEventArgs e)
        {
            // Tránh kích hoạt lật thẻ nếu click trúng nút Loa phát âm
            if (e.OriginalSource is Button || (e.OriginalSource is DependencyObject dep && VisualTreeHelper.GetParent(dep) is Button))
            {
                return;
            }

            if (!_cardRevealed)
            {
                var v = Verbs[_cardIndex];
                txtVerbPast.Text = $"Quá khứ (V2): {v.Past.Replace("/", " / ")}";
                txtVerbPast.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                txtVerbPast.FontWeight = FontWeights.Bold;
                txtVerbPP.Text = $"Phân từ II (V3): {v.PP.Replace("/", " / ")}";
                txtVerbPP.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                txtVerbPP.FontWeight = FontWeights.Bold;
                _cardRevealed = true;

                // Tự động phát âm cả chuỗi liên kết khi lật mặt sau như tài liệu mô tả
                Speak($"{v.Base}, {v.Past}, {v.PP}");
            }
            else
            {
                ShowCard(_cardIndex + 1);
            }
        }

        private void VerbPrev_Click(object sender, RoutedEventArgs e) => ShowCard(_cardIndex - 1);
        private void VerbNext_Click(object sender, RoutedEventArgs e) => ShowCard(_cardIndex + 1);
        private void VerbRandom_Click(object sender, RoutedEventArgs e) => ShowCard(_rng.Next(Verbs.Length));

        // ═══════════════════════════════════════════════════════════
        //  QUIZ — điền Past và PP & Sửa lỗi Race Condition / Enter Key
        // ═══════════════════════════════════════════════════════════

        private void VerbQuizPast_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                txtVerbQuizPP.Focus();
            }
        }

        private void VerbQuizPP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                VerbQuizCheck_Click(sender, e);
            }
        }

        private void VerbQuizStart_Click(object sender, RoutedEventArgs e)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 3)
            {
                sideMenu.SelectedIndex = 1;
            }
            _nextQuestionTimer?.Stop();
            _nextQuestionTimer = null;

            _quizCorrect = 0;
            _quizTotal = 0;
            _quizActive = true;

            _quizOrder.Clear();
            var indices = Enumerable.Range(0, Verbs.Length).OrderBy(_ => _rng.Next()).Take(15).ToList();
            _quizOrder.AddRange(indices);
            _quizIdx = 0;

            txtVerbQuizPast.Visibility = Visibility.Visible;
            txtVerbQuizPP.Visibility = Visibility.Visible;
            btnVerbQuizCheck.Visibility = Visibility.Visible;
            btnSpeakQuiz.Visibility = Visibility.Visible;
            btnVerbQuizStart.Content = "🔄 Chơi lại";

            ShowQuizQuestion();
        }

        private void ShowQuizQuestion()
        {
            if (_quizIdx >= _quizOrder.Count)
            {
                _quizActive = false;
                txtVerbQuizBase.Text = $"🎉 {_quizCorrect}/{_quizOrder.Count} đúng!";
                txtVerbQuizQ.Text = "Hoàn thành!";
                txtVerbQuizPast.Visibility = Visibility.Collapsed;
                txtVerbQuizPP.Visibility = Visibility.Collapsed;
                btnVerbQuizCheck.Visibility = Visibility.Collapsed;
                btnSpeakQuiz.Visibility = Visibility.Collapsed;
                txtVerbQuizFeedback.Text = "";
                return;
            }

            // Kích hoạt lại toàn bộ ô nhập văn bản và nút kiểm tra
            txtVerbQuizPast.IsEnabled = true;
            txtVerbQuizPP.IsEnabled = true;
            btnVerbQuizCheck.IsEnabled = true;

            var v = Verbs[_quizOrder[_quizIdx]];
            txtVerbQuizQ.Text = $"Câu {_quizIdx + 1}/{_quizOrder.Count} — Nghĩa: {v.Vi}";
            txtVerbQuizBase.Text = v.Base;
            txtVerbQuizPast.Text = "";
            txtVerbQuizPP.Text = "";
            txtVerbQuizFeedback.Text = "";
            btnSpeakQuiz.Visibility = Visibility.Visible;
            txtVerbQuizPast.Focus();

            UpdateQuizScore();
        }

        private void VerbQuizCheck_Click(object sender, RoutedEventArgs e)
        {
            if (!_quizActive || _quizIdx >= _quizOrder.Count || !btnVerbQuizCheck.IsEnabled) return;

            // Vô hiệu hóa ngay lập tức các nút nộp bài và ô nhập để chặn Race Condition
            txtVerbQuizPast.IsEnabled = false;
            txtVerbQuizPP.IsEnabled = false;
            btnVerbQuizCheck.IsEnabled = false;

            var v = Verbs[_quizOrder[_quizIdx]];
            string userPast = txtVerbQuizPast.Text.Trim().ToLowerInvariant();
            string userPP = txtVerbQuizPP.Text.Trim().ToLowerInvariant();

            bool pastOk = CheckAnswer(userPast, v.Past);
            bool ppOk = CheckAnswer(userPP, v.PP);

            _quizTotal++;
            if (pastOk && ppOk)
            {
                _quizCorrect++;
                txtVerbQuizFeedback.Text = "✅ Chính xác!";
                txtVerbQuizFeedback.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            }
            else
            {
                txtVerbQuizFeedback.Text = $"❌ Đáp án: {v.Past.Replace("/", " / ")} — {v.PP.Replace("/", " / ")}";
                txtVerbQuizFeedback.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
            }

            _quizIdx++;
            UpdateQuizScore();

            // Quản lý timer chuyển câu hỏi ở mức lớp tập trung chống trùng lặp luồng
            _nextQuestionTimer?.Stop();
            _nextQuestionTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1200)
            };
            _nextQuestionTimer.Tick += (s, ev) =>
            {
                _nextQuestionTimer.Stop();
                _nextQuestionTimer = null;
                ShowQuizQuestion();
            };
            _nextQuestionTimer.Start();
        }

        private static bool CheckAnswer(string userAnswer, string correct)
        {
            var options = correct.ToLowerInvariant().Split('/');
            return options.Any(o => o.Trim() == userAnswer);
        }

        private void UpdateQuizScore()
        {
            txtVerbQuizScore.Text = $"Điểm: {_quizCorrect}/{_quizTotal}";
            txtVerbQuizProgress.Text = $"Câu {System.Math.Min(_quizIdx + 1, _quizOrder.Count)}/{_quizOrder.Count}";
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
                        Icon = "📜",
                        Title = isVN ? "Viết nhật ký & Kể chuyện" : "Journaling & Storytelling",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_irregular_verbs_1_{suffix}.png",
                        Description = isVN
                            ? "Sử dụng thì Quá khứ đơn (Past Simple - V2) để kể lại các câu chuyện lịch sử hoặc viết nhật ký cá nhân về các sự việc đã diễn ra."
                            : "Use Past Simple (V2) to tell historical stories or write personal journal entries about events that already occurred."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💼",
                        Title = isVN ? "Viết CV & Kinh nghiệm" : "Resume & Life Experiences",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_irregular_verbs_2_{suffix}.png",
                        Description = isVN
                            ? "Sử dụng thì Hiện tại hoàn thành (Past Participle - V3) để mô tả những trải nghiệm, thành tựu học tập hoặc kinh nghiệm làm việc đã tích lũy trong cuộc sống."
                            : "Use Present Perfect (V3) to describe experiences, academic achievements, or work history accumulated throughout life on resumes."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📅",
                        Title = isVN ? "Lập lịch trình & Mục tiêu" : "Scheduling & Deadlines",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_irregular_verbs_3_{suffix}.png",
                        Description = isVN
                            ? "Sử dụng thì Tương lai hoàn thành (V3) để xác định các mốc thời gian sẽ hoàn thành một công việc nào đó trong tương lai (Ví dụ: 'I will have finished...')."
                            : "Use Future Perfect (V3) to specify deadlines when a goal or task will have been completed (e.g., 'I will have finished...')."
                    }
                };

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for IrregularVerbsTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewTable == null || viewFlashcard == null || viewQuiz == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewTable.Visibility = Visibility.Collapsed;
            viewFlashcard.Visibility = Visibility.Collapsed;
            viewQuiz.Visibility = Visibility.Collapsed;
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
                    viewFlashcard.Visibility = Visibility.Visible;
                    break;
                case 3:
                    viewQuiz.Visibility = Visibility.Visible;
                    break;
                case 4:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}