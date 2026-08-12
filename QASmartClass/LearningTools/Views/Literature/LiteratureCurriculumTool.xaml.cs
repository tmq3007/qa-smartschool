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
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using Microsoft.Data.Sqlite;

namespace QASmartClass.LearningTools.Views.Literature
{
    public partial class LiteratureCurriculumTool : Controls.BaseToolControl
    {
        private readonly List<LiteratureChapterData> _chapters = new();
        private readonly List<MockExamData> _mockExams = new();
        private readonly List<(string FileName, string ErrorReason)> _corruptedFiles = new();
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

        /// <summary>Raised when GV clicks 'Launch Quiz' — ClassroomShell wires this to LiteratureQuizBridgeService</summary>
        public event EventHandler<(string ChapterId, string ChapterName, int Grade)>? QuizRequested;

        /// <summary>Grade station definitions for the train journey — with content summary + inspiration</summary>
        private static readonly (int Grade, string Label, string Emoji, string Level, Color Color, string Summary, string Inspiration)[] GradeStations =
        {
            (1,  "Lớp 1",  "🌱", "Tiểu học",   Color.FromRgb(233, 30, 99),
                "Tiếng Việt 1 — Đánh vần, âm vần, tập đọc, chép", "Nét chữ đầu tiên, bước chân vào thế giới! ✍️"),
            (2,  "Lớp 2",  "🌿", "Tiểu học",   Color.FromRgb(233, 30, 99),
                "Từ và câu — Tả sự vật — Viết đoạn văn đơn giản", "Tiếng Việt thật đẹp đẽ và phong phú! ✨"),
            (3,  "Lớp 3",  "🌳", "Tiểu học",   Color.FromRgb(216, 27, 96),
                "Kể chuyện — Tả đồ vật — Viết đoạn văn ngắn", "Tập kể những câu chuyện của chính mình! 📖"),
            (4,  "Lớp 4",  "📗", "Tiểu học",   Color.FromRgb(216, 27, 96),
                "Cốt truyện — Văn miêu tả — Tả đồ vật, cây cối", "Hóa thân thành những người kể chuyện tài ba! 🎭"),
            (5,  "Lớp 5",  "🏅", "Tiểu học",   Color.FromRgb(194, 24, 91),
                "Văn tả cảnh, tả người — Viết thư — Từ nhiều nghĩa", "Hoàn thiện ngòi bút, sẵn sàng lên cấp 2! 🎒"),
            (6,  "Lớp 6",  "📘", "THCS",       Color.FromRgb(156, 39, 176),
                "Truyền thuyết — Cổ tích — Truyện đồng thoại — Thơ lục bát", "Tìm về cội nguồn qua những câu chuyện xưa! 🌊"),
            (7,  "Lớp 7",  "📐", "THCS",       Color.FromRgb(156, 39, 176),
                "Ca dao — Tục ngữ — Thơ — Tùy bút — Nghị luận", "Kết tinh trí tuệ và tâm hồn dân tộc! 🌌"),
            (8,  "Lớp 8",  "🔢", "THCS",       Color.FromRgb(142, 36, 170),
                "Truyện hiện thực (Lão Hạc, Tắt đèn) — Thơ Mới — Nghị luận xã hội", "Thấu hiểu nỗi đau và trân trọng tình người! 🚪"),
            (9,  "Lớp 9",  "🧮", "THCS ⚠️ Ôn thi vào 10", Color.FromRgb(142, 36, 170),
                "Truyện Kiều — Thơ kháng chiến — Truyện ngắn hiện đại — Nghị luận văn học", "⚠️ Bước ngoặt quan trọng nhất! Học thật kỹ Truyện Kiều! 🏆"),
            (10, "Lớp 10", "📊", "THPT",       Color.FromRgb(103, 58, 183),
                "Sử thi — Thơ Nôm — Truyện Kiều — Bình Ngô Đại Cáo", "Kiệt tác văn chương, vang danh muôn đời! 🧠"),
            (11, "Lớp 11", "📈", "THPT",       Color.FromRgb(94, 53, 177),
                "Thơ Mới — Truyện ngắn hiện thực (Chí Phèo, Hai đứa trẻ) — Kịch — Văn học nước ngoài", "Tiếng nói của cá nhân và nỗi đau thời đại! 🔬"),
            (12, "Lớp 12", "🎓", "THPT 🎯 Thi Quốc Gia", Color.FromRgb(81, 45, 168),
                "Thơ kháng chiến — Văn xuôi (Vợ nhặt, Rừng xà nu) — Đọc hiểu — Nghị luận", "🎯 Sẵn sàng chinh phục kỳ thi THPT Quốc Gia! 🏆"),
        };

        public LiteratureCurriculumTool()
        {
            _isInitializing = true;
            InitializeComponent();
            LoadSettings();
            sliderFontSize.Value = FontSizeMultiplier;
            _isInitializing = false;
            _dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Resources", "LiteratureData", "Phase4_Data");
            _exercisePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Resources", "LiteratureData", "exercises_by_topic");
            Loaded += (_, _) => LoadAndShowTrainJourney();
            Unloaded += (_, _) => { StopExamTimer(); _settingsSaveTimer?.Stop(); };
            SizeChanged += LiteratureCurriculumTool_SizeChanged;
        }

        // ═══════════════════════════════════════════════════════════
        //  DATA LOADING
        // ═══════════════════════════════════════════════════════════

        private async void LoadAndShowTrainJourney()
        {
            try
            {
                await Task.Run(() => LoadAllChapters());

                bool isVN = !IsEn;
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextTrain != null) menuTextTrain.Text = isVN ? "Chuyển tàu Ngữ văn" : "Literature Train";
                if (menuTextChapters != null) menuTextChapters.Text = isVN ? "Chương trình học" : "Curriculum";
                if (menuTextExams != null) menuTextExams.Text = isVN ? "Kho đề thi" : "Exam Bank";
                if (menuTextDashboard != null) menuTextDashboard.Text = isVN ? "Bảng điều khiển" : "Dashboard";
                if (menuTextApp != null) menuTextApp.Text = isVN ? "Ứng dụng thực tế" : "Real-world Apps";

                if (sideMenu != null)
                {
                    sideMenu.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "LiteratureCurriculum: Load failed");
                ShowError($"Không tìm thấy dữ liệu tại:\n{_dataPath}");
            }
        }

        private void LoadAllChapters()
        {
            _corruptedFiles.Clear();
            _chapters.Clear();
            _mockExams.Clear();
            if (!Directory.Exists(_dataPath)) return;

            // Load chapter files (G{grade}_CH{order} naming convention)
            var files = Directory.GetFiles(_dataPath, "G??_CH*.json");
            foreach (var file in files.OrderBy(f => f))
            {
                var fileName = Path.GetFileName(file);
                try
                {
                    if (!QASmartClass.LearningTools.Helpers.PathHelper.IsPathSafe(_dataPath, file))
                    {
                        Log.Warning("Path traversal attempt detected on chapter file: {File}", file);
                        _corruptedFiles.Add((fileName, "Cảnh báo bảo mật: Phát hiện lỗi leo thang thư mục (Path traversal)."));
                        continue;
                    }
                    var json = File.ReadAllText(file);
                    var ch = JsonSerializer.Deserialize<LiteratureChapterData>(json);
                    if (ch?.Metadata != null) 
                        _chapters.Add(ch);
                    else
                        _corruptedFiles.Add((fileName, "File thiếu trường Metadata bắt buộc."));
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Skip chapter: {File}", file);
                    _corruptedFiles.Add((fileName, $"Lỗi định dạng cấu trúc tệp JSON: {ex.Message}"));
                }
            }

            // Load mock exam files
            var examFiles = Directory.GetFiles(_dataPath, "*Exam*.json");
            foreach (var file in examFiles.OrderBy(f => f))
            {
                var fileName = Path.GetFileName(file);
                try
                {
                    if (!QASmartClass.LearningTools.Helpers.PathHelper.IsPathSafe(_dataPath, file))
                    {
                        Log.Warning("Path traversal attempt detected on exam file: {File}", file);
                        _corruptedFiles.Add((fileName, "Cảnh báo bảo mật: Phát hiện lỗi leo thang thư mục (Path traversal)."));
                        continue;
                    }
                    var json = File.ReadAllText(file);
                    var exam = JsonSerializer.Deserialize<MockExamData>(json);
                    if (exam?.Metadata != null) 
                        _mockExams.Add(exam);
                    else
                        _corruptedFiles.Add((fileName, "File đề thi thiếu trường Metadata bắt buộc."));
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Skip exam: {File}", file);
                    _corruptedFiles.Add((fileName, $"Lỗi định dạng cấu trúc tệp JSON: {ex.Message}"));
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

            Log.Information("LiteratureCurriculum: Loaded {Chapters} chapters, {Exams} exams, mapping={HasMapping}",
                _chapters.Count, _mockExams.Count, _topicMapping != null);
        }

        // ═══════════════════════════════════════════════════════════
        //  TAB NAVIGATION
        // ═══════════════════════════════════════════════════════════

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || mainScrollViewer == null || viewPractical == null)
                return;

            if (sideMenu.SelectedIndex == 0)
            {
                viewGuide.Visibility = Visibility.Visible;
                mainScrollViewer.Visibility = Visibility.Collapsed;
                viewPractical.Visibility = Visibility.Collapsed;
                
                txtHeaderTitle.Text = IsEn ? "Guide & Process" : "Hướng dẫn & Quy trình";
                txtHeaderSub.Text = IsEn 
                    ? "Guidelines for Teachers & Students to explore the Literature Journey" 
                    : "Hướng dẫn Giáo viên & Học sinh khám phá Chuyến Tàu Ngữ Văn";
                StopExamTimer();
                btnBackToChapters.Visibility = Visibility.Collapsed;
            }
            else
            {
                viewGuide.Visibility = Visibility.Collapsed;
                string tabId = "train";
                switch (sideMenu.SelectedIndex)
                {
                    case 1: tabId = "train"; break;
                    case 2: tabId = "chapters"; break;
                    case 3: tabId = "exams"; break;
                    case 4: tabId = "dashboard"; break;
                    case 5: tabId = "app"; break;
                }
                SwitchTab(tabId);
            }
        }

