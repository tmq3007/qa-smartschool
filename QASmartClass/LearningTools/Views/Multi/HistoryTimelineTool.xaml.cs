using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.IO;
using System.Text.Json;

namespace QASmartClass.LearningTools.Views.Multi
{
    public partial class HistoryTimelineTool : BaseToolControl, IDisposable
    {
        public class TimelineEvent
        {
            public int Year { get; set; }
            public string Title { get; set; } = "";
            public string Description { get; set; } = "";
            public string Category { get; set; } = "vietnam";
            public string Detail { get; set; } = "";
            public string Figures { get; set; } = "";
            public string Significance { get; set; } = "";
            public bool IsCustom { get; set; } = false;
        }

        private readonly List<TimelineEvent> _allEvents = new();
        private string _currentFilter = "all";

        private static readonly Dictionary<string, (Color Bg, Color Border, Color Text)> CategoryColors = new()
        {
            ["vietnam"] = (Color.FromRgb(255, 243, 224), Color.FromRgb(255, 183, 77), Color.FromRgb(230, 81, 0)),
            ["world"]   = (Color.FromRgb(227, 242, 253), Color.FromRgb(144, 202, 249), Color.FromRgb(21, 101, 192)),
            ["science"] = (Color.FromRgb(232, 245, 233), Color.FromRgb(165, 214, 167), Color.FromRgb(46, 125, 50)),
            ["culture"] = (Color.FromRgb(252, 228, 236), Color.FromRgb(239, 154, 154), Color.FromRgb(198, 40, 40)),
        };

        private static readonly Dictionary<string, string> CategoryIcons = new()
        {
            ["vietnam"] = "⭐",
            ["world"]   = "🌍",
            ["science"] = "🔬",
            ["culture"] = "🎨",
        };

        private static readonly Brush SelectedBrushAll = new SolidColorBrush(Color.FromRgb(26, 35, 126)); // #1A237E
        private static readonly Brush UnselectedBrushAll = new SolidColorBrush(Color.FromRgb(232, 234, 246)); // #E8EAF6
        private static readonly Brush SelectedBrushVN = new SolidColorBrush(Color.FromRgb(230, 81, 0)); // #E65100
        private static readonly Brush UnselectedBrushVN = new SolidColorBrush(Color.FromRgb(255, 243, 224)); // #FFF3E0
        private static readonly Brush SelectedBrushWorld = new SolidColorBrush(Color.FromRgb(21, 101, 192)); // #1565C0
        private static readonly Brush UnselectedBrushWorld = new SolidColorBrush(Color.FromRgb(227, 242, 253)); // #E3F2FD
        private static readonly Brush SelectedBrushScience = new SolidColorBrush(Color.FromRgb(46, 125, 50)); // #2E7D32
        private static readonly Brush UnselectedBrushScience = new SolidColorBrush(Color.FromRgb(232, 245, 233)); // #E8F5E9
        private static readonly Brush SelectedBrushCulture = new SolidColorBrush(Color.FromRgb(198, 40, 40)); // #C62828
        private static readonly Brush UnselectedBrushCulture = new SolidColorBrush(Color.FromRgb(252, 228, 236)); // #FCE4EC

        private static readonly string CustomEventsFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "QASmartClass", "custom_timeline_events.json");

        public HistoryTimelineTool()
        {
            InitializeComponent();
            LoadSampleEvents();
            LoadCustomEvents();
            Loaded += (_, _) => {
                RenderTimeline();
                UpdateFilterButtonStyles();
                TouchTextPad.Attach(txtSearch, mode: "text");
                TouchNumPad.Attach(txtYear, step: 1, allowDecimal: false, allowNegative: true);
                TouchTextPad.Attach(txtTitle, mode: "text");
                TouchTextPad.Attach(txtDescription, mode: "text");
                TouchTextPad.Attach(txtDetail, mode: "text");
                TouchTextPad.Attach(txtFigures, mode: "text");
                TouchTextPad.Attach(txtSignificance, mode: "text");
                LoadPracticalApps();

                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Ghi chú" : "Study Guide & Notes";
                if (menuTextTimeline != null) menuTextTimeline.Text = isVN ? "Tiến trình lịch sử" : "Historical Timeline";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                if (sideMenu != null)
                {
                    sideMenu.SelectedIndex = 0; // Default to timeline workspace
                }
            };
        }

