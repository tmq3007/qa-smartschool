using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Multi
{
    public class CountryModel
    {
        public string Flag { get; set; } = "";
        public string Name { get; set; } = "";
        public string Capital { get; set; } = "";
        public string Continent { get; set; } = "";
        public string Area { get; set; } = "";
    }

    public class CountryItemViewModel : INotifyPropertyChanged
    {
        public string Flag { get; set; } = "";
        public string Name { get; set; } = "";
        public string Capital { get; set; } = "";
        public string Continent { get; set; } = "";
        public string Area { get; set; } = "";

        private bool _isCapitalRevealed;
        public bool IsCapitalRevealed
        {
            get => _isCapitalRevealed;
            set
            {
                _isCapitalRevealed = value;
                OnPropertyChanged(nameof(IsCapitalRevealed));
                OnPropertyChanged(nameof(ShowCapitalMask));
                OnPropertyChanged(nameof(ShowCapitalText));
            }
        }

        private bool _isAreaRevealed;
        public bool IsAreaRevealed
        {
            get => _isAreaRevealed;
            set
            {
                _isAreaRevealed = value;
                OnPropertyChanged(nameof(IsAreaRevealed));
                OnPropertyChanged(nameof(ShowAreaMask));
                OnPropertyChanged(nameof(ShowAreaText));
            }
        }

        private bool _isFlashcardModeActive;
        public bool IsFlashcardModeActive
        {
            get => _isFlashcardModeActive;
            set
            {
                _isFlashcardModeActive = value;
                OnPropertyChanged(nameof(ShowCapitalMask));
                OnPropertyChanged(nameof(ShowCapitalText));
                OnPropertyChanged(nameof(ShowAreaMask));
                OnPropertyChanged(nameof(ShowAreaText));
            }
        }

        public Visibility ShowCapitalMask => (IsFlashcardModeActive && !IsCapitalRevealed) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShowCapitalText => (!IsFlashcardModeActive || IsCapitalRevealed) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ShowAreaMask => (IsFlashcardModeActive && !IsAreaRevealed) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShowAreaText => (!IsFlashcardModeActive || IsAreaRevealed) ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class CountriesTool : BaseToolControl
    {
        private string _selectedContinent = "All";
        private List<CountryModel> _allCountries = new();
        private readonly ObservableCollection<CountryItemViewModel> _filteredCountries = new();
        private readonly HashSet<string> _revealedCapitals = new();
        private readonly HashSet<string> _revealedAreas = new();

        // Game state variables
        private DispatcherTimer? _gameTimer;
        private int _timeLeft = 60;
        private int _score = 0;
        private int _streak = 0;
        private CountryModel? _currentTargetCountry;
        private string _correctCapital = "";

        public CountriesTool()
        {
            InitializeComponent();
            Loaded += async (_, _) => {
                lstCountries.ItemsSource = _filteredCountries;
                await LoadCountriesAsync();
                PerformFilter();
                TouchTextPad.Attach(txtCountrySearch, mode: "text");
                LoadPracticalApps();

                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextTable != null) menuTextTable.Text = isVN ? "Danh sách quốc gia" : "Country List";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                if (sideMenu != null)
                {
                    sideMenu.SelectedIndex = 0;
                }
            };
            Unloaded += (_, _) => StopGameTimer();
        }

        private async Task LoadCountriesAsync()
        {
            try
            {
                var localData = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Data", "countries.json");
                if (File.Exists(localData))
                {
                    string json = await File.ReadAllTextAsync(localData, Encoding.UTF8);
                    _allCountries = JsonSerializer.Deserialize<List<CountryModel>>(json) ?? new();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading countries JSON: " + ex.Message);
            }

            // Fallback load if file reading fails
            if (_allCountries == null || _allCountries.Count == 0)
            {
                LoadFallbackCountries();
            }
        }

        private void LoadFallbackCountries()
        {
            _allCountries = new List<CountryModel>
            {
                new() { Flag = "🇻🇳", Name = "Việt Nam", Capital = "Hà Nội", Continent = "Châu Á", Area = "331.212" },
                new() { Flag = "🇨🇳", Name = "Trung Quốc", Capital = "Bắc Kinh", Continent = "Châu Á", Area = "9.596.961" },
                new() { Flag = "🇯🇵", Name = "Nhật Bản", Capital = "Tô-ky-ô", Continent = "Châu Á", Area = "377.975" },
                new() { Flag = "🇰🇷", Name = "Hàn Quốc", Capital = "Xơ-un", Continent = "Châu Á", Area = "100.210" },
                new() { Flag = "🇺🇸", Name = "Hoa Kỳ", Capital = "Oa-sinh-tơn D.C.", Continent = "Châu Mỹ", Area = "9.833.520" },
                new() { Flag = "🇬🇧", Name = "Vương quốc Anh", Capital = "Luân Đôn", Continent = "Châu Âu", Area = "243.610" },
                new() { Flag = "🇫🇷", Name = "Pháp", Capital = "Pa-ri", Continent = "Châu Âu", Area = "640.679" }
            };
        }

        private void CountrySearch_Changed(object sender, TextChangedEventArgs e)
        {
            if (btnClearSearch != null)
            {
                btnClearSearch.Visibility = string.IsNullOrEmpty(txtCountrySearch.Text) ? Visibility.Collapsed : Visibility.Visible;
            }
            PerformFilter();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            if (txtCountrySearch != null)
            {
                txtCountrySearch.Text = "";
            }
        }

        private void ResetSearch_Click(object sender, RoutedEventArgs e)
        {
            if (txtCountrySearch != null)
            {
                txtCountrySearch.Text = "";
            }
            if (tabAll != null)
            {
                tabAll.IsChecked = true;
            }
            else
            {
                _selectedContinent = "All";
                PerformFilter();
            }
        }

        private void ContinentFilter_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.IsChecked == true)
            {
                _selectedContinent = rb.Tag?.ToString() ?? "All";
                PerformFilter();
            }
        }

        private void FlashcardOption_Changed(object sender, RoutedEventArgs e)
        {
            _revealedCapitals.Clear();
            _revealedAreas.Clear();
            PerformFilter();
        }

        private void PerformFilter()
        {
            if (txtCountrySearch == null || _allCountries == null || lstCountries == null) return;

            string q = txtCountrySearch.Text.Trim();

            var filtered = _allCountries.Where(c =>
            {
                // 1. Continent Filter
                if (_selectedContinent != "All" && c.Continent != _selectedContinent)
                    return false;

                // 2. Text Search Filter (Accent-insensitive & Flag Country Code matched)
                if (string.IsNullOrEmpty(q))
                    return true;

                return MatchSearch(c.Name, q) ||
                       MatchSearch(c.Capital, q) ||
                       MatchSearch(c.Continent, q) ||
                       MatchSearch(FlagToCountryCode(c.Flag), q);
            }).OrderBy(x => x.Continent).ThenBy(x => x.Name).ToList();

            lstCountries.ItemsSource = null;
            _filteredCountries.Clear();
            foreach (var c in filtered)
            {
                bool isCapitalRevealed = _revealedCapitals.Contains(c.Name);
                bool isAreaRevealed = _revealedAreas.Contains(c.Name);

                _filteredCountries.Add(new CountryItemViewModel
                {
                    Flag = c.Flag,
                    Name = c.Name,
                    Capital = c.Capital,
                    Continent = c.Continent,
                    Area = c.Area,
                    IsCapitalRevealed = isCapitalRevealed,
                    IsAreaRevealed = isAreaRevealed,
                    IsFlashcardModeActive = (btnHideCapitals != null && btnHideCapitals.IsChecked == true) || (btnHideAreas != null && btnHideAreas.IsChecked == true)
                });
            }
            lstCountries.ItemsSource = _filteredCountries;

            if (bdrNoResults != null)
            {
                bdrNoResults.Visibility = _filteredCountries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            // UX Improvement: Reset scroll to top on filter change
            if (_filteredCountries.Count > 0)
            {
                lstCountries.ScrollIntoView(_filteredCountries[0]);
            }
        }

        private static bool MatchSearch(string target, string query)
        {
            if (string.IsNullOrEmpty(query)) return true;
            if (string.IsNullOrEmpty(target)) return false;

            string normalizedTarget = RemoveDiacritics(target);
            string normalizedQuery = RemoveDiacritics(query);

            return normalizedTarget.Contains(normalizedQuery);
        }

        private static string FlagToCountryCode(string flag)
        {
            if (string.IsNullOrEmpty(flag) || flag.Length < 4) return string.Empty;

            try
            {
                int firstCode = char.ConvertToUtf32(flag, 0);
                int secondCode = char.ConvertToUtf32(flag, 2);

                if (firstCode >= 0x1F1E6 && firstCode <= 0x1F1FF &&
                    secondCode >= 0x1F1E6 && secondCode <= 0x1F1FF)
                {
                    char c1 = (char)('A' + (firstCode - 0x1F1E6));
                    char c2 = (char)('A' + (secondCode - 0x1F1E6));
                    return $"{c1}{c2}".ToLowerInvariant();
                }
            }
            catch { }

            return string.Empty;
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            string normalizedString = text.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                System.Globalization.UnicodeCategory unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    if (c == 'đ' || c == 'Đ')
                    {
                        stringBuilder.Append('d');
                    }
                    else
                    {
                        stringBuilder.Append(c);
                    }
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        private void RevealCapital_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is CountryItemViewModel vm)
            {
                vm.IsCapitalRevealed = true;
                _revealedCapitals.Add(vm.Name);
            }
        }

        private void RevealArea_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is CountryItemViewModel vm)
            {
                vm.IsAreaRevealed = true;
                _revealedAreas.Add(vm.Name);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  GAME MODE LOGIC
        // ═══════════════════════════════════════════════════════════

        private void GameMode_Checked(object sender, RoutedEventArgs e)
        {
            if (bdrTableHeader != null) bdrTableHeader.Visibility = Visibility.Collapsed;
            if (grdNormalMode != null) grdNormalMode.Visibility = Visibility.Collapsed;
            if (grdGameMode != null) grdGameMode.Visibility = Visibility.Visible;

            if (btnHideCapitals != null) btnHideCapitals.IsEnabled = false;
            if (btnHideAreas != null) btnHideAreas.IsEnabled = false;
            if (txtCountrySearch != null) txtCountrySearch.IsEnabled = false;
            if (pnlContinentFilters != null) pnlContinentFilters.IsEnabled = false;

            StartNewGame();
        }

        private void GameMode_Unchecked(object sender, RoutedEventArgs e)
        {
            StopGameTimer();
            if (bdrTableHeader != null) bdrTableHeader.Visibility = Visibility.Visible;
            if (grdNormalMode != null) grdNormalMode.Visibility = Visibility.Visible;
            if (grdGameMode != null) grdGameMode.Visibility = Visibility.Collapsed;

            if (btnHideCapitals != null) btnHideCapitals.IsEnabled = true;
            if (btnHideAreas != null) btnHideAreas.IsEnabled = true;
            if (txtCountrySearch != null) txtCountrySearch.IsEnabled = true;
            if (pnlContinentFilters != null) pnlContinentFilters.IsEnabled = true;

            PerformFilter();
        }

        private void StartNewGame()
        {
            _score = 0;
            _timeLeft = 60;
            _streak = 0;

            if (pnlGamePlay != null) pnlGamePlay.Visibility = Visibility.Visible;
            if (gridGameAnswers != null) gridGameAnswers.Visibility = Visibility.Visible;
            if (pnlGameEnd != null) pnlGameEnd.Visibility = Visibility.Collapsed;

            UpdateGameStats();
            NextQuestion();

            if (_gameTimer == null)
            {
                _gameTimer = new DispatcherTimer();
                _gameTimer.Interval = TimeSpan.FromSeconds(1);
                _gameTimer.Tick += GameTimer_Tick;
            }
            _gameTimer.Start();
        }

        private void StopGameTimer()
        {
            if (_gameTimer != null)
            {
                _gameTimer.Stop();
                _gameTimer.Tick -= GameTimer_Tick;
                _gameTimer = null;
            }
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            _timeLeft--;
            if (txtGameTimer != null)
            {
                txtGameTimer.Text = $"{_timeLeft}s";
            }

            if (_timeLeft <= 0)
            {
                EndGame();
            }
        }

        private void UpdateGameStats()
        {
            if (txtGameTimer != null) txtGameTimer.Text = $"{_timeLeft}s";
            if (txtGameScore != null) txtGameScore.Text = $"{_score} XP";
            if (txtGameStreak != null) txtGameStreak.Text = $"{_streak}";
        }

        private void NextQuestion()
        {
            if (_allCountries == null) return;

            var sourceList = _allCountries;
            if (_selectedContinent != "All")
            {
                sourceList = _allCountries.Where(c => c.Continent == _selectedContinent).ToList();
            }

            if (sourceList.Count < 4)
            {
                sourceList = _allCountries; // Fallback to all countries if continent has too few entries
            }

            if (sourceList.Count < 4) return;

            var rand = new Random();
            int idx = rand.Next(sourceList.Count);
            _currentTargetCountry = sourceList[idx];
            
            char[] splitChars = new char[] { ',', '/' };
            _correctCapital = _currentTargetCountry.Capital.Split(splitChars)[0].Trim();

            if (txtGameQuestionFlag != null) txtGameQuestionFlag.Text = _currentTargetCountry.Flag;
            if (txtGameQuestionText != null) txtGameQuestionText.Text = $"Tìm thủ đô của {_currentTargetCountry.Name}:";

            // Generate distractors from the SAME continent if possible (Smart Distractors!)
            var sameContinentCountries = _allCountries
                .Where(c => c.Continent == _currentTargetCountry.Continent && c.Name != _currentTargetCountry.Name)
                .ToList();

            List<string> options = new List<string> { _correctCapital };

            while (options.Count < 4 && sameContinentCountries.Count > 0)
            {
                int rIdx = rand.Next(sameContinentCountries.Count);
                string cap = sameContinentCountries[rIdx].Capital.Split(splitChars)[0].Trim();
                if (!options.Contains(cap))
                {
                    options.Add(cap);
                }
                sameContinentCountries.RemoveAt(rIdx);
            }

            var otherCountries = _allCountries.Where(c => c.Name != _currentTargetCountry.Name).ToList();
            while (options.Count < 4 && otherCountries.Count > 0)
            {
                int rIdx = rand.Next(otherCountries.Count);
                string cap = otherCountries[rIdx].Capital.Split(splitChars)[0].Trim(); // Safe fallback if capital properties differ
                if (!options.Contains(cap))
                {
                    options.Add(cap);
                }
                otherCountries.RemoveAt(rIdx);
            }

            options = options.OrderBy(x => Guid.NewGuid()).ToList();

            if (btnAnswerA != null) btnAnswerA.Content = options[0];
            if (btnAnswerB != null) btnAnswerB.Content = options[1];
            if (btnAnswerC != null) btnAnswerC.Content = options[2];
            if (btnAnswerD != null) btnAnswerD.Content = options[3];

            ResetAnswerButtonsStyle();
        }

        private void ResetAnswerButtonsStyle()
        {
            foreach (var btn in new[] { btnAnswerA, btnAnswerB, btnAnswerC, btnAnswerD })
            {
                if (btn != null)
                {
                    btn.ClearValue(BackgroundProperty);
                    btn.ClearValue(BorderBrushProperty);
                    btn.ClearValue(ForegroundProperty);
                    btn.IsEnabled = true;
                }
            }
        }

        private async void AnswerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedBtn)
            {
                DisableAnswerButtons();

                string selectedAnswer = clickedBtn.Content?.ToString() ?? "";
                bool isCorrect = (selectedAnswer == _correctCapital);

                if (isCorrect)
                {
                    _streak++;
                    _score += 10 * System.Math.Min(_streak, 5);
                    clickedBtn.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201));
                    clickedBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    clickedBtn.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                }
                else
                {
                    _streak = 0;
                    clickedBtn.Background = new SolidColorBrush(Color.FromRgb(255, 205, 210));
                    clickedBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    clickedBtn.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));

                    HighlightCorrectAnswer();
                }

                UpdateGameStats();

                await Task.Delay(800);

                if (_timeLeft > 0 && btnGameMode != null && btnGameMode.IsChecked == true)
                {
                    NextQuestion();
                }
            }
        }

        private void DisableAnswerButtons()
        {
            foreach (var btn in new[] { btnAnswerA, btnAnswerB, btnAnswerC, btnAnswerD })
            {
                if (btn != null) btn.IsEnabled = false;
            }
        }

        private void HighlightCorrectAnswer()
        {
            foreach (var btn in new[] { btnAnswerA, btnAnswerB, btnAnswerC, btnAnswerD })
            {
                if (btn != null && btn.Content?.ToString() == _correctCapital)
                {
                    btn.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201));
                    btn.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    btn.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                }
            }
        }

        private void EndGame()
        {
            StopGameTimer();

            if (pnlGamePlay != null) pnlGamePlay.Visibility = Visibility.Collapsed;
            if (gridGameAnswers != null) gridGameAnswers.Visibility = Visibility.Collapsed;
            if (pnlGameEnd != null) pnlGameEnd.Visibility = Visibility.Visible;

            if (txtGameEndResult != null)
            {
                txtGameEndResult.Text = $"Bạn đạt được {_score} XP";
            }

            string medal = "💪";
            string medalName = "Cố gắng lên lần sau!";

            if (_score >= 150)
            {
                medal = "🥇";
                medalName = "Huy chương Vàng Địa Lý!";
            }
            else if (_score >= 100)
            {
                medal = "🥈";
                medalName = "Huy chương Bạc Địa Lý!";
            }
            else if (_score >= 50)
            {
                medal = "🥉";
                medalName = "Huy chương Đồng Địa Lý!";
            }

            if (txtGameEndMedal != null) txtGameEndMedal.Text = medal;
            if (txtGameEndMedalName != null) txtGameEndMedalName.Text = medalName;
        }

        private void RestartGame_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }

        private void QuitGame_Click(object sender, RoutedEventArgs e)
        {
            if (btnGameMode != null) btnGameMode.IsChecked = false;
        }

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "🚢",
                    Title = isVN ? "Logistics & Thương mại" : "Logistics & Trade",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_countries_1_{suffix}.png",
                    Description = isVN 
                        ? "Xác định vị trí quốc gia, hải cảng và múi giờ là nền tảng để thiết kế tuyến đường vận tải biển/hàng không tối ưu, giúp tối thiểu chi phí logistics toàn cầu." 
                        : "Locating countries, seaports, and time zones is the foundation for designing optimal shipping and air routes, minimizing global logistics costs."
                },
                new PracticalAppItem
                {
                    Icon = "🗺️",
                    Title = isVN ? "Du lịch & Ngoại giao" : "Tourism & Diplomacy",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_countries_2_{suffix}.png",
                    Description = isVN 
                        ? "Hiểu biết về địa lý quốc gia, thủ đô và văn hóa vùng miền giúp thiết kế tour du lịch độc đáo, chuẩn bị thủ tục visa và hỗ trợ nghi thức giao tế ngoại giao chuẩn xác." 
                        : "Understanding country geography, capitals, and regional cultures helps design unique tour itineraries, prepare visa procedures, and support diplomatic protocols."
                },
                new PracticalAppItem
                {
                    Icon = "📈",
                    Title = isVN ? "Phân tích Thị trường" : "Global Market Analysis",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_countries_3_{suffix}.png",
                    Description = isVN 
                        ? "Diện tích quốc gia, phân bố dân cư và đặc điểm địa lý giúp các doanh nghiệp đa quốc gia ước lượng quy mô thị trường tiêu thụ và lập chiến lược định vị chi nhánh." 
                        : "Country area, population distribution, and geographical characteristics help multinational corporations estimate target market size and plan regional branch locations."
                }
            };

            try
            {
                if (practicalAppViewer != null) practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for CountriesTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || grdNormalMode == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            grdNormalMode.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            // Nếu đang trong chế độ chơi game, ta tắt chế độ chơi game để quay về bình thường
            if (btnGameMode != null && btnGameMode.IsChecked == true)
            {
                btnGameMode.IsChecked = false;
            }

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    grdNormalMode.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
