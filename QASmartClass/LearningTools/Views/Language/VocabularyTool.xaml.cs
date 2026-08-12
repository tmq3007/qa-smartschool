using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Language
{
    public partial class VocabularyTool : BaseToolControl, IDisposable
    {
        private readonly Random _rng = new();
        private int _quizIdx, _quizCorrect, _quizTotal;
        private List<VWord> _quizWords = new();
        private bool _quizActive, _isMCQuiz, _isEvaluating;
        // Flashcard
        private List<VWord> _fcWords = new();
        private int _fcIdx;
        private bool _fcFlipped;
        private SpeechSynthesizer _synthesizer = new SpeechSynthesizer();
        private VWord[] _currentFilteredWords = Array.Empty<VWord>();
        private bool _isFcAnimating = false;
        private int _displayLimit = 100;

        public VocabularyTool()
        {
            InitializeComponent();
            Loaded += (_, _) => Init();
            lstVocab.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(LstVocab_ScrollChanged));
        }

        private void ConfigureSynthesizer()
        {
            try
            {
                var voice = _synthesizer.GetInstalledVoices()
                    .FirstOrDefault(v => v.VoiceInfo.Culture.Name.StartsWith("en-US", StringComparison.OrdinalIgnoreCase) && v.Enabled);
                if (voice != null)
                {
                    _synthesizer.SelectVoice(voice.VoiceInfo.Name);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("English US voice not found for SpeechSynthesizer");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ConfigureSynthesizer error: " + ex.Message);
            }
        }

        private string SafeGetClipboardText()
        {
            try
            {
                return Clipboard.GetText();
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                MessageBox.Show("Bộ nhớ tạm (Clipboard) đang bận hoặc bị khóa bởi ứng dụng khác. Vui lòng thử lại sau.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return string.Empty;
            }
        }

        private void SafeSetClipboardText(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                MessageBox.Show("Không thể ghi vào bộ nhớ tạm (Clipboard) lúc này. Vui lòng thử lại sau.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task LoadVocabularyDataAsync()
        {
            if (AllCategories != null && AllCategories.Length > 0)
            {
                QASmartClass.LearningTools.Models.SRSTracker.SetValidWords(AllCategories.SelectMany(c => c.Words).Select(w => w.En));
                PopulateTopics();
                RenderVocab();
                return;
            }

            if (loadingOverlay != null) loadingOverlay.Visibility = Visibility.Visible;

            // Disable filters while loading
            if (cboLevel != null) cboLevel.IsEnabled = false;
            if (cboTopic != null) cboTopic.IsEnabled = false;
            if (txtSearch != null) txtSearch.IsEnabled = false;
            if (cboStatus != null) cboStatus.IsEnabled = false;
            if (cboAlphabet != null) cboAlphabet.IsEnabled = false;
            if (cboSort != null) cboSort.IsEnabled = false;

            try
            {
                AllCategories = await Task.Run(() =>
                {
                    try
                    {
                        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        var dataPath = System.IO.Path.Combine(baseDir, "Assets", "Data", "Language", "VocabularyData.json");
                        if (!System.IO.File.Exists(dataPath))
                        {
                            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                            var dir = System.IO.Path.GetDirectoryName(exePath);
                            if (!string.IsNullOrEmpty(dir))
                            {
                                dataPath = System.IO.Path.Combine(dir, "Assets", "Data", "Language", "VocabularyData.json");
                            }
                        }

                        if (System.IO.File.Exists(dataPath))
                        {
                            var json = System.IO.File.ReadAllText(dataPath);
                            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            return System.Text.Json.JsonSerializer.Deserialize<VCat[]>(json, options) ?? Array.Empty<VCat>();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error loading vocabulary data: " + ex.Message);
                    }
                    return Array.Empty<VCat>();
                });
            }
            catch
            {
                AllCategories = Array.Empty<VCat>();
            }

            if (loadingOverlay != null) loadingOverlay.Visibility = Visibility.Collapsed;

            // Enable filters after loading
            if (cboLevel != null) cboLevel.IsEnabled = true;
            if (cboTopic != null) cboTopic.IsEnabled = true;
            if (txtSearch != null) txtSearch.IsEnabled = true;
            if (cboStatus != null) cboStatus.IsEnabled = true;
            if (cboAlphabet != null) cboAlphabet.IsEnabled = true;
            if (cboSort != null) cboSort.IsEnabled = true;

            if (AllCategories != null && AllCategories.Length > 0)
            {
                QASmartClass.LearningTools.Models.SRSTracker.SetValidWords(AllCategories.SelectMany(c => c.Words).Select(w => w.En));
            }
            PopulateTopics();
            RenderVocab();
        }

        private void Init()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
            if (menuTextVocab != null) menuTextVocab.Text = isVN ? "Từ vựng" : "Vocabulary";
            if (menuTextFlashcard != null) menuTextFlashcard.Text = isVN ? "Thẻ ghi nhớ (Flashcard)" : "Flashcard";
            if (menuTextQuiz != null) menuTextQuiz.Text = isVN ? "Trắc nghiệm (Quiz)" : "Quiz";
            if (menuTextStats != null) menuTextStats.Text = isVN ? "Tiến độ học tập" : "Learning Progress";
            if (menuTextReading != null) menuTextReading.Text = isVN ? "Bài đọc học tập" : "Reading Practice";
            if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

            var blinkAnim = new DoubleAnimation(0.2, 1, TimeSpan.FromMilliseconds(600))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            iconFlip.BeginAnimation(OpacityProperty, blinkAnim);

            ConfigureSynthesizer();

            // Level combo
            var levels = new[] { "Tất cả", "A1 — Cơ bản", "A2 — Sơ cấp", "B1 — Trung cấp", "B2 — Trung cao", "C1 — Nâng cao", "C2 — Thành thạo" };
            foreach (var l in levels) cboLevel.Items.Add(new ComboBoxItem { Content = l });
            cboLevel.SelectedIndex = 0;
            
            // Alphabet combo
            if (cboAlphabet != null)
            {
                cboAlphabet.Items.Add(new ComboBoxItem { Content = "Tất cả" });
                for (char c = 'A'; c <= 'Z'; c++) cboAlphabet.Items.Add(new ComboBoxItem { Content = c.ToString() });
                cboAlphabet.SelectedIndex = 0;
            }

            if (txtSearch != null) TouchTextPad.Attach(txtSearch, mode: "text");
            if (txtVocabAnswer != null) TouchTextPad.Attach(txtVocabAnswer, mode: "text");

            LoadPracticalApps();

            _ = LoadVocabularyDataAsync();
        }

        private void PopulateTopics()
        {
            if (AllCategories == null) return;
            cboTopic.Items.Clear();
            string levelFilter = GetSelectedLevel();
            var filtered = levelFilter == "ALL" ? AllCategories : AllCategories.Where(c => c.Level == levelFilter).ToArray();
            cboTopic.Items.Add(new ComboBoxItem { Content = $"📚 Tất cả ({filtered.Sum(c => c.Words.Length)} từ)" });
            cboTopic.Items.Add(new ComboBoxItem { Content = $"📅 10 Từ Hôm Nay (Random theo ngày)" });
            cboTopic.Items.Add(new ComboBoxItem { Content = $"⭐ Danh sách của tôi (My List)" });
            foreach (var c in filtered)
                cboTopic.Items.Add(new ComboBoxItem { Content = $"{c.Icon} {c.Name} ({c.Words.Length})" });
            cboTopic.SelectedIndex = 0;
        }

        private string GetSelectedLevel()
        {
            int i = cboLevel?.SelectedIndex ?? 0;
            return i switch { 1 => "A1", 2 => "A2", 3 => "B1", 4 => "B2", 5 => "C1", 6 => "C2", _ => "ALL" };
        }

        private VWord[] GetCurrentWords()
        {
            if (AllCategories == null) return Array.Empty<VWord>();
            string lv = GetSelectedLevel();
            var cats = lv == "ALL" ? AllCategories : AllCategories.Where(c => c.Level == lv).ToArray();
            int topicIdx = cboTopic?.SelectedIndex ?? 0;
            
            IEnumerable<VWord> words;
            if (topicIdx <= 0) 
            {
                words = cats.SelectMany(c => c.Words);
            }
            else if (topicIdx == 1)
            {
                var all = cats.SelectMany(c => c.Words).ToArray();
                // Random based on current day of the year so it stays consistent throughout the day
                var rnd = new Random(DateTime.Now.DayOfYear + DateTime.Now.Year * 365);
                words = all.OrderBy(x => rnd.Next()).Take(10).ToArray();
            }
            else if (topicIdx == 2)
            {
                var customEn = QASmartClass.LearningTools.Models.SRSTracker.GetCustomList();
                var all = cats.SelectMany(c => c.Words);
                words = all.Where(w => customEn.Contains(w.En, StringComparer.OrdinalIgnoreCase));
            }
            else if (topicIdx - 3 < cats.Length) 
            {
                words = cats[topicIdx - 3].Words;
            }
            else 
            {
                words = Array.Empty<VWord>();
            }

            // Apply search
            string search = txtSearch?.Text?.Trim().ToLowerInvariant() ?? "";
            if (!string.IsNullOrEmpty(search))
            {
                words = words.Where(w => w.En.ToLowerInvariant().Contains(search) || w.Vi.ToLowerInvariant().Contains(search));
            }

            // Apply alphabet filter
            if (cboAlphabet != null && cboAlphabet.SelectedIndex > 0)
            {
                string letter = ((ComboBoxItem)cboAlphabet.SelectedItem).Content.ToString().ToLowerInvariant();
                words = words.Where(w => w.En.ToLowerInvariant().StartsWith(letter));
            }

            // Apply status filter
            if (cboStatus != null && cboStatus.SelectedIndex > 0)
            {
                var dueWords = QASmartClass.LearningTools.Models.SRSTracker.GetDueToday();
                if (cboStatus.SelectedIndex == 1) // Cần ôn tập
                {
                    words = words.Where(w => dueWords.Contains(w.En, StringComparer.OrdinalIgnoreCase));
                }
                else if (cboStatus.SelectedIndex == 2) // Đã học
                {
                    words = words.Where(w => QASmartClass.LearningTools.Models.SRSTracker.IsLearned(w.En));
                }
                else if (cboStatus.SelectedIndex == 3) // Chưa học
                {
                    words = words.Where(w => !QASmartClass.LearningTools.Models.SRSTracker.IsLearned(w.En));
                }
            }

            // Apply sort
            if (cboSort != null)
            {
                if (cboSort.SelectedIndex == 1) words = words.OrderBy(w => w.En);
                else if (cboSort.SelectedIndex == 2) words = words.OrderByDescending(w => w.En);
            }

            return words.ToArray();
        }

        private void Level_Changed(object s, SelectionChangedEventArgs e)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 1)
            {
                sideMenu.SelectedIndex = 0;
            }
            PopulateTopics();
        }

        private void Topic_Changed(object s, SelectionChangedEventArgs e)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 1)
            {
                sideMenu.SelectedIndex = 1;
            }
            RenderVocab();
        }

        private void Filter_Changed(object s, RoutedEventArgs e)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 1)
            {
                sideMenu.SelectedIndex = 1;
            }
            RenderVocab();
        }

        // ═══ RENDER VOCAB CARDS ═══
        private void RenderVocab(bool resetLimit = true)
        {
            if (lstVocab == null) return;
            
            _currentFilteredWords = GetCurrentWords();
            
            if (resetLimit)
            {
                _displayLimit = 100;
                lstVocab.Items.Clear();
            }
            
            int currentCount = lstVocab.Items.Count;
            int targetCount = System.Math.Min(_displayLimit, _currentFilteredWords.Length);
            
            for (int i = currentCount; i < targetCount; i++)
            {
                var w = _currentFilteredWords[i];
                var lvColor = LevelColor(w.Level);
                var border = new Border
                {
                    Background = Brushes.White, CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 10, 12, 10), Cursor = Cursors.Hand,
                    BorderBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)), BorderThickness = new Thickness(1)
                };
                
                var dp = new DockPanel();
                
                // Right side: Level Badge
                var badge = new Border { Background = new SolidColorBrush(lvColor), CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 2, 8, 2), VerticalAlignment = VerticalAlignment.Center };
                DockPanel.SetDock(badge, Dock.Right);
                badge.Child = new TextBlock { Text = w.Level, FontSize = 11, Foreground = Brushes.White, FontWeight = FontWeights.Bold };
                dp.Children.Add(badge);
                
                // Main content
                var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                
                var topRow = new StackPanel { Orientation = Orientation.Horizontal };
                var wordTxt = new TextBlock { Text = w.En, FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0,0,8,0) };
                wordTxt.Style = (Style)lstVocab.FindResource("VocabWordStyle");
                topRow.Children.Add(wordTxt);

                if (!string.IsNullOrEmpty(w.IPA))
                {
                    var ipaTxt = new TextBlock { Text = $"/{w.IPA}/", FontSize = 12, FontStyle = FontStyles.Italic, VerticalAlignment = VerticalAlignment.Bottom };
                    ipaTxt.Style = (Style)lstVocab.FindResource("VocabIpaStyle");
                    topRow.Children.Add(ipaTxt);
                }
                
                sp.Children.Add(topRow);
                
                var botRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
                if (!string.IsNullOrEmpty(w.PoS))
                {
                    var posTxt = new TextBlock { Text = $"{w.PoS} ", FontSize = 12, FontStyle = FontStyles.Italic };
                    posTxt.Style = (Style)lstVocab.FindResource("VocabPosStyle");
                    botRow.Children.Add(posTxt);
                }
                var viTxt = new TextBlock { Text = w.Vi, FontSize = 15, FontWeight = FontWeights.SemiBold };
                viTxt.Style = (Style)lstVocab.FindResource("VocabViStyle");
                botRow.Children.Add(viTxt);
                
                sp.Children.Add(botRow);
                dp.Children.Add(sp);
                border.Child = dp;
                
                // Add to list
                string secId = w.En.Replace(" ", "_").ToLowerInvariant();
                lstVocab.Items.Add(WrapWithSectionToolbar(border, "vocabulary", secId, $"{w.En} — {w.Vi}"));
            }
            
            if (resetLimit)
            {
                if (vocabDetailPanel != null) vocabDetailPanel.Visibility = Visibility.Hidden;
                if (lstVocab.Items.Count > 0)
                {
                    lstVocab.SelectedIndex = 0;
                    lstVocab.Focus();
                }
            }
        }

        private void LstVocab_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange > 0 && _currentFilteredWords != null)
            {
                double scrollableHeight = e.ExtentHeight - e.ViewportHeight;
                if (scrollableHeight > 0 && e.VerticalOffset >= scrollableHeight * 0.9)
                {
                    if (_displayLimit < _currentFilteredWords.Length)
                    {
                        _displayLimit += 100;
                        RenderVocab(resetLimit: false);
                    }
                }
            }
        }

        private void LstVocab_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            if (vocabDetailPanel == null) return;
            if (lstVocab.SelectedIndex < 0)
            {
                vocabDetailPanel.Visibility = Visibility.Hidden;
                return;
            }
            if (_currentFilteredWords != null && lstVocab.SelectedIndex < _currentFilteredWords.Length)
            {
                var w = _currentFilteredWords[lstVocab.SelectedIndex];
                vocabDetailPanel.Visibility = Visibility.Visible;
                txtDetailWord.Text = w.En;
                txtDetailIPA.Text = string.IsNullOrEmpty(w.IPA) ? "" : $"/{w.IPA}/";
                txtDetailPoS.Text = string.IsNullOrEmpty(w.PoS) ? "" : $"{w.PoS}";
                txtDetailMeaning.Text = w.Vi;
                txtDetailExample.Text = w.ExEn ?? "";
                
                btnPronounce.Tag = w.En;
                txtPronounceResult.Text = "";
                txtDetailLevel.Text = w.Level;
                badgeDetailLevel.Background = new SolidColorBrush(LevelColor(w.Level));
                // Advanced fields binding (Step 2: offline fallback image integration)
                bool imageLoaded = false;
                if (!string.IsNullOrEmpty(w.ImageUrl))
                {
                    try
                    {
                        var bitmap = new System.Windows.Media.Imaging.BitmapImage(new Uri(w.ImageUrl));
                        imgDetail.Source = bitmap;
                        bdrImage.Visibility = Visibility.Visible;
                        imageLoaded = true;
                    }
                    catch { }
                }

                if (!imageLoaded)
                {
                    try
                    {
                        var fallbackPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images", "vocab_fallback.png");
                        if (System.IO.File.Exists(fallbackPath))
                        {
                            imgDetail.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(fallbackPath));
                            bdrImage.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            imgDetail.Source = null;
                            bdrImage.Visibility = Visibility.Collapsed;
                        }
                    }
                    catch
                    {
                        imgDetail.Source = null;
                        bdrImage.Visibility = Visibility.Collapsed;
                    }
                }

                bool hasCollocations = w.Collocations != null && w.Collocations.Count > 0;
                pnlCollocations.Visibility = hasCollocations ? Visibility.Visible : Visibility.Collapsed;
                icCollocations.ItemsSource = w.Collocations;

                bool hasContext = w.ContextualExamples != null && w.ContextualExamples.Count > 0;
                pnlContext.Visibility = hasContext ? Visibility.Visible : Visibility.Collapsed;
                icContext.ItemsSource = w.ContextualExamples;

                bool hasSynonyms = w.Synonyms != null && w.Synonyms.Count > 0;
                pnlSynonyms.Visibility = hasSynonyms ? Visibility.Visible : Visibility.Collapsed;
                icSynonyms.ItemsSource = w.Synonyms;

                bool hasAntonyms = w.Antonyms != null && w.Antonyms.Count > 0;
                pnlAntonyms.Visibility = hasAntonyms ? Visibility.Visible : Visibility.Collapsed;
                icAntonyms.ItemsSource = w.Antonyms;

                pnlAdvancedData.Visibility = (hasCollocations || hasContext || hasSynonyms || hasAntonyms) ? Visibility.Visible : Visibility.Collapsed;
                
                chkLearned.Tag = w.En; // store current word En
                chkLearned.IsChecked = QASmartClass.LearningTools.Models.SRSTracker.IsLearned(w.En);
                btnToggleCustom.Tag = w.En;
                if (QASmartClass.LearningTools.Models.SRSTracker.IsInCustomList(w.En))
                {
                    btnToggleCustom.Content = "⭐ Bỏ khỏi My List";
                    btnToggleCustom.Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray
                }
                else
                {
                    btnToggleCustom.Content = "⭐ Thêm vào My List";
                    btnToggleCustom.Foreground = new SolidColorBrush(Color.FromRgb(255, 143, 0)); // Orange
                }
            }
        }

        private void BtnToggleCustom_Click(object s, RoutedEventArgs e)
        {
            if (btnToggleCustom.Tag is string wordEn)
            {
                bool added = QASmartClass.LearningTools.Models.SRSTracker.ToggleCustomList(wordEn);
                if (added)
                {
                    btnToggleCustom.Content = "⭐ Bỏ khỏi My List";
                    btnToggleCustom.Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray
                }
                else
                {
                    btnToggleCustom.Content = "⭐ Thêm vào My List";
                    btnToggleCustom.Foreground = new SolidColorBrush(Color.FromRgb(255, 143, 0)); // Orange
                }
            }
        }

        private void ChkLearned_Checked(object s, RoutedEventArgs e)
        {
            if (chkLearned.Tag is string wordEn)
            {
                QASmartClass.LearningTools.Models.SRSTracker.MarkLearned(wordEn);
            }
        }

        private void ChkLearned_Unchecked(object s, RoutedEventArgs e)
        {
            if (chkLearned.Tag is string wordEn)
            {
                QASmartClass.LearningTools.Models.SRSTracker.UnmarkLearned(wordEn);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewVocab == null || viewFlashcard == null || viewQuiz == null || viewStats == null || viewReading == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewVocab.Visibility = Visibility.Collapsed;
            viewFlashcard.Visibility = Visibility.Collapsed;
            viewQuiz.Visibility = Visibility.Collapsed;
            viewStats.Visibility = Visibility.Collapsed;
            viewReading.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            if (sideMenu.SelectedIndex == 4) // Stats tab
            {
                UpdateStatsView();
            }

            if (rootGrid != null)
            {
                if (sideMenu.SelectedIndex == 6) // Practical App
                {
                }
                else
                {
                }
            }

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewVocab.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewFlashcard.Visibility = Visibility.Visible;
                    break;
                case 3:
                    viewQuiz.Visibility = Visibility.Visible;
                    break;
                case 4:
                    viewStats.Visibility = Visibility.Visible;
                    break;
                case 5:
                    viewReading.Visibility = Visibility.Visible;
                    break;
                case 6:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void UpdateStatsView()
        {
            if (txtStatsToday == null) return;
            
            txtStatsToday.Text = QASmartClass.LearningTools.Models.SRSTracker.GetLearnedToday().ToString();
            txtStatsTotal.Text = QASmartClass.LearningTools.Models.SRSTracker.GetTotalLearned().ToString();
            
            var recentEn = QASmartClass.LearningTools.Models.SRSTracker.GetRecentLearned(10);
            var recentWords = new List<VWord>();
            foreach (var en in recentEn)
            {
                var match = AllCategories.SelectMany(c => c.Words).FirstOrDefault(w => w.En.Equals(en, StringComparison.OrdinalIgnoreCase));
                if (match != null) recentWords.Add(match);
            }
            lstRecentLearned.ItemsSource = recentWords;
        }

        private void ExportMyList_Click(object s, RoutedEventArgs e)
        {
            var code = QASmartClass.LearningTools.Models.SRSTracker.ExportCustomList();
            SafeSetClipboardText(code);
            MessageBox.Show("Mã danh sách của bạn đã được sao chép vào Clipboard!\nHãy dán (Ctrl+V) gửi cho học sinh/bạn bè.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ImportMyList_Click(object s, RoutedEventArgs e)
        {
            string code = SafeGetClipboardText();
            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Clipboard trống hoặc không đọc được. Vui lòng copy mã danh sách trước khi nhập.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (QASmartClass.LearningTools.Models.SRSTracker.ImportCustomList(code))
            {
                MessageBox.Show("Đã nhập danh sách thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                PopulateTopics();
            }
            else
            {
                MessageBox.Show("Mã danh sách không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══ FLASHCARD ═══
        private void LoadFlashcards()
        {
            _fcWords = GetCurrentWords().ToList();
            _fcIdx = 0; _fcFlipped = false;
            _isFcAnimating = false;
            if (btnFcRestart != null) btnFcRestart.Visibility = Visibility.Collapsed;
            ShowFlashcard();
        }

        private void Flashcard_Border_Click(object s, MouseButtonEventArgs e)
        {
            FlipCard();
        }

        private void Flashcard_Btn_Click(object s, RoutedEventArgs e)
        {
            FlipCard();
        }

        private void BtnFcRestart_Click(object s, RoutedEventArgs e)
        {
            LoadFlashcards();
        }

        private void FlipCard()
        {
            if (_fcWords.Count == 0) { LoadFlashcards(); return; }
            if (_fcIdx >= _fcWords.Count) return; // Finished
            if (_isFcAnimating) return; // Protect from concurrent clicks

            _isFcAnimating = true;
            var animOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            animOut.Completed += (s, ev) =>
            {
                _fcFlipped = !_fcFlipped;
                ShowFlashcardContent();
                var animIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
                animIn.Completed += (s2, ev2) => { _isFcAnimating = false; };
                fcScale.BeginAnimation(ScaleTransform.ScaleXProperty, animIn);
            };
            fcScale.BeginAnimation(ScaleTransform.ScaleXProperty, animOut);
        }

        private void ShowFlashcard()
        {
            if (_fcWords.Count == 0) { LoadFlashcards(); return; }
            _fcFlipped = false;
            ShowFlashcardContent();
        }

        private void ShowFlashcardContent()
        {
            if (_fcWords.Count == 0) { LoadFlashcards(); return; }
            if (_fcIdx < 0) _fcIdx = 0;
            if (_fcIdx >= _fcWords.Count) _fcIdx = _fcWords.Count - 1;
            var w = _fcWords[_fcIdx];
            txtFcWord.Text = w.En;
            txtFcIPA.Text = string.IsNullOrEmpty(w.IPA) ? "" : $"/{w.IPA}/";
            txtFcPartOfSpeech.Text = w.PoS ?? "";
            txtFcVietnamese.Text = w.Vi;
            txtFcVietnamese.Visibility = _fcFlipped ? Visibility.Visible : Visibility.Collapsed;
            txtFcExample.Text = w.ExEn ?? "";
            txtFcExample.Visibility = _fcFlipped ? Visibility.Visible : Visibility.Collapsed;
            txtFcProgress.Text = $"Thẻ {_fcIdx + 1}/{_fcWords.Count}";
            var lvColor = LevelColor(w.Level);
            flashcardBorder.BorderBrush = new SolidColorBrush(lvColor);

            if (btnFcShow != null) btnFcShow.Visibility = _fcFlipped ? Visibility.Collapsed : Visibility.Visible;
            if (pnlFcControls != null) pnlFcControls.Visibility = _fcFlipped ? Visibility.Visible : Visibility.Collapsed;
            if (btnFcRestart != null) btnFcRestart.Visibility = Visibility.Collapsed;

            // Text-to-Speech
            if (!_fcFlipped && !string.IsNullOrEmpty(w.En))
            {
                try
                {
                    _synthesizer.SpeakAsyncCancelAll();
                    _synthesizer.SpeakAsync(w.En);
                }
                catch { }
            }
        }

        private void ReviewCurrentCard(int quality)
        {
            if (_fcWords.Count == 0) return;
            var w = _fcWords[_fcIdx];
            QASmartClass.LearningTools.Models.SRSTracker.ReviewWord(w.En, quality);
            
            _fcIdx++;
            if (_fcIdx >= _fcWords.Count)
            {
                // Completed
                txtFcWord.Text = "🎉 Hoàn thành!";
                txtFcIPA.Text = "";
                txtFcPartOfSpeech.Text = "";
                txtFcVietnamese.Text = "Bạn đã ôn tập xong mục này.";
                txtFcVietnamese.Visibility = Visibility.Visible;
                txtFcExample.Visibility = Visibility.Collapsed;
                if (btnFcShow != null) btnFcShow.Visibility = Visibility.Collapsed;
                if (pnlFcControls != null) pnlFcControls.Visibility = Visibility.Collapsed;
                if (btnFcRestart != null) btnFcRestart.Visibility = Visibility.Visible;
                txtFcProgress.Text = "";
            }
            else
            {
                _fcFlipped = false;
                ShowFlashcard();
            }
        }

        private void Fc_Hard(object s, RoutedEventArgs e) { ReviewCurrentCard(0); }
        private void Fc_Good(object s, RoutedEventArgs e) { ReviewCurrentCard(3); }
        private void Fc_Easy(object s, RoutedEventArgs e) { ReviewCurrentCard(5); }

        // ═══ SPELLING QUIZ ═══
        private void VocabQuizStart_Click(object s, RoutedEventArgs e)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 3)
            {
                sideMenu.SelectedIndex = 1;
            }
            var words = GetCurrentWords();
            if (words.Length == 0)
            {
                MessageBox.Show("Danh sách từ vựng hiện tại trống. Vui lòng thêm từ hoặc thay đổi bộ lọc để bắt đầu bài quiz.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _isMCQuiz = false;
            _quizWords = words.OrderBy(_ => _rng.Next()).Take(10).ToList();
            _quizIdx = 0; _quizCorrect = 0; _quizTotal = 0; _quizActive = true; _isEvaluating = false;
            if (txtVocabAnswer != null)
            {
                txtVocabAnswer.Visibility = Visibility.Visible;
                txtVocabAnswer.IsEnabled = true;
                txtVocabAnswer.Foreground = Brushes.Black;
            }
            if (mcPanel != null) mcPanel.Visibility = Visibility.Collapsed;
            btnVocabStart.Content = "🔄 Chơi lại";
            ShowVocabQ();
        }

        private void ShowVocabQ()
        {
            if (_quizIdx >= _quizWords.Count)
            {
                _quizActive = false;
                txtVocabQ.Text = $"🎉 {_quizCorrect}/{_quizWords.Count}!";
                txtVocabHint.Text = _quizCorrect == _quizWords.Count ? "Xuất sắc!" : "Tiếp tục luyện tập nhé!";
                txtVocabAnswer.Visibility = Visibility.Collapsed;
                mcPanel.Visibility = Visibility.Collapsed;
                txtVocabFeedback.Text = "";
                return;
            }
            var w = _quizWords[_quizIdx];
            if (_isMCQuiz) ShowMCQuestion(w);
            else
            {
                txtVocabQ.Text = w.Vi;
                txtVocabHint.Text = $"Câu {_quizIdx + 1}/{_quizWords.Count} — Nhập từ tiếng Anh";
                txtVocabAnswer.Text = ""; txtVocabFeedback.Text = "";
                txtVocabAnswer.Focus();
            }
        }

        private void VocabAnswer_KeyDown(object s, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || !_quizActive || _isMCQuiz || _isEvaluating) return;
            _isEvaluating = true;
            if (txtVocabAnswer != null) txtVocabAnswer.IsEnabled = false;

            var w = _quizWords[_quizIdx];
            string ans = txtVocabAnswer.Text.Trim().ToLowerInvariant();
            _quizTotal++;
            if (ans == w.En.Trim().ToLowerInvariant())
            {
                _quizCorrect++;
                txtVocabFeedback.Text = "✅ Đúng!";
                txtVocabFeedback.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                txtVocabAnswer.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            }
            else
            {
                txtVocabFeedback.Text = $"❌ Đáp án: {w.En}";
                txtVocabFeedback.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                txtVocabAnswer.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
            }
            _quizIdx++;
            txtVocabScore.Text = $"Điểm: {_quizCorrect}/{_quizTotal}";
            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
            t.Tick += (_, _) =>
            {
                t.Stop();
                if (txtVocabAnswer != null)
                {
                    txtVocabAnswer.IsEnabled = true;
                    txtVocabAnswer.Foreground = Brushes.Black;
                }
                _isEvaluating = false;
                ShowVocabQ();
            };
            t.Start();
        }

        private void VocabAnswer_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Real-time color hint is disabled for pedagogical integrity
        }

        // ═══ MULTIPLE CHOICE QUIZ ═══
        private void MCQuizStart_Click(object s, RoutedEventArgs e)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 3)
            {
                sideMenu.SelectedIndex = 1;
            }
            var words = GetCurrentWords();
            if (words.Length == 0)
            {
                MessageBox.Show("Danh sách từ vựng hiện tại trống. Vui lòng thêm từ hoặc thay đổi bộ lọc để bắt đầu bài quiz.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _isMCQuiz = true;
            _quizWords = words.OrderBy(_ => _rng.Next()).Take(10).ToList();
            _quizIdx = 0; _quizCorrect = 0; _quizTotal = 0; _quizActive = true; _isEvaluating = false;
            if (txtVocabAnswer != null) txtVocabAnswer.Visibility = Visibility.Collapsed;
            if (mcPanel != null)
            {
                mcPanel.Visibility = Visibility.Visible;
                mcPanel.IsEnabled = true;
            }
            btnMCQuiz.Content = "🔄 Chơi lại";
            ShowVocabQ();
        }

        private void ShowMCQuestion(VWord w)
        {
            txtVocabQ.Text = w.En;
            txtVocabHint.Text = $"Câu {_quizIdx + 1}/{_quizWords.Count} — Chọn nghĩa đúng";
            txtVocabFeedback.Text = "";
            mcPanel.Children.Clear();
            mcPanel.Visibility = Visibility.Visible;

            var allWords = GetCurrentWords();
            var wrongs = allWords.Where(x => x.Vi != w.Vi).Select(x => x.Vi).Distinct().OrderBy(_ => _rng.Next()).ToList();
            if (wrongs.Count < 3)
            {
                var fallbackWrongs = AllCategories.SelectMany(c => c.Words)
                    .Where(x => x.Vi != w.Vi)
                    .Select(x => x.Vi)
                    .Distinct()
                    .OrderBy(_ => _rng.Next())
                    .Take(3 - wrongs.Count);
                wrongs.AddRange(fallbackWrongs);
            }
            var options = wrongs.Take(3).Append(w.Vi).OrderBy(_ => _rng.Next()).ToList();

            foreach (var opt in options)
            {
                var btn = new Button
                {
                    Content = opt, FontSize = 16, Padding = new Thickness(20, 8, 20, 8),
                    Margin = new Thickness(0, 4, 0, 4), MinWidth = 280,
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(1), Cursor = Cursors.Hand,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                var capturedOpt = opt;
                btn.Click += (_, _) => CheckMCAnswer(capturedOpt, w.Vi);
                mcPanel.Children.Add(btn);
            }
        }

        private void CheckMCAnswer(string selected, string correct)
        {
            if (!_quizActive || _isEvaluating) return;
            _isEvaluating = true;
            if (mcPanel != null) mcPanel.IsEnabled = false;

            _quizTotal++;
            if (selected == correct)
            {
                _quizCorrect++;
                txtVocabFeedback.Text = "✅ Đúng!";
                txtVocabFeedback.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            }
            else
            {
                txtVocabFeedback.Text = $"❌ Đáp án: {correct}";
                txtVocabFeedback.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
            }
            _quizIdx++;
            txtVocabScore.Text = $"Điểm: {_quizCorrect}/{_quizTotal}";
            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
            t.Tick += (_, _) =>
            {
                t.Stop();
                if (mcPanel != null) mcPanel.IsEnabled = true;
                _isEvaluating = false;
                ShowVocabQ();
            };
            t.Start();
        }

        // ═══ READING CONTEXT MODULE (PHASE 9) ═══
        private void GenerateReading_Click(object s, RoutedEventArgs e)
        {
            txtReadingContent.Inlines.Clear();
            var rnd = new Random();
            var words = GetCurrentWords().OrderBy(x => rnd.Next()).Take(8).ToList();
            if (words.Count == 0)
            {
                txtReadingContent.Inlines.Add(new Run("Không có từ vựng nào trong mục này.") { Foreground = Brushes.Gray });
                return;
            }

            foreach (var w in words)
            {
                string text = "";
                if (w.ContextualExamples != null && w.ContextualExamples.Count > 0)
                {
                    text = w.ContextualExamples.First().Value;
                }
                else if (!string.IsNullOrEmpty(w.ExEn))
                {
                    text = w.ExEn;
                }
                else continue;

                string pattern = @"\b" + System.Text.RegularExpressions.Regex.Escape(w.En) + @"\b";
                var regexMatch = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (regexMatch.Success)
                {
                    string before = text.Substring(0, regexMatch.Index);
                    string matchedText = regexMatch.Value;
                    string after = text.Substring(regexMatch.Index + regexMatch.Length);

                    txtReadingContent.Inlines.Add(new Run(before));

                    var hyper = new Hyperlink(new Run(matchedText))
                    {
                        TextDecorations = null,
                        Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)), // Blue #1565C0
                        FontWeight = FontWeights.Bold,
                        Cursor = Cursors.Hand
                    };
                    hyper.Click += (sender, args) => ShowWordPopup(w);
                    txtReadingContent.Inlines.Add(hyper);

                    txtReadingContent.Inlines.Add(new Run(after + "\n\n"));
                }
                else
                {
                    // Fallback to substring matching if regex boundary match fails
                    int idx = text.IndexOf(w.En, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        string before = text.Substring(0, idx);
                        string matchedText = text.Substring(idx, w.En.Length);
                        string after = text.Substring(idx + w.En.Length);

                        txtReadingContent.Inlines.Add(new Run(before));

                        var hyper = new Hyperlink(new Run(matchedText))
                        {
                            TextDecorations = null,
                            Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                            FontWeight = FontWeights.Bold,
                            Cursor = Cursors.Hand
                        };
                        hyper.Click += (sender, args) => ShowWordPopup(w);
                        txtReadingContent.Inlines.Add(hyper);

                        txtReadingContent.Inlines.Add(new Run(after + "\n\n"));
                    }
                    else
                    {
                        txtReadingContent.Inlines.Add(new Run(text + "\n\n"));
                    }
                }
            }
        }

        private void ShowWordPopup(VWord w)
        {
            popWordEn.Text = w.En;
            popWordIPA.Text = string.IsNullOrEmpty(w.IPA) ? "" : $"/{w.IPA}/";
            popWordVi.Text = w.Vi;
            popWordEx.Text = w.ExEn ?? "";
            popupDict.Visibility = Visibility.Visible;
        }

        private void ClosePopup_Click(object s, RoutedEventArgs e)
        {
            popupDict.Visibility = Visibility.Collapsed;
        }

        // ═══ PRONUNCIATION (PHASE 10) ═══
        private async void BtnPronounce_Click(object s, RoutedEventArgs e)
        {
            if (btnPronounce.Tag is not string wordEn) return;

            try
            {
                var hasEnUs = SpeechRecognitionEngine.InstalledRecognizers()
                    .Any(r => r.Culture.Name.Equals("en-US", StringComparison.OrdinalIgnoreCase));
                if (!hasEnUs)
                {
                    txtPronounceResult.Text = "⚠️ Lỗi: Thiếu gói ngôn ngữ nhận dạng en-US.";
                    txtPronounceResult.Foreground = new SolidColorBrush(Color.FromRgb(245, 124, 0));
                    return;
                }
            }
            catch (Exception ex)
            {
                txtPronounceResult.Text = $"⚠️ Lỗi Speech Engine: {ex.Message}";
                txtPronounceResult.Foreground = new SolidColorBrush(Color.FromRgb(245, 124, 0));
                return;
            }

            btnPronounce.IsEnabled = false;
            btnPronounce.Content = "⏳ Đang nghe...";
            txtPronounceResult.Text = "";

            try
            {
                await Task.Run(() =>
                {
                    using var recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));
                    recognizer.SetInputToDefaultAudioDevice();
                    var grammar = new Grammar(new GrammarBuilder(wordEn));
                    recognizer.LoadGrammar(grammar);

                    var result = recognizer.Recognize(TimeSpan.FromSeconds(3));
                    
                    Dispatcher.Invoke(() =>
                    {
                        if (result != null && result.Text.Equals(wordEn, StringComparison.OrdinalIgnoreCase))
                        {
                            txtPronounceResult.Text = "✅ Chính xác!";
                            txtPronounceResult.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                        }
                        else
                        {
                            txtPronounceResult.Text = "❌ Chưa đúng, thử lại nhé!";
                            txtPronounceResult.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47)); // Red
                        }
                    });
                });
            }
            catch (Exception)
            {
                txtPronounceResult.Text = "⚠️ Lỗi: Vui lòng cắm Micro.";
                txtPronounceResult.Foreground = new SolidColorBrush(Color.FromRgb(245, 124, 0)); // Orange
            }
            finally
            {
                btnPronounce.IsEnabled = true;
                btnPronounce.Content = "🎤 Luyện âm";
            }
        }

        // ═══ RESET ═══
        private void ResetAll_Click(object s, MouseButtonEventArgs e)
        {
            _quizActive = false; _quizIdx = 0; _quizCorrect = 0; _quizTotal = 0;
            _quizWords.Clear(); _fcWords.Clear(); _fcIdx = 0;
            if (cboLevel != null) cboLevel.SelectedIndex = 0;
        }

        // ═══ HELPERS ═══
        private static Color LevelColor(string lv) => lv switch
        {
            "A1" => (Color)ColorConverter.ConvertFromString("#4CAF50"),
            "A2" => (Color)ColorConverter.ConvertFromString("#2196F3"),
            "B1" => (Color)ColorConverter.ConvertFromString("#FF9800"),
            "B2" => (Color)ColorConverter.ConvertFromString("#7B1FA2"),
            "C1" => (Color)ColorConverter.ConvertFromString("#D32F2F"),
            "C2" => (Color)ColorConverter.ConvertFromString("#37474F"),
            _ => (Color)ColorConverter.ConvertFromString("#78909C")
        };

        // ═══════════════════════════════════════════════════
        // DISPOSABLE
        // ═══════════════════════════════════════════════════
        public void Dispose()
        {
            try
            {
                _synthesizer?.Dispose();
            }
            catch { }
        }

        // ═══════════════════════════════════════════════════
        // DATA — CEFR A1→C2 (from JSON)
        // ═══════════════════════════════════════════════════
        private record VWord(
            string En, 
            string Vi, 
            string Level, 
            string? IPA = null, 
            string? PoS = null, 
            string? ExEn = null,
            List<string>? Collocations = null,
            Dictionary<string, string>? ContextualExamples = null,
            string? ImageUrl = null,
            List<string>? Synonyms = null,
            List<string>? Antonyms = null
        );
        private record VCat(string Name, string Icon, string Level, VWord[] Words);

        private static VCat[] AllCategories = null;

        static VocabularyTool()
        {
            // Synchronous loading removed to prevent UI blocking.
            // Vocabulary data is loaded asynchronously via LoadVocabularyDataAsync.
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
                        Icon = "✉️",
                        Title = isVN ? "Giao tiếp & Email công sở" : "Workplace & Email",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_vocabulary_1_{suffix}.png",
                        Description = isVN
                            ? "Mở rộng vốn từ chuyên ngành giúp soạn thảo email chuyên nghiệp, viết CV và thuyết trình tiếng Anh tự tin nơi công sở."
                            : "Expanding professional vocabulary helps write business emails, build resumes, and deliver English presentations with confidence."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📚",
                        Title = isVN ? "Nghiên cứu & Học thuật" : "Academic & Research",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_vocabulary_2_{suffix}.png",
                        Description = isVN
                            ? "Từ vựng học thuật (Academic English) hỗ trợ đọc hiểu tài liệu nghiên cứu, viết luận văn và ôn tập các kỳ thi chứng chỉ như IELTS/TOEFL."
                            : "Academic English vocabulary helps comprehend scientific textbooks, write essays, and prepare for exams like IELTS or TOEFL."
                    },
                    new PracticalAppItem
                    {
                        Icon = "✈️",
                        Title = isVN ? "Du lịch & Hội nhập" : "Travel & Integration",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_vocabulary_3_{suffix}.png",
                        Description = isVN
                            ? "Từ vựng giao tiếp thực tế giúp bạn tự tin hỏi đường, đặt phòng khách sạn, giao lưu văn hóa và làm thủ tục sân bay khi đi nước ngoài."
                            : "Everyday communication vocabulary enables you to confidently ask for directions, book hotel rooms, airport checks, and chat with locals."
                    }
                };

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for VocabularyTool: {Err}", ex.Message);
            }
        }
    }
}