                private void LoadSampleEvents()
        {
            try
            {
                Uri? resourceUri = null;
                try
                {
                    _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
                    resourceUri = new Uri("pack://application:,,,/QASmartClass;component/Assets/Data/DefaultTimelineEvents.json");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to create pack Uri: {ex.Message}");
                }

                System.Windows.Resources.StreamResourceInfo? resourceStream = null;
                if (resourceUri != null)
                {
                    try
                    {
                        resourceStream = Application.GetResourceStream(resourceUri);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Application.GetResourceStream failed, will fallback to filesystem: {ex.Message}");
                    }
                }

                string? json = null;
                if (resourceStream != null)
                {
                    using (var reader = new StreamReader(resourceStream.Stream))
                    {
                        json = reader.ReadToEnd();
                    }
                }
                else
                {
                    // Fallback to filesystem for unit test environments
                    string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                    for (int i = 0; i < 5; i++)
                    {
                        string testPath = System.IO.Path.Combine(currentDir, "Assets", "Data", "DefaultTimelineEvents.json");
                        if (File.Exists(testPath))
                        {
                            json = File.ReadAllText(testPath);
                            break;
                        }
                        string testPath2 = System.IO.Path.Combine(currentDir, "QASmartClass", "Assets", "Data", "DefaultTimelineEvents.json");
                        if (File.Exists(testPath2))
                        {
                            json = File.ReadAllText(testPath2);
                            break;
                        }
                        var parent = Directory.GetParent(currentDir);
                        if (parent == null) break;
                        currentDir = parent.FullName;
                    }
                }

                if (!string.IsNullOrEmpty(json))
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var events = JsonSerializer.Deserialize<List<TimelineEvent>>(json, options);
                    if (events != null)
                    {
                        _allEvents.AddRange(events);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Could not load DefaultTimelineEvents.json from resource or filesystem.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load sample events: {ex.Message}");
            }
        }

        private void LoadCustomEvents()
        {
            try
            {
                var customDbEvents = Helpers.DbManager.GetCustomTimelineEvents();
                if (customDbEvents != null)
                {
                    foreach (var ev in customDbEvents)
                    {
                        _allEvents.Add(new TimelineEvent
                        {
                            Year = ev.Year,
                            Title = ev.Title,
                            Description = ev.Description,
                            Category = ev.Category,
                            Detail = ev.Detail,
                            Figures = ev.Figures,
                            Significance = ev.Significance,
                            IsCustom = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load custom events from database: {ex.Message}");
            }
        }

        private void SaveCustomEvents()
        {
            // Deprecated - saving directly to SQLite in BtnSaveEvent_Click
        }

        public static string FormatHistoricalYear(int year)
        {
            if (year < 0)
            {
                return $"{System.Math.Abs(year)} TCN";
            }
            return year.ToString();
        }

        private static bool TryParseHistoricalYear(string input, out int year)
        {
            year = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;

            string normalized = input.Trim().ToLowerInvariant();
            normalized = normalized.Replace("năm", "").Trim();

            bool isBC = false;
            if (normalized.EndsWith("tcn") || normalized.EndsWith("bc") || normalized.Contains("trước công nguyên"))
            {
                isBC = true;
                normalized = normalized.Replace("tcn", "")
                                       .Replace("bc", "")
                                       .Replace("trước công nguyên", "")
                                       .Trim();
            }

            if (int.TryParse(normalized, out int parsedVal))
            {
                if (parsedVal < 0)
                {
                    year = parsedVal;
                }
                else
                {
                    year = isBC ? -parsedVal : parsedVal;
                }
                return true;
            }
            return false;
        }

        private void RenderTimeline()
        {
            TopEventsPanel.Children.Clear();
            BottomEventsPanel.Children.Clear();
            YearMarkersPanel.Children.Clear();

            string keyword = txtSearch?.Text?.Trim().ToLower() ?? "";

            var events = _allEvents.Where(e => 
                (_currentFilter == "all" || e.Category == _currentFilter) &&
                (string.IsNullOrEmpty(keyword) || 
                 e.Title.ToLower().Contains(keyword) || 
                 e.Year.ToString().Contains(keyword) ||
                 FormatHistoricalYear(e.Year).ToLower().Contains(keyword) ||
                 (e.Figures != null && e.Figures.ToLower().Contains(keyword)) ||
                 (e.Description != null && e.Description.ToLower().Contains(keyword)) ||
                 (e.Detail != null && e.Detail.ToLower().Contains(keyword)) ||
                 (e.Significance != null && e.Significance.ToLower().Contains(keyword))
                )
            ).OrderBy(e => e.Year).ToList();

            if (events.Count == 0)
            {
                var noResultTxt = new TextBlock
                {
                    Text = "Không tìm thấy sự kiện nào phù hợp. Vui lòng thử lại!",
                    FontSize = 16,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 100, 0, 0)
                };
                YearMarkersPanel.Children.Add(noResultTxt);
                return;
            }

            double cardWidth = 240;
            double spacing = 30;

            for (int i = 0; i < events.Count; i++)
            {
                var ev = events[i];
                bool isTop = (i % 2 == 0);
                var colors = CategoryColors.ContainsKey(ev.Category) ? CategoryColors[ev.Category] : CategoryColors["vietnam"];
                string icon = CategoryIcons.ContainsKey(ev.Category) ? CategoryIcons[ev.Category] : "📌";

                var card = new StackPanel { Width = cardWidth, Margin = new Thickness(spacing / 2, 0, spacing / 2, 0) };
                var connector = new Border { Width = 3, Height = 30, Background = new SolidColorBrush(colors.Border), HorizontalAlignment = HorizontalAlignment.Center };
                var dot = new Ellipse { Width = 16, Height = 16, Fill = new SolidColorBrush(colors.Border), Stroke = Brushes.White, StrokeThickness = 3, HorizontalAlignment = HorizontalAlignment.Center };

                var cardBorder = new Border
                {
                    Background = new SolidColorBrush(colors.Bg),
                    BorderBrush = new SolidColorBrush(colors.Border),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(14, 12, 14, 12),
                    Cursor = Cursors.Hand
                };

                var cardContent = new StackPanel();
                cardContent.Children.Add(new TextBlock { Text = $"{icon} {FormatHistoricalYear(ev.Year)}", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(colors.Text), Margin = new Thickness(0, 0, 0, 4) });
                cardContent.Children.Add(new TextBlock { Text = ev.Title, FontSize = 18, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4) });
                cardContent.Children.Add(new TextBlock { Text = ev.Description, FontSize = 14, Foreground = Brushes.DimGray, TextWrapping = TextWrapping.Wrap });
                cardBorder.Child = cardContent;

                cardBorder.MouseLeftButtonUp += (_, _) => ShowEventDetail(ev);

                if (isTop)
                {
                    card.Children.Add(cardBorder); card.Children.Add(connector);
                    TopEventsPanel.Children.Add(card); BottomEventsPanel.Children.Add(new Border { Width = cardWidth + spacing });
                }
                else
                {
                    card.Children.Add(connector); card.Children.Add(cardBorder);
                    BottomEventsPanel.Children.Add(card); TopEventsPanel.Children.Add(new Border { Width = cardWidth + spacing });
                }

                var markerPanel = new StackPanel { Width = cardWidth + spacing, HorizontalAlignment = HorizontalAlignment.Center };
                markerPanel.Children.Add(dot);
                YearMarkersPanel.Children.Add(markerPanel);
            }
        }

        private void ShowEventDetail(TimelineEvent ev)
        {
            var colors = CategoryColors.ContainsKey(ev.Category) ? CategoryColors[ev.Category] : CategoryColors["vietnam"];
            DetailHeaderBand.Background = new SolidColorBrush(colors.Text);
            txtDetailYear.Text = ev.Year < 0 ? $"Năm {System.Math.Abs(ev.Year)} TCN" : $"Năm {ev.Year}";
            txtDetailTitle.Text = ev.Title;

            DetailContentPanel.Children.Clear();
            DetailContentPanel.Children.Add(CreateDetailSection("📝 Nội dung", string.IsNullOrEmpty(ev.Detail) ? ev.Description : ev.Detail));
            if (!string.IsNullOrEmpty(ev.Figures)) DetailContentPanel.Children.Add(CreateDetailSection("👤 Nhân vật", ev.Figures));
            if (!string.IsNullOrEmpty(ev.Significance)) DetailContentPanel.Children.Add(CreateDetailSection("⭐ Ý nghĩa", ev.Significance));

            // Smooth animation to slide in DetailPanel
            var animation = new DoubleAnimation
            {
                To = 350,
                Duration = TimeSpan.FromSeconds(0.2),
                DecelerationRatio = 0.3
            };
            DetailPanel.BeginAnimation(WidthProperty, animation);
        }

        private StackPanel CreateDetailSection(string title, string content)
        {
            var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
            sp.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(63, 81, 181)), Margin = new Thickness(0, 0, 0, 4) });
            sp.Children.Add(new TextBlock { Text = content, FontSize = 16, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.DarkSlateGray });
            return sp;
        }

