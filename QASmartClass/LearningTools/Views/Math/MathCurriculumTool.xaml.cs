using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using QASmartClass.LearningTools.Models;
using QASmartClass.Data;
using System.Threading.Tasks;
using Serilog;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class MathCurriculumTool : Controls.BaseToolControl
    {
        private readonly List<MathChapterData> _chapters = new();
        private readonly List<MockExamData> _mockExams = new();
        private readonly string _dataPath;
        private readonly string _exercisePath;
        private ToolTopicMappingData? _topicMapping;

        // ─── Exam Timer & State ───
        private DispatcherTimer? _examTimer;
        private int _examSecondsLeft;
        private TextBlock? _timerText;
        private Border? _timerBorder;
        private bool _examTimerExpired;
        private Dictionary<string, (int correct, int total)>? _topicStats;

        private string _activeTab = "train";
        private int? _selectedGrade;
        private string _currentDashboardView = "main"; // "main", "performance", "builder", "details"
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, StudentProgress> _studentScores = new();
        private DispatcherTimer? _leaderboardTimer;
        private StackPanel? _leaderboardListPanel;

        /// <summary>Raised when GV clicks 'Launch Quiz' — ClassroomShell wires this to MathQuizBridgeService</summary>
        public event EventHandler<(string ChapterId, string ChapterName, int Grade)>? QuizRequested;

        /// <summary>Grade station definitions for the train journey — with content summary + inspiration</summary>
        private static readonly (int Grade, string Label, string Emoji, string Level, Color Color, string Summary, string Inspiration)[] GradeStations =
        {
            (1,  "Lớp 1",  "🌱", "Tiểu học",   Color.FromRgb(76, 175, 80),
                "Đếm, cộng trừ trong 100, hình cơ bản", "Toán bắt đầu từ đôi bàn tay em! ✋"),
            (2,  "Lớp 2",  "🌿", "Tiểu học",   Color.FromRgb(76, 175, 80),
                "Bảng nhân chia, đo lường, hình phẳng", "Thế giới nhân chia thật diệu kỳ! ✨"),
            (3,  "Lớp 3",  "🌳", "Tiểu học",   Color.FromRgb(56, 142, 60),
                "Phân số, số thập phân, thời gian & tiền tệ", "Toán giúp em quản lý tiền tiêu vặt! 💰"),
            (4,  "Lớp 4",  "📗", "Tiểu học",   Color.FromRgb(56, 142, 60),
                "Hình học, phân số nâng cao, biểu đồ", "Biểu đồ kể câu chuyện bằng số! 📊"),
            (5,  "Lớp 5",  "🏅", "Tiểu học",   Color.FromRgb(46, 125, 50),
                "Toán lời văn, hình KG, tỉ số & ước lượng", "Sẵn sàng lên THCS — hành trang vững! 🎒"),
            (6,  "Lớp 6",  "📘", "THCS",       Color.FromRgb(30, 136, 229),
                "Số hữu tỉ, hình học phẳng, thống kê", "Thế giới số mở rộng — có cả số âm! 🌊"),
            (7,  "Lớp 7",  "📐", "THCS",       Color.FromRgb(30, 136, 229),
                "Đại số, tam giác & tứ giác, thống kê mô tả", "Đại số — ngôn ngữ của vũ trụ! 🌌"),
            (8,  "Lớp 8",  "🔢", "THCS",       Color.FromRgb(25, 118, 210),
                "PT bậc 2, hàm số, hình KG, bất PT", "Phương trình mở cánh cửa bí ẩn! 🚪"),
            (9,  "Lớp 9",  "🧮", "THCS",       Color.FromRgb(25, 118, 210),
                "Hình học phẳng NC, lượng giác, đường tròn", "Nền tảng vàng cho THPT! 🏆"),
            (10, "Lớp 10", "📊", "THPT",       Color.FromRgb(21, 101, 192),
                "Mệnh đề, hàm số, PT/BPT, lượng giác, vectơ", "Tư duy logic — sức mạnh thực sự! 🧠"),
            (11, "Lớp 11", "📈", "THPT",       Color.FromRgb(123, 31, 162),
                "Lượng giác NC, dãy số, giới hạn, đạo hàm", "Giải tích — chìa khóa khoa học hiện đại! 🔬"),
            (12, "Lớp 12", "🎓", "THPT + Thi", Color.FromRgb(230, 81, 0),
                "Đạo hàm UD, mũ-log, tích phân, số phức, HH KG", "Chinh phục đại học — đỉnh cao hành trình! 🏆"),
        };

        public MathCurriculumTool()
        {
            InitializeComponent();
            _dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Resources", "MathData", "Phase4_Data");
            _exercisePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Resources", "MathData", "exercises_by_topic");
            Loaded += (_, _) => {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextTrain != null) menuTextTrain.Text = isVN ? "Chuyến tàu toán học" : "Math Train";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                LoadAndShowTrainJourney();
                LoadPracticalApps();

                // Real-time Leaderboard hook
                try
                {
                    var app = Application.Current as QASmartTouch.App;
                    if (app?.NetworkService != null)
                    {
                        app.NetworkService.MessageReceived += OnStudentNetworkMessage;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Failed to subscribe to NetworkService.MessageReceived: {Err}", ex.Message);
                }
            };
            Unloaded += (_, _) => {
                StopExamTimer();
                StopLeaderboardTimer();
                try
                {
                    var app = Application.Current as QASmartTouch.App;
                    if (app?.NetworkService != null)
                    {
                        app.NetworkService.MessageReceived -= OnStudentNetworkMessage;
                    }
                }
                catch { }
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  DATA LOADING
        // ═══════════════════════════════════════════════════════════

        private async void LoadAndShowTrainJourney()
        {
            try
            {
                await Task.Run(() => LoadAllChapters());
                BuildTabBar();
                ShowTrainJourney();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "MathCurriculum: Load failed");
                ShowError($"Không tìm thấy dữ liệu tại:\n{_dataPath}");
            }
        }

        private void LoadAllChapters()
        {
            _chapters.Clear();
            _mockExams.Clear();
            if (!Directory.Exists(_dataPath)) return;

            // Load chapter files (G{grade}_CH{order} naming convention)
            var files = Directory.GetFiles(_dataPath, "G??_CH*.json");
            foreach (var file in files.OrderBy(f => f))
            {
                try
                {
                    if (!QASmartClass.LearningTools.Helpers.PathHelper.IsPathSafe(_dataPath, file))
                    {
                        Log.Warning("Path traversal attempt detected on chapter file: {File}", file);
                        continue;
                    }
                    var json = File.ReadAllText(file);
                    var ch = JsonSerializer.Deserialize<MathChapterData>(json);
                    if (ch?.Metadata != null) _chapters.Add(ch);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Skip chapter: {File}", Path.GetFileName(file));
                }
            }

            // Load mock exam files
            var examFiles = Directory.GetFiles(_dataPath, "*Exam*.json");
            foreach (var file in examFiles.OrderBy(f => f))
            {
                try
                {
                    if (!QASmartClass.LearningTools.Helpers.PathHelper.IsPathSafe(_dataPath, file))
                    {
                        Log.Warning("Path traversal attempt detected on exam file: {File}", file);
                        continue;
                    }
                    var json = File.ReadAllText(file);
                    var exam = JsonSerializer.Deserialize<MockExamData>(json);
                    if (exam?.Metadata != null)
                    {
                        // Safe check: Only load exams that have questions
                        if (exam.Questions != null && exam.Questions.Count > 0)
                        {
                            _mockExams.Add(exam);
                        }
                        else
                        {
                            Log.Warning("Skip empty exam template: {Title}", exam.Metadata.Title);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Skip exam: {File}", Path.GetFileName(file));
                }
            }
            // Load tool-topic mapping
            var mappingFile = Path.Combine(_dataPath, "tool_topic_mapping.json");
            if (File.Exists(mappingFile))
            {
                try
                {
                    if (!QASmartClass.LearningTools.Helpers.PathHelper.IsPathSafe(_dataPath, mappingFile))
                    {
                        Log.Warning("Path traversal attempt detected on mapping file: {File}", mappingFile);
                        return;
                    }
                    var mappingJson = File.ReadAllText(mappingFile);
                    _topicMapping = JsonSerializer.Deserialize<ToolTopicMappingData>(mappingJson);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Skip topic mapping");
                }
            }

            Log.Information("MathCurriculum: Loaded {Chapters} chapters, {Exams} exams, mapping={HasMapping}",
                _chapters.Count, _mockExams.Count, _topicMapping != null);
        }

        // ═══════════════════════════════════════════════════════════
        //  TAB NAVIGATION
        // ═══════════════════════════════════════════════════════════

        private void BuildTabBar()
        {
            tabBar.Children.Clear();
            var tabs = new[] {
                ("train",     "🚂 Chuyến Tàu"),
                ("chapters",  "📚 Chương Trình"),
                ("exams",     "📝 Kho Đề Thi"),
                ("dashboard", "📊 Dashboard GV"),
            };
            foreach (var (id, label) in tabs)
            {
                var capturedId = id;
                var isActive = _activeTab == id;
                var tab = new Border
                {
                    Background = isActive ? DS.LightBg(DS.BrandPrimary) : Brushes.Transparent,
                    CornerRadius = new CornerRadius(DS.RadiusChip, DS.RadiusChip, 0, 0),
                    Padding = new Thickness(16, 8, 16, 8),
                    Margin = new Thickness(0, 0, 4, 0),
                    Cursor = Cursors.Hand,
                    BorderBrush = isActive ? DS.Brush(DS.BrandPrimary) : DS.BrushAlpha(DS.ResultInfo, 60),
                    BorderThickness = new Thickness(0, 0, 0, isActive ? 3 : 1),
                    MinHeight = DS.TouchMinHeight
                };
                tab.Child = new TextBlock
                {
                    Text = label,
                    FontSize = DS.FontPreset,
                    FontWeight = isActive ? FontWeights.Bold : FontWeights.SemiBold,
                    FontFamily = DS.FontPrimary,
                    Foreground = isActive ? DS.Brush(DS.BrandPrimary) : DS.Brush(DS.ResultInfo)
                };
                tab.MouseLeftButtonDown += (_, _) => SwitchTab(capturedId);
                tab.MouseEnter += (_, _) => { if (_activeTab != capturedId) tab.Background = DS.LightBg(DS.ResultInfo); };
                tab.MouseLeave += (_, _) => { if (_activeTab != capturedId) tab.Background = Brushes.Transparent; };
                tabBar.Children.Add(tab);
            }
        }

        private void SwitchTab(string tabId)
        {
            _activeTab = tabId;
            _selectedGrade = null;
            BuildTabBar();
            StopExamTimer();
            btnBackToChapters.Visibility = Visibility.Collapsed;

            switch (tabId)
            {
                case "train":
                    txtHeaderTitle.Text = "🚂 Chuyến Tàu Toán Học";
                    txtHeaderSub.Text = "Hành trình Lớp 1 → Lớp 12 • Khám phá toàn bộ chương trình Toán";
                    ShowTrainJourney();
                    break;
                case "chapters":
                    ShowChapterGrid();
                    break;
                case "exams":
                    txtHeaderTitle.Text = "📝 Kho Đề Thi Toán Học";
                    txtHeaderSub.Text = "Đề thi định kỳ • THPT Quốc Gia • Đánh Giá Năng Lực";
                    ShowExamsTab();
                    break;
                case "dashboard":
                    txtHeaderTitle.Text = "📊 Dashboard Giáo Viên";
                    txtHeaderSub.Text = "Tổng quan dữ liệu • Phân tích kết quả học tập";
                    ShowDashboard();
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TRAIN JOURNEY VIEW
        // ═══════════════════════════════════════════════════════════

        private void ShowTrainJourney()
        {
            mainPanel.Children.Clear();
            btnBackToChapters.Visibility = Visibility.Collapsed;
            AnimateMainPanel();

            // F4. Welcome Banner thông minh
            var hour = DateTime.Now.Hour;
            var greeting = hour < 12 ? "Buổi sáng tốt lành! ☀️" 
                         : hour < 18 ? "Buổi chiều vui vẻ! 🌤️" 
                         : "Buổi tối học tập hiệu quả! 🌙";
            
            var quotes = new[] {
                "Toán học là thể dục của tư duy.",
                "Hôm nay khó khăn, ngày mai sẽ tốt đẹp hơn.",
                "Không có con đường tắt nào dẫn đến thành công.",
                "Học Toán để thấy vẻ đẹp của sự logic."
            };
            var quote = quotes[new Random().Next(quotes.Length)];

            var welcomeBanner = new Border
            {
                Background = new LinearGradientBrush(
                    Color.FromRgb(232, 245, 233), Color.FromRgb(227, 242, 253),
                    new Point(0, 0), new Point(1, 0)),
                CornerRadius = new CornerRadius(DS.RadiusCard),
                Padding = new Thickness(20, 16, 20, 16),
                Margin = new Thickness(0, 0, 0, 16),
                BorderBrush = DS.BrushAlpha(DS.BrandPrimary, 30),
                BorderThickness = new Thickness(1)
            };
            var welcomeSp = new StackPanel();
            welcomeSp.Children.Add(new TextBlock
            {
                Text = greeting, FontSize = 16, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.BrandPrimary),
                Margin = new Thickness(0, 0, 0, 4)
            });
            welcomeSp.Children.Add(new TextBlock
            {
                Text = $"💡 {quote}", FontSize = 12, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.ResultInfo), FontStyle = FontStyles.Italic
            });
            
            // Stats summary row
            var totalProblems = _chapters.Sum(c => c.Metadata.TotalProblems);
            var statsWrap = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
            void AddStat(string txt, Color c)
            {
                var b = new Border
                {
                    Background = DS.LightBg(c), CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 0, 8, 0)
                };
                b.Child = new TextBlock
                {
                    Text = txt, FontSize = 12, FontWeight = FontWeights.SemiBold,
                    FontFamily = DS.FontPrimary, Foreground = DS.Brush(c)
                };
                statsWrap.Children.Add(b);
            }
            AddStat($"📚 {_chapters.Count} chương", DS.BrandPrimary);
            AddStat($"📝 {totalProblems} bài tập", DS.ResultSpecial);
            AddStat($"🎯 {_mockExams.Count} đề thi", DS.ResultDanger);
            AddStat("🚂 12 ga hành trình", DS.CatScience);
            welcomeSp.Children.Add(statsWrap);
            welcomeBanner.Child = welcomeSp;
            mainPanel.Children.Add(welcomeBanner);

            // Train track container
            var trackSp = new StackPanel { Margin = new Thickness(0, 8, 0, 16) };
            
            // Start Station Flag
            var startFlag = new Border
            {
                Background = DS.Brush(DS.ResultSuccess), CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(16, 0, 0, 4),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            startFlag.Child = new TextBlock { Text = "🚩 GA KHỞI HÀNH — LỚP 1", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brushes.White, FontFamily = DS.FontPrimary };
            trackSp.Children.Add(startFlag);

            string? lastLevel = null;
            for (int i = 0; i < GradeStations.Length; i++)
            {
                var station = GradeStations[i];
                if (station.Level != lastLevel)
                {
                    lastLevel = station.Level;
                    var (bannerBg, slogan, levelIcon) = station.Level switch
                    {
                        "Tiểu học" => (new LinearGradientBrush(Color.FromRgb(232, 245, 233), Color.FromRgb(200, 230, 201), new Point(0,0), new Point(1,0)), "Gieo hạt — Nền tảng vững chắc", "🌱"),
                        "THCS" => (new LinearGradientBrush(Color.FromRgb(227, 242, 253), Color.FromRgb(187, 222, 251), new Point(0,0), new Point(1,0)), "Vươn cao — Khám phá thế giới số", "🔭"),
                        _ => (new LinearGradientBrush(Color.FromRgb(243, 229, 245), Color.FromRgb(225, 190, 231), new Point(0,0), new Point(1,0)), "Chinh phục — Sẵn sàng cho tương lai", "🚀")
                    };

                    var levelBanner = new Border
                    {
                        Background = bannerBg, CornerRadius = new CornerRadius(DS.RadiusChip),
                        Padding = new Thickness(16, 10, 16, 10), Margin = new Thickness(36, 16, 0, 8),
                        BorderBrush = DS.BrushAlpha(station.Color, 40), BorderThickness = new Thickness(1),
                        Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 4, ShadowDepth = 1, Opacity = 0.05 }
                    };
                    var lvlSp = new StackPanel();
                    var lvlHeader = new DockPanel();
                    lvlHeader.Children.Add(new TextBlock { Text = levelIcon, FontSize = 16, Margin = new Thickness(0,0,8,0) });
                    lvlHeader.Children.Add(new TextBlock
                    {
                        Text = $"══ {station.Level.ToUpper()} ══", FontSize = 13, FontWeight = FontWeights.Bold,
                        FontFamily = DS.FontPrimary, Foreground = DS.Brush(station.Color), VerticalAlignment = VerticalAlignment.Center
                    });
                    lvlSp.Children.Add(lvlHeader);
                    lvlSp.Children.Add(new TextBlock
                    {
                        Text = slogan, FontSize = 12, FontFamily = DS.FontPrimary,
                        Foreground = DS.Brush(station.Color), FontStyle = FontStyles.Italic, Margin = new Thickness(24, 2, 0, 0)
                    });
                    levelBanner.Child = lvlSp;
                    trackSp.Children.Add(levelBanner);
                }

                var rowGrid = new Grid { Margin = new Thickness(0, 0, 0, 0) };
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // ── RAILWAY TRACK VISUAL ──
                var trackContainer = new Grid();
                Grid.SetColumn(trackContainer, 0);

                // Vertical Rails (Two lines)
                var railLeft = new Border { Background = DS.BrushAlpha(DS.ResultInfo, 40), Width = 2, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(14, 0, 0, 0) };
                var railRight = new Border { Background = DS.BrushAlpha(DS.ResultInfo, 40), Width = 2, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 14, 0) };
                trackContainer.Children.Add(railLeft);
                trackContainer.Children.Add(railRight);

                // Horizontal Ties (Railway sleepers)
                var tiesSp = new StackPanel();
                for (int t = 0; t < 5; t++)
                {
                    tiesSp.Children.Add(new Border
                    {
                        Background = DS.BrushAlpha(DS.ResultInfo, 25),
                        Height = 2, Width = 12,
                        Margin = new Thickness(0, 8, 0, 8),
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                }
                trackContainer.Children.Add(tiesSp);
                rowGrid.Children.Add(trackContainer);

                // Dot (Station Indicator)
                var dotContainer = new Grid { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 18, 0, 0) };
                var dotGlow = new System.Windows.Shapes.Ellipse
                {
                    Width = 24, Height = 24, Fill = DS.BrushAlpha(station.Color, 20),
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                };
                var dot = new System.Windows.Shapes.Ellipse
                {
                    Width = 14, Height = 14, Fill = Brushes.White,
                    Stroke = DS.Brush(station.Color), StrokeThickness = 3,
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                };
                dotContainer.Children.Add(dotGlow);
                dotContainer.Children.Add(dot);
                Grid.SetColumn(dotContainer, 0);
                rowGrid.Children.Add(dotContainer);

                // Card
                var card = CreateStationCard(station);
                Grid.SetColumn(card, 1);
                rowGrid.Children.Add(card);

                trackSp.Children.Add(rowGrid);
            }
            
            // End Station Flag
            var endFlag = new Border
            {
                Background = DS.Brush(DS.ResultDanger), CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(16, 8, 0, 24),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            endFlag.Child = new TextBlock { Text = "🏁 GA KẾT THÚC — LỚP 12", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brushes.White, FontFamily = DS.FontPrimary };
            trackSp.Children.Add(endFlag);

            mainPanel.Children.Add(trackSp);

            // Mock exams section at bottom
            if (_mockExams.Count > 0)
            {
                mainPanel.Children.Add(new TextBlock
                {
                    Text = "═══ ĐỀ THI THỬ THPT ═══",
                    FontSize = DS.FontLabel, FontWeight = FontWeights.Bold,
                    FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.ResultDanger),
                    Margin = new Thickness(36, 16, 0, 6)
                });
                var examWrap = new WrapPanel { Margin = new Thickness(36, 0, 0, 8) };
                int examIdx = 0;
                foreach (var exam in _mockExams)
                {
                    examIdx++;
                    examWrap.Children.Add(CreateExamCard(exam, examIdx, DS.ResultDanger));
                }
                mainPanel.Children.Add(examWrap);
            }
        }

        private void ShowExamsTab()
        {
            mainPanel.Children.Clear();
            btnBackToChapters.Visibility = Visibility.Collapsed;
            AnimateMainPanel();

            // Group exams by grade
            var examsByGrade = _mockExams.GroupBy(e => e.Metadata.Grade ?? 12).OrderBy(g => g.Key).ToList();

            if (examsByGrade.Count == 0)
            {
                mainPanel.Children.Add(new TextBlock
                {
                    Text = "Đang tải dữ liệu hoặc chưa có đề thi nào...",
                    FontSize = 16, FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(DS.ResultInfo), Margin = new Thickness(36, 16, 0, 0)
                });
                return;
            }

            foreach (var group in examsByGrade)
            {
                var gradeStr = group.Key == 12 ? "Lớp 12 / THPT Quốc Gia" : $"Lớp {group.Key}";
                mainPanel.Children.Add(new TextBlock
                {
                    Text = $"═══ ĐỀ THI {gradeStr} ═══",
                    FontSize = DS.FontLabel, FontWeight = FontWeights.Bold,
                    FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.ResultDanger),
                    Margin = new Thickness(36, 16, 0, 6)
                });
                var examWrap = new WrapPanel { Margin = new Thickness(36, 0, 0, 8) };
                int examIdx = 0;
                foreach (var exam in group.OrderBy(e => e.Metadata.ExamType).ThenBy(e => e.Metadata.Title))
                {
                    examIdx++;
                    examWrap.Children.Add(CreateExamCard(exam, examIdx, DS.ResultDanger));
                }
                mainPanel.Children.Add(examWrap);
            }
        }

        private Border CreateStationCard(
            (int Grade, string Label, string Emoji, string Level, Color Color, string Summary, string Inspiration) station)
        {
            var chaptersForGrade = _chapters.Where(c => c.Metadata.Grade == station.Grade).ToList();
            var hasData = chaptersForGrade.Count > 0 || HasExerciseData(station.Grade);
            var problemCount = chaptersForGrade.Sum(c => c.Metadata.TotalProblems);

            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(DS.RadiusCard),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = hasData ? Cursors.Hand : Cursors.Arrow,
                BorderBrush = hasData ? DS.BrushAlpha(station.Color, 60) : DS.BrushAlpha(DS.ResultInfo, 30),
                BorderThickness = new Thickness(1),
                Opacity = hasData ? 1.0 : 0.6,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 4, ShadowDepth = 1, Opacity = 0.05,
                    Color = Colors.Black, Direction = 270
                }
            };

            // Setup tooltip
            if (hasData && chaptersForGrade.Count > 0)
            {
                var ttSp = new StackPanel { Margin = new Thickness(4) };
                ttSp.Children.Add(new TextBlock { Text = $"Chương trình {station.Label}", FontWeight = FontWeights.Bold, Foreground = DS.Brush(station.Color) });
                foreach (var ch in chaptersForGrade)
                {
                    ttSp.Children.Add(new TextBlock { Text = $"• {ch.Metadata.ChapterName} ({ch.Metadata.TotalProblems} bài)", FontSize = 12, Margin = new Thickness(0,2,0,0) });
                }
                card.ToolTip = new ToolTip { Content = ttSp, Background = Brushes.White, BorderBrush = DS.BrushAlpha(station.Color, 50), BorderThickness = new Thickness(1) };
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left stripe
            var stripe = new Border
            {
                Background = DS.Brush(station.Color),
                CornerRadius = new CornerRadius(DS.RadiusCard, 0, 0, DS.RadiusCard),
                Width = 6
            };
            Grid.SetColumn(stripe, 0);
            grid.Children.Add(stripe);

            // Station icon
            var iconBorder = new Border
            {
                Background = DS.LightBg(station.Color),
                CornerRadius = new CornerRadius(20),
                Width = 40, Height = 40,
                Margin = new Thickness(12, 8, 8, 8),
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = new TextBlock
            {
                Text = station.Emoji, FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(iconBorder, 1);
            grid.Children.Add(iconBorder);

            // Station info
            var infSp = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 8, 8, 8)
            };
            var titleDock = new DockPanel();
            var statusText = hasData
                ? $"{chaptersForGrade.Count} chương • {problemCount} bài"
                : "Sắp ra mắt";
            var statusBadge = new Border
            {
                Background = DS.LightBg(DS.ResultInfo), CornerRadius = new CornerRadius(4),
                Padding = new Thickness(4, 1, 4, 1), Margin = new Thickness(8, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            DockPanel.SetDock(statusBadge, Dock.Right);
            statusBadge.Child = new TextBlock { Text = statusText, FontSize = 12, Foreground = DS.Brush(DS.ResultInfo), VerticalAlignment = VerticalAlignment.Center };
            titleDock.Children.Add(statusBadge);
            
            titleDock.Children.Add(new TextBlock
            {
                Text = station.Label, FontSize = DS.FontLabel,
                FontWeight = FontWeights.Bold, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(hasData ? station.Color : DS.ResultInfo),
                VerticalAlignment = VerticalAlignment.Center
            });
            infSp.Children.Add(titleDock);

            // F3. Add summary & inspiration
            infSp.Children.Add(new TextBlock
            {
                Text = $"📌 {station.Summary}", FontSize = 13, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.ResultDark), Margin = new Thickness(0, 4, 0, 2), TextWrapping = TextWrapping.Wrap
            });
            infSp.Children.Add(new TextBlock
            {
                Text = $"💡 {station.Inspiration}", FontSize = 12, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(station.Color), FontStyle = FontStyles.Italic, TextWrapping = TextWrapping.Wrap
            });

            Grid.SetColumn(infSp, 2);
            grid.Children.Add(infSp);

            // Right arrow or lock
            var rightIcon = new TextBlock
            {
                Text = hasData ? "→" : "🔒",
                FontSize = 16, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(hasData ? station.Color : DS.ResultInfo),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 14, 0)
            };
            Grid.SetColumn(rightIcon, 3);
            grid.Children.Add(rightIcon);

            card.Child = grid;

            if (hasData)
            {
                var capturedGrade = station.Grade;
                var capturedColor = station.Color;
                card.MouseLeftButtonDown += (_, _) => ShowGradeStation(capturedGrade, capturedColor);
                card.MouseEnter += (_, _) =>
                {
                    card.BorderBrush = DS.Brush(station.Color);
                    card.BorderThickness = new Thickness(2);
                };
                card.MouseLeave += (_, _) =>
                {
                    card.BorderBrush = DS.BrushAlpha(station.Color, 60);
                    card.BorderThickness = new Thickness(1);
                };
            }
            return card;
        }

        private bool HasExerciseData(int grade)
        {
            // Grades 1-12: check loaded chapter data from Phase4_Data JSON files
            if (_chapters.Any(c => c.Metadata.Grade == grade))
                return true;

            // Fallback: exercise bank folders for THCS (legacy structure)
            if (grade >= 6 && grade <= 7)
                return Directory.Exists(Path.Combine(_exercisePath, "G06_G07"));
            if (grade >= 8 && grade <= 9)
                return Directory.Exists(Path.Combine(_exercisePath, "G08_G09"));

            return false;
        }

        private void ShowGradeStation(int grade, Color color)
        {
            _selectedGrade = grade;
            mainPanel.Children.Clear();
            AnimateMainPanel();
            btnBackToChapters.Visibility = Visibility.Visible;

            var station = GradeStations.First(s => s.Grade == grade);
            txtHeaderTitle.Text = $"{station.Emoji} {station.Label} — {station.Level}";

            // Show chapters for this grade (from Phase4_Data)
            var chaptersForGrade = _chapters.Where(c => c.Metadata.Grade == grade).ToList();
            if (chaptersForGrade.Count > 0)
            {
                txtHeaderSub.Text = $"{chaptersForGrade.Count} chương • " +
                    $"{chaptersForGrade.Sum(c => c.Metadata.TotalProblems)} bài tập";

                var wrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
                int idx = 0;
                foreach (var ch in chaptersForGrade)
                {
                    idx++;
                    wrap.Children.Add(CreateChapterCard(ch, idx, color));
                }
                mainPanel.Children.Add(wrap);
            }
            else
            {
                txtHeaderSub.Text = "Bài tập từ ngân hàng đề";
                // Show exercise bank data for this grade
                mainPanel.Children.Add(BuildExerciseBankView(grade, color));
            }
        }

        private UIElement BuildExerciseBankView(int grade, Color color)
        {
            var sp = new StackPanel();
            string? folderName = null;
            if (grade >= 6 && grade <= 7) folderName = "G06_G07";
            else if (grade >= 8 && grade <= 9) folderName = "G08_G09";

            if (folderName == null)
            {
                sp.Children.Add(new TextBlock
                {
                    Text = "📦 Dữ liệu đang được chuẩn bị...",
                    FontSize = DS.FontLabel, FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(DS.ResultInfo), Margin = new Thickness(8, 20, 0, 0)
                });
                return sp;
            }

            var folder = Path.Combine(_exercisePath, folderName);
            if (!Directory.Exists(folder))
            {
                sp.Children.Add(new TextBlock
                {
                    Text = $"📦 Chưa có dữ liệu tại: {folderName}",
                    FontSize = DS.FontLabel, FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(DS.ResultWarning), Margin = new Thickness(8, 20, 0, 0)
                });
                return sp;
            }

            var files = Directory.GetFiles(folder, "EX_*.json");
            foreach (var file in files.OrderBy(f => f))
            {
                try
                {
                    if (!QASmartClass.LearningTools.Helpers.PathHelper.IsPathSafe(_exercisePath, file))
                    {
                        Log.Warning("Path traversal attempt detected on exercise file: {File}", file);
                        continue;
                    }
                    var json = File.ReadAllText(file);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var topicName = root.TryGetProperty("topicName", out var tn)
                        ? tn.GetString() ?? Path.GetFileNameWithoutExtension(file)
                        : Path.GetFileNameWithoutExtension(file).Replace("EX_", "").Replace("_", " ");

                    var exCount = root.TryGetProperty("exercises", out var exArr)
                        ? exArr.GetArrayLength() : 0;

                    var exCard = new Border
                    {
                        Background = Brushes.White,
                        CornerRadius = new CornerRadius(DS.RadiusChip),
                        Padding = new Thickness(14, 10, 14, 10),
                        Margin = new Thickness(0, 0, 0, 8),
                        BorderBrush = DS.BrushAlpha(color, 40),
                        BorderThickness = new Thickness(0, 0, 0, 2)
                    };
                    var exSp = new StackPanel();
                    exSp.Children.Add(new TextBlock
                    {
                        Text = $"📘 {topicName}",
                        FontSize = DS.FontLabel, FontWeight = FontWeights.SemiBold,
                        FontFamily = DS.FontPrimary, Foreground = DS.Brush(color)
                    });
                    exSp.Children.Add(new TextBlock
                    {
                        Text = $"{exCount} bài tập • {folderName}",
                        FontSize = DS.FontNote, FontFamily = DS.FontPrimary,
                        Foreground = DS.Brush(DS.ResultInfo), Margin = new Thickness(0, 2, 0, 0)
                    });
                    exCard.Child = exSp;
                    sp.Children.Add(exCard);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Skip exercise file: {File}", Path.GetFileName(file));
                }
            }
            return sp;
        }

        // ═══════════════════════════════════════════════════════════
        //  DASHBOARD VIEW
        // ═══════════════════════════════════════════════════════════

        private void ShowDashboard()
        {
            _currentDashboardView = "main";
            mainPanel.Children.Clear();
            btnBackToChapters.Visibility = Visibility.Collapsed;
            AnimateMainPanel();

            // Data overview card
            var overviewSp = new StackPanel();
            overviewSp.Children.Add(new TextBlock
            {
                Text = "📈 Tổng Quan Hệ Thống",
                FontSize = DS.FontTitle, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.BrandPrimary),
                Margin = new Thickness(0, 0, 0, 10)
            });

            var metricsWrap = new WrapPanel();
            void AddMetric(string label, string value, Color c)
            {
                var m = new Border
                {
                    Background = DS.LightBg(c), CornerRadius = new CornerRadius(DS.RadiusChip),
                    Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(0, 0, 8, 8),
                    MinWidth = 140
                };
                var mSp = new StackPanel();
                mSp.Children.Add(new TextBlock
                {
                    Text = value, FontSize = 20, FontWeight = FontWeights.Bold,
                    FontFamily = DS.FontPrimary, Foreground = DS.Brush(c)
                });
                mSp.Children.Add(new TextBlock
                {
                    Text = label, FontSize = DS.FontNote,
                    FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.ResultInfo)
                });
                m.Child = mSp;
                metricsWrap.Children.Add(m);
            }

            var totalProblems = _chapters.Sum(c => c.Metadata.TotalProblems);
            var totalHours = _chapters.Sum(c => c.Metadata.EstimatedHours);
            AddMetric("Chương đã tải", _chapters.Count.ToString(), DS.BrandPrimary);
            AddMetric("Bài tập", totalProblems.ToString(), DS.ResultSpecial);
            AddMetric("Đề thi thử", _mockExams.Count.ToString(), DS.ResultDanger);
            AddMetric("Tổng giờ học", $"~{totalHours}h", DS.CatScience);
            AddMetric("Lớp có dữ liệu", _chapters.Select(c => c.Metadata.Grade).Distinct().Count().ToString(), DS.BrandAccent);

            overviewSp.Children.Add(metricsWrap);

            var actionsSp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            actionsSp.Children.Add(UI.PresetButton("📝 Tạo Đề Kiểm Tra", DS.BrandPrimary, () => ShowCustomQuizBuilder()));
            actionsSp.Children.Add(UI.PresetButton("📊 Xem Lịch Sử Làm Bài", DS.BrandSecondary, () => ShowPerformanceTracking()));
            overviewSp.Children.Add(actionsSp);

            mainPanel.Children.Add(UI.SectionCard(overviewSp, DS.BrandPrimary));

            // ── 🚀 Quick Quiz Actions ──
            var quickSp = new StackPanel();
            quickSp.Children.Add(new TextBlock
            {
                Text = "🚀 Quiz Nhanh — Chọn chương để thi",
                FontSize = 15, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.ResultSpecial),
                Margin = new Thickness(0, 0, 0, 10)
            });
            quickSp.Children.Add(new TextBlock
            {
                Text = "Tạo quiz → Phát qua WebSocket → HS làm bài trên tablet → Chấm điểm tự động → Phân nhóm A/B/C",
                FontSize = DS.FontNote, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.ResultInfo), TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });

            var quizBtnWrap = new WrapPanel();
            foreach (var grp in _chapters.GroupBy(c => c.Metadata.Grade).OrderBy(g => g.Key))
            {
                var gStation = GradeStations.FirstOrDefault(s => s.Grade == grp.Key);
                var gColor = gStation.Color != default ? gStation.Color : DS.CatMath;
                var chList = grp.ToList();

                foreach (var ch in chList)
                {
                    var capturedCh = ch;
                    var btn = new Border
                    {
                        Background = DS.LightBg(gColor),
                        CornerRadius = new CornerRadius(DS.RadiusChip),
                        Padding = new Thickness(10, 6, 10, 6),
                        Margin = new Thickness(0, 0, 6, 6),
                        Cursor = Cursors.Hand,
                        BorderBrush = DS.BrushAlpha(gColor, 40),
                        BorderThickness = new Thickness(1)
                    };
                    var btnSp = new StackPanel { Orientation = Orientation.Horizontal };
                    btnSp.Children.Add(new TextBlock
                    {
                        Text = $"{gStation.Emoji} L{ch.Metadata.Grade}",
                        FontSize = DS.FontTag, FontWeight = FontWeights.Bold,
                        FontFamily = DS.FontPrimary, Foreground = DS.Brush(gColor),
                        Margin = new Thickness(0, 0, 4, 0)
                    });
                    btnSp.Children.Add(new TextBlock
                    {
                        Text = ch.Metadata.ChapterName.Length > 18
                            ? ch.Metadata.ChapterName[..18] + "…"
                            : ch.Metadata.ChapterName,
                        FontSize = DS.FontTag, FontFamily = DS.FontPrimary,
                        Foreground = DS.Brush(DS.ResultDark)
                    });
                    btn.Child = btnSp;
                    btn.MouseEnter += (_, _) => { btn.BorderBrush = DS.Brush(gColor); btn.BorderThickness = new Thickness(2); };
                    btn.MouseLeave += (_, _) => { btn.BorderBrush = DS.BrushAlpha(gColor, 40); btn.BorderThickness = new Thickness(1); };
                    btn.MouseLeftButtonDown += (_, _) =>
                    {
                        ShowQuizPreview(capturedCh);
                    };
                    quizBtnWrap.Children.Add(btn);
                }
            }
            quickSp.Children.Add(quizBtnWrap);
            mainPanel.Children.Add(UI.SectionCard(quickSp, DS.ResultSpecial));
            var breakdownSp = new StackPanel();
            breakdownSp.Children.Add(new TextBlock
            {
                Text = "📊 Phân Bổ Theo Lớp",
                FontSize = 15, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.BrandSecondary),
                Margin = new Thickness(0, 0, 0, 10)
            });

            foreach (var g in _chapters.GroupBy(c => c.Metadata.Grade).OrderBy(x => x.Key))
            {
                var gStation = GradeStations.FirstOrDefault(s => s.Grade == g.Key);
                var gColor = gStation.Color != default ? gStation.Color : DS.CatMath;
                var gProblems = g.Sum(c => c.Metadata.TotalProblems);
                var pct = totalProblems > 0 ? (double)gProblems / totalProblems * 100 : 0;

                var rowSp = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
                var labelRow = new DockPanel();
                labelRow.Children.Add(new TextBlock
                {
                    Text = $"{gStation.Emoji} Lớp {g.Key} ({g.Count()} ch.)",
                    FontSize = DS.FontPreset, FontWeight = FontWeights.SemiBold,
                    FontFamily = DS.FontPrimary, Foreground = DS.Brush(gColor)
                });
                var fracText = new TextBlock
                {
                    Text = $"{gProblems} bài ({pct:F0}%)",
                    FontSize = DS.FontNote, FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(gColor), HorizontalAlignment = HorizontalAlignment.Right
                };
                DockPanel.SetDock(fracText, Dock.Right);
                labelRow.Children.Insert(0, fracText);
                rowSp.Children.Add(labelRow);

                // Progress bar
                var barBg = new Border
                {
                    Background = DS.LightBg(gColor), CornerRadius = new CornerRadius(3),
                    Height = 8, Margin = new Thickness(0, 2, 0, 0)
                };
                var barFill = new Border
                {
                    Background = DS.Brush(gColor), CornerRadius = new CornerRadius(3),
                    Height = 8, HorizontalAlignment = HorizontalAlignment.Left, Width = 0
                };
                barBg.Child = barFill;
                barBg.Loaded += (s, _) =>
                {
                    barFill.Width = ((Border)s!).ActualWidth * (pct / 100.0);
                };
                barBg.SizeChanged += (s, _) =>
                {
                    barFill.Width = ((Border)s!).ActualWidth * (pct / 100.0);
                };
                rowSp.Children.Add(barBg);
                breakdownSp.Children.Add(rowSp);
            }
            mainPanel.Children.Add(UI.SectionCard(breakdownSp, DS.BrandSecondary));

            // ── 📊 Difficulty Distribution ──
            var diffSp = new StackPanel();
            diffSp.Children.Add(new TextBlock
            {
                Text = "📊 Phân Bổ Độ Khó",
                FontSize = 15, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.CatScience),
                Margin = new Thickness(0, 0, 0, 10)
            });

            int easyCount = 0, medCount = 0, hardCount = 0;
            foreach (var ch in _chapters)
                foreach (var sec in ch.Sections)
                    foreach (var p in sec.Problems)
                    {
                        var d = (p.Difficulty ?? "").ToLower();
                        if (d.Contains("easy")) easyCount++;
                        else if (d.Contains("hard")) hardCount++;
                        else medCount++;
                    }

            void AddDiffBar(string label, int count, Color c, string emoji)
            {
                var pct = totalProblems > 0 ? (double)count / totalProblems * 100 : 0;
                var row = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
                var lbl = new TextBlock
                {
                    Text = $"{emoji} {label}", FontSize = DS.FontPreset,
                    FontWeight = FontWeights.SemiBold, FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(c), Width = 100
                };
                row.Children.Add(lbl);
                var valTxt = new TextBlock
                {
                    Text = $"{count} ({pct:F0}%)", FontSize = DS.FontNote,
                    FontFamily = DS.FontPrimary, Foreground = DS.Brush(c),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(valTxt, Dock.Right);
                row.Children.Add(valTxt);
                var barBg2 = new Border
                {
                    Background = DS.LightBg(c), CornerRadius = new CornerRadius(4),
                    Height = 14, Margin = new Thickness(8, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var barFill2 = new Border
                {
                    Background = DS.Brush(c), CornerRadius = new CornerRadius(4),
                    Height = 14, HorizontalAlignment = HorizontalAlignment.Left
                };
                barBg2.Child = barFill2;
                barBg2.Loaded += (s, _) => { barFill2.Width = ((Border)s!).ActualWidth * (pct / 100.0); };
                barBg2.SizeChanged += (s, _) => { barFill2.Width = ((Border)s!).ActualWidth * (pct / 100.0); };
                row.Children.Add(barBg2);
                diffSp.Children.Add(row);
            }

            AddDiffBar("Easy", easyCount, Color.FromRgb(76, 175, 80), "🟢");
            AddDiffBar("Medium", medCount, Color.FromRgb(255, 152, 0), "🟡");
            AddDiffBar("Hard", hardCount, Color.FromRgb(244, 67, 54), "🔴");
            mainPanel.Children.Add(UI.SectionCard(diffSp, DS.CatScience));

            // ── 🎯 Quiz Analytics ──
            var quizSp = new StackPanel();
            quizSp.Children.Add(new TextBlock
            {
                Text = "🎯 Quiz Analytics",
                FontSize = 15, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.ResultSpecial),
                Margin = new Thickness(0, 0, 0, 10)
            });

            int totalQuizzes = 0, totalQuizQs = 0, chaptersWithQuiz = 0;
            foreach (var ch in _chapters)
            {
                if (ch.QuizBank?.Count > 0)
                {
                    chaptersWithQuiz++;
                    totalQuizzes += ch.QuizBank.Count;
                    totalQuizQs += ch.QuizBank.Sum(q => q.Questions?.Count ?? 0);
                }
            }

            var quizMetrics = new WrapPanel();
            void AddQuizStat(string label, string value, Color c)
            {
                var m = new Border
                {
                    Background = DS.LightBg(c), CornerRadius = new CornerRadius(DS.RadiusChip),
                    Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 8, 8),
                    MinWidth = 120
                };
                var mSp = new StackPanel();
                mSp.Children.Add(new TextBlock
                {
                    Text = value, FontSize = 18, FontWeight = FontWeights.Bold,
                    FontFamily = DS.FontPrimary, Foreground = DS.Brush(c)
                });
                mSp.Children.Add(new TextBlock
                {
                    Text = label, FontSize = DS.FontNote,
                    FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.ResultInfo)
                });
                m.Child = mSp;
                quizMetrics.Children.Add(m);
            }

            AddQuizStat("Tổng Quiz", totalQuizzes.ToString(), DS.ResultSpecial);
            AddQuizStat("Câu hỏi Quiz", totalQuizQs.ToString(), DS.BrandPrimary);
            AddQuizStat("Chương có Quiz", $"{chaptersWithQuiz}/{_chapters.Count}", DS.CatScience);
            int quizCovPct = _chapters.Count > 0 ? chaptersWithQuiz * 100 / _chapters.Count : 0;
            AddQuizStat("Coverage", $"{quizCovPct}%", quizCovPct >= 80
                ? Color.FromRgb(76, 175, 80) : Color.FromRgb(255, 152, 0));
            AddQuizStat("Đề thi THPT", _mockExams.Count.ToString(), DS.ResultDanger);
            int mockQs = _mockExams.Sum(e => e.Questions?.Count ?? 0);
            AddQuizStat("Câu THPT", mockQs.ToString(), DS.ResultDanger);

            quizSp.Children.Add(quizMetrics);
            mainPanel.Children.Add(UI.SectionCard(quizSp, DS.ResultSpecial));

            // ── 🎯 A/B/C Phân Nhóm Học Sinh (Auto-Grouping) ──
            var groupSp = new StackPanel();
            groupSp.Children.Add(new TextBlock
            {
                Text = "🎯 Phân Nhóm Tự Động A/B/C",
                FontSize = 15, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.CatMath),
                Margin = new Thickness(0, 0, 0, 6)
            });
            groupSp.Children.Add(new TextBlock
            {
                Text = "Sau khi chạy Quiz qua WebSocket, hệ thống tự phân nhóm HS dựa trên điểm:",
                FontSize = DS.FontNote, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.ResultInfo), TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var groupItems = new[]
            {
                ("A", "🟢 Nhóm A — Giỏi (≥ 8.0)", "Giao bài nâng cao, mentor bạn nhóm C", Color.FromRgb(76, 175, 80)),
                ("B", "🟡 Nhóm B — Khá (5.0–7.9)", "Luyện tập bổ sung, Spaced Repetition", Color.FromRgb(255, 152, 0)),
                ("C", "🔴 Nhóm C — Cần cải thiện (< 5.0)", "Ôn tập lại lý thuyết, hỗ trợ 1-1", Color.FromRgb(244, 67, 54))
            };

            foreach (var (grp, title, desc, color) in groupItems)
            {
                var grpRow = new Border
                {
                    Background = DS.LightBg(color),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                var grpDock = new DockPanel();
                var grpBadge = new Border
                {
                    Background = DS.Brush(color),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                grpBadge.Child = new TextBlock
                {
                    Text = grp, FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White, FontFamily = DS.FontPrimary
                };
                grpDock.Children.Add(grpBadge);
                var grpInfo = new StackPanel();
                grpInfo.Children.Add(new TextBlock
                {
                    Text = title, FontSize = DS.FontPreset, FontWeight = FontWeights.SemiBold,
                    FontFamily = DS.FontPrimary, Foreground = DS.Brush(color)
                });
                grpInfo.Children.Add(new TextBlock
                {
                    Text = desc, FontSize = DS.FontNote,
                    FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.ResultInfo),
                    TextWrapping = TextWrapping.Wrap
                });
                grpDock.Children.Add(grpInfo);
                grpRow.Child = grpDock;
                groupSp.Children.Add(grpRow);
            }

            // Status indicator
            groupSp.Children.Add(new TextBlock
            {
                Text = "💡 Kết nối: MathQuizBridgeService → WebSocket → Auto-scoring → A/B/C grouping",
                FontSize = DS.FontTag, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.CatScience), FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 8, 0, 0)
            });
            mainPanel.Children.Add(UI.SectionCard(groupSp, DS.CatMath));

            // ── 📅 Spaced Repetition Tracker ──
            var srSp = new StackPanel();
            srSp.Children.Add(new TextBlock
            {
                Text = "📅 Spaced Repetition Tracker",
                FontSize = 15, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.BrandAccent),
                Margin = new Thickness(0, 0, 0, 6)
            });
            srSp.Children.Add(new TextBlock
            {
                Text = "Theo dõi lịch ôn tập theo thuật toán SM-2. Hệ thống gợi ý chương cần ôn dựa trên lần quiz gần nhất.",
                FontSize = DS.FontNote, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.ResultInfo), TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // SR schedule visualization
            var srGrid = new UniformGrid { Columns = 7, Margin = new Thickness(0, 0, 0, 8) };
            var dayNames = new[] { "T2", "T3", "T4", "T5", "T6", "T7", "CN" };
            foreach (var day in dayNames)
            {
                srGrid.Children.Add(new TextBlock
                {
                    Text = day, FontSize = DS.FontTag, FontWeight = FontWeights.Bold,
                    FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.ResultInfo),
                    HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 4)
                });
            }

            // 4 weeks of SR cells (placeholder visualization)
            var rng = new Random(42);
            for (int week = 0; week < 4; week++)
            {
                for (int day = 0; day < 7; day++)
                {
                    var intensity = rng.Next(0, 4); // 0=empty, 1-3=review intensity
                    var cellColor = intensity switch
                    {
                        3 => DS.BrandPrimary,
                        2 => DS.CatScience,
                        1 => DS.BrandAccent,
                        _ => Color.FromRgb(240, 240, 240)
                    };
                    var cell = new Border
                    {
                        Background = DS.Brush(cellColor),
                        CornerRadius = new CornerRadius(3),
                        Width = 20, Height = 20,
                        Margin = new Thickness(2),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Opacity = intensity > 0 ? 0.4 + intensity * 0.2 : 0.3,
                        ToolTip = intensity > 0 ? $"Tuần {week + 1}, {dayNames[day]}: {intensity} chương cần ôn" : "Không có lịch ôn"
                    };
                    srGrid.Children.Add(cell);
                }
            }
            srSp.Children.Add(srGrid);

            // SR legend
            var legendSp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            void AddLegend(string text, Color c, double opacity)
            {
                var legDot = new Border
                {
                    Background = DS.Brush(c), CornerRadius = new CornerRadius(3),
                    Width = 12, Height = 12, Margin = new Thickness(0, 0, 4, 0),
                    Opacity = opacity
                };
                legendSp.Children.Add(legDot);
                legendSp.Children.Add(new TextBlock
                {
                    Text = text, FontSize = DS.FontTag, FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(DS.ResultInfo), Margin = new Thickness(0, 0, 12, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            AddLegend("Ít", DS.BrandAccent, 0.6);
            AddLegend("Vừa", DS.CatScience, 0.7);
            AddLegend("Nhiều", DS.BrandPrimary, 0.9);
            srSp.Children.Add(legendSp);

            mainPanel.Children.Add(UI.SectionCard(srSp, DS.BrandAccent));
            var toolSp = new StackPanel();
            toolSp.Children.Add(new TextBlock
            {
                Text = "🔗 Tool Coverage Map",
                FontSize = 15, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.BrandAccent),
                Margin = new Thickness(0, 0, 0, 10)
            });

            var toolMap = new Dictionary<string, List<string>>();
            foreach (var ch in _chapters)
            {
                if (ch.Metadata.ToolMapping == null) continue;
                foreach (var tool in ch.Metadata.ToolMapping)
                {
                    if (!toolMap.ContainsKey(tool)) toolMap[tool] = new();
                    toolMap[tool].Add($"L{ch.Metadata.Grade} {ch.Metadata.ChapterName}");
                }
            }

            foreach (var kv in toolMap.OrderByDescending(x => x.Value.Count))
            {
                var toolRow = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
                var toolBadge = new Border
                {
                    Background = DS.LightBg(DS.BrandAccent),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 3, 8, 3),
                    Margin = new Thickness(0, 0, 8, 0)
                };
                toolBadge.Child = new TextBlock
                {
                    Text = $"🔧 {kv.Key}", FontSize = DS.FontNote,
                    FontWeight = FontWeights.SemiBold, FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(DS.BrandAccent)
                };
                toolRow.Children.Add(toolBadge);
                var countBadge = new Border
                {
                    Background = DS.Brush(DS.BrandAccent),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(6, 1, 6, 1),
                    VerticalAlignment = VerticalAlignment.Center
                };
                countBadge.Child = new TextBlock
                {
                    Text = kv.Value.Count.ToString(), FontSize = DS.FontTag,
                    FontWeight = FontWeights.Bold, Foreground = Brushes.White,
                    FontFamily = DS.FontPrimary
                };
                DockPanel.SetDock(countBadge, Dock.Right);
                toolRow.Children.Add(countBadge);
                toolRow.Children.Add(new TextBlock
                {
                    Text = string.Join(", ", kv.Value.Take(3)) + (kv.Value.Count > 3 ? $" +{kv.Value.Count - 3}" : ""),
                    FontSize = DS.FontNote, FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(DS.ResultInfo), TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                });
                toolSp.Children.Add(toolRow);
            }

            var unmappedTools = ToolRegistry.AllTools
                .Where(t => t.Category == ToolCategory.Math && !toolMap.ContainsKey(t.Id + "Tool") && !toolMap.ContainsKey(t.Id))
                .ToList();
            if (unmappedTools.Count > 0)
            {
                toolSp.Children.Add(new TextBlock
                {
                    Text = $"⚠️ {unmappedTools.Count} tool chưa được map:",
                    FontSize = DS.FontNote, FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(DS.ResultWarning),
                    Margin = new Thickness(0, 8, 0, 4)
                });
                var unmappedWrap = new WrapPanel();
                foreach (var t in unmappedTools)
                {
                    var badge = new Border
                    {
                        Background = DS.LightBg(DS.ResultWarning),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(6, 2, 6, 2),
                        Margin = new Thickness(0, 0, 4, 4)
                    };
                    badge.Child = new TextBlock
                    {
                        Text = t.Name, FontSize = DS.FontTag,
                        FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.ResultWarning)
                    };
                    unmappedWrap.Children.Add(badge);
                }
                toolSp.Children.Add(unmappedWrap);
            }

            mainPanel.Children.Add(UI.SectionCard(toolSp, DS.BrandAccent));

            // ── 🏆 BẢNG XẾP HẠNG THỜI GIAN THỰC ──
            var leaderboardSp = new StackPanel();
            leaderboardSp.Children.Add(new TextBlock
            {
                Text = "🏆 Bảng Xếp Hạng Lớp Học (Thời Gian Thực)",
                FontSize = 15, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.ResultSpecial),
                Margin = new Thickness(0, 0, 0, 6)
            });
            leaderboardSp.Children.Add(new TextBlock
            {
                Text = "Hiển thị Top 5 học sinh dẫn đầu lớp về điểm số và chuỗi trả lời đúng (Streak) cập nhật qua WebSocket:",
                FontSize = DS.FontNote, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.ResultInfo), TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });

            _leaderboardListPanel = new StackPanel();
            leaderboardSp.Children.Add(_leaderboardListPanel);
            mainPanel.Children.Add(UI.SectionCard(leaderboardSp, DS.ResultSpecial));

            StartLeaderboardTimer();
        }

        // ═══════════════════════════════════════════════════════════
        //  CHAPTER GRID VIEW
        // ═══════════════════════════════════════════════════════════

        private void ShowChapterGrid()
        {
            mainPanel.Children.Clear();
            btnBackToChapters.Visibility = Visibility.Collapsed;
            AnimateMainPanel();
            txtHeaderTitle.Text = "🚂 Hệ Thống Toán Học THPT";
            txtHeaderSub.Text = $"{_chapters.Count} chương • " +
                $"{_chapters.Sum(c => c.Metadata.TotalProblems)} bài tập";

            // Group by grade
            foreach (var gradeGroup in _chapters.GroupBy(c => c.Metadata.Grade).OrderBy(g => g.Key))
            {
                var grade = gradeGroup.Key;
                var gradeColor = grade switch
                {
                    10 => DS.CatMath,
                    11 => Color.FromRgb(123, 31, 162),
                    12 => Color.FromRgb(230, 81, 0),
                    _ => DS.CatMath
                };

                // Grade header
                var header = new TextBlock
                {
                    Text = $"📚 LỚP {grade} ({gradeGroup.Count()} chương)",
                    FontSize = DS.FontLabel, FontWeight = FontWeights.Bold,
                    FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(gradeColor),
                    Margin = new Thickness(4, 12, 0, 8)
                };
                mainPanel.Children.Add(header);

                var wrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
                int idx = 0;
                foreach (var ch in gradeGroup)
                {
                    idx++;
                    wrap.Children.Add(CreateChapterCard(ch, idx, gradeColor));
                }
                mainPanel.Children.Add(wrap);
            }

            // Mock exams section
            if (_mockExams.Count > 0)
            {
                var examColor = Color.FromRgb(198, 40, 40);
                mainPanel.Children.Add(new TextBlock
                {
                    Text = $"📝 ĐỀ THI THỬ THPT ({_mockExams.Count} đề)",
                    FontSize = DS.FontLabel, FontWeight = FontWeights.Bold,
                    FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(examColor),
                    Margin = new Thickness(4, 16, 0, 8)
                });

                var examWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
                int examIdx = 0;
                foreach (var exam in _mockExams)
                {
                    examIdx++;
                    examWrap.Children.Add(CreateExamCard(exam, examIdx, examColor));
                }
                mainPanel.Children.Add(examWrap);
            }
        }

        private Border CreateChapterCard(MathChapterData ch, int idx, Color color)
        {
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(14),
                Width = 240, Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 16, 16),
                BorderBrush = DS.BrushAlpha(color, 30),
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 12, ShadowDepth = 2, Opacity = 0.08,
                    Color = Colors.Black, Direction = 270
                }
            };

            var mainSp = new StackPanel();

            // Thumbnail Area (Top)
            var thumbnail = new Border
            {
                Height = 120,
                CornerRadius = new CornerRadius(13, 13, 0, 0),
                Background = new LinearGradientBrush(
                    ((SolidColorBrush)DS.LightBg(color)).Color,
                    ((SolidColorBrush)DS.BrushAlpha(color, 20)).Color,
                    new Point(0, 0), new Point(1, 1))
            };
            var iconTxt = new TextBlock
            {
                Text = "📐",
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.8
            };
            thumbnail.Child = iconTxt;
            mainSp.Children.Add(thumbnail);

            // Content Area (Bottom)
            var contentSp = new StackPanel { Margin = new Thickness(14, 12, 14, 14) };

            var titleDock = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            var badge = new Border
            {
                Background = DS.LightBg(color),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            badge.Child = new TextBlock
            {
                Text = $"CH{idx:D2}", FontSize = 12,
                FontWeight = FontWeights.Bold, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(color)
            };
            DockPanel.SetDock(badge, Dock.Left);
            titleDock.Children.Add(badge);

            var titleTxt = new TextBlock
            {
                Text = ch.Metadata.ChapterName,
                FontSize = 15, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.ResultDark),
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 44, // roughly 2 lines
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            titleDock.Children.Add(titleTxt);
            contentSp.Children.Add(titleDock);

            // Stats
            contentSp.Children.Add(new TextBlock
            {
                Text = $"{ch.Metadata.TotalSections} phần • {ch.Metadata.TotalProblems} bài",
                FontSize = 12, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.ResultInfo),
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Tags
            var tagPanel = new WrapPanel();
            var gradeBadge = new Border
            {
                Background = DS.Brush(color),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 6, 0)
            };
            gradeBadge.Child = new TextBlock
            {
                Text = $"Lớp {ch.Metadata.Grade}", FontSize = 12,
                Foreground = Brushes.White, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary
            };
            tagPanel.Children.Add(gradeBadge);

            if (!string.IsNullOrEmpty(ch.Metadata.ThptWeight))
            {
                var weightBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 243, 224)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 3, 8, 3)
                };
                weightBadge.Child = new TextBlock
                {
                    Text = $"🎯 {ch.Metadata.ThptWeight}", FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0)),
                    FontFamily = DS.FontPrimary, FontWeight = FontWeights.SemiBold
                };
                tagPanel.Children.Add(weightBadge);
            }
            contentSp.Children.Add(tagPanel);

            mainSp.Children.Add(contentSp);
            card.Child = mainSp;

            // Click → open chapter detail
            var capturedCh = ch;
            card.MouseLeftButtonDown += (_, _) => ShowChapterDetail(capturedCh);
            card.MouseEnter += (_, _) =>
            {
                card.BorderBrush = DS.Brush(color);
                card.BorderThickness = new Thickness(2);
            };
            card.MouseLeave += (_, _) =>
            {
                card.BorderBrush = DS.BrushAlpha(color, 30);
                card.BorderThickness = new Thickness(1);
            };

            return card;
        }

        // ═══════════════════════════════════════════════════════════
        //  CHAPTER DETAIL VIEW
        // ═══════════════════════════════════════════════════════════

        private void ShowChapterDetail(MathChapterData ch)
        {
            mainPanel.Children.Clear();
            btnBackToChapters.Visibility = Visibility.Visible;
            AnimateMainPanel();
            txtHeaderTitle.Text = $"📖 {ch.Metadata.ChapterName}";
            txtHeaderSub.Text = $"Lớp {ch.Metadata.Grade} • " +
                $"{ch.Metadata.TotalSections} phần • {ch.Metadata.TotalProblems} bài";
            // Action bar
            var actionBar = new WrapPanel { Margin = new Thickness(0, 0, 0, 16) };
            UI.PresetButton("🎮 Luyện Tập", DS.CatMath, () => ShowPracticeMode(ch), actionBar).Margin = new Thickness(0, 0, 8, 0);
            UI.PresetButton("🃏 Flashcard", DS.ResultPrimary, () => ShowFlashcardMode(ch), actionBar);
            mainPanel.Children.Add(actionBar);

            // Open Challenge (Banner nổi bật đầu chương)
            if (!string.IsNullOrEmpty(ch.OpenChallenge))
            {
                var challengeCard = new Border
                {
                    Background = new System.Windows.Media.LinearGradientBrush(DS.Brush(DS.ResultDanger).Color, System.Windows.Media.Colors.DarkRed, 0),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 12)
                };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = "🏆 Thử Thách Mở", FontWeight = FontWeights.Bold, Foreground = System.Windows.Media.Brushes.White, FontSize = 14 });
                sp.Children.Add(new TextBlock { Text = ch.OpenChallenge, Foreground = System.Windows.Media.Brushes.White, FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) });
                challengeCard.Child = sp;
                mainPanel.Children.Add(challengeCard);
            }

            // Fun Facts (Đầu Chapter Detail, nền vàng nhạt)
            if (ch.FunFacts?.Count > 0)
            {
                var sp = new StackPanel();
                sp.Children.Add(CreateSectionHeader("💡 Bạn Có Biết?", "#E65100"));
                foreach (var fact in ch.FunFacts)
                {
                    var row = UI.ResultRow(fact, DS.ResultWarning);
                    row.Margin = new Thickness(0, 0, 0, 4);
                    sp.Children.Add(row);
                }
                mainPanel.Children.Add(UI.SectionCard(sp, DS.ResultWarning));
            }

            // Tool links
            if (ch.Metadata.ToolMapping?.Count > 0)
            {
                var toolCard = UI.SectionCard(BuildToolLinks(ch.Metadata.ToolMapping), DS.CatMath);
                mainPanel.Children.Add(toolCard);
            }

            // Real World Apps (Ngay trên Concepts)
            if (ch.RealWorldApps?.Count > 0)
            {
                var sp = new StackPanel();
                sp.Children.Add(CreateSectionHeader("🌍 Toán Trong Đời Sống", "#2E7D32"));
                var wrap = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
                foreach (var app in ch.RealWorldApps)
                {
                    var appBorder = new Border
                    {
                        Background = DS.LightBg(DS.ResultSuccess),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(8),
                        Margin = new Thickness(0, 0, 8, 8),
                        Width = 200
                    };
                    var appSp = new StackPanel();
                    appSp.Children.Add(new TextBlock { Text = $"{app.Icon} {app.Title}", FontWeight = FontWeights.Bold, Foreground = DS.Brush(DS.ResultSuccess), TextWrapping = TextWrapping.Wrap });
                    appSp.Children.Add(new TextBlock { Text = app.Desc, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) });
                    appBorder.Child = appSp;
                    wrap.Children.Add(appBorder);
                }
                sp.Children.Add(wrap);
                mainPanel.Children.Add(UI.SectionCard(sp, DS.ResultSuccess));
            }

            // Sections
            foreach (var section in ch.Sections.OrderBy(s => s.Order))
            {
                mainPanel.Children.Add(BuildSection(section, ch.Metadata.ChapterId));
            }

            // Common mistakes
            if (ch.CommonMistakes?.Count > 0)
            {
                mainPanel.Children.Add(BuildMistakes(ch.CommonMistakes));
            }

            // Study Tips (Dưới Common Mistakes)
            if (ch.StudyTips?.Count > 0)
            {
                var sp = new StackPanel();
                sp.Children.Add(CreateSectionHeader("💡 Mẹo Học Tốt", "#757575"));
                foreach (var tip in ch.StudyTips)
                {
                    sp.Children.Add(UI.ResultRow(tip, DS.ResultInfo));
                }
                mainPanel.Children.Add(UI.SectionCard(sp, DS.ResultInfo));
            }

            // History Note (Footer chương, dạng timeline nhỏ)
            if (!string.IsNullOrEmpty(ch.HistoryNote))
            {
                var sp = new StackPanel();
                sp.Children.Add(CreateSectionHeader("📜 Lịch Sử Toán Học", "#1565C0"));
                var border = new Border
                {
                    BorderBrush = DS.Brush(DS.ResultPrimary),
                    BorderThickness = new Thickness(2, 0, 0, 0),
                    Padding = new Thickness(8, 0, 0, 0),
                    Margin = new Thickness(4, 4, 0, 0)
                };
                border.Child = new TextBlock { Text = ch.HistoryNote, TextWrapping = TextWrapping.Wrap, FontStyle = FontStyles.Italic };
                sp.Children.Add(border);
                mainPanel.Children.Add(UI.SectionCard(sp, DS.ResultPrimary));
            }
        }

        private StackPanel BuildToolLinks(List<string> tools)
        {
            var sp = new StackPanel();
            sp.Children.Add(CreateSectionHeader("🔗 Công cụ liên quan", "#1565C0"));
            var wrap = new WrapPanel();
            foreach (var toolName in tools)
            {
                var toolDef = ToolRegistry.AllTools
                    .FirstOrDefault(t => t.Name.Contains(toolName.Replace("Tool", ""))
                        || toolName.Contains(t.Id, StringComparison.OrdinalIgnoreCase));
                var label = toolDef?.Name ?? toolName;
                var btn = UI.PresetButton($"🔧 {label}", DS.CatMath, () =>
                {
                    if (toolDef != null)
                    {
                        var control = LearningToolsHub.CreateToolControl(toolDef.Id);
                        if (control != null)
                        {
                            mainPanel.Visibility = Visibility.Collapsed;
                            relatedToolContainer.Visibility = Visibility.Visible;
                            relatedToolContainer.Children.Clear();
                            
                            var btnBack = new Button
                            {
                                Content = "← Quay lại nội dung bài học",
                                FontSize = 14,
                                Foreground = DS.Brush(DS.BrandPrimary),
                                Background = Brushes.Transparent,
                                BorderThickness = new Thickness(0),
                                HorizontalAlignment = HorizontalAlignment.Left,
                                Cursor = Cursors.Hand,
                                Margin = new Thickness(0, 0, 0, 16),
                                FontFamily = DS.FontPrimary,
                                FontWeight = FontWeights.Bold
                            };
                            btnBack.Click += (s, eArgs) => 
                            {
                                relatedToolContainer.Visibility = Visibility.Collapsed;
                                relatedToolContainer.Children.Clear();
                                mainPanel.Visibility = Visibility.Visible;
                            };
                            
                            relatedToolContainer.Children.Add(btnBack);
                            relatedToolContainer.Children.Add(control);
                        }
                    }
                }, wrap);
            }
            sp.Children.Add(wrap);
            return sp;
        }

        private UIElement BuildSection(ChapterSection section, string chapterId)
        {
            var sectionGrid = new Grid();
            sectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) });
            sectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            sectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6, GridUnitType.Star) });

            var leftSp = new StackPanel();
            var rightSp = new StackPanel();
            Grid.SetColumn(leftSp, 0);
            Grid.SetColumn(rightSp, 2);
            sectionGrid.Children.Add(leftSp);
            sectionGrid.Children.Add(rightSp);

            // Concepts (Left)
            leftSp.Children.Add(CreateSectionHeader($"📘 {section.Order}. {section.SectionName} - Lý thuyết", "#0D47A1", 14));
            if (section.Concepts.Count > 0)
            {
                foreach (var c in section.Concepts)
                {
                    leftSp.Children.Add(BuildConceptCard(c));
                }
            }
            else
            {
                leftSp.Children.Add(new TextBlock { Text = "Chưa có lý thuyết cho phần này.", Foreground = Brushes.Gray, FontStyle = FontStyles.Italic, Margin = new Thickness(0,8,0,0) });
            }

            // Problems (Right)
            rightSp.Children.Add(CreateSectionHeader($"📝 Bài tập ({section.Problems.Count})", "#1565C0", 14));
            if (section.Problems.Count > 0)
            {
                var progressTb = new TextBlock
                {
                    Text = $"Tiến trình: 0 / {section.Problems.Count} bài",
                    FontSize = 13, FontWeight = FontWeights.Bold,
                    Foreground = DS.Brush(DS.ResultSuccess),
                    Margin = new Thickness(0, 0, 0, 12)
                };
                rightSp.Children.Add(progressTb);

                int correctCount = 0;
                Action onCorrect = () => {
                    correctCount++;
                    progressTb.Text = $"Tiến trình: {correctCount} / {section.Problems.Count} bài";
                    if (correctCount == section.Problems.Count) {
                        progressTb.Text += " 🎉 Hoàn thành xuất sắc!";
                        progressTb.Foreground = DS.Brush(Color.FromRgb(46, 125, 50));
                    }
                };

                foreach (var p in section.Problems)
                {
                    rightSp.Children.Add(BuildProblemCard(p, onCorrect));
                }
            }

            var sectionCard = UI.SectionCard(sectionGrid, Color.FromRgb(187, 222, 251));
            return WrapWithSectionToolbar(sectionCard, "math_curriculum",
                section.SectionId, section.SectionName);
        }

        private Border BuildConceptCard(Concept c)
        {
            var sp = new StackPanel();
            
            // Determine card type based on content
            bool hasTheorem = !string.IsNullOrEmpty(c.Theorem);
            bool hasFormula = !string.IsNullOrEmpty(c.Formula) || (c.Formulas != null && c.Formulas.Count > 0);
            
            Color bgColor = Brushes.White.Color;
            Color borderColor = Color.FromArgb(40, 0, 121, 107);
            string iconPrefix = "📘";

            if (hasTheorem)
            {
                bgColor = Color.FromRgb(255, 249, 196); // Light Yellow
                borderColor = Color.FromRgb(251, 192, 45);
                iconPrefix = "📌";
            }
            else if (hasFormula)
            {
                bgColor = Color.FromRgb(227, 242, 253); // Light Blue
                borderColor = Color.FromRgb(33, 150, 243);
                iconPrefix = "📐";
            }

            sp.Children.Add(new TextBlock
            {
                Text = $"{iconPrefix} {c.Name}", FontSize = 14,
                FontWeight = FontWeights.Bold, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.ResultDark)
            });

            if (!string.IsNullOrEmpty(c.Definition))
            {
                var defElement = UI.RenderMixedContent(c.Definition, 13, DS.Brush(DS.ResultInfo));
                if (defElement is FrameworkElement fe)
                {
                    fe.Margin = new Thickness(0, 6, 0, 0);
                }
                sp.Children.Add(defElement);
            }

            if (!string.IsNullOrEmpty(c.Theorem))
                UI.ResultRow($"📜 {c.Theorem}", DS.ResultDark, sp);

            if (!string.IsNullOrEmpty(c.Formula))
                UI.ResultRow($"📐 {c.Formula}", DS.ResultSpecial, sp);

            foreach (var f in c.Formulas ?? new())
                UI.ResultRow($"  • {f}", DS.ResultPrimary, sp);
            foreach (var p in c.Properties ?? new())
                UI.ResultRow($"  ▸ {p}", DS.ResultInfo, sp);
            foreach (var r in c.Rules ?? new())
                UI.ResultRow($"  📌 {r}", DS.ResultWarning, sp);
            foreach (var cs in c.Cases ?? new())
                UI.ResultRow($"  🔹 {cs}", DS.ResultInfo, sp);

            // Method: can be string or List<string>
            if (c.Method != null)
            {
                if (c.Method is System.Text.Json.JsonElement je)
                {
                    if (je.ValueKind == System.Text.Json.JsonValueKind.String)
                        UI.ResultRow($"  📋 {je.GetString()}", DS.ResultPrimary, sp);
                    else if (je.ValueKind == System.Text.Json.JsonValueKind.Array)
                        foreach (var item in je.EnumerateArray())
                            UI.ResultRow($"  📋 {item.GetString()}", DS.ResultPrimary, sp);
                }
            }

            // Types dictionary or array
            if (c.Types != null)
            {
                if (c.Types is System.Text.Json.JsonElement je)
                {
                    if (je.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in je.EnumerateArray())
                            UI.ResultRow($"  ▹ {item.GetString()}", DS.ResultInfo, sp);
                    }
                    else if (je.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        foreach (var prop in je.EnumerateObject())
                            UI.ResultRow($"  ▹ {prop.Name}: {prop.Value.GetString()}", DS.ResultInfo, sp);
                    }
                }
            }

            // Details: dict-like object with key-value pairs
            if (c.Details is System.Text.Json.JsonElement detailsEl
                && detailsEl.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var prop in detailsEl.EnumerateObject())
                    UI.ResultRow($"  🔸 {prop.Name}: {prop.Value.GetString()}", DS.ResultPrimary, sp);
            }

            // Examples: can be string[] or object[] (e.g. {z, real, imaginary})
            if (c.Examples is System.Text.Json.JsonElement examplesEl
                && examplesEl.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in examplesEl.EnumerateArray())
                {
                    if (item.ValueKind == System.Text.Json.JsonValueKind.String)
                        UI.ResultRow($"  💡 {item.GetString()}", DS.ResultInfo, sp);
                    else if (item.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        // Flatten object fields: {"z": "3+2i", "real": 3} → "z = 3+2i, real = 3"
                        var parts = new List<string>();
                        foreach (var p2 in item.EnumerateObject())
                            parts.Add($"{p2.Name}={p2.Value}");
                        UI.ResultRow($"  💡 {string.Join(", ", parts)}", DS.ResultInfo, sp);
                    }
                }
            }

            // Note
            if (!string.IsNullOrEmpty(c.Note))
                UI.ResultRow($"  📝 {c.Note}", DS.ResultWarning, sp);

            var card = new Border
            {
                Background = new SolidColorBrush(bgColor),
                CornerRadius = new CornerRadius(DS.RadiusCard),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = new SolidColorBrush(borderColor),
                BorderThickness = new Thickness(1, 1, 1, 3),
                Child = sp
            };
            return card;
        }

        private Border BuildProblemCard(Problem p, Action onCorrect = null)
        {
            var sp = new StackPanel();

            // Header: difficulty + points + bookmark
            var headerDock = new DockPanel();
            var diffColor = p.Difficulty switch
            {
                "Easy" => Color.FromRgb(46, 125, 50),
                "Hard" => Color.FromRgb(198, 40, 40),
                "Extreme" => Color.FromRgb(106, 27, 154),
                _ => Color.FromRgb(230, 81, 0)
            };
            var diffBadge = new Border
            {
                Background = DS.LightBg(diffColor),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2)
            };
            diffBadge.Child = new TextBlock
            {
                Text = p.Difficulty, FontSize = DS.FontTag,
                FontWeight = FontWeights.SemiBold, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(diffColor)
            };
            headerDock.Children.Add(diffBadge);

            var rightHeaderSp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            DockPanel.SetDock(rightHeaderSp, Dock.Right);

            var flagBtn = UI.PresetButton("🚩", DS.ResultDanger, () => {
                MessageBox.Show("Đã ghim bài toán vào Sổ tay ôn tập!", "Ghim Câu Khó", MessageBoxButton.OK, MessageBoxImage.Information);
            });
            flagBtn.Margin = new Thickness(0, 0, 8, 0);
            flagBtn.Padding = new Thickness(4, 2, 4, 2);
            rightHeaderSp.Children.Add(flagBtn);

            var ptsTb = new TextBlock
            {
                Text = $"{p.Points} điểm", FontSize = DS.FontTag,
                FontFamily = DS.FontPrimary,
                Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                VerticalAlignment = VerticalAlignment.Center
            };
            rightHeaderSp.Children.Add(ptsTb);

            headerDock.Children.Add(rightHeaderSp);
            sp.Children.Add(headerDock);

            // Question
            var questionElement = UI.RenderMixedContent(p.Question, DS.FontResult);
            if (questionElement is FrameworkElement fe)
            {
                fe.Margin = new Thickness(0, 6, 0, 0);
            }
            sp.Children.Add(questionElement);

            // Options (MCQ)
            if (p.Options?.Count > 0)
            {
                for (int i = 0; i < p.Options.Count; i++)
                {
                    var letter = ((char)('A' + i)).ToString();
                    var isCorrect = letter == p.GetAnswer();
                    var optColor = DS.ResultInfo;
                    var optBorder = UI.ResultRow("  " + FormatOptionText(p.Options[i], letter), optColor);
                    optBorder.Margin = new Thickness(0, 2, 0, 0);

                    // Click to check
                    var capturedLetter = letter;
                    var capturedCorrect = isCorrect;
                    var capturedAnswer = p.GetAnswer();
                    optBorder.MouseLeftButtonDown += (s, e) =>
                    {
                        e.Handled = true;
                        var b = (Border)s;
                        if (capturedCorrect)
                        {
                            b.Background = DS.LightBg(DS.ResultSuccess);
                            SetResultRowText(b, $"  ✅ {capturedLetter}. Đúng!", DS.ResultSuccess);
                            if (b.Tag == null) {
                                b.Tag = "answered";
                                onCorrect?.Invoke();
                            }
                        }
                        else
                        {
                            b.Background = DS.LightBg(DS.ResultDanger);
                            SetResultRowText(b, $"  ❌ {capturedLetter}. Sai — Đáp án: {capturedAnswer}", DS.ResultDanger);
                        }
                        
                        // F3. Flash animation feedback
                        var flash = new System.Windows.Media.Animation.DoubleAnimation { From = 0.3, To = 1.0, Duration = TimeSpan.FromMilliseconds(300) };
                        b.BeginAnimation(UIElement.OpacityProperty, flash);
                    };
                    sp.Children.Add(optBorder);
                }
            }

            // Solution toggle
            var solPanel = new StackPanel { Visibility = Visibility.Collapsed };
            if (!string.IsNullOrEmpty(p.Solution))
            {
                UI.ResultRow($"📝 {p.Solution}", DS.ResultPrimary, solPanel);
            }
            if (!string.IsNullOrEmpty(p.GetAnswer()))
            {
                UI.ResultRow($"✅ Đáp án: {p.GetAnswer()}", DS.ResultSuccess, solPanel);
            }
            sp.Children.Add(solPanel);

            var actionsPanel = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };

            if (!string.IsNullOrEmpty(p.GraphExpression))
            {
                var graphBtn = UI.PresetButton("📈 Mở Đồ Thị", DS.ResultSpecial, () => {
                    MessageBox.Show($"[Mô phỏng] Đã gửi lệnh mở công cụ đồ thị với biểu thức:\n\n{p.GraphExpression}", "Mở Đồ Thị Tương Tác", MessageBoxButton.OK, MessageBoxImage.Information);
                });
                graphBtn.Margin = new Thickness(0, 0, 8, 0);
                actionsPanel.Children.Add(graphBtn);
            }

            if (p.Hints != null && p.Hints.Count > 0)
            {
                var hintsPanel = new StackPanel { Visibility = Visibility.Collapsed, Margin = new Thickness(0, 8, 0, 8) };
                for (int i = 0; i < p.Hints.Count; i++)
                {
                    var row = UI.ResultRow($"💡 {p.Hints[i]}", DS.ResultWarning, hintsPanel);
                    row.Margin = new Thickness(0, 2, 0, 0);
                }
                sp.Children.Add(hintsPanel);

                var hintBtn = UI.PresetButton("💡 Xem Gợi Ý", DS.ResultWarning, () => {
                    hintsPanel.Visibility = hintsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                });
                hintBtn.Margin = new Thickness(0, 0, 8, 0);
                actionsPanel.Children.Add(hintBtn);
            }

            var showSolBtn = UI.PresetButton("📖 Lời Giải", DS.CatMath, () =>
            {
                solPanel.Visibility = solPanel.Visibility == Visibility.Visible
                    ? Visibility.Collapsed : Visibility.Visible;
            });
            actionsPanel.Children.Add(showSolBtn);
            
            sp.Children.Add(actionsPanel);

            return new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(DS.RadiusChip),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = DS.BrushAlpha(diffColor, 60),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Child = sp
            };
        }

        private UIElement BuildMistakes(List<CommonMistake> mistakes)
        {
            var sp = new StackPanel();
            sp.Children.Add(CreateSectionHeader("⚠️ Lỗi Thường Gặp", "#E65100"));
            foreach (var m in mistakes)
            {
                UI.ResultRow($"❌ {m.Mistake}", DS.ResultDanger, sp);
                UI.ResultRow($"✅ {m.Correction}", DS.ResultSuccess, sp);
            }
            return UI.SectionCard(sp, Color.FromRgb(255, 224, 178));
        }

        private void ShowError(string msg)
        {
            mainPanel.Children.Clear();
            mainPanel.Children.Add(new TextBlock
            {
                Text = $"❌ {msg}", FontSize = DS.FontLabel,
                FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.ResultDanger),
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(20)
            });
        }

        // ─── Active Exam State ───
        private MockExamData? _activeExam;
        private int _activeCorrect;
        private int _activeAnswered;

        private void BackToChapters_Click(object sender, RoutedEventArgs e)
        {
            // If exam is in progress, confirm before navigating away
            if (_examTimer != null && !_examTimerExpired)
            {
                var remaining = FormatTime(_examSecondsLeft);
                var result = MessageBox.Show(
                    $"⚠️ Bạn đang làm bài thi!\n\n" +
                    $"Còn {remaining} — {_activeAnswered}/{_activeExam?.Questions.Count ?? 0} câu đã trả lời.\n\n" +
                    $"Bạn có chắc muốn thoát?",
                    "Xác nhận thoát",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                // Broadcast partial result before leaving
                if (_activeExam != null)
                    BroadcastExamResult(_activeExam, _activeCorrect, _activeAnswered);
            }

            StopExamTimer();

            // Context-aware navigation
            if (_activeTab == "train")
            {
                if (_selectedGrade.HasValue)
                {
                    // Was viewing a grade station → go back to train journey
                    _selectedGrade = null;
                    txtHeaderTitle.Text = "🚂 Chuyến Tàu Toán Học";
                    txtHeaderSub.Text = "Hành trình Lớp 1 → Lớp 12 • Khám phá toàn bộ chương trình Toán";
                    ShowTrainJourney();
                }
                else
                {
                    ShowTrainJourney();
                }
            }
            else if (_activeTab == "dashboard")
            {
                if (_currentDashboardView == "details")
                {
                    ShowPerformanceTracking();
                }
                else if (_currentDashboardView == "performance" || _currentDashboardView == "builder")
                {
                    ShowDashboard();
                }
                else
                {
                    ShowDashboard();
                }
            }
            else
            {
                ShowChapterGrid();
            }
        }

        private void StopExamTimer()
        {
            if (_examTimer != null)
            {
                _examTimer.Stop();
                _examTimer = null;
            }
            _timerText = null;
            _timerBorder = null;
            _examTimerExpired = false;
            _activeExam = null;
            _activeCorrect = 0;
            _activeAnswered = 0;
        }

        // ═══════════════════════════════════════════════════════════
        //  MOCK EXAM UI
        // ═══════════════════════════════════════════════════════════

        private Border CreateExamCard(MockExamData exam, int idx, Color color)
        {
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(DS.RadiusCard),
                Width = 270, Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 12, 12),
                BorderBrush = DS.BrushAlpha(color, 40),
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 6, ShadowDepth = 2, Opacity = 0.06,
                    Color = Colors.Black, Direction = 270
                }
            };

            var sp = new StackPanel { Margin = new Thickness(14, 10, 14, 10) };

            // Title row
            var titleDock = new DockPanel();
            var badge = new Border
            {
                Background = DS.Brush(color),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 8, 0)
            };
            badge.Child = new TextBlock
            {
                Text = $"Đề {idx:D2}", FontSize = DS.FontTag,
                FontWeight = FontWeights.Bold, FontFamily = DS.FontPrimary,
                Foreground = Brushes.White
            };
            titleDock.Children.Add(badge);
            titleDock.Children.Add(new TextBlock
            {
                Text = exam.Metadata.Title,
                FontSize = 13, FontWeight = FontWeights.SemiBold,
                FontFamily = DS.FontPrimary,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            sp.Children.Add(titleDock);

            sp.Children.Add(new TextBlock
            {
                Text = $"{exam.Metadata.TotalQuestions} câu • {exam.Metadata.TimeLimit} phút • {exam.Metadata.TotalPoints} điểm",
                FontSize = 12, FontFamily = DS.FontPrimary,
                Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                Margin = new Thickness(0, 4, 0, 0)
            });

            card.Child = sp;
            var capturedExam = exam;
            card.MouseLeftButtonDown += (_, _) => ShowExamDetail(capturedExam);
            card.MouseEnter += (_, _) => { card.BorderBrush = DS.Brush(color); card.BorderThickness = new Thickness(2); };
            card.MouseLeave += (_, _) => { card.BorderBrush = DS.BrushAlpha(color, 40); card.BorderThickness = new Thickness(1); };
            return card;
        }

        private void ShowExamDetail(MockExamData exam)
        {
            StopExamTimer();
            _activeExam = exam;
            mainPanel.Children.Clear();
            btnBackToChapters.Visibility = Visibility.Visible;
            AnimateMainPanel();
            txtHeaderTitle.Text = $"📝 {exam.Metadata.Title}";
            txtHeaderSub.Text = $"{exam.Questions.Count} câu • {exam.Metadata.TimeLimit} phút • 0.2đ/câu";

            // ═══ COUNTDOWN TIMER BAR ═══
            var examColor = Color.FromRgb(198, 40, 40);
            _examSecondsLeft = exam.Metadata.TimeLimit * 60;
            _examTimerExpired = false;

            _timerBorder = new Border
            {
                Background = new LinearGradientBrush(
                    Color.FromRgb(232, 245, 233), Color.FromRgb(200, 230, 201),
                    new Point(0, 0), new Point(1, 0)),
                CornerRadius = new CornerRadius(DS.RadiusChip),
                Padding = new Thickness(16, 10, 16, 10),
                Margin = new Thickness(0, 0, 0, 12),
                BorderBrush = DS.BrushAlpha(Color.FromRgb(46, 125, 50), 60),
                BorderThickness = new Thickness(0, 0, 0, 2)
            };

            var timerDock = new DockPanel();

            // Timer icon + text
            var timerIcon = new TextBlock
            {
                Text = "⏱️", FontSize = 20,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            timerDock.Children.Add(timerIcon);

            _timerText = new TextBlock
            {
                Text = FormatTime(_examSecondsLeft),
                FontSize = 18, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(Color.FromRgb(46, 125, 50)),
                VerticalAlignment = VerticalAlignment.Center
            };
            timerDock.Children.Add(_timerText);

            // Timer label (right side)
            var timerLabel = new TextBlock
            {
                Text = $"Thời gian: {exam.Metadata.TimeLimit} phút",
                FontSize = DS.FontTag, FontFamily = DS.FontPrimary,
                Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(timerLabel, Dock.Right);
            timerDock.Children.Insert(0, timerLabel);

            _timerBorder.Child = timerDock;
            mainPanel.Children.Add(_timerBorder);

            // ═══ SCORE TRACKER ═══
            int correct = 0, answered = 0;
            var totalQ = exam.Questions.Count;
            var scoreText = new TextBlock
            {
                Text = $"📊 0/{totalQ} câu — Chưa trả lời",
                FontSize = DS.FontLabel, FontWeight = FontWeights.SemiBold,
                FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.CatMath),
                Margin = new Thickness(0, 0, 0, 12)
            };
            mainPanel.Children.Add(scoreText);

            // ═══ QUESTIONS ═══
            // Track all question containers for auto-lock on timeout
            var questionContainers = new List<Border>();

            // ═══ TOPIC ANALYTICS ═══
            _topicStats = new Dictionary<string, (int correct, int total)>();
            foreach (var q in exam.Questions)
            {
                var topic = q.Topic ?? "unknown";
                if (!_topicStats.ContainsKey(topic))
                    _topicStats[topic] = (0, 0);
            }

            void UpdateScore()
            {
                _activeCorrect = correct;
                _activeAnswered = answered;
                var score = correct * exam.Metadata.PointsPerQuestion;
                scoreText.Text = $"📊 {answered}/{totalQ} câu" +
                    $" • ✅ {correct} đúng • 🎯 {score:F1}/{exam.Metadata.TotalPoints}đ";

                // Check if all questions answered → show summary + stop timer
                if (answered >= totalQ)
                {
                    StopExamTimer();
                    ShowExamSummary(exam, correct, answered, _examSecondsLeft);
                    BroadcastExamResult(exam, correct, answered);
                }
            }

            foreach (var q in exam.Questions)
            {
                var capturedTopic = q.Topic ?? "unknown";
                var qBorder = BuildExamQuestion(q, () =>
                {
                    answered++;
                    // Track topic total
                    var cur = _topicStats!.GetValueOrDefault(capturedTopic, (0, 0));
                    _topicStats![capturedTopic] = (cur.Item1, cur.Item2 + 1);
                    UpdateScore();

                    // WebSocket: broadcast each answer in realtime
                    BroadcastQuizAnswer(exam.Metadata.ExamId, q.Id, q.GetAnswer());
                }, (isRight) =>
                {
                    if (isRight)
                    {
                        correct++;
                        // Track topic correct
                        var cur = _topicStats!.GetValueOrDefault(capturedTopic, (0, 0));
                        _topicStats![capturedTopic] = (cur.Item1 + 1, cur.Item2);
                    }
                    UpdateScore();
                });
                questionContainers.Add(qBorder);
                mainPanel.Children.Add(qBorder);
            }

            // ═══ START TIMER ═══
            _examTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _examTimer.Tick += (_, _) =>
            {
                _examSecondsLeft--;

                if (_timerText != null)
                    _timerText.Text = FormatTime(_examSecondsLeft);

                // Color transitions: green → orange → red
                UpdateTimerAppearance(_examSecondsLeft, exam.Metadata.TimeLimit * 60);

                if (_examSecondsLeft <= 0)
                {
                    _examTimerExpired = true;
                    StopExamTimer();

                    // Auto-lock all unanswered questions
                    if (_timerText != null)
                        _timerText.Text = "⌛ HẾT GIỜ!";
                    if (_timerBorder != null)
                        _timerBorder.Background = DS.LightBg(DS.ResultDanger);

                    // Show final summary
                    ShowExamSummary(exam, correct, answered, 0);
                    BroadcastExamResult(exam, correct, answered);
                }
            };
            _examTimer.Start();
        }

        // ═══════════════════════════════════════════════════════════
        //  TIMER HELPERS
        // ═══════════════════════════════════════════════════════════

        private static string FormatTime(int totalSeconds)
        {
            var m = totalSeconds / 60;
            var s = totalSeconds % 60;
            return $"{m:D2}:{s:D2}";
        }

        private static string FormatOptionText(string optText, string letter)
        {
            if (string.IsNullOrEmpty(optText)) return string.Empty;
            string cleaned = optText.Trim();
            if (cleaned.StartsWith(letter + ")") || cleaned.StartsWith(letter + ".") || cleaned.StartsWith(letter + " "))
                return cleaned;
            return $"{letter}. {optText}";
        }

        private void UpdateTimerAppearance(int secondsLeft, int totalSeconds)
        {
            if (_timerText == null || _timerBorder == null) return;

            double ratio = totalSeconds > 0 ? (double)secondsLeft / totalSeconds : 0.0;

            if (ratio > 0.5)
            {
                // Green zone (>50%)
                _timerText.Foreground = DS.Brush(Color.FromRgb(46, 125, 50));
                _timerBorder.Background = new LinearGradientBrush(
                    Color.FromRgb(232, 245, 233), Color.FromRgb(200, 230, 201),
                    new Point(0, 0), new Point(1, 0));
                _timerBorder.BorderBrush = DS.BrushAlpha(Color.FromRgb(46, 125, 50), 60);
            }
            else if (ratio > 0.2)
            {
                // Orange zone (20-50%)
                _timerText.Foreground = DS.Brush(Color.FromRgb(230, 81, 0));
                _timerBorder.Background = new LinearGradientBrush(
                    Color.FromRgb(255, 243, 224), Color.FromRgb(255, 224, 178),
                    new Point(0, 0), new Point(1, 0));
                _timerBorder.BorderBrush = DS.BrushAlpha(Color.FromRgb(230, 81, 0), 60);
            }
            else
            {
                // Red zone (<20%) — pulse effect with bold
                _timerText.Foreground = DS.Brush(DS.ResultDanger);
                _timerText.FontSize = secondsLeft % 2 == 0 ? 20 : 18; // Pulse
                _timerBorder.Background = new LinearGradientBrush(
                    Color.FromRgb(255, 235, 238), Color.FromRgb(255, 205, 210),
                    new Point(0, 0), new Point(1, 0));
                _timerBorder.BorderBrush = DS.Brush(DS.ResultDanger);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  EXAM SUMMARY
        // ═══════════════════════════════════════════════════════════

        private void ShowExamSummary(MockExamData exam, int correct, int answered, int secondsLeft)
        {
            var totalQ = exam.Questions.Count;
            var score = correct * exam.Metadata.PointsPerQuestion;
            var pct = totalQ > 0 ? (double)correct / totalQ * 100 : 0;
            var timeUsed = exam.Metadata.TimeLimit * 60 - secondsLeft;

            // Grade classification
            var (grade, gradeColor, gradeEmoji) = pct switch
            {
                >= 90 => ("Xuất sắc", Color.FromRgb(46, 125, 50), "🏆"),
                >= 70 => ("Khá", Color.FromRgb(21, 101, 192), "👍"),
                >= 50 => ("Trung bình", Color.FromRgb(230, 81, 0), "📝"),
                _ => ("Cần cố gắng", Color.FromRgb(198, 40, 40), "💪")
            };

            var summaryCard = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(DS.RadiusCard),
                Padding = new Thickness(20, 16, 20, 16),
                Margin = new Thickness(0, 16, 0, 16),
                BorderBrush = DS.BrushAlpha(gradeColor, 80),
                BorderThickness = new Thickness(0, 3, 0, 0),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 12, ShadowDepth = 3, Opacity = 0.1,
                    Color = Colors.Black, Direction = 270
                }
            };

            var sp = new StackPanel();

            // Grade header
            sp.Children.Add(new TextBlock
            {
                Text = $"{gradeEmoji} KẾT QUẢ: {grade}",
                FontSize = 18, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(gradeColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            });

            // Score display
            sp.Children.Add(new TextBlock
            {
                Text = $"🎯 {score:F1} / {exam.Metadata.TotalPoints} điểm ({pct:F0}%)",
                FontSize = 16, FontWeight = FontWeights.SemiBold,
                FontFamily = DS.FontPrimary,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Stats row
            var statsWrap = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };
            void AddStat(string text, Color color)
            {
                var statBadge = new Border
                {
                    Background = DS.LightBg(color),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 4, 10, 4),
                    Margin = new Thickness(4, 2, 4, 2)
                };
                statBadge.Child = new TextBlock
                {
                    Text = text, FontSize = 13,
                    FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(color)
                };
                statsWrap.Children.Add(statBadge);
            }
            AddStat($"✅ Đúng: {correct}/{totalQ}", DS.ResultSuccess);
            AddStat($"❌ Sai: {answered - correct}/{totalQ}", DS.ResultDanger);
            AddStat($"⏭️ Bỏ qua: {totalQ - answered}", DS.ResultInfo);
            AddStat($"⏱️ Thời gian: {FormatTime(timeUsed)}", DS.CatMath);
            sp.Children.Add(statsWrap);

            summaryCard.Child = sp;

            // Insert at top (after timer bar)
            if (mainPanel.Children.Count > 0)
                mainPanel.Children.Insert(0, summaryCard);
            else
                mainPanel.Children.Add(summaryCard);

            // ═══ TOPIC ANALYTICS ═══
            if (_topicStats?.Count > 0)
            {
                var analyticsCard = BuildTopicAnalytics(exam);
                if (analyticsCard != null)
                {
                    var insertIdx = mainPanel.Children.IndexOf(summaryCard) + 1;
                    if (insertIdx < mainPanel.Children.Count)
                        mainPanel.Children.Insert(insertIdx, analyticsCard);
                    else
                        mainPanel.Children.Add(analyticsCard);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TOPIC ANALYTICS
        // ═══════════════════════════════════════════════════════════

        /// <summary>Vietnamese display names for exam topic IDs</summary>
        private static readonly Dictionary<string, string> TopicDisplayNames = new()
        {
            ["dao_ham"] = "Đạo Hàm",
            ["tich_phan"] = "Tích Phân",
            ["mu_log"] = "Mũ & Logarit",
            ["so_phuc"] = "Số Phức",
            ["hinh_hoc_kg"] = "Hình Học Không Gian",
            ["hinh_hoc"] = "Hình Học Phẳng",
            ["to_hop_xs"] = "Tổ Hợp & Xác Suất",
            ["to_hop"] = "Tổ Hợp",
            ["xs"] = "Xác Suất",
            ["luong_giac"] = "Lượng Giác",
            ["ham_so"] = "Hàm Số & Đồ Thị",
            ["day_so"] = "Dãy Số & Cấp Số",
            ["gioi_han"] = "Giới Hạn",
            ["menh_de"] = "Mệnh Đề & Tập Hợp",
            ["tap_hop"] = "Tập Hợp",
            ["phuong_trinh"] = "Phương Trình"
        };

        /// <summary>Map exam topic ID → suggested tool IDs from ToolRegistry</summary>
        private static readonly Dictionary<string, string[]> TopicToToolIds = new()
        {
            ["dao_ham"] = new[] { "derivative" },
            ["tich_phan"] = new[] { "integral" },
            ["mu_log"] = new[] { "logarithm" },
            ["so_phuc"] = new[] { "complex_number" },
            ["hinh_hoc_kg"] = new[] { "solid_geometry", "vector" },
            ["hinh_hoc"] = new[] { "geometry", "coordinate" },
            ["to_hop_xs"] = new[] { "combinatorics", "probability" },
            ["to_hop"] = new[] { "combinatorics" },
            ["xs"] = new[] { "probability" },
            ["luong_giac"] = new[] { "trigonometry", "trig_equation", "identities" },
            ["ham_so"] = new[] { "derivative", "quadratic", "cubic" },
            ["day_so"] = new[] { "sequence" },
            ["gioi_han"] = new[] { "limit" },
            ["menh_de"] = Array.Empty<string>(),
            ["tap_hop"] = Array.Empty<string>(),
            ["phuong_trinh"] = new[] { "quadratic", "linear_system" }
        };

        /// <summary>Build topic-by-topic analytics card with progress bars and tool recommendations</summary>
        private Border? BuildTopicAnalytics(MockExamData exam)
        {
            if (_topicStats == null || _topicStats.Count == 0) return null;

            var sp = new StackPanel();

            // Header
            sp.Children.Add(new TextBlock
            {
                Text = "📊 PHÂN TÍCH THEO CHỦ ĐỀ",
                FontSize = 15, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.BrandPrimary),
                Margin = new Thickness(0, 0, 0, 10)
            });

            // Sort topics: weakest first
            var sorted = _topicStats
                .Where(kv => kv.Value.total > 0)
                .OrderBy(kv => kv.Value.total > 0 ? (double)kv.Value.correct / kv.Value.total : 1.0)
                .ToList();

            // Collect weak topics for recommendation section
            var weakTopics = new List<string>();

            foreach (var (topicId, stats) in sorted)
            {
                var pct = stats.total > 0 ? (double)stats.correct / stats.total * 100 : 0;
                var displayName = TopicDisplayNames.GetValueOrDefault(topicId, topicId);

                // Color based on performance
                var (barColor, emoji) = pct switch
                {
                    >= 80 => (DS.ResultSuccess, "✅"),
                    >= 50 => (DS.ResultWarning, "⚠️"),
                    _ => (DS.ResultDanger, "❌")
                };

                if (pct < 70) weakTopics.Add(topicId);

                // Topic row container
                var rowSp = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };

                // Label row: emoji + name + fraction
                var labelRow = new DockPanel();
                labelRow.Children.Add(new TextBlock
                {
                    Text = $"{emoji} {displayName}",
                    FontSize = 13, FontWeight = FontWeights.SemiBold,
                    FontFamily = DS.FontPrimary,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
                });
                var fractionText = new TextBlock
                {
                    Text = $"{stats.correct}/{stats.total} ({pct:F0}%)",

                    FontSize = 12, FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(barColor),
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                DockPanel.SetDock(fractionText, Dock.Right);
                labelRow.Children.Insert(0, fractionText); // Insert right-docked first
                rowSp.Children.Add(labelRow);

                // Progress bar
                var barBg = new Border
                {
                    Background = DS.LightBg(barColor),
                    CornerRadius = new CornerRadius(3),
                    Height = 8,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                var barFill = new Border
                {
                    Background = DS.Brush(barColor),
                    CornerRadius = new CornerRadius(3),
                    Height = 8,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 0 // Set after layout
                };
                barBg.Child = barFill;

                // Set bar width on loaded (percentage of parent width)
                barBg.Loaded += (s, _) =>
                {
                    var parent = (Border)s!;
                    barFill.Width = parent.ActualWidth * (pct / 100.0);
                };
                barBg.SizeChanged += (s, _) =>
                {
                    var parent = (Border)s!;
                    barFill.Width = parent.ActualWidth * (pct / 100.0);
                };

                rowSp.Children.Add(barBg);
                sp.Children.Add(rowSp);
            }

            // ═══ TOOL RECOMMENDATIONS for weak topics ═══
            if (weakTopics.Count > 0)
            {
                sp.Children.Add(new Border
                {
                    Background = DS.BrushAlpha(DS.ResultInfo, 30),
                    Height = 1,
                    Margin = new Thickness(0, 10, 0, 10)
                });

                sp.Children.Add(new TextBlock
                {
                    Text = "🔧 GỢI Ý LUYỆN TẬP",
                    FontSize = 13, FontWeight = FontWeights.Bold,
                    FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(DS.BrandAccent),
                    Margin = new Thickness(0, 0, 0, 6)
                });

                sp.Children.Add(new TextBlock
                {
                    Text = "Các chủ đề dưới 70% — hãy luyện thêm với công cụ sau:",
                    FontSize = 12, FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(DS.ResultInfo),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8)
                });

                var toolWrap = new WrapPanel();
                var addedToolIds = new HashSet<string>(); // Avoid duplicate buttons

                foreach (var topicId in weakTopics)
                {
                    var toolIds = TopicToToolIds.GetValueOrDefault(topicId, Array.Empty<string>());
                    foreach (var toolId in toolIds)
                    {
                        if (!addedToolIds.Add(toolId)) continue; // Skip duplicates

                        var toolDef = ToolRegistry.GetById(toolId);
                        if (toolDef == null) continue;

                        var topicName = TopicDisplayNames.GetValueOrDefault(topicId, topicId);
                        var stats = _topicStats.GetValueOrDefault(topicId, (0, 0));
                        var topicPct = stats.Item2 > 0 ? (double)stats.Item1 / stats.Item2 * 100 : 0;

                        var btn = UI.PresetButton(
                            $"{toolDef.Icon} {toolDef.Name}",
                            DS.BrandAccent,
                            () =>
                            {
                                var control = LearningToolsHub.CreateToolControl(toolDef.Id);
                                if (control != null)
                                {
                                    mainPanel.Children.Clear();
                                    mainPanel.Children.Add(control);
                                }
                            }, toolWrap);
                        btn.ToolTip = $"Luyện {topicName} ({topicPct:F0}% đúng)";
                    }
                }

                // Also add chapter-based recommendations from mapping
                if (_topicMapping?.ExamMapping != null)
                {
                    var examId = exam.Metadata.ExamId;
                    if (_topicMapping.ExamMapping.TryGetValue(examId, out var examMap))
                    {
                        foreach (var mappedTool in examMap.Tools)
                        {
                            // Find matching ToolRegistry entry
                            var toolDef = ToolRegistry.AllTools
                                .FirstOrDefault(t => mappedTool.Contains(t.Id, StringComparison.OrdinalIgnoreCase)
                                    || t.Name.Contains(mappedTool.Replace("Tool", ""), StringComparison.OrdinalIgnoreCase));

                            if (toolDef != null && addedToolIds.Add(toolDef.Id))
                            {
                                var btn = UI.PresetButton(
                                    $"📘 {toolDef.Name}",
                                    DS.CatMath,
                                    () =>
                                    {
                                        var control = LearningToolsHub.CreateToolControl(toolDef.Id);
                                        if (control != null)
                                        {
                                            mainPanel.Children.Clear();
                                            mainPanel.Children.Add(control);
                                        }
                                    }, toolWrap);
                                btn.ToolTip = $"Ôn tập — {toolDef.Description}";
                            }
                        }
                    }
                }

                sp.Children.Add(toolWrap);
            }
            else
            {
                // All topics ≥ 70% — show encouragement
                sp.Children.Add(new Border
                {
                    Background = DS.LightBg(DS.ResultSuccess),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 10, 0, 0),
                    Child = new TextBlock
                    {
                        Text = "🎉 Tuyệt vời! Bạn đạt ≥70% ở tất cả chủ đề. Tiếp tục luyện đề để giữ phong độ!",
                        FontSize = 13, FontFamily = DS.FontPrimary,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = DS.Brush(DS.ResultSuccess)
                    }
                });
            }

            return new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(DS.RadiusCard),
                Padding = new Thickness(18, 14, 18, 14),
                Margin = new Thickness(0, 0, 0, 16),
                BorderBrush = DS.BrushAlpha(DS.BrandPrimary, 60),
                BorderThickness = new Thickness(0, 2, 0, 0),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 10, ShadowDepth = 2, Opacity = 0.08,
                    Color = Colors.Black, Direction = 270
                },
                Child = sp
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  WEBSOCKET BROADCAST
        // ═══════════════════════════════════════════════════════════

        /// <summary>Broadcast individual quiz answer to teacher dashboard</summary>
        private void BroadcastQuizAnswer(string examId, int questionId, string answer)
        {
            try
            {
                var app = Application.Current as QASmartTouch.App;
                if (app == null) return;
                var bridge = app.NetworkService?.WebBridge;
                if (bridge?.IsRunning != true) return;

                var payload = JsonSerializer.Serialize(new
                {
                    type = "quiz_answer",
                    examId,
                    questionId,
                    answer,
                    timestamp = DateTime.Now.ToString("HH:mm:ss")
                });
                _ = bridge.BroadcastJsonToWebClients(payload);
            }
            catch (Exception ex)
            {
                Log.Debug("BroadcastQuizAnswer error: {Err}", ex.Message);
            }
        }

        /// <summary>Broadcast final exam result to teacher dashboard</summary>
        private void BroadcastExamResult(MockExamData exam, int correct, int answered)
        {
            try
            {
                var app = Application.Current as QASmartTouch.App;
                if (app == null) return;
                var bridge = app.NetworkService?.WebBridge;
                if (bridge?.IsRunning != true) return;

                var score = correct * exam.Metadata.PointsPerQuestion;

                // Build topic analytics for broadcast
                object? topicBreakdown = null;
                if (_topicStats?.Count > 0)
                {
                    topicBreakdown = _topicStats
                        .Where(kv => kv.Value.total > 0)
                        .Select(kv => new
                        {
                            topic = kv.Key,
                            name = TopicDisplayNames.GetValueOrDefault(kv.Key, kv.Key),
                            correct = kv.Value.correct,
                            total = kv.Value.total,
                            pct = System.Math.Round((double)kv.Value.correct / kv.Value.total * 100, 1)
                        })
                        .OrderBy(t => t.pct)
                        .ToList();
                }

                var payload = JsonSerializer.Serialize(new
                {
                    type = "exam_result",
                    examId = exam.Metadata.ExamId,
                    examTitle = exam.Metadata.Title,
                    totalQuestions = exam.Questions.Count,
                    answered,
                    correct,
                    score,
                    totalPoints = exam.Metadata.TotalPoints,
                    percentage = exam.Questions.Count > 0
                        ? (double)correct / exam.Questions.Count * 100 : 0,
                    topicAnalytics = topicBreakdown,
                    timestamp = DateTime.Now.ToString("HH:mm:ss")
                });
                _ = bridge.BroadcastJsonToWebClients(payload);
                Log.Information("Exam result broadcast: {ExamId}, score={Score}/{Total}, topics={TopicCount}",
                    exam.Metadata.ExamId, score, exam.Metadata.TotalPoints,
                    _topicStats?.Count ?? 0);
            }
            catch (Exception ex)
            {
                Log.Debug("BroadcastExamResult error: {Err}", ex.Message);
            }
        }

        private Border BuildExamQuestion(ExamQuestion q, Action onAnswer, Action<bool> onResult)
        {
            var sp = new StackPanel();
            var diff = q.GetDifficulty();
            var diffColor = diff switch
            {
                "Easy" => Color.FromRgb(46, 125, 50),
                "Hard" => Color.FromRgb(198, 40, 40),
                _ => Color.FromRgb(230, 81, 0)
            };

            // Header
            var header = new DockPanel();
            var numBadge = new Border
            {
                Background = DS.LightBg(DS.CatMath),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2)
            };
            numBadge.Child = new TextBlock
            {
                Text = $"Câu {q.Id}", FontSize = DS.FontTag,
                FontWeight = FontWeights.Bold, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.CatMath)
            };
            header.Children.Add(numBadge);

            var diffBadge = new Border
            {
                Background = DS.LightBg(diffColor),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(6, 0, 0, 0)
            };
            diffBadge.Child = new TextBlock
            {
                Text = diff, FontSize = DS.FontTag,
                FontWeight = FontWeights.SemiBold, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(diffColor)
            };
            header.Children.Add(diffBadge);
            sp.Children.Add(header);

            // Question text
            var questionElement = UI.RenderMixedContent(q.GetQuestion(), DS.FontResult);
            if (questionElement is FrameworkElement fe)
            {
                fe.Margin = new Thickness(0, 6, 0, 4);
            }
            sp.Children.Add(questionElement);

            // Options with click-to-answer
            bool alreadyAnswered = false;
            var options = q.GetOptions();
            var answer = q.GetAnswer();
            for (int i = 0; i < options.Count; i++)
            {
                var optText = options[i];
                var letter = ((char)('A' + i)).ToString();
                var displayText = FormatOptionText(optText, letter);
                var isCorrect = letter == answer;
                var optBorder = UI.ResultRow("  " + displayText, DS.ResultInfo);
                optBorder.Margin = new Thickness(0, 2, 0, 0);
                optBorder.Cursor = Cursors.Hand;

                var capLetter = letter;
                var capCorrect = isCorrect;
                optBorder.MouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    if (alreadyAnswered || _examTimerExpired) return;
                    alreadyAnswered = true;
                    onAnswer();

                    var b = (Border)s;
                    if (capCorrect)
                    {
                        b.Background = DS.LightBg(DS.ResultSuccess);
                        SetResultRowText(b, $"  ✅ {capLetter}. Đúng!", DS.ResultSuccess);
                    }
                    else
                    {
                        b.Background = DS.LightBg(DS.ResultDanger);
                        SetResultRowText(b, $"  ❌ {capLetter}. Sai — Đáp án: {answer}", DS.ResultDanger);
                    }
                    
                    // F3. Flash animation feedback
                    var flash = new System.Windows.Media.Animation.DoubleAnimation { From = 0.3, To = 1.0, Duration = TimeSpan.FromMilliseconds(300) };
                    b.BeginAnimation(UIElement.OpacityProperty, flash);
                    
                    onResult(capCorrect);
                };
                sp.Children.Add(optBorder);
            }

            // Solution toggle
            if (!string.IsNullOrEmpty(q.Solution))
            {
                var solPanel = new StackPanel { Visibility = Visibility.Collapsed };
                UI.ResultRow($"📝 {q.Solution}", DS.ResultPrimary, solPanel);
                sp.Children.Add(solPanel);
                var solBtn = UI.PresetButton("💡 Lời giải", DS.CatMath, () =>
                {
                    solPanel.Visibility = solPanel.Visibility == Visibility.Visible
                        ? Visibility.Collapsed : Visibility.Visible;
                });
                solBtn.Margin = new Thickness(0, 4, 0, 0);
                sp.Children.Add(solBtn);
            }

            return new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(DS.RadiusChip),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = DS.BrushAlpha(diffColor, 40),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Child = sp
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  QUIZ PREVIEW — replaces MessageBox placeholder
        // ═══════════════════════════════════════════════════════════

        private void ShowQuizPreview(MathChapterData ch)
        {
            // Collect multi-choice problems from this chapter
            var problems = ch.Sections
                .SelectMany(s => s.Problems)
                .Where(p => p.Options?.Count >= 2 && !string.IsNullOrEmpty(p.GetAnswer()))
                .Take(5)
                .ToList();

            var popup = new Window
            {
                Title = $"🚀 Quiz Preview — {ch.Metadata.ChapterName}",
                Width = 600, Height = 520,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                ResizeMode = ResizeMode.NoResize
            };
            try { popup.Owner = Window.GetWindow(this); } catch { }

            var sp = new StackPanel { Margin = new Thickness(20) };

            // Header
            sp.Children.Add(new TextBlock
            {
                Text = $"📚 {ch.Metadata.ChapterName}",
                FontSize = 16, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.BrandPrimary),
                Margin = new Thickness(0, 0, 0, 4)
            });
            sp.Children.Add(new TextBlock
            {
                Text = $"Lớp {ch.Metadata.Grade} • {ch.Metadata.TotalProblems} bài tập • {problems.Count} câu trắc nghiệm mẫu",
                FontSize = 12, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.ResultInfo), Margin = new Thickness(0, 0, 0, 12)
            });

            // Sample questions
            var scroll = new ScrollViewer { MaxHeight = 320, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var qPanel = new StackPanel();

            for (int qi = 0; qi < problems.Count; qi++)
            {
                var p = problems[qi];
                var qBorder = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 8),
                    BorderBrush = DS.BrushAlpha(DS.BrandSecondary, 30),
                    BorderThickness = new Thickness(0, 0, 0, 2)
                };
                var qSp = new StackPanel();
                qSp.Children.Add(new TextBlock
                {
                    Text = $"Câu {qi + 1}: {p.Question}",
                    FontSize = 13, FontWeight = FontWeights.SemiBold,
                    FontFamily = DS.FontPrimary, TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 4)
                });

                for (int oi = 0; oi < p.Options.Count; oi++)
                {
                    string letter = ((char)('A' + oi)).ToString();
                    qSp.Children.Add(new TextBlock
                    {
                        Text = "  " + FormatOptionText(p.Options[oi], letter),
                        FontSize = 12, FontFamily = DS.FontPrimary,
                        Foreground = DS.Brush(DS.ResultDark),
                        Margin = new Thickness(8, 1, 0, 1)
                    });
                }
                qBorder.Child = qSp;
                qPanel.Children.Add(qBorder);
            }
            scroll.Content = qPanel;
            sp.Children.Add(scroll);

            // Action buttons
            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };

            var btnLaunch = new Border
            {
                Background = DS.Brush(DS.BrandPrimary),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 8, 16, 8),
                Cursor = Cursors.Hand,
                Margin = new Thickness(8, 0, 0, 0)
            };
            btnLaunch.Child = new TextBlock
            {
                Text = "🚀 Phát Quiz cho HS",
                FontSize = 13, FontWeight = FontWeights.Bold,
                Foreground = Brushes.White, FontFamily = DS.FontPrimary
            };
            string capturedId = ch.Metadata.ChapterId;
            string capturedName = ch.Metadata.ChapterName;
            int capturedGrade = ch.Metadata.Grade;
            btnLaunch.MouseLeftButtonDown += (_, _) =>
            {
                QuizRequested?.Invoke(this, (capturedId, capturedName, capturedGrade));
                popup.Close();
            };

            var btnClose = new Border
            {
                Background = DS.BrushAlpha(DS.ResultInfo, 20),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 8, 16, 8),
                Cursor = Cursors.Hand
            };
            btnClose.Child = new TextBlock
            {
                Text = "✕ Đóng",
                FontSize = 13, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.ResultDark)
            };
            btnClose.MouseLeftButtonDown += (_, _) => popup.Close();

            btnPanel.Children.Add(btnClose);
            btnPanel.Children.Add(btnLaunch);
            sp.Children.Add(btnPanel);

            if (problems.Count == 0)
            {
                sp.Children.Add(new TextBlock
                {
                    Text = "⚠️ Chương này chưa có câu trắc nghiệm (chỉ có bài tự luận)",
                    FontSize = 13, Foreground = DS.Brush(DS.ResultWarning),
                    FontFamily = DS.FontPrimary, Margin = new Thickness(0, 8, 0, 0)
                });
            }

            popup.Content = sp;
            popup.ShowDialog();
        }

        private System.Speech.Synthesis.SpeechSynthesizer? _synth;

        private void SpeakText(string text)
        {
            try
            {
                if (_synth == null)
                {
                    _synth = new System.Speech.Synthesis.SpeechSynthesizer();
                    // Select a Vietnamese voice if available to avoid English pronunciation of VN text
                    var viVoice = _synth.GetInstalledVoices()
                        .FirstOrDefault(v => v.VoiceInfo.Culture.Name.Contains("vi"));
                    if (viVoice != null)
                    {
                        _synth.SelectVoice(viVoice.VoiceInfo.Name);
                    }
                }
                _synth.SpeakAsyncCancelAll();
                _synth.SpeakAsync(text);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "MathCurriculum: Speak failed");
            }
        }

        private void ShowFlashcardMode(MathChapterData ch)
        {
            var concepts = ch.Sections.SelectMany(s => s.Concepts).ToList();
            if (concepts.Count == 0)
            {
                ShowError("Chương này chưa có khái niệm nào để tạo flashcard.");
                return;
            }

            mainPanel.Children.Clear();
            btnBackToChapters.Visibility = Visibility.Visible;
            txtHeaderTitle.Text = $"🃏 Flashcard - {ch.Metadata.ChapterName}";
            txtHeaderSub.Text = $"Còn {concepts.Count} thẻ cần học";

            int currentIndex = 0;
            bool isFlipped = false;

            var card = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                CornerRadius = new CornerRadius(16),
                BorderThickness = new Thickness(2),
                BorderBrush = DS.Brush(DS.ResultPrimary),
                Width = 500, Height = 300,
                Padding = new Thickness(24),
                Margin = new Thickness(0, 32, 0, 16),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 10, Opacity = 0.1 },
                Cursor = Cursors.Hand
            };
            
            var spContent = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var cardTitle = new TextBlock
            {
                FontSize = 24, FontWeight = FontWeights.Bold, FontFamily = DS.FontPrimary,
                TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };
            var cardDesc = new TextBlock
            {
                FontSize = 16, FontFamily = DS.FontPrimary,
                TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center
            };
            var cardFormula = new TextBlock
            {
                FontSize = 16, FontFamily = DS.FontPrimary, FontStyle = FontStyles.Italic,
                TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center,
                Foreground = DS.Brush(DS.ResultDanger), Margin = new Thickness(0, 8, 0, 0)
            };
            
            spContent.Children.Add(cardTitle);
            spContent.Children.Add(cardDesc);
            spContent.Children.Add(cardFormula);
            card.Child = spContent;

            void UpdateCard()
            {
                if (concepts.Count == 0) return;
                var c = concepts[currentIndex];
                txtHeaderSub.Text = $"Còn {concepts.Count} thẻ cần học";
                
                if (!isFlipped)
                {
                    cardTitle.Text = c.Name;
                    cardTitle.Foreground = DS.Brush(DS.ResultDark);
                    cardDesc.Visibility = Visibility.Collapsed;
                    cardFormula.Visibility = Visibility.Collapsed;
                    card.Background = DS.LightBg(DS.ResultPrimary);
                }
                else
                {
                    cardTitle.Text = c.Name;
                    cardTitle.Foreground = DS.Brush(DS.ResultPrimary);
                    
                    cardDesc.Text = c.Definition;
                    cardDesc.Visibility = string.IsNullOrEmpty(c.Definition) ? Visibility.Collapsed : Visibility.Visible;
                    
                    cardFormula.Text = c.Formula;
                    cardFormula.Visibility = string.IsNullOrEmpty(c.Formula) ? Visibility.Collapsed : Visibility.Visible;
                    
                    card.Background = System.Windows.Media.Brushes.White;
                }
            }

            UpdateCard();

            card.MouseLeftButtonDown += (_, _) =>
            {
                isFlipped = !isFlipped;
                UpdateCard();
            };

            var btnPanel = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };
            
            var btnNext = UI.PresetButton("Chưa nhớ 🔄", DS.ResultWarning, () =>
            {
                isFlipped = false;
                currentIndex = (currentIndex + 1) % concepts.Count;
                UpdateCard();
            }, btnPanel);
            btnNext.Margin = new Thickness(0,0,16,0);
            
            var btnGotIt = UI.PresetButton("Biết rồi ✅", DS.ResultSuccess, () =>
            {
                isFlipped = false;
                concepts.RemoveAt(currentIndex);
                if (concepts.Count == 0)
                {
                    mainPanel.Children.Clear();
                    mainPanel.Children.Add(new TextBlock
                    {
                        Text = "🎉 Chúc mừng! Bạn đã hoàn thành tất cả flashcard của chương này.",
                        FontSize = 18, Foreground = DS.Brush(DS.ResultSuccess),
                        FontWeight = FontWeights.Bold, Margin = new Thickness(20),
                        TextWrapping = TextWrapping.Wrap
                    });
                    return;
                }
                currentIndex = currentIndex % concepts.Count;
                UpdateCard();
            }, btnPanel);

            var hintText = new TextBlock
            {
                Text = "(Click vào thẻ để lật mặt sau)",
                FontSize = 13, Foreground = DS.Brush(DS.ResultInfo),
                FontFamily = DS.FontPrimary, FontStyle = FontStyles.Italic,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };

            mainPanel.Children.Add(card);
            mainPanel.Children.Add(hintText);
            mainPanel.Children.Add(btnPanel);
        }

        private void ShowPracticeMode(MathChapterData ch)
        {
            var allProblems = ch.Sections.SelectMany(s => s.Problems).ToList();
            if (allProblems.Count == 0)
            {
                ShowError("Chương này chưa có câu hỏi luyện tập.");
                return;
            }

            mainPanel.Children.Clear();
            btnBackToChapters.Visibility = Visibility.Visible;
            txtHeaderTitle.Text = $"🎮 Cấu Hình Luyện Tập";
            txtHeaderSub.Text = $"Chọn số lượng câu hỏi và độ khó";

            var wrapNum = new WrapPanel { Margin = new Thickness(0, 16, 0, 16) };
            wrapNum.Children.Add(new TextBlock { Text = "Số lượng câu:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,16,0) });
            int selectedCount = 5;
            var btn5 = UI.PresetButton("5 câu", DS.ResultPrimary, () => selectedCount = 5, wrapNum); btn5.Margin = new Thickness(0,0,8,0);
            var btn10 = UI.PresetButton("10 câu", DS.ResultPrimary, () => selectedCount = 10, wrapNum); btn10.Margin = new Thickness(0,0,8,0);
            var btnAll = UI.PresetButton("Tất cả", DS.ResultPrimary, () => selectedCount = allProblems.Count, wrapNum);
            
            var wrapDiff = new WrapPanel { Margin = new Thickness(0, 0, 0, 24) };
            wrapDiff.Children.Add(new TextBlock { Text = "Độ khó:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,16,0) });
            string selectedDiff = "Mix";
            var btnMix = UI.PresetButton("Trộn", DS.ResultDark, () => selectedDiff = "Mix", wrapDiff); btnMix.Margin = new Thickness(0,0,8,0);
            var btnEasy = UI.PresetButton("Dễ", DS.ResultSuccess, () => selectedDiff = "Easy", wrapDiff); btnEasy.Margin = new Thickness(0,0,8,0);
            var btnMedium = UI.PresetButton("Trung bình", DS.ResultWarning, () => selectedDiff = "Medium", wrapDiff); btnMedium.Margin = new Thickness(0,0,8,0);
            var btnHard = UI.PresetButton("Khó", DS.ResultDanger, () => selectedDiff = "Hard", wrapDiff);
            
            mainPanel.Children.Add(wrapNum);
            mainPanel.Children.Add(wrapDiff);

            UI.PresetButton("🚀 Bắt Đầu Luyện Tập", DS.ResultSuccess, () =>
            {
                StartPractice(ch, allProblems, selectedCount, selectedDiff);
            }, mainPanel);
        }

        private void StartPractice(MathChapterData ch, List<Problem> allProblems, int count, string difficulty, string quizType = "Practice")
        {
            var rnd = new Random();
            var pool = difficulty == "Mix" ? allProblems : allProblems.Where(p => p.Difficulty == difficulty).ToList();
            if (pool.Count == 0) pool = allProblems;
            
            var selectedProblems = pool.OrderBy(x => rnd.Next()).Take(count).ToList();
            
            mainPanel.Children.Clear();
            txtHeaderTitle.Text = $"🎮 Luyện Tập - {ch.Metadata.ChapterName}";
            txtHeaderSub.Text = $"Đang làm {selectedProblems.Count} câu";

            int correctAnswers = 0;
            var problemResults = new List<MathProblemResult>();

            foreach (var p in selectedProblems)
            {
                var currentResult = new MathProblemResult 
                { 
                    Question = p.Question, 
                    CorrectAnswer = p.GetAnswer(), 
                    Solution = p.Solution 
                };
                problemResults.Add(currentResult);

                var spCard = new StackPanel { Margin = new Thickness(0,0,0,16) };
                
                var qDock = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
                
                if (ch.Metadata.Grade <= 3)
                {
                    var btnAudio = new Border
                    {
                        Background = DS.LightBg(DS.ResultPrimary),
                        CornerRadius = new CornerRadius(16),
                        Width = 32, Height = 32,
                        Cursor = Cursors.Hand,
                        Margin = new Thickness(8, 0, 0, 0),
                        Child = new TextBlock { Text = "🔊", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, FontSize = 16 }
                    };
                    DockPanel.SetDock(btnAudio, Dock.Right);
                    var capturedQ = p.Question;
                    btnAudio.MouseLeftButtonDown += (_, e) => { e.Handled = true; SpeakText(capturedQ); };
                    qDock.Children.Add(btnAudio);
                }

                var questionText = new TextBlock
                {
                    Text = p.Question, FontSize = DS.FontResult,
                    FontFamily = DS.FontPrimary, TextWrapping = TextWrapping.Wrap
                };
                qDock.Children.Add(questionText);
                spCard.Children.Add(qDock);
                
                var optWrap = new StackPanel();
                bool isAnswered = false;

                if (p.Options != null)
                {
                    for (int i = 0; i < p.Options.Count; i++)
                    {
                        var letter = ((char)('A' + i)).ToString();
                        var isCorrect = letter == p.GetAnswer();
                        var optBorder = UI.ResultRow("  " + FormatOptionText(p.Options[i], letter), DS.ResultInfo);
                        optBorder.Margin = new Thickness(0, 2, 0, 0);
                        
                        var capturedCorrect = isCorrect;
                        var capturedLetter = letter;
                        optBorder.MouseLeftButtonDown += (s, e) =>
                        {
                            if (isAnswered) return;
                            isAnswered = true;
                            
                            currentResult.UserAnswer = capturedLetter;
                            currentResult.IsCorrect = capturedCorrect;

                            if (capturedCorrect)
                            {
                                optBorder.Background = DS.LightBg(DS.ResultSuccess);
                                optBorder.BorderBrush = DS.Brush(DS.ResultSuccess);
                                correctAnswers++;
                            }
                            else
                            {
                                optBorder.Background = DS.LightBg(DS.ResultDanger);
                                optBorder.BorderBrush = DS.Brush(DS.ResultDanger);
                            }
                        };
                        optWrap.Children.Add(optBorder);
                    }
                }
                spCard.Children.Add(optWrap);
                
                var container = new Border
                {
                    Background = System.Windows.Media.Brushes.White,
                    CornerRadius = new CornerRadius(DS.RadiusCard),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 0, 0, 16),
                    BorderBrush = DS.BrushAlpha(DS.ResultPrimary, 40),
                    BorderThickness = new Thickness(1),
                    Child = spCard
                };
                mainPanel.Children.Add(container);
            }

            var startTime = DateTime.Now;
            UI.PresetButton("🏁 Hoàn Thành", DS.ResultPrimary, () =>
            {
                var timeSpent = (DateTime.Now - startTime).TotalSeconds;
                MessageBox.Show($"Bạn đã trả lời đúng {correctAnswers}/{selectedProblems.Count} câu hỏi!", "Kết quả", MessageBoxButton.OK, MessageBoxImage.Information);
                
                try
                {
                    var app = Application.Current as QASmartTouch.App;
                    if (app != null)
                    {
                        var db = app.Database;
                        var history = new MathQuizHistory
                        {
                            StudentCode = "GV", // Mặc định là GV nếu không có session
                            StudentName = "Giáo viên",
                            Grade = ch.Metadata.Grade.ToString(),
                            ChapterId = ch.Metadata.ChapterId,
                            ChapterName = ch.Metadata.ChapterName,
                            Difficulty = difficulty,
                            TotalQuestions = selectedProblems.Count,
                            CorrectAnswers = correctAnswers,
                            TimeSpentSeconds = timeSpent,
                            QuizType = quizType,
                            CompletedAt = DateTime.Now,
                            ProblemResultsJson = System.Text.Json.JsonSerializer.Serialize(problemResults)
                        };
                        db.MathQuizHistories.Add(history);
                        db.SaveChanges();

                        // 🔔 REAL-TIME ALERT: Notify teacher if score is critical (< 5.0) - Skip alert for GV
                        double scoreVal = (double)correctAnswers / selectedProblems.Count * 10.0;
                        if (scoreVal < 5.0 && history.StudentCode != "GV")
                        {
                            var shell = Window.GetWindow(this) as QASmartClass.Classroom.Views.ClassroomShell;
                            shell?.ShowToast("Cảnh báo Hiệu suất", $"⚠️ {history.StudentName} đạt kết quả thấp ({scoreVal:F1}/10) tại chương: {history.ChapterName}", "ALERT");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "MathCurriculum: Failed to save quiz history");
                }

                ShowChapterDetail(ch);
            }, mainPanel);
        }

        private void ShowPerformanceTracking(string gradeFilter = "All")
        {
            _currentDashboardView = "performance";
            mainPanel.Children.Clear();
            btnBackToChapters.Visibility = Visibility.Visible;
            AnimateMainPanel();

            var header = new Grid { Margin = new Thickness(0, 0, 0, 20) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleSp = new StackPanel();
            titleSp.Children.Add(UI.Title("📊 Theo dõi Kết quả Học tập", 28, DS.TextPrimary));
            titleSp.Children.Add(UI.Text("Phân tích tiến độ và hiệu quả luyện tập của học sinh", 14, DS.TextSecondary));
            header.Children.Add(titleSp);
            
            mainPanel.Children.Add(header);

            // Filter row
            var filterSp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 20) };
            filterSp.Children.Add(new TextBlock { Text = "Lọc theo khối lớp: ", VerticalAlignment = VerticalAlignment.Center, FontSize = 14 });
            var comboGrade = new ComboBox { Width = 120, Margin = new Thickness(10, 0, 0, 0) };
            comboGrade.Items.Add("Tất cả");
            var availableGrades = _chapters.Select(c => c.Metadata.Grade.ToString()).Distinct().OrderBy(g => g).ToList();
            foreach (var g in availableGrades) comboGrade.Items.Add("Lớp " + g);
            
            comboGrade.SelectedIndex = gradeFilter == "All" ? 0 : availableGrades.IndexOf(gradeFilter) + 1;
            comboGrade.SelectionChanged += (s, e) => {
                string selected = comboGrade.SelectedIndex == 0 ? "All" : availableGrades[comboGrade.SelectedIndex - 1];
                if (selected != gradeFilter) ShowPerformanceTracking(selected);
            };
            filterSp.Children.Add(comboGrade);

            var btnExport = UI.PresetButton("📥 Xuất CSV", DS.ResultInfo, () => ExportPerformanceToCSV(gradeFilter));
            btnExport.Margin = new Thickness(20, 0, 0, 0);
            filterSp.Children.Add(btnExport);

            var btnClear = UI.PresetButton("🗑️ Xóa Lịch Sử", DS.ResultDanger, () => {
                if (MessageBox.Show("Bạn có chắc chắn muốn xóa tất cả lịch sử làm bài? Thao tác này không thể hoàn tác.", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes) {
                    ClearQuizHistory();
                }
            });
            btnClear.Margin = new Thickness(10, 0, 0, 0);
            filterSp.Children.Add(btnClear);

            mainPanel.Children.Add(filterSp);

            try
            {
                var app = Application.Current as QASmartTouch.App;
                if (app == null) return;
                var db = app.Database;
                var query = db.MathQuizHistories.AsQueryable();
                if (gradeFilter != "All") query = query.Where(h => h.Grade == gradeFilter);
                
                var histories = query
                    .OrderByDescending(h => h.CompletedAt)
                    .Take(100)
                    .ToList();

                if (histories.Count == 0)
                {
                    mainPanel.Children.Add(UI.Text("Chưa có dữ liệu luyện tập cho khối này.", 16, DS.TextSecondary));
                    return;
                }

                // Quick Stats
                var statsGrid = new System.Windows.Controls.Primitives.UniformGrid { Columns = 4, Margin = new Thickness(0, 0, 0, 20) };
                
                var totalSessions = histories.Count;
                var totalCorrect = histories.Sum(h => h.CorrectAnswers);
                var totalQuestions = histories.Sum(h => h.TotalQuestions);
                var avgAccuracy = totalQuestions > 0 ? (double)totalCorrect / totalQuestions * 100 : 0;
                var totalSeconds = histories.Sum(h => h.TimeSpentSeconds);

                statsGrid.Children.Add(CreateStatCard("Tổng lượt làm bài", totalSessions.ToString(), "sessions", DS.Primary));
                statsGrid.Children.Add(CreateStatCard("Độ chính xác TB", $"{avgAccuracy:F1}%", "accuracy", DS.Success));
                statsGrid.Children.Add(CreateStatCard("Tổng câu đúng", $"{totalCorrect}/{totalQuestions}", "questions", DS.Warning));
                statsGrid.Children.Add(CreateStatCard("Tổng thời gian", $"{(totalSeconds / 60):F0} phút", "time", DS.Accent));

                mainPanel.Children.Add(statsGrid);

                // Chapter Summary Analysis
                var chapterAnalysis = histories.GroupBy(h => h.ChapterName)
                    .Select(g => new {
                        Name = g.Key,
                        AvgScore = g.Average(h => h.TotalQuestions > 0 ? (double)h.CorrectAnswers / h.TotalQuestions * 10.0 : 0.0),
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.AvgScore)
                    .ToList();

                mainPanel.Children.Add(UI.Title("🎯 Phân tích hiệu quả theo Chương", 20, DS.TextPrimary));
                
                // Smart Recommendations Section
                var strugglingChapters = chapterAnalysis.Where(x => x.AvgScore < 5.0).ToList();
                if (strugglingChapters.Count > 0)
                {
                    var recSp = new StackPanel { Margin = new Thickness(0, 5, 0, 15) };
                    var recBorder = new Border {
                        Background = DS.LightBg(DS.ResultDanger),
                        CornerRadius = new CornerRadius(12),
                        Padding = new Thickness(15),
                        BorderBrush = DS.BrushAlpha(DS.ResultDanger, 40),
                        BorderThickness = new Thickness(1)
                    };
                    var recContent = new StackPanel();
                    recContent.Children.Add(new TextBlock { 
                        Text = "💡 Gợi ý hỗ trợ: Các chương sau có điểm trung bình thấp (< 5.0). Giáo viên nên tổ chức ôn tập lại hoặc giao thêm bài tập cơ bản cho nhóm C.",
                        FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = DS.Brush(DS.ResultDanger), TextWrapping = TextWrapping.Wrap
                    });
                    var recChips = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
                    foreach(var sc in strugglingChapters) {
                        var chip = new Border {
                            Background = Brushes.White, CornerRadius = new CornerRadius(15),
                            Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 0, 8, 4),
                            BorderBrush = DS.Brush(DS.ResultDanger), BorderThickness = new Thickness(1)
                        };
                        chip.Child = new TextBlock { Text = $"⚠️ {sc.Name}", FontSize = 13, Foreground = DS.Brush(DS.ResultDanger) };
                        recChips.Children.Add(chip);
                    }
                    recContent.Children.Add(recChips);
                    recBorder.Child = recContent;
                    recSp.Children.Add(recBorder);
                    mainPanel.Children.Add(recSp);
                }

                var summaryWrap = new WrapPanel { Margin = new Thickness(0, 10, 0, 25) };
                foreach (var stat in chapterAnalysis)
                {
                    var statusColor = stat.AvgScore >= 8 ? DS.Success : (stat.AvgScore >= 5 ? DS.Warning : DS.ResultDanger);
                    var card = new Border { 
                        Background = Brushes.White, 
                        Padding = new Thickness(18), 
                        Margin = new Thickness(0, 0, 14, 14), 
                        CornerRadius = new CornerRadius(16),
                        MinWidth = 240,
                        BorderBrush = DS.BrushAlpha(statusColor, 30),
                        BorderThickness = new Thickness(1),
                        Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 8, ShadowDepth = 2, Opacity = 0.05, Color = Colors.Black }
                    };
                    var cardSp = new StackPanel();
                    cardSp.Children.Add(new TextBlock { Text = stat.Name, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = DS.Brush(DS.TextPrimary), TextTrimming = TextTrimming.CharacterEllipsis });
                    
                    var scoreRow = new DockPanel { Margin = new Thickness(0, 4, 0, 8) };
                    var scoreText = new TextBlock { Text = $"{stat.AvgScore:F1}", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = DS.Brush(statusColor) };
                    var subText = new TextBlock { Text = $"/10 • {stat.Count} lượt", FontSize = 13, Foreground = DS.Brush(DS.TextSecondary), VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(4, 0, 0, 2) };
                    scoreRow.Children.Add(scoreText);
                    scoreRow.Children.Add(subText);
                    cardSp.Children.Add(scoreRow);
                    
                    var barContainer = new Border { Height = 8, Background = DS.LightBg(DS.TextSecondary), CornerRadius = new CornerRadius(4), Opacity = 0.3 };
                    var bar = new Border { 
                        Height = 8, 
                        Background = new LinearGradientBrush(statusColor, Color.FromArgb(180, statusColor.R, statusColor.G, statusColor.B), 0),
                        HorizontalAlignment = HorizontalAlignment.Left, 
                        Width = stat.AvgScore * 20, 
                        CornerRadius = new CornerRadius(4)
                    };
                    barContainer.Child = bar;
                    cardSp.Children.Add(barContainer);
                    
                    card.Child = cardSp;
                    summaryWrap.Children.Add(card);
                }
                mainPanel.Children.Add(summaryWrap);

                // History Table Header
                var tableHeader = new DockPanel { Margin = new Thickness(0, 10, 0, 5) };
                tableHeader.Children.Add(UI.Title("📜 Nhật ký luyện tập chi tiết", 20, DS.TextPrimary));
                mainPanel.Children.Add(tableHeader);
                
                var grid = UI.StandardDataGrid(histories);
                grid.Margin = new Thickness(0, 5, 0, 0);

                grid.Columns.Add(new DataGridTextColumn { Header = "Thời gian", Binding = new System.Windows.Data.Binding("CompletedAt") { StringFormat = "dd/MM HH:mm" }, Width = 110 });
                grid.Columns.Add(new DataGridTextColumn { Header = "Học sinh", Binding = new System.Windows.Data.Binding("StudentName"), Width = 150 });
                grid.Columns.Add(new DataGridTextColumn { Header = "Lớp", Binding = new System.Windows.Data.Binding("Grade"), Width = 50 });
                grid.Columns.Add(new DataGridTextColumn { Header = "Loại", Binding = new System.Windows.Data.Binding("QuizType"), Width = 100 });
                grid.Columns.Add(new DataGridTextColumn { Header = "Nội dung", Binding = new System.Windows.Data.Binding("ChapterName"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
                grid.Columns.Add(new DataGridTextColumn { Header = "Đúng", Binding = new System.Windows.Data.Binding("CorrectAnswers"), Width = 60 });
                grid.Columns.Add(new DataGridTextColumn { Header = "Tổng", Binding = new System.Windows.Data.Binding("TotalQuestions"), Width = 60 });
                grid.Columns.Add(new DataGridTextColumn { Header = "Điểm", Binding = new System.Windows.Data.Binding("Score") { StringFormat = "F1" }, Width = 60 });
                grid.Columns.Add(new DataGridTextColumn { Header = "Thời gian (s)", Binding = new System.Windows.Data.Binding("TimeSpentSeconds") { StringFormat = "N0" }, Width = 90 });

                grid.MouseDoubleClick += (s, e) => {
                    if (grid.SelectedItem is MathQuizHistory selected) {
                        ShowQuizResultDetails(selected);
                    }
                };

                var scroll = new ScrollViewer { 
                    Content = grid, 
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto, 
                    Height = 450,
                    Padding = new Thickness(0, 0, 10, 0)
                };

                var tableHeaderSp = new StackPanel();
                tableHeaderSp.Children.Add(tableHeader);
                tableHeaderSp.Children.Add(new TextBlock { Text = "(Nhấp đúp vào một hàng để xem chi tiết từng câu hỏi)", FontSize = 12, Foreground = DS.Brush(DS.TextSecondary), Margin = new Thickness(0,-5,0,10), FontStyle = FontStyles.Italic });

                var tableBorder = new Border { 
                    Child = scroll, 
                    Margin = new Thickness(0, 10, 0, 20), 
                    BorderBrush = DS.BrushAlpha(DS.TextSecondary, 20), 
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10)
                };

                mainPanel.Children.Add(tableHeaderSp);
                mainPanel.Children.Add(tableBorder);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "MathCurriculum: Error loading performance tracking");
                mainPanel.Children.Add(UI.Text("Lỗi khi tải dữ liệu: " + ex.Message, 14, DS.ResultError));
            }
        }

        private Border CreateStatCard(string label, string value, string icon, Color color)
        {
            var border = new Border
            {
                Background = DS.LightBg(color),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(24),
                Margin = new Thickness(8),
                BorderBrush = DS.BrushAlpha(color, 80),
                BorderThickness = new Thickness(1)
            };

            var stack = new StackPanel();
            stack.Children.Add(UI.Text(label.ToUpper(), 11, color));
            stack.Children.Add(UI.Title(value, 32, color));
            
            border.Child = stack;
            return border;
        }

        private void ShowCustomQuizBuilder()
        {
            _currentDashboardView = "builder";
            mainPanel.Children.Clear();
            btnBackToChapters.Visibility = Visibility.Visible;
            AnimateMainPanel();
            
            var header = new Grid { Margin = new Thickness(0, 0, 0, 20) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleSp = new StackPanel();
            titleSp.Children.Add(UI.Title("🛠️ Thiết lập Đề Kiểm tra", 28, DS.TextPrimary));
            titleSp.Children.Add(UI.Text("Tùy chỉnh nội dung ôn tập và kiểm tra định kỳ", 14, DS.TextSecondary));
            header.Children.Add(titleSp);
            mainPanel.Children.Add(header);

            var selectionSp = new StackPanel();
            
            // 1. Grade Selection
            selectionSp.Children.Add(UI.Title("1. Chọn Khối lớp", 16, DS.BrandPrimary));
            var gradeWrap = new WrapPanel { Margin = new Thickness(0, 8, 0, 16) };
            var grades = _chapters.Select(c => c.Metadata.Grade).Distinct().OrderBy(g => g).ToList();
            
            var chapterListSp = new StackPanel();
            void RefreshChapterList(int grade)
            {
                chapterListSp.Children.Clear();
                var chapters = _chapters.Where(c => c.Metadata.Grade == grade).ToList();
                foreach (var ch in chapters)
                {
                    var cb = new CheckBox
                    {
                        Content = ch.Metadata.ChapterName,
                        Margin = new Thickness(0, 4, 0, 4),
                        FontSize = 14,
                        Tag = ch
                    };
                    chapterListSp.Children.Add(cb);
                }
            }

            foreach (var g in grades)
            {
                var capturedG = g;
                var gBtn = UI.PresetButton($"Lớp {g}", DS.ResultInfo, () => RefreshChapterList(capturedG));
                gBtn.Margin = new Thickness(0, 0, 8, 8);
                gradeWrap.Children.Add(gBtn);
            }
            selectionSp.Children.Add(gradeWrap);

            // 2. Chapter Selection
            selectionSp.Children.Add(UI.Title("2. Chọn Chương", 16, DS.BrandPrimary));
            var scroll = new ScrollViewer { 
                Content = chapterListSp, 
                Height = 250, 
                Margin = new Thickness(0, 8, 0, 16), 
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = DS.LightBg(DS.ResultInfo),
                Padding = new Thickness(10)
            };
            selectionSp.Children.Add(new Border { Child = scroll, CornerRadius = new CornerRadius(8), BorderBrush = DS.BrushAlpha(DS.ResultInfo, 40), BorderThickness = new Thickness(1) });
            
            // 3. Parameters
            selectionSp.Children.Add(UI.Title("3. Cấu hình", 16, DS.BrandPrimary));
            var paramGrid = new System.Windows.Controls.Grid { Margin = new Thickness(0, 8, 0, 16) };
            paramGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            paramGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            paramGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            paramGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Slider for count
            var lblCount = new TextBlock { Text = "Số câu hỏi: ", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,10,0) };
            var countSp = new StackPanel { Orientation = Orientation.Horizontal };
            var slider = new Slider { Minimum = 5, Maximum = 50, Value = 10, TickFrequency = 5, IsSnapToTickEnabled = true, Width = 300, VerticalAlignment = VerticalAlignment.Center };
            var txtVal = new TextBlock { Text = "10", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0), FontWeight = FontWeights.Bold };
            slider.ValueChanged += (s, e) => txtVal.Text = slider.Value.ToString();
            countSp.Children.Add(slider); countSp.Children.Add(txtVal);
            
            Grid.SetRow(lblCount, 0); Grid.SetColumn(lblCount, 0);
            Grid.SetRow(countSp, 0); Grid.SetColumn(countSp, 1);
            paramGrid.Children.Add(lblCount); paramGrid.Children.Add(countSp);

            // Difficulty
            var lblDiff = new TextBlock { Text = "Độ khó: ", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,10,10,0) };
            var comboDiff = new ComboBox { Width = 150, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0,10,0,0) };
            comboDiff.Items.Add("Trộn (Mix)");
            comboDiff.Items.Add("Dễ (Easy)");
            comboDiff.Items.Add("Trung bình (Medium)");
            comboDiff.Items.Add("Khó (Hard)");
            comboDiff.SelectedIndex = 0;

            Grid.SetRow(lblDiff, 1); Grid.SetColumn(lblDiff, 0);
            Grid.SetRow(comboDiff, 1); Grid.SetColumn(comboDiff, 1);
            paramGrid.Children.Add(lblDiff); paramGrid.Children.Add(comboDiff);

            selectionSp.Children.Add(paramGrid);

            // 4. Generate
            var generateBtn = UI.PresetButton("🚀 Tạo và Bắt Đầu Quiz", DS.ResultSuccess, () => {
                var selectedChapters = chapterListSp.Children.OfType<CheckBox>().Where(cb => cb.IsChecked == true).Select(cb => (MathChapterData)cb.Tag).ToList();
                if (selectedChapters.Count == 0) {
                    MessageBox.Show("Vui lòng chọn ít nhất một chương!");
                    return;
                }
                string diff = comboDiff.SelectedIndex switch { 1 => "Easy", 2 => "Medium", 3 => "Hard", _ => "Mix" };
                GenerateAndStartCustomQuiz(selectedChapters, (int)slider.Value, diff);
            });
            generateBtn.Padding = new Thickness(30, 12, 30, 12);
            generateBtn.HorizontalAlignment = HorizontalAlignment.Center;
            selectionSp.Children.Add(generateBtn);

            mainPanel.Children.Add(UI.SectionCard(selectionSp));

            // Default to first grade to show something
            if (grades.Count > 0) RefreshChapterList(grades[0]);
        }

        private void GenerateAndStartCustomQuiz(List<MathChapterData> chapters, int count, string difficulty)
        {
            var allProblems = chapters.SelectMany(ch => ch.Sections.SelectMany(s => s.Problems)).ToList();
            if (difficulty != "Mix")
            {
                var filtered = allProblems.Where(p => p.Difficulty == difficulty).ToList();
                if (filtered.Count > 0) allProblems = filtered;
            }

            if (allProblems.Count == 0)
            {
                MessageBox.Show("Không tìm thấy câu hỏi phù hợp với tiêu chí!");
                return;
            }

            var rnd = new Random();
            var selected = allProblems.OrderBy(x => rnd.Next()).Take(count).ToList();

            var customCh = new MathChapterData
            {
                Metadata = new ChapterMetadata
                {
                    ChapterId = "CUSTOM",
                    ChapterName = chapters.Count == 1 ? chapters[0].Metadata.ChapterName : "Đề Tổng Hợp (" + chapters.Count + " chương)",
                    Grade = chapters[0].Metadata.Grade
                }
            };

            StartPractice(customCh, selected, selected.Count, difficulty, "CustomQuiz");
        }
        private void ExportPerformanceToCSV(string gradeFilter)
        {
            try
            {
                var app = Application.Current as QASmartTouch.App;
                if (app == null) return;
                var db = app.Database;
                var query = db.MathQuizHistories.AsQueryable();
                if (gradeFilter != "All") query = query.Where(h => h.Grade == gradeFilter);
                
                var histories = query.OrderByDescending(h => h.CompletedAt).ToList();
                if (histories.Count == 0)
                {
                    MessageBox.Show("Không có dữ liệu để xuất.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"MathPerformance_{gradeFilter}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (sfd.ShowDialog() == true)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("Thoi Gian,Hoc Sinh,Ma HS,Khoi,Chuong,Do Kho,So Cau,Dung,Ty Le,Thoi Gian Lam(s),Loai");
                    foreach (var h in histories)
                    {
                        var rate = h.TotalQuestions > 0 ? (double)h.CorrectAnswers / h.TotalQuestions * 100.0 : 0.0;
                        sb.AppendLine($"{h.CompletedAt:yyyy-MM-dd HH:mm:ss},{h.StudentName},{h.StudentCode},{h.Grade},{h.ChapterName},{h.Difficulty},{h.TotalQuestions},{h.CorrectAnswers},{rate:F1}%,{h.TimeSpentSeconds:F0},{h.QuizType}");
                    }
                    // Write with UTF-8 BOM encoding to ensure Excel displays Vietnamese signs correctly
                    System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), new System.Text.UTF8Encoding(true));
                    MessageBox.Show($"Đã xuất dữ liệu thành công ra file: {System.IO.Path.GetFileName(sfd.FileName)}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "MathCurriculum: Export failed");
                MessageBox.Show("Lỗi khi xuất dữ liệu: " + ex.Message);
            }
        }

        private void ClearQuizHistory()
        {
            try
            {
                var app = Application.Current as QASmartTouch.App;
                if (app == null) return;
                var db = app.Database;
                db.MathQuizHistories.RemoveRange(db.MathQuizHistories);
                db.SaveChanges();
                ShowPerformanceTracking();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "MathCurriculum: Clear history failed");
                MessageBox.Show("Lỗi khi xóa dữ liệu: " + ex.Message);
            }
        }

        private void ShowQuizResultDetails(MathQuizHistory history)
        {
            _currentDashboardView = "details";
            mainPanel.Children.Clear();
            btnBackToChapters.Visibility = Visibility.Visible;
            AnimateMainPanel();
            txtHeaderTitle.Text = $"📄 Chi tiết: {history.ChapterName}";
            txtHeaderSub.Text = $"Học sinh: {history.StudentName} • {history.CompletedAt:dd/MM/yyyy HH:mm}";

            var problemResults = new List<MathProblemResult>();
            try {
                if (!string.IsNullOrEmpty(history.ProblemResultsJson))
                    problemResults = System.Text.Json.JsonSerializer.Deserialize<List<MathProblemResult>>(history.ProblemResultsJson) ?? new();
            } catch { /* Fallback for old data */ }

            var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
            
            // Score summary card
            var summaryCard = new Border {
                Background = DS.LightBg(history.Score >= 8 ? DS.Success : (history.Score >= 5 ? DS.Warning : DS.ResultDanger)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 20)
            };
            var summarySp = new StackPanel();
            summarySp.Children.Add(UI.Title($"Kết quả: {history.CorrectAnswers}/{history.TotalQuestions} câu đúng", 20, DS.TextPrimary));
            summarySp.Children.Add(UI.Text($"Điểm số: {history.Score:F1}/10 • Thời gian: {history.TimeSpentSeconds:F0} giây", 14, DS.TextSecondary));
            summaryCard.Child = summarySp;
            sp.Children.Add(summaryCard);

            if (problemResults.Count == 0)
            {
                sp.Children.Add(UI.Text("Không có dữ liệu chi tiết cho bài làm này (Dữ liệu cũ hoặc lỗi serialization).", 14, DS.ResultError));
            }

            foreach (var res in problemResults)
            {
                var resBorder = new Border {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 0, 0, 12),
                    BorderBrush = DS.BrushAlpha(res.IsCorrect ? DS.Success : DS.ResultDanger, 40),
                    BorderThickness = new Thickness(1)
                };
                
                var resSp = new StackPanel();
                resSp.Children.Add(new TextBlock { Text = res.Question, FontSize = 14, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
                
                var ansRow = new WrapPanel();
                ansRow.Children.Add(new TextBlock { Text = "Trả lời: ", FontSize = 13, Foreground = DS.Brush(DS.TextSecondary) });
                ansRow.Children.Add(new TextBlock { Text = string.IsNullOrEmpty(res.UserAnswer) ? "(Bỏ trống)" : res.UserAnswer, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = DS.Brush(res.IsCorrect ? DS.Success : DS.ResultDanger) });
                
                if (!res.IsCorrect)
                {
                    ansRow.Children.Add(new TextBlock { Text = "  •  Đáp án đúng: ", FontSize = 13, Foreground = DS.Brush(DS.TextSecondary), Margin = new Thickness(10, 0, 0, 0) });
                    ansRow.Children.Add(new TextBlock { Text = res.CorrectAnswer, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = DS.Brush(DS.Success) });
                }
                resSp.Children.Add(ansRow);

                if (!string.IsNullOrEmpty(res.Solution))
                {
                    var solBorder = new Border {
                        Background = DS.LightBg(DS.ResultInfo),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(10),
                        Margin = new Thickness(0, 8, 0, 0)
                    };
                    solBorder.Child = new TextBlock { Text = "💡 Giải thích: " + res.Solution, FontSize = 12, FontStyle = FontStyles.Italic, TextWrapping = TextWrapping.Wrap };
                    resSp.Children.Add(solBorder);
                }

                resBorder.Child = resSp;
                sp.Children.Add(resBorder);
            }

            var scroll = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(0,0,10,0) };
            mainPanel.Children.Add(scroll);
        }

        private void AnimateMainPanel()
        {
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };
            mainPanel.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new System.Collections.Generic.List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "🌉",
                    Title = isVN ? "Thiết kế Kỹ thuật & Kiến trúc" : "Engineering & Architectural Design",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_mathcurriculum_1_{suffix}.png",
                    Description = isVN 
                        ? "Vận dụng lượng giác, hình học tọa độ, vectơ lực và phép tính tích phân để thiết kế kết cấu cầu dây văng, phân tích khả năng chịu lực và tải trọng vật lý." 
                        : "Apply trigonometry, coordinate geometry, force vectors, and integral calculus to design cable-stayed bridge structures and analyze physical load capacities."
                },
                new PracticalAppItem
                {
                    Icon = "🧪",
                    Title = isVN ? "Toán học trong Hóa học & Phòng thí nghiệm" : "Mathematics in Chemistry & Lab Work",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_mathcurriculum_2_{suffix}.png",
                    Description = isVN 
                        ? "Ứng dụng phương trình trạng thái khí lý tưởng, hàm số logarit để tính độ pH, và đại số để cân bằng phương trình phản ứng hóa học cùng nồng độ dung dịch." 
                        : "Apply ideal gas equations, logarithmic functions to calculate pH levels, and algebra to balance chemical reaction equations and solution concentrations."
                },
                new PracticalAppItem
                {
                    Icon = "⚛️",
                    Title = isVN ? "Vật lý Toán & Cơ học Lượng tử" : "Mathematical Physics & Quantum Mechanics",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_mathcurriculum_3_{suffix}.png",
                    Description = isVN 
                        ? "Sử dụng giải tích hàm, đại số toán tử và tích phân nâng cao để nghiên cứu các tính chất phổ của hệ lượng tử và tính toán hiệu chỉnh phổ trong vật lý toán." 
                        : "Use functional analysis, operator algebras, and advanced integration to study the spectral properties of quantum systems and compute spectral corrections."
                },
                new PracticalAppItem
                {
                    Icon = "🎮",
                    Title = isVN ? "Đồ họa Máy tính & Mô phỏng Vật lý" : "Computer Graphics & Physics Simulations",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_formulas_3_{suffix}.png",
                    Description = isVN 
                        ? "Ứng dụng các công thức động học, vectơ và giải tích để mô phỏng chuyển động thực tế của nhân vật, hiệu ứng ánh sáng và va chạm vật lý trong trò chơi điện tử và đồ họa 3D." 
                        : "Apply kinematics formulas, vectors, and calculus to simulate realistic character movements, lighting effects, and physical collisions in video games and 3D graphics."
                },
                new PracticalAppItem
                {
                    Icon = "💻",
                    Title = isVN ? "Khoa học Máy tính & Logic Boolean" : "Computer Science & Boolean Logic",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_math_symbols_2_{suffix}.png",
                    Description = isVN 
                        ? "Ứng dụng lý thuyết tập hợp và đại số Boolean vào thiết kế cổng logic, tối ưu thuật toán và truy vấn cơ sở dữ liệu, đặt nền tảng tư duy toán học lập trình vững chắc." 
                        : "Apply set theory and Boolean algebra to logic gate design, algorithm optimization, and database queries, building a solid mathematical foundation for programming."
                },
                new PracticalAppItem
                {
                    Icon = "🤖",
                    Title = isVN ? "Học sâu & Thiết kế Mạng Nơ-ron" : "Deep Learning & Neural Network Design",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_math_symbols_3_{suffix}.png",
                    Description = isVN 
                        ? "Khám phá toán học đằng sau trí tuệ nhân tạo thông qua việc tính toán lan truyền ngược, đạo hàm hàm mất mát và tối ưu hóa trọng số trong các kiến trúc mạng nơ-ron phức tạp." 
                        : "Explore the mathematics behind artificial intelligence through backpropagation calculus, loss function derivatives, and weight optimization in complex neural network architectures."
                }
            };

            try
            {
                if (practicalAppViewer != null) practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for MathCurriculumTool: {Err}", ex.Message);
            }
        }

        private static void SetResultRowText(Border border, string text, Color color)
        {
            if (border.Child is Grid grid && grid.Children.Count > 0)
            {
                var content = UI.RenderMixedContent(text, DS.FontResult, DS.Brush(color));
                Grid.SetColumn(content, 0);
                grid.Children[0] = content;
            }
            else if (border.Child is TextBlock tb)
            {
                tb.Foreground = DS.Brush(color);
                tb.Text = text;
            }
        }

        private class StudentProgress
        {
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int Score { get; set; }
            public int Streak { get; set; }
            public DateTime LastUpdateTime { get; set; }
        }

        private void OnStudentNetworkMessage(object? sender, Classroom.Services.StudentMessageEventArgs e)
        {
            if (e == null || string.IsNullOrEmpty(e.Message)) return;

            try
            {
                if (e.Message.StartsWith("QUIZ_PROGRESS|"))
                {
                    var parts = e.Message.Split('|');
                    if (parts.Length >= 7)
                    {
                        string code = parts[1];
                        string name = parts[2];
                        int score = int.TryParse(parts[3], out int s) ? s : 0;
                        int streak = int.TryParse(parts[6], out int st) ? st : 0;

                        var prog = _studentScores.GetOrAdd(code, _ => new StudentProgress { Code = code });
                        prog.Name = name;
                        prog.Score = score;
                        prog.Streak = streak;
                        prog.LastUpdateTime = DateTime.Now;
                    }
                }
                else if (e.Message.StartsWith("{") && e.Message.EndsWith("}"))
                {
                    using (var doc = JsonDocument.Parse(e.Message))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "quiz_progress")
                        {
                            string code = e.StudentCode;
                            string name = root.TryGetProperty("studentName", out var nameProp) ? nameProp.GetString() ?? code : code;
                            int score = root.TryGetProperty("score", out var scoreProp) ? scoreProp.GetInt32() : 0;
                            int streak = root.TryGetProperty("streak", out var streakProp) ? streakProp.GetInt32() : 0;

                            var prog = _studentScores.GetOrAdd(code, _ => new StudentProgress { Code = code });
                            prog.Name = name;
                            prog.Score = score;
                            prog.Streak = streak;
                            prog.LastUpdateTime = DateTime.Now;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug("OnStudentNetworkMessage error: {Err}", ex.Message);
            }
        }

        private void StartLeaderboardTimer()
        {
            if (_leaderboardTimer == null)
            {
                _leaderboardTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
                _leaderboardTimer.Tick += (s, e) => UpdateLeaderboardUI();
            }
            _leaderboardTimer.Start();
            UpdateLeaderboardUI();
        }

        private void StopLeaderboardTimer()
        {
            _leaderboardTimer?.Stop();
        }

        private void UpdateLeaderboardUI()
        {
            if (_leaderboardListPanel == null) return;

            Task.Run(() =>
            {
                var topStudents = _studentScores.Values
                    .OrderByDescending(s => s.Score)
                    .ThenByDescending(s => s.Streak)
                    .ThenBy(s => s.LastUpdateTime)
                    .Take(5)
                    .ToList();

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_leaderboardListPanel == null) return;
                    _leaderboardListPanel.Children.Clear();

                    if (topStudents.Count == 0)
                    {
                        _leaderboardListPanel.Children.Add(new TextBlock
                        {
                            Text = "💤 Chưa có dữ liệu làm bài từ học sinh...",
                            FontSize = DS.FontLabel, FontFamily = DS.FontPrimary,
                            Foreground = DS.Brush(DS.ResultInfo),
                            FontStyle = FontStyles.Italic,
                            Margin = new Thickness(0, 4, 0, 4)
                        });
                        return;
                    }

                    var medals = new[] { "🥇", "🥈", "🥉", "4️⃣", "5️⃣" };
                    var bgs = new[] { "#FFF9C4", "#F5F5F5", "#FFE0B2", "#FFFFFF", "#FFFFFF" };
                    var fgs = new[] { "#E65100", "#424242", "#E65100", "#757575", "#757575" };

                    for (int i = 0; i < topStudents.Count; i++)
                    {
                        var student = topStudents[i];
                        var medal = medals[i];
                        var bg = bgs[i];
                        var fg = fgs[i];

                        var rowBorder = new Border
                        {
                            Background = (Brush)new BrushConverter().ConvertFromString(bg)!,
                            CornerRadius = new CornerRadius(8),
                            Padding = new Thickness(12, 8, 12, 8),
                            Margin = new Thickness(0, 0, 0, 4),
                            BorderBrush = (Brush)new BrushConverter().ConvertFromString("#E0E0E0")!,
                            BorderThickness = new Thickness(1)
                        };

                        var dock = new DockPanel();
                        
                        var medalTb = new TextBlock
                        {
                            Text = medal, Width = 30, FontSize = 16,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontFamily = DS.FontPrimary
                        };
                        dock.Children.Add(medalTb);

                        var scoreTb = new TextBlock
                        {
                            Text = $"{student.Score}đ", FontWeight = FontWeights.Bold,
                            Foreground = (Brush)new BrushConverter().ConvertFromString(fg)!,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontFamily = DS.FontPrimary, FontSize = 14
                        };
                        DockPanel.SetDock(scoreTb, Dock.Right);
                        dock.Children.Add(scoreTb);

                        var infoSp = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
                        infoSp.Children.Add(new TextBlock
                        {
                            Text = student.Name, FontWeight = FontWeights.SemiBold,
                            FontSize = 13, FontFamily = DS.FontPrimary,
                            Foreground = DS.Brush(DS.TextPrimary)
                        });
                        infoSp.Children.Add(new TextBlock
                        {
                            Text = $"Chuỗi đúng: {student.Streak} 🔥", FontSize = DS.FontNote,
                            FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.ResultInfo)
                        });
                        
                        dock.Children.Add(infoSp);
                        rowBorder.Child = dock;
                        
                        _leaderboardListPanel.Children.Add(rowBorder);
                    }
                }));
            });
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewTrain == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewTrain.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewTrain.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