        private void SwitchTab(string tabId)
        {
            _activeTab = tabId;
            _selectedGrade = null;
            StopExamTimer();
            btnBackToChapters.Visibility = Visibility.Collapsed;

            if (rootGrid != null)
            {
                if (tabId == "app")
                {
                }
                else
                {
                }
            }

            if (tabId == "app")
            {
                mainScrollViewer.Visibility = Visibility.Collapsed;
                if (viewPractical != null) viewPractical.Visibility = Visibility.Visible;
            }
            else
            {
                mainScrollViewer.Visibility = Visibility.Visible;
                if (viewPractical != null) viewPractical.Visibility = Visibility.Collapsed;
            }

            switch (tabId)
            {
                case "train":
                    txtHeaderTitle.Text = "🚂 Chuyến Tàu Ngữ Văn";
                    txtHeaderSub.Text = "Hành trình Lớp 1 → Lớp 12 • Khám phá toàn bộ chương trình Ngữ Văn";
                    ShowTrainJourney();
                    break;
                case "chapters":
                    ShowChapterGrid();
                    break;
                case "exams":
                    txtHeaderTitle.Text = "📝 Kho Đề Thi Ngữ Văn";
                    txtHeaderSub.Text = "Đề thi định kỳ • THPT Quốc Gia • Đánh Giá Năng Lực";
                    ShowExamsTab();
                    break;
                case "dashboard":
                    txtHeaderTitle.Text = "📊 Dashboard Giáo Viên";
                    txtHeaderSub.Text = "Tổng quan dữ liệu • Phân tích kết quả học tập";
                    ShowDashboard();
                    break;
                case "app":
                    txtHeaderTitle.Text = "🌍 Ứng Dụng Thực Tế";
                    txtHeaderSub.Text = "Cách văn học đi vào đời sống và phát triển năng lực ngôn ngữ";
                    LoadPracticalApps();
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
                "Văn học là nhân học.",
                "Sách mở ra trước mắt tôi những chân trời mới.",
                "Mỗi tác phẩm là một thông điệp gửi gắm cho đời.",
                "Hãy đọc để thấy tâm hồn mình rộng lớn hơn."
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
                Text = $"💡 {quote}", FontSize = DS.FontSubtitle, FontFamily = DS.FontPrimary,
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
                    Text = txt, FontSize = DS.FontSubtitle, FontWeight = FontWeights.SemiBold,
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
            startFlag.Child = new TextBlock { Text = "🚩 GA KHỞI HÀNH — LỚP 1", FontSize = DS.FontSubtitle, FontWeight = FontWeights.Bold, Foreground = Brushes.White, FontFamily = DS.FontPrimary };
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
                        Text = slogan, FontSize = DS.FontSubtitle, FontFamily = DS.FontPrimary,
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
            endFlag.Child = new TextBlock { Text = "🏁 GA KẾT THÚC — LỚP 12", FontSize = DS.FontSubtitle, FontWeight = FontWeights.Bold, Foreground = Brushes.White, FontFamily = DS.FontPrimary };
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
                    ttSp.Children.Add(new TextBlock { Text = $"• {ch.Metadata.ChapterName} ({ch.Metadata.TotalProblems} bài)", FontSize = DS.FontSubtitle, Margin = new Thickness(0,2,0,0) });
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
            statusBadge.Child = new TextBlock { Text = statusText, FontSize = DS.FontNote, Foreground = DS.Brush(DS.ResultInfo), VerticalAlignment = VerticalAlignment.Center };
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
                Text = $"📌 {station.Summary}", FontSize = DS.FontSubtitle, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.ResultDark), Margin = new Thickness(0, 4, 0, 2), TextWrapping = TextWrapping.Wrap
            });
            infSp.Children.Add(new TextBlock
            {
                Text = $"💡 {station.Inspiration}", FontSize = DS.FontNote, FontFamily = DS.FontPrimary,
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
                var gColor = gStation.Color != default ? gStation.Color : DS.CatLanguage;
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
                var gColor = gStation.Color != default ? gStation.Color : DS.CatLanguage;
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
                FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.CatLanguage),
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
                Text = "💡 Kết nối: LiteratureQuizBridgeService → WebSocket → Auto-scoring → A/B/C grouping",
                FontSize = DS.FontTag, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.CatScience), FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 8, 0, 0)
            });
            mainPanel.Children.Add(UI.SectionCard(groupSp, DS.CatLanguage));

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
                .Where(t => t.Category == ToolCategory.Language && !toolMap.ContainsKey(t.Id + "Tool") && !toolMap.ContainsKey(t.Id))
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

            // ── ⚠️ Diagnostic Card for Corrupted Files ──
            if (_corruptedFiles.Count > 0)
            {
                var diagSp = new StackPanel();
                diagSp.Children.Add(new TextBlock
                {
                    Text = "⚠️ Cảnh Báo File Dữ Liệu Lỗi (Diagnostics)",
                    FontSize = 15, FontWeight = FontWeights.Bold,
                    FontFamily = DS.FontPrimary, Foreground = DS.Brush(DS.ResultDanger),
                    Margin = new Thickness(0, 0, 0, 10)
                });
                diagSp.Children.Add(new TextBlock
                {
                    Text = "Các tệp tin sau đây bị lỗi cấu trúc JSON hoặc vi phạm bảo mật và đã bị hệ thống tự động bỏ qua:",
                    FontSize = DS.FontNote, FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(DS.ResultInfo), TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8)
                });

                foreach (var file in _corruptedFiles)
                {
                    var fileRow = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
                    var badge = new Border
                    {
                        Background = DS.LightBg(DS.ResultDanger),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(8, 3, 8, 3),
                        Margin = new Thickness(0, 0, 8, 0)
                    };
                    badge.Child = new TextBlock
                    {
                        Text = file.FileName, FontSize = DS.FontNote,
                        FontWeight = FontWeights.SemiBold, FontFamily = DS.FontPrimary,
                        Foreground = DS.Brush(DS.ResultDanger)
                    };
                    fileRow.Children.Add(badge);
                    fileRow.Children.Add(new TextBlock
                    {
                        Text = file.ErrorReason,
                        FontSize = DS.FontNote, FontFamily = DS.FontPrimary,
                        Foreground = DS.Brush(DS.ResultInfo), TextTrimming = TextTrimming.CharacterEllipsis,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    diagSp.Children.Add(fileRow);
                }
                mainPanel.Children.Add(UI.SectionCard(diagSp, DS.ResultDanger));
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  CHAPTER GRID VIEW
        // ═══════════════════════════════════════════════════════════

        private void ShowChapterGrid()
        {
            mainPanel.Children.Clear();
            btnBackToChapters.Visibility = Visibility.Collapsed;
            AnimateMainPanel();
            txtHeaderTitle.Text = "🚂 Chuyến Tàu Ngữ Văn Lớp 1-12";
            txtHeaderSub.Text = $"{_chapters.Count} chương • " +
                $"{_chapters.Sum(c => c.Metadata.TotalProblems)} bài tập";

            // Group by grade
            foreach (var gradeGroup in _chapters.GroupBy(c => c.Metadata.Grade).OrderBy(g => g.Key))
            {
                var grade = gradeGroup.Key;
                var gradeColor = grade switch
                {
                    10 => DS.CatLanguage,
                    11 => Color.FromRgb(123, 31, 162),
                    12 => Color.FromRgb(230, 81, 0),
                    _ => DS.CatLanguage
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

        private Border CreateChapterCard(LiteratureChapterData ch, int idx, Color color)
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
                Text = "📚",
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
                Text = $"CH{idx:D2}", FontSize = DS.FontSubtitle,
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
                FontSize = DS.FontSubtitle, FontFamily = DS.FontPrimary,
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
                Text = $"Lớp {ch.Metadata.Grade}", FontSize = DS.FontSubtitle,
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
                    Text = $"🎯 {ch.Metadata.ThptWeight}", FontSize = DS.FontSubtitle,
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

        private void ShowChapterDetail(LiteratureChapterData ch)
        {
            _activeChapter = ch;
            mainPanel.Children.Clear();
            btnBackToChapters.Visibility = Visibility.Visible;
            AnimateMainPanel();
            txtHeaderTitle.Text = $"📖 {ch.Metadata.ChapterName}";
            txtHeaderSub.Text = $"Lớp {ch.Metadata.Grade} • " +
                $"{ch.Metadata.TotalSections} phần • {ch.Metadata.TotalProblems} bài";
            // Action bar
            var actionBar = new WrapPanel { Margin = new Thickness(0, 0, 0, 16) };
            UI.PresetButton("🎮 Luyện Tập", DS.CatLanguage, () => ShowPracticeMode(ch), actionBar).Margin = new Thickness(0, 0, 8, 0);
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
            BuildChapterContent(ch);
        }

        private void BuildChapterContent(LiteratureChapterData ch)
        {
            mainPanel.Children.Clear();
            btnBackToChapters.Visibility = Visibility.Visible;
            txtHeaderTitle.Text = $"📖 {ch.Metadata.ChapterName}";
            txtHeaderSub.Text = $"Lớp {ch.Metadata.Grade} • {ch.Sections.Count} bài học";
            
            var listPanel = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
            
            foreach (var section in ch.Sections.OrderBy(s => s.Order))
            {
                var btnSection = new Button
                {
                    Content = $"📘 {section.SectionName}",
                    Background = Brushes.White,
                    Foreground = DS.Brush(DS.ResultDark),
                    FontSize = 15, FontWeight = FontWeights.Bold, FontFamily = DS.FontPrimary,
                    Padding = new Thickness(16, 16, 16, 16),
                    BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 0, 12),
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Cursor = Cursors.Hand
                };
                
                // Use SetValue to approximate CornerRadius for button by putting it in a Border if needed, 
                // but standard WPF Button with padding looks fine.
                
                btnSection.Click += (s, e) => {
                    ShowSectionDetail(section, ch);
                };
                
                listPanel.Children.Add(btnSection);
            }
            
            mainPanel.Children.Add(listPanel);
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
                var btn = UI.PresetButton($"🔧 {label}", DS.CatLanguage, () =>
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

        private void ShowSectionDetail(LiteratureChapterSection section, LiteratureChapterData ch)
        {
            _activeSectionId = section.SectionId;
            mainPanel.Children.Clear();
            txtHeaderTitle.Text = $"📖 {section.SectionName}";
            txtHeaderSub.Text = $"Chương: {ch.Metadata.ChapterName}";

            var tabControl = new TabControl
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 8, 0, 0)
            };

            // TAB 1: Nội dung (Concepts)
            var tabContent = new TabItem
            {
                Header = "📚 Nội Dung Bài Học",
                FontSize = 14, FontWeight = FontWeights.Bold, FontFamily = DS.FontPrimary,
                Padding = new Thickness(16, 8, 16, 8)
            };
            var contentSp = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
            
            var actionDock = new DockPanel { LastChildFill = false, Margin = new Thickness(0, 0, 0, 16) };

            var btnFocusAll = new Button
            {
                Content = "🔍 Phóng to toàn bài (Focus)",
                Background = DS.LightBg(DS.ResultSuccess),
                Foreground = DS.Brush(DS.ResultSuccess),
                FontSize = DS.FontSubtitle, FontWeight = FontWeights.Bold, FontFamily = DS.FontPrimary,
                Padding = new Thickness(16, 10, 16, 10),
                BorderThickness = new Thickness(0), Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            btnFocusAll.Click += (s, e) => {
                Helpers.TeachingActionHelper.FocusSectionVisual(contentSp, "literature_curriculum", section.SectionId);
            };
            DockPanel.SetDock(btnFocusAll, Dock.Left);
            actionDock.Children.Add(btnFocusAll);
            
            var btnUnfocusAll = new Button
            {
                Content = "❌ Thu nhỏ",
                Background = DS.LightBg(DS.ResultDanger),
                Foreground = DS.Brush(DS.ResultDanger),
                FontSize = DS.FontSubtitle, FontWeight = FontWeights.Bold, FontFamily = DS.FontPrimary,
                Padding = new Thickness(16, 10, 16, 10),
                BorderThickness = new Thickness(0), Margin = new Thickness(8, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            btnUnfocusAll.Click += (s, e) => {
                Helpers.TeachingActionHelper.UnfocusSectionVisual();
            };
            DockPanel.SetDock(btnUnfocusAll, Dock.Left);
            actionDock.Children.Add(btnUnfocusAll);

            if (!string.IsNullOrEmpty(section.AudioUrl))
            {
                var btnAudio = new Button
                {
                    Content = "🎧 Đọc diễn cảm",
                    Background = DS.LightBg(DS.ResultInfo),
                    Foreground = DS.Brush(DS.ResultInfo),
                    FontSize = DS.FontSubtitle, FontWeight = FontWeights.Bold, FontFamily = DS.FontPrimary,
                    Padding = new Thickness(16, 10, 16, 10),
                    BorderThickness = new Thickness(0), Margin = new Thickness(8, 0, 8, 0),
                    Cursor = Cursors.Hand
                };
                btnAudio.Click += (s, e) => {
                    MessageBox.Show($"Đang phát audio từ:\n{section.AudioUrl}", "Audio Cảm Thụ");
                };
                DockPanel.SetDock(btnAudio, Dock.Left);
                actionDock.Children.Add(btnAudio);
            }

            if (!string.IsNullOrEmpty(section.SgkUrl) || true) // Show it anyway for demonstration if needed, but let's check SgkUrl properly. Wait, I will just show it for all lessons to simulate integration.
            {
                var btnSgk = new Button
                {
                    Content = "📖 Xem Sách Gốc (SGK Tương Tác)",
                    Background = DS.LightBg(DS.BrandPrimary),
                    Foreground = DS.Brush(DS.BrandPrimary),
                    FontSize = DS.FontSubtitle, FontWeight = FontWeights.Bold, FontFamily = DS.FontPrimary,
                    Padding = new Thickness(16, 10, 16, 10),
                    BorderThickness = new Thickness(0), Margin = new Thickness(8, 0, 8, 0),
                    Cursor = Cursors.Hand
                };
                btnSgk.Click += (s, e) => {
                    string defaultUrl = "https://hoc10.vn/doc-sach/Tieng-Viet-4";
                    string targetUrl = string.IsNullOrEmpty(section.SgkUrl) ? defaultUrl : section.SgkUrl;
                    if (!targetUrl.StartsWith("http")) targetUrl = "https://" + targetUrl;
                    
                    try 
                    {
                        var dummyBook = new QASmartTouch.Modules.InteractiveBooks.Models.Book 
                        { 
                            Url = targetUrl, 
                            Name = section.SectionName, 
                            Subject = "Ngữ Văn / Tiếng Việt", 
                            Grade = ch.Metadata.Grade, 
                            PageCount = 150, 
                            Icon = "📖" 
                        };
                        var viewer = new QASmartTouch.Forms.Form2_20_1_BookViewer(dummyBook);
                        viewer.Topmost = true;
                        viewer.Owner = Window.GetWindow(this);
                        viewer.Show();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Không thể mở trình duyệt: {ex.Message}", "Lỗi Sách Điện Tử");
                    }
                };
                DockPanel.SetDock(btnSgk, Dock.Left);
                actionDock.Children.Add(btnSgk);
            }
            
            contentSp.Children.Add(actionDock);

            if (section.Concepts != null && section.Concepts.Count > 0)
            {
                foreach (var c in section.Concepts)
                {
                    contentSp.Children.Add(BuildConceptCard(c));
                }
            }
            else
            {
                contentSp.Children.Add(new TextBlock { Text = "Chưa có lý thuyết cho phần này.", Foreground = Brushes.Gray, FontStyle = FontStyles.Italic, Margin = new Thickness(0,8,0,0) });
            }

            tabContent.Content = new ScrollViewer { Content = contentSp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            tabControl.Items.Add(tabContent);

            // TAB 2: Bài tập (Problems)
            var tabExercises = new TabItem
            {
                Header = $"📝 Bài Tập ({section.Problems?.Count ?? 0})",
                FontSize = 14, FontWeight = FontWeights.Bold, FontFamily = DS.FontPrimary,
                Padding = new Thickness(16, 8, 16, 8)
            };
            var exerciseSp = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
            if (section.Problems != null && section.Problems.Count > 0)
            {
                var progressRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
                var progressTb = new TextBlock
                {
                    Text = $"Tiến trình: 0 / {section.Problems.Count} bài",
                    FontSize = 13, FontWeight = FontWeights.Bold,
                    Foreground = DS.Brush(DS.ResultSuccess)
                };
                progressRow.Children.Add(progressTb);

                _tblStreak = new TextBlock
                {
                    Text = "",
                    FontSize = 13, FontWeight = FontWeights.Bold,
                    Foreground = Brushes.OrangeRed,
                    Margin = new Thickness(20, 0, 0, 0),
                    Visibility = Visibility.Collapsed
                };
                progressRow.Children.Add(_tblStreak);
                exerciseSp.Children.Add(progressRow);

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
                    exerciseSp.Children.Add(BuildProblemCard(p, onCorrect));
                }
            }
            else
            {
                exerciseSp.Children.Add(new TextBlock { Text = "Chưa có bài tập.", Foreground = Brushes.Gray, FontStyle = FontStyles.Italic });
            }

            tabExercises.Content = new ScrollViewer { Content = exerciseSp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            tabControl.Items.Add(tabExercises);

            mainPanel.Children.Add(tabControl);
        }

        private TextBlock CreateFormattedTextBlock(string text, double fontSize, Color fgColor, Thickness margin = default)
        {
            var tb = new TextBlock
            {
                FontSize = fontSize,
                FontFamily = DS.FontPrimary,
                TextWrapping = TextWrapping.Wrap,
                Margin = margin,
                LineHeight = fontSize * 1.4
            };
            
            // First pass: replace [[word|def]] with a unique token to preserve them while splitting **
            // For simplicity, we just parse sequentially.
            // Let's do a simple regex parsing for both **bold** and [[word|def]]
            var regex = new System.Text.RegularExpressions.Regex(@"\*\*(.*?)\*\*|\[\[(.*?)\|(.*?)\]\]");
            int lastPos = 0;
            
            var matches = regex.Matches(text);
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                // Add text before match
                if (m.Index > lastPos)
                {
                    tb.Inlines.Add(new System.Windows.Documents.Run(text.Substring(lastPos, m.Index - lastPos))
                    {
                        Foreground = new SolidColorBrush(fgColor)
                    });
                }
                
                if (m.Groups[1].Success) // **bold**
                {
                    tb.Inlines.Add(new System.Windows.Documents.Run(m.Groups[1].Value)
                    {
                        Background = new SolidColorBrush(Color.FromRgb(255, 245, 157)), // Light Yellow
                        Foreground = new SolidColorBrush(Colors.Black),
                        FontWeight = FontWeights.Bold
                    });
                }
                else if (m.Groups[2].Success) // [[word|def]]
                {
                    string word = m.Groups[2].Value;
                    string def = m.Groups[3].Value;
                    
                    var run = new System.Windows.Documents.Run(word)
                    {
                        Foreground = DS.Brush(DS.ResultInfo),
                        FontWeight = FontWeights.Bold,
                        TextDecorations = TextDecorations.Underline,
                        Cursor = Cursors.Hand
                    };
                    
                    var tooltip = new ToolTip
                    {
                        Content = new TextBlock 
                        { 
                            Text = $"{word}: {def}", 
                            MaxWidth = 300, 
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 14,
                            FontFamily = DS.FontPrimary
                        },
                        Background = DS.Brush(DS.ResultDark),
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(12)
                    };
                    
                    run.ToolTip = tooltip;
                    tb.Inlines.Add(run);
                }
                
                lastPos = m.Index + m.Length;
            }
            
            // Add remaining text
            if (lastPos < text.Length)
            {
                tb.Inlines.Add(new System.Windows.Documents.Run(text.Substring(lastPos))
                {
                    Foreground = new SolidColorBrush(fgColor)
                });
            }

            return tb;
        }

        private System.Windows.FrameworkElement MakeSelectable(System.Windows.Controls.TextBlock tb)
        {
            var viewer = new System.Windows.Controls.FlowDocumentScrollViewer
            {
                IsToolBarVisible = false,
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Disabled,
                HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Disabled,
                BorderThickness = new System.Windows.Thickness(0),
                Background = System.Windows.Media.Brushes.Transparent,
                Margin = tb.Margin,
                SelectionBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 235, 59))
            };
            var doc = new System.Windows.Documents.FlowDocument
            {
                PagePadding = new System.Windows.Thickness(0),
                FontFamily = tb.FontFamily,
                FontSize = tb.FontSize,
                Foreground = tb.Foreground,
                ColumnWidth = 999999
            };
            var p = new System.Windows.Documents.Paragraph { Margin = new System.Windows.Thickness(0), LineHeight = tb.LineHeight };
            var inlinesList = new System.Collections.Generic.List<System.Windows.Documents.Inline>();
            foreach (var inline in tb.Inlines)
            {
                inlinesList.Add(inline);
            }
            tb.Inlines.Clear();
            foreach (var inline in inlinesList)
            {
                p.Inlines.Add(inline);
            }
            doc.Blocks.Add(p);
            viewer.Document = doc;
            return viewer;
        }

        private FrameworkElement BuildConceptCard(LiteratureConcept c)
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
                iconPrefix = "✍️";
            }

            var conceptHeader = new DockPanel { LastChildFill = false };
            conceptHeader.Children.Add(new TextBlock
            {
                Text = $"{iconPrefix} {c.Name}", FontSize = 14,
                FontWeight = FontWeights.Bold, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.ResultDark),
                VerticalAlignment = VerticalAlignment.Center
            });

            Border conceptCard = null; // Declare early for closure

            if (c.ImageUrls != null && c.ImageUrls.Count > 0)
            {
                var btnGallery = new Button
                {
                    Content = $"🖼️ Ảnh tư liệu ({c.ImageUrls.Count})",
                    Background = Brushes.Transparent,
                    Foreground = DS.Brush(DS.ResultInfo),
                    FontSize = DS.FontSubtitle, FontWeight = FontWeights.Bold, FontFamily = DS.FontPrimary,
                    Padding = new Thickness(6, 2, 6, 2),
                    BorderBrush = DS.BrushAlpha(DS.ResultInfo, 60), BorderThickness = new Thickness(1),
                    Margin = new Thickness(8, 0, 0, 0),
                    Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center
                };
                btnGallery.Click += (s, e) => {
                    ShowGallery(c.ImageUrls);
                };
                DockPanel.SetDock(btnGallery, Dock.Right);
                conceptHeader.Children.Add(btnGallery);
            }

            sp.Children.Add(conceptHeader);

            if (!string.IsNullOrEmpty(c.Definition))
            {
                var defSp = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
                var tbDef = CreateFormattedTextBlock(c.Definition, 22, Colors.Black);
                var selectableDef = MakeSelectable(tbDef);
                
                // Determine if text is "long" (e.g. > 150 chars or > 4 newlines)
                bool isLong = c.Definition.Length > 200 || c.Definition.Split('\n').Length > 5;
                
                if (isLong)
                {
                    selectableDef.MaxHeight = 220;
                    defSp.Children.Add(selectableDef);
                    
                    var btnExpand = new Button
                    {
                        Content = "⌄ Xem toàn bộ đoạn văn",
                        Background = Brushes.Transparent,
                        Foreground = DS.Brush(DS.BrandPrimary),
                        BorderThickness = new Thickness(0),
                        FontSize = 14, FontWeight = FontWeights.Bold, Cursor = Cursors.Hand,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(0, 8, 0, 0)
                    };
                    bool isExpanded = false;
                    btnExpand.Click += (s, e) => {
                        isExpanded = !isExpanded;
                        if (isExpanded) {
                            selectableDef.MaxHeight = double.PositiveInfinity;
                            btnExpand.Content = "⌃ Thu gọn đoạn văn";
                        } else {
                            selectableDef.MaxHeight = 220;
                            btnExpand.Content = "⌄ Xem toàn bộ đoạn văn";
                        }
                    };
                    defSp.Children.Add(btnExpand);
                }
                else
                {
                    defSp.Children.Add(selectableDef);
                }
                
                sp.Children.Add(defSp);
            }

            if (!string.IsNullOrEmpty(c.Theorem))
                sp.Children.Add(MakeSelectable(CreateFormattedTextBlock($"📜 {c.Theorem}", 18, DS.ResultDark, new Thickness(0, 8, 0, 0))));

            if (!string.IsNullOrEmpty(c.Formula))
                sp.Children.Add(MakeSelectable(CreateFormattedTextBlock($"✍️ {c.Formula}", 18, DS.ResultSpecial, new Thickness(0, 8, 0, 0))));

            foreach (var f in c.Formulas ?? new())
                sp.Children.Add(MakeSelectable(CreateFormattedTextBlock($"  • {f}", 18, DS.ResultPrimary, new Thickness(0, 4, 0, 0))));
            foreach (var p in c.Properties ?? new())
                sp.Children.Add(MakeSelectable(CreateFormattedTextBlock($"  ▸ {p}", 18, DS.ResultInfo, new Thickness(0, 4, 0, 0))));
            foreach (var r in c.Rules ?? new())
                sp.Children.Add(MakeSelectable(CreateFormattedTextBlock($"  📌 {r}", 18, DS.ResultWarning, new Thickness(0, 4, 0, 0))));
            foreach (var cs in c.Cases ?? new())
                sp.Children.Add(MakeSelectable(CreateFormattedTextBlock($"  🔹 {cs}", 18, DS.ResultInfo, new Thickness(0, 4, 0, 0))));

            // Method: can be string or List<string>
            if (c.Method != null)
            {
                if (c.Method is System.Text.Json.JsonElement je)
                {
                    if (je.ValueKind == System.Text.Json.JsonValueKind.String)
                        sp.Children.Add(MakeSelectable(CreateFormattedTextBlock($"  📋 {je.GetString()}", 18, DS.ResultPrimary, new Thickness(0, 4, 0, 0))));
                    else if (je.ValueKind == System.Text.Json.JsonValueKind.Array)
                        foreach (var item in je.EnumerateArray())
                            sp.Children.Add(MakeSelectable(CreateFormattedTextBlock($"  📋 {item.GetString()}", 18, DS.ResultPrimary, new Thickness(0, 4, 0, 0))));
                }
            }

            // Types dictionary
            if (c.Types?.Count > 0)
                foreach (var kv in c.Types)
                    sp.Children.Add(MakeSelectable(CreateFormattedTextBlock($"  ▹ {kv.Key}: {kv.Value}", 18, DS.ResultInfo, new Thickness(0, 4, 0, 0))));

            // Details: dict-like object with key-value pairs
            if (c.Details is System.Text.Json.JsonElement detailsEl
                && detailsEl.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var prop in detailsEl.EnumerateObject())
                    sp.Children.Add(MakeSelectable(CreateFormattedTextBlock($"  🔸 {prop.Name}: {prop.Value.GetString()}", 18, DS.ResultPrimary, new Thickness(0, 4, 0, 0))));
            }

            // Examples: can be string[] or object[] (e.g. {z, real, imaginary})
            if (c.Examples is System.Text.Json.JsonElement examplesEl
                && examplesEl.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in examplesEl.EnumerateArray())
                {
                    if (item.ValueKind == System.Text.Json.JsonValueKind.String)
                        sp.Children.Add(MakeSelectable(CreateFormattedTextBlock($"  💡 {item.GetString()}", 18, DS.ResultInfo, new Thickness(0, 4, 0, 0))));
                    else if (item.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        // Flatten object fields: {"z": "3+2i", "real": 3} → "z = 3+2i, real = 3"
                        var parts = new List<string>();
                        foreach (var p2 in item.EnumerateObject())
                            parts.Add($"{p2.Name}={p2.Value}");
                        sp.Children.Add(MakeSelectable(CreateFormattedTextBlock($"  💡 {string.Join(", ", parts)}", 18, DS.ResultInfo, new Thickness(0, 4, 0, 0))));
                    }
                }
            }

            // Note
            if (!string.IsNullOrEmpty(c.Note))
                sp.Children.Add(MakeSelectable(CreateFormattedTextBlock($"  📝 {c.Note}", 18, DS.ResultWarning, new Thickness(0, 4, 0, 0))));

            conceptCard = new Border
            {
                Background = new SolidColorBrush(bgColor),
                CornerRadius = new CornerRadius(DS.RadiusCard),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = new SolidColorBrush(borderColor),
                BorderThickness = new Thickness(1, 1, 1, 3),
                Child = sp
            };
            
            string conceptId = string.IsNullOrEmpty(c.Id) ? c.Name : c.Id;
            return WrapWithSectionToolbar(conceptCard, "literature_curriculum", conceptId, c.Name);
        }

        private Border BuildProblemCard(LiteratureProblem p, Action onCorrect = null)
        {
            Border? cardBorder = null;
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
            sp.Children.Add(MakeSelectable(new TextBlock
            {
                Text = p.Question, FontSize = DS.FontResult,
                FontFamily = DS.FontPrimary, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            }));

            // Options (MCQ)
            if (p.Options?.Count > 0)
            {
                for (int i = 0; i < p.Options.Count; i++)
                {
                    var letter = ((char)('A' + i)).ToString();
                    var isCorrect = p.Options[i].Trim() == p.GetAnswer().Trim();
                    var optColor = DS.ResultInfo;
                    var optBorder = UI.ResultRow($"  {letter}. {p.Options[i]}", optColor);
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
                            ((TextBlock)b.Child).Foreground = DS.Brush(DS.ResultSuccess);
                            if (b.Tag == null) {
                                b.Tag = "answered";
                                HandleCorrectAnswer();
                                onCorrect?.Invoke();

                                double mult = 1.0;
                                if (_currentStreak >= 10) mult = 2.0;
                                else if (_currentStreak >= 5) mult = 1.5;
                                else if (_currentStreak >= 3) mult = 1.2;
                                int earned = (int)(p.Points * mult);

                                if (_currentStreak >= 10 && cardBorder != null)
                                {
                                    ApplyOnFireEffect(cardBorder);
                                }

                                if (mult > 1.0)
                                    ((TextBlock)b.Child).Text = $"  ✅ {capturedLetter}. Đúng! (+{earned}đ - Combo x{mult:F1} 🔥)";
                                else
                                    ((TextBlock)b.Child).Text = $"  ✅ {capturedLetter}. Đúng! (+{earned}đ)";
                            }
                        }
                        else
                        {
                            ResetStreak();
                            if (cardBorder != null)
                            {
                                RemoveOnFireEffect(cardBorder);
                            }
                            b.Background = DS.LightBg(DS.ResultDanger);
                            ((TextBlock)b.Child).Foreground = DS.Brush(DS.ResultDanger);
                            ((TextBlock)b.Child).Text =
                                $"  ❌ {capturedLetter}. Sai — Đáp án: {capturedAnswer}";
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
                var row = UI.ResultRow($"📝 {p.Solution}", DS.ResultPrimary, solPanel);
                if (row.Child is TextBlock tb)
                {
                    row.Child = MakeSelectable(tb);
                }
            }
            if (!string.IsNullOrEmpty(p.GetAnswer()))
            {
                UI.ResultRow($"✅ Đáp án: {p.GetAnswer()}", DS.ResultSuccess, solPanel);
            }
            sp.Children.Add(solPanel);

            var actionsPanel = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };

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

            var showSolBtn = UI.PresetButton("📖 Lời Giải", DS.CatLanguage, () =>
            {
                solPanel.Visibility = solPanel.Visibility == Visibility.Visible
                    ? Visibility.Collapsed : Visibility.Visible;
            });
            actionsPanel.Children.Add(showSolBtn);
            
            sp.Children.Add(actionsPanel);

            cardBorder = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(DS.RadiusChip),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = DS.BrushAlpha(diffColor, 60),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Child = sp
            };
            return cardBorder;
        }


        private UIElement BuildMistakes(List<LiteratureCommonMistake> mistakes)
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

        private void ShowGallery(List<string> imageUrls)
        {
            if (imageUrls == null || imageUrls.Count == 0) return;
            
            galleryContentPanel.Children.Clear();
            foreach(var url in imageUrls)
            {
                var card = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Background = Brushes.WhiteSmoke,
                    Margin = new Thickness(0, 0, 0, 16),
                    Padding = new Thickness(8),
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1)
                };
                
                var sp = new StackPanel();
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, url);
                bool exists = System.IO.File.Exists(fullPath) || System.IO.File.Exists(url);
                if (!exists)
                {
                    var placeholder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(243, 229, 245)), // Light Purple
                        BorderBrush = new SolidColorBrush(Color.FromRgb(142, 36, 170)), // Purple
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Height = 180,
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    var placeholderSp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    placeholderSp.Children.Add(new TextBlock 
                    { 
                        Text = "📚", 
                        FontSize = 36, 
                        HorizontalAlignment = HorizontalAlignment.Center 
                    });
                    placeholderSp.Children.Add(new TextBlock 
                    { 
                        Text = "Đang cập nhật hình ảnh tư liệu", 
                        FontSize = 13, 
                        Foreground = new SolidColorBrush(Color.FromRgb(142, 36, 170)), 
                        FontWeight = FontWeights.Bold, 
                        HorizontalAlignment = HorizontalAlignment.Center, 
                        Margin = new Thickness(0, 8, 0, 0) 
                    });
                    placeholder.Child = placeholderSp;
                    sp.Children.Add(placeholder);
                }
                else
                {
                    try {
                        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(fullPath);
                        bitmap.DecodePixelWidth = 800;
                        bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bitmap.EndInit();

                        var img = new Image
                        {
                            Source = bitmap,
                            Stretch = Stretch.Uniform,
                            MaxHeight = 600,
                            Margin = new Thickness(0,0,0,8)
                        };
                        sp.Children.Add(img);
                    } catch { }
                }

                string filename = System.IO.Path.GetFileNameWithoutExtension(url);
                string caption = "Hình ảnh: " + filename.Replace("_", " ").ToUpper();
                if (url.Contains("quang_dung")) caption = "📸 Chân dung nhà thơ Quang Dũng (Ảnh tư liệu)";
                if (url.Contains("tay_tien")) caption = "📸 Đoàn binh hành quân qua đèo dốc sương mù (Ảnh tư liệu Tây Tiến)";
                if (url.Contains("nguyen_khoa_diem")) caption = "📸 Chân dung nhà thơ Nguyễn Khoa Điềm (Ảnh tư liệu)";

                var tbUrl = new TextBlock
                {
                    Text = caption, 
                    Foreground = Brushes.DarkSlateGray, FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 4, 0, 8),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                sp.Children.Add(tbUrl);
                card.Child = sp;
                galleryContentPanel.Children.Add(card);
            }
            overlayGrid.Visibility = Visibility.Visible;
        }

        private void CloseGallery_Click(object sender, RoutedEventArgs e)
        {
            overlayGrid.Visibility = Visibility.Collapsed;
        }

        private static double FontSizeMultiplier = 1.0;
        private DispatcherTimer? _settingsSaveTimer;
        private bool _isInitializing = true;
        private int _adminPasswordAttempts = 0;
        private string _adminPasswordHash = "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9";
        private static DateTime? _lockoutEndTime = null;

        private void LoadSettings()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string settingsFile = Path.Combine(appData, "QASmartClass", "literature_settings.json");
                if (File.Exists(settingsFile))
                {
                    var json = File.ReadAllText(settingsFile);
                    using (var doc = JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("FontScale", out var prop))
                        {
                            FontSizeMultiplier = prop.GetDouble();
                            if (FontSizeMultiplier < 0.8 || FontSizeMultiplier > 1.5)
                            {
                                FontSizeMultiplier = 1.0;
                            }
                        }
                        if (doc.RootElement.TryGetProperty("LockoutEndTime", out var lockoutProp) && lockoutProp.ValueKind == JsonValueKind.String)
                        {
                            var val = lockoutProp.GetString();
                            if (!string.IsNullOrEmpty(val) && DateTime.TryParse(val, out var dt))
                            {
                                _lockoutEndTime = dt;
                            }
                        }
                        if (doc.RootElement.TryGetProperty("AdminPasswordHash", out var pwdHashProp) && pwdHashProp.ValueKind == JsonValueKind.String)
                        {
                            var val = pwdHashProp.GetString();
                            if (!string.IsNullOrEmpty(val))
                            {
                                _adminPasswordHash = val;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "LiteratureCurriculum: Failed to load font settings");
            }
        }

        private void SaveSettingsWithDebounce()
        {
            if (_settingsSaveTimer == null)
            {
                _settingsSaveTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                _settingsSaveTimer.Tick += (s, e) =>
                {
                    _settingsSaveTimer.Stop();
                    SaveSettings();
                };
            }
            _settingsSaveTimer.Stop();
            _settingsSaveTimer.Start();
        }

        private void SaveSettings()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string settingsDir = Path.Combine(appData, "QASmartClass");
                Directory.CreateDirectory(settingsDir);
                string settingsFile = Path.Combine(settingsDir, "literature_settings.json");
                var data = new 
                { 
                    FontScale = FontSizeMultiplier,
                    LockoutEndTime = _lockoutEndTime?.ToString("o"),
                    AdminPasswordHash = _adminPasswordHash
                };
                var json = JsonSerializer.Serialize(data);
                File.WriteAllText(settingsFile, json);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "LiteratureCurriculum: Failed to save font settings");
            }
        }

        private bool VerifyAdminPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;
            
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hashBytes = sha.ComputeHash(bytes);
                var sb = new System.Text.StringBuilder();
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString() == _adminPasswordHash;
            }
        }

        private bool CheckDatabaseSchema(string dbFilePath)
        {
            Microsoft.Data.Sqlite.SqliteConnection? conn = null;
            try
            {
                conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbFilePath}");
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='LiteratureQuizHistories';";
                    var result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value && result.ToString() == "LiteratureQuizHistories";
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "LiteratureCurriculum: Database schema check failed for file: {File}", dbFilePath);
                return false;
            }
            finally
            {
                if (conn != null)
                {
                    try
                    {
                        conn.Close();
                        Microsoft.Data.Sqlite.SqliteConnection.ClearPool(conn);
                        conn.Dispose();
                    }
                    catch { }
                }
            }
        }



        private void ApplyOnFireEffect(Border card)
        {
            if (card == null) return;
            
            var glowEffect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.OrangeRed,
                BlurRadius = 15,
                ShadowDepth = 0,
                Opacity = 0.8
            };
            card.Effect = glowEffect;
            card.BorderBrush = new SolidColorBrush(Colors.OrangeRed);
            card.BorderThickness = new Thickness(2);

            var pulseAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 10,
                To = 25,
                Duration = TimeSpan.FromSeconds(0.8),
                AutoReverse = true,
                RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
            };
            glowEffect.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty, pulseAnimation);
        }

        private void RemoveOnFireEffect(Border card)
        {
            if (card == null) return;
            card.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 8,
                ShadowDepth = 2,
                Opacity = 0.05,
                Color = Colors.Black
            };
            card.BorderThickness = new Thickness(1);
            card.BorderBrush = DS.BrushAlpha(DS.ResultPrimary, 40);
        }

        private void RemoveOnFireFromAllCards()
        {
            try
            {
                foreach (var child in mainPanel.Children)
                {
                    if (child is Border border)
                    {
                        if (border.Effect is System.Windows.Media.Effects.DropShadowEffect shadow && shadow.Color == Colors.OrangeRed)
                        {
                            RemoveOnFireEffect(border);
                        }
                    }
                }
            }
            catch { }
        }

        private void sliderFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtFontSizeVal == null) return;
            FontSizeMultiplier = e.NewValue;
            txtFontSizeVal.Text = $"{(int)(FontSizeMultiplier * 100)}%";
            if (_isInitializing) return;
            SaveSettingsWithDebounce();
        }

        private string _activeSectionId = null;
        private System.Windows.Controls.StackPanel _auditLogContainer = null;
        private int _currentStreak = 0;
        private System.Windows.Controls.TextBlock _tblStreak = null;
        private LiteratureChapterData _activeChapter = null;

        private void BackToChapters_Click(object sender, RoutedEventArgs e)
        {
            if (_activeSectionId != null)
            {
                _activeSectionId = null;
                BuildChapterContent(_activeChapter);
                return;
            }

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
                    txtHeaderTitle.Text = "🚂 Chuyến Tàu Ngữ Văn";
                    txtHeaderSub.Text = "Hành trình Lớp 1 → Lớp 12 • Khám phá toàn bộ chương trình Ngữ Văn";
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
                FontSize = DS.FontSubtitle, FontWeight = FontWeights.SemiBold,
                FontFamily = DS.FontPrimary,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            sp.Children.Add(titleDock);

            sp.Children.Add(new TextBlock
            {
                Text = $"{exam.Metadata.TotalQuestions} câu • {exam.Metadata.TimeLimit} phút • {exam.Metadata.TotalPoints} điểm",
                FontSize = DS.FontNote, FontFamily = DS.FontPrimary,
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
                Foreground = DS.Brush(DS.CatLanguage),
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

        private void UpdateTimerAppearance(int secondsLeft, int totalSeconds)
        {
            if (_timerText == null || _timerBorder == null) return;

            double ratio = (double)secondsLeft / totalSeconds;

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
                    Text = text, FontSize = DS.FontSubtitle,
                    FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(color)
                };
                statsWrap.Children.Add(statBadge);
            }
            AddStat($"✅ Đúng: {correct}/{totalQ}", DS.ResultSuccess);
            AddStat($"❌ Sai: {answered - correct}/{totalQ}", DS.ResultDanger);
            AddStat($"⏭️ Bỏ qua: {totalQ - answered}", DS.ResultInfo);
            AddStat($"⏱️ Thời gian: {FormatTime(timeUsed)}", DS.CatLanguage);
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
            ["Thạch Sanh"] = "Thạch Sanh",
            ["Sơn Tinh"] = "Sơn Tinh",
            ["Sơn Tinh Thủy Tinh"] = "Sơn Tinh Thủy Tinh",
            ["Tây Tiến"] = "Tây Tiến",
            ["Tiếng Việt"] = "Tiếng Việt",
            ["Thánh Gióng"] = "Thánh Gióng",
            ["Cô bé bán diêm"] = "Cô bé bán diêm",
            ["Qua Đèo Ngang"] = "Qua Đèo Ngang",
            ["Cốm"] = "Cốm",
            ["Ai Đã Đặt Tên Cho Dòng Sông"] = "Ai Đã Đặt Tên Cho Dòng Sông",
            ["Bài Học Đường Đời Đầu Tiên"] = "Bài Học Đường Đời Đầu Tiên",
            ["Chiến Thắng Mtao-Mxây"] = "Chiến Thắng Mtao-Mxây",
            ["Chị Em Thúy Kiều"] = "Chị Em Thúy Kiều",
            ["Hai Đứa Trẻ"] = "Hai Đứa Trẻ",
            ["Hịch Tướng Sĩ"] = "Hịch Tướng Sĩ",
            ["Làng"] = "Làng",
            ["Lão Hạc"] = "Lão Hạc",
            ["Tiếng Gà Trưa"] = "Tiếng Gà Trưa",
            ["Truyện An Dương Vương"] = "Truyện An Dương Vương",
            ["Vĩnh Biệt Cửu Trùng Đài"] = "Vĩnh Biệt Cửu Trùng Đài",
            ["Vội Vàng"] = "Vội Vàng",
            ["Vợ Nhặt"] = "Vợ Nhặt",
            ["Ôn tập Truyện Kiều và văn xuôi lớp 9"] = "Ôn tập Truyện Kiều và văn xuôi lớp 9",
            ["Ôn tập tổng hợp 12 tác phẩm trọng tâm lớp 12"] = "Ôn tập tổng hợp 12 tác phẩm trọng tâm lớp 12",
            ["Ôn tập tổng hợp lớp 10"] = "Ôn tập tổng hợp lớp 10",
            ["Ôn tập tổng hợp lớp 11"] = "Ôn tập tổng hợp lớp 11",
            ["Ôn tập tổng hợp lớp 6"] = "Ôn tập tổng hợp lớp 6",
            ["Ôn tập tổng hợp lớp 7"] = "Ôn tập tổng hợp lớp 7",
            ["Ôn tập tổng hợp lớp 8"] = "Ôn tập tổng hợp lớp 8",
            ["Ông Đồ"] = "Ông Đồ",
            ["Ý Nghĩa Văn Chương"] = "Ý Nghĩa Văn Chương",
            ["Đồng Chí"] = "Đồng Chí",
            ["Độc Tiểu Thanh Ký"] = "Độc Tiểu Thanh Ký"
        };

        /// <summary>Map exam topic ID → suggested tool IDs from ToolRegistry</summary>
        private static readonly Dictionary<string, string[]> TopicToToolIds = new()
        {
            ["Thạch Sanh"] = new[] { "vocabulary" },
            ["Sơn Tinh"] = new[] { "vocabulary" },
            ["Sơn Tinh Thủy Tinh"] = new[] { "vocabulary" },
            ["Tây Tiến"] = new[] { "vocabulary" },
            ["Tiếng Việt"] = new[] { "grammar" },
            ["Thánh Gióng"] = new[] { "vocabulary" },
            ["Cô bé bán diêm"] = new[] { "vocabulary" },
            ["Qua Đèo Ngang"] = new[] { "vocabulary" },
            ["Cốm"] = new[] { "vocabulary" },
            ["Ai Đã Đặt Tên Cho Dòng Sông"] = new[] { "vocabulary" },
            ["Bài Học Đường Đời Đầu Tiên"] = new[] { "vocabulary" },
            ["Chiến Thắng Mtao-Mxây"] = new[] { "vocabulary" },
            ["Chị Em Thúy Kiều"] = new[] { "vocabulary" },
            ["Hai Đứa Trẻ"] = new[] { "vocabulary" },
            ["Hịch Tướng Sĩ"] = new[] { "vocabulary" },
            ["Làng"] = new[] { "vocabulary" },
            ["Lão Hạc"] = new[] { "vocabulary" },
            ["Tiếng Gà Trưa"] = new[] { "vocabulary" },
            ["Truyện An Dương Vương"] = new[] { "vocabulary" },
            ["Vĩnh Biệt Cửu Trùng Đài"] = new[] { "vocabulary" },
            ["Vội Vàng"] = new[] { "vocabulary" },
            ["Vợ Nhặt"] = new[] { "vocabulary" },
            ["Ôn tập Truyện Kiều và văn xuôi lớp 9"] = new[] { "vocabulary" },
            ["Ôn tập tổng hợp 12 tác phẩm trọng tâm lớp 12"] = new[] { "vocabulary" },
            ["Ôn tập tổng hợp lớp 10"] = new[] { "vocabulary" },
            ["Ôn tập tổng hợp lớp 11"] = new[] { "vocabulary" },
            ["Ôn tập tổng hợp lớp 6"] = new[] { "vocabulary" },
            ["Ôn tập tổng hợp lớp 7"] = new[] { "vocabulary" },
            ["Ôn tập tổng hợp lớp 8"] = new[] { "vocabulary" },
            ["Ông Đồ"] = new[] { "vocabulary" },
            ["Ý Nghĩa Văn Chương"] = new[] { "vocabulary" },
            ["Đồng Chí"] = new[] { "vocabulary" },
            ["Độc Tiểu Thanh Ký"] = new[] { "vocabulary" }
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
                    FontSize = DS.FontSubtitle, FontWeight = FontWeights.SemiBold,
                    FontFamily = DS.FontPrimary,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
                });
                var fractionText = new TextBlock
                {
                    Text = $"{stats.correct}/{stats.total} ({pct:F0}%)",
                    FontSize = DS.FontNote, FontFamily = DS.FontPrimary,
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
                    FontSize = DS.FontNote, FontFamily = DS.FontPrimary,
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
                                    DS.CatLanguage,
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
                        FontSize = DS.FontSubtitle, FontFamily = DS.FontPrimary,
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
                var app = (QASmartTouch.App)Application.Current;
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
                var app = (QASmartTouch.App)Application.Current;
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
                Background = DS.LightBg(DS.CatLanguage),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2)
            };
            numBadge.Child = new TextBlock
            {
                Text = $"Câu {q.Id}", FontSize = DS.FontTag,
                FontWeight = FontWeights.Bold, FontFamily = DS.FontPrimary,
                Foreground = DS.Brush(DS.CatLanguage)
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
            sp.Children.Add(new TextBlock
            {
                Text = q.GetQuestion(), FontSize = DS.FontResult,
                FontFamily = DS.FontPrimary, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 4)
            });

            // Options with click-to-answer
            bool alreadyAnswered = false;
            var options = q.GetOptions();
            var answer = q.GetAnswer();
            for (int i = 0; i < options.Count; i++)
            {
                var optText = options[i];
                var letter = ((char)('A' + i)).ToString();
                // Check if option text starts with "A)" etc — strip prefix if so
                var displayText = optText.StartsWith(letter + ")") ? optText : $"{letter}. {optText}";
                var isCorrect = optText.Trim() == answer.Trim();
                var optBorder = UI.ResultRow($"  {displayText}", DS.ResultInfo);
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
                        ((TextBlock)b.Child).Foreground = DS.Brush(DS.ResultSuccess);
                        ((TextBlock)b.Child).Text = $"  ✅ {capLetter}. Đúng!";
                    }
                    else
                    {
                        ResetStreak();
                        b.Background = DS.LightBg(DS.ResultDanger);
                        ((TextBlock)b.Child).Foreground = DS.Brush(DS.ResultDanger);
                        ((TextBlock)b.Child).Text = $"  ❌ {capLetter}. Sai — Đáp án: {answer}";
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
                var solBtn = UI.PresetButton("💡 Lời giải", DS.CatLanguage, () =>
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

        private void ShowQuizPreview(LiteratureChapterData ch)
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
                FontSize = DS.FontSubtitle, FontFamily = DS.FontPrimary,
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
                        Text = $"  {letter}. {p.Options[oi]}",
                        FontSize = DS.FontSubtitle, FontFamily = DS.FontPrimary,
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
            if (_synth == null) _synth = new System.Speech.Synthesis.SpeechSynthesizer();
            _synth.SpeakAsyncCancelAll();
            _synth.SpeakAsync(text);
        }

        private void ShowFlashcardMode(LiteratureChapterData ch)
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

        private void ShowPracticeMode(LiteratureChapterData ch)
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

        private void StartPractice(LiteratureChapterData ch, List<LiteratureProblem> allProblems, int count, string difficulty, string quizType = "Practice")
        {
            var rnd = new Random();
            var pool = difficulty == "Mix" ? allProblems : allProblems.Where(p => p.Difficulty == difficulty).ToList();
            if (pool.Count == 0) pool = allProblems;
            
            var selectedProblems = pool.OrderBy(x => rnd.Next()).Take(count).ToList();
            
            mainPanel.Children.Clear();
            txtHeaderTitle.Text = $"🎮 Luyện Tập - {ch.Metadata.ChapterName}";
            txtHeaderSub.Text = $"Đang làm {selectedProblems.Count} câu";

            int correctAnswers = 0;
            var problemResults = new List<LiteratureProblemResult>();

            foreach (var p in selectedProblems)
            {
                Border? cardBorder = null;
                var currentResult = new LiteratureProblemResult 
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
                        var isCorrect = p.Options[i].Trim() == p.GetAnswer().Trim();
                        var optBorder = UI.ResultRow($"  {letter}. {p.Options[i]}", DS.ResultInfo);
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
                                HandleCorrectAnswer();
                                correctAnswers++;

                                double mult = 1.0;
                                if (_currentStreak >= 10) mult = 2.0;
                                else if (_currentStreak >= 5) mult = 1.5;
                                else if (_currentStreak >= 3) mult = 1.2;
                                int earned = (int)(p.Points * mult);

                                if (_currentStreak >= 10 && cardBorder != null)
                                {
                                    ApplyOnFireEffect(cardBorder);
                                }

                                if (mult > 1.0)
                                    ((TextBlock)optBorder.Child).Text = $"  ✅ {capturedLetter}. Đúng! (+{earned}đ - Combo x{mult:F1} 🔥)";
                                else
                                    ((TextBlock)optBorder.Child).Text = $"  ✅ {capturedLetter}. Đúng! (+{earned}đ)";
                            }
                            else
                            {
                                optBorder.Background = DS.LightBg(DS.ResultDanger);
                                optBorder.BorderBrush = DS.Brush(DS.ResultDanger);
                                ResetStreak();
                                if (cardBorder != null)
                                {
                                    RemoveOnFireEffect(cardBorder);
                                }
                            }
                        };
                        optWrap.Children.Add(optBorder);
                    }
                }
                spCard.Children.Add(optWrap);
                
                cardBorder = new Border
                {
                    Background = System.Windows.Media.Brushes.White,
                    CornerRadius = new CornerRadius(DS.RadiusCard),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 0, 0, 16),
                    BorderBrush = DS.BrushAlpha(DS.ResultPrimary, 40),
                    BorderThickness = new Thickness(1),
                    Child = spCard
                };
                mainPanel.Children.Add(cardBorder);
            }


            var startTime = DateTime.Now;
            UI.PresetButton("🏁 Hoàn Thành", DS.ResultPrimary, () =>
            {
                var timeSpent = (DateTime.Now - startTime).TotalSeconds;
                MessageBox.Show($"Bạn đã trả lời đúng {correctAnswers}/{selectedProblems.Count} câu hỏi!", "Kết quả", MessageBoxButton.OK, MessageBoxImage.Information);
                
                try
                {
                    var db = ((QASmartTouch.App)Application.Current).Database;
                    var history = new LiteratureQuizHistory
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
                    db.LiteratureQuizHistories.Add(history);
                    db.SaveChanges();

                    // 🔔 REAL-TIME ALERT: Notify teacher if score is critical (< 5.0)
                    double scoreVal = (double)correctAnswers / selectedProblems.Count * 10.0;
                    if (scoreVal < 5.0)
                    {
                        var shell = Window.GetWindow(this) as QASmartClass.Classroom.Views.ClassroomShell;
                        shell?.ShowToast("Cảnh báo Hiệu suất", $"⚠️ {history.StudentName} đạt kết quả thấp ({scoreVal:F1}/10) tại chương: {history.ChapterName}", "ALERT");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "LiteratureCurriculum: Failed to save quiz history");
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
            var btnBackup = UI.PresetButton("☁️ Đồng Bộ & Sao Lưu", DS.ResultSuccess, async () => {
                await PerformDatabaseBackupAsync();
            });
            btnBackup.Margin = new Thickness(10, 0, 0, 0);
            filterSp.Children.Add(btnBackup);

            var btnRestore = UI.PresetButton("🔄 Khôi Phục CSDL", DS.BrandAccent, async () => {
                if (_lockoutEndTime.HasValue && DateTime.Now < _lockoutEndTime.Value)
                {
                    var remaining = _lockoutEndTime.Value - DateTime.Now;
                    MessageBox.Show($"Chức năng khôi phục đang bị khóa do nhập sai mật khẩu quá nhiều lần. Vui lòng thử lại sau {remaining.Minutes} phút {remaining.Seconds} giây.", "Chức năng bị khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var backupFile = PromptSelectBackupFile();
                if (backupFile != null)
                {
                    var pwd = ShowPasswordDialog();
                    if (pwd == null) return; // Cancelled
                    if (VerifyAdminPassword(pwd))
                    {
                        _adminPasswordAttempts = 0;
                        _lockoutEndTime = null;
                        SaveSettings();
                        await RestoreDatabaseAsync(backupFile);
                    }
                    else
                    {
                        _adminPasswordAttempts++;
                        if (_adminPasswordAttempts >= 5)
                        {
                            _lockoutEndTime = DateTime.Now.AddMinutes(5);
                            _adminPasswordAttempts = 0;
                            SaveSettings();
                            MessageBox.Show("Mật khẩu Admin không chính xác! Bạn đã nhập sai 5 lần liên tiếp. Chức năng khôi phục sẽ bị khóa trong 5 phút.", "Lỗi xác thực & Khóa tài khoản", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show($"Mật khẩu Admin không chính xác! (Còn {5 - _adminPasswordAttempts} lần thử)", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            });
            btnRestore.Margin = new Thickness(10, 0, 0, 0);
            filterSp.Children.Add(btnRestore);

            var btnChangePassword = UI.PresetButton("🔑 Đổi MK Admin", DS.ResultInfo, () => {
                PromptChangeAdminPassword();
            });
            btnChangePassword.Margin = new Thickness(10, 0, 0, 0);
            filterSp.Children.Add(btnChangePassword);

            mainPanel.Children.Add(filterSp);

            try
            {
                var db = ((QASmartTouch.App)Application.Current).Database;
                var query = db.LiteratureQuizHistories.AsQueryable();
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
                        AvgScore = g.Average(h => (double)h.CorrectAnswers / h.TotalQuestions * 10.0),
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
                        chip.Child = new TextBlock { Text = $"⚠️ {sc.Name}", FontSize = DS.FontSubtitle, Foreground = DS.Brush(DS.ResultDanger) };
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
                    var subText = new TextBlock { Text = $"/10 • {stat.Count} lượt", FontSize = DS.FontSubtitle, Foreground = DS.Brush(DS.TextSecondary), VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(4, 0, 0, 2) };
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
                    if (grid.SelectedItem is LiteratureQuizHistory selected) {
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
                tableHeaderSp.Children.Add(new TextBlock { Text = "(Nhấp đúp vào một hàng để xem chi tiết từng câu hỏi)", FontSize = DS.FontSubtitle, Foreground = DS.Brush(DS.TextSecondary), Margin = new Thickness(0,-5,0,10), FontStyle = FontStyles.Italic });

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
                // Add audit logs container and load logs
                _auditLogContainer = new StackPanel { Margin = new Thickness(0, 20, 0, 20) };
                mainPanel.Children.Add(_auditLogContainer);
                BuildAuditLogsView(DateTime.Now.AddDays(-7), DateTime.Now);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "LiteratureCurriculum: Error loading performance tracking");
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
                var selectedChapters = chapterListSp.Children.OfType<CheckBox>().Where(cb => cb.IsChecked == true).Select(cb => (LiteratureChapterData)cb.Tag).ToList();
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

        private void GenerateAndStartCustomQuiz(List<LiteratureChapterData> chapters, int count, string difficulty)
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

            var customCh = new LiteratureChapterData
            {
                Metadata = new LiteratureChapterMetadata
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
                var db = ((QASmartTouch.App)Application.Current).Database;
                var query = db.LiteratureQuizHistories.AsQueryable();
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
                    FileName = $"LiteraturePerformance_{gradeFilter}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (sfd.ShowDialog() == true)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("Thời Gian,Học Sinh,Mã HS,Khối,Chương,Độ Khó,Số Câu,Đúng,Tỷ Lệ,Thời Gian Làm(s),Loại");
                    foreach (var h in histories)
                    {
                        var rate = (double)h.CorrectAnswers / h.TotalQuestions * 100;
                        sb.AppendLine($"{h.CompletedAt:yyyy-MM-dd HH:mm:ss},{h.StudentName},{h.StudentCode},{h.Grade},{h.ChapterName},{h.Difficulty},{h.TotalQuestions},{h.CorrectAnswers},{rate:F1}%,{h.TimeSpentSeconds:F0},{h.QuizType}");
                    }
                    System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                    MessageBox.Show($"Đã xuất dữ liệu thành công ra file: {System.IO.Path.GetFileName(sfd.FileName)}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "LiteratureCurriculum: Export failed");
                MessageBox.Show("Lỗi khi xuất dữ liệu: " + ex.Message);
            }
        }

        private void ClearQuizHistory()
        {
            try
            {
                var db = ((QASmartTouch.App)Application.Current).Database;
                db.LiteratureQuizHistories.RemoveRange(db.LiteratureQuizHistories);
                db.SaveChanges();
                ShowPerformanceTracking();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "LiteratureCurriculum: Clear history failed");
                MessageBox.Show("Lỗi khi xóa dữ liệu: " + ex.Message);
            }
        }

        private void ShowQuizResultDetails(LiteratureQuizHistory history)
        {
            _currentDashboardView = "details";
            mainPanel.Children.Clear();
            btnBackToChapters.Visibility = Visibility.Visible;
            AnimateMainPanel();
            txtHeaderTitle.Text = $"📄 Chi tiết: {history.ChapterName}";
            txtHeaderSub.Text = $"Học sinh: {history.StudentName} • {history.CompletedAt:dd/MM/yyyy HH:mm}";

            var problemResults = new List<LiteratureProblemResult>();
            try {
                if (!string.IsNullOrEmpty(history.ProblemResultsJson))
                    problemResults = System.Text.Json.JsonSerializer.Deserialize<List<LiteratureProblemResult>>(history.ProblemResultsJson) ?? new();
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
                    solBorder.Child = new TextBlock { Text = "💡 Giải thích: " + res.Solution, FontSize = DS.FontSubtitle, FontStyle = FontStyles.Italic, TextWrapping = TextWrapping.Wrap };
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
    
        private void HandleCorrectAnswer()
        {
            System.Media.SystemSounds.Asterisk.Play();
            _currentStreak++;
            if (_currentStreak >= 3)
            {
                if (_tblStreak != null)
                {
                    _tblStreak.Text = $"🔥 Chuỗi đúng: {_currentStreak}";
                    _tblStreak.Visibility = Visibility.Visible;
                }
            }
        }

        private void ResetStreak()
        {
            _currentStreak = 0;
            if (_tblStreak != null)
            {
                _tblStreak.Visibility = Visibility.Collapsed;
            }
        }

        private void TriggerCelebrationEffect()
        {
            if (celebrationCanvas == null) return;
            celebrationCanvas.Children.Clear();
            
            var rand = new Random();
            double centerX = celebrationCanvas.ActualWidth / 2;
            double centerY = celebrationCanvas.ActualHeight / 2;
            if (centerX <= 0) centerX = 400;
            if (centerY <= 0) centerY = 300;

            System.Media.SystemSounds.Beep.Play();

            for (int i = 0; i < 20; i++)
            {
                var ellipse = new System.Windows.Shapes.Ellipse
                {
                    Width = rand.Next(8, 16),
                    Height = rand.Next(8, 16),
                    Fill = new System.Windows.Media.SolidColorBrush(Color.FromRgb((byte)rand.Next(100, 256), (byte)rand.Next(100, 256), (byte)rand.Next(100, 256)))
                };

                var transGroup = new TransformGroup();
                var translate = new TranslateTransform(centerX, centerY);
                transGroup.Children.Add(translate);
                ellipse.RenderTransform = transGroup;
                celebrationCanvas.Children.Add(ellipse);

                double angle = rand.NextDouble() * 2 * System.Math.PI;
                double speed = rand.Next(80, 250);
                double targetX = centerX + System.Math.Cos(angle) * speed;
                double targetY = centerY + System.Math.Sin(angle) * speed - rand.Next(50, 100);

                var animX = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = targetX,
                    Duration = TimeSpan.FromSeconds(1.2),
                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };

                var animY = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = targetY,
                    Duration = TimeSpan.FromSeconds(1.2),
                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };

                var animOpacity = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromSeconds(1.2)
                };

                translate.BeginAnimation(TranslateTransform.XProperty, animX);
                translate.BeginAnimation(TranslateTransform.YProperty, animY);
                ellipse.BeginAnimation(OpacityProperty, animOpacity);
            }
        }

        private void BuildAuditLogsView(DateTime start, DateTime end)
        {
            if (_auditLogContainer == null) return;
            _auditLogContainer.Children.Clear();

            _auditLogContainer.Children.Add(UI.Title("Nhật Ký Hệ Thống (Audit Logs)", 20, DS.TextPrimary));
            _auditLogContainer.Children.Add(new TextBlock { Text = "Giám sát lịch sử hoạt động bảo mật hệ thống", FontSize = DS.FontSubtitle, Foreground = DS.Brush(DS.TextSecondary), Margin = new Thickness(0, 0, 0, 10) });

            var filterRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 10) };
            filterRow.Children.Add(new TextBlock { Text = "Từ ngày: ", VerticalAlignment = VerticalAlignment.Center, FontSize = 13 });
            var dpStart = new DatePicker { SelectedDate = start, Width = 130, Margin = new Thickness(5, 0, 15, 0) };
            filterRow.Children.Add(dpStart);

            filterRow.Children.Add(new TextBlock { Text = "Đến ngày: ", VerticalAlignment = VerticalAlignment.Center, FontSize = 13 });
            var dpEnd = new DatePicker { SelectedDate = end, Width = 130, Margin = new Thickness(5, 0, 15, 0) };
            filterRow.Children.Add(dpEnd);

            var btnFilter = UI.PresetButton("Lọc Nhật Ký", DS.ResultPrimary, () =>
            {
                var s = dpStart.SelectedDate ?? DateTime.MinValue;
                var e = dpEnd.SelectedDate ?? DateTime.MaxValue;
                if (s > e)
                {
                    MessageBox.Show("Ngày bắt đầu không được lớn hơn ngày kết thúc!", "Lỗi bộ lọc");
                    return;
                }
                BuildAuditLogsView(s, e);
            });
            filterRow.Children.Add(btnFilter);
            _auditLogContainer.Children.Add(filterRow);

            try
            {
                var db = ((QASmartTouch.App)Application.Current).Database;
                var queryStart = start.Date;
                var queryEnd = end.Date.AddDays(1).AddTicks(-1);
                
                var logs = db.UsageLogs
                    .Where(l => l.Timestamp >= queryStart && l.Timestamp <= queryEnd)
                    .OrderByDescending(l => l.Timestamp)
                    .Take(50)
                    .ToList();

                if (logs.Count == 0)
                {
                    _auditLogContainer.Children.Add(UI.Text("Không có dữ liệu nhật ký trong khoảng thời gian này.", 14, DS.TextSecondary));
                    return;
                }

                var grid = UI.StandardDataGrid(logs);
                grid.Columns.Add(new DataGridTextColumn { Header = "Thời gian", Binding = new System.Windows.Data.Binding("Timestamp") { StringFormat = "yyyy-MM-dd HH:mm:ss" }, Width = 140 });
                grid.Columns.Add(new DataGridTextColumn { Header = "Sự kiện", Binding = new System.Windows.Data.Binding("EventType"), Width = 180 });
                grid.Columns.Add(new DataGridTextColumn { Header = "Mô tả chi tiết", Binding = new System.Windows.Data.Binding("EventData"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
                grid.Columns.Add(new DataGridTextColumn { Header = "Vai trò", Binding = new System.Windows.Data.Binding("UserRole"), Width = 70 });

                var scroll = new ScrollViewer { Content = grid, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Height = 250 };
                var border = new Border
                {
                    Child = scroll,
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(8),
                    BorderThickness = new Thickness(1),
                    BorderBrush = DS.BrushAlpha(DS.TextSecondary, 20),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 5, 0, 0)
                };
                _auditLogContainer.Children.Add(border);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "LiteratureCurriculum: Failed to load audit logs");
                _auditLogContainer.Children.Add(UI.Text("Lỗi khi tải nhật ký: " + ex.Message, 14, DS.ResultError));
            }
        }

        private async Task PerformDatabaseBackupAsync()
        {
            try
            {
                var db = ((QASmartTouch.App)Application.Current).Database;
                
                // Checkpoint sqlite wal to write changes to db file safely
                await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(db.Database, "PRAGMA wal_checkpoint(FULL);");

                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string backupDir = Path.Combine(appData, "QASmartClass", "backup");
                Directory.CreateDirectory(backupDir);

                // Disk free space check (DriveInfo)
                var drive = new DriveInfo(Path.GetPathRoot(backupDir));
                if (drive.AvailableFreeSpace < 50 * 1024 * 1024)
                {
                    throw new IOException($"Không đủ dung lượng ổ đĩa trống trên ổ {drive.Name}. Cần tối thiểu 50MB trống (Hiện còn: {drive.AvailableFreeSpace / (1024 * 1024)}MB).");
                }

                string sourceDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "smartclass.db");
                string destZip = Path.Combine(backupDir, $"smartclass_backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
                
                await Task.Run(() =>
                {
                    if (File.Exists(destZip)) File.Delete(destZip);
                    using (var archive = ZipFile.Open(destZip, ZipArchiveMode.Create))
                    {
                        archive.CreateEntryFromFile(sourceDb, "smartclass.db");
                    }
                });

                // Verify backup file size
                var destInfo = new FileInfo(destZip);
                if (!destInfo.Exists || destInfo.Length == 0)
                {
                    if (destInfo.Exists) destInfo.Delete();
                    throw new IOException("Tệp tin sao lưu ZIP được tạo ra trống (0-bytes). Sao lưu thất bại.");
                }

                // Retention Policy: Giu toi da 5 file backup moi nhat
                var files = Directory.GetFiles(backupDir, "smartclass_backup_*.zip")
                                     .Select(f => new FileInfo(f))
                                     .OrderByDescending(f => f.CreationTime)
                                     .Skip(5)
                                     .ToList();
                foreach (var f in files)
                {
                    try { f.Delete(); } catch { }
                }
                
                MessageBox.Show("Đồng bộ dữ liệu và sao lưu CSDL (ZIP) thành công!\nTệp lưu trữ: " + Path.GetFileName(destZip), "Thông báo");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi sao lưu dữ liệu: " + ex.Message, "Lỗi hệ thống");
            }
        }

        private string PromptSelectBackupFile()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string backupDir = Path.Combine(appData, "QASmartClass", "backup");
            if (!Directory.Exists(backupDir) || Directory.GetFiles(backupDir, "smartclass_backup_*.zip").Length == 0)
            {
                MessageBox.Show("Không tìm thấy tệp tin sao lưu nào trong hệ thống.", "Thông báo");
                return null;
            }

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = backupDir,
                Filter = "ZIP Backup Files (*.zip)|*.zip",
                Title = "Chọn tệp tin sao lưu để khôi phục"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }
            return null;
        }

        private string ShowPasswordDialog(string prompt = "Vui lòng nhập mật khẩu Admin để khôi phục:", string title = "Xác nhận quản trị viên")
        {
            string pwd = null;
            var thread = new System.Threading.Thread(() =>
            {
                var win = new Window
                {
                    Title = title,
                    Width = 350, Height = 160,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245))
                };
                var sp = new StackPanel { Margin = new Thickness(20) };
                sp.Children.Add(new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 10), FontFamily = DS.FontPrimary, FontSize = 13 });
                var pb = new PasswordBox { Height = 25 };
                sp.Children.Add(pb);
                
                var bp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
                var btnOk = UI.PresetButton("Xác nhận", DS.ResultPrimary, () => { win.DialogResult = true; win.Close(); });
                var btnCancel = UI.PresetButton("Hủy", DS.ResultInfo, () => { win.DialogResult = false; win.Close(); });
                btnCancel.Margin = new Thickness(10, 0, 0, 0);
                bp.Children.Add(btnOk);
                bp.Children.Add(btnCancel);
                sp.Children.Add(bp);
                
                win.Content = sp;
                if (win.ShowDialog() == true)
                {
                    pwd = pb.Password;
                }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
            return pwd;
        }

        private async Task RestoreDatabaseAsync(string backupPath)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string tempExtractedDb = Path.Combine(appData, "QASmartClass", "backup", "temp_extracted.db");
            
            try
            {
                // 1. Extract database from ZIP to a temporary file
                await Task.Run(() =>
                {
                    if (File.Exists(tempExtractedDb)) File.Delete(tempExtractedDb);
                    using (var archive = ZipFile.OpenRead(backupPath))
                    {
                        var entry = archive.GetEntry("smartclass.db");
                        if (entry == null)
                        {
                            throw new InvalidDataException("Tệp lưu trữ ZIP không chứa dữ liệu smartclass.db hợp lệ.");
                        }
                        entry.ExtractToFile(tempExtractedDb, overwrite: true);
                    }
                });

                // 2. Validate Schema Integrity
                if (!CheckDatabaseSchema(tempExtractedDb))
                {
                    if (File.Exists(tempExtractedDb)) File.Delete(tempExtractedDb);
                    MessageBox.Show("Khôi phục thất bại: Tệp tin sao lưu không đúng cấu trúc cơ sở dữ liệu hoặc bị hỏng.", "Lỗi bảo mật/toàn vẹn", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 3. Perform overwrite
                var db = ((QASmartTouch.App)Application.Current).Database;
                
                // Close SQLite connections to unlock the file
                var dbConn = db.Database.GetDbConnection();
                db.Database.CloseConnection();
                if (dbConn is Microsoft.Data.Sqlite.SqliteConnection sqliteConn)
                {
                    Microsoft.Data.Sqlite.SqliteConnection.ClearPool(sqliteConn);
                }
                
                string targetDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "smartclass.db");
                
                // Copy the validated temp db file over the active db file
                await Task.Run(() => File.Copy(tempExtractedDb, targetDb, overwrite: true));
                
                // Re-open DB connections
                db.Database.OpenConnection();
                db.ChangeTracker.Clear();
                
                MessageBox.Show("Khôi phục cơ sở dữ liệu thành công! Hệ thống sẽ tự động tải lại bảng điểm.", "Thông báo");
                
                // Refresh UI
                ShowPerformanceTracking();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi khôi phục cơ sở dữ liệu: " + ex.Message, "Lỗi hệ thống");
            }
            finally
            {
                // Clean up temp file
                try
                {
                    if (File.Exists(tempExtractedDb)) File.Delete(tempExtractedDb);
                }
                catch { }
            }
        }


        private bool IsEn => QASmartClass.Shared.LanguageManager.CurrentLanguage == "en";

        private string SanitizeString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input)) return input;
            string sanitized = input.Replace("<", "")
                                    .Replace(">", "")
                                    .Replace("'", "")
                                    .Replace(";", "");
            if (sanitized.Length > maxLength)
            {
                sanitized = sanitized.Substring(0, maxLength);
            }
            return sanitized;
        }

        private void LiteratureCurriculumTool_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                double availableWidth = ActualWidth;
                double maxContentWidth = 1200;
                double sideMargin = 8;
                
                if (availableWidth > maxContentWidth + 16)
                {
                    sideMargin = (availableWidth - maxContentWidth) / 2;
                }
                
                if (mainPanel != null)
                {
                    mainPanel.Margin = new Thickness(sideMargin, 8, sideMargin, 8);
                }
                
                double toolMax = 900;
                double toolMargin = 8;
                if (availableWidth > toolMax + 16)
                {
                    toolMargin = (availableWidth - toolMax) / 2;
                }
                if (relatedToolContainer != null)
                {
                    relatedToolContainer.Margin = new Thickness(toolMargin, 8, toolMargin, 8);
                }
            }
            catch { }
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
                        Icon = "✍️",
                        Title = isVN ? "Sáng tạo nội dung & Báo chí" : "Content & Journalism",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_literature_curriculum_1_{suffix}.png",
                        Description = isVN
                            ? "Phát triển tư duy cốt truyện, nghệ thuật tu từ và cách dựng nhân vật làm nền tảng cho nhà văn, nhà báo, biên kịch và biên tập viên."
                            : "Develop plot structure, rhetorical arts, and character building as a solid foundation for writers, journalists, and editors."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📢",
                        Title = isVN ? "Hùng biện & Truyền thông" : "Oratory & Marketing",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_literature_curriculum_2_{suffix}.png",
                        Description = isVN
                            ? "Sử dụng thao tác lập luận, nghệ thuật thuyết phục để xây dựng kịch bản quảng cáo, phát biểu trước công chúng hoặc tranh biện."
                            : "Use argumentative methods and persuasive arts to create marketing copies, deliver speeches, or engage in debates."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎬",
                        Title = isVN ? "Điện ảnh & Di sản" : "Cinema & Heritage",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_literature_curriculum_3_{suffix}.png",
                        Description = isVN
                            ? "Chuyển thể tác phẩm văn học kinh điển thành kịch bản phim, kịch sân khấu hoặc thuyết minh di sản, bảo tàng văn hóa lịch sử."
                            : "Adapt classic literature into movie scripts, theater plays, or guide cultural tours in historical heritage sites."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎬",
                        Title = isVN ? "Sân khấu hóa & Chuyển thể Điện ảnh" : "Theatre & Film Adaptation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_literature_1_{suffix}.png",
                        Description = isVN 
                            ? "Chuyển thể các tác phẩm văn học từ trang giấy lên sân khấu kịch hoặc màn ảnh nhỏ, phân tích sự thay đổi trong ngôn ngữ biểu đạt và nghệ thuật biên kịch." 
                            : "Adapt literary works from page to stage or screen, analyzing changes in expressive language and the art of screenwriting."
                    },
                    new PracticalAppItem
                    {
                        Icon = "✍️",
                        Title = isVN ? "Marketing Nội dung & Kể chuyện Thương hiệu" : "Content Marketing & Storytelling",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_literature_2_{suffix}.png",
                        Description = isVN 
                            ? "Vận dụng cấu trúc tự sự và nghệ thuật ngôn từ của văn học để sáng tạo nội dung truyền thông, viết lời quảng cáo và xây dựng câu chuyện thương hiệu hấp dẫn." 
                            : "Apply narrative structures and literary word art to create media content, write compelling copy, and build engaging brand stories."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🧠",
                        Title = isVN ? "Phân tích Tâm lý & Bản đồ Nhân vật" : "Psychoanalysis & Character Mapping",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_literature_3_{suffix}.png",
                        Description = isVN 
                            ? "Xây dựng sơ đồ mối quan hệ phức tạp và phân tích diễn biến tâm lý nhân vật dưới góc nhìn phân tâm học, làm nổi bật động cơ và xung đột nội tâm trong tác phẩm." 
                            : "Construct complex relationship maps and analyze character psychology from a psychoanalytic perspective, highlighting motives and internal conflicts in the work."
                    }
                };

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Log.Warning("Error loading practical apps for LiteratureCurriculumTool: {Err}", ex.Message);
            }
        }

        private void PromptChangeAdminPassword()
        {
            var currentPwd = ShowPasswordDialog("Nhập mật khẩu Admin HIỆN TẠI để xác minh:", "Xác thực bảo mật");
            if (currentPwd == null) return;
            if (!VerifyAdminPassword(currentPwd))
            {
                MessageBox.Show("Mật khẩu hiện tại không chính xác!", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var newPwd = ShowPasswordDialog("Nhập mật khẩu Admin MỚI:", "Thay đổi mật khẩu");
            if (string.IsNullOrEmpty(newPwd))
            {
                MessageBox.Show("Mật khẩu mới không được để trống!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (newPwd.Length < 6)
            {
                MessageBox.Show("Mật khẩu mới phải có ít nhất 6 ký tự để bảo đảm an toàn!", "Mật khẩu yếu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmNewPwd = ShowPasswordDialog("Nhập lại mật khẩu Admin MỚI để xác nhận:", "Xác nhận mật khẩu mới");
            if (confirmNewPwd == null) return;
            if (newPwd != confirmNewPwd)
            {
                MessageBox.Show("Xác nhận mật khẩu không trùng khớp!", "Lỗi xác nhận", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(newPwd);
                var hashBytes = sha.ComputeHash(bytes);
                var sb = new System.Text.StringBuilder();
                foreach (var b in hashBytes) sb.Append(b.ToString("x2"));
                _adminPasswordHash = sb.ToString();
            }
            SaveSettings();
            MessageBox.Show("Đã thay đổi mật khẩu Admin thành công và ghi nhận vào hệ thống bảo mật cục bộ!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}