        private void BtnCloseDetail_Click(object sender, RoutedEventArgs e)
        {
            // Smooth animation to close DetailPanel
            var animation = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(0.2),
                DecelerationRatio = 0.3
            };
            DetailPanel.BeginAnimation(WidthProperty, animation);
        }

        private void FilterCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string cat)
            {
                _currentFilter = cat;
                RenderTimeline();
                UpdateFilterButtonStyles();
                TimelineScroll.ScrollToHorizontalOffset(0);
            }
        }

        private void UpdateFilterButtonStyles()
        {
            if (filterButtonsPanel == null) return;
            foreach (var child in filterButtonsPanel.Children)
            {
                if (child is Button btn && btn.Tag is string cat)
                {
                    bool isSelected = (cat == _currentFilter);
                    switch (cat)
                    {
                        case "all":
                            btn.Background = isSelected ? SelectedBrushAll : UnselectedBrushAll;
                            btn.Foreground = isSelected ? Brushes.White : SelectedBrushAll;
                            btn.BorderBrush = SelectedBrushAll;
                            break;
                        case "vietnam":
                            btn.Background = isSelected ? SelectedBrushVN : UnselectedBrushVN;
                            btn.Foreground = isSelected ? Brushes.White : SelectedBrushVN;
                            btn.BorderBrush = SelectedBrushVN;
                            break;
                        case "world":
                            btn.Background = isSelected ? SelectedBrushWorld : UnselectedBrushWorld;
                            btn.Foreground = isSelected ? Brushes.White : SelectedBrushWorld;
                            btn.BorderBrush = SelectedBrushWorld;
                            break;
                        case "science":
                            btn.Background = isSelected ? SelectedBrushScience : UnselectedBrushScience;
                            btn.Foreground = isSelected ? Brushes.White : SelectedBrushScience;
                            btn.BorderBrush = SelectedBrushScience;
                            break;
                        case "culture":
                            btn.Background = isSelected ? SelectedBrushCulture : UnselectedBrushCulture;
                            btn.Foreground = isSelected ? Brushes.White : SelectedBrushCulture;
                            btn.BorderBrush = SelectedBrushCulture;
                            break;
                    }
                }
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            RenderTimeline();
            TimelineScroll.ScrollToHorizontalOffset(0);
        }

        private void BtnAddEvent_Click(object sender, RoutedEventArgs e)
        {
            txtYear.Text = ""; txtTitle.Text = ""; txtDescription.Text = ""; txtDetail.Text = ""; txtFigures.Text = ""; txtSignificance.Text = "";
            DialogOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCancelDialog_Click(object sender, RoutedEventArgs e) => DialogOverlay.Visibility = Visibility.Collapsed;

        private void BtnShowHelp_Click(object sender, RoutedEventArgs e)
        {
            if (sideMenu != null) sideMenu.SelectedIndex = 1;
        }

        private void BtnCloseHelp_Click(object sender, RoutedEventArgs e)
        {
            if (sideMenu != null) sideMenu.SelectedIndex = 1;
        }

        private void BtnSaveEvent_Click(object sender, RoutedEventArgs e)
        {
            if (TryParseHistoricalYear(txtYear.Text.Trim(), out int year) && !string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                // If Trước Công nguyên (TCN) is selected from cmbEra, convert positive year to negative
                if (cmbEra.SelectedItem is ComboBoxItem eraItem && eraItem.Tag?.ToString() == "bce" && year > 0)
                {
                    year = -year;
                }

                var title = txtTitle.Text.Trim();
                var description = txtDescription.Text.Trim();
                var category = (cmbCategory.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "vietnam";
                var detail = txtDetail.Text.Trim();
                var figures = txtFigures.Text.Trim();
                var significance = txtSignificance.Text.Trim();

                // Save directly to SQLite
                Helpers.DbManager.SaveCustomTimelineEvent(year, title, description, category, detail, figures, significance);

                _allEvents.Add(new TimelineEvent
                {
                    Year = year, Title = title, Description = description,
                    Detail = detail, Figures = figures, Significance = significance,
                    Category = category,
                    IsCustom = true
                });

                DialogOverlay.Visibility = Visibility.Collapsed;
                RenderTimeline();
            }
            else
            {
                MessageBox.Show("Vui lòng nhập năm hợp lệ (ví dụ: 1010 hoặc 207 TCN) và tiêu đề sự kiện.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa toàn bộ sự kiện hiện tại khỏi dòng thời gian không?\n(Lưu ý: Các sự kiện tự tạo của bạn cũng sẽ bị xóa khỏi bộ nhớ thiết bị)",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _allEvents.Clear();
                try
                {
                    Helpers.DbManager.ClearAllCustomTimelineEvents();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to clear custom events in database: {ex.Message}");
                }
                RenderTimeline();
            }
        }

        public void Dispose() { _allEvents.Clear(); }

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new System.Collections.Generic.List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "🏛️",
                    Title = isVN ? "Giám tuyển & Thiết kế Triển lãm" : "Curator Work & Exhibition Design",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_history_timeline_1_{suffix}.png",
                    Description = isVN 
                        ? "Các nhà giám tuyển bảo tàng sử dụng dải thời gian lịch sử để lên ý tưởng, sắp xếp hiện vật theo đúng trình tự thời gian và thiết kế không gian trưng bày theo chủ đề, giúp khách tham quan dễ dàng nắm bắt sự phát triển lịch sử văn hóa." 
                        : "Museum curators use historical timelines to brainstorm, arrange artifacts in correct chronological order, and design themed exhibition spaces, helping visitors easily grasp the progression of cultural history."
                },
                new PracticalAppItem
                {
                    Icon = "🕵️",
                    Title = isVN ? "Khảo cổ học Pháp lý & Xác định Niên đại" : "Forensic Archaeology & Historical Dating",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_history_timeline_2_{suffix}.png",
                    Description = isVN 
                        ? "Trong khảo cổ học, việc đối chiếu hiện vật tìm được với dải niên biểu lịch sử giúp liên kết các khám phá mới với các sự kiện đã biết, hỗ trợ xác định niên đại của các bộ hài cốt, công cụ và công trình cổ đại." 
                        : "In archaeology, cross-referencing discovered artifacts with historical timelines helps link new discoveries to known events, supporting the dating of ancient remains, tools, and structures."
                },
                new PracticalAppItem
                {
                    Icon = "📊",
                    Title = isVN ? "Nghiên cứu Học thuật & Dự báo Xu hướng" : "Academic Research & Trend Forecasting",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_history_timeline_3_{suffix}.png",
                    Description = isVN 
                        ? "Các nhà sử học và nhà kinh tế học vẽ dải thời gian của các cuộc khủng hoảng, cách mạng công nghiệp và thay đổi chính sách trong quá khứ để phân tích chu kỳ, tìm quy luật nhân quả và dự báo các xu hướng phát triển tiếp theo của xã hội." 
                        : "Historians and economists plot timelines of past crises, industrial revolutions, and policy changes to analyze cycles, find causal patterns, and forecast future developmental trends in society."
                }
            };

            try
            {
                if (practicalAppViewer != null) practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for HistoryTimelineTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewPractice == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewPractice.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewPractice.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}